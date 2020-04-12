/*
 * The MIT License (MIT)
 * Copyright (c) Arturo Rodriguez All rights reserved.
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
 
using System;
using System.Collections.Generic;

using System.Reflection;

using Newtonsoft.Json;

namespace QuantApp.Kernel
{
    public class RTDMessage
    {
        public enum MessageType
        {
            Subscribe = 1, 
            //MarketData = 2, StrategyData = 3, UpdateOrder = 4, UpdatePosition = 5, AddNewOrder = 6, AddNewPosition = 7, SavePortfolio = 8, Property = 9, Function = 10,
            UpdateQueue = 11,
            //CreateAccount = 12, CreateSubStrategy = 13,
            CRUD = 14,
            PING = 15,
            // RegisterWorkflow = 16,
            // Call = 17,
            // Response = 18,
            SaveM = 19,
            ProxyOpen = 20,
            ProxyContent = 21,
            ProxyClose = 22
        };

        public enum CRUDType
        {
            Create = 1, Read = 2, Update = 3, Delete = 4
        };

        public class CRUDMessage
        {
            public string TopicID { get; set; }
            public string ID { get; set; }
            public CRUDType Type { get; set; }
            public string Class { get; set; }
            public object Value { get; set; }

            public string ValueType { get; set; }
            public string ValueAssembly { get; set; }
        }

        public MessageType Type { get; set; }
        public object Content { get; set; }

        public int Counter { get; set; }
    }

    public class QueueMessage
    {
        public string ID { get; set; }
        public string TopicID { get; set; }
        public RTDMessage Message { get; set; }
        public string Comment { get; set; }
        public Boolean Executed { get; set; }
        public DateTime CreationTimestamp { get; set; }
        public DateTime ExecutionTimestamp { get; set; }
    }

    public class CallResponseMessage
    {
        public string ID { get; set; }
        public string Function { get; set; }
        public object Data { get; set; }
        public Boolean Executed { get; set; }
        public DateTime CreationTimestamp { get; set; }
        public DateTime ExecutionTimestamp { get; set; }
    }

    public class RTDEngine
    {
        public static Factories.IRTDEngineFactory Factory = null;
        // public static void Send(RTDMessage message)
        public static void Send(object message)
        {

            if (Factory != null)
            {
                Factory.Send(message);                               
            }
        }


        public readonly static object objLockSubscribe = new object();
        private static Dictionary<string, string> _localSubscriptions = new Dictionary<string, string>();
        public static void Subscribe(string topicID)
        {
            lock (objLockSubscribe)
            {
                if (!_localSubscriptions.ContainsKey(topicID))
                    _localSubscriptions.Add(topicID, topicID);

                RTDEngine.Send(new RTDMessage() { Type = RTDMessage.MessageType.Subscribe, Content = topicID });
            }
        }

        public readonly static object objLockQueue = new object();
        public static string AddQueue(QueueMessage message)
        {
            lock (objLockQueue)
            {
                string m_key = "_m_q_i_" + message.TopicID;

                string id = System.Guid.NewGuid().ToString();
                message.ID = id;

                M m = M.Base(m_key);
                m += message;

                if (Factory != null)
                    Factory.Send(new RTDMessage() { Type = RTDMessage.MessageType.UpdateQueue, Content = message.TopicID });


                m.Save();

                if (_localSubscriptions.ContainsKey(message.TopicID))
                   Factory.ProcessMessage(message);

                return id;
            }
        }


        public static void UpdateQueue(QueueMessage message)
        {
            lock (objLockQueue)
            {
                string m_key = "_m_q_i_" + message.TopicID;

                M m = M.Base(m_key);
                var res = m[x => M.V<string>(x, "ID") == message.ID];
                if (res != null && res.Count != 0)
                    foreach (object v in res)
                    {
                        m.Remove(v);
                    }
                m += message;

                m.Save();
            }
        }

        public static void ProcessMessages(string id)
        {
            if (Factory != null)
                Factory.ProcessMessages(id);
        }

        public static void Send(IEnumerable<string> to, string from, string subject, string message)
        {
            if(Factory != null)
                Factory.Send(to, from, subject, message);
        }

        public static List<object> GetQueue(string topicid)
        {
            string m_key = "_m_q_i_" + topicid;

            M m = M.Base(m_key);
            var res = m[x => true];
            return res;
        }

        public static List<object> GetQueue(string topicid, bool executed)
        {
            string m_key = "_m_q_i_" + topicid;

            M m = M.Base(m_key);
            var res = m[x => M.V<Boolean>(x, "Executed") == executed];
            return res;
        }

        public static bool Publish(object data)
        {
            if (Factory != null)
                return Factory.Publish(data);

            return false;
        }



    }
}
