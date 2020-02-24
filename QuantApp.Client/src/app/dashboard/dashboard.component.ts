import { Component, ViewChild } from '@angular/core';
import { QuantAppComponent } from '../quantapp/core/quantapp.component';
import { QAWorkbookComponent } from '../quantapp/workbook/qaworkbook.component';

import { NgbTabset } from '@ng-bootstrap/ng-bootstrap';


@Component({
  selector: 'dashboard-component',
  
  templateUrl: './dashboard.component.html',
  styles: []
})
export class DashboardComponent {

    functions = []
    workbooks = []

    workspaces = []
    
    wid = ""
    permission = -1

    workspace = {
        Permissions: []
    }

    @ViewChild('qawbook')
    private qawbook:QAWorkbookComponent

    

    constructor(private quantapp: QuantAppComponent) {

        this.quantapp.Get("m/servicedworkspaces", data => {
            // console.log(data)
            this.workspaces = data

            this.workspaces.forEach(workspace => {
                let wiid = workspace.ID + "--Workbook"
                this.quantapp.LinkAction(workspace.ID,
                    data => { //Load
                        
                        workspace.workspace = data[0].Value
                        
                        workspace.workspace.Permissions.forEach(x => {
                            if(x.ID == this.quantapp.quser.User.Email) this.permission = x.Permission
                        })
    
                        workspace.functions = []
                        workspace.activeFunctions = []
                        workspace.inActiveFunctions = []
                        data[0].Value.Functions.forEach(x => {
    
                            let fid = x + "-F-MetaData";
                            // console.log(fid)
                            this.quantapp.LinkAction(fid,
                                data => { //Load
                                    // console.log(data)
                                    
                                    data.forEach(x => {
                                        workspace.functions.push(x.Value)
                                        if(x.Value.Started)
                                            workspace.activeFunctions.push(x.Value)
                                        else
                                            workspace.inActiveFunctions.push(x.Value)
                                    })
                                },
                                data => { //Add
                                    // console.log("add", data)
                                    workspace.functions.push(data)
                                    workspace.functions = [...  workspace.functions]

                                    workspace.activeFunctions = []
                                    workspace.inActiveFunctions = []
                                    workspace.functions.forEach(x => {
                                        if(x.Value.Started)
                                            workspace.activeFunctions.push(x.Value)
                                        else
                                            workspace.inActiveFunctions.push(x.Value)
                                    })
                                },
                                data => { //Exchange
                                    // console.log("exchange",data)
                                    let id = workspace.functions.findIndex(x => x.ID == data.Value.ID)
                                    
                                    if(id > -1){
                                        workspace.functions[id] = data.Value
                                    }
                                    workspace.functions = [...  workspace.functions]

                                    workspace.activeFunctions = []
                                    workspace.inActiveFunctions = []
                                    workspace.functions.forEach(x => {
                                        if(x.Value.Started)
                                            workspace.activeFunctions.push(x.Value)
                                        else
                                            workspace.inActiveFunctions.push(x.Value)
                                    })
                                    
                                },
                                data => { //Remove
                                    //console.log("exchange",data)
                                    let id = workspace.functions.findIndex(x => x.ID == data.Value.ID)
                                    
                                    if(id > -1){
                                        workspace.functions.splice(id, 1)
                                        //this.functions[id] = data
                                    }
                                    workspace.functions = [...  workspace.functions]

                                    workspace.activeFunctions = []
                                    workspace.inActiveFunctions = []
                                    workspace.functions.forEach(x => {
                                        if(x.Value.Started)
                                            workspace.activeFunctions.push(x.Value)
                                        else
                                            workspace.inActiveFunctions.push(x.Value)
                                    })
                                }
                            );
                        });
    
                        // this.qastrategies.SetStrategies(data[0].Value.Strategies)

                        // console.log(this.workspaces)
                        
                        this.quantapp.LinkAction(wiid,
                            data => { //Load
                                workspace.workbooks = data
                            },
                            data => { //Add
                                // console.log(data)
                            },
                            data => { //Exchange
    
                                // console.log('Exchange', data)
                                
                                let counter = -1
                                for(let i = 0; i < workspace.workbooks.length; i++){
                                    // console.log(this.workbooks[i])
                                    if(workspace.workbooks[i].Key == data.Key){                
                                        counter = i
                                    }
                                }
                                
                                if(counter > -1)
                                workspace.workbooks[counter] = data
                                // console.log(data)
                            },
                            data => { //Remove
                                // console.log(data)
    
                                let counter = -1
                                for(let i = 0; i < workspace.workbooks.length; i++){
                                    if(workspace.workbooks[i].Key == data.Key){
                                        counter = i
                                    }
                                }
                                if(counter > -1)
                                workspace.workbooks.splice(counter,1)
                            }
                        );
                    },
                    data => { //Add
                    },
                    data => { //Exchange
                        workspace.workspace = data.Value
    
                        workspace.functions = []
                        data.Value.Functions.forEach(x => {
    
                            let fid = x + "-F-MetaData";
                            this.quantapp.LinkAction(fid,
                                data => { //Load                                
                                    data.forEach(x => {
                                        workspace.functions.push(x.Value)
                                    })
                                },
                                data => { //Add
                                    workspace.functions.push(data)
                                    workspace.functions = [...  workspace.functions]
                                },
                                data => { //Exchange
                                    let id = workspace.functions.findIndex(x => x.ID = data.Value.ID)
                                    
                                    if(id > -1){
                                        workspace.functions[id] = data.Value
                                    }
                                    workspace.functions = [...  workspace.functions]
                                    
                                },
                                data => { //Remove
                                    let id = workspace.functions.findIndex(x => x.ID = data.Value.ID)
                                    
                                    if(id > -1){
                                        workspace.functions.splice(id, 1)
                                    }
                                    workspace.functions = [...  workspace.functions]
                                }
                            );
                        });
                        // this.qastrategies.SetStrategies(data.Value.Strategies)
                        this.quantapp.LinkAction(wiid,
                            data => { //Load
                                workspace.workbooks = data
                            },
                            data => { //Add
                            },
                            data => { //Exchange
                                let counter = -1
                                for(let i = 0; i < workspace.workbooks.length; i++){
                                    if(workspace.workbooks[i].Key == data.Key){
                                        counter = i
                                    }
                                }
                                
                                workspace.workbooks[counter] = data
                                console.log(data)
                            },
                            data => { //Remove
                                let counter = -1
                                for(let i = 0; i < workspace.workbooks.length; i++){
                                    if(workspace.workbooks[i].Key == data.Key){                
                                        counter = i
                                    }
                                }
                                if(counter > -1)
                                workspace.workbooks.splice(counter,1)
                            }
                        );
                    },
                    data => { //Remove
                    }
                )
            })

            // this.wid = data[0].ID//params['id'];

            // let wiid = this.wid + "--Workbook"

            // this.quantapp.LinkAction(this.wid,
            //     data => { //Load
                    
            //         this.workspace = data[0].Value
                    
            //         this.workspace.Permissions.forEach(x => {
            //             if(x.ID == this.quantapp.quser.User.Email) this.permission = x.Permission
            //         })
                    
                    

            //         this.functions = []
            //         data[0].Value.Functions.forEach(x => {

            //             let fid = x + "-F-MetaData";
            //             // console.log(fid)
            //             this.quantapp.LinkAction(fid,
            //                 data => { //Load
            //                     // console.log(data)
                                
            //                     data.forEach(x => {
            //                         this.functions.push(x.Value)
            //                     })
            //                 },
            //                 data => { //Add
            //                     // console.log("add", data)
            //                     this.functions.push(data)
            //                     this.functions = [...  this.functions]
            //                 },
            //                 data => { //Exchange
            //                     // console.log("exchange",data)
            //                     let id = this.functions.findIndex(x => x.ID == data.Value.ID)
                                
            //                     if(id > -1){
            //                         this.functions[id] = data.Value
            //                     }
            //                     this.functions = [...  this.functions]
                                
            //                 },
            //                 data => { //Remove
            //                     //console.log("exchange",data)
            //                     let id = this.functions.findIndex(x => x.ID == data.Value.ID)
                                
            //                     if(id > -1){
            //                         this.functions.splice(id, 1)
            //                         //this.functions[id] = data
            //                     }
            //                     this.functions = [...  this.functions]
            //                 }
            //             );
            //         });

            //         // this.qastrategies.SetStrategies(data[0].Value.Strategies)
                    
            //         this.quantapp.LinkAction(wiid,
            //             data => { //Load
            //                 this.workbooks = data
            //             },
            //             data => { //Add
            //                 // console.log(data)
            //             },
            //             data => { //Exchange

            //                 // console.log('Exchange', data)
                            
            //                 let counter = -1
            //                 for(let i = 0; i < this.workbooks.length; i++){
            //                     // console.log(this.workbooks[i])
            //                     if(this.workbooks[i].Key == data.Key){                
            //                         counter = i
            //                     }
            //                 }
                            
            //                 if(counter > -1)
            //                     this.workbooks[counter] = data
            //                 // console.log(data)
            //             },
            //             data => { //Remove
            //                 // console.log(data)

            //                 let counter = -1
            //                 for(let i = 0; i < this.workbooks.length; i++){
            //                     if(this.workbooks[i].Key == data.Key){
            //                         counter = i
            //                     }
            //                 }
            //                 if(counter > -1)
            //                     this.workbooks.splice(counter,1)
            //             }
            //         );
            //     },
            //     data => { //Add
            //     },
            //     data => { //Exchange
            //         this.workspace = data.Value

            //         this.functions = []
            //         data.Value.Functions.forEach(x => {

            //             let fid = x + "-F-MetaData";
            //             this.quantapp.LinkAction(fid,
            //                 data => { //Load                                
            //                     data.forEach(x => {
            //                         this.functions.push(x.Value)
            //                     })
            //                 },
            //                 data => { //Add
            //                     this.functions.push(data)
            //                     this.functions = [...  this.functions]
            //                 },
            //                 data => { //Exchange
            //                     let id = this.functions.findIndex(x => x.ID = data.Value.ID)
                                
            //                     if(id > -1){
            //                         this.functions[id] = data.Value
            //                     }
            //                     this.functions = [...  this.functions]
                                
            //                 },
            //                 data => { //Remove
            //                     let id = this.functions.findIndex(x => x.ID = data.Value.ID)
                                
            //                     if(id > -1){
            //                         this.functions.splice(id, 1)
            //                     }
            //                     this.functions = [...  this.functions]
            //                 }
            //             );
            //         });
            //         // this.qastrategies.SetStrategies(data.Value.Strategies)
            //         this.quantapp.LinkAction(wiid,
            //             data => { //Load
            //                 this.workbooks = data
            //             },
            //             data => { //Add
            //             },
            //             data => { //Exchange
            //                 let counter = -1
            //                 for(let i = 0; i < this.workbooks.length; i++){
            //                     if(this.workbooks[i].Key == data.Key){
            //                         counter = i
            //                     }
            //                 }
                            
            //                 this.workbooks[counter] = data
            //                 console.log(data)
            //             },
            //             data => { //Remove
            //                 let counter = -1
            //                 for(let i = 0; i < this.workbooks.length; i++){
            //                     if(this.workbooks[i].Key == data.Key){                
            //                         counter = i
            //                     }
            //                 }
            //                 if(counter > -1)
            //                     this.workbooks.splice(counter,1)
            //             }
            //         );
            //     },
            //     data => { //Remove
            //     }
            // )

        })
    }

