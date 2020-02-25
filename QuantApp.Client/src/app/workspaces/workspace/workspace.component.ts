import { Component, ViewChild, ElementRef } from '@angular/core';
import { Chart } from 'angular-highcharts';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';

import { ActivatedRoute } from '@angular/router';

import { QuantAppComponent } from '../../quantapp/core/quantapp.component';
import { QAWorkbookComponent } from '../../quantapp/workbook/qaworkbook.component';
// import { QAStrategiesComponent } from '../../quantapp/strategies/qastrategies.component';

import { NgbTabset } from '@ng-bootstrap/ng-bootstrap';

import 'codemirror/mode/mllike/mllike.js';
import 'codemirror/mode/clike/clike.js';
import 'codemirror/mode/python/python.js';
import 'codemirror/mode/vb/vb.js';
import 'codemirror/mode/javascript/javascript.js';

import * as CodeMirror from 'codemirror/lib/codemirror.js';

@Component({
  selector: 'workspace-component',
  
  templateUrl: './workspace.component.html',
  styles: [
      `
  :host >>> .CodeMirror {
    height: auto;
  }
  `]
})
export class WorkspaceComponent {
    rows = []
    activeChoices = [true, false];

    functions = []
    workbooks = []
    
    workbooks_filtered = []
    functions_filtered = []
    

    wid = ""

    workspace = {
        Permissions: []
    }

    permission = -1
    permissionSet = false

    containerChart = {}
    processesChart = {}

    pod = { Log: null }

    // @ViewChild('qastrategies')
    // private qastrategies:QAStrategiesComponent

    @ViewChild('qawbook')
    private qawbook:QAWorkbookComponent

    @ViewChild('logarea')
    private logArea:ElementRef

    
    private newPermission = 0
    private newEmail = ''

    private newGroup = ''

    selectedWB = {Name: "All", ID: ""}
    selectedFunc = {Name: "Workbook", ID: "Workbook"}

    selectWBFunc(item){
        this.selectedWB = JSON.parse(item)
        this.functions_filtered = []
        this.selectedFunc = {Name: "Workbook", ID: "Workbook"}
        this.tabBeforeChange(4)
    }

    selectWBFuncFunc(item){
        this.selectedFunc = JSON.parse(item)
        this.tabBeforeChange(4)
    }

    
    users = { items: [] }
    users_filtered = []
    search = 'Loading users...'

    subgroups = []
    activeGroupID = ''

    //groupId = 'Public'

    updateUserFilter(event) {
        // console.log(event)
        const val = event.target.value.toLowerCase()
        this.search = val
        // filter our data
        const temp = this.users.items.filter(d => {
            return (d.first.toLowerCase().indexOf(val) !== -1|| (d.last + "").toLowerCase().indexOf(val) !== -1 || d.email.toLowerCase().indexOf(val) !== -1) || !val
        })
        // update the rows
        if(temp.length == 0)
            this.search = 'No users found...'
        this.users_filtered = temp
    }

