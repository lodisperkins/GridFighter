using Lodis.Gameplay;
using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Lodis.ScriptableObjects;
using UnityEngine.Events;
using Lodis.Input;

namespace Lodis.UI
{
    public class CSSManagerBehaviour : MonoBehaviour
    {
        [Header("CSS Menu Items")]

        [SerializeField]
        private int _currentPlayer = 1;
        [SerializeField]
        private GameObject _startText;
        [SerializeField]
        private PlayerColorManagerBehaviour _colorManager;
        [SerializeField]
        private BackgroundColorBehaviour _backgroundImage;

        [Header("P1 Menu Items")]

        [SerializeField]
        private GameObject _player1Root;
        [SerializeField]
        private Text _player1JoinInstruction;
        [SerializeField]
        private CSSCustomCharacterManager _p1CustomManager;
        [SerializeField]
        private PageManagerBehaviour _p1PageManager;
        [SerializeField]
        private MovesListBehaviour _player1MovesList;
        [SerializeField]
        private GameObject _p1InputProfileMenu;
        [SerializeField]
        private GameObject _readyP1Text;
        [SerializeField]
        private CharacterData _p1Data;
        [SerializeField]
        private InputRebindingBehaviour _p1Rebinder;

        [Header("P2 Menu Items")]

        [SerializeField]
        private MovesListBehaviour _player2MovesList;
        [SerializeField]
        private GameObject _player2Root;
        [SerializeField]
        private Text _player2JoinInstruction;
        [SerializeField]
        private CSSCustomCharacterManager _p2CustomManager;
        [SerializeField]
        private PageManagerBehaviour _p2PageManager;
        [SerializeField]
        private GameObject _p2InputProfileMenu;
        [SerializeField]
        private CharacterData _p2Data;
        [SerializeField]
        private GameObject _readyP2Text;
        [SerializeField]
        private BoolVariable _p2IsCustom;
        [SerializeField]
        private InputRebindingBehaviour _p2Rebinder;

        [Header("AI")]

        [SerializeField]
        private CharacterData _defaultAICharacter;
        [SerializeField]
        private CharacterData _dummyCharacter;

        private PlayerInputManager _inputManager;

        private bool _canStart;
        private bool _p1CharacterSelected;
        private bool _p2CharacterSelected;

        private bool _gridCreated;
        private int _p1ColorIndex = -1;
        private int _p2ColorIndex = -1;

        private string _player1SelectedCharacter;
        private string _player2SelectedCharacter;

        private bool _player1HasCustomSelected;
        private bool _player2HasCustomSelected;

        private MultiplayerEventSystem _player1EventSystem;
        private MultiplayerEventSystem _player2EventSystem;

        private UnityAction _onDisable;

        private bool _canSelectCharP1;
        private bool _canSelectCharP2;

        private void Start()
        {
            SetColor(1);
            SceneManager.sceneLoaded += ResetValues;
            SceneManagerBehaviour.Instance.P1Devices.Value = new InputDevice[0];
            SceneManagerBehaviour.Instance.P2Devices.Value = new InputDevice[0];
            
            _inputManager = GetComponent<PlayerInputManager>();
            if (!GridGameManager.IsHost)
            {
                _colorManager.SetPlayerColor(1, 5);
                _p1ColorIndex = 5;
                _canSelectCharP1 = true;
                SetDataP1(_defaultAICharacter);
                _player1JoinInstruction.enabled = false;
                _player1Root.SetActive(false);
                _currentPlayer = 2;
            }

            if (SceneManagerBehaviour.Instance.GameMode.Value == (int)GameMode.PlayerVSCPU || GridGameManager.IsHost)
            {
                _colorManager.SetPlayerColor(2, 5);
                _p2ColorIndex = 5;
                _canSelectCharP2 = true;
                SetDataP2(_defaultAICharacter);
                _player2JoinInstruction.enabled = false;
                _player2Root.SetActive(false);
                _p2IsCustom.Value = false;
            }
            else if (SceneManagerBehaviour.Instance.GameMode.Value == (int)GameMode.PRACTICE)
            {
                _colorManager.SetPlayerColor(2, 5);
                _p2ColorIndex = 5;
                _canSelectCharP2 = true;
                SetDataP2(_dummyCharacter);
                _player2JoinInstruction.enabled = false;
                _p2IsCustom.Value = false;
                _player2Root.SetActive(false);
            }
            SetColor(2);

        }

        private void OnDisable()
        {
            
            _onDisable?.Invoke();
            _onDisable = null;
        }

