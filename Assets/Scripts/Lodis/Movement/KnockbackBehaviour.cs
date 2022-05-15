using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Lodis.Gameplay;
using UnityEngine.Events;
using Lodis.ScriptableObjects;
using Lodis.Utility;

namespace Lodis.Movement
{

    [RequireComponent(typeof(GridPhysicsBehaviour))]
    public class KnockbackBehaviour : HealthBehaviour
    {
        [SerializeField]
        private  float _netForceLandingTolerance = 0.5f;
        [Tooltip("How fast will objects be allowed to travel in knockback")]
        [SerializeField]
        private ScriptableObjects.FloatVariable _maxMagnitude;

        private GridMovementBehaviour _movementBehaviour;
        private CharacterDefenseBehaviour _defenseBehaviour;
        private GridPhysicsBehaviour _gridPhysicsBehaviour;
       
        private Vector2 _newPanelPosition = new Vector2(float.NaN, float.NaN );
        private float _lastBaseKnockBack;
        private Vector3 _velocityOnLaunch;

        [SerializeField]
        private bool _tumbling;
        [SerializeField]
        private FloatVariable _gravityIncreaseRate;
        [SerializeField]
        private FloatVariable _gravityIncreaseValue;
        private RoutineBehaviour.TimedAction _gravityIncreaseTimer = new RoutineBehaviour.TimedAction();
        private float _startGravity;
        [SerializeField]
        private bool _inFreeFall;

        private Coroutine _currentCoroutine;

        private UnityAction _onKnockBack;
        private UnityAction _onKnockBackStart;
        private UnityAction _onKnockBackTemp;
        private UnityAction _onKnockBackStartTemp;
        private UnityAction _onTakeDamage;
        private UnityAction _onTakeDamageStart;
        private UnityAction _onTakeDamageStartTemp;
        private UnityAction _onTakeDamageTemp;
        private UnityAction _onHitStun;
        private UnityAction _onHitStunTemp;
       
        [Tooltip("The amount of time it takes for this object to regain footing after landing")]
        [SerializeField]
        private float _landingTime;
        [SerializeField]
        private float _knockDownTime;
        [SerializeField]
        private float _knockDownRecoverTime;
        [SerializeField]
        private float _knockDownRecoverInvincibleTime;
        [SerializeField]
        private float _knockDownLandingTime;

        [SerializeField]
        private Vector3 _freeFallGroundedPoint;
        [SerializeField]
        private Vector3 _freeFallGroundedPointExtents;
        [Tooltip("The position that will be used to check if this character is grounded")]
        [SerializeField]
        private Vector3 _idleGroundedPoint;
        [SerializeField]
        private Vector3 _idleGroundedPointExtents;
        [SerializeField]
        private Lodis.ScriptableObjects.FloatVariable _minimumLaunchMagnitude;
        [SerializeField]
        private bool _inHitStun;
        private bool _isFlinching;
        private float _timeInCurrentHitStun;
        private RoutineBehaviour.TimedAction _hitStunTimer = new RoutineBehaviour.TimedAction();
        private float _lastTotalKnockBack;
        private float _lastTimeInKnockBack;

        public float LastTimeInKnockBack
        {
            get { return _lastTimeInKnockBack; }
        }

        /// <summary>
        /// Whether or not this object is current regaining footing after hitting the ground
        /// </summary>
        public bool Landing { get; private set; }

        /// <summary>
        /// Returns if the object is in knockback
        /// </summary>
        public bool IsTumbling { get => _tumbling; set => _tumbling = value; }

        /// <summary>
        /// Returns the velocity of this object when it was first launched
        /// </summary>
        public Vector3 LaunchVelocity { get => _velocityOnLaunch; }

        /// <summary>
        /// The scale of the last base knock back value applied to the object
        /// </summary>
        public float LastBaseKnockBack { get => _lastBaseKnockBack; }
        
        /// <summary>
        /// The scale of the last knock back value applied to the object
        /// </summary>
        public float LastTotalKnockBack { get => _lastTotalKnockBack; }

        /// <summary>
        /// Whether or not this object is in the air without being in a tumble state
        /// </summary>
        public bool InFreeFall 
        {
            get =>_inFreeFall;
            set 
            {
                if (value)
                    _tumbling = !value;

                _inFreeFall = value;
            }
        }
       
