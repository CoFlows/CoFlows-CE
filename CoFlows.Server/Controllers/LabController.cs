/*
 * The MIT License (MIT)
 * Copyright (c) Arturo Rodriguez All rights reserved.
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using System.Net.Http;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

using Python.Runtime;

namespace CoFlows.Server.Controllers
{
    [Authorize, Route("[controller]/[action]")]
    public class LabController : Controller
    {
        public static System.Collections.Concurrent.ConcurrentDictionary<string, int> LabDB = new System.Collections.Concurrent.ConcurrentDictionary<string, int>();
        private static int lastLabPort = 20000;
        public readonly static object objLockLabGet = new object();

        /// <summary>
        /// Jupyter Lab GET proxy
        /// </summary>
        /// <returns>Success</returns>
        /// <response code="200"></response>
        [AllowAnonymous,HttpGet("/lab/{id}/{**url}")]
        public async Task LabGet(string id, string url = "")
        {
            string cokey = Request.Cookies["coflows"]; 
            if(cokey != null)
            {
                if(AccountController.sessionKeys.ContainsKey(cokey))
                {

                    QuantApp.Kernel.User quser = QuantApp.Kernel.User.FindUserBySecret(AccountController.sessionKeys[cokey]);
                    if(quser == null)
                    {
                        var content = Encoding.UTF8.GetBytes("Not Authorized...");
                        await Response.Body.WriteAsync(content, 0, content.Length);
                        return;
                    }                    
                }
                else
                {
                    var content = Encoding.UTF8.GetBytes("Not Authorized...");
                    await Response.Body.WriteAsync(content, 0, content.Length);
                    return;
                }
                
            }
            else
            {
                var content = Encoding.UTF8.GetBytes("Not Authorized...");
                await Response.Body.WriteAsync(content, 0, content.Length);
                return;
            }

            if(!Program.useJupyter)
            {
                Console.WriteLine("Jupyter is not on: " + id + " " + url);
                var content = Encoding.UTF8.GetBytes("This CoFlows container doesn't have Jupter started...");
                await Response.Body.WriteAsync(content, 0, content.Length);
            }
            
            if(url == null) url = "";
            var queryString = Request.QueryString;
            url = "/lab/" + id + "/" + url + queryString.Value;
            
            int labPort = 0;
            if(Program.useJupyter && !LabDB.ContainsKey(cokey + id))
            {
                QuantApp.Kernel.User quser = QuantApp.Kernel.User.FindUserBySecret(AccountController.sessionKeys[cokey]);
                bool isRoot = quser.ID == "QuantAppSecure_root";

                lastLabPort++;
                LabDB[cokey + id] = lastLabPort;

                var userName = System.Text.RegularExpressions.Regex.Replace(quser.Email, "[^a-zA-Z0-9 -]", "_");
                var createUser = "import subprocess;newUser = '" + userName + "';userExists = newUser in list(map(lambda x: x.split(':')[0], subprocess.check_output(['getent', 'passwd']).decode('utf-8').split('\\n'))); print('User exists: ' + newUser) if userExists else subprocess.check_call(['adduser', '--gecos', '\"First Last,RoomNumber,WorkPhone,HomePhone\"', '--disabled-password', '--home', f'/app/mnt/home/{newUser}/', '--shell', '/bin/bash', f'{newUser}']); print('no need to edit .bashrc') if (('PS1=\"\\\\u:\\\\w> \"' in open(f'/app/mnt/home/{newUser}/.bashrc').read())) else open(f'/app/mnt/home/{newUser}/.bashrc', 'a').write('PS1=\"\\\\u:\\\\w> \"')";
                // var createUser = "import subprocess;newUser = '" + userName + "';userExists = newUser in list(map(lambda x: x.split(':')[0], subprocess.check_output(['getent', 'passwd']).decode('utf-8').split('\\n'))); print('User exists: ' + newUser) if userExists else subprocess.check_call(['adduser', '--gecos', '\"First Last,RoomNumber,WorkPhone,HomePhone\"', '--disabled-password', '--home', f'/app/mnt/home/{newUser}/', '--shell', '/bin/bash', f'{newUser}']); print('no need to edit .bashrc') if (userExists and ('PS1=\"\\\\u:\\\\w> \"' in open(f'/app/mnt/home/{newUser}/.bashrc').read())) else open(f'/app/mnt/home/{newUser}/.bashrc', 'a').write('PS1=\"\\\\u:\\\\w> \"')";
                // var createUser = "import subprocess;newUser = '" + userName + "';userExists = newUser in list(map(lambda x: x.split(':')[0], subprocess.check_output(['getent', 'passwd']).decode('utf-8').split('\\n'))); print('User exists: ' + newUser) if userExists else subprocess.check_call(['adduser', '--gecos', '\"First Last,RoomNumber,WorkPhone,HomePhone\"', '--disabled-password', '--home', f'/app/mnt/home/{newUser}/', '--shell', '/bin/bash', f'{newUser}']);";
                var code = isRoot ? "import subprocess;subprocess.check_call(['jupyter', 'lab', '--port=" + lastLabPort + "', '--NotebookApp.notebook_dir=/app/mnt/', '--ip=*', '--NotebookApp.allow_remote_access=True', '--allow-root', '--no-browser', '--NotebookApp.token=\'\'', '--NotebookApp.password=\'\'', '--NotebookApp.disable_check_xsrf=True', '--NotebookApp.base_url=/lab/" + id + "'], cwd='/app/')" : "import subprocess;import os;subprocess.check_call(['sudo', '-u', '" + userName + "', 'jupyter', 'lab', '--port=" + lastLabPort + "', '--NotebookApp.notebook_dir=/app/mnt/', '--ip=*', '--NotebookApp.allow_remote_access=True', '--no-browser', '--NotebookApp.token=\'\'', '--NotebookApp.password=\'\'', '--NotebookApp.disable_check_xsrf=True', '--NotebookApp.base_url=/lab/" + id + "'], cwd='/app/mnt/home/" + userName + "')";
                
                var th = new System.Threading.Thread(() => {
                    using (Py.GIL())
                    {
                        Console.WriteLine("Starting Jupyter...");
                        
                        if(!isRoot)
                        {
                            Console.WriteLine(createUser);
                            PythonEngine.Exec(createUser);
                        }

                        Console.WriteLine(code);
                        PythonEngine.Exec(code);
                    }
                });
                th.Start();
                System.Threading.Thread.Sleep(1000 * 5);
            }
            
            labPort = LabDB[cokey + id];

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

            QuantApp.Kernel.User.ContextUser = new QuantApp.Kernel.UserData();
            HttpClient _httpClient = new HttpClient();
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

            var response = await _httpClient.GetAsync("http://localhost:" + labPort + url);
            string _message = "";
            try
            {
                var content = await response.Content.ReadAsByteArrayAsync();
                _message = System.Convert.ToBase64String(content);
            }
            catch(Exception e){}

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
            Response.ContentType = string.IsNullOrEmpty(_message) || response.Content.Headers.ContentType == null ? "" : response.Content.Headers.ContentType.ToString();
            Response.ContentLength = string.IsNullOrEmpty(_message) ? 0L : (long)response.Content.Headers.ContentLength;

            if(Response.ContentLength > 0 && Response.StatusCode != 204)
            {
                var content = System.Convert.FromBase64String(_message.ToString());
                await Response.Body.WriteAsync(content, 0, content.Length);
            }
        }

        /// <summary>
        /// Jupyter Lab POST proxy
        /// </summary>
        /// <returns>Success</returns>
        /// <response code="200"></response>
        [AllowAnonymous,HttpPost("/lab/{id}/{**url}")]
        public async Task LabPost(string id, string url = "")
        {
            string cokey = Request.Cookies["coflows"]; 
            if(cokey != null)
            {
                if(AccountController.sessionKeys.ContainsKey(cokey))
                {

                    QuantApp.Kernel.User quser = QuantApp.Kernel.User.FindUserBySecret(AccountController.sessionKeys[cokey]);
                    if(quser == null)
                    {
                        var content = Encoding.UTF8.GetBytes("Not Authorized...");
                        await Response.Body.WriteAsync(content, 0, content.Length);
                        return;
                    }                    
                }
                else
                {
                    var content = Encoding.UTF8.GetBytes("Not Authorized...");
                    await Response.Body.WriteAsync(content, 0, content.Length);
                    return;
                }
                
            }
            else
            {
                var content = Encoding.UTF8.GetBytes("Not Authorized...");
                await Response.Body.WriteAsync(content, 0, content.Length);
                return;
            }

            if(!Program.useJupyter)
            {
                Console.WriteLine("Jupyter is not on: " + id + " " + url);
                var content = Encoding.UTF8.GetBytes("This CoFlows container doesn't have Jupter started...");
                await Response.Body.WriteAsync(content, 0, content.Length);
            }
            else
            {
                if(url == null) url = "";
                var queryString = Request.QueryString;
                url = "/lab/" + id + "/" + url + queryString.Value;

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
                // Request.Body.CopyTo(mem);
                try
                {
                    await Request.Body.CopyToAsync(mem);
                }
                catch(Exception e){ Console.WriteLine(e);}
                mem.Position = 0;
            
                var _headers = headers;
                var streamContent = new StreamContent(mem);

                QuantApp.Kernel.User.ContextUser = new QuantApp.Kernel.UserData();
                HttpClient _httpClient = new HttpClient();
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

                int labPort = 0;
                if(Program.useJupyter && !LabDB.ContainsKey(cokey + id))
                {
                    QuantApp.Kernel.User quser = QuantApp.Kernel.User.FindUserBySecret(AccountController.sessionKeys[cokey]);
                    bool isRoot = quser.ID == "QuantAppSecure_root";

                    lastLabPort++;
                    LabDB[cokey + id] = lastLabPort;

                    var userName = System.Text.RegularExpressions.Regex.Replace(quser.Email, "[^a-zA-Z0-9 -]", "_");
                    var createUser = "import subprocess;newUser = '" + userName + "';userExists = newUser in list(map(lambda x: x.split(':')[0], subprocess.check_output(['getent', 'passwd']).decode('utf-8').split('\\n'))); print('User exists: ' + newUser) if userExists else subprocess.check_call(['adduser', '--gecos', '\"First Last,RoomNumber,WorkPhone,HomePhone\"', '--disabled-password', '--home', f'/app/mnt/home/{newUser}/', '--shell', '/bin/bash', f'{newUser}']); print('no need to edit .bashrc') if (('PS1=\"\\\\u:\\\\w> \"' in open(f'/app/mnt/home/{newUser}/.bashrc').read())) else open(f'/app/mnt/home/{newUser}/.bashrc', 'a').write('PS1=\"\\\\u:\\\\w> \"')";
                    // var createUser = "import subprocess;newUser = '" + userName + "';userExists = newUser in list(map(lambda x: x.split(':')[0], subprocess.check_output(['getent', 'passwd']).decode('utf-8').split('\\n'))); print('User exists: ' + newUser) if userExists else subprocess.check_call(['adduser', '--gecos', '\"First Last,RoomNumber,WorkPhone,HomePhone\"', '--disabled-password', '--home', f'/app/mnt/home/{newUser}/', '--shell', '/bin/bash', f'{newUser}']); print('no need to edit .bashrc') if userExists else open(f'/app/mnt/home/{newUser}/.bashrc', 'a').write('PS1=\"\\\\u:\\\\w> \"')";
                    var code = isRoot ? "import subprocess;subprocess.check_call(['jupyter', 'lab', '--port=" + lastLabPort + "', '--NotebookApp.notebook_dir=/app/mnt/', '--ip=*', '--NotebookApp.allow_remote_access=True', '--allow-root', '--no-browser', '--NotebookApp.token=\'\'', '--NotebookApp.password=\'\'', '--NotebookApp.disable_check_xsrf=True', '--NotebookApp.base_url=/lab/" + id + "'], cwd='/app/')" : "import subprocess;subprocess.check_call(['sudo', '-u', '" + userName + "', 'jupyter', 'lab', '--port=" + lastLabPort + "', '--NotebookApp.notebook_dir=/app/mnt/', '--ip=*', '--NotebookApp.allow_remote_access=True', '--no-browser', '--NotebookApp.token=\'\'', '--NotebookApp.password=\'\'', '--NotebookApp.disable_check_xsrf=True', '--NotebookApp.base_url=/lab/" + id + "'], cwd='/app/mnt/home/" + userName + "')";
                    var th = new System.Threading.Thread(() => {
                        using (Py.GIL())
                        {
                            Console.WriteLine("Starting Jupyter...");
                            
                            if(!isRoot)
                            {
                                Console.WriteLine(createUser);
                                PythonEngine.Exec(createUser);
                            }

                            Console.WriteLine(code);
                            PythonEngine.Exec(code);
                        }
                    });
                    th.Start();
                    System.Threading.Thread.Sleep(1000 * 5);
                }
                labPort = LabDB[cokey + id];
                
                QuantApp.Kernel.User.ContextUser = new QuantApp.Kernel.UserData();
                var response = await _httpClient.PostAsync("http://localhost:" + labPort + url, streamContent);

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

        /// <summary>
        /// Jupyter Lab PUT proxy
        /// </summary>
        /// <returns>Success</returns>
        /// <response code="200"></response>
        [AllowAnonymous,HttpPut("/lab/{id}/{**url}")]
        public async Task LabPut(string id, string url = "")
        {
            string cokey = Request.Cookies["coflows"]; 
            if(cokey != null)
            {
                if(AccountController.sessionKeys.ContainsKey(cokey))
                {

                    QuantApp.Kernel.User quser = QuantApp.Kernel.User.FindUserBySecret(AccountController.sessionKeys[cokey]);
                    if(quser == null)
                    {
                        var content = Encoding.UTF8.GetBytes("Not Authorized...");
                        await Response.Body.WriteAsync(content, 0, content.Length);
                        return;
                    }                    
                }
                else
                {
                    var content = Encoding.UTF8.GetBytes("Not Authorized...");
                    await Response.Body.WriteAsync(content, 0, content.Length);
                    return;
                }
                
            }
            else
            {
                var content = Encoding.UTF8.GetBytes("Not Authorized...");
                await Response.Body.WriteAsync(content, 0, content.Length);
                return;
            }

            if(!Program.useJupyter)
            {
                Console.WriteLine("Jupyter is not on: " + id + " " + url);
                var content = Encoding.UTF8.GetBytes("This CoFlows container doesn't have Jupter started...");
                await Response.Body.WriteAsync(content, 0, content.Length);
            }
            else
            {
                if(url == null) url = "";
                var queryString = Request.QueryString;
                url = "/lab/" + id + "/" + url + queryString.Value;

                var headers = new List<KeyValuePair<string, string>>();

                foreach(var head in Request.Headers)
                {
                    foreach(var val in head.Value)
                    {
                        try
                        {
                            headers.Add(new KeyValuePair<string, string>(head.Key, val));
                        }
                        catch{}
                    }
                }

                var mem = new MemoryStream();
                // Request.Body.CopyTo(mem);
                try
                {
                    await Request.Body.CopyToAsync(mem);
                }
                catch(Exception e){ Console.WriteLine(e);}
                mem.Position = 0;
                
                var _headers = headers;
                
                var streamContent = new StreamContent(mem);

                QuantApp.Kernel.User.ContextUser = new QuantApp.Kernel.UserData();
                HttpClient _httpClient = new HttpClient();
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

                int labPort = 0;
                if(Program.useJupyter && !LabDB.ContainsKey(cokey + id))
                {
                    QuantApp.Kernel.User quser = QuantApp.Kernel.User.FindUserBySecret(AccountController.sessionKeys[cokey]);
                    bool isRoot = quser.ID == "QuantAppSecure_root";

                    lastLabPort++;
                    LabDB[cokey + id] = lastLabPort;

                    var userName = System.Text.RegularExpressions.Regex.Replace(quser.Email, "[^a-zA-Z0-9 -]", "_");
                    var createUser = "import subprocess;newUser = '" + userName + "';userExists = newUser in list(map(lambda x: x.split(':')[0], subprocess.check_output(['getent', 'passwd']).decode('utf-8').split('\\n'))); print('User exists: ' + newUser) if userExists else subprocess.check_call(['adduser', '--gecos', '\"First Last,RoomNumber,WorkPhone,HomePhone\"', '--disabled-password', '--home', f'/app/mnt/home/{newUser}/', '--shell', '/bin/bash', f'{newUser}']); print('no need to edit .bashrc') if (('PS1=\"\\\\u:\\\\w> \"' in open(f'/app/mnt/home/{newUser}/.bashrc').read())) else open(f'/app/mnt/home/{newUser}/.bashrc', 'a').write('PS1=\"\\\\u:\\\\w> \"')";
                    // var createUser = "import subprocess;newUser = '" + userName + "';userExists = newUser in list(map(lambda x: x.split(':')[0], subprocess.check_output(['getent', 'passwd']).decode('utf-8').split('\\n'))); print('User exists: ' + newUser) if userExists else subprocess.check_call(['adduser', '--gecos', '\"First Last,RoomNumber,WorkPhone,HomePhone\"', '--disabled-password', '--home', f'/app/mnt/home/{newUser}/', '--shell', '/bin/bash', f'{newUser}']); print('no need to edit .bashrc') if userExists else open(f'/app/mnt/home/{newUser}/.bashrc', 'a').write('PS1=\"\\\\u:\\\\w> \"')";
                    var code = isRoot ? "import subprocess;subprocess.check_call(['jupyter', 'lab', '--port=" + lastLabPort + "', '--NotebookApp.notebook_dir=/app/mnt/', '--ip=*', '--NotebookApp.allow_remote_access=True', '--allow-root', '--no-browser', '--NotebookApp.token=\'\'', '--NotebookApp.password=\'\'', '--NotebookApp.disable_check_xsrf=True', '--NotebookApp.base_url=/lab/" + id + "'], cwd='/app/')" : "import subprocess;subprocess.check_call(['sudo', '-u', '" + userName + "', 'jupyter', 'lab', '--port=" + lastLabPort + "', '--NotebookApp.notebook_dir=/app/mnt/', '--ip=*', '--NotebookApp.allow_remote_access=True', '--no-browser', '--NotebookApp.token=\'\'', '--NotebookApp.password=\'\'', '--NotebookApp.disable_check_xsrf=True', '--NotebookApp.base_url=/lab/" + id + "'], cwd='/app/mnt/home/" + userName + "')";
                    var th = new System.Threading.Thread(() => {
                        using (Py.GIL())
                        {
                            Console.WriteLine("Starting Jupyter...");
                            
                            if(!isRoot)
                            {
                                Console.WriteLine(createUser);
                                PythonEngine.Exec(createUser);
                            }

                            Console.WriteLine(code);
                            PythonEngine.Exec(code);
                        }
                    });
                    th.Start();
                    System.Threading.Thread.Sleep(1000 * 5);
                }
                
                labPort = LabDB[cokey + id];

                var response = await _httpClient.PutAsync("http://localhost:" + labPort + url, streamContent);
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

        /// <summary>
        /// Jupyter Lab PATCH proxy
        /// </summary>
        /// <returns>Success</returns>
        /// <response code="200"></response>
        [AllowAnonymous,HttpPatch("/lab/{id}/{**url}")]
        public async Task LabPatch(string id, string url = "")
        {
            string cokey = Request.Cookies["coflows"]; 
            if(cokey != null)
            {
                if(AccountController.sessionKeys.ContainsKey(cokey))
                {

                    QuantApp.Kernel.User quser = QuantApp.Kernel.User.FindUserBySecret(AccountController.sessionKeys[cokey]);
                    if(quser == null)
                    {
                        var content = Encoding.UTF8.GetBytes("Not Authorized...");
                        await Response.Body.WriteAsync(content, 0, content.Length);
                        return;
                    }                    
                }
                else
                {
                    var content = Encoding.UTF8.GetBytes("Not Authorized...");
                    await Response.Body.WriteAsync(content, 0, content.Length);
                    return;
                }
                
            }
            else
            {
                var content = Encoding.UTF8.GetBytes("Not Authorized...");
                await Response.Body.WriteAsync(content, 0, content.Length);
                return;
            }

            if(!Program.useJupyter)
            {
                Console.WriteLine("Jupyter is not on: " + id + " " + url);
                var content = Encoding.UTF8.GetBytes("This CoFlows container doesn't have Jupter started...");
                await Response.Body.WriteAsync(content, 0, content.Length);
            }
            else
            {
                if(url == null) url = "";
                var queryString = Request.QueryString;
                url = "/lab/" + id + "/" + url + queryString.Value;

                var headers = new List<KeyValuePair<string, string>>();

                foreach(var head in Request.Headers)
                {
                    foreach(var val in head.Value)
                    {
                        try
                        {
                            headers.Add(new KeyValuePair<string, string>(head.Key, val));
                        }
                        catch{}
                    }
                }

                var mem = new MemoryStream();
                // Request.Body.CopyTo(mem);
                try
                {
                    await Request.Body.CopyToAsync(mem);
                }
                catch(Exception e){ Console.WriteLine(e);}
                mem.Position = 0;
                
                var _headers = headers;
                
                var streamContent = new StreamContent(mem);

                QuantApp.Kernel.User.ContextUser = new QuantApp.Kernel.UserData();
                HttpClient _httpClient = new HttpClient();
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

                int labPort = 0;
                if(Program.useJupyter && !LabDB.ContainsKey(cokey + id))
                {
                    QuantApp.Kernel.User quser = QuantApp.Kernel.User.FindUserBySecret(AccountController.sessionKeys[cokey]);
                    bool isRoot = quser.ID == "QuantAppSecure_root";

                    lastLabPort++;
                    LabDB[cokey + id] = lastLabPort;

                    var userName = System.Text.RegularExpressions.Regex.Replace(quser.Email, "[^a-zA-Z0-9 -]", "_");
                    var createUser = "import subprocess;newUser = '" + userName + "';userExists = newUser in list(map(lambda x: x.split(':')[0], subprocess.check_output(['getent', 'passwd']).decode('utf-8').split('\\n'))); print('User exists: ' + newUser) if userExists else subprocess.check_call(['adduser', '--gecos', '\"First Last,RoomNumber,WorkPhone,HomePhone\"', '--disabled-password', '--home', f'/app/mnt/home/{newUser}/', '--shell', '/bin/bash', f'{newUser}']); print('no need to edit .bashrc') if (('PS1=\"\\\\u:\\\\w> \"' in open(f'/app/mnt/home/{newUser}/.bashrc').read())) else open(f'/app/mnt/home/{newUser}/.bashrc', 'a').write('PS1=\"\\\\u:\\\\w> \"')";
                    // var createUser = "import subprocess;newUser = '" + userName + "';userExists = newUser in list(map(lambda x: x.split(':')[0], subprocess.check_output(['getent', 'passwd']).decode('utf-8').split('\\n'))); print('User exists: ' + newUser) if userExists else subprocess.check_call(['adduser', '--gecos', '\"First Last,RoomNumber,WorkPhone,HomePhone\"', '--disabled-password', '--home', f'/app/mnt/home/{newUser}/', '--shell', '/bin/bash', f'{newUser}']); print('no need to edit .bashrc') if userExists else open(f'/app/mnt/home/{newUser}/.bashrc', 'a').write('PS1=\"\\\\u:\\\\w> \"')";
                    var code = isRoot ? "import subprocess;subprocess.check_call(['jupyter', 'lab', '--port=" + lastLabPort + "', '--NotebookApp.notebook_dir=/app/mnt/', '--ip=*', '--NotebookApp.allow_remote_access=True', '--allow-root', '--no-browser', '--NotebookApp.token=\'\'', '--NotebookApp.password=\'\'', '--NotebookApp.disable_check_xsrf=True', '--NotebookApp.base_url=/lab/" + id + "'], cwd='/app/')" : "import subprocess;subprocess.check_call(['sudo', '-u', '" + userName + "', 'jupyter', 'lab', '--port=" + lastLabPort + "', '--NotebookApp.notebook_dir=/app/mnt/', '--ip=*', '--NotebookApp.allow_remote_access=True', '--no-browser', '--NotebookApp.token=\'\'', '--NotebookApp.password=\'\'', '--NotebookApp.disable_check_xsrf=True', '--NotebookApp.base_url=/lab/" + id + "'], cwd='/app/mnt/home/" + userName + "')";
                    var th = new System.Threading.Thread(() => {
                        using (Py.GIL())
                        {
                            Console.WriteLine("Starting Jupyter...");
                            
                            if(!isRoot)
                            {
                                Console.WriteLine(createUser);
                                PythonEngine.Exec(createUser);
                            }

                            Console.WriteLine(code);
                            PythonEngine.Exec(code);
                        }
                    });
                    th.Start();
                    System.Threading.Thread.Sleep(1000 * 5);
                }
                
                labPort = LabDB[cokey + id];

                var response = await _httpClient.PatchAsync("http://localhost:" + labPort + url, streamContent);

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
}