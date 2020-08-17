import { Component, ViewChild } from '@angular/core';

import { DrawingManager } from '@ngui/map';
import { Chart } from 'angular-highcharts';

import { CoFlowsComponent } from '../core/coflows.component';
import { DomSanitizer } from '@angular/platform-browser';



@Component({
  selector: 'coflows-query',
  templateUrl: './cfquery.component.html'
})

export class CFQueryComponent {
    
    chart : any = {}
    rows = []

    markers = {}
    polygons = {}
    selectedTab = ""

    templaceWB = {
        Name : 'main',
        ID: '',
        WorkflowID: 'Test_Workflow',
        Code: 
        `module Run
        open System
        open System.IO
        open AQI.AQILabs.Kernel
        open AQI.AQILabs.Kernel.Numerics.Util
        open AQI.AQILabs.SDK.Strategies
        open Newtonsoft.Json
    
        open CoFlows.Utils.FSharp
        
    
        type public Result = { Name : string; Value : int }
        
        
        let hello_world = 
            let out = StringWriter()
            out.WriteLine("Hello World")
            out.WriteLine("Here are a few examples of scripts / queries in F Sharp")
            out.ToString()
`
    }

    selectedWB = this.templaceWB

    mapOptions = {}
    map : google.maps.Map = null
    @ViewChild(DrawingManager) drawingManager: DrawingManager;

    compilationResult = ''
    results = []

    status = ''

    charts = {}
    figures = {}
    visibleCode = true


    
    constructor(private coflows: CoFlowsComponent, private sanitization:DomSanitizer) {
    }

    
    
    onMapReady(map){
        this.map = map;

        let center = new google.maps.LatLng(0, 0)
        this.mapOptions = {
            zoom: 2,
            center: center,
            gestureHandling: 'cooperative',
            mapTypeId: google.maps.MapTypeId.HYBRID,
            scaleControl: true
        };

        console.log(this.markers[this.selectedTab])
        this.markers[this.selectedTab].forEach(item => {

            var infowindow = new google.maps.InfoWindow({ content: item.Text });

            var marker = new google.maps.Marker({
                                position: {lat: item.Latitude, lng: item.Longitude},
                                map: this.map,
                                title: item.Name,
                                icon: {
                                    path: item.Angle == 0 ? google.maps.SymbolPath.CIRCLE : google.maps.SymbolPath.FORWARD_CLOSED_ARROW,
                                    fillColor: item.Color != '' ? item.Color : 'yellow',
                                    fillOpacity: .7,
                                    scale: item.Angle != null ? 2 : Math.pow(2, 3) / 2,//4,//Math.pow(2, 4) / 2,
                                    
                                    strokeColor: 'white',
                                    strokeWeight: .5,
                                    rotation: item.Angle
                                  }
                            })

            marker.addListener('click', x => { infowindow.open(this.map, marker) });

        })

        this.polygons[this.selectedTab].forEach(item => {

            
            var polygon = new google.maps.Polygon({
                paths: item.Coordinates,
                strokeOpacity: 0.8,
                strokeColor: 'yellow',
                strokeWeight: 2,
                fillColor: '#FFC107',
                fillOpacity: 0.15,    
                draggable:false,
                editable:false,
                map: this.map,
                zIndex: -1,
                
            });
            
        })

    }

