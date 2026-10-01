using Assets.Scripts.Lodis.Simulation;
using Lodis.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Types;
using UnityEngine;
using UnityEngine.Pool;

namespace FixedPoints
{
    /// <summary>
    /// Static class that can lerp objects similar to DoTween but is in line with rollback.
    /// </summary>
    public static class FixedLerp
    {
        private static SerializedListHandler<LerpAction> _actions;
        private static int _currentActionID;
        public static SerializedListHandler<LerpAction> Actions => _actions;

        static FixedLerp()
        {
            GridGame.OnSimulationUpdate += Update;
            _actions = new SerializedListHandler<LerpAction>("FixedLerp");
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

        public static void LogGameState(StringBuilder stringBuilder)
        {
            _actions.OnLogGameState(stringBuilder);
            stringBuilder.AppendLine($"CurrentActionID: {_currentActionID}");
        }

        /// <summary>
        /// Updates all active lerp actions.
        /// </summary>
        private static void Update(Fixed32 dt)
        {
            for (int i = _actions.Count - 1; i >= 0; i--)
            {
                _actions[i].Update(dt);
            }

            _actions.RemoveAll(l => l.Killed);
            //Debug.Log($"Fixed lerp action count: {Actions.Count}");
        }

        public static T GetAction<T>(FTransform target) where T : LerpAction
        {
            LerpAction action = _actions.Find(a => a is T && a.GetTarget() == target);

            return (T)action;
        }

        /// <summary>
        /// Returns the active lerp action with the specified deterministic action ID.
        /// This does not reactivate retained actions, so it is safe to use while
        /// reconnecting component references after deserialization.
        /// </summary>
        public static LerpAction GetSerializedActionByID(int actionID)
        {
            _actions.TryGetSerializedItem(item => item.ActionID == actionID, out LerpAction action, false);
            return action;
        }

        /// <summary>
        /// Changes the target entity's world position over time.
        /// </summary>
        /// <param name="target">The transform of the rollback simulation entity.</param> 
        /// <param name="endValue">The stopping point of the movement lerp.</param>
        /// <param name="duration">How long it will take to reach the stopping point.</param>
        /// <param name="curve">A curve to use to alter the lerp.</param>
        /// <param name="id">Optional ID for the lerp action.</param>
        public static LerpAction DoMove(FTransform target, FVector3 endValue, Fixed32 duration, FixedAnimationCurve curve = null, string id = null)
        {
            if (_actions.TryGetSerializedItem(item => item is MoveAction && item.ActionID == _currentActionID, out LerpAction retainedAction))
            {
                MoveAction reusedAction = (MoveAction)retainedAction;
                reusedAction.Reinitialize(target, target.WorldPosition, endValue, duration, curve);
                reusedAction.Name = id;
                _currentActionID++;
                return reusedAction;
            }

            LerpAction action = new MoveAction(target, target.WorldPosition, endValue, duration, curve);
            action.SetActionID(_currentActionID);
            action.Name = id;
            _actions.Add(action);
            _currentActionID++;
            return action;
        }

        /// <summary>
        /// Changes the target entity's world rotation over time.
        /// </summary>
        /// <param name="target">The transform of the rollback simulation entity.</param>
        /// <param name="endValue">The stopping point of the rotation lerp.</param>
        /// <param name="duration">How long it will take to reach the stopping point.</param>
        /// <param name="curve">A curve to use to alter the lerp.</param>
        /// <param name="id">Optional ID for the lerp action.</param>
        public static LerpAction DoRotate(FTransform target, FQuaternion endValue, Fixed32 duration, FixedAnimationCurve curve = null, string id = null)
        {
            if (_actions.TryGetSerializedItem(item => item is RotateAction && item.ActionID == _currentActionID, out LerpAction retainedAction))
            {
                RotateAction reusedAction = (RotateAction)retainedAction;
                reusedAction.Reinitialize(target, target.WorldRotation, endValue, duration, curve);
                reusedAction.Name = id;
                _currentActionID++;
                return reusedAction;
            }

            LerpAction action = new RotateAction(target, target.WorldRotation, endValue, duration, curve);
            action.SetActionID(_currentActionID);
            action.Name = id;
            _actions.Add(action);
            _currentActionID++;
            return action;
        }

        /// <summary>
        /// Changes the target entity's world scale over time.
        /// </summary>
        /// <param name="target">The transform of the rollback simulation entity.</param>
        /// <param name="endValue">The stopping point of the scale lerp.</param>
        /// <param name="duration">How long it will take to reach the stopping point.</param>
        /// <param name="curve">A curve to use to alter the lerp.</param>
        /// <param name="id">Optional ID for the lerp action.</param>
        public static LerpAction DoScale(FTransform target, FVector3 endValue, Fixed32 duration, FixedAnimationCurve curve = null, string id = null)
        {
            if (_actions.TryGetSerializedItem(item => item is ScaleAction && item.ActionID == _currentActionID, out LerpAction retainedAction))
            {
                ScaleAction reusedAction = (ScaleAction)retainedAction;
                reusedAction.Reinitialize(target, target.WorldScale, endValue, duration, curve);
                reusedAction.Name = id;
                _currentActionID++;
                return reusedAction;
            }

            LerpAction action = new ScaleAction(target, target.WorldScale, endValue, duration, curve);
            action.SetActionID(_currentActionID);
            action.Name = id;
            _actions.Add(action);
            _currentActionID++;
            return action;
        }

        /// <summary>
        /// Makes the target move to the end result quickly like a spring.
        /// </summary>
        /// <param name="target">The transform of the rollback simulation entity.</param>
        /// <param name="punchValue">The stopping point of the punch lerp.</param>
        /// <param name="duration">How long it will take to reach the stopping point.</param>
        /// <param name="curve">A curve to use to alter the lerp.</param>
        /// <param name="id">Optional ID for the lerp action.</param>
        public static LerpAction DoPunch(FTransform target, FVector3 punchValue, Fixed32 duration, FixedAnimationCurve curve = null, string id = null)
        {
            if (_actions.TryGetSerializedItem(item => item is PunchAction && item.ActionID == _currentActionID, out LerpAction retainedAction))
            {
                PunchAction reusedAction = (PunchAction)retainedAction;
                reusedAction.Reinitialize(target, punchValue, duration, curve);
                reusedAction.Name = id;
                _currentActionID++;
                return reusedAction;
            }

            LerpAction action = new PunchAction(target, punchValue, duration, curve);
            action.SetActionID(_currentActionID);
            action.Name = id;
            _actions.Add(action);
            _currentActionID++;
            return action;
        }

        /// <summary>
        /// Changes the target entity's world position over time in an arc to simulate a jump.
        /// </summary>
        /// <param name="target">The transform of the rollback simulation entity.</param>
        /// <param name="endValue">The stopping point of the jump lerp.</param>
        /// <param name="jumpPower">The height/power of the jump.</param>
        /// <param name="numJumps">Number of jumps to simulate.</param>
        /// <param name="duration">How long it will take to reach the stopping point.</param>
        /// <param name="curve">A curve to use to alter the lerp.</param>
        /// <param name="id">Optional ID for the lerp action.</param>
        public static LerpAction DoJump(FTransform target, FVector3 endValue, Fixed32 jumpPower, int numJumps, Fixed32 duration, FixedAnimationCurve curve = null, string id = null)
        {
            if (_actions.TryGetSerializedItem(item => item is JumpAction && item.ActionID == _currentActionID, out LerpAction retainedAction))
            {
                JumpAction reusedAction = (JumpAction)retainedAction;
                reusedAction.Reinitialize(target, target.WorldPosition, endValue, jumpPower, numJumps, duration, curve);
                reusedAction.Name = id;
                _currentActionID++;
                return reusedAction;
            }

            LerpAction action = new JumpAction(target, target.WorldPosition, endValue, jumpPower, numJumps, duration, curve);
            action.SetActionID(_currentActionID);
            action.Name = id;
            _actions.Add(action);
            _currentActionID++;
            return action;
        }

        /// <summary>
        /// Creates a tween that interpolates a value over time.
        /// </summary>
        /// <param name="getter">A delegate to get the current value.</param>
        /// <param name="setter">A delegate to set the value during interpolation.</param>
        /// <param name="endValue">The target value at the end of the tween.</param>
        /// <param name="duration">The duration of the tween in fixed time.</param>
        /// <param name="curve">Optional curve to modify the interpolation behavior.</param>
        /// <param name="id">Optional ID for the lerp action.</param>
        public static LerpAction To(Func<Fixed32> getter, Action<Fixed32> setter, Fixed32 endValue, Fixed32 duration, FixedAnimationCurve curve = null, string id = null)
        {
            if (_actions.TryGetSerializedItem(item => item is FixedTweenAction && item.ActionID == _currentActionID, out LerpAction retainedAction))
            {
                FixedTweenAction reusedAction = (FixedTweenAction)retainedAction;
                reusedAction.Reinitialize(getter, setter, getter(), endValue, duration, curve);
                reusedAction.Name = id;
                _currentActionID++;
                return reusedAction;
            }

            LerpAction action = new FixedTweenAction(getter, setter, getter(), endValue, duration, curve);
            action.SetActionID(_currentActionID);
            action.Name = id;
            _actions.Add(action);
            _currentActionID++;
            return action;
        }

        public static bool ContainsAction(LerpAction action)
        {
            return _actions.Contains(action);
        }

        public static void RemoveAction(LerpAction action)
        {
            _actions.Remove(action);
        }
        public static void AddAction(LerpAction action)
        {
            _actions.Add(action);
        }
    }

