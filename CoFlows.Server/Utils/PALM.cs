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

using QuantApp.Kernel;
using AQI.AQILabs.Kernel;


namespace CoFlows.Server.Utils
{
    public class PALMStrategy
    {
        public Strategy Strategy;
        public QuantApp.Kernel.User User;
        public QuantApp.Kernel.User Attorney;

        public PALMStrategy(Strategy strategy, QuantApp.Kernel.User user, QuantApp.Kernel.User attorney)
        {
            this.Strategy = strategy;
            this.User = user;
            this.Attorney = attorney;
        }
    }

    public class PALMPending
    {
        public Strategy Strategy;
        public QuantApp.Kernel.User User;
        public QuantApp.Kernel.User Attorney;
        public string Provider;
        public string AccountID;
        public DateTime SubmissionDate;
        public DateTime CreationDate;

        public PALMPending(Strategy strategy, QuantApp.Kernel.User user, QuantApp.Kernel.User attorney, string provider, string accountid, DateTime submissionDate, DateTime creationDate)
        {
            this.Strategy = strategy;
            this.User = user;
            this.Attorney = attorney;
            this.Provider = provider;
            this.AccountID = accountid;
            this.SubmissionDate = submissionDate;
            this.CreationDate = creationDate;
        }
    }

    public class PALM
    {
        public static string MasterName(QuantApp.Kernel.User user)
        {
            return "_PALM_" + user.ID + "_MaStEr";
        }

        public static List<Instrument> GetBookmarks(QuantApp.Kernel.User user)
        {
            string tableName = "PALM_Bookmarks";
            string searchString = "UserID LIKE '" + user.ID + "'";
            string targetString = null;
            DataTable _dataTable = QuantApp.Kernel.Database.DB["CloudApp"].GetDataTable(tableName, targetString, searchString);

            DataRowCollection rows = _dataTable.Rows;

            List<Instrument> ret = new List<Instrument>();

            if (rows.Count != 0)
            {
                foreach (DataRow row in rows)
                {
                    string userID = GetValue<string>(row, "UserID");
                    int instrumentID = GetValue<int>(row, "InstrumentID");

                    Instrument ins = Instrument.FindInstrument(instrumentID);
                    ret.Add(ins);
                }
            }

            return ret;
        }
        public static void AddBookmark(QuantApp.Kernel.User user, Instrument instrument)
        {
            string tableName = "PALM_Bookmarks";
            string searchString = "UserID LIKE '" + user.ID + "' AND InstrumentID = " + instrument.ID;
            string targetString = null;
            DataTable _dataTable = QuantApp.Kernel.Database.DB["CloudApp"].GetDataTable(tableName, targetString, searchString);

            DataRowCollection rows = _dataTable.Rows;

            if (rows.Count != 0)
                throw new Exception("Bookmark exists.");

            else
            {
                DataRow r = _dataTable.NewRow();

                r["UserID"] = user.ID;
                r["InstrumentID"] = instrument.ID;

                rows.Add(r);
                QuantApp.Kernel.Database.DB["CloudApp"].UpdateDataTable(_dataTable);
            }
        }
        public static void RemoveStrategy(QuantApp.Kernel.User user, Instrument instrument)
        {
            string tableName = "PALM_Strategies";
            string searchString = "UserID LIKE '" + user.ID + "' AND StrategyID = " + instrument.ID;
            string targetString = null;
            DataTable _dataTable = QuantApp.Kernel.Database.DB["CloudApp"].GetDataTable(tableName, targetString, searchString);

            DataRowCollection rows = _dataTable.Rows;

            if (rows.Count != 0)
            {
                rows[0].Delete();
                QuantApp.Kernel.Database.DB["CloudApp"].UpdateDataTable(_dataTable);
            }
        }

