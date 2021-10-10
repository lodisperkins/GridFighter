using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Dynamic;

namespace Lodis.Utility
{
    public class RoutineBehaviour : MonoBehaviour
    {
        public delegate void TimedEvent(params object[] args);

        public enum TimedActionCountType
        {
            SCALEDTIME,
            UNSCALEDTIME,
            FRAME
        }

        public struct TimedAction
        {
            public float TimeStarted;
            public float Duration;
            public TimedActionCountType CountType;
            public TimedEvent Event;
        }

        private List<TimedAction> _timedActions = new List<TimedAction>();

        private static RoutineBehaviour _instance;

        public static RoutineBehaviour Instance
        {
            get
            {
                if (!_instance)
                    _instance = FindObjectOfType(typeof(RoutineBehaviour)) as RoutineBehaviour;

                if (!_instance)
                {
                    GameObject timer = new GameObject("TimerObject");
                    _instance = timer.AddComponent<RoutineBehaviour>();
                }

                return _instance;
            }
        }

        /// <summary>
        /// Calls the given event after the given amount of time or frames have passed
        /// </summary>
        /// <param name="timedEvent">The action to do once the timer is complete</param>
        /// <param name="countType">The type of counter to create. Ex. Counting by frames, counting by scaled time, counting by unscaled time</param>
        /// <param name="duration">How long to wait before performing the action</param>
        /// <returns></returns>
        public TimedAction StartNewTimedAction(TimedEvent timedEvent, TimedActionCountType countType, float duration)
        {
            TimedAction action = new TimedAction { CountType = countType, Duration = duration, Event = timedEvent };

            switch (countType)
            {
                case TimedActionCountType.SCALEDTIME:
                    action.TimeStarted = Time.time;
                    break;
                case TimedActionCountType.UNSCALEDTIME:
                    action.TimeStarted = Time.unscaledTime;
                    break;
                case TimedActionCountType.FRAME:
                    action.TimeStarted = Time.frameCount;
                    break;
            }    

            _timedActions.Add(action);
            return action;
        }

        public bool StopTimedAction(TimedAction action)
        {
            return _timedActions.Remove(action);
        }

        private void TryInvokeTimedEvent(float time, int index)
        {
            if (time - _timedActions[index].TimeStarted >= _timedActions[index].Duration)
            {
                _timedActions[index].Event.Invoke();
                _timedActions.RemoveAt(index);
            }
        }

        // Update is called once per frame
        void Update()
        {
            for (int i = 0; i < _timedActions.Count; i++)
            {
                TimedAction currentAction = _timedActions[i];

                switch (currentAction.CountType)
                {
                    case TimedActionCountType.SCALEDTIME:
                        TryInvokeTimedEvent(Time.time, i);
                        break;
                    case TimedActionCountType.UNSCALEDTIME:
                        TryInvokeTimedEvent(Time.unscaledTime, i);
                        break;
                    case TimedActionCountType.FRAME:
                        TryInvokeTimedEvent(Time.frameCount, i);
                        break;
                }
            }
        }
    }
}
