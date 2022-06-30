using UnityEngine;

namespace Lodis.Utility
{
    public class TimedAction : DelayedAction
    {
        public float TimeStarted;
        public float Duration;
        public TimedActionCountType CountType;
        
        public TimedAction() { }
            
        public override bool TryInvokeEvent()
        {
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

            return false;
        }
            
    }
}

