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
using System.ComponentModel;

using AQI.AQILabs.Kernel.Numerics.Util;

namespace AQI.AQILabs.Kernel
{
    /// <summary>
    /// Enumeration of possible business day seach options.
    /// </summary>
    public enum BusinessDaySearchType
    {
        Previous = 1, Next = 2
    };

    /// <summary>
    /// Enumeration of possible day count conventions.
    /// </summary>
    public enum DayCountConvention
    {
        NotSet = -1, Act360 = 1, Act365 = 2, Thirty360 = 3, Thirty365 = 4, Actual = 5, Business = 6
    };

    /// <summary>
    /// Calendar class containing
    /// the functions managing the Calendar's collection of Business Days.
    /// This class also manages the connectivity
    /// to the database through a relevant Factories
    /// </summary>  
    public class Calendar : IEquatable<Calendar>
    {
        public static AQI.AQILabs.Kernel.Factories.ICalendarFactory Factory = null;

        private List<DateTime> _dateTimes = null;
        private Dictionary<int, BusinessDay> _dateIndexDictionary = new Dictionary<int, BusinessDay>();
        private Dictionary<DateTime, BusinessDay> _dateTimeDictionary = new Dictionary<DateTime, BusinessDay>();

        public static DateTime Close(DateTime date)
        {
            return date.Date.AddDays(1).AddMilliseconds(-10);
        }

        /// <summary>
        /// Property: contains int value for the unique ID of the calendar.
        /// </summary>
        /// <remarks>
        /// Main identifier for each Calendar in the System
        /// </remarks>
        public int ID { get; private set; }

        /// <summary>
        /// Constructor of the Calendar Class        
        /// </summary>
        /// <remarks>
        /// Only used Internaly by Kernel
        /// </remarks>
        public Calendar(int id, string name, string description)
        {
            this.ID = id;
            this._name = name;
            this._description = description;
        }

        /// <summary>
        /// Function: Set data structures representing the Calendar information.
        /// </summary>
        /// <param name="dateTimes">List of DateTimes of the calendar. 
        /// </param>
        /// <param name="dateIndexDictionary">Dictionary of integer index values of the dates used to manage business days.
        /// </param>
        /// <param name="dateTimeDictionary">Dictionary DateTime index values of the dates used to manage business days.
        /// </param>        
        public void SetData(List<DateTime> dateTimes, Dictionary<int, BusinessDay> dateIndexDictionary, Dictionary<DateTime, BusinessDay> dateTimeDictionary)
        {
            this._dateTimes = dateTimes;
            this._dateIndexDictionary = dateIndexDictionary;
            this._dateTimeDictionary = dateTimeDictionary;
        }

        /// <summary>
        /// Function: String representation of the calendar.
        /// </summary>
        public override string ToString()
        {
            return Name;
        }

        public bool Equals(Calendar other)
        {
            if ((object)other == null)
                return false;

            return ID == other.ID;
        }
        public override bool Equals(object other)
        {
            if (typeof(Calendar) == other.GetType())
                return Equals((Calendar)other);
            return false;
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator ==(Calendar x, Calendar y)
        {
            if ((object)x == null)
            {
                if ((object)y == null)
                    return true;
                else
                    return false;
            }

            return x.Equals(y);
        }
        public static bool operator !=(Calendar x, Calendar y)
        {
            if ((object)x == null)
            {
                if ((object)y != null)
                    return true;
                else
                    return false;
            }
            return !x.Equals(y);
        }

        /// <summary>
        /// Property: List of DateTimes contained in this calendar.
        /// </summary>
        public List<DateTime> DateTimes
        {
            get
            {
                return _dateTimes;
            }
            set
            {
                this._dateTimes = value;
            }
        }

        /// <summary>
        /// Property: Dictionary of integer valued indices ordering the business days contained in this calendar.
        /// </summary>
        public Dictionary<int, BusinessDay> DateIndexDictionary
        {
            get
            {
                return this._dateIndexDictionary;
            }
            set
            {
                this._dateIndexDictionary = value;
            }
        }

        /// <summary>
        /// Property: Dictionary of DateTime valued indices ordering the business days contained in this calendar.
        /// </summary>
        public Dictionary<DateTime, BusinessDay> DateTimeDictionary
        {
            get
            {
                return this._dateTimeDictionary;
            }
            set
            {
                this._dateTimeDictionary = value;
            }
        }

        private string _name;

        /// <summary>
        /// Property: Name of calendar.
        /// </summary>
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                this._name = value;
                Factory.SetProperty(this, "Name", value);
            }
        }
        private string _description;

