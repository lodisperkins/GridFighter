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

namespace Lodis.UI
{
    public class CSSManagerBehaviour : MonoBehaviour
    {
        [SerializeField]
        private GameObject _player1Root;
        [SerializeField]
        private Text _player1JoinInstruction;
        [SerializeField]
        private CSSCustomCharacterManager _p1CustomManager;
        [SerializeField]
        private GameObject _player2Root;
        [SerializeField]
        private Text _player2JoinInstruction;
        [SerializeField]
        private CSSCustomCharacterManager _p2CustomManager;
        [SerializeField]
        private CharacterData _p1Data;
        [SerializeField]
        private CharacterData _p2Data;
        [SerializeField]
        private GameObject _startText;
        [SerializeField]
        private GameObject _readyP1Text;
        [SerializeField]
        private GameObject _readyP2Text;
        [SerializeField]
        private PlayerColorManagerBehaviour _colorManager;
        [SerializeField]
        private BackgroundColorBehaviour _backgroundImage;
        [SerializeField]
        private CharacterData _defaultAICharacter;
        [SerializeField]
        private BoolVariable _p2IsCustom;
        private PlayerInputManager _inputManager;
        private bool _canStart;
        private bool _p1CharacterSelected;
        private bool _p2CharacterSelected;
        private bool _gridCreated;
        private int _p1ColorIndex = -1;
        private int _p2ColorIndex = -1;

        [SerializeField]
        private int _currentPlayer = 1;

        private void Start()
        {
            SetColor(1);
            SetColor(2);
            SceneManager.sceneLoaded += ResetValues;
            _inputManager = GetComponent<PlayerInputManager>();

            if (SceneManagerBehaviour.Instance.GameMode.Value == (int)GameMode.PRACTICE)
            {
                SetDataP2(_defaultAICharacter);
                _player2JoinInstruction.enabled = false;
                _p2IsCustom.Value = false;
            }
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

            MultiplayerEventSystem eventSystem = playerInput.GetComponent<MultiplayerEventSystem>();

            if (playerNum == 1)
            {
                eventSystem.playerRoot = _player1Root;
                _player1Root.SetActive(true);
                _p1CharacterSelected = false;
                _player1JoinInstruction.gameObject.SetActive(false);

                _p1CustomManager.SetEventSystems(eventSystem);
                _p1CustomManager.SetSelectedToFirstOption();

                SceneManagerBehaviour.Instance.P1ControlScheme = playerInput.currentControlScheme;
                SceneManagerBehaviour.Instance.P1Devices = playerInput.devices.ToArray();
            }
            else if (playerNum == 2)
            {
                eventSystem.playerRoot = _player2Root;
                _player2Root.SetActive(true); 
                _p2CharacterSelected = false;
                _player2JoinInstruction.gameObject.SetActive(false);

                _p2CustomManager.SetEventSystems(eventSystem);
                _p2CustomManager.SetSelectedToFirstOption();

                SceneManagerBehaviour.Instance.P2ControlScheme = playerInput.currentControlScheme;
                SceneManagerBehaviour.Instance.P2Devices = playerInput.devices.ToArray();
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

            if (playerNum == 1)
            {
                _p1ColorIndex++;

                if (_p1ColorIndex >= _colorManager.PossibleColors.Length)
                    _p1ColorIndex = 0;

                _colorManager.SetPlayerColor(playerNum, _p1ColorIndex);
                _backgroundImage.SetPrimaryColor(_colorManager.P1Color.Value / 2);
            }
            else if (playerNum == 2 && SceneManagerBehaviour.Instance.GameMode.Value != (int)GameMode.SINGLEPLAYER)
            {
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
            Vector2 direction = context.ReadValue<Vector2>();

            PageManagerBehaviour manager = null;

            if (playerNum == 1)
                manager = _p1CustomManager.PageManager;
            else if (playerNum == 2)
                manager = _p2CustomManager.PageManager;

            if (direction == Vector2.right)
                manager.GoToNextPage();
            else if (direction == Vector2.left)
                manager.GoToPreviousPage();
        }

        public void UpdateEventSystem(PlayerInput playerInput)
        {
            if (!_gridCreated)
            {
                BlackBoardBehaviour.Instance.Grid.CreateGrid();
                _gridCreated = true;
            }
            int num = _currentPlayer;
            playerInput.actions.actionMaps[1].FindAction("Cancel").started += context => ActivateMenu(playerInput, num);
            playerInput.actions.actionMaps[1].FindAction("Navigate").started += context => GoToPage(context, num);

            playerInput.actions.actionMaps[1].FindAction("MiddleClick").started += context =>
            {
                if (_canStart)
                    StartMatch();
                else if (!GetPlayerReady(num))
                    ActivateMenu(playerInput, num);
            };

            playerInput.actions.actionMaps[1].FindAction("RightClick").started += context => SetColor(num);
            playerInput.actions.actionMaps[1].FindAction("Cancel").performed += context => TryGoingToMainMenu(_currentPlayer);

            _currentPlayer = 2;
        }

        public void SetData(int player, CharacterData data)
        {
            if (player == 1)
                SetDataP1(data);
            else if (player == 2)
                SetDataP2(data);
        }

        public void SetDataP1(CharacterData data)
        {
            _p1Data.DisplayName = data.DisplayName;
            _p1Data.CharacterReference = data.CharacterReference;
            _p1Data.HeadShot = data.HeadShot;
            _p1CharacterSelected = true;
            _player1Root.SetActive(false);
        }

        public void SetDataP2(CharacterData data)
        {
            _p2Data.DisplayName = data.DisplayName;
            _p2Data.CharacterReference = data.CharacterReference;
            _p2Data.HeadShot = data.HeadShot;
            _p2CharacterSelected = true;
            _player2Root.SetActive(false);
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
            SceneManagerBehaviour.Instance.LoadScene(4);
        }

        void Update()
        {
            if (_currentPlayer <= 1)
                return;

            _readyP1Text.SetActive(_p1CharacterSelected);
            _readyP2Text.SetActive(_p2CharacterSelected);

            _canStart = _p1CharacterSelected && _p2CharacterSelected;
            _startText.SetActive(_canStart);
        }
    }
}