        public float LandingTime { get => _landingTime;}
        public bool IsDown { get; private set; }
        public bool RecoveringFromFall { get; private set; }
        public float KnockDownRecoverTime { get => _knockDownRecoverTime; set => _knockDownRecoverTime = value; }
        public float KnockDownLandingTime { get => _knockDownLandingTime; set => _knockDownLandingTime = value; }
        public GridPhysicsBehaviour Physics { get => _gridPhysicsBehaviour; set => _gridPhysicsBehaviour = value; }
        public bool InHitStun { get => _inHitStun;}
        public bool IsFlinching { get => _isFlinching; }
        public float TimeInCurrentHitStun { get => _timeInCurrentHitStun; }

        private void Awake()
        {
            _movementBehaviour = GetComponent<GridMovementBehaviour>();
            _defenseBehaviour = GetComponent<CharacterDefenseBehaviour>();
            Physics = GetComponent<GridPhysicsBehaviour>();
            Physics.AddOnCollisionWithGroundEvent(args => { if (InFreeFall) TryStartLandingLag(); });
            AddOnStunAction(() => { if (Landing) StopCoroutine(_currentCoroutine); });
            _startGravity = Physics.Gravity;
            AddOnTakeDamageAction(IncreaseKnockbackGravity);
        }

        // Start is called before the first frame update
        protected override void Start()
        {
            base.Start();
            AliveCondition = condition => gameObject.activeInHierarchy;
            _onKnockBack += () => Landing = false;
            _onKnockBackStart += () => { Stunned = false; _movementBehaviour.CurrentPanel.Occupied = false; };
        }

        /// <summary>
        /// Add a listener to the onKnockBack event.
        /// </summary>
        /// <param name="action">The new listener for the event.</param>
        public void AddOnKnockBackAction(UnityAction action)
        {
            _onKnockBack += action;
        }

        /// <summary>
        /// Add a listener to the onKnockBack event.
        /// </summary>
        /// <param name="action">The new listener for the event.</param>
        public void AddOnKnockBackTempAction(UnityAction action)
        {
            _onKnockBackTemp += action;
        }

        /// <summary>
        /// Add a listener to the onKnockBackStart event. Called before knock back is applied.
        /// </summary>
        /// <param name="action">The new listener for the event.</param>
        public void AddOnKnockBackStartAction(UnityAction action)
        {
            _onKnockBackStart += action;
        }

        /// <summary>
        /// Add a listener to the onKnockBackStart event. Called before knock back is applied.
        /// Cleared after called
        /// </summary>
        /// <param name="action">The new listener for the event.</param>
        public void AddOnKnockBackStartTempAction(UnityAction action)
        {
            _onKnockBackStartTemp += action;
        }

        /// <summary>
        /// Removes the listener from the on knock back start temporary event
        /// </summary>
        /// <param name="action"></param>
        public void RemoveOnKnockBackStartTempAction(UnityAction action)
        {
            _onKnockBackStartTemp -= action;
        }

        /// <summary>
        /// Adds an action to the event called when this object is damaged
        /// </summary>
        /// <param name="action">The new listener to to the event</param>
        public void AddOnTakeDamageAction(UnityAction action)
        {
            _onTakeDamage += action;
        }

        /// <summary>
        /// Adds an action to the event called when this object is damaged.
        /// Listeners cleared after event is called
        /// </summary>
        /// <param name="action">The new listener to to the event</param>
        public void AddOnTakeDamageTempAction(UnityAction action)
        {
            _onTakeDamageTemp += action;
        }/// <summary>
        /// Adds an action to the event called right before damage is applied
        /// </summary>
        /// <param name="action">The new listener to to the event</param>
        public void AddOnTakeDamageStartAction(UnityAction action)
        {
            _onTakeDamageStart += action;
        }

        /// <summary>
        /// Adds an action to the event called right before damage is applied.
        /// Listeners cleared after event is called
        /// </summary>
        /// <param name="action">The new listener to to the event</param>
        public void AddOnTakeDamageStartTempAction(UnityAction action)
        {
            _onTakeDamageStartTemp += action;
        }

        public void AddOnHitStunAction(UnityAction action)
        {
            _onHitStun += action;
        }
        public void AddOnHitStunTempAction(UnityAction action)
        {
            _onHitStunTemp += action;
        }

        private void IncreaseKnockbackGravity()
        {
            if (!IsTumbling) return;
            Physics.Gravity += _gravityIncreaseValue.Value;
        }

