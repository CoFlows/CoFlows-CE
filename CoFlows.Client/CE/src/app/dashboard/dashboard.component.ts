import { Component, ViewChild } from '@angular/core';
import { CoFlowsComponent } from '../coflows/core/coflows.component';
import { CFQueryComponent } from '../coflows/query/cfquery.component';

import { NgbTabset } from '@ng-bootstrap/ng-bootstrap';


@Component({
  selector: 'dashboard-component',
  
  templateUrl: './dashboard.component.html',
  styles: []
})
export class DashboardComponent {

    agents = []
    workbooks = []

    workflows = []
    
    wid = ""
    permission = -1

    workflow = {
        Permissions: []
    }

    @ViewChild('cfquery')
    private cfquery:CFQueryComponent

    constructor(private coflows: CoFlowsComponent) {

        this.coflows.Get("m/servicedworkflows", data => {
            // console.log(data)
            this.workflows = data

            this.workflows.forEach(workflow => {
                let wiid = workflow.ID + "--Queries"
                this.coflows.LinkAction(workflow.ID,
                    data => { //Load
                        
                        workflow.workflow = data[0].Value
                        
                        workflow.workflow.Permissions.forEach(x => {
                            if(x.ID == this.coflows.quser.User.Email) this.permission = x.Permission
                        })
    
                        workflow.agents = []
                        workflow.activeAgents = []
                        workflow.inActiveAgents = []
                        data[0].Value.Agents.forEach(x => {
    
                            let fid = x + "-F-MetaData";
                            // console.log(fid)
                            this.coflows.LinkAction(fid,
                                data => { //Load
                                    // console.log(data)
                                    
                                    data.forEach(x => {
                                        workflow.agents.push(x.Value)
                                        if(x.Value.Started)
                                            workflow.activeAgents.push(x.Value)
                                        else
                                            workflow.inActiveAgents.push(x.Value)
                                    })
                                },
                                data => { //Add
                                    // console.log("add", data)
                                    workflow.agents.push(data)
                                    workflow.agents = [...  workflow.agents]

                                    workflow.activeAgents = []
                                    workflow.inActiveAgents = []
                                    workflow.agents.forEach(x => {
                                        if(x.Value.Started)
                                            workflow.activeAgents.push(x.Value)
                                        else
                                            workflow.inActiveAgents.push(x.Value)
                                    })
                                },
                                data => { //Exchange
                                    // console.log("exchange",data)
                                    let id = workflow.agents.findIndex(x => x.ID == data.Value.ID)
                                    
                                    if(id > -1){
                                        workflow.agents[id] = data.Value
                                    }
                                    workflow.agents = [...  workflow.agents]

                                    workflow.activeAgents = []
                                    workflow.inActiveAgents = []
                                    workflow.agents.forEach(x => {
                                        if(x.Value.Started)
                                            workflow.activeAgents.push(x.Value)
                                        else
                                            workflow.inActiveAgents.push(x.Value)
                                    })
                                    
                                },
                                data => { //Remove
                                    //console.log("exchange",data)
                                    let id = workflow.agents.findIndex(x => x.ID == data.Value.ID)
                                    
                                    if(id > -1){
                                        workflow.agents.splice(id, 1)
                                        //this.agents[id] = data
                                    }
                                    workflow.agents = [...  workflow.agents]

                                    workflow.activeAgents = []
                                    workflow.inActiveAgents = []
                                    workflow.agents.forEach(x => {
                                        if(x.Value.Started)
                                            workflow.activeAgents.push(x.Value)
                                        else
                                            workflow.inActiveAgents.push(x.Value)
                                    })
                                }
                            );
                        });
    
                        // this.qastrategies.SetStrategies(data[0].Value.Strategies)

                        // console.log(this.workflows)
                        
                        this.coflows.LinkAction(wiid,
                            data => { //Load
                                workflow.workbooks = data
                            },
                            data => { //Add
                                // console.log(data)
                            },
                            data => { //Exchange
    
                                // console.log('Exchange', data)
                                
                                let counter = -1
                                for(let i = 0; i < workflow.workbooks.length; i++){
                                    // console.log(this.workbooks[i])
                                    if(workflow.workbooks[i].Key == data.Key){                
                                        counter = i
                                    }
                                }
                                
                                if(counter > -1)
                                workflow.workbooks[counter] = data
                                // console.log(data)
                            },
                            data => { //Remove
                                // console.log(data)
    
                                let counter = -1
                                for(let i = 0; i < workflow.workbooks.length; i++){
                                    if(workflow.workbooks[i].Key == data.Key){
                                        counter = i
                                    }
                                }
                                if(counter > -1)
                                workflow.workbooks.splice(counter,1)
                            }
                        );
                    },
                    data => { //Add
                    },
                    data => { //Exchange
                        workflow.workflow = data.Value
    
                        workflow.agents = []
                        data.Value.Agents.forEach(x => {
    
                            let fid = x + "-F-MetaData";
                            this.coflows.LinkAction(fid,
                                data => { //Load                                
                                    data.forEach(x => {
                                        workflow.agents.push(x.Value)
                                    })
                                },
                                data => { //Add
                                    workflow.agents.push(data)
                                    workflow.agents = [...  workflow.agents]
                                },
                                data => { //Exchange
                                    let id = workflow.agents.findIndex(x => x.ID = data.Value.ID)
                                    
                                    if(id > -1){
                                        workflow.agents[id] = data.Value
                                    }
                                    workflow.agents = [...  workflow.agents]
                                    
                                },
                                data => { //Remove
                                    let id = workflow.agents.findIndex(x => x.ID = data.Value.ID)
                                    
                                    if(id > -1){
                                        workflow.agents.splice(id, 1)
                                    }
                                    workflow.agents = [...  workflow.agents]
                                }
                            );
                        });
                        // this.qastrategies.SetStrategies(data.Value.Strategies)
                        this.coflows.LinkAction(wiid,
                            data => { //Load
                                workflow.workbooks = data
                            },
                            data => { //Add
                            },
                            data => { //Exchange
                                let counter = -1
                                for(let i = 0; i < workflow.workbooks.length; i++){
                                    if(workflow.workbooks[i].Key == data.Key){
                                        counter = i
                                    }
                                }
                                
                                workflow.workbooks[counter] = data
                                console.log(data)
                            },
                            data => { //Remove
                                let counter = -1
                                for(let i = 0; i < workflow.workbooks.length; i++){
                                    if(workflow.workbooks[i].Key == data.Key){                
                                        counter = i
                                    }
                                }
                                if(counter > -1)
                                workflow.workbooks.splice(counter,1)
                            }
                        );
                    },
                    data => { //Remove
                    }
                )
            })

            // this.wid = data[0].ID//params['id'];

            // let wiid = this.wid + "--Queries"

            // this.coflows.LinkAction(this.wid,
            //     data => { //Load
                    
            //         this.workflow = data[0].Value
                    
            //         this.workflow.Permissions.forEach(x => {
            //             if(x.ID == this.coflows.quser.User.Email) this.permission = x.Permission
            //         })
                    
                    

            //         this.agents = []
            //         data[0].Value.Agents.forEach(x => {

            //             let fid = x + "-F-MetaData";
            //             // console.log(fid)
            //             this.coflows.LinkAction(fid,
            //                 data => { //Load
            //                     // console.log(data)
                                
            //                     data.forEach(x => {
            //                         this.agents.push(x.Value)
            //                     })
            //                 },
            //                 data => { //Add
            //                     // console.log("add", data)
            //                     this.agents.push(data)
            //                     this.agents = [...  this.agents]
            //                 },
            //                 data => { //Exchange
            //                     // console.log("exchange",data)
            //                     let id = this.agents.findIndex(x => x.ID == data.Value.ID)
                                
            //                     if(id > -1){
            //                         this.agents[id] = data.Value
            //                     }
            //                     this.agents = [...  this.agents]
                                
            //                 },
            //                 data => { //Remove
            //                     //console.log("exchange",data)
            //                     let id = this.agents.findIndex(x => x.ID == data.Value.ID)
                                
            //                     if(id > -1){
            //                         this.agents.splice(id, 1)
            //                         //this.agents[id] = data
            //                     }
            //                     this.agents = [...  this.agents]
            //                 }
            //             );
            //         });

            //         // this.qastrategies.SetStrategies(data[0].Value.Strategies)
                    
            //         this.coflows.LinkAction(wiid,
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
            //         this.workflow = data.Value

            //         this.agents = []
            //         data.Value.Agents.forEach(x => {

            //             let fid = x + "-F-MetaData";
            //             this.coflows.LinkAction(fid,
            //                 data => { //Load                                
            //                     data.forEach(x => {
            //                         this.agents.push(x.Value)
            //                     })
            //                 },
            //                 data => { //Add
            //                     this.agents.push(data)
            //                     this.agents = [...  this.agents]
            //                 },
            //                 data => { //Exchange
            //                     let id = this.agents.findIndex(x => x.ID = data.Value.ID)
                                
            //                     if(id > -1){
            //                         this.agents[id] = data.Value
            //                     }
            //                     this.agents = [...  this.agents]
                                
            //                 },
            //                 data => { //Remove
            //                     let id = this.agents.findIndex(x => x.ID = data.Value.ID)
                                
            //                     if(id > -1){
            //                         this.agents.splice(id, 1)
            //                     }
            //                     this.agents = [...  this.agents]
            //                 }
            //             );
            //         });
            //         // this.qastrategies.SetStrategies(data.Value.Strategies)
            //         this.coflows.LinkAction(wiid,
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
        this.coflows.Get('m/activetoggle?id=' + id ,
            data => {
                // console.log(data)
            })
    }

    tabBeforeChange(event){
        // console.log(event)
        if(event == 0)
            CoFlowsComponent.UpdateInstruments = !(this.cfquery.status.indexOf('thinking...') == -1 && this.cfquery.results.length > 0)
        else
            CoFlowsComponent.UpdateInstruments = false

    }

    @ViewChild('tabs')
    private tabs:NgbTabset;

    createNewFunctionFs(){
        let templateCode = {
            WorkflowID: this.wid,
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
    
    open CoFlows.Kernel
    open CoFlows.Engine
    
    open Newtonsoft.Json
    
    type Result = { Name : string; Date : string }
    
    let master(): F_v01 =
        {
            ID = None
            WorkflowID = Some("` + this.wid + `")
            Code = None
            Name = "Hello F# Agent"
            Description = Some("Hello F# Analytics Agent Skeleton")
    
            MID = None //ID of MultiVerse entry which this agents is linked to
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

        this.coflows.Post('m/createf',
            templateCode
            ,
            //this.coflows.Post('strategy/portfoliolist',[93437],
            data => {
                console.log(data)
            })
    }

    createNewFunctionPy(){
        let templateCode = {
            WorkflowID: this.wid,
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
import CoFlows.Kernel as qak
import CoFlows.Engine as qae

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
    "` + this.wid + `", #Workflow ID
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

        this.coflows.Post('m/createf',
            templateCode
            ,
            //this.coflows.Post('strategy/portfoliolist',[93437],
            data => {
                console.log(data)
            })
    }

    createNewFunctionCs(){
        let templateCode = {
            WorkflowID: this.wid,
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
using CoFlows.Engine;
using CoFlows.Kernel;

public class CSharpAgent
{
    public static FPKG pkg()
    {
        return new FPKG(
            null, //ID
            "` + this.wid + `", //Workflow ID
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

        this.coflows.Post('m/createf',
            templateCode
            ,
            //this.coflows.Post('strategy/portfoliolist',[93437],
            data => {
                console.log(data)
            })
    }

    createNewFunctionVb(){
        let templateCode = {
            WorkflowID: this.wid,
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
    Imports CoFlows.Engine
    Imports CoFlows.Kernel
    
    
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
                "` + this.wid + `", 'Workflow ID
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

        this.coflows.Post('m/createf',
            templateCode
            ,
            //this.coflows.Post('strategy/portfoliolist',[93437],
            data => {
                console.log(data)
            })
    }
    
    createNewFunctionJs(){
        let templateCode = {
            WorkflowID: this.wid,
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
var qkernel = importNamespace('CoFlows.Kernel')
var qengine = importNamespace('CoFlows.Engine')


let pkg = new qengine.FPKG(
    null, //ID
    '` + this.wid + `', //Workflow ID
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

        this.coflows.Post('m/createf',
            templateCode
            ,
            //this.coflows.Post('strategy/portfoliolist',[93437],
            data => {
                console.log(data)
            })
    }
}
