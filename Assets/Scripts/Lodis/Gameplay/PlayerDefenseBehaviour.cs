using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{
    public class PlayerDefenseBehaviour : MonoBehaviour
    {
        private Movement.KnockbackBehaviour _knockBack;
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

        public bool IsParrying { get => _isParrying;}

        // Start is called before the first frame update
        void Start()
        {
            _knockBack = GetComponent<Movement.KnockbackBehaviour>();
            _material = GetComponent<Renderer>().material;
            _defaultColor = _material.color;
            _parryCollider.onHit += ActivateInvinciblity;
            _parryCollider.Owner = gameObject;
        }

        private IEnumerator ActivateParryRoutine()
        {
            _parryCollider.gameObject.SetActive(true);
            _isParrying = true;

            Vector3 moveVelocity = Vector3.zero;

            if (_knockBack.InHitStun)
            {
                moveVelocity = _knockBack.LastVelocity;
                _knockBack.StopAllForces();
            }

            yield return new WaitForSeconds(_parryLength);
            _parryCollider.gameObject.SetActive(false);
            _isParrying = false;
            _knockBack.UseGravity = true;

            if (_knockBack.InHitStun)
                _knockBack.ApplyImpulseForce(moveVelocity);
        }

        public void ActivateParry()
        {
            StartCoroutine(ActivateParryRoutine());
        }

        private void ActivateInvinciblity(params object[] args)
        {
            if (args.Length > 1)
            {
                ColliderBehaviour collider = (ColliderBehaviour)args[1];

                if (!collider)
                    return;
            }

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
            else
                _material.color = _defaultColor;
        }
    }
}
