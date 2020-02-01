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

//Azure Dependencies
using Microsoft.Azure.Management.ContainerInstance.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;


namespace QuantApp.Server
{
    public class Program
    {
        public static bool IsServer = false;
        private static string workspace_name = null;
        private static string hostName = null;
        private static string ssl_cert = null;
        private static string ssl_password = null;
        private static bool useJupyter = false;

        private static readonly System.Threading.AutoResetEvent _closing = new System.Threading.AutoResetEvent(false);
        public static void Main(string[] args)
        {
            #if NETCOREAPP3_0
            Console.Write("CoFlows CE - NetCoreApp 3.0... ");
            #endif

            #if NET461
            Console.Write("CoFlows CE - Net Framework 461... ");
            #endif

            Console.Write("Python starting... ");
            PythonEngine.Initialize();

            Code.InitializeCodeTypes(new Type[]{ 
                typeof(QuantApp.Engine.WorkSpace),
                typeof(Jint.Native.Array.ArrayConstructor)
                });

            var config_env = Environment.GetEnvironmentVariable("coflows_config");
            var config_file = Environment.GetEnvironmentVariable("config_file");

            if(string.IsNullOrEmpty(config_file))
                config_file = "quantapp_config.json";

            JObject config = string.IsNullOrEmpty(config_env) ? (JObject)JToken.ReadFrom(new JsonTextReader(File.OpenText(@"mnt/" + config_file))) : (JObject)JToken.Parse(config_env);
            workspace_name = config["Workspace"].ToString();
            hostName = config["Server"]["Host"].ToString();
            var secretKey = config["Server"]["SecretKey"].ToString();
            ssl_cert = config["Server"]["SSL"]["Cert"].ToString();
            ssl_password = config["Server"]["SSL"]["Password"].ToString();
            var sslFlag = !string.IsNullOrWhiteSpace(ssl_cert);

            useJupyter = config["Jupyter"].ToString().ToLower() == "true";

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

                // var pkg = Code.ProcessPackageFile(Code.UpdatePackageFile(workspace_name));
                // Code.ProcessPackageJSON(pkg);
                // SetDefaultWorkSpaces(new string[]{ pkg.ID });
                if(string.IsNullOrEmpty(config_env))
                {
                    var pkg = Code.ProcessPackageFile(workspace_name);
                    Code.ProcessPackageJSON(pkg);
                    SetDefaultWorkSpaces(new string[]{ pkg.ID });
                    Console.WriteLine(pkg.Name + " started");
                }
                else
                    Console.WriteLine("Empty server...");


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
            //Local
            else if(args != null && args.Length > 1 && args[0] == "local" && args[1] == "build")
            {
                PythonEngine.BeginAllowThreads();

                Databases(connectionString);
                Console.WriteLine("DB Connected");

                Console.WriteLine("Local build");

                var pkg = Code.ProcessPackageFile(Code.UpdatePackageFile(workspace_name));
                var res = Code.BuildRegisterPackage(pkg);
                if(string.IsNullOrEmpty(res))
                    Console.WriteLine("Success!!!");
                else
                    Console.WriteLine(res);
            }
            else if(args != null && args.Length > 2 && args[0] == "local" && args[1] == "query")
            {
                PythonEngine.BeginAllowThreads();

                Databases(connectionString);
                Console.WriteLine("Local Query " + DateTime.Now);
                Console.WriteLine("DB Connected");

                Console.WriteLine("CoFlows Local query... ");

                var queryID = args[2];
                var funcName = args.Length > 3 ? args[3] : null;
                var parameters = args.Length > 4 ? args.Skip(4).ToArray() : null;

                Console.WriteLine("QueryID: " + queryID);
                Console.WriteLine("FuncName: " + funcName);
                Console.WriteLine("Parameters: " + parameters);


                var pkg = Code.ProcessPackageFile(Code.UpdatePackageFile(workspace_name));
                Code.ProcessPackageJSON(pkg);
                

                if(parameters != null)
                    for(int i = 0; i < parameters.Length; i++)
                        Console.WriteLine("Parameter[" + i + "]: " + parameters[i]);

                
                var (code_name, code) = pkg.Queries.Where(entry => entry.ID == queryID).Select(entry => (entry.Name as string, entry.Content as string)).FirstOrDefault();
                var t0 = DateTime.Now;
                Console.WriteLine("Started: " + t0);

                // var wb = wb_res.FirstOrDefault() as CodeData;
                var codes = new List<Tuple<string,string>>();
                codes.Add(new Tuple<string, string>(code_name, code));

                var result = QuantApp.Engine.Utils.ExecuteCodeFunction(false, codes, funcName, parameters);
                //var result = Connection.Client.Execute(code, code_name, pkg.ID, queryID, funcName, parameters);
                var t1 = DateTime.Now;
                Console.WriteLine("Ended: " + t1 + " taking " + (t1 - t0));
                
                Console.WriteLine("Result: ");
                Console.WriteLine(result);
            }
            //Azure Container Instance
            else if(args != null && args.Length > 1 && args[0] == "azure" && args[1] == "deploy")
            {
                PythonEngine.BeginAllowThreads();

                Console.WriteLine("Azure Container Instance start...");

                var pkg = Code.ProcessPackageFile(Code.UpdatePackageFile(workspace_name));
                var res = Code.BuildRegisterPackage(pkg);

                if(string.IsNullOrEmpty(res))
                    Console.WriteLine("Build Success!!!");
                else
                    Console.WriteLine(res);

                AzureCredentials credentials = SdkContext.AzureCredentialsFactory.FromFile(config["AzureContainerInstance"]["AuthFile"].ToString());

                var azure = Azure
                    .Configure()
                    .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                    .Authenticate(credentials)
                    .WithDefaultSubscription();

                //=============================================================
                // Create a container group with one container instance of default CPU core count and memory size
                //   using public Docker image "seanmckenna/aci-hellofiles" which mounts the file share created previously
                //   as read/write shared container volume.

                string rgName = pkg.ID.ToLower() + "-rg";//SdkContext.RandomResourceName("rgACI", 15);
                string aciName = pkg.Name.ToLower(); //SdkContext.RandomResourceName(config["AzureContainerInstance"]["Dns"].ToString(), 20);
                // string shareName = pkg.Name.ToLower() + "-fileshare";//SdkContext.RandomResourceName("fileshare", 20);
                string containerImageName = "coflows/quant";
                // string volumeMountName = "aci-coflows-volume";

                Console.WriteLine("aciName: " + aciName);
                Console.WriteLine("rgName: " + rgName);


                // string rgName = pkg.ID.ToLower() + "-rg";

                try
                {
                    Console.WriteLine("Cleaning Resource Group: " + rgName);
                    azure.ResourceGroups.BeginDeleteByName(rgName);
                    

                    IResourceGroup resGroup = azure.ResourceGroups.GetByName(rgName);
                    while(resGroup != null)
                    {
                        resGroup = azure.ResourceGroups.GetByName(rgName);

                        Console.Write(".");

                        SdkContext.DelayProvider.Delay(1000);
                    }
                    Console.WriteLine();

                    Console.WriteLine("Cleaned Resource Group: " + rgName);
                }
                catch (Exception)
                {
                    Console.WriteLine("Did not create any resources in Azure. No clean up is necessary");
                }


                
                Region region = Region.Create(config["AzureContainerInstance"]["Region"].ToString());
                Console.WriteLine("region: " + region);
                
                Task.Run(() =>
                    azure.ContainerGroups.Define(aciName)
                    .WithRegion(region)
                    .WithNewResourceGroup(rgName)
                    .WithLinux()
                    .WithPublicImageRegistryOnly()
                    // .WithNewAzureFileShareVolume(volumeMountName, shareName)
                    .WithoutVolume()
                    .DefineContainerInstance(aciName)
                        .WithImage(containerImageName)
                        .WithExternalTcpPort(sslFlag ? 443 : 80)
                        // .WithVolumeMountSetting(volumeMountName, "/aci/logs/")
                        .WithCpuCoreCount(Int32.Parse(config["AzureContainerInstance"]["Cores"].ToString()))
                        .WithMemorySizeInGB(Int32.Parse(config["AzureContainerInstance"]["Mem"].ToString()))
                        // .WithGpuResource(0, GpuSku.V100)
                        .WithEnvironmentVariables(new Dictionary<string,string>(){ 
                            {"coflows_config", File.ReadAllText(@"mnt/" + config_file)}, 
                            })
                        .WithStartingCommandLine("dotnet", "QuantApp.Server.quant.lnx.dll", "server")
                        .Attach()
                    .WithDnsPrefix(config["AzureContainerInstance"]["Dns"].ToString()) 
                    .CreateAsync()
                );

                // Poll for the container group
                IContainerGroup containerGroup = null;
                while(containerGroup == null)
                {
                    containerGroup = azure.ContainerGroups.GetByResourceGroup(rgName, aciName);

                    Console.Write(".");

                    SdkContext.DelayProvider.Delay(1000);
                }

                Console.WriteLine();
                Console.WriteLine($"Container group state: {containerGroup.Refresh().State}");
                // Poll until the container group is running
                while(containerGroup.State != "Running")
                {
                    Console.Write(".");
                    
                    System.Threading.Thread.Sleep(1000);
                }

                
                Console.WriteLine("Container instance IP address: " + containerGroup.IPAddress);
                Console.WriteLine("Container instance Ports: " + string.Join(",", containerGroup.ExternalTcpPorts));

                string serverUrl = config["AzureContainerInstance"]["Dns"].ToString() + "." + config["AzureContainerInstance"]["Region"].ToString().ToLower() + ".azurecontainer.io";
                Console.WriteLine("Container instance DNS Prefix: " + serverUrl);
                SdkContext.DelayProvider.Delay(10000);

                Connection.Client.Init(serverUrl, sslFlag);

                if(!Connection.Client.Login(config["Server"]["SecretKey"].ToString()))
                    throw new Exception("CoFlows Not connected!");

                Connection.Client.Connect();
                Console.Write("Container connected! ");

                QuantApp.Kernel.M.Factory = new MFactory();

                Console.Write("Starting azure deployment... ");

                Code.UpdatePackageFile(workspace_name);
                var t0 = DateTime.Now;
                Console.WriteLine("Started: " + t0);
                var resDeploy = Connection.Client.PublishPackage(workspace_name);
                var t1 = DateTime.Now;
                Console.WriteLine("Ended: " + t1 + " taking " + (t1 - t0));
                Console.Write("Result: " + resDeploy);
            }
            else if(args != null && args.Length > 1 && args[0] == "azure" && args[1] == "remove")
            {
                PythonEngine.BeginAllowThreads();

                Console.WriteLine("Azure Container Instance remove start");

                var pkg = Code.ProcessPackageFile(Code.UpdatePackageFile(workspace_name));
                var res = Code.BuildRegisterPackage(pkg);

                if(!string.IsNullOrEmpty(res))
                    Console.WriteLine(res);

                AzureCredentials credentials = SdkContext.AzureCredentialsFactory.FromFile(config["AzureContainerInstance"]["AuthFile"].ToString());

                var azure = Azure
                    .Configure()
                    .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                    .Authenticate(credentials)
                    .WithDefaultSubscription();

                string rgName = pkg.ID.ToLower() + "-rg";

                try
                {
                    Console.WriteLine("Deleting Resource Group: " + rgName);
                    azure.ResourceGroups.BeginDeleteByName(rgName);
                    

                    IResourceGroup resGroup = azure.ResourceGroups.GetByName(rgName);
                    while(resGroup != null)
                    {
                        resGroup = azure.ResourceGroups.GetByName(rgName);

                        Console.Write(".");

                        SdkContext.DelayProvider.Delay(1000);
                    }
                    Console.WriteLine();

                    Console.WriteLine("Deleted Resource Group: " + rgName);
                }
                catch (Exception)
                {
                    Console.WriteLine("Did not create any resources in Azure. No clean up is necessary");
                }
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

                        #if MONO_LINUX || MONO_OSX
                        if(useJupyter)
                        {
                            var code = "import subprocess; subprocess.check_call(['jupyter', 'lab', '--NotebookApp.notebook_dir=/app/mnt', '--ip=*', '--NotebookApp.allow_remote_access=True', '--allow-root', '--no-browser', '--NotebookApp.token=\'\'', '--NotebookApp.password=\'\'', '--NotebookApp.disable_check_xsrf=True', '--NotebookApp.base_url=/lab/" + id + "'])";
                            
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
                        #endif
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
