/*
 * The MIT License (MIT)
 * Copyright (c) 2007-2019, Arturo Rodriguez All rights reserved.
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
 
using System.Collections;
using System.Collections.Generic;

namespace JVM
{
    public class JVMICollection : JVMObject, ICollection<object>
    {
        public JVMICollection(JVMObject obj):base(obj.Pointer, obj.JavaHashCode, obj.JavaClass){}
        public void CopyTo(object[] keyPairs, int index)
        {
        }

        public bool Contains(object obj){return true;}

        public bool Remove(object obj){ return true; }
        public bool IsReadOnly { get; }
        public void Clear(){}
        public void Add(object obj){}
        public int Count{ get; }

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
            return "JVMICollection - " + base.ToString();
        }
    }
}