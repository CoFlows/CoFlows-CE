/*
 * The MIT License (MIT)
 * Copyright (c) Arturo Rodriguez All rights reserved.
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
 
using System;
using Newtonsoft.Json;

namespace QuantApp.Kernel.JVM
{
    class JVMConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(JVMObject) == objectType;
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

            if(value is JVMIEnumerable)
            {
                writer.WriteStartArray();
                
                var jobj = (JVMIEnumerable)value;
                foreach(var element in jobj)
                    serializer.Serialize(writer, element, null);
                writer.WriteEndArray();
            }
            else if(value is JVMICollection)
            {
                writer.WriteStartArray();
                
                var jobj = (JVMICollection)value;
                foreach(var element in jobj)
                    serializer.Serialize(writer, element, null);
                writer.WriteEndArray();
            }
            else if(value is JVMIDictionary)
            {
                writer.WriteStartArray();
                
                var jobj = (JVMIDictionary)value;
                foreach(var element in jobj)
                {
                    writer.WritePropertyName(element.Key.ToString());
                    serializer.Serialize(writer, element.Value, null);
                }
                writer.WriteEndArray();
            }
            else
            {
                var jobj = (JVMObject)value;
                var properties = jobj.Properties;

                writer.WriteStartObject();

                foreach (var property in properties)
                {
                    if(!property.Key.StartsWith("$"))
                    {
                        writer.WritePropertyName(property.Key);
                        object result = jobj.TryGetMember(property.Key);
                        serializer.Serialize(writer, result, null);
                    }
                }

                writer.WriteEndObject();
            }
        }
    }
}