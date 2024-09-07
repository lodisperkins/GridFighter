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
        private bool _reboundColliderAdded;

        //Called when ability is created
        public override void Init(EntityDataBehaviour newOwner)
        {
            base.Init(newOwner);
        }

        protected override void OnStart(params object[] args)
        {
            base.OnStart(args);
            _speedMultiplier = abilityData.GetCustomStatValue("SpeedMultiplier");
            //Set default hitbox traits
            DestroyOnHit = false;
            IsMultiHit = true;
        }


        //Called when ability is used
        protected override void OnActivate(params object[] args)
        {
            base.OnActivate(args);
            _reboundCount = 0;

            //Stores the rebound collider and the hit box attached to this boomerang
            ColliderBehaviour[] colliderBehaviours = Projectile.GetComponents<ColliderBehaviour>();

            //If the boomerang only has a hit collider...
            if (colliderBehaviours.Length == 1)
            {
                //...add a rebound collider that can reflect the boomerang on hit
                _reboundCollider = Projectile.gameObject.AddComponent<ColliderBehaviour>();
                _reboundCollider.AddCollisionEvent(TryRedirectProjectile);
            }
            //Otherwise...
            else
            {
                //...update the rebound colliders event
                _reboundCollider = colliderBehaviours[1];
                _reboundCollider.ClearAllCollisionEvents();
                _reboundCollider.AddCollisionEvent(TryRedirectProjectile);
            }

            _reboundCollider.Spawner = Owner.Data;
        }

        /// <summary>
        /// Checks if the projectile hit a valid object. If so, changes velocity
        /// </summary>
        /// <param name="args"></param>
        public void TryRedirectProjectile(Collision collision)
        {
            GameObject other = collision.Entity.UnityObject;

            //If the projectile rebounded too many times...
            if (_reboundCount >= abilityData.GetCustomStatValue("MaxRebounds"))
            {   
                //...destroy it
                ObjectPoolBehaviour.Instance.ReturnGameObject(Projectile);
                _reboundCount = 0;
                return;
            }

            //Don't redirect the projectile if the player isn't standing still or just moving
            if (other == Owner)
            {
                CharacterStateMachineBehaviour stateMachine = other.GetComponent<CharacterStateMachineBehaviour>();

                if (stateMachine.StateMachine.CurrentState != "Idle" && stateMachine.StateMachine.CurrentState != "Moving" && stateMachine.StateMachine.CurrentState != "Attacking")
                {
                    return;
                }
            }

            Rigidbody projectile = Projectile.GetComponent<Rigidbody>();

            //If it hit a valid object...
            if ((other.CompareTag("Player") || other.CompareTag("Entity")))
            {
                //...reverse velocity
                projectile.AddForce(-projectile.velocity * _speedMultiplier * 2, ForceMode.VelocityChange);
                _reboundCount++;
                _reboundCollider.Spawner = collision.Entity;
            }
            //Otherwise if it hit a structure like a wall...
            else if(other.CompareTag("Structure"))
            {
                //...destroy it
                ObjectPoolBehaviour.Instance.ReturnGameObject(Projectile);
                _reboundCount = 0;
            }
        }
    }
}