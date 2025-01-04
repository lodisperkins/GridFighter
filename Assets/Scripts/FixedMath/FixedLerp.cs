using Assets.Scripts.Lodis.Simulation;
using System;
using System.Collections.Generic;
using System.IO;
using Types;
using UnityEngine;
using UnityGGPO;

namespace FixedPoints
{
    /// <summary>
    /// Static class that can lerp objects similar to DoTween but is in line with rollback.
    /// </summary>
    public static class FixedLerp
    {
        private static List<LerpAction> Actions { get; } = new List<LerpAction>();
        private static SerializedListHandler<LerpAction> serializedListHandler;

        static FixedLerp()
        {
            GridGame.OnSimulationUpdate += Update;
            GridGame.OnSerialization += SerializeActions;
            GridGame.OnDeserialization += DeserializeActions;
            serializedListHandler = new SerializedListHandler<LerpAction>(Actions);
            serializedListHandler.Name = "FixedLerp";
        }

        private static void SerializeActions(BinaryWriter bw)
        {
            //bw.Write(0);
            serializedListHandler.Serialize(bw);
        }

        private static void DeserializeActions(BinaryReader br)
        {
            //br.ReadInt32();
            serializedListHandler.Deserialize(br);
        }

        /// <summary>
        /// Updates all active lerp actions.
        /// </summary>
        private static void Update(Fixed32 dt)
        {
            for (int i = Actions.Count - 1; i >= 0; i--)
            {
                Actions[i].Update(dt);
            }

            Actions.RemoveAll(l => l.Killed);
            //Debug.Log($"Fixed lerp action count: {Actions.Count}");
        }

        public static T GetAction<T>(FTransform target) where T : LerpAction
        {
            LerpAction action = Actions.Find(a => a is T && a.GetTarget() == target);

            return (T)action;
        }

        /// <summary>
        /// Changes the target entity's world position over time.
        /// </summary>
        /// <param name="target">The transform of the rollback simulation entity.</param>
        /// <param name="endValue">The stopping point of the movement lerp.</param>
        /// <param name="duration">How long it will take to reach the stopping point.</param>
        /// <param name="curve">A curve to use to alter the lerp.</param>
        /// <returns></returns>
        public static LerpAction DoMove(FTransform target, FVector3 endValue, Fixed32 duration, FixedAnimationCurve curve = null)
        {
            LerpAction action = new MoveAction(target, target.WorldPosition, endValue, duration, curve);
            Actions.Add(action);
            return action;
        }

        /// <summary>
        /// Changes the target entity's world rotation over time.
        /// </summary>
        /// <param name="target">The transform of the rollback simulation entity.</param>
        /// <param name="endValue">The stopping point of the rotation lerp.</param>
        /// <param name="duration">How long it will take to reach the stopping point.</param>
        /// <param name="curve">A curve to use to alter the lerp.</param>
        /// <returns></returns>
        public static LerpAction DoRotate(FTransform target, FQuaternion endValue, Fixed32 duration, FixedAnimationCurve curve = null)
        {
            LerpAction action = new RotateAction(target, target.WorldRotation, endValue, duration, curve);
            Actions.Add(action);
            return action;
        }


        /// <summary>
        /// Changes the target entity's world scale over time.
        /// </summary>
        /// <param name="target">The transform of the rollback simulation entity.</param>
        /// <param name="endValue">The stopping point of the scale lerp.</param>
        /// <param name="duration">How long it will take to reach the stopping point.</param>
        /// <param name="curve">A curve to use to alter the lerp.</param>
        /// <returns></returns>
        public static LerpAction DoScale(FTransform target, FVector3 endValue, Fixed32 duration, FixedAnimationCurve curve = null)
        {
            LerpAction action = new ScaleAction(target, target.WorldScale, endValue, duration, curve);
            Actions.Add(action);
            return action;
        }


