using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{

    /// <summary>
    /// Throw a projectile that travels
    /// in the opposite direction when it hits something.
    /// Catch it on the way back to throw it again.
    /// </summary>
    public class DK_Boomerang : ProjectileAbility
    {
        private Vector3 _originalTravelDirection;
        private int _reboundCount;
        private ColliderBehaviour _reboundCollider;

	    //Called when ability is created
        public override void Init(GameObject newOwner)
        {
            base.Init(newOwner);
            //Load projectile asset
            projectile = (GameObject)Resources.Load("Projectiles/CrossProjectile");
            //Set default hitbox traits
            DestroyOnHit = false;
            IsMultiHit = true;
        }

	    //Called when ability is used
        protected override void Activate(params object[] args)
        {
            base.Activate(args);
            //Set the amount of frames the projectile will register a hit
            projectileCollider.HitFrames = 1;
            //Create a new collider that will handle reversing velocity
            _reboundCollider = _activeProjectiles[0].AddComponent<ColliderBehaviour>();

            //Initialize rebound collider
            _reboundCollider.Init(false, 0, owner, false, true);
            //Redirect projectile on hit
            _reboundCollider.OnHit += TryRedirectProjectile;
        }

        /// <summary>
        /// Checks if the projectile hit a valid object. If so, changes velocity
        /// </summary>
        /// <param name="args"></param>
        public void TryRedirectProjectile(params object[] args)
        {
            GameObject other = (GameObject)args[0];

            //If the projectile rebounded too many times...
            if (_reboundCount == abilityData.GetCustomStatValue("MaxRebounds"))
                //...destroy it
                MonoBehaviour.Destroy(_activeProjectiles[0]);

            Rigidbody projectile = _activeProjectiles[0].GetComponent<Rigidbody>();

            //If it hit a valid object...
            if ((other.CompareTag("Player") || other.CompareTag("Entity")))
            {
                //...reverse velocity
                projectile.AddForce(-projectile.velocity * 2, ForceMode.VelocityChange);
                _reboundCount++;
                _reboundCollider.ColliderOwner = other;
            }
            //Otherwise if it hit a structure like a wall...
            else if(other.CompareTag("Structure"))
            {
                //...destroy it
                MonoBehaviour.Destroy(_reboundCollider.gameObject);
            }
        }
    }
}