        /// <summary>
        /// Starts landing lag if the object just fell onto a structure
        /// </summary>
        /// <param name="args"></param>
        public void TryStartLandingLag(params object[] args)
        {
            if (!InFreeFall && !IsTumbling || Landing || IsDown)
            {
                Physics.StopVelocity();
                return;
            }

            if (Stunned) return;


            Physics.StopVelocity();

            _currentCoroutine = StartCoroutine(StartLandingLag());

            _onKnockBackTemp += CancelLanding;
        }

        private void CancelLanding()
        {
            StopCoroutine(_currentCoroutine);
            IsDown = false;
        }

        protected override IEnumerator ActivateStun(float time)
        {
            MovesetBehaviour moveset = GetComponent<MovesetBehaviour>();
            Input.InputBehaviour inputBehaviour = GetComponent<Input.InputBehaviour>();
            GridMovementBehaviour movement = GetComponent<GridMovementBehaviour>();
            

            Stunned = true;

            if (InFreeFall || IsTumbling)
               Physics.FreezeInPlaceByCondition(condition =>!Stunned, false, true);

            if (moveset)
            {
                moveset.enabled = false;
                moveset.EndCurrentAbility();
            }
            if (inputBehaviour)
            {
                inputBehaviour.enabled = false;
                inputBehaviour.StopAllCoroutines();
            }
            if (movement)
                movement.DisableMovement(condition => Stunned == false, false, true);

            _onKnockBackTemp += CancelStun;

            yield return new WaitForSeconds(time);

            if (moveset)
                moveset.enabled = true;
            if (inputBehaviour)
                inputBehaviour.enabled = true;

            Stunned = false;
        }

        public override void CancelStun()
        {
            base.CancelStun();

            Physics.UnfreezeObject();
        }

        public void CancelHitStun()
        {
            if (_hitStunTimer.GetEnabled())
                RoutineBehaviour.Instance.StopTimedAction(_hitStunTimer);

            _timeInCurrentHitStun = 0;
            _inHitStun = false;
            _isFlinching = false;
        }

        public override void OnTriggerEnter(Collider other)
        {
            Debug.Log("test");
        }
        public override void OnCollisionEnter(Collision collision)
        {

            HealthBehaviour damageScript = collision.gameObject.GetComponent<HealthBehaviour>();

            if (damageScript == null || !IsTumbling || IsInvincible)
                return;

            KnockbackBehaviour knockBackScript = damageScript as KnockbackBehaviour;

            //If no knockback script is attached, use this script to deal damage
            if (!knockBackScript)
                knockBackScript = this;

            float velocityMagnitude = knockBackScript.Physics.LastVelocity.magnitude;

            //Apply ricochet force and damage
            damageScript.TakeDamage(name, velocityMagnitude, 0, 0, DamageType.KNOCKBACK);
        }

        private IEnumerator StartLandingLag()
        {
            Landing = true;
            _movementBehaviour.DisableMovement(condition => !Landing, false, true);
            _lastTimeInKnockBack = 0;
            if (_defenseBehaviour)
            {
                if (_defenseBehaviour.BreakingFall)
                {
                    InFreeFall = false;
                    _tumbling = false;
                    yield return new WaitForSeconds(_defenseBehaviour.GroundTechLength);
                    Landing = false;
                    Physics.MakeKinematic();
                    yield return null;
                }
            }

            if (InFreeFall)
            {
                InFreeFall = false;
                yield return new WaitForSeconds(_landingTime);
                Landing = false;
            }
            else if (IsTumbling)
            {
                _tumbling = false;
                yield return new WaitForSeconds(KnockDownLandingTime);
                RoutineBehaviour.Instance.StopTimedAction(_gravityIncreaseTimer);
                Physics.Gravity = _startGravity;
                Landing = false;
                //Start knockdown
                IsDown = true;
                SetInvincibilityByTimer(_knockDownRecoverInvincibleTime);
                _movementBehaviour.DisableMovement(condition => !RecoveringFromFall && !IsDown, false, true);

                yield return new WaitForSeconds(_knockDownTime);
                RecoveringFromFall = true;
                IsDown = false;
                //Start recovery from knock down
                Physics.MakeKinematic(); 
                RoutineBehaviour.Instance.StartNewTimedAction(args => RecoveringFromFall = false, TimedActionCountType.SCALEDTIME, KnockDownRecoverTime);
            }

            if (_hitStunTimer.GetEnabled() && InHitStun)
            {
                RoutineBehaviour.Instance.StopTimedAction(_hitStunTimer);
                _inHitStun = false;
            }
        }

