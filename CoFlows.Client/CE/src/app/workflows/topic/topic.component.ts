import { Component, ViewChild, ElementRef } from '@angular/core';
import { Chart } from 'angular-highcharts';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';

import { ActivatedRoute } from '@angular/router';

import { CoFlowsComponent } from '../../coflows/core/coflows.component';

import { NgbTabset } from '@ng-bootstrap/ng-bootstrap';

@Component({
  selector: 'topic-component',
  
  templateUrl: './topic.component.html',
  styles: [
      `
  :host >>> .CodeMirror {
    height: auto;
  }
  `]
})
export class TopicComponent {
    rows = []
    activeChoices = [true, false];

    agents = []
    workbooks = []
    
    workbooks_filtered = []
    agents_filtered = []
    

    wid = ""

    workflow = {
        Permissions: []
    }

    permission = -1
    permissionSet = false

    containerChart = {}
    processesChart = {}

    pod = { Log: null }

    // @ViewChild('qastrategies')
    // private qastrategies:QAStrategiesComponent

    // @ViewChild('cfquery')
    // private cfquery:CFQueryComponent

    // @ViewChild('logarea')
    // private logArea:ElementRef

    
    private newPermission = 0
    private newEmail = ''

    private newGroup = ''

    selectedWB = {Name: "All", ID: ""}
    selectedFunc = {Name: "Workbook", ID: "Workbook"}

    selectWBFunc(item){
        this.selectedWB = JSON.parse(item)
        this.agents_filtered = []
        this.selectedFunc = {Name: "Workbook", ID: "Workbook"}
        this.tabBeforeChange(4)
    }

    selectWBFuncFunc(item){
        this.selectedFunc = JSON.parse(item)
        this.tabBeforeChange(4)
    }

    result = { columns: [], Item2: [], SubItems: []}
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

    subscribe() {
        // console.log(event)
        // const val = event.target.value
        // this.wid = val

        this.result = { columns: [], Item2: [], SubItems: []}
        this.users = { items: [] }
        this.users_filtered = []
        this.search = 'Loading users...'

        this.subgroups = []
        this.activeGroupID = ''
        this.permission = -1


        if(this.wid == "-")
            return

        this.activeGroupID = this.wid

        this.coflows.LinkAction(this.wid,
            data => { //Load

                this.permission = 2

                this.coflows.Get("administration/UsersApp_contacts?groupid=" + this.wid + "&agreements=false", data => {
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
                })
        
                this.coflows.Get("administration/subgroupsapp?groupid=" + this.wid + "&aggregated=true", data => {
                    // console.log(data)
                    this.subgroups = this.subgroups.concat(data);
                })
                

                this.result.Item2 = data

                for(let i = 0; i < this.result.Item2.length; i++){
                    try {
                        if(this.result.Item2[i].Value != undefined){
                            this.result.Item2[i].Value = JSON.parse(this.result.Item2[i].Value);
                        }
                    } catch (e) {}
                }

                let columns = []

                if(this.result.Item2 != undefined && this.result.Item2[0] != undefined){
                    for (let i in this.result.Item2[0].Value ) { 
                        columns.push({ prop: i, name: i })
                    }  
                
                
                

                    let isObject = Array.isArray(Object.keys(this.result.Item2[0].Value)) && typeof(this.result.Item2[0].Value) !== 'string'

                    if(!isObject){
                        this.result.columns = [{ prop: "Value", name: "Value"}]
                        if(true || this.result.Item2.forEach != undefined){
                            let res = []
                            this.result.Item2.forEach( val => { 
                                res.push({ __Key: val.Key, Value : val.Value })
                            })

                            this.result.Item2 = res
                        }
                    }
                    else
                    this.result.columns = columns  

                    if(isObject){
                        let res = []
                        this.result.Item2.forEach( val => { 
                            //console.log(val)
                            let vv = val.Value
                            try{
                                vv.__Key = val.Key
                            }
                            catch{}
                            
                            res.push(vv)
                        })

                        this.result.Item2 = res
                    }
                }
            },
            data => { //Add
                // console.log('ADD', data, result.Item2)

                let isObject = Array.isArray(Object.keys(data.Value)) && typeof(data.Value) !== 'string'
                if(!isObject){
                    data.__Key = data.Key
                    this.result.Item2.push(data)
                }
                else{
                    let vv = data.Value
                    try{
                        vv.__Key = data.Key
                    }
                    catch{}
                    this.result.Item2.push(vv)
                }

                this.result.Item2 = [... this.result.Item2]

                //console.log(result.Item2)

            }, 
            data => { //Exchange
                console.log('Exchange', data, this.result.Item2)

                let idx = this.result.Item2.findIndex(x => x.__Key == data.Key)
                if(idx > -1){
                    let isObject = Array.isArray(Object.keys(data.Value)) && typeof(data.Value) !== 'string'

                    if(!isObject){
                        data.__Key = data.Key
                        this.result.Item2[idx] = data
                    }
                    else{
                        let vv = data.Value
                        try{
                            vv.__Key = data.Key
                        }
                        catch{}
                        this.result.Item2[idx] = vv
                    }
                }
                this.result.Item2 = [... this.result.Item2]
            },
            data => {//Remove
                //console.log('Remove', data)

                let idx = this.result.Item2.findIndex(x => x.__Key == data.Key)
                if(idx > -1){
                    this.result.Item2.splice(idx,1)
                }

                this.result.Item2 = [... this.result.Item2]
            }, 
        )
    }

