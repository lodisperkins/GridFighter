using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.AccessControl;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GridGame.GamePlay
{
    public class MenuBehaviour : MonoBehaviour
    {
        [SerializeField] private GameObject _controlsPanel;
        [SerializeField] private Event onSelected;
        [SerializeField] private Event onCancel;
        [SerializeField] private Event onHighlighted;
        [SerializeField] private List<Image> _displayOptions;
        [SerializeField] private string _horizontalAxis;
        [SerializeField] private string _submitAxis;
        [SerializeField] private string _cancelAxis;
        private int _currentIndex;
        private bool _canPressButton;
        private bool _controlWindowUp;
        public bool gameWon;
        private void Start()
        {
            _controlWindowUp = false;
        }

        public void GoToNextOption()
        {
            _displayOptions[_currentIndex].color = Color.white;
            _currentIndex++;
            if (_currentIndex > _displayOptions.Count - 1)
            {
                _currentIndex = 0;
            }
            _displayOptions[_currentIndex].color = Color.yellow;
            onHighlighted.Raise(_displayOptions[_currentIndex].gameObject);
        }
        public void GoToPreviousOption()
        {
            _displayOptions[_currentIndex].color = Color.white;
            _currentIndex--;
            if (_currentIndex < 0)
            {
                _currentIndex = _displayOptions.Count - 1;
            }
            _displayOptions[_currentIndex].color = Color.yellow;
            onHighlighted.Raise(_displayOptions[_currentIndex].gameObject);
        }

        public void ToggleControlsMenu()
        {
                _controlsPanel.SetActive(!_controlsPanel.activeSelf);
                _controlWindowUp = !_controlWindowUp;
                _canPressButton = false;
        }
        public void Quit()
        {
                Application.Quit();
        }

        public void DoCurrentAction()
        {
             onSelected.Raise(_displayOptions[_currentIndex].gameObject);
        }

        private void Update()
        {

            if ((Input.GetAxis(_horizontalAxis) < .5 && Input.GetAxis(_horizontalAxis) > -.5))
            {
                if (_controlWindowUp == false)
                {
                    _canPressButton = true;
                }
            }

            if (Input.GetAxis(_horizontalAxis) <= -.8 && _canPressButton)
            {
                _canPressButton = false;
                GoToPreviousOption();
            }
            else if (Input.GetAxisRaw(_horizontalAxis) >= .8 && _canPressButton)
            {
                _canPressButton = false;
                GoToNextOption();
            }
            if ((Input.GetAxis(_horizontalAxis) < .5 && Input.GetAxis(_horizontalAxis) > -.5))
            {
                if (_controlWindowUp == false)
                {
                    _canPressButton = true;
                }
            }

            if (Input.GetAxis(_horizontalAxis) <= -.8 && _canPressButton)
            {
                _canPressButton = false;
                GoToPreviousOption();
            }
            else if (Input.GetAxisRaw(_horizontalAxis) >= .8 && _canPressButton)
            {
                _canPressButton = false;
                GoToNextOption();
            }
            if (Input.GetButtonDown(_submitAxis))
            {
                DoCurrentAction();
            }
            if(Input.GetButtonDown(_cancelAxis))
            {
                onCancel.Raise(gameObject);
            }
        }
    }

}

