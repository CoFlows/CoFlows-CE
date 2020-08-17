/*
 * The MIT License (MIT)
 * Copyright (c) Arturo Rodriguez All rights reserved.
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
 
using System;
using System.Threading.Tasks;

using System.Security.Claims;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;

using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

using CoFlows.Server.Utils;

using Newtonsoft.Json.Linq;

using QuantApp.Kernel;

namespace CoFlows.Server.Controllers
{
    [Authorize, Route("[controller]/[action]")]   
    public class OAuthController : Controller
    {
        /// <summary>
        /// Returns Oauth redirect settings to webapp for authentication
        /// </summary>
        /// <returns>Success</returns>
        /// <response code="200">
        /// Result:
        ///
        ///     {
        ///         "AzureAD": "Azure AD link",
        ///         "GitHub": "GitHub link",
        ///     }
        ///
        /// </response>
        [HttpGet, AllowAnonymous]
        public async Task<ActionResult> Data()
        {
            var azure_link = Program.config["Server"]["OAuth"] != null && Program.config["Server"]["OAuth"]["AzureAdB2C"] != null && Program.config["Server"]["OAuth"]["AzureAdB2C"]["SignInLink"] != null ? Program.config["Server"]["OAuth"]["AzureAdB2C"]["SignInLink"].ToString() : null;
            var github_link = Program.config["Server"]["OAuth"] != null && Program.config["Server"]["OAuth"]["GitHub"] != null && Program.config["Server"]["OAuth"]["GitHub"]["SignInLink"] != null ? Program.config["Server"]["OAuth"]["GitHub"]["SignInLink"].ToString() : null;
            
            return Ok(new {
                AzureAD = azure_link,
                GitHub = github_link,
            });
        }

        /// <summary>
        /// Redirect to webapp with access code
        /// </summary>
        /// <returns>Success</returns>
        /// <response code="301"></response>
        [HttpGet, HttpGet("{groupid}"), AllowAnonymous]
        public async void GitHub(string groupid, string code)
        {
            if(Program.config["Server"]["OAuth"] == null || Program.config["Server"]["OAuth"]["GitHub"] == null)
                return;

            string access_code = "";
            using(HttpClient httpClient = new HttpClient()){
                httpClient.Timeout = Timeout.InfiniteTimeSpan;
                
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var res = httpClient.PostAsync(
                    "https://github.com/login/oauth/access_token", 
                    new { 
                        client_id = Program.config["Server"]["OAuth"]["GitHub"]["ClientId"].ToString(),
                        client_secret = Program.config["Server"]["OAuth"]["GitHub"]["ClientSecret"].ToString(),
                        code = code
                    }.AsJson()).Result;

                var data = res.Content.ReadAsStringAsync().Result;

                dynamic d = JObject.Parse(data);
                access_code = d.access_token;
            }

            string email = "";
            string name = "";


            //Name & Email
            try
            {
                using(HttpClient httpClient = new HttpClient()){
                    httpClient.Timeout = Timeout.InfiniteTimeSpan;
                    
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("token", access_code);
                    httpClient.DefaultRequestHeaders.Add("User-Agent", "CoFlows");
                    
                    var res = httpClient.GetAsync("https://api.github.com/user").Result;

                    var data = res.Content.ReadAsStringAsync().Result;

                    dynamic d = JObject.Parse(data);
                    email = d.Email;
                    name = d.Name;
                }
            }
            catch { }

            if(string.IsNullOrEmpty(email))
            {
                //If Email fails above...
                using(HttpClient httpClient = new HttpClient()){
                    httpClient.Timeout = Timeout.InfiniteTimeSpan;
                    
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("token", access_code);
                    httpClient.DefaultRequestHeaders.Add("User-Agent", "CoFlows");
                    
                    var res = httpClient.GetAsync("https://api.github.com/user/emails").Result;

                    var data = res.Content.ReadAsStringAsync().Result;

                    var d = JArray.Parse(data);
                    email = d[0]["email"].ToString();
                }
            }

            string id = "QuantAppSecure_" + email.ToLower().Replace('@', '.').Replace(':', '.');

            var quser = QuantApp.Kernel.User.FindUser(id);
            if(quser == null)
            {
                var user = UserRepository.CreateUser(System.Guid.NewGuid().ToString(), "QuantAppSecure");

                user.FirstName = "";
                user.LastName = "";
                user.Email = email.ToLower();

                user.TenantName = id;
                
                quser = QuantApp.Kernel.User.FindUser(id);
                QuantApp.Kernel.Group group = QuantApp.Kernel.Group.FindGroup("Public");
                group.Add(quser, typeof(QuantApp.Kernel.User), AccessType.Invited);

                QuantApp.Kernel.Group gp = QuantApp.Kernel.Group.FindGroup(groupid);
                if (gp != null)
                    gp.Add(quser, typeof(QuantApp.Kernel.User), AccessType.Invited);
            }

            
            if(String.IsNullOrEmpty(quser.Secret))
            {
                var secret_key = QuantApp.Engine.Code.GetMd5Hash(quser.ID);
                quser.Secret = secret_key;
            }

            var sessionKey = System.Guid.NewGuid().ToString();
            AccountController.sessionKeys.TryAdd(sessionKey, quser.Secret);
            Response.Cookies.Append("coflows", sessionKey, new CookieOptions() { Expires = DateTime.Now.AddHours(24) });

            var claims = new[]
                {
                    new Claim(ClaimTypes.Email, quser.Email)
                };

            var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(Program.jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: "coflows-ce",
                audience: "coflows-ce",
                claims: claims,
                expires: DateTime.Now.AddDays(10),
                signingCredentials: creds);

            
            Response.Redirect("/authentication/token/" + new JwtSecurityTokenHandler().WriteToken(token), true);
        }
    }
}