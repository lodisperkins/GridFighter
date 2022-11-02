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
        private static bool _isActive = true;
        private bool _eventRaised;

        public static bool TimeUp
        {
            get { return _timeUp; }
        }

        public static bool IsInfinite { get => _isInfinite; set => _isInfinite = value; }
        public static bool IsActive { get => _isActive; set => _isActive = value; }

        // Start is called before the first frame update
        void Start()
        {
            Gameplay.GameManagerBehaviour.Instance.AddOnMatchRestartAction(ResetTimer);
            Gameplay.GameManagerBehaviour.Instance.AddOnMatchOverAction(() => IsActive = false);
            _matchTimeRemaining = _matchTime.Value;
        }

        public void ResetTimer()
        {
            _matchTimeRemaining = _matchTime.Value;
            _eventRaised = false;
            _timeUp = false;
            IsActive = true;
        }

        // Update is called once per frame
        void Update()
        {
            if (!IsActive)
                return;

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

            if (_timeUp && !_eventRaised)
            {
                _onTimerUp.Raise(gameObject);
                IsActive = false;
            }

            _timerText.text = timeText;
        }
    }
}
