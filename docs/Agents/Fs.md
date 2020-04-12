F# Agent
===
This is a generic example of F# agent following the generic structure within **CoFlows**.

    module Hello_World_FSharp

    open System
    open System.Net
    open System.IO

    open QuantApp.Kernel
    open QuantApp.Engine

    let defaultID = "xxx"
    let pkg(): FPKG =
        {
            ID = Some(defaultID)
            WorkflowID = Some("Hello_World_Workflow")
            Code = None
            Name = "Hello F# Agent"
            Description = Some("Hello F# Analytics Agent Sample")

            MID = Some(defaultID + "-MID") //ID of MultiVerse entry which this functions is linked to
            Load = Some(Utils.SetFunction(
                    "Load", 
                    Load(fun data ->
                            "Loading data" |> Console.WriteLine
                        )
                    ))
            Add = Some(Utils.SetFunction(
                    "Add", 
                    MCallback(fun id data ->
                            "Adding: " + id + " | " + data.ToString()) |> Console.WriteLine
                            ()
                        )
                    ))
            Exchange = Some(Utils.SetFunction(
                    "Exchange", 
                    MCallback(fun id data ->
                            "Exchanging: " + id + " | " + data.ToString() |> Console.WriteLine
                            ()
                        )
                    ))
            Remove = Some(Utils.SetFunction(
                    "Remove", 
                    MCallback(fun id data ->
                            "Removing: " + id + " | " + data.ToString() |> Console.WriteLine
                            ()
                        )
                    ))

            Body = Some(Utils.SetFunction(
                    "Body", 
                    Body(fun data ->
                        let command = JsonConvert.DeserializeObject<FunctionType>(data.ToString())

                        match command.Function with
                        
                        | _ -> "No Function: " + command.ToString()  :> obj
                    )))

            ScheduleCommand = Some("0 * * ? * *")
            Job = Some(Utils.SetFunction(
                    "Job", 
                    Job(fun date execType ->
                        "F# Agent Job: " + date.ToString() + " --> " + execType.ToString() |> Console.WriteLine
                        )
                    )) 
        |> F.ToFPKG
