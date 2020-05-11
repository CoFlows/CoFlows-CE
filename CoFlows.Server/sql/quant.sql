/*
 * The MIT License (MIT)
 * Copyright (c) Arturo Rodriguez All rights reserved.
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
 
CREATE TABLE PALM_Bookmarks (
    UserID Varchar(150) Not Null,
    InstrumentID int Not Null,
    PRIMARY KEY (UserID, InstrumentID)
    );

CREATE TABLE PALM_Strategies (
    UserID Varchar(150) Not Null,
    StrategyID int Not Null,
    AttorneyID Varchar(150) Not Null,
    Master int Not Null,
    PRIMARY KEY (UserID, StrategyID, AttorneyID, Master)
    );

CREATE TABLE PALM_Pending (
    UserID Varchar(150) Not Null,
    StrategyID int Not Null,
    AttorneyID Varchar(150) Not Null,
    Provider Text,
    AccountID Text,
    SubmissionDate DateTime Not Null,
    CreationDate DateTime,
    PRIMARY KEY (UserID, AttorneyID, SubmissionDate)
    );

CREATE TABLE Categories (
    ID int Not Null,
    AssetClass int,
    GeographicalRegion int,
    PRIMARY KEY (ID)
    );

CREATE TABLE CorporateAction (
    ID Varchar(50) Not Null,
    InstrumentID int Not Null,
    DeclaredDate DateTime Not Null,
    ExDate DateTime Not Null,
    RecordDate DateTime Not Null,
    PayableDate DateTime Not Null,
    Amount float,
    Frequency Varchar(250) Not Null,
    Type Varchar(250) Not Null,
    PRIMARY KEY (ID, InstrumentID, DeclaredDate, ExDate, RecordDate, PayableDate, Frequency, Type)
    );

CREATE TABLE Currency (
    ID int Not Null,
    Name Text Not Null,
    Description Text Not Null,
    CalendarID int Not Null,
    PRIMARY KEY (ID)
    );
-- INSERT INTO Currency (ID, Name, Description, CalendarID) Values (0, 'USD', 'United States', 0);

CREATE TABLE CurrencyPair (
    CurrencyBuyID int Not Null,
    CurrencySellID int Not Null,
    FXInstrumentID int Not Null,
    PRIMARY KEY (CurrencyBuyID, CurrencySellID, FXInstrumentID)
    );

CREATE TABLE DataProvider (
    ID int Not Null,
    Name Text Not Null,
    Description Text Not Null,
    PRIMARY KEY (ID)
    );
-- INSERT INTO DataProvider (ID, Name, Description) Values (0, 'AQI', 'Default Provider');

CREATE TABLE Deposit (
    ID int Not Null,
    DayCountConvention int,
    PRIMARY KEY (ID)
    );

CREATE TABLE Exchange (
    ID int Not Null,
    Name Text Not Null,
    Description Text Not Null,
    CalendarID int Not Null,
    PRIMARY KEY (ID)
    );

CREATE TABLE Future (
    ID int Not Null,
    FutureGenericMonths Text Not Null,
    FirstDeliveryDate DateTime Not Null,
    FirstNoticeDate DateTime Not Null,
    LastDeliveryDate DateTime Not Null,
    FirstTradeDate DateTime Not Null,
    LastTradeDate DateTime Not Null,
    TickSize Float Not Null,
    ContractSize Float Not Null,
    UnderlyingInstrumentID int Not Null,
    ContractMonth int,
    ContractYear int,
    PointSize float Not Null,
    PRIMARY KEY (ID)
    );

CREATE TABLE Instructions (
    PortfolioID int Not Null,
    InstrumentID int Not Null,
    Client Text,
    Destination Text,
    Account Text,
    Execution Text,
    MinExecution float Not Null,
    MaxExecution float Not Null,
    MinSize float,
    MinStep float,
    Margin float,
    PRIMARY KEY (PortfolioID, InstrumentID)
    );

CREATE TABLE Instrument (
    ID int Not Null,
    Name Text Not Null,
    InstrumentTypeID int Not Null,
    Description Text Not Null,
    CurrencyID int Not Null,
    FundingTypeID int Not Null,
    CustomCalendarID int Not Null,
    LongDescription Text,
    PRIMARY KEY (ID)
    );

-- CREATE TABLE InstrumentUniverseEntry (
--     ID int Not Null,
--     Timestamp DateTime Not Null,
--     ConstituentID int Not Null,
--     CONSTRAINT PK_InstrumentUniverseEntry PRIMARY KEY (ID)
-- )

-- CREATE TABLE InstrumentUniverse (
--     ID int Not Null,
--     Name Text Not Null,
--     Description Text Not Null,
--     CONSTRAINT PK_InstrumentUniverse PRIMARY KEY (ID)
-- )

CREATE TABLE InterestRate (
    ID int Not Null,
    Maturity int Not Null,
    MaturityType int Not Null,
    PRIMARY KEY (ID)
    );

CREATE TABLE InterestRateSwap (
    ID int Not Null,
    FloatDayCountConvention int Not Null,
    FloatFrequency int Not Null,
    FloatFrequencyType int Not Null,
    FixedDayCountConvention int Not Null,
    FixedFrequency int Not Null,
    FixedFrequencyType int Not Null,
    Effective int Not Null,
    PRIMARY KEY (ID)
    );

CREATE TABLE Isin (
    ID int Not Null,
    Content Text Not Null,
    PRIMARY KEY (ID)
    );

CREATE TABLE Options (
    ID int Not Null,
    ContractSize float Not Null,
    UnderlyingInstrumentID int Not Null,
    ExpiryDate DateTime Not Null,
    FirstTradeDate DateTime Not Null,
    StrikePrice float Not Null,
    IsCall int Not Null,
    ExerciseType Text Not Null,
    PRIMARY KEY (ID)
    );

CREATE TABLE PortfolioReserves (
    ID int Not Null,
    CurrencyID int Not Null,
    LongReserveID int Not Null,
    ShortReserveID int Not Null,
    PRIMARY KEY (ID, CurrencyID, LongReserveID, ShortReserveID)
    );

CREATE TABLE Portfolio (
    ID int Not Null,
    LongReserveID int Not Null,
    ShortReserveID int Not Null,
    ParentPortfolioID int Not Null,
    StrategyID int Not Null,
    CustodianID Text,
    AccountID Text,
    ResidualID int Not Null,
    Username Text,
    Password Text,
    KeyID Text,
    PRIMARY KEY (ID)
    );

CREATE TABLE ProcessedCorporateAction (
    ID Varchar(50) Not Null,
    PortfolioID int Not Null,
    PRIMARY KEY (ID, PortfolioID)
    );

CREATE TABLE Security (
    ID int Not Null,
    Isin Text Not Null,
    ExchangeID int Not Null,
    PointSize float,
    Sedol Text Not Null,
    PRIMARY KEY (ID)
    );

CREATE TABLE Sedol (
    ID int Not Null,
    Content Text,
    PRIMARY KEY (ID)
    );

CREATE TABLE SettlementCalendar (
    ID int Not Null,
    Name Text Not Null,
    Description Text Not Null,
    PRIMARY KEY (ID)
    );

-- INSERT INTO SettlementCalendar (ID, Name, Description) Values (0, 'US', 'United States');

CREATE TABLE SettlementCalendarDate (
    ID int Not Null,
    Timestamp DateTime Not Null,
    BusinessDayMonth int Not Null,
    BusinessDayYear int Not Null,
    BusinessDayIndex int Not Null,
    PRIMARY KEY (ID, Timestamp, BusinessDayMonth, BusinessDayYear, BusinessDayIndex)
    );

-- INSERT INTO SettlementCalendarDate (ID, Timestamp, BusinessDayMonth, BusinessDayYear, BusinessDayIndex) Values (0, '1950-01-01', 0,0,0);

CREATE TABLE Stock (
    ID int Not Null,
    DividendCurrencyID int Not Null,
    GICSSectorName Text Not Null,
    GICSIndustryName Text Not Null,
    GICSIndustryGroupName Text Not Null,
    GICSSubIndustryName Text Not Null,
    PRIMARY KEY (ID)
    );

CREATE TABLE Strategy (
    ID int Not Null,
    Class Text Not Null,
    PortfolioID int Not Null,
    InitialDate DateTime,
    FinalDate DateTime,
    DBConnection Text,
    Scheduler Text,
    PRIMARY KEY (ID)
    );

CREATE TABLE SystemData (
    ID int Not Null,
    Revision int Not Null,
    Deleted int Not Null,
    CreateTime DateTime Not Null,
    UpdateTime DateTime Not Null,
    TimeSeriesAccessType int Not Null,
    TimeSeriesRollType int Not Null,
    ExecutionCost float Not Null,
    CarryCostLong float Not Null,
    CarryCostShort float Not Null,
    CarryCostDayCount int Not Null,
    CarryCostDayCountBase float Not Null,
    ScaleFactor float Not Null,
    PRIMARY KEY (ID)
    );

CREATE TABLE SystemLog (
    ID Varchar(50) Not Null,
    InstrumentID int Not Null,
    EntryDate DateTime Not Null,
    EntryType int Not Null,
    EntryMessage Text Not Null,
    PRIMARY KEY (ID)
    );

CREATE TABLE ThirdPartyData (
    ID int Not Null,
    BloombergTicker Text,
    ReutersRIC Text,
    CSIUAMarket Text,
    CSIDeliveryCode int,
    CSINumCode int,
    YahooTicker Text,
    PRIMARY KEY (ID)
    );

-- IN STRATEGY TOO
CREATE TABLE TimeSeries (
    ID int Not Null,
    TimeSeriesTypeID int Not Null,
    Timestamp DateTime Not Null,
    Value float Not Null,
    ProviderID int Not Null,
    PRIMARY KEY (ID, TimeSeriesTypeID, Timestamp, Value, ProviderID)
    );

-- IN STRATEGY
CREATE TABLE Orders (
    ID Varchar(50) Not Null,
    PortfolioID int Not Null,
    ConstituentID int Not Null,
    OrderDate DateTime Not Null,
    Unit float Not Null,
    Aggregated int Not Null,
    OrderType int Not Null,
    Limits float Not Null,
    Status int Not Null,
    ExecutionLevel float Not Null,
    ExecutionDate DateTime Not Null,
    Client Text,
    Destination Text,
    Accoun Text,
    PRIMARY KEY (ID, PortfolioID, ConstituentID, OrderDate, Unit, Aggregated, OrderType)
    );

CREATE TABLE Position (
    ID int Not Null,
    ConstituentID int Not Null,
    Timestamp DateTime Not Null,
    Unit float Not Null,
    Strike float Not Null,
    StrikeTimestamp DateTime Not Null,
    InitialStrike float Not Null,
    InitialStrikeTimestamp DateTime Not Null,
    Aggregated int Not Null,
    PRIMARY KEY (ID, ConstituentID, Timestamp, Unit, Strike, StrikeTimestamp, InitialStrike, InitialStrikeTimestamp, Aggregated)
    );

CREATE TABLE StrategyMemory (
    ID int Not Null,
    MemoryTypeID int Not Null,
    TimeStamp DateTime Not Null,
    Value float Not Null,
    MemoryClassID int Not Null,
    PRIMARY KEY (ID, MemoryTypeID, TimeStamp, Value, MemoryClassID)
    );

-- NEED TO FIX CALENDARS! AT LEAST WEEKEND!!!