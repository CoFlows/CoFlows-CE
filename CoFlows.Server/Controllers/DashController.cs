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
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Text;
using System.Net.Http;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

using QuantApp.Engine;

namespace CoFlows.Server.Controllers
{
    [Authorize, Route("[controller]/[action]")]
    public class DashController : Controller
    {    
        private static System.Collections.Concurrent.ConcurrentDictionary<string, int> DashDB = new System.Collections.Concurrent.ConcurrentDictionary<string, int>();
        private static System.Collections.Concurrent.ConcurrentDictionary<string, string> DashDBScript = new System.Collections.Concurrent.ConcurrentDictionary<string, string>();
        private static int lastPort = 10000;
        public readonly static object objLockDashGet = new object();

        /// <summary>
        /// Dash GET proxy
        /// </summary>
        /// <returns>Success</returns>
        /// <response code="200"></response>
        [AllowAnonymous,HttpGet("/dash/{wid}/{qid}/{**url}")]
        public async Task DashGet(string wid, string qid, string url = "")
        {
            {
                // string key = Request.Cookies["coflows"]; 
                // if(key != null)
                // {
                //     QuantApp.Kernel.User quser = QuantApp.Kernel.User.FindUserBySecret(key);
                //     if(quser == null)
                //         return;

                //     Response.Cookies.Append("coflows", key, new CookieOptions() { Expires = DateTime.Now.AddHours(24) });  
                // }
                // else
                //     return;

            }

            var dashID = wid + qid;
            var port = 0;
            lock(objLockDashGet)
            {
                if(DashDB.ContainsKey(dashID))
                {
                    port = DashDB[dashID];

                    var m = QuantApp.Kernel.M.Base(wid);
                    var _res = m[x => true];
                    if(_res.Count > 0)
                    {
                        var workSpace = _res.FirstOrDefault() as Workflow;
                        var codes = new List<Tuple<string, string>>();
                        
                        var wb = QuantApp.Kernel.M.Base(wid + "--Queries");
                        var wb_res = wb[x => QuantApp.Kernel.M.V<string>(x, "ID") == qid];
                        if(wb_res.Count > 0)
                        {
                            var item = wb_res.FirstOrDefault() as CodeData;

                            if(DashDBScript[dashID] != item.Code)
                            {
                                DashDBScript[dashID] = item.Code;
                                codes.Add(new Tuple<string, string>(item.Name, item.Code));
                            
                                Console.WriteLine("Dash starting server: " + "/dash/" + wid + "/" + qid + "/" );
                                var result = QuantApp.Engine.Utils.ExecuteCodeFunction(true, codes, "run", new object[]{port, "/dash/" + wid + "/" + qid + "/"});
                            }
                        }
                        else
                        {
                            Console.WriteLine("DASH ERROR Workbook not found: " + qid);
                            return;
                        }
                    }
                    else
                    {
                        Console.WriteLine("DASH ERROR Workflow not found: " + wid);
                        return;
                    }
                }

                else
                {
                    lastPort++;
                    port = lastPort;
                    DashDB.TryAdd(dashID, port);

                    var m = QuantApp.Kernel.M.Base(wid);
                    var _res = m[x => true];
                    if(_res.Count > 0)
                    {
                        var workSpace = _res.FirstOrDefault() as Workflow;
                        var codes = new List<Tuple<string, string>>();
                        
                        var wb = QuantApp.Kernel.M.Base(wid + "--Queries");
                        var wb_res = wb[x => QuantApp.Kernel.M.V<string>(x, "ID") == qid];
                        if(wb_res.Count > 0)
                        {
                            var item = wb_res.FirstOrDefault() as CodeData;
                            codes.Add(new Tuple<string, string>(item.Name, item.Code));
                            DashDBScript[dashID] = item.Code;
                        
                            Console.WriteLine("Dash starting server: " + "/dash/" + wid + "/" + qid + "/" );
                            var result = QuantApp.Engine.Utils.ExecuteCodeFunction(true, codes, "run", new object[]{port, "/dash/" + wid + "/" + qid + "/"});
                        }
                        else
                        {
                            Console.WriteLine("DASH ERROR Workbook not found: " + qid);
                            return;
                        }
                    }
                    else
                    {
                        Console.WriteLine("DASH ERROR Workflow not found: " + wid);
                        return;
                    }
                }
            }

            if(url == null) url = "";
            var queryString = Request.QueryString;
            if(queryString != null && queryString.HasValue)
                url = "/dash/" + wid + "/" + qid + "/" + url + queryString.Value;
            else
                url = "/dash/" + wid + "/" + qid + "/" + url;

            var headers = new List<KeyValuePair<string, string>>();

            foreach(var head in Request.Headers)
            {
                foreach(var val in head.Value)
                {
                    try
                    {
                        headers.Add(new KeyValuePair<string, string>(head.Key, val.Replace("\"", "")));
                    }
                    catch{}
                }
            }

            var _headers = headers;

            
            var handler = new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            HttpClient _httpClient = new HttpClient(handler);
            _httpClient.DefaultRequestHeaders.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("gzip"));
            if(_headers != null)
            {
                foreach(var head in _headers)
                {
                    try
                    {
                        _httpClient.DefaultRequestHeaders.Add(head.Key, head.Value);
                    }
                    catch(Exception e){}
                }
            }

