/*
 * The MIT License (MIT)
 * Copyright (c) 2007-2019, Arturo Rodriguez All rights reserved.
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


using QuantApp.Server.Models;
using QuantApp.Server.Utils;

using System.Net;
using System.IO;
using System.Text;
using System.Net.Http;
using System.Net.Http.Headers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http;

using Newtonsoft.Json;
using QuantApp.Kernel;
using QuantApp.Engine;

using System.Net.Mail;

namespace QuantApp.Server.Controllers
{
    [Authorize, Route("[controller]/[action]")]
    public class MController : Controller
    {
        
        public class FunctionData
        {
            public string Name {get;set;}
            public object[] Parameters {get;set;}
        }

        public class CallData
        {
            public CodeData Code {get;set;}
            public FunctionData Function {get;set;}
        }

        public class FData
        {
            public string ID {get;set;}
            public string Parameters {get;set;}
        }

        [HttpGet]
        public async Task<IActionResult> UserData(string groupid, string type)
        {
            string userId = this.User.QID();
            if (userId == null)
                return Unauthorized();

            QuantApp.Kernel.User quser = QuantApp.Kernel.User.FindUser(userId);

            QuantApp.Kernel.Group group = QuantApp.Kernel.Group.FindGroup(groupid);
            string data = quser.GetData(group, type);

            return Ok(data);
        }

        [HttpGet]
        public async Task<IActionResult> Data(string type)
        {
            string userId = this.User.QID();
            if (userId == null)
                return Unauthorized();

            QuantApp.Kernel.User quser = QuantApp.Kernel.User.FindUser(userId);

            M m = M.Base(type);
            var res = m.KeyValues();

            return Ok(res);
        }

        [HttpGet]
        public async Task<IActionResult> RawData(string type)
        {
            string userId = this.User.QID();
            if (userId == null)
                return Unauthorized();

            QuantApp.Kernel.User quser = QuantApp.Kernel.User.FindUser(userId);

            M m = M.Base(type);
            var res = m.RawEntries();

            return Ok(res);
        }


        [HttpGet]
        public async Task<IActionResult> Save(string type)
        {
            string userId = this.User.QID();
            if (userId == null)
                return Unauthorized();

            QuantApp.Kernel.User quser = QuantApp.Kernel.User.FindUser(userId);

            M m = M.Base(type);
            m.Save();

            return Ok(new { Result = "saved"});
        }

        [HttpGet]
        public async Task<IActionResult> WorkSpace(string id)
        {
            string userId = this.User.QID();
            if (userId == null)
                return Unauthorized();

            QuantApp.Kernel.User quser = QuantApp.Kernel.User.FindUser(userId);

            var m = M.Base(id);
            var res = m[x => true];
            if(res.Count > 0)
            {
                var workSpace = res.FirstOrDefault() as WorkSpace;

                return Ok(workSpace);

            }

            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> DefaultWorkSpaces()
        {
            string userId = this.User.QID();
            if (userId == null)
                return Unauthorized();

            QuantApp.Kernel.User quser = QuantApp.Kernel.User.FindUser(userId);

            var ws = Program.GetDefaultWorkSpaces();
            var filtered = new List<WorkSpace>();

            foreach(var w in ws)
                foreach(var p in w.Permissions)
                    if(p.ID == quser.Email && (int)p.Permission > (int)AccessType.Denied)
                        {
                            var newWorkSpace = new WorkSpace(
                                w.ID, 
                                w.Name, 
                                w.Strategies,
                                null,//w.Code,
                                w.Functions, 
                                w.Permissions, 
                                w.NuGets, 
                                w.Pips, 
                                w.Jars, 
                                null,//w.Bins, 
                                null,//workSpace.Files, 
                                w.ReadMe, 
                                w.Publisher, 
                                w.PublishTimestamp, 
                                w.AutoDeploy, 
                                w.Container);
                            filtered.Add(newWorkSpace);
                        }
                
            

            return Ok(filtered);
        }

        [HttpPost]
        public async Task<IActionResult> CreatePKGFromJSON([FromBody] PKG data)
        {
            string userId = this.User.QID();
            if (userId == null)
                return Unauthorized();

            QuantApp.Kernel.User quser = QuantApp.Kernel.User.FindUser(userId);

            var workspace_ids = QuantApp.Kernel.M.Base("--CoFlows--WorkSpaces");
            if(workspace_ids[x => true].Where(x => x.ToString() == data.ID).Count() == 0)
            {
                workspace_ids.Add(data.ID);
                workspace_ids.Save();
            }

            QuantApp.Kernel.User.ContextUser = quser.ToUserData();

            var res = Code.ProcessPackageJSON(data);

            QuantApp.Kernel.User.ContextUser = new QuantApp.Kernel.UserData();

            return Ok(new {Result = res});
        }

        
        [HttpPost]
        public async Task<IActionResult> CompileFromJSON([FromBody] PKG data)
        {
            string userId = this.User.QID();
            if (userId == null)
                return Unauthorized();

            QuantApp.Kernel.User quser = QuantApp.Kernel.User.FindUser(userId);

            var newdata = new PKG(
                data.ID + "-" + Guid.NewGuid().ToString(),
                data.Name,
                data.Base,
                data.Agents,
                data.Queries,
                data.Permissions,
                data.NuGets,
                data.Pips,
                data.Jars,
                data.Bins,
                data.Files,
                data.ReadMe,
                data.Publisher,
                data.PublishTimestamp,
                data.AutoDeploy,
                data.Container);


            QuantApp.Kernel.User.ContextUser = quser.ToUserData();

            var res = Code.BuildCompileOnlyPackage(newdata);

            QuantApp.Kernel.User.ContextUser = new QuantApp.Kernel.UserData();


            return Ok(new {Result = res});
        }

        [HttpGet, AllowAnonymous]
        public async Task<FileStreamResult> GetZipFromWorkSpace(string id)
        {
            
            string key = Request.Cookies["coflows"]; 

            if(key == null)
                return null;
            
            QuantApp.Kernel.User quser = QuantApp.Kernel.User.FindUserBySecret(key);
            if(quser == null)
                return null;

            Response.Cookies.Append("coflows", key, new CookieOptions() { Expires = DateTime.Now.AddHours(24) });  
            
            
            // string userId = this.User.QID();
            // if (userId == null)
            //     return null;

            // QuantApp.Kernel.User quser = QuantApp.Kernel.User.FindUser(userId);

            var wsp = M.Base(id)[x => true][0] as WorkSpace;

            var up = QuantApp.Kernel.AccessType.Denied;
        
            foreach(var p in wsp.Permissions)
                if(p.ID == quser.Email && (int)p.Permission > (int)QuantApp.Kernel.AccessType.Denied)
                    up = p.Permission;
            

            if(up == AccessType.Write)
            {
                var pkg =  QuantApp.Engine.Code.ProcessPackageWorkspace(wsp);

                var bytes = QuantApp.Engine.Code.ProcessPackageToZIP(pkg);

                return new FileStreamResult(new MemoryStream(bytes), "application/zip")
                {
                    FileDownloadName = pkg.Name + ".zip"
                };
            }

            return null;
        }

        [HttpGet]
        public async Task<IActionResult> ServicedWorkSpaces()
        {
            string userId = this.User.QID();
            if (userId == null)
                return Unauthorized();

            QuantApp.Kernel.User quser = QuantApp.Kernel.User.FindUser(userId);

            var ws = Program.GetServicedWorkSpaces();
            var filtered = new List<WorkSpace>();

            foreach(var id in ws)
            {
                var w = QuantApp.Kernel.M.Base(id)[x => true].FirstOrDefault() as WorkSpace;
                foreach(var p in w.Permissions)
                    if(p.ID == quser.Email && (int)p.Permission > (int)AccessType.Denied || p.ID.ToLower() == "public")
                    {
                        var newWorkSpace = new WorkSpace(
                            w.ID, 
                            w.Name, 
                            w.Strategies,
                            null,//w.Code,
                            w.Functions, 
                            w.Permissions, 
                            w.NuGets, 
                            w.Pips, 
                            w.Jars, 
                            null,//w.Bins, 
                            null,//workSpace.Files, 
                            w.ReadMe, 
                            w.Publisher, 
                            w.PublishTimestamp, 
                            w.AutoDeploy, 
                            w.Container);
                        filtered.Add(newWorkSpace);
                    }
            }
                
            

            return Ok(filtered.GroupBy(p => p.ID).Select(g => g.FirstOrDefault()).OrderBy(x => x.Name));
        }

        [HttpGet]
        public async Task<IActionResult> ActiveToggle(string id)
        {
            string userId = this.User.QID();
            if (userId == null)
                return Unauthorized();

            QuantApp.Kernel.User quser = QuantApp.Kernel.User.FindUser(userId);

            F f = F.Find(id).Value;

            var m = M.Base(f.WorkspaceID);
            var res = m[x => true];
            if(res.Count > 0)
            {
                var w = res.FirstOrDefault() as WorkSpace;
                var up = QuantApp.Kernel.AccessType.Denied;

                var actives = QuantApp.Server.Program.GetDefaultWorkSpaces();
                var ok = false;
                foreach(var active in actives)
                    if(f.WorkspaceID == active.ID)
                        ok = true;

            
                foreach(var p in w.Permissions)
                    if(p.ID == quser.Email && (int)p.Permission > (int)QuantApp.Kernel.AccessType.Denied)
                        up = p.Permission;

                if(up == QuantApp.Kernel.AccessType.Write && ok)
                {
                    if(!f.Started)
                        f.Start();

                    else
                        f.Stop();

                }

            }

            return Ok(new { Ok = f.Started});
        }

        [HttpGet]
        public async Task<IActionResult> GetF(string id, string parameters)
        {
            string userId = this.User.QID();
            if (userId == null)
                return Unauthorized();

            QuantApp.Kernel.User quser = QuantApp.Kernel.User.FindUser(userId);

            F f = F.Find(id).Value;

            return Ok(f.Body(parameters));
        }

        
        [HttpPost]
        public async Task<IActionResult> PostF([FromBody] FData data)
        {
            string userId = this.User.QID();
            if (userId == null)
                return Unauthorized();

            QuantApp.Kernel.User quser = QuantApp.Kernel.User.FindUser(userId);

            F f = F.Find(data.ID).Value;

            return Ok(f.Body(data.Parameters));
        }

        [HttpPost]
        public async Task<IActionResult> CreateF([FromBody] FMeta data)
        {
            string userId = this.User.QID();
            if (userId == null)
                return Unauthorized();

            QuantApp.Kernel.User quser = QuantApp.Kernel.User.FindUser(userId);


            string res = QuantApp.Engine.Utils.RegisterCode(true, true, data.Code.Select(x => new System.Tuple<string, string>(x.Item1, x.Item2.Replace("\\n", "\r\n"))).ToFSharplist());

            if (res.Contains("Execution successfully completed.") || res.Trim() == "")
            {
                if(String.IsNullOrEmpty(data.ID))
                {
                    F f = F.Create(data).Item1;
                
                    if(data.Started)
                        f.Start();
                    else
                        f.Stop();

                    var m = M.Base(f.WorkspaceID);
                    var mres = m[x => true];
                    if(mres.Count > 0)
                    {
                        var workSpace = mres.FirstOrDefault() as WorkSpace;

                        var flist = workSpace.Functions.ToEnumerable();
                        var nflist = new List<string>();
                        
                        foreach(var fl in flist)
                            nflist.Add(fl);
                        nflist.Add(f.ID);
                        var ff = nflist.ToFSharplist();

                        var plist = workSpace.Permissions.ToEnumerable();
                        var nplist = new List<Permission>();
                        
                        foreach(var pl in plist)
                            nplist.Add(pl);

                        var pp = nplist.ToFSharplist();

                        var newWorkSpace = new WorkSpace(
                            workSpace.ID, 
                            workSpace.Name, 
                            workSpace.Strategies,
                             workSpace.Code, 
                             ff, 
                             pp, 
                             workSpace.NuGets, 
                             workSpace.Pips, 
                             workSpace.Jars, 
                             workSpace.Bins, 
                             workSpace.Files, 
                             workSpace.ReadMe, 
                             workSpace.Publisher, 
                             workSpace.PublishTimestamp, 
                             workSpace.AutoDeploy, 
                             workSpace.Container);
                        m.Exchange(workSpace, newWorkSpace);
                        m.Save();
                    }
                }
                else
                {
                    F f = F.Find(data.ID).Value;

                    if(data.Started)
                        f.Start();
                    else
                        f.Stop();


                    f.AddFunctionName = data.Add;
                    f.BodyFunctionName = data.Body;
                    f.Description = data.Description;
                    f.ExchangeFunctionName = data.Exchange;
                    f.JobFunctionName = data.Job;
                    f.LoadFunctionName = data.Load;
                    f.MID = data.MID;
                    f.Name = data.Name;
                    f.RemoveFunctionName = data.Remove;
                    f.ScheduleCommand = data.ScheduleCommand;
                    f.ScriptCode = data.Code.Select(x => new Tuple<string, string>(x.Item1, x.Item2.Replace("\\n", Environment.NewLine))).ToFSharplist();
                }
            }

            return Ok(new {Result = res});
        }

        [HttpPost]
        //HERE
        public async Task<IActionResult> PostEC([FromBody] CallData data)
        {
            string userId = this.User.QID();
            if (userId == null)
                return Unauthorized();

            QuantApp.Kernel.User quser = QuantApp.Kernel.User.FindUser(userId);

            QuantApp.Kernel.User.ContextUser = quser.ToUserData();

            if(!string.IsNullOrEmpty(data.Code.Name))
            {
                if(string.IsNullOrWhiteSpace(data.Code.ID))
                    data.Code.ID = System.Guid.NewGuid().ToString();


                var wb = M.Base(data.Code.WorkspaceID + "--Workbook");
                var wb_res = wb[x => M.V<string>(x, "ID") == data.Code.ID];
                if(wb_res.Count > 0)
                {
                    var item = wb_res.FirstOrDefault() as CodeData;
                    wb.Exchange(item, data.Code);
                }
                else
                    wb.Add(data.Code);

                wb.Save();
            }

            var m = M.Base(data.Code.WorkspaceID);
            var res = m[x => true];
            if(res.Count > 0)
            {
                var workSpace = res.FirstOrDefault() as WorkSpace;
                var codes = new List<Tuple<string, string>>();

                // foreach(var c in workSpace.Code)
                //     codes.Add(c);
                
                if(!string.IsNullOrEmpty(data.Code.Name))
                    // codes.Add(new Tuple<string, string>(data.Code.Name, data.Code.Code.Replace(data.Code.WorkspaceID, "$WID$")));
                    codes.Add(new Tuple<string, string>(data.Code.Name, data.Code.Code));
                else
                {
                    var wb = M.Base(data.Code.WorkspaceID + "--Workbook");
                    var wb_res = wb[x => M.V<string>(x, "ID") == data.Code.ID];
                    if(wb_res.Count > 0)
                    {
                        var item = wb_res.FirstOrDefault() as CodeData;
                        codes.Add(new Tuple<string, string>(item.Name, item.Code));
                        
                    }
                }

                var result = QuantApp.Engine.Utils.ExecuteCodeFunction(true, codes, data.Function.Name, data.Function.Parameters);

                QuantApp.Kernel.User.ContextUser = new QuantApp.Kernel.UserData();

                return Ok(result);
            }

            QuantApp.Kernel.User.ContextUser = new QuantApp.Kernel.UserData();

            return Ok(new { Result = "Empty" });
        }

        [HttpPost]
        public async Task<IActionResult> RemoveEC([FromBody] CodeData data)
        {
            string userId = this.User.QID();
            if (userId == null)
                return Unauthorized();

            QuantApp.Kernel.User quser = QuantApp.Kernel.User.FindUser(userId);

            if(string.IsNullOrWhiteSpace(data.ID))
                data.ID = System.Guid.NewGuid().ToString();

            var wb = M.Base(data.WorkspaceID + "--Workbook");
            var wb_res = wb[x => M.V<string>(x, "ID") == data.ID];
            if(wb_res.Count > 0)
            {
                var item = wb_res.FirstOrDefault();
                wb.Remove(item);
            }

            wb.Save();


            return Ok(new { Result = "Done" });
        }

        [HttpGet, AllowAnonymous]
        public async Task<IActionResult> GetWB(string workbook, string id, string name, string[] p)
        {
            string userId = this.User.QID();
            if (userId != null)
            {
                QuantApp.Kernel.User quser = QuantApp.Kernel.User.FindUser(userId);
                QuantApp.Kernel.User.ContextUser = quser.ToUserData();
            }
            else
                QuantApp.Kernel.User.ContextUser = new QuantApp.Kernel.UserData();

            var m = M.Base(workbook);
            var res = m[x => true];
            if(res.Count > 0)
            {
                var workSpace = res.FirstOrDefault() as WorkSpace;

                var wb_m = M.Base(workbook + "--Workbook");
                var wb_res = wb_m[x => M.V<string>(x, "ID") == id];
                if(wb_res.Count > 0)
                {
                    var wb = wb_res.FirstOrDefault() as CodeData;
                    var codes = new List<Tuple<string,string>>();

                    codes.Add(new Tuple<string, string>(wb.Name, wb.Code));


                    var execution = QuantApp.Engine.Utils.ExecuteCodeFunction(false, codes, name, p.Length == 0 ? null : p);
                    
                    var execution_result = execution.Result;
                    if(execution_result.Length == 0)
                        return Ok(execution.Compilation);
                        
                    foreach(var pair in execution_result)
                    {
                        if(pair.Item1 == name)
                            return Ok(pair.Item2);
                    }
                }
            }

            QuantApp.Kernel.User.ContextUser = new QuantApp.Kernel.UserData();
            return Ok(new { Result = "Empty" });
        }

        [HttpPost, AllowAnonymous]
        public async Task<IActionResult> GitHubPost(string key, string token, [FromBody]Newtonsoft.Json.Linq.JObject payload)
        {
            QuantApp.Kernel.User quser = QuantApp.Kernel.User.FindUserBySecret(key);
            if(quser == null)
                return Unauthorized();

            QuantApp.Kernel.User.ContextUser = quser.ToUserData();
            
            Console.WriteLine("GitHub push start...");
            Console.WriteLine("         QuantApp: " + key);
            Console.WriteLine("         QuantApp User: " + quser.Email);
            Console.WriteLine("         GitHub: " + token);
            string branch =  (string)payload["ref"];

            branch = branch.Substring(branch.LastIndexOf("/") + 1);
            Console.WriteLine("         Branch: " + branch);
            string zip_url = (string)payload["repository"]["svn_url"] + "/archive/" + branch + ".zip";
            Console.WriteLine("         Git Zip: " + zip_url);

            string committerEmail = (string)payload["head_commit"]["committer"]["email"];
            string committerName = (string)payload["head_commit"]["committer"]["name"];
            
            var request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(zip_url);

            if(!String.IsNullOrEmpty(token))
                request.Headers.Add(System.Net.HttpRequestHeader.Authorization, string.Concat("token ", token));

            request.Accept = "application/vnd.github.v3.raw";
            request.UserAgent = "coflows.quant.app";
            var response = request.GetResponse();
            

            var memoryStream = new MemoryStream();
            
            response.GetResponseStream().CopyTo(memoryStream);
            var bytes = memoryStream.ToArray();

            var data_old = Code.ProcessPackageFromGit(bytes);

            var data = branch == "master" ? data_old : new PKG(
                data_old.ID.EndsWith("-" + branch) ? data_old.ID : data_old.ID + "-" + branch,
                data_old.Name.EndsWith("-" + branch) ? data_old.Name : data_old.Name + "-" + branch,
                data_old.Base,
                data_old.Agents,
                data_old.Queries,
                data_old.Permissions,
                data_old.NuGets,
                data_old.Pips,
                data_old.Jars,
                data_old.Bins,
                data_old.Files,
                data_old.ReadMe,
                data_old.Publisher,
                data_old.PublishTimestamp,
                data_old.AutoDeploy,
                data_old.Container);


            if(!data.AutoDeploy)
            {
                Console.WriteLine("GitHub push done... no auto deploy");
                return Ok(new {Result = "Package is not set to autodeploy."});
            }
            

            /////////////////

            var workspace_ids = QuantApp.Kernel.M.Base("--CoFlows--WorkSpaces");
            if(workspace_ids[x => true].Where(x => x.ToString() == data.ID).Count() == 0)
            {
                workspace_ids.Add(data.ID);
                workspace_ids.Save();
            }

            QuantApp.Kernel.User.ContextUser = quser.ToUserData();

            var res = Code.ProcessPackageJSON(data);

            QuantApp.Kernel.User.ContextUser = new QuantApp.Kernel.UserData();
            Console.WriteLine("GitHub push done local");

            string mail_res1 = res.ToString();

            RTDEngine.Send(
                    new List<string>() {committerEmail + ";" + committerName}, 
                    "arturo@quant.app;CoFlows Builder", 
                    "GitHub build: " + data.Name + " (" + data.ID + ")", 
                    String.IsNullOrEmpty(mail_res1) ? "Contratulations! Package was successfully deployed to a cloud container..." : mail_res1);

            return Ok(res);
        }
    

        [AllowAnonymous,HttpGet("/lab/{id}/{**url}")]
        public async Task LabGet(string id, string url = "")
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

            var response = await _httpClient.GetAsync("http://localhost:8888" + url);
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

        [AllowAnonymous,HttpPost("/lab/{id}/{**url}")]
        public async Task LabPost(string id, string url = "")
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
            QuantApp.Kernel.User.ContextUser = new QuantApp.Kernel.UserData();
            var response = await _httpClient.PostAsync("http://localhost:8888" + url, streamContent);

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

        [AllowAnonymous,HttpPut("/lab/{id}/{**url}")]
        public async Task LabPut(string id, string url = "")
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
            var response = await _httpClient.PutAsync("http://localhost:8888" + url, streamContent);
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

        [AllowAnonymous,HttpPatch("/lab/{id}/{**url}")]
        public async Task LabPatch(string id, string url = "")
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
            var response = await _httpClient.PatchAsync("http://localhost:8888" + url, streamContent);

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
    
    
        private static System.Collections.Concurrent.ConcurrentDictionary<string, int> DashDB = new System.Collections.Concurrent.ConcurrentDictionary<string, int>();
        private static int lastPort = 10000;

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
            if(DashDB.ContainsKey(dashID))
                port = DashDB[dashID];
            else
            {
                lastPort++;
                port = lastPort;
                DashDB.TryAdd(dashID, port);
            }

            var m = M.Base(wid);
            var res = m[x => true];
            if(res.Count > 0)
            {
                var workSpace = res.FirstOrDefault() as WorkSpace;
                var codes = new List<Tuple<string, string>>();
                
                var wb = M.Base(wid + "--Workbook");
                var wb_res = wb[x => M.V<string>(x, "ID") == qid];
                if(wb_res.Count > 0)
                {
                    var item = wb_res.FirstOrDefault() as CodeData;
                    codes.Add(new Tuple<string, string>(item.Name, item.Code));
                }

                var result = QuantApp.Engine.Utils.ExecuteCodeFunction(true, codes, "run", new object[]{port, "/dash/" + wid + "/" + qid + "/"});
            }
            
            
            if(url == null) url = "";
            var queryString = Request.QueryString;
            url = "/dash/" + wid + "/" + qid + "/" + url + queryString.Value;

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
            for(int i = 0; i < 100; i++)
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
            if(DashDB.ContainsKey(dashID))
                port = DashDB[dashID];
            else
            {
                lastPort++;
                port = lastPort;
                DashDB.TryAdd(dashID, port);
            }

            if(url == null) url = "";
            var queryString = Request.QueryString;
            url = "/dash/" + wid + "/" + qid + "/" + url + queryString.Value;
            

            var m = M.Base(wid);
            var res = m[x => true];
            if(res.Count > 0)
            {
                var workSpace = res.FirstOrDefault() as WorkSpace;
                var codes = new List<Tuple<string, string>>();
                
                var wb = M.Base(wid + "--Workbook");
                var wb_res = wb[x => M.V<string>(x, "ID") == qid];
                if(wb_res.Count > 0)
                {
                    var item = wb_res.FirstOrDefault() as CodeData;
                    codes.Add(new Tuple<string, string>(item.Name, item.Code));
                }
                var result = QuantApp.Engine.Utils.ExecuteCodeFunction(true, codes, "run", new object[]{port, "/dash/" + wid + "/" + qid + "/"});
            }

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