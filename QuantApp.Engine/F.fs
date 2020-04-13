(*
 * The MIT License (MIT)
 * Copyright (c) Arturo Rodriguez All rights reserved.
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 *)

namespace QuantApp.Engine

open System
open QuantApp.Kernel

type public F_v01 =
    {
        ID : string option
        WorkflowID : string option

        Code : (string * string) list option
        Name : string
        Description : string option
        MID : string option

        Load : string option
        Add : string option
        Exchange : string option
        Remove : string option
        Body : string option

        ScheduleCommand : string option
        Job : string option
    }

type FMeta =
    {
        ID : string
        WorkflowID : string
        Code : (string * string) seq
        Name : string
        Description : string
        MID : string
        Load : string
        Add : string
        Exchange : string
        Remove : string
        Body : string
        Started : bool

        ScheduleCommand : string
        Job : string
    }

type FunctionType =
    {
        Function : string
        Data : string
    }

type F = 
    val ID : string
    val mutable private _workflowID : string
    val mutable private _name : string
    val mutable private _description : string
    val mutable private _mid : string
    val mutable private _scheduleCommand : string

    val mutable private _jobExecutor : FuncJobExecutor
    val mutable private _jobFunction : Job
    val mutable private _loadFunction : Load
    val mutable private _bodyFunction : Body
    val mutable private _addFunction : MCallback
    val mutable private _exchangeFunction : MCallback
    val mutable private _removeFunction : MCallback
    val mutable private _scriptCode : (string * string) list
    val mutable private _jobFunctionName : string
    val mutable private _loadFunctionName : string 
    val mutable private _bodyFunctionName : string 
    val mutable private _addFunctionName : string 
    val mutable private _exchangeFunctionName : string 
    val mutable private _removeFunctionName : string
    val mutable private _initialized : bool
    val mutable private _started : bool

    
    val mutable private monitorScriptCode : obj
    val mutable private monitorName : obj
    val mutable private monitorDescription : obj
    val mutable private monitorWorkflowID : obj
    val mutable private monitorMID : obj
    val mutable private monitorScheduleCommand : obj
    val mutable private monitorStarted : obj
    val mutable private monitorAddFunctionName : obj
    val mutable private monitorExchangeFunctionName : obj
    val mutable private monitorRemoveFunctionName : obj
    val mutable private monitorBodyFunctionName : obj
    val mutable private monitorLoadFunctionName : obj
    val mutable private monitorJobFunctionName : obj
    val mutable private monitorInitialize : obj

    /// <summary>
    /// Delegate function used to set the information ratio function
    /// </summary>
    member this.ScriptCode
        with get() : (string * string) list = this._scriptCode
        and set(value : (string * string) list) =
                lock (this.monitorScriptCode) (fun () ->
                    try
                        let fsi = System.Reflection.Assembly.Load("FSI-ASSEMBLY")
                        let fsi = if fsi |> isNull then System.Reflection.Assembly.Load("fsiAnyCpu") else fsi

                        if fsi |> isNull |> not then
                            fsi.GetTypes()
                            |> Seq.iter(fun t -> 
                                let name = 
                                    let n = t.ToString()
                                    if n.StartsWith("FSI_") then
                                        n.Substring(n.IndexOf(".") + 1)
                                    else
                                        n

                                if name |> M._systemAssemblies.ContainsKey |> not then
                                        M._systemAssemblies.TryAdd(name, fsi) |> ignore
                                        M._systemAssemblyNames.TryAdd(name, t.ToString()) |> ignore
                                else
                                    M._systemAssemblies.[name] <- fsi
                                    M._systemAssemblyNames.[name] <- t.ToString()
                                )
                        ()
                    with _ ->

                        let code = value
                        if Utils.CompileAll || Utils.ActiveWorkflowList |> Seq.filter(fun x -> x.ID = this.WorkflowID) |> Seq.isEmpty |> not then
                            
                            code 
                            |> List.map(fun (name, code) -> 
                                (
                                    name, 
                                    code
                                        //NetCore & Python
                                        .Replace("Utils.SetFunction(","Utils.SetFunction(\"" + this.ID + "-\" + ")

                                        //Java
                                        .Replace(".Invoke(\"SetFunction\",",".Invoke(\"SetFunction\",\"" + this.ID + "-\" + ")

                                        //Js
                                        .Replace("jsWrapper.Load(","jsWrapper.Load(\"" + this.ID + "-\" + ")
                                        .Replace("jsWrapper.Callback(","jsWrapper.Callback(\"" + this.ID + "-\" + ")
                                        .Replace("jsWrapper.Job(","jsWrapper.Job(\"" + this.ID + "-\" + ")
                                        .Replace("jsWrapper.Body(","jsWrapper.Body(\"" + this.ID + "-\" + ")
                                        .Replace("$WID$", this.WorkflowID)
                                        .Replace("$ID$", this.ID)
                                )
                            )
                            |> Utils.RegisterCode(false, true)
                            |> ignore
                        
                    if this.Initialized && this._scriptCode <> value then
                        this._scriptCode <- value
                        let m = this.ID + "-F-MetaData" |> M.Base

                        let pkg = 
                            { 
                                ID = this.ID
                                WorkflowID = this._workflowID
                                Code = this._scriptCode |> Seq.map(fun (name, code) -> (name, code.Replace(this._workflowID, "$WID$")))
                                Name = this._name
                                Description = this._description
                                MID = this._mid
                                
                                ScheduleCommand = this._scheduleCommand
                                Job = this._jobFunctionName
                                
                                Add = this._addFunctionName
                                Exchange = this._exchangeFunctionName
                                Remove = this._removeFunctionName

                                Load = this._loadFunctionName
                                Body = this._bodyFunctionName

                                Started = this._started
                            }

                        let res = m.[fun x-> M.V<string>(x,"ID") = this.ID]
                        if res.Count = 0 then pkg |> m.Add |> ignore else m.Exchange(res.[0], pkg)
                        m.Save()
                    else
                        this._scriptCode <- value |> List.map(fun (name, code) -> (name, code.Replace("$WID$", this._workflowID)))
                    )


        /// <summary>

    /// Delegate function used to set the add function
    /// </summary>
    member this.Name
        with get() : string = this._name
        and set(value) =
            lock (this.monitorName) (fun () ->
                if this.Initialized && this._name <> value then
                    this._name <-value
                    let m = this.ID + "-F-MetaData" |> M.Base

                    let pkg = 
                        { 
                            ID = this.ID
                            WorkflowID = this._workflowID
                            Code = this._scriptCode |> Seq.map(fun (name, code) -> (name, code.Replace(this._workflowID, "$WID$"))) |> Seq.map(fun (name, code) -> (name, code.Replace(this._workflowID, "$WID$")))
                            Name = this._name
                            Description = this._description
                            MID = this._mid
                            
                            ScheduleCommand = this._scheduleCommand
                            Job = this._jobFunctionName
                            
                            Add = this._addFunctionName
                            Exchange = this._exchangeFunctionName
                            Remove = this._removeFunctionName

                            Load = this._loadFunctionName
                            Body = this._bodyFunctionName

                            Started = this._started
                        }

                    let res = m.[fun x-> M.V<string>(x,"ID") = this.ID]

                    if res.Count = 0 then pkg |> m.Add |> ignore else m.Exchange(res.[0], pkg)
                    m.Save()
                else
                    this._name <- value
            )

    /// Delegate function used to set the add function
    /// </summary>
    member this.Description
        with get() : string = this._description
        and set(value) =
            lock (this.monitorDescription) (fun () ->
                

                if this.Initialized && this._description <> value then
                    this._description <- value
                    let m = this.ID + "-F-MetaData" |> M.Base

                    let pkg = 
                        { 
                            ID = this.ID
                            WorkflowID = this._workflowID
                            Code = this._scriptCode |> Seq.map(fun (name, code) -> (name, code.Replace(this._workflowID, "$WID$"))) |> Seq.map(fun (name, code) -> (name, code.Replace(this._workflowID, "$WID$")))
                            Name = this._name
                            Description = this._description
                            MID = this._mid
                            
                            ScheduleCommand = this._scheduleCommand
                            Job = this._jobFunctionName
                            
                            Add = this._addFunctionName
                            Exchange = this._exchangeFunctionName
                            Remove = this._removeFunctionName

                            Load = this._loadFunctionName
                            Body = this._bodyFunctionName

                            Started = this._started
                        }

                    let res = m.[fun x-> M.V<string>(x,"ID") = this.ID]
                    if res.Count = 0 then pkg |> m.Add |> ignore else m.Exchange(res.[0], pkg)
                    m.Save()
                else
                    this._description <- value
            )

    /// Delegate function used to set the add function
    /// </summary>
    member this.WorkflowID
        with get() : string = this._workflowID
        and set(value) =
            lock (this.monitorWorkflowID) (fun () ->
                

                if this.Initialized && this._workflowID <> value then
                    this._workflowID <- value
                    let m = this.ID + "-F-MetaData" |> M.Base

                    let pkg = 
                        { 
                            ID = this.ID
                            WorkflowID = this._workflowID
                            Code = this._scriptCode |> Seq.map(fun (name, code) -> (name, code.Replace(this._workflowID, "$WID$"))) |> Seq.map(fun (name, code) -> (name, code.Replace(this._workflowID, "$WID$")))
                            Name = this._name
                            Description = this._description
                            MID = this._mid
                            
                            ScheduleCommand = this._scheduleCommand
                            Job = this._jobFunctionName
                            
                            Add = this._addFunctionName
                            Exchange = this._exchangeFunctionName
                            Remove = this._removeFunctionName

                            Load = this._loadFunctionName
                            Body = this._bodyFunctionName

                            Started = this._started
                        }

                    let res = m.[fun x-> M.V<string>(x,"ID") = this.ID]
                    if res.Count = 0 then pkg |> m.Add |> ignore else m.Exchange(res.[0], pkg)
                    m.Save()
                else
                    this._workflowID <- value
            )
    /// Delegate function used to set the add function
    /// </summary>
    member this.MID
        with get() : string = this._mid
        and set(value) =
            lock (this.monitorMID) (fun () ->
                

                if this.Initialized && this._mid <> value then
                    this._mid <- value
                    let m = this.ID + "-F-MetaData" |> M.Base

                    let pkg = 
                        { 
                            ID = this.ID
                            WorkflowID = this._workflowID
                            Code = this._scriptCode |> Seq.map(fun (name, code) -> (name, code.Replace(this._workflowID, "$WID$")))
                            Name = this._name
                            Description = this._description
                            MID = this._mid
                            
                            ScheduleCommand = this._scheduleCommand
                            Job = this._jobFunctionName
                            
                            Add = this._addFunctionName
                            Exchange = this._exchangeFunctionName
                            Remove = this._removeFunctionName

                            Load = this._loadFunctionName
                            Body = this._bodyFunctionName

                            Started = this._started
                        }

                    let res = m.[fun x-> M.V<string>(x,"ID") = this.ID]
                    if res.Count = 0 then pkg |> m.Add |> ignore else m.Exchange(res.[0], pkg)
                    m.Save()
                else
                    this._mid <- value
            )

    /// Delegate function used to set the add function
    /// </summary>
    member this.ScheduleCommand
        with get() : string = this._scheduleCommand
        and set(value) =
            lock (this.monitorScheduleCommand) (fun () ->
                

                if this.Initialized && this._scheduleCommand <> value then
                    this._scheduleCommand <- value
                    let m = this.ID + "-F-MetaData" |> M.Base

                    let pkg = 
                        { 
                            ID = this.ID
                            WorkflowID = this._workflowID
                            Code = this._scriptCode |> Seq.map(fun (name, code) -> (name, code.Replace(this._workflowID, "$WID$"))) |> Seq.map(fun (name, code) -> (name, code.Replace(this._workflowID, "$WID$")))
                            Name = this._name
                            Description = this._description
                            MID = this._mid
                            
                            ScheduleCommand = this._scheduleCommand
                            Job = this._jobFunctionName
                            
                            Add = this._addFunctionName
                            Exchange = this._exchangeFunctionName
                            Remove = this._removeFunctionName

                            Load = this._loadFunctionName
                            Body = this._bodyFunctionName

                            Started = this._started
                        }

                    let res = m.[fun x-> M.V<string>(x,"ID") = this.ID]

                    if res.Count = 0 then pkg |> m.Add |> ignore else m.Exchange(res.[0], pkg)
                    m.Save()
                else
                    this._scheduleCommand <- value
            )

    member this.Started
        with get() : bool = this._started
        and set(value) =
            lock (this.monitorStarted) (fun () ->
                

                if this.Initialized && (this._started <> value) then
                    this._started <- value
                    let m = this.ID + "-F-MetaData" |> M.Base

                    let pkg = 
                        { 
                            ID = this.ID
                            WorkflowID = this._workflowID
                            Code = this._scriptCode |> Seq.map(fun (name, code) -> (name, code.Replace(this._workflowID, "$WID$"))) |> Seq.map(fun (name, code) -> (name, code.Replace(this._workflowID, "$WID$")))
                            Name = this._name
                            Description = this._description
                            MID = this._mid
                            
                            ScheduleCommand = this._scheduleCommand
                            Job = this._jobFunctionName
                            
                            Add = this._addFunctionName
                            Exchange = this._exchangeFunctionName
                            Remove = this._removeFunctionName

                            Load = this._loadFunctionName
                            Body = this._bodyFunctionName

                            Started = this._started
                        }

                    let res = m.[fun x-> M.V<string>(x,"ID") = this.ID]
                    if res.Count = 0 then pkg |> m.Add |> ignore else m.Exchange(res.[0], pkg)
                    m.Save()
                else
                    this._started <- value
            )

    member this.RemoteStop() =
        this._started <- false
        let m = this.ID + "-F-MetaData" |> M.Base

        let pkg = 
            { 
                ID = this.ID
                WorkflowID = this._workflowID
                Code = this._scriptCode |> Seq.map(fun (name, code) -> (name, code.Replace(this._workflowID, "$WID$"))) |> Seq.map(fun (name, code) -> (name, code.Replace(this._workflowID, "$WID$")))
                Name = this._name
                Description = this._description
                MID = this._mid
                
                ScheduleCommand = this._scheduleCommand
                Job = this._jobFunctionName
                
                Add = this._addFunctionName
                Exchange = this._exchangeFunctionName
                Remove = this._removeFunctionName

                Load = this._loadFunctionName
                Body = this._bodyFunctionName

                Started = this._started
            }

        let res = m.[fun x-> M.V<string>(x,"ID") = this.ID]
        if res.Count = 0 then pkg |> m.Add |> ignore else m.Exchange(res.[0], pkg)

        
    member this.Initialized
        with get() : bool = this._initialized
        and set(value) =
            this._initialized <- value

    /// <summary>
    /// Delegate function used to set the add function
    /// </summary>
    member this.AddFunctionName
        with get() : string = this._addFunctionName
        and set(value) =
            lock (this.monitorAddFunctionName) (fun () ->
                let _value = this.ID + "-" + value
                if not(isNull(Utils.GetFunction(_value))) then
                    this.AddFunction <- Utils.GetFunction(_value) :?> MCallback
                elif not(isNull(Utils.GetFunction(value))) then
                    this.AddFunction <- Utils.GetFunction(value) :?> MCallback
                
                
                if not(isNull(this.MID)) && not(isNull(this._addFunction)) then
                    let m_base = M.Base(this.MID)

                    m_base.RegisterAdd(this.ID, _value) |> ignore

                if this.Initialized && this._addFunctionName <> value then
                    this._addFunctionName <- value
                    let m = this.ID + "-F-MetaData" |> M.Base

                    let pkg = 
                        { 
                            ID = this.ID
                            WorkflowID = this._workflowID
                            Code = this._scriptCode |> Seq.map(fun (name, code) -> (name, code.Replace(this._workflowID, "$WID$"))) |> Seq.map(fun (name, code) -> (name, code.Replace(this._workflowID, "$WID$")))
                            Name = this._name
                            Description = this._description
                            MID = this._mid
                            
                            ScheduleCommand = this._scheduleCommand
                            Job = this._jobFunctionName
                            
                            Add = this._addFunctionName
                            Exchange = this._exchangeFunctionName
                            Remove = this._removeFunctionName

                            Load = this._loadFunctionName
                            Body = this._bodyFunctionName

                            Started = this._started
                        }

                    let res = m.[fun x-> M.V<string>(x,"ID") = this.ID]
                    if res.Count = 0 then pkg |> m.Add |> ignore else m.Exchange(res.[0], pkg)
                    m.Save()
                else
                    this._addFunctionName <- value
            )
            


    /// <summary>
    /// Delegate function used to set the exchange function
    /// </summary>
    member this.ExchangeFunctionName
        with get() : string = this._exchangeFunctionName
        and set(value) =
            lock (this.monitorExchangeFunctionName) (fun () ->
                let _value = this.ID + "-" + value
                if not(isNull(Utils.GetFunction(_value))) then
                    this.ExchangeFunction <- Utils.GetFunction(_value) :?> MCallback
                elif not(isNull(Utils.GetFunction(value))) then
                    this.ExchangeFunction <- Utils.GetFunction(value) :?> MCallback

                

                if not(isNull(this.MID)) && not(isNull(this._exchangeFunction)) then
                    let m_base = M.Base(this.MID)
                    m_base.RegisterExchange(this.ID, _value) |> ignore
                    

                if this.Initialized && this._exchangeFunctionName <> value then
                    this._exchangeFunctionName <- value
                    let m = this.ID + "-F-MetaData" |> M.Base

                    let pkg = 
                        { 
                            ID = this.ID
                            WorkflowID = this._workflowID
                            Code = this._scriptCode |> Seq.map(fun (name, code) -> (name, code.Replace(this._workflowID, "$WID$"))) |> Seq.map(fun (name, code) -> (name, code.Replace(this._workflowID, "$WID$")))
                            Name = this._name
                            Description = this._description
                            MID = this._mid
                            
                            ScheduleCommand = this._scheduleCommand
                            Job = this._jobFunctionName
                            
                            Add = this._addFunctionName
                            Exchange = this._exchangeFunctionName
                            Remove = this._removeFunctionName

                            Load = this._loadFunctionName
                            Body = this._bodyFunctionName

                            Started = this._started
                        }

                    let res = m.[fun x-> M.V<string>(x,"ID") = this.ID]
                    if res.Count = 0 then pkg |> m.Add |> ignore else m.Exchange(res.[0], pkg)
                    m.Save()
                else
                    this._exchangeFunctionName <- value
            )

    /// <summary>
    /// Delegate function used to set the exchange function
    /// </summary>
    member this.RemoveFunctionName
        with get() : string = this._removeFunctionName
        and set(value) =
            lock (this.monitorRemoveFunctionName) (fun () ->
                let _value = this.ID + "-" + value
                if not(isNull(Utils.GetFunction(_value))) then
                    this.RemoveFunction <- Utils.GetFunction(_value) :?> MCallback
                elif not(isNull(Utils.GetFunction(value))) then
                    this.RemoveFunction <- Utils.GetFunction(value) :?> MCallback

                

                if not(isNull(this.MID)) && not(isNull(this._removeFunction)) then
                    let m_base = M.Base(this.MID)
                    m_base.RegisterRemove(this.ID, _value) |> ignore


                if this.Initialized && this._removeFunctionName <> value then
                    this._removeFunctionName <- value
                    let m = this.ID + "-F-MetaData" |> M.Base

                    let pkg = 
                        { 
                            ID = this.ID
                            WorkflowID = this._workflowID
                            Code = this._scriptCode |> Seq.map(fun (name, code) -> (name, code.Replace(this._workflowID, "$WID$")))
                            Name = this._name
                            Description = this._description
                            MID = this._mid
                            
                            ScheduleCommand = this._scheduleCommand
                            Job = this._jobFunctionName
                            
                            Add = this._addFunctionName
                            Exchange = this._exchangeFunctionName
                            Remove = this._removeFunctionName

                            Load = this._loadFunctionName
                            Body = this._bodyFunctionName

                            Started = this._started
                        }

                    let res = m.[fun x-> M.V<string>(x,"ID") = this.ID]
                    if res.Count = 0 then pkg |> m.Add |> ignore else m.Exchange(res.[0], pkg)
                    m.Save()
                else
                    this._removeFunctionName <- value
            )

    /// <summary>
    /// Delegate function used to set the exchange function
    /// </summary>
    member this.BodyFunctionName
        with get() : string = this._bodyFunctionName
        and set(value) =
            lock (this.monitorBodyFunctionName) (fun () ->
                let _value = this.ID + "-" + value
                if not(isNull(Utils.GetFunction(_value))) then
                    this.BodyFunction <- Utils.GetFunction(_value) :?> Body
                elif not(isNull(Utils.GetFunction(value))) then
                    this.BodyFunction <- Utils.GetFunction(value) :?> Body

                

                if this.Initialized && this._bodyFunctionName <> value then
                    this._bodyFunctionName <- value
                    let m = this.ID + "-F-MetaData" |> M.Base

                    let pkg = 
                        { 
                            ID = this.ID
                            WorkflowID = this._workflowID
                            Code = this._scriptCode |> Seq.map(fun (name, code) -> (name, code.Replace(this._workflowID, "$WID$")))
                            Name = this._name
                            Description = this._description
                            MID = this._mid
                            
                            ScheduleCommand = this._scheduleCommand
                            Job = this._jobFunctionName
                            
                            Add = this._addFunctionName
                            Exchange = this._exchangeFunctionName
                            Remove = this._removeFunctionName

                            Load = this._loadFunctionName
                            Body = this._bodyFunctionName

                            Started = this._started
                        }

                    let res = m.[fun x-> M.V<string>(x,"ID") = this.ID]
                    if res.Count = 0 then pkg |> m.Add |> ignore else m.Exchange(res.[0], pkg)
                    m.Save()
                else
                    this._bodyFunctionName <- value
            )

    /// <summary>
    /// Delegate function used to set the exchange function
    /// </summary>
    member this.LoadFunctionName
        with get() : string = this._loadFunctionName
        and set(value) =
            lock (this.monitorLoadFunctionName) (fun () ->
                let _value = this.ID + "-" + value
                if not(isNull(Utils.GetFunction(_value))) then
                    this.LoadFunction <- Utils.GetFunction(_value) :?> Load
                elif not(isNull(Utils.GetFunction(value))) then
                    this.LoadFunction <- Utils.GetFunction(value) :?> Load

                

                if this.Initialized && this._loadFunctionName <> value then
                    this._loadFunctionName <- value
                    let m = this.ID + "-F-MetaData" |> M.Base

                    let pkg = 
                        { 
                            ID = this.ID
                            WorkflowID = this._workflowID
                            Code = this._scriptCode |> Seq.map(fun (name, code) -> (name, code.Replace(this._workflowID, "$WID$")))
                            Name = this._name
                            Description = this._description
                            MID = this._mid
                            
                            ScheduleCommand = this._scheduleCommand
                            Job = this._jobFunctionName
                            
                            Add = this._addFunctionName
                            Exchange = this._exchangeFunctionName
                            Remove = this._removeFunctionName

                            Load = this._loadFunctionName
                            Body = this._bodyFunctionName

                            Started = this._started
                        }

                    let res = m.[fun x-> M.V<string>(x,"ID") = this.ID]
                    if res.Count = 0 then pkg |> m.Add |> ignore else m.Exchange(res.[0], pkg)
                    m.Save()
                else
                    this._loadFunctionName <- value
            )

    /// <summary>
    /// Delegate function used to set the exchange function
    /// </summary>
    member this.JobFunctionName
        with get() : string = this._jobFunctionName
        and set(value) =
            lock (this.monitorJobFunctionName) (fun () ->
                let _value = this.ID + "-" + value
                if not(isNull(Utils.GetFunction(_value))) then
                    this.JobFunction <- Utils.GetFunction(_value) :?> Job
                elif not(isNull(Utils.GetFunction(value))) then
                    this.JobFunction <- Utils.GetFunction(value) :?> Job

                

                if this.Initialized && this._jobFunctionName <> value then
                    this._jobFunctionName <- value
                    let m = this.ID + "-F-MetaData" |> M.Base

                    let pkg = 
                        { 
                            ID = this.ID
                            WorkflowID = this._workflowID
                            Code = this._scriptCode |> Seq.map(fun (name, code) -> (name, code.Replace(this._workflowID, "$WID$")))
                            Name = this._name
                            Description = this._description
                            MID = this._mid

                            ScheduleCommand = this._scheduleCommand
                            Job = this._jobFunctionName
                            
                            Add = this._addFunctionName
                            Exchange = this._exchangeFunctionName
                            Remove = this._removeFunctionName

                            Load = this._loadFunctionName
                            Body = this._bodyFunctionName

                            Started = this._started
                        }

                    let res = m.[fun x-> M.V<string>(x,"ID") = this.ID]
                    if res.Count = 0 then pkg |> m.Add |> ignore else m.Exchange(res.[0], pkg)
                    m.Save()
                else
                    this._jobFunctionName <- value
            )


    member this.Start() =
        if not(String.IsNullOrWhiteSpace(this.ScheduleCommand)) && not(isNull(this.JobFunction)) then
            
            this._jobExecutor <- FuncJobExecutor(this.Name, Func<DateTime, string,int>(fun date key -> this.JobFunction.Invoke(date, key); 0))

            let commands = (if this.ScheduleCommand.Contains(";") then this.ScheduleCommand else (this.ScheduleCommand + ";")).Split(';')
            commands
            |> Array.filter(fun command -> not(String.IsNullOrWhiteSpace(command)))
            |> Array.iter(fun command ->
                this._jobExecutor.StartJob(command.Replace(";", ""))
                this.Started <- true
                )

    member this.Stop() =
        if not(String.IsNullOrWhiteSpace(this.ScheduleCommand)) && not(isNull(this.JobFunction)) && not(isNull(this._jobExecutor)) then
            this._jobExecutor.StopJob()
        this.Started <- false

    member this.ReStart() =
        this.Stop()
        this.Start()

    /// <summary>
    /// Constructor
    /// </summary> 
    new(ID : string) =
        { 
            ID = ID
            
            _workflowID = null
            _name = null
            _description = null
            _mid = null
            _scheduleCommand = null

            _addFunction = null
            _exchangeFunction = null
            _removeFunction = null
            _loadFunction = null
            _bodyFunction = null
            _jobFunction = null
            _jobExecutor = null

            _scriptCode = []
            
            _addFunctionName = null
            _exchangeFunctionName = null
            _removeFunctionName = null
            _loadFunctionName = null
            _bodyFunctionName = null
            _jobFunctionName = null

            _initialized = false
            _started = false

            monitorScriptCode = Object()
            monitorName = Object()
            monitorDescription = Object()
            monitorWorkflowID = Object()
            monitorMID = Object()
            monitorScheduleCommand = Object()
            monitorStarted = Object()
            monitorAddFunctionName = Object()
            monitorExchangeFunctionName = Object()
            monitorRemoveFunctionName = Object()
            monitorBodyFunctionName = Object()
            monitorLoadFunctionName = Object()
            monitorJobFunctionName = Object()
            monitorInitialize = Object()
        }

    /// <summary>
    /// Function: Initialize the strategy during runtime.
    /// </summary>
    member this.Initialize() =
        match this._initialized with
        | true -> ()
        | _ -> 
            lock (this.monitorInitialize) (fun () ->
                let m = this.ID + "-F-MetaData" |> M.Base

                let res = m.[fun x-> M.V<string>(x,"ID") = this.ID]
                if res.Count <> 0 then
                    try
                        let pkg = res.[0] :?> FMeta
                        
                        if not(String.IsNullOrWhiteSpace(pkg.Name)) then
                            this._name <- pkg.Name

                        if not(String.IsNullOrWhiteSpace(pkg.Description)) then
                            this._description <- pkg.Description

                        if not(String.IsNullOrWhiteSpace(pkg.ScheduleCommand)) then
                            this._scheduleCommand <- pkg.ScheduleCommand


                        if not(String.IsNullOrWhiteSpace(pkg.WorkflowID)) then
                            this._workflowID <- pkg.WorkflowID

                        this.ScriptCode <- 
                            pkg.Code 
                            |> Seq.map(fun (name, code) -> (name, code.Replace("$WID$", pkg.WorkflowID))) 
                            |> Seq.filter(fun (name, code) -> code |> String.IsNullOrWhiteSpace |> not) 
                            |> Seq.toList

                        if not(String.IsNullOrWhiteSpace(pkg.MID)) then
                            this._mid <- pkg.MID
                            let m_base = M.Base(this._mid)
                            if not(isNull(this._loadFunction)) then
                                let res = m_base.[fun x -> true]
                                res |> Seq.toArray  |> (this._loadFunction.Invoke) |> ignore
                        else
                            if this._loadFunction |> isNull |> not then
                                null  |> (this._loadFunction.Invoke) |> ignore


                        if not(String.IsNullOrWhiteSpace(pkg.Add)) then
                            this._addFunctionName <- pkg.Add
                            this.AddFunctionName <- pkg.Add

                        if not(String.IsNullOrWhiteSpace(pkg.Exchange)) then
                            this._exchangeFunctionName <- pkg.Exchange
                            this.ExchangeFunctionName <- pkg.Exchange

                        if not(String.IsNullOrWhiteSpace(pkg.Remove)) then
                            this._removeFunctionName <- pkg.Remove
                            this.RemoveFunctionName <- pkg.Remove

                        if not(String.IsNullOrWhiteSpace(pkg.Load)) then
                            this._loadFunctionName <- pkg.Load
                            this.LoadFunctionName <- pkg.Load

                        if not(String.IsNullOrWhiteSpace(pkg.Body)) then
                            this._bodyFunctionName <- pkg.Body
                            this.BodyFunctionName <- pkg.Body

                        if not(String.IsNullOrWhiteSpace(pkg.Job)) then
                            this._jobFunctionName <- pkg.Job
                            this.JobFunctionName <- pkg.Job

                        this.Initialized <- true
                        
                    with
                    | ex -> ex.ToString() |> Console.WriteLine |> ignore
                else
                    this.Initialized <- true
            )

    /// <summary>
    /// Function: called by when the underlying M entry has an added object
    /// </summary>
    member this.Add(key : string, data : obj) = 
        if this.AddFunction = null |> not then
            this.AddFunction.Invoke(key, data) |> ignore
    
    /// <summary>
    /// Delegate function used to set the add function
    /// </summary>
    member this.AddFunction
        with get() : MCallback = if isNull(this.AddFunctionName) || Utils.GetFunction(this.ID + "-" + this.AddFunctionName) = null then this._addFunction else Utils.GetFunction(this.ID + "-" + this.AddFunctionName) :?> MCallback
        and set(value : MCallback) = this._addFunction <- value 

    
    /// <summary>
    /// Function: called by when the underlying M entry has an exchanged object
    /// </summary>
    member this.Exchange(key : string, data : obj) = 
        if not(this.ExchangeFunction = null) then
            this.ExchangeFunction.Invoke(key, data) |> ignore
    
    /// <summary>
    /// Delegate function used to set the exchange function
    /// </summary>
    member this.ExchangeFunction
        with get() : MCallback = if isNull(this.ExchangeFunctionName) || Utils.GetFunction(this.ID + "-" + this.ExchangeFunctionName) = null then this._exchangeFunction else Utils.GetFunction(this.ID + "-" + this.ExchangeFunctionName) :?> MCallback
        and set(value : MCallback) = this._exchangeFunction <- value

    /// <summary>
    /// Function: called by when the underlying M entry has an exchanged object
    /// </summary>
    member this.Remove(key : string, data : obj) = 
        if not(this.RemoveFunction = null) then
            this.RemoveFunction.Invoke(key, data) |> ignore
    
    /// <summary>
    /// Delegate function used to set the exchange function
    /// </summary>
    member this.RemoveFunction
        with get() : MCallback = if isNull(this.RemoveFunctionName) || Utils.GetFunction(this.ID + "-" + this.RemoveFunctionName) = null then this._removeFunction else Utils.GetFunction(this.ID + "-" + this.RemoveFunctionName) :?> MCallback
        and set(value : MCallback) = this._removeFunction <- value

    /// <summary>
    /// Function: called by when the underlying M entry has an exchanged object
    /// </summary>
    member this.Load(data : obj[]) = 
        if not(isNull(this.LoadFunction)) then
            this.LoadFunction.Invoke(data) |> ignore
    
    /// <summary>
    /// Delegate function used to set the exchange function
    /// </summary>
    member this.LoadFunction
        with get() : Load = if isNull(this.LoadFunctionName) || Utils.GetFunction(this.ID + "-" + this.LoadFunctionName) = null then this._loadFunction else Utils.GetFunction(this.ID + "-" + this.LoadFunctionName) :?> Load
        and set(value : Load) = this._loadFunction <- value


    /// <summary>
    /// Function: called by when the underlying M entry has an exchanged object
    /// </summary>
    member this.Job(date : DateTime, executionType : string) = 
        if not(isNull(this.JobFunction)) then
            this.JobFunction.Invoke(date, executionType) |> ignore
    
    /// <summary>
    /// Delegate function used to set the exchange function
    /// </summary>
    member this.JobFunction
        with get() : Job = if isNull(this.JobFunctionName) || Utils.GetFunction(this.ID + "-" + this.JobFunctionName) = null then this._jobFunction else Utils.GetFunction(this.ID + "-" + this.JobFunctionName) :?> Job
        and set(value : Job) = this._jobFunction <- value


    /// <summary>
    /// Function: called by when the underlying M entry has an exchanged object
    /// </summary>
    member this.Body(data : obj) = 
        if not(this.BodyFunction = null) then
            this.BodyFunction.Invoke(data)
        else
            null

    /// <summary>
    /// Delegate function used to set the exchange function
    /// </summary>
    member this.BodyFunction
        with get() : Body = if isNull(this.BodyFunctionName) || Utils.GetFunction(this.ID + "-" + this.BodyFunctionName) = null then this._bodyFunction else Utils.GetFunction(this.ID + "-" + this.BodyFunctionName) :?> Body
        and set(value : Body) = this._bodyFunction <- value
    
    
    static member public Find(id : string) =
        if Utils._fDB.ContainsKey(id) then
            Some(Utils._fDB.[id] :?> F)
        else
            let m = M.Base(id + "-F-MetaData")
            let res = m.[fun _ -> true]
            if res.Count > 0 then
                let f = F(id)
                f.Initialize()
                let added = Utils._fDB.TryAdd(id, f)
                Some(f)
            else
                None

    /// <summary>
    /// Function: Create a strategy based of a PortfolioStrategy Package
    /// </summary>    
    /// <param name="pkg">Package
    /// </param>
    static member public Create(pkg : F_v01, code : (string * string) list option) =

        let uid = 
            if pkg.ID.IsNone then
                System.Guid.NewGuid().ToString()
            else
                pkg.ID.Value

        let wid = 
            if pkg.WorkflowID.IsNone then
                "$WID$"
            else
                pkg.WorkflowID.Value



        let result =
            try
                let fsi = System.Reflection.Assembly.Load("FSI-ASSEMBLY")
                let fsi = if fsi |> isNull then System.Reflection.Assembly.Load("fsiAnyCpu") else fsi
                ""
            with _ ->
                if code.IsSome then
                    code.Value 
                    |> List.map(fun (name, code) -> 
                        (
                            name, 
                            code
                                //NetCore & Python
                                .Replace("Utils.SetFunction(","Utils.SetFunction(\"" + uid + "-\" + ")
                                //Java
                                .Replace(".Invoke(\"SetFunction\",",".Invoke(\"SetFunction\",\"" + uid + "-\" + ")
                                //Js
                                .Replace("jsWrapper.Load(","jsWrapper.Load(\"" + uid + "-\" + ")
                                .Replace("jsWrapper.Callback(","jsWrapper.Callback(\"" + uid + "-\" + ")
                                .Replace("jsWrapper.Job(","jsWrapper.Job(\"" + uid + "-\" + ")
                                .Replace("jsWrapper.Body(","jsWrapper.Body(\"" + uid + "-\" + ")
                                .Replace("$WID$", wid)
                                .Replace("$ID$", uid)
                        ))
                    |> Utils.RegisterCode(false, true)
                elif pkg.Code.IsSome then
                    pkg.Code.Value 
                    |> List.map(fun (name, code) -> 
                        (
                            name, 
                            code
                                //NetCore & Python
                                .Replace("Utils.SetFunction(","Utils.SetFunction(\"" + uid + "-\" + ")
                                //Java
                                .Replace(".Invoke(\"SetFunction\",",".Invoke(\"SetFunction\",\"" + uid + "-\" + ")
                                //Js
                                .Replace("jsWrapper.Load(","jsWrapper.SetLoad(\"" + uid + "-\" + ")
                                .Replace("jsWrapper.Callback(","jsWrapper.Callback(\"" + uid + "-\" + ")
                                .Replace("jsWrapper.Job(","jsWrapper.Job(\"" + uid + "-\" + ")
                                .Replace("jsWrapper.Body(","jsWrapper.Body(\"" + uid + "-\" + ")
                                .Replace("$WID$", wid)
                                .Replace("$ID$", uid)
                        ))
                    |> Utils.RegisterCode(false, true)
                else
                    ""

        let f = F(uid)

        Utils._fDB.[uid] <- f

        f.Initialize()
        
        if pkg.WorkflowID.IsSome then
            f.WorkflowID <- pkg.WorkflowID.Value

        let containsPackage (text : string) : bool =
            let lines = text.ToLower().Split([|"\r\n"; "\r"; "\n"|], StringSplitOptions.RemoveEmptyEntries)
            if lines |> Array.isEmpty then
                true
            else
                let contains =
                    lines
                    |> Array.map(fun line -> line.ToLower().TrimStart())
                    |> Array.map(fun line -> line.StartsWith("namespace ") || line.StartsWith("package ") || line.StartsWith("#py-") || line.StartsWith("//js-"))
                    |> Array.map(fun res -> if res then 1 else 0)
                    |> Array.max
                contains = 1
        
        if code.IsSome then
            f.ScriptCode <- 
                


                code.Value 
                |> List.filter(fun (_, x) -> x |> containsPackage |> not)

        elif pkg.Code.IsSome then
            f.ScriptCode <- 
                pkg.Code.Value 
                |> List.filter(fun (_, x) -> x |> containsPackage |> not)

        if pkg.Name |> isNull |> not then
            f.Name <- pkg.Name

        if pkg.Description.IsSome then
            f.Description <- pkg.Description.Value

        if pkg.MID.IsSome then
            f.MID <- pkg.MID.Value

        if pkg.Load.IsSome then
            f.LoadFunctionName <- pkg.Load.Value

        if pkg.Body.IsSome then
            f.BodyFunctionName <- pkg.Body.Value

        if pkg.Add.IsSome then
            f.AddFunctionName <- pkg.Add.Value

        if pkg.Exchange.IsSome then
            f.ExchangeFunctionName <- pkg.Exchange.Value

        if pkg.Remove.IsSome then
            f.RemoveFunctionName <- pkg.Remove.Value

        if pkg.ScheduleCommand.IsSome then
            f.ScheduleCommand <- pkg.ScheduleCommand.Value

        if pkg.Job.IsSome then
            f.JobFunctionName <- pkg.Job.Value

        (f, result)


    static member public Create(meta : FMeta) =
        let pkg =
            {
                ID = None
                WorkflowID = if meta.WorkflowID |> String.IsNullOrEmpty |> not then Some(meta.WorkflowID) else None

                Code = if meta.Code |> isNull || meta.Code |> Seq.isEmpty then None else Some(meta.Code |> Seq.toList)
                Name = meta.Name
                Description = if meta.Description |> String.IsNullOrEmpty |> not then Some(meta.Description) else None
                MID = if meta.MID |> String.IsNullOrEmpty |> not then Some(meta.MID) else None

                Load = if meta.Load |> String.IsNullOrEmpty |> not then Some(meta.Load) else None
                Add = if meta.Add |> String.IsNullOrEmpty |> not then Some(meta.Add) else None
                Exchange = if meta.Exchange |> String.IsNullOrEmpty |> not then Some(meta.Exchange) else None
                Remove = if meta.Remove |> String.IsNullOrEmpty |> not then Some(meta.Remove) else None
                Body = if meta.Body |> String.IsNullOrEmpty |> not then Some(meta.Body) else None

                ScheduleCommand = if meta.ScheduleCommand |> String.IsNullOrEmpty |> not then Some(meta.ScheduleCommand) else None
                Job = if meta.Job |> String.IsNullOrEmpty |> not then Some(meta.Job) else None
            }
        F.Create(pkg, pkg.Code)


    static member public CreatePKG(pkg : FPKG, code : (string*string) seq) =
        let pkg =
            {
                ID = if pkg.ID |> String.IsNullOrEmpty |> not then Some(pkg.ID) else None
                WorkflowID = if pkg.WorkflowID |> String.IsNullOrEmpty |> not then Some(pkg.WorkflowID) else None

                Code = if code |> isNull || code |> Seq.isEmpty then None else Some(code |> Seq.toList)
                Name = pkg.Name
                Description = if pkg.Description |> String.IsNullOrEmpty |> not then Some(pkg.Description) else None
                MID = if pkg.MID |> String.IsNullOrEmpty |> not then Some(pkg.MID) else None

                Load = if pkg.Load |> String.IsNullOrEmpty |> not then Some(pkg.Load) else None
                Add = if pkg.Add |> String.IsNullOrEmpty |> not then Some(pkg.Add) else None
                Exchange = if pkg.Exchange |> String.IsNullOrEmpty |> not then Some(pkg.Exchange) else None
                Remove = if pkg.Remove |> String.IsNullOrEmpty |> not then Some(pkg.Remove) else None
                Body = if pkg.Body |> String.IsNullOrEmpty |> not then Some(pkg.Body) else None

                ScheduleCommand = if pkg.ScheduleCommand |> String.IsNullOrEmpty |> not then Some(pkg.ScheduleCommand) else None
                Job = if pkg.Job |> String.IsNullOrEmpty |> not then Some(pkg.Job) else None
            }
        if pkg.ID.IsSome then
            if F.Find(pkg.ID.Value).IsSome then
                let f = F.Find(pkg.ID.Value).Value
                f.Stop()

            let newF = F.Create(pkg, pkg.Code)

            // (fst newF).Start()
            newF

        else
            F.Create(pkg, pkg.Code)

    static member ToFPKG (pkg : F_v01) =
        {
            ID = if pkg.ID.IsNone then null else pkg.ID.Value
            WorkflowID = if pkg.WorkflowID.IsNone then null else pkg.WorkflowID.Value
            Name = pkg.Name
            Description = if pkg.Description.IsNone then null else pkg.Description.Value
            MID = if pkg.MID.IsNone then null else pkg.MID.Value
            Load = if pkg.Load.IsNone then null else pkg.Load.Value
            Add = if pkg.Add.IsNone then null else pkg.Add.Value
            Exchange = if pkg.Exchange.IsNone then null else pkg.Exchange.Value
            Remove = if pkg.Remove.IsNone then null else pkg.Remove.Value
            Body = if pkg.Body.IsNone then null else pkg.Body.Value

            ScheduleCommand = if pkg.ScheduleCommand.IsNone then null else pkg.ScheduleCommand.Value
            Job = if pkg.Job.IsNone then null else pkg.Job.Value
        }
