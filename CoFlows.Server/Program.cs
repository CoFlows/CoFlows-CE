/*
 * The MIT License (MIT)
 * Copyright (c) Arturo Rodriguez All rights reserved.
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

using CoFlows.Server.Utils;

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
using Microsoft.Azure.Management.ContainerInstance.Fluent.Models;


namespace CoFlows.Server
{
    public class Program
    {
        public static bool IsServer = false;
        public static string workflow_name = null;
        public static string hostName = null;
        public static string letsEncryptEmail = null;
        public static bool letsEncryptStaging = false;
        public static bool useJupyter = false;
        public static bool loadedJupyter = false;

        private static readonly System.Threading.AutoResetEvent _closing = new System.Threading.AutoResetEvent(false);
        public static void Main(string[] args)
        {
            #if NETCOREAPP3_1
            Console.Write("CoFlows CE - NetCoreApp 3.1... ");
            #endif

            #if NET461
            Console.Write("CoFlows CE - Net Framework 461... ");
            #endif

            Console.Write("Python starting... ");
            PythonEngine.Initialize();

            Code.InitializeCodeTypes(new Type[]{ 
                typeof(QuantApp.Engine.Workflow),
                typeof(Jint.Native.Array.ArrayConstructor)
                });

            var config_env = Environment.GetEnvironmentVariable("coflows_config");
            var config_file = Environment.GetEnvironmentVariable("config_file");

            if(string.IsNullOrEmpty(config_file))
                config_file = "coflows_config.json";

            JObject config = string.IsNullOrEmpty(config_env) ? (JObject)JToken.ReadFrom(new JsonTextReader(File.OpenText(@"mnt/" + config_file))) : (JObject)JToken.Parse(config_env);
            workflow_name = config["Workflow"].ToString();
            hostName = config["Server"]["Host"].ToString();
            var secretKey = config["Server"]["SecretKey"].ToString();
            
            letsEncryptEmail = config["Server"]["LetsEncrypt"]["Email"].ToString();
            letsEncryptStaging = config["Server"]["LetsEncrypt"]["Staging"].ToString().ToLower() == "true";

            var sslFlag = hostName.ToLower() != "localhost" && !string.IsNullOrWhiteSpace(letsEncryptEmail);

            useJupyter = config["Jupyter"].ToString().ToLower() == "true";

            // var connectionString = config["Database"].ToString();

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

                Code.UpdatePackageFile(workflow_name);
                var t0 = DateTime.Now;
                Console.WriteLine("Started: " + t0);
                var res = Connection.Client.PublishPackage(workflow_name);
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

                Code.UpdatePackageFile(workflow_name);
                var t0 = DateTime.Now;
                Console.WriteLine("Started: " + t0);
                var res = Connection.Client.BuildPackage(workflow_name);
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

                var pkg = Code.ProcessPackageFile(workflow_name, true);
                Console.WriteLine("Workflow: " + pkg.Name);

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

                var res = Connection.Client.RemoteLog(workflow_name);
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

                var res = Connection.Client.RemoteRemove(workflow_name);
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

                var res = Connection.Client.RemoteRestart(workflow_name);
                Console.WriteLine("Result: ");
                Console.WriteLine(res);
            }
            else if(args != null && args.Length > 0 && args[0] == "server")
            {
                PythonEngine.BeginAllowThreads();

                // Databases(connectionString);
                var connectionString = config["Database"]["Connection"].ToString();
                var type = config["Database"]["Type"].ToString();
                if(type.ToLower() == "mssql" || type.ToLower() == "postgres")
                    DatabasesDB(connectionString);
                else
                    DatabasesSqlite(connectionString);

                Console.WriteLine("QuantApp Server " + DateTime.Now);
                Console.WriteLine("DB Connected");

                Console.WriteLine("Local deployment");

                if(string.IsNullOrEmpty(config_env))
                {
                    var pkg = Code.ProcessPackageFile(workflow_name, true);
                    Code.ProcessPackageJSON(pkg);
                    SetDefaultWorkflows(new string[]{ pkg.ID }, false, config["Jupter"] != null && config["Jupter"].ToString().ToLower() == "true");
                    Console.WriteLine(pkg.Name + " started");

                    var _g = Group.FindGroup(pkg.ID);
                    if(_g == null)
                        _g = Group.CreateGroup(pkg.ID, pkg.ID);
                    
                    foreach(var _p in pkg.Permissions)
                    {
                        string _id = "QuantAppSecure_" + _p.ID.ToLower().Replace('@', '.').Replace(':', '.');
                        var _quser = QuantApp.Kernel.User.FindUser(_id);
                        if(_quser != null)
                            _g.Add(_quser, typeof(QuantApp.Kernel.User), _p.Permission);
                    }
                }
                else
                {
                    Console.WriteLine("Empty server...");
                    var workflow_ids = QuantApp.Kernel.M.Base("--CoFlows--Workflows")[xe => true];
                    foreach(var wsp in workflow_ids)
                    {
                        SetDefaultWorkflows(new string[]{ wsp.ToString() }, true, config["Jupter"] != null && config["Jupter"].ToString().ToLower() == "true");
                        Console.WriteLine(wsp + " started");
                    }
                }


                if(!sslFlag)
                    Init(new string[]{"--urls", "http://*:80"}, new Realtime.WebSocketListner(), typeof(Startup<CoFlows.Server.Realtime.RTDSocketMiddleware>));
                else
                    Init(args, new Realtime.WebSocketListner(), typeof(Startup<CoFlows.Server.Realtime.RTDSocketMiddleware>));
                
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

                // Databases(connectionString);
                var connectionString = config["Database"]["Connection"].ToString();
                var type = config["Database"]["Type"].ToString();
                if(type.ToLower() == "mssql" || type.ToLower() == "postgres")
                    DatabasesDB(connectionString);
                else
                    DatabasesSqlite(connectionString);

                Console.WriteLine("DB Connected");

                Console.WriteLine("Local build");

                var pkg = Code.ProcessPackageFile(Code.UpdatePackageFile(workflow_name), true);
                var res = Code.BuildRegisterPackage(pkg);
                if(string.IsNullOrEmpty(res))
                    Console.WriteLine("Success!!!");
                else
                    Console.WriteLine(res);
            }
            else if(args != null && args.Length > 2 && args[0] == "local" && args[1] == "query")
            {
                PythonEngine.BeginAllowThreads();

                // Databases(connectionString);
                var connectionString = config["Database"]["Connection"].ToString();
                var type = config["Database"]["Type"].ToString();
                if(type.ToLower() == "mssql" || type.ToLower() == "postgres")
                    DatabasesDB(connectionString);
                else
                    DatabasesSqlite(connectionString);

                Console.WriteLine("Local Query " + DateTime.Now);
                Console.WriteLine("DB Connected");

                Console.WriteLine("CoFlows Local query... ");

                var queryID = args[2];
                var funcName = args.Length > 3 ? args[3] : null;
                var parameters = args.Length > 4 ? args.Skip(4).ToArray() : null;

                Console.WriteLine("QueryID: " + queryID);
                Console.WriteLine("FuncName: " + funcName);
                Console.WriteLine("Parameters: " + parameters);


                var pkg = Code.ProcessPackageFile(Code.UpdatePackageFile(workflow_name), true);
                Code.ProcessPackageJSON(pkg);

                var _g = Group.FindGroup(pkg.ID);
                if(_g == null)
                    _g = Group.CreateGroup(pkg.ID, pkg.ID);
                
                foreach(var _p in pkg.Permissions)
                {
                    string _id = "QuantAppSecure_" + _p.ID.ToLower().Replace('@', '.').Replace(':', '.');
                    var _quser = QuantApp.Kernel.User.FindUser(_id);
                    if(_quser != null)
                        _g.Add(_quser, typeof(QuantApp.Kernel.User), _p.Permission);
                }

                

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
            else if(args != null && args.Length > 1 && args[0] == "aci" && args[1] == "deploy")
            {
                PythonEngine.BeginAllowThreads();

                Console.WriteLine();
                Console.WriteLine("Azure Container Instance start...");
                var t0 = DateTime.Now;
                Console.WriteLine("Started: " + t0);


                var pkg = Code.ProcessPackageFile(Code.UpdatePackageFile(workflow_name), true);
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

                string rgName = pkg.ID.ToLower() + "-rg";
                string aciName = pkg.Name.ToLower();
                string containerImageName = "coflows/ce";
                
                Console.WriteLine("Container Name: " + aciName);
                Console.WriteLine("Resource Group Name: " + rgName);

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
                catch (Exception e)
                {
                    Console.WriteLine();
                    Console.WriteLine("Did not create any resources in Azure. No clean up is necessary");
                }
                
                Region region = Region.Create(config["AzureContainerInstance"]["Region"].ToString());
                Console.WriteLine("Region: " + region);

                
                
                if(config["AzureContainerInstance"]["Gpu"] != null && config["AzureContainerInstance"]["Gpu"]["Cores"].ToString() != "" && config["AzureContainerInstance"]["Gpu"]["Cores"].ToString() != "0" && config["AzureContainerInstance"]["Gpu"]["SKU"].ToString() != "")
                {
                    Console.WriteLine("Creating a GPU container...");
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
                            .WithExternalTcpPorts(new int[]{ 80, 443 })
                            // .WithVolumeMountSetting(volumeMountName, "/aci/logs/")
                            .WithCpuCoreCount(Int32.Parse(config["AzureContainerInstance"]["Cores"].ToString()))
                            .WithMemorySizeInGB(Int32.Parse(config["AzureContainerInstance"]["Mem"].ToString()))
                            .WithGpuResource(
                                Int32.Parse(config["AzureContainerInstance"]["Gpu"]["Cores"].ToString()), 
                                config["AzureContainerInstance"]["Gpu"]["SKU"].ToString().ToLower() == "k80" ? GpuSku.K80 : config["AzureContainerInstance"]["Gpu"]["SKU"].ToString().ToLower() == "p100" ? GpuSku.P100 : GpuSku.V100
                                )
                            .WithEnvironmentVariables(new Dictionary<string,string>(){ 
                                {"coflows_config", File.ReadAllText(@"mnt/" + config_file)}, 
                                })
                            .WithStartingCommandLine("dotnet", "CoFlows.Server.lnx.dll", "server")
                            .Attach()
                        .WithDnsPrefix(config["AzureContainerInstance"]["Dns"].ToString()) 
                        .CreateAsync()
                    );
                }
                else
                {
                    Console.WriteLine("Creating a standard container...");
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
                            .WithExternalTcpPorts(new int[]{ 80, 443 })
                            // .WithExternalTcpPort(sslFlag ? 443 : 80)
                            // .WithVolumeMountSetting(volumeMountName, "/aci/logs/")
                            .WithCpuCoreCount(Int32.Parse(config["AzureContainerInstance"]["Cores"].ToString()))
                            .WithMemorySizeInGB(Int32.Parse(config["AzureContainerInstance"]["Mem"].ToString()))
                            .WithEnvironmentVariables(new Dictionary<string,string>(){ 
                                {"coflows_config", File.ReadAllText(@"mnt/" + config_file)}, 
                                })
                            .WithStartingCommandLine("dotnet", "CoFlows.Server.lnx.dll", "server")
                            .Attach()
                        .WithDnsPrefix(config["AzureContainerInstance"]["Dns"].ToString()) 
                        .CreateAsync()
                    );
                }
                

                // Poll for the container group
                IContainerGroup containerGroup = null;
                while(containerGroup == null)
                {
                    containerGroup = azure.ContainerGroups.GetByResourceGroup(rgName, aciName);

                    Console.Write(".");

                    SdkContext.DelayProvider.Delay(1000);
                }

                var lastContainerGroupState = containerGroup.Refresh().State;

                Console.WriteLine();
                Console.WriteLine($"Container group state: {containerGroup.Refresh().State}");
                // Poll until the container group is running
                while(containerGroup.State != "Running")
                {
                    var containerGroupState = containerGroup.Refresh().State;
                    if(containerGroupState != lastContainerGroupState)
                    {
                        Console.WriteLine();
                        Console.WriteLine(containerGroupState);
                        lastContainerGroupState = containerGroupState;
                    }
                    Console.Write(".");
                    
                    System.Threading.Thread.Sleep(1000);
                }
                Console.WriteLine();
                Console.WriteLine("Container instance IP address: " + containerGroup.IPAddress);
                Console.WriteLine("Container instance Ports: " + string.Join(",", containerGroup.ExternalTcpPorts));

                string serverUrl = config["AzureContainerInstance"]["Dns"].ToString() + "." + config["AzureContainerInstance"]["Region"].ToString().ToLower() + ".azurecontainer.io";
                Console.WriteLine("Container instance DNS Prefix: " + serverUrl);
                SdkContext.DelayProvider.Delay(10000);

                Connection.Client.Init(serverUrl, sslFlag);

                for(int i = 0; i < 50 ; i++)
                {
                    try
                    {
                        SdkContext.DelayProvider.Delay(10000);
                        // Console.WriteLine("Connecting to Cluster(" + i + "): " + CoFlows.Server.Program.hostName + " with SSL " + sslFlag + " " + config["Server"]["SecretKey"].ToString());
                        if(!Connection.Client.Login(config["Server"]["SecretKey"].ToString()))
                            throw new Exception("CoFlows Not connected!");

                        Connection.Client.Connect();
                        Console.WriteLine("Container connected! " + i);
                        break;
                    }
                    catch{}
                }

                QuantApp.Kernel.M.Factory = new MFactory();

                Console.Write("Starting azure deployment... ");

                Code.UpdatePackageFile(workflow_name);
                var resDeploy = Connection.Client.PublishPackage(workflow_name);
                var t1 = DateTime.Now;
                Console.WriteLine("Ended: " + t1 + " taking " + (t1 - t0));
                Console.Write("Result: " + resDeploy);
            }
            else if(args != null && args.Length > 1 && args[0] == "aci" && args[1] == "remove")
            {
                PythonEngine.BeginAllowThreads();

                Console.WriteLine("Azure Container Instance remove start");

                var pkg = Code.ProcessPackageFile(Code.UpdatePackageFile(workflow_name), true);
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

        private static void DatabasesSqlite(string sqliteFile)
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
                var script = File.ReadAllText(@"sql/create.sql");
                QuantApp.Kernel.Database.DB["Kernel"].ExecuteCommand(script);
            }
        }

        private static void DatabasesDB(string KernelConnectString)
        {
            if (QuantApp.Kernel.User.CurrentUser == null)
                QuantApp.Kernel.User.CurrentUser = new QuantApp.Kernel.User("System");

            if (QuantApp.Kernel.M.Factory == null)
            {         
                if(KernelConnectString.StartsWith("Server="))
                {
                    MSSQLDataSetAdapter KernelDataAdapter = new MSSQLDataSetAdapter();
                    KernelDataAdapter.ConnectString = KernelConnectString;
                    QuantApp.Kernel.Database.DB.Add("Kernel", KernelDataAdapter);
                }
                else if(KernelConnectString.StartsWith("Host="))
                {
                    var _KernelDataAdapter = new PostgresDataSetAdapter();
                    _KernelDataAdapter.ConnectString = KernelConnectString;
                    _KernelDataAdapter.CreateDB(KernelConnectString, new List<string> {
                        File.ReadAllText(@"sql/create.sql").Replace("DateTime", "timestamp"),
                        File.ReadAllText(@"sql/quant.sql").Replace("DateTime", "timestamp"),
                        File.ReadAllText(@"sql/cluster.sql").Replace("DateTime", "timestamp"),
                        File.ReadAllText(@"sql/calendars.sql"),
                        File.ReadAllText(@"sql/fic.sql")
                    });
                    var KernelDataAdapter = new PostgresDataSetAdapter();
                    KernelDataAdapter.ConnectString = KernelConnectString;
                    QuantApp.Kernel.Database.DB.Add("Kernel", KernelDataAdapter);
                }
                else
                {
                    if(!QuantApp.Kernel.Database.DB.ContainsKey("Kernel"))
                    {
                        SQLiteDataSetAdapter KernelDataAdapter = new SQLiteDataSetAdapter();
                        KernelDataAdapter.ConnectString = KernelConnectString;
                        QuantApp.Kernel.Database.DB.Add("Kernel", KernelDataAdapter);
                    }
                }


                QuantApp.Kernel.M.Factory = new QuantApp.Kernel.Adapters.SQL.Factories.SQLMFactory();
            }

            if (!QuantApp.Kernel.Database.DB.ContainsKey("CloudApp"))
            {
                QuantApp.Kernel.Database.DB.Add("CloudApp", QuantApp.Kernel.Database.DB["Kernel"]);
                QuantApp.Kernel.User.Factory = new QuantApp.Kernel.Adapters.SQL.Factories.SQLUserFactory();
                Group.Factory = new QuantApp.Kernel.Adapters.SQL.Factories.SQLGroupFactory();
            }
        }

        private static List<string> _wspServicedList = new List<string>();
        public static IEnumerable<Workflow> SetDefaultWorkflows(string[] ids, bool saveToDisk, bool startJupyter)
        {
            foreach(var id in ids)
            {
                try
                {
                    //Workflow loading...

                    var wsp = QuantApp.Kernel.M.Base(id)[x => true].FirstOrDefault() as Workflow;
                    
                    if(wsp != null)
                    {
                        QuantApp.Engine.Utils.ActiveWorkflowList.Add(wsp);
                        _wspServicedList.Add(id);

                        if(saveToDisk)
                        {
                            Code.InstallNuGets(wsp.NuGets);
                            Code.InstallPips(wsp.Pips);
                            Code.InstallJars(wsp.Jars);

                            var pkg = Code.ProcessPackageWorkflow(wsp);
                            Code.ProcessPackageJSON(pkg);
                            
                            var bytes = QuantApp.Engine.Code.ProcessPackageToZIP(pkg);
                            var archive = new ZipArchive(new MemoryStream(bytes));
                            
                            foreach(var entry in archive.Entries)
                            {
                                var entryStream = entry.Open();
                                var streamReader = new StreamReader(entryStream);
                                var content = streamReader.ReadToEnd();
                                // var filePath = "/Workflow/" + entry.FullName;
                                var filePath = "/app/mnt/" + entry.FullName;

                                System.IO.FileInfo file = new System.IO.FileInfo(filePath);
                                file.Directory.Create(); // If the directory already exists, this method does nothing.
                                System.IO.File.WriteAllText(file.FullName, content);
                            }
                        }
                        

                        // QuantApp.Engine.Utils.ActiveWorkflowList.Add(wsp);
                        // _wspServicedList.Add(id);

                        foreach(var fid in wsp.Agents)
                        {
                            var cfid = fid.Replace("$WID$",id);
                            var f = F.Find(cfid).Value;
                            f.Start();
                        }

                        // #if MONO_LINUX || MONO_OSX
                        // if(useJupyter && !loadedJupyter)
                        // {
                        //     loadedJupyter = true;
                        //     var userName = "arturo_rodriguez_coflows_com";
                        //     var createUser = "import subprocess;newUser = '" + userName + "';userExists = newUser in list(map(lambda x: x.split(':')[0], subprocess.check_output(['getent', 'passwd']).decode('utf-8').split('\\n'))); print('User exists: ' + newUser) if userExists else subprocess.check_call(['adduser', '--gecos', '\"First Last,RoomNumber,WorkPhone,HomePhone\"', '--disabled-password', '--home', f'/app/mnt/home/{newUser}/', '--shell', '/bin/bash', f'{newUser}']); print('no need to edit .bashrc') if userExists else open(f'/app/mnt/home/{newUser}/.bashrc', 'a').write('PS1=\"\\\\u:\\\\w> \"')";
                        //     var code = "import subprocess;subprocess.check_call(['sudo', '-u', '" + userName + "', 'jupyter', 'lab', '--port=8888', '--NotebookApp.notebook_dir=/app/mnt/', '--ip=*', '--NotebookApp.allow_remote_access=True', '--no-browser', '--NotebookApp.token=\'\'', '--NotebookApp.password=\'\'', '--NotebookApp.disable_check_xsrf=True', '--NotebookApp.base_url=/lab/" + id + "'], cwd='/app/mnt/home/" + userName + "')";
                            
                        //     var th = new System.Threading.Thread(() => {
                        //         using (Py.GIL())
                        //         {
                        //             Console.WriteLine("Starting Jupyter...");
                                    
                        //             Console.WriteLine(createUser);
                        //             PythonEngine.Exec(createUser);

                        //             Console.WriteLine(code);
                        //             PythonEngine.Exec(code);
                        //         }
                        //     });
                        //     th.Start();
                        // }
                        // #endif

                        
                    }
                    else
                    {
                        Console.WriteLine("Default workflow is null: " + id);
                        Environment.Exit(-1);
                    }

                }
                catch(Exception e)
                {
                    Console.WriteLine(e);
                }

            }

            return QuantApp.Engine.Utils.ActiveWorkflowList;
        }


        private static readonly object logLock = new object();
        public static IEnumerable<Workflow> GetDefaultWorkflows()
        {
            return QuantApp.Engine.Utils.ActiveWorkflowList;
        }

        public static void AddServicedWorkflows(string id)
        {
            //Workflow loading...
            var wsp = QuantApp.Kernel.M.Base(id)[x => true].FirstOrDefault() as Workflow;
            if(wsp != null)
            {
            
                _wspServicedList.RemoveAll(x => x == id);
                // foreach(var fid in wsp.Agents)
                // {
                //     var cfid = fid.Replace("$WID$",id);

                //     var f = F.Find(cfid).Value;
                //     f.Stop();
                // }

                _wspServicedList.Add(id);

                // #if MONO_LINUX || MONO_OSX
                // if(useJupyter)
                // {
                //     var code = "import subprocess; subprocess.check_call(['jupyter', 'lab', '--NotebookApp.notebook_dir=/app/mnt', '--ip=*', '--NotebookApp.allow_remote_access=True', '--allow-root', '--no-browser', '--NotebookApp.token=\'\'', '--NotebookApp.password=\'\'', '--NotebookApp.disable_check_xsrf=True', '--NotebookApp.base_url=/lab/" + id + "'])";
                //     var th = new System.Threading.Thread(() => {
                //         using (Py.GIL())
                //         {
                //             Console.WriteLine("Starting Jupyter...");
                //             Console.WriteLine(code);
                //             PythonEngine.Exec(code);
                //         }
                //     });
                //     th.Start();
                // }
                // #endif

                
            }
            else
                Console.WriteLine("Add serviced workflow is null: " + id);
        }

        public static void RemoveServicesWorkflow(string id)
        {
            Console.WriteLine("RemoveServicesWorkflow: " + id);
            _wspServicedList.RemoveAll(x => x == id);
            var workflow_ids = QuantApp.Kernel.M.Base("--CoFlows--Workflows");
            if(workflow_ids[x => true].Where(x => x.ToString() == id).Count() > 0)
            {
                workflow_ids.Remove(id);
                workflow_ids.Save();
            }
        }

        public static IEnumerable<string> GetServicedWorkflows()
        {
            return _wspServicedList;
        }

        public static void Init(string[] args, QuantApp.Kernel.Factories.IRTDEngineFactory socketFactory, Type startup)
        {
            // QuantApp.Kernel.RTDEngine.Factory = new Realtime.WebSocketListner();
            QuantApp.Kernel.RTDEngine.Factory = socketFactory;

            if (args == null || args.Length == 0 || args[0] == "server")
            {
                Console.WriteLine("Only accepts secure SSL connections...");
                Host.CreateDefaultBuilder(args)
                    .ConfigureWebHostDefaults(webBuilder =>
                    {
                        webBuilder.UseUrls(new string[] { "http://*", "https://*" });
                        webBuilder
                        // .ConfigureKestrel(serverOptions =>
                        // {
                        //     serverOptions.Listen(System.Net.IPAddress.Any, 80, listenOptions => {});
                        //     serverOptions.Listen(System.Net.IPAddress.Any, 443, listenOptions =>
                        //     {
                        //         // listenOptions.UseHttps();//ssl_cert, ssl_password);
                        //     });
                        // })
                        // .UseStartup<Startup>();
                        .UseStartup(startup);
                    })
                    .Build()
                    .Run();
            }
            else
            {
                Console.WriteLine("SSL encryption is not used....");
                Host.CreateDefaultBuilder(args)
                    .ConfigureWebHostDefaults(webBuilder =>
                    {
                        // webBuilder.UseStartup<Startup>();
                        webBuilder.UseStartup(startup);
                    })
                    .Build()
                    .Run();
            }
        }
    }
}