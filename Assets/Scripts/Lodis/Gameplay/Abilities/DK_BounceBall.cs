using FixedPoints;
using Lodis.Movement;
using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using Types;
using UnityEngine;
using UnityEngine.InputSystem.Utilities;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Spawns a ball of energy that slowly bounces towards the opponent.
    /// </summary>
    public class DK_BounceBall : ProjectileAbility
    {
        private int _ballCount;
        private Fixed32 _ballSpawnDelay;
        private FixedTimeAction _spawnRoutine;
        private List<EntityDataBehaviour> _balls = new List<EntityDataBehaviour>();
        private GridPhysicsBehaviour.BounceForce _ballBounce;

        //Called when ability is created
        public override void Init(EntityDataBehaviour newOwner)
        {
			base.Init(newOwner);

            _ballCount = (int)abilityData.GetCustomStatValue("BallCount");
            _ballSpawnDelay = abilityData.GetCustomStatValue("BallSpawnDelay");

            //Calculates the angle and magnitude of the force to be applied.
            Fixed32 radians = abilityData.GetCustomStatValue("ShotAngle");
            Fixed32 magnitude = abilityData.GetCustomStatValue("ShotForce");
            FVector3 force = new FVector3(Fixed32.Cos(radians), Fixed32.Sin(radians), 0) * magnitude;
            force.X *= OwnerMoveScript.GetAlignmentX();
            _ballBounce = new GridPhysicsBehaviour.BounceForce(-1, force);
        }

	    //Called when ability is used
        protected override void OnActivate(params object[] args)
        {
            SpawnBall();
            _spawnRoutine = FixedPointTimer.StartNewTimedAction(SpawnBall, _ballSpawnDelay).Loop(_ballCount - 2);
        }

        private void SpawnBall()
        {
            EntityDataBehaviour ballInstance = ObjectPoolBehaviour.Instance.GetObject(abilityData.visualPrefab.GetComponent<EntityDataBehaviour>(), OwnerMoveset.ProjectileSpawner.FixedTransform.WorldPosition, Owner.FixedTransform.WorldRotation);
            ballInstance.FixedTransform.WorldPosition += FVector3.Right * OwnerMoveScript.GetAlignmentX();
            //Get the collider to update collision information.
            HitColliderBehaviour hitCollider = ballInstance.GetComponent<HitColliderBehaviour>();
            hitCollider.ColliderInfo = GetColliderData(0);
            hitCollider.Spawner = Owner.Data;

            GridPhysicsBehaviour physics = ballInstance.GetComponent<GridPhysicsBehaviour>();

            //Velocity is stopped to prevent momentum from previous use from carrying over.
            physics.StopVelocity();

            
            physics.SetBounceForce(_ballBounce);

            _balls.Add(ballInstance);
        }

        protected override void OnEnd()
        {
            base.OnEnd();
        }

        protected override void OnMatchRestart()
        {
            _spawnRoutine?.Stop();

            foreach (EntityDataBehaviour ball in _balls)
            {
                ObjectPoolBehaviour.Instance.ReturnGameObject(ball);
            }
        }
    }
}