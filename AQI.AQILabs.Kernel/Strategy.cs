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
using System.Reflection;

using QuantApp.Kernel;

using AQI.AQILabs.Kernel.Numerics.Util;

namespace AQI.AQILabs.Kernel
{
    /// <summary>
    /// Enumeration of possible Direction Types
    /// This value that is set for all Strategy objects.
    /// </summary>    
    /// <remarks>
    /// Long to follow a positive exposure to the underlying strategy or Short for the opposite
    /// </remarks>
    public enum DirectionType
    {
        Long = 1, Short = -1
    };

    /// <summary>
    /// Structure used by Kernel when saving MemorySeries values in memory
    /// </summary>    
    /// <remarks>
    /// Internal use by Kernel Only.
    /// </remarks>
    public struct MemorySeriesPoint
    {
        public int ID;
        public int memorytype;
        public int memoryclass;
        public DateTime date;
        public double value;

        /// <summary>
        /// Constructor: ID--Strategy ID
        /// </summary>
        /// <remarks>
        /// MemorySeriesPoints are stored in a three dimensional matrix for each Strategy.
        /// Coordinates: (date, memorytype, memoryclass) for each value in a Strategy MemorySeries.
        /// </remarks>
        /// <param name="ID">ID for Strategy</param>        
        /// <param name="memorytype">Type of memory point</param>
        /// <param name="memoryclass">Class of memory point</param>
        /// <param name="value">TimeSeries point value</param>        
        public MemorySeriesPoint(int ID, int memorytype, int memoryclass, DateTime date, double value)
        {
            this.ID = ID;
            this.memorytype = memorytype;
            this.memoryclass = memoryclass;
            this.date = date;
            this.value = value;
        }
    }

    /// <summary>
    /// Structure used by Kernel when passing Logic Execution information to the ExecuteLogic function of a Strategy.
    /// </summary>    
    /// <remarks>
    /// Internal use by Kernel Only.
    /// </remarks>
    public struct ExecutionContext
    {
        //public double PortfolioReturn;
        //public double Index_t;
        public BusinessDay OrderDate;
        public double ReferenceAUM;

        /// <summary>
        /// Constructor of Order Context reprenting a specific snap-shot.
        /// </summary>
        /// <param name="orderDate">BusinessDay for this specific snap-shot</param>                
        /// <param name="reference_aum">AUM for this specific snap-shot</param>        
        public ExecutionContext(BusinessDay orderDate, double reference_aum)
        {
            OrderDate = orderDate;
            ReferenceAUM = reference_aum;
        }
    }

    /// <summary>
    /// Strategy skeleton containing
    /// the most general functions and variables.
    /// This class also manages the connectivity
    /// to the database through a relevant Factories.
    /// </summary>
    public class Strategy : Instrument
    {
        new public static AQI.AQILabs.Kernel.Factories.IStrategyFactory Factory = null;


        /// <summary>
        /// Property containt int value for the unique ID of the instrument
        /// </summary>
        /// <remarks>
        /// Main identifier for each Instrument in the System
        /// </remarks>e
        new public int ID
        {
            get
            {
                return base.ID;
            }
        }

        private bool _simulating = false;
        /// <summary>
        /// Property which is true if the strategy is currently being simulated historically.
        /// </summary>        
        public bool Simulating
        {
            get
            {
                if (Portfolio != null && Portfolio != Portfolio.MasterPortfolio)
                    return Portfolio.MasterPortfolio.Strategy.Simulating;

                return this._simulating;
            }
            set
            {
                this._simulating = value;
            }
        }

        /// <summary>
        /// Function: Clear the Strategy memory for a specific date. (Does not clear AUM Memory)
        /// </summary>       
        /// <param name="date">DateTime value date 
        /// </param>
        public void ClearMemory(DateTime date)
        {

            Factory.ClearMemory(this, date);
        }

        /// <summary>
        /// Function: Clear the Strategy AUM memory for a specific date.
        /// </summary>       
        /// <param name="date">DateTime value date 
        /// </param>
        public void ClearAUMMemory(DateTime date)
        {
            Factory.ClearAUMMemory(this, date);
        }

        /// <summary>
        /// Property: Tree valued reference to the execution Tree of the Strategy.
        /// </summary>
        /// <remarks>
        /// Can only be set by the Kernel.
        /// </remarks>
        [Newtonsoft.Json.JsonIgnore]
        public Tree Tree { get; private set; }

        private int _portfolioID = -10;
        private string _class = null;
        private DateTime _initialDate = DateTime.MinValue;
        private DateTime _finalDate = DateTime.MinValue;
        private string _dbConnection = null;
        private string _schedule = null;

        /// <summary>
        /// Constructor of the Strategy Class        
        /// </summary>
        /// <remarks>
        /// Only used Strategy implementations.
        /// </remarks>
        public Strategy(Instrument instrument)
            : base(instrument)
        {
            if (!SimulationObject)
                this.Cloud = instrument.Cloud;

            Factory.UpdateStrategyDB(this);
            Tree = Tree.GetTree(this);
            this._class = this.GetType().ToString();
        }

        /// <summary>
        /// Constructor of the Strategy Class        
        /// </summary>
        /// <remarks>
        /// Only used Strategy implementations.
        /// </remarks>
        public Strategy(Instrument instrument, string className)
            : base(instrument)
        {
            if (!SimulationObject)
                this.Cloud = instrument.Cloud;

            Factory.UpdateStrategyDB(this);
            Tree = Tree.GetTree(this);
            this._class = className;
        }

        /// <summary>
        /// Constructor of the Strategy Class        
        /// </summary>
        /// <remarks>
        /// Only used Strategy implementations.
        /// </remarks>
        public Strategy(int id)
            : base(id)
        {

            if (!SimulationObject)
                this.Cloud = Instrument.FindCleanInstrument(id).Cloud;

            Factory.UpdateStrategyDB(this);
            Tree = Tree.GetTree(this);
            this._class = this.GetType().ToString();
        }

        /// <summary>
        /// Property: Integer valued ID of the portfolio instance linked to this Strategy.
        /// </summary>
        public int PortfolioID
        {
            get
            {
                return _portfolioID;
            }
            set
            {
                this._portfolioID = value;
            }
        }

        [Newtonsoft.Json.JsonIgnore]
        public int PortfolioIDLocal
        {
            get
            {
                return _portfolioID;
            }
            set
            {
                this._portfolioID = value;
                this._portfolio = (Instrument.FindInstrument(value) as Portfolio);
                if (!SimulationObject)
                    if (Factory != null)
                        Factory.SetProperty(this, "PortfolioID", _portfolioID);

            }
        }

        private Portfolio _portfolio = null;

        /// <summary>
        /// Property: Portfolio valued reference to the portfolio instance linked to this Strategy.
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public Portfolio Portfolio
        {
            get
            {
                if (_portfolioID == -1 || _portfolioID == -10)
                    return null;
                else
                {
                    if (_portfolio == null)
                        _portfolio = Instrument.FindInstrument(_portfolioID) as Portfolio;// Factory.FindPortfolio(_portfolioID);
                    return _portfolio;
                }
            }
            set
            {
                if (_portfolio != value)
                {
                    if (value == null)
                    {
                        _portfolioID = -1;
                        _portfolio = null;
                    }
                    else
                    {
                        _portfolioID = value.ID;
                        _portfolio = value;
                    }
                    if (!SimulationObject)
                    {
                        if (Factory != null)
                            Factory.SetProperty(this, "PortfolioID", _portfolioID);

                        if (this.Cloud)
                            if (RTDEngine.Publish(this))
                                RTDEngine.Send(new RTDMessage() { Type = RTDMessage.MessageType.Property, Content = new RTDMessage.PropertyMessage { ID = ID, Name = "PortfolioIDLocal", Value = _portfolioID } });
                    }
                }
            }
        }

        /// <summary>
        /// Property: Checkes if the strategy is a Residual Strategy.
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public bool IsResidual
        {
            get
            {
                return (Portfolio != null && Portfolio.MasterPortfolio.Residual == this);
            }
        }

        /// <summary>
        /// Property: DateTime valued reference to the initial date of the strategy (t=0).
        /// </summary>
        /// <remarks>
        /// Used by Kernel when serializing object.
        /// </remarks>
        public DateTime InitialDateMemory
        {
            get
            {
                return _initialDate;
            }
            set
            {
                _initialDate = value;
            }
        }

        /// <summary>
        /// Property: DateTime valued reference to the final date the strategy.
        /// </summary>
        /// <remarks>
        /// Used by Kernel when serializing object.
        /// </remarks>
        public DateTime FinalDateMemory
        {
            get
            {
                return _finalDate;
            }
            set
            {
                _finalDate = value;
            }
        }

