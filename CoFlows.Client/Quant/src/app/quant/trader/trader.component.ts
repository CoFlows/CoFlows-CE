import { Chart } from 'angular-highcharts';
import { Component } from '@angular/core';
import { NgbTabChangeEvent } from '@ng-bootstrap/ng-bootstrap';
import { CoFlowsComponent } from '../../coflows/core/coflows.component';
// import {TreeTableModule} from 'primeng/treetable';
import { TreeNode } from 'primeng/api';

import { Router, ActivatedRoute, Params } from '@angular/router';

import {Observable, interval} from "rxjs"

import 'codemirror/mode/mllike/mllike.js';
import 'codemirror/mode/python/python.js';



// import 'codemirror/addon/display/panel.js';
// import 'codemirror/addon/display/fullscreen.js';
// import 'codemirror/addon/display/autorefresh.js';

import 'codemirror/addon/fold/foldgutter.js';

import 'codemirror/addon/fold/brace-fold.js';
import 'codemirror/addon/fold/comment-fold.js';
import 'codemirror/addon/fold/indent-fold.js';
import 'codemirror/addon/fold/markdown-fold.js';
import 'codemirror/addon/fold/xml-fold.js';
import 'codemirror/addon/fold/foldcode.js';
import * as CodeMirror from 'codemirror/lib/codemirror.js';


@Component({    
    selector: 'trader',
    styles: [`
  :host >>> .CodeMirror {
    height: auto;
  }

  .tree-row1 {
    border: none;
  }
  `],
    templateUrl: 'trader.component.html',
})


export class TraderComponent {

    // MainStrategyID = 90086;// LIVE
    //MainStrategyID = 90900;//CME Metals
    //MainStrategyID = 91164 //CME Metals + Crude
    MainStrategyID = 91258
    //MainStrategyID = 93437//Live Testing
    //MainStrategyID = 93250//Convexity
    // MainStrategyID = 93507//Coal MACD
    PortfolioID = 0
    chart_indicator = {};    
    chart = {};
    
    strategyData: TreeNode[] = [{}]
    portfolioData: TreeNode[] = [{}]
    orderData: TreeNode[] = [{}]
    monthlyPerformance: any = {}
    historicalOrders: any = {}
    statistics: any = [{}]

    code = '';
    compilationResult = '';

    indicators = {}//[{ Name: 'Empty', Indicators: [{Name: 'Empty', ID: 0, BID: 0}] }]
    timeSeriesType = [{Type: 'Close'}, {Type: 'Intraday'}, {Type: 'Live'}]
    selectedTimeSeriesType = this.timeSeriesType[0]
    selectedIndicator = this.indicators[0]
    selectedNode: any = {};
    currentGraphTab = '';
    hasIndicator = false;
    
    editorOptions = {
        lineWrapping: false,
        lineNumbers: true,
        //matchBrackets: true,
        // styleActiveLine: true,
        //readOnly: false,        
        mode: 'text/x-fsharp',
        //viewportMargin: Infinity,
        // foldGutter: true,
        // gutters: ["foldgutter"],
        
        // foldGutter: true,
        gutters: ["CodeMirror-linenumbers", "CodeMirror-foldgutter"],
        foldGutter: {
            rangeFinder: CodeMirror.fold.combine(CodeMirror.fold.indent, CodeMirror.fold.comment)             
        },
        
        indentUnit: 4,
        extraKeys:{
            Tab:  cm => {
                if (cm.doc.somethingSelected()) {
                    return CodeMirror.Pass;
                }
                var spacesPerTab = cm.getOption("indentUnit");
                console.log(spacesPerTab)
                var spacesToInsert = spacesPerTab - (cm.doc.getCursor("start").ch % spacesPerTab);    
                var spaces = Array(spacesToInsert + 1).join(" ");
                cm.replaceSelection(spaces, "end", "+input");
            }
        },
        
    };

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
    
    private positions2Tree(strategy, expand) {
        
        var positions = [];

        if (strategy.Portfolio != undefined) {
            strategy.Portfolio.forEach((element) => {    
                positions.push(this.positions2Tree(element, false));
            });
        }

        let node : TreeNode = {
            data: {
                Name: strategy.Description,
                ID: strategy.ID,
                Currency: strategy.Currency,
                Position: strategy.Position,
                PointSize: strategy.PointSize,
                DailyPnlAdjustment: strategy.DailyPnlAdjustment,
                VaR: this.coflows.formatNumber(strategy.VaR, false)
            },
            children: positions
        };

        node.expanded = expand;
        return node;
    }

