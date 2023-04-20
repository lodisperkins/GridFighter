using Lodis.Gameplay;
using Lodis.ScriptableObjects;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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

        public void UpdateDeckMenu()
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

        // Update is called once per frame
        void Update()
        {

        }
    }
}