(*
 * The MIT License (MIT)
 * Copyright (c) 2007-2019, Arturo Rodriguez All rights reserved.
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 *)

namespace AQI.AQILabs.SDK.Strategies

open System
open AQI.AQILabs.Kernel
open AQI.AQILabs.Kernel.Numerics.Util

open QuantApp.Kernel
open QuantApp.Engine

type PortfolioStrategyMeta =
    {
        ID : int
        Code : string
        ExposureFunctionName : string
        RiskFunctionName : string
        InformationRatioFunctionName : string 
        WeightFilterFunctionName : string 
        InstrumentFilterFunctionName : string
        IndicatorCalculationFunctionName : string
        RateCompoundingFunctionName : string
        AnalyseFunctionName : string
    }

/// <summary>
/// Class representing a strategy that optimises a portfolio in a multistep process:
/// 1) Target volatility for each underlying asset. This step sets an equal volatility weight to all assets.
/// 2) Concentration risk and Information Ratio management. This step mitigates the risk of an over exposure to a set of highly correlated assets. 
///    Furthermore, a tilt in the weight is also implemented based on the information ratios for each asset.
///    This entire step is achieved through the implementation of a Mean-Variance optimisation where all volatilities are equal to 1.0, the expected returns are normalised and transformed to information ratios.
/// 3) Target volatility for the entire portfolio. After steps 1 and 2, the portfolio will probably have a lower risk level than the target due to diversification.
///    This step adjusts the strategy's overall exposure in order to achieve the desired target volatility for the entire portfolio.
/// 4) Maximum individual exposure to each asset is implemented
/// 5) Maximum exposure to the entire portfolio is implemented
/// 6) Deleverage the portfolio if the portfolio's Value at Risk exceeds a given level. The exposure is changed linearly such that the new VaR given by the new weights is the limit VaR.
///    The implemented VaR measure is the UCITS calculation based on the 99 percentile of the distribution of the 20 day rolling return for the entire portfolio where each return is based on the current weights.
/// 7) Only rebalance if the notional exposure to a position changes by more than a given threshold.
/// The class also allows developers to specify a number of custom functions:
///     a) Risk: risk measure for each asset.
///     b) Exposure: defines if the portfolio should have a long (1.0) / short (-1.0) or neutral (0.0) exposure to a given asset.
///     c) InformationRatio: measure of risk-neutral expectation for each asset. This affectes the MV optimisation of the concentration risk management.
/// </summary>
type PortfolioStrategy = 
    inherit Strategy    
        
    val mutable private _exposureFunction : Exposure    
    val mutable private _riskFunction : Risk
    val mutable private _informationRatioFunction : InformationRatio 
    val mutable private _weightFilterFunction : WeightFilter 
    val mutable private _instrumentFilterFunction : InstrumentFilter 
    val mutable private _indicatorCalculationFunction : IndicatorCalculation 
    val mutable private _analyseFunction : Analyse

    val mutable private _scriptCode : string
    val mutable private _exposureFunctionName : string
    val mutable private _riskFunctionName : string
    val mutable private _informationRatioFunctionName : string 
    val mutable private _weightFilterFunctionName : string 
    val mutable private _instrumentFilterFunctionName : string 
    val mutable private _indicatorCalculationFunctionName : string
    val mutable private _rateCompoundingFunctionName : string
    val mutable private _analyseFunctionName : string

    val mutable private _initializing : bool

    //val mutable _initialized : bool

    /// <summary>
    /// Delegate function used to set the information ratio function
    /// </summary>
    member this.ScriptCode
        with get() : string = this._scriptCode
        and set(value : string) =
            Utils.RegisterCode (false, true) [this.Name.Substring(this.Name.LastIndexOf("/") + 1), value.Replace("Utils.SetFunction(","Utils.SetFunction(\"" + this.Portfolio.MasterPortfolio.Strategy.ID.ToString() + "-\" + ")] |> ignore
            this._scriptCode <- value

            if not(this.SimulationObject) && this.Initialized && not(this._initializing) then
                let m = M.Base(this.ID.ToString() + "-PortfolioStrategy-MetaData")

                let pkg = 
                    {
                        ID = this.ID 
                        Code = this._scriptCode
                        ExposureFunctionName = this._exposureFunctionName
                        RiskFunctionName = this._riskFunctionName
                        InformationRatioFunctionName = this._informationRatioFunctionName
                        WeightFilterFunctionName = this._weightFilterFunctionName
                        InstrumentFilterFunctionName = this._instrumentFilterFunctionName
                        IndicatorCalculationFunctionName = this._indicatorCalculationFunctionName
                        RateCompoundingFunctionName = this._rateCompoundingFunctionName
                        AnalyseFunctionName = this._analyseFunctionName
                    }

                let res = m.[fun x-> M.V<int>(x,"ID") = this.ID]
                if res.Count = 0 then
                    m.Add(pkg) |> ignore
                    
                else
                    m.Exchange(res.[0], pkg)
                m.Save()


    /// <summary>
    /// Delegate function used to set the information ratio function
    /// </summary>
    member this.ExposureFunctionName
        with get() : string = this._exposureFunctionName
        and set(value) =
            let _value = this.Portfolio.MasterPortfolio.Strategy.ID.ToString() + "-" + value
            if not(Utils.GetFunction(_value) = null) then
                this.ExposureFunction <- Utils.GetFunction(_value) :?> Exposure
            elif not(Utils.GetFunction(value) = null) then
                this.ExposureFunction <- Utils.GetFunction(value) :?> Exposure
            this._exposureFunctionName <- value
            SystemLog.Write(DateTime.Now, this, SystemLog.Type.Production,"PortfolioStrategy Set Exposure: " + value.ToString()) |> ignore
            
            if not(this.SimulationObject) && this.Initialized && not(this._initializing) then
                let m = M.Base(this.ID.ToString() + "-PortfolioStrategy-MetaData")

                let pkg = 
                    { 
                        ID = this.ID
                        Code = this._scriptCode
                        ExposureFunctionName = this._exposureFunctionName
                        RiskFunctionName = this._riskFunctionName
                        InformationRatioFunctionName = this._informationRatioFunctionName
                        WeightFilterFunctionName = this._weightFilterFunctionName
                        InstrumentFilterFunctionName = this._instrumentFilterFunctionName
                        IndicatorCalculationFunctionName = this._indicatorCalculationFunctionName
                        RateCompoundingFunctionName = this._rateCompoundingFunctionName
                        AnalyseFunctionName = this._analyseFunctionName
                    }


                let res = m.[fun x-> M.V<int>(x,"ID") = this.ID]
                if res.Count = 0 then
                    m.Add(pkg) |> ignore
                    
                else
                    m.Exchange(res.[0], pkg)
                m.Save()


    /// <summary>
    /// Delegate function used to set the information ratio function
    /// </summary>
    member this.RiskFunctionName
        with get() : string = this._riskFunctionName
        and set(value) =
            let _value = this.Portfolio.MasterPortfolio.Strategy.ID.ToString() + "-" + value
            if not(Utils.GetFunction(_value) = null) then
                this.RiskFunction <- Utils.GetFunction(_value) :?> Risk
            elif not(Utils.GetFunction(value) = null) then
                this.RiskFunction <- Utils.GetFunction(value) :?> Risk
            this._riskFunctionName <- value
            SystemLog.Write(DateTime.Now, this, SystemLog.Type.Production,"PortfolioStrategy Set Risk: " + value + " " + this.GetHashCode().ToString()) |> ignore
            
            if not(this.SimulationObject) && this.Initialized && not(this._initializing) then
                let m = M.Base(this.ID.ToString() + "-PortfolioStrategy-MetaData")

                let pkg = 
                    { 
                        ID = this.ID
                        Code = this._scriptCode
                        ExposureFunctionName = this._exposureFunctionName
                        RiskFunctionName = this._riskFunctionName
                        InformationRatioFunctionName = this._informationRatioFunctionName
                        WeightFilterFunctionName = this._weightFilterFunctionName
                        InstrumentFilterFunctionName = this._instrumentFilterFunctionName
                        IndicatorCalculationFunctionName = this._indicatorCalculationFunctionName
                        RateCompoundingFunctionName = this._rateCompoundingFunctionName
                        AnalyseFunctionName = this._analyseFunctionName
                    }

                let res = m.[fun x-> M.V<int>(x,"ID") = this.ID]
                if res.Count = 0 then
                    m.Add(pkg) |> ignore
                    
                else
                    m.Exchange(res.[0], pkg)
                m.Save()


    /// <summary>
    /// Delegate function used to set the information ratio function
    /// </summary>
    member this.InformationRatioFunctionName
        with get() : string = this._informationRatioFunctionName
        and set(value) =
            let _value = this.Portfolio.MasterPortfolio.Strategy.ID.ToString() + "-" + value
            if not(Utils.GetFunction(_value) = null) then
                this.InformationRatioFunction <- Utils.GetFunction(_value) :?> InformationRatio
            elif not(Utils.GetFunction(value) = null) then
                this.InformationRatioFunction <- Utils.GetFunction(value) :?> InformationRatio
            this._informationRatioFunctionName <- value
            
            SystemLog.Write(DateTime.Now, this, SystemLog.Type.Production,"PortfolioStrategy Set Information Ratio: " + value + " " + this.GetHashCode().ToString()) |> ignore

            

            if not(this.SimulationObject) && this.Initialized && not(this._initializing) then
                let m = M.Base(this.ID.ToString() + "-PortfolioStrategy-MetaData")

                let pkg = 
                    { 
                        ID = this.ID
                        Code = this._scriptCode
                        ExposureFunctionName = this._exposureFunctionName
                        RiskFunctionName = this._riskFunctionName
                        InformationRatioFunctionName = this._informationRatioFunctionName                   
                        WeightFilterFunctionName = this._weightFilterFunctionName
                        InstrumentFilterFunctionName = this._instrumentFilterFunctionName
                        IndicatorCalculationFunctionName = this._indicatorCalculationFunctionName
                        RateCompoundingFunctionName = this._rateCompoundingFunctionName
                        AnalyseFunctionName = this._analyseFunctionName
                    }

                let res = m.[fun x-> M.V<int>(x,"ID") = this.ID]
                if res.Count = 0 then
                    m.Add(pkg) |> ignore
                    
                else
                    m.Exchange(res.[0], pkg)
                m.Save()




    /// <summary>
    /// Delegate function used to set the information ratio function
    /// </summary>
    member this.WeightFilterFunctionName
        with get() : string = this._weightFilterFunctionName
        and set(value) =
            let _value = this.Portfolio.MasterPortfolio.Strategy.ID.ToString() + "-" + value
            if not(Utils.GetFunction(_value) = null) then
                this.WeightFilterFunction <- Utils.GetFunction(_value) :?> WeightFilter
            elif not(Utils.GetFunction(value) = null) then
                this.WeightFilterFunction <- Utils.GetFunction(value) :?> WeightFilter
            this._weightFilterFunctionName <- value
            SystemLog.Write(DateTime.Now, this, SystemLog.Type.Production,"PortfolioStrategy Set Weight Filter: " + value + " " + this.GetHashCode().ToString()) |> ignore
         

            if not(this.SimulationObject) && this.Initialized && not(this._initializing) then                
                let m = M.Base(this.ID.ToString() + "-PortfolioStrategy-MetaData")

                let pkg = 
                    { 
                        ID = this.ID
                        Code = this._scriptCode
                        ExposureFunctionName = this._exposureFunctionName
                        RiskFunctionName = this._riskFunctionName
                        InformationRatioFunctionName = this._informationRatioFunctionName
                        WeightFilterFunctionName = this._weightFilterFunctionName
                        InstrumentFilterFunctionName = this._instrumentFilterFunctionName
                        IndicatorCalculationFunctionName = this._indicatorCalculationFunctionName
                        RateCompoundingFunctionName = this._rateCompoundingFunctionName
                        AnalyseFunctionName = this._analyseFunctionName
                    }

                let res = m.[fun x-> M.V<int>(x,"ID") = this.ID]
                if res.Count = 0 then
                    m.Add(pkg) |> ignore
                    
                else
                    m.Exchange(res.[0], pkg)
                m.Save()


    /// <summary>
    /// Delegate function used to set the information ratio function
    /// </summary>
    member this.InstrumentFilterFunctionName
        with get() : string = this._instrumentFilterFunctionName
        and set(value) =
            let _value = this.Portfolio.MasterPortfolio.Strategy.ID.ToString() + "-" + value
            if not(Utils.GetFunction(_value) = null) then
                this.InstrumentFilterFunction <- Utils.GetFunction(_value) :?> InstrumentFilter
            elif not(Utils.GetFunction(value) = null) then
                this.InstrumentFilterFunction <- Utils.GetFunction(value) :?> InstrumentFilter
            this._instrumentFilterFunctionName <- value
            SystemLog.Write(DateTime.Now, this, SystemLog.Type.Production,"PortfolioStrategy Set Instrument Filter: " + value + " " + this.GetHashCode().ToString()) |> ignore
            

            if not(this.SimulationObject) && this.Initialized && not(this._initializing) then                

                let pkg = 
                    { 
                        ID = this.ID
                        Code = this._scriptCode
                        ExposureFunctionName = this._exposureFunctionName
                        RiskFunctionName = this._riskFunctionName
                        InformationRatioFunctionName = this._informationRatioFunctionName
                        WeightFilterFunctionName = this._weightFilterFunctionName
                        InstrumentFilterFunctionName = this._instrumentFilterFunctionName
                        IndicatorCalculationFunctionName = this._indicatorCalculationFunctionName
                        RateCompoundingFunctionName = this._rateCompoundingFunctionName
                        AnalyseFunctionName = this._analyseFunctionName
                    }

                let m = M.Base(this.ID.ToString() + "-PortfolioStrategy-MetaData")

                let res = m.[fun x-> M.V<int>(x,"ID") = this.ID]
                if res.Count = 0 then
                    m.Add(pkg) |> ignore
                    
                else
                    m.Exchange(res.[0], pkg)
                m.Save()


    /// <summary>
    /// Delegate function used to set the information ratio function
    /// </summary>
    member this.IndicatorCalculationFunctionName
        with get() : string = this._indicatorCalculationFunctionName
        and set(value) =
            let _value = this.Portfolio.MasterPortfolio.Strategy.ID.ToString() + "-" + value
            if not(Utils.GetFunction(_value) = null) then
                this.IndicatorCalculationFunction <- Utils.GetFunction(_value) :?> IndicatorCalculation
            elif not(Utils.GetFunction(value) = null) then
                this.IndicatorCalculationFunction <- Utils.GetFunction(value) :?> IndicatorCalculation
            this._indicatorCalculationFunctionName <- value
            SystemLog.Write(DateTime.Now, this, SystemLog.Type.Production,"PortfolioStrategy Set Indicator Calculation: " + value + " " + this.GetHashCode().ToString()) |> ignore
            

            if not(this.SimulationObject) && this.Initialized && not(this._initializing) then
                let m = M.Base(this.ID.ToString() + "-PortfolioStrategy-MetaData")

                let pkg = 
                    { 
                        ID = this.ID
                        Code = this._scriptCode
                        ExposureFunctionName = this._exposureFunctionName
                        RiskFunctionName = this._riskFunctionName
                        InformationRatioFunctionName = this._informationRatioFunctionName
                        WeightFilterFunctionName = this._weightFilterFunctionName
                        InstrumentFilterFunctionName = this._instrumentFilterFunctionName
                        IndicatorCalculationFunctionName = this._indicatorCalculationFunctionName
                        RateCompoundingFunctionName = this._rateCompoundingFunctionName
                        AnalyseFunctionName = this._analyseFunctionName
                    }

                
                let res = m.[fun x-> M.V<int>(x,"ID") = this.ID]
                if res.Count = 0 then
                    m.Add(pkg) |> ignore
                    
                else
                    m.Exchange(res.[0], pkg)
                        
                
                
                m.Save()

    /// <summary>
    /// Delegate function used to set the information ratio function
    /// </summary>
    member this.RateCompoundingFunctionName
        with get() : string = this._rateCompoundingFunctionName
        and set(value) =
            let _value = this.Portfolio.MasterPortfolio.Strategy.ID.ToString() + "-" + value
            if not(Utils.GetFunction(_value) = null) then
                this.RateCompounding <- Utils.GetFunction(_value) :?> Strategy.RateCompoundingType
            elif not(Utils.GetFunction(value) = null) then
                this.RateCompounding <- Utils.GetFunction(value) :?> Strategy.RateCompoundingType
            this._rateCompoundingFunctionName <- value
            SystemLog.Write(DateTime.Now, this, SystemLog.Type.Production,"PortfolioStrategy Set Rate Compounding: " + value + " " + this.GetHashCode().ToString()) |> ignore

            

            if not(this.SimulationObject) && this.Initialized && not(this._initializing) then

                let pkg = 
                    { 
                        ID = this.ID
                        Code = this._scriptCode
                        ExposureFunctionName = this._exposureFunctionName
                        RiskFunctionName = this._riskFunctionName
                        InformationRatioFunctionName = this._informationRatioFunctionName
                        WeightFilterFunctionName = this._weightFilterFunctionName
                        InstrumentFilterFunctionName = this._instrumentFilterFunctionName
                        IndicatorCalculationFunctionName = this._indicatorCalculationFunctionName
                        RateCompoundingFunctionName = this._rateCompoundingFunctionName
                        AnalyseFunctionName = this._analyseFunctionName
                    }

                let m = M.Base(this.ID.ToString() + "-PortfolioStrategy-MetaData")

                
                let res = m.[fun x-> M.V<int>(x,"ID") = this.ID]
                if res.Count = 0 then
                    m.Add(pkg) |> ignore
                    
                else
                    m.Exchange(res.[0], pkg)
                        
                
                
                m.Save()


    /// <summary>
    /// Delegate function used to set the information ratio function
    /// </summary>
    member this.AnalyseFunctionName
        with get() : string = this._analyseFunctionName
        and set(value) =
            let _value = this.Portfolio.MasterPortfolio.Strategy.ID.ToString() + "-" + value
            if not(Utils.GetFunction(_value) = null) then
                this.AnalyseFunction <- Utils.GetFunction(_value) :?> Analyse
            if not(Utils.GetFunction(value) = null) then
                this.AnalyseFunction <- Utils.GetFunction(value) :?> Analyse
            this._analyseFunctionName <- value
            SystemLog.Write(DateTime.Now, this, SystemLog.Type.Production,"PortfolioStrategy Set Analyse: " + value + " " + this.GetHashCode().ToString()) |> ignore

            

            if not(this.SimulationObject) && this.Initialized && not(this._initializing) then

                let pkg = 
                    { 
                        ID = this.ID
                        Code = this._scriptCode
                        ExposureFunctionName = this._exposureFunctionName
                        RiskFunctionName = this._riskFunctionName
                        InformationRatioFunctionName = this._informationRatioFunctionName
                        WeightFilterFunctionName = this._weightFilterFunctionName
                        InstrumentFilterFunctionName = this._instrumentFilterFunctionName
                        IndicatorCalculationFunctionName = this._indicatorCalculationFunctionName
                        RateCompoundingFunctionName = this._rateCompoundingFunctionName
                        AnalyseFunctionName = this._analyseFunctionName
                    }

                let m = M.Base(this.ID.ToString() + "-PortfolioStrategy-MetaData")

                
                let res = m.[fun x-> M.V<int>(x,"ID") = this.ID]
                if res.Count = 0 then
                    m.Add(pkg) |> ignore
                    
                else
                    m.Exchange(res.[0], pkg)
                        
                
                
                m.Save()

    /// <summary>
    /// Constructor
    /// </summary> 
    new(instrument : Instrument) = 
                                    { 
                                        inherit Strategy(instrument)
                                        _exposureFunction = Exposure(fun this orderDate instrument -> PortfolioStrategy.ExposureDefault(this :?> PortfolioStrategy, orderDate, instrument))
                                        _riskFunction = Risk(fun this orderDate timeSeries reference_aum -> PortfolioStrategy.RiskDefault(this :?> PortfolioStrategy, orderDate, timeSeries, reference_aum))                                 
                                        _informationRatioFunction = InformationRatio(fun this orderDate instrument current -> PortfolioStrategy.InformationRatioDefault(this :?> PortfolioStrategy, orderDate, instrument, current)) 
                                        _weightFilterFunction = null
                                        _instrumentFilterFunction = null
                                        _indicatorCalculationFunction = null
                                        _analyseFunction = null

                                        _scriptCode = null
                                        _exposureFunctionName = null
                                        _riskFunctionName = null
                                        _informationRatioFunctionName = null
                                        _weightFilterFunctionName = null
                                        _instrumentFilterFunctionName = null
                                        _indicatorCalculationFunctionName = null
                                        _rateCompoundingFunctionName = null
                                        _analyseFunctionName = 
                                            SystemLog.Write(DateTime.Now, null, SystemLog.Type.Production,instrument.ID.ToString() + "-PortfolioStrategy Initialise 1 " + base.GetHashCode().ToString()) |> ignore
                                            null                                        
                                        //_initialized = false

                                        _initializing = false
                                    }

    /// <summary>
    /// Constructor
    /// </summary> 
    new(instrument : Instrument, className : string) = 
                                    { 
                                        inherit Strategy(instrument, className)
                                        _exposureFunction = Exposure(fun this orderDate instrument -> PortfolioStrategy.ExposureDefault(this :?> PortfolioStrategy, orderDate, instrument))
                                        _riskFunction = Risk(fun this orderDate timeSeries reference_aum -> PortfolioStrategy.RiskDefault(this :?> PortfolioStrategy, orderDate, timeSeries, reference_aum))
                                        _informationRatioFunction = InformationRatio(fun this orderDate instrument current -> PortfolioStrategy.InformationRatioDefault(this :?> PortfolioStrategy, orderDate, instrument, current)) 
                                        _weightFilterFunction = null
                                        _instrumentFilterFunction = null
                                        _indicatorCalculationFunction = null
                                        _analyseFunction = null

                                        _scriptCode = null
                                        _exposureFunctionName = null
                                        _riskFunctionName = null
                                        _informationRatioFunctionName = null
                                        _weightFilterFunctionName = null
                                        _instrumentFilterFunctionName = null
                                        _indicatorCalculationFunctionName = null
                                        _rateCompoundingFunctionName = null
                                        _analyseFunctionName = 
                                            SystemLog.Write(DateTime.Now, null, SystemLog.Type.Production,instrument.ID.ToString() + "-PortfolioStrategy Initialise 2") |> ignore
                                            null
                                        //_initialized = false

                                        _initializing = false
                                    }

    /// <summary>
    /// Constructor
    /// </summary> 
    new(id : int) = 
                    { 
                        inherit Strategy(id)
                        _exposureFunction = Exposure(fun this orderDate instrument -> PortfolioStrategy.ExposureDefault(this :?> PortfolioStrategy, orderDate, instrument))
                        _riskFunction = Risk(fun this orderDate timeSeries reference_aum -> PortfolioStrategy.RiskDefault(this :?> PortfolioStrategy, orderDate, timeSeries, reference_aum))
                        _informationRatioFunction = InformationRatio(fun this orderDate instrument current -> PortfolioStrategy.InformationRatioDefault(this :?> PortfolioStrategy, orderDate, instrument, current)) 
                        _weightFilterFunction = null
                        _instrumentFilterFunction = null
                        _indicatorCalculationFunction = null
                        _analyseFunction = null

                        _scriptCode = null
                        _exposureFunctionName = null
                        _riskFunctionName = null
                        _informationRatioFunctionName = null
                        _weightFilterFunctionName = null
                        _instrumentFilterFunctionName = null
                        _indicatorCalculationFunctionName = null
                        _rateCompoundingFunctionName = null
                        _analyseFunctionName = 
                            SystemLog.Write(DateTime.Now, null, SystemLog.Type.Production,id.ToString() + "-PortfolioStrategy Initialise 3") |> ignore
                            null
                        //_initialized = false

                        _initializing = false
                    }
        
    /// <summary>
    /// Function: returns a list of names of used memory types.
    /// </summary>  
    override this.MemoryTypeNames() :  string[] = System.Enum.GetNames(typeof<MemoryType>)

    /// <summary>
    /// Function: returns a list of ids of used memory types.
    /// </summary>
    override this.MemoryTypeInt(name : string) = System.Enum.Parse(typeof<MemoryType> , name) :?> int

    /// <summary>
    /// Function: Initialize the strategy during runtime.
    /// </summary>
    override this.Initialize() =
        match this.Initialized with
        | true -> ()
        | _ -> 
            base.Initialize()
            lock (this.monitor) (fun () ->
                this._initializing <- true
                if not(this.SimulationObject) then
                    SystemLog.Write(DateTime.Now, this, SystemLog.Type.Production,"PortfolioStrategy Initialize Start. " + this.GetHashCode().ToString()) |> ignore
                    let m = M.Base(this.ID.ToString() + "-PortfolioStrategy-MetaData")
                    let res = m.[fun x-> M.V<int>(x,"ID") = this.ID]
                    if res.Count <> 0 then
                        try
                            let pkg = res.[0] :?> PortfolioStrategyMeta

                            if not(String.IsNullOrWhiteSpace(pkg.Code)) then                                
                                this.ScriptCode <- pkg.Code

                            if not(String.IsNullOrWhiteSpace(pkg.ExposureFunctionName)) then                                
                                this.ExposureFunctionName <- pkg.ExposureFunctionName

                            if not(String.IsNullOrWhiteSpace(pkg.IndicatorCalculationFunctionName)) then
                                this.IndicatorCalculationFunctionName <- pkg.IndicatorCalculationFunctionName

                            if not(String.IsNullOrWhiteSpace(pkg.InformationRatioFunctionName)) then                                
                                this.InformationRatioFunctionName <- pkg.InformationRatioFunctionName

                            if not(String.IsNullOrWhiteSpace(pkg.RiskFunctionName)) then                                
                                this.RiskFunctionName <- pkg.RiskFunctionName

                            if not(String.IsNullOrWhiteSpace(pkg.WeightFilterFunctionName)) then                                
                                this.WeightFilterFunctionName <- pkg.WeightFilterFunctionName

                            if not(String.IsNullOrWhiteSpace(pkg.InstrumentFilterFunctionName)) then                                
                                this.InstrumentFilterFunctionName <- pkg.InstrumentFilterFunctionName


                            if not(String.IsNullOrWhiteSpace(pkg.RateCompoundingFunctionName)) then                                
                                this.RateCompoundingFunctionName <- pkg.RateCompoundingFunctionName

                            if not(String.IsNullOrWhiteSpace(pkg.AnalyseFunctionName)) then                                
                                this.AnalyseFunctionName <- pkg.AnalyseFunctionName

                            SystemLog.Write(DateTime.Now, this, SystemLog.Type.Production,"PortfolioStrategy Initialize Done. " + this.GetHashCode().ToString()) |> ignore
                        with
                        | ex -> Console.WriteLine(ex.ToString()) |> ignore//Console.WriteLine(ex)                

                    else
                        SystemLog.Write(DateTime.Now, this, SystemLog.Type.Production,"PortfolioStrategy Initialize No Meta Data found.") |> ignore

                this._initializing <- false
                ) 
            
                                               
    override this.Clone(original : Strategy) =
        try
            let pkg = original :?> PortfolioStrategy

            if not(String.IsNullOrWhiteSpace(pkg.ScriptCode)) then
                this.ScriptCode <- pkg.ScriptCode

            if not(String.IsNullOrWhiteSpace(pkg.ExposureFunctionName)) then
                this.ExposureFunctionName <- pkg.ExposureFunctionName

            if not(String.IsNullOrWhiteSpace(pkg.IndicatorCalculationFunctionName)) then
                this.IndicatorCalculationFunctionName <- pkg.IndicatorCalculationFunctionName

            if not(String.IsNullOrWhiteSpace(pkg.InformationRatioFunctionName)) then
                this.InformationRatioFunctionName <- pkg.InformationRatioFunctionName

            if not(String.IsNullOrWhiteSpace(pkg.RiskFunctionName)) then
                this.RiskFunctionName <- pkg.RiskFunctionName

            if not(String.IsNullOrWhiteSpace(pkg.WeightFilterFunctionName)) then
                this.WeightFilterFunctionName <- pkg.WeightFilterFunctionName
            
            if not(String.IsNullOrWhiteSpace(pkg.InstrumentFilterFunctionName)) then
                this.InstrumentFilterFunctionName <- pkg.InstrumentFilterFunctionName

            if not(String.IsNullOrWhiteSpace(pkg.RateCompoundingFunctionName)) then
                this.RateCompoundingFunctionName <- pkg.RateCompoundingFunctionName

            if not(String.IsNullOrWhiteSpace(pkg.AnalyseFunctionName)) then
                this.AnalyseFunctionName <- pkg.AnalyseFunctionName
        with
        | ex -> Console.WriteLine(ex)

    /// <summary>
    /// Function: returns the high water mark which is used by the default exposure and information ratio functions.    
    /// </summary>
    member this.HighLowMark(instrument : Instrument, orderDate : BusinessDay) = 
        let ts_s = match instrument with
                    | x when x.GetType() = (typeof<PortfolioStrategy>) ->
                        let strategy = instrument :?> PortfolioStrategy
                        let instruments = strategy.Instruments(orderDate.DateTime, false)
                        if instruments.Count = 1 && not((Seq.head instruments.Values).InstrumentType = InstrumentType.Strategy) then
                            let ins = (Seq.head instruments.Values)
                            let ttype = if ins.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.ETF || ins.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Equity then TimeSeriesType.AdjClose else if ins.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Strategy then TimeSeriesType.Last else TimeSeriesType.Close
                            ins.GetTimeSeries(ttype)
                        else
                            strategy.GetTimeSeries(TimeSeriesType.Last)                
                    | _ -> 
                        let ttype = if instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.ETF || instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Equity then TimeSeriesType.AdjClose else if instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Strategy then TimeSeriesType.Last else TimeSeriesType.Close
                        instrument.GetTimeSeries(ttype)
                
        if ts_s = null then
            (0.0, 0.0 , 1.0, 1.0, ts_s)
        else
            let ts_s_count = if not (ts_s = null) then ts_s.Count else 0
            let idx_s = if ts_s_count > 0 then ts_s.GetClosestDateIndex(orderDate.DateTime, TimeSeries.DateSearchType.Previous) else 0
            let ts_s = ts_s.GetRange(1 , idx_s)
            let instrument_t_1 = if idx_s <= 0 then 0.0 else ts_s.[Math.Max(0, ts_s.Count - 2)]
            let instrument_t = if idx_s <= 0 then 0.0 else ts_s.[Math.Max(0, ts_s.Count - 1)]
            let hwm = if ts_s.Count = 0 then instrument_t_1 else ts_s.Maximum

            let lwm =                                 
                    let mutable i = ts_s.Count - 1
                    if i > 0 then
                        let mutable v = ts_s.[i]
                        while i - 1 >= 0 && not (ts_s.[i] = hwm) do
                            i <- i - 1
                            let v_i = ts_s.[i]
                            if v_i < v then
                                v <- v_i
                        v    
                    else
                        hwm
                        
            (hwm, lwm , instrument_t, instrument_t_1, ts_s)


    /// <summary>
    /// Function: called by this strategy during the logic execution in order to
    /// measuring the information ratio of a specific asset.    
    /// </summary>
    member this.InformationRatio(orderDate : BusinessDay, instrument : Instrument, current : bool) = 
        this.InformationRatioFunction.Invoke(this, orderDate, instrument, current)           

    /// <summary>
    /// Delegate function used to set the information ratio function
    /// </summary>
    member this.InformationRatioFunction
        with get() : InformationRatio = 
                if Utils.GetFunction(this.Portfolio.MasterPortfolio.Strategy.ID.ToString() + "-" + this.InformationRatioFunctionName) |> isNull |> not then 
                    Utils.GetFunction(this.Portfolio.MasterPortfolio.Strategy.ID.ToString() + "-" + this.InformationRatioFunctionName) :?> InformationRatio//this._informationRatioFunction

                elif Utils.GetFunction(this.InformationRatioFunctionName) |> isNull |> not then 
                    Utils.GetFunction(this.InformationRatioFunctionName) :?> InformationRatio//this._informationRatioFunction
                    
                else 
                    this._informationRatioFunction 
        and set(value) = this._informationRatioFunction <- value

    /// <summary>
    /// Function: default exposure measure implemented as the information ratio.
    /// The information ratio is set as 1.0 - distance from the asset's high water mark to penalise asset's that are far from their highest point and have not yet started recovering.
    /// </summary>
    static member InformationRatioDefault(this : PortfolioStrategy, orderDate : BusinessDay, instrument : Instrument, current : bool) = 
        
        let days_back = (int)this.[orderDate.DateTime, (int)MemoryType.DaysBack, TimeSeriesRollType.Last]
        
        if instrument = null then  // Aggregated IR                
            
            let aum = this.Portfolio.MasterPortfolio.Strategy.ExecutionContext(orderDate).ReferenceAUM
            
            let FXHedgeFlag = (int)this.Portfolio.MasterPortfolio.Strategy.[orderDate.DateTime, (int)MemoryType.FXHedgeFlag, TimeSeriesRollType.Last]

            let timeSeriesMap = Utils.TimeSeriesMap(this, this.Instruments(orderDate.DateTime, false).Values, orderDate, aum, days_back, FXHedgeFlag = 1, current)
            let weightedER, weightedTS =
                        let spositions =             
                            if current then
                                let positions = this.Portfolio.Positions(orderDate.DateTime, false)
                                positions 
                                |> Seq.filter (fun i -> not(this.Portfolio.IsReserve(i.Instrument)) && timeSeriesMap.ContainsKey(i.InstrumentID))
                                |> Seq.map(fun p -> (p.Instrument, p.Unit))
                            else
                                let positions = this.Portfolio.PositionOrders(orderDate.DateTime, false)
                                positions.Values 
                                |> Seq.filter (fun i -> not(this.Portfolio.IsReserve(i.Instrument)) && timeSeriesMap.ContainsKey(i.InstrumentID))
                                |> Seq.map(fun p -> (p.Instrument, p.Unit))

                        let stratValue (strategy : Strategy) = 
                            let timeSeriesMap = Utils.TimeSeriesMap(strategy, strategy.Instruments(orderDate.DateTime, true).Values, orderDate, aum, days_back, true, current)   
                            let spositions = 
                                if current then
                                    let positions = strategy.Portfolio.Positions(orderDate.DateTime, true)
                                    positions 
                                    |> Seq.filter (fun i -> not(strategy.Portfolio.IsReserve(i.Instrument)) && timeSeriesMap.ContainsKey(i.InstrumentID))
                                    |> Seq.map(fun p -> (p.Instrument, p.Unit))
                                else
                                    let positions = strategy.Portfolio.PositionOrders(orderDate.DateTime, true)
                                    positions.Values 
                                    |> Seq.filter (fun i -> not(strategy.Portfolio.IsReserve(i.Instrument)) && timeSeriesMap.ContainsKey(i.InstrumentID))
                                    |> Seq.map(fun p -> (p.Instrument, p.Unit))                    
                            let value =
                                spositions
                                |> Seq.filter(fun (instrument, unit) -> not (strategy.Portfolio.IsReserve(instrument)))
                                |> Seq.fold(fun acc (instrument, unit) ->                                                        
                                                            let fx = CurrencyPair.Convert(1.0, orderDate.DateTime, TimeSeriesType.Close, DataProvider.DefaultProvider, TimeSeriesRollType.Last, strategy.Portfolio.Currency, instrument.Currency)
                                                            acc + unit * instrument.[orderDate.DateTime, TimeSeriesType.Last, TimeSeriesRollType.Last] * fx * (if instrument :? Security then (instrument :?> Security).PointSize else 1.0)
                                            ) 0.0
                            value

                        let weightedER = spositions |> Seq.fold(fun acc (sinstrument, unit) ->
                                                                            let ir = 
                                                                                    if sinstrument.InstrumentType = InstrumentType.Strategy && (not ((sinstrument :?> Strategy).Portfolio = null) && (sinstrument :? PortfolioStrategy)) then
                                                                                        let portfolioStrategy = sinstrument :?> PortfolioStrategy
                                                                                        portfolioStrategy.InformationRatio(orderDate, null, current)
                                                                                    else   
                                                                                        this.InformationRatio(orderDate, sinstrument, current)
                                                                            let vol = this.Risk(orderDate,timeSeriesMap.[sinstrument.ID], aum)
                                                                            let er = ir * vol

                                                                            let unit = unit * (if sinstrument :? Security then (sinstrument :?> Security).PointSize else 1.0)

                                                                            let value = 
                                                                                if sinstrument.InstrumentType = InstrumentType.Strategy then
                                                                                    stratValue(sinstrument :?> Strategy)
                                                                                else
                                                                                    sinstrument.[orderDate.DateTime, (if sinstrument.InstrumentType = InstrumentType.Strategy then TimeSeriesType.Last else TimeSeriesType.Close), TimeSeriesRollType.Last]


                                                                            let fx = CurrencyPair.Convert(1.0, orderDate.DateTime, TimeSeriesType.Close, DataProvider.DefaultProvider, TimeSeriesRollType.Last, this.Portfolio.Currency, sinstrument.Currency)
                                                                    
                                                                            
                                                                            let weight = unit * fx * value / aum

                                                                            //Console.WriteLine(sinstrument.Name + " &&&&& " + unit.ToString() + " " + fx.ToString() + " " + value.ToString() + " " + aum.ToString())
                                                                            //Console.WriteLine(sinstrument.Description + " --> WGT:" + weight.ToString("0.00%") + " ER:" + (er).ToString("0.00%") + " WER:" + (er * weight).ToString("0.00%"))
                                                                            
                                                                            acc + er * weight) 0.0

                        let weightedTS = spositions |> Seq.fold(fun acc (sinstrument, unit) ->                                                                    
                                                                    let unit = unit * (if sinstrument :? Security then (sinstrument :?> Security).PointSize else 1.0)                                                                    
                                                                    
                                                                    let value = 
                                                                                if sinstrument.InstrumentType = InstrumentType.Strategy then
                                                                                    stratValue(sinstrument :?> Strategy)
                                                                                else
                                                                                    sinstrument.[orderDate.DateTime, (if sinstrument.InstrumentType = InstrumentType.Strategy then TimeSeriesType.Last else TimeSeriesType.Close), TimeSeriesRollType.Last]

                                                                    let fx = CurrencyPair.Convert(1.0, orderDate.DateTime, TimeSeriesType.Close, DataProvider.DefaultProvider, TimeSeriesRollType.Last, this.Portfolio.Currency, sinstrument.Currency)

                                                                    let ts = timeSeriesMap.[sinstrument.ID]
                                                                    let weight = fx * unit * value / aum

                                                                    if acc = null then weight * ts else acc + weight * ts) null

                        weightedER, weightedTS
          
            let weightedVol = if weightedTS = null then 1.0 else this.Risk(orderDate, weightedTS, aum)  

            //Console.WriteLine(this.Name + " ++++ ER: " + weightedER.ToString("0.00%") +  " VOL: " + weightedVol.ToString("0.00%"))
                
            let conviction = this.[orderDate.DateTime, (int)MemoryType.ConvictionLevel, TimeSeriesRollType.Last]
                
            let conviction = if Double.IsNaN(conviction) then (if this.Portfolio.ParentPortfolio = null then this.Portfolio.MasterPortfolio.Strategy else this.Portfolio.ParentPortfolio.Strategy).[orderDate.DateTime, (int)MemoryType.ConvictionLevel, this.ID, TimeSeriesRollType.Last] else conviction        
            let conviction = if Double.IsNaN(conviction) then 1.0 else conviction        
                
            let ir = (if (weightedVol) <= 0.0 then -1.0 else weightedER / weightedVol) + (conviction - 1.0)

            if Double.IsNaN(ir) then
                -1.0

            else
                Math.Min(10.0,Math.Max(-10.0,ir))

        elif instrument.InstrumentType = InstrumentType.Strategy && (not ((instrument :?> Strategy).Portfolio = null)) && (instrument :? PortfolioStrategy) then
            try
                let portfolioStrategy = instrument :?> PortfolioStrategy
                portfolioStrategy.InformationRatio(orderDate, null, current)
                                                                                            
            with
            | _ as ex ->
                Console.WriteLine(ex) 
                1.0
        else
            let (hwm, lwm, instrument_t, instrument_t_1, ts_s) = this.HighLowMark(instrument, orderDate)
            let conviction = this.[orderDate.DateTime, (int)MemoryType.ConvictionLevel, instrument.ID, TimeSeriesRollType.Last]
            let conviction = if Double.IsNaN(conviction) then this.[orderDate.DateTime, (int)MemoryType.ConvictionLevel, TimeSeriesRollType.Last] else conviction        
        
            if Double.IsNaN(conviction) || conviction = -10.0 then                                                                                  // Drawdown Adjustment
                let maxdd = Math.Min(instrument_t / hwm - 1.0, 0.0)                
                1.0 + maxdd
        
            elif conviction = -100.0 then                                                                                                             // Zero exposure
                -1.0

            else                                                                                                                                      // Defined IR
                conviction


    /// <summary>
    /// Function: called by this strategy during the logic execution in order to
    /// measuring the risk of a specific asset.
    /// The risk measure is quoted in cash terms.
    /// </summary>
    member this.Risk(orderDate : BusinessDay, timeSeries : TimeSeries , reference_aum : double) = 
        this.RiskFunction.Invoke(this, orderDate, timeSeries, reference_aum)    

    /// <summary>
    /// Delegate function used to set the risk measure function
    /// </summary>
    member this.RiskFunction
        with get() : Risk = 
                if Utils.GetFunction(this.Portfolio.MasterPortfolio.Strategy.ID.ToString() + "-" + this.RiskFunctionName) |> isNull |> not then 
                    Utils.GetFunction(this.Portfolio.MasterPortfolio.Strategy.ID.ToString() + "-" + this.RiskFunctionName) :?> Risk//this._informationRatioFunction

                elif Utils.GetFunction(this.RiskFunctionName) |> isNull |> not then 
                    Utils.GetFunction(this.RiskFunctionName) :?> Risk//this._informationRatioFunction
                    
                else 
                    this._riskFunction

        //with get() : Risk = if this.RiskFunctionName = null || Utils.GetFunction(this.Portfolio.MasterPortfolio.Strategy.ID.ToString() + "-" + this.RiskFunctionName) = null then this._riskFunction else Utils.GetFunction(this.Portfolio.MasterPortfolio.Strategy.ID.ToString() + "-" + this.RiskFunctionName) :?> Risk//this._riskFunction        
        and set(value) = this._riskFunction <- value

    /// <summary>
    /// Function: default risk measure defined as the realised quadratic variation * Sqrt(252) in cash terms
    /// </summary>
    static member RiskDefault(this : PortfolioStrategy, orderDate : BusinessDay, timeSeries : TimeSeries , reference_aum : double) =        
        let vol = sqrt((timeSeries / reference_aum).Variance * 252.0) 
        if vol < 1e-7 then 0.0 else vol
     

    /// <summary>
    /// Function: called by this strategy during the logic execution in order to
    /// define the exposure (1.0, 0.0 or -1.0) to a given instrument.
    /// </summary>
    member this.ExposureFunction
        with get() : Exposure = 
                if Utils.GetFunction(this.Portfolio.MasterPortfolio.Strategy.ID.ToString() + "-" + this.ExposureFunctionName) |> isNull |> not then 
                    Utils.GetFunction(this.Portfolio.MasterPortfolio.Strategy.ID.ToString() + "-" + this.ExposureFunctionName) :?> Exposure//this._informationRatioFunction

                elif Utils.GetFunction(this.ExposureFunctionName) |> isNull |> not then 
                    Utils.GetFunction(this.ExposureFunctionName) :?> Exposure//this._informationRatioFunction
                    
                else 
                    this._exposureFunction
        //with get() : Exposure = if this.ExposureFunctionName = null || Utils.GetFunction(this.Portfolio.MasterPortfolio.Strategy.ID.ToString() + "-" + this.ExposureFunctionName) = null then this._exposureFunction else Utils.GetFunction(this.Portfolio.MasterPortfolio.Strategy.ID.ToString() + "-" + this.ExposureFunctionName) :?> Exposure//this._exposureFunction
        
        and set(value) = this._exposureFunction <- value

    /// <summary>
    /// Delegate function used to set the exposure function
    /// </summary>
    member this.Exposure(orderDate : BusinessDay, instrument : Instrument) = 
        this.ExposureFunction.Invoke(this, orderDate, instrument)    

    /// <summary>
    /// Function: default exposure measure implemented as a stop-loss mechanism.
    /// The stop-loss is implemented if the asset is below a certain threshold from the previous high-watermark.
    /// The threshold is the difference between the High water mark and a volatility scaled level. The effect is that the
    /// stop-loss changes with the level of volatility making it more adaptive and less prone to locking in losses.
    /// </summary>
    static member ExposureDefault(this : PortfolioStrategy, orderDate : BusinessDay, instrument : Instrument) = 

        let conviction = this.[orderDate.DateTime, (int)MemoryType.ConvictionLevel, instrument.ID, TimeSeriesRollType.Last]

        let direction = (float) (this.Direction(orderDate.DateTime))

        if conviction = -100.0 then
            0.0
        
        elif instrument.InstrumentType = InstrumentType.Strategy then
            1.0 * direction
        else
            let (hwm, lwm, instrument_t, instrument_t_1, ts_s) = this.HighLowMark(instrument, orderDate)
            let days_back = (int)this.[orderDate.DateTime, (int)MemoryType.DaysBack, TimeSeriesRollType.Last]
            let exp_threshold = this.[orderDate.DateTime, (int)MemoryType.ExposureThreshold, TimeSeriesRollType.Last]
            let ttype = if instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.ETF || instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Equity then TimeSeriesType.AdjClose elif instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Strategy then TimeSeriesType.Last else TimeSeriesType.Close
            let ts = if ts_s.Count <= 1 then null else ts_s.GetRange(Math.Max(0, ts_s.Count - 1 - days_back) , Math.Max(0, ts_s.Count - 1)).LogReturn().ReplaceNaN(0.0)
            let vol = if ts = null then 0.0 else sqrt(ts.Variance * 252.0)            
            if ts = null then 
                1.0
            else 
                if (hwm * (1.0 - vol * exp_threshold) > instrument_t_1) then
                    0.0  
                else 
                    1.0 * direction


    /// <summary>
    /// Function: called by this strategy during the logic execution in order to
    /// measuring the risk of a specific asset.
    /// The risk measure is quoted in cash terms.
    /// </summary>
    member this.WeightFilter(orderDate : BusinessDay, weightMap : Map<int, float>) = 
        if this.WeightFilterFunction |> isNull then
            weightMap
        else
            this.WeightFilterFunction.Invoke(this, orderDate, weightMap)
    
    /// <summary>
    /// Delegate function used to set the risk measure function
    /// </summary>
    member this.WeightFilterFunction
        //with get() : WeightFilter = if this.WeightFilterFunctionName = null || Utils.GetFunction(this.Portfolio.MasterPortfolio.Strategy.ID.ToString() + "-" + this.WeightFilterFunctionName) = null then this._weightFilterFunction else Utils.GetFunction(this.Portfolio.MasterPortfolio.Strategy.ID.ToString() + "-" + this.WeightFilterFunctionName) :?> WeightFilter//this._weightFilterFunction
        with get() : WeightFilter = 
                if Utils.GetFunction(this.Portfolio.MasterPortfolio.Strategy.ID.ToString() + "-" + this.WeightFilterFunctionName) |> isNull |> not then 
                    Utils.GetFunction(this.Portfolio.MasterPortfolio.Strategy.ID.ToString() + "-" + this.WeightFilterFunctionName) :?> WeightFilter//this._informationRatioFunction

                elif Utils.GetFunction(this.WeightFilterFunctionName) |> isNull |> not then 
                    Utils.GetFunction(this.WeightFilterFunctionName) :?> WeightFilter//this._informationRatioFunction
                    
                else 
                    this._weightFilterFunction
        and set(value) = this._weightFilterFunction <- value


    /// <summary>
    /// Function: called by this strategy during the logic execution in order to
    /// measuring the risk of a specific asset.
    /// The risk measure is quoted in cash terms.
    /// </summary>
    member this.InstrumentFilter(orderDate : BusinessDay, timeSeriesMap : Map<int, TimeSeries>) = 
        if this.InstrumentFilterFunction |> isNull then
            timeSeriesMap
        else
            this.InstrumentFilterFunction.Invoke(this, orderDate, timeSeriesMap)


    /// <summary>
    /// Delegate function used to set the risk measure function
    /// </summary>
    member this.InstrumentFilterFunction
        //with get() : InstrumentFilter = if this.InstrumentFilterFunctionName = null || Utils.GetFunction(this.Portfolio.MasterPortfolio.Strategy.ID.ToString() + "-" + this.InstrumentFilterFunctionName) = null then this._instrumentFilterFunction else Utils.GetFunction(this.Portfolio.MasterPortfolio.Strategy.ID.ToString() + "-" + this.InstrumentFilterFunctionName) :?> InstrumentFilter//this._weightFilterFunction
        with get() : InstrumentFilter = 
                if Utils.GetFunction(this.Portfolio.MasterPortfolio.Strategy.ID.ToString() + "-" + this.InstrumentFilterFunctionName) |> isNull |> not then 
                    Utils.GetFunction(this.Portfolio.MasterPortfolio.Strategy.ID.ToString() + "-" + this.InstrumentFilterFunctionName) :?> InstrumentFilter//this._informationRatioFunction

                elif Utils.GetFunction(this.InstrumentFilterFunctionName) |> isNull |> not then 
                    Utils.GetFunction(this.InstrumentFilterFunctionName) :?> InstrumentFilter//this._informationRatioFunction
                    
                else 
                    this._instrumentFilterFunction
        and set(value) = this._instrumentFilterFunction <- value


    /// <summary>
    /// Function: called by this strategy during the logic execution in order to
    /// calculate indicators continuouly
    /// The risk measure is quoted in cash terms.
    /// </summary>
    member this.IndicatorCalculation(orderDate : BusinessDay) = 
        if this.IndicatorCalculationFunction |> isNull |> not then
            this.IndicatorCalculationFunction.Invoke(this, orderDate)

    /// <summary>
    /// Delegate function used to set the indicator calculation function
    /// </summary>
    member this.IndicatorCalculationFunction
        //with get() : IndicatorCalculation = if this.IndicatorCalculationFunctionName = null || Utils.GetFunction(this.Portfolio.MasterPortfolio.Strategy.ID.ToString() + "-" + this.IndicatorCalculationFunctionName) = null then this._indicatorCalculationFunction else Utils.GetFunction(this.Portfolio.MasterPortfolio.Strategy.ID.ToString() + "-" + this.IndicatorCalculationFunctionName) :?> IndicatorCalculation//this._indicatorCalculationFunction
        with get() : IndicatorCalculation = 
                if Utils.GetFunction(this.Portfolio.MasterPortfolio.Strategy.ID.ToString() + "-" + this.IndicatorCalculationFunctionName) |> isNull |> not then 
                    Utils.GetFunction(this.Portfolio.MasterPortfolio.Strategy.ID.ToString() + "-" + this.IndicatorCalculationFunctionName) :?> IndicatorCalculation//this._informationRatioFunction

                elif Utils.GetFunction(this.IndicatorCalculationFunctionName) |> isNull |> not then 
                    Utils.GetFunction(this.IndicatorCalculationFunctionName) :?> IndicatorCalculation//this._informationRatioFunction
                    
                else 
                    this._indicatorCalculationFunction
        and set(value) = this._indicatorCalculationFunction <- value


    /// <summary>
    /// Function: called by this strategy during the logic execution in order to
    /// calculate indicators continuouly
    /// The risk measure is quoted in cash terms.
    /// </summary>
    member this.Analyse(command : string) : Object = 
        if command = "DataCheck" then
            let sbuilder = new System.Text.StringBuilder()
            //sbuilder.Append("-------------------") |> ignore

            let ts = this.GetTimeSeries(TimeSeriesType.Last)
            let lastDate = ts.DateTimes.[ts.Count - 1]
            let lastValue = ts.[ts.Count - 1]

            sbuilder.AppendLine("Main Strategy, " + lastDate.ToString() + ", " + lastValue.ToString()) |> ignore

            let date = DateTime.Now
            let instruments = this.Instruments(date, true).Values
            instruments
            |> Seq.iter(fun i ->
                    let ts = i.GetTimeSeries(TimeSeriesType.Last)
                    let lastDate = ts.DateTimes.[ts.Count - 1]
                    let lastValue = ts.[ts.Count - 1]

                    sbuilder.AppendLine(i.Name + ", " + lastDate.ToString() + ", " + lastValue.ToString()) |> ignore
                )

            let currencies =
                instruments
                |> Seq.map(fun i -> i.Currency)
                |> Seq.distinct
            
            let rates =
                Instrument.InstrumentsType(InstrumentType.InterestRateSwap)
                |> Seq.append(Instrument.InstrumentsType(InstrumentType.Deposit))
                |> Seq.filter(fun i -> currencies |> Seq.map(fun c -> if c = i.Currency then 1 else 0) |> Seq.max = 1)
                |> Seq.iter(fun i ->
                    let ts = i.GetTimeSeries(TimeSeriesType.Last)
                    let lastDate = ts.DateTimes.[ts.Count - 1]
                    let lastValue = ts.[ts.Count - 1]

                    sbuilder.AppendLine(i.Name + ", " + lastDate.ToString() + ", " + lastValue.ToString()) |> ignore
                )

            sbuilder.ToString() :> Object

        elif this.AnalyseFunction |> isNull  |> not then
            this.AnalyseFunction.Invoke(this, command)
        
        else
            "No Analyse Function Set" :> Object

    /// <summary>
    /// Delegate function used to set the indicator calculation function
    /// </summary>
    member this.AnalyseFunction
        //with get() : Analyse = if this.AnalyseFunctionName = null || Utils.GetFunction(this.Portfolio.MasterPortfolio.Strategy.ID.ToString() + "-" + this.AnalyseFunctionName) = null then this._analyseFunction else Utils.GetFunction(this.Portfolio.MasterPortfolio.Strategy.ID.ToString() + "-" + this.AnalyseFunctionName) :?> Analyse//this._analyseFunction
        with get() : Analyse = 
                if Utils.GetFunction(this.Portfolio.MasterPortfolio.Strategy.ID.ToString() + "-" + this.AnalyseFunctionName) |> isNull |> not then 
                    Utils.GetFunction(this.Portfolio.MasterPortfolio.Strategy.ID.ToString() + "-" + this.AnalyseFunctionName) :?> Analyse//this._informationRatioFunction

                elif Utils.GetFunction(this.AnalyseFunctionName) |> isNull |> not then 
                    Utils.GetFunction(this.AnalyseFunctionName) :?> Analyse//this._informationRatioFunction
                    
                else 
                    this._analyseFunction
        and set(value) = this._analyseFunction <- value

    /// <summary>    
    /// Function: Logic for the optimisation process.
    /// 1) Target volatility for each underlying asset. This step sets an equal volatility weight to all assets.
    /// 2) Concentration risk and Information Ratio management. This step mitigates the risk of an over exposure to a set of highly correlated assets. 
    ///    Furthermore, a tilt in the weight is also implemented based on the information ratios for each asset.
    ///    This entire step is achieved through the implementation of a Mean-Variance optimisation where all volatilities are equal to 1.0, the expected returns are normalised and transformed to information ratios.
    /// 3) Target volatility for the entire portfolio. After steps 1 and 2, the portfolio will probably have a lower risk level than the target due to diversification.
    ///    This step adjusts the strategy's overall exposure in order to achieve the desired target volatility for the entire portfolio.
    /// 4) Maximum individual exposure to each asset is implemented
    /// 5) Maximum exposure to the entire portfolio is implemented
    /// 6) Deleverage the portfolio if the portfolio's Value at Risk exceeds a given level. The exposure is changed linearly such that the new VaR given by the new weights is the limit VaR.
    ///    The implemented VaR measure is the UCITS calculation based on the 99 percentile of the distribution of the 20 day rolling return for the entire portfolio where each return is based on the current weights.
    /// 7) Only rebalance if the notional exposure to a position changes by more than a given threshold.
    /// The class also allows developers to specify a number of custom functions:
    ///     a) Risk: risk measure for each asset.
    ///     b) Exposure: defines if the portfolio should have a long (1.0) / short (-1.0) or neutral (0.0) exposure to a given asset.
    ///     c) InformationRatio: measure of risk-neutral expectation for each asset. This affectes the MV optimisation of the concentration risk management.    
    /// </summary> 
    /// <param name="ctx">Context containing relevant environment information for the logic execution
    /// </param>           
    override this.ExecuteLogic(ctx : ExecutionContext, force : bool) =
        let master_calendar = this.Calendar        
        let orderDate = ctx.OrderDate
        let executionDate = orderDate
        
        
        let master_aum = this.Portfolio.MasterPortfolio.Strategy.ExecutionContext(orderDate).ReferenceAUM
        
        if false then //this.IsResidual then
            let rebalancing_threshold = this.Portfolio.MasterPortfolio.Strategy.[orderDate.DateTime, (int)MemoryType.RebalancingThreshold, TimeSeriesRollType.Last]
            let threshold = master_aum * rebalancing_threshold
            let instruments = this.Portfolio.MasterPortfolio.Strategy.Instruments(orderDate.DateTime, true).Values

            let FXHedgeFlag = (int)this.Portfolio.MasterPortfolio.Strategy.[orderDate.DateTime, (int)MemoryType.FXHedgeFlag, TimeSeriesRollType.Last]

            if FXHedgeFlag = 1 then
                this.Portfolio.HedgeFX(orderDate.DateTime, rebalancing_threshold * master_aum)
                            
            if not (instruments = null) then
                instruments                
                |> Seq.iter(fun instrument ->
                                            let position = this.Portfolio.FindPosition(instrument, orderDate.DateTime, true)
                                            let unitPosition = if position = null then 0.0 else position.Unit

                                            let orders = this.Portfolio.MasterPortfolio.FindOpenOrder(instrument, orderDate.DateTime, true)
                                            let unitOrder = if orders = null then 0.0 else orders.Values |> Seq.filter(fun order -> order.Type = OrderType.Market && order.OrderDate = orderDate.DateTime) |> Seq.fold(fun acc order -> order.Unit + acc) 0.0

                                            let diff = -(unitOrder + Math.Round(unitPosition))

                                            if(unitOrder + unitPosition) = 0.0 then
                                                this.RemoveInstrument(instrument, orderDate.DateTime)

                                            if not (diff = 0.0) then
                                                let instrument_value = instrument.[orderDate.DateTime, TimeSeriesType.Last, TimeSeriesRollType.Last] * (if instrument :? Security then (instrument :?> Security).PointSize else 1.0)
                                                if threshold < (Math.Abs(unitOrder + Math.Round(unitPosition)) * instrument_value) then
                                                    this.Portfolio.CreateOrder(instrument, orderDate.DateTime, Math.Round(unitOrder) - unitOrder - Math.Round(unitPosition), OrderType.Market, 0.0) |> ignore
                                                else
                                                    this.Portfolio.CreateOrder(instrument, orderDate.DateTime, diff, OrderType.Market, 0.0) |> ignore)
                                                                      
        elif not(this.IsResidual) then
            let FixedNotional = this.[orderDate.DateTime, (int)MemoryType.FixedNotional, TimeSeriesRollType.Last]
            let reference_aum = if ((not (Double.IsNaN(FixedNotional))) && FixedNotional > 0.0 && this.Portfolio.ParentPortfolio = null) then FixedNotional else master_aum//ctx.ReferenceAUM            

            this.IndicatorCalculation(orderDate) 

            match reference_aum with
            | 0.0 -> () // Stop calculations
            | _ ->      // Run calculations
                let threshold_rounding = 5
            
                let TargetVolatility = this.Portfolio.MasterPortfolio.Strategy.[orderDate.DateTime, (int)MemoryType.TargetVolatility, TimeSeriesRollType.Last]
                let IndividualVolatilityTargetFlag = (int)this.Portfolio.MasterPortfolio.Strategy.[orderDate.DateTime, (int)MemoryType.IndividialTargetVolatilityFlag, TimeSeriesRollType.Last]
                let GlobalVolatilityTargetFlag = (int)this.Portfolio.MasterPortfolio.Strategy.[orderDate.DateTime, (int)MemoryType.GlobalTargetVolatilityFlag, TimeSeriesRollType.Last]
                let ConcetrationFlag = (int)this.Portfolio.MasterPortfolio.Strategy.[orderDate.DateTime, (int)MemoryType.ConcentrationFlag, TimeSeriesRollType.Last]
                let ExposureFlag = (int)this.Portfolio.MasterPortfolio.Strategy.[orderDate.DateTime, (int)MemoryType.ExposureFlag, TimeSeriesRollType.Last]
                let FXHedgeFlag = (int)this.Portfolio.MasterPortfolio.Strategy.[orderDate.DateTime, (int)MemoryType.FXHedgeFlag, TimeSeriesRollType.Last]
                let rebalancing = (int)this.Portfolio.MasterPortfolio.Strategy.[orderDate.DateTime, (int)MemoryType.RebalancingFrequency, TimeSeriesRollType.Last]
                let days_back = (int)this.Portfolio.MasterPortfolio.Strategy.[orderDate.DateTime, (int)MemoryType.DaysBack, TimeSeriesRollType.Last]
                let instruments = this.Instruments(orderDate.DateTime, false)   

                let days_back = if days_back <= 0 then 3 else days_back

                
                let max_global_levarge, min_global_levarge = 
                    if this.Portfolio.MasterPortfolio = this.Portfolio then 
                        let max = this.[orderDate.DateTime, (int)MemoryType.GlobalMaximumLeverage, TimeSeriesRollType.Last] 
                        let min = this.[orderDate.DateTime, (int)MemoryType.GlobalMinimumLeverage, TimeSeriesRollType.Last] 
                        max, (if Double.IsNaN(min) then 0.0 else min)
                    else                                                                                 
                        10.0, 0.0


                let max_ind_levarge = this.[orderDate.DateTime, (int)MemoryType.IndividualMaximumLeverage, TimeSeriesRollType.Last]                                            
                let min_ind_levarge = this.[orderDate.DateTime, (int)MemoryType.IndividualMinimumLeverage, TimeSeriesRollType.Last]                                                    
                
                let liquidity_days_back = 
                    let ldb = this.[orderDate.DateTime, (int)MemoryType.LiquidityDaysBack, TimeSeriesRollType.Last]                                            
                    if Double.IsNaN(ldb) then
                        0
                    else
                        (int)ldb
                let liquidity_threshold = 
                    let lt = this.[orderDate.DateTime, (int)MemoryType.LiquidityThreshold, TimeSeriesRollType.Last]
                    if Double.IsNaN(lt) then
                        0.0
                    else
                        lt
                    
                
                let MVOptimizationFlag = this.[orderDate.DateTime, (int)MemoryType.MVOptimizationFlag, TimeSeriesRollType.Last]
                let MVOptimizationFlag = if Double.IsNaN(MVOptimizationFlag) then (int)this.Portfolio.MasterPortfolio.Strategy.[orderDate.DateTime, (int)MemoryType.MVOptimizationFlag, TimeSeriesRollType.Last] else (int)MVOptimizationFlag

                let rebalanceDayCheck = match rebalancing with
                                            | 0 -> true //Every day
                                            | -1 -> executionDate.DateTime.DayOfWeek > executionDate.AddBusinessDays(1).DateTime.DayOfWeek || executionDate.AddBusinessDays(1).DateTime.DayOfWeek = DayOfWeek.Saturday || executionDate.AddBusinessDays(1).DateTime.DayOfWeek = DayOfWeek.Sunday //Last day of the week
                                            | x when (x > 0 && x < 32) -> executionDate.DayMonth = rebalancing //x Business Day of the month
                                            | 32 -> executionDate.DayMonth > executionDate.AddBusinessDays(1).DayMonth //Last day of the month
                                            | 33 -> executionDate.DayMonth > executionDate.AddBusinessDays(1).DayMonth && (executionDate.DateTime.Month = 3 || executionDate.DateTime.Month = 6 || executionDate.DateTime.Month = 9 || executionDate.DateTime.Month = 12)
                                            | 34 -> executionDate.DayMonth > executionDate.AddBusinessDays(1).DayMonth && executionDate.DateTime.Month = 12 // Last day of the year
                                            | _ -> false
                
                match (days_back = Int32.MinValue || Double.IsNaN(TargetVolatility) || TargetVolatility = -1.0) with
                | true -> // Create positions if they don't exist because there is no logic to run 
                    let timeSeriesMap = Utils.TimeSeriesMap(this, this.Instruments(orderDate.DateTime, false).Values , orderDate, reference_aum, days_back, FXHedgeFlag = 1, false)

                    if rebalanceDayCheck then
                        let rebalancing_threshold = this.[orderDate.DateTime, (int)MemoryType.RebalancingThreshold, TimeSeriesRollType.Last]
                        if FXHedgeFlag = 1 then
                                this.Portfolio.HedgeFX(orderDate.DateTime, rebalancing_threshold * reference_aum)

                    Seq.toList instruments.Values
                    |> List.filter (fun instrument -> timeSeriesMap.ContainsKey(instrument.ID) && not(this.Exposure(orderDate, instrument) = 0.0))                                     // Filter out reserve instruments and instruments without timeseries data
                                        
                    |> List.iter (fun instrument ->
                        let position = this.Portfolio.FindPosition(instrument, orderDate.DateTime)
                        let size = if not (instrument.InstrumentType = InstrumentType.Strategy && not((instrument :?> Strategy).Portfolio = null)) then reference_aum / (instrument.[orderDate.DateTime, TimeSeriesType.Last, TimeSeriesRollType.Last] * (if instrument :? Security then (instrument :?> Security).PointSize else 1.0)) else reference_aum
                        let itype = if instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.ETF || instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Equity then TimeSeriesType.Last else TimeSeriesType.Last

                        if not(Double.IsNaN(size)) then
                            if ExposureFlag = 1 then
                                if instruments.Count = 1 then          
                                    let instrument = instruments.Values |> Seq.head
                                    if timeSeriesMap.ContainsKey(instrument.ID) && (timeSeriesMap.[instrument.ID].Count >= 5) then
                                        let timeSeries = timeSeriesMap.[instrument.ID]                                    
                                        let exposureWeight = this.Exposure(orderDate, instrument)
                                        if not (position = null) && exposureWeight = 0.0 then
                                            this.Portfolio.CreateTargetMarketOrder(instrument, orderDate.DateTime, 0.0) |> ignore
                                        elif position = null then
                                            this.Portfolio.CreateTargetMarketOrder(instrument, orderDate.DateTime, Math.Abs(size) * exposureWeight) |> ignore
                                            
                                elif position = null then
                                    this.Portfolio.CreateTargetMarketOrder(instrument, orderDate.DateTime, size) |> ignore

                            elif position = null then
                                this.Portfolio.CreateTargetMarketOrder(instrument, orderDate.DateTime, size) |> ignore)

                | _ -> // Run logic                                        
                    let openOrders = this.Portfolio.OpenOrders(orderDate.DateTime, true)                
                    let rebalance = if (not (openOrders = null) && not (openOrders.Count = 0)) || force then true else rebalanceDayCheck


                    let target_units_threshold =
                            let tt = this.[orderDate.DateTime, (int)MemoryType.TargetUnits_Threshold, TimeSeriesRollType.Last]
                            if Double.IsNaN(tt) then
                                0.0
                            else
                                tt


                    //Achieve Target
                    if target_units_threshold > 0.0 then
                        this.Portfolio.Positions(orderDate.DateTime, false) // Kill positions of instruments not in investment universe
                        |> Seq.filter(fun p -> not(this.Portfolio.IsReserve(p.Instrument)))
                        |> Seq.iter(fun p -> 
                            let instrument = p.Instrument
                            let target_units = this.[orderDate.DateTime, (int)MemoryType.TargetUnits, instrument.ID, TimeSeriesRollType.Last]
                            let target_units_submitted = this.[orderDate.DateTime, (int)MemoryType.TargetUnits_Submitted, instrument.ID, TimeSeriesRollType.Last]

                            let target_diff = target_units - target_units_submitted

                            if Math.Abs(target_diff) > 0.00001 then
                                let adv =
                                    [|0 .. liquidity_days_back - 1|]
                                    |> Array.map(fun i -> 
                                            let volume = instrument.[orderDate.AddBusinessDays(-i).DateTime, TimeSeriesType.Volume, TimeSeriesRollType.Last]// vol_ts.[orderDate.AddBusinessDays(-i), TimeSeries.DataSearchType.Previous]
                                            let volume = if Double.IsNaN(volume) then 0.0 else volume
                                            volume)
                                    |> Array.average
                            
                                let adv_max = adv * target_units_threshold
                                let units_max_ratio = Math.Min(1.0 , adv_max / Math.Abs(target_diff))
                                let units = target_diff * units_max_ratio

                                this.AddMemoryPoint(orderDate.DateTime, units + target_units_submitted, (int)MemoryType.TargetUnits_Submitted, instrument.ID)
                                this.Portfolio.CreateOrder(instrument, orderDate.DateTime, units, OrderType.Market, 0.0) |> ignore
                            )
                                            

                    let adjustStrategyAUM (instru : Instrument) = 
                        if instru.InstrumentType = InstrumentType.Strategy && not ((instru :?> Strategy).Portfolio = null) then
                            let strategy = instru :?> Strategy
                            
                            let strategy_aum = strategy.GetSODAUM(orderDate.DateTime, TimeSeriesType.Last)                            
                            let positions = strategy.Portfolio.PositionOrders(orderDate.DateTime, true)
                            let value_pos, value_neg =
                                positions.Values 
                                |> Seq.filter(fun position -> not (strategy.Portfolio.IsReserve(position.Instrument)))
                                |> Seq.fold(fun (acc_pos, acc_neg) position ->
                                                            let instrument = position.Instrument                                                                                                                                                                                                                    
                                                            //let fx = CurrencyPair.Convert(1.0, orderDate.DateTime, TimeSeriesType.Close, DataProvider.DefaultProvider, TimeSeriesRollType.Last, strategy.Portfolio.Currency, instrument.Currency)
                                                            let fx = CurrencyPair.Convert(1.0, orderDate.DateTime, strategy.Portfolio.Currency, instrument.Currency)

                                                            let pos = acc_pos + Math.Max(0.0, position.Unit) * instrument.[orderDate.DateTime, TimeSeriesType.Last, TimeSeriesRollType.Last] * fx * (if instrument :? Security then (instrument :?> Security).PointSize else 1.0)
                                                            let neg = acc_neg + -Math.Min(0.0, position.Unit) * instrument.[orderDate.DateTime, TimeSeriesType.Last, TimeSeriesRollType.Last] * fx * (if instrument :? Security then (instrument :?> Security).PointSize else 1.0)
                                                            (pos, neg)
                                            ) (0.0, 0.0)

                            let value = Math.Max(value_pos, value_neg)
                            
                            if Math.Abs(value) = 0.0 || strategy_aum = 0.0 then 
                                1.0
                            else 
                                Math.Abs(strategy_aum / value)
                        else
                            1.0

                    if rebalance then
                                    
                        let timeSeriesMap = Utils.TimeSeriesMap(this, instruments.Values, orderDate, reference_aum, days_back, FXHedgeFlag = 1, false)
                        let timeSeriesMap = this.InstrumentFilter(orderDate, timeSeriesMap)

                        let weightMap = if rebalance then

                                            this.Portfolio.Positions(orderDate.DateTime, false) // Kill positions of instruments not in investment universe
                                            |> Seq.filter(fun p -> not(timeSeriesMap.ContainsKey(p.Instrument.ID)) && not(this.Portfolio.IsReserve(p.Instrument)))
                                            |> Seq.iter(fun p -> 
                                                    let instrument = p.Instrument
                                                    if instrument :? Security && liquidity_days_back > 1 && target_units_threshold > 0.0 then
                                                        let oldUnits = p.Unit
                                                        let unit_diff = -oldUnits
                                
                                                        this.AddMemoryPoint(orderDate.DateTime, unit_diff, (int)MemoryType.TargetUnits, instrument.ID)

                                
                                                        let adv =
                                                            [|0 .. liquidity_days_back - 1|]
                                                            |> Array.map(fun i -> 
                                                                    let volume = instrument.[orderDate.AddBusinessDays(-i).DateTime, TimeSeriesType.Volume, TimeSeriesRollType.Last]// vol_ts.[orderDate.AddBusinessDays(-i), TimeSeries.DataSearchType.Previous]
                                                                    let volume = if Double.IsNaN(volume) then 0.0 else volume
                                                                    volume)
                                                            |> Array.average
                                                        let adv_max = adv * target_units_threshold

                                                        let units_max_ratio = Math.Min(1.0 , adv_max / Math.Abs(unit_diff))
                                                        
                                                        let unit_diff = unit_diff * units_max_ratio

                                                        this.AddMemoryPoint(orderDate.DateTime, unit_diff, (int)MemoryType.TargetUnits_Submitted, instrument.ID)
                                                        this.Portfolio.CreateOrder(instrument, orderDate.DateTime, unit_diff, OrderType.Market, 0.0) |> ignore
                                                    else
                                                        p.UpdateTargetMarketOrder(orderDate.DateTime, 0.0, UpdateType.OverrideUnits) |> ignore)
                                            
                                            instruments.Values
                                            |> Seq.filter (fun instrument -> timeSeriesMap.ContainsKey(instrument.ID)) // Filter out reserve instruments and instruments without timeseries data                                                                                  
                                            |> Seq.groupBy (fun instrument -> instrument.ID)
                                            |> Map.ofSeq                                                                                                                            

                                            |> Map.map (fun id tuple -> 1.0 / (double) instruments.Count)              // Individual Equal Weight Notional Weights                                            
                                                                                
                                            |> Map.map (fun id oldWeight ->                                            // Neutral Individual Volatility Weight (Step 1)
                                                let instrument = Instrument.FindInstrument(id)
                                                let strategy_ts = if timeSeriesMap.ContainsKey(id) then timeSeriesMap.[id] else null
                                                let dp_notional = match instrument with
                                                                    | x when x.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Strategy && (not ((x :?> Strategy).Portfolio = null)) ->
                                                                        let strategy = x :?> Strategy                                           
                                                                        let strategy_aum = strategy.GetSODAUM(orderDate.DateTime, TimeSeriesType.Last)                                                                
                                                                        let vol = if strategy_aum = 0.0 || strategy_ts = null then 0.0 elif strategy_ts.Count < 5 then TargetVolatility else this.Risk(orderDate, strategy_ts, reference_aum)
                                                                        
                                                                        let notional_allocation = if strategy_ts = null then 1.0 elif strategy_aum = 0.0 then 0.0 else 1.0
                                                                        let dp_unfiltered = if TargetVolatility <= 0.0 then 1.0 else if vol < 1e-5 || IndividualVolatilityTargetFlag = 0 then 1.0 else TargetVolatility / vol                                                                
                                                                        let dp = if dp_unfiltered < 1e-5 then 0.0 else dp_unfiltered

                                                                        dp * (if notional_allocation = 0.0 then 1.0 / (double)instruments.Count else notional_allocation)                                                        
                                                                    | _ -> 
                                                                        let vol = if strategy_ts = null || strategy_ts.Count < 5 then TargetVolatility else this.Risk(orderDate, strategy_ts, reference_aum)                                                                        

                                                                        let exposureWeight = if ExposureFlag = 1 then this.Exposure(orderDate, instrument) else 1.0
                                                                        let dp = if TargetVolatility <= 0.0 then 1.0 else if vol < 1e-5 || IndividualVolatilityTargetFlag = 0 then 1.0 else TargetVolatility / vol
                                                                        dp * (if exposureWeight = 0.0 then 0.0 else (if exposureWeight > 0.0 then 1.0 else -1.0))                  
                                                              
                                                if Double.IsNaN(dp_notional) then 0.0 else dp_notional)

                                            |> (fun weightMap ->                                                       // Neutral Correlation Weight (Step 2)
                                                if weightMap.Count <= 1 || not(ConcetrationFlag = 1) then
                                                    weightMap
                                                else
                                                    let weights = weightMap |> Map.toList |> List.map (fun (k,v) -> v)
                                                    let ids = weightMap |> Map.toList |> List.map (fun (k,v) -> k)
                                                    
                                                    let upper = 
                                                        ids 
                                                        |> List.map(fun id ->
                                                                            let ins = Instrument.FindInstrument(id)

                                                                            let liquidity_adj = 
                                                                                if ins :? Security && liquidity_days_back > 1 && liquidity_threshold > 0.0 then
                                                                                    
                                                                                    let adv =
                                                                                        [|0 .. liquidity_days_back - 1|]
                                                                                        |> Array.map(fun i -> 
                                                                                                let fx = CurrencyPair.Convert(1.0, orderDate.AddBusinessDays(-i).DateTime, this.Currency, ins.Currency)
                                                                                                let price = ins.[orderDate.AddBusinessDays(-i).DateTime, TimeSeriesType.Last, TimeSeriesRollType.Last] * (if ins :? Security then (ins :?> Security).PointSize else 1.0) * fx// price_ts.[orderDate.AddBusinessDays(-i), TimeSeries.DataSearchType.Previous]
                                                                                                let price = if Double.IsNaN(price) then 0.0 else price
                                                                                                let volume = ins.[orderDate.AddBusinessDays(-i).DateTime, TimeSeriesType.Volume, TimeSeriesRollType.Last]// vol_ts.[orderDate.AddBusinessDays(-i), TimeSeries.DataSearchType.Previous]
                                                                                                let volume = if Double.IsNaN(volume) then 0.0 else volume

                                                                                                price * volume
                                                                                                )
                                                                                        |> Array.average
                                                                                    let avg = liquidity_threshold * adv / reference_aum                                                                                    
                                                                                    avg
                                                                                else
                                                                                    100.0

                                                                            

                                                                            let max_ind_lev_i = this.[orderDate.DateTime, id, (int)MemoryType.IndividualMaximumLeverage, TimeSeriesRollType.Last]
                                                                            if not (Double.IsNaN(max_ind_lev_i)) then
                                                                                Math.Min(liquidity_adj, max_ind_lev_i)
                                                                            else
                                                                                Math.Min(liquidity_adj, 100.0))
                                                        |> List.toArray
                                                    let low_adj = -0.000001 / (double)weightMap.Count
                                                    let lower = 
                                                        ids 
                                                        |> List.map(fun id ->
                                                                            let ins = Instrument.FindInstrument(id)
                                                                            let min_ind_lev_i =  this.[orderDate.DateTime, id, (int)MemoryType.IndividualMinimumLeverage, TimeSeriesRollType.Last] - low_adj
                                                                            if not(Double.IsNaN(min_ind_lev_i)) then
                                                                                min_ind_lev_i
                                                                            else
                                                                                -100.0)
                                                        |> List.toArray                                                   

                                                    let optimizationTuple = 
                                                        weightMap
                                                        |> Map.map (fun id weight ->
                                                            let instrument = Instrument.FindInstrument(id)
                                                            let ts = if timeSeriesMap.ContainsKey(id) then timeSeriesMap.[id] else null  
                                                                                                                                                                                                                                                                                                                                                     
                                                            let vol = this.Risk(orderDate, ts, reference_aum)
                                                                                
                                                            let informationRatio = this.InformationRatio(orderDate, instrument, false)
                                                                                

                                                            let exp = if ExposureFlag = 1 then this.Exposure(orderDate, instrument) else 1.0                                                                                                                                                                
                                                            ((if exp = 0.0 then -100.0 else informationRatio) , if ts = null then null else ts / reference_aum))
                                                    let (informationRatio, weightTimeSeries) = (optimizationTuple |> Map.toList |> List.map (fun (k,v) -> fst v) , optimizationTuple |> Map.toList |> List.map (fun (k,v) -> snd v))                                            

                                                    let newWeights = 
                                                                    if  not(MVOptimizationFlag = 1) then
                                                                        let optimal_wgts = Utils.Optimize(weightTimeSeries, informationRatio)                                                                        
                                                                        optimal_wgts |>  Array.mapi(fun i w -> (if Double.IsNaN(w) then 0.0 else w) * (weights.[i])) |> Array.toList                                                                     
                                                                    else                       
                                                                    
                                                                        
                                                                        let group_contraints =
                                                                            ids
                                                                            |> List.toSeq
                                                                            |> Seq.map(fun id ->

                                                                                let mutable go = true
                                                                                let mutable i = 0
                                                                                while go do
                                                                                    let code = Int32.Parse(((int)AQI.AQILabs.SDK.Strategies.MemoryType.GroupLeverage).ToString() + (if i = 0 then "" else i.ToString()))
                                                                                    let groupid =  this.[orderDate.DateTime, id, code, TimeSeriesRollType.Last]
                                                                                    i <- i + 1
                                                                                    if Double.IsNaN(groupid) then
                                                                                        go <- false
                                                                                [|0 .. i|] 
                                                                                |> Array.map(fun i -> 
                                                                                    let code = Int32.Parse(((int)AQI.AQILabs.SDK.Strategies.MemoryType.GroupLeverage).ToString() + (if i = 0 then "" else i.ToString()))
                                                                                    let groupid =  this.[orderDate.DateTime, id, code, TimeSeriesRollType.Last]
                                                                                    (id, groupid)))
                                                                            |> Seq.concat
                                                                            |> Seq.filter(fun (id, groupid) -> not(Double.IsNaN(groupid)))
                                                                            |> Seq.groupBy(fun (id, groupid) -> groupid)
                                                                            |> Seq.map(fun (groupid, data) -> 
                                                                                    let dataMap = data |> Seq.map(fun (id, groupid) -> (id, groupid)) |> Map.ofSeq
                                                                                    let idvec = ids |> List.map(fun id -> if dataMap.ContainsKey(id) then 1.0 else 0.0)
                                                                                    let group_min =  this.[orderDate.DateTime, (int)groupid, (int)MemoryType.IndividualMinimumLeverage, TimeSeriesRollType.Last]
                                                                                    let group_max =  this.[orderDate.DateTime, (int)groupid, (int)MemoryType.IndividualMaximumLeverage, TimeSeriesRollType.Last]
                                                                                    (idvec |> List.toArray, group_min, group_max))
                                                                            |> Seq.filter(fun (data, gmin, gmax) -> not(Double.IsNaN(gmin)) && not(Double.IsNaN(gmax)))
                                                                            |> Seq.toList
                                                                        
                                                                        let current_weights = 
                                                                            ids 
                                                                            |> List.map(fun id ->
                                                                                                let ins = Instrument.FindInstrument(id)
                                                                            
                                                                                                let pos = this.Portfolio.FindPosition(ins, orderDate.DateTime)
                                                                                                let wgt = 
                                                                                                        if pos = null then
                                                                                                            0.0
                                                                                                        else
                                                                                                            if ins.InstrumentType = InstrumentType.Strategy && not((ins :?> Strategy).Portfolio = null) then
                                                                                                                (ins :?> Strategy).Portfolio.RiskNotional(orderDate.DateTime) / reference_aum
                                                                                                            else
                                                                                                                pos.NotionalValue(orderDate.DateTime) / reference_aum
                                                                                                wgt)
                                                                            |> List.toArray
                                                                        let transaction_costs = 
                                                                            let instructions = Market.Instructions()
                                                                            ids 
                                                                            |> List.map(fun id ->                                                                                                
                                                                                                let tc =
                                                                                                        let portfolio_instructions = 
                                                                                                            if instructions.ContainsKey(this.Portfolio.MasterPortfolio.ID) then
                                                                                                                instructions.[this.Portfolio.MasterPortfolio.ID]                                                                                                                
                                                                                                            elif instructions.ContainsKey(0) then
                                                                                                                instructions.[0]
                                                                                                            else
                                                                                                                null

                                                                                                        let ins = Instrument.FindInstrument(id)
                                                                                                        if ins.InstrumentType = InstrumentType.Strategy && not((ins :?> Strategy).Portfolio = null) then
                                                                                                            (ins :?> Strategy).Portfolio.Positions(orderDate.DateTime, true)
                                                                                                            |> Seq.map(fun p -> 
                                                                                                                    let instruction = 
                                                                                                                            if not(portfolio_instructions = null) && portfolio_instructions.ContainsKey(p.Instrument.ID) then
                                                                                                                                 portfolio_instructions.[p.Instrument.ID]
                                                                                                                            elif not(portfolio_instructions = null) && portfolio_instructions.ContainsKey(0) then
                                                                                                                                 portfolio_instructions.[0]
                                                                                                                            else
                                                                                                                                null

                                                                                                                    let tc = if instruction = null then 0.0 else instruction.ExecutionFee
                                                                                                                    let tc = if tc < 0.0 then -tc else tc
                                                                                                                    p.NotionalValue(orderDate.DateTime) * tc / reference_aum)
                                                                                                            |> Seq.sum

                                                                                                        else
                                                                                                            let instruction = 
                                                                                                                            if not(portfolio_instructions = null) && portfolio_instructions.ContainsKey(id) then
                                                                                                                                 portfolio_instructions.[id]
                                                                                                                            elif not(portfolio_instructions = null) && portfolio_instructions.ContainsKey(0) then
                                                                                                                                 portfolio_instructions.[0]
                                                                                                                            else
                                                                                                                                null
                                                                                                            
                                                                                                            let exec_fee = if instruction = null then 0.0 else instruction.ExecutionFee;
                                                                                                            let exec_min_fee = if instruction = null then 0.0 else instruction.ExecutionMinFee;
                                                                                                            let exec_max_fee = if instruction = null then 0.0 else instruction.ExecutionMaxFee;
                                                                                                            
                                                                                                            exec_fee
                                                                                                let tc = if tc < 0.0 then -tc else tc                                                                                            
                                                                                                tc)
                                                                            |> List.toArray  
                                                                            

                                                                        let market_impact_transaction_cost_functions =
                                                                            ids
                                                                            |> List.toArray
                                                                            |> Array.zip transaction_costs
                                                                            |> Array.map(fun (tc, id) -> (tc, Instrument.FindInstrument(id)))
                                                                            |> Array.map(fun (tc, ins) ->
                                                                                    let liquidity = 
                                                                                        if ins :? Security && liquidity_days_back > 1 && liquidity_threshold > 0.0 then                                                                                    
                                                                                            let adv_array =
                                                                                                [|0 .. liquidity_days_back - 1|]
                                                                                                |> Array.map(fun i -> 
                                                                                                        let fx = CurrencyPair.Convert(1.0, orderDate.AddBusinessDays(-i).DateTime, this.Currency, ins.Currency)
                                                                                                        let price = ins.[orderDate.AddBusinessDays(-i).DateTime, TimeSeriesType.Last, TimeSeriesRollType.Last] * (if ins :? Security then (ins :?> Security).PointSize else 1.0) * fx// price_ts.[orderDate.AddBusinessDays(-i), TimeSeries.DataSearchType.Previous]
                                                                                                        let price = if Double.IsNaN(price) then 0.0 else price
                                                                                                        let volume = ins.[orderDate.AddBusinessDays(-i).DateTime, TimeSeriesType.Volume, TimeSeriesRollType.Last]// vol_ts.[orderDate.AddBusinessDays(-i), TimeSeries.DataSearchType.Previous]
                                                                                                        let volume = if Double.IsNaN(volume) then 0.0 else volume

                                                                                                        price * volume
                                                                                                        )
                                                                                                
                                                                                            let adv = adv_array |> Array.average                                                                                                                                                                                        
                                                                                            adv
                                                                                        else
                                                                                            Double.MaxValue
                                                                                    
                                                                                    let marketImpactFactor =
                                                                                        if Market.SimulateMarketImpact = null then
                                                                                            0.0
                                                                                        else
                                                                                            Market.SimulateMarketImpact.Invoke(orderDate.DateTime, ins)

                                                                                    tc, liquidity, marketImpactFactor
                                                                                )                                                                                
                                                                            |> Array.map(fun (tc, liquidity, marketImpactFactor) -> 
                                                                                fun (dw : float) -> 
                                                                                                                                                                        
                                                                                    let adw = Math.Abs(dw)
                                                                                    let cash_dw = adw * reference_aum

                                                                                    let market_impact = marketImpactFactor * cash_dw / liquidity

                                                                                    //Console.WriteLine("-----------------")
                                                                                    //Console.WriteLine("market_impact: " + market_impact.ToString())
                                                                                    //Console.WriteLine("market_impact fator: " + marketImpactFactor.ToString())
                                                                                    //Console.WriteLine("cash_dw: " + cash_dw.ToString())
                                                                                    //Console.WriteLine("adw: " + adw.ToString())
                                                                                    //Console.WriteLine("liquidity: " + liquidity.ToString())

                                                                                    (adw * tc + market_impact)
                                                                                )

                                                                        let optw = Utils.OptimizeMV(weightTimeSeries, informationRatio, TargetVolatility, lower, upper, current_weights, market_impact_transaction_cost_functions, (if max_global_levarge = 10.0 then 1.0 else max_global_levarge), min_global_levarge, group_contraints)
                                                                        let optimal_wgts = (optw |> Array.toList)

                                                                        optimal_wgts |> Seq.toList |> List.mapi(fun i w -> if Double.IsNaN(w) then 0.0 else  Math.Max(0.0, w))
                                                    if Portfolio.DebugPositions then
                                                        Console.WriteLine("MAX GLO: " + max_global_levarge.ToString())
                                                        Console.WriteLine("MIN GLO: " + min_global_levarge.ToString())
                                                        Console.WriteLine("Total: " + (ids |> List.mapi(fun i element -> newWeights.[i]) |> List.sum).ToString())
                                                        ids 
                                                        |> List.iteri (fun i element -> 
                                                               let instrument = Instrument.FindInstrument(element)
                                                               let ir = informationRatio.[i]
                                                               let wgt = newWeights.[i]
                                                               
                                                               let upper_wgt = upper.[i]
                                                               let lower_wgt = lower.[i]

                                                               Console.WriteLine(instrument.Description + " -> " + ir.ToString("0.0%") + " " + wgt.ToString("0.0%") + " " + upper_wgt.ToString("0.0%") + " " + lower_wgt.ToString("0.0%")))

                                                    ids |> List.mapi (fun i element -> (element, Math.Max(lower.[i], Math.Min(upper.[i], newWeights.[i])))) |> Map.ofList)
                                                    
                                            |> (fun weightMap ->                                                       // Neutral Portfolio Volatility Weight (Step 3)
                                                if weightMap.Count = 0 || MVOptimizationFlag = 1 then
                                                    weightMap
                                                else
                                                    let weights = weightMap |> Map.toList |> List.map (fun (k,v) -> v)
                                                    let ids = weightMap |> Map.toList |> List.map (fun (k,v) -> k)

                                                    let aggregatedTimeSeries = weightMap |> Map.toList |> List.map (fun (k,v) -> (v, timeSeriesMap.[k])) |> List.fold (fun acc element -> if acc = null then ((fst element) * (snd element)) else acc + ((fst element) * (snd element))) null
                                                    let portfolio_vol = if aggregatedTimeSeries = null then TargetVolatility else this.Risk(orderDate, aggregatedTimeSeries, reference_aum)                                                                                        

                                                    

                                                    let dpp = if portfolio_vol < 1e-3 || GlobalVolatilityTargetFlag = 0 then 1.0 else Math.Min(10.0, TargetVolatility / portfolio_vol)                                                    
                                                    let newWeights = weights |> List.mapi(fun i w -> w * dpp)                                            
                                                    let newWeightMap = ids |> List.mapi (fun i element -> (element, newWeights.[i]))

                                                    let ids = newWeightMap |> List.map (fun (k,v) -> k)

                                                    newWeightMap |> Map.ofList)

                                            |> (fun weightMap -> this.WeightFilter(orderDate, weightMap))              // Custom Weight Filter (Step 4)

                                            |> Map.map (fun id weight ->                                               // Individually Capped Weights (Step 5)
                                                let ins = Instrument.FindInstrument(id)
                                                                                                                                                    
                                                let max_ind_lev_i_ = this.[orderDate.DateTime, id, (int)MemoryType.IndividualMaximumLeverage, TimeSeriesRollType.Last]
                                                let max_ind_lev_i = (if Double.IsNaN(max_ind_lev_i_) then max_ind_levarge else max_ind_lev_i_) // adj

                                                let min_ind_lev_i_ = this.[orderDate.DateTime, id, (int)MemoryType.IndividualMinimumLeverage, TimeSeriesRollType.Last]
                                                let min_ind_lev_i = (if Double.IsNaN(min_ind_lev_i_) then (if Double.IsNaN(min_ind_levarge) then -1.0 else min_ind_levarge) else min_ind_lev_i_) // adj

                                                (if Double.IsNaN(TargetVolatility) then 1.0 else Math.Max(min_ind_lev_i, Math.Min(max_ind_lev_i, weight))))
                                                
                                            |> (fun weightMap ->                                                       // Global Leverage Constrained Weight (Step 6)
                                                if weightMap.Count = 0 then
                                                    weightMap
                                                else
                                                    let weights = weightMap |> Map.toList |> List.map (fun (k,v) -> v)// / Math.Min(1.0, adjustStrategyAUM(Instrument.FindInstrument(k)))  )
                                                    let ids = weightMap |> Map.toList |> List.map (fun (k,v) -> k)

                                                    let aggregated_weights_pos = weights |> List.map(fun x -> Math.Max(0.0, x)) |> List.sum
                                                    let aggregated_weights_neg = weights |> List.map(fun x -> -Math.Min(0.0, x)) |> List.sum
                                                    let aggregated_weights = Math.Max(aggregated_weights_pos, aggregated_weights_neg)
                                                    
                                                    let max_leverage_scale = 
                                                                            if Math.Round((aggregated_weights), threshold_rounding) > Math.Round(max_global_levarge, threshold_rounding) then
                                                                                max_global_levarge / aggregated_weights 
                                                                            else 
                                                                                1.0                                                                                        
                                                    let newWeights = weights |> List.mapi(fun i w -> w * max_leverage_scale)
                                                    let newWeightMap = ids |> List.mapi (fun i element -> (element, newWeights.[i]))
                                                    newWeightMap |> Map.ofList)

                                            |> (fun weightMap ->                                                       // Maximum VaR Constrained Weight (Step 7)
                                                if weightMap.Count = 0 then
                                                    weightMap
                                                else
                                                    let days_window = 20
                                                    let days_back = 252
                                                    let level = 0.99
                                                    let level = 1.0 - level

                                                    let weights = weightMap |> Map.toList |> List.map (fun (k,v) -> v)                                            
                                                    let ids = weightMap |> Map.toList |> List.map (fun (k,v) -> k)
                                                    let TargetVAR = this.Portfolio.MasterPortfolio.Strategy.[orderDate.DateTime, (int)MemoryType.TargetVAR, TimeSeriesRollType.Last]
                                                    let GlobalVARTargetFlag = (int)this.Portfolio.MasterPortfolio.Strategy.[orderDate.DateTime, (int)MemoryType.GlobalTargetVARFlag, TimeSeriesRollType.Last]
                                                    if (GlobalVARTargetFlag = 1 && not (TargetVAR = 0.0)) then // VaR Calculation
                                                        let returnsList = 
                                                            Seq.toArray instruments.Values
                                                            |> Array.filter (fun instrument -> weightMap.ContainsKey(instrument.ID))
                                                            |> Array.map (fun instrument ->                                                
                                                                match instrument with
                                                                | x when x.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Strategy && (not ((x :?> Strategy).Portfolio = null)) -> // Generate aggregated list of returns for each instrument in portfolio of this strategy
                                                                    let strategy = x :?> Strategy
                                                                    let strategy_aum = strategy.GetSODAUM(orderDate.DateTime, TimeSeriesType.Last)
                                                                                                                                
                                                                    strategy.Portfolio.PositionOrders(orderDate.DateTime, true).Values
                                                                    |> Seq.toArray
                                                                    |> Array.filter (fun order -> not (strategy.Portfolio.IsReserve(order.Instrument)))
                                                                    |> Array.fold (fun acc position ->
                                                                        let ttype_sub = if position.Instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.ETF || position.Instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Equity then TimeSeriesType.AdjClose elif position.Instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Strategy then TimeSeriesType.Last else TimeSeriesType.Close;
                                                                        let ts = position.Instrument.GetTimeSeries(ttype_sub)
                                                                        let orders = this.Portfolio.FindOpenOrder(position.Instrument, orderDate.DateTime, true)
                                                                        let order = if orders.Count = 0 then null else orders.Values  |> Seq.toList |> List.filter (fun o -> o.Type = OrderType.Market) |> List.reduce (fun acc o -> o) 
                                                                        let idx = ts.GetClosestDateIndex(orderDate.DateTime, TimeSeries.DateSearchType.Previous)
                                                                        let weight = position.Unit * (if position.Instrument :? Security then (position.Instrument :?> Security).PointSize else 1.0)
                                                                        
                                                                        let rets = 
                                                                                [|1 .. days_back|] 
                                                                                |> Array.map (fun i ->
                                                                                    let first = Math.Max(0, idx - days_back + i - days_window)
                                                                                    let last = Math.Max(0, idx - days_back + i)
                                                                                    let fx_t = CurrencyPair.Convert(1.0, ts.DateTimes.[last], TimeSeriesType.Close, DataProvider.DefaultProvider, TimeSeriesRollType.Last, this.Portfolio.Currency, position.Instrument.Currency)
                                                                                    let fx_0 = CurrencyPair.Convert(1.0, ts.DateTimes.[first], TimeSeriesType.Close, DataProvider.DefaultProvider, TimeSeriesRollType.Last, this.Portfolio.Currency, position.Instrument.Currency)
                                                                                    let ret = (ts.[last] * fx_t) - (ts.[first] * fx_0)
                                                                                    ret * weight * weightMap.[instrument.ID])
                                                                        [|1 .. days_back|] |> Array.map (fun i -> acc.[i - 1] + rets.[i - 1])) (Array.zeroCreate days_back )
                                        
                                                                | _ ->  // Generate list of returns for this instrument
                                                                    let ttype = if instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.ETF || instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Equity then TimeSeriesType.Close elif instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Strategy then TimeSeriesType.Last else TimeSeriesType.Close              
                                                                    let ts = instrument.GetTimeSeries(ttype)                                
                                                                    let idx = ts.GetClosestDateIndex(orderDate.DateTime, TimeSeries.DateSearchType.Previous)
                                                                    let scale = (if instrument :? Security then (instrument :?> Security).PointSize else 1.0)

                                                                    [|1 .. days_back|] |> Array.map (fun i ->
                                                                        let first = Math.Max(0, idx - days_back + i - days_window)
                                                                        let last = Math.Min(ts.Count - 1, Math.Max(0, idx - days_back + i))
                                                                        let fx_t = CurrencyPair.Convert(1.0, ts.DateTimes.[last], TimeSeriesType.Close, DataProvider.DefaultProvider, TimeSeriesRollType.Last, this.Portfolio.Currency, instrument.Currency)
                                                                        let fx_0 = CurrencyPair.Convert(1.0, ts.DateTimes.[first], TimeSeriesType.Close, DataProvider.DefaultProvider, TimeSeriesRollType.Last, this.Portfolio.Currency, instrument.Currency)
                                                                        let ret = (ts.[last] * fx_t) - (ts.[first] * fx_0)
                                    
                                                                        ret * scale * (weightMap.[instrument.ID] * reference_aum) / (fx_t * ts.[last] * scale)))
                                    
                                                        //let rets = returnsList|> List.fold (fun (acc : float list) rets -> [1 .. 252] |> List.map( fun j -> acc.[j - 1] + rets.[j - 1])) (Array.zeroCreate 252 |> Array.toList) |> List.sort
                                                        let rets = returnsList |> Array.fold (fun (acc : float[] ) rets -> [|1 .. days_back|] |> Array.map( fun j -> acc.[j - 1] + rets.[j - 1])) (Array.zeroCreate days_back) |> Array.sort
                                                        let pctl = 0.01 * (double (rets.Length - 1)) + 1.0;
                                                        let pctl_n = (int)pctl
                                                        let pctl_d = pctl - (double)pctl_n
                                                        let VaR = (rets.[pctl_n] + pctl_d * (rets.[pctl_n + 1] - rets.[pctl_n])) / reference_aum
                                                
                                                        if (VaR <= TargetVAR) then                        
                                                            let max_var_scale = TargetVAR / VaR
                                                            let newWeights = weights |> List.mapi(fun i w -> w * max_var_scale)                                            
                                                            let newWeightMap = ids |> List.mapi (fun i element -> (element, newWeights.[i]))

                                                            newWeightMap |> Map.ofList
                                                        else
                                                            weightMap
                                                    else
                                                        weightMap)

                                        else
                                            [(0,0.0)] |> Map.ofList
                                    
                        let rebalancing_threshold = this.[orderDate.DateTime, (int)MemoryType.RebalancingThreshold, TimeSeriesRollType.Last]

                        let reference_aum = if ((not (Double.IsNaN(FixedNotional))) && FixedNotional > 0.0) then FixedNotional else ctx.ReferenceAUM
                            

                        if FXHedgeFlag = 1 then
                            this.Portfolio.HedgeFX(orderDate.DateTime, rebalancing_threshold * reference_aum)
                        
                        instruments.Values  //generate orders
                        |> Seq.toList
                        |> List.filter (fun instrument -> weightMap.ContainsKey(instrument.ID))
                        |> List.iter (fun instrument ->
                            let position = this.Portfolio.FindPosition(instrument, orderDate.DateTime)
                            let orders = this.Portfolio.FindOpenOrder(instrument, orderDate.DateTime, false)
                            let order = if orders = null || orders.Count = 0 then null else orders.Values  |> Seq.toList |> List.filter (fun o -> o.Type = OrderType.Market) |> List.reduce (fun acc o -> o) 
                                
                            let weight =
                                if weightMap.ContainsKey(instrument.ID) then 
                                    if ExposureFlag = 1 then
                                        let exp = this.Exposure(orderDate, instrument)
                                        if exp > 0.0 && weightMap.[instrument.ID] < 0.0 then
                                            weightMap.[instrument.ID] * exp
                                        else
                                            Math.Abs(weightMap.[instrument.ID]) * this.Exposure(orderDate, instrument) 
                                    else 
                                        weightMap.[instrument.ID]
                                else
                                    0.0

                            let weight = if Math.Abs(weight) < 1e-4 then 0.0 else weight
                            
                            let size_portfolio = (if instrument.InstrumentType = InstrumentType.Strategy && not ((instrument :?> Strategy).Portfolio = null) then (instrument :?> Strategy).Direction(orderDate.DateTime, (if weight >= 0.0 then DirectionType.Long else DirectionType.Short), true); Math.Abs(weight) else weight) * reference_aum// * reference_aum// * notional_strategy_adjustment
                            let adj = adjustStrategyAUM (instrument)

                            //let v = 
                            //    if instrument.InstrumentType = InstrumentType.Strategy && not ((instrument :?> Strategy).Portfolio = null) then
                            //        (instrument :?> Strategy).GetSODAUM(orderDate.DateTime, TimeSeriesType.Last)                            
                            //    else
                            //        instrument.[orderDate.DateTime, TimeSeriesType.Last, TimeSeriesRollType.Last]

                            //let size = CurrencyPair.Convert(size_portfolio, orderDate.DateTime, instrument.Currency, this.Currency) * adj//adjust_strategy_aum
                            // Console.WriteLine("Adj: " + adj.ToString() + " " + size_portfolio.ToString() + " " + (adj * size_portfolio).ToString())
                            let size = adj * size_portfolio / CurrencyPair.Convert(1.0, orderDate.DateTime, this.Currency, instrument.Currency)//adjust_strategy_aum
                            let instrument_value = (if instrument.InstrumentType = InstrumentType.Strategy && not((instrument :?> Strategy).Portfolio = null) then 1.0 else instrument.[orderDate.DateTime, TimeSeriesType.Last, TimeSeriesRollType.Last] * (if instrument :? Security then (instrument :?> Security).PointSize else 1.0))
                            let units = size / instrument_value

                            if instrument :? Security && liquidity_days_back > 1 && target_units_threshold > 0.0 then
                                let oldUnits = if not(position = null) then position.Unit else 0.0
                                let unit_diff = units - oldUnits
                                
                                this.AddMemoryPoint(orderDate.DateTime, unit_diff, (int)MemoryType.TargetUnits, instrument.ID)

                                
                                let adv =
                                    [|0 .. liquidity_days_back - 1|]
                                    |> Array.map(fun i -> 
                                            let volume = instrument.[orderDate.AddBusinessDays(-i).DateTime, TimeSeriesType.Volume, TimeSeriesRollType.Last]// vol_ts.[orderDate.AddBusinessDays(-i), TimeSeries.DataSearchType.Previous]
                                            let volume = if Double.IsNaN(volume) then 0.0 else volume
                                            volume)
                                    |> Array.average
                                let adv_max = adv * target_units_threshold

                                let units_max_ratio = Math.Min(1.0 , adv_max / Math.Abs(units))

                                let unit_diff = unit_diff * units_max_ratio
                                                                                                                                                        
                                this.AddMemoryPoint(orderDate.DateTime, unit_diff, (int)MemoryType.TargetUnits_Submitted, instrument.ID)

                                this.Portfolio.CreateOrder(instrument, orderDate.DateTime, unit_diff, OrderType.Market, 0.0) |> ignore
                            else
                                this.AddMemoryPoint(orderDate.DateTime, units, (int)MemoryType.TargetUnits, instrument.ID)
                                this.AddMemoryPoint(orderDate.DateTime, units, (int)MemoryType.TargetUnits_Submitted, instrument.ID)
                                if order |> isNull |> not then // if order exists then update
                                    if Portfolio.DebugPositions then
                                        "--Portfolio Strategy: " + size.ToString() + " " + orderDate.DateTime.ToString("yyyy-MM-dd HH:mm:ss.fff") |> Console.WriteLine
                                    order.UpdateTargetMarketOrder(orderDate.DateTime, size, UpdateType.OverrideNotional) |> ignore

                                elif position |> isNull |> not then // if position exists then update
                                    if Portfolio.DebugPositions then
                                        "--Portfolio Strategy: " + size.ToString() + " " + orderDate.DateTime.ToString("yyyy-MM-dd HH:mm:ss.fff") |> Console.WriteLine
                                    position.UpdateTargetMarketOrder(orderDate.DateTime, size, UpdateType.OverrideNotional) |> ignore

                                else
                                    this.Portfolio.CreateTargetMarketOrder(instrument, orderDate.DateTime, units) |> ignore)


    /// <summary>
    /// Function: Create a strategy based of a PortfolioStrategy Package
    /// </summary>    
    /// <param name="pkg">Package
    /// </param>
    member this.Package(calculate : bool) : MasterPkg_v1 =

        let t = this.Calendar.GetClosestBusinessDay(DateTime.Now, TimeSeries.DateSearchType.Next).DateTime
        let ts = this.GetTimeSeries(TimeSeriesType.Last)
        
        if this.InitialDate = DateTime.MinValue then
            this.InitialDate <- ts.DateTimes.[0]

        let days_back = (int)this.[t, (int)AQI.AQILabs.SDK.Strategies.MemoryType.DaysBack]

        let rec strategy_pkg (strategy : Strategy) : StrategyPkg_v1 =
            let instruments : InstrumentPkg_v1 list = 
            // let instruments : InstrumentPkg_v1 seq = 

                let positions = strategy.Portfolio.Positions(t, false) |> Seq.map(fun p -> (p.Instrument.ID, p)) |> Map.ofSeq
                let virpos = strategy.Portfolio.PositionOrders(t, false)
            
                strategy.Instruments(t, false).Values 
                |> Seq.filter(fun i -> not(i.InstrumentType = InstrumentType.Strategy && not((i :?> Strategy).Portfolio = null))) 
                |> Seq.filter(fun i -> not(strategy.Portfolio.IsReserve(i))) 
                |> Seq.toList
                |> List.map(fun i -> 
                // |> Seq.map(fun i -> 
                    let minlev = strategy.[t, i.ID, (int)AQI.AQILabs.SDK.Strategies.MemoryType.IndividualMinimumLeverage, TimeSeriesRollType.Last]
                    let maxlev = strategy.[t, i.ID, (int)AQI.AQILabs.SDK.Strategies.MemoryType.IndividualMaximumLeverage, TimeSeriesRollType.Last]
                    let grouplev = strategy.[t, i.ID, (int)AQI.AQILabs.SDK.Strategies.MemoryType.GroupLeverage, TimeSeriesRollType.Last]

                    let convlev = strategy.[t, (int)AQI.AQILabs.SDK.Strategies.MemoryType.ConvictionLevel, i.ID, TimeSeriesRollType.Last]

                    let minlev = if Double.IsNaN(minlev) then -100.0 else minlev
                    let maxlev = if Double.IsNaN(maxlev) then 100.0 else maxlev
                    let convlev_v = if Double.IsNaN(convlev) then None else Some(convlev)

                    let groupval = if Double.IsNaN(grouplev) || grouplev < -1000.0 then None else Some((int)grouplev)

                    let ts = i.GetTimeSeries(if i.InstrumentType = InstrumentType.ETF || i.InstrumentType = InstrumentType.Equity || i.InstrumentType = InstrumentType.Fund then TimeSeriesType.AdjClose else TimeSeriesType.Last)
                    let count = if ts = null then 0 else ts.Count
                    
                    let rets = 
                        let index = if ts = null then 0 else ts.GetClosestDateIndex(t, AQI.AQILabs.Kernel.Numerics.Util.TimeSeries.DateSearchType.Previous)
                        let rts = if not(ts = null || ts.Count < 5) then ts.GetRange(Math.Max(0, index - days_back), index) else ts
                        if rts = null || rts.Count < 5 then null else rts.LogReturn()
                                                            
                    {
                        ID = Some(i.ID)
                        Name = i.Name                        
                        Isin = (if i :? Security then Some((i :?> Security).Isin) else None)
                        Conviction = convlev_v
                        MinimumExposure = minlev
                        MaximumExposure = maxlev
                        GroupExposure = groupval
                        InitialWeight = 
                            
                            let unit = if positions.ContainsKey(i.ID) then positions.[i.ID].Unit else 0.0
                            if unit = 0.0 then
                                None
                            else
                                let fx = CurrencyPair.Convert(1.0, t, strategy.Currency, i.Currency)
                                let value = i.[t, TimeSeriesType.Last, TimeSeriesRollType.Last] * (if i :? AQI.AQILabs.Kernel.Security then (i :?> AQI.AQILabs.Kernel.Security).PointSize else 1.0)
                                let posvalue = value * unit * fx
                                Some(posvalue / strategy.Portfolio.MasterPortfolio.[t, TimeSeriesType.Last, TimeSeriesRollType.Last])

                        Meta = 
                            Some({
                                    Description = i.Description
                                    LongDescription = i.LongDescription
                                    Value = i.[t, TimeSeriesType.Last, TimeSeriesRollType.Last] * (if i :? AQI.AQILabs.Kernel.Security then (i :?> AQI.AQILabs.Kernel.Security).PointSize else 1.0)

                                    FX = CurrencyPair.Convert(1.0, t, strategy.Currency, i.Currency)

                                    CurrentUnit = if positions.ContainsKey(i.ID) then positions.[i.ID].Unit else 0.0
                                    ProjectedUnit = if virpos.ContainsKey(i.ID) then virpos.[i.ID].Unit else 0.0

                                    Volatility = if rets = null then 0.0 else Math.Sqrt(rets.Variance * 252.0)
                                    ValueAtRisk = Utils.VaRInstrument(i, t, strategy.Currency, 1, 60, 0.99)
                                    InformationRatio = if not(strategy :? PortfolioStrategy) then 0.0 else (strategy :?> PortfolioStrategy).InformationRatio(strategy.Calendar.GetClosestBusinessDay(t, TimeSeries.DateSearchType.Next), i, false)
                                    DrawDown = if ts = null || ts.Count < 5 then 0.0 else ts.[count - 1] / ts.Maximum - 1.0
        
                                    Currency = i.Currency.Name
                                    AssetClass = i.AssetClass.ToString()
                                    GeographicalRegion = i.GeographicalRegion.ToString()
                                    
                                    Return1D = if ts = null then 0.0 else ts.[count - 1] / ts.[Math.Max(0, count - 1 - 1)] - 1.0
                                    Return1W = if ts = null then 0.0 else ts.[count - 1] / ts.[Math.Max(0, count - 1 - 5)] - 1.0
                                    Return1M = if ts = null then 0.0 else ts.[count - 1] / ts.[Math.Max(0, count - 1 - 20)] - 1.0
                                    Return3M = if ts = null then 0.0 else ts.[count - 1] / ts.[Math.Max(0, count - 1 - 60)] - 1.0
                                    Return1Y = if ts = null then 0.0 else ts.[count - 1] / ts.[Math.Max(0, count - 1 - 260)] - 1.0
                            })
                    })

            let substrategies = 
                strategy.Instruments(t, false).Values 
                |> Seq.filter(fun i -> (i.InstrumentType = InstrumentType.Strategy && not((i :?> Strategy).Portfolio = null)))
                //|> Seq.filter(fun i -> i :? PortfolioStrategy)
                |> Seq.filter(fun i -> not(strategy.Portfolio.MasterPortfolio.Residual = (i :?> Strategy)))
                |> Seq.toList
                |> List.map(fun s -> strategy_pkg(s :?> Strategy))

            let target_vol = strategy.[t, (int)AQI.AQILabs.SDK.Strategies.MemoryType.TargetVolatility]
            let target_vol = if Double.IsNaN(target_vol) then 0.0 else target_vol

            let concentration_flag = (int) strategy.[t, (int)AQI.AQILabs.SDK.Strategies.MemoryType.ConcentrationFlag]
            let exposure_flag = (int) strategy.[t, (int)AQI.AQILabs.SDK.Strategies.MemoryType.ExposureFlag]

            let ind_max_lev = strategy.[t, (int)AQI.AQILabs.SDK.Strategies.MemoryType.IndividualMaximumLeverage]
            let ind_max_lev = if Double.IsNaN(ind_max_lev) then 1.0 else ind_max_lev
            let ind_min_lev = strategy.[t, (int)AQI.AQILabs.SDK.Strategies.MemoryType.IndividualMinimumLeverage]
            let ind_min_lev = if Double.IsNaN(ind_min_lev) then 0.0 else ind_min_lev
            
                        
            let mv_flag = (int) strategy.[t, (int)AQI.AQILabs.SDK.Strategies.MemoryType.MVOptimizationFlag]
            let fx_flag = (int) strategy.[t, (int)AQI.AQILabs.SDK.Strategies.MemoryType.FXHedgeFlag]

                        
            let target_var = strategy.[t, (int)AQI.AQILabs.SDK.Strategies.MemoryType.TargetVAR]
            let target_var = if Double.IsNaN(target_var) then 0.0 else target_var

            let glo_max_lev = strategy.[t, (int)AQI.AQILabs.SDK.Strategies.MemoryType.GlobalMaximumLeverage]
            let glo_max_lev = if Double.IsNaN(glo_max_lev) then 1.0 else glo_max_lev
            let glo_min_lev = strategy.[t, (int)AQI.AQILabs.SDK.Strategies.MemoryType.GlobalMinimumLeverage]
            let glo_min_lev = if Double.IsNaN(glo_min_lev) then 0.0 else glo_min_lev
            
            //let days_back = (int)strategy.[t, (int)AQI.AQILabs.SDK.Strategies.MemoryType.DaysBack]
            let reb_freq = (int)strategy.[t, (int)AQI.AQILabs.SDK.Strategies.MemoryType.RebalancingFrequency]

            let liquidity_days_back = (int)strategy.[t, (int)AQI.AQILabs.SDK.Strategies.MemoryType.LiquidityDaysBack]
            let liquidity_threshold = strategy.[t, (int)AQI.AQILabs.SDK.Strategies.MemoryType.LiquidityThreshold]
            let liquidity_execution_threshold = strategy.[t, (int)AQI.AQILabs.SDK.Strategies.MemoryType.TargetUnits_Threshold]


            let group_contraints = 
                let ids =
                    strategy.Instruments(t, false).Values 
                    
                    |> Seq.filter(fun i -> not(strategy.Portfolio.IsReserve(i))) 
                    |> Seq.toList
                    |> List.map(fun i -> i.ID)
                    
                ids
                |> List.toSeq
                |> Seq.map(fun id ->
                    let groupid =  strategy.[t, id, (int)MemoryType.GroupLeverage, TimeSeriesRollType.Last]
                    (id, groupid))
                |> Seq.groupBy(fun (id, groupid) -> groupid)
                |> Seq.map(fun (groupid, data) -> 
                        let dataMap = data |> Seq.map(fun (id, groupid) -> (id, groupid)) |> Map.ofSeq
                        let idvec = ids |> List.map(fun id -> if dataMap.ContainsKey(id) then 1.0 else 0.0)
                        let group_min =  strategy.[t, (int)groupid, (int)MemoryType.IndividualMinimumLeverage, TimeSeriesRollType.Last]
                        let group_max =  strategy.[t, (int)groupid, (int)MemoryType.IndividualMaximumLeverage, TimeSeriesRollType.Last]
                        ((int) groupid, group_min, group_max))
                |> Seq.filter(fun (groupid, gmin, gmax) -> not(Double.IsNaN(gmin)) && not(Double.IsNaN(gmax)))
                |> Seq.toList
                          
            let parent_memory =
                if strategy.Portfolio.ParentPortfolio = null then
                    None
                else
                    let ids = strategy.Portfolio.ParentPortfolio.Strategy.GetMemorySeriesIds()
                    Some
                            (ids
                            |> Seq.filter(fun pair -> pair.[0] = strategy.ID && not(pair.[1] = (int)AQI.AQILabs.SDK.Strategies.MemoryType.GroupLeverage))
                            |> Seq.map(fun pair ->
                                let v = strategy.Portfolio.ParentPortfolio.Strategy.[t, pair.[0], pair.[1]]
                                (v, pair.[1]))
                            |> Seq.toList)
                       
            let gid_value = 
                if strategy.Portfolio.ParentPortfolio = null then
                    None
                else
                    let gid = strategy.Portfolio.ParentPortfolio.Strategy.[t, strategy.ID, (int)AQI.AQILabs.SDK.Strategies.MemoryType.GroupLeverage]
                    if Double.IsNaN(gid) then None else Some((int) gid)
                            
            {
                ID = Some(strategy.ID)
                Name = strategy.Name.Replace("Account: ", "")
                Parameters = 
                    Some({
                            TargetVolatility = target_vol; MaximumVaR = target_var; DaysBack = days_back
                            Concentration = concentration_flag = 1; MeanVariance = mv_flag = 1                                         
                            FXHedge = fx_flag = 1
                            Frequency = reb_freq        
                            
                            LiquidityDaysBack = liquidity_days_back; LiquidityThreshold = liquidity_threshold; LiquidityExecutionThreshold = liquidity_execution_threshold
                        })
                SubStrategies = if substrategies |> List.length > 0 then Some(substrategies) else None                    
                Instruments = if instruments |> List.length > 0 then Some([this.InitialDate, instruments ]) else None
                MinimumExposure = glo_min_lev
                MaximumExposure = glo_max_lev
                
                GroupConstraints = if group_contraints |> List.length > 0 then Some(group_contraints |>  List.map(fun (id, min, max) -> { ID = id; MinimumExposure = min; MaximumExposure = max })) else None
                
                Risk = if strategy :? PortfolioStrategy && not(((strategy :?> PortfolioStrategy) :?> PortfolioStrategy).RiskFunctionName = null) then Some((strategy :?> PortfolioStrategy).RiskFunctionName) else None
                InformationRatio = if strategy :? PortfolioStrategy && not((strategy :?> PortfolioStrategy).InformationRatioFunctionName = null) then Some((strategy :?> PortfolioStrategy).InformationRatioFunctionName) else None
                IndicatorCalculation = if strategy :? PortfolioStrategy && not((strategy :?> PortfolioStrategy).IndicatorCalculationFunctionName = null) then Some((strategy :?> PortfolioStrategy).IndicatorCalculationFunctionName) else None
                WeightFilter = if strategy :? PortfolioStrategy && not((strategy :?> PortfolioStrategy).WeightFilterFunctionName = null) then Some((strategy :?> PortfolioStrategy).WeightFilterFunctionName) else None
                InstrumentFilter = if strategy :? PortfolioStrategy && not((strategy :?> PortfolioStrategy).InstrumentFilterFunctionName = null) then Some((strategy :?> PortfolioStrategy).InstrumentFilterFunctionName) else None
                Exposure = if strategy :? PortfolioStrategy && not((strategy :?> PortfolioStrategy).ExposureFunctionName = null) then Some((strategy :?> PortfolioStrategy).ExposureFunctionName) else None
                Analyse = if strategy :? PortfolioStrategy && not((strategy :?> PortfolioStrategy).AnalyseFunctionName = null) then Some((strategy :?> PortfolioStrategy).AnalyseFunctionName) else None

                GroupExposure = gid_value
                ParentMemory = parent_memory
                

                Meta = Some
                    ({
                        Description = strategy.Description.Replace("Account: ", "")
                        Value = strategy.[t, TimeSeriesType.Last, TimeSeriesRollType.Last]
                        CurrentAUM = 
                                    if strategy.Portfolio.ParentPortfolio = null then
                                        //strategy.GetAUM(t.Date, TimeSeriesType.Last)
                                        strategy.Portfolio.[t, TimeSeriesType.Last, TimeSeriesRollType.Last]
                                    else
                                        strategy.Portfolio.RiskNotional(t)
                                        
                        ProjectedAUM = //strategy.GetSODAUM(t, TimeSeriesType.Last)
                                    if strategy.Portfolio.ParentPortfolio = null then
                                        //strategy.GetSODAUM(t, TimeSeriesType.Last)
                                        strategy.Portfolio.[t, TimeSeriesType.Last, TimeSeriesRollType.Last]
                                    else
                                        strategy.Portfolio.PositionOrders(t, true).Values  
                                        |> Seq.filter(fun p -> not(strategy.Portfolio.IsReserve(p.Instrument)))                             
                                        |> Seq.map(fun p ->
                                            let instrument = p.Instrument                                                                                                                                                                                                                    
                                            let fx = CurrencyPair.Convert(1.0, t, TimeSeriesType.Close, DataProvider.DefaultProvider, TimeSeriesRollType.Last, strategy.Portfolio.Currency, instrument.Currency)
                                            p.Unit * instrument.[t, TimeSeriesType.Last, TimeSeriesRollType.Last] * fx * (if instrument :? Security then (instrument :?> Security).PointSize else 1.0))
                                        |> Seq.sum
                        TurnOver =
                            let instruments = strategy.Instruments(t, true)
                            let positions = strategy.Portfolio.Positions(t, true) |> Seq.map(fun p -> (p.Instrument.ID, p)) |> Map.ofSeq
                            let virpos = strategy.Portfolio.PositionOrders(t, true)
                            
                            instruments.Values
                            |> Seq.map(fun i -> 
                                    let current_pos = if positions.ContainsKey(i.ID) then positions.[i.ID].Unit else 0.0
                                    let projected_pos = if virpos.ContainsKey(i.ID) then virpos.[i.ID].Unit else 0.0
                                    let insvalue = i.[t, TimeSeriesType.Last, TimeSeriesRollType.Last] * (if i :? AQI.AQILabs.Kernel.Security then (i :?> AQI.AQILabs.Kernel.Security).PointSize else 1.0)

                                    let fx = CurrencyPair.Convert(1.0, t, TimeSeriesType.Close, DataProvider.DefaultProvider, TimeSeriesRollType.Last, strategy.Portfolio.Currency, i.Currency)
                                    //let fx = CurrencyPair.Convert(1.0, t, strategy.Currency, i.Currency)
                                    Math.Abs(current_pos - projected_pos) * fx * insvalue
                                )
                            |> Seq.sum                            
            
                        Cash =
                            {
                                Total = 
                                    {
                                        Currency = strategy.Currency.Name
                                        CurrentValue =
                                            strategy.Portfolio.Positions(t, true)
                                            |> Seq.filter(fun p -> strategy.Portfolio.IsReserve(p.Instrument))
                                            |> Seq.toList
                                            |> List.map(fun p ->
                                                    let fx = CurrencyPair.Convert(1.0, t, strategy.Currency, p.Instrument.Currency)
                                                    p.Unit * fx)
                                            |> List.sum
                                        
                                        ProjectedValue =
                                            let turn_over ccy =
                                                let instruments = strategy.Instruments(t, true).Values |> Seq.filter(fun i -> if ccy = "Total" then true else i.Currency.Name = ccy)
                                                let positions = strategy.Portfolio.Positions(t, true) |> Seq.map(fun p -> (p.Instrument.ID, p)) |> Map.ofSeq
                                                let virpos = strategy.Portfolio.PositionOrders(t, true)
                            
                                                instruments
                                                |> Seq.map(fun i -> 
                                                        let current_pos = if positions.ContainsKey(i.ID) then positions.[i.ID].Unit else 0.0
                                                        let projected_pos = if virpos.ContainsKey(i.ID) then virpos.[i.ID].Unit else 0.0
                                                        let insvalue = i.[t, TimeSeriesType.Last, TimeSeriesRollType.Last] * (if i :? AQI.AQILabs.Kernel.Security then (i :?> AQI.AQILabs.Kernel.Security).PointSize else 1.0)

                                                        let fx = 
                                                            if ccy = "Total" then 
                                                                CurrencyPair.Convert(1.0, t, strategy.Currency, i.Currency)
                                                            else 
                                                                1.0
                                                        
                                                        (current_pos - projected_pos) * fx * insvalue
                                                    )
                                                |> Seq.sum  
                                            let total =
                                                let positions = strategy.Portfolio.Positions(t, true)   
                                                positions
                                                |> Seq.filter(fun p -> strategy.Portfolio.IsReserve(p.Instrument))
                                                |> Seq.toList
                                                |> List.map(fun p ->
                                                        let fx = CurrencyPair.Convert(1.0, t, strategy.Currency, p.Instrument.Currency)
                                                
                                                        p.Unit * fx)
                                                |> List.sum
                                            total + (turn_over "Total")

                                        
                                        Rates = 
                                            let zeroCurveFactory = AQI.AQILabs.Derivatives.IRZeroCurveCollection.GetCollection(strategy.Currency)
                                            let date = strategy.Calendar.GetClosestBusinessDay(t, TimeSeries.DateSearchType.Previous)
                                            let zeroCurve = zeroCurveFactory.GetCurve(date)
                                                        
                                            {
                                                Rate1D = zeroCurve.ZeroRate(date.AddBusinessDays(1))
                                                Rate1W = zeroCurve.ZeroRate(date.AddBusinessDays(5))
                                                Rate1M = zeroCurve.ZeroRate(date.AddMonths(1, TimeSeries.DateSearchType.Previous))
                                                Rate3M = zeroCurve.ZeroRate(date.AddMonths(3, TimeSeries.DateSearchType.Previous))
                                                Rate6M = zeroCurve.ZeroRate(date.AddMonths(6, TimeSeries.DateSearchType.Previous))
                                                Rate1Y = zeroCurve.ZeroRate(date.AddYears(1, TimeSeries.DateSearchType.Previous))
                                                Rate3Y = zeroCurve.ZeroRate(date.AddYears(3, TimeSeries.DateSearchType.Previous))
                                                Rate5Y = zeroCurve.ZeroRate(date.AddYears(5, TimeSeries.DateSearchType.Previous))
                                            }                                            
                                    }
                                Currencies =                                        
                                        strategy.Portfolio.Positions(t, true)
                                        |> Seq.filter(fun p -> strategy.Portfolio.IsReserve(p.Instrument))                                                                       
                                        |> Seq.map(fun p -> p.Instrument.Currency.Name)
                                        |> Seq.append(
                                               strategy.Portfolio.PositionOrders(t, true).Values
                                               |> Seq.filter(fun p -> not(p.Unit = 0.0))
                                              
                                               |> Seq.map(fun p -> p.Instrument.Currency.Name)
                                            )
                                        
                                        |> Seq.distinct
                                        |> Seq.toList
                                        |> List.map(fun name ->
                                                {
                                                    Currency = name
                                                    CurrentValue =
                                                        strategy.Portfolio.Positions(t, true)
                                                        |> Seq.filter(fun p -> strategy.Portfolio.IsReserve(p.Instrument) && p.Instrument.Currency.Name = name)
                                                        |> Seq.toList
                                                        |> List.map(fun p ->                                                                
                                                                p.Unit)
                                                        |> List.sum
                                        
                                                    ProjectedValue =
                                                        let turn_over ccy =
                                                            let instruments = strategy.Instruments(t, true).Values |> Seq.filter(fun i -> if ccy = "Total" then true else i.Currency.Name = ccy)
                                                            let positions = strategy.Portfolio.Positions(t, true) |> Seq.map(fun p -> (p.Instrument.ID, p)) |> Map.ofSeq
                                                            let virpos = strategy.Portfolio.PositionOrders(t, true)
                            
                                                            instruments 
                                                            |> Seq.map(fun i -> 
                                                                    let current_pos = if positions.ContainsKey(i.ID) then positions.[i.ID].Unit else 0.0
                                                                    let projected_pos = if virpos.ContainsKey(i.ID) then virpos.[i.ID].Unit else 0.0
                                                                    let insvalue = i.[t, TimeSeriesType.Last, TimeSeriesRollType.Last] * (if i :? AQI.AQILabs.Kernel.Security then (i :?> AQI.AQILabs.Kernel.Security).PointSize else 1.0)

                                                                    let fx = 
                                                                        if ccy = "Total" then 
                                                                            CurrencyPair.Convert(1.0, t, strategy.Currency, i.Currency)
                                                                        else 
                                                                            1.0
                                                        
                                                                    (current_pos - projected_pos) * fx * insvalue
                                                                )
                                                            |> Seq.sum  
                                                        let total =
                                                            let positions = strategy.Portfolio.Positions(t, true)   
                                                            positions
                                                            |> Seq.filter(fun p -> strategy.Portfolio.IsReserve(p.Instrument) && p.Instrument.Currency.Name = name)
                                                            |> Seq.toList
                                                            |> List.map(fun p ->                                                
                                                                    p.Unit)
                                                            |> List.sum
                                                        total + (turn_over name)

                                                    Rates = 
                                                        let zeroCurveFactory = AQI.AQILabs.Derivatives.IRZeroCurveCollection.GetCollection(Currency.FindCurrency(name))
                                                        let date = strategy.Calendar.GetClosestBusinessDay(t, TimeSeries.DateSearchType.Previous)
                                                        let zeroCurve = zeroCurveFactory.GetCurve(date)
                                                        
                                                        {
                                                            Rate1D = zeroCurve.ZeroRate(date.AddBusinessDays(1))
                                                            Rate1W = zeroCurve.ZeroRate(date.AddBusinessDays(5))
                                                            Rate1M = zeroCurve.ZeroRate(date.AddMonths(1, TimeSeries.DateSearchType.Previous))
                                                            Rate3M = zeroCurve.ZeroRate(date.AddMonths(3, TimeSeries.DateSearchType.Previous))
                                                            Rate6M = zeroCurve.ZeroRate(date.AddMonths(6, TimeSeries.DateSearchType.Previous))
                                                            Rate1Y = zeroCurve.ZeroRate(date.AddYears(1, TimeSeries.DateSearchType.Previous))
                                                            Rate3Y = zeroCurve.ZeroRate(date.AddYears(3, TimeSeries.DateSearchType.Previous))
                                                            Rate5Y = zeroCurve.ZeroRate(date.AddYears(5, TimeSeries.DateSearchType.Previous))
                                                        }
                                                }
                                            )
                            }

                        CurrentRisk = 
                            {
                                Volatility = Utils.Volatility(strategy, t, days_back, true)
                                ValueAtRisk = Utils.VaR(strategy, t, 1, 60, 0.99, true)
                                InformationRatio = if not(strategy :? PortfolioStrategy) then 0.0 else (strategy :?> PortfolioStrategy).InformationRatio(strategy.Calendar.GetClosestBusinessDay(t, TimeSeries.DateSearchType.Previous), null, true)
                                RiskNotional =
                                    strategy.Portfolio.Positions(t, true) 
                                    |> Seq.filter (fun i -> not(strategy.Portfolio.IsReserve(i.Instrument)))
                                    |> Seq.map(fun p ->
                                        let unit = p.Unit * (if p.Instrument :? Security then (p.Instrument :?> Security).PointSize else 1.0)                                                                    
                                        //let value = p.Instrument.[t, (if p.Instrument.InstrumentType = InstrumentType.Strategy then TimeSeriesType.Last else TimeSeriesType.Close), TimeSeriesRollType.Last]
                                        let value = p.Instrument.[t, TimeSeriesType.Last, TimeSeriesRollType.Last]

                                        //let fx = CurrencyPair.Convert(1.0, t, TimeSeriesType.Close, DataProvider.DefaultProvider, TimeSeriesRollType.Last, strategy.Portfolio.Currency, p.Instrument.Currency)
                                        let fx = CurrencyPair.Convert(1.0, t, TimeSeriesType.Last, DataProvider.DefaultProvider, TimeSeriesRollType.Last, strategy.Portfolio.Currency, p.Instrument.Currency)

                                        let weight = fx * unit * value
 
                                        weight)
                                    |> Seq.sum     
                            }

                        ProjectedRisk =
                            {
                                Volatility = Utils.Volatility(strategy, t, days_back, false)
                                ValueAtRisk = Utils.VaR(strategy, t, 1, 60, 0.99, false)                                
                                InformationRatio = if not(strategy :? PortfolioStrategy) then 0.0 else (strategy :?> PortfolioStrategy).InformationRatio(strategy.Calendar.GetClosestBusinessDay(t, TimeSeries.DateSearchType.Previous), null, false)
                                RiskNotional =
                                    strategy.Portfolio.PositionOrders(t, true).Values 
                                    |> Seq.filter (fun i -> not(strategy.Portfolio.IsReserve(i.Instrument)))
                                    |> Seq.map(fun p ->
                                        let unit = p.Unit * (if p.Instrument :? Security then (p.Instrument :?> Security).PointSize else 1.0)                                                                    
                                        //let value = p.Instrument.[t, (if p.Instrument.InstrumentType = InstrumentType.Strategy then TimeSeriesType.Last else TimeSeriesType.Close), TimeSeriesRollType.Last]
                                        let value = p.Instrument.[t, TimeSeriesType.Last, TimeSeriesRollType.Last]

                                        let fx = CurrencyPair.Convert(1.0, t, TimeSeriesType.Close, DataProvider.DefaultProvider, TimeSeriesRollType.Last, strategy.Portfolio.Currency, p.Instrument.Currency)

                                        let weight = fx * unit * value
 
                                        weight)
                                    |> Seq.sum
                            }  
                            
                        Active = this.IsSchedulerStarted
                    })
            }
        
        if calculate then
            this.Tree.ClearOrders(t, true)
            this.Tree.ClearMemory(t)
            this.Tree.ExecuteLogic(t, true)
            this.Tree.PostExecuteLogic(t)

        let initialValue = this.Portfolio.[t, TimeSeriesType.Last, TimeSeriesRollType.Last]//ts.[0]
        //let initialValue = ts.[0]
        this.Simulating <- true
        let strat = strategy_pkg(this)
                   
        if calculate then
            this.Tree.ClearOrders(t, true)
            this.Tree.ClearMemory(t)

        this.Simulating <- false

        let fixedNotional = this.[t, (int)AQI.AQILabs.SDK.Strategies.MemoryType.FixedNotional]
        let fixedNotional = if Double.IsNaN(fixedNotional) then 0.0 else fixedNotional
            
            
        {
            Code = Some(this.ScriptCode)
            Strategy = strat
            RateCompounding = if this.RateCompoundingFunctionName = null then None else Some(this.RateCompoundingFunctionName)
            
            InitialDate = t//this.InitialDate
            InitialValue = initialValue
            FixedNotional = if fixedNotional > 0.0 then Some(fixedNotional) else None
            Residual = not(this.Portfolio.Residual = null)
            Simulated = true
            Currency = this.Currency.Name
            ScheduleCommand = this.ScheduleCommand
            Instructions = None            
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
    /// <param name="portfolio">Portfolio used by the strategy
    /// </param>
    /// <param name="underlyings">List of assets in the portfolio
    /// </param>
    /// <param name="fractionContract">True if fractional contracts are allowed. False if units of contracts are rounded to the closest interger.
    /// </param>
    static member public CreateStrategy(instrument : Instrument, initialDate : BusinessDay, initialValue : double, portfolio : Portfolio, underlyings : System.Collections.Generic.List<Instrument>) : PortfolioStrategy =
        match instrument with
        | x when x.InstrumentType = InstrumentType.Strategy ->
            let Strategy = new PortfolioStrategy(instrument)

            Strategy.Startup(initialDate, initialValue, portfolio)
            if not (underlyings = null) then underlyings |> Seq.toList |> List.iter (fun strategy -> Strategy.AddInstrument(strategy, initialDate.DateTime))
                                                            
            Strategy
        | _ -> raise (new Exception("Instrument not a Strategy"))


    /// <summary>
    /// Function: Create a strategy
    /// </summary>    
    /// <param name="name">Name
    /// </param>
    /// <param name="description">Description
    /// </param>
    /// <param name="startDate">Creating date
    /// </param>
    /// <param name="startValue">Starting NAV and portfolio AUM.
    /// </param>
    /// <param name="parent">Portfolio used by the parent strategy
    /// </param>
    /// <param name="simulated">True if not stored persistently.
    /// </param>
    /// <param name="fractionContract">True if fractional contracts are allowed. False if units of contracts are rounded to the closest interger.
    /// </param>
    /// <param name="cloud">True if the strategy is cloud hosted.
    /// </param>
    static member public Create(name : string, description : string, startDate : DateTime, startValue : double, parent : Portfolio, currency : Currency , simulated : Boolean , residual : Boolean, cloud : Boolean) : PortfolioStrategy =
            let calendar = Calendar.FindCalendar("All")

            let date = calendar.GetClosestBusinessDay(startDate, TimeSeries.DateSearchType.Previous)

            let strategy_funding = FundingType.TotalReturn

            if (parent = null) then            
                let usd_currency = if currency = null then Currency.FindCurrency("USD") else currency
                
                let usd_cash_strategy = Instrument.FindInstrument(usd_currency.Name + " - Cash")                
                let main_currency = usd_currency
                let main_cash_strategy = usd_cash_strategy

                // Master Strategy Portfolios
                let master_portfolio_instrument = Instrument.CreateInstrument(name + "/Portfolio", InstrumentType.Portfolio, description + " Strategy Portfolio", main_currency, FundingType.TotalReturn, simulated, cloud)
                let master_portfolio = Portfolio.CreatePortfolio(master_portfolio_instrument, main_cash_strategy, main_cash_strategy, null)                
                master_portfolio.TimeSeriesRoll <- TimeSeriesRollType.Last;
                master_portfolio.AddReserve(usd_currency, usd_cash_strategy, usd_cash_strategy)

                // Master Strategy Residual Portfolios
                let residual_portfolio =
                                        if residual then
                                            let residual_portfolio_instrument = Instrument.CreateInstrument(name + "/Residual/Portfolio", InstrumentType.Portfolio, description + " Portfolio Residual", main_currency, FundingType.TotalReturn, simulated, cloud)
                                            let residual_portfolio = Portfolio.CreatePortfolio(residual_portfolio_instrument, main_cash_strategy, main_cash_strategy, master_portfolio)                
                                            residual_portfolio.TimeSeriesRoll <- TimeSeriesRollType.Last;
                                            residual_portfolio.AddReserve(usd_currency, usd_cash_strategy, usd_cash_strategy)
                                            residual_portfolio
                                        else
                                            null
                
                Currency.Currencies
                |> Seq.iter (fun x_currency -> //ccy_name ->
                    let x_cash_strategy = Instrument.FindInstrument(x_currency.Name + " - Cash")
                    if not(x_cash_strategy = null) then
                        master_portfolio.AddReserve(x_currency, x_cash_strategy, x_cash_strategy)
                        if not(residual_portfolio = null) then
                            residual_portfolio.AddReserve(x_currency, x_cash_strategy, x_cash_strategy))
                
                master_portfolio.Reserves             
                |> List.ofSeq 
                |> List.filter (fun (instrument : Instrument) -> instrument.InstrumentType = InstrumentType.Strategy) 
                |> List.iter (fun strategy -> (strategy :?> Strategy).NAVCalculation(date) |> ignore)

                // Master Strategy Instruments, Strategies
                let master_strategy_instrument = Instrument.CreateInstrument(name, InstrumentType.Strategy, description, main_currency, strategy_funding, simulated, cloud)
                master_strategy_instrument.TimeSeriesRoll <- master_portfolio.TimeSeriesRoll
                let master_strategy = PortfolioStrategy.CreateStrategy(master_strategy_instrument, date, startValue, master_portfolio, new Collections.Generic.List<Instrument>())
                master_strategy.Calendar <- calendar
                master_portfolio.Strategy <- master_strategy

                // Residual Strategy Instruments, Strategies
                if not(residual_portfolio = null) then
                    let residual_strategy_instrument = Instrument.CreateInstrument(name + "/Residual", InstrumentType.Strategy, description + "/Residual", main_currency, strategy_funding, simulated, cloud)
                    residual_strategy_instrument.TimeSeriesRoll <- residual_portfolio.TimeSeriesRoll
                    let residual_strategy = PortfolioStrategy.CreateStrategy(residual_strategy_instrument, date, startValue, residual_portfolio, new Collections.Generic.List<Instrument>())
                    residual_strategy.Calendar <- calendar
                    residual_portfolio.Strategy <- residual_strategy
                
                    master_strategy.AddInstrument(residual_strategy, date.DateTime)
                    master_portfolio.Residual <- residual_strategy

                master_strategy.InitialDate <- date.DateTime
                master_strategy
            
            else            
                // Master Strategy Portfolios
                let master_portfolio_instrument = Instrument.CreateInstrument(name + "/Portfolio", InstrumentType.Portfolio, description + " Strategy Portfolio", parent.Currency, FundingType.TotalReturn, simulated, cloud);
                let master_portfolio = Portfolio.CreatePortfolio(master_portfolio_instrument, parent.LongReserve, parent.ShortReserve, parent);
                master_portfolio.TimeSeriesRoll <- TimeSeriesRollType.Last;

                parent.Reserves
                |> Seq.toList
                |> List.iter (fun reserve ->
                    master_portfolio.AddReserve(reserve.Currency, parent.Reserve(reserve.Currency, PositionType.Long), parent.Reserve(reserve.Currency, PositionType.Short)))

                // Master Strategy Instruments, Strategies
                let master_strategy_instrument = Instrument.CreateInstrument(name, InstrumentType.Strategy, description, parent.Currency, strategy_funding, simulated, cloud)
                master_strategy_instrument.TimeSeriesRoll <- master_portfolio.TimeSeriesRoll
                let master_strategy = PortfolioStrategy.CreateStrategy(master_strategy_instrument, date, startValue, master_portfolio, null)
                master_strategy.Calendar <- calendar
                master_portfolio.Strategy <- master_strategy
                        
                master_strategy.InitialDate <- date.DateTime
                master_strategy



    /// <summary>
    /// Function: Create a strategy based of a PortfolioStrategy Package
    /// </summary>    
    /// <param name="pkg">Package
    /// </param>
    static member public Create(pkg : MasterPkg_v05, code : string option) =
        let strategy_pkg = pkg.Strategy

        let result =
            try
                let fsi = System.Reflection.Assembly.Load("FSI-ASSEMBLY")
                ""
            with _ ->
                if code.IsSome then
                    Utils.RegisterCode (true, true) [pkg.Strategy.Name, code.Value]
                elif pkg.Code.IsSome then
                    Utils.RegisterCode (true, true) [pkg.Strategy.Name, pkg.Code.Value]
                else
                    ""

        let calendar = Calendar.FindCalendar("All")
        let ccy = Currency.FindCurrency(pkg.Currency)
        let firstDate = pkg.InitialDate
       
        let initialDay = calendar.GetClosestBusinessDay(firstDate, TimeSeries.DateSearchType.Previous)

        let uid = System.Guid.NewGuid().ToString()

        let master_parameters = strategy_pkg.Parameters

//        let createFutureStrategy (name : string) (underlyingInstrument : Instrument) (master : Portfolio) =                                                                       
//            let strategy = AQI.AQILabs.SDK.Strategies.RollingFutureStrategy.Create(name, name, startDate, startValue, underlyingInstrument, contract, rollDay, currency, master, simulated, false)
//            if not(master = null) then
//                master.Strategy.AddInstrument(strategy, startDate)
//            strategy

        let rec createStrategy (strategy_pkg : StrategyPkg_v05) (uid : string) (parent : Portfolio) : PortfolioStrategy = 
            let uid = uid + "/" + strategy_pkg.Name

            Console.WriteLine("Creating Strategy: " + strategy_pkg.Name)

            let strategy = PortfolioStrategy.Create(uid, strategy_pkg.Name, initialDay.DateTime, pkg.InitialValue, parent, ccy, pkg.Simulated, pkg.Residual, false)
                
            let _parameters = if strategy_pkg.Parameters.IsSome then strategy_pkg.Parameters else master_parameters

            if _parameters.IsSome then
                let parameters = _parameters.Value
                let addPoint x y =
                    strategy.AddMemoryPoint(initialDay.DateTime, x, (int)y)
            
                addPoint parameters.TargetVolatility MemoryType.TargetVolatility
                addPoint (if parameters.TargetVolatility > 0.0 then 1.0 else 0.0) MemoryType.GlobalTargetVolatilityFlag
                addPoint (if parameters.TargetVolatility > 0.0 then 1.0 else 0.0) MemoryType.IndividialTargetVolatilityFlag
                addPoint (if parameters.Concentration then 1.0 else 0.0) MemoryType.ConcentrationFlag
                addPoint (if strategy_pkg.Exposure.IsSome then 1.0 else 0.0) MemoryType.ExposureFlag
                addPoint -0.25 MemoryType.ExposureThreshold

                addPoint 100.0 MemoryType.IndividualMaximumLeverage
                addPoint -100.0 MemoryType.IndividualMinimumLeverage
            
                addPoint (if parameters.MeanVariance then 1.0 else 0.0) MemoryType.MVOptimizationFlag
                addPoint (if parameters.FXHedge then 1.0 else 0.0) MemoryType.FXHedgeFlag

                addPoint parameters.MaximumVaR MemoryType.TargetVAR
                addPoint (if parameters.MaximumVaR < 0.0 then 1.0 else 0.0) MemoryType.GlobalTargetVARFlag
    
                addPoint strategy_pkg.MaximumExposure MemoryType.GlobalMaximumLeverage
                addPoint strategy_pkg.MinimumExposure MemoryType.GlobalMinimumLeverage        
                addPoint ((float) parameters.DaysBack) MemoryType.DaysBack

                addPoint ((float) parameters.Frequency) AQI.AQILabs.SDK.Strategies.MemoryType.RebalancingFrequency
                addPoint 0.0 MemoryType.RebalancingThreshold

            strategy.InitialDate <- initialDay.DateTime

            if strategy_pkg.Instruments.IsSome then
                let t1 = DateTime.Now
                Console.WriteLine("Adding Instruments: " + strategy.Description + " / " + t1.ToString())
                strategy_pkg.Instruments.Value
                |> List.iteri(fun num instrument_pkg ->
                        let instrument = Instrument.FindInstrument(instrument_pkg.Name)
                        //Console.WriteLine("Adding Instrument: " + instrument.Name + " to " + strategy.Description)
                        strategy.AddInstrument(instrument, initialDay.DateTime, num)
                        strategy.AddMemoryPoint(initialDay.DateTime, instrument_pkg.MinimumExposure, instrument.ID, (int)AQI.AQILabs.SDK.Strategies.MemoryType.IndividualMinimumLeverage)
                        strategy.AddMemoryPoint(initialDay.DateTime, instrument_pkg.MaximumExposure, instrument.ID, (int)AQI.AQILabs.SDK.Strategies.MemoryType.IndividualMaximumLeverage)
                    
                        if instrument_pkg.GroupExposure.IsSome then
                            strategy.AddMemoryPoint(initialDay.DateTime, (float) instrument_pkg.GroupExposure.Value, instrument.ID, (int)AQI.AQILabs.SDK.Strategies.MemoryType.GroupLeverage)
                    )
                Console.WriteLine("Done Adding Instruments: " + strategy.Description + " / " + (DateTime.Now - t1).ToString())

            if strategy_pkg.SubStrategies.IsSome then
                strategy_pkg.SubStrategies.Value
                |> List.iter(fun substrategy_pkg ->
                        let substrategy = createStrategy substrategy_pkg uid (strategy.Portfolio)

                        Console.WriteLine("Adding Subs-strategy: " + substrategy.Description + " to " + strategy.Description)

                        strategy.AddMemoryPoint(initialDay.DateTime, substrategy_pkg.MinimumExposure, substrategy.ID, (int)AQI.AQILabs.SDK.Strategies.MemoryType.IndividualMinimumLeverage)
                        strategy.AddMemoryPoint(initialDay.DateTime, substrategy_pkg.MaximumExposure, substrategy.ID, (int)AQI.AQILabs.SDK.Strategies.MemoryType.IndividualMaximumLeverage)

                        if substrategy_pkg.ParentMemory.IsSome then
                            substrategy_pkg.ParentMemory.Value
                            |> List.iter(fun (value, id) -> strategy.AddMemoryPoint(initialDay.DateTime, value, substrategy.ID, id))

                        strategy.AddInstrument(substrategy, initialDay.DateTime)
                        if substrategy_pkg.GroupExposure.IsSome then
                            strategy.AddMemoryPoint(DateTime.MinValue, (float) substrategy_pkg.GroupExposure.Value, substrategy.ID, (int)AQI.AQILabs.SDK.Strategies.MemoryType.GroupLeverage)
                    )

            if strategy_pkg.GroupConstraints.IsSome then
                strategy_pkg.GroupConstraints.Value
                |> List.iter(fun groupc ->
                        strategy.AddMemoryPoint(DateTime.MinValue, groupc.MinimumExposure, groupc.ID, (int)AQI.AQILabs.SDK.Strategies.MemoryType.IndividualMinimumLeverage)
                        strategy.AddMemoryPoint(DateTime.MinValue, groupc.MaximumExposure, groupc.ID, (int)AQI.AQILabs.SDK.Strategies.MemoryType.IndividualMaximumLeverage)
                    )

            
            if strategy_pkg.InformationRatio.IsSome then
                //strategy._initialized <- true
                strategy.InformationRatioFunctionName <- strategy_pkg.InformationRatio.Value
                //strategy._initialized <- false

            if strategy_pkg.WeightFilter.IsSome then
                //strategy._initialized <- true
                strategy.WeightFilterFunctionName <- strategy_pkg.WeightFilter.Value
                //strategy._initialized <- false

            if strategy_pkg.IndicatorCalculation.IsSome then
                //strategy._initialized <- true
                strategy.IndicatorCalculationFunctionName <- strategy_pkg.IndicatorCalculation.Value
                //strategy._initialized <- false

            if strategy_pkg.Risk.IsSome then
                //strategy._initialized <- true
                strategy.RiskFunctionName <- strategy_pkg.Risk.Value
                //strategy._initialized <- false

            if strategy_pkg.Exposure.IsSome then
                //strategy._initialized <- true
                strategy.ExposureFunctionName <- strategy_pkg.Exposure.Value
                //strategy._initialized <- false


            if strategy_pkg.Analyse.IsSome then
                //strategy._initialized <- true
                strategy.AnalyseFunctionName <- strategy_pkg.Analyse.Value
                //strategy._initialized <- false

            strategy
        
        let strategy = createStrategy strategy_pkg uid null

        if code.IsSome then
            //strategy._initialized <- true
            strategy.ScriptCode <- code.Value
            //strategy._initialized <- false

        elif pkg.Code.IsSome then
            //strategy._initialized <- true
            strategy.ScriptCode <- pkg.Code.Value
            //strategy._initialized <- false

        if pkg.RateCompounding.IsSome then
            //strategy._initialized <- true
            strategy.RateCompoundingFunctionName <- pkg.RateCompounding.Value
            //strategy._initialized <- false

        if pkg.Instructions.IsSome then
            pkg.Instructions.Value
            |> List.iter(fun instruction_pkg ->
                    let instrument = if instruction_pkg.Instrument.IsSome then Instrument.FindInstrument(instruction_pkg.Instrument.Value) else null
                    
                    let Destination = if instruction_pkg.Destination.IsSome then instruction_pkg.Destination.Value else ""
                    let Account = if instruction_pkg.Account.IsSome then instruction_pkg.Account.Value else ""
                    let ExecutionFee = if instruction_pkg.ExecutionFee.IsSome then instruction_pkg.ExecutionFee.Value else 0.0
                    let MinimumExecutionFee = if instruction_pkg.MinimumExecutionFee.IsSome then instruction_pkg.MinimumExecutionFee.Value else 0.0
                    let MaximumExecutionFee = if instruction_pkg.MaximumExecutionFee.IsSome then instruction_pkg.MaximumExecutionFee.Value else 0.0
                    let MinimumSize = if instruction_pkg.MinimumSize.IsSome then instruction_pkg.MinimumSize.Value else 0.0
                    let MinimumStep = if instruction_pkg.MinimumStep.IsSome then instruction_pkg.MinimumStep.Value else 0.0
                    let Margin = if instruction_pkg.Margin.IsSome then instruction_pkg.Margin.Value else 0.0

                    Market.AddInstruction(new Instruction(strategy.Portfolio, instrument, instruction_pkg.Client, Destination, Account, ExecutionFee, MinimumExecutionFee, MaximumExecutionFee, MinimumSize, MinimumStep, Margin))
                )

        (strategy, result)

    /// <summary>
    /// Function: Create a strategy based of a PortfolioStrategy Package
    /// </summary>    
    /// <param name="pkg">Package
    /// </param>
    static member public Create(pkg : MasterPkg_v06, code : string option) =
        let strategy_pkg = pkg.Strategy

        let result =
            try
                let fsi = System.Reflection.Assembly.Load("FSI-ASSEMBLY")
                ""
            with _ ->
                if code.IsSome then
                    Utils.RegisterCode (true, true) [pkg.Strategy.Name, code.Value]
                elif pkg.Code.IsSome then
                    Utils.RegisterCode (true, true) [pkg.Strategy.Name, pkg.Code.Value]
                else
                    ""

        let calendar = Calendar.FindCalendar("All")
        let ccy = Currency.FindCurrency(pkg.Currency)
        let firstDate = pkg.InitialDate
       
        let initialDay = calendar.GetClosestBusinessDay(firstDate, TimeSeries.DateSearchType.Previous)

        let uid = System.Guid.NewGuid().ToString()

        let master_parameters = strategy_pkg.Parameters

//        let createFutureStrategy (name : string) (underlyingInstrument : Instrument) (master : Portfolio) =                                                                       
//            let strategy = AQI.AQILabs.SDK.Strategies.RollingFutureStrategy.Create(name, name, startDate, startValue, underlyingInstrument, contract, rollDay, currency, master, simulated, false)
//            if not(master = null) then
//                master.Strategy.AddInstrument(strategy, startDate)
//            strategy

        let rec createStrategy (strategy_pkg : StrategyPkg_v06) (uid : string) (parent : Portfolio) : PortfolioStrategy = 
            let uid = uid + "/" + strategy_pkg.Name

            Console.WriteLine("Creating Strategy: " + strategy_pkg.Name)

            let strategy = PortfolioStrategy.Create(uid, strategy_pkg.Name, initialDay.DateTime, pkg.InitialValue, parent, ccy, pkg.Simulated, pkg.Residual, false)
                            
            let _parameters = if strategy_pkg.Parameters.IsSome then strategy_pkg.Parameters else master_parameters

            if _parameters.IsSome then
                let parameters = _parameters.Value
                let addPoint x y =
                    strategy.AddMemoryPoint(initialDay.DateTime, x, (int)y)
            
                addPoint parameters.TargetVolatility MemoryType.TargetVolatility
                addPoint (if parameters.TargetVolatility > 0.0 then 1.0 else 0.0) MemoryType.GlobalTargetVolatilityFlag
                addPoint (if parameters.TargetVolatility > 0.0 then 1.0 else 0.0) MemoryType.IndividialTargetVolatilityFlag
                addPoint (if parameters.Concentration then 1.0 else 0.0) MemoryType.ConcentrationFlag
                addPoint (if strategy_pkg.Exposure.IsSome then 1.0 else 0.0) MemoryType.ExposureFlag
                addPoint -0.25 MemoryType.ExposureThreshold

                addPoint 100.0 MemoryType.IndividualMaximumLeverage
                addPoint -100.0 MemoryType.IndividualMinimumLeverage
            
                addPoint (if parameters.MeanVariance then 1.0 else 0.0) MemoryType.MVOptimizationFlag
                addPoint (if parameters.FXHedge then 1.0 else 0.0) MemoryType.FXHedgeFlag

                addPoint parameters.MaximumVaR MemoryType.TargetVAR
                addPoint (if parameters.MaximumVaR < 0.0 then 1.0 else 0.0) MemoryType.GlobalTargetVARFlag
    
                addPoint strategy_pkg.MaximumExposure MemoryType.GlobalMaximumLeverage
                addPoint strategy_pkg.MinimumExposure MemoryType.GlobalMinimumLeverage        
                addPoint ((float) parameters.DaysBack) MemoryType.DaysBack

                addPoint ((float) parameters.Frequency) AQI.AQILabs.SDK.Strategies.MemoryType.RebalancingFrequency
                addPoint 0.0 MemoryType.RebalancingThreshold

            strategy.InitialDate <- initialDay.DateTime

            if strategy_pkg.Instruments.IsSome then
                let t1 = DateTime.Now
                Console.WriteLine("Adding Instruments: " + strategy.Description + " / " + t1.ToString())
                strategy_pkg.Instruments.Value
                |> List.iteri(fun num instrument_pkg ->
                        let instrument = Instrument.FindInstrument(instrument_pkg.Name)
                        //Console.WriteLine("Adding Instrument: " + instrument.Name + " to " + strategy.Description)
                        strategy.AddInstrument(instrument, initialDay.DateTime, num)
                        strategy.AddMemoryPoint(initialDay.DateTime, instrument_pkg.MinimumExposure, instrument.ID, (int)AQI.AQILabs.SDK.Strategies.MemoryType.IndividualMinimumLeverage)
                        strategy.AddMemoryPoint(initialDay.DateTime, instrument_pkg.MaximumExposure, instrument.ID, (int)AQI.AQILabs.SDK.Strategies.MemoryType.IndividualMaximumLeverage)
                    
                        if instrument_pkg.GroupExposure.IsSome then
                            instrument_pkg.GroupExposure.Value
                            |> List.iteri(fun i ge -> 
                                        let code = Int32.Parse(((int)AQI.AQILabs.SDK.Strategies.MemoryType.GroupLeverage).ToString() + (if i = 0 then "" else i.ToString()))
                                        strategy.AddMemoryPoint(initialDay.DateTime, (float) ge, instrument.ID, code))
                    )
                Console.WriteLine("Done Adding Instruments: " + strategy.Description + " / " + (DateTime.Now - t1).ToString())

            if strategy_pkg.SubStrategies.IsSome then
                strategy_pkg.SubStrategies.Value
                |> List.iter(fun substrategy_pkg ->
                        let substrategy = createStrategy substrategy_pkg uid (strategy.Portfolio)

                        Console.WriteLine("Adding Subs-strategy: " + substrategy.Description + " to " + strategy.Description)

                        strategy.AddMemoryPoint(initialDay.DateTime, substrategy_pkg.MinimumExposure, substrategy.ID, (int)AQI.AQILabs.SDK.Strategies.MemoryType.IndividualMinimumLeverage)
                        strategy.AddMemoryPoint(initialDay.DateTime, substrategy_pkg.MaximumExposure, substrategy.ID, (int)AQI.AQILabs.SDK.Strategies.MemoryType.IndividualMaximumLeverage)

                        if substrategy_pkg.ParentMemory.IsSome then
                            substrategy_pkg.ParentMemory.Value
                            |> List.iter(fun (value, id) -> strategy.AddMemoryPoint(initialDay.DateTime, value, substrategy.ID, id))

                        strategy.AddInstrument(substrategy, initialDay.DateTime)
                        if substrategy_pkg.GroupExposure.IsSome then
                            substrategy_pkg.GroupExposure.Value
                            |> List.iteri(fun i ge -> 
                                    let code = Int32.Parse(((int)AQI.AQILabs.SDK.Strategies.MemoryType.GroupLeverage).ToString() + (if i = 0 then "" else i.ToString()))
                                    strategy.AddMemoryPoint(DateTime.MinValue, (float) ge, substrategy.ID, code))
                    )

            if strategy_pkg.GroupConstraints.IsSome then
                strategy_pkg.GroupConstraints.Value
                |> List.iter(fun groupc ->
                        strategy.AddMemoryPoint(DateTime.MinValue, groupc.MinimumExposure, groupc.ID, (int)AQI.AQILabs.SDK.Strategies.MemoryType.IndividualMinimumLeverage)
                        strategy.AddMemoryPoint(DateTime.MinValue, groupc.MaximumExposure, groupc.ID, (int)AQI.AQILabs.SDK.Strategies.MemoryType.IndividualMaximumLeverage)
                    )

            
            if strategy_pkg.InformationRatio.IsSome then
                //strategy._initialized <- true
                strategy.InformationRatioFunctionName <- strategy_pkg.InformationRatio.Value
                //strategy._initialized <- false

            if strategy_pkg.WeightFilter.IsSome then
                //strategy._initialized <- true
                strategy.WeightFilterFunctionName <- strategy_pkg.WeightFilter.Value
                //strategy._initialized <- false

            if strategy_pkg.IndicatorCalculation.IsSome then
                //strategy._initialized <- true
                strategy.IndicatorCalculationFunctionName <- strategy_pkg.IndicatorCalculation.Value
                //strategy._initialized <- false

            if strategy_pkg.Risk.IsSome then
                //strategy._initialized <- true
                strategy.RiskFunctionName <- strategy_pkg.Risk.Value
                //strategy._initialized <- false

            if strategy_pkg.Exposure.IsSome then
                //strategy._initialized <- true
                strategy.ExposureFunctionName <- strategy_pkg.Exposure.Value
                //strategy._initialized <- false


            if strategy_pkg.Analyse.IsSome then
                //strategy._initialized <- true
                strategy.AnalyseFunctionName <- strategy_pkg.Analyse.Value
                //strategy._initialized <- false

            strategy
        
        let strategy = createStrategy strategy_pkg uid null

        if code.IsSome then
            //strategy._initialized <- true
            strategy.ScriptCode <- code.Value
            //strategy._initialized <- false

        elif pkg.Code.IsSome then
            //strategy._initialized <- true
            strategy.ScriptCode <- pkg.Code.Value
            //strategy._initialized <- false

        if pkg.RateCompounding.IsSome then
            //strategy._initialized <- true
            strategy.RateCompoundingFunctionName <- pkg.RateCompounding.Value
            //strategy._initialized <- false

        if pkg.Instructions.IsSome then
            pkg.Instructions.Value
            |> List.iter(fun instruction_pkg ->
                    let instrument = if instruction_pkg.Instrument.IsSome then Instrument.FindInstrument(instruction_pkg.Instrument.Value) else null
                    
                    let Destination = if instruction_pkg.Destination.IsSome then instruction_pkg.Destination.Value else ""
                    let Account = if instruction_pkg.Account.IsSome then instruction_pkg.Account.Value else ""
                    let ExecutionFee = if instruction_pkg.ExecutionFee.IsSome then instruction_pkg.ExecutionFee.Value else 0.0
                    let MinimumExecutionFee = if instruction_pkg.MinimumExecutionFee.IsSome then instruction_pkg.MinimumExecutionFee.Value else 0.0
                    let MaximumExecutionFee = if instruction_pkg.MaximumExecutionFee.IsSome then instruction_pkg.MaximumExecutionFee.Value else 0.0
                    let MinimumSize = if instruction_pkg.MinimumSize.IsSome then instruction_pkg.MinimumSize.Value else 0.0
                    let MinimumStep = if instruction_pkg.MinimumStep.IsSome then instruction_pkg.MinimumStep.Value else 0.0
                    let Margin = if instruction_pkg.Margin.IsSome then instruction_pkg.Margin.Value else 0.0

                    Market.AddInstruction(new Instruction(strategy.Portfolio, instrument, instruction_pkg.Client, Destination, Account, ExecutionFee, MinimumExecutionFee, MaximumExecutionFee, MinimumSize, MinimumStep, Margin))
                )

        (strategy, result)


    /// <summary>
    /// Function: Create a strategy based of a PortfolioStrategy Package
    /// </summary>    
    /// <param name="pkg">Package
    /// </param>
    static member public Create(pkg : MasterPkg_v1, code : string option, firstDate : DateTime option, simulated : bool option) =
        let strategy_pkg = pkg.Strategy

        let result =
            try
                let fsi = System.Reflection.Assembly.Load("FSI-ASSEMBLY")
                ""
            with _ ->
                if code.IsSome then
                    Utils.RegisterCode (true, true) [pkg.Strategy.Name, code.Value]
                elif pkg.Code.IsSome && not(pkg.Code.Value = null) then
                    Utils.RegisterCode (true, true) [pkg.Strategy.Name, pkg.Code.Value]
                else
                    ""

        let calendar = Calendar.FindCalendar("All")
        let ccy = Currency.FindCurrency(pkg.Currency)
        let firstDate = if firstDate.IsNone then pkg.InitialDate else firstDate.Value
       
        let initialDay = calendar.GetClosestBusinessDay(firstDate, TimeSeries.DateSearchType.Next)

        let uid = System.Guid.NewGuid().ToString()

        let master_parameters = strategy_pkg.Parameters

        //let createFutureStrategy (name : string) (underlyingInstrument : Instrument) (master : Portfolio) =                                                                       
        //    let strategy = AQI.AQILabs.SDK.Strategies.RollingFutureStrategy.Create(name, name, initialDay.DateTime, pkg.InitialValue, underlyingInstrument, contract, rollDay, master.Currency, master, pkg.Simulated, false)
        //    if not(master = null) then
        //        master.Strategy.AddInstrument(strategy, initialDay.DateTime)
        //    strategy

        let rec createStrategy (strategy_pkg : StrategyPkg_v1) (uid : string) (parent : Portfolio) : PortfolioStrategy = 
            let uid = uid + "/" + strategy_pkg.Name

            
            let initial_value = pkg.InitialValue 
                    //if not(firstDate = pkg.InitialDate) && not(strategy_pkg.Meta.Value.CurrentAUM = 0.0) then                         
                    //    strategy_pkg.Meta.Value.CurrentAUM
                    //else 
                    //    pkg.InitialValue 

            Console.WriteLine("Creating Strategy: " + strategy_pkg.Name + " " + initial_value.ToString())


            let strategy = PortfolioStrategy.Create(uid, strategy_pkg.Name, initialDay.DateTime, initial_value, parent, ccy, (if simulated.IsSome then simulated.Value else pkg.Simulated), pkg.Residual, false)
                
            let _parameters = if strategy_pkg.Parameters.IsSome then strategy_pkg.Parameters else master_parameters

            if _parameters.IsSome then
                let parameters = _parameters.Value
                let addPoint x y =
                    strategy.AddMemoryPoint(initialDay.DateTime, x, (int)y)
            
                addPoint parameters.TargetVolatility MemoryType.TargetVolatility
                addPoint (if parameters.TargetVolatility > 0.0 then 1.0 else 0.0) MemoryType.GlobalTargetVolatilityFlag
                addPoint (if parameters.TargetVolatility > 0.0 then 1.0 else 0.0) MemoryType.IndividialTargetVolatilityFlag
                addPoint (if parameters.Concentration then 1.0 else 0.0) MemoryType.ConcentrationFlag
                addPoint (if strategy_pkg.Exposure.IsSome then 1.0 else 0.0) MemoryType.ExposureFlag
                addPoint -0.25 MemoryType.ExposureThreshold

                addPoint 100.0 MemoryType.IndividualMaximumLeverage
                addPoint -100.0 MemoryType.IndividualMinimumLeverage
            
                addPoint (if parameters.MeanVariance then 1.0 else 0.0) MemoryType.MVOptimizationFlag
                addPoint (if parameters.FXHedge then 1.0 else 0.0) MemoryType.FXHedgeFlag

                addPoint parameters.MaximumVaR MemoryType.TargetVAR
                addPoint (if parameters.MaximumVaR < 0.0 then 1.0 else 0.0) MemoryType.GlobalTargetVARFlag
    
                addPoint strategy_pkg.MaximumExposure MemoryType.GlobalMaximumLeverage
                addPoint strategy_pkg.MinimumExposure MemoryType.GlobalMinimumLeverage        
                addPoint ((float) parameters.DaysBack) MemoryType.DaysBack

               
                addPoint ((float) parameters.LiquidityDaysBack) MemoryType.LiquidityDaysBack
                addPoint (parameters.LiquidityThreshold) MemoryType.LiquidityThreshold
                addPoint (parameters.LiquidityExecutionThreshold) MemoryType.TargetUnits_Threshold
            
                addPoint ((float) parameters.Frequency) AQI.AQILabs.SDK.Strategies.MemoryType.RebalancingFrequency
                addPoint 0.0 MemoryType.RebalancingThreshold

            strategy.InitialDate <- initialDay.DateTime

            if strategy_pkg.SubStrategies.IsSome then
                strategy_pkg.SubStrategies.Value
                |> List.iter(fun substrategy_pkg ->
                        let substrategy = createStrategy substrategy_pkg uid (strategy.Portfolio)

                        Console.WriteLine("Adding Subs-strategy: " + substrategy.Description + " to " + strategy.Description)

                        strategy.AddMemoryPoint(initialDay.DateTime, substrategy_pkg.MinimumExposure, substrategy.ID, (int)AQI.AQILabs.SDK.Strategies.MemoryType.IndividualMinimumLeverage)
                        strategy.AddMemoryPoint(initialDay.DateTime, substrategy_pkg.MaximumExposure, substrategy.ID, (int)AQI.AQILabs.SDK.Strategies.MemoryType.IndividualMaximumLeverage)

                        if substrategy_pkg.ParentMemory.IsSome then
                            substrategy_pkg.ParentMemory.Value
                            |> List.iter(fun (value, id) -> strategy.AddMemoryPoint(initialDay.DateTime, value, substrategy.ID, id))

                        strategy.AddInstrument(substrategy, initialDay.DateTime)
                        if substrategy_pkg.GroupExposure.IsSome then
                            strategy.AddMemoryPoint(DateTime.MinValue, (float) substrategy_pkg.GroupExposure.Value, substrategy.ID, (int)AQI.AQILabs.SDK.Strategies.MemoryType.GroupLeverage)
                    )

            if strategy_pkg.Instruments.IsSome then
                let t1 = DateTime.Now
                Console.WriteLine("Adding Instruments: " + strategy.Description + " / " + t1.ToString())
                strategy_pkg.Instruments.Value
                |> Seq.iter(fun (date_pkg, instruments_list) ->
                    strategy.RemoveInstruments(date_pkg)                     
                    instruments_list
                    |> Seq.iteri(fun num instrument_pkg ->
                        let instrument = Instrument.FindInstrument(instrument_pkg.Name)
                        //Console.WriteLine("Adding Instrument (" + date_pkg.ToString() + "): " + instrument.Name + " to " + strategy.Description)
                        strategy.AddInstrument(instrument, date_pkg, num)
                        strategy.AddMemoryPoint(date_pkg, instrument_pkg.MinimumExposure, instrument.ID, (int)AQI.AQILabs.SDK.Strategies.MemoryType.IndividualMinimumLeverage)
                        strategy.AddMemoryPoint(date_pkg, instrument_pkg.MaximumExposure, instrument.ID, (int)AQI.AQILabs.SDK.Strategies.MemoryType.IndividualMaximumLeverage)
                    
                        if instrument_pkg.GroupExposure.IsSome then
                            strategy.AddMemoryPoint(date_pkg, (float) instrument_pkg.GroupExposure.Value, instrument.ID, (int)AQI.AQILabs.SDK.Strategies.MemoryType.GroupLeverage)

                        if instrument_pkg.Conviction.IsSome then
                            strategy.AddMemoryPoint(date_pkg, (float) instrument_pkg.Conviction.Value, (int)AQI.AQILabs.SDK.Strategies.MemoryType.ConvictionLevel, instrument.ID)


                        if instrument_pkg.InitialWeight.IsSome && not(Double.IsNaN(instrument_pkg.InitialWeight.Value)) && not(Double.IsInfinity(instrument_pkg.InitialWeight.Value)) then
                            let scale =
                                if (instrument.InstrumentType = InstrumentType.Fund || instrument.InstrumentType = InstrumentType.Equity || instrument.InstrumentType = InstrumentType.ETF) then 
                                    (instrument :?> Security).PointSize
                                else
                                    1.0
                            
                            let t = strategy.Calendar.GetClosestBusinessDay((if firstDate > date_pkg then firstDate else date_pkg), TimeSeries.DateSearchType.Next)

                            let fx = CurrencyPair.Convert(1.0, t.DateTime, strategy.Currency, instrument.Currency)
                            let instrumentValue = fx * instrument.[t.DateTime, TimeSeriesType.Last, TimeSeriesRollType.Last] * scale
                            
                            let weight = initial_value * instrument_pkg.InitialWeight.Value / instrumentValue

                            //Console.WriteLine(instrument.Name + " " + (strategy.Portfolio.MasterPortfolio.[t.DateTime, TimeSeriesType.Last, TimeSeriesRollType.Last]).ToString() + " " + (initial_value * instrument_pkg.InitialWeight.Value / initial_value).ToString())
                            strategy.Portfolio.CreatePosition(instrument, t.DateTime, weight, instrumentValue) |> ignore
                    ))
                Console.WriteLine("Done Adding Instruments: " + strategy.Description + " / " + (DateTime.Now - t1).ToString())
                
            

            if strategy.Portfolio.ParentPortfolio = null then
                if strategy_pkg.Meta.IsSome then
                    strategy_pkg.Meta.Value.Cash.Currencies
                    |> List.iter(fun cash ->
                            let ccy = Currency.FindCurrency(cash.Currency)
                            let notional_ccy_pos = strategy.Portfolio.GetReservePosition(initialDay.DateTime, ccy)
                            let notional_ccy = cash.CurrentValue - (if notional_ccy_pos = null then 0.0 else notional_ccy_pos.Unit)
                            strategy.Portfolio.UpdateReservePosition(initialDay.DateTime, notional_ccy, ccy) |> ignore
                        )

            if strategy_pkg.GroupConstraints.IsSome then
                strategy_pkg.GroupConstraints.Value
                |> List.iter(fun groupc ->
                        strategy.AddMemoryPoint(DateTime.MinValue, groupc.MinimumExposure, groupc.ID, (int)AQI.AQILabs.SDK.Strategies.MemoryType.IndividualMinimumLeverage)
                        strategy.AddMemoryPoint(DateTime.MinValue, groupc.MaximumExposure, groupc.ID, (int)AQI.AQILabs.SDK.Strategies.MemoryType.IndividualMaximumLeverage)
                    )

            
            if strategy_pkg.InformationRatio.IsSome then
                //strategy._initialized <- true
                strategy.InformationRatioFunctionName <- strategy_pkg.InformationRatio.Value
                //strategy._initialized <- false

            if strategy_pkg.WeightFilter.IsSome then
                //strategy._initialized <- true
                strategy.WeightFilterFunctionName <- strategy_pkg.WeightFilter.Value
                //strategy._initialized <- false

            if strategy_pkg.InstrumentFilter.IsSome then
                //strategy._initialized <- true
                strategy.InstrumentFilterFunctionName <- strategy_pkg.InstrumentFilter.Value
                //strategy._initialized <- false


            if strategy_pkg.IndicatorCalculation.IsSome then
                //strategy._initialized <- true
                strategy.IndicatorCalculationFunctionName <- strategy_pkg.IndicatorCalculation.Value
                //strategy._initialized <- false

            if strategy_pkg.Risk.IsSome then
                //strategy._initialized <- true
                strategy.RiskFunctionName <- strategy_pkg.Risk.Value
                //strategy._initialized <- false

            if strategy_pkg.Exposure.IsSome then
                //strategy._initialized <- true
                strategy.ExposureFunctionName <- strategy_pkg.Exposure.Value
                //strategy._initialized <- false


            if strategy_pkg.Analyse.IsSome then
                //strategy._initialized <- true
                strategy.AnalyseFunctionName <- strategy_pkg.Analyse.Value
                //strategy._initialized <- false

            strategy
        
        let strategy = createStrategy strategy_pkg uid null

        if pkg.FixedNotional.IsSome then
            strategy.AddMemoryPoint(initialDay.DateTime, pkg.FixedNotional.Value, (int)AQI.AQILabs.SDK.Strategies.MemoryType.FixedNotional)

        if code.IsSome then
            //strategy._initialized <- true
            strategy.ScriptCode <- code.Value
            //strategy._initialized <- false

        elif pkg.Code.IsSome then
            //strategy._initialized <- true
            if not(pkg.Code.Value = null) then
                strategy.ScriptCode <- pkg.Code.Value
            //strategy._initialized <- false

        if pkg.RateCompounding.IsSome then
            //strategy._initialized <- true
            strategy.RateCompoundingFunctionName <- pkg.RateCompounding.Value
            //strategy._initialized <- false

        if pkg.Instructions.IsSome then
            pkg.Instructions.Value
            |> List.iter(fun instruction_pkg ->
                    let instrument = if instruction_pkg.Instrument.IsSome then Instrument.FindInstrument(instruction_pkg.Instrument.Value) else null
                    
                    let Destination = if instruction_pkg.Destination.IsSome then instruction_pkg.Destination.Value else ""
                    let Account = if instruction_pkg.Account.IsSome then instruction_pkg.Account.Value else ""
                    let ExecutionFee = if instruction_pkg.ExecutionFee.IsSome then instruction_pkg.ExecutionFee.Value else 0.0
                    let MinimumExecutionFee = if instruction_pkg.MinimumExecutionFee.IsSome then instruction_pkg.MinimumExecutionFee.Value else 0.0
                    let MaximumExecutionFee = if instruction_pkg.MaximumExecutionFee.IsSome then instruction_pkg.MaximumExecutionFee.Value else 0.0
                    let MinimumSize = if instruction_pkg.MinimumSize.IsSome then instruction_pkg.MinimumSize.Value else 0.0
                    let MinimumStep = if instruction_pkg.MinimumStep.IsSome then instruction_pkg.MinimumStep.Value else 0.0
                    let Margin = if instruction_pkg.Margin.IsSome then instruction_pkg.Margin.Value else 0.0

                    Market.AddInstruction(new Instruction(strategy.Portfolio, instrument, instruction_pkg.Client, Destination, Account, ExecutionFee, MinimumExecutionFee, MaximumExecutionFee, MinimumSize, MinimumStep, Margin))
                )

        (strategy, result)
