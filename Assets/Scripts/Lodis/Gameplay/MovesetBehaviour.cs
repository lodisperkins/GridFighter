using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
        NONE
    }

    public class MovesetBehaviour : MonoBehaviour
    {
        private AbilityType _attackState = AbilityType.NONE;
        [Tooltip("The deck this character will be using.")]
        [SerializeField]
        private Deck _normalDeckRef;
        private Deck _normalDeck;
        [SerializeField]
        private Deck _specialDeckRef;
        private Deck _specialDeck;
        [SerializeField]
        private Ability[] _specialAbilitySlots = new Ability[2];
        private Ability _lastAbilityInUse;
        [Tooltip("The renderer attached to this object. Used to change color for debugging.")]
        [SerializeField]
        private Renderer _renderer;
        private Color _defaultColor;
        [SerializeField]
        private CharacterAnimationBehaviour _animationBehaviour;
        [Tooltip("This transform is where projectile will spawn by default for this object.")]
        [SerializeField]
        private Transform _projectileSpawnPoint;
        [Tooltip("This transform is where melee hit boxes will spawn by default for this object.")]
        [SerializeField]
        private Transform _meleeHitBoxSpawnPoint;
        [SerializeField]
        private float _deckReloadTime;

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
            _defaultColor = _renderer.material.color;
            _normalDeck = Instantiate(_normalDeckRef);
            _specialDeck = Instantiate(_specialDeckRef);
            InitializeDecks();
        }

        private void InitializeDecks()
        {
            _normalDeck.InitAbilities(gameObject);
            _specialDeck.InitAbilities(gameObject);
            _specialAbilitySlots[0] = _specialDeck.PopBack();
            _specialAbilitySlots[1] = _specialDeck.PopBack();
        }

        public bool GetCanUseAbility()
        {
            if (!AbilityInUse)
                return true;

            return _lastAbilityInUse.CheckIfAbilityCanBeCanceled();
        }

        /// <summary>
        /// True if there is some ability that is currently active.
        /// </summary>
        public bool AbilityInUse
        {
            get
            {
                if (_lastAbilityInUse != null)
                    return _lastAbilityInUse.InUse;

                return false;
            }
        }

        /// <summary>
        /// Gets the ability from the moveset deck based on the type passed in.
        /// </summary>
        /// <param name="abilityType">The ability type to search for.</param>
        /// <returns></returns>
        public Ability GetAbilityByType(AbilityType abilityType)
        {
            return _normalDeck[(int)abilityType];
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
            //Return if there is an ability in use that can't be canceled
            if (_lastAbilityInUse != null)
                if (_lastAbilityInUse.InUse && !_lastAbilityInUse.TryCancel())
                    return _lastAbilityInUse;

            //Find the ability in the deck abd use it
            Ability currentAbility = _normalDeck.GetAbilityByType(abilityType);

            if (currentAbility == null)
                return null;

            if (_animationBehaviour)
                _animationBehaviour.PlayAbilityAnimation(currentAbility);
            
            currentAbility.UseAbility(args);
            _lastAbilityInUse = currentAbility;
             

            //Return new ability
            return _lastAbilityInUse;
        }

        private void ReloadDeck()
        {
            _specialDeck = Instantiate(_specialDeckRef);
            _specialDeck.InitAbilities(gameObject);
            _specialDeck.Shuffle();
            _specialAbilitySlots[0] = _specialDeck.PopBack();
            _specialAbilitySlots[1] = _specialDeck.PopBack();
        }

        public IEnumerator ChargeNextAbility(int slot)
        {
            _specialAbilitySlots[slot] = null;
            if (_specialDeck.Count == 0 && _specialAbilitySlots[0] == null && _specialAbilitySlots[1] == null)
            {
                yield return new WaitForSeconds(_deckReloadTime);
                ReloadDeck();
            }
            else if (_specialDeck.Count > 0)
            {
                yield return new WaitForSeconds(_specialDeck[_specialDeck.Count - 1].abilityData.chargeTime);
                _specialAbilitySlots[slot] = _specialDeck.PopBack();
            }
        }

        public Ability UseSpecialAbility(int abilitySlot, params object[] args)
        {
            //Return if there is an ability in use that can't be canceled
            if (_lastAbilityInUse != null)
                if (_lastAbilityInUse.InUse && !_lastAbilityInUse.TryCancel())
                    return _lastAbilityInUse;

            //Find the ability in the deck abd use it
            Ability currentAbility = _specialAbilitySlots[abilitySlot];

            if (currentAbility == null)
                return null;

            if (_animationBehaviour)
                _animationBehaviour.PlayAbilityAnimation(currentAbility);

            currentAbility.UseAbility(args);
            currentAbility.onDeactivate += () => StartCoroutine(ChargeNextAbility(abilitySlot));

            _lastAbilityInUse = currentAbility;

            //Return new ability
            return _lastAbilityInUse;
        }

        private void Update()
        {
            //Update color for debugging
            if (_lastAbilityInUse != null)
            {
                if (_lastAbilityInUse.InUse && !_lastAbilityInUse.CheckIfAbilityCanBeCanceled())
                    _renderer.material.color = Color.grey;
                else
                    _renderer.material.color = _defaultColor;
            }
            else
            { 
                _renderer.material.color = _defaultColor;
            }

            string ability1Name = "Empty Slot";
            string ability2Name = "Empty Slot";

            if (_specialAbilitySlots[0] != null)
                ability1Name = _specialAbilitySlots[0].abilityData.abilityName;
            if (_specialAbilitySlots[1] != null)
                ability2Name = _specialAbilitySlots[1].abilityData.abilityName;

            Debug.Log("1. " + ability1Name + "\n2. " + ability2Name);
        }
    }
}