    /// <summary>
    /// Abstract class that defines base logic for all fixed lerping.
    /// </summary>
    public abstract class LerpAction : ISerializedListObject
    {

        #region Simulation Functions

        public virtual void OnSerialize(BinaryWriter bw)
        {
            bw.Write(ActionID);
            TimeElapsed.Serialize(bw);
            Duration.Serialize(bw);
            bw.Write((int)Unit);
            bw.Write(IsPaused);
            bw.Write(_killed);
        }


        public virtual void OnDeserialize(Deserializer br)
        {
            ActionID = br.ReadInt32();
            TimeElapsed = TimeElapsed.Deserialize(br);
            Duration = Duration.Deserialize(br);
            Unit = (FixedTimeAction.UnitOfTime)br.ReadInt32();
            IsPaused = br.ReadBoolean();
            _killed = br.ReadBoolean();
        }

        #endregion

        protected FTransform Target;
        protected Fixed32 Duration;
        protected FixedAnimationCurve Curve;
        protected Fixed32 TimeElapsed;
        protected bool IsPaused;
        private bool _killed;
        private int _frameStarted;
        public int ActionID { get; private set; }
        public string Name;
        public FixedTimeAction.UnitOfTime Unit;

        public bool Killed
        {
            get => _killed;
        }

        public int FrameStarted
        {
            get { return _frameStarted; }
        }

