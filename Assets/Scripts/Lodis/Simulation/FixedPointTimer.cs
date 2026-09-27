
using Assets.Scripts.Lodis.Simulation;
using Lodis;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Types;
using UnityGGPO;
using static FixedPoints.FixedAction;
using static FixedPoints.FixedTimeAction;

namespace FixedPoints
{
    public abstract class FixedAction : ISerializedListObject
    {

        public delegate void FixedDelayedEvent();
        protected FixedDelayedEvent onDelayComplete;
        protected FixedDelayedEvent onDelayCancel;

        private bool isActive;
        public int FrameStarted;
        public int FrameFinished;
        public EntityData Target;
        public int ActionID;
        private string _displayName;

        public bool IsActive 
        {
            get => isActive;
            set
            {
                isActive = value;
            }
        }
        public ListEvent OnAddedToList { get; set; }
        public int FrameAddedToSerializedList { get; set; }
        public int SerializedChecksum { get; set; }
        public ListEvent OnRemovedFromList { get; set; }

        /// <summary>
        /// The optional debug name shown for this action in serialized-list logs.
        /// Defaults to FixedAction when the action was started without a name.
        /// </summary>
        public string ListDisplayName => string.IsNullOrEmpty(_displayName) ? "FixedAction" : _displayName;

        public int FrameRemovedFromActiveList { get; set; }

        public abstract void TryPerformAction();

        protected abstract void Serialize(BinaryWriter bw);

        protected abstract void Deserialize(Deserializer br);

        protected abstract void LogGameState(StringBuilder sb);

        public virtual void Stop()
        {
            FixedPointTimer.StopAction(this);
            IsActive = false;
        }

        public virtual void Init()
        {
            FixedPointTimer.Actions.Add(this);
            IsActive = true;
        }

        internal void SetActionID(int debugId)
        {
            ActionID = debugId;
        }

        /// <summary>
        /// Sets the optional name used by rollback debug logs for this action.
        /// </summary>
        internal void SetDisplayName(string displayName)
        {
            _displayName = displayName;
        }

        #region Simulation Functions
        public void OnSerialize(BinaryWriter bw)
        {
            bw.Write(ActionID);
            bw.Write(IsActive);
            Serialize(bw);
        }

        public void OnDeserialize(Deserializer br)
        {
            ActionID = br.ReadInt32();
            IsActive = br.ReadBoolean();
            Deserialize(br);
        }

        public void OnLogGameState(StringBuilder sb)
        {
            sb.AppendLine($"            {ListDisplayName}");
            sb.AppendLine($"                  DebugId={ActionID}");
            sb.AppendLine($"                  IsActive={IsActive}");
            LogGameState(sb);
        }

        /// <summary>
        /// Hashes this fixed action's rollback payload for debug game-state logging.
        /// </summary>
        public int CalculateChecksum()
        {
            using (MemoryStream memoryStream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(memoryStream))
            {
                WriteChecksumPayload(writer);
                return CalcFletcher32(memoryStream.ToArray());
            }
        }
        #endregion

        /// <summary>
        /// Writes the rollback payload used to compare this fixed action's state in
        /// the debug checksum log.
        /// </summary>
        public virtual void WriteChecksumPayload(BinaryWriter bw)
        {
            OnSerialize(bw);
        }

        /// <summary>
        /// Produces a lightweight deterministic fingerprint of serialized timer data.
        /// </summary>
        private static int CalcFletcher32(byte[] data)
        {
            uint sum1 = 0;
            uint sum2 = 0;

            for (int i = 0; i < data.Length; ++i)
            {
                sum1 = (sum1 + data[i]) % 0xffff;
                sum2 = (sum2 + sum1) % 0xffff;
            }

            return unchecked((int)((sum2 << 16) | sum1));
        }
    }

    public class FixedTimeAction : FixedAction
    {
        public enum UnitOfTime
        {
            Scaled,
            Unscaled,
            PauseScaled
        }

        protected Fixed32 timeStarted;
        protected Fixed32 duration;
        protected Fixed32 timeRemaining;
        protected UnitOfTime unit;
        protected bool shouldLoop;
        protected int loopCount = 1;
        protected int startingLoopCount;
        protected Condition loopCondition;

        public delegate void FixedTimeActionEvent();
        /// <summary>
        /// Called when all loops have finished.
        /// </summary>
        public event FixedTimeActionEvent OnComplete;
        protected object[] eventArgs;
        private bool hasPaused;

