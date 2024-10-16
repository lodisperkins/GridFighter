﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Lodis.Gameplay;
using UnityEngine.Events;
using Lodis.ScriptableObjects;
using Lodis.Utility;
using Lodis.GridScripts;

namespace Lodis.Movement
{
    public enum AirState
    {
        NONE,
        TUMBLING,
        FREEFALL,
        BREAKINGFALL
    }
    
    [RequireComponent(typeof(GridPhysicsBehaviour))]
    public class KnockbackBehaviour : HealthBehaviour
    {
        [SerializeField]
        private bool _hasExploded;
        [SerializeField]
        private bool _outOfBounds;
        [SerializeField] private AirState _currentAirState;
        [SerializeField] private float _netForceLandingTolerance = 0.5f;

        [Tooltip("How fast will objects be allowed to travel in knockback")] [SerializeField]
        private FloatVariable _maxMagnitude;

        private GridMovementBehaviour _movementBehaviour;
        private HitStopBehaviour _hitstop;
        private GridPhysicsBehaviour _gridPhysicsBehaviour;
        private LandingBehaviour _landingBehaviour;

        private Vector2 _newPanelPosition = new Vector2(float.NaN, float.NaN);
        private float _lastBaseKnockBack;
        private Vector3 _launchForce;
        [Tooltip("The rate at which an objects move speed in air will decrease")]
        private FloatVariable _velocityDecayRate;
        [SerializeField] private FloatVariable _gravityIncreaseRate;
        [SerializeField] private FloatVariable _gravityIncreaseValue;
        private readonly TimedAction _gravityIncreaseTimer = new TimedAction();
        private float _startGravity;

        private UnityAction _onKnockBack;
        private UnityAction _onKnockBackStart;
        private UnityAction _onKnockBackTemp;
        private UnityAction _onKnockBackStartTemp;
        private UnityAction _onTakeDamageStart;
        private UnityAction _onTakeDamageStartTemp;
        private UnityAction _onTakeDamageTemp;
        private UnityAction _onHitStun;
        private UnityAction _onHitStunTemp;

        [SerializeField] private Vector3 _freeFallGroundedPoint;
        [SerializeField] private Vector3 _freeFallGroundedPointExtents;
        [Tooltip("The position that will be used to check if this character is grounded")] [SerializeField]
        private Vector3 _idleGroundedPoint;
        [SerializeField] private Vector3 _idleGroundedPointExtents;
        
        [SerializeField] private FloatVariable _minimumLaunchMagnitude;
        [SerializeField] private bool _inHitStun;
        private bool _isFlinching;
        private float _timeInCurrentHitStun;
        private TimedAction _hitStunTimer = new TimedAction();
        
        private float _lastTotalKnockBack;
        private float _lastTimeInKnockBack;
        private float _adjustedGravity;

        public float LastTimeInKnockBack
        {
            get { return _lastTimeInKnockBack; }
            set { _lastTimeInKnockBack = value; }
        }

        /// <summary>
        /// Returns the velocity of this object when it was first launched
        /// </summary>
        public Vector3 LaunchVelocity { get => _launchForce; }

        /// <summary>
        /// The scale of the last base knock back value applied to the object
        /// </summary>
        public float LastBaseKnockBack { get => _lastBaseKnockBack; }
        
        /// <summary>
        /// The scale of the last knock back value applied to the object
        /// </summary>
        public float LastTotalKnockBack { get => _lastTotalKnockBack; }
        public GridPhysicsBehaviour Physics { get => _gridPhysicsBehaviour; set => _gridPhysicsBehaviour = value; }
        public bool InHitStun { get => _inHitStun;}
        public bool IsFlinching { get => _isFlinching; }
        public float TimeInCurrentHitStun { get => _timeInCurrentHitStun; }

        public AirState CurrentAirState { get => _currentAirState; set => _currentAirState = value; }

        public GridMovementBehaviour MovementBehaviour => _movementBehaviour;

        public float StartGravity => _startGravity;

        public FloatVariable GravityIncreaseRate => _gravityIncreaseRate;

        public FloatVariable GravityIncreaseValue => _gravityIncreaseValue;

        public TimedAction GravityIncreaseTimer => _gravityIncreaseTimer;

        public LandingBehaviour LandingScript => _landingBehaviour;

        public float NetForceLandingTolerance => _netForceLandingTolerance;

        public FloatVariable MinimumLaunchMagnitude => _minimumLaunchMagnitude;

