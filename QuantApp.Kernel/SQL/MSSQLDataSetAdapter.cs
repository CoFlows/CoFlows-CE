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
using System.IO;

using System.Data;
using System.Data.Common;
using System.Data.SqlClient;


namespace QuantApp.Kernel.Adapters.SQL
{
    public class MSSQLDataSetAdapter : IDatabase
    {
        private SqlConnection _connection;

        public string ConnectString { get; set; }

        private Boolean _initialized = false;
        public void Initialize()
        {
            if (!_initialized || _connection.State == ConnectionState.Closed || _connection.State == ConnectionState.Broken)
            {
                _connection = new SqlConnection(ConnectString);
                _connection.Open();
                _initialized = true;
            }
        }

        public void Close()
        {
            _connection.Close();
        }

        public readonly object objLock = new object();


        public int NextAvailableID(DataRowCollection Rows, string idName)
        {
            lock (objLock)
            {
                Initialize();
                List<int> ids = new List<int>();
                foreach (DataRow r in Rows)
                    if (!(r[idName] is DBNull))
                    {
                        int id = (int)r[idName];
                        if (!ids.Contains(id))
                            ids.Add(id);
                    }

                int next_id = 0;
                for (; next_id < int.MaxValue; next_id++)
                    if (!ids.Contains(next_id))
                        break;
                return next_id;
            }
        }

        public List<string> TableScripts()
        {
            List<string> res = new List<string>();

            SqlConnection serverConnection = new SqlConnection(ConnectString);
            serverConnection.Open();

            List<string> tables = new List<string>();
            DataTable dt = serverConnection.GetSchema("Tables");
            foreach (DataRow row in dt.Rows)
            {


                string tablename = (string)row[2];
                tables.Add(tablename);
                string SelectString = @"SELECT TOP 1 * FROM " + tablename;

                SqlDataAdapter serverAdapter = new SqlDataAdapter();
                serverAdapter.SelectCommand = new SqlCommand(SelectString, serverConnection);

                DataSet datasetServer = new DataSet();
                serverAdapter.MissingSchemaAction = MissingSchemaAction.AddWithKey;
                serverAdapter.Fill(datasetServer, tablename);

                DataColumn[] primaryKeys = datasetServer.Tables[tablename].PrimaryKey;
                string createTable = "CREATE TABLE " + tablename + "(";
                DataColumnCollection columns = datasetServer.Tables[tablename].Columns;
                int counter = 0;
                foreach (DataColumn c in columns)
                {
                    string type = "";
                    if (c.DataType == typeof(int))
                        type = "int";
                    else if (c.DataType == typeof(double))
                        type = "float";
                    else if (c.DataType == typeof(string))
                        type = "ntext";
                    else if (c.DataType == typeof(DateTime))
                        type = "datetime";
                    else if (c.DataType == typeof(Boolean))
                        type = "bit";
                    else
                        type = "UNKWON";

                    createTable += (counter == 0 ? "" : ",") + c.ToString() + " " + type;

                    counter++;
                }

                if (primaryKeys != null && primaryKeys.Length > 0)
                {
                    createTable += ", PRIMARY KEY(";
                    counter = 0;
                    foreach (DataColumn c in primaryKeys)
                    {
                        createTable += (counter == 0 ? "" : ", ") + c.ToString();
                        counter++;
                    }

                    createTable += ")";
                }

                createTable += ")";

                res.Add(createTable);

            }
            serverConnection.Close();

            return res;
        }


        public DataTable GetDataTable(string table)
        {
            return GetDataTable(table, null, null);
        }

