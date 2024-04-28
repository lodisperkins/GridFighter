using Lodis.GridScripts;
using Lodis.Movement;
using Lodis.ScriptableObjects;
using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{
    public class RingBarrierBehaviour : HealthBehaviour
    {
        [Tooltip("The character that owns this ring barrier.")]
        [SerializeField]
        private GameObject _owner;
        [Tooltip("The length of the hit stun to apply to objects that crash into the ring barrier.")]
        [SerializeField]
        private float _hitStunOnCollision;
        [Tooltip("The collider that will be used to bounce characters away from the barrier.")]
        [SerializeField]
        private Collider _collider;
        private Collider _ownerCollider;
        [Tooltip("How fast a character in knockback has to travelto instantly shatter this barrier.")]
        [SerializeField]
        private FloatVariable _shatterSpeed;
        [Tooltip("The amount of speed a character needs to travel to damage this barrier at all.")]
        [SerializeField]
        private float _minimumDamageSpeed;
        [Tooltip("The amount of knockback characters will receive when being bounced off.")]
        [SerializeField]
        private float _knockBackDistance;
        [Tooltip("The angles characters will launch at after being bounced away from the barrier.")]
        [SerializeField]
        private float _launchAngle;
        [SerializeField]
        private ShieldController _shieldController;
        [Tooltip("Whether or no this barrier can be instantly shatter when the character is at high enough damage and speed.")]
        [SerializeField]
        private bool _canInstantShatter;
        [Tooltip("The game object that has the ring barrier mesh attached.")]
        [SerializeField]
        private GameObject _visuals;
        [Tooltip("The effect to play when a character hits the barrier.")]
        [SerializeField] private ParticleSystem _hitEffect;
        [Tooltip("The effect to play when a character damages the barrier.")]
        [SerializeField] private ParticleColorManagerBehaviour _takeDamageEffect;
        [Tooltip("The collider that explodes the loser of the match.")]
        [SerializeField] private GameObject _winCollider;
        private GridAlignment _alignment;
        private RingBarrierFeedbackBehaviour _ringBarrierFeedbackBehaviour;
        [SerializeField]
        private float _timeUntilNextHit = 0.01f;
        private bool _canHit = true;

        /// <summary>
        /// The character that owns this ring barrier.
        /// </summary>
        public GameObject Owner { get => _owner; set => _owner = value; }

        protected override void Awake()
        {
            base.Awake();
            //_shieldController.maxHP = Health;
            _ringBarrierFeedbackBehaviour = GetComponent<RingBarrierFeedbackBehaviour>();
        }

        protected override void Start()
        {
            base.Start();

            //Ignore the owner when it dies so they can pass through.
            AddOnDeathAction(() =>
            {
                Physics.IgnoreCollision(_collider, _ownerCollider);
                _winCollider.SetActive(true);
            });

            //Set invincibility for debugging based on match manager value.
            if (MatchManagerBehaviour.Instance.InvincibleBarriers)
                SetInvincibilityByCondition(condition => !MatchManagerBehaviour.Instance.InvincibleBarriers);

            _ownerCollider = Owner.GetComponent<GridPhysicsBehaviour>().BounceCollider;
            _alignment = Owner.GetComponent<GridMovementBehaviour>().Alignment;
        }

        /// <summary>
        /// Takes damage based on the damage type.
        /// If the damage is less than the durability
        /// or if the damage type isn't knockback type,
        /// no damage is dealt.
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
            if (!Owner || damageType != DamageType.KNOCKBACK || IsInvincible || (attacker != Owner) || damage < _minimumDamageSpeed)
                return 0;

            //Apply damage and activate damage effects.
            Health -= damage;

            CameraBehaviour.ShakeBehaviour.ShakeRotation();
            OnTakeDamageEvent.Raise(gameObject);
            _onTakeDamage?.Invoke();

            //_shieldController.GetHit(attacker.transform.position - transform.forward, transform.forward, 4, damage);
            return damage;
        }

        /// <summary>
        /// Takes damage based on the damage type.
        /// </summary>
        /// <param name="info">The hit collider data of the attack</param>
        /// <param name="attacker">The name of the object that damaged this object. Used for debugging</param>
        public override float TakeDamage(HitColliderData info, GameObject attacker)
        {
            if (!Owner || info.TypeOfDamage != DamageType.KNOCKBACK || (attacker != Owner) || info.Damage < _minimumDamageSpeed)
                return 0;


            //Apply damage and activate damage effects
            Health -= info.Damage;

            CameraBehaviour.ShakeBehaviour.ShakeRotation();

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
            if (!Owner || damageType != DamageType.KNOCKBACK || (attacker != Owner.name) || damage < _minimumDamageSpeed)
                return 0;

            //Apply damage and activate damage effects
            Health -= damage;

            OnTakeDamageEvent.Raise(gameObject);
            _onTakeDamage?.Invoke();
            return damage;
        }

        /// <summary>
        /// Gives the barrier a full heal and removes all visual damage.
        /// </summary>
        public override void ResetHealth()
        {
            gameObject.SetActive(true);
            base.ResetHealth();

            _visuals.SetActive(true);
            _winCollider.SetActive(false);

            Physics.IgnoreCollision(_collider, _ownerCollider, false);
            _ringBarrierFeedbackBehaviour.ResetVisuals();
        }

        public void Deactivate(bool spawnEffects = true)
        {
            Physics.IgnoreCollision(_collider, _ownerCollider);
            _winCollider.SetActive(true);
            _ringBarrierFeedbackBehaviour.DeactivateBarrier(spawnEffects);
        }

        public override void OnTriggerEnter(Collider collision)
        {
            if (!_canInstantShatter)
                return;

            //Only try to take damage if the object had a rigid body attached.
            KnockbackBehaviour knockback = collision.attachedRigidbody?.GetComponent<KnockbackBehaviour>();
            if (!knockback)
                return;

            //Calculates the dot product to ensure the character is moving towards the barrier.
            float dot = Vector3.Dot(transform.forward, knockback.Physics.LastVelocity.normalized);

            //Shatter the barrier if the pwner is being knocked back at the appropriate speed and damage.
            if (collision.gameObject == Owner && knockback.Physics.LastVelocity.magnitude >= _shatterSpeed.Value && dot < 0
                && knockback.CurrentAirState == AirState.TUMBLING && knockback.Health == knockback.MaxHealth.Value)
                TakeDamage(Owner, Health, 0, 0, DamageType.KNOCKBACK);
        }

        public override void OnCollisionEnter(Collision collision)
        {
            KnockbackBehaviour knockbackBehaviour = collision.gameObject.GetComponent<KnockbackBehaviour>();

            if (!knockbackBehaviour || knockbackBehaviour.Physics.LastVelocity.magnitude < _minimumDamageSpeed || !_canHit)
                return;

            if (knockbackBehaviour.CurrentAirState != AirState.TUMBLING)
                return;

            var offsetX = collision.transform.position.x - transform.position.x;
            float dir = offsetX / Mathf.Abs(offsetX);
            
            //Find the direction this collider was going to apply force originally
            Vector3 currentForceDirection = new Vector3(Mathf.Cos(_launchAngle) * dir, Mathf.Sin(_launchAngle), 0);

            //Find the new angle based on the direction of the attack on the x axis
            float dotProduct = Vector3.Dot(currentForceDirection, Vector3.right);
            float newAngle = Mathf.Acos(dotProduct);

            //Stops velocity so momentum is shifted completely.
            knockbackBehaviour.Physics.StopVelocity();

            //Creates a new hit collider to attack the character
            HitColliderData info = new HitColliderData { Name = name, BaseKnockBack = _knockBackDistance, KnockBackScale = 1.2f, HitAngle = newAngle, HitStunTime = _hitStunOnCollision, HitStopShakeStrength = 1, };
            HitColliderBehaviour hitCollider = new HitColliderBehaviour();
            hitCollider.ColliderInfo = info;

            //Deal damage to the character.
            knockbackBehaviour.LastCollider = hitCollider;
            knockbackBehaviour.TakeDamage(info,gameObject);

            _canHit = false;
            RoutineBehaviour.Instance.StartNewTimedAction(args => _canHit = true, TimedActionCountType.SCALEDTIME, _timeUntilNextHit);

            //Display the appropriate particle effect.
            Instantiate(_hitEffect.gameObject, collision.contacts[0].point, new Quaternion());
            if (collision.gameObject == Owner)
                Instantiate(_takeDamageEffect, collision.contacts[0].point, new Quaternion()).Alignment = _alignment;
        }
    }
}