        /// <summary>
        /// Gets the serialized amount of time this action has already progressed.
        /// </summary>
        public Fixed32 ElapsedTime => TimeElapsed;

        /// <summary>
        /// Gets the serialized total duration of this action.
        /// </summary>
        public Fixed32 TotalDuration => Duration;
        public ListEvent OnAddedToList { get; set; }
        public int FrameAddedToSerializedList { get; set; }
        public int SerializedChecksum { get; set; }
        public ListEvent OnRemovedFromList { get; set; }

        public string ListDisplayName => Name == null ? "LerpAction" : Name;

        public int FrameRemovedFromActiveList { get; set; }

        public delegate void LerpActionEvent();
        public event LerpActionEvent onKill;
        public event LerpActionEvent onRevive;
        public event LerpActionEvent onComplete;

        /// <param name="target">The transform of the entity this lerp action is for.</param>
        /// <param name="duration">How long this lerp action will last</param>
        /// <param name="curve">The animation curve that will control the flow of the action.</param>
        public LerpAction(FTransform target, Fixed32 duration, FixedAnimationCurve curve)
        {
            ResetState(target, duration, curve);
        }

        /// <summary>
        /// Assigns the deterministic ID used to find this retained action when a
        /// rollback replay creates the same lerp again.
        /// </summary>
        internal void SetActionID(int actionID)
        {
            ActionID = actionID;
        }

        protected void ResetState(FTransform target, Fixed32 duration, FixedAnimationCurve curve)
        {
            Target = target;
            Duration = duration;
            Curve = curve;
            TimeElapsed = 0;
            IsPaused = false;
            _killed = false;
            _frameStarted = GridGameManager.FrameNumber;
        }

