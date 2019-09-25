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

using AQI.AQILabs.Kernel.Numerics.Util;

namespace AQI.AQILabs.Kernel
{
    /// <summary>
    /// Futures class containing relevant information like dates and functions to retrieve active futures, futures chain, etc.
    /// </summary>
    public class Future : Security
    {
        new public static AQI.AQILabs.Kernel.Factories.IFutureFactory Factory = null;

        /// <summary>
        /// Function: Clear the futures from memory where their last trade date is prior to the given date
        /// </summary>
        /// <param name="date">reference date</param>
        public static void CleanFuturesFromMemory(DateTime date)
        {
            Factory.CleanFuturesFromMemory(date);
        }

        /// <summary>
        /// Property containt int value for the unique ID of the instrument
        /// </summary>
        /// <remarks>
        /// Main identifier for each Instrument in the System
        /// </remarks>
        new public int ID
        {
            get
            {
                return base.ID;
            }
        }

        private string _futureGenericMonths = null;
        private DateTime _firstDeliveryDate = DateTime.MinValue;
        private DateTime _firstNoticeDate = DateTime.MinValue;
        private DateTime _lastDeliveryDate = DateTime.MinValue;
        private DateTime _firstTradeDate = DateTime.MinValue;
        private DateTime _lastTradeDate = DateTime.MinValue;
        private double _tickSize = 0.0;
        private double _contractSize = 0.0;
        //private double _contractPointSize = 0.0;
        private int _contractMonth = 0;
        private int _contractYear = 0;

        /// <summary>
        /// Property: ID of the underlying instrument
        /// </summary>
        public int UnderlyingID { get; set; }

        /// <summary>
        /// Property: ID of the next future in the chain
        /// </summary>
        public int NextID { get; set; }

