C# Agent
===
This is a generic example of C# agent following the generic structure within **CoFlows**.

    using System;
    using QuantApp.Engine;
    using QuantApp.Kernel;

    public class CSharpAgent
    {
        public class CSEntry
        {
            public string Name;
            public string DateStr;
        }
        private static string defaultID = "xxx";
        public static FPKG pkg()
        {
            return new FPKG(
                defaultID, //ID
                "Hello_World_Workflow", //Workflow ID  
                "Hello C# Agent", //Name
                "Hello C# Analytics Agent Sample", //Description
                "xxx-MID", //F# Listener
                Utils.SetFunction("Load", new Load((object[] data) =>
                    {
                        Console.WriteLine("C# Agent Load");
                    })), 
                Utils.SetFunction("Add", new MCallback((string id, object data) =>
                    {
                        Console.WriteLine("C# Agent Add: " + id + data.ToString());
                    })), 
                Utils.SetFunction("Exchange", new MCallback((string id, object data) =>
                    {
                        Console.WriteLine("C# Agent Exchange: " + id);
                    })), 
                Utils.SetFunction("Remove", new MCallback((string id, object data) =>
                    {
                        Console.WriteLine("C# Agent Remove: " + id);
                    })), 
                Utils.SetFunction("Body", new Body((object data) =>
                    {
                        Console.WriteLine("C# Agent Body " + data);
                        return data;
                    })), 

                "0 * * ? * *", //Cron Schedule
                Utils.SetFunction("Job", new Job((DateTime date, string command) =>
                    {
                        Console.WriteLine("C# Agent Job: " + date +  " --> " + command);
                    }))
                );
        }
    }