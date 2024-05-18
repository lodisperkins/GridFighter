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
using System.Linq;

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
        private UnityEngine.EventSystems.EventSystem _eventSystem;
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
            for (int i = 0; i < 7; i++)
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

            //Get all armor pieces for the desired type
            List<Wearable> wearables = _characterManager.CustomCharacter.WearableDictionary.Values.ToList();
            List<Wearable> data = wearables.FindAll(item => item.Section == (BodySection)type);


            for (int i = 0; i < data.Count; i++)
            {
                Wearable currentData = data[i];

                //if (_characterManager.CustomCharacter.GetIsWearingItem(currentData))
                //    continue;

                SpawnButton(type, setSelected, iconTransform, currentData);
                setSelected = false;
            }
        }

        private void SpawnButton(int type, bool setSelected, Transform sectionTransform, Wearable currentData)
        {
            EventButtonBehaviour abilityButtonInstance = Instantiate(_buttonReference, sectionTransform);

            abilityButtonInstance.ButtonImage.sprite = currentData.DisplayIcon;
            abilityButtonInstance.name = currentData.ID;

            if (setSelected)
                Selected = abilityButtonInstance.gameObject;

            abilityButtonInstance.AddOnClickEvent(() =>
            {
                _characterManager.CurrentArmorType = type;
                _characterManager.ReplaceWearable(currentData.ID);
                _characterManager.ReplacementName = currentData.ID;
            });
        }

        public void SetCharacterName(Text inputText)
        {
            _characterManager.CharacterName = inputText.text;
        }
    }
}