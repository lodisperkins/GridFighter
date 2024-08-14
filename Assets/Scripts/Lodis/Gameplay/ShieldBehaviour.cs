using Lodis.ScriptableObjects;
using System.Collections;
using System.Collections.Generic;
using Types;
using UnityEngine;

namespace Lodis.Gameplay
{
    public class ShieldBehaviour : HealthBehaviour
    {
        [SerializeField]
        private GameObject _owner;

        public GameObject Owner { get => _owner; }

        protected override void Awake()
        {
            base.Awake();
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
        public override float TakeDamage(EntityData attacker, Fixed32 damage, Fixed32 baseKnockBack = default, Fixed32 hitAngle = default, DamageType damageType = DamageType.DEFAULT, Fixed32 hitStun = default)
        {
            if (!Owner) return 0;

            if (IsInvincible || (attacker.UnityObject == Owner))
                return 0;

            Health -= damage;

            return damage;
        }

        /// <summary>
        /// Takes damage based on the damage type.
        /// </summary>
        /// <param name="attacker">The name of the object that damaged this object. Used for debugging</param>
        public override float TakeDamage(HitColliderData info, EntityData attacker)
        {
            if (!IsAlive || IsInvincible  || (attacker.UnityObject == Owner))
                return 0;

            Health -= info.Damage;

            return info.Damage;
        }
    }
}