        public FixedTimeAction(FixedDelayedEvent action, Fixed32 duration, Fixed32 timeBegan, UnitOfTime unit = UnitOfTime.Scaled)
        {
            Configure(action, duration, timeBegan, unit);
        }

        /// <summary>
        /// The time that this action began. Value varies based on specified unit at start.
        /// </summary>
        public Fixed32 TimeStarted { get => timeStarted; private set => timeStarted = value; }

        /// <summary>
        /// The amount of time this action has left. Value varies based on specified unit at start.
        /// </summary>
        public Fixed32 Duration { get => duration;  set => duration = value; }

        /// <summary>
        /// The unit of time to use to measure the duration of this action.
        /// </summary>
        public UnitOfTime Unit { get => unit; set => unit = value; }

        /// <summary>
        /// Gets the amount of time left before this action is performed. Value changes depending on the unit of time being used.
        /// </summary>
        /// <returns>The amount of time left. Returns -1 if the unit of time is not valid.</returns>
        public Fixed32 GetTimeLeft()
        {
            if (unit == UnitOfTime.Scaled)
            {
                return GridGame.Time - timeStarted;
            }
            else if (unit == UnitOfTime.Unscaled)
            {
                return GridGame.UnscaledTime - timeStarted;
            }

            return -1;
        }

        /// <summary>
        /// Rehydrates a retained timed action with the same values a fresh allocation
        /// would receive so rollback can reuse the pooled instance safely.
        /// </summary>
        internal void Configure(FixedDelayedEvent action, Fixed32 newDuration, Fixed32 timeBegan, UnitOfTime newUnit)
        {
            TimeStarted = timeBegan;
            onDelayComplete = action;
            timeRemaining = newDuration;
            duration = newDuration;
            unit = newUnit;
            shouldLoop = false;
            loopCount = 1;
            startingLoopCount = 0;
            loopCondition = null;
            hasPaused = false;
            OnComplete = null;
        }

        /// <summary>
        /// Will repeat this action with a delay.
        /// </summary>
        /// <param name="count">The amount of times to repeat the action. Set to -1 to repeat it infinitely.</param>
        public FixedTimeAction Loop(int count = -1)
        {
            shouldLoop = true;

            startingLoopCount = count;

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
            startingLoopCount = -1;
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
            hasPaused = true;
        }

        /// <summary>
        /// Starts a new coroutine using the remaining time found when the action was paused.
        /// </summary>
        public void Resume()
        {
            hasPaused = false;
        }

        /// <summary>
        /// Sets the time started to be the time that this function is called and resets the loop count to what it was when this timer was started.
        /// </summary>
        public void Reset()
        {
            if (!IsActive)
                Init();

            TimeStarted = GridGame.Time;
            timeRemaining = duration;

            if (startingLoopCount > 0)
                loopCount = startingLoopCount - 1;
            else if (startingLoopCount != -1)
                loopCount = 1;
        }

        protected override void Serialize(BinaryWriter bw)
        {
            timeStarted.Serialize(bw);
            duration.Serialize(bw);
            timeRemaining.Serialize(bw);
            bw.Write((int)unit);
            bw.Write(shouldLoop);
            bw.Write(loopCount);
            bw.Write(startingLoopCount);
            bw.Write(hasPaused);
        }

        protected override void Deserialize(Deserializer br)
        {
            timeStarted = timeStarted.Deserialize(br);
            duration = duration.Deserialize(br);
            timeRemaining = timeRemaining.Deserialize(br);
            unit = (UnitOfTime)br.ReadInt32();
            shouldLoop = br.ReadBoolean();
            loopCount = br.ReadInt32();
            startingLoopCount = br.ReadInt32();
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
            else if (unit == UnitOfTime.PauseScaled)
            {
                timeRemaining -= GridGame.FixedTimeStep * !GridGame.IsPaused;
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
                    OnComplete?.Invoke();
                }
                else
                {
                    timeRemaining = duration;
                }
            }
        }

