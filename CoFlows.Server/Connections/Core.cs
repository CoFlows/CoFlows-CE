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

using System.Text;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Text;


using System.Data;
using Newtonsoft.Json;

using QuantApp.Kernel;
using QuantApp.Engine;

using QuantApp.Kernel.Adapters.SQL;

using System.Data;

namespace CoFlows.Core
{
    public class Initialize
    {
        public static void Databases(string KernelConnectString, string StrategyConnectString, string CloudAppConnectString)
        {
            if (QuantApp.Kernel.User.CurrentUser == null)
                QuantApp.Kernel.User.CurrentUser = new QuantApp.Kernel.User("System");

            if (!QuantApp.Kernel.Database.DB.ContainsKey("Kernel"))
            {
                MSSQLDataSetAdapter KernelDataAdapter = new MSSQLDataSetAdapter();
                KernelDataAdapter.ConnectString = KernelConnectString;
            
                QuantApp.Kernel.Database.DB.Add("Kernel", KernelDataAdapter);

                QuantApp.Kernel.M.Factory = new QuantApp.Kernel.Adapters.SQL.Factories.SQLMFactory();
            }

            if (!QuantApp.Kernel.Database.DB.ContainsKey("CloudApp"))
            {
                MSSQLDataSetAdapter CloudAppDataAdapter = new MSSQLDataSetAdapter();
                CloudAppDataAdapter.ConnectString = CloudAppConnectString;
                QuantApp.Kernel.Database.DB.Add("CloudApp", CloudAppDataAdapter);

                QuantApp.Kernel.User.Factory = new QuantApp.Kernel.Adapters.SQL.Factories.SQLUserFactory();
                Group.Factory = new QuantApp.Kernel.Adapters.SQL.Factories.SQLGroupFactory();
            }
        }
    }
    public static class Extensions
    {
        public static StringContent AsJson(this object o) => new StringContent(JsonConvert.SerializeObject(o), Encoding.UTF8, "application/json");
    }
    public class LogOnResult
    {
        public UserData User { get;set; }
        public string token { get; set; }

        public string Secret { get; set; }
        public string Session { get; set; }
    }

    public class Connection
    {
        // private int timeout = 1000 * 60 * 20;
        public static int timeout = 5;
        public static Connection Client = new Connection();

        public string server = "localhost";
        private Uri QuantAppURL = new Uri("http://localhost/");
        public Uri wsQuantAppURL = new Uri("ws://localhost/live");

        public void Init(string server, bool ssl)
        {
            if(ssl)
            {
                this.server = server;
                this.QuantAppURL = new Uri("https://" + server + "/");
                this.wsQuantAppURL = new Uri("wss://" + server + "/live");
            }
            else
            {
                this.server = server;
                this.QuantAppURL = new Uri("http://" + server + "/");
                this.wsQuantAppURL = new Uri("ws://" + server + "/live");
            }
        }
        protected string _token = null;
        
        string _lastUpdate = "";
        public T GetObject<T>(string path, object arg)
        { 
            DateTime date = DateTime.Now;
            string timeKey = date.Date + "/" + date.Hour;

            string str = GetString(path, arg);

            if (str == "_AQI_SecureClient_Error")
            {
                _lastUpdate = timeKey;
                if(_username != null)
                    Login(_username, _password);
                else if(_code != null)
                    Login(_code);

                str = GetString(path, arg);
            }

            
            return JsonConvert.DeserializeObject<T>(str);
            
        }

        public T GetObject<T>(string path)
        {
            DateTime date = DateTime.Now;
            string timeKey = date.Date + "/" + date.Hour;

            string str = GetURL(path);

            // try
            {
                if (str == "_AQI_SecureClient_Error")
                {
                    _lastUpdate = timeKey;
                    if(_username != null)
                        Login(_username, _password);
                    else if(_code != null)
                        Login(_code);
                    str = GetURL(path);
                }


                return JsonConvert.DeserializeObject<T>(str);
            }
            // catch (Exception e)
            // {
            //     Console.WriteLine(str);
            //     Console.WriteLine(e);
            //     return default(T);
            // }
        }
        public class LoginStruct
        {
            public string Username { get; set; }
            //public string Password { get; set; }
            //public string Code { get; set; }
        }

