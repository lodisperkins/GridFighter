using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using Lodis.Utility;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Base class for all projectile based abilities
    /// </summary>
    [System.Serializable]
    public class ProjectileAbility : Ability
    {
        public Transform SpawnTransform;
        //Usd to store a reference to the projectile prefab
        public GameObject ProjectileRef;
        public Vector3 ShotDirection;
        public GameObject Projectile;
        //The collider attached to the projectile
        public HitColliderData ProjectileColliderData;
        public List<GameObject> ActiveProjectiles = new List<GameObject>();
        public bool DestroyOnHit = true;
        public bool IsMultiHit = false;
        public bool UseGravity;
        public bool despawnAfterTimeLimit { get; private set; }

        public bool ScaleStats { get; set; }
        public override void Init(GameObject newOwner)
        {
            base.Init(newOwner);

            //initialize default stats
            
            owner = newOwner;
            ProjectileRef = abilityData?.visualPrefab;
        }

        public void CleanProjectileList(bool useName = false)
        {
            for (int i = 0; i < ActiveProjectiles.Count; i++)
            {
                if (!ActiveProjectiles[i].activeInHierarchy || (ActiveProjectiles[i].name != ProjectileRef.name + "(" + abilityData.name + ")" && useName))
                {
                    ActiveProjectiles.RemoveAt(i);
                    i--;
                }
            }
        }

        protected override void OnStart(params object[] args)
        {
            base.OnStart(args);
            ProjectileColliderData = GetColliderData(0);
            CleanProjectileList();
            ProjectileColliderData.OwnerAlignement = OwnerMoveScript.Alignment;
        }

        protected override void OnActivate(params object[] args)
        {

            //Log if a projectile couldn't be found
            if (!ProjectileRef)
            {
                Debug.LogError("Projectile for " + abilityData.abilityName + " could not be found.");
                return;
            }

            ProjectileSpawnerBehaviour projectileSpawner = OwnerMoveset.ProjectileSpawner;
            projectileSpawner.projectile = ProjectileRef;
            SpawnTransform = projectileSpawner.transform;
            ShotDirection = projectileSpawner.transform.forward;

            HitColliderData data = ProjectileColliderData;
            if (ScaleStats)
                data = ProjectileColliderData.ScaleStats((float)args[0]);

            Projectile = projectileSpawner.FireProjectile(ShotDirection * abilityData.GetCustomStatValue("Speed"), data, UseGravity);

            //Fire projectile
            Projectile.name += "(" + abilityData.name + ")";
            ActiveProjectiles.Add(Projectile);
        }

        public void DestroyActiveProjectiles()
        {
            CleanProjectileList();
            for (int i = 0; i < ActiveProjectiles.Count; i++)
            {
                ObjectPoolBehaviour.Instance.ReturnGameObject(ActiveProjectiles[i]);
            }
        }
    }
}


