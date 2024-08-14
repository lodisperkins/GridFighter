using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Enter ability description here
    /// </summary>
    public class SF_Straight : ProjectileAbility
    {
        public Transform spawnTransform = null;
        //Used to store a reference to the laser prefab
        private GameObject _projectile;
        //The collider attached to the laser
        private HitColliderData _projectileCollider;
        private Vector3 _defaultScale;

        public override void Init(EntityDataBehaviour newOwner)
        {
            base.Init(Owner);

            //initialize default stats
            abilityData = (ScriptableObjects.AbilityData)(Resources.Load("AbilityData/SF_Straight_Data"));

            //Load the projectile prefab
            _projectile = abilityData.visualPrefab;
            _defaultScale = _projectile.transform.localScale;
        }

        public void SpawnProjectile()
        {

            //Log if a projectile couldn't be found
            if (!_projectile)
            {
                Debug.LogError("Projectile for " + abilityData.abilityName + " could not be found.");
                return;
            }

            //Fire laser
            OwnerMoveset.ProjectileSpawner.Projectile = _projectile;
            GameObject newProjectile = OwnerMoveset.ProjectileSpawner.FireProjectile(abilityData.GetCustomStatValue("Speed"), _projectileCollider);
            newProjectile.transform.localScale = _defaultScale * 2;
            ActiveProjectiles.Add(newProjectile);
        }

        protected override void OnActivate(params object[] args)
        {
            //Initialize collider stats
            float powerScale = (float)args[0];
            _projectileCollider = GetColliderData(0);
            _projectileCollider = _projectileCollider.ScaleStats(powerScale);

            CleanProjectileList();

            //If the maximum amount of instances has been reached for this owner, don't spawn a new one
            if (ActiveProjectiles.Count < abilityData.GetCustomStatValue("MaxInstances") || abilityData.GetCustomStatValue("MaxInstances") < 0)
            {
                if (OwnerMoveScript.IsMoving)
                    OwnerMoveScript.AddOnMoveEndTempAction(SpawnProjectile);
                else
                    SpawnProjectile();
            }
        }
    }
}