        public static void RemoveBookmark(QuantApp.Kernel.User user, Instrument instrument)
        {
            string tableName = "PALM_Bookmarks";
            string searchString = "UserID LIKE '" + user.ID + "' AND InstrumentID = " + instrument.ID;
            string targetString = null;
            DataTable _dataTable = QuantApp.Kernel.Database.DB["CloudApp"].GetDataTable(tableName, targetString, searchString);

            DataRowCollection rows = _dataTable.Rows;

            if (rows.Count != 0)
            {
                rows[0].Delete();
                QuantApp.Kernel.Database.DB["CloudApp"].UpdateDataTable(_dataTable);
            }
        }


        public static List<PALMPending> GetPending(QuantApp.Kernel.User user)
        {
            string tableName = "PALM_Pending";
            string searchString = "UserID LIKE '" + user.ID + "' AND StrategyID = -1";
            string targetString = null;
            DataTable _dataTable = QuantApp.Kernel.Database.DB["CloudApp"].GetDataTable(tableName, targetString, searchString);

            DataRowCollection rows = _dataTable.Rows;

            List<PALMPending> ret = new List<PALMPending>();

            if (rows.Count != 0)
            {
                foreach (DataRow row in rows)
                {
                    string userID = GetValue<string>(row, "UserID");
                    int strategyID = GetValue<int>(row, "StrategyID");
                    string attorneyID = GetValue<string>(row, "AttorneyID");

                    string provider = GetValue<string>(row, "Provider");
                    string accountID = GetValue<string>(row, "AccountID");
                    DateTime submissionDate = GetValue<DateTime>(row, "SubmissionDate");
                    DateTime creationDate = GetValue<DateTime>(row, "CreationDate");

                    ret.Add(new PALMPending(strategyID == -1 ? null : Instrument.FindInstrument(strategyID) as Strategy, QuantApp.Kernel.User.FindUser(userID), QuantApp.Kernel.User.FindUser(attorneyID), provider, accountID, submissionDate, creationDate));
                }
            }

            return ret;
        }

        public static List<PALMPending> GetPendingAll(QuantApp.Kernel.User user)
        {
            string tableName = "PALM_Pending";
            string searchString = "UserID LIKE '" + user.ID + "'";
            string targetString = null;
            DataTable _dataTable = QuantApp.Kernel.Database.DB["CloudApp"].GetDataTable(tableName, targetString, searchString);

            DataRowCollection rows = _dataTable.Rows;

            List<PALMPending> ret = new List<PALMPending>();

            if (rows.Count != 0)
            {
                foreach (DataRow row in rows)
                {
                    string userID = GetValue<string>(row, "UserID");
                    int strategyID = GetValue<int>(row, "StrategyID");
                    string attorneyID = GetValue<string>(row, "AttorneyID");

                    string provider = GetValue<string>(row, "Provider");
                    string accountID = GetValue<string>(row, "AccountID");
                    DateTime submissionDate = GetValue<DateTime>(row, "SubmissionDate");
                    DateTime creationDate = GetValue<DateTime>(row, "CreationDate");

                    ret.Add(new PALMPending(strategyID == -1 ? null : Instrument.FindInstrument(strategyID) as Strategy, QuantApp.Kernel.User.FindUser(userID), QuantApp.Kernel.User.FindUser(attorneyID), provider, accountID, submissionDate, creationDate));
                }
            }

            return ret;
        }

        public static void UpdatePending(PALMPending pending)
        {
            string tableName = "PALM_Pending";
            string searchString = "UserID LIKE '" + pending.User.ID + "' AND Provider LIKE '" + pending.Provider + "' AND AccountID LIKE '" + pending.AccountID + "'";
            string targetString = null;
            DataTable _dataTable = QuantApp.Kernel.Database.DB["CloudApp"].GetDataTable(tableName, targetString, searchString);

            DataRowCollection rows = _dataTable.Rows;
            List<PALMPending> ret = new List<PALMPending>();

            if (rows.Count != 0)
            {
                foreach (DataRow row in rows)
                {
                    row["StrategyID"] = pending.Strategy == null ? -1 : pending.Strategy.ID;
                    row["AttorneyID"] = pending.Attorney.ID;
                    row["Provider"] = pending.Provider;
                    row["AccountID"] = pending.AccountID;
                    if (pending.CreationDate != DateTime.MinValue || pending.CreationDate != DateTime.MaxValue)
                        row["CreationDate"] = pending.CreationDate;
                }
            }

            QuantApp.Kernel.Database.DB["CloudApp"].UpdateDataTable(_dataTable);
        }

