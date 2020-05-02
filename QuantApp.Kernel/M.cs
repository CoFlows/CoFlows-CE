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
using System.Security.Cryptography;
using System.Text;

namespace QuantApp.Kernel
{

    public delegate void MCallback(string id, Object data);

    public class EntryChange
    {
        public int Command { get; set; }
        public string ID { get; set; }
        public object Data { get; set; }
        public string MID { get; set; }

        public string Type { get; set; }
        public string Assembly { get; set; }
    }
    public class RawEntry
    {
        public string ID { get; set; }
        public object Entry { get; set; }
        public string EntryID { get; set; }
        public string Type { get; set; }
        public string Assembly { get; set; }
    }
    
    /// <summary>
    /// Class that stores objects and allows for easy query on the objects properties.    
    /// Sample:    
    /// M.Base(ID) += new { x1 = 1, x2 = 2, x3 = 3 }; // Add new dynamic structure
    /// M.Base(ID) += new { x1 = 2, x2 = 2, x3 = 3 }; // Add new dynamic structure
    /// M.Base(ID) += new { x1 = "1", x2 = 2, x3 = 3, x4 = 4 }; // Add new dynamic structure
    ///
    /// var res = M.Base(ID)[x => M.V<int>(x, "x1") >= 1 || M.V<string>(x, "x1") == "1" ]; // Query
    ///
    /// Console.WriteLine("Count: " + res.Count());
    /// foreach (var v in res) Console.WriteLine("found: " + v + " " + M.V<int>(v, "x2"));    
    /// </summary>
    public class M
    {
        public static ConcurrentDictionary<string, object> StaticMemory = new ConcurrentDictionary<string, object>();
        public static ConcurrentDictionary<string, System.Reflection.Assembly> _systemAssemblies = new ConcurrentDictionary<string, System.Reflection.Assembly>();
        public static ConcurrentDictionary<string, System.Reflection.Assembly> _compiledAssemblies = new ConcurrentDictionary<string, System.Reflection.Assembly>();

        public static ConcurrentDictionary<string, string> _systemAssemblyNames = new ConcurrentDictionary<string, string>();
        public static ConcurrentDictionary<string, string> _compiledAssemblyNames = new ConcurrentDictionary<string, string>();

        public Type type = null;
        public static string CRUDClass = "Kernel.M";

        public static QuantApp.Kernel.Factories.IMFactory Factory = null;

        private static ConcurrentDictionary<string, M> instance;

        private ConcurrentDictionary<string, string> add_fdb = new ConcurrentDictionary<string, string>();
        private ConcurrentDictionary<string, string> exchange_fdb = new ConcurrentDictionary<string, string>();
        private ConcurrentDictionary<string, string> remove_fdb = new ConcurrentDictionary<string, string>();

        public static ConcurrentDictionary<string, Object> _dic = new ConcurrentDictionary<string, Object>();

        public void RegisterAdd(string fid, string func)
        {
            if(group == null)
                group = Group.FindGroup(ID);
            
            if(group != null && !string.IsNullOrEmpty(QuantApp.Kernel.User.ContextUser.ID) && group.List(QuantApp.Kernel.User.CurrentUser, typeof(QuantApp.Kernel.User), false).Count > 0)
            {
                var permission = group.PermissionContext();
                if(permission != AccessType.Write)
                    return;
            }
            if(add_fdb.ContainsKey(fid))
                add_fdb[fid] = func;
            else
                add_fdb.TryAdd(fid, func);
        }

        public void RegisterExchange(string fid, string func)
        { 
            if(group == null)
                group = Group.FindGroup(ID);
            
            if(group != null && !string.IsNullOrEmpty(QuantApp.Kernel.User.ContextUser.ID) && group.List(QuantApp.Kernel.User.CurrentUser, typeof(QuantApp.Kernel.User), false).Count > 0)
            {
                var permission = group.PermissionContext();
                if(permission != AccessType.Write)
                    return;
            }
            
            if(exchange_fdb.ContainsKey(fid))
                exchange_fdb[fid] = func;
            else
                exchange_fdb.TryAdd(fid, func);
        }

        public void RegisterRemove(string fid, string func)
        {
            if(group == null)
                group = Group.FindGroup(ID);
            
            if(group != null && !string.IsNullOrEmpty(QuantApp.Kernel.User.ContextUser.ID) && group.List(QuantApp.Kernel.User.CurrentUser, typeof(QuantApp.Kernel.User), false).Count > 0)
            {
                var permission = group.PermissionContext();
                if(permission != AccessType.Write)
                    return;
            }
            if(remove_fdb.ContainsKey(fid))
                remove_fdb[fid] = func;
            else
                remove_fdb.TryAdd(fid, func);
        }

