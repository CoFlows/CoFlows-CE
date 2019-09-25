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
using AQI.AQILabs.Kernel.Factories;

using QuantApp.Kernel;

namespace AQI.AQILabs.Kernel.Adapters.SQL.Factories
{
    public class SQLInterestRateFactory : IInterestRateFactory
    {
        public readonly static object objLock = new object();

        private static string _interestRateTableName = "InterestRate";
        private Dictionary<int, DataTable> _mainTables = new Dictionary<int, DataTable>();
        private Dictionary<int, InterestRate> _rates = new Dictionary<int, InterestRate>();

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

        public void SetProperty(InterestRate rate, string name, object value)
        {
            lock (objLock)
            {
                DataTable table = _mainTables[rate.ID];

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

        public InterestRate CreateInterestRate(Instrument instrument, int maturity, InterestRateTenorType maturityType)
        {
            lock (objLock)
            {
                if (!(instrument.InstrumentType == InstrumentType.Deposit || instrument.InstrumentType == InstrumentType.InterestRateSwap))
                    throw new Exception("Instrument is not an Interest Rate");

                string searchString = "ID = " + instrument.ID;
                string targetString = null;
                DataTable table = Database.DB["Kernel"].GetDataTable(_interestRateTableName, targetString, searchString);
                DataRowCollection rows = table.Rows;

                if (rows.Count == 0)
                {
                    DataRow r = table.NewRow();
                    r["ID"] = instrument.ID;
                    r["Maturity"] = maturity;
                    r["MaturityType"] = (int)maturityType;
                    rows.Add(r);
                    Database.DB["Kernel"].UpdateDataTable(table);

                    return InterestRate.FindInterestRate(instrument);
                }
                throw new Exception("InterestRate Already Exists");
            }
        }
        public InterestRate FindInterestRate(Instrument instrument)
        {
            lock (objLock)
            {
                if (_rates.ContainsKey(instrument.ID))
                    return _rates[instrument.ID];

                if (instrument.InstrumentType == InstrumentType.Deposit || instrument.InstrumentType == InstrumentType.InterestRateSwap)
                {
                    string tableName = _interestRateTableName;
                    string searchString = "ID = " + instrument.ID;
                    string targetString = null;
                    DataTable table = Database.DB["Kernel"].GetDataTable(tableName, targetString, searchString);

                    DataRowCollection rows = table.Rows;
                    if (rows.Count == 0)
                        return null;

                    DataRow r = rows[0];
                    int maturity = GetValue<int>(r, "Maturity");
                    InterestRateTenorType maturityType = (InterestRateTenorType)GetValue<int>(r, "MaturityType");

                    InterestRate rate = new InterestRate(instrument, maturity, maturityType);

                    _rates.Add(instrument.ID, rate);
                    if (_mainTables.ContainsKey(instrument.ID))
                        _mainTables[instrument.ID] = table;
                    else
                        _mainTables.Add(instrument.ID, table);

                    return rate;
                }

                throw new Exception("Instrument is not an Interest Rate");
            }
        }

        public void Remove(InterestRate rate)
        {
            _rates.Remove(rate.ID);

            DataTable table = _mainTables[rate.ID];

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
    }

    public class SQLDepositFactory : IDepositFactory
    {
        public readonly static object objLock = new object();
        private string _depositTableName = "Deposit";
        private Dictionary<int, Deposit> _deposits = new Dictionary<int, Deposit>();
        private Dictionary<int, DataTable> _mainTables = new Dictionary<int, DataTable>();

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

        public void SetProperty(Deposit deposit, string name, object value)
        {
            lock (objLock)
            {
                DataTable table = _mainTables[deposit.ID];

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

        public Deposit CreateDeposit(InterestRate rate, DayCountConvention dayCount)
        {
            if (rate.InstrumentType != InstrumentType.Deposit)
                throw new Exception("Instrument is not a Deposit");

            string searchString = null;// "Name LIKE '" + name + "'";
            string targetString = null;
            DataTable table = Database.DB["Kernel"].GetDataTable(_depositTableName, targetString, searchString);
            DataRowCollection rows = table.Rows;


            var lrows = from lrow in new LINQList<DataRow>(rows)
                        where GetValue<int>(lrow, "ID") == rate.ID
                        select lrow;

            if (lrows.Count() == 0)
            {
                DataRow r = table.NewRow();
                r["ID"] = rate.ID;
                r["DayCountConvention"] = (int)dayCount;
                rows.Add(r);
                Database.DB["Kernel"].UpdateDataTable(table);

                return FindDeposit(rate);
            }
            throw new Exception("Deposit Already Exists");
        }
        public Deposit FindDeposit(InterestRate rate)
        {
            if (rate.InstrumentType != InstrumentType.Deposit)
                throw new Exception("Instrument is not a Deposit");

            string tableName = _depositTableName;
            string searchString = "ID = " + rate.ID;
            string targetString = null;
            DataTable table = Database.DB["Kernel"].GetDataTable(tableName, targetString, searchString);

            DataRowCollection rows = table.Rows;
            if (rows.Count == 0)
                return null;

            DataRow r = rows[0];
            DayCountConvention dayCountConvention = (DayCountConvention)GetValue<int>(r, "DayCountConvention");

            Deposit deposit = new Deposit(rate, dayCountConvention);

            _deposits.Add(deposit.ID, deposit);
            if (_mainTables.ContainsKey(deposit.ID))
                _mainTables[deposit.ID] = table;
            else
                _mainTables.Add(deposit.ID, table);

            return deposit;
        }

        public void Remove(Deposit deposit)
        {
            _deposits.Remove(deposit.ID);

            DataTable table = _mainTables[deposit.ID];

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
    }

    public class SQLInterestRateSwapFactory : IInterestRateSwapFactory
    {
        public readonly static object objLock = new object();
        private static string _interestRateSwapTableName = "InterestRateSwap";
        private Dictionary<int, InterestRateSwap> _swaps = new Dictionary<int, InterestRateSwap>();
        private Dictionary<int, DataTable> _mainTables = new Dictionary<int, DataTable>();

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

        public void SetProperty(InterestRateSwap swap, string name, object value)
        {
            lock (objLock)
            {
                DataTable table = _mainTables[swap.ID];

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

        public InterestRateSwap CreateInterestRateSwap(InterestRate instrument, int floatFrequency, InterestRateTenorType floatFrequencyType, DayCountConvention floatDayCount, int fixedFrequency, InterestRateTenorType fixedFrequencyType, DayCountConvention fixedDayCount, int effective)
        {
            lock (objLock)
            {
                if (instrument.InstrumentType != InstrumentType.InterestRateSwap)
                    throw new Exception("Instrument is not an Interest Rate Swap");

                string searchString = null;// "Name LIKE '" + name + "'";
                string targetString = null;
                DataTable table = Database.DB["Kernel"].GetDataTable(_interestRateSwapTableName, targetString, searchString);
                DataRowCollection rows = table.Rows;


                var lrows = from lrow in new LINQList<DataRow>(rows)
                            where (int)lrow["ID"] == instrument.ID
                            select lrow;

                if (lrows.Count() == 0)
                {
                    DataRow r = table.NewRow();
                    r["ID"] = instrument.ID;
                    r["FloatFrequency"] = floatFrequency;
                    r["FloatFrequencyType"] = (int)floatFrequencyType;
                    r["FloatDayCountConvention"] = (int)floatDayCount;
                    r["FixedFrequency"] = fixedFrequency;
                    r["FixedFrequencyType"] = (int)fixedFrequencyType;
                    r["FixedDayCountConvention"] = (int)fixedDayCount;
                    r["Effective"] = effective;
                    rows.Add(r);
                    Database.DB["Kernel"].UpdateDataTable(table);

                    return FindInterestRateSwap(instrument);
                }
                throw new Exception("InterestRateSwap Already Exists");
            }
        }
        public InterestRateSwap FindInterestRateSwap(InterestRate instrument)
        {
            lock (objLock)
            {
                if (_swaps.ContainsKey(instrument.ID))
                    return _swaps[instrument.ID];

                if (instrument.InstrumentType != InstrumentType.InterestRateSwap)
                    throw new Exception("Instrument is not an Interest Rate Swap");

                string tableName = _interestRateSwapTableName;
                string searchString = "ID = " + instrument.ID;
                string targetString = null;
                DataTable table = Database.DB["Kernel"].GetDataTable(tableName, targetString, searchString);

                DataRowCollection rows = table.Rows;
                if (rows.Count == 0)
                    return null;

                DataRow r = rows[0];

                int floatFrequency = GetValue<int>(r, "FloatFrequency");
                InterestRateTenorType floatFrequencyType = (InterestRateTenorType)GetValue<int>(r, "FloatFrequencyType");
                DayCountConvention floatDayCountConvention = (DayCountConvention)GetValue<int>(r, "FloatDayCountConvention");
                int fixedFrequency = GetValue<int>(r, "FixedFrequency");
                InterestRateTenorType fixedFrequencyType = (InterestRateTenorType)GetValue<int>(r, "FixedFrequencyType");
                DayCountConvention fixedDayCountConvention = (DayCountConvention)GetValue<int>(r, "FixedDayCountConvention");
                int effective = GetValue<int>(r, "Effective");

                InterestRateSwap swap = new InterestRateSwap(instrument, floatFrequency, floatFrequencyType, floatDayCountConvention, fixedFrequency, fixedFrequencyType, fixedDayCountConvention, effective);

                _swaps.Add(swap.ID, swap);
                if (_mainTables.ContainsKey(swap.ID))
                    _mainTables[swap.ID] = table;
                else
                    _mainTables.Add(swap.ID, table);

                return swap;
            }
        }

        public void Remove(InterestRateSwap swap)
        {
            lock (objLock)
            {
                _swaps.Remove(swap.ID);

                DataTable table = _mainTables[swap.ID];

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
        }
    }
}