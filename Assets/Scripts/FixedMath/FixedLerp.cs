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

        static FixedLerp()
        {
            GridGame.OnSimulationUpdate += Update;
            GridGame.OnSerialization += SerializeActions;
            GridGame.OnDeserialization += DeserializeActions;
        }

        private static void SerializeActions(BinaryWriter bw)
        {
            for (int i = 0; i < Actions.Count; i++)
            {
                Actions[i].Serialize(bw);
            }
        }

        private static void DeserializeActions(BinaryReader br)
        {
            for (int i = 0; i < Actions.Count; i++)
            {
                Actions[i].Deserialize(br);
            }
        }

        private static void Update(float dt)
        {
            for (int i = Actions.Count - 1; i >= 0; i--)
            {
                if (Actions[i].Update())
                {
                    Actions.RemoveAt(i);
                }
            }
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
            LerpAction action = new MoveAction(target, target.Position, endValue, duration, curve);
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
            LerpAction action = new RotateAction(target, target.Rotation, endValue, duration, curve);
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
            LerpAction action = new ScaleAction(target, target.Scale, endValue, duration, curve);
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
            LerpAction action = new JumpAction(target, target.Position, endValue, jumpPower, numJumps, duration, curve);
            Actions.Add(action);
            return action;
        }

        public static void RemoveAction(LerpAction action)
        {
            Actions.Remove(action);
        }
    }

    /// <summary>
    /// Abstract class that defines base logic for all fixed lerping.
    /// </summary>
    public abstract class LerpAction
    {
        protected FTransform Target;
        protected Fixed32 Duration;
        protected FixedAnimationCurve Curve;
        protected Fixed32 TimeElapsed;
        protected bool IsPaused;
        private bool _killed;


        public delegate void LerpActionEvent();
        public event LerpActionEvent onKill;
        public event LerpActionEvent onComplete;

        /// <param name="target">The transform of the entity this lerp action is for.</param>
        /// <param name="duration">How long this lerp action will last</param>
        /// <param name="curve">The animation curve that will control the flow of the action.</param>
        public LerpAction(FTransform target, Fixed32 duration, FixedAnimationCurve curve)
        {
            Target = target;
            Duration = duration;
            Curve = curve;
            TimeElapsed = new Fixed32(0);
            IsPaused = false;
        }

        public bool IsPlaying()
        {
            return !IsPaused && TimeElapsed < Duration && !_killed;
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
        public void Rewind() => TimeElapsed = new Fixed32(0);

        public virtual void Serialize(BinaryWriter bw)
        {
            Target.Serialize(bw);
            Duration.Serialize(bw);
            TimeElapsed.Serialize(bw);
            bw.Write(IsPaused);
        }

        public virtual void Deserialize(BinaryReader br)
        {
            Target.Deserialize(br);
            Duration.Deserialize(br);
            TimeElapsed.Deserialize(br);
            IsPaused = br.ReadBoolean();
        }

        public bool Update()
        {
            if (IsPaused || _killed)
                return false;

            TimeElapsed += Utils.TimeGetTime();

            //If time is up...
            if (TimeElapsed.RawValue >= Duration.RawValue)
            {
                //...snap to the end.
                Apply(new Fixed32(1));

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
            Target.Position = newPosition;
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
            Target.Rotation = newRotation;
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
            Target.Scale = newScale;
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
            StartValue = target.Position;
        }

        protected override void Apply(Fixed32 t)
        {
            // Example punch logic, needs actual punch algorithm
            FVector3 newValue = StartValue + PunchValue * (new Fixed32(1) - t);
            Target.Position = newValue;
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
            //Jumping using parabolic arc
            Fixed32 progress = (Fixed32)(t * Fixed32.PI * NumJumps);
            Fixed32 yOffset = JumpPower * Fixed32.Sin(progress);
            FVector3 newValue = StartValue + (EndValue - StartValue) * t + new FVector3(new Fixed32(0), yOffset, new Fixed32(0));
            Target.Position = newValue;
        }
    }
}
