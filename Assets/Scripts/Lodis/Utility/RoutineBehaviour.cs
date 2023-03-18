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

    public delegate void DelayedEvent(params object[] args);

    public class RoutineBehaviour : MonoBehaviour
    {
        private List<DelayedAction> _delayedActions = new List<DelayedAction>();

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
        /// <param name="delayedEvent">The action to do once the timer is complete</param>
        /// <param name="countType">The type of counter to create. Ex. Counting by frames, counting by scaled time, counting by unscaled time</param>
        /// <param name="duration">How long to wait before performing the action</param>
        /// <param name="args"></param>
        /// <returns></returns>
        public TimedAction StartNewTimedAction(DelayedEvent delayedEvent, TimedActionCountType countType, float duration, params object[] args)
        {
            TimedAction action = new TimedAction { CountType = countType, Duration = duration, Event = delayedEvent, args = args};

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
            action.Event += context => _delayedActions.Remove(action);
            _delayedActions.Add(action);
            return action;
        }

        /// <summary>
        /// Calls the given event after the given amount of time or frames have passed
        /// </summary>
        /// <param name="delayedEvent"></param>
        /// <param name="condition"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public ConditionAction StartNewConditionAction(DelayedEvent delayedEvent, Condition condition, params object[] args)
        {
            ConditionAction action = new ConditionAction { EventCheck = condition, Event = delayedEvent, args = args};
            
            action.Enable();
            _delayedActions.Add(action);
            action.Event += context => _delayedActions.Remove(action);
            return action;
        }

        /// <summary>
        /// Stops the given timed action; preventing the event from being called
        /// </summary>
        /// <param name="action">The timed action to stop</param>
        /// <returns>False if the action is not in the list of actions</returns>
        public bool StopAction(DelayedAction action)
        {
            if (action == null)
                return false;

            action.Disable();
            action.OnCancel?.Invoke();
            return _delayedActions.Remove(action);
        }

        // Update is called once per frame
        void Update()
        {
            _delayedActions.RemoveAll(action => !action.GetEnabled());

            //Iterate through all actions to try to invoke their events
            for (int i = 0; i < _delayedActions.Count; i++)
            {
                if (_delayedActions[i].GetEnabled())
                    _delayedActions[i].TryInvokeEvent();
            }
        }
    }
}