        /// <summary>
        /// Property: string valued reference to the connection string for the persistent storage functionality.
        /// </summary>
        /// <remarks>
        /// Used by Kernel when serializing object.
        /// </remarks>
        public string DBConnectionMemory
        {
            get
            {
                return _dbConnection;
            }
            set
            {
                _dbConnection = value;
            }
        }

        /// <summary>
        /// Property: String valued reference to the scheduling information.
        /// </summary>
        public string ScheduleCommandMemory
        {
            get
            {
                return _schedule;
            }
            set
            {
                this._schedule = value;
            }
        }

        /// <summary>
        /// Delegate: skeleton for custom calculation functions called from the scheduler
        /// </summary>
        public delegate void Calculation(DateTime date);
        public Calculation JobCalculation = null;
        private StrategyJobExecutor _jobExecutor = null;

        /// <summary>
        /// Property: Is the strategy scheduler running?
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public bool IsSchedulerStarted { get; private set; }

        public static bool Executer = false;
        internal bool Executing = false;
        public readonly object ExecutionLock = new object();

        /// <summary>
        /// Function: start scheduler according the ScheduleCommand instructions
        /// </summary>
        public void StartScheduler()
        {
            try
            {
                StartSchedulerLocal();
                if (this.Cloud)
                    if (RTDEngine.Publish(this))
                        RTDEngine.Send(new RTDMessage() { Type = RTDMessage.MessageType.Function, Content = new RTDMessage.FunctionMessage { ID = ID, Name = "StartSchedulerLocal", Parameters = null } });
            }
            catch (Exception e)
            { }
        }

        /// <summary>
        /// Function: start scheduler according the ScheduleCommand instructions without remote distribution
        /// </summary>
        public void StartSchedulerLocal()
        {
            try
            {
                if (ScheduleCommand != null)
                {
                    _jobExecutor = new StrategyJobExecutor(this);

                    string[] commands = (this.ScheduleCommand.Contains(";") ? this.ScheduleCommand : (this.ScheduleCommand + ";")).Split(new char[] { ';' });

                    foreach (string command in commands)
                        if (!string.IsNullOrWhiteSpace(command))
                        {
                            if (Executer)
                                _jobExecutor.StartJob(command.Replace(";", ""));
                            IsSchedulerStarted = true;
                        }
                }
            }
            catch (Exception e)
            { }
        }

        /// <summary>
        /// Function: re-start scheduler according the ScheduleCommand instructions
        /// </summary>
        public void ReStartScheduler()
        {
            try
            {
                ReStartSchedulerLocal();
                if (this.Cloud)
                    if (RTDEngine.Publish(this))
                        RTDEngine.Send(new RTDMessage() { Type = RTDMessage.MessageType.Function, Content = new RTDMessage.FunctionMessage { ID = ID, Name = "ReStartSchedulerLocal", Parameters = null } });
            }
            catch (Exception e)
            { }
        }

        /// <summary>
        /// Function: re-start scheduler according the ScheduleCommand instructions
        /// </summary>
        public void ReStartSchedulerLocal()
        {
            try
            {

                if (ScheduleCommand != null && _jobExecutor != null)
                {
                    if (Executer)
                        _jobExecutor.StopJob();
                    string[] commands = ScheduleCommand.Split(new char[] { ';' });

                    foreach (string command in commands)
                        if (!string.IsNullOrWhiteSpace(command))
                            if (Executer)
                                _jobExecutor.StartJob(command.Replace(";", ""));

                    //if (this.Cloud)
                    //    RTDEngine.Send(new RTDMessage() { Type = RTDMessage.MessageType.Function, Content = new RTDMessage.FunctionMessage { ID = ID, Name = "ReStartScheduler", Parameters = null } });
                }
            }
            catch (Exception e)
            { }
        }

        /// <summary>
        /// Function: stop scheduler
        /// </summary>
        public void StopScheduler()
        {
            try
            {
                StopSchedulerLocal();
                if (this.Cloud)
                    if (RTDEngine.Publish(this))
                        RTDEngine.Send(new RTDMessage() { Type = RTDMessage.MessageType.Function, Content = new RTDMessage.FunctionMessage { ID = ID, Name = "StopSchedulerLocal", Parameters = null } });
            }
            catch (Exception e)
            { }
        }

        /// <summary>
        /// Function: stop scheduler
        /// </summary>
        public void StopSchedulerLocal()
        {
            try
            {
                if (_jobExecutor != null)
                {
                    if (Executer)
                        _jobExecutor.StopJob();
                    IsSchedulerStarted = false;

                    //if (this.Cloud)
                    //    RTDEngine.Send(new RTDMessage() { Type = RTDMessage.MessageType.Function, Content = new RTDMessage.FunctionMessage { ID = ID, Name = "StopScheduler", Parameters = null } });

                }
            }
            catch (Exception e)
            { }
        }

        /// <summary>
        /// Property: string valued reference to class name of the Strategy.
        /// </summary>
        /// <remarks>
        /// Used by Kernel when serializing object.
        /// </remarks>
        public string ClassMemory
        {
            get
            {
                return _class;
            }
            set
            {
                this._class = value;
            }
        }


        /// <summary>
        /// Property: string valued reference to class name of the Strategy.
        /// </summary>
        /// <remarks>
        /// Used by Kernel.
        /// </remarks>
        public string Class
        {
            get
            {
                return _class;
            }
            set
            {
                this._class = value;
                if (!SimulationObject)
                {
                    if (Factory != null)
                        Factory.SetProperty(this, "Class", value);

                    if (this.Cloud)
                        if (RTDEngine.Publish(this))
                            RTDEngine.Send(new RTDMessage() { Type = RTDMessage.MessageType.Property, Content = new RTDMessage.PropertyMessage { ID = ID, Name = "ClassLocal", Value = value } });
                }
            }
        }
        public string ClassLocal
        {
            get
            {
                return _class;
            }
            set
            {
                this._class = value;
                if (!SimulationObject)
                {
                    if (Factory != null)
                        Factory.SetProperty(this, "Class", value);
                }
            }
        }

        /// <summary>
        /// Property: DateTime valued reference to the initial date of the strategy (t=0).
        /// </summary>
        public DateTime InitialDate
        {
            get
            {
                return _initialDate;
            }
            set
            {
                _initialDate = value;
                if (!SimulationObject)
                {
                    if (Factory != null)
                        Factory.SetProperty(this, "InitialDate", value);

                    if (this.Cloud)
                        if (RTDEngine.Publish(this))
                            RTDEngine.Send(new RTDMessage() { Type = RTDMessage.MessageType.Property, Content = new RTDMessage.PropertyMessage { ID = ID, Name = "InitialDateLocal", Value = value } });
                }
            }
        }
        public DateTime InitialDateLocal
        {
            get
            {
                return _initialDate;
            }
            set
            {
                _initialDate = value;
                if (!SimulationObject)
                {
                    if (Factory != null)
                        Factory.SetProperty(this, "InitialDate", value);
                }
            }
        }

        /// <summary>
        /// Property: DateTime valued reference to the final date the strategy.
        /// </summary>
        public DateTime FinalDate
        {
            get
            {
                if (SimulationObject && _finalDate == DateTime.MinValue)
                    _finalDate = DateTime.MaxValue;

                if (_finalDate == DateTime.MinValue && !SimulationObject)
                {
                    if (_finalDate == DateTime.MinValue)
                        _finalDate = DateTime.MaxValue;
                }

                return _finalDate;
            }
            set
            {
                _finalDate = value;
                if (!SimulationObject)
                {
                    if (Factory != null)
                        Factory.SetProperty(this, "FinalDate", value);

                    if (this.Cloud)
                        if (RTDEngine.Publish(this))
                            RTDEngine.Send(new RTDMessage() { Type = RTDMessage.MessageType.Property, Content = new RTDMessage.PropertyMessage { ID = ID, Name = "FinalDateLocal", Value = value } });
                }
            }
        }
        public DateTime FinalDateLocal
        {
            get
            {
                if (SimulationObject && _finalDate == DateTime.MinValue)
                    _finalDate = DateTime.MaxValue;

                if (_finalDate == DateTime.MinValue && !SimulationObject)
                {
                    if (_finalDate == DateTime.MinValue)
                        _finalDate = DateTime.MaxValue;
                }

                return _finalDate;
            }
            set
            {
                _finalDate = value;
                if (!SimulationObject)
                {
                    if (Factory != null)
                        Factory.SetProperty(this, "FinalDate", value);
                }
            }
        }

