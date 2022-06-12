using System;
using Lodis.Movement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Shoots an arcing shot upwards that travels two panels.
    /// Can move over obstacles on the panel in front of it.
    /// </summary>
    public class WB_LobShot : ProjectileAbility
    {
        public Transform spawnTransform = null;
        //How fast the laser will travel
        public float shotSpeed = 20;
        //Usd to store a reference to the laser prefab
        private GameObject _projectile;
        //The collider attached to the laser
        private HitColliderBehaviour _projectileCollider;
        private float _shotDistance = 1;
        private float _distance;
        private float _angle;
        private float _gravity;

        //Called when ability is created
        public override void Init(GameObject newOwner)
        {
			base.Init(newOwner);

            //initialize default stats
            abilityData = (ScriptableObjects.AbilityData)(Resources.Load("AbilityData/WB_LobShot_Data"));
            owner = newOwner;
            
            _ownerMoveScript = owner.GetComponent<Movement.GridMovementBehaviour>();
            _distance = abilityData.GetCustomStatValue("PanelDistance");
            _angle = abilityData.GetCustomStatValue("Angle");
            _gravity = abilityData.GetCustomStatValue("Gravity");
            
            //Load the projectile prefab
            _projectile = abilityData.visualPrefab;
        }

        //Called when ability is used
        protected override void Activate(params object[] args)
        {
            _projectileCollider = (HitColliderBehaviour)GetColliderBehaviourCopy(0);

            //If no spawn transform has been set, use the default owner transform
            if (!ownerMoveset.ProjectileSpawnTransform)
                spawnTransform = owner.transform;
            else
                spawnTransform = ownerMoveset.ProjectileSpawnTransform;

            //Log if a projectile couldn't be found
            if (!_projectile)
                throw new Exception("Projectile for " + abilityData.abilityName + " could not be found.");

            CleanProjectileList();

            if (ActiveProjectiles.Count >= abilityData.GetCustomStatValue("MaxInstances") && abilityData.GetCustomStatValue("MaxInstances") >= 0)
                return;

            Vector2 offSet = new Vector2(1, 0) * -owner.transform.forward;
            offSet.x = Mathf.RoundToInt(offSet.x);
            offSet.y = Mathf.RoundToInt(offSet.y);

            _ownerMoveScript.MoveToPanel(_ownerMoveScript.Position + offSet);
            
            //Create object to spawn laser from
            GameObject spawnerObject = new GameObject();
            spawnerObject.transform.parent = spawnTransform;
            spawnerObject.transform.localPosition = Vector3.up / 2;
            var ownerForward = owner.transform.forward;
            spawnerObject.transform.forward = ownerForward;

            //Initialize and attach spawn script
            ProjectileSpawnerBehaviour spawnScript = spawnerObject.AddComponent<ProjectileSpawnerBehaviour>();
            spawnScript.projectile = _projectile;

            Vector3 launchForce = GridPhysicsBehaviour.CalculatGridForce(_distance, _angle);
            launchForce.x *= ownerForward.x;

            GameObject activeProjectile = spawnScript.FireProjectile(launchForce, _projectileCollider, true);

            GridPhysicsBehaviour gridPhysics = activeProjectile.GetComponent<GridPhysicsBehaviour>();
            gridPhysics.Gravity = _gravity;
            
            //Fire laser
            ActiveProjectiles.Add(activeProjectile);

            Object.Destroy(spawnerObject);
        }
    }
}