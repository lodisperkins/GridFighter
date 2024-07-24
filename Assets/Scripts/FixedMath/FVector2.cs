using Types;
using System;
using UnityEngine;

namespace FixedPoints
{
    [Serializable]
    public struct FVector2
    {
        [SerializeField] private Fixed32 _x;
        [SerializeField] private Fixed32 _y;

        /// <summary>
        /// Gets or sets the X component of the vector.
        /// </summary>
        public Fixed32 X
        {
            get { return _x; }
            set { _x = value; }
        }

        /// <summary>
        /// Gets or sets the Y component of the vector.
        /// </summary>
        public Fixed32 Y
        {
            get { return _y; }
            set { _y = value; }
        }

        /// <summary>
        /// Initializes a new instance of the FVector2 struct with the specified components.
        /// </summary>
        /// <param name="x">The X component.</param>
        /// <param name="y">The Y component.</param>
        public FVector2(Fixed32 x, Fixed32 y)
        {
            _x = x;
            _y = y;
        }

        /// <summary>
        /// Gets the magnitude (length) of the vector.
        /// </summary>
        public Fixed32 Magnitude => (Fixed32)Math.Sqrt((double)(X * X + Y * Y));

        /// <summary>
        /// Gets the squared magnitude (length) of the vector.
        /// </summary>
        public Fixed32 SqrMagnitude => X * X + Y * Y;

        /// <summary>
        /// Changes the vector to have a magnitude of one.
        /// </summary>
        public void Normalize()
        {
            Fixed32 magnitude = Magnitude;
            if (magnitude.RawValue == 0) return;
            X /= magnitude;
            Y /= magnitude;
        }

        /// <summary>
        /// Returns a new vector with a magnitude of one, without changing the original vector.
        /// </summary>
        /// <returns>A normalized vector.</returns>
        public FVector2 GetNormalized()
        {
            Fixed32 magnitude = Magnitude;
            if (magnitude.RawValue == 0) return new FVector2();
            return new FVector2(X / magnitude, Y / magnitude);
        }

        /// <summary>
        /// Calculates the dot product of two vectors.
        /// </summary>
        /// <param name="lhs">The left-hand vector.</param>
        /// <param name="rhs">The right-hand vector.</param>
        /// <returns>The dot product.</returns>
        public static Fixed32 Dot(FVector2 lhs, FVector2 rhs) => lhs.X * rhs.X + lhs.Y * rhs.Y;

        /// <summary>
        /// Calculates the distance between two vectors.
        /// </summary>
        /// <param name="lhs">The first vector.</param>
        /// <param name="rhs">The second vector.</param>
        /// <returns>The distance between the vectors.</returns>
        public static Fixed32 Distance(FVector2 lhs, FVector2 rhs) => (lhs - rhs).Magnitude;

        /// <summary>
        /// Linearly interpolates between two vectors by a given factor.
        /// </summary>
        /// <param name="a">The start vector.</param>
        /// <param name="b">The end vector.</param>
        /// <param name="t">The interpolation factor.</param>
        /// <returns>The interpolated vector.</returns>
        public static FVector2 Lerp(FVector2 a, FVector2 b, Fixed32 t)
        {
            t = Clamp01(t);
            return new FVector2(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);
        }

        /// <summary>
        /// Moves a vector towards a target vector by a given maximum distance.
        /// </summary>
        /// <param name="current">The current vector.</param>
        /// <param name="target">The target vector.</param>
        /// <param name="maxDistanceDelta">The maximum distance to move.</param>
        /// <returns>The moved vector.</returns>
        public static FVector2 MoveTowards(FVector2 current, FVector2 target, Fixed32 maxDistanceDelta)
        {
            FVector2 toVector = target - current;
            Fixed32 dist = toVector.Magnitude;
            if (dist <= maxDistanceDelta || dist == 0) return target;
            return current + toVector / dist * maxDistanceDelta;
        }

        /// <summary>
        /// Scales a vector by another vector.
        /// </summary>
        /// <param name="a">The first vector.</param>
        /// <param name="b">The second vector.</param>
        /// <returns>The scaled vector.</returns>
        public static FVector2 Scale(FVector2 a, FVector2 b) => new FVector2(a.X * b.X, a.Y * b.Y);

