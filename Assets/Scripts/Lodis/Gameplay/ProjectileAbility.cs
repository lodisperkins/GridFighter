using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Base class for all projectile based abilities
    /// </summary>
    [System.Serializable]
    public class ProjectileAbility : Ability
    {
        public Transform spawnTransform = null;
        //Usd to store a reference to the projectile prefab
        public GameObject projectile;
        //The collider attached to the projectile
        public HitColliderBehaviour projectileCollider;
        public List<GameObject> _activeProjectiles = new List<GameObject>();
        public bool DestroyOnHit = true;
        public bool IsMultiHit = false;

        public bool despawnAfterTimeLimit { get; private set; }

        public override void Init(GameObject newOwner)
        {
            base.Init(newOwner);

            //initialize default stats
            
            owner = newOwner;

            //Load the projectile prefab
            projectile = (GameObject)Resources.Load("Projectiles/Laser");
        }

        public void CleanProjectileList()
        {
            for (int i = 0; i < _activeProjectiles.Count; i++)
            {
                if (_activeProjectiles[i] == null)
                {
                    _activeProjectiles.RemoveAt(i);
                }
            }
        }

        protected override void Activate(params object[] args)
        {
            projectileCollider = new HitColliderBehaviour(abilityData.GetCustomStatValue("Damage"), abilityData.GetCustomStatValue("KnockBackScale"),
                abilityData.GetCustomStatValue("HitAngle"), despawnAfterTimeLimit, abilityData.GetCustomStatValue("Lifetime"), owner, DestroyOnHit ,IsMultiHit, true, abilityData.GetCustomStatValue("HitStun"));

            //If no spawn transform has been set, use the default owner transform
            if (!ownerMoveset.ProjectileSpawnTransform)
                spawnTransform = owner.transform;
            else
                spawnTransform = ownerMoveset.ProjectileSpawnTransform;

            //Log if a projectile couldn't be found
            if (!projectile)
            {
                Debug.LogError("Projectile for " + abilityData.abilityName + " could not be found.");
                return;
            }

            //Create object to spawn projectile from
            GameObject spawnerObject = new GameObject();
            spawnerObject.transform.parent = spawnTransform;
            spawnerObject.transform.localPosition = Vector3.zero;
            spawnerObject.transform.position = new Vector3(spawnerObject.transform.position.x, spawnerObject.transform.position.y, owner.transform.position.z);
            spawnerObject.transform.forward = owner.transform.forward;

            if (spawnerObject.transform.position.y > BlackBoardBehaviour.Instance.projectileHeight)
            {
                spawnerObject.transform.position = new Vector3
                    (spawnerObject.transform.position.x,
                    BlackBoardBehaviour.Instance.projectileHeight,
                    owner.transform.position.z);
            }

            //Initialize and attach spawn script
            ProjectileSpawnerBehaviour spawnScript = spawnerObject.AddComponent<ProjectileSpawnerBehaviour>();
            spawnScript.projectile = projectile;

            //Fire projectile
            _activeProjectiles.Add(spawnScript.FireProjectile(spawnerObject.transform.forward * abilityData.GetCustomStatValue("Speed"), projectileCollider));

            MonoBehaviour.Destroy(spawnerObject);
        }
    }
}


