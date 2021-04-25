using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{
    public class RingBarrierBehaviour : MonoBehaviour,IDamagable
    {
        [Tooltip("How much force has to be exerted on the barrier to break it.")]
        [SerializeField]
        private float _durability = 5;
        [Tooltip("How much this object will reduce the velocity of objects that bounce off of it.")]
        [SerializeField]
        private float _bounceDampen = 1;

        public float BounceDampen { get => _bounceDampen; set => _bounceDampen = value; }

        public float Health => _durability;

        /// <summary>
        /// Takes damage based on the damage type.
        /// If the damage is less than the durability
        /// or if the damage type isn't knockback type,
        /// no damage is dealt
        /// </summary>
        /// <param name="damage"></param>
        /// <param name="knockBackScale"></param>
        /// <param name="hitAngle"></param>
        /// <param name="damageType"></param>
        /// <returns></returns>
        public float TakeDamage(float damage, float knockBackScale = 0, float hitAngle = 0, DamageType damageType = DamageType.DEFAULT)
        {
            if (damageType != DamageType.KNOCKBACK)
                return 0;

            if (damage < _durability)
                return 0;

            _durability -= damage;

            return damage;
        }

        private void OnCollisionEnter(Collision collision)
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

            Debug.Log(knockbackScale * 2);

            //Apply ricochet force and damage
            knockBackScript.TakeDamage(knockbackScale * 2, knockbackScale / BounceDampen, hitAngle);
        }

        // Update is called once per frame
        void Update()
        {
            if (_durability <= 0)
                Destroy(gameObject);
        }
    }
}
