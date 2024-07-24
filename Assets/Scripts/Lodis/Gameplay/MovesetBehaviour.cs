using FixedPoints;
using Lodis.Movement;
using Lodis.ScriptableObjects;
using Lodis.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Lodis.Gameplay
{
    /// <summary>
    /// A specific classification of an ability.
    /// </summary>
    public enum AbilityType
    {
        WEAKNEUTRAL,
        WEAKSIDE,
        WEAKFORWARD,
        WEAKBACKWARD,
        STRONGNEUTRAL,
        STRONGSIDE,
        STRONGFORWARD,
        STRONGBACKWARD,
        SPECIAL,
        UNBLOCKABLE,
        BURST  
    }

    public enum LimbType
    {
        L_LEG,
        L_HAND,
        R_LEG,
        R_HAND
    }

    /// <summary>
    /// Event used when an ability hits an entity. 
    /// arg[0] = The game object collided with.
    /// arg[1] = The collision script for the object that was collided with
    /// arg[2] = The collider object that was collided with. Is type Collider if trigger and type Collision if not.
    /// arg[3] = The collider behaviour of the object that raised this event. 
    /// arg[4] = The health script of the object that was collided with.
    /// </summary>
    public delegate void AbilityHitEvent(Ability abilityUsed, params object[] collisionArgs);

    public class MovesetBehaviour : MonoBehaviour
    {
        [Header("Deck Settings")]
        [Tooltip("The basic ability deck this character will be using.")]
        [SerializeField]
        private Deck _normalDeckRef;
        private Deck _normalDeck;
        [Tooltip("The special ability deck that this character will be using")]
        [SerializeField]
        private Deck _specialDeckRef;
        private Deck _specialDeck;
        [Tooltip("The deck that stores used abilities")]
        [SerializeField]
        private Deck _discardDeck;
        [Tooltip("The amount of time it will take for the special deck to reload once all abilities are used")]
        [SerializeField]
        private float _deckReloadTime;
        [SerializeField]
        [Tooltip("How long it will take to start manually shuffling.")]
        private FloatVariable _manualShuffleStartTime;
        [SerializeField]
        [Tooltip("How long it will take to activate the manual shuffle.")]
        private FloatVariable _manualShuffleActiveTime;
        [SerializeField]
        [Tooltip("How long it will take to move again after shuffling.")]
        private FloatVariable _manualShuffleRecoverTime;
        private FloatVariable _manualShuffleWaitTime;

        

        [Tooltip("The slots that store the two loaded abilities from the special deck")]
        [SerializeField]
        private Ability[] _specialAbilitySlots = new Ability[2];
        [Tooltip("The slots that store the next two abilities that will be loaded from the special deck")]
        [SerializeField]
        private Ability _nextAbilitySlot;
        [SerializeField]
        private Ability _lastAbilityInUse;
        private float _lastAttackStrength;


        [Header("Ability Casting")]
        [SerializeField]
        private bool _abilityInUse;
        [SerializeField]
        private CharacterAnimationBehaviour _animationBehaviour;
        [Tooltip("This transform is where projectile will spawn by default for this object.")]
        [SerializeField]
        private ProjectileSpawnerBehaviour _projectileSpawner;
        [Tooltip("This transforms where melee hit boxes will spawn for this object.")]
        [SerializeField]
        private Transform[] _leftMeleeSpawns;
        [Tooltip("This transforms where melee hit boxes will spawn for this object.")]
        [SerializeField]
        private Transform[] _rightMeleeSpawns;
        [SerializeField]
        private Transform _heldItemSpawnLeft;
        [SerializeField]
        private Transform _heldItemSpawnRight;

        [Header("Energy Meter Settings")]
        [Tooltip("The amount of energy this character has")]
        [SerializeField]
        private float _energy;
        [Tooltip("The maximum amount of energy characters can have")]
        [SerializeField]
        private FloatVariable _maxEnergyRef;
        [Tooltip("The amount of energy regained passively")]
        [SerializeField]
        private FloatVariable _energyRechargeValue;
        [Tooltip("The amount of energy this character starts with")]
        [SerializeField]
        private FloatVariable _startEnergy;
        [Tooltip("The rate at which energy is regained")]
        [SerializeField]
        private FloatVariable _energyRechargeRate;
        [Tooltip("If true the character can charge energy passively")]
        [SerializeField]
        private bool _energyChargeEnabled = true;

        [Header("Burst Energy Settings")]
        [Tooltip("The amount of burst energy this character has")]
        [SerializeField]
        private float _burstEnergy;
        [Tooltip("The maximum amount of burst energy characters can have")]
        [SerializeField]
        private FloatVariable _maxBurstEnergyRef;
        [Tooltip("The amount of burst energy regained passively")]
        [SerializeField]
        private FloatVariable _burstEnergyRechargeValue;
        [Tooltip("The rate at which burst energy is regained")]
        [SerializeField]
        private FloatVariable _burstEnergyRechargeRate;
        [SerializeField]
        private bool _canBurst = true;

        [Header("Sounds")]
        [SerializeField]
        [Tooltip("The sound that will play when shuffling starts any time.")]
        private AudioClip _shuffleStart;
        [SerializeField]
        [Tooltip("The sound that will play when shuffling ends any time.")]
        private AudioClip _shuffleEnd;

        private UnityAction OnUpdateHand;
        private UnityAction OnManualShuffle;
        private UnityAction _onAutoShuffle;
        private bool _loadingShuffle;

        private CharacterStateMachineBehaviour _stateMachineScript;
        private bool _deckReloading;
        private Movement.GridMovementBehaviour _movementBehaviour;
        private KnockbackBehaviour _knockbackBehaviour;
        private MovesetBehaviour _opponentMoveset;
        private UnityAction _onUseAbility;
        private AbilityHitEvent _onHit;
        private AbilityHitEvent _onHitTemp;
        private TimedAction _rechargeAction;
        private TimedAction _deckShuffleAction;
        private TimedAction _burstAction;

        private FVector2 _lastAttackDirection;

        public ProjectileSpawnerBehaviour ProjectileSpawner => _projectileSpawner;


        /// <summary>
        /// True if there is some ability that is currently active.
        /// </summary>
        public bool AbilityInUse
        {
            get
            {
                if (_lastAbilityInUse != null)
                    return _abilityInUse = _lastAbilityInUse.InUse;

                return _abilityInUse = false;
            }
        }

        public Ability LastAbilityInUse { get => _lastAbilityInUse; }
        public UnityAction OnUseAbility { get => _onUseAbility; set => _onUseAbility = value; }
        public Ability[] SpecialAbilitySlots { get => _specialAbilitySlots; }
        public bool EnergyChargeEnabled 
        { 
            get => _energyChargeEnabled;
            set
            {
                _energyChargeEnabled = value;

                if (!_energyChargeEnabled)
                    RoutineBehaviour.Instance.StopAction(_rechargeAction);
            }
        }

        public float Energy
        { 
            get => _energy;
            private set 
            {
                _energy = value;
                _energy = Mathf.Clamp(_energy, 0, _maxEnergyRef.Value);
            }
        }

        public float BurstEnergy
        { 
            get => _burstEnergy;
            private set 
            {
                _burstEnergy = value;
                _burstEnergy = Mathf.Clamp(_burstEnergy, 0, _maxBurstEnergyRef.Value);
            }
        }

        public Ability NextAbilitySlot { get => _nextAbilitySlot; private set => _nextAbilitySlot = value; }
        public bool CanBurst { get => _canBurst; private set => _canBurst = value; }
        public UnityAction OnBurst { get; set; }
        public bool LoadingShuffle { get => _loadingShuffle; }
        public bool DeckReloading { get => _deckReloading; }
        public Transform[] LeftMeleeSpawns { get => _leftMeleeSpawns; set => _leftMeleeSpawns = value; }
        public Transform[] RightMeleeSpawns { get => _rightMeleeSpawns; set => _rightMeleeSpawns = value; }
        public FloatVariable MaxBurstEnergy { get => _maxBurstEnergyRef; }
        public float LastAttackStrength { get => _lastAttackStrength; private set => _lastAttackStrength = value; }
        public CharacterAnimationBehaviour AnimationBehaviour { get => _animationBehaviour; set => _animationBehaviour = value; }
        public Deck NormalDeckRef { get => _normalDeckRef; set => _normalDeckRef = value; }
        public Deck SpecialDeckRef { get => _specialDeckRef; set => _specialDeckRef = value; }
        public Transform HeldItemSpawnLeft { get => _heldItemSpawnLeft; private set => _heldItemSpawnLeft = value; }
        public Transform HeldItemSpawnRight { get => _heldItemSpawnRight; private set => _heldItemSpawnRight = value; }

        public static float DeckReloadTime { get; private set; }
        public FVector2 LastAttackDirection { get => _lastAttackDirection; private set => _lastAttackDirection = value; }

        private void Awake()
        {
            _movementBehaviour = GetComponent<Movement.GridMovementBehaviour>();
            _stateMachineScript = GetComponent<CharacterStateMachineBehaviour>();
            _knockbackBehaviour = GetComponent<KnockbackBehaviour>();

            DeckReloadTime = _deckReloadTime;

            if (MatchManagerBehaviour.InfiniteEnergy)
                _energy = _maxEnergyRef.Value;
        }

        // Start is called before the first frame update
        private void Start()
        {
            _normalDeck = Instantiate(NormalDeckRef);
            _normalDeck.AbilityData.Add((AbilityData)Resources.Load("AbilityData/B_DefensiveBurst_Data"));
            _normalDeck.AbilityData.Add((AbilityData)Resources.Load("AbilityData/B_OffensiveBurst_Data"));
            _specialDeck = Instantiate(SpecialDeckRef);

            _normalDeck.InitAbilities(gameObject);
            _specialDeck.InitAbilities(gameObject);

            _discardDeck = Deck.CreateInstance<Deck>();

            ResetSpecialDeck();

            _canBurst = true;
            BurstEnergy = MaxBurstEnergy.Value;

            if (!MatchManagerBehaviour.InfiniteEnergy)
                Energy = _startEnergy.Value;

            GameObject target = BlackBoardBehaviour.Instance.GetOpponentForPlayer(gameObject);
            if (!target) return;

            _opponentMoveset = target.GetComponent<MovesetBehaviour>();

            _manualShuffleWaitTime = _manualShuffleStartTime + _manualShuffleActiveTime + _manualShuffleRecoverTime;
            OnUseAbility += _knockbackBehaviour.DisableInvincibility;
        }

        private void OnDisable()
        {
            Debug.Log(gameObject.name + " moveset was disabled.");
        }

        public void ResetAll()
        {
            enabled = true;
            LastAbilityInUse?.EndAbility();
            ManualShuffle(true);

            if (!MatchManagerBehaviour.InfiniteEnergy)
                Energy = _startEnergy.Value;

            RoutineBehaviour.Instance.StopAction(_burstAction);
            RoutineBehaviour.Instance.StopAction(_rechargeAction);
            BurstEnergy = MaxBurstEnergy.Value; 
            _canBurst = true;
        }

        /// <summary>
        /// Load abilities into both decks. Shuffles the special deck
        /// </summary>
        private void ResetSpecialDeck()
        {
            while (_discardDeck.Count > 0)
                _specialDeck.AddAbility(_discardDeck.PopBack());

            _specialDeck.Shuffle();
            _specialAbilitySlots[0] = _specialDeck.PopBack();
            _specialAbilitySlots[1] = _specialDeck.PopBack();
            NextAbilitySlot = _specialDeck.PopBack();
            _deckReloading = false;
            OnUpdateHand?.Invoke();
        }

        /// <summary>
        /// Returns true if there is no ability in use. If there
        /// is an ability in use, returns true if the ability can be canceled
        /// </summary>
        public bool GetCanUseAbility()
        {
            if (!AbilityInUse)
                return true;

            return _lastAbilityInUse.CheckIfAbilityCanBeCanceledInPhase();
        }

        public Transform GetSpawnTransform(LimbType limb)
        {
            Transform limbTransform = null;
            switch (limb)
            {
                case LimbType.L_HAND:
                    limbTransform = LeftMeleeSpawns[1];
                    break;
                case LimbType.L_LEG:
                    limbTransform = LeftMeleeSpawns[0];
                    break;
                case LimbType.R_HAND:
                    limbTransform = RightMeleeSpawns[1];
                    break;
                case LimbType.R_LEG:
                    limbTransform = RightMeleeSpawns[0];
                    break;
            }

            return limbTransform;
        }

        /// <summary>
        /// Checks if the normal deck has an ability that matches the name
        /// </summary>
        /// <param name="abilityName">The name of the ability to search for. Do not use the file name.</param>
        public bool NormalDeckContains(string abilityName)
        {
            return _normalDeck.Contains(abilityName);
        }

        /// <summary>
        /// Checks if the normal deck has an ability that matches the name
        /// </summary>
        /// <param name="abilityName">The name of the ability to search for. Do not use the file name.</param>
        public bool NormalDeckContains(int ID)
        {
            return _normalDeck.Contains(ID);
        }

        /// <summary>
        /// Checks if the special deck has an ability that matches the name
        /// </summary>
        /// <param name="abilityName">The name of the ability to search for. Do not use the file name.</param>
        public bool SpecialDeckContains(string abilityName)
        {
            return _specialDeck.Contains(abilityName);
        }

        /// <summary>
        /// Gets the names of the special abilities currently placed
        /// into the characters hand
        /// </summary>
        public string[] GetAbilityNamesInCurrentSlots()
        {
            string[] names = new string[2];
            if (_specialAbilitySlots[0] != null)
                names[0] = _specialAbilitySlots[0].abilityData.abilityName;
            else
                names[0] = "";

            if (_specialAbilitySlots[1] != null)
                names[1] = _specialAbilitySlots[1].abilityData.abilityName;
            else
                names[1] = "";

            return names;
        }

        /// <summary>
        /// Gets the special ability at the given index in the active ability slots.
        /// </summary>
        public Ability GetAbilityInCurrentSlot(int index)
        {
            if (index < 0 || index >= _specialAbilitySlots.Length)
                return null;

            return _specialAbilitySlots[index];
        }

        /// <summary>
        /// Gets the special ability at the given index in the active ability slots.
        /// </summary>
        public Ability GetAbilityInCurrentSlotByID(int ID)
        {
            if (_specialAbilitySlots[0].abilityData.ID == ID)
                return _specialAbilitySlots[0];
            else if (_specialAbilitySlots[1].abilityData.ID == ID)
                return _specialAbilitySlots[1];

            return null;
        }

        public void AddOnUpdateHandAction(UnityAction action)
        {
            OnUpdateHand += action;
        }

        public void AddOnManualShuffleAction(UnityAction action)
        {
            OnManualShuffle += action;
        }

        public void AddOnAutoShuffleAction(UnityAction action)
        {
            _onAutoShuffle += action;
        }

        public void AddOnHitAction(AbilityHitEvent action)
        {
            _onHit += action;
        }

        public void AddOnHitTempAction(AbilityHitEvent action)
        {
            _onHitTemp += action;
        }

        /// <summary>
        /// Gets the ability from the moveset deck based on the type passed in.
        /// </summary>
        /// <param name="abilityType">The ability type to search for.</param>
        /// <returns></returns>
        public Ability GetAbilityByType(AbilityType abilityType)
        {
            return _normalDeck.GetAbilityByType(abilityType);
        }

        /// <summary>
        /// Gets the ability from the moveset deck based on the type passed in.
        /// </summary>
        /// <returns></returns>
        public Ability GetAbilityByName(string abilityName)
        {
            return _normalDeck.GetAbilityByName(abilityName) ?? _specialDeck.GetAbilityByName(abilityName);
        }

        /// <summary>
        /// Searches both decks for an ability that matches the condition.
        /// </summary>
        /// <param name="condition">The condition to use to find the ability. Will return the first ability to make this condition true.</param>
        public Ability GetAbility(Condition condition)
        {
            Ability ability = _normalDeck.GetAbilityByCondition(condition);

            if (ability == null)
                ability = _specialDeck.GetAbilityByCondition(condition);

            return ability;
        }

        public int GetSpecialAbilityIndex(Ability ability)
        {
            if (_specialAbilitySlots[0] == ability)
                return 0;
            else if (_specialAbilitySlots[1] == ability)
                return 1;

            return -1;
        }

        /// <summary>
        /// Uses a basic ability of the given type if one isn't already in use. If an ability is in use
        /// the ability to use will be activated if the current ability in use can be canceled.
        /// </summary>
        /// <param name="abilityType">The type of basic ability to use</param>
        /// <param name="args">Additional arguments to be given to the basic ability</param>
        /// <returns>The ability used.</returns>
        public Ability UseAbility(int id, params object[] args)
        {
            //Find the ability in the deck abd use it
            Ability currentAbility;

            if (_normalDeck.TryGetAbilityByID(id, out currentAbility))
                UseBasicAbility(currentAbility, args);

            if (_specialAbilitySlots[0]?.abilityData.ID == id)
                return UseSpecialAbility(0, args);
            else if (_specialAbilitySlots[1]?.abilityData.ID == id)
                return UseSpecialAbility(1, args);

            return null;
        }

        /// <summary>
        /// Uses a basic ability of the given type if one isn't already in use. If an ability is in use
        /// the ability to use will be activated if the current ability in use can be canceled.
        /// </summary>
        /// <param name="ability">The type of basic ability to use</param>
        /// <param name="args">Additional arguments to be given to the basic ability</param>
        /// <returns>The ability used.</returns>
        public Ability UseBasicAbility(Ability ability, params object[] args)
        {

            //Ignore player input if they aren't in a state that can attack
            if (_stateMachineScript.StateMachine.CurrentState != "Idle" && _stateMachineScript.StateMachine.CurrentState != "Attacking" && _stateMachineScript.StateMachine.CurrentState != "Moving" && ability.abilityData.AbilityType != AbilityType.BURST)
                return null;
            else if (ability.abilityData.AbilityType == AbilityType.BURST && !_canBurst)
                return null;

            //Return if there is an ability in use that can't be canceled
            if (_lastAbilityInUse != null)
                if (_lastAbilityInUse.InUse && !_lastAbilityInUse.TryCancel(ability))
                    return _lastAbilityInUse;

            if (ability == null)
                return null;

            ability.OnHitTemp += IncreaseEnergyFromDamage;
            ability.OnHitTemp += collisionArgs =>
            {
                _onHit?.Invoke(ability, collisionArgs);
                _onHitTemp?.Invoke(ability, collisionArgs);
            };

            ability.UseAbility(args);
            _lastAbilityInUse = ability;
            if (args?.Length > 1)
                LastAttackDirection = (FVector2)args[1];
            if (args?.Length > 0)
                _lastAttackStrength = (float)args[0];

            OnUseAbility?.Invoke();

            if (_lastAbilityInUse.abilityData.AbilityType == AbilityType.BURST)
            {
                OnBurst?.Invoke();
                _canBurst = false;
                BurstEnergy = 0;
            }

            //Return new ability
            return _lastAbilityInUse;
        }

        /// <summary>
        /// Uses a basic ability of the given type if one isn't already in use. If an ability is in use
        /// the ability to use will be activated if the current ability in use can be canceled.
        /// </summary>
        /// <param name="ability">The type of basic ability to use</param>
        /// <param name="args">Additional arguments to be given to the basic ability</param>
        /// <returns>The ability used.</returns>
        public Ability UseBasicAbility(AbilityType abilityType, params object[] args)
        {

            //Ignore player input if they aren't in a state that can attack
            if (_stateMachineScript.StateMachine.CurrentState != "Idle" && _stateMachineScript.StateMachine.CurrentState != "Attacking" && _stateMachineScript.StateMachine.CurrentState != "Moving" && abilityType != AbilityType.BURST)
                return null;
            else if (abilityType == AbilityType.BURST && !_canBurst)
                return null;

            //Find the ability in the deck abd use it
            Ability currentAbility = null;

            if (abilityType == AbilityType.BURST)
                currentAbility = _normalDeck.GetBurstAbility(_stateMachineScript.StateMachine.CurrentState);
            else
                currentAbility = _normalDeck.GetAbilityByType(abilityType);

            //Return if there is an ability in use that can't be canceled
            if (_lastAbilityInUse != null)
                if (_lastAbilityInUse.InUse && !_lastAbilityInUse.TryCancel(currentAbility))
                    return _lastAbilityInUse;

            if (currentAbility == null)
                return null;

            currentAbility.OnHitTemp += IncreaseEnergyFromDamage;

            currentAbility.OnHitTemp += collisionArgs =>
            {
                _onHit?.Invoke(currentAbility, collisionArgs);
                _onHitTemp?.Invoke(currentAbility, collisionArgs);
            };

            currentAbility.UseAbility(args);

            _lastAbilityInUse = currentAbility;
            if (args?.Length > 1)
                LastAttackDirection = (FVector2)args[1];

            if (args?.Length > 0)
                _lastAttackStrength = (float)args[0];

            OnUseAbility?.Invoke();

            if (_lastAbilityInUse.abilityData.AbilityType == AbilityType.BURST)
            {
                OnBurst?.Invoke();
                _canBurst = false;
                BurstEnergy = 0;
            }

            //Return new ability
            return _lastAbilityInUse;
        }
        
        /// <summary>
        /// Uses a basic ability of the given type if one isn't already in use. If an ability is in use
        /// the ability to use will be activated if the current ability in use can be canceled.
        /// </summary>
        /// <param name="abilityName">The name of the basic ability to use</param>
        /// <param name="args">Additional arguments to be given to the basic ability</param>
        /// <returns>The ability used.</returns>
        public Ability UseBasicAbility(string abilityName, params object[] args)
        {

            //Ignore player input if they aren't in a state that can attack
            if (_stateMachineScript.StateMachine.CurrentState != "Idle" && _stateMachineScript.StateMachine.CurrentState != "Attacking" && abilityName != "EnergyBurst" && _stateMachineScript.StateMachine.CurrentState != "Moving")
                return null;
            else if (abilityName == "EnergyBurst" && !_canBurst)
                return null;

            //Find the ability in the deck and use it
            Ability ability = _normalDeck.GetAbilityByName(abilityName);
            if (ability == null)
                return null;
            ability.OnHitTemp += IncreaseEnergyFromDamage;

            ability.OnHitTemp += collisionArgs =>
            {
                _onHit?.Invoke(ability, collisionArgs);
                _onHitTemp?.Invoke(ability, collisionArgs);
            };

            //Return if there is an ability in use that can't be canceled
            if (_lastAbilityInUse != null)
                if (_lastAbilityInUse.InUse && !_lastAbilityInUse.TryCancel(ability))
                    return _lastAbilityInUse;

            ability.UseAbility(args);
            _lastAbilityInUse = ability;
            if (args?.Length > 1)
                LastAttackDirection = (FVector2)args[1];
            if (args?.Length > 0)
                _lastAttackStrength = (float)args[0];

            OnUseAbility?.Invoke();

            if (_lastAbilityInUse.abilityData.AbilityType == AbilityType.BURST)
            {
                _specialDeck.ClearDeck();
                RemoveAbilityFromSlot(0);
                RemoveAbilityFromSlot(1);
            }
            //Return new ability
            return _lastAbilityInUse;
        }

        /// <summary>
        /// Uses a special ability
        /// </summary>
        /// <param name="abilitySlot">The index of the ability in the characters hand</param>
        /// <param name="args">additional arguments the ability may need</param>
        /// <returns>The ability that was used</returns>
        public Ability UseSpecialAbility(int abilitySlot, params object[] args)
        {
            //Ignore player input if they aren't in a state that can attack
            if (_stateMachineScript.StateMachine.CurrentState != "Idle" && _stateMachineScript.StateMachine.CurrentState != "Attacking" && _stateMachineScript.StateMachine.CurrentState != "Moving")
                return null;

            //Find the ability in the deck and use it
            Ability ability = _specialAbilitySlots[abilitySlot];
            //Return if there is an ability in use that can't be canceled
            if (_lastAbilityInUse != null)
                if (_lastAbilityInUse.InUse && !_lastAbilityInUse.TryCancel(ability))
                    return _lastAbilityInUse;

            if (ability == null)
                return null;
            else if (ability.MaxActivationAmountReached)
                return null;
            else if (ability.abilityData.EnergyCost > _energy && ability.currentActivationAmount == 0)
                return null;

            if (ability.currentActivationAmount == 0 && !MatchManagerBehaviour.InfiniteEnergy)
                _energy -= ability.abilityData.EnergyCost;

            ability.OnHitTemp += IncreaseEnergyFromDamage;
            ability.OnHitTemp += collisionArgs =>
            {
                _onHit?.Invoke(ability, collisionArgs);
                _onHitTemp?.Invoke(ability, collisionArgs);
            };

            ability.UseAbility(args);
            _lastAbilityInUse = ability;

            if (args?.Length > 1)
                LastAttackDirection = (FVector2)args[1];
            ability.currentActivationAmount++;

            if (ability.MaxActivationAmountReached)
                _discardDeck.AddAbility(_lastAbilityInUse);


            if (!_deckReloading)
                ability.onEnd += () => { if (_specialAbilitySlots[abilitySlot] == ability) UpdateHand(abilitySlot); };

            OnUseAbility?.Invoke();

            //Return new ability
            return _lastAbilityInUse;
        }

        /// <summary>
        /// Immediately cancels and ends the current ability in use
        /// </summary>
        public void EndCurrentAbility()
        {
            _lastAbilityInUse?.EndAbility();
        }

        /// <summary>
        /// Trys to charge the next ability and place it in the characters hand.
        /// If there are no abilties left in the deck, it is reloaded 
        /// </summary>
        /// <param name="slot"></param>
        private void UpdateHand(int slot)
        {
            _specialAbilitySlots[slot] = null;
            if (_specialDeck.Count > 0)
            {
                _specialAbilitySlots[slot] = NextAbilitySlot;
                NextAbilitySlot = _specialDeck.PopBack();
            }
            else if (NextAbilitySlot != null)
            {
                _specialAbilitySlots[slot] = NextAbilitySlot;
                NextAbilitySlot = null;
            }

            OnUpdateHand?.Invoke();
        }

        private void DiscardActiveSlots()
        {

            if (!_discardDeck.Contains(_specialAbilitySlots[0]) && _specialAbilitySlots[0] != null)
            {
                if (!_specialAbilitySlots[0].MaxActivationAmountReached)
                    _specialAbilitySlots[0].EndAbility();

                _discardDeck.AddAbility(_specialAbilitySlots[0]);
            }
            if (!_discardDeck.Contains(_specialAbilitySlots[1]) && _specialAbilitySlots[1] != null)
            {
                if (!_specialAbilitySlots[1].MaxActivationAmountReached)
                    _specialAbilitySlots[1].EndAbility();

                _discardDeck.AddAbility(_specialAbilitySlots[1]);
            }
            if (!_discardDeck.Contains(NextAbilitySlot) && NextAbilitySlot != null)
                _discardDeck.AddAbility(NextAbilitySlot);

            _specialAbilitySlots[0] = null;
            _specialAbilitySlots[1] = null;
            NextAbilitySlot = null;
        }

        public void ManualShuffle(bool instantShuffle = false)
        {
            if (LoadingShuffle)
                return;

            RoutineBehaviour.Instance.StopAction(_deckShuffleAction);
            DiscardActiveSlots();
            Sound.SoundManagerBehaviour.Instance.PlaySound(_shuffleStart);

            if (instantShuffle)
            {
                _discardDeck.AddAbilities(_specialDeck);
                _specialDeck.ClearDeck();
                ResetSpecialDeck();
                _loadingShuffle = false;
                return;
            }

            _loadingShuffle = true;
            OnUpdateHand?.Invoke();
            OnManualShuffle?.Invoke();

            _deckShuffleAction = RoutineBehaviour.Instance.StartNewTimedAction(args => 
            {
                _discardDeck.AddAbilities(_specialDeck);
                _specialDeck.ClearDeck();
                ResetSpecialDeck();
                Sound.SoundManagerBehaviour.Instance.PlaySound(_shuffleEnd);
            }, TimedActionCountType.SCALEDTIME, _manualShuffleStartTime + _manualShuffleActiveTime);
            
            RoutineBehaviour.Instance.StartNewTimedAction(args => 
            {
                _loadingShuffle = false;
            }, TimedActionCountType.SCALEDTIME, _manualShuffleWaitTime);


            _movementBehaviour.DisableMovement(condition => !_loadingShuffle, true, true);
        }

        /// <summary>
        /// Removes the ability from the characters hand.
        /// Trys to load a new ability if the deck isn't reloading
        /// </summary>
        /// <param name="index">The index of the ability in the players hnad</param>
        public void RemoveAbilityFromSlot(int index)
        {
            _specialAbilitySlots[index] = null;
            if (!_deckReloading)
                UpdateHand(index);
        }

        /// <summary>
        /// Removes the ability from the characters hand.
        /// Trys to load a new ability if the deck isn't reloading
        /// </summary>
        /// <param name="ability">The reference to the ability to remove</param>
        public void RemoveAbilityFromSlot(Ability ability)
        {
            for (int i = 0; i < _specialAbilitySlots.Length; i++)
            {
                if (_specialAbilitySlots[i] == ability)
                {
                    RemoveAbilityFromSlot(i);
                    if (!_deckReloading)
                        UpdateHand(i);
                }
            }
        }

        public bool TryUseEnergyForAction(UnityAction action, float actionCost)
        {
            if (Energy < actionCost) return false;

            if (!MatchManagerBehaviour.InfiniteEnergy)
                Energy -= actionCost;
            
            action?.Invoke();

            return true;
        }

        public bool TryUseEnergy(float actionCost)
        {
            if (Energy < actionCost) return false;

            if (!MatchManagerBehaviour.InfiniteEnergy)
                Energy -= actionCost;

            return true;
        }

        /// <summary>
        /// Increases the current energy by the damage of the attack received by an ability.
        /// Only works if the collider is owned by the opponent.
        /// </summary>
        public void IncreaseEnergyFromDamage(params object[] args)
        {
            HitColliderBehaviour hitCollider = (HitColliderBehaviour)args[3];
            HealthBehaviour health = (HealthBehaviour)args[4];
            bool? invincible = health?.IsInvincible == true;

            if (hitCollider.Owner != gameObject || invincible.GetValueOrDefault()) return;

            Energy += hitCollider.ColliderInfo.Damage / 50;

            if (_opponentMoveset)
            {
                _opponentMoveset.Energy += hitCollider.ColliderInfo.Damage / 200;
                _opponentMoveset.BurstEnergy += hitCollider.ColliderInfo.Damage / 5;
            }
        }

        public void CancelCurrentBurstCharge()
        {
            RoutineBehaviour.Instance.StopAction(_rechargeAction);
        }

        private void FixedUpdate()
        {
            //Call fixed update for abilities
            if (_lastAbilityInUse != null)
            {
                if (_lastAbilityInUse.InUse)
                {
                    _lastAbilityInUse.FixedUpdate();
                }
            }
        }

        private void Update()
        {

            //Call update for abilities
            if (_lastAbilityInUse != null)
            {
                if (_lastAbilityInUse.InUse)
                {
                    _lastAbilityInUse.Update();
                }
            }

            float burstEnergyRechargeRate = _burstEnergyRechargeRate;

            if (MatchManagerBehaviour.Instance.InfiniteBurst)
            {
                burstEnergyRechargeRate /= 100;
            }

            bool timerActive = (_rechargeAction?.GetEnabled()).GetValueOrDefault();

            if (!timerActive && EnergyChargeEnabled)
                _rechargeAction = RoutineBehaviour.Instance.StartNewTimedAction(timedEvent => Energy += _energyRechargeValue.Value, TimedActionCountType.SCALEDTIME, _energyRechargeRate.Value);

            timerActive = (_burstAction?.GetEnabled()).GetValueOrDefault();

            if (!timerActive)
                _burstAction = RoutineBehaviour.Instance.StartNewTimedAction(timedEvent => BurstEnergy += _burstEnergyRechargeValue.Value, TimedActionCountType.SCALEDTIME, burstEnergyRechargeRate);

            //Reload the deck if there are no cards in the hands or the deck
            if (_specialDeck.Count <= 0 && _specialAbilitySlots[0] == null && _specialAbilitySlots[1] == null && !_deckReloading && NextAbilitySlot == null && !_loadingShuffle)
            {
                _deckReloading = true;
                Sound.SoundManagerBehaviour.Instance.PlaySound(_shuffleStart);
                DiscardActiveSlots();
                _onAutoShuffle?.Invoke();
                _deckShuffleAction = RoutineBehaviour.Instance.StartNewTimedAction(timedEvent =>
                {
                    ResetSpecialDeck();
                    Sound.SoundManagerBehaviour.Instance.PlaySound(_shuffleEnd);
                }, TimedActionCountType.SCALEDTIME, _deckReloadTime);
            }

            if (!CanBurst)
                CanBurst = BurstEnergy == _maxBurstEnergyRef.Value;

            

            if (MatchManagerBehaviour.Instance.SuperInUse)
                CanBurst = false;
        }
    }
}