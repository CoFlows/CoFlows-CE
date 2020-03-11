
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
using System.Dynamic;
using System.Reflection;

using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace QuantApp.Kernel.JVM
{
    public interface IJVMTuple
    {
        [Newtonsoft.Json. JsonIgnore]
        JVMObject JVMObject { get; }
    }

    public class JVMTuple : JVMObject
    {
        internal JVMObject jVMObject;
        internal IJVMTuple jVMTuple;
        // public JVMTuple(JVMObject obj, IJVMTuple tuple):base(obj.Pointer, obj.JavaHashCode, obj.JavaClass)
        public JVMTuple(JVMObject obj, IJVMTuple tuple):base(obj.JavaHashCode, obj.JavaClass, true, "JVMTuple")
        {
            this.jVMObject = obj;
            this.jVMTuple = tuple;
            JVMObject.DB[obj.JavaHashCode] = new WeakReference(this);
        }

        public override int GetHashCode()
        {
            return this.JavaHashCode;
        }

        public override string ToString()
        {
            return "JVMTuple - " + base.ToString();
        }

        public override bool Equals(object obj)
        {
            return this.JavaHashCode == obj.GetHashCode();
        }

        public string ItemX { get { return "TEST"; }}
    }

    public class JVMTuple1 : Tuple<object>, IJVMTuple
    {
        private JVMObject jVMObject;
        public JVMTuple1(JVMObject obj, object item1) : base(item1) { jVMObject = obj; }

        public JVMObject JVMObject { get { return jVMObject; } }

        public override string ToString()
        {
            return "JVMTuple - " + jVMObject.ToString();
        }
    }

    public class JVMTuple2 : Tuple<object, object>, IJVMTuple
    {
        private JVMObject jVMObject;
        public JVMTuple2(JVMObject obj, object item1, object item2) : base(item1, item2) { jVMObject = obj; }

        public JVMObject JVMObject { get { return jVMObject; } }

        public override string ToString()
        {
            return "JVMTuple - " + jVMObject.ToString();
        }
    }

    public class JVMTuple3 : Tuple<object, object, object>, IJVMTuple
    {
        private JVMObject jVMObject;
        public JVMTuple3(JVMObject obj, object item1, object item2, object item3) : base(item1, item2, item3) { jVMObject = obj; }

        public JVMObject JVMObject { get { return jVMObject; } }

        public override string ToString()
        {
            return "JVMTuple - " + jVMObject.ToString();
        }
    }

    public class JVMTuple4 : Tuple<object, object, object, object>, IJVMTuple
    {
        private JVMObject jVMObject;
        public JVMTuple4(JVMObject obj, object item1, object item2, object item3, object item4) : base(item1, item2, item3, item4) { jVMObject = obj; }

        public JVMObject JVMObject { get { return jVMObject; } }

        public override string ToString()
        {
            return "JVMTuple - " + jVMObject.ToString();
        }
    }

    public class JVMTuple5 : Tuple<object, object, object, object, object>, IJVMTuple
    {
        private JVMObject jVMObject;
        public JVMTuple5(JVMObject obj, object item1, object item2, object item3, object item4, object item5) : base(item1, item2, item3, item4, item5) { jVMObject = obj; }

        public JVMObject JVMObject { get { return jVMObject; } }

        public override string ToString()
        {
            return "JVMTuple - " + jVMObject.ToString();
        }
    }

    public class JVMTuple6 : Tuple<object, object, object, object, object, object>, IJVMTuple
    {
        private JVMObject jVMObject;
        public JVMTuple6(JVMObject obj, object item1, object item2, object item3, object item4, object item5, object item6) : base(item1, item2, item3, item4, item5, item6) { jVMObject = obj; }

        public JVMObject JVMObject { get { return jVMObject; } }

        public override string ToString()
        {
            return "JVMTuple - " + jVMObject.ToString();
        }
    }

    public class JVMTuple7 : Tuple<object, object, object, object, object, object, object>, IJVMTuple
    {
        private JVMObject jVMObject;
        public JVMTuple7(JVMObject obj, object item1, object item2, object item3, object item4, object item5, object item6, object item7) : base(item1, item2, item3, item4, item5, item6, item7) { jVMObject = obj; }

        public JVMObject JVMObject { get { return jVMObject; } }

        public override string ToString()
        {
            return "JVMTuple - " + jVMObject.ToString();
        }
    }

    public class JVMTuple8 : Tuple<object, object, object, object, object, object, object, object>, IJVMTuple
    {
        private JVMObject jVMObject;
        public JVMTuple8(JVMObject obj, object item1, object item2, object item3, object item4, object item5, object item6, object item7, object item8) : base(item1, item2, item3, item4, item5, item6, item7, item8) { jVMObject = obj; }

        public JVMObject JVMObject { get { return jVMObject; } }

        public override string ToString()
        {
            return "JVMTuple - " + jVMObject.ToString();
        }
    }
}