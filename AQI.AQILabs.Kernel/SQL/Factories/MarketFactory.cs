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

using System.Data;
using AQI.AQILabs.Kernel.Factories;

using QuantApp.Kernel;

namespace AQI.AQILabs.Kernel.Adapters.SQL.Factories
{
    public class SQLMarketFactory : IMarketFactory
    {
        public readonly static object objLock = new object();

        private static string _instructionsTableName = "Instructions";
        private DataTable _instructionTable = null;

        public void Initialize()
        {
            if (_instructionTable == null)
                _instructionTable = Database.DB["Kernel"].GetDataTable(_instructionsTableName, null, null);
        }


        private Dictionary<string, Market.ClientConnection> _connectionDB = new Dictionary<string, Market.ClientConnection>();
        public void AddConnection(Market.ClientConnection connection)
        {
            if (!_connectionDB.ContainsKey(connection.Name))
                _connectionDB.Add(connection.Name, connection);
        }

        public void RemoveConnection(string name)
        {
            if (_connectionDB.ContainsKey(name))
                _connectionDB.Remove(name);
        }
        public Dictionary<string, Market.ClientConnection> GetClientConnetions()
        {
            return _connectionDB;
        }

        public object UpdateRecord(object obj) { return null; }


        public Dictionary<string, List<string>> ClientsDestinations()
        {
            Dictionary<string, List<string>> result = new Dictionary<string, List<string>>();

            result.Add("Simulator", new List<string> { "Default" });

            return result;
        }

        public void Update(Order order)
        {
        }

        private static Dictionary<string, double> _executionLevelsDB = new Dictionary<string, double>();

        protected List<Portfolio> portfolios = new List<Portfolio>();

        public void AddPortfolio(Portfolio portfolio)
        {
            if (!portfolios.Contains(portfolio))
                portfolios.Add(portfolio);
        }


        public void RemovePortfolio(Portfolio portfolio)
        {
            if (portfolios.Contains(portfolio))
                portfolios.Remove(portfolio);
        }


        public void Close()
        {
        }

        private string OrderKey(Order order, DateTime executionDate)
        {
            return OrderKey(order.Instrument, executionDate);
        }

        private string OrderKey(Instrument instrument, DateTime date)
        {
            return instrument.ID + "_" + date.ToBinary();
        }


        public double contracts = 0;

