using Lodis.Utility;
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
        private float _speedMultiplier;

	    //Called when ability is created
        public override void Init(GameObject newOwner)
        {
            base.Init(newOwner);
            //Load projectile asset
            ProjectileRef = (GameObject)Resources.Load("Projectiles/Prototype/CrossProjectile");
            _speedMultiplier = abilityData.GetCustomStatValue("SpeedMultiplier");
            //Set default hitbox traits
            DestroyOnHit = false;
            IsMultiHit = true;
        }

	    //Called when ability is used
        protected override void Activate(params object[] args)
        {
            //Redirect projectile on hit
            base.Activate(args);
            _reboundCollider = Projectile.AddComponent<ColliderBehaviour>();
            _reboundCollider.AddCollisionEvent(TryRedirectProjectile);
            _reboundCollider.Owner = owner;
        }

        /// <summary>
        /// Checks if the projectile hit a valid object. If so, changes velocity
        /// </summary>
        /// <param name="args"></param>
        public void TryRedirectProjectile(params object[] args)
        {
            GameObject other = (GameObject)args[0];

            //If the projectile rebounded too many times...
            if (_reboundCount >= abilityData.GetCustomStatValue("MaxRebounds"))
                //...destroy it
                ObjectPoolBehaviour.Instance.ReturnGameObject(ActiveProjectiles[0]);

            if (other == owner)
            {
                CharacterStateMachineBehaviour stateMachine = other.GetComponent<CharacterStateMachineBehaviour>();

                if (stateMachine.StateMachine.CurrentState != "Idle" && stateMachine.StateMachine.CurrentState != "Moving")
                    return;
            }

            Rigidbody projectile = ActiveProjectiles[0].GetComponent<Rigidbody>();

            //If it hit a valid object...
            if ((other.CompareTag("Player") || other.CompareTag("Entity")))
            {
                //...reverse velocity
                projectile.AddForce(-projectile.velocity * _speedMultiplier * 2, ForceMode.VelocityChange);
                _reboundCount++;
                _reboundCollider.Owner = other;
            }
            //Otherwise if it hit a structure like a wall...
            else if(other.CompareTag("Structure"))
            {
                //...destroy it
                ObjectPoolBehaviour.Instance.ReturnGameObject(_reboundCollider.gameObject);
            }
        }
    }
}