            string _message = "";
            System.Net.Http.HttpResponseMessage response = null;
            for(int i = 0; i < 4; i++)
            {
                try
                {
                    response = await _httpClient.GetAsync("http://localhost:" + port + url);
                    var content = await response.Content.ReadAsByteArrayAsync();
                    _message = System.Convert.ToBase64String(content);
                    break;
                }
                catch(Exception e){}
                System.Threading.Thread.Sleep(2 * 1000);
            }

            var rheaders = new List<KeyValuePair<string, string>>();

            try
            {
                foreach(var head in response.Headers)
                {
                    foreach(var val in head.Value)
                    {
                        try
                        {
                            rheaders.Add(new KeyValuePair<string, string>(head.Key, val));
                        }
                        catch(Exception e){}
                    }
                }
            }
            catch(Exception e){}

            foreach(var head in rheaders)
            {
                try
                {
                    string key = head.Key.ToString();
                    string val = head.Value.ToString();

                    if(key == "Set-Cookie")
                    {
                        string[] vals = val.Split(';');
                        string[] keyValPair = vals[0].Split('=');
                        string cname = keyValPair[0];

                        string cval = keyValPair[1].Replace("\"", "");

                        var option = new CookieOptions(); 
                        if(vals.Length > 1)
                        {
                            string[] expiryPair = vals[1].Split('=');
                            string date_str = expiryPair[1];
                            option.Expires = DateTime.Parse(date_str);
                        }

                        if(vals.Length > 2)
                        {
                            string[] pathPair = vals[2].Split('=');
                            option.Path = pathPair[1];
                        }

                        Response.Cookies.Append(cname, cval, option);
                    }
                    else
                        Response.Headers.Add(key, val);

                    
                }
                catch(Exception e)
                {
                    // Console.WriteLine(e);
                }
            }

            Response.StatusCode = (int)response.StatusCode;
            Response.ContentType = string.IsNullOrEmpty(_message) || response.Content.Headers.ContentType == null ? "" : response.Content.Headers.ContentType.ToString();
            if(Response.StatusCode != 204)
            {
                var content = System.Convert.FromBase64String(_message.ToString());
                Response.ContentLength = content.Length;
                await Response.Body.WriteAsync(content, 0, content.Length);
            }
        }

