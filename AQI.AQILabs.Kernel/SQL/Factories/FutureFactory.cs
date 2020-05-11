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

using System.Data;
using AQI.AQILabs.Kernel.Factories;

using QuantApp.Kernel;

namespace AQI.AQILabs.Kernel.Adapters.SQL.Factories
{
    public class SQLFutureFactory : IFutureFactory
    {
        private static string _futureTableName = "Future";

        private Dictionary<int, Future> _futureIdDB = new Dictionary<int, Future>();
        private Dictionary<DateTime, Dictionary<int, Future>> _currentFutureDB = new Dictionary<DateTime, Dictionary<int, Future>>();

        private Dictionary<int, DataTable> _mainTables = new Dictionary<int, DataTable>();

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


        public readonly static object createObjLock = new object();
        public Future CreateFuture(Security security, string generic_months, DateTime first_delivery, DateTime first_notice, DateTime last_delivery, DateTime first_trade, DateTime last_trade, double tick_size, double contract_size, Instrument underlying)
        {
            lock (createObjLock)
            {
                if (security.InstrumentType != InstrumentType.Future)
                    throw new Exception("Instrument is not a Future");

                string searchString = "ID = " + security.ID;
                string targetString = null;
                DataTable table = Database.DB["Kernel"].GetDataTable(_futureTableName, targetString, searchString);
                DataRowCollection rows = table.Rows;

                if (rows.Count == 0)
                {
                    DataRow r = table.NewRow();
                    r["ID"] = security.ID;
                    r["FutureGenericMonths"] = generic_months;
                    r["FirstDeliveryDate"] = first_delivery;
                    r["FirstNoticeDate"] = first_notice;
                    r["LastDeliveryDate"] = last_delivery;
                    r["FirstTradeDate"] = first_trade;
                    r["LastTradeDate"] = last_trade;
                    r["TickSize"] = tick_size;
                    r["ContractSize"] = contract_size;
                    r["UnderlyingInstrumentID"] = underlying.ID;
                    r["PointSize"] = 0;

                    try
                    {
                        string[] isub = security.Name.Trim().Split(new char[] { ' ' });
                        switch (isub[1])
                        {
                            case "F":
                                r["ContractMonth"] = 1;
                                break;

                            case "G":
                                r["ContractMonth"] = 2;
                                break;

                            case "H":
                                r["ContractMonth"] = 3;
                                break;

                            case "J":
                                r["ContractMonth"] = 4;
                                break;

                            case "K":
                                r["ContractMonth"] = 5;
                                break;

                            case "M":
                                r["ContractMonth"] = 6;
                                break;

                            case "N":
                                r["ContractMonth"] = 7;
                                break;

                            case "Q":
                                r["ContractMonth"] = 8;
                                break;

                            case "U":
                                r["ContractMonth"] = 9;
                                break;

                            case "V":
                                r["ContractMonth"] = 10;
                                break;

                            case "X":
                                r["ContractMonth"] = 11;
                                break;

                            case "Z":
                                r["ContractMonth"] = 12;
                                break;
                        }

                        int year = Int16.Parse(isub[2]);
                        r["ContractYear"] = year;
                    }
                    catch
                    {
                        r["ContractMonth"] = 0;
                        r["ContractYear"] = 2500;
                    }


                    rows.Add(r);


                    Database.DB["Kernel"].UpdateDataTable(table);

                    Future f = FindFuture(security);

                    f.ExecutionCost = underlying.ExecutionCost;
                    f.SetConstantCarryCost(underlying.CarryCostLong, underlying.CarryCostLong, underlying.CarryCostDayCountConvention, underlying.CarryCostDayCountBase);


                    if (!_futureIdDB.ContainsKey(f.ID))
                        _futureIdDB.Add(f.ID, f);

                    return f;
                }
                throw new Exception("Future Already Exists");
            }
        }

