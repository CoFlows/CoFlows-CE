/*
 * The MIT License (MIT)
 * Copyright (c) Arturo Rodriguez All rights reserved.
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
 
CREATE TABLE Roles (
    ID Varchar(50) Not Null,
    Name Text,
    Description Text,
    Parent Varchar(50),
    AccessType int Not Null,
    profile Varchar(50),
    url Varchar(50),
    OwnerID Varchar(150),
    PRIMARY KEY (ID)
    );

INSERT INTO Roles (ID, Name, Description, AccessType, profile, OwnerID) Values ('Public', 'Public', 'Public', -1, 'profile', 'QuantAppSecure_root');
INSERT INTO Roles (ID, Name, Description, AccessType, profile, OwnerID) Values ('Administrator', 'Administrator', 'Administrator', -1, 'profile', 'QuantAppSecure_root');

CREATE TABLE PermissionsRepository (
    PermissibleID Varchar(250) Not Null,
    GroupID Varchar(50) Not Null,
    AccessType Int Not Null,
    Type Text Not Null,
    PRIMARY KEY (PermissibleID, GroupID)
    );

INSERT INTO PermissionsRepository (PermissibleID, GroupID, AccessType, Type) Values ('QuantAppSecure_root','Administrator',2,'QuantApp.Kernel.User');
INSERT INTO PermissionsRepository (PermissibleID, GroupID, AccessType, Type) Values ('QuantAppSecure_root','Public',0,'QuantApp.Kernel.User');
INSERT INTO PermissionsRepository (PermissibleID, GroupID, AccessType, Type) Values ('QuantAppSecure_anonymous','Public',0,'QuantApp.Kernel.User');

CREATE TABLE Users (
    FirstName Text,
    LastName Text,
    IdentityProvider Varchar(150) Not Null,
    NameIdentifier Varchar(150) Not Null,
    Email Text,
    TenantName Varchar(150),
    Hash Varchar(150),
    Secret Varchar(150),
    GroupID Varchar(50),
    StripeID Varchar(150),
    MetaData Text,
    PRIMARY KEY (IdentityProvider, NameIdentifier)
    );

INSERT INTO Users (FirstName, LastName, IdentityProvider, NameIdentifier, Email, TenantName, Hash, Secret, GroupID, StripeID, MetaData) Values ('anonymous', '', 'QuantAppSecure', '3eaac093-9bd8-4bb5-a0a5-423b616cbcd7', 'anonymous', 'QuantAppSecure_anonymous', '8a9eccdf27c279700b10f8d89079bb6f', '', 'Public', '', '');
INSERT INTO Users (FirstName, LastName, IdentityProvider, NameIdentifier, Email, TenantName, Hash, Secret, GroupID, StripeID, MetaData) Values ('root', '', 'QuantAppSecure', 'dd554db4-b969-44d8-98ad-28575be368e5', 'root', 'QuantAppSecure_root', '202cb962ac59075b964b07152d234b70', '26499e5e555e9957725f51cc4d400384', '', '', '');

CREATE TABLE UserLoginRepository (
    UserID Varchar(150) Not Null,
    Timestamp DateTime Not Null,
    IP Text Not Null,
    PRIMARY KEY (UserID, Timestamp)
    );

CREATE TABLE UserHistoryRepository (
    UserID Varchar(150) Not Null,
    Timestamp DateTime Not Null,
    Url Text Not Null,
    IP Text Not Null,
    PRIMARY KEY (UserID, Timestamp)
    );

CREATE TABLE M (
    ID Varchar(150) Not Null,
    EntryID Varchar(150) Not Null,
    Entry Text Not Null,
    Assembly Text Not Null,
    Type Text Not Null,
    PRIMARY KEY (ID,EntryID)
    );