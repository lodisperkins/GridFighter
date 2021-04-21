using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{
    /// <summary>
    /// A deck is a list of abilities for a character. 
    /// </summary>
    public abstract class Deck
    {
        private List<Ability> _abilities;

        public Deck()
        {
            _abilities = new List<Ability>();
        }

        /// <summary>
        /// Use this to initialize all abilities in the deck
        /// </summary>
        /// <param name="owner"></param>
        public virtual void Init(GameObject owner)
        {
            foreach (Ability ability in _abilities)
                ability.Init(owner);
        }

        /// <summary>
        /// Adds the ability to the list of abilities
        /// </summary>
        /// <param name="ability"></param>
        public void AddAbility(Ability ability)
        {
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

        public Ability this[int index]
        {
            get { return _abilities[index]; }
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
            for (int i = _abilities.Count; i > 0; i--)
            {
                int j = Random.Range(0, i);

                Ability temp = _abilities[j];
                _abilities[j] = _abilities[i];
                _abilities[i] = temp;
            }
        }

    }
}