        /// <summary>
        /// Scales the vector by another vector.
        /// </summary>
        /// <param name="scale">The scale vector.</param>
        public void Scale(FVector2 scale)
        {
            X *= scale.X;
            Y *= scale.Y;
        }

        /// <summary>
        /// Reflects a vector off a normal.
        /// </summary>
        /// <param name="inDirection">The vector to reflect.</param>
        /// <param name="inNormal">The normal vector to reflect off.</param>
        /// <returns>The reflected vector.</returns>
        public static FVector2 Reflect(FVector2 inDirection, FVector2 inNormal)
        {
            return inDirection - 2 * Dot(inNormal, inDirection) * inNormal;
        }

        /// <summary>
        /// Returns a perpendicular vector.
        /// </summary>
        /// <param name="inDirection">The input vector.</param>
        /// <returns>The perpendicular vector.</returns>
        public static FVector2 Perpendicular(FVector2 inDirection) => new FVector2(-inDirection.Y, inDirection.X);

        /// <summary>
        /// Calculates the angle between two vectors.
        /// </summary>
        /// <param name="from">The starting vector.</param>
        /// <param name="to">The ending vector.</param>
        /// <returns>The angle in degrees.</returns>
        public static Fixed32 Angle(FVector2 from, FVector2 to)
        {
            Fixed32 denominator = (Fixed32)Math.Sqrt((double)(from.SqrMagnitude * to.SqrMagnitude));
            if (denominator.RawValue < Fixed32.Epsilon)
                return (Fixed32)0;

            Fixed32 dot = Clamp(Dot(from, to) / denominator, (Fixed32)(-1), (Fixed32)1);
            return (Fixed32)(Math.Acos((double)dot) * (180.0 / Math.PI));
        }

        /// <summary>
        /// Calculates the signed angle between two vectors.
        /// </summary>
        /// <param name="from">The starting vector.</param>
        /// <param name="to">The ending vector.</param>
        /// <returns>The signed angle in degrees.</returns>
        public static Fixed32 SignedAngle(FVector2 from, FVector2 to)
        {
            Fixed32 unsignedAngle = Angle(from, to);
            Fixed32 crossZ = from.X * to.Y - from.Y * to.X;
            return crossZ.RawValue < 0 ? -unsignedAngle : unsignedAngle;
        }