        /// <summary>
        /// Property: String valued reference to the scheduling information.
        /// </summary>
        public string ScheduleCommand
        {
            get
            {
                return _schedule;
            }
            set
            {
                this._schedule = value;

                if (IsSchedulerStarted)
                    this.ReStartScheduler();

                if (!SimulationObject)
                    if (Factory != null)
                        Factory.SetProperty(this, "Scheduler", value);

                if (this.Cloud)
                    if (RTDEngine.Publish(this))
                        RTDEngine.Send(new RTDMessage() { Type = RTDMessage.MessageType.Property, Content = new RTDMessage.PropertyMessage { ID = ID, Name = "ScheduleCommandLocal", Value = value } });
            }
        }
        public string ScheduleCommandLocal
        {
            get
            {
                return _schedule;
            }
            set
            {
                this._schedule = value;

                if (IsSchedulerStarted)
                    this.ReStartScheduler();

                if (!SimulationObject)
                    if (Factory != null)
                        Factory.SetProperty(this, "Scheduler", value);
            }
        }

        /// <summary>
        /// Property: string valued reference to the connection string for the persistent storage functionality.
        /// </summary>        
        public string DBConnection
        {
            get
            {
                return string.IsNullOrEmpty(_dbConnection) ? "DefaultStrategy" : _dbConnection;
            }
            set
            {
                _dbConnection = value;
                if (!SimulationObject)
                {
                    if (Factory != null)
                        Factory.SetProperty(this, "DBConnection", value);

                    if (this.Cloud)
                        if (RTDEngine.Publish(this))
                            RTDEngine.Send(new RTDMessage() { Type = RTDMessage.MessageType.Property, Content = new RTDMessage.PropertyMessage { ID = ID, Name = "DBConnectionLocal", Value = value } });
                }
            }
        }
        public string DBConnectionLocal
        {
            get
            {
                return string.IsNullOrEmpty(_dbConnection) ? "DefaultStrategy" : _dbConnection;
            }
            set
            {
                _dbConnection = value;
                if (!SimulationObject)
                {
                    if (Factory != null)
                        Factory.SetProperty(this, "DBConnection", value);
                }
            }
        }

        /// <summary>
        /// Function: Remove the Strategy from the persistent storage.
        /// </summary>            
        new public void Remove()
        {
            if (Portfolio != null)
            {
                Portfolio.Remove();
                Portfolio = null;
            }

            Factory.Remove(this);
            base.Remove();
        }

        /// <summary>
        /// Function: Remove strategy data string from and including a given date.
        /// </summary>         
        /// <param name="date">reference date.
        /// </param>
        new public void RemoveFrom(DateTime date)
        {
            base.RemoveFrom(date);
            if (Portfolio != null)
                Portfolio.RemoveFrom(date);

            Factory.RemoveFrom(this, date);
        }

        /// <summary>
        /// Function: Save and commit all values changed for this Instrument in persistent storage.
        /// </summary>            
        public override void Save()
        {
            SaveLocal();
            if (!SimulationObject)
            {
                if (this.Cloud)
                    if (RTDEngine.Publish(this))
                        RTDEngine.Send(new RTDMessage() { Type = RTDMessage.MessageType.Function, Content = new RTDMessage.FunctionMessage { ID = ID, Name = "SaveLocal", Parameters = null } });
            }
        }

        /// <summary>
        /// Function: Save and commit all values changed for this Instrument in persistent storage.
        /// </summary>            
        public override void SaveLocal()
        {
            if (this.Portfolio != null && !this.Portfolio.MasterPortfolio.CanSave)
                return;

            base.SaveLocal();
            if (!SimulationObject)
            {
                Factory.Save(this);
            }
        }

        /// <summary>
        /// Function: Retrieve memory series object.
        /// </summary>       
        /// <param name="memorytype">Memory series type of object to be retrieved.
        /// </param>
        /// <param name="memoryclass">Memory series class of object to be retrieved.
        /// </param>
        public TimeSeries GetMemorySeries(int memorytype, int memoryclass)
        {
            return Factory.GetMemorySeries(this, memorytype, memoryclass);
        }

        /// <summary>
        /// Function: Retrieve memory series object.
        /// </summary>       
        /// <param name="memorytype">Memory series type of object to be retrieved.
        /// </param>
        public TimeSeries GetMemorySeries(int memorytype)
        {
            return GetMemorySeries(memorytype, _max_id_do_not_use);
        }

        /// <summary>
        /// Function: Retrieve dictionary with all memory series objects index by a pair of integers
        /// representing the [memorytype, memoryclass].
        /// </summary>       
        public Dictionary<int[], TimeSeries> GetMemorySeries()
        {
            return Factory.GetMemorySeries(this);
        }

        /// <summary>
        /// Function: Retrieve dictionary with all memory series objects index by a pair of integers
        /// representing the [memorytype, memoryclass].
        /// </summary>       
        public List<int[]> GetMemorySeriesIds()
        {
            return new List<int[]>(Factory.GetMemorySeriesIds(this));
        }

        /// <summary>
        /// Function: Retrieve value from memory series object.
        /// </summary>       
        /// <param name="date">DateTime value representing the date of the value to be retrieved.
        /// </param>
        /// <param name="memorytype">Memory series type of object to be retrieved.
        /// </param>
        /// <param name="memoryclass">Memory series class of object to be retrieved.
        /// </param>
        /// <param name="timeSeriesRoll">Roll type of reference time series object.
        /// </param>
        public double this[DateTime date, int memorytype, int memoryclass, TimeSeriesRollType timeSeriesRoll]
        {
            get
            {
                return Factory.GetMemorySeriesPoint(this, date, memorytype, memoryclass, timeSeriesRoll);
            }
        }

        /// <summary>
        /// Function: Retrieve value from memory series object.
        /// </summary>       
        /// <param name="date">DateTime value representing the date of the value to be retrieved.
        /// </param>
        /// <param name="memorytype">Memory series type of object to be retrieved.
        /// </param>
        /// <param name="memoryclass">Memory series class of object to be retrieved.
        /// </param>
        public double this[DateTime date, int memroytype, int memroyclass]
        {
            get
            {
                return this[date, memroytype, memroyclass, TimeSeriesRoll];
            }
        }

        /// <summary>
        /// Function: Retrieve value from memory series object.
        /// </summary>       
        /// <param name="date">DateTime value representing the date of the value to be retrieved.
        /// </param>
        /// <param name="memorytype">Memory series type of object to be retrieved.
        /// </param>
        /// <param name="timeSeriesRoll">Roll type of reference time series object.
        /// </param>
        public double this[DateTime date, int memorytype, TimeSeriesRollType timeSeriesRoll]
        {
            get
            {
                return this[date, memorytype, _max_id_do_not_use, timeSeriesRoll];
            }
        }

        /// <summary>
        /// Function: Retrieve value from memory series object.
        /// </summary>       
        /// <param name="date">DateTime value representing the date of the value to be retrieved.
        /// </param>
        /// <param name="memorytype">Memory series type of object to be retrieved.
        /// </param>
        public double this[DateTime date, int memroytype]
        {
            get
            {
                return this[date, memroytype, _max_id_do_not_use, TimeSeriesRoll];
            }
        }


        /// <summary>
        /// Function: Add value to memory series object.
        /// </summary>       
        /// <param name="date">DateTime value representing the date of the value to be retrieved.
        /// </param>
        /// <param name="value">Value to be added to the time series object.
        /// </param>
        /// <param name="memorytype">Memory series type of object to be retrieved.
        /// </param>
        /// <param name="memoryclass">Memory series class of object to be retrieved.
        /// </param>
        public void AddMemoryPoint(DateTime date, double value, int memorytype, int memoryclass)
        {
            if (!(Double.IsNaN(value) || Double.IsInfinity(value)))
                Factory.AddMemoryPoint(this, date, value, memorytype, memoryclass, true);

            if (!this.SimulationObject && this.Cloud)
                if (RTDEngine.Publish(this))
                    RTDEngine.Send(new RTDMessage() { Type = RTDMessage.MessageType.StrategyData, Content = new RTDMessage.StrategyData() { InstrumentID = this.ID, Value = value, MemoryClassID = memoryclass, MemoryTypeID = memorytype, Timestamp = date } });
        }

        public void AddMemoryPoint(DateTime date, double value, int memorytype, int memoryclass, Boolean onlyMemory, Boolean share = true)
        {                
            if (!(Double.IsNaN(value) || Double.IsInfinity(value)))
            {
                Factory.AddMemoryPoint(this, date, value, memorytype, memoryclass, onlyMemory);

                if (!this.SimulationObject && this.Cloud && share)
                    if (RTDEngine.Publish(this))
                        RTDEngine.Send(new RTDMessage() { Type = RTDMessage.MessageType.StrategyData, Content = new RTDMessage.StrategyData() { InstrumentID = this.ID, Value = value, MemoryClassID = memoryclass, MemoryTypeID = memorytype, Timestamp = date } });

            }
        }

