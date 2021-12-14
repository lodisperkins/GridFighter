using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lodis.Utility;
using Lodis.Movement;

namespace Lodis.Gameplay
{

    public delegate void FallBreakEvent(Vector3 collisionNormal);
    /// <summary>
    /// Handles all defensive options for any character on the grid.
    /// </summary>
    public class CharacterDefenseBehaviour : MonoBehaviour
    {
        private Movement.KnockbackBehaviour _knockBack;
        private Input.InputBehaviour _input;
        private Movement.GridMovementBehaviour _movement;
        [Tooltip("How long the object will be parrying for.")]
        [SerializeField]
        private float _parryLength;
        [Tooltip("How quickly the character will fall after a successful mid-air parry.")]
        [SerializeField]
        private float _parryFallSpeed;
        [Tooltip("The collision script for the parry object.")]
        [SerializeField]
        private ColliderBehaviour _parryCollider;
        private bool _isParrying;
        [Tooltip("How long the character will be invincible for after a successful ground parry.")]
        [SerializeField]
        private float _parryInvincibilityLength;
        private Material _material;
        private Color _defaultColor; 
        [Tooltip("True if the parry cooldown timer is 0.")]
        [SerializeField]
        private bool _canParry = true;
        [Tooltip("How long it takes in seconds for the character to be able to parry again.")]
        [SerializeField]
        private float _airParryCooldown;
        private float _tempParryCooldown;
        [Tooltip("If the magnitude of the velocity reaches this amount, the character can't parry.")]
        [SerializeField]
        private float _parrySpeedLimit;
        private float _tempParrySpeedLimit;
        [Tooltip("How long the character is left immobile after a failed parry.")]
        [SerializeField]
        private float _groundParryRestTime;
        [Tooltip("How far the character will travel while air dodging.")]
        [SerializeField]
        private float _airDodgeDistance;
        [Tooltip("How fast the character will travel while air dodging.")]
        [SerializeField]
        private float _airDodgeSpeed;
        private float _airDodgeDistanceTolerance = 0.1f;
        [SerializeField]
        private float _parryStartUpTime;
        [Tooltip("How fast the wait time to parry in air will decrease as an object is in knockback.")]
        [SerializeField]
        private float _parryCoolDownDecreaseRate;
        [Tooltip("How fast the speed limit to parry in air will increase as an object is in knockback")]
        [SerializeField]
        private float _parrySpeedLimitIncreaseRate;
        [Tooltip("How fast the objects parry ability will upgrade as it's in air.")]
        [SerializeField]
        private float _parryUpgradeRate;
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
        [Tooltip("How long in seconds to stun an enemy after a successful parry.")]
        [SerializeField]
        private float _attackerStunTime;
        private RoutineBehaviour.TimedAction _cooldownTimedAction;
        public FallBreakEvent onFallBroken;

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
            _input = GetComponent<Input.InputBehaviour>();
            _movement = GetComponent<Movement.GridMovementBehaviour>();
            _material = GetComponent<Renderer>().material;
            _knockBack.AddOnKnockBackAction(ResetParry);
            _knockBack.AddOnKnockBackAction(() => StartCoroutine(UpgradeParry()));
            _knockBack.AddOnKnockBackAction(EnableBrace);

            //Initialize default values
            _defaultColor = _material.color;
            _parryCollider.OnHit += ActivateInvinciblity;
            _parryCollider.OnHit += TryReflectProjectile;
            _parryCollider.OnHit += TryStunAttacker;
            _parryCollider.ColliderOwner = gameObject;
        }

        /// <summary>
        /// Enables the parry collider and 
        /// freezes the character in air for a brief moment.
        /// </summary>
        /// <returns></returns>
        private void ActivateAirParry()
        {
            //If the velocity the character is moving at is above the speed limit break
            Vector3 moveVelocity = _knockBack.Physics.LastVelocity;
            if (moveVelocity.magnitude >= _tempParrySpeedLimit)
                return;

            //Stops the character from moving to make parrying easier
            _knockBack.Physics.FreezeInPlaceByTimer(_parryLength, true);

            //Enable parry and update state
            _parryCollider.gameObject.SetActive(true);
            _isParrying = true;
            _canParry = false;
            _knockBack.SetInvincibilityByCondition(condition => IsParrying == false);

            //Start timer for parry
            RoutineBehaviour.Instance.StartNewTimedAction(args => DeactivateAirParry(), TimedActionCountType.SCALEDTIME, _parryLength);
        }

