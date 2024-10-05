
using Lodis;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Types;
using UnityGGPO;
using static FixedPoints.FixedAction;
using static FixedPoints.FixedTimeAction;

namespace FixedPoints
{
    public abstract class FixedAction
    {

        public delegate void FixedDelayedEvent();
        protected FixedDelayedEvent onDelayComplete;
        protected FixedDelayedEvent onDelayCancel;

        private bool isActive;

        public bool IsActive { get => isActive; set => isActive = value; }

        public abstract void TryPerformAction();

        public virtual void Serialize(BinaryWriter bw)
        {
            bw.Write(IsActive);
        }

        public virtual void Deserialize(BinaryReader br)
        {
            IsActive = br.ReadBoolean();
        }

        public virtual void Stop()
        {
            FixedPointTimer.Actions.Remove(this);
            IsActive = false;
        }
    }

    public class FixedTimeAction : FixedAction
    {
        public enum UnitOfTime
        {
            Scaled,
            Unscaled
        }

        protected Fixed32 timeStarted;
        protected Fixed32 duration;
        protected Fixed32 timeRemaining;
        protected UnitOfTime unit;
        protected bool shouldLoop;
        protected int loopCount = 1;
        protected Condition loopCondition;


        protected object[] eventArgs;
        private bool hasPaused;

        public FixedTimeAction(FixedDelayedEvent action, float duration, UnitOfTime unit = UnitOfTime.Scaled)
        {
            onDelayComplete = action;
            timeRemaining = duration;
            this.duration = duration;
            this.unit = unit;
        }

        /// <summary>
        /// The time that this action began. Value varies based on specified unit at start.
        /// </summary>
        public float TimeStarted { get => timeStarted; private set => timeStarted = value; }

        /// <summary>
        /// The amount of time this action has left. Value varies based on specified unit at start.
        /// </summary>
        public float Duration { get => duration; private set => duration = value; }

        /// <summary>
        /// The unit of time to use to measure the duration of this action.
        /// </summary>
        public UnitOfTime Unit { get => unit; private set => unit = value; }

        /// <summary>
        /// Gets the amount of time left before this action is performed. Value changes depending on the unit of time being used.
        /// </summary>
        /// <returns>The amount of time left. Returns -1 if the unit of time is not valid.</returns>
        public Fixed32 GetTimeLeft()
        {
            if (unit == UnitOfTime.Scaled)
            {
                return GridGame.Time * GridGame.TimeScale - timeStarted;
            }
            else if (unit == UnitOfTime.Unscaled)
            {
                return GridGame.Time - timeStarted;
            }

            return -1;
        }

        /// <summary>
        /// Will repeat this action with a delay.
        /// </summary>
        /// <param name="count">The amount of times to repeat the action. Set to -1 to repeat it infinitely.</param>
        public FixedTimeAction Loop(int count = -1)
        {
            shouldLoop = true;

            if (count > 0)
                loopCount += count;
            else if (count == -1) 
                loopCount = -1;

            return this;
        }

        /// <summary>
        /// Will repeat this action with a delay.
        /// </summary>
        /// <param name="condition">The loop will stop when this condition is true.</param>
        public FixedTimeAction LoopUntil(Condition condition)
        {
            shouldLoop = true;
            loopCondition = condition;
            loopCount = -1;

            return this;
        }

        /// <summary>
        /// Stops the coroutine for this action and stores the amount of time that was left over.
        /// </summary>
        public void Pause()
        {
            duration = GetTimeLeft();

            hasPaused = true;
        }

        /// <summary>
        /// Starts a new coroutine using the remaining time found when the action was paused.
        /// </summary>
        public void Resume()
        {
            hasPaused = false;
        }

        public override void Serialize(BinaryWriter bw)
        {
            base.Serialize(bw);
            bw.Write(timeRemaining);
            bw.Write(loopCount);
            bw.Write(hasPaused);
        }

        public override void Deserialize(BinaryReader br)
        {
            base.Deserialize(br);
            timeRemaining = br.ReadInt64();
            loopCount = br.ReadInt32();
            hasPaused = br.ReadBoolean();
        }

        public override void TryPerformAction()
        {
            //Loop while the count is valid or if the loop is infinite.
            if (loopCount <= 0 && loopCount != -1 || !IsActive || hasPaused)
            {
                return;
            }

            //If we should be looping and the condition to stop is true exit.
            if (shouldLoop && loopCondition?.Invoke() == true)
            {
                IsActive = false;
                return;
            }


            //Handle timer logic
            if (unit == UnitOfTime.Scaled)
            {
                timeRemaining -= GridGame.FixedTimeStep * GridGame.TimeScale;
            }
            else if (unit == UnitOfTime.Unscaled)
            {
                timeRemaining -= GridGame.FixedTimeStep;
            }

            if (timeRemaining <= 0)
            {
                //Try to stop looping if the loop isn't infinite.
                if (loopCount != -1)
                    loopCount--;

                onDelayComplete.Invoke();

                if (loopCount == 0)
                {
                    IsActive = false;
                    Stop();
                }
                else
                {
                    timeRemaining = duration;
                }
            }
        }
    }

    public class FixedConditionAction : FixedAction
    {
        private Condition _condition;

        public FixedConditionAction(FixedDelayedEvent action, Condition condition)
        {
            onDelayComplete = action;
            _condition = condition;
        }

        public override void TryPerformAction()
        {
            if (_condition?.Invoke() == true)
            {
                onDelayComplete?.Invoke();
                IsActive = false;
                FixedPointTimer.Actions.Remove(this);
            }
        }
    }

    public class FixedPointTimer
    {
        private static List<FixedAction> _actions = new List<FixedAction>();

        public static List<FixedAction> Actions { get => _actions; private set => _actions = value; }

        static FixedPointTimer()
        {
            GridGame.OnSerialization += SerializeActions;
            GridGame.OnDeserialization += DeserializeActions;
        }

        public static FixedTimeAction StartNewTimedAction(FixedDelayedEvent action, Fixed32 duration, UnitOfTime unit = UnitOfTime.Scaled)
        {
            FixedTimeAction newAction = new FixedTimeAction(action, duration, unit);
            _actions.Add(newAction);
            newAction.IsActive = true;
            return newAction;
        }

        public static FixedConditionAction StartNewConditionAction(FixedDelayedEvent action, Condition condition)
        {
            FixedConditionAction fixedConditionAction = new FixedConditionAction(action, condition);
            _actions.Add(fixedConditionAction);
            return fixedConditionAction;
        }

        public static void StopAction(FixedAction action)
        {
            if (action == null) return;

            _actions.Remove(action);
            action.IsActive = false;
        }

        public static void SerializeActions(BinaryWriter bw)
        {
            for (int i = 0; i < _actions.Count; i++)
            {
                _actions[i].Serialize(bw);
            }
        }

        public static void DeserializeActions(BinaryReader br)
        {
            for (int i = 0; i < _actions.Count; i++)
            {
                _actions[i].Deserialize(br);
            }
        }
    }
}