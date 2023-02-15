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
        private GameObject _player1FirstSelected;
        [SerializeField]
        private GameObject _player2Root;
        [SerializeField]
        private GameObject _player2FirstSelected;
        [SerializeField]
        private CharacterData _p1Data;
        [SerializeField]
        private CharacterData _p2Data;

        [SerializeField]
        private int _currentPlayer = 1;

        public void UpdateEventSystem(PlayerInput playerInput)
        {
            MultiplayerEventSystem eventSystem = playerInput.GetComponent<MultiplayerEventSystem>();

            if (_currentPlayer == 1)
            {
                eventSystem.playerRoot = _player1Root;
                eventSystem.firstSelectedGameObject = _player1FirstSelected;
                _currentPlayer++;
                SceneManagerBehaviour.Instance.P1ControlScheme = playerInput.currentControlScheme;
                SceneManagerBehaviour.Instance.P1Devices = playerInput.devices.ToArray();
            }
            else if (_currentPlayer == 2)
            {
                eventSystem.playerRoot = _player2Root;
                eventSystem.firstSelectedGameObject = _player2FirstSelected;
                SceneManagerBehaviour.Instance.P2ControlScheme = playerInput.currentControlScheme;
                SceneManagerBehaviour.Instance.P2Devices = playerInput.devices.ToArray();
            }
        }

        public void SetDataP1(CharacterData data)
        {
            _p1Data.name = data.name;
            _p1Data.CharacterReference = data.CharacterReference;
        }

        public void SetDataP2(CharacterData data)
        {
            _p2Data.name = data.name;
            _p2Data.CharacterReference = data.CharacterReference;
        }

        public void StartMatch()
        {
            SceneManagerBehaviour.Instance.LoadScene(2);
        }
    }
}