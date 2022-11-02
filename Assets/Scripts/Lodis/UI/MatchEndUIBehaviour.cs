using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Lodis.Gameplay;
using Lodis.Utility;

namespace Lodis.UI
{
    public class MatchEndUIBehaviour : MonoBehaviour
    {
        [SerializeField]
        private Text _endText;
        [SerializeField]
        private GameObject _gameMenu;
        [SerializeField]
        private float _endTextDisplayDuration;

        public void DisplayEndText()
        {
            _endText.gameObject.SetActive(true);

            switch (GameManagerBehaviour.Instance.LastMatchResult)
            {
                case MatchResult.DRAW:
                    _endText.text = "Draw!";
                    break;
                case MatchResult.P1WINS:
                    _endText.text = "Player 1 Wins!";
                    RoutineBehaviour.Instance.StartNewTimedAction(args =>
                    {
                        _endText.gameObject.SetActive(false);
                        _gameMenu.SetActive(true);
                    }, TimedActionCountType.SCALEDTIME, _endTextDisplayDuration);
                    break;
                case MatchResult.P2WINS:
                    _endText.text = "Player 2 Wins!";
                    RoutineBehaviour.Instance.StartNewTimedAction(args =>
                    {
                        _endText.gameObject.SetActive(false);
                        _gameMenu.SetActive(true);
                    }, TimedActionCountType.SCALEDTIME, _endTextDisplayDuration);
                    break;
            }
        }
    }
}