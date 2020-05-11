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
using System.Threading.Tasks;

using System.Data;
using AQI.AQILabs.Kernel.Factories;
using AQI.AQILabs.Kernel.Numerics.Util;

using System.Data.SqlTypes;

using QuantApp.Kernel;


namespace AQI.AQILabs.Kernel.Adapters.SQL.Factories
{

    public class SQLInstrumentFactory : IInstrumentFactory
    {
        internal static string _mainTableName = "Instrument";
        internal static string _systemDataTableName = "SystemData";
        internal static string _thirdPartyDataTableName = "ThirdPartyData";
        internal static string _timeSeriesTableName = "TimeSeries";
        internal static string _categoriesTableName = "Categories";

        private ConcurrentDictionary<int, Instrument> _instrumentIdDB = new ConcurrentDictionary<int, Instrument>();
        private ConcurrentDictionary<string, Instrument> _instrumentNameDB = new ConcurrentDictionary<string, Instrument>();

        private ConcurrentDictionary<string, TimeSeries> _timeSeriesDatabase = new ConcurrentDictionary<string, TimeSeries>();
        private ConcurrentDictionary<string, ConcurrentDictionary<DateTime, TimeSeriesPoint>> _newTimeSeriesDatabase = new ConcurrentDictionary<string, ConcurrentDictionary<DateTime, TimeSeriesPoint>>();

        private Dictionary<string, int> _UADB = new Dictionary<string, int>();
        private List<string> _CSICache = new List<string>();

        public readonly static object createInstrumentLock = new object();
        public Instrument CreateInstrument(string name, InstrumentType instrumentType, string description, Currency currency, FundingType fundingType, Boolean simulated, Boolean cloud)
        {
            lock (createInstrumentLock)
            {
                if (!simulated)
                    return CreateInstrument(name, instrumentType, description, currency, fundingType);

                else if (!_instrumentNameDB.ContainsKey((name.StartsWith("$") ? "" : "$") + name))
                {

                    int id = -183;
                    do { id--; } while (_instrumentIdDB.ContainsKey(id));
                    Instrument i = new Instrument(id, (name.StartsWith("$") ? "" : "$") + name, description, null, instrumentType, currency.ID, (int)fundingType, -1, DateTime.Now, DateTime.Now, TimeSeriesAccessType.NotSet, TimeSeriesRollType.Last, AssetClass.NoAssetClass, GeographicalRegion.NoRegion, 0.0, 0.0, 0.0, DayCountConvention.Act360, 0.0, null, null, null, -1, -1, null, false, simulated);
                    UpdateInstrumentDB(i);

                    SystemLog.Write(DateTime.Now, i, SystemLog.Type.Production, "Created Instrument");
                    return i;
                }

                throw new Exception("Instrument Already Exists");
            }
        }

        public Instrument CreateInstrument(string name, InstrumentType instrumentType, string description, Currency currency, FundingType fundingType)
        {
            lock (createInstrumentLock)
            {
                string searchString = "Name LIKE '" + name + "'";
                string targetString = null;
                DataTable table = Database.DB["Kernel"].GetDataTable(_mainTableName, targetString, searchString);
                DataRowCollection rows = table.Rows;

                if (rows.Count == 0)
                {
                    int id = -1;
                    DataTable idtable = Database.DB["Kernel"].GetDataTable(_mainTableName, "MAX(ID)", null);
                    foreach (DataRow ir in idtable.Rows)
                    {
                        if(ir[0] is DBNull)
                            id = 0;
                        else
                            id = Convert.ToInt32(ir[0]);
                    }
                    id++;
                    DateTime createTime = DateTime.Now;
                    DataRow r = table.NewRow();
                    
                    r["ID"] = id;
                    r["Name"] = name;
                    r["InstrumentTypeID"] = (int)instrumentType;
                    r["Description"] = description;
                    r["CurrencyID"] = currency.ID;
                    r["FundingTypeID"] = (int)fundingType;
                    r["CustomCalendarID"] = -1;
                    rows.Add(r);
                    Database.DB["Kernel"].UpdateDataTable(table);

                    string searchStringSystemData = "ID = " + id;
                    string targetStringSystemData = null;
                    DataTable tableSystemData = Database.DB["Kernel"].GetDataTable(_systemDataTableName, targetStringSystemData, searchStringSystemData);
                    DataRow rSystemData = tableSystemData.NewRow();
                    rSystemData["ID"] = id;
                    rSystemData["CreateTime"] = createTime;
                    rSystemData["UpdateTime"] = createTime;
                    rSystemData["Revision"] = 0;
                    rSystemData["Deleted"] = false;
                    rSystemData["TimeSeriesAccessType"] = (int)TimeSeriesAccessType.Read;
                    rSystemData["TimeSeriesRollType"] = (int)TimeSeriesRollType.Exact;

                    rSystemData["ScaleFactor"] = 1.0;
                    rSystemData["ExecutionCost"] = 0.0;
                    rSystemData["CarryCostLong"] = 0.0;
                    rSystemData["CarryCostShort"] = 0.0;
                    rSystemData["CarryCostDayCount"] = DayCountConvention.Act360;
                    rSystemData["CarryCostDayCountBase"] = 360.0;


                    tableSystemData.Rows.Add(rSystemData);
                    Database.DB["Kernel"].UpdateDataTable(tableSystemData);

                    string searchStringThirdPartyData = "ID = " + id;
                    string targetStringThirdPartyData = null;
                    DataTable tableThirdPartyData = Database.DB["Kernel"].GetDataTable(_thirdPartyDataTableName, targetStringThirdPartyData, searchStringThirdPartyData);
                    DataRow rThirdPartyData = tableThirdPartyData.NewRow();
                    rThirdPartyData["ID"] = id;
                    rThirdPartyData["BloombergTicker"] = null;
                    rThirdPartyData["ReutersRIC"] = null;
                    tableThirdPartyData.Rows.Add(rThirdPartyData);
                    Database.DB["Kernel"].UpdateDataTable(tableThirdPartyData);

                    string searchStringCategories = "ID = " + id;
                    string targetStringCategories = null;
                    DataTable tableCategories = Database.DB["Kernel"].GetDataTable(_categoriesTableName, targetStringCategories, searchStringCategories);
                    DataRow rCategories = tableCategories.NewRow();
                    rCategories["ID"] = id;
                    rCategories["AssetClass"] = AssetClass.NoAssetClass;
                    rCategories["GeographicalRegion"] = GeographicalRegion.NoRegion;
                    tableCategories.Rows.Add(rCategories);
                    Database.DB["Kernel"].UpdateDataTable(tableCategories);

                    Instrument i = FindSecureInstrument(id);

                    SystemLog.Write(DateTime.Now, i, SystemLog.Type.Production, "Created Instrument");
                    return i;
                }
                throw new Exception("Instrument Already Exists");
            }
        }

