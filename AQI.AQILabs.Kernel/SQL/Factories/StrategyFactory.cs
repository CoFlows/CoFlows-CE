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
using System.Collections.Concurrent;

using AQI.AQILabs.Kernel.Factories;
using AQI.AQILabs.Kernel.Numerics.Util;

using QuantApp.Kernel;

namespace AQI.AQILabs.Kernel.Adapters.SQL.Factories
{
    public class SQLStrategyFactory : IStrategyFactory
    {
        public readonly static object objLock = new object();

        private static string _StrategyTableName = "Strategy";
        private static string _StrategyMemoryTableName = "StrategyMemory";

        private ConcurrentDictionary<string, TimeSeries> _memorySeriesDatabase = new ConcurrentDictionary<string, TimeSeries>();
        private ConcurrentDictionary<string, ConcurrentDictionary<DateTime, MemorySeriesPoint>> _newMemorySeriesDatabase = new ConcurrentDictionary<string, ConcurrentDictionary<DateTime, MemorySeriesPoint>>();

        private ConcurrentDictionary<int, Strategy> _strategyIdDB = new ConcurrentDictionary<int, Strategy>();
        private ConcurrentDictionary<int, DataTable> _mainTables = new ConcurrentDictionary<int, DataTable>();
        private ConcurrentDictionary<string, DataTable> _memorySeriesTables = new ConcurrentDictionary<string, DataTable>();

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

        public readonly static object findStrategyLock = new object();
        public Strategy FindStrategy(Instrument instrument)
        {
            lock (findStrategyLock)
            {
                if (instrument == null)
                    return null;

                if (_strategyIdDB.ContainsKey(instrument.ID) && (instrument as Strategy != null))
                    return _strategyIdDB[instrument.ID];

                if (instrument.InstrumentType == InstrumentType.Strategy)
                {
                    string tableName = _StrategyTableName;
                    string searchString = "ID = " + instrument.ID;
                    string targetString = null;
                    DataTable table = Database.DB["Kernel"].GetDataTable(tableName, targetString, searchString);

                    Strategy strat = null;
                    if (table.Rows.Count != 0)
                    {
                        DataRow _StrategyRow = table.Rows[0];

                        string classname = GetValue<string>(_StrategyRow, "Class");
                     
                        int portfolioID = GetValue<int>(_StrategyRow, "PortfolioID");
                        DateTime initialDate = GetValue<DateTime>(_StrategyRow, "InitialDate");
                     
                        string dbConnection = GetValue<string>(_StrategyRow, "DBConnection");
                        string scheduler = GetValue<string>(_StrategyRow, "Scheduler");

                        strat = Strategy.LoadStrategy(instrument, classname, portfolioID, initialDate, dbConnection, scheduler);
                    }
                    
                    if (strat == null)
                        return null;

                    UpdateStrategyDB(strat);

                    if (_mainTables.ContainsKey(instrument.ID))
                        _mainTables[instrument.ID] = table;
                    else
                        _mainTables.TryAdd(instrument.ID, table);

                    return strat;
                }

                throw new Exception("Instrument is not a Strategy");
            }
        }

        public Portfolio FindPortfolio(int id)
        {
            return (Portfolio)Instrument.Factory.FindSecureInstrument(id);
        }

        public readonly static object updateStrategyLock = new object();
        public void UpdateStrategyDB(Strategy strategy)
        {
            if (!_strategyIdDB.ContainsKey(strategy.ID))
                _strategyIdDB.TryAdd(strategy.ID, strategy);
            else
                _strategyIdDB[strategy.ID] = strategy;

            Instrument.Factory.UpdateInstrumentDB(strategy);
        }
        public TimeSeries GetMemorySeries(Strategy strategy, int memorytype, int memoryclass)
        {
            if (!_loadedStrategyTimeSeries.ContainsKey(strategy.ID))
            {
                IEnumerable<int[]> ids = GetMemorySeriesIds(strategy);
                if (ids.Count() <= 100)
                    GetMemorySeries(strategy);
            }

            return GetMemorySeries_internal(strategy, memorytype, memoryclass);

        }

