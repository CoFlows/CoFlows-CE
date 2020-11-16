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
using System.ComponentModel;

using AQI.AQILabs.Kernel.Factories;


namespace AQI.AQILabs.Kernel
{
    /// <summary>
    /// Class managing the connection to the market through all linked brokers.
    /// The Market Class enables the usage of multiple broker technologies through one system.
    /// Instructions define how the Market Class routes orders for specific constracts for a given strategy allowing the 
    /// developer to customise the execution process.    
    /// </summary>
    public class Market
    {
        private static Dictionary<string, OrderRecord> orderDict = new Dictionary<string, OrderRecord>();
        public static IMarketFactory Factory = null;
        public delegate void UpdateEvent(OrderRecord record);
        private static ConcurrentQueue<CashUpdateMessage> _cashMessages = new ConcurrentQueue<CashUpdateMessage>();

        public delegate Strategy CreateAccountStrategyType(string accountName, double aum, Currency ccy, string custodian, string username, string password, string portfolio, string parameters);
        public static CreateAccountStrategyType CreateAccount = null;

        /// <summary>        
        /// Delegate: The Submit defines the delegate that implementes the order submission function through a given client.
        /// </summary>
        public delegate void SubmitType(Order order);

        /// <summary>        
        /// Delegate: Defined market impact during Simulations
        /// </summary>
        public delegate double SimulateMarketImpactFactorType(DateTime datetime, Instrument instrument);
        public static SimulateMarketImpactFactorType SimulateMarketImpact = null;

        public static void Close()
        {

        }

        /// <summary>
        /// Structure representing the skeleton of a market connection.
        /// ClientConnection contains a list of destinations representing names of the brokers/markets the order are routed through.
        /// The SubmitFunction holds the delegate that implementes the order submission function through this client.
        /// </summary>
        public struct ClientConnection
        {
            public string Name;
            public List<string> Destinations;
            public SubmitType SubmitFunction;

            public ClientConnection(string Name, List<string> Destinations, SubmitType SubmitFunction)
            {
                this.Name = Name;
                if (Destinations == null || (Destinations != null && Destinations.Count == 0))
                {
                    this.Destinations = new List<string>();
                    this.Destinations.Add("Empty");
                }
                else
                    this.Destinations = Destinations;

                this.SubmitFunction = SubmitFunction;
            }
        }

        public static void Add(CashUpdateMessage message)
        {
            _cashMessages.Enqueue(message);
        }

