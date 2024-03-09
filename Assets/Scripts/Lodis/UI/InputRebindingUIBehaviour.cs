using DG.Tweening.Core.Easing;
using Lodis.Input;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Lodis.UI
{
    public class InputRebindingUIBehaviour : MonoBehaviour
    {
        [SerializeField]
        private InputRebindingBehaviour _rebindHandler;
        [SerializeField]
        private Text _infoBoxText;
        [SerializeField]
        private Transform _profileOptions;
        [SerializeField]
        private EventButtonBehaviour _backButton;
        [SerializeField]
        private EventButtonBehaviour _profileButton;
        [SerializeField]
        private UnityEvent _onProfileButtonClicked;
        private List<EventButtonBehaviour> _profileChoices;

        [SerializeField]
        private Transform[] _moveBindings;
        [SerializeField]
        private Transform _weakBinding;
        [SerializeField]
        private GameObject _listeningPanel;
        [SerializeField]
        private EventSystem _eventSystem;
        [SerializeField]
        private PageManagerBehaviour _pageManager;
        [SerializeField]
        private GameObject _videoPanel;
        [SerializeField]
        private InputActionReference _deleteAction;
        [SerializeField]
        private string _deletionPrompt;
        private bool _profileOptionSelected;

        public InputRebindingBehaviour RebindHandler { get => _rebindHandler; private set => _rebindHandler = value; }

        public void Awake()
        {
            _profileChoices = new List<EventButtonBehaviour>();
            _deleteAction.action.performed += AskDeleteOption;
            _deleteAction.action.Enable();
        }

        public void UpdateInfoBoxName()
        {
            _infoBoxText.text = RebindHandler.ProfileName;
        }

        public void SetListeningPanelActive(bool active)
        {
            _listeningPanel.SetActive(active);
        }

        public void SelectFirstBinding()
        {
            if (_moveBindings[0].gameObject.activeInHierarchy)
                _eventSystem.SetSelectedGameObject(_moveBindings[0].gameObject);    
            else
                _eventSystem.SetSelectedGameObject(_weakBinding.gameObject);
        }

        public void SetProfileOptionSelected(bool selected)
        {
            _profileOptionSelected = selected;
        }

        private void AskDeleteOption(InputAction.CallbackContext callbackContext)
        {
            if (_pageManager.CurrentPage.PageName != "ControllerProfiles" || !_profileOptionSelected)
                return;

            _profileOptions.parent.gameObject.SetActive(false);
            _pageManager.enabled = false;
            string newPrompt = _deletionPrompt + " " + RebindHandler.ProfileName + "?";

            ConfirmationMenuSpawner.Spawn(DeleteOption, EnableProfileOptionsMenu, _eventSystem, newPrompt, _backButton.gameObject);
        }

        private void EnableProfileOptionsMenu()
        {
            _profileOptions.parent.gameObject.SetActive(true);
            _pageManager.enabled = true;
        }

        private void DeleteOption()
        {
            EnableProfileOptionsMenu();

            RebindHandler.DeleteInputProfile();

            UpdateProfileOptions();
        }

        public virtual void UpdateProfileOptions()
        {
            if (RebindHandler.ProfileOptions == null)
                return;

            for (int i = _profileChoices.Count - 1; i >= 0; i--)
            {
                Destroy(_profileChoices[i].gameObject);
                _profileChoices[i].transform.SetParent(null);
                _profileChoices[i].gameObject.SetActive(false);
            }

            _profileChoices.Clear();

            foreach (string optionName in RebindHandler.ProfileOptions)
            {
                if (optionName == null)
                    continue;

                EventButtonBehaviour buttonInstance = Instantiate(_profileButton, _profileOptions.transform);
                buttonInstance.GetComponentInChildren<Text>().text = optionName;

                buttonInstance.AddOnSelectEvent(() =>
                {
                    RebindHandler.ProfileName = optionName;
                    _profileOptionSelected = true;
                });

                buttonInstance.AddOnClickEvent(() =>
                {
                    RebindHandler.LoadProfile(optionName);
                    _pageManager.GoToPageChild(1);
                    _videoPanel?.SetActive(false);
                    _infoBoxText.text = optionName;
                    _profileOptionSelected = true;
                    _onProfileButtonClicked?.Invoke();
                });
                _profileChoices.Add(buttonInstance);
            }
        }
    }
}