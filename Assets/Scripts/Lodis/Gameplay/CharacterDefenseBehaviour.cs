using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        [Tooltip("How long the character will be invincible for after getting up from a knockdown.")]
        [SerializeField]
        private float _recoverInvincibilityLength;
        private Material _material;
        private Color _defaultColor; 
        [Tooltip("True if the parry cooldown timer is 0.")]
        [SerializeField]
        private bool _canParry = true;
        [Tooltip("How long it takes in seconds for the character to be able to parry again.")]
        [SerializeField]
        private float _parryCooldown;
        private float _tempParryCooldown;
        [Tooltip("If the magnitude of the velocity reaches this amount, the character can't parry.")]
        [SerializeField]
        private float _parrySpeedLimit;
        private float _tempParrySpeedLimit;
        [Tooltip("How long the character is left immobile after a failed parry.")]
        [SerializeField]
        private float _parryRestTime;
        [Tooltip("How far the character will travel while air dodging.")]
        [SerializeField]
        private float _airDodgeDistance;
        [Tooltip("How fast the character will travel while air dodging.")]
        [SerializeField]
        private float _airDodgeSpeed;
        private float _airDodgeDistanceTolerance = 0.1f;
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
        private Coroutine _cooldownRoutine;
        public FallBreakEvent onFallBroken;

        public bool BreakingFall { get; private set; }
        public float BraceInvincibilityTime { get => _braceInvincibilityTime; }
        public bool CanParry { get => _canParry; }
        public bool IsParrying { get => _isParrying; }
        public bool IsBraced { get; private set; }

        public float RecoverInvincibilityLength { get => _recoverInvincibilityLength; }

        // Start is called before the first frame update
        void Start()
        {
            //Initialize components
            _knockBack = GetComponent<Movement.KnockbackBehaviour>();
            _input = GetComponent<Input.InputBehaviour>();
            _movement = GetComponent<Movement.GridMovementBehaviour>();
            _material = GetComponent<Renderer>().material;

            //Add knock back event listeners
            _knockBack.AddOnKnockBackAction(MakeInvincibleOnGetUp);
            _knockBack.AddOnKnockBackAction(ResetParry);
            _knockBack.AddOnKnockBackAction(() => StartCoroutine(UpgradeParry()));
            _knockBack.AddOnKnockBackAction(() => StopAllCoroutines());
            _knockBack.AddOnKnockBackAction(() => _isParrying = false);
            _knockBack.AddOnKnockBackAction(EnableBrace);

            //Initialize default values
            _defaultColor = _material.color;
            _parryCollider.OnHit += ActivateInvinciblity;
            _parryCollider.ColliderOwner = gameObject;
            onFallBroken += normal => { BreakingFall = true; StartCoroutine(FallBreakTimer()); };
        }

        private IEnumerator FallBreakTimer()
        {
            yield return new WaitForSeconds(BraceInvincibilityTime);
            BreakingFall = false;
        }

        /// <summary>
        /// Makes the player invincible when they get up after being knocked back
        /// </summary>
        private void MakeInvincibleOnGetUp()
        {
            Movement.Condition invincibilityCondition = condition => !_movement.IsMoving;
            _movement.AddOnMoveBeginTempAction(() => _knockBack.SetInvincibilityByCondition(invincibilityCondition));
            _movement.AddOnMoveEndTempAction(() => StartCoroutine(RecoverInvincibiltyRoutine()));
        }

        /// <summary>
        /// Sets the player to be invincible after waiting one frame.
        /// This is so that recover incincibility is activated after the condition check
        /// for the previous invincibility call.
        /// </summary>
        /// <returns></returns>
        private IEnumerator RecoverInvincibiltyRoutine()
        {
            yield return new WaitForEndOfFrame();
            _knockBack.SetInvincibilityByTimer(_recoverInvincibilityLength);
        }

        /// <summary>
        /// Enables the parry collider and 
        /// freezes the character in air for a brief moment.
        /// </summary>
        /// <returns></returns>
        private IEnumerator ActivateAirParryRoutine()
        {
            if (_knockBack.LastVelocity.magnitude == 0)
                yield return new WaitForFixedUpdate();

            //If the velocity the character is moving at is above the speed limit break
            Vector3 moveVelocity = _knockBack.LastVelocity;
            if (moveVelocity.magnitude >= _tempParrySpeedLimit)
                yield break;

            //Stops the character from moving to make parrying easier
            _knockBack.FreezeInPlaceByTimer(_parryLength);

            //Enable parry and update state
            _parryCollider.gameObject.SetActive(true);
            _isParrying = true;
            _canParry = false;

            //Start timer for parry
            yield return new WaitForSeconds(_parryLength);

            //Disable parry
            _parryCollider.gameObject.SetActive(false);
            _isParrying = false;
            _knockBack.UnfreezeObject();
            _knockBack.InFreeFall = true;

            //If the parry wasn't successful, reapply the old velocity
            if (!_knockBack.IsInvincible)
                _knockBack.ApplyVelocityChange(moveVelocity);

            //Start the parry cooldown
            StartCoroutine(RechargeParry(_tempParryCooldown));
        }

        /// <summary>
        /// Enables the parry collider and freezes character actions
        /// </summary>
        /// <returns></returns>
        private IEnumerator ActivateGroundParryRoutine()
        {
            //Enable parry and update states
            _parryCollider.gameObject.SetActive(true);
            _isParrying = true;
            _input.DisableInput(condition => _isParrying == false);
            _canParry = false;

            //Start timer for parry
            yield return new WaitForSeconds(_parryLength);

            //Disable parry
            _parryCollider.gameObject.SetActive(false);

            //Start timer for player immobility
            if (!_knockBack.IsInvincible)
                yield return new WaitForSeconds(_parryRestTime);

            //Allow the character to parry again
            _isParrying = false;
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
            _knockBack.StopVelocity();

            //Move to location while the character isn't in range
            while (Vector3.Distance(transform.position, newPosition) > _airDodgeDistanceTolerance)
            {
                //Sets the current position to be the current position in the interpolation
                //_knockBack.MoveRigidBodyToLocation(Vector3.Lerp(transform.position, newPosition, lerpVal += Time.deltaTime * _airDodgeSpeed));
                //Waits until the next fixed update before resuming to be in line with any physics calls
                yield return new WaitForFixedUpdate();
            }

            //Start cooldown
            StartCoroutine(RechargeParry(_parryCooldown));
        }

        /// <summary>
        /// Gives the character the ability to parry after the cooldown.
        /// </summary>
        /// <returns></returns>
        private IEnumerator RechargeParry(float parryCooldown)
        {
            yield return new WaitForSeconds(parryCooldown);
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
                StartCoroutine(ActivateGroundParryRoutine());
            else if (_canParry)
                StartCoroutine(ActivateAirParryRoutine());
        }

        /// <summary>
        /// Disables the parry collider and invincibilty.
        /// Gives the object the ability to parry again.
        /// </summary>
        public void ResetParry()
        {
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

            if (_cooldownRoutine != null)
                StopCoroutine(_cooldownRoutine);
        }

        /// <summary>
        /// Braces the object so it may catch it's fall.
        /// </summary>
        public void Brace()
        {
            if (!_knockBack.InHitStun || !_canBrace)
                return;

            StartCoroutine(ActivateBrace());
        }

        /// <summary>
        /// Braces the object based on the brace active time.
        /// Starts the cooldown when done.
        /// </summary>
        /// <returns></returns>
        private IEnumerator ActivateBrace()
        {
            IsBraced = true;
            _canBrace = false;
            yield return new WaitForSeconds(_braceActiveTime);
            IsBraced = false;
            _cooldownRoutine = StartCoroutine(ActivateBraceCooldown());
        }

        /// <summary>
        /// Sets can brace to true after the brace cooldown time has passed.
        /// </summary>
        /// <returns></returns>
        private IEnumerator ActivateBraceCooldown()
        {
            yield return new WaitForSeconds(_braceCooldownTime);
            _canBrace = true;
        }

        /// <summary>
        /// Moves the character in air for the amount of distance given for 
        /// the airDodgeDistance value.
        /// </summary>
        /// <param name="direction">The direction to air dodge in.</param>
        public void ActivateAirDodge(Vector2 direction)
        {
            if (_canParry && _knockBack.InHitStun)
                StartCoroutine(AirDodgeRoutine(direction));
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
                ColliderBehaviour collider = ((GameObject)args[0]).GetComponent<ColliderBehaviour>();

                if (!collider)
                    return;
            }

            _parryCollider.gameObject.SetActive(false);
            _isParrying = false;
            _canParry = true;

            //Unfreeze the object if velocity was stopped in air
            _knockBack.UnfreezeObject();

            //Deactivate the parry
            _parryCollider.gameObject.SetActive(false);
            _isParrying = false;

            //Apply force downward and make the character invincible if the character was in air
            if (_knockBack.InHitStun)
            {
                _knockBack.MakeKinematic();
                _knockBack.SetInvincibilityByCondition(context => !(_knockBack.InHitStun));
                return;
            }

            //Make the character invincible for a short amount of time if they're on the ground
            _knockBack.SetInvincibilityByTimer(_parryInvincibilityLength);
        }

        // Update is called once per frame
        void Update()
        {
            //Update color
            if (_knockBack.IsInvincible)
                _material.color = Color.green;

            if (_knockBack.CheckIfAtRest())
            {
                _tempParryCooldown = _parryCooldown;
                _tempParrySpeedLimit = _parrySpeedLimit;
            }
        }
    }
}
