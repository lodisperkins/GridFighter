﻿using System.Collections;
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
        public GameObject FireProjectile(Vector3 force)
        {
            if (!projectile)
                return null;

            GameObject temp = Instantiate(projectile, transform.position, new Quaternion(), null);
            Debug.Log(transform.position);
            Rigidbody rigidbody = temp.GetComponent<Rigidbody>();
            if (rigidbody)
                rigidbody.AddForce(force, ForceMode.Impulse);

            return temp;
        }

        /// <summary>
        /// Fires a projectile
        /// </summary>
        /// <param name="force">The amount of force to apply to the projectile</param>
        /// <param name="hitCollider">The hit collider to attach to the projectile</param>
        /// <returns></returns>
        public GameObject FireProjectile(Vector3 force, HitColliderBehaviour hitCollider)
        {
            if (!projectile)
                return null;

            GameObject temp = Instantiate(projectile, transform.position, new Quaternion(), null);

            HitColliderBehaviour collider = (temp.AddComponent<HitColliderBehaviour>());
            HitColliderBehaviour.Copy(hitCollider, collider);

            Rigidbody rigidbody = temp.GetComponent<Rigidbody>();
            if (rigidbody)
                rigidbody.AddForce(force, ForceMode.Impulse);

            return temp;
        }
    }
}


