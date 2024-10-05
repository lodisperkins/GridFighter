using Lodis.Movement;
using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using Types;
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
        private Fixed32 _speedMultiplier;
        private bool _firstCollisionHappened;

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
            _firstCollisionHappened = false;

            //Stores the rebound collider and the hit box attached to this boomerang
            _reboundCollider = Projectile.Data.GetComponentInChildren<ColliderBehaviour>(false);

            //If the boomerang doesn't have a rebound collider...
            if (!_reboundCollider)
            {
                //...log an error.
                Debug.LogError("Can't find rebound collider on boomerang ability for " + Owner.Data.Name);
            }
            //Otherwise...
            else
            {
                //...update the rebound colliders event
                _reboundCollider.ClearAllCollisionEvents();
                _reboundCollider.AddCollisionEvent(TryRedirectProjectile);
                _reboundCollider.Spawner = Owner.Data;
            }

        }

        /// <summary>
        /// Checks if the projectile hit a valid object. If so, changes velocity
        /// </summary>
        /// <param name="args"></param>
        public void TryRedirectProjectile(Collision collision)
        {
            if (!_firstCollisionHappened)
            {
                _firstCollisionHappened = true;
                return;
            }

            GameObject other = collision.OtherEntity.UnityObject;

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

                if (!stateMachine.CompareState("Idle", "Moving", "Attacking"))
                {
                    return;
                }
            }

            GridPhysicsBehaviour projectile = Projectile.Data.GetComponent<GridPhysicsBehaviour>();

            //If it hit a valid object...
            if ((other.CompareTag("Player") || other.CompareTag("Entity")))
            {
                //...reverse velocity
                projectile.ApplyVelocityChange(-projectile.Velocity * _speedMultiplier);
                _reboundCount++;
                _reboundCollider.Spawner = collision.OtherEntity;
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