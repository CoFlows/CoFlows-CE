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

using QuantApp.Kernel.Factories;

namespace QuantApp.Kernel
{
    public enum GroupAccessType
    {
        NotSet = -100, Public = -1, Private = 0, Hidden = 1, System = 2
    };

    public class Group : IEquatable<Group>
    {
        public static IGroupFactory Factory = null;

        private string _id;

        public Group(string id)
        {
            this._id = id;
        }

        public override string ToString()
        {
            return Name + " (" + ID + ")";
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

        private Group _parent = null;
        public Group Parent
        {
            get
            {
                if (Factory == null)
                    return null;

                if (_parent == null)
                    _parent = FindGroup(Factory.GetProperty<string>(this, "Parent"));

                if (_parent == null)
                    _parent = this;

                if (_parent == this)
                    return null;

                return _parent;
            }
            set
            {
                if (Factory != null)
                    Factory.SetProperty(this, "Parent", value.ID);

                _parent = value;
            }
        }

        public Group Master
        {
            get
            {
                if (Parent == null)
                    return this;

                else if (Parent.Parent != null)
                    return Parent.Master;

                else
                    return Parent;
            }
        }

        private string _name = null;
        public string Name
        {
            get
            {
                if (Factory == null)
                    return null;

                if (_name == null)
                    _name = Factory.GetProperty<string>(this, "Name");

                return _name;
            }
            set
            {
                if (Factory != null)
                    Factory.SetProperty(this, "Name", value);

                _name = value;
            }
        }

        private string _description = null;
        public string Description
        {
            get
            {
                if (Factory == null)
                    return null;

                if (_description == null)
                    _description = Factory.GetProperty<string>(this, "Description");

                return _description;
            }
            set
            {
                if (Factory != null)
                    Factory.SetProperty(this, "Description", value);

                _description = value;
            }
        }

        private GroupAccessType _access = GroupAccessType.NotSet;
        public GroupAccessType Access
        {
            get
            {
                if (Factory == null)
                    return GroupAccessType.Public;

                if (_access == GroupAccessType.NotSet)
                    _access = (GroupAccessType)Factory.GetProperty<int>(this, "AccessType");

                return _access;
            }
            set
            {
                if (Factory != null)
                    Factory.SetProperty(this, "AccessType", value);

                _access = value;
            }
        }


        public List<Group> SubGroups(bool aggregated)
        {
            if (Factory == null)
                return null;

            return Factory.SubGroups(User.CurrentUser, this, aggregated);
        }

        public List<Group> SubGroups(User user, bool aggregated)
        {
            if (Permission(User.CurrentUser) != AccessType.Write)
                return null;

            if (Factory == null)
                return null;

            return Factory.SubGroups(user, this, aggregated);
        }

        public void Add(Group group)
        {
            if (Factory == null)
                return;

            Factory.Add(this, group);
        }

        public void Remove(Group group)
        {
            if (Factory == null)
                return;

            Factory.Remove(this, group);
        }


        public List<IPermissible> List(Type type, bool aggregated)
        {
            if (Factory == null)
                return null;

            return Factory.List(User.CurrentUser, this, type, aggregated);
        }

        public List<IPermissible> List(User user, Type type, bool aggregated)
        {
            if (Permission(User.CurrentUser) != AccessType.Write)
                return null;

            if (Factory == null)
                return null;

            AccessType ac = Permission(User.CurrentUser, user);
            List<IPermissible> res = Factory.List(user, this, type, aggregated);

            if (res != null)
                return res.Where(p => ac >= Permission(user, p)).ToList();

            return res;
        }


        public List<IPermissible> List(Type type, AccessType accessType, bool aggregated)
        {
            if (Factory == null)
                return null;

            return Factory.List(User.CurrentUser, this, type, accessType, aggregated);
        }

        public List<IPermissible> List(User user, Type type, AccessType accessType, bool aggregated)
        {
            if (Permission(User.CurrentUser) != AccessType.Write)
                return new List<IPermissible>();

            if (Factory == null)
                return new List<IPermissible>();

            return Factory.List(user, this, type, accessType, aggregated);
        }

        public AccessType Permission(IPermissible permissible)
        {
            if (Factory == null)
                return AccessType.Write;

            return Factory.Permission(User.CurrentUser, this, permissible);
        }

        public DateTime Expiry(IPermissible permissible)
        {
            if (Factory == null)
                return DateTime.MaxValue;

            return Factory.Expiry(User.CurrentUser, this, permissible);
        }

        public AccessType Permission(UserData userData)
        {
            if (Factory == null)
                return AccessType.Write;

            return Factory.Permission(User.FindUser(userData.ID), this, User.FindUser(userData.ID));
        }

        public DateTime Expiry(UserData userData)
        {
            if (Factory == null)
                return DateTime.MaxValue;

            return Factory.Expiry(User.FindUser(userData.ID), this, User.FindUser(userData.ID));
        }

        public AccessType PermissionContext()
        {
            if (Factory == null)
                return AccessType.Write;
            var quser = User.FindUser(User.ContextUser.ID);
            if(quser == null)
                return AccessType.Denied;
                
            return Factory.Permission(quser, this, quser);
        }

        public DateTime ExpiryContext()
        {
            if (Factory == null)
                return DateTime.MaxValue;
            var quser = User.FindUser(User.ContextUser.ID);
            if(quser == null)
                return DateTime.MaxValue;
                
            return Factory.Expiry(quser, this, quser);
        }

        public List<IPermissible> ListContext(Type type, bool aggregated)
        {
            if (Factory == null)
                return new List<IPermissible>();
            var quser = User.FindUser(User.ContextUser.ID);
            if(quser == null)
                return new List<IPermissible>();

            return Factory.List(quser, this, type, aggregated);
        }

        public AccessType PermissionSecret(string secret)
        {
            if (Factory == null)
                return AccessType.Write;

            var quser = User.FindUserBySecret(secret);
            if(quser == null)
                return AccessType.Denied;

            return Factory.Permission(quser, this, quser);
        }

        private ConcurrentDictionary<string, ConcurrentDictionary<string, AccessType>> _db = new ConcurrentDictionary<string, ConcurrentDictionary<string, AccessType>>();

        public AccessType Permission(User user, IPermissible permissible)
        {
            if (Permission(User.CurrentUser) != AccessType.Write)
                return AccessType.Denied;

            if (Factory == null)
                return AccessType.Write;

            string ukey = user == null ? "" : user.PermissibleID;

            if (permissible == null)
                return AccessType.Denied;

            if (_db.ContainsKey(ukey) && _db[ukey].ContainsKey(permissible.PermissibleID))
                return _db[ukey][permissible.PermissibleID];

            AccessType type = Factory.Permission(user, this, permissible);

            if (!_db.ContainsKey(ukey))
                _db.TryAdd(ukey, new ConcurrentDictionary<string, AccessType>());

            if (!_db[ukey].ContainsKey(permissible.PermissibleID))
                _db[ukey].TryAdd(permissible.PermissibleID, type);
            else
                _db[ukey][permissible.PermissibleID] = type;

            return type;
        }

        public DateTime Expiry(User user, IPermissible permissible)
        {
            return Factory.Expiry(user, this, permissible);
        }

        public bool Exists(IPermissible permissible)
        {
            if (Factory == null)
                return false;

            return Factory.Exists(this, permissible);
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

        public void Add(IPermissible permissible, Type type, AccessType accessType, DateTime? expiry = null)
        {
            if (Factory == null)
                return;

            _db = new ConcurrentDictionary<string, ConcurrentDictionary<string, AccessType>>();

            Factory.Add(this, permissible, type, accessType, expiry);
        }

        public void Remove(IPermissible permissible)
        {
            if (Factory == null)
                return;

            Factory.Remove(this, permissible);
        }

        public void Remove()
        {
            if (Factory == null)
                return;

            Factory.Remove(this);
        }

        public static Group FindGroup(string groupID)
        {
            if(Factory == null)
                return null;
            return Factory.FindGroup(User.CurrentUser, groupID);
        }
        public static Group FindGroupByName(string groupName)
        {
            return Factory.FindGroupByName(User.CurrentUser, groupName);
        }
        public static List<Group> Groups(IPermissible permissible, AccessType type)
        {
            return Factory.Groups(User.CurrentUser, permissible, type);
        }
        public static List<Group> Groups(IPermissible permissible)
        {
            return Factory.Groups(User.CurrentUser, permissible);
        }

        public static List<Group> Groups()
        {
            return Factory.Groups(User.CurrentUser);
        }

        public static List<Group> MasterGroups()
        {
            return Factory.MasterGroups(User.CurrentUser);
        }

        public static Group CreateGroup(string name)
        {
            return Factory.CreateGroup(User.CurrentUser, name);
        }

        public static Group CreateGroup(string name, string id)
        {
            return Factory.CreateGroup(User.CurrentUser, name, id);
        }

        public static IPermissible FindPermissible(User user, Type type, string id)
        {
            if (type == typeof(User))
            {
                User u = User.FindUser(id);
                return u;
            }

            IPermissible p = FindPermissibleCustom(type, id);

            if (p != null && user.Permission(p) != AccessType.Denied)
                return p;

            return null;
        }

        public delegate IPermissible FindPermissibleEvent(Type type, string id);
        public static FindPermissibleEvent FindPermissibleFunction = null;

        public static IPermissible FindPermissibleCustom(Type type, string id)
        {
            if (FindPermissibleFunction != null)
                return FindPermissibleFunction(type, id);
            return null;
        }


        public bool Equals(Group other)
        {
            if (((object)other) == null)
                return false;
            return ID == other.ID;
        }
        public override bool Equals(object other)
        {
            try { return Equals((Group)other); }
            catch { return false; }
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        public static bool operator ==(Group x, Group y)
        {
            if (((object)x) == null && ((object)y) == null)
                return true;
            else if (((object)x) == null)
                return false;

            return x.Equals(y);
        }
        public static bool operator !=(Group x, Group y)
        {
            return !(x == y);
        }
    }

    public interface IPermissible
    {
        string PermissibleID { get; }
    }
}
