using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Enter ability description here
    /// </summary>
    public class SF_ChargeForwardShot : ProjectileAbility
    {
        public Transform spawnTransform = null;
        //Used to store a reference to the laser prefab
        private GameObject _projectile;
        //The collider attached to the laser
        private HitColliderData _projectileCollider;

        public override void Init(GameObject newOwner)
        {
            base.Init(newOwner);

            //initialize default stats
            abilityData = (ScriptableObjects.AbilityData)(Resources.Load("AbilityData/SF_ChargeForwardShot_Data"));

            //Load the projectile prefab
            _projectile = abilityData.visualPrefab;
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
            OwnerMoveset.ProjectileSpawner.projectile = _projectile;
            GameObject newProjectile = OwnerMoveset.ProjectileSpawner.FireProjectile(abilityData.GetCustomStatValue("Speed"), _projectileCollider);
            newProjectile.transform.localScale *= 2;
            ActiveProjectiles.Add(newProjectile);
        }

        protected override void Activate(params object[] args)
        {
            //Initialize collider stats
            float powerScale = (float)args[0];
            _projectileCollider = GetColliderData(0);
            _projectileCollider = _projectileCollider.ScaleStats(powerScale);

            CleanProjectileList();
            
            Vector2 moveDir = owner.transform.forward;

            //If the maximum amount of instances has been reached for this owner, don't spawn a new one
            if (ActiveProjectiles.Count < abilityData.GetCustomStatValue("MaxInstances") || abilityData.GetCustomStatValue("MaxInstances") < 0)
            { 
                if (_ownerMoveScript.MoveToPanel(_ownerMoveScript.Position + moveDir))
                    _ownerMoveScript.AddOnMoveEndTempAction(SpawnProjectile);
                else
                    SpawnProjectile();
            }
        }
    }
}