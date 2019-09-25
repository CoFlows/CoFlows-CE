/*
 * The MIT License (MIT)
 * Copyright (c) 2007-2019, Arturo Rodriguez All rights reserved.
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
 
using System;
using System.ComponentModel;

namespace AQI.AQILabs.Kernel
{
    public class DataProvider : IEquatable<DataProvider>
    {
        public static AQI.AQILabs.Kernel.Factories.IDataProviderFactory Factory = null;
        [Browsable(false)]

        public int ID { get; private set; }

        public DataProvider(int id, string name, string description)
        {
            this.ID = id;
            this._name = name;
            this._description = description;
        }

        public bool Equals(DataProvider x)
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
                return this._description;
            }
            set
            {
                this._description = value;
                Factory.SetProperty(this, "Description", value);
            }
        }
        public static DataProvider CreateDataProvider(string name, string description)
        {
            return Factory.CreateDataProvider(name, description);
        }
        public static DataProvider FindDataProvider(string name)
        {
            return Factory.FindDataProvider(name);
        }
        public static DataProvider FindDataProvider(int id)
        {
            return Factory.FindDataProvider(id);
        }
        public static DataProvider DefaultProvider = null;
    }
}
