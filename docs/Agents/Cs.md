C# Agent
===
This is a generic example of C# agent following the generic structure within **CoFlows**.

    using System;
    using System.Collections.Generic;
    using QuantApp.Engine;
    using QuantApp.Kernel;
    using Newtonsoft.Json.Linq;

    public class Agent
    {
        private static string workspaceID = "$WID$";
        public static FPKG pkg()
        {
            return new FPKG(
                workspaceID + "-Agent", //ID 1
                workspaceID, //Workflow ID  
                "C# Agent", //Name
                "C# Agent", //Description
                null, //MID
                Utils.SetFunction("Load", new Load((object[] data) => { })), 
                Utils.SetFunction("Add", new MCallback((string id, object data) => { })), 
                Utils.SetFunction("Exchange", new MCallback((string id, object data) => { })), 
                Utils.SetFunction("Remove", new MCallback((string id, object data) => { })), 
                Utils.SetFunction("Body", new Body((object data) => {
                    
                    var cmd = JObject.Parse(data.ToString());
                    if(cmd.ContainsKey("Data") && cmd["Data"].ToString() == "Initial Execution")
                        Console.WriteLine("     Agent Initial Execute @ " + DateTime.Now);

                    return data; 
                    })), 

                "0 * * ? * *", //Cron Schedule
                Utils.SetFunction("Job", new Job((DateTime date, string command) => { }))
                );
        }
    }