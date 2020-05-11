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


using System.Security.Principal;
using System.Security.Claims;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;


namespace CoFlows.Server.Utils
{
    public static class UserExtensions
    {
        public static string QID(this IPrincipal user)
        {
            if (user == null)
                return null;

            var identity = user.Identity as ClaimsIdentity;
            if (identity != null)
            {
                var tenantClaim = identity.Claims.SingleOrDefault(c => c.Type.Equals("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", StringComparison.OrdinalIgnoreCase));

                if (tenantClaim != null && !string.IsNullOrEmpty(tenantClaim.Value))
                    return tenantClaim.Value;

                else if (user.Identity.Name != null && user.Identity.Name.StartsWith("QuantAppSecure_"))
                    return user.Identity.Name;
            }

            return null;
        }
    }

    public class UserRepository
    {
        private static DataTable _userTable = null;
        private static DataTable UserTable
        {
            get
            {
                if (_userTable == null)
                {
                    string tableName = "Users";
                    string searchString = null;
                    string targetString = null;
                    _userTable = Database.DB["CloudApp"].GetDataTable(tableName, targetString, searchString);
                }

                return _userTable;
            }
        }

        private static Dictionary<string, User> _users = new Dictionary<string, User>();
        public static User RetrieveUser(string nameIdentifier, string identityProvider)
        {

            if (string.IsNullOrWhiteSpace(nameIdentifier) || string.IsNullOrWhiteSpace(identityProvider))
            {
                return null;
            }

            try
            {
                string key = nameIdentifier + identityProvider;

                if (_users.ContainsKey(key))
                    return _users[key];

                _userTable = null;
                DataRowCollection rows = UserTable.Rows;

                var lrows = from lrow in new LINQList<DataRow>(rows)
                            where (string)lrow[(identityProvider == "QuantAppSecure" ? "TenantName" : "NameIdentifier")] == nameIdentifier && (string)lrow["IdentityProvider"] == identityProvider
                            select lrow;


                if (lrows.Count() != 0)
                {
                    foreach (DataRow row in lrows)
                    {
                        User u = new User(UserTable, row);
                        try
                        {
                            _users.Add(key, u);
                        }
                        catch { }
                        return u;
                    }
                }
                return null;
                
            }
            catch (Exception e)
            {
                throw;
            }
        }

        public static IEnumerable<User> RetrieveUsers()
        {
            try
            {
                _userTable = null;
                DataRowCollection rows = UserTable.Rows;

                var lrows = from lrow in new LINQList<DataRow>(rows)
                            select lrow;


                if (lrows.Count() != 0)
                {
                    List<User> list = new List<User>();
                    foreach (DataRow row in lrows)
                    {
                        User u = new User(UserTable, row);
                        list.Add(u);
                    }
                    return list;
                }
                return null;
            }
            catch (Exception e)
            {
                throw;
            }
        }


        public static IEnumerable<User> RetrieveUsersFromTenant(string tenant)
        {
            if (string.IsNullOrWhiteSpace(tenant))
            {
                return new User[0];
            }

            try
            {
                if (_users.ContainsKey(tenant))
                    return new List<User> { _users[tenant] };

                _userTable = null;
                DataRowCollection rows = UserTable.Rows;

                var lrows = from lrow in new LINQList<DataRow>(rows)
                            where (string)lrow["TenantName"] == tenant
                            select lrow;


                if (lrows.Count() != 0)
                {
                    List<User> list = new List<User>();
                    foreach (DataRow row in lrows)
                    {
                        User u = new User(UserTable, row);
                        try
                        {
                            _users.Add(tenant, u);
                            list.Add(u);
                        }
                        catch { }
                    }
                    return list;
                }
                return null;
            }
            catch (Exception e)
            {
                throw;
            }
        }

        public static User CreateUser(string nameIdentifier, string identityProvider)
        {
            User user = RetrieveUser(nameIdentifier, identityProvider);

            if (user == null)
            {
                DataRow row = UserTable.NewRow();
                row["NameIdentifier"] = nameIdentifier;
                row["IdentityProvider"] = identityProvider;

                UserTable.Rows.Add(row);
                Database.DB["CloudApp"].UpdateDataTable(UserTable);

                return new User(UserTable, row);
            }
            else
                return user;
        }
        
