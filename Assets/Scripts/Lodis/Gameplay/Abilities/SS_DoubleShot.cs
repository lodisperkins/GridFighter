using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Shoots two shots: one shot travels down the row the character was previously,
    /// and the other travels down the panel character moved towards
    /// </summary>
    public class SS_DoubleShot : ProjectileAbility
    {
        //Usd to store a reference to the laser prefab
        private GameObject _projectile;
        //The collider attached to the laser
        private HitColliderData _projectileCollider;

        //Called when ability is created
        public override void Init(GameObject newOwner)
        {
            base.Init(newOwner);

            //initialize default stats
            abilityData = (ScriptableObjects.AbilityData)(Resources.Load("AbilityData/SS_DoubleShot_Data"));
            owner = newOwner;

            //Load the projectile prefab
            _projectile = abilityData.visualPrefab;
            
        }

        private void SpawnProjectile()
        {

            //Log if a projectile couldn't be found
            if (!_projectile)
            {
                Debug.LogError("Projectile for " + abilityData.abilityName + " could not be found.");
                return;
            }

            ProjectileSpawnerBehaviour projectileSpawner = OwnerMoveset.ProjectileSpawner;
            projectileSpawner.Projectile = _projectile;

            //Fire laser
            GameObject newProjectile = projectileSpawner.FireProjectile(abilityData.GetCustomStatValue("Speed"), _projectileCollider);

            ActiveProjectiles.Add(newProjectile);
        }

        //Called when ability is used
        protected override void OnActivate(params object[] args)
        {
            float powerScale = (float)args[0];

            _projectileCollider = GetColliderData(0);
            _projectileCollider = _projectileCollider.ScaleStats(powerScale);

            CleanProjectileList();

            if (ActiveProjectiles.Count >= abilityData.GetCustomStatValue("MaxInstances") && abilityData.GetCustomStatValue("MaxInstances") >= 0)
                return;

            Vector2 direction = (Vector2)args[1];
            direction.x = 0; 

            SpawnProjectile();

            if (OwnerMoveScript.IsMoving)
            {
                //Move when the player moves in position or just fire the shot if they can't move
                OwnerMoveScript.AddOnMoveEndTempAction(SpawnProjectile);
            }
        }
    }
}