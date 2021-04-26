using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{
    public class HealthBehaviour : MonoBehaviour
    {
        [SerializeField]
        private float _health;
        [SerializeField]
        private bool _destroyOnDeath;
        [SerializeField]
        private bool _isAlive;
        [Tooltip("How much this object will reduce the velocity of objects that bounce off of it.")]
        [SerializeField]
        private float _bounceDampen = 2;

        public bool IsAlive
        {
            get
            {
                return _isAlive;
            }
        }

        public float BounceDampen { get => _bounceDampen; set => _bounceDampen = value; }
        public float Health { get => _health; protected set => _health = value; }

        private void Start()
        {
            _isAlive = true;
        }

        public virtual float TakeDamage(float damage, float knockBackScale = 0, float hitAngle = 0, DamageType damageType = DamageType.DEFAULT)
        {
            if (!IsAlive)
                return 0;

            _health -= damage;

            if (_health < 0)
                _health = 0;

            return damage;
        }

        public virtual void OnCollisionEnter(Collision collision)
        {
            Movement.KnockbackBehaviour knockBackScript = collision.gameObject.GetComponent<Movement.KnockbackBehaviour>();
            //Checks if the object is not grid moveable and isn't in hit stun
            if (!knockBackScript || !knockBackScript.InHitStun)
                return;

            //Calculate the knockback and hit angle for the ricochet
            ContactPoint contactPoint = collision.GetContact(0);
            Vector3 direction = new Vector3(contactPoint.normal.x, contactPoint.normal.y, 0);
            float dotProduct = Vector3.Dot(Vector3.right, -direction);
            float hitAngle = Mathf.Acos(dotProduct);
            float velocityMagnitude = knockBackScript.LastVelocity.magnitude;
            float knockbackScale = knockBackScript.CurrentKnockBackScale * (velocityMagnitude / knockBackScript.LaunchVelocity.magnitude);

            if (knockbackScale == 0 || float.IsNaN(knockbackScale))
                return;

            //Apply ricochet force and damage
            knockBackScript.TakeDamage(knockbackScale * 2, knockbackScale / BounceDampen, hitAngle, DamageType.KNOCKBACK);
        }

        // Update is called once per frame
        public virtual void Update()
        {
            _isAlive = _health > 0;

            if (!IsAlive && _destroyOnDeath)
                Destroy(gameObject);
        }
    }
}


