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
using System.Text;

using System.Data;
using AQI.AQILabs.Kernel.Factories;

using QuantApp.Kernel;

namespace AQI.AQILabs.Kernel.Adapters.SQL.Factories
{
    public class SQLPortfolioFactory : IPortfolioFactory
    {
        internal static string _portfolioTableName = "Portfolio";
        internal static string _positionTableName = "Position";
        internal static string _orderTableName = "Orders";
        internal static string _portfolioReservesTableName = "PortfolioReserves";
        private static string _processedCorporateActionsTableName = "ProcessedCorporateAction";

        private ConcurrentDictionary<int, Portfolio> _portfolioIdDB = new ConcurrentDictionary<int, Portfolio>();

        private ConcurrentDictionary<int, DataTable> _positionsNewTables = new ConcurrentDictionary<int, DataTable>();
        private ConcurrentDictionary<int, DataTable> _ordersNewTables = new ConcurrentDictionary<int, DataTable>();
        private ConcurrentDictionary<int, DataTable> _reservesTables = new ConcurrentDictionary<int, DataTable>();
        private ConcurrentDictionary<int, DataTable> _corporateActionsTables = new ConcurrentDictionary<int, DataTable>();

        private ConcurrentDictionary<int, DataTable> _mainTables = new ConcurrentDictionary<int, DataTable>();

        public readonly static object objLock = new object();

        private int GetInt(object obj)
        {
            if (obj is Int64)
                obj = (int)(object)Convert.ToInt32(obj);

            return (int)obj;
        }

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

        Dictionary<int, Dictionary<string, int>> _processedCorporateActions = new Dictionary<int, Dictionary<string, int>>();
        Dictionary<int, List<string>> _newProcessedCorporateActions = new Dictionary<int, List<string>>();
        public bool ProcessedCorporateAction(Portfolio portfolio, CorporateAction action)
        {
            return _processedCorporateActions.ContainsKey(portfolio.ID) && (_processedCorporateActions[portfolio.ID].ContainsKey(action.ID));
        }

        public void ProcessCorporateAction(Portfolio portfolio, CorporateAction action)
        {
            lock (objLock)
            {
                if (!ProcessedCorporateAction(portfolio, action))
                {
                    if (!_processedCorporateActions.ContainsKey(portfolio.ID))
                        _processedCorporateActions.Add(portfolio.ID, new Dictionary<string, int>());

                    if (!_newProcessedCorporateActions.ContainsKey(portfolio.ID))
                        _newProcessedCorporateActions.Add(portfolio.ID, new List<string>());

                    _processedCorporateActions[portfolio.ID].Add(action.ID, portfolio.ID);
                    _newProcessedCorporateActions[portfolio.ID].Add(action.ID);
                }
            }
        }


        public Portfolio CreatePortfolio(Instrument instrument, Instrument deposit, Instrument borrow, Portfolio parent)
        {
            lock (objLock)
            {
                if (instrument.InstrumentType != InstrumentType.Portfolio)
                    throw new Exception("Instrument is not a Portfolio");

                if (!instrument.SimulationObject)
                {
                    string searchString = "ID = " + instrument.ID;
                    string targetString = null;

                    DataTable table = Database.DB["Kernel"].GetDataTable(_portfolioTableName, targetString, searchString);
                    DataRowCollection rows = table.Rows;


                    if (rows.Count == 0)
                    {
                        DataRow r = table.NewRow();
                        r["ID"] = instrument.ID;
                        r["LongReserveID"] = -1;
                        r["ShortReserveID"] = -1;
                        r["ParentPortfolioID"] = (parent == null ? -1 : parent.ID);
                        r["StrategyID"] = -1;
                        r["ResidualID"] = -1;



                        rows.Add(r);
                        Database.DB["Kernel"].UpdateDataTable(table);

                        Portfolio p = FindPortfolio(instrument, false);
                        if (parent != null)
                            p.ParentPortfolio = parent;

                        p.AddReserve(instrument.Currency, deposit, borrow);

                        return p;
                    }
                    else
                        throw new Exception("Portfolio Already Exists");
                }
                else
                {
                    Portfolio p = FindPortfolio(instrument, false);
                    if (parent != null)
                        p.ParentPortfolio = parent;

                    p.AddReserve(instrument.Currency, deposit, borrow);
                    return p;
                }
            }
        }

        public void UpdatePortfolioDB(Portfolio instrument)
        {
            if (!_portfolioIdDB.ContainsKey(instrument.ID))
                _portfolioIdDB.TryAdd(instrument.ID, instrument);
            else
                _portfolioIdDB[instrument.ID] = instrument;
        }

        public Portfolio FindParentPortfolio(int id)
        {
            return Portfolio.FindPortfolio(Instrument.Factory.FindSecureInstrument(id));
        }

        public Strategy FindStrategy(int id)
        {
            return Strategy.FindStrategy(Instrument.Factory.FindSecureInstrument(id));
        }

        public Portfolio FindPortfolio(Instrument instrument)
        {
            return FindPortfolio(instrument, false);
        }

        public Portfolio FindPortfolio(Instrument instrument, Boolean loadPositionsInMemory)
        {
            lock (objLock)
            {
                if (_portfolioIdDB.ContainsKey(instrument.ID))
                    return _portfolioIdDB[instrument.ID];

                if (instrument.InstrumentType != InstrumentType.Portfolio)
                    throw new Exception("Instrument is not a Portfolio");

                Portfolio p = new Portfolio(instrument, loadPositionsInMemory);

                UpdatePortfolioDB(p);

                if (!instrument.SimulationObject)
                {
                    string tableName = _portfolioTableName;
                    string searchString = "ID = " + instrument.ID;
                    string targetString = null;

                    DataTable mainTable = Database.DB["Kernel"].GetDataTable(tableName, targetString, searchString);

                    if (mainTable.Rows.Count == 0)
                        return null;

                    DataRow row = mainTable.Rows[0];

                    int strategyID = GetValue<int>(row, "StrategyID");
                    int residualID = GetValue<int>(row, "ResidualID");
                    int parentPortfolioID = GetValue<int>(row, "ParentPortfolioID");
                    
                    string accountID = GetValue<string>(row, "AccountID");
                    string custodianID = GetValue<string>(row, "CustodianID");

                    string username = GetValue<string>(row, "Username");
                    string password = GetValue<string>(row, "Password");
                    string key = GetValue<string>(row, "KeyID");

                    p.StrategyID = strategyID;
                    p.ResidualID = residualID;
                    p.ParentPortfolioID = parentPortfolioID;
                    
                    p._accountID = accountID;
                    p._custodianID = custodianID;

                    p._username = username;
                    p._password = password;
                    p._key = key;

                    if (!_mainTables.ContainsKey(instrument.ID))
                        _mainTables.TryAdd(instrument.ID, mainTable);

                    tableName = _positionTableName;
                    searchString = null;
                    targetString = "TOP 1 *";

                    if(Database.DB[p.StrategyDB] is QuantApp.Kernel.Adapters.SQL.SQLiteDataSetAdapter || Database.DB[p.StrategyDB] is QuantApp.Kernel.Adapters.SQL.PostgresDataSetAdapter)
                    {
                        searchString = " LIMIT 1";
                        targetString = "*";
                    }

                    DataTable positionNewTable = Database.DB[p.StrategyDB].GetDataTable(tableName, targetString, searchString);
                    if (positionNewTable != null)
                        positionNewTable.Clear();

                    if (!_positionsNewTables.ContainsKey(instrument.ID))
                        _positionsNewTables.TryAdd(p.ID, positionNewTable);

                    tableName = _orderTableName;
                    searchString = null;
                    targetString = "TOP 1 *";

                    if(Database.DB[p.StrategyDB] is QuantApp.Kernel.Adapters.SQL.SQLiteDataSetAdapter || Database.DB[p.StrategyDB] is QuantApp.Kernel.Adapters.SQL.PostgresDataSetAdapter)
                    {
                        searchString = " LIMIT 1";
                        targetString = "*";
                    }

                    DataTable orderNewTable = Database.DB[p.StrategyDB].GetDataTable(tableName, targetString, searchString);
                    if (orderNewTable != null)
                        orderNewTable.Clear();

                    if (!_ordersNewTables.ContainsKey(instrument.ID))
                        _ordersNewTables.TryAdd(p.ID, orderNewTable);

                    tableName = _portfolioReservesTableName;
                    searchString = "ID = " + p.ID;
                    targetString = null;

                    DataTable reserveTable = Database.DB["Kernel"].GetDataTable(tableName, targetString, searchString);

                    if (!_reservesTables.ContainsKey(instrument.ID))
                        _reservesTables.TryAdd(p.ID, reserveTable);

                    tableName = _processedCorporateActionsTableName;
                    searchString = "PortfolioID = " + p.ID;
                    targetString = null;

                    DataTable corporateActionsNewTables = Database.DB[p.StrategyDB].GetDataTable(tableName, targetString, searchString);
                    if (_corporateActionsTables.ContainsKey(p.ID))
                        _corporateActionsTables[p.ID] = corporateActionsNewTables;
                    else
                        _corporateActionsTables.TryAdd(p.ID, corporateActionsNewTables);

                    LoadReserves(p);
                }


                return p;
            }
        }

