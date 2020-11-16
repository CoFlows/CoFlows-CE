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
using AQI.AQILabs.Kernel.Factories;

using QuantApp.Kernel;

namespace AQI.AQILabs.Kernel.Adapters.SQL.Factories
{
    public class SQLSecurityFactory : ISecurityFactory
    {
        private ConcurrentDictionary<int, Security> _securityIdDB = new ConcurrentDictionary<int, Security>();
        private ConcurrentDictionary<string, Dictionary<int, Security>> _securityIsinDB = new ConcurrentDictionary<string, Dictionary<int, Security>>();
        private ConcurrentDictionary<string, Dictionary<int, Security>> _securitySedolDB = new ConcurrentDictionary<string, Dictionary<int, Security>>();
        private static string _securityTableName = "Security";
        private static string _corporateActionTableName = "CorporateAction";
        private static string _isinTableName = "Isin";
        private static string _sedolTableName = "Sedol";

        private ConcurrentDictionary<int, string> _isinIdDB = new ConcurrentDictionary<int, string>();
        private ConcurrentDictionary<string, int> _isinNameDB = new ConcurrentDictionary<string, int>();

        private ConcurrentDictionary<int, string> _sedolIdDB = new ConcurrentDictionary<int, string>();
        private ConcurrentDictionary<string, int> _sedolNameDB = new ConcurrentDictionary<string, int>();


        

        private ConcurrentDictionary<int, DataTable> _mainTables = new ConcurrentDictionary<int, DataTable>();

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
        public Security CreateSecurity(Instrument instrument, string isin, string sedol, Exchange exchange, double pointSize)
        {
            lock (createObjLock)
            {
                if (!(instrument.InstrumentType == InstrumentType.Equity || instrument.InstrumentType == InstrumentType.ETF || instrument.InstrumentType == InstrumentType.Future || instrument.InstrumentType == InstrumentType.Option || instrument.InstrumentType == InstrumentType.Warrant || instrument.InstrumentType == InstrumentType.Fund))
                    throw new Exception("Instrument is not a Security");

                string searchString = "ID = " + instrument.ID;
                string targetString = null;
                DataTable table = Database.DB["Kernel"].GetDataTable(_securityTableName, targetString, searchString);
                DataRowCollection rows = table.Rows;

                if (rows.Count == 0)
                {
                    DataRow r = table.NewRow();
                    r["ID"] = instrument.ID;
                    r["Isin"] = isin;
                    r["Sedol"] = sedol;
                    r["ExchangeID"] = exchange == null ? 0 : exchange.ID;
                    r["PointSize"] = pointSize;
                    rows.Add(r);
                    Database.DB["Kernel"].UpdateDataTable(table);

                    Security s = FindSecurity(instrument);

                    return s;
                }
                throw new Exception("Security Already Exists");
            }
        }

        public readonly static object findObjLock = new object();
        public Security FindSecurity(Instrument instrument)
        {
            lock (findObjLock)
            {
                if (_securityIdDB.ContainsKey(instrument.ID))
                    return _securityIdDB[instrument.ID];

                if (!(instrument.InstrumentType == InstrumentType.Equity || instrument.InstrumentType == InstrumentType.ETF || instrument.InstrumentType == InstrumentType.Future || instrument.InstrumentType == InstrumentType.Option || instrument.InstrumentType == InstrumentType.Warrant || instrument.InstrumentType == InstrumentType.Fund))
                    throw new Exception("Instrument is not a Security");

                string tableName = _securityTableName;
                string searchString = "ID = " + instrument.ID;
                string targetString = null;
                DataTable table = Database.DB["Kernel"].GetDataTable(tableName, targetString, searchString);

                DataRowCollection rows = table.Rows;
                if (rows.Count == 0)
                    return null;

                DataRow r = rows[0];
                string isin = GetValue<string>(r, "Isin");
                string sedol = GetValue<string>(r, "Sedol");
                int exchangeID = GetValue<int>(r, "ExchangeID");
                double pointSize = GetValue<double>(r, "PointSize");

                Security s = new Security(instrument, isin, sedol, exchangeID, pointSize);
                _securityIdDB.TryAdd(s.ID, s);

                if (!string.IsNullOrWhiteSpace(isin))
                {
                    if (!_securityIsinDB.ContainsKey(isin))
                        _securityIsinDB.TryAdd(isin, new Dictionary<int, Security>());

                    _securityIsinDB[isin].Add(s.ID, s);
                }

                _mainTables.TryAdd(s.ID, table);

                return s;
            }
        }

