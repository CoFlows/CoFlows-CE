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


using CoFlows.Server.Utils;

using System.IO;
using System.IO.Compression;
using System.Text;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

using QuantApp.Kernel;
using QuantApp.Engine;

namespace CoFlows.Server.Controllers
{
    [Authorize, Route("[controller]/[action]")]
    public class FlowController : Controller
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

        /// <summary>
        /// Get Workflow description as JSON object
        /// </summary>
        /// <param name="id">Workflow ID</param>
        /// <returns>Success</returns>
        /// <response code="200">Workflow JSON object</response>
        /// <response code="400">Workflow not found</response>
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

            return BadRequest(new { Data = "Workfow not found" });
        }

        /// <summary>
        /// Get list of Workflows managed by this container described as JSON objects
        /// </summary>
        /// <returns>Success</returns>
        /// <response code="200">Workflow JSON object</response>
        [HttpGet]
        public async Task<IActionResult> LocalWorkflows()
        {
            string userId = this.User.QID();
            if (userId == null)
                return Unauthorized();

            QuantApp.Kernel.User quser = QuantApp.Kernel.User.FindUser(userId);

            var ws = Program.LocalWorkflows();
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

        /// <summary>
        /// Create a workflow sending a JSON object
        /// </summary>
        /// <param name="data">Workflow object accoring to PKG schema</param>
        /// <returns>Success</returns>
        /// <response code="200">Workflow JSON object</response>
        /// <response code="400">Workflow not found</response>
        [HttpPost]
        public async Task<IActionResult> CreateWorkflow([FromBody] PKG data)
        // public async Task<IActionResult> CreatePKGFromJSON([FromBody] PKG data)
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


            try
            {
                var res = Code.ProcessPackageJSON(data);

                QuantApp.Kernel.User.ContextUser = new QuantApp.Kernel.UserData();

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
                }

                foreach(var fid in wsp.Agents)
                {
                    var cfid = fid.Replace("$WID$", data.ID);
                    var f = F.Find(cfid).Value;
                    f.Start();
                }

                Program.AddServicedWorkflows(wsp.ID);

                return Ok(new {Result = res});
            }
            catch(Exception e)
            {
                workflow_ids = QuantApp.Kernel.M.Base("--CoFlows--Workflows");

                Console.WriteLine("------- FLOW ERROR");

                foreach(var rr in workflow_ids[x => true])
                    Console.WriteLine(rr.ToString());
                
                if(workflow_ids[x => true].Where(x => x.ToString() == data.ID).Count() > 0)
                {
                    Console.WriteLine("Remove this workflow from startup...");
                    workflow_ids.Remove(data.ID);
                    workflow_ids.Save();
                }

                Console.WriteLine(e);

                throw e;
            }

            
        }
        
        /// <summary>
        /// Compile a workflow sending a JSON object
        /// </summary>
        /// <remark>
        /// This doesn't register or activate the 
        /// </remark>
        /// <param name="data">Workflow object accoring to PKG schema</param>
        /// <returns>Success</returns>
        /// <response code="200">Workflow JSON object</response>
        /// <response code="400">Workflow not found</response>
        [HttpPost]
        public async Task<IActionResult> CompileWorkflow([FromBody] PKG data)
        // public async Task<IActionResult> CompileFromJSON([FromBody] PKG data)
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

        /// <summary>
        /// Get zip file with workflow package
        /// </summary>
        /// <param name="id">Workflow ID</param>
        /// <returns>Success</returns>
        /// <response code="200"></response>
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

        /// <summary>
        /// Get list of Workflows accessible through this container described as JSON objects. A container can be managed 
        /// </summary>
        /// <returns>Success</returns>
        /// <response code="200">Workflow JSON object</response>
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

        /// <summary>
        /// Toggle to activate / deactive an agent's cron job
        /// </summary>
        /// <param name="id">Workflow ID</param>
        /// <returns>Success</returns>
        /// <response code="200"></response>
        /// <response code="400">Agent not found</response>
        [HttpGet]
        public async Task<IActionResult> ActiveAgentToggle(string id)
        {
            string userId = this.User.QID();
            if (userId == null)
                return Unauthorized();

            QuantApp.Kernel.User quser = QuantApp.Kernel.User.FindUser(userId);

            F f = F.Find(id).Value;

            if(f == null)
                return BadRequest(new { Data = "Agent not found" } );

            var m = M.Base(f.WorkflowID);
            var res = m[x => true];
            if(res.Count > 0)
            {
                var w = res.FirstOrDefault() as Workflow;
                var up = QuantApp.Kernel.AccessType.Denied;

                var actives = CoFlows.Server.Program.LocalWorkflows();
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

        /// <summary>
        /// Call Agent Body function through POST
        /// </summary>
        /// <returns>Success</returns>
        /// <param name="id">Agent ID</param>
        /// <param name="_cokey">User secrect</param>
        /// <param name="data">JSON object</param>
        /// <response code="200"></response>
        /// <response code="400">Agent not found</response>
        [HttpPost("{id}/{**parameters}"), AllowAnonymous]
        public async Task<IActionResult> Agent(string id, string _cokey, [FromBody] Newtonsoft.Json.Linq.JObject data)
        {
            string userId = this.User.QID();
            
            if(this.Request.Headers.ContainsKey("_cokey"))
                _cokey = this.Request.Headers["_cokey"];
            
            if(!string.IsNullOrEmpty(_cokey))
            {
                QuantApp.Kernel.User quser = QuantApp.Kernel.User.FindUserBySecret(_cokey);
                if(quser == null)
                    QuantApp.Kernel.User.ContextUser = new QuantApp.Kernel.UserData();
                else
                {
                    QuantApp.Kernel.User.ContextUser = quser.ToUserData();
                    userId = quser.ID;
                }
            }
            else if (userId != null)
            {
                QuantApp.Kernel.User quser = QuantApp.Kernel.User.FindUser(userId);
                if(quser == null)
                    QuantApp.Kernel.User.ContextUser = new QuantApp.Kernel.UserData();
                else
                {
                    QuantApp.Kernel.User.ContextUser = quser.ToUserData();
                    userId = quser.ID;
                }
            }
            else
                QuantApp.Kernel.User.ContextUser = new QuantApp.Kernel.UserData();
            
            var _agent = F.Find(id);

            if(_agent == null)
                return BadRequest(new { Data = "Agent not found" } );

            F f = _agent.Value;

            var result = f.Body(Newtonsoft.Json.JsonConvert.SerializeObject(data));

            QuantApp.Kernel.User.ContextUser = new QuantApp.Kernel.UserData();

            return Ok(result);
        }

        /// <summary>
        /// Create an Agent
        /// </summary>
        /// <returns>Success</returns>
        /// <param name="data">
        /// Data:
        ///
        ///     {
        ///         "ID": "Workflow ID",
        ///         "Parameters": "serialized JSON parameters"
        ///     }
        ///
        /// </param>
        /// <response code="200"></response>
        /// <response code="400">Agent not found</response>
        [HttpPost]
        public async Task<IActionResult> CreateAgent([FromBody] FMeta data)
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

        /// <summary>
        /// Create a new or alter existing Query
        /// </summary>
        /// <returns>Success</returns>
        /// <param name="data">JSON object according to Schema</param>
        /// <response code="200"></response>
        [HttpPost]
        public async Task<IActionResult> CreateQuery([FromBody] CallData data)
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

                if(!string.IsNullOrEmpty(data.Code.Name))
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

        /// <summary>
        /// Remove a Query
        /// </summary>
        /// <returns>Success</returns>
        /// <param name="data">JSON object according to Schema</param>
        /// <response code="200"></response>
        [HttpPost]
        public async Task<IActionResult> RemoveQuery([FromBody] CodeData data)
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

        /// <summary>
        /// Execute a Query
        /// </summary>
        /// <returns>Success</returns>
        /// <param name="wid">Workflow ID</param>
        /// <param name="qid">Query ID</param>
        /// <param name="name">Query name</param>
        /// <param name="_cokey">User secrect</param>
        /// <param name="p">parameters in list format</param>
        /// <response code="200">Result of query in JSON format</response>
        [HttpGet, AllowAnonymous]
        public async Task<IActionResult> GetQuery(string wid, string qid, string name, string _cokey, string[] p)
        {
            string userId = this.User.QID();
            if(!string.IsNullOrEmpty(_cokey))
            {
                QuantApp.Kernel.User quser = QuantApp.Kernel.User.FindUserBySecret(_cokey);
                if(quser == null)
                    QuantApp.Kernel.User.ContextUser = new QuantApp.Kernel.UserData();
                else
                {
                    QuantApp.Kernel.User.ContextUser = quser.ToUserData();
                    userId = quser.ID;
                }
            }
            else if (userId != null)
            {
                QuantApp.Kernel.User quser = QuantApp.Kernel.User.FindUser(userId);
                if(quser == null)
                    QuantApp.Kernel.User.ContextUser = new QuantApp.Kernel.UserData();
                else
                {
                    QuantApp.Kernel.User.ContextUser = quser.ToUserData();
                    userId = quser.ID;
                }
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

                    // Check permissions from meta data
                    var meta_data = QuantApp.Engine.Utils.ExecuteCodeFunction(false, codes, "??", null);

                    var hasPermission = false;
                    var setPermission = false;
                    
                    if(meta_data != null)
                    {
                        foreach(dynamic func in meta_data.Result)
                        {
                            if(func != null && func.Item1 == name)
                            {
                                var pp = func.Item2;
                                if(pp != null && pp.Permissions != null)
                                    foreach(var perm in pp.Permissions)
                                    {
                                        hasPermission = true;
                                        var permAccess = QuantApp.Kernel.User.PermissionContext(perm.GroupID);
                                        setPermission = !setPermission ? (int)permAccess >= (int)perm.Access : setPermission;
                                    }
                            }
                        }
                        
                    }

                    if(hasPermission && !setPermission)
                        return Unauthorized();
                    // execute code
                    
                    var execution = QuantApp.Engine.Utils.ExecuteCodeFunction(false, codes, name, p.Length == 0 ? null : p);

                    var execution_result = execution.Result;
                    if(execution_result.Length == 0)
                    {
                        QuantApp.Kernel.User.ContextUser = new QuantApp.Kernel.UserData();
                        return Ok(execution.Compilation);
                    }
                        
                    if(name != null)
                        foreach(var pair in execution_result)
                        {
                            if(pair.Item1 == name)
                            {
                                QuantApp.Kernel.User.ContextUser = new QuantApp.Kernel.UserData();
                                if(pair.Item3 != null)
                                    return BadRequest(pair.Item3);
                                else
                                    return Ok(pair.Item2);
                            }
                        }
                    else
                        return Ok(execution_result);
                }
            }

            QuantApp.Kernel.User.ContextUser = new QuantApp.Kernel.UserData();
            return Ok(new { Result = "Empty" });
        }

        /// <summary>
        /// Execute a Query
        /// </summary>
        /// <returns>Success</returns>
        /// <param name="wid">Workflow ID</param>
        /// <param name="qid">Query ID</param>
        /// <param name="name">Query name</param>
        /// <param name="_cokey">User secrect</param>
        /// <param name="parameters">parameters in list format</param>
        /// <response code="200">Result of query in JSON format</response>
        [HttpGet("{wid}/{qid}/{name}/{**parameters}"), AllowAnonymous]
        public async Task<IActionResult> Query(string wid, string qid, string name, string _cokey, string parameters = "")
        {
            string[] p = this.Request.Query.Where(x => x.Key != "_cokey").SelectMany(x => x.Value).ToArray();

            if(this.Request.Headers.ContainsKey("_cokey"))
                _cokey = this.Request.Headers["_cokey"];
            
            string userId = this.User.QID();
            if(!string.IsNullOrEmpty(_cokey))
            {
                QuantApp.Kernel.User quser = QuantApp.Kernel.User.FindUserBySecret(_cokey);
                if(quser == null)
                    QuantApp.Kernel.User.ContextUser = new QuantApp.Kernel.UserData();
                else
                {
                    QuantApp.Kernel.User.ContextUser = quser.ToUserData();
                    userId = quser.ID;
                }
            }
            else if (userId != null)
            {
                QuantApp.Kernel.User quser = QuantApp.Kernel.User.FindUser(userId);
                if(quser == null)
                    QuantApp.Kernel.User.ContextUser = new QuantApp.Kernel.UserData();
                else
                {
                    QuantApp.Kernel.User.ContextUser = quser.ToUserData();
                    userId = quser.ID;
                }
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

                    // Check permissions from meta data
                    var meta_data = QuantApp.Engine.Utils.ExecuteCodeFunction(false, codes, "??", null);

                    var hasPermission = false;
                    var setPermission = false;
                    
                    if(meta_data != null)
                    {
                        foreach(dynamic func in meta_data.Result)
                        {
                            if(func != null && func.Item1 == name)
                            {
                                var pp = func.Item2;
                                if(pp != null && pp.Permissions != null)
                                    foreach(var perm in pp.Permissions)
                                    {
                                        hasPermission = true;
                                        var permAccess = QuantApp.Kernel.User.PermissionContext(perm.GroupID);
                                        setPermission = !setPermission ? (int)permAccess >= (int)perm.Access : setPermission;
                                    }
                            }
                        }
                        
                    }

                    if(hasPermission && !setPermission)
                        return Unauthorized();
                    // execute code


                    var execution = QuantApp.Engine.Utils.ExecuteCodeFunction(false, codes, name, p.Length == 0 ? null : p);
                    
                    var execution_result = execution.Result;
                    if(execution_result.Length == 0)
                    {
                        QuantApp.Kernel.User.ContextUser = new QuantApp.Kernel.UserData();
                        return Ok(execution.Compilation);
                    }
                        
                    if(name != null)
                        foreach(var pair in execution_result)
                        {
                            if(pair.Item1 == name)
                            {
                                QuantApp.Kernel.User.ContextUser = new QuantApp.Kernel.UserData();
                                if(pair.Item3 != null)
                                    return BadRequest(pair.Item3);
                                else
                                    return Ok(pair.Item2);
                            }
                        }
                    else
                        return Ok(execution_result);
                }
            }

            QuantApp.Kernel.User.ContextUser = new QuantApp.Kernel.UserData();
            return Ok(new { Result = "Empty" });
        }


        /// <summary>
        /// Execute a Query
        /// </summary>
        /// <returns>Success</returns>
        /// <param name="wid">Workflow ID</param>
        /// <param name="qid">Query ID</param>
        /// <param name="name">Query name</param>
        /// <param name="_cokey">User secrect</param>
        /// <param name="_p">parameters in JSON format</param>
        /// <response code="200">Result of query in JSON format</response>
        [HttpPost("{wid}/{qid}/{name}/{**parameters}"), AllowAnonymous]
        public async Task<IActionResult> Query(string wid, string qid, string name, string _cokey,[FromBody] Newtonsoft.Json.Linq.JObject _p)
        {
            string[] p = new string[] { Newtonsoft.Json.JsonConvert.SerializeObject(_p) };

            if(this.Request.Headers.ContainsKey("_cokey"))
                _cokey = this.Request.Headers["_cokey"];
            
            string userId = this.User.QID();
            if(!string.IsNullOrEmpty(_cokey))
            {
                QuantApp.Kernel.User quser = QuantApp.Kernel.User.FindUserBySecret(_cokey);
                if(quser == null)
                    QuantApp.Kernel.User.ContextUser = new QuantApp.Kernel.UserData();
                else
                {
                    QuantApp.Kernel.User.ContextUser = quser.ToUserData();
                    userId = quser.ID;
                }
            }
            else if (userId != null)
            {
                QuantApp.Kernel.User quser = QuantApp.Kernel.User.FindUser(userId);
                if(quser == null)
                    QuantApp.Kernel.User.ContextUser = new QuantApp.Kernel.UserData();
                else
                {
                    QuantApp.Kernel.User.ContextUser = quser.ToUserData();
                    userId = quser.ID;
                }
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

                    // Check permissions from meta data
                    var meta_data = QuantApp.Engine.Utils.ExecuteCodeFunction(false, codes, "??", null);

                    var hasPermission = false;
                    var setPermission = false;
                    
                    if(meta_data != null)
                    {
                        foreach(dynamic func in meta_data.Result)
                        {
                            if(func != null && func.Item1 == name)
                            {
                                var pp = func.Item2;
                                if(pp != null && pp.Permissions != null)
                                    foreach(var perm in pp.Permissions)
                                    {
                                        hasPermission = true;
                                        var permAccess = QuantApp.Kernel.User.PermissionContext(perm.GroupID);
                                        setPermission = !setPermission ? (int)permAccess >= (int)perm.Access : setPermission;
                                    }
                            }
                        }
                        
                    }

                    if(hasPermission && !setPermission)
                        return Unauthorized();
                    // execute code


                    var execution = QuantApp.Engine.Utils.ExecuteCodeFunction(false, codes, name, p.Length == 0 ? null : p);
                    
                    var execution_result = execution.Result;
                    if(execution_result.Length == 0)
                    {
                        QuantApp.Kernel.User.ContextUser = new QuantApp.Kernel.UserData();
                        return Ok(execution.Compilation);
                    }
                        
                    if(name != null)
                        foreach(var pair in execution_result)
                        {
                            if(pair.Item1 == name)
                            {
                                QuantApp.Kernel.User.ContextUser = new QuantApp.Kernel.UserData();
                                if(pair.Item3 != null)
                                    return BadRequest(pair.Item3);
                                else
                                    return Ok(pair.Item2);
                            }
                        }
                    else
                        return Ok(execution_result);
                }
            }

            QuantApp.Kernel.User.ContextUser = new QuantApp.Kernel.UserData();
            return Ok(new { Result = "Empty" });
        }

        /// <summary>
        /// Generate Open API v3 description of Query
        /// </summary>
        /// <returns>Success</returns>
        /// <param name="wid">Workflow ID</param>
        /// <param name="qid">Query ID</param>
        /// <param name="_cokey">User secrect</param>
        /// <response code="200">YAML Open API v3 spec</response>
        [HttpGet("{wid}/{qid}"), AllowAnonymous]
        public async Task<IActionResult> OpenAPI(string wid, string qid, string _cokey)
        {
            string userId = this.User.QID();

            if(this.Request.Headers.ContainsKey("_cokey"))
                _cokey = this.Request.Headers["_cokey"];

            if(!string.IsNullOrEmpty(_cokey))
            {
                QuantApp.Kernel.User quser = QuantApp.Kernel.User.FindUserBySecret(_cokey);
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

            var permission = QuantApp.Kernel.User.PermissionContext(wid);
            if(permission == QuantApp.Kernel.AccessType.Denied)
                return Ok("Access denied");
            
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

                    var permissions = new Dictionary<string, bool>();
                    var _execution = QuantApp.Engine.Utils.ExecuteCodeFunction(false, codes, "?", null);

                    if(_execution != null)
                    {
                        try
                        {
                            foreach(dynamic func in _execution.Result)
                            {
                                if(func != null)
                                {
                                    var hasPermission = false;
                                    var setPermission = false;
                                    var pp = func.Item2;

                                    if(func.Item1 != "#info" && pp != null && pp.Permissions != null)
                                        foreach(var perm in pp.Permissions)
                                        {
                                            hasPermission = true;
                                            var permAccess = QuantApp.Kernel.User.PermissionContext(perm.GroupID);
                                            setPermission = !setPermission ? (int)permAccess >= (int)perm.Access : setPermission;
                                        }

                                    permissions[func.Item1] = func.Item1 == "#info" ? true : hasPermission ? setPermission : true;
                                }
                            }
                        }
                        catch(Exception e)
                        {
                            Console.WriteLine(e);
                        }
                        
                    }

                    var execution = 
                            new { 
                                Compilation = _execution.Compilation,
                                Result = _execution.Result.Where(x => permissions.ContainsKey(x.Item1) && permissions[x.Item1]).Select(x => {
                                    dynamic item2 = x.Item2;
                                    return new {
                                        Item1 = x.Item1,
                                        Item2 = x.Item1 == "#info" ? x.Item2 : new {
                                            Name = item2.Name,
                                            Description = item2.Description,
                                            Parameters = item2.Parameters,
                                            //Permissions = item2.Permissions,
                                            Returns = item2.Returns
                                        }
                                    };
                                }).ToList()
                            };

                    

                    string yaml = Program.OpenAPI(execution, "/flow/query/" + wid + "/" + qid);

                    QuantApp.Kernel.User.ContextUser = new QuantApp.Kernel.UserData();
                    return Ok(yaml);
                }
            }

            QuantApp.Kernel.User.ContextUser = new QuantApp.Kernel.UserData();
            return BadRequest(new { Data = "Query not found" });
        }

        /// <summary>
        /// GitHub Callback when a repository is changed
        /// </summary>
        /// <returns>Success</returns>
        /// <param name="key">Coflows User Secret</param>
        /// <param name="token">GitHub token</param>
        /// <param name="payload">GitHub payload</param>
        /// <response code="200"></response>
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
                    "no.reply@coflows.com;CoFlows Builder", 
                    "GitHub build: " + data.Name + " (" + data.ID + ")", 
                    String.IsNullOrEmpty(mail_res1) ? "Contratulations! Package was successfully deployed to a cloud container..." : mail_res1);

            return Ok(res);
        }
    
    }
}