        public readonly static object findObjLock = new object();
        public Future FindFuture(Security security)
        {
            lock (findObjLock)
            {
                if (_futureIdDB.ContainsKey(security.ID))
                    return _futureIdDB[security.ID];

                if (security.InstrumentType != InstrumentType.Future)
                    throw new Exception("Instrument is not a Future");

                string tableName = _futureTableName;
                string searchString = "ID = " + security.ID;
                string targetString = null;
                DataTable table = Database.DB["Kernel"].GetDataTable(tableName, targetString, searchString);

                DataRowCollection rows = table.Rows;
                if (rows.Count == 0)
                    return null;

                DataRow r = rows[0];

                string _futureGenericMonths = GetValue<string>(r, "FutureGenericMonths");
                DateTime _firstDeliveryDate = GetValue<DateTime>(r, "FirstDeliveryDate");
                DateTime _firstNoticeDate = GetValue<DateTime>(r, "FirstNoticeDate");
                DateTime _lastDeliveryDate = GetValue<DateTime>(r, "LastDeliveryDate");
                DateTime _firstTradeDate = GetValue<DateTime>(r, "FirstTradeDate");
                DateTime _lastTradeDate = GetValue<DateTime>(r, "LastTradeDate");
                double _tickSize = GetValue<double>(r, "TickSize");
                double _contractSize = GetValue<double>(r, "ContractSize");
                //double _pointSize = GetValue<double>(r, "PointSize");
                int _contractMonth = GetValue<int>(r, "ContractMonth");
                int _contractYear = GetValue<int>(r, "ContractYear");
                int underlyingID = GetValue<int>(r, "UnderlyingInstrumentID");
                int nextID = NextID(underlyingID, _lastTradeDate);
                int previousID = PreviousID(underlyingID, _lastTradeDate);


                Future f = new Future(security, _futureGenericMonths, _firstDeliveryDate, _firstNoticeDate, _lastDeliveryDate, _firstTradeDate, _lastTradeDate, _tickSize, _contractSize, _contractMonth, _contractYear, underlyingID, nextID, previousID);
                _futureIdDB.Add(f.ID, f);
                if (_mainTables.ContainsKey(f.ID))
                    _mainTables[f.ID] = table;
                else
                    _mainTables.Add(f.ID, table);
                return f;
            }
        }
        public Future FindFuture(int id)
        {
            lock (findObjLock)
            {
                if (_futureIdDB.ContainsKey(id))
                    return _futureIdDB[id];

                Instrument instrument = Instrument.FindInstrument(id);
                if (instrument == null)
                    return null;

                Security security = Security.FindSecurity(instrument);
                if (security == null)
                    return null;

                return FindFuture(security);
            }
        }