        public void ReceiveExecutionLevels(DateTime executionDate, Portfolio portfolio)
        {
            lock (objLock)
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
                                BusinessDay date = Calendar.FindCalendar("WE").GetBusinessDay(executionDate);
                                
                                if (date != null)
                                {
                                    double executionLevel = instrument[executionDate, TimeSeriesType.Last, instrument.InstrumentType == InstrumentType.Strategy ? TimeSeriesRollType.Last : TimeSeriesRollType.Last];

                                    if (instrument.InstrumentType == InstrumentType.Future)
                                        executionLevel *= (instrument as Future).PointSize;


                                    if (!double.IsNaN(executionLevel))
                                    {
                                        string key = OrderKey(order, executionDate);

                                        if (order.Unit != 0.0)
                                        {
                                            if (true)
                                            {
                                                if (instrument.InstrumentType == InstrumentType.Future)
                                                {
                                                    contracts += Math.Abs(order.Unit);
                                                    double n_contracts = order.Unit;

                                                    if (true)
                                                    {
                                                        double exec_fee = 0.0;

                                                        if (_InstructionDB.ContainsKey(portfolio.ID))
                                                        {
                                                            if (_InstructionDB[portfolio.ID].ContainsKey(instrument.ID))
                                                                exec_fee = _InstructionDB[portfolio.ID][instrument.ID].ExecutionFee;

                                                            else if (_InstructionDB[portfolio.ID].ContainsKey((instrument as Future).UnderlyingID))
                                                                exec_fee = _InstructionDB[portfolio.ID][(instrument as Future).UnderlyingID].ExecutionFee;

                                                            else if (_InstructionDB[portfolio.ID].ContainsKey(0))
                                                                exec_fee = _InstructionDB[portfolio.ID][0].ExecutionFee;
                                                        }

                                                        if (_InstructionDB.ContainsKey(0))
                                                        {
                                                            if (_InstructionDB[0].ContainsKey(instrument.ID))
                                                                exec_fee = _InstructionDB[0][instrument.ID].ExecutionFee;
                                                            else if (_InstructionDB[0].ContainsKey(0))
                                                                exec_fee = _InstructionDB[0][0].ExecutionFee;
                                                        }

                                                        if (order.Unit > 0)
                                                            executionLevel += exec_fee;
                                                        else if (order.Unit < 0)
                                                            executionLevel -= exec_fee;

                                                        Instrument underlying = (instrument as Future).Underlying;
                                                        double value = underlying.ExecutionCost;
                                                        if (value < 0)
                                                        {
                                                            value = -value;
                                                            if (order.Unit > 0)
                                                                executionLevel += value * executionLevel;
                                                            else
                                                                executionLevel -= value * executionLevel;
                                                        }
                                                        else
                                                        {
                                                            value *= (instrument as Future).PointSize;
                                                            if (order.Unit > 0)
                                                                executionLevel += value;
                                                            else
                                                                executionLevel -= value;
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    double exec = order.Instrument.ExecutionCost;

                                                    if (exec < 0)
                                                    {
                                                        exec = -exec;
                                                        if (order.Unit > 0)
                                                            executionLevel += exec * executionLevel;
                                                        else
                                                            executionLevel -= exec * executionLevel;
                                                    }
                                                    else
                                                    {
                                                        if (order.Unit > 0)
                                                            executionLevel += exec;
                                                        else
                                                            executionLevel -= exec;
                                                    }
                                                }
                                            }
                                        }

                                        portfolio.UpdateOrderTree(order, OrderStatus.Executed, double.NaN, executionLevel, executionDate);

                                        if (!_executionLevelsDB.ContainsKey(key))
                                            _executionLevelsDB.Add(key, executionLevel);
                                    }
                                    else
                                        portfolio.UpdateOrderTree(order, OrderStatus.NotExecuted, double.NaN, executionLevel, executionDate);
                                }
                            }
                        }
                }
            }
        }

        public void AddUpdateEvent(Market.UpdateEvent updateEvent) { }

        public void RemoveUpdateEvent(Market.UpdateEvent updateEvent) { }

        private T GetValue<T>(DataRow row, string columnname)
        {
            object res = null;
            if (row.RowState == DataRowState.Detached)
                return (T)res;
            if (typeof(T) == typeof(string))
                res = "";
            else if (typeof(T) == typeof(int))
                res = int.MinValue;
            else if (typeof(T) == typeof(double))
                res = double.NaN;
            else if (typeof(T) == typeof(DateTime))
                res = DateTime.MinValue;
            else if (typeof(T) == typeof(bool))
                res = false;
            object obj = row.Table.Columns.Contains(columnname) ? row[columnname] : null;
            if (obj is DBNull || obj == null)
                return (T)res;


            if (obj != null)
            {
                if (obj is Int64)
                    return (T)(object)Convert.ToInt32(obj);
            }

            return (T)obj;
        }



        private Dictionary<int, IMarketFactory> _IdDB = new Dictionary<int, IMarketFactory>();
        private Dictionary<string, IMarketFactory> _NameDB = new Dictionary<string, IMarketFactory>();

        private Dictionary<int, Dictionary<int, Instruction>> _InstructionDB = new Dictionary<int, Dictionary<int, Instruction>>();

        private void LoadInstructions()
        {
            lock (objLock)
            {
                if (_instructionTable == null)
                    _instructionTable = Database.DB["Kernel"].GetDataTable(_instructionsTableName, null, null);

                foreach (DataRow row in _instructionTable.Rows)
                {
                    int portfolioID = GetValue<int>(row, "PortfolioID");
                    int instrumentID = GetValue<int>(row, "InstrumentID");
                    string client = GetValue<string>(row, "Client");
                    string destination = GetValue<string>(row, "Destination");
                    string account = GetValue<string>(row, "Account");
                    double fee = GetValue<double>(row, "Execution");
                    double minfee = GetValue<double>(row, "MinExecution");
                    double maxfee = GetValue<double>(row, "MaxExecution");

                    double minsize = GetValue<double>(row, "MinSize");
                    double minstep = GetValue<double>(row, "MinStep");
                    double margin = GetValue<double>(row, "Margin");


                    Portfolio portfolio = portfolioID == 0 ? null : Instrument.FindInstrument(portfolioID) as Portfolio;
                    Instrument instrument = instrumentID == 0 ? null : Instrument.FindInstrument(instrumentID);
                    Instruction instruction = new Instruction(portfolio, instrument, client, destination, account, fee, minfee, maxfee, minsize, minstep, margin);

                    if (!_InstructionDB.ContainsKey(portfolioID))
                        _InstructionDB.Add(portfolioID, new Dictionary<int, Instruction>());

                    if (!_InstructionDB[portfolioID].ContainsKey(instrumentID))
                        _InstructionDB[portfolioID].Add(instrumentID, instruction);

                    _InstructionDB[portfolioID][instrumentID] = instruction;
                }
            }
        }

        public Dictionary<int, Dictionary<int, Instruction>> Instructions()
        {
            if (_InstructionDB.Count == 0)
                LoadInstructions();
            return _InstructionDB;
        }

        public void AddInstruction(Instruction instruction)
        {
            lock (objLock)
            {
                int portID = instruction.Portfolio == null ? 0 : instruction.Portfolio.ID;

                if (!_InstructionDB.ContainsKey(portID))
                    _InstructionDB.Add(portID, new Dictionary<int, Instruction>());

                int iid = instruction.Instrument == null ? 0 : instruction.Instrument.ID;

                if (!_InstructionDB[portID].ContainsKey(iid))
                    _InstructionDB[portID].Add(iid, instruction);

                _InstructionDB[portID][iid] = instruction;

                if (instruction.Portfolio != null && instruction.Portfolio.SimulationObject)
                    return;

                if (_instructionTable == null)
                    LoadInstructions();

                DataRowCollection rows = _instructionTable.Rows;
                DataRow[] rowArray = rows.Cast<DataRow>().ToArray();

                if (rows.Count == 0)
                {
                    DataRow r = _instructionTable.NewRow();
                    r["PortfolioID"] = portID;
                    r["InstrumentID"] = iid;
                    r["Client"] = instruction.Client;
                    r["Destination"] = instruction.Destination;
                    r["Account"] = instruction.Account;
                    r["Execution"] = instruction.ExecutionFee;
                    r["MinExecution"] = instruction.ExecutionMinFee;
                    r["MaxExecution"] = instruction.ExecutionMaxFee;


                    r["MinSize"] = instruction.MinSize;
                    r["MinStep"] = instruction.MinStep;
                    r["Margin"] = instruction.Margin;

                    _instructionTable.Rows.Add(r);

                    Database.DB["Kernel"].UpdateDataTable(_instructionTable);
                }
                else
                {
                    bool changed = false;
                    foreach (DataRow row in rowArray)
                        if (GetValue<int>(row, "PortfolioID") == portID && GetValue<int>(row, "InstrumentID") == iid)
                        {

                            row["Client"] = instruction.Client;
                            row["Destination"] = instruction.Destination;
                            row["Account"] = instruction.Account;
                            row["Execution"] = instruction.ExecutionFee;
                            row["MinExecution"] = instruction.ExecutionMinFee;
                            row["MaxExecution"] = instruction.ExecutionMaxFee;
                            row["MinSize"] = instruction.MinSize;
                            row["MinStep"] = instruction.MinStep;
                            row["Margin"] = instruction.Margin;
                            changed = true;
                        }

                    if (changed)
                        Database.DB["Kernel"].UpdateDataTable(_instructionTable);
                }
            }
        }

        public Instruction GetInstruction(Order order)
        {
            Dictionary<int, Dictionary<int, Instruction>> list = Instructions();

            int masterID = order.Portfolio.MasterPortfolio.ID;

            Instruction defaultInstruction = list.ContainsKey(0) && list[0].ContainsKey(0) ? list[0][0] : null;
            Instruction portfolioDefault = list.ContainsKey(masterID) && list[masterID].ContainsKey(0) ? list[masterID][0] : null;

            if (list.ContainsKey(masterID) && list[masterID].ContainsKey(order.InstrumentID))
                return list[masterID][order.InstrumentID];
            else if (portfolioDefault != null)
                return portfolioDefault;
            else
                return defaultInstruction;
        }
    }
}
