/*
 * The MIT License (MIT)
 * Copyright (c) 2007-2019, Arturo Rodriguez All rights reserved.
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
 
CREATE TABLE Roles (
    ID NVarchar(50) Not Null,
    Name Text,
    Description Text,
    Parent NVarchar(50),
    AccessType int Not Null,
    profile NVarchar(50),
    url NVarchar(50),
    OwnerID NVarchar(150),
    PRIMARY KEY (ID)
    );

INSERT INTO Roles (ID, Name, Description, AccessType, profile, OwnerID) Values ('Public', 'Public', 'Public', -1, 'profile', 'QuantAppSecure_root');
INSERT INTO Roles (ID, Name, Description, AccessType, profile, OwnerID) Values ('Administrator', 'Administrator', 'Administrator', -1, 'profile', 'QuantAppSecure_root');

CREATE TABLE PermissionsRepository (
    PermissibleID NVarchar(250) Not Null,
    GroupID NVarchar(50) Not Null,
    AccessType Int Not Null,
    Type Text Not Null,
    PRIMARY KEY (PermissibleID, GroupID)
    );

INSERT INTO PermissionsRepository (PermissibleID, GroupID, AccessType, Type) Values ('QuantAppSecure_root','Public',0,'QuantApp.Kernel.User');
INSERT INTO PermissionsRepository (PermissibleID, GroupID, AccessType, Type) Values ('QuantAppSecure_anonymous','Public',0,'QuantApp.Kernel.User');

CREATE TABLE Users (
    FirstName Text,
    LastName Text,
    IdentityProvider NVarchar(150) Not Null,
    NameIdentifier NVarchar(150) Not Null,
    Email Text,
    TenantName NVarchar(150),
    Hash Text,
    Secret Text,
    GroupID NVarchar(50),
    StripeID NVarchar(150),
    MetaData Text,
    PRIMARY KEY (IdentityProvider, NameIdentifier)
    );

INSERT INTO Users (FirstName, LastName, IdentityProvider, NameIdentifier, Email, TenantName, Hash, Secret, GroupID, StripeID, MetaData) Values ('anonymous', '', 'QuantAppSecure', '3eaac093-9bd8-4bb5-a0a5-423b616cbcd7', 'anonymous', 'QuantAppSecure_anonymous', '8a9eccdf27c279700b10f8d89079bb6f', '', 'Public', '', '');
INSERT INTO Users (FirstName, LastName, IdentityProvider, NameIdentifier, Email, TenantName, Hash, Secret, GroupID, StripeID, MetaData) Values ('root', '', 'QuantAppSecure', 'dd554db4-b969-44d8-98ad-28575be368e5', 'root', 'QuantAppSecure_root', '202cb962ac59075b964b07152d234b70', '', '', '', '');

CREATE TABLE UserLoginRepository (
    UserID NVarchar(150) Not Null,
    Timestamp DateTime Not Null,
    IP Text Not Null,
    PRIMARY KEY (UserID, Timestamp)
    );

CREATE TABLE UserHistoryRepository (
    UserID NVarchar(150) Not Null,
    Timestamp DateTime Not Null,
    Url Text Not Null,
    IP Text Not Null,
    PRIMARY KEY (UserID, Timestamp)
    );

CREATE TABLE M (
    ID NVarChar(150) Not Null,
    EntryID NVarChar(150) Not Null,
    Entry Text Not Null,
    Assembly Text Not Null,
    Type Text Not Null,
    PRIMARY KEY (ID,EntryID)
    );


-- Quant Tables


CREATE TABLE PALM_Bookmarks (
    UserID NVarchar(150) Not Null,
    InstrumentID int Not Null,
    PRIMARY KEY (UserID, InstrumentID)
    );

CREATE TABLE PALM_Strategies (
    UserID NVarchar(150) Not Null,
    StrategyID int Not Null,
    AttorneyID NVarchar(150) Not Null,
    Master Bit Not Null,
    PRIMARY KEY (UserID, StrategyID, AttorneyID, Master)
    );

CREATE TABLE PALM_Pending (
    UserID NVarchar(150) Not Null,
    StrategyID int Not Null,
    AttorneyID NVarchar(150) Not Null,
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
    ID NVarchar(50) Not Null,
    InstrumentID int Not Null,
    DeclaredDate DateTime Not Null,
    ExDate DateTime Not Null,
    RecordDate DateTime Not Null,
    PayableDate DateTime Not Null,
    Amount float,
    Frequency NVarchar(250) Not Null,
    Type NVarchar(250) Not Null,
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
INSERT INTO DataProvider (ID, Name, Description) Values (0, 'AQI', 'Default Provider');

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
    IsCall Bit Not Null,
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
    ID NVarchar(50) Not Null,
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
    Deleted bit Not Null,
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
    ID Nvarchar(50) Not Null,
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
    ID Nvarchar(50) Not Null,
    PortfolioID int Not Null,
    ConstituentID int Not Null,
    OrderDate DateTime Not Null,
    Unit float Not Null,
    Aggregated bit Not Null,
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
    Aggregated bit Not Null,
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