        public static void AddPending(PALMPending pending)
        {
            string tableName = "PALM_Pending";
            string searchString = "UserID LIKE '" + pending.User.ID + "'";
            string targetString = null;
            DataTable _dataTable = QuantApp.Kernel.Database.DB["CloudApp"].GetDataTable(tableName, targetString, searchString);

            DataRowCollection rows = _dataTable.Rows;

            var lrows = from lrow in new LINQList<DataRow>(rows)
                        where (DateTime)lrow["SubmissionDate"] == pending.SubmissionDate && (string)lrow["Provider"] == pending.Provider && (string)lrow["AccountID"] == pending.AccountID
                        select lrow;

            if (lrows.Count() == 0)
            {
                DataRow r = _dataTable.NewRow();
                r["UserID"] = pending.User.ID;
                r["StrategyID"] = pending.Strategy == null ? -1 : pending.Strategy.ID;
                r["SubmissionDate"] = pending.SubmissionDate;

                r["AttorneyID"] = pending.Attorney == null ? "" : pending.Attorney.ID;
                r["Provider"] = pending.Provider;
                r["AccountID"] = pending.AccountID;

                rows.Add(r);
                QuantApp.Kernel.Database.DB["CloudApp"].UpdateDataTable(_dataTable);
            }
        }

        public static PALMStrategy GetStrategy(Strategy strategy)
        {
            string tableName = "PALM_Strategies";
            string searchString = "StrategyID = " + strategy.ID;
            string targetString = null;
            DataTable _dataTable = QuantApp.Kernel.Database.DB["CloudApp"].GetDataTable(tableName, targetString, searchString);

            DataRowCollection rows = _dataTable.Rows;

            List<PALMStrategy> ret = new List<PALMStrategy>();

            if (rows.Count != 0)
            {
                foreach (DataRow row in rows)
                {
                    string userID = GetValue<string>(row, "UserID");
                    int strategyID = GetValue<int>(row, "StrategyID");
                    string attorneyID = GetValue<string>(row, "AttorneyID");

                    return new PALMStrategy(strategy, QuantApp.Kernel.User.FindUser(userID), QuantApp.Kernel.User.FindUser(attorneyID));
                }
            }

            return null;
        }
        public static void UpdateAttorney(Strategy strategy, QuantApp.Kernel.User attorney)
        {
            string tableName = "PALM_Strategies";
            string searchString = "StrategyID = " + strategy.ID;
            string targetString = null;
            DataTable _dataTable = QuantApp.Kernel.Database.DB["CloudApp"].GetDataTable(tableName, targetString, searchString);

            DataRowCollection rows = _dataTable.Rows;

            List<PALMStrategy> ret = new List<PALMStrategy>();

            if (rows.Count != 0)
            {
                foreach (DataRow row in rows)
                {
                    row["AttorneyID"] = attorney.ID;
                }
            }

            QuantApp.Kernel.Database.DB["CloudApp"].UpdateDataTable(_dataTable);
        }


        public static List<PALMStrategy> GetStrategy(QuantApp.Kernel.User user, bool master)
        {
            string tableName = "PALM_Strategies";
            string searchString = "UserID LIKE '" + user.ID + "' AND Master = " + (master ? 1 : 0);
            string targetString = null;
            DataTable _dataTable = QuantApp.Kernel.Database.DB["CloudApp"].GetDataTable(tableName, targetString, searchString);

            DataRowCollection rows = _dataTable.Rows;

            List<PALMStrategy> ret = new List<PALMStrategy>();

            if (rows.Count != 0)
            {
                foreach (DataRow row in rows)
                {
                    string userID = GetValue<string>(row, "UserID");
                    int strategyID = GetValue<int>(row, "StrategyID");
                    string attorneyID = GetValue<string>(row, "AttorneyID");

                    ret.Add(new PALMStrategy(Instrument.FindInstrument(strategyID) as Strategy, QuantApp.Kernel.User.FindUser(userID), QuantApp.Kernel.User.FindUser(attorneyID)));
                }
            }

            return ret;
        }