        /// <summary>
        /// Property: Description of calendar.
        /// </summary>
        public string Description
        {
            get
            {
                return _description;
            }
            set
            {
                this._description = value;
                Factory.SetProperty(this, "Description", value);
            }
        }

        /// <summary>
        /// Function: List of instruments that are linked to this calendar.
        /// </summary>
        public List<Instrument> Instruments()
        {
            var ins = from i in Instrument.Instruments()
                      where i.Calendar == this
                      select i;
            return new List<Instrument>(ins);
        }

        /// <summary>
        /// Function: List of instruments that are linked to this calendar.
        /// </summary>
        public BusinessDay NextTradingBusinessDate(DateTime date)
        {
            BusinessDay orderDate_local = GetClosestBusinessDay(date, TimeSeries.DateSearchType.Next);

            return orderDate_local.AddMilliseconds(1);
        }

        /// <summary>
        /// Function: Return the business day representing a given DateTime.
        /// </summary>
        /// <param name="date">Date to be translated to a business day.
        /// </param>
        public BusinessDay GetBusinessDay(DateTime date)
        {
            if (_dateTimeDictionary.ContainsKey(date.Date))
            {
                BusinessDay temp = _dateTimeDictionary[date.Date];
                BusinessDay bday = new BusinessDay(temp.DateTime, temp.DayMonth, temp.DayYear, temp.DayIndex, temp.CalendarID);

                //bday.SetTime(date.Hour, date.Minute, date.Second, date.Millisecond);
                bday.SetTime(date.TimeOfDay);

                return bday;// _dateTimeDictionary[date.Date].SetTime(date.Hour, date.Minute, date.Second, date.Millisecond);
            }
            else
                return null;
        }

        /// <summary>
        /// Function: Return the business day with a given index.
        /// </summary>
        /// <param name="idx">Index of the business date to be retrieved.
        /// </param>
        public BusinessDay GetBusinessDay(int idx)
        {
            if (_dateIndexDictionary.ContainsKey(idx))
            {
                BusinessDay bday = _dateIndexDictionary[idx];
                return new BusinessDay(bday.DateTime, bday.DayMonth, bday.DayYear, bday.DayIndex, bday.CalendarID);
            }
            else
                return null;
        }

        /// <summary>
        /// Function: Return the index of a given business day.
        /// </summary>
        /// <param name="date">Business day to retreive the index of.
        /// </param>
        public int GetBusinessDay(BusinessDay date)
        {
            return GetBusinessDay(date.DateTime).DayIndex;
        }