        public readonly static object currentObjLock = new object();
        public Future CurrentFuture(Instrument underlyingInstrument, DateTime date)
        {
            lock (currentObjLock)
            {

                if (_currentFutureDB.ContainsKey(date) && _currentFutureDB[date].ContainsKey(underlyingInstrument.ID))
                    return _currentFutureDB[date][underlyingInstrument.ID];


                string searchString = string.Format("UnderlyingInstrumentID={0} AND LastTradeDate>'{1:yyyy-MM-dd HH:mm:ss}' AND FirstNoticeDate>'{1:yyyy-MM-dd HH:mm:ss}' ORDER BY LastTradeDate", underlyingInstrument.ID, date);
                string targetString = "*";
                DataTable table = Database.DB["Kernel"].GetDataTable(_futureTableName, targetString, searchString);
                DataRowCollection rows = table.Rows;
                if (rows.Count == 0)
                    return null;
                else
                {
                    Future res = null;
                    Future prev = null;
                    foreach (DataRow r in rows)
                    {
                        int id = GetValue<int>(r, "ID");
                        Instrument instrument = Instrument.FindInstrument(id);
                        Security security = Security.FindSecurity(instrument);
                        Future future = FindFuture(security);

                        if (prev != null)
                        {
                            prev.NextFuture = future;
                            future.PreviousFuture = prev;
                        }

                        if (res == null)
                            res = future;
                    }

                    Future current = res;

                    DateTime lastDt = DateTime.Now;
                    for (DateTime dt = date; dt < lastDt; dt = dt.AddDays(1))
                    {
                        if (!_currentFutureDB.ContainsKey(dt))
                            _currentFutureDB.Add(dt, new Dictionary<int, Future>());

                        if ((dt == current.LastTradeDate || dt == current.FirstNoticeDate) && current.NextFuture != null)
                            current = current.NextFuture;

                        if (!_currentFutureDB[dt].ContainsKey(current.UnderlyingID))
                            _currentFutureDB[dt].Add(current.UnderlyingID, current);
                    }

                    return res;
                }
            }
        }
        public Future CurrentFuture(Instrument underlyingInstrument, double contract_size, DateTime date)
        {
            lock (currentObjLock)
            {

                if (_currentFutureDB.ContainsKey(date) && _currentFutureDB[date].ContainsKey(underlyingInstrument.ID))
                    return _currentFutureDB[date][underlyingInstrument.ID];


                string searchString = string.Format("UnderlyingInstrumentID={0} AND LastTradeDate>'{1:yyyy-MM-dd HH:mm:ss}' AND FirstNoticeDate>'{1:yyyy-MM-dd HH:mm:ss} AND ContractSize={2}' ORDER BY LastTradeDate", underlyingInstrument.ID, date, contract_size);
                string targetString = "*";
                DataTable table = Database.DB["Kernel"].GetDataTable(_futureTableName, targetString, searchString);
                DataRowCollection rows = table.Rows;
                if (rows.Count == 0)
                    return null;
                else
                {
                    Future res = null;
                    Future prev = null;
                    foreach (DataRow r in rows)
                    {
                        int id = GetValue<int>(r, "ID");
                        Instrument instrument = Instrument.FindInstrument(id);
                        Security security = Security.FindSecurity(instrument);
                        Future future = FindFuture(security);

                        if (prev != null)
                        {
                            prev.NextFuture = future;
                            future.PreviousFuture = prev;
                        }

                        if (res == null)
                            res = future;
                    }

                    Future current = res;

                    DateTime lastDt = DateTime.Now;
                    for (DateTime dt = date; dt < lastDt; dt = dt.AddDays(1))
                    {
                        if (!_currentFutureDB.ContainsKey(dt))
                            _currentFutureDB.Add(dt, new Dictionary<int, Future>());

                        if ((dt == current.LastTradeDate || dt == current.FirstNoticeDate) && current.NextFuture != null)
                            current = current.NextFuture;

                        if (!_currentFutureDB[dt].ContainsKey(current.UnderlyingID))
                            _currentFutureDB[dt].Add(current.UnderlyingID, current);
                    }

                    return res;
                }
            }
        }

        public readonly static object hasObjLock = new object();
        public Boolean HasFutures(Instrument underlyingInstrument)
        {
            lock (hasObjLock)
            {
                string searchString = string.Format("UnderlyingInstrumentID={0}", underlyingInstrument.ID);
                string targetString = "TOP 1 *";

                if(Database.DB["Kernel"] is QuantApp.Kernel.Adapters.SQL.SQLiteDataSetAdapter || Database.DB["Kernel"] is QuantApp.Kernel.Adapters.SQL.PostgresDataSetAdapter)
                {
                    searchString += " LIMIT 1";
                    targetString = "*";
                }

                DataTable table = Database.DB["Kernel"].GetDataTable(_futureTableName, targetString, searchString);
                DataRowCollection rows = table.Rows;
                if (rows.Count == 0)
                    return false;
                else
                    return true;
            }
        }