    onChangeActiveFunction(id, item){     
        this.quantapp.Get('m/activetoggle?id=' + id ,
            data => {
                // console.log(data)
            })
    }

    tabBeforeChange(event){
        // console.log(event)
        if(event == 0)
            QuantAppComponent.UpdateInstruments = !(this.qawbook.status.indexOf('thinking...') == -1 && this.qawbook.results.length > 0)
        else
            QuantAppComponent.UpdateInstruments = false

    }

    @ViewChild('tabs')
    private tabs:NgbTabset;

    createNewFunctionFs(){
        let templateCode = {
            WorkspaceID: this.wid,
            ID: "",
            Name: "Hello F# Agent",
            Description: "Hello F# Analytics Agent Skeleton",
            MID: "",
            
    
            Load: "Load",
            Add: "Add",
            Exchange: "Exchange",
            Remove: "Remove",
            Body: "Body",
            
            Job: "Job",
            ScheduleCommand: "0 * * ? * *",
            Started: false,
    
            Code: [ 
    `module Hello_World_PKG
    
    open System
    open System.Net
    open System.IO
    
    open QuantApp.Kernel
    open QuantApp.Engine
    
    open Newtonsoft.Json
    
    type Result = { Name : string; Date : string }
    
    let master(): F_v01 =
        {
            ID = None
            WorkspaceID = Some("` + this.wid + `")
            Code = None
            Name = "Hello F# Agent"
            Description = Some("Hello F# Analytics Agent Skeleton")
    
            MID = None //ID of MultiVerse entry which this functions is linked to
            Load = Some(Utils.SetFunction(
                    "Load", 
                    Load(fun data ->
                            data
                            |> Array.map(fun x -> x)
                            |> ignore
                        )
                    ))
            Add = Some(Utils.SetFunction(
                    "Add", 
                    MCallback(fun id data ->
                            Console.WriteLine("Hello F# Adding: " + id + " | " + data.ToString())
                        )
                    ))
            Exchange = Some(Utils.SetFunction(
                    "Exchange", 
                    MCallback(fun id data ->
                            Console.WriteLine("Hello F# Exchanging: " + id + " | " + data.ToString())
                        )
                    ))
            Remove = Some(Utils.SetFunction(
                    "Remove", 
                    MCallback(fun id data ->
                            Console.WriteLine("Hello F# Removing: " + id + " | " + data.ToString())
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
                        Console.WriteLine("F# Agent Job: " + date.ToString() + execType.ToString())
                    ))) 
        }
                    
            ` 
            ]

        }
        console.log(templateCode)

        this.quantapp.Post('m/createf',
            templateCode
            ,
            //this.quantapp.Post('strategy/portfoliolist',[93437],
            data => {
                console.log(data)
            })
    }

