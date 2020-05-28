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

using System.Text;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Text;


using System.Data;
using Newtonsoft.Json;

using QuantApp.Kernel;
using QuantApp.Engine;

using QuantApp.Kernel.Adapters.SQL;

using System.Data;

namespace CoFlows.Server
{
    public class Connection : CoFlows.Core.Connection
    {
        new public static Connection Client = new Connection();

        public readonly object mLock = new object();
        public M GetM(string id, Type type) 
        {
            lock(mLock)
            {
                try
                {
                    var path = "m/rawdata?type=" + id;
                    var rawentries = GetObject<List<RawEntry>>(path);
                    var m = new M();
                    m.LoadRaw(rawentries);

                    m.ID = id;
                    m.type = type;

                    var mess = new {
                        Type = QuantApp.Kernel.RTDMessage.MessageType.Subscribe,
                        Content= id
                    };
                    Connection.Client.Send(JsonConvert.SerializeObject(mess));

                    return m;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                return null;
            }
        }

        private static object consoleLock = new object();

        private const bool verbose = true;
        private static readonly TimeSpan delay = TimeSpan.FromMilliseconds(30000);
        private ClientWebSocket webSocket = null;

        public async Task Connect()
        {
            try
            {
                webSocket = new ClientWebSocket();
                if(_token != null)
                    webSocket.Options.SetRequestHeader("Authorization", "Bearer " + _token);

                // webSocket.Options.Cookies = new CookieContainer();
                
                webSocket.Options.Cookies.Add(new Cookie("coflows", _session){ Domain = this.server });

                var uriObject = this.wsQuantAppURL;
                await webSocket.ConnectAsync(uriObject, CancellationToken.None);


                QuantApp.Kernel.RTDEngine.Factory = new CoFlows.Server.Realtime.WebSocketListner(webSocket);
                
                await Receive(webSocket, async (result, length, buffer) =>
                {
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                        return;
                    }
                    else if (result.MessageType == WebSocketMessageType.Text)
                    {
                        string userMessage = Encoding.UTF8.GetString(buffer, 0, length);

                        CoFlows.Server.Realtime.WebSocketListner.appServer_NewMessageReceived(webSocket, userMessage, uriObject.PathAndQuery, null);

                        return;
                    }
                    else
                        Console.WriteLine("REC BIN3: " + result.MessageType);


                });
            }
            catch (Exception ex)
            {
                // Console.WriteLine("Exception: {0}", ex);
            }
            // finally
            // {
            //     Console.WriteLine();

            //     lock (consoleLock)
            //     {
            //         Console.ForegroundColor = ConsoleColor.Red;
            //         Console.WriteLine("WebSocket closed @ " + DateTime.Now);
            //         Console.ResetColor();
            //     }

            //     Console.WriteLine("Attempting to reconnect...");
            //     System.Threading.Thread.Sleep(25 * 1000);
            //     Connect();//uri);
            //     System.Threading.Thread.Sleep(1000);
            //     foreach(var wsp in QuantApp.Engine.Utils.ActiveWorkflowList)
            //     {
            //         var mess = new {
            //             Type = QuantApp.Kernel.RTDMessage.MessageType.RegisterWorkflow,
            //             Content= wsp.ID
            //         };
            //         Send(JsonConvert.SerializeObject(mess));
            //     }
            // }
        }

        private static object sendLock = new object();
        public void Send(string message)
        {
            lock (sendLock)
            {
                ArraySegment<byte> buffer = buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message));
                webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
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
                            break;

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
