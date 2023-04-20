using Lodis.Gameplay;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Lodis.UI
{
    public class AbilitySectionBehaviour : MonoBehaviour
    {
        [SerializeField]
        private AbilityType _abilityType;
        [SerializeField]
        private Text _headerText;

        [SerializeField]
        private Transform _iconHolder;

        public AbilityType AbilityType { get => _abilityType; private set => _abilityType = value; }
        public Transform IconHolder { get => _iconHolder; set => _iconHolder = value; }
        public Text HeaderText { get => _headerText; set => _headerText = value; }
    }
}