        /// <summary>
        /// Function: Add value to memory series object.
        /// </summary>       
        /// <param name="date">DateTime value representing the date of the value to be retrieved.
        /// </param>
        /// <param name="value">Value to be added to the time series object.
        /// </param>
        /// <param name="memorytype">Memory series type of object to be retrieved.
        /// </param>
        public void AddMemoryPoint(DateTime date, double value, int memorytype)
        {
            AddMemoryPoint(date, value, memorytype, _max_id_do_not_use, true);
        }
        public void AddMemoryPoint(DateTime date, double value, int memorytype, Boolean onlyMemory)
        {
            AddMemoryPoint(date, value, memorytype, _max_id_do_not_use, onlyMemory);
        }

        public static int _aum_id_do_not_use = -1010991803;
        public static int _aum_chg_id_do_not_use = -1010991813;
        public static int _aum_ord_chg_id_do_not_use = -1010991843;
        public static int _universe_id_do_not_use = -1010991823;
        public static int _max_id_do_not_use = -1010991833;
        public static int _direction_id_do_not_use = -1010991834;

        /// <summary>
        /// Function: Retrieve instrument universe.
        /// </summary>       
        /// <param name="date">DateTime value representing the date of the universe to be retrieved.
        /// </param>        
        public new Dictionary<int, Instrument> Instruments(DateTime date, bool aggregated)
        {
            Dictionary<int, Instrument> instruments = new Dictionary<int, Instrument>();

            if (aggregated)
                InternalInstruments(date, ref instruments);
            else
                for (int ii = 0; ; ii++)
                {
                    double underlyingID = this[date, _universe_id_do_not_use, -ii, TimeSeriesRollType.Last];
                    if (double.IsNaN(underlyingID) || underlyingID == double.MinValue || underlyingID == double.MaxValue)
                        break;
                    else
                    {
                        Instrument instrument = Instrument.FindInstrument((int)underlyingID);
                        if (instrument != null && !instruments.ContainsKey(instrument.ID))
                            instruments.Add(instrument.ID, instrument);
                    }
                }

            return instruments;
        }

        private void InternalInstruments(DateTime date, ref Dictionary<int, Instrument> instruments)
        {
            for (int ii = 0; ; ii++)
            {
                double underlyingID = this[date, _universe_id_do_not_use, -ii, TimeSeriesRollType.Last];
                if (double.IsNaN(underlyingID) || underlyingID == double.MinValue || underlyingID == double.MaxValue)
                    break;
                else
                {
                    Instrument instrument = Instrument.FindInstrument((int)underlyingID);
                    if (instrument != null)
                    {
                        if (instrument.InstrumentType == Kernel.InstrumentType.Strategy)
                            (instrument as Strategy).InternalInstruments(date, ref instruments);
                        else if (!instruments.ContainsKey(instrument.ID))
                            instruments.Add(instrument.ID, instrument);
                    }
                }
            }
        }

        /// <summary>
        /// Function: Add instrument to instrumnt universe.
        /// </summary>       
        /// <param name="date">DateTime value representing the date of the universe to be added.
        /// </param>
        public void AddInstrument(Instrument instrument, DateTime date)
        {
            Dictionary<int, Instrument> instruments = Instruments(date, false);

            if (!instruments.ContainsKey(instrument.ID))
            {
                this.AddMemoryPoint(date, instrument.ID, _universe_id_do_not_use, -instruments.Count);

                if (instrument.InstrumentType == Kernel.InstrumentType.Strategy)
                {
                    this.Tree.AddSubStrategy((Strategy)instrument);
                    this.AddRemoveSubStrategies((Strategy)instrument, instrument.Calendar.GetClosestBusinessDay(date, TimeSeries.DateSearchType.Previous));
                }
            }
        }

        /// <summary>
        /// Function: Add instrument to instrumnt universe.
        /// </summary>       
        /// <param name="date">DateTime value representing the date of the universe to be added.
        /// </param>
        public void AddInstrument(Instrument instrument, DateTime date, int i)
        {
            //Dictionary<int, Instrument> instruments = Instruments(date, false);

            //if (!instruments.ContainsKey(instrument.ID))
            {
                this.AddMemoryPoint(date, instrument.ID, _universe_id_do_not_use, -i);

                if (instrument.InstrumentType == Kernel.InstrumentType.Strategy)
                {
                    this.Tree.AddSubStrategy((Strategy)instrument);
                    this.AddRemoveSubStrategies((Strategy)instrument, instrument.Calendar.GetClosestBusinessDay(date, TimeSeries.DateSearchType.Previous));
                }
            }
        }

        /// <summary>
        /// Function: Remove instrument from instrumnt universe.
        /// </summary>       
        /// <param name="date">DateTime value representing the date of the universe to be removed.
        /// </param>
        public void RemoveInstrument(Instrument instrument, DateTime date)
        {
            Dictionary<int, Instrument> instruments = Instruments(date, false);

            if (instruments.ContainsKey(instrument.ID))
            {
                instruments.Remove(instrument.ID);
                int j = 0;
                foreach (Instrument i in instruments.Values)
                {
                    this.AddMemoryPoint(date, i.ID, _universe_id_do_not_use, -j);
                    j++;
                }

                this.AddMemoryPoint(date, double.MaxValue, _universe_id_do_not_use, -j);
            }
        }

        /// <summary>
        /// Function: Remove instrument from instrumnt universe.
        /// </summary>       
        /// <param name="date">DateTime value representing the date of the universe to be removed.
        /// </param>
        public void RemoveInstruments(DateTime date)
        {
            Dictionary<int, Instrument> instruments = Instruments(date, false);

            for (int i = 0; i < instruments.Count; i++)
                this.AddMemoryPoint(date, double.MaxValue, _universe_id_do_not_use, -i);
        }

        /// <summary>
        /// Function: Retreive next trading date in relation to a given date.
        /// </summary>       
        /// <param name="date">DateTime value representing the date of the next trading date to be retrieved.
        /// </param>
        public DateTime NextTradingDate(DateTime date)
        {
            return NextTradingBusinessDate(date).DateTime;
        }

        /// <summary>
        /// Function: Retreive next trading date in relation to a given date.
        /// </summary>       
        /// <param name="date">BusinessDay value representing the date of the next trading date to be retrieved.
        /// </param>
        public BusinessDay NextTradingBusinessDate(DateTime date)
        {
            return Calendar.NextTradingBusinessDate(date);
        }

        /// <summary>
        /// Function: Retreive assets under management (AUM) in relation to a given date.
        /// </summary>       
        /// <param name="date">DateTime value representing the date of the AUM to be retrieved.
        /// </param>
        /// <param name="ttype">Type of time series object.
        /// </param>
        public double GetAUM(DateTime date, TimeSeriesType ttype)
        {
            double value = this[date, _aum_id_do_not_use, (int)ttype, TimeSeriesRollType.Last];

            if (double.IsNaN(value) && date == date.Date)//UNDERSTAND!! || value < 0.0)
            {
                TimeSeries aum_ts = this.GetMemorySeries(_aum_id_do_not_use, (int)ttype);
                if (aum_ts != null && aum_ts.Count > 0)
                    return aum_ts[0];
            }

            return value;
        }

        /// <summary>
        /// Function: Retreive assets under management (AUM) in relation to a given date.
        /// </summary>       
        /// <param name="date">DateTime value representing the date of the AUM to be retrieved.
        /// </param>
        /// <param name="ttype">Type of time series object.
        /// </param>
        public double GetAUMChange(DateTime date, TimeSeriesType ttype)
        {

            double value = this[date, _aum_chg_id_do_not_use, (int)ttype, TimeSeriesRollType.Last];

            if (double.IsNaN(value))
                value = 0;

            return value;
        }

        /// <summary>
        /// Function: Retreive assets under management (AUM) in relation to a given date.
        /// </summary>       
        /// <param name="date">DateTime value representing the date of the AUM to be retrieved.
        /// </param>
        /// <param name="ttype">Type of time series object.
        /// </param>
        public double GetOrderAUMChange(DateTime date, TimeSeriesType ttype)
        {
            double value = this[date, _aum_ord_chg_id_do_not_use, (int)ttype, TimeSeriesRollType.Last];

            if (double.IsNaN(value))
                value = 0;

            return value;
        }

        /// <summary>
        /// Function: Retrieve assets under management (AUM) in relation to a given date.
        /// </summary>       
        /// <param name="start">DateTime value representing the date of the AUM to be retrieved.
        /// </param>
        /// /// <param name="end">DateTime value representing the date of the AUM to be retrieved.
        /// </param>
        /// <param name="ttype">Type of time series object.
        /// </param>
        public double GetAggregegatedAUMChanges(DateTime start, DateTime end, TimeSeriesType ttype)
        {
            if (start == end)
                return 0.0;

            TimeSeries ts = this.GetMemorySeries(_aum_chg_id_do_not_use, (int)ttype);
            //if (ts != null && ts.Count > 0)
            //    ts = ts.GetRange(start, end, TimeSeries.RangeFillType.None);



            if (ts != null && ts.Count > 0)
            {
                double res = 0;
                for (int i = 0; i < ts.Count; i++)
                {
                    DateTime dt = ts.DateTimes[i];
                    double val = ts.Data[i];
                    if (!double.IsInfinity(val) && !double.IsNaN(val) && (dt >= start && dt <= end))
                        res += val;
                }
                return res;
            }

            return 0;
        }

