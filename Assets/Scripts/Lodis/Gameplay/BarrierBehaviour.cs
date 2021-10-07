using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{
    public class BarrierBehaviour : HealthBehaviour
    {
        private Material _material;
        [SerializeField]
        private string[] _visibleLayers;
        private float _rangeToIgnoreUpAngle;
        private Movement.GridMovementBehaviour _movement;
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

        public string Owner { get => _owner; set => _owner = value; }

        // Start is called before the first frame update
        void Start()
        {
            _material = GetComponent<Renderer>().material;
            _movement = GetComponent<Movement.GridMovementBehaviour>();
        }

        public override float TakeDamage(string attacker, float damage, float knockBackScale = 0, float hitAngle = 0, DamageType damageType = DamageType.DEFAULT)
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
            float velocityMagnitude = knockBackScript.LastVelocity.magnitude;
            float knockbackScale = knockBackScript.CurrentKnockBackScale * (velocityMagnitude / knockBackScript.LaunchVelocity.magnitude);

            if (knockbackScale == 0 || float.IsNaN(knockbackScale) || !knockBackScript.InHitStun)
                return;

            //Apply ricochet force and damage
            knockBackScript.TakeDamage(name, _damageOnCollision, knockbackScale * _bounceScale / BounceDampen, hitAngle, true, true, DamageType.KNOCKBACK);
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
                knockBackScript.ApplyForce(Vector3.up * knockBackScript.Gravity);
                knockBackScript.ApplyForce(transform.forward * _pushScale);
            }
        }

        // Update is called once per frame
        public override void Update()
        {
            base.Update();

            int layerMask = LayerMask.GetMask(_visibleLayers);

            if (Physics.Raycast(transform.position, Vector3.forward, 1, layerMask))
                _material.color = new Color(1, 1, 1, 0.5f);
            else
                _material.color = new Color(1, 1, 1, 1);
        }
    }
}