        public void ActivateHitStunByTimer(float timeInHitStun)
        {
            if (timeInHitStun <= 0)
            {
                _isFlinching = false;
                return;
            }

            _inHitStun = true;
            _timeInCurrentHitStun = timeInHitStun;

            _movementBehaviour.DisableMovement(condition => CheckIfIdle(), false, true);

            if (_hitStunTimer.GetEnabled())
                RoutineBehaviour.Instance.StopTimedAction(_hitStunTimer);

            _hitStunTimer = RoutineBehaviour.Instance.StartNewTimedAction(args => { _inHitStun = false; _isFlinching = false; _timeInCurrentHitStun = 0; }, TimedActionCountType.SCALEDTIME, timeInHitStun);
            _onHitStun?.Invoke();
            _onHitStunTemp?.Invoke();
            _onHitStunTemp = null;
        }


        /// <summary>
        /// Gets whether or not this object is on the ground and not being effected by any forces
        /// </summary>
        public bool CheckIfIdle()
        {
            return !IsTumbling && !InFreeFall && Physics.ObjectAtRest && !Landing && !InHitStun &&!IsFlinching && !IsDown && !Stunned && !RecoveringFromFall;
        }
        public override float TakeDamage(string attacker, float damage, float baseKnockBack = 0, float hitAngle = 0, DamageType damageType = DamageType.DEFAULT, float hitStun = 0)
        {
            _onTakeDamageStart?.Invoke();
            _onTakeDamageStartTemp?.Invoke();

            //Return if there is no rigidbody or movement script attached
            if (!_movementBehaviour || IsInvincible)
                return 0;

            //Update current knockback scale
            _lastBaseKnockBack = baseKnockBack;

            //Adds damage to the total damage
            Health += damage;

            ActivateHitStunByTimer(hitStun);

            _onTakeDamage?.Invoke();
            _onTakeDamageTemp?.Invoke();
            _onTakeDamageTemp = null;

            float totalKnockback = (_lastBaseKnockBack + (_lastBaseKnockBack * (Health / BlackBoardBehaviour.Instance.MaxKnockBackHealth.Value * 100)));

            _lastTotalKnockBack = totalKnockback;
            //Calculates force and applies it to the rigidbody
            Vector3 knockBackForce = Physics.CalculatGridForce(totalKnockback, hitAngle);
            if (hitStun > 0)
                _isFlinching = true;

            if ((knockBackForce / Physics.Mass).magnitude > _minimumLaunchMagnitude.Value)
            {
                _onKnockBackStart?.Invoke();
                _onKnockBackStartTemp?.Invoke();
                _onKnockBackStartTemp = null;

                _velocityOnLaunch = knockBackForce;
                Physics.Rigidbody.isKinematic = false;

                if (_movementBehaviour.IsMoving)
                {
                    _movementBehaviour.canCancelMovement = true;
                    _movementBehaviour.MoveToPanel(_movementBehaviour.TargetPanel, true);
                    _movementBehaviour.canCancelMovement = false;
                }

                //Disables object movement on the grid
                _movementBehaviour.DisableMovement(condition => CheckIfIdle(), false, true);

                //Add force to objectd
                Physics.ApplyImpulseForce(_velocityOnLaunch);

                if (_velocityOnLaunch.magnitude > 0)
                {
                    _inFreeFall = false;
                    _tumbling = true;
                    _onKnockBack?.Invoke();
                    _onKnockBackTemp?.Invoke();
                    _onKnockBackTemp = null;
                }
            }

            return damage;
        }
        public override float TakeDamage(HitColliderInfo info, GameObject attacker)
        {
            _onTakeDamageStart?.Invoke();
            _onTakeDamageStartTemp?.Invoke();

            //Return if there is no rigidbody or movement script attached
            if (!_movementBehaviour || IsInvincible)
                return 0;

            //Adds damage to the total damage
            Health += info.Damage;

            ActivateHitStunByTimer(info.HitStunTime);

            _onTakeDamage?.Invoke();
            _onTakeDamageTemp?.Invoke();
            _onTakeDamageTemp = null;

            float totalKnockback = info.BaseKnockBack + Mathf.Round((Health / 100 * info.KnockBackScale));

            _lastTotalKnockBack = totalKnockback;
            //Calculates force and applies it to the rigidbody
            Vector3 knockBackForce = Physics.CalculatGridForce(totalKnockback, info.HitAngle);
            if (info.HitStunTime > 0)
                _isFlinching = true;

            if ((knockBackForce / Physics.Mass).magnitude > _minimumLaunchMagnitude.Value)
            {
                _onKnockBackStart?.Invoke();
                _onKnockBackStartTemp?.Invoke();
                _onKnockBackStartTemp = null;

                _velocityOnLaunch = knockBackForce;
                Physics.Rigidbody.isKinematic = false;

                if (_movementBehaviour.IsMoving)
                {
                    _movementBehaviour.canCancelMovement = true;
                    _movementBehaviour.MoveToPanel(_movementBehaviour.TargetPanel, true);
                    _movementBehaviour.canCancelMovement = false;
                }

                //Disables object movement on the grid
                _movementBehaviour.DisableMovement(condition => CheckIfIdle(), false, true);

                //Add force to objectd
                Physics.ApplyImpulseForce(_velocityOnLaunch);

                if (_velocityOnLaunch.magnitude > 0)
                {
                    _inFreeFall = false;
                    _tumbling = true;
                    _onKnockBack?.Invoke();
                    _onKnockBackTemp?.Invoke();
                    _onKnockBackTemp = null;
                }
            }

            return info.Damage;
        }