    private orders2Tree(strategy, expand) {
        
        var positions = [];

        if (strategy.Portfolio != undefined) {
            strategy.Portfolio.forEach((element) => {                    
                if(element.AggregatedOrders != null && element.AggregatedOrders.length > 0){
                    positions.push(this.orders2Tree(element, false));
                }
            });
        }

        if (strategy.AggregatedOrders != undefined) {
            strategy.AggregatedOrders.forEach((element) => {    
                var level = '';
                var status = '';
                if(element.Order.Status == 0 ){
                    status = 'Created: '
                    level = 'New'
                }
                else if(element.Order.Status == 1){
                    status = 'Submitted: '
                    level = 'Working'
                }
                else if(element.Order.Status == 2){
                    status = 'Filled: '
                    level = 'Filled@' + this.coflows.formatNumber(element.Order.ExecutionLevel / element.PointSize,false)
                }
                else if(element.Order.Status == 3){
                    status = 'Filled: '
                    level = 'Booked@' + this.coflows.formatNumber(element.Order.ExecutionLevel / element.PointSize,false)
                }
                else if(element.Order.Status == 4){                    
                    level = 'Cancelled'
                }
                let node : TreeNode = {
                    data: {
                        Name: element.Description,
                        ID: element.Order.ID,
                        Date: status + (element.Order.ExecutionLevel > 0 ? element.Order.ExecutionDate : element.Order.OrderDate),                        
                        Level: level,                        
                        Unit: element.Order.Unit,
                    },
                    children: []//positions
                };
                if(element.Order.Status != 4)
                    positions.push(node);
                
            });
        }

        let node : TreeNode = {
            data: {
                Name: strategy.Description,
                ID: strategy.ID,
                OrderDate: '',
                ExecutionDate: '',
                ExecutionLevel: '',
                Unit: '',
            },
            children: positions
        };

        node.expanded = expand;
        return node;
    }
    
    openPnl(node){
        if(node.children != undefined && node.children.length > 0){
            let agg_pnl = 0;
            node.children.forEach(item => agg_pnl += this.openPnl(item))            
            return agg_pnl;
        }
        else if(node.data != undefined && node.data.Position != undefined && node.data.Position != null)
            return this.coflows.FindInstrument(node.data.ID).Last * node.data.PointSize * node.data.Position.Unit -  node.data.Position.Strike;
        else
            return 0;
    }

    dailyPnl(node){
        if(node.children != undefined && node.children.length > 0){
            let agg_pnl = 0;
            node.children.forEach(item => agg_pnl += this.dailyPnl(item))            
            return agg_pnl;
        }
        else if(node.data != undefined && node.data.Position != undefined && node.data.Position != null)
            return this.coflows.FindInstrument(node.data.ID).Last * node.data.PointSize * node.data.Position.Unit -  node.data.Position.Strike - node.data.DailyPnlAdjustment;
        else if(node.data != undefined)
            return -node.data.DailyPnlAdjustment;
        
        else
            return 0;
    }

    notional(node, abs){
        if(node.children != undefined && node.children.length > 0){
            let agg_notional = 0;
            node.children.forEach(item => agg_notional += this.notional(item, abs))            
            return agg_notional;
        }
        else if(node.data != undefined && node.data.Position != undefined && node.data.Position != null)
            return (abs ? Math.abs(this.coflows.FindInstrument(node.data.ID).Last * node.data.PointSize * node.data.Position.Unit) : this.coflows.FindInstrument(node.data.ID).Last * node.data.PointSize * node.data.Position.Unit);
        else
            return 0;
    }

    lots(node,abs){
        if(node.children != undefined && node.children.length > 0){
            let agg_lots = 0;
            node.children.forEach(item => agg_lots += this.lots(item, abs))
            
            return agg_lots;
        }
        else if(node.data != undefined && node.data.Position != undefined && node.data.Position != null)
            return (!abs ? node.data.Position.Unit : Math.abs(node.data.Position.Unit));
        else
            return 0;
    }

    traded(node){        
        let agg_lots = 0;
        if(node != undefined && node.children != undefined && node.children.length > 0){            
            node.children.forEach(item => agg_lots += (item.data.Unit != '' && item.data.Level.includes('Booked@') ? Math.abs(item.data.Unit) : 0))
        }
        
        return agg_lots;
    }

