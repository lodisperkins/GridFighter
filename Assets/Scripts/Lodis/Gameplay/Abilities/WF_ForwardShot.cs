using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Shoots a stronger, slow moving shot.
    /// The shot travels for 2 panels before dissipating.
    /// </summary>
    public class WF_ForwardShot : ProjectileAbility
    {
        //How fast the laser will travel
        public float ShotSpeed = 2;
        //Usd to store a reference to the laser prefab
        private GameObject _projectile;
        //The collider attached to the laser
        private HitColliderData _projectileCollider;

        public override void Init(GameObject newOwner)
        {
            base.Init(newOwner);
            //initialize default stats
            abilityData = (ScriptableObjects.AbilityData)(Resources.Load("AbilityData/WF_ForwardShot_Data"));
            owner = newOwner;

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

            ProjectileSpawnerBehaviour projectileSpawner = OwnerMoveset.ProjectileSpawner;
            projectileSpawner.projectile = _projectile;

            //Fire laser
            GameObject newProjectile = projectileSpawner.FireProjectile(abilityData.GetCustomStatValue("Speed"), _projectileCollider);

            ActiveProjectiles.Add(newProjectile);
        }

        protected override void OnActivate(params object[] args)
        {
            _projectileCollider = GetColliderData(0);

            CleanProjectileList();
            
            Vector2 moveDir = owner.transform.forward;

            //Only fire if there aren't two many instances of this object active
            if (ActiveProjectiles.Count < abilityData.GetCustomStatValue("MaxInstances") || abilityData.GetCustomStatValue("MaxInstances") < 0)
            {
                if (_ownerMoveScript.IsMoving)
                    _ownerMoveScript.AddOnMoveEndTempAction(SpawnProjectile);
                else
                    SpawnProjectile();
            }
        }
    }
}