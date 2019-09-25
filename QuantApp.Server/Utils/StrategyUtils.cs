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

using AQI.AQILabs.Kernel;
using AQI.AQILabs.Kernel.Numerics.Util;
using AQI.AQILabs.SDK.Strategies;

namespace QuantApp.Server.Utils
{ 
    public class StrategyUtils
    {
        public static long ToUnixTimestamp(System.DateTime dt)
        {
            DateTime unixRef = new DateTime(1970, 1, 1, 0, 0, 0);
            return (dt.Ticks - unixRef.Ticks) / 10000000;
        }

        public static DateTime FromUnixTimestamp(long timestamp)
        {
            DateTime unixRef = new DateTime(1970, 1, 1, 0, 0, 0);
            return unixRef.AddSeconds(timestamp);
        }
        
        public static long ToJSTimestamp(System.DateTime dt)
        {
            DateTime unixRef = new DateTime(1970, 1, 1, 0, 0, 0);
            return (long)(dt - unixRef).TotalMilliseconds;
        }
        private static object  Simulation2JSON(Strategy strategy)
        {
            TimeSeries ts = strategy.GetTimeSeries(TimeSeriesType.Last, DataProvider.DefaultProvider, true);
            int count = ts.Count;
            var t0 = ts[Math.Max(0, count - 10000)];

            var jres_intraday = new List<object[]>();

            if (Instrument.TimeSeriesLoadFromDatabaseIntraday  && ts != null && ts.Count != 0)
            {

                double max = ts[0];
                for (int i = Math.Max(0, count - 10000); i < count; i++)
                {
                    DateTime date = ts.DateTimes[i];
                    double spot = ts[i] - t0;

                    jres_intraday.Add(new object[] { ToJSTimestamp(ts.DateTimes[i]), spot });
                }
            }

            ts = strategy.GetTimeSeries(TimeSeriesType.Close, DataProvider.DefaultProvider, true);

            int days = 20;

            var jres = new List<object[]>();

            if (ts != null && ts.Count != 0)
            {
                count = ts.Count;
                t0 = ts[Math.Max(0, count - 10000)];

                double max = ts[0] - t0;
                for (int i = Math.Max(0, count - 10000); i < count; i++)
                {
                    DateTime date = ts.DateTimes[i];
                    double spot = ts[i] - t0;
                    double vol = (count > days && i >= days ? ts.GetRange(i - days, i).LogReturn().StdDev * Math.Sqrt(252) : 0);
                    max = Math.Max(spot, max);
                    double maxdd = spot - max;

                    jres.Add(new object[] { ToJSTimestamp(ts.DateTimes[i]), spot, vol * 100, maxdd });
                }
            }


            Dictionary<string, double> total_count_instrument = new Dictionary<string, double>();
            Dictionary<string, double> positive_count_instrument = new Dictionary<string, double>();
            Dictionary<string, double> negative_count_instrument = new Dictionary<string, double>();
            Dictionary<string, double> positive_value_instrument = new Dictionary<string, double>();
            Dictionary<string, double> negative_value_instrument = new Dictionary<string, double>();

            double total_count = 0.0;
            double positive_count = 0.0;
            double negative_count = 0.0;

            var allOrders = strategy.Portfolio.Orders(true);
            var aos = new List<object>();
            if (allOrders != null)
            {
                var date = DateTime.Now;
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
                    {
                        
                        string name = o.Instrument.InstrumentType == InstrumentType.Future ? (o.Instrument as Future).Underlying.Name : o.Instrument.Name;
                        double pnl = pnlDict.ContainsKey(o.ID) ? pnlDict[o.ID] : 0;
                        total_count++;
                        if (total_count_instrument.ContainsKey(name))
                            total_count_instrument[name]++;
                        else
                            total_count_instrument.Add(name, 1);

                        Console.WriteLine(o + " ---> " + total_count_instrument[name]);

                        if (pnl > 0)
                        {
                            positive_count++;
                            if (positive_count_instrument.ContainsKey(name))
                                positive_count_instrument[name]++;
                            else
                                positive_count_instrument.Add(name, 1);

                            if (positive_value_instrument.ContainsKey(name))
                                positive_value_instrument[name] += pnl;
                            else
                                positive_value_instrument.Add(name, pnl);

                        }
                        else
                        {
                            negative_count++;
                            if (negative_count_instrument.ContainsKey(name))
                                negative_count_instrument[name]++;
                            else
                                negative_count_instrument.Add(name, 1);

                            if (negative_value_instrument.ContainsKey(name))
                                negative_value_instrument[name] += pnl;
                            else
                                negative_value_instrument.Add(name, pnl);
                        }
                    }
            }



