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

using QuantApp.Kernel.Factories;

namespace QuantApp.Kernel
{
    public enum AccessType
    {
        Invited = -2, Denied = -1, View = 0, Read = 1, Write = 2
    };

    public struct UserData
    {
        public string ID;
        public string Email;
        public string FirstName;
        public string LastName;
    }

    public class User : IEquatable<User>, IPermissible
    {
        public static IUserFactory Factory = null;

        private static User _currentUser = null;
        public static User CurrentUser
        {
            get
            {
                return _currentUser;
            }
            set
            {
                if (_currentUser == null)
                    _currentUser = value;
                else
                    throw new Exception("User already set");
            }
        }

        public UserData ToUserData()
        {
            return new UserData{ ID = this.ID, FirstName = this.FirstName, LastName = this.LastName, Email = this.Email };
        }

        private static System.Collections.Concurrent.ConcurrentDictionary<int,UserData> _usersThread = new System.Collections.Concurrent.ConcurrentDictionary<int, UserData>();
        public static UserData ContextUser
        {
            get
            {
                var tid = System.Threading.Thread.CurrentThread.ManagedThreadId;
                if(_usersThread.ContainsKey(tid))
                    return _usersThread[tid];
                else
                {
                    return new UserData();
                }
            }
            set
            {
                var tid = System.Threading.Thread.CurrentThread.ManagedThreadId;
                if(_usersThread.ContainsKey(tid))
                    _usersThread[tid] = value;
                else
                    _usersThread.TryAdd(tid, value);
            }
        }

        public static UserData GetContextUser()
        {
            return ContextUser;
        }

        public delegate AccessType PermissionContextDelegate(string userID, string groupID);
        public static PermissionContextDelegate PermissionContextFunction = null;
        public static AccessType PermissionContext(string groupID)
        {
            if(PermissionContextFunction == null)
            {
                var group = QuantApp.Kernel.Group.FindGroup(groupID);
                if(group == null)
                    return AccessType.Denied;

                return group.PermissionContext();
            }
            
            return PermissionContextFunction(ContextUser.ID, groupID);
        }


        public static UserData ContextUserBySecret(string secret)
        {
            return FindUserBySecret(secret).ToUserData();
        }

        public void SetContextUser()
        {
            ContextUser = this.ToUserData();
        }

        public static void SetContextUserSecret(string secret)
        {
            ContextUser = FindUserBySecret(secret).ToUserData();
        }

        public override string ToString()
        {
            return ID;
        }

        private string _id;

        public User(string id)
        {
            this._id = id;
        }

        public User(string id, string firstName, string lastName, string email, string meta, string secret)
        {
            this._id = id;
            this._firstName = firstName;
            this._lastName = lastName;
            this._email = email;
            this._meta = meta;
            this._secret = secret;
        }

        public string ID
        {
            get
            {
                return this._id;
            }
            private set
            {
                this._id = value;
            }
        }

        private string _firstName = null;
        public string FirstName
        {
            get
            {
                if(_firstName != null)
                    return _firstName;

                if (Factory == null)
                    return null;

                _firstName = Factory.GetProperty<string>(this, "FirstName");

                return _firstName;
            }
            set
            {
                _firstName = value;

                if (Factory != null)
                    Factory.SetProperty(this, "FirstName", value);
            }
        }

        private string _lastName = null;
        public string LastName
        {
            get
            {
                if(_lastName != null)
                    return _lastName;

                if (Factory == null)
                    return null;

                _lastName = Factory.GetProperty<string>(this, "LastName");

                return _lastName;
            }
            set
            {
                _lastName = value;
                if (Factory != null)
                    Factory.SetProperty(this, "LastName", value);
            }
        }

        private string _email = null;
        public string Email
        {
            get
            {
                if(_email != null)
                    return _email;

                if (Factory == null)
                    return null;

                _email = Factory.GetProperty<string>(this, "Email");
                return _email;
            }
            set
            {
                _email = value;
                if (Factory != null)
                    Factory.SetProperty(this, "Email", value);
            }
        }

        private string _secret = null;
        public string Secret
        {
            get
            {
                if(_secret != null)
                    return _secret;

                if (Factory == null)
                    return null;

                _secret = Factory.GetProperty<string>(this, "Secret");
                return _secret;
            }
            set
            {
                _secret = value;
                if (Factory != null)
                    Factory.SetProperty(this, "Secret", value);
            }
        }

        private string _meta = null;
        public string MetaData
        {
            get
            {
                if(_meta != null)
                    return _meta;

                if (Factory == null)
                    return null;

                _meta = Factory.GetProperty<string>(this, "MetaData");
                return _meta;
            }
            set
            {
                _meta = value;

                if (Factory != null)
                    Factory.SetProperty(this, "MetaData", value);
            }
        }

