using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using GridGame.VariableScripts;

namespace GridGame
{
    public class HealthBehaviour : MonoBehaviour
    {
        //the reference of what the objects health should start as
        [FormerlySerializedAs("Health_Ref")] [SerializeField]
        private IntVariable healthRef;

        //the current health of the object
        [FormerlySerializedAs("Health")] public IntVariable health;
        //whether or not the object is alive
        [FormerlySerializedAs("IsAlive")] [SerializeField]
        private bool isAlive;
        public bool isInvincible;
        [SerializeField]
        private bool _canStun = true;
        [SerializeField] private HealthBehaviour _secondaryHealthSource;
        
        public int conversionRate;
        //unity event raised when the object dies
        [SerializeField]
        UnityEvent OnDeath;

        public bool hasRaised;
        [SerializeField] private Event OnObjectDeath;
        [SerializeField] private Event OnHit;
        //the particles that should play on the objects death
        [SerializeField] private ParticleSystem ps;
        [SerializeField] private ParticleSystem ps2;
        public bool healthFull;
        public UnityEvent onStunned;
        public UnityEvent onUnstunned;
        private bool _stunned;
        
        public bool Stunned
        {
            get
            {
                return _stunned;
            }
        }
        
        public IntVariable HealthRef
        {
            get
            {
                return healthRef;
            }
        }

        public bool CanStun
        {
            get
            {
                return _canStun;
            }

            set
            {
                _canStun = value;
            }
        }

        // Use this for initialization
        public void Start()
        {
            health = VariableScripts.IntVariable.CreateInstance(healthRef.Val);
            isAlive = true;
            hasRaised = false;
        }
        //decrements the objects health by the damge amount given
        public void takeDamage(int damageVal)
        {
            if(gameObject == null || isInvincible)
            {
                return;
            }

            if (damageVal >= 5 &&!_stunned && CanStun)
            {
                StartCoroutine(Stun(2.5f));
            }
            
            health.Val -= damageVal;
            OnHit.Raise(gameObject);
            if (health.Val <= 0)
            {
                isAlive = false;
            }
        }

        IEnumerator Stun(float duration)
        {
            if (!CanStun)
                yield break;

            _stunned = true;
            onStunned.Invoke();
            yield return  new WaitForSeconds(duration);
            _stunned = false;
            onUnstunned.Invoke();
        }
        
        //destroys the block within a given time
        public void DestroyBlock(float time)
        {
            PlayDeathParticleSystems(2);
            GameObject tempGameObject = gameObject;
            if (OnObjectDeath != null && hasRaised == false)
            {
                hasRaised = true;
                OnObjectDeath.Raise(gameObject);
            }
            if (OnObjectDeath != null)
            {
                OnObjectDeath.Raise(gameObject);
            }
            Destroy(tempGameObject,time);
        }
        //plays particles after the object has died 
        public void PlayDeathParticleSystems(float duration)
        {
            ParticleSystem tempPs = Instantiate(ps,transform.position,transform.rotation);
            tempPs.Play();
            Destroy(tempPs, duration);
            if (ps2 != null)
            {
                var tempPs2 = Instantiate(ps2,transform.position,transform.rotation);
                tempPs.Play();
                Destroy(tempPs, duration);
            }
        }
        public void ClampedHeal(int val)
        { 
           if (healthFull)
           {
                return;
           }
            health.Val += val;
        }
        public void ClampedHeal(int val,int max)
        {
            if (health.Val >= max)
            {
                health.Val = max;
                return;
            }
            health.Val += val;
        }
        public void UnclampedHeal(int val)
        {
            health.Val += val;
        }
        // Update is called once per frame
        void Update()
        {
            if (isAlive == false && hasRaised == false)
            {
                OnDeath.Invoke();
                hasRaised = true;
                if (OnObjectDeath != null)
                {
                    OnObjectDeath.Raise(gameObject);
                }
            }
            healthFull = health.Val >= healthRef.Val;
        }
    }
}
