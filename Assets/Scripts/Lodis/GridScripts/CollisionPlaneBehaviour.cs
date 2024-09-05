using System.Collections;
using System.Collections.Generic;
using System.IO;
using FixedPoints;
using Lodis.Gameplay;
using Lodis.Movement;
using Lodis.Sound;
using Lodis.Utility;
using UnityEngine;

namespace Lodis.GridScripts
{
    public class CollisionPlaneBehaviour : SimulationBehaviour
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
        [SerializeField]
        private ParticleSystem _groundDustParticlesRef;
        [SerializeField]
        private ParticleSystem _debris;
        [SerializeField]
        private AudioClip _softLandingClip;
        [SerializeField]
        private AudioClip _hardLandingClip;

        private GameObject _groundDustParticles;

        public float BounceDampening { get => _bounceDampening; set => _bounceDampening = value; }

        public override void Deserialize(BinaryReader br)
        {
            throw new System.NotImplementedException();
        }

        public override void Serialize(BinaryWriter bw)
        {
            throw new System.NotImplementedException();
        }

        public override void OnHitEnter(Collision other)
        {
            //Get knock back script to apply force
            Movement.GridPhysicsBehaviour physics = other.Collider.OwnerPhysicsComponent;
            KnockbackBehaviour knockback = other.Entity.GetComponent<KnockbackBehaviour>();

            //Return if the object doesn't have one or is invincible
            if (!physics || !knockback)
                return;

            //Don't add a force if the object is traveling at a low speed
            float dotProduct = FVector3.Dot(physics.Velocity, FVector3.Up);
            if (physics.Velocity.Y >= 0)
                return;

            FVector3 particleSpawnPosition = new FVector3(other.Entity.Transform.WorldPosition.X, 0, other.Entity.Transform.WorldPosition.Z);
            if (knockback.CurrentAirState != AirState.TUMBLING)
                SoundManagerBehaviour.Instance.PlaySound(_softLandingClip, 0.8f);
            else
            {
                //physics.RB.isKinematic = false;

                SoundManagerBehaviour.Instance.PlaySound(_hardLandingClip, 0.8f);
                if (physics.Velocity.Magnitude >= _shakeSpeed)
                {
                    CameraBehaviour.ShakeBehaviour.ShakeRotation(_fallScreenShakeDuration, _fallScreenShakeStrength, _fallScreenShakeFrequency);
                    ObjectPoolBehaviour.Instance.GetObject(_debris.gameObject, (Vector3)particleSpawnPosition, Camera.main.transform.rotation);
                }
            }


            _groundDustParticles = ObjectPoolBehaviour.Instance.GetObject(_groundDustParticlesRef.gameObject, (Vector3)particleSpawnPosition, Camera.main.transform.rotation);
            ObjectPoolBehaviour.Instance.ReturnGameObject(_groundDustParticles, _groundDustParticlesRef.main.duration);

        }

        public override void OnHitStay(Collision other)
        {
            //Get knock back script to apply force
            Movement.GridPhysicsBehaviour physics = other.Collider.OwnerPhysicsComponent;

            //Return if the object doesn't have one or is invincible
            if (!physics)
                return;


            //Don't add a force if the object is traveling at a low speed
            float dotProduct = FVector3.Dot(physics.Velocity, FVector3.Up);
            if (dotProduct >= 0 || dotProduct == -1)
                return;

            if (physics.Velocity.X == 0)
                return;

            //Calculate and apply friction force
            physics.ApplyForce(_friction * (physics.Velocity.X / Mathf.Abs(physics.Velocity.X) * FVector3.Right));
        }
    }
}