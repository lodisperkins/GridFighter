using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lodis.Utility;
using Lodis.Movement;
using Lodis.GridScripts;
using Lodis.ScriptableObjects;
using UnityEngine.Events;

namespace Lodis.Gameplay
{

    public delegate void FallBreakEvent(bool onFloor);
    /// <summary>
    /// Handles all defensive options for any character on the grid.
    /// </summary>
    public class CharacterDefenseBehaviour : MonoBehaviour
    {
        private Movement.KnockbackBehaviour _knockBack;
        private Movement.GridMovementBehaviour _movement;
        private MovesetBehaviour _moveset;
        private HealthBehaviour _health;
        [Tooltip("How long the object will be parrying for.")]
        [SerializeField]
        private float _parryLength;
        [Tooltip("The collision script for the parry object.")]
        [SerializeField]
        private ColliderBehaviour _shieldCollider;
        [SerializeField]
        private bool _isParrying;
        [Tooltip("How long the character will be invincible for after a successful ground parry.")]
        [SerializeField]
        private float _parryInvincibilityLength;
        private bool _isDefending;
        private bool _isPhaseShifting;
        [Tooltip("True if the parry cooldown timer is 0.")]
        [SerializeField]
        private bool _canParry = true;
        [Tooltip("How long the character is left immobile after dropping shield.")]
        [SerializeField]
        private FloatVariable _shieldRestTime;
        [Tooltip("How long in seconds is the object braced for it's fall.")]
        [SerializeField]
        private float _braceActiveTime;
        [Tooltip("How fast will the shield drain energy when activated.")]
        [SerializeField]
        private FloatVariable _shieldDrainValue;
        [SerializeField]
        private bool _canBrace;
        [SerializeField]
        private bool _isBraced;
        [Tooltip("How long in seconds the object has to wait before it can brace itself again.")]
        [SerializeField]
        private float _braceCooldownTime;
        [Tooltip("How long in seconds the object is invincible after catching it's fall.")]
        [SerializeField]
        private float _groundTechInvincibilityTime;
        [Tooltip("How long in seconds the object is invincible after breaking its fall on a wall.")]
        [SerializeField]
        private float _wallTechInvincibilityTime;
        [Tooltip("How long in seconds the object is going to spend breaking its fall.")]
        [SerializeField]
        private float _fallBreakLength;
        [SerializeField]
        private int _wallTechJumpDistance;
        [SerializeField]
        private float _wallTechJumpHeight;
        [SerializeField]
        private float _wallTechJumpDuration;
        [Tooltip("How long in seconds to stun an enemy after a successful parry.")]
        [SerializeField]
        private float _attackerStunTime;
        [SerializeField]
        private float _phaseShiftDuration;
        [SerializeField]
        private FloatVariable _defaultPhaseShiftRestTime;
        [SerializeField]
        private FloatVariable _successPhaseShiftRestTime;
        private FloatVariable _currentPhaseShiftRestTime;
        private TimedAction _cooldownTimedAction;
        public FallBreakEvent onFallBroken;
        private TimedAction _parryTimer;
        private DelayedAction _disableFallBreakAction;
        private TimedAction _shieldTimer;
        private bool _canPhaseShift;
        [SerializeField]
        private GridGame.Event _phaseShiftEvent;
        [SerializeField]
        private GridGame.Event _phaseShiftSuccessEvent;
        [SerializeField]
        private GridGame.Event _onParryEvent;
        private UnityAction _onPhaseShift;
        [SerializeField]
        [Tooltip("The speed of the game after a phase shift passes through an attack.")]
        private FloatVariable _slowMotionTimeScale;
        [SerializeField]
        [Tooltip("The amount of time to stay in slow motion after a phase shift passes through an attack..")]
        private FloatVariable _slowMotionTime;
        [SerializeField]
        [Tooltip("The amount of time it takes to transition into slow motion.")]
        private FloatVariable _slowMotionTransitionSpeed;
        [SerializeField]
        [Tooltip("The effect to play when the player defends perfectly.")]
        private ParticleSystem _perfectDefenseEffect;
        private bool _isShielding;
        private bool _isResting;

