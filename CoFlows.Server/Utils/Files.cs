using System;
using System.Collections.Generic;
using System.Linq;

using System.Data;

using QuantApp.Kernel;
// using AQI.AQILabs.Kernel;


namespace CoFlows.Server.Utils
{    
    public class FilePermission : IPermissible
    {
        

        public string ID = null;
        public QuantApp.Kernel.User Owner = null;
        public byte[] Data = null;
        public DateTime Timestamp = DateTime.MinValue;
        public string Name = null;
        public string Type = null;
        
        public FilePermission(string id, string name, DateTime timestamp, QuantApp.Kernel.User owner, byte[] data, string type)
        {
            this.ID = id;
            this.Name = name;
            this.Timestamp = timestamp;
            this.Owner = owner;
            this.Data = data;
            this.Type = type;
        }


        public string PermissibleID
        {
            get
            {
                return ID;
            }
        }

        public String Size
        {
            get
            {
                if (Data == null)
                    return "0B";

                long byteCount = Data.Length;
                string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; //Longs run out around EB
                if (byteCount == 0)
                    return "0" + suf[0];
                long bytes = Math.Abs(byteCount);
                int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
                double num = Math.Round(bytes / Math.Pow(1024, place), 1);
                return (Math.Sign(byteCount) * num).ToString() + suf[place];
            }
        }
    }

    public class FileRepository
    {
        private static object GetValue(DataRow row, string columnname, Type type)
        {
            object res = null;
            if (row.RowState == DataRowState.Detached)
                return res;
            if (type == typeof(string))
                res = "";
            else if (type == typeof(int))
                res = int.MinValue;
            else if (type == typeof(double))
                res = double.NaN;
            else if (type == typeof(DateTime))
                res = DateTime.MinValue;
            else if (type == typeof(bool))
                res = false;
            object obj = row[columnname];
            if (obj is DBNull)
                return res;
            return obj;
        }

        /// <summary>
        /// File Repository
        /// </summary>

        private static DataTable _fileRepositoryDataTable = null;
        private static string _fileReposityTableName = "FileRepository";
        //private static DataTable FileRepositoryDataTable
        //{
        //    get
        //    {
        //        if (_fileRepositoryDataTable == null)
        //        {
        //            string searchString = null;
        //            string targetString = null;
        //            _fileRepositoryDataTable = Database.DB["CloudApp"].GetDataTable(_fileReposityTableName, targetString, searchString);
        //        }

        //        return _fileRepositoryDataTable;
        //    }
        //}
        public static Dictionary<string, FilePermission> Files()
        {
            string searchString = null;
            string targetString = null;
            _fileRepositoryDataTable = Database.DB["CloudApp"].GetDataTable(_fileReposityTableName, targetString, searchString);
            DataRowCollection rows = _fileRepositoryDataTable.Rows;

            //var lrows = from lrow in new LINQList<DataRow>(rows)
            //            orderby (DateTime)lrow["Timestamp"] descending                        
            //            select lrow;

            Dictionary<string, FilePermission> result = new Dictionary<string, FilePermission>();

            if (rows.Count != 0)
                foreach (DataRow row in rows)
                {
                    string id = (string)row["ID"];
                    string name = (string)row["Name"];
                    // string description = (string)row["Description"];
                    string type = (string)row["Type"];
                    string userid = (string)row["UserID"];
                    QuantApp.Kernel.User user = QuantApp.Kernel.User.FindUser(userid);
                    DateTime date = (DateTime)row["Timestamp"];
                    byte[] data = System.Convert.FromBase64String((string)row["Data"]);

                    Console.WriteLine("----- FILE: " + name);
                    // FilePermission file = new FilePermission(id, name, description, type, date, user, data);
                    FilePermission file = new FilePermission(id, name, date, user, data, type);
                    result.Add((string)row["ID"], file);
                }

            return result;
        }

        public static Dictionary<string, FilePermission> FilesByUser(string userID)
        {
            //_fileRepositoryDataTable = null;
            //DataTable table = DataTable;
            //DataRowCollection rows = FileRepositoryDataTable.Rows;

            string searchString = "UserID = '" + userID + "'";
            string targetString = null;
            _fileRepositoryDataTable = Database.DB["CloudApp"].GetDataTable(_fileReposityTableName, targetString, searchString);
            DataRowCollection rows = _fileRepositoryDataTable.Rows;


            //var lrows = from lrow in new LINQList<DataRow>(rows)
            //            where (string)lrow["UserID"] == userID
            //            orderby (DateTime)lrow["Timestamp"] descending
            //            select lrow;

            Dictionary<string, FilePermission> result = new Dictionary<string, FilePermission>();

            if (rows.Count != 0)
                foreach (DataRow row in rows)
                {
                    string id = (string)row["ID"];
                    string name = (string)row["Name"];
                    // string description = (string)row["Description"];
                    string type = (string)row["Type"];
                    string userid = (string)row["UserID"];
                    QuantApp.Kernel.User user = QuantApp.Kernel.User.FindUser(userid);
                    DateTime date = (DateTime)row["Timestamp"];
                    byte[] data = System.Convert.FromBase64String((string)row["Data"]);
                    // FilePermission file = new FilePermission(id, name, description, type, date, user, data);
                    Console.WriteLine("----- FILE: " + name);
                    FilePermission file = new FilePermission(id, name, date, user, data, type);
                    result.Add((string)row["ID"], file);
                }

            return result;
        }