        public readonly static object objLock = new object();

        public static M Base(string id, Type type = null)
        {
            lock (objLock)
            {
                if (instance == null)
                    instance = new ConcurrentDictionary<string, M>();

                if (!instance.ContainsKey(id))
                {
                    M m = Factory != null ? Factory.Find(id, type) : null;
                    if (m == null)
                        instance.TryAdd(id, new M());
                    else
                        instance.TryAdd(id, m);
                }

                instance[id].ID = id;
                instance[id].type = type;

                return instance[id];
            }
        }

        public static void Clear()
        {
            instance = new ConcurrentDictionary<string, M>();
        }

        public readonly object saveLock = new object();
        public void Save()
        {
            lock (saveLock)
            {
                if (Factory != null)
                    Factory.Save(this);

                changes = new List<EntryChange>();
            }
        }

        public void Delete()
        {
            M outM = null;
            instance.TryRemove(this.ID, out outM);

            if (Factory != null)
                Factory.Remove(this);
        }

        private ConcurrentDictionary<string, object> singularity = new ConcurrentDictionary<string, object>();
        private ConcurrentDictionary<string, string> singularity_type = new ConcurrentDictionary<string, string>();
        private ConcurrentDictionary<string, string> singularity_assembly = new ConcurrentDictionary<string, string>();
        private ConcurrentDictionary<object, string> singularity_inverse = new ConcurrentDictionary<object, string>();


        private List<EntryChange> changes = new List<EntryChange>();

        public string ID { get; set; }

        public readonly object editLock = new object();

        private Group group = null;

        /// <summary>
        /// Function: Add an object        
        /// </summary>
        /// <param name="data">object to be added</param>
        public string Add(object data)
        {
            lock (editLock)
            {
                if (data == null)
                {
                    Console.WriteLine("M not null");
                    return null;
                }

                if(group == null)
                    group = Group.FindGroup(ID);
                
                if(group != null && !string.IsNullOrEmpty(QuantApp.Kernel.User.ContextUser.ID) && group.List(QuantApp.Kernel.User.CurrentUser, typeof(QuantApp.Kernel.User), false).Count > 0)
                {
                    var permission = group.PermissionContext();
                    if(permission != AccessType.Write)
                        return null;
                }

                var invKey = data;
                if (!singularity_inverse.ContainsKey(invKey))
                {
                    string key = System.Guid.NewGuid().ToString();
                    singularity.TryAdd(key, data);
                    singularity_inverse.TryAdd(invKey, key);

                    
                    if(singularity.Count != singularity_inverse.Count) 
                        Console.WriteLine("ERROR");

                    EntryChange change = new EntryChange() { Command = 1, ID = key, Data = data, MID = ID, Type = data.GetType().ToString(), Assembly = data.GetType().Assembly.GetName().Name };
                    changes.Add(change);

                    var data_str = "";

                    if(
                        data.GetType().ToString().ToLower() == "system.string" ||  
                        data.GetType().ToString().ToLower() == "newtonsoft.json.linq.jobject" || 
                        (data is string)
                    )
                    {
                        string filtered_string =  data.ToString().Replace((char)27, '"').Replace((char)26, '\'');
                        if(filtered_string.StartsWith("\"") && filtered_string.EndsWith("\""))
                            filtered_string = filtered_string.Substring(1, filtered_string.Length - 2).Replace("\\\"", "\"");
                        data_str = filtered_string;
                    }
                    else
                        data_str = Newtonsoft.Json.JsonConvert.SerializeObject(data);


                    RTDMessage.CRUDMessage crud = new RTDMessage.CRUDMessage() { 
                        TopicID = ID, 
                        ID = key, 
                        Type = RTDMessage.CRUDType.Create, 
                        Class = CRUDClass, 
                        Value = data_str, 
                        ValueType = 
                            data.GetType() == typeof(System.Dynamic.ExpandoObject) || data.GetType() == typeof(System.Dynamic.DynamicObject) ? 
                            typeof(string).ToString() : 
                            data.GetType().ToString(), ValueAssembly = data.GetType().Assembly.GetName().Name 
                    };
                    RTDEngine.Send(new RTDMessage() { Type = RTDMessage.MessageType.CRUD, Content = crud });

                    if(add_fdb.Count > 0)
                        foreach(var func in add_fdb.Values.ToList())
                            if(_dic.ContainsKey(func))
                                try
                                {
                                    (_dic[func] as MCallback)(key, data);
                                }
                                catch{}


                    return key;
                }

                if(singularity.Count != singularity_inverse.Count) 
                        Console.WriteLine("ERROR");
                
                return null;
            }
        }

