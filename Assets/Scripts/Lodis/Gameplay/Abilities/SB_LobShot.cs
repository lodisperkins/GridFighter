﻿using Lodis.Movement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Shoots an arcing shot upwards that travels two panels.
    /// Can move over obstacles on the panel in front of it.
    /// On impact, four smaller lob shots spawn and travel to the nearest
    /// panel in all cardinal directions.
    /// </summary>
    public class SB_LobShot : ProjectileAbility
    {
        //Usd to store a reference to the laser prefab
        private GameObject _strongProjectileRef;
        private GameObject _weakProjectilRef;
        private Transform _weakSpawn;
        private HitColliderData _strongProjectileData;
        //The collider attached to the laser
        private HitColliderData _weakProjectileData;
        private float _strongShotDistance = 1;
        private float _weakShotDistance = 1;
        private float _weakShotAngle;
        private float _strongShotAngle;
        private float _weakShotGravity;
        private float _strongShotGravity;

        //Called when ability is created
        public override void Init(GameObject newOwner)
        {
            base.Init(newOwner);

            //initialize default stats
            abilityData = (ScriptableObjects.AbilityData)(Resources.Load("AbilityData/SB_LobShot_Data"));
            owner = newOwner;

            //Load the projectile prefab
            _strongProjectileRef = abilityData.visualPrefab;
            _weakProjectilRef = (GameObject)Resources.Load("Projectiles/Prototype/LobShot");
            _weakShotAngle = abilityData.GetCustomStatValue("WeakShotAngle");
            _strongShotAngle = abilityData.GetCustomStatValue("StrongShotAngle");
            _weakShotGravity = abilityData.GetCustomStatValue("WeakShotGravity");
            _strongShotGravity = abilityData.GetCustomStatValue("StrongShotGravity");
        }

        /// <summary>
        /// Spawns the smaller, weaker lobshot
        /// </summary>
        /// <param name="axis"></param>
        private GameObject SpawnStrongShot(Vector3 axis, float distance, float angle, GameObject projectile, HitColliderData hitColliderData, Vector3 position, float gravity)
        {
            OwnerMoveset.ProjectileSpawner.Projectile = projectile;

            Vector3 launchForce = GridPhysicsBehaviour.CalculatGridForce(distance, angle);
            launchForce.z += axis.z * launchForce.magnitude;
            launchForce.x *= axis.x;

            GameObject activeProjectile = OwnerMoveset.ProjectileSpawner.FireProjectile(launchForce, hitColliderData, true, false);
            activeProjectile.transform.position = position;

            GridPhysicsBehaviour gridPhysics = activeProjectile.GetComponent<GridPhysicsBehaviour>();
            gridPhysics.Gravity = gravity;
            return activeProjectile;
        }

        /// <summary>
        /// Spawns the four weak shots
        /// </summary>
        /// <param name="args"></param>
        private void SpawnWeakShots(params object[] args)
        {
            HitColliderBehaviour hitCollider = (HitColliderBehaviour)args[3];
            Vector3 position = hitCollider.transform.position + Vector3.up * abilityData.GetCustomStatValue("WeakShotSpawnHeight");
            SpawnStrongShot(new Vector3(1, 0, 0), _weakShotDistance, _weakShotAngle, _weakProjectilRef, _weakProjectileData, position, _weakShotGravity);
            SpawnStrongShot(new Vector3(-1, 0, 0), _weakShotDistance, _weakShotAngle, _weakProjectilRef, _weakProjectileData, position, _weakShotGravity);
            SpawnStrongShot(new Vector3(0, 0, 1), _weakShotDistance, _weakShotAngle, _weakProjectilRef, _weakProjectileData, position, _weakShotGravity);
            SpawnStrongShot(new Vector3(0, 0, -1), _weakShotDistance, _weakShotAngle, _weakProjectilRef, _weakProjectileData, position, _weakShotGravity);
            _strongProjectileData.OnHit = null;
        }

        //Called when ability is used
        protected override void OnActivate(params object[] args)
        {

            //Log if a projectile couldn't be found
            if (!_strongProjectileRef)
            {
                Debug.LogError("Projectile for " + abilityData.abilityName + " could not be found.");
                return;
            }

            //Initialize stats of strong and weak colliders
            float powerScale = (float)args[0];
            _strongShotDistance = (powerScale - 1) * abilityData.GetCustomStatValue("StrongShotForceIncreaseRate");
            _strongShotDistance = Mathf.Clamp(_strongShotDistance, 0, abilityData.GetCustomStatValue("StrongHitMaxPower"));

            //Initialize strong shot collider
            _strongProjectileData = GetColliderData(0);
            _strongProjectileData = _strongProjectileData.ScaleStats(powerScale);

            //Initialize weak shot collider
            _weakProjectileData = GetColliderData(1);
            _weakProjectileData = _weakProjectileData.ScaleStats(powerScale);

            CleanProjectileList();

            //If the maximum amount of lobshot instances has been reached for this owner, don't spawn a new one
            if (ActiveProjectiles.Count >= abilityData.GetCustomStatValue("MaxInstances") && abilityData.GetCustomStatValue("MaxInstances") >= 0)
                return;

            _strongProjectileData.OnHit += SpawnWeakShots;
            //Fire laser
            SpawnProjectiles();
        }

        private void SpawnProjectiles()
        {
            GameObject activeStrongShot = SpawnStrongShot(owner.transform.forward, _strongShotDistance, _strongShotAngle, _strongProjectileRef, _strongProjectileData, OwnerMoveset.ProjectileSpawner.transform.position, _strongShotGravity);
            _weakSpawn = activeStrongShot.transform;
            ActiveProjectiles.Add(activeStrongShot);
        }
    }
}