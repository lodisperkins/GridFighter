using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lodis.Movement;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Lodis.Utility;
using Lodis.ScriptableObjects;
using UnityEngine.Animations;
using Lodis.Input;
using UnityEngine.EventSystems;

namespace Lodis.UI
{

    public class BindingImageBehaviour : MonoBehaviour
    {
        [System.Serializable]
        public class ButtonData
        {
            [Tooltip("The action that goes along with this image.")]
            public BindingType Binding;
            public void UpdateButtonImage(string device, string manufacturer = "", string displayName = "")
            {
                string path = "ButtonPrompts/";

                if (manufacturer == "Sony Interactive Entertainment")
                    path += "Others/PS4/PS4_" + displayName;
                else if (device == "Keyboard" || device == "Mouse")
                    path += "Keyboard & Mouse/Light/" + displayName + "_Key_Light";
                else
                    path += "Others/Xbox 360/360_" + displayName;

                ImageToUpdate.sprite = Resources.Load<Sprite>(path);
            }

            public void SetImageToDefault()
            {
                ImageToUpdate.sprite = DefaultImage;
            }

            [Tooltip("The image to change into the correct binding image.")]
            public Image ImageToUpdate;
            [Tooltip("The image to display in case this action doesn't have a binding.")]
            public Sprite DefaultImage;
        }

        private bool _updatedButtons;
        [SerializeField]
        private ButtonData[] _buttonImages;
        [SerializeField]
        private InputProfileData _profileData;
        [SerializeField]
        private InputProfileData _xBoxData;
        [SerializeField]
        private InputProfileData _playstationData;
        [SerializeField]
        private InputProfileData _PCData;
        [SerializeField]
        private EventSystem _eventSystem;
        [Tooltip("If true, will search for the move component and grab the data based on the alignement.")]
        [SerializeField]
        private bool _getDataFromAlignment;
        [Tooltip("If true, will auto update the buttons during the first update.")]
        [SerializeField]
        private bool _updateButtonsAutomatically;
        [Tooltip("If true, will try to update images for movement on controllers.")]
        [SerializeField]
        private bool _dontIgnoreMove;

        // Start is called before the first frame update
        void Start()
        {
            if (_getDataFromAlignment)
            {
                GridMovementBehaviour movement = GetComponentInParent<GridMovementBehaviour>();

                if (movement.Alignment == GridScripts.GridAlignment.LEFT)
                    _profileData = SceneManagerBehaviour.Instance.P1InputProfile;
                else
                    _profileData = SceneManagerBehaviour.Instance.P2InputProfile;
            }
        }

        public void UpdateButtons(bool setSelected = true)
        {
            string deviceName = "";
            string manufacturer = "";

            //Stores the device and manufacturer names if theres input data.
            if (_profileData != null && _profileData.DeviceData?.Length > 0)
            {
                deviceName = _profileData.DeviceData[0].name;
                manufacturer = _profileData.DeviceData[0].description.manufacturer;
            }
            //Otherwise turn off images.
            else
            {
                foreach (ButtonData button in _buttonImages)
                {
                    button.ImageToUpdate.enabled = false;
                }
                return;
            }

            //Go through all the buttons to set their images.
            for (int i = 0;  i < _buttonImages.Length; i++)
            {
                ButtonData button = _buttonImages[i];

                //If the binding is for movement and the device isn't a keyboard...
                if ((int)button.Binding < 4 && (deviceName != "Keyboard" && deviceName != "Mouse") && !_dontIgnoreMove)
                {
                    //...disable it and continue.
                    button.ImageToUpdate.transform.parent.gameObject.SetActive(false);
                    continue;
                }

                if (button.Binding == BindingType.WeakAttack && (deviceName != "Keyboard" && deviceName != "Mouse") && setSelected)
                {
                    //...disable it and continue.
                    _eventSystem.SetSelectedGameObject(button.ImageToUpdate.transform.parent.gameObject);
                }

                //Update the device image using the appropriate binding.
                button.ImageToUpdate.enabled = true;
                button.ImageToUpdate.transform.parent.gameObject.SetActive(true);

                RebindData data = _profileData.GetBinding(button.Binding);

                if (data.DisplayName == "" && button.DefaultImage == null)
                {
                    button.ImageToUpdate.enabled = false;
                }
                else if (data.DisplayName == "" && button.DefaultImage != null)
                {
                    button.SetImageToDefault();
                }
                else
                {
                    button.UpdateButtonImage(deviceName, manufacturer, data.DisplayName);
                }
            }

            _updatedButtons = true;
        }

        public RebindData GetDefaultData(string device, string manufacturer, BindingType binding)
        {
            RebindData data = null;
            if (manufacturer == "Sony Interactive Entertainment")
                data = _playstationData.GetBinding(binding);
            else if (device == "Keyboard" || device == "Mouse")
                data = _PCData.GetBinding(binding);
            else
                data = _xBoxData.GetBinding(binding);

            return data;
        }

        // Update is called once per frame
        void Update()
        {
            if (!_updatedButtons && _updateButtonsAutomatically)
                UpdateButtons(false);
        }

    }
}