    constructor(private activatedRoute: ActivatedRoute, public coflows: CoFlowsComponent) {
        this.activatedRoute.params.subscribe(params => {
            let id = +params['id'];
            this.MainStrategyID = id;
        
            

            var strategy = this.coflows.FindInstrument(this.MainStrategyID);

            var LastPortfolioUpdate = strategy.LastPortfolioUpdate;
            interval(1000)        
            .subscribe(i => { 
                if(LastPortfolioUpdate != strategy.LastPortfolioUpdate){
                    LastPortfolioUpdate = strategy.LastPortfolioUpdate;
                    this.coflows.Get('strategy/portfoliostructure?id=' + this.MainStrategyID,
                    data => {
                        this.portfolioData = null;
                        this.orderData = null;

                        this.portfolioData = <TreeNode[]>[this.positions2Tree(data, true)];                
                        this.orderData = <TreeNode[]>[this.orders2Tree(data, true)];  
                    });
                    
            
                }
            });
            
            this.coflows.LinkAction(this.MainStrategyID + '-PortfolioStrategy-MetaData', 
                data => { //Load
                    if(data[0].Value.Code != null)
                        this.code = data[0].Value.Code; 
                },
                data => { //Add
                    if(data.Value.Code != null)
                        this.code = data.Value.Code; 
                },
                data => { //Exchange
                    if(data.Value.Code != null)
                        this.code = data.Value.Code; 
                },
                data => { //Remove
                }
            );

            this.coflows.Get('strategy/portfoliostructure?id=' + this.MainStrategyID,
            data => {
                console.log(data)

                var strategy = this.coflows.FindInstrument(this.MainStrategyID)
                strategy.IsActive = data.Active;
                strategy.ScheduleCommand = data.ScheduleCommand;

                this.portfolioData = <TreeNode[]>[this.positions2Tree(data, true)];
                this.orderData = <TreeNode[]>[this.orders2Tree(data, true)];
                this.updateChart(this.portfolioData[0]);

                CoFlowsComponent.UpdateInstruments = true
            });

            this.coflows.Get('strategy/historicalorders?id=' + this.MainStrategyID,
            data => {                
                this.historicalOrders = data                
            });

            this.coflows.Get('instrument/statistics?id=' + this.MainStrategyID + '&days=20',
            data => {          
                this.statistics = data
            });

            this.coflows.Get('strategy/analyse?id=' + this.MainStrategyID + '&command=indicators',
            data => {          
                //console.log(data)      
                this.indicators = {}        
                data.forEach(x => {
                    this.indicators[x.Name] = x.Indicators
                });
                //console.log(this.indicators)
                this.selectedIndicator = this.indicators[data[0].Name]
                //console.log(this.selectedIndicator)
            });

            

        });
    }
    
