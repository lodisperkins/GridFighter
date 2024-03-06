using DG.Tweening.Core.Easing;
using Lodis.Input;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.EventSystems;
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
        private Transform _backButton;
        [SerializeField]
        private EventButtonBehaviour _profileButton;
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

        public void Awake()
        {
            _profileChoices = new List<EventButtonBehaviour>(); 
        }

        public void UpdateInfoBoxName()
        {
            _infoBoxText.text = _rebindHandler.ProfileName;
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

        public void UpdateProfileOptions()
        {
            if (_rebindHandler.ProfileOptions == null)
                return;

            for (int i = _profileChoices.Count - 1; i >= 0; i--)
            {
                Destroy(_profileChoices[i].gameObject);
                _profileChoices[i].transform.SetParent(null);
            }

            _profileChoices.Clear();

            foreach (string optionName in _rebindHandler.ProfileOptions)
            {
                if (optionName == null)
                    continue;

                EventButtonBehaviour buttonInstance = Instantiate(_profileButton, _profileOptions.transform);
                buttonInstance.GetComponentInChildren<Text>().text = optionName;

                buttonInstance.AddOnClickEvent(() =>
                {
                    _rebindHandler.LoadProfile(optionName);
                    _pageManager.GoToPageChild(1);
                    _videoPanel.SetActive(false);
                    _infoBoxText.text = optionName;
                });
                _profileChoices.Add(buttonInstance);
            }
        }
    }
}