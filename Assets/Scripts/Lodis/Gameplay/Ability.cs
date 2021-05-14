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

    /// <summary>
    /// Abstract class that all abilities inherit from
    /// </summary>
    public abstract class Ability
    {
        //The type describes the strength and input value for the ability
        public BasicAbilityType abilityType;
        public DamageType damageType = DamageType.DEFAULT;
        //Name of the ability
        public string name = "Unassigned";
        //The object that is using the ability
        public GameObject owner = null;
        public MovesetBehaviour ownerMoveset = null;
        //How long the ability should be active for
        public float timeActive = 0;
        //How long does the object that used the ability need before being able to recover
        public float recoverTime = 0;
        //How long does the object that used the ability must wait before the ability activates
        public float startUpTime = 0;
        //If true, this ability can be canceled into others
        public bool canCancel = false;
        //Called when the character begins to use the ability and before the action actually happens
        public UnityAction onBegin = null;
        //Called when the ability is used and the recover time is up
        public UnityAction onEnd = null;
        //Called when the ability's action happens
        public UnityAction onActivate = null;
        //Called when the ability is used and before the character has recovered
        public UnityAction onDeactivate = null;

        private bool _inUse;

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
            yield return new WaitForSeconds(startUpTime);
            onActivate?.Invoke();
            Activate(args);
            yield return new WaitForSeconds(timeActive);
            onDeactivate?.Invoke();
            Deactivate();
            yield return new WaitForSeconds(recoverTime);
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

            ownerMoveset.StartCoroutine(StartAbility(args));
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
