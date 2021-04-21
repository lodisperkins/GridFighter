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
        private string _deckName;
        private Type _deckType;
        private Deck _deck;
        private Ability _currentAbilityInUse;

        // Start is called before the first frame update
        void Start()
        {
            _deckType = Type.GetType("Lodis.Gameplay." + _deckName);
            _deck = (Deck)Activator.CreateInstance(_deckType);
            InitializeAbilities();
        }

        private void InitializeAbilities()
        {
            _deck.Init(gameObject);
        }

        public Ability UseAbility(Attack abilityType, params object[] args)
        {
            if (_currentAbilityInUse != null)
                if (_currentAbilityInUse.InUse && !_currentAbilityInUse.canCancel)
                    return _currentAbilityInUse;

            _deck[(int)abilityType].UseAbility(args);
            _currentAbilityInUse = _deck[(int)abilityType];
            return _currentAbilityInUse;
        }

        // Update is called once per frame
        void Update()
        {
        }
    }
}


