using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Lodis.Gameplay
{
    public enum Attack
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
        private Attack _attackState = Attack.NONE;
        [SerializeField]
        private Deck _deck;
        private Ability _currentAbilityInUse;

        // Start is called before the first frame update
        void Start()
        {
            InitializeDeck();
        }

        private void InitializeDeck()
        {
            _deck.InitAbilities(gameObject);
        }

        /// <summary>
        /// Uses a basic ability of the given type if one isn't already in use. If an ability is in use
        /// the ability to use will be activated if the current ability in use can be canceled.
        /// </summary>
        /// <param name="abilityType">The type of basic ability to use</param>
        /// <param name="args">Additional arguments to be given to the basic ability</param>
        /// <returns></returns>
        public Ability UseBasicAbility(Attack abilityType, params object[] args)
        {
            if (_currentAbilityInUse != null)
                if (_currentAbilityInUse.InUse && !_currentAbilityInUse.canCancel)
                    return _currentAbilityInUse;

            _deck[(int)abilityType].UseAbility(args);
            _currentAbilityInUse = _deck[(int)abilityType];
            return _currentAbilityInUse;
        }
    }
}