        public bool BreakingFall { get; private set; }
        public float BraceInvincibilityTime { get => _groundTechInvincibilityTime; }
        public bool CanParry { get => _canParry; }
        public bool IsParrying { get => _isParrying; }
        public bool IsBraced { get => _isBraced; private set => _isBraced = value; }
        public float GroundTechLength { get => _fallBreakLength; set => _fallBreakLength = value; }
        public float WallTechJumpDuration { get => _wallTechJumpDuration; }
        public ColliderBehaviour ParryCollider { get => _shieldCollider; }
        public bool IsDefending { get => _isDefending; }
        public bool CanPhaseShift { get => _canPhaseShift; set => _canPhaseShift = value; }
        public bool IsPhaseShifting { get => _isPhaseShifting; }
        public FloatVariable CurrentPhaseShiftRestTime { get => _currentPhaseShiftRestTime; }
        public FloatVariable DefaultPhaseShiftRestTime { get => _defaultPhaseShiftRestTime; set => _defaultPhaseShiftRestTime = value; }
        public bool IsShielding { get => _isShielding; }
        public bool IsResting { get => _isResting; }

        // Start is called before the first frame update
        void Start()
        {
            //Initialize components
            _knockBack = GetComponent<Movement.KnockbackBehaviour>();
            _movement = GetComponent<Movement.GridMovementBehaviour>();
            _health = GetComponent<HealthBehaviour>();
            _moveset = GetComponent<MovesetBehaviour>();

            _knockBack.AddOnTakeDamageAction(StopShield);
            _knockBack.AddOnKnockBackAction(EnableBrace);
            _movement.AddOnMoveBeginAction(StopShield);

            _shieldCollider.AddCollisionEvent(args => 
            {
                if (args.Length < 2)
                    return;

                HitColliderBehaviour other = (HitColliderBehaviour)args[1];

                if (!other)
                    return;

                if (BlackBoardBehaviour.Instance.BlockEffect)
                    Instantiate(BlackBoardBehaviour.Instance.BlockEffect, other.transform.position + (.5f * Vector3.up), transform.rotation);

                if (IsParrying)
                    ActivateParryInvinciblity(args);
            }
            );

            _onPhaseShift += () => _phaseShiftEvent?.Raise(_shieldCollider.Owner);
        }

        public void AddOnPhaseShiftAction(UnityAction action)
        {
            _onPhaseShift += action;
        }

        public void ActivatePhaseShift(Vector2 moveDirection)
        {
            if (_isResting || !_movement.CanMove || _isPhaseShifting)
                return;

            if (moveDirection.magnitude > 1)
                moveDirection = new Vector2(moveDirection.x, 0);

            _isParrying = false;
            _isShielding = false;
            //Allow the character to parry again
            _canParry = true;
            _shieldCollider.gameObject.SetActive(false);

            _isPhaseShifting = true;

            PanelBehaviour panel;


            _movement.CancelMovement();
            if (!_movement.MoveToPanel(_movement.Position + (moveDirection * 2), false, _movement.Alignment, false, true, true))
            {
                _isPhaseShifting = false;
                RoutineBehaviour.Instance.StopAction(_shieldTimer);
                _isResting = false;
                return;
            }

            _currentPhaseShiftRestTime = _defaultPhaseShiftRestTime;
            _health.SetIntagibilityByTimer(_phaseShiftDuration);

            _onPhaseShift?.Invoke();
            _movement.AddOnMoveEndTempAction(() => { _isPhaseShifting = false; StartDefenseLag(_currentPhaseShiftRestTime, _isPhaseShifting); });
        }

        private void StartDefenseLag(FloatVariable time, bool value)
        {
            if (_isResting)
                return;

            _isResting = true;

            _movement.DisableMovement(condition => _isResting == false, true, true);
            //Start timer for player immobility
            RoutineBehaviour.Instance.StopAction(_shieldTimer);
            _shieldTimer = RoutineBehaviour.Instance.StartNewTimedAction(args => 
            { _isResting = false;  value = false; }, TimedActionCountType.SCALEDTIME, time.Value);
        }