        /// <summary>
        /// Function: Add an object        
        /// </summary>
        /// <param name="data">object to be added</param>
        public void AddID(string key, object data, string type, string assembly, bool update = true)
        {
            lock (editLock)
            {
                if (key == null || data == null)
                {
                    Console.WriteLine("M not added id: " + key + " " + data);
                    return;
                }

                if(group == null)
                    group = Group.FindGroup(ID);
                
                if(group != null && !string.IsNullOrEmpty(QuantApp.Kernel.User.ContextUser.ID) && group.List(QuantApp.Kernel.User.CurrentUser, typeof(QuantApp.Kernel.User), false).Count > 0)
                {
                    var permission = group.PermissionContext();
                    if(permission != AccessType.Write)
                        return;
                }

                var invKey = data;
                if (!singularity_inverse.ContainsKey(invKey))
                {
                    singularity.TryAdd(key, data);
                    singularity_type.TryAdd(key, type);
                    singularity_assembly.TryAdd(key, assembly);
                    singularity_inverse.TryAdd(invKey, key);

                    if(singularity.Count != singularity_inverse.Count) 
                            Console.WriteLine("ERROR");

                    changes.Add(new EntryChange() { Command = 1, ID = key, Data = data, Type = type, Assembly = assembly });

                    if(add_fdb.Count > 0)
                        foreach(var func in add_fdb.Values.ToList())
                            if(_dic.ContainsKey(func))
                                try
                                {
                                    (_dic[func] as MCallback)(key, data);
                                }
                                catch{}

                    if (update)
                    {

                        var data_str = "";

                        if(data.GetType().ToString().ToLower() == "system.string" ||  data.GetType().ToString().ToLower() == "newtonsoft.json.linq.jobject" || (data is string))
                        {
                            string filtered_string =  data.ToString().Replace((char)27, '"').Replace((char)26, '\'');
                            if(filtered_string.StartsWith("\"") && filtered_string.EndsWith("\""))
                                filtered_string = filtered_string.Substring(1, filtered_string.Length - 2).Replace("\\\"", "\"");
                            data_str = filtered_string;
                        }
                        else
                            data_str = Newtonsoft.Json.JsonConvert.SerializeObject(data);
                        RTDMessage.CRUDMessage crud = new RTDMessage.CRUDMessage() { TopicID = ID, ID = key, Type = RTDMessage.CRUDType.Create, Class = CRUDClass, Value = data_str, ValueType = type != null ? type : data.GetType() == typeof(System.Dynamic.ExpandoObject) || data.GetType() == typeof(System.Dynamic.DynamicObject) ? typeof(string).ToString() : data.GetType().ToString(), ValueAssembly = assembly != null ? assembly : data.GetType().Assembly.GetName().Name };
                        RTDEngine.Send(new RTDMessage() { Type = RTDMessage.MessageType.CRUD, Content = crud });
                    }
                }
            }
        }


        public void AddInternal(string key, object data, string type, string assembly)
        {
            lock (editLock)
            {
                if (key == null || data == null)
                {
                    Console.WriteLine("M not added internal: " + key + " " + data);
                    return;
                }

                if(group == null)
                    group = Group.FindGroup(ID);
                
                if(group != null && !string.IsNullOrEmpty(QuantApp.Kernel.User.ContextUser.ID) && group.List(QuantApp.Kernel.User.CurrentUser, typeof(QuantApp.Kernel.User), false).Count > 0)
                {
                    var permission = group.PermissionContext();
                    if(permission != AccessType.Write)
                        return;
                }

                var invKey = data;
                if (!singularity_inverse.ContainsKey(invKey))
                {
                    singularity.TryAdd(key, data);
                    singularity_type.TryAdd(key, type);
                    singularity_assembly.TryAdd(key, assembly);
                    singularity_inverse.TryAdd(invKey, key);

                    if(singularity.Count != singularity_inverse.Count) 
                            Console.WriteLine("ERROR");
                }
                else
                    Console.WriteLine("M not added internal: " + key + " " + data);
            }
        }