        /// <summary>
        /// Function: Retreive assets under management (AUM) in relation
        /// to the next business date of given date.
        /// </summary>       
        /// <param name="date">DateTime value representing the date of the AUM to be retrieved.
        /// </param>
        public double GetNextAUM(DateTime date, TimeSeriesType ttype)
        {
            double chg = GetOrderAUMChange(date, ttype);
            return GetAUM(date, ttype) + (double.IsNaN(chg) || double.IsInfinity(chg) ? 0 : chg);
        }

        /// <summary>
        /// Function: Retreive start of day assets under management (AUM) in relation
        /// to the next business date of given date.
        /// </summary>       
        /// <param name="date">DateTime value representing the date of the AUM to be retrieved.
        /// </param>
        public double GetSODAUM(DateTime date, TimeSeriesType ttype)
        {
            double aum_value = this.GetAUM(date.Date, TimeSeriesType.Last);

            double aum_chg = this.GetAggregegatedAUMChanges(date.Date, date, TimeSeriesType.Last);
            aum_chg = double.IsNaN(aum_chg) || double.IsInfinity(aum_chg) ? 0 : aum_chg;

            double chg = this.GetOrderAUMChange(date, ttype);
            chg = (double.IsNaN(chg) || double.IsInfinity(chg) ? 0 : chg);

            return Math.Max(0.0, aum_value += aum_chg + chg);
        }


        /// <summary>
        /// Function: Clear the Strategy AUM memory on the next trading date in relation to a specific date.
        /// </summary>       
        /// <param name="date">DateTime valued date 
        /// </param>
        public void ClearNextAUMMemory(DateTime date)
        {
            ClearAUMMemory(date);
        }

        /// <summary>
        /// Function: Update the AUM of the strategy.
        /// </summary>       
        /// <param name="orderDate">DateTime valued date 
        /// </param>
        /// <param name="aumValue">double valued AUM to be updated
        /// </param>        
        public virtual void UpdateAUMOrder(DateTime orderDate, double aumValue)
        {
            if (this.Portfolio != null)
            {
                double oldAUM = this.GetSODAUM(orderDate, TimeSeriesType.Last);

                if (double.IsNaN(oldAUM))
                    oldAUM = 0;

                double chgAUM = aumValue - oldAUM;

                //if (Math.Abs(chgAUM) < Portfolio._tolerance)
                //    return;

                this.Portfolio.UpdateNotionalOrder(orderDate, aumValue, TimeSeriesType.Last);

                Dictionary<int, Dictionary<string, Order>> orders = this.Portfolio.OpenOrders(orderDate, false);
                List<Position> pos = this.Portfolio.RiskPositions(orderDate, false);

                List<Order> res = new List<Order>();

                if (orders != null)
                    foreach (int orderKeys in orders.Keys.ToList())
                        foreach (string key in orders[orderKeys].Keys)
                        {
                            Order order = orders[orderKeys][key];
                            if (order.Instrument.InstrumentType == Kernel.InstrumentType.Strategy)
                                res.Add(order);
                            else if (order.Unit != 0)
                                res.Add(order);
                        }

                if ((res == null || (res != null && res.Count == 0)) && !(pos == null || (pos != null && pos.Count == 0)))
                    return;


                this.AddMemoryPoint(orderDate, chgAUM, _aum_ord_chg_id_do_not_use, (int)TimeSeriesType.Last);
            }
            else
            {
                double oldAUM = this.GetSODAUM(orderDate, TimeSeriesType.Last);
                if (double.IsNaN(oldAUM))
                    oldAUM = 0;

                double chgAUM = aumValue - oldAUM;

                //if (Math.Abs(chgAUM) < Portfolio._tolerance)
                //    return;

                this.AddMemoryPoint(orderDate, chgAUM, _aum_ord_chg_id_do_not_use, (int)TimeSeriesType.Last);
            }
        }

        /// <summary>
        /// Function: Update the AUM of the strategy.
        /// </summary>       
        /// <param name="orderDate">DateTime valued date 
        /// </param>
        /// <param name="aumValue">double valued AUM to be updated
        /// </param>
        /// <param name="UpdatePortfolio">If true --> generate positions proportionate to the AUM change.
        /// Otherwise only update AUM value.
        /// </param>
        public virtual void UpdateAUM(DateTime date, double aumValue, bool UpdatePortfolio)
        {
            aumValue = double.IsNaN(aumValue) ? 0.0 : aumValue;

            if (this.Portfolio != null && UpdatePortfolio && this.Portfolio.MasterPortfolio.Residual != this)
                this.Portfolio.UpdateNotional(date, aumValue);

            // This needs to be after the UpdateNotional since the position update needs to be based on the previous "next update"
            this.AddMemoryPoint(date, aumValue, _aum_id_do_not_use, (int)TimeSeriesType.Last);
            //this.AddMemoryPoint(date, 0, Strategy._aum_chg_id_do_not_use, (int)TimeSeriesType.Last);//HERE
        }


        /// <summary>
        /// Function: Set the direction type of the strategy
        /// </summary>       
        /// <param name="date">DateTime valued date 
        /// </param>
        /// <param name="sign">Long or Short
        /// </param>        
        public virtual void Direction(DateTime date, DirectionType sign, bool updateOrders = false)
        {
            int sign_old = (int)this[date, _direction_id_do_not_use, TimeSeriesRollType.Last];

            if ((int)sign == sign_old)
                return;
            this.AddMemoryPoint(date, (double)sign, _direction_id_do_not_use);

            if (updateOrders)
            {
                var instruments = this.Instruments(date, false);
                if (instruments != null)
                    foreach (var instrument in instruments.Values)  //generate orders
                    {
                        var position = this.Portfolio.FindPosition(instrument, date);
                        var orders = this.Portfolio.FindOpenOrder(instrument, date, false);
                        var order = orders == null || orders.Count == 0 ? null : orders.Values.Where(o => o.Type == OrderType.Market).First();// |> List.reduce (fun acc o -> o) 

                        if (order != null) // if order exists then update
                            //order.UpdateTargetMarketOrder(date, (int)sign * order.Unit, UpdateType.OverrideUnits);
                            this.Portfolio.CreateTargetMarketOrder(instrument, date, (int)sign * order.Unit);

                        else if (position != null) // if position exists then update
                            //position.UpdateTargetMarketOrder(date, (int)sign * position.Unit, UpdateType.OverrideUnits);
                            this.Portfolio.CreateTargetMarketOrder(instrument, date, (int)sign * position.Unit);
                    }
            }
        }

        /// <summary>
        /// Function: Get the direction type of the strategy
        /// </summary>       
        /// <param name="date">DateTime valued date 
        /// </param>        
        public virtual DirectionType Direction(DateTime date)
        {
            int sign_old = (int)this[date, _direction_id_do_not_use, TimeSeriesRollType.Last];
            if (sign_old == -1)
                return DirectionType.Short;
            else
                return DirectionType.Long;
        }


        private Boolean _initialized = false;
        private object _monitor = new object();
        protected object monitor { get { return _monitor; } }

        /// <summary>
        /// Property: True if Strategy has been initialized during runtime.
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public Boolean Initialized
        {
            get
            {
                return _initialized;
            }
            private set
            {
                _initialized = value;
            }
        }

        /// <summary>
        /// Function: Initialize the strategy during runtime.
        /// </summary>       
        public virtual void Initialize()
        {
            if (Initialized)
                return;
            Initialized = true;
            lock (monitor)
            {
                this.GetMemorySeriesIds();

                if (Portfolio != null && Portfolio.Reserves != null)
                    foreach (Instrument ins in Portfolio.Reserves)
                        if (ins.InstrumentType == AQILabs.Kernel.InstrumentType.Strategy)
                            this.Tree.AddSubStrategy((Strategy)ins);



                Dictionary<int, Instrument> instruments = this.Instruments(DateTime.Now, false);

                foreach (Instrument instrument in instruments.Values)
                {
                    if (instrument.InstrumentType == AQILabs.Kernel.InstrumentType.Strategy)
                    {

                        Strategy strategy = Strategy.FindStrategy(instrument);
                        if (strategy != null)
                            this.Tree.AddSubStrategy(strategy);
                    }
                }
            }
        }

