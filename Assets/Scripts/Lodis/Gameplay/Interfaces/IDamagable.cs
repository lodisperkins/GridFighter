using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{
    public interface IDamagable
    {
        float TakeDamage(float damage, float knockBackScale = 0, float hitAngle = 0, DamageType damageType = DamageType.DEFAULT);

        float BounceDampen { get; set; }

        float Health { get; }
    }
}