    createNewFunctionPy(){
        let templateCode = {
            WorkspaceID: this.wid,
            ID: "",
            Name: "Hello Python Agent",
            Description: "Hello Python Analytics Agent Skeleton",
            MID: "",
            
    
            Load: "Load",
            Add: "Add",
            Exchange: "Exchange",
            Remove: "Remove",
            Body: "Body",
            
            Job: "Job",
            ScheduleCommand: "0 * * ? * *",
            Started: false,
    
            Code: [ 
    `import clr

import System
import QuantApp.Kernel as qak
import QuantApp.Engine as qae

import json

def Add(id, data):
    System.Console.WriteLine("Python ADD: " + str(id) + " --> " + str(data))
    
def Exchange(id, data):
    System.Console.WriteLine("Python Exchange: " + str(id) + " --> " + str(data))
    
def Remove(id, data):
    System.Console.WriteLine("Python Remove: " + str(id) + " --> " + str(data))
    
def Load(data):
    System.Console.WriteLine("Python Loading: " + str(data))
    
def Body(data):
    System.Console.WriteLine("Python Body: " + str(data))
    return data

def Job(timestamp, data):
    System.Console.WriteLine("Python Job: " + str(timestamp) + " --> " + str(data))

def pkg():
    return qae.FPKG(
    None, #ID
    "` + this.wid + `", #Workspace ID
    "Hello Python Agent", #Name
    "Hello Python Analytics Agent Sample", #Description
    None, #M ID Listener
    qae.Utils.SetFunction("Load", qae.Load(Load)), 
    qae.Utils.SetFunction("Add", qak.MCallback(Add)), 
    qae.Utils.SetFunction("Exchange", qak.MCallback(Exchange)), 
    qae.Utils.SetFunction("Remove", qak.MCallback(Remove)), 
    qae.Utils.SetFunction("Body", qae.Body(Body)), 
    "0 * * ? * *", #Cron Schedule
    qae.Utils.SetFunction("Job", qae.Job(Job))
    )
            ` 
            ]

        }
        console.log(templateCode)

        this.quantapp.Post('m/createf',
            templateCode
            ,
            //this.quantapp.Post('strategy/portfoliolist',[93437],
            data => {
                console.log(data)
            })
    }

