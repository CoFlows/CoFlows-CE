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

open Accord.Math.Optimization


/// <summary>
/// Delegate function called by this strategy during the logic execution in order to decide
/// if the portfolio's should have exposure to a specific instrument.
/// </summary>
type Exposure = delegate of Strategy * BusinessDay * Instrument -> double

/// <summary>
/// Delegate function called by this strategy during the logic execution in order to measure
/// the risk of a specific timeseries used in the risk targeting process
/// </summary>
type Risk = delegate of Strategy * BusinessDay * TimeSeries * double -> double

/// <summary>
/// Delegate function called by this strategy during the logic execution in order to measure
/// the information ratio of a specific instrument used in the optimisation process
/// </summary>
type InformationRatio = delegate of Strategy * BusinessDay * Instrument * bool -> double

/// <summary>
/// Delegate function called by this strategy during the logic execution in order to
/// manipulate the weights after the optimisation process
/// </summary>
type WeightFilter = delegate of Strategy * BusinessDay * Map<int, float> -> Map<int, float>

/// <summary>
/// Delegate function called by this strategy during the logic execution in order to
/// manipulate the weights after the optimisation process
/// </summary>
type InstrumentFilter = delegate of Strategy * BusinessDay * Map<int, TimeSeries> -> Map<int, TimeSeries>

/// <summary>
/// Delegate function called by this strategy during the logic execution in order to
/// calculate indicators continuously
/// </summary>
type IndicatorCalculation = delegate of Strategy * BusinessDay -> unit

/// <summary>
/// Delegate function called by externally to analyse the strategy
/// </summary>
type Analyse = delegate of Strategy * string -> Object


/// <summary>
/// Enumeration all Memory types used in the SDK.Strategies namespace
/// </summary>
type public MemoryType =

    // PortfolioStrategy
    | TargetVolatility = 11 // Target volatility value
    | IndividialTargetVolatilityFlag = 2 // 1.0 if TargetVolatility is applied individually to the assets in the portfolio
    | GlobalTargetVolatilityFlag = 3 // 1.0 if TargetVolatility is applied to the entire portfolio

    | ConcentrationFlag = 4 // 1.0 if Concentration risk is to be managed by calculating correlations and aiming to balance exposures to risk factors 
       
    | ExposureFlag = 5 // 1.0 if exposure management is to be implemented
    | ExposureThreshold = 22 // Drawdown from peak at which the position is cut

    | TargetVAR = 6 // Maximum VaR level before the portfolio is deleveraged linearly
    | GlobalTargetVARFlag = 7 // 1.0 if a maximum VaR is to be implemented

    //| IndividualTargetWeight = 10 // 1.0 if fractional contracts are allowed. 0.0 is more realistic

    | IndividualMinimumLeverage = 38 // Maximum leverage applied per position
    | IndividualMaximumLeverage = 8 // Maximum leverage applied per position
    | GlobalMaximumLeverage = 9 // Maximum leverage applied for entire portfolio sum of all position notional values. Note: Spreads 1 - 1 give 0 exposure. Future are notionally and not marginally accounted for
    | GlobalMinimumLeverage = 31 // Minimum leverage applied for entire portfolio sum of all position notional values. Note: Only available when MVOptimizationFlag = 1. Spreads 1 - 1 give 0 exposure. Future are notionally and not marginally accounted for
    | GroupLeverage = 32 // Minimum leverage applied for entire portfolio sum of all position notional values. Note: Only available when MVOptimizationFlag = 1. Spreads 1 - 1 give 0 exposure. Future are notionally and not marginally accounted for

    //| FractionContract = 10 // 1.0 if fractional contracts are allowed. 0.0 is more realistic
    | DaysBack = 1  // Number of days used in the volatility and correlation calculations
    | RebalancingFrequency = 12 // Frequency of rebalancings
    | FixedNotional = 13  // 0.0 if no fixed notional else the notional exposure of the positions will reference this value
    | RebalancingThreshold = 14 // Minimum pct notional value before an order is submitted for a rebalancing
    | ConvictionLevel = 23 // Minimum pct notional value before an order is submitted for a rebalancing

    | MVOptimizationFlag = 36 // 1.0 if optimizing with contrained Mean-Variance or 0.0 if custom step optimization
    | FXHedgeFlag = 37 // 1.0 if optimizing with contrained Mean-Variance or 0.0 if custom step optimization
    
    | LiquidityDaysBack = 42 // 1.0 if optimizing with contrained Mean-Variance or 0.0 if custom step optimization
    | LiquidityThreshold = 43 // 1.0 if optimizing with contrained Mean-Variance or 0.0 if custom step optimization

    | TargetUnits = 44 // 1.0 if optimizing with contrained Mean-Variance or 0.0 if custom step optimization
    | TargetUnits_Submitted = 45 // 1.0 if optimizing with contrained Mean-Variance or 0.0 if custom step optimization
    | TargetUnits_Threshold = 46 // 1.0 if optimizing with contrained Mean-Variance or 0.0 if custom step optimization
    

    // AUM Constraints on Optimization
    //| AUMConstraintFlag = 31 // 1.0 if AUM constraint is to be implemented in optimization
    //| IndividualPctMaxAUM = 32
    //| SubportfolioPctMaxAUM = 33

    // ONDepositStrategy
    | Spread = 15 // Spread in pct terms applied to deposit rate accrued in Act/360
    | FundingID = 16 // Instrument ID where the values are stored as 5 --> 5% per annum accrued in Act/360

    // RollingFutureStrategy
    | UnderlyingID = 17 // ID of underlying instrument of futures to be rolled
    | Contract = 18 // Contract number in active chain to roll into
    | RollDay = 19 // Business day of the month to roll the contract
    | ActiveJan = 101
    | ActiveFeb = 102
    | ActiveMar = 103
    | ActiveApr = 104
    | ActiveMay = 105
    | ActiveJun = 106
    | ActiveJul = 107
    | ActiveAug = 108
    | ActiveSep = 109
    | ActiveOct = 110
    | ActiveNov = 111
    | ActiveDec = 112
    //| Sign = 20 // 1.0 if position exposure to the future -1.0 for a negative exposure

    //CMConstraints (NEED TO MOVE OUT)
    | IROptimizedFlag = 34
    | HedgedFlag = 35

    //LSStrategy (NEED TO MOVE OUT)
    | TSTypeList = 40
    | LSOptimizationType = 41


type public InstructionPkg =
    {
        Instrument : string option
        Client : string
        Destination : string option
        Account : string option
        ExecutionFee : float option
        MinimumExecutionFee : float option
        MaximumExecutionFee : float option
        MinimumSize : float option
        MinimumStep : float option
        Margin : float option
    }
    // with
    //     static member Some (x : InstructionPkg) = Some(x)
    //     static member Some (x : float) = Some(x)
    //     static member Some (x : string) = Some(x)

type public GroupConstraint =
    {
        ID : int
        MinimumExposure : float
        MaximumExposure : float
    }
    // with
    //     static member Some (x : GroupConstraint) = Some(x)

type public Parameters =
    {
        TargetVolatility : float
        MaximumVaR : float
        DaysBack : int
        Concentration : bool
        MeanVariance : bool
        Frequency : int
        FXHedge : bool
    }
    // with
    //     static member Some (x : Parameters) = Some(x)

///////////////// START V1
type public Parameters_v1 =
    {
        TargetVolatility : float
        MaximumVaR : float
        DaysBack : int
        Concentration : bool
        MeanVariance : bool
        Frequency : int
        FXHedge : bool

        LiquidityDaysBack : int
        LiquidityThreshold : float
        LiquidityExecutionThreshold : float
    }
    // with
    //     static member Some (x : Parameters_v1) = Some(x)

type public InstrumentMetaPkg_v1 =
    {
        Description : string
        LongDescription : string
        Value : float
        FX : float

        CurrentUnit : float
        ProjectedUnit : float

        Volatility : float
        ValueAtRisk : float
        InformationRatio : float
        DrawDown : float
        
        Currency : string
        AssetClass : string
        GeographicalRegion : string
                                    
        Return1D : float
        Return1W : float
        Return1M : float
        Return3M : float
        Return1Y : float
    }
    // with
    //     static member Some (x : InstrumentMetaPkg_v1) = Some(x)


type public InstrumentPkg_v1 =
    {
        ID : int option
        Name : string
        
        Isin : string option
        Conviction : float option
        MinimumExposure : float
        MaximumExposure : float
        GroupExposure : int option
        InitialWeight : float option

        Meta : InstrumentMetaPkg_v1 option
    }
    // with
    //     static member Some (x : InstrumentPkg_v1) = Some(x)
    //     static member Some (x : float) = Some(x)
    //     static member Some (x : string) = Some(x)
    //     static member Some (x : int) = Some(x)

type StrategyMetaCashRatesPkg_v1 =
    {
        Rate1D : float
        Rate1W : float
        Rate1M : float
        Rate3M : float
        Rate6M : float
        Rate1Y : float
        Rate3Y : float
        Rate5Y : float
    }
    // with
    //     static member Some (x : StrategyMetaCashRatesPkg_v1) = Some(x)

type StrategyMetaCashPkg_v1 =
    {
        Currency : string
        CurrentValue : float
        ProjectedValue : float

        Rates : StrategyMetaCashRatesPkg_v1
    }
    // with
    //     static member Some (x : StrategyMetaCashPkg_v1) = Some(x)


type StrategyMetaCashTreePkg_v1 =
    {
        Total : StrategyMetaCashPkg_v1
        Currencies : List<StrategyMetaCashPkg_v1>
    }
    // with
    //     static member Some (x : StrategyMetaCashTreePkg_v1) = Some(x)


type public StrategyMetaRiskPkg_v1 =
    {
        Volatility : float
        ValueAtRisk : float
        InformationRatio : float        
        RiskNotional : float
    }
    // with
    //     static member Some (x : StrategyMetaRiskPkg_v1) = Some(x)


type public StrategyMetaPkg_v1 =
    {
        Description : string
        Value : float

        CurrentAUM : float
        ProjectedAUM : float

        TurnOver : float
        
        Cash : StrategyMetaCashTreePkg_v1

        CurrentRisk : StrategyMetaRiskPkg_v1
        ProjectedRisk : StrategyMetaRiskPkg_v1     

        Active : bool
    }
    // with
        // static member Some (x : StrategyMetaPkg_v1) = Some(x)


type public StrategyPkg_v1 =
    {
        ID: int option
        Name : string
        Parameters : Parameters_v1 option
        SubStrategies : List<StrategyPkg_v1> option
        Instruments: List<DateTime * List<InstrumentPkg_v1>> option
        
        MinimumExposure : float
        MaximumExposure : float
        GroupExposure : int option
        GroupConstraints : List<GroupConstraint> option

        InformationRatio : string option
        WeightFilter : string option
        InstrumentFilter : string option
        IndicatorCalculation : string option
        Risk : string option
        Exposure : string option

        Analyse : string option

        ParentMemory : List<float * int> option

        Meta : StrategyMetaPkg_v1 option
    }
    with
        // static member Some (x : int) = Some(x)
        // static member Some (x : string) = Some(x)
        // static member Some (x : List<StrategyPkg_v1>) = Some(x)
        static member SomeInstruments (x : System.Collections.Generic.IEnumerable<obj>) = 
            Some(x |> Seq.map(fun x -> x :?> DateTime * System.Collections.Generic.List<InstrumentPkg_v1>) |> Seq.map(fun (d, x) -> (d, x |> Seq.toList)) |> Seq.toList)
        // static member SomeList<'T> (x : System.Collections.Generic.IEnumerable<obj>) = 
        //     Some(x |> Seq.map(fun _x -> _x :?> 'T) |> Seq.toList)
        // // static member Some (x : List<GroupConstraint>) = Some(x)
        // static member Some (x : List<float * int>) = Some(x)
        

type public MasterPkg_v1 =
    {        
        Strategy : StrategyPkg_v1
        Code : string option
        InitialDate : DateTime
        InitialValue : float
        Residual : bool
        Simulated : bool
        Currency : string
        RateCompounding : string option
        ScheduleCommand: string
        FixedNotional : float option

        Instructions : List<InstructionPkg> option
    }
    // with
        // static member Some (x : string) = Some(x)
        // static member Some (x : float) = Some(x)
