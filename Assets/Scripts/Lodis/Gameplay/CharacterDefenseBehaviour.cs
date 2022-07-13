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
        [Tooltip("How long the character is left immobile after a failed parry.")]
        [SerializeField]
        private float _groundParryRestTime;
        [SerializeField]
        private float _parryStartUpTime;
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
        private float _defaultPhaseShiftRestTime;
        [SerializeField]
        private float _successPhaseShiftRestTime;
        private float _currentPhaseShiftRestTime;
        private TimedAction _cooldownTimedAction;
        public FallBreakEvent onFallBroken;
        private TimedAction _parryTimer;
        private DelayedAction _disableFallBreakAction;
        private TimedAction _shieldTimer;
        private bool _canPhaseShift;
        private UnityAction _onPhaseShift;
        [SerializeField]
        [Tooltip("The speed of the game after a phase shift passes through an attack.")]
        private float _slowMotionTimeScale;
        [SerializeField]
        [Tooltip("The amount of time to stay in slow motion after a phase shift passes through an attack..")]
        private float _slowMotionTime;

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

            _movement.AddOnMoveBeginAction(ActivatePhaseShift);
            _shieldCollider.OnHit += args => { if (IsParrying) ActivateParryInvinciblity(args); };
        }

        public void AddOnPhaseShiftAction(UnityAction action)
        {
            _onPhaseShift += action;
        }

        public void ActivatePhaseShift()
        {
            if (!CanPhaseShift) return;
            _isParrying = false;
            //Allow the character to parry again
            _canParry = true;
            _isDefending = false;
            _isPhaseShifting = true;
            _shieldCollider.gameObject.SetActive(false);
            _currentPhaseShiftRestTime = _defaultPhaseShiftRestTime;
            _health.SetIntagibilityByTimer(_phaseShiftDuration);
            _onPhaseShift?.Invoke();
            _movement.AddOnMoveEndTempAction(StartPhaseShiftLag);
        }

        private void StartPhaseShiftLag()
        {
            _movement.DisableMovement(condition => _isPhaseShifting == false, false);
            //Start timer for player immobility
            _shieldTimer = RoutineBehaviour.Instance.StartNewTimedAction(args => { _isPhaseShifting = false; }, TimedActionCountType.SCALEDTIME, _currentPhaseShiftRestTime);
        }

        private void StartDefenseLag(float time)
        {
            _movement.DisableMovement(condition => _isDefending == false, false);
            //Start timer for player immobility
            _shieldTimer = RoutineBehaviour.Instance.StartNewTimedAction(args => { _isDefending = false; }, TimedActionCountType.SCALEDTIME, time);
        }

        /// <summary>
        /// Enables the parry collider and freezes character actions
        /// </summary>
        /// <returns></returns>
        private void ActivateParry()
        {
            _shieldCollider.tag = "Reflector";
            if (BlackBoardBehaviour.Instance.GetPlayerState(gameObject) != "Idle")
                return;

            //Enable parry and update states
            _shieldCollider.gameObject.SetActive(true);
            _isParrying = true;
            _isDefending = true;
            _canParry = false;

            RoutineBehaviour.Instance.StartNewTimedAction(args => DeactivateParry(), TimedActionCountType.SCALEDTIME, _parryLength);
        }

        private void DeactivateParry()
        {
            _shieldCollider.tag = "Untagged";
            _isParrying = false;
            //Allow the character to parry again
            _canParry = true;
            RoutineBehaviour.Instance.StopAction(_parryTimer);
        }

        public void DeactivateShield()
        {
            DeactivateParry();
            if (!_shieldCollider.gameObject.activeSelf)
                return;

            _isParrying = false;
            //Allow the character to parry again
            _canParry = true;

            _shieldCollider.gameObject.SetActive(false);

            StartDefenseLag(_groundParryRestTime);
        }

        /// <summary>
        /// Starts a parry cororoutine. The effects of the parry changes
        /// based on whether or not the character is in hit stun.
        /// </summary>
        public void BeginParry()
        {
            if (_parryTimer?.GetEnabled() == true)
                return;

            _parryTimer = RoutineBehaviour.Instance.StartNewTimedAction(args => { if (_canParry && _knockBack.CheckIfIdle()) ActivateParry(); }, TimedActionCountType.SCALEDTIME, _parryStartUpTime);
        }

        /// <summary>
        /// Disables the parry collider and invincibilty.
        /// Gives the object the ability to parry again.
        /// </summary>
        public void StopShield()
        {
            if (_knockBack.CheckIfIdle())
                DeactivateParry();

            RoutineBehaviour.Instance.StopAction(_parryTimer);
            RoutineBehaviour.Instance.StopAction(_shieldTimer);
            _shieldCollider.gameObject.SetActive(false);
            _isDefending = false;
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

            if (otherHitCollider.Owner == _shieldCollider.Owner)
                return;

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

            //If the player broke their fall on the other side...
            if (panel?.Alignment != _movement.Alignment && other.gameObject.CompareTag("Panel"))
                //...make them invincible until they're back on their side
                _knockBack.SetInvincibilityByCondition(condition =>
                {
                    BlackBoardBehaviour.Instance.Grid.GetPanelAtLocationInWorld(transform.position, out panel, true);
                    return panel.Alignment == _movement.Alignment;
                });
            //Otherwise set invincibility based on the object collided with
            else if (other.gameObject.CompareTag("Panel"))
                _knockBack.SetInvincibilityByTimer(BraceInvincibilityTime);
            else
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

            //Prevents the player from buffering a parry while breaking a fall
            if (_parryTimer != null)
                if (_parryTimer.GetEnabled()) RoutineBehaviour.Instance.StopAction(_parryTimer);

            _disableFallBreakAction = RoutineBehaviour.Instance.StartNewTimedAction(DisableFallBreaking, TimedActionCountType.SCALEDTIME, GroundTechLength);

            RoutineBehaviour.Instance.StopAction(_parryTimer);
            DeactivateParry();

            onFallBroken?.Invoke(true);
            _knockBack.CurrentAirState = AirState.BREAKINGFALL;
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
                
                _currentPhaseShiftRestTime = _successPhaseShiftRestTime;
                GameManagerBehaviour.Instance.ChangeTimeScale(_slowMotionTimeScale, _slowMotionTime);
            }

            if (!IsBraced || !(other.CompareTag("Structure") || other.CompareTag("Panel")) || BreakingFall)
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
            if (!IsBraced || !(collision.gameObject.CompareTag("Structure") || collision.gameObject.CompareTag("Panel")) || BreakingFall)
                return;

            BreakingFall = true;

            PanelBehaviour panel = collision.gameObject.GetComponent<PanelBehaviour>();

            if (panel?.Alignment != _movement.Alignment && collision.gameObject.CompareTag("Panel"))
                _knockBack.SetInvincibilityByCondition(condition => !_movement.IsMoving && _movement.CurrentPanel.Alignment == _movement.Alignment);
            else if (collision.gameObject.CompareTag("Panel"))
                _knockBack.SetInvincibilityByTimer(BraceInvincibilityTime);
            else
                _knockBack.SetInvincibilityByTimer(_wallTechInvincibilityTime);

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

            if (_parryTimer != null)
                if (_parryTimer.GetEnabled()) RoutineBehaviour.Instance.StopAction(_parryTimer);

            _disableFallBreakAction = RoutineBehaviour.Instance.StartNewTimedAction(DisableFallBreaking, TimedActionCountType.SCALEDTIME, GroundTechLength);
            _knockBack.Physics.FreezeInPlaceByTimer(GroundTechLength, false, true);


            RoutineBehaviour.Instance.StopAction(_parryTimer);
            DeactivateParry();

            onFallBroken?.Invoke(true);
            _knockBack.CurrentAirState = AirState.BREAKINGFALL;
            //Debug.Log("Collided with " + other.name);

        }
    }
}
