using Lodis.Movement;
using Lodis.ScriptableObjects;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{
    public class RingBarrierBehaviour : HealthBehaviour
    {
        public string Owner;
        [SerializeField]
        private float _bounceDampen;
        [Tooltip("The length of the hit stun to apply to objects that crash into the ring barrier.")]
        [SerializeField]
        private float _hitStunOnCollision;
        private MeshRenderer _renderer;

        private void Start()
        {
            _renderer = GetComponent<MeshRenderer>();
        }

        /// <summary>
        /// Takes damage based on the damage type.
        /// If the damage is less than the durability
        /// or if the damage type isn't knockback type,
        /// no damage is dealt
        /// </summary>
        /// <param name="attacker">The name of the object that damaged this object. Used for debugging</param>
        /// <param name="damage">The amount of damage being applied to the object. 
        /// Ring barriers only break if the damage amount is greater than the total health</param>
        /// <param name="baseKnockBack"></param>
        /// <param name="hitAngle"></param>
        /// <returns></returns>
        /// <param name="damageType">The type of damage this object will take</param>
        public override float TakeDamage(string attacker, float damage, float baseKnockBack = 0, float hitAngle = 0, DamageType damageType = DamageType.DEFAULT, float hitStun = 0)
        {
            if (damageType != DamageType.KNOCKBACK || IsInvincible || (attacker != Owner && Owner != ""))
                return 0;

            if (damage < Health)
                return 0;

            Health -= damage;

            return damage;
        }

        /// <summary>
        /// Takes damage based on the damage type.
        /// </summary>
        /// <param name="attacker">The name of the object that damaged this object. Used for debugging</param>
        public override float TakeDamage(HitColliderInfo info, GameObject attacker)
        {
            if (info.TypeOfDamage != DamageType.KNOCKBACK || (attacker.name != Owner && Owner != ""))
                return 0;

            if (info.Damage < Health)
                return 0;

            Health -= info.Damage;

            return info.Damage;
        }

        /// <summary>
        /// Takes damage based on the damage type.
        /// </summary>
        /// <param name="attacker">The name of the object that damaged this object. Used for debugging</param>
        /// <param name="abilityData">The data scriptable object associated with the ability</param>
        /// <param name="damageType">The type of damage this object will take</param>
        public override float TakeDamage(string attacker, AbilityData abilityData, DamageType damageType = DamageType.DEFAULT)
        {
            if (damageType != DamageType.KNOCKBACK || (attacker != Owner && Owner != ""))
                return 0;

            float damage = abilityData.GetColliderInfo(0).Damage;

            if (damage < Health)
                return 0;

            Health -= damage;

            return damage;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!IsAlive && collision.gameObject.name == Owner)
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
