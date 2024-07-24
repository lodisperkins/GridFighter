using System;
using Types;
using UnityEngine;


namespace FixedPoints
{
    public struct FVector3
    {
        private Fixed32 _x;
        private Fixed32 _y;
        private Fixed32 _z;

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
        /// Gets or sets the Z component of the vector.
        /// </summary>
        public Fixed32 Z
        {
            get { return _z; }
            set { _z = value; }
        }

        /// <summary>
        /// Initializes a new instance of the FVector3 struct with the specified components.
        /// </summary>
        /// <param name="x">The X component.</param>
        /// <param name="y">The Y component.</param>
        /// <param name="z">The Z component.</param>
        public FVector3(Fixed32 x, Fixed32 y, Fixed32 z)
        {
            _x = x;
            _y = y;
            _z = z;
        }

        /// <summary>
        /// Gets the magnitude (length) of the vector.
        /// </summary>
        public Fixed32 Magnitude => (Fixed32)Math.Sqrt((double)(X * X + Y * Y + Z * Z));

        /// <summary>
        /// Gets the squared magnitude (length) of the vector.
        /// </summary>
        public Fixed32 SqrMagnitude => X * X + Y * Y + Z * Z;

        /// <summary>
        /// Changes the vector to have a magnitude of one.
        /// </summary>
        public void Normalize()
        {
            Fixed32 magnitude = Magnitude;
            if (magnitude.RawValue == 0) return;
            X /= magnitude;
            Y /= magnitude;
            Z /= magnitude;
        }

        /// <summary>
        /// Returns a new vector with a magnitude of one, without changing the original vector.
        /// </summary>
        /// <returns>A normalized vector.</returns>
        public FVector3 GetNormalized()
        {
            Fixed32 magnitude = Magnitude;
            if (magnitude.RawValue == 0) return new FVector3();
            return new FVector3(X / magnitude, Y / magnitude, Z / magnitude);
        }

        /// <summary>
        /// Calculates the dot product of two vectors.
        /// </summary>
        /// <param name="lhs">The left-hand vector.</param>
        /// <param name="rhs">The right-hand vector.</param>
        /// <returns>The dot product.</returns>
        public static Fixed32 Dot(FVector3 lhs, FVector3 rhs) => lhs.X * rhs.X + lhs.Y * rhs.Y + lhs.Z * rhs.Z;

        /// <summary>
        /// Calculates the cross product of two vectors.
        /// </summary>
        /// <param name="lhs">The left-hand vector.</param>
        /// <param name="rhs">The right-hand vector.</param>
        /// <returns>The cross product vector.</returns>
        public static FVector3 Cross(FVector3 lhs, FVector3 rhs)
        {
            return new FVector3(
                lhs.Y * rhs.Z - lhs.Z * rhs.Y,
                lhs.Z * rhs.X - lhs.X * rhs.Z,
                lhs.X * rhs.Y - lhs.Y * rhs.X
            );
        }

        /// <summary>
        /// Calculates the distance between two vectors.
        /// </summary>
        /// <param name="lhs">The first vector.</param>
        /// <param name="rhs">The second vector.</param>
        /// <returns>The distance between the vectors.</returns>
        public static Fixed32 Distance(FVector3 lhs, FVector3 rhs) => (lhs - rhs).Magnitude;

        /// <summary>
        /// Linearly interpolates between two vectors by a given factor.
        /// </summary>
        /// <param name="a">The start vector.</param>
        /// <param name="b">The end vector.</param>
        /// <param name="t">The interpolation factor.</param>
        /// <returns>The interpolated vector.</returns>
        public static FVector3 Lerp(FVector3 a, FVector3 b, Fixed32 t)
        {
            t = Clamp01(t);
            return new FVector3(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t, a.Z + (b.Z - a.Z) * t);
        }

        /// <summary>
        /// Moves a vector towards a target vector by a given maximum distance.
        /// </summary>
        /// <param name="current">The current vector.</param>
        /// <param name="target">The target vector.</param>
        /// <param name="maxDistanceDelta">The maximum distance to move.</param>
        /// <returns>The moved vector.</returns>
        public static FVector3 MoveTowards(FVector3 current, FVector3 target, Fixed32 maxDistanceDelta)
        {
            FVector3 toVector = target - current;
            Fixed32 dist = toVector.Magnitude;
            if (dist <= maxDistanceDelta || dist == 0) return target;
            return current + toVector / dist * maxDistanceDelta;
        }

