using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lodis.ScriptableObjects;
using UnityEngine.UI;

namespace Lodis.UI
{
    public class MatchTimerBehaviour : MonoBehaviour
    {
        private static MatchTimerBehaviour _instance;
        [SerializeField]
        private Text _timerText;
        [SerializeField]
        private FloatVariable _matchTime;
        [SerializeField]
        private float _matchTimeRemaining;
        [SerializeField]
        private float _timeSinceRoundStart;
        [SerializeField]
        private GridGame.Event _onTimerUp;
        private bool _timeUp;
        private bool _isInfinite;
        private bool _isActive;
        private bool _eventRaised;


        public static MatchTimerBehaviour Instance
        {
            get
            {
                if (!_instance)
                    _instance = FindObjectOfType(typeof(MatchTimerBehaviour)) as MatchTimerBehaviour;

                if (!_instance)
                {
                    GameObject manager = new GameObject("MatchTimer");
                    _instance = manager.AddComponent<MatchTimerBehaviour>();
                }

                return _instance;
            }
        }

        public bool TimeUp
        {
            get { return _timeUp; }
        }

        public bool IsInfinite { get => _isInfinite; set => _isInfinite = value; }
        public bool IsActive { get => _isActive; set => _isActive = value; }
        public float MatchTimeRemaining { get => _matchTimeRemaining; private set => _matchTimeRemaining = value; }
        public float TimeSinceRoundStart { get => _timeSinceRoundStart; private set => _timeSinceRoundStart = value; }

        // Start is called before the first frame update
        void Start()
        {
            Gameplay.GameManagerBehaviour.Instance.AddOnMatchStartAction(() => IsActive = true);
            Gameplay.GameManagerBehaviour.Instance.AddOnMatchRestartAction(ResetTimer);
            Gameplay.GameManagerBehaviour.Instance.AddOnMatchOverAction(() => IsActive = false);
            MatchTimeRemaining = _matchTime.Value;
        }

        public void ResetTimer()
        {
            MatchTimeRemaining = _matchTime.Value;
            _eventRaised = false;
            _timeUp = false;
            TimeSinceRoundStart = 0;
        }

        // Update is called once per frame
        void Update()
        {
            TimeSinceRoundStart += Time.deltaTime;
            if (!IsActive)
                return;

            string timeText = "";
            if (!IsInfinite)
            {
                MatchTimeRemaining -= Time.deltaTime;
                _timeUp = MatchTimeRemaining <= 0;
                timeText = Mathf.Ceil(MatchTimeRemaining).ToString();
            }
            else
            {
                MatchTimeRemaining = float.PositiveInfinity;
                _timeUp = false;
                timeText = MatchTimeRemaining.ToString();
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
