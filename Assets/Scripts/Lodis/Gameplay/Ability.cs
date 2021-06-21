using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Lodis.Gameplay
{
    /// <summary>
    /// The type of damage an ability uses
    /// </summary>
    public enum DamageType
    {
        DEFAULT,
        KNOCKBACK
    }

    public enum AbilityPhase
    {
        STARTUP,
        ACTIVE,
        RECOVER
    }

    /// <summary>
    /// Abstract class that all abilities inherit from
    /// </summary>
    public abstract class Ability
    {
        //The object that is using the ability
        public GameObject owner = null;
        public MovesetBehaviour ownerMoveset = null;
        public ScriptableObjects.AbilityData abilityData;
        //Called when the character begins to use the ability and before the action actually happens
        public UnityAction onBegin = null;
        //Called when the ability is used and the recover time is up
        public UnityAction onEnd = null;
        //Called when the ability's action happens
        public UnityAction onActivate = null;
        //Called when the ability is used and before the character has recovered
        public UnityAction onDeactivate = null;
        private bool _inUse;

        public AbilityPhase AbilityPhase { get; private set; }


        /// <summary>
        /// Returns false at the end of the ability's recover time
        /// </summary>
        public bool InUse
        {
            get
            {
                return _inUse;
            }
        }
        
        private IEnumerator StartAbility(params object[] args)
        {
            _inUse = true;
            onBegin?.Invoke();
            AbilityPhase = AbilityPhase.STARTUP;
            yield return new WaitForSeconds(abilityData.startUpTime);
            onActivate?.Invoke();
            AbilityPhase = AbilityPhase.ACTIVE;
            Activate(args);
            yield return new WaitForSeconds(abilityData.timeActive);
            onDeactivate?.Invoke();
            AbilityPhase = AbilityPhase.RECOVER;
            Deactivate();
            yield return new WaitForSeconds(abilityData.recoverTime);
            onEnd?.Invoke();
            _inUse = false;
        }

        /// <summary>
        /// Starts the process of using an ability 
        /// </summary>
        /// <param name="args">Any additional information the ability may need</param>
        public void UseAbility(params object[] args)
        {
            //Return if this bullet has no owner
            if (!ownerMoveset)
            {
                Debug.LogError("Owner moveset component not found. Did you forget to call the base Init function?");
                return;
            }
            else if (!abilityData)
            {
                Debug.LogError("Ability data couldn't be found. Did you forget to load the resource?");
                return;
            }

            ownerMoveset.StartCoroutine(StartAbility(args));
        }

        public bool CheckIfAbilityCanBeCanceled()
        {
            switch (AbilityPhase)
            {
                case AbilityPhase.STARTUP:
                    if (abilityData.canCancelStartUp)
                    {
                        return true;
                    }
                    break;
                case AbilityPhase.ACTIVE:
                    if (abilityData.canCancelActive)
                    {
                        return true;
                    }
                    break;
                case AbilityPhase.RECOVER:
                    if (abilityData.canCancelRecover)
                    {
                        return true;
                    }
                    break;
            }

            return false;
        }

        /// <summary>
        /// Tries to cancel the ability
        /// </summary>
        /// <returns>Returns true if the current ability phase can be canceled</returns>
        public bool TryCancel()
        {
            switch (AbilityPhase)
            {
                case AbilityPhase.STARTUP:
                    if (abilityData.canCancelStartUp)
                    {
                        StopAbility();
                        return true;
                    }
                    break;
                case AbilityPhase.ACTIVE:
                    if (abilityData.canCancelActive)
                    {
                        StopAbility();
                        return true;
                    }
                    break;
                case AbilityPhase.RECOVER:
                    if (abilityData.canCancelRecover)
                    {
                        StopAbility();
                        return true;
                    }
                    break;
            }

            return false;
        }

        public void StopAbility()
        {
            ownerMoveset.StopAllCoroutines();
            _inUse = false;
        }

        protected abstract void Activate(params object[] args);
        public virtual void Init(GameObject owner)
        {
            ownerMoveset = owner.GetComponent<MovesetBehaviour>();
        }

        protected virtual void Deactivate() { }
    }
}