        public readonly static object objLock = new object();

        private T GetValue<T>(DataRow row, string columnname)
        {
            object res = null;
            if (row.RowState == DataRowState.Detached)
                return (T)res;
            if (typeof(T) == typeof(string))
                res = "";
            else if (typeof(T) == typeof(int))
                res = 0;
            else if (typeof(T) == typeof(double))
                res = 0.0;
            else if (typeof(T) == typeof(DateTime))
                res = DateTime.MinValue;
            else if (typeof(T) == typeof(bool))
                res = false;
            object obj = row[columnname];
            if (obj is DBNull)
                return (T)res;

            if (typeof(T) == typeof(int))
                return (T)(object)Convert.ToInt32(obj);
            return (T)obj;
        }

        public DataTable GetDataTable(string path, object arg)
        {
            try
            {
                DateTime t = DateTime.Now;


                HttpWebRequest req = WebRequest.Create(QuantAppURL.AbsoluteUri + path) as HttpWebRequest;
                req.Method = "POST";
                req.ContentType = "application/json";
                var _header = new WebHeaderCollection();
                _header.Add("Authorization", "Bearer " + _token);
                req.Headers = _header;
                req.Timeout = timeout;

                string requestContent = arg == null ? "" : JsonConvert.SerializeObject(arg);
                req.ContentLength = requestContent.Length;
                req.AllowAutoRedirect = false;

                using (StreamWriter w = new StreamWriter(req.GetRequestStream(), Encoding.ASCII)) { w.Write(requestContent); }

                var dataTable = new DataTable();
                

                using (HttpWebResponse res = req.GetResponse() as HttpWebResponse)
                {

                    var sbuilder = new StringBuilder(1000000);


                    using (StreamReader rdr = new StreamReader(res.GetResponseStream()))
                    {
                        var headers = rdr.ReadLine().Split(',');
                        var type_names = rdr.ReadLine().Split(',');
                        
                        int length = headers.Length;

                        var typeslist = new List<Type>();
                        for (int i = 0; i < length; i++)
                        {
                            var tp = Type.GetType(type_names[i]);
                            typeslist.Add(tp);
                            dataTable.Columns.Add(new DataColumn(headers[i], tp));
                        }

                        var types = typeslist.ToArray();

                        while (!rdr.EndOfStream)
                        {
                            var lines = rdr.ReadLine().Split(',');

                            var nrow = dataTable.NewRow();
                            for (int i = 0; i < length; i++)
                            {
                                var line = lines[i];
                                var type = types[i];
                                var header = headers[i];
                                if (type == typeof(int) || type == typeof(Int64))
                                {
                                    if (string.IsNullOrEmpty(line))
                                        nrow[header] = int.MinValue;
                                    else
                                        nrow[header] = int.Parse(line);
                                }
                                else if (type == typeof(double))
                                {
                                    if (string.IsNullOrEmpty(line))
                                        nrow[header] = double.NaN;
                                    else
                                        nrow[header] = double.Parse(line);
                                }
                                else if (type == typeof(bool))
                                {
                                    if (string.IsNullOrEmpty(line))
                                        nrow[header] = false;
                                    else
                                        nrow[header] = bool.Parse(line);
                                }
                                else if (type == typeof(DateTime))
                                {
                                    if (string.IsNullOrEmpty(line))
                                        nrow[header] = DateTime.MinValue;
                                    else
                                        nrow[header] = DateTime.Parse(line);
                                }                                
                                else// if (types[i] == typeof(string))
                                    nrow[header] = line.Replace((char)30, ',');
                            }
                            dataTable.Rows.Add(nrow);
                        }
                    }
                    return dataTable;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return null;
        }

        public string GetString(string path, object arg)
        {
            HttpClient httpClient = new HttpClient();
            httpClient.Timeout = new TimeSpan(0,0,timeout);
            
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            
            if(_token != null && path != "account/login")
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
            
            var res = httpClient.PostAsync(QuantAppURL.AbsoluteUri + path, arg.AsJson()).Result;
            return res.Content.ReadAsStringAsync().Result;
        }

        public string GetURL(string path)
        {
            HttpClient httpClient = new HttpClient();
            httpClient.Timeout = new TimeSpan(0,0,timeout);
            
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            
            if(_token != null && path != "account/login")
            
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
            
            var res = httpClient.GetAsync(QuantAppURL.AbsoluteUri + path).Result;
            return res.Content.ReadAsStringAsync().Result;
        }

        public List<string> GetString(string path)
        {
            HttpWebRequest req = WebRequest.Create(QuantAppURL.AbsoluteUri + path) as HttpWebRequest;
            req.Method = "GET";
            if(_token != null)
            {
                var header = new WebHeaderCollection();
                header.Add("Authorization", "Bearer " + _token);
                req.Headers = header;
            }
            req.Timeout = timeout * 1000;

            string requestContent = "";
            req.ContentLength = requestContent.Length;
            req.AllowAutoRedirect = false;

            using (HttpWebResponse res = req.GetResponse() as HttpWebResponse)
            {
                List<string> str = new List<string>();

                using (BufferedStream buffer = new BufferedStream(res.GetResponseStream()))
                {
                    using (StreamReader reader = new StreamReader(buffer))
                    {
                        while (reader.Peek() >= 0)
                        {
                            str.Add(reader.ReadLine());
                        }
                    }
                }
                return str;
            }
        }


        protected string _username = null;
        protected string _password = null;
        protected string _code = null;
        protected string _session = null;

        public Boolean Login(string username, string password, string file = null)
        {
            _username = username;
            _password = password;

            
            LogOnResult logonRes = GetObject<LogOnResult>("account/login", new { Username = username, Password = password });

            if(logonRes == null || string.IsNullOrEmpty(logonRes.token))
                return false;

            DateTime date = DateTime.Now;
            string timeKey = date.Date + "/" + date.Hour;
            _lastUpdate = timeKey;

            _token = logonRes.token;
            _code = logonRes.Secret;
            _session = logonRes.Session;

            if(file != null)
                System.IO.File.WriteAllText(file, logonRes.Secret);

            QuantApp.Kernel.User.ContextUser = logonRes.User;

            return true;
        }

        public Boolean Login(string key)
        {
            _token = null;
            _session = null; 

            _code = key;
            
            LogOnResult logonRes = GetObject<LogOnResult>("account/login", new { Code = key });

            if(logonRes == null || string.IsNullOrEmpty(logonRes.token))
                return false;

            DateTime date = DateTime.Now;
            string timeKey = date.Date + "/" + date.Hour;
            _lastUpdate = timeKey;

            _token = logonRes.token;
            _session = logonRes.Session;

            QuantApp.Kernel.User.ContextUser = logonRes.User;

            return true;
        }

        public void Logoff()
        {
            _token = null;
            GetObject<LogOnResult>("account/logoff", "");
        }

        public string PublishPackage(string file)
        {
            var pkg = QuantApp.Engine.Code.ProcessPackageFile(file, true);
            return PublishPackage(pkg);
        }

        public string BuildPackage(string file)
        {
            var pkg = QuantApp.Engine.Code.ProcessPackageFile(file, true);
            return BuildPackage(pkg);
        }

        public string RemoteLog(string file)
        {
            var pkg = QuantApp.Engine.Code.ProcessPackageFile(file, true);
            return RemoteLogID(pkg.ID);
        }

        public string RemoteRestart(string file)
        {
            var pkg = QuantApp.Engine.Code.ProcessPackageFile(file, true);
            return RemoteRestart(pkg);
        }

        public string RemoteRemove(string file)
        {
            var pkg = QuantApp.Engine.Code.ProcessPackageFile(file, true);
            return RemoteRemoveID(pkg.ID);
        }

        public string PublishPackage(QuantApp.Engine.PKG pkg)
        {
            return GetString("flow/CreateWorkflow",  pkg);
        }

        public string BuildPackage(QuantApp.Engine.PKG pkg)
        {
            return GetString("flow/CompileWorkflow",  pkg);
        }

        public class CreateComputeResult
        {
            public string ID {get;set;}
            public string Log {get;set;}
        }
        public Compute CreateCompute(QuantApp.Engine.PKG pkg)
        {
            var res = GetObject<CreateComputeResult>("cluster/CreateComputeFromJSON",  pkg);
            var comp = new Compute(pkg, this, res);
            return comp;
        }

        public Compute CreateCompute(string workflowID)
        {
            var workflow = QuantApp.Kernel.M.Base(workflowID)[x => true].First() as Workflow;
            var pkg = QuantApp.Engine.Code.ProcessPackageWorkflow(workflow);
            var res = GetObject<CreateComputeResult>("cluster/CreateComputeFromJSON",  pkg);
            var comp = new Compute(pkg, this, res);
            return comp;
        }

        public string RemoteLogID(string workflowID)
        {
            var podname = workflowID;
            string res = GetURL("cluster/PodLog?id=" + podname);

            var jobj = Newtonsoft.Json.Linq.JObject.Parse(res);

            if(jobj.ContainsKey("Log"))
            {
                var log = (string)jobj["Log"];
                var log_str = new StringBuilder();
                
                foreach(var lg in log.Split(new string[] { Environment.NewLine }, StringSplitOptions.None))
                    log_str.AppendLine(lg);

                return log_str.ToString();
            }

            return (string)jobj["Result"]; 
        }

        public string RemoteRestart(QuantApp.Engine.PKG pkg)
        {
            var podname = pkg.ID;
            string res = GetURL("cluster/RestartPod?id=" + podname);

            var jobj = Newtonsoft.Json.Linq.JObject.Parse(res);

            return (string)jobj["Result"];
        }

        // public string RemoteRemove(QuantApp.Engine.PKG pkg)
        public string RemoteRemoveID(string workflowID)
        {
            var podname = workflowID;
            string res = GetURL("cluster/RemovePod?id=" + podname);

            var jobj = Newtonsoft.Json.Linq.JObject.Parse(res);

            return (string)jobj["Result"];
        }
        

        public class FunctionData
        {
            public string Name {get;set;}
            public object[] Parameters {get;set;}
        }

        public class CallData
        {
            public QuantApp.Engine.CodeData Code {get;set;}
            public FunctionData Function {get;set;}
        }

        public object Execute(string code, string code_name, string workflowID, string queryID, string funcName, params object[] parameters)
        {
            var res = GetString("flow/createquery",  new CallData(){ 
                Code = new QuantApp.Engine.CodeData(code_name, queryID, code, workflowID),
                Function = new FunctionData() { Name = funcName, Parameters = parameters }
            });

            var jobj = Newtonsoft.Json.Linq.JObject.Parse(res);

            try
            { 
                return (jobj["Compilation"], jobj["Result"][0]["Item2"]); 
            }
            catch
            {
                try
                { 
                    return jobj["Result"][0]["Item2"]; 
                }
                catch
                {
                    try{ return jobj["Compilation"]; }catch{}
                }
            }
            return null;
        }

        public object Execute(string workflowID, string queryID, string funcName, params object[] parameters)
        {
            var res = GetString("flow/createquery",  new CallData(){ 
                Code = new QuantApp.Engine.CodeData("", queryID, "", workflowID),
                Function = new FunctionData() { Name = funcName, Parameters = parameters }
            });

            var jobj = Newtonsoft.Json.Linq.JObject.Parse(res);

            try
            { 
                return (jobj["Compilation"], jobj["Result"][0]["Item2"]); 
            }
            catch
            {
                try
                { 
                    return jobj["Result"][0]["Item2"]; 
                }
                catch
                {
                    try{ return jobj["Compilation"]; }catch{}
                }
            }
            return null;
        }
    }

    public class Compute
    {
        private Connection conn;
        public string ID;
        public string _Log;
        private QuantApp.Engine.PKG pkg;

        public Compute(QuantApp.Engine.PKG pkg, Connection conn, Connection.CreateComputeResult res)
        {
            this.pkg = pkg;
            this.conn = conn;
            this.ID = res.ID;
            this._Log = res.Log;
        }

        public object Execute(string queryID, string funcName, params object[] parameters)
        {
            var pair = pkg.Queries.Where(entry => entry.ID == queryID).Select(entry => entry).First();
            var code_name = pair.Name;
            var code = pair.Content;

            return this.conn.Execute(code, code_name, this.ID, queryID, funcName, parameters);
        }

        public string Log()
        {
            return this.conn.RemoteLogID(this.ID);
        }

        public string Remove()
        {
            return this.conn.RemoteRemoveID(this.ID);
        }
    }
}