        public List<Portfolio> FindPortfolio(string custodian, string accountid)
        {
            string tableName = _portfolioTableName;
            string searchString = "CustodianID LIKE '" + custodian + (accountid == null ? "'" : "' AND AccountID LIKE '" + accountid + "'");
            string targetString = null;

            DataTable mainTable = Database.DB["Kernel"].GetDataTable(tableName, targetString, searchString);

            List<Portfolio> res = new List<Portfolio>();
            
            foreach (DataRow row in mainTable.Rows)
            {
                int ID = GetValue<int>(row, "ID");

                int strategyID = GetValue<int>(row, "StrategyID");
                Instrument strat = Instrument.FindInstrument(strategyID);

                if (strat != null && !strat.Deleted)
                {
                    Portfolio p = Instrument.FindInstrument(ID) as Portfolio;

                    res.Add(p);
                }
            }
            if (res.Count > 0)
                return res;
            else
                return null;

        }

        public void UpdatePositionMemory(Position p, DateTime timestamp, Boolean addNew)
        {
            lock (objLock)
            {
                if (addNew)
                {
                    if (!p.Portfolio.SimulationObject)
                    {
                        if (!p.Aggregated || (p.Aggregated && !(p.Instrument.InstrumentType == AQI.AQILabs.Kernel.InstrumentType.Portfolio || (p.Instrument.InstrumentType == AQI.AQILabs.Kernel.InstrumentType.Strategy && (p.Instrument as Strategy).Portfolio != null))))
                        {
                            if (!_positionsNewTables.ContainsKey(p.Portfolio.ID))
                            {
                                if(Database.DB[p.Portfolio.StrategyDB] is QuantApp.Kernel.Adapters.SQL.SQLiteDataSetAdapter || Database.DB[p.Portfolio.StrategyDB] is QuantApp.Kernel.Adapters.SQL.PostgresDataSetAdapter)
                                    _positionsNewTables.TryAdd(p.Portfolio.ID, Database.DB[p.Portfolio.StrategyDB].GetDataTable(_positionTableName, "*", "ID = " + p.Portfolio.ID + " LIMIT 1"));
                                else
                                    _positionsNewTables.TryAdd(p.Portfolio.ID, Database.DB[p.Portfolio.StrategyDB].GetDataTable(_positionTableName, "TOP 1 *", "ID = " + p.Portfolio.ID));
                            }


                            DataRow r = _positionsNewTables[p.Portfolio.ID].NewRow();
                            r["ID"] = p.Portfolio.ID;
                            r["ConstituentID"] = p.Instrument.ID;
                            r["Timestamp"] = p.Timestamp;
                            r["Aggregated"] = p.Aggregated ? 1 : 0;
                            r["Unit"] = p.Unit;
                            r["Strike"] = p.Strike;
                            r["StrikeTimestamp"] = p.StrikeTimestamp;
                            r["InitialStrike"] = p.InitialStrike;
                            r["InitialStrikeTimestamp"] = p.Timestamp;

                            _positionsNewTables[p.Portfolio.ID].Rows.Add(r);


                            Database.DB[p.Portfolio.StrategyDB].UpdateDataTable(_positionsNewTables[p.Portfolio.ID]);
                        }
                    }
                }
                else
                {
                    if (!p.Portfolio.SimulationObject)
                    {
                        if (!p.Aggregated || (p.Aggregated && !(p.Instrument.InstrumentType == AQI.AQILabs.Kernel.InstrumentType.Portfolio || (p.Instrument.InstrumentType == AQI.AQILabs.Kernel.InstrumentType.Strategy && (p.Instrument as Strategy).Portfolio != null))))
                        {
                            string searchString = string.Format("ID={0} AND ConstituentID={1} AND Timestamp='{2:yyyy-MM-dd HH:mm:ss.fff}' AND Aggregated={3}", p.Portfolio.ID, p.Instrument.ID, p.Timestamp, p.Aggregated ? 1 : 0);
                            string targetString = "TOP 1 *";

                            if(Database.DB[p.Portfolio.StrategyDB] is QuantApp.Kernel.Adapters.SQL.SQLiteDataSetAdapter || Database.DB[p.Portfolio.StrategyDB] is QuantApp.Kernel.Adapters.SQL.PostgresDataSetAdapter)
                            {
                                searchString += " LIMIT 1";
                                targetString = "*";
                            }


                            DataTable table = Database.DB[p.Portfolio.StrategyDB].GetDataTable(_positionTableName, targetString, searchString);

                            List<DataRow> rows = new List<DataRow>();
                            if (table != null && table.Rows.Count > 0)
                            {
                                foreach (DataRow rd in table.Rows)
                                    rows.Add(rd);

                                foreach (DataRow rd in rows)
                                    table.Rows.Remove(rd);

                                DataRow r = table.NewRow();
                                r["ID"] = p.Portfolio.ID;
                                r["ConstituentID"] = p.Instrument.ID;
                                r["Timestamp"] = p.Timestamp;
                                r["Aggregated"] = false ? 1 : 0;
                                r["Unit"] = p.Unit;
                                r["Strike"] = p.Strike;
                                r["StrikeTimestamp"] = p.StrikeTimestamp;
                                r["InitialStrike"] = p.InitialStrike;
                                r["InitialStrikeTimestamp"] = p.InitialStrikeTimestamp;

                                table.Rows.Add(r);

                                Database.DB[p.Portfolio.StrategyDB].UpdateDataTable(table);
                            }
                        }
                    }

                }
            }
        }

