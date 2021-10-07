using System.Collections;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEditor;
using System;
using System.IO;
using Lodis.ScriptableObjects;
using Lodis.Movement;

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
        protected KnockbackBehaviour _ownerKnockBackScript;
        public ScriptableObjects.AbilityData abilityData;
        /// <summary>
        /// Called when the character begins to use the ability and before the action actually happens
        /// </summary>
        public UnityAction onBegin = null;
        /// <summary>
        /// Called when the ability is used and the recover time is up
        /// </summary>
        public UnityAction onEnd = null;
        /// <summary>
        /// Called when the ability's action happens
        /// </summary>
        public UnityAction onActivate = null;
        /// <summary>
        /// Called when the ability is used and before the character has recovered
        /// </summary>
        public UnityAction onDeactivate = null;
        private bool _inUse;
        protected Movement.GridMovementBehaviour _ownerMoveScript;
        protected CharacterAnimationBehaviour _ownerAnimationScript;
        public int currentActivationAmount;
        private bool _canPlayAnimation;

        public AbilityPhase CurrentAbilityPhase { get; private set; }

        /// <summary>
        /// If true, this ability is allowed to play its animation
        /// </summary>
        public bool CanPlayAnimation
        {
            get
            {
                return _canPlayAnimation;
            }
        }

        public bool MaxActivationAmountReached
        {
            get
            {
                return currentActivationAmount >= abilityData.maxActivationAmount;
            }
        }

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
            CurrentAbilityPhase = AbilityPhase.STARTUP;
            Start(args);
            yield return new WaitForSeconds(abilityData.startUpTime);
            onActivate?.Invoke();
            CurrentAbilityPhase = AbilityPhase.ACTIVE;
            Activate(args);
            yield return new WaitForSeconds(abilityData.timeActive);
            onDeactivate?.Invoke();
            CurrentAbilityPhase = AbilityPhase.RECOVER;
            Deactivate();
            yield return new WaitForSeconds(abilityData.recoverTime);
            End();
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

            ownerMoveset.StartAbilityCoroutine(StartAbility(args));
        }

        public bool CheckIfAbilityCanBeCanceled()
        {
            switch (CurrentAbilityPhase)
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
            switch (CurrentAbilityPhase)
            {
                case AbilityPhase.STARTUP:
                    if (abilityData.canCancelStartUp)
                    {
                        EndAbility();
                        return true;
                    }
                    break;
                case AbilityPhase.ACTIVE:
                    if (abilityData.canCancelActive)
                    {
                        EndAbility();
                        return true;
                    }
                    break;
                case AbilityPhase.RECOVER:
                    if (abilityData.canCancelRecover)
                    {
                        EndAbility();
                        return true;
                    }
                    break;
            }

            return false;
        }

        /// <summary>
        /// Stops ability immedialtely without calling any ending events
        /// </summary>
        public virtual void StopAbility()
        {
            ownerMoveset.StopAbilityRoutine();
            _inUse = false;
        }

        /// <summary>
        /// Stops ability, calls ending events, and removes the ability from the current ability slot
        /// </summary>
        public virtual void EndAbility()
        {
            ownerMoveset.StopAbilityRoutine();
            currentActivationAmount = abilityData.maxActivationAmount;
            onDeactivate?.Invoke();
            Deactivate();
            onEnd?.Invoke();
            End();
            _inUse = false;
        }

        public void EnableAnimation()
        {
            _canPlayAnimation = true;
        }

        public virtual void Init(GameObject newOwner)
        {
            owner = newOwner;
            abilityData = (ScriptableObjects.AbilityData)(Resources.Load("AbilityData/" + GetType().Name + "_Data"));
            currentActivationAmount = 0;
            _ownerMoveScript = newOwner.GetComponent<Movement.GridMovementBehaviour>();
            ownerMoveset = newOwner.GetComponent<MovesetBehaviour>();
            _ownerKnockBackScript = newOwner.GetComponent<KnockbackBehaviour>();

            _canPlayAnimation = !abilityData.playAnimationManually;
        }

        protected virtual void Start(params object[] args)
        {
            if (abilityData.cancelOnHit && _ownerKnockBackScript)
                _ownerKnockBackScript.AddOnTakeDamageTempAction(EndAbility);
            else if (abilityData.cancelOnKnockback && _ownerKnockBackScript)
                _ownerKnockBackScript.AddOnKnockBackStartTempAction(EndAbility);

        }

        protected abstract void Activate(params object[] args);

        protected virtual void Deactivate() { }

        protected virtual void End() 
        {
            if (!_ownerKnockBackScript.InHitStun)
                _ownerKnockBackScript.RemoveOnKnockBackStartTempAction(EndAbility);
        }

        public virtual void Update() { }

        public virtual void FixedUpdate() { }
    }

    public class CustomAssetModificationProcessor : UnityEditor.AssetModificationProcessor
    {
        private static string _nameOfClass;

        static void OnWillCreateAsset(string assetName)
        {
            //If the file wasn't created in the abilities folder, return
            if (!assetName.Contains("Assets/Scripts/Lodis/Gameplay/Abilities/"))
                return;

            //Break apart the string to get the name of the class 
            string[] substrings = assetName.Split('/', '.');
            _nameOfClass = substrings[substrings.Length - 3];

            //Write the name of the class to a text file for later use
            Stream stream = File.Open("Assets/Resources/LastAssetCreated.txt", FileMode.OpenOrCreate);
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(_nameOfClass);

            //Close the file
            writer.Close();
        }

        /// <summary>
        /// Creates data for a recently created ability if none exists already
        /// </summary>
        [UnityEditor.Callbacks.DidReloadScripts]
        public static void TryCreateAbilityData()
        {
            //Return if there is no text file contatining the ability class name
            if (!File.Exists("Assets/Resources/LastAssetCreated.txt"))
                return;

            //Initialize stream reader
            Stream stream = File.Open("Assets/Resources/LastAssetCreated.txt", FileMode.Open);
            StreamReader reader = new StreamReader(stream);

            //Stores the ability class name
            _nameOfClass = reader.ReadToEnd();

            //Close reader and delete unneeded file
            reader.Close();
            File.Delete("Assets/Resources/LastAssetCreated.txt");

            //Return if no name was found in the text file
            if (_nameOfClass == "")
                return;

            //Add the namespace to get the full class name for the ability
            string className = "Lodis.Gameplay." + _nameOfClass;
            //Find the new ability type using the full class name
            Type assetType = Type.GetType(className);

            //Get a reference to the base types
            Type baseType = Type.GetType("Lodis.Gameplay.Ability");
            Type projectileType = Type.GetType("Lodis.Gameplay.ProjectileAbility");

            //Check if there is already an ability data asset for this ability
            string[] results = AssetDatabase.FindAssets(_nameOfClass + "_Data", new[] { "Assets/Resources/AbilityData" });
            if (results.Length > 0)
            {
                return;
            }

            //If there is no ability data, create an based on it's base type
            AbilityData newData = null;
            if (assetType.BaseType == baseType)
                newData = ScriptableObject.CreateInstance<AbilityData>();
            else if (assetType.BaseType == projectileType)
                newData = ScriptableObject.CreateInstance<ProjectileAbilityData>();

            //If the instance was created successfully, create a new asset using the instance 
            if (newData != null)
            {
                AssetDatabase.CreateAsset(newData, "Assets/Resources/AbilityData/" + _nameOfClass + "_Data.Asset");
                Selection.activeObject = AssetDatabase.LoadMainAssetAtPath("Assets/Resources/AbilityData/" + _nameOfClass + "_Data.Asset");
                Debug.Log("Generated ability data for " + _nameOfClass);
            }

        }

        
    }
}
