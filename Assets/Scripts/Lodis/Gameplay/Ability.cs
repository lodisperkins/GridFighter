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
using Lodis.Sound;
using Lodis.Input;

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
        private List<HitColliderData> _colliderInfo;
        private TimedAction _currentTimer;
        private KnockbackBehaviour _ownerKnockBackScript;
        private Movement.GridMovementBehaviour _ownerMoveScript;
        private CharacterAnimationBehaviour _ownerAnimationScript;
        private InputBehaviour _ownerInput;
        private Vector3 _accessoryStartPosition;
        private Quaternion _accessoryStartRotation;
        private GameObject _accessoryInstance;
        //The object that is using the ability
        public GameObject owner = null;
        public MovesetBehaviour OwnerMoveset = null;
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
        public UnityAction onActivateStart = null;
        /// <summary>
        /// Called when the ability is used and before the character has recovered
        /// </summary>
        public UnityAction onRecover = null;
        public int currentActivationAmount;
        /// <summary>
        /// Called when the ability's collider hits an object
        /// </summary>
        public CollisionEvent OnHit = null;
        public CollisionEvent OnHitTemp = null;
        private bool _opponentHit;

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

        public InputBehaviour OwnerInput { get => _ownerInput; private set => _ownerInput = value; }
        protected KnockbackBehaviour OwnerKnockBackScript { get => _ownerKnockBackScript; private set => _ownerKnockBackScript = value; }
        protected GridMovementBehaviour OwnerMoveScript { get => _ownerMoveScript; private set => _ownerMoveScript = value; }
        protected CharacterAnimationBehaviour OwnerAnimationScript { get => _ownerAnimationScript; private set => _ownerAnimationScript = value; }
        public GameObject AccessoryInstance { get => _accessoryInstance; private set => _accessoryInstance = value; }

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
            SoundManagerBehaviour.Instance.PlaySound(abilityData.ActivateSound);
            _currentTimer = RoutineBehaviour.Instance.StartNewTimedAction(context => ActivePhase(args), TimedActionCountType.SCALEDTIME, abilityData.startUpTime);
            Start(args);
        }

        /// <summary>
        /// The phase during the ability activation. This is usually where hit boxes or status effects are spawned. 
        /// </summary>
        /// <param name="args"></param>
        protected void ActivePhase(params object[] args)
        {
            onActivateStart?.Invoke();
            CurrentAbilityPhase = AbilityPhase.ACTIVE;
            SoundManagerBehaviour.Instance.PlaySound(abilityData.ActiveSound);
            _currentTimer = RoutineBehaviour.Instance.StartNewTimedAction(context => RecoverPhase(args), TimedActionCountType.SCALEDTIME, abilityData.timeActive);
            Activate(args);
        }

        /// <summary>
        /// The phase after the ability activation. This is usually where the character is winding back into idle
        /// after activating the ability
        /// </summary>
        /// <param name="args"></param>
        protected void RecoverPhase(params object[] args)
        {
            CurrentAbilityPhase = AbilityPhase.RECOVER;
            SoundManagerBehaviour.Instance.PlaySound(abilityData.DeactivateSound);

            if (MaxActivationAmountReached)
                _currentTimer = RoutineBehaviour.Instance.StartNewTimedAction(arguments => EndAbility(), TimedActionCountType.SCALEDTIME, abilityData.recoverTime);
            else
                _currentTimer = RoutineBehaviour.Instance.StartNewTimedAction(arguments => _inUse = false, TimedActionCountType.SCALEDTIME, abilityData.recoverTime);

            Recover(args);
        }

        /// <summary>
        /// Starts the process of using an ability 
        /// </summary>
        /// <param name="args">Any additional information the ability may need</param>
        public void UseAbility(params object[] args)
        {
            //Return if this bullet has no owner
            if (!OwnerMoveset)
            {
                throw new Exception("Owner moveset component not found. Did you forget to call the base Init function?");
            }
            else if (!abilityData)
            {
                throw new Exception("Ability data couldn't be found. Did you forget to load the resource?");
            }

            StartUpPhase(args);
        }

        /// <summary>
        /// Checks to see if the ability is able to be canceled in the current phase
        /// </summary>
        public bool CheckIfAbilityCanBeCanceledInPhase()
        {
            return GetCurrentCancelRule()?.ComparePhase(CurrentAbilityPhase) == true;
        }

        /// <summary>
        /// Trys to cancel the ability
        /// </summary>
        /// <returns>Returns true if the current ability phase can be canceled</returns>
        public bool TryCancel(Ability nextAbility = null)
        {
            if (nextAbility?.abilityData.AbilityType == AbilityType.BURST)
            {
                EndAbility();
                return true;
            }

            if (!CheckIfAbilityCanBeCanceledInPhase())
                return false;

            if (GetCurrentCancelRule()?.CanOnlyCancelOnOpponentHit == true && !_opponentHit)
                return false;

            if (nextAbility != null)
            {
                if (nextAbility == this && GetCurrentCancelRule()?.CanCancelIntoSelf == false)
                    return false;
                else if (nextAbility.abilityData.AbilityType == AbilityType.SPECIAL && GetCurrentCancelRule()?.CanCancelIntoSpecial == false)
                    return false;
                else if ((int)nextAbility.abilityData.AbilityType < 8 && GetCurrentCancelRule()?.CanCancelIntoNormal == false)
                    return false;
            }

            EndAbility();
            return true;
        }

        /// <summary>
        /// Stops ability immedialtely without calling any ending events
        /// </summary>
        public virtual void StopAbility()
        {
            if (!_inUse)
                return;

            RoutineBehaviour.Instance.StopAction(_currentTimer);
            _inUse = false;
            currentActivationAmount = 0;
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
            onEnd?.Invoke();
            End();
            _inUse = false;
        }

        /// <summary>
        /// Initializes base stats and members for the ability
        /// </summary>
        /// <param name="newOwner">The user of the ability</param>
        public virtual void Init(GameObject newOwner)
        {
            owner = newOwner;
            abilityData = (ScriptableObjects.AbilityData)(Resources.Load("AbilityData/" + GetType().Name + "_Data"));
            OwnerMoveScript = newOwner.GetComponent<Movement.GridMovementBehaviour>();
            OwnerMoveset = newOwner.GetComponent<MovesetBehaviour>();
            OwnerKnockBackScript = newOwner.GetComponent<KnockbackBehaviour>();
            OwnerAnimationScript = newOwner.GetComponentInChildren<CharacterAnimationBehaviour>();
            _ownerInput = newOwner.GetComponentInParent<InputBehaviour>();

            _canPlayAnimation = !abilityData.playAnimationManually;

            _colliderInfo = new List<HitColliderData>();

            for (int i = 0; i < abilityData.ColliderInfoCount; i++)
            {
                HitColliderData info = abilityData.GetColliderInfo(i);

                if (abilityData.AbilityType == AbilityType.UNBLOCKABLE)
                    info.LayersToIgnore |= (1 << LayerMask.NameToLayer("Reflector"));
                else
                    info.LayersToIgnore |= (1 << LayerMask.NameToLayer("IgnoreHitColliders"));

                info.OwnerAlignement = OwnerMoveScript.Alignment;
                _colliderInfo.Add(info);
            }

            InitializeAccessory();
        }

        private void InitializeAccessory()
        {
            if (!abilityData.Accessory)
                return;


            for (int i = 0; i < owner.transform.childCount; i++)
            {
                Transform child = owner.transform.GetChild(i);

                if (child.CompareTag("Accessory"))
                {
                    _accessoryInstance = child.gameObject;
                    _accessoryStartPosition = _accessoryInstance.transform.localPosition;
                    _accessoryStartRotation = _accessoryInstance.transform.localRotation;
                    break;
                }
            }
        }

        public void ResetAccessory()
        {
            _accessoryInstance.transform.parent = owner.transform;
            _accessoryInstance.transform.localPosition = _accessoryStartPosition;
            _accessoryInstance.transform.localRotation = _accessoryStartRotation;
            EnableAccessory();

        }

        private void Start(params object[] args)
        {
            for (int i = 0; i < _colliderInfo.Count; i++)
            {
                OnHit += arguments =>
                {
                    if ((GameObject)arguments[0] == BlackBoardBehaviour.Instance.GetOpponentForPlayer(owner) && GetCurrentCancelRule()?.CanOnlyCancelOnOpponentHit == true)
                         _opponentHit = true;
                };

                HitColliderData data = _colliderInfo[i];
                data.AddOnHitEvent(arguments =>
                { OnHit?.Invoke(arguments); });
                data.AddOnHitEvent(arguments => { OnHitTemp?.Invoke(arguments); OnHitTemp = null; });
                _colliderInfo[i] = data;
            }

            CurrentAbilityPhase = AbilityPhase.STARTUP;
            _opponentHit = false;
            if (!OwnerKnockBackScript)
                return;

            OwnerKnockBackScript.AddOnTakeDamageTempAction(() => TryDamageCancel(0));
            OwnerKnockBackScript.AddOnHitStunTempAction(() => TryDamageCancel(1));
            OwnerKnockBackScript.AddOnKnockBackStartTempAction(() => TryDamageCancel(2));

            OnStart();
        }

        private void TryDamageCancel(int damageType)
        {
            if (!InUse)
                return;

            if (damageType == 0 && (GetCurrentCancelRule()?.cancelOnHit == true || abilityData.CancelAllOnHit))
                StopAbility();
            else if (damageType == 1 && (GetCurrentCancelRule()?.cancelOnFlinch == true || abilityData.CancelAllOnFlinch))
                StopAbility();
            else if (damageType == 2 && (GetCurrentCancelRule()?.cancelOnKnockback == true || abilityData.CancelAllOnKnockback))
                StopAbility();
        }

        /// <summary>
        /// Called at the beginning of ability activation
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnStart(params object[] args) {}

        /// <summary>
        /// Called when the ability is actually in action. Usually used to spawn hit boxes or status effects
        /// </summary>
        /// <param name="args"></param>
        private void Activate(params object[] args)
        {
            if (args == null || args.Length <= 1 || args[1] == null)
            {
                OnActivate(args);
                return;
            }

            Vector2 attackDirection = (Vector2)args[1];

            if (OwnerMoveScript.Alignment == GridScripts.GridAlignment.RIGHT)
                attackDirection.x *= -1;

            if (attackDirection.magnitude > 0 && (int)abilityData.AbilityType < 8)
            {
                OwnerMoveScript.CanCancelMovement = true;
                OwnerMoveScript.MoveToPanel(OwnerMoveScript.Position + attackDirection);
                OwnerMoveScript.CanCancelMovement = false;
            }


            OwnerMoveScript.AddOnMoveBeginTempAction(() =>
            {
                if (GetCurrentCancelRule()?.CanCancelOnMove == true)
                    EndAbility();
            });

            OnActivate(args);
        }


        /// <summary>
        /// Called when the ability is actually in action. Usually used to spawn hit boxes or status effects
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnActivate(params object[] args){}

        /// <summary>
        /// Called when the ability is entering its recovering phase after use
        /// </summary>
        private void Recover(params object[] args)
        {
            OnRecover(args);
        }

        /// <summary>
        /// Called when the ability is entering its recovering phase after use
        /// </summary>
        protected virtual void OnRecover(params object[] args) { }

        private void End()
        {

            currentActivationAmount = 0;
            if (OwnerKnockBackScript.CurrentAirState != AirState.TUMBLING)
                OwnerKnockBackScript.RemoveOnKnockBackStartTempAction(EndAbility);

            for (int i = 0; i < _colliderInfo.Count; i++)
            {
                _colliderInfo[i].AddOnHitEvent(arguments => OnHit?.Invoke(arguments));
                _colliderInfo[i].AddOnHitEvent(arguments => { OnHitTemp?.Invoke(arguments); OnHitTemp = null; });
            }

            onEnd = null;
            onActivateStart = null;
            onBegin = null;
            onRecover = null;
            OnHit = null;
            OnHitTemp = null;

            OnEnd();
        }

        /// <summary>
        /// Called after the ability has reach max activation limit or is canceled and just before the user goes back to idle
        /// </summary>
        protected virtual void OnEnd(){ }

        /// <summary>
        /// Called in every update for the ability owner
        /// </summary>
        public virtual void Update() { }

        /// <summary>
        /// Called in every fixed update for the ability owner
        /// </summary>
        public virtual void FixedUpdate() { }

        public HitColliderData GetColliderData(int index)
        {
            return _colliderInfo[index];
        }

        public HitColliderData GetColliderData(int index, float statScale)
        {
            return _colliderInfo[index].ScaleStats(statScale);
        }

        public HitColliderData GetColliderData(string name)
        {
            return _colliderInfo.Find(info => info.Name == name);
        }

        /// <summary>
        /// Make the accessory appear and play the spawn effect.
        /// </summary>
        public void EnableAccessory()
        {
            if (!abilityData.Accessory || !_accessoryInstance)
                return;

            GameObject spawnEffect = abilityData.Accessory.SpawnEffect;

            ObjectPoolBehaviour.Instance.GetObject(spawnEffect, _accessoryInstance.transform.position, _accessoryInstance.transform.rotation);

            _accessoryInstance.SetActive(true);
        }

        public CancellationRule GetCurrentCancelRule()
        {
            return abilityData.GetCancellationRule(CurrentAbilityPhase);
        }

        /// <summary>
        /// Make the accessory disappear and play the despawn effect.
        /// </summary>
        public void DisableAccessory()
        {
            if (!abilityData.Accessory || !_accessoryInstance)
                return;

            GameObject despawnEffect = abilityData.Accessory.DespawnEffect;

            ObjectPoolBehaviour.Instance.GetObject(despawnEffect, _accessoryInstance.transform.position, _accessoryInstance.transform.rotation);

            _accessoryInstance.SetActive(false);
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