        public static List<PALMStrategy> GetAllStrategies(QuantApp.Kernel.User user, bool master)
        {
            string tableName = "PALM_Strategies";
            string searchString = "(UserID LIKE '" + user.ID + "' OR AttorneyID LIKE '" + user.ID + "') AND Master = " + (master ? 1 : 0);
            string targetString = null;
            DataTable _dataTable = QuantApp.Kernel.Database.DB["CloudApp"].GetDataTable(tableName, targetString, searchString);

            DataRowCollection rows = _dataTable.Rows;

            List<PALMStrategy> ret = new List<PALMStrategy>();

            if (rows.Count != 0)
            {
                foreach (DataRow row in rows)
                {
                    string userID = GetValue<string>(row, "UserID");
                    int strategyID = GetValue<int>(row, "StrategyID");
                    string attorneyID = GetValue<string>(row, "AttorneyID");

                    var strategy = Instrument.FindInstrument(strategyID) as Strategy;
                    if (strategy != null)
                    {
                        if (!strategy.Deleted)
                        {
                            ret.Add(new PALMStrategy(strategy, QuantApp.Kernel.User.FindUser(userID), QuantApp.Kernel.User.FindUser(attorneyID)));
                        }
                        else
                        {
                            RemoveStrategy(user, strategy);
                        }
                    }
                }
            }

            return ret;
        }

        public static PALMStrategy AddStrategy(QuantApp.Kernel.User user, QuantApp.Kernel.User attorney, Strategy strategy)
        {
            string tableName = "PALM_Strategies";
            string searchString = "UserID LIKE '" + user.ID + "' AND StrategyID = " + strategy.ID;
            string targetString = null;
            DataTable _dataTable = QuantApp.Kernel.Database.DB["CloudApp"].GetDataTable(tableName, targetString, searchString);

            DataRowCollection rows = _dataTable.Rows;

            if (rows.Count != 0)
                throw new Exception("Strategy exists.");

            else
            {
                DataRow r = _dataTable.NewRow();

                r["UserID"] = user.ID;
                r["AttorneyID"] = attorney.ID;
                r["StrategyID"] = strategy.ID;
                r["Master"] = 1;

                rows.Add(r);
                QuantApp.Kernel.Database.DB["CloudApp"].UpdateDataTable(_dataTable);

                return new PALMStrategy(strategy, user, user);
            }
        }

        public static List<PALMStrategy> StrategiesByAttorney(QuantApp.Kernel.User attorney)
        {
            string tableName = "PALM_Strategies";
            string searchString = "AttorneyID LIKE '" + attorney.ID + "'";
            string targetString = null;
            DataTable _dataTable = QuantApp.Kernel.Database.DB["CloudApp"].GetDataTable(tableName, targetString, searchString);

            DataRowCollection rows = _dataTable.Rows;

            List<PALMStrategy> result = new List<PALMStrategy>();

            if (rows.Count != 0)
            {
                foreach (DataRow row in rows)
                {

                    string userID = GetValue<string>(row, "UserID");
                    int strategyID = GetValue<int>(row, "StrategyID");
                    string attorneyID = GetValue<string>(row, "AttorneyID");

                    result.Add(new PALMStrategy(Instrument.FindInstrument(strategyID) as Strategy, QuantApp.Kernel.User.FindUser(userID), QuantApp.Kernel.User.FindUser(attorneyID)));
                }
            }

            return result;
        }

        private static T GetValue<T>(DataRow row, string columnname)
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
            object obj = row[columnname];
            if (obj is DBNull)
                return (T)res;
            return (T)obj;
        }
    }
}