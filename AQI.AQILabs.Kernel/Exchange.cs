/*
 * The MIT License (MIT)
 * Copyright (c) 2007-2019, Arturo Rodriguez All rights reserved.
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;


namespace AQI.AQILabs.Kernel
{
    public class Exchange : IEquatable<Exchange>
    {
        public static AQILabs.Kernel.Factories.IExchangeFactory Factory = null;
        [Browsable(false)]

        public int ID { get; private set; }

        public Exchange(int id, string name, string description, int calendarID)
        {
            this.ID = id;
            this._name = name;
            this._description = description;
            this.CalendarID = calendarID;
        }

        public int CalendarID { get; set; }

        public bool Equals(Exchange x)
        {
            return ID == x.ID;
        }

        public override string ToString()
        {
            return Name;
        }
        private string _name = null;
        public string Name
        {
            get
            {
                return this._name;
            }
            set
            {
                this._name = value;
                Factory.SetProperty(this, "Name", value);
            }
        }
        private string _description = null;
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
        private Calendar _calendar = null;
        [Newtonsoft.Json.JsonIgnore]
        public Calendar Calendar
        {
            get
            {
                if (_calendar == null)
                    _calendar = Calendar.FindCalendar(CalendarID);

                return _calendar;
            }
            set
            {
                _calendar = value;
                CalendarID = value.ID;
                Factory.SetProperty(this, "CalendarID", value.ID);
            }
        }

        public static Exchange CreateExchange(string name, string description, Calendar calendar)
        {
            return Factory.CreateExchange(name, description, calendar);
        }
        public static Exchange FindExchange(string name)
        {
            return Factory.FindExchange(name);
        }
        public static Exchange FindExchange(int id)
        {
            return Factory.FindExchange(id);
        }

        /// <summary>
        /// Property: List of all exchanges in the system.
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public static List<Exchange> Exchanges
        {
            get
            {
                return Factory.Exchanges();
            }
        }
    }

}
