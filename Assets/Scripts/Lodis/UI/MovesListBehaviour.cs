using Lodis.Gameplay;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lodis.ScriptableObjects;

namespace Lodis.UI
{
    public class MovesListBehaviour : MonoBehaviour
    {
        [SerializeField]
        private Deck _deck;
        [SerializeField]
        private MoveDescriptionBehaviour _moveDescription;

        // Start is called before the first frame update
        void Start()
        {
            List<string> abilitiesDone = new List<string>();
            
            foreach (AbilityData data in _deck.AbilityData)
            {
                if (abilitiesDone.Contains(data.abilityName))
                    continue;

                Instantiate(_moveDescription, transform).SetAbility(data);
                abilitiesDone.Add(data.abilityName);
            }
        }
    }
}