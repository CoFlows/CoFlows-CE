/*
 * The MIT License (MIT)
 * Copyright (c) 2007-2019, Arturo Rodriguez All rights reserved.
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
 
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Dynamic;
using System.Reflection;

using Newtonsoft.Json;

namespace QuantApp.Kernel.JVM
{
    [JsonConverter(typeof(JVMConverter))]
    public class JVMObject : DynamicObject
    {
        /// <summary>
        /// Instance of object passed in
        /// </summary>
        object Instance;

        /// <summary>
        /// Cached type of the instance
        /// </summary>
        Type InstanceType;

        public int JavaHashCode;
        public string JavaClass;

        public unsafe IntPtr Pointer
        {
            get
            {
                IntPtr ptr = new IntPtr(Runtime.GetJVMObject(this.JavaHashCode));
                if(ptr == IntPtr.Zero)
                    throw new Exception("NO OBJECT");
                return ptr;
            }
        }




        /// <summary>
        /// String Dictionary that contains the extra dynamic values
        /// stored on this object/instance
        /// </summary>        
        /// <remarks>Using PropertyBag to support XML Serialization of the dictionary</remarks>
        // public PropertyBag Properties = new PropertyBag();

        public Dictionary<string,object> Properties = new Dictionary<string, object>();
        public Dictionary<string,Delegate> Members = new Dictionary<string, Delegate>();


        public static ConcurrentDictionary<int, JVMObject> DB = new ConcurrentDictionary<int, JVMObject>();

        /// <summary>
        /// This constructor just works off the internal dictionary and any 
        /// public properties of this object.
        /// 
        /// Note you can subclass Expando.
        /// </summary>
        public unsafe JVMObject(IntPtr _pointer, int jHashCode, string jClass) 
        {
            this.JavaClass = jClass;
            this.JavaHashCode = jHashCode;
            
            Initialize(this);            

            int hsh = this.JavaHashCode;

            Runtime.RegisterJVMObject(hsh, _pointer.ToPointer());

            if(!DB.ContainsKey(hsh))
            {
                DB.TryAdd(hsh, this);
            }
        }

        public override int GetHashCode()
        {
            return this.JavaHashCode;
        }

        public override string ToString()
        {
            try
            {
                dynamic obj = this;
                return "JVMObject(" + this.JavaClass + " - Ptr = " + Pointer + " : nhash = " + this.GetHashCode() + ": jhash = " + this.JavaHashCode + "): " + obj.toString();
            }
            catch
            {
                return "JVMObject(" + this.JavaClass + " - Ptr = " + Pointer + " : nhash = " + this.GetHashCode() + ": jhash = " + this.JavaHashCode + ")";
            }
        }

        public override bool Equals(object obj)
        {
            try
            {
                dynamic refobj = this;
                return refobj.equals(obj);
            }
            catch
            {
                return this.JavaHashCode == obj.GetHashCode();
            }
        }


        protected virtual void Initialize(object instance)
        {
            Instance = instance;
            if (instance != null)
                InstanceType = instance.GetType();           
        }

        /// <summary>
        /// Try to retrieve a member by name first from instance properties
        /// followed by the collection entries.
        /// </summary>
        /// <param name="binder"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            try
            {
                result = TryGetMember(binder.Name);
                return true;
            }
            catch
            {
                result = null;
                return false;
            }
        }

        public object TryGetMember(string name)
        {
            if (Properties.ContainsKey(name))
            {
                Tuple<string, object, Runtime.wrapSetProperty> funcs = (Tuple<string, object, Runtime.wrapSetProperty>)Properties[name];
                string ttype = funcs.Item1;
                object result;
                switch (ttype)
                {
                    case "bool":
                        result = ((Runtime.wrapGetProperty<bool>)funcs.Item2)();
                        break;

                    case "byte":
                        result = ((Runtime.wrapGetProperty<byte>)funcs.Item2)();
                        break;
                    
                    case "char":
                        result = ((Runtime.wrapGetProperty<char>)funcs.Item2)();
                        break;

                    case "short":
                        result = ((Runtime.wrapGetProperty<short>)funcs.Item2)();
                        break;
                    
                    case "int":
                        result = ((Runtime.wrapGetProperty<int>)funcs.Item2)();
                        break;

                    case "long":
                        result = ((Runtime.wrapGetProperty<long>)funcs.Item2)();
                        break;
                    
                    case "float":
                        result = ((Runtime.wrapGetProperty<float>)funcs.Item2)();
                        break;

                    case "double":
                        result = ((Runtime.wrapGetProperty<double>)funcs.Item2)();
                        break;

                    case "string":
                        result = ((Runtime.wrapGetProperty<string>)funcs.Item2)();
                        break;

                    case "object":
                        result = ((Runtime.wrapGetProperty<JVMObject>)funcs.Item2)();
                        break;

                    case "array":
                        result = ((Runtime.wrapGetProperty<object[]>)funcs.Item2)();
                        break;

                    default:
                        result = null;
                        break;
                }
                return result;
                
            }

            if (Instance != null)
            {
                try
                {
                    object result;
                    GetProperty(Instance, name, out result);
                    return result;
                }
                catch { }
            }

            return null;
        }


        
        /// <summary>
        /// Property setter implementation tries to retrieve value from instance 
        /// first then into this object
        /// </summary>
        /// <param name="binder"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            string signature = binder.Name;
            if (Instance != null)
            {
                try
                {
                    Tuple<string, object, Runtime.wrapSetProperty> funcs = (Tuple<string, object, Runtime.wrapSetProperty>)Properties[binder.Name];
                    funcs.Item3(value);
                    return true;
                }
                catch(Exception e) { Console.WriteLine(e); }
            }
            Properties[binder.Name] = value;
            return true;
        }

        public bool TrySetMember(string name, object value)
        {
            if(value is Delegate)
            {
                string signature = name;
                Delegate fun = value as Delegate;
                Members[signature] = fun;
                return true;
            }
            else
            {
                if (Properties.ContainsKey(name))
                {
                    Tuple<string, object, Runtime.wrapSetProperty> funcs = (Tuple<string, object, Runtime.wrapSetProperty>)Properties[name];
                    string ttype = funcs.Item1;

                    switch (ttype)
                    {
                        case "bool":
                            ((Runtime.wrapSetProperty)funcs.Item3)(Convert.ToBoolean(value));
                            break;

                        case "byte":
                            ((Runtime.wrapSetProperty)funcs.Item3)(Convert.ToByte(value));
                            break;
                        
                        case "char":
                            ((Runtime.wrapSetProperty)funcs.Item3)(Convert.ToChar(value));
                            break;

                        case "short":
                            ((Runtime.wrapSetProperty)funcs.Item3)(Convert.ToInt16(value));
                            break;
                        
                        case "int":
                            ((Runtime.wrapSetProperty)funcs.Item3)(Convert.ToInt32(value));
                            break;

                        case "long":
                            ((Runtime.wrapSetProperty)funcs.Item3)(Convert.ToInt64(value));
                            break;
                        
                        case "float":
                            ((Runtime.wrapSetProperty)funcs.Item3)(Convert.ToDecimal(value));
                            break;

                        case "double":
                            ((Runtime.wrapSetProperty)funcs.Item3)(Convert.ToDouble(value));
                            break;

                        case "string":
                            ((Runtime.wrapSetProperty)funcs.Item3)(Convert.ToString(value));
                            break;

                        case "object":
                            ((Runtime.wrapSetProperty)funcs.Item3)(value);
                            break;

                        case "array":
                            ((Runtime.wrapSetProperty)funcs.Item3)((object[])value);
                            break;

                        default:
                            ((Runtime.wrapSetProperty)funcs.Item3)(value);
                            break;
                    
                    }
                    return true;
            
                }
            }
            return false;
        }

        public bool TrySetField(string name, object value)
        {
            string signature = name;
            if (Instance != null)
            {
                try
                {
                    bool result = SetProperty(Instance, name, value);
                    if (result)
                        return true;
                }
                catch { }
            }
            Properties[name] = value;
            return true;
        }

        /// <summary>
        /// Dynamic invocation method. Currently allows only for Reflection based
        /// operation (no ability to add methods dynamically).
        /// </summary>
        /// <param name="binder"></param>
        /// <param name="args"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            string signature = binder.Name + "-";            
            
            
            foreach(var t in args)
                signature += Runtime.TransformType(t);

            if(Members.ContainsKey(signature))
            {
                if(args.Length > 0)
                    result = Members[signature].DynamicInvoke((object)args);
                
                else
                    result = Members[signature].DynamicInvoke((object)null);
                return true;
            }
            
            result = null;
            return false;
            
        }

        public object InvokeMember(string name, object[] args)
        {
            string signature = name + "-";
            
            foreach(var t in args)
                signature += Runtime.TransformType(t);

            if(Members.ContainsKey(signature))
            {
                if(args.Length > 0)
                    return Members[signature].DynamicInvoke((object)args);
                
                else
                    return Members[signature].DynamicInvoke((object)null);
            }
            return null;
        }

        /// <summary>
        /// Reflection Helper method to retrieve a property
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="name"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        protected bool GetProperty(object instance, string name, out object result)
        {
            if (instance == null)
                instance = this;

            var miArray = InstanceType.GetMember(name, BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.Instance);
            if (miArray != null && miArray.Length > 0)
            {
                var mi = miArray[0];
                if (mi.MemberType == MemberTypes.Property)
                {

                    Tuple<string, object, Runtime.wrapSetProperty> funcs = (Tuple<string, object, Runtime.wrapSetProperty>)((PropertyInfo)mi).GetValue(instance,null);
                    string ttype = funcs.Item1;
                    switch (ttype)
                    {
                        case "bool":
                            result = ((Runtime.wrapGetProperty<bool>)funcs.Item2)();
                            break;

                        case "byte":
                            result = ((Runtime.wrapGetProperty<byte>)funcs.Item2)();
                            break;
                        
                        case "char":
                            result = ((Runtime.wrapGetProperty<char>)funcs.Item2)();
                            break;

                        case "short":
                            result = ((Runtime.wrapGetProperty<short>)funcs.Item2)();
                            break;
                        
                        case "int":
                            result = ((Runtime.wrapGetProperty<int>)funcs.Item2)();
                            break;

                        case "long":
                            result = ((Runtime.wrapGetProperty<long>)funcs.Item2)();
                            break;
                        
                        case "float":
                            result = ((Runtime.wrapGetProperty<float>)funcs.Item2)();
                            break;

                        case "double":
                            result = ((Runtime.wrapGetProperty<double>)funcs.Item2)();
                            break;

                        default:
                            result = null;
                            break;
                    }

                    return true;
                }
            }

            result = null;
            return false;                
        }

        /// <summary>
        /// Reflection helper method to set a property value
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        protected bool SetProperty(object instance, string name, object value)
        {
            if (instance == null)
                instance = this;

            var miArray = InstanceType.GetMember(name, BindingFlags.Public | BindingFlags.SetProperty | BindingFlags.Instance);

            if (miArray != null && miArray.Length > 0)
            {
                var mi = miArray[0];
                if (mi.MemberType == MemberTypes.Property)
                {
                    ((PropertyInfo)mi).SetValue(Instance, value, null);
                    return true;
                }
            }
            return false;                
        }

        /// <summary>
        /// Reflection helper method to invoke a method
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="name"></param>
        /// <param name="args"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        protected bool InvokeMethod(object instance, string name, object[] args, out object result)
        {
            if (instance == null)
                instance = this;

            var miArray = InstanceType.GetMember(name,
                                    BindingFlags.InvokeMethod |
                                    BindingFlags.Public | BindingFlags.Instance);

            

            if (miArray != null && miArray.Length > 0)
            {
                var mi = miArray[0] as MethodInfo;
                result = mi.Invoke(Instance, args);
                return true;
            }

            result = null;
            return false;
        }



        /// <summary>
        /// Convenience method that provides a string Indexer 
        /// to the Properties collection AND the strongly typed
        /// properties of the object by name.
        /// 
        /// // dynamic
        /// exp["Address"] = "112 nowhere lane"; 
        /// // strong
        /// var name = exp["StronglyTypedProperty"] as string; 
        /// </summary>
        /// <remarks>
        /// The getter checks the Properties dictionary first
        /// then looks in PropertyInfo for properties.
        /// The setter checks the instance properties before
        /// checking the Properties dictionary.
        /// </remarks>
        /// <param name="key"></param>
        /// 
        /// <returns></returns>
        public object this[string key]
        {
            get
            {
                object result = null;
                // if (GetProperty(Instance, key, out result))
                //     return result;
                // else
                //     return null;
                Console.WriteLine("--------------JVMObject TRY GET MEMBER: " + key);
                return TryGetMember(key);
            }
            set
            {
                Console.WriteLine("--------------JVMObject TRY SET MEMBER: " + key);
                TrySetMember(key, value);
                // if (Properties.ContainsKey(key))
                // {
                //     Properties[key] = value;
                //     return;
                // }

                // // check instance for existance of type first
                // var miArray = InstanceType.GetMember(key, BindingFlags.Public | BindingFlags.GetProperty);
                // if (miArray != null && miArray.Length > 0)
                //     SetProperty(Instance, key, value);
                // else
                //     Properties[key] = value;
            }
        }
    }
}
