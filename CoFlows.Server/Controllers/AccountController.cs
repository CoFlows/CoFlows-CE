/*
 * The MIT License (MIT)
 * Copyright (c) Arturo Rodriguez All rights reserved.
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;

using System.Security.Claims;

using System.Text;
using System.Net.Http;

using System.Linq;


using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

using CoFlows.Server.Models;
using CoFlows.Server.Utils;

using Newtonsoft.Json;

using QuantApp.Kernel;

namespace CoFlows.Server.Controllers
{
    public static class Extensions
    {
        public static StringContent AsJson(this object o) => new StringContent(JsonConvert.SerializeObject(o), Encoding.UTF8, "application/json");
    }

    [Authorize, Route("[controller]/[action]")]   
    public class AccountController : Controller
    {        
        private static Dictionary<string, string> _secrets = new Dictionary<string, string>();

        /// <summary>
        /// User logout
        /// </summary>
        /// <response code="200">Success</response>
        [HttpGet, HttpPost]
        public async Task<IActionResult> Logout()
        {
            string key = Request.Cookies["coflows"]; 
            if(key != null)
            {
                var outk = "";
                if(sessionKeys.ContainsKey(key))
                    sessionKeys.Remove(key, out outk);

                var _outk = "";
                if(revSessionKeys.ContainsKey(outk))
                    revSessionKeys.Remove(outk, out _outk);
            }

            Response.Cookies.Delete("coflows");  
            Response.Cookies.Append("coflows", "", new CookieOptions() { Expires = DateTime.Now.AddMonths(-24) });  

            try
            {
                string userName = this.User.QID();
                if (userName != null)
                {
                    QuantApp.Kernel.User quser = QuantApp.Kernel.User.FindUser(userName);
                    quser.SetSecure(false);
                }
            }
            finally
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }

            

            return Ok();
        }

        public static ConcurrentDictionary<string, string> sessionKeys = new ConcurrentDictionary<string, string>();
        public static ConcurrentDictionary<string, string> revSessionKeys = new ConcurrentDictionary<string, string>();

        /// <summary>
        /// User login
        /// </summary>
        /// <param name="model">
        /// Updated User Data:
        ///
        ///     {
        ///         "Username": "Email",
        ///         "Password": "Password",
        ///         "Code": "If a username/password is not provided, the user can login with a secret code",
        ///         "GroupID": "Group to which the login should be authorised to."
        ///     }
        ///
        /// </param>
        /// <returns>Success</returns>
        /// <response code="200">Success</response>
        /// <response code="400">Could not verify user</response>
        [HttpPost, AllowAnonymous]
        public async Task<ActionResult> Login([FromBody] SecureLogOnViewModel model)
        {
            string id = null;
            QuantApp.Kernel.User user = null;

            bool secretLogin = false;

            if(model.Username != null)
            {
                id = "QuantAppSecure_" + model.Username.ToLower().Replace('@', '.').Replace(':', '.');
                user = QuantApp.Kernel.User.FindUser(id);
            }
            else if(model.Code != null)
            {
                user = QuantApp.Kernel.User.FindUserBySecret(model.Code);
                if(user != null)
                {
                    id = user.ID;
                    secretLogin = true;
                }
            }
            else
                return BadRequest(new { Data = "Could not verify user" });

            var group = QuantApp.Kernel.Group.FindGroup(model.GroupID);

            var permission = user == null ? QuantApp.Kernel.AccessType.Denied : group == null ? QuantApp.Kernel.AccessType.Read : group.Permission(null, user);

            if (user != null && permission != QuantApp.Kernel.AccessType.Denied && (secretLogin ? true : user.VerifyPassword(model.Password)))
            {
                var remoteIpAddress = Request.HttpContext.Features.Get<IHttpConnectionFeature>()?.RemoteIpAddress;
                string ip = remoteIpAddress.ToString();
                if(ip.Contains(":"))
                    ip = ip.Substring(ip.LastIndexOf(":") + 1);
                
                //string ip = (Request.ServerVariables["HTTP_X_FORWARDED_FOR"] ?? Request.ServerVariables["REMOTE_ADDR"]).Split(',')[0].Trim();
                UserRepository.AddLoginStamp(id, DateTime.UtcNow, ip);

                var claims = new[]
                {
                    new Claim(ClaimTypes.Email, user.Email)
                };

                var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(Program.jwtKey));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var token = new JwtSecurityToken(
                    issuer: "coflows-ce",
                    audience: "coflows-ce",
                    claims: claims,
                    expires: DateTime.Now.AddDays(10),
                    signingCredentials: creds);

                if(String.IsNullOrEmpty(user.Secret))
                {
                    var secret_key = QuantApp.Engine.Code.GetMd5Hash(user.ID);
                    user.Secret = secret_key;
                }

                var sessionKey = System.Guid.NewGuid().ToString();
                sessionKeys.TryAdd(sessionKey, user.Secret);
                revSessionKeys.TryAdd(user.Secret, sessionKey);
                Response.Cookies.Append("coflows", sessionKey, new CookieOptions() { Expires = DateTime.Now.AddHours(24) });

                return Ok(new
                {
                    User = user.ToUserData(),
                    token = new JwtSecurityTokenHandler().WriteToken(token),
                    Secret = user.Secret,
                    Session = sessionKey
                });
            }
            return BadRequest(new { Data = "Could not verify user" });
        }

        /// <summary>
        /// User registration
        /// </summary>
        /// <param name="model">
        /// Updated User Data:
        ///
        ///     {
        ///         "FirstName": "User's first name",
        ///         "LastName": "User's last name",
        ///         "Email": "User's email",
        ///         "Password": "User's password",
        ///         "Secret": "User's secret code that is used to login without username/password",
        ///         "GroupID": "Group to which the login should be authorised to."
        ///     }
        ///
        /// </param>
        /// <returns>Success</returns>
        /// <response code="200">Success</response>
        /// <response code="400">Could not verify user</response>
        [HttpPost, AllowAnonymous]
        public async Task<ActionResult> Register([FromBody] SecureRegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                string id = "QuantAppSecure_" + model.Email.ToLower().Replace('@', '.').Replace(':', '.');
                QuantApp.Kernel.User quser = QuantApp.Kernel.User.FindUser(id);
                if (quser == null)
                {
                    var user = UserRepository.CreateUser(System.Guid.NewGuid().ToString(), "QuantAppSecure");

                    user.FirstName = model.FirstName;
                    user.LastName = model.LastName;
                    user.Email = model.Email.ToLower();

                    string profile = model.GroupID;

                    user.TenantName = id;
                    user.Hash = QuantApp.Kernel.Adapters.SQL.Factories.SQLUserFactory.GetMd5Hash(model.Password);

                    if (model.Secret != null)
                    {
                        if (_secrets.ContainsKey(model.Secret))
                            user.Secret = _secrets[model.Secret];
                    }

                    var sessionKey = System.Guid.NewGuid().ToString();
                    sessionKeys.TryAdd(sessionKey, user.Secret);
                    revSessionKeys.TryAdd(user.Secret, sessionKey);
                    Response.Cookies.Append("coflows", sessionKey, new CookieOptions() { Expires = DateTime.Now.AddHours(24) });  

                    var claims = new[]
                        {
                            new Claim(ClaimTypes.Email, user.Email)
                        };

                    var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(Program.jwtKey));
                    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                    var token = new JwtSecurityToken(
                        issuer: "coflows-ce",
                        audience: "coflows-ce",
                        claims: claims,
                        expires: DateTime.Now.AddDays(10),
                        signingCredentials: creds);



                    quser = QuantApp.Kernel.User.FindUser(id);
                    QuantApp.Kernel.Group group = QuantApp.Kernel.Group.FindGroup("Public");
                    group.Add(quser, typeof(QuantApp.Kernel.User), AccessType.Invited);

                    QuantApp.Kernel.Group gp = Group.FindGroup(profile);
                    if (gp != null)
                        gp.Add(quser, typeof(QuantApp.Kernel.User), AccessType.Invited);

                    return Ok(new
                    {
                        User = quser.ToUserData(),
                        token = new JwtSecurityTokenHandler().WriteToken(token),
                        Secret = quser.Secret,
                        Session = sessionKey
                    });
                }
                else
                    return BadRequest(new { Value = false, ID = "Email is already in use..." });
            }

            string messages = string.Join("<br\\> ", ModelState.Values
                                        .SelectMany(x => x.Errors)
                                        .Select(x => x.ErrorMessage));

            return Ok(new { Value = false, ID = messages });
        }

        /// <summary>
        /// Returns information about user that is logged in
        /// </summary>
        /// <returns>Success</returns>
        /// <response code="200">
        /// Result:
        ///
        ///     {
        ///         "User":
        ///            {
        ///                "Loggedin" = true / false,
        ///                "ID" = "Current user's ID",
        ///                "Name" = "Current user's full name",
        ///                "FirstName" = "Current user's first name",
        ///                "LastName" = "Current user's last name",
        ///                "Email" = "Current user's email",
        ///                "MetaData" = "Current user's metadata",
        ///                "Secret" = "Current user's secret",
        ///                "Session" = "Current user's session key",
        ///            }
        ///     }
        ///
        /// </response>
        /// <response code="400">Group ID was not found</response>
        [HttpGet]
        public async Task<ActionResult> WhoAmI()
        {
            var userId = User.QID();

            if (userId == null)
                return Unauthorized();

            QuantApp.Kernel.User quser = QuantApp.Kernel.User.FindUser(userId);


            bool loggedin = false;
            string uid = "";
            string username = "";
            string firstname = "";
            string lastname = "";
            string email = "";
           string metadata = "";

            string secret = "";

            if (quser != null)
            {
                List<object> groups_serialized = new List<object>();
                loggedin = true;
                uid = quser.ID;
                username = quser.FirstName + " " + quser.LastName;
                firstname = quser.FirstName;
                lastname = quser.LastName;
                email = quser.Email;
                metadata = quser.MetaData;

                secret = quser.Secret;


                if(!revSessionKeys.ContainsKey(secret))
                {
                    var session = System.Guid.NewGuid().ToString();
                    sessionKeys.TryAdd(session, secret);
                    revSessionKeys.TryAdd(secret, session);
                }

                return Ok(new
                {
                    User = new
                    {
                        Loggedin = loggedin,
                        ID = uid,
                        Name = username,
                        FirstName = firstname,
                        LastName = lastname,
                        Email = email,
                        MetaData = metadata,
                        Secret = secret,
                        Session = revSessionKeys.ContainsKey(secret) ? revSessionKeys[secret] : ""
                    }
                });
            }
            else
            {
                return BadRequest(new { Data = "User not logged in"});
            }
        }

        /// <summary>
        /// Get the current user's Permission (accessType) to a group (groupid)
        /// </summary>
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
        public ActionResult GetPermission(string groupid)
        {
            string userId = this.User.QID();
            if (userId == null)
                return null;

            QuantApp.Kernel.IPermissible permissible = QuantApp.Kernel.User.FindUser(userId);
            QuantApp.Kernel.Group group = QuantApp.Kernel.Group.FindGroup(groupid);
            if(group == null)
                group = QuantApp.Kernel.Group.FindGroup(groupid.Replace("_Workflow",""));

            if(permissible != null && group != null)
                return Ok(new { Data = group.Permission(null, permissible) });

            return Ok(new { Data = AccessType.Denied });
        }

        /// <summary>
        /// Get the current user's Expiry of access to a group (groupid)
        /// </summary>
        /// <param name="groupid">Group ID</param>
        /// <returns>Success</returns>
        /// <response code="200">
        /// Result:
        ///
        ///     {
        ///         'Data': expiry
        ///     }
        ///
        /// </response>
        /// <response code="400">Permissible ID was not found or Group ID was not found</response>
        [HttpGet]
        public ActionResult GetExpiry(string groupid)
        {
            string userId = this.User.QID();
            if (userId == null)
                return null;

            QuantApp.Kernel.IPermissible permissible = QuantApp.Kernel.User.FindUser(userId);
            QuantApp.Kernel.Group group = QuantApp.Kernel.Group.FindGroup(groupid);
            if(group == null)
                group = QuantApp.Kernel.Group.FindGroup(groupid.Replace("_Workflow",""));

            if(permissible != null && group != null)
                return Ok(new { Data = group.Expiry(null, permissible) });

            return Ok(new { Data = DateTime.MaxValue });
        }

        /// <summary>
        /// Get the current user's Permissions (accessType) for all groups where the user has access
        /// </summary>
        /// <returns>Success</returns>
        /// <response code="200">
        /// Result:
        ///
        ///     {
        ///         'Data': [ { ID: '', Name: '', Permission: -1 } ]
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
        [HttpGet]
        public ActionResult GetPermissions()
        {
            string userId = this.User.QID();
            if (userId == null)
                return null;

            var quser = QuantApp.Kernel.User.FindUser(userId);
            QuantApp.Kernel.User.ContextUser = quser.ToUserData();
            var groups = QuantApp.Kernel.Group.MasterGroups();
           
            
            return Ok(new { Data =  groups.Where(x => (int)x.PermissionContext() > (int)AccessType.Denied).Select(x => new { ID = x.ID, Name = x.Name, Permission = x.PermissionContext(), Expiry = x.ExpiryContext() }) });
        }

        /// <summary>
        /// Get the current user's Expiry for all groups where the user has access
        /// </summary>
        /// <returns>Success</returns>
        /// <response code="200">
        /// Result:
        ///
        ///     {
        ///         'Data': [ { ID: '', Name: '', Expiry: '' } ]
        ///     }
        ///
        /// </response>
        [HttpGet]
        public ActionResult GetExpiries()
        {
            string userId = this.User.QID();
            if (userId == null)
                return null;

            var quser = QuantApp.Kernel.User.FindUser(userId);
            QuantApp.Kernel.User.ContextUser = quser.ToUserData();
            var groups = QuantApp.Kernel.Group.MasterGroups();
           
            
            return Ok(new { Data =  groups.Where(x => (int)x.Access > (int)AccessType.Denied).Select(x => new { ID = x.ID, Name = x.Name, Permission = x.PermissionContext() }) });
        }

        /// <summary>
        /// Get user data. This is a json object with any type of information linked to a group (groupid)
        /// </summary>
        /// <param name="groupid">Group ID</param>
        /// <param name="type">Type of data</param>
        /// <returns>Success</returns>
        /// <response code="200">Json object</response>
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
            public string UserID { get; set; }
            public string GroupID { get; set; }
            public string Type { get; set; }
            public Newtonsoft.Json.Linq.JObject data { get; set; }
        }
        /// <summary>
        /// Save user data. This is a json object with any type of information linked to a group (groupid)
        /// </summary>
        /// <param name="data">
        /// Data:
        ///
        ///     {
        ///         "UserID": "User's ID",
        ///         "GroupID": "Group linked to the data",
        ///         "Type": "Type of data (ID)",
        ///         "data": "JSON object"
        ///     }
        ///
        /// </param>
        /// <returns>Success</returns>
        /// <response code="200">Success</response>
        /// <response code="400">Group not found</response>
        [HttpPost]
        public async Task<IActionResult> SaveUserData([FromBody] SaveUserDataClass data)
        {
            string userId = this.User.QID();
            if (userId == null)
                return Unauthorized();

            QuantApp.Kernel.User quser = QuantApp.Kernel.User.FindUser(data.UserID);

            QuantApp.Kernel.Group group = QuantApp.Kernel.Group.FindGroup(data.GroupID);
            if(group == null)
                return BadRequest(new { Data = "Group not found" });

            quser.SaveData(group, data.Type, data.ToString());

            return Ok(new { Result = "ok" });
        }

        

    }
}