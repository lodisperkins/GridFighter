using Lodis.Gameplay;
using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace Lodis.UI
{
    public class CSUIManagerBehaviour : MonoBehaviour
    {
        [SerializeField]
        private GameObject _player1Root;
        [SerializeField]
        private CharacterSelectButtonBehaviour _player1FirstSelected;
        [SerializeField]
        private GameObject _player2Root;
        [SerializeField]
        private CharacterSelectButtonBehaviour _player2FirstSelected;
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
        private bool _canStart;
        private bool _p1CharacterSelected;
        private bool _p2CharacterSelected;
        private bool _gridCreated;

        [SerializeField]
        private int _currentPlayer = 1;

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

                SceneManagerBehaviour.Instance.P2ControlScheme = playerInput.currentControlScheme;
                SceneManagerBehaviour.Instance.P2Devices = playerInput.devices.ToArray();
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

            SceneManagerBehaviour.Instance.LoadScene(2);
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