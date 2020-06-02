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
using System.Linq;
using System.Threading.Tasks;

using System.Reflection;

using QuantApp.Kernel;
using QuantApp.Kernel.Adapters.SQL;
using QuantApp.Engine;

using CoFlows.Server.Utils;

using Python.Runtime;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using AQI.AQILabs.Kernel;

//Azure Dependencies
using Microsoft.Azure.Management.ContainerInstance.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.ContainerInstance.Fluent.Models;

namespace CoFlows.Server.Quant
{
    public class Program
    {
        private static readonly System.Threading.AutoResetEvent _closing = new System.Threading.AutoResetEvent(false);
        public static void Main(string[] args)
        {
            #if NETCOREAPP3_1
            Console.Write("CoFlows Quant - NetCoreApp 3.1... ");
            #endif

            #if NET461
            Console.Write("CoFlows Quant - Net Framework 461... ");
            #endif
            
            Console.Write("Python starting... ");
            PythonEngine.Initialize();

            Code.InitializeCodeTypes(new Type[]{ 
                typeof(QuantApp.Engine.Workflow),
                typeof(Jint.Native.Array.ArrayConstructor),
                typeof(AQI.AQILabs.Kernel.Instrument), 
                typeof(AQI.AQILabs.Derivatives.CashFlow), 
                typeof(AQI.AQILabs.SDK.Strategies.PortfolioStrategy)
                });

            var config_env = Environment.GetEnvironmentVariable("coflows_config");
            var config_file = Environment.GetEnvironmentVariable("config_file");

            if(string.IsNullOrEmpty(config_file))
                config_file = "coflows_config.json";

            JObject config = string.IsNullOrEmpty(config_env) ? (JObject)JToken.ReadFrom(new JsonTextReader(File.OpenText(@"mnt/" + config_file))) : (JObject)JToken.Parse(config_env);

            CoFlows.Server.Program.workflow_name = config["Workflow"].ToString();
            CoFlows.Server.Program.hostName = config["Server"]["Host"].ToString();
            var secretKey = config["Server"]["SecretKey"].ToString();

            var cloudHost = config["Cloud"]["Host"].ToString();
            var cloudKey = config["Cloud"]["SecretKey"].ToString();
            var cloudSSL = config["Cloud"]["SSL"].ToString();

            CoFlows.Server.Program.letsEncryptEmail = config["Server"]["LetsEncrypt"]["Email"].ToString();
            CoFlows.Server.Program.letsEncryptStaging = config["Server"]["LetsEncrypt"]["Staging"].ToString().ToLower() == "true";

            var sslFlag = CoFlows.Server.Program.hostName.ToLower() != "localhost" && !string.IsNullOrWhiteSpace(CoFlows.Server.Program.letsEncryptEmail);


            CoFlows.Server.Program.useJupyter = config["Jupyter"].ToString().ToLower() == "true";

            if(args != null && args.Length > 0 && args[0] == "lab")
            {
                Connection.Client.Init(CoFlows.Server.Program.hostName, sslFlag);

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

                Code.UpdatePackageFile(CoFlows.Server.Program.workflow_name);
                var t0 = DateTime.Now;
                Console.WriteLine("Started: " + t0);
                var res = Connection.Client.PublishPackage(CoFlows.Server.Program.workflow_name);
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

                Code.UpdatePackageFile(CoFlows.Server.Program.workflow_name);
                var t0 = DateTime.Now;
                Console.WriteLine("Started: " + t0);
                var res = Connection.Client.BuildPackage(CoFlows.Server.Program.workflow_name);
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

                var pkg = Code.ProcessPackageFile(CoFlows.Server.Program.workflow_name, true);
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

                var res = Connection.Client.RemoteLog(CoFlows.Server.Program.workflow_name);
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

                var res = Connection.Client.RemoteRemove(CoFlows.Server.Program.workflow_name);
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

                var res = Connection.Client.RemoteRestart(CoFlows.Server.Program.workflow_name);
                Console.WriteLine("Result: ");
                Console.WriteLine(res);
            }
            else if(args != null && args.Length > 0 && args[0] == "server")
            {
                PythonEngine.BeginAllowThreads();

                var type = config["Database"]["Type"].ToString();
                if(type.ToLower() == "mssql" || type.ToLower() == "postgres")
                {
                    if(config["Database"]["Connection"] == null)
                    {
                        var kernelString = config["Database"]["Kernel"].ToString();
                        var strategyString = config["Database"]["Strategies"].ToString();
                        var quantappString = config["Database"]["QuantApp"].ToString();
                        Databases(kernelString, strategyString, quantappString);
                    }
                    else
                    {
                        var kernelString = config["Database"]["Connection"].ToString();
                        var strategyString = kernelString;
                        var quantappString = kernelString;
                        Databases(kernelString, strategyString, quantappString);
                    }
                }
                else
                {
                    var connectionString = config["Database"]["Connection"].ToString();
                    Databases(connectionString);
                }

                SetRTD();

                Console.WriteLine("QuantApp Server " + DateTime.Now);
                Console.WriteLine("DB Connected");

                Console.WriteLine("Local deployment");

                if(string.IsNullOrEmpty(config_env))
                {
                    var pkg = Code.ProcessPackageFile(CoFlows.Server.Program.workflow_name, true);
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
                    var workspace_ids = QuantApp.Kernel.M.Base("--CoFlows--Workflows")[xe => true];
                    foreach(var wsp in workspace_ids)
                    {
                        SetDefaultWorkflows(new string[]{ wsp.ToString() }, true, config["Jupter"] != null && config["Jupter"].ToString().ToLower() == "true");
                        Console.WriteLine(wsp + " started");
                    }
                }


                /// QuantSpecific START
                Instrument.TimeSeriesLoadFromDatabaseIntraday = config["Quant"]["Intraday"].ToString().ToLower() == "true";
                if(Instrument.TimeSeriesLoadFromDatabaseIntraday)
                    Console.WriteLine("Intraday Timeseries");
                else
                    Console.WriteLine("Close Timeseries");
                Strategy.Executer = true;
                // Market.Initialize();

                var saveAll = config["Quant"]["AutoSave"].ToString().ToLower() == "true";
                if (saveAll) {
                    var ths = new System.Threading.Thread(x => (AQI.AQILabs.Kernel.Instrument.Factory as AQI.AQILabs.Kernel.Adapters.SQL.Factories.SQLInstrumentFactory).SaveAllLoop(5));
                    ths.Start();
                }
                else
                    Console.WriteLine("Not saving timeseries");

                /// QuantSpecific END
                
                if(!sslFlag)
                    CoFlows.Server.Program.Init(new string[]{"--urls", "http://*:80"}, new Realtime.WebSocketListner(), typeof(Startup<CoFlows.Server.Realtime.RTDSocketMiddleware>));
                else
                    CoFlows.Server.Program.Init(args, new Realtime.WebSocketListner(), typeof(Startup<CoFlows.Server.Realtime.RTDSocketMiddleware>));
                
            
            
                Task.Factory.StartNew(() => {
                    while (true)
                    {
                        System.Threading.Thread.Sleep(1000);
                    }
                });
                Console.CancelKeyPress += new ConsoleCancelEventHandler(OnExit);
                _closing.WaitOne();
            }
            else if(args != null && args.Length > 1 && args[0] == "local" && args[1] == "build")
            {
                PythonEngine.BeginAllowThreads();

                var type = config["Database"]["Type"].ToString();
                if(type.ToLower() == "mssql" || type.ToLower() == "postgres")
                {
                    if(config["Database"]["Connection"] == null)
                    {
                        var kernelString = config["Database"]["Kernel"].ToString();
                        var strategyString = config["Database"]["Strategies"].ToString();
                        var quantappString = config["Database"]["QuantApp"].ToString();
                        Databases(kernelString, strategyString, quantappString);
                    }
                    else
                    {
                        var kernelString = config["Database"]["Connection"].ToString();
                        var strategyString = kernelString;
                        var quantappString = kernelString;
                        Databases(kernelString, strategyString, quantappString);
                    }
                }
                else
                {
                    var connectionString = config["Database"]["Connection"].ToString();
                    Databases(connectionString);
                }

                Console.WriteLine("DB Connected");

                Console.WriteLine("Local build");

                var pkg = Code.ProcessPackageFile(Code.UpdatePackageFile(CoFlows.Server.Program.workflow_name), true);
                var res = Code.BuildRegisterPackage(pkg);

                if(string.IsNullOrEmpty(res))
                    Console.WriteLine("Success!!!");
                else
                    Console.WriteLine(res);
            }
            else if(args != null && args.Length > 2 && args[0] == "local" && args[1] == "query")
            {
                PythonEngine.BeginAllowThreads();

                var type = config["Database"]["Type"].ToString();
                if(type.ToLower() == "mssql" || type.ToLower() == "postgres")
                {
                    if(config["Database"]["Connection"] == null)
                    {
                        var kernelString = config["Database"]["Kernel"].ToString();
                        var strategyString = config["Database"]["Strategies"].ToString();
                        var quantappString = config["Database"]["QuantApp"].ToString();
                        Databases(kernelString, strategyString, quantappString);
                    }
                    else
                    {
                        var kernelString = config["Database"]["Connection"].ToString();
                        var strategyString = kernelString;
                        var quantappString = kernelString;
                        Databases(kernelString, strategyString, quantappString);
                    }
                }
                else
                {
                    var connectionString = config["Database"]["Connection"].ToString();
                    Databases(connectionString);
                }

                Console.WriteLine("Local Query " + DateTime.Now);
                Console.WriteLine("DB Connected");

                Console.WriteLine("CoFlows Local query... ");

                var queryID = args[2];
                var funcName = args.Length > 3 ? args[3] : null;
                var parameters = args.Length > 4 ? args.Skip(4).ToArray() : null;

                Console.WriteLine("QueryID: " + queryID);
                Console.WriteLine("FuncName: " + funcName);
                Console.WriteLine("Parameters: " + parameters);


                var pkg = Code.ProcessPackageFile(Code.UpdatePackageFile(CoFlows.Server.Program.workflow_name), true);
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


                var pkg = Code.ProcessPackageFile(Code.UpdatePackageFile(CoFlows.Server.Program.workflow_name), true);
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
                string containerImageName = "coflows/quant";
                
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
                            .WithStartingCommandLine("dotnet", "CoFlows.Server.quant.lnx.dll", "server")
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
                            // .WithVolumeMountSetting(volumeMountName, "/aci/logs/")
                            .WithCpuCoreCount(Int32.Parse(config["AzureContainerInstance"]["Cores"].ToString()))
                            .WithMemorySizeInGB(Int32.Parse(config["AzureContainerInstance"]["Mem"].ToString()))
                            .WithEnvironmentVariables(new Dictionary<string,string>(){ 
                                {"coflows_config", File.ReadAllText(@"mnt/" + config_file)}, 
                                })
                            .WithStartingCommandLine("dotnet", "CoFlows.Server.quant.lnx.dll", "server")
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

                Code.UpdatePackageFile(CoFlows.Server.Program.workflow_name);
                var resDeploy = Connection.Client.PublishPackage(CoFlows.Server.Program.workflow_name);
                var t1 = DateTime.Now;
                Console.WriteLine("Ended: " + t1 + " taking " + (t1 - t0));
                Console.Write("Result: " + resDeploy);
            }
            else if(args != null && args.Length > 1 && args[0] == "aci" && args[1] == "remove")
            {
                PythonEngine.BeginAllowThreads();
                Console.WriteLine();
                Console.WriteLine("Azure Container Instance remove start");

                var pkg = Code.ProcessPackageFile(Code.UpdatePackageFile(CoFlows.Server.Program.workflow_name), true);
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
                string aciName = pkg.Name.ToLower();
                
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

        private static void SetRTD()
        {
            CoFlows.Server.Realtime.WebSocketListner.RTDMessageFunction = new CoFlows.Server.Realtime.RTDMessageDelegate((message_string) => {
                var message = JsonConvert.DeserializeObject<QuantApp.Kernel.RTDMessage>(message_string);
                if ((int)message.Type == (int)AQI.AQILabs.Kernel.RTDMessage.MessageType.MarketData)
                {
                    try
                    {
                        AQI.AQILabs.Kernel.RTDMessage.MarketData content = JsonConvert.DeserializeObject<AQI.AQILabs.Kernel.RTDMessage.MarketData>(message.Content.ToString());

                        AQI.AQILabs.Kernel.Instrument instrument = AQI.AQILabs.Kernel.Instrument.FindInstrument(content.InstrumentID);

                        DateTime stamp = content.Timestamp;
                        if (content.Value != 0)
                            instrument.AddTimeSeriesPoint(stamp, content.Value, content.Type, AQI.AQILabs.Kernel.DataProvider.DefaultProvider, true, false);

                        return Tuple.Create(instrument.ID.ToString(), message_string);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("MarketData Exception: " + e);
                    }
                }
                else if ((int)message.Type == (int)AQI.AQILabs.Kernel.RTDMessage.MessageType.StrategyData)
                {
                    try
                    {
                        AQI.AQILabs.Kernel.RTDMessage.StrategyData content = JsonConvert.DeserializeObject<AQI.AQILabs.Kernel.RTDMessage.StrategyData>(message.Content.ToString());

                        AQI.AQILabs.Kernel.Strategy instrument = AQI.AQILabs.Kernel.Instrument.FindInstrument(content.InstrumentID) as AQI.AQILabs.Kernel.Strategy;

                        instrument.AddMemoryPoint(content.Timestamp, content.Value, content.MemoryTypeID, content.MemoryClassID, true, false);

                        return Tuple.Create(instrument.ID.ToString(), message_string);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("StrategyData Exception: " + e);
                    }
                }
                else if ((int)message.Type == (int)AQI.AQILabs.Kernel.RTDMessage.MessageType.CreateAccount)
                {
                    try
                    {
                        AQI.AQILabs.Kernel.RTDMessage.CreateAccount content = JsonConvert.DeserializeObject<AQI.AQILabs.Kernel.RTDMessage.CreateAccount>(message.Content.ToString());

                        int id = content.StrategyID;

                        AQI.AQILabs.Kernel.Strategy s = AQI.AQILabs.Kernel.Instrument.FindInstrument(id) as AQI.AQILabs.Kernel.Strategy;
                        if (s != null)
                        {
                            QuantApp.Kernel.User user = QuantApp.Kernel.User.FindUser(content.UserID);
                            QuantApp.Kernel.User attorney = content.AttorneyID == null ? null : QuantApp.Kernel.User.FindUser(content.AttorneyID);
                            if (user != null && attorney != null)
                            {
                                List<PALMPending> pendings = PALM.GetPending(user);
                                foreach (PALMPending pending in pendings)
                                {
                                    if (pending.AccountID == content.AccountID)
                                    {

                                        pending.Strategy = s;
                                        pending.CreationDate = s.CreateTime;
                                        pending.Attorney = attorney;
                                        PALM.UpdatePending(pending);
                                        PALM.AddStrategy(pending.User, pending.Attorney, s);
                                    }
                                }
                            }
                        }

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Create Account Exception: " + e);
                    }
                }
                else if ((int)message.Type == (int)AQI.AQILabs.Kernel.RTDMessage.MessageType.UpdateOrder)
                {
                    try
                    {
                        AQI.AQILabs.Kernel.RTDMessage.OrderMessage content = JsonConvert.DeserializeObject<AQI.AQILabs.Kernel.RTDMessage.OrderMessage>(message.Content.ToString());

                        content.Order.Portfolio.UpdateOrder(content.Order, content.OnlyMemory, false);

                        return Tuple.Create(content.Order.Portfolio.ID.ToString(), message_string);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("UpdateOrder Exception: " + e);
                    }
                }
                else if ((int)message.Type == (int)AQI.AQILabs.Kernel.RTDMessage.MessageType.UpdatePosition)
                {
                    try
                    {
                        AQI.AQILabs.Kernel.RTDMessage.PositionMessage content = JsonConvert.DeserializeObject<AQI.AQILabs.Kernel.RTDMessage.PositionMessage>(message.Content.ToString());

                        content.Position.Portfolio.UpdatePositionMemory(content.Position, content.Timestamp, content.AddNew, true, false);

                        return Tuple.Create(content.Position.Portfolio.ID.ToString(), message_string);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("UpdatePosition Exception: " + e + " --- " + e.StackTrace);
                    }
                }
                else if ((int)message.Type == (int)AQI.AQILabs.Kernel.RTDMessage.MessageType.AddNewOrder)
                {
                    try
                    {
                        AQI.AQILabs.Kernel.Order content = JsonConvert.DeserializeObject<AQI.AQILabs.Kernel.Order>(message.Content.ToString());

                        content.Portfolio.AddOrderMemory(content);

                        return Tuple.Create(content.Portfolio.ID.ToString(), message_string);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("AddNewOrder Exception: " + e);
                    }
                }
                else if ((int)message.Type == (int)AQI.AQILabs.Kernel.RTDMessage.MessageType.AddNewPosition)
                {
                    try
                    {
                        AQI.AQILabs.Kernel.Position content = JsonConvert.DeserializeObject<AQI.AQILabs.Kernel.Position>(message.Content.ToString());

                        content.Portfolio.UpdatePositionMemory(content, content.Timestamp, true, true, false);

                        return Tuple.Create(content.Portfolio.ID.ToString(), message_string);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("AddNewPosition Exception: " + e + " " + e.StackTrace);
                    }
                }
                else if ((int)message.Type == (int)AQI.AQILabs.Kernel.RTDMessage.MessageType.SavePortfolio)
                {
                    try
                    {
                        int content = JsonConvert.DeserializeObject<int>(message.Content.ToString());

                        (AQI.AQILabs.Kernel.Instrument.FindInstrument(content) as AQI.AQILabs.Kernel.Portfolio).SaveNewPositions();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
                else if ((int)message.Type == (int)AQI.AQILabs.Kernel.RTDMessage.MessageType.Property)
                {
                    try
                    {
                        AQI.AQILabs.Kernel.RTDMessage.PropertyMessage content = JsonConvert.DeserializeObject<AQI.AQILabs.Kernel.RTDMessage.PropertyMessage>(message.Content.ToString());

                        object obj = AQI.AQILabs.Kernel.Instrument.FindInstrument(content.ID);

                        PropertyInfo prop = obj.GetType().GetProperty(content.Name, BindingFlags.Public | BindingFlags.Instance);
                        if (null != prop && prop.CanWrite)
                        {
                            if (content.Value.GetType() == typeof(Int64))
                                prop.SetValue(obj, Convert.ToInt32(content.Value), null);
                            else
                                prop.SetValue(obj, content.Value, null);
                        }


                        AQI.AQILabs.Kernel.Instrument instrument = obj as AQI.AQILabs.Kernel.Instrument;
                        return Tuple.Create(instrument.ID.ToString(), message_string);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
                else if ((int)message.Type == (int)AQI.AQILabs.Kernel.RTDMessage.MessageType.Function)
                {
                    try
                    {
                        AQI.AQILabs.Kernel.RTDMessage.FunctionMessage content = JsonConvert.DeserializeObject<AQI.AQILabs.Kernel.RTDMessage.FunctionMessage>(message.Content.ToString());
                        object obj = AQI.AQILabs.Kernel.Instrument.FindInstrument(content.ID);

                        MethodInfo method = obj.GetType().GetMethod(content.Name);
                        if (null != method)
                            method.Invoke(obj, content.Parameters);

                        AQI.AQILabs.Kernel.Instrument instrument = obj as AQI.AQILabs.Kernel.Instrument;

                        return Tuple.Create(instrument.ID.ToString(), message_string);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
                return null;
            });
        }

        private static void Databases(string sqliteFile)
        {
            string KernelConnectString = "Data Source=" + sqliteFile;
            bool dbExists = File.Exists(sqliteFile);

            string CloudAppConnectString = KernelConnectString;
            string StrategyConnectString = KernelConnectString;

            if(!dbExists)
            {
                SQLiteDataSetAdapter KernelDataAdapter = new SQLiteDataSetAdapter();
                KernelDataAdapter.ConnectString = KernelConnectString;
                QuantApp.Kernel.Database.DB.Add("Kernel", KernelDataAdapter);
                Console.WriteLine("Creating table structure in: " + sqliteFile);
                var schema = File.ReadAllText(@"sql/create.sql");
                QuantApp.Kernel.Database.DB["Kernel"].ExecuteCommand(schema);
                var quant = File.ReadAllText(@"sql/quant.sql");
                QuantApp.Kernel.Database.DB["Kernel"].ExecuteCommand(quant);
                Console.WriteLine("Adding calendar data in: " + sqliteFile);
                var calendars = File.ReadAllText(@"sql/calendars.sql");
                QuantApp.Kernel.Database.DB["Kernel"].ExecuteCommand(calendars);
                Console.WriteLine("Adding fixed income and currency data in: " + sqliteFile);
                var fic = File.ReadAllText(@"sql/fic.sql");
                QuantApp.Kernel.Database.DB["Kernel"].ExecuteCommand(fic);
            }

            Databases(KernelConnectString, StrategyConnectString, CloudAppConnectString);
        }
        private static void Databases(string KernelConnectString, string StrategyConnectString, string CloudAppConnectString)
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


                // Quant
                Calendar.Factory = new AQI.AQILabs.Kernel.Adapters.SQL.Factories.SQLCalendarFactory();
                Currency.Factory = new AQI.AQILabs.Kernel.Adapters.SQL.Factories.SQLCurrencyFactory();
                CurrencyPair.Factory = new AQI.AQILabs.Kernel.Adapters.SQL.Factories.SQLCurrencyPairFactory();
                DataProvider.Factory = new AQI.AQILabs.Kernel.Adapters.SQL.Factories.SQLDataProviderFactory();
                Exchange.Factory = new AQI.AQILabs.Kernel.Adapters.SQL.Factories.SQLExchangeFactory();

                Instrument.Factory = new AQI.AQILabs.Kernel.Adapters.SQL.Factories.SQLInstrumentFactory();
                Security.Factory = new AQI.AQILabs.Kernel.Adapters.SQL.Factories.SQLSecurityFactory();
                Future.Factory = new AQI.AQILabs.Kernel.Adapters.SQL.Factories.SQLFutureFactory();
                Portfolio.Factory = new AQI.AQILabs.Kernel.Adapters.SQL.Factories.SQLPortfolioFactory();
                Strategy.Factory = new AQI.AQILabs.Kernel.Adapters.SQL.Factories.SQLStrategyFactory();
                Market.Factory = new AQI.AQILabs.Kernel.Adapters.SQL.Factories.SQLMarketFactory();

                InterestRate.Factory = new AQI.AQILabs.Kernel.Adapters.SQL.Factories.SQLInterestRateFactory();
                Deposit.Factory = new AQI.AQILabs.Kernel.Adapters.SQL.Factories.SQLDepositFactory();
                InterestRateSwap.Factory = new AQI.AQILabs.Kernel.Adapters.SQL.Factories.SQLInterestRateSwapFactory();

                DataProvider.DefaultProvider = DataProvider.FindDataProvider("AQI");

            }

            if (!QuantApp.Kernel.Database.DB.ContainsKey("CloudApp"))
            {
                if(CloudAppConnectString.StartsWith("Server="))
                {
                    if(KernelConnectString == CloudAppConnectString)
                        QuantApp.Kernel.Database.DB.Add("CloudApp", QuantApp.Kernel.Database.DB["Kernel"]);
                    else
                    {
                        MSSQLDataSetAdapter CloudAppDataAdapter = new MSSQLDataSetAdapter();
                        CloudAppDataAdapter.ConnectString = CloudAppConnectString;
                        QuantApp.Kernel.Database.DB.Add("CloudApp", CloudAppDataAdapter);
                    }
                }
                else
                {
                    if(KernelConnectString == CloudAppConnectString)
                        QuantApp.Kernel.Database.DB.Add("CloudApp", QuantApp.Kernel.Database.DB["Kernel"]);
                    else
                    {
                        SQLiteDataSetAdapter CloudAppDataAdapter = new SQLiteDataSetAdapter();
                        CloudAppDataAdapter.ConnectString = CloudAppConnectString;
                        QuantApp.Kernel.Database.DB.Add("CloudApp", CloudAppDataAdapter);
                    }
                }

                QuantApp.Kernel.User.Factory = new QuantApp.Kernel.Adapters.SQL.Factories.SQLUserFactory();
                Group.Factory = new QuantApp.Kernel.Adapters.SQL.Factories.SQLGroupFactory();
            }

            if (!QuantApp.Kernel.Database.DB.ContainsKey("DefaultStrategy"))
            {
                if(CloudAppConnectString.StartsWith("Server="))
                {
                    if(KernelConnectString == StrategyConnectString)
                        QuantApp.Kernel.Database.DB.Add("DefaultStrategy", QuantApp.Kernel.Database.DB["Kernel"]);
                    else
                    {
                        MSSQLDataSetAdapter StrategyDataAdapter = new MSSQLDataSetAdapter();
                        StrategyDataAdapter.ConnectString = StrategyConnectString;
                        QuantApp.Kernel.Database.DB.Add("DefaultStrategy", StrategyDataAdapter);
                    }
                }
                else
                {
                    if(KernelConnectString == StrategyConnectString)
                        QuantApp.Kernel.Database.DB.Add("DefaultStrategy", QuantApp.Kernel.Database.DB["Kernel"]);
                    else
                    {
                        SQLiteDataSetAdapter StrategyDataAdapter = new SQLiteDataSetAdapter();
                        StrategyDataAdapter.ConnectString = StrategyConnectString;
                        QuantApp.Kernel.Database.DB.Add("DefaultStrategy", StrategyDataAdapter);
                    }
                }
            }
        }

        public static IEnumerable<Workflow> SetDefaultWorkflows(string[] ids, bool saveToDisk, bool startJupyter)
        {
            return CoFlows.Server.Program.SetDefaultWorkflows(ids, saveToDisk, startJupyter);
        }


        public static IEnumerable<Workflow> GetDefaultWorkflows()
        {
            return CoFlows.Server.Program.GetDefaultWorkflows();
        }

        public static void AddServicedWorkflows(string id)
        {
            CoFlows.Server.Program.AddServicedWorkflows(id);
        }

        public static IEnumerable<string> GetServicedWorkflows()
        {
            return CoFlows.Server.Program.GetServicedWorkflows();
        }
    }
}
