using Lodis.ScriptableObjects;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{
    public class ShieldBehaviour : HealthBehaviour
    {
        [SerializeField]
        private GameObject _owner;

        public GameObject Owner { get => _owner; }

        private void Awake()
        {
            _owner = transform.root.gameObject;
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
            if (!Owner) return 0;

            if (IsInvincible || (attacker == Owner.name))
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
            if (!IsAlive || IsInvincible  || (attacker == Owner))
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
            if (!Owner) return 0;

            if (!IsAlive || IsInvincible || (attacker == Owner.name))
                return 0;

            float damage = abilityData.GetCustomStatValue("Damage");
            
            Health -= damage;

            return damage;
        }
    }
}