        /// <summary>
        /// Property: ID of the previous future in the chain
        /// </summary>
        public int PreviousID { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        //public Future(Security security, string futureGenericMonths, DateTime firstDeliveryDate, DateTime firstNoticeDate, DateTime lastDeliveryDate, DateTime firstTradeDate, DateTime lastTradeDate, double tickSize, double contractSize, double contractPointSize, int contractMonth, int contractYear, int UnderlyingID, int NextID, int PreviousID)
        public Future(Security security, string futureGenericMonths, DateTime firstDeliveryDate, DateTime firstNoticeDate, DateTime lastDeliveryDate, DateTime firstTradeDate, DateTime lastTradeDate, double tickSize, double contractSize, int contractMonth, int contractYear, int UnderlyingID, int NextID, int PreviousID)
            : base(security, security.Isin, security.Sedol, security.ExchangeID, security.PointSize)
        {
            this._futureGenericMonths = futureGenericMonths;
            this._firstDeliveryDate = firstDeliveryDate;
            this._firstNoticeDate = firstNoticeDate;
            this._lastDeliveryDate = lastDeliveryDate;
            this._firstTradeDate = firstTradeDate;
            this._lastTradeDate = lastTradeDate;
            this._tickSize = tickSize;
            this._contractSize = contractSize;
            //this._contractPointSize = contractPointSize;
            this._contractMonth = contractMonth;
            this._contractYear = contractYear;
            this.UnderlyingID = UnderlyingID;
            this.NextID = NextID;
            this.PreviousID = PreviousID;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        [Newtonsoft.Json.JsonConstructor]
        //public Future(int id, string futureGenericMonths, DateTime firstDeliveryDate, DateTime firstNoticeDate, DateTime lastDeliveryDate, DateTime firstTradeDate, DateTime lastTradeDate, double tickSize, double contractSize, double contractPointSize, int contractMonth, int contractYear, int UnderlyingID, int NextID, int PreviousID)
        public Future(int id, string futureGenericMonths, DateTime firstDeliveryDate, DateTime firstNoticeDate, DateTime lastDeliveryDate, DateTime firstTradeDate, DateTime lastTradeDate, double tickSize, double contractSize, int contractMonth, int contractYear, int UnderlyingID, int NextID, int PreviousID)
            : base(Instrument.FindCleanInstrument(id), Security.FindSecurity(Instrument.FindCleanInstrument(id)).Isin, Security.FindSecurity(Instrument.FindCleanInstrument(id)).Sedol, Security.FindSecurity(Instrument.FindCleanInstrument(id)).ExchangeID, Security.FindSecurity(Instrument.FindCleanInstrument(id)).PointSize)
        {
            this._futureGenericMonths = futureGenericMonths;
            this._firstDeliveryDate = firstDeliveryDate;
            this._firstNoticeDate = firstNoticeDate;
            this._lastDeliveryDate = lastDeliveryDate;
            this._firstTradeDate = firstTradeDate;
            this._lastTradeDate = lastTradeDate;
            this._tickSize = tickSize;
            this._contractSize = contractSize;
            //this._contractPointSize = contractPointSize;
            this._contractMonth = contractMonth;
            this._contractYear = contractYear;
            this.UnderlyingID = UnderlyingID;
            this.NextID = NextID;
            this.PreviousID = PreviousID;
        }

        /// <summary>
        /// Property: string representation of monthly schedule
        /// </summary>
        public string FutureGenericMonths
        {
            get
            {
                return _futureGenericMonths;
            }
            set
            {
                this._futureGenericMonths = value;
                Factory.SetProperty(this, "FutureGenericMonths", value);
            }
        }

        /// <summary>
        /// Property: first date where delivery may happen
        /// </summary>
        public DateTime FirstDeliveryDate
        {
            get
            {
                return _firstDeliveryDate;
            }
            set
            {
                this._firstDeliveryDate = value;
                Factory.SetProperty(this, "FirstDeliveryDate", value);
            }
        }

        /// <summary>
        /// Property: first notice date
        /// The day after which an investor who has purchased a futures contract may be required to take physical delivery of the contract's underlying commodity. First notice day varies by contract; it also depends on exchange rules. If the first business day of the delivery month was Monday, Oct. 1, first notice day would typically fall one to three business days prior, so it could be Wednesday, Sept. 26, Thursday, Sept.27, or Friday, Sept. 28. Most investors close out their positions before first notice day because they don't want to own physical commodities. 
        /// </summary>
        public DateTime FirstNoticeDate
        {
            get
            {
                return this._firstNoticeDate;
            }
            set
            {
                this._firstNoticeDate = value;
                Factory.SetProperty(this, "FirstNoticeDate", value);
            }
        }

        /// <summary>
        /// Property: last date where delivery may happen
        /// </summary>
        public DateTime LastDeliveryDate
        {
            get
            {
                return this._lastDeliveryDate;
            }
            set
            {
                this._lastDeliveryDate = value;
                Factory.SetProperty(this, "LastDeliveryDate", value);
            }
        }

        /// <summary>
        /// Property: first date the contract is traded
        /// </summary>
        public DateTime FirstTradeDate
        {
            get
            {
                return this._firstTradeDate;
            }
            set
            {
                this._firstTradeDate = value;
                Factory.SetProperty(this, "FirstTradeDate", value);
            }
        }

        /// <summary>
        /// Property: last date the contract is traded
        /// </summary>
        public DateTime LastTradeDate
        {
            get
            {
                return this._lastTradeDate;
            }
            set
            {
                this._lastTradeDate = value;
                Factory.SetProperty(this, "LastTradeDate", value);
            }
        }

        /// <summary>
        /// Property: contract tick size.
        /// Minimum price movement of the contract.
        /// </summary>
        public double TickSize
        {
            get
            {
                return _tickSize;
            }
            set
            {
                this._tickSize = value;
                Factory.SetProperty(this, "TickSize", value);
            }
        }

        /// <summary>
        /// Property: contract size.
        /// The deliverable quantity of commodities or financial instruments underlying futures contracts that are traded on an exchange.
        /// </summary>
        public double ContractSize
        {
            get
            {
                if (Name.StartsWith("SP"))
                    return 50;

                return _contractSize;
            }
            set
            {
                this._contractSize = value;
                Factory.SetProperty(this, "ContractSize", value);
            }
        }

        ///// <summary>
        ///// Property: point size.
        ///// Value of one point also called a multiplier
        ///// </summary>
        //public double PointSize
        //{
        //    get
        //    {
        //        if (Name.StartsWith("SP"))
        //            return 50;

        //        return _contractPointSize;
        //    }
        //    set
        //    {
        //        this._contractPointSize = value;
        //        Factory.SetProperty(this, "PointSize", value);
        //    }
        //}

        /// <summary>
        /// Property: Refernece month of the contract
        /// </summary>
        public int ContractMonth
        {
            get
            {
                return _contractMonth;
            }
            set
            {
                this._contractMonth = value;
                Factory.SetProperty(this, "ContractMonth", value);
            }
        }

        /// <summary>
        /// Property: Refernece year of the contract
        /// </summary>
        public int ContractYear
        {
            get
            {
                return _contractYear;
            }
            set
            {
                this._contractYear = value;
                Factory.SetProperty(this, "ContractYear", value);
            }
        }



        private Instrument _underlying = null;
        /// <summary>
        /// Property: Underlying instrument
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public Instrument Underlying
        {
            get
            {
                if (_underlying == null)
                {
                    _underlying = FindInstrument(UnderlyingID);
                }
                return _underlying;
            }
            set
            {
                if (value == null)
                    throw new Exception("Underlying Instrument is Null");

                if (Underlying.ID != value.ID)
                {
                    UnderlyingID = value.ID;
                    _underlying = value;
                    Factory.SetProperty(this, "UnderlyingInstrumentID", value);
                }
            }
        }
        private Future _nextFuture = null;

        /// <summary>
        /// Property: Next future in chain.
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public Future NextFuture
        {
            get
            {

                if (_nextFuture == null)
                {
                    _nextFuture = Factory.FindFuture(NextID);
                    if (_nextFuture != null)
                        _nextFuture.PreviousFuture = this;
                }

                return _nextFuture;
            }
            set
            {
                _nextFuture = value;
            }

        }

        private Future _previousFuture = null;
        /// <summary>
        /// Property: Previous future in chain.
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public Future PreviousFuture
        {
            get
            {
                if (_previousFuture == null)
                {
                    _previousFuture = Factory.FindFuture(PreviousID);
                    if (_previousFuture != null)
                        _previousFuture.NextFuture = this;
                }

                return _previousFuture;
            }
            set
            {
                _previousFuture = value;
            }
        }

        /// <summary>
        /// Function: Weighted average of contract value * liquidity for a given number of days. Liquidity type is usually volume.
        /// </summary>
        /// <param name="date">reference date</param>
        /// <param name="days">number of days in weighted average</param>
        /// <param name="liquidityType">type of liquidity. (Volume usually)</param>
        public double AverageDailyLiquidity(DateTime date, int days, TimeSeriesType liquidityType)
        {
            TimeSeries ts_close = this.GetTimeSeries(TimeSeriesType.Close);
            int idx_date_close = ts_close.GetClosestDateIndex(date, TimeSeries.DateSearchType.Previous);

            ts_close = ts_close.GetRange(Math.Max(0, idx_date_close - (days - 1)), idx_date_close);

            TimeSeries ts_liquidity = this.GetTimeSeries(liquidityType);
            int idx_date_liquidity = ts_liquidity.GetClosestDateIndex(date, TimeSeries.DateSearchType.Previous);
            ts_liquidity = ts_liquidity.GetRange(Math.Max(0, idx_date_liquidity - (days - 1)), idx_date_liquidity);

            if (ts_liquidity != null && ts_liquidity.Count != 0)
            {
                TimeSeries ts_dollar_liquidity = ts_close * ts_liquidity * this.ContractSize;
                double adl = ts_dollar_liquidity.Average();

                return adl;
            }
            return 0;
        }

        /// <summary>
        /// Function: Remove future from memory and persistent storage.
        /// </summary>
        new public void Remove()
        {
            Factory.Remove(this);
            base.Remove();
        }

        /// <summary>
        /// Function: Create a future
        /// </summary>
        public static Future CreateFuture(Security security, string generic_months, DateTime first_delivery, DateTime first_notice, DateTime last_delivery, DateTime first_trade, DateTime last_trade, double tick_size, double contract_size, Instrument underlying)
        {
            return Factory.CreateFuture(security, generic_months, first_delivery, first_notice, last_delivery, first_trade, last_trade, tick_size, contract_size, underlying);
        }

        /// <summary>
        /// Function: Find a future
        /// </summary>
        public static Future FindFuture(Security security)
        {
            if (security is Future)
                return security as Future;

            return Factory.FindFuture(security);
        }

        /// <summary>
        /// Function: Find the current futures for a given underlying instrument at a given date       
        /// </summary>
        /// <param name="underlyingInstrument">reference underlying instrument</param>
        /// <param name="date">reference date</param>
        public static Future CurrentFuture(Instrument underlyingInstrument, DateTime date)
        {
            return Factory.CurrentFuture(underlyingInstrument, date);
        }

        /// <summary>
        /// Function: Find the current futures for a given underlying instrument at a given date and a specific contract size       
        /// </summary>
        /// <param name="underlyingInstrument">reference underlying instrument</param>
        /// <param name="contract_size">reference contract size</param>
        /// <param name="date">reference date</param>
        public static Future CurrentFuture(Instrument underlyingInstrument, double contract_size, DateTime date)
        {
            return Factory.CurrentFuture(underlyingInstrument, date);
        }

        /// <summary>
        /// Function: check if underlying instrument has linked futures
        /// </summary>
        /// <param name="underlyingInstrument">reference underlying instrument</param>        
        public static Boolean HasFutures(Instrument underlyingInstrument)
        {
            return Factory.HasFutures(underlyingInstrument);
        }

        /// <summary>
        /// Function: List of all underlying instruments with futures in the system.
        /// </summary>
        public static List<Instrument> Underlyings()
        {
            return Factory.Underlyings();
        }

        /// <summary>
        /// Function: List of all active futures for an underlying instruments at a given date
        /// </summary>
        /// <param name="underlyingInstrument">reference underlying instrument</param>
        /// <param name="date">reference date</param>
        public static List<Future> ActiveFutures(Instrument underlyingInstrument, DateTime date)
        {
            return Factory.ActiveFutures(underlyingInstrument, date);
        }

        /// <summary>
        /// Function: List of all active futures for an underlying instruments at a given date with a specific contract size.
        /// </summary>
        /// <param name="underlyingInstrument">reference underlying instrument</param>
        /// <param name="contract_size">reference contract size</param>
        /// <param name="date">reference date</param>
        public static List<Future> ActiveFutures(Instrument underlyingInstrument, double contract_size, DateTime date)
        {
            return Factory.ActiveFutures(underlyingInstrument, date);
        }
    }
}
