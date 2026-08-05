using Lodis.Gameplay;
using Lodis.ScriptableObjects;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Lodis.Utility;
using System;
using Lodis.CharacterCreation;
using UnityEngine.Video;
using DG.Tweening;
using System.IO;
using UnityEngine.InputSystem;

namespace Lodis.UI
{
    public class DeckBuildingUIManagerBehaviour : MonoBehaviour
    {
        [SerializeField]
        private DeckBuildingManagerBehaviour _buildManager;
        [SerializeField]
        private CustomCharacterManagerBehaviour _customCharacterManager;

        [Header("Current Deck Page")]
        [SerializeField]
        private MovesListBehaviour _movesList;
        [SerializeField]
        private EventButtonBehaviour _loadoutButton;
        [SerializeField]
        private GameObject _loadoutOptions;
        [SerializeField]
        private bool _setSelectedToFirstLoadout;
        [SerializeField]
        private Text _infoTextBox;
        [SerializeField]
        private VideoPlayer _infoPlayer;
        [SerializeField]
        private EventButtonBehaviour _backIconSlot;
        [SerializeField]
        private EventButtonBehaviour _forwardIconSlot;
        [SerializeField]
        private EventButtonBehaviour _neutralIconSlot;
        [SerializeField] private EventButtonBehaviour _upDownIconSlot;

        [SerializeField] private EventButtonBehaviour[] _specialIcons;

        [SerializeField] private AbilitySectionBehaviour[] _abilitySections;

        [SerializeField] private EventButtonBehaviour _abilityButton;
        [SerializeField] private UnityEngine.EventSystems.EventSystem _eventSystem;
        [SerializeField] private PageManagerBehaviour _pageManager;
        [SerializeField] private PlayerColorManagerBehaviour _colorManager;
        [SerializeField] private bool _makingNewCharacter;

        [SerializeField] private PlayerInput _playerInput;
        [SerializeField] private ControlSchemeManager _controlSchemeManager;

        private List<EventButtonBehaviour> _deckChoices = new List<EventButtonBehaviour>();
        private GameObject _lastSelectedSpecial;
        private GameObject _lastSelected;
        private string _originalName;
        private string _potentialName;



        public GameObject Selected
        {
            get { return EventManager.currentSelectedGameObject; }
            set
            {
                EventManager.SetSelectedGameObject(value);
                _lastSelected = value;
            }
        }

        public PageManagerBehaviour PageManager { get => _pageManager; }
        public UnityEngine.EventSystems.EventSystem EventManager { get => _eventSystem; set => _eventSystem = value; }

        // Start is called before the first frame update
        void Start()
        {
            _playerInput = _eventSystem.GetComponent<PlayerInput>();
            UpdateLoadoutOptions();
        }

        public void UpdateLoadoutOptions()
        {
            if (_buildManager.DeckOptions == null)
                return;

            for (int i = _deckChoices.Count - 1; i >= 0; i--)
            {
                Destroy(_deckChoices[i].gameObject);
                _deckChoices[i].transform.SetParent(null);
            }

            _deckChoices.Clear();

            foreach (string optionName in _buildManager.DeckOptions)
            {
                if (optionName == null)
                    continue;

                EventButtonBehaviour buttonInstance = Instantiate(_loadoutButton, _loadoutOptions.transform);
                buttonInstance.GetComponentInChildren<Text>().text = optionName;

                buttonInstance.AddOnSelectEvent(() =>
                {
                    _customCharacterManager.LoadCustomCharacter(optionName);
                    _colorManager?.Recolor();
                });

                buttonInstance.AddOnClickEvent(() =>
                {
                    _buildManager.LoadCustomDeck(optionName);
                    _potentialName = optionName;
                    _originalName = optionName;
                    PageManager.GoToPageChild(0);
                });
                _deckChoices.Add(buttonInstance);
            }
        }

        public void ToggleAllItemsInDeck(bool enabled)
        {
            foreach (EventButtonBehaviour button in _specialIcons)
            {
                button.UIButton.interactable = enabled;
            }
            _upDownIconSlot.UIButton.interactable = enabled;
            _neutralIconSlot.UIButton.interactable = enabled;
            _backIconSlot.UIButton.interactable = enabled;
            _forwardIconSlot.UIButton.interactable = enabled;
        }

        public void SetSelectedToLast()
        {
            Selected = _lastSelected;
        }

        public void UpdateDeck()
        {
            _movesList.UpdateUI(_buildManager.NormalDeck, _buildManager.SpecialDeck);
        }

        public void FocusAbilitySection(string sectionName)
        {
            foreach (AbilitySectionBehaviour section in _abilitySections)
            {
                if (section.name == sectionName)
                {
                    section.gameObject.SetActive(true);
                    UpdateIconChoicesWithType((int)section.AbilityType, true);
                    continue;
                }

                section.gameObject.SetActive(false);
            }
        }

