using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lodis.Utility;
using Lodis.Movement;
using Lodis.GridScripts;
using Lodis.ScriptableObjects;
using UnityEngine.Events;
using Lodis.Input;
using FixedPoints;

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
        [Tooltip("How far the character will jump after successfully catching themselves when against a wall.")]
        [SerializeField]
        private int _wallTechJumpDistance;
        [Tooltip("How high the character will jump after successfully catching themselves when against a wall.")]
        [SerializeField]
        private float _wallTechJumpHeight;
        [Tooltip("How long it will take the character to jump after successfully catching themselves when against a wall")]
        [SerializeField]
        private float _wallTechJumpDuration;
        [Tooltip("How long in seconds to stun an enemy after a successful parry.")]
        [SerializeField]
        private float _attackerStunTime;
        [Tooltip("How long the character will be phase-shifting for.")]
        [SerializeField]
        private float _phaseShiftDuration;
        [Tooltip("The amount of time the character will not be able to perform actions after phase-shifting without dodging an attack")]
        [SerializeField]
        private FloatVariable _defaultPhaseShiftRestTime;
        [Tooltip("The amount of time the character will not be able to perform actions after phase-shifting and dodging an attack")]
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
        private CustomEventSystem.Event _phaseShiftEvent;
        [SerializeField]
        private CustomEventSystem.Event _phaseShiftSuccessEvent;
        [SerializeField]
        private CustomEventSystem.Event _onParryEvent;
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
        [Tooltip("The script responsible for giving this character input/commands.")]
        private IControllable _controller;
        private bool _isShielding;
        private bool _isResting;

        /// <summary>
        /// Whether or not this character is in the process of catching themselves after a fall
        /// </summary>
        public bool BreakingFall { get; private set; }

        /// <summary>
        /// How long this character will be invincible after catching themselves after a fall
        /// </summary>
        public float BraceInvincibilityTime { get => _groundTechInvincibilityTime; }

        /// <summary>
        /// Whether or not this character can activate a parry while shielding
        /// </summary>
        public bool CanParry { get => _canParry; }

        /// <summary>
        /// Whether or not this character is has activated a parry while shielding
        /// </summary>
        public bool IsParrying { get => _isParrying; }

        /// <summary>
        /// Whether or not this character is ready to catch themselves after a fall
        /// </summary>
        public bool IsBraced { get => _isBraced; private set => _isBraced = value; }

        /// <summary>
        /// How long it takes this character to catch themselve on the ground after a fall
        /// </summary>
        public float GroundTechLength { get => _fallBreakLength; set => _fallBreakLength = value; }

        /// <summary>
        /// How long it will take the character to jump after successfully catching themselves when against a wall
        /// </summary>
        public float WallTechJumpDuration { get => _wallTechJumpDuration; }

        /// <summary>
        /// The collider attached to this character that will be used to block and parry
        /// </summary>
        public ColliderBehaviour ParryCollider { get => _shieldCollider; }

        /// <summary>
        /// Whether or not this character is using any defense action
        /// </summary>
        public bool IsDefending { get => _isDefending; }

        /// <summary>
        /// Whether or no this character can phase shift through attacks
        /// </summary>
        public bool CanPhaseShift { get => _canPhaseShift; set => _canPhaseShift = value; }

        /// <summary>
        /// Whether or no this character is phase-shifting
        /// </summary>
        public bool IsPhaseShifting { get => _isPhaseShifting; }

        /// <summary>
        /// The current time the character has to wait after phase-shifting.
        /// Varies based on whether or not the charater successfully dodged an attack
        /// </summary>
        public FloatVariable CurrentPhaseShiftRestTime { get => _currentPhaseShiftRestTime; }

        /// <summary>
        /// The amount of time the character will not be able to perform actions after phase-shifting without dodging an attack
        /// </summary>
        public FloatVariable DefaultPhaseShiftRestTime { get => _defaultPhaseShiftRestTime; set => _defaultPhaseShiftRestTime = value; }

        /// <summary>
        /// Whether or not this character has their shield active
        /// </summary>
        public bool IsShielding { get => _isShielding; }

        /// <summary>
        /// Whether or not this character is unable to perform another action due to a previously used defense action
        /// </summary>
        public bool IsResting { get => _isResting; }

        // Start is called before the first frame update
        void Start()
        {
            //Initialize components
            _knockBack = GetComponent<Movement.KnockbackBehaviour>();
            _movement = GetComponent<Movement.GridMovementBehaviour>();
            _health = GetComponent<HealthBehaviour>();
            _moveset = GetComponent<MovesetBehaviour>();
            _controller = GetComponentInParent<IControllable>();

            _knockBack.AddOnTakeDamageAction(StopShield);
            _knockBack.AddOnKnockBackAction(EnableBrace);
            _movement.AddOnMoveBeginAction(StopShield);

            _shieldCollider.AddCollisionEvent(collision => 
            {
                HitColliderBehaviour other = collision.Entity.GetComponent<HitColliderBehaviour>();

                if (!other)
                    return;

                //Spawns particles after block for player feedback
                if (BlackBoardBehaviour.Instance.BlockEffect)
                    Instantiate(BlackBoardBehaviour.Instance.BlockEffect, other.transform.position + (.5f * Vector3.up), transform.rotation);

                //If the player was parrying still, make them invincible briefly
                if (IsParrying)
                    ActivateParryInvinciblity(collision);
            }
            );

            _onPhaseShift += () => _phaseShiftEvent?.Raise(_shieldCollider.Spawner.UnityObject);
        }

        /// <summary>
        /// Adds another action to perform when a phase-shift starts
        /// </summary>
        public void AddOnPhaseShiftAction(UnityAction action)
        {
            _onPhaseShift += action;
        }

        /// <summary>
        /// Activates phase shift. Character moves while intangible.
        /// </summary>
        /// <param name="moveDirection">THe direction for the character move towards while phase-shifting</param>
        public void ActivatePhaseShift(FVector2 moveDirection)
        {
            if (_isResting || !_movement.CanMove || _isPhaseShifting)
                return;

            //Only take the x value if a diagnol direction was given
            if (moveDirection.Magnitude > 1)
                moveDirection = new FVector2(moveDirection.X, 0);

            //Disable shield to prevent the shield from colliding with other attacks
            _isParrying = false;
            _isShielding = false;

            //Allow the character to parry again
            _canParry = true;
            _shieldCollider.gameObject.SetActive(false);

            _isPhaseShifting = true;

            PanelBehaviour panel;

            //Stops character movement and tries to move to a new panel
            _movement.CancelMovement();
            _movement.Position = _movement.CurrentPanel.Position;
            if (!_movement.MoveToPanel(_movement.Position + (moveDirection * 2), false, _movement.Alignment, false, true, true))
            {
                //Cancel the phase shift if the character couldn't move a valid panel
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

        /// <summary>
        /// Prevents the character from moving after using a defense action
        /// </summary>
        /// <param name="time">The amount of time the character will be resting for</param>
        /// <param name="value"></param>
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

        /// <summary>
        /// Removes the shields ability to reflect projectiles
        /// </summary>
        private void DeactivateReflector()
        {
            _shieldCollider.tag = "Untagged";
            _isParrying = false;
            //Allow the character to parry again
            _canParry = true;
        }

        /// <summary>
        /// Turns of the shield and makes the character rest before using another action
        /// </summary>
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
            if (!_canParry || BlackBoardBehaviour.Instance.GetPlayerState(_shieldCollider.Spawner.UnityObject) != "Idle")
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
        }

        /// <summary>
        /// Disables the brace and starts the cooldown
        /// </summary>
        public void DeactivateBrace()
        {
            IsBraced = false;
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
            MatchManagerBehaviour.Instance.ChangeTimeScale(0, 0, 0.05f);
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
            float direction = transform.position.x - other.transform.position.x;
            direction /= Mathf.Abs(direction);


            if (_controller.AttackDirection.X != direction || !other.CompareTag("Structure") || other.CompareTag("CollisionPlane") || BreakingFall)
                return;


            BreakingFall = true;
            _knockBack.CurrentAirState = AirState.BREAKINGFALL;

            PanelBehaviour panel = other.GetComponent<PanelBehaviour>();

            _knockBack.SetInvincibilityByTimer(_wallTechInvincibilityTime);
            Instantiate(_perfectDefenseEffect, transform.position, Camera.main.transform.rotation);

            if (_disableFallBreakAction?.GetEnabled() == true)
                RoutineBehaviour.Instance.StopAction(_disableFallBreakAction);

            transform.rotation = Quaternion.Euler(0, direction * 90, 0);

            _disableFallBreakAction = RoutineBehaviour.Instance.StartNewTimedAction(DisableFallBreaking, TimedActionCountType.SCALEDTIME, _wallTechJumpDuration / 2.0f);
            _knockBack.Physics.Jump(_wallTechJumpHeight, _wallTechJumpDistance, _wallTechJumpDuration, true, true);
            onFallBroken?.Invoke(false);
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
                if (collider.Spawner.UnityObject == gameObject) return;

                _phaseShiftSuccessEvent?.Raise(gameObject);
                _currentPhaseShiftRestTime = _successPhaseShiftRestTime;
                MatchManagerBehaviour.Instance.ChangeTimeScale(_slowMotionTimeScale.Value, _slowMotionTransitionSpeed.Value, _slowMotionTime.Value);
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

        //private void OnCollisionEnter(Collision collision)
        //{
        //    BreakFall(collision.Entity.UnityObject);
        //}

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
