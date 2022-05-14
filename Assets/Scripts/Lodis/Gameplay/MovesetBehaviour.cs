using Lodis.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
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
        UNBLOCKABLE
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
        [Tooltip("The slots that store the two loaded abilities from the special deck")]
        [SerializeField]
        private Ability[] _specialAbilitySlots = new Ability[2];
        [SerializeField]
        private Ability _lastAbilityInUse;
        [SerializeField]
        private bool _abilityInUse;
        [SerializeField]
        private CharacterAnimationBehaviour _animationBehaviour;
        [Tooltip("This transform is where projectile will spawn by default for this object.")]
        [SerializeField]
        private Transform _projectileSpawnPoint;
        [Tooltip("This transform is where melee hit boxes will spawn by default for this object.")]
        [SerializeField]
        private Transform _meleeHitBoxSpawnPoint;
        [Tooltip("The amount of time it will take for the special deck to reload once all abilities are used")]
        [SerializeField]
        private float _deckReloadTime;
        private bool _deckReloading;
        private Movement.GridMovementBehaviour _movementBehaviour;
        private Input.InputBehaviour _inputBehaviour;
        private UnityAction _onUseAbility;

        public Transform ProjectileSpawnTransform
        {
            get
            {
                return _projectileSpawnPoint;
            }
        }

        public Transform MeleeHitBoxSpawnTransform
        {
            get
            {
                return _meleeHitBoxSpawnPoint;
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            _normalDeck = Instantiate(_normalDeckRef);
            _specialDeck = Instantiate(_specialDeckRef);
            InitializeDecks();
            _movementBehaviour = GetComponent<Movement.GridMovementBehaviour>();
            _inputBehaviour = GetComponent<Input.InputBehaviour>();
        }

        /// <summary>
        /// Load abilities into both decks. Shuffles the special deck
        /// </summary>
        private void InitializeDecks()
        {
            _normalDeck.InitAbilities(gameObject);
            _specialDeck.InitAbilities(gameObject);
            _specialDeck.Shuffle();
            _specialAbilitySlots[0] = _specialDeck.PopBack();
            _specialAbilitySlots[1] = _specialDeck.PopBack();
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
        /// <param name="name">The name of the ability to search for. Do not use the file name.</param>
        public bool NormalDeckContains(string name)
        {
            return _normalDeck.Contains(name);
        }

        /// <summary>
        /// Checks if the special deck has an ability that matches the name
        /// </summary>
        /// <param name="name">The name of the ability to search for. Do not use the file name.</param>
        public bool SpecialDeckContains(string name)
        {
            return _specialDeck.Contains(name);
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
        /// <param name="abilityType">The ability type to search for.</param>
        /// <returns></returns>
        public Ability GetAbilityByName(string name)
        {
            if (_normalDeck.GetAbilityByName(name) == null)
                return _specialDeck.GetAbilityByName(name);

            return _normalDeck.GetAbilityByName(name);
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

            //Find the ability in the deck abd use it
            Ability currentAbility = _normalDeck.GetAbilityByType(abilityType);
            //Return if there is an ability in use that can't be canceled
            if (_lastAbilityInUse != null)
                if (_lastAbilityInUse.InUse && !_lastAbilityInUse.TryCancel(currentAbility))
                    return _lastAbilityInUse;

            if (currentAbility == null)
                return null;

            currentAbility.UseAbility(args);
            _lastAbilityInUse = currentAbility;

            OnUseAbility?.Invoke();

            //Return new ability
            return _lastAbilityInUse;
        }
        
        /// <summary>
        /// Uses a basic ability of the given type if one isn't already in use. If an ability is in use
        /// the ability to use will be activated if the current ability in use can be canceled.
        /// </summary>
        /// <param name="abilityType">The type of basic ability to use</param>
        /// <param name="args">Additional arguments to be given to the basic ability</param>
        /// <returns>The ability used.</returns>
        public Ability UseBasicAbility(string abilityName, params object[] args)
        {

            //Find the ability in the deck abd use it
            Ability currentAbility = _normalDeck.GetAbilityByName(abilityName);

            //Return if there is an ability in use that can't be canceled
            if (_lastAbilityInUse != null)
                if (_lastAbilityInUse.InUse && !_lastAbilityInUse.TryCancel(currentAbility))
                    return _lastAbilityInUse;

            if (currentAbility == null)
                return null;

            currentAbility.UseAbility(args);
            _lastAbilityInUse = currentAbility;

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
        /// Creates a new instance of the special deck and shuffles its abilities
        /// </summary>
        private void ReloadDeck()
        {
            _specialDeck = Instantiate(_specialDeckRef);
            _specialDeck.InitAbilities(gameObject);
            _specialDeck.Shuffle();
            _specialAbilitySlots[0] = _specialDeck.PopBack();
            _specialAbilitySlots[1] = _specialDeck.PopBack();
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
                RoutineBehaviour.Instance.StartNewTimedAction(timedEvent => _specialAbilitySlots[slot] = _specialDeck.PopBack(), TimedActionCountType.SCALEDTIME, _specialDeck[_specialDeck.Count - 1].abilityData.chargeTime);
            }
        }

        /// <summary>
        /// Uses a special ability
        /// </summary>
        /// <param name="abilitySlot">The index of the ability in the characters hand</param>
        /// <param name="args">additional arguments the ability may need</param>
        /// <returns>The ability that was used</returns>
        public Ability UseSpecialAbility(int abilitySlot, params object[] args)
        {
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

            //Doesn't increment ability use amount before checking max
            currentAbility.UseAbility(args);
            _lastAbilityInUse = currentAbility;

            currentAbility.currentActivationAmount++;

            if (!_deckReloading)
                currentAbility.onEnd += () => UpdateHand(abilitySlot);

            OnUseAbility?.Invoke();
            //Return new ability
            return _lastAbilityInUse;
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

            //Reload the deck if there are no cards in the hands or the deck
            if (_specialDeck.Count <= 0 && _specialAbilitySlots[0] == null && _specialAbilitySlots[1] == null && !_deckReloading)
            {
                _deckReloading = true;
                RoutineBehaviour.Instance.StartNewTimedAction(timedEvent => { ReloadDeck(); _deckReloading = false; }, TimedActionCountType.SCALEDTIME, _deckReloadTime);
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


