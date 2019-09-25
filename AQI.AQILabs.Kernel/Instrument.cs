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
using System.ComponentModel;

using AQI.AQILabs.Kernel.Numerics.Util;

using QuantApp.Kernel;
using QuantApp.Kernel.Factories;

namespace AQI.AQILabs.Kernel
{
    /// <summary>
    /// Enumeration of possible Instrument Types
    /// This value is set for all Instrument objects.
    /// </summary>
    public enum InstrumentType
    {
        None = 0, Equity = 1, Index = 2, Future = 3, Currency = 4, FXRate = 5, InterestRateFixing = 6,
        InterestRateSwap = 7, Fund = 8, ETF = 9, Warrant = 10, Commodity = 11, Option = 12,
        Strategy = 13, Portfolio = 14, Deposit = 15,
        SpreadBet = 16
    };

    /// <summary>
    /// Enumeration of possible Asset Classes
    /// This value is set for all Instrument objects.
    /// </summary>
    public enum AssetClass
    {
        NoAssetClass = 0,
        Equity = 1,
        Energy = 2, Grains = 3, Metals = 4, Softs = 5, Livestock = 6,
        Credit = 7,
        FX = 8,
        RealEstate = 9,
        Strategy = 10
    };

    /// <summary>
    /// Enumeration of possible Geographical Regions
    /// This value is set for all Instrument objects.
    /// </summary>
    public enum GeographicalRegion
    {
        NoRegion = 0,
        NorthAmerica = 1,
        SouthAmerica = 2,
        Europe = 3,
        MiddleEast = 4,
        Asia = 5,
        Africa = 6,
        Australia = 7,
        Global = 8
    };



    /// <summary>
    /// Enumeration of possible Funding Types
    /// This value is set for all Instrument objects.
    /// </summary>    
    /// <remarks>
    /// NA represents Not Applicable and is used for
    /// synthetic instruments. For example an Index or any
    /// asset not representing a tradable investment vehicle.
    /// </remarks>
    public enum FundingType
    {
        TotalReturn = -1, NA = 0, ExcessReturn = 1
    };

    /// <summary>
    /// Enumeration of possible TimeSeries Types
    /// This value is set for all TimeSeries objects.
    /// </summary>    
    /// <remarks>
    /// Close is the most commonly used value for all instruments
    /// while the others are not available for all.
    /// </remarks>
    public enum TimeSeriesType
    {
        Last = 1, Open = 2, Tick = 3, Executed = 4, High = 5, Low = 6, Volume = 7, OpenInterest = 8, AdjClose = 9,
        Bid = 10, Ask = 11, Close = 12, MarketCap = 13, AdjPriceReturn = 14, DebtRatio = 15,

        /// <summary>
        /// ClearMacro Patch...
        /// </summary>
        HistoricalGDP = 101, ForecastGDP_1y = 102, ForecastGDP_2y = 103,
        CorporateProfitMargin = 104, ForwardPE = 105, PBook = 106, PSales = 107, PCF = 108, EPS = 109, CPI = 110,
        PerCapitaRealGDP = 111, CurrentDividendYield_1y = 112, DividendPayoutRatio = 113, SwapRate = 114,
        IndexBondRating = 115, YTM = 116, Duration = 117, AverageLife = 118, Convexity = 119, YieldCurve = 120,
        //CM Research Instrument Types
        ExpectedReturn = 200, //For storing Expected Return numbers of Research Instrument
        HedgedReturn = 201, //For storing hedged return on Currency Instrument
        UnhedgedReturn = 202, //For storing unhedged return on Currency Instrument
        SpotExRateChange = 203, //For storing fxchange
        InflationMeasure = 204,
        InformationRatio = 400,

        FxCarry = 301,
        FxRealCarry = 302,
        FxVolCarry = 303,
        FxValuation = 304
    };

    /// <summary>
    /// Enumeration of possible TimeSeries Access Types
    /// Read: Users are only allowed to read data from the TimeSeries object unless this variable is changed
    /// Write: Users are allowed to read/write data to the TimeSeries object  
    /// </summary>    
    /// <remarks>
    /// NotSet: Dummy variable for internal use only.    
    /// </remarks>
    public enum TimeSeriesAccessType
    {
        Read = 1, Write = 2, NotSet = 3
    };

    /// <summary>
    /// Enumeration of possible TimeSeries Roll Types
    /// Exact: Only available dates will return a value, if the date is not available, a call will return NaN
    /// Last: The value for the most previous available date will be returned if the specific date is not available
    /// </summary>    
    /// <remarks>
    /// NotSet: Dummy variable for internal use only.    
    /// </remarks>
    public enum TimeSeriesRollType
    {
        Exact = 1, Last = 2, NotSet = 3
    };


    /// <summary>
    /// Structure used by Kernel when saving TimeSeries values in memory
    /// </summary>    
    /// <remarks>
    /// Internal use by Kernel Only.
    /// </remarks>
    public struct TimeSeriesPoint
    {
        public int ID;
        public TimeSeriesType type;
        public DateTime date;
        public double value;
        public int ProviderID;

        /// <summary>
        /// Constructor: ID--Instrument ID
        /// </summary>    
        /// <param name="ID">ID for instrument</param>        
        /// <param name="type">TimeSeries Type: Open, Close, etc</param>
        /// <param name="value">TimeSeries point value</param>
        /// <param name="ProviderID">ID for provider of Data</param>        
        public TimeSeriesPoint(int ID, TimeSeriesType type, DateTime date, double value, int ProviderID)
        {
            this.ID = ID;
            this.type = type;
            this.date = date;
            this.value = value;
            this.ProviderID = ProviderID;
        }

        public override string ToString()
        {
            return this.ID + " " + this.date.ToShortDateString() + " " + this.value + " " + this.type;
        }
    }


    /// <summary>
    /// Generic delete for the transformation and filtering
    /// TimeSeries data.
    /// </summary>
    /// <remarks>
    /// Can be used for random simulations.
    /// </remarks>
    public delegate TimeSeries TimeSeriesFilter(Instrument instrument, TimeSeries timeSeries, TimeSeriesType type);

    /// <summary>
    /// Instrument skeleton containing
    /// the most general functions and variables.
    /// This class also manages the connectivity
    /// to the database through a relevant Factories
    /// </summary>    
    [Serializable]
    public class Instrument : IEquatable<Instrument>, IPermissible
    {
        public static AQI.AQILabs.Kernel.Factories.IInstrumentFactory Factory = null;
        [Browsable(false)]

        private TimeSeriesAccessType _timeSeriesAccess = TimeSeriesAccessType.NotSet;
        private TimeSeriesRollType _timeSeriesRoll = TimeSeriesRollType.NotSet;

        private TimeSeriesFilter _timeSeriesFilter = null;

        /// <summary>
        /// Global variable used for time series filter function.
        /// </summary>
        /// <remarks>
        /// Example: This function can be used to randomly perturb the time series in robustness simulations.
        /// </remarks>
        public static TimeSeriesFilter TimeSeriesFilterStatic = null;

        /// <summary>
        /// Global variable if true, the system will always load time series from persistent storage and override memory values.
        /// </summary>
        public static Boolean TimeSeriesLoadFromDatabase = false;
        public static Boolean TimeSeriesLoadFromDatabaseIntraday = false;

        /// <summary>
        /// TimeSeries Filter for a specific instance.
        /// If no specific Filter is set, a generic
        /// filter is used if the generic filter is not null.
        /// </summary>
        public TimeSeriesFilter TimeSeriesFilter
        {
            get
            {
                if (TimeSeriesFilterStatic == null && _timeSeriesFilter == null)
                    return null;

                if (_timeSeriesFilter != null)
                    return _timeSeriesFilter;

                return TimeSeriesFilterStatic;
            }
            set
            {
                this._timeSeriesFilter = value;
            }
        }

        /// <summary>
        /// Removes the TimeSeries values from
        /// memory for this Instrument
        /// </summary>        
        public void CleanTimeSeriesFromMemory()
        {
            Factory.CleanTimeSeriesFromMemory(this);
        }

        /// <summary>
        /// Removes any reference in memory of
        /// Instrument
        /// </summary>
        /// <remarks>
        /// Static function
        /// </remarks>
        /// <param name="instrument">The instrument 
        /// that is to be removed from memory
        /// </param>        
        public static void CleanMemory(Instrument instrument)
        {
            Factory.CleanMemory(instrument);
        }