    createNewFunctionCs(){
        let templateCode = {
            WorkspaceID: this.wid,
            ID: "",
            Name: "Hello C# Agent",
            Description: "Hello C# Analytics Agent Skeleton",
            MID: "",
            
    
            Load: "Load",
            Add: "Add",
            Exchange: "Exchange",
            Remove: "Remove",
            Body: "Body",
            
            Job: "Job",
            ScheduleCommand: "0 * * ? * *",
            Started: false,
    
            Code: [ 
    `//cs
using System;
using QuantApp.Engine;
using QuantApp.Kernel;

public class CSharpAgent
{
    public static FPKG pkg()
    {
        return new FPKG(
            null, //ID
            "` + this.wid + `", //Workspace ID
            "Hello C# Agent", //Name
            "Hello C# Analytics Agent Sample", //Description
            null, //M ID Listener
            Utils.SetFunction("Load", new Load((object[] data) =>
                {
                    CSTest.CSharpBase.Load(null);
                    // Console.WriteLine("C# Agent Load");
                })), 
            Utils.SetFunction("Add", new MCallback((string id, object data) =>
                {
                    // Console.WriteLine("C# Agent Add: " + id + data.ToString());
                })), 
            Utils.SetFunction("Exchange", new MCallback((string id, object data) =>
                {
                    // Console.WriteLine("C# Agent Exchange: " + id);
                })), 
            Utils.SetFunction("Remove", new MCallback((string id, object data) =>
                {
                    // Console.WriteLine("C# Agent Remove: " + id);
                })), 
            Utils.SetFunction("Body", new Body((object data) =>
                {
                    // Console.WriteLine("C# Agent Body " + data);
                    return data;
                })), 

            "0 * * ? * *", //Cron Schedule
            Utils.SetFunction("Job", new Job((DateTime date, string command) =>
                {
                    // Console.WriteLine("C# Agent Job: " + date +  " --> " + command);
                }))
            );
    }
}
            ` 
            ]

        }
        console.log(templateCode)

        this.quantapp.Post('m/createf',
            templateCode
            ,
            //this.quantapp.Post('strategy/portfoliolist',[93437],
            data => {
                console.log(data)
            })
    }