        /// <summary>
        /// Delegate: Skeleton for a delegate function to create a custom ExecuteLogic procedure
        /// </summary>       
        /// <param name="strategy">reference strategy
        /// </param>
        /// <param name="context">Context containing relevant environment information for the logic execution.
        /// </param>
        public delegate void ExecuteLogicType(Strategy strategy, ExecutionContext context, bool force);
        public ExecuteLogicType ExecuteLogicFunction = null;

        /// <summary>
        /// Function: Virtual function implemented by the Strategy developer.
        /// </summary>
        /// <param name="context">context of Order Generation Calculation.
        /// </param>
        public virtual void ExecuteLogic(ExecutionContext context, bool force)
        {
            if (ExecuteLogicFunction != null)
                ExecuteLogicFunction(this, context, force);
        }

        /// <summary>
        /// Function: Abstract function implemented by the Strategy developer
        /// returning a string array of the Memory type names.
        /// </summary>
        public virtual string[] MemoryTypeNames()
        {
            return null;
        }

        /// <summary>
        /// Function: Abstract function implemented by the Strategy developer
        /// returning an integer linked to the Memory name.
        /// </summary>
        public virtual int MemoryTypeInt(string name)
        {
            return int.MinValue;
        }

        /// <summary>
        /// Delegate: Skeleton for a delegate function to create a custom Interest Rate Compounding procedure
        /// </summary>       
        /// <param name="strategy">reference strategy
        /// </param>
        /// <param name="date">date to calculate the interest rate
        /// </param>
        public delegate void RateCompoundingType(Strategy strategy, BusinessDay date);
        public RateCompoundingType RateCompounding = null;


        static readonly object objLock = new object();
        /// <summary>
        /// Function: virtual function that calculates the NAV of the Strategy.
        /// </summary>
        /// <remarks>
        /// This function is called by default unless the Strategy developer
        /// overrides it for custom functionality.
        /// </remarks>
        public virtual double NAVCalculation(BusinessDay date)
        {
            ///////////////////////////////////////////////////
            // Index Calculation
            /////////////////////////////////////////////////// 
            double portvalue_mid = Portfolio == null ? 0 : Portfolio[date.DateTime, TimeSeriesType.Last, DataProvider.DefaultProvider, TimeSeriesRollType.Last];
            if (double.IsNaN(portvalue_mid))
                portvalue_mid = 0;

            //double rate = 0.0;
            //if (RateCompounding != null)
            //    RateCompounding(this, date);
            RateCompounding?.Invoke(this, date);

            double index_t_1 = this[date.AddMilliseconds(-1).DateTime, TimeSeriesType.Last, DataProvider.DefaultProvider, TimeSeriesRollType.Last];
            double aum_value = this.GetAUM(date.DateTime, TimeSeriesType.Last);
            if (double.IsNaN(index_t_1))
                index_t_1 = this[date.DateTime, TimeSeriesType.Last, DataProvider.DefaultProvider, TimeSeriesRollType.Last];

            if (this.FundingType == FundingType.ExcessReturn)
                portvalue_mid += aum_value;



            double portfolio_return = portvalue_mid - aum_value;// +rate;

            double index_t = portfolio_return + index_t_1;

            //if(true)//this.Portfolio.ParentPortfolio != null)
            //{
            //    var ts = this.GetTimeSeries(TimeSeriesType.Last);
            //    DateTime lastTime = ts.DateTimes[ts.Count - 1];
            //    double current_aum_agg = this.GetAggregegatedAUMChanges(lastTime, date.DateTime, TimeSeriesType.Last);
            //    double current_aum = this.GetAUMChange(date.DateTime, TimeSeriesType.Last);
                
            //    Console.WriteLine("+++++++++++++++" + this.Name + " " + index_t + " " + index_t_1 + " " + portfolio_return + " " + aum_value);
            //    //index_t += current_aum_agg;

            //}
            ///////////////////////////////////////////////////

            if (Portfolio.DebugPositions && this.Portfolio.ParentPortfolio == null)
                if (Math.Abs(index_t / index_t_1 - 1) > 0.1)
                {
                    Console.WriteLine("Strategy Error: " + DateTime.Now.ToString("hh:mm:ss.fff"));
                    Console.WriteLine("Name: " + this.Name);
                    Console.WriteLine("index_t: " + index_t);
                    Console.WriteLine("portvalue_mid_t: " + portvalue_mid);
                    Console.WriteLine("aum_value_t: " + aum_value);
                    foreach (var p in this.Portfolio.Positions(date.DateTime, true))
                        Console.WriteLine(p.Instrument.Name + " " + p.NotionalValue(date.DateTime) + " " + p.Unit + " " + p.Instrument[date.DateTime, TimeSeriesType.Last, TimeSeriesRollType.Last]);

                    Console.WriteLine("index_t_1: " + index_t_1);
                    Console.WriteLine("portvalue_mid_t_1: " + Portfolio[date.AddMilliseconds(-1).DateTime, TimeSeriesType.Last, DataProvider.DefaultProvider, TimeSeriesRollType.Last]);
                    Console.WriteLine("aum_value_t_1: " + this.GetAUM(date.AddMilliseconds(-1).DateTime, TimeSeriesType.Last));


                    foreach (var p in this.Portfolio.Positions(date.AddMilliseconds(-1).DateTime, true))
                        Console.WriteLine(p.Instrument.Name + " " + p.NotionalValue(date.AddMilliseconds(-1).DateTime));

                    throw new Exception();
                }

            // Store Portfolio Value prior to rebalancing to today
            CommitNAVCalculation(date, index_t, TimeSeriesType.Last);
            UpdateAUM(date.DateTime, portvalue_mid, false);

            return portvalue_mid;
        }

        /// <summary>
        /// Function: function that quickly calculates the NAV of the Strategy.
        /// for distribution of subscribed clients
        /// </summary>        
        public double QuickNAVShare(BusinessDay date)
        {
            ///////////////////////////////////////////////////
            // Index Calculation
            /////////////////////////////////////////////////// 
            double portvalue_mid = Portfolio == null ? 0 : Portfolio[date.DateTime, TimeSeriesType.Last, DataProvider.DefaultProvider, TimeSeriesRollType.Last];
            if (double.IsNaN(portvalue_mid))
                portvalue_mid = 0;

            
            RateCompounding?.Invoke(this, date);

            double index_t_1 = this[date.AddMilliseconds(-1).DateTime, TimeSeriesType.Last, DataProvider.DefaultProvider, TimeSeriesRollType.Last];
            double aum_value = this.GetAUM(date.DateTime, TimeSeriesType.Last);
            if (double.IsNaN(index_t_1))
                index_t_1 = this[date.DateTime, TimeSeriesType.Last, DataProvider.DefaultProvider, TimeSeriesRollType.Last];

            if (this.FundingType == FundingType.ExcessReturn)
                portvalue_mid += aum_value;



            double portfolio_return = portvalue_mid - aum_value;// +rate;

            var ts = this.GetTimeSeries(TimeSeriesType.Last);
            double index_t = portfolio_return + index_t_1 - ts[0];

            if (!this.SimulationObject && this.Cloud && Math.Abs(index_t - index_t_1) > Portfolio._tolerance)
                if (RTDEngine.Publish(this))
                    RTDEngine.Send(new RTDMessage() { Type = RTDMessage.MessageType.MarketData, Content = new RTDMessage.MarketData() { InstrumentID = this.ID, Value = index_t, Type = TimeSeriesType.Last, Timestamp = date.DateTime } });

            return index_t;
        }


        /// <summary>
        /// Function: virtual function called by the Execution Tree after the Order Generation function
        /// has been called for each strategy in the tree.
        /// </summary>
        /// <remarks>
        /// This function is called by default unless the Strategy developer
        /// overrides it for custom functionality.
        /// </remarks>
        public virtual void PostExecuteLogic(BusinessDay orderDate)
        {
        }

        /// <summary>
        /// Function: protected function called when commiting a NAV value.
        /// </summary>
        /// <remarks>
        /// Only called by Kernel or in the custom NAV calculation function.
        /// </remarks>
        protected void CommitNAVCalculation(BusinessDay date, double value, TimeSeriesType type)
        {
            if (type == TimeSeriesType.High || type == TimeSeriesType.Low)
                throw new Exception("Strategy High or Low is not implemented");

            AddTimeSeriesPoint(date.DateTime, value, type, DataProvider.DefaultProvider);
        }

