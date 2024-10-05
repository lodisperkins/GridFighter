using FixedPoints;
using Lodis.Movement;
using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Types;
using UnityEngine;
using UnityEngine.InputSystem.Utilities;

namespace Lodis.Gameplay
{
    public class ProjectileSpawnerBehaviour : SimulationBehaviour
    {
        public EntityDataBehaviour Projectile = null;
        public EntityDataBehaviour Owner = null;

        public override void Deserialize(BinaryReader br)
        {
        }

        /// <summary>
        /// Fires a projectile
        /// </summary>
        /// <param name="force">The amount of force to apply to the projectile</param>
        /// <returns></returns>
        /// <param name="useGravity"></param>
        public EntityDataBehaviour FireProjectile(FVector3 force, bool useGravity = false)
        {
            if (!Projectile)
                return null;
            
            EntityDataBehaviour temp = ObjectPoolBehaviour.Instance.GetObject(Projectile, FixedTransform.WorldPosition, FixedTransform.WorldRotation);

            GridPhysicsBehaviour rigidbody = temp.GetComponent<GridPhysicsBehaviour>();
            rigidbody.UseGravity = useGravity;
            rigidbody.Velocity = FVector3.Zero;

            if (rigidbody)
                rigidbody.ApplyImpulseForce(force);

            return temp;
        }

        /// <summary>
        /// Fires a projectile
        /// </summary>
        /// <param name="forceScale">The amount of force to apply to the projectile</param>
        /// <returns></returns>
        /// <param name="useGravity"></param>
        public EntityDataBehaviour FireProjectile(Fixed32 forceScale, bool useGravity = false)
        {
            if (!Projectile)
                return null;

            EntityDataBehaviour temp = ObjectPoolBehaviour.Instance.GetObject(Projectile, FixedTransform.WorldPosition, FixedTransform.WorldRotation);

            GridPhysicsBehaviour rigidbody = temp.GetComponent<GridPhysicsBehaviour>();
            rigidbody.UseGravity = useGravity;
            rigidbody.Velocity = FVector3.Zero;

            if (rigidbody)
                rigidbody.ApplyImpulseForce(FixedTransform.Forward * forceScale);

            return temp;
        }

        /// <summary>
        /// Adds a physics component, and applies an impulse force to the object
        /// </summary>
        /// <param name="force">The amount of force to apply to the projectile</param>
        /// <param name="hitColliderInfo">The hit collider info to attach to the projectile</param>
        /// <returns></returns>
        /// <param name="useGravity"></param>
        public EntityDataBehaviour FireProjectile(FVector3 force, HitColliderData hitColliderInfo, bool useGravity = false, bool faceHeading = true)
        {
            if (!Projectile)
                return null;

            EntityDataBehaviour temp = ObjectPoolBehaviour.Instance.GetObject(Projectile, FixedTransform.WorldPosition, FixedTransform.WorldRotation);

            if (!temp.TryGetComponent(out HitColliderBehaviour collider))
                collider = temp.gameObject.AddComponent<HitColliderBehaviour>();

            collider.ColliderInfo = hitColliderInfo;
            collider.Spawner = Owner;

            
            if (!temp.TryGetComponent<GridPhysicsBehaviour>(out var physics))
            {
                physics = temp.gameObject.AddComponent<GridPhysicsBehaviour>();
            }

            physics.StopVelocity();

            physics.FaceHeading = faceHeading;

            physics.UseGravity = useGravity;

            physics.ApplyImpulseForce(force);

            return temp;
        }

        /// <summary>
        /// Adds a physics component, and applies an impulse force to the object
        /// </summary>
        /// <param name="forceScale">The amount of force to apply to the projectile</param>
        /// <param name="hitColliderInfo">The hit collider info to attach to the projectile</param>
        /// <returns></returns>
        /// <param name="useGravity"></param>
        public EntityDataBehaviour FireProjectile(float forceScale, HitColliderData hitColliderInfo, bool useGravity = false, bool faceHeading = true)
        {
            if (!Projectile)
                return null;

            EntityDataBehaviour temp = ObjectPoolBehaviour.Instance.GetObject(Projectile, FixedTransform.WorldPosition, FixedTransform.WorldRotation);

            
            if (!temp.gameObject.TryGetComponent<GridPhysicsBehaviour>(out var physics))
            {
                physics = temp.gameObject.AddComponent<GridPhysicsBehaviour>();
            }

            if (!temp.gameObject.TryGetComponent(out HitColliderBehaviour collider))
                collider = temp.gameObject.AddComponent<HitColliderBehaviour>();

            collider.ColliderInfo = hitColliderInfo;
            collider.Spawner = Owner;


            physics.StopVelocity();

            physics.FaceHeading = faceHeading;

            physics.UseGravity = useGravity;

            physics.GridActive = false;

            physics.ApplyImpulseForce(FixedTransform.Forward * forceScale);

            return temp;
        }

        public override void Serialize(BinaryWriter bw)
        {
            throw new System.NotImplementedException();
        }
    }
}


