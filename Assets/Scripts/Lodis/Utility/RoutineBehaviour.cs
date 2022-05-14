using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Dynamic;
using System;

namespace Lodis.Utility
{
    public enum TimedActionCountType
    {
        SCALEDTIME,
        UNSCALEDTIME,
        FRAME
    }

    public class RoutineBehaviour : MonoBehaviour
    {
        public delegate void TimedEvent(params object[] args);

        

        public class TimedAction
        {
            public float TimeStarted;
            public float Duration;
            public TimedActionCountType CountType;
            public TimedEvent Event;
            private bool _isActive;

            public TimedAction() { }

            public bool GetEnabled() { return _isActive; }
            public void Enable() { _isActive = true; }
            public void Disable() { _isActive = false; }
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
            action.Enable();
            _timedActions.Add(action);
            return action;
        }

        /// <summary>
        /// Stops the given timed action; preventing the event from being called
        /// </summary>
        /// <param name="action">The timed action to stop</param>
        /// <returns>False if the action is not in the list of actions</returns>
        public bool StopTimedAction(TimedAction action)
        {
            if (action == null)
                return false;

            action.Disable();
            return _timedActions.Remove(action);
        }

        private void TryInvokeTimedEvent(float time, int index)
        {
            TimedAction action = _timedActions[index];

            if (time - action.TimeStarted >= action.Duration && action.GetEnabled())
            {
                action.Disable();
                action.Event.Invoke();
                _timedActions.Remove(action);
            }
        }

        // Update is called once per frame
        void Update()
        {
            //Iterate through all actions to try to invok their events
            for (int i = 0; i < _timedActions.Count; i++)
            {
                TimedAction currentAction = _timedActions[i];

                //Call event based on the type of counter
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