        /// <summary>
        /// Projects a vector onto another vector.
        /// </summary>
        /// <param name="vector">The vector to project.</param>
        /// <param name="onNormal">The vector to project onto.</param>
        /// <returns>The projected vector.</returns>
        public static FVector3 Project(FVector3 vector, FVector3 onNormal)
        {
            Fixed32 sqrMag = Dot(onNormal, onNormal);
            if (sqrMag.RawValue < Fixed32.Epsilon) return Zero;
            Fixed32 dot = Dot(vector, onNormal);
            return new FVector3(onNormal.X * dot / sqrMag, onNormal.Y * dot / sqrMag, onNormal.Z * dot / sqrMag);
        }

        /// <summary>
        /// Projects a vector onto a plane defined by a normal.
        /// </summary>
        /// <param name="vector">The vector to project.</param>
        /// <param name="planeNormal">The normal of the plane.</param>
        /// <returns>The projected vector.</returns>
        public static FVector3 ProjectOnPlane(FVector3 vector, FVector3 planeNormal)
        {
            return vector - Project(vector, planeNormal);
        }

        /// <summary>
        /// Reflects a vector off a normal.
        /// </summary>
        /// <param name="inDirection">The vector to reflect.</param>
        /// <param name="inNormal">The normal vector to reflect off.</param>
        /// <returns>The reflected vector.</returns>
        public static FVector3 Reflect(FVector3 inDirection, FVector3 inNormal)
        {
            return inDirection - 2 * Dot(inNormal, inDirection) * inNormal;
        }

        /// <summary>
        /// Returns a perpendicular vector.
        /// </summary>
        /// <param name="inDirection">The input vector.</param>
        /// <returns>The perpendicular vector.</returns>
        public static FVector3 Perpendicular(FVector3 inDirection) => new FVector3(-inDirection.Y, inDirection.X, inDirection.Z);

        /// <summary>
        /// Calculates the angle between two vectors.
        /// </summary>
        /// <param name="from">The starting vector.</param>
        /// <param name="to">The ending vector.</param>
        /// <returns>The angle in degrees.</returns>
        public static Fixed32 Angle(FVector3 from, FVector3 to)
        {
            Fixed32 denominator = (Fixed32)Math.Sqrt((double)(from.SqrMagnitude * to.SqrMagnitude));
            if (denominator.RawValue < Fixed32.Epsilon)
                return (Fixed32)0;

            Fixed32 dot = Clamp(Dot(from, to) / denominator, (Fixed32)(-1), (Fixed32)1);
            return (Fixed32)(Math.Acos((double)dot) * (180.0 / Math.PI));
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
        public static FVector3 operator +(FVector3 lhs, FVector3 rhs) => new FVector3(lhs.X + rhs.X, lhs.Y + rhs.Y, lhs.Z + rhs.Z);

        /// <summary>
        /// Subtracts one vector from another.
        /// </summary>
        /// <param name="lhs">The first vector.</param>
        /// <param name="rhs">The second vector.</param>
        /// <returns>The difference of the vectors.</returns>
        public static FVector3 operator -(FVector3 lhs, FVector3 rhs) => new FVector3(lhs.X - rhs.X, lhs.Y - rhs.Y, lhs.Z - rhs.Z);

        /// <summary>
        /// Subtracts one vector from another.
        /// </summary>
        /// <param name="lhs">The first vector.</param>
        /// <param name="rhs">The second vector.</param>
        /// <returns>The difference of the vectors.</returns>
        public static FVector3 operator -(FVector3 lhs) => new FVector3(-lhs.X, -lhs.Y, -lhs.Z);

        /// <summary>
        /// Multiplies a vector by a scalar.
        /// </summary>
        /// <param name="lhs">The vector.</param>
        /// <param name="scalar">The scalar.</param>
        /// <returns>The scaled vector.</returns>
        public static FVector3 operator *(FVector3 lhs, Fixed32 scalar) => new FVector3(lhs.X * scalar, lhs.Y * scalar, lhs.Z * scalar);

        /// <summary>
        /// Multiplies a vector by a scalar.
        /// </summary>
        /// <param name="lhs">The vector.</param>
        /// <param name="scalar">The scalar.</param>
        /// <returns>The scaled vector.</returns>
        public static FVector3 operator *(Fixed32 scalar, FVector3 lhs) => new FVector3(lhs.X * scalar, lhs.Y * scalar, lhs.Z * scalar);

        /// <summary>
        /// Divides a vector by a scalar.
        /// </summary>
        /// <param name="lhs">The vector.</param>
        /// <param name="scalar">The scalar.</param>
        /// <returns>The scaled vector.</returns>
        public static FVector3 operator /(FVector3 lhs, Fixed32 scalar) => new FVector3(lhs.X / scalar, lhs.Y / scalar, lhs.Z / scalar);

        /// <summary>
        /// Checks if two vectors are equal.
        /// </summary>
        /// <param name="lhs">The first vector.</param>
        /// <param name="rhs">The second vector.</param>
        /// <returns>True if the vectors are equal, false otherwise.</returns>
        public static bool operator ==(FVector3 lhs, FVector3 rhs) => lhs.X == rhs.X && lhs.Y == rhs.Y && lhs.Z == rhs.Z;

        /// <summary>
        /// Checks if two vectors are not equal.
        /// </summary>
        /// <param name="lhs">The first vector.</param>
        /// <param name="rhs">The second vector.</param>
        /// <returns>True if the vectors are not equal, false otherwise.</returns>
        public static bool operator !=(FVector3 lhs, FVector3 rhs) => !(lhs == rhs);

        /// <summary>
        /// Checks if the current vector is equal to another object.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        /// <returns>True if the object is a vector and is equal to the current vector, false otherwise.</returns>
        public override bool Equals(object obj)
        {
            if (!(obj is FVector3)) return false;
            FVector3 v = (FVector3)obj;
            return X == v.X && Y == v.Y && Z == v.Z;
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
                hash = hash * 23 + Z.GetHashCode();
                return hash;
            }
        }

