F# Agent
===
This is a generic example of F# agent following the generic structure within **CoFlows**.


    module Agent
    
    open System
    open System.Collections.Generic
    open Newtonsoft.Json.Linq

    open QuantApp.Kernel
    open QuantApp.Engine

    let workspaceID = "$WID$"
    let pkg(): FPKG =
        {
            ID = workspaceID + "-Agent" |> Some
            WorkflowID = workspaceID |> Some
            Code = None
            Name = "F# Agent"
            Description = "F# Agent" |> Some

            MID = None //MID
            Load = (fun data -> ()) |> Utils.Load("$ID$-Load") |> Some
            Add = (fun id data -> ()) |> Utils.Callback("$ID$-Add") |> Some
            Exchange = (fun id data -> ()) |> Utils.Callback("$ID$-Exchange") |> Some
            Remove = (fun id data -> ()) |> Utils.Callback("$ID$-Remove") |> Some

            Body = (fun data -> 
                let cmd = JObject.Parse(data.ToString())
                if cmd.ContainsKey("Data") && cmd.["Data"].ToString() = "Initial Execution" then
                    Console.WriteLine("     Agent Initial Execute @ " + DateTime.Now.ToString())

                data
                ) |> Utils.Body("$ID$-Body") |> Some

            ScheduleCommand = "0 * * ? * *" |> Some
            Job = (fun date execType -> ()) |> Utils.Job("$ID$-Job") |> Some
        }
        |> F.ToFPKG