    constructor(private activatedRoute: ActivatedRoute, private quantapp: QuantAppComponent, private modalService: NgbModal) {

        this.activatedRoute.params.subscribe(params => {
            this.wid = params['id'];

            this.subgroups = [ { Name: 'Master', ID: this.wid }]
            this.activeGroupID = this.wid
            
            this.users = { items: [] }
            this.users_filtered = []
            this.search = 'Loading users...'

            let wiid = this.wid + "--Workbook"

            // console.log(quantapp)

            
            this.quantapp.LinkAction(this.wid,
            data => { //Load

                this.workspace = data[0].Value
                // console.log(this.workspace)

                this.workspace.Permissions.forEach(x => {
                    if(x.ID == this.quantapp.quser.User.Email) this.permission = x.Permission
                })

                
                if(this.permission == 2){
                    this.editorOptionsFs.readOnly = false
                    this.editorOptionsCs.readOnly = false
                    this.editorOptionsVb.readOnly = false
                    this.editorOptionsPy.readOnly = false
                    this.editorOptionsJs.readOnly = false
                    this.editorOptionsJava.readOnly = false
                    this.editorOptionsScala.readOnly = false
                }

                this.permissionSet = true

                let i = 0
                data[0].Value.Code.forEach(x => {
                    this.files.push(i)
                    i++
                })

                this.functions = []
                data[0].Value.Functions.forEach(x => {
                    //let fid = data.Functions[0] + "-F-MetaData";
                    let fid = x + "-F-MetaData";
                    // console.log(fid)
                    this.quantapp.LinkAction(fid,
                        data => { //Load
                            // console.log(data)
                            
                            data.forEach(x => {
                                this.functions.push(x.Value)
                            })
                        },
                        data => { //Add
                            console.log("add", data)
                            this.functions.push(data)
                            this.functions = [...  this.functions]
                        },
                        data => { //Exchange
                            console.log("exchange",data)
                            let id = this.functions.findIndex(x => x.ID == data.Value.ID)
                            
                            if(id > -1){
                                this.functions[id] = data.Value
                            }
                            this.functions = [...  this.functions]
                            
                        },
                        data => { //Remove
                            //console.log("exchange",data)
                            let id = this.functions.findIndex(x => x.ID == data.Value.ID)
                            
                            if(id > -1){
                                this.functions.splice(id, 1)
                                //this.functions[id] = data
                            }
                            this.functions = [...  this.functions]
                        }
                    );
                });
            
                // if(this.qastrategies != undefined)
                //     this.qastrategies.SetStrategies(data[0].Value.Strategies)

                this.quantapp.LinkAction(wiid,
                    data => { //Load
                        this.workbooks = data
                        this.workbooks.push({Value: {Name: "All", ID: ""}})
                        // console.log(data)
                        this.tabBeforeChange(3)

                        this.tabBeforeChange(4)

                    },
                    data => { //Add
                        console.log(data)
                    },
                    data => { //Exchange

                        console.log('Exchange', data)
                        
                        let counter = -1
                        for(let i = 0; i < this.workbooks.length; i++){
                            if(this.workbooks[i].Key == data.Key){                
                                counter = i
                            }
                        }
                        
                        if(counter > -1)
                            this.workbooks[counter] = data

                        // if(this.selectedWB.ID == data.ID)
                        //     this.selectedWB = data

                        


                        console.log(data)
                    },
                    data => { //Remove
                        console.log(data)

                        let counter = -1
                        for(let i = 0; i < this.workbooks.length; i++){
                            if(this.workbooks[i].ID == data.ID){                
                                counter = i
                            }
                        }
                        if(counter > -1)
                            this.workbooks.splice(counter,1)

                        // if(this.workbooks.length > 0)
                        //     this.selectedWB = this.workbooks[0]
                        // else
                        //     this.selectedWB = this.templaceWB
                    }
                );
            },
            data => { //Add

            },
            data => { //Exchange
                console.log(data)
                this.workspace = data.Value

                let i = 0
                data.Value.Code.forEach(x => {
                    this.files.push(i)
                    i++
                })

                this.functions = []
                data.Value.Functions.forEach(x => {
                    //let fid = data.Functions[0] + "-F-MetaData";
                    let fid = x + "-F-MetaData";
                    // console.log(fid)
                    this.quantapp.LinkAction(fid,
                        data => { //Load
                            console.log(data)
                            
                            data.forEach(x => {
                                this.functions.push(x.Value)
                            })
                        },
                        data => { //Add
                            console.log("add", data)
                            this.functions.push(data)
                            this.functions = [...  this.functions]
                        },
                        data => { //Exchange
                            console.log("exchange",data)
                            let id = this.functions.findIndex(x => x.ID = data.Value.ID)
                            
                            if(id > -1){
                                this.functions[id] = data.Value
                            }
                            this.functions = [...  this.functions]
                            
                        },
                        data => { //Remove
                            //console.log("exchange",data)
                            let id = this.functions.findIndex(x => x.ID = data.Value.ID)
                            
                            if(id > -1){
                                this.functions.splice(id, 1)
                                //this.functions[id] = data
                            }
                            this.functions = [...  this.functions]
                        }
                    );
                });
            
                // if(this.qastrategies != undefined)
                //     this.qastrategies.SetStrategies(data.Value.Strategies)

                this.quantapp.LinkAction(wiid,
                    data => { //Load
                        this.workbooks = data
                    },
                    data => { //Add
                        console.log(data)
                    },
                    data => { //Exchange

                        console.log('Exchange', data)
                        
                        let counter = -1
                        for(let i = 0; i < this.workbooks.length; i++){
                            if(this.workbooks[i].Key == data.Key){
                                counter = i
                            }
                        }
                        
                        if(counter > -1)
                            this.workbooks[counter] = data

                        console.log(data)
                    },
                    data => { //Remove
                        console.log(data)

                        let counter = -1
                        for(let i = 0; i < this.workbooks.length; i++){
                            if(this.workbooks[i].Key == data.Key){
                                counter = i
                            }
                        }
                        if(counter > -1)
                            this.workbooks.splice(counter,1)
                    }
                );
            },
            data => { //Remove

            });

            let podCounter = 0
            
            // let iid = setInterval(x => {
            //     this.quantapp.Get('m/podlog?id=' + this.wid ,
            //     data => {
            //         // console.log(data)
            //         if(data.Log != undefined){
            //             let flag = this.logArea != undefined  ? this.logArea.nativeElement.scrollTop / this.logArea.nativeElement.scrollHeight >= 0.5 : false
                            
            //             this.pod = { Log: data.Log }
            //             if(this.logArea != undefined && (flag || podCounter == 0))
            //                 this.logArea.nativeElement.scrollTop = this.logArea.nativeElement.scrollHeight

            //             if(this.logArea != undefined)
            //                 podCounter++
            //         }
            //         else
            //             clearInterval(iid)

            //     });
            // }, 2500);

            // let t0 = Date.now()
            this.quantapp.Get("administration/UsersApp_contacts?groupid=" + this.wid + "&agreements=false", data => {
                // console.log(data)
                if(data == null){
                    this.users_filtered = []
                    this.search = 'no users found'
                }
                else{
                    this.users = data
                    this.users_filtered = this.users.items
                    if(this.users_filtered.length == 0)
                        this.search = 'no users found'
                        // this.search = ''
                }
            });

            this.quantapp.Get("administration/subgroupsapp?groupid=" + this.wid + "&aggregated=true", data => {
                // console.log(data)
                this.subgroups = this.subgroups.concat(data);
            });

        });
    }

