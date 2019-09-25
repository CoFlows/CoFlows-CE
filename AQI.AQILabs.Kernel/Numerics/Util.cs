using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using AQI.AQILabs.Kernel.Numerics.Math.LinearAlgebra;
using System.Data;
using AQI.AQILabs.Kernel.Numerics.Math.Statistics;
using System.ComponentModel;

namespace AQI.AQILabs.Kernel.Numerics.Util
{
    using Math = System.Math;

    public class Alignment
    {
        // Methods
        public static TimeSeries AddHoles(TimeSeries series, IList<int> holes)
        {
            TimeSeries series2 = null;
            if (holes == null)
            {
                return series;
            }
            int count = series.Count;
            int num2 = holes.Count;
            series2 = new TimeSeries(count + num2, series.DateTimes);
            int num3 = 0;
            for (int i = 0; i < (count + num2); i++)
            {
                if ((num3 < holes.Count) && (i == holes[num3]))
                {
                    series2[i] = double.NaN;
                    num3++;
                }
                else
                {
                    series2[i] = series[i - num3];
                }
            }
            return series2;
        }

        public static TimeSeries RemoveHoles(TimeSeries series, out IList<int> holes)
        {
            return RemoveHoles(new TimeSeries[] { series }, out holes)[0];
        }

        public static TimeSeries[] RemoveHoles(TimeSeries[] series, out IList<int> holes)
        {
            int length = series.Length;
            TimeSeries[] seriesArray = null;
            int num2 = 0;
            DateTimeList dateTimes = null;
            foreach (TimeSeries series2 in series)
            {
                if (series2.Count > num2)
                {
                    num2 = series2.Count;
                    dateTimes = series2.DateTimes;
                }
            }
            holes = null;
            int[] numArray = new int[length];
            for (int i = 0; i < length; i++)
            {
                numArray[i] = series[i].Count;
            }
            for (int j = 0; j < num2; j++)
            {
                for (int m = 0; m < length; m++)
                {
                    TimeSeries series3 = series[m];
                    if ((j >= numArray[m]) || double.IsNaN(series3[j]))
                    {
                        if (holes == null)
                        {
                            holes = new List<int>();
                        }
                        ((List<int>)holes).Add(j);
                        break;
                    }
                }
            }
            if (holes == null)
            {
                return series;
            }
            seriesArray = new TimeSeries[length];
            int count = holes.Count;
            for (int k = 0; k < length; k++)
            {
                seriesArray[k] = new TimeSeries(num2 - count, dateTimes);
                int num9 = 0;
                for (int n = 0; n < num2; n++)
                {
                    if ((num9 < count) && (n == holes[num9]))
                    {
                        num9++;
                    }
                    else
                    {
                        seriesArray[k][n - num9] = series[k][n];
                    }
                }
            }
            return seriesArray;
        }
    }

    [Serializable]
    public class DateTimeList : IEnumerable, IComparable
    {
        // Fields
        public DateTime[] _dateTimes;

        // Methods
        public DateTimeList(int size)
        {
            this._dateTimes = (size > 0) ? new DateTime[size] : null;
        }

        public DateTimeList(DateTime[] dateTimes)
        {
            this._dateTimes = new DateTime[dateTimes.Length];
            for (int i = 0; i < dateTimes.Length; i++)
            {
                this._dateTimes[i] = dateTimes[i];
            }
        }

        public DateTimeList(IList<DateTime> dateTimes)
        {
            this._dateTimes = new DateTime[dateTimes.Count];
            int num = 0;
            foreach (DateTime time in dateTimes)
            {
                this._dateTimes[num++] = time;
            }
        }

        public DateTimeList(DateTimeList dateTimes)
        {
            this._dateTimes = new DateTime[dateTimes._dateTimes.Length];
            for (int i = 0; i < dateTimes._dateTimes.Length; i++)
            {
                this._dateTimes[i] = dateTimes._dateTimes[i];
            }
        }

        /*public IEnumerator<DateTime> GetEnumerator()
        {
            return (this._dateTimes.GetEnumerator() as IEnumerator<DateTime>);
        }*/

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this._dateTimes.GetEnumerator();
        }

        // Properties
        public int Count
        {
            get
            {
                return this._dateTimes.Length;
            }
        }

        public DateTime this[int index]
        {
            get
            {
                return this._dateTimes[Math.Min(index, this.Count - 1)];
            }
            set
            {
                this._dateTimes[index] = value;
            }
        }

        public bool ContainsTimeOfDayPart
        {
            get
            {
                for (int i = 0; i < _dateTimes.Length; i++)
                {
                    if (_dateTimes[i].TimeOfDay.Ticks != 0)
                        return true;
                }
                return false;
            }
        }

