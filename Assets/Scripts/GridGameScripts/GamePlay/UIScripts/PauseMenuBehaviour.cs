using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.AccessControl;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GridGame.GamePlay
{
	public class PauseMenuBehaviour : MonoBehaviour
	{
		//raised when the game is paused
		[SerializeField]
		private Event OnPause;
		//raised when the game is unpaused
		[SerializeField]
		private Event OnUnPause;
		//true if the game is poused
		public bool isPaused;
		[SerializeField] private GameObject _controlsPanel;
		[SerializeField] private List<Event> _actions;
		[SerializeField] private List<Text> _displayOptions;
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
			if (isPaused || gameWon)
			{
				_displayOptions[_currentIndex].color = Color.cyan;
				_currentIndex++;
				if (_currentIndex > _displayOptions.Count -1)
				{
					_currentIndex = 0;
				}
				_displayOptions[_currentIndex].color = Color.white;
			}
		}

		public void ToggleGameWon()
		{
			gameWon = true;
		}
		public void GoToPreviousOption()
		{
			if (isPaused || gameWon)
			{
				_displayOptions[_currentIndex].color = Color.cyan;
				_currentIndex--;
				if (_currentIndex < 0)
				{
					_currentIndex = _displayOptions.Count -1;
				}
				_displayOptions[_currentIndex].color = Color.white;
			}
		}

		public void PauseGame()
		{
			if (Time.timeScale == 1)
			{
				Time.timeScale = 0;
				isPaused = true;
				OnPause.Raise(gameObject);
			}
			else
			{
				Time.timeScale = 1;
				isPaused = false;
				OnUnPause.Raise(gameObject);
			}
		}

		public void ToggleControlsMenu()
		{
			if (isPaused)
			{
				_controlsPanel.SetActive(!_controlsPanel.activeSelf);
				_controlWindowUp = !_controlWindowUp;
				_canPressButton = false;
			}
		}
		public void Restart()
		{
			if (isPaused || gameWon)
			{
				Time.timeScale = 1;
				isPaused = false;
				OnUnPause.Raise(gameObject);
				SceneManager.LoadScene("BattleScene");
			}
		}
        public void ReturnToBlockSelect()
        {
            Time.timeScale = 1;
            isPaused = false;
            OnUnPause.Raise(gameObject);
            SceneManager.LoadScene("TowerSelectScene");
        }
		public void Quit()
		{
			if (isPaused || gameWon)
			{
				Application.Quit();
			}
		}

		public void DoCurrentAction()
		{
			if (isPaused || gameWon)
			{
				_actions[_currentIndex].Raise(gameObject);
			}
		}

		private void Update()
		{
			if (Input.GetButtonDown("Pause"))
			{
				PauseGame();
			}

			if ((Input.GetAxis("Vertical1") < .5 && Input.GetAxis("Vertical1") > -.5) && Input.GetAxis("Vertical2") > -.5&&(Input.GetAxis("Vertical2") < .5 ))
			{
				if (_controlWindowUp == false) 
				{
					_canPressButton = true;
				}
			}
			
			if (Input.GetAxis("Vertical1") >= .8 && _canPressButton)
			{
				_canPressButton = false;
				GoToPreviousOption();
			}
			else if (Input.GetAxisRaw("Vertical1") <= -.8 && _canPressButton)
			{
				_canPressButton = false;
				GoToNextOption();
			}
			if (Input.GetAxis("Vertical2") >= .8 && _canPressButton)
			{
				_canPressButton = false;
				GoToPreviousOption();
			}
			else if (Input.GetAxisRaw("Vertical2") <= -.8 && _canPressButton)
			{
				_canPressButton = false;
				GoToNextOption();
			}
			if (Input.GetButtonDown("Submit") || Input.GetButtonDown("Submit2"))
			{
				DoCurrentAction();
			}
		}
	}

}

