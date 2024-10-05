using System;
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
    [System.Serializable]
    public struct Fixed32
    {
        public const int Epsilon = 1;
        private const int FractionMask = 0xffff;
        public static Fixed32 PI = (Fixed32)3.1415926535897932384626433832795;
        private const int DefaultScale = 16;
        private int _scale;

        public long RawValue;
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


        public Fixed32(int rawValue)
        {
            RawValue = rawValue;
            _scale = DefaultScale;
        }

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

        public int Sign()
        {
            if (RawValue > 0)
                return 1;
            else if (RawValue < 0)
                return -1;
            else
                return 0;
        }

        public static Fixed32 Abs(Fixed32 value)
        {
            Fixed32 result = value < new Fixed32(0) ? -value : value;
            return result;
        }

        // Square Root function for Fixed32
        public static Fixed32 Sqrt(Fixed32 value)
        {
            if (value.RawValue < 0)
                throw new ArgumentException("Square root of negative number is not defined for Fixed32.");

            if (value.RawValue == 0)
                return new Fixed32(0);  // The square root of 0 is 0.

            // Initial guess (using value/2 as a rough estimate)
            Fixed32 guess = value / 2;


            // Newton's method iteration (converges quickly)
            for (int i = 0; i < 10; i++)  // 10 iterations should be sufficient
            {
                Fixed32 nextGuess = (guess + value / guess) / 2;

                // If the result has converged, stop iterating
                if (nextGuess.RawValue == guess.RawValue)
                    break;

                guess = nextGuess;
            }

            return guess;
        }

        public static Fixed32 Round(Fixed32 value)
        {
            Fixed32 half = (Fixed32)0.5f;  // Representing 0.5 in Fixed32
            Fixed32 roundedValue;

            // If the value is positive, add 0.5 and truncate.
            if (value >= 0)
            {
                roundedValue = value + half;
            }
            // If the value is negative, subtract 0.5 and truncate.
            else
            {
                roundedValue = value - half;
            }

            // Truncate the fractional part by shifting to the right, equivalent to Mathf.Floor for positive numbers.
            return new Fixed32(roundedValue.WholeNumber << value.Scale);
        }



        public static Fixed32 Clamp(Fixed32 value, Fixed32 a, Fixed32 b)
        {
            if (value < a)
            {
                value = a;
            }
            else if (value > b)
            {
                value = b;
            }

            return value;   
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

        public static Fixed32 operator -(Fixed32 num)
        {
            num.RawValue = -num.RawValue;
            return num;
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

        public static Fixed32 Acos(Fixed32 x)
        {
            // Ensure the input is within the valid range [-1, 1]
            if (x < -1 || x > 1)
                throw new ArgumentOutOfRangeException("x", "Acos input must be between -1 and 1");

            // Coefficients for the approximation of acos
            // Polynomial approximation for acos(x) when x is in [-1, 1]
            Fixed32 c1 = new Fixed32(102939);   // approximately 1.5707288
            Fixed32 c2 = new Fixed32(-13901);  // approximately -0.2121144
            Fixed32 c3 = new Fixed32(4866);    // approximately 0.0742610
            Fixed32 c4 = new Fixed32(-1227);    // approximately -0.0187293

            // Calculate the square root of (1 - x^2)
            Fixed32 sqrtTerm = Fixed32.Sqrt(1 - x);

            // Polynomial approximation
            Fixed32 result = c1 + (x * (c2 + (x * (c3 + (x * c4)))));

            return sqrtTerm * result;
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
