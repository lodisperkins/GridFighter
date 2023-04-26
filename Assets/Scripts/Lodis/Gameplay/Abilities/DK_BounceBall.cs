using Lodis.Movement;
using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Spawns a ball of energy that slowly bounces towards the opponent.
    /// </summary>
    public class DK_BounceBall : ProjectileAbility
    {
        private GameObject _ball;

        //Called when ability is created
        public override void Init(GameObject newOwner)
        {
			base.Init(newOwner);
        }

	    //Called when ability is used
        protected override void OnActivate(params object[] args)
        {
            //Instantiate ball.
            _ball = ObjectPoolBehaviour.Instance.GetObject(abilityData.visualPrefab, OwnerMoveset.ProjectileSpawner.transform.position, abilityData.visualPrefab.transform.rotation);

            //Get the collider to update collision information.
            HitColliderBehaviour hitCollider = _ball.GetComponent<HitColliderBehaviour>();
            hitCollider.ColliderInfo = GetColliderData(0);
            hitCollider.Owner = owner;

            GridPhysicsBehaviour physics = _ball.GetComponent<GridPhysicsBehaviour>();

            //Velocity is stopped to prevent momentum from previous use from carrying over.
            physics.StopVelocity();

            //Calculates the angle and magnitude of the force to be applied.
            float radians = abilityData.GetCustomStatValue("ShotAngle");
            float magnitude = abilityData.GetCustomStatValue("ShotForce");
            Vector3 force = new Vector3(Mathf.Cos(radians), Mathf.Sin(radians)) * magnitude;
            force.x *= OwnerMoveScript.GetAlignmentX();

            physics.ApplyImpulseForce(force);
        }

        public override void StopAbility()
        {
            base.StopAbility();
            ObjectPoolBehaviour.Instance.ReturnGameObject(_ball);
        }
    }
}