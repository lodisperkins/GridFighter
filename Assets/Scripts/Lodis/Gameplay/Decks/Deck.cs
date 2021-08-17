using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using Lodis.ScriptableObjects;

namespace Lodis.Gameplay
{
    public enum AbilityName
    {
        WN_Blaster,
        WS_DoubleShot,
        WF_ForwardShot,
        WB_LobShot,
        SN_ChargeShot,
        SS_ChargeDoubleShot,
        SF_ChargeForwardShot,
        SB_ChargeLobShot
    }

    /// <summary>
    /// A deck is a list of abilities for a character. 
    /// </summary>
    [CreateAssetMenu(menuName = "Deck")]
    public class Deck : ScriptableObject
    {
        private List<Ability> _abilities = new List<Ability>();
        [SerializeField]
        private List<AbilityData> _abilityData;
        [SerializeField]
        private string _deckName;

        public string DeckName
        {
            get
            {
                return _deckName;
            }
        }

        public List<AbilityData> AbilityData
        {
            get
            {
                return _abilityData;
            }
        }

        public int Count
        {
            get
            {
                return _abilities.Count;
            }
        }

        private void InitDeck()
        {
            ClearDeck();

            foreach (AbilityData ability in AbilityData)
            {
                string name = ability.name.Substring(0, ability.name.Length - 5);
                Type abilityType = Type.GetType("Lodis.Gameplay." + name);

                if (abilityType == null)
                {
                    Debug.LogError("Couldn't find ability type. Name was " + name + ". Maybe the ability is mispelled?");
                    continue;
                }

                AddAbility((Ability)Activator.CreateInstance(abilityType));
            }
        }

        /// <summary>
        /// Use this to initialize all abilities in the deck
        /// </summary>
        /// <param name="owner"></param>
        public virtual void InitAbilities(GameObject owner)
        {
            InitDeck();

            foreach (Ability ability in _abilities)
                ability.Init(owner);
        }

        /// <summary>
        /// Adds the ability to the list of abilities
        /// </summary>
        /// <param name="ability"></param>
        public void AddAbility(Ability ability)
        {
            if (_abilities == null)
                _abilities = new List<Ability>();

            _abilities.Add(ability);
        }

        /// <summary>
        /// Removes the ability from the list
        /// </summary>
        /// <param name="ability"></param>
        public void RemoveAbility(Ability ability)
        {
            _abilities.Remove(ability);
        }

        /// <summary>
        /// Removes the ability from the list
        /// </summary>
        /// <param name="index"></param>
        /// <returns>False if the index is out of range</returns>
        public bool RemoveAbility(int index)
        {
            if (index < 0 || index >= _abilities.Count)
                return false;

            _abilities.RemoveAt(index);
            return true;
        }

        /// <summary>
        /// Removes the last ability in the list
        /// </summary>
        /// <returns>The last ability in the list</returns>
        public Ability PopBack()
        {
            if (_abilities.Count == 0)
                return null;

            Ability ability = _abilities[_abilities.Count - 1];
            RemoveAbility(ability);

            return ability;
        }

        /// <summary>
        /// Removes all abilities from the deck
        /// </summary>
        public void ClearDeck()
        {
            if (_abilities != null)
                _abilities.Clear();
        }

        public Ability this[int index]
        {
            get { return _abilities[index]; }
        }

        /// <summary>
        /// Gets the first ability in the deck that matches the type
        /// </summary>
        /// <param name="type">The type of ability to search for</param>
        /// <returns></returns>
        public Ability GetAbilityByType(AbilityType type)
        {
            foreach (Ability ability in _abilities)
            {
                if (ability.abilityData.abilityType == type)
                    return ability;
            }

            Debug.LogError("Couldn't find ability of type " + type.ToString() + " in deck " + DeckName);

            return null;
        }

        public IEnumerator GetEnumerator()
        {
            return _abilities.GetEnumerator();
        }

        /// <summary>
        /// Gives each ability a random position in the deck
        /// </summary>
        public void Shuffle()
        {
            //Yates shuffle algorithm
            for (int i = _abilities.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i);

                Ability temp = _abilities[j];
                _abilities[j] = _abilities[i];
                _abilities[i] = temp;
            }
        }

    }
}

