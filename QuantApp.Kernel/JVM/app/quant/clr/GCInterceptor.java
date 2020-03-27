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


public class GCInterceptor
{
    private static WeakHashMap<Object, CallbackRef> _table = new WeakHashMap<Object, CallbackRef>();

    
    // public static void RegisterGCEvent(Object obj, int id, Action<object, int> action)
    public static void RegisterGCEvent(Object obj, int id)
    {
        
        
        if(!_table.containsKey(obj))
        {
            // System.out.println("JAVA RegisterGCEvent: " + obj);
            CallbackRef callbackRef = new CallbackRef(obj, id);
            _table.put(obj, callbackRef);
        }
        // else
        //     System.out.println("JAVA RegisterGCEvent already exists: " + obj + " " + id);
        // bool found = _table.TryGetValue(obj, out callbackRef);
        // if (found)
        // {
        //     callbackRef.Collected += action;
        //     return;
        // }

        // callbackRef = new CallbackRef(obj, id);
        // callbackRef.Collected += action;
        // _table.Add(obj, callbackRef);
    }

    // public static void DeregisterGCEvent(this object obj, int id, Action<object, int> action)
    // {
    //     CallbackRef callbackRef;
    //     bool found = _table.TryGetValue(obj, out callbackRef);
    //     if (!found)
    //         throw new Exception("No events registered");

    //     callbackRef.Collected -= action;
    // }
}