import { Chart } from 'angular-highcharts';
import { Component } from '@angular/core';
import { NgbTabChangeEvent } from '@ng-bootstrap/ng-bootstrap';
import { CoFlowsComponent } from '../../coflows/core/coflows.component';
import { TreeNode } from 'primeng/primeng';

import {Router, ActivatedRoute, Params} from '@angular/router';

import {Observable} from "rxjs"

import 'codemirror/mode/mllike/mllike.js';

import 'codemirror/addon/fold/foldcode.js';
import 'codemirror/addon/fold/foldgutter.js';

import 'codemirror/addon/fold/brace-fold.js';
import 'codemirror/addon/fold/comment-fold.js';
import 'codemirror/addon/fold/indent-fold.js';
import 'codemirror/addon/fold/markdown-fold.js';
import 'codemirror/addon/fold/xml-fold.js';

import * as CodeMirror from 'codemirror/lib/codemirror.js';

@Component({    
    templateUrl: 'simulation.component.html',
})


export class SimulationComponent {

    // MainStrategyID = 90086;// LIVE
    //MainStrategyID = 90900;//CME Metals
    //MainStrategyID = 91164 //CME Metals + Crude
    //MainStrategyID = 91258
    //MainStrategyID = 93437//Live Testing
    //MainStrategyID = 93250//Convexity
    // MainStrategyID = 93507//Coal MACD
    //PortfolioID = 0
    //chart_indicator = {};    
    chart = {};
    
    //strategyData: TreeNode[] = [{}]
    //portfolioData: TreeNode[] = [{}]
    //orderData: TreeNode[] = [{}]
    monthlyPerformance: any = {}
    historicalOrders: any = {}
    statistics: any = [{}]

    //code = '';
    //compilationResult = '';

    //indicators = [{Name: 'Empty', ID: 0, BID: 0}]
    timeSeriesType = [{Type: 'Close'}, {Type: 'Intraday'}]
    selectedTimeSeriesType = this.timeSeriesType[0]
    //selectedIndicator = this.indicators[0]
    //selectedNode: any = {};
    currentGraphTab = '';
    //hasIndicator = false;
    
    

    parseDate(item){
        let monthNames = [
            "Jan", "Feb", "Mar",
            "Apr", "May", "Jun", "Jul",
            "Aug", "Sep", "Oct",
            "Nov", "Dec"
          ];

        let date = new Date(Date.parse(item))
        let str = date.getFullYear() + '-' + monthNames[date.getMonth()] + '-' + date.getDate()
        return str
    }

    number(n){
        return n > 9 ? "" + n: "0" + n;
    }
    parseTime(item){
        
        let date = new Date(Date.parse(item))
        let str = this.number(date.getHours()) + ':' + this.number(date.getMinutes()) + ':' + this.number(date.getSeconds()) + '.' + this.number(date.getMilliseconds())
        return str
    }


    closeSeries : any = {}
    lastSeries : any = {}

    sets : any = []
    selectedSet : any = {}
    selectedSimulation : any = {}

    constructor(private activatedRoute: ActivatedRoute, public coflows: CoFlowsComponent) {

        this.activatedRoute.params.subscribe(params => {
            let id = params['id'];

            this.coflows.LinkAction(id, 
            data => {
                console.log(data)
                //let key = x.Key
                let set_counter = 0
                data.forEach(element => {
                    let pkg = element.Value
                    pkg.SObjects = []
                    let so_index = 0
                    
                    pkg.Strategies.forEach(strat => {
                        let s = JSON.parse(strat)
                        s.SIndex = so_index
                        so_index++
                        pkg.SObjects.push(s)
                    });
                    pkg.Index = set_counter
                    set_counter++
                    this.sets.push(pkg)
                });

                let pkg = this.sets[0]
                this.selectedSet = pkg
                
                //let value = pkg.SObjects[0]
                this.selectedSimulation = pkg.SObjects[0]

                console.log(this.sets)
                console.log(this.selectedSet)
                console.log(this.selectedSimulation)

                
                this.closeSeries = this.selectedSimulation.History
                this.lastSeries = this.selectedSimulation.Intraday
                this.statistics = this.selectedSimulation.Statistics
                this.monthlyPerformance = this.selectedSimulation.Monthly

                this.updateChart()
            },
            data => { //Add

            },
            data => { //Exchange

            },
            data => { //Remove

            });
                        
        });
    }

    onChangeSet(element){
        console.log(element)
        console.log(this.sets)
        this.selectedSet = this.sets[+element]//JSON.parse(element)

        console.log(this.selectedSet)
        

        this.selectedSimulation = this.selectedSet.SObjects[0]

        this.closeSeries = this.selectedSimulation.History
        this.lastSeries = this.selectedSimulation.Intraday
        this.statistics = this.selectedSimulation.Statistics
        this.monthlyPerformance = this.selectedSimulation.Monthly

        this.updateChart()
    }