        protected override void LogGameState(StringBuilder sb)
        {
            sb.AppendLine($"TimeStarted: {TimeStarted}");
            sb.AppendLine($"Duration: {Duration}");
            sb.AppendLine($"TimeRemaining: {timeRemaining}");
            sb.AppendLine($"Unit: {Unit}");
            sb.AppendLine($"ShouldLoop: {shouldLoop}");
            sb.AppendLine($"LoopCount: {loopCount}");
            sb.AppendLine($"StartingLoopCount: {startingLoopCount}");
            sb.AppendLine($"HasPaused: {hasPaused}");
        }
    }

    public class FixedConditionAction : FixedAction
    {
        private Condition _condition;

        public FixedConditionAction(FixedDelayedEvent action, Condition condition)
        {
            Configure(action, condition);
        }

        /// <summary>
        /// Rehydrates a retained condition action with the same callback and predicate
        /// a fresh allocation would receive for the current simulation pass.
        /// </summary>
        internal void Configure(FixedDelayedEvent action, Condition condition)
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

        protected override void Deserialize(Deserializer br)
        {
        }

        protected override void LogGameState(StringBuilder sb)
        {
        }

        protected override void Serialize(BinaryWriter bw)
        {
        }
    }

    public class FixedPointTimer
    {
        private static List<FixedAction> _actionsToRemove = new List<FixedAction>();
        private static SerializedListHandler<FixedAction> _actions = new SerializedListHandler<FixedAction>("Fixed Point Timer Actions");
        private static int _currentActionID = 0;

        public static SerializedListHandler<FixedAction> Actions { get => _actions; private set => _actions = value; }

        public static void LogGameState(StringBuilder stringBuilder)
        {
            _actions.OnLogGameState(stringBuilder);
            stringBuilder.AppendLine($"CurrentActionID: {_currentActionID}");
        }

        /// <summary>
        /// Starts a timed fixed-point action. An optional display name appears in
        /// serialized-list debug logs and does not affect simulation state.
        /// </summary>
        public static FixedTimeAction StartNewTimedAction(FixedDelayedEvent action, Fixed32 duration, UnitOfTime unit = UnitOfTime.Scaled, string displayName = null)
        {
            //Check if the serialized list handler already has a this action.
            if (_actions.TryGetSerializedItem(item => item is FixedTimeAction && item.ActionID == _currentActionID, out FixedAction retainedActionObject))
            {
                FixedTimeAction retainedAction = (FixedTimeAction)retainedActionObject;
                retainedAction.SetDisplayName(displayName);
                retainedAction.Configure(action, duration, GridGame.Time, unit);
                retainedAction.FrameStarted = GridGameManager.FrameNumber;
                retainedAction.IsActive = true;

                _currentActionID++;
                return retainedAction;
            }

            //If it doesn't make a new one.
            FixedTimeAction newAction = new FixedTimeAction(action, duration, GridGame.Time, unit);
            newAction.SetActionID(_currentActionID);
            newAction.SetDisplayName(displayName);
            newAction.FrameStarted = GridGameManager.FrameNumber;
            _actions.Add(newAction);
            newAction.IsActive = true;

            _currentActionID++;
            return newAction;
        }

        /// <summary>
        /// Starts a condition-based fixed-point action. An optional display name
        /// appears in serialized-list debug logs and does not affect simulation state.
        /// </summary>
        public static FixedConditionAction StartNewConditionAction(FixedDelayedEvent action, Condition condition, string displayName = null)
        {
            //Check if the serialized list handler already has a this action.
            if (_actions.TryGetSerializedItem(item => item is FixedConditionAction && item.ActionID == _currentActionID, out FixedAction retainedActionObject))
            {
                FixedConditionAction retainedAction = (FixedConditionAction)retainedActionObject;
                retainedAction.SetDisplayName(displayName);
                retainedAction.Configure(action, condition);
                retainedAction.FrameStarted = GridGameManager.FrameNumber;
                retainedAction.IsActive = true;

                _currentActionID++;
                return retainedAction;
            }

            //If it doesn't make a new one.
            FixedConditionAction fixedConditionAction = new FixedConditionAction(action, condition);
            fixedConditionAction.SetActionID(_currentActionID);
            fixedConditionAction.SetDisplayName(displayName);
            fixedConditionAction.FrameStarted = GridGameManager.FrameNumber;
            fixedConditionAction.IsActive = true;

            _actions.Add(fixedConditionAction);

            _currentActionID++;
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
            _actions.Serialize(bw);
            bw.Write(_currentActionID);
        }
         
        public static void DeserializeActions(Deserializer br)
        {
            _actions.Deserialize(br);
            _currentActionID = br.ReadInt32();
        }
    }
}
