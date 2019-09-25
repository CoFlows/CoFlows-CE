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
    public class SQLDataProviderFactory : IDataProviderFactory
    {
        private static string _mainTableName = "DataProvider";
        public DataTable MainTable;

        public readonly static object objLock = new object();
        private Dictionary<int, DataTable> _mainTables = new Dictionary<int, DataTable>();
        private Dictionary<int, DataProvider> _providerIdDB = new Dictionary<int, DataProvider>();
        private Dictionary<string, DataProvider> _providerNameDB = new Dictionary<string, DataProvider>();


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
        public DataProvider CreateDataProvider(string name, string description)
        {
            lock (objLock)
            {
                string searchString = null;
                string targetString = null;
                DataTable table = Database.DB["Kernel"].GetDataTable(_mainTableName, targetString, searchString);
                DataRowCollection rows = table.Rows;

                var lrows = from lrow in new LINQList<DataRow>(rows)
                            where GetValue<string>(lrow, "Name") == name
                            select lrow;

                if (lrows.Count() == 0)
                {
                    DataRow r = table.NewRow();

                    int id = Database.DB["Kernel"].NextAvailableID(table.Rows, "ID");
                    r["ID"] = id;
                    r["Name"] = name;
                    r["Description"] = description;
                    rows.Add(r);
                    Database.DB["Kernel"].UpdateDataTable(table);

                    return FindDataProvider(id);
                }
                throw new Exception("DataProvider Already Exists");
            }
        }
        public DataProvider FindDataProvider(string name)
        {
            lock (objLock)
            {
                if (_providerNameDB.ContainsKey(name))
                    return _providerNameDB[name];

                string searchString = "Name LIKE '" + name + "'";
                string targetString = null;
                DataTable table = Database.DB["Kernel"].GetDataTable(_mainTableName, targetString, searchString);

                DataRowCollection rows = table.Rows;
                if (rows.Count == 0)
                    throw new Exception("DataProvider Doesn't Exists");

                DataRow row = rows[0];
                int id = GetValue<int>(row, "ID");
                string description = GetValue<string>(row, "Description");
                _mainTables.Add(id, table);

                DataProvider p = new DataProvider(id, name, description);
                _providerNameDB.Add(name, p);
                _providerIdDB.Add(id, p);

                return p;
            }
        }
        public DataProvider FindDataProvider(int id)
        {
            lock (objLock)
            {
                if (_providerIdDB.ContainsKey(id))
                    return _providerIdDB[id];

                string searchString = "ID = " + id;
                string targetString = null;
                DataTable table = Database.DB["Kernel"].GetDataTable(_mainTableName, targetString, searchString);

                DataRowCollection rows = table.Rows;
                if (rows.Count == 0)
                    throw new Exception("DataProvider Doesn't Exists");

                DataRow row = rows[0];
                string name = GetValue<string>(row, "Name");
                string description = GetValue<string>(row, "Description");
                _mainTables.Add(id, table);

                DataProvider p = new DataProvider(id, name, description);
                _providerNameDB.Add(name, p);
                _providerIdDB.Add(id, p);

                return p;
            }
        }

        public void SetProperty(DataProvider provider, string name, object value)
        {
            lock (objLock)
            {
                if (_mainTables.ContainsKey(provider.ID))
                {
                    DataTable table = _mainTables[provider.ID];

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
