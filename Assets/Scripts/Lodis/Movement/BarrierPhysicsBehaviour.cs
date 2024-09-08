using FixedPoints;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Movement
{
    public class BarrierPhysicsBehaviour : GridPhysicsBehaviour
    {
        [Tooltip("How much force will be applied to object standing on top of the barrier to push them off.")]
        [SerializeField]
        private float _pushScale;

        public override void OnHitEnter(Collision collision) 
        {
            //Adds a force to objects to push them off of the field barrier if they land on top
            if (collision.Normal != FVector2.Down)
                base.OnHitEnter(collision);
        }

        public override void OnHitStay(Collision collision)
        {
            Movement.KnockbackBehaviour knockBackScript = collision.OtherCollider.OwnerPhysicsComponent.GetComponent<Movement.KnockbackBehaviour>();
            //Checks if the object is not grid moveable and isn't in hit stun
            if (!knockBackScript)
                return;

            //Adds a force to objects to push them off of the field barrier if they land on top
            if (collision.Normal != FVector2.Down)
            {
                FVector3 pushVelocity = new FVector3(knockBackScript.Physics.Velocity.X, _pushScale, 0);
                knockBackScript.Physics.ApplyImpulseForce(FVector3.Up * knockBackScript.Physics.Gravity / 10);
                knockBackScript.Physics.ApplyForce(pushVelocity);
            }
        }
    }
}
