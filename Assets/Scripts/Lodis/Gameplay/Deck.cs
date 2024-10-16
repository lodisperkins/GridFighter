﻿using System.Collections;
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
        private static int _seed = -1;

        private List<Ability> _abilities = new List<Ability>();
        [SerializeField]
        private List<AbilityData> _abilityData = new List<AbilityData>();
        [SerializeField]
        private string _deckName;

        public string DeckName
        {
            get
            {
                return _deckName;
            }
            set
            {
                _deckName = value;
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

        public static int Seed { get => _seed; set => _seed = value; }


        private void InitDeck()
        {
            ClearDeck();

            foreach (AbilityData ability in _abilityData)
            {
                string name = ability.name.Substring(0, ability.name.Length - 5);
                Type abilityType = Type.GetType("Lodis.Gameplay." + name);

                if (abilityType == null)
                {
                    Debug.LogError("Couldn't find ability type. Name was " + name + ". Maybe the ability is misspelled?");
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
        /// Adds the ability to the list of abilities
        /// </summary>
        /// <param name="ability"></param>
        public void AddAbilities(Deck abilityDeck)
        {
            if (_abilities == null)
                _abilities = new List<Ability>();

            _abilities.AddRange(abilityDeck._abilities);
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
                if (ability.abilityData.AbilityType == type)
                    return ability;
            }

            Debug.LogError("Couldn't find ability of type " + type.ToString() + " in deck " + DeckName);

            return null;
        }

        /// <summary>
        /// Gets the first ability in the deck that matches the type
        /// </summary>
        /// <param name="id">The type of ability to search for</param>
        /// <returns></returns>
        public Ability GetAbilityByID(int id)
        {
            foreach (Ability ability in _abilities)
            {
                if (ability.abilityData.ID == id)
                    return ability;
            }

            Debug.LogError("Couldn't find ability of ID " + id.ToString() + " in deck " + DeckName);

            return null;
        }

        /// <summary>
        /// Gets the first ability in the deck that matches the type
        /// </summary>
        /// <param name="id">The type of ability to search for</param>
        /// <returns></returns>
        public bool TryGetAbilityByID(int id, out Ability ability)
        {
            ability = null;

            foreach (Ability currentAbility in _abilities)
            {
                if (currentAbility.abilityData.ID == id)
                {
                    ability = currentAbility;
                    return true;
                }
            }

            return false;
        }

        public Ability GetBurstAbility(string state)
        {
            string burstType = "";

            if (state == "Attacking")
                burstType = "Offensive Burst";
            else
                burstType = "Defensive Burst";

            foreach (Ability ability in _abilities)
            {
                if (ability.abilityData.abilityName == burstType)
                    return ability;
            }

            Debug.LogError("Couldn't find burst ability of type " + burstType + " in deck " + DeckName);

            return null;
        }

        /// <summary>
        /// Gets the first ability in the deck that matches the type
        /// </summary>
        /// <param name="type">The type of ability to search for</param>
        /// <returns></returns>
        public AbilityData GetAbilityDataByType(AbilityType type)
        {
            foreach (AbilityData abilityData in _abilityData)
            {
                if (abilityData.AbilityType == type)
                    return abilityData;
            }

            Debug.LogError("Couldn't find ability data of type " + type.ToString() + " in deck " + DeckName);

            return null;
        }

        /// <summary>
        /// Gets the first ability in the deck that matches the name
        /// </summary>
        /// <param name="type">The type of ability to search for</param>
        /// <returns></returns>
        public AbilityData GetAbilityDataByName(string name)
        {
            foreach (AbilityData abilityData in _abilityData)
            {
                if (abilityData.abilityName == name)
                    return abilityData;
            }

            Debug.LogError("Couldn't find ability data named" + name + " in deck " + DeckName);

            return null;
        }

        /// <summary>
        /// Gets the first ability in the deck that matches the type
        /// </summary>
        /// <param name="type">The type of ability to search for</param>
        /// <returns></returns>
        public void SetAbilityDataByType(AbilityType type, AbilityData data)
        {
            for (int i = 0; i < _abilityData.Count; i++)
            {
                if (_abilityData[i].AbilityType == type)
                {
                    _abilityData[i] = data;
                    return;
                }
            }

            Debug.LogError("Couldn't find ability data of type " + type.ToString() + " in deck " + DeckName);
        }

        /// <summary>
        /// Gets the first ability in the deck that matches the name
        /// </summary>
        /// <param name="name">The name of ability to search for</param>
        /// <returns></returns>
        public Ability GetAbilityByName(string name)
        {
            foreach (Ability ability in _abilities)
            {
                if (ability.abilityData.abilityName == name)
                    return ability;
            }

            return null;
        }

        /// <summary>
        /// Gets the first ability in the deck that matches the name
        /// </summary>
        /// <param name="name">The name of ability to search for</param>
        /// <returns></returns>
        public Ability GetAbilityByCondition(Condition condition)
        {
            return _abilities.Find(ability => condition.Invoke(ability));
        }

        public bool Contains(string name)
        {
            if (name == null)
                return false;

            for(int i = 0; i < _abilities.Count; i++)
            {
                if (_abilities[i].abilityData.abilityName == name)
                    return true;
            }
            return false;
        }

        public bool Contains(int ID)
        {
            if (name == null)
                return false;

            for(int i = 0; i < _abilities.Count; i++)
            {
                if (_abilities[i].abilityData.ID == ID)
                    return true;
            }
            return false;
        }

        public bool Contains(Ability ability)
        {
            if (ability == null)
                return false;

            for(int i = 0; i < _abilities.Count; i++)
            {
                if (_abilities[i] == ability)
                    return true;
            }
            return false;
        }

        public bool Contains(AbilityData abilityData)
        {
            if (abilityData == null)
                return false;

            for(int i = 0; i < _abilityData.Count; i++)
            {
                if (_abilityData[i].abilityName == abilityData.abilityName)
                    return true;
            }
            return false;
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
            System.Random random;

            if (Seed == -1)
            {
                System.Random rand = new System.Random();
                Seed = rand.Next(-int.MaxValue, int.MaxValue);
            }

            random = new System.Random(Seed);

            //Yates shuffle algorithm
            for (int i = _abilities.Count - 1; i > 0; i--)
            {
                int j = random.Next(0, i);

                Ability temp = _abilities[j];
                _abilities[j] = _abilities[i];
                _abilities[i] = temp;
            }
        }

    }
}