        public TimeSeries GetMemorySeries_internal(Strategy strategy, int memorytype, int memoryclass)
        {
            string key = strategy.ID + "_" + memorytype + "_" + memoryclass;

            if (!Instrument.TimeSeriesLoadFromDatabase)
                if (_memorySeriesDatabase.ContainsKey(key))
                    return _memorySeriesDatabase[key];

            TimeSeries res;

            if (!_strategyMemoryIdsDB[strategy.ID].ContainsKey(memorytype + "_" + memoryclass) && (strategy.SimulationObject || strategy.Simulating))
            {
                res = new TimeSeries(0, new DateTimeList(new DateTime[0]));

                if (_memorySeriesDatabase.ContainsKey(key))
                    _memorySeriesDatabase[key] = res;
                else
                    _memorySeriesDatabase.TryAdd(key, res);

                return res;
            }




            if (!_strategyMemoryIdsDB[strategy.ID].ContainsKey(memorytype + "_" + memoryclass))
            {
                _strategyMemoryIdsDB[strategy.ID].TryAdd(memorytype + "_" + memoryclass, new int[] { memorytype, memoryclass });

                res = new TimeSeries(0);

                if (_memorySeriesDatabase.ContainsKey(key))
                    _memorySeriesDatabase[key] = res;
                else
                    _memorySeriesDatabase.TryAdd(key, res);
            }

            string tableName = _StrategyMemoryTableName;
            string searchString = "ID = " + strategy.ID + string.Format(" AND MemoryTypeID={0} AND MemoryClassID={1}", memorytype, memoryclass);
            string targetString = "TimeStamp, Value";
            DataTable memorySeriesTable = Database.DB[strategy.StrategyDB].GetDataTable(tableName, targetString, searchString);

            List<double> vallist = new List<double>();
            List<DateTime> dtlist = new List<DateTime>();

            if (memorySeriesTable != null && memorySeriesTable.Columns.Count > 0)
            {
                int tsidx = memorySeriesTable.Columns["TimeStamp"].Ordinal;
                int validx = memorySeriesTable.Columns["Value"].Ordinal;
                DataRowCollection rs = memorySeriesTable.Rows;

                foreach (DataRow r in rs)
                {
                    DateTime t = (DateTime)r[tsidx];
                    double val = (double)r[validx];
                    dtlist.Add(t);
                    vallist.Add(val);

                }
            }
            res = new TimeSeries(vallist.Count, new DateTimeList(dtlist));

            for (int i = 0; i < vallist.Count; i++)
                res.Data[i] = vallist[i];

            if (_memorySeriesDatabase.ContainsKey(key))
                _memorySeriesDatabase[key] = res;
            else
                _memorySeriesDatabase.TryAdd(key, res);

            return _memorySeriesDatabase[key];
        }

        public void AddMemorySeries(Strategy strategy, int memorytype, int memoryclass, TimeSeries timeseries)
        {
            string key = strategy.ID + "_" + memorytype + "_" + memoryclass;

            foreach (DateTime date in timeseries.DateTimes)
                if (!double.IsNaN(timeseries[date]))
                    AddMemoryPoint(strategy, date, timeseries[date], memorytype, memoryclass, true);
        }
        private ConcurrentDictionary<int, string> _loadedStrategyTimeSeries = new ConcurrentDictionary<int, string>();

