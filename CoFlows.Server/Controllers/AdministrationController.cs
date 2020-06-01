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
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;


using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

using CoFlows.Server.Utils;

using QuantApp.Kernel;

namespace CoFlows.Server.Controllers
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
                group = QuantApp.Kernel.Group.FindGroup(groupid.Replace("_Workflow",""));

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
                group = QuantApp.Kernel.Group.FindGroup(groupid.Replace("_Workflow",""));

            if(user != null)
            {
                group.Remove(user);

                return Ok(new { Data = "ok" });
            }
            return Ok(new { Data = "error" });
        }

        public ActionResult GetPermission(string userid, string groupid)
        {
            string userId = this.User.QID();
            if (userId == null)
                return null;

            QuantApp.Kernel.User user = QuantApp.Kernel.User.FindUser(userid);
            QuantApp.Kernel.Group group = QuantApp.Kernel.Group.FindGroup(groupid);
            if(group == null)
                group = QuantApp.Kernel.Group.FindGroup(groupid.Replace("_Workflow",""));

            if(user != null)
                return Ok(new { Data = group.Permission(user) });

            return Ok(new { Data = AccessType.Denied });
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
                    group = QuantApp.Kernel.Group.FindGroup(groupid.Replace("_Workflow",""));

                if(group == null)
                    group = QuantApp.Kernel.Group.CreateGroup(groupid, groupid);


                group.Add(user, typeof(QuantApp.Kernel.User), (AccessType)accessType);

                return Ok(new { Data = "ok" });
            }

            return Ok(new { Data = "User not found..." });

        }

        public class UpdateUserData
        {
            public string UserID;
            public string FirstName;
            public string LastName;
            public string MetaData;
        }
        [HttpPost]
        public ActionResult UpdateUser([FromBody] UpdateUserData data)
        {
            try
            {
                var quser = QuantApp.Kernel.User.FindUser(data.UserID);
                if (!string.IsNullOrWhiteSpace(data.FirstName))
                    quser.FirstName = data.FirstName;
                if (!string.IsNullOrWhiteSpace(data.LastName))
                    quser.LastName = data.LastName;
                if (!string.IsNullOrWhiteSpace(data.MetaData))
                    quser.MetaData = data.MetaData;

                return Ok(new { Data = "ok" });
            }
            catch
            {
                return Ok(new { Data = "error" });
            }
        }
        
        public ActionResult SubGroups(string groupid, bool aggregated)
        {
            string userId = this.User.QID();
            if (userId == null)
                return null;

            QuantApp.Kernel.User user = QuantApp.Kernel.User.FindUser(userId);
            QuantApp.Kernel.Group role = QuantApp.Kernel.Group.FindGroup(groupid);

            if(role == null)
                return null;

            List<Group> sgroups = role.SubGroups(aggregated);
            List<object> jres = new List<object>();

            foreach (Group group in sgroups)
            {
                jres.Add(new
                {
                    ID = group.ID,
                    Name = group.Name,
                    Description = group.Description,
                    ParentID = group.Parent == null ? null : group.Parent.ID
                    // Permission = ac.ToString(),
                });
            }

            return Ok(jres);
        }

        public ActionResult UserData(string id, string groupid, bool aggregated)
        {
            string userId = this.User.QID();
            if (userId == null)
                return null;

            QuantApp.Kernel.User user = QuantApp.Kernel.User.FindUser(userId);

            QuantApp.Kernel.User quser = QuantApp.Kernel.User.FindUser(id);

            QuantApp.Kernel.Group role = QuantApp.Kernel.Group.FindGroup(groupid);

            if(role == null)
                return null;

            List<Group> sgroups = role.SubGroups(aggregated);
            

            List<object> jres = new List<object>();

            var lastLogin = UserRepository.LastUserLogin(id);

            foreach (QuantApp.Kernel.Group group in sgroups)
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

            return Ok(new {
                ID = quser.ID, 
                Email = quser.Email,
                Permission = role.Permission(null, quser).ToString(),
                MetaData = quser.MetaData,
                FirstName = quser.FirstName, 
                LastName = quser.LastName, 
                LastLogin = lastLogin,
                Groups = jres 
                });                
        }


        public IActionResult Users(string groupid)
        {
            string userId = this.User.QID();
            if (userId == null)
                return null;

            QuantApp.Kernel.User user = QuantApp.Kernel.User.FindUser(userId);

            if(user == null)
                return null;
                
            QuantApp.Kernel.Group role = QuantApp.Kernel.Group.FindGroup(groupid);

            if(role == null)
                role = QuantApp.Kernel.Group.FindGroup(groupid.Replace("_Workflow",""));

            if(role == null)
            {
                role = QuantApp.Kernel.Group.CreateGroup(groupid, groupid);
                // return null;
            }

            List<IPermissible> users = role.Master.List(QuantApp.Kernel.User.CurrentUser, typeof(QuantApp.Kernel.User), false);

            // Dictionary<string, List<string>> lastLogin = UserRepository.LastUserLogins(role);

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
                        FirstName = quser.FirstName,
                        LastName = quser.LastName,
                        Email = quser.Email,
                        Permission = ac.ToString(),
                        MetaData = quser.MetaData,
                        // LastLoginDate = !lastLogin.ContainsKey(quser.ID) ? "" : lastLogin[quser.ID][0],
                        // LastLoginIP = !lastLogin.ContainsKey(quser.ID) ? "" : lastLogin[quser.ID][1],
                    });
                }
                else
                    role.Remove(user_mem);

            }

            return Ok(jres);
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

        public class NewSubGroupClass
        {
            public string Name;
            public string Description; 
            public string ParentID;
        }
        [HttpPost]
        public ActionResult NewSubGroup([FromBody] NewSubGroupClass data)
        {
            string userId = this.User.QID();
            if (userId == null)
                return null;

            QuantApp.Kernel.User user = QuantApp.Kernel.User.FindUser(userId);
            
            Group parent = QuantApp.Kernel.Group.FindGroup(data.ParentID);

            if (parent != null)
            {
                Group group = QuantApp.Kernel.Group.CreateGroup(data.Name);
                group.Description = data.Description;
                parent.Add(group);

                return Ok(new { Data = "ok" });
            }
            return Ok(new { Data = "error" });
        }

        public class EditSubGroupClass
        {
            public string ID;
            public string Name;
            public string Description;
        }
        [HttpPost]
        public ActionResult EditSubGroup([FromBody] EditSubGroupClass data)
        {
            string userId = this.User.QID();
            if (userId == null)
                return null;

            QuantApp.Kernel.User user = QuantApp.Kernel.User.FindUser(userId);

            Group group = QuantApp.Kernel.Group.FindGroup(data.ID);
            group.Name = data.Name;
            group.Description = data.Description;
            
            return Ok(new { Data = "ok" });
            
        }

        public IActionResult Group(string groupid)
        {
            string userId = this.User.QID();
            if (userId == null)
                return null;

            QuantApp.Kernel.User user = QuantApp.Kernel.User.FindUser(userId);
            QuantApp.Kernel.Group group = QuantApp.Kernel.Group.FindGroup(groupid);

            AccessType ac = group.Permission(null, user);
            if (ac != AccessType.Denied)
                return Ok(new {
                        ID = group.ID,
                        Name = group.Name,
                        ParentID = group.Parent == null ? null : group.Parent.ID,
                        Description = group.Description
                    });

            return Ok(new { Data = "error" });
        }

        public class ChangePasswordClass
        {
            public string UserID;
            public string OldPassword;
            public string NewPassword;
        }
        [HttpPost]
        public ActionResult UpdatePassword([FromBody] ChangePasswordClass data)
        {
            try
            {
                var users = UserRepository.RetrieveUsersFromTenant(data.UserID);
                var ienum = users.GetEnumerator();
                ienum.MoveNext();
                var user = ienum.Current;
                
                var quser = QuantApp.Kernel.User.FindUser(data.UserID);

                // string userid, string old_password, string new_password
                
                if(!quser.VerifyPassword(data.OldPassword))
                    return Ok(new { Data = "Incorrect password"});

                if (!string.IsNullOrWhiteSpace(data.NewPassword))
                {
                    user.Hash = QuantApp.Kernel.Adapters.SQL.Factories.SQLUserFactory.GetMd5Hash(data.NewPassword);
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

        public class ResetPasswordClass
        {
            public string Email; //email + ";" + name
            public string From; //email + ";" + name
            public string Subject;
            public string Message;
        }

        [HttpPost, AllowAnonymous]
        public ActionResult ResetPassword([FromBody] ResetPasswordClass data)
        {
            try
            {
                string id = "QuantAppSecure_" + data.Email.ToLower().Replace('@', '.').Replace(':', '.');

                var users = UserRepository.RetrieveUsersFromTenant(id);
                var ienum = users.GetEnumerator();
                ienum.MoveNext();
                var user = ienum.Current;
                
                var quser = QuantApp.Kernel.User.FindUser(id);

                var newPassword = System.Guid.NewGuid().ToString();
                user.Hash = QuantApp.Kernel.Adapters.SQL.Factories.SQLUserFactory.GetMd5Hash(newPassword);

                RTDEngine.Send(new List<string>{ data.Email + ";" + quser.FirstName + " " + quser.LastName }, data.From, data.Subject, data.Message.Replace("$Password$", newPassword));
                return Ok(new { Result = "ok" });
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
                return Ok(new { Result = e.ToString() });
            }
        }




        ////////////////

        
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
        

        // public IActionResult UsersApp_contacts(string groupid, bool agreements)
        // {
        //     string userId = this.User.QID();
        //     if (userId == null)
        //         return null;

        //     QuantApp.Kernel.User user = QuantApp.Kernel.User.FindUser(userId);

        //     if(user == null)
        //         return null;
                
        //     QuantApp.Kernel.Group role = QuantApp.Kernel.Group.FindGroup(groupid);

        //     if(role == null)
        //         role = QuantApp.Kernel.Group.FindGroup(groupid.Replace("_Workflow",""));

        //     if(role == null)
        //     {
        //         role = QuantApp.Kernel.Group.CreateGroup(groupid, groupid);
        //         // return null;
        //     }

        //     List<IPermissible> users = role.Master.List(QuantApp.Kernel.User.CurrentUser, typeof(QuantApp.Kernel.User), false);

        //     Dictionary<string, List<string>> lastLogin = UserRepository.LastUserLogins(role);

        //     List<object> jres = new List<object>();

        //     foreach (QuantApp.Kernel.User user_mem in users)
        //     {
        //         QuantApp.Kernel.User quser = QuantApp.Kernel.User.FindUser(user_mem.ID);

        //         if (quser != null)
        //         {
        //             List<object> jres_tracks = new List<object>();

        //             var ac = role.Permission(null, user_mem);

        //             jres.Add(new
        //             {
        //                 ID = quser.ID,
        //                 first = quser.FirstName,
        //                 last = quser.LastName,
        //                 email = quser.Email,
        //                 group = ac.ToString(),
        //                 meta = quser.MetaData,
        //                 LastLoginDate = !lastLogin.ContainsKey(quser.ID) ? "" : lastLogin[quser.ID][0],
        //                 LastLoginIP = !lastLogin.ContainsKey(quser.ID) ? "" : lastLogin[quser.ID][1],
        //             });
        //         }
        //         else
        //             role.Remove(user_mem);

        //     }

        //     return Ok(new { items = jres });
        // }

        

        // [HttpPost]
        // public string EditGroupApp(string id, string name, string description, string planID, string profile, string apps, string stripeApiKey, string colordark, string parentid, string url, string dashboard, string redirect)
        // {
        //     try
        //     {
        //         string userId = this.User.QID();
        //         if (userId == null)
        //             return null;

        //         QuantApp.Kernel.User user = QuantApp.Kernel.User.FindUser(userId);
        //         QuantApp.Kernel.User publicUser = QuantApp.Kernel.User.FindUser("anonymous");

        //         Group parent = string.IsNullOrWhiteSpace(parentid) ? null : QuantApp.Kernel.Group.FindGroup(parentid);

        //         Group group = string.IsNullOrWhiteSpace(id) ? QuantApp.Kernel.Group.CreateGroup(name) : QuantApp.Kernel.Group.FindGroup(id);

        //         if (parent != null && string.IsNullOrWhiteSpace(id))
        //             group.Parent = parent;

        //         if (parent == null)
        //         {
        //             group.Add(user, typeof(QuantApp.Kernel.User), AccessType.Write);
        //             group.Add(publicUser, typeof(QuantApp.Kernel.User), AccessType.Denied);
        //         }

        //         group.Name = name;

        //         string des = description.Trim().Replace("_&l;_", "<").Replace("_&r;_", ">");
        //         if (!string.IsNullOrWhiteSpace(des) && des[des.Length - 1] == '\x0006')
        //             des = des.Substring(0, des.Length - 2);
        //         group.Description = des;

        //         GroupRepository.Set(group, "Profile", profile);


        //         GroupRepository.Set(group, "URL", url);

        //         return "ok";
        //     }
        //     catch (Exception e)
        //     {
        //         Console.WriteLine(e);
        //     }
        //     return "error";
        // }
        
        



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

        public class MessageClass
        {
            public List<string> To;
            public string From; //email + ";" + name
            public string Subject;
            public string Message;
        }

        [HttpPost, AllowAnonymous]
        public ActionResult Send([FromBody] MessageClass data)
        {
            try
            {
                RTDEngine.Send(data.To, data.From, data.Subject, data.Message);
                return Ok(new { Result = "ok" });
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
                return Ok(new { Result = e.ToString() });
            }
        }
    }
}