    onChangeSimulation(element){
                
        console.log(element)
        console.log(this.selectedSet)
        console.log(this.selectedSimulation)

        this.selectedSimulation = this.selectedSet.SObjects[+element]//JSON.parse(element)
         
        this.closeSeries = this.selectedSimulation.History
        this.lastSeries = this.selectedSimulation.Intraday
        this.statistics = this.selectedSimulation.Statistics
        this.monthlyPerformance = this.selectedSimulation.Monthly

        this.updateChart()
    }
    
    updateChart(){
        //this.selectedNode = node;        

        if(this.selectedTimeSeriesType.Type != 'Close'){
            this.onChangeTimeSeries(JSON.stringify(this.selectedTimeSeriesType));
            return;
        }

        let data = this.closeSeries
        var spot = [], vlm = [], opn = [], vol = [], maxdd = [], flags = [], dataLength = data.length;
        for (var i = 0; i < dataLength; i++) {
            spot.push([data[i][0], data[i][1]]);
            vol.push([data[i][0], data[i][2]]);
            maxdd.push([data[i][0], data[i][3]]);
            if (data[i][4] != "") {
                flags.push({ x: data[i][0], title: i + 1, text: data[i][4] });
            }                
        }

        var chart_config = {
            chart: {                    
                zoomType: 'x',
            },
            title: {
                text: null
            },
            credits: {
                enabled: false
            },
            useHighStocks: true,                
            legend: {
                enabled: false
            },
            xAxis: [{
                type: 'datetime',
            }],
            yAxis: [{
                title: {
                    offset: 75,
                    text: 'Performance',
                },
                gridLineColor: 'rgba(154,154,154,0.20)',
                alignTicks: false,
                height: 190,
                lineWidth: 0,
                minPadding: 0.005,
                maxPadding: 0.005,

                // labels: {
                //     margin: 60,
                //     formatter: x => {
                //         console.log(x)
                //         return this.coflows.formatNumber(x.value, true);
                //     }
                // }
            },
            {
                title: {
                    offset: 57,
                    text: 'Drawdown',
                },
                gridLineColor: 'rgba(154,154,154,0.20)',
                alignTicks: false,
                top: 225,
                offset: 0,
                height: 120,
                lineWidth: 0,
                minPadding: 0.005,
                maxPadding: 0.005,
            //    labels: {
            //        formatter: function () {
            //            return formatYAxis(this.value, true) + "%";
            //        }
            //    }
            }
            ],

            tooltip: {
                headerFormat: '<b>{point.key}</b><br />',
                valueDecimals: 2
            },
            options: {
                chart: {
                    zoomType: 'x'
                },
                rangeSelector: {
                    enabled: true
                },
                navigator: {
                    enabled: true
                }
            },
            series: [{
                type: 'line',
                name: 'Performance',
                data: spot,
                shadow: true,
                id: 'dataseries',
                shape: 'none',
                yAxis: 0,
                marker: {
                    enabled: false
                }                    
            },
            {
            //     type: 'area',
            //     name: 'Volatility',
            //     data: vol,
            //     shadow: true,
            //     yAxis: 1,
            //     tooltip: {
            //         valueSuffix: ' %'
            //     },
            //     marker: {
            //         enabled: false
            //     }
            // }, {
                type: 'area',
                name: 'Drawdown',
                data: maxdd,
                shadow: true,
                yAxis: 1,
                tooltip: {
                    valueSuffix: ' %'
                },
                marker: {
                    enabled: false
                }
            }                
            // , {
            //     type: 'flags',
            //     data: flags,
            //     shadow: true,
            //     onSeries: 'dataseries',
            //     shape: 'circlepin',
            //     width: 16
            // }
        ]};

        this.chart = new Chart(chart_config);
        
    }

    
    onChangeTimeSeries(item){
        if(item != null){
            this.selectedTimeSeriesType = JSON.parse(item);
        }
        //this.selectedNode = node;   
              
        //var node = this.selectedNode; 
        if(this.selectedTimeSeriesType.Type == 'Close')
            return this.updateChart()


        let data = this.lastSeries
        var spot = [], dataLength = data.length;
        var count = 0;
        for (var i = count; i < dataLength; i++) {                
            spot.push([data[i][0], data[i][1]]);
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

        this.chart = new Chart(chart);
    }
   
    stringify(item){
        return JSON.stringify(item)
    }


    public tabBeforeChange($event: NgbTabChangeEvent) {        
        this.currentGraphTab = $event.nextId;
    }
}
