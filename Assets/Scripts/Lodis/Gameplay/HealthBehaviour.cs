using Lodis.Input;
using Lodis.Movement;
using Lodis.ScriptableObjects;
using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Lodis.Gameplay
{
    public class HealthBehaviour : MonoBehaviour
    {
        [Tooltip("The measurement of the amount of damage this object can take or has taken")]
        [SerializeField]
        private float _health;
        [Tooltip("The starting amount of damage this object can take or has taken. Set to -1 to start with max health.")]
        [SerializeField]
        private float _startingHealth = -1;
        [Tooltip("The maximum amount of damage this object can take or has taken")]
        [SerializeField]
        private FloatVariable _maxHealth;
        [Tooltip("Whether or not this object should be deleted if the health is 0")]
        [SerializeField]
        private bool _destroyOnDeath;
        [Tooltip("Whether or not the health value for this object is above 0")]
        [SerializeField]
        private bool _isAlive = true;
        [Tooltip("Whether or not this object can be damaged or knocked back")]
        [SerializeField]
        private bool _isInvincible;
        private TimedAction _invincibilityTimer;
        private Condition _invincibilityCondition;
        private Condition _intagibilityCondition;
        [Tooltip("Whether or not this object is in a stunned state")]
        [SerializeField]
        private bool _stunned;
        [SerializeField]
        private bool _isIntangible;
        [SerializeField]
        private Renderer _meshRenderer;
        protected Material _material;
        private Color _defaultColor;
        private Coroutine _stunRoutine;
        private MovesetBehaviour _moveset;
        private InputBehaviour _input;
        private UnityAction _onStun;
        private UnityAction _onDeath;
        private string _defaultLayer;
        [SerializeField]
        private Color _invincibilityColor;
        [SerializeField]
        private Color _intangibilityColor;
        private HitColliderBehaviour _lastCollider;
        protected GridMovementBehaviour Movement;
        protected Condition AliveCondition;
        protected UnityAction _onTakeDamage;
        [FormerlySerializedAs("OnTakeDamage")] [SerializeField]
        protected GridGame.Event OnTakeDamageEvent;


        public bool Stunned 
        {
            get
            {
                return _stunned;
            }
            protected set
            {
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

        public float Health 
        { 
            get => _health;
            protected set
            {
                //Prevent damage if the object is invincible or dead
                if (value < _health && (_isInvincible || !IsAlive))
                {
                    return;
                }

                _health = value;
            }
        }
        public bool IsInvincible { get => _isInvincible; }
        public FloatVariable MaxHealth { get => _maxHealth; }

        public HitColliderBehaviour LastCollider
        {
            get => _lastCollider;
            set => _lastCollider = value;
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

            if (_startingHealth < 0)
                _health = _maxHealth.Value;
            else
                _health = _startingHealth;
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
        public virtual float TakeDamage(GameObject attacker, float damage, float baseKnockBack = 0, float hitAngle = 0, DamageType damageType = DamageType.DEFAULT, float hitStun = 0)
        {
            float damageTaken = _health;

            Health -= damage;

            damageTaken -= Health;

            if (Health < 0)
                _health = 0;

            OnTakeDamageEvent.Raise(gameObject);
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
        public virtual float TakeDamage(HitColliderInfo info, GameObject attacker)
        {
            float damageTaken = _health;

            Health -= info.Damage;

            damageTaken -= Health;

            if (_health < 0)
                _health = 0;

            OnTakeDamageEvent.Raise(gameObject);
            return damageTaken;
        }

        /// <summary>
        /// Takes damage based on the damage type.
        /// </summary>
        /// <param name="attacker">The name of the object that damaged this object. Used for debugging</param>
        /// <param name="abilityData">The data scriptable object associated with the ability</param>
        /// <param name="damageType">The type of damage this object will take</param>
        public virtual float TakeDamage(string attacker, AbilityData abilityData, DamageType damageType = DamageType.DEFAULT)
        {
            float damageTaken = _health;

            Health -= abilityData.GetColliderInfo(0).Damage;

            damageTaken -= Health;

            if (_health < 0)
                _health = 0;

            OnTakeDamageEvent.Raise(gameObject);
            return damageTaken;
        }

        public virtual float Heal(float healthAmount)
        {
            _health = healthAmount;
            return healthAmount;
        }

        /// <summary>
        /// Starts the timer for the movement and input being disabled
        /// </summary>
        /// <param name="time">The amount of time the object is stunned</param>
        protected virtual IEnumerator ActivateStun(float time)
        {
            Stunned = true;
            _moveset = GetComponent<MovesetBehaviour>();
            _input = GetComponent<Input.InputBehaviour>();
            Movement = GetComponent<GridMovementBehaviour>();

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
            if (Movement)
            {
                Movement.DisableMovement(condition => Stunned == false, false, true);
            }


            yield return new WaitForSeconds(time);

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
        public void Stun(float time)
        {
            if (Stunned || IsInvincible)
                return;

            _stunRoutine = StartCoroutine(ActivateStun(time));
            _onStun?.Invoke();
        }

        public virtual void CancelStun()
        {
            if (!Stunned)
                return;

            StopCoroutine(_stunRoutine);

            //Enable components if the actor has them attached
            if (_moveset)
                _moveset.enabled = true;
            if (_input)
                _input.enabled = true;

            Stunned = false;
        }

        public void AddOnStunAction(UnityAction action)
        {
            _onStun += action;
        }

        public void AddOnDeathAction(UnityAction action)
        {
            _onDeath += action;
        }

        /// <summary>
        /// Adds an action to the event called when this object is damaged
        /// </summary>
        /// <param name="action">The new listener to to the event</param>
        public void AddOnTakeDamageAction(UnityAction action)
        {
            _onTakeDamage += action;
        }
        
        /// <summary>
        /// Makes the object invincible. No damage can be taken by the object
        /// </summary>
        /// <param name="time">How long in seconds the object is invincible for</param>
        public void SetInvincibilityByTimer(float time)
        {
            _isInvincible = true;

            if (_invincibilityTimer != null)
                RoutineBehaviour.Instance.StopAction(_invincibilityTimer);

            _invincibilityTimer = RoutineBehaviour.Instance.StartNewTimedAction(args => _isInvincible = false, TimedActionCountType.SCALEDTIME, time);

        }

        /// <summary>
        /// Makes the object invincible. No damage can be taken by the object
        /// </summary>
        /// <param name="condition">Will turn off invincibility when this condition is true</param>
        public void SetInvincibilityByCondition(Condition condition)
        {
            _invincibilityCondition = condition;
            _isInvincible = true;
        }
        
        /// <summary>
        /// Makes the object invincible. No damage can be taken by the object
        /// </summary>
        /// <param name="time">How long in seconds the object is invincible for</param>
        public void SetIntagibilityByTimer(float time)
        {
            _isIntangible = true;
            gameObject.layer = LayerMask.NameToLayer("IgnoreHitColliders");
            RoutineBehaviour.Instance.StartNewTimedAction(args => _isIntangible = false, TimedActionCountType.SCALEDTIME, time);
        }

        /// <summary>
        /// Makes the object invincible. No damage can be taken by the object
        /// </summary>
        /// <param name="condition">Will turn off invincibility when this condition is true</param>
        public void SetIntagibilityByCondition(Condition condition)
        {
            _intagibilityCondition = condition;
            gameObject.layer = LayerMask.NameToLayer("IgnoreHitColliders");
            _isIntangible = true;
        }

        /// <summary>
        /// Deactivates the invincibility and clears the condition event for disabling automatically
        /// </summary>
        public void DisableInvincibility()
        {
            if (!_isInvincible)
                return;

            StopAllCoroutines();
            _invincibilityCondition = null;
            _isInvincible = false;
        }
        public virtual void OnCollisionEnter(Collision collision)
        {
            KnockbackBehaviour knockBackScript = collision.gameObject.GetComponent<KnockbackBehaviour>();
            //Checks if the object is not grid moveable and isn't in hit stun
            if (!knockBackScript || knockBackScript.CurrentAirState != AirState.TUMBLING)
                return;

            float velocityMagnitude = knockBackScript.Physics.LastVelocity.magnitude;;

            //Apply ricochet force and damage
            knockBackScript.TakeDamage(gameObject, velocityMagnitude, 0, 0, DamageType.KNOCKBACK);
        }

        public virtual void OnTriggerEnter(Collider other)
        {
            KnockbackBehaviour knockBackScript = other.gameObject.GetComponent<KnockbackBehaviour>();
            //Checks if the object is not grid moveable and isn't in hit stun
            if (!knockBackScript || knockBackScript.CurrentAirState != AirState.TUMBLING)
                return;

            float velocityMagnitude = knockBackScript.Physics.LastVelocity.magnitude; ;

            //Apply ricochet force and damage
            knockBackScript.TakeDamage(gameObject, velocityMagnitude, 0, 0, DamageType.KNOCKBACK);
        }

        // Update is called once per frame
        public virtual void Update()
        {
            //Death check
            if (IsAlive && Health <= 0)
                _onDeath?.Invoke();

            _isAlive = AliveCondition.Invoke();

            if (!IsAlive && _destroyOnDeath)
                Destroy(gameObject);

            //Invincibilty check
            if (_invincibilityCondition?.Invoke() == true)
            {
                _isInvincible = false;
                _invincibilityCondition = null;
            }  

            //Intangibilty check
            if (_intagibilityCondition?.Invoke() == true)
            {
                _isIntangible = false;
                _intagibilityCondition = null;
            }

            //Update color
            if (IsInvincible && _meshRenderer)
                _material.color = _invincibilityColor;
            else if (_isIntangible && _meshRenderer)
                _material.color = _intangibilityColor;
            else if (_meshRenderer)
                _material.color = _defaultColor;

            gameObject.layer = _isIntangible? LayerMask.NameToLayer("IgnoreHitColliders") : gameObject.layer = LayerMask.NameToLayer(_defaultLayer);

            //Clamp health
            if (Health > _maxHealth.Value)
                Health = _maxHealth.Value;
        }
    }
}


