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


using CoFlows.Server.Models;
using CoFlows.Server.Utils;

using System.Net;
using System.IO;
using System.IO.Compression;
using System.IO.Compression;
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
using Python.Runtime;


using System.Net.Mail;

namespace CoFlows.Server.Controllers
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

            return Ok(Newtonsoft.Json.JsonConvert.DeserializeObject(quser.GetData(group, type)));
        }

        public class SaveUserDataClass
        {
            public string UserID;
            public string GroupID;
            public string Type;
            public Newtonsoft.Json.Linq.JObject data;
        }
        [HttpPost]
        public async Task<IActionResult> SaveUserData([FromBody] SaveUserDataClass data)//string groupid, string type)
        {
            string userId = this.User.QID();
            if (userId == null)
                return Unauthorized();

            QuantApp.Kernel.User quser = QuantApp.Kernel.User.FindUser(data.UserID);

            QuantApp.Kernel.Group group = QuantApp.Kernel.Group.FindGroup(data.GroupID);

            quser.SaveData(group, data.Type, data.ToString());

            return Ok(new { Result = "ok" });
        }

        [HttpGet]
        public async Task<IActionResult> Data(string type)
        {
            string userId = this.User.QID();
            if (userId == null)
                return Unauthorized();

            QuantApp.Kernel.User quser = QuantApp.Kernel.User.FindUser(userId);

            if (userId != null)
            {
                if(quser == null)
                    QuantApp.Kernel.User.ContextUser = new QuantApp.Kernel.UserData();
                else
                    QuantApp.Kernel.User.ContextUser = quser.ToUserData();
            }
            else
                QuantApp.Kernel.User.ContextUser = new QuantApp.Kernel.UserData();

            M m = M.Base(type);
            var res = m.KeyValues();

            QuantApp.Kernel.User.ContextUser = new QuantApp.Kernel.UserData();

            return Ok(res);
        }

        [HttpGet]
        public async Task<IActionResult> RawData(string type)
        {
            string userId = this.User.QID();
            if (userId == null)
                return Unauthorized();

            QuantApp.Kernel.User quser = QuantApp.Kernel.User.FindUser(userId);

            if (userId != null)
            {
                if(quser == null)
                    QuantApp.Kernel.User.ContextUser = new QuantApp.Kernel.UserData();
                else
                    QuantApp.Kernel.User.ContextUser = quser.ToUserData();
            }
            else
                QuantApp.Kernel.User.ContextUser = new QuantApp.Kernel.UserData();

            M m = M.Base(type);
            var res = m.RawEntries();

            QuantApp.Kernel.User.ContextUser = new QuantApp.Kernel.UserData();

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
        public async Task<IActionResult> Workflow(string id)
        {
            string userId = this.User.QID();
            if (userId == null)
                return Unauthorized();

            QuantApp.Kernel.User quser = QuantApp.Kernel.User.FindUser(userId);

            var m = M.Base(id);
            var res = m[x => true];
            if(res.Count > 0)
            {
                var workflow = res.FirstOrDefault() as Workflow;

                return Ok(workflow);

            }

            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> DefaultWorkflows()
        {
            string userId = this.User.QID();
            if (userId == null)
                return Unauthorized();

            QuantApp.Kernel.User quser = QuantApp.Kernel.User.FindUser(userId);

            var ws = Program.GetDefaultWorkflows();
            var filtered = new List<Workflow>();

            foreach(var w in ws)
                foreach(var p in w.Permissions)
                    if(p.ID == quser.Email && (int)p.Permission > (int)AccessType.Denied)
                        {
                            var newWorkflow = new Workflow(
                                w.ID, 
                                w.Name, 
                                w.Strategies,
                                null,//w.Code,
                                w.Agents, 
                                w.Permissions, 
                                w.NuGets, 
                                w.Pips, 
                                w.Jars, 
                                null,//w.Bins, 
                                null,//workflow.Files, 
                                w.ReadMe, 
                                w.Publisher, 
                                w.PublishTimestamp, 
                                w.AutoDeploy, 
                                w.Container);
                            filtered.Add(newWorkflow);
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

            var workflow_ids = QuantApp.Kernel.M.Base("--CoFlows--Workflows");
            if(workflow_ids[x => true].Where(x => x.ToString() == data.ID).Count() == 0)
            {
                workflow_ids.Add(data.ID);
                workflow_ids.Save();
            }

            var _g = Group.FindGroup(data.ID);
            if(_g == null)
                _g = Group.CreateGroup(data.ID, data.ID);
            
            foreach(var _p in data.Permissions)
            {
                string _id = "QuantAppSecure_" + _p.ID.ToLower().Replace('@', '.').Replace(':', '.');
                var _quser = QuantApp.Kernel.User.FindUser(_id);
                if(_quser != null)
                    _g.Add(_quser, typeof(QuantApp.Kernel.User), _p.Permission);
            }

            QuantApp.Kernel.User.ContextUser = quser.ToUserData();

            var res = Code.ProcessPackageJSON(data);

            QuantApp.Kernel.User.ContextUser = new QuantApp.Kernel.UserData();

            try
            {
                var wsp = QuantApp.Kernel.M.Base(data.ID)[x => true][0] as Workflow;
                
                var _pkg = QuantApp.Engine.Code.ProcessPackageWorkflow(wsp);
                if(Directory.Exists("/app/mnt/Agents"))
                    Directory.Delete("/app/mnt/Agents", true);
                if(Directory.Exists("/app/mnt/Base"))
                    Directory.Delete("/app/mnt/Base", true);
                if(Directory.Exists("/app/mnt/Files"))
                    Directory.Delete("/app/mnt/Files", true);
                if(Directory.Exists("/app/mnt/Queries"))
                    Directory.Delete("/app/mnt/Queries", true);

                var bytes = QuantApp.Engine.Code.ProcessPackageToZIP(_pkg);

                var archive = new ZipArchive(new MemoryStream(bytes));
                
                foreach(var entry in archive.Entries)
                {
                    var entryStream = entry.Open();
                    var streamReader = new StreamReader(entryStream);
                    var content = streamReader.ReadToEnd();
                    var filePath = "/app/mnt/" + entry.FullName;

                    System.IO.FileInfo file = new System.IO.FileInfo(filePath);
                    file.Directory.Create(); // If the directory already exists, this method does nothing.
                    System.IO.File.WriteAllText(file.FullName, content);

                    // var entryStream = entry.Open();
                    // var streamReader = new StreamReader(entryStream);
                    // var content = streamReader.ReadToEnd();
                    // var filePath = "/Workflow/" + entry.FullName;

                    // System.IO.FileInfo file = new System.IO.FileInfo(filePath);
                    // file.Directory.Create(); // If the directory already exists, this method does nothing.
                    // System.IO.File.WriteAllText(file.FullName, content);
                }

                foreach(var fid in wsp.Agents)
                {
                    var cfid = fid.Replace("$WID$", data.ID);
                    var f = F.Find(cfid).Value;
                    f.Start();
                }

                // var code = "import subprocess; subprocess.check_call(['jupyter', 'lab', '--NotebookApp.notebook_dir=/app/mnt', '--ip=*', '--NotebookApp.allow_remote_access=True', '--allow-root', '--no-browser', '--NotebookApp.token=\'\'', '--NotebookApp.password=\'\'', '--NotebookApp.disable_check_xsrf=True', '--NotebookApp.base_url=/lab/" + data.ID + "'])";


                // var th = new System.Threading.Thread(() => {
                //     using (Py.GIL())
                //     {
                //         Console.WriteLine("Starting Jupyter...");
                //         Console.WriteLine(code);
                //         PythonEngine.Exec(code);
                //     }
                // });
                // th.Start();

                Program.AddServicedWorkflows(wsp.ID);
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }

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
        public async Task<FileStreamResult> GetZipFromWorkflow(string id)
        {
            QuantApp.Kernel.User quser = null;
            string cokey = Request.Cookies["coflows"]; 
            if(cokey != null)
            {
                if(AccountController.sessionKeys.ContainsKey(cokey))
                {

                    quser = QuantApp.Kernel.User.FindUserBySecret(AccountController.sessionKeys[cokey]);
                    if(quser == null)
                    {
                        var content = Encoding.UTF8.GetBytes("Not Authorized...");
                        await Response.Body.WriteAsync(content, 0, content.Length);
                        return null;
                    }                    
                }
                else
                {
                    var content = Encoding.UTF8.GetBytes("Not Authorized...");
                    await Response.Body.WriteAsync(content, 0, content.Length);
                    return null;
                }
                
            }
            else
            {
                var content = Encoding.UTF8.GetBytes("Not Authorized...");
                await Response.Body.WriteAsync(content, 0, content.Length);
                return null;
            }

            // Response.Cookies.Append("coflows", key, new CookieOptions() { Expires = DateTime.Now.AddHours(24) });  
            
            
            // string userId = this.User.QID();
            // if (userId == null)
            //     return null;

            // QuantApp.Kernel.User quser = QuantApp.Kernel.User.FindUser(userId);

            var wsp = M.Base(id)[x => true][0] as Workflow;

            var up = QuantApp.Kernel.AccessType.Denied;
        
            foreach(var p in wsp.Permissions)
                if(p.ID == quser.Email && (int)p.Permission > (int)QuantApp.Kernel.AccessType.Denied)
                    up = p.Permission;
            

            if(up == AccessType.Write)
            {
                var pkg =  QuantApp.Engine.Code.ProcessPackageWorkflow(wsp);

                var bytes = QuantApp.Engine.Code.ProcessPackageToZIP(pkg);

                return new FileStreamResult(new MemoryStream(bytes), "application/zip")
                {
                    FileDownloadName = pkg.Name + ".zip"
                };
            }

            return null;
        }

        [HttpGet]
        public async Task<IActionResult> ServicedWorkflows()
        {
            string userId = this.User.QID();

            if (userId == null)
                return Unauthorized();

            QuantApp.Kernel.User quser = QuantApp.Kernel.User.FindUser(userId);

            if (userId != null)
            {
                if(quser == null)
                    QuantApp.Kernel.User.ContextUser = new QuantApp.Kernel.UserData();
                else
                    QuantApp.Kernel.User.ContextUser = quser.ToUserData();
            }
            else
                QuantApp.Kernel.User.ContextUser = new QuantApp.Kernel.UserData();

            var ws = Program.GetServicedWorkflows();
            var filtered = new List<Workflow>();

            foreach(var id in ws)
            {
                var added = false;
                var m = QuantApp.Kernel.M.Base(id);
                var l = m[x => true];
                if(l != null && l.Count > 0)
                {
                    var w = l[0] as Workflow;
                    foreach(var p in w.Permissions)
                        if(p.ID == quser.Email && (int)p.Permission > (int)AccessType.Denied || p.ID.ToLower() == "public")
                        {
                            added = true;
                            var newWorkflow = new Workflow(
                                w.ID, 
                                w.Name, 
                                w.Strategies,
                                null,//w.Code,
                                w.Agents, 
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
                            filtered.Add(newWorkflow);
                            break;
                        }
                    
                    if(!added)
                    {
                        var group = Group.FindGroup(id);
                        if(group != null && (int)group.Permission(quser.ToUserData()) > (int)AccessType.Denied)
                        {
                            added = true;
                            var newWorkflow = new Workflow(
                                w.ID, 
                                w.Name, 
                                w.Strategies,
                                null,//w.Code,
                                w.Agents, 
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
                            filtered.Add(newWorkflow);
                        }
                    }
                }
            }

            if(filtered.Count == 0)
                return Ok(filtered);

            var orderFilter = from s in filtered
                orderby s.Name 
                select s;

            QuantApp.Kernel.User.ContextUser = new QuantApp.Kernel.UserData();

            return Ok(orderFilter.ToList());
        }

        [HttpGet]
        public async Task<IActionResult> ActiveToggle(string id)
        {
            string userId = this.User.QID();
            if (userId == null)
                return Unauthorized();

            QuantApp.Kernel.User quser = QuantApp.Kernel.User.FindUser(userId);

            F f = F.Find(id).Value;

            var m = M.Base(f.WorkflowID);
            var res = m[x => true];
            if(res.Count > 0)
            {
                var w = res.FirstOrDefault() as Workflow;
                var up = QuantApp.Kernel.AccessType.Denied;

                var actives = CoFlows.Server.Program.GetDefaultWorkflows();
                var ok = false;
                foreach(var active in actives)
                    if(f.WorkflowID == active.ID)
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

                    var m = M.Base(f.WorkflowID);
                    var mres = m[x => true];
                    if(mres.Count > 0)
                    {
                        var workflow = mres.FirstOrDefault() as Workflow;

                        var flist = workflow.Agents.ToEnumerable();
                        var nflist = new List<string>();
                        
                        foreach(var fl in flist)
                            nflist.Add(fl);
                        nflist.Add(f.ID);
                        var ff = nflist.ToFSharplist();

                        var plist = workflow.Permissions.ToEnumerable();
                        var nplist = new List<Permission>();
                        
                        foreach(var pl in plist)
                            nplist.Add(pl);

                        var pp = nplist.ToFSharplist();

                        var newWorkflow = new Workflow(
                            workflow.ID, 
                            workflow.Name, 
                            workflow.Strategies,
                             workflow.Code, 
                             ff, 
                             pp, 
                             workflow.NuGets, 
                             workflow.Pips, 
                             workflow.Jars, 
                             workflow.Bins, 
                             workflow.Files, 
                             workflow.ReadMe, 
                             workflow.Publisher, 
                             workflow.PublishTimestamp, 
                             workflow.AutoDeploy, 
                             workflow.Container);
                        m.Exchange(workflow, newWorkflow);
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


                var wb = M.Base(data.Code.WorkflowID + "--Queries");
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

            var m = M.Base(data.Code.WorkflowID);
            var res = m[x => true];
            if(res.Count > 0)
            {
                var workflow = res.FirstOrDefault() as Workflow;
                var codes = new List<Tuple<string, string>>();

                // foreach(var c in workflow.Code)
                //     codes.Add(c);
                
                if(!string.IsNullOrEmpty(data.Code.Name))
                    // codes.Add(new Tuple<string, string>(data.Code.Name, data.Code.Code.Replace(data.Code.WorkflowID, "$WID$")));
                    codes.Add(new Tuple<string, string>(data.Code.Name, data.Code.Code));
                else
                {
                    var wb = M.Base(data.Code.WorkflowID + "--Queries");
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

            var wb = M.Base(data.WorkflowID + "--Queries");
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
        public async Task<IActionResult> GetQuery(string wid, string qid, string name, string uid, string[] p)
        {
            string userId = this.User.QID();
            if(!string.IsNullOrEmpty(uid))
            {
                QuantApp.Kernel.User quser = QuantApp.Kernel.User.FindUserBySecret(uid);
                if(quser == null)
                    QuantApp.Kernel.User.ContextUser = new QuantApp.Kernel.UserData();
                else
                    QuantApp.Kernel.User.ContextUser = quser.ToUserData();
            }
            else if (userId != null)
            {
                QuantApp.Kernel.User quser = QuantApp.Kernel.User.FindUser(userId);
                if(quser == null)
                    QuantApp.Kernel.User.ContextUser = new QuantApp.Kernel.UserData();
                else
                    QuantApp.Kernel.User.ContextUser = quser.ToUserData();
            }
            else
                QuantApp.Kernel.User.ContextUser = new QuantApp.Kernel.UserData();

            var m = M.Base(wid);
            var res = m[x => true];
            if(res.Count > 0)
            {
                var workflow = res.FirstOrDefault() as Workflow;

                var wb_m = M.Base(wid + "--Queries");
                var wb_res = wb_m[x => M.V<string>(x, "ID") == qid];
                if(wb_res.Count > 0)
                {
                    var wb = wb_res.FirstOrDefault() as CodeData;
                    var codes = new List<Tuple<string,string>>();

                    codes.Add(new Tuple<string, string>(wb.Name, wb.Code));


                    var execution = QuantApp.Engine.Utils.ExecuteCodeFunction(false, codes, name, p.Length == 0 ? null : p);
                    
                    var execution_result = execution.Result;
                    if(execution_result.Length == 0)
                    {
                        QuantApp.Kernel.User.ContextUser = new QuantApp.Kernel.UserData();
                        return Ok(execution.Compilation);
                    }
                        
                    foreach(var pair in execution_result)
                    {
                        if(pair.Item1 == name)
                        {
                            QuantApp.Kernel.User.ContextUser = new QuantApp.Kernel.UserData();
                            return Ok(pair.Item2);
                        }
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
            request.UserAgent = Program.hostName;
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

            var workflow_ids = QuantApp.Kernel.M.Base("--CoFlows--Workflows");
            if(workflow_ids[x => true].Where(x => x.ToString() == data.ID).Count() == 0)
            {
                workflow_ids.Add(data.ID);
                workflow_ids.Save();
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
    
        public static System.Collections.Concurrent.ConcurrentDictionary<string, int> LabDB = new System.Collections.Concurrent.ConcurrentDictionary<string, int>();
        private static int lastLabPort = 20000;
        public readonly static object objLockLabGet = new object();
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
    
    
        private static System.Collections.Concurrent.ConcurrentDictionary<string, int> DashDB = new System.Collections.Concurrent.ConcurrentDictionary<string, int>();
        private static System.Collections.Concurrent.ConcurrentDictionary<string, string> DashDBScript = new System.Collections.Concurrent.ConcurrentDictionary<string, string>();
        private static int lastPort = 10000;
        public readonly static object objLockDashGet = new object();

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
                

                    var m = M.Base(wid);
                    var res = m[x => true];
                    if(res.Count > 0)
                    {
                        var workflow = res.FirstOrDefault() as Workflow;
                        var codes = new List<Tuple<string, string>>();
                        
                        var wb = M.Base(wid + "--Queries");
                        var wb_res = wb[x => M.V<string>(x, "ID") == qid];
                        if(wb_res.Count > 0)
                        {
                            var item = wb_res.FirstOrDefault() as CodeData;
                            codes.Add(new Tuple<string, string>(item.Name, item.Code));
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

                    
                    

                    var m = M.Base(wid);
                    var res = m[x => true];
                    if(res.Count > 0)
                    {
                        var workflow = res.FirstOrDefault() as Workflow;
                        var codes = new List<Tuple<string, string>>();
                        
                        var wb = M.Base(wid + "--Queries");
                        var wb_res = wb[x => M.V<string>(x, "ID") == qid];
                        if(wb_res.Count > 0)
                        {
                            var item = wb_res.FirstOrDefault() as CodeData;
                            codes.Add(new Tuple<string, string>(item.Name, item.Code));
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