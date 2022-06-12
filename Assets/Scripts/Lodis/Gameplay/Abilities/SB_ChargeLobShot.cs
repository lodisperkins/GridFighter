using Lodis.Movement;
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
    public class SB_ChargeLobShot : ProjectileAbility
    {
        //Usd to store a reference to the laser prefab
        private GameObject _strongProjectileRef;
        private GameObject _weakProjectilRef;
        private Transform _weakSpawn;
        private HitColliderBehaviour _strongProjectileCollider;
        //The collider attached to the laser
        private HitColliderBehaviour _weakProjectileCollider;
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
            abilityData = (ScriptableObjects.AbilityData)(Resources.Load("AbilityData/SB_ChargeLobShot_Data"));
            owner = newOwner;

            //Load the projectile prefab
            _strongProjectileRef = abilityData.visualPrefab;
            _weakProjectilRef = (GameObject)Resources.Load("Projectiles/LobShot");
            _weakShotAngle = abilityData.GetCustomStatValue("WeakShotAngle");
            _strongShotAngle = abilityData.GetCustomStatValue("StrongShotAngle");
            _weakShotGravity = abilityData.GetCustomStatValue("WeakShotGravity");
            _strongShotGravity = abilityData.GetCustomStatValue("StrongShotGravity");
        }

        /// <summary>
        /// Spawns the smaller, weaker lobshot
        /// </summary>
        /// <param name="axis"></param>
        private GameObject SpawnShot(Vector3 axis, float distance, float angle, GameObject projectile, HitColliderBehaviour hitCollider, Transform spawn, float gravity)
        {
            //Create object to spawn laser from
            GameObject spawnerObject = new GameObject();
            spawnerObject.transform.parent = spawn;
            spawnerObject.transform.localPosition = Vector3.up / 2;
            spawnerObject.transform.forward = owner.transform.forward;

            //Initialize and attach spawn script
            ProjectileSpawnerBehaviour spawnScript = spawnerObject.AddComponent<ProjectileSpawnerBehaviour>();
            spawnScript.projectile = projectile;

            Vector3 launchForce = GridPhysicsBehaviour.CalculatGridForce(distance, angle);
            launchForce.z += axis.z * launchForce.magnitude;
            launchForce.x *= axis.x;

            GameObject activeProjectile = spawnScript.FireProjectile(launchForce, hitCollider, true);

            GridPhysicsBehaviour gridPhysics = activeProjectile.GetComponent<GridPhysicsBehaviour>();
            gridPhysics.Gravity = gravity;
            //Fire laser

            MonoBehaviour.Destroy(spawnerObject);

            return activeProjectile;
        }

        /// <summary>
        /// Spawns the four weak shots
        /// </summary>
        /// <param name="args"></param>
        private void SpawnWeakShots(params object[] args)
        {
            SpawnShot(new Vector3(1, 0, 0), _weakShotDistance, _weakShotAngle, _weakProjectilRef, _weakProjectileCollider, _weakSpawn, _weakShotGravity);
            SpawnShot(new Vector3(-1, 0, 0), _weakShotDistance, _weakShotAngle, _weakProjectilRef, _weakProjectileCollider, _weakSpawn, _weakShotGravity);
            SpawnShot(new Vector3(0, 0, 1), _weakShotDistance, _weakShotAngle, _weakProjectilRef, _weakProjectileCollider, _weakSpawn, _weakShotGravity);
            SpawnShot(new Vector3(0, 0, -1), _weakShotDistance, _weakShotAngle, _weakProjectilRef, _weakProjectileCollider, _weakSpawn, _weakShotGravity);
            _strongProjectileCollider.OnHit = null;
        }

        //Called when ability is used
        protected override void Activate(params object[] args)
        {
            //If no spawn transform has been set, use the default owner transform
            if (!ownerMoveset.ProjectileSpawnTransform)
                SpawnTransform = owner.transform;
            else
                SpawnTransform = ownerMoveset.ProjectileSpawnTransform;

            //Log if a projectile couldn't be found
            if (!_strongProjectileRef)
            {
                Debug.LogError("Projectile for " + abilityData.abilityName + " could not be found.");
                return;
            }

            //Initialize stats of strong and weak colliders
            float powerScale = (float)args[0];
            _strongShotDistance = (powerScale - 1) / abilityData.GetCustomStatValue("StrongShotForceIncreaseRate");
            _strongShotDistance = Mathf.Clamp(_strongShotDistance, 0, abilityData.GetCustomStatValue("StrongHitMaxPower"));

            //Initialize strong shot collider
            _strongProjectileCollider = GetColliderBehaviourCopy(0);
            _strongProjectileCollider.ColliderInfo = _strongProjectileCollider.ColliderInfo.ScaleStats(powerScale);

            //Initialize weak shot collider
            _weakProjectileCollider = GetColliderBehaviourCopy(1);
            _weakProjectileCollider.ColliderInfo = _weakProjectileCollider.ColliderInfo.ScaleStats(powerScale);

            CleanProjectileList();
           
            //If the maximum amount of lobshot instances has been reached for this owner, don't spawn a new one
            if (ActiveProjectiles.Count >= abilityData.GetCustomStatValue("MaxInstances") && abilityData.GetCustomStatValue("MaxInstances") >= 0)
                return;

            Vector2 offSet = new Vector2(1, 0) * -owner.transform.forward;
            offSet.x = Mathf.RoundToInt(offSet.x);
            offSet.y = Mathf.RoundToInt(offSet.y);

            _ownerMoveScript.MoveToPanel(_ownerMoveScript.Position + offSet);

            _strongProjectileCollider.OnHit += SpawnWeakShots;
            //Fire laser
            GameObject activeStrongShot = SpawnShot(owner.transform.forward, _strongShotDistance, _strongShotAngle, _strongProjectileRef, _strongProjectileCollider, SpawnTransform, _strongShotGravity);
            _weakSpawn = activeStrongShot.transform;
            ActiveProjectiles.Add(activeStrongShot);
        }
    }
}