        /// <summary>
        /// Function: Return the business day closest to a given date.
        /// </summary>
        /// <param name="date">Reference date.
        /// </param>
        /// <param name="type">Type of search.
        /// </param>
        public BusinessDay GetClosestBusinessDay(DateTime date, TimeSeries.DateSearchType type)
        {
            BusinessDay res = GetBusinessDay(date);
            if (res != null)
            {
                BusinessDay ret = new BusinessDay(res.DateTime, res.DayMonth, res.DayYear, res.DayIndex, res.CalendarID);
                return ret;
            }
            if (type == TimeSeries.DateSearchType.Previous)
            {
                DateTime firstDate = _dateTimes[0];
                DateTime lastDate = _dateTimes[_dateTimes.Count - 1];

                if (date >= lastDate)
                {
                    BusinessDay temp = GetBusinessDay(lastDate);
                    BusinessDay bday = new BusinessDay(temp.DateTime, temp.DayMonth, temp.DayYear, temp.DayIndex, temp.CalendarID);

                    res.SetTime(date.TimeOfDay);

                    return res;
                }

                for (DateTime d = date; d >= firstDate; d = d.AddDays(-1))
                {
                    res = GetBusinessDay(d);
                    if (res != null)
                    {
                        BusinessDay bday = new BusinessDay(res.DateTime, res.DayMonth, res.DayYear, res.DayIndex, res.CalendarID);

                        bday.SetTime(date.TimeOfDay);

                        return bday;
                    }
                }
            }
            else
            {
                DateTime lastDate = _dateTimes[_dateTimes.Count - 1];

                for (DateTime d = date; d <= lastDate; d = d.AddDays(1))
                {
                    res = GetBusinessDay(d);
                    if (res != null)
                    {
                        BusinessDay bday = new BusinessDay(res.DateTime, res.DayMonth, res.DayYear, res.DayIndex, res.CalendarID);

                        bday.SetTime(date.TimeOfDay);

                        return bday;
                    }
                }
            }

            return null;
        }


        /// <summary>
        /// Function: Add business day to the calendar.
        /// </summary>
        /// <param name="date">Reference date.
        /// </param>
        /// <param name="businessDayMonth">Index of the date in the date's calendar month.
        /// </param>        
        /// <param name="businessDayYear">Index of the date in the date's calendar year.
        /// </param>        
        /// <param name="businessDayIndex">Index of the date's in the entire calendar.
        /// </param>                
        public void AddBusinessDay(DateTime date, int businessDayMonth, int businessDayYear, int businessDayIndex)
        {
            if (GetBusinessDay(date) != null)
                throw new Exception("Date Already Exists");

            BusinessDay bday = new BusinessDay(date, businessDayMonth, businessDayYear, businessDayIndex, this.ID);
            _dateIndexDictionary.Add(bday.DayIndex, bday);
            _dateTimeDictionary.Add(bday.DateTime, bday);

            Factory.AddBusinessDay(bday);
        }

        /// <summary>
        /// Function: Commit business days in storage.
        /// </summary>
        public void Save()
        {
            Factory.Save(this);
        }

        /// <summary>
        /// Function: Remove calendar from memory and storage.
        /// </summary>
        public void Remove()
        {
            Factory.Remove(this);
        }


        /// <summary>
        /// Function: Create calendar.
        /// </summary>
        /// <param name="name">Name of calendar.
        /// </param>
        /// <param name="description">Description of calendar.
        /// </param>
        public static Calendar CreateCalendar(string name, string description)
        {
            return Factory.CreateCalendar(name, description);
        }

        /// <summary>
        /// Function: Find a calendar with a given name in memory or persistent storage.
        /// </summary>
        /// <param name="name">Name to be searched.
        /// </param>
        public static Calendar FindCalendar(string name)
        {
            return Factory.FindCalendar(name);
        }

        /// <summary>
        /// Function: Find a calendar with a given ID in memory or persistent storage.
        /// </summary>
        /// <param name="id">ID to be searched.
        /// </param>
        public static Calendar FindCalendar(int id)
        {
            return Factory.FindCalendar(id);
        }

        /// <summary>
        /// Property: List of all calendars in the system.
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public static List<Calendar> Calendars
        {
            get
            {
                return Factory.Calendars();
            }
        }

