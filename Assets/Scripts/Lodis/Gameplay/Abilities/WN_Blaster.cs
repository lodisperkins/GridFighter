using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{
    public class WN_Blaster : Ability
    {
        public Transform spawnTransform = null;
        public float shotSpeed = 10;
        private GameObject projectile;
        private HitColliderBehaviour projectileCollider;
        public override void Init(GameObject newOwner)
        {
            abilityType = Attack.WEAKNEUTRAL;
            name = "WN_Blaster";
            activeFrames = 0;
            restFrames = 1;
            startUpFrames = 1;
            canCancel = false;
            projectile = (GameObject)Resources.Load("Projectiles/Laser");
            owner = newOwner;

            if (projectile)
                projectileCollider = projectile.GetComponent<HitColliderBehaviour>();
        }

        public override void Activate()
        {
            if (!spawnTransform)
                spawnTransform = owner.transform;

            if (!projectile)
            {
                Debug.LogError("Projectile for " + name + " could not be found.");
                return;
            }

            GameObject spawnerObject = new GameObject();
            spawnerObject.transform.parent = spawnTransform;
            spawnerObject.transform.localPosition = Vector3.zero;
            spawnerObject.transform.forward = owner.transform.forward;
            ProjectileSpawnerBehaviour spawnScript = spawnerObject.AddComponent<ProjectileSpawnerBehaviour>();
            spawnScript.projectile = projectile;
            spawnScript.FireProjectile(spawnerObject.transform.forward * shotSpeed);
        }
    }
}