        /// <summary>
        /// Function: Startup function called once during the creation of the strategy.
        /// If the strategy is persistently stored, this should only be called at creation.
        /// </summary>        
        public virtual void Startup(BusinessDay initialDate, double initialValue, Portfolio portfolio)
        {
            if (!SimulationObject)
                Factory.Startup(this.ID, this.GetType().ToString());

            if (portfolio != null)
            {
                Portfolio = portfolio;
                Portfolio.Strategy = this;
            }
            CommitNAVCalculation(initialDate, initialValue, TimeSeriesType.Last);

            ///////////////////////////////////            

            if (portfolio != null)
            {
                foreach (Instrument ins in Portfolio.Reserves)
                {
                    if (ins.InstrumentType == Kernel.InstrumentType.Strategy)
                    {
                        ((Strategy)ins).Initialize();
                        this.Tree.AddSubStrategy((Strategy)ins);
                    }
                }

                double valPre = portfolio[initialDate.DateTime];
                Portfolio.UpdateReservePosition(initialDate.DateTime, initialValue - valPre, Currency);
                this.UpdateAUM(initialDate.DateTime, initialValue, true);

                double valPost = portfolio[initialDate.DateTime];
                CommitNAVCalculation(initialDate, Math.Abs(valPost), TimeSeriesType.Last);


                this.UpdateAUM(initialDate.DateTime, valPost, true);
            }

            Initialize();

            this.AddRemoveSubStrategies(initialDate);
        }

        ///// <summary>
        ///// Function: Find strategy by name in both memory and persistent storage
        ///// </summary>       
        ///// <param name="name">string valued strategy name.
        ///// </param>
        //public static Strategy FindStrategy(string name)
        //{
        //    return Factory.FindStrategy(name);
        //}

        /// <summary>
        /// Delegate: Skeleton for a delegate function to customely create an instance of a strategy of a specific class linked to a base instrument.
        /// </summary>       
        /// <param name="instrument">Instrument valued base instrument.
        /// </param>
        /// <param name="classname">string valued name of the Strategy class implemention.
        /// </param>
        /// <param name="portfolioID">integer valued ID of the strategy's portfolio.
        /// </param>
        /// <param name="initialDate">DateTime value of the initial date.
        /// </param>
        /// <param name="dbConnection">string value of the connection address to the persistent storage.
        /// </param>
        public delegate Strategy LoadStrategyEvent(Instrument instrument, string classname, int portfolioID, DateTime initialDate, string dbConnection, string scheduler);
        public static event LoadStrategyEvent StrategyLoader;
        private static Dictionary<string, Type> _types = new Dictionary<string, Type>();

        /// <summary>
        /// Function: Create an instance of a strategy of a specific class linked to a base instrument.
        /// </summary>       
        /// <param name="instrument">Instrument valued base instrument.
        /// </param>
        /// <param name="classname">string valued name of the Strategy class implemention.
        /// </param>
        /// <param name="portfolioID">integer valued ID of the strategy's portfolio.
        /// </param>
        /// <param name="initialDate">DateTime value of the initial date.
        /// </param>
        /// <param name="dbConnection">string value of the connection address to the persistent storage.
        /// </param>
        public static Strategy LoadStrategy(Instrument instrument, string classname, int portfolioID, DateTime initialDate, string dbConnection, string scheduler)
        {
            if (StrategyLoader == null)
            {
                Type type = null;

                if (_types.ContainsKey(classname))
                    type = _types[classname];
                else
                {
                    string tempClassNane = classname;
                    while (tempClassNane.Contains("."))
                    {
                        try
                        {

                            //string assemblyname = classname.Contains("+") ? classname.Substring(0, classname.LastIndexOf(".")) : classname.Substring(0, classname.LastIndexOf("."));
                            string assemblyname = tempClassNane.Substring(0, tempClassNane.LastIndexOf("."));
                            Assembly assembly = Assembly.Load(assemblyname);
                            type = assembly.GetType(tempClassNane);
                            break;
                        }
                        catch
                        {
                            tempClassNane = tempClassNane.Substring(0, tempClassNane.LastIndexOf("."));
                        }
                    }

                    if (type == null)
                        type = typeof(Strategy);

                    _types.Add(classname, type);
                }

                Strategy strategy = null;

                try
                {
                    strategy = (Strategy)Activator.CreateInstance(type, new Object[] { instrument });
                }
                catch
                {
                    strategy = (Strategy)Activator.CreateInstance(typeof(Strategy), new Object[] { instrument });
                }

                strategy.PortfolioID = portfolioID;
                strategy.InitialDateMemory = initialDate;
                strategy.DBConnectionMemory = dbConnection;
                strategy.ScheduleCommandMemory = scheduler;

                return strategy;

            }
            else
                return (Strategy)StrategyLoader(instrument, classname, portfolioID, initialDate, dbConnection, scheduler);
        }

        public delegate Dictionary<int[], TimeSeries> CloneFilter(Dictionary<int[], TimeSeries> memory);
        [Newtonsoft.Json.JsonIgnore]
        public CloneFilter CloneFilterFunction { get; set; }

        /// <summary>
        /// Function: Create a clone of this strategy.
        /// </summary>        
        /// <param name="portfolioClone">Clone of the base strategy's portfolio</param>
        /// <param name="initialDate">Clone's initialDate</param>
        /// <param name="refDate">Clone's refDate</param>
        /// <param name="cloned">internal table of previously cloned base ids and respective cloned strategies</param>
        /// <param name="simulated">true if the strategy is simulated and not persistent</param>
        internal Strategy Clone(Portfolio portfolioClone, DateTime initialDate, DateTime refDate, Dictionary<int, Strategy> cloned, bool simulated)
        {
            string assemblyname = Class.Substring(0, Class.LastIndexOf("."));
            Strategy clone = LoadStrategy(this.Clone(simulated) as Instrument, Class, portfolioClone != null ? portfolioClone.ID : -1, initialDate, DBConnection, ScheduleCommand);

            if (portfolioClone != null)
                portfolioClone.Strategy = clone;

            Dictionary<int[], TimeSeries> memory = GetMemorySeries();
            if (CloneFilterFunction != null)
                memory = CloneFilterFunction(memory);

            Dictionary<int[], TimeSeries> memory_new = new Dictionary<int[], TimeSeries>();

            foreach (int[] key in memory.Keys)
            {
                if (key[0] != _aum_ord_chg_id_do_not_use && key[1] != _aum_chg_id_do_not_use)
                {
                    TimeSeries ts = memory[key];
                    if (ts.Count > 0)
                    {
                        TimeSeries net = new TimeSeries();
                        double val = ts[refDate, TimeSeries.DateSearchType.Previous];
                        net.AddDataPoint(initialDate, val);
                        memory_new.Add(key, net);
                    }
                }
            }

            foreach (int[] key in memory_new.Keys)
            {
                for (int i = 0; i < memory_new[key].Count; i++)
                {
                    double v = memory_new[key][i];
                    if (cloned.Keys.Contains((int)v))
                        memory_new[key][i] = cloned[(int)v].ID;
                }

                int key0 = cloned.Keys.Contains(key[0]) ? cloned[key[0]].ID : key[0];
                int key1 = cloned.Keys.Contains(key[1]) ? cloned[key[1]].ID : key[1];

                Factory.AddMemorySeries(clone, key0, key1, memory_new[key]);
            }

            if (this.Portfolio == null)
                clone.Startup(Calendar.GetClosestBusinessDay(initialDate, TimeSeries.DateSearchType.Previous), this.GetTimeSeries(TimeSeriesType.Last).Values[0], portfolioClone);

            clone.Clone(this);

            return clone;
        }

        /// <summary>
        /// Function: Virtual function implemented by the Strategy developer.
        /// </summary>
        /// <param name="context">context of Order Generation Calculation.
        /// </param>
        public virtual void Clone(Strategy original)
        {

        }


        /// <summary>
        /// Function: Find strategy by base instrument in both memory and persistent storage
        /// </summary>       
        /// <param name="instrument">Instrument valued base instrument.
        /// </param>
        public static Strategy FindStrategy(Instrument instrument)
        {
            if (instrument is Strategy)
                return instrument as Strategy;

            return Factory.FindStrategy(instrument);
        }

        /// <summary>
        /// Function: Generate the order context for a specific order date.
        /// </summary>
        /// <param name="orderDate">BusinessDay valued date.
        /// </param>
        public ExecutionContext ExecutionContext(BusinessDay orderDate)
        {
            // Add Active Strategies
            foreach (Strategy s in Tree.SubStrategies)
            {
                if (!Portfolio.IsReserve(s))
                {
                    Position position = Portfolio.FindPosition(s, orderDate.DateTime);

                    if (position != null && !ActiveStrategies.ContainsKey(s.ID))
                        ActiveStrategies.TryAdd(s.ID, s);
                }
            }

            Strategy reference = this;

            double aum_value = reference.GetSODAUM(orderDate.DateTime, TimeSeriesType.Last);
            return new ExecutionContext(orderDate, Math.Max(0.0, aum_value));
        }

        [Newtonsoft.Json.JsonIgnore]
        public ConcurrentDictionary<int, Strategy> ActiveStrategies = new ConcurrentDictionary<int, Strategy>();