        private ConcurrentDictionary<int, ConcurrentDictionary<string, int[]>> _strategyMemoryIdsDB = new ConcurrentDictionary<int, ConcurrentDictionary<string, int[]>>();
        public IEnumerable<int[]> GetMemorySeriesIds(Strategy strategy)
        {
            if (_strategyMemoryIdsDB.ContainsKey(strategy.ID))
                return _strategyMemoryIdsDB[strategy.ID].Values.AsEnumerable();
            else
                _strategyMemoryIdsDB.TryAdd(strategy.ID, new ConcurrentDictionary<string, int[]>());

            ConcurrentDictionary<string, int[]> res = new ConcurrentDictionary<string, int[]>();

            if (!strategy.Simulating && !strategy.SimulationObject && !_loadedStrategyTimeSeries.ContainsKey(strategy.ID))// && _memorySeriesDatabase.Count == 0)
            {
                string tableName = _StrategyMemoryTableName;
                string searchString = "ID = " + strategy.ID;
                string targetString = "DISTINCT MemoryTypeID, MemoryClassID";
                DataTable memorySeriesTable = Database.DB[strategy.StrategyDB].GetDataTable(tableName, targetString, searchString);

                DataRowCollection rs = memorySeriesTable.Rows;

                if (rs.Count == 0)
                {

                    if (!_loadedStrategyTimeSeries.ContainsKey(strategy.ID))
                        _loadedStrategyTimeSeries.TryAdd(strategy.ID, "");


                    return res.Values.AsEnumerable();
                }

                int mtidx = memorySeriesTable.Columns["MemoryTypeID"].Ordinal;
                int mcidx = memorySeriesTable.Columns["MemoryClassID"].Ordinal;

                List<int[]> tmpRes = new List<int[]>();

                foreach (DataRow r in rs)
                {
                    var memorytype_obj = r[mtidx];
                    var memoryclass_obj = r[mcidx];


                    int memorytype;
                    int memoryclass;

                    if (memorytype_obj is Int64)
                        memorytype = (int)(object)Convert.ToInt32(memorytype_obj);
                    else
                        memorytype = (int)memorytype_obj;

                    if (memoryclass_obj is Int64)
                        memoryclass = (int)(object)Convert.ToInt32(memoryclass_obj);
                    else
                        memoryclass = (int)memoryclass_obj;

                    string key = strategy.ID + "_" + memorytype + "_" + memoryclass;
                    res.TryAdd(memorytype + "_" + memoryclass, new int[] { memorytype, memoryclass });
                }

                _strategyMemoryIdsDB[strategy.ID] = res;
            }

            if (!_loadedStrategyTimeSeries.ContainsKey(strategy.ID))
                _loadedStrategyTimeSeries.TryAdd(strategy.ID, "");



            return res.Values.AsEnumerable();
        }


        public Dictionary<int[], TimeSeries> GetMemorySeries(Strategy strategy)
        {
            Dictionary<int[], TimeSeries> res = new Dictionary<int[], TimeSeries>();
            foreach (string key in _memorySeriesDatabase.Keys)
            {
                if (key.StartsWith(strategy.ID + "_"))
                {
                    string[] k = key.Split('_');
                    int memorytype = int.Parse(k[1]);
                    int memoryclass = int.Parse(k[2]);
                    if (memoryclass != Strategy._aum_id_do_not_use && memorytype != Strategy._aum_id_do_not_use && memoryclass != Strategy._aum_chg_id_do_not_use && memorytype != Strategy._aum_chg_id_do_not_use && memoryclass != Strategy._aum_ord_chg_id_do_not_use && memorytype != Strategy._aum_ord_chg_id_do_not_use)
                        res.Add(new int[] { memorytype, memoryclass }, _memorySeriesDatabase[key]);
                }
            }

            if (!strategy.Simulating && !strategy.SimulationObject && !_loadedStrategyTimeSeries.ContainsKey(strategy.ID))// && _memorySeriesDatabase.Count == 0)
            {
                string tableName = _StrategyMemoryTableName;
                string searchString = "ID = " + strategy.ID;
                string targetString = "TimeStamp, Value, MemoryTypeID, MemoryClassID";// "DISTINCT MemoryTypeID, MemoryClassID";
                DataTable memorySeriesTable = Database.DB[strategy.StrategyDB].GetDataTable(tableName, targetString, searchString);

                DataRowCollection rs = memorySeriesTable.Rows;

                if (rs.Count == 0)
                {
                    if (!_loadedStrategyTimeSeries.ContainsKey(strategy.ID))
                        _loadedStrategyTimeSeries.TryAdd(strategy.ID, "");


                    return res;
                }

                int counter = 0;

                List<double> vallist = new List<double>();
                List<DateTime> dtlist = new List<DateTime>();
                
                int tsidx = memorySeriesTable.Columns["TimeStamp"].Ordinal;
                int validx = memorySeriesTable.Columns["Value"].Ordinal;

                int mtidx = memorySeriesTable.Columns["MemoryTypeID"].Ordinal;
                int mcidx = memorySeriesTable.Columns["MemoryClassID"].Ordinal;

                Dictionary<string, int[]> tmpRes = new Dictionary<string, int[]>();

                foreach (DataRow r in rs)
                {
                    var memorytype_obj = r[mtidx];
                    var memoryclass_obj = r[mcidx];

                    int memorytype;
                    int memoryclass;

                    if (memorytype_obj is Int64)
                        memorytype = (int)(object)Convert.ToInt32(memorytype_obj);
                    else
                        memorytype = (int)memorytype_obj;

                    if (memoryclass_obj is Int64)
                        memoryclass = (int)(object)Convert.ToInt32(memoryclass_obj);
                    else
                        memoryclass = (int)memoryclass_obj;

                    string key = strategy.ID + "_" + memorytype + "_" + memoryclass;

                    DateTime t = (DateTime)r[tsidx];
                    double val = (double)r[validx];

                    if (!_memorySeriesDatabase.ContainsKey(key))
                        _memorySeriesDatabase.TryAdd(key, new TimeSeries());
                    
                    if (!tmpRes.ContainsKey(key))
                        tmpRes.Add(key, new int[] { memorytype, memoryclass });

                    _memorySeriesDatabase[key].AddDataPoint(t, val);
                }

                foreach (var key in tmpRes.Keys)
                {
                    res.Add(tmpRes[key], _memorySeriesDatabase[key]);
                    counter++;
                }


                foreach (int[] keys in res.Keys)
                {
                    string key = strategy.ID + "_" + keys[0] + "_" + keys[1];
                    TimeSeries ts = res[keys];
                    if (_memorySeriesDatabase.ContainsKey(key))
                        _memorySeriesDatabase[key] = ts;
                    else
                        _memorySeriesDatabase.TryAdd(key, ts);
                }
            }

            if (!_loadedStrategyTimeSeries.ContainsKey(strategy.ID))
                _loadedStrategyTimeSeries.TryAdd(strategy.ID, "");



            return res;
        }