        private void ResetValues(Scene arg0, LoadSceneMode arg1)
        {
            SetColor(1);
            SetColor(2);
            _canStart = false;
            _p1CharacterSelected = false;
            _p2CharacterSelected = false;
        }
        private void ActivateMenu(PlayerInput playerInput, int playerNum)
        {
            if (!playerInput)
                return;

            if (playerNum == 1 && GridGameManager.IsHost)
            {
                _player1EventSystem = playerInput.GetComponent<MultiplayerEventSystem>();
                _player1EventSystem.playerRoot = _player1Root;
                _player1Root.SetActive(true);
                _p1CharacterSelected = false;
                _player1JoinInstruction.gameObject.SetActive(false);
                _p1CustomManager.SetEventSystems(_player1EventSystem);
                _p1CustomManager.SetSelectedToFirstOption();
                SceneManagerBehaviour.Instance.P1ControlScheme = playerInput.currentControlScheme;
                SceneManagerBehaviour.Instance.P1Devices.Value = playerInput.devices.ToArray();

                _p1Rebinder.ResetToDefault();
            }
            else if (playerNum == 2 && !GridGameManager.IsHost)
            {
                _player2EventSystem = playerInput.GetComponent<MultiplayerEventSystem>();
                _player2EventSystem.playerRoot = _player2Root;
                _player2Root.SetActive(true);
                _p2CharacterSelected = false;
                _player2JoinInstruction.gameObject.SetActive(false);
                _p2CustomManager.SetEventSystems(_player2EventSystem);
                _p2CustomManager.SetSelectedToFirstOption();
                SceneManagerBehaviour.Instance.P2ControlScheme = playerInput.currentControlScheme;
                SceneManagerBehaviour.Instance.P2Devices.Value = playerInput.devices.ToArray();

                _p2Rebinder.ResetToDefault();
            }
        }
        public void UpdateColor(int playerNum)
        {
            if (!_colorManager)
                return;
            if (playerNum == 1)
            {
                _colorManager.SetPlayerColor(playerNum, _p1ColorIndex);
                _backgroundImage.SetPrimaryColor(_colorManager.P1Color.Value / 2);
            }
            else if (playerNum == 2 && SceneManagerBehaviour.Instance.GameMode.Value != (int)GameMode.SINGLEPLAYER)
            {
                _colorManager.SetPlayerColor(playerNum, _p2ColorIndex);
                _backgroundImage.SetSecondaryColor(_colorManager.P2Color.Value / 2);
            }
        }
        private void SetColor(int playerNum)
        {
            if (!_colorManager)
                return;

            if (playerNum == 1 && !_p1CharacterSelected)
            {
                _p1ColorIndex++;

                if (_p1ColorIndex == _p2ColorIndex && _p2CharacterSelected)
                    _p1ColorIndex++;

                if (_p1ColorIndex >= _colorManager.PossibleColors.Length)
                    _p1ColorIndex = 0;

                _colorManager.SetPlayerColor(playerNum, _p1ColorIndex);
                _backgroundImage.SetPrimaryColor(_colorManager.P1Color.Value / 2);
            }
            else if (playerNum == 2 && SceneManagerBehaviour.Instance.GameMode.Value != (int)GameMode.SINGLEPLAYER && !_p2CharacterSelected)
            {
                _p2ColorIndex++;
                if (_p2ColorIndex == _p1ColorIndex && _p1CharacterSelected)
                    _p2ColorIndex++;
                if (_p2ColorIndex >= _colorManager.PossibleColors.Length)
                    _p2ColorIndex = 0;
                _colorManager.SetPlayerColor(playerNum, _p2ColorIndex);
                _backgroundImage.SetSecondaryColor(_colorManager.P2Color.Value / 2);
            }
        }
        public bool GetPlayerReady(int num)
        {
            if (num == 1)
                return _p1CharacterSelected;
            return _p2CharacterSelected;
        }
        private void GoToPage(InputAction.CallbackContext context, int playerNum)
        {
            if (GetPlayerSelected(playerNum) || !CheckMenuActive(playerNum) || GetMovesListOpen(playerNum))
                return;

            Vector2 direction = context.ReadValue<Vector2>();
            PageManagerBehaviour manager = null;
            CSSCustomCharacterManager charManager = null;


            if (playerNum == 1)
            {
                manager = _p1CustomManager.PageManager;
                charManager = _p1CustomManager;
            }
            else if (playerNum == 2)
            {
                manager = _p2CustomManager.PageManager;
                charManager = _p2CustomManager;
            }

            if (manager.CurrentPage.PageName == "InputProfile" + playerNum)
                return;

            if (direction == Vector2.right)
                manager.GoToPageChild(1);
            else if (direction == Vector2.left && charManager.HasCustomDecks)
                manager.GoToPageParent();
        }