        public string GetProperty(string name)
        {
            if (Factory == null)
                return null;

            return Factory.GetProperty<string>(this, name);
        }


        public void SetProperty(string name, string value)
        {
            if (Factory != null)
                Factory.SetProperty(this, name, value);
        }

        public Group Group
        {
            get
            {
                if (Factory == null)
                    return null;

                string groupID = Factory.GetProperty<string>(this, "GroupID");
                Group group = Group.FindGroup(groupID);
                if (group == null)
                {
                    group = Group.CreateGroup("Personal: " + Email);
                    group.Add(this, typeof(QuantApp.Kernel.User), AccessType.Write);

                    Factory.SetProperty(this, "GroupID", group.ID);
                }

                return group;
            }
        }

        public string PermissibleID
        {
            get
            {
                return ID;
            }
        }

        public bool VerifyPassword(string password)
        {
            if (Factory == null)
                return false;

            return Factory.VerifyPassword(this, password);
        }

        private bool _isSecure = false;
        public Boolean IsSecure()
        {
            return _isSecure;
        }

        public void SetSecure(bool level)
        {
            _isSecure = level;
        }

        public AccessType Permission(IPermissible permissible)
        {
            if (Factory == null)
                return AccessType.Write;

            if (ID == "System")
                return AccessType.Write;

            return Factory.Permission(this, permissible);
        }

        public List<Group> Groups(bool aggregated)
        {
            if (Factory == null)
                return null;

            return Factory.Groups(this, aggregated);
        }

        public List<Group> MasterGroups()
        {
            if (Factory == null)
                return null;

            return Factory.MasterGroups(this);
        }

        public List<Group> Groups(AccessType type, bool aggregated)
        {
            if (Factory == null)
                return null;

            return Factory.Groups(this, type, aggregated);
        }

        public List<Group> MasterGroups(AccessType type)
        {
            if (Factory == null)
                return null;

            return Factory.MasterGroups(this, type);
        }


        public string GetData(Group group, string type)
        {
            M m = M.Base("--" + type + "-" + this.ID);

            string data = "";
            var res = m[x => M.V<string>(x, "ID") == group.ID];
            if (res.Count > 0)
            {
                var o = res.FirstOrDefault();
                var st = Newtonsoft.Json.JsonConvert.SerializeObject(o);
                return st;
            }

            return data;
        }

        public class Entry
        {
            public string ID { get; set; }
            public string Value { get; set; }
        }

        public bool SaveData(Group group, string type, string value)
        {
            try
            {
                M m = M.Base("--" + type + "-" + this.ID);
                var res = m[x => M.V<string>(x, "ID") == group.ID];

                bool addNew = true;
                foreach (object o in res)
                {
                    addNew = false;
                    m.Exchange(o, new Entry { ID = group.ID, Value = value });
                }

                if (addNew)
                    m += new Entry { ID = group.ID, Value = value };

                m.Save();

                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Notify(Group group, string value)
        {
            string queue = this.GetData(group, "Notifications");
            if (queue == null || queue == "")
                queue = "[]";

            var data = new List<dynamic>(Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(queue));

            string text = "{'title' : '" + value + "', 'completed' : false }";
            data.Insert(0, Newtonsoft.Json.Linq.JObject.Parse(text));

            string newData = Newtonsoft.Json.JsonConvert.SerializeObject(data);
            this.SaveData(group, "Notifications", newData);

        }

        public static User CreateUser(string firstName, string lastName, string email)
        {
            if (Factory == null)
                return null;

            return Factory.CreateUser(firstName, lastName, email);
        }

        public static User FindUser(string id)
        {
            if (Factory == null)
                return null;

            return Factory.FindUser(id);
        }

        public static User FindUserBySecret(string key)
        {
            if (Factory == null)
                return null;

            return Factory.FindUserBySecret(key);
        }

        public static List<User> Users()
        {
            if (Factory == null)
                return null;

            return Factory.Users();
        }

        public bool Equals(User other)
        {
            if (((object)other) == null)
                return false;
            return ID == other.ID;
        }
        public override bool Equals(object other)
        {
            try { return Equals((User)other); }
            catch { return false; }
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        public static bool operator ==(User x, User y)
        {
            if (((object)x) == null && ((object)y) == null)
                return true;
            else if (((object)x) == null)
                return false;

            return x.Equals(y);
        }
        public static bool operator !=(User x, User y)
        {
            return !(x == y);
        }
    }
}
