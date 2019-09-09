/*
 * The MIT License (MIT)
 * Copyright (c) 2007-2019, Arturo Rodriguez All rights reserved.
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

using Python.Runtime;

using Dynamitey;

namespace QuantApp.Kernel.JVM
{
    public class Runtime
    {
        /*
            Solaris: libJNIWrapper.so
            Linux: libJNIWrapper.so
            Win: JNIWrapper.dll
            Mac: libJNIWrapper.jnilib

        */

        #if MONO_OSX
        private const string JVMDll = "libjvm.dylib";
        private const string InvokerDll = "libJNIWrapper.jnilib";

        #elif MONO_LINUX
        private const string JVMDll = "libjvm.so";
        private const string InvokerDll = "libJNIWrapper.so";
        
        #else //Windows
        private const string JVMDll = "jvm.dll";
        private const string InvokerDll = "JNIWrapper.dll";
        #endif

        
        internal static ConcurrentDictionary<int,object> DB = new ConcurrentDictionary<int, object>();

        [DllImport(JVMDll)] private unsafe static extern int  JNI_CreateJavaVM(void** ppVm, void** ppEnv, void* pArgs);
        [DllImport(InvokerDll)] private unsafe static extern void SetfnGetProperty(void* func);

        [DllImport(InvokerDll)] private unsafe static extern void SetfnSetProperty(void* func);
        [DllImport(InvokerDll)] private unsafe static extern void SetfnInvokeFunc(void* func);
        [DllImport(InvokerDll)] private unsafe static extern void SetfnCreateInstance(void* func);
        [DllImport(InvokerDll)] public unsafe static extern void SetfnInvoke(void* func);
        [DllImport(InvokerDll)] public unsafe static extern void SetfnRegisterFunc(void* func);
        
        
        [DllImport(InvokerDll)] private unsafe static extern int  AttacheThread(void* ppVm, void** pEnv);
        [DllImport(InvokerDll)] private unsafe static extern int  DetacheThread(void* ppVm);
        [DllImport(InvokerDll)] private unsafe static extern int  MakeJavaVMInitArgs(string classpath, string libpath, void** ppArgs );
        [DllImport(InvokerDll)] private unsafe static extern void FreeJavaVMInitArgs( void* pArgs );

        [DllImport(InvokerDll)] private unsafe static extern int  FindClass( void* pEnv, String sClass, void** ppClass);
        [DllImport(InvokerDll)] private unsafe static extern string  GetClassName(void* pEnv, void* entity);

        [DllImport(InvokerDll)] private unsafe static extern int NewObjectP(void*  pEnv, void* sType, String szArgs, int len, void** pArgs , void** ppObj);
        [DllImport(InvokerDll)] private unsafe static extern int NewObject(void*  pEnv, String sType, String szArgs, int len, void** pArgs , void** ppObj);
        
        [DllImport(InvokerDll)] private unsafe static extern int NewBooleanObject(void*  pEnv, bool val , void** ppObj);
        [DllImport(InvokerDll)] private unsafe static extern int NewByteObject(void*  pEnv, byte val , void** ppObj);
        [DllImport(InvokerDll)] private unsafe static extern int NewCharacterObject(void*  pEnv, char val , void** ppObj);
        [DllImport(InvokerDll)] private unsafe static extern int NewShortObject(void*  pEnv, short val , void** ppObj);
        [DllImport(InvokerDll)] private unsafe static extern int NewIntegerObject(void*  pEnv, int val , void** ppObj);
        [DllImport(InvokerDll)] private unsafe static extern int NewLongObject(void*  pEnv, long val , void** ppObj);
        [DllImport(InvokerDll)] private unsafe static extern int NewFloatObject(void*  pEnv, float val , void** ppObj);
        [DllImport(InvokerDll)] private unsafe static extern int NewDoubleObject(void*  pEnv, double val , void** ppObj);


        [DllImport(InvokerDll)] private unsafe static extern int GetStaticMethodID(void*  pEnv, void*  pClass, String szName, String szArgs, void** ppMid);
        [DllImport(InvokerDll)] private unsafe static extern int GetMethodID(void*  pEnv, void*  pClass, String szName, String szArgs, void** ppMid);

        [DllImport(InvokerDll)] private unsafe static extern int GetStaticFieldID(void*  pEnv, void*  pClass, String szName, String sig, void** ppFid);
        [DllImport(InvokerDll)] private unsafe static extern int GetFieldID(void*  pEnv, void*  pClass, String szName, String sig, void** ppFid);

        [DllImport(InvokerDll)] private unsafe static extern int CallStaticVoidMethod(void* pEnv, void* pClass, void* pMid, int len, void** pArgs);
        [DllImport(InvokerDll)] private unsafe static extern int CallVoidMethod(void* pEnv, void* pClass, void* pMid, int len, void** pArgs);

        [DllImport(InvokerDll)] private unsafe static extern int GetObjectClass(void* pEnv, void* pObject, void** pClass, void** nameClass);
        [DllImport(InvokerDll)] private unsafe static extern int CallStaticObjectMethod(void* pEnv, void* pClass, void* pMid, void** pObject, int len, void** pArgs);
        [DllImport(InvokerDll)] private unsafe static extern int CallObjectMethod(void* pEnv, void* pClass, void* pMid, void** pObject, int len, void** pArgs);
        [DllImport(InvokerDll)] private unsafe static extern int GetStaticObjectField(void* pEnv, void* pClass, void* pMid, void** pObject);
        [DllImport(InvokerDll)] private unsafe static extern int GetObjectField(void* pEnv, void* pClass, void* pMid, void** pObject);
        [DllImport(InvokerDll)] private unsafe static extern int SetStaticObjectField(void* pEnv, void* pClass, void* pMid, void* pObject);
        [DllImport(InvokerDll)] private unsafe static extern int SetObjectField(void* pEnv, void* pClass, void* pMid, void* pObject);


        [DllImport(InvokerDll)] private unsafe static extern int CallStaticIntMethod(void* pEnv, void* pClass, void* pMid, int len, void** pArgs);
        [DllImport(InvokerDll)] private unsafe static extern int CallIntMethod(void* pEnv, void* pClass, void* pMid, int len, void** pArgs);
        [DllImport(InvokerDll)] private unsafe static extern int GetStaticIntField(void* pEnv, void* pClass, void* pMid);
        [DllImport(InvokerDll)] private unsafe static extern int GetIntField(void* pEnv, void* pClass, void* pMid);
        [DllImport(InvokerDll)] private unsafe static extern int SetStaticIntField(void* pEnv, void* pClass, void* pMid, int val);
        [DllImport(InvokerDll)] private unsafe static extern int SetIntField(void* pEnv, void* pClass, void* pMid, int val);
        
        
        [DllImport(InvokerDll)] private unsafe static extern long CallStaticLongMethod(void* pEnv, void* pClass, void* pMid, int len, void** pArgs);
        [DllImport(InvokerDll)] private unsafe static extern long CallLongMethod(void* pEnv, void* pClass, void* pMid, int len, void** pArgs);
        
        [DllImport(InvokerDll)] private unsafe static extern long GetStaticLongField(void* pEnv, void* pClass, void* pMid);
        [DllImport(InvokerDll)] private unsafe static extern long GetLongField(void* pEnv, void* pClass, void* pMid);
        [DllImport(InvokerDll)] private unsafe static extern int SetStaticLongField(void* pEnv, void* pClass, void* pMid, long val);
        [DllImport(InvokerDll)] private unsafe static extern int SetLongField(void* pEnv, void* pClass, void* pMid, long val);
        
        [DllImport(InvokerDll)] private unsafe static extern float CallStaticFloatMethod(void* pEnv, void* pClass, void* pMid, int len,  void** pArgs);
        [DllImport(InvokerDll)] private unsafe static extern float CallFloatMethod(void* pEnv, void* pClass, void* pMid, int len,  void** pArgs);
        [DllImport(InvokerDll)] private unsafe static extern float GetStaticFloatField(void* pEnv, void* pClass, void* pMid);
        [DllImport(InvokerDll)] private unsafe static extern float GetFloatField(void* pEnv, void* pClass, void* pMid);
        [DllImport(InvokerDll)] private unsafe static extern int SetStaticFloatField(void* pEnv, void* pClass, void* pMid, float val);
        [DllImport(InvokerDll)] private unsafe static extern int SetFloatField(void* pEnv, void* pClass, void* pMid, float val);

        [DllImport(InvokerDll)] private unsafe static extern double CallStaticDoubleMethod(void* pEnv, void* pClass, void* pMid, int len, void** pArgs);
        [DllImport(InvokerDll)] private unsafe static extern double CallDoubleMethod( void* pEnv, void* pClass, void* pMid, int len, void** pArgs);
        [DllImport(InvokerDll)] private unsafe static extern double GetStaticDoubleField(void* pEnv, void* pClass, void* pMid);
        [DllImport(InvokerDll)] private unsafe static extern double GetDoubleField( void* pEnv, void* pClass, void* pMid);
        [DllImport(InvokerDll)] private unsafe static extern int SetStaticDoubleField(void* pEnv, void* pClass, void* pMid, double val);
        [DllImport(InvokerDll)] private unsafe static extern int SetDoubleField(void* pEnv, void* pClass, void* pMid, double val);


        [DllImport(InvokerDll)] private unsafe static extern bool CallStaticBooleanMethod(void* pEnv, void* pClass, void* pMid, int len, void** pArgs);
        [DllImport(InvokerDll)] private unsafe static extern bool CallBooleanMethod( void* pEnv, void* pClass, void* pMid, int len, void** pArgs);
        [DllImport(InvokerDll)] private unsafe static extern bool GetStaticBooleanField(void* pEnv, void* pClass, void* pMid);
        [DllImport(InvokerDll)] private unsafe static extern bool GetBooleanField( void* pEnv, void* pClass, void* pMid);
        [DllImport(InvokerDll)] private unsafe static extern int SetStaticBooleanField(void* pEnv, void* pClass, void* pMid, bool val);
        [DllImport(InvokerDll)] private unsafe static extern int SetBooleanField(void* pEnv, void* pClass, void* pMid, bool val);

        
        [DllImport(InvokerDll)] private unsafe static extern byte CallStaticByteMethod(void* pEnv, void* pClass, void* pMid, int len, void** pArgs);
        [DllImport(InvokerDll)] private unsafe static extern byte CallByteMethod( void* pEnv, void* pClass, void* pMid, int len, void** pArgs);
        [DllImport(InvokerDll)] private unsafe static extern byte GetStaticByteField(void* pEnv, void* pClass, void* pMid);
        [DllImport(InvokerDll)] private unsafe static extern byte GetByteField( void* pEnv, void* pClass, void* pMid);
        [DllImport(InvokerDll)] private unsafe static extern int SetStaticByteField(void* pEnv, void* pClass, void* pMid, byte val);
        [DllImport(InvokerDll)] private unsafe static extern int SetByteField(void* pEnv, void* pClass, void* pMid, byte val);


        [DllImport(InvokerDll)] private unsafe static extern char CallStaticCharMethod(void* pEnv, void* pClass, void* pMid, int len, void** pArgs);
        [DllImport(InvokerDll)] private unsafe static extern char CallCharMethod( void* pEnv, void* pClass, void* pMid, int len, void** pArgs);
        [DllImport(InvokerDll)] private unsafe static extern char GetStaticCharField(void* pEnv, void* pClass, void* pMid);
        [DllImport(InvokerDll)] private unsafe static extern char GetCharField( void* pEnv, void* pClass, void* pMid);
        [DllImport(InvokerDll)] private unsafe static extern int SetStaticCharField(void* pEnv, void* pClass, void* pMid, char val);
        [DllImport(InvokerDll)] private unsafe static extern int SetCharField(void* pEnv, void* pClass, void* pMid, char val);


        [DllImport(InvokerDll)] private unsafe static extern short CallStaticShortMethod(void* pEnv, void* pClass, void* pMid, int len, void** pArgs);
        [DllImport(InvokerDll)] private unsafe static extern short CallShortMethod( void* pEnv, void* pClass, void* pMid, int len, void** pArgs);
        [DllImport(InvokerDll)] private unsafe static extern short GetStaticShortField(void* pEnv, void* pClass, void* pMid);
        [DllImport(InvokerDll)] private unsafe static extern short GetShortField( void* pEnv, void* pClass, void* pMid);
        [DllImport(InvokerDll)] private unsafe static extern int SetStaticShortField(void* pEnv, void* pClass, void* pMid, short val);
        [DllImport(InvokerDll)] private unsafe static extern int SetShortField(void* pEnv, void* pClass, void* pMid, short val);


        [DllImport(InvokerDll)] private unsafe static extern void* GetJavaString( void* pEnv, string str );
        [DllImport(InvokerDll)] private unsafe static extern string GetNetString( void* pEnv, void* jstr );


        [DllImport(InvokerDll)] private unsafe static extern int NewObjectArrayP( void*  pEnv, int nDimension, void* pClass, void** ppArray );
        [DllImport(InvokerDll)] private unsafe static extern int NewObjectArray( void*  pEnv, int nDimension, String sType, void** ppArray );
        [DllImport(InvokerDll)] private unsafe static extern int SetObjectArrayElement( void* pEnv, void* pArray, int index, void* value);
        [DllImport(InvokerDll)] private unsafe static extern int GetObjectArrayElement( void* pEnv, void* pArray, int index, void** pObject);


        [DllImport(InvokerDll)] private unsafe static extern int NewIntArray( void* pEnv, int nDimension, void** ppArray );
        [DllImport(InvokerDll)] private unsafe static extern int SetIntArrayElement( void* pEnv, void* pArray, int index, int value);
        [DllImport(InvokerDll)] private unsafe static extern int GetIntArrayElement( void* pEnv, void* pArray, int index);
        

        [DllImport(InvokerDll)] private unsafe static extern int NewLongArray( void* pEnv, int nDimension, void** ppArray );
        [DllImport(InvokerDll)] private unsafe static extern int SetLongArrayElement( void* pEnv, void* pArray, int index, long value);
        [DllImport(InvokerDll)] private unsafe static extern long GetLongArrayElement( void* pEnv, void* pArray, int index);
        

        [DllImport(InvokerDll)] private unsafe static extern int NewFloatArray( void* pEnv, int nDimension, void** ppArray );
        [DllImport(InvokerDll)] private unsafe static extern int SetFloatArrayElement( void* pEnv, void* pArray, int index, float value);
        [DllImport(InvokerDll)] private unsafe static extern float GetFloatArrayElement( void* pEnv, void* pArray, int index);


        [DllImport(InvokerDll)] private unsafe static extern int NewDoubleArray( void* pEnv, int nDimension, void** ppArray );
        [DllImport(InvokerDll)] private unsafe static extern int SetDoubleArrayElement( void* pEnv, void* pArray, int index, double value);
        [DllImport(InvokerDll)] private unsafe static extern double GetDoubleArrayElement( void* pEnv, void* pArray, int index);


        [DllImport(InvokerDll)] private unsafe static extern int NewBooleanArray( void* pEnv, int nDimension, void** ppArray );
        [DllImport(InvokerDll)] private unsafe static extern int SetBooleanArrayElement( void* pEnv, void* pArray, int index, bool value);
        [DllImport(InvokerDll)] private unsafe static extern bool GetBooleanArrayElement( void* pEnv, void* pArray, int index);

        [DllImport(InvokerDll)] private unsafe static extern int NewByteArray( void* pEnv, int nDimension, void** ppArray );
        [DllImport(InvokerDll)] private unsafe static extern int SetByteArrayElement( void* pEnv, void* pArray, int index, byte value);
        [DllImport(InvokerDll)] private unsafe static extern byte GetByteArrayElement( void* pEnv, void* pArray, int index);

        [DllImport(InvokerDll)] private unsafe static extern int NewShortArray( void* pEnv, int nDimension, void** ppArray );
        [DllImport(InvokerDll)] private unsafe static extern int SetShortArrayElement( void* pEnv, void* pArray, int index, short value);
        [DllImport(InvokerDll)] private unsafe static extern short GetShortArrayElement( void* pEnv, void* pArray, int index);
        

        [DllImport(InvokerDll)] private unsafe static extern int NewCharArray( void* pEnv, int nDimension, void** ppArray );
        [DllImport(InvokerDll)] private unsafe static extern int SetCharArrayElement( void* pEnv, void* pArray, int index, char value);
        [DllImport(InvokerDll)] private unsafe static extern char GetCharArrayElement( void* pEnv, void* pArray, int index);
        

        [DllImport(InvokerDll)] private unsafe static extern int DestroyJavaVM( void* pJVM );

        private static IntPtr JVMPtr;

        public static bool Loaded = false;

        private static SetCreateInstance delCreateInstance;
        private static GCHandle gchCallFunc;
        private static SetInvoke delInvoke;
        private static GCHandle gchInvoke;
        private static SetRegisterFunc delRegisterFunc;
        private static GCHandle gchRegisterFunc;
        private static SetInvokeFunc delInvokeFunc;
        private static GCHandle gchInvokeFunc;
        private static SetSetProperty delSetProperty;
        private static GCHandle gchSetProperty;
        private static SetGetProperty delGetProperty;
        private static GCHandle gchGetProperty;
        
        public unsafe static int InitJVM(string classpath = ".:app.quant.clr.jar", string libpath = ".")
        {

            delCreateInstance = new SetCreateInstance(Java_app_quant_clr_CLRRuntime_nativeCreateInstance);
            gchCallFunc = GCHandle.Alloc(delCreateInstance);
            SetfnCreateInstance(Marshal.GetFunctionPointerForDelegate<SetCreateInstance>(delCreateInstance).ToPointer());

            delInvoke = new SetInvoke(Java_app_quant_clr_CLRRuntime_nativeInvoke);
            gchInvoke = GCHandle.Alloc(delInvoke);
            SetfnInvoke(Marshal.GetFunctionPointerForDelegate<SetInvoke>(delInvoke).ToPointer());
            
            delRegisterFunc = new SetRegisterFunc(Java_app_quant_clr_CLRRuntime_nativeRegisterFunc);
            gchRegisterFunc = GCHandle.Alloc(delRegisterFunc);
            SetfnRegisterFunc(Marshal.GetFunctionPointerForDelegate<SetRegisterFunc>(delRegisterFunc).ToPointer());
            
            delInvokeFunc = new SetInvokeFunc(Java_app_quant_clr_CLRRuntime_nativeInvokeFunc);
            gchInvokeFunc = GCHandle.Alloc(delInvokeFunc);
            SetfnInvokeFunc(Marshal.GetFunctionPointerForDelegate<SetInvokeFunc>(delInvokeFunc).ToPointer());

            delSetProperty = new SetSetProperty(Java_app_quant_clr_CLRRuntime_nativeSetProperty);
            gchSetProperty = GCHandle.Alloc(delSetProperty);
            SetfnSetProperty(Marshal.GetFunctionPointerForDelegate<SetSetProperty>(delSetProperty).ToPointer());
            
            delGetProperty = new SetGetProperty(Java_app_quant_clr_CLRRuntime_nativeGetProperty);
            gchGetProperty = GCHandle.Alloc(delGetProperty);
            SetfnGetProperty(Marshal.GetFunctionPointerForDelegate<SetGetProperty>(delGetProperty).ToPointer());
            
            void*  pJVM;    // JVM struct
            void*  pEnv;    // JVM environment
            void*  pVMArgs; // VM args
            
            // Fill the pVMArgs structs
            MakeJavaVMInitArgs(classpath, libpath, &pVMArgs );
            
            // Create JVM
            int nRes = JNI_CreateJavaVM( &pJVM, &pEnv, pVMArgs );
            
            if(nRes == 0)
            {
                JVMPtr = (IntPtr)pJVM;
                Loaded = true;
            }

            var classpathList = classpath.Substring(1).Split(':');
            SetClassPath(classpathList);


            return nRes;        
        }


        private static unsafe int Java_app_quant_clr_CLRRuntime_nativeCreateInstance(string classname, int len, void** args)
        {
            void*  pEnv;// = (void*)EnvPtr;
            if(AttacheThread((void*)JVMPtr,&pEnv) != 0) throw new Exception ("Attach to thread error");
            
            object[] classes_obj = getJavaArray(new IntPtr(args), "[Ljava/lang/Object;");
            

            Type ct = null;
            Assembly asm = System.Reflection.Assembly.GetEntryAssembly();
            ct = asm.GetType(classname);

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

            object obj = null;
            
            try
            {
                obj = asm.CreateInstance(
                    typeName: classname, // string including namespace of the type
                    ignoreCase: false,
                    bindingAttr: BindingFlags.Default,
                    binder: null,  // use default binder
                    args: classes_obj,
                    culture: null, // use CultureInfo from current thread
                    activationAttributes: null
                );
            }
            catch(System.MissingMethodException e)
            {
                obj = ct;
            }

            if(obj == null)
                return 0;

            int hashCode = obj.GetHashCode();

            if(!DB.ContainsKey(hashCode))
                DB.TryAdd(hashCode, obj);

            return hashCode;
        }
        private unsafe delegate int SetCreateInstance(string classname, int len, void** args);
        
        private static unsafe void* Java_app_quant_clr_CLRRuntime_nativeInvoke(int hashCode, string funcname, int len, void** args)
        {
            object[] classes_obj = getJavaArray(new IntPtr(args), "[Ljava/lang/Object;");

            try
            {
                if(DB.ContainsKey(hashCode))
                {
                    object obj = DB[hashCode];

                    if(obj is Type)
                    {
                        MethodInfo method = (obj as Type).GetMethod(funcname);
                        object res = method.Invoke(null, classes_obj);
                        if(res == null)
                            return IntPtr.Zero.ToPointer();
                        
                        return getObjectPointer(res);
                    }
                    else if(obj is DynamicObject)
                    {

                        var res = Dynamic.InvokeMember(obj,funcname,classes_obj);
                        
                        if(res == null)
                            return IntPtr.Zero.ToPointer();

                        return getObjectPointer((object)res);
                    }
                    else
                    {
                        
                        MethodInfo method = obj.GetType().GetMethod(funcname);

                        object res = method.Invoke(obj, classes_obj);
                        
                        if(res == null)
                            return IntPtr.Zero.ToPointer();

                        return getObjectPointer(res);
                    }
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("Java_app_quant_clr_CLRRuntime_nativeInvoke: " + hashCode + " " + funcname + " " + len + " " + classes_obj);
                Console.WriteLine(e);
            }

            return null;
            
        }
        private unsafe delegate void* SetInvoke(int ptr, string funcname, int len, void** args);


        private static unsafe void* Java_app_quant_clr_CLRRuntime_nativeRegisterFunc(string funcname, int hashCode)
        {
            void*  pEnv;
            if(AttacheThread((void*)JVMPtr,&pEnv) != 0) throw new Exception ("Attach to thread error");
            try
            {
                JVMDelegate del = new JVMDelegate(funcname, hashCode);
                
                int clrHashCode = del.GetHashCode();
                void* ptr_res = (void *)(clrHashCode);
                
                object[] args_object = new object[] { funcname, hashCode };

                void** ar_newInstance = stackalloc void*[args_object.Length];
                getJavaParameters(ref ar_newInstance, args_object);
                void* pObj;
                int ro = NewObject( pEnv, "app/quant/clr/CLRObject", "(Ljava/lang/String;I)V", args_object.Length, ar_newInstance, &pObj );

                DB.TryAdd(hashCode, del);

                return pObj;
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
                return IntPtr.Zero.ToPointer();
            }
            
        }
        private unsafe delegate void* SetRegisterFunc(string funcname, int hashCode);
        

        public static unsafe object InvokeFunc(int hashCode, object[] args)
        {
            void** pAr_len = stackalloc void*[args == null ? 1 : 2];
            object[] pAr_len_data = args == null ? new object[]{ hashCode } : new object[]{ hashCode, args };
            getJavaParameters(ref pAr_len, pAr_len_data);

            void*  pEnv;
            if(AttacheThread((void*)JVMPtr,&pEnv) != 0) throw new Exception ("Attach to thread error");
            void*  pNetBridgeClass;
            if(FindClass( pEnv, "app/quant/clr/CLRRuntime", &pNetBridgeClass) == 0)
            {

                void*  pInvokeMethod;
                if(GetStaticMethodID( pEnv, pNetBridgeClass, "InvokeDelegate", "(I[Ljava/lang/Object;)Ljava/lang/Object;", &pInvokeMethod ) == 0)
                {
                    void*  pGetCLRObject;
                    if(CallStaticObjectMethod( pEnv, pNetBridgeClass, pInvokeMethod, &pGetCLRObject, 2, pAr_len) == 0)
                    {
                        void* pClass;
                        void* pNameClass;
                        if(GetObjectClass(pEnv, pGetCLRObject, &pClass, &pNameClass) == 0)
                        {
                            string clsName = GetNetString(pEnv, pNameClass);

                            switch(clsName)
                            {
                                case "java.lang.Boolean":
                                    void*  pInvokeMethod_boolean;
                                    GetMethodID( pEnv, pGetCLRObject, "booleanValue", "()Z", &pInvokeMethod_boolean );
                                    
                                    
                                    void** pAr_boolean = stackalloc void*[1];
                                    return CallBooleanMethod( pEnv, pGetCLRObject, pInvokeMethod_boolean, 1, pAr_boolean);

                                case "java.lang.Byte":
                                    void*  pInvokeMethod_byte;
                                    GetMethodID( pEnv, pGetCLRObject, "byteValue", "()B", &pInvokeMethod_byte );
                                    
                                    
                                    void** pAr_byte = stackalloc void*[1];
                                    return CallByteMethod( pEnv, pGetCLRObject, pInvokeMethod_byte, 1, pAr_byte);

                                case "java.lang.Character":
                                    void*  pInvokeMethod_char;
                                    GetMethodID( pEnv, pGetCLRObject, "charValue", "()C", &pInvokeMethod_char );
                                    
                                    
                                    void** pAr_char = stackalloc void*[1];
                                    return CallCharMethod( pEnv, pGetCLRObject, pInvokeMethod_char, 1, pAr_char);

                                case "java.lang.Short":
                                    void*  pInvokeMethod_short;
                                    GetMethodID( pEnv, pGetCLRObject, "shortValue", "()S", &pInvokeMethod_short );
                                    
                                    
                                    void** pAr_short = stackalloc void*[1];
                                    return CallShortMethod( pEnv, pGetCLRObject, pInvokeMethod_short, 1, pAr_short);

                                case "java.lang.Integer":
                                    void*  pInvokeMethod_int;
                                    GetMethodID( pEnv, pGetCLRObject, "intValue", "()I", &pInvokeMethod_int );
                                    
                                    
                                    void** pAr_int = stackalloc void*[1];
                                    return CallIntMethod( pEnv, pGetCLRObject, pInvokeMethod_int, 1, pAr_int);

                                case "java.lang.Long":
                                    void*  pInvokeMethod_long;
                                    GetMethodID( pEnv, pGetCLRObject, "longValue", "()J", &pInvokeMethod_long );
                                    
                                    
                                    void** pAr_long = stackalloc void*[1];
                                    return CallLongMethod( pEnv, pGetCLRObject, pInvokeMethod_long, 1, pAr_long);


                                case "java.lang.Float":
                                    void*  pInvokeMethod_float;
                                    GetMethodID( pEnv, pGetCLRObject, "floatValue", "()F", &pInvokeMethod_float );
                                    
                                    
                                    void** pAr_float = stackalloc void*[1];
                                    return CallFloatMethod( pEnv, pGetCLRObject, pInvokeMethod_float, 1, pAr_float);

                                case "java.lang.Double":
                                    void*  pInvokeMethod_double;
                                    GetMethodID( pEnv, pGetCLRObject, "doubleValue", "()D", &pInvokeMethod_double );
                                    
                                    
                                    void** pAr_double = stackalloc void*[1];
                                    return CallDoubleMethod( pEnv, pGetCLRObject, pInvokeMethod_double, 1, pAr_double);


                                case "java.lang.String":
                                    return GetNetString(pEnv, pGetCLRObject);


                                case "java.util.LocalDateTime":
                                    return GetNetDateTime(pEnv, pGetCLRObject);

                                default:
                                    if(clsName.StartsWith("["))
                                        return getJavaArray(new IntPtr(pGetCLRObject), clsName);
                                    
                                    else
                                    {
                                            int hashID_res = getHashCode(pGetCLRObject);

                                            
                                            if(JVMDelegate.DB.ContainsKey(hashID_res)) //check if it is a CLRObject
                                                return JVMDelegate.DB[hashID_res];

                                            else if(Runtime.DB.ContainsKey(hashID_res)) //check if it is a JVMObject
                                                return Runtime.DB[hashID_res];


                                            else if(JVMObject.DB.ContainsKey(hashID_res)) //check if it is a JVMObject
                                                return JVMObject.DB[hashID_res];

                                            else
                                            {
                                                string cls = clsName.StartsWith("L") && clsName.EndsWith(";") ? clsName.Substring(1).Replace(";","") : clsName;
                                                return CreateInstancePtr(cls, null, new IntPtr(pGetCLRObject), null );
                                            }
                                        }
                            }
                        
                        }
                        else
                            Console.WriteLine("InvokeDelegate GetObjectClass error");
                    }
                    else
                        Console.WriteLine("InvokeDelegate error");
                }
                else
                    Console.WriteLine("InvokeDelegate GetStaticMethod error");
            }
            else
                Console.WriteLine("InvokeDelegate find class app/quant/clr/CLRRuntime error");

            return null;
            
        }

        private static unsafe void* getObjectPointer(object res)
        {
            if(res is PyObject)
            {
                var pres = res as PyObject;
                if(PyString.IsStringType(pres))
                    res = pres.AsManagedObject(typeof(string));

                else if(PyFloat.IsFloatType(pres))
                    res = pres.AsManagedObject(typeof(float));

                else if(PyInt.IsIntType(pres))
                    res = pres.AsManagedObject(typeof(int));

                else if(PyDict.IsDictType(pres))
                    res = pres.AsManagedObject(typeof(Dictionary<object, object>));

                else if(PyList.IsListType(pres))
                    res = pres.AsManagedObject(typeof(List<object>));

                else if(PyLong.IsLongType(pres))
                    res = pres.AsManagedObject(typeof(long));

                else if(PySequence.IsSequenceType(pres))
                    res = pres.AsManagedObject(typeof(IEnumerable<object>));

                else if(PyTuple.IsTupleType(pres))
                    res = pres.AsManagedObject(typeof(System.Tuple));
            }


            void*  pEnv;
            if(AttacheThread((void*)JVMPtr,&pEnv) != 0) throw new Exception ("Attach to thread error");
            Type type = res == null ? null : res.GetType();

            
            switch(Type.GetTypeCode(type))
            { 
                case TypeCode.Boolean:
                    void* res_bool;
                    if(NewBooleanObject(pEnv, (bool)res, &res_bool) == 0)
                        return res_bool;
                    else
                        throw new Exception("New primitive creation error");
                    
                case TypeCode.Byte:
                    void* res_byte;
                    if(NewByteObject(pEnv, (byte)res, &res_byte) == 0)
                        return res_byte;
                    else
                        throw new Exception("New primitive creation error");

                case TypeCode.Char:
                    void* res_char;
                    if(NewCharacterObject(pEnv, (char)res, &res_char) == 0)
                        return res_char;
                    else
                        throw new Exception("New primitive creation error");

                case TypeCode.Int16:
                    void* res_short;
                    if(NewShortObject(pEnv, (short)res, &res_short) == 0)
                        return res_short;
                    else
                        throw new Exception("New primitive creation error");

                case TypeCode.Int32: 
                    void* res_int;
                    if(NewIntegerObject(pEnv, (int)res, &res_int) == 0)
                        return res_int;
                    else
                        throw new Exception("New primitive creation error");

                case TypeCode.Int64:
                    void* res_long;
                    if(NewLongObject(pEnv, (long)res, &res_long) == 0)
                        return res_long;
                    else
                        throw new Exception("New primitive creation error");

                case TypeCode.Single:
                    void* res_float;
                    if(NewFloatObject(pEnv, (float)res, &res_float) == 0)
                        return res_float;
                    else
                        throw new Exception("New primitive creation error");

                case TypeCode.Double:
                    void* res_double;
                    if(NewDoubleObject(pEnv, (double)res, &res_double) == 0)
                        return res_double;
                    else
                        throw new Exception("New primitive creation error");

                case TypeCode.String:
                    void* string_arg = GetJavaString(pEnv, (string)res);
                    return (void**)string_arg;

                case TypeCode.DateTime:
                    void* date_arg = GetJavaDateTime(pEnv, (DateTime)res);
                    return (void**)date_arg;
                    

                default:

                    if(res != null && JVMDelegate.DB.ContainsKey(res.GetHashCode()))
                    {
                        JVMDelegate jobj = res as JVMDelegate; 
                        return (void *)(jobj.Pointer);
                    }

                    else if(res != null && DB.ContainsKey(res.GetHashCode()))
                    {
                        void*  pNetBridgeClass;
                        if(FindClass( pEnv, "app/quant/clr/CLRRuntime", &pNetBridgeClass) == 0)
                        {
                            void* ptr_res = (void *)(res.GetHashCode());

                            void*  pGetCLRObjectMethod;
                            if(GetStaticMethodID( pEnv, pNetBridgeClass, "GetCLRObject", "(I)Lapp/quant/clr/CLRObject;", &pGetCLRObjectMethod ) == 0)
                            {
                                void** pAr_len = stackalloc void*[1];
                                object[] pAr_len_data = new object[]{ res.GetHashCode() };
                                getJavaParameters(ref pAr_len, pAr_len_data);

                                void*  pGetCLRObject;
                                if(CallStaticObjectMethod( pEnv, pNetBridgeClass, pGetCLRObjectMethod, &pGetCLRObject, 1, pAr_len) == 0)
                                    return pGetCLRObject;
                                else
                                    throw new Exception("GetObjectPointer: CallStaticObjectMethod error");
                            }
                            else
                                throw new Exception("GetObjectPointer: GetCLRObject get method error");
                        }
                        else
                            throw new Exception("GetObjectPointer: get class CLRRuntime error");
                    }

                    else if(res is IJVMTuple)
                    {
                        IJVMTuple jobj = res as IJVMTuple; 
                        void* ptr = (void *)(jobj.JVMObject.Pointer);
                        return ptr;
                    }
                    else if(res is JVMTuple)
                    {
                        JVMTuple jobj = res as JVMTuple; 
                        void* ptr = (void *)(jobj.jVMObject.Pointer);
                        return ptr;
                    }
                    
                    else if(res is JVMObject)
                    {
                        JVMObject jobj = res as JVMObject; 
                        return (void *)(jobj.Pointer);
                    }

                    else if(res is Array)
                    {
                        Array sub = res as Array;
                        JVMObject javaArray = getJavaArray(sub);
                        return javaArray.Pointer.ToPointer();
                    }

                    

                    else if(res is IEnumerable<object>)
                    {
                        void* ptr_res = (void *)(res.GetHashCode());

                        void** pAr_len = stackalloc void*[2];
                        object[] pAr_len_data = new object[]{ res.GetType().ToString(), res.GetHashCode() };
                        getJavaParameters(ref pAr_len, pAr_len_data);

                        void* pObj;
                        void* CLRObjClass;
                        void*  pLoadClassMethod; // The executed method struct
                        if(FindClass( pEnv, "app/quant/clr/CLRIterable", &CLRObjClass) == 0)
                        {
                            void* pClass;
                            if(NewObjectP( pEnv, CLRObjClass, "(Ljava/lang/String;I)V", 2, pAr_len, &pObj ) == 0)
                            {
                                DB.TryAdd(res.GetHashCode(), res);
                                return pObj;
                            }
                            else
                                throw new Exception("GetObjectPointer: new instance error");
                        }
                        else
                            throw new Exception("GetObjectPointer: get class error");
                    }

                    else if(res is IEnumerator<object>)
                    {
                        void* ptr_res = (void *)(res.GetHashCode());

                        void** pAr_len = stackalloc void*[2];
                        object[] pAr_len_data = new object[]{ res.GetType().ToString(), res.GetHashCode() };
                        getJavaParameters(ref pAr_len, pAr_len_data);

                        void* pObj;
                        void* CLRObjClass;
                        void*  pLoadClassMethod; // The executed method struct
                        if(FindClass( pEnv, "app/quant/clr/CLRIterator", &CLRObjClass) == 0)
                        {
                            void* pClass;
                            if(NewObjectP( pEnv, CLRObjClass, "(Ljava/lang/String;I)V", 2, pAr_len, &pObj ) == 0)
                            {
                                DB.TryAdd(res.GetHashCode(), res);
                                return pObj;
                            }
                            else
                                throw new Exception("GetObjectPointer: new instance error");
                        }
                        else
                            throw new Exception("GetObjectPointer: get class error");
                    }

                    else
                    {
                        void* ptr_res = (void *)(res.GetHashCode());

                        void** pAr_len = stackalloc void*[2];
                        object[] pAr_len_data = new object[]{ res.GetType().ToString(), res.GetHashCode() };
                        getJavaParameters(ref pAr_len, pAr_len_data);

                        void* pObj;
                        void* CLRObjClass;
                        void*  pLoadClassMethod; // The executed method struct
                        if(FindClass( pEnv, "app/quant/clr/CLRObject", &CLRObjClass) == 0)
                        {
                            void* pClass;
                            if(NewObjectP( pEnv, CLRObjClass, "(Ljava/lang/String;I)V", 2, pAr_len, &pObj ) == 0)
                            {
                                DB.TryAdd(res.GetHashCode(), res);
                                return pObj;
                            }
                            else
                                throw new Exception("GetObjectPointer: new instance error");
                        }
                        else
                            throw new Exception("GetObjectPointer: get class error");
                    }
                        

                    break;
            }
            return IntPtr.Zero.ToPointer();
        }

        private static unsafe void* Java_app_quant_clr_CLRRuntime_nativeInvokeFunc(int hashCode, int len, void** args)
        {
            void*  pEnv;
            if(AttacheThread((void*)JVMPtr,&pEnv) != 0) throw new Exception ("Attach to thread error");
            void*  pNetBridgeClass;
            if(FindClass( pEnv, "app/quant/clr/CLRRuntime", &pNetBridgeClass) == 0)
            {
                void*  pArrayClassesMethod;
                if(GetStaticMethodID( pEnv, pNetBridgeClass, "ArrayClasses", "([Ljava/lang/Object;)[Ljava/lang/String;", &pArrayClassesMethod ) == 0)
                {

                    IntPtr pNetBridgeClassPtr = new IntPtr(pNetBridgeClass);
                    IntPtr ArrayClassesMethodPtr = new IntPtr(pArrayClassesMethod);

                    object[] classes_obj = getJavaArray(new IntPtr(args), "[Ljava/lang/Object;");

                    if(JVMDelegate.DB.ContainsKey(hashCode))
                    {
                        object res = JVMDelegate.DB[hashCode].Invoke(classes_obj);
                        return getObjectPointer(res);
                    }
                }
                else
                    throw new Exception("Get ArrayClasses method error");
            }
            else
                throw new Exception("Get CLRRuntime class error");

            return null;
        }
        private unsafe delegate void* SetInvokeFunc(int hashCode, int len, void** args);
        

        private static unsafe void Java_app_quant_clr_CLRRuntime_nativeSetProperty(int hashCode, string name, void** args)
        {
            void*  pEnv;
            if(AttacheThread((void*)JVMPtr,&pEnv) != 0) throw new Exception ("Attach to thread error");
            void*  pNetBridgeClass;
            if(FindClass( pEnv, "app/quant/clr/CLRRuntime", &pNetBridgeClass) == 0)
            {
                void*  pArrayClassesMethod;
                if(GetStaticMethodID( pEnv, pNetBridgeClass, "ArrayClasses", "([Ljava/lang/Object;)[Ljava/lang/String;", &pArrayClassesMethod ) == 0)
                {
                    IntPtr pNetBridgeClassPtr = new IntPtr(pNetBridgeClass);
                    IntPtr ArrayClassesMethodPtr = new IntPtr(pArrayClassesMethod);

                    object[] classes_obj = getJavaArray(new IntPtr(args), "[Ljava/lang/Object;");

                    if(DB.ContainsKey(hashCode))
                    {
                        object obj = DB[hashCode];
                        if(obj == null)
                            return;

                        
                        if(obj is DynamicObject)
                        {
                            Dynamic.InvokeSet(obj, name, obj);
                        }
                        else if(obj is ExpandoObject)
                        {
                            var exp = obj as IDictionary<string, object>;
                            exp.Add(name, obj);
                        }
                        else
                        {
                            FieldInfo field = obj.GetType().GetField(name);
                            PropertyInfo property = obj.GetType().GetProperty(name);

                            if(field != null)
                                field.SetValue(obj, classes_obj[0]);
                            else
                                property.SetValue(obj, classes_obj[0]);
                            }
                    }
                }
                else
                    throw new Exception("Get static ArrayClasses method");
            }
            else
                throw new Exception("Find CLRRuntime class error");
        }
        private unsafe delegate void SetSetProperty(int hashCode, string name, void** pObj);
        

        private static unsafe void* Java_app_quant_clr_CLRRuntime_nativeGetProperty(int hashCode, string name)
        {
            if(DB.ContainsKey(hashCode))
            {
                object obj = DB[hashCode];
                if(obj == null)
                    return null;

                
                if(obj is DynamicObject)
                {

                    var res = Dynamic.InvokeGet(obj, name);
                    
                    if(res == null)
                        return IntPtr.Zero.ToPointer();

                    return getObjectPointer((object)res);
                }
                else if(obj is ExpandoObject)
                {
                    var exp = obj as IDictionary<string, object>;
                    return getObjectPointer(exp[name]);
                }
                else
                {

                    FieldInfo field = obj.GetType().GetField(name);
                    PropertyInfo property = obj.GetType().GetProperty(name);
                    
                    object res;
                    
                    if(field != null)
                        res = field.GetValue(obj);
                    else if(property != null)
                        res = property.GetValue(obj);
                    else
                        return null;

                    return getObjectPointer(res);
                }
            }
            return null;
        }
        private unsafe delegate void* SetGetProperty(int hashCode, string name);

        public delegate T wrapFunction<T>(params object[] args);
        public delegate void wrapAction(params object[] args);

        public delegate T wrapGetProperty<T>();
        public delegate void wrapSetProperty(object args);

        private unsafe static void getJavaParameters(ref void** ar_call, object[] call_args)
        {
            void*  pEnv;
            if(AttacheThread((void*)JVMPtr,&pEnv) != 0) throw new Exception ("Attach to thread error");
            int call_len = call_args == null ? 0 : call_args.Length;

            for(int i = 0; i < call_len; i++)
            {
                var arg = call_args[i];

                if(arg is ObjectWrapper)
                 arg = (arg as ObjectWrapper).Object;
                
                var type = arg.GetType();

                switch(Type.GetTypeCode(type))
                { 
                    case TypeCode.Boolean:
                        bool bool_arg = (bool)arg;
                        ar_call[i] = *(void**)&bool_arg;
                        
                        break;

                    case TypeCode.Byte:
                        byte byte_arg = (byte)arg;
                        ar_call[i] = *(void**)&byte_arg;
                        
                        break;

                    case TypeCode.Char:
                        char char_arg = (char)arg;
                        ar_call[i] = *(void**)&char_arg;

                        break;

                    case TypeCode.Int16:
                        short short_arg = (short)arg;
                        ar_call[i] = *(void**)&short_arg;
                        
                        break;

                    case TypeCode.Int32: 
                        int int_arg = (int)arg;
                        ar_call[i] = *(void**)&int_arg;

                        break;
                        
                    case TypeCode.Int64:
                        long long_arg = (long)arg;
                        ar_call[i] = *(void**)&long_arg;
                        
                        break;

                    case TypeCode.Single:
                        float float_arg = (float)arg;
                        ar_call[i] = *(void**)&float_arg;
                        
                        break;

                    case TypeCode.Double:
                        double double_arg = (double)arg;
                        ar_call[i] = *(void**)&double_arg;
                        
                        break;

                    case TypeCode.String:
                        void* string_arg = GetJavaString(pEnv, (string)arg);
                        
                        ar_call[i] = string_arg;
                        break;

                    default:
                        
                        if(arg is JVMTuple)
                        {
                            JVMTuple jobj = arg as JVMTuple; 
                            void* ptr = (void *)(jobj.jVMObject.Pointer);
                            
                            ar_call[i] = ptr;
                        }

                        else if(arg is IJVMTuple)
                        {
                            IJVMTuple jobj = arg as IJVMTuple; 
                            void* ptr = (void *)(JVMObject.DB[jobj.JVMObject.JavaHashCode].Pointer);
                            
                            ar_call[i] = ptr;
                        }

                        else if(arg is JVMObject)
                        {
                            JVMObject jobj = arg as JVMObject; 
                            void* ptr = (void *)(jobj.Pointer);
                            
                            ar_call[i] = ptr;
                        }

                        else if(arg is Array)
                        {
                            Array sub = arg as Array;
                            JVMObject javaArray = getJavaArray(sub);
                            ar_call[i] = javaArray.Pointer.ToPointer();
                        }

                        else
                        {
                            void* ptr_res = (void *)(arg.GetHashCode());

                            void** pAr_len = stackalloc void*[2];
                            object[] pAr_len_data = new object[]{ arg.GetType().ToString(), arg.GetHashCode() };
                            getJavaParameters(ref pAr_len, pAr_len_data);

                            void* pObj;
                            void* CLRObjClass;
                            void*  pLoadClassMethod; // The executed method struct
                            if(FindClass( pEnv, "app/quant/clr/CLRObject", &CLRObjClass) == 0)
                            {
                                void* pClass;
                                if(NewObjectP( pEnv, CLRObjClass, "(Ljava/lang/String;I)V", 2, pAr_len, &pObj ) == 0)
                                {
                                    ar_call[i] = pObj;

                                    RegisterJVMObject(getHashCode(pObj) ,pObj);

                                    DB.TryAdd(arg.GetHashCode(), arg);
                                }
                                else
                                    throw new Exception("Create CLRObject instance error");        
                            }
                            else
                                throw new Exception("Get CLRObject class error");        
                        }
                        break;
                }
            }
        }

        public static unsafe DateTime GetNetDateTime(void* pEnv, void* pDate)
        {
            if(pDate != IntPtr.Zero.ToPointer())
            {
                void** pAr_len = stackalloc void*[0];
                object[] pAr_len_data = new object[]{  };
                getJavaParameters(ref pAr_len, pAr_len_data);

                void*  pDateClass;
                if(FindClass( pEnv, "java/time/LocalDateTime", &pDateClass) == 0)
                {

                    void*  pInvokeMethod;
                    if(GetMethodID( pEnv, pDate, "toString", "()Ljava/lang/String;", &pInvokeMethod ) == 0)
                    {
                        void*  pDateStr;
                        CallObjectMethod( pEnv, pDate, pInvokeMethod, &pDateStr, 0, pAr_len);

                        string str = GetNetString(pEnv, pDateStr);
                        return DateTime.Parse(str);
                    }
                    else
                        Console.WriteLine("LocalDateTime toString Method not found");
                }
                else
                    Console.WriteLine("LocalDateTime class not found");
            }

            return DateTime.MinValue;
        }

        public static unsafe void* GetJavaDateTime(void* pEnv, DateTime date)
        {
            void** pAr_len = stackalloc void*[7];
            object[] pAr_len_data = new object[]{ date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second, date.Millisecond * 1000000 };
            getJavaParameters(ref pAr_len, pAr_len_data);

            void*  pDateClass;
            if(FindClass( pEnv, "java/time/LocalDateTime", &pDateClass) == 0)
            {
                void*  pInvokeMethod;
                if(GetStaticMethodID( pEnv, pDateClass, "of", "(IIIIIII)Ljava/time/LocalDateTime;", &pInvokeMethod ) == 0)
                {

                    void* pDate;
                    if(CallStaticObjectMethod( pEnv, pDateClass, pInvokeMethod, &pDate, 7, pAr_len) == 0)
                        return pDate;
                    else
                        throw new Exception("GetJavaDateTime call LocalDateTime.of error");
                }
                else
                    Console.WriteLine("Date method not found");
            }
            else
                Console.WriteLine("Date class not found");

            return IntPtr.Zero.ToPointer();
        }

        private unsafe static JVMObject getJavaArray(object[] array)
        {
            void*  pEnv;
            if(AttacheThread((void*)JVMPtr,&pEnv) != 0) throw new Exception ("Attach to thread error");

            void*  pNetBridgeClass;
            if(FindClass( pEnv, "app/quant/clr/CLRRuntime", &pNetBridgeClass) == 0)
            {
                Array sub = array as Array;

                object lastObject = null;

                string cls = "";
                foreach(var o_s in sub)
                {
                    object res = o_s;
                    if(res is PyObject)
                    {
                        var pres = res as PyObject;
                        if(PyString.IsStringType(pres))
                            res = pres.AsManagedObject(typeof(string));

                        else if(PyFloat.IsFloatType(pres))
                            res = pres.AsManagedObject(typeof(float));

                        else if(PyInt.IsIntType(pres))
                            res = pres.AsManagedObject(typeof(int));

                        else if(PyDict.IsDictType(pres))
                            res = pres.AsManagedObject(typeof(Dictionary<object, object>));

                        else if(PyList.IsListType(pres))
                            res = pres.AsManagedObject(typeof(List<object>));

                        else if(PyLong.IsLongType(pres))
                            res = pres.AsManagedObject(typeof(long));

                        else if(PySequence.IsSequenceType(pres))
                            res = pres.AsManagedObject(typeof(IEnumerable<object>));

                        else if(PyTuple.IsTupleType(pres))
                            res = pres.AsManagedObject(typeof(System.Tuple));
                    }

                    object o = res;

                    string ocls = o is JVMObject ? ((JVMObject)o).JavaClass : Runtime.TransformType(o);

                    if(String.IsNullOrEmpty(cls))
                        cls = ocls;
                    else if(cls != ocls)
                    {
                        cls = "java/lang/Object";
                        break;
                    }
                    lastObject = o;
                }

                bool isObject = false;
                void*  pJArray;
                int arrLength = sub.Length;
                switch(cls)
                {
                    case "Z":
                        if(NewBooleanArray( pEnv, arrLength, &pJArray ) != 0)
                            throw new Exception("error creating new primitive array");
                        break;

                    case "B":
                        if(NewByteArray( pEnv, arrLength, &pJArray ) != 0)
                            throw new Exception("error creating new primitive array");
                        break;

                    case "C":
                        if(NewCharArray( pEnv, arrLength, &pJArray ) != 0)
                            throw new Exception("error creating new primitive array");
                        break;

                    case "S":
                        if(NewShortArray( pEnv, arrLength, &pJArray ) != 0)
                            throw new Exception("error creating new primitive array");
                        break;

                    case "I":
                        if(NewIntArray( pEnv, arrLength, &pJArray ) != 0)
                            throw new Exception("error creating new primitive array");
                        break;

                    case "J":
                        if(NewLongArray( pEnv, arrLength, &pJArray ) != 0)
                            throw new Exception("error creating new primitive array");
                        break;

                    case "F":
                        if(NewFloatArray( pEnv, arrLength, &pJArray ) != 0)
                            throw new Exception("error creating new primitive array");
                        break;

                    case "D":
                        if(NewDoubleArray( pEnv, arrLength, &pJArray ) != 0)
                            throw new Exception("error creating new primitive array");
                        break;

                    default:
                        isObject = true;

                        if(arrLength == 0)
                            return null;

                        if(!cls.Contains("java/lang/String"))
                            cls = "java/lang/Object";


                        if(NewObjectArray( pEnv, arrLength, cls, &pJArray ) != 0)
                            throw new Exception("error creating object array");
                        break;
                }

                
                for(int ii = 0; ii < arrLength; ii++)
                {
                    var sub_element = sub.GetValue(ii);
                    if(sub_element == null)
                        SetObjectArrayElement(pEnv, pJArray, ii, IntPtr.Zero.ToPointer());

                    else
                    {
                        object res = sub_element;
                        if(res is PyObject)
                        {
                            var pres = res as PyObject;
                            if(PyString.IsStringType(pres))
                                res = pres.AsManagedObject(typeof(string));

                            else if(PyFloat.IsFloatType(pres))
                                res = pres.AsManagedObject(typeof(float));

                            else if(PyInt.IsIntType(pres))
                                res = pres.AsManagedObject(typeof(int));

                            else if(PyDict.IsDictType(pres))
                                res = pres.AsManagedObject(typeof(Dictionary<object, object>));

                            else if(PyList.IsListType(pres))
                                res = pres.AsManagedObject(typeof(List<object>));

                            else if(PyLong.IsLongType(pres))
                                res = pres.AsManagedObject(typeof(long));

                            else if(PySequence.IsSequenceType(pres))
                                res = pres.AsManagedObject(typeof(IEnumerable<object>));

                            else if(PyTuple.IsTupleType(pres))
                                res = pres.AsManagedObject(typeof(System.Tuple));
                        }

                        sub_element = res;
                        var sub_type = sub_element.GetType();
                        switch(Type.GetTypeCode(sub_type))
                        { 
                            case TypeCode.Boolean:
                                if(!isObject)
                                    SetBooleanArrayElement(pEnv, pJArray, ii, (bool)sub_element);
                                else
                                {
                                    void* pObjBool;
                                    NewBooleanObject(pEnv, (bool)sub_element, &pObjBool);
                                    SetObjectArrayElement(pEnv, pJArray, ii, pObjBool);
                                }

                                break;

                            case TypeCode.Byte:
                                if(!isObject)
                                    SetByteArrayElement(pEnv, pJArray, ii, (byte)sub_element);
                                else
                                {
                                    void* pObjB;
                                    NewByteObject(pEnv, (byte)sub_element, &pObjB);
                                    SetObjectArrayElement(pEnv, pJArray, ii, pObjB);
                                }
                                break;

                            case TypeCode.Char:
                                if(!isObject)
                                    SetCharArrayElement(pEnv, pJArray, ii, (char)sub_element);
                                else
                                {
                                    void* pObjC;
                                    NewCharacterObject(pEnv, (char)sub_element, &pObjC);
                                    SetObjectArrayElement(pEnv, pJArray, ii, pObjC);
                                }
                                break;

                            case TypeCode.Int16:
                                if(!isObject)
                                    SetShortArrayElement(pEnv, pJArray, ii, (short)sub_element);
                                else
                                {
                                    void* pObjS;
                                    NewShortObject(pEnv, (short)sub_element, &pObjS);
                                    SetObjectArrayElement(pEnv, pJArray, ii, pObjS);
                                }
                                break;

                            case TypeCode.Int32: 
                                if(!isObject)
                                    SetIntArrayElement(pEnv, pJArray, ii, (int)sub_element);
                                else
                                {
                                    void* pObjI;
                                    NewIntegerObject(pEnv, (int)sub_element, &pObjI);
                                    SetObjectArrayElement(pEnv, pJArray, ii, pObjI);
                                }
                                break;
                                
                            case TypeCode.Int64:
                                if(!isObject)
                                    SetLongArrayElement(pEnv, pJArray, ii, (long)sub_element);
                                else
                                {
                                    void* pObjL;
                                    NewLongObject(pEnv, (long)sub_element, &pObjL);
                                    SetObjectArrayElement(pEnv, pJArray, ii, pObjL);
                                }
                                break;

                            case TypeCode.Single:
                                if(!isObject)
                                    SetFloatArrayElement(pEnv, pJArray, ii, (float)sub_element);
                                else
                                {
                                    void* pObjF;
                                    NewFloatObject(pEnv, (float)sub_element, &pObjF);
                                    SetObjectArrayElement(pEnv, pJArray, ii, pObjF);
                                }
                                break;

                            case TypeCode.Double:
                                if(!isObject)
                                    SetDoubleArrayElement(pEnv, pJArray, ii, (double)sub_element);
                                else
                                {
                                    void* pObjD;
                                    NewDoubleObject(pEnv, (double)sub_element, &pObjD);
                                    SetObjectArrayElement(pEnv, pJArray, ii, pObjD);
                                }
                                break;

                            case TypeCode.String:
                                void* string_arg_s = GetJavaString(pEnv, (string)sub_element);
                                SetObjectArrayElement(pEnv, pJArray, ii, string_arg_s);
                                break;

                            case TypeCode.DateTime:
                                void* pDate = GetJavaDateTime(pEnv, (DateTime)sub_element);
                                SetObjectArrayElement(pEnv, pJArray, ii, pDate);
                                break;

                            default:

                                if(JVMDelegate.DB.ContainsKey(sub_element.GetHashCode()))
                                {
                                    JVMDelegate jobj = sub_element as JVMDelegate; 
                                    void* ptr = (void *)(jobj.Pointer);
                                    SetObjectArrayElement(pEnv, pJArray, ii, ptr);
                                }

                                else if(DB.ContainsKey(sub_element.GetHashCode()))
                                {
                                    void* ptr_res = (void *)(sub_element.GetHashCode());

                                    void*  pGetCLRObjectMethod;
                                    GetStaticMethodID( pEnv, pNetBridgeClass, "GetCLRObject", "(I)Lapp/quant/clr/CLRObject;", &pGetCLRObjectMethod );

                                    void** pAr_len = stackalloc void*[1];
                                    object[] pAr_len_data = new object[]{ sub_element.GetHashCode() };
                                    getJavaParameters(ref pAr_len, pAr_len_data);

                                    void*  pGetCLRObject;
                                    CallStaticObjectMethod( pEnv, pNetBridgeClass, pGetCLRObjectMethod, &pGetCLRObject, 1, pAr_len);

                                    SetObjectArrayElement(pEnv, pJArray, ii, pGetCLRObject);
                                }

                                else if(sub_element is JVMTuple)
                                {
                                    JVMTuple jobj = sub_element as JVMTuple; 
                                    void* ptr = (void *)(jobj.jVMObject.Pointer);
                                    SetObjectArrayElement(pEnv, pJArray, ii, ptr);
                                }
                                else if(sub_element is IJVMTuple)
                                {
                                    IJVMTuple jobj = sub_element as IJVMTuple; 
                                    void* ptr = (void *)(jobj.JVMObject.Pointer);
                                    SetObjectArrayElement(pEnv, pJArray, ii, ptr);
                                }

                                else if(sub_element is JVMObject)
                                {
                                    JVMObject jobj = sub_element as JVMObject; 
                                    void* ptr = (void *)(jobj.Pointer);
                                    SetObjectArrayElement(pEnv, pJArray, ii, ptr);
                                }

                                else if(sub_element is Array)
                                {                                    
                                    Array sub_array = sub_element as Array;
                                    JVMObject javaArray = getJavaArray(sub_array);

                                    SetObjectArrayElement(pEnv, pJArray, ii, javaArray.Pointer.ToPointer());
                                }

                                else if(sub_element is IEnumerable<object>)
                                {
                                    void* ptr_res = (void *)(sub_element.GetHashCode());

                                    void** pAr_len = stackalloc void*[2];
                                    object[] pAr_len_data = new object[]{ sub_element.GetType().ToString(), sub_element.GetHashCode() };
                                    getJavaParameters(ref pAr_len, pAr_len_data);

                                    void* pObj;
                                    void* CLRObjClass;
                                    void*  pLoadClassMethod; // The executed method struct
                                    if(FindClass( pEnv, "app/quant/clr/CLRIterable", &CLRObjClass) == 0)
                                    {
                                        void* pClass;
                                        if(NewObjectP( pEnv, CLRObjClass, "(Ljava/lang/String;I)V", 2, pAr_len, &pObj ) == 0)
                                        {
                                            RegisterJVMObject(getHashCode(pObj) ,pObj);
                                            DB.TryAdd(res.GetHashCode(), res);
                                            SetObjectArrayElement(pEnv, pJArray, ii, pObj);
                                            //return pObj;
                                        }
                                        else
                                            throw new Exception("GetObjectPointer: new instance error");
                                    }
                                    else
                                        throw new Exception("GetObjectPointer: get class error");
                                }

                                else if(res is IEnumerator<object>)
                                {
                                    void* ptr_res = (void *)(res.GetHashCode());

                                    void** pAr_len = stackalloc void*[2];
                                    object[] pAr_len_data = new object[]{ res.GetType().ToString(), res.GetHashCode() };
                                    getJavaParameters(ref pAr_len, pAr_len_data);

                                    void* pObj;
                                    void* CLRObjClass;
                                    void*  pLoadClassMethod; // The executed method struct
                                    if(FindClass( pEnv, "app/quant/clr/CLRIterator", &CLRObjClass) == 0)
                                    {
                                        void* pClass;
                                        if(NewObjectP( pEnv, CLRObjClass, "(Ljava/lang/String;I)V", 2, pAr_len, &pObj ) == 0)
                                        {
                                            RegisterJVMObject(getHashCode(pObj) ,pObj);
                                            DB.TryAdd(res.GetHashCode(), res);
                                            SetObjectArrayElement(pEnv, pJArray, ii, pObj);
                                        }
                                        else
                                            throw new Exception("GetObjectPointer: new instance error");
                                    }
                                    else
                                        throw new Exception("GetObjectPointer: get class error");
                                }

                                else
                                {
                                    void* ptr_res = (void *)(sub_element.GetHashCode());

                                    void** pAr_len = stackalloc void*[2];
                                    object[] pAr_len_data = new object[]{ sub_element.GetType().ToString(), sub_element.GetHashCode() };
                                    getJavaParameters(ref pAr_len, pAr_len_data);

                                    void* pObj;
                                    void* CLRObjClass;
                                    void*  pLoadClassMethod; // The executed method struct
                                    if(FindClass( pEnv, "app/quant/clr/CLRObject", &CLRObjClass) == 0)
                                    {
                                        void* pClass;
                                        if(NewObjectP( pEnv, CLRObjClass, "(Ljava/lang/String;I)V", 2, pAr_len, &pObj ) == 0)
                                        {
                                            RegisterJVMObject(getHashCode(pObj) ,pObj);
                                            SetObjectArrayElement(pEnv, pJArray, ii, pObj);
                                            DB.TryAdd(sub_element.GetHashCode(), sub_element);
                                        }
                                        else
                                            throw new Exception("GetObjectPointer: new instance error");
                                    }
                                    else
                                        throw new Exception("GetObjectPointer: get class error");
                                }
                                break;
                        }
                        
                    }
                }

                int hashID = getHashCode(pJArray);

                return new JVMObject(new IntPtr(pJArray), hashID, cls);
            
            }
            else
                throw new Exception("get CLRRuntime class error");
        }

        private unsafe static object[] getJavaArray(IntPtr ObjectPtr, string returnSignature)//, IntPtr pNetBridgeClassPtr, IntPtr ArrayClassesMethodPtr)
        {
            void*  pEnv;
            if(AttacheThread((void*)JVMPtr,&pEnv) != 0) throw new Exception ("Attach to thread error");
            void*  pNetBridgeClass;
            FindClass( pEnv, "app/quant/clr/CLRRuntime", &pNetBridgeClass);

            void*  pArrayClassesMethod;
            GetStaticMethodID( pEnv, pNetBridgeClass, "ArrayClasses", "([Ljava/lang/Object;)[Ljava/lang/String;", &pArrayClassesMethod );

            IntPtr pNetBridgeClassPtr = new IntPtr(pNetBridgeClass);
            IntPtr ArrayClassesMethodPtr = new IntPtr(pArrayClassesMethod);

            void* pObjResult = ObjectPtr.ToPointer();
            int HashCode = getHashCode(pObjResult);

            JVMObject ret_arr = new JVMObject(new IntPtr(pObjResult), HashCode, returnSignature);

            void* _pNetBridgeClass = (void*)pNetBridgeClassPtr;

            int ret_arr_len = getArrayLength(ret_arr);
            object[] resultArray = new object[ret_arr_len];


            void* pArrClasses = IntPtr.Zero.ToPointer();
            if(returnSignature == "[Z")
            {
                for(int i = 0; i < ret_arr_len; i++)
                    resultArray[i] = GetBooleanArrayElement(pEnv, pObjResult, i);
            }
            else if(returnSignature == "[B")
            {
                for(int i = 0; i < ret_arr_len; i++)
                    resultArray[i] = GetByteArrayElement(pEnv, pObjResult, i);
            }
            else if(returnSignature == "[C")
            {
                for(int i = 0; i < ret_arr_len; i++)
                    resultArray[i] = GetCharArrayElement(pEnv, pObjResult, i);
            }
            else if(returnSignature == "[S")
            {
                for(int i = 0; i < ret_arr_len; i++)
                    resultArray[i] = GetShortArrayElement(pEnv, pObjResult, i);
            }
            else if(returnSignature == "[I")
            {
                for(int i = 0; i < ret_arr_len; i++)
                    resultArray[i] = GetIntArrayElement(pEnv, pObjResult, i);
            }
            else if(returnSignature == "[J")
            {
                for(int i = 0; i < ret_arr_len; i++)
                    resultArray[i] = GetLongArrayElement(pEnv, pObjResult, i);
            }
            else if(returnSignature == "[F")
            {
                for(int i = 0; i < ret_arr_len; i++)
                    resultArray[i] = GetFloatArrayElement(pEnv, pObjResult, i);
            }
            else if(returnSignature == "[D")
            {
                for(int i = 0; i < ret_arr_len; i++)
                    resultArray[i] = GetDoubleArrayElement(pEnv, pObjResult, i);
            }
            else
            {
                void* pArrClassesMethodID = ArrayClassesMethodPtr.ToPointer();
                
                void** pArg_ArrClassesMethod = stackalloc void*[1];
                object[] ar_data_pArrClasses = new object[]{ ret_arr };
                getJavaParameters(ref pArg_ArrClassesMethod, ar_data_pArrClasses);
                
                CallStaticObjectMethod( pEnv, _pNetBridgeClass, pArrClassesMethodID, &pArrClasses, 1, pArg_ArrClassesMethod);
                
                for(int i = 0; i < ret_arr_len; i++)
                {
                    
                    void* pElementClass;
                    GetObjectArrayElement(pEnv, pArrClasses, i, &pElementClass);
                    
                    if(new IntPtr(pElementClass) == IntPtr.Zero)
                    {
                        resultArray[i] = null;
                    }
                    else
                    {
                        string retElementClass = GetNetString(pEnv, pElementClass);
                        

                        if(retElementClass.StartsWith("prim-"))
                        {
                            retElementClass = retElementClass.Replace("prim-","");
                            string ttype = retElementClass.Substring(0, retElementClass.LastIndexOf("-"));
                            string value = retElementClass.Substring(retElementClass.LastIndexOf("-") + 1);

                            switch(ttype)
                            {
                                case "java.lang.Boolean":
                                    resultArray[i] = Boolean.Parse(value);
                                    break;
                                case "java.lang.Byte":
                                    resultArray[i] = Byte.Parse(value);
                                    break;
                                case "java.lang.Character":
                                    resultArray[i] = Char.Parse(value);
                                    break;
                                case "java.lang.Short":
                                    resultArray[i] = Int16.Parse(value);
                                    break;
                                case "java.lang.Integer":
                                    resultArray[i] = Int32.Parse(value);
                                    
                                    break;
                                case "java.lang.Long":
                                    resultArray[i] = Int64.Parse(value);
                                    break;
                                case "java.lang.Float":
                                    resultArray[i] = Single.Parse(value);
                                    break;
                                case "java.lang.Double":
                                    resultArray[i] = Double.Parse(value);

                                    break;
                            }

                        }
                        else if(retElementClass == "java.lang.String")
                        {
                            void* pElement_string;
                            GetObjectArrayElement(pEnv, pObjResult, i, &pElement_string);
                            if(IntPtr.Zero.ToPointer() == pElement_string)
                                resultArray[i] = null;
                            else
                                resultArray[i] = GetNetString(pEnv, pElement_string);
                        }
                        else if(retElementClass == "java.util.LocalDateTime")
                        {
                            void* pElement_date;
                            GetObjectArrayElement(pEnv, pObjResult, i, &pElement_date);
                            if(IntPtr.Zero.ToPointer() == pElement_date)
                                resultArray[i] = null;
                            else
                                resultArray[i] = GetNetDateTime(pEnv, pElement_date);
                        }
                        else
                        {
                            void* pElement_object;
                            GetObjectArrayElement(pEnv, pObjResult, i, &pElement_object);
                            
                            if(IntPtr.Zero.ToPointer() == pElement_object)
                            {
                                Console.WriteLine("NULL OBJ: " + i);
                                resultArray[i] = null;
                            }
                            else
                            {
                                IntPtr returnPtr = new IntPtr(pElement_object);

                                if(!retElementClass.StartsWith("["))
                                {
                                    int hashID_res = getHashCode(pElement_object);

                                    if(JVMDelegate.DB.ContainsKey(hashID_res)) //check if it is a JVMDelegate
                                        resultArray[i] = JVMDelegate.DB[hashID_res].func;

                                    else if(JVMObject.DB.ContainsKey(hashID_res)) //check if it is a JVMObject
                                        resultArray[i] = JVMObject.DB[hashID_res];
                                    
                                    else if(Runtime.DB.ContainsKey(hashID_res)) //check if it is a CLRObject
                                    {
                                        resultArray[i] = Runtime.DB[hashID_res];
                                    }

                                    else
                                    {
                                        string cls = retElementClass.StartsWith("L") && retElementClass.EndsWith(";") ? retElementClass.Substring(1).Replace(";","") : retElementClass;

                                        resultArray[i] =  getObject(pEnv, cls, pElement_object);
                                    }
                                }
                                else
                                {
                                    resultArray[i] =  getJavaArray(returnPtr, retElementClass);
                                }
                            }
                        }
                        
                    }
                }
                
            }

            return resultArray;
            
        }

        private unsafe static JVMObject getJavaArray(IEnumerable<object> array)
        {
            int arrLength = array.Count();
            object[] res = new object[arrLength];

            for(int i = 0; i < arrLength; i++)
                res[i] = array.ElementAt(i);
            

            return getJavaArray(res);
        }

        private unsafe static JVMObject getJavaArray(Array array)
        {
            int arrLength = array.Length;
            object[] res = new object[arrLength];

            for(int i = 0; i < arrLength; i++)
                res[i] = array.GetValue(i);

            return getJavaArray(res);
            
        }

        public unsafe static object Python(System.Func<object[], object> func)
        {
            using(Py.GIL())
            {
                return func(null);
            }
        }

        internal static String TransformType(Type type)
        {
            switch(Type.GetTypeCode(type))
            { 
                case TypeCode.Boolean:
                    return "Z";

                case TypeCode.Byte:
                    return "B";

                case TypeCode.Char:
                    return "C";

                case TypeCode.Int16:
                    return "S";

                case TypeCode.Int32: 
                    return "I";
                    
                case TypeCode.Int64:
                    return "J";

                case TypeCode.Single:
                    return "F";

                case TypeCode.Double:
                    return "D";

                case TypeCode.Empty:
                    return "V";

                case TypeCode.String:
                    return "Ljava/lang/String;";

                case TypeCode.DateTime:
                    return "Ljava/time/LocalDateTime;";
                
                default:
                    if(type == typeof(ObjectWrapper))
                        return "Ljava/lang/Object;";

                    if(type == typeof(void))
                        return "V";
                    
                    else if(type.IsArrayOf<Boolean>())
                        return "[Z";

                    else if(type.IsArrayOf<Byte>())
                        return "[B";

                    else if(type.IsArrayOf<Char>())
                        return "[C";

                    else if(type.IsArrayOf<Int16>())
                        return "[S";

                    else if(type.IsArrayOf<Int32>())
                        return "[I";

                    else if(type.IsArrayOf<Int64>())
                        return "[J";

                    else if(type.IsArrayOf<Single>())
                        return "[F";

                    else if(type.IsArrayOf<Double>())
                        return "[D";

                    else if(type.IsArrayOf<string>())
                        return "[Ljava/lang/String;";

                    else if(type.IsArrayOf<DateTime>())
                        return "[java/time/LocalDateTime;";

                    else if(type.IsArrayOf<object>())
                        return "[Ljava/lang/Object;";

                    else
                        return "Ljava/lang/Object;";

            } 
        }
        internal static String TransformType(Object obj)
        {
            if(obj == null)
                return "Ljava/lang/Object;";

            Type type = obj.GetType();
            switch(Type.GetTypeCode(type))
            { 
                case TypeCode.Boolean:
                    return "Z";

                case TypeCode.Byte:
                    return "B";

                case TypeCode.Char:
                    return "C";

                case TypeCode.Int16:
                    return "S";

                case TypeCode.Int32: 
                    return "I";
                    
                case TypeCode.Int64:
                    return "J";

                case TypeCode.Single:
                    return "F";

                case TypeCode.Double:
                    return "D";

                case TypeCode.Empty:
                    return "V";

                case TypeCode.String:
                    return "Ljava/lang/String;";

                case TypeCode.DateTime:
                    return "Ljava/time/LocalDateTime;";
                
                default:
                    if(obj is ObjectWrapper)
                        return "Ljava/lang/Object;";
                    else if(obj is Array)
                    {
                        var arr = obj as Array;
                        string cls = "";
                        foreach(var o in arr)
                        {
                            string ocls = o is JVMObject ? "L" + ((JVMObject)o).JavaClass + ";" : TransformType(o);
                            if(String.IsNullOrEmpty(cls))
                                cls = ocls;
                            else if(cls != ocls)
                            {
                                cls = "Ljava/lang/Object;";
                                break;
                            }
                        }
                        return "[" + cls;
                    }
                    else if(obj is JVMObject)
                    {
                        string cls = ((JVMObject)obj).JavaClass.Replace(".","/");
                        if(cls.Length > 1)
                            cls = "L" + cls + ";";
                        return cls;
                    }
                    else
                        return "Ljava/lang/Object;";

            } 
        }

        private unsafe static int getHashCode(void* pObj)
        {
            if(pObj == IntPtr.Zero.ToPointer())
                return 0;

            void*  pEnv;
            if(AttacheThread((void*)JVMPtr,&pEnv) == 0)
            {
                void** sig_hash_ar_call = stackalloc void*[1];
                getJavaParameters(ref sig_hash_ar_call, null);
                
                void*  pMethodSigHashCode;
                if(GetMethodID( pEnv, pObj, "hashCode", "()I", &pMethodSigHashCode ) == 0)
                    return CallIntMethod( pEnv, pObj, pMethodSigHashCode, 0, sig_hash_ar_call);
                else
                    throw new Exception("get hashCode method error");
                
            }
            else
                throw new Exception("Attach thread error");
        }

        private unsafe static bool isIterable(void* pObj)
        {
            if(pObj == IntPtr.Zero.ToPointer())
                return false;

            void*  pEnv;
            if(AttacheThread((void*)JVMPtr,&pEnv) == 0)
            {
                void* pNetBridgeClass;
                if(FindClass( pEnv, "app/quant/clr/CLRRuntime", &pNetBridgeClass) == 0 )
                {
                    void** pArg_lcs = stackalloc void*[1];
                    pArg_lcs[0] = *(void**)&pObj;
                    
                    void*  pMethodSigHashCode;
                    if(GetStaticMethodID( pEnv, pNetBridgeClass, "isIterable", "(Ljava/lang/Object;)Z", &pMethodSigHashCode ) == 0)
                        return CallStaticBooleanMethod( pEnv, pNetBridgeClass, pMethodSigHashCode, 1, pArg_lcs);
                    else
                        throw new Exception("get isIterable method error");
                }
                else
                    throw new Exception("isIterable get class error");
                
            }
            else
                throw new Exception("Attach thread error");
        }

        private unsafe static bool isMap(void* pObj)
        {
            if(pObj == IntPtr.Zero.ToPointer())
                return false;

            void*  pEnv;
            if(AttacheThread((void*)JVMPtr,&pEnv) == 0)
            {
                void* pNetBridgeClass;
                if(FindClass( pEnv, "app/quant/clr/CLRRuntime", &pNetBridgeClass) == 0 )
                {
                    void** pArg_lcs = stackalloc void*[1];
                    pArg_lcs[0] = *(void**)&pObj;
                    
                    void*  pMethodSigHashCode;
                    if(GetStaticMethodID( pEnv, pNetBridgeClass, "isMap", "(Ljava/lang/Object;)Z", &pMethodSigHashCode ) == 0)
                        return CallStaticBooleanMethod( pEnv, pNetBridgeClass, pMethodSigHashCode, 1, pArg_lcs);
                    else
                        throw new Exception("get isMap method error");
                }
                else
                    throw new Exception("isMap get class error");
                
            }
            else
                throw new Exception("Attach thread error");
        }

        private unsafe static bool isCollection(void* pObj)
        {
            if(pObj == IntPtr.Zero.ToPointer())
                return false;

            void*  pEnv;
            if(AttacheThread((void*)JVMPtr,&pEnv) == 0)
            {
                void* pNetBridgeClass;
                if(FindClass( pEnv, "app/quant/clr/CLRRuntime", &pNetBridgeClass) == 0 )
                {
                    void** pArg_lcs = stackalloc void*[1];
                    pArg_lcs[0] = *(void**)&pObj;
                    
                    void*  pMethodSigHashCode;
                    if(GetStaticMethodID( pEnv, pNetBridgeClass, "isCollection", "(Ljava/lang/Object;)Z", &pMethodSigHashCode ) == 0)
                        return CallStaticBooleanMethod( pEnv, pNetBridgeClass, pMethodSigHashCode, 1, pArg_lcs);
                    else
                        throw new Exception("get isCollection method error");
                }
                else
                    throw new Exception("isCollection get class error");
                
            }
            else
                throw new Exception("Attach thread error");
        }

        private unsafe static int getClass(void* pObj, ref void* pClass)
        {
            if(pObj == IntPtr.Zero.ToPointer())
            {
                pClass = IntPtr.Zero.ToPointer();
                return -2;
            }
            void*  pEnv;// = (void*)EnvPtr;
            if(AttacheThread((void*)JVMPtr,&pEnv) == 0)
            {

                void* pNameClass;
                void* _pClass;
                if(GetObjectClass(pEnv, pObj, &_pClass, &pNameClass) == 0)
                {
                    pClass = _pClass;
                    return 0;
                }
                return -1;
                
            }
            return -2;
        }

        private unsafe static int getClassName(void* pObj, ref string cName)
        {
            if(pObj == IntPtr.Zero.ToPointer())
            {
                cName =  null;
                return -2;
            }
            void*  pEnv;// = (void*)EnvPtr;
            if(AttacheThread((void*)JVMPtr,&pEnv) == 0)
            {

                void* pNameClass;
                void* _pClass;
                if(GetObjectClass(pEnv, pObj, &_pClass, &pNameClass) == 0)
                {

                    cName = GetNetString(pEnv, pNameClass);
                    return 0;
                }
                return -1;
                
            }
            return -2;
        }

        private unsafe static int getArrayLength(JVMObject sig_arr)
        {
            void*  pEnv;// = (void*)EnvPtr;
            if(AttacheThread((void*)JVMPtr,&pEnv) != 0) throw new Exception ("Attach to thread error");

            void* pArrayClass;
            if(FindClass( pEnv, "java/lang/reflect/Array", &pArrayClass) == 0)
            {
                void* pArrayLengthMethod;
                if(GetStaticMethodID( pEnv, pArrayClass, "getLength", "(Ljava/lang/Object;)I", &pArrayLengthMethod) == 0)
                {
                    void** pAr_len = stackalloc void*[1];
                    object[] pAr_len_data = new object[]{ sig_arr };
                    getJavaParameters(ref pAr_len, pAr_len_data);

                    return CallStaticIntMethod( pEnv, pArrayClass, pArrayLengthMethod, 1, pAr_len);
                }
                else
                    throw new Exception("getLength get method error");
            }
            else
                throw new Exception("get Array class error");
        }
        
        public unsafe static void SetClassPath(string[] paths)
        {
            // Console.WriteLine("SET CLASSPATH: " + path);
            void*  pEnv;// = (void*)EnvPtr;
            if(AttacheThread((void*)JVMPtr,&pEnv) != 0) throw new Exception ("Attach to thread error");
            void* pNetBridgeClass;
            void* pSetPathMethod;

            if(FindClass( pEnv, "app/quant/clr/CLRRuntime", &pNetBridgeClass) == 0 )
            {
                if( GetStaticMethodID( pEnv, pNetBridgeClass, "SetBaseClassPath", "([Ljava/lang/String;)V", &pSetPathMethod ) == 0 )
                {
                    void** pArg_lcs = stackalloc void*[1];
                    // object[] ar_data = new object[]{ new string[]{ path } };
                    object[] ar_data = new object[]{ paths };
                    getJavaParameters(ref pArg_lcs, ar_data);
                    if(CallStaticVoidMethod( pEnv, pNetBridgeClass, pSetPathMethod, 1, pArg_lcs) != 0)
                        Console.WriteLine("JAVA path not set..." + paths);
                }
                else
                    Console.WriteLine("SetBaseClassPath method not found");
            }
        }

        public static string[] Signature(object obj)
        {
            MethodInfo[] methodInfos = (obj is Type ? obj as Type : obj.GetType()).GetMethods();

            var arr = new List<string>();

            for(int i = 0; i < methodInfos.Length; i++)
            {
                var m = methodInfos[i];

                if(m.IsPublic)
                {
                    string sig = (m.IsStatic ? "S-" : "") + "M/" + m.Name + "-(";
                    foreach(var p in m.GetParameters())
                        sig += TransformType(p.ParameterType);
                    sig += ")" + TransformType(m.ReturnType);


                    arr.Add(sig);
                }

            }

            return arr.ToArray();
        }
        public unsafe static void RegisterJVMObject(int hashCode, void* pObj)
        {
            
            void*  pEnv;
            if(AttacheThread((void*)JVMPtr,&pEnv) != 0) throw new Exception ("Attach to thread error");
            void* pNetBridgeClass;
            void* pSetPathMethod;

            if(FindClass( pEnv, "app/quant/clr/CLRRuntime", &pNetBridgeClass) == 0 )
            {
                if( GetStaticMethodID( pEnv, pNetBridgeClass, "RegisterObject", "(ILjava/lang/Object;)V", &pSetPathMethod ) == 0 )
                {
                    void** pArg_lcs = stackalloc void*[2];
                    pArg_lcs[0] = *(void**)&hashCode;
                    pArg_lcs[1] = pObj;
                    
                    if(CallStaticVoidMethod( pEnv, pNetBridgeClass, pSetPathMethod, 2, pArg_lcs) != 0)
                        Console.WriteLine("JAVA Object not registered...");
                }
                else
                    Console.WriteLine("RegisterObject method not found");
            }
        }

        public unsafe static void* GetJVMObject(int hashCode)
        {
            
            void*  pEnv;
            if(AttacheThread((void*)JVMPtr,&pEnv) != 0) throw new Exception ("Attach to thread error");
            void* pNetBridgeClass;
            void* pGetJVMObjectMethod;

            if(FindClass( pEnv, "app/quant/clr/CLRRuntime", &pNetBridgeClass) == 0 )
            {
                if( GetStaticMethodID( pEnv, pNetBridgeClass, "GetObject", "(I)Ljava/lang/Object;", &pGetJVMObjectMethod ) == 0 )
                {
                    void** pAr_len = stackalloc void*[1];
                    object[] pAr_len_data = new object[]{ hashCode };
                    getJavaParameters(ref pAr_len, pAr_len_data);

                    void*  pGetJVMObject;
                    CallStaticObjectMethod( pEnv, pNetBridgeClass, pGetJVMObjectMethod, &pGetJVMObject, 1, pAr_len);
                    
                    return pGetJVMObject;
                }
                else
                    Console.WriteLine("GetObject method not found");
            }

            return IntPtr.Zero.ToPointer();
        }
        
        public unsafe static object getObject(void* _pEnv, string cls, void* pObjResult)
        {
            string cName = null;
            IntPtr returnPtr = new IntPtr(pObjResult);

            if(getClassName(pObjResult, ref cName) == 0)
            {
                switch(cName)
                {
                    case "java.lang.Boolean":
                        void*  pInvokeMethod_boolean;
                        if(GetMethodID( _pEnv, pObjResult, "booleanValue", "()Z", &pInvokeMethod_boolean ) == 0)
                        {
                            void** pAr_boolean = stackalloc void*[1];
                            return CallBooleanMethod( _pEnv, pObjResult, pInvokeMethod_boolean, 1, pAr_boolean);
                        }
                        else
                            throw new Exception("Error in booleanValue of object result");

                    case "java.lang.Byte":
                        void*  pInvokeMethod_byte;
                        if(GetMethodID( _pEnv, pObjResult, "byteValue", "()B", &pInvokeMethod_byte ) == 0)
                        {
                            void** pAr_byte = stackalloc void*[1];
                            return CallByteMethod( _pEnv, pObjResult, pInvokeMethod_byte, 1, pAr_byte);
                        }
                        else
                            throw new Exception("Error in byteValue of object result");
                    

                    case "java.lang.Character":
                        void*  pInvokeMethod_char;
                        if(GetMethodID( _pEnv, pObjResult, "charValue", "()C", &pInvokeMethod_char ) == 0)
                        {
                            void** pAr_char = stackalloc void*[1];
                            return CallCharMethod( _pEnv, pObjResult, pInvokeMethod_char, 1, pAr_char);
                        }
                        else
                            throw new Exception("Error in charValue of object result");

                    case "java.lang.Short":
                        void*  pInvokeMethod_short;

                        if(GetMethodID( _pEnv, pObjResult, "shortValue", "()S", &pInvokeMethod_short ) == 0)
                        {
                            void** pAr_short = stackalloc void*[1];
                            return CallShortMethod( _pEnv, pObjResult, pInvokeMethod_short, 1, pAr_short);
                        }
                        else
                            throw new Exception("Error in shortValue of object result");


                    case "java.lang.Integer":
                        void*  pInvokeMethod_int;
                        if(GetMethodID( _pEnv, pObjResult, "intValue", "()I", &pInvokeMethod_int ) == 0)
                        {
                            void** pAr_int = stackalloc void*[1];
                            return CallIntMethod( _pEnv, pObjResult, pInvokeMethod_int, 1, pAr_int);
                        }
                        else
                            throw new Exception("Error in intValue of object result");

                    case "java.lang.Long":
                        void*  pInvokeMethod_long;
                        if(GetMethodID( _pEnv, pObjResult, "longValue", "()J", &pInvokeMethod_long ) == 0)
                        {
                            void** pAr_long = stackalloc void*[1];
                            return CallLongMethod( _pEnv, pObjResult, pInvokeMethod_long, 1, pAr_long);
                        }
                        else
                            throw new Exception("Error in longValue of object result");

                    case "java.lang.Float":
                        void*  pInvokeMethod_float;
                        if(GetMethodID( _pEnv, pObjResult, "floatValue", "()F", &pInvokeMethod_float ) == 0)
                        {
                            void** pAr_float = stackalloc void*[1];
                            return CallFloatMethod( _pEnv, pObjResult, pInvokeMethod_float, 1, pAr_float);
                        }
                        else
                            throw new Exception("Error in floatValue of object result");


                    case "java.lang.Double":
                        void*  pInvokeMethod_double;
                        if(GetMethodID( _pEnv, pObjResult, "doubleValue", "()D", &pInvokeMethod_double ) == 0)
                        {
                            void** pAr_double = stackalloc void*[1];
                            return CallDoubleMethod( _pEnv, pObjResult, pInvokeMethod_double, 1, pAr_double);
                        }
                        else
                            throw new Exception("Error in doubleValue of object result");

                    case "java.lang.String":
                        return GetNetString(_pEnv, pObjResult);

                    case "java.util.LocalDateTime":
                        return GetNetDateTime(_pEnv, pObjResult);

                    case "scala.Tuple1":
                    {
                        dynamic _tuple = CreateInstancePtr(cName, null, returnPtr, null );
                        JVMTuple1 tuple = new JVMTuple1(_tuple, _tuple._1());
                        JVMObject.DB[_tuple.JavaHashCode] = new JVMTuple(_tuple, tuple);
                        return tuple;
                    }
                    case "scala.Tuple2":
                    {
                        dynamic _tuple = CreateInstancePtr(cName, null, returnPtr, null );
                        JVMTuple2 tuple = new JVMTuple2(_tuple, _tuple._1(), _tuple._2());
                        JVMObject.DB[_tuple.JavaHashCode] = new JVMTuple(_tuple, tuple);
                        return tuple;
                    }
                    case "scala.Tuple3":
                    {
                        dynamic _tuple = CreateInstancePtr(cName, null, returnPtr, null );
                        JVMTuple3 tuple = new JVMTuple3(_tuple, _tuple._1(), _tuple._2(), _tuple._3());
                        JVMObject.DB[_tuple.JavaHashCode] = new JVMTuple(_tuple, tuple);
                        return tuple;
                    }
                    case "scala.Tuple4":
                    {
                        dynamic _tuple = CreateInstancePtr(cName, null, returnPtr, null );
                        JVMTuple4 tuple = new JVMTuple4(_tuple, _tuple._1(), _tuple._2(), _tuple._3(), _tuple._4());
                        JVMObject.DB[_tuple.JavaHashCode] = new JVMTuple(_tuple, tuple);
                        return tuple;
                    }
                    case "scala.Tuple5":
                    {
                        dynamic _tuple = CreateInstancePtr(cName, null, returnPtr, null );
                        JVMTuple5 tuple = new JVMTuple5(_tuple, _tuple._1(), _tuple._2(), _tuple._3(), _tuple._4(), _tuple._5());
                        JVMObject.DB[_tuple.JavaHashCode] = new JVMTuple(_tuple, tuple);
                        return tuple;
                    }
                    case "scala.Tuple6":
                    {
                        dynamic _tuple = CreateInstancePtr(cName, null, returnPtr, null );
                        JVMTuple6 tuple = new JVMTuple6(_tuple, _tuple._1(), _tuple._2(), _tuple._3(), _tuple._4(), _tuple._5(), _tuple._6());
                        JVMObject.DB[_tuple.JavaHashCode] = new JVMTuple(_tuple, tuple);
                        return tuple;
                    }
                    case "scala.Tuple7":
                    {
                        dynamic _tuple = CreateInstancePtr(cName, null, returnPtr, null );
                        JVMTuple7 tuple = new JVMTuple7(_tuple, _tuple._1(), _tuple._2(), _tuple._3(), _tuple._4(), _tuple._5(), _tuple._6(), _tuple._7());
                        JVMObject.DB[_tuple.JavaHashCode] = new JVMTuple(_tuple, tuple);
                        return tuple;
                    }
                    case "scala.Tuple8":
                    {
                        dynamic _tuple = CreateInstancePtr(cName, null, returnPtr, null );
                        JVMTuple8 tuple = new JVMTuple8(_tuple, _tuple._1(), _tuple._2(), _tuple._3(), _tuple._4(), _tuple._5(), _tuple._6(), _tuple._7(), _tuple._8());
                        JVMObject.DB[_tuple.JavaHashCode] = new JVMTuple(_tuple, tuple);
                        return tuple;
                    }

                    default:
                        return CreateInstancePtr(cName, null, returnPtr, null );
                }
            }
            else
                throw new Exception("Runtime Result class not found: " + cls);

        }
        
        public unsafe static JVMObject CreateInstance( string sClass, params object[] args )
        {
            return CreateInstancePtr(sClass, null, IntPtr.Zero, args );
        }

        public unsafe static JVMObject CreateInstancePath( string sClass, string path, params object[] args )
        {
            void*  _pEnv;
            if(AttacheThread((void*)JVMPtr,&_pEnv) != 0) throw new Exception ("Attach to thread error");

            void*  _pNetBridgeClass;
            if(FindClass( _pEnv, "app/quant/clr/CLRRuntime", &_pNetBridgeClass) == 0)
            {
                void* _pClass;
                void*  pLoadClassMethod; // The executed method struct
                if(GetStaticMethodID( _pEnv, _pNetBridgeClass, "LoadClass", "(Z[Ljava/lang/String;)Ljava/lang/Class;", &pLoadClassMethod ) == 0)
                {
                    
                    void** pArg_lcs = stackalloc void*[2];
                    object[] _ar_data = new object[]{ true, new string[]{ sClass, path } };
                    getJavaParameters(ref pArg_lcs, _ar_data);
                    if(CallStaticObjectMethod( _pEnv, _pNetBridgeClass, pLoadClassMethod, &_pClass, 2, pArg_lcs) == 0)
                        return CreateInstancePtr(sClass, path, IntPtr.Zero, args );
                    else
                        throw new Exception("CreateInstancePath call: CallStaticObjectMethod error");
                }
                else
                    throw new Exception("CreateInstancePath Get method: CallStaticObjectMethod error");
            }
            else
                throw new Exception("CreateInstancePath get CLRRuntime class error");
        }

        private unsafe static JVMObject CreateInstancePtr( string sClass, string path, IntPtr objPtr, object[] args )
        {
            void*  pNetBridgeClass;  // Class struct of the executed method
            void*  pSignaturesMethod; // The executed method struct
            void*  pArrayClassesMethod; // The executed method struct
            
            try
            {
            if(Loaded)
            {
                void*  pEnv;
                if(AttacheThread((void*)JVMPtr,&pEnv) != 0) throw new Exception ("Attach to thread error");
                
                if(FindClass( pEnv, "app/quant/clr/CLRRuntime", &pNetBridgeClass) == 0 )
                {
                    if(GetStaticMethodID( pEnv, pNetBridgeClass, "ArrayClasses", "([Ljava/lang/Object;)[Ljava/lang/String;", &pArrayClassesMethod ) == 0)
                    {
                        // Find the main method
                        if( GetStaticMethodID( pEnv, pNetBridgeClass, "Signatures", "(Ljava/lang/String;)[Ljava/lang/String;", &pSignaturesMethod ) == 0 )
                        {
                            void*  pClass = pClass = IntPtr.Zero.ToPointer();

                            int classFound = -1;

                            if(classFound != 0)
                            {
                                void*  pLoadClassMethod; // The executed method struct
                                
                                if( GetStaticMethodID( pEnv, pNetBridgeClass, "LoadClass", "([Ljava/lang/String;)Ljava/lang/Class;", &pLoadClassMethod ) == 0 )
                                {
                                    void** pArg_lcs = stackalloc void*[1];
                                    object[] ar_data = new object[]{ path == null ? new string[]{ sClass } : new string[]{ sClass, path } };
                                    getJavaParameters(ref pArg_lcs, ar_data);
                                    
                                    classFound = CallStaticObjectMethod( pEnv, pNetBridgeClass, pLoadClassMethod, &pClass, 1, pArg_lcs);
                                }
                                else
                                {
                                    Console.WriteLine("NetCore: LoadClass not found");
                                    classFound = -1;
                                    pClass = IntPtr.Zero.ToPointer();
                                }
                            }


                            //LoadClass
                            if(classFound == 0 )
                            {
                                void** pArg_sig = stackalloc void*[1];
                                object[] ar_data = new object[]{ sClass };
                                getJavaParameters(ref pArg_sig, ar_data);
                                void* rArr;
                                
                                int res = CallStaticObjectMethod( pEnv, pNetBridgeClass, pSignaturesMethod, &rArr, 1, pArg_sig);

                                if(res == 0)
                                {
                                    int sig_hashID = getHashCode(rArr);

                                    JVMObject sig_arr = new JVMObject(new IntPtr(rArr), sig_hashID, "[Ljava/lang/String;");


                                    int rArrLen = getArrayLength(sig_arr);

                                    var signatures = new List<string>();
                                    for(int i = 0; i < rArrLen; i++)
                                    {
                                        void* pElement;
                                        GetObjectArrayElement(pEnv, rArr, i, &pElement);
                                        string signature = GetNetString(pEnv, pElement);
                                        signatures.Add(signature);
                                    }

                                    void* pObj;
                                    IntPtr ObjectPtr;

                                    if(objPtr == IntPtr.Zero)
                                    {
                                        if(args == null || args.Length == 0)
                                        {
                                            void** ar_newInstance = stackalloc void*[1];
                                            if(NewObjectP( pEnv, pClass, "()V", 0, ar_newInstance, &pObj ) != 0)
                                            {
                                                Console.WriteLine("Error instantiating object (): " + sClass);
                                                throw new Exception("Error instantiating object (): " + sClass);
                                            }
                                        }
                                        else
                                        {
                                            void** ar_newInstance = stackalloc void*[args.Length];
                                            object[] args_object = new object[args.Length];
                                            string argSig = "";
                                            for(int i = 0; i < args.Length; i++)
                                            {
                                                argSig += TransformType(args[i]);
                                                args_object[i] = args[i];
                                            }

                                            getJavaParameters(ref ar_newInstance, args_object);
                                            
                                            if(NewObjectP( pEnv, pClass, "(" + argSig + ")V", args.Length, ar_newInstance, &pObj ) != 0)
                                                throw new Exception("Error instantiating object (...): " + sClass);
                                        }
                                        ObjectPtr = new IntPtr(pObj);
                                    }
                                    else
                                        ObjectPtr = objPtr;


                                    int hashID = getHashCode(ObjectPtr.ToPointer());

                                    dynamic expandoObject = new JVMObject(ObjectPtr, hashID, sClass);

                                    if(isMap(ObjectPtr.ToPointer()))
                                        expandoObject = new JVMIDictionary(expandoObject);

                                    else if(isCollection(ObjectPtr.ToPointer()))
                                        expandoObject = new JVMICollection(expandoObject);

                                    else if(isIterable(ObjectPtr.ToPointer()))
                                        expandoObject = new JVMIEnumerable(expandoObject);

                                    JVMObject.DB[expandoObject.JavaHashCode] = expandoObject;
                                    
                                    foreach(var signature in signatures)
                                    {
                                        if(signature.StartsWith("F/") || signature.StartsWith("S-F/"))
                                        {
                                            bool isStatic = signature.StartsWith("S-");
                                            string name = signature.Replace("F/","").Replace("S-","");
                                            name = name.Substring(0, name.IndexOf("-"));
                                            string returnSignature = signature.Substring(signature.LastIndexOf("-") + 1);

                                            switch (returnSignature)
                                            {
                                                case "Z": //Boolean
                                                    expandoObject.TrySetField(name, 
                                                        new Tuple<string, object, wrapSetProperty>(
                                                            "bool",
                                                            (wrapGetProperty<bool>)(() => {
                                                                void*  _pEnv;
                                                                if(AttacheThread((void*)JVMPtr,&_pEnv) != 0) throw new Exception ("Attach to thread error");
                                                                void* _pObj = GetJVMObject(hashID);
                                                                if(_pObj != IntPtr.Zero.ToPointer())
                                                                {
                                                                    void*  pField;
                                                                    if(isStatic) 
                                                                    { 
                                                                        void* _pClass = IntPtr.Zero.ToPointer();
                                                                        if(getClass(_pObj, ref _pClass) == 0)
                                                                            if(GetStaticFieldID( _pEnv, _pClass, name, returnSignature, &pField ) == 0)
                                                                                return GetStaticBooleanField( _pEnv, _pClass, pField);
                                                                            else
                                                                                throw new Exception("Runtime Static Field not found: " + name);
                                                                        else
                                                                            throw new Exception("Runtime Class not found: " + name);
                                                                    } 
                                                                    else  
                                                                    { 
                                                                        if(GetFieldID( _pEnv, _pObj, name, returnSignature, &pField ) == 0)
                                                                            return GetBooleanField( _pEnv, _pObj, pField);
                                                                        else
                                                                            throw new Exception("Runtime Field not found: " + name);
                                                                    }
                                                                }
                                                                else
                                                                    throw new Exception("Runtime Object not found: " + name);
                                                            }),
                                                            (wrapSetProperty)((val) => {
                                                                void*  _pEnv;
                                                                if(AttacheThread((void*)JVMPtr,&_pEnv) != 0) throw new Exception ("Attach to thread error");
                                                                void* _pObj = GetJVMObject(hashID);
                                                                if(_pObj != IntPtr.Zero.ToPointer())
                                                                {
                                                                    void*  pField;
                                                                    if(isStatic) 
                                                                    { 
                                                                        void* _pClass = IntPtr.Zero.ToPointer();
                                                                        if(getClass(_pObj, ref _pClass) == 0)
                                                                            if(GetStaticFieldID( _pEnv, _pClass, name, returnSignature, &pField ) == 0)
                                                                                SetStaticBooleanField( _pEnv, _pClass, pField, (bool) val);
                                                                            else
                                                                                throw new Exception("Runtime Static Field not found: " + name);
                                                                        else
                                                                            throw new Exception("Runtime Class not found: " + name);
                                                                    } 
                                                                    else  
                                                                    { 
                                                                        if(GetFieldID( _pEnv, _pObj, name, returnSignature, &pField ) == 0)
                                                                            SetBooleanField( _pEnv, _pObj, pField, (bool) val);
                                                                        else
                                                                            throw new Exception("Runtime Field not found: " + name);
                                                                    }
                                                                }
                                                                else
                                                                    throw new Exception("Runtime Object not found: " + name);
                                                            })
                                                        ));
                                                    break;
                                                case "B": //Byte
                                                    expandoObject.TrySetField(name, 
                                                        new Tuple<string, object, wrapSetProperty>(
                                                            "byte",
                                                            (wrapGetProperty<byte>)(() => {
                                                                void*  _pEnv;
                                                                if(AttacheThread((void*)JVMPtr,&_pEnv) != 0) throw new Exception ("Attach to thread error");
                                                                void* _pObj = GetJVMObject(hashID);
                                                                if(_pObj != IntPtr.Zero.ToPointer())
                                                                {
                                                                    void*  pField;
                                                                    if(isStatic) 
                                                                    { 
                                                                        void* _pClass = IntPtr.Zero.ToPointer();
                                                                        if(getClass(_pObj, ref _pClass) == 0)
                                                                            if(GetStaticFieldID( _pEnv, _pClass, name, returnSignature, &pField ) == 0)
                                                                                return GetStaticByteField( _pEnv, _pClass, pField);
                                                                            else
                                                                                throw new Exception("Runtime Static Field not found: " + name);
                                                                        else
                                                                            throw new Exception("Runtime Class not found: " + name);
                                                                    } 
                                                                    else  
                                                                    { 
                                                                        if(GetFieldID( _pEnv, _pObj, name, returnSignature, &pField ) == 0)
                                                                            return GetByteField( _pEnv, _pObj, pField);
                                                                        else
                                                                            throw new Exception("Runtime Field not found: " + name);
                                                                    }
                                                                }
                                                                else
                                                                    throw new Exception("Runtime Object not found: " + name);
                                                            }),
                                                            (wrapSetProperty)((val) => {
                                                                void*  _pEnv;
                                                                if(AttacheThread((void*)JVMPtr,&_pEnv) != 0) throw new Exception ("Attach to thread error");
                                                                void* _pObj = GetJVMObject(hashID);
                                                                if(_pObj != IntPtr.Zero.ToPointer())
                                                                {
                                                                    void*  pField;
                                                                    if(isStatic) 
                                                                    { 
                                                                        void* _pClass = IntPtr.Zero.ToPointer();
                                                                        if(getClass(_pObj, ref _pClass) == 0)
                                                                            if(GetStaticFieldID( _pEnv, _pClass, name, returnSignature, &pField ) == 0)
                                                                                SetStaticByteField( _pEnv, _pClass, pField, (byte) val);
                                                                            else
                                                                                throw new Exception("Runtime Static Field not found: " + name);
                                                                        else
                                                                            throw new Exception("Runtime Class not found: " + name);
                                                                    } 
                                                                    else  
                                                                    { 
                                                                        if(GetFieldID( _pEnv, _pObj, name, returnSignature, &pField ) == 0)
                                                                            SetByteField( _pEnv, _pObj, pField, (byte) val);
                                                                        else
                                                                            throw new Exception("Runtime Field not found: " + name);
                                                                    }
                                                                }
                                                                else
                                                                    throw new Exception("Runtime Object not found: " + name);
                                                            })
                                                        ));
                                                    break;
                                            
                                                case "C": //Char
                                                    expandoObject.TrySetField(name, 
                                                        new Tuple<string, object, wrapSetProperty>(
                                                            "char",
                                                            (wrapGetProperty<char>)(() => {
                                                                void*  _pEnv;
                                                                if(AttacheThread((void*)JVMPtr,&_pEnv) != 0) throw new Exception ("Attach to thread error");
                                                                void* _pObj = GetJVMObject(hashID);
                                                                if(_pObj != IntPtr.Zero.ToPointer())
                                                                {
                                                                    void*  pField;
                                                                    if(isStatic) 
                                                                    { 
                                                                        void* _pClass = IntPtr.Zero.ToPointer();
                                                                        if(getClass(_pObj, ref _pClass) == 0)
                                                                            if(GetStaticFieldID( _pEnv, _pClass, name, returnSignature, &pField ) == 0)
                                                                                return GetStaticCharField( _pEnv, _pClass, pField);
                                                                            else
                                                                                throw new Exception("Runtime Static Field not found: " + name);
                                                                        else
                                                                            throw new Exception("Runtime Class not found: " + name);
                                                                    } 
                                                                    else  
                                                                    { 
                                                                        if(GetFieldID( _pEnv, _pObj, name, returnSignature, &pField ) == 0)
                                                                            return GetCharField( _pEnv, _pObj, pField);
                                                                        else
                                                                            throw new Exception("Runtime Field not found: " + name);
                                                                    }
                                                                }
                                                                else
                                                                    throw new Exception("Runtime Object not found: " + name);
                                                            }),
                                                            (wrapSetProperty)((val) => {
                                                                void*  _pEnv;
                                                                if(AttacheThread((void*)JVMPtr,&_pEnv) != 0) throw new Exception ("Attach to thread error");
                                                                void* _pObj = GetJVMObject(hashID);
                                                                if(_pObj != IntPtr.Zero.ToPointer())
                                                                {
                                                                    void*  pField;
                                                                    if(isStatic) 
                                                                    { 
                                                                        void* _pClass = IntPtr.Zero.ToPointer();
                                                                        if(getClass(_pObj, ref _pClass) == 0)
                                                                            if(GetStaticFieldID( _pEnv, _pClass, name, returnSignature, &pField ) == 0)
                                                                                SetStaticCharField( _pEnv, _pClass, pField, (char) val);
                                                                            else
                                                                                throw new Exception("Runtime Static Field not found: " + name);
                                                                        else
                                                                            throw new Exception("Runtime Class not found: " + name);
                                                                    } 
                                                                    else  
                                                                    { 
                                                                        if(GetFieldID( _pEnv, _pObj, name, returnSignature, &pField ) == 0)
                                                                            SetCharField( _pEnv, _pObj, pField, (char) val);
                                                                        else
                                                                            throw new Exception("Runtime Field not found: " + name);
                                                                    }
                                                                }
                                                                else
                                                                    throw new Exception("Runtime Object not found: " + name);
                                                            })
                                                        ));
                                                    break;
                                                
                                                case "S": //Short
                                                    expandoObject.TrySetField(name, 
                                                        new Tuple<string, object, wrapSetProperty>(
                                                            "short",
                                                            (wrapGetProperty<short>)(() => {
                                                                void*  _pEnv;
                                                                if(AttacheThread((void*)JVMPtr,&_pEnv) != 0) throw new Exception ("Attach to thread error");
                                                                void* _pObj = GetJVMObject(hashID);
                                                                if(_pObj != IntPtr.Zero.ToPointer())
                                                                {
                                                                    void*  pField;
                                                                    if(isStatic) 
                                                                    { 
                                                                        void* _pClass = IntPtr.Zero.ToPointer();
                                                                        if(getClass(_pObj, ref _pClass) == 0)
                                                                            if(GetStaticFieldID( _pEnv, _pClass, name, returnSignature, &pField ) == 0)
                                                                                return GetStaticShortField( _pEnv, _pClass, pField);
                                                                            else
                                                                                throw new Exception("Runtime Static Field not found: " + name);
                                                                        else
                                                                            throw new Exception("Runtime Class not found: " + name);
                                                                    } 
                                                                    else  
                                                                    { 
                                                                        if(GetFieldID( _pEnv, _pObj, name, returnSignature, &pField ) == 0)
                                                                            return GetShortField( _pEnv, _pObj, pField);
                                                                        else
                                                                            throw new Exception("Runtime Field not found: " + name);
                                                                    }
                                                                }
                                                                else
                                                                    throw new Exception("Runtime Object not found: " + name);
                                                            }),
                                                            (wrapSetProperty)((val) => {
                                                                void*  _pEnv;
                                                                if(AttacheThread((void*)JVMPtr,&_pEnv) != 0) throw new Exception ("Attach to thread error");
                                                                void* _pObj = GetJVMObject(hashID);
                                                                if(_pObj != IntPtr.Zero.ToPointer())
                                                                {
                                                                    void*  pField;
                                                                    if(isStatic) 
                                                                    { 
                                                                        void* _pClass = IntPtr.Zero.ToPointer();
                                                                        if(getClass(_pObj, ref _pClass) == 0)
                                                                            if(GetStaticFieldID( _pEnv, _pClass, name, returnSignature, &pField ) == 0)
                                                                                SetStaticShortField( _pEnv, _pClass, pField, (short) val);
                                                                            else
                                                                                throw new Exception("Runtime Static Field not found: " + name);
                                                                        else
                                                                            throw new Exception("Runtime Class not found: " + name);
                                                                    } 
                                                                    else  
                                                                    { 
                                                                        if(GetFieldID( _pEnv, _pObj, name, returnSignature, &pField ) == 0)
                                                                            SetShortField( _pEnv, _pObj, pField, (short) val);
                                                                        else
                                                                            throw new Exception("Runtime Field not found: " + name);
                                                                    }
                                                                }
                                                                else
                                                                    throw new Exception("Runtime Object not found: " + name);
                                                            })
                                                        ));
                                                    break;
                                                
                                                case "I": //Int
                                                    expandoObject.TrySetField(name, 
                                                        new Tuple<string, object, wrapSetProperty>(
                                                            "int",
                                                            (wrapGetProperty<int>)(() => {
                                                                void*  _pEnv;
                                                                if(AttacheThread((void*)JVMPtr,&_pEnv) != 0) throw new Exception ("Attach to thread error");
                                                                void* _pObj = GetJVMObject(hashID);
                                                                if(_pObj != IntPtr.Zero.ToPointer())
                                                                {
                                                                    void*  pField;
                                                                    if(isStatic) 
                                                                    { 
                                                                        void* _pClass = IntPtr.Zero.ToPointer();
                                                                        if(getClass(_pObj, ref _pClass) == 0)
                                                                            if(GetStaticFieldID( _pEnv, _pClass, name, returnSignature, &pField ) == 0)
                                                                                return GetStaticIntField( _pEnv, _pClass, pField);
                                                                            else
                                                                                throw new Exception("Runtime Static Field not found: " + name);
                                                                        else
                                                                            throw new Exception("Runtime Class not found: " + name);
                                                                    } 
                                                                    else  
                                                                    { 
                                                                        if(GetFieldID( _pEnv, _pObj, name, returnSignature, &pField ) == 0)
                                                                            return GetIntField( _pEnv, _pObj, pField);
                                                                        else
                                                                            throw new Exception("Runtime Field not found: " + name);
                                                                    }
                                                                }
                                                                else
                                                                    throw new Exception("Runtime Object not found: " + name);
                                                            }),
                                                            (wrapSetProperty)((val) => {
                                                                void*  _pEnv;
                                                                if(AttacheThread((void*)JVMPtr,&_pEnv) != 0) throw new Exception ("Attach to thread error");
                                                                void* _pObj = GetJVMObject(hashID);
                                                                if(_pObj != IntPtr.Zero.ToPointer())
                                                                {
                                                                    void*  pField;
                                                                    if(isStatic) 
                                                                    { 
                                                                        void* _pClass = IntPtr.Zero.ToPointer();
                                                                        if(getClass(_pObj,  ref _pClass) == 0)
                                                                            if(GetStaticFieldID( _pEnv, _pClass, name, returnSignature, &pField ) == 0)
                                                                                SetStaticIntField( _pEnv, _pClass, pField, (int) val);
                                                                            else
                                                                                throw new Exception("Runtime Static Field not found: " + name);
                                                                        else
                                                                            throw new Exception("Runtime Class not found: " + name);
                                                                    } 
                                                                    else  
                                                                    { 
                                                                        if(GetFieldID( _pEnv, _pObj, name, returnSignature, &pField ) == 0)
                                                                            SetIntField( _pEnv, _pObj, pField, (int) val);
                                                                        else
                                                                            throw new Exception("Runtime Field not found: " + name);
                                                                    }
                                                                }
                                                                else
                                                                    throw new Exception("Runtime Object not found: " + name);
                                                            })
                                                        ));
                                                    break;
                                                
                                                case "J": //Long
                                                    expandoObject.TrySetField(name, 
                                                        new Tuple<string, object, wrapSetProperty>(
                                                            "long",
                                                            (wrapGetProperty<long>)(() => {
                                                                void*  _pEnv;
                                                                if(AttacheThread((void*)JVMPtr,&_pEnv) != 0) throw new Exception ("Attach to thread error");
                                                                void* _pObj = GetJVMObject(hashID);
                                                                if(_pObj != IntPtr.Zero.ToPointer())
                                                                {
                                                                    void*  pField;
                                                                    if(isStatic) 
                                                                    { 
                                                                        void* _pClass = IntPtr.Zero.ToPointer();
                                                                        if(getClass(_pObj,  ref _pClass) == 0)
                                                                            if(GetStaticFieldID( _pEnv, _pClass, name, returnSignature, &pField ) == 0)
                                                                                return GetStaticLongField( _pEnv, _pClass, pField);
                                                                            else
                                                                                throw new Exception("Runtime Static Field not found: " + name);
                                                                        else
                                                                            throw new Exception("Runtime Class not found: " + name);
                                                                    } 
                                                                    else  
                                                                    { 
                                                                        if(GetFieldID( _pEnv, _pObj, name, returnSignature, &pField ) == 0)
                                                                            return GetLongField( _pEnv, _pObj, pField);
                                                                        else
                                                                            throw new Exception("Runtime Field not found: " + name);
                                                                    }
                                                                }
                                                                else
                                                                    throw new Exception("Runtime Object not found: " + name);
                                                            }),
                                                            (wrapSetProperty)((val) => {
                                                                void*  _pEnv;
                                                                if(AttacheThread((void*)JVMPtr,&_pEnv) != 0) throw new Exception ("Attach to thread error");
                                                                void* _pObj = GetJVMObject(hashID);
                                                                if(_pObj != IntPtr.Zero.ToPointer())
                                                                {
                                                                    void*  pField;
                                                                    if(isStatic) 
                                                                    { 
                                                                        void* _pClass = IntPtr.Zero.ToPointer();
                                                                        if(getClass(_pObj,  ref _pClass) == 0)
                                                                            if(GetStaticFieldID( _pEnv, _pClass, name, returnSignature, &pField ) == 0)
                                                                                SetStaticLongField( _pEnv, _pClass, pField, (long) val);
                                                                            else
                                                                                throw new Exception("Runtime Static Field not found: " + name);
                                                                        else
                                                                            throw new Exception("Runtime Class not found: " + name);
                                                                    } 
                                                                    else  
                                                                    { 
                                                                        if(GetFieldID( _pEnv, _pObj, name, returnSignature, &pField ) == 0)
                                                                            SetLongField( _pEnv, _pObj, pField, (long) val);
                                                                        else
                                                                            throw new Exception("Runtime Field not found: " + name);
                                                                    }
                                                                }
                                                                else
                                                                    throw new Exception("Runtime Object not found: " + name);
                                                            })
                                                        ));
                                                    break;
                                                
                                                case "F": //Float
                                                    expandoObject.TrySetField(name, 
                                                        new Tuple<string, object, wrapSetProperty>(
                                                            "float",
                                                            (wrapGetProperty<float>)(() => {
                                                                void*  _pEnv;
                                                                if(AttacheThread((void*)JVMPtr,&_pEnv) != 0) throw new Exception ("Attach to thread error");
                                                                void* _pObj = GetJVMObject(hashID);
                                                                if(_pObj != IntPtr.Zero.ToPointer())
                                                                {
                                                                    void*  pField;
                                                                    if(isStatic) 
                                                                    { 
                                                                        void* _pClass = IntPtr.Zero.ToPointer();
                                                                        if(getClass(_pObj,  ref _pClass) == 0)
                                                                            if(GetStaticFieldID( _pEnv, _pClass, name, returnSignature, &pField ) == 0)
                                                                                return GetStaticFloatField( _pEnv, _pClass, pField);
                                                                            else
                                                                                throw new Exception("Runtime Static Field not found: " + name);
                                                                        else
                                                                            throw new Exception("Runtime Class not found: " + name);
                                                                    } 
                                                                    else  
                                                                    { 
                                                                        if(GetFieldID( _pEnv, _pObj, name, returnSignature, &pField ) == 0)
                                                                            return GetFloatField( _pEnv, _pObj, pField);
                                                                        else
                                                                            throw new Exception("Runtime Field not found: " + name);
                                                                    }
                                                                }
                                                                else
                                                                    throw new Exception("Runtime Object not found: " + name);
                                                            }),
                                                            (wrapSetProperty)((val) => {
                                                                void*  _pEnv;
                                                                if(AttacheThread((void*)JVMPtr,&_pEnv) != 0) throw new Exception ("Attach to thread error");
                                                                void* _pObj = GetJVMObject(hashID);
                                                                if(_pObj != IntPtr.Zero.ToPointer())
                                                                {
                                                                    void*  pField;
                                                                    if(isStatic) 
                                                                    { 
                                                                        void* _pClass = IntPtr.Zero.ToPointer();
                                                                        if(getClass(_pObj,  ref _pClass) == 0)
                                                                            if(GetStaticFieldID( _pEnv, _pClass, name, returnSignature, &pField ) == 0)
                                                                                SetStaticFloatField( _pEnv, _pClass, pField, (float) val);
                                                                            else
                                                                                throw new Exception("Runtime Static Field not found: " + name);
                                                                        else
                                                                            throw new Exception("Runtime Class not found: " + name);
                                                                    } 
                                                                    else  
                                                                    { 
                                                                        if(GetFieldID( _pEnv, _pObj, name, returnSignature, &pField ) == 0)
                                                                            SetFloatField( _pEnv, _pObj, pField, (float) val);
                                                                        else
                                                                            throw new Exception("Runtime Field not found: " + name);
                                                                    }
                                                                }
                                                                else
                                                                    throw new Exception("Runtime Object not found: " + name);
                                                            })
                                                        ));
                                                    break;
                                                
                                                case "D": //Double
                                                    expandoObject.TrySetField(name, 
                                                        new Tuple<string, object, wrapSetProperty>(
                                                            "double",
                                                            (wrapGetProperty<double>)(() => {
                                                                void*  _pEnv;
                                                                if(AttacheThread((void*)JVMPtr,&_pEnv) != 0) throw new Exception ("Attach to thread error");
                                                                void* _pObj = GetJVMObject(hashID);
                                                                if(_pObj != IntPtr.Zero.ToPointer())
                                                                {
                                                                    void*  pField;
                                                                    if(isStatic) 
                                                                    { 
                                                                        void* _pClass = IntPtr.Zero.ToPointer();
                                                                        if(getClass(_pObj,  ref _pClass) == 0)
                                                                            if(GetStaticFieldID( _pEnv, _pClass, name, returnSignature, &pField ) == 0)
                                                                                return GetStaticDoubleField( _pEnv, _pClass, pField);
                                                                            else
                                                                                throw new Exception("Runtime Static Field not found: " + name);
                                                                        else
                                                                            throw new Exception("Runtime Class not found: " + name);
                                                                    } 
                                                                    else  
                                                                    { 
                                                                        if(GetFieldID( _pEnv, _pObj, name, returnSignature, &pField ) == 0)
                                                                            return GetDoubleField( _pEnv, _pObj, pField);
                                                                        else
                                                                            throw new Exception("Runtime Field not found: " + name);
                                                                    }
                                                                }
                                                                else
                                                                    throw new Exception("Runtime Object not found: " + name);
                                                            }),
                                                            (wrapSetProperty)((val) => {
                                                                void*  _pEnv;
                                                                if(AttacheThread((void*)JVMPtr,&_pEnv) != 0) throw new Exception ("Attach to thread error");
                                                                void* _pObj = GetJVMObject(hashID);
                                                                if(_pObj != IntPtr.Zero.ToPointer())
                                                                {
                                                                    void*  pField;
                                                                    if(isStatic) 
                                                                    { 
                                                                        void* _pClass = IntPtr.Zero.ToPointer();
                                                                        if(getClass(_pObj,  ref _pClass) == 0)
                                                                            if(GetStaticFieldID( _pEnv, _pClass, name, returnSignature, &pField ) == 0)
                                                                                SetStaticDoubleField( _pEnv, _pClass, pField, (double) val);
                                                                            else
                                                                                throw new Exception("Runtime Static Field not found: " + name);
                                                                        else
                                                                            throw new Exception("Runtime Class not found: " + name);
                                                                    } 
                                                                    else  
                                                                    { 
                                                                        if(GetFieldID( _pEnv, _pObj, name, returnSignature, &pField ) == 0)
                                                                            SetDoubleField( _pEnv, _pObj, pField, (double) val);
                                                                        else
                                                                            throw new Exception("Runtime Field not found: " + name);
                                                                    }
                                                                }
                                                                else
                                                                    throw new Exception("Runtime Object not found: " + name);
                                                            })
                                                        ));
                                                    break;
                                                
                                                case "Ljava/lang/String;": //String

                                                    expandoObject.TrySetField(name, 
                                                        new Tuple<string, object, wrapSetProperty>(
                                                            "string",
                                                            (wrapGetProperty<string>)(() => {
                                                                void*  _pEnv;
                                                                if(AttacheThread((void*)JVMPtr,&_pEnv) != 0) throw new Exception ("Attach to thread error");
                                                                void* _pObj = GetJVMObject(hashID);
                                                                if(_pObj != IntPtr.Zero.ToPointer())
                                                                {
                                                                    void*  pField;
                                                                    if(isStatic) 
                                                                    { 
                                                                        void* pObjResult;
                                                                        void* _pClass = IntPtr.Zero.ToPointer();
                                                                        if(getClass(_pObj, ref _pClass) == 0)
                                                                            if(GetStaticFieldID( _pEnv, _pClass, name, returnSignature, &pField ) == 0)
                                                                                if(GetStaticObjectField( _pEnv, _pClass, pField, &pObjResult) == 0)
                                                                                    return GetNetString(_pEnv, pObjResult);
                                                                                else
                                                                                    throw new Exception("Runtime Calling Field not error: " + name);
                                                                            else
                                                                                throw new Exception("Runtime Static Field not found: " + name);
                                                                        else
                                                                            throw new Exception("Runtime Class not found: " + name);
                                                                    } 
                                                                    else  
                                                                    { 
                                                                        void* pObjResult;
                                                                        if(GetFieldID( _pEnv, _pObj, name, returnSignature, &pField ) == 0)
                                                                            if(GetObjectField( _pEnv, _pObj, pField, &pObjResult) == 0)
                                                                                return GetNetString(_pEnv, pObjResult);
                                                                            else
                                                                                throw new Exception("Runtime Calling Field not error: " + name);
                                                                        else
                                                                            throw new Exception("Runtime Field not found: " + name);
                                                                    }
                                                                }
                                                                else
                                                                    throw new Exception("Runtime Object not found: " + name);
                                                            }),
                                                            (wrapSetProperty)((val) => {
                                                                void*  _pEnv;
                                                                if(AttacheThread((void*)JVMPtr,&_pEnv) != 0) throw new Exception ("Attach to thread error");
                                                                void* _pObj = GetJVMObject(hashID);
                                                                if(_pObj != IntPtr.Zero.ToPointer())
                                                                {
                                                                    void* jstring = GetJavaString(_pEnv, (string)val);
                                                                    void*  pField;
                                                                    if(isStatic) 
                                                                    { 
                                                                        void* _pClass = IntPtr.Zero.ToPointer();
                                                                        if(getClass(_pObj, ref _pClass) == 0)
                                                                            if(GetStaticFieldID( _pEnv, _pClass, name, returnSignature, &pField ) == 0)
                                                                                SetStaticObjectField( _pEnv, _pClass, pField, jstring);
                                                                            else
                                                                                throw new Exception("Runtime Static Field not found: " + name);
                                                                        else
                                                                            throw new Exception("Runtime Class not found: " + name);
                                                                    } 
                                                                    else  
                                                                    { 
                                                                        if(GetFieldID( _pEnv, _pObj, name, returnSignature, &pField ) == 0)
                                                                            SetObjectField( _pEnv, _pObj, pField, jstring);
                                                                        else
                                                                            throw new Exception("Runtime Field not found: " + name);
                                                                    }
                                                                }
                                                                else
                                                                    throw new Exception("Runtime Object not found: " + name);
                                                            })
                                                        ));
                                                    break;

                                                default:
                                                    
                                                    if(returnSignature.StartsWith("["))
                                                    {
                                                        expandoObject.TrySetField(name, 
                                                            new Tuple<string, object, wrapSetProperty>(
                                                                "array",
                                                                (wrapGetProperty<object[]>)(() => {
                                                                    void*  _pEnv;// = (void*)EnvPtr;
                                                                    if(AttacheThread((void*)JVMPtr,&_pEnv) != 0) throw new Exception ("Attach to thread error");

                                                                    void* _pObj = GetJVMObject(hashID);
                                                                    if(_pObj != IntPtr.Zero.ToPointer())
                                                                    {
                                                                        void*  pField;
                                                                        if(isStatic) 
                                                                        { 
                                                                            void* pObjResult;
                                                                            void* _pClass = IntPtr.Zero.ToPointer();
                                                                            if(getClass(_pObj,  ref _pClass) == 0)
                                                                                if(GetStaticFieldID( _pEnv, _pClass, name, returnSignature, &pField ) == 0)
                                                                                    if(GetStaticObjectField( _pEnv, _pClass, pField, &pObjResult) == 0)
                                                                                        return getJavaArray(new IntPtr(pObjResult), returnSignature);
                                                                                    else
                                                                                        throw new Exception("Runtime Calling Field not error: " + name);
                                                                                else
                                                                                    throw new Exception("Runtime Static Field not found: " + name);
                                                                            else
                                                                                throw new Exception("Runtime Class not found: " + name);
                                                                        } 
                                                                        else  
                                                                        { 
                                                                            void* pObjResult;
                                                                            if(GetFieldID( _pEnv, _pObj, name, returnSignature, &pField ) == 0)
                                                                                if(GetObjectField( _pEnv, _pObj, pField, &pObjResult) == 0)
                                                                                    return getJavaArray(new IntPtr(pObjResult), returnSignature);
                                                                                else
                                                                                    throw new Exception("Runtime Calling Field not error: " + name);
                                                                            else
                                                                                throw new Exception("Runtime Field not found: " + name);
                                                                        }
                                                                    }
                                                                    else
                                                                        throw new Exception("Runtime Object not found: " + name);

                                                                    
                                                                }),
                                                                (wrapSetProperty)((val) => {
                                                                    
                                                                    string typename = val.GetType().ToString();
                                                                    void*  _pEnv;
                                                                    if(AttacheThread((void*)JVMPtr,&_pEnv) != 0) throw new Exception ("Attach to thread error");
                                                                    void* _pObj = GetJVMObject(hashID);
                                                                    if(_pObj != IntPtr.Zero.ToPointer())
                                                                    {
                                                                        void*  pField;
                                                                        switch(typename)
                                                                        {
                                                                            case "System.Boolean[]":
                                                                                JVMObject vobj_bool = getJavaArray((bool[])val);
                                                                                if(isStatic) 
                                                                                { 
                                                                                    void* _pClass = IntPtr.Zero.ToPointer();
                                                                                    if(getClass(_pObj,  ref _pClass) == 0)
                                                                                        if(GetStaticFieldID( _pEnv, _pClass, name, returnSignature, &pField ) == 0)
                                                                                            SetStaticObjectField( _pEnv, _pClass, pField, vobj_bool.Pointer.ToPointer());
                                                                                        else
                                                                                            throw new Exception("Runtime Static Field not found: " + name);
                                                                                    else
                                                                                        throw new Exception("Runtime Class not found: " + name);
                                                                                } 
                                                                                else  
                                                                                { 
                                                                                    if(GetFieldID( _pEnv, _pObj, name, returnSignature, &pField ) == 0)
                                                                                        SetObjectField( _pEnv, _pObj, pField, vobj_bool.Pointer.ToPointer());
                                                                                    else
                                                                                        throw new Exception("Runtime Field not found: " + name);
                                                                                }
                                                                                
                                                                                
                                                                                break;

                                                                            case "System.Byte[]":
                                                                                bool[] byte_arr = (bool[])val;
                                                                                
                                                                                JVMObject vobj_byte = getJavaArray((byte[])val);
                                                                                if(isStatic) 
                                                                                { 
                                                                                    void* _pClass = IntPtr.Zero.ToPointer();
                                                                                    if(getClass(_pObj,  ref _pClass) == 0)
                                                                                        if(GetStaticFieldID( _pEnv, _pClass, name, returnSignature, &pField ) == 0)
                                                                                            SetStaticObjectField( _pEnv, _pClass, pField, vobj_byte.Pointer.ToPointer());
                                                                                        else
                                                                                            throw new Exception("Runtime Static Field not found: " + name);
                                                                                    else
                                                                                        throw new Exception("Runtime Class not found: " + name);
                                                                                } 
                                                                                else  
                                                                                { 
                                                                                    if(GetFieldID( _pEnv, _pObj, name, returnSignature, &pField ) == 0)
                                                                                        SetObjectField( _pEnv, _pObj, pField, vobj_byte.Pointer.ToPointer());
                                                                                    else
                                                                                        throw new Exception("Runtime Field not found: " + name);
                                                                                }
                                                                                
                                                                                break;
                                                                                
                                                                            case "System.Char[]":
                                                                                JVMObject vobj_char = getJavaArray((char[])val);
                                                                                if(isStatic) 
                                                                                { 
                                                                                    void* _pClass = IntPtr.Zero.ToPointer();
                                                                                    if(getClass(_pObj,  ref _pClass) == 0)
                                                                                        if(GetStaticFieldID( _pEnv, _pClass, name, returnSignature, &pField ) == 0)
                                                                                            SetStaticObjectField( _pEnv, _pClass, pField, vobj_char.Pointer.ToPointer());
                                                                                        else
                                                                                            throw new Exception("Runtime Static Field not found: " + name);
                                                                                    else
                                                                                        throw new Exception("Runtime Class not found: " + name);
                                                                                } 
                                                                                else  
                                                                                { 
                                                                                    if(GetFieldID( _pEnv, _pObj, name, returnSignature, &pField ) == 0)
                                                                                        SetObjectField( _pEnv, _pObj, pField, vobj_char.Pointer.ToPointer());
                                                                                    else
                                                                                        throw new Exception("Runtime Field not found: " + name);
                                                                                }
                                                                                
                                                                                break;

                                                                            case "System.Short[]":
                                                                                
                                                                                JVMObject vobj_short = getJavaArray((short[])val);
                                                                                if(isStatic) 
                                                                                { 
                                                                                    void* _pClass = IntPtr.Zero.ToPointer();
                                                                                    if(getClass(_pObj,  ref _pClass) == 0)
                                                                                        if(GetStaticFieldID( _pEnv, _pClass, name, returnSignature, &pField ) == 0)
                                                                                            SetStaticObjectField( _pEnv, _pClass, pField, vobj_short.Pointer.ToPointer());
                                                                                        else
                                                                                            throw new Exception("Runtime Static Field not found: " + name);
                                                                                    else
                                                                                        throw new Exception("Runtime Class not found: " + name);
                                                                                } 
                                                                                else  
                                                                                { 
                                                                                    if(GetFieldID( _pEnv, _pObj, name, returnSignature, &pField ) == 0)
                                                                                        SetObjectField( _pEnv, _pObj, pField, vobj_short.Pointer.ToPointer());
                                                                                    else
                                                                                        throw new Exception("Runtime Field not found: " + name);
                                                                                }
                                                                                
                                                                                break;

                                                                            case "System.Int32[]":
                                                                                
                                                                                JVMObject vobj_int = getJavaArray((int[])val);
                                                                                if(isStatic) 
                                                                                { 
                                                                                    void* _pClass = IntPtr.Zero.ToPointer();
                                                                                    if(getClass(_pObj,  ref _pClass) == 0)
                                                                                        if(GetStaticFieldID( _pEnv, _pClass, name, returnSignature, &pField ) == 0)
                                                                                            SetStaticObjectField( _pEnv, _pClass, pField, vobj_int.Pointer.ToPointer());
                                                                                        else
                                                                                            throw new Exception("Runtime Static Field not found: " + name);
                                                                                    else
                                                                                        throw new Exception("Runtime Class not found: " + name);
                                                                                } 
                                                                                else  
                                                                                { 
                                                                                    if(GetFieldID( _pEnv, _pObj, name, returnSignature, &pField ) == 0)
                                                                                        SetObjectField( _pEnv, _pObj, pField, vobj_int.Pointer.ToPointer());
                                                                                    else
                                                                                        throw new Exception("Runtime Field not found: " + name);
                                                                                }
                                                                                
                                                                                break;

                                                                            case "System.Int64[]":
                                                                                
                                                                                JVMObject vobj_long = getJavaArray((long[])val);
                                                                                if(isStatic) 
                                                                                { 
                                                                                    void* _pClass = IntPtr.Zero.ToPointer();
                                                                                    if(getClass(_pObj,  ref _pClass) == 0)
                                                                                        if(GetStaticFieldID( _pEnv, _pClass, name, returnSignature, &pField ) == 0)
                                                                                            SetStaticObjectField( _pEnv, _pClass, pField, vobj_long.Pointer.ToPointer());
                                                                                        else
                                                                                            throw new Exception("Runtime Static Field not found: " + name);
                                                                                    else
                                                                                        throw new Exception("Runtime Class not found: " + name);
                                                                                } 
                                                                                else  
                                                                                { 
                                                                                    if(GetFieldID( _pEnv, _pObj, name, returnSignature, &pField ) == 0)
                                                                                        SetObjectField( _pEnv, _pObj, pField, vobj_long.Pointer.ToPointer());
                                                                                    else
                                                                                        throw new Exception("Runtime Field not found: " + name);
                                                                                }
                                                                                
                                                                                break;

                                                                            case "System.Float[]":
                                                                                
                                                                                JVMObject vobj_float = getJavaArray((float[])val);
                                                                                if(isStatic) 
                                                                                { 
                                                                                    void* _pClass = IntPtr.Zero.ToPointer();
                                                                                    if(getClass(_pObj,  ref _pClass) == 0)
                                                                                        if(GetStaticFieldID( _pEnv, _pClass, name, returnSignature, &pField ) == 0)
                                                                                            SetStaticObjectField( _pEnv, _pClass, pField, vobj_float.Pointer.ToPointer());
                                                                                        else
                                                                                            throw new Exception("Runtime Static Field not found: " + name);
                                                                                    else
                                                                                        throw new Exception("Runtime Class not found: " + name);
                                                                                } 
                                                                                else  
                                                                                { 
                                                                                    if(GetFieldID( _pEnv, _pObj, name, returnSignature, &pField ) == 0)
                                                                                        SetObjectField( _pEnv, _pObj, pField, vobj_float.Pointer.ToPointer());
                                                                                    else
                                                                                        throw new Exception("Runtime Field not found: " + name);
                                                                                }
                                                                                
                                                                                break;


                                                                            case "System.Double[]":
                                                                                
                                                                                JVMObject vobj_double = getJavaArray((double[])val);
                                                                                if(isStatic) 
                                                                                { 
                                                                                    void* _pClass = IntPtr.Zero.ToPointer();
                                                                                    if(getClass(_pObj,  ref _pClass) == 0)
                                                                                        if(GetStaticFieldID( _pEnv, _pClass, name, returnSignature, &pField ) == 0)
                                                                                            SetStaticObjectField( _pEnv, _pClass, pField, vobj_double.Pointer.ToPointer());
                                                                                        else
                                                                                            throw new Exception("Runtime Static Field not found: " + name);
                                                                                    else
                                                                                        throw new Exception("Runtime Class not found: " + name);
                                                                                } 
                                                                                else  
                                                                                { 
                                                                                    if(GetFieldID( _pEnv, _pObj, name, returnSignature, &pField ) == 0)
                                                                                        SetObjectField( _pEnv, _pObj, pField, vobj_double.Pointer.ToPointer());
                                                                                    else
                                                                                        throw new Exception("Runtime Field not found: " + name);
                                                                                }
                                                                                
                                                                                break;

                                                                            default:
                                                                           
                                                                                JVMObject vobj_obj = getJavaArray((object[])val);
                                                                                if(isStatic) 
                                                                                { 
                                                                                    void* _pClass = IntPtr.Zero.ToPointer();
                                                                                    if(getClass(_pObj,  ref _pClass) == 0)
                                                                                        if(GetStaticFieldID( _pEnv, _pClass, name, returnSignature, &pField ) == 0)
                                                                                            SetStaticObjectField( _pEnv, _pClass, pField, vobj_obj.Pointer.ToPointer());
                                                                                        else
                                                                                            throw new Exception("Runtime Static Field not found: " + name);
                                                                                    else
                                                                                        throw new Exception("Runtime Class not found: " + name);

                                                                                } 
                                                                                else  
                                                                                { 
                                                                                    if(GetFieldID( _pEnv, _pObj, name, returnSignature, &pField ) == 0)
                                                                                        SetObjectField( _pEnv, _pObj, pField, vobj_obj.Pointer.ToPointer());
                                                                                    else
                                                                                        throw new Exception("Runtime Field not found: " + name);
                                                                                }
                                                                                
                                                                                break;
                                                                        }
                                                                        
                                                                    }
                                                                    else
                                                                        throw new Exception("Runtime Object not found: " + name);
                                                                    })
                                                        ));
                                                    }
                                                    else
                                                    {
                                                        expandoObject.TrySetField(name, 
                                                            new Tuple<string, object, wrapSetProperty>(
                                                                "object",
                                                                (wrapGetProperty<object>)(() => {
                                                                    void*  _pEnv;
                                                                    if(AttacheThread((void*)JVMPtr,&_pEnv) != 0) throw new Exception ("Attach to thread error");
                                                                    void* _pObj = GetJVMObject(hashID);
                                                                    if(_pObj != IntPtr.Zero.ToPointer())
                                                                    {
                                                                        void*  pField;
                                                                        if(isStatic) 
                                                                        { 
                                                                            void* pObjResult;
                                                                            void* _pClass = IntPtr.Zero.ToPointer();
                                                                            if(getClass(_pObj,  ref _pClass) == 0)
                                                                                if(GetStaticFieldID( _pEnv, _pClass, name, returnSignature, &pField ) == 0)
                                                                                    if(GetStaticObjectField( _pEnv, _pClass, pField, &pObjResult) == 0)
                                                                                    {
                                                                                        IntPtr returnPtr = new IntPtr(pObjResult);

                                                                                        int hashID_res = getHashCode(pObjResult);

                                                                                        if(JVMObject.DB.ContainsKey(hashID_res))
                                                                                            return JVMObject.DB[hashID_res];

                                                                                        else if(DB.ContainsKey(hashID_res))
                                                                                            return (JVMObject)DB[hashID_res];
                                                                                        else
                                                                                        {
                                                                                            string cls = returnSignature.StartsWith("L") && returnSignature.EndsWith(";") ? returnSignature.Substring(1).Replace(";","").Replace("/",".") : returnSignature;

                                                                                            return getObject(_pEnv, cls, pObjResult);
                                                                                        }
                                                                                    }
                                                                                    else
                                                                                        throw new Exception("Runtime Calling Field not error: " + name);
                                                                                else
                                                                                    throw new Exception("Runtime Static Field not found: " + name);
                                                                            else
                                                                                throw new Exception("Runtime Class not found: " + name);
                                                                            
                                                                        } 
                                                                        else  
                                                                        { 
                                                                            void* pObjResult;
                                                                            if(GetFieldID( _pEnv, _pObj, name, returnSignature, &pField ) == 0)
                                                                                if(GetObjectField( _pEnv, _pObj, pField, &pObjResult) == 0)
                                                                                {
                                                                                    Console.WriteLine("CALLING FIELD");
                                                                                    IntPtr returnPtr = new IntPtr(pObjResult);

                                                                                    int hashID_res = getHashCode(pObjResult);

                                                                                    if(JVMObject.DB.ContainsKey(hashID_res))
                                                                                        return JVMObject.DB[hashID_res];

                                                                                    else if(DB.ContainsKey(hashID_res))
                                                                                        return (JVMObject)DB[hashID_res];
                                                                                    else
                                                                                    {
                                                                                        string cls = returnSignature.StartsWith("L") && returnSignature.EndsWith(";")? returnSignature.Substring(1).Replace(";","") : returnSignature;
                                                                                        return CreateInstancePtr(cls, null, returnPtr, null );
                                                                                    }
                                                                                }

                                                                                else
                                                                                    throw new Exception("Runtime Calling Field not error: " + name);
                                                                            else
                                                                                throw new Exception("Runtime Field not found: " + name);
                                                                        }
                                                                    }
                                                                    else
                                                                        throw new Exception("Runtime Object not found: " + name);
                                                                }),
                                                                (wrapSetProperty)((val) => {
                                                                    void*  _pEnv;
                                                                    if(AttacheThread((void*)JVMPtr,&_pEnv) != 0) throw new Exception ("Attach to thread error");
                                                                    void* _pObj = GetJVMObject(hashID);
                                                                    if(_pObj != IntPtr.Zero.ToPointer())
                                                                    {
                                                                        JVMObject vobj = (JVMObject)val;
                                                                        void*  pField;
                                                                        if(isStatic) 
                                                                        { 
                                                                            void* _pClass = IntPtr.Zero.ToPointer();
                                                                            if(getClass(_pObj,  ref _pClass) == 0)
                                                                                if(GetStaticFieldID( _pEnv, _pClass, name, returnSignature, &pField ) == 0)
                                                                                    SetStaticObjectField( _pEnv, _pClass, pField, vobj.Pointer.ToPointer());
                                                                                else
                                                                                    throw new Exception("Runtime Static Field not found: " + name);
                                                                            else
                                                                                throw new Exception("Runtime Class not found: " + name);
                                                                        } 
                                                                        else  
                                                                        { 
                                                                            if(GetFieldID( _pEnv, _pObj, name, returnSignature, &pField ) == 0)
                                                                                SetObjectField( _pEnv, _pObj, pField, vobj.Pointer.ToPointer());
                                                                            else
                                                                                throw new Exception("Runtime Field not found: " + name);
                                                                        }
                                                                    }
                                                                    else
                                                                        throw new Exception("Runtime Object not found: " + name);
                                                                })
                                                            ));
                                                    }
                                                    break;
                                                    
                                            }

                                        }

                                        else if(signature.StartsWith("M/") || signature.StartsWith("S-M/"))
                                        {
                                            bool isStatic = signature.StartsWith("S-");
                                            string name = signature.Replace("M/","").Replace("S-","");
                                            name = name.Substring(0, name.IndexOf("("));
                                            string argsSignature = signature.Substring(signature.IndexOf("(") + 1, signature.LastIndexOf(")") - 1 - signature.IndexOf("("));
                                            string returnSignature = signature.Substring(signature.IndexOf(")") + 1);

                                            string preArgsSignature = argsSignature;
                                            argsSignature = "-" + argsSignature;
                                            
                                            switch (returnSignature)
                                            {
                                                case "Z": //Boolean                                                    
                                                    expandoObject.TrySetMember(name + argsSignature, (wrapFunction<bool>)((call_args) => {
                                                        void*  _pEnv;
                                                        if(AttacheThread((void*)JVMPtr,&_pEnv) != 0) throw new Exception ("Attach to thread error");

                                                        int call_len = call_args == null ? 0 : call_args.Length;
                                                        void** ar_call = stackalloc void*[call_len];
                                                        getJavaParameters(ref ar_call, call_args);

                                                        void* _pObj = GetJVMObject(hashID);
                                                        if(_pObj != IntPtr.Zero.ToPointer())
                                                        {
                                                            void* pMethod;
                                                            if(isStatic) 
                                                            { 
                                                                void* _pClass = IntPtr.Zero.ToPointer();
                                                                if(getClass(_pObj,  ref _pClass) == 0)
                                                                    if(GetStaticMethodID( _pEnv, _pClass, name, "(" + preArgsSignature + ")" + returnSignature, &pMethod ) == 0)
                                                                        return CallStaticBooleanMethod( _pEnv, _pClass, pMethod, call_len, ar_call);
                                                                    else
                                                                        throw new Exception("Runtime Static Method not found: " + name + "(" + preArgsSignature + ")" + returnSignature );
                                                                else
                                                                    throw new Exception("Runtime Class not found: " + name + "(" + preArgsSignature + ")" + returnSignature );
                                                            } 
                                                            else  
                                                            { 
                                                                if(GetMethodID( _pEnv, _pObj, name, "(" + preArgsSignature + ")" + returnSignature, &pMethod ) == 0)
                                                                    return CallBooleanMethod( _pEnv, _pObj, pMethod, call_len, ar_call);
                                                                else
                                                                    throw new Exception("Runtime Method not found: " + name + "(" + preArgsSignature + ")" + returnSignature );
                                                            }
                                                        }
                                                        else
                                                            throw new Exception("Runtime Object not found: " + name + "(" + preArgsSignature + ")" + returnSignature );
                                                    }));
                                                    break;
                                                case "B": //Byte
                                                    expandoObject.TrySetMember(name + argsSignature, (wrapFunction<byte>)((call_args) => {
                                                        
                                                        void*  _pEnv;
                                                        if(AttacheThread((void*)JVMPtr,&_pEnv) != 0) throw new Exception ("Attach to thread error");


                                                        int call_len = call_args == null ? 0 : call_args.Length;
                                                        void** ar_call = stackalloc void*[call_len];
                                                        getJavaParameters(ref ar_call, call_args);
                                                        
                                                        void* _pObj = GetJVMObject(hashID);
                                                        if(_pObj != IntPtr.Zero.ToPointer())
                                                        {
                                                            void* pMethod;
                                                            if(isStatic) 
                                                            { 
                                                                void* _pClass = IntPtr.Zero.ToPointer();
                                                                if(getClass(_pObj,  ref _pClass) == 0)
                                                                    if(GetStaticMethodID( _pEnv, _pClass, name, "(" + preArgsSignature + ")" + returnSignature, &pMethod ) == 0)
                                                                        return CallStaticByteMethod( _pEnv, _pClass, pMethod, call_len, ar_call);
                                                                    else
                                                                        throw new Exception("Runtime Static Method not found: " + name + "(" + preArgsSignature + ")" + returnSignature );
                                                                else
                                                                    throw new Exception("Runtime Class not found: " + name + "(" + preArgsSignature + ")" + returnSignature );
                                                            } 
                                                            else  
                                                            { 
                                                                if(GetMethodID( _pEnv, _pObj, name, "(" + preArgsSignature + ")" + returnSignature, &pMethod ) == 0)
                                                                    return CallByteMethod( _pEnv, _pObj, pMethod, call_len, ar_call);
                                                                else
                                                                    throw new Exception("Runtime Method not found: " + name + "(" + preArgsSignature + ")" + returnSignature );
                                                            }
                                                        }
                                                        else
                                                            throw new Exception("Runtime Object not found: " + name + "(" + preArgsSignature + ")" + returnSignature );
                                                    }));
                                                    break;
                                            
                                                case "C": //Char
                                                    
                                                    expandoObject.TrySetMember(name + argsSignature, (wrapFunction<char>)((call_args) => {
                                                        
                                                        void*  _pEnv;
                                                        if(AttacheThread((void*)JVMPtr,&_pEnv) != 0) throw new Exception ("Attach to thread error");


                                                        int call_len = call_args == null ? 0 : call_args.Length;
                                                        void** ar_call = stackalloc void*[call_len];
                                                        getJavaParameters(ref ar_call, call_args);
                                                        
                                                        void* _pObj = GetJVMObject(hashID);
                                                        if(_pObj != IntPtr.Zero.ToPointer())
                                                        {
                                                            void* pMethod;
                                                            if(isStatic) 
                                                            { 
                                                                void* _pClass = IntPtr.Zero.ToPointer();
                                                                if(getClass(_pObj,  ref _pClass) == 0)
                                                                    if(GetStaticMethodID( _pEnv, _pClass, name, "(" + preArgsSignature + ")" + returnSignature, &pMethod ) == 0)
                                                                        return CallStaticCharMethod( _pEnv, _pClass, pMethod, call_len, ar_call);
                                                                    else
                                                                        throw new Exception("Runtime Static Method not found: " + name + "(" + preArgsSignature + ")" + returnSignature );
                                                                else
                                                                    throw new Exception("Runtime Class not found: " + name + "(" + preArgsSignature + ")" + returnSignature );
                                                            } 
                                                            else  
                                                            { 
                                                                if(GetMethodID( _pEnv, _pObj, name, "(" + preArgsSignature + ")" + returnSignature, &pMethod ) == 0)
                                                                    return CallCharMethod( _pEnv, _pObj, pMethod, call_len, ar_call);
                                                                else
                                                                    throw new Exception("Runtime Method not found: " + name + "(" + preArgsSignature + ")" + returnSignature );
                                                            }
                                                        }
                                                        else
                                                            throw new Exception("Runtime Object not found: " + name + "(" + preArgsSignature + ")" + returnSignature );
                                                    }));
                                                    break;
                                                
                                                case "S": //Short
                                                    
                                                    expandoObject.TrySetMember(name + argsSignature, (wrapFunction<short>)((call_args) => {
                                                        
                                                        void*  _pEnv;
                                                        if(AttacheThread((void*)JVMPtr,&_pEnv) != 0) throw new Exception ("Attach to thread error");


                                                        int call_len = call_args == null ? 0 : call_args.Length;
                                                        void** ar_call = stackalloc void*[call_len];
                                                        getJavaParameters(ref ar_call, call_args);
                                                        
                                                        void* _pObj = GetJVMObject(hashID);
                                                        if(_pObj != IntPtr.Zero.ToPointer())
                                                        {
                                                            void* pMethod;
                                                            if(isStatic) 
                                                            { 
                                                                void* _pClass = IntPtr.Zero.ToPointer();
                                                                if(getClass(_pObj,  ref _pClass) == 0)
                                                                    if(GetStaticMethodID( _pEnv, _pClass, name, "(" + preArgsSignature + ")" + returnSignature, &pMethod ) == 0)
                                                                        return CallStaticShortMethod( _pEnv, _pClass, pMethod, call_len, ar_call);
                                                                    else
                                                                        throw new Exception("Runtime Static Method not found: " + name + "(" + preArgsSignature + ")" + returnSignature );
                                                                else
                                                                    throw new Exception("Runtime Class not found: " + name + "(" + preArgsSignature + ")" + returnSignature );
                                                            } 
                                                            else  
                                                            { 
                                                                if(GetMethodID( _pEnv, _pObj, name, "(" + preArgsSignature + ")" + returnSignature, &pMethod ) == 0)
                                                                    return CallShortMethod( _pEnv, _pObj, pMethod, call_len, ar_call);
                                                                else
                                                                    throw new Exception("Runtime Method not found: " + name + "(" + preArgsSignature + ")" + returnSignature );
                                                            }
                                                        }
                                                        else
                                                            throw new Exception("Runtime Object not found: " + name + "(" + preArgsSignature + ")" + returnSignature );
                                                    }));
                                                    break;
                                                
                                                case "I": //Int
                                                    expandoObject.TrySetMember(name + argsSignature, (wrapFunction<int>)((call_args) => {
                                                        void*  _pEnv;
                                                        if(AttacheThread((void*)JVMPtr,&_pEnv) != 0) throw new Exception ("Attach to thread error");

                                                        int call_len = call_args == null ? 0 : call_args.Length;
                                                        void** ar_call = stackalloc void*[call_len];
                                                        getJavaParameters(ref ar_call, call_args);
                                                        
                                                        void* _pObj = GetJVMObject(hashID);
                                                        if(_pObj != IntPtr.Zero.ToPointer())
                                                        {
                                                            void* pMethod;
                                                            if(isStatic) 
                                                            { 
                                                                void* _pClass = IntPtr.Zero.ToPointer();
                                                                if(getClass(_pObj,  ref _pClass) == 0)
                                                                    if(GetStaticMethodID( _pEnv, _pClass, name, "(" + preArgsSignature + ")" + returnSignature, &pMethod ) == 0)
                                                                        return CallStaticIntMethod( _pEnv, _pClass, pMethod, call_len, ar_call);
                                                                    else
                                                                        throw new Exception("Runtime Static Method not found: " + name + "(" + preArgsSignature + ")" + returnSignature );
                                                                else
                                                                    throw new Exception("Runtime Class not found: " + name + "(" + preArgsSignature + ")" + returnSignature );
                                                            } 
                                                            else  
                                                            { 
                                                                if(GetMethodID( _pEnv, _pObj, name, "(" + preArgsSignature + ")" + returnSignature, &pMethod ) == 0)
                                                                    return CallIntMethod( _pEnv, _pObj, pMethod, call_len, ar_call);
                                                                else
                                                                    throw new Exception("Runtime Method not found: " + name + "(" + preArgsSignature + ")" + returnSignature );
                                                            }
                                                        }
                                                        else
                                                            throw new Exception("Runtime Object not found: " + name + "(" + preArgsSignature + ")" + returnSignature );
                                                    }));
                                                    break;
                                                
                                                case "J": //Long
                                                    
                                                    expandoObject.TrySetMember(name + argsSignature, (wrapFunction<long>)((call_args) => {
                                                        void*  _pEnv;
                                                        if(AttacheThread((void*)JVMPtr,&_pEnv) != 0) throw new Exception ("Attach to thread error");


                                                        int call_len = call_args == null ? 0 : call_args.Length;
                                                        void** ar_call = stackalloc void*[call_len];
                                                        getJavaParameters(ref ar_call, call_args);

                                                        void* _pObj = GetJVMObject(hashID);
                                                        if(_pObj != IntPtr.Zero.ToPointer())
                                                        {
                                                            void* pMethod;
                                                            if(isStatic) 
                                                            { 
                                                                void* _pClass = IntPtr.Zero.ToPointer();
                                                                if(getClass(_pObj,  ref _pClass) == 0)
                                                                    if(GetStaticMethodID( _pEnv, _pClass, name, "(" + preArgsSignature + ")" + returnSignature, &pMethod ) == 0)
                                                                        return CallStaticLongMethod( _pEnv, _pClass, pMethod, call_len, ar_call);
                                                                    else
                                                                        throw new Exception("Runtime Static Method not found: " + name + "(" + preArgsSignature + ")" + returnSignature );
                                                                else
                                                                    throw new Exception("Runtime Class not found: " + name + "(" + preArgsSignature + ")" + returnSignature );
                                                            } 
                                                            else  
                                                            { 
                                                                if(GetMethodID( _pEnv, _pObj, name, "(" + preArgsSignature + ")" + returnSignature, &pMethod ) == 0)
                                                                    return CallLongMethod( _pEnv, _pObj, pMethod, call_len, ar_call);
                                                                else
                                                                    throw new Exception("Runtime Method not found: " + name + "(" + preArgsSignature + ")" + returnSignature );
                                                            }
                                                        }
                                                        else
                                                            throw new Exception("Runtime Object not found: " + name + "(" + preArgsSignature + ")" + returnSignature );
                                                    }));
                                                    break;
                                                
                                                case "F": //Float
                                                    expandoObject.TrySetMember(name + argsSignature, (wrapFunction<float>)((call_args) => {
                                                        
                                                        void*  _pEnv;// = (void*)EnvPtr;
                                                        if(AttacheThread((void*)JVMPtr,&_pEnv) != 0) throw new Exception ("Attach to thread error");


                                                        int call_len = call_args == null ? 0 : call_args.Length;
                                                        void** ar_call = stackalloc void*[call_len];
                                                        getJavaParameters(ref ar_call, call_args);

                                                        void* _pObj = GetJVMObject(hashID);
                                                        if(_pObj != IntPtr.Zero.ToPointer())
                                                        {
                                                            void* pMethod;
                                                            if(isStatic) 
                                                            { 
                                                                void* _pClass = IntPtr.Zero.ToPointer();
                                                                if(getClass(_pObj,  ref _pClass) == 0)
                                                                    if(GetStaticMethodID( _pEnv, _pClass, name, "(" + preArgsSignature + ")" + returnSignature, &pMethod ) == 0)
                                                                        return CallStaticFloatMethod( _pEnv, _pClass, pMethod, call_len, ar_call);
                                                                    else
                                                                        throw new Exception("Runtime Static Method not found: " + name + "(" + preArgsSignature + ")" + returnSignature );
                                                                else
                                                                    throw new Exception("Runtime Class not found: " + name + "(" + preArgsSignature + ")" + returnSignature );
                                                            } 
                                                            else  
                                                            { 
                                                                if(GetMethodID( _pEnv, _pObj, name, "(" + preArgsSignature + ")" + returnSignature, &pMethod ) == 0)
                                                                    return CallFloatMethod( _pEnv, _pObj, pMethod, call_len, ar_call);
                                                                else
                                                                    throw new Exception("Runtime Method not found: " + name + "(" + preArgsSignature + ")" + returnSignature );
                                                            }
                                                        }
                                                        else
                                                            throw new Exception("Runtime Object not found: " + name + "(" + preArgsSignature + ")" + returnSignature );
                                                    }));
                                                    break;
                                                
                                                case "D": //Double
                                                    
                                                    expandoObject.TrySetMember(name + argsSignature, (wrapFunction<double>)((call_args) => {
                                                        void*  _pEnv;
                                                        if(AttacheThread((void*)JVMPtr,&_pEnv) != 0) throw new Exception ("Attach to thread error");


                                                        int call_len = call_args == null ? 0 : call_args.Length;
                                                        void** ar_call = stackalloc void*[call_len];
                                                        getJavaParameters(ref ar_call, call_args);
                                                        
                                                        void* _pObj = GetJVMObject(hashID);
                                                        if(_pObj != IntPtr.Zero.ToPointer())
                                                        {
                                                            void* pMethod;
                                                            if(isStatic) 
                                                            { 
                                                                void* _pClass = IntPtr.Zero.ToPointer();
                                                                if(getClass(_pObj,  ref _pClass) == 0)
                                                                    if(GetStaticMethodID( _pEnv, _pClass, name, "(" + preArgsSignature + ")" + returnSignature, &pMethod ) == 0)
                                                                        return CallStaticDoubleMethod( _pEnv, _pClass, pMethod, call_len, ar_call);
                                                                    else
                                                                        throw new Exception("Runtime Static Method not found: " + name + "(" + preArgsSignature + ")" + returnSignature );
                                                                else
                                                                    throw new Exception("Runtime Class not found: " + name + "(" + preArgsSignature + ")" + returnSignature );
                                                            } 
                                                            else  
                                                            { 
                                                                if(GetMethodID( _pEnv, _pObj, name, "(" + preArgsSignature + ")" + returnSignature, &pMethod ) == 0)
                                                                    return CallDoubleMethod( _pEnv, _pObj, pMethod, call_len, ar_call);
                                                                else
                                                                    throw new Exception("Runtime Method not found: " + name + "(" + preArgsSignature + ")" + returnSignature );
                                                            }
                                                        }
                                                        else
                                                            throw new Exception("Runtime Object not found: " + name + "(" + preArgsSignature + ")" + returnSignature );
                                                    }));
                                                    break;
                                                
                                                case "V": //Void
                                                    expandoObject.TrySetMember(name + argsSignature, (wrapAction)((call_args) => {
                                                        
                                                        void*  _pEnv;
                                                        if(AttacheThread((void*)JVMPtr,&_pEnv) != 0) throw new Exception ("Attach to thread error");

                                                        // Console.WriteLine("CALLING VOID: " + );


                                                        int call_len = call_args == null ? 0 : call_args.Length;
                                                        void** ar_call = stackalloc void*[call_len];
                                                        getJavaParameters(ref ar_call, call_args);
                                                        
                                                        void* _pObj = GetJVMObject(hashID);
                                                        if(_pObj != IntPtr.Zero.ToPointer())
                                                        {
                                                            void* pMethod;
                                                            if(isStatic) 
                                                            { 
                                                                void* _pClass = IntPtr.Zero.ToPointer();
                                                                if(getClass(_pObj,  ref _pClass) == 0)
                                                                    if(GetStaticMethodID( _pEnv, _pClass, name, "(" + preArgsSignature + ")" + returnSignature, &pMethod ) == 0)
                                                                        CallStaticVoidMethod( _pEnv, _pClass, pMethod, call_len, ar_call);
                                                                    else
                                                                        throw new Exception("Runtime Static Method not found: " + name + "(" + preArgsSignature + ")" + returnSignature );
                                                                else
                                                                    throw new Exception("Runtime Class not found: " + name + "(" + preArgsSignature + ")" + returnSignature );
                                                            } 
                                                            else  
                                                            { 
                                                                if(GetMethodID( _pEnv, _pObj, name, "(" + preArgsSignature + ")" + returnSignature, &pMethod ) == 0)
                                                                    CallVoidMethod( _pEnv, _pObj, pMethod, call_len, ar_call);
                                                                else
                                                                    throw new Exception("Runtime Method not found: " + name + "(" + preArgsSignature + ")" + returnSignature );
                                                            }
                                                        }
                                                        else
                                                            throw new Exception("Runtime Object not found: " + name + "(" + preArgsSignature + ")" + returnSignature );
                                                    }));
                                                    break;
                                                
                                                case "Ljava/lang/String;": //String
                                                    
                                                    expandoObject.TrySetMember(name + argsSignature, (wrapFunction<string>)((call_args) => {
                                                        
                                                        void*  _pEnv;
                                                        if(AttacheThread((void*)JVMPtr,&_pEnv) != 0) throw new Exception ("Attach to thread error");


                                                        int call_len = call_args == null ? 0 : call_args.Length;
                                                        void** ar_call = stackalloc void*[call_len];
                                                        getJavaParameters(ref ar_call, call_args);
                                                        
                                                        void* pObjResult = IntPtr.Zero.ToPointer();
                                                        void* _pObj = GetJVMObject(hashID);
                                                        if(_pObj != pObjResult)
                                                        {
                                                            void* pMethod;
                                                            if(isStatic) 
                                                            { 
                                                                void* _pClass = IntPtr.Zero.ToPointer();
                                                                if(getClass(_pObj,  ref _pClass) == 0)
                                                                    if(GetStaticMethodID( _pEnv, _pClass, name, "(" + preArgsSignature + ")" + returnSignature, &pMethod ) == 0)
                                                                        CallStaticObjectMethod( _pEnv, _pClass, pMethod, &pObjResult, call_len, ar_call);
                                                                    else
                                                                        throw new Exception("Runtime Static Method not found: " + name + "(" + preArgsSignature + ")" + returnSignature );
                                                                else
                                                                    throw new Exception("Runtime Class not found: " + name + "(" + preArgsSignature + ")" + returnSignature );
                                                            } 
                                                            else  
                                                            { 
                                                                if(GetMethodID( _pEnv, _pObj, name, "(" + preArgsSignature + ")" + returnSignature, &pMethod ) == 0)
                                                                    CallObjectMethod( _pEnv, _pObj, pMethod, &pObjResult, call_len, ar_call);
                                                                else
                                                                    throw new Exception("Runtime Method not found: " + name + "(" + preArgsSignature + ")" + returnSignature );
                                                            }

                                                            return GetNetString(_pEnv, pObjResult);
                                                        }
                                                        else
                                                            throw new Exception("Runtime Object not found: " + name + "(" + preArgsSignature + ")" + returnSignature );
                                                    }));
                                                    break;

                                                case "Ljava/time/LocalDateTime;": //String
                                                    
                                                    expandoObject.TrySetMember(name + argsSignature, (wrapFunction<DateTime>)((call_args) => {

                                                        void*  _pEnv;
                                                        if(AttacheThread((void*)JVMPtr,&_pEnv) != 0) throw new Exception ("Attach to thread error");


                                                        int call_len = call_args == null ? 0 : call_args.Length;
                                                        void** ar_call = stackalloc void*[call_len];
                                                        getJavaParameters(ref ar_call, call_args);
                                                        
                                                        void* pObjResult = IntPtr.Zero.ToPointer();
                                                        void* _pObj = GetJVMObject(hashID);
                                                        if(_pObj != pObjResult)
                                                        {
                                                            void* pMethod;
                                                            if(isStatic) 
                                                            { 
                                                                void* _pClass = IntPtr.Zero.ToPointer();
                                                                if(getClass(_pObj,  ref _pClass) == 0)
                                                                    if(GetStaticMethodID( _pEnv, _pClass, name, "(" + preArgsSignature + ")" + returnSignature, &pMethod ) == 0)
                                                                        CallStaticObjectMethod( _pEnv, _pClass, pMethod, &pObjResult, call_len, ar_call);
                                                                    else
                                                                        throw new Exception("Runtime Static Method not found: " + name + "(" + preArgsSignature + ")" + returnSignature );
                                                                else
                                                                    throw new Exception("Runtime Class not found: " + name + "(" + preArgsSignature + ")" + returnSignature );
                                                            } 
                                                            else  
                                                            { 
                                                                if(GetMethodID( _pEnv, _pObj, name, "(" + preArgsSignature + ")" + returnSignature, &pMethod ) == 0)
                                                                    CallObjectMethod( _pEnv, _pObj, pMethod, &pObjResult, call_len, ar_call);
                                                                else
                                                                    throw new Exception("Runtime Method not found: " + name + "(" + preArgsSignature + ")" + returnSignature );
                                                            }
                                                        
                                                            return GetNetDateTime(_pEnv, pObjResult);
                                                        }
                                                        else
                                                            throw new Exception("Runtime Object not found: " + name + "(" + preArgsSignature + ")" + returnSignature );
                                                    }));
                                                    break;

                                                default:
                                                    
                                                    if(returnSignature.StartsWith("["))
                                                    {
                                                        expandoObject.TrySetMember(name + argsSignature, (wrapFunction<object[]>)((call_args)  => {
                                                            void*  _pEnv;
                                                            if(AttacheThread((void*)JVMPtr,&_pEnv) != 0) throw new Exception ("Attach to thread error");


                                                            int call_len = call_args == null ? 0 : call_args.Length;
                                                            void** ar_call = stackalloc void*[call_len];
                                                            getJavaParameters(ref ar_call, call_args);


                                                            void* pObjResult = IntPtr.Zero.ToPointer();
                                                            void* _pObj = GetJVMObject(hashID);
                                                            if(_pObj != pObjResult)
                                                            {
                                                                void* pMethod;
                                                                if(isStatic) 
                                                                { 
                                                                    void* _pClass = IntPtr.Zero.ToPointer();
                                                                    if(getClass(_pObj,  ref _pClass) == 0)
                                                                        if(GetStaticMethodID( _pEnv, _pClass, name, "(" + preArgsSignature + ")" + returnSignature, &pMethod ) == 0)
                                                                            CallStaticObjectMethod( _pEnv, _pClass, pMethod, &pObjResult, call_len, ar_call);
                                                                        else
                                                                            throw new Exception("Runtime Static Method not found: " + name + "(" + preArgsSignature + ")" + returnSignature );
                                                                    else
                                                                        throw new Exception("Runtime Class not found: " + name + "(" + preArgsSignature + ")" + returnSignature );
                                                                } 
                                                                else  
                                                                { 
                                                                    if(GetMethodID( _pEnv, _pObj, name, "(" + preArgsSignature + ")" + returnSignature, &pMethod ) == 0)
                                                                        CallObjectMethod( _pEnv, _pObj, pMethod, &pObjResult, call_len, ar_call);
                                                                    else
                                                                        throw new Exception("Runtime Method not found: " + name + "(" + preArgsSignature + ")" + returnSignature );
                                                                }

                                                                IntPtr ptr = new IntPtr(pObjResult);

                                                                if(ptr == IntPtr.Zero)
                                                                    return null;
                                                            
                                                                return getJavaArray(ptr, returnSignature);
                                                            }
                                                            else
                                                                throw new Exception("Runtime Object not found: " + name + "(" + preArgsSignature + ")" + returnSignature );
                                                        }));
                                                    }
                                                    else
                                                    {
                                                        expandoObject.TrySetMember(name + argsSignature, (wrapFunction<object>)((call_args)  => {
                                                            
                                                            void*  _pEnv;
                                                            if(AttacheThread((void*)JVMPtr,&_pEnv) != 0) throw new Exception ("Attach to thread error");


                                                            int call_len = call_args == null ? 0 : call_args.Length;
                                                            void** ar_call = stackalloc void*[call_len];
                                                            getJavaParameters(ref ar_call, call_args);

                                                            void* pObjResult = IntPtr.Zero.ToPointer();
                                                            void* _pObj = GetJVMObject(hashID);
                                                            if(_pObj != pObjResult)
                                                            {
                                                                void* pMethod;
                                                                if(isStatic) 
                                                                { 
                                                                    void* _pClass = IntPtr.Zero.ToPointer();
                                                                    if(getClass(_pObj,  ref _pClass) == 0)
                                                                        if(GetStaticMethodID( _pEnv, _pClass, name, "(" + preArgsSignature + ")" + returnSignature, &pMethod ) == 0)
                                                                            CallStaticObjectMethod( _pEnv, _pClass, pMethod, &pObjResult, call_len, ar_call);
                                                                        else
                                                                            throw new Exception("Runtime Static Method not found: " + name + "(" + preArgsSignature + ")" + returnSignature );
                                                                    else
                                                                        throw new Exception("Runtime Class not found: " + name + "(" + preArgsSignature + ")" + returnSignature );
                                                                } 
                                                                else  
                                                                { 
                                                                    if(GetMethodID( _pEnv, _pObj, name, "(" + preArgsSignature + ")" + returnSignature, &pMethod ) == 0)
                                                                        CallObjectMethod( _pEnv, _pObj, pMethod, &pObjResult, call_len, ar_call);
                                                                    else
                                                                        throw new Exception("Runtime Method not found: " + name + "(" + preArgsSignature + ")" + returnSignature );
                                                                }
                                                                
                                                                IntPtr returnPtr = new IntPtr(pObjResult);

                                                                if(returnPtr == IntPtr.Zero)
                                                                    return null;

                                                                int hashID_res = getHashCode(pObjResult);

                                                                
                                                                if(JVMDelegate.DB.ContainsKey(hashID_res))
                                                                    return JVMDelegate.DB[hashID_res];


                                                                else if(Runtime.DB.ContainsKey(hashID_res))
                                                                {
                                                                    // Console.WriteLine("Runtime Exists: " + hashID_res);
                                                                    return Runtime.DB[hashID_res];
                                                                }

                                                                
                                                                else if(JVMObject.DB.ContainsKey(hashID_res))
                                                                {
                                                                    // Console.WriteLine("JVMObject Exists: " + hashID_res);

                                                                    if(JVMObject.DB[hashID_res] is JVMTuple)
                                                                    {
                                                                        JVMTuple jobj = JVMObject.DB[hashID_res] as JVMTuple;
                                                                        return jobj.jVMTuple;
                                                                    }
                                                                    return JVMObject.DB[hashID_res];
                                                                }

                                                                else
                                                                {
                                                                    string cls = returnSignature.StartsWith("L") && returnSignature.EndsWith(";") ? returnSignature.Substring(1).Replace(";","").Replace("/",".") : returnSignature;

                                                                    return getObject(_pEnv, cls, pObjResult);
                                                                }
                                                            }
                                                            else
                                                                throw new Exception("Runtime Object not found: " + name + "(" + preArgsSignature + ")" + returnSignature );
                                                        }));
                                                    }

                                                    break;
                                            }
                                        
                                        }
                                    }

                                    return expandoObject;
                                }
                                else
                                    throw new Exception("Error Executing signature: " + sClass);
                            }
                            else
                                throw new Exception("CLASS NOT FOUND: " + sClass);
                        }
                        else 
                            throw new Exception("can not find method Signatures: " + sClass);
                    }
                    else 
                        throw new Exception("ArrayClasses get method error");
                }
                else 
                    throw new Exception("CLRRuntime class not found error");
            }
            else 
                throw new Exception("JVM Engine not loaded");
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }
    }

    internal class ObjectWrapper
    {
        public object Object;

        public ObjectWrapper(object obj)
        {
            this.Object = obj;
        }
    }

    internal static class TypeExtensions
    {
        public static bool IsArrayOf<T>(this Type type)
        {
            return type == typeof (T[]);
        }
    } 
}

namespace JVM
{
    public class Runtime : QuantApp.Kernel.JVM.Runtime {}
}
