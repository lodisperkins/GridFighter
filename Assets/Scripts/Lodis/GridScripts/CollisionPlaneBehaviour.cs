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

        public float BounceDampening { get => _bounceDampening; set => _bounceDampening = value; }

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