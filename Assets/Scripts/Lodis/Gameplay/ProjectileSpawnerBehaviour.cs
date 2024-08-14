using FixedPoints;
using Lodis.Movement;
using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Lodis.Gameplay
{
    public class ProjectileSpawnerBehaviour : SimulationBehaviour
    {
        public GameObject Projectile = null;
        public EntityData Owner = null;

        public override void Deserialize(BinaryReader br)
        {
        }

        /// <summary>
        /// Fires a projectile
        /// </summary>
        /// <param name="force">The amount of force to apply to the projectile</param>
        /// <returns></returns>
        /// <param name="useGravity"></param>
        public GameObject FireProjectile(Vector3 force, bool useGravity = false)
        {
            if (!Projectile)
                return null;

            GameObject temp = Instantiate(Projectile, transform.position, transform.rotation, null);
            Debug.Log(transform.position);
            Rigidbody rigidbody = temp.GetComponent<Rigidbody>();
            rigidbody.useGravity = useGravity;
            rigidbody.velocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
            if (rigidbody)
                rigidbody.AddForce(force, ForceMode.Impulse);

            return temp;
        }

        /// <summary>
        /// Fires a projectile
        /// </summary>
        /// <param name="forceScale">The amount of force to apply to the projectile</param>
        /// <returns></returns>
        /// <param name="useGravity"></param>
        public GameObject FireProjectile(float forceScale, bool useGravity = false)
        {
            if (!Projectile)
                return null;

            GameObject temp = Instantiate(Projectile, transform.position, transform.rotation, null);
            Debug.Log(transform.position);
            Rigidbody rigidbody = temp.GetComponent<Rigidbody>();
            rigidbody.useGravity = useGravity;
            rigidbody.velocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
            if (rigidbody)
                rigidbody.AddForce(transform.forward * forceScale, ForceMode.Impulse);

            return temp;
        }

        /// <summary>
        /// Adds a physics component, and applies an impulse force to the object
        /// </summary>
        /// <param name="force">The amount of force to apply to the projectile</param>
        /// <param name="hitColliderInfo">The hit collider info to attach to the projectile</param>
        /// <returns></returns>
        /// <param name="useGravity"></param>
        public GameObject FireProjectile(FVector3 force, HitColliderData hitColliderInfo, bool useGravity = false, bool faceHeading = true)
        {
            if (!Projectile)
                return null;

            GameObject temp = ObjectPoolBehaviour.Instance.GetObject(Projectile, transform.position, transform.rotation);

            HitColliderBehaviour collider;
            if (!temp.TryGetComponent(out collider))
                collider = temp.AddComponent<HitColliderBehaviour>();

            collider.ColliderInfo = hitColliderInfo;
            collider.Owner = Owner;

            GridPhysicsBehaviour physics = temp.GetComponent<GridPhysicsBehaviour>();

            if (physics == null)
            {
                physics = temp.AddComponent<GridPhysicsBehaviour>();
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
        public GameObject FireProjectile(float forceScale, HitColliderData hitColliderInfo, bool useGravity = false, bool faceHeading = true)
        {
            if (!Projectile)
                return null;

            GameObject temp = ObjectPoolBehaviour.Instance.GetObject(Projectile, transform.position, transform.rotation);

            HitColliderBehaviour collider;
            if (!temp.TryGetComponent(out collider))
                collider = temp.AddComponent<HitColliderBehaviour>();

            collider.ColliderInfo = hitColliderInfo;
            collider.Owner = Owner;

            GridPhysicsBehaviour physics = temp.GetComponent<GridPhysicsBehaviour>();

            if (physics == null)
            {
                physics = temp.AddComponent<GridPhysicsBehaviour>();
            }

            physics.StopVelocity();

            physics.FaceHeading = faceHeading;

            physics.UseGravity = useGravity;

            physics.ApplyImpulseForce(Owner.Transform.Forward * forceScale);

            return temp;
        }

        public override void Serialize(BinaryWriter bw)
        {
            throw new System.NotImplementedException();
        }
    }
}