    submitCode(workbook){
        CoFlowsComponent.UpdateInstruments = false
        this.selectedWB = workbook

        //return
        let t0 = new Date()
        let t00 = Date.now()
        this.status = 'thinking... ' + t0.toTimeString()//.getHours() + ":" + t0.getMinutes() + ":" + t0.getSeconds() + ":" + t0.getMilliseconds()
        
        this.coflows.Post('flow/createquery', 
        {
            Code: workbook,
            Function: {
                Name: null,
                Parameters: null
            }
        }
        ,
        data => {
            let t1 = new Date()
            this.status = "executed in " + (Date.now() - t00) / 1000 + " seconds @ " + t1.toTimeString()
            this.results = []

            if(data.Result.length > 0){
                
                data.Result.forEach(result => {
                    if((typeof result.Item2 === 'string' || result.Item2 instanceof String) && result.Item2 != '' || !(typeof result.Item2 === 'string' || result.Item2 instanceof String)){
                        this.results.push(result)

                        if(result.Item1.indexOf('_chart') >= 0){
                            let data = result.Item2

                            result.IsChart = data.indexOf('Python.Runtime.PythonException:') < 0
                            var spot = [], dataLength = data.length;
                            for (let i = 0; i < dataLength; i++) {
                                let item = result.Item2[i]
                                spot.push([item.Date, item.Value]);
                            }

                            var chart = {
                                chart: {
                                    type: 'spline',
                                    zoomType: 'x',
                                    marginRight: 10,
                                },
                                legend: {
                                    enabled: false
                                },
                                xAxis: {
                                    type: 'datetime',
                                    tickPixelInterval: 150
                                },
                                title: {
                                    text: null//Linechart'
                                },
                                credits: {
                                    enabled: false
                                },
                                series: [{
                                    name: 'Line 1',
                                    data: spot
                                }]
                            };
                
                            this.charts[result.Item1] = new Chart(chart);

                        }
                        else if(result.Item1.indexOf('_fig') >= 0){
                            let data = result.Item2

                            result.IsFigure = data.indexOf('Python.Runtime.PythonException:') < 0
                            this.figures[result.Item1] = this.sanitization.bypassSecurityTrustResourceUrl('data:image/svg+xml;base64,' + btoa(data))
                        }
                        else if(result.Item1.indexOf('dash_init') >= 0){
                            let data = result.Item2

                            result.IsDash = true
                            result.URL = this.sanitization.bypassSecurityTrustResourceUrl(this.coflows.coflows_server + 'dash/' + workbook.WorkflowID + '/' + workbook.ID + '?uid=' + this.coflows.quser.User.Secret)
                            result.URL2 = '/workflows/app/dash/' + workbook.WorkflowID + '/' + workbook.ID + '?uid=' + this.coflows.quser.User.Secret
                            
                        }
                        else if(result.Item1.indexOf('_map') >= 0){
                            let name = result.Item1
                            
                            result.IsMap = true
                            if(result.Item2.Item1 != undefined){
                                let data = result.Item2.Item1
                                var dataLength = data.length;
                                this.markers[name] = []
                                for (let i = 0; i < dataLength; i++) {
                                    let item = data[i]

                                    this.markers[name].push(item)
                                }

                                this.polygons[name] = []
                                data = result.Item2.Item2
                                var dataLength = data.length;
                                for (let i = 0; i < dataLength; i++) {
                                    let item = data[i]//.Coordinates

                                    this.polygons[name].push(item)
                                }
                            }
                            else {
                                let data = result.Item2
                                
                                if(data[0].Coordinates != undefined){
                                    //console.log(data,name)
                                    this.polygons[name] = []
                                    this.markers[name] = []
                                    //data = result.Item2.Item2
                                    var dataLength = data.length;
                                    for (let i = 0; i < dataLength; i++) {
                                        let item = data[i]//.Coordinates
        
                                        this.polygons[name].push(item)
                                    }
                                }
                                else{
                                    var dataLength = data.length;
                                    this.markers[name] = []
                                    this.polygons[name] = []
                                    for (let i = 0; i < dataLength; i++) {
                                        let item = data[i]
        
                                        this.markers[name].push(item)
                                    }
                                }
                            }


                        }
                        else {
                            let columns = []

                            for (let i in result.Item2[0] ) { 
                                columns.push({ prop: i, name: i, IsMap: i.indexOf('_map') >= 0})
                            }   

                            if(result.Item2[0] != undefined && result.Item2[0][0] != undefined){ //Table 
                                
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
                            else if(result.Item2.ID != undefined && result.Item2.type == null) { 
                                let fid = result.Item2.ID
                                this.coflows.LinkAction(fid,
                                    data => { //Load
                                        console.log(data)

                                        result.Item2 = data
                                        
                                        for(let i = 0; i < result.Item2.length; i++){
                                            try {
                                                if(result.Item2[i].Value != undefined){
                                                    result.Item2[i].Value = JSON.parse(result.Item2[i].Value);
                                                }
                                            } catch (e) {}
                                        }

                                        let columns = []

                                        if(result.Item2 != undefined && result.Item2[0] != undefined){
                                            for (let i in result.Item2[0].Value ) { 
                                                columns.push({ prop: i, name: i, IsMap: i.indexOf('_map') >= 0})
                                            }  
                                        
                                        
                                        //console.log(columns)

                                            let isObject = Array.isArray(Object.keys(result.Item2[0].Value)) && typeof(result.Item2[0].Value) !== 'string'

                                            if(!isObject){
                                                result.columns = [{ prop: "Value", name: "Value"}]
                                                if(true || result.Item2.forEach != undefined){
                                                    let res = []
                                                    result.Item2.forEach( val => { 
                                                        res.push({ __Key: val.Key, Value : val.Value })
                                                    })
                
                                                    result.Item2 = res
                                                }
                                            }
                                            else
                                                result.columns = columns  

                                            if(isObject){
                                                let res = []
                                                result.Item2.forEach( val => { 
                                                    //console.log(val)
                                                    let vv = val.Value
                                                    try{
                                                        vv.__Key = val.Key
                                                    }
                                                    catch{}
                                                    
                                                    res.push(vv)
                                                })
            
                                                result.Item2 = res
                                            }
                                        }
                                    },
                                    data => { //Add
                                        // console.log('ADD', data, result.Item2)

                                        let isObject = Array.isArray(Object.keys(data.Value)) && typeof(data.Value) !== 'string'
                                        if(!isObject){
                                            data.__Key = data.Key
                                            result.Item2.push(data)
                                        }
                                        else{
                                            let vv = data.Value
                                            try{
                                                vv.__Key = data.Key
                                            }
                                            catch{}
                                            result.Item2.push(vv)
                                        }

                                        result.Item2 = [... result.Item2]

                                        //console.log(result.Item2)

                                    }, 
                                    data => { //Exchange
                                        console.log('Exchange', data, result.Item2)

                                        let idx = result.Item2.findIndex(x => x.__Key == data.Key)
                                        if(idx > -1){
                                            let isObject = Array.isArray(Object.keys(data.Value)) && typeof(data.Value) !== 'string'

                                            if(!isObject){
                                                data.__Key = data.Key
                                                result.Item2[idx] = data
                                            }
                                            else{
                                                let vv = data.Value
                                                try{
                                                    vv.__Key = data.Key
                                                }
                                                catch{}
                                                result.Item2[idx] = vv
                                            }
                                        }
                                        result.Item2 = [... result.Item2]
                                    },
                                    data => {//Remove
                                        //console.log('Remove', data)

                                        let idx = result.Item2.findIndex(x => x.__Key == data.Key)
                                        if(idx > -1){
                                            result.Item2.splice(idx,1)
                                        }

                                        result.Item2 = [... result.Item2]
                                    }, 
                                )
                            }
                            else
                                result.columns = columns  
                        }
                    }

                    result.SubItems = []
                })
                
                this.compilationResult = data.Compilation//data.Compilation.indexOf('System.Reflection.TargetInvocationException: Exception has been thrown by the target of an invocation') >= 0 ? data.Compilation : null
                // console.log(this.compilationResult)
                // console.log(this.results)

                if(this.compilationResult == null || this.compilationResult == ''){
                    // console.log('hide')
                    this.visibleCode = false
                }
            } //Text
            else{
                this.compilationResult = data.Compilation
            }
        })
    }

    removeResults(){
        CoFlowsComponent.UpdateInstruments = true
        this.compilationResult = ''
        this.results = []
    }

    onTabClick(event){
        this.selectedTab = event
        
    }


    private addItem_internal(items, name, item){
        let result = { Item1: name, Item2: item, columns: [], IsChart: false, IsMap: false, IsFigure: false, IsDash: false, URL: null, URL2: null}
        if(result.Item1.indexOf('_chart') >= 0){
            //let name = result.Item1
            
            let data = result.Item2
            result.IsChart = data.indexOf('Python.Runtime.PythonException:') < 0
            var spot = [], dataLength = data.length;
            for (let i = 0; i < dataLength; i++) {
                let item = result.Item2[i]
                spot.push([item.Date, item.Value]);
            }

            var chart = {
                chart: {
                    type: 'spline',
                    zoomType: 'x',
                    marginRight: 10,
                },
                legend: {
                    enabled: false
                },
                xAxis: {
                    type: 'datetime',
                    tickPixelInterval: 150
                },
                title: {
                    text: null//Linechart'
                },
                credits: {
                    enabled: false
                },
                series: [{
                    name: 'Line 1',
                    data: spot
                }]
            };

            //this.charts[result.Item1] = new Chart(chart);
            this.charts[name] = new Chart(chart);

        }
        else if(result.Item1.indexOf('_fig') >= 0){
            
            let data = result.Item2

            result.IsFigure = data.indexOf('Python.Runtime.PythonException:') < 0
            
            this.figures[result.Item1] = this.sanitization.bypassSecurityTrustResourceUrl('data:image/svg+xml;base64,' + btoa(data))

        }
        else if(result.Item1.indexOf('dash_init') >= 0){
            let data = result.Item2

            result.IsDash = true
            result.URL = this.sanitization.bypassSecurityTrustResourceUrl(this.coflows.coflows_server + 'dash/' + this.selectedWB.WorkflowID + '/' + this.selectedWB.ID + '?uid=' + this.coflows.quser.User.Secret)
            result.URL2 = '/workflows/app/dash/' + this.selectedWB.WorkflowID + '/' + this.selectedWB.ID + '?uid=' + this.coflows.quser.User.Secret
            
        }
        else if(result.Item1.indexOf('_map') >= 0){
            let name = this.selectedTab
            
            result.IsMap = true
            
            console.log(result.Item2)
            if(result.Item2.Item2 != undefined){
                let data = result.Item2.Item1
                var dataLength = data.length;
                this.markers[name] = []
                for (let i = 0; i < dataLength; i++) {
                    let item = data[i]

                    this.markers[name].push(item)
                }

                this.polygons[name] = []
                data = result.Item2.Item2
                var dataLength = data.length;
                for (let i = 0; i < dataLength; i++) {
                    let item = data[i]//.Coordinates

                    this.polygons[name].push(item)
                }
            }
            else {
                let data = result.Item2
                if(data[0].Coordinates != undefined){
                    this.polygons[name] = []
                    this.markers[name] = []
                    //data = result.Item2//.Item2
                    var dataLength = data.length;
                    for (let i = 0; i < dataLength; i++) {
                        let item = data[i]//.Coordinates

                        this.polygons[name].push(item)
                    }
                }
                else{
                    var dataLength = data.length;
                    this.markers[name] = []
                    this.polygons[name] = []
                    for (let i = 0; i < dataLength; i++) {
                        let item = data[i]

                        this.markers[name].push(item)
                    }
                }
            }


        }
        else {
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
        }

        items.push(result)
    }

    addItem(items, name, item){
        if(item.D_link != undefined || name == 'D_link'){
            this.coflows.Get(
                'flow/getquery?wid=' + this.selectedWB.WorkflowID + '&qid=' + this.selectedWB.ID + '&_cokey=' + this.coflows.quser.User.Secret + '&' + (item.D_link != undefined ? item.D_link : item), 
                data => { 
                    this.addItem_internal(items, name, data)
                 });
        }
        else{
            this.addItem_internal(items, name, item)
        }
        
    }

    removeItem(items, item){
        let i = items.indexOf(item)
        items.splice(i,1)
    }

    resetExecution(){
        this.compilationResult = null
        this.results = []
        this.status = ''
        this.visibleCode = true
    }

    removeCode(workbook){
        this.status = 'thinking...'
        let t0 = Date.now()
        this.coflows.Post('flow/removequery', 
        workbook,
        data => {
            let tEnd = Date.now()
            this.status = "executed in " + (tEnd - t0) / 1000 + " seconds"
            console.log(data)
            this.results = []
        })
    }

    toggleVisibleCode(){
        this.visibleCode = !this.visibleCode
    }
}