        public readonly static object updateInstrumentLock = new object();
        public void UpdateInstrumentDB(Instrument instrument)
        {
            if (!_instrumentIdDB.ContainsKey(instrument.ID))
                _instrumentIdDB.TryAdd(instrument.ID, instrument);
            else
                _instrumentIdDB[instrument.ID] = instrument;

            if (!_instrumentNameDB.ContainsKey(instrument.Name))
                _instrumentNameDB.TryAdd(instrument.Name, instrument);
            else
                _instrumentNameDB[instrument.Name] = instrument;
        }

        private ConcurrentDictionary<string, string> _notFound = new ConcurrentDictionary<string, string>();
        public Instrument FindInstrument(User user, string name)
        {
            if (_instrumentNameDB.ContainsKey(name))
            {
                Instrument i = _instrumentNameDB[name];

                if (i.InstrumentType == InstrumentType.Strategy)
                {
                    try
                    {
                        Strategy strategy = i as Strategy;
                        if (strategy == null)
                            return null;// i;
                        _instrumentNameDB[name] = strategy;
                        return strategy;
                    }
                    catch { }
                }

                else if (i.InstrumentType == InstrumentType.Portfolio)
                {
                    try
                    {
                        Portfolio portfolio = i as Portfolio;
                        if (portfolio == null)
                            return null;// i;
                        _instrumentNameDB[name] = portfolio;
                        return portfolio;
                    }
                    catch { }
                }

                if (user.Permission(i) != AccessType.Denied)
                {
                    UpdateInstrumentDB(i);
                    return i;
                }
                else if (i.InstrumentType == InstrumentType.Portfolio)
                {
                    try
                    {
                        if (((Portfolio)i).Strategy != null && user.Permission((((Portfolio)i).Strategy)) != AccessType.Denied)
                        {
                            UpdateInstrumentDB(i);
                            return i;
                        }
                    }
                    catch { }
                }
                else if (i.InstrumentType == InstrumentType.Strategy)
                {
                    try
                    {
                        if ((((Strategy)i).Portfolio != null) && (((Strategy)i).Portfolio.ParentPortfolio != null) && (((Strategy)i).Portfolio.ParentPortfolio.Strategy != null) && user.Permission(((Strategy)i).Portfolio.ParentPortfolio.Strategy) != AccessType.Denied)
                        {
                            UpdateInstrumentDB(i);
                            return i;
                        }
                    }
                    catch { }
                }

                return i;
            }

            if(_notFound.ContainsKey(name))
                return null;

            string searchString = "Name LIKE '" + name + "'";
            string targetString = null;
            DataTable table = Database.DB["Kernel"].GetDataTable(_mainTableName, targetString, searchString);

            DataRowCollection rows = table.Rows;
            if (rows.Count == 0)
            {
                _notFound.TryAdd(name,name);
                return null;
            }

            else
            {
                int id = GetValue<int>(rows[0], "ID");
                Instrument i = FindInstrument(user, id);
                return i;
            }
        }

        public IEnumerable<Instrument> FindInstruments(User user, string name)
        {
            string searchString = "Name LIKE '%" + name + "%' OR Description LIKE '%" + name + "%'";
            string targetString = "Top 100 ID";

            if(Database.DB["Kernel"] is QuantApp.Kernel.Adapters.SQL.SQLiteDataSetAdapter || Database.DB["Kernel"] is QuantApp.Kernel.Adapters.SQL.PostgresDataSetAdapter)
            {
                searchString += " LIMIT 100";
                targetString = "ID";
            }

            DataTable table = Database.DB["Kernel"].GetDataTable(_mainTableName, targetString, searchString);

            List<Instrument> ret = new List<Instrument>();

            DataRowCollection rows = table.Rows;
            foreach (DataRow row in rows)
            {
                int id = GetValue<int>(row, "ID");
                Instrument i = FindInstrument(user, id);
                ret.Add(i);
            }

            return ret;
        }


