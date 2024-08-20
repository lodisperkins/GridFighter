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
        /// <summary>
        /// The smallest possible difference between two fixed-point numbers.
        /// </summary>
        public const int Epsilon = 1;

        /// <summary>
        /// Mask used to isolate the fractional part of the number.
        /// </summary>
        private const int FractionMask = 0xffff;

        /// <summary>
        /// A constant representing the mathematical constant π (Pi).
        /// </summary>
        public static Fixed32 PI = (Fixed32)3.1415926535897932384626433832795;

        /// <summary>
        /// Default scale factor, which defines the number of fractional bits.
        /// </summary>
        private const int DefaultScale = 16;

        /// <summary>
        /// The raw underlying value of the fixed-point number, where the lower bits represent
        /// the fractional part and the higher bits represent the whole number part.
        /// </summary>
        public long RawValue { get; private set; }

        /// <summary>
        /// The scale factor, indicating how many bits are used for the fractional part.
        /// </summary>
        public int Scale { get; private set; }

        /// <summary>
        /// Constructor that initializes the fixed-point number with a specific scale and a whole number of zero.
        /// </summary>
        /// <param name="scale">The number of bits to use for the fractional part.</param>
        public Fixed32(int scale) : this(scale, 0) { }

        /// <summary>
        /// Constructor that initializes the fixed-point number with a specific scale and a given whole number.
        /// </summary>
        /// <param name="scale">The number of bits to use for the fractional part.</param>
        /// <param name="wholeNumber">The initial whole number part of the fixed-point number.</param>
        public Fixed32(int scale, int wholeNumber)
        {
            // Set the scale factor.
            this.Scale = scale;

            // Store the whole number part, shifted by the scale to represent the fixed-point value.
            this.RawValue = wholeNumber << scale;
        }

        /// <summary>
        /// Gets the whole number part of the fixed-point number.
        /// </summary>
        public int WholeNumber
        {
            get
            {
                // Extract the whole number by shifting the raw value to the right by the scale factor.
                // Adjusts for negative values by adding 1 if there is a fractional component.
                return (int)(this.RawValue >> this.Scale) +
                    (this.RawValue < 0 && this.Fraction != 0 ? 1 : 0);
            }
        }

        /// <summary>
        /// Gets the fractional part of the fixed-point number, masked to remove the whole number bits.
        /// </summary>
        public int Fraction
        {
            get
            {
                // Isolate the fractional bits using a bitwise AND with the fraction mask.
                return (int)(this.RawValue & FractionMask);
            }
        }

        /// <summary>
        /// Serializes the fixed-point number to a binary writer, storing the scale and raw value.
        /// </summary>
        /// <param name="bw">The binary writer to serialize to.</param>
        public void Serialize(BinaryWriter bw)
        {
            bw.Write(Scale);
            bw.Write(RawValue);
        }

        /// <summary>
        /// Deserializes the fixed-point number from a binary reader, restoring the scale and raw value.
        /// </summary>
        /// <param name="br">The binary reader to deserialize from.</param>
        public void Deserialize(BinaryReader br)
        {
            RawValue = br.ReadInt64();
            Scale = br.Read();
        }

        /// <summary>
        /// Addition operator for adding two fixed-point numbers.
        /// </summary>
        public static Fixed32 operator +(Fixed32 leftHandSide, Fixed32 rightHandSide)
        {
            leftHandSide.RawValue += rightHandSide.RawValue;

            return leftHandSide;
        }

        /// <summary>
        /// Subtraction operator for subtracting one fixed-point number from another.
        /// </summary>
        public static Fixed32 operator -(Fixed32 leftHandSide, Fixed32 rightHandSide)
        {
            leftHandSide.RawValue -= rightHandSide.RawValue;

            return leftHandSide;
        }

        /// <summary>
        /// Multiplication operator for multiplying two fixed-point numbers.
        /// </summary>
        public static Fixed32 operator *(Fixed32 leftHandSide, Fixed32 rightHandSide)
        {
            var result = leftHandSide.RawValue * rightHandSide.RawValue;

            leftHandSide.RawValue = result >> leftHandSide.Scale;

            return leftHandSide;
        }

        /// <summary>
        /// Division operator for dividing one fixed-point number by another.
        /// </summary>
        public static Fixed32 operator /(Fixed32 leftHandSide, Fixed32 rightHandSide)
        {
            var result = (leftHandSide.RawValue << leftHandSide.Scale) / rightHandSide.RawValue;

            leftHandSide.RawValue = result;

            return leftHandSide;
        }

        /// <summary>
        /// Explicit conversion to a double-precision floating-point number.
        /// </summary>
        public static explicit operator double(Fixed32 number)
        {
            return (double)number.RawValue / (1 << number.Scale);
        }

        /// <summary>
        /// Implicit conversion to an integer, returning the whole number part of the fixed-point value.
        /// </summary>
        public static implicit operator int(Fixed32 number)
        {
            return number.WholeNumber;
        }

        /// <summary>
        /// Implicit conversion from an integer to a fixed-point number, using the default scale.
        /// </summary>
        public static implicit operator Fixed32(int number)
        {
            return new Fixed32(DefaultScale, number);
        }

        /// <summary>
        /// Implicit conversion to a single-precision floating-point number.
        /// </summary>
        public static implicit operator float(Fixed32 number) => (float)(double)number;

        /// <summary>
        /// Implicit conversion from a single-precision floating-point number to a fixed-point number.
        /// </summary>
        public static implicit operator Fixed32(float number)
        {
            // Convert float to fixed-point representation
            long rawValue = (long)(number * (1 << DefaultScale));

            return new Fixed32(DefaultScale) { RawValue = rawValue };
        }


        /// <summary>
        /// Returns a string representation of the fixed-point number as a double-precision floating-point number.
        /// </summary>
        public override string ToString()
        {
            return ((double)this).ToString();
        }

        // Trigonometric functions using Taylor series approximations

        /// <summary>
        /// Computes the sine of a fixed-point angle (in radians).
        /// </summary>
        public static Fixed32 Sin(Fixed32 radians)
        {
            return (Fixed32)Mathf.Sin(radians);
        }

        /// <summary>
        /// Computes the cosine of a fixed-point angle (in radians).
        /// </summary>
        public static Fixed32 Cos(Fixed32 radians)
        {
            return (Fixed32)Mathf.Cos(radians);
        }

        /// <summary>
        /// Computes the tangent of a fixed-point angle (in radians).
        /// </summary>
        public static Fixed32 Tan(Fixed32 angle)
        {
            return Sin(angle) / Cos(angle);
        }
    }
}
