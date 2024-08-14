using System.IO;

namespace Types
{
    /// <summary>
    /// Struct for fixed point numbers.
    /// Gotten from this repo here: https://github.com/stormmuller/fixed-point-types/blob/master/Types/Types/Fixed32.cs
    /// </summary>
    public struct Fixed32
    {
        public const int Epsilon = 1;

        private const int FractionMask = 0xffff;
        public static Fixed32 PI = (Fixed32)3.1415926535897932384626433832795;
        private const int DefaultScale = 16;

        public long RawValue { get; private set; }
        public int Scale { get; private set; }

        public Fixed32(int scale) : this(scale, 0) { }

        public Fixed32(int scale, int wholeNumber)
        {
            this.Scale = scale;
            this.RawValue = wholeNumber << scale;
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
        public static implicit operator Fixed32(float number) => (Fixed32)(double)number;


        public override string ToString()
        {
            return ((double)this).ToString();
        }

        // Trigonometric functions using Taylor series approximations
        public static Fixed32 Sin(Fixed32 angle)
        {
            double radians = (double)angle * (PI / 180);
            double sin = radians;
            double term = radians;
            for (int i = 3; i <= 15; i += 2)
            {
                term *= -radians * radians / (i * (i - 1));
                sin += term;
            }
            return (Fixed32)sin;
        }

        public static Fixed32 Cos(Fixed32 angle)
        {
            double radians = (double)angle * (PI / 180);
            double cos = 1.0;
            double term = 1.0;
            for (int i = 2; i <= 14; i += 2)
            {
                term *= -radians * radians / (i * (i - 1));
                cos += term;
            }
            return (Fixed32)cos;
        }

        public static Fixed32 Tan(Fixed32 angle)
        {
            return Sin(angle) / Cos(angle);
        }
    }
}