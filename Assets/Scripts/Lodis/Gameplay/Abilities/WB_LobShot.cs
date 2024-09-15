using System;
using FixedPoints;
using Lodis.Movement;
using Types;
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
        private EntityDataBehaviour _projectile;
        //The collider attached to the laser
        private HitColliderData _projectileCollider;
        private float _distance;
        private float _angle;
        private float _gravity;

        //Called when ability is created
        public override void Init(EntityDataBehaviour newOwner)
        {
			base.Init(newOwner);

            //initialize default stats
            abilityData = (ScriptableObjects.AbilityData)(Resources.Load("AbilityData/WB_LobShot_Data"));
            Owner = newOwner;
            _distance = abilityData.GetCustomStatValue("PanelDistance");
            _angle = abilityData.GetCustomStatValue("Angle");
            _gravity = abilityData.GetCustomStatValue("Gravity");
            
            //Load the projectile prefab
            _projectile = abilityData.visualPrefab.GetComponent<EntityDataBehaviour>();
        }

        //Called when ability is used
        protected override void OnActivate(params object[] args)
        {
            _projectileCollider = GetColliderData(0);

            //Log if a projectile couldn't be found
            if (!_projectile)
                throw new Exception("Projectile for " + abilityData.abilityName + " could not be found.");

            CleanProjectileList();

            //Exit if too many lobshots are active
            if (ActiveProjectiles.Count >= abilityData.GetCustomStatValue("MaxInstances") && abilityData.GetCustomStatValue("MaxInstances") >= 0)
                return;

            FireProjectile();
        }

        private void FireProjectile()
        {
            //Create a "gun" to fire the shot from
            ProjectileSpawnerBehaviour projectileSpawner = OwnerMoveset.ProjectileSpawner;
            projectileSpawner.Projectile = _projectile;

            //Calculate the force needed to make the lobshot travel the given distance
            FVector3 launchForce = GridPhysicsBehaviour.CalculatGridForce(_distance, _angle, new Fixed32(642908), new Fixed32(65536));
            launchForce.X *= projectileSpawner.transform.forward.x;

            //Store the gravity of the lobshot for to change its falling speed
            EntityDataBehaviour activeProjectile = projectileSpawner.FireProjectile(launchForce, _projectileCollider, true, true);
            GridPhysicsBehaviour gridPhysics = activeProjectile.GetComponent<GridPhysicsBehaviour>();
            gridPhysics.Gravity = _gravity;

            ActiveProjectiles.Add(activeProjectile);
        }
    }
}