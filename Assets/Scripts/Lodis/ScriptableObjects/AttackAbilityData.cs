using Lodis.Gameplay;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Lodis.ScriptableObjects
{
    [CreateAssetMenu(menuName = "AbilityData/AttackAbilityData")]
    class AttackAbilityData : AbilityData
    {

        public HitColliderInfo[] HitColliders
        {
            get => (HitColliderInfo[])base.ColliderData;
        }

        public AttackAbilityData()
        {
            _customStats = new Stat[] { new Stat("Damage", 0), new Stat("baseKnockBack", 0), new Stat("HitAngle", 0) };
        }
    }
}
