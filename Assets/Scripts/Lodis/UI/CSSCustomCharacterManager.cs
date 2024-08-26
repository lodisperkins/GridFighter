
using Lodis.CharacterCreation;
using Lodis.ScriptableObjects;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Lodis.UI
{
    public class CSSCustomCharacterManager : MonoBehaviour
    {
        [SerializeField]
        private DeckBuildingManagerBehaviour _buildManager;
        [SerializeField]
        private CustomCharacterManagerBehaviour _customCharacterManager;
        [SerializeField]
        private PageManagerBehaviour _pageManager;
        [SerializeField]
        private AutoScrollBehaviour _customMenuScrollBehaviour;
        [SerializeField]
        private AutoScrollBehaviour _defaultMenuScrollBehaviour;
        [SerializeField]
        private AutoScrollBehaviour _inputProfileScrollBehaviour;
        [SerializeField]
        private DisplayCharacterSpawnBehaviour _characterSpawner;
        [SerializeField]
        private GameObject _characterDisplayModel;
        [SerializeField]
        private GameObject _customMenu;
        [SerializeField]
        private CharacterData _customCharacterData;
        [SerializeField]
        private BoolVariable _customFlag;
        [SerializeField]
        private CSSManagerBehaviour _characterSelectManager;
        [SerializeField]
        [Range(1,2)]
        private int _playerNum;
        [SerializeField]
        private GameObject _arrows;

        [Header("Current Deck Page")]
        [SerializeField]
        private EventButtonBehaviour _loadoutButton;
        [SerializeField]
        private GameObject _loadoutOptions;

        private List<EventButtonBehaviour> _deckChoices = new List<EventButtonBehaviour>();
        [SerializeField]
        private UnityEngine.EventSystems.EventSystem _eventSystem;
        [SerializeField]
        private GameObject _firstDefaultOption;
        private GameObject _lastSelected;


        public GameObject Selected
        {
            get { return EventManager.currentSelectedGameObject; }
            set
            {
                EventManager.SetSelectedGameObject(value);
                _lastSelected = value;
            }
        }
        public UnityEngine.EventSystems.EventSystem EventManager { get => _eventSystem; set => _eventSystem = value; }
        public PageManagerBehaviour PageManager { get => _pageManager; set => _pageManager = value; }
        public bool HasCustomDecks
        {
            get { return _deckChoices.Count > 0; }
        }

        // Start is called before the first frame update
        void Start()
        {
            _characterSpawner.AddOnSpawnEvent(() =>
            {
                if (!_customMenu.activeInHierarchy)
                    return;

                _customCharacterManager.CustomCharacter = _characterSpawner.PreviousCharacterInstance.GetComponentInChildren<MeshReplacementBehaviour>();
            });

            UpdateLoadoutOptions();
        }

        public void SetSelectedToFirstOption()
        {
            _pageManager.GoToRootPage();
            if (_deckChoices.Count > 0)
                Selected = _deckChoices[0].gameObject;
            else
            {
                Selected = _firstDefaultOption;
                _pageManager.GoToPageChild(1);
                EventButtonBehaviour buttonBehaviour = _firstDefaultOption.GetComponent<EventButtonBehaviour>();
                buttonBehaviour.OnSelect(null);
                buttonBehaviour.OnSelect();
                EventManager.UpdateModules();
            }
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

            EventButtonBehaviour previousInstance = null;

            for (int i = 0; i < _buildManager.DeckOptions.Length; i++)
            {
                string optionName = _buildManager.DeckOptions[i];

                if (optionName == null)
                    continue;

                EventButtonBehaviour buttonInstance = Instantiate(_loadoutButton, _loadoutOptions.transform);
                buttonInstance.Init();

                buttonInstance.GetComponentInChildren<Text>().text = optionName;

                buttonInstance.AddOnSelectEvent(() =>
                {
                    //This isn't null when it's selected again. Look at property of custom character.
                    if (!_customCharacterManager.CustomCharacter)
                        _characterSpawner.SpawnEntity(_characterDisplayModel); 
                });

                buttonInstance.AddOnSelectEvent(() =>
                {
                    _customCharacterManager.LoadCustomCharacter(optionName);
                    _characterSelectManager.UpdateColor(_playerNum);
                    _characterSelectManager.UpdateCustomSelectedName(_playerNum, optionName);
                });

                buttonInstance.AddOnClickEvent(() =>
                {
                    _buildManager.LoadCustomDeck(optionName);
                    _customCharacterData.DisplayName = optionName;
                    _characterSelectManager.SetData(_playerNum, _customCharacterData);
                    _customFlag.Value = true;
                });

                if (PageManager.RootPage.FirstSelected == null)
                    PageManager.RootPage.FirstSelected = buttonInstance.gameObject;

                _deckChoices.Add(buttonInstance);

                Navigation navigationRules = new Navigation();
                navigationRules.mode = Navigation.Mode.Explicit;

                if (previousInstance)
                {
                    navigationRules.selectOnUp = previousInstance.UIButton;
                    Navigation previousNavigation = previousInstance.UIButton.navigation;

                    previousNavigation.selectOnDown = buttonInstance.UIButton;
                    previousInstance.UIButton.navigation = previousNavigation;
                }

                buttonInstance.UIButton.navigation = navigationRules;
                previousInstance = buttonInstance;
            }

            _arrows.SetActive(_buildManager.DeckOptions.Length != 0);
        }

        public void SetEventSystems(UnityEngine.EventSystems.EventSystem eventSystem)
        {
            _eventSystem = eventSystem;
            _customMenuScrollBehaviour.EventSystem = eventSystem;
            _defaultMenuScrollBehaviour.EventSystem = eventSystem;
            PageManager.EventManager = eventSystem;    
            _inputProfileScrollBehaviour.EventSystem = eventSystem;
        }

        public void SetSelectedToLast()
        {
            Selected = _lastSelected;
        }
    }
}