        public readonly static object nextObjLock = new object();
        public int NextID(int id, DateTime lastTradeDate)
        {
            lock (nextObjLock)
            {
                string searchString = string.Format("UnderlyingInstrumentID={0} AND LastTradeDate>'{1:yyyy-MM-dd HH:mm:ss}' ORDER BY LastTradeDate", id, lastTradeDate);
                string targetString = "TOP 1 *";

                if(Database.DB["Kernel"] is QuantApp.Kernel.Adapters.SQL.SQLiteDataSetAdapter || Database.DB["Kernel"] is QuantApp.Kernel.Adapters.SQL.PostgresDataSetAdapter)
                {
                    searchString += " LIMIT 1";
                    targetString = "*";
                }

                DataTable table = Database.DB["Kernel"].GetDataTable(_futureTableName, targetString, searchString);
                DataRowCollection rows = table.Rows;
                if (rows.Count == 0)
                    return -1;
                else
                {
                    int nextID = GetValue<int>(rows[0], "ID");
                    return nextID;
                }
            }
        }

        public readonly static object prevObjLock = new object();
        public int PreviousID(int id, DateTime lastTradeDate)
        {
            lock (prevObjLock)
            {
                string searchString = string.Format("UnderlyingInstrumentID={0} AND LastTradeDate<'{1:yyyy-MM-dd HH:mm:ss}' ORDER BY LastTradeDate DESC", id, lastTradeDate);
                string targetString = "TOP 1 *";

                if(Database.DB["Kernel"] is QuantApp.Kernel.Adapters.SQL.SQLiteDataSetAdapter || Database.DB["Kernel"] is QuantApp.Kernel.Adapters.SQL.PostgresDataSetAdapter)
                {
                    searchString += " LIMIT 1";
                    targetString = "*";
                }
                
                DataTable table = Database.DB["Kernel"].GetDataTable(_futureTableName, targetString, searchString);
                DataRowCollection rows = table.Rows;
                if (rows.Count == 0)
                    return -1;
                else
                {
                    int previousID = GetValue<int>(rows[0], "ID");
                    return previousID;
                }
            }
        }

        public readonly static object undObjLock = new object();
        public List<Instrument> Underlyings()
        {

            lock (undObjLock)
            {
                string searchString = null;
                string targetString = "DISTINCT UnderlyingInstrumentID";
                DataTable table = Database.DB["Kernel"].GetDataTable(_futureTableName, targetString, searchString);
                DataRowCollection rows = table.Rows;
                if (rows.Count == 0)
                    return null;
                else
                {
                    List<Instrument> res = new List<Instrument>();
                    foreach (DataRow r in rows)
                    {
                        int id = GetValue<int>(r, "UnderlyingInstrumentID");
                        Instrument instrument = Instrument.FindInstrument(id);
                        res.Add(instrument);
                    }

                    return res;
                }
            }
        }

        public readonly static object actObjLock = new object();
        public List<Future> ActiveFutures(Instrument underlyingInstrument, DateTime date)
        {

            lock (actObjLock)
            {
                string searchString = underlyingInstrument != null ? string.Format("LastTradeDate>'{0:yyyy-MM-dd HH:mm:ss}' AND FirstNoticeDate>'{0:yyyy-MM-dd HH:mm:ss}' AND UnderlyingInstrumentID={1} ORDER BY LastTradeDate", date, underlyingInstrument.ID) : string.Format("LastTradeDate>'{0:yyyy-MM-dd HH:mm:ss}' AND FirstNoticeDate>'{0:yyyy-MM-dd HH:mm:ss}' ORDER BY LastTradeDate", date);
                string targetString = "*";
                DataTable table = Database.DB["Kernel"].GetDataTable(_futureTableName, targetString, searchString);
                DataRowCollection rows = table.Rows;
                if (rows.Count == 0)
                    return null;
                else
                {
                    List<Future> res = new List<Future>();
                    foreach (DataRow r in rows)
                    {
                        int id = GetValue<int>(r, "ID");
                        Instrument instrument = Instrument.FindInstrument(id);
                        Security security = Security.FindSecurity(instrument);
                        Future future = FindFuture(security);

                        res.Add(future);
                    }

                    return res;
                }
            }
        }

