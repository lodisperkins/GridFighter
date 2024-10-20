﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Lodis.Gameplay;
using Lodis.Utility;
using UnityEngine.EventSystems;

namespace Lodis.UI
{
    public class MatchEndUIBehaviour : MonoBehaviour
    {
        [SerializeField]
        private Text _endText;
        [SerializeField]
        private GameObject _gameMenu;
        [SerializeField]
        private AudioSource _announcer;
        [SerializeField]
        private AudioClip _tieClip;
        [SerializeField]
        private AudioClip _winnerClip;
        [SerializeField]
        private float _endTextDisplayDuration;
        [SerializeField]
        private UnityEngine.EventSystems.EventSystem _eventSystem;
        [SerializeField]
        private Button _firstGameMenuButton;

        public void DisplayEndText()
        {
            _endText.gameObject.SetActive(true);

            switch (MatchManagerBehaviour.Instance.LastMatchResult)
            {
                case MatchResult.DRAW:
                    _endText.text = "Draw!";
                    _announcer.Stop();
                    _announcer.PlayOneShot(_tieClip);
                    break;
                case MatchResult.P1WINS:
                    _endText.text = "Player 1 Wins!";
                    RoutineBehaviour.Instance.StartNewTimedAction(args =>
                    {
                        _endText.gameObject.SetActive(false);
                        _gameMenu.SetActive(true);
                        _eventSystem.SetSelectedGameObject(_firstGameMenuButton.gameObject);
                        _firstGameMenuButton.OnSelect(null);
                    }, TimedActionCountType.SCALEDTIME, _endTextDisplayDuration);
                    _announcer.Stop();
                    _announcer.PlayOneShot(_winnerClip);
                    break;
                case MatchResult.P2WINS:
                    _endText.text = "Player 2 Wins!";
                    _announcer.Stop();
                    _announcer.PlayOneShot(_winnerClip);
                    RoutineBehaviour.Instance.StartNewTimedAction(args =>
                    {
                        _endText.gameObject.SetActive(false);
                        _gameMenu.SetActive(true);
                        _eventSystem.SetSelectedGameObject(_firstGameMenuButton.gameObject);
                        _firstGameMenuButton.OnSelect(null);
                    }, TimedActionCountType.SCALEDTIME, _endTextDisplayDuration);
                    break;
            }
        }
    }
}