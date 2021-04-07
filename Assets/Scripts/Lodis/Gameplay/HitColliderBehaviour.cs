using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Lodis.Gameplay
{
    public class HitColliderBehaviour : MonoBehaviour
    {
        [SerializeField]
        private float _damage;
        [SerializeField]
        private float _knockBackScale;
        [SerializeField]
        private float _hitAngle;
        [SerializeField]
        private float _activeFrames;
        [SerializeField]
        private bool _isMultiHit;
        private float _currentFramesActive;
        private List<GameObject> _collisions;

        private void Awake()
        {
            _collisions = new List<GameObject>();
        }

        public void Init(float damage, float knockBackScale, float hitAngle, float activeFrames)
        {
            _damage = damage;
            _knockBackScale = knockBackScale;
            _hitAngle = hitAngle;
            _activeFrames = activeFrames;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_collisions.Contains(other.gameObject) || _isMultiHit)
                return;

            _collisions.Add(other.gameObject);

            //Grab whatever health script is attached to this object. If none return
            IDamagable damageScript = other.GetComponent<IDamagable>();

            if (damageScript != null)
                damageScript.TakeDamage(_damage, _knockBackScale, _hitAngle);
        }

        private void OnTriggerStay(Collider other)
        {
            if (!_isMultiHit)
                return;

            //Grab whatever health script is attached to this object. If none return
            IDamagable damageScript = other.GetComponent<IDamagable>();

            if (damageScript != null)
                damageScript.TakeDamage(_damage, _knockBackScale, _hitAngle);
        }

        private void FixedUpdate()
        {
            if (_currentFramesActive >= _activeFrames)
                Destroy(gameObject);

            _currentFramesActive += Time.deltaTime;
        }
    }
}