        /// <summary>
        /// Clamps a value between a minimum and a maximum value.
        /// </summary>
        /// <param name="value">The value to clamp.</param>
        /// <param name="min">The minimum value.</param>
        /// <param name="max">The maximum value.</param>
        /// <returns>The clamped value.</returns>
        public static Fixed32 Clamp(Fixed32 value, Fixed32 min, Fixed32 max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        /// <summary>
        /// Clamps a value between 0 and 1.
        /// </summary>
        /// <param name="value">The value to clamp.</param>
        /// <returns>The clamped value.</returns>
        public static Fixed32 Clamp01(Fixed32 value)
        {
            if (value < (Fixed32)0) return (Fixed32)0;
            if (value > (Fixed32)1) return (Fixed32)1;
            return value;
        }

        /// <summary>
        /// Adds two vectors.
        /// </summary>
        /// <param name="lhs">The first vector.</param>
        /// <param name="rhs">The second vector.</param>
        /// <returns>The sum of the vectors.</returns>
        public static FVector2 operator +(FVector2 lhs, FVector2 rhs) => new FVector2(lhs.X + rhs.X, lhs.Y + rhs.Y);

        /// <summary>
        /// Subtracts one vector from another.
        /// </summary>
        /// <param name="lhs">The first vector.</param>
        /// <param name="rhs">The second vector.</param>
        /// <returns>The difference of the vectors.</returns>
        public static FVector2 operator -(FVector2 lhs, FVector2 rhs) => new FVector2(lhs.X - rhs.X, lhs.Y - rhs.Y);

        /// <summary>
        /// Subtracts one vector from another.
        /// </summary>
        /// <param name="lhs">The first vector.</param>
        /// <param name="rhs">The second vector.</param>
        /// <returns>The difference of the vectors.</returns>
        public static FVector2 operator -(FVector2 lhs) => new FVector2(-lhs.X, -lhs.Y);

        /// <summary>
        /// Multiplies a vector by a scalar.
        /// </summary>
        /// <param name="lhs">The vector.</param>
        /// <param name="scalar">The scalar.</param>
        /// <returns>The scaled vector.</returns>
        public static FVector2 operator *(FVector2 lhs, Fixed32 scalar) => new FVector2(lhs.X * scalar, lhs.Y * scalar);

        /// <summary>
        /// Multiplies a vector by a scalar.
        /// </summary>
        /// <param name="lhs">The vector.</param>
        /// <param name="scalar">The scalar.</param>
        /// <returns>The scaled vector.</returns>
        public static FVector2 operator *(Fixed32 scalar, FVector2 lhs) => new FVector2(lhs.X * scalar, lhs.Y * scalar);

        /// <summary>
        /// Divides a vector by a scalar.
        /// </summary>
        /// <param name="lhs">The vector.</param>
        /// <param name="scalar">The scalar.</param>
        /// <returns>The scaled vector.</returns>
        public static FVector2 operator /(FVector2 lhs, Fixed32 scalar) => new FVector2(lhs.X / scalar, lhs.Y / scalar);

        /// <summary>
        /// Checks if two vectors are equal.
        /// </summary>
        /// <param name="lhs">The first vector.</param>
        /// <param name="rhs">The second vector.</param>
        /// <returns>True if the vectors are equal, false otherwise.</returns>
        public static bool operator ==(FVector2 lhs, FVector2 rhs) => lhs.X == rhs.X && lhs.Y == rhs.Y;

        /// <summary>
        /// Checks if two vectors are not equal.
        /// </summary>
        /// <param name="lhs">The first vector.</param>
        /// <param name="rhs">The second vector.</param>
        /// <returns>True if the vectors are not equal, false otherwise.</returns>
        public static bool operator !=(FVector2 lhs, FVector2 rhs) => !(lhs == rhs);

        /// <summary>
        /// Checks if the current vector is equal to another object.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        /// <returns>True if the object is a vector and is equal to the current vector, false otherwise.</returns>
        public override bool Equals(object obj)
        {
            if (!(obj is FVector2)) return false;
            FVector2 v = (FVector2)obj;
            return X == v.X && Y == v.Y;
        }

        /// <summary>
        /// Gets the hash code of the vector.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + X.GetHashCode();
                hash = hash * 23 + Y.GetHashCode();
                return hash;
            }
        }

        /// <summary>
        /// Returns a string representation of the vector.
        /// </summary>
        /// <returns>A string representing the vector.</returns>
        public override string ToString() => $"({X}, {Y})";

        /// <summary>
        /// Gets a vector with both components set to zero.
        /// </summary>
        public static FVector2 Zero => new FVector2(0f, 0f);

        /// <summary>
        /// Gets a vector with both components set to one.
        /// </summary>
        public static FVector2 One => new FVector2(1f, 1f);

        /// <summary>
        /// Gets a vector pointing up (0, 1).
        /// </summary>
        public static FVector2 Up => new FVector2(0f, 1f);

        /// <summary>
        /// Gets a vector pointing down (0, -1).
        /// </summary>
        public static FVector2 Down => new FVector2(0f, -1f);

        /// <summary>
        /// Gets a vector pointing left (-1, 0).
        /// </summary>
        public static FVector2 Left => new FVector2(-1f, 0f);

        /// <summary>
        /// Gets a vector pointing right (1, 0).
        /// </summary>
        public static FVector2 Right => new FVector2(1f, 0f);


        /// <summary>
        /// Converts a Unity Vector3 to a FVector3.
        /// </summary>
        /// <param name="vector">The Unity Vector3.</param>
        public static explicit operator FVector2(Vector2 vector)
        {
            return new FVector2((Fixed32)vector.x, (Fixed32)vector.y);
        }

        /// <summary>
        /// Converts a FVector3 to a Unity Vector3.
        /// </summary>
        /// <param name="vector">The FVector3.</param>
        public static explicit operator Vector2(FVector2 vector)
        {
            return new Vector2((float)(double)vector.X, (float)(double)vector.Y);
        }
    }
}