        /// <summary>
        /// Function: Exchange an object        
        /// </summary>
        /// <param name="data">object to be removed</param>
        public void Exchange(object dataOld, object dataNew)
        {
            lock (editLock)
            {
                var invKeyOld = dataOld;
                if (singularity_inverse.ContainsKey(invKeyOld))
                {
                    if(group == null)
                    group = Group.FindGroup(ID);
                
                    if(group != null && !string.IsNullOrEmpty(QuantApp.Kernel.User.ContextUser.ID) && group.List(QuantApp.Kernel.User.CurrentUser, typeof(QuantApp.Kernel.User), false).Count > 0)
                    {
                        var permission = group.PermissionContext();
                        if(permission != AccessType.Write)
                            return;
                    }
                    string key = singularity_inverse[invKeyOld];
                    object oo = null;
                    singularity.TryRemove(key, out oo);
                    string ooo = null;
                    singularity_inverse.TryRemove(invKeyOld, out ooo);

                    if(singularity.Count != singularity_inverse.Count) 
                        Console.WriteLine("ERROR");

                    var invKeyNew = dataNew;
                    if (!singularity_inverse.ContainsKey(invKeyNew))
                    {
                        singularity.TryAdd(key, dataNew);
                        singularity_inverse.TryAdd(invKeyNew, key);
                    }


                    if(singularity.Count != singularity_inverse.Count) 
                        Console.WriteLine("ERROR");
                    

                    var res = changes.Where(x => x != null && (x as EntryChange).ID == key).ToList();
                    foreach (var x in res)
                        changes.Remove(x);


                    EntryChange changeRemove = new EntryChange() { Command = -1, ID = key, Data = dataOld, MID = ID, Type = dataOld.GetType().ToString(), Assembly = dataOld.GetType().Assembly.GetName().Name };
                    changes.Add(changeRemove);

                    EntryChange changeAdd = new EntryChange() { Command = 1, ID = key, Data = dataNew, MID = ID, Type = dataNew.GetType().ToString(), Assembly = dataNew.GetType().Assembly.GetName().Name };
                    changes.Add(changeAdd);

                    var data_str = "";

                    if(dataNew.GetType().ToString().ToLower() == "system.string" ||  dataNew.GetType().ToString().ToLower() == "newtonsoft.json.linq.jobject" || (dataNew is string))
                    {
                        string filtered_string =  dataNew.ToString().Replace((char)27, '"').Replace((char)26, '\'');
                        if(filtered_string.StartsWith("\"") && filtered_string.EndsWith("\""))
                            filtered_string = filtered_string.Substring(1, filtered_string.Length - 2);
                        data_str = filtered_string;
                    }
                    else
                        data_str = Newtonsoft.Json.JsonConvert.SerializeObject(dataNew);

                    RTDMessage.CRUDMessage crud = new RTDMessage.CRUDMessage() { TopicID = ID, ID = key, Type = RTDMessage.CRUDType.Update, Class = CRUDClass, Value = data_str, ValueType = dataNew.GetType() == typeof(System.Dynamic.ExpandoObject) || dataNew.GetType() == typeof(System.Dynamic.DynamicObject) ? typeof(string).ToString() :  dataNew.GetType().ToString(), ValueAssembly = dataNew.GetType().Assembly.GetName().Name };
                    RTDEngine.Send(new RTDMessage() { Type = RTDMessage.MessageType.CRUD, Content = crud });

                    if(exchange_fdb.Count > 0)
                        foreach(var func in exchange_fdb.Values.ToList())
                            if(_dic.ContainsKey(func))
                                try
                                {
                                    (_dic[func] as MCallback)(key, dataNew);
                                }
                                catch{}
                }
            }
        }

