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
    public class SQLExchangeFactory : IExchangeFactory
    {
        public DataTable MainTable;
        private static string _mainTableName = "Exchange";

        public readonly static object objLock = new object();
        private Dictionary<int, DataTable> _mainTables = new Dictionary<int, DataTable>();
        private Dictionary<int, Exchange> _exchangeIdDB = new Dictionary<int, Exchange>();
        private Dictionary<string, Exchange> _exchangeNameDB = new Dictionary<string, Exchange>();


        private T GetValue<T>(DataRow row, string columnname)
        {
            object res = null;
            if (row.RowState == DataRowState.Detached)
                return (T)res;
            if (typeof(T) == typeof(string))
                res = "";
            else if (typeof(T) == typeof(int))
                res = 0;
            else if (typeof(T) == typeof(double))
                res = 0.0;
            else if (typeof(T) == typeof(DateTime))
                res = DateTime.MinValue;
            else if (typeof(T) == typeof(bool))
                res = false;
            object obj = row[columnname];
            if (obj is DBNull)
                return (T)res;

            if (typeof(T) == typeof(int))
                return (T)(object)Convert.ToInt32(obj);
            return (T)obj;
        }

        public Exchange CreateExchange(string name, string description, Calendar calendar)
        {
            lock (objLock)
            {
                string searchString = null;
                string targetString = null;
                DataTable table = Database.DB["Kernel"].GetDataTable(_mainTableName, targetString, searchString);
                DataRowCollection rows = table.Rows;

                var lrows = from lrow in new LINQList<DataRow>(rows)
                            where (string)lrow["Name"] == name
                            select lrow;

                if (lrows.Count() == 0)
                {
                    DataRow r = table.NewRow();
                    r["ID"] = Database.DB["Kernel"].NextAvailableID(table.Rows, "ID");
                    r["Name"] = name;
                    r["Description"] = description;
                    r["CalendarID"] = calendar.ID;
                    rows.Add(r);
                    Database.DB["Kernel"].UpdateDataTable(table);

                    return FindExchange(name);
                }
                throw new Exception("Exchange Already Exists");
            }
        }
        public Exchange FindExchange(string name)
        {
            lock (objLock)
            {
                if (_exchangeNameDB.ContainsKey(name))
                    return _exchangeNameDB[name];

                string searchString = "NAME LIKE '" + name + "'";
                string targetString = null;
                DataTable table = Database.DB["Kernel"].GetDataTable(_mainTableName, targetString, searchString);

                DataRowCollection rows = table.Rows;
                if (rows.Count == 0)
                    throw new Exception("Exchange Doesn't Exists");

                DataRow row = rows[0];
                int id = GetValue<int>(row, "ID");
                string description = GetValue<string>(row, "Description");
                int calendarID = GetValue<int>(row, "CalendarID");
                _mainTables.Add(id, table);

                Exchange x = new Exchange(id, name, description, calendarID);
                this._exchangeIdDB.Add(id, x);
                this._exchangeNameDB.Add(name, x);

                return x;
            }
        }
        public Exchange FindExchange(int id)
        {
            lock (objLock)
            {
                if (_exchangeIdDB.ContainsKey(id))
                    return _exchangeIdDB[id];

                string searchString = "ID = " + id;
                string targetString = null;
                DataTable table = Database.DB["Kernel"].GetDataTable(_mainTableName, targetString, searchString);

                DataRowCollection rows = table.Rows;
                if (rows.Count == 0)
                    throw new Exception("Exchange Doesn't Exists");
                DataRow row = rows[0];
                string name = GetValue<string>(row, "Name");
                string description = GetValue<string>(row, "Description");
                int calendarID = GetValue<int>(row, "CalendarID");
                _mainTables.Add(id, table);

                Exchange x = new Exchange(id, name, description, calendarID);
                this._exchangeIdDB.Add(id, x);
                this._exchangeNameDB.Add(name, x);

                return x;
            }
        }

        public List<Exchange> Exchanges()
        {
            string searchString = null;
            string targetString = null;
            DataTable table = Database.DB["Kernel"].GetDataTable(_mainTableName, targetString, searchString);

            DataRowCollection rows = table.Rows;
            if (rows.Count == 0)
                return null;
            else
            {
                List<Exchange> list = new List<Exchange>();
                foreach (DataRow row in rows)
                {
                    int id = (int)row["ID"];
                    list.Add(FindExchange(id));
                }
                return list;
            }
        }

        public void SetProperty(Exchange exchange, string name, object value)
        {
            lock (objLock)
            {
                if (_mainTables.ContainsKey(exchange.ID))
                {
                    DataTable table = _mainTables[exchange.ID];

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
