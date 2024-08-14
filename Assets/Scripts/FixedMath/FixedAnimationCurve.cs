using System.Collections.Generic;
using System;
using Types;
using UnityEngine;

namespace FixedPoints
{
    public class FixedAnimationCurve
    {
        // List to store keyframes
        private List<Keyframe> keyframes;

        // Constructor that initializes the curve using a Unity AnimationCurve
        public FixedAnimationCurve(AnimationCurve unityCurve)
        {
            keyframes = new List<Keyframe>();
            foreach (var key in unityCurve.keys)
            {
                // Add each keyframe from the Unity curve to the fixed-point curve
                AddKey((Fixed32)key.time, (Fixed32)key.value);
            }
        }

        // Method to add a keyframe to the curve
        public void AddKey(Fixed32 time, Fixed32 value)
        {
            keyframes.Add(new Keyframe(time, value));
            // Ensure the keyframes are sorted by time
            keyframes.Sort((a, b) => a.Time.RawValue.CompareTo(b.Time.RawValue));
        }

        // Method to evaluate the curve at a given time
        public Fixed32 Evaluate(Fixed32 time)
        {
            if (keyframes.Count == 0)
                throw new InvalidOperationException("No keyframes in the animation curve.");

            if (time.RawValue <= keyframes[0].Time.RawValue)
                return keyframes[0].Value;
            if (time.RawValue >= keyframes[keyframes.Count - 1].Time.RawValue)
                return keyframes[keyframes.Count - 1].Value;

            // Find the two keyframes surrounding the given time
            Keyframe left = keyframes[0], right = keyframes[0];
            foreach (var key in keyframes)
            {
                if (key.Time.RawValue > time.RawValue)
                {
                    right = key;
                    break;
                }
                left = key;
            }

            // Perform linear interpolation between the two keyframes
            Fixed32 t = (time - left.Time) / (right.Time - left.Time);
            return left.Value + t * (right.Value - left.Value);
        }

        // Nested struct to represent a keyframe
        public struct Keyframe
        {
            public Fixed32 Time { get; private set; }
            public Fixed32 Value { get; private set; }

            public Keyframe(Fixed32 time, Fixed32 value)
            {
                Time = time;
                Value = value;
            }
        }
    }
}