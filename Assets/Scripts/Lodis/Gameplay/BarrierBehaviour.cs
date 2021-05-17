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

        // Start is called before the first frame update
        void Start()
        {
            _material = GetComponent<Renderer>().material;
            _movement = GetComponent<Movement.GridMovementBehaviour>();
        }

        public override void OnCollisionEnter(Collision collision)
        {
            Movement.KnockbackBehaviour knockBackScript = collision.gameObject.GetComponent<Movement.KnockbackBehaviour>();
            //Checks if the object is not grid moveable and isn't in hit stun
            if (!knockBackScript || !knockBackScript.InHitStun)
                return;

            //Calculate the knockback and hit angle for the ricochet
            ContactPoint contactPoint = collision.GetContact(0);
            Vector3 direction = new Vector3(contactPoint.normal.x, contactPoint.normal.y, 0);
            float dotProduct = Vector3.Dot(Vector3.right, -direction);
            float hitAngle = Mathf.Acos(dotProduct);
            float velocityMagnitude = knockBackScript.LastVelocity.magnitude;
            float knockbackScale = knockBackScript.CurrentKnockBackScale * (velocityMagnitude / knockBackScript.LaunchVelocity.magnitude);

            if (knockbackScale == 0 || float.IsNaN(knockbackScale))
                return;

            //Apply ricochet force and damage
            knockBackScript.TakeDamage(name, knockbackScale * 2, knockbackScale / BounceDampen, hitAngle, DamageType.KNOCKBACK);
        }

        // Update is called once per frame
        void Update()
        {
            int layerMask = LayerMask.GetMask(_visibleLayers);

            if (Physics.Raycast(transform.position, Vector3.forward, 1, layerMask))
                _material.color = new Color(1, 1, 1, 0.5f);
            else
                _material.color = new Color(1, 1, 1, 1);
        }
    }
}
