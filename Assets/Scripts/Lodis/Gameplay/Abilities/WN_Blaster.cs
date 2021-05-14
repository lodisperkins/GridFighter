using System.Collections;
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
            abilityType = BasicAbilityType.WEAKNEUTRAL;
            name = "WN_Blaster";
            timeActive = 0.1f;
            recoverTime = 0;
            startUpTime = 0;
            canCancel = false;
            owner = newOwner;
            _projectileCollider = new HitColliderBehaviour(1, 0, 0, true, 3, owner, true);

            //Load the projectile prefab
            _projectile = (GameObject)Resources.Load("Projectiles/Laser");
        }

        protected override void Activate(params object[] args)
        {
            //If no spawn transform has been set, use the default owner transform
            if (!spawnTransform)
                spawnTransform = owner.transform;

            //Log if a projectile couldn't be found
            if (!_projectile)
            {
                Debug.LogError("Projectile for " + name + " could not be found.");
                return;
            }

            //Create object to spawn laser from
            GameObject spawnerObject = new GameObject();
            spawnerObject.transform.parent = spawnTransform;
            spawnerObject.transform.localPosition = Vector3.zero;
            spawnerObject.transform.forward = owner.transform.forward;

            //Initialize and attach spawn script
            ProjectileSpawnerBehaviour spawnScript = spawnerObject.AddComponent<ProjectileSpawnerBehaviour>();
            spawnScript.projectile = _projectile;

            //Fire laser
            spawnScript.FireProjectile(spawnerObject.transform.forward * shotSpeed, _projectileCollider);

            MonoBehaviour.Destroy(spawnerObject);
        }
    }
}