        /// <summary>
        /// Function: Exchange an object        
        /// </summary>
        /// <param name="data">object to be removed</param>
        public void ExchangeID(string key, object dataNew, string type, string assembly, bool update = true)
        {
            lock (editLock)
            {

                if (singularity.ContainsKey(key))
                {
                    if(group == null)
                    group = Group.FindGroup(ID);
                
                    if(group != null && !string.IsNullOrEmpty(QuantApp.Kernel.User.ContextUser.ID) && group.List(QuantApp.Kernel.User.CurrentUser, typeof(QuantApp.Kernel.User), false).Count > 0)
                    {
                        var permission = group.PermissionContext();
                        if(permission != AccessType.Write)
                            return;
                    }

                    object dataOld = singularity[key];
                    var invKeyOld = dataOld;

                    object oo = null;
                    singularity.TryRemove(key, out oo);
                    string tt = "";
                    singularity_type.TryRemove(key, out tt);
                    singularity_assembly.TryRemove(key, out tt);
                    string ooo = null;
                    singularity_inverse.TryRemove(invKeyOld, out ooo);

                    if(singularity.Count != singularity_inverse.Count) 
                        Console.WriteLine("ERROR");
                    
                    var invKeyNew = dataNew;
                    if (!singularity_inverse.ContainsKey(invKeyNew))
                    {
                        singularity.TryAdd(key, dataNew);
                        singularity_type.TryAdd(key, type);
                        singularity_assembly.TryAdd(key, assembly);
                        singularity_inverse.TryAdd(invKeyNew, key);
                    }

                    var res = changes.Where(x => x != null && (x as EntryChange).ID == key).ToList();
                    foreach (var x in res)
                        changes.Remove(x);

                    EntryChange changeRemove = new EntryChange() { Command = -1, ID = key, Data = dataOld, MID = ID, Type = dataOld.GetType().ToString(), Assembly = dataOld.GetType().Assembly.GetName().Name };
                    changes.Add(changeRemove);

                    EntryChange changeAdd = new EntryChange() { Command = 1, ID = key, Data = dataNew, MID = ID, Type = type != null ? type : dataNew.GetType().ToString(), Assembly = assembly != null ? assembly : dataNew.GetType().Assembly.GetName().Name };
                    changes.Add(changeAdd);

                    if(exchange_fdb.Count > 0)
                        foreach(var func in exchange_fdb.Values.ToList())
                            if(_dic.ContainsKey(func))
                                try
                                {
                                    (_dic[func] as MCallback)(key, dataNew);
                                }
                                catch{}
                                

                    
                    if(update)
                    {
                        var data_str = "";

                        if(dataNew.GetType().ToString().ToLower() == "system.string" ||  dataNew.GetType().ToString().ToLower() == "newtonsoft.json.linq.jobject" || (dataNew is string))
                        {
                            string filtered_string =  dataNew.ToString().Replace((char)27, '"').Replace((char)26, '\'');
                            if(filtered_string.StartsWith("\"") && filtered_string.EndsWith("\""))
                                filtered_string = filtered_string.Substring(1, filtered_string.Length - 2);
                            data_str = filtered_string;
                        }
                        else
                            data_str = Newtonsoft.Json.JsonConvert.SerializeObject(dataNew);
                        RTDMessage.CRUDMessage crud = new RTDMessage.CRUDMessage() { TopicID = ID, ID = key, Type = RTDMessage.CRUDType.Update, Class = CRUDClass, Value = data_str, ValueType = type != null ? type : dataNew.GetType() == typeof(System.Dynamic.ExpandoObject) || dataNew.GetType() == typeof(System.Dynamic.DynamicObject) ? typeof(string).ToString() :  dataNew.GetType().ToString(), ValueAssembly = assembly != null ? assembly : dataNew.GetType().Assembly.GetName().Name };
                        RTDEngine.Send(new RTDMessage() { Type = RTDMessage.MessageType.CRUD, Content = crud });
                    }
                }
            }
        }

        /// <summary>
        /// Function: Remove an object        
        /// </summary>
        /// <param name="data">object to be removed</param>
        public void Remove(object data)
        {
            lock (editLock)
            {
                var invKey = data;
                if (singularity_inverse.ContainsKey(invKey))
                {
                    if(group == null)
                    group = Group.FindGroup(ID);
                
                    if(group != null && !string.IsNullOrEmpty(QuantApp.Kernel.User.ContextUser.ID) && group.List(QuantApp.Kernel.User.CurrentUser, typeof(QuantApp.Kernel.User), false).Count > 0)
                    {
                        var permission = group.PermissionContext();
                        if(permission != AccessType.Write)
                            return;
                    }

                    string key = singularity_inverse[invKey];

                    object oo = null;
                    singularity.TryRemove(key, out oo);
                    string tt = "";
                    singularity_type.TryRemove(key, out tt);
                    singularity_assembly.TryRemove(key, out tt);
                    
                    string ooo = null;
                    singularity_inverse.TryRemove(invKey, out ooo);


                    if(singularity.Count != singularity_inverse.Count) 
                        Console.WriteLine("ERROR");

                    var res = changes.Where(x => x != null && (x as EntryChange).ID == key).ToList();
                    foreach (var x in res)
                        changes.Remove(x);

                    EntryChange change = new EntryChange() { Command = -1, ID = key, Data = data, MID = ID, Type = data.GetType().ToString(), Assembly = data.GetType().Assembly.GetName().Name };
                    changes.Add(change);

                    RTDMessage.CRUDMessage crud = new RTDMessage.CRUDMessage() { TopicID = ID, ID = key, Type = RTDMessage.CRUDType.Delete, Class = CRUDClass, Value = data.GetType() == typeof(string) ? data : Newtonsoft.Json.JsonConvert.SerializeObject(data), ValueType = data.GetType() == typeof(System.Dynamic.ExpandoObject) || data.GetType() == typeof(System.Dynamic.DynamicObject) ? typeof(string).ToString() : data.GetType().ToString(), ValueAssembly = data.GetType().Assembly.GetName().Name };
                    RTDEngine.Send(new RTDMessage() { Type = RTDMessage.MessageType.CRUD, Content = crud });
                    
                    if(remove_fdb.Count > 0)
                        foreach(var func in remove_fdb.Values.ToList())
                            if(_dic.ContainsKey(func))
                                try
                                {
                                    (_dic[func] as MCallback)(key, data);
                                }
                                catch{}

                }
            }
        }