        public Dictionary<int[], TimeSeries> GetMemorySeriesBAK(Strategy strategy)
        {
            Dictionary<int[], TimeSeries> res = new Dictionary<int[], TimeSeries>();

            foreach (string key in _memorySeriesDatabase.Keys)
            {
                if (key.StartsWith(strategy.ID + "_"))
                {
                    string[] k = key.Split('_');
                    int memorytype = int.Parse(k[1]);
                    int memoryclass = int.Parse(k[2]);
                    if (memoryclass != Strategy._aum_id_do_not_use && memorytype != Strategy._aum_id_do_not_use && memoryclass != Strategy._aum_chg_id_do_not_use && memorytype != Strategy._aum_chg_id_do_not_use && memoryclass != Strategy._aum_ord_chg_id_do_not_use && memorytype != Strategy._aum_ord_chg_id_do_not_use)
                        res.Add(new int[] { memorytype, memoryclass }, _memorySeriesDatabase[key]);
                }
            }

            if (!strategy.Simulating && !strategy.SimulationObject && !_loadedStrategyTimeSeries.ContainsKey(strategy.ID))// && _memorySeriesDatabase.Count == 0)
            {
                string tableName = _StrategyMemoryTableName;
                string searchString = "ID = " + strategy.ID;
                string targetString = "DISTINCT MemoryTypeID, MemoryClassID";
                DataTable memorySeriesTable = Database.DB[strategy.StrategyDB].GetDataTable(tableName, targetString, searchString);

                DataRowCollection rs = memorySeriesTable.Rows;

                if (rs.Count == 0)
                    return res;

                int counter = 0;
                foreach (DataRow r in rs)
                {
                    var memorytype_obj = r["MemoryTypeID"];
                    var memoryclass_obj = r["MemoryClassID"];

                    int memorytype;
                    int memoryclass;

                    if (memorytype_obj is Int64)
                        memorytype = (int)(object)Convert.ToInt32(memorytype_obj);
                    else
                        memorytype = (int)memorytype_obj;

                    if (memoryclass_obj is Int64)
                        memoryclass = (int)(object)Convert.ToInt32(memoryclass_obj);
                    else
                        memoryclass = (int)memoryclass_obj;

                    TimeSeries ts = GetMemorySeries_internal(strategy, memorytype, memoryclass);
                    res.Add(new int[] { memorytype, memoryclass }, ts);
                    counter++;
                }


                foreach (int[] keys in res.Keys)
                {
                    string key = strategy.ID + "_" + keys[0] + "_" + keys[1];
                    TimeSeries ts = res[keys];
                    if (_memorySeriesDatabase.ContainsKey(key))
                        _memorySeriesDatabase[key] = ts;
                    else
                        _memorySeriesDatabase.TryAdd(key, ts);
                }
            }

            if (!_loadedStrategyTimeSeries.ContainsKey(strategy.ID))
                _loadedStrategyTimeSeries.TryAdd(strategy.ID, "");



            return res;
        }

