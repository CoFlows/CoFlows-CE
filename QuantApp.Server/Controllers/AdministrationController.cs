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
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;


using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

using QuantApp.Server.Utils;

using QuantApp.Kernel;

namespace QuantApp.Server.Controllers
{
    [Authorize, Route("[controller]/[action]")]   
    public class AdministrationController : Controller
    {        
        public ActionResult SetPermission(string userid, string groupid, int accessType)
        {
            string userId = this.User.QID();
            if (userId == null)
                return null;

            QuantApp.Kernel.User user = QuantApp.Kernel.User.FindUser(userid);
            QuantApp.Kernel.Group group = QuantApp.Kernel.Group.FindGroup(groupid);
            if(group == null)
                group = QuantApp.Kernel.Group.FindGroup(groupid.Replace("_WorkSpace",""));

            if(group == null)
                group = QuantApp.Kernel.Group.CreateGroup(groupid, groupid);

            group.Add(user, typeof(QuantApp.Kernel.User), (AccessType)accessType);

            return Ok(new { Data = "ok" });
        }
        public ActionResult RemovePermission(string userid, string groupid)
        {
            string userId = this.User.QID();
            if (userId == null)
                return null;

            QuantApp.Kernel.User user = QuantApp.Kernel.User.FindUser(userid);
            QuantApp.Kernel.Group group = QuantApp.Kernel.Group.FindGroup(groupid);
            if(group == null)
                group = QuantApp.Kernel.Group.FindGroup(groupid.Replace("_WorkSpace",""));


            group.Remove(user);

            return Ok(new { Data = "ok" });
        }

        public ActionResult AddPermission(string groupid, string email, int accessType)
        {
            if(email == null)
                return Ok(new { Data = "User not found..." });
                
            string userid = "QuantAppSecure_" + email.ToLower().Replace('@', '.').Replace(':', '.');

            QuantApp.Kernel.User user = QuantApp.Kernel.User.FindUser(userid);

            if(user != null)
            {
                QuantApp.Kernel.Group group = QuantApp.Kernel.Group.FindGroup(groupid);
                if(group == null)
                    group = QuantApp.Kernel.Group.FindGroup(groupid.Replace("_WorkSpace",""));

                if(group == null)
                    group = QuantApp.Kernel.Group.CreateGroup(groupid, groupid);


                group.Add(user, typeof(QuantApp.Kernel.User), (AccessType)accessType);

                return Ok(new { Data = "ok" });
            }

            return Ok(new { Data = "User not found..." });

        }
        
        public ActionResult SubGroupsApp(string groupid, bool aggregated)
        {
            string userId = this.User.QID();
            if (userId == null)
                return null;

            QuantApp.Kernel.User user = QuantApp.Kernel.User.FindUser(userId);
            QuantApp.Kernel.Group role = QuantApp.Kernel.Group.FindGroup(groupid);

            List<Group> sgroups = role.SubGroups(aggregated);

            // Console.WriteLine("---+++++++++- " + role + " " + sgroups + " " + sgroups.Count);

            List<object> jres = new List<object>();

            foreach (Group group in sgroups)
            {
                // AccessType ac = group.Permission(null, user);
                // Console.WriteLine("---- " + ac + " " + user + " " + group);
                // if (ac != AccessType.Denied)
                {
                    jres.Add(new
                    {
                        ID = group.ID,
                        Name = group.Name,
                        Description = group.Description,
                        // Permission = ac.ToString(),
                    });
                }
                

            }

            return Ok(jres);
        }

        
        public ActionResult UsersApp()
        {
            string userId = this.User.QID();
            if (userId == null)
                return null;

            QuantApp.Kernel.User user = QuantApp.Kernel.User.FindUser(userId);

            List<object> jres = new List<object>();

            foreach (Utils.User usr in UserRepository.RetrieveUsers())
            {
                string id = usr.TenantName;
                QuantApp.Kernel.User quser = QuantApp.Kernel.User.FindUser(id);
                if (quser != null)
                    jres.Add(new { ID = quser.ID, FirstName = quser.FirstName, LastName = quser.LastName, Email = quser.Email });
            }

            return Ok(jres);
        }
        
