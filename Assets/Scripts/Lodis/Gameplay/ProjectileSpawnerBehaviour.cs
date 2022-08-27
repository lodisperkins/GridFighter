using Lodis.Movement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{
    public class ProjectileSpawnerBehaviour : MonoBehaviour
    {
        public GameObject projectile = null;
        public GameObject Owner = null;

        /// <summary>
        /// Fires a projectile
        /// </summary>
        /// <param name="force">The amount of force to apply to the projectile</param>
        /// <returns></returns>
        /// <param name="useGravity"></param>
        public GameObject FireProjectile(Vector3 force, bool useGravity = false)
        {
            if (!projectile)
                return null;

            GameObject temp = Instantiate(projectile, transform.position, transform.rotation, null);
            Debug.Log(transform.position);
            Rigidbody rigidbody = temp.GetComponent<Rigidbody>();
            rigidbody.useGravity = useGravity;
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
            if (!projectile)
                return null;

            GameObject temp = Instantiate(projectile, transform.position, transform.rotation, null);
            Debug.Log(transform.position);
            Rigidbody rigidbody = temp.GetComponent<Rigidbody>();
            rigidbody.useGravity = useGravity;
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
        public GameObject FireProjectile(Vector3 force, HitColliderData hitColliderInfo, bool useGravity = false, bool faceHeading = true)
        {
            if (!projectile)
                return null;

            GameObject temp = Instantiate(projectile, transform.position, transform.rotation, null);

            HitColliderBehaviour collider = (temp.AddComponent<HitColliderBehaviour>());
            collider.ColliderInfo = hitColliderInfo;
            collider.Owner = Owner;

            GridPhysicsBehaviour physics = temp.GetComponent<GridPhysicsBehaviour>();

            if (physics == null)
            {
                physics = temp.AddComponent<GridPhysicsBehaviour>();
            }

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
            if (!projectile)
                return null;

            GameObject temp = Instantiate(projectile, transform.position, transform.rotation, null);

            HitColliderBehaviour collider = (temp.AddComponent<HitColliderBehaviour>());
            collider.ColliderInfo = hitColliderInfo;
            collider.Owner = Owner;

            GridPhysicsBehaviour physics = temp.GetComponent<GridPhysicsBehaviour>();

            if (physics == null)
            {
                physics = temp.AddComponent<GridPhysicsBehaviour>();
            }

            physics.FaceHeading = faceHeading;

            physics.UseGravity = useGravity;

            physics.ApplyImpulseForce(transform.forward * forceScale);

            return temp;
        }
    }
}


