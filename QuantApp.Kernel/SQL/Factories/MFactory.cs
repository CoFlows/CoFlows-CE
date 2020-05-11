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

using System.Data;

using QuantApp.Kernel;
using QuantApp.Kernel.Factories;

using System.Reflection;


namespace QuantApp.Kernel.Adapters.SQL.Factories
{
    public class SQLMFactory : IMFactory
    {
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

        private string _mainTableName = "M";

        public readonly static object objLock = new object();
        private Dictionary<int, DataTable> _mainTables = new Dictionary<int, DataTable>();

        public M Find(string id, Type type)
        {
            lock (objLock)
            {
                string searchString = "ID = '" + id + "'";
                string targetString = null;

                DateTime t0 = DateTime.Now;
                DataTable table = Database.DB["Kernel"].GetDataTable(_mainTableName, targetString, searchString);
                DateTime t1 = DateTime.Now;

                DataRowCollection rows = table.Rows;

                if (rows.Count == 0)
                    return null;

                M m = new M();

                var dict = new Dictionary<string, Type>();
                foreach (DataRow r in rows)
                {

                    string entryID = GetValue<string>(r, "EntryID");
                    string entryString = GetValue<string>(r, "Entry");

                    string typeName = GetValue<string>(r, "Type");
                    string assemblyName = GetValue<string>(r, "Assembly");

                    Type tp = type;


                    try
                    {
                        if (type == null)
                        {
                            
                            try
                            {

                                tp = Type.GetType(typeName);
                                if(tp == null && !dict.ContainsKey(typeName))
                                {
                                    Assembly assembly = M._systemAssemblies.ContainsKey(typeName) ? M._systemAssemblies[typeName] : (M._compiledAssemblyNames.ContainsKey(typeName) ? M._compiledAssemblies[M._compiledAssemblyNames[typeName]] : System.Reflection.Assembly.Load(assemblyName));
                                    tp = assembly.GetType(M._systemAssemblyNames.ContainsKey(typeName) ? M._systemAssemblyNames[typeName] : typeName);
                                    dict.Add(typeName, tp);
                                }
                            }
                            catch
                            {
                                tp = null;
                            }

                            if(!dict.ContainsKey(typeName))
                                dict.Add(typeName, tp);
                        }

                        tp = dict.ContainsKey(typeName) ? dict[typeName] : tp;

                        string filtered_string = entryString.Replace((char)27, '"').Replace((char)26, '\'');
                        if(tp != typeof(Nullable) && filtered_string.StartsWith("\"") && filtered_string.EndsWith("\""))
                            filtered_string = filtered_string.Substring(1, filtered_string.Length - 2).Replace("\\\"", "\"");
                        object obj = tp == typeof(Nullable) || tp == typeof(string) ? filtered_string : Newtonsoft.Json.JsonConvert.DeserializeObject(filtered_string, tp);

                        m.AddInternal(entryID, obj, typeName, assemblyName);
                    }
                    catch (Exception e)
                    {
                        m.AddInternal(entryID, entryString.Replace((char)27, '"').Replace((char)26, '\''), typeName, assemblyName);
                    }
                }
                return m;
            }
        }

        public void Remove(M m)
        {
            Database.DB["Kernel"].ExecuteCommand("DELETE FROM " + _mainTableName + " WHERE ID = '" + m.ID + "'");
        }

        public void Save(M m)
        {
            lock (m)
            {
                var changes = m.Changes.ToList();

                string del = "";
                foreach(var entry in changes)
                    del += "DELETE FROM " + _mainTableName + " WHERE ID = '"+ m.ID + "' AND EntryID = '" + entry.ID + "';";

                if (!string.IsNullOrEmpty(del))
                    Database.DB["Kernel"].ExecuteCommand(del);

                string searchString = "ID = '" + m.ID + "'";
                string targetString = "TOP 0 *";

                if(Database.DB["Kernel"] is QuantApp.Kernel.Adapters.SQL.SQLiteDataSetAdapter || Database.DB["Kernel"] is QuantApp.Kernel.Adapters.SQL.PostgresDataSetAdapter)
                {
                    searchString += " LIMIT 0";
                    targetString = "*";
                }
                DataTable table = Database.DB["Kernel"].GetDataTable(_mainTableName, targetString, searchString);


                int counter = 0;
                foreach(var entry in changes)
                {
                    var obj = entry;
                    counter++;
                    if (obj != null && (obj.Command == 1 || obj.Command == 0))
                    {
                        DataRow r = table.NewRow();
                        r["ID"] = m.ID;
                        r["EntryID"] = obj.ID;
                        r["Assembly"] = obj.Assembly;
                        r["Type"] = obj.Type;

                        if(obj.Type.ToLower() == "system.string" ||  obj.Type.ToLower() == "newtonsoft.json.linq.jobject" || (obj.Data is string))
                        {
                            string filtered_string =  obj.Data.ToString().Replace((char)27, '"').Replace((char)26, '\'');
                            if(filtered_string.StartsWith("\"") && filtered_string.EndsWith("\""))
                                filtered_string = filtered_string.Substring(1, filtered_string.Length - 2).Replace("\\\"", "\"");
                            r["Entry"] = filtered_string.Replace('"', (char)27).Replace('\'', (char)26);
                        }
                        else
                            r["Entry"] = Newtonsoft.Json.JsonConvert.SerializeObject(obj.Data).Replace('"', (char)27).Replace('\'', (char)26);

                        table.Rows.Add(r);

                        if (counter == 1000)
                        {
                            counter = 0;
                            Database.DB["Kernel"].UpdateDataTable(table);

                            searchString = "ID = '" + m.ID + "'";
                            targetString = "TOP 0 *";
                            
                            if(Database.DB["Kernel"] is QuantApp.Kernel.Adapters.SQL.SQLiteDataSetAdapter || Database.DB["Kernel"] is QuantApp.Kernel.Adapters.SQL.PostgresDataSetAdapter)
                            {
                                searchString += " LIMIT 0";
                                targetString = "*";
                            }

                            table = Database.DB["Kernel"].GetDataTable(_mainTableName, targetString, searchString);

                        }
                    }
                }

                Database.DB["Kernel"].UpdateDataTable(table);
            }
        }
    }
}
