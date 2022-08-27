using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Shoots two shots: one shot travels down the row the character was
    /// previously, and the other travels down the panel character moved towards.
    /// </summary>
    public class WS_DoubleShot : ProjectileAbility
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
            abilityData = (ScriptableObjects.AbilityData)(Resources.Load("AbilityData/WS_DoubleShot_Data"));
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
            projectileSpawner.projectile = _projectile;

            //Fire laser
            GameObject newProjectile = projectileSpawner.FireProjectile(abilityData.GetCustomStatValue("Speed"), _projectileCollider);

            ActiveProjectiles.Add(newProjectile);
        }

        private IEnumerator Shoot(Vector2 direction)
        {
            SpawnProjectile();
            yield return new WaitForSeconds(abilityData.GetCustomStatValue("TimeBetweenShots"));

            if (_ownerMoveScript.MoveToPanel(_ownerMoveScript.Position + direction, false, _ownerMoveScript.Alignment))
                _ownerMoveScript.AddOnMoveEndTempAction(SpawnProjectile);
            else
                SpawnProjectile();
        }

	    //Called when ability is used
        protected override void Activate(params object[] args)
        {
            _projectileCollider = GetColliderData(0);
             

            CleanProjectileList();

            if (ActiveProjectiles.Count >= abilityData.GetCustomStatValue("MaxInstances") && abilityData.GetCustomStatValue("MaxInstances") >= 0)
                return;

            Vector2 direction = (Vector2)args[1];
            direction.x = 0;
            _ownerMoveScript.StartCoroutine(Shoot(direction));
        }
    }
}