        public IEnumerable<Security> FindSecurityByIsin(string isin)
        {
            lock (findObjLock)
            {
                if (_securityIsinDB.ContainsKey(isin))
                    return _securityIsinDB[isin].Values;

                string tableName = _securityTableName;
                string searchString = "Isin LIKE '" + isin + "'";
                string targetString = null;
                DataTable table = Database.DB["Kernel"].GetDataTable(tableName, targetString, searchString);

                DataRowCollection rows = table.Rows;
                if (rows.Count == 0)
                {
                    _securityIsinDB.TryAdd(isin, new Dictionary<int, Security>());
                    return null;
                }

                Dictionary<int, Security> ls = new Dictionary<int, Security>();
                foreach (DataRow r in rows)
                {
                    int id = GetValue<int>(r, "ID");

                    Security s = Instrument.FindInstrument(id) as Security;
                    ls.Add(id, s);
                }

                _securityIsinDB.TryAdd(isin, ls);


                return ls.Values;
            }
        }

        public IEnumerable<Security> FindSecurityByIsin(int isinID)
        {
            lock (findObjLock)
            {
                string isin = Security.FindIsin(isinID);

                if (_securityIsinDB.ContainsKey(isin))
                    return _securityIsinDB[isin].Values;

                string tableName = _securityTableName;
                string searchString = "Isin LIKE '" + isin + "'";
                string targetString = null;
                DataTable table = Database.DB["Kernel"].GetDataTable(tableName, targetString, searchString);

                DataRowCollection rows = table.Rows;
                if (rows.Count == 0)
                    return null;

                Dictionary<int, Security> ls = new Dictionary<int, Security>();
                foreach (DataRow r in rows)
                {
                    int id = GetValue<int>(r, "ID");
                    Security s = Instrument.FindInstrument(id) as Security;
                    ls.Add(id, s);
                }

                _securityIsinDB.TryAdd(isin, ls);


                return ls.Values;
            }
        }

        public readonly static object findIsinObjLock = new object();
        private int CreateIsin(string isin)
        {
            string searchString = "Isin LIKE '" + isin + "'";
            string targetString = null;
            DataTable table = Database.DB["Kernel"].GetDataTable(_isinTableName, targetString, searchString);
            DataRowCollection rows = table.Rows;
            if (rows.Count == 0)
            {
                int id = -1;
                DataTable idtable = Database.DB["Kernel"].GetDataTable(_isinTableName, "MAX(ID)", null);
                foreach (DataRow ir in idtable.Rows)
                {
                    id = (int)ir[0];
                }
                id++;
                DateTime createTime = DateTime.Now;
                DataRow r = table.NewRow();
                r["ID"] = id;
                r["Isin"] = isin;
                table.Rows.Add(r);
                Database.DB["Kernel"].UpdateDataTable(table);

                return id;
            }
            throw new Exception("Isin Already Exists");
        }

        public string FindIsin(int isin)
        {
            lock (findIsinObjLock)
            {
                if (_isinIdDB.ContainsKey(isin))
                    return _isinIdDB[isin];

                string tableName = _isinTableName;
                string searchString = "ID = " + isin;
                string targetString = null;
                DataTable table = Database.DB["Kernel"].GetDataTable(tableName, targetString, searchString);

                DataRowCollection rows = table.Rows;
                if (rows.Count == 0)
                    return null;

                DataRow r = rows[0];
                string res = GetValue<string>(r, "Isin");

                if (!_isinIdDB.ContainsKey(isin))
                    _isinIdDB.TryAdd(isin, res);

                if (!_isinNameDB.ContainsKey(res))
                    _isinNameDB.TryAdd(res, isin);

                return res;
            }
        }
        public int FindIsin(string isin)
        {
            lock (findIsinObjLock)
            {
                if (_isinNameDB.ContainsKey(isin))
                    return _isinNameDB[isin];

                string tableName = _isinTableName;
                string searchString = "Isin Like '" + isin + "'";
                string targetString = null;
                DataTable table = Database.DB["Kernel"].GetDataTable(tableName, targetString, searchString);

                DataRowCollection rows = table.Rows;
                if (rows.Count == 0)
                {
                    int id = CreateIsin(isin);

                    if (!_isinNameDB.ContainsKey(isin))
                        _isinNameDB.TryAdd(isin, id);

                    if (!_isinIdDB.ContainsKey(id))
                        _isinIdDB.TryAdd(id, isin);

                    return id;
                }

                DataRow r = rows[0];
                
                int res = GetValue<int>(r, "ID");

                if (!_isinNameDB.ContainsKey(isin))
                    _isinNameDB.TryAdd(isin, res);

                if (!_isinIdDB.ContainsKey(res))
                    _isinIdDB.TryAdd(res, isin);

                return res;
            }
        }


