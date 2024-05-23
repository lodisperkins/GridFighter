using UnityEngine;
using UnityEngine.Events;

namespace Lodis.Utility
{
    public class TimedAction : DelayedAction
    {
        public float TimeStarted;
        public float Duration;
        public TimedActionCountType CountType;
        private float _timeLeft;
        private bool _isPaused;

        public TimedAction() { }

        public bool IsPaused { get => _isPaused; }
        public float TimeLeft { get => _timeLeft; private set => _timeLeft = value; }

        public void Pause()
        {
            _isPaused = true;
        }

        public void Unpause()
        {
            _isPaused = false;

            //Call event based on the type of counter
            switch (CountType)
            {
                case TimedActionCountType.SCALEDTIME:
                    TimeStarted = Time.time;
                    break;
                case TimedActionCountType.UNSCALEDTIME:
                    TimeStarted = Time.unscaledTime;
                    break;
                case TimedActionCountType.FRAME:
                    TimeStarted = Time.frameCount;
                    break;
                case TimedActionCountType.CHARACTERSCALEDTIME:
                    TimeStarted = RoutineBehaviour.Instance.CharacterTime;
                    break;
            }

            Duration = TimeLeft;
        }
            
        public override bool TryInvokeEvent()
        {
            if (_isPaused)
                return false;

            float time = 0;
            
            //Call event based on the type of counter
            switch (CountType)
            {
                case TimedActionCountType.SCALEDTIME:
                    time = Time.time;
                    break;
                case TimedActionCountType.UNSCALEDTIME:
                    time = Time.unscaledTime;
                    break;
                case TimedActionCountType.FRAME:
                    time = Time.frameCount;
                    break;
                case TimedActionCountType.CHARACTERSCALEDTIME:
                    time = RoutineBehaviour.Instance.CharacterTime;
                    break;
            }

            if (time - TimeStarted >= Duration && GetEnabled())
            {
                Disable();
                Event.Invoke();
                return true;
            }

            TimeLeft = Duration - (time - TimeStarted);
            return false;
        }
            
    }
}

