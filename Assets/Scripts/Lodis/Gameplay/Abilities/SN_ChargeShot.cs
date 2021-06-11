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
        //How fast the laser will travel
        public float shotSpeed = 15;
        public float shotDamage = 15;
        public float knockBackScale = 1;
        public float hitAngle = 0.4f;
        public float lifeTime = 1.0f;

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
            _projectile = (GameObject)Resources.Load("Projectiles/ChargeShot");
        }

	    //Called when ability is used
        protected override void Activate(params object[] args)
        {
            float powerScale = (float)args[0];

            //If no spawn transform has been set, use the default owner transform
            if (!spawnTransform)
                spawnTransform = owner.transform;

            //Log if a projectile couldn't be found
            if (!_projectile)
            {
                Debug.LogError("Projectile for " + abilityData.name + " could not be found.");
                return;
            }

            shotDamage *= powerScale;
            knockBackScale *= powerScale;

            _projectileCollider = new HitColliderBehaviour(shotDamage, knockBackScale, 0.4f, true, 8.0f, owner, true);

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

            shotDamage /= powerScale;
            knockBackScale /= powerScale;
        }
    }
}