        public static List<DateTime> UserLoginHistory(string userID)
        {
            string tableName = "UserLoginRepository";
            string searchString = "UserID LIKE '" + userID + "' ORDER BY Timestamp DESC";
            string targetString = null;
            DataTable _dataTable = Database.DB["CloudApp"].GetDataTable(tableName, targetString, searchString);

            DataRowCollection rows = _dataTable.Rows;

            List<DateTime> result = new List<DateTime>();

            if (rows.Count != 0)
            {
                foreach (DataRow row in rows)
                    result.Add((DateTime)row["Timestamp"]);
            }

            return result;
        }

        public static Dictionary<string, List<string>> LastUserLogins()
        {
            string tableName = "(SELECT UserID,Timestamp, IP, Rank() over (Partition BY UserID ORDER BY Timestamp DESC ) AS Rank FROM UserLoginRepository) rs";// "UserLoginRepository";
            string searchString = "Rank = 1";
            string targetString = " UserID, Timestamp, IP";
            DataTable _dataTable = Database.DB["CloudApp"].GetDataTable(tableName, targetString, searchString);

            DataRowCollection rows = _dataTable.Rows;

            Dictionary<string, List<string>> ret = new Dictionary<string, List<string>>();

            if (rows.Count != 0)
            {
                foreach (DataRow row in rows)
                    ret.Add((string)row["UserID"], new List<string> { ((DateTime)row["Timestamp"]).ToString(), (string)row["IP"] });

            }

            return ret;
        }


        public static Dictionary<string, List<string>> LastUserLogins(Group group)
        {
            string tableName = "(SELECT UserID,Timestamp, IP, Rank() over (Partition BY UserID ORDER BY Timestamp DESC ) AS Rank FROM UserLoginRepository) rs";// "UserLoginRepository";
            string searchString = "Rank = 1 AND UserID in(SELECT PermissibleID FROM  PermissionsRepository WHERE GroupID LIKE '" + group.ID + "' AND Type LIKE 'QuantApp.Kernel.User')";// "UserID LIKE '" + userID + "' ORDER BY Timestamp DESC";
            string targetString = " UserID, Timestamp, IP";
            DataTable _dataTable = Database.DB["CloudApp"].GetDataTable(tableName, targetString, searchString);

            DataRowCollection rows = _dataTable.Rows;

            Dictionary<string, List<string>> ret = new Dictionary<string, List<string>>();

            if (rows.Count != 0)
            {
                foreach (DataRow row in rows)
                    ret.Add((string)row["UserID"], new List<string> { ((DateTime)row["Timestamp"]).ToString(), (string)row["IP"] });

            }

            return ret;
        }

        public static DateTime LastUserLogin(string userID)
        {
            string tableName = "UserLoginRepository";
            string searchString = "UserID LIKE '" + userID + "' ORDER BY Timestamp DESC";
            string targetString = "TOP 1 * ";

            if(Database.DB["CloudApp"] is QuantApp.Kernel.Adapters.SQL.SQLiteDataSetAdapter || Database.DB["CloudApp"] is QuantApp.Kernel.Adapters.SQL.PostgresDataSetAdapter)
            {
                searchString = "UserID LIKE '" + userID + "' ORDER BY Timestamp DESC LIMIT 1";
                targetString = "*";
            }

            DataTable _dataTable = Database.DB["CloudApp"].GetDataTable(tableName, targetString, searchString);

            DataRowCollection rows = _dataTable.Rows;

            if (rows.Count != 0)
            {
                foreach (DataRow row in rows)
                    return (DateTime)row["Timestamp"];
            }

            return DateTime.MinValue;
        }


        public static void AddLoginStamp(string userID, DateTime timestamp, string ip)
        {
            if (!string.IsNullOrWhiteSpace(userID))
            {
                string tableName = "UserLoginRepository";
                string searchString = "UserID LIKE '" + userID + "' AND Timestamp = '" + timestamp.ToString("yyyy/MM/dd HH:mm:ss.fff") + "'";
                string targetString = null;
                DataTable _dataTable = Database.DB["CloudApp"].GetDataTable(tableName, targetString, searchString);

                DataRowCollection rows = _dataTable.Rows;

                if (rows.Count == 0)
                {
                    DataRow r = _dataTable.NewRow();
                    r["Timestamp"] = timestamp;
                    r["UserID"] = userID;
                    r["IP"] = ip;
                    rows.Add(r);
                    Database.DB["CloudApp"].UpdateDataTable(_dataTable);
                }
            }
        }