        /// <summary>
        /// Returns a string representation of the vector.
        /// </summary>
        /// <returns>A string representing the vector.</returns>
        public override string ToString() => $"({X}, {Y}, {Z})";

        /// <summary>
        /// Gets a vector with all components set to zero.
        /// </summary>
        public static FVector3 Zero => new FVector3(0f, 0f, 0f);

        /// <summary>
        /// Gets a vector with all components set to one.
        /// </summary>
        public static FVector3 One => new FVector3(1f, 1f, 1f);

        /// <summary>
        /// Gets a vector pointing up (0, 1, 0).
        /// </summary>
        public static FVector3 Up => new FVector3(0f, 1f, 0f);

        /// <summary>
        /// Gets a vector pointing down (0, -1, 0).
        /// </summary>
        public static FVector3 Down => new FVector3(0f, -1f, 0f);

        /// <summary>
        /// Gets a vector pointing left (-1, 0, 0).
        /// </summary>
        public static FVector3 Left => new FVector3(-1f, 0f, 0f);

        /// <summary>
        /// Gets a vector pointing right (1, 0, 0).
        /// </summary>
        public static FVector3 Right => new FVector3(1f, 0f, 0f);

        /// <summary>
        /// Gets a vector pointing forward (0, 0, 1).
        /// </summary>
        public static FVector3 Forward => new FVector3(0f, 0f, 1f);

        /// <summary>
        /// Gets a vector pointing backward (0, 0, -1).
        /// </summary>
        public static FVector3 Back => new FVector3(0f, 0f, -1f);

        /// <summary>
        /// Converts a Unity Vector3 to a FVector3.
        /// </summary>
        /// <param name="vector">The Unity Vector3.</param>
        public static explicit operator FVector3(Vector3 vector)
        {
            return new FVector3((Fixed32)vector.x, (Fixed32)vector.y, (Fixed32)vector.z);
        }

        /// <summary>
        /// Converts a FVector3 to a Unity Vector3.
        /// </summary>
        /// <param name="vector">The FVector3.</param>
        public static explicit operator Vector3(FVector3 vector)
        {
            return new Vector3((float)(double)vector.X, (float)(double)vector.Y, (float)(double)vector.Z);
        }

        public static implicit operator FVector3(FVector2 vector)
        {
            return new FVector3(vector.X, vector.Y, (Fixed32)0);
        }

        public static implicit operator FVector2(FVector3 vector)
        {
            return new FVector2(vector.X, vector.Y);
        }

    }
}