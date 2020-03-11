/*
 * The MIT License (MIT)
 * Copyright (c) Arturo Rodriguez All rights reserved.
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

package app.quant.clr;

import java.lang.ref.WeakReference;
import java.util.*;
import java.util.concurrent.*;

public class CLRObject
{
    public static ConcurrentHashMap<Integer, CLRObject> __DB = new ConcurrentHashMap<Integer, CLRObject>();
    public static ConcurrentHashMap<Integer, WeakReference> DB = new ConcurrentHashMap<Integer, WeakReference>();

    public int Pointer;
    public String ClassName;
    Boolean __cache = false;

    public CLRObject(String classname, int ptr, boolean cache)
    {
        this.Pointer = ptr;
        this.ClassName = classname;
        // if(cache)
            // __DB.put(ptr, this);
        DB.put(ptr, new WeakReference(this));
        //GCInterceptor.RegisterGCEvent(this, ptr); //TESTING
    }

    public CLRObject(String classname, int ptr)
    {
        this.Pointer = ptr;
        this.ClassName = classname;
        // if(cache)
        __DB.put(ptr, this);
        __cache = true;
        // System.out.println("CLR OBJECT: " + classname + "  --> " + ptr);
        DB.put(ptr, new WeakReference(this));
    }

    public synchronized Object Invoke(String funcname, Object... args)
    {
        return CLRRuntime.Invoke(Pointer, funcname, args);
    }

    public synchronized Object InvokeArr(String funcname, Object[] args)
    {
        return CLRRuntime.Invoke(Pointer, funcname, args);
    }

    public synchronized Object GetProperty(String name)
    {
        return CLRRuntime.GetProperty(Pointer, name);
    }

    public synchronized void SetProperty(String name, Object value)
    {
        CLRRuntime.SetProperty(Pointer, name, value);
    }

    @Override
    public int hashCode() 
    {
		return Pointer;
    }

    @Override
    public String toString() 
    {
		return "CLRObject: " + ClassName;// + " ( Ptr = " + Pointer + " | jhash = " + this.hashCode() + ")";
    }
    
    @Override
    protected void finalize() throws Throwable 
    {
        // if(__cache)
            // System.out.println("JAVA FINALISE(" + Pointer + "): " + this + " " + CLRRuntime.__DB.size());
        if(__DB.containsKey(Pointer))
            __DB.remove(Pointer);
        CLRRuntime.nativeRemoveObject(Pointer);
        super.finalize();
    }

}