        public List<Future> ActiveFutures(Instrument underlyingInstrument, double contract_size, DateTime date)
        {

            lock (actObjLock)
            {
                string searchString = underlyingInstrument != null ? string.Format("LastTradeDate>'{0:yyyy-MM-dd HH:mm:ss}' AND FirstNoticeDate>'{0:yyyy-MM-dd HH:mm:ss}' AND UnderlyingInstrumentID={1} AND ContractSize={2} ORDER BY LastTradeDate", date, underlyingInstrument.ID, contract_size) : string.Format("LastTradeDate>'{0:yyyy-MM-dd HH:mm:ss}' AND FirstNoticeDate>'{0:yyyy-MM-dd HH:mm:ss}' ORDER BY LastTradeDate", date);
                string targetString = "*";
                DataTable table = Database.DB["Kernel"].GetDataTable(_futureTableName, targetString, searchString);
                DataRowCollection rows = table.Rows;
                if (rows.Count == 0)
                    return null;
                else
                {
                    List<Future> res = new List<Future>();
                    foreach (DataRow r in rows)
                    {
                        int id = GetValue<int>(r, "ID");
                        Instrument instrument = Instrument.FindInstrument(id);
                        Security security = Security.FindSecurity(instrument);
                        Future future = FindFuture(security);

                        res.Add(future);
                    }

                    return res;
                }
            }
        }

        public readonly static object cleanObjLock = new object();
        public void CleanFuturesFromMemory(DateTime date)
        {
            lock (cleanObjLock)
            {
                List<Future> remove = new List<Future>();
                foreach (Future f in _futureIdDB.Values)
                {
                    if (f.LastTradeDate < date)
                        remove.Add(f);
                }

                DateTime[] dts = _currentFutureDB.Keys.ToArray();

                foreach (DateTime dt in dts)
                {
                    if (dt <= date)
                    {
                        Future[] cfs = _currentFutureDB[dt].Values.ToArray();
                        foreach (Future cf in cfs)
                        {
                            Instrument.CleanMemory(cf);
                            Security.CleanMemory(cf);

                            cf.NextFuture.PreviousFuture = null;

                            Instrument und = cf.Underlying;
                            if (_currentFutureDB[dt].ContainsKey(und.ID))
                            {
                                _mainTables[_currentFutureDB[dt][und.ID].ID] = null;
                                _currentFutureDB[dt][und.ID] = null;
                                _currentFutureDB[dt].Remove(und.ID);
                            }
                        }

                        _currentFutureDB[dt] = null;
                        _currentFutureDB.Remove(dt);
                    }
                }

                foreach (Future f in remove)
                {
                    Instrument.CleanMemory(f);
                    Security.CleanMemory(f);

                    if (_futureIdDB.ContainsKey(f.ID))
                    {
                        _mainTables[f.ID] = null;
                        _futureIdDB[f.ID] = null;
                        _futureIdDB.Remove(f.ID);
                    }
                }
            }
        }

        public readonly static object setObjLock = new object();
        public void SetProperty(Future future, string name, object value)
        {
            lock (setObjLock)
            {
                DataTable table = _mainTables[future.ID];

                if (table != null)
                {
                    DataRowCollection rows = table.Rows;
                    if (rows.Count == 0)
                        return;

                    DataRow row = rows[0];
                    row[name] = value;
                    Database.DB["Kernel"].UpdateDataTable(table);
                }
            }
        }

        public void Remove(Future future)
        {
            _futureIdDB.Remove(future.ID);

            DataTable table = _mainTables[future.ID];

            if (table != null)
            {
                DataRowCollection rows = table.Rows;
                if (rows.Count == 0)
                    return;

                DataRow row = rows[0];
                row.Delete();
                Database.DB["Kernel"].UpdateDataTable(table);
            }
        }
    }
}
