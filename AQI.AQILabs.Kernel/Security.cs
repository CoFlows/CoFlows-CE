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
using AQI.AQILabs.Kernel.Numerics.Util;

namespace AQI.AQILabs.Kernel
{
    /// <summary>
    /// Class containing the corporate action data. No processing logic is contained here. The processing logic is in the portfolio.
    /// </summary>
    public class CorporateAction : IEquatable<CorporateAction>
    {
        public bool Equals(CorporateAction other)
        {
            if (((object)other) == null)
                return false;
            return Security.ID == other.Security.ID && ExDate == other.ExDate && RecordDate == other.RecordDate && DeclaredDate == other.DeclaredDate && PayableDate == other.PayableDate && Amount == other.Amount && Frequency == other.Frequency && Type == other.Type;
        }
        public override bool Equals(object other)
        {
            try { return Equals((CorporateAction)other); }
            catch { return false; }
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator ==(CorporateAction x, CorporateAction y)
        {
            if (((object)x) == null && ((object)y) == null)
                return true;
            else if (((object)x) == null)
                return false;

            return x.Equals(y);
        }
        public static bool operator !=(CorporateAction x, CorporateAction y)
        {
            return !(x == y);
        }

        public string ID { get; set; }

        /// <summary>
        /// Property: Security linked to the Corporate Action
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public Security Security
        {
            get
            {
                if (SecurityID == -1)
                    return null;
                return Instrument.FindInstrument(SecurityID) as Security;
            }
        }

        /// <summary>
        /// Property: ID of the security linked to the Corporate Action
        /// </summary>
        public int SecurityID { get; set; }

        /// <summary>
        /// Property: Declared Date
        /// Date in which the action was announced
        /// </summary>
        public DateTime DeclaredDate { get; set; }

        /// <summary>
        /// Property: Ex Date
        /// The date at which a stock will trade "cum ex" (without entitlement). So for example in a normal cash dividend, if the exdate is 25.11.2008 then the stock will trade without the right to the cash dividendfrom the 25.11.2008 onwards. Cum (latin for with) and Ex (latin for without).
        /// Expiry date / Expiration date
        /// 1) The date at which an option or a warrant expires, and therefore cannot be exercised any longer.
        /// 2) The date at which a Tender Offer expires, ie the day up until shareholders can tender their shares to the offer.
        /// </summary>
        public DateTime ExDate { get; set; }

        /// <summary>
        /// Property: Ex Date
        /// The date at which your positions will be recorded in order to calculate your entitlements. So for example; if the positions in your account on record date are 100,000 shares and a cash dividend pays EUR 0.25 per share then your entitlement will be calculated as 100,000 x EUR 0.25 = EUR 25,000.
        /// </summary>
        public DateTime RecordDate { get; set; }

        /// <summary>
        /// Property: Pay date. When the action is paid.        
        /// </summary>
        public DateTime PayableDate { get; set; }

        /// <summary>
        /// Property: Amount
        /// </summary>
        public double Amount { get; set; }

        /// <summary>
        /// Property: Frequency
        /// </summary>
        public string Frequency { get; set; }

        /// <summary>
        /// Property: Type
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public CorporateAction(string ID, int securityID, DateTime declaredDate, DateTime exDate, DateTime recordDate, DateTime payableDate, double amount, string frequency, string type)
        {
            this.ID = ID;
            this.SecurityID = securityID;
            this.DeclaredDate = declaredDate;
            this.ExDate = exDate;
            this.RecordDate = recordDate;
            this.PayableDate = payableDate;

            this.Amount = amount;

            this.Frequency = frequency;
            this.Type = type;
        }

        /// <summary>
        /// Function: string representation of the action.
        /// </summary>
        public override string ToString()
        {
            return Security + " " + PayableDate.ToShortDateString() + " (" + Type + ") " + Amount;
        }
    }

    /// <summary>
    /// Security class containing relevant information like exchange and ISIN while also logic for management of corporate actions.
    /// </summary>
    public class Security : Instrument
    {
        new public static AQI.AQILabs.Kernel.Factories.ISecurityFactory Factory = null;
        private static Dictionary<int, Security> _securityIdDB = new Dictionary<int, Security>();

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

        /// <summary>
        /// Function: List of corporate actions
        /// </summary>
        public List<CorporateAction> CorporateActions()
        {
            return Factory.CorporateActions(this);
        }

        /// <summary>
        /// Function: List of corporate actions for a given date
        /// </summary>
        public List<CorporateAction> CorporateActions(DateTime date)
        {
            return Factory.CorporateActions(this, date);
        }

        /// <summary>
        /// Function: Add corporate action to memory and persistent storage
        /// </summary>
        public void AddCorporateAction(CorporateAction action)
        {
            Factory.AddCorporateAction(this, action);
        }

