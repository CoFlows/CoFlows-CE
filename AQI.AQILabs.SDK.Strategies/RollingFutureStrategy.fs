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

/// <summary>
/// Class representing a strategy that rolls a future for a specific underlying according to a given roll schedule.
/// </summary>
type RollingFutureStrategy = 
    inherit Strategy

    val mutable _underlyingInstrument : Instrument
        
    /// <summary>
    /// Constructor
    /// </summary> 
    new(instrument : Instrument) = { inherit Strategy(instrument); _underlyingInstrument = null; }         

    /// <summary>
    /// Constructor
    /// </summary> 
    new(id : int) = { inherit Strategy(id); _underlyingInstrument = null; }
      
    /// <summary>
    /// Function: returns a list of names of used memory types.
    /// </summary>   
    override this.MemoryTypeNames() : string[] = System.Enum.GetNames(typeof<MemoryType>)

    /// <summary>
    /// Function: returns a list of ids of used memory types.
    /// </summary>  
    override this.MemoryTypeInt(name : string) = System.Enum.Parse(typeof<MemoryType> , name) :?> int

    /// <summary>
    /// Property: returns the underlying instrument of the rolled futures.
    /// </summary>  
    member this.UnderlyingInstrument 
        with get() = this._underlyingInstrument
        and private set value = this._underlyingInstrument <- value


    /// <summary>
    /// Function: Set active months
    /// </summary> 
    member this.SetActiveMonths(date: DateTime, Jan : bool, Feb : bool, Mar: bool, Apr: bool, May: bool, Jun: bool, Jul: bool, Aug: bool, Sep: bool, Oct: bool, Nov: bool, Dec: bool) =
        this.AddMemoryPoint(date, (if Jan then 1.0 else -1.0), (int)MemoryType.ActiveJan)
        this.AddMemoryPoint(date, (if Feb then 1.0 else -1.0), (int)MemoryType.ActiveFeb)
        this.AddMemoryPoint(date, (if Mar then 1.0 else -1.0), (int)MemoryType.ActiveMar)
        this.AddMemoryPoint(date, (if Apr then 1.0 else -1.0), (int)MemoryType.ActiveApr)
        this.AddMemoryPoint(date, (if May then 1.0 else -1.0), (int)MemoryType.ActiveMay)
        this.AddMemoryPoint(date, (if Jun then 1.0 else -1.0), (int)MemoryType.ActiveJun)
        this.AddMemoryPoint(date, (if Jul then 1.0 else -1.0), (int)MemoryType.ActiveJul)
        this.AddMemoryPoint(date, (if Aug then 1.0 else -1.0), (int)MemoryType.ActiveAug)
        this.AddMemoryPoint(date, (if Sep then 1.0 else -1.0), (int)MemoryType.ActiveSep)
        this.AddMemoryPoint(date, (if Oct then 1.0 else -1.0), (int)MemoryType.ActiveOct)
        this.AddMemoryPoint(date, (if Nov then 1.0 else -1.0), (int)MemoryType.ActiveNov)
        this.AddMemoryPoint(date, (if Dec then 1.0 else -1.0), (int)MemoryType.ActiveDec)

    /// <summary>
    /// Function: Get active months
    /// </summary> 
    member this.GetActiveMonths(date: DateTime) =
        let jan = this.[date, (int)MemoryType.ActiveJan, TimeSeriesRollType.Last]
        let feb = this.[date, (int)MemoryType.ActiveFeb, TimeSeriesRollType.Last]
        let mar = this.[date, (int)MemoryType.ActiveMar, TimeSeriesRollType.Last]
        let apr = this.[date, (int)MemoryType.ActiveApr, TimeSeriesRollType.Last]
        let may = this.[date, (int)MemoryType.ActiveMay, TimeSeriesRollType.Last]
        let jun = this.[date, (int)MemoryType.ActiveJun, TimeSeriesRollType.Last]
        let jul = this.[date, (int)MemoryType.ActiveJul, TimeSeriesRollType.Last]
        let aug = this.[date, (int)MemoryType.ActiveAug, TimeSeriesRollType.Last]
        let sep = this.[date, (int)MemoryType.ActiveSep, TimeSeriesRollType.Last]
        let oct = this.[date, (int)MemoryType.ActiveOct, TimeSeriesRollType.Last]
        let nov = this.[date, (int)MemoryType.ActiveNov, TimeSeriesRollType.Last]
        let dec = this.[date, (int)MemoryType.ActiveDec, TimeSeriesRollType.Last]

        let jan = if Double.IsNaN(jan) then 1.0 else jan
        let feb = if Double.IsNaN(feb) then 1.0 else feb
        let mar = if Double.IsNaN(mar) then 1.0 else mar
        let apr = if Double.IsNaN(apr) then 1.0 else apr
        let may = if Double.IsNaN(may) then 1.0 else may
        let jun = if Double.IsNaN(jun) then 1.0 else jun
        let jul = if Double.IsNaN(jul) then 1.0 else jul
        let aug = if Double.IsNaN(aug) then 1.0 else aug
        let sep = if Double.IsNaN(sep) then 1.0 else sep
        let oct = if Double.IsNaN(oct) then 1.0 else oct
        let nov = if Double.IsNaN(nov) then 1.0 else nov
        let dec = if Double.IsNaN(dec) then 1.0 else dec

        [jan; feb; mar; apr; may; jun; jul; aug; sep; oct; nov; dec]
        

    /// <summary>
    /// Function: Initialize the strategy during runtime.
    /// </summary>      
    override this.Initialize() =        
        match this.Initialized with
        | true -> ()
        | _ ->                                         
            this.UnderlyingInstrument <- Instrument.FindInstrument((int)this.[DateTime.Now, (int)MemoryType.UnderlyingID, TimeSeriesRollType.Last])
            base.Initialize()

    /// <summary>
    /// Function: Returns the id of the contract traded at a given date.
    /// </summary> 
    /// <param name="date">reference date.
    /// </param>
    member this.Contract(date : DateTime) =
        (int) this.[date, (int)MemoryType.Contract, TimeSeriesRollType.Last]


    member this.Future(date: BusinessDay) =
        let future_current0 = 
            let positions = this.Portfolio.PositionOrders(date.DateTime)
            
                                                    
            let instrument = this.UnderlyingInstrument;
            
            // list of positions in the portfolio
            let positions_sorted = if not (positions = null) then
                                        positions.Values
                                        |> Seq.toList
                                        |> List.filter (fun pos ->
                                            pos.Instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Future)
                                        |> List.filter (fun pos ->
                                            let fut = pos.Instrument :?> Future
                                            fut.Underlying = instrument)
                                        |> List.sortBy (fun pos ->
                                            let fut = pos.Instrument :?> Future
                                            fut.LastTradeDate)
                                        |> List.toArray
                                    else
                                        null

            let position0 = if not (positions = null) && positions_sorted.Length = 1 then positions_sorted.[0] else null
            // future with closest expiry
            let future_current0 = if not (positions = null) && positions_sorted.Length = 1 then position0.Instrument :?> Future else null            
        
            if future_current0 = null then
                let contract_num = this.Contract(date.DateTime)
                

                let nextFuture = 
                    let activeMonths = this.GetActiveMonths(date.DateTime)

                    let rollDate = 
                        let rollDayDouble = this.[date.DateTime, (int)MemoryType.RollDay, TimeSeriesRollType.Last]
                        if Double.IsNaN(rollDayDouble) then 5 else (int)rollDayDouble            
        
                    let nextFuture = ref (Future.ActiveFutures(this.UnderlyingInstrument, date.DateTime).[contract_num])
                    let nextRollDate = (if !nextFuture = null then null else this.Calendar.GetClosestBusinessDay((if (!nextFuture).LastTradeDate < (!nextFuture).FirstNoticeDate then (!nextFuture).LastTradeDate else (!nextFuture).FirstNoticeDate), TimeSeries.DateSearchType.Next).AddBusinessDays(-(rollDate)))


                    if not (nextRollDate = null) && (nextRollDate.DateTime <= date.DateTime.Date) then
                        nextFuture := (!nextFuture).NextFuture

                    while not(activeMonths.[(!nextFuture).ContractMonth - 1] = 1.0) do
                        nextFuture := (!nextFuture).NextFuture

                    !nextFuture
                nextFuture
            else
                future_current0

        future_current0


    member this.Roll(date: BusinessDay) =
        let calendar = Calendar.FindCalendar("WE")

        //let reference_aum = this.GetAUM(date.DateTime.Date, TimeSeriesType.Last)
                                    
        let sign_double = this.[date.DateTime, Strategy._direction_id_do_not_use, TimeSeriesRollType.Last]
        let sign = if sign_double |> Double.IsNaN then 0 else sign_double |> int
        let contract = (int)this.[date.DateTime, (int)MemoryType.Contract, TimeSeriesRollType.Last]
        let rollDayDouble = this.[date.DateTime, (int)MemoryType.RollDay, TimeSeriesRollType.Last]
        let FXHedgeFlag = (int)this.Portfolio.MasterPortfolio.Strategy.[date.DateTime, (int)MemoryType.FXHedgeFlag, TimeSeriesRollType.Last]

        if this._underlyingInstrument |> isNull then
            this._underlyingInstrument <- Instrument.FindInstrument((int)this.[date.Close, (int)MemoryType.UnderlyingID, TimeSeriesRollType.Last])

        let positions = this.Portfolio.Positions(date.DateTime)
        let rollDate = if Double.IsNaN(rollDayDouble) then 5 else (int)rollDayDouble            
        let instrument = this._underlyingInstrument
            
        // list of positions in the portfolio
        let positions_sorted = if not (positions = null) then
                                    positions
                                    |> Seq.toList
                                    |> List.filter (fun pos ->
                                        pos.Instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Future)
                                    |> List.filter (fun pos ->
                                        let fut = pos.Instrument :?> Future
                                        fut.Underlying = instrument)
                                    |> List.sortBy (fun pos ->
                                        let fut = pos.Instrument :?> Future
                                        fut.LastTradeDate)
                                    |> List.toArray
                                else
                                    null

        // position in future with closest expiry
        let position0 = if not (positions = null) && positions_sorted.Length = 1 then positions_sorted.[0] else null

        if not (position0 = null) then   
            let old_unit = position0.Unit
            position0.UpdateTargetMarketOrder(date.DateTime, 0.0, UpdateType.OverrideUnits) |> ignore
            this.RemoveInstruments(date.DateTime)
                    
            // get next future                    
            let nextFuture = ref (Future.CurrentFuture(instrument, date.DateTime.Date))
            let nextRollDate = (if !nextFuture |> isNull then null else calendar.GetClosestBusinessDay((if (!nextFuture).LastTradeDate < (!nextFuture).FirstNoticeDate then (!nextFuture).LastTradeDate else (!nextFuture).FirstNoticeDate), TimeSeries.DateSearchType.Next).AddBusinessDays(-(rollDate)))

            let activeMonths = this.GetActiveMonths(date.DateTime)

            if (nextRollDate |> isNull |> not) && (nextRollDate.DateTime <= date.DateTime.Date) then
                nextFuture := (!nextFuture).NextFuture

            while activeMonths.[(!nextFuture).ContractMonth - 1] <> 1.0 do
                nextFuture := (!nextFuture).NextFuture

            if !nextFuture |> isNull |> not then                    
                // roll to the specified contract
                [1 .. contract - 1] |> List.iter (fun i -> nextFuture := (!nextFuture).NextFuture)
                        
                this.RemoveInstruments(date.DateTime)

                if FXHedgeFlag = 1 then
                    this.Portfolio.HedgeFX(date.DateTime, 0.0)
                
                let unit = old_unit
                this.AddInstrument(!nextFuture, date.DateTime)                        
                
                this.Portfolio.CreateTargetMarketOrder(!nextFuture, date.DateTime, Math.Abs(unit) * (double) sign) |> ignore
                

    /// <summary>
    /// Function: Execute the rolling future logic.
    /// </summary> 
    /// <param name="ctx">Context containing relevant environment information for the logic execution
    /// </param>
    override this.ExecuteLogic(ctx : ExecutionContext, force : bool) =
        
        let calendar = Calendar.FindCalendar("WE")
        let orderDate = ctx.OrderDate        
        let reference_aum = ctx.ReferenceAUM

        match ctx.ReferenceAUM with
        | 0.0 -> () // Stop calculations
        | _ ->      // Run calculations
            let sign = (int)this.[orderDate.DateTime, Strategy._direction_id_do_not_use, TimeSeriesRollType.Last]
            let contract = (int)this.[orderDate.DateTime, (int)MemoryType.Contract, TimeSeriesRollType.Last]
            let rollDayDouble = this.[orderDate.DateTime, (int)MemoryType.RollDay, TimeSeriesRollType.Last]
            let FXHedgeFlag = (int)this.Portfolio.MasterPortfolio.Strategy.[orderDate.DateTime, (int)MemoryType.FXHedgeFlag, TimeSeriesRollType.Last]
            if this._underlyingInstrument = null then
                this._underlyingInstrument <- Instrument.FindInstrument((int)this.[orderDate.Close, (int)MemoryType.UnderlyingID, TimeSeriesRollType.Last])

            let positions = this.Portfolio.Positions(orderDate.DateTime)
            let rollDate = if Double.IsNaN(rollDayDouble) then 5 else (int)rollDayDouble            
            let instrument = this._underlyingInstrument;
            
            // list of positions in the portfolio
            let positions_sorted = if not (positions = null) then
                                        positions
                                        |> Seq.toList
                                        |> List.filter (fun pos ->
                                            pos.Instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Future)
                                        |> List.filter (fun pos ->
                                            let fut = pos.Instrument :?> Future
                                            fut.Underlying = instrument)
                                        |> List.sortBy (fun pos ->
                                            let fut = pos.Instrument :?> Future
                                            fut.LastTradeDate)
                                        |> List.toArray
                                    else
                                        null

            // position in future with closest expiry
            let position0 = if not (positions = null) && positions_sorted.Length = 1 then positions_sorted.[0] else null
            // future with closest expiry
            let future_current0 = if not (positions = null) && positions_sorted.Length = 1 then position0.Instrument :?> Future else null            
            // roll date of the future with closest expiry
            let currentRollDate0 = if future_current0 = null then null else calendar.GetClosestBusinessDay((if future_current0.LastTradeDate < future_current0.FirstNoticeDate then future_current0.LastTradeDate else future_current0.FirstNoticeDate), TimeSeries.DateSearchType.Next).AddBusinessDays(-(rollDate))

            // if a position exists
            if not (currentRollDate0 = null) then
                let old_unit = position0.Unit
                // if the future held in the portfolio has a roll date that is prior or equal to the current date
                if (Calendar.Close(currentRollDate0.DateTime) <= orderDate.DateTime) then //Roll at close of day
                    
                    // cut existing position
                    position0.UpdateTargetMarketOrder(orderDate.DateTime, 0.0, UpdateType.OverrideUnits) |> ignore
                    this.RemoveInstruments(orderDate.DateTime)
                    
                    // get next future                    
                    let nextFuture = ref (Future.CurrentFuture(instrument, orderDate.DateTime.Date))
                    let nextRollDate = (if !nextFuture = null then null else calendar.GetClosestBusinessDay((if (!nextFuture).LastTradeDate < (!nextFuture).FirstNoticeDate then (!nextFuture).LastTradeDate else (!nextFuture).FirstNoticeDate), TimeSeries.DateSearchType.Next).AddBusinessDays(-(rollDate)))

                    let activeMonths = this.GetActiveMonths(orderDate.DateTime)

                    if not (nextRollDate = null) && (nextRollDate.DateTime <= orderDate.DateTime.Date) then
                        nextFuture := (!nextFuture).NextFuture

                    while not(activeMonths.[(!nextFuture).ContractMonth - 1] = 1.0) do
                        nextFuture := (!nextFuture).NextFuture

                    if not (!nextFuture = null) then                    
                        // roll to the specified contract
                        [1 .. contract - 1] |> List.iter (fun i -> nextFuture := (!nextFuture).NextFuture)
                        
                        this.RemoveInstruments(orderDate.DateTime)
                        let contract_value = (!nextFuture).[orderDate.Close, TimeSeriesType.Last, DataProvider.DefaultProvider, TimeSeriesRollType.Last] * (!nextFuture).PointSize

                        if FXHedgeFlag = 1 then
                            this.Portfolio.HedgeFX(orderDate.DateTime, 0.0)

                        let unit = old_unit
                        this.AddInstrument(!nextFuture, orderDate.DateTime)
                        
                        this.Portfolio.CreateTargetMarketOrder(!nextFuture, orderDate.DateTime, Math.Abs(unit) * (double) sign) |> ignore
                    
            // If no positions exist               
            else
            
                // find next contract
                let nextFuture = ref (Future.CurrentFuture(instrument, orderDate.DateTime.Date))                
                let nextRollDate = (if !nextFuture = null then null else calendar.GetClosestBusinessDay((if (!nextFuture).LastTradeDate < (!nextFuture).FirstNoticeDate then (!nextFuture).LastTradeDate else (!nextFuture).FirstNoticeDate), TimeSeries.DateSearchType.Next).AddBusinessDays(-(rollDate - 1 * 0)))

                let activeMonths = this.GetActiveMonths(orderDate.DateTime)

                if not (nextRollDate = null) then
                    if (nextRollDate.DateTime <= orderDate.DateTime.Date) then
                        nextFuture := (!nextFuture).NextFuture

                    while not(activeMonths.[(!nextFuture).ContractMonth - 1] = 1.0) do
                        nextFuture := (!nextFuture).NextFuture


                // roll to the specified contract
                [1 .. contract - 1] |> List.iter (fun i -> nextFuture := (!nextFuture).NextFuture)

                if not (!nextFuture = null) then                
                    let reference_aum_local = reference_aum * CurrencyPair.Convert(1.0, orderDate.DateTime, TimeSeriesType.Last, DataProvider.DefaultProvider, TimeSeriesRollType.Last, this.Portfolio.Currency, instrument.Currency)

                    if FXHedgeFlag = 1 then
                            this.Portfolio.HedgeFX(orderDate.DateTime, 0.0)

                    this.RemoveInstruments(orderDate.DateTime)
                    let contractValue = (!nextFuture).[orderDate.Close, TimeSeriesType.Last, DataProvider.DefaultProvider, TimeSeriesRollType.Last] * (!nextFuture).PointSize
                    let unit = (if Double.IsNaN(reference_aum_local) then reference_aum else reference_aum_local) / contractValue                    
                    if (not (Double.IsInfinity(unit) || Double.IsNaN(unit))) then
                        this.AddInstrument(!nextFuture, orderDate.DateTime)
                        
                        this.Portfolio.CreateTargetMarketOrder(!nextFuture, orderDate.DateTime, Math.Abs(unit) * (double) sign) |> ignore


    /// <summary>
    /// Function: Set the direction type of the strategy
    /// </summary>       
    /// <param name="date">DateTime valued date 
    /// </param>
    /// <param name="sign">Long or Short
    /// </param>  
    override this.Direction(date : DateTime, sign : DirectionType, update : bool) =                        
        let sign_old = (int)this.[date, Strategy._direction_id_do_not_use, TimeSeriesRollType.Last]

        this.UnderlyingInstrument <- Instrument.FindInstrument((int)this.[date, (int)MemoryType.UnderlyingID, TimeSeriesRollType.Last])
        let positions = this.Portfolio.PositionOrders(date, true)
        let positions_sorted = if not (positions = null) then
                                positions.Values
                                |> Seq.toList
                                |> List.filter (fun pos ->
                                    pos.Instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Future)
                                |> List.filter (fun pos ->
                                    let fut = pos.Instrument :?> Future
                                    fut.Underlying = this.UnderlyingInstrument)
                                |> List.sortBy (fun pos ->
                                    let fut = pos.Instrument :?> Future
                                    fut.LastTradeDate)
                                |> List.toArray

                                else
                                    null

        let sign_old =
            if positions_sorted |> isNull || positions_sorted |> Array.isEmpty then
                sign_old
            elif positions_sorted.[0].Unit <> 0.0 then
                Math.Sign(positions_sorted.[0].Unit)
            else
                sign_old


        if ((int)sign = sign_old) then
            ()
        else
            this.AddMemoryPoint(date, (double)sign, Strategy._direction_id_do_not_use)           
            //this.UnderlyingInstrument <- Instrument.FindInstrument((int)this.[date, (int)MemoryType.UnderlyingID, TimeSeriesRollType.Last])
            let positions = this.Portfolio.PositionOrders(date, true)
            //let positions = this.Portfolio.AggregatedPositionOrders(date);
            let positions_sorted = if not (positions = null) then
                                    positions.Values
                                    |> Seq.toList
                                    |> List.filter (fun pos ->
                                        pos.Instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Future)
                                    |> List.filter (fun pos ->
                                        let fut = pos.Instrument :?> Future//Future.FindFuture(Security.FindSecurity(pos.Instrument))
                                        fut.Underlying = this.UnderlyingInstrument)
                                    |> List.sortBy (fun pos ->
                                        let fut = pos.Instrument :?> Future//Future.FindFuture(Security.FindSecurity(pos.Instrument))
                                        fut.LastTradeDate)
                                    |> List.toArray

                                    else
                                        null

            let position0 = ref null
            if (not (positions = null) && positions_sorted.Length = 1) then
                position0 := positions_sorted.[0]
                let orders = this.Portfolio.FindOpenOrder(position0.Value.Instrument, date, false)
                let order = if orders.Count = 0 then null else orders.Values  |> Seq.toList |> List.filter (fun o -> o.Type = OrderType.Market) |> List.reduce (fun acc o -> o) 
                let position = this.Portfolio.FindPosition(position0.Value.Instrument, date)

                if not(order = null) then
                    if Portfolio.DebugPositions then
                        "-- Rolling Future Order: " + sign.ToString() + " " + (!position0).Unit.ToString() |> Console.WriteLine
                    order.UpdateTargetMarketOrder(date, (double)sign * Math.Abs((!position0).Unit), UpdateType.OverrideUnits) |> ignore
                elif not(position = null) then
                    "-- Rolling Future Position: " + sign.ToString() + " " + (!position0).Unit.ToString() |> Console.WriteLine
                    position.UpdateTargetMarketOrder(date, (double)sign * Math.Abs((!position0).Unit), UpdateType.OverrideUnits) |> ignore
                                              

    override this.Direction(date : DateTime) =
        let sign_old = (int)this.[date, Strategy._direction_id_do_not_use, TimeSeriesRollType.Last]

        this.UnderlyingInstrument <- Instrument.FindInstrument((int)this.[date, (int)MemoryType.UnderlyingID, TimeSeriesRollType.Last])
        let positions = this.Portfolio.PositionOrders(date, true);
        
        let positions_sorted = if not (positions = null) then
                                positions.Values
                                |> Seq.toList
                                |> List.filter (fun pos ->
                                    pos.Instrument.InstrumentType = AQI.AQILabs.Kernel.InstrumentType.Future)
                                |> List.filter (fun pos ->
                                    let fut = pos.Instrument :?> Future
                                    fut.Underlying = this.UnderlyingInstrument)
                                |> List.sortBy (fun pos ->
                                    let fut = pos.Instrument :?> Future
                                    fut.LastTradeDate)
                                |> List.toArray

                                else
                                    null

        let sign_old =
            if positions_sorted |> isNull then
                sign_old
            elif positions_sorted.[0].Unit <> 0.0 then
                Math.Sign(positions_sorted.[0].Unit)
            else
                sign_old


        Enum.ToObject(typeof<DirectionType>, sign_old) :?> DirectionType
                                              

    /// <summary>
    /// Function: Create a strategy
    /// </summary>    
    /// <param name="name">Name
    /// </param>
    /// <param name="initialDay">Creating date
    /// </param>
    /// <param name="initialValue">Starting NAV and portfolio AUM.
    /// </param>
    /// <param name="underlyingInstrument">Underlying instrument
    /// </param>
    /// <param name="contract">number of contract to roll into in the active chain
    /// </param>
    /// <param name="rollDay">number of business day of the month to implement the roll
    /// </param>
    /// <param name="portfolio">Portfolio to be used in this strategy
    /// </param>
    static member public CreateStrategy(instrument : Instrument, initialDate : BusinessDay, initialValue : double, underlyingInstrument : Instrument , contract : int, rollDay : int, portfolio : Portfolio) : RollingFutureStrategy =
        match instrument with
        | x when x.InstrumentType = InstrumentType.Strategy ->

            let Strategy = new RollingFutureStrategy(instrument)
                
            portfolio.Strategy <- Strategy
            if not(underlyingInstrument = null) then
                Strategy.Calendar <- underlyingInstrument.Calendar
                Strategy.AddMemoryPoint(DateTime.MinValue, (double)underlyingInstrument.ID, (int)MemoryType.UnderlyingID)

            Strategy.AddMemoryPoint(DateTime.MinValue, (double)contract, (int)MemoryType.Contract)
            Strategy.AddMemoryPoint(DateTime.MinValue, (double)rollDay, (int)MemoryType.RollDay)

            Strategy.Startup(initialDate, initialValue, portfolio)

            Strategy
        | _ -> raise (new Exception("Instrument not a Strategy"))


    /// <summary>
    /// Function: Create a strategy
    /// </summary>    
    /// <param name="name">Name
    /// </param>
    /// <param name="description">Description
    /// </param>
    /// <param name="initialDay">Creating date
    /// </param>
    /// <param name="initialValue">Starting NAV and portfolio AUM.
    /// </param>
    /// <param name="underlyingInstrument">Underlying instrument
    /// </param>
    /// <param name="contract">number of contract to roll into in the active chain
    /// </param>
    /// <param name="rollDay">number of business day of the month to implement the roll
    /// </param>
    /// <param name="parent">Parent portfolio of parent strategy
    /// </param>
    /// <param name="simulated">True if not stored in persistent storage
    /// </param>    
    static member public Create(name : string, description : string, startDate : DateTime, startValue : double, underlyingInstrument : Instrument , contract : int, rollDay : int, main_currency : Currency , simulated : Boolean) : RollingFutureStrategy =
            let calendar = Calendar.FindCalendar("All")

            let date = calendar.GetClosestBusinessDay(startDate, TimeSeries.DateSearchType.Previous)

            let strategy_funding = FundingType.TotalReturn

            let main_cash_strategy = Instrument.FindInstrument(main_currency.Name + " - Cash")

            // Master Strategy Portfolios
            let master_portfolio_instrument = Instrument.CreateInstrument(name + "/Portfolio", InstrumentType.Portfolio, description + " Strategy Portfolio", main_currency, FundingType.TotalReturn, simulated, false)
            let master_portfolio = Portfolio.CreatePortfolio(master_portfolio_instrument, main_cash_strategy, main_cash_strategy, null)                
            master_portfolio.TimeSeriesRoll <- TimeSeriesRollType.Last;

            Currency.Currencies
            |> Seq.iter (fun x_currency -> //ccy_name ->
                let x_cash_strategy = Instrument.FindInstrument(x_currency.Name + " - Cash")
                if not(x_cash_strategy = null) then
                    master_portfolio.AddReserve(x_currency, x_cash_strategy, x_cash_strategy))

            // Master Strategy Instruments, Strategies
            let master_strategy_instrument = Instrument.CreateInstrument(name, InstrumentType.Strategy, description + " Strategy", main_currency, strategy_funding, simulated, false)
            master_strategy_instrument.TimeSeriesRoll <- master_portfolio.TimeSeriesRoll
            let master_strategy = RollingFutureStrategy.CreateStrategy(master_strategy_instrument, date, startValue, underlyingInstrument, contract, rollDay, master_portfolio)
            master_strategy.Calendar <- calendar
            master_portfolio.Strategy <- master_strategy
                
            master_strategy