        public void AddMemoryPoint(Strategy strategy, DateTime date, double value, int memorytype, int memoryclass, Boolean onlyMemory)
        {
            string key = strategy.ID + "_" + memorytype + "_" + memoryclass;

            GetMemorySeries(strategy, memorytype, memoryclass);

            if (_memorySeriesDatabase.ContainsKey(key))
            {
                if (_memorySeriesDatabase[key].ContainsDate(date))
                {
                    _memorySeriesDatabase[key][date] = value;

                    if (_newMemorySeriesDatabase.ContainsKey(key) && _newMemorySeriesDatabase[key].ContainsKey(date))
                        _newMemorySeriesDatabase[key][date] = new MemorySeriesPoint(strategy.ID, memorytype, memoryclass, date, value);

                }
                else
                {

                    if (!_newMemorySeriesDatabase.ContainsKey(key))
                        _newMemorySeriesDatabase.TryAdd(key, new ConcurrentDictionary<DateTime, MemorySeriesPoint>());

                    if (!_newMemorySeriesDatabase[key].ContainsKey(date))
                        _newMemorySeriesDatabase[key].TryAdd(date, new MemorySeriesPoint(strategy.ID, memorytype, memoryclass, date, value));
                    else
                        _newMemorySeriesDatabase[key][date] = new MemorySeriesPoint(strategy.ID, memorytype, memoryclass, date, value);

                    if (_memorySeriesDatabase.ContainsKey(key) && !_memorySeriesDatabase[key].ContainsDate(date))
                        _memorySeriesDatabase[key].AddDataPoint(date, value);
                }
            }

            if (!onlyMemory && !strategy.SimulationObject && Instrument.TimeSeriesLoadFromDatabase)
                Save(strategy);

            if (!onlyMemory && !strategy.SimulationObject && strategy.Cloud)
                if (RTDEngine.Publish(strategy))
                    RTDEngine.Send(new RTDMessage() { Type = RTDMessage.MessageType.StrategyData, Content = new RTDMessage.StrategyData() { InstrumentID = strategy.ID, Value = value, MemoryClassID = memoryclass, MemoryTypeID = memorytype, Timestamp = date } });
        }

        public double GetMemorySeriesPoint(Strategy strategy, DateTime date, int memorytype, int memoryclass, TimeSeriesRollType timeSeriesRoll)
        {
            string key = strategy.ID + "_" + memorytype + "_" + memoryclass;

            if (!Instrument.TimeSeriesLoadFromDatabase)
            {
                if (!_memorySeriesDatabase.ContainsKey(key))
                    GetMemorySeries(strategy, memorytype, memoryclass);

                if (timeSeriesRoll == TimeSeriesRollType.Exact)
                    return _memorySeriesDatabase[key][date];
                else
                    return _memorySeriesDatabase[key][date, TimeSeries.DateSearchType.Previous];
            }

            string tableName = _StrategyMemoryTableName;
            string searchString;
            string targetString = "TOP 1 *";

            if (timeSeriesRoll == TimeSeriesRollType.Exact)
                searchString = string.Format("ID={0} AND MemoryTypeID={1} AND MemoryClassID={2} AND Timestamp='{3:yyyy-MM-dd HH:mm:ss.fff}' ORDER BY Timestamp DESC", strategy.ID, (int)memorytype, (int)memoryclass, date);
            else
                searchString = string.Format("ID={0} AND MemoryTypeID={1} AND MemoryClassID={2} AND Timestamp<='{3:yyyy-MM-dd HH:mm:ss.fff}' ORDER BY Timestamp DESC", strategy.ID, (int)memorytype, (int)memoryclass, date);

            if(Database.DB[strategy.StrategyDB] is QuantApp.Kernel.Adapters.SQL.SQLiteDataSetAdapter || Database.DB[strategy.StrategyDB] is QuantApp.Kernel.Adapters.SQL.PostgresDataSetAdapter)
            {
                searchString += " LIMIT 1";
                targetString = "*";
            }

            DataTable _datesTable = Database.DB[strategy.StrategyDB].GetDataTable(tableName, targetString, searchString);
            DataRowCollection rs = _datesTable.Rows;

            if (rs.Count == 0)
                return double.NaN;

            DataRow r = rs[0];

            return (double)r["Value"];
        }

