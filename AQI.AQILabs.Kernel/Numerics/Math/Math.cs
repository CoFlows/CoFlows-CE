using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AQI.AQILabs.Kernel.Numerics.Math.Math
{
    using Math = System.Math;

    public static class Precision
    {
        // Fields
        private static readonly double _doubleMachinePrecision = Math.Pow(2.0, -53.0);
        private const int s_BinaryBaseNumber = 2;
        private static readonly double s_DoubleMachinePrecision = Math.Pow(2.0, -53.0);
        private const int s_DoublePrecision = 0x35;
        private static readonly int s_NumberOfDecimalPlacesForDoubles = ((int)Math.Ceiling(Math.Abs(Math.Log10(s_DoubleMachinePrecision))));
        private static readonly int s_NumberOfDecimalPlacesForFloats = ((int)Math.Ceiling(Math.Abs(Math.Log10(s_SingleMachinePrecision))));
        private static readonly double s_SingleMachinePrecision = Math.Pow(2.0, -24.0);
        private const int s_SinglePrecision = 0x18;

        // Methods
        public static int CompareWithDecimalPlaces(double first, double second, int numberOfDecimalPlaces)
        {
            if (!double.IsNaN(first) && !double.IsNaN(second))
            {
                if (double.IsInfinity(first) || double.IsInfinity(second))
                {
                    return first.CompareTo(second);
                }
                if (EqualsWithinDecimalPlaces(first, second, numberOfDecimalPlaces))
                {
                    return 0;
                }
            }
            return first.CompareTo(second);
        }

        public static int CompareWithTolerance(double first, double second, long maxUlps)
        {
            if (!double.IsNaN(first) && !double.IsNaN(second))
            {
                if (double.IsInfinity(first) || double.IsInfinity(second))
                {
                    return first.CompareTo(second);
                }
                if (EqualsWithTolerance(first, second, maxUlps))
                {
                    return 0;
                }
            }
            return first.CompareTo(second);
        }

        private static bool EqualsWithinAbsoluteDecimalPlaces(double first, double second, int numberOfDecimalPlaces)
        {
            double num = Math.Pow(10.0, (double)-(numberOfDecimalPlaces - 1));
            return (Math.Abs((double)(first - second)) < (num / 2.0));
        }

        public static bool EqualsWithinDecimalPlaces(double first, double second, int numberOfDecimalPlaces)
        {
            if (double.IsNaN(first) || double.IsNaN(second))
            {
                return false;
            }
            if (double.IsInfinity(first) || double.IsInfinity(second))
            {
                return (first == second);
            }
            if (numberOfDecimalPlaces <= 0)
            {
                throw new ArgumentOutOfRangeException("numberOfSignificantFigures");
            }
            if (first.Equals(second))
            {
                return true;
            }
            if ((Math.Abs(first) >= (10.0 * s_DoubleMachinePrecision)) && (Math.Abs(second) >= (10.0 * s_DoubleMachinePrecision)))
            {
                return EqualsWithinRelativeDecimalPlaces(first, second, numberOfDecimalPlaces);
            }
            return EqualsWithinAbsoluteDecimalPlaces(first, second, numberOfDecimalPlaces);
        }

        private static bool EqualsWithinRelativeDecimalPlaces(double first, double second, int numberOfDecimalPlaces)
        {
            int num = Magnitude(first);
            int num2 = Magnitude(second);
            if (Math.Max(num, num2) > (Math.Min(num, num2) + 1))
            {
                return false;
            }
            double num4 = Math.Pow(10.0, (double)-(numberOfDecimalPlaces - 1)) / 2.0;
            if (first > second)
            {
                return (((first * Math.Pow(10.0, (double)-num)) - num4) < (second * Math.Pow(10.0, (double)-num)));
            }
            return (((second * Math.Pow(10.0, (double)-num2)) - num4) < (first * Math.Pow(10.0, (double)-num2)));
        }

        public static bool EqualsWithTolerance(double first, double second, long maxUlps)
        {
            if (maxUlps < 1L)
            {
                throw new ArgumentOutOfRangeException("maxUlps");
            }
            if (double.IsInfinity(first) || double.IsInfinity(second))
            {
                return (first == second);
            }
            if (double.IsNaN(first) || double.IsNaN(second))
            {
                return false;
            }
            long directionalLongFromDouble = GetDirectionalLongFromDouble(first);
            long num2 = GetDirectionalLongFromDouble(second);
            if (first <= second)
            {
                return ((directionalLongFromDouble + maxUlps) >= num2);
            }
            return ((num2 + maxUlps) >= directionalLongFromDouble);
        }

        private static long GetDirectionalLongFromDouble(double value)
        {
            long longFromDouble = GetLongFromDouble(value);
            if (longFromDouble < 0L)
            {
                return (-9223372036854775808L - longFromDouble);
            }
            return longFromDouble;
        }

        private static long GetLongFromDouble(double value)
        {
            return BitConverter.DoubleToInt64Bits(value);
        }

        public static bool IsLargerWithDecimalPlaces(double first, double second, int numberOfDecimalPlaces)
        {
            return ((!double.IsNaN(first) && !double.IsNaN(second)) && (CompareWithDecimalPlaces(first, second, numberOfDecimalPlaces) > 0));
        }

        public static bool IsLargerWithTolerance(double first, double second, long maxUlps)
        {
            return ((!double.IsNaN(first) && !double.IsNaN(second)) && (CompareWithTolerance(first, second, maxUlps) > 0));
        }

        public static bool IsSmallerWithDecimalPlaces(double first, double second, int numberOfDecimalPlaces)
        {
            return ((!double.IsNaN(first) && !double.IsNaN(second)) && (CompareWithDecimalPlaces(first, second, numberOfDecimalPlaces) < 0));
        }

        public static bool IsSmallerWithTolerance(double first, double second, long maxUlps)
        {
            return ((!double.IsNaN(first) && !double.IsNaN(second)) && (CompareWithTolerance(first, second, maxUlps) < 0));
        }

        public static int Magnitude(double value)
        {
            if (value.Equals((double)0.0))
            {
                return 0;
            }
            double d = Math.Log10(Math.Abs(value));
            if (d < 0.0)
            {
                return (int)Math.Truncate((double)(d - 1.0));
            }
            return (int)Math.Truncate(d);
        }

        public static double MaximumMatchingFloatingPointNumber(double value, long ulpsDifference)
        {
            double num;
            double num2;
            RangeOfMatchingFloatingPointNumbers(value, ulpsDifference, out num, out num2);
            return num2;
        }

        public static double MinimumMatchingFloatingPointNumber(double value, long ulpsDifference)
        {
            double num;
            double num2;
            RangeOfMatchingFloatingPointNumbers(value, ulpsDifference, out num, out num2);
            return num;
        }

        public static void RangeOfMatchingFloatingPointNumbers(double value, long ulpsDifference, out double bottomRangeEnd, out double topRangeEnd)
        {
            if (ulpsDifference < 1L)
            {
                throw new ArgumentOutOfRangeException("ulpsDifference");
            }
            if (double.IsInfinity(value))
            {
                topRangeEnd = value;
                bottomRangeEnd = value;
            }
            else if (double.IsNaN(value))
            {
                topRangeEnd = double.NaN;
                bottomRangeEnd = double.NaN;
            }
            else
            {
                long longFromDouble = GetLongFromDouble(value);
                if (longFromDouble < 0L)
                {
                    if (Math.Abs((long)(-9223372036854775808L - longFromDouble)) < ulpsDifference)
                    {
                        topRangeEnd = BitConverter.Int64BitsToDouble(ulpsDifference + (-9223372036854775808L - longFromDouble));
                    }
                    else
                    {
                        topRangeEnd = BitConverter.Int64BitsToDouble(longFromDouble - ulpsDifference);
                    }
                    if (Math.Abs(longFromDouble) < ulpsDifference)
                    {
                        bottomRangeEnd = double.MinValue;
                    }
                    else
                    {
                        bottomRangeEnd = BitConverter.Int64BitsToDouble(longFromDouble + ulpsDifference);
                    }
                }
                else
                {
                    if ((0x7fffffffffffffffL - longFromDouble) < ulpsDifference)
                    {
                        topRangeEnd = double.MaxValue;
                    }
                    else
                    {
                        topRangeEnd = BitConverter.Int64BitsToDouble(longFromDouble + ulpsDifference);
                    }
                    if (longFromDouble > ulpsDifference)
                    {
                        bottomRangeEnd = BitConverter.Int64BitsToDouble(longFromDouble - ulpsDifference);
                    }
                    else
                    {
                        bottomRangeEnd = BitConverter.Int64BitsToDouble(-9223372036854775808L + (ulpsDifference - longFromDouble));
                    }
                }
            }
        }

        public static void RangeOfMatchingUlps(double value, double relativeDifference, out long bottomRangeEnd, out long topRangeEnd)
        {
            if (relativeDifference < 0.0)
            {
                throw new ArgumentOutOfRangeException("relativeDifference");
            }
            if (double.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException();
            }
            if (double.IsNaN(value))
            {
                throw new ArgumentOutOfRangeException();
            }
            if (value.Equals((double)0.0))
            {
                topRangeEnd = GetLongFromDouble(relativeDifference);
                bottomRangeEnd = topRangeEnd;
            }
            else
            {
                long directionalLongFromDouble = GetDirectionalLongFromDouble(value + (relativeDifference * Math.Abs(value)));
                long num2 = GetDirectionalLongFromDouble(value - (relativeDifference * Math.Abs(value)));
                long num3 = GetDirectionalLongFromDouble(value);
                topRangeEnd = Math.Abs((long)(directionalLongFromDouble - num3));
                bottomRangeEnd = Math.Abs((long)(num3 - num2));
            }
        }

        public static double Value(double value)
        {
            if (value.Equals((double)0.0))
            {
                return value;
            }
            int num = Magnitude(value);
            return (value * Math.Pow(10.0, (double)-num));
        }

        // Properties
        public static int NumberOfDecimalPlacesForDoubles
        {
            get
            {
                return s_NumberOfDecimalPlacesForDoubles;
            }
        }

        public static int NumberOfDecimalPlacesForFloats
        {
            get
            {
                return s_NumberOfDecimalPlacesForFloats;
            }
        }

        // Nested Types
        public sealed class DoubleComparer : IComparer<double>
        {
            // Fields
            private int m_NumberOfSignificantDigits;
            private long m_Ulps;

            // Methods
            public DoubleComparer(int numberOfSignificantDigits)
            {
                this.m_Ulps = -1L;
                this.m_NumberOfSignificantDigits = -1;
                if (numberOfSignificantDigits < 1)
                {
                    throw new ArgumentOutOfRangeException("numberOfSignificantDigits");
                }
                this.m_NumberOfSignificantDigits = numberOfSignificantDigits;
            }

            public DoubleComparer(long ulps)
            {
                this.m_Ulps = -1L;
                this.m_NumberOfSignificantDigits = -1;
                if (ulps < 0L)
                {
                    throw new ArgumentOutOfRangeException("ulps");
                }
                this.m_Ulps = ulps;
            }

            public int Compare(double x, double y)
            {
                if (this.m_Ulps <= -1L)
                {
                    return Precision.CompareWithDecimalPlaces(x, y, this.m_NumberOfSignificantDigits);
                }
                return Precision.CompareWithTolerance(x, y, this.m_Ulps);
            }

            // Properties
            public int NumberOfSignificantDecimals
            {
                get
                {
                    return this.m_NumberOfSignificantDigits;
                }
                set
                {
                    if (value < 1)
                    {
                        throw new ArgumentOutOfRangeException("value");
                    }
                    this.m_Ulps = -1L;
                    this.m_NumberOfSignificantDigits = value;
                }
            }

            public long Ulps
            {
                get
                {
                    return this.m_Ulps;
                }
                set
                {
                    if (value < 0L)
                    {
                        throw new ArgumentOutOfRangeException("value");
                    }
                    this.m_NumberOfSignificantDigits = -1;
                    this.m_Ulps = value;
                }
            }
        }
    }
}