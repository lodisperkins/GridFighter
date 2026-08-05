using FixedPoints;
using Lodis.Input;
using Lodis.Movement;
using Lodis.ScriptableObjects;
using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Types;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Lodis.Gameplay
{
    public class HealthBehaviour : SimulationBehaviour
    {
        public enum ArmorType
        {
            HitsToBreak,
            DamageThreshold,
            TimeToBreak
        }

        [Tooltip("The measurement of the amount of damage this object can take or has taken")]
        [SerializeField] private Fixed32 _health;
        [Tooltip("The starting amount of damage this object can take or has taken. Set to -1 to start with max health.")]
        [SerializeField] private float _startingHealth = -1;
        [Tooltip("The maximum amount of damage this object can take or has taken")]
        [SerializeField] private FloatVariable _maxHealth;

        [Tooltip("Whether or not this object should be deleted if the health is 0")]
        [SerializeField] private bool _destroyOnDeath;
        [SerializeField] protected UnityEvent _onTakeDamage;
        [SerializeField] private UnityEvent _onDeath;

        [Tooltip("Whether or not the health value for this object is above 0")]
        [SerializeField] private bool _isAlive = true;
        [Tooltip("Whether or not this object can be damaged or knocked back")]
        [SerializeField] private bool _isInvincible;
        [Tooltip("Whether or not this object is in a stunned state")]
        [SerializeField] private bool _stunned;
        [SerializeField] private bool _isIntangible;
        [SerializeField] private bool _hasArmor;
        [SerializeField] private bool _counterStanceActive;


        [SerializeField] private Renderer _meshRenderer;

        //---
        private FixedTimeAction _invincibilityTimer;
        private Condition _invincibilityCondition;
        private Condition _intagibilityCondition;
        protected Material _material;
        private Color _defaultColor;
        private Coroutine _stunRoutine;
        private MovesetBehaviour _moveset;
        private InputBehaviour _input;
        private UnityAction _onInvincibilityActivated;
        private UnityAction _onInvincibilityDeactivated;
        private UnityAction _onIntagibilityActivated;
        private UnityAction _onIntagibilityDeactivated;
        private UnityAction _onStunEnabled;
        private UnityAction _onStunDisabled;
        private UnityAction _onArmorActivated;
        private UnityAction _onArmorDeactivated;
        private UnityAction<Fixed32> _onArmorHit;
        private UnityAction _onArmorBroken;
        private UnityAction _onArmorBrokenTemp;
        private UnityAction _onCounterStanceActive;
        private UnityAction _onCounterStanceInactive;
        private int _damageableAbilityID = -1;

        private Fixed32 _armorHealth;
        private ArmorType _currentArmorType;

        private string _defaultLayer;
        private HitColliderBehaviour _lastCollider;
        private HitStopBehaviour _hitStop;
        private CharacterDefenseBehaviour _defenseBehaviour;

        protected GridMovementBehaviour _movement;
        private Condition aliveCondition;
        private FixedTimeAction _stunTimer;
        private FixedTimeAction _armorTimer;
        private FixedTimeAction _counterTimer;

        public bool Stunned
        {
            get
            {
                return _stunned;
            }
            protected set
            {
                if (value == false && _stunned)
                    _onStunDisabled?.Invoke();

                _stunned = value;
            }

        }

        public virtual bool IsAlive
        {
            get
            {
                return _isAlive;
            }
        }

        public Fixed32 Health
        {
            get => _health;
            protected set
            {
                //Prevent damage if the object is invincible or dead
                if (value < _health && (_isInvincible || !IsAlive))
                {
                    return;
                }

                if (value > MaxHealth)
                {
                    _health = MaxHealth;
                    return;
                }

                _health = value;
            }
        }

        public bool IsInvincible
        {
            get => _isInvincible;

            protected set
            {
                if (!value && _isInvincible)
                    _onInvincibilityDeactivated?.Invoke();
                else if (value && !_isInvincible)
                    _onInvincibilityActivated?.Invoke();

                _isInvincible = value;
            }
        }

        public bool HasArmor
        {
            get => _hasArmor;

            protected set
            {
                if (!value && _hasArmor)
                    _onArmorDeactivated?.Invoke();
                else if (value && !_hasArmor)
                    _onArmorActivated?.Invoke();

                _hasArmor = value;
            }
        }

        public bool CounterStanceActive
        {
            get => _counterStanceActive;
            protected set
            {
                if (!value && _counterStanceActive)
                    _onCounterStanceInactive?.Invoke();
                else if (value && !_counterStanceActive)
                    _onCounterStanceActive?.Invoke();

                _counterStanceActive = value;
            }
        }

        public FloatVariable MaxHealth { get => _maxHealth; }

        public HitColliderBehaviour LastCollider
        {
            get => _lastCollider;
            set => _lastCollider = value;
        }

        public bool IsIntangible
        {
            get => _isIntangible;
            set
            {
                if (!value && _isIntangible)
                    _onIntagibilityDeactivated?.Invoke();
                else if (value && !_isIntangible)
                    _onIntagibilityActivated?.Invoke();

                _isIntangible = value;
            }
        }

        public CharacterDefenseBehaviour DefenseBehaviour { get => _defenseBehaviour; private set => _defenseBehaviour = value; }
        public Renderer MeshRenderer { get => _meshRenderer; set => _meshRenderer = value; }
        public int DamageableAbilityID { get => _damageableAbilityID; private set => _damageableAbilityID = value; }
        public Condition AliveCondition { get => aliveCondition; set => aliveCondition = value; }
        public Fixed32 TimeInCurrentStun { get; protected set; }
        public Fixed32 LastAbilityID { get; protected set; }

        public override string LogName => "HealthBehaviour";

        public override void Serialize(BinaryWriter bw)
        {
            _health.Serialize(bw);
            bw.Write(_isInvincible);
            bw.Write(_stunned);
            bw.Write(_isIntangible);
            bw.Write(DamageableAbilityID);
            bw.Write(_hasArmor);
            bw.Write(_counterStanceActive);
            LastAbilityID.Serialize(bw);
        }

        public override void Deserialize(BinaryReader br)
        {
            _health = _health.Deserialize(br);
            _isInvincible = br.ReadBoolean();
            _stunned = br.ReadBoolean();
            _isIntangible = br.ReadBoolean();
            DamageableAbilityID = br.ReadInt32();
            _hasArmor = br.ReadBoolean();
            _counterStanceActive = br.ReadBoolean();
            LastAbilityID = LastAbilityID.Deserialize(br);
        }

        /// <summary>
        /// Hashes the serialized health and invincibility state so damage-related
        /// mismatches can be tied to this behavior.
        /// </summary>
        protected override string[] GetLogItems()
        {
            return new string[] 
            {
                $"Health: {_health}",
                $"IsInvincible: {_isInvincible}",
                $"Stunned: {_stunned}",
                $"IsIntangible: {_isIntangible}",
                $"DamageableAbilityID: {_damageableAbilityID}",
                $"HasArmor: {_hasArmor}",
                $"CounterStanceActive: {_counterStanceActive}",
                $"LastAbilityID: {LastAbilityID}"
            };
        }

        protected override void Awake()
        {
            base.Awake();

            if (_startingHealth < 0)
                _health = _maxHealth.FixedValue;
            else
                _health = _startingHealth;

            DefenseBehaviour = GetComponent<CharacterDefenseBehaviour>();
            _moveset = GetComponent<MovesetBehaviour>();
            _input = GetComponent<Input.InputBehaviour>();
            _movement = GetComponent<GridMovementBehaviour>();
            _hitStop = GetComponent<HitStopBehaviour>();
        }

        protected virtual void Start()
        {
            _isAlive = true;
            AliveCondition = condition => _health > 0;
            _defaultLayer = LayerMask.LayerToName(gameObject.layer);

            if (_meshRenderer)
            {
                _material = _meshRenderer.material;
                _defaultColor = _material.color;
            }

        }

        /// <summary>
        /// Only hitboxes that come from an ability with this ID can collide with this object.
        /// </summary>
        /// <param name="abilityID">The ability ID of the hit colliders.</param>
        /// <param name="condition">When to allow this object to be hit by any collider again.</param>
        public void SetDamageableAbilityID(int abilityID, Condition condition)
        {
            _damageableAbilityID = abilityID;

            RoutineBehaviour.Instance.StartNewConditionAction(args => DamageableAbilityID = -1, condition);
        }

        /// <summary>
        /// Takes damage based on the damage type.
        /// </summary>
        /// <param name="attacker">The name of the object that damaged this object. Used for debugging</param>
        /// <param name="damage">The amount of damage being applied to the object. 
        /// Ring barriers only break if the damage amount is greater than the total health</param>
        /// <param name="baseKnockBack"></param>
        /// <param name="hitAngle"></param>
        /// <returns></returns>
        /// <param name="damageType">The type of damage this object will take</param>
        public virtual Fixed32 TakeDamage(EntityData attacker, Fixed32 damage, Fixed32 baseKnockBack = default, Fixed32 hitAngle = default, DamageType damageType = DamageType.DEFAULT, Fixed32 hitStun = default)
        {
            if (UpdateSuperArmor(damage))
                return 0;

            Fixed32 damageTaken = _health;

            Health -= damage;

            damageTaken -= Health;

            if (Health < 0)
                _health = 0;

            _onTakeDamage?.Invoke();
            return damageTaken;
        }

        /// <summary>
        /// Takes damage based on the damage type.
        /// </summary>
        /// <param name="attacker">The name of the object that damaged this object. Used for debugging</param>
        /// <param name="damage">The amount of damage being applied to the object. 
        /// Ring barriers only break if the damage amount is greater than the total health</param>
        /// <param name="baseKnockBack"></param>
        /// <param name="hitAngle"></param>
        /// <returns></returns>
        /// <param name="damageType">The type of damage this object will take</param>
        public virtual Fixed32 TakeDamage(HitColliderData info, EntityData attacker)
        {
            if (UpdateSuperArmor(info.Damage))
                return 0;

            Fixed32 damageTaken = _health;

            Health -= info.Damage;

            damageTaken -= Health;

            if (_health < 0)
                _health = 0;

            LastAbilityID = info.AbilityID;
            _onTakeDamage?.Invoke();
            return damageTaken;
        }

        /// <summary>
        /// Doesnt call any events, don't do nothing fancy, just straight number go down.
        /// </summary>
        public virtual void TakeDamageRaw(Fixed32 damage)
        {
            Health -= damage;
        }

        public virtual Fixed32 Heal(Fixed32 healthAmount)
        {
            _health = healthAmount;
            return healthAmount;
        }

        public virtual void ResetHealth()
        {
            _health = _startingHealth == -1 ? _maxHealth.FixedValue : _startingHealth;
            _isAlive = true;

            if (_stunned)
                CancelStun();

            DisableSuperArmor();
        }

        public bool CanBeHit()
        {
            return !IsInvincible && !IsIntangible && !HasArmor;
        }

        public bool UpdateSuperArmor(Fixed32 damage)
        {
            if (!HasArmor)
                return false;

            switch (_currentArmorType)
            {
                case ArmorType.HitsToBreak:
                    _armorHealth--;
                    _onArmorHit?.Invoke(_armorHealth);

                    if (_armorHealth <= 0)
                    {
                        _onArmorBroken?.Invoke();
                        _onArmorBrokenTemp?.Invoke();
                        _onArmorBrokenTemp = null;
                        HasArmor = false;
                    }
                    break;

                case ArmorType.DamageThreshold:
                    _armorHealth -= damage;
                    _onArmorHit?.Invoke(damage);

                    if (_armorHealth <= 0)
                    {
                        _onArmorBroken?.Invoke();
                        _onArmorBrokenTemp?.Invoke();
                        _onArmorBrokenTemp = null;
                        HasArmor = false;
                    }
                    break;
                default:
                    _onArmorHit?.Invoke(damage);
                    break;
            }

            return true;
        }

        public void EnableSuperArmor(ArmorType armorType, Fixed32 armorValue)
        {
            HasArmor = true;

            _currentArmorType = armorType;
            _armorHealth = armorValue;

            if (_currentArmorType == ArmorType.TimeToBreak)
            {
                _armorTimer = FixedPointTimer.StartNewTimedAction(() => HasArmor = false, armorValue);
            }
        }

        public void EnableCounterStance(Fixed32 time, UnityAction onCounterSuccess)
        {
            EnableSuperArmor(ArmorType.HitsToBreak, 1);
            _counterTimer = FixedPointTimer.StartNewTimedAction(DisableSuperArmor, time);
            _onArmorBrokenTemp += onCounterSuccess;
            _onArmorBrokenTemp += () => CounterStanceActive = false;

            CounterStanceActive = true;
        }

        public void DisableSuperArmor()
        {
            HasArmor = false;
            CounterStanceActive = false;
            _armorTimer?.Stop();
        }

        /// <summary>
        /// Starts the timer for the movement and input being disabled
        /// </summary>
        /// <param name="time">The amount of time the object is stunned</param>
        protected void DeactivateStun()
        {
            //Enable components if the actor has them attached
            if (_moveset)
                _moveset.enabled = true;
            if (_input)
                _input.enabled = true;
            Stunned = false;
        }

        /// <summary>
        /// Disables abilty use and input for the object if applicable
        /// </summary>
        /// <param name="time">The amount of time to disable the components for</param>
        public virtual void Stun(Fixed32 time)
        {
            if (Stunned || IsInvincible || _defenseBehaviour?.IsShielding == true || IsIntangible || HasArmor)
                return;

            Stunned = true;

            _hitStop.CancelHitStop(true);

            //Disable components if the object has them attached
            if (_moveset)
            {
                _moveset.enabled = false;
                _moveset.EndCurrentAbility();
            }
            if (_input)
            {
                _input.enabled = false;
                _input.StopAllCoroutines();
            }
            if (_movement)
            {
                _movement.DisableMovement(condition => Stunned == false, false, true);
            }

            _stunTimer = FixedPointTimer.StartNewTimedAction(DeactivateStun, time);
            TimeInCurrentStun = time;
            _onStunEnabled?.Invoke();
        }

        public virtual void CancelStun()
        {
            if (!Stunned)
                return;

            _stunTimer.Stop();

            DeactivateStun();
        }

        public void AddOnStunAction(UnityAction action)
        {
            _onStunEnabled += action;
        }
        public void AddOnStunDisabledAction(UnityAction action)
        {
            _onStunDisabled += action;
        }

        public void AddOnDeathAction(UnityAction action)
        {
            _onDeath.AddListener(action);
        }

        /// <summary>
        /// Adds an action to the event called when this object is damaged
        /// </summary>
        /// <param name="action">The new listener to to the event</param>
        public void AddOnTakeDamageAction(UnityAction action)
        {
            _onTakeDamage.AddListener(action);
        }

        public void AddOnInvincibilityActiveAction(UnityAction action)
        {
            _onInvincibilityActivated += action;
        }

        public void AddOnInvincibilityInactiveAction(UnityAction action)
        {
            _onInvincibilityDeactivated += action;
        }

        public void AddOnIntangibilityActiveAction(UnityAction action)
        {
            _onIntagibilityActivated += action;
        }

        public void AddOnIntangibilityInactiveAction(UnityAction action)
        {
            _onIntagibilityDeactivated += action;
        }

        public void AddOnArmorActiveAction(UnityAction action)
        {
            _onArmorActivated += action;
        }

        public void AddOnArmorHitAction(UnityAction<Fixed32> action)
        {
            _onArmorHit += action;
        }

        public void RemoveOnArmorHitAction(UnityAction<Fixed32> action)
        {
            _onArmorHit -= action;
        }

        public void AddOnArmorInactiveAction(UnityAction action)
        {
            _onArmorDeactivated += action;
        }

        public void AddOnArmorBrokenAction(UnityAction action)
        {
            _onArmorBroken += action;
        }

        public void RemoveOnArmorBrokenAction(UnityAction action)
        {
            _onArmorBroken -= action;
        }

        public void AddOnCounterStanceActiveAction(UnityAction action)
        {
            _onCounterStanceActive += action;
        }

        public void AddOnCounterStanceInactiveAction(UnityAction action)
        {
            _onCounterStanceInactive += action;
        }

        /// <summary>
        /// Makes the object invincible. No damage can be taken by the object
        /// </summary>
        /// <param name="time">How long in seconds the object is invincible for</param>
        public void SetInvincibilityByTimer(float time)
        {
            IsInvincible = true;

            if (_invincibilityTimer != null)
                _invincibilityTimer.Stop();

            _invincibilityTimer = FixedPointTimer.StartNewTimedAction(() => IsInvincible = false, time);

        }

        /// <summary>
        /// Makes the object invincible. No damage can be taken by the object
        /// </summary>
        /// <param name="condition">Will turn off invincibility when this condition is true</param>
        public void SetInvincibilityByCondition(Condition condition)
        {
            _invincibilityCondition = condition;
            IsInvincible = true;
        }

        /// <summary>
        /// Makes the object invincible. No damage can be taken by the object
        /// </summary>
        /// <param name="time">How long in seconds the object is invincible for</param>
        public void SetIntagibilityByTimer(float time)
        {
            IsIntangible = true;
            //TODO add layer system
            gameObject.layer = LayerMask.NameToLayer("IgnoreHitColliders");
            RoutineBehaviour.Instance.StartNewTimedAction(args => IsIntangible = false, TimedActionCountType.SCALEDTIME, time);
        }

        /// <summary>
        /// Makes the object invincible. No damage can be taken by the object
        /// </summary>
        /// <param name="condition">Will turn off invincibility when this condition is true</param>
        public void SetIntagibilityByCondition(Condition condition)
        {
            _intagibilityCondition = condition;
            gameObject.layer = LayerMask.NameToLayer("IgnoreHitColliders");
            IsIntangible = true;
        }

        /// <summary>
        /// Deactivates the invincibility and clears the condition event for disabling automatically
        /// </summary>
        public void DisableInvincibility()
        {
            if (!_isInvincible)
                return;

            _invincibilityTimer?.Stop();
            _invincibilityCondition = null;
            IsInvincible = false;
        }

        public void UpdateIsAlive()
        {
            _isAlive = AliveCondition.Invoke();
        }

        // Update is called once per frame
        public override void Tick(Fixed32 dt)
        {
            //Death check
            if (IsAlive && Health <= 0)
                _onDeath?.Invoke();

            UpdateIsAlive();

            if (!IsAlive && _destroyOnDeath)
                GridGame.RemoveEntityFromGame(Entity.Data);

            //Invincibilty check
            if (_invincibilityCondition?.Invoke() == true)
            {
                IsInvincible = false;
                _invincibilityCondition = null;
            }

            //Intangibilty check
            if (_intagibilityCondition?.Invoke() == true)
            {
                IsIntangible = false;
                _intagibilityCondition = null;
            }

            gameObject.layer = IsIntangible ? LayerMask.NameToLayer("IgnoreHitColliders") : gameObject.layer = LayerMask.NameToLayer(_defaultLayer);

            //Clamp health
            if (Health > _maxHealth.FixedValue)
                Health = _maxHealth.FixedValue;
        }

    }
}


