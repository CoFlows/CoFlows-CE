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
    public class CashFlowGroup : Strategy
    {
        public enum MemoryType
        {
            Amount = 1, StartDate = 2, EndDate = 3, DiscountType = 4, Frequency = 5
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
        private DateTime _startDate = DateTime.MinValue;
        private DateTime _endDate = DateTime.MinValue;
        private InterestRateTenorType _frequency = InterestRateTenorType.Daily;

        public double Amount
        {
            get
            {
                return _amount;
            }
            set
            {
                this._amount = value;
                this.AddMemoryPoint(DateTime.MinValue, value, -(int)MemoryType.Amount, false);
            }
        }
        public DateTime StartDate
        {
            get
            {
                return _startDate;
            }
            set
            {
                this._startDate = value;
                this.AddMemoryPoint(DateTime.MinValue, value.ToOADate(), -(int)MemoryType.StartDate, false);
            }
        }
        public DateTime EndDate
        {
            get
            {
                return _endDate;
            }
            set
            {
                this._endDate = value;
                this.AddMemoryPoint(DateTime.MinValue, value.ToOADate(), -(int)MemoryType.EndDate, false);
            }
        }
        public InterestRateTenorType Frequency
        {
            get
            {
                return _frequency;
            }
            set
            {
                this._frequency = value;
                this.AddMemoryPoint(DateTime.MinValue, (int)value, -(int)MemoryType.Frequency, false);
            }
        }

        public Dictionary<DiscountType, double> Discounts()
        {
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
            this.AddMemoryPoint(DateTime.MinValue, value, (int)dtype, false);
        }
        public double Discount(DiscountType dtype)
        {
            double _discount = this[DateTime.MinValue, (int)dtype, TimeSeriesRollType.Last];
            if (double.IsNaN(_discount) || _discount == double.MinValue || _discount == double.MaxValue)
                return 0.0;

            return _discount;
        }

        public double NPV(BusinessDay businessDay)
        {
            double val = 0.0;

            foreach (CashFlow flow in CashFlows(businessDay.DateTime).Values.ToList())
                val += flow.NPV(businessDay);

            return val;
        }

        public void AddCashFlow(CashFlow cashFlow, DateTime date)
        {
            this.AddInstrument(cashFlow, date);
            this.Portfolio.CreatePosition(cashFlow, date, 1.0, cashFlow.NPV(Calendar.GetBusinessDay(date)));
        }
        public Dictionary<DateTime, CashFlow> CashFlows(DateTime date)
        {
            Dictionary<int, Instrument> instruments = Instruments(date, false);
            Dictionary<DateTime, CashFlow> _cashflows = new Dictionary<DateTime, CashFlow>();

            if (instruments != null)
                foreach (int id in instruments.Keys.ToList())
                {
                    Instrument i = instruments[id];
                    CashFlow cf = i as CashFlow;
                    if (!_cashflows.ContainsKey(cf.Date))
                        _cashflows.Add(cf.Date, cf);
                }

            return _cashflows;
        }
        public void RemoveCashFlow(CashFlow cashFlow, DateTime date)
        {
            this.RemoveInstrument(cashFlow, DateTime.MinValue);

            Position position = this.Portfolio.FindPosition(cashFlow, date);
            if (position != null)
                position.UpdatePosition(date, 0.0, cashFlow.NPV(Calendar.GetBusinessDay(date)), RebalancingType.Reserve, UpdateType.OverrideUnits);

            cashFlow.Remove();
        }

        public void GenerateCashFlows(DateTime creationDate)
        {
            BusinessDay date = Calendar.GetClosestBusinessDay(StartDate, TimeSeries.DateSearchType.Previous);
            BusinessDay end = Calendar.GetClosestBusinessDay(EndDate, TimeSeries.DateSearchType.Next);

            BusinessDay expiryDate = date;

            for (int i = 1; expiryDate.DateTime <= end.DateTime; i++)
            {
                switch (Frequency)
                {
                    case InterestRateTenorType.Daily:
                        expiryDate = date.AddActualDays(i, TimeSeries.DateSearchType.Next);
                        break;
                    case InterestRateTenorType.Weekly:
                        expiryDate = date.AddActualDays(7 * i, TimeSeries.DateSearchType.Next);
                        break;
                    case InterestRateTenorType.Monthly:
                        expiryDate = date.AddMonths(i, TimeSeries.DateSearchType.Next);
                        break;
                    case InterestRateTenorType.Yearly:
                        expiryDate = date.AddYears(i, TimeSeries.DateSearchType.Next);
                        break;
                    default:
                        break;
                }

                CashFlow cf = CashFlow.CreateCashFlow(Name + "-" + expiryDate, Currency, Calendar.GetBusinessDay(creationDate), Amount, expiryDate, this, SimulationObject);
                this.AddCashFlow(cf, creationDate);

                //date = expiryDate;
            }
        }

        public void ClearCashFlows(DateTime date)
        {
            Dictionary<DateTime, CashFlow> cashflows = CashFlows(date);
            foreach (CashFlow cf in cashflows.Values)
                this.RemoveCashFlow(cf, date);
        }

        public CashFlowGroup(Instrument instrument)
            : base(instrument) { }

        public CashFlowGroup(int id)
            : base(id) { }

        public override void Initialize()
        {
            if (Initialized)
                return;

            _amount = this[DateTime.Now, -(int)MemoryType.Amount, TimeSeriesRollType.Last];
            _startDate = DateTime.FromOADate((long)this[DateTime.Now, -(int)MemoryType.StartDate, TimeSeriesRollType.Last]);
            _endDate = DateTime.FromOADate((long)this[DateTime.Now, -(int)MemoryType.EndDate, TimeSeriesRollType.Last]);
            _frequency = (InterestRateTenorType)this[DateTime.Now, -(int)MemoryType.Frequency, TimeSeriesRollType.Last];

            base.Initialize();
        }

        public override double NAVCalculation(BusinessDay date)
        {
            double val = this.Portfolio[date.DateTime, TimeSeriesType.Last, TimeSeriesRollType.Last];
            CommitNAVCalculation(date, val, TimeSeriesType.Last);
            return val;
        }

        public static CashFlowGroup CreateCashFlowGroup(string name, Currency ccy, BusinessDay initialDate, double amount, BusinessDay startDate, BusinessDay endDate, InterestRateTenorType ttype, bool simulated, Portfolio parent)
        {
            Instrument instrument = Instrument.CreateInstrument(name, InstrumentType.Strategy, name, ccy, FundingType.TotalReturn, simulated);

            return CreateStrategy(instrument, initialDate, amount, startDate, endDate, ttype, parent);
        }
        public static CashFlowGroup CreateStrategy(Instrument instrument, BusinessDay initialDate, double amount, BusinessDay startDate, BusinessDay endDate, InterestRateTenorType ttype, Portfolio parent)
        {
            if (instrument.InstrumentType == InstrumentType.Strategy)
            {
                CashFlowGroup Strategy = new CashFlowGroup(instrument);
                Strategy.TimeSeriesRoll = TimeSeriesRollType.Last;
                Strategy.Amount = amount;
                Strategy.StartDate = startDate.DateTime;
                Strategy.EndDate = endDate.DateTime;
                Strategy.Frequency = ttype;

                Portfolio portfolio = null;

                if (parent != null)
                {
                    Instrument portfolio_instrument = Instrument.CreateInstrument(instrument.Name + "/Portfolio", InstrumentType.Portfolio, instrument.Name + "/Portfolio", instrument.Currency, FundingType.TotalReturn, instrument.SimulationObject);
                    portfolio = Portfolio.CreatePortfolio(portfolio_instrument, parent.LongReserve, parent.ShortReserve, parent);
                    portfolio.TimeSeriesRoll = TimeSeriesRollType.Last;

                    foreach (Instrument reserve in parent.Reserves)
                        portfolio.AddReserve(reserve.Currency, parent.Reserve(reserve.Currency, PositionType.Long), parent.Reserve(reserve.Currency, PositionType.Short));

                    if (parent.Strategy != null)
                        parent.Strategy.AddInstrument(Strategy, initialDate.DateTime);
                }
                else
                {
                    Currency main_currency = instrument.Currency;
                    //ConstantStrategy main_cash_strategy = ConstantStrategy.CreateStrategy(instrument.Name + "/" + main_currency + "/Cash", main_currency, initialDate, 1.0, instrument.SimulationObject);
                    Instrument main_cash_strategy = Instrument.FindInstrument(main_currency.Name + " - Cash");
                    Instrument portfolio_instrument = Instrument.CreateInstrument(instrument.Name + "/Portfolio", InstrumentType.Portfolio, instrument.Name + "/Portfolio", main_currency, FundingType.TotalReturn, instrument.SimulationObject);
                    portfolio = Portfolio.CreatePortfolio(portfolio_instrument, main_cash_strategy, main_cash_strategy, parent);
                    portfolio.TimeSeriesRoll = TimeSeriesRollType.Last;
                    portfolio.AddReserve(main_currency, main_cash_strategy, main_cash_strategy);
                }


                portfolio.Strategy = Strategy;
                Strategy.GenerateCashFlows(initialDate.DateTime);

                double npv = Strategy.NPV(initialDate);

                Strategy.Startup(initialDate, npv, portfolio);
                Strategy.InitialDate = new DateTime(1990, 01, 06);

                double npv2 = Strategy.NPV(initialDate);

                // if (parent != null)
                //    parent.CreatePosition(Strategy, initialDate.DateTime, (Strategy[initialDate.DateTime] == 0.0 ? 0 : 1.0), npv2);

                if (!instrument.SimulationObject)
                {
                    Strategy.Portfolio.MasterPortfolio.Strategy.Tree.SaveNewPositions();
                    Strategy.Portfolio.MasterPortfolio.Strategy.Tree.Save();
                }

                return Strategy;
            }
            else
                throw new Exception("Instrument not a Strategy");
        }
    }
}
