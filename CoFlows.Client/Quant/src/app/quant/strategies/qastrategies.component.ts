import { Component } from '@angular/core';
import { Chart } from 'angular-highcharts';

import { CoFlowsComponent } from '../../coflows/core/coflows.component';

@Component({
  selector: 'quantapp-strategies',
  
  templateUrl: './qastrategies.component.html',
  styles: []
})

export class QAStrategiesComponent {
    strategies = []
    AggregatedVaR = 0
    activeChoices = [true, false];
    chart : any = {}
    monthlyPerformance: any = {}
    statistics: any = [{}]

    notional(node, abs){
        let agg_notional = 0;

        node.forEach(element => {
            agg_notional += (abs ? Math.abs(this.coflows.FindInstrument(element.ID).Last * element.PointSize * element.Position.Unit) : this.coflows.FindInstrument(element.ID).Last * element.PointSize * element.Position.Unit);
        });

        return agg_notional;
    }

    contracts(node, abs){
        let agg_notional = 0;

        node.forEach(element => {
            agg_notional += (abs ? Math.abs(element.Position.Unit) : element.Position.Unit);
        });

        return agg_notional;
    }

    totalNotional(rows, abs){
        let agg_notional = 0;

        rows.forEach(node =>{
            node.positions.forEach(element => {
                agg_notional += (abs ? Math.abs(this.coflows.FindInstrument(element.ID).Last * element.PointSize * element.Position.Unit) : this.coflows.FindInstrument(element.ID).Last * element.PointSize * element.Position.Unit);
            });
        });

        return agg_notional;
    }

    totalContracts(rows, abs){
        let agg_notional = 0;

        rows.forEach(node =>{
            node.positions.forEach(element => {
                agg_notional += (abs ? Math.abs(element.Position.Unit) : element.Position.Unit);
            });
        });

        return agg_notional;
    }

    totalPnL(rows){
        let agg_notional = 0;

        rows.forEach(node =>{
            agg_notional += this.coflows.FindInstrument(node.id).Last - node.dailyAdjustment
        });

        return agg_notional;
    }

    constructor(private coflows: CoFlowsComponent) {
    }

    SetStrategies(strategyIDs){
        this.coflows.Post('strategy/portfoliolist',strategyIDs,
        data => {
            
            this.strategies = []

            this.AggregatedVaR = data.VaR;
            data.Strategies.forEach(element => {
                let item = {
                    active: element.Active,
                    strategy: element.Description,
                    dailyAdjustment: element.DailyPnlAdjustment,                
                    id: element.ID,
                    var: element.VaR,
                    orders: element.AggregatedOrders,
                    positions: element.AggregatedPositions
                }

                this.coflows.FindInstrument(element.ID).IsActive = element.Active
                this.strategies.push(item)
            });

            this.monthlyPerformance = data.MonthlyPerformance
            this.statistics = data.Statistics
            
            var spot = [], maxdd = [], dataLength = data.TimeSeries.length;        
            for (var i = 0; i < dataLength; i++) {                
                spot.push([data.TimeSeries[i][0], data.TimeSeries[i][1]]);
                maxdd.push([data.TimeSeries[i][0], data.TimeSeries[i][2]]);
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

            CoFlowsComponent.UpdateInstruments = true
        });
    }

    onChangeActiveStrategy(id, item){     
        console.log(id, item)
            var mess = {};
            if(item){            
                mess = {
                    Type: 10, //Message Type Function
                    Content: { ID: id, Name: "StartScheduler", Parameters: null }
                };
            }
            else{
                mess = {
                    Type: 10, //Message Type Function
                    Content: { ID: id, Name: "StopScheduler", Parameters: null }
                }; 
            }
            this.coflows.Send(mess);
    }

    onChangeActiveFunction(id, item){     
        console.log(id, item)

        this.coflows.Get('m/activetoggle?id=' + id ,
            data => {
                console.log(data)

            });

    }
}
