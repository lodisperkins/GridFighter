using Lodis.Movement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{
    public class RingBarrierBehaviour : HealthBehaviour
    {
        public string owner;
        [SerializeField]
        private float _bounceDampen;
        /// <summary>
        /// Takes damage based on the damage type.
        /// If the damage is less than the durability
        /// or if the damage type isn't knockback type,
        /// no damage is dealt
        /// </summary>
        /// <param name="attacker">The name of the object that damaged this object. Used for debugging</param>
        /// <param name="damage">The amount of damage being applied to the object. 
        /// Ring barriers only break if the damage amount is greater than the total health</param>
        /// <param name="knockBackScale"></param>
        /// <param name="hitAngle"></param>
        /// <returns></returns>
        /// <param name="damageType">The type of damage thid object will take</param>
        public override float TakeDamage(string attacker, float damage, float knockBackScale = 0, float hitAngle = 0, DamageType damageType = DamageType.DEFAULT, float hitStun = 0)
        {
            if (damageType != DamageType.KNOCKBACK || IsInvincible || (attacker != owner && owner != ""))
                return 0;

            if (damage < Health)
                return 0;

            Health -= damage;

            return damage;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!IsAlive && collision.gameObject.name == owner)
            {
                Physics.IgnoreCollision(collision.collider, GetComponent<Collider>());
                GetComponent<MeshRenderer>().enabled = false;
            }

            GridPhysicsBehaviour gridPhysicsBehaviour = collision.gameObject.GetComponent<GridPhysicsBehaviour>();

            if (!gridPhysicsBehaviour)
                return;

            if (_bounceDampen == 0)
                _bounceDampen = 1;
            
            gridPhysicsBehaviour.ApplyImpulseForce(-(Vector3.right * gridPhysicsBehaviour.LastVelocity.x * 2)  / _bounceDampen);
        }
    }
}
