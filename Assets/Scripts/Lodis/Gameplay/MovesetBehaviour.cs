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
    public enum BasicAbilityType
    {
        WEAKNEUTRAL,
        WEAKSIDE,
        WEAKFORWARD,
        WEAKBACKWARD,
        STRONGNEUTRAL,
        STRONGSIDE,
        STRONGFORWARD,
        STRONGBACKWARD,
        NONE
    }

    public class MovesetBehaviour : MonoBehaviour
    {
        private BasicAbilityType _attackState = BasicAbilityType.NONE;
        [Tooltip("The deck this character will be using.")]
        [SerializeField]
        private Deck _deckRef;
        private Deck _deck;
        private Ability _lastAbilityInUse;
        [Tooltip("The renderer attached to this object. Used to change color for debugging.")]
        [SerializeField]
        private Renderer _renderer;
        private Color _defaultColor;

        // Start is called before the first frame update
        void Start()
        {
            _defaultColor = _renderer.material.color;
            _deck = Instantiate(_deckRef);
            InitializeDeck();
        }

        private void InitializeDeck()
        {
            _deck.InitAbilities(gameObject);
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
        public Ability GetAbilityByType(BasicAbilityType abilityType)
        {
            return _deck[(int)abilityType];
        }

        /// <summary>
        /// Uses a basic ability of the given type if one isn't already in use. If an ability is in use
        /// the ability to use will be activated if the current ability in use can be canceled.
        /// </summary>
        /// <param name="abilityType">The type of basic ability to use</param>
        /// <param name="args">Additional arguments to be given to the basic ability</param>
        /// <returns>The ability used.</returns>
        public Ability UseBasicAbility(BasicAbilityType abilityType, params object[] args)
        {
            //Return if there is an ability in use that can't be canceled
            if (_lastAbilityInUse != null)
                if (_lastAbilityInUse.InUse && !_lastAbilityInUse.canCancel)
                {
                    return _lastAbilityInUse;
                }

            //Find the ability in the deck abd use it
            _deck[(int)abilityType].UseAbility(args);
            _lastAbilityInUse = _deck[(int)abilityType];

            //Return new ability
            return _lastAbilityInUse;
        }

        private void Update()
        {
            //Update color for debugging
            if (_lastAbilityInUse != null)
            {
                if (_lastAbilityInUse.InUse && !_lastAbilityInUse.canCancel)
                    _renderer.material.color = Color.grey;
                else
                    _renderer.material.color = _defaultColor;
            }
            else
            { 
                _renderer.material.color = _defaultColor;
            }

        }
    }
}


