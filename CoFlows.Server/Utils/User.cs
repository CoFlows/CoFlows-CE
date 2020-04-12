/*
 * The MIT License (MIT)
 * Copyright (c) Arturo Rodriguez All rights reserved.
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using System.Data;
using QuantApp.Kernel;

namespace CoFlows.Server.Utils
{
    public class User
    {
        private DataTable _table = null;
        private DataRow _row = null;
        public User(DataTable table, DataRow row)
        {
            this._table = table;
            this._row = row;
        }

        protected object GetValue(DataRow row, string columnname, Type type)
        {
            object res = null;
            if (row.RowState == DataRowState.Detached)
                return res;
            if (type == typeof(string))
                res = "";
            else if (type == typeof(int))
                res = int.MinValue;
            else if (type == typeof(double))
                res = double.NaN;
            else if (type == typeof(DateTime))
                res = DateTime.MinValue;
            else if (type == typeof(bool))
                res = false;
            object obj = row[columnname];
            if (obj is DBNull)
                return res;
            return obj;
        }

        public string FirstName
        {
            get
            {
                return (string)GetValue(_row, "FirstName", typeof(string));
            }
            set
            {
                _row["FirstName"] = value;
                Database.DB["CloudApp"].UpdateDataTable(_table);
            }
        }

        public string LastName
        {
            get
            {
                return (string)GetValue(_row, "LastName", typeof(string));
            }
            set
            {
                _row["LastName"] = value;
                Database.DB["CloudApp"].UpdateDataTable(_table);
            }
        }

        public string IdentityProvider
        {
            get
            {
                return (string)GetValue(_row, "IdentityProvider", typeof(string));
            }
            set
            {
                _row["IdentityProvider"] = value;
                Database.DB["CloudApp"].UpdateDataTable(_table);
            }
        }

        public string NameIdentifier
        {
            get
            {
                return (string)GetValue(_row, "NameIdentifier", typeof(string));
            }
            set
            {
                _row["NameIdentifier"] = value;
                Database.DB["CloudApp"].UpdateDataTable(_table);
            }
        }

        public string Email
        {
            get
            {
                return (string)GetValue(_row, "Email", typeof(string));
            }
            set
            {
                _row["Email"] = value;
                Database.DB["CloudApp"].UpdateDataTable(_table);
            }
        }

        public string TenantName
        {
            get
            {
                return (string)GetValue(_row, "TenantName", typeof(string));
            }
            set
            {
                _row["TenantName"] = value;
                Database.DB["CloudApp"].UpdateDataTable(_table);
            }
        }

        public string Hash
        {
            get
            {
                return (string)GetValue(_row, "Hash", typeof(string));
            }
            set
            {
                _row["Hash"] = value;
                Database.DB["CloudApp"].UpdateDataTable(_table);
            }
        }

        public string Secret
        {
            get
            {
                return (string)GetValue(_row, "Secret", typeof(string));
            }
            set
            {
                _row["Secret"] = value;
                Database.DB["CloudApp"].UpdateDataTable(_table);
            }
        }
    }
}