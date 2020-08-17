/*
 * The MIT License (MIT)
 * Copyright (c) Arturo Rodriguez All rights reserved.
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;

using Microsoft.FSharp.Collections;

using QuantApp.Kernel;

namespace CoFlows.Server.Utils
{
    public class AzureAD
    {
        public static string getToken()
        {
            if(!(Program.config["Server"]["OAuth"] != null && Program.config["Server"]["OAuth"]["AzureAdB2C"] != null))
                return null;
            var client_id =  Program.config["Server"]["OAuth"] != null && Program.config["Server"]["OAuth"]["AzureAdB2C"] != null && Program.config["Server"]["OAuth"]["AzureAdB2C"]["ClientId"] != null ? Program.config["Server"]["OAuth"]["AzureAdB2C"]["ClientId"].ToString() : "";
            var client_secret =  Program.config["Server"]["OAuth"] != null && Program.config["Server"]["OAuth"]["AzureAdB2C"] != null && Program.config["Server"]["OAuth"]["AzureAdB2C"]["ClientSecret"] != null ? Program.config["Server"]["OAuth"]["AzureAdB2C"]["ClientSecret"].ToString() : "";
            var domain =  Program.config["Server"]["OAuth"] != null && Program.config["Server"]["OAuth"]["AzureAdB2C"] != null && Program.config["Server"]["OAuth"]["AzureAdB2C"]["Domain"] != null ? Program.config["Server"]["OAuth"]["AzureAdB2C"]["Domain"].ToString() : "";

            string res = "";
            var result = new List<object>();
            Task.Run(async () => {   
                using(HttpClient httpClient = new HttpClient()){
                    httpClient.Timeout = Timeout.InfiniteTimeSpan;
                    
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));

                    var nvc = new List<KeyValuePair<string, string>>();
                    nvc.Add(new KeyValuePair<string, string>("scope", "https://graph.microsoft.com/.default"));
                    nvc.Add(new KeyValuePair<string, string>("grant_type", "client_credentials"));
                    
                    nvc.Add(new KeyValuePair<string, string>("client_id", client_id));
                    nvc.Add(new KeyValuePair<string, string>("client_secret", client_secret));

                    var req = new HttpRequestMessage(HttpMethod.Post, "https://login.microsoftonline.com/" + domain + "/oauth2/v2.0/token") { Content = new FormUrlEncodedContent(nvc) };
                    var data = await httpClient.SendAsync(req);
                    var dd = await data.Content.ReadAsStringAsync();
                    
                    dynamic d = JObject.Parse(dd.ToString());

                    res = d.access_token;
          
                }
            }).Wait();
            
            return res;
        }
        
        public static List<object> GraphUsers(string access_code)
        {       
            if(!(Program.config["Server"]["OAuth"] != null && Program.config["Server"]["OAuth"]["AzureAdB2C"] != null))
                return null;
             
            var defGroupId =  Program.config["Server"]["OAuth"] != null && Program.config["Server"]["OAuth"]["AzureAdB2C"] != null && Program.config["Server"]["OAuth"]["AzureAdB2C"]["DefaultGroupId"] != null ? Program.config["Server"]["OAuth"]["AzureAdB2C"]["DefaultGroupId"].ToString() : "";
                        

            string res = "";
            Task.Run(async () => {   
                using(HttpClient httpClient = new HttpClient()){                    
                    httpClient.Timeout = Timeout.InfiniteTimeSpan;
                    // string access_code = getToken();

                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access_code);

                    var req = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/users?$select=identities,surname,givenName");
                    var data = await httpClient.SendAsync(req);
                    res = await data.Content.ReadAsStringAsync();
                }
            }).Wait();

            var users = JObject.Parse(res);

            var result = new List<object>();

            foreach(var user in users["value"])
            {
                var email = "";
                foreach(var identity in user["identities"])
                    if(identity["signInType"].ToString() == "emailAddress")
                        email = identity["issuerAssignedId"].ToString();
                    
                var firstName = user["givenName"].ToString();
                var lastName = user["surname"].ToString(); 

                result.Add(new { Email = email, FirstName = firstName, LastName = lastName});

                //Sync to CoFlows users.
                if(email != "")
                {
                    var qid = "QuantAppSecure_" + email.ToLower().Replace('@', '.').Replace(':', '.');
                    var quser = QuantApp.Kernel.User.FindUser(qid);
                    
                    if(quser == null)
                    {
                        Console.WriteLine("--- CREATE NEW USER: " + qid);
                        var nuser = UserRepository.CreateUser(System.Guid.NewGuid().ToString(), "QuantAppSecure");

                        nuser.FirstName = firstName != null ? firstName : "No first name";
                        nuser.LastName = lastName != null ? lastName : "No last name";
                        nuser.Email = email.ToLower();

                        nuser.TenantName = qid;
                        nuser.Hash = QuantApp.Kernel.Adapters.SQL.Factories.SQLUserFactory.GetMd5Hash(System.Guid.NewGuid().ToString());

                        nuser.Secret = QuantApp.Engine.Code.GetMd5Hash(qid);

                        quser = QuantApp.Kernel.User.FindUser(qid);
                        QuantApp.Kernel.Group group = QuantApp.Kernel.Group.FindGroup("Public");
                        group.Add(quser, typeof(QuantApp.Kernel.User), AccessType.Invited);

                        QuantApp.Kernel.Group gp = Group.FindGroup(defGroupId);
                        if (gp != null)
                            gp.Add(quser, typeof(QuantApp.Kernel.User), AccessType.View);
                    }
                }
            }
        
            return result;
        }
        
        public static List<object> GraphGroups(string access_code)
        {
            if(!(Program.config["Server"]["OAuth"] != null && Program.config["Server"]["OAuth"]["AzureAdB2C"] != null))
                return null;
            
            var defGroupId =  Program.config["Server"]["OAuth"] != null && Program.config["Server"]["OAuth"]["AzureAdB2C"] != null && Program.config["Server"]["OAuth"]["AzureAdB2C"]["DefaultGroupId"] != null ? Program.config["Server"]["OAuth"]["AzureAdB2C"]["DefaultGroupId"].ToString() : "";
            var defGroup = QuantApp.Kernel.Group.FindGroup(defGroupId);

            string res = "";
            var result = new List<object>();
            Task.Run(async () => {   
                using(HttpClient httpClient = new HttpClient()){
                    httpClient.Timeout = Timeout.InfiniteTimeSpan;
                
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access_code);

                    var req = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/groups");
                    var data = await httpClient.SendAsync(req);
                    
                    res = await data.Content.ReadAsStringAsync();

                    var groups = JObject.Parse(res);

                    foreach(var group in groups["value"])
                    {
                        var id = group["id"].ToString();
                        var name = group["displayName"].ToString(); 

                        req = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/groups/" + id + "/members?$select=identities,surname,givenName");
                        data = await httpClient.SendAsync(req);
                        
                        res = await data.Content.ReadAsStringAsync();

                        var members = JObject.Parse(res);

                        var sub_result = new List<object>();

                        
                        // Create Group
                        var qgroup = QuantApp.Kernel.Group.FindGroup(id);
                        if(qgroup == null)
                        {
                            qgroup = QuantApp.Kernel.Group.CreateGroup(name, id);
                            qgroup.Parent = defGroup;
                        }
                        
                        foreach(var member in members["value"])
                        {
                            var email = "";
                            foreach(var identity in member["identities"])
                                if(identity["signInType"].ToString() == "emailAddress")
                                    email = identity["issuerAssignedId"].ToString();
                                
                            var firstName = member["givenName"].ToString();
                            var lastName = member["surname"].ToString(); 

                            sub_result.Add(new { Email = email, FirstName = firstName, LastName = lastName});

                            if(email != "")
                            {
                                var qid = "QuantAppSecure_" + email.ToLower().Replace('@', '.').Replace(':', '.');
                                var quser = QuantApp.Kernel.User.FindUser(qid);
                                
                                if(quser == null)
                                {
                                    var nuser = UserRepository.CreateUser(System.Guid.NewGuid().ToString(), "QuantAppSecure");

                                    nuser.FirstName = firstName != null ? firstName : "No first name";
                                    nuser.LastName = lastName != null ? lastName : "No last name";
                                    nuser.Email = email.ToLower();

                                    nuser.TenantName = qid;
                                    nuser.Hash = QuantApp.Kernel.Adapters.SQL.Factories.SQLUserFactory.GetMd5Hash(System.Guid.NewGuid().ToString());

                                    nuser.Secret = QuantApp.Engine.Code.GetMd5Hash(qid);

                                    quser = QuantApp.Kernel.User.FindUser(qid);
                                    QuantApp.Kernel.Group publicGroup = QuantApp.Kernel.Group.FindGroup("Public");
                                    publicGroup.Add(quser, typeof(QuantApp.Kernel.User), AccessType.Invited);

                                    if (defGroup != null)
                                        defGroup.Add(quser, typeof(QuantApp.Kernel.User), AccessType.View);
                                    
                                }

                                if (qgroup != null)
                                    qgroup.Add(quser, typeof(QuantApp.Kernel.User), AccessType.View);
                            }
                        }

                        List<IPermissible> users = qgroup.Master.List(QuantApp.Kernel.User.CurrentUser, typeof(QuantApp.Kernel.User), false);
                        foreach(var u in users)
                        {
                            var qu = u as QuantApp.Kernel.User;
                            var emails = sub_result.Where(x => {
                                dynamic d = x;
                                return d.Email == qu.Email;
                                });

                            var perm = qgroup.Permission(null, qu);
                            
                            if(emails.Count() == 0 && perm != AccessType.Write)
                                qgroup.Remove(qu);

                        }

                        result.Add(new { ID = id, Name = name, Members = sub_result });
                    }                
                }
            }).Wait();
            
            return result;
        }

        private static DateTime _lastSync = DateTime.MinValue;
        private static string access_code = null;
        private static DateTime _lastToken = DateTime.MinValue;
        private static DateTime _lastUser = DateTime.MinValue;
        public readonly static object permLock = new object();
        
        public static void Sync()
        {
            if(!(Program.config["Server"]["OAuth"] != null && Program.config["Server"]["OAuth"]["AzureAdB2C"] != null))
                return;
        
            lock (permLock)
            {
                var now = DateTime.Now;        
                
                if((now - _lastSync).TotalSeconds > 10)
                {
                    var t0 = now;
                    if((now - _lastToken).TotalSeconds > 1800)
                    {
                        access_code = getToken();
                        _lastToken = now;
                    }

                    var t1 = DateTime.Now;
                    if((now - _lastUser).TotalSeconds > 30)
                    {
                        GraphUsers(access_code);
                        _lastUser = now;
                    }
                    
                    var t2 = DateTime.Now;
                    GraphGroups(access_code);
                    var t3 = DateTime.Now;
                    Console.WriteLine("--------- Create Users & Groups: Total = " + (DateTime.Now - t0).TotalSeconds + " Token = "  + (t1 - t0).TotalSeconds + " Users = "  + (t2 - t1).TotalSeconds + " Groups = "  + (t3 - t2).TotalSeconds);
                    _lastSync = DateTime.Now;
                }
            }
        }
    }
}