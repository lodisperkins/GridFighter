﻿using Lodis.Gameplay;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.ScriptableObjects
{
    [CreateAssetMenu(menuName = "AbilityData/ProjectileAbilityData")]
    class ProjectileAbilityData : AbilityData
    {
        public ProjectileAbilityData()
        {
            _customStats = new Stat[] { new Stat("Damage", 0), new Stat("baseKnockBack", 0), new Stat("HitAngle", 0), new Stat("HitStunTime", 0), new Stat("Speed", 1), new Stat("Lifetime", 1), new Stat("MaxInstances", -1) };
        }
    }
}
