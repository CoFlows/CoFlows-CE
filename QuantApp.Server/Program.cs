/*
 * The MIT License (MIT)
 * Copyright (c) 2007-2019, Arturo Rodriguez All rights reserved.
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;


using QuantApp.Kernel;
using QuantApp.Kernel.Adapters.SQL;
using QuantApp.Engine;

using QuantApp.Server.Utils;

using Python.Runtime;
using QuantApp.Kernel.JVM;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace QuantApp.Server
{
    public class Program
    {
        public static bool IsServer = false;
        private static string workspace_name = null;
        private static string hostName = null;
        private static string ssl_cert = null;
        private static string ssl_password = null;

        private static readonly System.Threading.AutoResetEvent _closing = new System.Threading.AutoResetEvent(false);
        public static void Main(string[] args)
        {
            #if NETCOREAPP3_0
            Console.Write("NetCoreApp 3.0... ");
            #endif

            #if NET461
            Console.Write("Net Framework 461... ");
            #endif
            
            Console.Write("Python starting... ");
            PythonEngine.Initialize();

            Code.InitializeCodeTypes(new Type[]{ 
                typeof(QuantApp.Engine.WorkSpace),
                typeof(Jint.Native.Array.ArrayConstructor)
                });

            JObject config = (JObject)JToken.ReadFrom(new JsonTextReader(File.OpenText(@"mnt/quantapp_config.json")));
            workspace_name = config["Workspace"].ToString();
            hostName = config["Server"]["Host"].ToString();
            var secretKey = config["Server"]["SecretKey"].ToString();
            ssl_cert = config["Server"]["SSL"]["Cert"].ToString();
            ssl_password = config["Server"]["SSL"]["Password"].ToString();
            var sslFlag = !string.IsNullOrWhiteSpace(ssl_cert);

            var connectionString = config["Database"].ToString();

            var cloudHost = config["Cloud"]["Host"].ToString();
            var cloudKey = config["Cloud"]["SecretKey"].ToString();
            var cloudSSL = config["Cloud"]["SSL"].ToString();

            if(args != null && args.Length > 0 && args[0] == "lab")
            {
                Connection.Client.Init(hostName, sslFlag);

                if(!Connection.Client.Login(secretKey))
                    throw new Exception("CoFlows Not connected!");

                Connection.Client.Connect();
                Console.Write("server connected! ");

                QuantApp.Kernel.M.Factory = new MFactory();
                
                var pargs = new string[] {"-m", "ipykernel_launcher.py", "-f", args[1] };
                Console.Write("Starting lab... ");

                Python.Runtime.Runtime.Py_Main(pargs.Length, pargs);
                Console.WriteLine("started lab... ");

            }
            //Cloud
            else if(args != null && args.Length > 1 && args[0] == "cloud" && args[1] == "deploy")
            {
                Console.WriteLine("Cloud Host: " + cloudHost);
                Console.WriteLine("Cloud SSL: " + cloudSSL);
                Connection.Client.Init(cloudHost, cloudSSL.ToLower() == "true");

                if(!Connection.Client.Login(cloudKey))
                    throw new Exception("CoFlows Not connected!");

                Connection.Client.Connect();
                Console.Write("server connected! ");

                QuantApp.Kernel.M.Factory = new MFactory();

                Console.Write("Starting cloud deployment... ");

                Code.UpdatePackageFile(workspace_name);
                var t0 = DateTime.Now;
                Console.WriteLine("Started: " + t0);
                var res = Connection.Client.PublishPackage(workspace_name);
                var t1 = DateTime.Now;
                Console.WriteLine("Ended: " + t1 + " taking " + (t1 - t0));
                Console.Write("Result: " + res);
            }
            else if(args != null && args.Length > 1 && args[0] == "cloud" && args[1] == "build")
            {
                Console.WriteLine("Cloud Host: " + cloudHost);
                Console.WriteLine("Cloud SSL: " + cloudSSL);
                Connection.Client.Init(cloudHost, cloudSSL.ToLower() == "true");

                if(!Connection.Client.Login(cloudKey))
                    throw new Exception("CoFlows Not connected!");

                Connection.Client.Connect();
                Console.Write("server connected! ");

                QuantApp.Kernel.M.Factory = new MFactory();

                Console.Write("CoFlows Cloud build... ");

                Code.UpdatePackageFile(workspace_name);
                var t0 = DateTime.Now;
                Console.WriteLine("Started: " + t0);
                var res = Connection.Client.BuildPackage(workspace_name);
                var t1 = DateTime.Now;
                Console.WriteLine("Ended: " + t1 + " taking " + (t1 - t0));
                Console.Write("Result: " + res);
            }
            else if(args != null && args.Length > 2 && args[0] == "cloud" && args[1] == "query")
            {
                Console.WriteLine("Cloud Host: " + cloudHost);
                Console.WriteLine("Cloud SSL: " + cloudSSL);
                Connection.Client.Init(cloudHost, cloudSSL.ToLower() == "true");

                if(!Connection.Client.Login(cloudKey))
                    throw new Exception("CoFlows Not connected!");

                Connection.Client.Connect();
                Console.Write("server connected! ");

                QuantApp.Kernel.M.Factory = new MFactory();

                Console.WriteLine("CoFlows Cloud query... ");

                var queryID = args[2];
                var funcName = args.Length > 3 ? args[3] : null;
                var parameters = args.Length > 4 ? args.Skip(4).ToArray() : null;

                var pkg = Code.ProcessPackageFile(workspace_name);
                Console.WriteLine("Workspace: " + pkg.Name);

                Console.WriteLine("Query ID: " + queryID);
                Console.WriteLine("Function Name: " + funcName);
                
                if(parameters != null)
                    for(int i = 0; i < parameters.Length; i++)
                        Console.WriteLine("Parameter[" + i + "]: " + parameters[i]);

                
                var (code_name, code) = pkg.Queries.Where(entry => entry.ID == queryID).Select(entry => (entry.Name as string, entry.Content as string)).FirstOrDefault();

                var t0 = DateTime.Now;
                Console.WriteLine("Started: " + t0);
                var result = Connection.Client.Execute(code, code_name, pkg.ID, queryID, funcName, parameters);
                var t1 = DateTime.Now;
                Console.WriteLine("Ended: " + t1 + " taking " + (t1 - t0));
                
                Console.WriteLine("Result: ");
                Console.WriteLine(result);
            }
            else if(args != null && args.Length > 1 && args[0] == "cloud" && args[1] == "log")
            {
                Console.WriteLine("Cloud Host: " + cloudHost);
                Console.WriteLine("Cloud SSL: " + cloudSSL);
                Connection.Client.Init(cloudHost, cloudSSL.ToLower() == "true");

                if(!Connection.Client.Login(cloudKey))
                    throw new Exception("CoFlows Not connected!");

                Connection.Client.Connect();
                Console.Write("server connected! ");

                QuantApp.Kernel.M.Factory = new MFactory();

                Console.Write("CoFlows Cloud log... ");

                var res = Connection.Client.RemoteLog(workspace_name);
                Console.WriteLine("Result: ");
                Console.WriteLine(res);
            }
            else if(args != null && args.Length > 1 && args[0] == "cloud" && args[1] == "remove")
            {
                Console.WriteLine("Cloud Host: " + cloudHost);
                Console.WriteLine("Cloud SSL: " + cloudSSL);
                Connection.Client.Init(cloudHost, cloudSSL.ToLower() == "true");

                if(!Connection.Client.Login(cloudKey))
                    throw new Exception("CoFlows Not connected!");

                Connection.Client.Connect();
                Console.Write("server connected! ");

                QuantApp.Kernel.M.Factory = new MFactory();

                Console.Write("CoFlows Cloud log... ");

                var res = Connection.Client.RemoteRemove(workspace_name);
                Console.WriteLine("Result: ");
                Console.WriteLine(res);
            }
            else if(args != null && args.Length > 1 && args[0] == "cloud" && args[1] == "restart")
            {
                Console.WriteLine("Cloud Host: " + cloudHost);
                Console.WriteLine("Cloud SSL: " + cloudSSL);
                Connection.Client.Init(cloudHost, cloudSSL.ToLower() == "true");

                if(!Connection.Client.Login(cloudKey))
                    throw new Exception("CoFlows Not connected!");

                Connection.Client.Connect();
                Console.Write("server connected! ");

                QuantApp.Kernel.M.Factory = new MFactory();

                Console.Write("CoFlows Cloud log... ");

                var res = Connection.Client.RemoteRestart(workspace_name);
                Console.WriteLine("Result: ");
                Console.WriteLine(res);
            }
            else if(args != null && args.Length > 0 && args[0] == "server")
            {
                PythonEngine.BeginAllowThreads();

                Databases(connectionString);
                Console.WriteLine("QuantApp Server " + DateTime.Now);
                Console.WriteLine("DB Connected");

                Console.WriteLine("Local deployment");

                var pkg = Code.ProcessPackageFile(workspace_name);
                Code.ProcessPackageJSON(pkg);
                SetDefaultWorkSpaces(new string[]{ pkg.ID });


                #if NETCOREAPP3_0
                if(!sslFlag)
                    Init(new string[]{"--urls", "http://*:80"});
                else
                    Init(args);
                #endif

                #if NET461
                Init(new string[]{"--urls", "http://*:80"});
                #endif
            
            
                Task.Factory.StartNew(() => {
                    while (true)
                    {
                        // Console.WriteLine(DateTime.Now.ToString());
                        System.Threading.Thread.Sleep(1000);
                    }
                });
                Console.CancelKeyPress += new ConsoleCancelEventHandler(OnExit);
                _closing.WaitOne();
            }
            else
                Console.WriteLine("Wrong argument");

        }

        protected static void OnExit(object sender, ConsoleCancelEventArgs args)
        {
            Console.WriteLine("bye bye!");
            _closing.Set();
            Environment.Exit(0);
        }

        private static void Databases(string sqliteFile)
        {
     
            string KernelConnectString = "Data Source=" + sqliteFile;

            bool dbExists = File.Exists(sqliteFile);

            string CloudAppConnectString = KernelConnectString;

            SQLiteDataSetAdapter KernelDataAdapter = new SQLiteDataSetAdapter();
            
            KernelDataAdapter.ConnectString = KernelConnectString;
            SQLiteDataSetAdapter CloudAppDataAdapter = KernelDataAdapter;
            
            if (QuantApp.Kernel.User.CurrentUser == null)
                QuantApp.Kernel.User.CurrentUser = new QuantApp.Kernel.User("System");

            if (!QuantApp.Kernel.Database.DB.ContainsKey("Kernel"))
            {         
                QuantApp.Kernel.Database.DB.Add("Kernel", KernelDataAdapter);

                QuantApp.Kernel.M.Factory = new QuantApp.Kernel.Adapters.SQL.Factories.SQLMFactory();
            }

            if (!QuantApp.Kernel.Database.DB.ContainsKey("CloudApp"))
            {
                QuantApp.Kernel.Database.DB.Add("CloudApp", CloudAppDataAdapter);

                QuantApp.Kernel.User.Factory = new QuantApp.Kernel.Adapters.SQL.Factories.SQLUserFactory();
                Group.Factory = new QuantApp.Kernel.Adapters.SQL.Factories.SQLGroupFactory();
            }


            if(!dbExists)
            {
                Console.WriteLine("Creating table structure in: " + sqliteFile);
                var script = File.ReadAllText(@"create.sql");
                QuantApp.Kernel.Database.DB["Kernel"].ExecuteCommand(script);
            }
        }

        private static List<string> _wspServicedList = new List<string>();
        public static IEnumerable<WorkSpace> SetDefaultWorkSpaces(string[] ids)
        {
            foreach(var id in ids)
            {
                try
                {
                    //Workspace loading...

                    var wsp = QuantApp.Kernel.M.Base(id)[x => true].FirstOrDefault() as WorkSpace;
                    
                    if(wsp != null)
                    {
                        QuantApp.Engine.Utils.ActiveWorkSpaceList.Add(wsp);
                        _wspServicedList.Add(id);

                        foreach(var fid in wsp.Functions)
                        {
                            var cfid = fid.Replace("$WID$",id);
                            var f = F.Find(cfid).Value;
                            f.Start();
                        }
                    
                        var code = "import subprocess; subprocess.check_call(['jupyter', 'lab', '--NotebookApp.notebook_dir=/App/mnt', '--ip=*', '--NotebookApp.allow_remote_access=True', '--allow-root', '--no-browser', '--NotebookApp.token=\'\'', '--NotebookApp.password=\'\'', '--NotebookApp.disable_check_xsrf=True', '--NotebookApp.base_url=/lab/" + id + "'])";
                        
                        
                        var th = new System.Threading.Thread(() => {
                            using (Py.GIL())
                            {
                                Console.WriteLine("Starting Jupyter...");
                                Console.WriteLine(code);
                                PythonEngine.Exec(code);
                            }
                        });
                        th.Start();
                    }
                    else
                    {
                        Console.WriteLine("Default workspace is null: " + id);
                        Environment.Exit(-1);
                    }

                }
                catch(Exception e)
                {
                    Console.WriteLine(e);
                }

            }

            return QuantApp.Engine.Utils.ActiveWorkSpaceList;
        }


        private static readonly object logLock = new object();
        public static IEnumerable<WorkSpace> GetDefaultWorkSpaces()
        {
            return QuantApp.Engine.Utils.ActiveWorkSpaceList;
        }

        public static void AddServicedWorkSpaces(string id)
        {
            //Workspace loading...
            var wsp = QuantApp.Kernel.M.Base(id)[x => true].FirstOrDefault() as WorkSpace;
            if(wsp != null)
            {
            
                _wspServicedList.RemoveAll(x => x == id);
                foreach(var fid in wsp.Functions)
                {
                    var cfid = fid.Replace("$WID$",id);

                    var f = F.Find(cfid).Value;
                    f.Stop();
                }

                _wspServicedList.Add(id);
            }
            else
                Console.WriteLine("Add serviced workspace is null: " + id);
        }

        public static IEnumerable<string> GetServicedWorkSpaces()
        {
            return _wspServicedList;
        }

        public static void Init(string[] args)
        {
            QuantApp.Kernel.RTDEngine.Factory = new Realtime.WebSocketListner();

            if (args == null || args.Length == 0)            
                Host.CreateDefaultBuilder(args)
                    .ConfigureWebHostDefaults(webBuilder =>
                    {
                        webBuilder.ConfigureKestrel(serverOptions =>
                        {
                            serverOptions.Listen(System.Net.IPAddress.Any, 443, listenOptions =>
                            {
                                listenOptions.UseHttps(ssl_cert, ssl_password);
                            });
                        })
                        .UseStartup<Startup>();
                        
                    })
                    .Build()
                    .Run();
            else
                Host.CreateDefaultBuilder(args)
                    .ConfigureWebHostDefaults(webBuilder =>
                    {
                        webBuilder.UseStartup<Startup>();
                    })
                    .Build()
                    .Run();            
        }
    }
}
