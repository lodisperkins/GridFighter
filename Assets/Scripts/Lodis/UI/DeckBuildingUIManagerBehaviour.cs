using Lodis.Gameplay;
using Lodis.ScriptableObjects;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Lodis.UI
{
    public class DeckBuildingUIManagerBehaviour : MonoBehaviour
    {
        [SerializeField]
        private DeckBuildingManagerBehaviour _buildManager;

        [Header("Current Deck Page")]
        [SerializeField]
        private Button _loadoutButton;
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
        private Image[] _specialIcons;

        [SerializeField]
        private GameObject[] _abilitySections;

        [SerializeField]
        private Text _abilityIconHeader;
        [SerializeField]
        private RectTransform _iconTransform;
        [SerializeField]
        private EventButtonBehaviour _abilityButton;
        [SerializeField]
        private EventSystem _eventSystem;

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
                Instantiate(_loadoutButton, _loadoutOptions.transform);
            }
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
                _specialIcons[i].sprite = data.DisplayIcon;
                _specialIcons[i].color = BlackBoardBehaviour.Instance.AbilityCostColors[(int)data.EnergyCost];
            }
        }

        public void FocusAbilitySection(string sectionName)
        {
            foreach (GameObject section in _abilitySections)
            {
                if (section.name == sectionName)
                {
                    section.SetActive(true);
                    continue;
                }

                section.SetActive(false);
            }
        }

        public void UpdateIconChoicesWithType(int type)
        {

            _buildManager.CurrentAbilityType = type;

            switch (type)
            {
                case 0:
                    _abilityIconHeader.text = "Neutral";
                    break;
                case 1:
                    _abilityIconHeader.text = "Up/Down";
                    break;
                case 2:
                    _abilityIconHeader.text = "Forward";
                    break;
                case 3:
                    _abilityIconHeader.text = "Backward";
                    break;
                case 8:
                    _abilityIconHeader.text = "Special";
                    break;
                case 9:
                    _abilityIconHeader.text = "Unblockable";
                    break;
            }


            for (int i = _iconTransform.childCount - 1; i >= 0; i--)
            {
                Destroy(_iconTransform.GetChild(i).gameObject);
            }

            List<AbilityData> data = _buildManager.ReplacementAbilities.AbilityData.FindAll(abilityData => abilityData.AbilityType == (AbilityType)type);

            bool selectedSet = false;

            for (int i = 0;  i < data.Count; i++)
            {
                AbilityData currentData = data[i];

                if (_buildManager.NormalDeck.AbilityData.Contains(currentData) || _buildManager.SpecialDeck.AbilityData.Contains(currentData))
                    continue;

                EventButtonBehaviour abilityButtonInstance = Instantiate(_abilityButton, _iconTransform);

                if (!selectedSet)
                {
                    _eventSystem.SetSelectedGameObject(abilityButtonInstance.gameObject);
                    selectedSet = true;
                }

                abilityButtonInstance.Image.sprite = currentData.DisplayIcon;
                abilityButtonInstance.Image.color = BlackBoardBehaviour.Instance.AbilityCostColors[(int)currentData.EnergyCost];
                abilityButtonInstance.name = currentData.abilityName;

                abilityButtonInstance.AddOnClickEvent(() =>
                {
                    _buildManager.ReplaceAbility(currentData.abilityName);
                    UpdateDeck();
                });
            }
        }
    }
}