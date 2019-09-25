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
using System.ComponentModel;

namespace AQI.AQILabs.Kernel
{
    /// <summary>
    /// Currency class containing
    /// the functions and variables.
    /// This class also manages the connectivity
    /// to the database through a relevant Factories
    /// </summary>  
    public class Currency : IEquatable<Currency>
    {
        [Browsable(false)]

        public static AQI.AQILabs.Kernel.Factories.ICurrencyFactory Factory = null;

        /// <summary>
        /// Constructor of the Currency Class        
        /// </summary>
        /// <remarks>
        /// Only used Internaly by Kernel
        /// </remarks>
        public Currency(int id, string name, string description, int calendarID)
        {
            this.ID = id;
            this.Name = name;
            this.Description = description;
            this.CalendarID = calendarID;
        }

        /// <summary>
        /// Property containt int value for the unique ID of the Currency.
        /// </summary>
        /// <remarks>
        /// Main identifier for each Currency in the System
        /// </remarks>
        public int ID { get; private set; }

        /// <summary>
        /// Property containt int value for the unique ID of the calendar linked to this currency.
        /// </summary>
        public int CalendarID { get; set; }

        public bool Equals(Currency other)
        {
            if (((object)other) == null)
                return false;

            return ID == other.ID;
        }
        public override bool Equals(object other)
        {
            if (typeof(Currency) == other.GetType())
                return Equals((Currency)other);
            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator ==(Currency x, Currency y)
        {
            if (((object)x) == null && ((object)y) == null)
                return true;
            else if (((object)x) == null)
                return false;

            return x.Equals(y);
        }
        public static bool operator !=(Currency x, Currency y)
        {
            if (((object)x) == null && ((object)y) == null)
                return false;
            else if (((object)x) == null)
                return true;

            return !x.Equals(y);
        }

        /// <summary>
        /// Function: String representation of the currency.
        /// </summary>
        public override string ToString()
        {
            return Name;
        }

        private string _name;

        /// <summary>
        /// Property: Name of this currency.
        /// </summary>
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                this._name = value;
                Factory.SetProperty(this, "Name", value);
            }
        }
        private string _description;

        /// <summary>
        /// Property: Description of this currency.
        /// </summary>
        public string Description
        {
            get
            {
                return _description;
            }
            set
            {
                this._description = value;
                Factory.SetProperty(this, "Description", value);
            }
        }

        private Calendar _calendar = null;

        /// <summary>
        /// Property: Calendar of of this currency.
        /// </summary>
        public Calendar Calendar
        {
            get
            {
                if (_calendar == null)
                    _calendar = Calendar.FindCalendar(CalendarID);

                return _calendar;
            }
            set
            {
                this._calendar = value;
                this.CalendarID = value.ID;
                Factory.SetProperty(this, "CalendarID", value.ID);
            }
        }

        [Newtonsoft.Json.JsonIgnore]

        /// <summary>
        /// Property: List of instruments denominated in this currency.
        /// </summary>
        public List<Instrument> Instruments
        {
            get
            {
                var ins = from i in Instrument.Instruments()
                          where i.Currency == this
                          select i;
                return new List<Instrument>(ins);
            }
        }

        /// <summary>
        /// Function: Create currency in the system.
        /// </summary>
        /// <param name="name">Name of currency.
        /// </param>
        /// <param name="description">Description of the currency.
        /// </param>
        /// <param name="calendar">Calendar linked to this currency.
        /// </param>
        public static Currency CreateCurrency(string name, string description, Calendar calendar)
        {
            return Factory.CreateCurrency(name, description, calendar);
        }

        /// <summary>
        /// Function: Find a currency with a given name in memory or storage.
        /// </summary>
        /// <param name="name">Name of currency.
        /// </param>
        public static Currency FindCurrency(string name)
        {
            return Factory.FindCurrency(name);
        }

        /// <summary>
        /// Function: Find a currency with a given ID in memory or storage.
        /// </summary>
        /// <param name="id">ID of currency.
        /// </param>
        public static Currency FindCurrency(int id)
        {
            return Factory.FindCurrency(id);
        }

        /// <summary>
        /// Property: List of currencies in the system.
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public static List<Currency> Currencies
        {
            get
            {
                return Factory.Currencies();
            }
        }

