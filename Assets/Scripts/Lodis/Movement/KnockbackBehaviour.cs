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
        [Tooltip("How fast will objects be allowed to travel in knockback")]
        [SerializeField]
        private ScriptableObjects.FloatVariable _maxMagnitude;

        private GridMovementBehaviour _movementBehaviour;
        private CharacterDefenseBehaviour _defenseBehaviour;
        private GridPhysicsBehaviour _gridPhysicsBehaviour;
       
        private Vector2 _newPanelPosition = new Vector2(float.NaN, float.NaN );
        private float _currentKnockBackScale;
        private Vector3 _velocityOnLaunch;

        [SerializeField]
        private bool _tumbling;
        [SerializeField]
        private bool _inFreeFall;

        private Coroutine _currentCoroutine;

        private UnityAction _onKnockBack;
        private UnityAction _onKnockBackStart;
        private UnityAction _onTakeDamage;
        private UnityAction _onKnockBackTemp;
        private UnityAction _onKnockBackStartTemp;
        private UnityAction _onTakeDamageTemp;
       
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
        private bool _inHitStun;
        private RoutineBehaviour.TimedAction _hitStunTimer;

        /// <summary>
        /// Whether or not this object is current regaining footing after hitting the ground
        /// </summary>
        public bool Landing { get; private set; }

        /// <summary>
        /// Returns if the object is in knockback
        /// </summary>
        public bool Tumbling {get => _tumbling; }

        /// <summary>
        /// Returns the velocity of this object when it was first launched
        /// </summary>
        public Vector3 LaunchVelocity { get => _velocityOnLaunch; }

        /// <summary>
        /// The scale of the last knock back value applied to the object
        /// </summary>
        public float CurrentKnockBackScale { get => _currentKnockBackScale; }

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

        private void Awake()
        {
            _movementBehaviour = GetComponent<GridMovementBehaviour>();
            _defenseBehaviour = GetComponent<CharacterDefenseBehaviour>();
            Physics = GetComponent<GridPhysicsBehaviour>();
        }

        // Start is called before the first frame update
        void Start()
        {
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
        }

        /// <summary>
        /// Starts landing lag if the object just fell onto a structure
        /// </summary>
        /// <param name="args"></param>
        public void TryStartLandingLag(params object[] args)
        {
            if (!InFreeFall && !Tumbling || Landing || IsDown)
            {
                Physics.StopVelocity();
                return;
            }

            _currentCoroutine = StartCoroutine(StartLandingLag());

            _onKnockBackTemp += CancelLanding;
        }

        private void CancelLanding()
        {
            StopCoroutine(_currentCoroutine);
        }

        protected override IEnumerator ActivateStun(float time)
        {
            MovesetBehaviour moveset = GetComponent<MovesetBehaviour>();
            Input.InputBehaviour inputBehaviour = GetComponent<Input.InputBehaviour>();
            GridMovementBehaviour movement = GetComponent<GridMovementBehaviour>();

            Stunned = true;

            if (InFreeFall || Tumbling)
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

        public override void OnCollisionEnter(Collision collision)
        {
            HealthBehaviour damageScript = collision.gameObject.GetComponent<HealthBehaviour>();

            if (damageScript == null || !Tumbling || IsInvincible)
                return;

            KnockbackBehaviour knockBackScript = damageScript as KnockbackBehaviour;

            //If no knockback script is attached, use this script to deal damage
            if (!knockBackScript)
                knockBackScript = this;

            float velocityMagnitude = knockBackScript.Physics.LastVelocity.magnitude;

            //Apply ricochet force and damage
            damageScript.TakeDamage(name, velocityMagnitude, 0, 0, DamageType.KNOCKBACK);
        }

        public override void OnTriggerEnter(Collider other)
        {
            HealthBehaviour damageScript = other.gameObject.GetComponent<HealthBehaviour>();

            if (damageScript == null || !Tumbling || IsInvincible)
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

            if (_defenseBehaviour)
            {
                if (_defenseBehaviour.BreakingFall)
                {
                    InFreeFall = false;
                    _tumbling = false;
                    yield return new WaitForSeconds(_defenseBehaviour.FallBreakLength);
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
            else if (Tumbling)
            {
                yield return new WaitForSeconds(KnockDownLandingTime);

                //Start knockdown
                IsDown = true;
                SetInvincibilityByTimer(_knockDownRecoverInvincibleTime);

                yield return new WaitForSeconds(_knockDownTime);

                //Start recovery from knock down
                Landing = false;
                _movementBehaviour.DisableMovement(condition => !IsDown, false, true);
                RecoveringFromFall = true;
                Physics.MakeKinematic(); 
                RoutineBehaviour.Instance.StartNewTimedAction(args => { IsDown = false; RecoveringFromFall = false; } , TimedActionCountType.SCALEDTIME, KnockDownRecoverTime);
            }
        }

        public void ActivateHitStunByTimer(float timeInHitStun)
        {
            _inHitStun = true;

            if(_hitStunTimer.GetEnabled())
                RoutineBehaviour.Instance.StopTimedAction(_hitStunTimer);

            _hitStunTimer = RoutineBehaviour.Instance.StartNewTimedAction(args => _inHitStun = false, TimedActionCountType.SCALEDTIME, timeInHitStun);
        }


        /// <summary>
        /// Gets whether or not this object is on the ground and not being effected by any forces
        /// </summary>
        public bool CheckIfIdle()
        {
            return !Tumbling && !InFreeFall && Physics.ObjectAtRest;
        }

        /// /// <summary>
        /// Damages this game object and applies a backwards force based on the angle
        /// </summary>
        /// <param name="attacker">The name of the object that damaged this object. Used for debugging</param>
        /// <param name="damage">The amount of damage being applied to the object. 
        /// Ring barriers only break if the damage amount is greater than the total health</param>
        /// <param name="knockBackScale"></param>
        /// <param name="hitAngle"></param>
        /// <returns></returns>
        /// <param name="damageType">The type of damage thid object will take</param>
        public override float TakeDamage(string attacker, float damage, float knockBackScale = 0, float hitAngle = 0, DamageType damageType = DamageType.DEFAULT)
        {
            //Return if there is no rigidbody or movement script attached
            if (!_movementBehaviour  || IsInvincible)
                return 0;

            //Update current knockback scale
            _currentKnockBackScale = knockBackScale;

            //Adds damage to the total damage
            Health += damage;
            Health = Mathf.Clamp(Health, 0, BlackBoardBehaviour.Instance.MaxKnockBackHealth.Value);

            _onTakeDamage?.Invoke();
            _onTakeDamageTemp?.Invoke();
            _onTakeDamageTemp = null;

            float totalKnockback = (knockBackScale + (knockBackScale * (Health / BlackBoardBehaviour.Instance.MaxKnockBackHealth.Value * 100)));

            _currentKnockBackScale = totalKnockback;
            //Calculates force and applies it to the rigidbody
            Vector3 knockBackForce = Physics.CalculatGridForce(totalKnockback, hitAngle);

            if (knockBackForce.magnitude > 0)
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

        /// /// <summary>
        /// Damages this game object and applies a backwards force based on the angle
        /// </summary>
        /// <param name="attacker">The name of the object that damaged this object. Used for debugging</param>
        /// <param name="damage">The amount of damage being applied to the object. 
        /// Ring barriers only break if the damage amount is greater than the total health</param>
        /// <param name="knockBackScale">How many panels far will this attakc make the object travel</param>
        /// <param name="hitAngle">The angle to launch the object</param>
        /// <param name="knockBackIsFixed">If true, the knock back won't be scaled based on health or mass</param>
        /// <param name="ignoreMass">If true, the force applied to the object won't change based in mass</param>
        /// <param name="damageType">The type of damage this object will take</param>
        public float TakeDamage(string attacker, float damage, float knockBackScale, float hitAngle, bool knockBackIsFixed, bool ignoreMass, DamageType damageType = DamageType.DEFAULT)
        {
            //Return if there is no rigidbody or movement script attached
            if (!_movementBehaviour || IsInvincible)
                return 0;

            //Adds damage to the total damage
            Health += damage;
            Health = Mathf.Clamp(Health, 0, BlackBoardBehaviour.Instance.MaxKnockBackHealth.Value);

            //Invoke damage events
            _onTakeDamage?.Invoke();
            _onTakeDamageTemp?.Invoke();
            _onTakeDamageTemp = null;

            //Apply the damage and weight to find the amount of knock back to be applied
            float totalKnockback = 0;
            if (!knockBackIsFixed)
                totalKnockback = (knockBackScale + (knockBackScale * (Health / BlackBoardBehaviour.Instance.MaxKnockBackHealth.Value * 100)));
            else
                totalKnockback = knockBackScale;

            //Update current knockback scale
            _currentKnockBackScale = totalKnockback;

            //Calculates force and applies it to the rigidbody
            Vector3 knockBackForce = Physics.CalculatGridForce(totalKnockback, hitAngle);

            if (knockBackForce.magnitude <= 0)
                return damage;

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

            return damage;
        }

        private void UpdateGroundedColliderPosition()
        {
            if (Tumbling)
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

            if (Physics.Acceleration.magnitude <= 0 && Physics.Rigidbody.isKinematic)
                _inFreeFall = false;

            if (Physics.RigidbodyInactive() || Physics.Rigidbody.isKinematic || InFreeFall)
                _tumbling = false;

            UpdateGroundedColliderPosition();

            if (Physics.IsGrounded)
            {
                if ((Physics.LastVelocity.magnitude <= 0.5f && Physics.Acceleration.magnitude <= 0.5f) && (Tumbling || InFreeFall) && !InHitStun)
                    TryStartLandingLag();
            }
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
        private float _knockbackScale;
        private float _hitAngle;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            _owner = (KnockbackBehaviour)target;

            _damage = EditorGUILayout.FloatField("Damage", _damage);
            _knockbackScale = EditorGUILayout.FloatField("Knockback Scale", _knockbackScale);
            _hitAngle = EditorGUILayout.FloatField("Hit Angle", _hitAngle);

            if (GUILayout.Button("Test Attack"))
            {
                _owner.TakeDamage(name, _damage, _knockbackScale, _hitAngle, DamageType.KNOCKBACK);
            }
        }
    }

#endif
}


