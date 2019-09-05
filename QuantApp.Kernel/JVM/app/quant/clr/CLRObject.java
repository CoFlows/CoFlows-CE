/*
 * The MIT License (MIT)
 * Copyright (c) 2007-2019, Arturo Rodriguez All rights reserved.
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

package app.quant.clr;

import java.util.*;

public class CLRObject
{
    public static Map<Integer, CLRObject> DB = new HashMap<Integer, CLRObject>();

    public int Pointer;
    public String ClassName;

    public CLRObject(String classname, int ptr)
    {
        this.Pointer = ptr;
        this.ClassName = classname;
        if(!DB.containsKey(ptr))
            DB.put(ptr, this);
    }

    public Object Invoke(String funcname, Object... args)
    {
        return CLRRuntime.Invoke(Pointer, funcname, args);
    }

    public Object InvokeArr(String funcname, Object[] args)
    {
        return CLRRuntime.Invoke(Pointer, funcname, args);
    }

    public Object GetProperty(String name)
    {
        return CLRRuntime.GetProperty(Pointer, name);
    }

    public void SetProperty(String name, Object value)
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

}