        /// <summary>
        /// Makes the target move to the end result quickly like a spring.
        /// </summary>
        /// <param name="target">The transform of the rollback simulation entity.</param>
        /// <param name="endValue">The stopping point of the punch lerp.</param>
        /// <param name="duration">How long it will take to reach the stopping point.</param>
        /// <param name="curve">A curve to use to alter the lerp.</param>
        /// <returns></returns>
        public static LerpAction DoPunch(FTransform target, FVector3 punchValue, Fixed32 duration, FixedAnimationCurve curve = null)
        {
            LerpAction action = new PunchAction(target, punchValue, duration, curve);
            Actions.Add(action);
            return action;
        }


        /// <summary>
        /// Changes the target entity's world position over time in an arc to simulate a jump.
        /// </summary>
        /// <param name="target">The transform of the rollback simulation entity.</param>
        /// <param name="endValue">The stopping point of the jump lerp.</param>
        /// <param name="duration">How long it will take to reach the stopping point.</param>
        /// <param name="curve">A curve to use to alter the lerp.</param>
        /// <returns></returns>
        public static LerpAction DoJump(FTransform target, FVector3 endValue, Fixed32 jumpPower, int numJumps, Fixed32 duration, FixedAnimationCurve curve = null)
        {
            LerpAction action = new JumpAction(target, target.WorldPosition, endValue, jumpPower, numJumps, duration, curve);
            Actions.Add(action);
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
        public static LerpAction To(Func<Fixed32> getter, Action<Fixed32> setter, Fixed32 endValue, Fixed32 duration, FixedAnimationCurve curve = null)
        {
            // Create a new tween action and add it to the list.
            LerpAction action = new FixedTweenAction(getter, setter, getter(), endValue, duration, curve);
            Actions.Add(action);

            return action;
        }

        public static bool ContainsAction(LerpAction action)
        {
            return Actions.Contains(action);
        }

        public static void RemoveAction(LerpAction action)
        {
            Actions.Remove(action);
        }
        public static void AddAction(LerpAction action)
        {
            if (Actions?.Contains(action) == true) return;

            Actions.Add(action);
        }
    }

    /// <summary>
    /// Abstract class that defines base logic for all fixed lerping.
    /// </summary>
    public abstract class LerpAction : ISerializedListObject
    {
        protected FTransform Target;
        protected Fixed32 Duration;
        protected FixedAnimationCurve Curve;
        protected Fixed32 TimeElapsed;
        protected bool IsPaused;
        private bool _killed;
        private int _frameStarted;

        public bool Killed
        {
            get => _killed;
        }

        public int FrameStarted
        {
            get { return _frameStarted; }
        }

        public delegate void LerpActionEvent();
        public event LerpActionEvent onKill;
        public event LerpActionEvent onRevive;
        public event LerpActionEvent onComplete;

        /// <param name="target">The transform of the entity this lerp action is for.</param>
        /// <param name="duration">How long this lerp action will last</param>
        /// <param name="curve">The animation curve that will control the flow of the action.</param>
        public LerpAction(FTransform target, Fixed32 duration, FixedAnimationCurve curve)
        {
            Target = target;
            Duration = duration;
            Curve = curve;
            TimeElapsed = 0;
            IsPaused = false;
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
                Debug.LogWarning("Tried to killed an action that was already killed.");
                return;
            }

            TimeElapsed = Duration;
            FixedLerp.RemoveAction(this);
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

        public virtual void OnSerialize(BinaryWriter bw)
        {
            TimeElapsed.Serialize(bw);
            bw.Write(IsPaused);
            bw.Write(_killed);
        }

        public virtual void OnDeserialize(BinaryReader br)
        {
            FixedLerp.AddAction(this);
            TimeElapsed.Deserialize(br);
            IsPaused = br.ReadBoolean();
            _killed = br.ReadBoolean();
        }
        public ListEvent OnAddedToList { get; set; }
        public int FrameSerialized { get; set; }
        public ListEvent OnRemovedFromList { get; set; }