        private void DeactivateAirParry()
        {
            //Disable parry
            _parryCollider.gameObject.SetActive(false);
            _isParrying = false;

            if (!_knockBack.CheckIfIdle())
                _knockBack.InFreeFall = true;

            //Start the parry cooldown
            RoutineBehaviour.Instance.StartNewTimedAction(args => _canParry = true, TimedActionCountType.SCALEDTIME, _tempParryCooldown);
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
            ColliderBehaviour otherHitCollider = other.GetComponentInParent<ColliderBehaviour>();

            Rigidbody otherRigidbody = other.GetComponentInParent<Rigidbody>();

            //If the object collided with is an enemy projectile...
            if (otherHitCollider && otherRigidbody && !otherHitCollider.CompareTag("Player") && !otherHitCollider.CompareTag("Entity"))
            {

            if (otherHitCollider.ColliderOwner == _parryCollider.ColliderOwner)
                return;
                //...reset the active time and reverse its velocity
                otherHitCollider.ColliderOwner = _parryCollider.ColliderOwner;
                otherHitCollider.ResetActiveTime();
                otherRigidbody.AddForce(-otherRigidbody.velocity * 2, ForceMode.Impulse);

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

            if (knockback && other != _parryCollider.ColliderOwner)
                if (!knockback.CheckIfIdle())
                {
                    knockback.Physics.FreezeInPlaceByTimer(_attackerStunTime, false, true);
                }

            if (other != _parryCollider.ColliderOwner)
                healthBehaviour.Stun(_attackerStunTime);
        }

        /// <summary>
        /// Enables the parry collider and freezes character actions
        /// </summary>
        /// <returns></returns>
        private void ActivateGroundParry()
        {
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
        /// Finds a new location based on the distance value and direction.
        /// Then lerps character position to new location.
        /// </summary>
        /// <param name="direction">The direction the character is air dodging in.</param>
        /// <returns></returns>
        private IEnumerator AirDodgeRoutine(Vector2 direction)
        {
            //Find the new position after air dodging
            float lerpVal = 0;
            Vector3 airDodgeOffset = new Vector3(direction.x, 0, direction.y) * _airDodgeDistance;
            Vector3 newPosition = transform.position + airDodgeOffset;

            //Stop all forces acting on the character
            _knockBack.Physics.StopVelocity();

            //Move to location while the character isn't in range
            while (Vector3.Distance(transform.position, newPosition) > _airDodgeDistanceTolerance)
            {
                //Sets the current position to be the current position in the interpolation
                //_knockBack.(Vector3.Lerp(transform.position, newPosition, lerpVal += Time.deltaTime * _airDodgeSpeed));
                //Waits until the next fixed update before resuming to be in line with any physics calls
                yield return new WaitForFixedUpdate();
            }

            //Start cooldown
            yield return new WaitForSeconds(_airParryCooldown);
            _canParry = true;
        }

        /// <summary>
        /// Increases the stats of the parry while the object is in air.
        /// </summary>
        /// <returns></returns>
        private IEnumerator UpgradeParry()
        {
            while (_knockBack.InHitStun || _knockBack.InFreeFall)
            {
                yield return new WaitForSeconds(_parryUpgradeRate);
                _tempParryCooldown -= _parryCoolDownDecreaseRate;
                _tempParrySpeedLimit += _parrySpeedLimitIncreaseRate;
            }
        }

        /// <summary>
        /// Starts a parry cororoutine. The effects of the parry changes
        /// based on whether or not the character is in hit stun.
        /// </summary>
        public void ActivateParry()
        {
            if (_canParry && !_knockBack.InHitStun)
                RoutineBehaviour.Instance.StartNewTimedAction(args => ActivateGroundParry(), TimedActionCountType.SCALEDTIME, _parryStartUpTime);
            else if (_canParry)
            {
                Collider bounceCollider = _knockBack.Physics.BounceCollider;
                Collider[] hits = Physics.OverlapBox(bounceCollider.gameObject.transform.position, bounceCollider.bounds.extents * 2, new Quaternion(), LayerMask.GetMask("Structure", "Panels"));

                if (hits.Length == 0)
                    RoutineBehaviour.Instance.StartNewTimedAction(args => ActivateAirParry(), TimedActionCountType.SCALEDTIME, _parryStartUpTime);
            }
        }

        /// <summary>
        /// Disables the parry collider and invincibilty.
        /// Gives the object the ability to parry again.
        /// </summary>
        public void ResetParry()
        {
            if (!_isParrying)
                return;

            _knockBack.DisableInvincibility();
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
            if (!_knockBack.InHitStun || !_canBrace)
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
        /// Moves the character in air for the amount of distance given for 
        /// the airDodgeDistance value.
        /// </summary>
        /// <param name="direction">The direction to air dodge in.</param>
        public void ActivateAirDodge(Vector2 direction)
        {
            if (_canParry && _knockBack.InHitStun)
                _knockBack.Physics.ApplyVelocityChange(direction * _airDodgeDistance);
                //StartCoroutine(AirDodgeRoutine(direction));
        }

        /// <summary>
        /// Makes the character invincible after a collision occurs
        /// </summary>
        /// <param name="args">The collision arguments</param>
        private void ActivateInvinciblity(params object[] args)
        {
            //Return if the object collided with doesn't have a collider script attached
            if (args.Length > 0)
            {
                ColliderBehaviour collider = ((GameObject)args[0]).GetComponentInChildren<ColliderBehaviour>();

                if (!collider)
                    return;
            }

            _parryCollider.gameObject.SetActive(false);
            _isParrying = false;
            _canParry = true;

            //Unfreeze the object if velocity was stopped in air
            _knockBack.Physics.UnfreezeObject();

            //Deactivate the parry
            _parryCollider.gameObject.SetActive(false);
            _isParrying = false;

            //Apply force downward and make the character invincible if the character was in air
            if (_knockBack.InHitStun)
            {
                _knockBack.InFreeFall = true;
                _knockBack.Physics.ApplyVelocityChange(Vector3.down * _parryFallSpeed);
                _knockBack.SetInvincibilityByCondition(context => !(_knockBack.InFreeFall));
                return;
            }

            //Make the character invincible for a short amount of time if they're on the ground
            _knockBack.SetInvincibilityByTimer(_parryInvincibilityLength);
        }

        private void OnDrawGizmos()
        {
            Collider bounceCollider = _knockBack.Physics.BounceCollider;
            if (_knockBack.InHitStun)
                Gizmos.DrawCube(bounceCollider.gameObject.transform.position, bounceCollider.bounds.extents * 1.5f);
        }
        private void OnTriggerEnter(Collider other)
        {
            if (IsBraced && (other.CompareTag("Structure") || other.CompareTag("Panel")))
            {
                _knockBack.SetInvincibilityByTimer(BraceInvincibilityTime);
                BreakingFall = true;
                RoutineBehaviour.Instance.StartNewTimedAction(args => BreakingFall = false, TimedActionCountType.SCALEDTIME, FallBreakLength);

                _knockBack.Physics.StopVelocity();

                Vector3 collisionDirection = (other.ClosestPoint(transform.position) - transform.position).normalized;
                if (collisionDirection.x != 0)
                {
                    transform.LookAt(new Vector2(collisionDirection.x, transform.position.y));
                    _knockBack.InFreeFall = true;
                }

                onFallBroken?.Invoke(collisionDirection);
                DeactivateAirParry();
                DeactivateGroundParry();
                _knockBack.TryStartLandingLag();
                return;
            }
            Debug.Log("Collided with " + other.name);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (IsBraced && (collision.gameObject.CompareTag("Structure") || collision.gameObject.CompareTag("Panel")))
            {
                _knockBack.SetInvincibilityByTimer(BraceInvincibilityTime);
                BreakingFall = true;
                RoutineBehaviour.Instance.StartNewTimedAction(args => BreakingFall = false, TimedActionCountType.SCALEDTIME, FallBreakLength);

                _knockBack.Physics.StopVelocity();

                Vector3 collisionDirection = collision.GetContact(0).normal;
                if (collisionDirection.x != 0)
                {
                    transform.LookAt(new Vector2(collisionDirection.x, transform.position.y));
                    _knockBack.InFreeFall = true;
                }

                onFallBroken?.Invoke(collisionDirection);
                _knockBack.TryStartLandingLag();
                return;
            }
            Debug.Log("Collided with " + collision.gameObject.name);

        }
        // Update is called once per frame
        void Update()
        {
            //Update color
            if (_knockBack.IsInvincible)
                _material.color = Color.green;

            if (_knockBack.CheckIfIdle())
            {
                _tempParryCooldown = _airParryCooldown;
                _tempParrySpeedLimit = _parrySpeedLimit;
            }
        }
    }
}