        public bool HasExploded 
        { 
            get => _hasExploded;
            set
            {
                _hasExploded = value;

                UpdateIsAlive();
            }
        }

        public bool OutOfBounds { get => _outOfBounds; set => _outOfBounds = value; }

        protected override void Awake()
        {
            base.Awake();

            _velocityDecayRate = Resources.Load<FloatVariable>("ScriptableObjects/VelocityDecayRate");

            _landingBehaviour = GetComponent<LandingBehaviour>();
            _movementBehaviour = GetComponent<GridMovementBehaviour>();
            _hitstop = GetComponent<HitStopBehaviour>();
            Physics = GetComponent<GridPhysicsBehaviour>();


            AddOnTakeDamageAction(IncreaseKnockbackGravity);
            //AddOnTakeDamageAction(_movementBehaviour.SnapToTarget);
            
            MatchManagerBehaviour.Instance.AddOnMatchRestartAction(() => Physics.Gravity = _startGravity);
        }

        // Start is called before the first frame update
        protected override void Start()
        {
            base.Start();
            AliveCondition = args => !HasExploded;
            _onKnockBackStart += () => 
            {
                Stunned = false;

                if (!_movementBehaviour)
                    return;

                _movementBehaviour.CurrentPanel.Occupied = false;

                //Disables object movement on the grid
                _movementBehaviour.DisableMovement(condition => CheckIfIdle(), true, true);
            };

            _onTakeDamage += () =>
            {
                if (!_movementBehaviour || !_movementBehaviour.IsMoving)
                    return;

                _movementBehaviour.CanCancelMovement = true;
                PanelBehaviour panel;
                BlackBoardBehaviour.Instance.Grid.GetPanelAtLocationInWorld(transform.position, out panel);
                _movementBehaviour.MoveToPanel(panel);
                _movementBehaviour.CanCancelMovement = false;
            };

            _startGravity = Physics.Gravity;
            _adjustedGravity = _startGravity;
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
        /// Removes the listener from the on knock back start  event
        /// </summary>
        /// <param name="action"></param>
        public void RemoveOnKnockBackStartAction(UnityAction action)
        {
            _onKnockBackStart -= action;
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
            if (CurrentAirState != AirState.TUMBLING) return;
            Physics.Gravity += _gravityIncreaseValue.Value;
            _adjustedGravity = Physics.Gravity;
        }

        public void IgnoreAdjustedGravity(Condition resetCondition)
        {
            Physics.Gravity = _startGravity;

            RoutineBehaviour.Instance.StartNewConditionAction(args => Physics.Gravity = _adjustedGravity, resetCondition);
        }

        protected override IEnumerator ActivateStun(float time)
        {
            MovesetBehaviour moveset = GetComponent<MovesetBehaviour>();
            Input.InputBehaviour inputBehaviour = GetComponent<Input.InputBehaviour>();
            GridMovementBehaviour movement = GetComponent<GridMovementBehaviour>();

            Stunned = true;
            if (CurrentAirState == AirState.FREEFALL || CurrentAirState == AirState.TUMBLING)
               Physics.FreezeInPlaceByCondition(condition =>!Stunned && !_hitstop.HitStopActive, false, true, false, true);

            
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
            if (movement && CurrentAirState == AirState.NONE)
                movement.DisableMovement(condition => Stunned == false, false, true);

            AirState previousState = CurrentAirState;

            CurrentAirState = AirState.NONE;
            
            _onKnockBackTemp += CancelStun;

            yield return new WaitForSeconds(time);

            if (moveset)
                moveset.enabled = true;
            if (inputBehaviour)
                inputBehaviour.enabled = true;

            CurrentAirState = previousState;

            _onKnockBackTemp -= CancelStun;
            Stunned = false;
        }

        public override void CancelStun()
        {
            base.CancelStun();
        }

        public override void ResetHealth()
        {
            base.ResetHealth();

            CancelStun();
            OutOfBounds = false;
            HasExploded = false;
            CancelHitStun();
            CurrentAirState = AirState.NONE;
            Physics.CancelFreeze();
            Physics.RB.isKinematic = true; 
        }

        public void CancelHitStun()
        {
            if (_hitStunTimer.GetEnabled())
                RoutineBehaviour.Instance.StopAction(_hitStunTimer);

            _timeInCurrentHitStun = 0;
            _inHitStun = false;
            _isFlinching = false;
        }

        public override void OnCollisionEnter(Collision collision)
        {
            HealthBehaviour damageScript = collision.gameObject.GetComponent<HealthBehaviour>();

            if (damageScript == null)
                return;

            if (CurrentAirState != AirState.TUMBLING && CurrentAirState != AirState.BREAKINGFALL)
                return;

            KnockbackBehaviour knockBackScript = damageScript as KnockbackBehaviour;

            //If no knockback script is attached, use this script to deal damage
            if (!knockBackScript)
                knockBackScript = this;

            float velocityMagnitude = knockBackScript.Physics.LastVelocity.magnitude;

            //Apply ricochet force and damage
            damageScript.TakeDamage(gameObject, velocityMagnitude, 0, 0, DamageType.KNOCKBACK);
        }

        private void ActivateHitStunByTimer(float timeInHitStun)
        {
            if (timeInHitStun <= 0)
            {
                _isFlinching = false;
                return;
            }

            _inHitStun = true;
            _timeInCurrentHitStun = timeInHitStun;

            if (_hitStunTimer.GetEnabled())
                RoutineBehaviour.Instance.StopAction(_hitStunTimer);

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
            return CurrentAirState == AirState.NONE && !Physics.IsFrozen && Physics.ObjectAtRest && !_landingBehaviour.Landing && !InHitStun &&!IsFlinching && !_landingBehaviour.IsDown && !Stunned && !_landingBehaviour.RecoveringFromFall;
        }
        public override float TakeDamage(GameObject attacker, float damage, float baseKnockBack = 0, float hitAngle = 0, DamageType damageType = DamageType.DEFAULT, float hitStun = 0)
        {
            _onTakeDamageStart?.Invoke();
            _onTakeDamageStartTemp?.Invoke();

            //Return if there is no rigidbody or movement script attached
            if (IsInvincible)
                return 0;

            //Update current knockback scale
            _lastBaseKnockBack = baseKnockBack;

            //Adds damage to the total damage
            Health += damage;

            ActivateHitStunByTimer(hitStun);

            _onTakeDamage?.Invoke();
            _onTakeDamageTemp?.Invoke();
            _onTakeDamageTemp = null;
            float totalKnockback = _lastBaseKnockBack;

            _lastTotalKnockBack = totalKnockback;

            //Calculates force and applies it to the rigidbody
            Vector3 knockBackForce = GridPhysicsBehaviour.CalculatGridForce(totalKnockback, hitAngle, _startGravity, Physics.Mass);

            if (hitStun > 0)
                _isFlinching = true;

            if ((knockBackForce / Physics.Mass).magnitude > MinimumLaunchMagnitude.Value)
            {
                _onKnockBackStart?.Invoke();
                _onKnockBackStartTemp?.Invoke();
                _onKnockBackStartTemp = null;

                _launchForce = knockBackForce;
                Physics.RB.isKinematic = false;

                //Add force to objectd
                Physics.ApplyImpulseForce(_launchForce);

                if (_launchForce.magnitude > 0)
                {
                    CurrentAirState = AirState.TUMBLING;
                    _onKnockBack?.Invoke();
                    _onKnockBackTemp?.Invoke();
                    _onKnockBackTemp = null;
                }
            }

            return damage;
        }
        public override float TakeDamage(HitColliderData info, GameObject attacker)
        {
            _onTakeDamageStart?.Invoke();
            _onTakeDamageStartTemp?.Invoke();

            //Return if there is no rigidbody or movement script attached
            if (IsInvincible)
                return 0;

            //Adds damage to the total damage
            Health += info.Damage;


            _onTakeDamage?.Invoke();
            OnTakeDamageEvent.Raise(gameObject);
            _onTakeDamageTemp?.Invoke();
            _onTakeDamageTemp = null;

            float totalKnockback = GetTotalKnockback(info.BaseKnockBack, info.KnockBackScale, Health);

            _lastTotalKnockBack = totalKnockback;
            //Calculates force and applies it to the rigidbody
            Vector3 knockBackForce = Physics.CalculatGridForce(totalKnockback, info.HitAngle, _startGravity, Physics.Mass, info.ClampForceWithinRing);
            if (info.HitStunTime > 0)
                _isFlinching = true;

            ActivateHitStunByTimer(info.HitStunTime);

            if ((knockBackForce / Physics.Mass).magnitude > MinimumLaunchMagnitude.Value)
            {
                _onKnockBackStart?.Invoke();
                _onKnockBackStartTemp?.Invoke();
                _onKnockBackStartTemp = null;

                _launchForce = knockBackForce;
                Physics.RB.isKinematic = false;

                //Add force to objectd
                Physics.ApplyImpulseForce(_launchForce, false, info.IgnoreMomentum);

                if (_launchForce.magnitude > 0)
                {
                    CurrentAirState = AirState.TUMBLING;
                    _onKnockBack?.Invoke();
                    _onKnockBackTemp?.Invoke();
                    _onKnockBackTemp = null;
                }
            }
            else if (_landingBehaviour.Landing)
            {
                knockBackForce = Vector3.up * MinimumLaunchMagnitude.Value;

                _onKnockBackStart?.Invoke();
                _onKnockBackStartTemp?.Invoke();
                _onKnockBackStartTemp = null;

                _launchForce = knockBackForce;
                Physics.RB.isKinematic = false;

                //Disables object movement on the grid
                _movementBehaviour.DisableMovement(condition => CheckIfIdle(), false, true);

                //Add force to objectd
                Physics.ApplyImpulseForce(_launchForce, false, info.IgnoreMomentum);

                if (_launchForce.magnitude > 0)
                {
                    CurrentAirState = AirState.TUMBLING;
                    _onKnockBack?.Invoke();
                    _onKnockBackTemp?.Invoke();
                    _onKnockBackTemp = null;
                }
            }
            else
                _movementBehaviour.DisableMovement(condition => CheckIfIdle(), false, true);

            return info.Damage;
        }

        public static float GetTotalKnockback(float baseKnockback, float knockbackScale, float health)
        {
            return baseKnockback + Mathf.Round((health / 100 * knockbackScale));
        }

        public override float TakeDamage(string attacker, AbilityData abilityData, DamageType damageType = DamageType.DEFAULT)
        {
            _onTakeDamageStart?.Invoke();
            _onTakeDamageStartTemp?.Invoke();

            //Return if there is no rigidbody or movement script attached
            if (IsInvincible)
                return 0;

            //Get the hit collider data of the first collider attached to the ability data
            HitColliderData info = abilityData.GetColliderInfo(0);

            //Adds damage to the total damage
            Health += info.Damage;
            
            //Apply hit stun
            ActivateHitStunByTimer(info.HitStunTime);
            
            //Call damage events to let others know this object was hit
            _onTakeDamage?.Invoke();
            OnTakeDamageEvent.Raise(gameObject);
            _onTakeDamageTemp?.Invoke();
            _onTakeDamageTemp = null;

            //Calculate knockback value based on the current health and scale of the attack
            float totalKnockback = GetTotalKnockback(info.BaseKnockBack, info.KnockBackScale, Health);
            _lastTotalKnockBack = totalKnockback;

            //Calculates force and applies it to the rigidbody
            Vector3 knockBackForce = GridPhysicsBehaviour.CalculatGridForce(totalKnockback, info.HitAngle, _startGravity, Physics.Mass);
            if (info.HitStunTime > 0)
                _isFlinching = true;

            //Return if this attack doesn't generate enough force
            if (!((knockBackForce / Physics.Mass).magnitude > MinimumLaunchMagnitude.Value)) return info.Damage;
            
            //Call events to let others know this character has began knockback
            _onKnockBackStart?.Invoke();
            _onKnockBackStartTemp?.Invoke();
            _onKnockBackStartTemp = null;

            //Store the force used to launch the character
            _launchForce = knockBackForce;
            Physics.RB.isKinematic = false;

            //Add force to object
            Physics.ApplyImpulseForce(_launchForce);

            if (!(_launchForce.magnitude > 0)) return info.Damage;
            
            //Set the new air state and call the knockback events
            CurrentAirState = AirState.TUMBLING;
            _onKnockBack?.Invoke();
            _onKnockBackTemp?.Invoke();
            _onKnockBackTemp = null;

            return info.Damage;
        }

        /// /// <summary>
        /// Damages this game object and applies a backwards force based on the angle
        /// </summary>
        /// <param name="attacker">The name of the object that damaged this object. Used for debugging</param>
        /// <param name="damage">The amount of damage being applied to the object. 
        /// Ring barriers only break if the damage amount is greater than the total health</param>
        /// <param name="baseKnockBack">How many panels far will this attakc make the object travel</param>
        /// <param name="hitAngle">The angle to launch the object</param>
        /// <param name="knockBackScale">How much the knockback will scale with damage</param>
        /// <param name="damageType">The type of damage this object will take</param>
        /// <param name="hitStun">The amount of time the character will be in hit stun</param>
        public float TakeDamage(string attacker, float damage, float baseKnockBack, float hitAngle, float hitStun, float knockBackScale, DamageType damageType = DamageType.DEFAULT)
        {
            _onTakeDamageStart?.Invoke();
            _onTakeDamageStartTemp?.Invoke();

            //Return if there is no rigidbody or movement script attached
            if (!_movementBehaviour || IsInvincible)
                return 0;

            //Adds damage to the total damage
            Health += damage;
            
            //Apply hit stun
            ActivateHitStunByTimer(hitStun);
            
            //Call damage events to let others know this object was hit
            _onTakeDamage?.Invoke();
            OnTakeDamageEvent.Raise(gameObject);
            _onTakeDamageTemp?.Invoke();
            _onTakeDamageTemp = null;

            //Calculate knockback value based on the current health and scale of the attack
            float totalKnockback = GetTotalKnockback(baseKnockBack, knockBackScale, Health);
            _lastTotalKnockBack = totalKnockback;

            //Calculates force and applies it to the rigidbody
            Vector3 knockBackForce = GridPhysicsBehaviour.CalculatGridForce(totalKnockback, hitAngle, _startGravity, Physics.Mass);
            if (hitStun > 0)
                _isFlinching = true;

            //Return if this attack doesn't generate enough force
            if (!((knockBackForce / Physics.Mass).magnitude > MinimumLaunchMagnitude.Value)) return damage;
            
            //Call events to let others know this character has began knockback
            _onKnockBackStart?.Invoke();
            _onKnockBackStartTemp?.Invoke();
            _onKnockBackStartTemp = null;
            
            //Store the force used to launch the character
            _launchForce = knockBackForce;
            Physics.RB.isKinematic = false;
            
            //Snap the object to its target panel if it was moving
            if (_movementBehaviour.IsMoving)
            {
                _movementBehaviour.CanCancelMovement = true;
                _movementBehaviour.MoveToPanel(_movementBehaviour.TargetPanel, true);
                _movementBehaviour.CanCancelMovement = false;
            }

            //Disables object movement on the grid
            _movementBehaviour.DisableMovement(condition => CheckIfIdle(), false, true);

            //Add force to object
            Physics.ApplyImpulseForce(_launchForce);

            //If the launch force is too small don't update the air state
            if (!(_launchForce.magnitude > 0)) return damage;
            
            //Set the new air state and call the knockback events
            CurrentAirState = AirState.TUMBLING;
            _onKnockBack?.Invoke();
            _onKnockBackTemp?.Invoke();
            _onKnockBackTemp = null;

            //Return the damage taken for debugging 
            return damage;
        }

        private void UpdateGroundedColliderPosition()
        {
            switch (CurrentAirState)
            {
                case AirState.TUMBLING:
                {
                    var bounds = Physics.BounceCollider.bounds;
                    Physics.GroundedBoxPosition = bounds.center;
                    Physics.GroundedBoxExtents = bounds.extents * 2;
                    break;
                }
                case AirState.FREEFALL:
                    Physics.GroundedBoxPosition = _freeFallGroundedPoint + transform.position;
                    Physics.GroundedBoxExtents = _freeFallGroundedPointExtents;
                    break;
                default:
                    Physics.GroundedBoxPosition = _idleGroundedPoint + transform.position;
                    Physics.GroundedBoxExtents = _idleGroundedPointExtents;
                    break;
            }
        }

        private void OnDrawGizmos()
        {
            if (Application.isPlaying) return;
            
            Gizmos.color = Color.blue;
            var position = transform.position;
            Gizmos.DrawCube(_freeFallGroundedPoint + position, _freeFallGroundedPointExtents);
            Gizmos.color = Color.green;
            Gizmos.DrawCube(_idleGroundedPoint + position, _idleGroundedPointExtents);
        }

        private void FixedUpdate()
        {
            if (Physics.RB.velocity.magnitude > _maxMagnitude.Value)
                Physics.RB.velocity = Physics.RB.velocity.normalized * _maxMagnitude.Value;

            UpdateGroundedColliderPosition();
        }

        public override void Update()
        {
            base.Update();

            if (CurrentAirState == AirState.TUMBLING) _lastTimeInKnockBack += Time.deltaTime;

            LandingScript.enabled = !OutOfBounds;
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
                _owner.TakeDamage(null, _damage, _baseKnockBack, _hitAngle, DamageType.KNOCKBACK, _hitStun);
            }
        }
    }

#endif
}