    modalMessage = ""
    activeModal = null
    open(content) {
        this.activeModal = content
        this.modalService.open(content).result.then((result) => {
            
            console.log(result)
            // if(result == 'restart'){
            //     this.modalMessage = "Restarting..."
            //     this.quantapp.Get('m/restartpod?id=' + this.wid ,
            //     data => {
            //         // console.log(data)
            //         this.modalMessage = ''
            //         this.modalService.dismissAll(content)
            //     });
            // }
            // else if(result == 'delete'){
            //     this.modalMessage = "Removing..."
            //     this.quantapp.Get('m/removepod?id=' + this.wid ,
            //     data => {
            //         // console.log(data)
            //         this.modalMessage = ''
            //         this.modalService.dismissAll(content)
            //     });
            // }

        }, (reason) => {
            // console.log(reason)
        });
    }

    onChangeFile(item){
        this.fileidx = item
    }

    onChangeActiveFunction(id, item){     
        // console.log(id, item)

        this.quantapp.Get('m/activetoggle?id=' + id ,
            data => {
                console.log(data)

            });
    }

    viewGroup(sgid){
        this.users_filtered = []
        this.search = 'loading users...'
        this.quantapp.Get("administration/UsersApp_contacts?groupid=" + sgid + "&agreements=false", data => {
            // console.log(data)
            this.activeGroupID = sgid
            if(data == null){
                this.users_filtered = []
                this.search = 'no users found'
            }
            else{
                this.users = data
                this.users_filtered = this.users.items
                if(this.users_filtered.length > 0)
                    this.search = ''
            }
        });
    }
    
    setPermission(id, permission_old, permission_new){
        console.log(id, permission_old, permission_new)
        if(permission_old != permission_new){
            this.quantapp.Get('administration/setpermission?userid=' + id + '&groupid=' + this.activeGroupID + '&accessType=' + permission_new,
                data => {
                    // console.log(data)
                    this.quantapp.showMessage('Permission updated')
                });
        }
    }

