/*
 * The MIT License (MIT)
 * Copyright (c) 2007-2019, Arturo Rodriguez All rights reserved.
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
 
using System;
using System.Data;

using QuantApp.Kernel;

namespace AQI.AQILabs.Kernel
{
    public delegate void DataUpdatedEvent(DateTime date);

    public class DataFeed
    {
        static string _mainTableName = "DataFeed";

        public static IDataFeed Adapter;

        public static event DataUpdatedEvent DataUpdated;

        public static void DataUpdatedTrigger(DateTime date)
        {
            if (DataUpdated != null)
                DataUpdated(date);
        }

        public static DateTime LastUpdate()
        {
            string searchString = string.Format(" ORDER BY Timestamp DESC");
            string targetString = "TOP 1 *";

            DataTable table = Database.DB["Kernel"].GetDataTable(_mainTableName + searchString, targetString, null);

            DataRowCollection rows = table.Rows;
            if (rows.Count == 0)
                return DateTime.MinValue;
            else
                return (DateTime)rows[0]["Timestamp"];
        }

        public static void AddUpdateTime(DateTime date)
        {
            string searchString = null;
            string targetString = null;
            DataTable table = Database.DB["Kernel"].GetDataTable(_mainTableName, targetString, searchString);
            DataRowCollection rows = table.Rows;

            DataRow r = table.NewRow();
            r["Timestamp"] = date;
            rows.Add(r);
            Database.DB["Kernel"].UpdateDataTable(table);
        }

        public static void UpdateData()
        {
            Adapter.UpdateData(LastUpdate());
        }
    }


    public interface IDataFeed
    {
        void UpdateData(DateTime date);
    }
}
