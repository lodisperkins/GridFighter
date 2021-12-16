using Lodis.Input;
using Lodis.Movement;
using Lodis.ScriptableObjects;
using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{
    public class HealthBehaviour : MonoBehaviour
    {
        [Tooltip("The measurement of the amount of damage this object can take or has taken")]
        [SerializeField]
        private float _health;
        [Tooltip("Whether or not this object should be deleted if the health is 0")]
        [SerializeField]
        private bool _destroyOnDeath;
        [Tooltip("Whether or not the health value for this object is above 0")]
        [SerializeField]
        private bool _isAlive = true;
        [Tooltip("Whether or not this object can be damaged or knocked back")]
        [SerializeField]
        private bool _isInvincible;
        private Condition _invincibilityCondition;
        [Tooltip("Whether or not this object is in a stunned state")]
        [SerializeField]
        private bool _stunned;
        private Coroutine _stunRoutine;
        private MovesetBehaviour _moveset;
        private InputBehaviour _input;
        protected GridMovementBehaviour _movement;

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

        public bool IsAlive
        {
            get
            {
                return _isAlive;
            }
        }

        public float Health { get => _health; protected set => _health = value; }
        public bool IsInvincible { get => _isInvincible; }

        private void Start()
        {
            _isAlive = true;
        }

        /// <summary>
        /// Takes damage based on the damage type.
        /// </summary>
        /// <param name="attacker">The name of the object that damaged this object. Used for debugging</param>
        /// <param name="damage">The amount of damage being applied to the object. 
        /// Ring barriers only break if the damage amount is greater than the total health</param>
        /// <param name="knockBackScale"></param>
        /// <param name="hitAngle"></param>
        /// <returns></returns>
        /// <param name="damageType">The type of damage this object will take</param>
        public virtual float TakeDamage(string attacker, float damage, float knockBackScale = 0, float hitAngle = 0, DamageType damageType = DamageType.DEFAULT, float hitStun = 0)
        {
            if (!IsAlive || IsInvincible)
                return 0;

            _health -= damage;

            if (_health < 0)
                _health = 0;

            return damage;
        }

        /// <summary>
        /// Takes damage based on the damage type.
        /// </summary>
        /// <param name="attacker">The name of the object that damaged this object. Used for debugging</param>
        /// <param name="abilityData">The data scriptable object associated with the ability</param>
        /// <param name="damageType">The type of damage this object will take</param>
        public virtual float TakeDamage(string attacker, AbilityData abilityData, DamageType damageType = DamageType.DEFAULT)
        {
            if (!IsAlive || IsInvincible)
                return 0;

            _health -= abilityData.GetCustomStatValue("Damage");

            if (_health < 0)
                _health = 0;

            return abilityData.GetCustomStatValue("Damage");
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
            _movement = GetComponent<GridMovementBehaviour>();

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
            if (Stunned)
                return;

            _stunRoutine = StartCoroutine(ActivateStun(time));
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

        /// <summary>
        /// Makes the object invincible. No damage can be taken by the object
        /// </summary>
        /// <param name="time">How long in seconds the object is invincible for</param>
        public void SetInvincibilityByTimer(float time)
        {
            _isInvincible = true;
            RoutineBehaviour.Instance.StartNewTimedAction(args => _isInvincible = false, TimedActionCountType.SCALEDTIME, time);
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
            if (!knockBackScript || !knockBackScript.Tumbling)
                return;

            float velocityMagnitude = knockBackScript.Physics.LastVelocity.magnitude;;

            //Apply ricochet force and damage
            knockBackScript.TakeDamage(name, velocityMagnitude, 0, 0, DamageType.KNOCKBACK);
        }

        public virtual void OnTriggerEnter(Collider other)
        {
            KnockbackBehaviour knockBackScript = other.gameObject.GetComponent<KnockbackBehaviour>();
            //Checks if the object is not grid moveable and isn't in hit stun
            if (!knockBackScript || !knockBackScript.Tumbling)
                return;

            float velocityMagnitude = knockBackScript.Physics.LastVelocity.magnitude; ;

            //Apply ricochet force and damage
            knockBackScript.TakeDamage(name, velocityMagnitude, 0, 0, DamageType.KNOCKBACK);
        }

        // Update is called once per frame
        public virtual void Update()
        {
            //Death check
            _isAlive = _health > 0;

            if (!IsAlive && _destroyOnDeath)
                Destroy(gameObject);

            //Invincibilty check
            if (_invincibilityCondition != null)
            {
                if (_invincibilityCondition.Invoke())
                {
                    _isInvincible = false;
                    _invincibilityCondition = null;
                }    
            }
        }
    }
}