///////////////// END V1

///////////////// START V0.6
type public InstrumentPkg_v06 =
    {
        Name : string
        MinimumExposure : float
        MaximumExposure : float
        GroupExposure : List<int> option
    }

type public StrategyPkg_v06 =
    {
        Name : string
        Parameters : Parameters option
        SubStrategies : List<StrategyPkg_v06> option
        Instruments: List<InstrumentPkg_v06> option
        
        MinimumExposure : float
        MaximumExposure : float
        GroupExposure : List<int> option
        GroupConstraints : List<GroupConstraint> option

        InformationRatio : string option
        WeightFilter : string option
        IndicatorCalculation : string option
        Risk : string option
        Exposure : string option

        Analyse : string option

        ParentMemory : List<float * int> option
    }

type public MasterPkg_v06 =
    {
        Strategy : StrategyPkg_v06
        Code : string option
        InitialDate : DateTime
        InitialValue : float
        Residual : bool
        Simulated : bool
        Currency : string
        RateCompounding : string option

        Instructions : List<InstructionPkg> option
    }
///////////////// END V0.6

///////////////// START V0.5
type public InstrumentPkg_v05 =
    {
        Name : string
        MinimumExposure : float
        MaximumExposure : float
        GroupExposure : int option
    }

type public StrategyPkg_v05 =
    {
        Name : string
        Parameters : Parameters option
        SubStrategies : List<StrategyPkg_v05> option
        Instruments: List<InstrumentPkg_v05> option
        
        MinimumExposure : float
        MaximumExposure : float
        GroupExposure : int option
        GroupConstraints : List<GroupConstraint> option

        InformationRatio : string option
        WeightFilter : string option
        IndicatorCalculation : string option
        Risk : string option
        Exposure : string option

        Analyse : string option

        ParentMemory : List<float * int> option
    }

type public MasterPkg_v05 =
    {
        Strategy : StrategyPkg_v05
        Code : string option
        InitialDate : DateTime
        InitialValue : float
        Residual : bool
        Simulated : bool
        Currency : string
        RateCompounding : string option

        Instructions : List<InstructionPkg> option
    }
///////////////// END V0.5

type Time2TargetRiskElement =
    {
        StartValue : float
        StartDate : DateTime
        DrawdownValue : float
        DrawdownDate : DateTime
        RealizedReturn : float
        TargetDate : DateTime
        LastDate : DateTime
        Found : bool
        //Trajectory : TimeSeries option
    }

type Matrix = AQI.AQILabs.Kernel.Numerics.Math.LinearAlgebra.DenseMatrix
type Vector = AQI.AQILabs.Kernel.Numerics.Math.LinearAlgebra.DenseVector

/// <summary>
/// Utility module with a set of functions used by all strategies in this namespace
/// </summary>
module Utils =

    let Some<'T> (x : 'T) = Some(x)

    let SomeList<'T> (x : System.Collections.Generic.IEnumerable<obj>) = Some(x |> Seq.map(fun _x -> _x :?> 'T) |> Seq.toList)

    let SetFunction(name : string, func: obj) = QuantApp.Engine.Utils.SetFunction(name, func)

    let Time2TargetProbatilities (ref_date: DateTime) (targetReturn : float) (targetTime : float) (exposure : float option, minHoldingTime : float option) (days_back : int) (ts : TimeSeries) =
        
        let i = ts.GetClosestDateIndex(ref_date, TimeSeries.DateSearchType.Previous)
        let ttrisk startI =
            let element =
                [|startI .. i|]
                |> Array.fold(fun element i -> 
                
                                if not(element.Found) then
                                    let currentValue = ts.[i]
                                    let currentDate = ts.DateTimes.[i]
                                                                        
                                    let exposure = if exposure.IsSome then exposure.Value else 1.0
                                    let currentReturn = exposure * (currentValue / element.StartValue - 1.0)
                        
                                    let drawdownValue = Math.Min(currentReturn, element.DrawdownValue)
                                    let drawdownDate = if drawdownValue = currentReturn then currentDate else element.DrawdownDate

                                    let runningTime = (currentDate - element.StartDate).TotalDays / 365.0
                                    
                                    let minHoldingTime = if minHoldingTime.IsSome then minHoldingTime.Value else 0.0
                                    if currentReturn >= targetReturn && minHoldingTime <= runningTime then //STOP                                            
                                        { StartValue = element.StartValue; StartDate = element.StartDate; DrawdownValue = drawdownValue; DrawdownDate = drawdownDate; RealizedReturn = currentReturn; TargetDate = currentDate; LastDate =  currentDate; Found = true }//; Trajectory = None }
                                    else
                                        { StartValue = element.StartValue; StartDate = element.StartDate; DrawdownValue = drawdownValue; DrawdownDate = drawdownDate; RealizedReturn = currentReturn; TargetDate =  element.TargetDate; LastDate =  currentDate; Found = element.Found }//; Trajectory = None }
                                else
                                    element

                        ) { StartValue = ts.[startI]; StartDate = ts.DateTimes.[startI]; DrawdownValue = 0.0; DrawdownDate = DateTime.MaxValue; RealizedReturn = 0.0; TargetDate = DateTime.MaxValue; LastDate =  DateTime.MaxValue; Found = false } //; Trajectory = None } //(ts.[startI], ts.DateTimes.[startI], 0.0, DateTime.MaxValue, 0.0, DateTime.MaxValue, false)
            element

        let data_i = [|i - days_back .. i - 1|] |> Array.map(ttrisk)
    
        let count_i = data_i |> Array.length
        let data_found = data_i |> Array.filter(fun element -> ((element.TargetDate - element.StartDate).TotalDays / 365.0) <= targetTime)
        let data_not_found = data_i |> Array.filter(fun element -> not (((element.TargetDate - element.StartDate).TotalDays / 365.0) <= targetTime))
            
        let avg_wt = (data_found |> Array.map(fun element -> element.RealizedReturn * targetTime * 365.0 / (element.TargetDate - element.StartDate).TotalDays)  |> Array.sum) / (float) count_i
        let avg_pt = (data_found |> Array.map(fun element -> 1.0)  |> Array.sum) / (float) count_i
        let avg_dd_found = (data_found |> Array.map(fun element -> element.DrawdownValue)  |> Array.sum) / (float) count_i
        let avg_dd_not_found = (data_not_found |> Array.map(fun element -> element.DrawdownValue)  |> Array.sum) / (float) count_i
        let avg_dd = avg_dd_found + avg_dd_not_found

        (avg_wt, avg_pt, avg_dd)

    /// <summary>
    /// Function: Covariance calculator. Ensure all timeseries have the same length and data frequency.
    /// </summary>
    /// <param name="tlist">List of timeseries for calculation</param>
    let Covariance(tlist : List<TimeSeries>) =
        let length = List.length tlist
        let covariance = new Matrix(length , length)
        [|0 .. (length - 1)|]
        |> Array.iter (fun i -> 
            [|0 .. (length - 1)|]
            |> Array.iter (fun j ->
                //covariance.[i, j] <- if i = j then AQI.AQILabs.Kernel.Numerics.Math.Statistics.Statistics.Covariance(tlist.[i].Data, tlist.[j].Data) else 0.0
                covariance.[i, j] <- AQI.AQILabs.Kernel.Numerics.Math.Statistics.Statistics.Covariance(tlist.[i].Data, tlist.[j].Data)
                covariance.[j, i] <- covariance.[i, j]))
        covariance


    /// <summary>
    /// Function: Correlation calculator. Ensure all timeseries have the same length and data frequency.
    /// </summary>
    /// <param name="tlist">List of timeseries for calculation</param>
    let Correlation(tlist : List<TimeSeries>) =
        let length = List.length tlist
                
        let tlist = 
            tlist 
            |> List.map(fun ts -> 
                                let var = ts.Variance
                                let adj = if var < 1e-30 then 0.0 else sqrt(1.0 / var)// * var
                                ts * adj
                        )
        let correlation = Covariance tlist
        [|0 .. (length - 1)|] |> Array.iter(fun i -> correlation.[i,i] <- 1.0)
        correlation   

    let seconds_wait = 60 * 1
    let tol_check = 1e-5
    let max_tries = 2
    let max_assets = 10000//30
    /// <summary>
    /// Function: Constrained MV-Optimize a list of volatility-normalized timeseries.
    /// All timeseries should have a volatility of 1.0. 
    /// Information ratios can be used to affect MV.
    /// If Information ratios are 1.0 then weights are risk-parity weights.
    /// </summary>
    /// <param name="tsl">List of timeseries for calculation</param>
    /// <param name="informationratio">List of respective information ratios</param>    
    let Optimize(tsl : List<TimeSeries> , informationratio : List<double>) =
        /////////////// Max Asset Filtering
        let tsl_count_master = List.length tsl

        //let master_solution = [0 .. tsl_count_master - 1] |> List.map(fun i -> 0.0) |> List.toArray
        let master_solution = new System.Collections.Generic.List<double>()
        [0 .. tsl_count_master - 1] |> List.iter(fun i -> master_solution.Add(0.0))

        let max_assets = Math.Min(max_assets, tsl_count_master) 
        let tuples = 
            [0 .. tsl_count_master - 1]
            |> List.map(fun i -> (informationratio.[i], i))
        let tuples_ordered = tuples |> List.sortBy(fun (inf, i) -> -inf)
        
        let informationratio = 
            [0 .. max_assets - 1] 
            |> List.map(fun i -> 
                let oldId = snd tuples_ordered.[i] 
                informationratio.[oldId])

        let tsl = 
            [0 .. max_assets - 1] 
            |> List.map(fun i -> 
                let oldId = snd tuples_ordered.[i] 
                tsl.[oldId])

        ////////////////
        let tsl_count = List.length tsl
        let optimal_wgts = new Vector(tsl_count, 1.0 / (double)tsl_count)
        let mutable counter_opt = 0;
        let lower_bound_wgts = new Vector(tsl_count, 0.0)

        let correlation = Correlation tsl

        let rets = new Vector(tsl_count, 1.0)
        [0 .. tsl_count - 1] |> List.iter (fun i -> rets.[i] <- informationratio.[i])

        if tsl_count < 2 then
            optimal_wgts |> Seq.toArray
        else        
            // We will optimize the 2-variable function f(x, y) = -x -y
            let obj = 
                fun (optimal_wgts : float[]) ->
                    let optimal_wgts_v = new Vector(tsl_count, 1.0)
                    [0 .. tsl_count - 1] |> List.iter (fun i -> optimal_wgts_v.[i] <- optimal_wgts.[i])                    
                    let vol = sqrt(optimal_wgts_v * correlation * optimal_wgts_v)
                    let er = optimal_wgts_v * rets
                    -er / vol
            let f = new NonlinearObjectiveFunction(tsl_count, obj)
            // Under the following constraints
            let constraints = new System.Collections.Generic.List<NonlinearConstraint>()
            constraints.Add(new NonlinearConstraint(tsl_count, fun (optimal_wgts : float[]) ->  (optimal_wgts |> Array.sum) - 1.0 >= 0.0))
            constraints.Add(new NonlinearConstraint(tsl_count, fun (optimal_wgts : float[]) ->  (optimal_wgts |> Array.sum) - 1.0 <= 0.0))
            [0 .. tsl_count - 1] 
            |> List.iter (fun i -> constraints.Add(new NonlinearConstraint(tsl_count, fun (optimal_wgts : float[]) ->  optimal_wgts.[i] - lower_bound_wgts.[i] >= 0.0)))

            let rec solution_fun = fun num_tries ->
                let cobyla = new Cobyla(f, constraints)

                let t1 = DateTime.Now
                let th = new System.Threading.Thread(fun () ->                
                    //Console.WriteLine("Cobyla Start(" + (max_tries - num_tries).ToString()  + "): " + tsl_count.ToString() + " " + t1.ToString())
                    let success = cobyla.Minimize()
                    let tt = DateTime.Now - t1
                    ()
                    //Console.WriteLine("Cobyla End(" + (max_tries - num_tries).ToString()  + "): " + tsl_count.ToString() + " " + cobyla.Iterations.ToString()+ " " + cobyla.Status.ToString() + " " + tt.ToString())
                    )
                th.Start()
            
                if not(th.Join(seconds_wait * 1000)) then // wait for x seconds then end...
                    th.Abort()
                    let tt = DateTime.Now - t1
                    Console.WriteLine("Cobyla Locked(" + (max_tries - num_tries).ToString()  + "): " + tsl_count.ToString() + " " + cobyla.Iterations.ToString()+ " " + cobyla.Status.ToString() + " " + tt.ToString())
                        
                let minimum = cobyla.Value       // Minimum should be -2 * sqrt(0.5)
                let solution = cobyla.Solution  // Vector should be [sqrt(0.5), sqrt(0.5)]

                if num_tries <= 0 then
                    solution
                else
                    if [0 .. tsl_count - 1] |> List.map(fun i -> if solution.[i] > (1.0 + tol_check) || solution.[i] < (0.0 - tol_check) then 1 else 0) |> List.max = 1 then
                        solution_fun (num_tries - 1)
                    else
                        solution
            let solution = solution_fun max_tries

            [0 .. max_assets - 1] 
            |> List.iter(fun i -> 
            let oldIdsnd = snd tuples_ordered.[i]
            master_solution.[oldIdsnd] <- solution.[i])
        

            master_solution |> Seq.toArray

    
    /// <summary>
    /// Function: Constrained MV-Optimize a list of volatility-normalized timeseries.
    /// All timeseries should have a volatility of 1.0. 
    /// Information ratios can be used to affect MV.
    /// If Information ratios are 1.0 then weights are risk-parity weights.
    /// </summary>
    /// <param name="tsl">List of timeseries for calculation</param>
    /// <param name="informationratio">List of respective information ratios</param>
    let OptimizeMV(tsl : List<TimeSeries> , informationratio : List<double>, target_vol : double, lower_bounds : float[], upper_bounds : float[], current_weights : float[], transaction_costs : (float -> float)[], sum_max : float, sum_min : float, group_constraint : List<(float[] * float * float)>) =
        /////////////// Max Asset Filtering
        let tsl_count_master = List.length tsl

        //let master_solution = [0 .. tsl_count_master - 1] |> List.map(fun i -> 0.0) |> List.toArray
        let master_solution = new System.Collections.Generic.List<double>()
        [|0 .. tsl_count_master - 1|] |> Array.iter(fun i -> master_solution.Add(0.0))

        let max_assets = Math.Min(max_assets, tsl_count_master) 
        let tuples = 
            [|0 .. tsl_count_master - 1|]
            |> Array.map(fun i -> (informationratio.[i], i))
        let tuples_ordered = tuples |> Array.sortBy(fun (inf, i) -> -inf)
        
        let informationratio = 
            [|0 .. max_assets - 1|] 
            |> Array.map(fun i -> 
                let oldId = snd tuples_ordered.[i] 
                informationratio.[oldId])

        let tsl = 
            [|0 .. max_assets - 1|] 
            |> Array.map(fun i -> 
                let oldId = snd tuples_ordered.[i] 
                tsl.[oldId])

        let lower_bounds = 
            [|0 .. max_assets - 1|] 
            |> Array.map(fun i -> 
                let oldId = snd tuples_ordered.[i] 
                lower_bounds.[oldId])

        let upper_bounds = 
            [|0 .. max_assets - 1|] 
            |> Array.map(fun i -> 
                let oldId = snd tuples_ordered.[i] 
                upper_bounds.[oldId])


        let current_weights = 
            [|0 .. max_assets - 1|] 
            |> Array.map(fun i -> 
                let oldId = snd tuples_ordered.[i] 
                current_weights.[oldId])
        
        let transaction_costs = 
            [|0 .. max_assets - 1|] 
            |> Array.map(fun i -> 
                let oldId = snd tuples_ordered.[i] 
                transaction_costs.[oldId])
        
        ////////////////
        let tsl_count = Array.length tsl
        let tsl_count_filtered = informationratio |> Array.filter(fun i -> i > -100.0) |> Array.length

        let optimal_wgts = new Vector(tsl_count, sum_max / (float) tsl_count_filtered)
        let covariance = Covariance (tsl |> Array.toList)   

        let avg_vol_pre = sqrt(252.0 * (optimal_wgts * covariance * optimal_wgts))

        [|0 .. tsl_count - 1|] |> Array.iter (fun i -> optimal_wgts.[i] <-if informationratio.[i] <= -100.0 then 0.0 else optimal_wgts.[i])
        let mutable counter_opt = 0;

        
        
        
        let vars = new Vector(tsl_count, 1.0)
        [|0 .. tsl_count - 1|] |> Array.iter (fun i -> vars.[i] <- tsl.[i].Variance)
        
        let rets = new Vector(tsl_count, 1.0)
        [|0 .. tsl_count - 1|] |> Array.iter (fun i -> rets.[i] <- (informationratio.[i]) * (if informationratio.[i] = -100.0 then 10.0 else sqrt(vars.[i] * 252.0)))
        
             
        let avg_vol = sqrt(252.0 * (optimal_wgts * covariance * optimal_wgts))

        //Console.WriteLine("Number of Assets: " + tsl_count_filtered.ToString())
        //Console.WriteLine("Avg Vol Pre = " + avg_vol_pre.ToString())
        //Console.WriteLine("Avg Vol = " + avg_vol.ToString())

        if tsl_count_filtered < 2 then
            optimal_wgts |> Seq.toArray
        else        
        
            let obj = 
                if (informationratio |> Array.max) = (informationratio |> Array.min) then
                    fun (optimal_wgts : float[]) ->
                        let optimal_wgts_v = new Vector(tsl_count, 1.0)
                        [|0 .. tsl_count - 1|] |> Array.iter (fun i -> optimal_wgts_v.[i] <- optimal_wgts.[i]) 
                        //let tc = [|0 .. tsl_count - 1|] |> Array.map (fun i -> Math.Abs(optimal_wgts.[i] - current_weights.[i])) |> Array.sum                        
                        let tc = [|0 .. tsl_count - 1|] |> Array.map (fun i -> transaction_costs.[i](optimal_wgts.[i] - current_weights.[i])) |> Array.sum                        
                        let er = optimal_wgts_v * rets - tc
                        let vol = 252.0 * (optimal_wgts_v * covariance * optimal_wgts_v)
                        -er / vol
                else
                    fun (optimal_wgts : float[]) ->
                        let optimal_wgts_v = new Vector(tsl_count, 1.0)
                        [|0 .. tsl_count - 1|] |> Array.iter (fun i -> optimal_wgts_v.[i] <- optimal_wgts.[i])    
                        //let tc = [|0 .. tsl_count - 1|] |> Array.map (fun i -> Math.Abs(optimal_wgts.[i] - current_weights.[i])) |> Array.sum
                        //let tc = [|0 .. tsl_count - 1|] |> Array.map (fun i -> Math.Abs(optimal_wgts.[i] - current_weights.[i])) |> Array.sum
                        let tc = [|0 .. tsl_count - 1|] |> Array.map (fun i -> transaction_costs.[i](optimal_wgts.[i] - current_weights.[i])) |> Array.sum

                        //Console.WriteLine(tc)
                        let er = optimal_wgts_v * rets - tc
                        -er
            let f = new NonlinearObjectiveFunction(tsl_count, obj)
            // Under the following constraints
            let constraints = new System.Collections.Generic.List<NonlinearConstraint>()

            let sum_adj =  target_vol / avg_vol
            //let sum_adj = avg_vol / target_vol

            let floor = Math.Max(sum_max * (if sum_adj > 1.0 then (1.0 / sum_adj) else sum_adj), sum_min)
