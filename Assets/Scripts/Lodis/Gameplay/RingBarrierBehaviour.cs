using Lodis.Movement;
using Lodis.ScriptableObjects;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{
    public class RingBarrierBehaviour : HealthBehaviour
    {
        public GameObject Owner;
        [SerializeField]
        private float _bounceDampen;
        [Tooltip("The length of the hit stun to apply to objects that crash into the ring barrier.")]
        [SerializeField]
        private float _hitStunOnCollision;
        [SerializeField]
        private Collider _collider;
        private Collider _ownerCollider;
        [SerializeField]
        private GameObject _visual;
        [SerializeField]
        private FloatVariable _shatterVelocityMagnitude;
        [SerializeField]
        private float _minimumDamageSpeed;
        [SerializeField]
        private float _knockBackDistance;
        [SerializeField]
        private float _launchAngle;
        [SerializeField]
        private ShieldController _shieldController;

        protected override void Awake()
        {
            base.Awake();
            _shieldController.maxHP = Health;
        }

        protected override void Start()
        {
            base.Start();
            AddOnDeathAction(DeactivateBarrier);
            //_shieldController.GetHit(transform.position, transform.forward, 1, 0);
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
        /// <param name="hitStun">The amount of time the object will be in hit stun</param>
        public override float TakeDamage(GameObject attacker, float damage, float baseKnockBack = 0, float hitAngle = 0, DamageType damageType = DamageType.DEFAULT, float hitStun = 0)
        {
            if (!Owner) return 0;

            if (damageType != DamageType.KNOCKBACK || IsInvincible || (attacker != Owner) || damage < _minimumDamageSpeed)
                return 0;

            Health -= damage;
            OnTakeDamageEvent.Raise(gameObject);
            _onTakeDamage?.Invoke();
            _shieldController.GetHit(attacker.transform.position, transform.forward, 4, damage);
            return damage;
        }

        /// <summary>
        /// Takes damage based on the damage type.
        /// </summary>
        /// <param name="info">The hit collider data of the attack</param>
        /// <param name="attacker">The name of the object that damaged this object. Used for debugging</param>
        public override float TakeDamage(HitColliderInfo info, GameObject attacker)
        {
            if (!Owner) return 0;
            if (info.TypeOfDamage != DamageType.KNOCKBACK || (attacker != Owner) || info.Damage < _minimumDamageSpeed)
                return 0;

            Health -= info.Damage;

            OnTakeDamageEvent.Raise(gameObject);
            _onTakeDamage?.Invoke();
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
            if (!Owner) return 0;
            if (damageType != DamageType.KNOCKBACK || (attacker != Owner.name) || damage < _minimumDamageSpeed)
                return 0;

            Health -= damage;

            OnTakeDamageEvent.Raise(gameObject);
            _onTakeDamage?.Invoke();
            return damage;
        }

        private void DeactivateBarrier()
        {
            Physics.IgnoreCollision(_collider, _ownerCollider);
            _visual.SetActive(false);
        }

        public override void OnTriggerEnter(Collider collision)
        {
            GridPhysicsBehaviour gridPhysicsBehaviour = collision.gameObject.GetComponent<GridPhysicsBehaviour>();

            if (collision.gameObject == Owner && gridPhysicsBehaviour.LastVelocity.magnitude >= _shatterVelocityMagnitude.Value)
            {
                DeactivateBarrier();
            }
        }

        public override void OnCollisionEnter(Collision collision)
        {
            KnockbackBehaviour knockbackBehaviour = collision.gameObject.GetComponent<KnockbackBehaviour>();

            if (!knockbackBehaviour || knockbackBehaviour.Physics.LastVelocity.magnitude < _minimumDamageSpeed)
                return;

            if (knockbackBehaviour.CurrentAirState != AirState.TUMBLING)
                return;
            
            if (collision.gameObject == Owner && !_ownerCollider)
                _ownerCollider = collision.collider;

            if (_bounceDampen == 0)
                _bounceDampen = 1;

            var offsetX = collision.transform.position.x - transform.position.x;
            float dir = offsetX / Mathf.Abs(offsetX);
            
            //Find the direction this collider was going to apply force originally
            Vector3 currentForceDirection = new Vector3(Mathf.Cos(_launchAngle) * dir, Mathf.Sin(_launchAngle), 0);

            //Find the new angle based on the direction of the attack on the x axis
            float dotProduct = Vector3.Dot(currentForceDirection, Vector3.right);
            float newAngle = Mathf.Acos(dotProduct);

            knockbackBehaviour.Physics.StopVelocity();
            HitColliderInfo info = new HitColliderInfo { Name = name, BaseKnockBack = _knockBackDistance, KnockBackScale = 1.2f, HitAngle = newAngle, HitStunTime = _hitStunOnCollision, HitStopTimeModifier = 1, };
            HitColliderBehaviour hitCollider = new HitColliderBehaviour();
            hitCollider.ColliderInfo = info;
            knockbackBehaviour.LastCollider = hitCollider;
            knockbackBehaviour.TakeDamage(info,gameObject);
        }
    }
}
