/*
 * The MIT License (MIT)
 * Copyright (c) 2007-2019, Arturo Rodriguez All rights reserved.
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
 
using System;
using System.Collections.Generic;

using System.Reflection;

using Newtonsoft.Json;

namespace AQI.AQILabs.Kernel
{
    public class RTDMessage
    {
        public enum MessageType
        {
            //Subscribe = 1, 
            MarketData = 2, StrategyData = 3, UpdateOrder = 4, UpdatePosition = 5, AddNewOrder = 6, AddNewPosition = 7,
            SavePortfolio = 8,
            Property = 9, Function = 10,
            //UpdateQueue = 11,
            CreateAccount = 12, CreateSubStrategy = 13,
            //CRUD = 14,
            //PING = 15
        };

        public enum OrderType
        {
            Market = 1, Limit = 2
        };

        public enum ReportType
        {
            Submission = 1, Execution = 2
        };

        // public enum CRUDType
        // {
        //     Create = 1, Read = 2, Update = 3, Delete = 4
        // };

        // public class CRUDMessage
        // {
        //     public string TopicID { get; set; }
        //     public string ID { get; set; }
        //     public CRUDType Type { get; set; }
        //     public string Class { get; set; }
        //     public object Value { get; set; }
        // }

        public class CreateAccount
        {
            public string Provider { get; set; }
            public string CurrencyID { get; set; }
            public string AccountID { get; set; }
            public string Username { get; set; }
            public string Password { get; set; }
            public string Key { get; set; }

            public int StrategyID { get; set; }
            public string UserID { get; set; }
            public string AttorneyID { get; set; }

            public string Portfolio { get; set; }
            public string Parameters { get; set; }
        }

        public class CreateSubStrategy
        {
            public int ParentID { get; set; }
            public string Type { get; set; }
        }

        public class MarketData
        {
            public int InstrumentID { get; set; }
            public DateTime Timestamp { get; set; }
            public TimeSeriesType Type { get; set; }
            public double Value { get; set; }
        }

        public class StrategyData
        {
            public int InstrumentID { get; set; }
            public double Value { get; set; }
            public DateTime Timestamp { get; set; }
            public int MemoryTypeID { get; set; }
            public int MemoryClassID { get; set; }
        }

        public class PositionMessage
        {
            public Position Position { get; set; }
            public DateTime Timestamp { get; set; }
            public Boolean AddNew { get; set; }
        }

        public class OrderMessage
        {
            public Order Order { get; set; }
            public Boolean OnlyMemory { get; set; }
        }

        public class PropertyMessage
        {
            public int ID { get; set; }
            public string Name { get; set; }
            public object Value { get; set; }
        }

        public class FunctionMessage
        {
            public int ID { get; set; }
            public string Name { get; set; }
            public object[] Parameters { get; set; }
        }

        public MessageType Type { get; set; }
        public object Content { get; set; }

        public int Counter { get; set; }
    }

    // public class QueueMessage
    // {
    //     public string ID { get; set; }
    //     public string TopicID { get; set; }
    //     public RTDMessage Message { get; set; }
    //     public string Comment { get; set; }
    //     public Boolean Executed { get; set; }
    //     public DateTime CreationTimestamp { get; set; }
    //     public DateTime ExecutionTimestamp { get; set; }
    // }


    public class StrategyRTDEngine
    {
        public delegate Strategy SubmitAccountCreationType(string accountName, Currency ccy, string custodian, string username, string password, string key, string portfolio, string parameters);
        public static SubmitAccountCreationType SubmitAccountCreation = null;

        public delegate Strategy SubmitSubStrategyType(Strategy parent, string type);
        public static SubmitSubStrategyType SubmitSubStrategy = null;
    }

}  
//         public static Factories.IRTDEngineFactory Factory = null;
//         public static void Send(RTDMessage message)
//         {

//             if (Factory != null)
//             {
//                 Factory.Send(message);                               
//             }
//         }


//         public readonly static object objLockSubscribe = new object();
//         private static Dictionary<string, string> _localSubscriptions = new Dictionary<string, string>();
//         public static void Subscribe(string topicID)
//         {
//             lock (objLockSubscribe)
//             {
//                 if (!_localSubscriptions.ContainsKey(topicID))
//                     _localSubscriptions.Add(topicID, topicID);

//                 RTDEngine.Send(new RTDMessage() { Type = RTDMessage.MessageType.Subscribe, Content = topicID });
//             }
//         }

//         public readonly static object objLockQueue = new object();
//         public static string AddQueue(QueueMessage message)
//         {
//             lock (objLockQueue)
//             {
//                 string m_key = "_m_q_i_" + message.TopicID;

//                 string id = System.Guid.NewGuid().ToString();
//                 message.ID = id;

//                 M m = M.Base(m_key);
//                 m += message;

//                 if (Factory != null)
//                     Factory.Send(new RTDMessage() { Type = RTDMessage.MessageType.UpdateQueue, Content = message.TopicID });


//                 m.Save();

//                 if (_localSubscriptions.ContainsKey(message.TopicID))
//                     ProcessMessage(message);

//                 return id;
//             }
//         }


//         public static void UpdateQueue(QueueMessage message)
//         {
//             lock (objLockQueue)
//             {
//                 string m_key = "_m_q_i_" + message.TopicID;

//                 //string id = System.Guid.NewGuid().ToString();
//                 //message.ID = id;

//                 M m = M.Base(m_key);
//                 var res = m[x => M.V<string>(x, "ID") == message.ID];
//                 if (res != null && res.Count != 0)
//                     foreach (object v in res)
//                     {
//                         m.Remove(v);
//                         //QueueMessage oldMessage = m[x => M.V<string>(x, "ID") == message.ID];
//                     }
//                 m += message;

//                 m.Save();

//                 //if (Factory != null)
//                 //    Factory.Send(new RTDMessage() { Type = RTDMessage.MessageType.UpdateQueue, Content = message.InstrumentID });

//                 //return id;
//             }
//         }

//         public static void ProcessMessage(QueueMessage message)
//         {
//             RTDMessage rtd_message = message.Message;

//             if (rtd_message.Type == RTDMessage.MessageType.StrategyData)
//             {
//                 try
//                 {
//                     RTDMessage.StrategyData content = JsonConvert.DeserializeObject<RTDMessage.StrategyData>(rtd_message.Content.ToString());

//                     //string contract = content.InstrumentID.ToString();

//                     Strategy instrument = Instrument.FindInstrument(content.InstrumentID) as Strategy;

//                     instrument.AddMemoryPoint(content.Timestamp, content.Value, content.MemoryTypeID, content.MemoryClassID, false);
//                 }
//                 catch (Exception e)
//                 {
//                     SystemLog.Write(DateTime.Now, null, SystemLog.Type.Production, "Client StrategyData Exception: " + e);
//                 }
//             }
//             else if (rtd_message.Type == RTDMessage.MessageType.Property)
//             {
//                 try
//                 {
//                     RTDMessage.PropertyMessage content = JsonConvert.DeserializeObject<RTDMessage.PropertyMessage>(rtd_message.Content.ToString());
//                     object obj = Instrument.FindInstrument(content.ID);

//                     PropertyInfo prop = obj.GetType().GetProperty(content.Name, BindingFlags.Public | BindingFlags.Instance);
//                     if (null != prop && prop.CanWrite)
//                     {
//                         if (content.Value.GetType() == typeof(Int64))
//                             prop.SetValue(obj, Convert.ToInt32(content.Value), null);
//                         else
//                             prop.SetValue(obj, content.Value, null);
//                     }


//                 }
//                 catch (Exception e)
//                 {
//                     Console.WriteLine(e);
//                 }
//             }
//             else if (rtd_message.Type == RTDMessage.MessageType.Function)
//             {
//                 try
//                 {
//                     RTDMessage.FunctionMessage content = JsonConvert.DeserializeObject<RTDMessage.FunctionMessage>(rtd_message.Content.ToString());
//                     object obj = Instrument.FindInstrument(content.ID);

//                     MethodInfo method = obj.GetType().GetMethod(content.Name);
//                     if (null != method)
//                         method.Invoke(obj, content.Parameters);
//                 }
//                 catch (Exception e)
//                 {
//                     Console.WriteLine(e);
//                 }
//             }
//             else if (rtd_message.Type == RTDMessage.MessageType.CreateAccount)
//             {
//                 try
//                 {

//                     RTDMessage.CreateAccount content = (rtd_message.Content.GetType() == typeof(string)) ? JsonConvert.DeserializeObject<RTDMessage.CreateAccount>(rtd_message.Content.ToString()) : (RTDMessage.CreateAccount)rtd_message.Content;
//                     Currency ccy = Currency.FindCurrency(content.CurrencyID);
//                     string provider = content.Provider;
//                     string accountID = content.AccountID;
//                     string username = content.Username;
//                     string password = content.Password;
//                     string key = content.Key;
//                     string portfolio = content.Portfolio;
//                     string parameters = content.Parameters;
//                     Strategy s = RTDEngine.SubmitAccountCreation(accountID, ccy, provider, username, password, key, portfolio, parameters);

//                     if (s != null)
//                     {
//                         content.StrategyID = s.ID;
//                         content.AttorneyID = User.CurrentUser.ID;
//                     }

//                     if (Factory != null)
//                         Factory.Send(new RTDMessage() { Type = RTDMessage.MessageType.CreateAccount, Content = content });
//                 }
//                 catch (Exception e)
//                 {
//                     Console.WriteLine(e);
//                 }
//             }
//             else if (rtd_message.Type == RTDMessage.MessageType.CreateSubStrategy)
//             {
//                 try
//                 {
//                     RTDMessage.CreateSubStrategy content = (RTDMessage.CreateSubStrategy)rtd_message.Content;
//                     int parentID = content.ParentID;
//                     string type = content.Type;

//                     Strategy s = Instrument.FindInstrument(parentID) as Strategy;
//                     if (s != null && SubmitSubStrategy != null)
//                         SubmitSubStrategy(s, type);

//                     //Strategy s = Market.SubmitAccountCreation(accountID, ccy, provider);

//                     //if (s != null)
//                     //{
//                     //    content.StrategyID = s.ID;
//                     //    content.AttorneyID = User.CurrentUser.ID;
//                     //}

//                     //if (Factory != null)
//                     //    Factory.Send(new RTDMessage() { Type = RTDMessage.MessageType.CreateAccount, Content = content });
//                 }
//                 catch (Exception e)
//                 {
//                     Console.WriteLine(e);
//                 }
//             }

//             message.ExecutionTimestamp = DateTime.Now;
//             message.Executed = true;

//             if (Factory != null)
//                 Factory.Send(new RTDMessage() { Type = RTDMessage.MessageType.UpdateQueue, Content = message });
//         }

//         public static void ProcessMessages(string id)
//         {
//             if (Factory != null)
//                 Factory.ProcessMessages(id);
//         }

//         public static void Send(IEnumerable<string> to, string from, string subject, string message)
//         {
//             if(Factory != null)
//                 Factory.Send(to, from, subject, message);
//         }

//         public static QueueMessage GetQueue(Instrument instrument, string ID)
//         {
//             string m_key = "_m_q_i_" + instrument.ID;

//             M m = M.Base(m_key);
//             var res = m[x => M.V<string>(x, "ID") == ID];

//             if (res != null && res.Count != 0)
//                 return (QueueMessage)res[0];
//             else
//                 return null;
//         }
//         public static List<object> GetQueue(Instrument instrument)
//         {
//             string m_key = "_m_q_i_" + instrument.ID;

//             M m = M.Base(m_key);
//             var res = m[x => true];
//             return res;
//         }

//         public static List<object> GetQueue(Instrument instrument, bool executed)
//         {
//             string m_key = "_m_q_i_" + instrument.ID;

//             M m = M.Base(m_key);
//             var res = m[x => M.V<Boolean>(x, "Executed") == executed];
//             return res;
//         }

//         public static List<object> GetQueue(string topicid)
//         {
//             string m_key = "_m_q_i_" + topicid;

//             M m = M.Base(m_key);
//             var res = m[x => true];
//             return res;
//         }

//         public static List<object> GetQueue(string topicid, bool executed)
//         {
//             string m_key = "_m_q_i_" + topicid;

//             M m = M.Base(m_key);
//             var res = m[x => M.V<Boolean>(x, "Executed") == executed];
//             return res;
//         }

//         public static bool Publish(Instrument instrument)
//         {
//             if (Factory != null)
//                 return Factory.Publish(instrument);

//             return false;
//         }
//     }
// }