    createNewFunctionVb(){
        let templateCode = {
            WorkspaceID: this.wid,
            ID: "",
            Name: "Hello VB Agent",
            Description: "Hello VB Analytics Agent Skeleton",
            MID: "",
            
    
            Load: "Load",
            Add: "Add",
            Exchange: "Exchange",
            Remove: "Remove",
            Body: "Body",
            
            Job: "Job",
            ScheduleCommand: "0 * * ? * *",
            Started: false,
    
            Code: [ 
    `'vb
    Imports System
    Imports QuantApp.Engine
    Imports QuantApp.Kernel
    
    
    Public Class VBAgent    
        Public Shared Sub Load(data() As object) 
            ' Console.WriteLine("VBAgent Agent Load")
        End Sub
    
        public Shared Sub Add(id As String, data As object)
            ' Console.WriteLine("VBAgent Agent Add: " + id + " " + data.ToString())
        End Sub
    
        public Shared Sub Exchange(id As String, data As object) 
            ' Console.WriteLine("VBAgent Agent Exchange: " + id)
        End Sub
    
        public Shared Sub Remove(id As String, data As object)
            ' Console.WriteLine("VBAgent Agent Remove: " + id)
        End Sub
    
        public Shared Function Body(data As object) As object
            ' Console.WriteLine("VBAgent Body " + data.ToString())
            Return data
        End Function
    
    
        public Shared Sub Job(datetime As DateTime, command As string)
            ' Console.WriteLine("VBAgent Agent Job")
        End Sub
    
        public Shared Function pkg() As FPKG
            Return new FPKG(
                Nothing, 'ID
                "` + this.wid + `", 'Workspace ID
                "Hello VB Agent", 'Name
                "Hello VB Analytics Agent Sample", 'Description
                Nothing, 'M ID Listener
                Utils.SetFunction("Load", new Load(AddressOf Load)), 
                Utils.SetFunction("Add", new MCallback(AddressOf Add)), 
                Utils.SetFunction("Exchange", new MCallback(AddressOf Exchange)), 
                Utils.SetFunction("Remove", new MCallback(AddressOf Remove)), 
                Utils.SetFunction("Body", new Body(AddressOf Body)), 
                "0 * * ? * *", 'Cron Schedule
                Utils.SetFunction("Job", new Job(AddressOf Job))
                )
        End Function
    End Class
            ` 
            ]

        }
        console.log(templateCode)

        this.quantapp.Post('m/createf',
            templateCode
            ,
            //this.quantapp.Post('strategy/portfoliolist',[93437],
            data => {
                console.log(data)
            })
    }
    
