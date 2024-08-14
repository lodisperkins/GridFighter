using FixedPoints;
using Lodis.Movement;
using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using Types;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Spawns a ball of energy that slowly bounces towards the opponent.
    /// </summary>
    public class DK_BounceBall : ProjectileAbility
    {
        private int _ballCount;
        private float _ballSpawnDelay;
        private Coroutine _spawnRoutine;
        List<GameObject> _balls = new List<GameObject>();

        //Called when ability is created
        public override void Init(EntityDataBehaviour newOwner)
        {
			base.Init(Owner);

            _ballCount = (int)abilityData.GetCustomStatValue("BallCount");
            _ballSpawnDelay = abilityData.GetCustomStatValue("BallSpawnDelay");
        }

	    //Called when ability is used
        protected override void OnActivate(params object[] args)
        {
            _spawnRoutine = OwnerMoveset.StartCoroutine(SpawnBalls());
        }

        private IEnumerator SpawnBalls()
        {
            GameObject ballInstance = null;

            for (int i = 0; i < _ballCount; i++)
            {
                //Instantiate ball.
                ballInstance = ObjectPoolBehaviour.Instance.GetObject(abilityData.visualPrefab, OwnerMoveset.ProjectileSpawner.transform.position, abilityData.visualPrefab.transform.rotation);

                //Get the collider to update collision information.
                HitColliderBehaviour hitCollider = ballInstance.GetComponent<HitColliderBehaviour>();
                hitCollider.ColliderInfo = GetColliderData(0);
                hitCollider.Owner = Owner.Data;

                GridPhysicsBehaviour physics = ballInstance.GetComponent<GridPhysicsBehaviour>();

                //Velocity is stopped to prevent momentum from previous use from carrying over.
                physics.StopVelocity();

                //Calculates the angle and magnitude of the force to be applied.
                float radians = abilityData.GetCustomStatValue("ShotAngle");
                float magnitude = abilityData.GetCustomStatValue("ShotForce");
                FVector3 force = new FVector3(Fixed32.Cos(radians), Fixed32.Sin(radians), 0) * magnitude;
                force.X *= OwnerMoveScript.GetAlignmentX();

                physics.ApplyImpulseForce(force);

                _balls.Add(ballInstance);

                yield return new WaitForSeconds(_ballSpawnDelay);
            }
        }

        protected override void OnEnd()
        {
            base.OnEnd();
            OwnerMoveset.StopCoroutine(SpawnBalls());
        }

        protected override void OnMatchRestart()
        {
            OwnerMoveset.StopCoroutine(SpawnBalls());

            foreach(GameObject ball in _balls)
            {
                ObjectPoolBehaviour.Instance.ReturnGameObject(ball);
            }
        }
    }
}