﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using Lodis.Utility;
using FixedPoints;

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
        public EntityDataBehaviour ProjectileRef;
        public FVector3 ShotDirection;
        public EntityDataBehaviour Projectile;
        //The collider attached to the projectile
        public HitColliderData ProjectileColliderData;
        public List<EntityDataBehaviour> ActiveProjectiles = new();
        public bool DestroyOnHit = true;
        public bool IsMultiHit = false;
        public bool UseGravity;
        public bool DespawnAfterTimeLimit { get; private set; }

        public float Speed;

        public bool ScaleStats { get; set; }
        public override void Init(EntityDataBehaviour newOwner)
        {
            base.Init(newOwner);

            //initialize default stats
            
            Owner = newOwner;
            ProjectileRef = abilityData?.visualPrefab.GetComponent<EntityDataBehaviour>();
        }

        public void CleanProjectileList(bool useName = false)
        {
            for (int i = 0; i < ActiveProjectiles.Count; i++)
            {
                if (!ActiveProjectiles[i].Active || (ActiveProjectiles[i].name != ProjectileRef.name + "(" + abilityData.name + ")" && useName))
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
            projectileSpawner.Projectile = ProjectileRef;
            SpawnTransform = projectileSpawner.transform;
            ShotDirection = projectileSpawner.FixedTransform.Forward;

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


