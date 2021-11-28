using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Shoots a single powerful charge shot down the row the character is facing.
    /// </summary>
    public class SN_ChargeShot : Ability
    {
        public Transform spawnTransform = null;
        public float shotDamage = 15;
        public float knockBackScale = 1;

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
            float powerScale = (float)args[0];

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
            shotDamage = abilityData.GetCustomStatValue("Damage") * powerScale;
            knockBackScale = abilityData.GetCustomStatValue("KnockBackScale") * powerScale;
            _projectileCollider = new HitColliderBehaviour(shotDamage, knockBackScale, abilityData.GetCustomStatValue("HitAngle"), true,
                abilityData.GetCustomStatValue("Lifetime"), owner, true);
            _projectileCollider.IgnoreColliders = abilityData.IgnoreColliders;
            _projectileCollider.Priority = abilityData.ColliderPriority;

            //Create object to spawn laser from
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
            spawnScript.projectile = _projectile;

            //Fire laser
            spawnScript.FireProjectile(spawnerObject.transform.forward * abilityData.GetCustomStatValue("Speed"), _projectileCollider);
        }
    }
}