using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Lodis.Gameplay
{
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
        [SerializeField]
        private Deck _deckRef;
        private Deck _deck;
        private Ability _lastAbilityInUse;
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

        public bool AbilityInUse
        {
            get
            {
                if (_lastAbilityInUse != null)
                    return _lastAbilityInUse.InUse;

                return false;
            }
        }

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
        /// <returns></returns>
        public Ability UseBasicAbility(BasicAbilityType abilityType, params object[] args)
        {
            if (_lastAbilityInUse != null)
                if (_lastAbilityInUse.InUse && !_lastAbilityInUse.canCancel)
                {
                    return _lastAbilityInUse;
                }

            _deck[(int)abilityType].UseAbility(args);
            _lastAbilityInUse = _deck[(int)abilityType];
            return _lastAbilityInUse;
        }

        private void Update()
        {
            if (_lastAbilityInUse != null)
                if (_lastAbilityInUse.InUse && !_lastAbilityInUse.canCancel)
                    _renderer.material.color = Color.grey;
                else
                    _renderer.material.color = _defaultColor;

        }
    }
}


