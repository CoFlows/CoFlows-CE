/*
 * The MIT License (MIT)
 * Copyright (c) 2007-2019, Arturo Rodriguez All rights reserved.
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
 
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

using AQI.AQILabs.Kernel.Numerics.Util;

namespace AQI.AQILabs.Kernel
{
    /// <summary>
    /// Class containing the logic that manages the 
    /// execution of the Tree structure of multi-level strategies.
    /// </summary>
    public class Tree
    {
        private static ConcurrentDictionary<int, Tree> _treeDB = new ConcurrentDictionary<int, Tree>();
        private static ConcurrentDictionary<DateTime, ConcurrentDictionary<int, double>> _strategyExecutionDB = new ConcurrentDictionary<DateTime, ConcurrentDictionary<int, double>>();
        private ConcurrentDictionary<int, Strategy> _strategyDB = new ConcurrentDictionary<int, Strategy>();

        /// <summary>
        /// Function: Return the tree object for a given strategy
        /// </summary>    
        /// <param name="strategy">reference strategy.
        /// </param>
        public static Tree GetTree(Strategy strategy)
        {
            if (!_treeDB.ContainsKey(strategy.ID))
                _treeDB.TryAdd(strategy.ID, new Tree(strategy));

            return _treeDB[strategy.ID];
        }

        private Strategy _parentStrategy = null;

        /// <summary>
        /// Constructor: Creates a tree for a given strategy
        /// </summary>    
        /// <param name="strategy">reference strategy.
        /// </param>
        public Tree(Strategy strategy)
        {
            this._parentStrategy = strategy;
        }

        /// <summary>
        /// Property: returns the strategy linked to this node of the tree
        /// </summary>  
        public Strategy Strategy
        {
            get
            {
                return _parentStrategy;
            }
        }

        /// <summary>
        /// Function: Initialize the tree and sub-nodes during runtime.
        /// </summary>       
        public void Initialize()
        {
            _parentStrategy.Initialize();

            foreach (Strategy strat in _strategyDB.Values.ToList())
                strat.Tree.Initialize();
        }


        /// <summary>
        /// Function: Remove the Strategy and sub-nodes from the persistent storage.
        /// </summary>          
        public void Remove()
        {
            if (_parentStrategy != null)
            {
                foreach (Strategy strat in _strategyDB.Values.ToList())
                    strat.Tree.Remove();

                foreach (ConcurrentDictionary<int, double> db in _strategyExecutionDB.Values.ToList())
                    if (db != null && db.ContainsKey(_parentStrategy.ID))
                    {
                        double v = 0;
                        db.TryRemove(_parentStrategy.ID, out v);
                    }

                if (_treeDB.ContainsKey(_parentStrategy.ID))
                {
                    Tree v = null;
                    _treeDB.TryRemove(_parentStrategy.ID, out v);
                }

                if (_strategyDB.ContainsKey(_parentStrategy.ID))
                {
                    Strategy v = null;
                    _strategyDB.TryRemove(_parentStrategy.ID, out v);
                }
                _parentStrategy.Remove();
                _parentStrategy = null;
            }
        }

        /// <summary>
        /// Function: Remove strategy and sub-nodes data string from and including a given date.
        /// </summary>         
        /// <param name="date">reference date.
        /// </param>
        public void RemoveFrom(DateTime date)
        {
            if (_parentStrategy != null)
            {
                foreach (Strategy strat in _strategyDB.Values.ToList())
                    strat.Tree.RemoveFrom(date);

                foreach (ConcurrentDictionary<int, double> db in _strategyExecutionDB.Values.ToList())
                    if (db.ContainsKey(_parentStrategy.ID))
                    {
                        double v = 0;
                        db.TryRemove(_parentStrategy.ID, out v);
                    }

                if (_treeDB.ContainsKey(_parentStrategy.ID))
                {
                    Tree v = null;
                    _treeDB.TryRemove(_parentStrategy.ID, out v);
                }

                _parentStrategy.RemoveFrom(date);
            }
        }

        /// <summary>
        /// Function: Add a sub-strategy to the tree
        /// </summary>         
        /// <param name="date">reference date.
        /// </param>
        public void AddSubStrategy(Strategy strategy)
        {
            if (strategy.Tree.ContainsStrategy(_parentStrategy))
                throw new Exception("Strategy: " + _parentStrategy + " is already a Sub-Strategy of: " + strategy);

            strategy.Initialize();
            if (!_strategyDB.ContainsKey(strategy.ID))
                _strategyDB.TryAdd(strategy.ID, strategy);
        }

        /// <summary>
        /// Function: Remove a sub-strategy to the tree
        /// </summary>         
        /// <param name="date">reference date.
        /// </param>
        public void RemoveSubStrategy(Strategy strategy)
        {
            if (!strategy.Tree.ContainsStrategy(_parentStrategy))
                throw new Exception("Strategy: " + _parentStrategy + " is not a Sub-Strategy of: " + strategy);

            strategy.Initialize();
            if (_strategyDB.ContainsKey(strategy.ID))
            {
                Strategy v = null;
                _strategyDB.TryRemove(strategy.ID, out v);
            }
        }

        /// <summary>
        /// Property: List of sub strategies
        /// </summary>         
        public List<Strategy> SubStrategies
        {
            get
            {
                return _strategyDB.Values.ToList();
            }
        }


        public readonly object objLock = new object();
        /// <summary>
        /// Function: Create a clone of this strategy and all sub-strategies.
        /// </summary>
        /// <param name="initialDate">initial date for the strategies in the cloned tree</param>
        /// <param name="finalDate">final date for the strategies in the cloned tree</param>
        /// <param name="simulated">true if strategies in tree are to be simulated and not persistent</param>
        public Tree Clone(DateTime initialDate, DateTime finalDate, bool simulated)
        {
            lock (objLock)
            {
                Dictionary<int, Strategy> clones = new Dictionary<int, Strategy>();
                Dictionary<int, double> initial_values = new Dictionary<int, double>();

                Tree clone = Clone_Internal(initialDate, finalDate, clones, initial_values, simulated);

                try
                {
                    clone.Startup(_parentStrategy.Calendar.GetClosestBusinessDay(initialDate, TimeSeries.DateSearchType.Previous), initial_values);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                return clone;
            }
        }

        /// <summary>
        /// Function: recursive helper function for cloning process.
        /// </summary>        
        /// <param name="initialDate">Clone's initialDate</param>
        /// <param name="refDate">Clone's refDate</param>
        /// <param name="clones">internal table of previously cloned base ids and respective cloned strategies</param>
        /// <param name="initial_values">internal table of initial values for the new cloned strategies</param>
        /// <param name="simulated">true if the strategy is simulated and not persistent</param>
        private Tree Clone_Internal(DateTime initialDate, DateTime refDate, Dictionary<int, Strategy> clones, Dictionary<int, double> initial_values, bool simulated)
        {
            Dictionary<int, Strategy> clones_internal = new Dictionary<int, Strategy>();

            foreach (Strategy strat in _strategyDB.Values.ToList())
            {
                Tree tree_clone = strat.Tree.Clone_Internal(initialDate, refDate, clones, initial_values, simulated);
                if (tree_clone != null)
                {
                    if (!clones.ContainsKey(strat.ID))
                        clones.Add(strat.ID, tree_clone.Strategy);
                    clones_internal.Add(strat.ID, tree_clone.Strategy);
                }
            }

            foreach (Strategy strategy in _strategyDB.Values.ToList())
                if (strategy.Portfolio == null)
                {
                    if (!clones.ContainsKey(strategy.ID))
                    {
                        Strategy subclone = strategy.Clone(null, initialDate, refDate, clones, simulated);

                        if (!clones.ContainsKey(strategy.ID))
                            clones.Add(strategy.ID, subclone);

                        clones_internal.Add(strategy.ID, subclone);

                        initial_values.Add(subclone.ID, strategy.GetTimeSeries(TimeSeriesType.Last).Values[0]);
                    }
                    else
                        clones_internal.Add(strategy.ID, clones[strategy.ID]);
                }

            if (_parentStrategy.Portfolio != null)
            {
                Portfolio portfolioClone = _parentStrategy.Portfolio.Clone(simulated);
                foreach (int[] ids in _parentStrategy.Portfolio.ReserveIds.ToList())
                {
                    Currency ccy = Currency.FindCurrency(ids[0]);
                    Instrument longReserve = Instrument.FindInstrument(ids[1]);
                    Instrument shortReserve = Instrument.FindInstrument(ids[2]);

                    portfolioClone.AddReserve(ccy, longReserve.InstrumentType == InstrumentType.Strategy ? clones[longReserve.ID] : longReserve, shortReserve.InstrumentType == InstrumentType.Strategy ? clones[shortReserve.ID] : shortReserve);
                }

                Strategy strategyClone = _parentStrategy.Clone(portfolioClone, initialDate, refDate, clones, simulated);

                if (!clones.ContainsKey(_parentStrategy.ID))
                    clones.Add(_parentStrategy.ID, strategyClone);

                initial_values.Add(strategyClone.ID, _parentStrategy.GetAUM(DateTime.Now, TimeSeriesType.Last));

                Tree clone = strategyClone.Tree;
                foreach (Strategy st in clones_internal.Values.ToList())
                {
                    bool isResidual = st.IsResidual;

                    if (st.Portfolio != null)
                        st.Portfolio.ParentPortfolio = clone.Strategy.Portfolio;

                    if (isResidual)
                        st.Portfolio.MasterPortfolio.Residual = st;


                    clone.AddSubStrategy(st);

                }

                if (_parentStrategy.IsResidual)
                    strategyClone.Portfolio.MasterPortfolio.Residual = strategyClone;

                return clone;
            }
            return null;
        }

        /// <summary>
        /// Function: recursive helper function for cloning process including positions in the base portfolios.
        /// </summary>        
        /// <param name="initialDate">Clone's initialDate</param>
        /// <param name="finalDate">Clone's finalDate</param>
        /// <param name="clones">internal table of previously cloned base ids and respective cloned strategies</param>
        /// <param name="initial_values">internal table of initial values for the new cloned strategies</param>
        /// <param name="simulated">true if the strategy is simulated and not persistent</param>
        private void Clone_Internal_Positions(DateTime initialDate, DateTime finalDate, Dictionary<int, Strategy> clones, Dictionary<int, double> initial_values, bool simulated)
        {
            Dictionary<int, Strategy> clones_internal = new Dictionary<int, Strategy>();

            foreach (Strategy strat in _strategyDB.Values.ToList())
            {
                if (strat.InitialDate <= initialDate && strat.FinalDate >= finalDate)
                    clones_internal.Add(strat.ID, clones[strat.ID]);
            }

            if (_parentStrategy.Portfolio != null)
            {
                foreach (Strategy st in clones_internal.Values.ToList())
                {
                    if (!clones[_parentStrategy.ID].Portfolio.IsReserve(st))
                        clones[_parentStrategy.ID].Portfolio.CreatePosition(st, initialDate, (initial_values[st.ID] == 0.0 ? 0 : 1.0), initial_values[st.ID]);
                }
            }
        }

        /// <summary>
        /// Function: Checks if the Tree contains a given strategy
        /// </summary>       
        /// <param name="strategy">reference strategy
        /// </param>
        public bool ContainsStrategy(Strategy strategy)
        {
            foreach (Strategy strat in _strategyDB.Values.ToList())
                if (strat.Tree.ContainsStrategy(strat))
                    return true;

            return _strategyDB.ContainsKey(strategy.ID);
        }

        /// <summary>
        /// Function: Calculate the NAV strategies without portfolios prior to the ones with portfolios.
        /// Used for index calculation for example.
        /// </summary>
        /// <param name="day">reference day</param>
        public double PreNAVCalculation(DateTime day)
        {
            foreach (Strategy strat in _strategyDB.Values.ToList())
                if (strat.InitialDate <= day && strat.FinalDate >= day && (strat.Portfolio != null || (strat.Portfolio == null && strat.FundingType != FundingType.TotalReturn)))
                    strat.Tree.PreNAVCalculation(day);


            //BusinessDay date_local = _parentStrategy.Calendar.GetBusinessDay(day);
            BusinessDay date_local = Calendar.FindCalendar("All").GetBusinessDay(day);
            if (date_local != null)
            {
                if (!_strategyExecutionDB.ContainsKey(date_local.DateTime))
                    _strategyExecutionDB.TryAdd(date_local.DateTime, new ConcurrentDictionary<int, double>());

                if (_parentStrategy.Portfolio == null && !_strategyExecutionDB[date_local.DateTime].ContainsKey(_parentStrategy.ID))
                    _strategyExecutionDB[date_local.DateTime].TryAdd(_parentStrategy.ID, _parentStrategy.NAVCalculation(date_local));
            }

            return 0;
        }

        /// <summary>
        /// Function: Startup function called once during the creation of the strategy.       
        /// </summary>
        /// <remarks>called during the cloning process</remarks>
        private void Startup(BusinessDay initialDate, Dictionary<int, double> initial_values)
        {
            foreach (Strategy strat in _strategyDB.Values.ToList())
                strat.Tree.Startup(initialDate, initial_values);

            _parentStrategy.Startup(initialDate, Math.Abs(initial_values[_parentStrategy.ID]), _parentStrategy.Portfolio);
            if (initial_values[_parentStrategy.ID] < 0)
                _parentStrategy.UpdateAUMOrder(initialDate.DateTime, initial_values[_parentStrategy.ID]);
        }

        /// <summary>
        /// Function: Calculates the NAV of the Strategies in the Tree.
        /// </summary>
        /// <param name="day">reference day</param>
        public double NAVCalculation(DateTime day)
        {
            foreach (Strategy strat in _strategyDB.Values.ToList())
                if (strat.InitialDate <= day && strat.FinalDate >= day && (strat.Portfolio != null || (strat.Portfolio == null && strat.FundingType != FundingType.TotalReturn)))
                    strat.Tree.NAVCalculation(day);

            if (!_strategyExecutionDB.ContainsKey(day))
                _strategyExecutionDB.TryAdd(day, new ConcurrentDictionary<int, double>());

            BusinessDay date_local = Calendar.FindCalendar("All").GetBusinessDay(day);
            //BusinessDay date_local = _parentStrategy.Calendar.GetBusinessDay(day);
            if (date_local != null)
            {
                ConcurrentDictionary<int, double> dict = _strategyExecutionDB[date_local.DateTime];
                DateTime t1 = DateTime.Now;
                double v = _parentStrategy.NAVCalculation(date_local);
                tt12 += (DateTime.Now - t1);
                if (!dict.ContainsKey(_parentStrategy.ID))
                    dict.TryAdd(_parentStrategy.ID, v);

                return v;
            }
            return 0.0;
        }
        public static TimeSpan tt12 = (DateTime.Now - DateTime.Now);

        /// <summary>
        /// Function: Add or remove strategy as a node in the Tree.
        /// </summary>
        /// <param name="day">reference day</param>
        public void AddRemoveSubStrategies(DateTime day)
        {
            foreach (Strategy strat in _strategyDB.Values.ToList())
                if (strat.InitialDate <= day && strat.FinalDate >= day && strat.Portfolio != null)
                    strat.Tree.AddRemoveSubStrategies(day);

            BusinessDay date_local = _parentStrategy.Calendar.GetBusinessDay(day);
            if (date_local != null)
                _parentStrategy.AddRemoveSubStrategies(date_local);
        }

        /// <summary>
        /// Function: Executed the logic for each strategy and its sub-nodes
        /// </summary>
        /// <param name="orderDate">reference day</param>
        public void ExecuteLogic(DateTime orderDate, bool force = false)
        {
            ExecuteLogic_internal(orderDate, true, force);
            if (Strategy.Portfolio != null && Strategy.Portfolio.Residual != null)
            {
                BusinessDay orderDate_local = Calendar.FindCalendar("All").GetBusinessDay(orderDate);
                if (orderDate_local != null)
                {
                    Strategy residual = Strategy.Portfolio.Residual;
                    residual.ExecuteLogic(residual.ExecutionContext(orderDate_local), force);
                }
            }
        }

        /// <summary>
        /// Function: Internal function to executed the logic for each strategy and its sub-nodes
        /// </summary>
        /// <param name="orderDate">reference day</param>
        /// /// <param name="recursive">true if recursive</param>
        private void ExecuteLogic_internal(DateTime orderDate, bool recursive, bool force)
        {
            if (_parentStrategy.Portfolio != null)
            {
                DateTime executionDate = orderDate.AddDays(1);

                ConcurrentQueue<Strategy> recalcs = new ConcurrentQueue<Strategy>();

                Parallel.ForEach(_strategyDB.Values, strat =>
                //foreach(var strat in _strategyDB.Values)
                {
                    if (strat.InitialDate <= orderDate && strat.FinalDate >= executionDate && strat.Portfolio != null && !strat.IsResidual)
                    {
                        //double oldAUM = strat.GetSODAUM(orderDate, TimeSeriesType.Last);
                        double oldAUM = strat.GetAUM(orderDate, TimeSeriesType.Last);
                        if (double.IsNaN(oldAUM) || oldAUM == 0.0)
                        {
                            double old_strat_aum = strat.GetAUM(orderDate, TimeSeriesType.Last);
                            double old_port_aum = strat.Portfolio[orderDate, TimeSeriesType.Last, TimeSeriesRollType.Last];
                            Console.WriteLine("ENQUEUE: " + strat + " " + oldAUM + " " + old_strat_aum + " " + old_port_aum);
                            recalcs.Enqueue(strat);
                        }
                        //else
                        //    strat.Tree.ExecuteLogic(orderDate, false);

                        //if
                        strat.Tree.ExecuteLogic_internal(orderDate, false, force);
                    }
                });

                //BusinessDay orderDate_local = _parentStrategy.Calendar.GetBusinessDay(orderDate);
                BusinessDay orderDate_local = Calendar.FindCalendar("All").GetBusinessDay(orderDate);
                //

                if (_parentStrategy.Initialized && orderDate_local != null && !_parentStrategy.IsResidual)
                {
                    _parentStrategy.ExecuteLogic(_parentStrategy.ExecutionContext(orderDate_local), force);

                    if (!recursive)
                        return;

                    double newAUM = _parentStrategy.GetSODAUM(orderDate, TimeSeriesType.Last);

                    if (!double.IsNaN(newAUM))
                    {
                        Boolean recalc = false;

                        Parallel.ForEach(recalcs, strat =>
                        {
                            if (strat.InitialDate <= orderDate_local.DateTime && strat.FinalDate >= orderDate_local.AddBusinessDays(1).DateTime && !strat.IsResidual)
                            {
                                //double oldAUM = strat.GetNextAUM(orderDate, TimeSeriesType.Last);
                                double oldAUM = strat.GetSODAUM(orderDate, TimeSeriesType.Last);

                                if (!double.IsNaN(oldAUM))
                                    recalc = true;
                            }
                        });


                        if (recalc)
                        {
                            _parentStrategy.ClearMemory(orderDate_local.DateTime);

                            _parentStrategy.Tree.ClearOrders(orderDate_local.DateTime, true);
                            _parentStrategy.Tree.ExecuteLogic_internal(orderDate_local.DateTime, false, force);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Function: Calls PostExecuteLogic for each strategy and its sub-nodes
        /// </summary>
        /// <param name="orderDate">reference day</param>
        public void PostExecuteLogic(DateTime day)
        {
            foreach (Strategy strat in _strategyDB.Values.ToList())
                if (strat.InitialDate <= day && strat.FinalDate >= day)
                    strat.Tree.PostExecuteLogic(day);

            BusinessDay date_local = Calendar.FindCalendar("All").GetBusinessDay(day);
            //BusinessDay date_local = _parentStrategy.Calendar.GetBusinessDay(day);
            if (date_local != null)
                _parentStrategy.PostExecuteLogic(date_local);
        }

        /// <summary>
        /// Function: Initialisation process for the Tree.
        /// </summary>
        /// <param name="orderDate">reference day</param>
        public void InitializeProcess(DateTime date)
        {
            InitializeProcess(date, true);
        }

        /// <summary>
        /// Function: Initialisation process for the Tree.
        /// </summary>
        /// <param name="orderDate">reference day</param>
        /// <param name="preNavCalculaiton">true if pre-nav calculation is to be performed</param>
        private void InitializeProcess(DateTime date, Boolean preNavCalculaiton)
        {
            BusinessDay date_local = _parentStrategy.Calendar.GetBusinessDay(date);

            if (preNavCalculaiton)
                PreNAVCalculation(date_local.DateTime);

            if (_parentStrategy.Portfolio != null)
                _parentStrategy.Portfolio.SubmitOrders(date_local.DateTime);
        }

        //static readonly object objLock = new object();
        /// <summary>
        /// Function: Simulation process.
        /// </summary>
        /// <param name="date">reference date</param>
        /// <remarks>
        /// 1) ExecuteLogic
        /// 2) PostExecuteLogic
        /// 3) SubmitOrders
        /// then 3 milliseconds later
        /// 4) PreNavCalculation
        /// 5) ReceiveExecutionLevels
        /// 6) ManageCorporateActions
        /// 7) BookOrders
        /// 8) MarginFutures
        /// 9) HedgeFX
        /// 10) NAVCalculation
        /// 11) AddRemoveSubStrategies
        /// </remarks>
        public void Process(DateTime date)
        {
            _parentStrategy.Portfolio.CanSave = false;
            if (_parentStrategy.Portfolio != null)
            {
                DateTime t1 = DateTime.Now;
                _parentStrategy.Tree.ExecuteLogic(date);
                DateTime t2 = DateTime.Now;
                tt1 += (t2 - t1);
                _parentStrategy.Tree.PostExecuteLogic(date);
                DateTime t3 = DateTime.Now;
                tt2 += (t3 - t2);
                DateTime t4 = DateTime.Now;
                tt3 += (t4 - t3);
                var submitted = _parentStrategy.Portfolio.SubmitOrders(date);
                DateTime t5 = DateTime.Now;
                tt4 += (t5 - t4);
            }

            int d = 3;
            DateTime t6 = DateTime.Now;
            _parentStrategy.Tree.PreNAVCalculation(date.AddMilliseconds(d));
            DateTime t7 = DateTime.Now;
            tt5 += (t7 - t6);

            if (_parentStrategy.Portfolio != null)
            {
                DateTime t8 = DateTime.Now;
                _parentStrategy.Portfolio.ReceiveExecutionLevels(date.AddMilliseconds(d));
                DateTime t9 = DateTime.Now;
                tt6 += (t9 - t8);
                _parentStrategy.Tree.ManageCorporateActions(date.AddMilliseconds(d));
                DateTime t10 = DateTime.Now;
                tt7 += (t10 - t9);
                var booked = _parentStrategy.Tree.BookOrders(date.AddMilliseconds(d));
                DateTime t11 = DateTime.Now;
                tt8 += (t11 - t10);
                _parentStrategy.Tree.NAVCalculation(date.AddMilliseconds(d));//CHANGE
                DateTime t12 = DateTime.Now;
                tt9 += (t12 - t11);
                
                _parentStrategy.Tree.MarginFutures(date.AddMilliseconds(d));//CHANGE
                DateTime t13 = DateTime.Now;
                tt10 += (t13 - t12);
                _parentStrategy.Tree.AddRemoveSubStrategies(date.AddMilliseconds(d));
                DateTime t14 = DateTime.Now;
                tt11 += (t14 - t13);
            }

            _parentStrategy.Portfolio.CanSave = true;
        }

        public static TimeSpan tt1 = (DateTime.Now - DateTime.Now);
        public static TimeSpan tt2 = (DateTime.Now - DateTime.Now);
        public static TimeSpan tt3 = (DateTime.Now - DateTime.Now);
        public static TimeSpan tt4 = (DateTime.Now - DateTime.Now);
        public static TimeSpan tt5 = (DateTime.Now - DateTime.Now);
        public static TimeSpan tt6 = (DateTime.Now - DateTime.Now);
        public static TimeSpan tt7 = (DateTime.Now - DateTime.Now);
        public static TimeSpan tt8 = (DateTime.Now - DateTime.Now);
        public static TimeSpan tt9 = (DateTime.Now - DateTime.Now);
        public static TimeSpan tt10 = (DateTime.Now - DateTime.Now);
        public static TimeSpan tt11 = (DateTime.Now - DateTime.Now);

        internal readonly object objLockLoad = new object();
        /// <summary>
        /// Function: Load portfolio memory for entire tree and all history
        /// </summary>        
        public void LoadPortfolioMemory(bool onlyPositions = false)
        {

            lock (objLockLoad)
            {
                onlyPositions = false;

                if (_parentStrategy.Portfolio != null && _parentStrategy.Portfolio.MasterPortfolio == _parentStrategy.Portfolio)
                {
                    _parentStrategy.Portfolio.MasterPortfolio._loading = true;
                    _parentStrategy.Portfolio.MasterPortfolio.CanSave = false;
                }



                if (_parentStrategy.Portfolio != null)
                    _parentStrategy.Portfolio.LoadPositionOrdersMemory(DateTime.MinValue, false, onlyPositions);

                foreach (Strategy strat in _strategyDB.Values.ToList())
                    strat.Tree.LoadPortfolioMemory(onlyPositions);

                // if (_parentStrategy.Portfolio != null)
                    // _parentStrategy.Portfolio.LoadPositionOrdersMemory(DateTime.MinValue, false, onlyPositions);



                if (_parentStrategy.Portfolio != null && _parentStrategy.Portfolio.MasterPortfolio == _parentStrategy.Portfolio)
                {
                    _parentStrategy.Portfolio.MasterPortfolio._loading = false;
                    _parentStrategy.Portfolio.MasterPortfolio.CanSave = true;
                }
            }
        }

        /// <summary>
        /// Function: Load portfolio memory for entire tree on a given date
        /// </summary>        
        /// <param name="date">reference date</param>
        public void LoadPortfolioMemory(DateTime date, bool onlyPositions = false)
        {
            Initialize();
            LoadPortfolioMemory(onlyPositions);
            return;
            lock (_parentStrategy)
            {

                if (_parentStrategy.Portfolio != null && _parentStrategy.Portfolio.MasterPortfolio == _parentStrategy.Portfolio)
                {
                    _parentStrategy.Portfolio.MasterPortfolio._loading = true;
                    _parentStrategy.Portfolio.MasterPortfolio.CanSave = false;
                }

                if (_parentStrategy.Portfolio != null)
                    _parentStrategy.Portfolio.LoadPositionOrdersMemory(date, false, onlyPositions);

                foreach (Strategy strat in _strategyDB.Values.ToList())
                    strat.Tree.LoadPortfolioMemory(date, onlyPositions);

                if (_parentStrategy.Portfolio != null && _parentStrategy.Portfolio.MasterPortfolio == _parentStrategy.Portfolio)
                {
                    _parentStrategy.Portfolio.MasterPortfolio._loading = false;
                    _parentStrategy.Portfolio.MasterPortfolio.CanSave = true;
                }
            }
        }

        /// <summary>
        /// Function: Loading portfolio memory for entire tree on a given date from persistent memory only.
        /// always over write non-persistent memory.
        /// </summary>        
        /// <param name="date">reference date</param>
        public void LoadPortfolioMemory(DateTime date, bool force, bool onlyPositions = false)
        {
            Initialize();
            LoadPortfolioMemory(onlyPositions);
            return;
            lock (_parentStrategy)
            {
                if (_parentStrategy.Portfolio != null && _parentStrategy.Portfolio.MasterPortfolio == _parentStrategy.Portfolio)
                {
                    _parentStrategy.Portfolio.MasterPortfolio._loading = true;
                    _parentStrategy.Portfolio.MasterPortfolio.CanSave = false;
                }



                if (_parentStrategy.Portfolio != null)
                    _parentStrategy.Portfolio.LoadPositionOrdersMemory(date, force, onlyPositions);


                foreach (Strategy strat in _strategyDB.Values.ToList())
                    strat.Tree.LoadPortfolioMemory(date, force, onlyPositions);

                if (_parentStrategy.Portfolio != null && _parentStrategy.Portfolio.MasterPortfolio == _parentStrategy.Portfolio)
                {
                    _parentStrategy.Portfolio.MasterPortfolio._loading = false;
                    _parentStrategy.Portfolio.MasterPortfolio.CanSave = true;
                }
            }
        }

        /// <summary>
        /// Function: Loading portfolio memory for entire tree on a given date from persistent memory only.
        /// always over write non-persistent memory.
        /// </summary>        
        /// <param name="date">reference date</param>
        public void UpdatePositions(DateTime date)
        {
            foreach (Strategy strat in _strategyDB.Values.ToList())
                strat.Tree.UpdatePositions(date);

            if (_parentStrategy.Portfolio != null)
                _parentStrategy.Portfolio.UpdatePositions(date);

        }

        /// <summary>
        /// Function: Manage corporate actions of the tree on a given date.
        /// </summary>        
        /// <param name="date">reference date</param>
        public void ManageCorporateActions(DateTime date)
        {
            foreach (Strategy strat in _strategyDB.Values.ToList())
                strat.Tree.ManageCorporateActions(date);

            if (_parentStrategy.Portfolio != null)
                _parentStrategy.Portfolio.ManageCorporateActions(date);
        }

        /// <summary>
        /// Function: Margin futures of the tree on a given date.
        /// </summary>        
        /// <param name="date">reference date</param>
        public void MarginFutures(DateTime date)
        {
            foreach (Strategy strat in _strategyDB.Values.ToList())
                strat.Tree.MarginFutures(date);

            if (_parentStrategy.Portfolio != null)
                _parentStrategy.Portfolio.MarginFutures(date);
        }

        /// <summary>
        /// Function: Hedge FX of the tree on a given date.
        /// </summary>        
        /// <param name="date">reference data</param>
        public void HedgeFX(DateTime date, double threshhold)
        {
            foreach (Strategy strat in _strategyDB.Values.ToList())
                strat.Tree.HedgeFX(date, threshhold);

            if (_parentStrategy.Portfolio != null)
                _parentStrategy.Portfolio.HedgeFX(date, threshhold);
        }

        /// <summary>
        /// Function: Book orders of the tree on a given date.
        /// </summary>        
        /// <param name="executionDay">reference date</param>
        public List<Position> BookOrders(DateTime executionDay)
        {
            //int count = 0;
            var ret = new List<Position>();
            bool prevCanSave = true;
            if (_parentStrategy.Portfolio != null && _parentStrategy.Portfolio.MasterPortfolio.Strategy == _parentStrategy)
            {
                prevCanSave = _parentStrategy.Portfolio.CanSave;
                _parentStrategy.Portfolio.CanSaveLocal = false;
            }




            if (_parentStrategy.Initialized && _parentStrategy.Portfolio != null)
            {
                List<Position> ps = _parentStrategy.Portfolio.BookOrders(executionDay);//, TimeSeriesType.Last);
                if (ps != null)
                    //count += ps.Count;
                    foreach (var p in ps)
                        ret.Add(p);
            }

            foreach (Strategy strat in _strategyDB.Values.ToList())
                if (strat.InitialDate <= executionDay && strat.FinalDate >= executionDay)
                    //count += strat.Tree.BookOrders(executionDay);
                    foreach (var o in strat.Tree.BookOrders(executionDay))
                        ret.Add(o);

            if (_parentStrategy.Portfolio != null && _parentStrategy.Portfolio.MasterPortfolio.Strategy == _parentStrategy && prevCanSave)
                _parentStrategy.Portfolio.CanSaveLocal = true;

            return ret;// count;
        }

        /// <summary>
        /// Function: Re book orders of the tree on a given date.
        /// </summary>        
        /// <param name="executionDay">reference date</param>
        public int ReBookOrders(DateTime executionDay)
        {
            int count = 0;

            foreach (Strategy strat in _strategyDB.Values.ToList())
                if (strat.InitialDate <= executionDay && strat.FinalDate >= executionDay)
                    count += strat.Tree.ReBookOrders(executionDay);

            if (_parentStrategy.Initialized && _parentStrategy.Portfolio != null)
            {
                List<Position> ps = _parentStrategy.Portfolio.ReBookOrders(executionDay);
                if (ps != null)
                    count += ps.Count;
            }

            return count;
        }

        /// <summary>
        /// Function: Save and commit all new positions changed for this tree in persistent storage.
        /// </summary> 
        public void SaveNewPositions()
        {
            foreach (Strategy strat in _strategyDB.Values.ToList())
                strat.Tree.SaveNewPositions();

            if (_parentStrategy.Portfolio != null)
                _parentStrategy.Portfolio.SaveNewPositions();
        }

        /// <summary>
        /// Function: Save and commit all new positions changed for this tree in persistent storage.
        /// </summary> 
        public void SaveNewPositionsLocal()
        {
            foreach (Strategy strat in _strategyDB.Values.ToList())
                strat.Tree.SaveNewPositionsLocal();

            if (_parentStrategy.Portfolio != null)
                _parentStrategy.Portfolio.SaveNewPositionsLocal();
        }

        /// <summary>
        /// Function: Save and commit all values changed for this tree in persistent storage.
        /// </summary>    
        public void Save()
        {
            foreach (Strategy strat in _strategyDB.Values.ToList())
                strat.Tree.Save();

            _parentStrategy.Save();
        }

        /// <summary>
        /// Function: Save and commit all values changed for this tree in persistent storage.
        /// </summary>    
        public void SaveLocal()
        {
            foreach (Strategy strat in _strategyDB.Values.ToList())
                strat.Tree.SaveLocal();

            _parentStrategy.SaveLocal();
        }

        /// <summary>
        /// Function: Clear the Strategy memory of the entire tree below this for a specific date. (Does not clear AUM Memory)
        /// </summary>       
        /// <param name="date">DateTime value date 
        /// </param>
        public void ClearMemory(DateTime date)
        {
            foreach (Strategy strat in _strategyDB.Values.ToList())
                strat.Tree.ClearMemory(date);

            if (_parentStrategy.Initialized)
                _parentStrategy.ClearMemory(date);
        }

        /// <summary>
        /// Function: Clear the portfolio's new orders of the entire tree below this for a specific date.
        /// </summary>       
        /// <param name="date">DateTime value date 
        /// </param>
        /// <param name="clearMemory">True if clear AUM memory also.</param>
        public void ClearOrders(DateTime orderDate, bool clearMemory)
        {
            foreach (Strategy strat in _strategyDB.Values.ToList())
                strat.Tree.ClearOrders(orderDate, clearMemory);

            BusinessDay date_local = _parentStrategy.Calendar.GetBusinessDay(orderDate);
            if (date_local != null)
                if (_parentStrategy.Initialized)
                {
                    if (clearMemory)
                        _parentStrategy.ClearNextAUMMemory(orderDate);

                    if (_parentStrategy.Portfolio != null)
                        _parentStrategy.Portfolio.ClearOrders(orderDate);
                }
        }
    }
}