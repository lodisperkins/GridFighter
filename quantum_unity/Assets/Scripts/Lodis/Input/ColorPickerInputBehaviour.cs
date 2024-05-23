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
        private InputAction _action;
        private int _currentIndex;
        private bool _canChangeColor;

        // Start is called before the first frame update
        void Start()
        {
            _playerColorManager = GetComponent<PlayerColorManagerBehaviour>();
            _playerColorManager.SetPlayerColor(1, 0);
            _action = _playerInput.actions.actionMaps[1].FindAction("RightClick");
        }

        private void SetColor()
        {
            _currentIndex++;

            if (_currentIndex >= _playerColorManager.PossibleColors.Length)
                _currentIndex = 0;

            _playerColorManager.SetPlayerColor(1, _currentIndex);
        }

        private void Update()
        {
            bool buttonDown = _action.ReadValue<float>() == 1;

            if (buttonDown && _canChangeColor)
            {
                SetColor();
                _canChangeColor = false;
            }

            if (!buttonDown)
                _canChangeColor = true;
        }
    }
}
