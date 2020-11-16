/*
 * The MIT License (MIT)
 * Copyright (c) 2007-2019, Arturo Rodriguez All rights reserved.
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
 
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Data;
using System.Text;

using AQI.AQILabs.Kernel;
using AQI.AQILabs.Kernel.Numerics.Util;

namespace AQI.AQILabs.Derivatives
{
    public struct CashFlowRate
    {
        private InterestRateTenorType _frequencyType;
        private int _frequency;
        private InterestRateTenorType _maturityType;
        private int _maturity;
        private DayCountConvention _dayCount;
        private double _rate;

        public CashFlowRate(InterestRateTenorType frequencyType, int frequency, InterestRateTenorType maturityType, int maturity, DayCountConvention dayCount, double rate)
        {
            this._frequencyType = frequencyType;
            this._frequency = frequency;
            this._maturityType = maturityType;
            this._maturity = maturity;
            this._dayCount = dayCount;
            this._rate = rate;
        }

        public InterestRateTenorType FrequencyType
        {
            get
            {
                return _frequencyType;
            }
            set
            {
                this._frequencyType = value;
            }
        }

        public int Frequency
        {
            get
            {
                return _frequency;
            }
            set
            {
                this._frequency = value;
            }
        }

        public InterestRateTenorType MaturityType
        {
            get
            {
                return _maturityType;
            }
            set
            {
                this._maturityType = value;
            }
        }

        public int Maturity
        {
            get
            {
                return _maturity;
            }
            set
            {
                this._maturity = value;
            }
        }

        public DayCountConvention DayCount
        {
            get
            {
                return _dayCount;
            }
            set
            {
                this._dayCount = value;
            }
        }

        public double Rate
        {
            get
            {
                return _rate;
            }
            set
            {
                this._rate = value;
            }
        }
    }

    public class IRZeroCurve
    {
        private ConcurrentDictionary<double, double> _zeroRates = new ConcurrentDictionary<double, double>();
        private BusinessDay _startDate = null;

        private Dictionary<DateTime, CashFlowRate> _cashFlows = new Dictionary<DateTime, CashFlowRate>();

        public IRZeroCurve(BusinessDay startDate)
        {
            this._startDate = startDate;
        }

        public BusinessDay StartDate
        {
            get
            {
                return _startDate;
            }
        }

        public void AddCashFlow(double rate, InterestRateTenorType frequencyType, int frequency, InterestRateTenorType maturityType, int maturity, DayCountConvention dayCount)
        {
            DateTime expiryDate = new DateTime();
            switch (maturityType)
            {
                case InterestRateTenorType.Daily:
                    expiryDate = _startDate.AddActualDays(maturity, TimeSeries.DateSearchType.Next).DateTime;
                    break;
                case InterestRateTenorType.Weekly:
                    expiryDate = _startDate.AddActualDays(maturity * 7, TimeSeries.DateSearchType.Next).DateTime;
                    break;
                case InterestRateTenorType.Monthly:
                    expiryDate = _startDate.AddMonths(maturity, TimeSeries.DateSearchType.Next).DateTime;
                    break;
                case InterestRateTenorType.Yearly:
                    expiryDate = _startDate.AddYears(maturity, TimeSeries.DateSearchType.Next).DateTime;
                    break;
                default:
                    break;
            }

            CashFlowRate cf = new CashFlowRate(frequencyType, frequency, maturityType, maturity, dayCount, rate);

            if (!_cashFlows.ContainsKey(expiryDate))
            {
                _cashFlows.Add(expiryDate, cf);
            }
        }

        public void BootStrap()
        {
            List<DateTime> dates = _cashFlows.Keys.ToList();
            dates.Sort();
            int length = dates.Count;

            for (int i = 0; i < length; i++)
            {
                CashFlowRate cf = _cashFlows[dates[i]];
                _addCashFlow(cf);
            }
        }

        private void _addCashFlow(CashFlowRate cashFlow)
        {
            // Console.WriteLine("Add Cashflow:" + cashFlow.Rate + " " + cashFlow.MaturityType + " " + cashFlow.Maturity + " " + cashFlow.FrequencyType + " " + cashFlow.Frequency + " " + cashFlow.DayCount);

            double rate = cashFlow.Rate;
            InterestRateTenorType frequencyType = cashFlow.FrequencyType;
            int frequency = cashFlow.Frequency;
            InterestRateTenorType maturityType = cashFlow.MaturityType;
            int maturity = cashFlow.Maturity;
            DayCountConvention dayCount = cashFlow.DayCount;

            BusinessDay expiryDate = null;
            switch (maturityType)
            {
                case InterestRateTenorType.Daily:
                    expiryDate = _startDate.AddActualDays(maturity, TimeSeries.DateSearchType.Next);
                    //expiryDate = _startDate.AddActualDays(maturity, BusinessDaySearchType.Next);
                    break;
                case InterestRateTenorType.Weekly:
                    expiryDate = _startDate.AddActualDays(maturity * 7, TimeSeries.DateSearchType.Next);
                    //expiryDate = _startDate.AddActualDays(maturity * 7, BusinessDaySearchType.Next);
                    break;
                case InterestRateTenorType.Monthly:
                    expiryDate = _startDate.AddMonths(maturity, TimeSeries.DateSearchType.Next);
                    //expiryDate = _startDate.AddMonths(maturity, BusinessDaySearchType.Next);
                    break;
                case InterestRateTenorType.Yearly:
                    expiryDate = _startDate.AddYears(maturity, TimeSeries.DateSearchType.Next);
                    //expiryDate = _startDate.AddYears(maturity, BusinessDaySearchType.Next);
                    break;
                default:
                    break;
            }

            BusinessDay runningDate = _startDate;

            if (frequencyType == maturityType && frequency == maturity)
            {
                double t = expiryDate.YearsBetween(_startDate, dayCount);
                double pv = 1 / (1 + rate * t);
                t = expiryDate.YearsBetween(_startDate, DayCountConvention.Act365);
                _zeroRates.TryAdd(t, -Math.Log(pv) / t);
            }
            else
            {
                int counter = 1;
                double pvs = 0;
                double PV = 0;
                double t = 0;

                bool flag = true;
                BusinessDay previousDate = _startDate;

                while (flag)
                {
                    previousDate = runningDate;
                    if (frequencyType == InterestRateTenorType.Daily)
                        runningDate = runningDate.AddBusinessDays(frequency * counter);
                    else if (frequencyType == InterestRateTenorType.Weekly)
                        runningDate = _startDate.AddActualDays(7 * frequency * counter, TimeSeries.DateSearchType.Next);
                    //runningDate = _startDate.AddActualDays(7 * frequency * counter, BusinessDaySearchType.Next);
                    else if (frequencyType == InterestRateTenorType.Monthly)
                        runningDate = _startDate.AddMonths(frequency * counter, TimeSeries.DateSearchType.Next);
                    //runningDate = _startDate.AddMonths(frequency * counter, BusinessDaySearchType.Next);
                    else if (frequencyType == InterestRateTenorType.Yearly)
                        runningDate = _startDate.AddYears(frequency * counter, TimeSeries.DateSearchType.Next);
                    //runningDate = _startDate.AddYears(frequency * counter, BusinessDaySearchType.Next);

                    flag = (runningDate.DateTime < expiryDate.DateTime);

                    if (flag)
                    {
                        counter++;
                        t = runningDate.YearsBetween(previousDate, dayCount);
                        pvs += rate * t * PresentValue(runningDate.YearsBetween(_startDate, DayCountConvention.Act365));
                        t = expiryDate.YearsBetween(runningDate, dayCount);
                        PV = (1 - pvs) / (1 + rate * t);
                    }
                }


                t = expiryDate.YearsBetween(_startDate, DayCountConvention.Act365);
                double zeroRate = -Math.Log(PV) / t;
                _zeroRates.TryAdd(t, zeroRate);
            }
        }

        public double PresentValue(double t)
        {
            double r = ZeroRate(t);
            return Math.Exp(-r * t);
        }

        public double ZeroRate(double t)
        {
            if (t < 0)
                t = 0;

            if (_zeroRates.ContainsKey(t))
                return _zeroRates[t];
            else
            {
                List<double> t_s = _zeroRates.Keys.ToList();
                t_s.Sort();
                int length = t_s.Count;
                int idx = 1;
                for (int i = length - 2; i >= 1; i--)
                {
                    if (t >= t_s[i])
                    {
                        idx = i;
                        break;
                    }
                }

                if (length - 2 < idx)
                    return 0.0;

                double t1 = t_s[idx];
                double t2 = t_s[idx + 1];

                double rate1 = _zeroRates[t1];
                double rate2 = _zeroRates[t2];

                double rate = rate1 + (t - t1) * (rate2 - rate1) / (t2 - t1);

                if(rate < 0)
                    Console.WriteLine("TEST");

                return rate;
            }
        }

        public double PresentValue(BusinessDay day)
        {
            double t = (day.DateTime <= _startDate.DateTime ? 0 : 1) * day.YearsBetween(_startDate, DayCountConvention.Act365);
            return PresentValue(t);
        }

        public double ZeroRate(BusinessDay day)
        {
            double t = (day.DateTime <= _startDate.DateTime ? 0 : 1) * day.YearsBetween(_startDate, DayCountConvention.Act365);

            return ZeroRate(t);
        }

        public double PresentValue(double t, double spread)
        {
            return Math.Exp(-(ZeroRate(t) + spread) * t);
        }

        public double PresentValue(BusinessDay day, double spread)
        {
            double t = day.YearsBetween(_startDate, DayCountConvention.Act365);
            return PresentValue(t, spread);
        }
    }

    public class IRZeroCurveCollection
    {
        private Currency _currency = null;
        private ConcurrentDictionary<DateTime, IRZeroCurve> _curve_dictionary = new ConcurrentDictionary<DateTime, IRZeroCurve>();

        InterestRate[] _interest_rate_lst = null;

        public IRZeroCurveCollection(Currency currency)
        {
            this._currency = currency;
            Initialize();
        }

        private bool _initialized = false;

        public void Initialize()
        {
            if (_initialized)
                return;

            _interest_rate_lst = (from i in Instrument.InstrumentsType(InstrumentType.InterestRateSwap)
                                      //where (i.InstrumentType == InstrumentType.InterestRateSwap || i.InstrumentType == InstrumentType.Deposit) && i.Currency == _currency
                                  where i.Currency == _currency
                                  //select InterestRate.FindInterestRate(i)).ToArray();
                                  select (i as InterestRate)).ToArray();

            _interest_rate_lst = _interest_rate_lst.Concat(from i in Instrument.InstrumentsType(InstrumentType.Deposit)
                                          //where (i.InstrumentType == InstrumentType.InterestRateSwap || i.InstrumentType == InstrumentType.Deposit) && i.Currency == _currency
                                      where i.Currency == _currency
                                      select (i as InterestRate)).ToArray();

            _initialized = true;
        }

        public IRZeroCurve GenerateCurve(BusinessDay currentDate)
        {
            if (_curve_dictionary.ContainsKey(currentDate.DateTime))
                return _curve_dictionary[currentDate.DateTime];

            IRZeroCurve curve = new IRZeroCurve(currentDate);

            foreach (InterestRate rate in _interest_rate_lst)
            {

                try
                {
                    double value = rate[currentDate.DateTime, TimeSeriesType.Last, DataProvider.DefaultProvider, TimeSeriesRollType.Exact];

                    if (double.IsNaN(value))
                        value = rate[Calendar.Close(currentDate.DateTime), TimeSeriesType.Last, DataProvider.DefaultProvider, TimeSeriesRollType.Last];

                    // Console.WriteLine("Generate Curve: " + currentDate.DateTime + " " + rate + " " + value);
                    if (rate.InstrumentType == InstrumentType.Deposit)
                    {
                        Deposit deposit = (rate as Deposit);
                        curve.AddCashFlow(value, deposit.MaturityType, deposit.Maturity, deposit.MaturityType, deposit.Maturity, deposit.DayCountConvention);
                    }
                    else if (rate.InstrumentType == InstrumentType.InterestRateSwap)
                    {
                        InterestRateSwap swap = (rate as InterestRateSwap);
                        curve.AddCashFlow(value, swap.FixedFrequencyType, swap.FixedFrequency, swap.MaturityType, swap.Maturity, swap.FixedDayCountConvention);
                    }
                }
                catch { }

            }

            curve.BootStrap();
            if (_curve_dictionary.ContainsKey(currentDate.DateTime))
                _curve_dictionary[currentDate.DateTime] = curve;
            else
                _curve_dictionary.TryAdd(currentDate.DateTime, curve);

            return curve;
        }

        public IRZeroCurve GetCurve(BusinessDay day)
        {
            if (!_curve_dictionary.ContainsKey(day.DateTime))
                GenerateCurve(day);
            return _curve_dictionary[day.DateTime];
        }

        public Currency Currency
        {
            get
            {
                return _currency;
            }
        }

        private static ConcurrentDictionary<Currency, IRZeroCurveCollection> _collection = new ConcurrentDictionary<Currency, IRZeroCurveCollection>();


        public static IRZeroCurveCollection GetCollection(Currency ccy)
        {

            if (!_collection.ContainsKey(ccy))
                _collection.TryAdd(ccy, new IRZeroCurveCollection(ccy));

            return _collection[ccy];
        }
    }
}