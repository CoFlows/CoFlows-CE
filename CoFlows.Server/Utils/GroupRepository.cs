/*
 * The MIT License (MIT)
 * Copyright (c) Arturo Rodriguez All rights reserved.
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using System.Data;
using QuantApp.Kernel;

namespace CoFlows.Server.Utils
{
    public class GroupRepository
    {
        private static T GetValue<T>(DataRow row, string columnname)
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
            object obj = row[columnname];
            if (obj is DBNull)
                return (T)res;
            return (T)obj;
        }

        private static Dictionary<string, Dictionary<string, string>> _db = new Dictionary<string, Dictionary<string, string>>();
        public static void Set(Group group, string key, string value)
        {
            if (_db.ContainsKey(group.ID) && _db[group.ID].ContainsKey(key))
                _db[group.ID][key] = value;

            string tableName = "Roles";
            string searchString = "ID = '" + group.ID + "'";
            string targetString = null;
            DataTable _dataTable = Database.DB["CloudApp"].GetDataTable(tableName, targetString, searchString);

            DataRowCollection rows = _dataTable.Rows;

            if (rows.Count != 0)
            {
                rows[0][key] = value;
                Database.DB["CloudApp"].UpdateDataTable(_dataTable);
            }
        }

        public static string Get(Group group, string key)
        {
            if (_db.ContainsKey(group.ID) && _db[group.ID].ContainsKey(key))
                return _db[group.ID][key];


            string tableName = "Roles";
            string searchString = null;
            string targetString = null;
            DataTable _dataTable = Database.DB["CloudApp"].GetDataTable(tableName, targetString, searchString);

            DataRowCollection rows = _dataTable.Rows;

            foreach (DataRow row in rows)
            {
                string id = GetValue<string>(row, "ID");
                string value = GetValue<string>(row, key);

                if (!_db.ContainsKey(id))
                    _db.Add(id, new Dictionary<string, string>());

                if (!_db[id].ContainsKey(key))
                    _db[id].Add(key, value);
                else
                    _db[id][key] = value;
            }

            if (_db.ContainsKey(group.ID) && _db[group.ID].ContainsKey(key))
                return _db[group.ID][key];
            else
                return "";
        }

        public static Group FindByProfile(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return null;

            string tableName = "Roles";
            string searchString = "profile = '" + key + "'";
            string targetString = null;
            DataTable _dataTable = Database.DB["CloudApp"].GetDataTable(tableName, targetString, searchString);

            DataRowCollection rows = _dataTable.Rows;

            if (rows.Count != 0)
                return Group.FindGroup((string)rows[0]["ID"]);

            return null;
        }

        public static Group FindByURL(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return null;

            string tableName = "Roles";
            string searchString = "url LIKE '%" + url + "%'";
            string targetString = null;
            DataTable _dataTable = Database.DB["CloudApp"].GetDataTable(tableName, targetString, searchString);

            DataRowCollection rows = _dataTable.Rows;

            if (rows.Count != 0)
                return Group.FindGroup((string)rows[0]["ID"]);

            return null;
        }
    }
}