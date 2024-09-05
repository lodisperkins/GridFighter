

using System;
using System.IO;
using Types;
using UnityEngine;
using static PixelCrushers.DialogueSystem.ActOnDialogueEvent;

namespace FixedPoints
{
    [Serializable]
    public struct FQuaternion
    {
        public Fixed32 X;
        public Fixed32 Y;
        public Fixed32 Z;
        public Fixed32 W;

        public FQuaternion(Fixed32 x, Fixed32 y, Fixed32 z, Fixed32 w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public static FQuaternion Identity => new FQuaternion(0, 0, 0, 1);

        public static FQuaternion Euler(FVector3 eulerAngles)
        {
            // Convert eulerAngles to radians
            var x = eulerAngles.X * Fixed32.PI / 180;
            var y = eulerAngles.Y * Fixed32.PI / 180;
            var z = eulerAngles.Z * Fixed32.PI / 180;

            var cx = Fixed32.Cos(x / 2);
            var sx = Fixed32.Sin(x / 2);
            var cy = Fixed32.Cos(y / 2);
            var sy = Fixed32.Sin(y / 2);
            var cz = Fixed32.Cos(z / 2);
            var sz = Fixed32.Sin(z / 2);

            FQuaternion result = new FQuaternion(
                sx * cy * cz - cx * sy * sz,
                cx * sy * cz + sx * cy * sz,
                cx * cy * sz - sx * sy * cz,
                cx * cy * cz + sx * sy * sz
            );

            result.Normalize();
            return result;
        }

        public static FQuaternion Euler(Fixed32 angleX, Fixed32 angleY, Fixed32 angleZ)
        {
            // Convert eulerAngles to radians
            var x = angleX * Fixed32.PI / 180f;
            var y = angleY * Fixed32.PI / 180f;
            var z = angleZ * Fixed32.PI / 180f;

            var cx = Fixed32.Cos(x / 2);
            var sx = Fixed32.Sin(x / 2);
            var cy = Fixed32.Cos(y / 2);
            var sy = Fixed32.Sin(y / 2);
            var cz = Fixed32.Cos(z / 2);
            var sz = Fixed32.Sin(z / 2);

            FQuaternion result = new FQuaternion(
                sx * cy * cz - cx * sy * sz,
                cx * sy * cz + sx * cy * sz,
                cx * cy * sz - sx * sy * cz,
                cx * cy * cz + sx * sy * sz
            );

            result.Normalize();
            return result;
        }

        public static FQuaternion LookRotation(FVector3 forward, FVector3 up)
        {
            forward = forward.GetNormalized();
            FVector3 right = FVector3.Cross(up, forward).GetNormalized();
            up = FVector3.Cross(forward, right);

            Fixed32 m00 = right.X;
            Fixed32 m01 = right.Y;
            Fixed32 m02 = right.Z;
            Fixed32 m10 = up.X;
            Fixed32 m11 = up.Y;
            Fixed32 m12 = up.Z;
            Fixed32 m20 = forward.X;
            Fixed32 m21 = forward.Y;
            Fixed32 m22 = forward.Z;

            Fixed32 num8 = (m00 + m11) + m22;
            FQuaternion quaternion = new FQuaternion();
            if (num8 > 0f)
            {
                Fixed32 num = (Fixed32)Math.Sqrt((double)(num8 + 1f));
                quaternion.W = num * 0.5f;
                num = 0.5f / num;
                quaternion.X = (m12 - m21) * num;
                quaternion.Y = (m20 - m02) * num;
                quaternion.Z = (m01 - m10) * num;
                return quaternion;
            }
            if ((m00 >= m11) && (m00 >= m22))
            {
                Fixed32 num7 = (Fixed32)Math.Sqrt((double)(((1f + m00) - m11) - m22));
                Fixed32 num4 = 0.5f / num7;
                quaternion.X = 0.5f * num7;
                quaternion.Y = (m01 + m10) * num4;
                quaternion.Z = (m02 + m20) * num4;
                quaternion.W = (m12 - m21) * num4;
                return quaternion;
            }
            if (m11 > m22)
            {
                Fixed32 num6 = (Fixed32)Math.Sqrt((double)(((1f + m11) - m00) - m22));
                Fixed32 num3 = 0.5f / num6;
                quaternion.X = (m10 + m01) * num3;
                quaternion.Y = 0.5f * num6;
                quaternion.Z = (m21 + m12) * num3;
                quaternion.W = (m20 - m02) * num3;
                return quaternion;
            }
            Fixed32 num5 = (Fixed32)Math.Sqrt((double)(((1f + m22) - m00) - m11));
            Fixed32 num2 = 0.5f / num5;
            quaternion.X = (m20 + m02) * num2;
            quaternion.Y = (m21 + m12) * num2;
            quaternion.Z = 0.5f * num5;
            quaternion.W = (m01 - m10) * num2;
            return quaternion;
        }

        public static FQuaternion operator *(FQuaternion lhs, FQuaternion rhs)
        {
            return new FQuaternion(
                lhs.W * rhs.X + lhs.X * rhs.W + lhs.Y * rhs.Z - lhs.Z * rhs.Y,
                lhs.W * rhs.Y - lhs.X * rhs.Z + lhs.Y * rhs.W + lhs.Z * rhs.X,
                lhs.W * rhs.Z + lhs.X * rhs.Y - lhs.Y * rhs.X + lhs.Z * rhs.W,
                lhs.W * rhs.W - lhs.X * rhs.X - lhs.Y * rhs.Y - lhs.Z * rhs.Z
            );
        }

        public static FVector3 operator *(FQuaternion rotation, FVector3 point)
        {
            var x2 = rotation.X + rotation.X;
            var y2 = rotation.Y + rotation.Y;
            var z2 = rotation.Z + rotation.Z;

            var xx2 = rotation.X * x2;
            var yy2 = rotation.Y * y2;
            var zz2 = rotation.Z * z2;
            var xy2 = rotation.X * y2;
            var xz2 = rotation.X * z2;
            var yz2 = rotation.Y * z2;
            var wx2 = rotation.W * x2;
            var wy2 = rotation.W * y2;
            var wz2 = rotation.W * z2;

            return new FVector3(
                (1 - (yy2 + zz2)) * point.X + (xy2 - wz2) * point.Y + (xz2 + wy2) * point.Z,
                (xy2 + wz2) * point.X + (1 - (xx2 + zz2)) * point.Y + (yz2 - wx2) * point.Z,
                (xz2 - wy2) * point.X + (yz2 + wx2) * point.Y + (1 - (xx2 + yy2)) * point.Z
            );
        }
        public static FQuaternion Lerp(FQuaternion a, FQuaternion b, Fixed32 t)
        {
            // Ensure the interpolation is within range
            t = t < 0 ? 0 : (t > 1 ? 1 : t);

            // Perform linear interpolation
            var result = new FQuaternion(
                a.X + (b.X - a.X) * t,
                a.Y + (b.Y - a.Y) * t,
                a.Z + (b.Z - a.Z) * t,
                a.W + (b.W - a.W) * t
            );

            // Normalize the result to ensure it's a valid quaternion
            result.Normalize();
            return result;
        }
        public void Normalize()
        {
            var magnitude = (Fixed32)Math.Sqrt((double)(X * X + Y * Y + Z * Z + W * W));
            if (magnitude > 0)
            {
                X /= magnitude;
                Y /= magnitude;
                Z /= magnitude;
                W /= magnitude;
            }
        }

        public static FQuaternion Inverse(FQuaternion rotation)
        {
            return new FQuaternion(-rotation.X, -rotation.Y, -rotation.Z, rotation.W);
        }

        // Explicit cast to UnityEngine.Quaternion
        public static explicit operator UnityEngine.Quaternion(FQuaternion fq)
        {
            return new UnityEngine.Quaternion((float)fq.X, (float)fq.Y, (float)fq.Z, (float)fq.W);
        }

        // Explicit cast to UnityEngine.Quaternion
        public static explicit operator FQuaternion(UnityEngine.Quaternion uq)
        {
            return new FQuaternion(uq.x, uq.y, uq.z, uq.w);
        }

        public void Serialize(BinaryWriter bw)
        {
            X.Serialize(bw);
            Y.Serialize(bw);
            Z.Serialize(bw);
            W.Serialize(bw);
        }

        public void Deserialize(BinaryReader br)
        {
            X.Deserialize(br);
            Y.Deserialize(br);
            Z.Deserialize(br);
            W.Deserialize(br);
        }
    }
}