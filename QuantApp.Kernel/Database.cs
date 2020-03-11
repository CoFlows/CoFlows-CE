/*
 * The MIT License (MIT)
 * Copyright (c) Arturo Rodriguez All rights reserved.
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using System.Data;
using System.Data.Common;


namespace QuantApp.Kernel
{
    public class Database
    {
        public static Dictionary<string, IDatabase> DB = new Dictionary<string, IDatabase>();

    }

    public interface IDatabase
    {
        void Initialize();

        void Close();

        int NextAvailableID(DataRowCollection Rows, string idName);

        DataTable ExecuteDataTable(string table, string command);

        DataTable GetDataTable(string table);

        DataTable GetDataTable(string table, string target, string search);

        DataTable UpdateDataTable(DataTable datatable, string target, string search);

        void UpdateDataTable(DataTable datatable);

        void AddDataTable(DataTable datatable);

        void DeleteDataTable(DataTable datatable);

        void ExecuteCommand(string command);

        DbDataReader ExecuteReader(string command);

        StreamReader ExecuteStreamReader(string command);

    }

    public class LINQList<T> : IEnumerable<T>, IEnumerable
    {
        IEnumerable items;

        public LINQList(IEnumerable items)
        {
            this.items = items;
        }

        #region IEnumerable<DataRow> Members
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            foreach (T item in items)
                yield return item;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            IEnumerable<T> ie = this;
            return ie.GetEnumerator();
        }
        #endregion
    }
}
