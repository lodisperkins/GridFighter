using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lodis.ScriptableObjects;
using UnityEngine.UI;

namespace Lodis.UI
{
    public class MatchTimerBehaviour : MonoBehaviour
    {
        [SerializeField]
        private Text _timerText;
        [SerializeField]
        private FloatVariable _matchTime;
        [SerializeField]
        private float _matchTimeRemaining;
        [SerializeField]
        private GridGame.Event _onTimerUp;
        private static bool _timeUp;
        private static bool _isInfinite;
        private bool _eventRaised;

        public static bool TimeUp
        {
            get { return _timeUp; }
        }

        public static bool IsInfinite { get => _isInfinite; set => _isInfinite = value; }

        // Start is called before the first frame update
        void Start()
        {
            Gameplay.GameManagerBehaviour.Instance.AddOnMatchRestartAction(() => { _matchTimeRemaining = _matchTime.Value; _eventRaised = false; });
            _matchTimeRemaining = _matchTime.Value;
        }

        // Update is called once per frame
        void Update()
        {
            string timeText = "";
            if (!IsInfinite)
            {
                _matchTimeRemaining -= Time.deltaTime;
                _timeUp = _matchTimeRemaining <= 0;
                timeText = Mathf.Ceil(_matchTimeRemaining).ToString();
            }
            else
            {
                _matchTimeRemaining = float.PositiveInfinity;
                _timeUp = false;
                timeText = _matchTimeRemaining.ToString();
            }

            if (_timeUp)
            {
                if (!_eventRaised)
                    _onTimerUp.Raise(gameObject);

                return;
            }

            

            _timerText.text = timeText;
        }
    }
}
