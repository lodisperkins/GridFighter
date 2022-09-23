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
    /// Abilities that are not in the special ability deck, but are
    /// a part of the characters normal moveset.
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

    public class MovesetBehaviour : MonoBehaviour
    {
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
        [Tooltip("The slots that store the two loaded abilities from the special deck")]
        [SerializeField]
        private Ability[] _specialAbilitySlots = new Ability[2];
        [Tooltip("The slots that store the next two abilities that will be loaded from the special deck")]
        [SerializeField]
        private Ability _nextAbilitySlot;
        [SerializeField]
        private Ability _lastAbilityInUse;
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
        [Tooltip("The amount of time it will take for the special deck to reload once all abilities are used")]
        [SerializeField]
        private float _deckReloadTime;
        [Tooltip("The amount of energy this character has")]
        [SerializeField]
        private float _energy;
        [Tooltip("The maximum amount of energy characters can have")]
        [SerializeField]
        private FloatVariable _maxEnergyRef;
        [Tooltip("The amount of energy regained passively")]
        [SerializeField]
        private FloatVariable _energyRechargeValue;
        [Tooltip("The rate at which energy is regained")]
        [SerializeField]
        private FloatVariable _energyRechargeRate;
        [Tooltip("If true the character can charge energy passively")]
        [SerializeField]
        private bool _energyChargeEnabled = true;
        [SerializeField]
        private bool _canBurst;
        [SerializeField]
        private float _burstChargeTime;
        [SerializeField]
        [Tooltip("How long the player will wait before beginning a manual shuffle.")]
        private float _shuffleWaitTime;
        private UnityAction OnUpdateHand;
        private bool _loadingShuffle;

        private CharacterStateMachineBehaviour _stateMachineScript;
        private bool _deckReloading;
        private Movement.GridMovementBehaviour _movementBehaviour;
        private MovesetBehaviour _opponentMoveset;
        private UnityAction _onUseAbility;
        private TimedAction _rechargeAction;
        private TimedAction _deckShuffleAction;

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

        public Ability NextAbilitySlot { get => _nextAbilitySlot; private set => _nextAbilitySlot = value; }

        public float BurstChargeTime { get => _burstChargeTime; private set => _burstChargeTime = value; }
        public bool CanBurst { get => _canBurst; private set => _canBurst = value; }
        public UnityAction OnBurst { get; set; }
        public bool LoadingShuffle { get => _loadingShuffle; }
        public bool DeckReloading { get => _deckReloading; }
        public Transform[] LeftMeleeSpawns { get => _leftMeleeSpawns; }
        public Transform[] RightMeleeSpawns { get => _rightMeleeSpawns; }

        private void Awake()
        {
            _movementBehaviour = GetComponent<Movement.GridMovementBehaviour>();
            _stateMachineScript = GetComponent<CharacterStateMachineBehaviour>();

            if (GameManagerBehaviour.InfiniteEnergy)
                _energy = _maxEnergyRef.Value;
        }

        // Start is called before the first frame update
        private void Start()
        {
            _normalDeck = Instantiate(_normalDeckRef);
            _normalDeck.AbilityData.Add((AbilityData)Resources.Load("AbilityData/B_EnergyBurst_Data"));
            _specialDeck = Instantiate(_specialDeckRef);
            _normalDeck.InitAbilities(gameObject);
            _specialDeck.InitAbilities(gameObject);
            _discardDeck = Deck.CreateInstance<Deck>();
            ResetSpecialDeck();
            RoutineBehaviour.Instance.StartNewTimedAction(arguments => _canBurst = true, TimedActionCountType.SCALEDTIME, _burstChargeTime);

            GameObject target = BlackBoardBehaviour.Instance.GetOpponentForPlayer(gameObject);
            if (!target) return;

            _opponentMoveset = target.GetComponent<MovesetBehaviour>();
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

            return _lastAbilityInUse.CheckIfAbilityCanBeCanceled();
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


        public void AddOnUpdateHandAction(UnityAction action)
        {
            OnUpdateHand += action;
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
        /// Uses a basic ability of the given type if one isn't already in use. If an ability is in use
        /// the ability to use will be activated if the current ability in use can be canceled.
        /// </summary>
        /// <param name="abilityType">The type of basic ability to use</param>
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
            Ability currentAbility = _normalDeck.GetAbilityByType(abilityType);
            //Return if there is an ability in use that can't be canceled
            if (_lastAbilityInUse != null)
                if (_lastAbilityInUse.InUse && !_lastAbilityInUse.TryCancel(currentAbility))
                    return _lastAbilityInUse;

            if (currentAbility == null)
                return null;

            currentAbility.OnHitTemp += IncreaseEnergyFromDamage;

            currentAbility.UseAbility(args);
            _lastAbilityInUse = currentAbility;

            OnUseAbility?.Invoke();

            if (_lastAbilityInUse.abilityData.AbilityType == AbilityType.BURST)
            {
                OnBurst?.Invoke();
                _canBurst = false;
                RoutineBehaviour.Instance.StartNewTimedAction(arguments => _canBurst = true, TimedActionCountType.SCALEDTIME, _burstChargeTime);
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
            if (_stateMachineScript.StateMachine.CurrentState != "Idle" && _stateMachineScript.StateMachine.CurrentState != "Attacking" && abilityName != "EnergyBurst")
                return null;
            else if (abilityName == "EnergyBurst" && _deckReloading)
                return null;

            //Find the ability in the deck and use it
            Ability currentAbility = _normalDeck.GetAbilityByName(abilityName);
            if (currentAbility == null)
                return null;
            currentAbility.OnHitTemp += IncreaseEnergyFromDamage;
            
            //Return if there is an ability in use that can't be canceled
            if (_lastAbilityInUse != null)
                if (_lastAbilityInUse.InUse && !_lastAbilityInUse.TryCancel(currentAbility))
                    return _lastAbilityInUse;

            currentAbility.UseAbility(args);
            _lastAbilityInUse = currentAbility;

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
            if (_stateMachineScript.StateMachine.CurrentState != "Idle" && _stateMachineScript.StateMachine.CurrentState != "Attacking")
                return null;

            //Find the ability in the deck and use it
            Ability currentAbility = _specialAbilitySlots[abilitySlot];
            //Return if there is an ability in use that can't be canceled
            if (_lastAbilityInUse != null)
                if (_lastAbilityInUse.InUse && !_lastAbilityInUse.TryCancel(currentAbility))
                    return _lastAbilityInUse;

            if (currentAbility == null)
                return null;
            else if (currentAbility.MaxActivationAmountReached)
                return null;
            else if (currentAbility.abilityData.EnergyCost > _energy && currentAbility.currentActivationAmount == 0)
                return null;

            if (currentAbility.currentActivationAmount == 0 && !GameManagerBehaviour.InfiniteEnergy)
                _energy -= currentAbility.abilityData.EnergyCost;

            currentAbility.OnHitTemp += IncreaseEnergyFromDamage;
            currentAbility.UseAbility(args);
            _lastAbilityInUse = currentAbility;

            currentAbility.currentActivationAmount++;

            if (currentAbility.MaxActivationAmountReached)
                _discardDeck.AddAbility(_lastAbilityInUse);


            if (!_deckReloading)
                currentAbility.onEnd += () => { if (_specialAbilitySlots[abilitySlot] == currentAbility) UpdateHand(abilitySlot); };

            OnUseAbility?.Invoke();

            //Return new ability
            return _lastAbilityInUse;
        }

        // ReSharper disable Unity.PerformanceAnalysis
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
                    _specialAbilitySlots[0].StopAbility();

                _discardDeck.AddAbility(_specialAbilitySlots[0]);
            }
            if (!_discardDeck.Contains(_specialAbilitySlots[1]) && _specialAbilitySlots[1] != null)
            {
                if (!_specialAbilitySlots[1].MaxActivationAmountReached)
                    _specialAbilitySlots[1].StopAbility();

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
            RoutineBehaviour.Instance.StopAction(_deckShuffleAction);
            DiscardActiveSlots();

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

            _deckShuffleAction = RoutineBehaviour.Instance.StartNewTimedAction(args => 
            {
                _discardDeck.AddAbilities(_specialDeck);
                _specialDeck.ClearDeck();
                _loadingShuffle = false;
            }, TimedActionCountType.SCALEDTIME, _shuffleWaitTime);
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

            if (!GameManagerBehaviour.InfiniteEnergy)
                Energy -= actionCost;
            
            action?.Invoke();

            return true;
        }

        public bool TryUseEnergy(float actionCost)
        {
            if (Energy < actionCost) return false;

            if (!GameManagerBehaviour.InfiniteEnergy)
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

            Energy += hitCollider.ColliderInfo.Damage / 100;

            if (_opponentMoveset)
                _opponentMoveset.Energy += hitCollider.ColliderInfo.Damage / 200;
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

            bool timerActive = (_rechargeAction?.GetEnabled()).GetValueOrDefault();

            if (!timerActive && EnergyChargeEnabled)
                _rechargeAction = RoutineBehaviour.Instance.StartNewTimedAction(timedEvent => Energy += _energyRechargeValue.Value, TimedActionCountType.SCALEDTIME, _energyRechargeRate.Value);

            //Reload the deck if there are no cards in the hands or the deck
            if (_specialDeck.Count <= 0 && _specialAbilitySlots[0] == null && _specialAbilitySlots[1] == null && !_deckReloading && NextAbilitySlot == null && !_loadingShuffle)
            {
                _deckReloading = true;
                DiscardActiveSlots();
                _deckShuffleAction = RoutineBehaviour.Instance.StartNewTimedAction(timedEvent => ResetSpecialDeck(), TimedActionCountType.SCALEDTIME, _deckReloadTime);
            }


            //CODE FOR DEBUGGING

            string ability1Name = "Empty Slot";
            string ability2Name = "Empty Slot";

            if (_specialAbilitySlots[0] != null)
                ability1Name = _specialAbilitySlots[0].abilityData.abilityName;
            if (_specialAbilitySlots[1] != null)
                ability2Name = _specialAbilitySlots[1].abilityData.abilityName;

            //Debug.Log("1. " + ability1Name + "\n2. " + ability2Name);
        }
    }
}


