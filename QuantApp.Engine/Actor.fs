(*
 * The MIT License (MIT)
 * Copyright (c) Arturo Rodriguez All rights reserved.
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 *)

namespace QuantApp.Engine

open System

open Akka.Actor
open Akka.Routing
open Akka.Configuration
open Akka.Cluster
open Akka.Cluster.Tools.Singleton
open Akka.Cluster.Sharding
open Akka.Persistence
open Akka.FSharp

open Akkling
open Akkling.Persistence
open Akkling.Cluster
open Akkling.Cluster.Sharding
open Hyperion

/// <summary>
/// Utility module for akka.net functionality
/// </summary>
module Actor =
    let getSystem() = QuantApp.Kernel.Actor.getSystem()

    let selectOrSpawn system (refName : string) body =
        try   
            (select system $"/user/{refName}").ResolveOne(TimeSpan.FromSeconds(1.)) |> Async.RunSynchronously
        with
        | _ as e ->
            let lastIdx = refName.LastIndexOf("/")
            let refName = if lastIdx > 0 then refName.Substring(lastIdx + 1) else refName
            spawn system refName <| props(body)
        |> retype

    let initWorkers<'A,'B> (refName :  string) (func : 'A -> 'B) =
        let system = QuantApp.Kernel.Actor.getSystem()

        selectOrSpawn system $"{refName}"
        <| fun (ctx : Actor<obj>) -> 
            let rec loop (state : Map<string, IActorRef<obj>>) = actor {
                let! msg = ctx.Receive ()
                let sender = ctx.Sender()
                match msg with
                | :? LifecycleEvent as e ->
                    match e with
                    | PreStart -> ()//printfn "packageActor %A has started" ctx.Self
                    | PostStop -> ()//printfn "packageActor %A has stopped" ctx.Self
                    | _ -> return Unhandled
                
                | :? (string*'B) as pkg->
                    let key, value = pkg
                    
                    let state = 
                        if key |> state.ContainsKey then
                            let respondTo = state.[key]
                            respondTo <! box value
                            state.Remove(key)
                        else
                            state

                    return! loop state

                | :? 'A as payload ->
                    let uid = System.Guid.NewGuid().ToString()

                    let state = state.Add(uid, sender)

                    let worker = 
                        selectOrSpawn ctx $"{refName}/workerActor-{uid}"
                        <| fun (ctx : Actor<obj>) -> 
                            let rec loop () = actor {
                                let! msg = ctx.Receive ()
                                let sender = ctx.Sender()
                                match msg with
                                | :? LifecycleEvent as e ->
                                    match e with
                                    | PreStart -> ()//printfn "packageActor %A has started" ctx.Self
                                    | PostStop -> ()//printfn "packageActor %A has stopped" ctx.Self
                                    | _ -> return Unhandled

                                | :? (string*'A) as payload ->
                                    let key, value = payload
                                    let pkg = value |> func
                                    sender <! (key, pkg)
                                    return Stop
                            }
                            loop ()

                    worker <! (uid, payload)

                    return! loop state
            }

            loop Map.empty


    let callWorkers<'A,'B> (refName : string) (pkg : 'A) : 'B=
        let system = QuantApp.Kernel.Actor.getSystem()
        let actorRef = select system $"/user/{refName}"

        actorRef <? pkg
        |> Async.RunSynchronously