    createNewFunctionJs(){
        let templateCode = {
            WorkspaceID: this.wid,
            ID: "",
            Name: "Javascript Agent",
            Description: "Hello Javascript Analytics Agent Skeleton",
            MID: "",
            
    
            Load: "Load",
            Add: "Add",
            Exchange: "Exchange",
            Remove: "Remove",
            Body: "Body",
            
            Job: "Job",
            ScheduleCommand: "0 * * ? * *",
            Started: false,
    
            Code: [ 
    `//js
let log = System.Console.WriteLine
var qkernel = importNamespace('QuantApp.Kernel')
var qengine = importNamespace('QuantApp.Engine')


let pkg = new qengine.FPKG(
    null, //ID
    '` + this.wid + `', //Workspace ID
    'Hello Js Agent', //Name
    'Hello Js Analytics Agent Sample', //Description
    null, //M ID Listener
    jsWrapper.SetLoad('Load', 
        function(data){
            // log('JS Load: ' + data)
        }),

    jsWrapper.SetCallback('Add', 
        function(id, data){
            //log('JS Add: ' + id + ' ' + String(data.Name))//.Name)
        }), 

    jsWrapper.SetCallback('Exchange', 
        function(id, data){
            // log('JS Exchange: ' + id + ' ' + data)// + ' ' + data.Name)
        }), 

    jsWrapper.SetCallback('Remove', 
        function(id, data){
            // log('JS Remove:' + id + ' ' + data)
        }), 

    jsWrapper.SetBody('Body', 
        function(data){
            // log('JS Body: ' + data)
            return data;
        }), 

    '0 * * ? * *', //Cron Schedule
    jsWrapper.SetJob('Job', 
        function(date, data){
            // log('JS Job: ' + date + ' ' + data)
        })
    )
            ` 
            ]

        }
        console.log(templateCode)

        this.quantapp.Post('m/createf',
            templateCode
            ,
            //this.quantapp.Post('strategy/portfoliolist',[93437],
            data => {
                console.log(data)
            })
    }
}
