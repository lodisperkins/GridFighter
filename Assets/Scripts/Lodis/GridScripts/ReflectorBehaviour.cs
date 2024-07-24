using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lodis.Movement;
using FixedPoints;

namespace Lodis.Gameplay
{
    [RequireComponent(typeof(ColliderBehaviour))]
    [RequireComponent(typeof(Rigidbody))]
    public class ReflectorBehaviour : MonoBehaviour
    {
        private ColliderBehaviour _collider;
        [Tooltip("How long are opponents stunned after their melee attacks are blocked")]
        [SerializeField]
        private float _attackerStunTime;
        [Tooltip("If true, projectiles will have the lifetime timer reset when reflect")]
        [SerializeField]
        private bool _resetProjectileTimer;

        private void Awake()
        {
            _collider = GetComponent<ColliderBehaviour>();
            _collider.AddCollisionEvent(OnCollision);

            gameObject.tag = "Reflector";
        }

        /// <summary>
        /// Checks if the object it collided with is an enemy projectile.
        /// If so, reverses velocity
        /// </summary>
        /// <param name="gameObject"></param>
        public void TryReflectProjectile(HitColliderBehaviour otherCollider)
        {
            GridPhysicsBehaviour gridPhysics = otherCollider.transform.root.GetComponent<GridPhysicsBehaviour>();

            //Only reflect if this object has physics
            if (!gridPhysics) return;
            
            //Don't reflect if this is the owner's projectile
            if (otherCollider.Owner == _collider.gameObject)
                return;

            //Change the projectiles owner and velocity
            otherCollider.Owner = _collider.Owner;
            otherCollider.ColliderInfo.OwnerAlignement = _collider.Owner.GetComponent<GridMovementBehaviour>().Alignment;
            otherCollider.ResetActiveTime();
            gridPhysics.ApplyVelocityChange((Vector3)(-gridPhysics.LastVelocity * 2f));
        }

        /// <summary>
        /// Stuns the attacker if there is a health script attached to the parent.
        /// </summary>
        /// <param name="other">The object that attacked this reflector</param>
        public void TryStunAttacker(GameObject other)
        {
            //If the object is the owner of this collider return
            if (other == _collider.Owner) return;

            //Only stun if this object has a health component and isn't already stunned
            HealthBehaviour healthBehaviour = other.GetComponentInChildren<HealthBehaviour>();
            if (!healthBehaviour)
                return;
            if (healthBehaviour.Stunned) return;

            //Call stun
            healthBehaviour.Stun(_attackerStunTime);
        }

        private void OnCollision(params object[] args)
        {
            GameObject other = (GameObject)args[0];
            HitColliderBehaviour hitCollider = args[1] as HitColliderBehaviour;

            if (!hitCollider || !CompareTag("Reflector")) return;

            //If the hitbox is attached to a character stun them
            if (other.transform.root.CompareTag("Player") || other.transform.root.CompareTag("Entity"))
            {
                TryStunAttacker(other.transform.root.gameObject);
                return;
            }

            //If its not attached to a character treat it as a projectile
            TryReflectProjectile(hitCollider);
        }
    }
}