        /// <summary>
        /// Function: Add a new a strategy that is not in the execution tree if the new strategy's start date allows it.
        /// Or remove a strategy in the execution tree that has expired according to it's final date.
        /// </summary>
        /// <remarks>
        /// This function will create positions in this strategy's portfolio when relevant and aggregate
        /// the new strategy's positions to this strategy's portfolio.
        /// When removing the strategy, this function will remove aggregated positions linked to the removed strategy
        /// from this strategy's portfolio.
        /// </remarks>
        /// <param name="date">BusinessDay valued date.
        /// </param>
        public virtual void AddRemoveSubStrategies(BusinessDay date)
        {
            //Console.WriteLine("Add Strategy: " + this + " " + date.DateTime);

            Dictionary<int, Instrument> instruments = this.Instruments(date.DateTime, false);
            if (instruments != null)
                foreach (Instrument instrument in instruments.Values.ToList())
                {
                    if (instrument.InstrumentType == Kernel.InstrumentType.Strategy)
                    {
                        Strategy strategy = instrument as Strategy;
                        if (strategy != null && !this.Tree.ContainsStrategy(strategy))
                            this.Tree.AddSubStrategy(strategy);
                    }
                }

            if (Portfolio != null)
                foreach (Strategy s in Tree.SubStrategies.ToList())
                    //this.AddRemoveSubStrategies(s, date);                    
                    this.AddInstrument(s, date.DateTime);

        }

        /// <summary>
        /// Function: virtual function implemented by strategy developers to manage a local strategy investment universe.
        /// </summary>
        /// <param name="strategy">Strategy to be added or removed.
        /// </param>
        /// <param name="date">BusinessDay valued date.
        /// </param>
        public virtual void AddRemoveSubStrategies(Strategy strategy, BusinessDay date)
        {
            if (!Portfolio.IsReserve(strategy) && strategy.Portfolio != null)
            {

                Position position = Portfolio.FindPosition(strategy, date.DateTime);
                if (position == null)
                {
                    double aum = strategy.GetSODAUM(date.DateTime, TimeSeriesType.Last);
                    if (strategy.Portfolio == null)
                        aum = strategy[date.DateTime, TimeSeriesType.Last, DataProvider.DefaultProvider, TimeSeriesRollType.Last];
                    if (aum > 0.0 && date.DateTime >= strategy.InitialDate && date.DateTime < strategy.FinalDate)
                    {
                        ActiveStrategies.TryAdd(strategy.ID, strategy);
                        strategy.Initialize();

                        double value = (strategy.Portfolio != null ? strategy.GetAUM(date.DateTime, TimeSeriesType.Last) : strategy[date.DateTime, TimeSeriesType.Last, DataProvider.DefaultProvider, TimeSeriesRollType.Last]);

                        Portfolio.CreatePosition(strategy, date.DateTime, 1.0, value);

                        SystemLog.Write("ADD: " + strategy + " " + this + " AUM: " + aum + " " + Portfolio[date.DateTime] + " " + date.DateTime);

                        if (strategy.Portfolio != null)
                        {
                            strategy.Portfolio.MarginFutures(date.DateTime);
                            strategy.Portfolio.HedgeFX(date.DateTime, 0);
                        }
                    }
                }
                else
                {
                    if (!ActiveStrategies.ContainsKey(strategy.ID))
                        ActiveStrategies.TryAdd(strategy.ID, strategy);

                    if (date.DateTime >= strategy.FinalDate)
                    {
                        Strategy outrem = null;
                        ActiveStrategies.TryRemove(strategy.ID, out outrem);

                        double value = (strategy.Portfolio != null ? strategy.Portfolio[date.DateTime, TimeSeriesType.Last, DataProvider.DefaultProvider, TimeSeriesRollType.Last] : strategy[date.DateTime, TimeSeriesType.Last, DataProvider.DefaultProvider, TimeSeriesRollType.Last]);

                        if (strategy.Portfolio != null)
                        {
                            strategy.Portfolio.MarginFutures(date.DateTime);

                            List<Position> positions = strategy.Portfolio.Positions(date.DateTime);
                            if (positions != null)
                                foreach (Position p in positions)
                                    if (!strategy.Portfolio.IsReserve(p.Instrument))
                                        p.UpdatePosition(date.DateTime, 0, p.Instrument[date.DateTime, TimeSeriesType.Last, TimeSeriesRollType.Last], RebalancingType.Reserve, UpdateType.OverrideNotional);
                            strategy.Portfolio.HedgeFX(date.DateTime, 0);
                        }

                        Portfolio.UpdateReservePosition(date.DateTime, position.Value(date.DateTime), Currency);
                        position.UpdatePosition(date.DateTime, 0, double.NaN, RebalancingType.Reserve, UpdateType.OverrideNotional);
                        SystemLog.Write("Remove Strategy: " + strategy + " " + date.DateTime.ToShortDateString() + " " + value);
                    }
                }
            }
        }

        /// <summary>
        /// Function: Retrieve a list of instruments from both memory and persistent storage.
        /// </summary>
        /// <param name="type">Type of instrument to be retrieved.
        /// </param>         
        public static List<Strategy> ActiveMasters(DateTime date)
        {
            return Factory.ActiveMasters(User.CurrentUser, date);
        }

        /// <summary>
        /// Function: Aggregate PnL Values of multiple strategies
        /// </summary>
        /// <param name="strategies">List of strategies
        /// </param>
        public static TimeSeries AggregatedPnL(IEnumerable<Strategy> strategies, TimeSeriesType ttype)
        {
            var strats = new List<Strategy>();
            var dates = new Dictionary<DateTime, string>();


            foreach (var strategy in strategies)
                if (strategy != null)
                {
                    strategy.Tree.Initialize();
                    strategy.Tree.LoadPortfolioMemory();

                    strats.Add(strategy);

                    var ts = strategy.GetTimeSeries(ttype);
                    for (int i = 0; i < ts.Count; i++)
                        if (!dates.ContainsKey(ts.DateTimes[i]))
                            dates.Add(ts.DateTimes[i], "");
                }

            var dates_sorted = dates.Keys.OrderBy(x => x).ToList();
            var aggregated_ts = new TimeSeries(dates_sorted.Count, new DateTimeList(dates_sorted));
            foreach (var strategy in strategies)
            {
                var ts = strategy.GetTimeSeries(ttype);
                var firstDate = ts.DateTimes[0];

                for (int i = 0; i < aggregated_ts.Count; i++)
                    if (firstDate <= aggregated_ts.DateTimes[i])
                    {
                        if (Double.IsNaN(aggregated_ts[i]))
                            aggregated_ts[i] = ts[aggregated_ts.DateTimes[i], TimeSeries.DateSearchType.Previous] - ts[0];
                        else
                            aggregated_ts[i] += ts[aggregated_ts.DateTimes[i], TimeSeries.DateSearchType.Previous] - ts[0];
                    }
            }

            return aggregated_ts;
        }

        /// <summary>
        /// Function: Create a strategy
        /// </summary>    
        /// <param name="name">Name
        /// </param>
        /// <param name="initialDay">Creating date
        /// </param>
        /// <param name="initialValue">Starting NAV and portfolio AUM.
        /// </param>
        /// <param name="ccy">Base currency
        /// </param>
        /// <param name="simulated">True if not stored persistently.
        /// </param>
        public static Strategy CreateStrategy(string name, Currency ccy, BusinessDay initialDay, double initialValue, bool simulated, Portfolio parent = null)
        {
            Instrument instrument = Instrument.CreateInstrument(name, InstrumentType.Strategy, name, ccy, FundingType.TotalReturn, simulated);
            Strategy strategy = new Strategy(instrument);

            Instrument main_cash = Instrument.FindInstrument(ccy.Name + " - Cash");

            Instrument portfolio_instrument = Instrument.CreateInstrument(instrument.Name + "/Portfolio", InstrumentType.Portfolio, instrument.Name + "/Portfolio", ccy, FundingType.TotalReturn, instrument.SimulationObject);
            Portfolio portfolio = Portfolio.CreatePortfolio(portfolio_instrument, main_cash, main_cash, parent);
            portfolio.TimeSeriesRoll = TimeSeriesRollType.Last;

            portfolio.AddReserve(ccy, main_cash, main_cash);

            List<Currency> ccys = Currency.Currencies;
            foreach (Currency cy in ccys)
            {
                Instrument cash = Instrument.FindInstrument(cy.Name + " - Cash");
                if (cash != null)
                    portfolio.AddReserve(cy, cash, cash);
            }


            portfolio.Strategy = strategy;

            strategy.Startup(initialDay, initialValue, portfolio);
            strategy.InitialDate = initialDay.DateTime;

            if (!instrument.SimulationObject)
            {
                strategy.Portfolio.MasterPortfolio.Strategy.Tree.SaveNewPositions();
                strategy.Portfolio.MasterPortfolio.Strategy.Tree.Save();
            }

            return strategy;

        }
    }
}