    updateChart(node){
        //console.log(node)
        this.selectedNode = node;        
        this.updateIndicator(node)

        if(this.selectedTimeSeriesType.Type != 'Close'){
            this.onChangeTimeSeries(JSON.stringify(this.selectedTimeSeriesType));
            return;
        }

        let id = node.data.ID

        this.coflows.Get('instrument/monthlyperformance?id=' + id,
        data => {
            this.monthlyPerformance = data
            //console.log(data)
        })

        this.coflows.Get('instrument/statistics?id=' + id + '&days=20',
        data => {          
            this.statistics = data
        });

        this.coflows.Get('instrument/timeseries?id=' + id + '&days=20&spot_type=spot_close',
        data => {
            var spot = [], vlm = [], opn = [], vol = [], maxdd = [], flags = [], dataLength = data.length;
            for (var i = 0; i < dataLength; i++) {
                spot.push([data[i][0], data[i][1]]);
                vol.push([data[i][0], data[i][2]]);
                maxdd.push([data[i][0], data[i][3]]);
                if (data[i][4] != "") {
                    flags.push({ x: data[i][0], title: i + 1, text: data[i][4] });
                }                
            }

            if(node.children.length != 0){            
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
            else{

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
                            text: 'Drawdown / Volatility',
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
                        type: 'area',
                        name: 'Volatility',
                        data: vol,
                        shadow: true,
                        yAxis: 1,
                        tooltip: {
                            valueSuffix: ' %'
                        },
                        marker: {
                            enabled: false
                        }
                    }, {
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
        });
    }

    
    onChangeTimeSeries(item){
        if(item != null){
            this.selectedTimeSeriesType = JSON.parse(item);
        }
        //this.selectedNode = node;   
              
        var node = this.selectedNode; 
        if(this.selectedTimeSeriesType.Type == 'Close')
            return this.updateChart(this.selectedNode)
        
        this.updateIndicator(node)
        let id = node.data.ID
        this.coflows.Get('instrument/intradaytimeseries?id=' + id + '',
        data => {
            var spot = [], dataLength = data.length;
            var count = this.selectedTimeSeriesType.Type == 'Live' ? dataLength - 100 : 0;
            for (var i = count; i < dataLength; i++) {                
                spot.push([data[i][0], data[i][1]]);
            }

            var instrument = this.coflows.FindInstrument(id)
            var isLive = this.selectedTimeSeriesType.Type == 'Live';
            var chart = {
                chart: {
                    type: 'spline',
                    zoomType: 'x',
                    marginRight: 10,
                    events: {
                        load: function () {
                            if(isLive){
                                //set up the updating of the chart each second                                        
                                interval(1000)        
                                .subscribe(i => { 
                                    if(this.series != undefined){
                                        var series = this.series[0];
                                        var last = series[series.length - 1];
                                        if(instrument.Last > 0 && last != instrument.Last){                                        
                                            var x = (new Date()).getTime(), // current time
                                                y = instrument.Last
                                            series.addPoint([x, y], true, true);
                                        }
                                    }
                                });
                            }
        
                        }
                    }
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

        });
    }
   
    updateIndicator(node){
        this.onChangeIndicator(null);
    }

    stringify(item){
        return JSON.stringify(item)
    }

    onChangeIndicator(item){
        if(item != null){
            this.selectedIndicator = JSON.parse(item)
        }
        else {
            this.selectedIndicator = this.indicators[this.selectedNode.data.Name]
        }
            
        let node = this.selectedNode
        
        
        this.coflows.Get('strategy/historicalorders?id=' + node.data.ID,
        data => {
            this.historicalOrders = null
            this.historicalOrders = data
        });
        if(node.parent == undefined){
            this.hasIndicator = false;
            return;
        }

        let id = node.parent.data.ID
        let uid = node.data.ID

        let iid = this.selectedIndicator.length > 0 ? this.selectedIndicator[0].ID : this.selectedIndicator.ID//this.indicators[node.data.Name].ID
        let bid = this.selectedIndicator.length > 0 ? this.selectedIndicator[0].BID : this.selectedIndicator.BID//this.indicators[node.data.Name].BID

        this.coflows.Get('strategy/indicator?id=' + id + '&uid=' + uid + '&iid=' + iid + '&bid=' + bid,
        data => {   
            // if(data.length > 0)
            this.hasIndicator = true;
            // else
            //     this.hasIndicator = false;
                
            var indicator = [], benchmark = [],dataLength = data.length;
            for (var i = 0; i < dataLength; i++) {
                indicator.push([data[i][0], data[i][1]]);
                benchmark.push([data[i][0], data[i][2]]);
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
                    //height: 190,
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
                }],

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
                    dashStyle: 'Dash',
                    
                    name: 'Indicator',
                    data: indicator,
                    shadow: false,
                    id: 'dataseries',
                    shape: 'none',
                    yAxis: 0,
                    marker: {
                        enabled: false
                    }                    
                },
                {
                    type: 'line',
                    name: 'Benchmark',
                    data: benchmark,
                    shadow: true,
                    id: 'dataseries',
                    shape: 'none',
                    yAxis: 0,
                    marker: {
                        enabled: false
                    }                    
                }                    
            ]};

            this.chart_indicator = new Chart(chart_config);
        });
    }

    onChangeActive(item){        
        var mess = {};
        if(item == 0){            
            mess = {
                Type: 10, //Message Type Function
                Content: { ID: this.MainStrategyID, Name: "StartScheduler", Parameters: null }
            };
        }
        else{
            mess = {
                Type: 10, //Message Type Function
                Content: { ID: this.MainStrategyID, Name: "StopScheduler", Parameters: null }
            }; 
        }
        this.coflows.Send(mess);
    }

    onChangeSchedule(item){
        var schedule = item.path[0].value
        
        var mess = {
            Type: 9, //Message Type Property
            Content: { ID: this.MainStrategyID, Name: "ScheduleCommand", Value: schedule }
        };
        
        this.coflows.Send(mess);
    }

    submitCode(){
        this.coflows.Post('strategy/submitcode', 
        {
            id: this.MainStrategyID,
            code: this.code
        },
        data => {                        
            this.compilationResult = data;
        });
    }

    public tabBeforeChange($event: NgbTabChangeEvent) {        
        this.currentGraphTab = $event.nextId;
    }
}