    addPermissionMessage = ''
    addPermission(){
        // console.log(this.newEmail, this.newPermission)
        this.quantapp.Get('administration/addpermission?email=' + this.newEmail + '&groupid=' + this.wid + '&accessType=' + this.newPermission,
            data => {
                if(data.Data == "ok"){

                    this.search = 'Reloading permissions...'
                    this.modalService.dismissAll(this.activeModal)

                    let t0 = Date.now()
                    this.users_filtered = []
                    this.quantapp.Get("administration/UsersApp_contacts?groupid=" + this.wid + "&agreements=false", data => {
                        // this.quantapp.Get("administration/UsersApp_contacts?groupid=00ab632b-b083-4204-bc82-6b50aa2ffb8d&agreements=false", data => {
                        this.users = data
                        this.users_filtered = this.users.items
                        // console.log(data, (Date.now() - t0) / 1000)
                        this.search = ''
                        
                    });
                    
                    
                }
                else
                    this.addPermissionMessage = data.Data

                // console.log(data)
            });
        // AddPermission(string email, int accessType)
    }

    addGroupMessage = ''
    addGroup(){
        // console.log(this.newEmail, this.newPermission)
        this.quantapp.Get('administration/newsubgroup?name=' + this.newGroup + '&parendid=' + this.wid,
            data => {
                if(data.Data == "ok"){
                    this.modalService.dismissAll(this.activeModal)

                    this.quantapp.Get("administration/subgroupsapp?groupid=" + this.wid + "&aggregated=true", data => {
                        // console.log(data)
                        this.subgroups = [ { Name: 'Master', ID: this.wid }]
                        this.subgroups = this.subgroups.concat(data);
                        this.activeGroupID = this.subgroups[0].ID;

                        this.viewGroup(this.activeGroupID);
                    });
                }
                else
                    this.addGroupMessage = data.Data

                console.log(data)
            });
        // AddPermission(string email, int accessType)
    }

    removeGroupMessage = ''
    removeGroup(){
        // console.log(this.newEmail, this.newPermission)
        this.quantapp.Get('administration/RemoveGroup?id=' + this.activeGroupID,
            data => {
                if(data.Data == "ok"){

                    // this.search = 'Reloading permissions...'
                    this.modalService.dismissAll(this.activeModal)

                    this.quantapp.Get("administration/subgroupsapp?groupid=" + this.wid + "&aggregated=true", data => {
                        console.log(data)
                        this.subgroups = [ { Name: 'Master', ID: this.wid }]
                        this.subgroups = this.subgroups.concat(data);
                        this.activeGroupID = this.subgroups[0].ID;

                        this.viewGroup(this.activeGroupID);
                    });

                    // let t0 = Date.now()
                    // this.users_filtered = []
                    // this.quantapp.Get("administration/UsersApp_contacts?groupid=" + this.wid + "&agreements=false", data => {
                    //     // this.quantapp.Get("administration/UsersApp_contacts?groupid=00ab632b-b083-4204-bc82-6b50aa2ffb8d&agreements=false", data => {
                    //     this.users = data
                    //     this.users_filtered = this.users.items
                    //     // console.log(data, (Date.now() - t0) / 1000)
                    //     this.search = ''
                        
                    // });
                    
                    
                }
                else
                    this.addGroupMessage = data.Data

                console.log(data)
            });
        // AddPermission(string email, int accessType)
    }
     
    activePermissionID = null
    openRemovePermission(content, permission) {
        // console.log(content, permission)
        this.activeModal = content
        this.activePermissionID = permission.ID
        this.modalService.open(content).result.then((result) => {
            
            // console.log(result)
            // if(result == 'restart'){
            //     this.modalMessage = "Restarting..."
            //     this.quantapp.Get('m/restartpod?id=' + this.wid ,
            //     data => {
            //         // console.log(data)
            //         this.modalMessage = ''
            //         this.modalService.dismissAll(content)
            //     });
            // }
            // else if(result == 'delete'){
            //     this.modalMessage = "Removing..."
            //     this.quantapp.Get('m/removepod?id=' + this.wid ,
            //     data => {
            //         // console.log(data)
            //         this.modalMessage = ''
            //         this.modalService.dismissAll(content)
            //     });
            // }

        }, (reason) => {
            // console.log(reason)
            // this.modalService.dismissAll(content)
        });

        // this.modalMessage = ''
        
    }
    removePermissionMessage = ''
    removePermission(){
        let id = this.activePermissionID
        // console.log(id)
        this.search = 'Reloading permissions...'
                
        this.quantapp.Get('administration/removepermission?userid=' + id + '&groupid=' + this.wid,
            data => {
                let t0 = Date.now()
                this.users_filtered = []
                this.quantapp.Get("administration/UsersApp_contacts?groupid=" + this.wid + "&agreements=false", data => {
                    // this.quantapp.Get("administration/UsersApp_contacts?groupid=00ab632b-b083-4204-bc82-6b50aa2ffb8d&agreements=false", data => {
                    this.users = data
                    this.users_filtered = this.users.items
                    // console.log(data, (Date.now() - t0) / 1000)
                    this.search = ''
                    this.modalService.dismissAll(this.activeModal)
                });
            });
    
    }