        public readonly static object findSedolObjLock = new object();
        public IEnumerable<Security> FindSecurityBySedol(string sedol)
        {
            lock (findSedolObjLock)
            {
                if (_securitySedolDB.ContainsKey(sedol))
                    return _securitySedolDB[sedol].Values;

                
                string tableName = _securityTableName;
                string searchString = "Sedol LIKE '" + sedol + "'";
                string targetString = null;
                DataTable table = Database.DB["Kernel"].GetDataTable(tableName, targetString, searchString);

                DataRowCollection rows = table.Rows;
                if (rows.Count == 0)
                {
                    _securitySedolDB.TryAdd(sedol, new Dictionary<int, Security>());
                    return null;
                }

                Dictionary<int, Security> ls = new Dictionary<int, Security>();
                foreach (DataRow r in rows)
                {
                    int id = GetValue<int>(r, "ID");

                    Security s = Instrument.FindInstrument(id) as Security;
                    ls.Add(id, s);
                }

                _securitySedolDB.TryAdd(sedol, ls);


                return ls.Values;
            }
        }

        public IEnumerable<Security> FindSecurityBySedol(int sedolID)
        {
            lock (findSedolObjLock)
            {
                string sedol = Security.FindSedol(sedolID);

                if (_securitySedolDB.ContainsKey(sedol))
                    return _securitySedolDB[sedol].Values;

                string tableName = _securityTableName;
                string searchString = "Sedol LIKE '" + sedol + "'";
                string targetString = null;
                DataTable table = Database.DB["Kernel"].GetDataTable(tableName, targetString, searchString);

                DataRowCollection rows = table.Rows;
                if (rows.Count == 0)
                    return null;

                Dictionary<int, Security> ls = new Dictionary<int, Security>();
                foreach (DataRow r in rows)
                {
                    int id = GetValue<int>(r, "ID");
                    Security s = Instrument.FindInstrument(id) as Security;
                    ls.Add(id, s);
                }

                _securitySedolDB.TryAdd(sedol, ls);


                return ls.Values;
            }
        }