        /// <summary>
        /// Progresses the lerp through time.
        /// </summary>
        /// <returns>Whether or not the lerp has completed on this update.</returns>
        public bool Update(Fixed32 dt)
        {
            if (IsPaused || _killed)
                return false;

            TimeElapsed += dt;

            //If time is up...
            if (TimeElapsed >= Duration)
            {    
                //...snap to the end.
                Apply(1);

                _killed = true;
                FixedLerp.RemoveAction(this);
                onComplete?.Invoke();

                return true;
            }
            //Otherwise...
            else
            {
                //...either apply the curve value or the value of linear time.
                Apply(Curve != null ? Curve.Evaluate(TimeElapsed / Duration) : TimeElapsed / Duration);
                return false;
            }
        }

        /// <summary>
        /// Abstract function meant to be overriden so that other action can apply some transformation to the target entity.
        /// </summary>
        /// <param name="t"></param>
        protected abstract void Apply(Fixed32 t);

        public bool CheckIfCanBeAddedToList()
        {
            return !_killed/* && FrameStarted <= GridGameManager.FrameNumber*/;
        }
    }

    /// <summary>
    /// An action that changes the world position of the target entity.
    /// </summary>
    public class MoveAction : LerpAction
    {
        private FVector3 StartValue;
        private FVector3 EndValue;

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

        public override void OnSerialize(BinaryWriter bw)
        {
            base.OnSerialize(bw);
            StartValue.Serialize(bw);
            EndValue.Serialize(bw);
        }

        public override void OnDeserialize(BinaryReader br)
        {
            base.OnDeserialize(br);
            StartValue.Deserialize(br);
            EndValue.Deserialize(br);
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

        public override void OnSerialize(BinaryWriter bw)
        {
            base.OnSerialize(bw);
            StartValue.Serialize(bw);
            EndValue.Serialize(bw);
        }

        public override void OnDeserialize(BinaryReader br)
        {
            base.OnDeserialize(br);
            StartValue.Deserialize(br);
            EndValue.Deserialize(br);
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

        public override void OnSerialize(BinaryWriter bw)
        {
            base.OnSerialize(bw);
            StartValue.Serialize(bw);
            EndValue.Serialize(bw);
        }

        public override void OnDeserialize(BinaryReader br)
        {
            base.OnDeserialize(br);
            StartValue.Deserialize(br);
            EndValue.Deserialize(br);
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

        public override void OnSerialize(BinaryWriter bw)
        {
            base.OnSerialize(bw);
            StartValue.Serialize(bw);
            PunchValue.Serialize(bw);
        }

        public override void OnDeserialize(BinaryReader br)
        {
            base.OnDeserialize(br);
            StartValue.Deserialize(br);
            PunchValue.Deserialize(br);
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

        public override void OnSerialize(BinaryWriter bw)
        {
            base.OnSerialize(bw);
            StartValue.Serialize(bw);
            EndValue.Serialize(bw);
        }

        public override void OnDeserialize(BinaryReader br)
        {
            base.OnDeserialize(br);
            StartValue.Deserialize(br);
            EndValue.Deserialize(br);
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
        private readonly Func<Fixed32> _getter;
        private readonly Action<Fixed32> _setter;
        private Fixed32 _startValue;
        private Fixed32 _endValue;

        public FixedTweenAction(Func<Fixed32> getter, Action<Fixed32> setter, Fixed32 startValue, Fixed32 endValue, Fixed32 duration, FixedAnimationCurve curve = null)
            : base(null, duration, curve)
        {
            _getter = getter;
            _setter = setter;
            _startValue = startValue;
            _endValue = endValue;
        }

        protected override void Apply(Fixed32 t)
        {
            Fixed32 currentValue = _startValue + (_endValue - _startValue) * t;
            _setter(currentValue);
        }

        public override void OnSerialize(BinaryWriter bw)
        {
            base.OnSerialize(bw);
            _startValue.Serialize(bw);
            _endValue.Serialize(bw);
        }

        public override void OnDeserialize(BinaryReader br)
        {
            base.OnDeserialize(br);
            _startValue = (Fixed32)br.ReadSingle();
            _endValue = (Fixed32)br.ReadSingle();
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
