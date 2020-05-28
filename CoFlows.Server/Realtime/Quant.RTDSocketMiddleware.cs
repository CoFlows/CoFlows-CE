/*
 * The MIT License (MIT)
 * Copyright (c) Arturo Rodriguez All rights reserved.
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
 
using System;
using System.Text;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Net.Http;
using System.Net.Http.Headers;

using System.IO;
using System.IO.Compression;

using System.Linq;

using System.Reflection;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Newtonsoft.Json;

using QuantApp.Kernel;
using AQI.AQILabs.Kernel;
using AQI.AQILabs.Kernel.Factories;

using QuantApp.Kernel;
using QuantApp.Engine;
using Python.Runtime;


using CoFlows.Server.Utils;
using CoFlows.Server.Realtime;

namespace CoFlows.Server.Realtime
{
    public class HttpProxyRequest
    {
        public string Url { get; set; }
        public string Content { get; set;}
        public List<KeyValuePair<string, string>> Headers { get; set; }
    }
    public class RTDSocketMiddleware
    {
        private readonly RequestDelegate _next;
        internal readonly RTDSocketManager _socketManager;
        internal static ConcurrentDictionary<string, WebSocket> _proxies = new ConcurrentDictionary<string, WebSocket>();


        public RTDSocketMiddleware(RequestDelegate next,  RTDSocketManager socketManager)
        {
            _next = next;
            _socketManager = socketManager;

            WebSocketListner.manager = socketManager;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                if (!context.WebSockets.IsWebSocketRequest)
                {
                    try
                    {
                        await _next.Invoke(context);
                    }
                    catch{}
                    return;
                }

                QuantApp.Kernel.User quser = null;

                if(!context.User.Identity.IsAuthenticated)
                {
                    string cokey = context.Request.Cookies["coflows"]; 
                    if(cokey != null)
                    {
                        if(CoFlows.Server.Controllers.AccountController.sessionKeys.ContainsKey(cokey))
                        {
                            quser = QuantApp.Kernel.User.FindUserBySecret(CoFlows.Server.Controllers.AccountController.sessionKeys[cokey]);
                            if(quser == null)
                            {
                                await _next.Invoke(context);
                                return;
                            }                    
                        }
                        else
                        {
                            await _next.Invoke(context);
                            return;
                        }
                    }
                    else
                    {
                        await _next.Invoke(context);
                        return;
                    }
                }

                var queryString = context.Request.QueryString;
                var path = context.Request.Path.ToString() + queryString;

                
                var headers = new List<KeyValuePair<string, string>>();

                foreach(var head in context.Request.Headers)
                {
                    foreach(var val in head.Value)
                    {
                        try
                        {
                            headers.Add(new KeyValuePair<string, string>(head.Key, val.Replace("%7C", "|")));
                        }
                        catch{}
                    }
                }

                if(path.StartsWith("/lab/"))
                {
                    var wid = path.Replace("/lab/", "");
                    wid = wid.Substring(0, wid.IndexOf("/"));

                    if(CoFlows.Server.Realtime.WebSocketListner.registered_workspaces_id.ContainsKey(wid))
                    {
                        var sid = CoFlows.Server.Realtime.WebSocketListner.registered_workspaces_id[wid];
                        var _socket = WebSocketListner.registered_sockets[sid];

                        var mess = new QuantApp.Kernel.RTDMessage { 
                            Type = QuantApp.Kernel.RTDMessage.MessageType.ProxyOpen, 
                            Content = new HttpProxyRequest { 
                                Url = path, 
                                Content = "ws://localhost:8888",
                                Headers = headers
                                } 
                            };

                        var mess_str = Newtonsoft.Json.JsonConvert.SerializeObject(mess);

                        ArraySegment<byte> buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(mess_str));
                        _socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                }

                var socket = await context.WebSockets.AcceptWebSocketAsync();
                var id = _socketManager.AddSocket(socket);
                var address = context.Connection.RemoteIpAddress;
                

                if(WebSocketListner.registered_address.ContainsKey(id))
                    WebSocketListner.registered_address[id] = address;
                else
                    WebSocketListner.registered_address.TryAdd(id, address);

                if(WebSocketListner.registered_sockets.ContainsKey(id))
                    WebSocketListner.registered_sockets[id] = socket;
                else
                    WebSocketListner.registered_sockets.TryAdd(id, socket);
                    
                await Receive(socket, async (result, length, buffer) =>
                {
                    if(quser != null)
                        QuantApp.Kernel.User.ContextUser = quser.ToUserData();

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await _socketManager.RemoveSocket(id);
                        QuantApp.Kernel.User.ContextUser = new UserData();
                        return;
                    }
                    else if (result.MessageType == WebSocketMessageType.Text)
                    {
                        string userMessage = Encoding.UTF8.GetString(buffer, 0, length);
                        WebSocketListner.appServer_NewMessageReceived(socket, userMessage, path, headers);
                        QuantApp.Kernel.User.ContextUser = new UserData();
                        return;
                    }
                    else
                        Console.WriteLine("REC BIN1: " + result.MessageType);

                    QuantApp.Kernel.User.ContextUser = new UserData();

                });
                
            }
            catch(Exception e)
            {
                Console.WriteLine("Invoke " + e);
                Console.WriteLine(e.StackTrace);
            }
        }

        private async Task Receive(WebSocket socket, Action<WebSocketReceiveResult, int, byte[]> handleMessage)
        {
            while (socket.State == WebSocketState.Open)
            {
                try
                {
                    int maxSize = 1024 * 2000000;
                    int bufferSize = 500;
                    int increaseSize = 1024 * 10;
                    var buffer = new byte[bufferSize];
                    var offset = 0;
                    var free = buffer.Length;
                    WebSocketReceiveResult result = null;
                    int counter = 0;
                    while (true)
                    {

                        result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer, offset, free), cancellationToken: CancellationToken.None);
                        offset += result.Count;
                        free -= result.Count;
                        if (result.EndOfMessage) 
                        {
                            // if(counter > 0)
                                // Console.WriteLine("done: " + offset + " " + free);
                            break;
                        }
                        if (free == 0)
                        {
                            if(counter > 5)
                                increaseSize = 1024 * 100;

                            else if(counter > 10)
                                increaseSize = 1024 * 1000;

                            else if(counter > 20)
                                increaseSize = 1024 * 10000;

                            else if(counter > 30)
                                increaseSize = 1024 * 100000;

                            else if(counter > 40)
                                increaseSize = 1024 * 1000000;
                            // No free space
                            // Resize the outgoing buffer
                            var newSize = buffer.Length + increaseSize;
                            // Console.WriteLine("more data: " + offset + " " + newSize);
                            
                            // Check if the new size exceeds a 
                            
                            // It should suit the data it receives
                            // This limit however has a max value of 2 billion bytes (2 GB)
                            if (newSize > maxSize)
                                throw new Exception ("Maximum size exceeded");
                            
                            var newBuffer = new byte[newSize];
                            Array.Copy(buffer, 0, newBuffer, 0, offset);
                            buffer = newBuffer;
                            free = buffer.Length - offset;
                            counter++;
                        }
                    }
                    // var result = await socket.ReceiveAsync(buffer: new ArraySegment<byte>(buffer), cancellationToken: CancellationToken.None);

                    try
                    {
                        handleMessage(result, offset, buffer);
                    }
                    catch(Exception t)
                    {
                        Console.WriteLine(t);
                    }
                }
                catch (Exception e)
                {
                    var id = WebSocketListner.manager.GetId(socket);
                    if(WebSocketListner.registered_id_workspaces.ContainsKey(id))
                    {
                        var wid = WebSocketListner.registered_id_workspaces[id];
                        try
                        {
                            var wsp_ais = QuantApp.Kernel.M.Base(wid)[x => true].FirstOrDefault() as Workflow;
                            foreach(var fid in wsp_ais.Agents)
                            {
                                var f = F.Find(fid).Value;
                                f.RemoteStop();
                            }
                        }
                        catch{}

                        string none = "";
                        WebSocketListner.registered_id_workspaces.TryRemove(id, out none);

                        if(WebSocketListner.registered_workspaces_id.ContainsKey(wid))
                            WebSocketListner.registered_workspaces_id.TryRemove(wid, out none);
                        
                    }

                    if(WebSocketListner.registered_address.ContainsKey(id))
                    {
                        System.Net.IPAddress ip = null;
                        WebSocketListner.registered_address.TryRemove(id, out ip);
                    }

                    break;
                }
            }
        }
    }

    public delegate System.Tuple<string, string> RTDMessageDelegate(string content);

    public class WebSocketListner : QuantApp.Kernel.Factories.IRTDEngineFactory
    {
        public static ConcurrentDictionary<string, ConcurrentDictionary<string, WebSocket>> subscriptions = new ConcurrentDictionary<string, ConcurrentDictionary<string, WebSocket>>();
        public static ConcurrentDictionary<string, ConcurrentDictionary<string, QuantApp.Kernel.UserData>> users = new ConcurrentDictionary<string, ConcurrentDictionary<string, QuantApp.Kernel.UserData>>();
        
        public static ConcurrentDictionary<string, string> registered_id_workspaces = new ConcurrentDictionary<string, string>();
        public static ConcurrentDictionary<string, string> registered_workspaces_id = new ConcurrentDictionary<string, string>();
        public static ConcurrentDictionary<string, System.Net.IPAddress> registered_address = new ConcurrentDictionary<string, System.Net.IPAddress>();
        public static ConcurrentDictionary<string, WebSocket> registered_sockets = new ConcurrentDictionary<string, WebSocket>();
        public static ConcurrentDictionary<string, string> traders = new ConcurrentDictionary<string, string>();

        public static RTDSocketManager manager = null;

        public readonly static object objLock = new object();

        private ClientWebSocket _socket = null;

        public WebSocketListner(){ }

        public WebSocketListner(ClientWebSocket _socket){ this._socket = _socket; }

        static int counter = 0;

        private static System.Collections.Concurrent.ConcurrentDictionary<string, int> DashDB = new System.Collections.Concurrent.ConcurrentDictionary<string, int>();
        // private static int lastPort = 10000;

        public static RTDMessageDelegate RTDMessageFunction = null;

        public static void appServer_NewMessageReceived(WebSocket session, string message_string, string path, List<KeyValuePair<string, string>> headers)
        {
            try
            {
                string skey = manager == null ? null : manager.GetId(session);
                if (!string.IsNullOrWhiteSpace(message_string))
                {
                    DateTime t1 = DateTime.Now;

                    QuantApp.Kernel.RTDMessage message = null;

                    if(path.StartsWith("/lab/"))
                    {
                        var sessionID = session.GetHashCode() + path;
                        if(RTDSocketMiddleware._proxies.ContainsKey(sessionID))
                        {
                            // Console.WriteLine("Got from browser and send(" + DateTime.Now.ToString("hh:mm:ss.fff") + "): " + sessionID);// + message_string);
                            var _socket = RTDSocketMiddleware._proxies[sessionID];

                            var mess = new QuantApp.Kernel.RTDMessage { Type = QuantApp.Kernel.RTDMessage.MessageType.ProxyContent, Content = new HttpProxyRequest { Url = path, Content = message_string } };
                            var mess_str = Newtonsoft.Json.JsonConvert.SerializeObject(mess);

                            ArraySegment<byte> buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(mess_str));
                            _socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
                        }
                        else
                             Console.WriteLine("Socket Not Found(" + path + "): " + message_string);
                    }
                    
                    else
                    {
                        try
                        {
                            message = JsonConvert.DeserializeObject<QuantApp.Kernel.RTDMessage>(message_string);
                        }
                        catch{}

                        if (message != null)
                        {
                            if (message.Type == QuantApp.Kernel.RTDMessage.MessageType.Subscribe)
                            {
                                try
                                {
                                    string contract = message.Content.ToString();

                                    if (contract.StartsWith("$"))
                                    {
                                        contract = contract.Substring(1, contract.Length - 1);
                                        if (!traders.ContainsKey(contract))
                                        {
                                            traders.TryAdd(contract, skey);
                                            Send(session, message_string);
                                        }
                                    }

                                    

                                    if (!subscriptions.ContainsKey(contract))
                                        subscriptions.TryAdd(contract, new ConcurrentDictionary<string, WebSocket>());

                                    if (!subscriptions[contract].ContainsKey(skey))
                                        subscriptions[contract].TryAdd(skey, session);

                                    if (!users.ContainsKey(contract))
                                        users.TryAdd(contract, new ConcurrentDictionary<string, QuantApp.Kernel.UserData>());

                                    if (!users[contract].ContainsKey(skey))
                                        users[contract].TryAdd(skey, QuantApp.Kernel.User.ContextUser);

                                    // Console.WriteLine("Subscribed: " + skey + " -- " + contract);

                                    int id = -1;
                                    int.TryParse(contract, out id);
                                    if (id > 0)
                                    {
                                        var instrument = AQI.AQILabs.Kernel.Instrument.FindInstrument(id);

                                        if(instrument is AQI.AQILabs.Kernel.Strategy)
                                        {
                                            var strategy = instrument as AQI.AQILabs.Kernel.Strategy;
                                            strategy.Tree.Initialize();
                                            strategy.Tree.LoadPortfolioMemory();
                                        }

                                        var t = DateTime.Now;

                                        var last = instrument[t, AQI.AQILabs.Kernel.TimeSeriesType.Last, AQI.AQILabs.Kernel.TimeSeriesRollType.Last];

                                        if (QuantApp.Kernel.RTDEngine.Publish(instrument))
                                            QuantApp.Kernel.RTDEngine.Send(new AQI.AQILabs.Kernel.RTDMessage() { Type = AQI.AQILabs.Kernel.RTDMessage.MessageType.MarketData, Content = new AQI.AQILabs.Kernel.RTDMessage.MarketData() { InstrumentID = instrument.ID, Value = last, Type = AQI.AQILabs.Kernel.TimeSeriesType.Last, Timestamp = DateTime.Now } });

                                    }
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine("Subsribe Exception: " + e + " " + skey);
                                }
                            }
                            // else if (message.Type == QuantApp.Kernel.RTDMessage.MessageType.RegisterWorkspace)
                            // {
                            //     try
                            //     {
                                    
                            //         string workspace = message.Content.ToString();
                                    
                            //         Program.AddServicedWorkSpaces(workspace);

                            //         if (!registered_id_workspaces.ContainsKey(skey))
                            //             registered_id_workspaces.TryAdd(skey, workspace);

                            //         if (!registered_workspaces_id.ContainsKey(workspace))
                            //             registered_workspaces_id.TryAdd(workspace, skey);

                            //         Console.WriteLine("Register: " + skey + " -- " + workspace);
                            //     }
                            //     catch (Exception e)
                            //     {
                            //         Console.WriteLine("Register Exception: " + e + " " + skey);
                            //     }
                            // }

                            else if (message.Type == QuantApp.Kernel.RTDMessage.MessageType.SaveM)
                            {
                                try
                                {
                                    
                                    string mid = message.Content.ToString();
                                    
                                    QuantApp.Kernel.M m = QuantApp.Kernel.M.Base(mid);
                                    m.Save();

                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine("SaveM Exception: " + e + " " + skey);
                                }
                            }
                            else if (message.Type == QuantApp.Kernel.RTDMessage.MessageType.PING)
                            {
                                try
                                {
                                    DateTime stamp = (DateTime)message.Content;

                                    var response = JsonConvert.SerializeObject(new QuantApp.Kernel.RTDMessage() { Type = QuantApp.Kernel.RTDMessage.MessageType.PING, Content = DateTime.Now });
                                    Send(session, response);
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine("MarketData Exception: " + e + " " + skey);
                                }
                            }
                            else if (message.Type == QuantApp.Kernel.RTDMessage.MessageType.UpdateQueue)
                            {
                                QuantApp.Kernel.QueueMessage qm = JsonConvert.DeserializeObject<QuantApp.Kernel.QueueMessage>(message.Content.ToString());

                                QuantApp.Kernel.RTDEngine.UpdateQueue(qm);

                                Share(session, qm.TopicID, message_string);
                            }
                            else if (message.Type == QuantApp.Kernel.RTDMessage.MessageType.CRUD)
                            {
                                QuantApp.Kernel.RTDMessage.CRUDMessage qm = JsonConvert.DeserializeObject<QuantApp.Kernel.RTDMessage.CRUDMessage>(message.Content.ToString());

                                try
                                {
                                    var type = Type.GetType(qm.ValueType);
                                    if(type == null)
                                    {
                                        Assembly assembly = QuantApp.Kernel.M._systemAssemblies.ContainsKey(qm.ValueType) ? QuantApp.Kernel.M._systemAssemblies[qm.ValueType] : (QuantApp.Kernel.M._compiledAssemblies.ContainsKey(qm.ValueType) ? QuantApp.Kernel.M._compiledAssemblies[qm.ValueType] : System.Reflection.Assembly.Load(qm.ValueAssembly));
                                        type = assembly.GetType(QuantApp.Kernel.M._systemAssemblyNames.ContainsKey(qm.ValueType) ? QuantApp.Kernel.M._systemAssemblyNames[qm.ValueType] : (QuantApp.Kernel.M._compiledAssemblyNames.ContainsKey(qm.ValueType) ? QuantApp.Kernel.M._compiledAssemblyNames[qm.ValueType] : qm.ValueType));
                                    }

                                    string filtered_string =  qm.Value.ToString().Replace((char)27, '"').Replace((char)26, '\'');
                                    if(filtered_string.StartsWith("\"") && filtered_string.EndsWith("\""))
                                        filtered_string = filtered_string.Substring(1, filtered_string.Length - 2).Replace("\\\"", "\"");

                                    if(type == typeof(string) || qm.ValueType == null || type == typeof(Nullable))
                                        qm.Value = filtered_string;

                                    else if(type != null)//((QuantApp.Kernel.M._systemAssemblies.ContainsKey(qm.ValueType) || QuantApp.Kernel.M._compiledAssemblies.ContainsKey(qm.ValueType)))
                                        qm.Value = JsonConvert.DeserializeObject(filtered_string, type);
                                    
                                }
                                catch {}

                                if (qm.Class == QuantApp.Kernel.M.CRUDClass)
                                    QuantApp.Kernel.M.Base(qm.TopicID).Process(qm);

                                Share(session, qm.TopicID, message_string);
                            }

                            else if (message.Type == QuantApp.Kernel.RTDMessage.MessageType.ProxyOpen)
                            {
                                var pd = JsonConvert.DeserializeObject<HttpProxyRequest>(message.Content.ToString());
                                var client = ProxyConnection.Client(session,  pd.Url);
                                client.Connect(pd.Content, pd.Headers);

                            }
                            else if (message.Type == QuantApp.Kernel.RTDMessage.MessageType.ProxyContent)
                            {
                                var pd = JsonConvert.DeserializeObject<HttpProxyRequest>(message.Content.ToString());
                                
                                var sessionId = session.GetHashCode() + pd.Url;
                                if(RTDSocketMiddleware._proxies.ContainsKey(sessionId))
                                {
                                    var client = RTDSocketMiddleware._proxies[sessionId];
                                    ArraySegment<byte> buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(pd.Content));
                                    client.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
                                }
                                else
                                {
                                    var client = ProxyConnection.Client(session, pd.Url);
                                    client.Send(pd.Content);
                                }
                            }

                            else if ((int)message.Type == (int)AQI.AQILabs.Kernel.RTDMessage.MessageType.MarketData)
                            {
                                try
                                {
                                    AQI.AQILabs.Kernel.RTDMessage.MarketData content = JsonConvert.DeserializeObject<AQI.AQILabs.Kernel.RTDMessage.MarketData>(message.Content.ToString());

                                    AQI.AQILabs.Kernel.Instrument instrument = AQI.AQILabs.Kernel.Instrument.FindInstrument(content.InstrumentID);

                                    DateTime stamp = content.Timestamp;
                                    if (content.Value != 0)
                                        instrument.AddTimeSeriesPoint(stamp, content.Value, content.Type, AQI.AQILabs.Kernel.DataProvider.DefaultProvider, true, false);

                                    
                                    Share(session, instrument.ID.ToString(), message_string);

                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine("MarketData Exception: " + e + " " + skey);
                                }
                            }
                            else if ((int)message.Type == (int)AQI.AQILabs.Kernel.RTDMessage.MessageType.StrategyData)
                            {
                                try
                                {
                                    AQI.AQILabs.Kernel.RTDMessage.StrategyData content = JsonConvert.DeserializeObject<AQI.AQILabs.Kernel.RTDMessage.StrategyData>(message.Content.ToString());

                                    AQI.AQILabs.Kernel.Strategy instrument = AQI.AQILabs.Kernel.Instrument.FindInstrument(content.InstrumentID) as AQI.AQILabs.Kernel.Strategy;

                                    instrument.AddMemoryPoint(content.Timestamp, content.Value, content.MemoryTypeID, content.MemoryClassID, true, false);

                                    Share(session, instrument.ID.ToString(), message_string);
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine("StrategyData Exception: " + e + " " + skey);
                                }
                            }
                            else if ((int)message.Type == (int)AQI.AQILabs.Kernel.RTDMessage.MessageType.CreateAccount)
                            {
                                try
                                {
                                    AQI.AQILabs.Kernel.RTDMessage.CreateAccount content = JsonConvert.DeserializeObject<AQI.AQILabs.Kernel.RTDMessage.CreateAccount>(message.Content.ToString());

                                    int id = content.StrategyID;

                                    AQI.AQILabs.Kernel.Strategy s = AQI.AQILabs.Kernel.Instrument.FindInstrument(id) as AQI.AQILabs.Kernel.Strategy;
                                    if (s != null)
                                    {
                                        QuantApp.Kernel.User user = QuantApp.Kernel.User.FindUser(content.UserID);
                                        QuantApp.Kernel.User attorney = content.AttorneyID == null ? null : QuantApp.Kernel.User.FindUser(content.AttorneyID);
                                        if (user != null && attorney != null)
                                        {
                                            List<PALMPending> pendings = PALM.GetPending(user);
                                            foreach (PALMPending pending in pendings)
                                            {
                                                if (pending.AccountID == content.AccountID)
                                                {

                                                    pending.Strategy = s;
                                                    pending.CreationDate = s.CreateTime;
                                                    pending.Attorney = attorney;
                                                    PALM.UpdatePending(pending);
                                                    PALM.AddStrategy(pending.User, pending.Attorney, s);
                                                }
                                            }
                                        }
                                    }

                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine("Create Account Exception: " + e + " " + skey);
                                }
                            }
                            else if ((int)message.Type == (int)AQI.AQILabs.Kernel.RTDMessage.MessageType.UpdateOrder)
                            {
                                try
                                {
                                    AQI.AQILabs.Kernel.RTDMessage.OrderMessage content = JsonConvert.DeserializeObject<AQI.AQILabs.Kernel.RTDMessage.OrderMessage>(message.Content.ToString());

                                    content.Order.Portfolio.UpdateOrder(content.Order, content.OnlyMemory, false);

                                    Share(session, content.Order.Portfolio.ID.ToString(), message_string);
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine("UpdateOrder Exception: " + e + " " + skey);
                                }
                            }
                            else if ((int)message.Type == (int)AQI.AQILabs.Kernel.RTDMessage.MessageType.UpdatePosition)
                            {
                                try
                                {
                                    AQI.AQILabs.Kernel.RTDMessage.PositionMessage content = JsonConvert.DeserializeObject<AQI.AQILabs.Kernel.RTDMessage.PositionMessage>(message.Content.ToString());

                                    content.Position.Portfolio.UpdatePositionMemory(content.Position, content.Timestamp, content.AddNew, true, false);

                                    Share(session, content.Position.Portfolio.ID.ToString(), message_string);

                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine("UpdatePosition Exception: " + e + " --- " + e.StackTrace + " " + skey);
                                }
                            }
                            else if ((int)message.Type == (int)AQI.AQILabs.Kernel.RTDMessage.MessageType.AddNewOrder)
                            {
                                try
                                {
                                    AQI.AQILabs.Kernel.Order content = JsonConvert.DeserializeObject<AQI.AQILabs.Kernel.Order>(message.Content.ToString());

                                    content.Portfolio.AddOrderMemory(content);

                                    Share(session, content.Portfolio.ID.ToString(), message_string);
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine("AddNewOrder Exception: " + e + " " + skey);
                                }
                            }
                            else if ((int)message.Type == (int)AQI.AQILabs.Kernel.RTDMessage.MessageType.AddNewPosition)
                            {
                                try
                                {
                                    AQI.AQILabs.Kernel.Position content = JsonConvert.DeserializeObject<AQI.AQILabs.Kernel.Position>(message.Content.ToString());

                                    content.Portfolio.UpdatePositionMemory(content, content.Timestamp, true, true, false);

                                    Share(session, content.Portfolio.ID.ToString(), message_string);
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine("AddNewPosition Exception: " + e + " " + e.StackTrace + " " + skey);
                                }
                            }
                            else if ((int)message.Type == (int)AQI.AQILabs.Kernel.RTDMessage.MessageType.SavePortfolio)
                            {
                                try
                                {
                                    int content = JsonConvert.DeserializeObject<int>(message.Content.ToString());

                                    (AQI.AQILabs.Kernel.Instrument.FindInstrument(content) as AQI.AQILabs.Kernel.Portfolio).SaveNewPositions();
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e);
                                }
                            }
                            else if ((int)message.Type == (int)AQI.AQILabs.Kernel.RTDMessage.MessageType.Property)
                            {
                                try
                                {
                                    AQI.AQILabs.Kernel.RTDMessage.PropertyMessage content = JsonConvert.DeserializeObject<AQI.AQILabs.Kernel.RTDMessage.PropertyMessage>(message.Content.ToString());

                                    object obj = AQI.AQILabs.Kernel.Instrument.FindInstrument(content.ID);

                                    PropertyInfo prop = obj.GetType().GetProperty(content.Name, BindingFlags.Public | BindingFlags.Instance);
                                    if (null != prop && prop.CanWrite)
                                    {
                                        if (content.Value.GetType() == typeof(Int64))
                                            prop.SetValue(obj, Convert.ToInt32(content.Value), null);
                                        else
                                            prop.SetValue(obj, content.Value, null);
                                    }


                                    AQI.AQILabs.Kernel.Instrument instrument = obj as AQI.AQILabs.Kernel.Instrument;
                                    Share(session, instrument.ID.ToString(), message_string);
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e);
                                }
                            }
                            else if ((int)message.Type == (int)AQI.AQILabs.Kernel.RTDMessage.MessageType.Function)
                            {
                                try
                                {
                                    AQI.AQILabs.Kernel.RTDMessage.FunctionMessage content = JsonConvert.DeserializeObject<AQI.AQILabs.Kernel.RTDMessage.FunctionMessage>(message.Content.ToString());
                                    object obj = AQI.AQILabs.Kernel.Instrument.FindInstrument(content.ID);

                                    MethodInfo method = obj.GetType().GetMethod(content.Name);
                                    if (null != method)
                                        method.Invoke(obj, content.Parameters);


                                    AQI.AQILabs.Kernel.Instrument instrument = obj as AQI.AQILabs.Kernel.Instrument;

                                    Share(session, instrument.ID.ToString(), message_string);
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e);
                                }
                            }

                            else if(RTDMessageFunction != null)
                            {
                                var mess = RTDMessageFunction(message_string);
                                if(mess != null)
                                    Share(session, mess.Item1, mess.Item2);
                            }
                        }

                        else
                        {
                            Console.WriteLine("UNKNOWN(" + path + "): " + message_string);
                        }
                        
                    }

                    DateTime t2 = DateTime.Now;
                    //Console.WriteLine("Converting: (" + (t2 - t1) + ")" + counter + "-" + message.Counter + "/" + message_string);
                    counter++;
                }
                else
                    Console.WriteLine("--------------EMPTY STRING: " + path);
                
            
            }
            catch (Exception e)
            {
                Console.WriteLine("Server Receive Message Exception: " + e);
            }
        }

        public void ProcessMessage(QuantApp.Kernel.QueueMessage message)
        {
            QuantApp.Kernel.RTDMessage rtd_message = message.Message;

            if ((int)rtd_message.Type == (int)AQI.AQILabs.Kernel.RTDMessage.MessageType.StrategyData)
            {
                try
                {
                    AQI.AQILabs.Kernel.RTDMessage.StrategyData content = JsonConvert.DeserializeObject<AQI.AQILabs.Kernel.RTDMessage.StrategyData>(rtd_message.Content.ToString());
                    AQI.AQILabs.Kernel.Strategy instrument = AQI.AQILabs.Kernel.Instrument.FindInstrument(content.InstrumentID) as AQI.AQILabs.Kernel.Strategy;

                    instrument.AddMemoryPoint(content.Timestamp, content.Value, content.MemoryTypeID, content.MemoryClassID, false);
                }
                catch (Exception e)
                {
                    //SystemLog.WriteLine(DateTime.Now, null, SystemLog.Type.Production, "Client StrategyData Exception: " + e);
                }
            }
            else if ((int)rtd_message.Type == (int)AQI.AQILabs.Kernel.RTDMessage.MessageType.Property)
            {
                try
                {
                    AQI.AQILabs.Kernel.RTDMessage.PropertyMessage content = JsonConvert.DeserializeObject<AQI.AQILabs.Kernel.RTDMessage.PropertyMessage>(rtd_message.Content.ToString());
                    object obj = AQI.AQILabs.Kernel.Instrument.FindInstrument(content.ID);

                    PropertyInfo prop = obj.GetType().GetProperty(content.Name, BindingFlags.Public | BindingFlags.Instance);
                    if (null != prop && prop.CanWrite)
                    {
                        if (content.Value.GetType() == typeof(Int64))
                            prop.SetValue(obj, Convert.ToInt32(content.Value), null);
                        else
                            prop.SetValue(obj, content.Value, null);
                    }


                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            else if ((int)rtd_message.Type == (int)AQI.AQILabs.Kernel.RTDMessage.MessageType.Function)
            {
                try
                {
                    AQI.AQILabs.Kernel.RTDMessage.FunctionMessage content = JsonConvert.DeserializeObject<AQI.AQILabs.Kernel.RTDMessage.FunctionMessage>(rtd_message.Content.ToString());
                    object obj = AQI.AQILabs.Kernel.Instrument.FindInstrument(content.ID);

                    MethodInfo method = obj.GetType().GetMethod(content.Name);
                    if (null != method)
                        method.Invoke(obj, content.Parameters);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            else if ((int)rtd_message.Type == (int)AQI.AQILabs.Kernel.RTDMessage.MessageType.CreateAccount)
            {
                try
                {

                    AQI.AQILabs.Kernel.RTDMessage.CreateAccount content = (rtd_message.Content.GetType() == typeof(string)) ? JsonConvert.DeserializeObject<AQI.AQILabs.Kernel.RTDMessage.CreateAccount>(rtd_message.Content.ToString()) : (AQI.AQILabs.Kernel.RTDMessage.CreateAccount)rtd_message.Content;
                    AQI.AQILabs.Kernel.Currency ccy = AQI.AQILabs.Kernel.Currency.FindCurrency(content.CurrencyID);
                    string provider = content.Provider;
                    string accountID = content.AccountID;
                    string username = content.Username;
                    string password = content.Password;
                    string key = content.Key;
                    string portfolio = content.Portfolio;
                    string parameters = content.Parameters;
                    AQI.AQILabs.Kernel.Strategy s = AQI.AQILabs.Kernel.StrategyRTDEngine.SubmitAccountCreation(accountID, ccy, provider, username, password, key, portfolio, parameters);

                    if (s != null)
                    {
                        content.StrategyID = s.ID;
                        content.AttorneyID = QuantApp.Kernel.User.CurrentUser.ID;
                    }

                    Send(new AQI.AQILabs.Kernel.RTDMessage() { Type = AQI.AQILabs.Kernel.RTDMessage.MessageType.CreateAccount, Content = content });
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            else if ((int)rtd_message.Type == (int)AQI.AQILabs.Kernel.RTDMessage.MessageType.CreateSubStrategy)
            {
                try
                {
                    AQI.AQILabs.Kernel.RTDMessage.CreateSubStrategy content = (AQI.AQILabs.Kernel.RTDMessage.CreateSubStrategy)rtd_message.Content;
                    int parentID = content.ParentID;
                    string type = content.Type;

                    AQI.AQILabs.Kernel.Strategy s = AQI.AQILabs.Kernel.Instrument.FindInstrument(parentID) as AQI.AQILabs.Kernel.Strategy;
                    if (s != null && AQI.AQILabs.Kernel.StrategyRTDEngine.SubmitSubStrategy != null)
                        AQI.AQILabs.Kernel.StrategyRTDEngine.SubmitSubStrategy(s, type);

                    //Strategy s = Market.SubmitAccountCreation(accountID, ccy, provider);

                    //if (s != null)
                    //{
                    //    content.StrategyID = s.ID;
                    //    content.AttorneyID = User.CurrentUser.ID;
                    //}

                    //if (Factory != null)
                    //    Factory.Send(new RTDMessage() { Type = RTDMessage.MessageType.CreateAccount, Content = content });
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

            message.ExecutionTimestamp = DateTime.Now;
            message.Executed = true;

            //if (Factory != null)
            Send(new QuantApp.Kernel.RTDMessage() { Type = QuantApp.Kernel.RTDMessage.MessageType.UpdateQueue, Content = message });
        }


        // private static ConcurrentDictionary<string, object> call_responses = new ConcurrentDictionary<string, object>();
        // public static async Task<object> Call(string workspaceID, string name, object args)
        // {
        //     try
        //     {
        //         var data = new CallResponseMessage {
        //             Function = name,
        //             Data = args,

        //             ID = System.Guid.NewGuid().ToString(),
        //             Executed = false,
        //             CreationTimestamp = DateTime.Now,
        //         };

        //         object tmp = null;
        //         if(call_responses.ContainsKey(data.ID))
        //             call_responses.TryRemove(data.ID, out tmp);
                
                

        //         while(true)
        //         {
        //             System.Threading.Thread.Sleep(250);
        //             if(QuantApp.Server.Realtime.WebSocketListner.registered_workspaces_id.ContainsKey(workspaceID))
        //             {
        //                 var message = new QuantApp.Kernel.RTDMessage() { Type = QuantApp.Kernel.RTDMessage.MessageType.Call, Content = data };
        //                 var sid = QuantApp.Server.Realtime.WebSocketListner.registered_workspaces_id[workspaceID];
        //                 var socket = QuantApp.Server.Realtime.WebSocketListner.registered_sockets[sid];

        //                 string message_string = JsonConvert.SerializeObject(message);
        //                 Send(socket, message_string);
        //                 break;
        //             }
        //         }

                
        //         while(true)
        //         {
        //             System.Threading.Thread.Sleep(250);
        //             if(call_responses.ContainsKey(data.ID))
        //             {
        //                 var result = call_responses[data.ID];
        //                 // object tmp = null;
        //                 call_responses.TryRemove(data.ID, out tmp);
        //                 return result;
        //             }
        //         }
        //     }
        //     catch(Exception e)
        //     {
        //         Console.WriteLine("CALL ERROR: " + e);
        //     }
            
        //     return null;
        // }

        // public static object Responde(WebSocket socket, CallResponseMessage crm, object args)
        // {
        //     var data = new CallResponseMessage {
        //         Function = crm.Function,
        //         Data = args,

        //         ID = crm.ID,
        //         Executed = true,
        //         CreationTimestamp = crm.CreationTimestamp,
        //         ExecutionTimestamp = DateTime.Now
        //     };
            
        //     var message = new QuantApp.Kernel.RTDMessage() { Type = QuantApp.Kernel.RTDMessage.MessageType.Response, Content = data };

        //     string message_string = JsonConvert.SerializeObject(message);
            
        //     Send(socket, message_string);

        //     return null;
        // }

        private static ConcurrentDictionary<WebSocket, object> locks = new ConcurrentDictionary<WebSocket, object>();
        private readonly static object objLockSend = new object();
        private static void Send(WebSocket connection, string message)
        {
            if (!locks.ContainsKey(connection))
                locks.TryAdd(connection, new object());

            lock (locks[connection])
            {
                ArraySegment<byte> buffer = buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message));
                connection.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }

        private static void Share(WebSocket session, string topicID, string message)
        {
            if(manager == null)
                return;
            
            if (subscriptions.ContainsKey(topicID))
            {
                string skey = manager.GetId(session);
                if (subscriptions[topicID] != null)
                    foreach (WebSocket connection in subscriptions[topicID].Values)
                    {
                        string ckey = manager.GetId(connection);
                        try
                        {
                            var _user = users[topicID][ckey];
                            var group = Group.FindGroup(topicID);
                            var permission = AccessType.View;
                            if(group != null && !string.IsNullOrEmpty(_user.ID) && group.List(QuantApp.Kernel.User.CurrentUser, typeof(QuantApp.Kernel.User), false).Count > 0)
                                permission = group.Permission(_user);

                            if (ckey != skey && permission != AccessType.Denied)
                                Send(connection, message);
                        }
                        catch (Exception e)
                        {
                            WebSocket v = null;
                            string v2 = null;
                            subscriptions[topicID].TryRemove(ckey, out v);
                            if (traders.ContainsKey(topicID) && traders[topicID] == ckey)
                                traders.TryRemove(topicID, out v2);

                            if(WebSocketListner.registered_address.ContainsKey(ckey))
                            {
                                System.Net.IPAddress ip = null;
                                WebSocketListner.registered_address.TryRemove(ckey, out ip);
                            }

                            if(WebSocketListner.registered_sockets.ContainsKey(ckey))
                            {
                                WebSocket ip = null;
                                WebSocketListner.registered_sockets.TryRemove(ckey, out ip);
                            }

                            
                        }
                    }
            }
        }

        public bool Publish(object data)
        {
            return true;
        }
        public async Task Send(object message)
        {
            try
            {
                string message_string = JsonConvert.SerializeObject(message);
                string topicID = "";


                if (message.GetType() == typeof(QuantApp.Kernel.RTDMessage) && ((QuantApp.Kernel.RTDMessage)message).Type == QuantApp.Kernel.RTDMessage.MessageType.CRUD)
                {
                    QuantApp.Kernel.RTDMessage.CRUDMessage content = (QuantApp.Kernel.RTDMessage.CRUDMessage)((QuantApp.Kernel.RTDMessage)message).Content;
                    topicID = content.TopicID;
                }
                else if (message.GetType() == typeof(QuantApp.Kernel.RTDMessage) && ((QuantApp.Kernel.RTDMessage)message).Type == QuantApp.Kernel.RTDMessage.MessageType.UpdateQueue)
                {
                    topicID = ((QuantApp.Kernel.RTDMessage)message).Content.ToString();
                }
                
                else if (message.GetType() == typeof(AQI.AQILabs.Kernel.RTDMessage) && ((AQI.AQILabs.Kernel.RTDMessage)message).Type == AQI.AQILabs.Kernel.RTDMessage.MessageType.Function)
                {
                    AQI.AQILabs.Kernel.RTDMessage.FunctionMessage content = (AQI.AQILabs.Kernel.RTDMessage.FunctionMessage)((AQI.AQILabs.Kernel.RTDMessage)message).Content;
                    topicID = content.ID.ToString();
                }
                else if (message.GetType() == typeof(AQI.AQILabs.Kernel.RTDMessage) && ((AQI.AQILabs.Kernel.RTDMessage)message).Type == AQI.AQILabs.Kernel.RTDMessage.MessageType.Property)
                {
                    AQI.AQILabs.Kernel.RTDMessage.PropertyMessage content = (AQI.AQILabs.Kernel.RTDMessage.PropertyMessage)((AQI.AQILabs.Kernel.RTDMessage)message).Content;
                    topicID = content.ID.ToString();
                }
                else if (message.GetType() == typeof(AQI.AQILabs.Kernel.RTDMessage) && ((AQI.AQILabs.Kernel.RTDMessage)message).Type == AQI.AQILabs.Kernel.RTDMessage.MessageType.StrategyData)
                {
                    AQI.AQILabs.Kernel.RTDMessage.StrategyData content = (AQI.AQILabs.Kernel.RTDMessage.StrategyData)((AQI.AQILabs.Kernel.RTDMessage)message).Content;
                    topicID = content.InstrumentID.ToString();
                }
                else if (message.GetType() == typeof(AQI.AQILabs.Kernel.RTDMessage) && ((AQI.AQILabs.Kernel.RTDMessage)message).Type == AQI.AQILabs.Kernel.RTDMessage.MessageType.MarketData)
                {
                    AQI.AQILabs.Kernel.RTDMessage.MarketData content = (AQI.AQILabs.Kernel.RTDMessage.MarketData)((AQI.AQILabs.Kernel.RTDMessage)message).Content;
                    topicID = content.InstrumentID.ToString();
                }


                else if (message.GetType() == typeof(AQI.AQILabs.Kernel.RTDMessage) && (((AQI.AQILabs.Kernel.RTDMessage)message).Type == AQI.AQILabs.Kernel.RTDMessage.MessageType.AddNewOrder || ((AQI.AQILabs.Kernel.RTDMessage)message).Type == AQI.AQILabs.Kernel.RTDMessage.MessageType.UpdateOrder))
                {
                    AQI.AQILabs.Kernel.RTDMessage.OrderMessage content = (AQI.AQILabs.Kernel.RTDMessage.OrderMessage)((AQI.AQILabs.Kernel.RTDMessage)message).Content;
                    topicID = content.Order.PortfolioID.ToString();
                }

                else if (message.GetType() == typeof(AQI.AQILabs.Kernel.RTDMessage) && (((AQI.AQILabs.Kernel.RTDMessage)message).Type == AQI.AQILabs.Kernel.RTDMessage.MessageType.AddNewPosition || ((AQI.AQILabs.Kernel.RTDMessage)message).Type == AQI.AQILabs.Kernel.RTDMessage.MessageType.UpdatePosition))
                {
                    AQI.AQILabs.Kernel.RTDMessage.PositionMessage content = (AQI.AQILabs.Kernel.RTDMessage.PositionMessage)((AQI.AQILabs.Kernel.RTDMessage)message).Content;
                    topicID = content.Position.PortfolioID.ToString();
                }

                else if (message.GetType() == typeof(AQI.AQILabs.Kernel.RTDMessage) && ((AQI.AQILabs.Kernel.RTDMessage)message).Type == AQI.AQILabs.Kernel.RTDMessage.MessageType.CreateAccount)
                {
                    try
                    {
                        AQI.AQILabs.Kernel.RTDMessage.CreateAccount content = (((AQI.AQILabs.Kernel.RTDMessage)message).Content.GetType() == typeof(string)) ? JsonConvert.DeserializeObject<AQI.AQILabs.Kernel.RTDMessage.CreateAccount>(((AQI.AQILabs.Kernel.RTDMessage)message).Content.ToString()) : (AQI.AQILabs.Kernel.RTDMessage.CreateAccount)((AQI.AQILabs.Kernel.RTDMessage)message).Content;

                        int id = content.StrategyID;

                        AQI.AQILabs.Kernel.Strategy s = AQI.AQILabs.Kernel.Instrument.FindInstrument(id) as AQI.AQILabs.Kernel.Strategy;
                        if (s != null)
                        {
                            QuantApp.Kernel.User user = QuantApp.Kernel.User.FindUser(content.UserID);
                            QuantApp.Kernel.User attorney = content.AttorneyID == null || content.AttorneyID == "System" ? user : QuantApp.Kernel.User.FindUser(content.AttorneyID);
                            if (user != null && attorney != null)
                            {
                                List<PALMPending> pendings = PALM.GetPending(user);
                                foreach (PALMPending pending in pendings)
                                {
                                    if (pending.AccountID == content.AccountID)
                                    {

                                        pending.Strategy = s;
                                        pending.CreationDate = s.CreateTime;
                                        pending.Attorney = attorney;
                                        PALM.UpdatePending(pending);
                                        PALM.AddStrategy(pending.User, pending.Attorney, s);
                                    }
                                }
                            }
                        }

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        //Console.WriteLine("Create Account Exception: " + e + " " + session.SecWebSocketKey);
                    }
                }


                if (manager != null && subscriptions.ContainsKey(topicID))
                    foreach (WebSocket connection in subscriptions[topicID].Values)
                    {
                        string ckey = manager.GetId(connection);
                        InternalSend(connection, topicID, message_string, ckey);
                    }
                else if(_socket != null)
                    InternalSend(_socket, topicID, message_string, null);
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void InternalSend(WebSocket connection, string topicID, string message_string, string ckey)
        {
            bool success = false;
            for (int i = 0; i <= 5; i++)
            {
                if (success)
                    break;

                try
                {
                    if (connection.State == WebSocketState.Open)
                    {
                        var group = Group.FindGroup(topicID);
                        var permission = AccessType.View;
                        
                        if(users.ContainsKey(topicID) && users[topicID].ContainsKey(ckey))
                        {
                            var _user = users[topicID][ckey];
                            
                            if(group != null && !string.IsNullOrEmpty(_user.ID) && group.List(QuantApp.Kernel.User.CurrentUser, typeof(QuantApp.Kernel.User), false).Count > 0)
                                permission = group.Permission(_user);
                        }

                        if(permission != AccessType.Denied)
                            Send(connection, message_string);
                        success = true;
                    }

                    else if(ckey != null)
                    {
                        WebSocket v = null;
                        string v2 = null;
                        subscriptions[topicID].TryRemove(ckey, out v);
                        if (traders.ContainsKey(topicID) && traders[topicID] == ckey)
                            traders.TryRemove(topicID, out v2);

                        if(WebSocketListner.registered_address.ContainsKey(ckey))
                        {
                            System.Net.IPAddress ip = null;
                            WebSocketListner.registered_address.TryRemove(ckey, out ip);
                        }

                        if(WebSocketListner.registered_sockets.ContainsKey(ckey))
                        {
                            WebSocket ip = null;
                            WebSocketListner.registered_sockets.TryRemove(ckey, out ip);
                        }
                    }
                }
                catch (Exception e)
                {
                    if(ckey != null)
                    {
                        WebSocket v = null;
                        string v2 = null;
                        subscriptions[topicID].TryRemove(ckey, out v);
                        if (traders.ContainsKey(topicID) && traders[topicID] == ckey)
                            traders.TryRemove(topicID, out v2);

                        if(WebSocketListner.registered_address.ContainsKey(ckey))
                        {
                            System.Net.IPAddress ip = null;
                            WebSocketListner.registered_address.TryRemove(ckey, out ip);
                        }

                        if(WebSocketListner.registered_sockets.ContainsKey(ckey))
                        {
                            WebSocket ip = null;
                            WebSocketListner.registered_sockets.TryRemove(ckey, out ip);
                        }

                        Console.WriteLine(e);
                    }
                }
            }

            if (!success && ckey != null)
            {
                WebSocket v = null;
                string v2 = null;
                subscriptions[topicID].TryRemove(ckey, out v);
                if (traders.ContainsKey(topicID) && traders[topicID] == ckey)
                    traders.TryRemove(topicID, out v2);

                if(WebSocketListner.registered_address.ContainsKey(ckey))
                {
                    System.Net.IPAddress ip = null;
                    WebSocketListner.registered_address.TryRemove(ckey, out ip);
                }

                if(WebSocketListner.registered_sockets.ContainsKey(ckey))
                {
                    WebSocket ip = null;
                    WebSocketListner.registered_sockets.TryRemove(ckey, out ip);
                }
            }
        }

        public void ProcessMessages(string id)
        {

        }

        public void Send(IEnumerable<string> to, string from, string subject, string message)
        {
            try
            {
                var mailMsg = new System.Net.Mail.MailMessage();

                //// To
                foreach(var pairs in to)
                {
                    var pair = pairs.Split(';');
                    var email = pair[0];
                    var name = pair[1];
                    mailMsg.To.Add(new System.Net.Mail.MailAddress(email, name));
                }

                //// From
                var pairFrom = from.Split(';');
                var emailFrom = pairFrom[0];
                var nameFrom = pairFrom[1];
                mailMsg.From = new System.Net.Mail.MailAddress(emailFrom, nameFrom);

                mailMsg.Subject = subject;
                mailMsg.AlternateViews.Add(System.Net.Mail.AlternateView.CreateAlternateViewFromString(message, null, System.Net.Mime.MediaTypeNames.Text.Plain));
                mailMsg.AlternateViews.Add(System.Net.Mail.AlternateView.CreateAlternateViewFromString(message.Replace(System.Environment.NewLine, "<br>"), null, System.Net.Mime.MediaTypeNames.Text.Html));

                //// Init SmtpClient and send
                var smtpClient = new System.Net.Mail.SmtpClient("smtp.sendgrid.net", Convert.ToInt32(587));
                var credentials = new System.Net.NetworkCredential("aqi", "Capital!1234");
                smtpClient.Credentials = credentials;

                smtpClient.Send(mailMsg);
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }


    public class ProxyConnection : CoFlows.Core.Connection
    {
        //new public static ProxyConnection Client = new ProxyConnection();
        private static ConcurrentDictionary<string, ProxyConnection> registered_sockets = new ConcurrentDictionary<string, ProxyConnection>();

        public new static ProxyConnection Client(WebSocket browserSocket, string path)
        {
            string socket_id = browserSocket.GetHashCode() + path;
            if(!registered_sockets.ContainsKey(socket_id))
                registered_sockets.TryAdd(socket_id, new ProxyConnection(browserSocket, path));

            return registered_sockets[socket_id];

        }


        public readonly object mLock = new object();
        
        private static object consoleLock = new object();
        private const bool verbose = true;
        private static readonly TimeSpan delay = TimeSpan.FromMilliseconds(30000);
        private ClientWebSocket webSocket = null;
        private string Path = null;
        

        private WebSocket browserSocket = null;

        public ProxyConnection(WebSocket browserSocket, string path)
        {
            // Console.WriteLine("ProxyConnection New: " + path);
            this.browserSocket = browserSocket;
            this.Path = path;
            
        }

        public ClientWebSocket ClientWebSocket { get{ return webSocket; } }

        public async Task Close()
        {
            string socket_id = browserSocket.GetHashCode() + this.Path;
            ProxyConnection tmp = null;
            if(registered_sockets.ContainsKey(socket_id))
                registered_sockets.TryRemove(socket_id, out tmp);

            webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure,"", CancellationToken.None);
            webSocket.Dispose();
        }

        public System.Net.WebSockets.WebSocketState State()
        {
            return webSocket.State;
        }

        private string URI = null;
        private List<KeyValuePair<string, string>> Headers = null;

        public async Task Connect(string uri, List<KeyValuePair<string, string>> headers)
        {
            
            try
            {
                if(!(webSocket == null || webSocket.State == System.Net.WebSockets.WebSocketState.Closed))
                {
                    string uriPath = uri + this.Path;
                    // Console.WriteLine("ProxyConnection waiting: " + uriPath);
                    // Thread.Sleep(250);
                    while(webSocket.State != System.Net.WebSockets.WebSocketState.Open)
                    {
                        if(webSocket.State == System.Net.WebSockets.WebSocketState.Closed)
                            break;
                        Thread.Sleep(250);
                    }

                    // if(webSocket.State == System.Net.WebSockets.WebSocketState.Closed)
                    //     Console.WriteLine("ProxyConnection Waiting Closed(" + DateTime.Now.ToString("hh:mm:ss.fff") + "): " + this.Path);
                    // else
                    //     Console.WriteLine("ProxyConnection Waiting Opened(" + DateTime.Now.ToString("hh:mm:ss.fff") + "): " + this.Path);
                }
                else
                {
                    this.URI = uri;
                    this.Headers = headers;

                    string uriPath = uri + this.Path;

                    webSocket = new ClientWebSocket();
                    //webSocket.Options.UseDefaultCredentials = true;
                    webSocket.Options.Cookies = new CookieContainer();
                    

                    if(headers != null)
                    {
                        foreach(var head in headers)
                        {
                            try
                            {
                                var key = head.Key;
                                if(!key.Contains("Sec-WebSocket") && !key.Contains("Upgrade") && !key.Contains("Cookie"))// && !key.Contains("Connection"))// || key.Contains("Sec-WebSocket-Key"))
                                // if(!key.Contains("Sec-WebSocket") && !key.Contains("Cookie"))// && !key.Contains("Connection"))// || key.Contains("Sec-WebSocket-Key"))
                                {
                                    var val = head.Value.Replace(Program.hostName, "localhost:8888").Replace("https", "http");
                                    
                                    // Console.WriteLine("ProxyConnect Header(" + key + "): " + val.Replace("%7C", "|"));
                                    webSocket.Options.SetRequestHeader(key, val.Replace("%7C", "|"));
                                }
                                else if(key == "Cookie")
                                {
                                    var valArr = head.Value.Replace(Program.hostName, "localhost:8888").Replace("https", "http").Split(';');
                                    // var valArr = val;
                                    foreach(var val in valArr)
                                    {
                                        var cookieVal = val.Split('=');
                                        var cname = cookieVal[0].Trim();
                                        var cval = cookieVal[1].Replace("%7C", "|").Trim();
                                        // Console.WriteLine("ProxyConnect Cookie(" + cname + "): " + cval);
                                        webSocket.Options.Cookies.Add(new Cookie(cname, cval){ Domain = "localhost" });
                                    }
                                }
                            }
                            catch(Exception e)
                            {
                                // Console.WriteLine(e);
                            }
                        }
                    }

                    // Console.WriteLine("ProxyConnect Connecting: " + uriPath + " " + DateTime.Now.ToString("hh:mm:ss.fff"));
                    await webSocket.ConnectAsync(new Uri(uriPath), CancellationToken.None);


                    

                    // Thread.Sleep(250);
                    while(webSocket.State != System.Net.WebSockets.WebSocketState.Open)
                    {
                        if(webSocket.State == System.Net.WebSockets.WebSocketState.Closed)
                            break;
                        Thread.Sleep(250);
                    }

                    // Console.WriteLine("ProxyConnect Connection Done: " + uriPath + " " + DateTime.Now.ToString("hh:mm:ss.fff"));

                    // if(webSocket.State == System.Net.WebSockets.WebSocketState.Open)
                    //     Console.WriteLine("ProxyConnection Opened(" + DateTime.Now.ToString(" hh:mm:ss.fff") + "): " + this.Path);
                    // else
                    //     Console.WriteLine("ProxyConnection Closed WHILE(" + DateTime.Now.ToString(" hh:mm:ss.fff") + "): " + this.Path);

                    this.retryCounter = 0;

                    await Receive(webSocket, async (result, length, buffer) =>
                    {
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            string userMessage = Encoding.UTF8.GetString(buffer, 0, length);

                            Console.WriteLine("ProxyConnection Closing(" + DateTime.Now.ToString("hh:mm:ss.fff") + "):" + this.Path);// + mess_str);
                            
                            // var mess = new RTDMessage { 
                            //     Type = RTDMessage.MessageType.ProxyClose, 
                            //     Content = new HttpProxyRequest { 
                            //         Url = this.Path, 
                            //         Content = userMessage,
                            //         Headers = headers
                            //     } 
                            // };

                            // var mess_str = Newtonsoft.Json.JsonConvert.SerializeObject(mess);

                            

                            // ArraySegment<byte> _buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(mess_str));
                            // await browserSocket.SendAsync(_buffer, WebSocketMessageType.Text, true, CancellationToken.None);
                            

                            // Console.WriteLine("ProxyConnection Closing(" + DateTime.Now.ToString("hh:mm:ss.fff") + "):" + this.Path);// + mess_str);
                            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                            return;
                        }
                        else if (result.MessageType == WebSocketMessageType.Text)
                        {
                            string userMessage = Encoding.UTF8.GetString(buffer, 0, length);
                            
                            var mess = new QuantApp.Kernel.RTDMessage { 
                                Type = QuantApp.Kernel.RTDMessage.MessageType.ProxyContent, 
                                Content = new HttpProxyRequest { 
                                    Url = this.Path, 
                                    Content = userMessage,
                                    Headers = headers
                                } 
                            };

                            var mess_str = this.Path.StartsWith("/lab/__root") ? Encoding.UTF8.GetString(buffer, 0, length) : Newtonsoft.Json.JsonConvert.SerializeObject(mess);

                            // Console.WriteLine("ProxyConnection from Jupyter(" + DateTime.Now.ToString("hh:mm:ss.fff") + "): " + this.Path);// + mess_str);
                            // Console.WriteLine("ProxyConnection from Jupyter(" + DateTime.Now.ToString("hh:mm:ss.fff") + "): " + this.Path + " "  + mess_str);

                            ArraySegment<byte> _buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(mess_str));
                            browserSocket.SendAsync(_buffer, WebSocketMessageType.Text, true, CancellationToken.None);
                            return;
                        }

                        else
                            Console.WriteLine("REC BIN2: " + result.MessageType);

                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: {0}", ex);
            }
            // finally
            // {
            //     if (webSocket != null)
            //         webSocket.Dispose();
            //     Console.WriteLine();

            //     lock (consoleLock)
            //     {
            //         Console.ForegroundColor = ConsoleColor.Red;
            //         Console.WriteLine("WebSocket closed @ " + DateTime.Now);
            //         Console.ResetColor();
            //     }

                
                
            // }
        }

        private object sendLock = new object();
        private int retryCounter = 0;
        public void Send(string message)
        {
            lock (sendLock)
            {
                int counter = 0;
                while(webSocket.State != System.Net.WebSockets.WebSocketState.Open && counter < 4 * 60)
                {
                    counter++;
                    if(webSocket.State == System.Net.WebSockets.WebSocketState.Open)
                        break;
                    Thread.Sleep(250);
                }

                try
                {
                    if(webSocket.State == System.Net.WebSockets.WebSocketState.Open)
                    {
                        // Console.WriteLine("Send to Jupyter: " + DateTime.Now.ToString("hh:mm:ss.fff") + " " + message);
                        ArraySegment<byte> buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message));
                        webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                    else
                    {
                        Console.WriteLine("Send to Jupyter closed: " + message);
                        if(retryCounter < 10)
                        {
                            retryCounter++;
                            this.Connect(this.URI, this.Headers);
                            this.Send(message);
                        }
                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        private async Task Receive(WebSocket socket, Action<WebSocketReceiveResult, int, byte[]> handleMessage)
        {
            // var buffer = new byte[1024 * 10000];

            
            // while (socket.State == WebSocketState.Open)
            // {
            //     try
            //     {
            //         var result = await socket.ReceiveAsync(buffer: new ArraySegment<byte>(buffer), cancellationToken: CancellationToken.None);

            //         try
            //         {
            //             handleMessage(result, buffer);
            //         }
            //         catch(Exception t)
            //         {
            //             Console.WriteLine(t);
            //         }
            //     }
            //     catch (Exception e)
            //     {
            //         Console.WriteLine(e);
            //         break;
            //     }
            // }



            while (socket.State == WebSocketState.Open)
            {
                try
                {
                    int maxSize = 1024 * 2000000;
                    int bufferSize = 500;
                    int increaseSize = 1024 * 10;
                    var buffer = new byte[bufferSize];
                    var offset = 0;
                    var free = buffer.Length;
                    WebSocketReceiveResult result = null;
                    int counter = 0;
                    while (true)
                    {

                        result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer, offset, free), cancellationToken: CancellationToken.None);
                        offset += result.Count;
                        free -= result.Count;
                        if (result.EndOfMessage) 
                        {
                            // if(counter > 0)
                                // Console.WriteLine("done: " + offset + " " + free);
                            break;
                        }
                        if (free == 0)
                        {
                            if(counter > 5)
                                increaseSize = 1024 * 100;

                            else if(counter > 10)
                                increaseSize = 1024 * 1000;

                            else if(counter > 20)
                                increaseSize = 1024 * 10000;

                            else if(counter > 30)
                                increaseSize = 1024 * 100000;

                            else if(counter > 40)
                                increaseSize = 1024 * 1000000;
                            // No free space
                            // Resize the outgoing buffer
                            var newSize = buffer.Length + increaseSize;
                            // Console.WriteLine("more data: " + offset + " " + newSize);
                            
                            // Check if the new size exceeds a 
                            
                            // It should suit the data it receives
                            // This limit however has a max value of 2 billion bytes (2 GB)
                            if (newSize > maxSize)
                                throw new Exception ("Maximum size exceeded");
                            
                            var newBuffer = new byte[newSize];
                            Array.Copy(buffer, 0, newBuffer, 0, offset);
                            buffer = newBuffer;
                            free = buffer.Length - offset;
                            counter++;
                        }
                    }
                    // var result = await socket.ReceiveAsync(buffer: new ArraySegment<byte>(buffer), cancellationToken: CancellationToken.None);

                    try
                    {
                        handleMessage(result, offset, buffer);
                    }
                    catch(Exception t)
                    {
                        Console.WriteLine(t);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    break;
                }
            }
        }
    }
}