        public void AddNewPosition(Position p)
        {
            lock (objLock)
            {
                if (!_positionsNewTables.ContainsKey(p.Portfolio.ID))
                {
                    if(Database.DB[p.Portfolio.StrategyDB] is QuantApp.Kernel.Adapters.SQL.SQLiteDataSetAdapter || Database.DB[p.Portfolio.StrategyDB] is QuantApp.Kernel.Adapters.SQL.PostgresDataSetAdapter)
                        _positionsNewTables.TryAdd(p.Portfolio.ID, Database.DB[p.Portfolio.StrategyDB].GetDataTable(_positionTableName, "*", "ID = " + p.Portfolio.ID + " LIMIT 1"));
                    else
                        _positionsNewTables.TryAdd(p.Portfolio.ID, Database.DB[p.Portfolio.StrategyDB].GetDataTable(_positionTableName, "TOP 1 *", "ID = " + p.Portfolio.ID));
                }

                DataRow r = _positionsNewTables[p.Portfolio.ID].NewRow();
                r["ID"] = p.Portfolio.ID;
                r["ConstituentID"] = p.Instrument.ID;
                r["Timestamp"] = p.Timestamp;
                r["Aggregated"] = p.Aggregated ? 1 : 0;
                r["Unit"] = p.Unit;
                r["Strike"] = p.Strike;
                r["StrikeTimestamp"] = p.StrikeTimestamp;
                r["InitialStrike"] = p.InitialStrike;
                r["InitialStrikeTimestamp"] = p.Timestamp;

                _positionsNewTables[p.Portfolio.ID].Rows.Add(r);
            }
        }
        public void AddNewOrder(Order o)
        {
            lock (objLock)
            {
                if (o.Unit == 0)
                    return;

                if (o.Portfolio.Cloud)
                    RTDEngine.Send(new RTDMessage() { Type = RTDMessage.MessageType.AddNewOrder, Content = new RTDMessage.OrderMessage() { Order = o, OnlyMemory = true } });
                
                UpdateOrder(o);
                return;

                DataRow r = _ordersNewTables[o.Portfolio.ID].NewRow();
                r["ID"] = o.ID;
                r["PortfolioID"] = o.Portfolio.ID;
                r["ConstituentID"] = o.Instrument.ID;
                r["OrderDate"] = o.OrderDate;
                r["ExecutionDate"] = o.ExecutionDate;
                r["OrderType"] = o.Type;
                if(Database.DB[o.Portfolio.StrategyDB] is QuantApp.Kernel.Adapters.SQL.SQLiteDataSetAdapter || Database.DB[o.Portfolio.StrategyDB] is QuantApp.Kernel.Adapters.SQL.PostgresDataSetAdapter)
                    r["Limits"] = o.Limit;
                else
                    r["Limit"] = o.Limit;
                r["Unit"] = o.Unit;
                r["Status"] = o.Status;
                r["ExecutionLevel"] = double.IsNaN(o.ExecutionLevel) ? 0.0 : o.ExecutionLevel;
                r["Aggregated"] = o.Aggregated ? 1 : 0;

                r["Client"] = o.Client;
                r["Destination"] = o.Destination;
                r["Account"] = o.Account;

                _ordersNewTables[o.Portfolio.ID].Rows.Add(r);
            }
        }

        System.Collections.Concurrent.ConcurrentDictionary<int, System.Collections.Concurrent.ConcurrentQueue<Portfolio.PositionMessage>> _newPositionQueue = new System.Collections.Concurrent.ConcurrentDictionary<int, System.Collections.Concurrent.ConcurrentQueue<Portfolio.PositionMessage>>();
        public void AddNewPositionMessage(Portfolio.PositionMessage p)
        {
            if (!_newPositionQueue.ContainsKey(p.Position.PortfolioID))
                _newPositionQueue.TryAdd(p.Position.PortfolioID, new System.Collections.Concurrent.ConcurrentQueue<Portfolio.PositionMessage>());

            _newPositionQueue[p.Position.PortfolioID].Enqueue(p);
        }

