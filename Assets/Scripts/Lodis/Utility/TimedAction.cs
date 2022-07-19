using UnityEngine;

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
            }

            Duration = _timeLeft;
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
            }

            if (time - TimeStarted >= Duration && GetEnabled())
            {
                Disable();
                Event.Invoke();
                return true;
            }

            _timeLeft = Duration - (time - TimeStarted);
            return false;
        }
            
    }
}

