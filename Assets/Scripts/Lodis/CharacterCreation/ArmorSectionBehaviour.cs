using Lodis.ScriptableObjects;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Lodis.CharacterCreation
{
    public class ArmorSectionBehaviour : MonoBehaviour
    {
        [SerializeField]
        private BodySection _bodySection;
        [SerializeField]
        private Text _headerText;

        [SerializeField]
        private Transform _iconHolder;

        public Transform IconHolder { get => _iconHolder; set => _iconHolder = value; }
        public Text HeaderText { get => _headerText; set => _headerText = value; }
        public BodySection BodySection { get => _bodySection; private set => _bodySection = value; }
    }
}