        private int CreateSedol(string sedol)
        {
            string searchString = "Sedol LIKE '" + sedol + "'";
            string targetString = null;
            DataTable table = Database.DB["Kernel"].GetDataTable(_sedolTableName, targetString, searchString);
            DataRowCollection rows = table.Rows;
            if (rows.Count == 0)
            {
                int id = -1;
                DataTable idtable = Database.DB["Kernel"].GetDataTable(_sedolTableName, "MAX(ID)", null);
                foreach (DataRow ir in idtable.Rows)
                {
                    id = (int)ir[0];
                }
                id++;
                DateTime createTime = DateTime.Now;
                DataRow r = table.NewRow();

                r["ID"] = id;
                r["Sedol"] = sedol;
                table.Rows.Add(r);
                Database.DB["Kernel"].UpdateDataTable(table);

                return id;
            }
            throw new Exception("Sedol Already Exists");
        }

        
        public string FindSedol(int sedol)
        {
            lock (findSedolObjLock)
            {
                if (_sedolIdDB.ContainsKey(sedol))
                    return _sedolIdDB[sedol];

                string tableName = _sedolTableName;
                string searchString = "ID = " + sedol;
                string targetString = null;
                DataTable table = Database.DB["Kernel"].GetDataTable(tableName, targetString, searchString);

                DataRowCollection rows = table.Rows;
                if (rows.Count == 0)
                    return null;

                DataRow r = rows[0];
                string res = GetValue<string>(r, "Sedol");

                if (!_sedolIdDB.ContainsKey(sedol))
                    _sedolIdDB.TryAdd(sedol, res);

                if (!_sedolNameDB.ContainsKey(res))
                    _sedolNameDB.TryAdd(res, sedol);

                return res;
            }
        }
        public int FindSedol(string sedol)
        {
            lock (findSedolObjLock)
            {
                if (_sedolNameDB.ContainsKey(sedol))
                    return _sedolNameDB[sedol];

                string tableName = _sedolTableName;
                string searchString = "Sedol Like '" + sedol + "'";
                string targetString = null;
                DataTable table = Database.DB["Kernel"].GetDataTable(tableName, targetString, searchString);

                DataRowCollection rows = table.Rows;
                if (rows.Count == 0)
                {
                    int id = CreateSedol(sedol);

                    if (!_sedolNameDB.ContainsKey(sedol))
                        _sedolNameDB.TryAdd(sedol, id);

                    if (!_sedolIdDB.ContainsKey(id))
                        _sedolIdDB.TryAdd(id, sedol);

                    return id;
                }

                DataRow r = rows[0];

                int res = GetValue<int>(r, "ID");

                if (!_sedolNameDB.ContainsKey(sedol))
                    _sedolNameDB.TryAdd(sedol, res);

                if (!_sedolIdDB.ContainsKey(res))
                    _sedolIdDB.TryAdd(res, sedol);

                return res;
            }
        }








        public readonly static object corpObjLock = new object();

        private ConcurrentDictionary<int, ConcurrentDictionary<DateTime, List<CorporateAction>>> _corporateActions = new ConcurrentDictionary<int, ConcurrentDictionary<DateTime, List<CorporateAction>>>();

        public List<CorporateAction> CorporateActions(Security security)
        {
            lock (corpObjLock)
            {
                if (!_corporateActions.ContainsKey(security.ID))
                    CorporateActions(security, DateTime.Today);

                List<CorporateAction> result = new List<CorporateAction>();

                foreach (DateTime date in _corporateActions[security.ID].Keys.ToList())
                    result.AddRange(_corporateActions[security.ID][date]);

                return result;
            }
        }
        public List<CorporateAction> CorporateActions(Security security, DateTime date)
        {
            lock (corpObjLock)
            {
                if (!_corporateActions.ContainsKey(security.ID))
                {
                    if (!_corporateActions.ContainsKey(security.ID))
                        _corporateActions.TryAdd(security.ID, new ConcurrentDictionary<DateTime, List<CorporateAction>>());

                    string tableName = _corporateActionTableName;
                    string searchString = "InstrumentID = " + security.ID;
                    string targetString = null;
                    DataTable table = Database.DB["Kernel"].GetDataTable(tableName, targetString, searchString);

                    DataRowCollection rows = table.Rows;

                    foreach (DataRow r in rows)
                    {
                        string id = GetValue<string>(r, "ID");
                        DateTime declaredDate = GetValue<DateTime>(r, "DeclaredDate");
                        DateTime exDate = GetValue<DateTime>(r, "ExDate");
                        DateTime recordDate = GetValue<DateTime>(r, "RecordDate");
                        DateTime payableDate = GetValue<DateTime>(r, "PayableDate");
                        double amount = GetValue<double>(r, "Amount");
                        string frequency = GetValue<string>(r, "Frequency");
                        string type = GetValue<string>(r, "Type");

                        CorporateAction caction = new CorporateAction(id, security.ID, declaredDate, exDate, recordDate, payableDate, amount, frequency, type);

                        if (!_corporateActions[security.ID].ContainsKey(exDate))
                            _corporateActions[security.ID].TryAdd(exDate, new List<CorporateAction>());

                        _corporateActions[security.ID][exDate].Add(caction);
                    }
                }

                if (!_corporateActions.ContainsKey(security.ID))
                    _corporateActions.TryAdd(security.ID, new ConcurrentDictionary<DateTime, List<CorporateAction>>());

                if (!_corporateActions[security.ID].ContainsKey(date))
                    _corporateActions[security.ID].TryAdd(date, new List<CorporateAction>());



                if (_corporateActions.ContainsKey(security.ID) && _corporateActions[security.ID].ContainsKey(date))
                    return _corporateActions[security.ID][date];

                return new List<CorporateAction>();
            }
        }

