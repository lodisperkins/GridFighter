
using FixedPoints;
using Lodis.Movement;
using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using Types;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Strike the ground to send a 
    ///shockwave that travels up to
    ///5 panels away.Enemies caught
    ///in the shockwave will be launched upwards.
    /// </summary>
    public class DK_GroundPound : ProjectileAbility
    {
        private KnockbackBehaviour _knockBackBehaviour;
        private GameObject _explosionEffect;

        //Called when ability is created
        public override void Init(EntityDataBehaviour newOwner)
        {
			base.Init(newOwner);
            _explosionEffect = (GameObject)Resources.Load("Effects/MediumExplosion");
            _knockBackBehaviour = Owner.GetComponent<KnockbackBehaviour>();
        }

        protected override void OnStart(params object[] args)
        {
            base.OnStart();
            _knockBackBehaviour.Physics.Jump(2, 0, abilityData.startUpTime);

            //Disable movement to prevent the ability being interrupted
            OwnerMoveScript.DisableMovement(condition => CurrentAbilityPhase == AbilityPhase.RECOVER || !InUse, false, true);
        }

        //Called when ability is used
        protected override void OnActivate(params object[] args)
        {
            //Spawn the explosion effect.
            Object.Instantiate(_explosionEffect, OwnerMoveScript.CurrentPanel.transform.position, Camera.main.transform.rotation);

            //Fire a projectile in front.
            base.OnActivate(args);


            //Fire a projectile in back.

            ShotDirection = -OwnerMoveScript.FixedTransform.Forward;

            //Log if a projectile couldn't be found
            if (!ProjectileRef)
            {
                Debug.LogError("Projectile for " + abilityData.abilityName + " could not be found.");
                return;
            }

            ProjectileSpawnerBehaviour projectileSpawner = OwnerMoveset.ProjectileSpawner;
            projectileSpawner.Projectile = ProjectileRef;
            SpawnTransform = projectileSpawner.FixedTransform;

            HitColliderData data = GetColliderData(1);
            if (ScaleStats)
                data = ProjectileColliderData.ScaleStats((Fixed32)args[0]);

            Projectile = projectileSpawner.FireProjectile(ShotDirection * abilityData.GetCustomStatValue("Speed"), data, UseGravity, FaceHeading);

            //Fire projectile
            Projectile.name += "(" + abilityData.name + ")";
            ActiveProjectiles.Add(Projectile);

            CameraBehaviour.ShakeBehaviour.ShakeRotation();
        }
    }
}