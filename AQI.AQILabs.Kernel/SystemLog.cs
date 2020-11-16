/*
 * The MIT License (MIT)
 * Copyright (c) 2007-2019, Arturo Rodriguez All rights reserved.
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections.Generic;

namespace AQI.AQILabs.Kernel
{

    public class SystemLogEntry
    {
        private string _id;
        private DateTime _date;
        private Instrument _instrument;
        private SystemLog.Type _type;
        private object _message;

        public SystemLogEntry(string id, DateTime date, Instrument instrument, SystemLog.Type type, object message)
        {
            _id = id;
            _date = date;
            _instrument = instrument;
            _type = type;
            _message = message;
        }

        public bool Equals(SystemLogEntry other)
        {
            if (((object)other) == null)
                return false;
            return ID == other.ID;
        }
        public override bool Equals(object other)
        {
            if (typeof(SystemLogEntry) != other.GetType())
                return false;

            return Equals((SystemLogEntry)other);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public string ID
        {
            get
            {
                return _id;
            }
        }
        public DateTime Date
        {
            get
            {
                return _date;
            }
        }
        public Instrument Instrument
        {
            get
            {
                return _instrument;
            }
        }
        public SystemLog.Type Type
        {
            get
            {
                return _type;
            }
        }
        public object Message
        {
            get
            {
                return _message;
            }
        }
        public override string ToString()
        {
            return "[" + Type + "]/" + Date.ToShortDateString() + "/(" + Instrument + "): " + Message;
        }
    }

    public class SystemLog
    {
        public static ISystemLog Adapter;

        public enum Type
        {
            Production = 1, Debug = 2, Development = 3, DataFeed = 4, User = 5
        };

        public static SystemLogEntry Write(DateTime date, Instrument instrument, Type type, object message)
        {
            if (Adapter == null) return null;
            return Adapter.Write(date, instrument, type, message == null ? "" : message.ToString());
        }

        public static void Write(object message)
        {
            if (Adapter == null) return;
            Adapter.Write(message == null ? "" : message.ToString());
        }

        public static ICollection<SystemLogEntry> Entries(DateTime start, DateTime end, Instrument instrument, SystemLog.Type? type)
        {
            if (Adapter == null) return null;
            return Adapter.Entries(start, end, instrument, type);
        }

        public static ICollection<SystemLogEntry> Entries()
        {
            if (Adapter == null) return null;
            return Adapter.Entries();
        }

        public static void RemoveEntry(SystemLogEntry entry)
        {
            if (Adapter == null) return;
            Adapter.RemoveEntry(entry);
        }
        public static void RemoveEntries(ICollection<SystemLogEntry> entries)
        {
            if (Adapter == null) return;
            Adapter.RemoveEntries(entries);
        }
    }

    public interface ISystemLog
    {
        SystemLogEntry Write(DateTime date, Instrument instrument, SystemLog.Type type, object message);
        void Write(string message);

        ICollection<SystemLogEntry> Entries(DateTime start, DateTime end, Instrument instrument, SystemLog.Type? type);
        ICollection<SystemLogEntry> Entries();

        void RemoveEntry(SystemLogEntry entry);
        void RemoveEntries(ICollection<SystemLogEntry> entry);
    }
}