        private bool CheckMenuActive(int playerNum)
        {
            if (!_player1Root)
                return false;

            if (playerNum == 1)
                return _player1Root.activeInHierarchy;

            return _player2Root.activeInHierarchy;
        }

        public void UpdateEventSystem(PlayerInput playerInput)
        {
            if (!_gridCreated)
            {
                BlackBoardBehaviour.Instance.Grid.CreateGrid();
                _gridCreated = true;
            }
            int num = _currentPlayer;

            PlayerControls controls = new PlayerControls();
            controls.Enable();
            controls.devices = playerInput.devices;

            playerInput.onActionTriggered += context =>
            {
                if (!GetPlayerSelected(num) && !CheckMenuActive(num) && !GetMovesListOpen(num))
                {
                    RoutineBehaviour.Instance.StartNewTimedAction(args => ActivateMenu(playerInput, num), TimedActionCountType.FRAME, 1);
                    RoutineBehaviour.Instance.StartNewTimedAction(args => SetCanSelectChar(num), TimedActionCountType.FRAME, 2);
                }

                SetDataP1(_defaultAICharacter);
            };

            controls.UI.Navigate.started += context => GoToPage(context, num);
            controls.UI.MiddleClick.started += context =>
            {
                if (_canStart)
                    StartMatch();
                else if (!GetPlayerReady(num))
                    ActivateMenu(playerInput, num);
            };

            controls.UI.RightClick.started += context =>
            {
                if (CheckMenuActive(num))
                    SetColor(num);
            };
            controls.UI.Cancel.started += context =>
            {
                if (GetPlayerReady(num))
                    ActivateMenu(playerInput, num);
                else
                    GoToPreviousPage(num);
            };
            controls.UI.Cancel.performed += context => TryGoingToMainMenu(num);
            controls.UI.Toggle.performed += context => OpenMoveList(num);
            controls.UI.Option1.Enable();
            controls.UI.Option1.performed += context => OpenControlsMenu(num);

            _onDisable += () =>
            {
                controls.Dispose();
                controls.Disable();
            };

            _currentPlayer = 2;
        }

        private void GoToPreviousPage(int playerNum)
        {
            PageManagerBehaviour pageManager = null;
            CSSCustomCharacterManager custManager = null;

            if (playerNum == 1)
            {
                pageManager = _p1PageManager;
                custManager = _p1CustomManager;
            }
            else if (playerNum == 2)
            {
                pageManager = _p2PageManager;
                custManager = _p2CustomManager;
            }

            if (pageManager.CurrentPage?.PageParent?.PageName == "custom" && !custManager.HasCustomDecks)
                pageManager.GoToPage("default");
            else if (pageManager.CurrentPage?.PageName == "default" && !custManager.HasCustomDecks)
                return;
            else
                pageManager.GoToPageParent();
        }

        private void SetCanSelectChar(int playerNum)
        {
            if (playerNum == 1)
                _canSelectCharP1 = true;
            else if (playerNum == 2)
                _canSelectCharP2 = true;
        }

        public void SetData(int player, CharacterData data)
        {
            if (player == 1 && _canSelectCharP1)
                SetDataP1(data);
            else if (player == 2 && _canSelectCharP2)
                SetDataP2(data);
        }
        public void SetDataP1(CharacterData data)
        {
            _p1Data.DisplayName = data.DisplayName;
            _p1Data.CharacterReference = data.CharacterReference;
            _p1Data.HeadShot = data.HeadShot;
            _p1CharacterSelected = true;
            _p1PageManager.GoToPage("ready");

            if (_p1ColorIndex == _p2ColorIndex && (!_p2CharacterSelected || SceneManagerBehaviour.Instance.GameMode != (int)GameMode.MULTIPLAYER))
                SetColor(2);
        }
        public void SetDataP2(CharacterData data)
        {

            _p2Data.DisplayName = data.DisplayName;
            _p2Data.CharacterReference = data.CharacterReference;
            _p2Data.HeadShot = data.HeadShot;
            _p2CharacterSelected = true;
            _p2PageManager.GoToPage("ready");

            if (_p2ColorIndex == _p1ColorIndex && !_p1CharacterSelected)
                SetColor(1);
        }

        public void UpdateP1SelectedName(CharacterData data)
        {
            _player1HasCustomSelected = false;

            _player1SelectedCharacter = data.DisplayName;
        }