        public DataTable GetDataTable(string table, string target, string search)
        {
            try
            {
                using (SqlConnection _connectionInternal = new SqlConnection(ConnectString))
                {
                    string SelectString = @"SELECT " + (target == null ? "*" : target) + " FROM " + table + (search == null ? "" : (search.Trim().ToLower().StartsWith("order") ? " " : " WHERE ") + search);

                    DataSet dataset = new DataSet();
                    try
                    {
                        if (_connectionInternal.State != ConnectionState.Open)
                            _connectionInternal.Open();

                        SqlDataAdapter adapter = new SqlDataAdapter();
                        adapter.SelectCommand = new SqlCommand(SelectString, _connectionInternal);
                        adapter.SelectCommand.CommandTimeout = 0 * 60 * 15;

                        adapter.MissingSchemaAction = MissingSchemaAction.AddWithKey;

                        adapter.Fill(dataset, table);
                    }
                    catch (Exception ex)
                    {
                        SqlConnection.ClearAllPools();

                        if (_connectionInternal.State != ConnectionState.Open)
                            _connectionInternal.Open();

                        SqlDataAdapter adapter = new SqlDataAdapter();
                        adapter.SelectCommand = new SqlCommand(SelectString, _connectionInternal);
                        adapter.SelectCommand.CommandTimeout = 0 * 60 * 15;

                        adapter.Fill(dataset, table);


                    }
                    _connectionInternal.Close();

                    return dataset.Tables[table];
                }
            }
            catch (Exception e)
            {
                string SelectString = @"SELECT " + (target == null ? "*" : target) + " FROM " + table + (search == null ? "" : (search.Trim().ToLower().StartsWith("order") ? " " : " WHERE ") + search);
                Console.WriteLine(SelectString + e);
                return null;
            }
        }

        public DataTable ExecuteDataTable(string table, string command)
        {
            using (SqlConnection _connectionInternal = new SqlConnection(ConnectString))
            {
                DataSet dataset = new DataSet();
                try
                {
                    if (_connectionInternal.State != ConnectionState.Open)
                        _connectionInternal.Open();

                    SqlDataAdapter adapter = new SqlDataAdapter();
                    adapter.SelectCommand = new SqlCommand(command, _connectionInternal);
                    adapter.SelectCommand.CommandTimeout = 0 * 60 * 15;

                    adapter.MissingSchemaAction = MissingSchemaAction.AddWithKey;

                    adapter.Fill(dataset, table);
                }
                catch
                {
                    SqlConnection.ClearAllPools();

                    if (_connectionInternal.State != ConnectionState.Open)
                        _connectionInternal.Open();

                    SqlDataAdapter adapter = new SqlDataAdapter();
                    adapter.SelectCommand = new SqlCommand(command, _connectionInternal);
                    adapter.SelectCommand.CommandTimeout = 0 * 60 * 15;

                    adapter.Fill(dataset, table);
                }
                _connectionInternal.Close();
                return dataset.Tables[table];
            }
        }

        public DataTable UpdateDataTable(DataTable table, string target, string search)
        {
            using (SqlConnection _connectionInternal = new SqlConnection(ConnectString))
            {
                if (_connectionInternal.State != ConnectionState.Open)
                    _connectionInternal.Open();

                string SelectString = @"SELECT TOP 1 " + (target == null ? "*" : target) + " FROM " + table.TableName + (search == null ? "" : " WHERE " + search);

                SqlDataAdapter adapter = new SqlDataAdapter();
                adapter.SelectCommand = new SqlCommand(SelectString, _connectionInternal);
                adapter.SelectCommand.CommandTimeout = 0 * 60 * 15;

                adapter.UpdateBatchSize = 0;
                adapter.Update(table.DataSet, table.TableName);


                table.AcceptChanges();

                _connectionInternal.Close();

                return table;
            }
        }

