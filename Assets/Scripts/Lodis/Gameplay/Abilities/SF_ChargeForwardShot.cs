using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Enter ability description here
    /// </summary>
    public class SF_ChargeForwardShot : ProjectileAbility
    {
        public Transform spawnTransform = null;
        //Used to store a reference to the laser prefab
        private GameObject _projectile;
        //The collider attached to the laser
        private HitColliderBehaviour _projectileCollider;

        public override void Init(GameObject newOwner)
        {
            base.Init(newOwner);

            //initialize default stats
            abilityData = (ScriptableObjects.AbilityData)(Resources.Load("AbilityData/SF_ChargeForwardShot_Data"));

            //Load the projectile prefab
            _projectile = abilityData.visualPrefab;
        }

        public void SpawnProjectile()
        {
            //If no spawn transform has been set, use the default owner transform
            if (!ownerMoveset.ProjectileSpawnTransform)
                spawnTransform = owner.transform;
            else
                spawnTransform = ownerMoveset.ProjectileSpawnTransform;

            //Log if a projectile couldn't be found
            if (!_projectile)
            {
                Debug.LogError("Projectile for " + abilityData.abilityName + " could not be found.");
                return;
            }

            //Create object to spawn laser from
            GameObject spawnerObject = new GameObject();
            spawnerObject.transform.parent = spawnTransform;
            spawnerObject.transform.localPosition = Vector3.zero;
            spawnerObject.transform.position = new Vector3(spawnerObject.transform.position.x, spawnerObject.transform.position.y, owner.transform.position.z);
            spawnerObject.transform.forward = owner.transform.forward;

            //Initialize and attach spawn script
            ProjectileSpawnerBehaviour spawnScript = spawnerObject.AddComponent<ProjectileSpawnerBehaviour>();
            spawnScript.projectile = _projectile;

            //Fire laser
            GameObject newProjectile = spawnScript.FireProjectile(spawnerObject.transform.forward * abilityData.GetCustomStatValue("Speed"), _projectileCollider);

            ActiveProjectiles.Add(newProjectile);

            MonoBehaviour.Destroy(spawnerObject);
        }

        protected override void Activate(params object[] args)
        {
            //Initialize collider stats
            float powerScale = (float)args[0];
            _projectileCollider = (HitColliderBehaviour)GetColliderBehaviourCopy(0);
            _projectileCollider.ColliderInfo = _projectileCollider.ColliderInfo.ScaleStats(powerScale);

            CleanProjectileList();
            
            Vector2 moveDir = owner.transform.forward;

            //If the maximum amount of instances has been reached for this owner, don't spawn a new one
            if (ActiveProjectiles.Count < abilityData.GetCustomStatValue("MaxInstances") || abilityData.GetCustomStatValue("MaxInstances") < 0)
            { 
                if (_ownerMoveScript.MoveToPanel(_ownerMoveScript.Position + moveDir))
                    _ownerMoveScript.AddOnMoveEndTempAction(SpawnProjectile);
                else
                    SpawnProjectile();
            }
        }
    }
}