    constructor(private activatedRoute: ActivatedRoute, private coflows: CoFlowsComponent, private modalService: NgbModal) {

        this.activatedRoute.params.subscribe(params => {
            this.wid = params['id'];

            if(this.wid == "-")
                this.wid = ""

            console.log(this.wid)

            this.subgroups = [ { Name: 'Master', ID: this.wid }]
            this.activeGroupID = this.wid
            
            this.users = { items: [] }
            this.users_filtered = []
            this.search = 'Loading users...'

            // let wiid = this.wid + "--Queries"

            // console.log(coflows)

            
            // this.coflows.LinkAction(this.wid,
            //     data => { //Load
                    

            //         this.result.Item2 = data

            //         console.log(this.result.Item2)
                    
            //         for(let i = 0; i < this.result.Item2.length; i++){
            //             try {
            //                 if(this.result.Item2[i].Value != undefined){
            //                     this.result.Item2[i].Value = JSON.parse(this.result.Item2[i].Value);
            //                 }
            //             } catch (e) {}
            //         }

            //         let columns = []

            //         if(this.result.Item2 != undefined && this.result.Item2[0] != undefined){
            //             for (let i in this.result.Item2[0].Value ) { 
            //                 columns.push({ prop: i, name: i })
            //             }  
                    
                    
                    

            //             let isObject = Array.isArray(Object.keys(this.result.Item2[0].Value)) && typeof(this.result.Item2[0].Value) !== 'string'

            //             if(!isObject){
            //                 this.result.columns = [{ prop: "Value", name: "Value"}]
            //                 if(true || this.result.Item2.forEach != undefined){
            //                     let res = []
            //                     this.result.Item2.forEach( val => { 
            //                         res.push({ __Key: val.Key, Value : val.Value })
            //                     })

            //                     this.result.Item2 = res
            //                 }
            //             }
            //             else
            //             this.result.columns = columns  

            //             if(isObject){
            //                 let res = []
            //                 this.result.Item2.forEach( val => { 
            //                     //console.log(val)
            //                     let vv = val.Value
            //                     try{
            //                         vv.__Key = val.Key
            //                     }
            //                     catch{}
                                
            //                     res.push(vv)
            //                 })

            //                 this.result.Item2 = res
            //             }
            //         }

            //         console.log(this.result)
            //     },
            //     data => { //Add
            //         // console.log('ADD', data, result.Item2)

            //         let isObject = Array.isArray(Object.keys(data.Value)) && typeof(data.Value) !== 'string'
            //         if(!isObject){
            //             data.__Key = data.Key
            //             this.result.Item2.push(data)
            //         }
            //         else{
            //             let vv = data.Value
            //             try{
            //                 vv.__Key = data.Key
            //             }
            //             catch{}
            //             this.result.Item2.push(vv)
            //         }

            //         this.result.Item2 = [... this.result.Item2]

            //         //console.log(result.Item2)

            //     }, 
            //     data => { //Exchange
            //         console.log('Exchange', data, this.result.Item2)

            //         let idx = this.result.Item2.findIndex(x => x.__Key == data.Key)
            //         if(idx > -1){
            //             let isObject = Array.isArray(Object.keys(data.Value)) && typeof(data.Value) !== 'string'

            //             if(!isObject){
            //                 data.__Key = data.Key
            //                 this.result.Item2[idx] = data
            //             }
            //             else{
            //                 let vv = data.Value
            //                 try{
            //                     vv.__Key = data.Key
            //                 }
            //                 catch{}
            //                 this.result.Item2[idx] = vv
            //             }
            //         }
            //         this.result.Item2 = [... this.result.Item2]
            //     },
            //     data => {//Remove
            //         //console.log('Remove', data)

            //         let idx = this.result.Item2.findIndex(x => x.__Key == data.Key)
            //         if(idx > -1){
            //             this.result.Item2.splice(idx,1)
            //         }

            //         this.result.Item2 = [... this.result.Item2]
            //     }, 
            // )

            // let t0 = Date.now()
            

        })
    }

