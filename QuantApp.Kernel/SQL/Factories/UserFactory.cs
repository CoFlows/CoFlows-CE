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
using System.Text;

using System.Data;
using System.Security.Cryptography;

using QuantApp.Kernel.Factories;

namespace QuantApp.Kernel.Adapters.SQL.Factories
{
    public class SQLUserFactory : IUserFactory
    {
        private Dictionary<string, Group> roles = null;
        private Dictionary<int, AccessType> instruments = new Dictionary<int, AccessType>();

        public void SetProperty(User user, string name, object value)
        {
            string tableName = userTableName;
            string searchString = "TenantName = '" + user.ID + "'";
            string targetString = null;
            DataTable table = Database.DB["CloudApp"].GetDataTable(tableName, targetString, searchString);

            if (table != null)
            {
                DataRowCollection rows = table.Rows;
                if (rows.Count == 0)
                    return;

                DataRow row = rows[0];
                if (row[name] != value)
                {
                    row[name] = value;
                    Database.DB["CloudApp"].UpdateDataTable(table);
                }
            }
        }

        public T GetProperty<T>(User user, string name)
        {
            object res = null;
            string tableName = userTableName;
            string searchString = "TenantName = '" + user.ID + "'";
            string targetString = null;
            DataTable table = Database.DB["CloudApp"].GetDataTable(tableName, targetString, searchString);
            
            DataRowCollection rows = table.Rows;
            
            DataRow row = rows[0];
        
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

            object obj = row[name];
            if (obj is DBNull)
                return (T)res;
            return (T)obj;
        }


        protected object GetValue(DataRow row, string columnname, Type type)
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

        private static Dictionary<string, User> _userDB = new Dictionary<string, User>();

        string userTableName = "Users";
        string permissionTableName = "PermissionsRepository";

        public readonly static object objLock_findUser = new object();
        public User FindUser(string user)
        {
            // lock (objLock_findUser)
            {
                if (user == null)
                    return null;

                if (_userDB.ContainsKey(user))
                    return _userDB[user];

                string tableName = userTableName;
                string searchString = "TenantName = '" + user + "'";
                string targetString = null;
                DataTable table = Database.DB["CloudApp"].GetDataTable(tableName, targetString, searchString);
                DataRowCollection rows = table.Rows;

                if (rows.Count != 0)
                {
                    foreach (DataRow row in rows)
                    {
                        var firstName = GetValue(row, "FirstName", typeof(string)) as string;
                        var lastName = GetValue(row, "LastName", typeof(string)) as string;
                        var email = GetValue(row, "Email", typeof(string)) as string;
                        var meta = GetValue(row, "MetaData", typeof(string)) as string;
                        var secret = GetValue(row, "Secret", typeof(string)) as string;

                        User u = new User(user, firstName, lastName, email, meta, secret);
                        _userDB.Add(user, u);
                        return u;
                    }
                }

                // if (user.StartsWith("QuantAppSecure:"))
                //     return new User(user);

                return null;
            }
        }

        public readonly static object objLock_findUserSecret = new object();
        public User FindUserBySecret(string key)
        {
            // lock (objLock_findUserSecret)
            {
                string tableName = userTableName;
                string searchString = "Secret = '" + key + "'";
                string targetString = null;

                DataTable table = Database.DB["CloudApp"].GetDataTable(tableName, targetString, searchString);
                
                DataRowCollection rows = table.Rows;

                if (rows != null && rows.Count != 0)
                {
                    foreach (DataRow row in rows)
                    {
                        var user = GetValue(row, "TenantName", typeof(string)) as string;
                        var firstName = GetValue(row, "FirstName", typeof(string)) as string;
                        var lastName = GetValue(row, "LastName", typeof(string)) as string;
                        var email = GetValue(row, "Email", typeof(string)) as string;
                        var meta = GetValue(row, "MetaData", typeof(string)) as string;
                        var secret = GetValue(row, "Secret", typeof(string)) as string;

                        User u = new User(user, firstName, lastName, email, meta, secret);
                        // User u = new User(user);
                        if(!_userDB.ContainsKey(user))
                            _userDB.Add(user, u);
                        return u;
                    }
                }

                return null;
            }
        }
        public List<User> Users()
        {
            string tableName = userTableName;
            string searchString = null;
            string targetString = null;
            DataTable table = Database.DB["CloudApp"].GetDataTable(tableName, targetString, searchString);
            DataRowCollection rows = table.Rows;

            if (rows.Count != 0)
            {
                foreach (DataRow row in rows)
                {
                    User u = FindUser((string)row["TenantName"]);
                }
            }

            return new List<User>(_userDB.Values);
        }

        // public readonly static object objLock = new object();

        public List<Group> Groups(User user, bool aggregated)
        { 
            roles = _Groups(user);

            List<Group> result = new List<Group>();
            foreach (Group role in roles.Values)
            {
                List<Group> sg = role.SubGroups(aggregated);
                if (sg != null)
                    foreach (Group g in sg)
                        if (!result.Contains(g) && g.Permission(user, user) != AccessType.Denied)
                            result.Add(g);


                if (!result.Contains(role) && role.Permission(user, user) != AccessType.Denied)
                    result.Add(role);
            }

            return result;
        }

