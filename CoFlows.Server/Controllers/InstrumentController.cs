/*
 * The MIT License (MIT)
 * Copyright (c) Arturo Rodriguez All rights reserved.
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
 
using System;
using System.Collections.Generic;
using System.Linq;
// using System.Threading;
using System.Threading.Tasks;

// using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

// using CoFlows.Server.Models;
using CoFlows.Server.Utils;

using Microsoft.AspNetCore.Authorization;

// using Newtonsoft.Json;
using AQI.AQILabs.Kernel;
using AQI.AQILabs.Kernel.Numerics.Util;
// using AQI.AQILabs.SDK.Strategies;

namespace CoFlows.Server.Controllers
{
    [Authorize, Route("[controller]/[action]")]
    public class InstrumentController : Controller
    {
        /// <summary>
        /// Get the timeseries array
        /// </summary>
        /// <remarks>
        /// Timeseries types (spot_type and b_type):
        ///     
        ///     Last = 1
        ///     Open = 2
        ///     High = 4
        ///     Low  = 6
        ///     Volume = 7
        ///     AdjClose = 9
        ///     Bid = 10
        ///     Ask = 11
        ///     Close = 12
        ///     AdjPriceReturn = 14
        /// 
        /// </remarks>
        /// <param name="id">Instrument ID</param>
        /// <param name="spot_type">Spot type</param>
        /// <param name="lastDate">Last day in the timeseries, if left empty then data the timeseries is until the last available date</param>
        /// <param name="days">Nr of days in the standard deviation calculation</param>
        /// <param name="bid">Benchmark ID, if left empty then no benchmark is used</param>
        /// <param name="b_type">Type of benchmark timeseries</param>
        /// <returns>Success</returns>
        /// <response code="200">
        /// Result:
        /// 
        ///     [[date, timeseries_value, volatility, max_drawdown, benchmark_value], ...]
        ///
        /// </response>
        [HttpGet]
        public async Task<IActionResult> TimeSeries(int id, string spot_type, string lastDate, int days = 20, int bid = -1, string b_type = "")
        {
            string userId = this.User.QID();
            if (userId == null)
                return Unauthorized();

            
            QuantApp.Kernel.User user = QuantApp.Kernel.User.FindUser(userId);
            Instrument instrument = Instrument.FindInstrument(id);

            TimeSeries ts_close = instrument.GetTimeSeries(instrument.InstrumentType == InstrumentType.ETF || instrument.InstrumentType == InstrumentType.Equity ? TimeSeriesType.AdjClose : TimeSeriesType.Close, DataProvider.DefaultProvider, true);

            if (instrument.InstrumentType == InstrumentType.Strategy && spot_type != null && spot_type.Trim() != "" && spot_type.Trim() != "spot_close")
            {
                Strategy strategy = (instrument as Strategy);

                if (strategy != null)
                {
                    int tid = int.MinValue;
                    if (!int.TryParse(spot_type, out tid))
                        tid = strategy.MemoryTypeInt(spot_type);
                    ts_close = strategy.GetMemorySeries(tid);
                }

                if (ts_close == null || (ts_close != null && ts_close.Count == 0))
                    ts_close = instrument.GetTimeSeries((TimeSeriesType)Enum.Parse(typeof(TimeSeriesType), spot_type), DataProvider.DefaultProvider, true);
            }
            else if (!string.IsNullOrWhiteSpace(spot_type) && spot_type.Trim() != "spot_close")
            {
                ts_close = instrument.GetTimeSeries((TimeSeriesType)Enum.Parse(typeof(TimeSeriesType), spot_type), DataProvider.DefaultProvider, true);
            }


            TimeSeries bm_close = null;
            if (bid != -1)
            {
                Instrument bm = Instrument.FindInstrument(bid);

                bm_close = bm.GetTimeSeries(bm.InstrumentType == InstrumentType.ETF || bm.InstrumentType == InstrumentType.Equity ? TimeSeriesType.AdjClose : TimeSeriesType.Last, DataProvider.DefaultProvider, true);


                if (bm.InstrumentType == InstrumentType.Strategy && b_type != null && b_type.Trim() != "" && b_type.Trim() != "spot_close")
                {
                    Strategy strategy = (bm as Strategy);

                    if (strategy != null)
                    {
                        int tid = int.MinValue;
                        if (!int.TryParse(b_type, out tid))
                            tid = strategy.MemoryTypeInt(b_type);
                        bm_close = strategy.GetMemorySeries(tid);
                    }

                    if (bm_close == null || (bm_close != null && bm_close.Count == 0))
                        bm_close = instrument.GetTimeSeries((TimeSeriesType)Enum.Parse(typeof(TimeSeriesType), b_type), DataProvider.DefaultProvider, true);
                }
                else if (!string.IsNullOrWhiteSpace(b_type) && b_type.Trim() != "spot_close")
                {
                    bm_close = bm.GetTimeSeries((TimeSeriesType)Enum.Parse(typeof(TimeSeriesType), b_type), DataProvider.DefaultProvider, true);
                }


            }


            if (lastDate != null && ts_close != null && ts_close.Count != 0)
                ts_close = ts_close.GetRange(ts_close.DateTimes[0], string.IsNullOrWhiteSpace(lastDate) ? DateTime.Today : DateTime.Parse(lastDate), AQI.AQILabs.Kernel.Numerics.Util.TimeSeries.RangeFillType.None);


            var jres = new List<object[]>();

            if (ts_close != null && ts_close.Count != 0)
            {
                int count = ts_close.Count;

                var isStrategy = instrument is Strategy && (instrument as Strategy).Portfolio != null;

                var t0 = isStrategy ? ts_close[Math.Max(0, count - 10000)] : 0;

                double max = ts_close[0] - t0;
                for (int i = Math.Max(0, count - 10000); i < count; i++)
                {
                    

                    DateTime date = ts_close.DateTimes[i];
                    double spot = ts_close[i] - t0;
                    double vol = 0;
                    
                    try
                    {
                        vol = (count > days && i >= days ? ts_close.GetRange(i - days, i).LogReturn().StdDev * Math.Sqrt(252) : 0);
                    }
                    catch { }

                    max = Math.Max(spot, max);
                    double maxdd = isStrategy ? spot - max : (spot / max - 1.0);

                    if (instrument.InstrumentType == InstrumentType.Strategy && spot_type != null && spot_type.Trim() != "" && spot_type.Trim() != "spot_close")
                    {
                        vol = 0;
                        max = 0;
                        maxdd = 0;
                    }

                    double bm_spot = 0;
                    if (bm_close != null)
                        bm_spot = bm_close[date, AQI.AQILabs.Kernel.Numerics.Util.TimeSeries.DateSearchType.Previous];

                    jres.Add(new object[] { Utils.Utils.ToJSTimestamp(ts_close.DateTimes[i]), spot, vol * 100, maxdd * (isStrategy ? 1 : 100), bm_spot });
                    //jres.Add(new object[] { Utils.Utils.ToJSTimestamp(ts_close.DateTimes[i]), spot, vol, maxdd, flags.ContainsKey(date.Date) ? flags[date.Date].Content : "", flags.ContainsKey(date.Date) ? flags[date.Date].ID : "", bm_spot });
                }
            }

            return Ok(jres);
        }

        /// <summary>
        /// Get an intraday timeseries array
        /// </summary>
        /// <param name="id">Instrument ID</param>
        /// <returns>Success</returns>
        /// <response code="200">
        /// Result:
        /// 
        ///     [[date, timeseries_value], ...]
        ///
        /// </response>
        [HttpGet]
        public async Task<IActionResult> IntradayTimeSeries(int id)
        {
            string userId = this.User.QID();
            if (userId == null)
                return Unauthorized();

            QuantApp.Kernel.User user = QuantApp.Kernel.User.FindUser(userId);

            Instrument instrument = Instrument.FindInstrument(id);
            var ts = instrument.GetTimeSeries(TimeSeriesType.Last);
            
            var jres = new List<object[]>();
            
            if (ts != null && ts.Count != 0)
            {
                int count = ts.Count;

                var t0 = instrument is Strategy && (instrument as Strategy).Portfolio != null ? ts[Math.Max(0, count - 10000)] : 0;

                for (int i = Math.Max(0, count - 10000); i < count; i++)
                {
                    DateTime date = ts.DateTimes[i];
                    double value = ts[i] - t0;                    

                    jres.Add(new object[] { Utils.Utils.ToJSTimestamp(date), value });
                }
            }

            return Ok(jres);
        }

        /// <summary>
        /// Get monthly and yearly performances of an instrument
        /// </summary>
        /// <param name="id">Instrument ID</param>
        /// <returns>Success</returns>
        /// <response code="200">
        /// Result:
        /// 
        ///     jres.Add(new { Year = yearly_result[0], Jan = yearly_result[1], Feb = yearly_result[2], Mar = yearly_result[3], Apr = yearly_result[4], May = yearly_result[5], Jun = yearly_result[6], Jul = yearly_result[7], Aug = yearly_result[8], Sep = yearly_result[9], Oct = yearly_result[10], Nov = yearly_result[11], Dec = yearly_result[12], Yearly = yearly_result[13] });
        ///     [{
        ///         'Year': year, 'Jan': jan_return, 'Feb': feb_return, 'Mar': mar_return, 'Apr': apr_return, 'May': may_return, 'Jun': jun_return, 'Jul': jul_return, 'Aug': aug_return, 'Sep': sep_return, 'Oct': oct_return, 'Nov': nov_return, 'Dec': dec_return, 'Yearly': yearly_return
        ///      }, ...]
        ///
        /// </response>
        [HttpGet]
        public async Task<IActionResult> MonthlyPerformance(int id)
        {
            string userId = this.User.QID();
            if (userId == null)
                return Unauthorized();

            Instrument underlying = Instrument.FindInstrument(id);

            var isStrategy = underlying is Strategy && (underlying as Strategy).Portfolio != null;

            TimeSeries ts = underlying.GetTimeSeries(underlying.InstrumentType == InstrumentType.ETF || underlying.InstrumentType == InstrumentType.Equity ? TimeSeriesType.AdjClose : TimeSeriesType.Last);
            

            var jres = new List<object>();

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
                                yearly_result[month] = isStrategy ? (v2 - v1).ToString("##,0.00") : Math.Round((v2 / v1 - 1.0) * 100.0, 1) + "%";
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

                    yearly_result[13] = isStrategy ? yv_abs.ToString("##,0.00") : (Double.IsNaN(yv) ? 0.0 : yv) + "%";

                    //jres.Add(yearly_result);
                    jres.Add(new { Year = yearly_result[0], Jan = yearly_result[1], Feb = yearly_result[2], Mar = yearly_result[3], Apr = yearly_result[4], May = yearly_result[5], Jun = yearly_result[6], Jul = yearly_result[7], Aug = yearly_result[8], Sep = yearly_result[9], Oct = yearly_result[10], Nov = yearly_result[11], Dec = yearly_result[12], Yearly = yearly_result[13] });
                }
            }

            return Ok(jres);
        }

        /// <summary>
        /// Get monthly and yearly performances of an instrument
        /// </summary>
        /// <param name="id">Instrument ID</param>
        /// <param name="days">Nr of days in the standard deviation calculation, default is 20</param>
        /// <param name="bid">Benchmark ID, if left empty then no benchmark is used</param>
        /// <returns>Success</returns>
        /// <response code="200">
        /// Result:
        /// 
        ///     jres.Add(new { Year = yearly_result[0], Jan = yearly_result[1], Feb = yearly_result[2], Mar = yearly_result[3], Apr = yearly_result[4], May = yearly_result[5], Jun = yearly_result[6], Jul = yearly_result[7], Aug = yearly_result[8], Sep = yearly_result[9], Oct = yearly_result[10], Nov = yearly_result[11], Dec = yearly_result[12], Yearly = yearly_result[13] });
        ///     [{
        ///         'One Day Performance': value, 
        ///         'Five Day Performance': value, 
        ///         'MTD Performance': value, 
        ///         'YTD Performance': value, 
        ///         'Current Volatility': value, 
        ///         'Average Volatility': value, 
        ///         'Maximum Volatility': value, 
        ///         'Minimum Volatility': value, 
        ///         'Current Drawdown': value, 
        ///         'Average Drawdown': value, 
        ///         'Maximum Drawdown': value, 
        ///         'Annual Return': value, 
        ///         'Sharpe Ratio': value, 
        ///         'Turnover': value
        ///      }, ...]
        ///
        /// </response>
        [HttpGet]
        public async Task<IActionResult> Statistics(int id, int days, int bid = -1)
        {
            string userId = this.User.QID();
            if (userId == null)
                return Unauthorized();


            Instrument instrument = Instrument.FindInstrument(id);
            TimeSeries ts = instrument.GetTimeSeries(instrument.InstrumentType == InstrumentType.ETF || instrument.InstrumentType == InstrumentType.Equity ? TimeSeriesType.AdjClose : (instrument.InstrumentType == InstrumentType.Strategy ? TimeSeriesType.Close : TimeSeriesType.Close), DataProvider.DefaultProvider, true);
            if (ts == null || (ts != null && ts.Count == 0))
                ts = instrument.GetTimeSeries(TimeSeriesType.Last, DataProvider.DefaultProvider, true);

            Instrument bmIns = Instrument.FindInstrument(bid);
            if (bmIns == null) bmIns = Instrument.FindInstrument(1663);
            TimeSeries bm_ts = bmIns.GetTimeSeries(bmIns.InstrumentType == InstrumentType.ETF || bmIns.InstrumentType == InstrumentType.Equity ? TimeSeriesType.AdjClose : (bmIns.InstrumentType == InstrumentType.Strategy ? TimeSeriesType.Close : TimeSeriesType.Close), DataProvider.DefaultProvider, true);

            var jres = new List<object>();

            if (ts != null && ts.Count > 1)
            {
                List<double> vols = new List<double>();
                List<double> maxdds = new List<double>();

                List<double> bm_vols = new List<double>();
                List<double> bm_maxdds = new List<double>();

                double last_vol = 0.0;
                double last_dd = 0.0;

                double bm_last_vol = 0.0;
                double bm_last_dd = 0.0;

                {
                    int count = ts.Count;

                    double max = ts[0];
                    double bm_max = bm_ts[0];

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

                    for (int i = 0; i < bm_ts.Count; i++)
                    {
                        double bm_spot = bm_ts[i];

                        double bm_vol = (count > days && i >= days ? bm_ts.GetRange(i - days, i).LogReturn().StdDev * Math.Sqrt(252) : 0);

                        bm_last_vol = bm_vol;
                        bm_max = Math.Max(bm_spot, bm_max);
                        double bm_maxdd = bm_spot / bm_max - 1.0;
                        bm_last_dd = bm_maxdd;

                        if (bm_ts.Count > days && i >= days)
                            bm_vols.Add(bm_vol);

                        bm_maxdds.Add(bm_maxdd);
                    }

                    int ts_count = ts.Count;

                    //jres.Add(new { Measure = "", Value = "", BenchMark = "" });

                    if (ts_count - 2 >= 0)
                        jres.Add(new { Measure = "One Day Performance", Value = Math.Round(((ts[ts_count - 1] - ts[ts_count - 2]) / ts[ts_count - 2]) * 100.0, 2) + "%", BenchMark = Math.Round(((bm_ts[bm_ts.DateTimes[bm_ts.Count - 1]] - bm_ts[bm_ts.DateTimes[bm_ts.Count - 2]]) / bm_ts[bm_ts.DateTimes[bm_ts.Count - 2]]) * 100.0, 2) + "%" });

                    if (ts_count - 6 >= 0)
                        jres.Add(new { Measure = "Five Day Performance", Value = Math.Round(((ts[ts_count - 1] - ts[ts_count - 6]) / ts[ts_count - 6]) * 100.0, 2) + "%", BenchMark = Math.Round(((bm_ts[bm_ts.DateTimes[bm_ts.Count - 1]] - bm_ts[bm_ts.DateTimes[bm_ts.Count - 6]]) / bm_ts[bm_ts.DateTimes[bm_ts.Count - 6]]) * 100.0, 2) + "%" });

                    int idx = ts.GetClosestDateIndex(new DateTime(ts.DateTimes[ts_count - 1].Year, ts.DateTimes[ts_count - 1].Month, 01), AQI.AQILabs.Kernel.Numerics.Util.TimeSeries.DateSearchType.Previous) + 1;
                    int idx_bm = bm_ts.GetClosestDateIndex(new DateTime(bm_ts.DateTimes[bm_ts.Count - 1].Year, bm_ts.DateTimes[bm_ts.Count - 1].Month, 01), AQI.AQILabs.Kernel.Numerics.Util.TimeSeries.DateSearchType.Previous) + 1;
                    if (idx > 0 && idx_bm > 0)
                        jres.Add(new { Measure = "MTD Performance", Value = Math.Round(((ts[ts_count - 1] - ts[idx - 1]) / ts[idx - 1]) * 100.0, 2) + "%", BenchMark = Math.Round(((bm_ts[bm_ts.DateTimes[bm_ts.Count - 1]] - bm_ts[bm_ts.DateTimes[idx_bm - 1]]) / bm_ts[bm_ts.DateTimes[idx_bm - 1]]) * 100.0, 2) + "%" });

                    idx = ts.GetClosestDateIndex(new DateTime(ts.DateTimes[ts_count - 1].Year, 01, 01), AQI.AQILabs.Kernel.Numerics.Util.TimeSeries.DateSearchType.Previous);
                    idx_bm = bm_ts.GetClosestDateIndex(new DateTime(bm_ts.DateTimes[bm_ts.Count - 1].Year, 01, 01), AQI.AQILabs.Kernel.Numerics.Util.TimeSeries.DateSearchType.Previous);
                    if (idx > 0 && idx_bm > 0)
                        jres.Add(new { Measure = "YTD Performance", Value = Math.Round(((ts[ts_count - 1] - ts[idx]) / ts[idx]) * 100.0, 2) + "%", BenchMark = Math.Round(((bm_ts[bm_ts.DateTimes[bm_ts.Count - 1]] - bm_ts[bm_ts.DateTimes[idx_bm]]) / bm_ts[bm_ts.DateTimes[idx_bm]]) * 100.0, 2) + "%" });

                    //jres.Add(new { Measure = "", Value = "", BenchMark = "" });

                    jres.Add(new { Measure = "Current Volatility", Value = Math.Round(last_vol * 100.0, 2) + "%", BenchMark = Math.Round(bm_last_vol * 100.0, 2) + "%" });
                    jres.Add(new { Measure = "Average Volatility", Value = Math.Round(vols.Count == 0 ? 0 : vols.Average() * 100.0, 2) + "%", BenchMark = Math.Round(bm_vols.Count == 0 ? 0 : bm_vols.Average() * 100.0, 2) + "%" });
                    jres.Add(new { Measure = "Maximum Volatility", Value = Math.Round(vols.Count == 0 ? 0 : vols.Max() * 100.0, 2) + "%", BenchMark = Math.Round(bm_vols.Count == 0 ? 0 : bm_vols.Max() * 100.0, 2) + "%" });
                    jres.Add(new { Measure = "Minimum Volatility", Value = Math.Round(vols.Count == 0 ? 0 : vols.Min() * 100, 2) + "%", BenchMark = Math.Round(bm_vols.Count == 0 ? 0 : bm_vols.Min() * 100, 2) + "%" });

                    //jres.Add(new { Measure = "", Value = "", BenchMark = "" });

                    jres.Add(new { Measure = "Current Drawdown", Value = Math.Round(last_dd * 100.0, 2) + "%", BenchMark = Math.Round(bm_last_dd * 100.0, 2) + "%" });
                    jres.Add(new { Measure = "Average Drawdown", Value = Math.Round(maxdds.Count == 0 ? 0 : maxdds.Average() * 100.0, 2) + "%", BenchMark = Math.Round(bm_maxdds.Count == 0 ? 0 : bm_maxdds.Average() * 100.0, 2) + "%" });
                    jres.Add(new { Measure = "Maximum Drawdown", Value = Math.Round(maxdds.Count == 0 ? 0 : maxdds.Min() * 100, 2) + "%", BenchMark = Math.Round(bm_maxdds.Count == 0 ? 0 : bm_maxdds.Min() * 100, 2) + "%" });

                    //jres.Add(new { Measure = "", Value = "", BenchMark = "" });

                    double t = (ts.DateTimes[ts.Count - 1] - ts.DateTimes[0]).TotalDays / 365;
                    double irr = Math.Pow(ts[ts.Count - 1] / ts[0], 1.0 / t) - 1.0;
                    double vol_total = ts.LogReturn().StdDev * Math.Sqrt(252.0);
                    if (double.IsNaN(vol_total) || double.IsInfinity(vol_total) || vol_total == 0)
                        vol_total = 1.0;

                    double bm_t = (bm_ts.DateTimes[bm_ts.Count - 1] - bm_ts.DateTimes[0]).TotalDays / 365;
                    double bm_irr = Math.Pow(bm_ts[bm_ts.DateTimes[bm_ts.Count - 1]] / bm_ts[bm_ts.DateTimes[0]], 1.0 / bm_t) - 1.0;
                    double bm_vol_total = bm_ts.LogReturn().StdDev * Math.Sqrt(252.0);
                    if (double.IsNaN(bm_vol_total) || double.IsInfinity(bm_vol_total) || bm_vol_total == 0)
                        bm_vol_total = 1.0;

                    jres.Add(new { Measure = "Annual Return", Value = Math.Round(irr * 100.0, 2) + "%", BenchMark = Math.Round(bm_irr * 100.0, 2) + "%" });
                    jres.Add(new { Measure = "Sharpe Ratio", Value = Math.Round(irr / vol_total, 2), BenchMark = Math.Round(bm_irr / bm_vol_total, 2) });

                    double turnOver = (instrument as Strategy) == null ? 0 : AQI.AQILabs.SDK.Strategies.Utils.TurnOver(instrument as Strategy, true);//Math.Round(CMStrategies.Utils.GetTurnover(id, ts.DateTimes[0], ts.DateTimes[ts.Count - 1]), 4);
                    //currentPortfolioId = id;
                    //currentPortfolioTurnOver = turnOver;
                    //jres.Add(new object[] { "Turnover", Math.Round(turnOver * 100, 2) + "%", 0.0 });
                    jres.Add(new { Measure = "Turnover", Value = Math.Round(turnOver * 100.0, 2) + "%", BenchMark = "" });
                }
            }

            return Ok(jres);
        }

        /// <summary>
        /// Get Timeseries in CSV format
        /// </summary>
        /// <param name="id">Instrument ID</param>
        /// <returns>Success</returns>
        /// <response code="200">CSV file</response>
        [HttpGet, AllowAnonymous]
        public FileContentResult TimeSeriesCSV(int id)
        {
            var ins = Instrument.FindInstrument(id);

            TimeSeries ts_close = ins.GetTimeSeries(TimeSeriesType.Last);
            
            var jres = new List<object[]>();

            string res = "Date,Value" + Environment.NewLine;

            if (ts_close != null && ts_close.Count != 0)
            {
                int count = ts_close.Count;

                double max = ts_close[0];

                for (int i = Math.Max(0, count - 10000); i < count; i++)
                {
                    DateTime date = ts_close.DateTimes[i];
                    double spot = ts_close[i];

                    res += ts_close.DateTimes[i].ToShortDateString() + "," + spot + Environment.NewLine;
                }
            }


            return File(new System.Text.UTF8Encoding().GetBytes(res), "text/csv", (ins.Name) + ".csv");
        }

    }
}