        public void SaveNewPositions(Portfolio portfolio)
        {
            lock (objLock)
            {
                if (portfolio.SimulationObject)
                    return;

                if (!_positionsNewTables.ContainsKey(portfolio.ID))
                {
                    if(Database.DB[portfolio.StrategyDB] is QuantApp.Kernel.Adapters.SQL.SQLiteDataSetAdapter || Database.DB[portfolio.StrategyDB] is QuantApp.Kernel.Adapters.SQL.PostgresDataSetAdapter)
                        _positionsNewTables.TryAdd(portfolio.ID, Database.DB[portfolio.StrategyDB].GetDataTable(_positionTableName, "*", "ID = " + portfolio.ID + " LIMIT 1"));
                    else
                        _positionsNewTables.TryAdd(portfolio.ID, Database.DB[portfolio.StrategyDB].GetDataTable(_positionTableName, "TOP 1 *", "ID = " + portfolio.ID));
                }

                if (!_corporateActionsTables.ContainsKey(portfolio.ID))
                    _corporateActionsTables.TryAdd(portfolio.ID, Database.DB[portfolio.StrategyDB].GetDataTable(_processedCorporateActionsTableName, null, "PortfolioID = " + portfolio.ID));

                if (!_ordersNewTables.ContainsKey(portfolio.ID))
                {
                    if(Database.DB[portfolio.StrategyDB] is QuantApp.Kernel.Adapters.SQL.SQLiteDataSetAdapter || Database.DB[portfolio.StrategyDB] is QuantApp.Kernel.Adapters.SQL.PostgresDataSetAdapter)
                        _ordersNewTables.TryAdd(portfolio.ID, Database.DB[portfolio.StrategyDB].GetDataTable(_orderTableName, "*", "PortfolioID = " + portfolio.ID + " LIMIT 1"));
                    else
                        _ordersNewTables.TryAdd(portfolio.ID, Database.DB[portfolio.StrategyDB].GetDataTable(_orderTableName, "TOP 1 *", "PortfolioID = " + portfolio.ID));
                }


                Database.DB[portfolio.StrategyDB].UpdateDataTable(_positionsNewTables[portfolio.ID]);
                Database.DB[portfolio.StrategyDB].UpdateDataTable(_ordersNewTables[portfolio.ID]);

                if (_newProcessedCorporateActions.ContainsKey(portfolio.ID))
                {
                    foreach (string id in _newProcessedCorporateActions[portfolio.ID].ToArray())
                    {
                        DataRow row = _corporateActionsTables[portfolio.ID].NewRow();
                        row["ID"] = id;
                        row["PortfolioID"] = portfolio.ID;
                        _corporateActionsTables[portfolio.ID].Rows.Add(row);
                    }

                    _newProcessedCorporateActions[portfolio.ID] = new List<string>();

                    Database.DB[portfolio.StrategyDB].UpdateDataTable(_corporateActionsTables[portfolio.ID]);
                    _corporateActionsTables[portfolio.ID] = Database.DB[portfolio.StrategyDB].GetDataTable(_processedCorporateActionsTableName, "PortfolioID = " + portfolio.ID, null);

                }

                if (_newOrders.ContainsKey(portfolio.ID))
                {
                    StringBuilder command_builder = new StringBuilder(1000000);
                    int counter = 0;
                    foreach (string id in _newOrders[portfolio.ID].Keys.ToArray())
                    {
                        counter++;
                        Order o = _newOrders[portfolio.ID][id];

                        string line = "DELETE FROM " + _orderTableName + " WHERE ID = '" + o.ID + "' AND PortfolioID = " + o.PortfolioID + ";"
                                + "INSERT INTO " + _orderTableName + " (ID, PortfolioID, ConstituentID, OrderDate, Unit, Aggregated, OrderType, Limit, Status, ExecutionLevel, ExecutionDate, Client, Destination, Account) "
                                + " VALUES ('" + o.ID + "',"
                                + o.PortfolioID + ","
                                + o.InstrumentID + ","
                                + string.Format("'{0:yyyy-MM-dd HH:mm:ss.fff}'", (o.OrderDate > new DateTime(9999, 12, 31, 23, 59, 59, 997) ? new DateTime(9999, 12, 31, 23, 59, 59, 997) : o.OrderDate)) + ","
                                + (double.IsNaN(o.Unit) || double.IsInfinity(o.Unit) ? 0.0 : o.Unit) + ","
                                + (o.Aggregated ? 1 : 0) + ","
                                + (int)o.Type + ","
                                + (double.IsNaN(o.Limit) || double.IsInfinity(o.Limit) ? 0.0 : o.Limit) + ","
                                + (int)o.Status + ","
                                + (double.IsNaN(o.ExecutionLevel) || double.IsInfinity(o.ExecutionLevel) ? 0.0 : o.ExecutionLevel) + ","
                                + string.Format("'{0:yyyy-MM-dd HH:mm:ss.fff}'", (o.ExecutionDate > new DateTime(9999, 12, 31, 23, 59, 59, 997) ? new DateTime(9999, 12, 31, 23, 59, 59, 997) : o.ExecutionDate)) + ","
                                + "'" + o.Client + "',"
                                + "'" + o.Destination + "',"
                                + "'" + o.Account + "'"
                                + ");";
                        
                        if(Database.DB[portfolio.StrategyDB] is QuantApp.Kernel.Adapters.SQL.SQLiteDataSetAdapter || Database.DB[portfolio.StrategyDB] is QuantApp.Kernel.Adapters.SQL.PostgresDataSetAdapter)
                            line = "DELETE FROM " + _orderTableName + " WHERE ID = '" + o.ID + "' AND PortfolioID = " + o.PortfolioID + ";"
                                    + "INSERT INTO " + _orderTableName + " (ID, PortfolioID, ConstituentID, OrderDate, Unit, Aggregated, OrderType, Limits, Status, ExecutionLevel, ExecutionDate, Client, Destination, Account) "
                                    + " VALUES ('" + o.ID + "',"
                                    + o.PortfolioID + ","
                                    + o.InstrumentID + ","
                                    + string.Format("'{0:yyyy-MM-dd HH:mm:ss.fff}'", (o.OrderDate > new DateTime(9999, 12, 31, 23, 59, 59, 997) ? new DateTime(9999, 12, 31, 23, 59, 59, 997) : o.OrderDate)) + ","
                                    + (double.IsNaN(o.Unit) || double.IsInfinity(o.Unit) ? 0.0 : o.Unit) + ","
                                    + (o.Aggregated ? 1 : 0) + ","
                                    + (int)o.Type + ","
                                    + (double.IsNaN(o.Limit) || double.IsInfinity(o.Limit) ? 0.0 : o.Limit) + ","
                                    + (int)o.Status + ","
                                    + (double.IsNaN(o.ExecutionLevel) || double.IsInfinity(o.ExecutionLevel) ? 0.0 : o.ExecutionLevel) + ","
                                    + string.Format("'{0:yyyy-MM-dd HH:mm:ss.fff}'", (o.ExecutionDate > new DateTime(9999, 12, 31, 23, 59, 59, 997) ? new DateTime(9999, 12, 31, 23, 59, 59, 997) : o.ExecutionDate)) + ","
                                    + "'" + o.Client + "',"
                                    + "'" + o.Destination + "',"
                                    + "'" + o.Account + "'"
                                    + ");";



                        command_builder.Append(line);

                        if (counter == 100000)
                        {
                            Console.WriteLine("Writing to orders");
                            Database.DB[portfolio.StrategyDB].ExecuteCommand(command_builder.ToString());
                            command_builder = new StringBuilder(1000000);
                            counter = 0;
                        }

                        o = null;
                        _newOrders[portfolio.ID].TryRemove(id, out o);
                    }

                    if (command_builder.Length > 0)
                    {
                        Console.WriteLine("Writing LAST to orders");
                        Database.DB[portfolio.StrategyDB].ExecuteCommand(command_builder.ToString());
                    }
                }

                if (_newPositionQueue.ContainsKey(portfolio.ID))
                {
                    Portfolio.PositionMessage p;

                    StringBuilder command_builder = new StringBuilder(1000000);
                    int counter = 0;
                    while (_newPositionQueue[portfolio.ID].TryDequeue(out p))
                    {
                        counter++;
                        
                        if (p.Command == 1)
                        {
                            command_builder.Append("INSERT INTO " + _positionTableName + " (ID, ConstituentID, Unit, Strike, InitialStrike, Timestamp, StrikeTimestamp, InitialStrikeTimestamp, Aggregated) "
                            + " VALUES (" + p.Position.PortfolioID + ","
                            + p.Position.InstrumentID + ","
                            + (double.IsNaN(p.Position.Unit) || double.IsInfinity(p.Position.Unit) ? 0 : p.Position.Unit) + ","
                            + p.Position.Strike + ","
                            + p.Position.InitialStrike + ","
                            + string.Format("'{0:yyyy-MM-dd HH:mm:ss.fff}'", p.Position.Timestamp) + ","
                            + string.Format("'{0:yyyy-MM-dd HH:mm:ss.fff}'", p.Position.StrikeTimestamp) + ","
                            + string.Format("'{0:yyyy-MM-dd HH:mm:ss.fff}'", p.Position.InitialStrikeTimestamp) + ","
                            + 0
                            + ");");

                        }
                        else
                        {
                            command_builder.Append("DELETE FROM " + _positionTableName + " WHERE "
                            + " ID = " + p.Position.PortfolioID
                            + " AND ConstituentID = " + p.Position.InstrumentID
                            + " AND Unit = " + (double.IsNaN(p.Position.Unit) || double.IsInfinity(p.Position.Unit) ? 0 : p.Position.Unit)
                            + " AND Strike = " + p.Position.Strike
                            + " AND InitialStrike = " + p.Position.InitialStrike
                            + " AND Timestamp = " + string.Format("'{0:yyyy-MM-dd HH:mm:ss.fff}'", p.Position.Timestamp)
                            + " AND StrikeTimestamp = " + string.Format("'{0:yyyy-MM-dd HH:mm:ss.fff}'", p.Position.StrikeTimestamp)
                            + " AND InitialStrikeTimestamp = " + string.Format("'{0:yyyy-MM-dd HH:mm:ss.fff}'", p.Position.InitialStrikeTimestamp)
                            + ";");
                        }

                        if (counter == 100000)
                        {
                            Console.WriteLine("Writing to positions");
                            Database.DB[portfolio.StrategyDB].ExecuteCommand(command_builder.ToString());
                            command_builder = new StringBuilder(1000000);
                            counter = 0;
                        }
                    }

                    if (command_builder.Length > 0)
                    {
                        Console.WriteLine("Writing LAST to positions");
                        Database.DB[portfolio.StrategyDB].ExecuteCommand(command_builder.ToString());
                    }
                }

                if(Database.DB[portfolio.StrategyDB] is QuantApp.Kernel.Adapters.SQL.SQLiteDataSetAdapter || Database.DB[portfolio.StrategyDB] is QuantApp.Kernel.Adapters.SQL.PostgresDataSetAdapter)
                {
                    _positionsNewTables[portfolio.ID] = Database.DB[portfolio.StrategyDB].GetDataTable(_positionTableName, "*", "ID = -10000 LIMIT 1");// + portfolio.ID);;
                    _ordersNewTables[portfolio.ID] = Database.DB[portfolio.StrategyDB].GetDataTable(_orderTableName, "*", "PortfolioID = -1000 LIMIT 1");// + portfolio.ID);;
                }
                else
                {
                    _positionsNewTables[portfolio.ID] = Database.DB[portfolio.StrategyDB].GetDataTable(_positionTableName, "TOP 1 *", "ID = -10000");// + portfolio.ID);;
                    _ordersNewTables[portfolio.ID] = Database.DB[portfolio.StrategyDB].GetDataTable(_orderTableName, "TOP 1 *", "PortfolioID = -1000");// + portfolio.ID);;
                }


                
            }
        }

