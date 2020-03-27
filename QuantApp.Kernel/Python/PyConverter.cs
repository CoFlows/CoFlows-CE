/*
 * The MIT License (MIT)
 * Copyright (c) Arturo Rodriguez All rights reserved.
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
 
using System;
using Newtonsoft.Json;

namespace Python.Runtime
{
    class PyConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return true;//typeof(JVMObject) == objectType;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            // we currently support only writing of JSON
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                serializer.Serialize(writer, null);
                return;
            }

            var jobj = (PyObject)value;
            var jojb_type = jobj.GetPythonType().ToString();

            if(jojb_type == "<class 'NoneType'>")
                writer.WriteNull();

            else if(PyDict.IsDictType(jobj))
            {
                var dict = new PyDict(jobj);
                var keys = dict.Keys();

                writer.WriteStartObject();

                foreach (PyObject key in keys)
                {
                    string name = key.ToString();
                    var val = jobj[key];//.ToString();
                    
                    writer.WritePropertyName(name);
                    serializer.Serialize(writer, val, null);
                }
                writer.WriteEndObject();
            }
            else if(PyInt.IsIntType(jobj))
            {
                var pobj = new PyInt(jobj);
                writer.WriteValue(pobj.ToInt32());
            }
            else if(PyFloat.IsFloatType(jobj))
            {
                var pobj = new PyFloat(jobj);
                writer.WriteValue(pobj.ToDouble());
            }
            else if(PyLong.IsLongType(jobj))
            {
                var pobj = new PyLong(jobj);
                writer.WriteValue(pobj.ToInt64());
            }
            else if(PyString.IsStringType(jobj) || jojb_type == "<class 'datetime.date'>")
            {
                // var pobj = new PyString(jobj);
                writer.WriteValue(jobj.ToString());
            }
            else if(jobj.IsIterable())// && !PyDict.IsDictType(jobj))
            {
                writer.WriteStartArray();
                foreach(var element in jobj)
                    serializer.Serialize(writer, element, null);
                writer.WriteEndArray();
            }
            else
            {
                var properties = jobj.Dir();
                if(properties != null)
                {
                    writer.WriteStartObject();

                    foreach (PyObject property in properties)
                    {
                        string name = property.ToString();
                        if(!property.IsCallable() && !name.StartsWith("__"))
                        {
                            var attr = jobj.GetAttr(property);//.ToString();
                            writer.WritePropertyName(name);
                            try
                            {
                                serializer.Serialize(writer, attr, null);
                            }
                            catch
                            {
                                writer.WriteNull();
                            }
                        }
                    }
                    writer.WriteEndObject();
                }
            }
        }
    }
}