        public readonly static object objLockDashPost = new object();
        /// <summary>
        /// Dash POST proxy
        /// </summary>
        /// <returns>Success</returns>
        /// <response code="200"></response>
        [AllowAnonymous,HttpPost("/dash/{wid}/{qid}/{**url}")]
        public async Task DashPost(string wid, string qid, string url = "")
        {
            // {
            //     string key = Request.Cookies["coflows"]; 
            //     if(key != null)
            //     {
            //         QuantApp.Kernel.User quser = QuantApp.Kernel.User.FindUserBySecret(key);
            //         if(quser == null)
            //             return;

            //         Response.Cookies.Append("coflows", key, new CookieOptions() { Expires = DateTime.Now.AddHours(24) });  
            //     }
            //     else
            //         return;

            // }

            

            var dashID = wid + qid;
            var port = 0;
            lock(objLockDashPost)
            {
                if(DashDB.ContainsKey(dashID))
                {
                    port = DashDB[dashID];

                    var m = QuantApp.Kernel.M.Base(wid);
                    var _res = m[x => true];
                    if(_res.Count > 0)
                    {
                        var workSpace = _res.FirstOrDefault() as Workflow;
                        var codes = new List<Tuple<string, string>>();
                        
                        var wb = QuantApp.Kernel.M.Base(wid + "--Queries");
                        var wb_res = wb[x => QuantApp.Kernel.M.V<string>(x, "ID") == qid];
                        if(wb_res.Count > 0)
                        {
                            var item = wb_res.FirstOrDefault() as CodeData;

                            if(DashDBScript[dashID] != item.Code)
                            {
                                DashDBScript[dashID] = item.Code;
                                codes.Add(new Tuple<string, string>(item.Name, item.Code));
                            
                                Console.WriteLine("Dash starting server: " + "/dash/" + wid + "/" + qid + "/" );
                                var result = QuantApp.Engine.Utils.ExecuteCodeFunction(true, codes, "run", new object[]{port, "/dash/" + wid + "/" + qid + "/"});
                            }
                        }
                        else
                        {
                            Console.WriteLine("DASH ERROR Workbook not found: " + qid);
                            return;
                        }
                    }
                    else
                    {
                        Console.WriteLine("DASH ERROR Workflow not found: " + wid);
                        return;
                    }
                }

                else
                {
                    lastPort++;
                    port = lastPort;
                    DashDB.TryAdd(dashID, port);

                    var m = QuantApp.Kernel.M.Base(wid);
                    var _res = m[x => true];
                    if(_res.Count > 0)
                    {
                        var workSpace = _res.FirstOrDefault() as Workflow;
                        var codes = new List<Tuple<string, string>>();
                        
                        var wb = QuantApp.Kernel.M.Base(wid + "--Queries");
                        var wb_res = wb[x => QuantApp.Kernel.M.V<string>(x, "ID") == qid];
                        if(wb_res.Count > 0)
                        {
                            var item = wb_res.FirstOrDefault() as CodeData;
                            codes.Add(new Tuple<string, string>(item.Name, item.Code));
                            DashDBScript[dashID] = item.Code;
                        
                            Console.WriteLine("Dash starting server: " + "/dash/" + wid + "/" + qid + "/" );
                            var result = QuantApp.Engine.Utils.ExecuteCodeFunction(true, codes, "run", new object[]{port, "/dash/" + wid + "/" + qid + "/"});
                        }
                        else
                        {
                            Console.WriteLine("DASH ERROR Workbook not found: " + qid);
                            return;
                        }
                    }
                    else
                    {
                        Console.WriteLine("DASH ERROR Workflow not found: " + wid);
                        return;
                    }
                }
            }
                    
            if(url == null) url = "";
            var queryString = Request.QueryString;
            if(queryString != null && queryString.HasValue)
                url = "/dash/" + wid + "/" + qid + "/" + url + queryString.Value;
            else
                url = "/dash/" + wid + "/" + qid + "/" + url;

            var headers = new List<KeyValuePair<string, string>>();

            foreach(var head in Request.Headers)
            {
                foreach(var val in head.Value)
                {
                    try
                    {
                        headers.Add(new KeyValuePair<string, string>(head.Key, val.Replace("\"", "")));
                    }
                    catch{}
                }
            }

            var mem = new MemoryStream();
            try
            {
                await Request.Body.CopyToAsync(mem);
            }
            catch(Exception e){ Console.WriteLine(e);}
            mem.Position = 0;
        
            var _headers = headers;
            var jsonContent = new StringContent(Encoding.UTF8.GetString(mem.ToArray()), Encoding.UTF8, "application/json");

            QuantApp.Kernel.User.ContextUser = new QuantApp.Kernel.UserData();
            var handler = new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            HttpClient _httpClient = new HttpClient(handler);
            _httpClient.DefaultRequestHeaders.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("gzip"));
            
            if(_headers != null)
            {
                foreach(var head in _headers)
                {
                    try
                    {
                        _httpClient.DefaultRequestHeaders.Add(head.Key, head.Value);
                    }
                    catch(Exception e)
                    {
                    }
                }
            }
            QuantApp.Kernel.User.ContextUser = new QuantApp.Kernel.UserData();
            var response = await _httpClient.PostAsync("http://localhost:" + port + url, jsonContent);

            var content = await response.Content.ReadAsByteArrayAsync();
            
            var rheaders = new List<KeyValuePair<string, string>>();

            try
            {
                foreach(var head in response.Headers)
                {
                    foreach(var val in head.Value)
                    {
                        try
                        {
                            rheaders.Add(new KeyValuePair<string, string>(head.Key, val));
                        }
                        catch(Exception e)
                        {
                            Console.WriteLine(e);
                        }
                    }
                }
            }
            catch(Exception e){}


            foreach(var head in rheaders)
            {
                try
                {
                    string key = head.Key.ToString();
                    string val = head.Value.ToString();

                    if(key == "Set-Cookie")
                    {
                        string[] vals = val.Split(';');
                        string[] keyValPair = vals[0].Split('=');
                        string cname = keyValPair[0];

                        string cval = keyValPair[1].Replace("\"", "");
                        var option = new CookieOptions(); 
                        if(vals.Length > 1)
                        {
                            string[] expiryPair = vals[1].Split('=');
                            string date_str = expiryPair[1];
                            option.Expires = DateTime.Parse(date_str);
                        }

                        if(vals.Length > 2)
                        {
                            string[] pathPair = vals[2].Split('=');
                            option.Path = pathPair[1];
                        }

                        Response.Cookies.Append(cname, cval, option);
                    }
                    else
                        Response.Headers.Add(key, val);
                    
                }
                catch(Exception e)
                {
                }
            }

            Response.StatusCode = (int)response.StatusCode;
            if(response.Content.Headers.ContentType != null)
                Response.ContentType = response.Content.Headers.ContentType.ToString();
            Response.ContentLength = content.Length;

            if(content.Length > 0 && Response.StatusCode != 204)
                await Response.Body.WriteAsync(content, 0, content.Length);
        }
    }
}