        /// <summary>
        /// Function: Add set of corporate actions to memory and persistent storage
        /// </summary>
        public void AddCorporateAction(Dictionary<DateTime, List<CorporateAction>> actions)
        {
            Factory.AddCorporateAction(this, actions);
        }


        private ConcurrentDictionary<DateTime, TimeSeries> _trDB = new ConcurrentDictionary<DateTime, TimeSeries>();
        public TimeSeries GetTotalReturnTimeSeries()
        {
            TimeSeries last_ts = this.GetTimeSeries(TimeSeriesType.Close);

            if (last_ts == null || last_ts.Count == 0)
                return null;

            int count = last_ts.Count;

            DateTime lastDate = last_ts.DateTimes[count - 1];

            if (_trDB.ContainsKey(lastDate))
                return _trDB[lastDate];



            TimeSeries tr_ts = new TimeSeries(1);
            tr_ts.AddDataPoint(last_ts.DateTimes[0], last_ts[0]);

            var all_corps = this.CorporateActions();

            for (int i = 1; i < count; i++)
            {
                DateTime date = last_ts.DateTimes[i];
                DateTime date_1 = last_ts.DateTimes[i - 1];
                double value = last_ts[i];
                double value_1 = last_ts[i - 1];


                double adjustment = 1.0;
                foreach (CorporateAction c in all_corps)
                    if (c.ExDate >= date.Date)
                    {
                        if (c.Type == "Stock Split")
                            adjustment *= c.Amount;
                        else if (c.Type == "Scrip" && c.Amount != 0.0)
                            adjustment /= c.Amount;
                        else if (c.Type == "Spinoff" && c.Amount != 1.0)
                            adjustment /= 1 - c.Amount;
                    }

                var corps = this.CorporateActions(date.Date);
                if (corps != null)
                    foreach (CorporateAction corp in corps)
                    {
                        double amount = corp.Amount;
                        if (corp.Type != "Stock Split" && corp.Type != "Scrip" && corp.Type != "Spinoff")// && corp.Type != "Rights Issue")
                            value += amount * adjustment;
                        else if (corp.Type == "Spinoff" && corp.Amount != 1.0)
                        {
                            value /= 1 - amount;
                        }

                        else if (corp.Type == "Stock Split")
                        {
                            value *= amount;
                        }
                        else if (corp.Type == "Scrip" && corp.Amount != 0.0)
                        {
                            value /= amount;
                        }
                    }


                double v_1 = tr_ts[i - 1];
                double v = v_1 * (value / value_1);
                tr_ts.AddDataPoint(date, v);

            }

            double scale = last_ts[count - 1] / tr_ts[count - 1];

            tr_ts *= scale;

            _trDB.TryAdd(lastDate, tr_ts);

            return tr_ts;
        }


        private ConcurrentDictionary<DateTime, TimeSeries> _prDB = new ConcurrentDictionary<DateTime, TimeSeries>();
        public TimeSeries GetPriceReturnTimeSeries()
        {
            TimeSeries last_ts = this.GetTimeSeries(TimeSeriesType.Close);

            if (last_ts == null || last_ts.Count == 0)
                return null;

            int count = last_ts.Count;

            DateTime lastDate = last_ts.DateTimes[count - 1];

            if (_prDB.ContainsKey(lastDate))
                return _prDB[lastDate];



            TimeSeries tr_ts = new TimeSeries(1);
            tr_ts.AddDataPoint(last_ts.DateTimes[0], last_ts[0]);

            for (int i = 1; i < count; i++)
            {
                DateTime date = last_ts.DateTimes[i];
                DateTime date_1 = last_ts.DateTimes[i - 1];
                double value = last_ts[i];
                double value_1 = last_ts[i - 1];

                //var corps = this.CorporateActions(date_1.Date);
                var corps = this.CorporateActions(date.Date);
                if (corps != null)
                    foreach (CorporateAction corp in corps)
                    {
                        //Console.WriteLine(corp);

                        double amount = corp.Amount;
                        if (corp.Type != "Stock Split" && corp.Type != "Scrip" && corp.Type != "Spinoff")
                            value += amount * 0;// No Dividend Reinvestment
                        else if (corp.Type == "Spinoff" && corp.Amount != 1.0)
                        {
                            value /= 1 - amount;
                        }
                        else if (corp.Type == "Stock Split")
                        {
                            value *= amount;
                        }
                        else if (corp.Type == "Scrip" && corp.Amount != 0.0)
                        {
                            value /= amount;
                        }
                    }


                double v_1 = tr_ts[i - 1];
                double v = v_1 * (value / value_1);
                tr_ts.AddDataPoint(date, v);

            }

            double scale = last_ts[count - 1] / tr_ts[count - 1];

            tr_ts *= scale;

            _prDB.TryAdd(lastDate, tr_ts);

            return tr_ts;
        }
        /// <summary>
        /// Function: Remove security from internal memory. Nothing to do with persistent storage
        /// </summary>
        public static void CleanMemory(Security security)
        {
            if (_securityIdDB.ContainsKey(security.ID))
            {
                _securityIdDB[security.ID] = null;
                _securityIdDB.Remove(security.ID);
            }
        }

