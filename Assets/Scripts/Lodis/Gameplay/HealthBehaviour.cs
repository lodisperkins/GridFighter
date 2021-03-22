using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{
    public class HealthBehaviour : MonoBehaviour, IDamagable
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

        public float TakeDamage(params object[] args)
        {
            float damageAmount = (float)args[0];

            if (!IsAlive)
                return 0;

            _health -= damageAmount;

            if (_health < 0)
                _health = 0;

            return damageAmount;
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


