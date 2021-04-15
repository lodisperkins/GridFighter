using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{
    public class WN_Blaster : Ability
    {
        public Transform spawnTransform = null;
        public float shotSpeed = 20;
        private GameObject _projectile;
        private HitColliderBehaviour _projectileCollider;
        public override void Init(GameObject newOwner)
        {
            abilityType = Attack.WEAKNEUTRAL;
            name = "WN_Blaster";
            activeFrames = 0;
            restFrames = 1;
            startUpFrames = 1;
            canCancel = false;
            _projectile = (GameObject)Resources.Load("Projectiles/Laser");
            owner = newOwner;

            _projectileCollider = new HitColliderBehaviour(1, 5, 0, true, 1, owner, true);
        }

        public override void Activate()
        {
            if (!spawnTransform)
                spawnTransform = owner.transform;

            if (!_projectile)
            {
                Debug.LogError("Projectile for " + name + " could not be found.");
                return;
            }

            GameObject spawnerObject = new GameObject();

            spawnerObject.transform.parent = spawnTransform;
            spawnerObject.transform.localPosition = Vector3.zero;
            spawnerObject.transform.forward = owner.transform.forward;

            ProjectileSpawnerBehaviour spawnScript = spawnerObject.AddComponent<ProjectileSpawnerBehaviour>();
            spawnScript.projectile = _projectile;
            spawnScript.FireProjectile(spawnerObject.transform.forward * shotSpeed, _projectileCollider);
        }
    }
}