        public static void RemoveLoginStamp(string userID, DateTime timestamp)
        {
            string tableName = "UserLoginRepository";
            string searchString = "UserID LIKE '" + userID + "' ORDER BY Timestamp DESC";
            string targetString = null;
            DataTable _dataTable = Database.DB["CloudApp"].GetDataTable(tableName, targetString, searchString);

            DataRowCollection rows = _dataTable.Rows;

            var lrows = from lrow in new LINQList<DataRow>(rows)
                        where (DateTime)lrow["Timestamp"] == timestamp
                        select lrow;

            DataRow[] frows = lrows.ToArray();

            foreach (DataRow row in frows)
                row.Delete();

            Database.DB["CloudApp"].UpdateDataTable(_dataTable);
        }

    
        public static Dictionary<DateTime, string> UserHistory(string userID)
        {
            string tableName = "UserHistoryRepository";
            string searchString = "UserID LIKE '" + userID + "' ORDER BY Timestamp DESC";
            string targetString = null;
            DataTable _dataTable = Database.DB["CloudApp"].GetDataTable(tableName, targetString, searchString);

            DataRowCollection rows = _dataTable.Rows;

            Dictionary<DateTime, string> result = new Dictionary<DateTime, string>();

            if (rows.Count != 0)
            {
                foreach (DataRow row in rows)
                    result.Add((DateTime)row["Timestamp"], (string)row["Url"]);
            }

            return result;
        }

        public static List<string> LastUserHistory(string userID)
        {
            string tableName = "UserHistoryRepository";
            string searchString = "UserID LIKE '" + userID + "' ORDER BY Timestamp DESC";
            string targetString = "TOP 1 *";
            
            if(Database.DB["CloudApp"] is QuantApp.Kernel.Adapters.SQL.SQLiteDataSetAdapter || Database.DB["CloudApp"] is QuantApp.Kernel.Adapters.SQL.PostgresDataSetAdapter)
            {
                searchString = "UserID LIKE '" + userID + "' ORDER BY Timestamp DESC LIMIT 1";
                targetString = "*";
            }
            
            DataTable _dataTable = Database.DB["CloudApp"].GetDataTable(tableName, targetString, searchString);

            DataRowCollection rows = _dataTable.Rows;

            Dictionary<DateTime, string> result = new Dictionary<DateTime, string>();

            if (rows.Count != 0)
            {
                foreach (DataRow row in rows)
                    return new List<string> { ((DateTime)row["Timestamp"]).ToString(), (string)row["IP"] };
            }

            return null;
        }


        public static void AddHistoryStamp(string userID, DateTime timestamp, string url, string ip)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(userID))
                {
                    string tableName = "UserHistoryRepository";
                    string searchString = "UserID LIKE '" + userID + "' ORDER BY Timestamp DESC";
                    string targetString = "TOP 1 *";
                    if(Database.DB["CloudApp"] is QuantApp.Kernel.Adapters.SQL.SQLiteDataSetAdapter || Database.DB["CloudApp"] is QuantApp.Kernel.Adapters.SQL.PostgresDataSetAdapter)
                    {
                        searchString = "UserID LIKE '" + userID + "' ORDER BY Timestamp DESC LIMIT 1";
                        targetString = "*";
                    }
                    
                    DataTable _dataTable = Database.DB["CloudApp"].GetDataTable(tableName, targetString, searchString);

                    DataRowCollection rows = _dataTable.Rows;

                    var lrows = from lrow in new LINQList<DataRow>(rows)
                                where (DateTime)lrow["Timestamp"] == timestamp
                                select lrow;

                    if (lrows.Count() == 0)
                    {
                        DataRow r = _dataTable.NewRow();
                        r["Timestamp"] = timestamp;
                        r["Url"] = url;
                        r["UserID"] = userID;
                        r["IP"] = ip;
                        rows.Add(r);
                        Database.DB["CloudApp"].UpdateDataTable(_dataTable);
                    }
                }
            }
            catch { }
        }

        public static void RemoveHistoryStamp(string userID, DateTime timestamp)
        {
            string tableName = "UserHistoryRepository";
            string searchString = "UserID LIKE '" + userID + "' ORDER BY Timestamp DESC";
            string targetString = null;
            DataTable _dataTable = Database.DB["CloudApp"].GetDataTable(tableName, targetString, searchString);

            DataRowCollection rows = _dataTable.Rows;

            var lrows = from lrow in new LINQList<DataRow>(rows)
                        where (DateTime)lrow["Timestamp"] == timestamp
                        select lrow;

            DataRow[] frows = lrows.ToArray();

            foreach (DataRow row in frows)
                row.Delete();

            Database.DB["CloudApp"].UpdateDataTable(_dataTable);
        }
    }
}