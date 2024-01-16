using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lodis.Movement;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Lodis.Utility;

namespace Lodis.UI
{
    public enum DeviceType
    {
        KEYBOARD,
        XBOX,
        PLAYSTATION
    }

    public class DeviceImageBehaviour : MonoBehaviour
    {
        [System.Serializable]
        public class ButtonData
        {
            public Sprite KeyboardButton;
            public Sprite XBoxButton;
            public Sprite PlaystationButton;

            public void UpdateButtonImage(string device, string manufacturer = "")
            {
                if (manufacturer == "Sony Interactive Entertainment")
                    ImageToUpdate.sprite = PlaystationButton;
                else if (device == "Keyboard" || device == "Mouse")
                    ImageToUpdate.sprite = KeyboardButton;
                else
                    ImageToUpdate.sprite = XBoxButton;
            }

            public Image ImageToUpdate;
        }

        [SerializeField, Range(1,2)]
        private int _playerID;
        [SerializeField]
        private bool _getPlayerIDFromAlignment;
        [SerializeField]
        private ButtonData[] _actions;
        private bool _updatedButtons;


        // Start is called before the first frame update
        void Start()
        {
            if (_getPlayerIDFromAlignment)
            {
                GridMovementBehaviour movement = GetComponentInParent<GridMovementBehaviour>();
                _playerID = movement.Alignment == GridScripts.GridAlignment.LEFT ? 1 : 2;
            }
        }

        private void UpdateButtons()
        {
            string deviceName = ""; 
            string manufacturer = "";

            if (_playerID == 1 && SceneManagerBehaviour.Instance.P1Devices?.Length > 0)
            {
                deviceName = SceneManagerBehaviour.Instance.P1Devices[0].name;
                manufacturer = SceneManagerBehaviour.Instance.P1Devices[0].description.manufacturer;
            }
            else if (_playerID == 2 && SceneManagerBehaviour.Instance.P2Devices?.Length > 0)
            {
                deviceName = SceneManagerBehaviour.Instance.P2Devices[0].name;
                manufacturer = SceneManagerBehaviour.Instance.P2Devices[0].description.manufacturer;
            }
            else
            {
                foreach (ButtonData button in _actions)
                {
                    button.ImageToUpdate.enabled = false;
                }
                return;
            }

            foreach (ButtonData button in _actions)
            {
                button.ImageToUpdate.enabled = true;
                button.UpdateButtonImage(deviceName, manufacturer);
            }

            _updatedButtons = true;
        }

        // Update is called once per frame
        void Update()
        {
            if (!_updatedButtons)
                UpdateButtons();
        }
        
    }
}