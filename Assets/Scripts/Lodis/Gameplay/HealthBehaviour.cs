using Lodis.Movement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{
    public class HealthBehaviour : MonoBehaviour
    {
        [SerializeField]
        private float _health;
        [SerializeField]
        private bool _destroyOnDeath;
        [SerializeField]
        private bool _isAlive = true;
        [Tooltip("How much this object will reduce the velocity of objects that bounce off of it.")]
        [SerializeField]
        private float _bounceDampen = 2;
        [SerializeField]
        private bool _isInvincible;
        private Condition _invincibilityCondition;
        private bool _stunned;

        public bool Stunned 
        {
            get
            {
                return _stunned;
            }

        }
        public bool IsAlive
        {
            get
            {
                return _isAlive;
            }
        }

        public float BounceDampen { get => _bounceDampen; set => _bounceDampen = value; }
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
        public virtual float TakeDamage(string attacker, float damage, float knockBackScale = 0, float hitAngle = 0, DamageType damageType = DamageType.DEFAULT)
        {
            if (!IsAlive || IsInvincible)
                return 0;

            _health -= damage;

            if (_health < 0)
                _health = 0;

            return damage;
        }

        private IEnumerator SetInvincibility(float time)
        {
            _isInvincible = true;
            yield return new WaitForSeconds(time);
            _isInvincible = false;
        }

        protected virtual IEnumerator ActivateStun(float time)
        {
            MovesetBehaviour moveset = GetComponent<MovesetBehaviour>();
            Input.InputBehaviour inputBehaviour = GetComponent<Input.InputBehaviour>();

            if (moveset)
            {
                moveset.enabled = false;
                moveset.StopAbilityRoutine();
            }
            if (inputBehaviour)
            {
                inputBehaviour.enabled = false;
                inputBehaviour.StopAllCoroutines();
            }


            yield return new WaitForSeconds(time);

            if (moveset)
                moveset.enabled = true;
            if (inputBehaviour)
                inputBehaviour.enabled = true;
        }

        public void Stun(float time)
        {
            StartCoroutine(ActivateStun(time));
        }

        /// <summary>
        /// Makes the object invincible. No damage can be taken by the object
        /// </summary>
        /// <param name="time">How long in seconds the object is invincible for</param>
        public void SetInvincibilityByTimer(float time)
        {
            StartCoroutine(SetInvincibility(time));
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

        public void DisableInvincibility()
        {
            StopAllCoroutines();
            _invincibilityCondition = null;
            _isInvincible = false;
        }

        public virtual void OnCollisionEnter(Collision collision)
        {
            Movement.KnockbackBehaviour knockBackScript = collision.gameObject.GetComponent<KnockbackBehaviour>();
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