        /// <summary>
        /// Enables the parry collider and freezes character actions
        /// </summary>
        /// <returns></returns>
        private void ActivateReflector()
        {
            _shieldCollider.tag = "Reflector";
            //Enable parry and update states
            _shieldCollider.gameObject.SetActive(true);
            _canParry = false;

            RoutineBehaviour.Instance.StartNewTimedAction(args => DeactivateReflector(), TimedActionCountType.SCALEDTIME, _parryLength);
        }

        private void DeactivateReflector()
        {
            _shieldCollider.tag = "Untagged";
            _isParrying = false;
            //Allow the character to parry again
            _canParry = true;
        }

        public void DeactivateShield()
        {
            if (!_isShielding && !_isParrying)
                return;

            DeactivateReflector();

            //Allow the character to parry again
            _canParry = true;
            _shieldCollider.gameObject.SetActive(false);
            _moveset.EnergyChargeEnabled = true;
            StartDefenseLag(_shieldRestTime, _isShielding);
        }

        /// <summary>
        /// Starts a parry cororoutine. The effects of the parry changes
        /// based on whether or not the character is in hit stun.
        /// </summary>
        public void BeginParry()
        {
            if (!_canParry || BlackBoardBehaviour.Instance.GetPlayerState(_shieldCollider.Owner) != "Idle")
                return;

            _isParrying = true;
            _movement.CancelMovement();
            ActivateReflector();
        }

        /// <summary>
        /// Disables the parry collider and invincibilty.
        /// Gives the object the ability to parry again.
        /// </summary>
        public void StopShield()
        {
            if (_knockBack.CheckIfIdle())
                DeactivateReflector();

            RoutineBehaviour.Instance.StopAction(_parryTimer);
            RoutineBehaviour.Instance.StopAction(_shieldTimer);
            _shieldCollider.gameObject.SetActive(false);
            _isShielding = false;
            _isResting = false;
            _isParrying = false;
            _canParry = true;
        }

        /// <summary>
        /// Enables the ability to brace and stops the cooldown timer.
        /// </summary>
        private void EnableBrace()
        {
            IsBraced = false;
            _canBrace = true;

            RoutineBehaviour.Instance.StopAction(_cooldownTimedAction);
        }

        /// <summary>
        /// Braces the object so it may catch it's fall.
        /// </summary>
        public void Brace()
        {
            if (_knockBack.CurrentAirState != AirState.TUMBLING || !_canBrace)
                return;

            IsBraced = true;
            _canBrace = false;
            RoutineBehaviour.Instance.StartNewTimedAction(args => DeactivateBrace(), TimedActionCountType.SCALEDTIME, _braceActiveTime);
        }

        /// <summary>
        /// Disables the brace and starts the cooldown
        /// </summary>
        private void DeactivateBrace()
        {
            IsBraced = false;
            _cooldownTimedAction = RoutineBehaviour.Instance.StartNewTimedAction(args => _canBrace = true, TimedActionCountType.SCALEDTIME, _braceCooldownTime);
        }

        /// <summary>
        /// Makes the character invincible after a collision occurs
        /// </summary>
        /// <param name="args">The collision arguments</param>
        private void ActivateParryInvinciblity(params object[] args)
        {
            GameObject other = (GameObject)args[0];
            //Get collider and rigidbody to check the owner and add the force
            ColliderBehaviour otherHitCollider = null;
            //Return if the object collided with doesn't have a collider script attached
            if (args.Length > 0)
            {
                otherHitCollider = other.GetComponentInParent<ColliderBehaviour>();

                if (!otherHitCollider)
                    return;
            }

            _onParryEvent?.Raise(gameObject);
            GameManagerBehaviour.Instance.ChangeTimeScale(0, 0, 0.05f);
            Instantiate(_perfectDefenseEffect, transform.position + Vector3.up * 0.5f, Camera.main.transform.rotation);

            //Deactivate the parry
            _shieldCollider.gameObject.SetActive(false);
            _isParrying = false;
            _isDefending = false;
            _canParry = true;
            //Make the character invincible for a short amount of time if they're on the ground
            _knockBack.SetInvincibilityByTimer(_parryInvincibilityLength);
        }

