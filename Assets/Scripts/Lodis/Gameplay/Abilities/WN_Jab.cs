using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Shoots a quick firing laser. Lasers have no
    /// knockback and deal little damage.Useful for
    /// applying pressure and applying chip damage
    /// </summary>
    [System.Serializable]
    public class WN_Jab : ProjectileAbility
    {
        [SerializeField]
        public Transform spawnTransform = null;
        //Usd to store a reference to the laser prefab
        private GameObject _projectile;
        //The collider attached to the laser
        private HitColliderData _projectileCollider;

        public override void Init(GameObject newOwner)
        {
            base.Init(newOwner);

            //initialize default stats
            abilityData = (ScriptableObjects.AbilityData)(Resources.Load("AbilityData/WN_Jab_Data"));
            owner = newOwner;

            //Load the projectile prefab
            _projectile = abilityData.visualPrefab;
        }

        protected override void OnActivate(params object[] args)
        {
            _projectileCollider = GetColliderData(0);

            CleanProjectileList();

            //Log if a projectile couldn't be found
            if (!_projectile)
            {
                Debug.LogError("Projectile for " + abilityData.abilityName + " could not be found.");
                return;
            }

            OwnerMoveset.ProjectileSpawner.projectile = _projectile;

            if (ActiveProjectiles.Count < abilityData.GetCustomStatValue("MaxInstances") || abilityData.GetCustomStatValue("MaxInstances") < 0)
            {
                //Fire laser
                GameObject projectile = OwnerMoveset.ProjectileSpawner.FireProjectile(abilityData.GetCustomStatValue("Speed"), _projectileCollider);
                ActiveProjectiles.Add(projectile);
            }
        }
    }
}


