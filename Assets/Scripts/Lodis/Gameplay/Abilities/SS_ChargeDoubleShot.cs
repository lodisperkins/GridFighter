using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Shoots two shots: one shot travels down the row the character was previously,
    /// and the other travels down the panel character moved towards
    /// </summary>
    public class SS_ChargeDoubleShot : Ability
    {
        public Transform spawnTransform = null;
        //How fast the laser will travel
        public float shotSpeed = 10;
        private float _shotDamage = 5;
        private float _shotKnockBack = 1;
        //Usd to store a reference to the laser prefab
        private GameObject _projectile;
        //The collider attached to the laser
        private HitColliderBehaviour _projectileCollider;
        private Movement.GridMovementBehaviour _ownerMoveScript;
        private float _timeBetweenShots;

        //Called when ability is created
        public override void Init(GameObject newOwner)
        {
            base.Init(newOwner);

            //initialize default stats
            abilityType = BasicAbilityType.STRONGSIDE;
            name = "SS_DoubleShot";
            timeActive = .2f;
            recoverTime = .3f;
            startUpTime = .1f;
            canCancel = false;
            owner = newOwner;
            _ownerMoveScript = owner.GetComponent<Movement.GridMovementBehaviour>();

            //Load the projectile prefab
            _projectile = (GameObject)Resources.Load("Projectiles/ChargeShot");
        }

        private void SpawnProjectile()
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

        private IEnumerator Shoot(Vector2 direction)
        {
            SpawnProjectile();
            yield return new WaitForSeconds(_timeBetweenShots);
            _ownerMoveScript.MoveToPanel(_ownerMoveScript.Position + direction, false, _ownerMoveScript.Alignment);
            _ownerMoveScript.AddOnMoveEndTempAction(SpawnProjectile);
        }

        //Called when ability is used
        protected override void Activate(params object[] args)
        {
            float powerScale = (float)args[0];

            _shotDamage *= powerScale;
            _shotKnockBack *= powerScale;

            _projectileCollider = new HitColliderBehaviour(_shotDamage, _shotKnockBack, 0.2f, true, 2, owner, true);

            Vector2 direction = (Vector2)args[1];
            _ownerMoveScript.StartCoroutine(Shoot(direction));

            _shotDamage /= powerScale;
            _shotKnockBack /= powerScale;
        }
    }
}