        public override float TakeDamage(string attacker, AbilityData abilityData, DamageType damageType = DamageType.DEFAULT)
        {
            _onTakeDamageStart?.Invoke();
            _onTakeDamageStartTemp?.Invoke();

            //Return if there is no rigidbody or movement script attached
            if (!_movementBehaviour || IsInvincible)
                return 0;

            //Update current knockback scale
            _lastBaseKnockBack = abilityData.GetCustomStatValue("baseKnockBack");

            //Adds damage to the total damage
            Health += abilityData.GetCustomStatValue("Damage");

            ActivateHitStunByTimer(abilityData.GetCustomStatValue("HitStunTimer"));

            _onTakeDamage?.Invoke();
            _onTakeDamageTemp?.Invoke();
            _onTakeDamageTemp = null;

            float totalKnockback = (_lastBaseKnockBack + (_lastBaseKnockBack * Health / BlackBoardBehaviour.Instance.MaxKnockBackHealth.Value)) * (abilityData.GetColliderInfo(0).KnockBackScale / 100);

            _lastTotalKnockBack = totalKnockback;
            //Calculates force and applies it to the rigidbody
            Vector3 knockBackForce = Physics.CalculatGridForce(totalKnockback, abilityData.GetCustomStatValue("HitAngle"));

            if (abilityData.GetCustomStatValue("HitStunTimer") > 0)
                _isFlinching = true;

            if ((knockBackForce / Physics.Mass).magnitude > _minimumLaunchMagnitude.Value)
            {
                _onKnockBackStart?.Invoke();
                _onKnockBackStartTemp?.Invoke();
                _onKnockBackStartTemp = null;

                _velocityOnLaunch = knockBackForce;
                Physics.Rigidbody.isKinematic = false;

                if (_movementBehaviour.IsMoving)
                {
                    _movementBehaviour.canCancelMovement = true;
                    _movementBehaviour.MoveToPanel(_movementBehaviour.TargetPanel, true);
                    _movementBehaviour.canCancelMovement = false;
                }

                //Disables object movement on the grid
                _movementBehaviour.DisableMovement(condition => CheckIfIdle(), false, true);

                //Add force to objectd
                Physics.ApplyImpulseForce(_velocityOnLaunch);

                if (_velocityOnLaunch.magnitude > 0)
                {
                    _inFreeFall = false;
                    _tumbling = true;
                    _onKnockBack?.Invoke();
                    _onKnockBackTemp?.Invoke();
                    _onKnockBackTemp = null;
                }
            }

            return abilityData.GetCustomStatValue("Damage");
        }

