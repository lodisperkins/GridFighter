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
        private HitColliderData _projectileCollider;
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
            _projectileCollider = GetColliderData(0);

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

            ProjectileSpawnerBehaviour projectileSpawner = OwnerMoveset.ProjectileSpawner;
            projectileSpawner.projectile = _projectile;

            Vector3 launchForce = GridPhysicsBehaviour.CalculatGridForce(_distance, _angle);
            launchForce.x *= projectileSpawner.transform.forward.x;

            GameObject activeProjectile = projectileSpawner.FireProjectile(launchForce, _projectileCollider, true, true);

            GridPhysicsBehaviour gridPhysics = activeProjectile.GetComponent<GridPhysicsBehaviour>();
            gridPhysics.Gravity = _gravity;
            
            //Fire laser
            ActiveProjectiles.Add(activeProjectile);
        }
    }
}