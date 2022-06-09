using Lodis.Movement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{
    public class ProjectileSpawnerBehaviour : MonoBehaviour
    {
        public GameObject projectile = null;

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

            GameObject temp = Instantiate(projectile, transform.position, new Quaternion(), null);
            Debug.Log(transform.position);
            Rigidbody rigidbody = temp.GetComponent<Rigidbody>();
            rigidbody.useGravity = useGravity;
            if (rigidbody)
                rigidbody.AddForce(force, ForceMode.Impulse);

            return temp;
        }

        /// <summary>
        /// Adds a physics component, and applies an impulse force to the object
        /// </summary>
        /// <param name="force">The amount of force to apply to the projectile</param>
        /// <param name="hitCollider">The hit collider to attach to the projectile</param>
        /// <returns></returns>
        /// <param name="useGravity"></param>
        public GameObject FireProjectile(Vector3 force, HitColliderBehaviour hitCollider, bool useGravity = false)
        {
            if (!projectile)
                return null;

            GameObject temp = Instantiate(projectile, transform.position, new Quaternion(), null);

            HitColliderBehaviour collider = (temp.AddComponent<HitColliderBehaviour>());
            HitColliderBehaviour.Copy(hitCollider, collider);

            GridPhysicsBehaviour physics = temp.GetComponent<GridPhysicsBehaviour>();

            if (physics == null)
            {
                physics = temp.AddComponent<GridPhysicsBehaviour>();
                physics.MovementBehaviour.enabled = false;
            }

            physics.UseGravity = useGravity;

            physics.ApplyImpulseForce(force);

            return temp;
        }
    }
}


