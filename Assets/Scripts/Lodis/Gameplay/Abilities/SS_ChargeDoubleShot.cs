﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Shoots two shots: one shot travels down the row the character was previously,
    /// and the other travels down the panel character moved towards
    /// </summary>
    public class SS_ChargeDoubleShot : ProjectileAbility
    {
        public Transform spawnTransform = null;
        private float _shotDamage = 5;
        private float _shotKnockBack = 1;
        //Usd to store a reference to the laser prefab
        private GameObject _projectile;
        //The collider attached to the laser
        private HitColliderBehaviour _projectileCollider;
        private Movement.GridMovementBehaviour _ownerMoveScript;

        //Called when ability is created
        public override void Init(GameObject newOwner)
        {
            base.Init(newOwner);

            //initialize default stats
            abilityData = (ScriptableObjects.AbilityData)(Resources.Load("AbilityData/SS_ChargeDoubleShot_Data"));
            owner = newOwner;
            _ownerMoveScript = owner.GetComponent<Movement.GridMovementBehaviour>();

            //Load the projectile prefab
            _projectile = (GameObject)Resources.Load("Projectiles/ChargeShot");
        }

        private void SpawnProjectile()
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
            GameObject newProjectile = spawnScript.FireProjectile(spawnerObject.transform.forward * abilityData.GetCustomStatValue("Speed"), _projectileCollider);

            _activeProjectiles.Add(newProjectile);

            MonoBehaviour.Destroy(spawnerObject);
        }

        private IEnumerator Shoot(Vector2 direction)
        {
            SpawnProjectile();
            yield return new WaitForSeconds(abilityData.GetCustomStatValue("TimeBetweenShots"));
            _ownerMoveScript.MoveToPanel(_ownerMoveScript.Position + direction, false, _ownerMoveScript.Alignment);
            _ownerMoveScript.AddOnMoveEndTempAction(SpawnProjectile);
        }

        //Called when ability is used
        protected override void Activate(params object[] args)
        {
            float powerScale = (float)args[0];

            _shotDamage = abilityData.GetCustomStatValue("Damage") * powerScale;
            _shotKnockBack = abilityData.GetCustomStatValue("KnockBackScale") * powerScale;

            _projectileCollider = new HitColliderBehaviour(_shotDamage, _shotKnockBack,
                 abilityData.GetCustomStatValue("HitAngle"), true, abilityData.GetCustomStatValue("Lifetime"), owner, true);

            CleanProjectileList();

            if (_activeProjectiles.Count >= abilityData.GetCustomStatValue("MaxInstances") && abilityData.GetCustomStatValue("MaxInstances") >= 0)
                return;

            Vector2 direction = (Vector2)args[1];
            _ownerMoveScript.StartCoroutine(Shoot(direction));
        }
    }
}