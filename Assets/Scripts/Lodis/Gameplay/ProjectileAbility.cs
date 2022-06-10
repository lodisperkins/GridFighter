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
        public Transform SpawnTransform = null;
        //Usd to store a reference to the projectile prefab
        public GameObject ProjectileRef;
        public GameObject Projectile;
        //The collider attached to the projectile
        public HitColliderBehaviour ProjectileCollider;
        public List<GameObject> ActiveProjectiles = new List<GameObject>();
        public bool DestroyOnHit = true;
        public bool IsMultiHit = false;

        public bool despawnAfterTimeLimit { get; private set; }

        public override void Init(GameObject newOwner)
        {
            base.Init(newOwner);

            //initialize default stats
            
            owner = newOwner;

            //Load the projectile prefab
            ProjectileRef = (GameObject)Resources.Load("Projectiles/Laser");

            //If no spawn transform has been set, use the default owner transform
            if (!ownerMoveset.ProjectileSpawnTransform)
                SpawnTransform = owner.transform;
            else
                SpawnTransform = ownerMoveset.ProjectileSpawnTransform;
        }

        public void CleanProjectileList()
        {
            for (int i = 0; i < ActiveProjectiles.Count; i++)
            {
                if (!ActiveProjectiles[i])
                {
                    ActiveProjectiles.RemoveAt(i);
                    i--;
                }
            }
        }

        protected override void Start(params object[] args)
        {
            base.Start(args);
            ProjectileCollider = (HitColliderBehaviour)GetColliderBehaviourCopy(0);
        }

        protected override void Activate(params object[] args)
        {

            //Log if a projectile couldn't be found
            if (!ProjectileRef)
            {
                Debug.LogError("Projectile for " + abilityData.abilityName + " could not be found.");
                return;
            }

            //Create object to spawn projectile from
            GameObject spawnerObject = new GameObject();
            spawnerObject.transform.parent = SpawnTransform;
            spawnerObject.transform.localPosition = Vector3.zero;
            spawnerObject.transform.position = new Vector3(spawnerObject.transform.position.x, spawnerObject.transform.position.y, owner.transform.position.z);
            spawnerObject.transform.forward = owner.transform.forward;

            //Initialize and attach spawn script
            ProjectileSpawnerBehaviour spawnScript = spawnerObject.AddComponent<ProjectileSpawnerBehaviour>();
            spawnScript.projectile = ProjectileRef;

            Projectile = spawnScript.FireProjectile(spawnerObject.transform.forward * abilityData.GetCustomStatValue("Speed"), ProjectileCollider);

            ProjectileCollider = Projectile.GetComponent<HitColliderBehaviour>();

            //Fire projectile
            ActiveProjectiles.Add(Projectile);

            MonoBehaviour.Destroy(spawnerObject);
        }

        public void DestroyActiveProjectiles()
        {
            CleanProjectileList();
            for (int i = 0; i < ActiveProjectiles.Count; i++)
            {
                MonoBehaviour.Destroy(ActiveProjectiles[i]);
            }
        }

        public override void StopAbility()
        {
            base.StopAbility();
            //DestroyActiveProjectiles();
        }
    }
}