        public List<Group> MasterGroups(User user)
        {
            roles = _Groups(user);
            return new List<Group>((from r in roles.Values where r.Parent == null select r).ToList());
        }

        public readonly static object objLock_Permission = new object();
        public AccessType Permission(User user, IPermissible permissible)
        {
            lock (objLock_Permission)
            {
                AccessType t = AccessType.Denied;

                List<Group> roles = user.Groups(true);

                foreach (Group r in roles)
                {
                    AccessType gt = r.Permission(user, permissible);

                    gt = t > gt ? AccessType.Denied : gt;
                    t = (AccessType)Math.Max((int)gt, (int)t);

                    if (r.ID == "Administrator")
                    {
                        gt = r.Permission(user, user);
                        t = gt != AccessType.Denied ? AccessType.Write : AccessType.Denied;

                        if (t == AccessType.Write)
                            return t;
                    }
                }

                return t;
            }
        }

        public List<Group> Groups(User user, AccessType type, bool aggregated)
        {
            roles = _Groups(user);

            List<Group> result = new List<Group>();
            foreach (Group role in roles.Values)
            {
                List<Group> sg = role.SubGroups(aggregated);
                if (sg != null)
                    foreach (Group g in sg)
                        if (!result.Contains(g) && g.Permission(user) == type)
                            result.Add(g);

                if (!result.Contains(role) && role.Permission(user) == type)
                    result.Add(role);
            }
            return result;
        }

        public List<Group> MasterGroups(User user, AccessType type)
        {
            roles = _Groups(user);

            List<Group> result = new List<Group>();
            foreach (Group role in roles.Values)
                if (role.Parent == null && role.Permission(user) == type)
                    result.Add(role);

            return result;
        }

        protected Dictionary<string, Group> _Groups(User user)
        {
            string tableName = permissionTableName;
            string searchString = "PermissibleID = '" + user.PermissibleID + "' AND AccessType > -1";
            string targetString = null;
            DataTable table = Database.DB["CloudApp"].GetDataTable(tableName, targetString, searchString);
            DataRowCollection rows = table.Rows;

            Dictionary<string, Group> result = new Dictionary<string, Group>();
            if (rows.Count != 0)
            {
                foreach (DataRow row in rows)
                {
                    string groupName = (string)row["GroupID"];
                    Group role = Group.FindGroup(groupName);
                    if(role != null)
                    {
                        if (!result.ContainsKey(role.ID))
                            result.Add(role.ID, role);
                    }
                }
            }

            Group public_role = Group.FindGroup("Public");
            if (!result.ContainsKey(public_role.ID))
                result.Add(public_role.ID, public_role);

            return result;
        }

        public void Reset()
        {
            roles = null;
            instruments = new Dictionary<int, AccessType>();
        }

        public Boolean VerifyPassword(User user, string password)
        {
            string tableName = userTableName;
            string searchString = "TenantName = '" + user.ID + "'";
            string targetString = null;
            DataTable table = Database.DB["CloudApp"].GetDataTable(tableName, targetString, searchString);
            DataRowCollection rows = table.Rows;


            string hash = GetMd5Hash(password);
            string pass = (string)GetValue(rows[0], "Hash", typeof(string));

            return hash == pass;
        }

        public static string GetMd5Hash(string input)
        {
            MD5 md5Hash = MD5.Create();
            // Convert the input string to a byte array and compute the hash. 
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes 
            // and create a string.
            StringBuilder sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data  
            // and format each one as a hexadecimal string. 
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }
            
            return sBuilder.ToString();
        }

        // Verify a hash against a string. 
        public static bool VerifyMd5Hash(string input, string hash)
        {
            MD5 md5Hash = MD5.Create();

            // Hash the input. 
            string hashOfInput = GetMd5Hash(input);

            // Create a StringComparer an compare the hashes.
            StringComparer comparer = StringComparer.OrdinalIgnoreCase;

            if (0 == comparer.Compare(hashOfInput, hash))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public User CreateUser(string firstName, string lastName, string email)
        {
            string tableName = userTableName;
            string searchString = "Email LIKE '" + email + "'";
            string targetString = null;
            DataTable table = Database.DB["CloudApp"].GetDataTable(tableName, targetString, searchString);
            DataRowCollection rows = table.Rows;

            if (rows.Count == 0)
            {
                DataRow r = table.NewRow();
                string id = System.Guid.NewGuid().ToString();
                r["TenantName"] = System.Guid.NewGuid().ToString();
                r["FirstName"] = firstName;
                r["LastName"] = lastName;
                r["Email"] = email;
                rows.Add(r);
                Database.DB["CloudApp"].UpdateDataTable(table);

                // return new User(id);

                return new User(id, (string)r["FirstName"], (string)r["LastName"], (string)r["Email"], null, null);
            }

            return null;
        }

        public void Remove(User user)
        {
            string tableName = userTableName;
            string searchString = "TenantName = '" + user.ID + "'";
            string targetString = null;
            DataTable table = Database.DB["CloudApp"].GetDataTable(tableName, targetString, searchString);
            DataRowCollection rows = table.Rows;
            
            foreach (DataRow row in rows)
                row.Delete();

            Database.DB["CloudApp"].UpdateDataTable(table);
        }

    }

}