        public void AddDataTable(DataTable table)
        {
            using (SqlConnection cn = new SqlConnection(ConnectString))
            {
                if (cn.State != ConnectionState.Open)
                    cn.Open();
                try
                {
                    using (SqlBulkCopy bulkCopy = new SqlBulkCopy(cn))
                    {
                        bulkCopy.DestinationTableName = table.TableName;
                        bulkCopy.BulkCopyTimeout = 0;
                        bulkCopy.BatchSize = 0;
                        bulkCopy.WriteToServer(table);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                cn.Close();
            }

            return;
        }

        public void UpdateDataTable(DataTable table)
        {
            using (SqlConnection _connectionInternal = new SqlConnection(ConnectString))
            {
                try
                {
                    if (_connectionInternal.State != ConnectionState.Open)
                        _connectionInternal.Open();

                    DataTable changes = table.GetChanges(DataRowState.Added);
                    if (changes != null)
                    {
                        DataRowCollection rows = changes.Rows;
                        DataColumnCollection cols = changes.Columns;

                        int colLength = cols.Count;
                        int rowLength = rows.Count;


                        string insert = @"INSERT INTO " + table.TableName + "(";

                        for (int i = 0; i < colLength; i++)                                                   
                            insert += cols[i].ColumnName + ",";
                        
                        insert = insert.Substring(0, insert.Length - 1) + ")";

                        var transaction = _connectionInternal.BeginTransaction();

                        for (int j = 0; j < rowLength; j++)
                        {
                            DataRow row = rows[j];

                            string addstring = insert + " VALUES(";

                            for (int i = 0; i < colLength; i++)
                            {
                                DataColumn col = cols[i];
                                if (col.DataType == typeof(string) || col.DataType == typeof(char))
                                    addstring += "'" + row[col] + "' , ";

                                else if (col.DataType == typeof(bool))
                                    addstring += ((bool)row[col] ? 1 : 0) + " , ";

                                else if (col.DataType == typeof(DateTime))
                                    addstring += string.IsNullOrWhiteSpace(row[col].ToString()) ? "null, " : "'" + ((DateTime)row[col]).ToString("yyyy-MM-dd HH:mm:ss.fff") + "' , ";

                                else
                                    addstring += (string.IsNullOrWhiteSpace(row[col].ToString()) ? "null" : row[col].ToString()) + " , ";
                            }

                            addstring = addstring.Substring(0, addstring.Length - 2) + ");";
                            SqlCommand command = new SqlCommand(addstring, _connectionInternal, transaction);
                            command.ExecuteNonQuery();
                        }
                        transaction.Commit();
                        table.AcceptChanges();                        
                    }


                    changes = table.GetChanges(DataRowState.Modified);
                    if (changes != null)
                    {
                        DataRowCollection rows = changes.Rows;
                        DataColumnCollection cols = changes.Columns;

                        DataColumn[] primary = changes.PrimaryKey;
                        int pl = primary.Length;
                        
                        var names = new Dictionary<string, string>();
                        for (int i = 0; i < pl; i++)
                        {
                            var p = primary[i];
                            string name = p.ColumnName;
                            names.Add(name, "");                            
                        }

                        int colLength = cols.Count;
                        int rowLength = rows.Count;
                        
                        var transaction = _connectionInternal.BeginTransaction();

                        for (int j = 0; j < rowLength; j++)
                        {
                            string update = @"UPDATE " + table.TableName + " SET ";

                            DataRow row = rows[j];

                            int added = 0;

                            for (int i = 0; i < colLength; i++)
                            {
                                var col = cols[i];
                                string name = col.ColumnName;
                                if (!names.ContainsKey(name))
                                {
                                    added++;
                                    string value = "";
                                    if (col.DataType == typeof(string) || col.DataType == typeof(char))
                                        value = "'" + row[col] + "' , ";

                                    else if (col.DataType == typeof(bool))
                                        value = ((bool)row[col] ? 1 : 0) + " , ";

                                    else if (col.DataType == typeof(DateTime))
                                        value = string.IsNullOrWhiteSpace(row[col].ToString()) ? "null, " : "'" + ((DateTime)row[col]).ToString("yyyy-MM-dd HH:mm:ss.fff") + "' , ";

                                    else
                                        value = (string.IsNullOrWhiteSpace(row[col].ToString()) ? "null" : row[col].ToString()) + " , ";

                                    update += name + " = " + value;
                                }
                            }

                            if (added > 0)
                            {
                                update = update.Substring(0, update.Length - 2) + " WHERE ";

                                for (int i = 0; i < pl; i++)
                                {
                                    var col = primary[i];
                                    string name = col.ColumnName;
                                    string value = "";

                                    if (col.DataType == typeof(string) || col.DataType == typeof(char))
                                        value = "'" + row[col] + "'";

                                    else if (col.DataType == typeof(bool))
                                        value = ((bool)row[col] ? 1 : 0).ToString();

                                    else if (col.DataType == typeof(DateTime))
                                        value = string.IsNullOrWhiteSpace(row[col].ToString()) ? "null" : "'" + ((DateTime)row[col]).ToString("yyyy-MM-dd HH:mm:ss.fff") + "' ";

                                    else
                                        value = (string.IsNullOrWhiteSpace(row[col].ToString()) ? "null" : row[col].ToString());

                                    update += name + " = " + value + (i == pl - 1 ? ";" : " AND ");
                                }

                                update = update.Trim();
                                if (update.EndsWith(", ;"))
                                    update = update.Replace(", ;", ";");

                                SqlCommand command = new SqlCommand(update, _connectionInternal, transaction);
                                command.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();
                        table.AcceptChanges();                        
                    }

                    changes = table.GetChanges(DataRowState.Deleted);
                    if (changes != null)
                    {
                        DataRowCollection rows = changes.Rows;
                        DataColumnCollection cols = changes.Columns;

                        DataColumn[] primary = changes.PrimaryKey;
                        int pl = primary.Length;

                        var names = new Dictionary<string, string>();
                        for (int i = 0; i < pl; i++)
                        {
                            var p = primary[i];
                            string name = p.ColumnName;
                            names.Add(name, "");
                        }

                        int colLength = cols.Count;
                        int rowLength = rows.Count;

                        var transaction = _connectionInternal.BeginTransaction();

                        for (int j = 0; j < rowLength; j++)
                        {
                            string update = @"DELETE FROM " + table.TableName + " WHERE ";

                            DataRow row = rows[j];

                            for (int i = 0; i < pl; i++)
                            {
                                // var col = primary[i];
                                // string name = col.ColumnName;
                                // string value = "";

                                // if (col.DataType == typeof(string) || col.DataType == typeof(char))
                                //     value = "'" + row[col] + "' , ";

                                // else if (col.DataType == typeof(bool))
                                //     value = ((bool)row[col] ? 1 : 0).ToString();

                                // else if (col.DataType == typeof(DateTime))
                                //     value = string.IsNullOrWhiteSpace(row[col].ToString()) ? "null, " : "'" + ((DateTime)row[col]).ToString("yyyy-MM-dd HH:mm:ss.fff") + "' ";

                                // else
                                //     value = (string.IsNullOrWhiteSpace(row[col].ToString()) ? "null" : row[col].ToString());

                                // update += name + " = " + value + (i == pl - 1 ? ";" : " AND ");

                                var col = primary[i];
                                string name = col.ColumnName;
                                string value = "";

                                if (col.DataType == typeof(string) || col.DataType == typeof(char))
                                    value = "'" + row[col, DataRowVersion.Original] + "'";

                                else if (col.DataType == typeof(bool))
                                    value = ((bool)row[col, DataRowVersion.Original] ? 1 : 0).ToString();

                                else if (col.DataType == typeof(DateTime))
                                    value = string.IsNullOrWhiteSpace(row[col, DataRowVersion.Original].ToString()) ? "null" : "'" + ((DateTime)row[col, DataRowVersion.Original]).ToString("yyyy-MM-dd HH:mm:ss.fff") + "' ";

                                else
                                    value = (string.IsNullOrWhiteSpace(row[col, DataRowVersion.Original].ToString()) ? "null" : row[col, DataRowVersion.Original].ToString());

                                update += name + " = " + value + (i == pl - 1 ? ";" : " AND ");
                            }

                            update = update.Trim();
                            if (update.EndsWith(", ;"))
                                update = update.Replace(", ;", ";");


                            SqlCommand command = new SqlCommand(update, _connectionInternal, transaction);
                            command.ExecuteNonQuery();
                        }
                        transaction.Commit();
                        table.AcceptChanges();
                    }
                    _connectionInternal.Close();
                }
                catch (Exception e)
                {
                    SqlConnection.ClearAllPools();
                    Console.WriteLine(e);
                }
            }
        }

        public void DeleteDataTable(DataTable table)
        {
            lock (objLock)
            {
                Initialize();

                string DeleteString = @"SELECT * FROM " + table.TableName;

                SqlDataAdapter adapter = new SqlDataAdapter();
                adapter.SelectCommand = new SqlCommand(DeleteString, _connection);
                adapter.SelectCommand.CommandTimeout = 0 * 60 * 15;

                adapter.Update(table.DataSet, table.TableName);
                table.AcceptChanges();
            }
        }

        public void ExecuteCommand(string command)
        {
            lock (objLock)
            {
                if (string.IsNullOrWhiteSpace(command))
                    return;

                using (SqlConnection _connectionInternal = new SqlConnection(ConnectString))
                {
                    _connectionInternal.Open();
                    SqlCommand com = new SqlCommand(command);
                    com.CommandTimeout = 0 * 60 * 15;
                    com.Connection = _connectionInternal;
                    
                    com.ExecuteNonQuery();

                    _connectionInternal.Close();
                
                }
            }
        }

        public void CreateDB(string connectionString, List<string> schemas)
        {
            // var connString = connectionString.Substring(0, connectionString.IndexOf("Database=") - 1);
            // var dbName = connectionString.Substring(connectionString.IndexOf("Database="));
            // dbName = dbName.Substring(dbName.IndexOf("=") + 1);
            var dbName = "";

            var cons = connectionString.Split(';');
            foreach(var f in cons)
                if(f.ToLower().StartsWith("server="))
                    dbName = f.ToLower().Replace("server=", "");
            
            var connString = connectionString;

            Console.WriteLine("MSSql database: " + dbName);

            try
            {
                var conn = new SqlConnection(connString);
                conn.Open();
                
                using (var cmd = new SqlCommand("SELECT 1 FROM sys.Tables WHERE NAME='" + dbName + "' AND Type = 'U'", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    if(reader.Read())
                        return;
                }
                
                bool addData = true;
                Console.WriteLine("MSSql creating database" + dbName);
                using (var cmd = new SqlCommand("CREATE DATABASE " + dbName, conn))
                cmd.ExecuteNonQuery();
            }
            catch{}

            Console.WriteLine("Checking Schema: " + dbName);
            foreach(var schema in schemas)
                try
                {
                    ExecuteCommand(schema);
                }
                catch{}

            Console.WriteLine("Processed DB: " + dbName);
        }

        public DbDataReader ExecuteReader(string command)
        {
            lock (objLock)
            {
                if (string.IsNullOrWhiteSpace(command))
                    return null;
                SqlConnection _connection_internal = new SqlConnection(ConnectString);
                _connection_internal.Open();

                SqlCommand com = new SqlCommand(command);
                com.CommandTimeout = 0 * 60 * 15;
                com.Connection = _connection_internal;
                return com.ExecuteReader();
            }
        }

        public StreamReader ExecuteStreamReader(string command)
        {
            lock (objLock)
            {
                if (string.IsNullOrWhiteSpace(command))
                    return null;

                SqlConnection _connection_internal = new SqlConnection(ConnectString);
                _connection_internal.Open();

                SqlCommand com = new SqlCommand(command);
                com.CommandTimeout = 0 * 60 * 15;
                com.Connection = _connection_internal;

                var ms = new MemoryStream();
                var sw = new StreamWriter(ms);

                var dataReader = com.ExecuteReader();
                /////////////////////////
                for (int i = 0; i < dataReader.FieldCount; i++)
                {
                    var name = dataReader.GetName(i);
                    if (string.IsNullOrEmpty(name))
                        name = "result";

                    sw.Write(name + (i == dataReader.FieldCount - 1 ? "" : ","));
                }
                sw.WriteLine();

                for (int i = 0; i < dataReader.FieldCount; i++)
                    sw.Write(dataReader.GetFieldType(i) + (i == dataReader.FieldCount - 1 ? "" : ","));
                sw.WriteLine();

                var stream = new MemoryStream();

                var t0 = DateTime.Now;
                int counter = 0;
                while (dataReader.Read())
                {
                    counter++;
                    for (int i = 0; i < dataReader.FieldCount; i++)
                    {
                        var value = dataReader.GetValue(i);
                        var value_str = "";
                        if (value.GetType() == typeof(DateTime))
                            value_str = ((DateTime)value).ToString("yyyy-MM-ddTHH:mm:ss.fff");
                        else if (value.GetType() == typeof(string))
                            value_str = value.ToString().Replace(',', (char)30);
                        else
                            value_str = value.ToString();

                        sw.Write(value_str + (i == dataReader.FieldCount - 1 ? "" : ","));
                    }

                    sw.WriteLine();
                }

                
                dataReader.Close();
                ((System.Data.SqlClient.SqlDataReader)dataReader).Close();
                _connection_internal.Close();

                ms.Position = 0;

                var sr = new StreamReader(ms);



                return sr;
            }
        }
    }
}