        /// <summary>
        /// Function: Remove an object        
        /// </summary>
        /// <param name="data">object to be removed</param>
        public void RemoveID(string key, bool update = true)
        {
            lock (editLock)
            {
                if (singularity.ContainsKey(key))
                {
                    if(group == null)
                    group = Group.FindGroup(ID);
                
                    if(group != null && !string.IsNullOrEmpty(QuantApp.Kernel.User.ContextUser.ID) && group.List(QuantApp.Kernel.User.CurrentUser, typeof(QuantApp.Kernel.User), false).Count > 0)
                    {
                        var permission = group.PermissionContext();
                        if(permission != AccessType.Write)
                            return;
                    }
                    object data = singularity[key];

                    object oo = null;
                    singularity.TryRemove(key, out oo);
                    string tt = "";
                    singularity_type.TryRemove(key, out tt);
                    singularity_assembly.TryRemove(key, out tt);
                    
                    string ooo = null;
                    var invKey = data;
                    singularity_inverse.TryRemove(invKey, out ooo);

                    if(singularity.Count != singularity_inverse.Count) 
                        Console.WriteLine("ERROR");
                    
                    var res = changes.Where(x => x != null && (x as EntryChange).ID == key).ToList();
                    foreach (var x in res)
                    {
                        changes.Remove(x);
                    }

                    EntryChange change = new EntryChange() { Command = -1, ID = key, Data = data, MID = ID, Type = data.GetType().ToString(), Assembly = data.GetType().Assembly.GetName().Name };
                    changes.Add(change);
                    
                    if(remove_fdb.Count > 0)
                        foreach(var func in remove_fdb.Values.ToList())
                            if(_dic.ContainsKey(func))
                                try
                                {
                                    (_dic[func] as MCallback)(key, data);
                                }
                                catch{}

                    if(update)
                    {
                        RTDMessage.CRUDMessage crud = new RTDMessage.CRUDMessage() { TopicID = ID, ID = key, Type = RTDMessage.CRUDType.Delete, Class = CRUDClass, Value = data.GetType() == typeof(string) ? data : Newtonsoft.Json.JsonConvert.SerializeObject(data), ValueType = data.GetType() == typeof(System.Dynamic.ExpandoObject) || data.GetType() == typeof(System.Dynamic.DynamicObject) ? typeof(string).ToString() : data.GetType().ToString(), ValueAssembly = data.GetType().Assembly.GetName().Name };
                        RTDEngine.Send(new RTDMessage() { Type = RTDMessage.MessageType.CRUD, Content = crud });
                    }

                }
            }
        }


        public readonly object processLock = new object();
        /// <summary>
        /// Function: Process change
        /// </summary>
        /// <param name="change">change to be processed</param>
        public void Process(RTDMessage.CRUDMessage message)
        {
            lock (processLock)
            {
                if(group == null)
                    group = Group.FindGroup(ID);
                
                if(group != null && !string.IsNullOrEmpty(QuantApp.Kernel.User.ContextUser.ID) && group.List(QuantApp.Kernel.User.CurrentUser, typeof(QuantApp.Kernel.User), false).Count > 0)
                {
                    var permission = group.PermissionContext();
                    if(permission != AccessType.Write)
                        return;
                }

                if (singularity.ContainsKey(message.ID))
                {
                    if (message.Type == RTDMessage.CRUDType.Create)
                        this.AddID(message.ID, message.Value, message.ValueType, message.ValueAssembly, false);
                    else if (message.Type == RTDMessage.CRUDType.Delete)
                        this.RemoveID(message.ID, false);
                    else
                        this.ExchangeID(message.ID, message.Value, message.ValueType, message.ValueAssembly, false);

                }
                else if (message.Type == RTDMessage.CRUDType.Create || message.Type == RTDMessage.CRUDType.Update)
                    this.AddID(message.ID, message.Value, message.ValueType, message.ValueAssembly, false);
            }
        }

        [Newtonsoft.Json.JsonIgnore]
        public List<EntryChange> Changes { get { return changes; } }

        /// <summary>
        /// Operator: Add an object        
        /// </summary>
        /// <param name="y">object to be added</param>
        public static M operator +(M x, object y)
        {
            x.Add(y);
            return x;
        }

        /// <summary>
        /// Operator: Remove an object        
        /// </summary>
        /// <param name="y">object to be removed</param>
        public static M operator -(M x, object y)
        {
            x.Remove(y);
            return x;
        }

