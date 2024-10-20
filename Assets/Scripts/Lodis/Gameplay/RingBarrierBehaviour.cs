using FixedPoints;
using Lodis.GridScripts;
using Lodis.Movement;
using Lodis.ScriptableObjects;
using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using Types;
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
        private Fixed32 _hitStunOnCollision;
        [Tooltip("How fast a character in knockback has to travelto instantly shatter this barrier.")]
        [SerializeField]
        private FloatVariable _shatterSpeed;
        [Tooltip("The amount of speed a character needs to travel to damage this barrier at all.")]
        [SerializeField]
        private Fixed32 _minimumDamageSpeed;
        [Tooltip("The amount of knockback characters will receive when being bounced off.")]
        [SerializeField]
        private Fixed32 _knockBackDistance;
        [Tooltip("The angles characters will launch at after being bounced away from the barrier.")]
        [SerializeField]
        private Fixed32 _launchAngle;
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
        private Fixed32 _timeUntilNextHit;

        //---
        private bool _canHit = true;
        private EntityData _ownerData;

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
                GridGame.IgnoreCollision(Entity.Data, _ownerData);
                FixedPointTimer.StartNewTimedAction(() => _winCollider.SetActive(true), new Fixed32(6553));
            });

            //Set invincibility for debugging based on match manager value.
            if (MatchManagerBehaviour.Instance.InvincibleBarriers)
                SetInvincibilityByCondition(condition => !MatchManagerBehaviour.Instance.InvincibleBarriers);

            _ownerData = Owner.GetComponent<EntityDataBehaviour>().Data;
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
        public override float TakeDamage(EntityData attacker, Fixed32 damage, Fixed32 baseKnockBack = default, Fixed32 hitAngle = default, DamageType damageType = DamageType.DEFAULT, Fixed32 hitStun = default)
        {
            if (!Owner || damageType != DamageType.KNOCKBACK || IsInvincible || (attacker.UnityObject != Owner) || damage < _minimumDamageSpeed)
                return 0;

            //Apply damage and activate damage effects.
            Health -= damage;

            CameraBehaviour.ShakeBehaviour.ShakeRotation();
            _onTakeDamage?.Invoke();

            //_shieldController.GetHit(attacker.transform.position - transform.forward, transform.forward, 4, damage);
            return damage;
        }

        /// <summary>
        /// Takes damage based on the damage type.
        /// </summary>
        /// <param name="info">The hit collider data of the attack</param>
        /// <param name="attacker">The name of the object that damaged this object. Used for debugging</param>
        public override float TakeDamage(HitColliderData info, EntityData attacker)
        {
            if (!Owner || info.TypeOfDamage != DamageType.KNOCKBACK || (attacker.UnityObject != Owner) || info.Damage < _minimumDamageSpeed)
                return 0;


            //Apply damage and activate damage effects
            Health -= info.Damage;

            CameraBehaviour.ShakeBehaviour.ShakeRotation();

            _onTakeDamage?.Invoke();
            return info.Damage;
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

            GridGame.IgnoreCollision(Entity.Data, _ownerData, false);
            _ringBarrierFeedbackBehaviour.ResetVisuals();
        }

        public void Deactivate(bool spawnEffects = true)
        {
            GridGame.IgnoreCollision(Entity.Data, _ownerData);
            _winCollider.SetActive(true);
            _ringBarrierFeedbackBehaviour.DeactivateBarrier(spawnEffects);
        }

        public override void OnOverlapEnter(Collision collision)
        {
            if (!_canInstantShatter || collision.OtherEntity.UnityObject == null)
                return;

            //Only try to take damage if the object had a rigid body attached.
            KnockbackBehaviour knockback = collision.OtherEntity.UnityObject.GetComponent<KnockbackBehaviour>();
            if (!knockback)
                return;

            //Calculates the dot product to ensure the character is moving towards the barrier.
            float dot = FVector3.Dot((FVector3)transform.forward, knockback.Physics.Velocity.GetNormalized());

            //Shatter the barrier if the pwner is being knocked back at the appropriate speed and damage.
            if (collision.OtherEntity.UnityObject == Owner && knockback.Physics.Velocity.Magnitude >= _shatterSpeed.Value && dot < 0
                && knockback.CurrentAirState == AirState.TUMBLING && knockback.Health == knockback.MaxHealth.Value)
                TakeDamage(collision.OtherEntity, Health, 0, 0, DamageType.KNOCKBACK);
        }

        public override void OnHitEnter(Collision collision)
        {
            KnockbackBehaviour knockbackBehaviour = collision.OtherEntity.UnityObject.GetComponent<KnockbackBehaviour>();

            if (!knockbackBehaviour || knockbackBehaviour.Physics.Velocity.Magnitude < _minimumDamageSpeed || !_canHit)
                return;

            if (knockbackBehaviour.CurrentAirState != AirState.TUMBLING)
                return;

            Fixed32 dir = Owner.GetComponent<GridMovementBehaviour>().GetAlignmentX();
            
            //Find the direction this collider was going to apply force originally
            FVector3 currentForceDirection = new FVector3(Fixed32.Cos(_launchAngle) * dir, Fixed32.Sin(_launchAngle), 0);

            //Find the new angle based on the direction of the attack on the x axis
            Fixed32 dotProduct = FVector3.Dot(currentForceDirection, FVector3.Right);
            Fixed32 newAngle = Fixed32.Acos(dotProduct);

            //Stops velocity so momentum is shifted completely.
            //knockbackBehaviour.Physics.StopVelocity();

            //Creates a new hit collider to attack the character
            HitColliderData info = new HitColliderData { Name = name, BaseKnockBack = knockbackBehaviour.Physics.Velocity.Magnitude / 2, KnockBackScale = 1.2f, HitAngle = newAngle, HitStunTime = _hitStunOnCollision, HitStopShakeStrength = 1, };
            HitColliderBehaviour hitCollider = new HitColliderBehaviour();
            hitCollider.ColliderInfo = info;

            //Deal damage to the character.
            knockbackBehaviour.LastCollider = hitCollider;
            knockbackBehaviour.TakeDamage(info,collision.OtherEntity);

            _canHit = false;
            FixedPointTimer.StartNewTimedAction(() => _canHit = true, _timeUntilNextHit);

            //Display the appropriate particle effect.

            Vector3 particleSpawn = new Vector3(collision.ContactPoint.X, collision.ContactPoint.Y, knockbackBehaviour.FixedTransform.WorldPosition.Z);

            Instantiate(_hitEffect.gameObject, particleSpawn, new Quaternion());
            if (collision.OtherEntity.UnityObject == Owner)
                Instantiate(_takeDamageEffect, particleSpawn, new Quaternion()).Alignment = _alignment;
        }
    }
}
