/*
 * The MIT License (MIT)
 * Copyright (c) Arturo Rodriguez All rights reserved.
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

using CoFlows.Server.Utils;

using QuantApp.Kernel;

namespace CoFlows.Server.Controllers
{
    [Authorize, Route("[controller]/[action]")]   
    public class AdministrationController : Controller
    {        
        /// <summary>
        /// Set Permission (accessType) to a permissible id (pid) for a group (groupid)
        /// </summary>
        /// <remarks>
        /// The permissible values for the Access Types are:
        ///
        ///     Invited = -2
        ///     Denied = -1
        ///     View = 0
        ///     Read = 1
        ///     Write = 2
        ///
        /// </remarks>
        /// <param name="pid">Permissible ID</param>
        /// <param name="groupid">Group ID</param>
        /// <param name="accessType">Access Type</param>
        /// <param name="year">Expiry Year</param>
        /// <param name="month">Expiry Month</param>
        /// <param name="day">Expiry Day</param>
        /// <returns>Success</returns>
        /// <response code="200">Success</response>
        /// <response code="400">Permissible ID was not found or accessType has an incorrect value</response>
        [HttpGet]
        public ActionResult SetPermission(string pid, string groupid, int accessType, int year = 9999, int month = 12, int day = 31)
        {
            string userId = this.User.QID();
            if (userId == null)
                return null;

            QuantApp.Kernel.IPermissible permissible = QuantApp.Kernel.User.FindUser(pid);
            if(permissible == null)
                permissible = FileRepository.File(pid);

            if(permissible == null)
                return BadRequest(new { Data = "Permissible ID was not found"});

            try
            {
                var testAccesss = (AccessType)accessType;
            }
            catch
            {
                return BadRequest(new { Data = "accessType needs to be an integer between -2 and 2"});   
            }

            QuantApp.Kernel.Group group = QuantApp.Kernel.Group.FindGroup(groupid);
            if(group == null)
                group = QuantApp.Kernel.Group.FindGroup(groupid.Replace("_Workflow",""));

            if(group == null)
                group = QuantApp.Kernel.Group.CreateGroup(groupid, groupid);

            group.Add(permissible, typeof(QuantApp.Kernel.User), (AccessType)accessType, new DateTime(year, month, day));

            return Ok(new { Data = "ok" });
        }
        
        
        /// <summary>
        /// Remove a Permission a permissible id (pid) from a group (groupid)
        /// </summary>
        /// <param name="pid">Permissible ID</param>
        /// <param name="groupid">Group ID</param>
        /// <returns>Success</returns>
        /// <response code="200">Success</response>
        /// <response code="400">Permissible ID was not found</response>
        [HttpGet]
        public ActionResult RemovePermission(string pid, string groupid)
        {
            string userId = this.User.QID();
            if (userId == null)
                return null;

            QuantApp.Kernel.IPermissible permissible = QuantApp.Kernel.User.FindUser(pid);
            if(permissible == null)
                permissible = FileRepository.File(pid);

            QuantApp.Kernel.Group group = QuantApp.Kernel.Group.FindGroup(groupid);
            if(group == null)
                group = QuantApp.Kernel.Group.FindGroup(groupid.Replace("_Workflow",""));

            if(permissible != null)
            {
                group.Remove(permissible);

                return Ok(new { Data = "ok" });
            }
            return BadRequest(new { Data = "Permissible ID not found" });
        }

        /// <summary>
        /// Get Permission (accessType) of a permissible id (pid) for a group (groupid)
        /// </summary>
        /// <param name="pid">Permissible ID</param>
        /// <param name="groupid">Group ID</param>
        /// <returns>Success</returns>
        /// <response code="200">
        /// Result:
        ///
        ///     {
        ///         'Data': accessType
        ///     }
        ///
        /// Where accessType is:
        ///
        ///         Invited = -2
        ///         Denied = -1
        ///         View = 0
        ///         Read = 1
        ///         Write = 2
        ///
        /// </response>
        /// <response code="400">Permissible ID was not found or Group ID was not found</response>
        [HttpGet]
        public ActionResult GetPermission(string pid, string groupid)
        {
            string userId = this.User.QID();
            if (userId == null)
                return null;

            QuantApp.Kernel.IPermissible permissible = QuantApp.Kernel.User.FindUser(pid);
            if(permissible == null)
                permissible = FileRepository.File(pid);

            if(permissible == null)
                return BadRequest(new { Data = "Permissible ID not found" });

            QuantApp.Kernel.Group group = QuantApp.Kernel.Group.FindGroup(groupid);
            if(group == null)
                group = QuantApp.Kernel.Group.FindGroup(groupid.Replace("_Workflow",""));

            if(group == null)
                return BadRequest(new { Data = "Group ID not found" });

            if(permissible != null)
                return Ok(new { Data = group.Permission(null, permissible) });

            return Ok(new { Data = AccessType.Denied });
        }


        /// <summary>
        /// Add a non-existing user permission (email) for a group (groupid)
        /// </summary>
        /// <remarks>
        /// The permissible values for the Access Types are:
        ///
        ///     Invited = -2
        ///     Denied = -1
        ///     View = 0
        ///     Read = 1
        ///     Write = 2
        ///
        /// </remarks>
        /// <param name="groupid">Group ID</param>
        /// <param name="email">Email of the user</param>
        /// <param name="accessType">Access Type</param>
        /// <param name="year">Expiry Year</param>
        /// <param name="month">Expiry Month</param>
        /// <param name="day">Expiry Day</param>
        /// <returns>Success</returns>
        /// <response code="200">Success</response>
        /// <response code="400">Email was not found or accessType has an incorrect value</response>
        [HttpGet]
        public ActionResult AddPermission(string groupid, string email, int accessType, int year = 9999, int month = 12, int day = 31)
        {
            if(email == null)
                return BadRequest(new { Data = "User not found..." });

            try
            {
                var testAccesss = (AccessType)accessType;
            }
            catch
            {
                return BadRequest(new { Data = "accessType needs to be an integer between -2 and 2"});   
            }
                
            string userid = "QuantAppSecure_" + email.ToLower().Replace('@', '.').Replace(':', '.');

            QuantApp.Kernel.User user = QuantApp.Kernel.User.FindUser(userid);

            if(user != null)
            {
                QuantApp.Kernel.Group group = QuantApp.Kernel.Group.FindGroup(groupid);
                if(group == null)
                    group = QuantApp.Kernel.Group.FindGroup(groupid.Replace("_Workflow",""));

                if(group == null)
                    group = QuantApp.Kernel.Group.CreateGroup(groupid, groupid);


                group.Add(user, typeof(QuantApp.Kernel.User), (AccessType)accessType, new DateTime(year, month, day));

                return Ok(new { Data = "ok" });
            }

            return BadRequest(new { Data = "User not found..." });

        }

        public class UpdateUserData
        {
            public string UserID {get;set;}
            public string FirstName {get;set;}            
            public string LastName {get;set;}
            public string MetaData {get;set;}
        }
        /// <summary>
        /// Update a User's data
        /// </summary>
        /// <param name="data">
        /// Updated User Data:
        ///
        ///     {
        ///         "UserID": "User ID",
        ///         "FirstName": "User's first name",
        ///         "LastName": "User's last name",
        ///         "MetaData": "User's data stored in JSON format linked to this group"
        ///     }
        ///
        /// </param>
        /// <returns>Success</returns>
        /// <response code="200">Success</response>
        /// <response code="400">User not found</response>
        [HttpPost]
        public ActionResult UpdateUser([FromBody] UpdateUserData data)
        {
            try
            {
                var quser = QuantApp.Kernel.User.FindUser(data.UserID);

                if(quser == null)
                    return BadRequest(new { Data = "User not found" });
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
                return BadRequest(new { Data = "error" });
            }
        }
        
        /// <summary>
        /// Get sub groups of a group (groupid)
        /// </summary>
        /// <param name="groupid">Group ID of parent</param>
        /// <param name="aggregated">Aggregate all subgroups of subgroups recursively (true or false)</param>
        /// <returns>Success</returns>
        /// <response code="200">
        /// Result:
        ///
        ///     [{
        ///         "ID": "Sub group ID",
        ///         "Name": "Sub group Name",
        ///         "Description": "Sub group description",
        ///         "ParentID": "Parent Group's ID",
        ///     }, 
        ///     ...]
        ///
        /// </response>
        /// <response code="400">Group ID was not found</response>
        [HttpGet]
        public ActionResult SubGroups(string groupid, bool aggregated)
        {
            string userId = this.User.QID();
            if (userId == null)
                return null;

            QuantApp.Kernel.User user = QuantApp.Kernel.User.FindUser(userId);
            QuantApp.Kernel.Group role = QuantApp.Kernel.Group.FindGroup(groupid);

            if(role == null)
                return BadRequest(new { Data = "Group not found" });

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
                });
            }

            return Ok(jres);
        }

        // [HttpGet]
        // public ActionResult UserData(string id, string groupid, bool aggregated)
        // {
        //     string userId = this.User.QID();
        //     if (userId == null)
        //         return null;

        //     QuantApp.Kernel.User user = QuantApp.Kernel.User.FindUser(userId);

        //     QuantApp.Kernel.User quser = QuantApp.Kernel.User.FindUser(id);

        //     QuantApp.Kernel.Group role = QuantApp.Kernel.Group.FindGroup(groupid);

        //     if(role == null)
        //         return null;

        //     List<Group> sgroups = role.SubGroups(aggregated);
            

        //     List<object> jres = new List<object>();

        //     var lastLogin = UserRepository.LastUserLogin(id);

        //     foreach (QuantApp.Kernel.Group group in sgroups)
        //     {
        //         if (!group.Name.StartsWith("Personal: "))
        //         {
        //             AccessType accessType = group.Permission(null, quser);

        //             jres.Add(
        //                 new
        //                 {
        //                     ID = group.ID,
        //                     Name = group.Name,
        //                     Permission = accessType.ToString()
        //                 }
        //                 );
        //         }
        //     }

        //     return Ok(new {
        //         ID = quser.ID, 
        //         Email = quser.Email,
        //         Permission = role.Permission(null, quser).ToString(),
        //         MetaData = quser.MetaData,
        //         FirstName = quser.FirstName, 
        //         LastName = quser.LastName, 
        //         LastLogin = lastLogin,
        //         Groups = jres 
        //         });                
        // }


        /// <summary>
        /// Get users that are members of a group (groupid)
        /// </summary>
        /// <param name="groupid">Group ID of parent</param>
        /// <returns>Success</returns>
        /// <response code="200">
        /// Result:
        ///
        ///     [{
        ///         "ID": "User ID",
        ///         "FirstName": "User's first name",
        ///         "LastName": "User's last name",
        ///         "Email": "User's email",
        ///         "Permission": "User's permission to the Group (groupid)",
        ///         "Expiry": "User's expiry to the Group (groupid)",
        ///         "MetaData": "User's data stored in JSON format linked to this group",
        ///     }, 
        ///     ...]
        ///
        /// </response>
        [HttpGet]
        public IActionResult Users(string groupid)
        {
            string userId = this.User.QID();
            if (userId == null)
                return null;

            QuantApp.Kernel.User user = QuantApp.Kernel.User.FindUser(userId);
            QuantApp.Kernel.Group role = QuantApp.Kernel.Group.FindGroup(groupid);

            if(role == null)
                role = QuantApp.Kernel.Group.FindGroup(groupid.Replace("_Workflow",""));

            if(role == null)
                role = QuantApp.Kernel.Group.CreateGroup(groupid, groupid);

            List<IPermissible> users = role.Master.List(QuantApp.Kernel.User.CurrentUser, typeof(QuantApp.Kernel.User), false);

            List<object> jres = new List<object>();

            foreach (QuantApp.Kernel.User user_mem in users)
            {
                QuantApp.Kernel.User quser = QuantApp.Kernel.User.FindUser(user_mem.ID);

                if (quser != null)
                {
                    var ac = role.Permission(null, user_mem);
                    var exp = role.Expiry(null, user_mem);

                    if(quser.ID != "System")
                        jres.Add(new
                        {
                            ID = quser.ID,
                            FirstName = quser.FirstName,
                            LastName = quser.LastName,
                            Email = quser.Email,
                            Permission = ac.ToString(),
                            Expiry = new { year = exp.Year, month = exp.Month, day = exp.Day},
                            MetaData = quser.MetaData,
                        });
                }
                else
                    role.Remove(user_mem);

            }

            return Ok(jres);
        }

        /// <summary>
        /// Remove a group (groupid)
        /// </summary>
        /// <param name="groupid">Group ID</param>
        /// <returns>Success</returns>
        /// <response code="200">Success</response>
        /// <response code="400">Group ID was not found</response>
        [HttpGet]
        public ActionResult RemoveGroup(string groupid)
        {
            string userId = this.User.QID();
            if (userId == null)
                return null;

            QuantApp.Kernel.User user = QuantApp.Kernel.User.FindUser(userId);

            Group group = QuantApp.Kernel.Group.FindGroup(groupid);
            if (group != null)
            {
                group.Remove();

                return Ok(new { Data = "ok" });
            }
            return BadRequest(new { Data = "error" });
        }

        public class NewSubGroupClass
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public string ParentID { get; set; }
        }
        
        /// <summary>
        /// Create a new sub group
        /// </summary>
        /// <param name="data">
        /// New subgroup data:
        ///
        ///     {
        ///         "Name": "Subgroup's name",
        ///         "Description": "Subgroup's description",
        ///         "ParentID": "Subgroup's parent group id"
        ///     }
        ///
        /// </param>
        /// <returns>Success</returns>
        /// <response code="200">Success</response>
        /// <response code="400">Group ID not found</response>
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
            return BadRequest(new { Data = "error" });
        }

        public class EditSubGroupClass
        {
            public string ID { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
        }
        /// <summary>
        /// Create a new sub group
        /// </summary>
        /// <param name="data">
        /// Edit subgroup data:
        ///
        ///     {
        ///         "ID": "Group's id"
        ///         "Name": "Group's new name",
        ///         "Description": "Group's new description",
        ///     }
        ///
        /// </param>
        /// <returns>Success</returns>
        /// <response code="200">Success</response>
        /// <response code="400">Group ID not found</response>
        [HttpPost]
        public ActionResult EditSubGroup([FromBody] EditSubGroupClass data)
        {
            string userId = this.User.QID();
            if (userId == null)
                return null;

            QuantApp.Kernel.User user = QuantApp.Kernel.User.FindUser(userId);

            Group group = QuantApp.Kernel.Group.FindGroup(data.ID);

            if(group == null)
                return BadRequest(new { Data = "Group not found"});

            group.Name = data.Name;
            group.Description = data.Description;
            
            return Ok(new { Data = "ok" });
            
        }

        /// <summary>
        /// Get group information
        /// </summary>
        /// <param name="groupid">Group ID of parent</param>
        /// <returns>Success</returns>
        /// <response code="200">
        /// Result:
        ///
        ///     {
        ///         "ID": "Group ID",
        ///         "Name": "Group Name",
        ///         "ParentID": "Parent group's ID",
        ///         "Description": "Group's description",
        ///     }
        ///
        /// </response>
        [HttpGet]
        public IActionResult Group(string groupid)
        {
            string userId = this.User.QID();
            if (userId == null)
                return null;

            QuantApp.Kernel.User user = QuantApp.Kernel.User.FindUser(userId);
            QuantApp.Kernel.Group group = QuantApp.Kernel.Group.FindGroup(groupid);

            if(group == null)
                return BadRequest(new { Data = "Group not found"});

            AccessType ac = group.Permission(null, user);
            if (ac != AccessType.Denied)
                return Ok(new {
                        ID = group.ID,
                        Name = group.Name,
                        ParentID = group.Parent == null ? null : group.Parent.ID,
                        Description = group.Description
                    });

            return BadRequest(new { Data = "Group access denied" });
        }

        public class ChangePasswordClass
        {
            public string UserID { get; set; }
            public string OldPassword { get; set; }
            public string NewPassword { get; set; }
        }
        /// <summary>
        /// Update a user password
        /// </summary>
        /// <param name="data">
        /// Change password data:
        ///
        ///     {
        ///         "UserID": "User ID",
        ///         "OldPassword": "User's old password",
        ///         "NewPassword": "User's new password"
        ///     }
        ///
        /// </param>
        /// <returns>Success</returns>
        /// <response code="200">Success</response>
        /// <response code="400">Old password incorrect or new password is empty</response>
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

                
                if(!quser.VerifyPassword(data.OldPassword))
                    return BadRequest(new { Data = "Incorrect password"});

                if (!string.IsNullOrWhiteSpace(data.NewPassword))
                {
                    user.Hash = QuantApp.Kernel.Adapters.SQL.Factories.SQLUserFactory.GetMd5Hash(data.NewPassword);
                    return Ok(new { Data = "ok"});
                }
                else
                    return BadRequest(new { Data = "Empty new password"});
            }
            catch(Exception e)
            {
                return Ok(new { Data = e.ToString() });
            }
        }

        
        public class ResetPasswordClass
        {
            public string Email { get; set; }
            public string From { get; set; }
            public string Subject { get; set; }
            public string Message { get; set; }
        }

        /// <summary>
        /// Send a password reset email
        /// </summary>
        /// <param name="data">
        /// Change password data:
        ///
        ///     {
        ///         "Email": "User's email",
        ///         "From": "Source email (where the email was sent from as seen in the reset email)",
        ///         "Subject": "Email subject",
        ///         "Message": "Email message, this text must contain the value $Password$ which will be exchange with the new password"
        ///     }
        ///
        /// Message variable example:
        ///
        ///     Dear XXX, you new password is :
        ///         $Password$
        ///     Please reset after login.
        ///
        /// </param>
        /// <returns>Success</returns>
        /// <response code="200">Success</response>
        /// <response code="400">User not found</response>
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
                if(quser == null)
                    return BadRequest(new { Data = "User not found" });

                var newPassword = System.Guid.NewGuid().ToString();
                user.Hash = QuantApp.Kernel.Adapters.SQL.Factories.SQLUserFactory.GetMd5Hash(newPassword);

                RTDEngine.Send(new List<string>{ data.Email + ";" + quser.FirstName + " " + quser.LastName }, data.From, data.Subject, data.Message.Replace("$Password$", newPassword));
                return Ok(new { Result = "ok" });
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
                return BadRequest(new { Data = e.ToString() });
            }
        }

        /// <summary>
        /// Reset the user secret
        /// </summary>
        /// <response code="200">Success</response>
        /// <response code="400">An error occured </response>
        [HttpGet]
        public ActionResult ResetSecret()
        {
            string userId = this.User.QID();
            if (userId == null)
                return Unauthorized();

            try
            {
                var users = UserRepository.RetrieveUsersFromTenant(userId);
                var ienum = users.GetEnumerator();
                ienum.MoveNext();
                var user = ienum.Current;
                
                var quser = QuantApp.Kernel.User.FindUser(userId);

                quser.Secret = System.Guid.NewGuid().ToString();

                return Ok(quser.Secret);
            }
            catch(Exception e)
            {
                return BadRequest(e);
            }
        }

        public class MessageClass
        {
            public List<string> To { get; set; }
            public string From { get; set; }
            public string Subject { get; set; }
            public string Message { get; set; }
        }
        /// <summary>
        /// Send an email
        /// </summary>
        /// <param name="data">
        /// Message data:
        ///
        ///     {
        ///         "To": ["email1;Name1", "email2;Name2"],
        ///         "From": "Source email (where the email was sent from as seen in the reset email)",
        ///         "Subject": "Email subject",
        ///         "Message": "Email message"
        ///     }
        ///
        ///     Note: Emails must have the format: email;Name
        ///     Example: john.doe@email.com;John Doe
        ///
        /// </param>
        /// <returns>Success</returns>
        /// <response code="200">Success</response>
        /// <response code="400">User not found</response>
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
                return BadRequest(new { Result = e.ToString() });
            }
        }
    }
}