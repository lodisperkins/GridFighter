using System.Collections;
using System.Collections.Generic;
using Lodis.Gameplay;
using Lodis.Movement;
using UnityEngine;

namespace Lodis.GridScripts
{
    public class CollisionPlaneBehaviour : MonoBehaviour
    {
        [Tooltip("How quick objects bouncing on the plane stop bouncing")]
        [SerializeField]
        private float _bounceDampening = 3.0f;
        [Tooltip("The amount of resistance the plane has against objects sliding over it")]
        [SerializeField]
        private float _friction = 3.0f;
        [Tooltip("The minimum speed needed to make the screen shake when a player falls.")]
        [SerializeField]
        private float _shakeSpeed;
        [SerializeField]
        private float _fallScreenShakeDuration;
        [SerializeField]
        private int _fallScreenShakeFrequency;
        [SerializeField]
        private float _fallScreenShakeStrength;

        public float BounceDampening { get => _bounceDampening; set => _bounceDampening = value; }

        private void OnCollisionEnter(Collision other)
        {
            //Get knock back script to apply force
            Movement.GridPhysicsBehaviour physics = other.transform.GetComponent<Movement.GridPhysicsBehaviour>();
            KnockbackBehaviour knockback = other.transform.GetComponent<KnockbackBehaviour>();

            //Return if the object doesn't have one or is invincible
            if (!physics || !knockback)
                return;

            //Don't add a force if the object is traveling at a low speed
            float dotProduct = Vector3.Dot(physics.LastVelocity, Vector3.up);
            if (physics.LastVelocity.y >= 0 || physics.LastVelocity.magnitude < _shakeSpeed || knockback.CurrentAirState != AirState.TUMBLING)
                return;

            //Calculate and apply friction force
            CameraBehaviour.ShakeBehaviour.ShakeRotation(_fallScreenShakeDuration, _fallScreenShakeStrength, _fallScreenShakeFrequency);
        }

        private void OnCollisionStay(Collision other)
        {
            //Get knock back script to apply force
            Movement.GridPhysicsBehaviour physics = other.transform.root.GetComponent<Movement.GridPhysicsBehaviour>();

            //Return if the object doesn't have one or is invincible
            if (!physics)
                return;

            //Don't add a force if the object is traveling at a low speed
            float dotProduct = Vector3.Dot(physics.LastVelocity, Vector3.up);
            if (dotProduct >= 0 || dotProduct == -1)
                return;

            //Calculate and apply friction force
            Vector3 frictionForce = new Vector3(physics.Mass * physics.LastVelocity.x, 0, 0).normalized * _friction;
            physics.ApplyForce(-frictionForce);
        }
    }
}