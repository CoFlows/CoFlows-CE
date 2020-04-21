(*
 * The MIT License (MIT)
 * Copyright (c) Arturo Rodriguez All rights reserved.
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 *)

namespace QuantApp.Engine

open System
open System.IO
open QuantApp.Kernel

type CodeData =
    {    
        Name : string
        mutable ID : string
        Code : string
        WorkflowID : string
    }


type PKG_Base =
    {
        Name : string
        Content : string
    }

type PKG_Agent =
    {
        Name : string
        Content : string
        Exe : string
    }

type PKG_Query =
    {
        Name : string
        Content : string
        ID : string
    }

type Permission =
    {
        ID : string
        Permission : QuantApp.Kernel.AccessType
    }

type NuGetPackage =
    {
        ID : string
        Version : string
    }

type PipPackage =
    {
        ID : string
    }

type JarPackage =
    {
        Url : string
    }

type Resource =
    {
        Cpu : string //2 = vcpu 2, 0.5 = 50% of 1 vcpu
        Mem : string //50Gi = 50 Gb RAM, 200Mi = 200Mb RAM
    }

type VolumeMount =
    {
        MountPath : string //Path to mount the volume in the container
        Name : string //Name of volume
        SubPath : string
    }

type Container =
    {
        Type : string
        Request : Resource
        Limit : Resource
        VolumeMounts : seq<VolumeMount>
        Storage : string
    }


type FilePackage =
    {
        Name : string
        Content : string
    }

type PKG = 
    {
        ID : string
        Name : string
        Base : seq<PKG_Base>
        Agents : seq<PKG_Agent>
        Queries : seq<PKG_Query>
        Permissions: seq<Permission>
        NuGets : seq<NuGetPackage>
        Pips : seq<PipPackage>
        Jars : seq<JarPackage>
        Bins : seq<FilePackage>
        Files : seq<FilePackage>
        ReadMe : string
        Publisher : string
        PublishTimestamp : DateTime
        AutoDeploy : bool
        Container : Container
    }


type FPKG =
    {
        ID : string
        WorkflowID : string
        Name : string
        Description : string
        MID : string
        Load : string
        Add : string
        Exchange : string
        Remove : string
        Body : string

        ScheduleCommand : string
        Job : string
    }

type ExecuteCodeResult =
    {
        Result : (string * obj) list
        Compilation : string
    }

type BuildCode = delegate of (string * string) list -> string
type RegisterCode = delegate of bool * bool * (string * string) list -> string
type ExecuteCode = delegate of (string * string) list -> ExecuteCodeResult
type ExecuteCodeFunction = delegate of bool * (string * string) list * string * obj[] -> ExecuteCodeResult

type Workflow =
    {
        ID : string
        Name : string
        Strategies : int list
        Code : (string * string) list
        Agents : string list
        Permissions : Permission list
        NuGets : NuGetPackage list
        Pips : PipPackage list
        Jars : JarPackage list
        Bins : FilePackage list
        Files : FilePackage list
        ReadMe : string
        Publisher : string
        PublishTimestamp : DateTime
        AutoDeploy : bool
        Container : Container
    }


type Load = delegate of obj[] -> unit
type Body = delegate of obj -> obj
type Job = delegate of DateTime * string -> unit


/// <summary>
/// Utility module with a set of Agents used by all strategies in this namespace
/// </summary>
module Utils =

    let ActiveWorkflowList = System.Collections.Generic.List<Workflow>()
    let mutable CompileAll = false

    let mutable _registerCode : RegisterCode = null
    let mutable _buildCode : BuildCode = null
    
    let mutable _executeCode : ExecuteCode = null
    let mutable _executeCodeFunction : ExecuteCodeFunction = null
    let _fDB = System.Collections.Concurrent.ConcurrentDictionary<string, obj>()

    let SetRegisterCode(func : RegisterCode) =
        if _registerCode |> isNull then
            _registerCode <- func

    let SetBuildCode(func : BuildCode) =
        if _buildCode |> isNull then
            _buildCode <- func

    let SetExecuteCode(func : ExecuteCode) =
        if _executeCode |> isNull then
            _executeCode <- func


    let SetExecuteCodeFunction(func : ExecuteCodeFunction) =
        if _executeCodeFunction |> isNull then
            _executeCodeFunction <- func

    let SetFunction(name : string, func: obj) =
        if M._dic.ContainsKey(name) then
            M._dic.[name] <- func
        else
            M._dic.TryAdd(name, func) |> ignore
        name

    let PipeFunction (name : string) (func : obj) =
        if M._dic.ContainsKey(name) then
            M._dic.[name] <-  func
        else
            M._dic.TryAdd(name, func) |> ignore
        name

    let RegisterCode (saveDisk : bool, execute : bool) (code : (string * string) list) =
        if _registerCode |> isNull |> not then
            _registerCode.Invoke(saveDisk, execute, code)
        else
            null

    let BuildCode(code : (string * string) list) =
        if _buildCode |> isNull |> not then
            _buildCode.Invoke(code)
        else
            null

    let ExecuteCode(code : (string * string) seq) =
        if _executeCode |> isNull |> not then
            _executeCode.Invoke(code |> Seq.toList)
        else
            { Result = [("",null)]; Compilation = null }

    let ExecuteCodeFunction(saveDisk : bool, code : (string * string) seq, name : string, parameters : obj[]) =
        if _executeCodeFunction |> isNull |> not then
            _executeCodeFunction.Invoke(saveDisk, code |> Seq.toList, name, parameters)
        else
            { Result = [("",null)]; Compilation = null }

    let CreatePKG(code : (string * string) seq, name: string, parameters : obj[]) : FPKG * ((string * string) seq) = 
        let text, res = ExecuteCodeFunction(false, code, name, parameters).Result.[0]
        if res :? string then
            res |> Console.WriteLine
            raise(Exception(res.ToString()))
        else
            (res :?> FPKG), code


    let GetFunction(name : string) =
        if name |> isNull || M._dic.ContainsKey(name) |> not then
            null

        else
            M._dic.[name]

    let Load (name : string) func = Load(func) |> PipeFunction(name)
    let Body (name : string) func = Body(func) |> PipeFunction(name)
    let Job (name : string) func = Job(func) |> PipeFunction(name)
    let Callback (name : string) func = MCallback(func) |> PipeFunction(name)