        public void Save(Strategy strategy)
        {
            if (strategy.SimulationObject)
                return;

            Dictionary<string, DataTable> _memorySeriesTables = new Dictionary<string, DataTable>();

            ConcurrentDictionary<string, ConcurrentDictionary<DateTime, MemorySeriesPoint>> ls = _newMemorySeriesDatabase;
            if (ls.Count != 0)
            {
                foreach (ConcurrentDictionary<DateTime, MemorySeriesPoint> pts in ls.Values.ToList())
                    foreach (MemorySeriesPoint p in pts.Values.ToList())
                    {
                        if (p.ID == strategy.ID && !double.IsNaN(p.value))
                        {
                            string key = strategy.ID + "_" + p.memorytype + "_" + p.memoryclass;

                            if (!_memorySeriesTables.ContainsKey(key))
                            {
                                string tableName = _StrategyMemoryTableName;
                                string searchString = "ID = -100000" + string.Format(" AND MemoryTypeID={0} AND MemoryClassID={1}", (int)p.memorytype, p.memoryclass);
                                string targetString = null;
                                DataTable timeSeriesTable = Database.DB[strategy.StrategyDB].GetDataTable(tableName, targetString, searchString);
                                _memorySeriesTables.Add(key, timeSeriesTable);
                            }


                            DataTable table = _memorySeriesTables[key];

                            DataRow[] rs = null;
                            if (rs != null && rs.Length != 0)
                            {
                                rs[0]["Value"] = p.value;
                            }
                            else
                            {
                                DataRow r = table.NewRow();
                                r["ID"] = p.ID;
                                r["MemoryTypeID"] = (int)p.memorytype;
                                r["MemoryClassID"] = (int)p.memoryclass;
                                r["Timestamp"] = p.date == DateTime.MinValue ? new DateTime(1900, 01, 01) : p.date;
                                r["Value"] = p.value;
                                table.Rows.Add(r);
                            }
                        }
                    }

                if (_newMemorySeriesDatabase != null)
                {
                    string[] keys = _newMemorySeriesDatabase.Keys.ToArray();
                    foreach (string key in keys)
                        if (key.StartsWith(strategy.ID + "_"))
                        {
                            ConcurrentDictionary<DateTime, MemorySeriesPoint> v = null;
                            _newMemorySeriesDatabase.TryRemove(key, out v);
                        }
                }
            }
            foreach (string key in _memorySeriesTables.Keys.ToList())
                if (key.StartsWith(strategy.ID + "_"))
                {
                    Database.DB[strategy.StrategyDB].AddDataTable(_memorySeriesTables[key]);
                }
        }
        public void Remove(Strategy strategy)
        {
            if (_memorySeriesDatabase != null)
            {
                string[] keys = _memorySeriesDatabase.Keys.ToArray();
                foreach (string key in keys)
                    if (key.StartsWith(strategy.ID + "_"))
                    {
                        TimeSeries v = null;
                        _memorySeriesDatabase.TryRemove(key, out v);
                    }
            }

            if (_memorySeriesTables != null)
            {
                string[] keys = _memorySeriesTables.Keys.ToArray();
                foreach (string key in keys)
                    if (key.StartsWith(strategy.ID + "_"))
                    {
                        if (_memorySeriesTables[key].Rows.Count > 0)
                            _memorySeriesTables[key].Rows[0].Delete();
                        {
                            DataTable v = null;
                            _memorySeriesTables.TryRemove(key, out v);
                        }
                    }
            }

            if (_strategyIdDB.ContainsKey(strategy.ID))
            {
                Strategy v = null;
                _strategyIdDB.TryRemove(strategy.ID, out v);
            }

            if (_mainTables.ContainsKey(strategy.ID))
            {
                DataTable table = _mainTables[strategy.ID];

                if (table != null)
                {
                    DataRowCollection rows = table.Rows;
                    if (rows.Count == 0)
                        return;

                    DataRow row = rows[0];
                    row.Delete();
                    Database.DB["Kernel"].UpdateDataTable(table);
                }
                Database.DB[strategy.StrategyDB].ExecuteCommand("DELETE FROM " + _StrategyMemoryTableName + " WHERE ID = " + strategy.ID);

            }
        }