    tabBeforeChange(event){
        // console.log(event)
        if(event == 0 && this.qawbook != undefined)
            QuantAppComponent.UpdateInstruments = !(this.qawbook.status.indexOf('thinking...') == -1 && this.qawbook.results.length > 0)
        else
            QuantAppComponent.UpdateInstruments = false

        if(event == 3){
            // let iid = setInterval(x => {
            //     this.quantapp.Get('m/podlog?id=' + this.wid ,
            //     data => {
            //         console.log(data)
            //         if(data.Log != undefined){
            //             this.pod = { Log: data.Log }
            //             if(this.logArea != undefined)
            //                 this.logArea.nativeElement.scrollTop = this.logArea.nativeElement.scrollHeight
            //         }

            //     });
            // });
        }

        // if(event == 4){
        //     this.quantapp.Get('m/containerresourcedata?id=' + this.wid ,
        //     data => {
        //         if(data.length > 0) {
        //             // console.log(data)

        //             var cores = [], memory = [], dataLength = data.length;
        //             for (var i = 0; i < dataLength; i++) {
        //                 cores.push([data[i].Timestamp, data[i].Cores]);
        //                 memory.push([data[i].Timestamp, data[i].Memory]);
        //             }

        //             var chart_config = {
        //                 chart: {                    
        //                     zoomType: 'x',
        //                 },
        //                 title: {
        //                     text: null
        //                 },
        //                 credits: {
        //                     enabled: false
        //                 },
        //                 useHighStocks: true,                
        //                 legend: {
        //                     enabled: false
        //                 },
        //                 xAxis: [{
        //                     type: 'datetime',
        //                 }],
        //                 yAxis: [{
        //                     title: {
        //                         offset: 75,
        //                         text: 'Cores',
        //                     },
        //                     gridLineColor: 'rgba(154,154,154,0.20)',
        //                     alignTicks: false,
        //                     height: 190,
        //                     lineWidth: 0,
        //                     minPadding: 0.005,
        //                     maxPadding: 0.005,
        //                 },
        //                 {
        //                     title: {
        //                         offset: 57,
        //                         text: 'Memory (MB)',
        //                     },
        //                     gridLineColor: 'rgba(154,154,154,0.20)',
        //                     alignTicks: false,
        //                     top: 225,
        //                     offset: 0,
        //                     height: 120,
        //                     lineWidth: 0,
        //                     minPadding: 0.005,
        //                     maxPadding: 0.005,
        //                 //    labels: {
        //                 //        formatter: function () {
        //                 //            return formatYAxis(this.value, true) + "%";
        //                 //        }
        //                 //    }
        //                 }
        //                 ],

        //                 tooltip: {
        //                     headerFormat: '<b>{point.key}</b><br />',
        //                     valueDecimals: 2
        //                 },
        //                 options: {
        //                     chart: {
        //                         zoomType: 'x'
        //                     },
        //                     rangeSelector: {
        //                         enabled: true
        //                     },
        //                     navigator: {
        //                         enabled: true
        //                     }
        //                 },
        //                 series: [{
        //                     type: 'line',
        //                     name: 'Cores',
        //                     data: cores,
        //                     shadow: true,
        //                     id: 'dataseries',
        //                     shape: 'none',
        //                     yAxis: 0,
        //                     marker: {
        //                         enabled: false
        //                     }                    
        //                 },
        //                 {
        //                     type: 'area',
        //                     name: 'Memory (MB)',
        //                     data: memory,
        //                     shadow: true,
        //                     yAxis: 1,
        //                     // tooltip: {
        //                     //     valueSuffix: ' %'
        //                     // },
        //                     marker: {
        //                         enabled: false
        //                     }
        //                 }
        //             ]};

        //             this.containerChart = new Chart(chart_config);
        //         }
        //         else
        //             this.containerChart = null
                
        //     });

        //     this.quantapp.Get('m/processresourcedata?id=' + this.wid ,
        //     data => {
        //         if(data.length > 0) {
        //             // console.log(data)

        //             let map = new Map<string, Map<string, object>>()
        //             let names = []
        //             this.functions_filtered = []
        //             data.forEach(x => {
                        
        //                 if(!map[x.WorkbookID]){
        //                     names.push(x.WorkbookID)
        //                     map[x.WorkbookID] = new Map<string, object>()
        //                 }

        //                 if(!(map[x.WorkbookID][x.FunctionID])){
        //                     if(this.selectedWB.ID == x.WorkbookID && x.FunctionID != "" && x.WorkbookID != ""){
        //                         this.functions_filtered.push({ Value: {Name : x.FunctionID, ID: x.FunctionID }})
        //                     }

        //                     map[x.WorkbookID][x.FunctionID] = []
        //                 }

        //                 map[x.WorkbookID][x.FunctionID].push({ Timestamp: x.Timestamp, Value: x.TimeSpan})
        //             })
        //             // console.log(map)

        //             if(this.workbooks_filtered.length == 0){
        //                 this.workbooks.forEach(x => {
        //                     if(map[x.Value.ID] || x.Value.Name == "Workbook") {
        //                         this.workbooks_filtered.push(x)
        //                     }
        //                 })
        //             }
        //             this.functions_filtered.push({ Value: {Name : "Workbook", ID: "Workbook" }})
        //             // console.log(this.functions_filtered)

        //             // console.log(this.selectedFunc.ID)

        //             var timeSpan = [], dataLength = data.length;
        //             for (var i = 0; i < dataLength; i++) {
        //                 if(this.selectedWB.ID == "" || this.selectedWB.ID == undefined)
        //                     timeSpan.push([data[i].Timestamp, data[i].TimeSpan])
        //                 else{
        //                     if(data[i].WorkbookID == this.selectedWB.ID)
        //                         if(this.selectedFunc.ID == "" || this.selectedFunc.ID == "Workbook")
        //                             timeSpan.push([data[i].Timestamp, data[i].TimeSpan])
        //                         else
        //                             if(data[i].FunctionID == this.selectedFunc.ID)
        //                                 timeSpan.push([data[i].Timestamp, data[i].TimeSpan])
        //                         //else
        //                         //    timeSpan.push([data[i].Timestamp, data[i].TimeSpan])
        //                 }

        //                 // this.selectedFunc
        //             }

        //             var chart_config = {
        //                 chart: {                    
        //                     zoomType: 'x',
        //                 },
        //                 title: {
        //                     text: null
        //                 },
        //                 credits: {
        //                     enabled: false
        //                 },
        //                 useHighStocks: true,                
        //                 legend: {
        //                     enabled: false
        //                 },
        //                 xAxis: [{
        //                     type: 'datetime',
        //                 }],
        //                 yAxis: [{
        //                     title: {
        //                         offset: 75,
        //                         text: 'Seconds',
        //                     },
        //                     gridLineColor: 'rgba(154,154,154,0.20)',
        //                     alignTicks: false,
        //                     // height: 190,
        //                     // lineWidth: 0,
        //                     // minPadding: 0.005,
        //                     // maxPadding: 0.005,
        //                 }
        //                 //,
        //                 // {
        //                 //     title: {
        //                 //         offset: 57,
        //                 //         text: 'Memory',
        //                 //     },
        //                 //     gridLineColor: 'rgba(154,154,154,0.20)',
        //                 //     alignTicks: false,
        //                 //     top: 225,
        //                 //     offset: 0,
        //                 //     height: 120,
        //                 //     lineWidth: 0,
        //                 //     minPadding: 0.005,
        //                 //     maxPadding: 0.005,
        //                 // //    labels: {
        //                 // //        formatter: function () {
        //                 // //            return formatYAxis(this.value, true) + "%";
        //                 // //        }
        //                 // //    }
        //                 // }
        //                 ],

        //                 tooltip: {
        //                     headerFormat: '<b>{point.key}</b><br />',
        //                     valueDecimals: 2
        //                 },
        //                 options: {
        //                     chart: {
        //                         zoomType: 'x'
        //                     },
        //                     rangeSelector: {
        //                         enabled: true
        //                     },
        //                     navigator: {
        //                         enabled: true
        //                     }
        //                 },
        //                 series: [{
        //                     type: 'line',
        //                     name: 'TimeSpan',
        //                     data: timeSpan,
        //                     shadow: true,
        //                     id: 'dataseries',
        //                     shape: 'none',
        //                     yAxis: 0,
        //                     marker: {
        //                         enabled: false
        //                     }                    
        //                 }
        //                 //,
        //                 // {
        //                 //     type: 'area',
        //                 //     name: 'Memory (MB)',
        //                 //     data: memory,
        //                 //     shadow: true,
        //                 //     yAxis: 1,
        //                 //     // tooltip: {
        //                 //     //     valueSuffix: ' %'
        //                 //     // },
        //                 //     marker: {
        //                 //         enabled: false
        //                 //     }
        //                 // }
        //             ]};

        //             this.processesChart = new Chart(chart_config);
        //         }
        //         else
        //             this.processesChart = null
        //     });
        // }
    }