        /// <summary>
        /// Property containt int value for the unique ID of the instrument
        /// </summary>
        /// <remarks>
        /// Main identifier for each Instrument in the System
        /// </remarks>
        public int ID { get; set; }

        /// <summary>
        /// Property containt int value for the unique ID of the instrument used by the permissioning system.
        /// </summary>
        public string PermissibleID
        {
            get
            {
                return ID.ToString();
            }
        }

        public bool Equals(Instrument other)
        {
            if (((object)other) == null)
                return false;
            return ID == other.ID;
        }
        public override bool Equals(object other)
        {
            try { return Equals((Instrument)other); }
            catch { return false; }
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator ==(Instrument x, Instrument y)
        {
            if (((object)x) == null && ((object)y) == null)
                return true;
            else if (((object)x) == null)
                return false;

            return x.Equals(y);
        }
        public static bool operator !=(Instrument x, Instrument y)
        {
            return !(x == y);
        }

        /// <summary>
        /// Constructor of the Instrument Class        
        /// </summary>
        /// <remarks>
        /// Only used Internaly by Kernel
        /// </remarks>
        /// <param name="instrument">Instrument representing base instrument.        
        /// </param>
        protected Instrument(Instrument instrument)
        {
            this._simulationObject = instrument.SimulationObject;
            this.ID = instrument.ID;
            this._name = instrument.Name;
            this._description = instrument.Description;
            this._longDescription = instrument.LongDescription;

            this.CurrencyID = instrument.CurrencyID;
            this._instrumentType = instrument.InstrumentType;
            this._fundingTypeID = (int)instrument.FundingType;
            this._customCalendarID = instrument.Calendar.ID;
            this._executioncost = instrument._executioncost;
            //this._scalefactor = instrument._scalefactor;
            this._carrycostlong = instrument.CarryCostLong;
            this._carrycostshort = instrument.CarryCostShort;
            this._daycountCarry = instrument._daycountCarry;
            this._daycountBaseCarry = instrument._daycountBaseCarry;
            this._deleted = instrument._deleted;

            this._timeSeriesAccess = instrument.TimeSeriesAccess;
            this._timeSeriesRoll = instrument.TimeSeriesRoll;

            this._createTime = instrument.CreateTime;
            this._updateTime = instrument.UpdateTime;
            this._bloombergCode = instrument.BloombergCode;
            this._reutersCode = instrument.ReutersCode;
            this._csiUAMarket = instrument.CSIUAMarket;
            this._csiDeliveryCode = instrument.CSIDeliveryCode;
            this._csiNumCode = instrument.CSINumCode;
            this._yahooCode = instrument.YahooCode;

            this._assetClass = instrument.AssetClass;
            this._region = instrument.GeographicalRegion;

            if (this.Cloud)
                RTDEngine.Subscribe(this.ID.ToString());
        }

        /// <summary>
        /// Constructor of the Instrument Class        
        /// </summary>
        /// <remarks>
        /// Only used Internaly by Kernel
        /// </remarks>
        /// <param name="id">int valued ID
        /// </param>
        protected Instrument(int id)
        {
            Instrument instrument = Instrument.FindInstrument(id);

            this._simulationObject = instrument.SimulationObject;
            this.ID = instrument.ID;
            this._name = instrument.Name;
            this._description = instrument.Description;
            this._longDescription = instrument.LongDescription;

            this.CurrencyID = instrument.CurrencyID;
            this._instrumentType = instrument.InstrumentType;
            this._fundingTypeID = (int)instrument.FundingType;
            this._customCalendarID = instrument.Calendar.ID;
            this._executioncost = instrument._executioncost;
            //this._scalefactor = instrument._scalefactor;
            this._carrycostlong = instrument.CarryCostLong;
            this._carrycostshort = instrument.CarryCostShort;
            this._daycountCarry = instrument._daycountCarry;
            this._daycountBaseCarry = instrument._daycountBaseCarry;
            this._deleted = instrument._deleted;

            this._timeSeriesAccess = instrument.TimeSeriesAccess;
            this._timeSeriesRoll = instrument.TimeSeriesRoll;

            this._createTime = instrument.CreateTime;
            this._updateTime = instrument.UpdateTime;
            this._bloombergCode = instrument.BloombergCode;
            this._reutersCode = instrument.ReutersCode;
            this._csiUAMarket = instrument.CSIUAMarket;
            this._csiDeliveryCode = instrument.CSIDeliveryCode;
            this._csiNumCode = instrument.CSINumCode;
            this._yahooCode = instrument.YahooCode;

            if (this.Cloud)
                RTDEngine.Subscribe(this.ID.ToString());
        }

        /// <summary>
        /// Function: Create a clone of this instrument.
        /// </summary>
        public Instrument Clone(bool simulated)
        {
            string filteredName = Name;
            if (filteredName.StartsWith("$"))
                filteredName = filteredName.Substring(1);

            if (filteredName.Contains("(") && filteredName.Contains(")"))
                filteredName = filteredName.Substring(0, filteredName.LastIndexOf("("));

            string filteredDescription = Description;
            if (filteredDescription.StartsWith("$"))
                filteredDescription = filteredDescription.Substring(1);

            if (filteredDescription.Contains("(") && filteredDescription.Contains(")"))
                filteredDescription = filteredDescription.Substring(0, filteredDescription.LastIndexOf("("));

            string name = filteredName + "(1)";
            int i = 1;
            for (; ; i++)
            {
                name = filteredName + "(" + i + ")";
                Instrument instrument = Instrument.FindInstrument(name);
                if (instrument == null)
                {
                    instrument = Instrument.FindInstrument("$" + name);
                    if (instrument == null)
                        break;
                }
            }
            Instrument clone = CreateInstrument(name, InstrumentType, filteredDescription + "(" + i + ")", Currency, FundingType, simulated);
            clone.Calendar = Calendar;
            clone.SetConstantCarryCost(_carrycostlong, _carrycostshort, _daycountCarry, _daycountBaseCarry);
            clone.ExecutionCost = ExecutionCost;

            //Instrument clone = new Instrument(ID);
            //clone._simulationObject = true;
            return clone;
        }

        private Boolean _simulationObject = false;
        private string _name = null;
        private string _description = null;
        private string _longDescription = null;
        public int CurrencyID { get; set; }
        private Currency _currency = null;
        private InstrumentType _instrumentType = InstrumentType.None;
        private Calendar _calendar = null;
        private double _executioncost = double.NaN;
        //private double _scalefactor = 1.0;
        private double _carrycostlong = double.NaN;
        private double _carrycostshort = double.NaN;
        private DayCountConvention _daycountCarry = DayCountConvention.NotSet;
        private double _daycountBaseCarry = double.NaN;

        private Kernel.AssetClass _assetClass = Kernel.AssetClass.NoAssetClass;
        private GeographicalRegion _region = GeographicalRegion.NoRegion;

        private int _fundingTypeID = -10;
        private int _customCalendarID = -10;

        /// <summary>
        /// Property: Integer valued ID of custom Calendar.
        /// </summary>
        public int CustomCalendarID
        {
            get
            {
                return _customCalendarID;
            }
            set
            {
                this._customCalendarID = value;
            }
        }



        private DateTime _createTime = DateTime.MinValue;
        private DateTime _updateTime = DateTime.MinValue;
        private string _bloombergCode = null;
        private string _reutersCode = null;
        private string _csiUAMarket = null;
        private int _csiDeliveryCode = -1;
        private string _yahooCode = null;
        private int _csiNumCode = -1;

        private bool _deleted = false;

        /// <summary>
        /// Constructor of the Instrument Class        
        /// </summary>
        /// <remarks>
        /// Only used Internaly by Kernel
        /// </remarks>
        public Instrument(int ID,
            string name,
            string description,
            string longDescription,
            InstrumentType instrumentType,
            int currencyID,
            int fundingType,
            int customCalendarID,
            DateTime createTime,
            DateTime updateTime,
            TimeSeriesAccessType timeSeriesAccessType,
            TimeSeriesRollType timeSeriesRollType,

            AssetClass assetClass,
            GeographicalRegion region,

            double executionCost,
            double carryCostLong,
            double carryCostShort,
            DayCountConvention dayCount,
            double carryCostDayCountBase,

            string bloombergTicker,
            string reutersRic,
            string csiUAMarket,
            int csiDeliveryCode,
            int csiNumCode,
            string yahooTicker,
            bool deleted,
            bool simulationObject)
        {
            this._simulationObject = simulationObject;
            this.ID = ID;
            this._name = name;
            this._description = description;
            this._longDescription = longDescription;

            this.CurrencyID = currencyID;
            this._instrumentType = instrumentType;
            this._fundingTypeID = fundingType;
            this._customCalendarID = customCalendarID;
            this._executioncost = executionCost;
            //this._scalefactor = scalefactor;
            this._carrycostlong = carryCostLong;
            this._carrycostshort = carryCostShort;
            this._daycountCarry = dayCount;
            this._daycountBaseCarry = carryCostDayCountBase;
            this._deleted = deleted;

            this._timeSeriesAccess = timeSeriesAccessType;
            this._timeSeriesRoll = timeSeriesRollType;

            this._assetClass = assetClass;
            this._region = region;

            this._createTime = createTime;
            this._updateTime = updateTime;
            this._bloombergCode = bloombergTicker;
            this._reutersCode = reutersRic;
            this._csiUAMarket = csiUAMarket;
            this._csiDeliveryCode = csiDeliveryCode;
            this._csiNumCode = csiNumCode;
            this._yahooCode = yahooTicker;

            RTDEngine.Subscribe(this.ID.ToString());
        }

        private bool _loading = true;
        public Instrument() { }


        /// <summary>
        /// Function: String representation of the Instrument.
        /// </summary>
        public override string ToString()
        {
            return Name + " (" + ID + ")";
        }

        /// <summary>
        /// Property: Database Connection where Strategy data is stored
        /// </summary>    
        public virtual string StrategyDB
        {
            get
            {
                //return InstrumentType == Kernel.InstrumentType.Strategy ? ((Strategy)this).DBConnection : "Kernel";
                return InstrumentType == Kernel.InstrumentType.Strategy && (this as Strategy) != null ? ((Strategy)this).DBConnection : "Kernel";
            }
        }

        /// <summary>
        /// Function: Remove the Instrument from the persistent storage.
        /// </summary>            
        public void Remove()
        {
            //if (SimulationObject)
            //    return;

            if (RemoveCallback != null)
                RemoveCallback(this);

            try
            {
                SystemLog.RemoveEntries(SystemLog.Entries(DateTime.MinValue, DateTime.MaxValue, this, SystemLog.Type.Debug));
                SystemLog.RemoveEntries(SystemLog.Entries(DateTime.MinValue, DateTime.MaxValue, this, SystemLog.Type.Development));
                SystemLog.RemoveEntries(SystemLog.Entries(DateTime.MinValue, DateTime.MaxValue, this, SystemLog.Type.Production));
            }
            catch (Exception e) { SystemLog.Write(e); }

            Factory.Remove(this);
            try
            { RemoveTimeSeries(); }
            catch (Exception e) { SystemLog.Write(e); }
        }

        public void RemoveFrom(DateTime date)
        {
            if (SimulationObject)
                return;

            Factory.RemoveFrom(this, date);
        }

        /// <summary>
        /// Property: True if the Instrument is simulated meaning it is not persistent and is not stored in persistent storage, else False.
        /// </summary>
        public Boolean SimulationObject
        {
            get
            {
                return this._simulationObject;
            }
        }


        private Boolean _cloud = true;
        /// <summary>
        /// Property: True if the Instrument is stored on the Cloud.
        /// </summary>
        public Boolean Cloud
        {
            get
            {
                return SimulationObject ? false : this._cloud;
            }
            set
            {
                if (SimulationObject)
                    throw new Exception("Unable to set this property when object is a SimulationObject.");
                this._cloud = value;
            }
        }

        /// <summary>
        /// Property: Name of Instrument
        /// </summary>
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;

                if (!_simulationObject)
                    Factory.SetProperty(this, "Name", value);

                if (this.Cloud && !_loading)
                    if (RTDEngine.Publish(this))
                        RTDEngine.Send(new RTDMessage() { Type = RTDMessage.MessageType.Property, Content = new RTDMessage.PropertyMessage { ID = ID, Name = "NameLocal", Value = value } });
            }
        }

