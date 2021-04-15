using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{
    public class ProjectileSpawnerBehaviour : MonoBehaviour
    {
        public GameObject projectile = null;

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

        public GameObject FireProjectile(Vector3 force, HitColliderBehaviour hitCollider)
        {
            if (!projectile)
                return null;

            GameObject temp = Instantiate(projectile, transform.position, new Quaternion(), null);

            HitColliderBehaviour collider = (temp.AddComponent<HitColliderBehaviour>());
            HitColliderBehaviour.Copy(hitCollider, collider);

            Debug.Log(transform.position);

            Rigidbody rigidbody = temp.GetComponent<Rigidbody>();
            if (rigidbody)
                rigidbody.AddForce(force, ForceMode.Impulse);

            return temp;
        }
    }
}


