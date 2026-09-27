using FixedPoints;
using Lodis.Input;
using Lodis.Movement;
using Lodis.ScriptableObjects;
using Lodis.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Types;
using UnityEditor.Playables;
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

    public class MovesetBehaviour : SimulationBehaviour
    {

        #region Simulation Functions

        public override void Serialize(BinaryWriter bw)
        {
            //Serialize normal flags and values
            _energy.Serialize(bw);
            _burstEnergy.Serialize(bw);
            bw.Write(AbilityInUse);
            bw.Write(_energyChargeEnabled);
            bw.Write(_canDefensiveBurst);
            bw.Write(_canOffensiveBurst);
            bw.Write(_burstLocked);
            bw.Write(_loadingShuffle);
            bw.Write(_deckReloading);
            bw.Write(_canBurstPredict);
            bw.Write(_lastAttackStrength);
            _lastAttackDirection.Serialize(bw);
            bw.Write(_initializedDecks);

            //Serialize the decks.
            _normalDeck.Serialize(bw);
            _specialDeck.Serialize(bw);
            _discardDeck.Serialize(bw);

            // Serialize the last ability we used. If it is a special ability, serialize it as well.
            bw.Write(_lastAbilityInUse != null ? _lastAbilityInUse.abilityData.ID : 0);

            if (_lastAbilityInUse?.abilityData.AbilityType == AbilityType.SPECIAL)
                _lastAbilityInUse?.OnSerialize(bw);


            //Serialize the special ability slots.
            bw.Write(_specialAbilitySlots[0] != null ? _specialAbilitySlots[0].abilityData.ID : 0);
            _specialAbilitySlots[0]?.OnSerialize(bw);

            bw.Write(_specialAbilitySlots[1] != null ? _specialAbilitySlots[1].abilityData.ID : 0);
            _specialAbilitySlots[1]?.OnSerialize(bw);

            bw.Write(_nextAbilitySlot != null ? _nextAbilitySlot.abilityData.ID : 0);
            _nextAbilitySlot?.OnSerialize(bw);
        }


        public override void Deserialize(Deserializer br)
        {
            //Deserialize normal flags and values
            _energy = _energy.Deserialize(br);
            _burstEnergy = _burstEnergy.Deserialize(br);
            _abilityInUse = br.ReadBoolean();
            _energyChargeEnabled = br.ReadBoolean();
            _canDefensiveBurst = br.ReadBoolean();
            _canOffensiveBurst = br.ReadBoolean();
            BurstLocked = br.ReadBoolean();
            _loadingShuffle = br.ReadBoolean();
            _deckReloading = br.ReadBoolean();
            _canBurstPredict = br.ReadBoolean();
            _lastAttackStrength = br.ReadInt32();
            _lastAttackDirection = _lastAttackDirection.Deserialize(br);
            _initializedDecks = br.ReadBoolean();

            //Deserialize the decks.
            _normalDeck.Deserialize(br);
            _specialDeck.Deserialize(br);
            _discardDeck.Deserialize(br);

            //Deserialize the last ability we used.
            int ID = br.ReadInt32();

            if (ID != 0)
            {
                _lastAbilityInUse = GetSerializedAbility(ID);

                //Only special abilities are deserialized.
                //Normal abilities are always deserialized since they never leave the deck.
                if (_lastAbilityInUse.abilityData.AbilityType == AbilityType.SPECIAL)
                    _lastAbilityInUse?.OnDeserialize(br);
            }
            else
            {
                _lastAbilityInUse = null;
            }


            //Deserialize the special ability slots.
            ID = br.ReadInt32();
            if (ID != 0)
            {
                _specialAbilitySlots[0] = GetSerializedAbility(ID);
                _specialAbilitySlots[0]?.OnDeserialize(br);
            }
            else
            {
                _specialAbilitySlots[0] = null;
            }

            ID = br.ReadInt32();
            if (ID != 0)
            {
                _specialAbilitySlots[1] = GetSerializedAbility(ID);
                _specialAbilitySlots[1]?.OnDeserialize(br);
            }
            else
            {
                _specialAbilitySlots[1] = null;
            }

            ID = br.ReadInt32();
            if (ID != 0)
            {
                _nextAbilitySlot = GetSerializedAbility(ID);
                _nextAbilitySlot?.OnDeserialize(br);
            }
            else
            {
                _nextAbilitySlot = null;
            }
        }



        protected override string[] GetLogItems()
        {
            List<string> logItems = new List<string>
            {
                $"Energy: {_energy}",
                $"Burst Energy: {_burstEnergy}",
                $"Ability In Use: {_abilityInUse}",
                $"Energy Charge Enabled: {_energyChargeEnabled}",
                $"Can Defensive Burst: {_canDefensiveBurst}",
                $"Can Offensive Burst: {_canOffensiveBurst}",
                $"Burst Locked: {_burstLocked}",
                $"Loading Shuffle: {_loadingShuffle}",
                $"Deck Reloading: {_deckReloading}",
                $"Can Burst Predict: {_canBurstPredict}",
                $"Last Attack Strength: {_lastAttackStrength}",
                $"Last Attack Direction: {_lastAttackDirection}",
                $"Initialized Decks: {_initializedDecks}"
            };

            AddSerializedDeckLogItems(logItems, "Normal Deck", _normalDeck);
            AddSerializedDeckLogItems(logItems, "Special Deck", _specialDeck);
            // The discard deck is serialized with the other decks because it
            // determines the contents of a later reshuffle.
            AddSerializedDeckLogItems(logItems, "Discard Deck", _discardDeck);

            // Keep this sequence aligned with Serialize. The last ability writes its
            // base payload only when it is a special ability.
            AddSerializedAbilityLogItems(logItems, "Last Ability In Use", _lastAbilityInUse,
                _lastAbilityInUse?.abilityData.AbilityType == AbilityType.SPECIAL);

            // Slots and the next slot always write their base Ability payload when
            // present, regardless of ability type.
            AddSerializedAbilityLogItems(logItems, "Special Ability Slot 0", _specialAbilitySlots[0], true, includeDisplayName: true);
            AddSerializedAbilityLogItems(logItems, "Special Ability Slot 1", _specialAbilitySlots[1], true, includeDisplayName: true);
            AddSerializedAbilityLogItems(logItems, "Next Ability Slot", _nextAbilitySlot, true, includeDisplayName: true);

            return logItems.ToArray();
        }

        #endregion

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
        private Fixed32 _deckReloadTime;
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
        private Fixed32 _energy;
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
        [Tooltip("If true the character can increase their energy from dealing damage")]
        [SerializeField]
        private bool _increaseEnergyFromDamage = true;

        [Header("Burst Energy Settings")]
        [Tooltip("The amount of burst energy this character has")]
        [SerializeField]
        private Fixed32 _burstEnergy;
        [Tooltip("The maximum amount of burst energy characters can have")]
        [SerializeField]
        private FloatVariable _maxBurstEnergyRef;
        [Tooltip("The amount of burst energy regained passively")]
        [SerializeField]
        private FloatVariable _burstEnergyRechargeValue;
        [Tooltip("The rate at which burst energy is regained")]
        [SerializeField]
        private FloatVariable _burstEnergyRechargeRate;
        [Tooltip("How much burst energy it costs to do an offensive burst.")]
        [SerializeField]
        private FloatVariable _offensiveBurstCost;
        [Tooltip("How much burst energy it costs to do an offensive burst.")]
        [SerializeField]
        private FloatVariable _burstHitRechargeRate;
        [Tooltip("The rate at which burst energy is regained when it is set to infinite")]
        [SerializeField]
        private FloatVariable _infiniteBurstEnergyRechargeRate;
        [SerializeField] private bool _canDefensiveBurst = true;
        [SerializeField] private bool _canOffensiveBurst = true;
        [SerializeField] private bool _burstLocked;

        [Header("Sounds")]
        [SerializeField]
        [Tooltip("The sound that will play when shuffling starts any time.")]
        private AudioClip _shuffleStart;
        [SerializeField]
        [Tooltip("The sound that will play when shuffling ends any time.")]
        private AudioClip _shuffleEnd;

        //---
        private UnityAction OnUpdateHand;
        private UnityAction OnManualShuffle;
        private UnityAction _onAutoShuffle;
        private bool _loadingShuffle;
        private bool _canBurstPredict;
        private bool _initializedDecks;

        private CharacterStateMachineBehaviour _stateMachineScript;
        private bool _deckReloading;
        private Movement.GridMovementBehaviour _movementBehaviour;
        private KnockbackBehaviour _knockbackBehaviour;
        private InputBehaviour _inputBehaviour;
        private MovesetBehaviour _opponentMoveset;
        private UnityAction _onUseAbility;
        private AbilityHitEvent _onHit;
        private AbilityHitEvent _onHitTemp;
        private FixedTimeAction _rechargeAction;
        private FixedTimeAction _deckShuffleAction;
        private FixedTimeAction _burstAction;
        private FixedTimeAction _burstLockAction;

        private FVector2 _lastAttackDirection;
        private Fixed32 _currentBurstRechargeRate;
        private Fixed32 _lastAttackStrength;
        private (Ability, object[]) _lastSerializedAbility;
        private bool _serializedAbility;
        private bool _lastBurstInfiniteOption;

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

                if (!value)
                {
                    _rechargeAction?.Stop();
                }
                else if (value && !_energyChargeEnabled)
                {
                    _rechargeAction = FixedPointTimer.StartNewTimedAction(() => Energy += _energyRechargeValue.FixedValue, _energyRechargeRate.FixedValue).Loop();
                }

                _energyChargeEnabled = value;
            }
        }

        public Fixed32 Energy
        { 
            get => _energy;
            private set 
            {
                _energy = value;
                _energy = Fixed32.Clamp(_energy, 0, _maxEnergyRef.FixedValue);
            }
        }

        public Fixed32 BurstEnergy
        { 
            get => _burstEnergy;
            private set 
            {
                _burstEnergy = value;
                _burstEnergy = Fixed32.Clamp(_burstEnergy, 0, _maxBurstEnergyRef.FixedValue);
                //Debug.Log("Burst energy set to" + _burstEnergy);
            }
        }

        public Ability NextAbilitySlot { get => _nextAbilitySlot; private set => _nextAbilitySlot = value; }
        public bool CanDefensiveBurst { get => _canDefensiveBurst; private set => _canDefensiveBurst = value; }
        public bool CanOffensiveBurst { get => _canOffensiveBurst; private set => _canOffensiveBurst = value; }
        public UnityAction OnBurst { get; set; }
        public bool LoadingShuffle { get => _loadingShuffle; }
        public bool DeckReloading { get => _deckReloading; }
        public Transform[] LeftMeleeSpawns { get => _leftMeleeSpawns; set => _leftMeleeSpawns = value; }
        public Transform[] RightMeleeSpawns { get => _rightMeleeSpawns; set => _rightMeleeSpawns = value; }
        public FloatVariable MaxBurstEnergy { get => _maxBurstEnergyRef; }
        public Fixed32 LastAttackStrength { get => _lastAttackStrength; private set => _lastAttackStrength = value; }
        public CharacterAnimationBehaviour AnimationBehaviour { get => _animationBehaviour; set => _animationBehaviour = value; }
        public Deck NormalDeckRef { get => _normalDeckRef; set => _normalDeckRef = value; }
        public Deck SpecialDeckRef { get => _specialDeckRef; set => _specialDeckRef = value; }
        public Deck SpecialDeck { get => _specialDeck; }
        public Transform HeldItemSpawnLeft { get => _heldItemSpawnLeft; private set => _heldItemSpawnLeft = value; }
        public Transform HeldItemSpawnRight { get => _heldItemSpawnRight; private set => _heldItemSpawnRight = value; }

        public static Fixed32 DeckReloadTime { get; private set; }
        public FVector2 LastAttackDirection { get => _lastAttackDirection; private set => _lastAttackDirection = value; }
        public bool BurstLocked { get => _burstLocked; private set => _burstLocked = value; }

        public override string LogName => "MovesetBehaviour";




        /// <summary>
        /// Adds an ability ID and, when Serialize writes one, its complete direct
        /// payload to the moveset serialization log in the same order as the byte
        /// stream. This uses the ability's virtual game-state log so subclass fields
        /// written by OnSerialize are not omitted from the parent Moveset log.
        /// </summary>
        private static void AddSerializedAbilityLogItems(List<string> logItems, string label, Ability ability, bool writesPayload, bool includeDisplayName = false)
        {
            int abilityId = ability != null ? ability.abilityData.ID : 0;
            logItems.Add($"{label} ID: {abilityId}");

            if (includeDisplayName)
                logItems.Add($"{label} Display Name: {ability?.ListDisplayName ?? "None"}");

            if (!writesPayload || ability == null)
                return;

            StringBuilder abilityLog = new StringBuilder();
            ability.OnLogGameState(abilityLog);

            using StringReader reader = new StringReader(abilityLog.ToString());
            // The Moveset log already writes this ability's ID and optional display
            // name above. The remaining lines are the exact serialized state,
            // including fields appended by concrete ability implementations.
            reader.ReadLine();
            reader.ReadLine();

            string abilityLogItem;
            while ((abilityLogItem = reader.ReadLine()) != null)
                logItems.Add($"{label} {abilityLogItem}");
        }

        /// <summary>
        /// Adds the deck's serialized list header, retained-slot order, and ability
        /// payloads to the moveset log without changing deck serialization.
        /// </summary>
        private static void AddSerializedDeckLogItems(List<string> logItems, string label, Deck deck)
        {
            logItems.Add($"{label}:");

            foreach (string deckLogItem in deck.GetSerializedLogItems())
                logItems.Add($"{label} {deckLogItem}");
        }

        protected override void Awake()
        {
            base.Awake();

            _movementBehaviour = GetComponent<Movement.GridMovementBehaviour>();
            _stateMachineScript = GetComponent<CharacterStateMachineBehaviour>();
            _knockbackBehaviour = GetComponent<KnockbackBehaviour>();
            _inputBehaviour = GetComponentInParent<InputBehaviour>();

            _normalDeck = Instantiate(_normalDeckRef);
            _specialDeck = Instantiate(_specialDeckRef);
            _discardDeck = Deck.CreateInstance<Deck>();
            OnUseAbility += _knockbackBehaviour.DisableInvincibility;
            
            _normalDeck.AbilityData.Add((AbilityData)Resources.Load("AbilityData/B_DefensiveBurst_Data"));
            _normalDeck.AbilityData.Add((AbilityData)Resources.Load("AbilityData/B_OffensiveBurst_Data"));


            //Set up other references and parameters
            GameObject target = BlackBoardBehaviour.Instance.GetOpponentForPlayer(gameObject);
            if (!target) return;

            _opponentMoveset = target.GetComponent<MovesetBehaviour>();

        }

        public override void Init()
        {

            DeckReloadTime = _deckReloadTime;

            if (MatchManagerBehaviour.Instance.InfiniteEnergy)
                _energy = _maxEnergyRef.FixedValue;
        }

        public override void Begin()
        {
            base.Begin();

            if (!_initializedDecks)
            {
                _normalDeck.DestroyDeck(true);
                _normalDeck.InitAbilities(Entity);
                _specialDeck.DestroyDeck(true);
                _specialDeck.InitAbilities(Entity);

                _initializedDecks = true;
            }

            //Set up deck
            ResetSpecialDeck();

            //Set up energy meters
            _canDefensiveBurst = true;
            BurstEnergy = MaxBurstEnergy.FixedValue;

            if (!MatchManagerBehaviour.Instance.InfiniteEnergy)
                Energy = _startEnergy.FixedValue;

            _currentBurstRechargeRate = _burstEnergyRechargeRate.FixedValue;

            _rechargeAction = FixedPointTimer.StartNewTimedAction(() => Energy += _energyRechargeValue.FixedValue, 1).Loop();

            //SetBurstCharge();
            _manualShuffleWaitTime = _manualShuffleStartTime + _manualShuffleActiveTime + _manualShuffleRecoverTime;
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

            if (!MatchManagerBehaviour.Instance.InfiniteEnergy)
                Energy = _startEnergy.FixedValue;

            _burstAction?.Stop();
            _rechargeAction?.Stop();

            _rechargeAction = FixedPointTimer.StartNewTimedAction(() => Energy += _energyRechargeValue.FixedValue, 1).Loop();

            BurstEnergy = MaxBurstEnergy.FixedValue; 
            SetBurstCharge();
            _canDefensiveBurst = true;
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

        public void SetBurstCharge()
        {
            if (MatchManagerBehaviour.Instance.InfiniteBurst)
            {
                _currentBurstRechargeRate = _infiniteBurstEnergyRechargeRate.FixedValue;
            }
            else
            {
                _currentBurstRechargeRate = _burstEnergyRechargeRate.FixedValue;
            }

            if (_burstAction == null)
            {
                _burstAction = FixedPointTimer.StartNewTimedAction(() =>
                {
                    BurstEnergy += _burstEnergyRechargeValue.FixedValue;

                    //Stop charging so it doesnt overflow.
                    if (BurstEnergy >= MaxBurstEnergy)
                    {
                        BurstEnergy = MaxBurstEnergy;
                        _burstAction.Stop();
                    }

                    if (MatchManagerBehaviour.Instance != null && MatchManagerBehaviour.Instance.InfiniteBurst)
                    {
                        _burstAction.Duration = _infiniteBurstEnergyRechargeRate.FixedValue;
                    }
                    else
                    {
                        _burstAction.Duration = _burstEnergyRechargeRate.FixedValue;
                    }

                }, _currentBurstRechargeRate).Loop();
            }
            else
            {
                _burstAction.Reset();
            }
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
            if (_specialAbilitySlots[0]?.abilityData.ID == ID)
                return _specialAbilitySlots[0];
            else if (_specialAbilitySlots[1]?.abilityData.ID == ID)
                return _specialAbilitySlots[1];

            return null;
        }

        public bool CheckIfAbilityIDInCurrentSlots(int ID)
        {
            if (_specialAbilitySlots[0]?.abilityData.ID == ID)
                return true;
            else if (_specialAbilitySlots[1]?.abilityData.ID == ID)
                return true;

            return false;
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
        /// Searches both decks and all slots for an ability that matches the condition.
        /// </summary>
        /// <param name="condition">The condition to use to find the ability. Will return the first ability to make this condition true.</param>
        public Ability GetAbility(Condition condition)
        {
            Ability ability = _normalDeck.GetAbilityByCondition(condition);
            
            if (ability == null)
            {
                if (condition.Invoke(SpecialAbilitySlots[0]))
                    return SpecialAbilitySlots[0];
                else if (condition.Invoke(SpecialAbilitySlots[1]))
                    return SpecialAbilitySlots[1];
            }

            if (ability == null && condition.Invoke(_nextAbilitySlot))
                return _nextAbilitySlot;

            if (ability == null)
                ability = _specialDeck.GetAbilityByCondition(condition);

            return ability;
        }

        /// <summary>
        /// Searches both decks and all slots for an ability that matches the condition.
        /// </summary>
        /// <param name="id">The ID to use to find the ability. Will return the first ability to make this condition true.</param>
        public Ability GetAbility(int id)
        {
            Ability ability = _normalDeck.GetAbilityByID(id);
            
            if (ability == null)
            {
                if (SpecialAbilitySlots[0].abilityData.ID == id)
                    return SpecialAbilitySlots[0];
                else if (SpecialAbilitySlots[1].abilityData.ID == id)
                    return SpecialAbilitySlots[1];
            }
            else if (_nextAbilitySlot.abilityData.ID == id)
            {
                return _nextAbilitySlot;
            }

            return _specialDeck.GetAbilityByID(id);
        }

        public int GetSpecialAbilityIndex(Ability ability)
        {
            if (_specialAbilitySlots[0] == ability)
                return 0;
            else if (_specialAbilitySlots[1] == ability)
                return 1;

            return -1;
        }

        public Ability GetSerializedAbility(int id)
        {
            Ability ability = _normalDeck.GetSerializedAbility(id);

            if (ability == null)
                ability = _specialDeck.GetSerializedAbility(id);

            return ability;
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
            if (ability == null)
                return null;

            //Ignore player input if they aren't in a state that can attack
            if (_stateMachineScript.StateMachine.CurrentState != "Idle" && _stateMachineScript.StateMachine.CurrentState != "Attacking" && _stateMachineScript.StateMachine.CurrentState != "Moving" && ability.abilityData.AbilityType != AbilityType.BURST)
                return null;

            if (ability.abilityData.abilityName == "Offensive Burst" && BurstEnergy < _offensiveBurstCost)
            {
                return null;
            }
            else if (ability.abilityData.abilityName == "Defensive Burst" && BurstEnergy != MaxBurstEnergy)
            {
                return null;
            }

            if ((ability.abilityData.abilityName == "Defensive Burst" && !_canDefensiveBurst) || (ability.abilityData.abilityName == "Offensive Burst" && !_canOffensiveBurst))
                return null;

            //Return if there is an ability in use that can't be canceled
            if (_lastAbilityInUse != null)
                if (_lastAbilityInUse.InUse && !_lastAbilityInUse.TryCancel(ability))
                    return _lastAbilityInUse;

            ability.ClearAbilityEvents();

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
                _lastAttackStrength = (Fixed32)args[0];

            OnUseAbility?.Invoke();

            if (_lastAbilityInUse.abilityData.AbilityType == AbilityType.BURST)
            {
                if (_lastAbilityInUse.abilityData.abilityName == "Offensive Burst" && BurstEnergy >= _offensiveBurstCost)
                {
                    BurstEnergy -= _offensiveBurstCost;
                    _canOffensiveBurst = BurstEnergy >= _offensiveBurstCost;

                    OnBurst?.Invoke();

                    //If the burst meter was full that means charging stop so we should restart again.
                    if (_canDefensiveBurst)
                        SetBurstCharge();
                }
                else if (_lastAbilityInUse.abilityData.abilityName == "Defensive Burst")
                {
                    BurstEnergy = 0;
                    OnBurst?.Invoke();
                    SetBurstCharge();
                }

                _canDefensiveBurst = false;
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

            //Find the ability in the deck abd use it
            Ability ability = null;

            if (abilityType == AbilityType.BURST)
            {
                ability = _normalDeck.GetBurstAbility(_stateMachineScript.StateMachine.CurrentState);

                if (ability.abilityData.abilityName == "Offensive Burst" && BurstEnergy < _offensiveBurstCost)
                {
                    return null;
                }
                else if (ability.abilityData.abilityName == "Defensive Burst" && BurstEnergy != MaxBurstEnergy)
                {
                    return null;
                }

                if ((ability.abilityData.abilityName == "Defensive Burst" && !_canDefensiveBurst) || (ability.abilityData.abilityName == "Offensive Burst" && !_canOffensiveBurst))
                    return null;
            }
            else
            {
                ability = _normalDeck.GetAbilityByType(abilityType);
            }

            if (ability == null)
                return null;

            //Return if there is an ability in use that can't be canceled
            if (_lastAbilityInUse != null)
                if (_lastAbilityInUse.InUse && !_lastAbilityInUse.TryCancel(ability))
                    return _lastAbilityInUse;

            ability.ClearAbilityEvents();

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
                _lastAttackStrength = (Fixed32)args[0];

            OnUseAbility?.Invoke();

            if (_lastAbilityInUse.abilityData.AbilityType == AbilityType.BURST)
            {
                if (_lastAbilityInUse.abilityData.abilityName == "Offensive Burst" && BurstEnergy >= _offensiveBurstCost)
                {
                    BurstEnergy -= _offensiveBurstCost;
                    _canOffensiveBurst = BurstEnergy >= _offensiveBurstCost;
                    OnBurst?.Invoke();

                    //If the burst meter was full that means charging stop so we should restart again.
                    if (_canDefensiveBurst)
                        SetBurstCharge();
                }
                else if (_lastAbilityInUse.abilityData.abilityName == "Defensive Burst")
                {
                    BurstEnergy = 0;
                    _canOffensiveBurst = false;
                    OnBurst?.Invoke();
                    SetBurstCharge();
                }

                _canDefensiveBurst = false;
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

            //Find the ability in the deck and use it
            Ability ability = _normalDeck.GetAbilityByName(abilityName);
            if (ability == null)
                return null;

            if (ability.abilityData.abilityName == "Offensive Burst" && BurstEnergy < _offensiveBurstCost)
            {
                return null;
            }
            else if (ability.abilityData.abilityName == "Defensive Burst" && BurstEnergy != MaxBurstEnergy)
            {
                return null;
            }

            if ((ability.abilityData.abilityName == "Defensive Burst" && !_canDefensiveBurst) || (ability.abilityData.abilityName == "Offensive Burst" && !_canOffensiveBurst))
                return null;

            if (LastAbilityInUse?.abilityData.AbilityType == AbilityType.BURST && ability.abilityData.AbilityType == AbilityType.BURST)
                return null;

            ability.ClearAbilityEvents();

            ability.OnHitTemp += IncreaseEnergyFromDamage;

            ability.OnHitTemp += collisionArgs =>
            {
                _onHit?.Invoke(ability, collisionArgs);
                _onHitTemp?.Invoke(ability, collisionArgs);
            };

            //Return if there is an ability in use that can't be canceled
            if (_lastAbilityInUse != null)
            {
                if (_lastAbilityInUse.InUse && !_lastAbilityInUse.TryCancel(ability))
                    return _lastAbilityInUse;
            }
            

            ability.UseAbility(args);
            _lastAbilityInUse = ability;
            if (args?.Length > 1)
                LastAttackDirection = (FVector2)args[1];
            if (args?.Length > 0)
                _lastAttackStrength = (float)args[0];

            OnUseAbility?.Invoke();

            if (_lastAbilityInUse.abilityData.AbilityType == AbilityType.BURST)
            {
                if (_lastAbilityInUse.abilityData.abilityName == "Offensive Burst" && BurstEnergy >= _offensiveBurstCost)
                {
                    BurstEnergy -= _offensiveBurstCost;
                    _canOffensiveBurst = BurstEnergy >= _offensiveBurstCost;
                    OnBurst?.Invoke();

                    //If the burst meter was full that means charging stop so we should restart again.
                    if (_canDefensiveBurst)
                        SetBurstCharge();

                }
                else if (_lastAbilityInUse.abilityData.abilityName == "Defensive Burst")
                {
                    BurstEnergy = 0;
                    OnBurst?.Invoke();
                    SetBurstCharge();
                }

                _canDefensiveBurst = false;
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

            if (ability.currentActivationAmount == 0 && !MatchManagerBehaviour.Instance.InfiniteEnergy)
                _energy -= ability.abilityData.EnergyCost;

            ability.ClearAbilityEvents();

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

            if (!_discardDeck.Contains(_lastAbilityInUse))
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
                {
                    _specialAbilitySlots[0].OnDeckReshuffle();
                    _specialAbilitySlots[0].EndAbility();
                }

                _discardDeck.AddAbility(_specialAbilitySlots[0]);
            }
            if (!_discardDeck.Contains(_specialAbilitySlots[1]) && _specialAbilitySlots[1] != null)
            {
                if (!_specialAbilitySlots[1].MaxActivationAmountReached)
                {
                    _specialAbilitySlots[1].OnDeckReshuffle();
                    _specialAbilitySlots[1].EndAbility();
                }

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

            _deckShuffleAction?.Stop();
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

            _deckShuffleAction = FixedPointTimer.StartNewTimedAction(() => 
            {
                _discardDeck.AddAbilities(_specialDeck);
                _specialDeck.ClearDeck();
                ResetSpecialDeck();
                Sound.SoundManagerBehaviour.Instance.PlaySound(_shuffleEnd);
            }, _manualShuffleStartTime.FixedValue + _manualShuffleActiveTime.FixedValue);
            
            FixedPointTimer.StartNewTimedAction(() => 
            {
                _loadingShuffle = false;
            }, _manualShuffleWaitTime.FixedValue);


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

            if (!MatchManagerBehaviour.Instance.InfiniteEnergy)
                Energy -= actionCost;
            
            action?.Invoke();

            return true;
        }

        public bool TryUseEnergy(float actionCost)
        {
            if (MatchManagerBehaviour.Instance.InfiniteEnergy)
                return true;

            if (Energy < actionCost) return false;

            if (!MatchManagerBehaviour.Instance.InfiniteEnergy)
                Energy -= actionCost;

            return true;
        }

        /// <summary>
        /// Increases the current energy by the damage of the attack received by an ability.
        /// Only works if the collider is owned by the opponent.
        /// </summary>
        public void IncreaseEnergyFromDamage(Collision collision)
        {
            if (!_increaseEnergyFromDamage)
                return;

            HitColliderBehaviour hitCollider = collision.Entity.GetComponent<HitColliderBehaviour>();
            HealthBehaviour health = collision.OtherEntity.GetComponent<HealthBehaviour>();
            bool? invincible = health?.IsInvincible == true;

            if (hitCollider.Spawner != Entity.Data || invincible.GetValueOrDefault() || collision.OtherEntity != _opponentMoveset.Entity.Data || hitCollider.ColliderInfo.DontGiveEnergyOnHit)
            {
                return;
            }

            int decreaseRate =  health.Health == health.MaxHealth ?  10 : 50;

            Energy += hitCollider.ColliderInfo.Damage / decreaseRate;


            if (_opponentMoveset)
            {
                _opponentMoveset.Energy += hitCollider.ColliderInfo.Damage / 100;
                //When being hit burst energy will always increase by the minimum but will scale with heavy attacks.
                Fixed32 rechargeVal = Fixed32.Clamp(_burstHitRechargeRate * hitCollider.ColliderInfo.Damage / 10, _burstHitRechargeRate, MaxBurstEnergy);
                _opponentMoveset.BurstEnergy += rechargeVal;
            }
        }

        public void LockBurst(Fixed32 time)
        {
            BurstLocked = true;

            if (_burstLockAction?.IsActive == true)
            {
                _burstLockAction.Reset();
                return;
            }

            _burstLockAction = FixedPointTimer.StartNewTimedAction(() => BurstLocked = false, time);
        }

        private void Update()
        {

            if (_lastBurstInfiniteOption != MatchManagerBehaviour.Instance.InfiniteBurst)
            {
                SetBurstCharge();
                _lastBurstInfiniteOption = MatchManagerBehaviour.Instance.InfiniteBurst;
            }
            if (MatchManagerBehaviour.Instance.InfiniteEnergy)
            {
                Energy = _startEnergy.FixedValue;
            }



            //Old update
            //Call update for abilities
            if (_lastAbilityInUse != null)
            {
                if (_lastAbilityInUse.InUse)
                {
                    _lastAbilityInUse.Update();
                }
            }
        }

        public override void Tick(Fixed32 dt)
        {
            base.Tick(dt);

            //Reload the deck if there are no cards in the hands or the deck
            if (_specialDeck.Count <= 0 && _specialAbilitySlots[0] == null && _specialAbilitySlots[1] == null && !_deckReloading && NextAbilitySlot == null && !_loadingShuffle)
            {
                _deckReloading = true;
                Sound.SoundManagerBehaviour.Instance.PlaySound(_shuffleStart);
                DiscardActiveSlots();
                _onAutoShuffle?.Invoke();
                _deckShuffleAction = FixedPointTimer.StartNewTimedAction(() =>
                {
                    ResetSpecialDeck();
                    Sound.SoundManagerBehaviour.Instance.PlaySound(_shuffleEnd);
                }, _deckReloadTime);
            }

            if (!CanDefensiveBurst)
            {
                CanDefensiveBurst = BurstEnergy == _maxBurstEnergyRef.FixedValue;
            }

            if (!CanOffensiveBurst)
            {
                CanOffensiveBurst = BurstEnergy >= _offensiveBurstCost;
            }

            if (MatchManagerBehaviour.Instance.SuperInUse || BurstLocked)
            {
                CanDefensiveBurst = false;
                CanOffensiveBurst = false;
            }

            //old fixed update
            //Call fixed update for abilities
            if (_lastAbilityInUse != null)
            {
                if (_lastAbilityInUse.InUse)
                {
                    _lastAbilityInUse.Tick(dt);
                }
            }
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

    }
}
