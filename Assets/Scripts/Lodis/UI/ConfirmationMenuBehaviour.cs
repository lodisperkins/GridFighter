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
        [SerializeField] private EventButtonBehaviour _yesButton;
        [SerializeField] private EventButtonBehaviour _noButton;
        [SerializeField] private Text _promptText;
        [SerializeField] private Animator _animator;

        //---
        private GameObject _selectionOnClose;
        private GameObject _selectionOnYes;
        private GameObject _selectionOnNo;
        private bool _initialized;
        private EventSystem _eventSystem;


        public void Init(UnityAction yes, UnityAction no, EventSystem eventSystem, string prompt, GameObject selectionOnClose, string leftText = "Yes", string rightText = "No")
        {
            _promptText.text = prompt;

            if (yes != null)
                _yesButton.AddOnClickEvent(yes);

            _yesButton.SetText(leftText);
            _yesButton.AddOnClickEvent(Close);

            _noButton.gameObject.SetActive(no != null);

            if (no != null)
            {
                _noButton.AddOnClickEvent(no);
                _noButton.AddOnClickEvent(Close);
                _noButton.SetText(rightText);
            }

            _eventSystem = eventSystem;

            _eventSystem.SetSelectedGameObject(_yesButton.gameObject);
            _yesButton.OnSelect();

            _selectionOnClose = selectionOnClose;
            _initialized = true;
        }

        public void Init(UnityAction yes, UnityAction no, EventSystem eventSystem, string prompt, GameObject selectionOnYes, GameObject selectionOnNo, string leftText = "Yes", string rightText = "No")
        {
            _promptText.text = prompt;

            if (yes != null)
                _yesButton.AddOnClickEvent(yes);

            _yesButton.SetText(leftText);
            _yesButton.AddOnClickEvent(Close);
            _yesButton.AddOnClickEvent(SelectYesOption);

            _noButton.gameObject.SetActive(no != null);

            if (no != null)
            {
                _noButton.AddOnClickEvent(no);
                _noButton.AddOnClickEvent(Close);
                _noButton.SetText(rightText);
                _noButton.AddOnClickEvent(SelectNoOption);
            }

            _eventSystem = eventSystem;

            _eventSystem.SetSelectedGameObject(_yesButton.gameObject);
            _yesButton.OnSelect();

            _selectionOnYes = selectionOnYes;
            _selectionOnNo = selectionOnNo;
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

        public void SelectYesOption()
        {
            if (_selectionOnYes)
                _eventSystem.SetSelectedGameObject(_selectionOnYes);
        }

        public void SelectNoOption()
        {
            if (_selectionOnNo)
                _eventSystem.SetSelectedGameObject(_selectionOnNo);
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
