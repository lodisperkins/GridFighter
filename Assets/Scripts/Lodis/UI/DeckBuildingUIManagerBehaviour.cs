using Lodis.Gameplay;
using Lodis.ScriptableObjects;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Lodis.Utility;
using System;

namespace Lodis.UI
{
    public class DeckBuildingUIManagerBehaviour : MonoBehaviour
    {
        [SerializeField]
        private DeckBuildingManagerBehaviour _buildManager;

        [Header("Current Deck Page")]
        [SerializeField]
        private EventButtonBehaviour _loadoutButton;
        [SerializeField]
        private GameObject _loadoutOptions;
        [SerializeField]
        private Text _infoTextBox;

        [SerializeField]
        private Image _unblockableIconSlot;
        [SerializeField]
        private Image _backIconSlot;
        [SerializeField]
        private Image _forwardIconSlot;
        [SerializeField]
        private Image _neautralIconSlot;
        [SerializeField]
        private Image _upDownIconSlot;

        [SerializeField]
        private EventButtonBehaviour[] _specialIcons;

        [SerializeField]
        private AbilitySectionBehaviour[] _abilitySections;

        [SerializeField]
        private Text _abilityIconHeader;
        [SerializeField]
        private EventButtonBehaviour _abilityButton;
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
        // Start is called before the first frame update
        void Start()
        {
            UpdateLoadoutOptions();
        }

        public void UpdateLoadoutOptions()
        {
            if (_buildManager.DeckOptions == null)
                return;

            foreach (string optionName in _buildManager.DeckOptions)
            {
                if (optionName == null)
                    continue;

                EventButtonBehaviour buttonInstance = Instantiate(_loadoutButton, _loadoutOptions.transform);
                buttonInstance.GetComponentInChildren<Text>().text = optionName;
                buttonInstance.AddOnClickEvent(() =>
                {
                    _buildManager.LoadCustomDeck("Custom");
                    _eventSystem.GetComponent<PageManagerBehaviour>().GoToNextPage();
                });
            }
        }

        public void ToggleAllSpecialsInDeck(bool enabled)
        {
            foreach (EventButtonBehaviour button in _specialIcons)
            {
                button.UIButton.interactable = enabled;
            }
        }

        public void SetSelectedToLast()
        {
            Selected = _lastSelected;
        }

        public void UpdateDeck()
        {
            _unblockableIconSlot.sprite = _buildManager.NormalDeck.GetAbilityDataByType(Gameplay.AbilityType.UNBLOCKABLE).DisplayIcon;
            _backIconSlot.sprite = _buildManager.NormalDeck.GetAbilityDataByType(Gameplay.AbilityType.WEAKBACKWARD).DisplayIcon;
            _forwardIconSlot.sprite = _buildManager.NormalDeck.GetAbilityDataByType(Gameplay.AbilityType.WEAKFORWARD).DisplayIcon;
            _neautralIconSlot.sprite = _buildManager.NormalDeck.GetAbilityDataByType(Gameplay.AbilityType.WEAKNEUTRAL).DisplayIcon;
            _upDownIconSlot.sprite = _buildManager.NormalDeck.GetAbilityDataByType(Gameplay.AbilityType.WEAKSIDE).DisplayIcon;

            for (int i = 0; i < _buildManager.SpecialDeck.AbilityData.Count; i++)
            {
                AbilityData data = _buildManager.SpecialDeck.AbilityData[i];
                _specialIcons[i].UIButton.image.sprite = data.DisplayIcon;
                _specialIcons[i].UIButton.image.color = BlackBoardBehaviour.Instance.AbilityCostColors[(int)data.EnergyCost];
            }
        }

        public void FocusAbilitySection(string sectionName)
        {
            foreach (AbilitySectionBehaviour section in _abilitySections)
            {
                if (section.name == sectionName)
                {
                    section.gameObject.SetActive(true);
                    continue;
                }

                section.gameObject.SetActive(false);
            }
        }

        public void UpdateAllIconSections()
        {
            for (int i = 0; i < 4; i++)
                UpdateIconChoicesWithType(i);

            UpdateIconChoicesWithType(8);
            UpdateIconChoicesWithType(9);

            Selected = _abilitySections[0].IconHolder.GetChild(0).gameObject;
        }

        public void UpdateIconChoicesWithType(int type, bool setSelected = false)
        {

            Transform iconTransform = Array.Find(_abilitySections, section => section.AbilityType == (AbilityType)type).IconHolder;

            for (int i = iconTransform.childCount - 1; i >= 0; i--)
            {
                Destroy(iconTransform.GetChild(i).gameObject);
                iconTransform.GetChild(i).SetParent(null);
            }

            List<AbilityData> data = _buildManager.ReplacementAbilities.AbilityData.FindAll(abilityData => abilityData.AbilityType == (AbilityType)type);


            for (int i = 0; i < data.Count; i++)
            {
                AbilityData currentData = data[i];

                if (_buildManager.NormalDeck.Contains(currentData) || _buildManager.SpecialDeck.Contains(currentData))
                    continue;

                EventButtonBehaviour abilityButtonInstance = Instantiate(_abilityButton, iconTransform);

                abilityButtonInstance.Image.sprite = currentData.DisplayIcon;
                abilityButtonInstance.Image.color = BlackBoardBehaviour.Instance.AbilityCostColors[(int)currentData.EnergyCost];
                abilityButtonInstance.name = currentData.abilityName;

                if (setSelected)
                    Selected = abilityButtonInstance.gameObject;


                if ((AbilityType)type == AbilityType.SPECIAL)
                {
                    abilityButtonInstance.AddOnClickEvent(() =>
                    {
                        _buildManager.CurrentAbilityType = type;
                        _buildManager.ReplacementName = currentData.abilityName;
                        ToggleAllSpecialsInDeck(true);
                        Selected = _specialIcons[0].gameObject;
                    });
                }
                else
                {
                    abilityButtonInstance.AddOnClickEvent(() =>
                    {
                        _buildManager.CurrentAbilityType = type;
                        _buildManager.ReplaceAbility(currentData.abilityName);
                        UpdateDeck();
                        UpdateIconChoicesWithType(type, true);
                    });
                }

                setSelected = false;
            }
        }
    }
}