        private static bool initialized = false;
        /// <summary>        
        /// Function: Initialize the market connections.
        /// </summary>
        public static void Initialize()
        {
            if (initialized)
                return;

            Factory.Initialize();

            System.Threading.Thread th = new System.Threading.Thread(() =>
            {
                while (true)
                {
                    try
                    {
                        System.Threading.Thread.Sleep(500);

                        DateTime t = DateTime.Now;



                        CashUpdateMessage message;
                        while (_cashMessages.TryDequeue(out message))
                        {

                            Console.WriteLine("UPDATE CASH: " + message.Portfolio.Name);
                            if (!message.Portfolio.MasterPortfolio.Strategy.Executing)
                            {
                                lock (message.Portfolio.MasterPortfolio.Strategy.ExecutionLock)
                                {
                                    message.Portfolio.MasterPortfolio.Strategy.Executing = true;
                                    message.Portfolio.Strategy.Tree.UpdatePositions(t);
                                    message.Portfolio.UpdateReservePosition(t, message.Value, message.Currency);
                                    message.Portfolio.MasterPortfolio.Strategy.Executing = false;
                                }
                            }
                            else
                            {
                                _cashMessages.Enqueue(message);
                            }

                            //message.Portfolio.MasterPortfolio.Strategy.Tree.SaveLocal();
                            //message.Portfolio.MasterPortfolio.Strategy.Tree.SaveNewPositionsLocal();

                        }

                        BookOrders(t);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
            });

            th.Start();

            initialized = true;
        }

        /// <summary>        
        /// Function: Add a connection used by the market class to route orders.
        /// </summary>
        /// <param name="connection">reference connection</param>
        public static void AddConnection(Market.ClientConnection connection)
        {
            if (!_connectionDB.ContainsKey(connection.Name))
                _connectionDB.Add(connection.Name, connection);
            //Factory.AddConnection(connection);
        }

        /// <summary>        
        /// Function: Remove a connection.
        /// </summary>
        /// <param name="name">name of the connection</param>
        public static void RemoveConnection(string name)
        {
            if (_connectionDB.ContainsKey(name))
                _connectionDB.Remove(name);

            //Factory.RemoveConnection(name);
        }

        protected static ConcurrentDictionary<int, Portfolio> portfolios = new ConcurrentDictionary<int, Portfolio>();

        /// <summary>        
        /// Function: Add a portfolio to be monitored and managed by the Market.
        /// </summary>
        /// <param name="portfolio">reference portfolio</param>
        public static void AddPortfolio(Portfolio portfolio)
        {
            if (!portfolios.ContainsKey(portfolio.ID))
            {
                portfolios.TryAdd(portfolio.ID, portfolio);
                //Factory.a(instruction);
            }
        }

        /// <summary>        
        /// Function: Remove a portfolio that has been monitored and managed by the Market.
        /// </summary>
        /// <param name="portfolio">reference portfolio</param>
        public static void RemovePortfolio(Portfolio portfolio)
        {
            if (portfolios.ContainsKey(portfolio.ID))
            {
                Portfolio v = null;
                portfolios.TryRemove(portfolio.ID, out v);
            }
        }

        public readonly static object objLock2 = new object();
        public readonly static object objLock1 = new object();
        /// <summary>        
        /// Function: Submit all new orders for a given portfolio generated at a specific time
        /// </summary>
        /// <param name="portfolio">reference portfolio</param>
        /// <param name="orderDate">reference date</param>
        public static object[] SubmitOrders(DateTime orderDate, Portfolio portfolio)
        {
            if (portfolio.MasterPortfolio.Strategy.Simulating)
            {
                lock (objLock1)
                {
                    Dictionary<int, Dictionary<string, Order>> orders = portfolio.OpenOrders(orderDate, true);



                    if (orders != null && orders.Count != 0)
                    {
                        List<Order> toSubmit = new List<Order>();

                        List<Order> ordersSubmit = new List<Order>();
                        foreach (int i in orders.Keys)
                        {
                            Dictionary<string, Order> os = orders[i];
                            foreach (string orderID in os.Keys.ToList())
                            {
                                Order order = os[orderID];

                                if (!order.Portfolio.MasterPortfolio.Strategy.Simulating)
                                {
                                    if (order.OrderDate == Calendar.Close(order.OrderDate))
                                        portfolio.UpdateOrderTree(order, OrderStatus.Submitted, double.NaN, double.NaN, DateTime.MaxValue);
                                    if (order.Unit != 0.0 && !ordersSubmit.Contains(order))
                                        ordersSubmit.Add(order);
                                }

                                //Submit(order);                            
                                toSubmit.Add(order);
                            }
                        }

                        var toSubmitOrdered = toSubmit.OrderBy(x => x.Unit);

                        foreach (Order order in toSubmitOrdered)
                            Submit(order);
                    }

                    return null;
                }
            }
            else
            {
                Dictionary<int, Dictionary<string, Order>> orders = portfolio.OpenOrders(orderDate, true);



                if (orders != null && orders.Count != 0)
                {
                    List<Order> toSubmit = new List<Order>();

                    List<Order> ordersSubmit = new List<Order>();
                    foreach (int i in orders.Keys)
                    {
                        Dictionary<string, Order> os = orders[i];
                        foreach (string orderID in os.Keys.ToList())
                        {
                            Order order = os[orderID];

                            if (!order.Portfolio.MasterPortfolio.Strategy.Simulating)
                            {
                                if (order.OrderDate == Calendar.Close(order.OrderDate))
                                    portfolio.UpdateOrderTree(order, OrderStatus.Submitted, double.NaN, double.NaN, DateTime.MaxValue);
                                if (order.Unit != 0.0 && !ordersSubmit.Contains(order))
                                    ordersSubmit.Add(order);
                            }

                            //Submit(order);                            
                            toSubmit.Add(order);
                        }
                    }

                    //var toSubmitOrdered = toSubmit.OrderBy(x => x.Unit);

                    foreach (Order order in toSubmit)
                        Submit(order);
                }

                return null;
            }
        }

        /// <summary>        
        /// Function: Submit a specific order
        /// </summary>
        /// <param name="order">reference order</param>        
        public static object Submit(Order order)
        {
            if (!order.Portfolio.MasterPortfolio.Strategy.Simulating)
            {
                lock (objLock2)
                {
                    if (order.Unit != 0 && order.Status == OrderStatus.New)
                    //if (order.Status == OrderStatus.New)
                    {
                        Instrument instrument = order.Instrument;

                        Dictionary<int, Dictionary<int, Instruction>> list = Instructions();
                        Instruction defaultInstruction = list.ContainsKey(0) && list[0].ContainsKey(0) ? list[0][0] : null;
                        Instruction portfolioDefault = list.ContainsKey(order.Portfolio.MasterPortfolio.ID) && list[order.Portfolio.MasterPortfolio.ID].ContainsKey(0) ? list[order.Portfolio.MasterPortfolio.ID][0] : null;

                        Instruction instruction = GetInstruction(order);

                        double minSize = 0.0;
                        double minStep = 0.0;
                        double margin = 0.0;

                        if (instruction != null && (order.Client == null || string.IsNullOrWhiteSpace(order.Client) || order.Client == "Inherit"))
                        {
                            minSize = instruction.MinSize;
                            minStep = instruction.MinStep;
                            margin = instruction.Margin;

                            if (instruction.Client == "Inherit")
                            {
                                if (portfolioDefault == null || instruction.Client == "Inherit")
                                {
                                    if (defaultInstruction != null)
                                    {
                                        order.Client = defaultInstruction.Client;
                                        order.Destination = defaultInstruction.Destination;
                                        order.Account = defaultInstruction.Account;

                                        minSize = defaultInstruction.MinSize;
                                        minStep = defaultInstruction.MinStep;
                                        margin = defaultInstruction.Margin;
                                    }
                                }
                                else
                                {
                                    order.Client = portfolioDefault.Client;
                                    order.Destination = portfolioDefault.Destination;
                                    order.Account = portfolioDefault.Account;

                                    minSize = portfolioDefault.MinSize;
                                    minStep = portfolioDefault.MinStep;
                                    margin = portfolioDefault.Margin;
                                }

                            }
                            else
                            {
                                order.Client = instruction.Client;
                                order.Destination = instruction.Destination;
                                order.Account = instruction.Account;


                            }

                            // Create Residual orders that ensure the target portfolio contains the desired units while the residual portfolio contains the necessary orders based on the minimum size orders allowed by the broker.
                            if (order.Portfolio.Residual != null)
                            {
                                Position oldResPosition = order.Portfolio.Residual.Portfolio.FindPosition(instrument, order.OrderDate, true);
                                double oldResUnit = (oldResPosition == null ? 0.0 : ((instrument.InstrumentType == AQI.AQILabs.Kernel.InstrumentType.Portfolio || (instrument.InstrumentType == AQI.AQILabs.Kernel.InstrumentType.Strategy && (instrument as Strategy).Portfolio != null)) ? 0.0 : oldResPosition.Unit));


                                Position oldAggPosition = order.Portfolio.FindPosition(instrument, order.OrderDate, true);
                                double oldAggUnit = (oldAggPosition == null ? 0.0 : ((instrument.InstrumentType == AQI.AQILabs.Kernel.InstrumentType.Portfolio || (instrument.InstrumentType == AQI.AQILabs.Kernel.InstrumentType.Strategy && (instrument as Strategy).Portfolio != null)) ? 0.0 : oldAggPosition.Unit));
                                double wantedTarget = oldAggUnit + order.Unit - oldResUnit;

                                double residual = 0.0;

                                if (Math.Abs(wantedTarget) > 1e-3)
                                {

                                    double wantedUnit = order.Unit - oldResUnit;

                                    double round_down_units = minStep == 0 ? wantedUnit : (Math.Floor(wantedUnit / minStep) * minStep);
                                    double adj_units = wantedUnit >= 0 ? (wantedUnit < minSize * 0.5 ? 0 : Math.Max(minStep, Math.Max(round_down_units, minSize))) : (wantedUnit > -minSize * 0.5 ? 0 : Math.Min(-minStep, Math.Min(round_down_units, -minSize)));
                                    residual = wantedUnit - adj_units + oldResUnit;// +oldUnit;// wantedUnit % Math.Max(1, minStep) - Math.Abs(round_down_units - adj_units) * minStep;
                                }
                                else
                                    residual = oldResUnit;


                                order.Portfolio.Residual.Portfolio.CreateOrder(order.Instrument, order.OrderDate, -residual, OrderType.Market, 0.0);
                                //order.Portfolio.Residual.Portfolio.CreateTargetMarketOrder(order.Instrument, order.OrderDate, -residual * Math.Sign(order.Unit));
                            }

                            order.Portfolio.MasterPortfolio.UpdateOrderTree(order, OrderStatus.Submitted, double.NaN, double.NaN, DateTime.MaxValue, order.Client, order.Destination, order.Account);
                        }
                        else
                            order.Portfolio.MasterPortfolio.UpdateOrderTree(order, OrderStatus.Submitted, double.NaN, double.NaN, DateTime.MaxValue);

                        if (Portfolio.DebugPositions)
                            Console.WriteLine("Submit Order: " + order.Instrument + " " + order.Unit + " " + DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss.fff"));


                        if (order.Portfolio.MasterPortfolio.Strategy.Simulating)
                            return null;

                        OrderRecord orderRecord = new OrderRecord()
                        {
                            Date = order.OrderDate.TimeOfDay.ToString(),
                            OrderID = order.ID,
                            Name = order.Instrument.Name,
                            Side = order.Unit > 0 ? "Long" : "Short",
                            Type = order.Type.ToString(),
                            Unit = Math.Abs(order.Unit).ToString("#,##0.##"),
                            Price = (decimal)order.Limit,
                            Status = "New",
                            RootPortfolioID = order.Portfolio.MasterPortfolio.ID,
                            RootPortfolioName = order.Portfolio.MasterPortfolio.Strategy.Description,
                            ParentPortfolioID = order.Portfolio.ID
                        };

                        DateTime t1 = DateTime.Now;

                        Dictionary<string, Market.ClientConnection> clientConnections = _connectionDB;// Factory.GetClientConnetions();

                        if (clientConnections.ContainsKey(order.Client))
                        {
                            if (clientConnections[order.Client].SubmitFunction != null)
                                clientConnections[order.Client].SubmitFunction(order);
                        }

                        if (order.OrderDate.Date == DateTime.Today)
                        {
                            if (orderDict.ContainsKey(orderRecord.OrderID))
                                orderDict[orderRecord.OrderID] = orderRecord;
                            else
                                orderDict.Add(orderRecord.OrderID, orderRecord);

                            UpdateRecord(orderRecord);
                        }
                        return orderRecord;
                    }
                    else if (order.Unit == 0 && order.Status == OrderStatus.New)
                    {
                        double last = order.Instrument[order.OrderDate.Date, TimeSeriesType.Last, TimeSeriesRollType.Last] * (order.Instrument as Security != null ? (order.Instrument as Security).PointSize : 1.0);
                        order.Portfolio.MasterPortfolio.UpdateOrderTree(order, OrderStatus.Executed, double.NaN, last, order.OrderDate, "Simulator", "Model", "NA");
                        //portfolio.UpdateOrderTree(order, OrderStatus.Executed, double.NaN, executionLevel, executionDate);
                    }
                    return null;
                }
            }
            else
            {
                if (order.Unit != 0 && order.Status == OrderStatus.New)
                //if (order.Status == OrderStatus.New)
                {
                    Instrument instrument = order.Instrument;

                    Dictionary<int, Dictionary<int, Instruction>> list = Instructions();
                    Instruction defaultInstruction = list.ContainsKey(0) && list[0].ContainsKey(0) ? list[0][0] : null;
                    Instruction portfolioDefault = list.ContainsKey(order.Portfolio.MasterPortfolio.ID) && list[order.Portfolio.MasterPortfolio.ID].ContainsKey(0) ? list[order.Portfolio.MasterPortfolio.ID][0] : null;

                    Instruction instruction = GetInstruction(order);

                    double minSize = 0.0;
                    double minStep = 0.0;
                    double margin = 0.0;

                    if (instruction != null && (order.Client == null || string.IsNullOrWhiteSpace(order.Client) || order.Client == "Inherit"))
                    {
                        minSize = instruction.MinSize;
                        minStep = instruction.MinStep;
                        margin = instruction.Margin;

                        if (instruction.Client == "Inherit")
                        {
                            if (portfolioDefault == null || instruction.Client == "Inherit")
                            {
                                if (defaultInstruction != null)
                                {
                                    order.Client = defaultInstruction.Client;
                                    order.Destination = defaultInstruction.Destination;
                                    order.Account = defaultInstruction.Account;

                                    minSize = defaultInstruction.MinSize;
                                    minStep = defaultInstruction.MinStep;
                                    margin = defaultInstruction.Margin;
                                }
                            }
                            else
                            {
                                order.Client = portfolioDefault.Client;
                                order.Destination = portfolioDefault.Destination;
                                order.Account = portfolioDefault.Account;

                                minSize = portfolioDefault.MinSize;
                                minStep = portfolioDefault.MinStep;
                                margin = portfolioDefault.Margin;
                            }

                        }
                        else
                        {
                            order.Client = instruction.Client;
                            order.Destination = instruction.Destination;
                            order.Account = instruction.Account;


                        }

                        // Create Residual orders that ensure the target portfolio contains the desired units while the residual portfolio contains the necessary orders based on the minimum size orders allowed by the broker.
                        if (order.Portfolio.Residual != null)
                        {
                            Position oldResPosition = order.Portfolio.Residual.Portfolio.FindPosition(instrument, order.OrderDate, true);
                            double oldResUnit = (oldResPosition == null ? 0.0 : ((instrument.InstrumentType == AQI.AQILabs.Kernel.InstrumentType.Portfolio || (instrument.InstrumentType == AQI.AQILabs.Kernel.InstrumentType.Strategy && (instrument as Strategy).Portfolio != null)) ? 0.0 : oldResPosition.Unit));


                            Position oldAggPosition = order.Portfolio.FindPosition(instrument, order.OrderDate, true);
                            double oldAggUnit = (oldAggPosition == null ? 0.0 : ((instrument.InstrumentType == AQI.AQILabs.Kernel.InstrumentType.Portfolio || (instrument.InstrumentType == AQI.AQILabs.Kernel.InstrumentType.Strategy && (instrument as Strategy).Portfolio != null)) ? 0.0 : oldAggPosition.Unit));
                            double wantedTarget = oldAggUnit + order.Unit - oldResUnit;

                            double residual = 0.0;

                            if (Math.Abs(wantedTarget) > 1e-3)
                            {

                                double wantedUnit = order.Unit - oldResUnit;

                                double round_down_units = minStep == 0 ? wantedUnit : (Math.Floor(wantedUnit / minStep) * minStep);
                                double adj_units = wantedUnit >= 0 ? (wantedUnit < minSize * 0.5 ? 0 : Math.Max(minStep, Math.Max(round_down_units, minSize))) : (wantedUnit > -minSize * 0.5 ? 0 : Math.Min(-minStep, Math.Min(round_down_units, -minSize)));
                                residual = wantedUnit - adj_units + oldResUnit;// +oldUnit;// wantedUnit % Math.Max(1, minStep) - Math.Abs(round_down_units - adj_units) * minStep;
                            }
                            else
                                residual = oldResUnit;


                            order.Portfolio.Residual.Portfolio.CreateOrder(order.Instrument, order.OrderDate, -residual, OrderType.Market, 0.0);
                            //order.Portfolio.Residual.Portfolio.CreateTargetMarketOrder(order.Instrument, order.OrderDate, -residual * Math.Sign(order.Unit));
                        }

                        order.Portfolio.MasterPortfolio.UpdateOrderTree(order, OrderStatus.Submitted, double.NaN, double.NaN, DateTime.MaxValue, order.Client, order.Destination, order.Account);
                    }
                    else
                        order.Portfolio.MasterPortfolio.UpdateOrderTree(order, OrderStatus.Submitted, double.NaN, double.NaN, DateTime.MaxValue);

                    return null;

                }
                else if (order.Unit == 0 && order.Status == OrderStatus.New)
                {
                    double last = order.Instrument[order.OrderDate.Date, TimeSeriesType.Last, TimeSeriesRollType.Last] * (order.Instrument as Security != null ? (order.Instrument as Security).PointSize : 1.0);
                    order.Portfolio.MasterPortfolio.UpdateOrderTree(order, OrderStatus.Executed, double.NaN, last, order.OrderDate, "Simulator", "Model", "NA");
                    //portfolio.UpdateOrderTree(order, OrderStatus.Executed, double.NaN, executionLevel, executionDate);
                }
                return null;
            }
        }

        public static ConcurrentDictionary<Instrument, double> Costs = new ConcurrentDictionary<Instrument, double>();
        public static ConcurrentDictionary<Instrument, double> Notional = new ConcurrentDictionary<Instrument, double>();
        public static ConcurrentDictionary<Instrument, double> Contracts = new ConcurrentDictionary<Instrument, double>();

        /// <summary>        
        /// Function: Receive execution levels for a given portfolio. This function is usually used for historical simulations.
        /// </summary>
        /// <param name="executionDate">reference date for the executed orders</param>   
        /// <param name="portfolio">reference portfolio</param>
        public static void ReceiveExecutionLevels(DateTime executionDate, Portfolio portfolio)
        {
            Dictionary<int, Dictionary<string, Order>> orders = portfolio.OpenOrders(executionDate, true);
            if (orders != null)
            {
                foreach (Dictionary<string, Order> os in orders.Values)
                    foreach (Order order in os.Values)
                    {
                        if (order.Status == OrderStatus.Submitted)
                        {
                            Instrument instrument = order.Instrument;
                            BusinessDay date = Calendar.FindCalendar("All").GetBusinessDay(executionDate);

                            if (date != null)
                            {
                                //double ask = instrument[executionDate, TimeSeriesType.Ask, instrument.InstrumentType == InstrumentType.Strategy ? TimeSeriesRollType.Last : TimeSeriesRollType.Last];
                                //double bid = instrument[executionDate, TimeSeriesType.Bid, instrument.InstrumentType == InstrumentType.Strategy ? TimeSeriesRollType.Last : TimeSeriesRollType.Last];
                                double executionLevel = instrument[executionDate, TimeSeriesType.Last, instrument.InstrumentType == InstrumentType.Strategy ? TimeSeriesRollType.Last : TimeSeriesRollType.Last];

                                //if (order.Unit > 0 && !double.IsNaN(ask))
                                //    executionLevel = ask;
                                //else if (order.Unit < 0 && !double.IsNaN(bid))
                                //    executionLevel = bid;

                                if (instrument as Security != null)
                                    executionLevel *= (instrument as Security).PointSize;


                                if (!double.IsNaN(executionLevel))
                                {
                                    if (Math.Abs(order.Unit) >= 1)
                                    {
                                        if (instrument.InstrumentType == InstrumentType.Equity || instrument.InstrumentType == InstrumentType.ETF || instrument.InstrumentType == InstrumentType.Fund || instrument.InstrumentType == InstrumentType.Index || instrument.InstrumentType == InstrumentType.Strategy || instrument.InstrumentType == InstrumentType.SpreadBet || instrument.InstrumentType == InstrumentType.Future || instrument.InstrumentType == InstrumentType.Commodity)
                                        {
                                            double n_contracts = order.Unit;

                                            double exec_fee = 0.0;
                                            double exec_min_fee = 0.0;
                                            double exec_max_fee = 0.0;

                                            Dictionary<int, Dictionary<int, Instruction>> instructions = Factory.Instructions();

                                            if (instructions.ContainsKey(portfolio.ID))
                                            {
                                                if (instructions[portfolio.ID].ContainsKey(instrument.ID))
                                                {
                                                    exec_fee = instructions[portfolio.ID][instrument.ID].ExecutionFee;
                                                    exec_min_fee = instructions[portfolio.ID][instrument.ID].ExecutionMinFee;
                                                    exec_max_fee = instructions[portfolio.ID][instrument.ID].ExecutionMaxFee;
                                                }
                                                else if (instrument.InstrumentType == InstrumentType.Future && instructions[portfolio.ID].ContainsKey((instrument as Future).UnderlyingID))
                                                {
                                                    exec_fee = instructions[portfolio.ID][(instrument as Future).UnderlyingID].ExecutionFee;
                                                    exec_min_fee = instructions[portfolio.ID][(instrument as Future).UnderlyingID].ExecutionMinFee;
                                                    exec_max_fee = instructions[portfolio.ID][(instrument as Future).UnderlyingID].ExecutionMaxFee;
                                                }
                                                else if (instructions[portfolio.ID].ContainsKey(0))
                                                {
                                                    exec_fee = instructions[portfolio.ID][0].ExecutionFee;
                                                    exec_min_fee = instructions[portfolio.ID][0].ExecutionMinFee;
                                                    exec_max_fee = instructions[portfolio.ID][0].ExecutionMaxFee;
                                                }
                                            }

                                            else if (instructions.ContainsKey(0))
                                            {
                                                if (instructions[0].ContainsKey(instrument.ID))
                                                {
                                                    exec_fee = instructions[0][instrument.ID].ExecutionFee;
                                                    exec_min_fee = instructions[0][instrument.ID].ExecutionMinFee;
                                                    exec_max_fee = instructions[0][instrument.ID].ExecutionMaxFee;
                                                }
                                                else if (instrument.InstrumentType == InstrumentType.Future && instructions[0].ContainsKey((instrument as Future).UnderlyingID))
                                                {
                                                    exec_fee = instructions[0][(instrument as Future).UnderlyingID].ExecutionFee;
                                                    exec_min_fee = instructions[0][(instrument as Future).UnderlyingID].ExecutionMinFee;
                                                    exec_max_fee = instructions[0][(instrument as Future).UnderlyingID].ExecutionMaxFee;
                                                }
                                                else if (instructions[0].ContainsKey(0))
                                                {
                                                    exec_fee = instructions[0][0].ExecutionFee;
                                                    exec_min_fee = instructions[0][0].ExecutionMinFee;
                                                    exec_max_fee = instructions[0][0].ExecutionMaxFee;
                                                }
                                            }

                                            if (exec_fee < 0)
                                            {
                                                exec_fee = -exec_fee;
                                                exec_fee *= executionLevel;
                                            }

                                            if (exec_min_fee < 0)
                                            {
                                                exec_min_fee = -exec_min_fee;
                                                exec_min_fee *= executionLevel * Math.Abs(order.Unit);
                                            }

                                            if (exec_max_fee < 0)
                                            {
                                                exec_max_fee = -exec_max_fee;
                                                exec_max_fee *= executionLevel * Math.Abs(order.Unit);
                                            }


                                            if (Math.Abs(order.Unit) * exec_fee < exec_min_fee)
                                                exec_fee = exec_min_fee / Math.Abs(order.Unit);
                                            else if (exec_max_fee != 0.0 && (Math.Abs(order.Unit) * exec_fee) > exec_max_fee)
                                                exec_fee = exec_max_fee / Math.Abs(order.Unit);

                                            if (!Costs.ContainsKey(portfolio))
                                                Costs.TryAdd(portfolio, 0);
                                            if (!Notional.ContainsKey(portfolio))
                                                Notional.TryAdd(portfolio, 0);
                                            if (!Contracts.ContainsKey(portfolio))
                                                Contracts.TryAdd(portfolio, 0);

                                            Costs[portfolio] += exec_fee * Math.Abs(order.Unit);
                                            Notional[portfolio] += executionLevel * Math.Abs(order.Unit);
                                            Contracts[portfolio] += Math.Abs(order.Unit);


                                            double marketImpact = 0;
                                            if (SimulateMarketImpact != null)
                                            {
                                                double adv = 0;
                                                BusinessDay bday = order.Portfolio.Calendar.GetClosestBusinessDay(executionDate, Numerics.Util.TimeSeries.DateSearchType.Previous);
                                                Instrument ins = order.Instrument;
                                                for (int i = 0; i < 10; i++)
                                                {
                                                    double fx = CurrencyPair.Convert(1.0, bday.AddBusinessDays(-i).DateTime, order.Portfolio.Currency, order.Instrument.Currency);
                                                    double price_t = ins[bday.AddBusinessDays(-i).DateTime, TimeSeriesType.Last, TimeSeriesRollType.Last] * (ins is Security ? (ins as Security).PointSize : 1.0) * fx;
                                                    price_t = Double.IsNaN(price_t) ? 0.0 : price_t;
                                                    double volume = ins[bday.AddBusinessDays(-i).DateTime, TimeSeriesType.Volume, TimeSeriesRollType.Last];
                                                    volume = Double.IsNaN(volume) ? 0.0 : volume;

                                                    adv += price_t * volume;
                                                }
                                                adv /= 10;
                                                double notional_trade = Math.Abs(order.Unit) * order.Instrument[bday.DateTime, TimeSeriesType.Last, TimeSeriesRollType.Last] * (ins is Security ? (ins as Security).PointSize : 1.0) * CurrencyPair.Convert(1.0, bday.DateTime, order.Portfolio.Currency, order.Instrument.Currency);
                                                marketImpact = executionLevel * (notional_trade / adv) * SimulateMarketImpact(executionDate, instrument);
                                            }

                                            if (order.Unit > 0)
                                                executionLevel += exec_fee + marketImpact;
                                            else if (order.Unit < 0)
                                                executionLevel -= exec_fee + marketImpact;

                                            executionLevel = Math.Max(0.001, executionLevel);

                                            //if (instrument.InstrumentType == InstrumentType.Future)
                                            //{
                                            //    Instrument underlying = (instrument as Future).Underlying;
                                            //    double value = underlying.ExecutionCost;
                                            //    if (value < 0)
                                            //    {
                                            //        value = -value;
                                            //        if (order.Unit > 0)
                                            //            executionLevel += value * executionLevel;
                                            //        else
                                            //            executionLevel -= value * executionLevel;
                                            //    }
                                            //    else
                                            //    {
                                            //        value *= (instrument as Future).PointSize;
                                            //        if (order.Unit > 0)
                                            //            executionLevel += value;
                                            //        else
                                            //            executionLevel -= value;
                                            //    }
                                            //}
                                            //else
                                            //{
                                            //    //Instrument underlying = (instrument as Future).Underlying;
                                            //    double value = instrument.ExecutionCost;
                                            //    if (value < 0)
                                            //    {
                                            //        value = -value;
                                            //        if (order.Unit > 0)
                                            //            executionLevel += value * executionLevel;
                                            //        else
                                            //            executionLevel -= value * executionLevel;
                                            //    }
                                            //    else
                                            //    {
                                            //        //value *= (instrument as Future).PointSize;
                                            //        if (order.Unit > 0)
                                            //            executionLevel += value;
                                            //        else
                                            //            executionLevel -= value;
                                            //    }

                                            //    executionLevel = Math.Max(0.0001, executionLevel);
                                            //}
                                        }
                                    }

                                    portfolio.UpdateOrderTree(order, OrderStatus.Executed, double.NaN, executionLevel, executionDate);
                                }
                                else
                                    portfolio.UpdateOrderTree(order, OrderStatus.NotExecuted, double.NaN, executionLevel, executionDate);
                            }
                        }
                    }
            }
        }

        /// <summary>        
        /// Function: Add an instruction to the market class
        /// </summary>
        /// <param name="instruction">reference instruction</param>   
        public static void AddInstruction(Instruction instruction)
        {
            Factory.AddInstruction(instruction);
        }

        /// <summary>        
        /// Function: returns an instruction for a given order
        /// </summary>
        /// <param name="order">reference order</param>  
        public static Instruction GetInstruction(Order order)
        {
            Dictionary<int, Dictionary<int, Instruction>> list = Instructions();

            int masterID = order.Portfolio.MasterPortfolio.ID;

            Instruction inherit = new Instruction(null, null, "Inherit", " ", " ", 0, 0, 0, 0, 0, 0);

            Instruction defaultInstruction = list.ContainsKey(0) && list[0].ContainsKey(0) ? list[0][0] : null;
            Instruction portfolioDefault = list.ContainsKey(masterID) && list[masterID].ContainsKey(0) ? list[masterID][0] : null;

            if (list.ContainsKey(masterID) && list[masterID].ContainsKey(order.InstrumentID))
                return list[masterID][order.InstrumentID];
            else if (list.ContainsKey(masterID) && (order.Instrument.InstrumentType == InstrumentType.Future) && list[masterID].ContainsKey((order.Instrument as Future).UnderlyingID))
                return list[masterID][(order.Instrument as Future).UnderlyingID];
            else if (portfolioDefault != null)
                return portfolioDefault;
            else
                return inherit;
        }

        /// <summary>        
        /// Function: returns a dictionary of all instructions.
        /// The dictionary's keys are portfolio IDs and the values are a second set of dictionaries.
        /// The second set of dictionary's keys are instrument IDs and the values are the instructions.
        /// </summary>
        public static Dictionary<int, Dictionary<int, Instruction>> Instructions()
        {
            return Factory.Instructions();
        }


        private static Dictionary<string, Market.ClientConnection> _connectionDB = new Dictionary<string, Market.ClientConnection>();
        /// <summary>        
        /// Function: returns a dictionary of all destinations categories by their respective clients.
        /// The dictionary's keys are clients and the values are a second set of lists containing the respective destinations.        
        /// </summary>
        public static Dictionary<string, List<string>> ClientsDestinations()
        {
            Dictionary<string, List<string>> result = new Dictionary<string, List<string>>();

            result.Add("Simulator", new List<string> { "Default" });
            result.Add("Inherit", new List<string> { " " });

            foreach (Market.ClientConnection connection in _connectionDB.Values)
                if (!result.ContainsKey(connection.Name))
                    result.Add(connection.Name, connection.Destinations);

            return result;
        }


        /// <summary>        
        /// Function: Book orders.
        /// Note: During live paper trading also execute simulated orders.
        /// </summary>
        public static void BookOrders(DateTime t)
        {

            //foreach (Portfolio portfolio in portfolios.Values.ToList()

            System.Threading.Tasks.Parallel.ForEach(portfolios.Values.ToList(), (portfolio) =>
            {
                if (!(portfolio.MasterPortfolio != null && portfolio.MasterPortfolio == portfolio))
                    RemovePortfolio(portfolio);

                if (portfolio.Strategy != null && !portfolio.Strategy.Simulating && !portfolio._loading && !portfolio.Strategy.Executing)
                {
                    lock (portfolio.Strategy.ExecutionLock)
                    {
                        t = DateTime.Now;
                        portfolio.Strategy.Executing = true;
                        try
                        {
                            //if (portfolio != null)
                            {
                                bool book = false;
                                var booked = new List<Order>();
                                //DateTime t = DateTime.Now;
                                Dictionary<int, Dictionary<string, Order>> orders = portfolio.OpenOrders(t, true);
                                if (orders != null)
                                {
                                    if (Portfolio.DebugPositions)
                                        Console.WriteLine("BOOKING: " + portfolio.MasterPortfolio.Strategy + " " + t.ToString("yyyy-MM-dd hh:mm:ss.fff") + " " + DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss.fff"));

                                    foreach (Dictionary<string, Order> os in orders.Values.ToList())
                                    {
                                        foreach (Order order in os.Values.ToList())
                                        {
                                            //Console.WriteLine("Order: " + order + " " + order.Client);
                                            if (order.Status == OrderStatus.Submitted && order.Client == "Simulator")
                                                if (false)
                                                {
                                                    portfolio.UpdateOrderTree(order, OrderStatus.NotExecuted, double.NaN, double.NaN, t);
                                                    Console.WriteLine("Order Not Executed Test");
                                                }
                                                else if (order.OrderDate.Date == DateTime.Today)
                                                {

                                                    try
                                                    {
                                                        double last = order.Instrument[t, TimeSeriesType.Last, TimeSeriesRollType.Last];
                                                        double last_5h = order.Instrument[t.AddHours(-5), TimeSeriesType.Last, TimeSeriesRollType.Last];

                                                        //if (last != last_5h)
                                                        {
                                                            double bid = last;// order.Instrument[t, TimeSeriesType.Bid, TimeSeriesRollType.Last];
                                                            double ask = last;// order.Instrument[t, TimeSeriesType.Ask, TimeSeriesRollType.Last];

                                                            if (double.IsNaN(bid))
                                                                bid = last;

                                                            if (double.IsNaN(ask))
                                                                ask = last;

                                                            if (order.Type == OrderType.Market || (order.Limit >= ask && order.Unit > 0) || (order.Limit <= bid && order.Unit < 0))
                                                            {

                                                                double n_contracts = order.Unit;

                                                                double exec_fee = 0.0;
                                                                double exec_min_fee = 0.0;
                                                                double exec_max_fee = 0.0;

                                                                double minSize = 0.0;
                                                                double minStep = 0.0;
                                                                double margin = 0.0;

                                                                double executionLevel = (order.Unit < 0 ? bid : ask) * (order.Instrument as Security != null ? (order.Instrument as Security).PointSize : 1);

                                                                Dictionary<int, Dictionary<int, Instruction>> instructions = Factory.Instructions();

                                                                if (instructions.ContainsKey(portfolio.ID))
                                                                {
                                                                    if (instructions[portfolio.ID].ContainsKey(order.Instrument.ID))
                                                                    {
                                                                        exec_fee = instructions[portfolio.ID][order.Instrument.ID].ExecutionFee;
                                                                        exec_min_fee = instructions[portfolio.ID][order.Instrument.ID].ExecutionMinFee;
                                                                        exec_max_fee = instructions[portfolio.ID][order.Instrument.ID].ExecutionMaxFee;

                                                                        minSize = instructions[portfolio.ID][order.Instrument.ID].MinSize;
                                                                        minStep = instructions[portfolio.ID][order.Instrument.ID].MinStep;
                                                                        margin = instructions[portfolio.ID][order.Instrument.ID].Margin;
                                                                    }
                                                                    else if (order.Instrument.InstrumentType == InstrumentType.Future && instructions[portfolio.ID].ContainsKey((order.Instrument as Future).UnderlyingID))
                                                                    {
                                                                        exec_fee = instructions[portfolio.ID][(order.Instrument as Future).UnderlyingID].ExecutionFee;
                                                                        exec_min_fee = instructions[portfolio.ID][(order.Instrument as Future).UnderlyingID].ExecutionMinFee;
                                                                        exec_max_fee = instructions[portfolio.ID][(order.Instrument as Future).UnderlyingID].ExecutionMaxFee;

                                                                        minSize = instructions[portfolio.ID][(order.Instrument as Future).UnderlyingID].MinSize;
                                                                        minStep = instructions[portfolio.ID][(order.Instrument as Future).UnderlyingID].MinStep;
                                                                        margin = instructions[portfolio.ID][(order.Instrument as Future).UnderlyingID].Margin;
                                                                    }

                                                                    else if (instructions[portfolio.ID].ContainsKey(0))
                                                                    {
                                                                        exec_fee = instructions[portfolio.ID][0].ExecutionFee;
                                                                        exec_min_fee = instructions[portfolio.ID][0].ExecutionMinFee;
                                                                        exec_max_fee = instructions[portfolio.ID][0].ExecutionMaxFee;

                                                                        minSize = instructions[portfolio.ID][0].MinSize;
                                                                        minStep = instructions[portfolio.ID][0].MinStep;
                                                                        margin = instructions[portfolio.ID][0].Margin;
                                                                    }
                                                                }

                                                                else if (instructions.ContainsKey(0))
                                                                {
                                                                    if (instructions[0].ContainsKey(order.Instrument.ID))
                                                                    {
                                                                        exec_fee = instructions[0][order.Instrument.ID].ExecutionFee;
                                                                        exec_min_fee = instructions[0][order.Instrument.ID].ExecutionMinFee;
                                                                        exec_max_fee = instructions[0][order.Instrument.ID].ExecutionMaxFee;

                                                                        minSize = instructions[0][order.Instrument.ID].MinSize;
                                                                        minStep = instructions[0][order.Instrument.ID].MinStep;
                                                                        margin = instructions[0][order.Instrument.ID].Margin;
                                                                    }
                                                                    else if (order.Instrument.InstrumentType == InstrumentType.Future && instructions[0].ContainsKey((order.Instrument as Future).UnderlyingID))
                                                                    {
                                                                        exec_fee = instructions[0][(order.Instrument as Future).UnderlyingID].ExecutionFee;
                                                                        exec_min_fee = instructions[0][(order.Instrument as Future).UnderlyingID].ExecutionMinFee;
                                                                        exec_max_fee = instructions[0][(order.Instrument as Future).UnderlyingID].ExecutionMaxFee;

                                                                        minSize = instructions[0][(order.Instrument as Future).UnderlyingID].MinSize;
                                                                        minStep = instructions[0][(order.Instrument as Future).UnderlyingID].MinStep;
                                                                        margin = instructions[0][(order.Instrument as Future).UnderlyingID].Margin;
                                                                    }
                                                                    else if (instructions[0].ContainsKey(0))
                                                                    {
                                                                        exec_fee = instructions[0][0].ExecutionFee;
                                                                        exec_min_fee = instructions[0][0].ExecutionMinFee;
                                                                        exec_max_fee = instructions[0][0].ExecutionMaxFee;

                                                                        minSize = instructions[0][0].MinSize;
                                                                        minStep = instructions[0][0].MinStep;
                                                                        margin = instructions[0][0].Margin;
                                                                    }
                                                                }

                                                                if (exec_fee < 0)
                                                                {
                                                                    exec_fee = -exec_fee;
                                                                    exec_fee *= executionLevel;
                                                                }

                                                                if (exec_min_fee < 0)
                                                                {
                                                                    exec_min_fee = -exec_min_fee;
                                                                    exec_min_fee *= executionLevel * Math.Abs(order.Unit);
                                                                }

                                                                if (exec_max_fee < 0)
                                                                {
                                                                    exec_max_fee = -exec_max_fee;
                                                                    exec_max_fee *= executionLevel * Math.Abs(order.Unit);
                                                                }


                                                                if (Math.Abs(order.Unit) * exec_fee < exec_min_fee)
                                                                    exec_fee = exec_min_fee / Math.Abs(order.Unit);
                                                                else if (exec_max_fee != 0.0 && (Math.Abs(order.Unit) * exec_fee) > exec_max_fee)
                                                                    exec_fee = exec_max_fee / Math.Abs(order.Unit);

                                                                if (!Costs.ContainsKey(portfolio))
                                                                    Costs.TryAdd(portfolio, 0);
                                                                if (!Notional.ContainsKey(portfolio))
                                                                    Notional.TryAdd(portfolio, 0);
                                                                if (!Contracts.ContainsKey(portfolio))
                                                                    Contracts.TryAdd(portfolio, 0);

                                                                Costs[portfolio] += exec_fee * Math.Abs(order.Unit);
                                                                Notional[portfolio] += executionLevel * Math.Abs(order.Unit);
                                                                Contracts[portfolio] += Math.Abs(order.Unit);

                                                                if (order.Unit > 0)
                                                                    executionLevel += exec_fee;
                                                                else if (order.Unit < 0)
                                                                    executionLevel -= exec_fee;


                                                                Console.WriteLine("Execute Order: " + order.Instrument + " " + order.Unit + " " + DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss.fff"));

                                                                OrderRecord ord = Market.GetOrderRecord(order.ID);
                                                                if (ord != null)
                                                                {
                                                                    ord.Price = (decimal)(executionLevel / (order.Instrument as Security != null ? (order.Instrument as Security).PointSize : 1));
                                                                    portfolio.UpdateOrderTree(order, OrderStatus.Executed, double.NaN, executionLevel, t);
                                                                    ord.Status = "Executed";

                                                                    book = true;
                                                                    Market.RemoveOrderRecord(order.ID);

                                                                    UpdateRecord(ord);
                                                                }
                                                                else
                                                                {

                                                                    book = true;
                                                                    portfolio.UpdateOrderTree(order, OrderStatus.Executed, double.NaN, (order.Unit < 0 ? bid : ask), t);
                                                                }

                                                                booked.Add(order);
                                                            }
                                                        }
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        Console.WriteLine(e);
                                                    }
                                                }
                                        }
                                    }
                                    var pos = portfolio.MasterPortfolio.Strategy.Tree.BookOrders(t);

                                    if (book || pos.Count != 0)
                                    {
                                        UpdateRecord(portfolio.MasterPortfolio, null);

                                        //System.Threading.Thread th = new System.Threading.Thread(() =>
                                        //{
                                        //    lock (portfolio.MasterPortfolio.Strategy.executionLock)
                                        //    {
                                        portfolio.MasterPortfolio.Strategy.Tree.NAVCalculation(t);
                                        portfolio.MasterPortfolio.Strategy.Tree.Save();
                                        portfolio.MasterPortfolio.Strategy.Tree.SaveNewPositions();
                                        //portfolio.MasterPortfolio.Strategy.Executing = false;
                                        //    }
                                        //});
                                        //th.Start();

                                    }

                                    if (Portfolio.DebugPositions)
                                        Console.WriteLine("BOOKED: " + pos.Count + " " + portfolio.MasterPortfolio.Strategy + " " + t.ToString("yyyy-MM-dd hh:mm:ss.fff") + " " + DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss.fff"));




                                }
                                else
                                    portfolio.MasterPortfolio.Strategy.Tree.BookOrders(t);



                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.ToString());
                        }
                        portfolio.MasterPortfolio.Strategy.Executing = false;
                    }
                }
            });
        }

        private static Market.UpdateEvent _updateDB_All;
        private static Dictionary<int, Market.UpdateEvent> _updateDB = new Dictionary<int, UpdateEvent>();
        /// <summary>        
        /// Function: Add an update function that is called by the Market connection when an order status is changed.
        /// This function is usually used to implement a UI change but can be used for any external updating of information linked to an order.
        /// </summary>
        public static void AddUpdateEvent(Portfolio portfolio, Market.UpdateEvent updateEvent)
        {
            if (portfolio != null)
            {
                //_updateDB.Add(portfolio.ID, updateEvent);
                if (_updateDB.ContainsKey(portfolio.ID))
                    _updateDB[portfolio.ID] += updateEvent;
                else
                    _updateDB.Add(portfolio.ID, updateEvent);
            }

            _updateDB_All += updateEvent;
        }

        /// <summary>        
        /// Function: remove an update function that is called by the Market connection when an order status is changed.
        /// </summary>
        public static void RemoveUpdateEvent(Portfolio portfolio)//Market.UpdateEvent updateEvent)
        {
            _updateDB.Remove(portfolio.ID);
        }

        /// <summary>        
        /// Function: update all the update events added to the market with this record
        /// </summary>
        public static void UpdateRecord(OrderRecord record)
        {
            UpdateRecord(Instrument.FindInstrument(record.RootPortfolioID) as Portfolio, record);
        }

        /// <summary>        
        /// Function: update all the update events added to the market with this record
        /// </summary>
        public static void UpdateRecord(Portfolio portfolio, OrderRecord record)
        {
            try
            {
                if (_updateDB != null && _updateDB.ContainsKey(portfolio.MasterPortfolio.ID))
                    _updateDB[portfolio.MasterPortfolio.ID](record);

                if (_updateDB_All != null)
                    _updateDB_All(record);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        /// <summary>        
        /// Function: Update and a fill value and time for a specific order. This is usually called by the market connection when the brokers reverts with a fill confirmation.
        /// </summary>
        public static void RecordFillValue(Order order, DateTime fillTime, double fillValue)
        {
            order.Portfolio.MasterPortfolio.UpdateOrderTree(order, OrderStatus.Executed, double.NaN, fillValue, fillTime);
            OrderRecord record = Market.GetOrderRecord(order.ID);
            record.Price = (decimal)order.ExecutionLevel;
            record.Status = "Executed";
            UpdateRecord(record);
            Market.RemoveOrderRecord(order.ID);
        }

        /// <summary>        
        /// Function: Returns the OrderRecord object related to an order ID.
        /// The OrderRecord is an object that implements a notification mechanism to inform external systems about changes to the order.
        /// This is implemented outside of the Order object in order to keep the internal process as quick and lightweight as possible.
        /// </summary>
        public static OrderRecord GetOrderRecord(string id)
        {
            if (orderDict.ContainsKey(id))
                return orderDict[id];
            return null;
        }

        /// <summary>        
        /// Function: Remove an order record when it is not required.        
        /// </summary>
        private static void RemoveOrderRecord(string id)
        {
            if (orderDict.ContainsKey(id))
                orderDict.Remove(id);
        }
    }

    /// <summary>
    /// Class containing the structure for cash message update.
    /// The Instruction is defined by:
    /// Portfolio -> The portfolio the instruction is linked to
    /// Value -> Amount to be updated
    /// Currency -> Currency of amount
    /// </summary>
    public class CashUpdateMessage
    {
        /// <summary>
        /// Property: returns the portfolio the message is linked to
        /// </summary>
        public Portfolio Portfolio { get; set; }

        /// <summary>
        /// Property: returns the value the message is linked to
        /// </summary>
        public double Value { get; set; }

        /// <summary>
        /// Property: returns the currency the message is linked to
        /// </summary>
        public Currency Currency { get; set; }
        public CashUpdateMessage(Portfolio portfolio, double value, Currency currency)
        {
            this.Portfolio = portfolio;
            this.Value = value;
            this.Currency = currency;
        }
    }

    /// <summary>
    /// Class containing the information required by the Market Class to route an order through the correct broker technology.
    /// The Instruction is defined by:
    /// Portfolio -> The portfolio the instruction is linked to
    /// Instrument -> The instrument  the instruction is meant to transact
    /// Client -> The string identifier for the API connection used to route the order. Could be EMSX API, QuickFIX, IBAPI, IGAPI, etc.
    /// Destination -> The string identifier to which broker/router to use for the defined client. If the Client is EMSX, the destination is the broker identifier
    /// Account -> The account where the order should be routed to and settled
    /// ExecutionFee -> Fee that is not passed through the API information if any
    /// </summary>
    public class Instruction
    {
        /// <summary>
        /// Property: returns the portfolio the instruction is linked to
        /// </summary>
        public Portfolio Portfolio { get; private set; }

        /// <summary>
        /// Property: returns the instrument the instruction is meant to transact
        /// </summary>
        public Instrument Instrument { get; private set; }

        /// <summary>
        /// Property: returns the string identifier for the API connection used to route the order. Could be EMSX API, QuickFIX, IBAPI, IGAPI, etc.
        /// </summary>
        public string Client { get; set; }

        /// <summary>
        /// Property: returns the string identifier to which broker/router to use for the defined client. If the Client is EMSX, the destination is the broker identifier
        /// </summary>
        public string Destination { get; set; }

        /// <summary>
        /// Property: returns the account where the order should be routed to and settled
        /// </summary>
        public string Account { get; set; }

        /// <summary>
        /// Property: returns the fee that is not passed through the API information if any
        /// </summary>
        public double ExecutionFee { get; set; }

        /// <summary>
        /// Property: returns the minimum fee that is not passed through the API information if any
        /// </summary>
        public double ExecutionMinFee { get; set; }

        /// <summary>
        /// Property: returns the maximum fee that is not passed through the API information if any
        /// </summary>
        public double ExecutionMaxFee { get; set; }

        /// <summary>
        /// Property: returns the margin
        /// </summary>
        public double Margin { get; set; }

        /// <summary>
        /// Property: returns the minimum deal size
        /// </summary>
        public double MinSize { get; set; }

        /// <summary>
        /// Property: returns the minimum increment based off the min deal size
        /// </summary>
        public double MinStep { get; set; }

        public Instruction(Portfolio portfolio, Instrument instrument, string client, string destination, string account, double executionfee, double minexecutionfee, double maxexecutionfee, double minsize, double minstep, double margin)
        {
            Portfolio = portfolio;
            Instrument = instrument;
            Client = client;
            Destination = destination;
            Account = account;
            ExecutionFee = executionfee;
            ExecutionMinFee = minexecutionfee;
            ExecutionMaxFee = maxexecutionfee;

            MinSize = minsize;
            MinStep = minstep;
            Margin = margin;
        }
    }

    /// <summary>
    /// The OrderRecord class is used to notify external systems regarding status changes.
    /// This is usually used by UI updates.
    /// </summary>
    public class OrderRecord : NotifyPropertyChangedBase, IEquatable<OrderRecord>
    {
        public OrderRecord()
        {
        }

        public bool Equals(OrderRecord other)
        {
            if (((object)other) == null)
                return false;
            return _orderID == other._orderID;
        }
        public override bool Equals(object other)
        {
            try { return Equals((OrderRecord)other); }
            catch { return false; }
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator ==(OrderRecord x, OrderRecord y)
        {
            if (((object)x) == null && ((object)y) == null)
                return true;
            else if (((object)x) == null)
                return false;

            return x.Equals(y);
        }
        public static bool operator !=(OrderRecord x, OrderRecord y)
        {
            return !(x == y);
        }

        private string date = "";
        public string Date
        {
            get { return date; }
            set { date = value; OnPropertyChanged("Date"); }
        }

        private string _name = "";
        public string Name
        {
            get { return _name; }
            set { _name = value; OnPropertyChanged("Name"); }
        }

        private string _side = "";
        public string Side
        {
            get { return _side; }
            set { _side = value; OnPropertyChanged("Side"); }
        }

        private string _ordType = "";
        public string Type
        {
            get { return _ordType; }
            set { _ordType = value; OnPropertyChanged("Type"); }
        }

        private decimal _price = 0m;
        public decimal Price
        {
            get { return _price; }
            set { _price = value; OnPropertyChanged("Price"); }
        }

        private string _ordQty;
        public string Unit
        {
            get { return _ordQty; }
            set { _ordQty = value; OnPropertyChanged("Unit"); }
        }

        private string _status { get; set; }
        public string Status
        {
            get { return _status; }
            set { _status = value; OnPropertyChanged("Status"); }
        }

        private string _orderID = "(unset)";
        public string OrderID
        {
            get { return _orderID; }
            set { _orderID = value; OnPropertyChanged("OrderID"); }
        }

        private int _rootPortfolioId { get; set; }
        public int RootPortfolioID
        {
            get { return _rootPortfolioId; }
            set { _rootPortfolioId = value; OnPropertyChanged("MasterID"); }
        }

        private string _rootPortfolioName { get; set; }
        public string RootPortfolioName
        {
            get { return _rootPortfolioName; }
            set { _rootPortfolioName = value; OnPropertyChanged("MasterName"); }
        }


        private int _parentPortfolioId { get; set; }
        public int ParentPortfolioID
        {
            get { return _parentPortfolioId; }
            set { _parentPortfolioId = value; OnPropertyChanged("PortfolioID"); }
        }

    }


    public class ClientRecord : NotifyPropertyChangedBase
    {
        private string date = "";
        public string Date
        {
            get { return date; }
            set { date = value; OnPropertyChanged("Date"); }
        }

        private string _name = "";
        public string Name
        {
            get { return _name; }
            set { _name = value; OnPropertyChanged("Name"); }
        }

        private string _pnl = "";
        public string PNL
        {
            get { return _pnl; }
            set { _pnl = value; OnPropertyChanged("PNL"); }
        }

        private string _aum = "";
        public string AUM
        {
            get { return _aum; }
            set { _aum = value; OnPropertyChanged("AUM"); }
        }

        private string _status { get; set; }
        public string Status
        {
            get { return _status; }
            set { _status = value; OnPropertyChanged("Status"); }
        }

        private int _strategyId { get; set; }
        public int StrategyID
        {
            get { return _strategyId; }
            set { _strategyId = value; OnPropertyChanged("StrategyID"); }
        }

    }

    public abstract class NotifyPropertyChangedBase : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged Members

        /// <summary>
        /// Raised when a property on this object has a new value.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises this object's PropertyChanged event.
        /// </summary>
        /// <param name="propertyName">The property that has a new value.</param>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                var e = new PropertyChangedEventArgs(propertyName);
                handler(this, e);
            }
        }

        #endregion // INotifyPropertyChanged Members
    }
}