        private string _isin = null;
        private string _sedol = null;


        /// <summary>
        /// Constructor
        /// </summary>
        public Security(Instrument instrument, string isin, string sedol, int exchangeID, double pointSize)
            : base(instrument)
        {
            this._isin = isin;
            this._sedol = sedol;

            this.ExchangeID = exchangeID;
            this._contractPointSize = pointSize;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        [Newtonsoft.Json.JsonConstructor]
        public Security(int id, string isin, string sedol, int exchangeID, double pointSize)
            : base(Instrument.FindCleanInstrument(id))
        {
            this._isin = isin;
            this._sedol = sedol;

            this.ExchangeID = exchangeID;
            this._contractPointSize = pointSize;
        }

        /// <summary>
        /// Property: ISIN value
        /// </summary>
        public string Isin
        {
            get
            {
                return _isin;
            }
            set
            {
                if (value == null)
                    throw new Exception("Value is NULL");
                this._isin = value;

                if (!SimulationObject)
                    Factory.SetProperty(this, "Isin", value);
            }
        }

        /// <summary>
        /// Property: ISIN value
        /// </summary>
        public int IsinID
        {
            get
            {
                return FindIsin(_isin);
            }
        }


        /// <summary>
        /// Property: Sedol value
        /// </summary>
        public string Sedol
        {
            get
            {
                return _sedol;
            }
            set
            {
                if (value == null)
                    throw new Exception("Value is NULL");
                this._sedol = value;

                if (!SimulationObject)
                    Factory.SetProperty(this, "Sedol", value);
            }
        }

        /// <summary>
        /// Property: Sedol value
        /// </summary>
        public int SedolID
        {
            get
            {
                return FindSedol(_sedol);
            }
        }


        /// <summary>
        /// Property: ID of exchange this security is traded on
        /// </summary>
        public int ExchangeID { get; set; }

        /// <summary>
        /// Property: Exchange this security is traded on
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public Exchange Exchange
        {
            get
            {
                return Exchange.FindExchange(ExchangeID);
            }
            set
            {
                if (value == null)
                    throw new Exception("Value is NULL");

                this.ExchangeID = value.ID;

                if (!SimulationObject)
                    Factory.SetProperty(this, "ExchangeID", value.ID);
            }
        }

        private double _contractPointSize = 0.0;
        /// <summary>
        /// Property: point size.
        /// Value of one point also called a multiplier
        /// </summary>
        public double PointSize
        {
            get
            {
                if (Name.StartsWith("SP ") && this.InstrumentType == InstrumentType.Future)
                    return 50;

                return _contractPointSize;
            }
            set
            {
                this._contractPointSize = value;
                Factory.SetProperty(this, "PointSize", value);
            }
        }

        //[Newtonsoft.Json.JsonIgnore]
        //new public Calendar Calendar
        //{
        //    get
        //    {
        //        return Exchange.Calendar;
        //    }
        //}

        //new public void Remove()
        //{
        //    Factory.Remove(this);
        //    base.Remove();
        //}

        /// <summary>
        /// Function: Create security
        /// </summary>        
        public static Security CreateSecurity(Instrument instrument, string isin, string sedol, Exchange exchange, double pointSize)
        {
            return Factory.CreateSecurity(instrument, isin, sedol, exchange, pointSize);
        }

        /// <summary>
        /// Function: Find security
        /// </summary>        
        public static Security FindSecurity(Instrument instrument)
        {
            if (instrument is Security)
                return instrument as Security;

            return Factory.FindSecurity(instrument);
        }

        /// Function: Find security by Isin
        /// </summary>        
        public static IEnumerable<Security> FindSecurityByIsin(string isin)
        {
            return Factory.FindSecurityByIsin(isin);
        }

        /// Function: Find security by Isin
        /// </summary>        
        public static IEnumerable<Security> FindSecurityByIsin(int isinID)
        {
            return Factory.FindSecurityByIsin(isinID);
        }

        public static string FindIsin(int isin)
        {
            return Factory.FindIsin(isin);
        }

        public static int FindIsin(string isin)
        {
            return Factory.FindIsin(isin);
        }


        /// Function: Find security by Sedol
        /// </summary>        
        public static IEnumerable<Security> FindSecurityBySedol(string sedol)
        {
            return Factory.FindSecurityBySedol(sedol);
        }

        /// Function: Find security by Sedol
        /// </summary>        
        public static IEnumerable<Security> FindSecurityBySedol(int sedolID)
        {
            return Factory.FindSecurityBySedol(sedolID);
        }

        public static string FindSedol(int sedol)
        {
            return Factory.FindSedol(sedol);
        }

        public static int FindSedol(string sedol)
        {
            return Factory.FindSedol(sedol);
        }
    }
}
