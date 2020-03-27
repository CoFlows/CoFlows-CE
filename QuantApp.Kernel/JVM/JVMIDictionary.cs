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

    public class JVMIDictionary : JVMObject, IDictionary<object, object>
    {
        private JVMObject _obj;
        private JVMObject jhashMap;
        // public JVMIDictionary(JVMObject obj):base(obj.Pointer, obj.JavaHashCode, obj.JavaClass)
        public JVMIDictionary(JVMObject obj):base(obj.JavaHashCode, obj.JavaClass, true, "JVMIDictionary")
        {
            this._obj = obj;
            this.jhashMap = this;
        }


        public void Add(object key, object value)
        {
            dynamic dyn = jhashMap;
            dyn.put(new ObjectWrapper(key), new ObjectWrapper(value));
        }

        public bool Remove(object key)
        {
            dynamic dyn = jhashMap;
            if(this.ContainsKey(key))
            {
                dyn.remove(new ObjectWrapper(key));
                return true;
            }
            return false;
        }

        public bool ContainsKey(object key)
        {
            dynamic dyn = jhashMap;
            return dyn.containsKey(new ObjectWrapper(key));
        }

        public bool TryGetValue(object key, out object value)
        {
            dynamic dyn = jhashMap;
            
            if(this.ContainsKey(key))
            {
                var wk = new ObjectWrapper(key);
                value = dyn.get(wk);
                return true;
            }
            value = null;
            return false;
        }

        public object this[object key]
        {
            get
            {
                dynamic dyn = jhashMap;
                if(this.ContainsKey(key))
                {
                    var wk = new ObjectWrapper(key);
                    return dyn.get(wk);
                }
                else
                    return null;
            }
            set
            {
                var wk = new ObjectWrapper(key);
                var wv = new ObjectWrapper(value);
                dynamic dyn = jhashMap;
                dyn.put(wk, wv);
            }
        }

        public ICollection<object> Keys 
        { 
            get
            {
                dynamic dyn = jhashMap;
                return dyn.keySet();
            } 
        }
        public ICollection<object> Values 
        { 
            get
            {
                dynamic dyn = jhashMap;
                return dyn.values();
            } 
        }

        public int Count 
        { 
            get
            {
                dynamic dyn = jhashMap;
                return dyn.size();
            }
        }

        public bool IsReadOnly { get { return false; } }

        public void Add(KeyValuePair<object, object> keyPair)
        {
            this.Add(keyPair.Key, keyPair.Value);
        }

        public void Clear()
        {
            dynamic dyn = jhashMap;
            dyn.clear();
        }

        public bool Contains(KeyValuePair<object, object> keyPair)
        {
            if(this.ContainsKey(keyPair.Key))
                return this[keyPair.Key].Equals(keyPair.Value);
            
            return false;
        }

        public void CopyTo(KeyValuePair<object,object>[] keyPairs, int index)
        {
        }

        public bool Remove(KeyValuePair<object,object> keyPair)
        {
            if(this.ContainsKey(keyPair.Key) && this[keyPair.Key].Equals(keyPair.Value))
                this.Remove(keyPair.Key);
            
            return false;
        }

        public IEnumerator<KeyValuePair<object, object>> GetEnumerator()
        {
            return new JVMDictionaryIEnumerator(this);
        }

        private IEnumerator<KeyValuePair<object, object>> _GetEnumerator()
        {
            return this.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator) _GetEnumerator();
        }

        public override string ToString()
        {
            return "JVMIDictionary - " + base.ToString();
        }
    }

    public class JVMDictionaryIEnumerator : IEnumerator<KeyValuePair<object, object>>
    {
        private JVMObject jenumerator;
        private JVMIDictionary jdic;

        public JVMDictionaryIEnumerator(JVMIDictionary jobj)
        {
            this.jdic = jobj;
            dynamic dyn = jobj;
            this.jenumerator = dyn.Keys.iterator();
        }

        private KeyValuePair<object, object> _current;
        private bool _init = false;

        public bool MoveNext()
        {
            this._init = true;
            dynamic jenum = this.jenumerator;
            if(jenum.hasNext())
            {
                object key = jenum.next();
                _current = new KeyValuePair<object, object>(key, this.jdic[key]);
                return true;
            }
            return false;
        }

        public void Reset()
        {
            dynamic jenum = this.jenumerator;
            while(jenum.hasNext()) 
            {
                object key = jenum.next();
                _current = new KeyValuePair<object, object>(key, this.jdic[key]);
            }
        }

        public KeyValuePair<object, object> Current
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
            get { return this.Current; }
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