    //Workbook Sourcecode

    files = []
    fileidx = 0
    code = '...';
    editorOptionsFs = {
        lineWrapping: false,
        lineNumbers: true,
        readOnly: true,
        mode: 'text/x-fsharp',//, text/x-cython',
        foldGutter: {
            rangeFinder: CodeMirror.fold.combine(CodeMirror.fold.indent, CodeMirror.fold.comment)             
        },
        gutters: ["CodeMirror-linenumbers", "CodeMirror-foldgutter"],

        indentUnit: 4,
        extraKeys:{
            Tab:  cm => {
                if (cm.doc.somethingSelected()) {
                    return CodeMirror.Pass;
                }
                var spacesPerTab = cm.getOption("indentUnit");
                // console.log(spacesPerTab)
                var spacesToInsert = spacesPerTab - (cm.doc.getCursor("start").ch % spacesPerTab);    
                var spaces = Array(spacesToInsert + 1).join(" ");
                cm.replaceSelection(spaces, "end", "+input");
            }
        },
        
    };

    editorOptionsCs = {
        lineWrapping: false,
        lineNumbers: true,
        readOnly: true,
        mode: 'text/x-csharp',//, text/x-cython',
        foldGutter: {
            rangeFinder: CodeMirror.fold.combine(CodeMirror.fold.indent, CodeMirror.fold.comment)             
        },
        gutters: ["CodeMirror-linenumbers", "CodeMirror-foldgutter"],

        indentUnit: 4,
        extraKeys:{
            Tab:  cm => {
                if (cm.doc.somethingSelected()) {
                    return CodeMirror.Pass;
                }
                var spacesPerTab = cm.getOption("indentUnit");
                // console.log(spacesPerTab)
                var spacesToInsert = spacesPerTab - (cm.doc.getCursor("start").ch % spacesPerTab);    
                var spaces = Array(spacesToInsert + 1).join(" ");
                cm.replaceSelection(spaces, "end", "+input");
            }
        },
        
    };