        /// /// <summary>
        /// Damages this game object and applies a backwards force based on the angle
        /// </summary>
        /// <param name="attacker">The name of the object that damaged this object. Used for debugging</param>
        /// <param name="damage">The amount of damage being applied to the object. 
        /// Ring barriers only break if the damage amount is greater than the total health</param>
        /// <param name="baseKnockBack">How many panels far will this attakc make the object travel</param>
        /// <param name="hitAngle">The angle to launch the object</param>
        /// <param name="knockBackIsFixed">If true, the knock back won't be scaled based on health or mass</param>
        /// <param name="ignoreMass">If true, the force applied to the object won't change based in mass</param>
        /// <param name="damageType">The type of damage this object will take</param>
        public float TakeDamage(string attacker, float damage, float baseKnockBack, float hitAngle, float hitStun, bool knockBackIsFixed, bool ignoreMass, DamageType damageType = DamageType.DEFAULT)
        {
            _onTakeDamageStart?.Invoke();
            _onTakeDamageStartTemp?.Invoke();

            //Return if there is no rigidbody or movement script attached
            if (!_movementBehaviour || IsInvincible)
                return 0;

            //Adds damage to the total damage
            Health += damage;

            ActivateHitStunByTimer(hitStun);
            _lastBaseKnockBack = baseKnockBack;
            //Invoke damage events
            _onTakeDamage?.Invoke();
            _onTakeDamageTemp?.Invoke();
            _onTakeDamageTemp = null;

            //Apply the damage and weight to find the amount of knock back to be applied
            float totalKnockback = 0;
            if (!knockBackIsFixed)
                totalKnockback = (baseKnockBack + (baseKnockBack * (Health / BlackBoardBehaviour.Instance.MaxKnockBackHealth.Value * 100)));
            else
                totalKnockback = baseKnockBack;

            //Update current knockback scale
            _lastTotalKnockBack = totalKnockback;

            //Calculates force and applies it to the rigidbody
            Vector3 knockBackForce = Physics.CalculatGridForce(totalKnockback, hitAngle);

            if (hitStun > 0)
                _isFlinching = true;

            if ((knockBackForce / Physics.Mass).magnitude > _minimumLaunchMagnitude.Value)
            {

                //Invoke knock back events
                _onKnockBackStart?.Invoke();
                _onKnockBackStartTemp?.Invoke();
                _onKnockBackStartTemp = null;

                _velocityOnLaunch = knockBackForce;
                Physics.Rigidbody.isKinematic = false;

                if (_movementBehaviour.IsMoving)
                {
                    _movementBehaviour.canCancelMovement = true;
                    _movementBehaviour.MoveToPanel(_movementBehaviour.TargetPanel, true);
                    _movementBehaviour.canCancelMovement = false;
                }

                //Disables object movement on the grid
                _movementBehaviour.DisableMovement(condition => CheckIfIdle(), false, true);
                if (knockBackIsFixed)
                    //Add force to objectd using mass
                    Physics.ApplyImpulseForce(_velocityOnLaunch);
                else
                    //Add force to the object ignoring mass
                    Physics.ApplyVelocityChange(_velocityOnLaunch);

                if (_velocityOnLaunch.magnitude > 0)
                {
                    _inFreeFall = false;
                    _tumbling = true;
                    _onKnockBack?.Invoke();
                    _onKnockBackTemp?.Invoke();
                    _onKnockBackTemp = null;
                }
            }

            return damage;
        }


