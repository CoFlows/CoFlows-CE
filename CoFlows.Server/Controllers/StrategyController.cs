/*
 * The MIT License (MIT)
 * Copyright (c) 2007-2019, Arturo Rodriguez All rights reserved.
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.Authorization;

using Newtonsoft.Json;

using QuantApp.Kernel;
using QuantApp.Engine;

using AQI.AQILabs.Kernel;
using AQI.AQILabs.Kernel.Numerics.Util;
using AQI.AQILabs.SDK.Strategies;

using CoFlows.Server.Utils;

namespace CoFlows.Server.Controllers
{
    [Authorize, Route("[controller]/[action]")]
    public class StrategyController : Controller
    {

        public class SubmitCodeModel
        {
            public int id { get; set; }
            public string code { get; set; }
        }
        [HttpPost]
        public async Task<IActionResult> SubmitCode([FromBody] SubmitCodeModel data)
        {
            string userId = this.User.QID();
            if (userId == null)
                return Unauthorized();

            PortfolioStrategy strat = Instrument.FindInstrument(data.id) as PortfolioStrategy;
            if (strat != null)
            {
                strat.Tree.Initialize();
                strat.Tree.LoadPortfolioMemory(true);

                string res = QuantApp.Engine.Utils.RegisterCode(true, true, (new List<System.Tuple<string, string>>(){ new System.Tuple<string, string>(strat.Name, data.code) }.ToFSharplist()));
                if (res.Contains("Execution successfully completed.") || res.Trim() == "")
                    strat.ScriptCode = data.code;

                return Ok(Newtonsoft.Json.JsonConvert.SerializeObject(res));
            }

            return NotFound(data.id);
        }
                                
        [HttpGet, HttpPost]
        public async Task<IActionResult> Analyse(int id, string command)
        {
            string userId = this.User.QID();
            if (userId == null)
                return Unauthorized();

            Instrument instrument = Instrument.FindInstrument(id);
            Strategy strategy = instrument as Strategy;

            strategy.Tree.Initialize();
            strategy.Tree.LoadPortfolioMemory(true);

            try
            {
                object res = (strategy as PortfolioStrategy).Analyse(command);

                return Ok(res);
            }
            catch(Exception e)
            {
                return BadRequest(e);
            }           
        }

        public class Entry
        {
            public string ID { get; set; }
            public object Value { get; set; }
        }

        [HttpGet]
        public async Task<IActionResult> Package(int id, bool calculate)
        {
            string userId = this.User.QID();
            if (userId == null)
                return Unauthorized();

            PortfolioStrategy strategy = Instrument.FindInstrument(id) as PortfolioStrategy;
            if (strategy == null)
                return NotFound(id);

            strategy.Tree.Initialize();
            strategy.Tree.LoadPortfolioMemory();

            return Ok(strategy.Package(calculate));
        }
        
        [HttpPost]
        public async Task<IActionResult> CalculatePackage(string package)
        {
            string userId = this.User.QID();
            if (userId == null)
                return Unauthorized();

            QuantApp.Kernel.User user = QuantApp.Kernel.User.FindUser(userId);


            var jsonSerializerSettings = new JsonSerializerSettings();
            jsonSerializerSettings.MissingMemberHandling = MissingMemberHandling.Ignore;

            MasterPkg_v1 pkg = Newtonsoft.Json.JsonConvert.DeserializeObject<MasterPkg_v1>(package, jsonSerializerSettings);

            PortfolioStrategy strategy = PortfolioStrategy.Create(pkg, null, new Microsoft.FSharp.Core.FSharpOption<DateTime>(DateTime.Now), null).Item1;

            if (strategy == null)
                return BadRequest();

            return Ok(strategy.Package(true));
        }
        
        [HttpPost]
        public async Task<IActionResult> SimulatePackage(int id, string package)
        {
            string userId = this.User.QID();
            if (userId == null)
                return Unauthorized();
            
            var jsonSerializerSettings = new JsonSerializerSettings();
            jsonSerializerSettings.MissingMemberHandling = MissingMemberHandling.Ignore;

            MasterPkg_v1 pkg = Newtonsoft.Json.JsonConvert.DeserializeObject<MasterPkg_v1>(package, jsonSerializerSettings);

            PortfolioStrategy strategy = PortfolioStrategy.Create(pkg, null, new Microsoft.FSharp.Core.FSharpOption<DateTime>(pkg.InitialDate), null).Item1;


            if (strategy == null)
                return BadRequest();

            DateTime t1 = DateTime.Now;

            string mkey = "--Simulations-" + id;
            QuantApp.Kernel.M m = QuantApp.Kernel.M.Base(mkey);
            var res = m[x => true];

            string entry_key = System.Guid.NewGuid().ToString();

            Thread th = new Thread(() =>
            {
                string SimulationName = t1.ToString();

                object simulation_package = Newtonsoft.Json.JsonConvert.SerializeObject(new
                {
                    //SimKey = entry_key,
                    ID = entry_key,
                    Name = SimulationName,
                    CreationTime = t1,
                    SimulationTime = t1 - t1,
                    PercentDone = 0,
                    History = "",
                    Statistics = "",
                    Allocations = "",
                    Strategy = pkg
                });

                m.AddID(entry_key, simulation_package, null, null, true);

                int memid = Int32.MinValue;
                double value = 0;

                Strategy clone = Extras.SimulateClone(strategy, DateTime.MinValue, memid, value, (x) =>
                {

                    simulation_package = Newtonsoft.Json.JsonConvert.SerializeObject(new
                    {
                        //SimKey = entry_key,
                        ID = entry_key,
                        Name = SimulationName,
                        CreationTime = t1,
                        SimulationTime = DateTime.Now - t1,
                        PercentDone = x,
                        History = "",
                        Statistics = "",
                        Allocations = "",
                        Strategy = pkg
                    });

                    m.ExchangeID(entry_key, simulation_package, null, null);
                    return 0;
                });
                TimeSpan tt = DateTime.Now - t1;

                TimeSeries ts = clone.GetTimeSeries(TimeSeriesType.Last, DataProvider.DefaultProvider, true);

                int days = 20;

                var jres = new List<object[]>();

                if (ts != null && ts.Count != 0)
                {
                    int count = ts.Count;

                    double max = ts[0];
                    for (int i = Math.Max(0, count - 10000); i < count; i++)
                    {
                        DateTime date = ts.DateTimes[i];
                        double spot = ts[i];
                        double vol = (count > days && i >= days ? ts.GetRange(i - days, i).LogReturn().StdDev * Math.Sqrt(252) : 0);
                        max = Math.Max(spot, max);
                        double maxdd = spot / max - 1.0;

                        jres.Add(new object[] { Utils.Utils.ToJSTimestamp(ts.DateTimes[i]), spot, vol * 100, maxdd * 100 });
                    }
                }

                var jres_stats = new List<object>();
                var jres_allocation = new List<object>();

                List<long> dates = new List<long>();
                Dictionary<string, List<double>> allocations = new Dictionary<string, List<double>>();
                allocations.Add("Cash", new List<double>());
                double irr = 0;
                double avg_vol = 0;

                if ((ts != null && ts.Count > 1))
                {
                    //ts = ts.GetRange(ts.DateTimes[0], DateTime.Today, TimeSeries.RangeFillType.None);                

                    List<double> vols = new List<double>();
                    List<double> maxdds = new List<double>();

                    double last_vol = 0.0;
                    double last_dd = 0.0;



                    //if (ts != null && ts.Count != 0)
                    {
                        int count = ts.Count;

                        double max = ts[0];

                        for (int i = 0; i < ts.Count; i++)
                        {
                            double spot = ts[i];

                            double vol = (count > days && i >= days ? ts.GetRange(i - days, i).LogReturn().StdDev * Math.Sqrt(252) : 0);

                            last_vol = vol;
                            max = Math.Max(spot, max);
                            double maxdd = spot / max - 1.0;
                            last_dd = maxdd;

                            if (ts.Count > days && i >= days)
                                vols.Add(vol);

                            maxdds.Add(maxdd);
                        }

                        int ts_count = ts.Count;

                        //jres.Add(new { Measure = "", Value = "", BenchMark = "" });

                        if (ts_count - 2 >= 0)
                            jres_stats.Add(new { Measure = "One Day Performance", Value = ((ts[ts_count - 1] - ts[ts_count - 2]) / ts[ts_count - 2]) });

                        if (ts_count - 6 >= 0)
                            jres_stats.Add(new { Measure = "Five Day Performance", Value = ((ts[ts_count - 1] - ts[ts_count - 6]) / ts[ts_count - 6]) });

                        int idx = ts.GetClosestDateIndex(new DateTime(ts.DateTimes[ts_count - 1].Year, ts.DateTimes[ts_count - 1].Month, 01), TimeSeries.DateSearchType.Previous) + 1;

                        if (idx > 0)
                            jres_stats.Add(new { Measure = "MTD Performance", Value = ((ts[ts_count - 1] - ts[idx - 1]) / ts[idx - 1]) });

                        idx = ts.GetClosestDateIndex(new DateTime(ts.DateTimes[ts_count - 1].Year, 01, 01), TimeSeries.DateSearchType.Previous);

                        if (idx > 0)
                            jres_stats.Add(new { Measure = "YTD Performance", Value = ((ts[ts_count - 1] - ts[idx]) / ts[idx]) });

                        //jres.Add(new { Measure = "", Value = "", BenchMark = "" });

                        jres_stats.Add(new { Measure = "Current Volatility", Value = last_vol });
                        jres_stats.Add(new { Measure = "Average Volatility", Value = vols.Count == 0 ? 0 : vols.Average() });
                        jres_stats.Add(new { Measure = "Maximum Volatility", Value = vols.Count == 0 ? 0 : vols.Max() });
                        jres_stats.Add(new { Measure = "Minimum Volatility", Value = vols.Count == 0 ? 0 : vols.Min() });

                        //jres.Add(new { Measure = "", Value = "", BenchMark = "" });

                        jres_stats.Add(new { Measure = "Current Drawdown", Value = last_dd });
                        jres_stats.Add(new { Measure = "Average Drawdown", Value = maxdds.Count == 0 ? 0 : maxdds.Average() });
                        jres_stats.Add(new { Measure = "Maximum Drawdown", Value = maxdds.Count == 0 ? 0 : maxdds.Min() });

                        //jres.Add(new { Measure = "", Value = "", BenchMark = "" });

                        double t = (ts.DateTimes[ts.Count - 1] - ts.DateTimes[0]).TotalDays / 365;
                        irr = Math.Pow(ts[ts.Count - 1] / ts[0], 1.0 / t) - 1.0;
                        double vol_total = ts.LogReturn().StdDev * Math.Sqrt(252.0);

                        if (!(double.IsNaN(vol_total) || double.IsInfinity(vol_total)))
                            avg_vol = vol_total;

                        if (double.IsNaN(vol_total) || double.IsInfinity(vol_total) || vol_total == 0)
                            vol_total = 1.0;

                        jres_stats.Add(new { Measure = "IRR", Value = irr });
                        jres_stats.Add(new { Measure = "Sharpe Ratio", Value = irr / vol_total });

                        double turnOver = (clone as Strategy) == null ? 0 : AQI.AQILabs.SDK.Strategies.Utils.TurnOver(clone as Strategy, true);//Math.Round(CMStrategies.Utils.GetTurnover(id, ts.DateTimes[0], ts.DateTimes[ts.Count - 1]), 4);
                                                                                                                                               //currentPortfolioId = id;
                                                                                                                                               //currentPortfolioTurnOver = turnOver;
                                                                                                                                               //jres.Add(new object[] { "Turnover", Math.Round(turnOver * 100, 2) + "%", 0.0 });
                        jres_stats.Add(new { Measure = "Turnover", Value = turnOver });




                        ////////////

                        Dictionary<int, Instrument> instruments = clone.Instruments(DateTime.Now, false);
                        for (int i = 0; i < ts.Count; i++)
                        {
                            DateTime date = ts.DateTimes[i];
                            double spot = ts[i];

                            double aum = clone.GetSODAUM(date, TimeSeriesType.Last);

                            //List<Position> pos = clone.Portfolio.RiskPositions(date, false);
                            if (instruments != null)
                                //foreach (Position p in pos)
                                foreach (var ins in instruments.Values)
                                {
                                    Position p = clone.Portfolio.FindPosition(ins, date, false);
                                    double notional_val = 0.0;
                                    //Instrument ins = p.Instrument;

                                    string name = ins.Name;
                                    name = name.Substring(name.LastIndexOf('/') + 1);

                                    if (!allocations.ContainsKey(name))
                                        allocations.Add(name, new List<double>());

                                    if (p != null)
                                    {
                                        if (ins.InstrumentType == InstrumentType.Strategy && (ins as Strategy).Portfolio != null)
                                            notional_val = (ins as Strategy).Portfolio.RiskNotional(date);
                                        else
                                            notional_val = p.NotionalValue(date);
                                    }
                                    else
                                        notional_val = 0;

                                    allocations[name].Add(notional_val / aum);
                                }

                            dates.Add(Utils.Utils.ToJSTimestamp(date));
                            allocations["Cash"].Add(1.0 - clone.Portfolio.RiskNotional(date) / clone.GetSODAUM(date, TimeSeriesType.Last));

                        }

                        foreach (string key in allocations.Keys)
                            jres_allocation.Add(new { name = key, data = allocations[key] });
                    }
                }





                simulation_package = Newtonsoft.Json.JsonConvert.SerializeObject(new
                {
                    ID = entry_key,
                    Name = SimulationName,
                    CreationTime = t1,
                    SimulationTime = tt,
                    PercentDone = 1,
                    History = jres,
                    Statistics = jres_stats,
                    IRR = irr,
                    Vol = avg_vol,
                    Allocations = new
                    {
                        dates = dates,
                        allocation = jres_allocation
                    },
                    Strategy = pkg
                });

                //m += new Entry { ID = t1.ToString(), Value = simulation_package };
                m.ExchangeID(entry_key, simulation_package, null, null);

                m.Save();
            });

            th.Start();

            return Ok("started");
        }
        
        [HttpPost]
        public async Task<IActionResult> RemoveSimulation(int id, string key)
        {
            string userId = this.User.QID();
            if (userId == null)
                return null;

            string mkey = "--Simulations-" + id;
            QuantApp.Kernel.M m = QuantApp.Kernel.M.Base(mkey);

            m.Remove(key);
            //var res = m[x => true];
            m.Save();

            return Ok();           
        }


        private object PortfolioJSON(int id, string date)
        {

            var jres = new List<object>();
            
            Instrument instrument = Instrument.FindInstrument(id);
            DateTime datetime = DateTime.MinValue;
            
            Strategy strategy = instrument as Strategy;
            strategy.Tree.Initialize();
            strategy.Tree.LoadPortfolioMemory();
            //if (strategy.Portfolio.ParentPortfolio == null)
            //    strategy.Simulating = true;

            Portfolio portfolio = strategy.Portfolio;
            if (portfolio == null)
                return null;

            datetime = Calendar.FindCalendar("All").GetClosestBusinessDay(string.IsNullOrWhiteSpace(date) ? DateTime.Now : DateTime.Parse(date), TimeSeries.DateSearchType.Next).DateTime;

            var aggregated = false;

            Strategy main_strategy = strategy;

            
            Dictionary<int, VirtualPosition> virpos = main_strategy.Portfolio.PositionOrders(datetime, aggregated);
            Dictionary<int, Position> positions = new Dictionary<int, Position>();

            List<Position> ps = main_strategy.Portfolio.Positions(datetime, aggregated);

            if (ps != null)
                foreach (Position p in ps)
                    positions.Add(p.Instrument.ID, p);


            var instruments = main_strategy.Instruments(datetime, aggregated);
            if (instruments != null)
            {
                if(ps != null)
                {
                    foreach (var p in ps)
                        if (!main_strategy.Portfolio.IsReserve(p.Instrument) && !instruments.ContainsKey(p.InstrumentID))
                            instruments.Add(p.InstrumentID, p.Instrument);
                }
                foreach (var ins in instruments.Values)
                {
                    VirtualPosition pos = virpos.ContainsKey(ins.ID) ? virpos[ins.ID] : null;
                    var position = main_strategy.Portfolio.FindPosition(ins, datetime);

                    double insvalue = ins.InstrumentType == InstrumentType.Strategy && (ins as Strategy).Portfolio != null ? 1.0 : ins[datetime, TimeSeriesType.Last, TimeSeriesRollType.Last] * (ins is AQI.AQILabs.Kernel.Security ? (ins as AQI.AQILabs.Kernel.Security).PointSize : 1.0);
                    double fx = CurrencyPair.Convert(1.0, datetime, instrument.Currency, ins.Currency);
                    
                    double pointSize = ins is Security ? (ins as Security).PointSize : 1;

                    var sub_positions = new List<object>();

                    if (ins.InstrumentType == InstrumentType.Strategy && (ins as Strategy).Portfolio != null)
                    {
                        var orders = (ins as Strategy).Portfolio.Orders(datetime, true);
                        var os = new List<object>();
                        if (orders != null)
                            foreach (var sos in orders.Values)
                                foreach (var o in sos.Values)
                                    if (Math.Abs(o.Unit) >= 1)
                                        os.Add(new
                                        {
                                            Description = o.Instrument.Description.Substring(o.Instrument.Description.LastIndexOf("/") + 1).Replace("Account: ", ""),
                                            PointSize = o.Instrument is Security ? (o.Instrument as Security).PointSize : 1,
                                            Order = o
                                        });

                        var dailyPnLAdj = (ins as Strategy).Portfolio[datetime, TimeSeriesType.Last, TimeSeriesRollType.Last] - (ins as Strategy).Portfolio[datetime.Date, TimeSeriesType.Last, TimeSeriesRollType.Last];

                        jres.Add(
                        new
                        {
                            ID = ins.ID,
                            Name = ins.Name,
                            Description = ins.Description.Substring(ins.Description.LastIndexOf("/") + 1).Replace("Account: ", ""),
                            Currency = ins.Currency.Name,
                            FX = fx,
                            Value = insvalue,
                            VaR = AQI.AQILabs.SDK.Strategies.Utils.VaR(ins as Strategy, datetime, 1, 60, 0.99, true),
                            PointSize = pointSize,
                            Position = position,
                            Portfolio = PortfolioJSON(ins.ID, date),
                            AggregatedOrders = orders != null ? os : null,
                            DailyPnlAdjustment = dailyPnLAdj
                        });
                    }
                    else
                    {
                        var orders = main_strategy.Portfolio.FindOrder(ins, datetime);

                        var markDate = position == null ? datetime : position.StrikeTimestamp;
                        var markDate_0 = markDate.Date;

                        var dailyPnLAdj = 0.0;
                        if (markDate.Date < datetime.Date)
                        {
                            markDate_0 = position.StrikeTimestamp;
                            markDate = datetime.Date;


                            double insvalue_mark = ins.InstrumentType == InstrumentType.Strategy && (ins as Strategy).Portfolio != null ? 1.0 : ins[markDate, TimeSeriesType.Last, TimeSeriesRollType.Last] * (ins is AQI.AQILabs.Kernel.Security ? (ins as AQI.AQILabs.Kernel.Security).PointSize : 1.0);

                            double insvalue_mark_t = ins.InstrumentType == InstrumentType.Strategy && (ins as Strategy).Portfolio != null ? 1.0 : ins[datetime, TimeSeriesType.Last, TimeSeriesRollType.Last] * (ins is AQI.AQILabs.Kernel.Security ? (ins as AQI.AQILabs.Kernel.Security).PointSize : 1.0);

                            double insvalue_0 = position == null ? 0 : (position.Strike / position.Unit);// ins.InstrumentType == InstrumentType.Strategy && (ins as Strategy).Portfolio != null ? 1.0 : ins[markDate_0, TimeSeriesType.Last, TimeSeriesRollType.Last] * (ins is AQI.AQILabs.Kernel.Security ? (ins as AQI.AQILabs.Kernel.Security).PointSize : 1.0);
                            //var position_carried = main_strategy.Portfolio.FindPosition(ins, markDate_0);
                            dailyPnLAdj = position == null ? 0 : position.Unit * (insvalue_mark - insvalue_0);
                        }
                        else
                        {
                            double insvalue_mark = ins.InstrumentType == InstrumentType.Strategy && (ins as Strategy).Portfolio != null ? 1.0 : ins[markDate, TimeSeriesType.Last, TimeSeriesRollType.Last] * (ins is AQI.AQILabs.Kernel.Security ? (ins as AQI.AQILabs.Kernel.Security).PointSize : 1.0);

                            double insvalue_mark_t = ins.InstrumentType == InstrumentType.Strategy && (ins as Strategy).Portfolio != null ? 1.0 : ins[datetime, TimeSeriesType.Last, TimeSeriesRollType.Last] * (ins is AQI.AQILabs.Kernel.Security ? (ins as AQI.AQILabs.Kernel.Security).PointSize : 1.0);

                            double insvalue_0 = ins.InstrumentType == InstrumentType.Strategy && (ins as Strategy).Portfolio != null ? 1.0 : ins[markDate_0, TimeSeriesType.Last, TimeSeriesRollType.Last] * (ins is AQI.AQILabs.Kernel.Security ? (ins as AQI.AQILabs.Kernel.Security).PointSize : 1.0);
                            var position_carried = main_strategy.Portfolio.FindPosition(ins, markDate_0);
                            var carried_value = position_carried == null ? 0 : position_carried.Unit * (insvalue_mark - insvalue_0);

                            dailyPnLAdj = -carried_value;

                            if (orders != null)
                            {
                                var ordersSorted = orders.Values.Where(o => o.Status == OrderStatus.Booked && (position == null ? true : (o.ExecutionDate < position.StrikeTimestamp) && Math.Abs(o.Unit) > Portfolio._tolerance)).OrderBy(o => o.OrderDate);
                                foreach (var order in ordersSorted)
                                {
                                    if (position != null && Math.Abs(position.Strike / position.Unit - order.ExecutionLevel) < Portfolio._tolerance)
                                        dailyPnLAdj -= position.Unit * (insvalue_mark - order.ExecutionLevel);
                                    else
                                        dailyPnLAdj -= order.Unit * (insvalue_mark - order.ExecutionLevel);
                                }
                            }
                        }
                        
                        jres.Add(
                        new
                        {
                            ID = ins.ID,
                            Name = ins.Name,
                            Description = ins.Description.Substring(ins.Description.LastIndexOf("/") + 1).Replace("Account: ", ""),
                            Currency = ins.Currency.Name,
                            FX = fx,
                            Value = insvalue,
                            VaR = position == null ? 0.0 : AQI.AQILabs.SDK.Strategies.Utils.VaRPosition(ins, datetime, instrument.Currency, 1, 60, 0.99, pointSize * position.Unit),
                            PointSize = pointSize,
                            Position = position,
                            Orders = orders != null ? orders.Values.Where(order => Math.Abs(order.Unit) >= 1) : null,
                            DailyPnlAdjustment = dailyPnLAdj
                        });
                    }


                }
            }

            
            //if (strategy.Portfolio.ParentPortfolio == null)
            //    strategy.Simulating = false;

            return jres;            
        }


        [HttpGet]
        public async Task<IActionResult> PortfolioStructure(int id)
        {
            string userId = this.User.QID();
            if (userId == null)
                return Unauthorized();

            Strategy strategy = Instrument.FindInstrument(id) as Strategy;
            if (strategy == null)
                return NotFound(id);

            strategy.Tree.Initialize();
            strategy.Tree.LoadPortfolioMemory();

            DateTime date =  DateTime.Now;

            var orders = strategy.Portfolio.Orders(date, true);            
            var os = new List<object>();
            if (orders != null)
                foreach (var sos in orders.Values)
                    foreach (var o in sos.Values)
                        if (Math.Abs(o.Unit) >= 1)
                            os.Add(new {
                                Description = o.Instrument.Description.Substring(o.Instrument.Description.LastIndexOf("/") + 1).Replace("Account: ", ""),
                                PointSize = o.Instrument is Security ? (o.Instrument as Security).PointSize : 1,
                                Order = o });
            
            var dailyPnLAdj = strategy[date, TimeSeriesType.Last, TimeSeriesRollType.Last] - strategy[date.Date, TimeSeriesType.Last, TimeSeriesRollType.Last];

            return Ok(
                new
                {
                    ID = strategy.ID,
                    PortfolioID = strategy.PortfolioID,
                    Name = strategy.Name,
                    Description = strategy.Description.Substring(strategy.Description.LastIndexOf("/") + 1).Replace("Account: ", ""),
                    Currency = strategy.Currency.Name,
                    VaR = AQI.AQILabs.SDK.Strategies.Utils.VaR(strategy, date, 1, 60, 0.99, true),
                    Portfolio = PortfolioJSON(id, date.ToString()),
                    AggregatedOrders = orders != null ? os : null,
                    DailyPnlAdjustment = dailyPnLAdj,
                    ScheduleCommand = strategy.ScheduleCommand,
                    Active = strategy.IsSchedulerStarted
                });
        }

        [HttpGet]
        public async Task<IActionResult> HistoricalOrders(int id)
        {
            string userId = this.User.QID();
            if (userId == null)
                return Unauthorized();

            Strategy strategy = Instrument.FindInstrument(id) as Strategy;
            if (strategy == null)
                return NotFound(id);

            strategy.Tree.Initialize();
            strategy.Tree.LoadPortfolioMemory();

            var date = DateTime.Now;
           

            var allOrders = strategy.Portfolio.Orders(true);
            var aos = new List<object>();
            if (allOrders != null)
            {
                var pnlDict = new Dictionary<string, double>();
                //var revOrderedGroups = allOrders.Where(x => Math.Abs(x.Unit) >= 1 && x.Status == OrderStatus.Booked).OrderBy(x => x.OrderDate).GroupBy(x => x.InstrumentID).Select(grp => new { ID = grp.Key, Orders = grp.ToList() }).ToList();
                var revOrderedGroups = allOrders.Where(x => Math.Abs(x.Unit) >= 1 && x.Status == OrderStatus.Booked).GroupBy(x => x.InstrumentID).Select(grp => new { ID = grp.Key, Orders = grp.ToList() }).ToList();
                foreach (var grp in revOrderedGroups)
                {
                    var revOrdered = grp.Orders;
                    for (int i = 0; i < revOrdered.Count; i++)
                    {
                        double pnl = 0.0;
                        var o = revOrdered[i];
                        if (i == revOrdered.Count - 1)
                        {
                            var current_level = o.Instrument[date, TimeSeriesType.Last, TimeSeriesRollType.Last] * (o.Instrument is Security ? (o.Instrument as Security).PointSize : 1);
                            pnl = o.Unit * (current_level - o.ExecutionLevel);
                        }
                        else
                        {
                            var o_next = revOrdered[i + 1];
                            pnl = o.Unit * (o_next.ExecutionLevel - o.ExecutionLevel);
                        }
                        
                        if (pnlDict.ContainsKey(o.ID))
                            pnlDict[o.ID] = pnl;
                        else
                            pnlDict.Add(o.ID, pnl);
                    }
                }
                
                allOrders = allOrders.OrderByDescending(x => x.OrderDate).ToList();

                foreach (var o in allOrders)
                    if (Math.Abs(o.Unit) + Portfolio._tolerance >= 1)
                        aos.Add(new
                        {
                            Description = o.Instrument.Description.Substring(o.Instrument.Description.LastIndexOf("/") + 1).Replace("Account: ", ""),
                            PointSize = o.Instrument is Security ? (o.Instrument as Security).PointSize : 1,
                            Order = o,
                            PnL = pnlDict.ContainsKey(o.ID) ? pnlDict[o.ID] : 0 
                        });
            }
            
            return Ok(allOrders != null ? aos : null);
        }


        [HttpPost]
        public async Task<IActionResult> PortfolioList([FromBody] int[] ids)
        {
            string userId = this.User.QID();
            if (userId == null)
                return Unauthorized();

            DateTime date = DateTime.Now;
            
            var ttype = TimeSeriesType.Close;

            var strats = new List<Strategy>();
            var jres = new List<object>();
            foreach (int id in ids)
            {
                Strategy strategy = Instrument.FindInstrument(id) as Strategy;
                if (strategy != null)
                {
                    strategy.Tree.Initialize();
                    strategy.Tree.LoadPortfolioMemory();

                    strats.Add(strategy);

                    var orders = strategy.Portfolio.Orders(date, true);
                    var os = new List<object>();
                    if (orders != null)
                        foreach (var sos in orders.Values)
                            foreach (var o in sos.Values)
                                if (Math.Abs(o.Unit) >= 1)
                                    os.Add(new
                                    {
                                        Description = o.Instrument.Description.Substring(o.Instrument.Description.LastIndexOf("/") + 1).Replace("Account: ", ""),
                                        PointSize = o.Instrument is Security ? (o.Instrument as Security).PointSize : 1,
                                        Order = o
                                    });

                    var ps = new List<object>();
                    var positions = strategy.Portfolio.Positions(date, true);
                    if (positions != null)
                        foreach (var position in positions)
                        {
                            var ins = position.Instrument;
                            if (!strategy.Portfolio.IsReserve(ins))
                            {
                                double insvalue = ins.InstrumentType == InstrumentType.Strategy && (ins as Strategy).Portfolio != null ? 1.0 : ins[date, TimeSeriesType.Last, TimeSeriesRollType.Last] * (ins is AQI.AQILabs.Kernel.Security ? (ins as AQI.AQILabs.Kernel.Security).PointSize : 1.0);
                                double fx = CurrencyPair.Convert(1.0, date, strategy.Currency, ins.Currency);

                                double pointSize = ins is Security ? (ins as Security).PointSize : 1;

                                ps.Add(
                                new
                                {
                                    ID = ins.ID,
                                    Name = ins.Name,
                                    Description = ins.Description.Substring(ins.Description.LastIndexOf("/") + 1).Replace("Account: ", ""),
                                    Currency = ins.Currency.Name,
                                    FX = fx,
                                    Value = insvalue,
                                    PointSize = pointSize,
                                    Position = position
                                });
                            }

                        }

                    var dailyPnLAdj = strategy.Portfolio[date.Date, TimeSeriesType.Last, TimeSeriesRollType.Last] - strategy.GetTimeSeries(TimeSeriesType.Last)[0];

                    jres.Add(new
                    {
                        ID = strategy.ID,
                        PortfolioID = strategy.PortfolioID,
                        Name = strategy.Name,
                        Description = strategy.Description.Substring(strategy.Description.LastIndexOf("/") + 1).Replace("Account: ", ""),
                        Currency = strategy.Currency.Name,
                        VaR = AQI.AQILabs.SDK.Strategies.Utils.VaR(strategy, date, 1, 60, 0.99, true),                        
                        AggregatedOrders = orders != null ? os : null,
                        AggregatedPositions = positions != null ? ps : null,
                        DailyPnlAdjustment = dailyPnLAdj,
                        ScheduleCommand = strategy.ScheduleCommand,
                        Active = strategy.IsSchedulerStarted
                    });
                }
            }

            var aggregated_ts = Strategy.AggregatedPnL(strats, ttype);

            var ts_jres = new List<object[]>();
            if (aggregated_ts != null && aggregated_ts.Count != 0)
            {
                int count = aggregated_ts.Count;

                double max = 0;
                for (int i = Math.Max(0, count - 10000); i < count; i++)
                {
                    DateTime dt = aggregated_ts.DateTimes[i];
                    double value = aggregated_ts[i];

                    max = Math.Max(value, max);
                    double maxdd = value - max;


                    ts_jres.Add(new object[] { Utils.Utils.ToJSTimestamp(dt), value, maxdd });
                }
            }

            var monthly_jres = new List<object>();

            if (aggregated_ts != null && aggregated_ts.Count != 0)
            {
                DateTime dt_first = aggregated_ts.DateTimes[0];
                DateTime dt_last = aggregated_ts.DateTimes[aggregated_ts.Count - 1];
                DateTime dt_first_clean = new DateTime(dt_first.Year, dt_first.Month, 1);
                DateTime dt_last_clean = new DateTime(dt_last.Year, dt_last.Month, 1);

                int year_last = dt_last.Year;
                int year_first = dt_first.Year;

                for (int year = year_first; year <= year_last; year++)
                {
                    object[] yearly_result = new object[14];

                    yearly_result[0] = year;

                    for (int month = 1; month <= 12; month++)
                    {
                        DateTime dt1 = new DateTime(year, month, 1);


                        DateTime dt2 = new DateTime(year, month, 1).AddMonths(1).AddDays(-1);

                        if (dt1 >= dt_first_clean && dt1 <= dt_last_clean)
                        {
                            dt1 = dt1 >= dt_last ? dt_last : dt1.AddDays(-1);
                            dt1 = dt1 <= dt_first ? dt_first : dt1;

                            if (dt1.AddDays(1).Month == dt1.Month)
                                dt2 = new DateTime(year, month, 1).AddMonths(1).AddDays(-1);

                            dt2 = dt_last > dt2 ? dt2 : dt_last;

                            double v1 = aggregated_ts[dt1, AQI.AQILabs.Kernel.Numerics.Util.TimeSeries.DateSearchType.Previous];
                            double v2 = aggregated_ts[dt2, AQI.AQILabs.Kernel.Numerics.Util.TimeSeries.DateSearchType.Previous];

                            if (!double.IsNaN(v1) && !double.IsNaN(v2) && v1 != 0)
                                yearly_result[month] = (v2 - v1).ToString("##,0.00");
                            else
                                yearly_result[month] = "";
                        }
                        else
                            yearly_result[month] = "";
                    }

                    DateTime dt1_y = new DateTime(year, 1, 1).AddDays(-1);
                    dt1_y = dt_first > dt1_y ? dt_first : dt1_y;

                    DateTime dt2_y = new DateTime(year, 1, 1).AddYears(1).AddDays(-1);
                    dt2_y = dt_last > dt2_y ? dt2_y : dt_last;
                    
                    double yv_abs = aggregated_ts[dt2_y, AQI.AQILabs.Kernel.Numerics.Util.TimeSeries.DateSearchType.Previous] - aggregated_ts[dt1_y, AQI.AQILabs.Kernel.Numerics.Util.TimeSeries.DateSearchType.Previous];

                    yearly_result[13] = yv_abs.ToString("##,0.00");

                    //jres.Add(yearly_result);
                    monthly_jres.Add(new { Year = yearly_result[0], Jan = yearly_result[1], Feb = yearly_result[2], Mar = yearly_result[3], Apr = yearly_result[4], May = yearly_result[5], Jun = yearly_result[6], Jul = yearly_result[7], Aug = yearly_result[8], Sep = yearly_result[9], Oct = yearly_result[10], Nov = yearly_result[11], Dec = yearly_result[12], Yearly = yearly_result[13] });
                }
            }

            var stats_jres = new List<object>();

            var days = 20;

            if (aggregated_ts != null && aggregated_ts.Count > 1)
            {
                //ts = ts.GetRange(ts.DateTimes[0], DateTime.Today, AQI.AQILabs.Kernel.Numerics.Util.TimeSeries.RangeFillType.None);
                //bm_ts = bm_ts.GetRange(ts.DateTimes[0], DateTime.Today, AQI.AQILabs.Kernel.Numerics.Util.TimeSeries.RangeFillType.None);

                List<double> vols = new List<double>();
                List<double> maxdds = new List<double>();

                //List<double> bm_vols = new List<double>();
                //List<double> bm_maxdds = new List<double>();

                double last_vol = 0.0;
                double last_dd = 0.0;

                //double bm_last_vol = 0.0;
                //double bm_last_dd = 0.0;

                //if (ts != null && ts.Count != 0)
                {
                    int count = aggregated_ts.Count;

                    double max = aggregated_ts[0];
                    //double bm_max = bm_ts[0];

                    for (int i = 0; i < aggregated_ts.Count; i++)
                    {
                        double spot = aggregated_ts[i];

                        double vol = (count > days && i >= days ? aggregated_ts.GetRange(i - days, i).DifferenceReturn().StdDev * Math.Sqrt(252) : 0);

                        last_vol = vol;
                        max = Math.Max(spot, max);
                        double maxdd = spot - max;
                        last_dd = maxdd;

                        if (aggregated_ts.Count > days && i >= days)
                            vols.Add(vol);

                        maxdds.Add(maxdd);
                    }

                    int ts_count = aggregated_ts.Count;

                    //jres.Add(new { Measure = "", Value = "", BenchMark = "" });

                    if (ts_count - 2 >= 0)
                        stats_jres.Add(new { Measure = "One Day Performance", Value = aggregated_ts[ts_count - 1] - aggregated_ts[ts_count - 2] });

                    if (ts_count - 6 >= 0)
                        stats_jres.Add(new { Measure = "Five Day Performance", Value = aggregated_ts[ts_count - 1] - aggregated_ts[ts_count - 6] });

                    int idx = aggregated_ts.GetClosestDateIndex(new DateTime(aggregated_ts.DateTimes[ts_count - 1].Year, aggregated_ts.DateTimes[ts_count - 1].Month, 01), AQI.AQILabs.Kernel.Numerics.Util.TimeSeries.DateSearchType.Previous) + 1;
                    //int idx_bm = bm_ts.GetClosestDateIndex(new DateTime(bm_ts.DateTimes[bm_ts.Count - 1].Year, bm_ts.DateTimes[bm_ts.Count - 1].Month, 01), AQI.AQILabs.Kernel.Numerics.Util.TimeSeries.DateSearchType.Previous) + 1;
                    if (idx > 0)// && idx_bm > 0)
                        stats_jres.Add(new { Measure = "MTD Performance", Value = aggregated_ts[ts_count - 1] - aggregated_ts[idx - 1] });

                    idx = aggregated_ts.GetClosestDateIndex(new DateTime(aggregated_ts.DateTimes[ts_count - 1].Year, 01, 01), AQI.AQILabs.Kernel.Numerics.Util.TimeSeries.DateSearchType.Previous);
                    //idx_bm = bm_ts.GetClosestDateIndex(new DateTime(bm_ts.DateTimes[bm_ts.Count - 1].Year, 01, 01), AQI.AQILabs.Kernel.Numerics.Util.TimeSeries.DateSearchType.Previous);
                    if (idx > 0)// && idx_bm > 0)
                        stats_jres.Add(new { Measure = "YTD Performance", Value = aggregated_ts[ts_count - 1] - aggregated_ts[idx] });

                    //jres.Add(new { Measure = "", Value = "", BenchMark = "" });

                    stats_jres.Add(new { Measure = "Current Volatility", Value = last_vol });
                    stats_jres.Add(new { Measure = "Average Volatility", Value = vols.Count == 0 ? 0 : vols.Average() });
                    stats_jres.Add(new { Measure = "Maximum Volatility", Value = vols.Count == 0 ? 0 : vols.Max() });
                    stats_jres.Add(new { Measure = "Minimum Volatility", Value = vols.Count == 0 ? 0 : vols.Min() });

                    //jres.Add(new { Measure = "", Value = "", BenchMark = "" });

                    stats_jres.Add(new { Measure = "Current Drawdown", Value = last_dd });
                    stats_jres.Add(new { Measure = "Average Drawdown", Value = maxdds.Count == 0 ? 0 : maxdds.Average() });
                    stats_jres.Add(new { Measure = "Maximum Drawdown", Value = maxdds.Count == 0 ? 0 : maxdds.Min() });

                    //jres.Add(new { Measure = "", Value = "", BenchMark = "" });

                    double t = (aggregated_ts.DateTimes[aggregated_ts.Count - 1] - aggregated_ts.DateTimes[0]).TotalDays / 365;
                    double irr = aggregated_ts[aggregated_ts.Count - 1] / t;
                    double vol_total = aggregated_ts.DifferenceReturn().StdDev * Math.Sqrt(252.0);
                    if (double.IsNaN(vol_total) || double.IsInfinity(vol_total) || vol_total == 0)
                        vol_total = 1.0;

                    
                    stats_jres.Add(new { Measure = "Annual Return", Value = irr});
                    stats_jres.Add(new { Measure = "Sharpe Ratio", Value = irr / vol_total });

                    //double turnOver = AQI.AQILabs.SDK.Strategies.Utils.TurnOver(instrument as Strategsy, true);//Math.Round(CMStrategies.Utils.GetTurnover(id, ts.DateTimes[0], ts.DateTimes[ts.Count - 1]), 4);
                    //currentPortfolioId = id;
                    //currentPortfolioTurnOver = turnOver;
                    //jres.Add(new object[] { "Turnover", Math.Round(turnOver * 100, 2) + "%", 0.0 });
                    //stats_jres.Add(new { Measure = "Turnover", Value = Math.Round(turnOver * 100.0, 2) + "%", BenchMark = "" });
                }
            }


            var VaR = AQI.AQILabs.SDK.Strategies.Utils.VaRAggregated(strats, date, 1, 60, 0.99, false);


            return Ok(new { VaR = VaR, TimeSeries = ts_jres, MonthlyPerformance = monthly_jres, Statistics = stats_jres, Strategies = jres});            
        }

        [HttpGet]
        public async Task<IActionResult> Indicator(int id, int uid, int iid, int bid)
        {
            string userId = this.User.QID();
            if (userId == null)
                return Unauthorized();


            QuantApp.Kernel.User user = QuantApp.Kernel.User.FindUser(userId);

            Strategy strategy = Instrument.FindInstrument(id) as Strategy;
            Instrument underlying = Instrument.FindInstrument(uid);
            if (underlying is Future)
                underlying = (underlying as Future).Underlying;
            else if(underlying is RollingFutureStrategy)
                underlying = (underlying as RollingFutureStrategy).UnderlyingInstrument;

            var indicator_ts = strategy.GetMemorySeries(underlying.ID, iid);
            var benchmark_ts = strategy.GetMemorySeries(underlying.ID, bid);
            
            var jres = new List<object[]>();

            if (indicator_ts != null && indicator_ts.Count != 0)
            {
                int count = indicator_ts.Count;
                
                for (int i = Math.Max(0, count - 10000); i < count; i++)
                {
                    DateTime date = indicator_ts.DateTimes[i];
                    double indicator = indicator_ts[i];
                    double benchmark = bid == 0 ? 0 : benchmark_ts[i];

                    jres.Add(new object[] { Utils.Utils.ToJSTimestamp(date), indicator, benchmark });
                }
            }

            return Ok(jres);
        }


    }
}