using Lodis.Gameplay;
using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Lodis.UI
{
    public class CSUIManagerBehaviour : MonoBehaviour
    {
        [SerializeField]
        private GameObject _player1Root;
        [SerializeField]
        private Button _player1FirstSelected;
        [SerializeField]
        private Text _player1JoinInstruction;
        [SerializeField]
        private GameObject _player2Root;
        [SerializeField]
        private Button _player2FirstSelected;
        [SerializeField]
        private Text _player2JoinInstruction;
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

                eventSystem.SetSelectedGameObject(_player1FirstSelected.gameObject);
                _player1FirstSelected.OnSelect(null);
                _p1CharacterSelected = false;
                _player1JoinInstruction.gameObject.SetActive(false);

                SceneManagerBehaviour.Instance.P1ControlScheme = playerInput.currentControlScheme;
                SceneManagerBehaviour.Instance.P1Devices = playerInput.devices.ToArray();
            }
            else if (playerNum == 2)
            {
                eventSystem.playerRoot = _player2Root;
                _player2Root.SetActive(true); 

                eventSystem.SetSelectedGameObject(_player2FirstSelected.gameObject);
                _player2FirstSelected.OnSelect(null);
                _p2CharacterSelected = false;
                _player2JoinInstruction.gameObject.SetActive(false);

                SceneManagerBehaviour.Instance.P2ControlScheme = playerInput.currentControlScheme;
                SceneManagerBehaviour.Instance.P2Devices = playerInput.devices.ToArray();
            }
        }

        private void SetColor(int playerNum)
        {
            if (playerNum == 1)
            {
                _p1ColorIndex++;

                if (_p1ColorIndex >= _colorManager.PossibleColors.Length)
                    _p1ColorIndex = 0;

                _colorManager.SetPlayerColor(playerNum, _p1ColorIndex);
            }
            else if (playerNum == 2)
            {
                _p2ColorIndex++;

                if (_p2ColorIndex >= _colorManager.PossibleColors.Length)
                    _p2ColorIndex = 0;

                _colorManager.SetPlayerColor(playerNum, _p2ColorIndex);
            }
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

            playerInput.actions.actionMaps[1].FindAction("MiddleClick").started += context =>
            {
                StartMatch();
                ActivateMenu(playerInput, num);
            };

            playerInput.actions.actionMaps[1].FindAction("RightClick").started += context => SetColor(num);

            _currentPlayer = 2;
        }

        public void SetDataP1(CharacterData data)
        {
            _p1Data.name = data.name;
            _p1Data.CharacterReference = data.CharacterReference;
            _p1CharacterSelected = true;
        }

        public void SetDataP2(CharacterData data)
        {
            _p2Data.name = data.name;
            _p2Data.CharacterReference = data.CharacterReference;
            _p2CharacterSelected = true;
        }

        public void StartMatch()
        {
            if (!_canStart)
                return;

            SceneManagerBehaviour.Instance.LoadScene(3);
        }

        void Update()
        {
            if (_currentPlayer <= 1)
                return;

            _player1Root.SetActive(!_p1CharacterSelected);
            _readyP1Text.SetActive(_p1CharacterSelected);
            _readyP2Text.SetActive(_p2CharacterSelected);

            _canStart = _p1CharacterSelected && _p2CharacterSelected;
            _startText.SetActive(_canStart);
        }
    }
}