        public void UpdateP2SelectedName(CharacterData data)
        {
            _player2HasCustomSelected = false;
            _player2SelectedCharacter = data.DisplayName;
        }

        public void UpdateCustomSelectedName(int playerNum, string name)
        {
            if (playerNum == 1)
            {
                _player1SelectedCharacter = name;
                _player1HasCustomSelected = true;
            }
            else if (playerNum == 2)
            {
                _player2SelectedCharacter = name;
                _player2HasCustomSelected = true;
            }
        }

        public bool GetPlayerSelected(int playerNum)
        {
            if (playerNum == 1)
                return _p1CharacterSelected;
            else if (playerNum == 2)
                return _p2CharacterSelected;

            return false;
        }

        private void LoadMovesListDecks(out Deck normal, out Deck special, string selectedCharacter, bool isCustom)
        {
            if (isCustom)
            {
                normal = DeckBuildingManagerBehaviour.LoadCustomNormalDeck(selectedCharacter);
                special = DeckBuildingManagerBehaviour.LoadCustomSpecialDeck(selectedCharacter);
            }
            else
            {
                normal = DeckBuildingManagerBehaviour.LoadPresetNormalDeck(selectedCharacter);
                special = DeckBuildingManagerBehaviour.LoadPresetSpecialDeck(selectedCharacter);
            }
        }

        private bool GetMovesListOpen(int playerNum)
        {
            if (!_player1MovesList)
                return false;

            if (playerNum == 1)
                return _player1MovesList.gameObject.activeInHierarchy;
            else if (playerNum == 2)
                return _player2MovesList.gameObject.activeInHierarchy;

            return false;
        }

        public void ResetRoot(int playerNum)
        {
            if (playerNum == 1)
                _player1EventSystem.playerRoot = _player1Root;
            else if (playerNum == 2)
                _player2EventSystem.playerRoot = _player2Root;
        }

        public void CloseMovesList(int playerNum)
        {
            if (playerNum == 1)
            {
                _player1MovesList.transform.parent.gameObject.SetActive(false);
                _player1EventSystem.playerRoot = _player1Root;
            }
            else if (playerNum == 2)
            {
                _player2MovesList.transform.parent.gameObject.SetActive(false);
                _player2EventSystem.playerRoot = _player2Root;
            }
        }

        public void OpenMoveList(int playerNum)
        {
            if (!CheckMenuActive(playerNum))
                return;

            Deck normal = null;
            Deck special = null;

            if (playerNum == 1 && _p1PageManager.CurrentPage.PageName != "InputProfile")
            {
                LoadMovesListDecks(out normal, out special, _player1SelectedCharacter, _player1HasCustomSelected);
                _player1MovesList.UpdateUI(normal, special, _player1EventSystem);
                _p1PageManager.GoToPageChild(0);
            }
            else if (playerNum == 2 && _p2PageManager.CurrentPage.PageName != "InputProfile")
            {
                LoadMovesListDecks(out normal, out special, _player2SelectedCharacter, _player2HasCustomSelected);
                _player2MovesList.UpdateUI(normal, special, _player2EventSystem);
                _p2PageManager.GoToPageChild(0);
            }
        }

        public void OpenControlsMenu(int playerNum)
        {
            if (!CheckMenuActive(playerNum))
                return;

            if (playerNum == 1)
            {
                _p1PageManager.GoToPage("InputProfile");
            }
            else if (playerNum == 2)
            {
                _p2PageManager.GoToPage("InputProfile");
            }
        }



        public void TryGoingToMainMenu(int playerNum)
        {
            if (SceneManagerBehaviour.Instance.SceneIndex == 4)
                return;
            if (_p1CharacterSelected && playerNum == 1)
                return;
            if (_p2CharacterSelected && playerNum == 2)
                return;
            SceneManagerBehaviour.Instance.LoadScene(1);
        }
        public void StartMatch()
        {
            if (SceneManagerBehaviour.Instance.GameMode.Value == (int)GameMode.PlayerVSCPU || SceneManagerBehaviour.Instance.GameMode.Value == (int)GameMode.MULTIPLAYER)
                SceneManagerBehaviour.Instance.LoadScene(4);
            else if (SceneManagerBehaviour.Instance.GameMode.Value == (int)GameMode.PRACTICE)
                SceneManagerBehaviour.Instance.LoadScene(5);
        }
        void Update()
        {
            if (_currentPlayer <= 1)
                return;
            _canStart = _p1CharacterSelected && _p2CharacterSelected;
            _startText.SetActive(_canStart);
        }
    }
}