        public bool IsPlaying()
        {
            return !IsPaused && TimeElapsed < Duration && !_killed && FixedLerp.ContainsAction(this);
        }

        public FTransform GetTarget()
        {
            return Target;
        }

        /// <summary>
        /// Stop the timer for the lerp action.
        /// </summary>
        public void Pause() => IsPaused = true;
        /// <summary>
        /// Continues the timer for the lerp action.
        /// </summary>
        public void Resume()
        {
            if (_killed)
            {
                Debug.LogWarning("Tried to resume an action that was already killed.");
                return;
            }

            IsPaused = false;
        }

        /// <summary>
        /// Force the lerp action to stop.
        /// </summary>
        public void Kill()
        {
            if (_killed)
            {
                return;
            }

            TimeElapsed = Duration;
            FixedLerp.RemoveAction(this);

            //Debug.Log($"Killed {ID}");
            _killed = true;
            onKill?.Invoke();
        }

        /// <summary>
        /// Reset the lerp action to the orignal starting position.
        /// </summary>
        public void Rewind()
        {
            IsPaused = false;
            _killed = false;
            TimeElapsed = 0;
            _frameStarted = GridGameManager.FrameNumber;
            onRevive?.Invoke();
            FixedLerp.AddAction(this);
        }



        public virtual void OnLogGameState(StringBuilder sb)
        {
            sb.AppendLine($"            {ListDisplayName}");
            sb.AppendLine($"                  DebugId={ActionID}");
            sb.AppendLine($"                  TimeElapsed={TimeElapsed}");
            sb.AppendLine($"                  Duration={Duration}");
            sb.AppendLine($"                  Unit={Unit}");
            sb.AppendLine($"                  IsPaused={IsPaused}");
            sb.AppendLine($"                  Killed={_killed}");
        }

        /// <summary>
        /// Hashes this lerp action's rollback payload for debug game-state logging.
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

        /// <summary>
        /// Writes the rollback payload used to compare this lerp action's state in
        /// the debug checksum log.
        /// </summary>
        public virtual void WriteChecksumPayload(BinaryWriter bw)
        {
            OnSerialize(bw);
        }

        /// <summary>
        /// Produces a lightweight deterministic fingerprint of serialized lerp data.
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

        /// <summary>
        /// Progresses the lerp through time.
        /// </summary>
        /// <returns>Whether or not the lerp has completed on this update.</returns>
        public bool Update(Fixed32 dt)
        {
            if (IsPaused || _killed)
            {
                //Debug.Log($"Didnt update {ID} because it was paused or killed.");
                return false;
            }

            if (Unit == FixedTimeAction.UnitOfTime.Scaled)
            {
                dt *= GridGame.TimeScale;
            }
            else if (Unit == FixedTimeAction.UnitOfTime.PauseScaled && GridGame.IsPaused)
            {
                dt = 0;
            }

            TimeElapsed += dt;

            //Debug.Log("Update called for " + ID);

            //If time is up...
            if (TimeElapsed >= Duration)
            {    
                //...snap to the end.
                Apply(1);

                _killed = true;
                FixedLerp.RemoveAction(this);
                onComplete?.Invoke();

                //Debug.Log($"Completed lerp {ID}");
                return true;
            }
            //Otherwise...
            else
            {
                //...either apply the curve value or the value of linear time.
                Apply(Curve != null ? Curve.Evaluate(TimeElapsed / Duration) : TimeElapsed / Duration);

                //Debug.Log($"Evaluating lerp {ID}. Currently at {TimeElapsed / Duration}");
                return false;
            }
        }

        /// <summary>
        /// Abstract function meant to be overriden so that other action can apply some transformation to the target entity.
        /// </summary>
        /// <param name="t"></param>
        protected abstract void Apply(Fixed32 t);

    }

    /// <summary>
    /// An action that changes the world position of the target entity.
    /// </summary>
    public class MoveAction : LerpAction
    {

        #region Simulation Functions

        public override void OnSerialize(BinaryWriter bw)
        {
            base.OnSerialize(bw);
            StartValue.Serialize(bw);
            EndValue.Serialize(bw);
        }

