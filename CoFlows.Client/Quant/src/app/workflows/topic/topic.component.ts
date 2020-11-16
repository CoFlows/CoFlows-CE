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

    private newPermission = 0
    private newEmail = ''

    result = { columns: [], Item2: [], SubItems: []}
    users = []
    users_filtered = []
    search = 'Loading users...'

    subgroups = []
    activeGroupID = ''

    updateUserFilter(event) {
        const val = event.target.value.toLowerCase()
        this.search = val

        const temp = this.users.filter(d => {
            return (d.FirstName.toLowerCase().indexOf(val) !== -1|| (d.LastName + "").toLowerCase().indexOf(val) !== -1 || d.Email.toLowerCase().indexOf(val) !== -1) || !val
        })

        if(temp.length == 0)
            this.search = 'No users found...'
        this.users_filtered = temp
    }

    subscribe() {
        this.result = { columns: [], Item2: [], SubItems: []}
        this.users = []
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

                this.coflows.Get("administration/Users?groupid=" + this.wid, data => {
                    // console.log(data)
                    if(data == null){
                        this.users_filtered = []
                        this.search = 'no users found'
                    }
                    else{
                        this.users = data
                        this.users_filtered = this.users
                        if(this.users_filtered.length == 0)
                            this.search = 'no users found'
                            // this.search = ''
                    }
                })
        
                this.coflows.Get("administration/subgroups?groupid=" + this.wid + "&aggregated=true", data => {
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
            
            this.users = []
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
        }, (reason) => {
        });
    }


    viewGroup(sgid){
        this.users_filtered = []
        this.search = 'loading users...'
        this.coflows.Get("administration/Users?groupid=" + sgid, data => {
            // console.log(data)
            this.activeGroupID = sgid
            if(data == null){
                this.users_filtered = []
                this.search = 'no users found'
            }
            else{
                this.users = data
                this.users_filtered = this.users
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
                    this.coflows.Get("administration/Users?groupid=" + this.wid, data => {
                        this.users = data
                        this.users_filtered = this.users
                        // console.log(data, (Date.now() - t0) / 1000)
                        this.search = ''
                        
                    });
                    
                    
                }
                else
                    this.addPermissionMessage = data.Data

            });
    }

    activePermissionID = null
    openRemovePermission(content, permission) {
        // console.log(content, permission)
        this.activeModal = content
        this.activePermissionID = permission.ID
        this.modalService.open(content).result.then((result) => {
        }, (reason) => {
        });        
    }
    removePermissionMessage = ''
    removePermission(){
        let id = this.activePermissionID
        
        this.search = 'Reloading permissions...'
                
        this.coflows.Get('administration/removepermission?userid=' + id + '&groupid=' + this.wid,
            data => {
                let t0 = Date.now()
                this.users_filtered = []
                this.coflows.Get("administration/Users?groupid=" + this.wid, data => {
                    this.users = data
                    this.users_filtered = this.users
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
