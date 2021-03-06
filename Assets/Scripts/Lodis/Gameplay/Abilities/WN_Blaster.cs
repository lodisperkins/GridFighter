﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Shoots a quick firing laser. Lasers have no
    /// knockback and deal little damage.Useful for
    /// applying pressure and applying chip damage
    /// </summary>
    public class WN_Blaster : Ability
    {
        public Transform spawnTransform = null;
        //How fast the laser will travel
        public float shotSpeed = 20;
        //Usd to store a reference to the laser prefab
        private GameObject _projectile;
        //The collider attached to the laser
        private HitColliderBehaviour _projectileCollider;

        public override void Init(GameObject newOwner)
        {
            base.Init(newOwner);

            //initialize default stats
            abilityData = (ScriptableObjects.AbilityData)(Resources.Load("AbilityData/WN_Blaster_Data"));
            owner = newOwner;
            _projectileCollider = new HitColliderBehaviour(1, 0, 0, true, 3, owner, true);

            //Load the projectile prefab
            _projectile = (GameObject)Resources.Load("Projectiles/Laser");
        }

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
                Debug.LogError("Projectile for " + abilityData.name + " could not be found.");
                return;
            }

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
            spawnScript.FireProjectile(spawnerObject.transform.forward * shotSpeed, _projectileCollider);

            MonoBehaviour.Destroy(spawnerObject);
        }
    }
}