        public DateTime LastSave = DateTime.MinValue;
        public bool IsSaving = false;
        public void SaveAll()
        {
            try
            {
                Parallel.ForEach(_instrumentIdDB.Values, new ParallelOptions { MaxDegreeOfParallelism = 20 }, i =>
                {
                    IsSaving = true;
                    
                    try
                    {
                        if (!i.SimulationObject)
                        {
                            if (!(i.InstrumentType == InstrumentType.Strategy && (i as Strategy != null) && (i as Strategy).Portfolio != null))
                                i.SaveLocal();
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                });
                IsSaving = false;
                LastSave = DateTime.Now;
            }
            catch { }
        }

        public void SaveAllLoop(int min = 180)
        {
            while (true)
            {
                System.Threading.Thread.Sleep(60 * 1000 * min);
                SaveAll();
            }
        }

        private T GetValue<T>(DataRow row, string columnname)
        {
            object res = null;
            if (row.RowState == DataRowState.Detached)
                return (T)res;
            if (typeof(T) == typeof(string))
                res = "";
            else if (typeof(T) == typeof(int))
                res = int.MinValue;
            else if (typeof(T) == typeof(double))
                res = double.NaN;
            else if (typeof(T) == typeof(DateTime))
                res = DateTime.MinValue;
            else if (typeof(T) == typeof(bool))
                res = false;
            object obj = row.Table.Columns.Contains(columnname) ? row[columnname] : null;
            if (obj is DBNull || obj == null)
                return (T)res;


            if (obj != null)
            {
                if (obj is Int64)
                    return (T)(object)Convert.ToInt32(obj);
            }

            return (T)obj;
        }

        private Dictionary<int, DataTable> _mainTables = new Dictionary<int, DataTable>();
        private Dictionary<int, DataTable> _systemTables = new Dictionary<int, DataTable>();
        private Dictionary<int, DataTable> _thirdPartyTables = new Dictionary<int, DataTable>();
        private Dictionary<int, DataTable> _categoriesTables = new Dictionary<int, DataTable>();

        public readonly static object findCleanInstrumentLock = new object();
        public Instrument FindCleanInstrument(User user, int id)
        {
            lock (findCleanInstrumentLock)
            {

                if (_instrumentIdDB.ContainsKey(id))
                    return _instrumentIdDB[id];

                string searchString = _mainTableName + ".ID = " + id + " AND " + _mainTableName + ".ID = " + _systemDataTableName + ".ID AND " + _mainTableName + ".ID = " + _thirdPartyDataTableName + ".ID AND " + _mainTableName + ".ID = " + _categoriesTableName + ".ID";
                
                string targetString = "Name, LongDescription, Description, InstrumentTypeID, CurrencyID, FundingTypeID, CustomCalendarID, CreateTime, UpdateTime, TimeSeriesAccessType, TimeSeriesRollType, ExecutionCost, CarryCostLong, CarryCostShort, CarryCostDayCount, CarryCostDayCountBase, Deleted, BloombergTicker, ReutersRIC, CSIUAMarket, CSIDeliveryCode, CSINumCode, YahooTicker, AssetClass, GeographicalRegion";
                DataTable table = Database.DB["Kernel"].GetDataTable(_mainTableName + "," + _systemDataTableName + "," + _thirdPartyDataTableName + "," + _categoriesTableName, targetString, searchString);

                DataRowCollection rows = table.Rows;
                if (rows.Count == 0)
                    return null;
                else
                {
                    DataRow r = rows[0];
                    string name = GetValue<string>(r, "Name");
                    string longDescription = GetValue<string>(r, "LongDescription");
                    string description = GetValue<string>(r, "Description");
                    InstrumentType instrumentType = (InstrumentType)GetValue<int>(r, "InstrumentTypeID");
                    int currencyID = GetValue<int>(r, "CurrencyID");
                    FundingType fundingType = (FundingType)GetValue<int>(r, "FundingTypeID");
                    int customCalendarID = GetValue<int>(r, "CustomCalendarID");
                    
                    DataRow rSystemData = r;// tableSystemData.Rows[0];
                    DateTime createTime = GetValue<DateTime>(rSystemData, "CreateTime");
                    DateTime updateTime = GetValue<DateTime>(rSystemData, "UpdateTime");
                    TimeSeriesAccessType timeSeriesAccessType = (TimeSeriesAccessType)GetValue<int>(rSystemData, "TimeSeriesAccessType");
                    TimeSeriesRollType timeSeriesRollType = (TimeSeriesRollType)GetValue<int>(rSystemData, "TimeSeriesRollType");
                    
                    double executionCost = GetValue<double>(rSystemData, "ExecutionCost");
                    double carryCostLong = GetValue<double>(rSystemData, "CarryCostLong");
                    double carryCostShort = GetValue<double>(rSystemData, "CarryCostShort");
                    DayCountConvention dayCount = (DayCountConvention)GetValue<int>(rSystemData, "CarryCostDayCount");
                    double carryCostDayCountBase = GetValue<double>(rSystemData, "CarryCostDayCountBase");
                    bool deleted = GetValue<bool>(rSystemData, "Deleted");

                    DataRow rThirdPartyData = r;

                    string bloombergTicker = GetValue<string>(rThirdPartyData, "BloombergTicker");
                    string reutersRic = GetValue<string>(rThirdPartyData, "ReutersRIC");
                    string csiUAMarket = GetValue<string>(rThirdPartyData, "CSIUAMarket");
                    int csiDeliveryCode = GetValue<int>(rThirdPartyData, "CSIDeliveryCode");
                    int csiNumCode = GetValue<int>(rThirdPartyData, "CSINumCode");
                    string yahooTicker = GetValue<string>(rThirdPartyData, "YahooTicker");

                    DataRow rCategories = r;
                    AssetClass assetClass = GetValue<AssetClass>(rCategories, "AssetClass");
                    GeographicalRegion region = GetValue<GeographicalRegion>(rCategories, "GeographicalRegion");

                    Instrument i = new Instrument(id, name, description, longDescription, instrumentType, currencyID, (int)fundingType, customCalendarID, createTime, updateTime, timeSeriesAccessType, timeSeriesRollType, assetClass, region, executionCost, carryCostLong, carryCostShort, dayCount, carryCostDayCountBase, bloombergTicker, reutersRic, csiUAMarket, csiDeliveryCode, csiNumCode, yahooTicker, deleted, false);
                    UpdateInstrumentDB(i);
                    return i;
                }
            }
        }

        public readonly static object findSecureInstrumentLock = new object();
        public Instrument FindSecureInstrument(int id)
        {
            lock (findSecureInstrumentLock)
            {

                if (_instrumentIdDB.ContainsKey(id))
                {
                    Instrument i = _instrumentIdDB[id];
                    if (i.InstrumentType == InstrumentType.Strategy)
                    {
                        if (i is Strategy)
                            return i;
                        try
                        {
                            Strategy ii = Strategy.FindStrategy(i);
                            if (ii != null)
                            {
                                i = ii;
                                _instrumentIdDB[id] = ii;
                            }
                        }
                        catch { }
                    }
                    else if (i.InstrumentType == InstrumentType.Portfolio)
                    {
                        if (i is Portfolio)
                            return i;
                        try
                        {
                            Portfolio ii = Portfolio.FindPortfolio(i);
                            if (ii != null)
                            {
                                i = ii;
                                _instrumentIdDB[id] = ii;
                            }
                        }
                        catch { }
                    }
                    else if (i.InstrumentType == InstrumentType.Future)
                    {
                        if (i is Future)
                            return i;
                        try
                        {
                            Future ii = Future.FindFuture(Security.FindSecurity(i));
                            if (ii != null)
                            {
                                i = ii;
                                _instrumentIdDB[id] = ii;
                            }
                        }
                        catch { }
                    }
                    else if (i.InstrumentType == InstrumentType.Equity || i.InstrumentType == InstrumentType.ETF || i.InstrumentType == InstrumentType.Fund)
                    {
                        if (i is Security)
                            return i;
                        try
                        {
                            Security ii = Security.FindSecurity(i);
                            if (ii != null)
                            {
                                i = ii;
                                _instrumentIdDB[id] = ii;
                            }
                        }
                        catch { }
                    }
                    else if (i.InstrumentType == InstrumentType.Deposit)
                    {
                        if (i is Deposit)
                            return i;
                        try
                        {
                            Deposit ii = Deposit.FindDeposit(InterestRate.FindInterestRate(i));
                            if (ii != null)
                            {
                                i = ii;
                                _instrumentIdDB[id] = ii;
                            }
                        }
                        catch { }
                    }
                    else if (i.InstrumentType == InstrumentType.InterestRateSwap)
                    {
                        if (i is InterestRateSwap)
                            return i;
                        try
                        {
                            InterestRateSwap ii = InterestRateSwap.FindInterestRateSwap(InterestRate.FindInterestRate(i));
                            if (ii != null)
                            {
                                i = ii;
                                _instrumentIdDB[id] = ii;
                            }
                        }
                        catch { }
                    }

                    UpdateInstrumentDB(i);
                    return i;
                }

                string searchString = _mainTableName + ".ID = " + id + " AND " + _mainTableName + ".ID = " + _systemDataTableName + ".ID AND " + _mainTableName + ".ID = " + _thirdPartyDataTableName + ".ID AND " + _mainTableName + ".ID = " + _categoriesTableName + ".ID";
                string targetString = "Name, LongDescription, Description, InstrumentTypeID, CurrencyID, FundingTypeID, CustomCalendarID, CreateTime, UpdateTime, TimeSeriesAccessType, TimeSeriesRollType, ExecutionCost, CarryCostLong, CarryCostShort, CarryCostDayCount, CarryCostDayCountBase, Deleted, BloombergTicker, ReutersRIC, CSIUAMarket, CSIDeliveryCode, CSINumCode, YahooTicker, AssetClass, GeographicalRegion";
                DataTable table = Database.DB["Kernel"].GetDataTable(_mainTableName + "," + _systemDataTableName + "," + _thirdPartyDataTableName + "," + _categoriesTableName, targetString, searchString);
                
                DataRowCollection rows = table.Rows;
                if (rows.Count == 0)
                    return null;
                else
                {
                    DataRow r = rows[0];
                    string name = GetValue<string>(r, "Name");
                    string longDescription = GetValue<string>(r, "LongDescription");
                    string description = GetValue<string>(r, "Description");
                    InstrumentType instrumentType = (InstrumentType)GetValue<int>(r, "InstrumentTypeID");
                    int currencyID = GetValue<int>(r, "CurrencyID");
                    FundingType fundingType = (FundingType)GetValue<int>(r, "FundingTypeID");
                    int customCalendarID = GetValue<int>(r, "CustomCalendarID");
                    
                    DataRow rSystemData = r;
                    DateTime createTime = GetValue<DateTime>(rSystemData, "CreateTime");
                    DateTime updateTime = GetValue<DateTime>(rSystemData, "UpdateTime");
                    TimeSeriesAccessType timeSeriesAccessType = (TimeSeriesAccessType)GetValue<int>(rSystemData, "TimeSeriesAccessType");
                    TimeSeriesRollType timeSeriesRollType = (TimeSeriesRollType)GetValue<int>(rSystemData, "TimeSeriesRollType");
                    
                    double executionCost = GetValue<double>(rSystemData, "ExecutionCost");
                    double carryCostLong = GetValue<double>(rSystemData, "CarryCostLong");
                    double carryCostShort = GetValue<double>(rSystemData, "CarryCostShort");
                    DayCountConvention dayCount = (DayCountConvention)GetValue<int>(rSystemData, "CarryCostDayCount");
                    double carryCostDayCountBase = GetValue<double>(rSystemData, "CarryCostDayCountBase");
                    bool deleted = GetValue<bool>(rSystemData, "Deleted");

                    DataRow rThirdPartyData = r;
                    
                    string bloombergTicker = GetValue<string>(rThirdPartyData, "BloombergTicker");
                    string reutersRic = GetValue<string>(rThirdPartyData, "ReutersRIC");
                    string csiUAMarket = GetValue<string>(rThirdPartyData, "CSIUAMarket");
                    int csiDeliveryCode = GetValue<int>(rThirdPartyData, "CSIDeliveryCode");
                    int csiNumCode = GetValue<int>(rThirdPartyData, "CSINumCode");
                    string yahooTicker = GetValue<string>(rThirdPartyData, "YahooTicker");

                    DataRow rCategories = r;
                    AssetClass assetClass = GetValue<AssetClass>(rCategories, "AssetClass");
                    GeographicalRegion region = GetValue<GeographicalRegion>(rCategories, "GeographicalRegion");

                    Instrument i = new Instrument(id, name, description, longDescription, instrumentType, currencyID, (int)fundingType, customCalendarID, createTime, updateTime, timeSeriesAccessType, timeSeriesRollType, assetClass, region, executionCost, carryCostLong, carryCostShort, dayCount, carryCostDayCountBase, bloombergTicker, reutersRic, csiUAMarket, csiDeliveryCode, csiNumCode, yahooTicker, deleted, false);
                    Instrument ii = null;
                    if (i.InstrumentType == InstrumentType.Strategy)
                        ii = Strategy.FindStrategy(i);
                    else if (i.InstrumentType == InstrumentType.Portfolio)
                        ii = Portfolio.FindPortfolio(i);
                    else if (i.InstrumentType == InstrumentType.Future)
                    {
                        Security s = Security.FindSecurity(i);
                        if (s != null)
                            ii = Future.FindFuture(s);
                    }
                    else if (i.InstrumentType == InstrumentType.Equity || i.InstrumentType == InstrumentType.ETF || i.InstrumentType == InstrumentType.Fund)
                        ii = Security.FindSecurity(i);

                    else if (i.InstrumentType == InstrumentType.Deposit)
                    {
                        var iii = InterestRate.FindInterestRate(i);
                        if(iii != null)
                            ii = Deposit.FindDeposit(iii);
                    }

                    else if (i.InstrumentType == InstrumentType.InterestRateSwap)
                    {
                        var iii = InterestRate.FindInterestRate(i);
                        if(iii != null)
                            ii = InterestRateSwap.FindInterestRateSwap(iii);
                    }

                    if (ii != null)
                        i = ii;
                    UpdateInstrumentDB(i);
                    return i;
                }
            }
        }

        public readonly static object setPropertyLock = new object();
        public void SetProperty(Instrument instrument, string name, object value)
        {
            lock (setPropertyLock)
            {
                DataTable table = null;

                if (name == "Name" || name == "LongDescription" || name == "Description" || name == "InstrumentTypeID" || name == "CurrencyID" || name == "FundingTypeID" || name == "CustomCalendarID")
                {
                    if (!_mainTables.ContainsKey(instrument.ID))
                    {
                        string search = "ID = " + instrument.ID;
                        string target = null;
                        _mainTables.Add(instrument.ID, Database.DB["Kernel"].GetDataTable(_mainTableName, target, search));
                    }

                    table = _mainTables[instrument.ID];
                }
                else if (name == "CreateTime" || name == "UpdateTime" || name == "TimeSeriesAccessType" || name == "TimeSeriesRollType" || name == "ExecutionCost" || name == "CarryCostLong" || name == "CarryCostShort" || name == "CarryCostDayCount" || name == "CarryCostDayCountBase" || name == "ScaleFactor" || name == "Deleted")
                {
                    if (!_systemTables.ContainsKey(instrument.ID))
                    {
                        string search = "ID = " + instrument.ID;
                        string target = null;
                        _systemTables.Add(instrument.ID, Database.DB["Kernel"].GetDataTable(_systemDataTableName, target, search));
                    }

                    table = _systemTables[instrument.ID];
                }
                else if (name == "BloombergTicker" || name == "ReutersRIC" || name == "CSIUAMarket" || name == "CSIDeliveryCode" || name == "CSINumCode" || name == "YahooTicker")
                {
                    if (!_thirdPartyTables.ContainsKey(instrument.ID))
                    {
                        string search = "ID = " + instrument.ID;
                        string target = null;
                        _thirdPartyTables.Add(instrument.ID, Database.DB["Kernel"].GetDataTable(_thirdPartyDataTableName, target, search));
                    }

                    table = _thirdPartyTables[instrument.ID];
                }
                else if (name == "AssetClass" || name == "GeographicalRegion")
                {
                    if (!_categoriesTables.ContainsKey(instrument.ID))
                    {
                        string search = "ID = " + instrument.ID;
                        string target = null;
                        _categoriesTables.Add(instrument.ID, Database.DB["Kernel"].GetDataTable(_categoriesTableName, target, search));
                    }

                    table = _categoriesTables[instrument.ID];
                }

                if (table != null)
                {
                    DataRowCollection rows = table.Rows;
                    if (rows.Count == 0)
                        return;

                    DataRow row = rows[0];
                    if (row[name] != value)
                    {
                        row[name] = value;
                        Database.DB["Kernel"].UpdateDataTable(table);
                    }
                }
            }
        }

        public readonly static object findInstrumentUserLock = new object();
        public Instrument FindInstrument(User user, int id)
        {
            Instrument ins = FindSecureInstrument(id);
            if (ins == null)
                return null;

            if (user.Permission(ins) != AccessType.Denied)
            {
                UpdateInstrumentDB(ins);
                return ins;
            }

            return null;
        }
        public Instrument FindInstrumentCSIUA(User user, string CSIUAMarket, int CSIDeliveryCode)
        {
            string key = CSIUAMarket + "_" + CSIDeliveryCode;
            if (_UADB.ContainsKey(key))
                return FindInstrument(user, _UADB[key]);

            string searchString = "CSIUAMarket LIKE '" + CSIUAMarket + "' AND CSIDeliveryCode " + (CSIDeliveryCode == -1 ? " IS NULL" : " = " + (CSIDeliveryCode.ToString()));
            string targetString = null;
            DataTable table = Database.DB["Kernel"].GetDataTable(_thirdPartyDataTableName, targetString, searchString);

            DataRowCollection rows = table.Rows;
            if (rows.Count == 0)
                return null;

            var id_obj = rows[0]["ID"];
            if (id_obj is Int64)
                id_obj = (int)(object)Convert.ToInt32(id_obj);

            Instrument i = FindInstrument(user, (int)id_obj);
            if (user.Permission(i) != AccessType.Denied)
            {

                _UADB.Add(key, (int)id_obj);
                return i;
            }
            return null;
        }
        public Instrument FindInstrumentCSI(User user, int CSINumCode, int CSIDeliveryCode, Boolean onlyCache)
        {
            string key = CSINumCode + "_" + CSIDeliveryCode;
            if (_UADB.ContainsKey(key))
                return FindInstrument(user, _UADB[key]);

            if (onlyCache)
            {
                if (_CSICache.Contains(key))
                    return null;
                else
                    _CSICache.Add(key);
            }

            string searchString = "CSINumCode = '" + CSINumCode + "' AND CSIDeliveryCode " + (CSIDeliveryCode == -1 ? " IS NULL" : " = " + (CSIDeliveryCode.ToString()));
            string targetString = null;
            DataTable table = Database.DB["Kernel"].GetDataTable(_thirdPartyDataTableName, targetString, searchString);

            DataRowCollection rows = table.Rows;
            if (rows.Count == 0)
                return null;

            int id = (int)rows[0]["ID"];

            Instrument i = FindInstrument(user, id);
            if (user.Permission(i) != AccessType.Denied)
            {

                _UADB.Add(key, id);
                return i;
            }
            return null;
        }

        public void ClearCSICache()
        {
            _CSICache = new List<string>();
        }

        public List<Instrument> Instruments(User user)
        {
            string searchString = null;
            string targetString = null;
            DataTable table = Database.DB["Kernel"].GetDataTable(_mainTableName, targetString, searchString);

            DataRowCollection rows = table.Rows;
            List<Instrument> list = new List<Instrument>();
            if (rows.Count != 0)
                foreach (DataRow row in rows)
                {
                    var id_obj = row["ID"];
                    if (id_obj is Int64)
                        id_obj = (int)(object)Convert.ToInt32(id_obj);

                    Instrument i = FindInstrument(user, (int)id_obj);
                    if (i != null)
                    {
                        UpdateInstrumentDB(i);
                        list.Add(i);
                    }
                }
            if (_instrumentIdDB != null && _instrumentIdDB.Count != 0)
                foreach (Instrument ii in _instrumentIdDB.Values)
                    if (user.Permission(ii) != AccessType.Denied && !list.Contains(ii))
                        list.Add(ii);

            return list;
        }

        public List<Instrument> InstrumentsType(User user, InstrumentType type)
        {
            string searchString = string.Format("InstrumentTypeID={0}", (int)type);
            string targetString = "*";
            DataTable table = Database.DB["Kernel"].GetDataTable(_mainTableName, targetString, searchString);

            DataRowCollection rows = table.Rows;

            List<Instrument> list = new List<Instrument>();
            if (rows.Count != 0)
                foreach (DataRow row in rows)
                {
                    var id_obj = row["ID"];
                    if (id_obj is Int64)
                        id_obj = (int)(object)Convert.ToInt32(id_obj);

                    Instrument i = FindInstrument(user, (int)id_obj);
                    if (i != null)
                    {
                        UpdateInstrumentDB(i);
                        list.Add(i);
                    }
                }
            return list;
        }

        public void Remove(Instrument instrument)
        {
            if (_timeSeriesDatabase != null)
            {
                string[] keys = _timeSeriesDatabase.Keys.ToArray();
                foreach (string key in keys)
                    if (key.StartsWith(instrument.ID + "_"))
                    {
                        TimeSeries oo = null;
                        _timeSeriesDatabase.TryRemove(key, out oo);
                    }
            }

            RemoveTimeSeries(instrument);

            Instrument v = null;
            if (_instrumentIdDB.ContainsKey(instrument.ID))
                _instrumentIdDB.TryRemove(instrument.ID, out v);


            if (_instrumentNameDB.ContainsKey(instrument.Name))
                _instrumentNameDB.TryRemove(instrument.Name, out v);

            Database.DB["Kernel"].ExecuteCommand("DELETE FROM " + _systemDataTableName + " WHERE ID = " + instrument.ID);

            Database.DB["Kernel"].ExecuteCommand("DELETE FROM " + _thirdPartyDataTableName + " WHERE ID = " + instrument.ID);
            
            Database.DB["Kernel"].ExecuteCommand("DELETE FROM " + _mainTableName + " WHERE ID = " + instrument.ID);
        }


        public void RemoveFrom(Instrument instrument, DateTime date)
        {
            try
            {
                Database.DB[instrument.StrategyDB].ExecuteCommand("DELETE FROM " + _timeSeriesTableName + " WHERE ID = " + instrument.ID + " AND Timestamp >= '" + date.ToString("yyyy/MM/dd HH:mm:ss.fff") + "'");
            }
            catch (Exception e) { SystemLog.Write(e); }
        }

        public TimeSeries GetTimeSeries(Instrument instrument, TimeSeriesType tstype, DataProvider provider, Boolean LoadFromDatabase)
        {
            string key = instrument.ID + "_" + (tstype == TimeSeriesType.Close ? TimeSeriesType.Last : tstype) + "_" + provider.ID;
            string key_close = instrument.ID + "_" + TimeSeriesType.Close + "_" + provider.ID;

            if (instrument.SimulationObject || !LoadFromDatabase)
            {
                if (tstype == TimeSeriesType.Close && _timeSeriesDatabase.ContainsKey(key_close))
                    return _timeSeriesDatabase[key_close];
                else if (_timeSeriesDatabase.ContainsKey(key))
                    return _timeSeriesDatabase[key];
            }

            if (tstype == TimeSeriesType.AdjClose)
            {
                Security security = instrument as Security;
                if (security != null)
                {
                    TimeSeries tr_ts = security.GetTotalReturnTimeSeries();
                    _timeSeriesDatabase.TryAdd(key, tr_ts);

                    return _timeSeriesDatabase[key];
                }
                else
                    tstype = TimeSeriesType.Close;
            }
            else if (tstype == TimeSeriesType.AdjPriceReturn)
            {
                Security security = instrument as Security;
                if (security != null)
                {
                    TimeSeries tr_ts = security.GetPriceReturnTimeSeries();
                    _timeSeriesDatabase.TryAdd(key, tr_ts);

                    return _timeSeriesDatabase[key];
                }
                else
                    tstype = TimeSeriesType.Close;
            }

            TimeSeries res = null, res_close = null;

            DateTime lastDate = new DateTime(1950, 1, 1);

            string tableName = _timeSeriesTableName;
            string searchString = instrument.InstrumentType == InstrumentType.Strategy ? "ID = " + instrument.ID + string.Format(" AND TimeSeriesTypeID={0} AND ProviderID={1}  Order By Timestamp", (int)(tstype == TimeSeriesType.Close ? TimeSeriesType.Last : tstype), provider.ID) : "ID = " + instrument.ID + string.Format(" AND TimeSeriesTypeID={0} AND ProviderID={1} AND (CONVERT(TIME,Timestamp) = '23:59:59.990')  Order By Timestamp", (int)(tstype == TimeSeriesType.Close ? TimeSeriesType.Last : tstype), provider.ID);
            if(Database.DB[instrument.StrategyDB] is QuantApp.Kernel.Adapters.SQL.SQLiteDataSetAdapter || Database.DB[instrument.StrategyDB] is QuantApp.Kernel.Adapters.SQL.PostgresDataSetAdapter)
                searchString = instrument.InstrumentType == InstrumentType.Strategy ? "ID = " + instrument.ID + string.Format(" AND TimeSeriesTypeID={0} AND ProviderID={1}  Order By Timestamp", (int)(tstype == TimeSeriesType.Close ? TimeSeriesType.Last : tstype), provider.ID) : "ID = " + instrument.ID + string.Format(" AND TimeSeriesTypeID={0} AND ProviderID={1} AND (strftime('%H:%M:%f', Timestamp) = '23:59:59.990')  Order By Timestamp", (int)(tstype == TimeSeriesType.Close ? TimeSeriesType.Last : tstype), provider.ID);


            if (tstype == TimeSeriesType.Tick || Instrument.TimeSeriesLoadFromDatabaseIntraday)
            {
                if (tstype == TimeSeriesType.Tick)
                    tstype = TimeSeriesType.Last;
                
                if(instrument.Name.EndsWith("- Cash"))
                    searchString = instrument.InstrumentType == InstrumentType.Strategy ? "ID = " + instrument.ID + string.Format(" AND TimeSeriesTypeID={0} AND ProviderID={1}  Order By Timestamp", (int)(tstype == TimeSeriesType.Close ? TimeSeriesType.Last : tstype), provider.ID) : "ID = " + instrument.ID + string.Format(" AND TimeSeriesTypeID={0} AND ProviderID={1} Order By Timestamp", (int)(tstype == TimeSeriesType.Close ? TimeSeriesType.Last : tstype), provider.ID);
                else
                {
                    
                    if(Database.DB[instrument.StrategyDB] is QuantApp.Kernel.Adapters.SQL.SQLiteDataSetAdapter || Database.DB[instrument.StrategyDB] is QuantApp.Kernel.Adapters.SQL.PostgresDataSetAdapter)
                        searchString = instrument.InstrumentType == InstrumentType.Strategy ? "ID = " + instrument.ID + string.Format(" AND TimeSeriesTypeID={0} AND ProviderID={1}  Order By Timestamp", (int)(tstype == TimeSeriesType.Close ? TimeSeriesType.Last : tstype), provider.ID) : "ID = " + instrument.ID + string.Format(" AND TimeSeriesTypeID={0} AND ProviderID={1} AND (strftime('%H:%M:%f', Timestamp) <> '23:59:59.990') Order By Timestamp", (int)(tstype == TimeSeriesType.Close ? TimeSeriesType.Last : tstype), provider.ID);
                    else
                        searchString = instrument.InstrumentType == InstrumentType.Strategy ? "ID = " + instrument.ID + string.Format(" AND TimeSeriesTypeID={0} AND ProviderID={1}  Order By Timestamp", (int)(tstype == TimeSeriesType.Close ? TimeSeriesType.Last : tstype), provider.ID) : "ID = " + instrument.ID + string.Format(" AND TimeSeriesTypeID={0} AND ProviderID={1} AND (CONVERT(TIME,Timestamp) <> '23:59:59.990') Order By Timestamp", (int)(tstype == TimeSeriesType.Close ? TimeSeriesType.Last : tstype), provider.ID);
                }
            }
            string targetString = "Timestamp,Value";

            DataTable timeSeriesTable = Database.DB[instrument.StrategyDB].GetDataTable(tableName, targetString, searchString);

            if (tstype == TimeSeriesType.Close || tstype == TimeSeriesType.Last)
            {
                List<double> vallist = new List<double>();
                List<DateTime> dtlist = new List<DateTime>();

                List<double> vallist_close = new List<double>();
                List<DateTime> dtlist_close = new List<DateTime>();

                if (timeSeriesTable.Columns.Count > 0)
                {

                    int tsidx = timeSeriesTable.Columns["TimeStamp"].Ordinal;
                    int validx = timeSeriesTable.Columns["Value"].Ordinal;
                    DataRowCollection rs = timeSeriesTable.Rows;


                    foreach (DataRow r in rs)
                    {
                        DateTime t = (DateTime)r[tsidx];
                        double val = (double)r[validx];
                        if (t >= Calendar.Close(t))
                        {
                            dtlist_close.Add(t);
                            vallist_close.Add(val);
                        }

                        dtlist.Add(t);
                        vallist.Add(val);

                        lastDate = t;
                    }
                }

                res = new TimeSeries(vallist.Count, new DateTimeList(dtlist));

                for (int i = 0; i < vallist.Count; i++)
                    res.Data[i] = vallist[i];

                res_close = new TimeSeries(vallist_close.Count, new DateTimeList(dtlist_close));

                for (int i = 0; i < vallist_close.Count; i++)
                    res_close.Data[i] = vallist_close[i];
            }
            else
            {
                List<double> vallist = new List<double>();
                List<DateTime> dtlist = new List<DateTime>();

                if (timeSeriesTable.Columns.Count > 0)
                {

                    int tsidx = timeSeriesTable.Columns["TimeStamp"].Ordinal;
                    int validx = timeSeriesTable.Columns["Value"].Ordinal;
                    DataRowCollection rs = timeSeriesTable.Rows;


                    foreach (DataRow r in rs)
                    {
                        DateTime t = (DateTime)r[tsidx];
                        double val = (double)r[validx];

                        dtlist.Add(t);
                        vallist.Add(val);

                        lastDate = t;
                    }
                }

                res = new TimeSeries(vallist.Count, new DateTimeList(dtlist));

                for (int i = 0; i < vallist.Count; i++)
                    res.Data[i] = vallist[i];
            }

            if (instrument.TimeSeriesFilter != null)
                res = instrument.TimeSeriesFilter(instrument, res, tstype);

            if (_timeSeriesDatabase.ContainsKey(key))
                _timeSeriesDatabase[key] = res;
            else
                _timeSeriesDatabase.TryAdd(key, res);

            if (tstype == TimeSeriesType.Close || tstype == TimeSeriesType.Last)
            {
                if (instrument.TimeSeriesFilter != null)
                    res_close = instrument.TimeSeriesFilter(instrument, res_close, tstype);

                if (_timeSeriesDatabase.ContainsKey(key_close))
                    _timeSeriesDatabase[key_close] = res_close;
                else
                    _timeSeriesDatabase.TryAdd(key_close, res_close);
            }

            if ((res != null && res.Count > 0) || (res_close != null && res_close.Count > 0))
            {
                if (tstype == TimeSeriesType.Close)
                    return _timeSeriesDatabase[key_close];

                return _timeSeriesDatabase[key];
            }


            if (!(instrument.InstrumentType == InstrumentType.Strategy || instrument.InstrumentType == InstrumentType.Portfolio) && Instrument.GetTimeSeriesFunction != null)// && AQI.AQILabs.SecureWebClient.Config.DataSource == DataSourceType.Bloomberg)
            {
                Dictionary<DateTime, double> values = Instrument.GetTimeSeriesFunction(instrument, lastDate, tstype);

                if ((values == null || values.Count == 0) && (res == null || res.Count == 0))
                {
                    res = new TimeSeries(0, new DateTimeList(new DateTime[0]));

                    if (instrument.TimeSeriesFilter != null)
                        res = instrument.TimeSeriesFilter(instrument, res, tstype);

                    if (_timeSeriesDatabase.ContainsKey(key))
                        _timeSeriesDatabase[key] = res;
                    else
                        _timeSeriesDatabase.TryAdd(key, res);
                }

                else if (tstype == TimeSeriesType.Close || tstype == TimeSeriesType.Last)
                {
                    if (values != null && values.Count > 0)
                    {
                        bool ok = false;

                        foreach (DateTime t in values.Keys)
                        {
                            if (t.Date != lastDate.Date && !double.IsNaN(values[t]))
                            {

                                ok = true;

                                if (tstype == TimeSeriesType.Close || tstype == TimeSeriesType.Last)
                                {
                                    if (t == Calendar.Close(t))
                                        AddTimeSeriesPoint(instrument, t, (double)values[t], TimeSeriesType.Close, provider, true);

                                    AddTimeSeriesPoint(instrument, t, (double)values[t], TimeSeriesType.Last, provider, false);
                                }
                                else
                                    AddTimeSeriesPoint(instrument, t, (double)values[t], tstype, provider, false);
                            }
                        }

                        if (ok)
                            Save(instrument);
                    }
                }
                else
                {

                    if (values != null && values.Count > 0)
                    {
                        bool ok = false;
                        foreach (DateTime t in values.Keys)
                        {
                            if (t.Date != lastDate.Date && !double.IsNaN(values[t]))
                            {
                                ok = true;


                                AddTimeSeriesPoint(instrument, t, (double)values[t], tstype, provider, false);
                            }
                        }

                        if (ok)
                            Save(instrument);
                    }
                }
                
            }

            if (tstype == TimeSeriesType.Close)
                return _timeSeriesDatabase[key_close];

            return _timeSeriesDatabase[key];
        }

        public double GetTimeSeriesPoint(Instrument instrument, DateTime date, TimeSeriesType type, DataProvider provider, TimeSeriesRollType timeSeriesRoll, int num)
        {
            string key = instrument.ID + "_" + type + "_" + provider.ID;

            if (!_timeSeriesDatabase.ContainsKey(key))
                GetTimeSeries(instrument, type, provider, Instrument.TimeSeriesLoadFromDatabase);

            if (num == 0)
            {
                if (timeSeriesRoll == TimeSeriesRollType.Exact)
                    return _timeSeriesDatabase[key][date];
                else
                    return _timeSeriesDatabase[key][date, TimeSeries.DateSearchType.Previous];
            }
            else
            {
                int count = _timeSeriesDatabase[key].Count;

                if (count == 1)
                    return _timeSeriesDatabase[key][0];

                return _timeSeriesDatabase[key][count - num - 1];
            }
        }
        public void AddTimeSeriesPoint(Instrument instrument, DateTime date, double value, TimeSeriesType type, DataProvider provider, Boolean onlyMemory)
        {
            string key = instrument.ID + "_" + type + "_" + provider.ID;

            GetTimeSeries(instrument, type, provider, Instrument.TimeSeriesLoadFromDatabase);

            try
            {
                if (_timeSeriesDatabase.ContainsKey(key))
                {
                    if (_timeSeriesDatabase[key].ContainsDate(date))
                    {
                        _timeSeriesDatabase[key][date] = value;


                        if (_newTimeSeriesDatabase.ContainsKey(key) && _newTimeSeriesDatabase[key].ContainsKey(date))
                            _newTimeSeriesDatabase[key][date] = new TimeSeriesPoint(instrument.ID, type, date, value, provider.ID);
                    }
                    else
                    {
                        if (!_newTimeSeriesDatabase.ContainsKey(key))
                            _newTimeSeriesDatabase.TryAdd(key, new ConcurrentDictionary<DateTime, TimeSeriesPoint>());

                        if (!_newTimeSeriesDatabase[key].ContainsKey(date))
                            _newTimeSeriesDatabase[key].TryAdd(date, new TimeSeriesPoint(instrument.ID, type, date, value, provider.ID));
                        else
                            _newTimeSeriesDatabase[key][date] = new TimeSeriesPoint(instrument.ID, type, date, value, provider.ID);
                    }
                }
            }
            catch (Exception e) { Console.WriteLine(e); }

            try
            {
                if (_timeSeriesDatabase.ContainsKey(key) && !_timeSeriesDatabase[key].ContainsDate(date))
                    _timeSeriesDatabase[key].AddDataPoint(date, value);
            }
            catch (Exception e)
            {
                SystemLog.Write(e);
            }

            if (!onlyMemory && !instrument.SimulationObject && Instrument.TimeSeriesLoadFromDatabase)
                Save(instrument);

        }

        public readonly static object removeTimeSeriesLock = new object();
        public void RemoveTimeSeries(Instrument instrument, TimeSeriesType tstype, DataProvider provider)
        {
            lock (removeTimeSeriesLock)
            {
                Database.DB[instrument.StrategyDB].ExecuteCommand("DELETE FROM " + _timeSeriesTableName + " WHERE ID = " + instrument.ID + string.Format(" AND TimeSeriesTypeID={0} AND ProviderID={1}", (int)tstype, provider.ID));
            }
        }

        public void RemoveTimeSeries(Instrument instrument)
        {
            lock (removeTimeSeriesLock)
            {
                Database.DB[instrument.StrategyDB].ExecuteCommand("DELETE FROM " + _timeSeriesTableName + " WHERE ID = " + instrument.ID);
            }
        }



        public static DateTime RoundToSqlDateTime(DateTime date)
        {
            return new SqlDateTime(date).Value;
        }

        public void Save(Instrument instrument)
        {
            ConcurrentDictionary<string, DataTable> _timeSeriesTables = new ConcurrentDictionary<string, DataTable>();
            ConcurrentDictionary<string, ConcurrentDictionary<DateTime, TimeSeriesPoint>> ls = _newTimeSeriesDatabase;
            Dictionary<string, DateTime> maxDates = new Dictionary<string, DateTime>();
            Dictionary<string, Dictionary<DateTime, DataRow>> dates = new Dictionary<string, Dictionary<DateTime, DataRow>>();
            Dictionary<string, string> check = new Dictionary<string, string>();
            if (ls.Count != 0)
            {
                foreach (ConcurrentDictionary<DateTime, TimeSeriesPoint> pts in ls.Values.ToList())
                    foreach (TimeSeriesPoint p in pts.Values.ToList())
                    {
                        if (p.ID == instrument.ID && !double.IsNaN(p.value))
                        {
                            string key = p.ID + "_" + p.type + "_" + p.ProviderID;

                            if (!_timeSeriesTables.ContainsKey(key))
                            {
                                string tableName = _timeSeriesTableName;
                                string searchString = "ID = -10000 " + string.Format(" AND TimeSeriesTypeID={0} AND ProviderID={1}", (int)p.type, p.ProviderID);
                                string targetString = null;
                                DataTable timeSeriesTable = Database.DB[instrument.StrategyDB].GetDataTable(tableName, targetString, searchString);
                                _timeSeriesTables.TryAdd(key, timeSeriesTable);

                                if (!dates.ContainsKey(key))
                                    dates.Add(key, new Dictionary<DateTime, DataRow>());

                                DataRowCollection rows = timeSeriesTable.Rows;
                                DateTime maxDate = DateTime.MinValue;
                                foreach (DataRow row in rows)
                                {
                                    if (!dates[key].ContainsKey((DateTime)row["timestamp"]))
                                    {
                                        DateTime d = (DateTime)row["timestamp"];
                                        maxDate = d > maxDate ? d : maxDate;
                                        dates[key].Add((DateTime)row["timestamp"], row);
                                    }
                                }

                                if (maxDates.ContainsKey(key))
                                    maxDates[key] = maxDate;
                                else
                                    maxDates.Add(key, maxDate);
                            }


                            DataTable table = _timeSeriesTables[key];

                            Boolean newRow = true;

                            {
                                if (maxDates.ContainsKey(key))
                                {
                                    if (p.date <= maxDates[key])
                                    {
                                        if (dates[key].ContainsKey(p.date))
                                        {
                                            dates[key][p.date]["Value"] = p.value;
                                            newRow = false;
                                        }

                                    }
                                }

                                if (newRow)
                                {

                                    string t = RoundToSqlDateTime((p.date == DateTime.MinValue ? new DateTime(1900, 01, 01) : p.date)).ToString("yyyy-MM-dd HH:mm:ss.fff");
                                    string idk = p.ID + "_" + p.type + "_" + t + "_" + p.value + "_" + p.ProviderID;
                                    
                                    if (!check.ContainsKey(idk))
                                    {
                                        DataRow r = table.NewRow();
                                        r["ID"] = p.ID;
                                        r["TimeSeriesTypeID"] = (int)p.type;
                                        r["Timestamp"] = t;
                                        r["Value"] = p.value;
                                        r["ProviderID"] = p.ProviderID;
                                        table.Rows.Add(r);
                                        check.Add(idk, idk);
                                    }
                                }
                            }
                        }
                    }


                if (_newTimeSeriesDatabase != null)
                {
                    string[] keys = _newTimeSeriesDatabase.Keys.ToArray();
                    foreach (string key in keys)
                        if (key.StartsWith(instrument.ID + "_"))
                        {
                            ConcurrentDictionary<DateTime, TimeSeriesPoint> v = null;
                            _newTimeSeriesDatabase.TryRemove(key, out v);
                        }
                }

            }

            foreach (string key in _timeSeriesTables.Keys.ToList())
                if (key.StartsWith(instrument.ID + "_"))
                    Database.DB[instrument.StrategyDB].AddDataTable(_timeSeriesTables[key]);
            
        }

        public void CleanMemory(Instrument instrument)
        {
            if (_instrumentIdDB.ContainsKey(instrument.ID))
            {
                _systemTables[instrument.ID] = null;
                _systemTables.Remove(instrument.ID);

                _thirdPartyTables[instrument.ID] = null;
                _thirdPartyTables.Remove(instrument.ID);

                Instrument v = null;
                _instrumentIdDB[instrument.ID] = null;
                _instrumentIdDB.TryRemove(instrument.ID, out v);
            }

            if (_instrumentNameDB.ContainsKey(instrument.Name))
            {
                Instrument v = null;
                _instrumentNameDB[instrument.Name] = null;
                _instrumentNameDB.TryRemove(instrument.Name, out v);
            }

            CleanTimeSeriesFromMemory(instrument);
        }

        public void CleanTimeSeriesFromMemory(Instrument instrument)
        {
            string[] keysdb = _timeSeriesDatabase.Keys.ToArray();
            foreach (string key in keysdb)
                if (key.StartsWith(instrument.ID + "_"))
                {
                    TimeSeries oo = null;
                    _timeSeriesDatabase.TryRemove(key, out oo);
                }

        }
    }
}