//                if sum_adj >= 1.5 then
//                    sum / sum_adj                                  
//                elif sum_adj <= 0.5 then
//                    sum * sum_adj                    
//                else                
//                    1.0
            //Console.WriteLine("Global Floor: " + floor.ToString())
            constraints.Add(new NonlinearConstraint(tsl_count, fun (optimal_wgts : float[]) ->  (optimal_wgts |> Array.sum) - floor >= 0.0))
            constraints.Add(new NonlinearConstraint(tsl_count, fun (optimal_wgts : float[]) ->  (optimal_wgts |> Array.sum) - sum_max <= 0.0))

            group_constraint
            |> List.iter(fun (data, gmin, gmax) -> 

                let ordered_data = 
                    [|0 .. max_assets - 1|] 
                    |> Array.map(fun i -> 
                        let oldId = snd tuples_ordered.[i] 
                        data.[oldId])
                constraints.Add(new NonlinearConstraint(tsl_count, fun (optimal_wgts : float[]) ->  ([0 .. tsl_count - 1] |> List.map(fun i -> optimal_wgts.[i] * ordered_data.[i]) |> List.sum) - gmin >= 0.0))
                constraints.Add(new NonlinearConstraint(tsl_count, fun (optimal_wgts : float[]) ->  ([0 .. tsl_count - 1] |> List.map(fun i -> optimal_wgts.[i] * ordered_data.[i]) |> List.sum) - gmax <= 0.0))                
            )

            let volf = fun (optimal_wgts : float[]) -> 
                let optimal_wgts_v = new Vector(tsl_count, 1.0)
                [|0 .. tsl_count - 1|] |> Array.iter (fun i -> optimal_wgts_v.[i] <- optimal_wgts.[i])
                let vol = 252.0 * (optimal_wgts_v * covariance * optimal_wgts_v)
                vol - target_vol * target_vol

            //constraints.Add(new NonlinearConstraint(tsl_count, fun (optimal_wgts : float[]) -> volf(optimal_wgts) >= 0.0))
            constraints.Add(new NonlinearConstraint(tsl_count, fun (optimal_wgts : float[]) -> volf(optimal_wgts) <= 0.0))

            let upperbound_opt = 0.0//1.5 / (float) tsl_count_filtered
            [|0 .. tsl_count - 1|] 
            |> Array.iter (fun i -> 
                constraints.Add(new NonlinearConstraint(tsl_count, fun (optimal_wgts : float[]) ->  Math.Max(upperbound_opt, upper_bounds.[i]) - optimal_wgts.[i] >= 0.0))
                constraints.Add(new NonlinearConstraint(tsl_count, fun (optimal_wgts : float[]) ->  optimal_wgts.[i] - lower_bounds.[i] >= 0.0))
                )
            //Console.WriteLine("Upper Ind Bound: " + upperbound_opt.ToString())

            let rec solution_fun = fun num_tries ->
                let cobyla = new Cobyla(f, constraints)

                let t1 = DateTime.Now
                let th = new System.Threading.Thread(fun () ->                
                    //Console.WriteLine("Cobyla MV Start(" + (max_tries - num_tries).ToString()  + "): " + tsl_count.ToString() + " " + t1.ToString())
                    let success = cobyla.Minimize()
//                    
                    let tt = DateTime.Now - t1
//                    
                    //Console.WriteLine("Cobyla MV End(" + (max_tries - num_tries).ToString()  + "): " + tsl_count.ToString() + " " + cobyla.Iterations.ToString()+ " " + cobyla.Status.ToString() + " " + tt.ToString())
                    //if (cobyla.Status = CobylaStatus.NoPossibleSolution) then
                    //    Console.WriteLine("NO SOLUTION")
                    ()
                    )
                th.Start()
            
                if not(th.Join(seconds_wait * 1000)) then // wait for x seconds then end...
                    th.Abort()
                    let tt = DateTime.Now - t1
                    Console.WriteLine("Cobyla MV Locked(" + (max_tries - num_tries).ToString()  + "): " + tsl_count.ToString() + " " + cobyla.Iterations.ToString()+ " " + cobyla.Status.ToString() + " " + tt.ToString())
                        
                let minimum = cobyla.Value       // Minimum should be -2 * sqrt(0.5)
                let solution = cobyla.Solution  // Vector should be [sqrt(0.5), sqrt(0.5)]
                
                if num_tries <= 0 then
                    solution
                else
                    if [|0 .. tsl_count - 1|] |> Array.map(fun i -> if solution.[i] < (lower_bounds.[i] - tol_check) || solution.[i] > (Math.Max(upperbound_opt, upper_bounds.[i]) + tol_check) then 1 else 0) |> Array.max = 1 then
                        solution_fun (num_tries - 1)
                    else
                        solution

                
            let solution = solution_fun max_tries

            /////
            //let optimal_wgts_v = new Vector(tsl_count, 1.0)
            //[0 .. tsl_count - 1] |> List.iter (fun i -> optimal_wgts_v.[i] <- solution.[i]) 
            
            //let vol = sqrt(252.0 * (optimal_wgts_v * covariance * optimal_wgts_v))
            //Console.WriteLine("Optimal Vol = " + vol.ToString())

            //let tc = [|0 .. tsl_count - 1|] |> Array.map (fun i -> transaction_costs.[i](optimal_wgts_v.[i] - current_weights.[i])) |> Array.sum
            //let er = optimal_wgts_v * rets            
            //let ir = er / vol

            //Console.WriteLine("Optimal TC = " + tc.ToString())
            //Console.WriteLine("Optimal ER = " + er.ToString())
            //Console.WriteLine("Optimal IR = " + ir.ToString())
            //////

            //if sum_min = 0.0 then
            //Console.WriteLine(solution |> Array.sum)

            [|0 .. max_assets - 1|] 
            |> Array.iter(fun i -> 
            let oldIdsnd = snd tuples_ordered.[i]
            //Console.WriteLine(solution.[i])
            master_solution.[oldIdsnd] <- solution.[i])
        

            master_solution |> Seq.toArray
       
    /// <summary>
    /// Function: Generates a map of cash difference timeseries for the sub-strategies and positions in a given strategies portfolio.
    /// If the position is in a strategy, a synthetic timeseries is generated by aggregating the sub-strategies positions and timeseries.
    /// The volatility of these timeseries is an absolute cash volatility
    /// </summary>
    /// <param name="strategy">parent strategy</param>
    /// <param name="orderDate">reference date</param>
    /// <param name="reference_aum">aum of parent strategy</param>
    /// <param name="days_back">number of days used in the timeseries</param>
    /// <param name="fx_hedge">number of days used in the timeseries</param>
    let TimeSeriesMap (strategy : Strategy, instruments : seq<Instrument>, orderDate: BusinessDay, reference_aum : double, days_back : int, fx_hedge : bool, current: bool) =
        let max_log_chg = 0.5
        //let timeSeriesMapDirty = instruments
        let timeSeriesMap = instruments
                                |> Seq.filter (fun instrument -> // filter out reserve instruments and instruments without timeseries data
                                    let ttype = if instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.ETF || instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Equity || instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Fund then TimeSeriesType.AdjClose else if instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Strategy then TimeSeriesType.Last else TimeSeriesType.Close
                                    
                                    match instrument with
                                    | x when x.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Strategy && (strategy.Portfolio.IsReserve(instrument)) -> false
                                    | x when x.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Strategy && ((x :?> Strategy).IsResidual) -> false
                                    | x when x.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Strategy && (not ((x :?> Strategy).Portfolio = null)) ->                                        
                                        Seq.toList ((instrument :?> Strategy).Instruments(orderDate.DateTime, true).Values)
                                        |> List.filter (fun sub_instrument -> 
                                            let sttype = if sub_instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.ETF || sub_instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Equity || sub_instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Fund then TimeSeriesType.AdjClose elif sub_instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Strategy then TimeSeriesType.Last else TimeSeriesType.Close
                                            let sub_ts = sub_instrument.GetTimeSeries(sttype)   
                                                                                                                     
                                            let idx = if sub_ts = null || sub_ts.Count = 0 then 0 else sub_ts.GetClosestDateIndex(orderDate.DateTime, TimeSeries.DateSearchType.Previous)                                            
                                            
                                            (not (strategy.Portfolio.IsReserve(sub_instrument))) && (idx > 5) && not(sub_ts = null)) |> List.length > 0 
                                    | _ -> 
                                        let ts_s = instrument.GetTimeSeries(ttype)

                                        let ts_s_count = if not (ts_s = null) then ts_s.Count else 0
                                        let idx_s = if ts_s_count > 0 then ts_s.GetClosestDateIndex(orderDate.DateTime, TimeSeries.DateSearchType.Previous) else 0                                                                
                                        
                                        (not (strategy.Portfolio.IsReserve(instrument)) && idx_s > 5 && not(ts_s = null)))
                                
                                |> Seq.groupBy (fun instrument -> instrument.ID)
                                |> Map.ofSeq
                                |> Map.map (fun id tuple ->                                                 // Generate timeseries
                                    let instrument = Instrument.FindInstrument(id)
                                                                        
                                    let dateList = new System.Collections.Generic.List<DateTime>()
                                    [|0 .. days_back|] |> Array.iter(fun i -> dateList.Add(orderDate.AddBusinessDays(- days_back + i).DateTime))

                                    match instrument with
                                    | x when x.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Strategy && (not ((x :?> Strategy).Portfolio = null)) ->
                                        let sub_strategy = x :?> Strategy
                                        let portfolio = sub_strategy.Portfolio
                                        let positions = 
                                                if current then
                                                    portfolio.Positions(orderDate.DateTime, true)
                                                    |> Seq.map(fun position -> (position.Instrument.ID, position.Unit))
                                                    |> Map.ofSeq
                                                else
                                                    portfolio.PositionOrders(orderDate.DateTime, true).Values
                                                    |> Seq.map(fun position -> (position.Instrument.ID, position.Unit))
                                                    |> Map.ofSeq

                                        let (ts, value) = 
                                            Seq.toList (strategy.Instruments(orderDate.DateTime, true))                                        
                                            |> List.filter (fun value -> 
                                                let instrument = value.Value
                                                                                        
                                                let sttype = if instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.ETF || instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Equity || instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Fund then TimeSeriesType.AdjClose elif instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Strategy then TimeSeriesType.Last else TimeSeriesType.Close
                                                let sub_ts = instrument.GetTimeSeries(sttype)                                
                                                let idx = if sub_ts = null || sub_ts.Count = 0 then 0 else sub_ts.GetClosestDateIndex(orderDate.DateTime, TimeSeries.DateSearchType.Previous)                                            

                                                (not (portfolio.IsReserve(instrument))) && ((idx > 1)))
                                                                                        
                                            |> List.map (fun value -> 
                                                let sub_instrument = value.Value
                                                let posval =(if positions.ContainsKey(sub_instrument.ID) then positions.[sub_instrument.ID] else 0.0)
                                                                                                                                                
                                                let ttype = if sub_instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.ETF || sub_instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Equity || sub_instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Fund then TimeSeriesType.AdjClose else if sub_instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Strategy then TimeSeriesType.Last else TimeSeriesType.Close
                                                let ts_s = sub_instrument.GetTimeSeries(ttype)
                                                                                        
                                                let normalized_ts = new TimeSeries(dateList.Count, new DateTimeList(dateList))
                                                
                                                let date_ref = normalized_ts.DateTimes.[days_back]
                                                //let fx_ref = CurrencyPair.Convert(1.0, date_ref, TimeSeriesType.Close, DataProvider.DefaultProvider, TimeSeriesRollType.Last, strategy.Portfolio.Currency, sub_instrument.Currency)
                                                let fx_ref = CurrencyPair.Convert(1.0, date_ref, strategy.Portfolio.Currency, sub_instrument.Currency)
                                                let subval_ref = sub_instrument.[date_ref, ttype, TimeSeriesRollType.Last] * (if Double.IsNaN(fx_ref) then 1.0 else fx_ref) * (if sub_instrument :? Security then (sub_instrument :?> Security).PointSize else 1.0)

                                                let wgt = subval_ref * posval / reference_aum

                                        
                                                [|0 .. days_back|] |> Array.iter(fun i ->                                                                                                                                                                                                                                                                                                                                 
                                                                                        let date = normalized_ts.DateTimes.[i]
                                                                                        //let fx = CurrencyPair.Convert(1.0, (if fx_hedge then orderDate.DateTime else date), TimeSeriesType.Close, DataProvider.DefaultProvider, TimeSeriesRollType.Last, strategy.Portfolio.Currency, sub_instrument.Currency)
                                                                                        let fx = CurrencyPair.Convert(1.0, (if fx_hedge then orderDate.DateTime else date), strategy.Portfolio.Currency, sub_instrument.Currency)
                                                                                        let subval = sub_instrument.[date, ttype, TimeSeriesRollType.Last] * (if Double.IsNaN(fx) then 1.0 else fx) * (if sub_instrument :? Security then (sub_instrument :?> Security).PointSize else 1.0)
                                                                                        normalized_ts.[i] <-  subval / subval_ref)
                                                                                               
                                                normalized_ts * wgt
                                                )
                                            |> List.fold (fun (acc : TimeSeries, aggval) value ->
                                                let subval = value.[value.Count - 1]
                                                let sub_ts_diff = (value.DifferenceReturn()).ReplaceNaN(0.0)
                                                                                                                                                                        
                                                (match acc with
                                                | x when x =  null -> sub_ts_diff
                                                | x when x.Count = sub_ts_diff.Count -> acc + sub_ts_diff                                                
                                                | _ -> acc), aggval + subval)  (null, 0.0)
                                        (ts * reference_aum).ReplaceNaN(0.0)                                        
                                        

                                    | _ -> 
                                        let ttype = if instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.ETF || instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Equity || instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Fund then TimeSeriesType.AdjClose else if instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Strategy then TimeSeriesType.Last else TimeSeriesType.Close
                                        let ts_s = instrument.GetTimeSeries(ttype)
                                        
                                        let normalized_ts = new TimeSeries(dateList.Count, new DateTimeList(dateList))
                                        let date_ref = normalized_ts.DateTimes.[days_back]
                                        //let fx_ref = CurrencyPair.Convert(1.0, date_ref, TimeSeriesType.Close, DataProvider.DefaultProvider, TimeSeriesRollType.Last, strategy.Portfolio.Currency, instrument.Currency)
                                        let fx_ref = CurrencyPair.Convert(1.0, date_ref, strategy.Portfolio.Currency, instrument.Currency)
                                        let subval_ref = instrument.[date_ref, ttype, TimeSeriesRollType.Last] * (if Double.IsNaN(fx_ref) then 1.0 else fx_ref) * (if instrument :? Security then (instrument :?> Security).PointSize else 1.0)
                                        
                                        [|0 .. days_back|] |> Array.iter(fun i ->                                                                                                                                                                                                                                                                                                                                 
                                                                                let date = normalized_ts.DateTimes.[i]
                                                                                //let fx = CurrencyPair.Convert(1.0, (if fx_hedge then orderDate.DateTime else date), TimeSeriesType.Close, DataProvider.DefaultProvider, TimeSeriesRollType.Last, strategy.Portfolio.Currency, instrument.Currency)
                                                                                let fx = CurrencyPair.Convert(1.0, (if fx_hedge then orderDate.DateTime else date), strategy.Portfolio.Currency, instrument.Currency)
                                                                                let subval = instrument.[date, ttype, TimeSeriesRollType.Last] * (if Double.IsNaN(fx) then 1.0 else fx) * (if instrument :? Security then (instrument :?> Security).PointSize else 1.0)
                                                                                normalized_ts.[i] <- reference_aum * subval / subval_ref)

                                        normalized_ts.DifferenceReturn().ReplaceNaN(0.0))
                                |> Map.filter (fun id tuple -> not (tuple = null))
        
        timeSeriesMap 
    
    let VaRAggregated (strategies : seq<Strategy>, orderDate : DateTime, days_window : int, days_back : int, level : float, current : bool) =
        let level = 1.0 - level
        
        let aggReturnsList =
            strategies
            |> Seq.map(fun strategy ->
            let returnsList = 
                if current then
                    let instruments = strategy.Portfolio.Positions(orderDate, true)
                    Seq.toArray instruments
                    |> Array.filter (fun pos -> not(strategy.Portfolio.IsReserve(pos.Instrument)))
                    |> Array.map (fun pos -> 
                        let instrument = pos.Instrument  
                        let ttype = if instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.ETF || instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Equity || instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Fund then TimeSeriesType.AdjClose elif instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Strategy then TimeSeriesType.Last else TimeSeriesType.Close
                        let insvalue = pos.Instrument.[orderDate, ttype, TimeSeriesRollType.Last];
                        let fx = CurrencyPair.Convert(1.0, orderDate, strategy.Currency, pos.Instrument.Currency);

                        let ts = instrument.GetTimeSeries(ttype)                                
                        let idx = ts.GetClosestDateIndex(orderDate, TimeSeries.DateSearchType.Previous)

                        let scale = (if instrument :? Security then (instrument :?> Security).PointSize else 1.0)

                        [|1 .. days_back|] 
                        |> Array.map (fun i ->
                                let first = Math.Max(0, idx - days_back + i - days_window)
                                let last = Math.Max(0, idx - days_back + i)
                                let fx_t = CurrencyPair.Convert(1.0, ts.DateTimes.[last], TimeSeriesType.Close, DataProvider.DefaultProvider, TimeSeriesRollType.Last, strategy.Portfolio.Currency, instrument.Currency)
                                let fx_0 = CurrencyPair.Convert(1.0, ts.DateTimes.[first], TimeSeriesType.Close, DataProvider.DefaultProvider, TimeSeriesRollType.Last, strategy.Portfolio.Currency, instrument.Currency)
                                let ret = (ts.[last] * fx_t) - (ts.[first] * fx_0)
                                ret * pos.Unit * scale)
                    )
            
                else
                    let instruments = strategy.Portfolio.PositionOrders(orderDate, true)
                    Seq.toArray instruments.Values
                    |> Array.filter (fun pos -> not(strategy.Portfolio.IsReserve(pos.Instrument)))
                    |> Array.map (fun pos -> 
                        let instrument = pos.Instrument  
                        let ttype = if instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.ETF || instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Equity || instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Fund then TimeSeriesType.AdjClose elif instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Strategy then TimeSeriesType.Last else TimeSeriesType.Close
                        let insvalue = pos.Instrument.[orderDate, ttype, TimeSeriesRollType.Last];
                        let fx = CurrencyPair.Convert(1.0, orderDate, strategy.Currency, pos.Instrument.Currency);

                        let ts = instrument.GetTimeSeries(ttype)                                
                        let idx = ts.GetClosestDateIndex(orderDate, TimeSeries.DateSearchType.Previous)

                        let scale = (if instrument :? Security then (instrument :?> Security).PointSize else 1.0)

                        [|1 .. days_back|] 
                        |> Array.map (fun i ->
                                let first = Math.Max(0, idx - days_back + i - days_window)
                                let last = Math.Max(0, idx - days_back + i)
                                let fx_t = CurrencyPair.Convert(1.0, ts.DateTimes.[last], TimeSeriesType.Close, DataProvider.DefaultProvider, TimeSeriesRollType.Last, strategy.Portfolio.Currency, instrument.Currency)
                                let fx_0 = CurrencyPair.Convert(1.0, ts.DateTimes.[first], TimeSeriesType.Close, DataProvider.DefaultProvider, TimeSeriesRollType.Last, strategy.Portfolio.Currency, instrument.Currency)
                                let ret = (ts.[last] * fx_t) - (ts.[first] * fx_0)
                                ret * pos.Unit * scale)
                        )
                                    
            //let rets = returnsList|> List.fold (fun (acc : float list) rets -> [1 .. days_back] |> List.map( fun j -> acc.[j - 1] + rets.[j - 1])) (Array.zeroCreate days_back |> Array.toList) |> List.sort
            let rets = returnsList |> Array.fold (fun (acc : float[] ) rets -> [|1 .. days_back|] |> Array.map( fun j -> acc.[j - 1] + rets.[j - 1])) (Array.zeroCreate days_back) |> Array.sort
            rets
            )
            |> Seq.toArray

        let rets = aggReturnsList |> Array.fold (fun (acc : float[] ) rets -> [|1 .. days_back|] |> Array.map( fun j -> acc.[j - 1] + rets.[j - 1])) (Array.zeroCreate days_back) |> Array.sort
        let pctl = level * (double (rets.Length - 1)) + 1.0;
        let pctl_n = (int)pctl
        let pctl_d = pctl - (double)pctl_n
        let VaR = (rets.[pctl_n] + pctl_d * (rets.[pctl_n + 1] - rets.[pctl_n])) // reference_aum
        VaR
    
    let VaR (strategy : Strategy, orderDate : DateTime, days_window : int, days_back : int, level : float, current : bool) =
        //let days_window = 20
        //let days_back = 252
        //let level = 0.01
        let level = 1.0 - level

        //let reference_aum = strategy.GetSODAUM(orderDate, TimeSeriesType.Last)
        
        
        let returnsList = 
            if current then
                let instruments = strategy.Portfolio.Positions(orderDate, true)
                Seq.toArray instruments
                |> Array.filter (fun pos -> not(strategy.Portfolio.IsReserve(pos.Instrument)))
                |> Array.map (fun pos -> 
                    let instrument = pos.Instrument  
                    let ttype = if instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.ETF || instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Equity || instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Fund then TimeSeriesType.AdjClose elif instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Strategy then TimeSeriesType.Last else TimeSeriesType.Close
                    let insvalue = pos.Instrument.[orderDate, ttype, TimeSeriesRollType.Last];
                    let fx = CurrencyPair.Convert(1.0, orderDate, strategy.Currency, pos.Instrument.Currency);

                    let ts = instrument.GetTimeSeries(ttype)                                
                    let idx = ts.GetClosestDateIndex(orderDate, TimeSeries.DateSearchType.Previous)

                    let scale = (if instrument :? Security then (instrument :?> Security).PointSize else 1.0)
                    //let weightPos = pos.Unit * scale * insvalue * fx // reference_aum         

                    [|1 .. days_back|] 
                    |> Array.map (fun i ->
                            let first = Math.Max(0, idx - days_back + i - days_window)
                            let last = Math.Max(0, idx - days_back + i)
                            let fx_t = CurrencyPair.Convert(1.0, ts.DateTimes.[last], TimeSeriesType.Close, DataProvider.DefaultProvider, TimeSeriesRollType.Last, strategy.Portfolio.Currency, instrument.Currency)
                            let fx_0 = CurrencyPair.Convert(1.0, ts.DateTimes.[first], TimeSeriesType.Close, DataProvider.DefaultProvider, TimeSeriesRollType.Last, strategy.Portfolio.Currency, instrument.Currency)
                            //let fx_t = CurrencyPair.Convert(1.0, ts.DateTimes.[last], strategy.Portfolio.Currency, instrument.Currency)
                            //let fx_0 = CurrencyPair.Convert(1.0, ts.DateTimes.[first], strategy.Portfolio.Currency, instrument.Currency)
                            let ret = (ts.[last] * fx_t) - (ts.[first] * fx_0)
                            ret * pos.Unit * scale)
                )
            
            else
                let instruments = strategy.Portfolio.PositionOrders(orderDate, true)
                Seq.toArray instruments.Values
                |> Array.filter (fun pos -> not(strategy.Portfolio.IsReserve(pos.Instrument)))
                |> Array.map (fun pos -> 
                    let instrument = pos.Instrument  
                    let ttype = if instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.ETF || instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Equity || instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Fund then TimeSeriesType.AdjClose elif instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Strategy then TimeSeriesType.Last else TimeSeriesType.Close
                    let insvalue = pos.Instrument.[orderDate, ttype, TimeSeriesRollType.Last];
                    let fx = CurrencyPair.Convert(1.0, orderDate, strategy.Currency, pos.Instrument.Currency);

                    let ts = instrument.GetTimeSeries(ttype)                                
                    let idx = ts.GetClosestDateIndex(orderDate, TimeSeries.DateSearchType.Previous)

                    let scale = (if instrument :? Security then (instrument :?> Security).PointSize else 1.0)
                    //let weightPos = pos.Unit * scale * insvalue * fx // reference_aum         

                    [|1 .. days_back|] 
                    |> Array.map (fun i ->
                            let first = Math.Max(0, idx - days_back + i - days_window)
                            let last = Math.Max(0, idx - days_back + i)
                            let fx_t = CurrencyPair.Convert(1.0, ts.DateTimes.[last], TimeSeriesType.Close, DataProvider.DefaultProvider, TimeSeriesRollType.Last, strategy.Portfolio.Currency, instrument.Currency)
                            let fx_0 = CurrencyPair.Convert(1.0, ts.DateTimes.[first], TimeSeriesType.Close, DataProvider.DefaultProvider, TimeSeriesRollType.Last, strategy.Portfolio.Currency, instrument.Currency)
                            //let fx_t = CurrencyPair.Convert(1.0, ts.DateTimes.[last], strategy.Portfolio.Currency, instrument.Currency)
                            //let fx_0 = CurrencyPair.Convert(1.0, ts.DateTimes.[first], strategy.Portfolio.Currency, instrument.Currency)
                            let ret = (ts.[last] * fx_t) - (ts.[first] * fx_0)
                            ret * pos.Unit * scale)
                    )
                                    
        //let rets = returnsList|> List.fold (fun (acc : float list) rets -> [1 .. days_back] |> List.map( fun j -> acc.[j - 1] + rets.[j - 1])) (Array.zeroCreate days_back |> Array.toList) |> List.sort
        let rets = returnsList |> Array.fold (fun (acc : float[] ) rets -> [|1 .. days_back|] |> Array.map( fun j -> acc.[j - 1] + rets.[j - 1])) (Array.zeroCreate days_back) |> Array.sort
        let pctl = level * (double (rets.Length - 1)) + 1.0;
        let pctl_n = (int)pctl
        let pctl_d = pctl - (double)pctl_n
        let VaR = (rets.[pctl_n] + pctl_d * (rets.[pctl_n + 1] - rets.[pctl_n])) // reference_aum
        VaR

    let VaRInstrument (instrument : Instrument, orderDate : DateTime, ccy : Currency, days_window : int, days_back : int, level : float) = 
        let level = 1.0 - level
        try
            let ttype = if instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.ETF || instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Equity || instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Fund then TimeSeriesType.AdjClose elif instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Strategy then TimeSeriesType.Last else TimeSeriesType.Close              
            let ts = instrument.GetTimeSeries(ttype)                                
            let idx = ts.GetClosestDateIndex(orderDate, TimeSeries.DateSearchType.Previous)

            let returnsList = 
                [|1 .. days_back|]
                |> Array.map (fun i ->
                let first = Math.Max(0, idx - days_back + i - days_window)
                let last = Math.Max(0, idx - days_back + i)
                let fx_t = CurrencyPair.Convert(1.0, ts.DateTimes.[last], TimeSeriesType.Last, DataProvider.DefaultProvider, TimeSeriesRollType.Last, ccy, instrument.Currency)
                let fx_0 = CurrencyPair.Convert(1.0, ts.DateTimes.[first], TimeSeriesType.Last, DataProvider.DefaultProvider, TimeSeriesRollType.Last, ccy, instrument.Currency)
                let ret = (ts.[last] * fx_t) / (ts.[first] * fx_0) - 1.0                                    
                ret)
        
            let rets = returnsList |> Array.filter(Double.IsNaN >> not) |> Array.sort
            let pctl = level * (double (rets.Length - 1)) + 1.0;
            let pctl_n = (int)pctl
            let pctl_d = pctl - (double)pctl_n
            let VaR = (rets.[pctl_n] + pctl_d * (rets.[pctl_n + 1] - rets.[pctl_n]))
            VaR
        with
        | ex -> 
            Console.WriteLine(ex)
            0.0

    let VaRPosition (instrument : Instrument, orderDate : DateTime, ccy : Currency, days_window : int, days_back : int, level : float, unit : float) = 
        let level = 1.0 - level
        try
            let ttype = if instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.ETF || instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Equity || instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Fund then TimeSeriesType.AdjClose elif instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Strategy then TimeSeriesType.Last else TimeSeriesType.Close              
            let ts = instrument.GetTimeSeries(ttype)                                
            let idx = ts.GetClosestDateIndex(orderDate, TimeSeries.DateSearchType.Previous)

            let returnsList = 
                [|1 .. days_back|]
                |> Array.map (fun i ->
                let first = Math.Max(0, idx - days_back + i - days_window)
                let last = Math.Max(0, idx - days_back + i)
                let fx_t = CurrencyPair.Convert(1.0, ts.DateTimes.[last], TimeSeriesType.Last, DataProvider.DefaultProvider, TimeSeriesRollType.Last, ccy, instrument.Currency)
                let fx_0 = CurrencyPair.Convert(1.0, ts.DateTimes.[first], TimeSeriesType.Last, DataProvider.DefaultProvider, TimeSeriesRollType.Last, ccy, instrument.Currency)
                let ret = ((ts.[last] * fx_t) - (ts.[first] * fx_0)) * unit
                ret)
        
            let rets = returnsList |> Array.sort
            let pctl = level * (double (rets.Length - 1)) + 1.0;
            let pctl_n = (int)pctl
            let pctl_d = pctl - (double)pctl_n
            let VaR = (rets.[pctl_n] + pctl_d * (rets.[pctl_n + 1] - rets.[pctl_n]))
            VaR
        with
        | ex -> 
            Console.WriteLine(ex)
            0.0

    let VolatilityAttribution (strategy : Strategy , date : DateTime, days_back : int, current : bool, attribution : bool) =
            
        let agg = true
        //let ref_aum = if current then strategy.Portfolio.MasterPortfolio.Strategy.GetAUM(date.Date, TimeSeriesType.Last) else strategy.Portfolio.MasterPortfolio.Strategy.GetSODAUM(date, TimeSeriesType.Last)
        let ref_aum = 
            if attribution then
                if current then strategy.Portfolio.MasterPortfolio.Strategy.GetAUM(date.Date, TimeSeriesType.Last) else strategy.Portfolio.MasterPortfolio.Strategy.GetSODAUM(date, TimeSeriesType.Last)
            else

                if current then strategy.GetAUM(date.Date, TimeSeriesType.Last) else strategy.GetSODAUM(date, TimeSeriesType.Last)

        //let timeSeriesMap = TimeSeriesMap(strategy, strategy.Instruments(date, agg).Values, strategy.Calendar.GetClosestBusinessDay(date, TimeSeries.DateSearchType.Previous), ref_aum, days_back, true, current)
        let timeSeriesMap = TimeSeriesMap(strategy, strategy.Instruments(date, agg).Values, Calendar.FindCalendar("All").GetClosestBusinessDay(date, TimeSeries.DateSearchType.Previous), ref_aum, days_back, true, current)

        
        let spositions = 
            
                if current then
                    let positions = strategy.Portfolio.Positions(date, agg)
                    positions 
                    |> Seq.filter (fun i -> not(strategy.Portfolio.IsReserve(i.Instrument)) && timeSeriesMap.ContainsKey(i.InstrumentID))
                    |> Seq.map(fun p -> (p.Instrument, p.Unit))
                else
                    let positions = strategy.Portfolio.PositionOrders(date, agg)
                    positions.Values 
                    |> Seq.filter (fun i -> not(strategy.Portfolio.IsReserve(i.Instrument)) && timeSeriesMap.ContainsKey(i.InstrumentID))
                    |> Seq.map(fun p -> (p.Instrument, p.Unit))


        let stratValue (strategy : Strategy) = 
                        //let timeSeriesMap = TimeSeriesMap(strategy, strategy.Instruments(date, true).Values, strategy.Calendar.GetClosestBusinessDay(date, TimeSeries.DateSearchType.Previous), ref_aum, days_back, true, current)   
                        let timeSeriesMap = TimeSeriesMap(strategy, strategy.Instruments(date, true).Values, Calendar.FindCalendar("All").GetClosestBusinessDay(date, TimeSeries.DateSearchType.Previous), ref_aum, days_back, true, current)   
                        let spositions = 
                            if current then
                                let positions = strategy.Portfolio.Positions(date, true)
                                positions 
                                |> Seq.filter (fun i -> not(strategy.Portfolio.IsReserve(i.Instrument)) && timeSeriesMap.ContainsKey(i.InstrumentID))
                                |> Seq.map(fun p -> (p.Instrument, p.Unit))
                            else
                                let positions = strategy.Portfolio.PositionOrders(date, true)
                                positions.Values 
                                |> Seq.filter (fun i -> not(strategy.Portfolio.IsReserve(i.Instrument)) && timeSeriesMap.ContainsKey(i.InstrumentID))
                                |> Seq.map(fun p -> (p.Instrument, p.Unit))                    
                        let value =
                            spositions
                            |> Seq.filter(fun (instrument, unit) -> not (strategy.Portfolio.IsReserve(instrument)))
                            |> Seq.fold(fun acc (instrument, unit) ->                                                        
                                                        let fx = CurrencyPair.Convert(1.0, date, TimeSeriesType.Close, DataProvider.DefaultProvider, TimeSeriesRollType.Last, strategy.Portfolio.Currency, instrument.Currency)
                                                        acc + unit * instrument.[date, TimeSeriesType.Last, TimeSeriesRollType.Last] * fx * (if instrument :? Security then (instrument :?> Security).PointSize else 1.0)
                                        ) 0.0
                        value

        let weightedTS = 
            spositions 
            |> Seq.fold(fun acc (sinstrument, unit) ->
                            //let sinstrument = vp.Instrument
                            let unit = unit * (if sinstrument :? Security then (sinstrument :?> Security).PointSize else 1.0)                                                                    
                            let value = 
                                if sinstrument.InstrumentType = InstrumentType.Strategy then
                                    stratValue(sinstrument :?> Strategy)
                                else
                                    //sinstrument.[date, (if sinstrument.InstrumentType = InstrumentType.Strategy then TimeSeriesType.Last else TimeSeriesType.AdjClose), TimeSeriesRollType.Last]
                                    sinstrument.[date, (if sinstrument.InstrumentType = InstrumentType.Strategy then TimeSeriesType.Last else TimeSeriesType.Close), TimeSeriesRollType.Last]

                            let fx = CurrencyPair.Convert(1.0, date, TimeSeriesType.Close, DataProvider.DefaultProvider, TimeSeriesRollType.Last, strategy.Portfolio.Currency, sinstrument.Currency)

                            let ts = timeSeriesMap.[sinstrument.ID]
                            let weight = fx * unit * value / ref_aum

                            if acc = null then weight * ts else acc + weight * ts) null

        let vol = if weightedTS = null then 0.0 else sqrt((weightedTS / ref_aum).Variance * 252.0) 
        vol

    let Volatility (strategy : Strategy , date : DateTime, days_back : int, current : bool) =
        VolatilityAttribution(strategy, date, days_back, current, false)
        
    let VolatilityEqw (strategy : Strategy , date : DateTime, days_back : int) =
    
        let agg = true
        let ref_aum = strategy.Portfolio.MasterPortfolio.Strategy.GetAUM(date.Date, TimeSeriesType.Last)
        let timeSeriesMap = TimeSeriesMap(strategy, strategy.Instruments(date, agg).Values, strategy.Calendar.GetClosestBusinessDay(date, TimeSeries.DateSearchType.Previous), ref_aum, days_back, true, true)
                    
        let weightedTS = 
            strategy.Instruments(date, true).Values
            |> Seq.filter(fun sinstrument -> timeSeriesMap.ContainsKey(sinstrument.ID))
            |> Seq.fold(fun acc sinstrument ->
                            
                            let unit = (if sinstrument :? Security then (sinstrument :?> Security).PointSize else 1.0)                                                                    
                            let value = sinstrument.[date, (if sinstrument.InstrumentType = InstrumentType.Strategy then TimeSeriesType.Last else TimeSeriesType.Close), TimeSeriesRollType.Last]

                            let fx = CurrencyPair.Convert(1.0, date, TimeSeriesType.Close, DataProvider.DefaultProvider, TimeSeriesRollType.Last, strategy.Portfolio.Currency, sinstrument.Currency)

                            let ts = timeSeriesMap.[sinstrument.ID]
                            
                            let weight = (1.0 / (float) timeSeriesMap.Count)// / ref_aum

                            if acc = null then weight * ts else acc + weight * ts) null

        let vol = if weightedTS = null then 0.0 else sqrt((weightedTS / ref_aum).Variance * 252.0) 
        vol

    let AverageCorrelation (strategy : Strategy , date : DateTime, days_back : int) =
        let reference_aum = strategy.GetSODAUM(date, TimeSeriesType.Last)
        let positions = strategy.Portfolio.PositionOrders(date, true)

        let wgts = positions.Values |> Seq.filter(fun position -> not (strategy.Portfolio.IsReserve(position.Instrument)))
                                    |> Seq.map(fun position -> 
                                                                let fx = CurrencyPair.Convert(1.0, date, TimeSeriesType.Close, DataProvider.DefaultProvider, TimeSeriesRollType.Last, strategy.Portfolio.Currency, position.Instrument.Currency)
                                                                (if Double.IsNaN(fx) then 1.0 else fx) * position.Instrument.[date, TimeSeriesType.Last, TimeSeriesRollType.Last] * position.Unit / reference_aum)
                                    |> Seq.toList

        let tsl = positions.Values |> Seq.filter(fun position -> not (strategy.Portfolio.IsReserve(position.Instrument)))
                                   |> Seq.map(fun position ->
                                                                    let instrument = position.Instrument
                                                                    
                                                                    let ttype = if instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.ETF || instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Equity then TimeSeriesType.AdjClose else if instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Strategy then TimeSeriesType.Last else TimeSeriesType.Close
                                                                    let ts_s = instrument.GetTimeSeries(ttype)
                                                                    let ts_s_count = if not (ts_s = null) then ts_s.Count else 0
                                                                    let idx_s = if ts_s_count > 0 then ts_s.GetClosestDateIndex(date, TimeSeries.DateSearchType.Previous) else 0
                                                                    let ts_s = instrument.GetTimeSeries(ttype).GetRange(1 , idx_s)

                            
                                                                    let fx = CurrencyPair.Convert(1.0, date, TimeSeriesType.Close, DataProvider.DefaultProvider, TimeSeriesRollType.Last, strategy.Portfolio.Currency, instrument.Currency)
                                                                    let normalized_ts = ((if Double.IsNaN(fx) then 1.0 else fx) * ts_s.GetRange(Math.Max(0 , ts_s.Count - 1 - days_back) , ts_s.Count - 1))                                                                    
                                                                    position.Unit * normalized_ts.DifferenceReturn().ReplaceNaN(0.0) / reference_aum
                                              )
                                    |> Seq.toList
        
        let tsl_count = List.length tsl
        if tsl_count = 0 then
            0.0
        else
            try
                let correlation = Correlation tsl
                [0 .. tsl_count - 1] 
                |> List.iter (fun i -> 
                                    [0 .. tsl_count - 1] 
                                    |> List.iter (fun j -> 
                                                        correlation.[i,j] <- if i = j then 0.0 else correlation.[i,j] * wgts.[i] * wgts.[j]
                                                 )
                             )
                let arr = correlation.ToColumnWiseArray()
                let sum = (wgts |> Seq.map(fun w -> w * w) |> Seq.fold(fun w acc -> w + acc) 0.0)/ ((float)tsl_count * (float)tsl_count)
                let avg = arr |> Seq.average
                avg / (if sum = 0.0 then 1.0 else sum)
            with
            _ -> 0.0

    let TurnOver (strategy : Strategy) annual =
        
        if strategy.Portfolio = null then
            0.0
        else

            strategy.Tree.Initialize()
            strategy.Tree.LoadPortfolioMemory()

            let ts = strategy.GetTimeSeries(TimeSeriesType.Last)

            let turnover = 
                [0 .. ts.Count - 1] 
                |> List.map(fun i -> ts.DateTimes.[i]) 
                |> List.map(fun date -> 
                    //let aum = if annual then strategy.GetAUM(date, TimeSeriesType.Last) else 1.0
                    let aum = if annual then strategy.Portfolio.MasterPortfolio.[date, TimeSeriesType.Last, TimeSeriesRollType.Last] else 1.0
                    strategy.Portfolio.Orders(date, true).Values
                    |> Seq.map(fun kp -> kp.Values)
                    |> Seq.concat
                    |> Seq.filter(fun o -> o.Status = OrderStatus.Booked)
                    |> Seq.fold(fun acc o -> 
                        let fx = CurrencyPair.Convert(1.0, date, TimeSeriesType.Close, DataProvider.DefaultProvider, TimeSeriesRollType.Last, strategy.Portfolio.Currency, o.Instrument.Currency)
                        acc + (Math.Abs(o.Unit) * o.ExecutionLevel * fx / aum) ) 0.0
                    )
                |> List.sum

            if annual then
                0.5 * turnover / ((float)(ts.DateTimes.[ts.Count - 1] - ts.DateTimes.[0]).TotalDays / 365.0)
            else
                turnover


    let VaROld (strategy : Strategy, orderDate : DateTime) =
        let reference_aum = strategy.GetSODAUM(orderDate, TimeSeriesType.Last)
        
        let instruments = strategy.Portfolio.Positions(orderDate, false);
        if instruments = null then
            0.0
        else
            let returnsList = 
                Seq.toList instruments
                |> List.filter (fun pos -> not(strategy.Portfolio.IsReserve(pos.Instrument)))
                |> List.map (fun pos -> 
                    let instrument = pos.Instrument  
                    let insvalue = pos.Instrument.[orderDate, TimeSeriesType.Last, TimeSeriesRollType.Last];
                    let fx = CurrencyPair.Convert(1.0, orderDate, strategy.Currency, pos.Instrument.Currency);

                    let scale = (if instrument :? Security then (instrument :?> Security).PointSize else 1.0)
                    let weightPos = pos.Unit * scale * insvalue * fx / reference_aum         
                                                    
                    match instrument with
                    | x when x.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Strategy && (not ((x :?> Strategy).Portfolio = null)) -> // Generate aggregated list of returns for each instrument in portfolio of this strategy
                        let strategy = x :?> Strategy
                        let strategy_aum = strategy.GetSODAUM(orderDate, TimeSeriesType.Last)
                                                                                
                        strategy.Portfolio.PositionOrders(orderDate, true).Values
                        |> Seq.toList
                        |> List.filter (fun order -> not (strategy.Portfolio.IsReserve(order.Instrument)))
                        |> List.fold (fun acc position ->
                            let ttype_sub = if position.Instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.ETF || position.Instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Equity then TimeSeriesType.AdjClose elif position.Instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Strategy then TimeSeriesType.Last else TimeSeriesType.Close;
                            let ts = position.Instrument.GetTimeSeries(ttype_sub)
                            let orders = strategy.Portfolio.FindOpenOrder(position.Instrument, orderDate, true)
                            let order = if orders = null || orders.Count = 0 then null else orders.Values  |> Seq.toList |> List.filter (fun o -> o.Type = OrderType.Market) |> List.reduce (fun acc o -> o) 
                            let idx = ts.GetClosestDateIndex(orderDate, TimeSeries.DateSearchType.Previous)
                            let weight = position.Unit * (if position.Instrument :? Security then (position.Instrument :?> Security).PointSize else 1.0);
                            let rets = [1 .. 252] 
                                    |> List.map (fun i ->
                                        let first = Math.Max(0, idx - 252 + i - 20)
                                        let last = Math.Max(0, idx - 252 + i)
                                        let fx_t = CurrencyPair.Convert(1.0, ts.DateTimes.[last], TimeSeriesType.Last, DataProvider.DefaultProvider, TimeSeriesRollType.Last, strategy.Portfolio.Currency, position.Instrument.Currency)
                                        let fx_0 = CurrencyPair.Convert(1.0, ts.DateTimes.[first], TimeSeriesType.Last, DataProvider.DefaultProvider, TimeSeriesRollType.Last, strategy.Portfolio.Currency, position.Instrument.Currency)
                                        let ret = (ts.[last] * fx_t) - (ts.[first] * fx_0)
                                        ret * weight * weightPos)
                            [1 .. 252] |> List.map (fun i -> acc.[i - 1] + rets.[i - 1])) (Array.zeroCreate 252 |> Array.toList)
                                        
                    | _ ->  // Generate list of returns for this instrument
                        let ttype = if instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.ETF || instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Equity then TimeSeriesType.Close elif instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Strategy then TimeSeriesType.Last else TimeSeriesType.Close              
                        let ts = instrument.GetTimeSeries(ttype)                                
                        let idx = ts.GetClosestDateIndex(orderDate, TimeSeries.DateSearchType.Previous)
                        let scale = (if instrument :? Security then (instrument :?> Security).PointSize else 1.0)
                    
                        [1 .. 252] |> List.map (fun i ->
                            let first = Math.Max(0, idx - 252 + i - 20)
                            let last = Math.Max(0, idx - 252 + i)
                            let fx_t = CurrencyPair.Convert(1.0, ts.DateTimes.[last], TimeSeriesType.Last, DataProvider.DefaultProvider, TimeSeriesRollType.Last, strategy.Portfolio.Currency, instrument.Currency)
                            let fx_0 = CurrencyPair.Convert(1.0, ts.DateTimes.[first], TimeSeriesType.Last, DataProvider.DefaultProvider, TimeSeriesRollType.Last, strategy.Portfolio.Currency, instrument.Currency)
                            let ret = (ts.[last] * fx_t) - (ts.[first] * fx_0)
                            //let baseRet = fx_t * ts.[last]
                            //if baseRet = 0.0 then 0.0 else ret * (weightPos * reference_aum) / baseRet))
                            ret * scale * (weightPos * reference_aum) / (fx_t * ts.[last] * scale)))
                                    
            let rets = returnsList|> List.fold (fun (acc : float list) rets -> [1 .. 252] |> List.map( fun j -> acc.[j - 1] + rets.[j - 1])) (Array.zeroCreate 252 |> Array.toList) |> List.sort
            let pctl = 0.01 * (double (rets.Length - 1)) + 1.0;
            let pctl_n = (int)pctl
            let pctl_d = pctl - (double)pctl_n
            let VaR = (rets.[pctl_n] + pctl_d * (rets.[pctl_n + 1] - rets.[pctl_n])) / reference_aum
            VaR

    let VolatilityOld (strategy : Strategy , date : DateTime, days_back : int) =        
        let positions = strategy.Portfolio.Positions(date, true)

        let reference_aum = if strategy.Portfolio.MasterPortfolio = strategy.Portfolio then
                                strategy.GetSODAUM(date, TimeSeriesType.Last)
                            else
                                positions |> Seq.filter(fun p -> not(strategy.Portfolio.IsReserve(p.Instrument)))
                                                 |> Seq.fold(fun acc p ->

                                                            let instrument = p.Instrument
                                                            let ttype = if instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.ETF || instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Equity then TimeSeriesType.AdjClose else if instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Strategy then TimeSeriesType.Last else TimeSeriesType.Close                                                            
                                                            let fx = CurrencyPair.Convert(1.0, date, TimeSeriesType.Close, DataProvider.DefaultProvider, TimeSeriesRollType.Last, strategy.Portfolio.Currency, instrument.Currency)
                                                            let unit = p.Unit
                                                            let scale = (if instrument :? Security then (instrument :?> Security).PointSize else 1.0)
                            
                                                            let value = instrument.[date, TimeSeriesType.Last, TimeSeriesRollType.Last]
                                                            acc + value * scale * unit *fx
                                                        ) 0.0
        
        if reference_aum = 0.0 then
            0.0
        elif positions = null then
            0.0
        else
            let tsl = positions |> Seq.filter(fun position -> not (strategy.Portfolio.IsReserve(position.Instrument)))
                                
                                        
                                |> Seq.map(fun position ->
                                                                let instrument = position.Instrument
                                                                    
                                                                let ttype = if instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.ETF || instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Equity then TimeSeriesType.AdjClose else if instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Strategy then TimeSeriesType.Last else TimeSeriesType.Close
                                                                let ts_s = instrument.GetTimeSeries(ttype)
                                                                let ts_s_count = if not (ts_s = null) then ts_s.Count else 0
                                                                let idx_s = if ts_s_count > 0 then ts_s.GetClosestDateIndex(date, TimeSeries.DateSearchType.Previous) else 0
                                                                let ts_s = instrument.GetTimeSeries(ttype).GetRange(1 , idx_s)

                                                                let scale = (if instrument :? Security then (instrument :?> Security).PointSize else 1.0)
                            
                                                                let fx = CurrencyPair.Convert(1.0, date, TimeSeriesType.Close, DataProvider.DefaultProvider, TimeSeriesRollType.Last, strategy.Portfolio.Currency, instrument.Currency)
                                                                let normalized_ts = ((if Double.IsNaN(fx) then 1.0 else fx) * ts_s.GetRange(Math.Max(0 , ts_s.Count - 1 - days_back) , ts_s.Count - 1))                                                                    
                                                                scale * position.Unit * normalized_ts.DifferenceReturn().ReplaceNaN(0.0) / reference_aum
                                            )
                                |> Seq.toList

            let aggregatedTimeSeries = [0 .. tsl.Length - 1] |> List.map (fun i ->  tsl.[i]) |> List.fold (fun acc ts -> 
                                                                                                                        if acc = null then 
                                                                                                                            ts 
                                                                                                                        else 
                                                                                                                            try 
                                                                                                                                acc + ts 
                                                                                                                            with 
                                                                                                                            _ -> acc) null
            let portfolio_vol = if aggregatedTimeSeries = null then 0.0 else sqrt((aggregatedTimeSeries).Variance * 252.0)
       
            portfolio_vol

    let AverageCorrelationOld (strategy : Strategy , date : DateTime, days_back : int) =
        let reference_aum = strategy.GetSODAUM(date, TimeSeriesType.Last)
        let positions = strategy.Portfolio.Positions(date, true)
        if positions = null then
            0.0
        else
            let wgts = positions |> Seq.filter(fun position -> not (strategy.Portfolio.IsReserve(position.Instrument)))
                                        |> Seq.map(fun position -> 
                                                                    let fx = CurrencyPair.Convert(1.0, date, TimeSeriesType.Close, DataProvider.DefaultProvider, TimeSeriesRollType.Last, strategy.Portfolio.Currency, position.Instrument.Currency)
                                                                    (if Double.IsNaN(fx) then 1.0 else fx) * position.Instrument.[date, TimeSeriesType.Last, TimeSeriesRollType.Last] * position.Unit / reference_aum)
                                        |> Seq.toList

            let tsl = positions |> Seq.filter(fun position -> not (strategy.Portfolio.IsReserve(position.Instrument)))
                                       |> Seq.map(fun position ->
                                                                        let instrument = position.Instrument
                                                                    
                                                                        let ttype = if instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.ETF || instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Equity then TimeSeriesType.AdjClose else if instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Strategy then TimeSeriesType.Last else TimeSeriesType.Close
                                                                        let ts_s = instrument.GetTimeSeries(ttype)
                                                                        let ts_s_count = if not (ts_s = null) then ts_s.Count else 0
                                                                        let idx_s = if ts_s_count > 0 then ts_s.GetClosestDateIndex(date, TimeSeries.DateSearchType.Previous) else 0
                                                                        let ts_s = instrument.GetTimeSeries(ttype).GetRange(1 , idx_s)

                            
                                                                        let fx = CurrencyPair.Convert(1.0, date, TimeSeriesType.Close, DataProvider.DefaultProvider, TimeSeriesRollType.Last, strategy.Portfolio.Currency, instrument.Currency)
                                                                        let normalized_ts = ((if Double.IsNaN(fx) then 1.0 else fx) * ts_s.GetRange(Math.Max(0 , ts_s.Count - 1 - days_back) , ts_s.Count - 1))                                                                    
                                                                        position.Unit * normalized_ts.DifferenceReturn().ReplaceNaN(0.0) / reference_aum
                                                  )
                                        |> Seq.toList
        
            let tsl_count = List.length tsl
            if tsl_count = 0 then
                0.0
            else
                try
                    let correlation = Correlation tsl
                    [0 .. tsl_count - 1] 
                    |> List.iter (fun i -> 
                                        [0 .. tsl_count - 1] 
                                        |> List.iter (fun j -> 
                                                            correlation.[i,j] <- if i = j then 0.0 else correlation.[i,j] * wgts.[i] * wgts.[j]
                                                     )
                                 )
                    let arr = correlation.ToColumnWiseArray()
                    let sum = (wgts |> Seq.map(fun w -> w * w) |> Seq.fold(fun w acc -> w + acc) 0.0)/ ((float)tsl_count * (float)tsl_count)
                    let avg = arr |> Seq.average
                    avg / (if sum = 0.0 then 1.0 else sum)
                with
                _ -> 0.0

    let Risks (strategy : Strategy, date : DateTime) =
        let ts = strategy.GetTimeSeries(TimeSeriesType.Last)
        let (max, drawdown) = ts |> Seq.fold(fun acc i -> ((if (fst acc) > i then (fst acc) else i), (if i / (fst acc) - 1.0 < (snd acc) then i / (fst acc) - 1.0  else snd acc))) (100.0, 100.0)
        let length = ts.Count
        let t = (ts.DateTimes.[length - 1] - ts.DateTimes.[0]).TotalDays / 365.0
        let vol = sqrt(ts.LogReturn().Variance * 252.0)
        
        let irr = Math.Pow(ts.[length - 1] / ts.[0] , (1.0 / t)) - 1.0

        //let date = DateTime.Now
        //let reference_aum = strategy.GetSODAUM(date, TimeSeriesType.Last)
        let reference_aum = strategy.Portfolio.[date, TimeSeriesType.Last, TimeSeriesRollType.Last]
        let days_back = 60
        
        let positions = strategy.Portfolio.Positions(date, true)

        let wgts = positions |> Seq.filter(fun position -> not (strategy.Portfolio.IsReserve(position.Instrument)))
                                    |> Seq.map(fun position -> 
                                                                let fx = CurrencyPair.Convert(1.0, date, TimeSeriesType.Close, DataProvider.DefaultProvider, TimeSeriesRollType.Last, strategy.Portfolio.Currency, position.Instrument.Currency)
                                                                let ival = position.Instrument.[date, TimeSeriesType.Last, TimeSeriesRollType.Last]
                                                                (if Double.IsNaN(fx) then 1.0 else fx) * ival  * position.Unit / reference_aum)
                                    |> Seq.toList

        let tsl = positions |> Seq.filter(fun position -> not (strategy.Portfolio.IsReserve(position.Instrument)))
                                   |> Seq.map(fun position ->
                                                                    let instrument = position.Instrument
                                                                    
                                                                    let ttype = if instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.ETF || instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Equity then TimeSeriesType.AdjClose else if instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Strategy then TimeSeriesType.Last else TimeSeriesType.Close
                                                                    let ts_s = instrument.GetTimeSeries(ttype)
                                                                    let ts_s_count = if not (ts_s = null) then ts_s.Count else 0
                                                                    let idx_s = if ts_s_count > 0 then ts_s.GetClosestDateIndex(date, TimeSeries.DateSearchType.Previous) else 0
                                                                    let ts_s = instrument.GetTimeSeries(ttype).GetRange(1 , idx_s)

                            
                                                                    let fx = CurrencyPair.Convert(1.0, date, TimeSeriesType.Close, DataProvider.DefaultProvider, TimeSeriesRollType.Last, strategy.Portfolio.Currency, instrument.Currency)
                                                                    let normalized_ts = ((if Double.IsNaN(fx) then 1.0 else fx) * ts_s.GetRange(Math.Max(0 , ts_s.Count - 1 - days_back) , ts_s.Count - 1))                                                                    
                                                                    position.Unit * normalized_ts.DifferenceReturn().ReplaceNaN(0.0) * (if instrument :? Security then (instrument :?> Security).PointSize else 1.0) / reference_aum
                                              )
                                    |> Seq.toList

        let aggvol = positions |> Seq.filter(fun position -> not (strategy.Portfolio.IsReserve(position.Instrument)))
                                      |> Seq.fold(fun acc position ->
                                                                    let instrument = position.Instrument
                                                                    
                                                                    let ttype = if instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.ETF || instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Equity then TimeSeriesType.AdjClose else if instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Strategy then TimeSeriesType.Last else TimeSeriesType.Close
                                                                    let ts_s = instrument.GetTimeSeries(ttype)
                                                                    let ts_s_count = if not (ts_s = null) then ts_s.Count else 0
                                                                    let idx_s = if ts_s_count > 0 then ts_s.GetClosestDateIndex(date, TimeSeries.DateSearchType.Previous) else 0
                                                                    let ts_s = instrument.GetTimeSeries(ttype).GetRange(1 , idx_s)

                            
                                                                    let fx = CurrencyPair.Convert(1.0, date, TimeSeriesType.Close, DataProvider.DefaultProvider, TimeSeriesRollType.Last, strategy.Portfolio.Currency, instrument.Currency)
                                                                    let normalized_ts = ((if Double.IsNaN(fx) then 1.0 else fx) * ts_s.GetRange(Math.Max(0 , ts_s.Count - 1 - days_back) , ts_s.Count - 1))
                                                                    acc + (position.Unit * normalized_ts.DifferenceReturn().ReplaceNaN(0.0) / reference_aum).Variance
                                              ) 0.0


        let mutable tot = 0.0
        positions 
        //|> Seq.filter(fun position -> not (strategy.Portfolio.IsReserve(position.Instrument)))
        |> Seq.iter(fun position ->
                                    let instrument = position.Instrument
                                                                    
                                    //let ttype = if instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.ETF || instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Equity then TimeSeriesType.AdjClose else if instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Strategy then TimeSeriesType.Last else TimeSeriesType.Close
                                    //let ts_s = instrument.GetTimeSeries(ttype)
                                    //let ts_s_count = if not (ts_s = null) then ts_s.Count else 0
                                    //let idx_s = if ts_s_count > 0 then ts_s.GetClosestDateIndex(date, TimeSeries.DateSearchType.Previous) else 0
                                    //let ts_s = instrument.GetTimeSeries(ttype).GetRange(1 , idx_s)

                            
                                    let fx = CurrencyPair.Convert(1.0, date, TimeSeriesType.Close, DataProvider.DefaultProvider, TimeSeriesRollType.Last, strategy.Portfolio.Currency, instrument.Currency)
                                    //let value = instrument.[date, TimeSeriesType.Last, TimeSeriesRollType.Last] * position.Unit * (if instrument :? Security then (instrument :?> Security).PointSize else 1.0) * fx
                                    let value = instrument.[date, TimeSeriesType.Last, TimeSeriesRollType.Last] * position.Unit * (if instrument :? Security then (instrument :?> Security).PointSize else 1.0) * fx / reference_aum
                                    //Console.WriteLine("Wgt: " +  (instrument.[date, TimeSeriesType.Last, TimeSeriesRollType.Last]).ToString("#0.00") + " " + instrument.Description + " " + date.ToString())
                                    tot <- tot + value
                                    Console.WriteLine("Wgt: " +  value.ToString("#0.00%") + " " + (instrument.[date, TimeSeriesType.Last, TimeSeriesRollType.Last]).ToString() + " " + (position.Unit).ToString() + " " + instrument.Description + " - " + instrument.Currency.Name + " " + position.Timestamp.ToString() + " " + position.StrikeTimestamp.ToString())
                    )
                                       
        Console.WriteLine("------------------ " + (1.0-tot).ToString())
        strategy.Portfolio.PositionOrders(date, false).Values 
        |> Seq.filter(fun position -> not (strategy.Portfolio.IsReserve(position.Instrument)) && position.Instrument.InstrumentType = InstrumentType.Strategy)
        |> Seq.iter(fun position ->
                                    let instrument = position.Instrument
                                                                    
//                                    let ttype = if instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.ETF || instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Equity then TimeSeriesType.AdjClose else if instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Strategy then TimeSeriesType.Last else TimeSeriesType.Close
//                                    let ts_s = instrument.GetTimeSeries(ttype)
//                                    let ts_s_count = if not (ts_s = null) then ts_s.Count else 0
//                                    let idx_s = if ts_s_count > 0 then ts_s.GetClosestDateIndex(date, TimeSeries.DateSearchType.Previous) else 0
//                                    let ts_s = instrument.GetTimeSeries(ttype).GetRange(1 , idx_s)
//
//                            
//                                    let fx = CurrencyPair.Convert(1.0, date, TimeSeriesType.Close, DataProvider.DefaultProvider, TimeSeriesRollType.Last, strategy.Portfolio.Currency, instrument.Currency)
                                    let value = (instrument :?> Strategy).Portfolio.RiskNotional(date) / reference_aum //instrument.[date, TimeSeriesType.Last, TimeSeriesRollType.Last] * position.Unit * (if instrument :? Security then (instrument :?> Security).PointSize else 1.0) * fx / reference_aum

//                                    let adjustStrategyAUM (instru : Instrument) = 
//                                        if instru.InstrumentType = InstrumentType.Strategy && not ((instru :?> Strategy).Portfolio = null) then
//                                            let strategy = instru :?> Strategy
//                                            let strategy_aum = strategy.GetSODAUM(date, TimeSeriesType.Last)                            
//                                            let positions = strategy.Portfolio.PositionOrders(date, true)
//                                            let value =
//                                                positions.Values 
//                                                |> Seq.filter(fun position -> not (strategy.Portfolio.IsReserve(position.Instrument)))
//                                                |> Seq.fold(fun acc position ->
//                                                                            let instrument = position.Instrument                                                                                                                                                                                                                    
//                                                                            let fx = CurrencyPair.Convert(1.0, date, TimeSeriesType.Close, DataProvider.DefaultProvider, TimeSeriesRollType.Last, strategy.Portfolio.Currency, instrument.Currency)
//                                                                            acc + position.Unit * instrument.[date, TimeSeriesType.Last, TimeSeriesRollType.Last] * fx * (if instrument :? Security then (instrument :?> Security).PointSize else 1.0)
//                                                            ) 0.0
//                                            if Math.Abs(value) < 1e-5 || strategy_aum = 0.0 then 
//                                                1.0
//                                            else 
//                                                strategy_aum / value
//                                        else 1.0

                                    Console.WriteLine("Wgt: " +  (value).ToString("#0.00%") + " " + instrument.Description)
                                    //Console.WriteLine("Wgt: " +  (value).ToString("#0.00%") + " " + instrument.Description)
                    )

        let aggregatedTimeSeries = [0 .. tsl.Length - 1] |> List.map (fun i ->  tsl.[i]) |> List.fold (fun acc ts -> if acc = null then ts else acc + ts) null
        let portfolio_vol = if aggregatedTimeSeries = null then 0.0 else sqrt((aggregatedTimeSeries).Variance * 252.0)
        let strategy_vol = sqrt((ts.LogReturn()).Variance * 252.0)

//        let tsl_count = List.length tsl
//        let vars = new Vector(tsl_count, 1.0)
//        [0 .. tsl_count - 1] |> List.iter (fun i -> vars.[i] <- tsl.[i].Variance)
//        let covariance = Covariance tsl
//        let wgts_v = new Vector(tsl_count, 1.0)
//        [0 .. tsl_count - 1] |> List.iter (fun i -> wgts_v.[i] <- wgts.[i])
//               
//        let vv = sqrt(252.0 * (wgts_v.PointwiseMultiply(wgts_v) * vars + wgts_v * covariance * wgts_v))

        Console.WriteLine("Last: " + ts.[length - 1].ToString())
        Console.WriteLine("IRR: " + irr.ToString())
        Console.WriteLine("Strategy Vol: " + strategy_vol.ToString())
        Console.WriteLine("Portfolio Vol: " + portfolio_vol.ToString())
        Console.WriteLine("DD: " + drawdown.ToString())
                
        (ts.[length - 1], irr, vol * 100.0, drawdown, portfolio_vol * 100.0)//, vv * 100.0)