            var jres_stats = new List<object>();
            double irr = 0;
            //double avg_vol = 0;

            if ((ts != null && ts.Count > 1))
            {
                List<double> vols = new List<double>();
                List<double> maxdds = new List<double>();

                double last_vol = 0.0;
                double last_dd = 0.0;

                {
                    count = ts.Count;

                    double max = 0;

                    for (int i = 0; i < ts.Count; i++)
                    {
                        double spot = ts[i] - ts[0];

                        double vol = (count > days && i >= days ? ts.GetRange(i - days, i).DifferenceReturn().StdDev * Math.Sqrt(252) : 0);

                        last_vol = vol;
                        max = Math.Max(spot, max);
                        double maxdd = spot - max;
                        last_dd = maxdd;

                        if (ts.Count > days && i >= days)
                            vols.Add(vol);

                        maxdds.Add(maxdd);
                    }

                    int ts_count = ts.Count;

                    //jres.Add(new { Measure = "", Value = "", BenchMark = "" });

                    if (ts_count - 2 >= 0)
                        jres_stats.Add(new { Measure = "One Day Performance", Value = ts[ts_count - 1] - ts[ts_count - 2] });

                    if (ts_count - 6 >= 0)
                        jres_stats.Add(new { Measure = "Five Day Performance", Value = ts[ts_count - 1] - ts[ts_count - 6] });

                    int idx = ts.GetClosestDateIndex(new DateTime(ts.DateTimes[ts_count - 1].Year, ts.DateTimes[ts_count - 1].Month, 01), TimeSeries.DateSearchType.Previous) + 1;

                    if (idx > 0)
                        jres_stats.Add(new { Measure = "MTD Performance", Value = ts[ts_count - 1] - ts[idx - 1] });

                    idx = ts.GetClosestDateIndex(new DateTime(ts.DateTimes[ts_count - 1].Year, 01, 01), TimeSeries.DateSearchType.Previous);

                    if (idx > 0)
                        jres_stats.Add(new { Measure = "YTD Performance", Value = ts[ts_count - 1] - ts[idx] });

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
                    irr = (ts[ts.Count - 1] - ts[0]) / t;
                    double vol_total = ts.DifferenceReturn().StdDev;// * Math.Sqrt(252.0);
                    if (double.IsNaN(vol_total) || double.IsInfinity(vol_total) || vol_total == 0)
                        vol_total = 1.0;


                    jres_stats.Add(new { Measure = "Annual Return", Value = irr });
                    jres_stats.Add(new { Measure = "Sharpe Ratio", Value = irr / vol_total });
                    //jres_stats.Add(new { Measure = "Sharpe Ratio", Value = (ts[ts.Count - 1] - ts[0]) / vol_total });

                    jres_stats.Add(new { Measure = "Total Trades", Value = total_count });
                    jres_stats.Add(new { Measure = "Positive Trades", Value = positive_count / total_count });
                    jres_stats.Add(new { Measure = "Negative Trades", Value = negative_count / total_count });

                    foreach(var key in total_count_instrument.Keys)
                    {
                        jres_stats.Add(new { Measure = key + " Total Trades", Value = total_count_instrument[key] });
                        jres_stats.Add(new { Measure = key + " Positive Trades", Value = positive_count_instrument[key] / total_count_instrument[key] });
                        jres_stats.Add(new { Measure = key + " Negative Trades", Value = negative_count_instrument[key] / total_count_instrument[key] });
                        
                        jres_stats.Add(new { Measure = key + " Positive Value", Value = positive_value_instrument[key] / positive_count_instrument[key] });
                        jres_stats.Add(new { Measure = key + " Negative Value", Value = negative_value_instrument[key] / negative_count_instrument[key] });
                    }

                }
            }

