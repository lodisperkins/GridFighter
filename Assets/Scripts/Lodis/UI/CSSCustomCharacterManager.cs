
using Lodis.CharacterCreation;
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
        private AutoScrollBehaviour _customMenuScrollBehaviour;
        [SerializeField]
        private DisplayCharacterSpawnBehaviour _characterSpawner;
        [SerializeField]
        private GameObject _characterDisplayModel;
        [SerializeField]
        private GameObject _customMenu;
        [SerializeField]
        private CharacterData _customCharacterData;
        [SerializeField]
        private CSSManagerBehaviour _characterSelectManager;
        [SerializeField]
        [Range(1,2)]
        private int _playerNum;

        [Header("Current Deck Page")]
        [SerializeField]
        private EventButtonBehaviour _loadoutButton;
        [SerializeField]
        private GameObject _loadoutOptions;

        private List<EventButtonBehaviour> _deckChoices = new List<EventButtonBehaviour>();
        [SerializeField]
        private EventSystem _eventSystem;
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
        public EventSystem EventManager { get => _eventSystem; set => _eventSystem = value; }

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
            Selected = _deckChoices[0].gameObject;
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
                    if (!_customCharacterManager.CustomCharacter)
                        _characterSpawner.SpawnEntity(_characterDisplayModel); 
                });

                buttonInstance.AddOnSelectEvent(() => _customCharacterManager.LoadCustomCharacter(optionName));

                buttonInstance.AddOnClickEvent(() =>
                {
                    _buildManager.LoadCustomDeck(optionName);
                    _characterSelectManager.SetData(_playerNum, _customCharacterData);
                });
                _deckChoices.Add(buttonInstance);
            }
        }

        public void SetEventSystems(EventSystem eventSystem)
        {
            _eventSystem = eventSystem;
            _customMenuScrollBehaviour.EventSystem = eventSystem;
        }

        public void SetSelectedToLast()
        {
            Selected = _lastSelected;
        }
    }
}