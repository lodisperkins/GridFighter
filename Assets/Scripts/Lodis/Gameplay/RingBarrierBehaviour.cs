using Lodis.Movement;
using Lodis.ScriptableObjects;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{
    public class RingBarrierBehaviour : HealthBehaviour
    {
        public string Owner;
        [SerializeField]
        private float _bounceDampen;
        [Tooltip("The length of the hit stun to apply to objects that crash into the ring barrier.")]
        [SerializeField]
        private float _hitStunOnCollision;
        private MeshRenderer _renderer;
        [SerializeField]
        private float _shatterVelocityMagnitude;
        [SerializeField]
        private float _minimumDamageSpeed;
        [SerializeField]
        private float _knockBackDistance;
        [SerializeField]
        private float _launchAngle;

        protected override void Start()
        {
            base.Start();
            _renderer = GetComponent<MeshRenderer>();
        }

        /// <summary>
        /// Takes damage based on the damage type.
        /// If the damage is less than the durability
        /// or if the damage type isn't knockback type,
        /// no damage is dealt
        /// </summary>
        /// <param name="attacker">The name of the object that damaged this object. Used for debugging</param>
        /// <param name="damage">The amount of damage being applied to the object. 
        /// Ring barriers only break if the damage amount is greater than the total health</param>
        /// <param name="baseKnockBack"></param>
        /// <param name="hitAngle"></param>
        /// <returns></returns>
        /// <param name="damageType">The type of damage this object will take</param>
        public override float TakeDamage(string attacker, float damage, float baseKnockBack = 0, float hitAngle = 0, DamageType damageType = DamageType.DEFAULT, float hitStun = 0)
        {
            if (damageType != DamageType.KNOCKBACK || IsInvincible || (attacker != Owner && Owner != "") || damage < _minimumDamageSpeed)
                return 0;

            Health -= damage;

            OnTakeDamage.Raise(gameObject);
            return damage;
        }

        /// <summary>
        /// Takes damage based on the damage type.
        /// </summary>
        /// <param name="attacker">The name of the object that damaged this object. Used for debugging</param>
        public override float TakeDamage(HitColliderInfo info, GameObject attacker)
        {
            if (info.TypeOfDamage != DamageType.KNOCKBACK || (attacker.name != Owner && Owner != "") || info.Damage < _minimumDamageSpeed)
                return 0;

            Health -= info.Damage;

            OnTakeDamage.Raise(gameObject);
            return info.Damage;
        }

        /// <summary>
        /// Takes damage based on the damage type.
        /// </summary>
        /// <param name="attacker">The name of the object that damaged this object. Used for debugging</param>
        /// <param name="abilityData">The data scriptable object associated with the ability</param>
        /// <param name="damageType">The type of damage this object will take</param>
        public override float TakeDamage(string attacker, AbilityData abilityData, DamageType damageType = DamageType.DEFAULT)
        {
            float damage = abilityData.GetColliderInfo(0).Damage;

            if (damageType != DamageType.KNOCKBACK || (attacker != Owner && Owner != "") || damage < _minimumDamageSpeed)
                return 0;


            Health -= damage;

            OnTakeDamage.Raise(gameObject);
            return damage;
        }

        private void OnTriggerEnter(Collider collision)
        {
            GridPhysicsBehaviour gridPhysicsBehaviour = collision.gameObject.GetComponent<GridPhysicsBehaviour>();

            if (collision.gameObject.name == Owner && gridPhysicsBehaviour.LastVelocity.magnitude >= _shatterVelocityMagnitude)
            {
                gameObject.SetActive(false);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            GridPhysicsBehaviour gridPhysicsBehaviour = collision.gameObject.GetComponent<GridPhysicsBehaviour>();

            if (!gridPhysicsBehaviour || gridPhysicsBehaviour.LastVelocity.magnitude < _minimumDamageSpeed)
                return;

            if (_bounceDampen == 0)
                _bounceDampen = 1;

            //Find the direction this collider was going to apply force originally
            Vector3 currentForceDirection = new Vector3(Mathf.Cos(_launchAngle) * transform.forward.x, Mathf.Sin(_launchAngle), 0);

            //Find the new angle based on the direction of the attack on the x axis
            float dotProduct = Vector3.Dot(currentForceDirection, Vector3.right);
            float newAngle = Mathf.Acos(dotProduct);

            Vector3 force = gridPhysicsBehaviour.CalculatGridForce(_knockBackDistance, newAngle);
            gridPhysicsBehaviour.StopVelocity();
            gridPhysicsBehaviour.ApplyImpulseForce(force);
        }
    }
}
