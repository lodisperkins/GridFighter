using System.Collections;
using System.Collections.Generic;
using Types;
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
        private EntityDataBehaviour _projectile;
        private Fixed32 _powerScale;

        //The collider attached to the laser
        private HitColliderData _projectileCollider;
        private Vector3 _defaultScale;

        public override void Init(EntityDataBehaviour newOwner)
        {
            base.Init(newOwner);

            //initialize default stats
            abilityData = (ScriptableObjects.AbilityData)(Resources.Load("AbilityData/SF_Straight_Data"));

            //Load the projectile prefab
            _projectile = abilityData.visualPrefab.GetComponent<EntityDataBehaviour>();
            _defaultScale = _projectile.transform.localScale;
        }

        public void SpawnProjectile()
        {
            if (!InUse)
                return;
               

            //Log if a projectile couldn't be found
            if (!_projectile)
            {
                Debug.LogError("Projectile for " + abilityData.abilityName + " could not be found.");
                return;
            }

            //Fire laser
            ProjectileSpawnerBehaviour projectileSpawner = OwnerMoveset.ProjectileSpawner;
            projectileSpawner.Projectile = _projectile;

            //Fire laser
            EntityDataBehaviour newProjectile = projectileSpawner.FireProjectile(abilityData.GetCustomStatValue("Speed") * _powerScale, _projectileCollider);

            ActiveProjectiles.Add(newProjectile);
        }

        protected override void OnActivate(params object[] args)
        {
            //CameraBehaviour.ShakeBehaviour.ShakeRotation(0.2f);
            //Initialize collider stats
            _powerScale = (Fixed32)args[0];
            _projectileCollider = GetColliderData(0);
            _projectileCollider = _projectileCollider.ScaleStats(_powerScale);

            CleanProjectileList();

            //If the maximum amount of instances has been reached for this owner, don't spawn a new one
            if (ActiveProjectiles.Count < abilityData.GetCustomStatValue("MaxInstances") || abilityData.GetCustomStatValue("MaxInstances") < 0)
            {
                if (OwnerMoveScript.IsMoving)
                    OwnerMoveScript.AddOnMoveEndTempAction(SpawnProjectile);
                else
                    SpawnProjectile();
            }
            else
            {
                SpawnMaxInstanceSmoke();
            }
        }
    }
}