using Lodis.Gameplay;
using Lodis.ScriptableObjects;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Lodis.Utility;
using System;
using Lodis.UI;

namespace Lodis.CharacterCreation
{
    public class CustomCharacterUIManagerBehaviour : MonoBehaviour
    {
        [SerializeField]
        private CustomCharacterManagerBehaviour _characterManager;

        [SerializeField]
        private ArmorSectionBehaviour[] _armorSections;

        private List<EventButtonBehaviour> _deckChoices = new List<EventButtonBehaviour>();

        [SerializeField]
        private EventButtonBehaviour _buttonReference;
        [SerializeField]
        private EventSystem _eventSystem;
        private GameObject _lastSelectedSpecial;
        private GameObject _lastSelected;


        public GameObject Selected
        {
            get { return _eventSystem.currentSelectedGameObject; }
            set
            {
                _eventSystem.SetSelectedGameObject(value);
                _lastSelected = value;
            }
        }

        public void SetSelectedToLast()
        {
            Selected = _lastSelected;
        }

        public void UpdateAllIconSections()
        {
            for (int i = 0; i < 6; i++)
                UpdateIconChoicesWithType(i);

            Selected = _armorSections[0].IconHolder.GetChild(0).gameObject;
        }

        public void UpdateIconChoicesWithType(int type, bool setSelected = false)
        {

            Transform iconTransform = Array.Find(_armorSections, section => section.BodySection == (BodySection)type).IconHolder;

            for (int i = iconTransform.childCount - 1; i >= 0; i--)
            {
                Destroy(iconTransform.GetChild(i).gameObject);
                iconTransform.GetChild(i).SetParent(null);
            }

            List<ArmorData> data = _characterManager.ReplacementArmorData.FindAll(armorData => armorData.BodySection == (BodySection)type);


            for (int i = 0; i < data.Count; i++)
            {
                ArmorData currentData = data[i];

                if (_characterManager.CharacterArmorPieces.Contains(currentData))
                    continue;

                EventButtonBehaviour abilityButtonInstance = Instantiate(_buttonReference, iconTransform);

                abilityButtonInstance.Image.sprite = currentData.DisplayIcon;
                abilityButtonInstance.name = currentData.ArmorSetName;

                if (setSelected)
                    Selected = abilityButtonInstance.gameObject;

                abilityButtonInstance.AddOnClickEvent(() =>
                {
                    _characterManager.CurrentArmorType = type;
                    _characterManager.ReplaceArmorPiece(currentData);
                    _characterManager.ReplacementName = currentData.ArmorSetName;
                });

                setSelected = false;
            }
        }

        public void SetCharacterName(Text inputText)
        {
            _characterManager.CharacterName = inputText.text;
        }
    }
}