        public string NameLocal
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;

                if (!_simulationObject)
                    Factory.SetProperty(this, "Name", value);
            }
        }

        /// <summary>
        /// Property: Type of Instrument
        /// </summary>
        public InstrumentType InstrumentType
        {
            get
            {
                return _instrumentType;
            }
            set
            {
                _instrumentType = value;
                if (!_simulationObject)
                    Factory.SetProperty(this, "InstrumentTypeID", value);
            }
        }

        /// <summary>
        /// Property: Description of Instrument
        /// </summary>
        public string Description
        {
            get
            {
                return _description;
            }
            set
            {
                _description = value;
                if (!_simulationObject)
                {
                    Factory.SetProperty(this, "Description", value);
                    if (this.Cloud && !_loading)
                        if (RTDEngine.Publish(this))
                            RTDEngine.Send(new RTDMessage() { Type = RTDMessage.MessageType.Property, Content = new RTDMessage.PropertyMessage { ID = ID, Name = "DescriptionLocal", Value = value } }); ;
                }
            }
        }
        public string DescriptionLocal
        {
            get
            {
                return _description;
            }
            set
            {
                _description = value;
                if (!_simulationObject)
                {
                    Factory.SetProperty(this, "Description", value);
                }
            }
        }

        /// <summary>
        /// Property: Long Description of Instrument. Usually in HTML format.
        /// </summary>
        public string LongDescription
        {
            get
            {
                return _longDescription;
            }
            set
            {
                _longDescription = value;

                if (!_simulationObject)
                {
                    Factory.SetProperty(this, "LongDescription", value);
                    if (this.Cloud && !_loading)
                        if (RTDEngine.Publish(this))
                            RTDEngine.Send(new RTDMessage() { Type = RTDMessage.MessageType.Property, Content = new RTDMessage.PropertyMessage { ID = ID, Name = "LongDescriptionLocal", Value = value } });
                }
            }
        }
        public string LongDescriptionLocal
        {
            get
            {
                return _longDescription;
            }
            set
            {
                _longDescription = value;

                if (!_simulationObject)
                {
                    Factory.SetProperty(this, "LongDescription", value);
                }
            }
        }

        /// <summary>
        /// Property: Asset Class of Instrument
        /// </summary>
        public Kernel.AssetClass AssetClass
        {
            get
            {
                return _assetClass;
            }
            set
            {
                _assetClass = value;

                if (!_simulationObject)
                    Factory.SetProperty(this, "AssetClass", value);

                if (this.Cloud && !_loading)
                    if (RTDEngine.Publish(this))
                        RTDEngine.Send(new RTDMessage() { Type = RTDMessage.MessageType.Property, Content = new RTDMessage.PropertyMessage { ID = ID, Name = "AssetClassLocal", Value = value } });
            }
        }

        public Kernel.AssetClass AssetClassLocal
        {
            get
            {
                return _assetClass;
            }
            set
            {
                _assetClass = value;

                if (!_simulationObject)
                    Factory.SetProperty(this, "AssetClass", value);
            }
        }

        /// <summary>
        /// Property: Geographical Region of Instrument
        /// </summary>
        public GeographicalRegion GeographicalRegion
        {
            get
            {
                return _region;
            }
            set
            {
                _region = value;

                if (!_simulationObject)
                    Factory.SetProperty(this, "GeographicalRegion", value);

                if (this.Cloud && !_loading)
                    if (RTDEngine.Publish(this))
                        RTDEngine.Send(new RTDMessage() { Type = RTDMessage.MessageType.Property, Content = new RTDMessage.PropertyMessage { ID = ID, Name = "GeographicalRegionLocal", Value = value } });
            }
        }

        public GeographicalRegion GeographicalRegionLocal
        {
            get
            {
                return _region;
            }
            set
            {
                _region = value;

                if (!_simulationObject)
                    Factory.SetProperty(this, "GeographicalRegion", value);
            }
        }

        /// <summary>
        /// Property: If instrument has been deleted but still in the system.
        /// </summary>
        public bool Deleted
        {
            get
            {
                return _deleted;
            }
            set
            {
                _deleted = value;

                if (!_simulationObject)
                    Factory.SetProperty(this, "Deleted", value);

                if (value && DeleteCallback != null)
                    DeleteCallback(this);

                if (this.Cloud && !_loading)
                    if (RTDEngine.Publish(this))
                        RTDEngine.Send(new RTDMessage() { Type = RTDMessage.MessageType.Property, Content = new RTDMessage.PropertyMessage { ID = ID, Name = "DeletedLocal", Value = value } });
            }
        }

        public bool DeletedLocal
        {
            get
            {
                return _deleted;
            }
            set
            {
                _deleted = value;

                if (!_simulationObject)
                    Factory.SetProperty(this, "Deleted", value);
            }
        }

        /// <summary>
        /// Property: Reference to the Currency object representing the instruments currency.
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public Currency Currency
        {
            get
            {
                if (_currency == null)
                    _currency = Currency.FindCurrency(this.CurrencyID);
                return _currency;
            }
            set
            {
                _currency = value;
                this.CurrencyID = value.ID;
                if (!_simulationObject)
                    Factory.SetProperty(this, "CurrencyID", value.ID);
            }
        }

        /// <summary>
        /// Property: Funding of Instrument.
        /// </summary>
        public FundingType FundingType
        {
            get
            {
                if (_fundingTypeID == 0)
                    return FundingType.NA;
                else if (_fundingTypeID == 1)
                    return FundingType.ExcessReturn;
                else if (_fundingTypeID == -1)
                    return FundingType.TotalReturn;
                else
                    throw new Exception("Funding type not set");
            }
            set
            {
                if (value == FundingType.NA)
                {
                    if (!_simulationObject)
                        Factory.SetProperty(this, "FundingTypeID", 0);

                    _fundingTypeID = 0;
                }
                else if (value == FundingType.ExcessReturn)
                {
                    if (!_simulationObject)
                        Factory.SetProperty(this, "FundingTypeID", 1);
                    _fundingTypeID = 1;
                }
                else
                {
                    if (!_simulationObject)
                        Factory.SetProperty(this, "FundingTypeID", -1);
                    _fundingTypeID = -1;
                }
            }
        }

        /// <summary>
        /// Property: Reference to the Calendar object representing the instruments trading calendar. Returns Custom Calendaer if it exists, otherwise returns Calendar of Currency.
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public Calendar Calendar
        {
            get
            {
                if (_customCalendarID == -1 || _customCalendarID == -10)
                    _calendar = Currency.Calendar;
                else if (_calendar == null)
                {
                    _calendar = Calendar.FindCalendar(_customCalendarID);
                    _customCalendarID = _calendar.ID;
                }

                return _calendar;
            }
            set
            {
                if (_calendar != value)
                {
                    if (value == null)
                    {
                        _customCalendarID = -1;
                        _calendar = Currency.Calendar;
                    }
                    else
                    {
                        _customCalendarID = value.ID;
                        _calendar = _calendar = Calendar.FindCalendar(_customCalendarID);
                    }

                    if (!_simulationObject)
                        Factory.SetProperty(this, "CustomCalendarID", _customCalendarID);
                }
            }
        }

        /// <summary>
        /// Property: Type of access when writing into the Instrument's time series object.
        /// </summary>
        public TimeSeriesAccessType TimeSeriesAccess
        {
            get
            {
                return _timeSeriesAccess;
            }
            set
            {
                _timeSeriesAccess = value;
                if (!_simulationObject)
                    Factory.SetProperty(this, "TimeSeriesAccessType", value);
            }
        }

        /// <summary>
        /// Property: Type of roll when reading a specific date from into the Instrument's time series object.
        /// </summary>
        public TimeSeriesRollType TimeSeriesRoll
        {
            get
            {
                return _timeSeriesRoll;
            }
            set
            {
                _timeSeriesRoll = value;
                if (!_simulationObject)
                    Factory.SetProperty(this, "TimeSeriesRollType", value);
            }
        }

        ///// <summary>
        ///// Property: Factor applied to transform price series to standard in the system.
        ///// Example: UK stocks are quoted in pence but the system takes pounds so scale factor is 1/100 for assets quoted in GBp
        ///// </summary>
        //public double ScaleFactor
        //{
        //    get
        //    {
        //        return this._scalefactor;
        //    }
        //    set
        //    {
        //        this._scalefactor = value;
        //        if (!_simulationObject)
        //        {
        //            Factory.SetProperty(this, "ScaleFactor", value);

        //            if (this.Cloud && !_loading)
        //                if (RTDEngine.Publish(this))
        //                    RTDEngine.Send(new RTDMessage() { Type = RTDMessage.MessageType.Property, Content = new RTDMessage.PropertyMessage { ID = ID, Name = "ScaleFactorLocal", Value = value } });
        //        }
        //    }
        //}
        //public double ScaleFactorLocal
        //{
        //    get
        //    {
        //        return this._scalefactor;
        //    }
        //    set
        //    {
        //        this._scalefactor = value;
        //        if (!_simulationObject)
        //        {
        //            Factory.SetProperty(this, "ScaleFactor", value);
        //        }
        //    }
        //}


        /// <summary>
        /// Property: Cost applied during execution of simulation in percentage points of price 
        /// </summary>
        public double ExecutionCost
        {
            get
            {
                return this._executioncost;
            }
            set
            {
                this._executioncost = value;
                if (!_simulationObject)
                {
                    Factory.SetProperty(this, "ExecutionCost", value);

                    if (this.Cloud && !_loading)
                        if (RTDEngine.Publish(this))
                            RTDEngine.Send(new RTDMessage() { Type = RTDMessage.MessageType.Property, Content = new RTDMessage.PropertyMessage { ID = ID, Name = "ExecutionCostLocal", Value = value } });
                }
            }
        }
        public double ExecutionCostLocal
        {
            get
            {
                return this._executioncost;
            }
            set
            {
                this._executioncost = value;
                if (!_simulationObject)
                {
                    Factory.SetProperty(this, "ExecutionCost", value);
                }
            }
        }

        /// <summary>
        /// Property: Cost applied during execution of simulation in absoulte terms in reference to value in time series object.
        /// </summary>
        public double ExecutionValue(DateTime date, TimeSeriesType type)
        {
            return this[date, type, DataProvider.DefaultProvider, TimeSeriesRollType.Last] * this._executioncost;
        }

        /// <summary>
        /// Property: Cost applied during simulation in percentage points of price when a portfolio holds a long position of the Instrument.
        /// </summary>
        public double CarryCostLong
        {
            get
            {
                return this._carrycostlong;
            }
            set
            {
                this._carrycostlong = value;
                if (!_simulationObject)
                {
                    Factory.SetProperty(this, "CarryCostLong", value);

                    if (this.Cloud && !_loading)
                        if (RTDEngine.Publish(this))
                            RTDEngine.Send(new RTDMessage() { Type = RTDMessage.MessageType.Property, Content = new RTDMessage.PropertyMessage { ID = ID, Name = "CarryCostLongLocal", Value = value } });
                }
            }
        }
        public double CarryCostLongLocal
        {
            get
            {
                return this._carrycostlong;
            }
            set
            {
                this._carrycostlong = value;
                if (!_simulationObject)
                {
                    Factory.SetProperty(this, "CarryCostLong", value);
                }
            }
        }

        /// <summary>
        /// Property: Cost applied during simulation in percentage points of price when a portfolio holds a short position of the Instrument.
        /// </summary>
        public double CarryCostShort
        {
            get
            {
                return this._carrycostshort;
            }
            set
            {
                this._carrycostshort = value;
                if (!_simulationObject)
                {
                    Factory.SetProperty(this, "CarryCostShort", value);

                    if (this.Cloud && !_loading)
                        if (RTDEngine.Publish(this))
                            RTDEngine.Send(new RTDMessage() { Type = RTDMessage.MessageType.Property, Content = new RTDMessage.PropertyMessage { ID = ID, Name = "CarryCostShortLocal", Value = value } });
                }
            }
        }
        public double CarryCostShortLocal
        {
            get
            {
                return this._carrycostshort;
            }
            set
            {
                this._carrycostshort = value;
                if (!_simulationObject)
                {
                    Factory.SetProperty(this, "CarryCostShort", value);
                }
            }
        }

        /// <summary>
        /// Property: Day count convention applied during simulation in percentage points of price when a portfolio holds a position of the Instrument.
        /// </summary>       
        public DayCountConvention CarryCostDayCountConvention
        {
            get
            {
                return this._daycountCarry;
            }
            set
            {
                this._daycountCarry = value;
                if (!_simulationObject)
                {
                    Factory.SetProperty(this, "CarryCostDayCount", value);

                    if (this.Cloud && !_loading)
                        if (RTDEngine.Publish(this))
                            RTDEngine.Send(new RTDMessage() { Type = RTDMessage.MessageType.Property, Content = new RTDMessage.PropertyMessage { ID = ID, Name = "CarryCostDayCountConventionLocal", Value = value } });
                }
            }
        }
        public DayCountConvention CarryCostDayCountConventionLocal
        {
            get
            {
                return this._daycountCarry;
            }
            set
            {
                this._daycountCarry = value;
                if (!_simulationObject)
                {
                    Factory.SetProperty(this, "CarryCostDayCount", value);
                }
            }
        }

        /// <summary>
        /// Property: Day count base applied during simulation in percentage points of price when a portfolio holds a position of the Instrument. Usual values are 250, 252, 360 or 360.
        /// </summary>
        public double CarryCostDayCountBase
        {
            get
            {
                return this._daycountBaseCarry;
            }
            set
            {
                this._daycountBaseCarry = value;
                if (!_simulationObject)
                {
                    Factory.SetProperty(this, "CarryCostDayCountBase", value);

                    if (this.Cloud && !_loading)
                        if (RTDEngine.Publish(this))
                            RTDEngine.Send(new RTDMessage() { Type = RTDMessage.MessageType.Property, Content = new RTDMessage.PropertyMessage { ID = ID, Name = "CarryCostDayCountBaseLocal", Value = value } });
                }
            }
        }
        public double CarryCostDayCountBaseLocal
        {
            get
            {
                return this._daycountBaseCarry;
            }
            set
            {
                this._daycountBaseCarry = value;
                if (!_simulationObject)
                {
                    Factory.SetProperty(this, "CarryCostDayCountBase", value);
                }
            }
        }

        /// <summary>
        /// Function: Set carry cost parameteres.
        /// </summary>       
        /// <param name="costlong">double valued Carry cost for long positions
        /// </param>
        /// <param name="costshort">double valued Carry cost for short positions
        /// </param>
        /// <param name="daycount">Day count convention applied during portfolio simulations
        /// </param>
        /// <param name="costlong">double valued Carry cost for long positions
        /// </param>
        public void SetConstantCarryCost(double costlong, double costshort, DayCountConvention daycount, double daycountBase)
        {

            this._carrycostlong = costlong;
            this._carrycostshort = costshort;
            this._daycountCarry = daycount;
            this._daycountBaseCarry = daycountBase;

            if (!_simulationObject)
            {
                Factory.SetProperty(this, "CarryCostLong", costlong);
                Factory.SetProperty(this, "CarryCostShort", costshort);
                Factory.SetProperty(this, "CarryCostDayCount", (int)daycount);
                Factory.SetProperty(this, "CarryCostDayCountBase", daycountBase);

                if (this.Cloud && !_loading)
                {
                    if (RTDEngine.Publish(this))
                    {
                        RTDEngine.Send(new RTDMessage() { Type = RTDMessage.MessageType.Property, Content = new RTDMessage.PropertyMessage { ID = ID, Name = "CarryCostLongLocal", Value = costlong } });
                        RTDEngine.Send(new RTDMessage() { Type = RTDMessage.MessageType.Property, Content = new RTDMessage.PropertyMessage { ID = ID, Name = "CarryCostShortLocal", Value = costshort } });
                        RTDEngine.Send(new RTDMessage() { Type = RTDMessage.MessageType.Property, Content = new RTDMessage.PropertyMessage { ID = ID, Name = "CarryCostDayCountLocal", Value = daycount } });
                        RTDEngine.Send(new RTDMessage() { Type = RTDMessage.MessageType.Property, Content = new RTDMessage.PropertyMessage { ID = ID, Name = "CarryCostDayCountBaseLocal", Value = daycountBase } });
                    }
                }
            }
        }

        /// <summary>
        /// Function: Carry cost calculation
        /// </summary>       
        /// <param name="dateStart">DateTime valued start date of calculation period for carry cost.
        /// </param>
        /// <param name="dateEnd">DateTime valued end date of calculation period for carry cost.
        /// </param>
        /// <param name="type">Time series type of time series point used in carry cost calculation.
        /// </param>
        /// <param name="ctype">Position type (Long or Short)
        /// </param>
        public double CarryCost(DateTime dateStart, DateTime dateEnd, TimeSeriesType type, PositionType ctype)
        {
            double val = CarryCalculationFilter(dateStart, dateEnd, type, ctype);
            if (!double.IsNaN(val))
                return val;
            double _carrycost = (ctype == PositionType.Long ? this._carrycostlong : this._carrycostshort);

            return this[dateStart, type, DataProvider.DefaultProvider, TimeSeriesRollType.Last] * _carrycost * ((double)(dateEnd.Date - dateStart.Date).TotalDays) / (_daycountBaseCarry == 0.0 ? 1.0 : _daycountBaseCarry);
        }


        /// <summary>
        /// Delegate: Carry cost calculation event. Skeleton type used to specify carry cost functions for specific instruments.
        /// </summary>       
        /// <param name="dateStart">DateTime valued start date of calculation period for carry cost.
        /// </param>
        /// <param name="dateEnd">DateTime valued end date of calculation period for carry cost.
        /// </param>
        /// <param name="type">Time series type of time series point used in carry cost calculation.
        /// </param>
        /// <param name="ctype">Position type (Long or Short)
        /// </param>
        public delegate double CarryCalculationEvent(Instrument instrument, DateTime dateStart, DateTime dateEnd, TimeSeriesType type, PositionType ctype);
        private static Dictionary<int, CarryCalculationEvent> _carryCalculation = new Dictionary<int, CarryCalculationEvent>();

        /// <summary>
        /// Function: Add carry cost calculation event.
        /// </summary>       
        /// <param name="instrument">Instrument to which link the calculation event.
        /// </param>
        /// <param name="pnlCalcEvent">Event linked to the instrument.
        /// </param>
        public static void AddCarryCalculationEvent(Instrument instrument, CarryCalculationEvent pnlCalcEvent)
        {
            if (_carryCalculation.ContainsKey(instrument.ID))
                _carryCalculation[instrument.ID] += pnlCalcEvent;
            else
                _carryCalculation.Add(instrument.ID, pnlCalcEvent);
        }

        /// <summary>
        /// Function: Remove carry cost calculation event.
        /// </summary>       
        /// <param name="instrument">Instrument to which link the calculation event.
        /// </param>
        /// <param name="pnlCalcEvent">Event linked to the instrument.
        /// </param>
        public static void RemoveCarryCalculationEvent(Instrument instrument, CarryCalculationEvent pnlCalcEvent)
        {
            if (_carryCalculation.ContainsKey(instrument.ID))
                _carryCalculation[instrument.ID] -= pnlCalcEvent;
        }

        /// <summary>
        /// Function: Carry cost calculation from the event calculation database.
        /// </summary>       
        /// <param name="dateStart">DateTime valued start date of calculation period for carry cost.
        /// </param>
        /// <param name="dateEnd">DateTime valued end date of calculation period for carry cost.
        /// </param>
        /// <param name="type">Time series type of time series point used in carry cost calculation.
        /// </param>
        /// <param name="ctype">Position type (Long or Short)
        /// </param>
        public double CarryCalculationFilter(DateTime dateStart, DateTime dateEnd, TimeSeriesType type, PositionType ctype)
        {
            if (_carryCalculation.ContainsKey(ID))
                return _carryCalculation[ID](this, dateStart, dateEnd, type, ctype);
            else if (InstrumentType == AQI.AQILabs.Kernel.InstrumentType.Future && _carryCalculation.ContainsKey(((Future)this).Underlying.ID))
                return _carryCalculation[((Future)this).Underlying.ID](this, dateStart, dateEnd, type, ctype);

            return double.NaN;
        }

        /// <summary>
        /// Property: Creation time of Instrument.
        /// </summary>
        public DateTime CreateTime
        {
            get
            {
                return _createTime;
            }
        }

        /// <summary>
        /// Property: Last update time of Instrument.
        /// </summary>
        public DateTime UpdateTime
        {
            get
            {
                return _updateTime;
            }
            set
            {
                this._updateTime = value;
                Factory.SetProperty(this, "UpdateTime", value);
            }
        }

        /// <summary>
        /// Property: Bloomberg code of Instrument.
        /// </summary>
        public string BloombergCode
        {
            get
            {
                return _bloombergCode;
            }
            set
            {
                this._bloombergCode = value;
                Factory.SetProperty(this, "BloombergTicker", value);

                if (this.Cloud && !_loading)
                    if (RTDEngine.Publish(this))
                        RTDEngine.Send(new RTDMessage() { Type = RTDMessage.MessageType.Property, Content = new RTDMessage.PropertyMessage { ID = ID, Name = "BloombergCodeLocal", Value = value } });
            }
        }
        public string BloombergCodeLocal
        {
            get
            {
                return _bloombergCode;
            }
            set
            {
                this._bloombergCode = value;
                Factory.SetProperty(this, "BloombergTicker", value);
            }
        }


        /// <summary>
        /// Property: Reuters code of Instrument.
        /// </summary>
        public string ReutersCode
        {
            get
            {
                return _reutersCode;
            }
            set
            {
                this._reutersCode = value;
                Factory.SetProperty(this, "ReutersRIC", value);

                if (this.Cloud && !_loading)
                    if (RTDEngine.Publish(this))
                        RTDEngine.Send(new RTDMessage() { Type = RTDMessage.MessageType.Property, Content = new RTDMessage.PropertyMessage { ID = ID, Name = "ReutersCodeLocal", Value = value } });
            }
        }
        public string ReutersCodeLocal
        {
            get
            {
                return _reutersCode;
            }
            set
            {
                this._reutersCode = value;
                Factory.SetProperty(this, "ReutersRIC", value);
            }
        }

        /// <summary>
        /// Property: CSI Unfair Advantage code of Instrument.
        /// </summary>
        public string CSIUAMarket
        {
            get
            {
                return _csiUAMarket;
            }
            set
            {
                this._csiUAMarket = value;
                Factory.SetProperty(this, "CSIUAMarket", value);

                if (this.Cloud && !_loading)
                    if (RTDEngine.Publish(this))
                        RTDEngine.Send(new RTDMessage() { Type = RTDMessage.MessageType.Property, Content = new RTDMessage.PropertyMessage { ID = ID, Name = "CSIUAMarketLocal", Value = value } }); ;
            }
        }
        public string CSIUAMarketLocal
        {
            get
            {
                return _csiUAMarket;
            }
            set
            {
                this._csiUAMarket = value;
                Factory.SetProperty(this, "CSIUAMarket", value);
            }
        }

        /// <summary>
        /// Property: CSI FTP code of Instrument.
        /// </summary>
        public int CSIDeliveryCode
        {
            get
            {
                return _csiDeliveryCode;
            }
            set
            {
                this._csiDeliveryCode = value;
                Factory.SetProperty(this, "CSIDeliveryCode", value);

                if (this.Cloud && !_loading)
                    if (RTDEngine.Publish(this))
                        RTDEngine.Send(new RTDMessage() { Type = RTDMessage.MessageType.Property, Content = new RTDMessage.PropertyMessage { ID = ID, Name = "CSIDeliveryCodeLocal", Value = value } }); ;
            }
        }
        public int CSIDeliveryCodeLocal
        {
            get
            {
                return _csiDeliveryCode;
            }
            set
            {
                this._csiDeliveryCode = value;
                Factory.SetProperty(this, "CSIDeliveryCode", value);
            }
        }

        /// <summary>
        /// Property: CSI Yahoo code of Instrument.
        /// </summary>
        public string YahooCode
        {
            get
            {
                return _yahooCode;
            }
            set
            {
                this._yahooCode = value;
                Factory.SetProperty(this, "YahooTicker", value);

                if (this.Cloud && !_loading)
                    if (RTDEngine.Publish(this))
                        RTDEngine.Send(new RTDMessage() { Type = RTDMessage.MessageType.Property, Content = new RTDMessage.PropertyMessage { ID = ID, Name = "YahooCodeLocal", Value = value } });
            }
        }
        public string YahooCodeLocal
        {
            get
            {
                return _yahooCode;
            }
            set
            {
                this._yahooCode = value;
                Factory.SetProperty(this, "YahooTicker", value);
            }
        }

        /// <summary>
        /// Property: CSI FTP Number code of Instrument.
        /// </summary>
        public int CSINumCode
        {
            get
            {
                return _csiNumCode;
            }
            set
            {
                this._csiNumCode = value;
                Factory.SetProperty(this, "CSINumCode", value);

                if (this.Cloud && !_loading)
                    if (RTDEngine.Publish(this))
                        RTDEngine.Send(new RTDMessage() { Type = RTDMessage.MessageType.Property, Content = new RTDMessage.PropertyMessage { ID = ID, Name = "CSINumCodeLocal", Value = value } });
            }
        }
        public int CSINumCodeLocal
        {
            get
            {
                return _csiNumCode;
            }
            set
            {
                this._csiNumCode = value;
                Factory.SetProperty(this, "CSINumCode", value);
            }
        }

        /// <summary>
        /// Function: Retrieve value from time series object.
        /// </summary>       
        /// <param name="date">DateTime value representing the date of the value to be retrieved.
        /// </param>
        /// <param name="type">Time series type of time series point.
        /// </param>
        /// <param name="provider">Provider of reference time series object (Standard is AQI).
        /// </param>
        /// <param name="timeSeriesRoll">Roll type of reference time series object.
        /// </param>
        /// <param name="num">Interger valued number to add or substract from reference date. 0 --> value of the reference date; -1 --> date prior to reference date; -5 --> 5 days prior to reference date.
        /// </param>
        virtual public double this[DateTime date, TimeSeriesType type, DataProvider provider, TimeSeriesRollType timeSeriesRoll, int num]
        {
            get
            {
                double val = Factory.GetTimeSeriesPoint(this, date, type, provider, timeSeriesRoll, num);
                if (double.IsNaN(val))
                    val = Factory.GetTimeSeriesPoint(this, date.Date, type, provider, timeSeriesRoll, num);

                return val;
            }
        }

        /// <summary>
        /// Function: Retrieve value from time series object.
        /// </summary>       
        /// <param name="date">DateTime value representing the date of the value to be retrieved.
        /// </param>
        /// <param name="type">Time series type of time series point.
        /// </param>
        /// <param name="provider">Provider of reference time series object (Standard is AQI).
        /// </param>
        /// <param name="timeSeriesRoll">Roll type of reference time series object.
        /// </param>
        virtual public double this[DateTime date, TimeSeriesType type, DataProvider provider, TimeSeriesRollType timeSeriesRoll]
        {
            get
            {
                return this[date, type, provider, timeSeriesRoll, 0];
            }
        }

        /// <summary>
        /// Function: Retrieve value from time series object.
        /// </summary>       
        /// <param name="date">DateTime value representing the date of the value to be retrieved.
        /// </param>        
        virtual public double this[DateTime date]
        {
            get
            {
                return this[date, TimeSeriesType.Last, DataProvider.DefaultProvider, TimeSeriesRoll, 0];
            }
        }

        /// <summary>
        /// Function: Retrieve value from time series object.
        /// </summary>       
        /// <param name="date">DateTime value representing the date of the value to be retrieved.
        /// </param>
        /// <param name="type">Time series type of time series point.
        /// </param>
        virtual public double this[DateTime date, TimeSeriesType ttype]
        {
            get
            {
                return this[date, ttype, DataProvider.DefaultProvider, TimeSeriesRoll, 0];
            }
        }

        /// <summary>
        /// Function: Retrieve value from time series object.
        /// </summary>       
        /// <param name="date">DateTime value representing the date of the value to be retrieved.
        /// </param>
        /// <param name="type">Time series type of time series point.
        /// </param>
        /// <param name="timeSeriesRoll">Roll type of reference time series object.
        /// </param>
        virtual public double this[DateTime date, TimeSeriesType ttype, TimeSeriesRollType roll]
        {
            get
            {
                return this[date, ttype, DataProvider.DefaultProvider, roll, 0];
            }
        }

        /// <summary>
        /// Function: Retrieve value from time series object.
        /// </summary>       
        /// <param name="date">DateTime value representing the date of the value to be retrieved.
        /// </param>
        /// <param name="type">Time series type of time series point.
        /// </param>
        /// <param name="timeSeriesRoll">Roll type of reference time series object.
        /// </param>
        /// <param name="num">Interger valued number to add or substract from reference date. 0 --> value of the reference date; -1 --> date prior to reference date; -5 --> 5 days prior to reference date.
        /// </param> 
        virtual public double this[DateTime date, TimeSeriesType ttype, TimeSeriesRollType roll, int num]
        {
            get
            {
                return this[date, ttype, DataProvider.DefaultProvider, roll, num];
            }
        }

        /// <summary>
        /// Function: Add value to time series object.
        /// </summary>       
        /// <param name="date">DateTime value representing the date of the value to be retrieved.
        /// </param>
        /// <param name="value">Value to be added to the time series object.
        /// </param>
        /// <param name="type">Time series type of time series point.
        /// </param>
        /// <param name="provider">Provider of reference time series object (Standard is AQI).
        /// </param>       
        public void AddTimeSeriesPoint(DateTime date, double value, TimeSeriesType type, DataProvider provider)
        {
            this.AddTimeSeriesPoint(date, value, type, provider, true);
        }

        /// <summary>
        /// Function: Add value to time series object.
        /// </summary>       
        /// <param name="date">DateTime value representing the date of the value to be retrieved.
        /// </param>
        /// <param name="value">Value to be added to the time series object.
        /// </param>
        /// <param name="type">Time series type of time series point.
        /// </param>
        /// <param name="provider">Provider of reference time series object (Standard is AQI).
        /// </param>
        /// <param name="onlyMemory">Boolean variable set as true if the added value should not be persistent. False otherwise.
        /// </param>
        public void AddTimeSeriesPoint(DateTime date, double value, TimeSeriesType type, DataProvider provider, Boolean onlyMemory, Boolean share = true)
        {
            CalculationEvent(date, value, type);
            Factory.AddTimeSeriesPoint(this, date, value, type, provider, onlyMemory);

            if (!this.SimulationObject && this.Cloud && share)
                if (RTDEngine.Publish(this))
                    RTDEngine.Send(new RTDMessage() { Type = RTDMessage.MessageType.MarketData, Content = new RTDMessage.MarketData() { InstrumentID = this.ID, Value = value, Type = type, Timestamp = date } });
        }

        /// <summary>
        /// Delegate: Structure for a function called during a value change event.
        /// </summary>       
        /// <param name="date">DateTime value representing the date of the value to be retrieved.
        /// </param>
        /// <param name="value">Value to be added to the time series object.
        /// </param>
        /// <param name="type">Time series type of time series point.
        /// </param>
        public delegate void ValueChangeFunction(DateTime date, double value, TimeSeriesType type);

        /// <summary>
        /// Event: Triggers linked to a value change event.
        /// </summary>       
        public event ValueChangeFunction ValueChangeTrigger = null;

        /// <summary>
        /// Function: Function called to trigger all the value change functions.
        /// </summary>       
        /// <param name="date">DateTime value representing the date of the value to be retrieved.
        /// </param>
        /// <param name="value">Value to be added to the time series object.
        /// </param>
        /// <param name="type">Time series type of time series point.
        /// </param>
        public void CalculationEvent(DateTime date, double value, TimeSeriesType type)
        {
            if (ValueChangeTrigger != null)
                ValueChangeTrigger(date, value, type);
        }

        /// <summary>
        /// Function: Save and commit all values changed for this Instrument in persistent storage.
        /// </summary>       
        public virtual void Save()
        {
            SaveLocal();
            if (!this.SimulationObject)
            {
                if (this.Cloud && !_loading)
                    if (RTDEngine.Publish(this))
                        RTDEngine.Send(new RTDMessage() { Type = RTDMessage.MessageType.Function, Content = new RTDMessage.FunctionMessage { ID = ID, Name = "SaveLocal", Parameters = null } });
            }
        }

        /// <summary>
        /// Function: Save and commit all values changed for this Instrument in persistent storage.
        /// </summary>       
        public virtual void SaveLocal()
        {
            if (!this.SimulationObject)
                Factory.Save(this);
        }

        /// <summary>
        /// Function: Retrieve time series object.
        /// </summary>       
        /// <param name="tstype">Time series type of object to be retrieved.
        /// </param>
        /// <param name="provider">Provider of reference time series object (Standard is AQI).
        /// </param>
        public TimeSeries GetTimeSeries(TimeSeriesType tstype, DataProvider provider)
        {
            return GetTimeSeries(tstype, provider, TimeSeriesLoadFromDatabase);
        }

        /// <summary>
        /// Function: Retrieve time series object.
        /// </summary>       
        /// <param name="tstype">Time series type of object to be retrieved.
        /// </param>
        /// <param name="LoadFromDatabase">True if time series is to be reloaded each type this function is called. False otherwise.
        /// </param>
        public TimeSeries GetTimeSeries(TimeSeriesType tstype, Boolean LoadFromDatabase)
        {
            return GetTimeSeries(tstype, DataProvider.DefaultProvider, LoadFromDatabase);
        }

        /// <summary>
        /// Function: Retrieve time series object.
        /// </summary>       
        /// <param name="tstype">Time series type of object to be retrieved.
        /// </param>
        /// <param name="provider">Provider of reference time series object (Standard is AQI).
        /// </param>
        /// <param name="LoadFromDatabase">True if time series is to be reloaded each type this function is called. False otherwise.
        /// </param>
        public TimeSeries GetTimeSeries(TimeSeriesType tstype, DataProvider provider, Boolean LoadFromDatabase)
        {
            if (tstype == TimeSeriesType.AdjClose)
            {
                Security security = this as Security;
                if (security != null)
                    return security.GetTotalReturnTimeSeries();
            }

            else if (tstype == TimeSeriesType.AdjPriceReturn)
            {
                Security security = this as Security;
                if (security != null)
                    return security.GetPriceReturnTimeSeries();
            }


            else if (tstype == TimeSeriesType.Close)
                return this.GetCloseTimeSeries();
            

            TimeSeries ts = Factory.GetTimeSeries(this, tstype, provider, LoadFromDatabase);
            if (tstype == TimeSeriesType.AdjClose && (ts == null || (ts != null && ts.Count == 0)))
                ts = Factory.GetTimeSeries(this, TimeSeriesType.Close, provider, LoadFromDatabase);
            return ts;
        }

        /// <summary>
        /// Function: Retrieve time series object.
        /// </summary>       
        /// <param name="tstype">Time series type of object to be retrieved.
        /// </param>
        public TimeSeries GetTimeSeries(TimeSeriesType tstype)
        {
            TimeSeries ts = GetTimeSeries(tstype, DataProvider.DefaultProvider);
            return ts;
        }

        private ConcurrentDictionary<DateTime, TimeSeries> _prDB = new ConcurrentDictionary<DateTime, TimeSeries>();
        public TimeSeries GetCloseTimeSeries()
        {
            TimeSeries last_ts = this.GetTimeSeries(TimeSeriesType.Last);

            if (last_ts == null || last_ts.Count == 0)
                return null;

            int count = last_ts.Count;

            DateTime lastDate = Calendar.Close(last_ts.DateTimes[count - 1]);

            if (_prDB.ContainsKey(lastDate))
                return _prDB[lastDate];



            TimeSeries tr_ts = new TimeSeries(1);
            //tr_ts.AddDataPoint(last_ts.DateTimes[0], last_ts[0]);

            DateTime last_date = DateTime.MinValue;

            for (int i = 0; i < count; i++)
            {
                DateTime _d = last_ts.DateTimes[i];
                DateTime date = Calendar.Close(_d);

                if (_d >= date && last_date != date.Date)                
                {
                    last_date = date.Date;
                   
                    double value = this[date, TimeSeriesType.Last, TimeSeriesRollType.Last];
                    tr_ts.AddDataPoint(date, value);
                }
                else if(i < count && last_ts.DateTimes[i + 1].Date != _d.Date && _d.Date != last_date.Date && _d.DayOfWeek != DayOfWeek.Sunday)
                {
                    last_date = date.Date;
                    
                    double value = this[date, TimeSeriesType.Last, TimeSeriesRollType.Last];
                    tr_ts.AddDataPoint(date, value);
                }
            }
                
            _prDB.TryAdd(lastDate, tr_ts);

            return tr_ts;
        }


        public delegate Dictionary<DateTime, double> GetTimeSeriesType(Instrument instrument, DateTime date, TimeSeriesType tstype);
        public static GetTimeSeriesType GetTimeSeriesFunction = null;

        public delegate void SubscribeType(Instrument instrument);
        public static SubscribeType SubscribeFunction = null;

        public delegate Instrument FindInstrumentType(int id, string name);
        public static FindInstrumentType FindInstrumentFunction = null;

        /// <summary>
        /// Function: Remove specific time series object from memory and persistent storage.
        /// </summary>       
        /// <param name="tstype">Time series type of object to be retrieved.
        /// </param>
        /// <param name="provider">Provider of reference time series object (Standard is AQI).
        /// </param>
        public void RemoveTimeSeries(TimeSeriesType tstype, DataProvider provider)
        {
            Factory.RemoveTimeSeries(this, tstype, provider);
        }

        /// <summary>
        /// Function: Remove all time series objects from memory and persistent storage.
        /// </summary>       
        public void RemoveTimeSeries()
        {
            Factory.RemoveTimeSeries(this);
        }

        /// <summary>
        /// Function: Create instrument
        /// </summary>       
        /// <param name="name">string valued instrument name.
        /// </param>
        /// <param name="instrumentType">InstrumentType valued instrument type.
        /// </param>
        /// <param name="description">string valued instrument description.
        /// </param>
        /// <param name="currency">Currency valued currency object.
        /// </param>
        /// <param name="fundingType">Funding type of instrument.
        /// </param>        
        /// <param name="simulated">Booblean valued simulation flag. True if simulated, False otherwise.
        /// </param>        
        public static Instrument CreateInstrument(string name, InstrumentType instrumentType, string description, Currency currency, FundingType fundingType, Boolean simulated)
        {
            Instrument instrument = Factory.CreateInstrument(name, instrumentType, description, currency, fundingType, simulated, false);

            return instrument;
        }

        /// <summary>
        /// Function: Create instrument
        /// </summary>       
        /// <param name="name">string valued instrument name.
        /// </param>
        /// <param name="instrumentType">InstrumentType valued instrument type.
        /// </param>
        /// <param name="description">string valued instrument description.
        /// </param>
        /// <param name="currency">Currency valued currency object.
        /// </param>
        /// <param name="fundingType">Funding type of instrument.
        /// </param>        
        /// <param name="simulated">Booblean valued simulation flag. True if simulated, False otherwise.
        /// </param>        
        /// <param name="cloud">Booblean valued simulation flag. True if simulated, False otherwise.
        /// </param>        
        public static Instrument CreateInstrument(string name, InstrumentType instrumentType, string description, Currency currency, FundingType fundingType, Boolean simulated, Boolean cloud)
        {
            Instrument instrument = Factory.CreateInstrument(name, instrumentType, description, currency, fundingType, simulated, cloud);

            return instrument;
        }

        /// <summary>
        /// Function: Create instrument. Not simulated by default.
        /// </summary>       
        /// <param name="name">string valued instrument name.
        /// </param>
        /// <param name="instrumentType">InstrumentType valued instrument type.
        /// </param>
        /// <param name="description">string valued instrument description.
        /// </param>
        /// <param name="currency">Currency valued currency object.
        /// </param>
        /// <param name="fundingType">Funding type of instrument.
        /// </param>        
        public static Instrument CreateInstrument(string name, InstrumentType instrumentType, string description, Currency currency, FundingType fundingType)
        {
            Instrument instrument = Factory.CreateInstrument(name, instrumentType, description, currency, fundingType);

            return instrument;
        }

        public delegate void FindInstrumentCallbackType(Instrument instrument);
        public static FindInstrumentCallbackType FindInstrumentCallback = null;

        public delegate void DeleteCallbackType(Instrument instrument);
        public static DeleteCallbackType DeleteCallback = null;

        public delegate void RemoveCallbackType(Instrument instrument);
        public static RemoveCallbackType RemoveCallback = null;


        /// <summary>
        /// Function: Find instrument by name in both memory and persistent storage
        /// </summary>       
        /// <param name="name">string valued instrument name.
        /// </param>
        public static IEnumerable<Instrument> FindInstruments(string name)
        {
            return Factory.FindInstruments(User.CurrentUser, name);
        }

        /// <summary>
        /// Function: Find instrument by name in both memory and persistent storage
        /// </summary>       
        /// <param name="name">string valued instrument name.
        /// </param>
        public static Instrument FindInstrument(string name)
        {
            Instrument instrument = Factory.FindInstrument(User.CurrentUser, name);

            if (FindInstrumentCallback != null)
                FindInstrumentCallback(instrument);

            if (instrument != null)
                instrument._loading = false;
            return instrument;
        }

        /// <summary>
        /// Function: Find instrument by id in both memory and persistent storage.
        /// </summary>
        /// <remarks>
        /// Only used Internaly by Kernel
        /// </remarks>
        /// <param name="id">Integer valued instrument id.
        /// </param>
        internal static Instrument FindSecureInstrument(int id)
        {
            Instrument instrument = Factory.FindSecureInstrument(id);

            if (FindInstrumentCallback != null)
                FindInstrumentCallback(instrument);

            if (instrument != null)
                instrument._loading = false;
            return instrument;
        }

        /// <summary>
        /// Function: Find instrument by id in both memory and persistent storage. This function does not restrict Instruments to permission contraints therefore its only used by the Kernel.
        /// </summary>       
        /// <param name="id">Integer valued instrument id.
        /// </param>
        public static Instrument FindCleanInstrument(int id)
        {
            Instrument instrument = Factory.FindCleanInstrument(User.CurrentUser, id);

            if (FindInstrumentCallback != null)
                FindInstrumentCallback(instrument);

            if (instrument != null)
                instrument._loading = false;
            return instrument;
        }

        /// <summary>
        /// Function: Find instrument by id in both memory and persistent storage.
        /// </summary>       
        /// <param name="id">Integer valued instrument id.
        /// </param>
        public static Instrument FindInstrument(int id)
        {
            Instrument instrument = Factory.FindInstrument(User.CurrentUser, id);

            if (FindInstrumentCallback != null)
                FindInstrumentCallback(instrument);

            if (instrument != null)
                instrument._loading = false;
            return instrument;
        }

        /// <summary>
        /// Function: Find instrument by id in both memory and persistent storage.
        /// </summary>       
        /// <param name="CSIUAMarket">string valued instrument CSI Unfair Advantage code.
        /// </param>
        /// <param name="CSIDeliveryCode">string valued instrument CSI FTP code.
        /// </param> 
        public static Instrument FindInstrumentCSIUA(string CSIUAMarket, int CSIDeliveryCode)
        {
            Instrument instrument = Factory.FindInstrumentCSIUA(User.CurrentUser, CSIUAMarket, CSIDeliveryCode);

            if (FindInstrumentCallback != null)
                FindInstrumentCallback(instrument);

            if (instrument != null)
                instrument._loading = false;
            return instrument;
        }

        /// <summary>
        /// Function: Find instrument by id in both memory and persistent storage.
        /// </summary>       
        /// <param name="CSIUAMarket">string valued instrument CSI Unfair Advantage code.
        /// </param>
        /// <param name="CSIDeliveryCode">string valued instrument CSI FTP code.
        /// </param> 
        /// <param name="onlyCache">Boolean valued flag. True if only search in memory, False if search in both memory and persistent storage.
        /// </param> 
        public static Instrument FindInstrumentCSI(int CSINumCode, int CSIDeliveryCode, Boolean onlyCache)
        {
            Instrument instrument = Factory.FindInstrumentCSI(User.CurrentUser, CSINumCode, CSIDeliveryCode, onlyCache);

            if (instrument != null)
                RTDEngine.Subscribe(instrument.ID.ToString());

            if (FindInstrumentCallback != null)
                FindInstrumentCallback(instrument);
            if (instrument != null)
                instrument._loading = false;
            return instrument;
        }

        /// <summary>
        /// Function: Clear memory cache of CSI linked instruments.
        /// </summary>
        public static void ClearCSICache()
        {
            Factory.ClearCSICache();
        }


        /// <summary>
        /// Property: Retrieve all instruments in memory and persistent storage.
        /// </summary>
        //[Newtonsoft.Json.JsonIgnore]
        public static List<Instrument> Instruments()
        {
            //get
            {
                return Factory.Instruments(User.CurrentUser);
            }
        }


        /// <summary>
        /// Function: Retrieve a list of instruments from both memory and persistent storage.
        /// </summary>
        /// <param name="type">Type of instrument to be retrieved.
        /// </param>         
        public static List<Instrument> InstrumentsType(InstrumentType type)
        {
            return Factory.InstrumentsType(User.CurrentUser, type);
        }
    }
}