    editorOptionsVb = {
        lineWrapping: false,
        lineNumbers: true,
        readOnly: true,
        mode: 'text/x-vb',//, text/x-cython',
        foldGutter: {
            rangeFinder: CodeMirror.fold.combine(CodeMirror.fold.indent, CodeMirror.fold.comment)             
        },
        gutters: ["CodeMirror-linenumbers", "CodeMirror-foldgutter"],

        indentUnit: 4,
        extraKeys:{
            Tab:  cm => {
                if (cm.doc.somethingSelected()) {
                    return CodeMirror.Pass;
                }
                var spacesPerTab = cm.getOption("indentUnit");
                // console.log(spacesPerTab)
                var spacesToInsert = spacesPerTab - (cm.doc.getCursor("start").ch % spacesPerTab);    
                var spaces = Array(spacesToInsert + 1).join(" ");
                cm.replaceSelection(spaces, "end", "+input");
            }
        },
        
    };

    editorOptionsPy = {
        lineWrapping: false,
        lineNumbers: true,
        readOnly: true,
        mode: 'text/x-cython',//, text/x-cython',
        foldGutter: {
            rangeFinder: CodeMirror.fold.combine(CodeMirror.fold.indent, CodeMirror.fold.comment)             
        },
        gutters: ["CodeMirror-linenumbers", "CodeMirror-foldgutter"],

        indentUnit: 4,
        extraKeys:{
            Tab:  cm => {
                if (cm.doc.somethingSelected()) {
                    return CodeMirror.Pass;
                }
                var spacesPerTab = cm.getOption("indentUnit");
                // console.log(spacesPerTab)
                var spacesToInsert = spacesPerTab - (cm.doc.getCursor("start").ch % spacesPerTab);    
                var spaces = Array(spacesToInsert + 1).join(" ");
                cm.replaceSelection(spaces, "end", "+input");
            }
        },
        
    };