        public override void OnDeserialize(Deserializer br)
        {
            base.OnDeserialize(br);
            StartValue = StartValue.Deserialize(br);
            EndValue = EndValue.Deserialize(br);
        }

        #endregion


        private FVector3 StartValue;
        private FVector3 EndValue;

        /// <summary>
        /// Gets the serialized position from which this move action interpolates.
        /// </summary>
        public FVector3 StartPosition => StartValue;

        /// <summary>
        /// Gets the serialized position this move action is interpolating toward.
        /// </summary>
        public FVector3 EndPosition => EndValue;

        //Need a way to serialize pool. Maybe swap to pool for all actions

        public MoveAction(FTransform target, FVector3 startValue, FVector3 endValue, Fixed32 duration, FixedAnimationCurve curve = null)
            : base(target, duration, curve)
        {
            StartValue = startValue;
            EndValue = endValue;
        }

        protected override void Apply(Fixed32 t)
        {
            FVector3 newPosition = StartValue + (EndValue - StartValue) * t;
            Target.WorldPosition = newPosition;
        }

        public override void OnLogGameState(StringBuilder sb)
        {
            base.OnLogGameState(sb);
            sb.AppendLine($"                  StartValue={StartValue}");
            sb.AppendLine($"                  EndValue={EndValue}");
        }

        public void Reinitialize(FTransform target, FVector3 startValue, FVector3 endValue, Fixed32 duration, FixedAnimationCurve curve = null)
        {
            ResetState(target, duration, curve);
            StartValue = startValue;
            EndValue = endValue;
            Name = null;
        }

        public void ChangeEndValue(FVector3 newEndValue)
        {
            Rewind();
            EndValue = newEndValue;
        }

        public void ChangeStartValue(FVector3 newStartValue)
        {
            Rewind();
            StartValue = newStartValue;
        }

        public void ChangeValues(FVector3 newStartValue, FVector3 newEndValue)
        {
            Rewind();
            StartValue = newStartValue;
            EndValue = newEndValue;
        }
    }

    /// <summary>
    /// An action that changes the world rotation of the target entity.
    /// </summary>
    public class RotateAction : LerpAction
    {

        #region Simulation Functions

        public override void OnSerialize(BinaryWriter bw)
        {
            base.OnSerialize(bw);
            StartValue.Serialize(bw);
            EndValue.Serialize(bw);
        }


        public override void OnDeserialize(Deserializer br)
        {
            base.OnDeserialize(br);
            StartValue = StartValue.Deserialize(br);
            EndValue = EndValue.Deserialize(br);
        }

        #endregion

        private FQuaternion StartValue;
        private FQuaternion EndValue;

        public RotateAction(FTransform target, FQuaternion startValue, FQuaternion endValue, Fixed32 duration, FixedAnimationCurve curve = null)
            : base(target, duration, curve)
        {
            StartValue = startValue;
            EndValue = endValue;
        }

        protected override void Apply(Fixed32 t)
        {
            FQuaternion newRotation = FQuaternion.Lerp(StartValue, EndValue, t);
            Target.WorldRotation = newRotation;
        }



        public override void OnLogGameState(StringBuilder sb)
        {
            base.OnLogGameState(sb);
            sb.AppendLine($"                  StartValue={StartValue}");
            sb.AppendLine($"                  EndValue={EndValue}");
        }

        public void Reinitialize(FTransform target, FQuaternion startValue, FQuaternion endValue, Fixed32 duration, FixedAnimationCurve curve = null)
        {
            ResetState(target, duration, curve);
            StartValue = startValue;
            EndValue = endValue;
            Name = null;
        }

        public void ChangeEndValue(FQuaternion newEndValue)
        {
            Rewind();
            EndValue = newEndValue;
        }

        public void ChangeStartValue(FQuaternion newStartValue)
        {
            Rewind();
            StartValue = newStartValue;
        }

        public void ChangeValues(FQuaternion newStartValue, FQuaternion newEndValue)
        {
            Rewind();
            StartValue = newStartValue;
            EndValue = newEndValue;
        }
    }

    /// <summary>
    /// An action that changes the world scale of the target entity.
    /// </summary>
    public class ScaleAction : LerpAction
    {

        #region Simulation Functions

        public override void OnSerialize(BinaryWriter bw)
        {
            base.OnSerialize(bw);
            StartValue.Serialize(bw);
            EndValue.Serialize(bw);
        }


