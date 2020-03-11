/*
 * The MIT License (MIT)
 * Copyright (c) Arturo Rodriguez All rights reserved.
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

 // NEED TO IMPLEMENT https://docs.oracle.com/javase/7/docs/api/java/util/WeakHashMap.html
 
 package app.quant.clr;

 import java.io.*;
 import java.lang.ref.WeakReference;
 import java.lang.reflect.*;
 import java.util.*;
 import java.util.concurrent.*;
 import java.util.function.*;
 
 import java.net.*;



public class CallbackRef implements AutoCloseable
{
    // private Object _obj;
    private int _id;

    // public event Action<object, int> Collected;

    public CallbackRef(Object obj, int id)
    {
        // if(!CLRRuntime.DB.containsKey(id))
            // System.out.println("JAVA CallbackRef: " + obj);
        // _obj = obj;
        _id = id;
    }

    @Override
    // protected void finalize() throws Throwable 
    public void close()
    {
        // System.out.println("JAVA GC Close CALLBACK: " + _id);
        CLRRuntime.nativeRemoveObject(_id);
        // Action<object, int> handle = Collected;
        // if (handle != null)
        //     handle(_obj, _id);
    }

    @Override
    protected void finalize() throws Throwable 
    // public void close()
    {
        // System.out.println("JAVA GC Finalize CALLBACK: " + _id);
        CLRRuntime.nativeRemoveObject(_id);
        // Action<object, int> handle = Collected;
        // if (handle != null)
        //     handle(_obj, _id);
    }
}