        /// <summary>
        /// Property: List of currency names in the system.
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public static List<string> CurrencyNames
        {
            get
            {
                return Factory.CurrencyNames();
            }
        }
    }

    /// <summary>
    /// Currency Pair class containing
    /// the functions managing FX conversion.
    /// This class also manages the connectivity
    /// to the database through a relevant Factories
    /// </summary>  
    public class CurrencyPair
    {
        public static AQI.AQILabs.Kernel.Factories.ICurrencyPairFactory Factory = null;

        /// <summary>
        /// Property containt int value for the unique ID of the calendar.
        /// </summary>
        /// <remarks>
        /// Main identifier for each Calendar in the System
        /// </remarks>
        public int ID { get; private set; }

        private Currency _buy = null;
        private Currency _sell = null;
        private Instrument _fxInstrument = null;

        private int _buyID = -1;
        private int _sellID = -1;
        private int _fxInstrumentID = -1;

        /// <summary>
        /// Constructor of the CurrencyPair Class
        /// </summary>
        /// <remarks>
        /// Only used Internaly by Kernel
        /// </remarks>
        public CurrencyPair(int buy, int sell, int fxinstrument)
        {
            this._buyID = buy;
            this._sellID = sell;
            this._fxInstrumentID = fxinstrument;
        }

        /// <summary>
        /// Property: ID of the purchasing currency.
        /// </summary>
        public int CurrencyBuyID
        {
            get
            {
                return _buyID;
            }
            set
            {
                _buyID = value;
                _buy = Currency.FindCurrency(value);
                Factory.SetProperty(this, "CurrencyBuyID", value);
            }
        }

        /// <summary>
        /// Property: ID of the selling currency.
        /// </summary>
        public int CurrencySellID
        {
            get
            {
                return _sellID;
            }
            set
            {
                _sellID = value;
                _sell = Currency.FindCurrency(value);
                Factory.SetProperty(this, "CurrencySellID", value);
            }
        }

        /// <summary>
        /// Function: String representation of the currency pair.
        /// </summary>
        public override string ToString()
        {
            return CurrencyBuy + " -- " + CurrencySell;
        }

        /// <summary>
        /// Property: Purchasing currency.
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public Currency CurrencyBuy
        {
            get
            {
                if (_buy == null)
                    _buy = Currency.FindCurrency(_buyID);
                return _buy;
            }
            set
            {
                _buyID = value.ID;
                _buy = value;
                Factory.SetProperty(this, "CurrencyBuyID", value.ID);
            }
        }

        /// <summary>
        /// Property: Selling currency.
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public Currency CurrencySell
        {
            get
            {
                if (_sell == null)
                    _sell = Currency.FindCurrency(_sellID);
                return _sell;
            }
            set
            {
                _sellID = value.ID;
                _sell = value;
                Factory.SetProperty(this, "CurrencySellID", value.ID);
            }
        }

        /// <summary>
        /// Property: ID of the instrument representing the FX.
        /// </summary>
        public int FXInstrumentID
        {
            get
            {
                return _fxInstrumentID;
            }
            set
            {
                _fxInstrumentID = value;
                _fxInstrument = Instrument.FindInstrument(value);
                Factory.SetProperty(this, "FXInstrumentID", value);
            }
        }

        /// <summary>
        /// Property: Instrument representing the FX.
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public Instrument FXInstrument
        {
            get
            {
                if (_fxInstrument == null)
                    _fxInstrument = Instrument.FindInstrument(_fxInstrumentID);

                return _fxInstrument;
            }
            set
            {
                _fxInstrument = value;
                Factory.SetProperty(this, "FXInstrumentID", value.ID);
            }
        }

        /// <summary>
        /// Function: Convert a value from the Sell currency denomination to the Buy currency denomination on a given date.
        /// </summary>
        /// <param name="value">Value to be converted.
        /// </param>
        /// <param name="date">Reference date.
        /// </param>
        /// <param name="buy">Target currency in the conversion.
        /// </param>
        /// <param name="sell">Initial currency in the conversion.
        /// </param>        
        public static double Convert(double value, DateTime date, Currency buy, Currency sell)
        {
            return Convert(value, date, TimeSeriesType.Last, DataProvider.DefaultProvider, TimeSeriesRollType.Last, buy, sell);
        }

        /// <summary>
        /// Function: Convert a value from the Sell currency denomination to the Buy currency denomination on a given date.
        /// </summary>
        /// <param name="value">Value to be converted.
        /// </param>
        /// <param name="date">Reference date.
        /// </param>
        /// <param name="type">Time series type of time series point.
        /// </param>
        /// <param name="provider">Provider of reference time series object (Standard is AQI).
        /// </param>
        /// <param name="timeSeriesRoll">Roll type of reference time series object.
        /// </param>
        /// <param name="buy">Target currency in the conversion.
        /// </param>
        /// <param name="sell">Initial currency in the conversion.
        /// </param>
        public static double Convert(double value, DateTime date, TimeSeriesType type, DataProvider provider, TimeSeriesRollType timeSeriesRoll, Currency buy, Currency sell)
        {
            if (buy == sell)
                return value;

            CurrencyPair pair = CurrencyPair.FindCurrencyPair(buy, sell);


            if (pair != null)
                return pair.Convert(value, date, type, provider, timeSeriesRoll);

            CurrencyPair pair_inv = CurrencyPair.FindCurrencyPair(sell, buy);

            if (pair_inv != null)
                return pair_inv.ConvertInverse(value, date, type, provider, timeSeriesRoll);

            //Console.WriteLine("WARNING: NO CURRENCY PAIR FOUND - " + buy.Name + "/" + sell.Name);
            return value;
        }

        /// <summary>
        /// Function: Convert a value from the Sell currency denomination to the Buy currency denomination on a given date.
        /// The conversion is dependent on the definition of the FX Instrument.
        /// </summary>
        /// <param name="value">Value to be converted.
        /// </param>
        /// <param name="date">Reference date.
        /// </param>
        /// <param name="type">Time series type of time series point.
        /// </param>
        /// <param name="provider">Provider of reference time series object (Standard is AQI).
        /// </param>
        /// <param name="timeSeriesRoll">Roll type of reference time series object.
        /// </param>
        public double Convert(double value, DateTime date, TimeSeriesType type, DataProvider provider, TimeSeriesRollType timeSeriesRoll)
        {
            double fxi = FXInstrument[date, type, provider, timeSeriesRoll];

            if (double.IsNaN(fxi))
                return value;

            return value * fxi;
        }

        /// <summary>
        /// Function: Convert a value from the Sell currency denomination to the Buy currency denomination on a given date.
        /// The conversion is dependent on the definition of the inverse of the FX Instrument.
        /// </summary>
        /// <param name="value">Value to be converted.
        /// </param>
        /// <param name="date">Reference date.
        /// </param>
        /// <param name="type">Time series type of time series point.
        /// </param>
        /// <param name="provider">Provider of reference time series object (Standard is AQI).
        /// </param>
        /// <param name="timeSeriesRoll">Roll type of reference time series object.
        /// </param>
        public double ConvertInverse(double value, DateTime date, TimeSeriesType type, DataProvider provider, TimeSeriesRollType timeSeriesRoll)
        {
            return value / FXInstrument[date, type, provider, timeSeriesRoll];
        }

        /// <summary>
        /// Function: Create a currency pair in the system.
        /// </summary>
        /// <param name="buy">Denomination of the purchasing currency.
        /// </param>
        /// <param name="sell">Denomination of the selling currency.
        /// </param>
        /// <param name="fxInstrument">FX Instrument.
        /// </param>
        public static CurrencyPair CreateCurrencyPair(Currency buy, Currency sell, Instrument fxInstrument)
        {
            return Factory.CreateCurrencyPair(buy, sell, fxInstrument);
        }

        /// <summary>
        /// Function: Find a currency pair given buy and sell denominations in memory or storage.
        /// </summary>
        /// <param name="buy">Purchasing currency.
        /// </param>
        /// <param name="buy">Purchasing currency.
        /// </param>
        public static CurrencyPair FindCurrencyPair(Currency buy, Currency sell)
        {
            return Factory.FindCurrencyPair(buy, sell);
        }

        /// <summary>
        /// Function: Find a currency pair given an FX instrument.
        /// </summary>
        /// <param name="FXInstrument">FX Instrument.
        /// </param>
        public static CurrencyPair FindCurrencyPair(Instrument FXInstrument)
        {
            return Factory.FindCurrencyPair(FXInstrument);
        }
    }
}