        /// <summary>
        /// Function: Query the M through a predicate
        /// </summary>
        /// <param name="predicate">query</param>
        public List<KeyValuePair<string, object>> KeyValues()
        {
            lock(editLock)
            {
                if(group == null)
                    group = Group.FindGroup(ID);
                
                if(group != null && !string.IsNullOrEmpty(QuantApp.Kernel.User.ContextUser.ID) && group.List(QuantApp.Kernel.User.CurrentUser, typeof(QuantApp.Kernel.User), false).Count > 0)
                {
                    var permission = group.PermissionContext();
                    if(permission == AccessType.Denied)
                        return new List<KeyValuePair<string, object>>();
                }

                return singularity.ToList();
            }
        }

        public List<RawEntry> RawEntries()
        {
            lock(editLock)
            {
                if(group == null)
                    group = Group.FindGroup(ID);

                if(group != null && !string.IsNullOrEmpty(QuantApp.Kernel.User.ContextUser.ID) && group.List(QuantApp.Kernel.User.CurrentUser, typeof(QuantApp.Kernel.User), false).Count > 0)
                {
                    var permission = group.PermissionContext();
                    if(permission == AccessType.Denied)
                        return new List<RawEntry>();
                }

                var entries = singularity.ToList();
                var res = new List<RawEntry>();
                foreach(var entry in entries)
                {
                    var rawentry = new RawEntry();
                    rawentry.ID = this.ID;
                    rawentry.EntryID = entry.Key;
                    rawentry.Type = singularity_type.ContainsKey(entry.Key) ? singularity_type[entry.Key] : entry.Value.GetType().ToString();
                    rawentry.Assembly = singularity_assembly.ContainsKey(entry.Key) ? singularity_assembly[entry.Key] : entry.Value.GetType().Assembly.GetName().Name;

                    if(rawentry.Type.ToLower() == "system.string" ||  rawentry.Type.ToLower() == "newtonsoft.json.linq.jobject" || (entry.Value is string))
                    {
                        string filtered_string =  entry.Value.ToString().Replace((char)27, '"').Replace((char)26, '\'');
                        if(filtered_string.StartsWith("\"") && filtered_string.EndsWith("\""))
                            filtered_string = filtered_string.Substring(1, filtered_string.Length - 2).Replace("\\\"", "\"");
                        rawentry.Entry = filtered_string;
                    }
                    else
                        rawentry.Entry = Newtonsoft.Json.JsonConvert.SerializeObject(entry.Value);


                    
                    res.Add(rawentry);
                }

                return res;
            }
        }

        public void LoadRaw(List<RawEntry> rawEntries)
        {
            lock(editLock)
            {
                if(rawEntries == null)
                    return;

                if(group == null)
                    group = Group.FindGroup(ID);
                
                if(group != null && !string.IsNullOrEmpty(QuantApp.Kernel.User.ContextUser.ID) && group.List(QuantApp.Kernel.User.CurrentUser, typeof(QuantApp.Kernel.User), false).Count > 0)
                {
                    var permission = group.PermissionContext();
                    if(permission == AccessType.Denied)
                        return;
                }

                foreach (var rawEntry in rawEntries)
                {

                    string entryID = rawEntry.EntryID;
                    string typeName = rawEntry.Type;
                    string assemblyName = rawEntry.Assembly;

                    try
                    {
                        if (type == null)
                        {
                            
                            

                            type = Type.GetType(typeName);
                            if(type == null)
                            {
                                var assembly = M._systemAssemblies.ContainsKey(typeName) ? M._systemAssemblies[typeName] : (M._compiledAssemblyNames.ContainsKey(typeName) ? M._compiledAssemblies[M._compiledAssemblyNames[typeName]] : System.Reflection.Assembly.Load(assemblyName));
                                type = assembly.GetType(M._systemAssemblyNames.ContainsKey(typeName) ? M._systemAssemblyNames[typeName] : typeName);
                            }
                        }

                        DateTime d1 = DateTime.Now;
                        object obj = type == typeof(Nullable) || type == typeof(string) ? rawEntry.Entry : Newtonsoft.Json.JsonConvert.DeserializeObject(rawEntry.Entry as string, type);
                        DateTime d2 = DateTime.Now;

                        this.AddInternal(entryID, obj, typeName, assemblyName);
                    }
                    catch (Exception e)
                    {
                        this.AddInternal(entryID, rawEntry.Entry, typeName, assemblyName);
                    }
                }
            }
        }


        /// <summary>
        /// Function: Query the M through a predicate
        /// </summary>
        /// <param name="predicate">query</param>
        public List<object> this[Func<object, bool> predicate]
        {
            get
            {
                lock(editLock)
                {
                    if(group == null)
                        group = Group.FindGroup(ID);
                    if(group != null && !string.IsNullOrEmpty(QuantApp.Kernel.User.ContextUser.ID) && group.List(QuantApp.Kernel.User.CurrentUser, typeof(QuantApp.Kernel.User), false).Count > 0)
                    {
                        var permission = group.PermissionContext();
                        if(permission == AccessType.Denied)
                            return new List<object>();
                    }



                    List<object> res = singularity.Values.Where(predicate).Select(x => x).ToList();
                    return res;
                }
            }
        }