        public void FocusAbilitySection(int sectionType)
        {
            foreach (AbilitySectionBehaviour section in _abilitySections)
            {
                if ((int)section.AbilityType == sectionType)
                {
                    section.gameObject.SetActive(true);
                    UpdateIconChoicesWithType((int)section.AbilityType, true);
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

            Selected = _abilitySections[0].IconHolder.GetChild(0).gameObject;
        }

        public void UpdateIconChoicesWithType(int type, bool setSelected = false)
        {
            // Get all the icons for the type that was chosen.
            Transform iconTransform = Array.Find(_abilitySections, section => section.AbilityType == (AbilityType)type).IconHolder;

            //Clear out all of the old icons in the section.
            for (int i = iconTransform.childCount - 1; i >= 0; i--)
            {
                Destroy(iconTransform.GetChild(i).gameObject);
                iconTransform.GetChild(i).SetParent(null);
            }

            List<AbilityData> data = _buildManager.ReplacementAbilities.AbilityData.FindAll(abilityData => abilityData.AbilityType == (AbilityType)type);

            // Populate new icons in the section.
            for (int i = 0; i < data.Count; i++)
            {
                AbilityData currentData = data[i];

                // Don't display abilities that are already in the users deck.
                if (_buildManager.NormalDeck.Contains(currentData) || _buildManager.SpecialDeck.Contains(currentData) || currentData.ShouldHideInMenu)
                    continue;

                // Create a new button for the ability.
                EventButtonBehaviour abilityButtonInstance = Instantiate(_abilityButton, iconTransform);

                //Initialize all of the things so it appears correctly and updates info properly.
                abilityButtonInstance.Init();
                abilityButtonInstance.ButtonImage.sprite = currentData.DisplayIcon;
                abilityButtonInstance.name = currentData.abilityName;

                MoveDescriptionBehaviour moveDescription = abilityButtonInstance.GetComponent<MoveDescriptionBehaviour>();

                moveDescription.Init(_infoTextBox, _infoPlayer, currentData);

                // When we select the ability as a replacement, add it to the deck and go back to the previous page.
                abilityButtonInstance.AddOnClickEvent(() =>
                {
                    _buildManager.CurrentAbilityType = type;
                    _buildManager.ReplaceAbility(currentData.abilityName);
                    UpdateDeck();
                    UpdateIconChoicesWithType(type, true);
                    _pageManager.GoToPageParent();
                });

                if (setSelected)
                {
                    Selected = abilityButtonInstance.gameObject;
                    abilityButtonInstance.OnSelect();
                    _eventSystem.UpdateModules();
                }
                setSelected = false;
            }
        }

        public void SetCurrentName(string currentName)
        {
            _originalName = currentName;
        }


        public void Save()
        {
            string path = Application.persistentDataPath + "/CustomCharacters" + "/" + _potentialName + "_ArmorSet.txt";

            if (String.IsNullOrEmpty(_potentialName))
            {
                GoToNameKeyBoard("You must give your character a name first.");
                return;
            }

            if (File.Exists(path) && _potentialName != _originalName)
            {
                GoToNameKeyBoard("A character has already been made using this name.");
                return;
            }

            _customCharacterManager.RenameFile(_potentialName);
            _customCharacterManager.SetCharacterName(_potentialName);
            string name = _customCharacterManager.CharacterName;

            _buildManager.RenameFile(name);
            _buildManager.SetDeckNames(name);

            _customCharacterManager.SaveCharacter();
            _buildManager.SaveDecks();
            SavePopupBehaviour.ChangesAreSaved.Value = true;
        }

        public void GoToNameKeyBoard(string prompt)
        {
            SetInputAny();
            ConfirmationMenuSpawner.Spawn(() =>
                    _pageManager.GoToPage("NameKeyboard"), null, _eventSystem,
                    prompt, leftText: "Okay"
                );
        }

        public void SetNamesEmpty()
        {
            _potentialName = string.Empty;
            _originalName = string.Empty;
            _customCharacterManager.SetCharacterName(_potentialName);
            _buildManager.SetDeckNames(_potentialName);
        }

        //need to handle saving changes to a name that already exists
        public void SetDeckNames(Text inputText)
        {
            SavePopupBehaviour.ChangesAreSaved.Value = false;

            _potentialName = inputText.text;
            _pageManager.GoToPageParent();
        }

        public void SetInputKeyboardOnly()
        {
            _playerInput.neverAutoSwitchControlSchemes = true;
            _controlSchemeManager.SwitchToGamepad(true);
        }

        public void SetInputAny()
        {
            _playerInput.neverAutoSwitchControlSchemes = false;
            _playerInput.enabled = true;
        }

        public void SetMakingNewCharacter(bool makingNewCharacter)
        {
            _makingNewCharacter = makingNewCharacter;
        }
    }
}