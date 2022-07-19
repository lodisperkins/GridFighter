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
using Lodis.Utility;

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
    [System.Serializable]
    public abstract class Ability
    {
        private bool _inUse;
        private bool _canPlayAnimation;
        private List<HitColliderBehaviour> _colliders;
        private TimedAction _currentTimer;
        protected KnockbackBehaviour _ownerKnockBackScript;
        protected Movement.GridMovementBehaviour _ownerMoveScript;
        protected CharacterAnimationBehaviour _ownerAnimationScript;
        //The object that is using the ability
        public GameObject owner = null;
        public MovesetBehaviour ownerMoveset = null;
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
        public int currentActivationAmount;
        /// <summary>
        /// Called when the ability's collider hits an object
        /// </summary>
        public CollisionEvent OnHit = null;
        public CollisionEvent OnHitTemp = null;

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


        /// <summary>
        /// Gets whether or not this ability has reached its maximum amount of uses
        /// </summary>
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

        /// <summary>
        /// The timed action that is counting down to the next ability phase
        /// </summary>
        public TimedAction CurrentTimer
        {
            get { return _currentTimer; }
        }

        /// <summary>
        /// The phase before an the ability is activated. This is where the character is building up
        /// to the ability's activation
        /// </summary>
        /// <param name="args"></param>
        protected void StartUpPhase(params object[] args)
        {
            _inUse = true;
            onBegin?.Invoke();
            CurrentAbilityPhase = AbilityPhase.STARTUP;
            Start(args);
            _currentTimer = RoutineBehaviour.Instance.StartNewTimedAction(context => ActivePhase(args), TimedActionCountType.SCALEDTIME, abilityData.startUpTime);
        }

        /// <summary>
        /// The phase during the ability activation. This is usually where hit boxes or status effects are spawned. 
        /// </summary>
        /// <param name="args"></param>
        protected void ActivePhase(params object[] args)
        {
            onActivate?.Invoke();
            CurrentAbilityPhase = AbilityPhase.ACTIVE;
            Activate(args);
            _currentTimer = RoutineBehaviour.Instance.StartNewTimedAction(context => RecoverPhase(args), TimedActionCountType.SCALEDTIME, abilityData.timeActive);
        }

        /// <summary>
        /// The phase after the ability activation. This is usually where the character is winding back into idle
        /// after activating the ability
        /// </summary>
        /// <param name="args"></param>
        protected void RecoverPhase(params object[] args)
        {
            CurrentAbilityPhase = AbilityPhase.RECOVER;
            
            if (MaxActivationAmountReached)
                _currentTimer = RoutineBehaviour.Instance.StartNewTimedAction(arguments => EndAbility(), TimedActionCountType.SCALEDTIME, abilityData.recoverTime);
            else
                _currentTimer = RoutineBehaviour.Instance.StartNewTimedAction(arguments => { _inUse = false; Deactivate(); }, TimedActionCountType.SCALEDTIME, abilityData.recoverTime);
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
                throw new Exception("Owner moveset component not found. Did you forget to call the base Init function?");
                return;
            }
            else if (!abilityData)
            {
                throw new Exception("Ability data couldn't be found. Did you forget to load the resource?");
                return;
            }

            StartUpPhase(args);
        }

        /// <summary>
        /// Checks to see if the ability is able to be canceled in the current phase
        /// </summary>
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
        /// Trys to cancel the ability
        /// </summary>
        /// <returns>Returns true if the current ability phase can be canceled</returns>
        public bool TryCancel(Ability nextAbility = null)
        {
            if (nextAbility != null)
            {
                if (nextAbility == this && !abilityData.CanCancelIntoSelf)
                    return false;
                else if (nextAbility.abilityData.AbilityType == AbilityType.SPECIAL && abilityData.CanCancelIntoSpecial)
                    return true;
                else if ((int)nextAbility.abilityData.AbilityType < 8 && abilityData.CanCancelIntoNormal)
                    return true;
            }
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
            RoutineBehaviour.Instance.StopAction(_currentTimer);
            _inUse = false;
        }

        /// <summary>
        /// Stops the ability from moving to the next phase
        /// </summary>
        public virtual void PauseAbilityTimer()
        {
            _currentTimer.Pause();
        }

        /// <summary>
        /// Allows the ability to move on to the next phase
        /// </summary>
        public virtual void UnpauseAbilityTimer()
        {
            _currentTimer.Unpause();
        }

        /// <summary>
        /// Stops ability, calls ending events, and removes the ability from the current ability slot
        /// </summary>
        public virtual void EndAbility()
        {
            RoutineBehaviour.Instance.StopAction(_currentTimer);
            onDeactivate?.Invoke();
            Deactivate();
            onEnd?.Invoke();
            End();
            _inUse = false;
        }

        /// <summary>
        /// Manually activates the animation
        /// </summary>
        public void EnableAnimation()
        {
            _canPlayAnimation = true;
        }

        /// <summary>
        /// Manually deactivates the animation
        /// </summary>
        public void DisableAnimation()
        {
            _canPlayAnimation = false;
        }

        /// <summary>
        /// Initializes base stats and members for the ability
        /// </summary>
        /// <param name="newOwner">The user of the ability</param>
        public virtual void Init(GameObject newOwner)
        {
            owner = newOwner;
            abilityData = (ScriptableObjects.AbilityData)(Resources.Load("AbilityData/" + GetType().Name + "_Data"));
            currentActivationAmount = 0;
            _ownerMoveScript = newOwner.GetComponent<Movement.GridMovementBehaviour>();
            ownerMoveset = newOwner.GetComponent<MovesetBehaviour>();
            _ownerKnockBackScript = newOwner.GetComponent<KnockbackBehaviour>();

            _canPlayAnimation = !abilityData.playAnimationManually;

            _colliders = new List<HitColliderBehaviour>();

            for (int i = 0; i < abilityData.ColliderInfoCount; i++)
            {
                HitColliderBehaviour colliderComponent = new HitColliderBehaviour(abilityData.GetColliderInfo(i), owner);
                colliderComponent.OnHit = OnHit;

                if (abilityData.AbilityType == AbilityType.UNBLOCKABLE)
                    colliderComponent.ColliderInfo.LayersToIgnore |= (1 << LayerMask.NameToLayer("Reflector"));
                else
                    colliderComponent.ColliderInfo.LayersToIgnore |= (1 << LayerMask.NameToLayer("IgnoreHitColliders"));

                _colliders.Add(colliderComponent);
            }
        }

        /// <summary>
        /// Called at the beginning of ability activation
        /// </summary>
        /// <param name="args"></param>
        protected virtual void Start(params object[] args)
        {
            if (!_ownerKnockBackScript)
                return;

            for (int i = 0; i < _colliders.Count; i++)
            {
                _colliders[i].OnHit += arguments => OnHit?.Invoke(arguments);
                _colliders[i].OnHit += arguments => { OnHitTemp?.Invoke(arguments); OnHitTemp = null; };
                if (abilityData.CanCancelOnOpponentHit)
                    OnHit += arguments => 
                    {
                        if ((GameObject)arguments[0] != BlackBoardBehaviour.Instance.GetOpponentForPlayer(owner))
                            return;

                        EndAbility(); 
                    };
            }

            if (abilityData.cancelOnHit)
                _ownerKnockBackScript.AddOnTakeDamageTempAction(EndAbility);
            else if (abilityData.cancelOnFlinch)
                _ownerKnockBackScript.AddOnHitStunTempAction(EndAbility);
            else if (abilityData.cancelOnKnockback)
                _ownerKnockBackScript.AddOnKnockBackStartTempAction(EndAbility);

        }

        /// <summary>
        /// Called when the ability is actually in action. Usually used to spawn hit boxes or status effects
        /// </summary>
        /// <param name="args"></param>
        protected abstract void Activate(params object[] args);

        /// <summary>
        /// Called when the ability is entering its recovering phase after use
        /// </summary>
        protected virtual void Deactivate() { }

        /// <summary>
        /// Called after the ability has recovered and just before the user goes back to idle
        /// </summary>
        protected virtual void End()
        {
            if (_ownerKnockBackScript.CurrentAirState != AirState.TUMBLING)
                _ownerKnockBackScript.RemoveOnKnockBackStartTempAction(EndAbility);

            for (int i = 0; i < _colliders.Count; i++)
            {
                _colliders[i].OnHit -= arguments => OnHit?.Invoke(arguments);
                _colliders[i].OnHit -= arguments => { OnHitTemp?.Invoke(arguments); OnHitTemp = null; };
            }
        }

        /// <summary>
        /// Called in every update for the ability owner
        /// </summary>
        public virtual void Update() { }

        /// <summary>
        /// Called in every fixed update for the ability owner
        /// </summary>
        public virtual void FixedUpdate() { }

        public HitColliderBehaviour GetColliderBehaviourCopy(int index)
        {
            HitColliderBehaviour hitColliderBehaviour = new HitColliderBehaviour();
            HitColliderBehaviour.Copy(_colliders[index], hitColliderBehaviour);
            return hitColliderBehaviour;
        }

        public HitColliderBehaviour GetColliderBehaviourCopy(string name)
        {
            HitColliderBehaviour hitColliderBehaviour = new HitColliderBehaviour();

            for (int i = 0; i < _colliders.Count; i++)
            {
                if (_colliders[i].ColliderInfo.Name == name)
                {
                    HitColliderBehaviour.Copy(_colliders[i], hitColliderBehaviour);
                    return hitColliderBehaviour;
                }
            }

            return null;
        }
    }

#if UNITY_EDITOR
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
#endif
}