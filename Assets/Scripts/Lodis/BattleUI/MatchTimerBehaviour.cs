using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lodis.ScriptableObjects;
using UnityEngine.UI;
using Lodis.Utility;
using System.IO;
using Types;
using NaughtyAttributes;

namespace Lodis.UI
{
    public class MatchTimerBehaviour : SimulationBehaviour
    {
        private static MatchTimerBehaviour _instance;
        [SerializeField]
        private Text _timerText;
        [SerializeField]
        private FloatVariable _matchTime;
        [SerializeField]
        private Fixed32 _matchTimeRemaining;
        [SerializeField]
        private Fixed32 _timeSinceRoundStart;
        [SerializeField]
        private CustomEventSystem.Event _onTimerUp;
        [SerializeField]
        private bool _isInfinite;
        private bool _timeUp;
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
        public Fixed32 MatchTimeRemaining { get => _matchTimeRemaining; private set => _matchTimeRemaining = value; }
        public Fixed32 TimeSinceRoundStart { get => _timeSinceRoundStart; private set => _timeSinceRoundStart = value; }

        public override string LogName => "MatchTimerBehaviour";

        public override void Serialize(BinaryWriter bw)
        {
            bw.Write(_isInfinite);
            bw.Write(_isActive);
            bw.Write(_timeUp);
            bw.Write(_eventRaised);
            _matchTimeRemaining.Serialize(bw);
            _timeSinceRoundStart.Serialize(bw);
        }

        public override void Deserialize(BinaryReader br)
        {
            _isInfinite = br.ReadBoolean();
            _isActive = br.ReadBoolean();
            _timeUp = br.ReadBoolean();
            _eventRaised = br.ReadBoolean();
            _matchTimeRemaining = _matchTimeRemaining.Deserialize(br);
            _timeSinceRoundStart = _timeSinceRoundStart.Deserialize(br);
        }

        /// <summary>
        /// Hashes the serialized timer state so a match-clock divergence can be tied
        /// back to this component immediately.
        /// </summary>
        protected override string[] GetLogItems()
        {
            return new string[]
            {
                $"IsInfinite={_isInfinite}",
                $"IsActive={_isActive}",
                $"TimeUp={_timeUp}",
                $"EventRaised={_eventRaised}",
                $"MatchTimeRemaining={_matchTimeRemaining}",
                $"TimeSinceRoundStart={_timeSinceRoundStart}"
            };
        }

        // Start is called before the first frame update
        public override void Begin()
        {
            Gameplay.MatchManagerBehaviour.Instance.AddOnMatchStartAction(() => IsActive = true);
            Gameplay.MatchManagerBehaviour.Instance.AddOnMatchRestartAction(ResetTimer);
            Gameplay.MatchManagerBehaviour.Instance.AddOnMatchOverAction(() => IsActive = false);
            MatchTimeRemaining = _matchTime.FixedValue;
        }

        public void ResetTimer()
        {
            if (SceneManagerBehaviour.Instance.CurrentGameMode == (int)GameMode.TUTORIAL || SceneManagerBehaviour.Instance.CurrentGameMode == (int)GameMode.PRACTICE)
                return;

            if (!IsInfinite)
                MatchTimeRemaining = _matchTime.FixedValue;
            else
                MatchTimeRemaining = float.PositiveInfinity;

            _eventRaised = false;
            _timeUp = false;
            TimeSinceRoundStart = 0;

            string timeText = "";
            int minutes = Fixed32.FloorToInt(MatchTimeRemaining / 60f);
            int seconds = Fixed32.FloorToInt(MatchTimeRemaining - minutes * 60f);

            string formattedTime = string.Format("{0:0}:{1:00}", minutes, seconds);

            timeText = formattedTime;
            _timerText.text = timeText;
        }

        [Button]
        private void SkipTimeForDebug()
        {
            MatchTimeRemaining = 1;
        }

        // Update is called once per frame
        public override void Tick(Fixed32 dt)
        {
            TimeSinceRoundStart += dt;
            if (!IsActive)
                return;

            string timeText = "";
            if (!IsInfinite)
            {
                MatchTimeRemaining -= dt;
                _timeUp = MatchTimeRemaining <= 0;

                int minutes = Fixed32.FloorToInt(MatchTimeRemaining / 60f);
                int seconds = Fixed32.FloorToInt(MatchTimeRemaining - minutes * 60f);

                string formattedTime = string.Format("{0:0}:{1:00}", minutes, seconds);

                timeText = formattedTime;
            }
            else
            {
                MatchTimeRemaining = float.PositiveInfinity;
                _timeUp = false;

                timeText = "Infinite";
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
