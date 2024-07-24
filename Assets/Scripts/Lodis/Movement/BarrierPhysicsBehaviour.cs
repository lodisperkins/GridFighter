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

        protected override void OnCollisionEnter(Collision collision) 
        {
            //Calculate the knockback and hit angle for the ricochet
            ContactPoint contactPoint = collision.GetContact(0);

            //Adds a force to objects to push them off of the field barrier if they land on top
            if (contactPoint.normal != Vector3.down)
                base.OnCollisionEnter(collision);
        }

        private void OnCollisionStay(Collision collision)
        {
            Movement.KnockbackBehaviour knockBackScript = collision.gameObject.GetComponent<Movement.KnockbackBehaviour>();
            //Checks if the object is not grid moveable and isn't in hit stun
            if (!knockBackScript)
                return;

            //Calculate the knockback and hit angle for the ricochet
            ContactPoint contactPoint = collision.GetContact(0);

            //Adds a force to objects to push them off of the field barrier if they land on top
            if (contactPoint.normal == Vector3.down)
            {
                Vector3 pushVelocity = new Vector3(knockBackScript.Physics.LastVelocity.X, _pushScale, 0);
                knockBackScript.Physics.ApplyImpulseForce(Vector3.up * knockBackScript.Physics.Gravity / 10);
                knockBackScript.Physics.ApplyForce(pushVelocity);
            }
        }
    }
}
