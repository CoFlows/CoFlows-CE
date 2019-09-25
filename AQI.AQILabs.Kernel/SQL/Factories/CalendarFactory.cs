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
    public class SQLCalendarFactory : ICalendarFactory
    {
        private string _mainTableName = "SettlementCalendar";
        private static string _dateTableName = "SettlementCalendarDate";

        private Dictionary<int, Calendar> _calendarIdDB = new Dictionary<int, Calendar>();
        private Dictionary<string, Calendar> _calendarNameDB = new Dictionary<string, Calendar>();

        private Dictionary<int, DataTable> _mainTables = new Dictionary<int, DataTable>();
        private Dictionary<int, DataTable> _dateTables = new Dictionary<int, DataTable>();

        public readonly static object objLock = new object();

        public Calendar CreateCalendar(string name, string description)
        {
            lock (objLock)
            {
                string searchString = null;
                string targetString = null;
                DataTable table = Database.DB["Kernel"].GetDataTable(_mainTableName, targetString, searchString);
                DataRowCollection rows = table.Rows;


                var lrows = from lrow in new LINQList<DataRow>(rows)
                            where (string)lrow["Name"] == name
                            select lrow;

                if (lrows.Count() == 0)
                {
                    DataRow r = table.NewRow();
                    int id = Database.DB["Kernel"].NextAvailableID(table.Rows, "ID");
                    r["ID"] = id;
                    r["Name"] = name;
                    r["Description"] = description;
                    rows.Add(r);
                    Database.DB["Kernel"].UpdateDataTable(table);

                    Calendar cal = FindCalendar(id);

                    return cal;
                }
                throw new Exception("Calendar Already Exists");
            }
        }

        private T GetValue<T>(DataRow row, string columnname)
        {
            object res = null;
            if (row.RowState == DataRowState.Detached)
                return (T)res;
            if (typeof(T) == typeof(string))
                res = "";
            else if (typeof(T) == typeof(int))
                res = 0;
            else if (typeof(T) == typeof(double))
                res = 0.0;
            else if (typeof(T) == typeof(DateTime))
                res = DateTime.MinValue;
            else if (typeof(T) == typeof(bool))
                res = false;
            object obj = row[columnname];
            if (obj is DBNull)
                return (T)res;

            if (typeof(T) == typeof(int))
                return (T)(object)Convert.ToInt32(obj);
            return (T)obj;
        }

        private void LoadData(Calendar calendar)
        {
            lock (objLock)
            {
                if (_dateTables.ContainsKey(calendar.ID))
                {
                    DataTable table = _dateTables[calendar.ID];
                    DataRowCollection rows = table.Rows;

                    if (rows.Count == 0)
                        return;

                    int id_time = table.Columns["Timestamp"].Ordinal;
                    int id_index = table.Columns["BusinessDayIndex"].Ordinal;
                    int id_month = table.Columns["BusinessDayMonth"].Ordinal;
                    int id_year = table.Columns["BusinessDayYear"].Ordinal;

                    List<DateTime> _dateTimes = new List<DateTime>();
                    Dictionary<int, BusinessDay> _dateIndexDictionary = new Dictionary<int, BusinessDay>();
                    Dictionary<DateTime, BusinessDay> _dateTimeDictionary = new Dictionary<DateTime, BusinessDay>();

                    foreach (DataRow r in rows)
                    {
                        DateTime date = GetValue<DateTime>(r, "TimeStamp");
                        BusinessDay bday = new BusinessDay(date, GetValue<int>(r, "BusinessDayMonth"), GetValue<int>(r, "BusinessDayYear"), GetValue<int>(r, "BusinessDayIndex"), calendar.ID);
                        _dateTimeDictionary.Add(bday.DateTime, bday);
                        _dateIndexDictionary.Add(bday.DayIndex, bday);
                        _dateTimes.Add(date);
                    }

                    _dateTimes.Sort();

                    calendar.SetData(_dateTimes, _dateIndexDictionary, _dateTimeDictionary);
                }
            }
        }

        public Calendar FindCalendar(int id)
        {
            lock (objLock)
            {
                if (_calendarIdDB.ContainsKey(id))
                    return _calendarIdDB[id];

                string searchString = "ID = " + id;
                string targetString = null;
                DataTable mainTable = Database.DB["Kernel"].GetDataTable(_mainTableName, targetString, searchString);

                DataRowCollection rows = mainTable.Rows;
                if (rows.Count == 0)
                    throw new Exception("Calendar Doesn't Exists");

                DataRow row = rows[0];
                string name = GetValue<string>(row, "Name");
                string description = GetValue<string>(row, "Description");

                Calendar cal = new Calendar(id, name, description);
                _calendarIdDB.Add(id, cal);
                _calendarNameDB.Add(cal.Name, cal);
                _mainTables.Add(cal.ID, mainTable);

                searchString = "ID = " + cal.ID;
                targetString = null;
                DataTable dateTable = Database.DB["Kernel"].GetDataTable(_dateTableName, targetString, searchString);

                _dateTables.Add(cal.ID, dateTable);

                LoadData(cal);
                return cal;
            }
        }
        public Calendar FindCalendar(string name)
        {
            lock (objLock)
            {
                if (_calendarNameDB.ContainsKey(name))
                    return _calendarNameDB[name];

                string searchString = "Name LIKE '" + name + "'";
                string targetString = null;
                DataTable mainTable = Database.DB["Kernel"].GetDataTable(_mainTableName, targetString, searchString);

                DataRowCollection rows = mainTable.Rows;
                if (rows.Count == 0)
                    throw new Exception("Calendar Doesn't Exists");


                DataRow row = rows[0];
                int id = GetValue<int>(row, "ID");
                string description = GetValue<string>(row, "Description");

                Calendar cal = new Calendar(id, name, description);

                _calendarIdDB.Add(id, cal);
                _calendarNameDB.Add(cal.Name, cal);
                _mainTables.Add(cal.ID, mainTable);

                searchString = "ID = " + cal.ID;
                targetString = null;
                DataTable dateTable = Database.DB["Kernel"].GetDataTable(_dateTableName, targetString, searchString);

                _dateTables.Add(cal.ID, dateTable);

                LoadData(cal);
                return cal;
            }
        }

        public List<Calendar> Calendars()
        {
            string searchString = null;
            string targetString = null;
            DataTable table = Database.DB["Kernel"].GetDataTable(_mainTableName, targetString, searchString);

            DataRowCollection rows = table.Rows;
            if (rows.Count == 0)
                return null;
            else
            {
                List<Calendar> list = new List<Calendar>();
                foreach (DataRow row in rows)
                {
                    int id = (int)row["ID"];
                    list.Add(FindCalendar(id));
                }
                return list;
            }
        }
        public List<string> CalendarNames()
        {
            string searchString = null;
            string targetString = null;
            DataTable table = Database.DB["Kernel"].GetDataTable(_mainTableName, targetString, searchString);

            DataRowCollection rows = table.Rows;
            if (rows.Count == 0)
                return null;
            else
            {
                List<string> list = new List<string>();
                foreach (DataRow row in rows)
                    list.Add((string)row["Name"]);
                return list;
            }
        }

        public void SetProperty(Calendar calendar, string name, object value)
        {
            lock (objLock)
            {
                if (_mainTables.ContainsKey(calendar.ID))
                {
                    DataTable table = _mainTables[calendar.ID];

                    DataRowCollection rows = table.Rows;
                    if (rows.Count == 0)
                        return;

                    DataRow row = rows[0];
                    row[name] = value;
                    Database.DB["Kernel"].UpdateDataTable(table);
                }
            }
        }

        public void AddBusinessDay(BusinessDay bday)
        {
            lock (objLock)
            {
                if (_dateTables.ContainsKey(bday.CalendarID))
                {
                    DataTable table = _dateTables[bday.CalendarID];
                    DataRow r = table.NewRow();

                    r["ID"] = bday.CalendarID;
                    r["Timestamp"] = bday.DateTime;
                    r["BusinessDayMonth"] = bday.DayMonth;
                    r["BusinessDayYear"] = bday.DayYear;
                    r["BusinessDayIndex"] = bday.DayIndex;
                    table.Rows.Add(r);
                }
            }
        }

        public void Save(Calendar calendar)
        {
            lock (objLock)
            {
                if (_mainTables.ContainsKey(calendar.ID))
                {
                    DataTable table = _mainTables[calendar.ID];
                    Database.DB["Kernel"].UpdateDataTable(table);
                }

                if (_dateTables.ContainsKey(calendar.ID))
                {
                    DataTable table = _dateTables[calendar.ID];
                    Database.DB["Kernel"].UpdateDataTable(table);
                }
            }
        }

        public void Remove(Calendar calendar)
        {
            lock (objLock)
            {
                DataTable table = _mainTables[calendar.ID];

                DataRowCollection rows = table.Rows;
                if (rows.Count == 0)
                    return;

                DataRow row = rows[0];
                row.Delete();
                Database.DB["Kernel"].DeleteDataTable(table);


                table = _dateTables[calendar.ID];
                DataRowCollection rs = table.Rows;
                if (rs.Count != 0)
                {
                    foreach (DataRow r in rs)
                        r.Delete();

                    Database.DB["Kernel"].DeleteDataTable(table);
                }

                _calendarIdDB.Remove(calendar.ID);
                _calendarNameDB.Remove(calendar.Name);

                _dateTables.Remove(calendar.ID);
                _mainTables.Remove(calendar.ID);
            }
        }
    }
}
