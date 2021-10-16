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
    private int _id;
    public CallbackRef(Object obj, int id)
    {
        _id = id;
    }

    @Override
    public void close()
    {
        CLRRuntime.nativeRemoveObject(_id);
    }

    @Override
    protected void finalize() throws Throwable 
    // public void close()
    {        
        CLRRuntime.nativeRemoveObject(_id);

        if(CLRObject.__DB.containsKey(_id))
        {
            System.out.println("JAVA GC Finalize CALLBACK: " + _id);
            CLRObject.__DB.remove(_id);
        }

        if(CLRRuntime._DBID.containsKey(_id))
            CLRRuntime._DBID.remove(_id);

        if(CLRRuntime._SDBID.containsKey(_id))
            CLRRuntime._SDBID.remove(_id);
    }
}