        private void BreakFall(GameObject other)
        {
            if (other == null) return;

            BreakingFall = true;

            //Breaks fall on the ground

            PanelBehaviour panel = other.GetComponent<PanelBehaviour>();

            _knockBack.SetInvincibilityByTimer(_wallTechInvincibilityTime);

            if (_disableFallBreakAction?.GetEnabled() == true)
                RoutineBehaviour.Instance.StopAction(_disableFallBreakAction);


            //Breaks fall on structures

            //Makes the character jump if they broke their fall on structure
            if (other.gameObject.CompareTag("Structure"))
            {
                _disableFallBreakAction = RoutineBehaviour.Instance.StartNewConditionAction(DisableFallBreaking, condition => !_knockBack.Physics.IsFrozen);
                _knockBack.CurrentAirState = AirState.BREAKINGFALL;
                _knockBack.Physics.FreezeInPlaceByTimer(WallTechJumpDuration, false, true);
                onFallBroken?.Invoke(false);
                return;
            }
        }

        private void OnDrawGizmos()
        {
            if (!_knockBack || _knockBack.CurrentAirState != AirState.TUMBLING)
                return;
            
            Collider bounceCollider = _knockBack.Physics.BounceCollider;
            Gizmos.DrawCube(bounceCollider.gameObject.transform.position, bounceCollider.bounds.extents * 1.5f);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_isPhaseShifting)
            {
                HitColliderBehaviour collider = other.attachedRigidbody?.GetComponent<HitColliderBehaviour>();

                if (!collider) return;
                if (collider.Owner == gameObject) return;

                _phaseShiftSuccessEvent?.Raise(gameObject);
                _currentPhaseShiftRestTime = _successPhaseShiftRestTime;
                GameManagerBehaviour.Instance.ChangeTimeScale(_slowMotionTimeScale.Value, _slowMotionTransitionSpeed.Value, _slowMotionTime.Value);
            }

            if (!IsBraced || !other.CompareTag("Structure") || other.CompareTag("CollisionPlane") || BreakingFall)
                return;

            BreakFall(other.gameObject);
        }

        private void DisableFallBreaking(params object[]  args)
        {
            BreakingFall = false;
            _knockBack.Physics.IgnoreForces = false;
            _disableFallBreakAction = null;
            if (!_knockBack.Physics.IsGrounded)
                _knockBack.CurrentAirState = AirState.FREEFALL;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!IsBraced || !collision.gameObject.CompareTag("Structure") || collision.gameObject.CompareTag("CollisionPlane") || BreakingFall)
                return;


            BreakingFall = true;

            PanelBehaviour panel = collision.gameObject.GetComponent<PanelBehaviour>();

            _knockBack.SetInvincibilityByTimer(_wallTechInvincibilityTime);
            Instantiate(_perfectDefenseEffect, transform.position, Camera.main.transform.rotation);

            if (_disableFallBreakAction?.GetEnabled() == true)
                RoutineBehaviour.Instance.StopAction(_disableFallBreakAction);

            _disableFallBreakAction = RoutineBehaviour.Instance.StartNewConditionAction(DisableFallBreaking, condition => !_knockBack.Physics.IsFrozen);

            if (collision.gameObject.CompareTag("Structure"))
            {

                _disableFallBreakAction = RoutineBehaviour.Instance.StartNewConditionAction(DisableFallBreaking, condition => !_knockBack.Physics.IsFrozen);
                _knockBack.CurrentAirState = AirState.BREAKINGFALL;
                _knockBack.Physics.FreezeInPlaceByTimer(WallTechJumpDuration, false, true);
                onFallBroken?.Invoke(false);
                return;
            }

        }

        private void Update()
        {
            _isShielding = !_isParrying && _shieldCollider.gameObject.activeSelf;

            _isDefending = _isShielding || _isPhaseShifting || _isParrying;
            if (!_isShielding)
            {
                _moveset.EnergyChargeEnabled = true;
                return;
            }

            _moveset.EnergyChargeEnabled = false;

            if (!_moveset.TryUseEnergy(_shieldDrainValue.Value * Time.deltaTime))
                DeactivateShield();
        }
    }
}