        public ActionResult UserApp(string id)
        {
            string userId = this.User.QID();
            if (userId == null)
                return null;

            QuantApp.Kernel.User user = QuantApp.Kernel.User.FindUser(userId);

            QuantApp.Kernel.User quser = QuantApp.Kernel.User.FindUser(id);

            List<object> jres = new List<object>();

            foreach (QuantApp.Kernel.Group group in QuantApp.Kernel.Group.MasterGroups())
            {
                if (!group.Name.StartsWith("Personal: "))
                {
                    AccessType accessType = group.Permission(null, quser);

                    jres.Add(
                        new
                        {
                            ID = group.ID,
                            Name = group.Name,
                            Permission = accessType.ToString()
                        }
                        );
                }
            }


            return Ok(new { FirstName = quser.FirstName, LastName = quser.LastName, Groups = jres });                
        }
        
        public IActionResult UsersApp_contacts(string groupid, bool agreements)
        {
            string userId = this.User.QID();
            if (userId == null)
                return null;

            QuantApp.Kernel.User user = QuantApp.Kernel.User.FindUser(userId);

            if(user == null)
                return null;
                
            QuantApp.Kernel.Group role = QuantApp.Kernel.Group.FindGroup(groupid);

            if(role == null)
                role = QuantApp.Kernel.Group.FindGroup(groupid.Replace("_WorkSpace",""));

            if(role == null)
            {
                role = QuantApp.Kernel.Group.CreateGroup(groupid, groupid);
                // return null;
            }

            List<IPermissible> users = role.Master.List(QuantApp.Kernel.User.CurrentUser, typeof(QuantApp.Kernel.User), false);

            Dictionary<string, List<string>> lastLogin = UserRepository.LastUserLogins(role);

            List<object> jres = new List<object>();

            foreach (QuantApp.Kernel.User user_mem in users)
            {
                QuantApp.Kernel.User quser = QuantApp.Kernel.User.FindUser(user_mem.ID);

                if (quser != null)
                {
                    List<object> jres_tracks = new List<object>();

                    var ac = role.Permission(null, user_mem);

                    jres.Add(new
                    {
                        ID = quser.ID,
                        first = quser.FirstName,
                        last = quser.LastName,
                        email = quser.Email,
                        group = ac.ToString(),
                        meta = quser.MetaData,
                        LastLoginDate = !lastLogin.ContainsKey(quser.ID) ? "" : lastLogin[quser.ID][0],
                        LastLoginIP = !lastLogin.ContainsKey(quser.ID) ? "" : lastLogin[quser.ID][1],
                    });
                }
                else
                    role.Remove(user_mem);

            }

            return Ok(new { items = jres });
        }

        public IActionResult GroupDataApp(string groupid)
        {
            string userId = this.User.QID();
            if (userId == null)
                return null;

            QuantApp.Kernel.User user = QuantApp.Kernel.User.FindUser(userId);
            QuantApp.Kernel.Group group = QuantApp.Kernel.Group.FindGroup(groupid);

            string profile = group.GetProperty("Profile");

            string url = group.Master.GetProperty("URL");
            List<object> jres_apps = new List<object>();

            object jres = null;

            AccessType ac = group.Permission(null, user);
            if (ac != AccessType.Denied)
                jres = new
                {
                    ID = group.ID,
                    Name = group.Name,
                    Master = group == group.Master,
                    Description = group.Description,
                    Profile = profile,
                    URL = url
                };

            return Ok(jres);
        }