        /// <summary>
        /// Function: Query the M through a predicate
        /// </summary>
        /// <param name="predicate">query</param>
        public List<object> Query(Func<object, bool> predicate)
        {
            lock(editLock)
            {
                if(group == null)
                    group = Group.FindGroup(ID);
                
                if(group != null && !string.IsNullOrEmpty(QuantApp.Kernel.User.ContextUser.ID) && group.List(QuantApp.Kernel.User.CurrentUser, typeof(QuantApp.Kernel.User), false).Count > 0)
                {
                    var permission = group.PermissionContext();
                    if(permission == AccessType.Denied)
                        return new List<object>();
                }

                List<object> res = singularity.Values.Where(predicate).Select(x => x).ToList();
                return res;
            }
        }

        /// <summary>
        /// Function: Query the M through a predicate
        /// </summary>
        /// <param name="predicate">query</param>
        public IEnumerable<IGrouping<object, object>> this[Func<object, bool> predicate, Func<object, object> groupby]
        {
            get
            {
                lock(editLock)
                {
                    if(group == null)
                        group = Group.FindGroup(ID);
                    
                    if(group != null && !string.IsNullOrEmpty(QuantApp.Kernel.User.ContextUser.ID) && group.List(QuantApp.Kernel.User.CurrentUser, typeof(QuantApp.Kernel.User), false).Count > 0)
                    {
                        var permission = group.PermissionContext();
                        if(permission == AccessType.Denied)
                            return null;
                    }

                    IEnumerable<IGrouping<object, object>> res = singularity.Values.Where(predicate).Select(x => x).GroupBy(groupby);
                    return res;
                }
            }
        }

        private static ConcurrentDictionary<string,Newtonsoft.Json.Linq.JObject> __stringJObject = new ConcurrentDictionary<string,Newtonsoft.Json.Linq.JObject>();

        /// <summary>
        /// Function: Returns the value of a property for a given object
        /// </summary>
        /// <param name="T">Type of return</param>
        /// <param name="x">object</param>
        /// <param name="property">Property Name</param>
        public static T V<T>(object x, string property)
        {
            object nul = null;
            if (typeof(T) == typeof(string))
                nul = "";
            else if (typeof(T) == typeof(int))
                nul = int.MinValue;
            else if (typeof(T) == typeof(double))
                nul = double.NaN;
            else if (typeof(T) == typeof(DateTime))
                nul = DateTime.MinValue;
            else if (typeof(T) == typeof(bool))
                nul = false;

            else if (typeof(T) == typeof(bool))
                nul = false;
        
            if(x.GetType() == typeof(Newtonsoft.Json.Linq.JObject) || x is string)
            {
                if(x is string && __stringJObject.ContainsKey(x as string))
                    __stringJObject.TryAdd(x as string, Newtonsoft.Json.Linq.JObject.Parse(x as string));

                var jobj = ((Newtonsoft.Json.Linq.JObject)(x is string ? (__stringJObject.ContainsKey(x as string) ? __stringJObject[x as string] : Newtonsoft.Json.Linq.JObject.Parse(x as string)) : x)).Value<T>(property);

                if(jobj == null)
                    return (T)nul;

                Type jobjType = jobj.GetType();
                if (jobjType == typeof(T))
                    return (T)jobj;
                else
                    return (T)nul;
            }

            var obj = (x.GetType().GetProperty(property) != null ? x.GetType().GetProperty(property).GetValue(x, null) : null);

            if (obj == null)
                return (T)nul;

            Type objType = obj.GetType();
            if (objType == typeof(T))
                return (T)obj;
            else
                return (T)nul;
        }

        /// <summary>
        /// Function: Returns the value of a property for a given object
        /// </summary>
        /// <param name="T">Type of return</param>
        /// <param name="x">object</param>
        /// <param name="property">Property Name</param>
        public static T C<T>(object x)
        {
            object nul = null;
            if (typeof(T) == typeof(string))
                nul = "";
            else if (typeof(T) == typeof(int))
                nul = int.MinValue;
            else if (typeof(T) == typeof(double))
                nul = double.NaN;
            else if (typeof(T) == typeof(DateTime))
                nul = DateTime.MinValue;
            else if (typeof(T) == typeof(bool))
                nul = false;

            else if (typeof(T) == x.GetType())
                return (T)x;

            var str = x is string ? x as string : Newtonsoft.Json.JsonConvert.SerializeObject(x);
            return (T)Newtonsoft.Json.JsonConvert.DeserializeObject(str, typeof(T));
        }
    }
}
