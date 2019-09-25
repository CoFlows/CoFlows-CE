
(*
 * The MIT License (MIT)
 * Copyright (c) 2007-2019, Arturo Rodriguez All rights reserved.
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 *)

namespace AQI.AQILabs.SDK.Strategies

open System
open System.IO

open System.Threading

open AQI.AQILabs.Kernel
open AQI.AQILabs.Kernel.Numerics.Util
open AQI.AQILabs.SDK.Strategies

module Extras =

    let CreateStrategy (accountID : string) (startValue : double) (currency : Currency) (custodian : string) (username : string) (password : string) (portfolio : string) (parameters : string) : Strategy =
        let calendar = Calendar.FindCalendar("WE");
        let startDate = DateTime.Now    
        let date = calendar.GetClosestBusinessDay(startDate, TimeSeries.DateSearchType.Previous)//.AddBusinessDays(-1)

        let parameters = Newtonsoft.Json.Linq.JObject.Parse(parameters);           
        let portfolio = Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(portfolio);
                
        // Startup parameters for the strategy
        let description = "Account: " + accountID
        let name = System.Guid.NewGuid().ToString() + "/" + description
        let VT = Double.Parse(parameters.["TargetVol"].ToString())// 0.05    
        let max_ind_lev = 200.0
        let max_glo_lev = 0.9
        let management_fee = 0.0
        let performance_fee = 0.0
        let residual = parameters.["Residual"].ToString() = "1"
        let simulated = false
        let cloud = true
        let days_back = Int32.Parse(parameters.["DaysBack"].ToString())// 60
        let drawDownThreshold = 0.25
    
        let createStrategy (name : string) (underlyings : List<string>) (master : Portfolio) : Strategy =        
            let strategy = AQI.AQILabs.SDK.Strategies.PortfolioStrategy.Create(name, (if master = null then description else name), startDate, startValue, master, currency, simulated, residual, cloud)
            let addPoint x y =
                strategy.AddMemoryPoint(date.DateTime, x, (int)y)
        
            addPoint VT AQI.AQILabs.SDK.Strategies.MemoryType.TargetVolatility
            addPoint 1.0 AQI.AQILabs.SDK.Strategies.MemoryType.GlobalTargetVolatilityFlag
            addPoint 1.0 AQI.AQILabs.SDK.Strategies.MemoryType.IndividialTargetVolatilityFlag
        
            addPoint (Double.Parse(parameters.["ConcentrationFlag"].ToString())) AQI.AQILabs.SDK.Strategies.MemoryType.ConcentrationFlag
            addPoint (Double.Parse(parameters.["RiskDistributionFlag"].ToString())) AQI.AQILabs.SDK.Strategies.MemoryType.MVOptimizationFlag
            addPoint (Double.Parse(parameters.["HedgeFXFlag"].ToString())) AQI.AQILabs.SDK.Strategies.MemoryType.FXHedgeFlag

            addPoint (Double.Parse(parameters.["ExposureFlag"].ToString())) AQI.AQILabs.SDK.Strategies.MemoryType.ExposureFlag
            addPoint drawDownThreshold AQI.AQILabs.SDK.Strategies.MemoryType.ExposureThreshold

            addPoint (Double.Parse(parameters.["TargetVaR"].ToString())) AQI.AQILabs.SDK.Strategies.MemoryType.TargetVAR
            addPoint (if Double.Parse(parameters.["TargetVaR"].ToString()) = 0.0 then 0.0 else 1.0) AQI.AQILabs.SDK.Strategies.MemoryType.GlobalTargetVARFlag
    
            addPoint max_glo_lev AQI.AQILabs.SDK.Strategies.MemoryType.GlobalMaximumLeverage
            addPoint max_ind_lev AQI.AQILabs.SDK.Strategies.MemoryType.IndividualMaximumLeverage

            addPoint ((double)days_back) AQI.AQILabs.SDK.Strategies.MemoryType.DaysBack

            addPoint (Double.Parse(parameters.["Frequency"].ToString())) AQI.AQILabs.SDK.Strategies.MemoryType.RebalancingFrequency
            addPoint 0.0 AQI.AQILabs.SDK.Strategies.MemoryType.RebalancingThreshold

            let underlyingStrategies = underlyings |> List.map(fun iname -> Instrument.FindInstrument(iname))
            underlyingStrategies |> List.iter(fun i ->  strategy.AddInstrument(i, date.DateTime))
        
            if not(master = null) then
                master.Strategy.AddInstrument(strategy, date.DateTime)
                
            strategy :> Strategy
    
        let master_strategy = createStrategy name [] null

        portfolio 
        |> Seq.iter(fun str -> 
                        let pair = str.Split(' ');
                        let id = Int32.Parse(pair.[0])
                        let ins = Instrument.FindInstrument(id)
                        let insVal = ins.[date.DateTime, TimeSeriesType.Last, TimeSeriesRollType.Last]
                        let fx = CurrencyPair.Convert(1.0, date.DateTime, ins.Currency, master_strategy.Currency)

                        let amount = if pair.[1].Contains("%") then
                                        let pct = Double.Parse(pair.[1].Replace("%","")) / 100.0

                                        if pct = 0.0 then
                                            0.0
                                        else
                                            Math.Round(startValue * pct * fx) / insVal
                                     else
                                        Double.Parse(pair.[1])

                        master_strategy.AddInstrument(ins, date.DateTime)
                        master_strategy.Portfolio.CreatePosition(ins, date.DateTime, amount, insVal) |> ignore
                    )

        master_strategy.ScheduleCommand <- "E:0 5 15 ? * MON-FRI;M:0 0/10 15-16 ? * MON-FRI"
        master_strategy.Portfolio.AccountID <- accountID
        master_strategy.Portfolio.CustodianID <- custodian
        master_strategy.Portfolio.Username <- username
        master_strategy.Portfolio.Password <- password
        master_strategy
    
    let CreatePHMStrategy (accountID : string) (startValue : double) (currency : Currency) (custodian : string) (username : string) (password : string) (portfolio : string) (parameters : string) : Strategy =
        let calendar = Calendar.FindCalendar("WE");
        let startDate = DateTime.Now    
        let date = calendar.GetClosestBusinessDay(startDate, TimeSeries.DateSearchType.Previous)//.AddBusinessDays(-1)

        let parameters = Newtonsoft.Json.Linq.JObject.Parse(parameters);           
        let portfolio = Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(portfolio);

        let description = "Account: " + accountID
        let name = System.Guid.NewGuid().ToString() + "/" + description
        let VT = Double.Parse(parameters.["TargetVol"].ToString())// 0.05    
        let max_ind_lev = 200.0
        let max_glo_lev = 0.9
        let management_fee = 0.0
        let performance_fee = 0.0
        let residual = parameters.["Residual"].ToString() = "1"
        let simulated = false
        let cloud = true
        let days_back = Int32.Parse(parameters.["DaysBack"].ToString())// 60
        let drawDownThreshold = 0.25
    
        let createStrategy (name : string) (underlyings : List<string>) (master : Portfolio) : Strategy =        
            let strategy = AQI.AQILabs.SDK.Strategies.PortfolioStrategy.Create(name, (if master = null then description else name), startDate, startValue, master, currency, simulated, residual, cloud)
            let addPoint x y =
                strategy.AddMemoryPoint(date.DateTime, x, (int)y)
        
            addPoint VT AQI.AQILabs.SDK.Strategies.MemoryType.TargetVolatility
            addPoint 1.0 AQI.AQILabs.SDK.Strategies.MemoryType.GlobalTargetVolatilityFlag
            addPoint 1.0 AQI.AQILabs.SDK.Strategies.MemoryType.IndividialTargetVolatilityFlag
        
            addPoint (Double.Parse(parameters.["ConcentrationFlag"].ToString())) AQI.AQILabs.SDK.Strategies.MemoryType.ConcentrationFlag
            addPoint (Double.Parse(parameters.["RiskDistributionFlag"].ToString())) AQI.AQILabs.SDK.Strategies.MemoryType.MVOptimizationFlag
            addPoint (Double.Parse(parameters.["HedgeFXFlag"].ToString())) AQI.AQILabs.SDK.Strategies.MemoryType.FXHedgeFlag

            addPoint (Double.Parse(parameters.["ExposureFlag"].ToString())) AQI.AQILabs.SDK.Strategies.MemoryType.ExposureFlag
            addPoint drawDownThreshold AQI.AQILabs.SDK.Strategies.MemoryType.ExposureThreshold

            addPoint (Double.Parse(parameters.["TargetVaR"].ToString())) AQI.AQILabs.SDK.Strategies.MemoryType.TargetVAR
            addPoint (if Double.Parse(parameters.["TargetVaR"].ToString()) = 0.0 then 0.0 else 1.0) AQI.AQILabs.SDK.Strategies.MemoryType.GlobalTargetVARFlag
    
            addPoint max_glo_lev AQI.AQILabs.SDK.Strategies.MemoryType.GlobalMaximumLeverage
            addPoint max_ind_lev AQI.AQILabs.SDK.Strategies.MemoryType.IndividualMaximumLeverage

            addPoint ((double)days_back) AQI.AQILabs.SDK.Strategies.MemoryType.DaysBack

            addPoint (Double.Parse(parameters.["Frequency"].ToString())) AQI.AQILabs.SDK.Strategies.MemoryType.RebalancingFrequency
            addPoint 0.0 AQI.AQILabs.SDK.Strategies.MemoryType.RebalancingThreshold

            let underlyingStrategies = underlyings |> List.map(fun iname -> Instrument.FindInstrument(iname))
            underlyingStrategies |> List.iter(fun i ->  strategy.AddInstrument(i, date.DateTime))
        
            if not(master = null) then
                master.Strategy.AddInstrument(strategy, date.DateTime)
                
            strategy :> Strategy
    
        let master_strategy = createStrategy name [] null
        
        let equity_strategy = createStrategy (name + "/Equities") [] master_strategy.Portfolio
        let credit_strategy = createStrategy (name + "/Credit") [] master_strategy.Portfolio
        let bond_strategy = createStrategy (name + "/Bonds") [] master_strategy.Portfolio
        let linkedBonds_strategy = createStrategy (name + "/Linked-Bonds") [] master_strategy.Portfolio
        let commodity_strategy = createStrategy (name + "/Commodities") [] master_strategy.Portfolio
        let fx_strategy = createStrategy (name + "/FX") [] master_strategy.Portfolio

        portfolio 
        |> Seq.iter(fun str -> 
                        let pair = str.Split(' ');
                        let id = Int32.Parse(pair.[0])
                        let ins = Instrument.FindInstrument(id)
                        let insVal = ins.[date.DateTime, TimeSeriesType.Last, TimeSeriesRollType.Last]
                        let fx = CurrencyPair.Convert(1.0, date.DateTime, ins.Currency, master_strategy.Currency)

                        let amount = if pair.[1].Contains("%") then
                                        let pct = Double.Parse(pair.[1].Replace("%","")) / 100.0

                                        if pct = 0.0 then
                                            0.0
                                        else
                                            Math.Round(startValue * pct * fx) / insVal
                                     else
                                        Double.Parse(pair.[1])
                    
                        if ins.Description.Trim().Contains("Equity") then
                            equity_strategy.AddInstrument(ins, date.DateTime)
                            equity_strategy.Portfolio.CreatePosition(ins, date.DateTime, amount, insVal) |> ignore

                        else if ins.Description.Trim().Contains("Credit") then
                            credit_strategy.AddInstrument(ins, date.DateTime)
                            credit_strategy.Portfolio.CreatePosition(ins, date.DateTime, amount, insVal) |> ignore

                        else if ins.Description.Trim().Contains("LBond") then
                            bond_strategy.AddInstrument(ins, date.DateTime)
                            bond_strategy.Portfolio.CreatePosition(ins, date.DateTime, amount, insVal) |> ignore
                    
                        else if ins.Description.Trim().Contains("LinkBond") then
                            linkedBonds_strategy.AddInstrument(ins, date.DateTime)
                            linkedBonds_strategy.Portfolio.CreatePosition(ins, date.DateTime, amount, insVal) |> ignore

                        else if ins.Description.Trim().Contains("Commodity") then
                            commodity_strategy.AddInstrument(ins, date.DateTime)
                            commodity_strategy.Portfolio.CreatePosition(ins, date.DateTime, amount, insVal) |> ignore

                        else if ins.Description.Trim().Contains("Currency") || ins.Description.Trim().Contains("FX") then
                            fx_strategy.AddInstrument(ins, date.DateTime)
                            fx_strategy.Portfolio.CreatePosition(ins, date.DateTime, amount, insVal) |> ignore

                    )

        if not master_strategy.SimulationObject then
            let th = new Thread(fun () ->
                master_strategy.Portfolio.CanSave <- true
                master_strategy.Tree.Save()
                master_strategy.Tree.SaveNewPositions())
            th.Start()


        master_strategy.ScheduleCommand <- "E:0 5 15 ? * MON-FRI;M:0 0/10 15-16 ? * MON-FRI"
        master_strategy.Portfolio.AccountID <- accountID
        master_strategy.Portfolio.CustodianID <- custodian
        master_strategy.Portfolio.Username <- username
        master_strategy.Portfolio.Password <- password
        master_strategy

    let CreateSubStrategy (parent : Strategy) (startValue : double) (stype : string) : Strategy =
        let calendar = Calendar.FindCalendar("WE");
        let startDate = DateTime.Now    
        let date = calendar.GetClosestBusinessDay(startDate, TimeSeries.DateSearchType.Previous)//.AddBusinessDays(-1)

        let parameters = Newtonsoft.Json.Linq.JObject.Parse(stype);               
        let portfolio = Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(parameters.["portfolio"].ToString());
                    
        // Startup parameters for the strategy
        let description = parameters.["Name"].ToString()
        let name = parent.Name + "/" + description
    
        let VT = parent.Portfolio.MasterPortfolio.Strategy.[startDate, (int)AQI.AQILabs.SDK.Strategies.MemoryType.TargetVolatility, TimeSeriesRollType.Last]
        let TV = parent.Portfolio.MasterPortfolio.Strategy.[startDate, (int)AQI.AQILabs.SDK.Strategies.MemoryType.TargetVAR, TimeSeriesRollType.Last]
        let frequency = parent.Portfolio.MasterPortfolio.Strategy.[startDate, (int)AQI.AQILabs.SDK.Strategies.MemoryType.RebalancingFrequency, TimeSeriesRollType.Last]
        let fxFlag = parent.Portfolio.MasterPortfolio.Strategy.[startDate, (int)AQI.AQILabs.SDK.Strategies.MemoryType.FXHedgeFlag, TimeSeriesRollType.Last]
        let mvFlag = parent.Portfolio.MasterPortfolio.Strategy.[startDate, (int)AQI.AQILabs.SDK.Strategies.MemoryType.MVOptimizationFlag, TimeSeriesRollType.Last]
        let concentrationFlag = parent.Portfolio.MasterPortfolio.Strategy.[startDate, (int)AQI.AQILabs.SDK.Strategies.MemoryType.ConcentrationFlag, TimeSeriesRollType.Last]    
        let days_back = parent.Portfolio.MasterPortfolio.Strategy.[startDate, (int)AQI.AQILabs.SDK.Strategies.MemoryType.DaysBack, TimeSeriesRollType.Last]
        let exposureFlag = parent.Portfolio.MasterPortfolio.Strategy.[startDate, (int)AQI.AQILabs.SDK.Strategies.MemoryType.ExposureFlag, TimeSeriesRollType.Last]
        let gVTFlag = parent.Portfolio.MasterPortfolio.Strategy.[startDate, (int)AQI.AQILabs.SDK.Strategies.MemoryType.GlobalTargetVolatilityFlag, TimeSeriesRollType.Last]
        let iVTFlag = parent.Portfolio.MasterPortfolio.Strategy.[startDate, (int)AQI.AQILabs.SDK.Strategies.MemoryType.IndividialTargetVolatilityFlag, TimeSeriesRollType.Last]

        let max_ind_lev = 200.0
        let max_glo_lev = 0.9
        let management_fee = 0.0
        let performance_fee = 0.0
        let residual = false
        let simulated = false
        let cloud = true    
        let drawDownThreshold = 0.25
    
        let createStrategy (name : string) (underlyings : List<string>) (master : Portfolio) : Strategy =        
            let strategy = AQI.AQILabs.SDK.Strategies.PortfolioStrategy.Create(name, (if master = null then description else name), startDate, startValue, master, parent.Currency, simulated, residual, cloud)
            let addPoint x y =
                strategy.AddMemoryPoint(date.DateTime, x, (int)y)
        
            addPoint VT AQI.AQILabs.SDK.Strategies.MemoryType.TargetVolatility
            addPoint gVTFlag AQI.AQILabs.SDK.Strategies.MemoryType.GlobalTargetVolatilityFlag
            addPoint iVTFlag AQI.AQILabs.SDK.Strategies.MemoryType.IndividialTargetVolatilityFlag
        
            addPoint concentrationFlag AQI.AQILabs.SDK.Strategies.MemoryType.ConcentrationFlag
            addPoint mvFlag AQI.AQILabs.SDK.Strategies.MemoryType.MVOptimizationFlag
            addPoint fxFlag AQI.AQILabs.SDK.Strategies.MemoryType.FXHedgeFlag

            addPoint exposureFlag AQI.AQILabs.SDK.Strategies.MemoryType.ExposureFlag
            addPoint drawDownThreshold AQI.AQILabs.SDK.Strategies.MemoryType.ExposureThreshold

            addPoint TV AQI.AQILabs.SDK.Strategies.MemoryType.TargetVAR
            addPoint (if TV = 0.0 then 0.0 else 1.0) AQI.AQILabs.SDK.Strategies.MemoryType.GlobalTargetVARFlag
    
            addPoint max_glo_lev AQI.AQILabs.SDK.Strategies.MemoryType.GlobalMaximumLeverage
            addPoint max_ind_lev AQI.AQILabs.SDK.Strategies.MemoryType.IndividualMaximumLeverage

            addPoint days_back AQI.AQILabs.SDK.Strategies.MemoryType.DaysBack

            addPoint frequency AQI.AQILabs.SDK.Strategies.MemoryType.RebalancingFrequency
            addPoint 0.0 AQI.AQILabs.SDK.Strategies.MemoryType.RebalancingThreshold

            let underlyingStrategies = underlyings |> List.map(fun iname -> Instrument.FindInstrument(iname))
            underlyingStrategies |> List.iter(fun i ->  strategy.AddInstrument(i, date.DateTime))
        
            if not(master = null) then
                master.Strategy.AddInstrument(strategy, date.DateTime)
                
            strategy :> Strategy
    
        let new_strategy = createStrategy name [] parent.Portfolio
        
        portfolio 
        |> Seq.iter(fun str -> 
                        let pair = str.Split(' ');
                        let id = Int32.Parse(pair.[0])
                        let ins = Instrument.FindInstrument(id)
                        let insVal = ins.[date.DateTime, TimeSeriesType.Last, TimeSeriesRollType.Last]
                        let fx = CurrencyPair.Convert(1.0, date.DateTime, ins.Currency, new_strategy.Currency)

                        let amount = if pair.[1].Contains("%") then
                                        let pct = Double.Parse(pair.[1].Replace("%","")) / 100.0

                                        if pct = 0.0 then
                                            0.0
                                        else
                                            Math.Round(startValue * pct * fx) / insVal
                                     else
                                        Double.Parse(pair.[1])
                    
                    
                    
                        new_strategy.AddInstrument(ins, date.DateTime)
                        new_strategy.Portfolio.CreatePosition(ins, date.DateTime, amount, insVal) |> ignore
                    )

        new_strategy

    let SubmitAccountCreation (accountName : string) (ccy : Currency) (custodian : string) (username : string) (password : string) (key : string) (portfolio : string) (parameters : string) : Strategy =
        Console.WriteLine("Submit Account Creation: " + accountName + " " + ccy.Name + " " + custodian)
    
        let s =
            if (custodian = "Simulated") then                
                let s = CreateStrategy accountName 1000000.0 ccy custodian username password portfolio parameters
                s 

            else if (custodian = "PHM") then                
                let s = CreatePHMStrategy accountName 1000000.0 ccy custodian username password portfolio parameters
                s

            else
                null        
        s
      
    let SubmitSubStrategy (parent : Strategy) (stype : string) : Strategy =
       let startDate = System.DateTime.Now    
       let startValue = parent.GetSODAUM(startDate, TimeSeriesType.Last)

       CreateSubStrategy parent startValue stype

    let InitializeSystem() =
        Market.CreateAccount <- Market.CreateAccountStrategyType(CreateStrategy)
        AQI.AQILabs.Kernel.StrategyRTDEngine.SubmitAccountCreation <- AQI.AQILabs.Kernel.StrategyRTDEngine.SubmitAccountCreationType(SubmitAccountCreation)
        AQI.AQILabs.Kernel.StrategyRTDEngine.SubmitSubStrategy <- AQI.AQILabs.Kernel.StrategyRTDEngine.SubmitSubStrategyType(SubmitSubStrategy)
    

        //["QuantApp"; "ClearMacro"]
        //|> List.iter(fun topic -> RTDEngine.Subscribe(topic))


        AQI.AQILabs.Kernel.Market.Instructions() |> ignore
        AQI.AQILabs.Kernel.Market.ClientsDestinations() |> ignore    

        Market.Initialize()

        let ins = System.Collections.Concurrent.ConcurrentDictionary<int, Instrument>()
        Instrument.FindInstrumentCallback <- (Instrument.FindInstrumentCallbackType(fun i ->                      
                                            if ((i |> isNull |> not) && not(ins.ContainsKey(i.ID))) then   
                                            
                                                if (i.Cloud) then
                                                    QuantApp.Kernel.RTDEngine.Send(QuantApp.Kernel.RTDMessage(Type = QuantApp.Kernel.RTDMessage.MessageType.Subscribe, Content = ("$" + i.Name)))

                                                if (i.InstrumentType = InstrumentType.Portfolio) then
                                                //if false then
                                                    try
                                                        let portfolio = i :?> Portfolio
                                                        if portfolio.ParentPortfolio |> isNull then
                                                            ins.TryAdd(i.ID, i) |> ignore
                                                        
                                                    with
                                                    | ex -> Console.WriteLine("Error in subscribing portfolio: "  + ex.ToString())

                                                elif (i.InstrumentType = InstrumentType.Strategy && not((i :?> Strategy).Portfolio = null)) then                                                
                                                    try
                                                        let strategy = i :?> Strategy
                                                        ins.TryAdd(i.ID, i) |> ignore

                                                        let navCalc = System.Threading.Thread(fun () -> 
                                                            let cal = Calendar.FindCalendar("All")
                                                            while true do
                                                                try

                                                                    System.Threading.Thread.Sleep(250)
                                                                    let t = cal.GetClosestBusinessDay(DateTime.Now, TimeSeries.DateSearchType.Previous)
                                                                    
                                                                    strategy.QuickNAVShare(t) |> ignore
                                                                with _ -> () 
                                                            )
                                                        navCalc.Start()
                                                        
                                                    with
                                                    | ex -> Console.WriteLine("Error in subscribing strategy: "  + ex.ToString())

                                                else
                                                    //AQI.AQILabs.SecureWebClient.Connectors.IB.Utils.Adapter.subscribe(i)
                                                    if i.InstrumentType = InstrumentType.ETF || i.InstrumentType = InstrumentType.Equity || i.InstrumentType = InstrumentType.Fund then
                                                        (i :?> Security).CorporateActions() |> ignore
                                                    ins.TryAdd(i.ID, i) |> ignore
                                                    
                                                    if i :? Security && (i :? Future && (i:?> Future).LastTradeDate >= DateTime.Now || not(i :? Future)) then
                                                        let priceSim = System.Threading.Thread(fun () -> 
                                                            Console.WriteLine("Price Simulation: " + i.Name)
                                                            while true do
                                                                System.Threading.Thread.Sleep(1000)
                                                                let t = DateTime.Now
                                                            
                                                                let last = i.[t, TimeSeriesType.Last, TimeSeriesRollType.Last]
                                                                let last_n = last * (1.0 + ((System.Random()).NextDouble() - 0.5) * 0.0001)
                                                            
                                                                //let bid = i.[t, TimeSeriesType.Bid, TimeSeriesRollType.Last]
                                                                //let bid_n = bid * (1.0 + ((new System.Random()).NextDouble() - 0.5) * 0.0001)
                                                            
                                                                //let ask = i.[t, TimeSeriesType.Ask, TimeSeriesRollType.Last]
                                                                //let ask_n = ask * (1.0 + ((new System.Random()).NextDouble() - 0.5) * 0.0001)
                                                            
                                                                i.AddTimeSeriesPoint(t, last_n, TimeSeriesType.Last, DataProvider.DefaultProvider, true, true)
                                                                //i.AddTimeSeriesPoint(t, bid_n, TimeSeriesType.Ask, DataProvider.DefaultProvider, true, true)
                                                                //i.AddTimeSeriesPoint(t, ask_n, TimeSeriesType.Bid, DataProvider.DefaultProvider, true, true)
                                                            )
                                                        priceSim.Start()
                                                ))

    type TimeIncrement = delegate of BusinessDay -> BusinessDay
    let QuantAppSimulate(strategy : Strategy, debug : bool, startDate : DateTime, endDate : DateTime, timeIncrement : TimeIncrement, update : System.Func<float, float>) =
            strategy.Tree.Initialize()
            let simStartDate = strategy.Calendar.GetClosestBusinessDay(startDate, TimeSeries.DateSearchType.Next)
            let simEndDate = endDate
        
            strategy.Simulating <- true
            let start_time = DateTime.Now
            let mutable t = timeIncrement.Invoke simStartDate
            while t.DateTime <= simEndDate do
                let t1 = DateTime.Now
                strategy.Tree.Process(t.DateTime)
                if debug then
                    if Calendar.Close(t.DateTime) = t.DateTime && not(t.DateTime.Year = t.AddBusinessDays(1).DateTime.Year) || (not(Calendar.Close(t.DateTime) = t.DateTime)) then
                        let progress = Math.Min(1.0, (t.DateTime - startDate).TotalDays / (simEndDate - startDate).TotalDays)
                        if not(update = null) then
                            update.Invoke(progress) |> ignore

                    Console.WriteLine(t.DateTime.ToString() + " --> " + strategy.[t.DateTime.AddMilliseconds(3.0)].ToString("#,##0.##"))
            
                t <- timeIncrement.Invoke t

            strategy.Simulating <- false
            let end_time = DateTime.Now
            Console.WriteLine("Simulation Time: " + (end_time - start_time).ToString())
            Console.WriteLine(t.DateTime.ToString() + " --> " + strategy.[t.DateTime].ToString("#,##0.##"))
            t

    let SimulateClone(base_strategy : Strategy, startDate : DateTime, memoryID : int, value : float, update : System.Func<float, float>) : Strategy =

        let calendar = Calendar.FindCalendar("WE")
        let endDate = DateTime.Now

        let startDate =
            if startDate = DateTime.MinValue then            
                base_strategy.Instruments(endDate, true).Values
                |> Seq.map(fun instrument ->
                        let ts = instrument.GetTimeSeries(TimeSeriesType.Last)
                        ts.DateTimes.[0]
                    )
                |> Seq.min
            else
                startDate
        

        let date = calendar.GetClosestBusinessDay(startDate, TimeSeries.DateSearchType.Previous)
        base_strategy.Tree.Initialize()
        let master_strategy = base_strategy.Tree.Clone(startDate, DateTime.Now, true).Strategy

        if not(memoryID = Int32.MinValue) then
            master_strategy.AddMemoryPoint(startDate, value, memoryID)

        let mkey = "--Simulations-" + base_strategy.ID.ToString()
     
        // Initialize Simulation    
        let t1 = DateTime.Now
        SystemLog.Write("Strategy: " + date.DateTime.ToShortDateString() + " " + master_strategy.Portfolio.[date.DateTime].ToString() + " " + master_strategy.[date.DateTime].ToString());

        QuantAppSimulate(master_strategy, false, startDate, endDate, TimeIncrement(fun (t : BusinessDay) -> t.AddBusinessDays(1)), update) |> ignore

        let dt = (DateTime.Now - t1).ToString()
        Console.WriteLine(dt)

        if not master_strategy.SimulationObject then
            master_strategy.Tree.Save()
            master_strategy.Tree.SaveNewPositions()

        master_strategy

    let ClearOrders (strat : Strategy) (date : DateTime) =
        let orders = strat.Portfolio.OpenOrders(date, true)

        if (not(orders = null) && not(orders.Count = 0)) then
            orders.Keys
            |> Seq.iter(fun i -> 
                let os = orders.[i];
                os.Keys
                |> Seq.iter(fun orderID -> 
                    let order = os.[orderID];
                    
                    Console.WriteLine("Cancelling existing order: " + order.ToString())
                    
                    let norder = order.Portfolio.FindOrder(order.ID, false)
                    if not(norder = null) then
                        norder.Portfolio.UpdateOrderTree(norder, OrderStatus.NotExecuted, Double.NaN, Double.NaN, date)                                  
                    order.Portfolio.UpdateOrderTree(order, OrderStatus.NotExecuted, Double.NaN, Double.NaN, date)                
                    )                
                ) 

        let positions = strat.Portfolio.Positions(date)
        positions
        |> Seq.iter(fun pos ->
            if pos.Instrument.InstrumentType = InstrumentType.Strategy then
                let sub_strat = pos.Instrument :?> Strategy
                let orders = sub_strat.Portfolio.OpenOrders(date, false)
                if not(orders = null) then
                    orders.Values
                    |> Seq.iter(fun lorders ->
                            lorders.Values
                            |> Seq.iter(fun order ->
                                order.Portfolio.UpdateOrderTree(order, OrderStatus.NotExecuted, Double.NaN, Double.NaN, date)                
                                Console.WriteLine(order)
                                )                    
                        )
                Console.WriteLine(orders)
            )

    let ClosePositions (strat : Strategy) (date : DateTime) =        
        strat.Portfolio.Positions(date, false)
        |> Seq.filter(fun pos -> not(strat.Portfolio.IsReserve(pos.Instrument)))
        |> Seq.iter(fun pos -> pos.UpdateTargetMarketOrder(date, 0.0, UpdateType.OverrideNotional) |> ignore)
        strat.Portfolio.SubmitOrders(date) |> ignore

    