        public override void OnDeserialize(Deserializer br)
        {
            base.OnDeserialize(br);
            StartValue = StartValue.Deserialize(br);
            EndValue = EndValue.Deserialize(br);
        }

        #endregion

        private FVector3 StartValue;
        private FVector3 EndValue;

        public ScaleAction(FTransform target, FVector3 startValue, FVector3 endValue, Fixed32 duration, FixedAnimationCurve curve = null)
            : base(target, duration, curve)
        {
            StartValue = startValue;
            EndValue = endValue;
        }

        protected override void Apply(Fixed32 t)
        {
            FVector3 newScale = StartValue + (EndValue - StartValue) * t;
            Target.WorldScale = newScale;
        }



        public override void OnLogGameState(StringBuilder sb)
        {
            base.OnLogGameState(sb);
            sb.AppendLine($"                  StartValue={StartValue}");
            sb.AppendLine($"                  EndValue={EndValue}");
        }

        public void Reinitialize(FTransform target, FVector3 startValue, FVector3 endValue, Fixed32 duration, FixedAnimationCurve curve = null)
        {
            ResetState(target, duration, curve);
            StartValue = startValue;
            EndValue = endValue;
            Name = null;
        }

        public void ChangeEndValue(FVector3 newEndValue)
        {
            Rewind();
            EndValue = newEndValue;
        }

        public void ChangeStartValue(FVector3 newStartValue)
        {
            Rewind();
            StartValue = newStartValue;
        }

        public void ChangeValues(FVector3 newStartValue, FVector3 newEndValue)
        {
            Rewind();
            StartValue = newStartValue;
            EndValue = newEndValue;
        }
    }


    public class PunchAction : LerpAction
    {

        #region Simulation Functions

        public override void OnSerialize(BinaryWriter bw)
        {
            base.OnSerialize(bw);
            StartValue.Serialize(bw);
            PunchValue.Serialize(bw);
        }


        public override void OnDeserialize(Deserializer br)
        {
            base.OnDeserialize(br);
            StartValue = StartValue.Deserialize(br);
            PunchValue = PunchValue.Deserialize(br);
        }

        #endregion

        private FVector3 PunchValue;
        private FVector3 StartValue;

        public PunchAction(FTransform target, FVector3 punchValue, Fixed32 duration, FixedAnimationCurve curve = null)
            : base(target, duration, curve)
        {
            PunchValue = punchValue;
            StartValue = target.WorldPosition;
        }

        protected override void Apply(Fixed32 t)
        {
            FVector3 newValue = StartValue + PunchValue * (1 - t);
            Target.WorldPosition = newValue;
        }



        public override void OnLogGameState(StringBuilder sb)
        {
            base.OnLogGameState(sb);
            sb.AppendLine($"                  StartValue={StartValue}");
            sb.AppendLine($"                  PunchValue={PunchValue}");
        }

        public void Reinitialize(FTransform target, FVector3 punchValue, Fixed32 duration, FixedAnimationCurve curve = null)
        {
            ResetState(target, duration, curve);
            StartValue = target.WorldPosition;
            PunchValue = punchValue;
            Name = null;
        }

        public void ChangeEndValue(FVector3 newEndValue)
        {
            Rewind();
            PunchValue = newEndValue;
        }

        public void ChangeStartValue(FVector3 newStartValue)
        {
            Rewind();
            StartValue = newStartValue;
        }

        public void ChangeValues(FVector3 newStartValue, FVector3 newPunchValue)
        {
            Rewind();
            StartValue = newStartValue;
            PunchValue = newPunchValue;
        }
    }


    public class JumpAction : LerpAction
    {

        #region Simulation Functions

        public override void OnSerialize(BinaryWriter bw)
        {
            base.OnSerialize(bw);
            StartValue.Serialize(bw);
            EndValue.Serialize(bw);
        }


        public override void OnDeserialize(Deserializer br)
        {
            base.OnDeserialize(br);
            StartValue = StartValue.Deserialize(br);
            EndValue = EndValue.Deserialize(br);
        }

        #endregion

        private FVector3 StartValue;
        private FVector3 EndValue;
        private Fixed32 JumpPower;
        private int NumJumps;

