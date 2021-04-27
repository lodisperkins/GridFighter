using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{
    public class RingBarrierBehaviour : HealthBehaviour
    {
        /// <summary>
        /// Takes damage based on the damage type.
        /// If the damage is less than the durability
        /// or if the damage type isn't knockback type,
        /// no damage is dealt
        /// </summary>
        /// <param name="damage">The amount of damage being applied to the object. 
        /// Ring barriers only break if the damage amount is greater than the total health</param>
        /// <param name="knockBackScale"></param>
        /// <param name="hitAngle"></param>
        /// <param name="damageType"></param>
        /// <returns></returns>
        public override float TakeDamage(float damage, float knockBackScale = 0, float hitAngle = 0, DamageType damageType = DamageType.DEFAULT)
        {
            if (damageType != DamageType.KNOCKBACK)
                return 0;

            if (damage < Health)
                return 0;

            Health -= damage;

            return damage;
        }
    }
}
