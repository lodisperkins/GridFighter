using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Enter ability description here
    /// </summary>
    public class SF_ChargeForwardShot : Ability
    {
        public Transform spawnTransform = null;
        //How fast the laser will travel
        public float shotSpeed = 0.5f;
        //Usd to store a reference to the laser prefab
        private GameObject _projectile;
        //The collider attached to the laser
        private HitColliderBehaviour _projectileCollider;
        private Movement.GridMovementBehaviour _ownerMoveScript;

        public override void Init(GameObject newOwner)
        {
            base.Init(newOwner);

            //initialize default stats
            abilityType = AbilityType.WEAKFORWARD;
            name = "WF_ForwardShot";
            timeActive = .2f;
            recoverTime = .1f;
            startUpTime = .1f;
            canCancel = false;
            owner = newOwner;
            _projectileCollider = new HitColliderBehaviour(1, 1, 0.2f, true, 7, owner, true);
            _ownerMoveScript = owner.GetComponent<Movement.GridMovementBehaviour>();

            //Load the projectile prefab
            _projectile = (GameObject)Resources.Load("Projectiles/ChargeShot");
        }

        public void SpawnProjectile()
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

        protected override void Activate(params object[] args)
        {
            _ownerMoveScript.AddOnMoveEndTempAction(SpawnProjectile);
            Vector2 moveDir = owner.transform.forward;
            _ownerMoveScript.MoveToPanel(_ownerMoveScript.Position + moveDir);
        }
    }
}