            var jres_monthly = new List<object>();

            if (ts != null && ts.Count != 0)
            {
                DateTime dt_first = ts.DateTimes[0];
                DateTime dt_last = ts.DateTimes[ts.Count - 1];
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

                            double v1 = ts[dt1, AQI.AQILabs.Kernel.Numerics.Util.TimeSeries.DateSearchType.Previous];
                            double v2 = ts[dt2, AQI.AQILabs.Kernel.Numerics.Util.TimeSeries.DateSearchType.Previous];

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

                    double yv = Math.Round((ts[dt2_y, AQI.AQILabs.Kernel.Numerics.Util.TimeSeries.DateSearchType.Previous] / ts[dt1_y, AQI.AQILabs.Kernel.Numerics.Util.TimeSeries.DateSearchType.Previous] - 1.0) * 100.0, 1);
                    double yv_abs = ts[dt2_y, AQI.AQILabs.Kernel.Numerics.Util.TimeSeries.DateSearchType.Previous] - ts[dt1_y, AQI.AQILabs.Kernel.Numerics.Util.TimeSeries.DateSearchType.Previous];

                    yearly_result[13] = yv_abs.ToString("##,0.00");

                    //jres.Add(yearly_result);
                    jres_monthly.Add(new { Year = yearly_result[0], Jan = yearly_result[1], Feb = yearly_result[2], Mar = yearly_result[3], Apr = yearly_result[4], May = yearly_result[5], Jun = yearly_result[6], Jul = yearly_result[7], Aug = yearly_result[8], Sep = yearly_result[9], Oct = yearly_result[10], Nov = yearly_result[11], Dec = yearly_result[12], Yearly = yearly_result[13] });
                }
            }
            


            object simulation_package = Newtonsoft.Json.JsonConvert.SerializeObject(new
            {
                ID = System.Guid.NewGuid(),
                Name = strategy.Description.Substring(strategy.Description.LastIndexOf("/") + 1).Replace("Account: ", ""),                
                History = jres,
                Intraday = Instrument.TimeSeriesLoadFromDatabaseIntraday ? jres_intraday : null,
                Statistics = jres_stats,
                Monthly = jres_monthly,
                Strategy = strategy is PortfolioStrategy && strategy.Portfolio.ParentPortfolio == null ? (strategy as PortfolioStrategy).Package(false) : null,

                //TotalTrades = total_count,
                //PositiveTrades = positive_count,
                //NegativeTrades = negative_count,

                //TotalTradesByInstrument = total_count_instrument,
                //PositiveTradesByInstrument = positive_count_instrument,
                //NegativeTradesByInstrument = negative_count_instrument,

                //PositiveValueByInstrument = positive_value_instrument,
                //NegativeValueByInstrument = negative_value_instrument,
            });
            return simulation_package;
        }

        public static string StoreSimulation(string set_key, PortfolioStrategy strategy, string description)
        {
            string entry_key = System.Guid.NewGuid().ToString();
            string mkey = set_key == null ? "--Simulations-" + entry_key : set_key;
            QuantApp.Kernel.M m = QuantApp.Kernel.M.Base(mkey);
            //var res = m[x => true];

            

            DateTime t1 = DateTime.Now;

            string SimulationName = t1.ToString();

            List<object> olist = new List<object>();
            olist.Add(Simulation2JSON(strategy as Strategy));

            foreach (var s in strategy.Instruments(DateTime.Now, false).Values)
                if (s.InstrumentType == InstrumentType.Strategy && (s as Strategy).Portfolio != null)
                    olist.Add(Simulation2JSON(s as Strategy));
            
            string key = m.Add(Newtonsoft.Json.JsonConvert.SerializeObject(new { Name = description, Strategies = olist }));
            m.Save();

            return mkey;
        }
    }
}