        public float TakeDamage(string attacker, AbilityData abilityData, bool knockBackIsFixed, bool ignoreMass, DamageType damageType = DamageType.DEFAULT)
        {
            _onTakeDamageStart?.Invoke();
            _onTakeDamageStartTemp?.Invoke();

            //Return if there is no rigidbody or movement script attached
            if (!_movementBehaviour || IsInvincible)
                return 0;

            float baseKnockBack = abilityData.GetCustomStatValue("baseKnockBack");
            float damage = abilityData.GetCustomStatValue("Damage");
            //Adds damage to the total damage
            Health += damage;

            ActivateHitStunByTimer(abilityData.GetCustomStatValue("HitStunTimer"));

            //Invoke damage events
            _onTakeDamage?.Invoke();
            _onTakeDamageTemp?.Invoke();
            _onTakeDamageTemp = null;

            _lastBaseKnockBack = baseKnockBack;
            //Apply the damage and weight to find the amount of knock back to be applied
            float totalKnockback = 0;
            if (!knockBackIsFixed)
                totalKnockback = (baseKnockBack + (baseKnockBack * (Health / BlackBoardBehaviour.Instance.MaxKnockBackHealth.Value * 100)));
            else
                totalKnockback = baseKnockBack;

            //Update current knockback scale
            _lastTotalKnockBack = totalKnockback;

            //Calculates force and applies it to the rigidbody
            Vector3 knockBackForce = Physics.CalculatGridForce(totalKnockback, abilityData.GetCustomStatValue("HitAngle"));

            if (abilityData.GetCustomStatValue("HitStunTimer") > 0)
                _isFlinching = true;

            if ((knockBackForce / Physics.Mass).magnitude > _minimumLaunchMagnitude.Value)
            {

                //Invoke knock back events
                _onKnockBackStart?.Invoke();
                _onKnockBackStartTemp?.Invoke();
                _onKnockBackStartTemp = null;

                _velocityOnLaunch = knockBackForce;
                Physics.Rigidbody.isKinematic = false;

                if (_movementBehaviour.IsMoving)
                {
                    _movementBehaviour.canCancelMovement = true;
                    _movementBehaviour.MoveToPanel(_movementBehaviour.TargetPanel, true);
                    _movementBehaviour.canCancelMovement = false;
                }

                //Disables object movement on the grid
                _movementBehaviour.DisableMovement(condition => CheckIfIdle(), false, true);
                if (knockBackIsFixed)
                    //Add force to objectd using mass
                    Physics.ApplyImpulseForce(_velocityOnLaunch);
                else
                    //Add force to the object ignoring mass
                    Physics.ApplyVelocityChange(_velocityOnLaunch);

                if (_velocityOnLaunch.magnitude > 0)
                {
                    _inFreeFall = false;
                    _tumbling = true;
                    _onKnockBack?.Invoke();
                    _onKnockBackTemp?.Invoke();
                    _onKnockBackTemp = null;
                }
            }

            return damage;
        }

        private void UpdateGroundedColliderPosition()
        {
            if (IsTumbling)
            {
                Physics.GroundedBoxPosition = Physics.BounceCollider.bounds.center;
                Physics.GroundedBoxExtents = Physics.BounceCollider.bounds.extents * 2;
            }
            else if (InFreeFall)
            {
                Physics.GroundedBoxPosition = _freeFallGroundedPoint + transform.position;
                Physics.GroundedBoxExtents = _freeFallGroundedPointExtents;
            }
            else
            {
                Physics.GroundedBoxPosition = _idleGroundedPoint + transform.position;
                Physics.GroundedBoxExtents = _idleGroundedPointExtents;
            }
        }

        private void OnDrawGizmos()
        {
            if(!Application.isPlaying)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawCube(_freeFallGroundedPoint + transform.position, _freeFallGroundedPointExtents);
                Gizmos.color = Color.green;
                Gizmos.DrawCube(_idleGroundedPoint + transform.position, _idleGroundedPointExtents);
            }
        }

        private void FixedUpdate()
        {
            if (Physics.Rigidbody.velocity.magnitude > _maxMagnitude.Value)
                Physics.Rigidbody.velocity = Physics.Rigidbody.velocity.normalized * _maxMagnitude.Value;

            UpdateGroundedColliderPosition();
        }

        public override void Update()
        {
            base.Update();
            if (Physics.IsGrounded && Physics.Acceleration.magnitude <= _netForceLandingTolerance && Physics.LastVelocity.y <= 0 && (IsTumbling || InFreeFall))
            {
                TryStartLandingLag();
            }

            if (IsTumbling) _lastTimeInKnockBack += Time.deltaTime;
        }
    }


    /// <summary>
    /// Editor script to test attacks
    /// </summary>
#if UNITY_EDITOR

    [CustomEditor(typeof(KnockbackBehaviour))]
    class KnockbackEditor : Editor
    {
        private KnockbackBehaviour _owner;
        private float _damage;
        private float _baseKnockBack;
        private float _hitAngle;
        private float _hitStun;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            _owner = (KnockbackBehaviour)target;

            _damage = EditorGUILayout.FloatField("Damage", _damage);
            _baseKnockBack = EditorGUILayout.FloatField("Knockback Scale", _baseKnockBack);
            _hitAngle = EditorGUILayout.FloatField("Hit Angle", _hitAngle);
            _hitStun = EditorGUILayout.FloatField("Hit Stun", _hitStun);

            if (GUILayout.Button("Test Attack"))
            {
                _owner.TakeDamage(name, _damage, _baseKnockBack, _hitAngle, DamageType.KNOCKBACK, _hitStun);
            }
        }
    }

#endif
}


