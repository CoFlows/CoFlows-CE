using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AQI.AQILabs.Kernel.Numerics.Math.Properties;

namespace AQI.AQILabs.Kernel.Numerics.Math.Statistics
{
    using Math = System.Math;

    public static class Statistics
    {
        // Methods
        public static double Maximum(IEnumerable<double> data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }
            double minValue = double.MinValue;
            int num2 = 0;
            foreach (double num3 in data)
            {
                minValue = Math.Max(minValue, num3);
                num2++;
            }
            if (num2 == 0)
            {
                throw new ArgumentException(Resources.CollectionEmpty, "data");
            }
            return minValue;
        }

        public static double Maximum(IEnumerable<double?> data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }
            double minValue = double.MinValue;
            int num2 = 0;
            foreach (double? nullable in data)
            {
                if (nullable.HasValue)
                {
                    minValue = Math.Max(minValue, nullable.Value);
                    num2++;
                }
            }
            if (num2 == 0)
            {
                throw new ArgumentException(Resources.CollectionEmpty, "data");
            }
            return minValue;
        }

        public static double Maximum(double[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }
            int length = data.Length;
            if (length == 0)
            {
                throw new ArgumentException(Resources.CollectionEmpty, "data");
            }
            double minValue = double.MinValue;
            for (int i = 0; i < length; i++)
            {
                minValue = Math.Max(minValue, data[i]);
            }
            return minValue;
        }

        public static double Mean(double[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }
            double num = 0.0;
            int num2 = 0;
            int length = data.Length;
            for (int i = 0; i < length; i++)
            {
                num += (data[i] - num) / ((double)(++num2));
            }
            return num;
        }

        public static double Mean(IEnumerable<double?> data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }
            double num = 0.0;
            int num2 = 0;
            foreach (double? nullable in data)
            {
                if (nullable.HasValue)
                {
                    num += (nullable.Value - num) / ((double)(++num2));
                }
            }
            return num;
        }

        public static double Mean(IEnumerable<double> data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }
            double num = 0.0;
            int num2 = 0;
            foreach (double num3 in data)
            {
                num += (num3 - num) / ((double)(++num2));
            }
            return num;
        }

        public static double Median(double[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }
            int length = data.Length;
            double[] array = new double[length];
            data.CopyTo(array, 0);
            Array.Sort<double>(array);
            int index = (length / 2) - 1;
            if ((length % 2) == 0)
            {
                return ((array[index] + array[index + 1]) / 2.0);
            }
            return array[index + 1];
        }

        public static double Median(IEnumerable<double> data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }
            List<double> list = new List<double>(data);
            list.Sort();
            int num = (list.Count / 2) - 1;
            if ((list.Count % 2) == 0)
            {
                return ((list[num] + list[num + 1]) / 2.0);
            }
            return list[num + 1];
        }

        public static double Median(IEnumerable<double?> data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }
            List<double> list = new List<double>();
            foreach (double? nullable in data)
            {
                if (nullable.HasValue)
                {
                    list.Add(nullable.Value);
                }
            }
            if (list.Count == 0)
            {
                throw new ArgumentException(Resources.CollectionEmpty, "data");
            }
            return Median((IEnumerable<double>)list);
        }

        public static double Minimum(IEnumerable<double?> data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }
            double maxValue = double.MaxValue;
            int num2 = 0;
            foreach (double? nullable in data)
            {
                if (nullable.HasValue)
                {
                    maxValue = Math.Min(maxValue, nullable.Value);
                    num2++;
                }
            }
            if (num2 == 0)
            {
                throw new ArgumentException(Resources.CollectionEmpty, "data");
            }
            return maxValue;
        }

        public static double Minimum(double[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }
            int length = data.Length;
            if (length == 0)
            {
                throw new ArgumentException(Resources.CollectionEmpty, "data");
            }
            double maxValue = double.MaxValue;
            for (int i = 0; i < length; i++)
            {
                maxValue = Math.Min(maxValue, data[i]);
            }
            return maxValue;
        }

        public static double Minimum(IEnumerable<double> data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }
            double maxValue = double.MaxValue;
            int num2 = 0;
            foreach (double num3 in data)
            {
                maxValue = Math.Min(maxValue, num3);
                num2++;
            }
            if (num2 == 0)
            {
                throw new ArgumentException(Resources.CollectionEmpty, "data");
            }
            return maxValue;
        }

        public static double StandardDeviation(IEnumerable<double> data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }
            return Math.Sqrt(Variance(data));
        }

        public static double StandardDeviation(double[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }
            return Math.Sqrt(Variance(data));
        }

        public static double StandardDeviation(IEnumerable<double?> data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }
            return Math.Sqrt(Variance(data));
        }

        public static double Variance(double[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }
            double num = 0.0;
            double num2 = 0.0;
            int num3 = 0;
            int length = data.Length;
            if (length > 0)
            {
                num3++;
                num2 = data[0];
            }
            for (int i = 1; i < length; i++)
            {
                num3++;
                double num6 = data[i];
                num2 += num6;
                double num7 = (num3 * num6) - num2;
                num += (num7 * num7) / ((double)(num3 * (num3 - 1)));
            }
            return (num / ((double)(num3 - 1)));
        }

        public static double Variance(IEnumerable<double> data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }
            double num = 0.0;
            double current = 0.0;
            int num3 = 0;
            IEnumerator<double> enumerator = data.GetEnumerator();
            if (enumerator.MoveNext())
            {
                num3++;
                current = enumerator.Current;
            }
            while (enumerator.MoveNext())
            {
                num3++;
                double num4 = enumerator.Current;
                current += num4;
                double num5 = (num3 * num4) - current;
                num += (num5 * num5) / ((double)(num3 * (num3 - 1)));
            }
            return (num / ((double)(num3 - 1)));
        }

        public static double Variance(IEnumerable<double?> data)
        {
            double? nullable;
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }
            double num = 0.0;
            double num2 = 0.0;
            int num3 = 0;
            IEnumerator<double?> enumerator = data.GetEnumerator();
            do
            {
                if (!enumerator.MoveNext())
                {
                    goto Label_00AD;
                }
                nullable = enumerator.Current;
            }
            while (!nullable.HasValue);
            num3++;
            double? current = enumerator.Current;
            num2 = current.Value;
            Label_00AD:
            while (enumerator.MoveNext())
            {
                double? nullable3 = enumerator.Current;
                if (nullable3.HasValue)
                {
                    num3++;
                    double? nullable4 = enumerator.Current;
                    double num4 = nullable4.Value;
                    num2 += num4;
                    double num5 = (num3 * num4) - num2;
                    num += (num5 * num5) / ((double)(num3 * (num3 - 1)));
                }
            }
            return (num / ((double)(num3 - 1.0)));
        }

        public static double QuadraticVariation(double[] data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            double res = 0;

            for (int i = 0; i < data.Length; i++)
                res += data[i] * data[i];

            return Math.Sqrt(res / data.Length);
        }

        public static double Covariance(double[] data1, double[] data2)
        {
            if (data1 == null || data2 == null)
                throw new ArgumentNullException("data");

            if (data1.Length != data2.Length)
                throw new Exception("Length of both arrays need to be equal");

            double avg1 = data1.Average();
            double avg2 = data2.Average();

            double res = 0;

            for (int i = 0; i < data1.Length; i++)
                res += (data1[i] - avg1) * (data2[i] - avg2);

            return res / (data1.Length - 1.0);
        }

        public static double Covariance(IEnumerable<double> data1, IEnumerable<double> data2)
        {
            if (data1 == null || data2 == null)
                throw new ArgumentNullException("data");

            return Covariance(data1.ToArray(), data2.ToArray());
        }

        public static double KendallTau(double[] data1, double[] data2)
        {
            if (data1 == null || data2 == null)
                throw new ArgumentNullException("data");

            if (data1.Length != data2.Length)
                throw new Exception("Length of both arrays need to be equal");

            double concordant = 0.0;
            double discordant = 0.0;

            for (int i = 1; i < data1.Length; i++)
            {
                if (Math.Sign(data1[i] - data1[i - 1]) == Math.Sign(data2[i] - data2[i - 1]))
                    concordant++;
                else
                    discordant++;
            }

            return (concordant - discordant) / (0.5 * (double)data1.Length * ((double)data1.Length - 1.0));
        }

        public static double KendallTau(IEnumerable<double> data1, IEnumerable<double> data2)
        {
            if (data1 == null || data2 == null)
                throw new ArgumentNullException("data");

            return KendallTau(data1.ToArray(), data2.ToArray());
        }

        public static double CoQuadraticVariation(double[] data1, double[] data2)
        {
            if (data1 == null || data2 == null)
                throw new ArgumentNullException("data");

            if (data1.Length != data2.Length)
                throw new Exception("Length of both arrays need to be equal");

            double res = 0;

            for (int i = 0; i < data1.Length; i++)
                res += (data1[i]) * (data2[i]);

            return res / (data1.Length - 1.0);
        }

        public static double CoQuadraticVariation(IEnumerable<double> data1, IEnumerable<double> data2)
        {
            if (data1 == null || data2 == null)
                throw new ArgumentNullException("data");

            return CoQuadraticVariation(data1.ToArray(), data2.ToArray());
        }

    }
}
