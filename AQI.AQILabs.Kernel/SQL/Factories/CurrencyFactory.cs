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
    public class SQLCurrencyFactory : ICurrencyFactory
    {
        private string _mainTableName = "Currency";

        private Dictionary<int, Currency> _currencyIdDB = new Dictionary<int, Currency>();
        private Dictionary<string, Currency> _currencyNameDB = new Dictionary<string, Currency>();

        public readonly static object objLock = new object();
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

        public Currency CreateCurrency(string name, string description, Calendar calendar)
        {
            lock (objLock)
            {
                string searchString = null;// "Name LIKE '" + name + "'";
                string targetString = null;
                DataTable table = Database.DB["Kernel"].GetDataTable(_mainTableName, targetString, searchString);
                DataRowCollection rows = table.Rows;

                var lrows = from lrow in new LINQList<DataRow>(rows)
                            where (string)lrow["Name"] == name
                            select lrow;

                if (lrows.Count() == 0)
                {
                    DataRow r = table.NewRow();
                    int id = Database.DB["Kernel"].NextAvailableID(table.Rows, "ID");
                    r["ID"] = id;
                    r["Name"] = name;
                    r["Description"] = description;
                    r["CalendarID"] = calendar.ID;
                    rows.Add(r);
                    Database.DB["Kernel"].UpdateDataTable(table);

                    Currency ccy = new Currency(id, name, description, calendar.ID);

                    UpdateCurrencyDB(ccy);

                    return ccy;
                }
                throw new Exception("Currency Already Exists");
            }
        }

        protected void UpdateCurrencyDB(Currency currency)
        {
            lock (objLock)
            {
                if (!_currencyIdDB.ContainsKey(currency.ID))
                    _currencyIdDB.Add(currency.ID, currency);
                else
                    _currencyIdDB[currency.ID] = currency;

                if (!_currencyNameDB.ContainsKey(currency.Name))
                    _currencyNameDB.Add(currency.Name, currency);
                else
                    _currencyNameDB[currency.Name] = currency;
            }
        }

        public Currency FindCurrency(string name)
        {
            lock (objLock)
            {
                if (_currencyNameDB.ContainsKey(name))
                    return _currencyNameDB[name];

                string searchString = "Name LIKE '" + name + "'";
                string targetString = null;
                DataTable table = Database.DB["Kernel"].GetDataTable(_mainTableName, targetString, searchString);

                DataRowCollection rows = table.Rows;
                if (rows.Count == 0)
                    return null;
                else
                {
                    DataRow row = rows[0];

                    int id = GetValue<int>(row, "ID");
                    string description = GetValue<string>(row, "Description");
                    int calendarID = GetValue<int>(row, "CalendarID");
                    Calendar calendar = Calendar.FindCalendar(calendarID);

                    Currency ccy = new Currency(id, name, description, calendar.ID);
                    _mainTables.Add(ccy.ID, table);
                    UpdateCurrencyDB(ccy);
                    return ccy;
                }
            }
        }
        public Currency FindCurrency(int id)
        {
            lock (objLock)
            {
                if (_currencyIdDB.ContainsKey(id))
                    return _currencyIdDB[id];

                string searchString = "ID = " + id;
                string targetString = null;
                DataTable table = Database.DB["Kernel"].GetDataTable(_mainTableName, targetString, searchString);

                DataRowCollection rows = table.Rows;
                if (rows.Count == 0)
                    throw new Exception("Currency Doesn't Exists");
                else
                {
                    DataRow row = rows[0];
                    string name = GetValue<string>(row, "Name");
                    string description = GetValue<string>(row, "Description");
                    int calendarID = GetValue<int>(row, "CalendarID");
                    Calendar calendar = Calendar.FindCalendar(calendarID);

                    Currency ccy = new Currency(id, name, description, calendar.ID);
                    _mainTables.Add(ccy.ID, table);
                    UpdateCurrencyDB(ccy);
                    return ccy;
                }
            }
        }

        public List<Currency> Currencies()
        {
            lock (objLock)
            {
                string searchString = null;
                string targetString = null;
                DataTable table = Database.DB["Kernel"].GetDataTable(_mainTableName, targetString, searchString);

                DataRowCollection rows = table.Rows;
                if (rows.Count == 0)
                    return null;
                else
                {
                    List<Currency> list = new List<Currency>();
                    foreach (DataRow row in rows)
                    {
                        int id = GetValue<int>(row, "ID");
                        list.Add(FindCurrency(id));
                    }
                    return list;
                }
            }
        }

        public List<string> CurrencyNames()
        {
            lock (objLock)
            {
                string searchString = null;
                string targetString = null;
                DataTable table = Database.DB["Kernel"].GetDataTable(_mainTableName, targetString, searchString);

                DataRowCollection rows = table.Rows;
                if (rows.Count == 0)
                    return null;
                else
                {
                    List<string> list = new List<string>();
                    foreach (DataRow row in rows)
                        list.Add(GetValue<string>(row, "Name"));
                    return list;
                }
            }
        }

        public void SetProperty(Currency currency, string name, object value)
        {
            lock (objLock)
            {
                if (_mainTables.ContainsKey(currency.ID))
                {
                    DataTable table = _mainTables[currency.ID];

                    DataRowCollection rows = table.Rows;
                    if (rows.Count == 0)
                        return;

                    DataRow row = rows[0];
                    row[name] = value;
                    Database.DB["Kernel"].UpdateDataTable(table);
                }
            }
        }
    }

    public class SQLCurrencyPairFactory : ICurrencyPairFactory
    {
        public readonly static object objLock = new object();
        private Dictionary<string, CurrencyPair> _pairStringDB = new Dictionary<string, CurrencyPair>();
        private Dictionary<int, CurrencyPair> _pairIntDB = new Dictionary<int, CurrencyPair>();

        private static string _mainTableName = "CurrencyPair";
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

        public CurrencyPair CreateCurrencyPair(Currency buy, Currency sell, Instrument fxInstrument)
        {
            lock (objLock)
            {
                string searchString = "CurrencyBuyID = " + buy.ID + " AND CurrencySellID = " + sell.ID;
                string targetString = null;
                DataTable table = Database.DB["Kernel"].GetDataTable(_mainTableName, targetString, searchString);
                DataRowCollection rows = table.Rows;

                if (rows.Count == 0)
                {
                    DataRow r = table.NewRow();
                    r["CurrencyBuyID"] = buy.ID;
                    r["CurrencySellID"] = sell.ID;
                    r["FXInstrumentID"] = fxInstrument.ID;
                    rows.Add(r);
                    Database.DB["Kernel"].UpdateDataTable(table);

                    return new CurrencyPair(buy.ID, sell.ID, fxInstrument.ID);
                }
                throw new Exception("CurrencyPair Already Exists");
            }
        }
        public CurrencyPair FindCurrencyPair(Currency buy, Currency sell)
        {
            lock (objLock)
            {
                string id = buy.ID + "_" + sell.ID;
                if (_pairStringDB.ContainsKey(id))
                    return _pairStringDB[id];

                string searchString = "CurrencyBuyID = " + buy.ID + " AND CurrencySellID = " + sell.ID;
                string targetString = null;
                DataTable table = Database.DB["Kernel"].GetDataTable(_mainTableName, targetString, searchString);

                DataRowCollection rows = table.Rows;
                if (rows.Count == 0)
                {
                    _pairStringDB.Add(id, null);
                    return null;
                }

                DataRow row = rows[0];

                int fXInstrumentID = GetValue<int>(row, "FXInstrumentID");

                CurrencyPair cp = new CurrencyPair(buy.ID, sell.ID, fXInstrumentID);
                _pairStringDB.Add(id, cp);

                if (!_mainTables.ContainsKey(fXInstrumentID))
                    _mainTables.Add(fXInstrumentID, table);

                if (!_pairIntDB.ContainsKey(fXInstrumentID))
                    _pairIntDB.Add(fXInstrumentID, cp);

                return cp;
            }
        }
        public CurrencyPair FindCurrencyPair(Instrument FXInstrument)
        {
            lock (objLock)
            {
                if (_pairIntDB.ContainsKey(FXInstrument.ID))
                    return _pairIntDB[FXInstrument.ID];

                string searchString = "FXInstrumentID = " + FXInstrument.ID;
                string targetString = null;
                DataTable table = Database.DB["Kernel"].GetDataTable(_mainTableName, targetString, searchString);

                DataRowCollection rows = table.Rows;
                if (rows.Count == 0)
                    return null;

                DataRow row = rows[0];

                int currencyBuyID = GetValue<int>(row, "CurrencyBuyID");
                int currencySellID = GetValue<int>(row, "CurrencySellID");

                string id = currencyBuyID + "_" + currencySellID;

                CurrencyPair cp = new CurrencyPair(currencyBuyID, currencySellID, FXInstrument.ID);
                if (!_pairStringDB.ContainsKey(id))
                    _pairStringDB.Add(id, cp);
                _pairIntDB.Add(FXInstrument.ID, cp);

                if (!_mainTables.ContainsKey(FXInstrument.ID))
                    _mainTables.Add(FXInstrument.ID, table);

                return cp;
            }
        }

        public void SetProperty(CurrencyPair currencyPair, string name, object value)
        {
            lock (objLock)
            {
                int id = currencyPair.FXInstrumentID;

                if (_mainTables.ContainsKey(id))
                {
                    DataTable table = _mainTables[id];

                    DataRowCollection rows = table.Rows;
                    if (rows.Count == 0)
                        return;

                    DataRow row = rows[0];
                    row[name] = value;
                    Database.DB["Kernel"].UpdateDataTable(table);
                }
            }
        }
    }
}
