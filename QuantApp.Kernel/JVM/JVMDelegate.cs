/*
 * The MIT License (MIT)
 * Copyright (c) Arturo Rodriguez All rights reserved.
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
 
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Dynamic;
using System.Reflection;

using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace QuantApp.Kernel.JVM
{
    public class JVMDelegate
    {
        // public static ConcurrentDictionary<int, JVMDelegate> DB = new ConcurrentDictionary<int, JVMDelegate>();
        public static ConcurrentDictionary<int, WeakReference> DB = new ConcurrentDictionary<int, WeakReference>();
        public int Pointer;
        public Delegate func;

        public string ClassName;
        private readonly object objLock_JVMDelegate = new object();
        public JVMDelegate(string classname, int pointer)
        {
            // lock(objLock_JVMDelegate)
            {
                this.Pointer = pointer;

                Type ct = null;
                Assembly asm = System.Reflection.Assembly.GetEntryAssembly();
                ct = asm.GetType(classname);

                this.ClassName = classname;

                if(ct == null)
                {
                    asm = System.Reflection.Assembly.GetExecutingAssembly();
                    ct = asm.GetType(classname);
                }

                if(ct == null)
                {
                    asm = System.Reflection.Assembly.GetCallingAssembly();
                    ct = asm.GetType(classname);
                }

                foreach(AssemblyName assemblyName in System.Reflection.Assembly.GetEntryAssembly().GetReferencedAssemblies())
                {
                    asm = System.Reflection.Assembly.Load(assemblyName);
                    ct = asm.GetType(classname);
                    if(ct != null)
                        break;
                }

                if(ct == null)
                    foreach(AssemblyName assemblyName in System.Reflection.Assembly.GetExecutingAssembly().GetReferencedAssemblies())
                    {
                        asm = System.Reflection.Assembly.Load(assemblyName);
                        ct = asm.GetType(classname);
                        if(ct != null)
                            break;
                    }


                if(ct == null)
                    foreach(AssemblyName assemblyName in System.Reflection.Assembly.GetCallingAssembly().GetReferencedAssemblies())
                    {
                        asm = System.Reflection.Assembly.Load(assemblyName);
                        ct = asm.GetType(classname);
                        if(ct != null)
                            break;
                    }

                if(ct == null)
                {
                    Console.WriteLine("Error loading type: " + classname);
                    return;
                }

                MethodInfo method = ct.GetMethod("Invoke");

                ParameterInfo[] pinfo = method.GetParameters();
                int argLen = pinfo.Length;

                bool isVoid = method.ReturnType.ToString() == "System.Void";
                Type[] targs = new Type[argLen + (isVoid ? 0 : 1)];
                

                for(int i = 0; i < argLen; i++)
                    targs[i] = pinfo[i].ParameterType;
                
                if(isVoid)
                {
                    if(argLen == 0)
                        func = typeof(JVMDelegate).GetMethod((isVoid ? "a" : "f") + argLen).CreateDelegate(ct, this);
                    else
                        func = typeof(JVMDelegate).GetMethod((isVoid ? "a" : "f") + argLen).MakeGenericMethod(targs).CreateDelegate(ct, this);
                }
                else
                {
                    targs[argLen] = method.ReturnType;
                    func = typeof(JVMDelegate).GetMethod((isVoid ? "a" : "f") + argLen).MakeGenericMethod(targs).CreateDelegate(ct, this);
                }

                
                // DB[pointer] = this;
                DB[pointer] = new WeakReference(this);
            }
        }

        private readonly object objLock_Invoke = new object();
        public object Invoke(params object[] args)
        {
            // lock(objLock_Invoke)
            {
                return fx(args);
            }
        }

        ~JVMDelegate() 
        {
            // Console.WriteLine("JVMDelegate DISPOSE 2: " + this);
            int hsh = Pointer;
            if(DB.ContainsKey(hsh))
            {
                WeakReference ot;
                DB.TryRemove(hsh, out ot);
                // if(DB.TryRemove(hsh, out ot))
                    // Console.WriteLine("REMOVED!: " + ot);
                // else
                    // Console.WriteLine("NOT REMOVED 1");
            }
            // else
                // Console.WriteLine("NOT REMOVED 2");

            if(Runtime.__DB.ContainsKey(hsh))
            {
                object _o;
                Runtime.__DB.TryRemove(hsh, out _o);
            }

            this.Dispose();
            

                // Runtime.RegisterJVMObject(hsh, _pointer.ToPointer());

                // if(!DB.ContainsKey(hsh))
                {
                    // DB.TryAdd(hsh, this);
                    // __DB[hsh] = this;
                    
                    
                }
            // if (Ptr != IntPtr.Zero) 
            // {
            //     Marshal.FreeHGlobal(Ptr);
            //     Ptr = IntPtr.Zero;
            // }
        }

        public void Dispose() 
        {
            int hsh = Pointer;

            object ou;
            if(Runtime.__DB.ContainsKey(hsh))
                Runtime.__DB.TryRemove(hsh, out ou);

            // Console.WriteLine("JVMDelegate DISPOSE: " + this);
            // Marshal.FreeHGlobal(Ptr);
            // Ptr = IntPtr.Zero;
            // GC.SuppressFinalize(this);
        }

        private readonly object objLock_fx = new object();
        public object fx(params object[] args)
        {
            // lock(objLock_fx)
            {
                try
                {
                    return Runtime.InvokeFunc(Pointer, args);
                }
                catch(Exception e)
                {
                    Console.WriteLine("JVMDelevate fx(" + Pointer + "): " + e + " " + args);
                    if(args != null)
                        foreach (var item in args)
                        {
                            Console.WriteLine("-------(" + item.GetHashCode() + "): " + item);
                        }
                    return null;
                }
            }
        }
        public T f0<T>()
        {
            return (T)fx();
        }
        public T f1<A0, T>(A0 x)
        {
            return (T)fx(x);
        }
        public T f2<A0, A1, T>(A0 x0, A1 x1)
        {
            return (T)fx(x0, x1);
        }
        public T f3<A0, A1, A2, T>(A0 x0, A1 x1, A2 x2)
        {
            return (T)fx(x0, x1, x2);
        }

        public T f4<A0, A1, A2, A3, T>(A0 x0, A1 x1, A2 x2, A3 x3)
        {
            return (T)fx(x0, x1, x2, x3);
        }

        public T f5<A0, A1, A2, A3, A4, T>(A0 x0, A1 x1, A2 x2, A3 x3, A4 x4)
        {
            return (T)fx(x0, x1, x2, x3, x4);
        }

        public T f6<A0, A1, A2, A3, A4, A5, T>(A0 x0, A1 x1, A2 x2, A3 x3, A4 x4, A5 x5)
        {
            return (T)fx(x0, x1, x2, x3, x4, x5);
        }

        public T f7<A0, A1, A2, A3, A4, A5, A6, T>(A0 x0, A1 x1, A2 x2, A3 x3, A4 x4, A5 x5, A6 x6)
        {
            return (T)fx(x0, x1, x2, x3, x4, x5, x6);
        }

        public T f8<A0, A1, A2, A3, A4, A5, A6, A7, T>(A0 x0, A1 x1, A2 x2, A3 x3, A4 x4, A5 x5, A6 x6, A7 x7)
        {
            return (T)fx(x0, x1, x2, x3, x4, x5, x6, x7);
        }

        public T f9<A0, A1, A2, A3, A4, A5, A6, A7, A8, T>(A0 x0, A1 x1, A2 x2, A3 x3, A4 x4, A5 x5, A6 x6, A7 x7, A8 x8)
        {
            return (T)fx(x0, x1, x2, x3, x4, x5, x6, x7, x8);
        }

        public T f10<A0, A1, A2, A3, A4, A5, A6, A7, A8, A9, T>(A0 x0, A1 x1, A2 x2, A3 x3, A4 x4, A5 x5, A6 x6, A7 x7, A8 x8, A9 x9)
        {
            return (T)fx(x0, x1, x2, x3, x4, x5, x6, x7, x8, x9);
        }

        public T f11<A0, A1, A2, A3, A4, A5, A6, A7, A8, A9, A10, T>(A0 x0, A1 x1, A2 x2, A3 x3, A4 x4, A5 x5, A6 x6, A7 x7, A8 x8, A9 x9, A10 x10)
        {
            return (T)fx(x0, x1, x2, x3, x4, x5, x6, x7, x8, x9, x10);
        }

        public T f12<A0, A1, A2, A3, A4, A5, A6, A7, A8, A9, A10, A11, T>(A0 x0, A1 x1, A2 x2, A3 x3, A4 x4, A5 x5, A6 x6, A7 x7, A8 x8, A9 x9, A10 x10, A11 x11)
        {
            return (T)fx(x0, x1, x2, x3, x4, x5, x6, x7, x8, x9, x10, x11);
        }

        public T f13<A0, A1, A2, A3, A4, A5, A6, A7, A8, A9, A10, A11, A12, T>(A0 x0, A1 x1, A2 x2, A3 x3, A4 x4, A5 x5, A6 x6, A7 x7, A8 x8, A9 x9, A10 x10, A11 x11, A12 x12)
        {
            return (T)fx(x0, x1, x2, x3, x4, x5, x6, x7, x8, x9, x10, x11, x12);
        }

        public T f14<A0, A1, A2, A3, A4, A5, A6, A7, A8, A9, A10, A11, A12, A13, T>(A0 x0, A1 x1, A2 x2, A3 x3, A4 x4, A5 x5, A6 x6, A7 x7, A8 x8, A9 x9, A10 x10, A11 x11, A12 x12, A13 x13)
        {
            return (T)fx(x0, x1, x2, x3, x4, x5, x6, x7, x8, x9, x10, x11, x12, x13);
        }

        public T f15<A0, A1, A2, A3, A4, A5, A6, A7, A8, A9, A10, A11, A12, A13, A14, T>(A0 x0, A1 x1, A2 x2, A3 x3, A4 x4, A5 x5, A6 x6, A7 x7, A8 x8, A9 x9, A10 x10, A11 x11, A12 x12, A13 x13, A14 x14)
        {
            return (T)fx(x0, x1, x2, x3, x4, x5, x6, x7, x8, x9, x10, x11, x12, x13, x14);
        }

        public T f16<A0, A1, A2, A3, A4, A5, A6, A7, A8, A9, A10, A11, A12, A13, A14, A15, T>(A0 x0, A1 x1, A2 x2, A3 x3, A4 x4, A5 x5, A6 x6, A7 x7, A8 x8, A9 x9, A10 x10, A11 x11, A12 x12, A13 x13, A14 x14, A15 x15)
        {
            return (T)fx(x0, x1, x2, x3, x4, x5, x6, x7, x8, x9, x10, x11, x12, x13, x14, x15);
        }

        public void a0()
        {
            fx();
        }
        public void a1<A0>(A0 x)
        {
            fx(x);
        }
        public void a2<A0, A1>(A0 x0, A1 x1)
        {
            fx(x0, x1);
        }
        public void a3<A0, A1, A2>(A0 x0, A1 x1, A2 x2)
        {
            fx(x0, x1, x2);
        }

        public void a4<A0, A1, A2, A3>(A0 x0, A1 x1, A2 x2, A3 x3)
        {
            fx(x0, x1, x2, x3);
        }

        public void a5<A0, A1, A2, A3, A4>(A0 x0, A1 x1, A2 x2, A3 x3, A4 x4)
        {
            fx(x0, x1, x2, x3, x4);
        }

        public void a6<A0, A1, A2, A3, A4, A5>(A0 x0, A1 x1, A2 x2, A3 x3, A4 x4, A5 x5)
        {
            fx(x0, x1, x2, x3, x4, x5);
        }

        public void a7<A0, A1, A2, A3, A4, A5, A6>(A0 x0, A1 x1, A2 x2, A3 x3, A4 x4, A5 x5, A6 x6)
        {
            fx(x0, x1, x2, x3, x4, x5, x6);
        }

        public void a8<A0, A1, A2, A3, A4, A5, A6, A7>(A0 x0, A1 x1, A2 x2, A3 x3, A4 x4, A5 x5, A6 x6, A7 x7)
        {
            fx(x0, x1, x2, x3, x4, x5, x6, x7);
        }

        public void a9<A0, A1, A2, A3, A4, A5, A6, A7, A8>(A0 x0, A1 x1, A2 x2, A3 x3, A4 x4, A5 x5, A6 x6, A7 x7, A8 x8)
        {
            fx(x0, x1, x2, x3, x4, x5, x6, x7, x8);
        }

        public void a10<A0, A1, A2, A3, A4, A5, A6, A7, A8, A9>(A0 x0, A1 x1, A2 x2, A3 x3, A4 x4, A5 x5, A6 x6, A7 x7, A8 x8, A9 x9)
        {
            fx(x0, x1, x2, x3, x4, x5, x6, x7, x8, x9);
        }

        public void a11<A0, A1, A2, A3, A4, A5, A6, A7, A8, A9, A10>(A0 x0, A1 x1, A2 x2, A3 x3, A4 x4, A5 x5, A6 x6, A7 x7, A8 x8, A9 x9, A10 x10)
        {
            fx(x0, x1, x2, x3, x4, x5, x6, x7, x8, x9, x10);
        }

        public void a12<A0, A1, A2, A3, A4, A5, A6, A7, A8, A9, A10, A11>(A0 x0, A1 x1, A2 x2, A3 x3, A4 x4, A5 x5, A6 x6, A7 x7, A8 x8, A9 x9, A10 x10, A11 x11)
        {
            fx(x0, x1, x2, x3, x4, x5, x6, x7, x8, x9, x10, x11);
        }

        public void a13<A0, A1, A2, A3, A4, A5, A6, A7, A8, A9, A10, A11, A12>(A0 x0, A1 x1, A2 x2, A3 x3, A4 x4, A5 x5, A6 x6, A7 x7, A8 x8, A9 x9, A10 x10, A11 x11, A12 x12)
        {
            fx(x0, x1, x2, x3, x4, x5, x6, x7, x8, x9, x10, x11, x12);
        }

        public void a14<A0, A1, A2, A3, A4, A5, A6, A7, A8, A9, A10, A11, A12, A13>(A0 x0, A1 x1, A2 x2, A3 x3, A4 x4, A5 x5, A6 x6, A7 x7, A8 x8, A9 x9, A10 x10, A11 x11, A12 x12, A13 x13)
        {
            fx(x0, x1, x2, x3, x4, x5, x6, x7, x8, x9, x10, x11, x12, x13);
        }

        public void a15<A0, A1, A2, A3, A4, A5, A6, A7, A8, A9, A10, A11, A12, A13, A14>(A0 x0, A1 x1, A2 x2, A3 x3, A4 x4, A5 x5, A6 x6, A7 x7, A8 x8, A9 x9, A10 x10, A11 x11, A12 x12, A13 x13, A14 x14)
        {
            fx(x0, x1, x2, x3, x4, x5, x6, x7, x8, x9, x10, x11, x12, x13, x14);
        }

        public void a16<A0, A1, A2, A3, A4, A5, A6, A7, A8, A9, A10, A11, A12, A13, A14, A15>(A0 x0, A1 x1, A2 x2, A3 x3, A4 x4, A5 x5, A6 x6, A7 x7, A8 x8, A9 x9, A10 x10, A11 x11, A12 x12, A13 x13, A14 x14, A15 x15)
        {
            fx(x0, x1, x2, x3, x4, x5, x6, x7, x8, x9, x10, x11, x12, x13, x14, x15);
        }
    }
}