        public void AddCorporateAction(Security security, CorporateAction action)
        {
            try
            {
                action.SecurityID = security.ID;
                if (CorporateActions(action.Security, action.ExDate).Contains(action))
                    return;

                _corporateActions[action.Security.ID][action.ExDate].Add(action);
            }
            catch { }
            string searchString = "ID = '" + action.ID + "'";
            string targetString = null;
            DataTable table = Database.DB["Kernel"].GetDataTable(_corporateActionTableName, targetString, searchString);
            DataRowCollection rows = table.Rows;

            if (rows.Count == 0)
            {
                DataRow r = table.NewRow();
                r["ID"] = action.ID;
                r["InstrumentID"] = security.ID;
                r["DeclaredDate"] = action.DeclaredDate;
                r["ExDate"] = action.ExDate;
                r["RecordDate"] = action.RecordDate;
                r["PayableDate"] = action.PayableDate;
                r["Amount"] = action.Amount;
                r["Frequency"] = action.Frequency;
                r["Type"] = action.Type;

                rows.Add(r);
                Database.DB["Kernel"].UpdateDataTable(table);
            }
        }

        public void AddCorporateAction(Security security, Dictionary<DateTime, List<CorporateAction>> actions)
        {
            if (actions == null || (actions != null && actions.Count == 0))
                return;

            List<CorporateAction> newActions = new List<CorporateAction>();

            foreach (List<CorporateAction> acts in actions.Values)
            {
                foreach (CorporateAction action in acts)
                {
                    action.SecurityID = security.ID;
                    try
                    {
                        if (!CorporateActions(action.Security, action.ExDate).Contains(action))
                        {
                            _corporateActions[action.Security.ID][action.ExDate].Add(action);
                            newActions.Add(action);
                        }
                    }
                    catch { }
                }
            }

            if (newActions.Count > 0)
            {
                string searchString = "InstrumentID = '" + security.ID + "'";
                string targetString = null;
                DataTable table = Database.DB["Kernel"].GetDataTable(_corporateActionTableName, targetString, searchString);
                DataRowCollection rows = table.Rows;


                foreach (CorporateAction action in newActions)
                {

                    //if (rows.Count == 0)
                    {
                        DataRow r = table.NewRow();
                        r["ID"] = action.ID;
                        r["InstrumentID"] = security.ID;
                        r["DeclaredDate"] = action.DeclaredDate;
                        r["ExDate"] = action.ExDate;
                        r["RecordDate"] = action.RecordDate;
                        r["PayableDate"] = action.PayableDate;
                        r["Amount"] = action.Amount;
                        r["Frequency"] = action.Frequency;
                        r["Type"] = action.Type;

                        rows.Add(r);

                    }
                }

                Database.DB["Kernel"].UpdateDataTable(table);
            }
        }

        public readonly static object setObjLock = new object();
        public void SetProperty(Security security, string name, object value)
        {
            lock (setObjLock)
            {
                DataTable table = _mainTables[security.ID];

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

        public readonly static object removeObjLock = new object();
        public void Remove(Security security)
        {
            lock (removeObjLock)
            {
                SystemLog.Write("-------Removing: " + this);

                DataTable table = _mainTables[security.ID];

                if (table != null)
                {
                    DataRowCollection rows = table.Rows;
                    if (rows.Count == 0)
                        return;

                    DataRow row = rows[0];
                    row.Delete();
                    Database.DB["Kernel"].UpdateDataTable(table);
                }
                Security outs = null;
                _securityIdDB.TryRemove(security.ID, out outs);
                if (_securityIsinDB.ContainsKey(security.Isin))
                {
                    Dictionary<int, Security> outss = null;
                    _securityIsinDB.TryRemove(security.Isin, out outss);
                }
            }
        }
    }
}