        System.Collections.Concurrent.ConcurrentDictionary<int, System.Collections.Concurrent.ConcurrentDictionary<string, Order>> _newOrders = new System.Collections.Concurrent.ConcurrentDictionary<int, System.Collections.Concurrent.ConcurrentDictionary<string, Order>>();
        public void UpdateOrder(Order order)
        {
            if (order.Unit == 0)
                return;

            if (!order.Portfolio.SimulationObject)
            {
                string key = string.Format("{0}_{1}_{2}", order.ID, order.PortfolioID, order.Aggregated ? 1 : 0);

                if (!_newOrders.ContainsKey(order.PortfolioID))
                    _newOrders.TryAdd(order.PortfolioID, new System.Collections.Concurrent.ConcurrentDictionary<string, Order>());

                if (_newOrders[order.PortfolioID].ContainsKey(key))
                    _newOrders[order.PortfolioID][key] = order;
                else
                    _newOrders[order.PortfolioID].TryAdd(key, order);

                return;





                string searchString = string.Format("ID='{0}' AND PortfolioID='{1}' AND Aggregated={2}", order.ID, order.PortfolioID, order.Aggregated ? 1 : 0);
                string targetString = "TOP 1 *";

                if(Database.DB[order.Portfolio.StrategyDB] is QuantApp.Kernel.Adapters.SQL.SQLiteDataSetAdapter || Database.DB[order.Portfolio.StrategyDB] is QuantApp.Kernel.Adapters.SQL.PostgresDataSetAdapter)
                {
                    searchString += " LIMIT 1";
                    targetString = "*";
                }


                DataTable orderTable = Database.DB[order.Portfolio.StrategyDB].GetDataTable(_orderTableName, targetString, searchString);

                DataRowCollection rows = orderTable.Rows;

                if (rows.Count > 0)
                {
                    try
                    {
                        DataRow row = rows[0];
                        if (order.Unit != 0)
                        {
                            row["Status"] = order.Status;
                            row["ExecutionDate"] = order.ExecutionDate;
                            row["Unit"] = order.Unit;
                            row["ExecutionLevel"] = double.IsNaN(order.ExecutionLevel) ? 0.0 : order.ExecutionLevel;

                            rows[0]["Client"] = order.Client;
                            rows[0]["Destination"] = order.Destination;
                            rows[0]["Account"] = order.Account;


                            Database.DB[order.Portfolio.StrategyDB].UpdateDataTable(orderTable);//_ordersTables[order.Portfolio.ID]);
                        }
                        else
                        {
                            Database.DB[order.Portfolio.StrategyDB].ExecuteCommand("DELETE FROM " + _orderTableName + " WHERE " + searchString);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
            }
        }

        public DateTime LastOrderTimestamp(Portfolio portfolio, DateTime date)
        {
            string searchString = (date.Date == DateTime.MinValue ? string.Format("PortfolioID={0}", portfolio.ID) : string.Format("PortfolioID={0} AND OrderDate<='{1:yyyy-MM-dd HH:mm:ss.fff}'", portfolio.ID, Calendar.Close(date))) + " ORDER BY OrderDate DESC";
            string targetString = "TOP 1 *";

            if(Database.DB[portfolio.StrategyDB] is QuantApp.Kernel.Adapters.SQL.SQLiteDataSetAdapter || Database.DB[portfolio.StrategyDB] is QuantApp.Kernel.Adapters.SQL.PostgresDataSetAdapter)
            {
                searchString += " LIMIT 1";
                targetString = "*";
            }


            DataTable table = Database.DB[portfolio.StrategyDB].GetDataTable(_orderTableName, targetString, searchString);
            DataRowCollection rows = table.Rows;

            if (rows.Count != 0)
                return (DateTime)rows[0]["OrderDate"];

            return DateTime.MinValue;
        }

        public DateTime LastPositionTimestamp(Portfolio portfolio, DateTime date)
        {
            if (!portfolio.SimulationObject)
            {
                string searchString = (date.Date == DateTime.MinValue ? string.Format("ID={0}", portfolio.ID) : string.Format("ID={0} AND Timestamp<='{1:yyyy-MM-dd HH:mm:ss.fff}'", portfolio.ID, Calendar.Close(date))) + " ORDER BY Timestamp DESC";
                string targetString = "TOP 1 *";

                if(Database.DB[portfolio.StrategyDB] is QuantApp.Kernel.Adapters.SQL.SQLiteDataSetAdapter || Database.DB[portfolio.StrategyDB] is QuantApp.Kernel.Adapters.SQL.PostgresDataSetAdapter)
                {
                    searchString += " LIMIT 1";
                    targetString = "*";
                }

                DataTable table = Database.DB[portfolio.StrategyDB].GetDataTable(_positionTableName, targetString, searchString);
                DataRowCollection rows = table.Rows;

                if (rows.Count != 0)
                    return (DateTime)rows[0]["Timestamp"];
            }

            return DateTime.MinValue;
        }

        public DateTime FirstPositionTimestamp(Portfolio portfolio, DateTime date)
        {
            if (!portfolio.SimulationObject)
            {
                string searchString = (date == DateTime.MinValue ? string.Format("ID={0}", portfolio.ID) : string.Format("ID={0} AND Timestamp<='{1:yyyy-MM-dd HH:mm:ss.fff}'", portfolio.ID, Calendar.Close(date))) + " ORDER BY Timestamp";
                string targetString = "TOP 1 *";

                if(Database.DB[portfolio.StrategyDB] is QuantApp.Kernel.Adapters.SQL.SQLiteDataSetAdapter || Database.DB[portfolio.StrategyDB] is QuantApp.Kernel.Adapters.SQL.PostgresDataSetAdapter)
                {
                    searchString += " LIMIT 1";
                    targetString = "*";
                }

                DataTable table = Database.DB[portfolio.StrategyDB].GetDataTable(_positionTableName, targetString, searchString);
                DataRowCollection rows = table.Rows;

                if (rows.Count != 0)
                    return (DateTime)rows[0]["Timestamp"];
            }

            return DateTime.MinValue;
        }

        public List<Order> NonExecutedOrders(Portfolio portfolio, DateTime timestamp, TimeSeriesType ttype, Boolean aggregated)
        {
            DateTime lastTime = DateTime.MinValue;

            if (timestamp.Date != DateTime.MinValue)
                lastTime = LastOrderTimestamp(portfolio, timestamp);

            if (lastTime.Date == DateTime.MinValue)
                lastTime = new DateTime(1900, 01, 01);


            List<Order> orders = new List<Order>();

            string searchString = timestamp.Date == DateTime.MinValue ? string.Format("PortfolioID={0} AND Aggregated={1}", portfolio.ID, aggregated ? 1 : 0) : string.Format("PortfolioID={0} AND OrderDate<='{1:yyyy-MM-dd HH:mm:ss.fff}' AND Aggregated={2} AND OrderDate>='{3:yyyy-MM-dd HH:mm:ss.fff}'", portfolio.ID, timestamp, aggregated ? 1 : 0, lastTime);
            string targetString = null;

            DataTable table = Database.DB[portfolio.StrategyDB].GetDataTable(_orderTableName, targetString, searchString);

            DataRowCollection rows = table.Rows;

            if (rows.Count == 0)
                return orders;

            foreach (DataRow row in rows)
            {
                Order o = new Order((string)row["ID"], portfolio.ID, GetInt(row["ConstituentID"]), (double)row["Unit"], (DateTime)row["OrderDate"], (DateTime)row["ExecutionDate"], (OrderType)row["OrderType"], (double)row["Limit"], (OrderStatus)row["Status"], (double)row["ExecutionLevel"], (Boolean)row["Aggregated"], GetValue<string>(row, "Client"), GetValue<string>(row, "Destination"), GetValue<string>(row, "Account"));
                if(Database.DB[portfolio.StrategyDB] is QuantApp.Kernel.Adapters.SQL.SQLiteDataSetAdapter || Database.DB[portfolio.StrategyDB] is QuantApp.Kernel.Adapters.SQL.PostgresDataSetAdapter)
                    o = new Order((string)row["ID"], portfolio.ID, GetInt(row["ConstituentID"]), (double)row["Unit"], (DateTime)row["OrderDate"], (DateTime)row["ExecutionDate"], (OrderType)row["OrderType"], (double)row["Limits"], (OrderStatus)row["Status"], (double)row["ExecutionLevel"], (Boolean)row["Aggregated"], GetValue<string>(row, "Client"), GetValue<string>(row, "Destination"), GetValue<string>(row, "Account"));

                if ((o.Status == OrderStatus.NotExecuted && o.ExecutionDate == timestamp))
                    orders.Add(o);
            }
            if (orders.Count == 0)
                return null;
            else
                return orders;
        }

        public List<Order> OpenOrders(Portfolio portfolio, DateTime timestamp, TimeSeriesType ttype, Boolean aggregated)
        {
            List<Order> orders = new List<Order>();
            if (portfolio.SimulationObject)
                return orders;

            DateTime lastTime = DateTime.MinValue;

            if (timestamp.Date == DateTime.MinValue)
                timestamp = new DateTime(1900, 01, 01);
            else
                lastTime = LastOrderTimestamp(portfolio, timestamp);


            if (lastTime.Date == DateTime.MinValue)
                lastTime = new DateTime(1900, 01, 01);


            string searchString = timestamp.Date == DateTime.MinValue ? string.Format("PortfolioID={0} AND Aggregated={1} ORDER BY OrderDate Desc", portfolio.ID, aggregated ? 1 : 0) : string.Format("PortfolioID={0} AND OrderDate<='{1:yyyy-MM-dd HH:mm:ss.fff}' AND Aggregated={2} AND OrderDate>='{3:yyyy-MM-dd HH:mm:ss.fff}' ORDER BY OrderDate Desc", portfolio.ID, timestamp, aggregated ? 1 : 0, lastTime);
            string targetString = null;

            DataTable table = Database.DB[portfolio.StrategyDB].GetDataTable(_orderTableName, targetString, searchString);

            DataRowCollection rows = table.Rows;

            if (rows.Count == 0)
                return orders;

            foreach (DataRow row in rows)
            {
                Order o = new Order((string)row["ID"], portfolio.ID, GetInt(row["ConstituentID"]), (double)row["Unit"], (DateTime)row["OrderDate"], (DateTime)row["ExecutionDate"], (OrderType)row["OrderType"], (double)row["Limit"], (OrderStatus)row["Status"], (double)row["ExecutionLevel"], (Boolean)row["Aggregated"], GetValue<string>(row, "Client"), GetValue<string>(row, "Destination"), GetValue<string>(row, "Account"));
                if(Database.DB[portfolio.StrategyDB] is QuantApp.Kernel.Adapters.SQL.SQLiteDataSetAdapter || Database.DB[portfolio.StrategyDB] is QuantApp.Kernel.Adapters.SQL.PostgresDataSetAdapter)
                    o = new Order((string)row["ID"], portfolio.ID, GetInt(row["ConstituentID"]), (double)row["Unit"], (DateTime)row["OrderDate"], (DateTime)row["ExecutionDate"], (OrderType)row["OrderType"], (double)row["Limits"], (OrderStatus)row["Status"], (double)row["ExecutionLevel"], (Boolean)row["Aggregated"], GetValue<string>(row, "Client"), GetValue<string>(row, "Destination"), GetValue<string>(row, "Account"));

                if (o.Status != OrderStatus.Booked && o.Status != OrderStatus.NotExecuted)
                    orders.Add(o);
            }
            if (orders.Count == 0)
                return null;
            else
                return orders;
        }

        public void LoadReserves(Portfolio portfolio)
        {
            if (!portfolio.SimulationObject)
            {
                if (!_reservesTables.ContainsKey(portfolio.ID))
                {
                    DataTable reserveTable = Database.DB["Kernel"].GetDataTable(_portfolioReservesTableName, null, "ID = " + portfolio.ID);
                    _reservesTables.TryAdd(portfolio.ID, reserveTable);
                }


                foreach (DataRow row in _reservesTables[portfolio.ID].Rows)
                {
                    int ccy_id = GetValue<int>(row, "CurrencyID");
                    int long_id = GetValue<int>(row, "LongReserveID");
                    int short_id = GetValue<int>(row, "ShortReserveID");

                    portfolio.AddReserveMemory(Currency.FindCurrency(ccy_id), Instrument.FindInstrument(long_id), Instrument.FindInstrument(short_id));
                }

                if (!_processedCorporateActions.ContainsKey(portfolio.ID))
                    _processedCorporateActions.Add(portfolio.ID, new Dictionary<string, int>());

                if (!_corporateActionsTables.ContainsKey(portfolio.ID))
                    _corporateActionsTables.TryAdd(portfolio.ID, Database.DB[portfolio.StrategyDB].GetDataTable(_processedCorporateActionsTableName, null, "PortfolioID = " + portfolio.ID));


                foreach (DataRow r in _corporateActionsTables[portfolio.ID].Rows)
                    _processedCorporateActions[portfolio.ID].Add((string)r["ID"], portfolio.ID);
            }
        }

        public void AddReserve(Portfolio portfolio, Currency ccy, Instrument longInstrument, Instrument shortInstrument)
        {
            lock (objLock)
            {
                if (!_reservesTables.ContainsKey(portfolio.ID))
                {
                    DataTable reserveTable = Database.DB["Kernel"].GetDataTable(_portfolioReservesTableName, null, "ID = " + portfolio.ID);
                    _reservesTables.TryAdd(portfolio.ID, reserveTable);
                }

                DataRowCollection rows = _reservesTables[portfolio.ID].Rows;
                DataRow[] rowArray = rows.Cast<DataRow>().ToArray();



                if (rows.Count == 0)
                {
                    DataRow r = _reservesTables[portfolio.ID].NewRow();
                    r["ID"] = portfolio.ID;
                    r["CurrencyID"] = ccy.ID;
                    r["LongReserveID"] = (longInstrument == null ? -1 : longInstrument.ID);
                    r["shortReserveID"] = (shortInstrument == null ? -1 : shortInstrument.ID);
                    _reservesTables[portfolio.ID].Rows.Add(r);

                    Database.DB["Kernel"].UpdateDataTable(_reservesTables[portfolio.ID]);
                }
                else
                {
                    bool changed = false;
                    foreach (DataRow row in rowArray)
                        if (!changed)
                            if (GetValue<int>(row, "CurrencyID") == ccy.ID)
                            {
                                row["LongReserveID"] = longInstrument.ID;
                                row["ShortReserveID"] = shortInstrument.ID;
                                changed = true;
                            }
                            else
                            {
                                DataRow r = _reservesTables[portfolio.ID].NewRow();
                                r["ID"] = portfolio.ID;
                                r["CurrencyID"] = ccy.ID;
                                r["LongReserveID"] = (longInstrument == null ? -1 : longInstrument.ID);
                                r["shortReserveID"] = (shortInstrument == null ? -1 : shortInstrument.ID);
                                _reservesTables[portfolio.ID].Rows.Add(r);

                                changed = true;
                            }
                    if (changed)
                        Database.DB["Kernel"].UpdateDataTable(_reservesTables[portfolio.ID]);
                }
            }
        }

        public List<Position> LoadPositions(Portfolio portfolio, DateTime date)
        {
            List<Position> positions = new List<Position>();

            string tableName = _positionTableName;

            string searchTimeString = "FORMAT((SELECT TOP 1 Timestamp FROM " + tableName + " WHERE " + string.Format("ID={0} AND Timestamp<='{1:yyyy-MM-dd HH:mm:ss.fff}'", portfolio.ID, Calendar.Close(date)) + " ORDER BY Timestamp DESC), 'yyyy-MM-dd')";
            string searchString = (date.Date == DateTime.MinValue ? "" : "DECLARE @DATE VARCHAR(MAX); SET @DATE=" + searchTimeString + ";") + "SELECT * FROM " + tableName + " WHERE ID = " + portfolio.ID + (date.Date == DateTime.MinValue ? "" : " AND " + string.Format("Timestamp>=@DATE + ' 00:00:00.000'") + " AND " + string.Format("Timestamp<=@DATE + ' 23:59:59.999'")) + "  ORDER BY Timestamp" + (date.Date == DateTime.MinValue ? "" : " DESC");

            if(Database.DB[portfolio.StrategyDB] is QuantApp.Kernel.Adapters.SQL.SQLiteDataSetAdapter || Database.DB[portfolio.StrategyDB] is QuantApp.Kernel.Adapters.SQL.PostgresDataSetAdapter)
            {
                searchTimeString = "FORMAT((SELECT Timestamp FROM " + tableName + " WHERE " + string.Format("ID={0} AND Timestamp<='{1:yyyy-MM-dd HH:mm:ss.fff}'", portfolio.ID, Calendar.Close(date)) + " ORDER BY Timestamp DESC LIMIT 1), 'yyyy-MM-dd')";
                searchString = (date.Date == DateTime.MinValue ? "" : "DECLARE @DATE VARCHAR(MAX); SET @DATE=" + searchTimeString + ";") + "SELECT * FROM " + tableName + " WHERE ID = " + portfolio.ID + (date.Date == DateTime.MinValue ? "" : " AND " + string.Format("Timestamp>=@DATE + ' 00:00:00.000'") + " AND " + string.Format("Timestamp<=@DATE + ' 23:59:59.999'")) + "  ORDER BY Timestamp" + (date.Date == DateTime.MinValue ? "" : " DESC");
            }
            DataTable positionTable = Database.DB[portfolio.StrategyDB].ExecuteDataTable(tableName, searchString);

            DataRowCollection rows = positionTable.Rows;
            
            foreach (DataRow r in rows)
            {
                Position p = new Position(portfolio.ID, GetInt(r["ConstituentID"]), (double)r["Unit"], (DateTime)r["Timestamp"], (double)r["Strike"], (DateTime)r["InitialStrikeTimestamp"], (double)r["InitialStrike"], (DateTime)r["StrikeTimestamp"], (Boolean)r["Aggregated"]);
                positions.Add(p);
            }
            return positions;
        }

        public List<Order> LoadOrders(Portfolio portfolio, DateTime date)
        {
            List<Order> orders = new List<Order>();

            string tableName = _orderTableName;

            string searchTimeString = "FORMAT((SELECT TOP 1 OrderDate FROM " + tableName + " WHERE " + string.Format("PortfolioID={0} AND OrderDate<='{1:yyyy-MM-dd HH:mm:ss.fff}'", portfolio.ID, Calendar.Close(date)) + " ORDER BY OrderDate DESC), 'yyyy-MM-dd')";
            string searchString = (date.Date == DateTime.MinValue ? "" : "DECLARE @DATE VARCHAR(MAX); SET @DATE=" + searchTimeString + ";") + "SELECT * FROM " + tableName + " WHERE PortfolioID = " + portfolio.ID + (date.Date == DateTime.MinValue ? "" : " AND " + string.Format("OrderDate>=@DATE + ' 00:00:00.000'") + " AND " + string.Format("OrderDate<=@DATE + ' 23:59:59.999'")) + "  ORDER BY OrderDate";

            if(Database.DB[portfolio.StrategyDB] is QuantApp.Kernel.Adapters.SQL.SQLiteDataSetAdapter || Database.DB[portfolio.StrategyDB] is QuantApp.Kernel.Adapters.SQL.PostgresDataSetAdapter)
            {
                searchTimeString = "FORMAT((SELECT OrderDate FROM " + tableName + " WHERE " + string.Format("PortfolioID={0} AND OrderDate<='{1:yyyy-MM-dd HH:mm:ss.fff}'", portfolio.ID, Calendar.Close(date)) + " ORDER BY OrderDate DESC LIMIT 1), 'yyyy-MM-dd')";
                searchString = (date.Date == DateTime.MinValue ? "" : "DECLARE @DATE VARCHAR(MAX); SET @DATE=" + searchTimeString + ";") + "SELECT * FROM " + tableName + " WHERE PortfolioID = " + portfolio.ID + (date.Date == DateTime.MinValue ? "" : " AND " + string.Format("OrderDate>=@DATE + ' 00:00:00.000'") + " AND " + string.Format("OrderDate<=@DATE + ' 23:59:59.999'")) + "  ORDER BY OrderDate";
            }

            DataTable orderTable = Database.DB[portfolio.StrategyDB].ExecuteDataTable(tableName, searchString);

            DataRowCollection rows = orderTable.Rows;

            foreach (DataRow r in rows)
            {
                Order o = new Order((string)r["ID"], portfolio.ID, GetInt(r["ConstituentID"]), (double)r["Unit"], (DateTime)r["OrderDate"], (DateTime)r["ExecutionDate"], (OrderType)r["OrderType"], (double)r["Limit"], (OrderStatus)r["Status"], (double)r["ExecutionLevel"], (Boolean)r["Aggregated"], GetValue<string>(r, "Client"), GetValue<string>(r, "Destination"), GetValue<string>(r, "Account"));
                if(Database.DB[portfolio.StrategyDB] is QuantApp.Kernel.Adapters.SQL.SQLiteDataSetAdapter || Database.DB[portfolio.StrategyDB] is QuantApp.Kernel.Adapters.SQL.PostgresDataSetAdapter)
                    o = new Order((string)r["ID"], portfolio.ID, GetInt(r["ConstituentID"]), (double)r["Unit"], (DateTime)r["OrderDate"], (DateTime)r["ExecutionDate"], (OrderType)r["OrderType"], (double)r["Limits"], (OrderStatus)r["Status"], (double)r["ExecutionLevel"], (Boolean)r["Aggregated"], GetValue<string>(r, "Client"), GetValue<string>(r, "Destination"), GetValue<string>(r, "Account"));
                orders.Add(o);
            }

            if (date.Date == DateTime.MinValue)
                return orders;

            return orders;
        }

        public void RemoveReserves(Portfolio portfolio)
        {
            lock (objLock)
            {
                if (!portfolio.SimulationObject)
                {
                    if (!_reservesTables.ContainsKey(portfolio.ID))
                    {
                        DataTable reserveTable = Database.DB["Kernel"].GetDataTable(_portfolioReservesTableName, null, "ID = " + portfolio.ID);
                        _reservesTables.TryAdd(portfolio.ID, reserveTable);
                    }

                    foreach (DataRow row in _reservesTables[portfolio.ID].Rows)
                        row.Delete();

                    Database.DB["Kernel"].UpdateDataTable(_reservesTables[portfolio.ID]);
                }
            }
        }


        public void Remove(Portfolio portfolio)
        {
            if (_portfolioIdDB.ContainsKey(portfolio.ID))
            {
                Portfolio v = null;
                _portfolioIdDB.TryRemove(portfolio.ID, out v);
            }

            if (_processedCorporateActions.ContainsKey(portfolio.ID))
                _processedCorporateActions.Remove(portfolio.ID);

            try
            {
                string tableName = _processedCorporateActionsTableName;
                string searchString = "PortfolioID = " + portfolio.ID;

                Database.DB[portfolio.StrategyDB].ExecuteCommand("DELETE FROM " + tableName + " WHERE " + searchString);
            }
            catch (Exception e) { SystemLog.Write(e); }




            try
            {
                string tableName = _positionTableName;
                string searchString = "ID = " + portfolio.ID;

                Database.DB[portfolio.StrategyDB].ExecuteCommand("DELETE FROM " + tableName + " WHERE " + searchString);
            }
            catch (Exception e) { SystemLog.Write(e); }

            try
            {
                string tableName = _orderTableName;
                string searchString = "PortfolioID = " + portfolio.ID;

                Database.DB[portfolio.StrategyDB].ExecuteCommand("DELETE FROM " + tableName + " WHERE " + searchString);

            }
            catch (Exception e) { SystemLog.Write(e); }

            try
            {
                string tableName = _portfolioReservesTableName;
                string searchString = "ID = " + portfolio.ID;

                Database.DB["Kernel"].ExecuteCommand("DELETE FROM " + tableName + " WHERE " + searchString);
            }
            catch (Exception e) { SystemLog.Write(e); }



            Database.DB["Kernel"].ExecuteCommand("DELETE FROM " + _portfolioTableName + " WHERE ID = " + portfolio.ID);
        }
        public void Remove1(Portfolio portfolio)
        {
            RemoveReserves(portfolio);

            if (_portfolioIdDB.ContainsKey(portfolio.ID))
            {
                Portfolio v = null;
                _portfolioIdDB.TryRemove(portfolio.ID, out v);
            }

            try
            {
                string tableName = _positionTableName;
                string searchString = "ID = " + portfolio.ID + " ORDER BY Timestamp";
                string targetString = null;

                DataTable positionTable = Database.DB[portfolio.StrategyDB].GetDataTable(tableName, targetString, searchString);

                DataRowCollection rowsPosition = positionTable.Rows;
                if (rowsPosition.Count != 0)
                {
                    foreach (DataRow r in rowsPosition)
                        r.Delete();

                    Database.DB[portfolio.StrategyDB].DeleteDataTable(positionTable);
                }
            }
            catch (Exception e) { SystemLog.Write(e); }

            try
            {
                string tableName = _orderTableName;
                string searchString = "PortfolioID = " + portfolio.ID + " ORDER BY OrderDate";
                string targetString = null;

                DataTable orderTable = Database.DB[portfolio.StrategyDB].GetDataTable(tableName, targetString, searchString);

                DataRowCollection rowsPosition = orderTable.Rows;
                if (rowsPosition.Count != 0)
                {
                    foreach (DataRow r in rowsPosition)
                        r.Delete();

                    Database.DB[portfolio.StrategyDB].DeleteDataTable(orderTable);
                }
            }
            catch (Exception e) { SystemLog.Write(e); }

            if (!_mainTables.ContainsKey(portfolio.ID))
                _mainTables.TryAdd(portfolio.ID, Database.DB["Kernel"].GetDataTable(_portfolioTableName, null, "ID = " + portfolio.ID));


            DataTable table = _mainTables[portfolio.ID];

            if (table != null)
            {
                DataRowCollection rows = table.Rows;
                if (rows.Count == 0)
                    return;

                DataRow row = rows[0];
                row.Delete();
                Database.DB["Kernel"].UpdateDataTable(table);
            }

        }

        public void RemoveFrom(Portfolio portfolio, DateTime date)
        {
            try
            {
                RemoveOrdersFrom(portfolio, date);
                RemovePositionsFrom(portfolio, date);
                RemoveCorporateActions(portfolio);
            }
            catch (Exception e) { SystemLog.Write(e); }
        }

        public void RemovePositionsFrom(Portfolio portfolio, DateTime date)
        {
            try
            {
                Database.DB[portfolio.StrategyDB].ExecuteCommand("DELETE FROM " + _positionTableName + " WHERE ID = " + portfolio.ID + " AND Timestamp >= '" + date.ToString("yyyy/MM/dd HH:mm:ss.fff") + "'");
            }
            catch (Exception e) { SystemLog.Write(e); }
        }

        public void RemoveOrdersFrom(Portfolio portfolio, DateTime date)
        {
            try
            {
                Database.DB[portfolio.StrategyDB].ExecuteCommand("DELETE FROM " + _orderTableName + " WHERE PortfolioID = " + portfolio.ID + " AND OrderDate >= '" + date.ToString("yyyy/MM/dd HH:mm:ss.fff") + "'");
            }
            catch (Exception e) { SystemLog.Write(e); }
        }

        public void RemoveCorporateActions(Portfolio portfolio)
        {
            try
            {
                Database.DB[portfolio.StrategyDB].ExecuteCommand("DELETE FROM " + _processedCorporateActionsTableName + " WHERE PortfolioID = " + portfolio.ID);
            }
            catch (Exception e) { SystemLog.Write(e); }
        }

        public void SetProperty(Portfolio instrument, string name, object value)
        {
            lock (objLock)
            {
                if (instrument.SimulationObject)
                    return;
                if (!_mainTables.ContainsKey(instrument.ID))
                    _mainTables.TryAdd(instrument.ID, Database.DB["Kernel"].GetDataTable(_portfolioTableName, null, "ID = " + instrument.ID));

                DataTable table = _mainTables[instrument.ID];

                if (table != null)
                {
                    DataRowCollection rows = table.Rows;
                    if (rows.Count == 0)
                        return;

                    DataRow row = rows[0];
                    row[name] = value;
                    Database.DB["Kernel"].UpdateDataTable(table);
                }
            }
        }
    }
}
