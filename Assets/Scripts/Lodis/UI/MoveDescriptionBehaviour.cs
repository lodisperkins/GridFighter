using Lodis.ScriptableObjects;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Lodis.UI
{
    public class MoveDescriptionBehaviour : MonoBehaviour
    {
        [SerializeField]
        private AbilityData _ability;
        [SerializeField]
        private Image _iconImage;
        [SerializeField]
        private Text _description;

        // Start is called before the first frame update
        void Start()
        {
            SetAbility(_ability);
        }

        public void SetAbility(AbilityData data)
        {
            _ability = data;
            _iconImage.sprite = _ability.DisplayIcon;
            _description.text = _ability.abilityName + "\n\n" + _ability.abilityDescription + "\n\nEnergy Cost: " + _ability.EnergyCost;
        }
    }
}