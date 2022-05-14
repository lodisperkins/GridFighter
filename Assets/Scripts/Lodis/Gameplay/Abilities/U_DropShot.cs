using Lodis.GridScripts;
using Lodis.Movement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// An unblockable ability that drops a shot onto the opponents head if they are on the same row.
    /// </summary>
    public class U_DropShot : ProjectileAbility
    {
	    //Called when ability is created
        public override void Init(GameObject newOwner)
        {
			base.Init(newOwner);
            ProjectileRef = abilityData.visualPrefab;
        }

        private GridMovementBehaviour GetTarget()
        {
            Ray ray = new Ray(owner.transform.position, owner.transform.forward);
            RaycastHit info;
            GridMovementBehaviour movement = null;
            int layerMask = LayerMask.GetMask("Entity", "Player");

            if (Physics.Raycast(ray, out info, BlackBoardBehaviour.Instance.Grid.Width, layerMask))
                movement = info.transform.GetComponent<GridMovementBehaviour>();

            return movement;
        }

	    //Called when ability is used
        protected override void Activate(params object[] args)
        {
            CleanProjectileList();

            //Log if a projectile couldn't be found
            if (!ProjectileRef)
            {
                Debug.LogError("Projectile for " + abilityData.abilityName + " could not be found.");
                return;
            }

            GridMovementBehaviour target = GetTarget();

            if (!target || ActiveProjectiles.Count >= abilityData.GetCustomStatValue("MaxInstances")) return;

            //Create object to spawn projectile from
            GameObject spawnerObject = new GameObject();
            spawnerObject.transform.position = target.CurrentPanel.transform.position + Vector3.up * abilityData.GetCustomStatValue("SpawnHeight");
            spawnerObject.transform.forward = Vector3.down;

            //Initialize and attach spawn script
            ProjectileSpawnerBehaviour spawnScript = spawnerObject.AddComponent<ProjectileSpawnerBehaviour>();
            spawnScript.projectile = ProjectileRef;

            ProjectileCollider = GetColliderBehaviourCopy(0);

            Projectile = spawnScript.FireProjectile(spawnerObject.transform.forward * abilityData.GetCustomStatValue("Speed"), ProjectileCollider);

            Projectile.GetComponent<GridTrackerBehaviour>().Marker = MarkerType.UNBLOCKABLE;

            //Fire projectile
            ActiveProjectiles.Add(Projectile);

            MonoBehaviour.Destroy(spawnerObject);
        }
    }
}