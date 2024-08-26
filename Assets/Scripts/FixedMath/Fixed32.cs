using System.IO;
using UnityEngine;

namespace Types
{
    /// <summary>
    /// Struct for representing fixed-point numbers. This is useful in scenarios where
    /// floating-point precision is not desirable, such as in certain financial calculations
    /// or low-level graphics operations. The number is stored with a fixed number of fractional bits.
    /// Gotten from this repo here: https://github.com/stormmuller/fixed-point-types/blob/master/Types/Types/Fixed32.cs
    /// </summary>
    public struct Fixed32
    {
        public const int Epsilon = 1;
        private const int FractionMask = 0xffff;
        public static Fixed32 PI = (Fixed32)3.1415926535897932384626433832795;
        private const int DefaultScale = 16;
        private int _scale;

        public long RawValue { get; private set; }
        public int Scale 
        {
            get
            {
                if (_scale == 0)
                {
                    return DefaultScale;
                }

                return _scale;
            }
            private set
            {
                _scale = value;
            }
        }


        public Fixed32(int scale = DefaultScale) : this(scale, 0) { }

        public Fixed32(int scale, int wholeNumber)
        {
            _scale = scale;
            RawValue = wholeNumber << scale;
        }

        public int WholeNumber
        {
            get
            {
                return (int)(this.RawValue >> this.Scale) +
                    (this.RawValue < 0 && this.Fraction != 0 ? 1 : 0);
            }
        }

        public int Fraction
        {
            get
            {
                return (int)(this.RawValue & FractionMask);
            }
        }

        public void Serialize(BinaryWriter bw)
        {
            bw.Write(Scale);
            bw.Write(RawValue);
        }

        public void Deserialize(BinaryReader br)
        {
            RawValue = br.ReadInt64();
            Scale = br.Read();
        }

        public static Fixed32 operator +(Fixed32 leftHandSide, Fixed32 rightHandSide)
        {
            leftHandSide.RawValue += rightHandSide.RawValue;
            return leftHandSide;
        }

        public static Fixed32 operator -(Fixed32 leftHandSide, Fixed32 rightHandSide)
        {
            leftHandSide.RawValue -= rightHandSide.RawValue;
            return leftHandSide;
        }

        public static Fixed32 operator *(Fixed32 leftHandSide, Fixed32 rightHandSide)
        {
            var result = leftHandSide.RawValue * rightHandSide.RawValue;
            leftHandSide.RawValue = result >> leftHandSide.Scale;
            return leftHandSide;
        }

        public static Fixed32 operator /(Fixed32 leftHandSide, Fixed32 rightHandSide)
        {
            var result = (leftHandSide.RawValue << leftHandSide.Scale) / rightHandSide.RawValue;
            leftHandSide.RawValue = result;
            return leftHandSide;
        }

        public static explicit operator double(Fixed32 number)
        {
            return (double)number.RawValue / (1 << number.Scale);
        }

        public static implicit operator int(Fixed32 number)
        {
            return number.WholeNumber;
        }

        public static implicit operator Fixed32(int number)
        {
            return new Fixed32(DefaultScale, number);
        }

        public static implicit operator float(Fixed32 number) => (float)(double)number;

        public static implicit operator Fixed32(float number)
        {
            long rawValue = (long)(number * (1 << DefaultScale));
            return new Fixed32() { RawValue = rawValue };
        }

        public override string ToString()
        {
            return ((double)this).ToString();
        }

        // Trigonometric functions using Mathf approximations

        public static Fixed32 Sin(Fixed32 radians)
        {
            return (Fixed32)Mathf.Sin(radians);
        }

        public static Fixed32 Cos(Fixed32 radians)
        {
            return (Fixed32)Mathf.Cos(radians);
        }

        public static Fixed32 Tan(Fixed32 angle)
        {
            return Sin(angle) / Cos(angle);
        }

        // Overloading comparison operators
        public static bool operator <(Fixed32 left, Fixed32 right)
        {
            return left.RawValue < right.RawValue;
        }

        public static bool operator >(Fixed32 left, Fixed32 right)
        {
            return left.RawValue > right.RawValue;
        }

        public static bool operator <=(Fixed32 left, Fixed32 right)
        {
            return left.RawValue <= right.RawValue;
        }

        public static bool operator >=(Fixed32 left, Fixed32 right)
        {
            return left.RawValue >= right.RawValue;
        }

        public static bool operator ==(Fixed32 left, Fixed32 right)
        {
            return left.RawValue == right.RawValue;
        }

        public static bool operator !=(Fixed32 left, Fixed32 right)
        {
            return left.RawValue != right.RawValue;
        }

        // Override Equals and GetHashCode to align with == and !=
        public override bool Equals(object obj)
        {
            if (!(obj is Fixed32))
                return false;

            return this == (Fixed32)obj;
        }

        public override int GetHashCode()
        {
            return RawValue.GetHashCode();
        }
    }
}