        public JumpAction(FTransform target, FVector3 startValue, FVector3 endValue, Fixed32 jumpPower, int numJumps, Fixed32 duration, FixedAnimationCurve curve = null)
            : base(target, duration, curve)
        {
            StartValue = startValue;
            EndValue = endValue;
            JumpPower = jumpPower;
            NumJumps = numJumps;
        }

        protected override void Apply(Fixed32 t)
        {
            Fixed32 progress = (Fixed32)(t * Fixed32.PI * NumJumps);
            Fixed32 yOffset = JumpPower * Fixed32.Sin(progress);
            FVector3 newValue = StartValue + (EndValue - StartValue) * t + new FVector3(0, yOffset, 0);
            Target.WorldPosition = newValue;
        }



        public override void OnLogGameState(StringBuilder sb)
        {
            base.OnLogGameState(sb);
            sb.AppendLine($"                  StartValue={StartValue}");
            sb.AppendLine($"                  EndValue={EndValue}");
        }

        public void Reinitialize(FTransform target, FVector3 startValue, FVector3 endValue, Fixed32 jumpPower, int numJumps, Fixed32 duration, FixedAnimationCurve curve = null)
        {
            ResetState(target, duration, curve);
            StartValue = startValue;
            EndValue = endValue;
            JumpPower = jumpPower;
            NumJumps = numJumps;
            Name = null;
        }

        public void ChangeEndValue(FVector3 newEndValue)
        {
            Rewind();
            EndValue = newEndValue;
        }

        public void ChangeStartValue(FVector3 newStartValue)
        {
            Rewind();
            StartValue = newStartValue;
        }

        public void ChangeValues(FVector3 newStartValue, FVector3 newEndValue)
        {
            Rewind();
            StartValue = newStartValue;
            EndValue = newEndValue;
        }
    }


    /// <summary>
    /// Represents a fixed-point tween action that interpolates a value over time.
    /// </summary>
    public class FixedTweenAction : LerpAction
    {

        #region Simulation Functions

        public override void OnSerialize(BinaryWriter bw)
        {
            base.OnSerialize(bw);
            _startValue.Serialize(bw);
            _endValue.Serialize(bw);
        }


        public override void OnDeserialize(Deserializer br)
        {
            base.OnDeserialize(br);
            _startValue = _startValue.Deserialize(br);
            _endValue = _endValue.Deserialize(br);
        }

        #endregion

        private Func<Fixed32> _getter;
        private Action<Fixed32> _setter;
        private Fixed32 _startValue;
        private Fixed32 _endValue;

        public FixedTweenAction(Func<Fixed32> getter, Action<Fixed32> setter, Fixed32 startValue, Fixed32 endValue, Fixed32 duration, FixedAnimationCurve curve = null)
            : base(null, duration, curve)
        {
            Reinitialize(getter, setter, startValue, endValue, duration, curve);
        }

        protected override void Apply(Fixed32 t)
        {
            Fixed32 currentValue = _startValue + (_endValue - _startValue) * t;
            _setter(currentValue);
        }



        public override void OnLogGameState(StringBuilder sb)
        {
            base.OnLogGameState(sb);
            sb.AppendLine($"                  StartValue={_startValue}");
            sb.AppendLine($"                  EndValue={_endValue}");
        }

        /// <summary>
        /// Reconfigures a retained tween action when the same deterministic action
        /// ID is created again during rollback replay.
        /// </summary>
        public void Reinitialize(Func<Fixed32> getter, Action<Fixed32> setter, Fixed32 startValue, Fixed32 endValue, Fixed32 duration, FixedAnimationCurve curve = null)
        {
            ResetState(null, duration, curve);
            _getter = getter;
            _setter = setter;
            _startValue = startValue;
            _endValue = endValue;
            Name = null;
        }


        public void ChangeEndValue(Fixed32 newEndValue)
        {
            Rewind();
            _endValue = newEndValue;
        }

        public void ChangeStartValue(Fixed32 newStartValue)
        {
            Rewind();
            _startValue = newStartValue;
        }

        public void ChangeValues(Fixed32 newStartValue, Fixed32 newEndValue)
        {
            Rewind();
            _startValue = newStartValue;
            _endValue = newEndValue;
        }
    }

}
