using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{
    public class PlayerDefenseBehaviour : MonoBehaviour
    {
        private Movement.KnockbackBehaviour _knockBack;
        private Input.InputBehaviour _input;
        [SerializeField]
        private float _parryLength;
        [SerializeField]
        private float _parryForce;
        [SerializeField]
        private float _parryFallSpeed;
        [SerializeField]
        private ColliderBehaviour _parryCollider;
        private bool _isParrying;
        [SerializeField]
        private float _invincibilityLength;
        private Material _material;
        private Color _defaultColor;
        [SerializeField]
        private bool _canParry = true;
        [SerializeField]
        private float _parryCooldown;
        [SerializeField]
        private float _airDodgeDistance;
        [SerializeField]
        private float _airDodgeSpeed;
        private float _airDodgeDistanceTolerance = 0.1f;
        [SerializeField]
        private float _parrySpeedLimit;
        [SerializeField]
        private float _parryParryRestTime;

        public bool CanParry { get => _canParry; }
        public bool IsParrying { get => _isParrying; }

        // Start is called before the first frame update
        void Start()
        {
            _knockBack = GetComponent<Movement.KnockbackBehaviour>();
            _input = GetComponent<Input.InputBehaviour>();
            _material = GetComponent<Renderer>().material;
            _defaultColor = _material.color;
            _parryCollider.onHit += ActivateInvinciblity;
            _parryCollider.Owner = gameObject;
        }

        private IEnumerator ActivateAirParryRoutine()
        {
            Vector3 moveVelocity = Vector3.zero;

            moveVelocity = _knockBack.LastVelocity;

            if (moveVelocity.magnitude >= _parrySpeedLimit)
                yield break;

            _knockBack.FreezeInPlaceByTimer(_parryLength);

            _parryCollider.gameObject.SetActive(true);
            _isParrying = true;
            _canParry = false;
            _knockBack.InFreeFall = true;

            yield return new WaitForSeconds(_parryLength);
            _parryCollider.gameObject.SetActive(false);
            _isParrying = false;

            if (!_knockBack.IsInvincible)
                _knockBack.ApplyVelocityChange(moveVelocity);

            StartCoroutine(RechargeParry());
        }

        private IEnumerator ActivateGroundParryRoutine()
        {
            Vector3 moveVelocity = Vector3.zero;

            _parryCollider.gameObject.SetActive(true);
            _isParrying = true;
            _input.DisableInput(condition => _isParrying == false);
            _canParry = false;
            _knockBack.InFreeFall = true;

            yield return new WaitForSeconds(_parryLength);
            _parryCollider.gameObject.SetActive(false);

            yield return new WaitForSeconds(_parryParryRestTime);

            _isParrying = false;

            _canParry = true;
        }

        private IEnumerator AirDodgeRoutine(Vector2 direction)
        {
            float lerpVal = 0;
            Vector3 airDodgeForce = new Vector3(direction.x, 0, direction.y) * _airDodgeDistance;
            Vector3 newPosition = transform.position + airDodgeForce;
            _knockBack.StopVelocity();

            while (Vector3.Distance(transform.position, newPosition) > _airDodgeDistanceTolerance)
            {
                //Sets the current position to be the current position in the interpolation
                _knockBack.MoveRigidBodyToLocation(Vector3.Lerp(transform.position, newPosition, lerpVal += Time.deltaTime * _airDodgeSpeed));
                //Waits until the next fixed update before resuming to be in line with any physics calls
                yield return new WaitForFixedUpdate();
            }

            StartCoroutine(RechargeParry());
        }

        private IEnumerator RechargeParry()
        {
            yield return new WaitForSeconds(_parryCooldown);
            _canParry = true;
        }

        public void ActivateParry()
        {
            if (_canParry && !_knockBack.InHitStun)
                StartCoroutine(ActivateGroundParryRoutine());
            else if (_canParry)
                StartCoroutine(ActivateAirParryRoutine());
        }

        public void ActivateAirDodge(Vector2 direction)
        {
            if (_canParry && _knockBack.InHitStun)
                StartCoroutine(AirDodgeRoutine(direction));
        }

        private void ActivateInvinciblity(params object[] args)
        {

            if (args.Length > 0)
            {
                ColliderBehaviour collider = ((GameObject)args[0]).GetComponent<ColliderBehaviour>();

                if (!collider)
                    return;
            }

            _knockBack.UnfreezeObject();

            _parryCollider.gameObject.SetActive(false);
            _isParrying = false;

            if (_knockBack.InHitStun)
            {
                _knockBack.StopVelocity();
                _knockBack.ApplyImpulseForce(Vector3.down * _parryFallSpeed);
                _knockBack.SetInvincibilityByCondition(context => !(_knockBack.InHitStun));
                return;
            }

            _knockBack.SetInvincibilityByTimer(_invincibilityLength);
        }

        // Update is called once per frame
        void Update()
        {
            if (_knockBack.IsInvincible)
                _material.color = Color.green;
        }
    }
}
