/*
 * The MIT License (MIT)
 * Copyright (c) Arturo Rodriguez All rights reserved.
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;

using System.Data;

using QuantApp.Kernel.Factories;

namespace QuantApp.Kernel.Adapters.SQL.Factories
{
    public class SQLGroupFactory : IGroupFactory
    {
        string groupTableName = "Roles";
        string permissionTableName = "PermissionsRepository";

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

        public void SetProperty(Group group, string name, object value)
        {
            string tableName = groupTableName;
            string searchString = "ID = '" + group.ID + "'";
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

        public T GetProperty<T>(Group group, string name)
        {
            object res = null;
            string tableName = groupTableName;
            string searchString = "ID = '" + group.ID + "'";
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

            if (typeof(T) == typeof(string))
                return (T)(object)(obj.ToString());

            return (T)obj;
        }

        public Group FindGroup(User user, string groupID)
        {
            if (groupID == null)
                return null;

            string key = user.ID + groupID;

            if (_groupDBID.ContainsKey(key))
            {
                if (_groupDBID[key].Permission(user, user) == AccessType.Denied)
                    return null;
                return _groupDBID[key];
            }

            string tableName = groupTableName;
            string searchString = "ID = '" + groupID + "'";
            string targetString = null;
            DataTable table = Database.DB["CloudApp"].GetDataTable(tableName, targetString, searchString);
            DataRowCollection rows = table.Rows;

            if (rows.Count != 0)
            {
                foreach (DataRow row in rows)
                {
                    Group r = new Group(groupID);
                    if (r.Permission(user) != AccessType.Denied)
                    {
                        try
                        {
                            _groupDBID.TryAdd(key, r);
                        }
                        catch { }
                        return r;

                    }
                }
            }
            

            return null;
        }


        public Group FindGroupByName(User user, string groupName)
        {
            if (groupName == null)
                return null;

            string key = user.ID + groupName;

            if (_groupDBName.ContainsKey(key))
            {
                if (_groupDBName[key].Permission(user, user) == AccessType.Denied)
                    return null;
                return _groupDBName[key];
            }

            string tableName = groupTableName;
            string searchString = "Name LIKE '" + groupName + "'";
            string targetString = null;
            DataTable table = Database.DB["CloudApp"].GetDataTable(tableName, targetString, searchString);
            DataRowCollection rows = table.Rows;

            if (rows.Count != 0)
            {
                foreach (DataRow row in rows)
                {
                    string groupID = (string)row["ID"];
                    Group r = FindGroup(user, groupID);
                    if (r != null)
                        return r;
                }
            }
            

            return null;
        }



        public List<Group> Groups(User user, IPermissible permissible, AccessType type)
        {

            string tableName = permissionTableName;
            string searchString = "AccessType = " + (int)type + "' AND PermissibleID = '" + permissible.PermissibleID + "'";
            string targetString = null;
            DataTable table = Database.DB["CloudApp"].GetDataTable(tableName, targetString, searchString);
            DataRowCollection rows = table.Rows;

            List<Group> result = new List<Group>();

            if (rows.Count != 0)
            {
                foreach (DataRow row in rows)
                {
                    string id = (string)row["GroupID"];
                    result.Add(FindGroup(user, id));
                }
            }

            return result;
        }



        public List<Group> Groups(User user, IPermissible permissible)
        {
            string tableName = permissionTableName;
            string searchString = "PermissibleID = '" + permissible.PermissibleID + "'";
            string targetString = null;
            DataTable table = Database.DB["CloudApp"].GetDataTable(tableName, targetString, searchString);
            DataRowCollection rows = table.Rows;

            List<Group> result = new List<Group>();

            if (rows.Count != 0)
            {
                foreach (DataRow row in rows)
                {
                    string id = (string)row["GroupID"];
                    result.Add(FindGroup(user, id));
                }
            }

            return result;
        }

        public List<Group> SubGroups(User user, Group group, bool aggregated)
        {
            string key = user.ID + group.ID + aggregated;

            if (_subGroupListDB.ContainsKey(key))
                return _subGroupListDB[key];

            string tableName = groupTableName;
            string searchString = "Parent = '" + group.ID + "' ORDER BY URL";
            string targetString = null;
            DataTable table = Database.DB["CloudApp"].GetDataTable(tableName, targetString, searchString);
            DataRowCollection rows = table.Rows;

            List<Group> result = new List<Group>();

            AccessType ac = group.Permission(user, user);

            if (rows.Count != 0)
            {
                foreach (DataRow row in rows)
                {
                    string id = (string)row["ID"];
                    Group sg = FindGroup(user, id);
                    if (aggregated && sg != null)
                    {
                        List<Group> sgs = SubGroups(user, sg, aggregated);
                        if (sgs != null)
                            foreach (Group ssg in sgs)
                                if (ssg != null && !result.Contains(ssg) && ssg.Permission(user, user) != AccessType.Denied)
                                    result.Add(ssg);
                    }

                    if (sg != null && !result.Contains(sg) && sg.Permission(user, user) != AccessType.Denied)
                        result.Add(sg);
                }
            }

            try
            {
                _subGroupListDB.TryAdd(key, result);
            }
            catch { }

            return result;
            
        }



        public List<Group> Groups(User user)
        {
            if (_groupListDB.ContainsKey(user.ID))
                return _groupListDB[user.ID];

            string tableName = groupTableName;
            string searchString = null;
            string targetString = null;
            DataTable table = Database.DB["CloudApp"].GetDataTable(tableName, targetString, searchString);
            DataRowCollection rows = table.Rows;

            List<Group> result = new List<Group>();

            if (rows.Count != 0)
            {
                foreach (DataRow row in rows)
                {
                    string id = (string)row["ID"];
                    Group r = FindGroup(user, id);

                    if (r != null && !result.Contains(r))
                        result.Add(r);
                }
            }

            _groupListDB.TryAdd(user.ID, result);

            return result;
        }

        public List<Group> MasterGroups(User user)
        {
            List<Group> result = Groups(user);
            List<Group> masters = new List<Group>();
            foreach (Group g in result)
                if (g.Parent == null)
                    masters.Add(g);

            return masters;
        }

        public readonly static object list1Lock = new object();

        public List<IPermissible> List(User user, Group group, Type type, bool aggregated)
        {
            lock (list1Lock)
            {

                string key = user.ID + group.ID + type + aggregated;

                if (_listPermissibleTypeAccessType.ContainsKey(key))
                    return _listPermissibleTypeAccessType[key];

                List<IPermissible> result = new List<IPermissible>();

                if (aggregated)
                {
                    List<Group> subgroups = group.SubGroups(user, aggregated);
                    if (subgroups != null)
                    {
                        foreach (Group sg in subgroups)
                        {
                            List<IPermissible> ips = sg.List(user, type, false);
                            if (ips != null)
                                foreach (IPermissible ip in ips)
                                    if (!result.Contains(ip))
                                        result.Add(ip);
                        }
                    }
                }

                if (_rawPermissibleDB.Count == 0)
                {
                    string tableName = permissionTableName;
                    string searchString = null;
                    string targetString = null;
                    DataTable table = Database.DB["CloudApp"].GetDataTable(tableName, targetString, searchString);
                    DataRowCollection rows = table.Rows;

                    System.Diagnostics.Debug.WriteLine("List: " + user + " " + group + " " + type);

                    if (rows.Count != 0)
                        foreach (DataRow row in rows)
                        {
                            string gid = (string)row["GroupID"];
                            if (!_rawPermissibleDB.ContainsKey(gid))
                                _rawPermissibleDB.TryAdd(gid, new ConcurrentDictionary<string, AccessType>());
                            if (!_rawPermissibleTypeDB.ContainsKey(gid))
                                _rawPermissibleTypeDB.TryAdd(gid, new ConcurrentDictionary<string, string>());

                            _rawPermissibleDB[gid].TryAdd((string)row["PermissibleID"], (AccessType)row["AccessType"]);
                            _rawPermissibleTypeDB[gid].TryAdd((string)row["PermissibleID"], (string)row["Type"]);
                        }
                }
                else
                {

                    if (_rawPermissibleTypeDB.ContainsKey(group.ID))
                    {
                        foreach (string pid in _rawPermissibleTypeDB[group.ID].Keys.ToList())
                            if (_rawPermissibleTypeDB[group.ID][pid] == type.ToString())
                            {
                                IPermissible permissible = Group.FindPermissible(user, type, pid);
                                if (permissible != null && !result.Contains(permissible))
                                    result.Add(permissible);
                            }
                    }
                }

                if (result.Count > 0)
                    try
                    {
                        _listPermissibleTypeAccessType.TryAdd(key, result);
                    }
                    catch { }

                return result;
            }
        }


        public readonly static object list2Lock = new object();
        public List<IPermissible> List(User user, Group group, Type type, AccessType accessType, bool aggregated)
        {
            lock (list2Lock)
            {

                string key = user.ID + group.ID + type + accessType + aggregated;

                if (_listPermissibleTypeAccessType.ContainsKey(key))
                    return _listPermissibleTypeAccessType[key];

                List<IPermissible> result = new List<IPermissible>();

                if (aggregated)
                {
                    List<Group> subgroups = group.SubGroups(user, aggregated);
                    if (subgroups != null)
                    {
                        foreach (Group sg in subgroups)
                        {
                            List<IPermissible> ips = sg.List(type, accessType, false);
                            if (ips != null)
                                foreach (IPermissible ip in ips)
                                    if (!result.Contains(ip))
                                        result.Add(ip);
                        }
                    }
                }

                if (_rawPermissibleDB.Count == 0)
                {
                    string tableName = permissionTableName;
                    string searchString = null;
                    string targetString = null;
                    DataTable table = Database.DB["CloudApp"].GetDataTable(tableName, targetString, searchString);
                    DataRowCollection rows = table.Rows;

                    System.Diagnostics.Debug.WriteLine("List: " + user + " " + group + " " + type);

                    if (rows.Count != 0)
                        foreach (DataRow row in rows)
                        {
                            string gid = (string)row["GroupID"];
                            if (!_rawPermissibleDB.ContainsKey(gid))
                                _rawPermissibleDB.TryAdd(gid, new ConcurrentDictionary<string, AccessType>());
                            if (!_rawPermissibleTypeDB.ContainsKey(gid))
                                _rawPermissibleTypeDB.TryAdd(gid, new ConcurrentDictionary<string, string>());

                            _rawPermissibleDB[gid].TryAdd((string)row["PermissibleID"], (AccessType)row["AccessType"]);
                            _rawPermissibleTypeDB[gid].TryAdd((string)row["PermissibleID"], (string)row["Type"]);
                        }
                }
                else
                {

                    if (_rawPermissibleTypeDB.ContainsKey(group.ID))
                    {
                        foreach (string pid in _rawPermissibleTypeDB[group.ID].Keys.ToList())
                            if (_rawPermissibleTypeDB[group.ID][pid] == type.ToString() && (int)_rawPermissibleDB[group.ID][pid] >= (int)accessType)
                            {
                                IPermissible permissible = Group.FindPermissible(user, type, pid);
                                if (permissible != null && !result.Contains(permissible))
                                    result.Add(permissible);
                            }
                    }
                }

                try
                {
                    if (result.Count > 0)
                        _listPermissibleTypeAccessType.TryAdd(key, result);
                }
                catch { }

                return result;
            }
        }

        public readonly static object permLock = new object();
        public AccessType Permission(User user, Group group, IPermissible permissible)
        {
            lock (permLock)
            {
                AccessType type = AccessType.Denied;

                if (group.Parent != null)
                {
                    AccessType pt = Permission(user, group.Parent, permissible);
                    if (pt == AccessType.Write)
                        type = AccessType.Write;
                }

                string uid = user == null ? "" : user.ID;


                if (_permissibleDB.ContainsKey(uid) && _permissibleDB[uid].ContainsKey(group.ID) && _permissibleDB[uid][group.ID].ContainsKey(permissible.PermissibleID))
                    return _permissibleDB[uid][group.ID][permissible.PermissibleID];

                if (!_permissibleDB.ContainsKey(uid))
                    _permissibleDB.TryAdd(uid, new ConcurrentDictionary<string, ConcurrentDictionary<string, AccessType>>());

                if (!_permissibleDB[uid].ContainsKey(group.ID))
                    _permissibleDB[uid].TryAdd(group.ID, new ConcurrentDictionary<string, AccessType>());



                if (user != null && user.PermissibleID == "System")
                    type = AccessType.Write;

                if (permissible == null)
                    return AccessType.Denied;

                if (type != AccessType.Write)
                {
                    type = group.ID == "Administrator" ? AccessType.Denied : FindGroup(User.CurrentUser, "Administrator").Permission(user, permissible);

                    if (type == AccessType.Write)
                    {
                        if (!_permissibleDB[uid][group.ID].ContainsKey(permissible.PermissibleID))
                            _permissibleDB[uid][group.ID].TryAdd(permissible.PermissibleID, type);
                        return type;
                    }

                    if (type == AccessType.Denied)
                    {
                        if (_rawPermissibleDB.Count == 0)
                        {
                            _rawPermissibleDB.TryAdd(group.ID, new ConcurrentDictionary<string, AccessType>());
                            _rawPermissibleTypeDB.TryAdd(group.ID, new ConcurrentDictionary<string, string>());

                            string tableName = permissionTableName;
                            string searchString = null;
                            string targetString = null;
                            DataTable table = Database.DB["CloudApp"].GetDataTable(tableName, targetString, searchString);
                            DataRowCollection rows = table.Rows;

                            System.Diagnostics.Debug.WriteLine("Permission: " + user + " " + group + " " + permissible + " " + type);

                            if (rows.Count != 0)
                                foreach (DataRow row in rows)
                                {
                                    string gid = (string)row["GroupID"];
                                    if (!_rawPermissibleDB.ContainsKey(gid))
                                        _rawPermissibleDB.TryAdd(gid, new ConcurrentDictionary<string, AccessType>());
                                    if (!_rawPermissibleTypeDB.ContainsKey(gid))
                                        _rawPermissibleTypeDB.TryAdd(gid, new ConcurrentDictionary<string, string>());

                                    _rawPermissibleDB[gid].TryAdd((string)row["PermissibleID"], (AccessType)row["AccessType"]);
                                    _rawPermissibleTypeDB[gid].TryAdd((string)row["PermissibleID"], (string)row["Type"]);
                                }
                        }


                        if (_rawPermissibleDB.ContainsKey(group.ID))
                        {
                            if (_rawPermissibleDB[group.ID].ContainsKey(permissible.PermissibleID))
                                type = _rawPermissibleDB[group.ID][permissible.PermissibleID];
                            else
                                return type;
                        }
                        else return type;


                        if (group.Parent != null)
                        {
                            AccessType pt = Permission(user, group.Parent, permissible);
                            if (pt == AccessType.Write)
                                type = AccessType.Write;
                        }
                    }

                    if (!(permissible is User))
                    {
                        AccessType gtype = group.Permission(user, user);

                        if (user != null)
                            type = type > gtype ? AccessType.Denied : gtype;
                    }

                }

                if (user == null)
                    return type;

                if (!_permissibleDB.ContainsKey(uid))
                    _permissibleDB.TryAdd(uid, new ConcurrentDictionary<string, ConcurrentDictionary<string, AccessType>>());

                if (!_permissibleDB[uid].ContainsKey(group.ID))
                    _permissibleDB[uid].TryAdd(group.ID, new ConcurrentDictionary<string, AccessType>());

                if (!_permissibleDB[uid][group.ID].ContainsKey(permissible.PermissibleID))
                    _permissibleDB[uid][group.ID].TryAdd(permissible.PermissibleID, type);
                return type;
            }
        }

        public bool Exists(Group group, IPermissible permissible)
        {
            string tableName = permissionTableName;
            string searchString = "GroupID = '" + group.ID + "' AND PermissibleID = '" + permissible.PermissibleID + "'";
            string targetString = null;
            DataTable table = Database.DB["CloudApp"].GetDataTable(tableName, targetString, searchString);
            DataRowCollection rows = table.Rows;

            return rows.Count != 0;
        }

        private ConcurrentDictionary<string, ConcurrentDictionary<string, ConcurrentDictionary<string, AccessType>>> _permissibleDB = new ConcurrentDictionary<string, ConcurrentDictionary<string, ConcurrentDictionary<string, AccessType>>>();
        private ConcurrentDictionary<string, ConcurrentDictionary<string, AccessType>> _rawPermissibleDB = new ConcurrentDictionary<string, ConcurrentDictionary<string, AccessType>>();
        private ConcurrentDictionary<string, ConcurrentDictionary<string, string>> _rawPermissibleTypeDB = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>();
        private ConcurrentDictionary<string, Group> _groupDBID = new ConcurrentDictionary<string, Group>();
        private ConcurrentDictionary<string, Group> _groupDBName = new ConcurrentDictionary<string, Group>();
        private ConcurrentDictionary<string, List<Group>> _subGroupListDB = new ConcurrentDictionary<string, List<Group>>();
        private ConcurrentDictionary<string, List<Group>> _groupListDB = new ConcurrentDictionary<string, List<Group>>();
        ConcurrentDictionary<string, List<IPermissible>> _listPermissibleTypeAccessType = new ConcurrentDictionary<string, List<IPermissible>>();

        private void CleanMemory()
        {
            _permissibleDB = new ConcurrentDictionary<string, ConcurrentDictionary<string, ConcurrentDictionary<string, AccessType>>>();
            _groupDBID = new ConcurrentDictionary<string, Group>();
            _groupDBName = new ConcurrentDictionary<string, Group>();
            _subGroupListDB = new ConcurrentDictionary<string, List<Group>>();
            _groupListDB = new ConcurrentDictionary<string, List<Group>>();
            _listPermissibleTypeAccessType = new ConcurrentDictionary<string, List<IPermissible>>();
            _rawPermissibleDB = new ConcurrentDictionary<string, ConcurrentDictionary<string, AccessType>>();
            _rawPermissibleTypeDB = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>();
        }

        public void Add(Group group, IPermissible permissible, Type type, AccessType accessType)
        {
            CleanMemory();
            string tableName = permissionTableName;
            string searchString = "GroupID = '" + group.ID + "' AND PermissibleID = '" + permissible.PermissibleID + "'";
            string targetString = null;
            DataTable table = Database.DB["CloudApp"].GetDataTable(tableName, targetString, searchString);
            DataRowCollection rows = table.Rows;

            if (rows.Count == 0)
            {
                DataRow r = table.NewRow();
                r["PermissibleID"] = permissible.PermissibleID;
                r["GroupID"] = group.ID;
                r["Type"] = type.ToString();
                r["AccessType"] = (int)accessType;

                rows.Add(r);
                Database.DB["CloudApp"].UpdateDataTable(table);
            }
            else
            {
                rows[0]["AccessType"] = (int)accessType;
                Database.DB["CloudApp"].UpdateDataTable(table);
            }
        }

        public void Remove(Group group, IPermissible permissible)
        {
            CleanMemory();
            string tableName = permissionTableName;
            string searchString = "GroupID = '" + group.ID + "' AND PermissibleID = '" + permissible.PermissibleID + "'";
            string targetString = null;
            DataTable table = Database.DB["CloudApp"].GetDataTable(tableName, targetString, searchString);
            DataRowCollection rows = table.Rows;

            foreach (DataRow row in rows)
                row.Delete();

            Database.DB["CloudApp"].UpdateDataTable(table);
        }

        public void Add(Group group, Group subgroup)
        {
            CleanMemory();
            subgroup.Parent = group;
        }

        public void Remove(Group group, Group subgroup)
        {
            CleanMemory();
            subgroup.Parent = null;
        }

        public void Remove(Group group)
        {
            CleanMemory();
            string tableName = permissionTableName;
            string searchString = "GroupID = '" + group.ID + "'";
            string targetString = null;
            DataTable table = Database.DB["CloudApp"].GetDataTable(tableName, targetString, searchString);
            DataRowCollection rows = table.Rows;

            foreach (DataRow row in rows)
                row.Delete();

            Database.DB["CloudApp"].UpdateDataTable(table);


            tableName = groupTableName;
            searchString = "ID = '" + group.ID + "'";
            targetString = null;
            table = Database.DB["CloudApp"].GetDataTable(tableName, targetString, searchString);
            rows = table.Rows;

            foreach (DataRow row in rows)
                row.Delete();

            Database.DB["CloudApp"].UpdateDataTable(table);
        }


        public Group CreateGroup(User user, string groupName)
        {
            CleanMemory();

            if (string.IsNullOrWhiteSpace(groupName))
                return null;
            string id = System.Guid.NewGuid().ToString();

            string tableName = groupTableName;
            string searchString = "ID = '" + id + "'";
            string targetString = null;
            DataTable table = Database.DB["CloudApp"].GetDataTable(tableName, targetString, searchString);
            DataRowCollection rows = table.Rows;

            if (rows.Count == 0)
            {
                DataRow r = table.NewRow();
                r["ID"] = id;
                r["Name"] = groupName;
                r["AccessType"] = GroupAccessType.Hidden;
                rows.Add(r);
                Database.DB["CloudApp"].UpdateDataTable(table);

                Group g = FindGroup(User.CurrentUser, id);
                g.Add(user, typeof(User), AccessType.Write);
                return g;
            }

            return null;
        }

        public Group CreateGroup(User user, string groupName, string id)
        {
            CleanMemory();

            if (string.IsNullOrWhiteSpace(groupName) || string.IsNullOrWhiteSpace(id))
                return null;
            
            string tableName = groupTableName;
            string searchString = "ID = '" + id + "'";
            string targetString = null;
            DataTable table = Database.DB["CloudApp"].GetDataTable(tableName, targetString, searchString);
            DataRowCollection rows = table.Rows;

            if (rows.Count == 0)
            {
                DataRow r = table.NewRow();
                r["ID"] = id;
                r["Name"] = groupName;
                r["AccessType"] = GroupAccessType.Hidden;
                rows.Add(r);
                Database.DB["CloudApp"].UpdateDataTable(table);

                Group g = FindGroup(User.CurrentUser, id);
                g.Add(user, typeof(User), AccessType.Write);
                return g;
            }

            return null;
        }
    }

}
