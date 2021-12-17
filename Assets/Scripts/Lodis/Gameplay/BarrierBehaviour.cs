using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lodis.Utility;

namespace Lodis.Gameplay
{
    public class BarrierBehaviour : HealthBehaviour
    {
        private Material _material;
        [Tooltip("All layers that will be visible if placed behind this barrier")]
        [SerializeField]
        private LayerMask _visibleLayers;
        private float _rangeToIgnoreUpAngle;
        [Tooltip("The name of the gameobject that owns this barrier")]
        [SerializeField]
        private string _owner = "";
        [Tooltip("How much force will be applied to object standing on top of the barrier to push them off.")]
        [SerializeField]
        private float _pushScale;
        [Tooltip("The amount of damage to deal to objects that collide with the barrier.")]
        [SerializeField]
        private float _damageOnCollision;
        [SerializeField]
        private float _bounceScale;
        [Tooltip("The length of the hit stun applied to the objects that are knocked into this barrier.")]
        [SerializeField]
        private float _hitStunOnCollision;

        public string Owner { get => _owner; set => _owner = value; }

        // Start is called before the first frame update
        void Start()
        {
            _material = GetComponent<Renderer>().material;
            _movement = GetComponent<Movement.GridMovementBehaviour>();
        }

        /// <summary>
        /// Inherited from health behaviour.
        /// Barriers only take damage from  owners if the type is knock back damage.
        /// </summary>
        /// <param name="attacker">The name of the object that is attacking</param>
        /// <param name="damage">The amount of damage this attack would do. Ignored if damage type isn't knock back</param>
        /// <param name="knockBackScale">How far this object will be knocked back. Ignored for barriers</param>
        /// <param name="hitAngle">The angle to launch this object. Ignore for barriers</param>
        /// <param name="damageType">The type of damage being received</param>
        /// <returns>The amount of damage taken. Returns 0 if the attacker was the owner and if the type wasn't knock back </returns>
        public override float TakeDamage(string attacker, float damage, float knockBackScale = 0, float hitAngle = 0, DamageType damageType = DamageType.DEFAULT, float hitStun = 0)
        {
            if (attacker == Owner && damageType == DamageType.KNOCKBACK || attacker != Owner && damageType != DamageType.KNOCKBACK)
                return base.TakeDamage(attacker, damage, knockBackScale, hitAngle, damageType);
            else if (Owner == "")
                return base.TakeDamage(attacker, damage, knockBackScale, hitAngle, damageType);

            return 0;
        }

        public override void OnCollisionEnter(Collision collision)
        {
            Movement.KnockbackBehaviour knockBackScript = collision.gameObject.GetComponent<Movement.KnockbackBehaviour>();
            //Checks if the object is not grid moveable and isn't in hit stun
            if (!knockBackScript)
                return;

            //Calculate the knockback and hit angle for the ricochet
            ContactPoint contactPoint = collision.GetContact(0);

            Vector3 direction = new Vector3(contactPoint.normal.x, contactPoint.normal.y, 0);
            float dotProduct = Vector3.Dot(Vector3.right, -direction);
            float hitAngle = Mathf.Acos(dotProduct);
            float velocityMagnitude = knockBackScript.Physics.LastVelocity.magnitude;
            float knockbackScale = knockBackScript.CurrentKnockBackScale * (velocityMagnitude / knockBackScript.LaunchVelocity.magnitude);

            if (knockbackScale == 0 || float.IsNaN(knockbackScale) || !knockBackScript.Tumbling)
                return;

            //Apply ricochet force and damage
            knockBackScript.TakeDamage(name, _damageOnCollision, 0, 0, DamageType.KNOCKBACK, _hitStunOnCollision);
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
                knockBackScript.Physics.ApplyForce(Vector3.up * knockBackScript.Physics.Gravity);
                knockBackScript.Physics.ApplyForce(transform.forward * _pushScale);
            }
        }

        // Update is called once per frame
        public override void Update()
        {
            base.Update();

            //Make the material transparent if there is an object behind the barrier
            if (Physics.Raycast(transform.position, Vector3.forward, 1, _visibleLayers))
                _material.color = new Color(1, 1, 1, 0.5f);
            else
                _material.color = new Color(1, 1, 1, 1);
        }
    }
}
