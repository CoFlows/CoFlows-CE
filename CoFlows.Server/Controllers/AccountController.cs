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
using System.Net.Mail;

using System.Text;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;

using System.Linq;


using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Identity;

using CoFlows.Server.Models;
using CoFlows.Server.Utils;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

        [HttpGet, HttpPost]
        public async Task<IActionResult> Logout()
        {
            string key = Request.Cookies["coflows"]; 
            if(key != null)
            {
                var outk = "";
                if(sessionKeys.ContainsKey(key))
                    sessionKeys.Remove(key, out outk);
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
                return BadRequest("Could not verify user");

            var group = QuantApp.Kernel.Group.FindGroup(model.GroupID);

            var permission = user == null ? QuantApp.Kernel.AccessType.Denied : group == null ? QuantApp.Kernel.AccessType.Read : group.Permission(null, user);

            if (user != null && permission != QuantApp.Kernel.AccessType.Denied && (secretLogin ? true : user.VerifyPassword(model.Password)))
            {
                var remoteIpAddress = Request.HttpContext.Features.Get<IHttpConnectionFeature>()?.RemoteIpAddress;
                string ip = remoteIpAddress.ToString();
                //string ip = (Request.ServerVariables["HTTP_X_FORWARDED_FOR"] ?? Request.ServerVariables["REMOTE_ADDR"]).Split(',')[0].Trim();
                UserRepository.AddLoginStamp(id, DateTime.UtcNow, ip);

                var claims = new[]
                {
                    new Claim(ClaimTypes.Name, id)
                };

                var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes("___Secret-QuantApp-Capital!1234"));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var token = new JwtSecurityToken(
                    issuer: "quant.app",
                    audience: "quant.app",
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
                Response.Cookies.Append("coflows", sessionKey, new CookieOptions() { Expires = DateTime.Now.AddHours(24) });

                return Ok(new
                {
                    User = user.ToUserData(),
                    token = new JwtSecurityTokenHandler().WriteToken(token),
                    Secret = user.Secret,
                    Session = sessionKey
                });
            }
            return BadRequest("Could not verify user");
        }

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

                    if (model.EncodedSecret != null)
                    {
                        if (_secrets.ContainsKey(model.EncodedSecret))
                            user.Secret = _secrets[model.EncodedSecret];
                    }

                    var sessionKey = System.Guid.NewGuid().ToString();
                    sessionKeys.TryAdd(sessionKey, user.Secret);
                    Response.Cookies.Append("coflows", sessionKey, new CookieOptions() { Expires = DateTime.Now.AddHours(24) });  

                    var claims = new[]
                        {
                            new Claim(ClaimTypes.Name, id)
                        };

                    var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes("___Secret-QuantApp-Capital!1234"));
                    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                    var token = new JwtSecurityToken(
                        issuer: "quant.app",
                        audience: "quant.app",
                        claims: claims,
                        expires: DateTime.Now.AddDays(10),
                        signingCredentials: creds);



                    quser = QuantApp.Kernel.User.FindUser(id);
                    QuantApp.Kernel.Group group = QuantApp.Kernel.Group.FindGroup("Public");
                    group.Add(quser, typeof(QuantApp.Kernel.User), AccessType.Invited);

                    // QuantApp.Kernel.Group gp = GroupRepository.FindByProfile(profile);
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
                    return Ok(new { Value = false, ID = "Email is already in use..." });
            }

            string messages = string.Join("<br\\> ", ModelState.Values
                                        .SelectMany(x => x.Errors)
                                        .Select(x => x.ErrorMessage));

            return Ok(new { Value = false, ID = messages });
        }

        [HttpGet, AllowAnonymous]
        public async Task<ActionResult> WhoAmI()
        {
            var userId = User.QID();

            if (userId == null)
                userId = "anonymous";

            QuantApp.Kernel.User quser = QuantApp.Kernel.User.FindUser(userId);


            bool loggedin = false;
            string uid = "";
            string username = "";
            string firstname = "";
            string lastname = "";
            string email = "";
            // string groupID = "";
            // string groupName = "";
            // string groupDescription = "";
            // string groupPermission = "";
            // string masterGroupID = "";
            // string masterGroupName = "";
            // string masterGroupDescription = "";
            // string masterGroupPermission = "";
            string metadata = "";

            string secret = "";

            if (quser != null && quser.ID != "anonymous")            
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

                // Response.Cookies.Append("coflows", quser.Secret, new CookieOptions() { Expires = DateTime.Now.AddHours(24) });  

                // var appCookie = Request.Cookies["QuantAppProfile"];

                // string profile = appCookie != null ? appCookie : null;

                // QuantApp.Kernel.Group group = string.IsNullOrWhiteSpace(profile) ? null : GroupRepository.FindByProfile(profile);
                // if (group == null)
                // {
                //     var location = new Uri($"{Request.Scheme}://{Request.Host}{Request.Path}{Request.QueryString}");
                //     group = GroupRepository.FindByURL(location.AbsoluteUri);
                // }

                // groupID = (group != null ? group.ID : "Public");

                // var qgroup = QuantApp.Kernel.Group.FindGroup(groupID);

                // groupName = qgroup.Name;
                // groupPermission = qgroup.Permission(null, quser).ToString();

                // masterGroupID = qgroup.Master.ID;
                // masterGroupName = qgroup.Master.Name;
                // masterGroupPermission = qgroup.Master.Permission(null, quser).ToString();

                // var groups = quser.MasterGroups();
                // if (groups != null)
                //     foreach (var s_group in groups)
                //     {
                //         groups_serialized.Add(new
                //         {
                //             ID = s_group.ID,
                //             Name = (s_group.Name.StartsWith("Personal:") ? "Personal" : s_group.Name),
                //             Permission = s_group.Permission(null, quser).ToString()
                //         });
                //     }

                // List<string> lastLogin = UserRepository.LastUserHistory(quser.ID);

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
                        // Administrator = QuantApp.Kernel.Group.FindGroup("Administrator").Permission(null, quser).ToString(),
                        MetaData = metadata,
                        // LastLoginDate = lastLogin == null ? null : lastLogin[0],
                        // LastLoginIP = lastLogin == null ? null : lastLogin[1],
                        Secret = secret
                    },
                    // Group = new
                    // {
                    //     ID = groupID,
                    //     Name = groupName,
                    //     Description = groupDescription,
                    //     Permission = groupPermission
                    // },
                    // MasterGroup = new
                    // {
                    //     ID = masterGroupID,
                    //     Name = masterGroupName,
                    //     Description = masterGroupDescription,
                    //     Permission = masterGroupPermission
                    // },
                    // Groups = groups_serialized,
                });
            }
            else
            {
                return Ok();
            }
        }
    }
}