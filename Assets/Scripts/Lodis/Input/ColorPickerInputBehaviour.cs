using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Lodis.UI
{
    public class ColorPickerInputBehaviour : MonoBehaviour
    {
        [SerializeField]
        private PlayerInput _playerInput;
        private PlayerColorManagerBehaviour _playerColorManager;
        private int _currentIndex;

        // Start is called before the first frame update
        void Start()
        {
            _playerColorManager = GetComponent<PlayerColorManagerBehaviour>();
            _playerColorManager.SetPlayerColor(1, 0);
            _playerInput.actions.actionMaps[1].FindAction("RightClick").started += SetColor;
        }

        private void SetColor(InputAction.CallbackContext context)
        {
            _currentIndex++;

            if (_currentIndex >= _playerColorManager.PossibleColors.Length)
                _currentIndex = 0;

            _playerColorManager.SetPlayerColor(1, _currentIndex);
        }

        private void OnDisable()
        {
            _playerInput.actions.actionMaps[1].FindAction("RightClick").started -= SetColor;
        }
    }
}