    editorOptionsJs = {
        lineWrapping: false,
        lineNumbers: true,
        readOnly: true,
        mode: 'text/javascript',//, text/x-cython',
        foldGutter: {
            rangeFinder: CodeMirror.fold.combine(CodeMirror.fold.indent, CodeMirror.fold.comment)             
        },
        gutters: ["CodeMirror-linenumbers", "CodeMirror-foldgutter"],

        indentUnit: 4,
        extraKeys:{
            Tab:  cm => {
                if (cm.doc.somethingSelected()) {
                    return CodeMirror.Pass;
                }
                var spacesPerTab = cm.getOption("indentUnit");
                // console.log(spacesPerTab)
                var spacesToInsert = spacesPerTab - (cm.doc.getCursor("start").ch % spacesPerTab);    
                var spaces = Array(spacesToInsert + 1).join(" ");
                cm.replaceSelection(spaces, "end", "+input");
            }
        },
        
    };

    editorOptionsJava = {
        lineWrapping: false,
        lineNumbers: true,
        readOnly: true,
        mode: 'text/x-java',//, text/x-cython',
        foldGutter: {
            rangeFinder: CodeMirror.fold.combine(CodeMirror.fold.indent, CodeMirror.fold.comment)             
        },
        gutters: ["CodeMirror-linenumbers", "CodeMirror-foldgutter"],

        indentUnit: 4,
        extraKeys:{
            Tab:  cm => {
                if (cm.doc.somethingSelected()) {
                    return CodeMirror.Pass;
                }
                var spacesPerTab = cm.getOption("indentUnit");
                // console.log(spacesPerTab)
                var spacesToInsert = spacesPerTab - (cm.doc.getCursor("start").ch % spacesPerTab);    
                var spaces = Array(spacesToInsert + 1).join(" ");
                cm.replaceSelection(spaces, "end", "+input");
            }
        },
        
    };

    editorOptionsScala = {
        lineWrapping: false,
        lineNumbers: true,
        readOnly: true,
        mode: 'text/x-scala',//, text/x-cython',
        foldGutter: {
            rangeFinder: CodeMirror.fold.combine(CodeMirror.fold.indent, CodeMirror.fold.comment)             
        },
        gutters: ["CodeMirror-linenumbers", "CodeMirror-foldgutter"],

        indentUnit: 4,
        extraKeys:{
            Tab:  cm => {
                if (cm.doc.somethingSelected()) {
                    return CodeMirror.Pass;
                }
                var spacesPerTab = cm.getOption("indentUnit");
                // console.log(spacesPerTab)
                var spacesToInsert = spacesPerTab - (cm.doc.getCursor("start").ch % spacesPerTab);    
                var spaces = Array(spacesToInsert + 1).join(" ");
                cm.replaceSelection(spaces, "end", "+input");
            }
        },
        
    };


    //Workbook functionality
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
    
            Code: [ { Item1: "Hello_Fs_Agent.fs", Item2:
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
            }]

        }
        // console.log(templateCode)

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
    
            Code: [ { Item1: "Hello_Python_Agent.py", Item2:
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
            }]

        }
        // console.log(templateCode)

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
    
            Code: [ { Item1: "Hello_Cs_Agent.cs", Item2:
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
            }]

        }
        // console.log(templateCode)

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
    
            Code: [ { Item1: "Hello_Vb_Agent.vb", Item2:
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
            }]

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
    
            Code: [ { Item1: "Hello_Js_Agent.js", Item2:
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
            }]

        }
        // console.log(templateCode)

        this.quantapp.Post('m/createf',
            templateCode
            ,
            //this.quantapp.Post('strategy/portfoliolist',[93437],
            data => {
                console.log(data)
            })
    }
}