        [HttpPost]
        public string EditGroupApp(string id, string name, string description, string planID, string profile, string apps, string stripeApiKey, string colordark, string parentid, string url, string dashboard, string redirect)
        {
            try
            {
                string userId = this.User.QID();
                if (userId == null)
                    return null;

                QuantApp.Kernel.User user = QuantApp.Kernel.User.FindUser(userId);
                QuantApp.Kernel.User publicUser = QuantApp.Kernel.User.FindUser("anonymous");

                Group parent = string.IsNullOrWhiteSpace(parentid) ? null : QuantApp.Kernel.Group.FindGroup(parentid);

                Group group = string.IsNullOrWhiteSpace(id) ? QuantApp.Kernel.Group.CreateGroup(name) : QuantApp.Kernel.Group.FindGroup(id);

                if (parent != null && string.IsNullOrWhiteSpace(id))
                    group.Parent = parent;

                if (parent == null)
                {
                    group.Add(user, typeof(QuantApp.Kernel.User), AccessType.Write);
                    group.Add(publicUser, typeof(QuantApp.Kernel.User), AccessType.Denied);
                }

                group.Name = name;

                string des = description.Trim().Replace("_&l;_", "<").Replace("_&r;_", ">");
                if (!string.IsNullOrWhiteSpace(des) && des[des.Length - 1] == '\x0006')
                    des = des.Substring(0, des.Length - 2);
                group.Description = des;

                GroupRepository.Set(group, "Profile", profile);


                GroupRepository.Set(group, "URL", url);

                return "ok";
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return "error";
        }

        public ActionResult RemoveGroup(string id)
        {
            string userId = this.User.QID();
            if (userId == null)
                return null;

            QuantApp.Kernel.User user = QuantApp.Kernel.User.FindUser(userId);

            Group group = QuantApp.Kernel.Group.FindGroup(id);
            if (group != null)
            {
                group.Remove();

                return Ok(new { Data = "ok" });
            }
            return Ok(new { Data = "error" });
        }

        public ActionResult NewSubGroup(string name, string parendid)
        {
            string userId = this.User.QID();
            if (userId == null)
                return null;

            QuantApp.Kernel.User user = QuantApp.Kernel.User.FindUser(userId);

            Group parent = QuantApp.Kernel.Group.FindGroup(parendid);

            if (parent != null)
            {
                Group group = QuantApp.Kernel.Group.CreateGroup(name);
                parent.Add(group);

                return Ok(new { Data = "ok" });
            }
            return Ok(new { Data = "error" });
        }

        public class UpdateUserData
        {
            public string UserID;
            public string First;
            public string Last;
            public string MetaData;
        }
        [HttpPost]
        public ActionResult UpdateUser_App([FromBody] UpdateUserData data)
        {
            try
            {
                var quser = QuantApp.Kernel.User.FindUser(data.UserID);
                if (!string.IsNullOrWhiteSpace(data.First))
                    quser.FirstName = data.First;
                if (!string.IsNullOrWhiteSpace(data.Last))
                    quser.LastName = data.Last;
                if (!string.IsNullOrWhiteSpace(data.MetaData))
                    quser.MetaData = data.MetaData;

                return Ok(new { Data = "ok" });
            }
            catch
            {
                return Ok(new { Data = "error" });
            }
        }

        
        public ActionResult UpdatePassword_App(string userid, string old_password, string new_password)
        {
            try
            {
                var users = UserRepository.RetrieveUsersFromTenant(userid);
                var ienum = users.GetEnumerator();
                ienum.MoveNext();
                var user = ienum.Current;
                
                var quser = QuantApp.Kernel.User.FindUser(userid);
                
                if(!quser.VerifyPassword(old_password))
                    return Ok(new { Data = "Incorrect password"});

                if (!string.IsNullOrWhiteSpace(new_password))
                {
                    user.Hash = QuantApp.Kernel.Adapters.SQL.Factories.SQLUserFactory.GetMd5Hash(new_password);
                    return Ok(new { Data = "ok"});
                }
                else
                    return Ok(new { Data = "Empty new password"});
            }
            catch(Exception e)
            {
                return Ok(new { Data = e.ToString() });
            }
        }



        [HttpPost, AllowAnonymous]
        public string SendMessage(string id, string name, string email, string subject, string message)
        {
            try
            {
                RTDEngine.Send(new List<string>(){"arturo@quant.app;Arturo Rodriguez"}, email + ";" + name, subject, message);
                return "ok";
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
                return "error";
            }
        }
    }
}