using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis
{
    public class HealthBehaviour : MonoBehaviour
    {
        [SerializeField]
        private float _health;
        [SerializeField]
        private bool _destroyOnDeath;
        [SerializeField]
        private bool _isAlive;

        public bool IsAlive
        {
            get
            {
                return _isAlive;
            }
        }

        private void Start()
        {
            _isAlive = true;
        }

        public void TakeDamage(float damageAmount)
        {
            if (!IsAlive)
                return;

            _health -= damageAmount;

            if (_health < 0)
                _health = 0;
        }

        // Update is called once per frame
        void Update()
        {
            _isAlive = _health > 0;

            if (!IsAlive && _destroyOnDeath)
                Destroy(gameObject);
        }
    }
}


