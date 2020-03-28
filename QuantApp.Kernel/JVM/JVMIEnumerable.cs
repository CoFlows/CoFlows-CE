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

namespace QuantApp.Kernel.JVM
{
    
    public class JVMIEnumerable : JVMObject, IEnumerable<object>
    {
        private JVMObject _obj;
        // public JVMIEnumerable(JVMObject obj):base(obj.JavaHashCode, obj.JavaClass, true)
        public JVMIEnumerable(JVMObject obj):base(obj.JavaHashCode, obj.JavaClass, true, "JVMIEnumerable")
        {
            this._obj = obj;
        }

        public IEnumerator<object> GetEnumerator()
        {
            return new JVMIEnumerator(this);
        }

        private IEnumerator<object> _GetEnumerator()
        {
            return this.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator) _GetEnumerator();
        }

        public override string ToString()
        {
            return "JVMIEnumerable - " + base.ToString();
        }
    }

    public class JVMIEnumerator : IEnumerator<object>
    {
        private JVMObject jenumerator;
        private JVMObject jobj;

        public JVMIEnumerator(JVMObject jobj)
        {
            this.jobj = jobj;
            dynamic dyn = jobj;
            this.jenumerator = dyn.iterator();
        }

        private object _current;
        private bool _init = false;

        public bool MoveNext()
        {
            this._init = true;
            dynamic jenum = this.jenumerator;
            if(jenum.hasNext())
            {
                _current = jenum.next();
                return true;
            }
            return false;
        }

        public void Reset()
        {
            dynamic jenum = this.jenumerator;
            while(jenum.hasNext()) 
               _current = jenum.next();
        }

        public object Current
        {
            get
            {
                if(!this._init)
                    this.MoveNext();

                return _current;
            }
        }

        object IEnumerator.Current
        {
            get 
            { 
                return this.Current; 
            }
        }

        private bool disposedValue = false;
        public void Dispose()
        {
            Dispose(true);
            // GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    // Dispose of managed resources.
                }
                // _current = null;
                // if (_sr != null) {
                // _sr.Close();
                // _sr.Dispose();
                // }
            }

            this.disposedValue = true;
        }
    }
}