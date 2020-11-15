(*
 * The MIT License (MIT)
 * Copyright (c) Arturo Rodriguez All rights reserved.
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 *)

module XXX
 
open System
open System.Collections.Generic
open Newtonsoft.Json.Linq

open QuantApp.Kernel
open QuantApp.Engine

let workspaceID = "$WID$"
let pkg(): FPKG =
    {
        ID = workspaceID + "-XXX" |> Some
        WorkflowID = workspaceID |> Some
        Code = None
        Name = "F# XXX Agent"
        Description = "F# XXX Agent" |> Some

        MID = None //MID
        Load = (fun data -> ()) |> Utils.Load("$ID$-Load") |> Some
        Add = (fun id data -> ()) |> Utils.Callback("$ID$-Add") |> Some
        Exchange = (fun id data -> ()) |> Utils.Callback("$ID$-Exchange") |> Some
        Remove = (fun id data -> ()) |> Utils.Callback("$ID$-Remove") |> Some

        Body = (fun data -> 
            let cmd = JObject.Parse(data.ToString())
            if cmd.ContainsKey("Data") && cmd.["Data"].ToString() = "Initial Execution" then
                Console.WriteLine("     XXX Initial Execute @ " + DateTime.Now.ToString())

            data
            ) |> Utils.Body("$ID$-Body") |> Some

        ScheduleCommand = "0 * * ? * *" |> Some
        Job = (fun date execType -> ()) |> Utils.Job("$ID$-Job") |> Some
    }
    |> F.ToFPKG