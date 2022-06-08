using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Shoots a single powerful charge shot down the row the character is facing.
    /// </summary>
    public class SN_ChargeShot : ProjectileAbility
    {
        public Transform spawnTransform = null;

        //Usd to store a reference to the laser prefab
        private GameObject _projectile;
        //The collider attached to the laser
        private HitColliderBehaviour _projectileCollider;

        //Called when ability is created
        public override void Init(GameObject newOwner)
        {
            base.Init(newOwner);

            //initialize default stats
            abilityData = (ScriptableObjects.AbilityData)(Resources.Load("AbilityData/SN_ChargeShot_Data"));
            owner = newOwner;

            //Load the projectile prefab
            _projectile = abilityData.visualPrefab;
        }

	    //Called when ability is used
        protected override void Activate(params object[] args)
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

            //Initialize collider stats
            float powerScale = (float)args[0];
            _projectileCollider = (HitColliderBehaviour)GetColliderBehaviourCopy(0);
            _projectileCollider.ColliderInfo = _projectileCollider.ColliderInfo.ScaleStats(powerScale);

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
            ActiveProjectiles.Add(spawnScript.FireProjectile(spawnerObject.transform.forward * abilityData.GetCustomStatValue("Speed"), _projectileCollider));
        }
    }
}