    modalMessage = ""
    activeModal = null
    open(content) {
        this.activeModal = content
        this.modalService.open(content).result.then((result) => {
            
            // console.log(result)
            // if(result == 'restart'){
            //     this.modalMessage = "Restarting..."
            //     this.coflows.Get('m/restartpod?id=' + this.wid ,
            //     data => {
            //         // console.log(data)
            //         this.modalMessage = ''
            //         this.modalService.dismissAll(content)
            //     });
            // }
            // else if(result == 'delete'){
            //     this.modalMessage = "Removing..."
            //     this.coflows.Get('m/removepod?id=' + this.wid ,
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


    viewGroup(sgid){
        this.users_filtered = []
        this.search = 'loading users...'
        this.coflows.Get("administration/UsersApp_contacts?groupid=" + sgid + "&agreements=false", data => {
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
            this.coflows.Get('administration/setpermission?userid=' + id + '&groupid=' + this.activeGroupID + '&accessType=' + permission_new,
                data => {
                    // console.log(data)
                    this.coflows.showMessage('Permission updated')
                });
        }
    }

    addPermissionMessage = ''
    addPermission(){
        // console.log(this.newEmail, this.newPermission)
        this.coflows.Get('administration/addpermission?email=' + this.newEmail + '&groupid=' + this.wid + '&accessType=' + this.newPermission,
            data => {
                if(data.Data == "ok"){

                    this.search = 'Reloading permissions...'
                    this.modalService.dismissAll(this.activeModal)

                    let t0 = Date.now()
                    this.users_filtered = []
                    this.coflows.Get("administration/UsersApp_contacts?groupid=" + this.wid + "&agreements=false", data => {
                        // this.coflows.Get("administration/UsersApp_contacts?groupid=00ab632b-b083-4204-bc82-6b50aa2ffb8d&agreements=false", data => {
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
        this.coflows.Get('administration/newsubgroup?name=' + this.newGroup + '&parendid=' + this.wid,
            data => {
                if(data.Data == "ok"){
                    this.modalService.dismissAll(this.activeModal)

                    this.coflows.Get("administration/subgroupsapp?groupid=" + this.wid + "&aggregated=true", data => {
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
        this.coflows.Get('administration/RemoveGroup?id=' + this.activeGroupID,
            data => {
                if(data.Data == "ok"){

                    // this.search = 'Reloading permissions...'
                    this.modalService.dismissAll(this.activeModal)

                    this.coflows.Get("administration/subgroupsapp?groupid=" + this.wid + "&aggregated=true", data => {
                        console.log(data)
                        this.subgroups = [ { Name: 'Master', ID: this.wid }]
                        this.subgroups = this.subgroups.concat(data);
                        this.activeGroupID = this.subgroups[0].ID;

                        this.viewGroup(this.activeGroupID);
                    });

                    // let t0 = Date.now()
                    // this.users_filtered = []
                    // this.coflows.Get("administration/UsersApp_contacts?groupid=" + this.wid + "&agreements=false", data => {
                    //     // this.coflows.Get("administration/UsersApp_contacts?groupid=00ab632b-b083-4204-bc82-6b50aa2ffb8d&agreements=false", data => {
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
            //     this.coflows.Get('m/restartpod?id=' + this.wid ,
            //     data => {
            //         // console.log(data)
            //         this.modalMessage = ''
            //         this.modalService.dismissAll(content)
            //     });
            // }
            // else if(result == 'delete'){
            //     this.modalMessage = "Removing..."
            //     this.coflows.Get('m/removepod?id=' + this.wid ,
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
                
        this.coflows.Get('administration/removepermission?userid=' + id + '&groupid=' + this.wid,
            data => {
                let t0 = Date.now()
                this.users_filtered = []
                this.coflows.Get("administration/UsersApp_contacts?groupid=" + this.wid + "&agreements=false", data => {
                    // this.coflows.Get("administration/UsersApp_contacts?groupid=00ab632b-b083-4204-bc82-6b50aa2ffb8d&agreements=false", data => {
                    this.users = data
                    this.users_filtered = this.users.items
                    // console.log(data, (Date.now() - t0) / 1000)
                    this.search = ''
                    this.modalService.dismissAll(this.activeModal)
                });
            });
    
    }

    tabBeforeChange(event){}


    //Workbook functionality
    @ViewChild('tabs')
    private tabs:NgbTabset;

    addItem(items, name, item){
        let result = { Item1: name, Item2: item, columns: [] }
        
        //else {
        let columns = []

        for (let i in result.Item2[0] ) { 
            columns.push({ prop: i, name: i})
        }   

        if(result.Item2[0][0] != undefined){ //Table 
            
            if(result.Item2.length > 1){
                result.columns = [{ prop: "Value", name: "Value"}]
                if(result.Item2.forEach != undefined){
                    let res = []
                    result.Item2.forEach( val => { 
                    
                        res.push({ Value : val })
                    })

                    result.Item2 = res
                }
            }
        }
        else
            result.columns = columns  
        items.push(result)
    }

    removeItem(items, item){
        let i = items.indexOf(item)
        items.splice(i,1)
    }
}
