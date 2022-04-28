using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lodis.Utility;
using Lodis.Movement;
using Lodis.GridScripts;

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
        private HealthBehaviour _health;
        [Tooltip("How long the object will be parrying for.")]
        [SerializeField]
        private float _parryLength;
        [Tooltip("The collision script for the parry object.")]
        [SerializeField]
        private ColliderBehaviour _parryCollider;
        [SerializeField]
        private bool _isParrying;
        [Tooltip("How long the character will be invincible for after a successful ground parry.")]
        [SerializeField]
        private float _parryInvincibilityLength;
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
        [SerializeField]
        private bool _canBrace;
        [Tooltip("How long in seconds the object has to wait before it can brace itself again.")]
        [SerializeField]
        private float _braceCooldownTime;
        [Tooltip("How long in seconds the object is invincible after catching it's fall.")]
        [SerializeField]
        private float _braceInvincibilityTime;
        [Tooltip("How long in seconds the object is going to spend breaking its fall.")]
        [SerializeField]
        private float _fallBreakLength;
        [SerializeField]
        private float _wallTechJumpDistance;
        [SerializeField]
        private float _wallTechJumpAngle;
        [Tooltip("How long in seconds to stun an enemy after a successful parry.")]
        [SerializeField]
        private float _attackerStunTime;
        private RoutineBehaviour.TimedAction _cooldownTimedAction;
        public FallBreakEvent onFallBroken;
        private RoutineBehaviour.TimedAction _parryTimer;
        [Tooltip("The maximum angle allowed between the collision normal and the back of the character in order to count as a wall fall break.")]
        [SerializeField]
        private float _wallFallBreakAngle;

        [Tooltip("The maximum angle allowed between the collision normal and the bottom of the character in order to count as a floor fall break.")]
        [SerializeField]
        private float _floorFallBreakAngle;

        public bool BreakingFall { get; private set; }
        public float BraceInvincibilityTime { get => _braceInvincibilityTime; }
        public bool CanParry { get => _canParry; }
        public bool IsParrying { get => _isParrying; }
        public bool IsBraced { get; private set; }
        public float FallBreakLength { get => _fallBreakLength; set => _fallBreakLength = value; }

        // Start is called before the first frame update
        void Start()
        {
            //Initialize components
            _knockBack = GetComponent<Movement.KnockbackBehaviour>();
            _movement = GetComponent<Movement.GridMovementBehaviour>();
            _health = GetComponent<HealthBehaviour>();

            _knockBack.AddOnKnockBackAction(ResetParry);
            _knockBack.AddOnKnockBackAction(EnableBrace);

            _parryCollider.OnHit += ActivateInvinciblity;
        }

        /// <summary>
        /// Checks if the object it collided with is an enemy projectile.
        /// If so, reverses velocity
        /// </summary>
        /// <param name="gameObject"></param>
        public void TryReflectProjectile(params object[] args)
        {
            GameObject other = (GameObject)args[0];
            //Get collider and rigidbody to check the owner and add the force
            HitColliderBehaviour otherHitCollider = other.GetComponentInParent<HitColliderBehaviour>();

            GridPhysicsBehaviour gridPhysics = other.GetComponentInParent<GridPhysicsBehaviour>();

            //If the object collided with is an enemy projectile...
            if (otherHitCollider && gridPhysics && !otherHitCollider.CompareTag("Player") && !otherHitCollider.CompareTag("Entity"))
            {

                if (otherHitCollider.Owner == _parryCollider.Owner)
                    return;
                //...reset the active time and reverse its velocity
                otherHitCollider.Owner = _parryCollider.Owner;
                otherHitCollider.ResetActiveTime();
                gridPhysics.ApplyVelocityChange(-gridPhysics.LastVelocity * 2);

            }
        }

        public void TryStunAttacker(params object[] args)
        {
            GameObject other = (GameObject)args[0];
            HealthBehaviour healthBehaviour = other.GetComponentInParent<HealthBehaviour>();
            KnockbackBehaviour knockback = other.GetComponentInParent<KnockbackBehaviour>();

            if (!healthBehaviour || other.CompareTag("Entity") || other.CompareTag("Player"))
                return;
            else if (healthBehaviour.Stunned)
                return;

            if (knockback && other != _parryCollider.Owner)
                if (!knockback.CheckIfIdle())
                {
                    knockback.Physics.FreezeInPlaceByTimer(_attackerStunTime, false, true);
                }

            if (other != _parryCollider.Owner)
                healthBehaviour.Stun(_attackerStunTime);
        }

        /// <summary>
        /// Enables the parry collider and freezes character actions
        /// </summary>
        /// <returns></returns>
        private void ActivateGroundParry()
        {
            if (_knockBack.InHitStun)
                return;

            //Enable parry and update states
            _parryCollider.gameObject.SetActive(true);
            _isParrying = true;
            _movement.DisableMovement(condition => _isParrying == false, true, true);
            _canParry = false;
            _knockBack.SetInvincibilityByTimer(_parryLength);

            RoutineBehaviour.Instance.StartNewTimedAction(args => DeactivateGroundParry(), TimedActionCountType.SCALEDTIME, _parryLength);
        }

        private void DeactivateGroundParry()
        {
            //Disable parry
            _parryCollider.gameObject.SetActive(false);

            //Start timer for player immobility
            if (!_knockBack.IsInvincible)
            {
                RoutineBehaviour.Instance.StartNewTimedAction(args => { _isParrying = false; _canParry = true; }, TimedActionCountType.SCALEDTIME, _groundParryRestTime);
                return;
            }

            _isParrying = false;
            //Allow the character to parry again
            _canParry = true;
        }

        /// <summary>
        /// Starts a parry cororoutine. The effects of the parry changes
        /// based on whether or not the character is in hit stun.
        /// </summary>
        public void ActivateParry()
        {
            _parryTimer = RoutineBehaviour.Instance.StartNewTimedAction(args => EnableParryByType(), TimedActionCountType.SCALEDTIME, _parryStartUpTime);
        }

        private void EnableParryByType()
        {
            if (_canParry && _knockBack.CheckIfIdle())
                ActivateGroundParry();
        }

        /// <summary>
        /// Disables the parry collider and invincibilty.
        /// Gives the object the ability to parry again.
        /// </summary>
        public void ResetParry()
        {
            if (!_isParrying)
                return;

            //_knockBack.DisableInvincibility();

            if (_knockBack.CheckIfIdle())
                DeactivateGroundParry();

            RoutineBehaviour.Instance.StopTimedAction(_parryTimer);
            _parryCollider.gameObject.SetActive(false);
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

            RoutineBehaviour.Instance.StopTimedAction(_cooldownTimedAction);
        }

        /// <summary>
        /// Braces the object so it may catch it's fall.
        /// </summary>
        public void Brace()
        {
            if (!_knockBack.IsTumbling || !_canBrace)
                return;

            ActivateBrace();
        }

        /// <summary>
        /// Braces the object based on the brace active time.
        /// Starts the cooldown when done.
        /// </summary>
        /// <returns></returns>
        private void ActivateBrace()
        {
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
        private void ActivateInvinciblity(params object[] args)
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

            if (otherHitCollider.Owner == _parryCollider.Owner)
                return;

            //Deactivate the parry
            _parryCollider.gameObject.SetActive(false);
            _isParrying = false;
            _canParry = true;

            //Make the character invincible for a short amount of time if they're on the ground
            _knockBack.SetInvincibilityByTimer(_parryInvincibilityLength);
        }

        private void OnDrawGizmos()
        {
            if (!_knockBack)
                return;

            if (_knockBack.IsTumbling)
            {
                Collider bounceCollider = _knockBack.Physics.BounceCollider;
                Gizmos.DrawCube(bounceCollider.gameObject.transform.position, bounceCollider.bounds.extents * 1.5f);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsBraced || !(other.CompareTag("Structure") || other.CompareTag("Panel")) || BreakingFall)
                return;

            BreakingFall = true;

            PanelBehaviour panel = other.GetComponent<PanelBehaviour>();

            if (panel?.Alignment != _movement.Alignment && other.gameObject.CompareTag("Panel"))
                _knockBack.SetInvincibilityByCondition(condition => 
                {
                    BlackBoardBehaviour.Instance.Grid.GetPanelAtLocationInWorld(transform.position, out panel, true);
                    return panel.Alignment == _movement.Alignment; 
                } );
            else
                _knockBack.SetInvincibilityByTimer(BraceInvincibilityTime);

            _knockBack.Physics.StopVelocity();

            RoutineBehaviour.Instance.StartNewTimedAction(DisableFallBreaking, TimedActionCountType.SCALEDTIME, FallBreakLength);

            if (other.gameObject.CompareTag("Structure"))
            {
                Vector3 jumpForce = _knockBack.Physics.CalculatGridForce(_wallTechJumpDistance, _wallTechJumpAngle);
                _knockBack.Physics.ApplyImpulseForce(jumpForce);
                _knockBack.IsTumbling = false;
                onFallBroken?.Invoke(false);
                return;
            }

            if (_parryTimer != null)
                if (_parryTimer.GetEnabled()) RoutineBehaviour.Instance.StopTimedAction(_parryTimer);

            RoutineBehaviour.Instance.StopTimedAction(_parryTimer);
            DeactivateGroundParry();

            onFallBroken?.Invoke(true);
            _knockBack.TryStartLandingLag();
            return;
            //Debug.Log("Collided with " + other.name);
        }

        private void DisableFallBreaking(params object[] args)
        {
            BreakingFall = false;
            _knockBack.Physics.IgnoreForces = false;
            if (!_knockBack.Physics.IsGrounded) _knockBack.InFreeFall = true;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!IsBraced || !(collision.gameObject.CompareTag("Structure") || collision.gameObject.CompareTag("Panel")) || BreakingFall)
                return;

            BreakingFall = true;

            PanelBehaviour panel = collision.gameObject.GetComponent<PanelBehaviour>();

            if (panel?.Alignment != _movement.Alignment && collision.gameObject.CompareTag("Panel"))
                _knockBack.SetInvincibilityByCondition(condition => !_movement.IsMoving && _movement.CurrentPanel.Alignment == _movement.Alignment);
            else
                 _knockBack.SetInvincibilityByTimer(BraceInvincibilityTime);

            RoutineBehaviour.Instance.StartNewTimedAction(DisableFallBreaking, TimedActionCountType.SCALEDTIME, FallBreakLength);

            _knockBack.Physics.StopVelocity();

            if (collision.gameObject.CompareTag("Structure"))
            {
                Vector3 jumpForce = _knockBack.Physics.CalculatGridForce(_wallTechJumpDistance, _wallTechJumpAngle);
                _knockBack.Physics.ApplyVelocityChange(jumpForce / _knockBack.Physics.Mass);
                onFallBroken?.Invoke(false);
                return;
            }

            if (_parryTimer != null)
                if (_parryTimer.GetEnabled()) RoutineBehaviour.Instance.StopTimedAction(_parryTimer);

            RoutineBehaviour.Instance.StopTimedAction(_parryTimer);
            DeactivateGroundParry();

            onFallBroken?.Invoke(true);
            _knockBack.TryStartLandingLag();
            //Debug.Log("Collided with " + other.name);

        }
    }
}