        /// <summary>
        /// Property: List of all calendar names in the system.
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public static List<string> CalendarNames
        {
            get
            {
                return Factory.CalendarNames();
            }
        }
    }

    /// <summary>
    /// Business day class containing
    /// the most general functions and variables.
    /// </summary>
    public class BusinessDay : IEquatable<BusinessDay>
    {
        [Browsable(false)]

        public bool Equals(BusinessDay other)
        {
            if (((object)other) == null)
                return false;
            return DateTime == other.DateTime;
        }
        public override bool Equals(object other)
        {
            try { return Equals((BusinessDay)other); }
            catch { return false; }
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator ==(BusinessDay x, BusinessDay y)
        {
            if (((object)x) == null && ((object)y) == null)
                return true;
            else if (((object)x) == null)
                return false;

            return x.Equals(y);
        }
        public static bool operator !=(BusinessDay x, BusinessDay y)
        {
            return !(x == y);
        }

        /// <summary>
        /// Constructor of the BusinessDay Class        
        /// </summary>
        /// <remarks>
        /// Only used Internaly by Kernel
        /// </remarks>
        public BusinessDay(DateTime date, int DayMonth, int DayYear, int DayIndex, int calendarID)
        {
            this.DateTime = date;
            this.DayMonth = DayMonth;
            this.DayYear = DayYear;
            this.DayIndex = DayIndex;
            this.CalendarID = calendarID;
        }

        /// <summary>
        /// Function: String representation of the Instrument.
        /// </summary>
        public override string ToString()
        {
            return DateTime.ToString();
        }

        /// <summary>
        /// Property: DateTime representation of the business day.
        /// </summary>
        public DateTime DateTime { get; set; }

        /// <summary>
        /// Property: DateTime representation of the Close of the business day.
        /// </summary>
        public DateTime Close
        {
            get
            {
                return DateTime.Date.AddDays(1).AddMilliseconds(-10);
            }
        }

        /// <summary>
        /// Property: BusinessDay representation of the Close of the business day.
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public BusinessDay CloseBusinessDay
        {
            get
            {
                return new BusinessDay(Close, DayMonth, DayYear, DayIndex, CalendarID);
            }
        }

        public void SetTime(TimeSpan span)
        {
            DateTime = DateTime.Date + span;
        }



        /// <summary>
        /// Property: Index of the date in the date's calendar month.
        /// </summary>
        public int DayMonth { get; set; }

        /// <summary>
        /// Property: Index of the date in the date's year month.
        /// </summary>
        public int DayYear { get; set; }

        /// <summary>
        /// Property: Index of the date in calendar.
        /// </summary>
        public int DayIndex { get; set; }

        /// <summary>
        /// Property: ID of the calendar host of this business day.
        /// </summary>
        public int CalendarID { get; set; }

        /// <summary>
        /// Function: Returns the business day that is NUM milliseconds away.
        /// </summary>
        /// <param name="num">Number of seconds away.
        /// </param>
        public BusinessDay AddMilliseconds(int num)
        {
            DateTime t = DateTime.AddMilliseconds(num);
            if (t.Date == DateTime.Date)
            {
                BusinessDay bday = new BusinessDay(t, DayMonth, DayYear, DayIndex, CalendarID);
                return bday;
            }
            else
            {
                BusinessDay bday1 = Calendar.GetBusinessDay(t);
                if (bday1 != null)
                {
                    BusinessDay bday = new BusinessDay(t, bday1.DayMonth, bday1.DayYear, bday1.DayIndex, bday1.CalendarID);

                    bday.SetTime(t.TimeOfDay);
                    return bday;
                }
                else
                {
                    bday1 = Calendar.GetClosestBusinessDay(t, TimeSeries.DateSearchType.Next);
                    BusinessDay bday = new BusinessDay(bday1.DateTime, bday1.DayMonth, bday1.DayYear, bday1.DayIndex, bday1.CalendarID);

                    bday.SetTime(new TimeSpan(0));
                    return bday;
                }
            }
        }

        /// <summary>
        /// Function: Returns the business day that is NUM seconds away.
        /// </summary>
        /// <param name="num">Number of seconds away.
        /// </param>
        public BusinessDay AddSeconds(int num)
        {
            DateTime t = DateTime.AddSeconds(num);
            if (t.Date == DateTime.Date)
            {
                BusinessDay bday = new BusinessDay(t, DayMonth, DayYear, DayIndex, CalendarID);
                return bday;
            }
            else
            {
                BusinessDay bday1 = Calendar.GetBusinessDay(t);
                if (bday1 != null)
                {
                    BusinessDay bday = new BusinessDay(t, bday1.DayMonth, bday1.DayYear, bday1.DayIndex, bday1.CalendarID);

                    bday.SetTime(t.TimeOfDay);
                    return bday;
                }
                else
                {
                    bday1 = Calendar.GetClosestBusinessDay(t, TimeSeries.DateSearchType.Next);
                    BusinessDay bday = new BusinessDay(bday1.DateTime, bday1.DayMonth, bday1.DayYear, bday1.DayIndex, bday1.CalendarID);

                    bday.SetTime(new TimeSpan(0));
                    return bday;
                }
            }
        }

        /// <summary>
        /// Function: Returns the business day that is NUM minutes away.
        /// </summary>
        /// <param name="num">Number of minutes away.
        /// </param>
        public BusinessDay AddMinutes(int num)
        {
            DateTime t = DateTime.AddMinutes(num);
            if (t.Date == DateTime.Date)
            {
                BusinessDay bday = new BusinessDay(t, DayMonth, DayYear, DayIndex, CalendarID);
                return bday;
            }
            else
            {
                BusinessDay bday1 = Calendar.GetBusinessDay(t);
                if (bday1 != null)
                {
                    BusinessDay bday = new BusinessDay(t, bday1.DayMonth, bday1.DayYear, bday1.DayIndex, bday1.CalendarID);

                    bday.SetTime(t.TimeOfDay);
                    return bday;
                }
                else
                {
                    bday1 = Calendar.GetClosestBusinessDay(t, TimeSeries.DateSearchType.Next);
                    BusinessDay bday = new BusinessDay(bday1.DateTime, bday1.DayMonth, bday1.DayYear, bday1.DayIndex, bday1.CalendarID);

                    bday.SetTime(new TimeSpan(0));
                    return bday;
                }
            }
        }

        /// <summary>
        /// Function: Returns the business day that is NUM hours away.
        /// </summary>
        /// <param name="num">Number of hours away.
        /// </param>
        public BusinessDay AddHours(int num)
        {
            DateTime t = DateTime.AddHours(num);
            if (t.Date == DateTime.Date)
            {
                BusinessDay bday = new BusinessDay(t, DayMonth, DayYear, DayIndex, CalendarID);
                return bday;
            }
            else
            {
                BusinessDay bday1 = Calendar.GetBusinessDay(t);
                if (bday1 != null)
                {
                    BusinessDay bday = new BusinessDay(t, bday1.DayMonth, bday1.DayYear, bday1.DayIndex, bday1.CalendarID);

                    bday.SetTime(t.TimeOfDay);
                    return bday;
                }
                else
                {
                    bday1 = Calendar.GetClosestBusinessDay(t, TimeSeries.DateSearchType.Next);
                    BusinessDay bday = new BusinessDay(bday1.DateTime, bday1.DayMonth, bday1.DayYear, bday1.DayIndex, bday1.CalendarID);

                    bday.SetTime(new TimeSpan(0));
                    return bday;
                }
            }
        }

        /// <summary>
        /// Function: Returns the business day that is NUM business days away.
        /// </summary>
        /// <param name="num">Number of business days away.
        /// </param>
        public BusinessDay AddBusinessDays(int num)
        {
            BusinessDay bday = Calendar.GetBusinessDay(DayIndex + num);
            bday.SetTime(DateTime.TimeOfDay);

            return bday;
        }

        /// <summary>
        /// Function: Returns the closest business day that is NUM calendar days away.
        /// </summary>
        /// <param name="num">Number of business days away.
        /// </param>
        /// <param name="type">Type of search.
        /// </param>
        public BusinessDay AddActualDays(int num, TimeSeries.DateSearchType type)
        {
            BusinessDay bday = Calendar.GetClosestBusinessDay(DateTime.AddDays(num), type);
            bday.SetTime(DateTime.TimeOfDay);

            return bday;
        }

        /// <summary>
        /// Function: Returns the closest business day that is NUM months away.
        /// </summary>
        /// <param name="num">Number of months away.
        /// </param>
        /// <param name="type">Type of search.
        /// </param>
        public BusinessDay AddMonths(int num, TimeSeries.DateSearchType type)
        {
            DateTime newDate = this.DateTime.AddMonths(num);

            BusinessDay bday = Calendar.GetClosestBusinessDay(newDate, type);
            bday.SetTime(DateTime.TimeOfDay);

            return bday;
        }

        /// <summary>
        /// Function: Returns the closest business day that is NUM years away.
        /// </summary>
        /// <param name="num">Number of years away.
        /// </param>
        /// <param name="type">Type of search.
        /// </param>
        public BusinessDay AddYears(int num, TimeSeries.DateSearchType type)
        {
            DateTime newDate = this.DateTime.AddYears(num);

            BusinessDay bday = Calendar.GetClosestBusinessDay(newDate, type);
            bday.SetTime(DateTime.TimeOfDay);

            return bday;
        }


        /// <summary>
        /// Function: Number of days between this business day and a given business day.
        /// </summary>
        /// <param name="day">Business day.
        /// </param>
        public int BusinessDaysBetween(BusinessDay day)
        {
            return Math.Abs(DayIndex - Calendar.GetBusinessDay(day.DateTime).DayIndex);
        }

        /// <summary>
        /// Property: Calendar host of this business day.
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public Calendar Calendar
        {
            get
            {
                return Calendar.FindCalendar(CalendarID);
            }
        }

        /// <summary>
        /// Function: Number of years between this business day and a given business day where day count convention has custom base.
        /// </summary>
        /// <param name="dayCount">Number of business days away.
        /// </param>
        /// <param name="baseCount">custom divisor in calculations of day count convention. Usually 252, 260, 360 or 365.
        /// </param>
        public double YearsBetween(BusinessDay day, DayCountConvention dayCount, double baseCount)
        {
            DateTime d1 = (this.DateTime < day.DateTime ? this.DateTime : day.DateTime);
            DateTime d2 = (d1 == this.DateTime ? day.DateTime : this.DateTime);

            double t = -1;
            if (dayCount == DayCountConvention.Act360)
                t = (d2.Date - d1.Date).TotalDays / baseCount;
            else if (dayCount == DayCountConvention.Act365)
                t = (d2.Date - d1.Date).TotalDays / baseCount;
            else if (dayCount == DayCountConvention.Thirty360)
                t = (Math.Max(0, 30 - d1.Day) + Math.Min(30, d2.Day) + 360 * (d2.Year - d1.Year) + 30 * (d2.Month - d1.Month - 1)) / baseCount;
            else if (dayCount == DayCountConvention.Thirty365)
                t = (Math.Max(0, 30 - d1.Day) + Math.Min(30, d2.Day) + 360 * (d2.Year - d1.Year) + 30 * (d2.Month - d1.Month - 1)) / baseCount;

            else if (dayCount == DayCountConvention.Actual)
                t = (d2.Date - d1.Date).TotalDays / baseCount;

            else if (dayCount == DayCountConvention.Actual)
                t = this.BusinessDaysBetween(day) / baseCount;

            return t;
        }

        /// <summary>
        /// Function: Number of years between this business day and a given business day.
        /// </summary>
        /// <param name="dayCount">Number of business days away.
        /// </param>
        public double YearsBetween(BusinessDay day, DayCountConvention dayCount)
        {
            double t = -1;
            if (dayCount == DayCountConvention.Act360)
                return YearsBetween(day, dayCount, 360.0);
            else if (dayCount == DayCountConvention.Act365)
                return YearsBetween(day, dayCount, 365.0);
            else if (dayCount == DayCountConvention.Thirty360)
                return YearsBetween(day, dayCount, 360.0);
            else if (dayCount == DayCountConvention.Thirty365)
                return YearsBetween(day, dayCount, 365.0);

            return t;
        }
    }
}

