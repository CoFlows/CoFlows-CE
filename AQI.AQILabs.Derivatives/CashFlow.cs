/*
 * The MIT License (MIT)
 * Copyright (c) 2007-2019, Arturo Rodriguez All rights reserved.
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using AQI.AQILabs.Kernel;
using AQI.AQILabs.Kernel.Numerics.Util;

namespace AQI.AQILabs.Derivatives
{
    /// <summary>
    /// Enumeration of possible Discount Types    
    /// </summary>
    public enum DiscountType
    {
        Tax = 0
    };

    public class CashFlow : Strategy
    {
        public enum MemoryType
        {
            Amount = 1, Date = 2, DiscountType = 3, GroupID = 4
        };

        public override string[] MemoryTypeNames()
        {
            return Enum.GetNames(typeof(MemoryType));
        }
        public override int MemoryTypeInt(string name)
        {
            return (int)Enum.Parse(typeof(MemoryType), name);
        }

        private double _amount = 0;
        private DateTime _date = DateTime.MinValue;
        private int _groupID = 0;
        private CashFlowGroup _group = null;

        public double Amount
        {
            get
            {
                if (Group != null)
                    return Group.Amount;

                return _amount;
            }
            set
            {
                if (Group == null)
                {
                    this._amount = value;
                    this.AddMemoryPoint(DateTime.MinValue, value, -(int)MemoryType.Amount, false);
                }
            }
        }
        public DateTime Date
        {
            get
            {
                return _date;
            }
            set
            {
                this._date = value;
                this.AddMemoryPoint(DateTime.MinValue, value.ToOADate(), -(int)MemoryType.Date, false);
            }
        }
        public CashFlowGroup Group
        {
            get
            {
                if (_group == null && _groupID != 0)
                    this._group = Instrument.FindInstrument(_groupID) as CashFlowGroup;
                return _group;
            }
            set
            {
                if (value != null)
                {
                    this._group = value;
                    this._groupID = value.ID;
                    this.AddMemoryPoint(DateTime.MinValue, value.ID, -(int)MemoryType.GroupID, false);
                }
            }
        }

        private IRZeroCurveCollection _curveCollection = null;

        public Dictionary<DiscountType, double> Discounts()
        {
            if (Group != null)
                return this.Group.Discounts();

            Dictionary<DiscountType, double> _discounts = new Dictionary<DiscountType, double>();

            foreach (DiscountType dc in Enum.GetValues(typeof(DiscountType)))
            {
                double _discount = this[DateTime.MinValue, (int)dc, TimeSeriesRollType.Last];
                if (double.IsNaN(_discount) || _discount == double.MinValue || _discount == double.MaxValue)
                    break;

                _discounts.Add(dc, _discount);
            }

            return _discounts;
        }
        public void SetDiscount(DiscountType dtype, double value)
        {
            if (Group != null)
                return;

            this.AddMemoryPoint(DateTime.MinValue, value, (int)dtype, false);
        }
        public double Discount(DiscountType dtype)
        {
            if (Group != null)
                return Group.Discount(dtype);

            double _discount = this[DateTime.MinValue, (int)dtype, TimeSeriesRollType.Last];
            if (double.IsNaN(_discount) || _discount == double.MinValue || _discount == double.MaxValue)
                return 0.0;

            return _discount;
        }

        public double NPV(BusinessDay businessDay)
        {
            IRZeroCurve curve = _curveCollection == null ? null : _curveCollection.GenerateCurve(businessDay);
            return Amount * (curve.PresentValue(this.Calendar.GetClosestBusinessDay(Date, TimeSeries.DateSearchType.Previous)));
        }

        public CashFlow(Instrument instrument)
            : base(instrument)
        {
            _curveCollection = IRZeroCurveCollection.GetCollection(this.Currency);
        }

        public CashFlow(int id)
            : base(id)
        {
            _curveCollection = IRZeroCurveCollection.GetCollection(this.Currency);
        }

        public override void Initialize()
        {
            if (Initialized)
                return;

            _amount = this[DateTime.Now, -(int)MemoryType.Amount, TimeSeriesRollType.Last];
            _date = DateTime.FromOADate((long)this[DateTime.Now, -(int)MemoryType.Date, TimeSeriesRollType.Last]);
            double _groupIDd = (int)this[DateTime.Now, -(int)MemoryType.GroupID, TimeSeriesRollType.Last];

            if (double.IsNaN(_groupIDd) || double.IsInfinity(_groupIDd))
                _groupID = 0;
            else
                _groupID = (int)_groupIDd;

            base.Initialize();
        }

        public override double NAVCalculation(BusinessDay date)
        {
            double val = NPV(date);

            CommitNAVCalculation(date, val, TimeSeriesType.Last);
            return val;
        }

        public static CashFlow CreateCashFlow(string name, Currency ccy, BusinessDay initialDate, double amount, BusinessDay date, CashFlowGroup group, bool simulated)
        {
            Instrument instrument = Instrument.CreateInstrument(name, InstrumentType.Strategy, name, ccy, FundingType.TotalReturn, simulated);
            return CreateStrategy(instrument, initialDate, amount, date, group);
        }
        public static CashFlow CreateStrategy(Instrument instrument, BusinessDay initialDate, double amount, BusinessDay date, CashFlowGroup group)
        {
            if (instrument.InstrumentType == InstrumentType.Strategy)
            {
                CashFlow Strategy = new CashFlow(instrument);

                Strategy.Amount = amount;
                Strategy.Date = date.DateTime;

                //Strategy.AddMemoryPoint(DateTime.MinValue, amount, -(int)MemoryType.Amount);
                //Strategy.AddMemoryPoint(DateTime.MinValue, date.DateTime.ToOADate(), -(int)MemoryType.Date);
                if (group != null)
                    Strategy.Group = group;
                //Strategy.AddMemoryPoint(DateTime.MinValue, group.ID, -(int)MemoryType.GroupID);
                double npv = Strategy.NPV(initialDate);
                Strategy.Startup(initialDate, npv, null);

                Strategy.InitialDate = new DateTime(1990, 01, 06);

                return Strategy;
            }
            else
                throw new Exception("Instrument not a Strategy");
        }
    }
}