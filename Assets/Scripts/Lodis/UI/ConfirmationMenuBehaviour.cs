using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;


namespace Lodis.UI
{
    internal class ConfirmationMenuBehaviour : MonoBehaviour
    {
        [SerializeField]
        private EventButtonBehaviour _yesButton;
        [SerializeField]
        private EventButtonBehaviour _noButton;
        [SerializeField]
        private Text _promptText;
        [SerializeField]
        private Animator _animator;
        private GameObject _selectionOnClose;
        private bool _initialized;
        private EventSystem _eventSystem;


        public void Init(UnityAction yes, UnityAction no, EventSystem eventSystem, string prompt, GameObject selectionOnClose)
        {
            _promptText.text = prompt;

            _yesButton.AddOnClickEvent(yes);
            _noButton.AddOnClickEvent(no);

            _yesButton.AddOnClickEvent(Close);
            _noButton.AddOnClickEvent(Close);

            _eventSystem = eventSystem;

            _eventSystem.SetSelectedGameObject(_yesButton.gameObject);
            _yesButton.OnSelect();

            _selectionOnClose = selectionOnClose;
            _initialized = true;
        }

        public void Open()
        {
            if (!_initialized)
            {
                Debug.LogError("Confirmation menu is not initialized. Call the Init function before using.");
                return;
            }

            _animator.Play("Open");
        }

        public void Close()
        {
            _animator.Play("Close");

            if (_selectionOnClose)
            {
                _eventSystem.SetSelectedGameObject(_selectionOnClose);
                //_eventSystem.UpdateModules();
            }

            _yesButton.ClearOnClickEvent(); 
            _noButton.ClearOnClickEvent();
        }
    }
}