        public static FilePermission File(string id)
        {
            //_fileRepositoryDataTable = null;

            //DataRowCollection rows = FileRepositoryDataTable.Rows;

            string searchString = "ID = '" + id + "'";
            string targetString = null;
            _fileRepositoryDataTable = Database.DB["CloudApp"].GetDataTable(_fileReposityTableName, targetString, searchString);
            DataRowCollection rows = _fileRepositoryDataTable.Rows;

            //var lrows = from lrow in new LINQList<DataRow>(rows)
            //            where (string)lrow["Id"] == id
            //            select lrow;

            if (rows.Count != 0)
                foreach (DataRow row in rows)
                {
                    string name = (string)row["Name"];
                    // string description = (string)row["Description"];
                    string type = (string)row["Type"];
                    string userid = (string)row["UserID"];
                    QuantApp.Kernel.User user = QuantApp.Kernel.User.FindUser(userid);
                    DateTime date = (DateTime)row["Timestamp"];
                    byte[] data = System.Convert.FromBase64String((string)row["Data"]);
                    // FilePermission file = new FilePermission(id, name, description, type, date, user, data);
                    FilePermission file = new FilePermission(id, name, date, user, data, type);
                    return file;
                }
            return null;
        }

        // public static string EditDescription(string id, string description)
        // {
        //     //_fileRepositoryDataTable = null;

        //     //DataRowCollection rows = FileRepositoryDataTable.Rows;

        //     string searchString = "ID = '" + id + "'";
        //     string targetString = null;
        //     _fileRepositoryDataTable = Database.DB["CloudApp"].GetDataTable(_fileReposityTableName, targetString, searchString);
        //     DataRowCollection rows = _fileRepositoryDataTable.Rows;

        //     //var lrows = from lrow in new LINQList<DataRow>(rows)
        //     //            where (string)lrow["Id"] == id
        //     //            select lrow;

        //     if (rows.Count != 0)
        //         foreach (DataRow row in rows)
        //         {
        //             row["Description"] = description;
        //             Database.DB["CloudApp"].UpdateDataTable(_fileRepositoryDataTable);
        //             return "ok";
        //         }
        //     return null;
        // }

        public static void Remove(string id)
        {
            //_fileRepositoryDataTable = null;

            //DataRowCollection rows = FileRepositoryDataTable.Rows;

            string searchString = "ID = '" + id + "'";
            string targetString = null;
            _fileRepositoryDataTable = Database.DB["CloudApp"].GetDataTable(_fileReposityTableName, targetString, searchString);
            DataRowCollection rows = _fileRepositoryDataTable.Rows;

            //var lrows = from lrow in new LINQList<DataRow>(rows)
            //            where (string)lrow["Id"] == id
            //            select lrow;



            if (rows.Count != 0)
                foreach (DataRow row in rows)
                    row.Delete();

            Database.DB["CloudApp"].UpdateDataTable(_fileRepositoryDataTable);
        }
        public static void AddFile(string id, string name, byte[] data, string type, DateTime timestamp, string userID, string groupID)
        {
            //DataTable table = DataTable;
            //DataRowCollection rows = FileRepositoryDataTable.Rows;
            string searchString = "ID = '" + id + "'";
            string targetString = "TOP 1 *";
            if(Database.DB["CloudApp"] is QuantApp.Kernel.Adapters.SQL.SQLiteDataSetAdapter || Database.DB["CloudApp"] is QuantApp.Kernel.Adapters.SQL.PostgresDataSetAdapter)
            {
                searchString += "LIMIT 1";
                targetString = "*";
            }

            _fileRepositoryDataTable = Database.DB["CloudApp"].GetDataTable(_fileReposityTableName, targetString, searchString);
            DataRowCollection rows = _fileRepositoryDataTable.Rows;

            QuantApp.Kernel.User user = QuantApp.Kernel.User.FindUser(userID);
            FilePermission file = new FilePermission(id, name, timestamp, user, data, type);

            Group group = Group.FindGroup(groupID);
            if (group != null)
            {
                group.Add(file, typeof(FilePermission), AccessType.Write);
            }

            DataRow r = _fileRepositoryDataTable.NewRow();
            r["ID"] = id;
            r["Data"] = System.Convert.ToBase64String(data);
            r["Type"] = type;
            r["Name"] = name;
            // r["Description"] = description;
            r["Timestamp"] = timestamp;
            r["UserID"] = userID;
            rows.Add(r);
            Database.DB["CloudApp"].UpdateDataTable(_fileRepositoryDataTable);
        }
    }
}