        public void RemoveFrom(Strategy strategy, DateTime date)
        {
            try
            {
                Database.DB[strategy.StrategyDB].ExecuteCommand("DELETE FROM " + _StrategyMemoryTableName + " WHERE ID = " + strategy.ID + " AND Timestamp >= '" + date.ToString("yyyy/MM/dd HH:mm:ss.fff") + "'");
            }
            catch (Exception e) { SystemLog.Write(e); }
        }

        public void ClearMemory(Strategy strategy, DateTime date)
        {
            lock (objLock)
            {
                foreach (string key in _memorySeriesDatabase.Keys.ToList())
                {
                    if (key.StartsWith(strategy.ID.ToString()) && !key.Contains(Strategy._aum_id_do_not_use.ToString()) && !key.Contains(Strategy._aum_chg_id_do_not_use.ToString()) && !key.Contains(Strategy._aum_ord_chg_id_do_not_use.ToString()) && !key.Contains(Strategy._universe_id_do_not_use.ToString()))
                        _memorySeriesDatabase[key].RemoveDataPoint(date);
                }

                foreach (string key in _newMemorySeriesDatabase.Keys.ToList())
                    if (key.StartsWith(strategy.ID.ToString()) && !key.Contains(Strategy._aum_id_do_not_use.ToString()) && !key.Contains(Strategy._aum_chg_id_do_not_use.ToString()) && !key.Contains(Strategy._aum_ord_chg_id_do_not_use.ToString()) && !key.Contains(Strategy._universe_id_do_not_use.ToString()) && _newMemorySeriesDatabase[key].ContainsKey(date))
                    {
                        MemorySeriesPoint v = new MemorySeriesPoint();
                        _newMemorySeriesDatabase[key].TryRemove(date, out v);
                    }


                if (Instrument.TimeSeriesLoadFromDatabase)
                {
                    string tableName = _StrategyMemoryTableName;
                    string searchString = string.Format("ID={0} AND Timestamp='{1:yyyy-MM-dd HH:mm:ss.fff}' AND (MemoryTypeID<>{2} OR MemoryClassID<>{2} OR MemoryTypeID<>{3} OR MemoryClassID<>{3} OR MemoryTypeID<>{4} OR MemoryClassID<>{4} OR MemoryTypeID<>{5} OR MemoryClassID<>{5})", strategy.ID, date, Strategy._aum_id_do_not_use, Strategy._aum_chg_id_do_not_use, Strategy._universe_id_do_not_use, Strategy._aum_ord_chg_id_do_not_use);
                    string targetString = null;

                    DataTable _datesTable = Database.DB[strategy.StrategyDB].GetDataTable(tableName, targetString, searchString);
                    DataRowCollection rs = _datesTable.Rows;

                    foreach (DataRow r in rs)
                        r.Delete();

                    Database.DB[strategy.StrategyDB].UpdateDataTable(_datesTable);
                }
            }
        }