        public int CompareTo(object obj)
        {
            if (!(obj is DateTimeList))
                throw new ArgumentException("Object is not a DateTimeList");
            DateTimeList comp = (DateTimeList)obj;
            int m = Math.Min(Count, comp.Count);
            for (int i = 0; i < m; i++)
            {
                int c = _dateTimes[i].CompareTo(comp[i]);
                if (c != 0)
                    return c;
            }
            if (Count > m)
                return 1;
            if (comp.Count > m)
                return -1;
            return 0;
        }
    }

    public enum TimeSeriesSynchronizeMethod { Latest, Exact, Union } // Union och Interpolate not implemented yet

    [Serializable]
    public class TimeSeries : IEnumerable<double>, IEnumerable, IEnumerable<TimeSeriesItem>
    {
        // Fields
        private DenseVector _data;
        private DateTimeList _dateTimes;
        private int _bufferCount = 0;

        //Arturo 2010/10/10
        private Dictionary<DateTime, int> _dateIndex = null;

        public void Sort()
        {
            DenseVector _dataNew = new DenseVector(_data.Data);

            List<DateTime> orderedDates = (from d in _dateIndex.Keys orderby d select d).ToList();
            for (int i = 0; i < orderedDates.Count; i++)
            {

                _dataNew[i] = _data[_dateIndex[orderedDates[i]]];
                _dateTimes[i] = orderedDates[i];
                _dateIndex[orderedDates[i]] = i;
            }

            _data = _dataNew;
        }

        public enum DateSearchType
        {
            Previous = 1, Next = 2
        };

        #region Contructors
        // Methods
        public TimeSeries()
        {
            //this._data = null;
            //this._dateTimes = null;

            int size = 1;
            DateTimeList dateTimes = new DateTimeList(new List<DateTime>() { DateTime.MinValue });

            if ((dateTimes != null) && (dateTimes.Count < size))
            {
                throw new ApplicationException("DateTimeList contains fewer dates than number of points");
            }
            this._data = (size > 0) ? new DenseVector(size, double.NaN) : null;
            this._dateTimes = (dateTimes != null) ? new DateTimeList(dateTimes) : null;

            if (this._dateTimes != null)
            {
                int length = _dateTimes.Count;
                if (this._dateIndex == null)
                {
                    this._dateIndex = new Dictionary<DateTime, int>();

                    for (int i = 0; i < length; i++)
                        this._dateIndex[this._dateTimes[i]] = i;
                }
            }
        }

        public TimeSeries(TimeSeries series)
            : this(new List<double>(series._data.Data), series.DateTimes)//, series._dateIndex, true, true)
        {
            //this._bufferCount = series._bufferCount;
            //if (this.Count != series.Count)
            //    Console.WriteLine("");

            //if (this._data.Count != series._data.Count)
            //    Console.WriteLine("");

            //if (this._dateIndex.Count != series._dateIndex.Count)
            //    Console.WriteLine("");

            //if (this._dateTimes.Count != series._dateTimes.Count)
            //    Console.WriteLine("");

        }

        public TimeSeries(IList<double> data)
            : this(data, null)
        {
        }

        public TimeSeries(int size)
        {
            //int size = 1;
            List<DateTime> list = new List<DateTime>();
            for (int i = 0; i < size; i++)
                list.Add(DateTime.MinValue);
            DateTimeList dateTimes = new DateTimeList(list);

            if ((dateTimes != null) && (dateTimes.Count < size))
            {
                throw new ApplicationException("DateTimeList contains fewer dates than number of points");
            }
            this._data = (size > 0) ? new DenseVector(size, double.NaN) : null;
            this._dateTimes = (dateTimes != null) ? new DateTimeList(dateTimes) : null;

            if (this._dateTimes != null)
            {
                int length = _dateTimes.Count;
                if (this._dateIndex == null)
                {
                    this._dateIndex = new Dictionary<DateTime, int>();

                    for (int i = 0; i < length; i++)
                        this._dateIndex[this._dateTimes[i]] = i;
                }
            }

            this._bufferCount = size == 0 ? 0 : (this._data.Count - 1);
        }

        private TimeSeries(IList<double> data, DateTimeList dateTimes)
        {
            this._data = ((data != null) && (data.Count > 0)) ? new DenseVector(data) : null;
            this._dateTimes = (dateTimes != null) ? new DateTimeList(dateTimes) : null;

            if (this._dateTimes != null)
            {
                int length = _dateTimes.Count;
                if (this._dateIndex == null)
                {
                    this._dateIndex = new Dictionary<DateTime, int>();

                    for (int i = 0; i < length; i++)
                        this._dateIndex[this._dateTimes[i]] = i;
                }
            }
        }

        public TimeSeries(int size, DateTimeList dateTimes)
        {
            if ((dateTimes != null) && (dateTimes.Count < size))
            {
                throw new ApplicationException("DateTimeList contains fewer dates than number of points");
            }
            this._data = (size > 0) ? new DenseVector(size, double.NaN) : null;
            this._dateTimes = (dateTimes != null) ? new DateTimeList(dateTimes) : null;

            if (this._dateTimes != null)
            {
                int length = _dateTimes.Count;
                if (this._dateIndex == null)
                {
                    this._dateIndex = new Dictionary<DateTime, int>();

                    for (int i = 0; i < length; i++)
                        this._dateIndex[this._dateTimes[i]] = i;
                }
            }

            //this._bufferCount = this._data.Count;
        }

        //public TimeSeries(int size, double value)
        //{
        //    //this._data = (size > 0) ? new DenseVector(size, value) : null;
        //    //this._dateTimes = null;

        //    //int size = 1;
        //    DateTimeList dateTimes = new DateTimeList(new List<DateTime>() { DateTime.MinValue });

        //    if ((dateTimes != null) && (dateTimes.Count < size))
        //    {
        //        throw new ApplicationException("DateTimeList contains fewer dates than number of points");
        //    }
        //    this._data = (size > 0) ? new DenseVector(size, double.NaN) : null;
        //    this._dateTimes = (dateTimes != null) ? new DateTimeList(dateTimes) : null;

        //    if (this._dateTimes != null)
        //    {
        //        int length = _dateTimes.Count;
        //        if (this._dateIndex == null)
        //        {
        //            this._dateIndex = new Dictionary<DateTime, int>();

        //            for (int i = 0; i < length; i++)
        //                this._dateIndex[this._dateTimes[i]] = i;
        //        }
        //    }
        //}

        //public TimeSeries(double startValue, double endValue, double step)
        //{
        //    if ((((startValue < endValue) && (step < 0.0)) || ((startValue > endValue) && (step > 0.0))) || (step == 0.0))
        //    {
        //        throw new ArgumentException("Invalid arguments passed to constructor");
        //    }
        //    if (step > 0.0)
        //    {
        //        int size = (int)(((endValue - startValue) / step) + 1.0);
        //        this._data = new DenseVector(size);
        //        double num2 = startValue;
        //        for (int i = 0; i < size; i++)
        //        {
        //            this._data[i] = num2;
        //            num2 += step;
        //        }
        //    }
        //    else
        //    {
        //        int num4 = (int)(((startValue - endValue) / -step) + 1.0);
        //        this._data = new DenseVector(num4);
        //        double num5 = startValue;
        //        for (int j = 0; j < num4; j++)
        //        {
        //            this._data[j] = num5;
        //            num5 += step;
        //        }
        //    }
        //    this._dateTimes = null;
        //}

        //public TimeSeries(DataTable table)
        //{
        //    _dateTimes = null;
        //    if ((table.Rows.Count == 0) || (table.Columns.Count==0))
        //    {
        //        _data = null;
        //        return;
        //    }
        //    _data = new DenseVector(table.Rows.Count, double.NaN);
        //    int valuecol = 0;
        //    if (table.Columns.Count > 1)
        //    {
        //        _dateTimes = new DateTimeList(table.Rows.Count);
        //        valuecol = 1;
        //    }
        //    int i = 0;
        //    foreach (DataRow r in table.Rows)
        //    {
        //        if (valuecol>0)
        //            _dateTimes[i] = Convert.ToDateTime(r[0]);
        //        _data[i++] = Convert.ToDouble(r[valuecol]);
        //    }
        //}

        ////internal TimeSeries(int size, DateTimeList dateTimes, double value)
        //internal TimeSeries(int size, DateTimeList dateTimes, Dictionary<DateTime, int> dateIndex, double value, bool copyDateTimes)
        //{
        //    if ((dateTimes != null) && (dateTimes.Count < size))
        //    {
        //        throw new ApplicationException("DateTimeList contains fewer dates than number of points");
        //    }
        //    this._data = (size > 0) ? new DenseVector(size, value) : null;
        //    this._dateTimes = (dateTimes != null) ? new DateTimeList(dateTimes) : null;

        //    //if (this._dateTimes != null)
        //    //{
        //    //    int length = _dateTimes.Count;
        //    //    if (this._dateIndex == null)
        //    //    {
        //    //        this._dateIndex = new Dictionary<DateTime, int>();

        //    //        for (int i = 0; i < length; i++)
        //    //            this._dateIndex[this._dateTimes[i]] = i;
        //    //    }
        //    //}

        //    if (dateIndex != null)
        //    {
        //        if (copyDateTimes)
        //        {
        //            this._dateIndex = new Dictionary<DateTime, int>();
        //            foreach (DateTime date in dateIndex.Keys)
        //                this._dateIndex.Add(date, dateIndex[date]);
        //        }
        //        else
        //            this._dateIndex = dateIndex;
        //    }
        //}

        //private TimeSeries(DenseVector data, DateTimeList dateTimes, Dictionary<DateTime, int> dateIndex, bool copyData, bool copyDateTimes)
        //{
        //    if (data != null)
        //    {
        //        this._data = copyData ? new DenseVector(data) : data;
        //    }
        //    else
        //    {
        //        this._data = null;
        //    }
        //    if (dateTimes != null)
        //    {
        //        this._dateTimes = copyDateTimes ? new DateTimeList(dateTimes) : dateTimes;
        //    }
        //    else
        //    {
        //        this._dateTimes = null;
        //    }
        //    if (dateIndex != null)
        //    {
        //        if (copyDateTimes)
        //        {
        //            this._dateIndex = new ConcurrentDictionary<DateTime, int>();
        //            foreach (DateTime date in dateIndex.Keys)
        //                this._dateIndex.TryAdd(date, dateIndex[date]);
        //        }
        //        else
        //            this._dateIndex = dateIndex;
        //    }

        //    //if (this._dateTimes != null)
        //    //{
        //    //    int length = _dateTimes.Count;
        //    //    if (this._dateIndex == null)
        //    //    {
        //    //        this._dateIndex = new Dictionary<DateTime, int>();
        //    //        bool ok = this._dateTimes._dateTimes.Max() == DateTime.MinValue;
        //    //        for (int i = 0; i < length; i++)
        //    //            if ((this._dateTimes[i] != DateTime.MinValue) || (!ok))
        //    //                this._dateIndex[this._dateTimes[i]] = i;
        //    //    }
        //    //}
        //}
        #endregion

        #region Blomman Members
        public TimeSeries Abs()
        {
            TimeSeries output = null;
            new AbsHelper().Calculate(ref output, this);
            return output;
        }

        public TimeSeries Add(TimeSeries series)
        {
            TimeSeries output = null;
            new AdditionHelper().Calculate(ref output, this, series);
            return output;
        }

        public TimeSeries Add(double value)
        {
            TimeSeries output = null;
            new AdditionHelper().Calculate(ref output, this, value);
            return output;
        }

        public TimeSeries And(TimeSeries series)
        {
            TimeSeries output = null;
            new ComparisonAndHelper().Calculate(ref output, this, series);
            return output;
        }

        public TimeSeries Divide(TimeSeries series)
        {
            TimeSeries output = null;
            new DivisionHelper().Calculate(ref output, this, series);
            return output;
        }

        public TimeSeries Divide(double value)
        {
            TimeSeries output = null;
            new DivisionHelper().Calculate(ref output, this, value);
            return output;
        }

        public bool Equals(TimeSeries series)
        {
            return (this.Data == series.Data);
        }

        public IEnumerator<double> GetEnumerator()
        {
            return this.Data.GetEnumerator();
        }

        public TimeSeries Higher(TimeSeries series)
        {
            TimeSeries output = null;
            new HigherHelper().Calculate(ref output, this, series);
            return output;
        }

        public TimeSeries IsCrossing(TimeSeries series)
        {
            IList<int> list;
            TimeSeries[] seriesArray2 = Alignment.RemoveHoles(new TimeSeries[] { this.ShiftRight(1), series.ShiftRight(1), this, series }, out list);
            int count = seriesArray2[0].Count;
            TimeSeries series2 = new TimeSeries(count, seriesArray2[0].DateTimes);
            for (int i = 0; i < count; i++)
            {
                series2[i] = (((seriesArray2[0][i] >= seriesArray2[1][i]) && (seriesArray2[2][i] < seriesArray2[3][i])) || ((seriesArray2[0][i] <= seriesArray2[1][i]) && (seriesArray2[2][i] > seriesArray2[3][i]))) ? ((double)1) : ((double)0);
            }
            return Alignment.AddHoles(series2, list);
        }

        public TimeSeries IsCrossing(double value)
        {
            IList<int> list;
            TimeSeries[] seriesArray2 = Alignment.RemoveHoles(new TimeSeries[] { this.ShiftRight(1), this }, out list);
            int count = seriesArray2[0].Count;
            TimeSeries series = new TimeSeries(count, seriesArray2[0].DateTimes);
            for (int i = 0; i < count; i++)
            {
                series[i] = (((seriesArray2[0][i] >= value) && (seriesArray2[1][i] < value)) || ((seriesArray2[0][i] <= value) && (seriesArray2[1][i] > value))) ? ((double)1) : ((double)0);
            }
            return Alignment.AddHoles(series, list);
        }

        public TimeSeries IsCrossingAbove(TimeSeries series)
        {
            IList<int> list;
            TimeSeries[] seriesArray2 = Alignment.RemoveHoles(new TimeSeries[] { this.ShiftRight(1), series.ShiftRight(1), this, series }, out list);
            int count = seriesArray2[0].Count;
            TimeSeries series2 = new TimeSeries(count, seriesArray2[0].DateTimes);
            for (int i = 0; i < count; i++)
            {
                series2[i] = ((seriesArray2[0][i] <= seriesArray2[1][i]) && (seriesArray2[2][i] > seriesArray2[3][i])) ? ((double)1) : ((double)0);
            }
            return Alignment.AddHoles(series2, list);
        }

        public TimeSeries IsCrossingAbove(double value)
        {
            IList<int> list;
            TimeSeries[] seriesArray2 = Alignment.RemoveHoles(new TimeSeries[] { this.ShiftRight(1), this }, out list);
            int count = seriesArray2[0].Count;
            TimeSeries series = new TimeSeries(count, seriesArray2[0].DateTimes);
            for (int i = 0; i < count; i++)
            {
                series[i] = ((seriesArray2[0][i] <= value) && (seriesArray2[1][i] > value)) ? ((double)1) : ((double)0);
            }
            return Alignment.AddHoles(series, list);
        }

        public TimeSeries IsCrossingBelow(TimeSeries series)
        {
            IList<int> list;
            TimeSeries[] seriesArray2 = Alignment.RemoveHoles(new TimeSeries[] { this.ShiftRight(1), series.ShiftRight(1), this, series }, out list);
            int count = seriesArray2[0].Count;
            TimeSeries series2 = new TimeSeries(count, seriesArray2[0].DateTimes);
            for (int i = 0; i < count; i++)
            {
                series2[i] = ((seriesArray2[0][i] >= seriesArray2[1][i]) && (seriesArray2[2][i] < seriesArray2[3][i])) ? ((double)1) : ((double)0);
            }
            return Alignment.AddHoles(series2, list);
        }

        public TimeSeries IsCrossingBelow(double value)
        {
            IList<int> list;
            TimeSeries[] seriesArray2 = Alignment.RemoveHoles(new TimeSeries[] { this.ShiftRight(1), this }, out list);
            int count = seriesArray2[0].Count;
            TimeSeries series = new TimeSeries(count, seriesArray2[0].DateTimes);
            for (int i = 0; i < count; i++)
            {
                series[i] = ((seriesArray2[0][i] >= value) && (seriesArray2[1][i] < value)) ? ((double)1) : ((double)0);
            }
            return Alignment.AddHoles(series, list);
        }

        public TimeSeries IsEqual(TimeSeries series)
        {
            TimeSeries output = null;
            new ComparisonEqualHelper().Calculate(ref output, this, series);
            return output;
        }

        public TimeSeries IsEqual(double value)
        {
            TimeSeries output = null;
            new ComparisonEqualHelper().Calculate(ref output, this, value);
            return output;
        }

        public TimeSeries IsFalling()
        {
            TimeSeries output = null;
            new ComparisonGreaterHelper().Calculate(ref output, this.ShiftRight(1), this);
            return output;
        }

        public TimeSeries IsGreater(TimeSeries series)
        {
            TimeSeries output = null;
            new ComparisonGreaterHelper().Calculate(ref output, this, series);
            return output;
        }

        public TimeSeries IsGreater(double value)
        {
            TimeSeries output = null;
            new ComparisonGreaterHelper().Calculate(ref output, this, value);
            return output;
        }

        public TimeSeries IsGreaterEqual(TimeSeries series)
        {
            TimeSeries output = null;
            new ComparisonGreaterEqualHelper().Calculate(ref output, this, series);
            return output;
        }

        public TimeSeries IsGreaterEqual(double value)
        {
            TimeSeries output = null;
            new ComparisonGreaterEqualHelper().Calculate(ref output, this, value);
            return output;
        }

        private static bool IsHole(double value)
        {
            return double.IsNaN(value);
        }

        public TimeSeries IsLess(TimeSeries series)
        {
            TimeSeries output = null;
            new ComparisonLessHelper().Calculate(ref output, this, series);
            return output;
        }

        public TimeSeries IsLess(double value)
        {
            TimeSeries output = null;
            new ComparisonLessHelper().Calculate(ref output, this, value);
            return output;
        }

        public TimeSeries IsLessEqual(TimeSeries series)
        {
            TimeSeries output = null;
            new ComparisonLessEqualHelper().Calculate(ref output, this, series);
            return output;
        }

        public TimeSeries IsLessEqual(double value)
        {
            TimeSeries output = null;
            new ComparisonLessEqualHelper().Calculate(ref output, this, value);
            return output;
        }

        public TimeSeries IsNotEqual(TimeSeries series)
        {
            TimeSeries output = null;
            new ComparisonNotEqualHelper().Calculate(ref output, this, series);
            return output;
        }

        public TimeSeries IsNotEqual(double value)
        {
            TimeSeries output = null;
            new ComparisonNotEqualHelper().Calculate(ref output, this, value);
            return output;
        }

        public TimeSeries IsRising()
        {
            TimeSeries output = null;
            new ComparisonLessHelper().Calculate(ref output, this.ShiftRight(1), this);
            return output;
        }

        public TimeSeries Lower(TimeSeries series)
        {
            TimeSeries output = null;
            new LowerHelper().Calculate(ref output, this, series);
            return output;
        }

        public TimeSeries Multiply(TimeSeries series)
        {
            TimeSeries output = null;
            new MultiplicationHelper().Calculate(ref output, this, series);
            return output;
        }

        public TimeSeries Multiply(double value)
        {
            TimeSeries output = null;
            new MultiplicationHelper().Calculate(ref output, this, value);
            return output;
        }

        public TimeSeries Negate()
        {
            return this.Multiply((double)-1.0);
        }

        public static TimeSeries operator +(TimeSeries series1, TimeSeries series2)
        {
            if (series1 == null || series2 == null)
                return null;

            return series1.Add(series2);
        }

        public static TimeSeries operator +(TimeSeries series, double value)
        {
            return series.Add(value);
        }

        public static TimeSeries operator +(double value, TimeSeries series)
        {
            TimeSeries output = null;
            new AdditionHelper().Calculate(ref output, value, series);
            return output;
        }

        public static TimeSeries operator /(TimeSeries series1, TimeSeries series2)
        {
            return series1.Divide(series2);
        }

        public static TimeSeries operator /(TimeSeries series, double value)
        {
            return series.Divide(value);
        }

        public static TimeSeries operator /(double value, TimeSeries series)
        {
            TimeSeries output = null;
            new DivisionHelper().Calculate(ref output, value, series);
            return output;
        }

        public static TimeSeries operator >(TimeSeries series1, TimeSeries series2)
        {
            return series1.IsGreater(series2);
        }

        public static TimeSeries operator >(TimeSeries series, double value)
        {
            return series.IsGreater(value);
        }

        public static TimeSeries operator >(double value, TimeSeries series)
        {
            TimeSeries output = null;
            new ComparisonGreaterHelper().Calculate(ref output, value, series);
            return output;
        }

        public static TimeSeries operator >=(TimeSeries series1, TimeSeries series2)
        {
            return series1.IsGreaterEqual(series2);
        }

        public static TimeSeries operator >=(TimeSeries series, double value)
        {
            return series.IsGreaterEqual(value);
        }

        public static TimeSeries operator >=(double value, TimeSeries series)
        {
            TimeSeries output = null;
            new ComparisonGreaterEqualHelper().Calculate(ref output, value, series);
            return output;
        }

        public static TimeSeries operator <(TimeSeries series1, TimeSeries series2)
        {
            return series1.IsLess(series2);
        }

        public static TimeSeries operator <(TimeSeries series, double value)
        {
            return series.IsLess(value);
        }

        public static TimeSeries operator <(double value, TimeSeries series)
        {
            TimeSeries output = null;
            new ComparisonLessHelper().Calculate(ref output, value, series);
            return output;
        }

        public static TimeSeries operator <=(TimeSeries series1, TimeSeries series2)
        {
            return series1.IsLessEqual(series2);
        }

        public static TimeSeries operator <=(TimeSeries series, double value)
        {
            return series.IsLessEqual(value);
        }

        public static TimeSeries operator <=(double value, TimeSeries series)
        {
            TimeSeries output = null;
            new ComparisonLessEqualHelper().Calculate(ref output, value, series);
            return output;
        }

        public static TimeSeries operator *(TimeSeries series1, TimeSeries series2)
        {
            return series1.Multiply(series2);
        }

        public static TimeSeries operator *(TimeSeries series, double value)
        {
            if (series == null)
                return null;
            return series.Multiply(value);
        }

        public static TimeSeries operator *(double value, TimeSeries series)
        {
            TimeSeries output = null;
            new MultiplicationHelper().Calculate(ref output, value, series);
            return output;
        }

        public static TimeSeries operator -(TimeSeries series1, TimeSeries series2)
        {
            return series1.Subtract(series2);
        }

        public static TimeSeries operator -(TimeSeries series, double value)
        {
            return series.Subtract(value);
        }

        public static TimeSeries operator -(double value, TimeSeries series)
        {
            TimeSeries output = null;
            new SubtractionHelper().Calculate(ref output, value, series);
            return output;
        }

        public TimeSeries Or(TimeSeries series)
        {
            TimeSeries output = null;
            new ComparisonOrHelper().Calculate(ref output, this, series);
            return output;
        }

        public TimeSeries RemoveNaNInfinity()
        {
            int count = this.Count;
            if (count == 0 || this._data == null)
                return this;

            bool hasprob = false;
            double[] numArray = this._data.Data;
            for (int i = 0; i < numArray.Length; i++)
            {
                double d = numArray[i];
                if (double.IsNaN(d) || double.IsInfinity(d))
                {
                    hasprob = true;
                    break;
                }
            }

            if (!hasprob)
                return this;

            //if (count <= 0)
            //{
            //    return new TimeSeries(this);
            //}
            IList<double> data = new List<double>();
            List<DateTime> dtlist = new List<DateTime>();

            for (int i = 0; i < count; i++)
            {
                double d = numArray[i];
                DateTime dt = this.DateTimes[i];
                float f = (float)d;
                if ((!double.IsNaN(d) && !double.IsInfinity(d)) && (!float.IsNaN(f) && !float.IsInfinity(f)))
                {
                    data.Add(d);
                    dtlist.Add(dt);
                }
            }
            return new TimeSeries(data, new DateTimeList(dtlist));
        }

        public TimeSeries Replace(double oldValue, double newValue)
        {
            if (double.IsNaN(oldValue))
            {
                return this;//this.ReplaceNaN(newValue);
            }
            int count = this.Count;
            if (count <= 0)
            {
                return new TimeSeries(this);
            }
            DenseVector data = new DenseVector(count);
            TimeSeries series = new TimeSeries(new List<double>(data), this.DateTimes);//, this.DateTimeIndex, false, true);
            double[] numArray1 = series._data.Data;
            double[] numArray = this._data.Data;
            for (int i = 0; i < count; i++)
            {
                double num3 = numArray[i];
                if (IsHole(num3))
                {
                    series[i] = num3;
                }
                else if (num3 == oldValue)
                {
                    series[i] = newValue;
                }
                else
                {
                    series[i] = num3;
                }
            }
            return series;
        }

        public TimeSeries ReplaceNaN(double newValue)
        {
            int count = this.Count;
            if (count <= 0)
            {
                return this;// new TimeSeries(this);
            }
            DenseVector data = new DenseVector(count);
            //TimeSeries series = new TimeSeries(data, this.DateTimes, this.DateTimeIndex, false, true);
            TimeSeries series = new TimeSeries(new List<double>(data), this.DateTimes);//, this.DateTimeIndex, false, true);
            double[] numArray1 = series._data.Data;
            double[] numArray = this._data.Data;
            for (int i = 0; i < count; i++)
            {
                double d = numArray[i];
                if (double.IsNaN(d))
                {
                    series[i] = newValue;
                }
                else
                {
                    series[i] = d;
                }
            }
            return series;
        }

        public TimeSeries ReplaceNaNInfinity(double newValue)
        {
            int count = this.Count;
            if (count <= 0)
            {
                return this;//new TimeSeries(this);
            }
            DenseVector data = new DenseVector(count);
            //TimeSeries series = new TimeSeries(data, this.DateTimes, this.DateTimeIndex, false, true);
            TimeSeries series = new TimeSeries(new List<double>(data), this.DateTimes);//, this.DateTimeIndex, false, true);
            double[] numArray1 = series._data.Data;
            double[] numArray = this._data.Data;
            for (int i = 0; i < count; i++)
            {
                double d = numArray[i];
                if (double.IsNaN(d) || double.IsInfinity(d))
                {
                    series[i] = newValue;
                }
                else
                {
                    series[i] = d;
                }
            }
            return series;
        }

        public TimeSeries SetCeiling(double value)
        {
            TimeSeries output = null;
            new LowerHelper().Calculate(ref output, this, value);
            return output;
        }

        public TimeSeries SetFloor(double value)
        {
            TimeSeries output = null;
            new HigherHelper().Calculate(ref output, this, value);
            return output;
        }

        public TimeSeries ShiftLeft(int amount)
        {
            IList<int> list;
            TimeSeries series = Alignment.RemoveHoles(this, out list);
            int count = series.Count;
            DateTimeList dateTimes = series.DateTimes;
            TimeSeries series2 = new TimeSeries(count, dateTimes);
            int num2 = amount;
            int num3 = 0;
            while (num3 < count)
            {
                series2[num3] = ((num2 >= 0) && (num2 < count)) ? series[num2] : double.NaN;
                num3++;
                num2++;
            }
            return Alignment.AddHoles(series2, list);
        }

        public TimeSeries ShiftRight(int amount)
        {
            return this.ShiftLeft(-amount);
        }

        public TimeSeries Subtract(TimeSeries series)
        {
            TimeSeries output = null;
            new SubtractionHelper().Calculate(ref output, this, series);
            return output;
        }

        public TimeSeries Subtract(double value)
        {
            TimeSeries output = null;
            new SubtractionHelper().Calculate(ref output, this, value);
            return output;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.Data.GetEnumerator();
        }

        public double[] ToArray()
        {
            if (this._data == null)
            {
                return new double[0];
            }
            return this._data.ToArray();
        }

        // Properties        
        public int Count
        {
            get
            {
                if (this._data != null)
                {
                    return this._data.Count - _bufferCount;
                }
                return 0;
            }
        }

        public Vector Data
        {
            get
            {
                if (this._data == null)
                {
                    throw new NullReferenceException("Attempted to access TimeSeries data with null data.");
                }
                return this._data;
            }
        }

        public DateTimeList DateTimes
        {
            get
            {
                return this._dateTimes;
            }
            set
            {
                if (value != null)
                {
                    if (value.Count < this.Count)
                    {
                        throw new ApplicationException("DateTimeList contains fewer indices than values");
                    }
                    this._dateTimes = new DateTimeList(value);
                }
            }
        }

        public Dictionary<DateTime, int> DateTimeIndex
        {
            get
            {
                return this._dateIndex;
            }
        }


        public double this[int index]
        {
            get
            {
                if (this._data == null)
                {
                    throw new NullReferenceException("Attempted to access TimeSeries with no data.");
                }
                return this._data[index];
            }
            set
            {
                if (this._data == null)
                {
                    throw new NullReferenceException("Attempted to access TimeSeries with no data.");
                }
                this._data[index] = value;
            }
        }

        // Arturo 2010/10/10
        public double this[DateTime date, DateSearchType type]
        {
            get
            {
                int idx = -100;
                //try
                {
                    idx = GetClosestDateIndex(date, type);
                    if (idx == -1)
                        return double.NaN;

                    return this[idx];
                }
                //catch (Exception e)
                {
                    //SystemLog.Write("ERROR:" + idx + " | " + e);
                    //throw e;
                }
            }
        }

        public double this[DateTime date]
        {
            get
            {
                if (this._data == null)
                {
                    return double.NaN;
                    //throw new NullReferenceException("Attempted to access TimeSeries with no data.");
                }

                if (this._dateIndex == null)
                {
                    this._dateIndex = new Dictionary<DateTime, int>();

                    int data_length = Count;

                    for (int i = 0; i < data_length; i++)
                    {
                        this._dateIndex[this._dateTimes[i]] = i;
                    }

                }
                if (!this._dateIndex.ContainsKey(date))
                    return double.NaN;

                return this._data[this._dateIndex[date]];
            }
            set
            {
                if (this._data == null)
                {
                    throw new NullReferenceException("Attempted to access TimeSeries with no data.");
                }

                if (this._dateIndex == null)
                {
                    this._dateIndex = new Dictionary<DateTime, int>();

                    int data_length = Count;

                    for (int i = 0; i < data_length; i++)
                        this._dateIndex[this._dateTimes[i]] = i;
                }
                this._data[this._dateIndex[date]] = value;
            }
        }


        public readonly object objLock = new object();
        public void AddDataPoint(DateTime date, double value)
        {
            lock (objLock)
            {

                if (this.ContainsDate(date))
                {
                    this._data[_dateIndex[date]] = value;
                    return;
                    //throw new Exception("Date Already Exists: " + this[date]);
                }
                else if (this.Count == 1 && (this.DateTimes[0] == DateTime.MinValue || this.DateTimes[0] == new DateTime(1950, 1, 1)))
                //else if (_bufferCount == 0 && (this.DateTimes[0] == DateTime.MinValue || this.DateTimes[0] == new DateTime(1950, 1, 1)))
                {
                    //_bufferCount
                    //this._data[0] = value;
                    //this._dateIndex[]

                    int size = 1;
                    DateTimeList dateTimes = new DateTimeList(new List<DateTime>() { date });

                    if ((dateTimes != null) && (dateTimes.Count < size))
                    {
                        throw new ApplicationException("DateTimeList contains fewer dates than number of points");
                    }
                    //this._data = (size > 0) ? new DenseVector(size, double.NaN) : null;
                    this._data[0] = value;
                    this._dateTimes = (dateTimes != null) ? new DateTimeList(dateTimes._dateTimes) : null;

                    if (this._dateTimes != null)
                    {
                        int length = _dateTimes.Count;
                        this._dateIndex = null;
                        if (this._dateIndex == null)
                        {
                            this._dateIndex = new Dictionary<DateTime, int>();

                            for (int i = 0; i < length; i++)
                                this._dateIndex[this._dateTimes[i]] = i;
                        }
                    }

                    //_bufferCount = 1;

                    return;
                }



                //int increase = 500;// 260;
                int increase = 260;

                if (_bufferCount == 0)
                {
                    int length = Count + increase;

                    DenseVector dv = new DenseVector(length, double.NaN);
                    DateTimeList dates = new DateTimeList(length);

                    for (int i = 0; i < length - increase; i++)
                    {
                        dv[i] = _data[i];
                        dates[i] = _dateTimes[i];
                    }

                    dv[Count] = value;
                    dates[Count] = date;

                    this._data = dv;
                    this._dateTimes = dates;

                    _bufferCount = increase - 1;
                }
                else
                {
                    this._data[Count] = value;
                    this._dateTimes[Count] = date;
                    _bufferCount--;
                }

                _bufferCount = Math.Max(0, _bufferCount);

                if (this._dateIndex == null)
                {
                    this._dateIndex = new Dictionary<DateTime, int>();

                    for (int i = 0; i < Count; i++)
                        this._dateIndex[this._dateTimes[i]] = i;
                }
                else
                    this._dateIndex.Add(date, Count - 1);
            }
        }

        public void RemoveDataPoint(DateTime date)
        {
            if (this._data == null)
                return;

            int index = GetDateIndex(date);
            if (index < 0)
                return;

            this._data[index] = double.NaN;
            this._dateTimes[index] = DateTime.MinValue;
            //int outd = 0;
            this._dateIndex.Remove(date);//, out outd);
            this._bufferCount++;
        }

        public int GetDateIndex(DateTime date)
        {
            if (this._data == null)
                return -1;//throw new NullReferenceException("Attempted to access TimeSeries with no data.");

            if (this._dateIndex == null)
            {
                this._dateIndex = new Dictionary<DateTime, int>();

                //int data_length = this._data.Count;
                int data_length = Count;// this._data.Count;

                for (int i = 0; i < data_length; i++)
                    this._dateIndex[this._dateTimes[i]] = i;
            }

            if (this._dateIndex.ContainsKey(date))
                return this._dateIndex[date];
            else
                return -1;
        }

        public int GetClosestDateIndex(DateTime date, DateSearchType type)
        {
            if (this._data == null)
                return -1;

            if (Count == 0)
                return -1;

            int res = GetDateIndex(date);
            if (res != -1)
                return res;

            int lastN = Count - 1;
            int startN = 0;

            DateTime lastDate = _dateTimes[Count - 1];
            DateTime firstDate = _dateTimes[0];

            if (date >= lastDate)
                return Count - 1;

            if (date < firstDate)
                return -1;
            //return GetDateIndex(lastDate);

            int midN = (int)((lastN + startN) / 2);

            for (int i = Count - 1; i >= 0; i--) // RUN THROUGH ENTIRE SET FROM END
            {
                if (_dateTimes[midN] < date)
                    startN = midN;
                else
                    lastN = midN;


                midN = (int)((lastN + startN) / 2);

                if (startN == midN || lastN == midN)
                    if (type == DateSearchType.Previous)
                        return startN;
                    else
                        return startN + 1;
            }


            return -1;
        }

        public int GetClosestDateIndex2(DateTime date, DateSearchType type)
        {
            if (this._data == null)
                return -1;

            if (Count == 0)
                return -1;

            int res = GetDateIndex(date);
            if (res != -1)
                return res;
            if (type == DateSearchType.Previous)
            {
                int lastN = Count - 1;
                int startN = 0;

                DateTime lastDate = _dateTimes[Count - 1];

                if (date >= lastDate)
                    return Count - 1;
                //return GetDateIndex(lastDate);

                //int midN = (int)((lastN + startN) / 2);

                for (int i = Count - 1; i >= 0; i--) // RUN THROUGH ENTIRE SET FROM END
                {
                    if (_dateTimes[i] <= date)
                        return i;


                    //DateTime mid = _dateTimes[midN];
                    //DateTime start = _dateTimes[startN];
                    //DateTime last = _dateTimes[lastN];

                    //if (_dateTimes[midN] < date)
                    //    startN = midN;
                    //else
                    //    lastN = midN;


                    //midN = (int)((lastN + startN) / 2);
                    ////DateTime mid2 = _dateTimes[midN];
                    ////DateTime startN1 = _dateTimes[startN + 1];

                    //if (startN == midN || lastN == midN)
                    //    if (type == DateSearchType.Previous)
                    //        return startN;
                    //    else
                    //        return startN + 1;
                }
            }
            else // NEXT
            {
                int endN = Count - 1;

                DateTime lastDate = _dateTimes[Count - 1];

                res = GetDateIndex(date);
                if (res != -1)
                    return res;



                for (int i = endN; i >= 0; i--) // RUN THROUGH ENTIRE SET FROM END
                {
                    if (_dateTimes[i] < date)
                        return i + 1;
                }
            }

            return -1;
        }


        public bool ContainsDate(DateTime date)
        {
            if (this._dateIndex != null)
                return this._dateIndex.ContainsKey(date);
            else
                return false;
        }

        public double Maximum
        {
            get
            {
                return AQI.AQILabs.Kernel.Numerics.Math.Statistics.Statistics.Maximum(this.RemoveNaNInfinity()._data.Data);
            }
        }

        public double Mean
        {
            get
            {
                return AQI.AQILabs.Kernel.Numerics.Math.Statistics.Statistics.Mean(this.RemoveNaNInfinity()._data.Data);
            }
        }

        public double Median
        {
            get
            {
                return AQI.AQILabs.Kernel.Numerics.Math.Statistics.Statistics.Median(this.RemoveNaNInfinity()._data.Data);
            }
        }

        public double Minimum
        {
            get
            {
                return AQI.AQILabs.Kernel.Numerics.Math.Statistics.Statistics.Minimum(this.RemoveNaNInfinity()._data.Data);
            }
        }

        public double StdDev
        {
            get
            {
                return AQI.AQILabs.Kernel.Numerics.Math.Statistics.Statistics.StandardDeviation(this.RemoveNaNInfinity()._data.Data);
            }
        }
        public double AnnualVolatility
        {
            get
            {
                double sum = 0.0;
                int counter = 0;
                for (int i = 1; i < this.Count; i++)
                {
                    DateTime date_1 = this.DateTimes[i - 1];
                    double value_1 = this[i - 1];

                    DateTime date = this.DateTimes[i];
                    double value = this[i];

                    if (!double.IsNaN(value) && !double.IsNaN(value_1))
                    {
                        double dt = (date - date_1).TotalDays / 252.0;
                        sum += Math.Pow(value / value_1 - 1.0, 2.0) / dt;
                        counter++;
                    }
                }
                if (counter > 0)
                    sum /= counter;

                return Math.Sqrt(sum);
            }
        }
        public double QuadraticVariation
        {
            get
            {
                return AQI.AQILabs.Kernel.Numerics.Math.Statistics.Statistics.QuadraticVariation(this.RemoveNaNInfinity()._data.Data);
            }
        }

        public Vector Values
        {
            get
            {
                return this.Data;
            }
        }

        public double Variance
        {
            get
            {
                TimeSeries ts = this.RemoveNaNInfinity();
                if (ts == null || ts.Count <= 1)
                    return 0.0;

                return AQI.AQILabs.Kernel.Numerics.Math.Statistics.Statistics.Variance(ts._data.Data);
            }
        }

        // Nested Types
        private class AbsHelper : TimeSeries.CalculationHelper
        {
            // Methods
            private double Calculate(double seriesValue)
            {
                return Math.Abs(seriesValue);
            }

            protected override void SeriesCalcLoop(double[] outputData, double[] seriesData, int numLoops)
            {
                for (int i = 0; i < numLoops; i++)
                {
                    double num2 = seriesData[i];
                    if (TimeSeries.IsHole(num2))
                    {
                        outputData[i] = num2;
                    }
                    else
                    {
                        outputData[i] = this.Calculate(num2);
                    }
                }
            }
        }

        private class AdditionHelper : TimeSeries.CalculationHelper
        {
            // Methods
            private double Calculate(double lhsValue, double rhsValue)
            {
                return (lhsValue + rhsValue);
            }

            protected override void TsTsCalcLoop(double[] outputData, double[] lhsData, double[] rhsData, int numLoops)
            {
                for (int i = 0; i < numLoops; i++)
                {
                    double num2 = lhsData[i];
                    double num3 = rhsData[i];
                    bool flag = TimeSeries.IsHole(num2);
                    bool flag2 = TimeSeries.IsHole(num3);
                    if (flag || flag2)
                    {
                        outputData[i] = flag ? num2 : num3;
                    }
                    else
                    {
                        outputData[i] = this.Calculate(num2, num3);
                    }
                }
            }

            protected override void TsValCalcLoop(double[] outputData, double[] lhsData, double rhsValue, int numLoops)
            {
                for (int i = 0; i < numLoops; i++)
                {
                    double num2 = lhsData[i];
                    if (TimeSeries.IsHole(num2))
                    {
                        outputData[i] = num2;
                    }
                    else
                    {
                        outputData[i] = this.Calculate(num2, rhsValue);
                    }
                }
            }

            protected override void ValTsCalcLoop(double[] outputData, double lhsValue, double[] rhsData, int numLoops)
            {
                for (int i = 0; i < numLoops; i++)
                {
                    double num2 = rhsData[i];
                    if (TimeSeries.IsHole(num2))
                    {
                        outputData[i] = num2;
                    }
                    else
                    {
                        outputData[i] = this.Calculate(lhsValue, num2);
                    }
                }
            }
        }

        private abstract class CalculationHelper
        {
            // Methods
            protected CalculationHelper()
            {
            }

            public void Calculate(ref TimeSeries output, TimeSeries series)
            {
                int size = (series != null) ? series.Count : 0;
                int num2 = (output != null) ? output.Count : 0;
                if (size <= 0)
                {
                    output = null;
                }
                else
                {
                    if (num2 != size)
                    {
                        DenseVector vector = new DenseVector(size);
                        //output = new TimeSeries(vector, series.DateTimes, series.DateTimeIndex, false, true);
                        output = new TimeSeries(new List<double>(vector), series.DateTimes);//, this.DateTimeIndex, false, true);
                        num2 = size;
                    }
                    double[] data = output._data.Data;
                    double[] seriesData = series._data.Data;
                    this.SeriesCalcLoop(data, seriesData, size);
                }
            }

            public void Calculate(ref TimeSeries output, TimeSeries lhs, TimeSeries rhs)
            {
                int num = (lhs != null) ? lhs.Count : 0;
                int num2 = (rhs != null) ? rhs.Count : 0;
                int num3 = (output != null) ? output.Count : 0;
                int numLoops = Math.Min(num, num2);
                int size = Math.Max(num, num2);
                if (numLoops <= 0)
                {
                    output = null;
                }
                else
                {
                    if (num3 != size)
                    {
                        DenseVector vector = new DenseVector(size);
                        //output = new TimeSeries(vector, lhs.DateTimes, lhs.DateTimeIndex, false, true);
                        output = new TimeSeries(new List<double>(vector), lhs.DateTimes);//, lhs.DateTimeIndex, false, true);
                        num3 = size;
                    }
                    double[] data = output._data.Data;
                    double[] lhsData = lhs._data.Data;
                    double[] rhsData = rhs._data.Data;
                    this.TsTsCalcLoop(data, lhsData, rhsData, numLoops);
                    if (numLoops < size)
                    {
                        data.SetValue((double)1.0 / (double)0.0, numLoops, size - 1);
                    }
                }
            }

            public void Calculate(ref TimeSeries output, TimeSeries lhs, double rhs)
            {
                int size = (lhs != null) ? lhs.Count : 0;
                int num2 = (output != null) ? output.Count : 0;
                if (size <= 0)
                {
                    output = null;
                }
                else
                {
                    if (num2 != size)
                    {
                        DenseVector vector = new DenseVector(size);
                        //output = new TimeSeries(vector, lhs.DateTimes, lhs.DateTimeIndex, false, true);
                        output = new TimeSeries(new List<double>(vector), lhs.DateTimes); //, lhs.DateTimeIndex, false, true);
                        num2 = size;
                    }
                    double[] data = output._data.Data;
                    double[] lhsData = lhs._data.Data;
                    this.TsValCalcLoop(data, lhsData, rhs, size);
                }
            }

            public void Calculate(ref TimeSeries output, double lhs, TimeSeries rhs)
            {
                int size = (rhs != null) ? rhs.Count : 0;
                int num2 = (output != null) ? output.Count : 0;
                if (size <= 0)
                {
                    output = null;
                }
                else
                {
                    if (num2 != size)
                    {
                        DenseVector vector = new DenseVector(size);
                        //output = new TimeSeries(vector, rhs.DateTimes, rhs.DateTimeIndex, false, true);
                        output = new TimeSeries(new List<double>(vector), rhs.DateTimes);//, rhs.DateTimeIndex, false, true);
                        num2 = size;
                    }
                    double[] data = output._data.Data;
                    double[] rhsData = rhs._data.Data;
                    this.ValTsCalcLoop(data, lhs, rhsData, size);
                }
            }

            protected virtual void SeriesCalcLoop(double[] outputData, double[] seriesData, int numLoops)
            {
                throw new InvalidOperationException();
            }

            protected virtual void TsTsCalcLoop(double[] outputData, double[] lhsData, double[] rhsData, int numLoops)
            {
                throw new InvalidOperationException();
            }

            protected virtual void TsValCalcLoop(double[] outputData, double[] lhsData, double rhsValue, int numLoops)
            {
                throw new InvalidOperationException();
            }

            protected virtual void ValTsCalcLoop(double[] outputData, double lhsValue, double[] rhsData, int numLoops)
            {
                throw new InvalidOperationException();
            }
        }

        private class ComparisonAndHelper : TimeSeries.CalculationHelper
        {
            // Methods
            private double Calculate(double lhsValue, double rhsValue)
            {
                return (((lhsValue != 0.0) && (rhsValue != 0.0)) ? ((double)1) : ((double)0));
            }

            protected override void TsTsCalcLoop(double[] outputData, double[] lhsData, double[] rhsData, int numLoops)
            {
                for (int i = 0; i < numLoops; i++)
                {
                    double num2 = lhsData[i];
                    double num3 = rhsData[i];
                    bool flag = TimeSeries.IsHole(num2);
                    bool flag2 = TimeSeries.IsHole(num3);
                    if (flag || flag2)
                    {
                        outputData[i] = flag ? num2 : num3;
                    }
                    else
                    {
                        outputData[i] = this.Calculate(num2, num3);
                    }
                }
            }
        }

        private class ComparisonEqualHelper : TimeSeries.CalculationHelper
        {
            // Methods
            private double Calculate(double lhsValue, double rhsValue)
            {
                return ((lhsValue == rhsValue) ? ((double)1) : ((double)0));
            }

            protected override void TsTsCalcLoop(double[] outputData, double[] lhsData, double[] rhsData, int numLoops)
            {
                for (int i = 0; i < numLoops; i++)
                {
                    double num2 = lhsData[i];
                    double num3 = rhsData[i];
                    bool flag = TimeSeries.IsHole(num2);
                    bool flag2 = TimeSeries.IsHole(num3);
                    if (flag || flag2)
                    {
                        outputData[i] = flag ? num2 : num3;
                    }
                    else
                    {
                        outputData[i] = this.Calculate(num2, num3);
                    }
                }
            }

            protected override void TsValCalcLoop(double[] outputData, double[] lhsData, double rhsValue, int numLoops)
            {
                for (int i = 0; i < numLoops; i++)
                {
                    double num2 = lhsData[i];
                    if (TimeSeries.IsHole(num2))
                    {
                        outputData[i] = num2;
                    }
                    else
                    {
                        outputData[i] = this.Calculate(num2, rhsValue);
                    }
                }
            }

            protected override void ValTsCalcLoop(double[] outputData, double lhsValue, double[] rhsData, int numLoops)
            {
                for (int i = 0; i < numLoops; i++)
                {
                    double num2 = rhsData[i];
                    if (TimeSeries.IsHole(num2))
                    {
                        outputData[i] = num2;
                    }
                    else
                    {
                        outputData[i] = this.Calculate(lhsValue, num2);
                    }
                }
            }
        }

        private class ComparisonGreaterEqualHelper : TimeSeries.CalculationHelper
        {
            // Methods
            private double Calculate(double lhsValue, double rhsValue)
            {
                return ((lhsValue >= rhsValue) ? ((double)1) : ((double)0));
            }

            protected override void TsTsCalcLoop(double[] outputData, double[] lhsData, double[] rhsData, int numLoops)
            {
                for (int i = 0; i < numLoops; i++)
                {
                    double num2 = lhsData[i];
                    double num3 = rhsData[i];
                    bool flag = TimeSeries.IsHole(num2);
                    bool flag2 = TimeSeries.IsHole(num3);
                    if (flag || flag2)
                    {
                        outputData[i] = flag ? num2 : num3;
                    }
                    else
                    {
                        outputData[i] = this.Calculate(num2, num3);
                    }
                }
            }

            protected override void TsValCalcLoop(double[] outputData, double[] lhsData, double rhsValue, int numLoops)
            {
                for (int i = 0; i < numLoops; i++)
                {
                    double num2 = lhsData[i];
                    if (TimeSeries.IsHole(num2))
                    {
                        outputData[i] = num2;
                    }
                    else
                    {
                        outputData[i] = this.Calculate(num2, rhsValue);
                    }
                }
            }

            protected override void ValTsCalcLoop(double[] outputData, double lhsValue, double[] rhsData, int numLoops)
            {
                for (int i = 0; i < numLoops; i++)
                {
                    double num2 = rhsData[i];
                    if (TimeSeries.IsHole(num2))
                    {
                        outputData[i] = num2;
                    }
                    else
                    {
                        outputData[i] = this.Calculate(lhsValue, num2);
                    }
                }
            }
        }

        private class ComparisonGreaterHelper : TimeSeries.CalculationHelper
        {
            // Methods
            private double Calculate(double lhsValue, double rhsValue)
            {
                return ((lhsValue > rhsValue) ? ((double)1) : ((double)0));
            }

            protected override void TsTsCalcLoop(double[] outputData, double[] lhsData, double[] rhsData, int numLoops)
            {
                for (int i = 0; i < numLoops; i++)
                {
                    double num2 = lhsData[i];
                    double num3 = rhsData[i];
                    bool flag = TimeSeries.IsHole(num2);
                    bool flag2 = TimeSeries.IsHole(num3);
                    if (flag || flag2)
                    {
                        outputData[i] = flag ? num2 : num3;
                    }
                    else
                    {
                        outputData[i] = this.Calculate(num2, num3);
                    }
                }
            }

            protected override void TsValCalcLoop(double[] outputData, double[] lhsData, double rhsValue, int numLoops)
            {
                for (int i = 0; i < numLoops; i++)
                {
                    double num2 = lhsData[i];
                    if (TimeSeries.IsHole(num2))
                    {
                        outputData[i] = num2;
                    }
                    else
                    {
                        outputData[i] = this.Calculate(num2, rhsValue);
                    }
                }
            }

            protected override void ValTsCalcLoop(double[] outputData, double lhsValue, double[] rhsData, int numLoops)
            {
                for (int i = 0; i < numLoops; i++)
                {
                    double num2 = rhsData[i];
                    if (TimeSeries.IsHole(num2))
                    {
                        outputData[i] = num2;
                    }
                    else
                    {
                        outputData[i] = this.Calculate(lhsValue, num2);
                    }
                }
            }
        }

        private class ComparisonLessEqualHelper : TimeSeries.CalculationHelper
        {
            // Methods
            private double Calculate(double lhsValue, double rhsValue)
            {
                return ((lhsValue <= rhsValue) ? ((double)1) : ((double)0));
            }

            protected override void TsTsCalcLoop(double[] outputData, double[] lhsData, double[] rhsData, int numLoops)
            {
                for (int i = 0; i < numLoops; i++)
                {
                    double num2 = lhsData[i];
                    double num3 = rhsData[i];
                    bool flag = TimeSeries.IsHole(num2);
                    bool flag2 = TimeSeries.IsHole(num3);
                    if (flag || flag2)
                    {
                        outputData[i] = flag ? num2 : num3;
                    }
                    else
                    {
                        outputData[i] = this.Calculate(num2, num3);
                    }
                }
            }

            protected override void TsValCalcLoop(double[] outputData, double[] lhsData, double rhsValue, int numLoops)
            {
                for (int i = 0; i < numLoops; i++)
                {
                    double num2 = lhsData[i];
                    if (TimeSeries.IsHole(num2))
                    {
                        outputData[i] = num2;
                    }
                    else
                    {
                        outputData[i] = this.Calculate(num2, rhsValue);
                    }
                }
            }

            protected override void ValTsCalcLoop(double[] outputData, double lhsValue, double[] rhsData, int numLoops)
            {
                for (int i = 0; i < numLoops; i++)
                {
                    double num2 = rhsData[i];
                    if (TimeSeries.IsHole(num2))
                    {
                        outputData[i] = num2;
                    }
                    else
                    {
                        outputData[i] = this.Calculate(lhsValue, num2);
                    }
                }
            }
        }

        private class ComparisonLessHelper : TimeSeries.CalculationHelper
        {
            // Methods
            private double Calculate(double lhsValue, double rhsValue)
            {
                return ((lhsValue < rhsValue) ? ((double)1) : ((double)0));
            }

            protected override void TsTsCalcLoop(double[] outputData, double[] lhsData, double[] rhsData, int numLoops)
            {
                for (int i = 0; i < numLoops; i++)
                {
                    double num2 = lhsData[i];
                    double num3 = rhsData[i];
                    bool flag = TimeSeries.IsHole(num2);
                    bool flag2 = TimeSeries.IsHole(num3);
                    if (flag || flag2)
                    {
                        outputData[i] = flag ? num2 : num3;
                    }
                    else
                    {
                        outputData[i] = this.Calculate(num2, num3);
                    }
                }
            }

            protected override void TsValCalcLoop(double[] outputData, double[] lhsData, double rhsValue, int numLoops)
            {
                for (int i = 0; i < numLoops; i++)
                {
                    double num2 = lhsData[i];
                    if (TimeSeries.IsHole(num2))
                    {
                        outputData[i] = num2;
                    }
                    else
                    {
                        outputData[i] = this.Calculate(num2, rhsValue);
                    }
                }
            }

            protected override void ValTsCalcLoop(double[] outputData, double lhsValue, double[] rhsData, int numLoops)
            {
                for (int i = 0; i < numLoops; i++)
                {
                    double num2 = rhsData[i];
                    if (TimeSeries.IsHole(num2))
                    {
                        outputData[i] = num2;
                    }
                    else
                    {
                        outputData[i] = this.Calculate(lhsValue, num2);
                    }
                }
            }
        }

        private class ComparisonNotEqualHelper : TimeSeries.CalculationHelper
        {
            // Methods
            private double Calculate(double lhsValue, double rhsValue)
            {
                return ((lhsValue != rhsValue) ? ((double)1) : ((double)0));
            }

            protected override void TsTsCalcLoop(double[] outputData, double[] lhsData, double[] rhsData, int numLoops)
            {
                for (int i = 0; i < numLoops; i++)
                {
                    double num2 = lhsData[i];
                    double num3 = rhsData[i];
                    bool flag = TimeSeries.IsHole(num2);
                    bool flag2 = TimeSeries.IsHole(num3);
                    if (flag || flag2)
                    {
                        outputData[i] = flag ? num2 : num3;
                    }
                    else
                    {
                        outputData[i] = this.Calculate(num2, num3);
                    }
                }
            }

            protected override void TsValCalcLoop(double[] outputData, double[] lhsData, double rhsValue, int numLoops)
            {
                for (int i = 0; i < numLoops; i++)
                {
                    double num2 = lhsData[i];
                    if (TimeSeries.IsHole(num2))
                    {
                        outputData[i] = num2;
                    }
                    else
                    {
                        outputData[i] = this.Calculate(num2, rhsValue);
                    }
                }
            }

            protected override void ValTsCalcLoop(double[] outputData, double lhsValue, double[] rhsData, int numLoops)
            {
                for (int i = 0; i < numLoops; i++)
                {
                    double num2 = rhsData[i];
                    if (TimeSeries.IsHole(num2))
                    {
                        outputData[i] = num2;
                    }
                    else
                    {
                        outputData[i] = this.Calculate(lhsValue, num2);
                    }
                }
            }
        }

        private class ComparisonOrHelper : TimeSeries.CalculationHelper
        {
            // Methods
            private double Calculate(double lhsValue, double rhsValue)
            {
                return (((lhsValue != 0.0) || (rhsValue != 0.0)) ? ((double)1) : ((double)0));
            }

            protected override void TsTsCalcLoop(double[] outputData, double[] lhsData, double[] rhsData, int numLoops)
            {
                for (int i = 0; i < numLoops; i++)
                {
                    double num2 = lhsData[i];
                    double num3 = rhsData[i];
                    bool flag = TimeSeries.IsHole(num2);
                    bool flag2 = TimeSeries.IsHole(num3);
                    if (flag || flag2)
                    {
                        outputData[i] = flag ? num2 : num3;
                    }
                    else
                    {
                        outputData[i] = this.Calculate(num2, num3);
                    }
                }
            }
        }

        private class DivisionHelper : TimeSeries.CalculationHelper
        {
            // Methods
            private double Calculate(double lhsValue, double rhsValue)
            {
                return (lhsValue / rhsValue);
            }

            protected override void TsTsCalcLoop(double[] outputData, double[] lhsData, double[] rhsData, int numLoops)
            {
                for (int i = 0; i < numLoops; i++)
                {
                    double num2 = lhsData[i];
                    double num3 = rhsData[i];
                    bool flag = TimeSeries.IsHole(num2);
                    bool flag2 = TimeSeries.IsHole(num3);
                    if (flag || flag2)
                    {
                        outputData[i] = flag ? num2 : num3;
                    }
                    else
                    {
                        outputData[i] = this.Calculate(num2, num3);
                    }
                }
            }

            protected override void TsValCalcLoop(double[] outputData, double[] lhsData, double rhsValue, int numLoops)
            {
                for (int i = 0; i < numLoops; i++)
                {
                    double num2 = lhsData[i];
                    if (TimeSeries.IsHole(num2))
                    {
                        outputData[i] = num2;
                    }
                    else
                    {
                        outputData[i] = this.Calculate(num2, rhsValue);
                    }
                }
            }

            protected override void ValTsCalcLoop(double[] outputData, double lhsValue, double[] rhsData, int numLoops)
            {
                for (int i = 0; i < numLoops; i++)
                {
                    double num2 = rhsData[i];
                    if (TimeSeries.IsHole(num2))
                    {
                        outputData[i] = num2;
                    }
                    else
                    {
                        outputData[i] = this.Calculate(lhsValue, num2);
                    }
                }
            }
        }

        private class HigherHelper : TimeSeries.CalculationHelper
        {
            // Methods
            private double Calculate(double lhsValue, double rhsValue)
            {
                return Math.Max(lhsValue, rhsValue);
            }

            protected override void TsTsCalcLoop(double[] outputData, double[] lhsData, double[] rhsData, int numLoops)
            {
                for (int i = 0; i < numLoops; i++)
                {
                    double num2 = lhsData[i];
                    double num3 = rhsData[i];
                    bool flag = TimeSeries.IsHole(num2);
                    bool flag2 = TimeSeries.IsHole(num3);
                    if (flag || flag2)
                    {
                        outputData[i] = flag ? num2 : num3;
                    }
                    else
                    {
                        outputData[i] = this.Calculate(num2, num3);
                    }
                }
            }

            protected override void TsValCalcLoop(double[] outputData, double[] lhsData, double rhsValue, int numLoops)
            {
                for (int i = 0; i < numLoops; i++)
                {
                    double num2 = lhsData[i];
                    if (TimeSeries.IsHole(num2))
                    {
                        outputData[i] = num2;
                    }
                    else
                    {
                        outputData[i] = this.Calculate(num2, rhsValue);
                    }
                }
            }
        }

        private class LowerHelper : TimeSeries.CalculationHelper
        {
            // Methods
            private double Calculate(double lhsValue, double rhsValue)
            {
                return Math.Min(lhsValue, rhsValue);
            }

            protected override void TsTsCalcLoop(double[] outputData, double[] lhsData, double[] rhsData, int numLoops)
            {
                for (int i = 0; i < numLoops; i++)
                {
                    double num2 = lhsData[i];
                    double num3 = rhsData[i];
                    bool flag = TimeSeries.IsHole(num2);
                    bool flag2 = TimeSeries.IsHole(num3);
                    if (flag || flag2)
                    {
                        outputData[i] = flag ? num2 : num3;
                    }
                    else
                    {
                        outputData[i] = this.Calculate(num2, num3);
                    }
                }
            }

            protected override void TsValCalcLoop(double[] outputData, double[] lhsData, double rhsValue, int numLoops)
            {
                for (int i = 0; i < numLoops; i++)
                {
                    double num2 = lhsData[i];
                    if (TimeSeries.IsHole(num2))
                    {
                        outputData[i] = num2;
                    }
                    else
                    {
                        outputData[i] = this.Calculate(num2, rhsValue);
                    }
                }
            }
        }

        private class MultiplicationHelper : TimeSeries.CalculationHelper
        {
            // Methods
            private double Calculate(double lhsValue, double rhsValue)
            {
                return (lhsValue * rhsValue);
            }

            protected override void TsTsCalcLoop(double[] outputData, double[] lhsData, double[] rhsData, int numLoops)
            {
                for (int i = 0; i < numLoops; i++)
                {
                    double num2 = lhsData[i];
                    double num3 = rhsData[i];
                    bool flag = TimeSeries.IsHole(num2);
                    bool flag2 = TimeSeries.IsHole(num3);
                    if (flag || flag2)
                    {
                        outputData[i] = flag ? num2 : num3;
                    }
                    else
                    {
                        outputData[i] = this.Calculate(num2, num3);
                    }
                }
            }

            protected override void TsValCalcLoop(double[] outputData, double[] lhsData, double rhsValue, int numLoops)
            {
                for (int i = 0; i < numLoops; i++)
                {
                    double num2 = lhsData[i];
                    if (TimeSeries.IsHole(num2))
                    {
                        outputData[i] = num2;
                    }
                    else
                    {
                        outputData[i] = this.Calculate(num2, rhsValue);
                    }
                }
            }

            protected override void ValTsCalcLoop(double[] outputData, double lhsValue, double[] rhsData, int numLoops)
            {
                for (int i = 0; i < numLoops; i++)
                {
                    double num2 = rhsData[i];
                    if (TimeSeries.IsHole(num2))
                    {
                        outputData[i] = num2;
                    }
                    else
                    {
                        outputData[i] = this.Calculate(lhsValue, num2);
                    }
                }
            }
        }

        private class SubtractionHelper : TimeSeries.CalculationHelper
        {
            // Methods
            private double Calculate(double lhsValue, double rhsValue)
            {
                return (lhsValue - rhsValue);
            }

            protected override void TsTsCalcLoop(double[] outputData, double[] lhsData, double[] rhsData, int numLoops)
            {
                for (int i = 0; i < numLoops; i++)
                {
                    double num2 = lhsData[i];
                    double num3 = rhsData[i];
                    bool flag = TimeSeries.IsHole(num2);
                    bool flag2 = TimeSeries.IsHole(num3);
                    if (flag || flag2)
                    {
                        outputData[i] = flag ? num2 : num3;
                    }
                    else
                    {
                        outputData[i] = this.Calculate(num2, num3);
                    }
                }
            }

            protected override void TsValCalcLoop(double[] outputData, double[] lhsData, double rhsValue, int numLoops)
            {
                for (int i = 0; i < numLoops; i++)
                {
                    double num2 = lhsData[i];
                    if (TimeSeries.IsHole(num2))
                    {
                        outputData[i] = num2;
                    }
                    else
                    {
                        outputData[i] = this.Calculate(num2, rhsValue);
                    }
                }
            }

            protected override void ValTsCalcLoop(double[] outputData, double lhsValue, double[] rhsData, int numLoops)
            {
                for (int i = 0; i < numLoops; i++)
                {
                    double num2 = rhsData[i];
                    if (TimeSeries.IsHole(num2))
                    {
                        outputData[i] = num2;
                    }
                    else
                    {
                        outputData[i] = this.Calculate(lhsValue, num2);
                    }
                }
            }
        }
        #endregion

        #region AQI Members
        public TimeSeries Synchronize(TimeSeries secondary)
        {
            return Synchronize(secondary, TimeSeriesSynchronizeMethod.Latest);
        }

        public TimeSeries Synchronize(TimeSeries secondary, TimeSeriesSynchronizeMethod method)
        {
            if (method == TimeSeriesSynchronizeMethod.Union)
            {
                List<DateTime> dates = new List<DateTime>();
                for (int i = 0; i < this.DateTimes.Count - 1; i++)
                {
                    if (!dates.Contains(this.DateTimes[i]))
                    {
                        dates.Add(this.DateTimes[i]);
                    }
                }

                for (int i = 0; i < secondary.DateTimes.Count - 1; i++)
                {
                    if (!dates.Contains(secondary.DateTimes[i]))
                    {
                        dates.Add(secondary.DateTimes[i]);
                    }
                }

                List<DateTime> dates_union = new List<DateTime>();
                for (int i = 0; i < dates.Count; i++)
                {
                    DateTime date = dates[i];
                    if (this.DateTimes._dateTimes.Contains(date) && secondary.DateTimes._dateTimes.Contains(date) && !(dates_union.Contains(date)))
                    {
                        dates_union.Add(date);
                    }
                }

                TimeSeries res = new TimeSeries(dates_union.Count, new DateTimeList(dates_union.ToArray()));
                for (int i = 0; i < res.Count; i++)
                {
                    res[i] = secondary[res.DateTimes[i]];
                }

                return res;
            }
            else
            {
                TimeSeries res = new TimeSeries(this.Count, this.DateTimes);
                if (secondary.Count == 0)
                    return res;
                int j = 0;
                double sv = double.NaN;
                DateTime st = secondary.DateTimes[j];
                for (int i = 0; i < this.Count; i++)
                {
                    if (method == TimeSeriesSynchronizeMethod.Exact)
                        sv = double.NaN;
                    int jn = -1;
                    DateTime pt = this.DateTimes[i];
                    while ((j != jn) && ((j + 1) < secondary.Count))
                    {
                        DateTime stn = secondary.DateTimes[j + 1];
                        if (stn <= pt)
                        {
                            j++;
                            sv = secondary[j];
                            st = stn;
                        }
                        else
                            jn = j;
                    }
                    res[i] = sv;
                }
                return res;
            }
        }

        public TimeSeries Synchronize(List<TimeSeries> list, TimeSeriesSynchronizeMethod method)
        {
            if (method == TimeSeriesSynchronizeMethod.Union)
            {
                List<DateTime> dates = new List<DateTime>();
                int list_length = list.Count;

                for (int i = 0; i < this.DateTimes.Count - 1; i++)
                {
                    if (!dates.Contains(this.DateTimes[i]))
                    {
                        dates.Add(this.DateTimes[i]);
                    }
                }

                for (int j = 0; j < list_length; j++)
                {
                    TimeSeries secondary = list[j];
                    for (int i = 0; i < secondary.DateTimes.Count - 1; i++)
                    {
                        if (!dates.Contains(secondary.DateTimes[i]))
                        {
                            dates.Add(secondary.DateTimes[i]);
                        }
                    }
                }

                List<DateTime> dates_union = new List<DateTime>();
                for (int i = 0; i < dates.Count; i++)
                {
                    DateTime date = dates[i];

                    bool flag = true;
                    for (int j = 0; j < list_length; j++)
                    {
                        TimeSeries secondary = list[j];
                        if (!secondary.DateTimes._dateTimes.Contains(date))
                        {
                            flag = false;
                            break;
                        }
                    }


                    if (this.DateTimes._dateTimes.Contains(date) && flag && !(dates_union.Contains(date)))
                    {
                        dates_union.Add(date);
                    }
                }

                TimeSeries res = new TimeSeries(dates_union.Count, new DateTimeList(dates_union.ToArray()));
                for (int i = 0; i < res.Count; i++)
                {
                    res[i] = this[res.DateTimes[i]];
                }

                return res;
            }
            else if (method == TimeSeriesSynchronizeMethod.Latest)
            {
                List<DateTime> dates = new List<DateTime>();
                int list_length = list.Count;

                for (int i = 0; i < this.DateTimes.Count - 1; i++)
                {
                    if (!dates.Contains(this.DateTimes[i]))
                    {
                        dates.Add(this.DateTimes[i]);
                    }
                }

                for (int j = 0; j < list_length; j++)
                {
                    TimeSeries secondary = list[j];
                    for (int i = 0; i < secondary.DateTimes.Count - 1; i++)
                    {
                        if (!dates.Contains(secondary.DateTimes[i]))
                        {
                            dates.Add(secondary.DateTimes[i]);
                        }
                    }
                }

                List<DateTime> dates_union = new List<DateTime>();
                for (int i = 0; i < dates.Count; i++)
                {
                    DateTime date = dates[i];

                    bool flag = true;
                    for (int j = 0; j < list_length; j++)
                    {
                        TimeSeries secondary = list[j];
                        if (!secondary.DateTimes._dateTimes.Contains(date))
                        {
                            flag = false;
                            break;
                        }
                    }


                    if (this.DateTimes._dateTimes.Contains(date) && flag && !(dates_union.Contains(date)))
                    {
                        dates_union.Add(date);
                    }
                }

                dates_union.Sort();
                DateTime firstDate = dates_union[0];

                List<DateTime> lastDates = new List<DateTime>();
                foreach (DateTime dt in dates)
                {
                    if (dt >= firstDate && !lastDates.Contains(dt))
                    {
                        lastDates.Add(dt);
                    }
                }

                TimeSeries res = new TimeSeries(dates_union.Count, new DateTimeList(lastDates.ToArray()));
                for (int i = 0; i < res.Count; i++)
                {
                    res[i] = this[res.DateTimes[i]];
                }

                return res;
            }
            else
            {
                throw new Exception("Synchronize Methods not Implemented");
            }
        }

        public TimeSeries Log()
        {
            TimeSeries res = new TimeSeries(this);
            for (int i = 0; i < Count; i++)
            {
                double v = this[i];
                if (!double.IsNaN(v))
                    if (v > 0.0)
                        res[i] = Math.Log(v);
                    else
                        res[i] = double.NaN;
                else
                    res[i] = double.NaN;
            }
            return res;
        }

        public TimeSeries LogReturn()
        {

            List<double> vals = new List<double>();
            List<DateTime> dates = new List<DateTime>();

            for (int i = 1; i < this.Count; i++)
            {
                DateTime t = _dateTimes[i];
                dates.Add(t);

                double d = Math.Log(this[i] / this[i - 1]);

                if (!Double.IsNaN(d) && !Double.IsInfinity(d))
                    vals.Add(d);

            }
            return new TimeSeries(vals, new DateTimeList(dates));

            TimeSeries res = new TimeSeries(this);// this.Log();
            //TimeSeries res = this.Log();
            double v0 = res[0];
            res[0] = 0.0;
            for (int i = 1; i < res.Count; i++)
            {
                double v = res[i];
                if (!double.IsNaN(v))
                {
                    res[i] = Math.Log(v / v0);
                    //res[i] = v - v0;
                    v0 = v;
                }
                else
                    res[i] = double.NaN;
            }
            return res.GetRange(1, res.Count - 1);
        }

        public TimeSeries RatioReturn()
        {
            List<double> vals = new List<double>();
            List<DateTime> dates = new List<DateTime>();

            for (int i = 1; i < this.Count; i++)
            {
                DateTime t = _dateTimes[i];
                dates.Add(t);

                double d = this[i] / this[i - 1] - 1.0;

                if (!Double.IsNaN(d) && !Double.IsInfinity(d))
                    vals.Add(d);

            }
            return new TimeSeries(vals, new DateTimeList(dates));

            TimeSeries res = new TimeSeries(this);
            for (int i = 1; i < res.Count; i++)
                res[i] = this[i] / this[i - 1] - 1.0;

            return res.GetRange(1, res.Count - 1);
        }

        public TimeSeries DifferenceReturn()
        {
            List<double> vals = new List<double>();
            List<DateTime> dates = new List<DateTime>();

            for (int i = 1; i < this.Count; i++)
            {
                DateTime t = _dateTimes[i];
                dates.Add(t);

                double d = this[i] - this[i - 1];

                if (!Double.IsNaN(d) && !Double.IsInfinity(d))
                    vals.Add(d);
                else
                    vals.Add(0.0);

            }

            return new TimeSeries(vals, new DateTimeList(dates));

            TimeSeries res = new TimeSeries(this);

            for (int i = 1; i < res.Count; i++)
                res[i] = this[i] - this[i - 1];

            return res.GetRange(1, res.Count - 1);
        }


        public TimeSeries TotalReturn(TimeSeries dividends)
        {
            TimeSeries divadj;
            if (this.DateTimes.CompareTo(dividends.DateTimes) != 0)
                divadj = Synchronize(dividends, TimeSeriesSynchronizeMethod.Exact).ReplaceNaN(0.0);
            else
                divadj = dividends.ReplaceNaN(0.0);
            TimeSeries res = new TimeSeries(this);
            int i = 0;
            while (double.IsNaN(res[i]))
                i++;
            double v = res[i++];
            while (i < res.Count)
            {
                v = v * (this[i] / this[i - 1]) + divadj[i];
                res[i] = v;
                i++;
            }
            return res;
        }

        public TimeSeries GetEndOfMonths()
        {
            Dictionary<int, int> dict = new Dictionary<int, int>();
            for (int i = 0; i < this.DateTimes.Count; i++)
            {
                DateTime t = this.DateTimes[i];
                int k = 100 * t.Year + t.Month;
                if (dict.ContainsKey(k))
                {
                    if (t > this.DateTimes[dict[k]])
                        dict[k] = i;
                }
                else
                {
                    dict.Add(k, i);
                }
            }
            List<double> vals = new List<double>();
            List<DateTime> dates = new List<DateTime>();
            foreach (var i in dict.Values)
            {
                vals.Add(this[i]);
                dates.Add(this.DateTimes[i]);
            }
            TimeSeries res = new TimeSeries(vals);
            res.DateTimes = new DateTimeList(dates);
            return res;
        }

        public enum IntervalType
        {
            Second = 1, Minute = 2, Hour = 3, Day = 4
        };

        public TimeSeries GetIntervals(int interval, IntervalType type)
        {
            Dictionary<int, int> dict = new Dictionary<int, int>();

            DateTime firstDate = this.DateTimes[0];
            DateTime lastDate = this.DateTimes[this.Count - 1];

            DateTime date = firstDate.Date.AddHours(firstDate.Hour).AddMinutes(firstDate.Minute).AddSeconds(firstDate.Second);
            if (type == IntervalType.Minute)
                date = firstDate.Date.AddHours(firstDate.Hour).AddMinutes(firstDate.Minute).AddSeconds(0);
            else if (type == IntervalType.Hour)
                date = firstDate.Date.AddHours(firstDate.Hour).AddMinutes(0).AddSeconds(0);
            else if (type == IntervalType.Day)
                date = firstDate.Date;

            List<double> vals = new List<double>();
            List<DateTime> dates = new List<DateTime>();

            while (date <= lastDate)
            {
                date = date < firstDate ? firstDate : date;
                vals.Add(this[date, DateSearchType.Previous]);
                dates.Add(date);

                if (type == IntervalType.Second)
                    date = date.AddSeconds(interval);
                else if (type == IntervalType.Minute)
                    date = date.AddMinutes(interval);
                else if (type == IntervalType.Hour)
                    date = date.AddHours(interval);
                else if (type == IntervalType.Day)
                    date = date.AddDays(interval);
            }
            TimeSeries res = new TimeSeries(vals);
            res.DateTimes = new DateTimeList(dates);
            return res;
        }

        public TimeSeries GetSparseSubset(int distance)
        {
            List<DateTime> dates = new List<DateTime>();
            for (int i = this.DateTimes.Count - 1; i >= 0; i -= distance)
                dates.Add(this.DateTimes[i]);

            List<double> vals = new List<double>();

            foreach (var i in dates)
                vals.Add(this[i]);

            TimeSeries res = new TimeSeries(vals);
            res.DateTimes = new DateTimeList(dates);
            return res;
        }

        public enum RangeFillType
        {
            None = 0, Days = 1
        };

        public TimeSeries GetRange(DateTime start, DateTime end, RangeFillType type)
        {
            if (_dateTimes == null)
                throw new ArgumentException("Timeseries is missing dates");
            //List<double> vals = new List<double>();
            //List<DateTime> dates = new List<DateTime>();

            TimeSeries res = new TimeSeries();

            if (type == RangeFillType.None)
            {
                for (int i = 0; i < this.Count; i++)
                {
                    DateTime t = _dateTimes[i];
                    if ((t >= start) && (t <= end) && !Double.IsNaN(_data[i]) && !Double.IsInfinity(_data[i]))
                        res.AddDataPoint(t, _data[i]);

                    if (t > end)
                        break;
                }
            }
            else
            {
                for (DateTime t = start; t <= end; t = t.AddDays(1))
                {
                    if ((t >= start) && (t <= end))
                    {
                        double d = this[t, DateSearchType.Previous];
                        if (!Double.IsInfinity(d) && !Double.IsNaN(d))
                            res.AddDataPoint(t, d);
                    }

                    if (t > end)
                        break;
                }
            }

            return res;//.RemoveNaNInfinity();
        }


        public TimeSeries GetRange(int start, int end)
        {
            if (_dateTimes == null)
                throw new ArgumentException("Timeseries is missing dates");
            List<double> vals = new List<double>();
            List<DateTime> dates = new List<DateTime>();

            //TimeSeries res = new TimeSeries(1);



            for (int i = start; i <= end; i++)
            {
                DateTime t = _dateTimes[i];
                dates.Add(t);

                if (!Double.IsNaN(_data[i]) && !Double.IsInfinity(_data[i]))
                {

                    vals.Add(_data[i]);


                    //res.AddDataPoint(t, _data[i]);
                }
                else
                    vals.Add(0.0);
                //res.AddDataPoint(t, 0.0);
            }

            //TimeSeries res = new TimeSeries(vals);
            //res.DateTimes = new DateTimeList(dates);
            //return res.RemoveNaNInfinity();
            return new TimeSeries(vals, new DateTimeList(dates));
            //return res;//.ReplaceNaNInfinity(0.0);
        }

        public double[] MaxMin(int start, int end)
        {
            if (_dateTimes == null)
                throw new ArgumentException("Timeseries is missing dates");

            double max = _data[start];
            double min = max;

            for (int i = start; i <= end; i++)
            {
                double v = _data[i];
                max = Math.Max(v, max);
                min = Math.Min(v, min);
            }

            return new double[] { max, min };
        }

        public double Percentile(double value)
        {
            int m = _data.Count;
            double count = 0;
            for (int i = 0; i < m; i++)
                if (value >= _data[i])
                    count++;
            return count / m;
        }
        #endregion

        #region IEnumerable<TimeSeriesItem> Members

        IEnumerator<TimeSeriesItem> IEnumerable<TimeSeriesItem>.GetEnumerator()
        {
            return new TimeSeriesEnumerator(this);
        }

        #endregion

    }

    public class TimeSeriesEnumerator : IEnumerator<TimeSeriesItem>
    {
        private int index;
        private TimeSeries ts;

        public TimeSeriesEnumerator(TimeSeries ats)
        {
            ((IEnumerator)this).Reset();
            ts = ats;
        }

        #region IEnumerator<TimeSeriesItem> Members

        TimeSeriesItem IEnumerator<TimeSeriesItem>.Current
        {
            get
            {
                return new TimeSeriesItem(ts, index);
            }
        }

        #endregion

        #region IDisposable Members

        void IDisposable.Dispose()
        {
        }

        #endregion

        #region IEnumerator Members

        object IEnumerator.Current
        {
            get { return index; }
        }

        bool IEnumerator.MoveNext()
        {
            index++;
            return index < ts.Data.Count;
        }

        void IEnumerator.Reset()
        {
            index = -1;
        }

        #endregion
    }

    public class TimeSeriesItem
    {
        private TimeSeries ts;
        private int index;

        public TimeSeriesItem(TimeSeries ats, int aindex)
        {
            ts = ats;
            index = aindex;
        }

        public DateTime TimeStamp
        {
            get
            {
                if (ts.DateTimes == null)
                    return DateTime.MinValue;
                return ts.DateTimes[index];
            }
            set
            {
                if (ts.DateTimes == null)
                    throw new InvalidOperationException();
                ts.DateTimes[index] = value;
            }
        }

        public double Value
        {
            get
            {
                return ts.Data[index];
            }
            set
            {
                ts.Data[index] = value;
            }
        }
    }

    public class TimeSeriesCollection : IEnumerable
    {
        public TimeSeriesCollection()
        {
            tss = new List<TimeSeries>();
            names = new List<string>();
            //currs = new List<string>();
            namedict = new Dictionary<string, int>();
            DateTimes = null;
        }

        public TimeSeriesCollection(DateTimeList dates, int nSeries)
        {
            tss = new List<TimeSeries>();
            names = new List<string>();
            //currs = new List<string>();
            namedict = new Dictionary<string, int>();
            DateTimes = null;

            for (int i = 0; i < nSeries; i++)
            {
                TimeSeries ts = new TimeSeries(dates.Count, dates);
                tss.Add(ts);
            }
        }

        private List<TimeSeries> tss;
        private List<string> names;
        //private List<string> currs;
        private Dictionary<string, int> namedict;

        public DateTimeList DateTimes { get; set; }

        public void Add(string name, TimeSeries ts)
        {
            if (tss.Count > 0)
            {
                /*if (DateTimes == null)
                {
                    if (ts.DateTimes != null)
                        throw new ArgumentException("All timeseries must be without dates, if first timeseries is missing dates");
                }
                else
                {
                    if (ts.DateTimes == null)
                        throw new ArgumentException("All timeseries must have dates, if first timeseries has dates");
                    //if (DateTimes.CompareTo(ts.DateTimes)!=0)
                    if (DateTimes.Count!=ts.DateTimes.Count)
                        throw new ArgumentException("Size of timeseries doesn´t match");
                }*/
                if (ts.Count != tss[0].Count)
                    throw new ArgumentException("All timeseries must be of same length");
            }
            names.Add(name);
            //currs.Add(curr);
            tss.Add(ts);
            if ((DateTimes == null) && (ts.DateTimes != null))
                DateTimes = new DateTimeList(ts.DateTimes);
            namedict.Add(name, names.Count - 1);
        }

        public int Count
        {
            get
            {
                return tss.Count;
            }
        }

        public List<string> Names
        {
            get
            {
                return names;
            }
        }

        public TimeSeries this[int index]
        {
            get
            {
                return tss[index];
            }
            set
            {
                tss[index] = value;
            }
        }

        public double this[int index, int t]
        {
            get
            {
                return tss[index][t];
            }
            set
            {
                tss[index][t] = value;
            }
        }

        public TimeSeries this[string name]
        {
            get
            {
                int index = namedict[name];
                return tss[index];
            }
            set
            {
                int index = namedict[name];
                tss[index] = value;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return tss.GetEnumerator();
        }
    }
}