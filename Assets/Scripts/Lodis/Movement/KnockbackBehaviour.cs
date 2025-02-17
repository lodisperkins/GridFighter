using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Lodis.Gameplay;
using UnityEngine.Events;
using Lodis.ScriptableObjects;
using Lodis.Utility;
using Lodis.GridScripts;
using Types;
using FixedPoints;
using System.IO;
using System.Net.Http.Headers;

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
        [Header("State Descriptors")]
        [Tooltip("Whether or not the character has died from being out of bounds.")]
        [SerializeField] private bool _hasExploded;
        [Tooltip("Whether or not the character is outside their ring barrier.")]
        [SerializeField] private bool _outOfBounds;
        [SerializeField] private bool _inHitStun;
        [Tooltip("The current description of how the character is behaving in the air.")]
        [SerializeField] private AirState _currentAirState;

        [Header("Knockback Physics Parameters")]
        [Tooltip("How fast will objects be allowed to travel in knockback.")]
        [SerializeField] private FloatVariable _maxMagnitude;

        [Tooltip("How long it takes gravity to increase while being comboed.")]
        [SerializeField] private FloatVariable _gravityIncreaseRate;
        [Tooltip("How much gravity will increase while being comboed.")]
        [SerializeField] private FloatVariable _gravityIncreaseValue;

        [Tooltip("Knockback forces must be above this value to actually move the character.")]
        [SerializeField] private FloatVariable _minimumLaunchMagnitude;

        //---
        private Fixed32 _startGravity;

        private UnityAction _onKnockBack;
        private UnityAction _onKnockBackStart;
        private UnityAction _onKnockBackTemp;
        private UnityAction _onKnockBackStartTemp;
        private UnityAction _onTakeDamageStart;
        private UnityAction _onTakeDamageStartTemp;
        private UnityAction _onTakeDamageTemp;
        private UnityAction _onHitStun;
        private UnityAction _onHitStunTemp;

        private GridMovementBehaviour _movementBehaviour;
        private HitStopBehaviour _hitstop;
        private GridPhysicsBehaviour _gridPhysicsBehaviour;
        private LandingBehaviour _landingBehaviour;

        private float _lastBaseKnockBack;
        private FVector3 _launchForce;
        private bool _isFlinching;
        private Fixed32 _timeInCurrentHitStun;
        private FixedTimeAction _hitStunTimer;
        
        private float _lastTotalKnockBack;
        private Fixed32 _adjustedGravity;
        private bool _isSlidingHit;

        /// <summary>
        /// Returns the velocity of this object when it was first launched
        /// </summary>
        public FVector3 LaunchVelocity { get => _launchForce; }

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
        public Fixed32 TimeInCurrentHitStun { get => _timeInCurrentHitStun; }

        public AirState CurrentAirState 
        {
            get => _currentAirState;
            set => _currentAirState = value;
        }

        public GridMovementBehaviour MovementBehaviour => _movementBehaviour;

        public Fixed32 StartGravity => _startGravity;

        public FloatVariable GravityIncreaseRate => _gravityIncreaseRate;

        public FloatVariable GravityIncreaseValue => _gravityIncreaseValue;

        public LandingBehaviour LandingScript => _landingBehaviour;

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
        public bool IsSlidingHit { get => _isSlidingHit; private set => _isSlidingHit = value; }

        protected override void Awake()
        {
            base.Awake();

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

                if (_movementBehaviour.CurrentPanel != null)
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

        public override void Serialize(BinaryWriter bw)
        {
            base.Serialize(bw);
            bw.Write(_hasExploded);
            bw.Write(_outOfBounds);
            bw.Write((int)_currentAirState);
            _launchForce.Serialize(bw);
            bw.Write(_inHitStun);
            bw.Write(_isFlinching);
            _timeInCurrentHitStun.Serialize(bw);
            bw.Write(_isSlidingHit);
        }

        public override void Deserialize(BinaryReader br)
        {
            base.Deserialize(br);
            _hasExploded = br.ReadBoolean();
            _outOfBounds = br.ReadBoolean();
            _currentAirState = (AirState)br.ReadInt32();
            _launchForce.Deserialize(br);
            _inHitStun = br.ReadBoolean();
            _isFlinching = br.ReadBoolean();
            _timeInCurrentHitStun.Deserialize(br);
            _isSlidingHit = br.ReadBoolean();
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
            Physics.Gravity += _gravityIncreaseValue.FixedValue;
            _adjustedGravity = Physics.Gravity;
        }

        public void IgnoreAdjustedGravity(Condition resetCondition)
        {
            Physics.Gravity = _startGravity;

            FixedPointTimer.StartNewConditionAction(() => Physics.Gravity = _adjustedGravity, resetCondition);
        }

        public override void Stun(Fixed32 time)
        {
            if (Stunned || IsInvincible || IsIntangible)
                return;

            base.Stun(time);

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
            Physics.StopVelocity();
            //Physics.RB.isKinematic = true;
        }

        public void CancelHitStun()
        {
            if (_hitStunTimer?.IsActive == true)
                _hitStunTimer.Stop();

            _timeInCurrentHitStun = 0;
            _inHitStun = false;
            _isFlinching = false;
        }

        public override void OnHitEnter(Collision collision)
        {
            HealthBehaviour damageScript = collision.OtherEntity.UnityObject.GetComponent<HealthBehaviour>();

            if (damageScript == null)
                return;

            if (CurrentAirState != AirState.TUMBLING && CurrentAirState != AirState.BREAKINGFALL)
                return;

            KnockbackBehaviour knockBackScript = damageScript as KnockbackBehaviour;

            //If no knockback script is attached, use this script to deal damage
            if (!knockBackScript)
                knockBackScript = this;

            float velocityMagnitude = knockBackScript.Physics.Velocity.Magnitude;

            //Apply ricochet force and damage
            damageScript.TakeDamage(Entity, velocityMagnitude, 0, 0, DamageType.KNOCKBACK);
        }

        private void ActivateHitStunByTimer(Fixed32 timeInHitStun)
        {
            if (timeInHitStun <= 0)
            {
                _isFlinching = false;
                return;
            }

            _inHitStun = true;
            _timeInCurrentHitStun = timeInHitStun;

            if (_hitStunTimer?.IsActive == true)
                _hitStunTimer.Stop();

            _hitStunTimer = FixedPointTimer.StartNewTimedAction(() => { _inHitStun = false; _isFlinching = false; _timeInCurrentHitStun = 0; }, timeInHitStun);
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
        public override Fixed32 TakeDamage(EntityData attacker, Fixed32 damage, Fixed32 baseKnockBack = default, Fixed32 hitAngle = default, DamageType damageType = DamageType.DEFAULT, Fixed32 hitStun = default)
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
            IsSlidingHit = false;

            float totalKnockback = _lastBaseKnockBack;

            _lastTotalKnockBack = totalKnockback;

            //Calculates force and applies it to the rigidbody
            FVector3 knockBackForce = GridPhysicsBehaviour.CalculatGridForce(totalKnockback, hitAngle, _startGravity, Physics.Mass);

            if (Physics.IsGrounded && knockBackForce.X > 0 && knockBackForce.Y < new Fixed32(655))
            {
                IsSlidingHit = true;
            }

            if (hitStun > 0)
                _isFlinching = true;

            if ((knockBackForce / Physics.Mass).Magnitude > MinimumLaunchMagnitude.FixedValue || Physics.BouncePending)
            {
                _onKnockBackStart?.Invoke();
                _onKnockBackStartTemp?.Invoke();
                _onKnockBackStartTemp = null;

                _launchForce = knockBackForce;
                //Physics.RB.isKinematic = false;

                //Add force to objectd
                Physics.ApplyImpulseForce(_launchForce);

                if (_launchForce.Magnitude > 0)
                {
                    CurrentAirState = AirState.TUMBLING;
                    _onKnockBack?.Invoke();
                    _onKnockBackTemp?.Invoke();
                    _onKnockBackTemp = null;
                }
            }

            return damage;
        }
        public override Fixed32 TakeDamage(HitColliderData info, EntityData attacker)
        {
            _onTakeDamageStart?.Invoke();
            _onTakeDamageStartTemp?.Invoke();

            //Return if there is no rigidbody or movement script attached
            if (IsInvincible)
                return 0;

            //Adds damage to the total damage
            Health += info.Damage;


            _onTakeDamage?.Invoke();
            _onTakeDamageTemp?.Invoke();
            _onTakeDamageTemp = null;
            IsSlidingHit = false;

            Fixed32 totalKnockback = GetTotalKnockback(info.BaseKnockBack, info.KnockBackScale, Health);

            _lastTotalKnockBack = totalKnockback;
            //Calculates force and applies it to the rigidbody
            FVector3 knockBackForce = Physics.CalculateGridForce(totalKnockback, info.HitAngle, _startGravity, Physics.Mass, info.ClampForceWithinRing);
            if (info.HitStunTime > 0)
                _isFlinching = true;


            if (Physics.IsGrounded && knockBackForce.X > 0 && knockBackForce.Y < new Fixed32(655))
            {
                IsSlidingHit = true;
            }

            ActivateHitStunByTimer(info.HitStunTime);

            if ((knockBackForce / Physics.Mass).Magnitude > MinimumLaunchMagnitude.FixedValue || Physics.BouncePending)
            {
                _onKnockBackStart?.Invoke();
                _onKnockBackStartTemp?.Invoke();
                _onKnockBackStartTemp = null;

                _launchForce = knockBackForce;
                //Physics.RB.isKinematic = false;

                //Add force to objectd
                Physics.ApplyImpulseForce(_launchForce, true, info.IgnoreMomentum);

                if (_launchForce.Magnitude > 0)
                {
                    CurrentAirState = AirState.TUMBLING;
                    _onKnockBack?.Invoke();
                    _onKnockBackTemp?.Invoke();
                    _onKnockBackTemp = null;
                }
            }
            else if (_landingBehaviour.Landing)
            {
                knockBackForce = FVector3.Up * MinimumLaunchMagnitude.FixedValue;

                _onKnockBackStart?.Invoke();
                _onKnockBackStartTemp?.Invoke();
                _onKnockBackStartTemp = null;

                _launchForce = knockBackForce;
                //Physics.RB.isKinematic = false;

                //Disables object movement on the grid
                _movementBehaviour.DisableMovement(condition => CheckIfIdle(), false, true);

                //Add force to objectd
                Physics.ApplyImpulseForce(_launchForce, false, info.IgnoreMomentum);

                if (_launchForce.Magnitude > 0)
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

        public static Fixed32 GetTotalKnockback(Fixed32 baseKnockback, Fixed32 knockbackScale, Fixed32 health)
        {
            return baseKnockback + baseKnockback * (health / 100 * knockbackScale);
        }

        public override void Tick(Fixed32 dt)
        {
            base.Tick(dt);

            LandingScript.enabled = !OutOfBounds;

            if (Physics.Velocity.Magnitude > _maxMagnitude.FixedValue && CurrentAirState == AirState.TUMBLING)
                Physics.ApplyVelocityChange(Physics.Velocity.GetNormalized() * _maxMagnitude.FixedValue);

            //UpdateGroundedColliderPosition();
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