        public void ClearAUMMemory(Strategy strategy, DateTime date)
        {
            foreach (string key in _memorySeriesDatabase.Keys)
            {
                if (key.StartsWith(strategy.ID.ToString()) && (key.Contains(Strategy._aum_id_do_not_use.ToString()) || key.Contains(Strategy._aum_chg_id_do_not_use.ToString()) || key.Contains(Strategy._aum_ord_chg_id_do_not_use.ToString())))
                    _memorySeriesDatabase[key].RemoveDataPoint(date);
            }

            foreach (string key in _newMemorySeriesDatabase.Keys)
                if (key.StartsWith(strategy.ID.ToString()) && (key.Contains(Strategy._aum_id_do_not_use.ToString()) || key.Contains(Strategy._aum_chg_id_do_not_use.ToString()) || key.Contains(Strategy._aum_ord_chg_id_do_not_use.ToString())) && _newMemorySeriesDatabase[key].ContainsKey(date))
                {
                    MemorySeriesPoint v = new MemorySeriesPoint();
                    _newMemorySeriesDatabase[key].TryRemove(date, out v);
                }

            if (Instrument.TimeSeriesLoadFromDatabase)
            {
                string tableName = _StrategyMemoryTableName;
                string searchString = string.Format("ID={0} AND Timestamp='{1:yyyy-MM-dd HH:mm:ss.fff}' AND (MemoryTypeID={2} OR MemoryClassID={2} OR MemoryTypeID={3} OR MemoryClassID={3} OR MemoryTypeID={4} OR MemoryClassID={4})", strategy.ID, date, Strategy._aum_id_do_not_use, Strategy._aum_chg_id_do_not_use, Strategy._aum_ord_chg_id_do_not_use);
                string targetString = null;

                DataTable _datesTable = Database.DB[strategy.StrategyDB].GetDataTable(tableName, targetString, searchString);
                DataRowCollection rs = _datesTable.Rows;

                foreach (DataRow r in rs)
                    r.Delete();

                Database.DB[strategy.StrategyDB].UpdateDataTable(_datesTable);
            }
        }

        public void Startup(int id, string className)
        {
            if (!_mainTables.ContainsKey(id))
            {
                string tableName = _StrategyTableName;
                string searchString = "ID = " + id;
                string targetString = null;
                DataTable table = Database.DB["Kernel"].GetDataTable(tableName, targetString, searchString);

                if (table.Rows.Count == 0)
                {
                    DataRow nr = table.NewRow();
                    nr["ID"] = id;
                    nr["Class"] = className;
                    nr["PortfolioID"] = -1;
                    table.Rows.Add(nr);
                    Database.DB["Kernel"].UpdateDataTable(table);
                }

                _mainTables.TryAdd(id, table);
            }
        }

        public void SetProperty(Strategy strategy, string name, object value)
        {
            if (!strategy.SimulationObject)
                Startup(strategy.ID, strategy.GetType().ToString());


            DataTable table = _mainTables[strategy.ID];

            if (table != null)
            {
                DataRowCollection rows = table.Rows;

                if (rows.Count == 0)
                {
                    DataRow nr = table.NewRow();
                    nr["ID"] = strategy.ID;
                    nr["Class"] = strategy.GetType().ToString();
                    nr["PortfolioID"] = strategy.Portfolio == null ? -1 : strategy.PortfolioID;
                    table.Rows.Add(nr);
                    Database.DB["Kernel"].UpdateDataTable(table);
                }

                DataRow row = rows[0];
                row[name] = value;
                Database.DB["Kernel"].UpdateDataTable(table);
            }
        }

        public List<Strategy> ActiveMasters(User user, DateTime date)
        {
            List<Strategy> ret = new List<Strategy>();

            string searchString = string.Format("PortfolioID <> -1 AND (FinalDate>='{0:yyyy-MM-dd HH:mm:ss.fff}' OR FinalDate is Null) AND Strategy.ID = Portfolio.StrategyID AND ParentPortfolioID = -1", date);
            string targetString = "*";
            DataTable table = Database.DB["Kernel"].GetDataTable(_StrategyTableName + ", " + SQLPortfolioFactory._portfolioTableName, targetString, searchString);

            DataRowCollection rows = table.Rows;

            if (rows.Count != 0)
                foreach (DataRow row in rows)
                {
                    var id_obj = row["ID"];
                    if (id_obj is Int64)
                        id_obj = (int)(object)Convert.ToInt32(id_obj);

                    int id = (int)id_obj;
                    Strategy i = Instrument.FindInstrument(id) as Strategy;
                    if (i != null && i.Portfolio.ParentPortfolio == null)
                    {
                        if (user.Permission(i) != AccessType.Denied)
                            ret.Add(i);
                    }
                }

            if (_strategyIdDB != null)
                foreach (Strategy strategy in _strategyIdDB.Values)
                {
                    if (!ret.Contains(strategy) && strategy.Portfolio != null && strategy.Portfolio.MasterPortfolio.Strategy == strategy)
                        ret.Add(strategy);
                }

            return ret;
        }
    }
}
