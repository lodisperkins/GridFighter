using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Shoots a single powerful charge shot down the row the character is facing.
    /// </summary>
    public class SN_ChargeShot : ProjectileAbility
    {
        public Transform spawnTransform = null;

        //Usd to store a reference to the laser prefab
        private GameObject _projectile;
        //The collider attached to the laser
        private HitColliderData _projectileCollider;

        //Called when ability is created
        public override void Init(GameObject newOwner)
        {
            base.Init(newOwner);

            //initialize default stats
            abilityData = (ScriptableObjects.AbilityData)(Resources.Load("AbilityData/SN_ChargeShot_Data"));
            owner = newOwner;

            //Load the projectile prefab
            _projectile = abilityData.visualPrefab;
        }

	    //Called when ability is used
        protected override void OnActivate(params object[] args)
        {

            //Log if a projectile couldn't be found
            if (!_projectile)
            {
                Debug.LogError("Projectile for " + abilityData.abilityName + " could not be found.");
                return;
            }

            //Initialize collider stats
            float powerScale = (float)args[0];
            _projectileCollider = GetColliderData(0);
            _projectileCollider = _projectileCollider.ScaleStats(powerScale);

            //Initialize and attach spawn script
            OwnerMoveset.ProjectileSpawner.projectile = _projectile;

            //Fire laser
            ActiveProjectiles.Add(OwnerMoveset.ProjectileSpawner.FireProjectile(abilityData.GetCustomStatValue("Speed"), _projectileCollider));
        }
    }
}