/*
 * The MIT License (MIT)
 * Copyright (c) Arturo Rodriguez All rights reserved.
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

 // NEED TO IMPLEMENT https://docs.microsoft.com/en-us/dotnet/api/system.runtime.compilerservices.conditionalweaktable-2?view=netframework-4.8
 // https://docs.microsoft.com/en-us/dotnet/api/system.weakreference?view=netframework-4.8
 
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

        
        internal static ConcurrentDictionary<int,object> __DB = new ConcurrentDictionary<int, object>();
        internal static ConcurrentDictionary<int,WeakReference> DB = new ConcurrentDictionary<int, WeakReference>();
        
        
        // internal static ConditionalWeakTable<int,object> DB = new ConditionalWeakTable<int, object>();
        

        [DllImport(JVMDll)] private unsafe static extern int  JNI_CreateJavaVM(void** ppVm, void** ppEnv, void* pArgs);
        [DllImport(InvokerDll)] private unsafe static extern void SetfnGetProperty(void* func);
        [DllImport(InvokerDll)] private unsafe static extern void SetfnRemoveObject(void* func);
        [DllImport(InvokerDll)] private unsafe static extern void SetfnSetProperty(void* func);
        [DllImport(InvokerDll)] private unsafe static extern void SetfnInvokeFunc(void* func);
        [DllImport(InvokerDll)] private unsafe static extern void SetfnCreateInstance(void* func);
        [DllImport(InvokerDll)] public unsafe static extern void SetfnInvoke(void* func);
        [DllImport(InvokerDll)] public unsafe static extern void SetfnRegisterFunc(void* func);
        
        
        [DllImport(InvokerDll)] private unsafe static extern int  AttacheThread(void* ppVm, void** pEnv);
        [DllImport(InvokerDll)] private unsafe static extern int  DetacheThread(void* ppVm);
        [DllImport(InvokerDll)] private unsafe static extern int  MakeJavaVMInitArgs(string classpath, string libpath, void** ppArgs );
        [DllImport(InvokerDll)] private unsafe static extern void FreeJavaVMInitArgs( void* pArgs );

        [DllImport(InvokerDll)] internal unsafe static extern int  FindClass( void* pEnv, String sClass, void** ppClass);
        

        [DllImport(InvokerDll)] internal unsafe static extern int NewObjectP(void*  pEnv, void* sType, String szArgs, int len, void** pArgs , void** ppObj);
        [DllImport(InvokerDll)] internal unsafe static extern int NewObject(void*  pEnv, String sType, String szArgs, int len, void** pArgs , void** ppObj);
        
        [DllImport(InvokerDll)] internal unsafe static extern int NewBooleanObject(void*  pEnv, bool val , void** ppObj);
        [DllImport(InvokerDll)] internal unsafe static extern int NewByteObject(void*  pEnv, byte val , void** ppObj);
        [DllImport(InvokerDll)] internal unsafe static extern int NewCharacterObject(void*  pEnv, char val , void** ppObj);
        [DllImport(InvokerDll)] internal unsafe static extern int NewShortObject(void*  pEnv, short val , void** ppObj);
        [DllImport(InvokerDll)] internal unsafe static extern int NewIntegerObject(void*  pEnv, int val , void** ppObj);
        [DllImport(InvokerDll)] internal unsafe static extern int NewLongObject(void*  pEnv, long val , void** ppObj);
        [DllImport(InvokerDll)] internal unsafe static extern int NewFloatObject(void*  pEnv, float val , void** ppObj);
        [DllImport(InvokerDll)] internal unsafe static extern int NewDoubleObject(void*  pEnv, double val , void** ppObj);


        [DllImport(InvokerDll)] internal unsafe static extern int GetStaticMethodID(void*  pEnv, void*  pClass, String szName, String szArgs, void** ppMid);
        [DllImport(InvokerDll)] private unsafe static extern int GetMethodID(void*  pEnv, void*  pClass, String szName, String szArgs, void** ppMid);

        [DllImport(InvokerDll)] private unsafe static extern int GetStaticFieldID(void*  pEnv, void*  pClass, String szName, String sig, void** ppFid);
        [DllImport(InvokerDll)] private unsafe static extern int GetFieldID(void*  pEnv, void*  pClass, String szName, String sig, void** ppFid);

        [DllImport(InvokerDll)] private unsafe static extern int CallStaticVoidMethod(void* pEnv, void* pClass, void* pMid, int len, void** pArgs);
        [DllImport(InvokerDll)] private unsafe static extern int CallVoidMethod(void* pEnv, void* pClass, void* pMid, int len, void** pArgs);

        [DllImport(InvokerDll)] private unsafe static extern int GetObjectClass(void* pEnv, void* pObject, void** pClass, void** nameClass);
        [DllImport(InvokerDll)] internal unsafe static extern int CallStaticObjectMethod(void* pEnv, void* pClass, void* pMid, void** pObject, int len, void** pArgs);
        [DllImport(InvokerDll)] private unsafe static extern int CallObjectMethod(void* pEnv, void* pClass, void* pMid, void** pObject, int len, void** pArgs);
        [DllImport(InvokerDll)] private unsafe static extern int GetStaticObjectField(void* pEnv, void* pClass, void* pMid, void** pObject);
        [DllImport(InvokerDll)] private unsafe static extern int GetObjectField(void* pEnv, void* pClass, void* pMid, void** pObject);
        [DllImport(InvokerDll)] private unsafe static extern int SetStaticObjectField(void* pEnv, void* pClass, void* pMid, void* pObject);
        [DllImport(InvokerDll)] private unsafe static extern int SetObjectField(void* pEnv, void* pClass, void* pMid, void* pObject);


        [DllImport(InvokerDll)] private unsafe static extern int CallStaticIntMethod(void* pEnv, void* pClass, void* pMid, int len, void** pArgs, int* res);
        [DllImport(InvokerDll)] private unsafe static extern int CallIntMethod(void* pEnv, void* pClass, void* pMid, int len, void** pArgs, int* res);
        [DllImport(InvokerDll)] private unsafe static extern int GetStaticIntField(void* pEnv, void* pClass, void* pMid, int* res);
        [DllImport(InvokerDll)] private unsafe static extern int GetIntField(void* pEnv, void* pClass, void* pMid, int* res);
        [DllImport(InvokerDll)] private unsafe static extern int SetStaticIntField(void* pEnv, void* pClass, void* pMid, int val);
        [DllImport(InvokerDll)] private unsafe static extern int SetIntField(void* pEnv, void* pClass, void* pMid, int val);
        
        
        [DllImport(InvokerDll)] private unsafe static extern int CallStaticLongMethod(void* pEnv, void* pClass, void* pMid, int len, void** pArgs, long* res);
        [DllImport(InvokerDll)] private unsafe static extern int CallLongMethod(void* pEnv, void* pClass, void* pMid, int len, void** pArgs, long* res);
        [DllImport(InvokerDll)] private unsafe static extern int GetStaticLongField(void* pEnv, void* pClass, void* pMid, long* res);
        [DllImport(InvokerDll)] private unsafe static extern int GetLongField(void* pEnv, void* pClass, void* pMid, long* res);
        [DllImport(InvokerDll)] private unsafe static extern int SetStaticLongField(void* pEnv, void* pClass, void* pMid, long val);
        [DllImport(InvokerDll)] private unsafe static extern int SetLongField(void* pEnv, void* pClass, void* pMid, long val);
        
        [DllImport(InvokerDll)] private unsafe static extern int CallStaticFloatMethod(void* pEnv, void* pClass, void* pMid, int len,  void** pArgs, float* val);
        [DllImport(InvokerDll)] private unsafe static extern int CallFloatMethod(void* pEnv, void* pClass, void* pMid, int len,  void** pArgs, float* val);
        [DllImport(InvokerDll)] private unsafe static extern int GetStaticFloatField(void* pEnv, void* pClass, void* pMid, float* val);
        [DllImport(InvokerDll)] private unsafe static extern int GetFloatField(void* pEnv, void* pClass, void* pMid, float* val);
        [DllImport(InvokerDll)] private unsafe static extern int SetStaticFloatField(void* pEnv, void* pClass, void* pMid, float val);
        [DllImport(InvokerDll)] private unsafe static extern int SetFloatField(void* pEnv, void* pClass, void* pMid, float val);

        [DllImport(InvokerDll)] private unsafe static extern int CallStaticDoubleMethod(void* pEnv, void* pClass, void* pMid, int len, void** pArgs, double* res);
        [DllImport(InvokerDll)] private unsafe static extern int CallDoubleMethod( void* pEnv, void* pClass, void* pMid, int len, void** pArgs, double* res);
        [DllImport(InvokerDll)] private unsafe static extern int GetStaticDoubleField(void* pEnv, void* pClass, void* pMid, double* res);
        [DllImport(InvokerDll)] private unsafe static extern int GetDoubleField( void* pEnv, void* pClass, void* pMid, double* res);
        [DllImport(InvokerDll)] private unsafe static extern int SetStaticDoubleField(void* pEnv, void* pClass, void* pMid, double val);
        [DllImport(InvokerDll)] private unsafe static extern int SetDoubleField(void* pEnv, void* pClass, void* pMid, double val);


        [DllImport(InvokerDll)] private unsafe static extern int CallStaticBooleanMethod(void* pEnv, void* pClass, void* pMid, int len, void** pArgs, bool* val);
        [DllImport(InvokerDll)] private unsafe static extern int CallBooleanMethod( void* pEnv, void* pClass, void* pMid, int len, void** pArgs, bool* val);
        [DllImport(InvokerDll)] private unsafe static extern int GetStaticBooleanField(void* pEnv, void* pClass, void* pMid, bool* val);
        [DllImport(InvokerDll)] private unsafe static extern int GetBooleanField( void* pEnv, void* pClass, void* pMid, bool* val);
        [DllImport(InvokerDll)] private unsafe static extern int SetStaticBooleanField(void* pEnv, void* pClass, void* pMid, bool val);
        [DllImport(InvokerDll)] private unsafe static extern int SetBooleanField(void* pEnv, void* pClass, void* pMid, bool val);

        
        [DllImport(InvokerDll)] private unsafe static extern int CallStaticByteMethod(void* pEnv, void* pClass, void* pMid, int len, void** pArgs, byte* val);
        [DllImport(InvokerDll)] private unsafe static extern int CallByteMethod( void* pEnv, void* pClass, void* pMid, int len, void** pArgs, byte* val);
        [DllImport(InvokerDll)] private unsafe static extern int GetStaticByteField(void* pEnv, void* pClass, void* pMid, byte* val);
        [DllImport(InvokerDll)] private unsafe static extern int GetByteField( void* pEnv, void* pClass, void* pMid, byte* val);
        [DllImport(InvokerDll)] private unsafe static extern int SetStaticByteField(void* pEnv, void* pClass, void* pMid, byte val);
        [DllImport(InvokerDll)] private unsafe static extern int SetByteField(void* pEnv, void* pClass, void* pMid, byte val);


        [DllImport(InvokerDll)] private unsafe static extern int CallStaticCharMethod(void* pEnv, void* pClass, void* pMid, int len, void** pArgs, char* val);
        [DllImport(InvokerDll)] private unsafe static extern int CallCharMethod( void* pEnv, void* pClass, void* pMid, int len, void** pArgs, char* val);
        [DllImport(InvokerDll)] private unsafe static extern int GetStaticCharField(void* pEnv, void* pClass, void* pMid, char* val);
        [DllImport(InvokerDll)] private unsafe static extern int GetCharField( void* pEnv, void* pClass, void* pMid, char* val);
        [DllImport(InvokerDll)] private unsafe static extern int SetStaticCharField(void* pEnv, void* pClass, void* pMid, char val);
        [DllImport(InvokerDll)] private unsafe static extern int SetCharField(void* pEnv, void* pClass, void* pMid, char val);


        [DllImport(InvokerDll)] private unsafe static extern int CallStaticShortMethod(void* pEnv, void* pClass, void* pMid, int len, void** pArgs, short* val);
        [DllImport(InvokerDll)] private unsafe static extern int CallShortMethod( void* pEnv, void* pClass, void* pMid, int len, void** pArgs, short* val);
        [DllImport(InvokerDll)] private unsafe static extern int GetStaticShortField(void* pEnv, void* pClass, void* pMid, short* val);
        [DllImport(InvokerDll)] private unsafe static extern int GetShortField( void* pEnv, void* pClass, void* pMid, short* val);
        [DllImport(InvokerDll)] private unsafe static extern int SetStaticShortField(void* pEnv, void* pClass, void* pMid, short val);
        [DllImport(InvokerDll)] private unsafe static extern int SetShortField(void* pEnv, void* pClass, void* pMid, short val);


        [DllImport(InvokerDll)] internal unsafe static extern void* GetJavaString( void* pEnv, string str );
        [DllImport(InvokerDll)] internal unsafe static extern string GetNetString( void* pEnv, void* jstr );

        [DllImport(InvokerDll)] internal unsafe static extern string GetException( void* pEnv );


        [DllImport(InvokerDll)] internal unsafe static extern int NewObjectArrayP( void*  pEnv, int nDimension, void* pClass, void** ppArray );
        [DllImport(InvokerDll)] internal unsafe static extern int NewObjectArray( void*  pEnv, int nDimension, String sType, void** ppArray );
        [DllImport(InvokerDll)] internal unsafe static extern int SetObjectArrayElement( void* pEnv, void* pArray, int index, void* value);
        [DllImport(InvokerDll)] internal unsafe static extern int GetObjectArrayElement( void* pEnv, void* pArray, int index, void** pObject);


        [DllImport(InvokerDll)] internal unsafe static extern int NewIntArray( void* pEnv, int nDimension, void** ppArray );
        [DllImport(InvokerDll)] internal unsafe static extern int SetIntArrayElement( void* pEnv, void* pArray, int index, int value);
        [DllImport(InvokerDll)] internal unsafe static extern int GetIntArrayElement( void* pEnv, void* pArray, int index);
        

        [DllImport(InvokerDll)] internal unsafe static extern int NewLongArray( void* pEnv, int nDimension, void** ppArray );
        [DllImport(InvokerDll)] internal unsafe static extern int SetLongArrayElement( void* pEnv, void* pArray, int index, long value);
        [DllImport(InvokerDll)] internal unsafe static extern long GetLongArrayElement( void* pEnv, void* pArray, int index);
        

        [DllImport(InvokerDll)] internal unsafe static extern int NewFloatArray( void* pEnv, int nDimension, void** ppArray );
        [DllImport(InvokerDll)] internal unsafe static extern int SetFloatArrayElement( void* pEnv, void* pArray, int index, float value);
        [DllImport(InvokerDll)] internal unsafe static extern float GetFloatArrayElement( void* pEnv, void* pArray, int index);


        [DllImport(InvokerDll)] internal unsafe static extern int NewDoubleArray( void* pEnv, int nDimension, void** ppArray );
        [DllImport(InvokerDll)] internal unsafe static extern int SetDoubleArrayElement( void* pEnv, void* pArray, int index, double value);
        [DllImport(InvokerDll)] internal unsafe static extern double GetDoubleArrayElement( void* pEnv, void* pArray, int index);


        [DllImport(InvokerDll)] internal unsafe static extern int NewBooleanArray( void* pEnv, int nDimension, void** ppArray );
        [DllImport(InvokerDll)] internal unsafe static extern int SetBooleanArrayElement( void* pEnv, void* pArray, int index, bool value);
        [DllImport(InvokerDll)] internal unsafe static extern bool GetBooleanArrayElement( void* pEnv, void* pArray, int index);

        [DllImport(InvokerDll)] internal unsafe static extern int NewByteArray( void* pEnv, int nDimension, void** ppArray );
        [DllImport(InvokerDll)] internal unsafe static extern int SetByteArrayElement( void* pEnv, void* pArray, int index, byte value);
        [DllImport(InvokerDll)] internal unsafe static extern byte GetByteArrayElement( void* pEnv, void* pArray, int index);

        [DllImport(InvokerDll)] internal unsafe static extern int NewShortArray( void* pEnv, int nDimension, void** ppArray );
        [DllImport(InvokerDll)] internal unsafe static extern int SetShortArrayElement( void* pEnv, void* pArray, int index, short value);
        [DllImport(InvokerDll)] internal unsafe static extern short GetShortArrayElement( void* pEnv, void* pArray, int index);
        

        [DllImport(InvokerDll)] internal unsafe static extern int NewCharArray( void* pEnv, int nDimension, void** ppArray );
        [DllImport(InvokerDll)] internal unsafe static extern int SetCharArrayElement( void* pEnv, void* pArray, int index, char value);
        [DllImport(InvokerDll)] internal unsafe static extern char GetCharArrayElement( void* pEnv, void* pArray, int index);
        

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
        private static SetRemoveObject delRemoveObject;
        private static GCHandle gchRemoveObject;
        
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

            delRemoveObject = new SetRemoveObject(Java_app_quant_clr_CLRRuntime_nativeRemoveObject);
            gchRemoveObject = GCHandle.Alloc(delRemoveObject);
            SetfnRemoveObject(Marshal.GetFunctionPointerForDelegate<SetRemoveObject>(delRemoveObject).ToPointer());
            
            
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

        // internal static ConcurrentDictionary<object, int> DBID = new ConcurrentDictionary<object, int>();
        internal static ConditionalWeakTable<object, object> DBID = new ConditionalWeakTable<object, object>();
        internal static ConcurrentDictionary<int, int> _DBID = new ConcurrentDictionary<int, int>();
        private readonly static object objLock_GetID = new object();
        public static unsafe int GetID(object obj, bool cache)
        {
            lock(objLock_GetID)
            {
                if(obj == null)
                    return 0;

                var _type = obj.GetType();
                

                // if(obj is JVMObject)
                // {
                //     int id = (obj as JVMObject).JavaHashCode;
                //     if(!__DB.ContainsKey(id))
                //         __DB.TryAdd(id, obj);
                //         // __DB[id] = obj;
                //     obj.RegisterGCEvent(id, delegate(object _obj, int _id)
                //     {
                //         RemoveID(_id);
                //     });
                //     return (obj as JVMObject).JavaHashCode;
                // }
                // else if(obj is IJVMTuple)
                // {
                //     int id = (obj as IJVMTuple).JVMObject.JavaHashCode;
                //     // if(!__DB.ContainsKey(id))
                //     //     __DB.TryAdd(id, obj);
                //     __DB[id] = obj;
                //     obj.RegisterGCEvent(id, delegate(object _obj, int _id)
                //     {
                //         RemoveID(_id);
                //     });
                //     return (obj as IJVMTuple).JVMObject.JavaHashCode;
                // }

                object _id;

                if(!DBID.TryGetValue(obj, out _id))
                {
                    
                    var id = CreateID();
                    obj.RegisterGCEvent(id, delegate(object _obj, int _id)
                    {
                        RemoveID(_id);
                    });
                    if(cache)
                    {
                        // switch(Type.GetTypeCode(_type))
                        // { 
                        //     case TypeCode.Boolean:
                        //         break;

                        //     case TypeCode.Byte:
                        //         break;

                        //     case TypeCode.Char:
                        //         break;

                        //     case TypeCode.Int16:
                        //         break;

                        //     case TypeCode.Int32: 
                        //         break;
                                
                        //     case TypeCode.Int64:
                        //         break;

                        //     case TypeCode.Single:
                        //         break;

                        //     case TypeCode.Double:
                        //         break;

                        //     case TypeCode.String:
                        //         break;

                        //     case TypeCode.DateTime:
                        //         break;

                        //     default:
                        //         // Console.WriteLine("------ GETID: " + obj);
                        //         void*  pEnv;// = (void*)EnvPtr;
                        //         if(AttacheThread((void*)JVMPtr,&pEnv) != 0) throw new Exception ("Attach to thread error");
                        //         // void* ptr = getObjectPointer(pEnv, obj);
                        //         // RegisterJVMObject(pEnv, id, ptr);

                                
                                
                        //         // __DB.TryAdd(id, obj);
                        //         break;
                        // }
                        
                        
                        __DB.TryAdd(id, obj);
                    }
                    DBID.AddOrUpdate(obj, id);
                    return id;

                    // for(int i = 0; i < 100; i++)
                    // {
                    //     var id = System.Guid.NewGuid().ToString().GetHashCode();
                    //     if(!_DBID.ContainsKey(id))
                    //     {
                    //         _DBID.TryAdd(id, id);
                            
                    //         // THIS IS NECESSARY
                    //         // if(cache) //NO
                    //         __DB.TryAdd(id, obj);

                    //         obj.RegisterGCEvent(id, delegate(object _obj, int _id)
                    //         {
                    //             // int __id;
                    //             // _DBID.TryRemove(_id, out __id);
                    //             // Console.WriteLine("Object(" + _obj + ") with hash code " + _id + " recently collected: ");
                    //             RemoveID(_id);
                    //         });
                            
                    //         DBID.AddOrUpdate(obj, id);
                    //         return id;
                    //     }
                    //     // Console.WriteLine("++++ GETID RETRY: " + i + " " + obj + " == " + obj.GetType());
                    // }

                    // Console.WriteLine("------------- GETID ERROR: " + obj);
                }

                // if(cache)
                // {
                //     // Console.WriteLine(obj.GetType() + " --> "  + obj);
                //     __DB.TryAdd((int)_id, obj);
                // }

                return (int)_id;
            }
        }

        private readonly static object objLock_CreateID = new object();
        internal unsafe static int CreateID()
        {
            lock(objLock_CreateID)
            {
                void*  pEnv;
                if(AttacheThread((void*)JVMPtr,&pEnv) != 0) throw new Exception ("Attach to thread error");
                void* pNetBridgeClass;
                void* pSetPathMethod;

                if(FindClass( pEnv, "app/quant/clr/CLRRuntime", &pNetBridgeClass) == 0 )
                {
                    if( GetStaticMethodID( pEnv, pNetBridgeClass, "CreateID", "()I", &pSetPathMethod ) == 0 )
                    // if( GetStaticMethodID( pEnv, pNetBridgeClass, "RegisterObject", "(Ljava/lang/Object;)V", &pSetPathMethod ) == 0 )
                    {
                        void** pArg_lcs = stackalloc void*[0];
                        // pArg_lcs[0] = *(void**)&hashCode;
                        // pArg_lcs[0] = *(void**)&id;
                        // void** pArg_lcs = stackalloc void*[1];
                        // pArg_lcs[0] = pObj;
                        
                        int _res = 0;
                        if(CallStaticIntMethod( pEnv, pNetBridgeClass, pSetPathMethod, 0, pArg_lcs,  &_res) != 0)
                            Console.WriteLine("CLR JAVA CreateID Object error");

                        // Console.WriteLine("CS getHashCode: " + _res);

                        return _res;
                    }
                    else
                        Console.WriteLine("CreateID method not found");

                    
                }

                return 0;
            }
        }

        private static unsafe int Java_app_quant_clr_CLRRuntime_nativeRemoveObject(void* pEnv, int ptr)
        {
            // return 0;
            // if(DB.ContainsKey(ptr) && DB[ptr].IsAlive)
            //     DB[ptr].Target.Dispose();

            // if(JVMObject.__DB.ContainsKey(ptr)) //DEF NO
            // {
            //     JVMObject oo;
            //     JVMObject.__DB.TryRemove(ptr, out oo);
            //     Console.WriteLine("RUNTIME DELETE JVMObject: " + ptr + " " + oo);
            // }
            
            // if(_DBID.ContainsKey(ptr))
            // {
            //     int _i;
            //     _DBID.TryRemove(ptr, out _i);
            // }

            // if(!JVMDelegate.DB.ContainsKey(ptr) && DB.ContainsKey(ptr))
            // {
            //     WeakReference _o;
            //     DB.TryRemove(ptr, out _o);
            // }
            
            // if(MethodDB.ContainsKey(ptr)) //DEF NO
            // {
            //     ConcurrentDictionary<string,MethodInfo> _o;
            //     MethodDB.TryRemove(ptr, out _o);
            // }

            if(__DB.ContainsKey(ptr))
            {
                object oo;
                __DB.TryRemove(ptr, out oo);

                // Console.WriteLine("RUNTIME DELETE Object: " + ptr + " " + oo);
            }
            return 0;
        }
        private unsafe delegate int SetRemoveObject(void* pEnv, int hashCode);

        private readonly static object objLock_Java_app_quant_clr_CLRRuntime_nativeCreateInstance = new object();
        private static unsafe int Java_app_quant_clr_CLRRuntime_nativeCreateInstance(void* pEnv, string classname, int len, void** args)
        {
            lock(objLock_Java_app_quant_clr_CLRRuntime_nativeCreateInstance)
            {
                try
                {
                    void*  pNetBridgeClass;
                    if(FindClass( pEnv, "app/quant/clr/CLRRuntime", &pNetBridgeClass) != 0) throw new Exception ("getJavaArray Find class error");

                    object[] classes_obj = len == 0 ? null : getJavaArray(pEnv, pNetBridgeClass, len, args, "[Ljava/lang/Object;");
                    
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

                    
                    if(ct == null)
                        foreach(Assembly assembly in M._compiledAssemblies.Values)
                        {
                            asm = assembly;
                            ct = asm.GetType(classname);
                            if(ct != null)
                                break;
                        }

                    if(ct == null)
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
                    {
                        Console.WriteLine("CLR Java_app_quant_clr_CLRRuntime_nativeCreateInstance obj null: " + classname + " (" + len + ") " + classes_obj);
                        return 0;
                    }

                    int hashCode = GetID(obj, true); // IMPORTANT TRUE
                
                    if(!(obj is JVMObject) && !(obj is IJVMTuple))
                        // DB[hashCode] = obj;
                        DB[hashCode] = new WeakReference(obj);

                    return hashCode;
                }
                catch(Exception e)
                {
                    Console.WriteLine("Java_app_quant_clr_CLRRuntime_nativeCreateInstance(" + classname + "): " + e);
                    return -1;
                }
            }
        }
        private unsafe delegate int SetCreateInstance(void* pEnv, string classname, int len, void** args);
        
        // internal static ConcurrentDictionary<string,MethodInfo> MethodDB = new ConcurrentDictionary<string, MethodInfo>();
        internal static ConcurrentDictionary<int,ConcurrentDictionary<string,MethodInfo>> MethodDB = new ConcurrentDictionary<int,ConcurrentDictionary<string,MethodInfo>>();
        private readonly static object objLock_Java_app_quant_clr_CLRRuntime_nativeInvoke = new object();
        private static unsafe void* Java_app_quant_clr_CLRRuntime_nativeInvoke(void* pEnv, int hashCode, string funcname, int len, void** args)
        {
            lock(objLock_Java_app_quant_clr_CLRRuntime_nativeInvoke)
            {                
                try
                {
                    void*  pNetBridgeClass;
                    if(FindClass( pEnv, "app/quant/clr/CLRRuntime", &pNetBridgeClass) != 0) throw new Exception ("getJavaArray Find class error");

                    object[] classes_obj = len == 0 ? null : getJavaArray(pEnv, pNetBridgeClass, len, args, "[Ljava/lang/Object;");

                    if(DB.ContainsKey(hashCode) && DB[hashCode].IsAlive)
                    {
                        object obj = DB[hashCode].Target;

                        if(obj is Type)
                        {
                            // Console.WriteLine("CLR NATIVE INVOKE Type(" + hashCode + "): " + obj + " " + funcname);
                            var key = hashCode + funcname + len;
                            if(!MethodDB.ContainsKey(hashCode))
                                MethodDB.TryAdd(hashCode, new ConcurrentDictionary<string,MethodInfo>());

                            if(!MethodDB[hashCode].ContainsKey(key))
                                MethodDB[hashCode].TryAdd(key, getSuperMethod(obj as Type, funcname));
                            MethodInfo method = MethodDB[hashCode][key];
                        
                            if(method == null)
                            {
                                Console.WriteLine("-----> method null 1");
                                return IntPtr.Zero.ToPointer();
                            }

                            object res = method.Invoke(null, classes_obj);

                            if(res == null)
                                return IntPtr.Zero.ToPointer();
                        
                            var ret = getObjectPointer(pEnv, res);

                            return ret;

                        }
                        else if(obj is DynamicObject)
                        {
                            // Console.WriteLine("CLR NATIVE INVOKE dynamic(" + hashCode + "): " + obj + " " + funcname + " --> " + classes_obj  + " --> " + (classes_obj == null));
                            var res = classes_obj == null ? Dynamic.InvokeMember(obj,funcname) : Dynamic.InvokeMember(obj,funcname,classes_obj);
                            if(res == null)
                                return IntPtr.Zero.ToPointer();

                            var ret = getObjectPointer(pEnv, (object)res);

                            return ret;
                        }
                        else
                        {
                            // Console.WriteLine("CLR NATIVE INVOKE else(" + hashCode + "): " + obj + " " + funcname);
                            var key = hashCode + funcname + len;

                            if(!MethodDB.ContainsKey(hashCode))
                                MethodDB.TryAdd(hashCode, new ConcurrentDictionary<string,MethodInfo>());
                                
                            if(!MethodDB[hashCode].ContainsKey(key))
                                MethodDB[hashCode].TryAdd(key, getSuperMethod(obj.GetType(), funcname));
                            MethodInfo method = MethodDB[hashCode][key];

                            if(method == null)
                            {
                                Console.WriteLine("-----> method null 2");
                                return IntPtr.Zero.ToPointer();
                            }
                            
                            object res = method.Invoke(obj, classes_obj);

                            if(res == null)
                                return IntPtr.Zero.ToPointer();

                            var ret = getObjectPointer(pEnv, res);
                            return ret;
                        }
                    }
                    throw new Exception("Java_app_quant_clr_CLRRuntime_nativeInvoke: no hashCode in DB: " + hashCode);
                }
                catch(Exception e)
                {
                    Console.WriteLine("Java_app_quant_clr_CLRRuntime_nativeInvoke(" + hashCode + "): " + funcname + " " + e);
                }

                return null;
            }
        }

        private readonly static object objLock_getSuperMethod = new object();
        private static MethodInfo getSuperMethod(Type type, string funcname)
        {
            // // lock(objLock_getSuperMethod)
            {
                if(type == null)
                    return null;
                MethodInfo method = type.GetMethod(funcname);
                if(method == null)
                {
                    Type[] interfaces = type.GetInterfaces();
                    if(interfaces != null)
                        foreach(var t in interfaces)
                        {
                            var m = getSuperMethod(t, funcname);
                            if(m != null)
                                return m;
                        }
                    Type tbase = type.BaseType;
                    return getSuperMethod(tbase, funcname);
                }

                return method;
            }
        }

        private readonly static object objLock_getSuperField = new object();
        private static FieldInfo getSuperField(Type type, string funcname)
        {
            // // lock(objLock_getSuperField)
            {
                if(type == null)
                    return null;
                
                FieldInfo field = type.GetField(funcname);
                if(field == null)
                {
                    Type[] interfaces = type.GetInterfaces();
                    if(interfaces != null)
                        foreach(var t in interfaces)
                        {
                            var m = getSuperField(t, funcname);
                            if(m != null)
                                return m;
                        }
                    Type tbase = type.BaseType;
                    return getSuperField(tbase, funcname);
                }

                return field;
            }
        }

        private readonly static object objLock_getSuperProperty = new object();
        private static PropertyInfo getSuperProperty(Type type, string funcname)
        {
            // // lock(objLock_getSuperProperty)
            {
                if(type == null)
                    return null;
                
                PropertyInfo property = type.GetProperty(funcname);
                if(property == null)
                {
                    Type[] interfaces = type.GetInterfaces();
                    if(interfaces != null)
                        foreach(var t in interfaces)
                        {
                            var m = getSuperProperty(t, funcname);
                            if(m != null)
                                return m;
                        }
                    Type tbase = type.BaseType;
                    return getSuperProperty(tbase, funcname);
                }

                return property;
            }
        }
        private unsafe delegate void* SetInvoke(void* pEnv, int ptr, string funcname, int len, void** args);

        private readonly static object objLock_Java_app_quant_clr_CLRRuntime_nativeRegisterFunc = new object();
        private static unsafe void* Java_app_quant_clr_CLRRuntime_nativeRegisterFunc(void* pEnv, string funcname, int hashCode)
        {
            lock(objLock_Java_app_quant_clr_CLRRuntime_nativeRegisterFunc)
            {
                try
                {
                    JVMDelegate del = new JVMDelegate(funcname, hashCode);
                    
                    // if(!__DB.ContainsKey(hashCode))
                    //     __DB[hashCode] = del; //important
                    // else
                    //     Console.WriteLine("CLR 758 ERROR HASHCODE EXISTS");
                    int hsh = GetID(del, false);
                    // DB[hashCode] = new WeakReference(del);
                    if(DB.ContainsKey(hsh))
                        Console.WriteLine("----- CLR 762 HASH Conflict");
                    // DB[hsh] = new WeakReference(del);
                    return IntPtr.Zero.ToPointer();
                }
                catch(Exception e)
                {
                    Console.WriteLine("Java_app_quant_clr_CLRRuntime_nativeRegisterFunc: " + funcname + " " + e);
                    return IntPtr.Zero.ToPointer();
                }
            }
            
        }
        private unsafe delegate void* SetRegisterFunc(void* pEnv, string funcname, int hashCode);
        
        private readonly static object objLock_InvokeFunc = new object();
        public static unsafe object InvokeFunc(int hashCode, object[] args)
        {
            lock(objLock_InvokeFunc)
            // // lock(objLock_getJavaParameters)
            {
                try
                {
                    // Console.WriteLine("----------INVOKEFUNC");
                    void*  pEnv;
                    if(AttacheThread((void*)JVMPtr,&pEnv) != 0) throw new Exception ("Attach to thread error");

                    bool nullResult = false;

                    void*  pNetBridgeClass;
                    if(FindClass( pEnv, "app/quant/clr/CLRRuntime", &pNetBridgeClass) == 0)
                    {
                        void*  pInvokeMethod;
                        if(GetStaticMethodID( pEnv, pNetBridgeClass, "InvokeDelegate", "(I[Ljava/lang/Object;)Ljava/lang/Object;", &pInvokeMethod ) == 0)
                        {
                            // Console.WriteLine("InvokeFunc(" + hashCode + "): " + args);
                            // if(args != null)
                            //     foreach (var item in args)
                            //     {
                            //         Console.WriteLine("-------(" + GetID(item) + "): " + item);
                            //     }
                            object[] pAr_len_data = args == null ? new object[]{ hashCode } : new object[]{ hashCode, args };
                            void** pAr_len = (void**)(new StructWrapper(pEnv, pAr_len_data)).Ptr;

                            void*  pGetCLRObject;
                            if(CallStaticObjectMethod( pEnv, pNetBridgeClass, pInvokeMethod, &pGetCLRObject, args == null ? 1 : 2, pAr_len) == 0)
                            {
                                if(new IntPtr(pGetCLRObject) != IntPtr.Zero)
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
                                                bool res_bool;
                                                if(CallBooleanMethod( pEnv, pGetCLRObject, pInvokeMethod_boolean, 1, pAr_boolean, &res_bool) != 0)
                                                    throw new Exception(GetException(pEnv));
                                                
                                                DetacheThread((void*)JVMPtr);
                                                return res_bool;

                                            case "java.lang.Byte":
                                                void*  pInvokeMethod_byte;
                                                GetMethodID( pEnv, pGetCLRObject, "byteValue", "()B", &pInvokeMethod_byte );
                                                
                                                
                                                void** pAr_byte = stackalloc void*[1];
                                                byte res_byte;
                                                if(CallByteMethod( pEnv, pGetCLRObject, pInvokeMethod_byte, 1, pAr_byte, &res_byte) != 0)
                                                    throw new Exception(GetException(pEnv));
                                                DetacheThread((void*)JVMPtr);
                                                return res_byte;

                                            case "java.lang.Character":
                                                void*  pInvokeMethod_char;
                                                GetMethodID( pEnv, pGetCLRObject, "charValue", "()C", &pInvokeMethod_char );
                                                
                                                
                                                void** pAr_char = stackalloc void*[1];
                                                char _res;
                                                if(CallCharMethod( pEnv, pGetCLRObject, pInvokeMethod_char, 1, pAr_char, &_res) != 0)
                                                    throw new Exception(GetException(pEnv));
                                                
                                                DetacheThread((void*)JVMPtr);
                                                return _res;

                                            case "java.lang.Short":
                                                void*  pInvokeMethod_short;
                                                GetMethodID( pEnv, pGetCLRObject, "shortValue", "()S", &pInvokeMethod_short );
                                                
                                                
                                                void** pAr_short = stackalloc void*[1];
                                                short res_short;
                                                if(CallShortMethod( pEnv, pGetCLRObject, pInvokeMethod_short, 1, pAr_short, &res_short) != 0)
                                                    throw new Exception(GetException(pEnv));
                                                
                                                DetacheThread((void*)JVMPtr);
                                                return res_short;

                                            case "java.lang.Integer":
                                                void*  pInvokeMethod_int;
                                                GetMethodID( pEnv, pGetCLRObject, "intValue", "()I", &pInvokeMethod_int );
                                                
                                                
                                                void** pAr_int = stackalloc void*[1];
                                                int res_int;
                                                if(CallIntMethod( pEnv, pGetCLRObject, pInvokeMethod_int, 1, pAr_int, &res_int) != 0)
                                                    throw new Exception(GetException(pEnv));

                                                DetacheThread((void*)JVMPtr);
                                                return res_int;

                                            case "java.lang.Long":
                                                void*  pInvokeMethod_long;
                                                GetMethodID( pEnv, pGetCLRObject, "longValue", "()J", &pInvokeMethod_long );
                                                
                                                
                                                void** pAr_long = stackalloc void*[1];
                                                long res_long;
                                                if(CallLongMethod( pEnv, pGetCLRObject, pInvokeMethod_long, 1, pAr_long, &res_long) != 0)
                                                    throw new Exception(GetException(pEnv));

                                                DetacheThread((void*)JVMPtr);
                                                return res_long;


                                            case "java.lang.Float":
                                                void*  pInvokeMethod_float;
                                                GetMethodID( pEnv, pGetCLRObject, "floatValue", "()F", &pInvokeMethod_float );
                                                
                                                
                                                void** pAr_float = stackalloc void*[1];
                                                float res_float;
                                                if(CallFloatMethod( pEnv, pGetCLRObject, pInvokeMethod_float, 1, pAr_float, &res_float) != 0)
                                                    throw new Exception(GetException(pEnv));

                                                DetacheThread((void*)JVMPtr);
                                                return res_float;

                                            case "java.lang.Double":
                                                void*  pInvokeMethod_double;
                                                GetMethodID( pEnv, pGetCLRObject, "doubleValue", "()D", &pInvokeMethod_double );
                                                
                                                
                                                void** pAr_double = stackalloc void*[1];
                                                double res_double;
                                                if(CallDoubleMethod( pEnv, pGetCLRObject, pInvokeMethod_double, 1, pAr_double, &res_double) != 0)
                                                    throw new Exception(GetException(pEnv));

                                                DetacheThread((void*)JVMPtr);
                                                return res_double;


                                            case "java.lang.String":
                                                var _ret_str = GetNetString(pEnv, pGetCLRObject);
                                                DetacheThread((void*)JVMPtr);
                                                return _ret_str;


                                            case "java.time.LocalDateTime":
                                                var _ret_dt = GetNetDateTime(pEnv, pGetCLRObject);
                                                DetacheThread((void*)JVMPtr);
                                                return _ret_dt;

                                            default:
                                                if(clsName.StartsWith("["))
                                                {
                                                    int arr_len = getArrayLength(pEnv, pGetCLRObject);
                                                    var ret = getJavaArray(pEnv, pNetBridgeClass, arr_len, pGetCLRObject, clsName);
                                                    DetacheThread((void*)JVMPtr);
                                                    return ret;
                                                }
                                                else
                                                {
                                                    int hashID_res = GetJVMID(pEnv, pGetCLRObject, true);

                                                    
                                                    if(JVMDelegate.DB.ContainsKey(hashID_res) && JVMDelegate.DB[hashID_res].IsAlive) //check if it is a CLRObject
                                                    {
                                                        DetacheThread((void*)JVMPtr);
                                                        return JVMDelegate.DB[hashID_res].Target;
                                                    }

                                                    else if(Runtime.DB.ContainsKey(hashID_res) && Runtime.DB[hashID_res].IsAlive) //check if it is a JVMObject
                                                    {
                                                        DetacheThread((void*)JVMPtr);
                                                        return Runtime.DB[hashID_res].Target;
                                                    }


                                                    else if(JVMObject.DB.ContainsKey(hashID_res) && JVMObject.DB[hashID_res].IsAlive) //check if it is a JVMObject
                                                    {
                                                        DetacheThread((void*)JVMPtr);
                                                        // return JVMObject.DB[hashID_res];
                                                        return JVMObject.DB[hashID_res].Target;
                                                    }

                                                    else
                                                    {
                                                        string cls = clsName.StartsWith("L") && clsName.EndsWith(";") ? clsName.Substring(1).Replace(";","") : clsName;


                                                        var _ret = CreateInstancePtr(pEnv, cls, null, new IntPtr(pGetCLRObject), null );
                                                        // GetID(_ret, true); //NOT SURE
                                                        DetacheThread((void*)JVMPtr);
                                                        return _ret;
                                                    }
                                                }
                                        }
                                    
                                    }
                                }
                                else
                                {
                                    DetacheThread((void*)JVMPtr);
                                    return null;
                                }
                            }
                            else
                            {
                                Console.WriteLine("---InvokeFunc(" + hashCode + "): " + args);
                                // if(args != null)
                                //     foreach (var item in args)
                                //     {
                                //         Console.WriteLine("-------(" + GetID(item) + "): " + item);
                                //     }
                            }

                        }
                    }
                    return null;
                    // throw new Exception(GetException(pEnv));
                }
                catch(Exception e)
                {
                    Console.WriteLine("CS InvokeFunc: " + e);
                    return null;
                }
            }
        }

        private readonly static object objLock_getObjectPointer = new object();
        private static unsafe void* getObjectPointer(void* pEnv, object res)
        {
            lock(objLock_getObjectPointer)
            {
                try
                {
                    if(res == null)
                    {
                        Console.WriteLine("CLR getObjectPointer 1 not found: " + res);
                        return IntPtr.Zero.ToPointer();
                    }

                    if(res is PyObject)
                    {
                        var pres = res as PyObject;
                        if(PyString.IsStringType(pres))
                        {
                            res = pres.AsManagedObject(typeof(string));
                        }

                        else if(PyFloat.IsFloatType(pres))
                        {
                            res = pres.AsManagedObject(typeof(float));
                        }

                        else if(PyInt.IsIntType(pres))
                        {
                            res = pres.AsManagedObject(typeof(int));
                        }

                        else if(PyDict.IsDictType(pres))
                        {
                            res = pres.AsManagedObject(typeof(Dictionary<object, object>));
                        }

                        else if(PyLong.IsLongType(pres))
                        {
                            res = pres.AsManagedObject(typeof(long));
                        }

                        else if(PyTuple.IsTupleType(pres))
                        {
                            res = pres.AsManagedObject(typeof(System.Tuple));
                        }
                    }


                    Type type = res == null ? null : res.GetType();
        
                    switch(Type.GetTypeCode(type))
                    { 
                        case TypeCode.Boolean:
                            void* res_bool;
                            if(NewBooleanObject(pEnv, (bool)res, &res_bool) == 0)
                                return res_bool;
                            else
                                throw new Exception(GetException(pEnv));
                            
                        case TypeCode.Byte:
                            void* res_byte;
                            if(NewByteObject(pEnv, (byte)res, &res_byte) == 0)
                                return res_byte;
                            else
                                throw new Exception(GetException(pEnv));

                        case TypeCode.Char:
                            void* res_char;
                            if(NewCharacterObject(pEnv, (char)res, &res_char) == 0)
                                return res_char;
                            else
                                throw new Exception(GetException(pEnv));

                        case TypeCode.Int16:
                            void* res_short;
                            if(NewShortObject(pEnv, (short)res, &res_short) == 0)
                                return res_short;
                            else
                                throw new Exception(GetException(pEnv));

                        case TypeCode.Int32: 
                            void* res_int;
                            if(NewIntegerObject(pEnv, (int)res, &res_int) == 0)
                                return res_int;
                            else
                                throw new Exception(GetException(pEnv));

                        case TypeCode.Int64:
                            void* res_long;
                            if(NewLongObject(pEnv, (long)res, &res_long) == 0)
                                return res_long;
                            else
                                throw new Exception(GetException(pEnv));

                        case TypeCode.Single:
                            void* res_float;
                            if(NewFloatObject(pEnv, (float)res, &res_float) == 0)
                                return res_float;
                            else
                                throw new Exception(GetException(pEnv));

                        case TypeCode.Double:
                            void* res_double;
                            if(NewDoubleObject(pEnv, (double)res, &res_double) == 0)
                                return res_double;
                            else
                                throw new Exception(GetException(pEnv));

                        case TypeCode.String:
                            void* string_arg = GetJavaString(pEnv, (string)res);
                            return (void**)string_arg;

                        case TypeCode.DateTime:
                            void* date_arg = GetJavaDateTime(pEnv, (DateTime)res);
                            return (void**)date_arg;
                            

                        default:

                            void*  pNetBridgeClass;
                            if(FindClass( pEnv, "app/quant/clr/CLRRuntime", &pNetBridgeClass) != 0) throw new Exception("Class not found");

                            var id = GetID(res, true); // IMPORTANT TRUE
                            // var id = GetID(res, false);

                            if(res != null)
                                res.RegisterGCEvent(id, delegate(object _obj, int _id)
                                {
                                    // Console.WriteLine("------______++____---Object(" + _obj + ") with hash code " + _id + " recently collected: ");
                                    Runtime.RemoveID(_id);
                                });

                            if(res != null && JVMDelegate.DB.ContainsKey(id))
                            {
                                JVMDelegate jobj = res as JVMDelegate; 
                                // RegisterJVMObject(pEnv, id, (void *)(jobj.Pointer));
                                return (void *)(jobj.Pointer);
                            }

                            else if(res != null && DB.ContainsKey(id))
                            {
                                // Console.WriteLine("---- RETURN CACHE CLRObject: " + id + " " + res);
                                if(true)//FindClass( pEnv, "app/quant/clr/CLRRuntime", &pNetBridgeClass) == 0)
                                {
                                    void*  pGetCLRObjectMethod;
                                    if(GetStaticMethodID( pEnv, pNetBridgeClass, "GetCLRObject", "(I)Lapp/quant/clr/CLRObject;", &pGetCLRObjectMethod ) == 0)
                                    {
                                        object[] pAr_len_data = new object[]{ id };
                                        void** pAr_len = (void**)(new StructWrapper(pEnv, pAr_len_data)).Ptr;

                                        void*  pGetCLRObject;
                                        if(CallStaticObjectMethod( pEnv, pNetBridgeClass, pGetCLRObjectMethod, &pGetCLRObject, 1, pAr_len) == 0)
                                        {
                                            // RegisterJVMObject(pEnv, id, pGetCLRObject);
                                            return pGetCLRObject;
                                        }
                                        else
                                            throw new Exception(GetException(pEnv));
                                    }
                                    else
                                        throw new Exception(GetException(pEnv));
                                }
                                else
                                    throw new Exception(GetException(pEnv));
                            }

                            else if(res is IJVMTuple)
                            {
                                IJVMTuple jobj = res as IJVMTuple; 
                                void* ptr = GetJVMObject(pEnv, pNetBridgeClass, jobj.JVMObject.JavaHashCode);
                                // RegisterJVMObject(pEnv, id, ptr);
                                return ptr;
                            }
                            else if(res is JVMTuple)
                            {
                                JVMTuple jobj = res as JVMTuple; 
                                void* ptr = GetJVMObject(pEnv, pNetBridgeClass, jobj.JavaHashCode);
                                // RegisterJVMObject(pEnv, id, ptr);
                                return ptr;
                            }
                            
                            else if(res is JVMObject)
                            {
                                

                                JVMObject jobj = res as JVMObject; 

                                // Console.WriteLine("---- RETURN JVMObject: " + id + " " + jobj.JavaHashCode + " " + res);
                                void* ptr = GetJVMObject(pEnv, pNetBridgeClass, jobj.JavaHashCode);
                                // RegisterJVMObject(pEnv, id, ptr);
                                return ptr;
                            }

                            else if(res is Array)
                            {
                                Array sub = res as Array;
                                
                                JVMObject javaArray = getJavaArray(pEnv, pNetBridgeClass, sub);
                                // Console.WriteLine("---- RETURN JVMObject: " + sub + " " + javaArray.JavaHashCode + " " + res);
                                
                                void* ptr = GetJVMObject(pEnv, pNetBridgeClass, javaArray.JavaHashCode);
                                return ptr;
                            }

                            else if(res is IEnumerable<object> || (res is PyObject && PyList.IsListType((PyObject)res)))
                            {
                                if(res is PyObject && PyList.IsListType((PyObject)res))
                                    res = new PyList((PyObject)res);
                                
                                object[] pAr_len_data = new object[]{ res.GetType().ToString(), id };
                                void** pAr_len = (void**)(new StructWrapper(pEnv, pAr_len_data)).Ptr;

                                void* pObj;
                                void* CLRObjClass;
                                void*  pLoadClassMethod; // The executed method struct
                                if(FindClass( pEnv, "app/quant/clr/CLRIterable", &CLRObjClass) == 0)
                                {
                                    void* pClass;
                                    if(NewObjectP( pEnv, CLRObjClass, "(Ljava/lang/String;I)V", 2, pAr_len, &pObj ) == 0)
                                    {
                                        if(DB.ContainsKey(id))
                                            Console.WriteLine("CLR 1238 Hash Conflict");

                                        if(!(res is JVMObject) && !(res is IJVMTuple))
                                            // DB[id] = res;
                                            DB[id] = new WeakReference(res);

                                        // RegisterJVMObject(pEnv, id, pObj);
                                        return pObj;
                                    }
                                    else
                                        throw new Exception(GetException(pEnv));
                                }
                                else
                                    throw new Exception(GetException(pEnv));
                            }

                            else if(res is IEnumerator<object>)
                            {
                                void* ptr_res = (void *)(res.GetHashCode());

                                object[] pAr_len_data = new object[]{ res.GetType().ToString(), id };
                                void** pAr_len = (void**)(new StructWrapper(pEnv, pAr_len_data)).Ptr;

                                void* pObj;
                                void* CLRObjClass;
                                void*  pLoadClassMethod; // The executed method struct
                                if(FindClass( pEnv, "app/quant/clr/CLRIterator", &CLRObjClass) == 0)
                                {
                                    void* pClass;
                                    if(NewObjectP( pEnv, CLRObjClass, "(Ljava/lang/String;I)V", 2, pAr_len, &pObj ) == 0)
                                    {
                                        if(DB.ContainsKey(id))
                                            Console.WriteLine("CLR 1268 Hash Conflict");

                                        if(!(res is JVMObject) && !(res is IJVMTuple))
                                            // DB[id] = res;
                                            DB[id] = new WeakReference(res);

                                        // RegisterJVMObject(pEnv, id, pObj);
                                        return pObj;
                                    }
                                    else
                                        throw new Exception(GetException(pEnv));
                                }
                                else
                                    throw new Exception(GetException(pEnv));
                            }

                            else
                            {
                                object[] pAr_len_data = new object[]{ res.GetType().ToString(), id, false };
                                
                                void** pAr_len = (void**)(new StructWrapper(pEnv, pAr_len_data)).Ptr;

                                void* pObj;
                                void* CLRObjClass;
                                void*  pLoadClassMethod; // The executed method struct
                                if(FindClass( pEnv, "app/quant/clr/CLRObject", &CLRObjClass) == 0)
                                {
                                    void* pClass;
                                    if(NewObjectP( pEnv, CLRObjClass, "(Ljava/lang/String;I)V", 2, pAr_len, &pObj ) == 0)
                                    {
                                        if(DB.ContainsKey(id))
                                            Console.WriteLine("CLR 1299 Hash Conflict");

                                        if(!(res is JVMObject) && !(res is IJVMTuple))
                                        {
                                            // DB[id] = res;
                                            DB[id] = new WeakReference(res);
                                        }
                                        // RegisterJVMObject(pEnv, id, pObj);
                                        return pObj;
                                    }
                                    else
                                        throw new Exception(GetException(pEnv));
                                }
                                else
                                    throw new Exception(GetException(pEnv));
                            }
                                

                            break;
                    }

                    Console.WriteLine("CLR getObjectPointer 2 not found: " + res);
                    return IntPtr.Zero.ToPointer();
                }
                catch(Exception e)
                {
                    Console.WriteLine("CLR getObjectPointer error: " + e);
                    return IntPtr.Zero.ToPointer();
                }
            }
        }

        private readonly static object objLock_Java_app_quant_clr_CLRRuntime_nativeInvokeFunc = new object();
        // private static unsafe void* Java_app_quant_clr_CLRRuntime_nativeInvokeFunc(void* pEnv, int hashCode, int len, void** args)
        private static unsafe void* Java_app_quant_clr_CLRRuntime_nativeInvokeFunc(void* pEnv, int hashCode, int len, void** args)
        {
            lock(objLock_Java_app_quant_clr_CLRRuntime_nativeInvokeFunc)
            // lock(objLock_getJavaParameters)
            {
                try
                {
                    void*  pNetBridgeClass;
                    if(FindClass( pEnv, "app/quant/clr/CLRRuntime", &pNetBridgeClass) == 0)
                    {
                        void*  pArrayClassesMethod;
                        if(GetStaticMethodID( pEnv, pNetBridgeClass, "ArrayClasses", "([Ljava/lang/Object;)[Ljava/lang/String;", &pArrayClassesMethod ) == 0)
                        {
                            object[] classes_obj = getJavaArray(pEnv, pNetBridgeClass, len, args, "[Ljava/lang/Object;");

                            if(JVMDelegate.DB.ContainsKey(hashCode) && JVMDelegate.DB[hashCode].IsAlive)
                            {
                                object res = ((JVMDelegate)JVMDelegate.DB[hashCode].Target).Invoke(classes_obj);
                                var ret =  getObjectPointer(pEnv, res);
                                // RegisterJVMObject(pEnv, hashCode, ret); //Wrong HashCode
                                return ret;
                            }
                            throw new Exception("JVMDelegate not found");
                        }
                    }
                    
                    throw new Exception(GetException(pEnv));
                }
                catch(Exception e)
                {
                    Console.WriteLine("Java_app_quant_clr_CLRRuntime_nativeInvokeFunc: " + hashCode + " " + e);
                    // Console.WriteLine("==" + DB[hashCode]);
                    
                    // if(classes_obj != null)
                    // {
                    //     Console.WriteLine(len + " " + classes_obj.Length);
                    //     foreach (var item in classes_obj)
                    //     {
                    //         Console.WriteLine("---- " + item);
                    //     }
                    // }
                    // throw e;
                }
                return null;
            }
        }
        private unsafe delegate void* SetInvokeFunc(void* pEnv, int hashCode, int len, void** args);
        
        private readonly static object objLock_Java_app_quant_clr_CLRRuntime_nativeSetProperty = new object();
        private static unsafe void Java_app_quant_clr_CLRRuntime_nativeSetProperty(void* pEnv, int hashCode, string name, void** args)
        {
            lock(objLock_Java_app_quant_clr_CLRRuntime_nativeSetProperty)
            {
                try
                {
                    void*  pNetBridgeClass;
                    if(FindClass( pEnv, "app/quant/clr/CLRRuntime", &pNetBridgeClass) == 0)
                    {
                        void*  pArrayClassesMethod;
                        if(GetStaticMethodID( pEnv, pNetBridgeClass, "ArrayClasses", "([Ljava/lang/Object;)[Ljava/lang/String;", &pArrayClassesMethod ) == 0)
                        {
                            // int hashID = GetID(pEnv, args);
                            // int _arr_len = getArrayLength(pEnv, new JVMObject(hashID, "[Ljava/lang/Object;", true));
                            
                            if(DB.ContainsKey(hashCode))
                            {
                                object obj = DB[hashCode].Target;
                                if(obj == null)
                                {
                                    return;
                                }

                                
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
                                    Console.WriteLine("Java_app_quant_clr_CLRRuntime_nativeSetProperty");
                                    int _arr_len = getArrayLength(pEnv, args); //TESTING
                                    object[] classes_obj = getJavaArray(pEnv, pNetBridgeClass, _arr_len, args, "[Ljava/lang/Object;");

                                    FieldInfo field = getSuperField(obj.GetType(), name);
                                    if(field != null)
                                        field.SetValue(obj, classes_obj[0]);
                                    else
                                    {
                                        PropertyInfo property = getSuperProperty(obj.GetType(), name);
                                        if(property != null)
                                            property.SetValue(obj, classes_obj[0]);
                                    }
                                }
                            }
                        }
                        else
                            throw new Exception("Get static ArrayClasses method");
                    }
                    else
                        throw new Exception("Find CLRRuntime class error");
                }
                catch(Exception e)
                {
                    Console.WriteLine("CLR Java_app_quant_clr_CLRRuntime_nativeSetProperty: " + e);
                }
            }
        }
        private unsafe delegate void SetSetProperty(void* pEnv, int hashCode, string name, void** pObj);
        
        private readonly static object objLock_Java_app_quant_clr_CLRRuntime_nativeGetProperty = new object();
        private static unsafe void* Java_app_quant_clr_CLRRuntime_nativeGetProperty(void* pEnv, int hashCode, string name)
        {
            lock(objLock_Java_app_quant_clr_CLRRuntime_nativeGetProperty)
            {
                try
                {
                    if(DB.ContainsKey(hashCode))
                    {
                        object obj = DB[hashCode].Target;
                        if(obj == null)
                            return null;

                        // Console.WriteLine("Java_app_quant_clr_CLRRuntime_nativeGetProperty: " + obj);
                                    

                        
                        if(obj is DynamicObject)
                        {
                            try
                            {

                                var res = Dynamic.InvokeGet(obj, name);
                                
                                if(res == null)
                                {
                                    return IntPtr.Zero.ToPointer();
                                }

                                // Console.WriteLine("Java_app_quant_clr_CLRRuntime_nativeGetProperty 1: " + res);

                                var ret = getObjectPointer(pEnv, (object)res);
                                return ret;
                            }
                            catch
                            {
                                return IntPtr.Zero.ToPointer();
                            }
                        }
                        else if(obj is ExpandoObject)
                        {
                            try
                            {
                                var exp = obj as IDictionary<string, object>;
                                // Console.WriteLine("Java_app_quant_clr_CLRRuntime_nativeGetProperty 2: " + exp[name]);
                                var ret = getObjectPointer(pEnv, exp[name]);
                                return ret;
                            }
                            catch
                            {
                                return IntPtr.Zero.ToPointer();
                            }
                        }
                        else
                        {
                            try
                            {
                                FieldInfo field = getSuperField(obj.GetType(), name);
                                
                                
                                object res;
                                
                                if(field != null)
                                    res = field.GetValue(obj);
                                else
                                {
                                    PropertyInfo property = getSuperProperty(obj.GetType(),name);
                                    if(property != null)
                                        res = property.GetValue(obj);
                                    else
                                    {
                                        return null;
                                    }
                                }

                                // Console.WriteLine("Java_app_quant_clr_CLRRuntime_nativeGetProperty 3: " + res);
                                var ret = getObjectPointer(pEnv, res);


                                return ret;
                            }
                            catch
                            {
                                return IntPtr.Zero.ToPointer();
                            }
                        }
                    }

                    Console.WriteLine("Java_app_quant_clr_CLRRuntime_nativeGetProperty ERROR NOT FOUND(" + hashCode + "): " + name);
                    return null;
                }
                catch(Exception e)
                {
                    Console.WriteLine("CLR Java_app_quant_clr_CLRRuntime_nativeGetProperty: " + e);
                    return (void*)IntPtr.Zero;
                }
            }
        }
        private unsafe delegate void* SetGetProperty(void* pEnv, int hashCode, string name);

        public delegate T wrapFunction<T>(params object[] args);
        public delegate void wrapAction(params object[] args);

        public delegate T wrapGetProperty<T>();
        public delegate void wrapSetProperty(object args);

        private readonly static object objLock_GetNetDateTime = new object();
        public static unsafe DateTime GetNetDateTime(void* pEnv, void* pDate)
        {
            lock(objLock_GetNetDateTime)
            {
                try
                {
                    if(pDate != IntPtr.Zero.ToPointer())
                    {
                        object[] pAr_len_data = new object[]{  };
                        void** pAr_len = (void**)(new StructWrapper(pEnv, pAr_len_data)).Ptr;

                        void*  pDateClass;
                        if(FindClass( pEnv, "java/time/LocalDateTime", &pDateClass) == 0)
                        {
                            void*  pInvokeMethod;
                            if(GetMethodID( pEnv, pDate, "toString", "()Ljava/lang/String;", &pInvokeMethod ) == 0)
                            {
                                void*  pDateStr;
                                if(CallObjectMethod( pEnv, pDate, pInvokeMethod, &pDateStr, 0, pAr_len) == 0)
                                {
                                    if(new IntPtr(pDateStr) != IntPtr.Zero)
                                    {
                                        string str = GetNetString(pEnv, pDateStr);
                                        return DateTime.Parse(str);
                                    }
                                }
                                else
                                    throw new Exception(GetException(pEnv));
                            }
                            else
                                throw new Exception(GetException(pEnv));
                        }
                        else
                            throw new Exception(GetException(pEnv));
                    }
                
                    return DateTime.MinValue;
                }
                catch(Exception e)
                {
                    Console.WriteLine("CLR GetNetDateTime: " + e);
                    return DateTime.MinValue;
                }
            }
        }

        private readonly static object objLock_GetJavaDateTime = new object();
        public static unsafe void* GetJavaDateTime(void* pEnv, DateTime date)
        {
            lock(objLock_GetJavaDateTime)
            {
                try
                {
                    // void** pAr_len = stackalloc void*[7];
                    object[] pAr_len_data = new object[]{ date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second, date.Millisecond * 1000000 };
                    // getJavaParameters(pEnv, ref pAr_len, pAr_len_data);
                    void** pAr_len = (void**)(new StructWrapper(pEnv, pAr_len_data)).Ptr;

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
                                throw new Exception(GetException(pEnv));
                        }
                        else
                            throw new Exception(GetException(pEnv));
                    }
                    else
                        throw new Exception(GetException(pEnv));

                    return IntPtr.Zero.ToPointer();
                }
                catch(Exception e)
                {
                    Console.WriteLine("CLR GetJavaDateTime: " + e);
                    return IntPtr.Zero.ToPointer();
                }
            }
        }

        private readonly static object objLock_getJavaArray_1 = new object();
        private unsafe static JVMObject getJavaArray(void* pEnv, void* pNetBridgeClass, object[] array)
        {
            // cache = true;
            lock(objLock_getJavaArray_1)
            {
                try
                {
                    if(true)
                    {
                        if(array == null)
                        {
                            Console.WriteLine("CLR getJavaArray JVMObject null");
                            return null;
                        }
                        Array sub = array as Array;
                        // GetID(sub, false);

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
                                    throw new Exception(GetException(pEnv));
                                break;

                            case "B":
                                if(NewByteArray( pEnv, arrLength, &pJArray ) != 0)
                                    throw new Exception(GetException(pEnv));
                                break;

                            case "C":
                                if(NewCharArray( pEnv, arrLength, &pJArray ) != 0)
                                    throw new Exception(GetException(pEnv));
                                break;

                            case "S":
                                if(NewShortArray( pEnv, arrLength, &pJArray ) != 0)
                                    throw new Exception(GetException(pEnv));
                                break;

                            case "I":
                                if(NewIntArray( pEnv, arrLength, &pJArray ) != 0)
                                    throw new Exception(GetException(pEnv));
                                break;

                            case "J":
                                if(NewLongArray( pEnv, arrLength, &pJArray ) != 0)
                                    throw new Exception(GetException(pEnv));
                                break;

                            case "F":
                                if(NewFloatArray( pEnv, arrLength, &pJArray ) != 0)
                                    throw new Exception(GetException(pEnv));
                                break;

                            case "D":
                                if(NewDoubleArray( pEnv, arrLength, &pJArray ) != 0)
                                    throw new Exception(GetException(pEnv));
                                break;

                            default:
                                isObject = true;

                                if(arrLength == 0)
                                    return null;

                                if(!cls.Contains("java/lang/String"))
                                    cls = "java/lang/Object";


                                if(NewObjectArray( pEnv, arrLength, cls, &pJArray ) != 0)
                                    throw new Exception(GetException(pEnv));
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
                                // var subID = GetID(sub_element, true);
                                // var subID = GetID(sub_element, false); //TESTING
                                // var subID = GetID(sub_element, cache);

                                // sub_element.RegisterGCEvent(subID, delegate(object _obj, int _id)
                                // {
                                //     // Console.WriteLine("++++++++Object(" + _obj + ") with hash code " + _id + " recently collected: " + cache);
                                //     RemoveID(_id);
                                // });
                                
                                var sub_type = sub_element.GetType();
                                switch(Type.GetTypeCode(sub_type))
                                { 
                                    case TypeCode.Boolean:
                                        if(!isObject)
                                            SetBooleanArrayElement(pEnv, pJArray, ii, (bool)sub_element);
                                        else
                                        {
                                            void* pObjBool;
                                            if(NewBooleanObject(pEnv, (bool)sub_element, &pObjBool) != 0)
                                                throw new Exception(GetException(pEnv));
                                            SetObjectArrayElement(pEnv, pJArray, ii, pObjBool);
                                        }

                                        break;

                                    case TypeCode.Byte:
                                        if(!isObject)
                                            SetByteArrayElement(pEnv, pJArray, ii, (byte)sub_element);
                                        else
                                        {
                                            void* pObjB;
                                            if(NewByteObject(pEnv, (byte)sub_element, &pObjB) != 0)
                                                throw new Exception(GetException(pEnv));
                                            SetObjectArrayElement(pEnv, pJArray, ii, pObjB);
                                        }
                                        break;

                                    case TypeCode.Char:
                                        if(!isObject)
                                            SetCharArrayElement(pEnv, pJArray, ii, (char)sub_element);
                                        else
                                        {
                                            void* pObjC;
                                            if(NewCharacterObject(pEnv, (char)sub_element, &pObjC) != 0)
                                                throw new Exception(GetException(pEnv));
                                            SetObjectArrayElement(pEnv, pJArray, ii, pObjC);
                                        }
                                        break;

                                    case TypeCode.Int16:
                                        if(!isObject)
                                            SetShortArrayElement(pEnv, pJArray, ii, (short)sub_element);
                                        else
                                        {
                                            void* pObjS;
                                            if(NewShortObject(pEnv, (short)sub_element, &pObjS) != 0)
                                                throw new Exception(GetException(pEnv));
                                            SetObjectArrayElement(pEnv, pJArray, ii, pObjS);
                                        }
                                        break;

                                    case TypeCode.Int32: 
                                        if(!isObject)
                                            SetIntArrayElement(pEnv, pJArray, ii, (int)sub_element);
                                        else
                                        {
                                            void* pObjI;
                                            if(NewIntegerObject(pEnv, (int)sub_element, &pObjI) != 0)
                                                throw new Exception(GetException(pEnv));
                                            SetObjectArrayElement(pEnv, pJArray, ii, pObjI);
                                        }
                                        break;
                                        
                                    case TypeCode.Int64:
                                        if(!isObject)
                                            SetLongArrayElement(pEnv, pJArray, ii, (long)sub_element);
                                        else
                                        {
                                            void* pObjL;
                                            if(NewLongObject(pEnv, (long)sub_element, &pObjL) != 0)
                                                throw new Exception(GetException(pEnv));
                                            SetObjectArrayElement(pEnv, pJArray, ii, pObjL);
                                        }
                                        break;

                                    case TypeCode.Single:
                                        if(!isObject)
                                            SetFloatArrayElement(pEnv, pJArray, ii, (float)sub_element);
                                        else
                                        {
                                            void* pObjF;
                                            if(NewFloatObject(pEnv, (float)sub_element, &pObjF) != 0)
                                                throw new Exception(GetException(pEnv));
                                            SetObjectArrayElement(pEnv, pJArray, ii, pObjF);
                                        }
                                        break;

                                    case TypeCode.Double:
                                        if(!isObject)
                                            SetDoubleArrayElement(pEnv, pJArray, ii, (double)sub_element);
                                        else
                                        {
                                            void* pObjD;
                                            if(NewDoubleObject(pEnv, (double)sub_element, &pObjD) != 0)
                                                throw new Exception(GetException(pEnv));
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

                                        // var subID = GetID(sub_element, true);
                                        Console.WriteLine("CLR Runtime cs 1972 CALLING CHECK");
                                        var subID = GetID(sub_element, false); //not getting called

                                        if(JVMDelegate.DB.ContainsKey(subID))
                                        {
                                            JVMDelegate jobj = sub_element as JVMDelegate; 
                                            void* ptr = (void *)(jobj.Pointer);
                                            SetObjectArrayElement(pEnv, pJArray, ii, ptr);
                                        }

                                        else if(DB.ContainsKey(subID))
                                        {
                                            void*  pGetCLRObjectMethod;
                                            if(GetStaticMethodID( pEnv, pNetBridgeClass, "GetCLRObject", "(I)Lapp/quant/clr/CLRObject;", &pGetCLRObjectMethod ) == 0)
                                            {
                                                object[] pAr_len_data = new object[]{ subID };
                                                void** pAr_len = (void**)(new StructWrapper(pEnv, pAr_len_data)).Ptr;

                                                void*  pGetCLRObject;
                                                if(CallStaticObjectMethod( pEnv, pNetBridgeClass, pGetCLRObjectMethod, &pGetCLRObject, 1, pAr_len) != 0)
                                                    throw new Exception(GetException(pEnv));

                                                SetObjectArrayElement(pEnv, pJArray, ii, pGetCLRObject);
                                                // RemoveID(subID);
                                            }
                                            else
                                            {
                                                Console.WriteLine("CLR getJavaArray - GetCLRObject error");
                                                throw new Exception(GetException(pEnv));
                                            }
                                        }

                                        else if(sub_element is JVMTuple)
                                        {
                                            JVMTuple jobj = sub_element as JVMTuple; 
                                            // void* ptr = (void *)(jobj.jVMObject.Pointer);
                                            void* ptr = GetJVMObject(pEnv, pNetBridgeClass, jobj.jVMObject.JavaHashCode);
                                            SetObjectArrayElement(pEnv, pJArray, ii, ptr);
                                            // RemoveID(jobj.jVMObject.JavaHashCode);
                                        }
                                        else if(sub_element is IJVMTuple)
                                        {
                                            IJVMTuple jobj = sub_element as IJVMTuple; 
                                            // void* ptr = (void *)(jobj.JVMObject.Pointer);
                                            void* ptr = GetJVMObject(pEnv, pNetBridgeClass, jobj.JVMObject.JavaHashCode);
                                            SetObjectArrayElement(pEnv, pJArray, ii, ptr);
                                            // RemoveID(jobj.JVMObject.JavaHashCode);
                                        }

                                        else if(sub_element is JVMObject)
                                        {
                                            JVMObject jobj = sub_element as JVMObject; 
                                            void* ptr = GetJVMObject(pEnv, pNetBridgeClass, jobj.JavaHashCode);
                                            SetObjectArrayElement(pEnv, pJArray, ii, ptr);
                                            // RemoveID(jobj.JavaHashCode);
                                        }

                                        else if(sub_element is Array)
                                        {                                    
                                            Array sub_array = sub_element as Array;
                                            JVMObject javaArray = getJavaArray(pEnv, pNetBridgeClass, sub_array);

                                            void* ptr = GetJVMObject(pEnv, pNetBridgeClass, javaArray.JavaHashCode);
                                            SetObjectArrayElement(pEnv, pJArray, ii, ptr);
                                            // RemoveID(javaArray.JavaHashCode);
                                        }

                                        else if(sub_element is IEnumerable<object>)
                                        {
                                            object[] pAr_len_data = new object[]{ sub_element.GetType().ToString(), subID };
                                            void** pAr_len = (void**)(new StructWrapper(pEnv, pAr_len_data)).Ptr;

                                            void* pObj;
                                            void* CLRObjClass;
                                            void*  pLoadClassMethod; // The executed method struct
                                            if(FindClass( pEnv, "app/quant/clr/CLRIterable", &CLRObjClass) == 0)
                                            {
                                                void* pClass;
                                                if(NewObjectP( pEnv, CLRObjClass, "(Ljava/lang/String;I)V", 2, pAr_len, &pObj ) == 0)
                                                {
                                                    if(DB.ContainsKey(subID))
                                                        Console.WriteLine("CLR 1983 Hash Conflict");

                                                    // int _id = GetID(pEnv, pObj);
                                                    // RegisterJVMObject(pEnv, GetID(pEnv, pObj) ,pObj);
                                                    if(!(res is JVMObject) && !(res is IJVMTuple))
                                                        // DB[subID] = res;
                                                        DB[subID] = new WeakReference(res);

                                                    
                                                    SetObjectArrayElement(pEnv, pJArray, ii, pObj);
                                                    // Console.WriteLine("RUNTIME CLRITERABLE:  " + subID);
                                                    // RemoveID(subID);
                                                }
                                                else
                                                    throw new Exception(GetException(pEnv));
                                            }
                                            else
                                                throw new Exception(GetException(pEnv));
                                        }

                                        else if(res is IEnumerator<object>)
                                        {
                                            object[] pAr_len_data = new object[]{ res.GetType().ToString(), subID };
                                            void** pAr_len = (void**)(new StructWrapper(pEnv, pAr_len_data)).Ptr;

                                            void* pObj;
                                            void* CLRObjClass;
                                            void*  pLoadClassMethod; // The executed method struct
                                            if(FindClass( pEnv, "app/quant/clr/CLRIterator", &CLRObjClass) == 0)
                                            {
                                                void* pClass;
                                                if(NewObjectP( pEnv, CLRObjClass, "(Ljava/lang/String;I)V", 2, pAr_len, &pObj ) == 0)
                                                {
                                                    if(DB.ContainsKey(subID))
                                                        Console.WriteLine("CLR 2015 Hash Conflict");

                                                    // int _id = GetID(pEnv, pObj);
                                                    // RegisterJVMObject(pEnv, GetID(pEnv, pObj) ,pObj);
                                                    if(!(res is JVMObject) && !(res is IJVMTuple))
                                                        // DB[subID] = res;
                                                        DB[subID] = new WeakReference(res);
                                                
                                                    
                                                    SetObjectArrayElement(pEnv, pJArray, ii, pObj);
                                                    // Console.WriteLine("RUNTIME CLRIterator:  " + subID);
                                                    // RemoveID(subID);
                                                }
                                                else
                                                    throw new Exception(GetException(pEnv));
                                            }
                                            else
                                                throw new Exception(GetException(pEnv));
                                        }

                                        else if(res is System.Func<Object, Object>)
                                        {
                                            object[] pAr_len_data = new object[]{ res.GetType().ToString(), subID };
                                            void** pAr_len = (void**)(new StructWrapper(pEnv, pAr_len_data)).Ptr;

                                            void* pObj;
                                            void* CLRObjClass;
                                            void*  pLoadClassMethod; // The executed method struct
                                            if(FindClass( pEnv, "app/quant/clr/function/CLRFunction1", &CLRObjClass) == 0)
                                            {
                                                void* pClass;
                                                if(NewObjectP( pEnv, CLRObjClass, "(Ljava/lang/String;I)V", 2, pAr_len, &pObj ) == 0)
                                                {
                                                    if(DB.ContainsKey(subID))
                                                        Console.WriteLine("CLR 2047 Hash Conflict");

                                                    // int _id = GetID(pEnv, pObj);
                                                    // RegisterJVMObject(pEnv, GetID(pEnv, pObj) ,pObj);
                                                    if(!(res is JVMObject) && !(res is IJVMTuple))
                                                        // DB[subID] = res;
                                                        DB[subID] = new WeakReference(res);

                                                    
                                                    SetObjectArrayElement(pEnv, pJArray, ii, pObj);
                                                    // RemoveID(subID);
                                                }
                                                else
                                                    throw new Exception(GetException(pEnv));
                                            }
                                            else
                                                throw new Exception(GetException(pEnv));
                                        }
                                        else if(res is System.Func<Object, Object, Object>)
                                        {
                                            object[] pAr_len_data = new object[]{ res.GetType().ToString(), subID };
                                            void** pAr_len = (void**)(new StructWrapper(pEnv, pAr_len_data)).Ptr;

                                            void* pObj;
                                            void* CLRObjClass;
                                            void*  pLoadClassMethod; // The executed method struct
                                            if(FindClass( pEnv, "app/quant/clr/function/CLRFunction2", &CLRObjClass) == 0)
                                            {
                                                void* pClass;
                                                if(NewObjectP( pEnv, CLRObjClass, "(Ljava/lang/String;I)V", 2, pAr_len, &pObj ) == 0)
                                                {
                                                    if(DB.ContainsKey(subID))
                                                        Console.WriteLine("CLR 2077 Hash Conflict");

                                                    // int _id = GetID(pEnv, pObj);
                                                    // RegisterJVMObject(pEnv, GetID(pEnv, pObj) ,pObj);
                                                    if(!(res is JVMObject) && !(res is IJVMTuple))
                                                        // DB[subID] = res;
                                                        DB[subID] = new WeakReference(res);

                                                    
                                                    SetObjectArrayElement(pEnv, pJArray, ii, pObj);
                                                    // RemoveID(subID);
                                                }
                                                else
                                                    throw new Exception(GetException(pEnv));
                                            }
                                            else
                                                throw new Exception(GetException(pEnv));
                                        }
                                        else if(res is System.Func<Object, Object, Object, Object>)
                                        {
                                            object[] pAr_len_data = new object[]{ res.GetType().ToString(), subID };
                                            void** pAr_len = (void**)(new StructWrapper(pEnv, pAr_len_data)).Ptr;

                                            void* pObj;
                                            void* CLRObjClass;
                                            void*  pLoadClassMethod; // The executed method struct
                                            if(FindClass( pEnv, "app/quant/clr/function/CLRFunction3", &CLRObjClass) == 0)
                                            {
                                                void* pClass;
                                                if(NewObjectP( pEnv, CLRObjClass, "(Ljava/lang/String;I)V", 2, pAr_len, &pObj ) == 0)
                                                {
                                                    if(DB.ContainsKey(subID))
                                                        Console.WriteLine("CLR 2114 Hash Conflict");

                                                    // int _id = GetID(pEnv, pObj);
                                                    // RegisterJVMObject(pEnv, GetID(pEnv, pObj) ,pObj);
                                                    if(!(res is JVMObject) && !(res is IJVMTuple))
                                                        // DB[subID] = res;
                                                        DB[subID] = new WeakReference(res);

                                                    
                                                    SetObjectArrayElement(pEnv, pJArray, ii, pObj);
                                                    // RemoveID(subID);
                                                }
                                                else
                                                    throw new Exception(GetException(pEnv));
                                            }
                                            else
                                                throw new Exception(GetException(pEnv));
                                        }
                                        else if(res is System.Func<Object, Object, Object, Object, Object>)
                                        {
                                            object[] pAr_len_data = new object[]{ res.GetType().ToString(), subID };
                                            void** pAr_len = (void**)(new StructWrapper(pEnv, pAr_len_data)).Ptr;

                                            void* pObj;
                                            void* CLRObjClass;
                                            void*  pLoadClassMethod; // The executed method struct
                                            if(FindClass( pEnv, "app/quant/clr/function/CLRFunction4", &CLRObjClass) == 0)
                                            {
                                                void* pClass;
                                                if(NewObjectP( pEnv, CLRObjClass, "(Ljava/lang/String;I)V", 2, pAr_len, &pObj ) == 0)
                                                {
                                                    if(DB.ContainsKey(subID))
                                                        Console.WriteLine("CLR 2146 Hash Conflict");

                                                    // int _id = GetID(pEnv, pObj);
                                                    // RegisterJVMObject(pEnv, GetID(pEnv, pObj) ,pObj);
                                                    if(!(res is JVMObject) && !(res is IJVMTuple))
                                                        // DB[subID] = res;
                                                        DB[subID] = new WeakReference(res);
                                                    SetObjectArrayElement(pEnv, pJArray, ii, pObj);
                                                    // RemoveID(subID);
                                                }
                                                else
                                                    throw new Exception(GetException(pEnv));
                                            }
                                            else
                                                throw new Exception(GetException(pEnv));
                                        }
                                        else if(res is System.Func<Object, Object, Object, Object, Object, Object>)
                                        {
                                            object[] pAr_len_data = new object[]{ res.GetType().ToString(), subID };
                                            void** pAr_len = (void**)(new StructWrapper(pEnv, pAr_len_data)).Ptr;

                                            void* pObj;
                                            void* CLRObjClass;
                                            void*  pLoadClassMethod; // The executed method struct
                                            if(FindClass( pEnv, "app/quant/clr/function/CLRFunction5", &CLRObjClass) == 0)
                                            {
                                                void* pClass;
                                                if(NewObjectP( pEnv, CLRObjClass, "(Ljava/lang/String;I)V", 2, pAr_len, &pObj ) == 0)
                                                {
                                                    if(DB.ContainsKey(subID))
                                                        Console.WriteLine("CLR 2173 Hash Conflict");

                                                    // int _id = GetID(pEnv, pObj);
                                                    // RegisterJVMObject(pEnv, GetID(pEnv, pObj) ,pObj);
                                                    if(!(res is JVMObject) && !(res is IJVMTuple))
                                                        // DB[subID] = res;
                                                        DB[subID] = new WeakReference(res);
                                                    SetObjectArrayElement(pEnv, pJArray, ii, pObj);
                                                    // RemoveID(subID);
                                                }
                                                else
                                                    throw new Exception(GetException(pEnv));
                                            }
                                            else
                                                throw new Exception(GetException(pEnv));
                                        }
                                        else if(res is System.Func<Object, Object, Object, Object, Object, Object, Object>)
                                        {
                                            object[] pAr_len_data = new object[]{ res.GetType().ToString(), subID };
                                            void** pAr_len = (void**)(new StructWrapper(pEnv, pAr_len_data)).Ptr;

                                            void* pObj;
                                            void* CLRObjClass;
                                            void*  pLoadClassMethod; // The executed method struct
                                            if(FindClass( pEnv, "app/quant/clr/function/CLRFunction6", &CLRObjClass) == 0)
                                            {
                                                void* pClass;
                                                if(NewObjectP( pEnv, CLRObjClass, "(Ljava/lang/String;I)V", 2, pAr_len, &pObj ) == 0)
                                                {
                                                    if(DB.ContainsKey(subID))
                                                        Console.WriteLine("CLR 2200 Hash Conflict");

                                                    // int _id = GetID(pEnv, pObj);
                                                    // RegisterJVMObject(pEnv, GetID(pEnv, pObj) ,pObj);
                                                    if(!(res is JVMObject) && !(res is IJVMTuple))
                                                        // DB[subID] = res;
                                                        DB[subID] = new WeakReference(res);
                                                    SetObjectArrayElement(pEnv, pJArray, ii, pObj);
                                                    // RemoveID(subID);
                                                }
                                                else
                                                    throw new Exception(GetException(pEnv));
                                            }
                                            else
                                                throw new Exception(GetException(pEnv));
                                        }
                                        else if(res is System.Func<Object, Object, Object, Object, Object, Object, Object, Object>)
                                        {
                                            object[] pAr_len_data = new object[]{ res.GetType().ToString(), subID };
                                            void** pAr_len = (void**)(new StructWrapper(pEnv, pAr_len_data)).Ptr;

                                            void* pObj;
                                            void* CLRObjClass;
                                            void*  pLoadClassMethod; // The executed method struct
                                            if(FindClass( pEnv, "app/quant/clr/function/CLRFunction7", &CLRObjClass) == 0)
                                            {
                                                void* pClass;
                                                if(NewObjectP( pEnv, CLRObjClass, "(Ljava/lang/String;I)V", 2, pAr_len, &pObj ) == 0)
                                                {
                                                    if(DB.ContainsKey(subID))
                                                        Console.WriteLine("CLR 2227 Hash Conflict");

                                                    // int _id = GetID(pEnv, pObj);
                                                    // RegisterJVMObject(pEnv, GetID(pEnv, pObj) ,pObj);
                                                    if(!(res is JVMObject) && !(res is IJVMTuple))
                                                        // DB[subID] = res;
                                                        DB[subID] = new WeakReference(res);
                                                    SetObjectArrayElement(pEnv, pJArray, ii, pObj);
                                                    // RemoveID(subID);
                                                }
                                                else
                                                    throw new Exception(GetException(pEnv));
                                            }
                                            else
                                                throw new Exception(GetException(pEnv));
                                        }
                                        else if(res is System.Func<Object, Object, Object, Object, Object, Object, Object, Object, Object>)
                                        {
                                            object[] pAr_len_data = new object[]{ res.GetType().ToString(), subID };
                                            void** pAr_len = (void**)(new StructWrapper(pEnv, pAr_len_data)).Ptr;

                                            void* pObj;
                                            void* CLRObjClass;
                                            void*  pLoadClassMethod; // The executed method struct
                                            if(FindClass( pEnv, "app/quant/clr/function/CLRFunction8", &CLRObjClass) == 0)
                                            {
                                                void* pClass;
                                                if(NewObjectP( pEnv, CLRObjClass, "(Ljava/lang/String;I)V", 2, pAr_len, &pObj ) == 0)
                                                {
                                                    if(DB.ContainsKey(subID))
                                                        Console.WriteLine("CLR 2254 Hash Conflict");

                                                    // int _id = GetID(pEnv, pObj);
                                                    // RegisterJVMObject(pEnv, GetID(pEnv, pObj) ,pObj);
                                                    if(!(res is JVMObject) && !(res is IJVMTuple))
                                                        // DB[subID] = res;
                                                        DB[subID] = new WeakReference(res);
                                                    SetObjectArrayElement(pEnv, pJArray, ii, pObj);
                                                    // RemoveID(subID);
                                                }
                                                else
                                                    throw new Exception(GetException(pEnv));
                                            }
                                            else
                                                throw new Exception(GetException(pEnv));
                                        }
                                        else if(res is System.Func<Object, Object, Object, Object, Object, Object, Object, Object, Object, Object>)
                                        {
                                            object[] pAr_len_data = new object[]{ res.GetType().ToString(), subID };
                                            void** pAr_len = (void**)(new StructWrapper(pEnv, pAr_len_data)).Ptr;

                                            void* pObj;
                                            void* CLRObjClass;
                                            void*  pLoadClassMethod; // The executed method struct
                                            if(FindClass( pEnv, "app/quant/clr/function/CLRFunction9", &CLRObjClass) == 0)
                                            {
                                                void* pClass;
                                                if(NewObjectP( pEnv, CLRObjClass, "(Ljava/lang/String;I)V", 2, pAr_len, &pObj ) == 0)
                                                {
                                                    if(DB.ContainsKey(subID))
                                                        Console.WriteLine("CLR 2281 Hash Conflict");

                                                    // int _id = GetID(pEnv, pObj);
                                                    // RegisterJVMObject(pEnv, GetID(pEnv, pObj) ,pObj);
                                                    if(!(res is JVMObject) && !(res is IJVMTuple))
                                                        // DB[subID] = res;
                                                        DB[subID] = new WeakReference(res);
                                                    SetObjectArrayElement(pEnv, pJArray, ii, pObj);
                                                    // RemoveID(subID);
                                                }
                                                else
                                                    throw new Exception(GetException(pEnv));
                                            }
                                            else
                                                throw new Exception(GetException(pEnv));
                                        }
                                        else if(res is System.Func<Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object>)
                                        {
                                            object[] pAr_len_data = new object[]{ res.GetType().ToString(), subID };
                                            void** pAr_len = (void**)(new StructWrapper(pEnv, pAr_len_data)).Ptr;

                                            void* pObj;
                                            void* CLRObjClass;
                                            void*  pLoadClassMethod; // The executed method struct
                                            if(FindClass( pEnv, "app/quant/clr/function/CLRFunction10", &CLRObjClass) == 0)
                                            {
                                                void* pClass;
                                                if(NewObjectP( pEnv, CLRObjClass, "(Ljava/lang/String;I)V", 2, pAr_len, &pObj ) == 0)
                                                {
                                                    if(DB.ContainsKey(subID))
                                                        Console.WriteLine("CLR 2308 Hash Conflict");

                                                    // int _id = GetID(pEnv, pObj);
                                                    // RegisterJVMObject(pEnv, GetID(pEnv, pObj) ,pObj);
                                                    if(!(res is JVMObject) && !(res is IJVMTuple))
                                                        // DB[subID] = res;
                                                        DB[subID] = new WeakReference(res);
                                                    SetObjectArrayElement(pEnv, pJArray, ii, pObj);
                                                    // RemoveID(subID);
                                                }
                                                else
                                                    throw new Exception(GetException(pEnv));
                                            }
                                            else
                                                throw new Exception(GetException(pEnv));
                                        }
                                        else if(res is System.Func<Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object>)
                                        {
                                            object[] pAr_len_data = new object[]{ res.GetType().ToString(), subID };
                                            void** pAr_len = (void**)(new StructWrapper(pEnv, pAr_len_data)).Ptr;

                                            void* pObj;
                                            void* CLRObjClass;
                                            void*  pLoadClassMethod; // The executed method struct
                                            if(FindClass( pEnv, "app/quant/clr/function/CLRFunction11", &CLRObjClass) == 0)
                                            {
                                                void* pClass;
                                                if(NewObjectP( pEnv, CLRObjClass, "(Ljava/lang/String;I)V", 2, pAr_len, &pObj ) == 0)
                                                {
                                                    if(DB.ContainsKey(subID))
                                                        Console.WriteLine("CLR 2335 Hash Conflict");

                                                    // int _id = GetID(pEnv, pObj);
                                                    // RegisterJVMObject(pEnv, GetID(pEnv, pObj) ,pObj);
                                                    if(!(res is JVMObject) && !(res is IJVMTuple))
                                                        // DB[subID] = res;
                                                        DB[subID] = new WeakReference(res);
                                                    SetObjectArrayElement(pEnv, pJArray, ii, pObj);
                                                    // RemoveID(subID);
                                                }
                                                else
                                                    throw new Exception(GetException(pEnv));
                                            }
                                            else
                                                throw new Exception(GetException(pEnv));
                                        }
                                        else if(res is System.Func<Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object>)
                                        {
                                            object[] pAr_len_data = new object[]{ res.GetType().ToString(), subID };
                                            void** pAr_len = (void**)(new StructWrapper(pEnv, pAr_len_data)).Ptr;

                                            void* pObj;
                                            void* CLRObjClass;
                                            void*  pLoadClassMethod; // The executed method struct
                                            if(FindClass( pEnv, "app/quant/clr/function/CLRFunction12", &CLRObjClass) == 0)
                                            {
                                                void* pClass;
                                                if(NewObjectP( pEnv, CLRObjClass, "(Ljava/lang/String;I)V", 2, pAr_len, &pObj ) == 0)
                                                {
                                                    if(DB.ContainsKey(subID))
                                                        Console.WriteLine("CLR 2361 Hash Conflict");

                                                    // int _id = GetID(pEnv, pObj);
                                                    // RegisterJVMObject(pEnv, GetID(pEnv, pObj) ,pObj);
                                                    if(!(res is JVMObject) && !(res is IJVMTuple))
                                                        // DB[subID] = res;
                                                        DB[subID] = new WeakReference(res);
                                                    SetObjectArrayElement(pEnv, pJArray, ii, pObj);
                                                    // RemoveID(subID);
                                                }
                                                else
                                                    throw new Exception(GetException(pEnv));
                                            }
                                            else
                                                throw new Exception(GetException(pEnv));
                                        }
                                        else if(res is System.Func<Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object>)
                                        {
                                            object[] pAr_len_data = new object[]{ res.GetType().ToString(), subID };
                                            void** pAr_len = (void**)(new StructWrapper(pEnv, pAr_len_data)).Ptr;

                                            void* pObj;
                                            void* CLRObjClass;
                                            void*  pLoadClassMethod; // The executed method struct
                                            if(FindClass( pEnv, "app/quant/clr/function/CLRFunction13", &CLRObjClass) == 0)
                                            {
                                                void* pClass;
                                                if(NewObjectP( pEnv, CLRObjClass, "(Ljava/lang/String;I)V", 2, pAr_len, &pObj ) == 0)
                                                {
                                                    if(DB.ContainsKey(subID))
                                                        Console.WriteLine("CLR 2389 Hash Conflict");

                                                    // int _id = GetID(pEnv, pObj);
                                                    // RegisterJVMObject(pEnv, GetID(pEnv, pObj) ,pObj);                                                // DB.TryAdd(res.GetHashCode(), res);
                                                    if(!(res is JVMObject) && !(res is IJVMTuple))
                                                        // DB[subID] = res;
                                                        DB[subID] = new WeakReference(res);
                                                    SetObjectArrayElement(pEnv, pJArray, ii, pObj);
                                                    // RemoveID(subID);
                                                }
                                                else
                                                    throw new Exception(GetException(pEnv));
                                            }
                                            else
                                                throw new Exception(GetException(pEnv));
                                        }
                                        else if(res is System.Func<Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object>)
                                        {
                                            object[] pAr_len_data = new object[]{ res.GetType().ToString(), subID };
                                            void** pAr_len = (void**)(new StructWrapper(pEnv, pAr_len_data)).Ptr;

                                            void* pObj;
                                            void* CLRObjClass;
                                            void*  pLoadClassMethod; // The executed method struct
                                            if(FindClass( pEnv, "app/quant/clr/function/CLRFunction14", &CLRObjClass) == 0)
                                            {
                                                void* pClass;
                                                if(NewObjectP( pEnv, CLRObjClass, "(Ljava/lang/String;I)V", 2, pAr_len, &pObj ) == 0)
                                                {
                                                    if(DB.ContainsKey(subID))
                                                        Console.WriteLine("CLR 2416 Hash Conflict");

                                                    // int _id = GetID(pEnv, pObj);
                                                    // RegisterJVMObject(pEnv, GetID(pEnv, pObj) ,pObj);
                                                    if(!(res is JVMObject) && !(res is IJVMTuple))
                                                        // DB[subID] = res;
                                                        DB[subID] = new WeakReference(res);
                                                    SetObjectArrayElement(pEnv, pJArray, ii, pObj);
                                                    // RemoveID(subID);
                                                }
                                                else
                                                    throw new Exception(GetException(pEnv));
                                            }
                                            else
                                                throw new Exception(GetException(pEnv));
                                        }
                                        else if(res is System.Func<Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object>)
                                        {
                                            object[] pAr_len_data = new object[]{ res.GetType().ToString(), subID };
                                            void** pAr_len = (void**)(new StructWrapper(pEnv, pAr_len_data)).Ptr;

                                            void* pObj;
                                            void* CLRObjClass;
                                            void*  pLoadClassMethod; // The executed method struct
                                            if(FindClass( pEnv, "app/quant/clr/function/CLRFunction15", &CLRObjClass) == 0)
                                            {
                                                void* pClass;
                                                if(NewObjectP( pEnv, CLRObjClass, "(Ljava/lang/String;I)V", 2, pAr_len, &pObj ) == 0)
                                                {
                                                    if(DB.ContainsKey(subID))
                                                        Console.WriteLine("CLR 2443 Hash Conflict");
                                                    // int _id = GetID(pEnv, pObj);
                                                    // RegisterJVMObject(pEnv, GetID(pEnv, pObj) ,pObj);
                                                    if(!(res is JVMObject) && !(res is IJVMTuple))
                                                        // DB[subID] = res;
                                                        DB[subID] = new WeakReference(res);
                                                    SetObjectArrayElement(pEnv, pJArray, ii, pObj);
                                                    // RemoveID(subID);
                                                }
                                                else
                                                    throw new Exception(GetException(pEnv));
                                            }
                                            else
                                                throw new Exception(GetException(pEnv));
                                        }

                                        else
                                        {
                                            object[] pAr_len_data = new object[]{ sub_element.GetType().ToString(), subID, false };
                                            void** pAr_len = (void**)(new StructWrapper(pEnv, pAr_len_data)).Ptr;

                                            void* pObj;
                                            void* CLRObjClass;
                                            void*  pLoadClassMethod; // The executed method struct
                                            if(FindClass( pEnv, "app/quant/clr/CLRObject", &CLRObjClass) == 0)
                                            {
                                                void* pClass;
                                                if(NewObjectP( pEnv, CLRObjClass, "(Ljava/lang/String;IZ)V", 3, pAr_len, &pObj ) == 0)
                                                {
                                                    if(DB.ContainsKey(subID))
                                                        Console.WriteLine("CLR 2470 Hash Conflict");
                                                    // int _id = GetID(pEnv, pObj);
                                                    // RegisterJVMObject(pEnv, GetID(pEnv, pObj) ,pObj);
                                                    SetObjectArrayElement(pEnv, pJArray, ii, pObj);
                                                    if(!(sub_element is JVMObject) && !(sub_element is IJVMTuple))
                                                        // DB[subID] = sub_element;
                                                        DB[subID] = new WeakReference(sub_element);

                                                    // RemoveID(subID);
                                                }
                                                else
                                                    throw new Exception(GetException(pEnv));
                                            }
                                            else
                                                throw new Exception(GetException(pEnv));
                                        }
                                        break;
                                }

                                // sub_element.RegisterGCEvent(subID, delegate(object _obj, int _id)
                                // {
                                //     // Console.WriteLine("=========++++-~~~-++++Object(" + _obj + ") with hash code " + _id + " recently collected: ");
                                //     RemoveID(_id);
                                // });

                                // if(__DB.ContainsKey(subID))
                                // {
                                //     object _out;
                                //     __DB.TryRemove(subID, out _out);
                                // }
                                
                            }
                        }

                        int hashID = GetJVMID(pEnv, pJArray, true);
                        // int hashID = GetJVMID(pEnv, pJArray,false);
                        // RegisterJVMObject(pEnv, hashID, pJArray);

                        sub.RegisterGCEvent(hashID, delegate(object _obj, int _id)
                        {
                            // Console.WriteLine("=========++++++++Object(" + _obj + ") with hash code " + _id + " recently collected: ");
                            RemoveID(_id);
                        });

                        // if(__DB.ContainsKey(hashID))
                        // {
                        //     object _out;
                        //     __DB.TryRemove(hashID, out _out);
                        // }

                        

                        // RegisterJVMObject(pEnv, hashID, pJArray);
                        // if(cache)
                        //     Console.WriteLine("------------: " + cls + " " + cache);
                        // return new JVMObject(hashID, cls, cache); // IMPORTANT TRUE
                        var jo = new JVMObject(hashID, cls, true, "javaArray 2475"); // IMPORTANT TRUE
                        // GetID(jo, false);
                        // __DB[hashID] = jo;
                        return jo;
                    }
                    else
                        throw new Exception(GetException(pEnv));
                }
                catch(Exception e)
                {
                    Console.WriteLine("CLR getJavaArray ERROR: " + e);
                    return null;
                }
            }
        }

        private readonly static object objLock_getJavaArray_2 = new object();
        private unsafe static object[] getJavaArray(void* pEnv, void* pNetBridgeClass, int len, void* pObjResult, string returnSignature)//, IntPtr pNetBridgeClassPtr, IntPtr ArrayClassesMethodPtr)
        {
            lock(objLock_getJavaArray_2)
            // // lock(objLock_getJavaParameters)
            {
                try
                {
                    if(pObjResult == (void*)IntPtr.Zero)
                    {
                        Console.WriteLine("CLR getJavaArray pointer null");
                    }
                    int HashCode = GetJVMID(pEnv, pObjResult, true);

                    // 
                    void* _pNetBridgeClass = pNetBridgeClass;//(void*)pNetBridgeClassPtr;
                    int ret_arr_len = len;//getArrayLength(pEnv, ret_arr);

                    object[] resultArray = new object[ret_arr_len];

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
                        void*  pArrayClassesMethod;
                        if(GetStaticMethodID( pEnv, pNetBridgeClass, "ArrayClasses", "([Ljava/lang/Object;)[Ljava/lang/String;", &pArrayClassesMethod ) != 0)
                            throw new Exception(GetException(pEnv));

                        // JVMObject ret_arr = new JVMObject(HashCode, returnSignature, true);
                        
                        // object[] ar_data_pArrClasses = new object[]{ ret_arr };
                        // void** pArg_ArrClassesMethod = (void**)(new StructWrapper(pEnv, ar_data_pArrClasses)).Ptr;

                        var size = Unsafe.SizeOf<object[]>();
                        void** _ptr = (void**)Marshal.AllocHGlobal(size);
                        _ptr[0] = pObjResult;

                        
                        void* pArrClasses = IntPtr.Zero.ToPointer();
                        // if(CallStaticObjectMethod( pEnv, pNetBridgeClass, pArrayClassesMethod, &pArrClasses, 1, pArg_ArrClassesMethod) != 0) throw new Exception("Exception: getJavaArray CallStaticObjectMethod");
                        if(CallStaticObjectMethod( pEnv, pNetBridgeClass, pArrayClassesMethod, &pArrClasses, 1, _ptr) != 0) throw new Exception(GetException(pEnv));
                        
                        for(int i = 0; i < ret_arr_len; i++)
                        {
                            
                            void* pElementClass;
                            if(GetObjectArrayElement(pEnv, pArrClasses, i, &pElementClass) != 0)
                            {
                                Console.WriteLine("----ERROR: " + i + " " + ret_arr_len);
                                throw new Exception(GetException(pEnv));
                            }
                            
                            if(new IntPtr(pElementClass) == IntPtr.Zero)
                                resultArray[i] = null;
                            
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
                                else if(retElementClass == "java.time.LocalDateTime")
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
                                        if(!retElementClass.StartsWith("["))
                                        {
                                            int hashID_res = GetJVMID(pEnv, pElement_object, true);

                                            if(JVMDelegate.DB.ContainsKey(hashID_res) && JVMDelegate.DB[hashID_res].IsAlive) //check if it is a JVMDelegate
                                                resultArray[i] = ((JVMDelegate)JVMDelegate.DB[hashID_res].Target).func;

                                            else if(JVMObject.DB.ContainsKey(hashID_res) && JVMObject.DB[hashID_res].IsAlive) //check if it is a JVMObject
                                                // resultArray[i] = JVMObject.DB[hashID_res];
                                                resultArray[i] = JVMObject.DB[hashID_res].Target;
                                            
                                            else if(Runtime.DB.ContainsKey(hashID_res) && Runtime.DB[hashID_res].IsAlive) //check if it is a CLRObject
                                            {
                                                resultArray[i] = Runtime.DB[hashID_res].Target;
                                            }

                                            else
                                            {
                                                string cls = retElementClass.StartsWith("L") && retElementClass.EndsWith(";") ? retElementClass.Substring(1).Replace(";","") : retElementClass;

                                                resultArray[i] =  getObject(pEnv, cls, pElement_object);
                                            }
                                        }
                                        else
                                        {
                                            // int _hashID = GetID(pEnv, pElement_object);
                                            // JVMObject _ret_arr = new JVMObject(_hashID, retElementClass, true);
                                            // int _ret_arr_len = getArrayLength(pEnv, _ret_arr);
                                            int _ret_arr_len = getArrayLength(pEnv, pElement_object); //TESTING
                                            resultArray[i] =  getJavaArray(pEnv, _pNetBridgeClass, _ret_arr_len, pElement_object, retElementClass);
                                            // TESTING no diff
                                            // if(JVMObject.__DB.ContainsKey(_hashID))
                                            // {
                                            //     JVMObject oo;
                                            //     JVMObject.__DB.TryRemove(_hashID, out oo);
                                            // }
                                        }
                                    }
                                }
                                
                            }
                        }

                        
                        // if(JVMObject.__DB.ContainsKey(HashCode))
                        // {
                        //     JVMObject _out;
                        //     JVMObject.__DB.TryRemove(HashCode, out _out);
                        // }

                        
                    }

                    return resultArray;
                }
                catch(Exception e)
                {
                    Console.WriteLine("CLR getJavaArray 2: " + e);
                    return null;
                }
            }
        }

        private readonly static object objLock_getJavaArray_3 = new object();
        private unsafe static JVMObject getJavaArray(void* pEnv, void* pNetBridgeClass, IEnumerable<object> array)
        {
            lock(objLock_getJavaArray_3)
            {
                int arrLength = array.Count();
                object[] res = new object[arrLength];

                for(int i = 0; i < arrLength; i++)
                    res[i] = array.ElementAt(i);
                

                return getJavaArray(pEnv, pNetBridgeClass, res);
            }
        }

        private readonly static object objLock_getJavaArray_4 = new object();
        private unsafe static JVMObject getJavaArray(void* pEnv, void* pNetBridgeClass, Array array)
        {
            lock(objLock_getJavaArray_4)
            {
                int arrLength = array.Length;
                object[] res = new object[arrLength];

                for(int i = 0; i < arrLength; i++)
                    res[i] = array.GetValue(i);

                return getJavaArray(pEnv, pNetBridgeClass, res);
            }
        }

        public unsafe static object Python(System.Func<object[], object> func)
        {
            using(Py.GIL())
            {
                return func(null);
            }
        }

        private readonly static object objLock_TransformType_1 = new object();
        internal static String TransformType(Type type)
        {
            // // lock(objLock_TransformType_1)
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

                        // else if(type == typeof(System.Func<Object, Object>))
                        // {
                        //     Console.WriteLine("JAVA FUNCTION");
                        //     return "Ljava/util/function/Function;";
                        // }
                        else if(type == typeof(System.Func<Object, Object>))
                            return "Lscala/Function1;";
                        else if(type == typeof(System.Func<Object, Object, Object>))
                            return "Lscala/Function2;";
                        else if(type == typeof(System.Func<Object, Object, Object, Object>))
                            return "Lscala/Function3;";
                        else if(type == typeof(System.Func<Object, Object, Object, Object, Object>))
                            return "Lscala/Function4;";
                        else if(type == typeof(System.Func<Object, Object, Object, Object, Object, Object>))
                            return "Lscala/Function5;";
                        else if(type == typeof(System.Func<Object, Object, Object, Object, Object, Object, Object>))
                            return "Lscala/Function6;";
                        else if(type == typeof(System.Func<Object, Object, Object, Object, Object, Object, Object, Object>))
                            return "Lscala/Function7;";
                        else if(type == typeof(System.Func<Object, Object, Object, Object, Object, Object, Object, Object, Object>))
                            return "Lscala/Function8;";
                        else if(type == typeof(System.Func<Object, Object, Object, Object, Object, Object, Object, Object, Object, Object>))
                            return "Lscala/Function9;";
                        else if(type == typeof(System.Func<Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object>))
                            return "Lscala/Function10;";
                        else if(type == typeof(System.Func<Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object>))
                            return "Lscala/Function11;";
                        else if(type == typeof(System.Func<Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object>))
                            return "Lscala/Function12;";
                        else if(type == typeof(System.Func<Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object>))
                            return "Lscala/Function13;";
                        else if(type == typeof(System.Func<Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object>))
                            return "Lscala/Function14;";
                        else if(type == typeof(System.Func<Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object>))
                            return "Lscala/Function15;";

                        else if(type == typeof(IEnumerable<object>))
                            return "Ljava/lang/Iterable;";
                        else
                            return "Ljava/lang/Object;";

                } 
            }
        }

        private readonly static object objLock_TransformType_2 = new object();
        internal static String TransformType(Object obj)
        {
            // // lock(objLock_TransformType_2)
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
                        // else if(obj is System.Func<Object, Object>)
                        // {
                        //     Console.WriteLine("JAVA FUNCTION");
                        //     return "Ljava/util/function/Function;";
                        // }
                        else if(type == typeof(System.Func<Object, Object>))
                            return "Lscala/Function1;";
                        else if(type == typeof(System.Func<Object, Object, Object>))
                            return "Lscala/Function2;";
                        else if(type == typeof(System.Func<Object, Object, Object, Object>))
                            return "Lscala/Function3;";
                        else if(type == typeof(System.Func<Object, Object, Object, Object, Object>))
                            return "Lscala/Function4;";
                        else if(type == typeof(System.Func<Object, Object, Object, Object, Object, Object>))
                            return "Lscala/Function5;";
                        else if(type == typeof(System.Func<Object, Object, Object, Object, Object, Object, Object>))
                            return "Lscala/Function6;";
                        else if(type == typeof(System.Func<Object, Object, Object, Object, Object, Object, Object, Object>))
                            return "Lscala/Function7;";
                        else if(type == typeof(System.Func<Object, Object, Object, Object, Object, Object, Object, Object, Object>))
                            return "Lscala/Function8;";
                        else if(type == typeof(System.Func<Object, Object, Object, Object, Object, Object, Object, Object, Object, Object>))
                            return "Lscala/Function9;";
                        else if(type == typeof(System.Func<Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object>))
                            return "Lscala/Function10;";
                        else if(type == typeof(System.Func<Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object>))
                            return "Lscala/Function11;";
                        else if(type == typeof(System.Func<Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object>))
                            return "Lscala/Function12;";
                        else if(type == typeof(System.Func<Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object>))
                            return "Lscala/Function13;";
                        else if(type == typeof(System.Func<Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object>))
                            return "Lscala/Function14;";
                        else if(type == typeof(System.Func<Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object>))
                            return "Lscala/Function15;";
                        else if(obj is IEnumerable<object>)
                            return "Ljava/lang/Iterable;";

                        else
                            return "Ljava/lang/Object;";

                } 
            }
        }

        private readonly static object objLock_getHashCode = new object();
        // internal unsafe static int GetID(void* pEnv, void* pObj)
        internal unsafe static int GetJVMID(void* pEnv, void* pObj, bool cache)
        {
            lock(objLock_getHashCode)
            { 
                try
                {
                    void* pNetBridgeClass;
                    void* pSetPathMethod;

                    if(FindClass( pEnv, "app/quant/clr/CLRRuntime", &pNetBridgeClass) == 0 )
                    {
                        if( GetStaticMethodID( pEnv, pNetBridgeClass, "GetID", "(Ljava/lang/Object;Z)I", &pSetPathMethod ) == 0 )
                        {
                            void** pArg_lcs = stackalloc void*[2];
                            pArg_lcs[0] = pObj;
                            pArg_lcs[1] = *(void**)&cache;
                            int _res;
                            if(CallStaticIntMethod( pEnv, pNetBridgeClass, pSetPathMethod, 2, pArg_lcs, &_res) != 0)
                                Console.WriteLine("JAVA Object not registered...");

                            return _res;
                        }
                        else
                            Console.WriteLine("getHashCode method not found");
                    }

                    return 0;
                }
                catch(Exception e)
                {
                    Console.WriteLine("CLR GetID void: " + e);
                    return 0;
                }
            }
        }

        internal unsafe static int RemoveID(int id)
        {
            // return 0;
            // Console.WriteLine("RUNTIME REMOVEID: " + id);
            // lock(objLock_RegisterJVMObject)
            {

                if(DB.ContainsKey(id)) //TEST
                {
                    WeakReference _o;
                    DB.TryRemove(id, out _o);
                }

                // if(__DB.ContainsKey(id)) //NO
                // {
                //     object _o;
                //     __DB.TryRemove(id, out _o);
                // }

                if(MethodDB.ContainsKey(id)) //NO
                {
                    ConcurrentDictionary<string,MethodInfo> _o;
                    MethodDB.TryRemove(id, out _o);
                }

                // if(_DBID.ContainsKey(id)) //TEST
                // {
                //     int _i;
                //     _DBID.TryRemove(id, out _i);
                // }
                
                void*  pEnv;
                if(AttacheThread((void*)JVMPtr,&pEnv) != 0) throw new Exception ("Attach to thread error");
                void* pNetBridgeClass;
                void* pSetPathMethod;

                if(FindClass( pEnv, "app/quant/clr/CLRRuntime", &pNetBridgeClass) == 0 )
                {
                    if( GetStaticMethodID( pEnv, pNetBridgeClass, "RemoveID", "(I)V", &pSetPathMethod ) == 0 )
                    // if( GetStaticMethodID( pEnv, pNetBridgeClass, "RegisterObject", "(Ljava/lang/Object;)V", &pSetPathMethod ) == 0 )
                    {
                        void** pArg_lcs = stackalloc void*[1];
                        // pArg_lcs[0] = *(void**)&hashCode;
                        pArg_lcs[0] = *(void**)&id;
                        // void** pArg_lcs = stackalloc void*[1];
                        // pArg_lcs[0] = pObj;
                        
                        int _res = 0;
                        if(CallStaticVoidMethod( pEnv, pNetBridgeClass, pSetPathMethod, 1, pArg_lcs) != 0)
                            Console.WriteLine("JAVA RemoveID Object not registered...");

                        // Console.WriteLine("CS getHashCode: " + _res);

                        return _res;
                    }
                    else
                        Console.WriteLine("RemoveID method not found");

                    
                }

                return 0;
            }
        }

        private readonly static object objLock_isIterable = new object();
        private unsafe static bool isIterable(void* pEnv, void* pNetBridgeClass, void* pObj)
        {
            lock(objLock_isIterable)
            {
                try
                {
                    if(pObj == IntPtr.Zero.ToPointer())
                        return false;

                    // void*  pEnv;
                    if(true)//AttacheThread((void*)JVMPtr,&pEnv) == 0)
                    {
                        // void* pNetBridgeClass;
                        if(true)//FindClass( pEnv, "app/quant/clr/CLRRuntime", &pNetBridgeClass) == 0 )
                        {
                            void** pArg_lcs = stackalloc void*[1];
                            pArg_lcs[0] = *(void**)&pObj;
                            
                            void*  pMethodSigHashCode;
                            if(GetStaticMethodID( pEnv, pNetBridgeClass, "isIterable", "(Ljava/lang/Object;)Z", &pMethodSigHashCode ) == 0)
                            {
                                bool _res;
                                if(CallStaticBooleanMethod( pEnv, pNetBridgeClass, pMethodSigHashCode, 1, pArg_lcs, &_res) != 0)
                                    throw new Exception(GetException(pEnv));

                                return _res;
                            }
                            else
                                throw new Exception(GetException(pEnv));
                        }
                        else
                            throw new Exception(GetException(pEnv));
                        
                    }
                    else
                        throw new Exception(GetException(pEnv));
                }
                catch(Exception e)
                {
                    Console.WriteLine("CLR isIterable: " + e);
                    return false;
                }
            }
        }

        private readonly static object objLock_isMap = new object();
        private unsafe static bool isMap(void* pEnv, void* pNetBridgeClass, void* pObj)
        {
            lock(objLock_isMap)
            {
                try
                {
                    if(pObj == IntPtr.Zero.ToPointer())
                        return false;

                    // void*  pEnv;
                    if(true)//AttacheThread((void*)JVMPtr,&pEnv) == 0)
                    {
                        // void* pNetBridgeClass;
                        if(true)//FindClass( pEnv, "app/quant/clr/CLRRuntime", &pNetBridgeClass) == 0 )
                        {
                            void** pArg_lcs = stackalloc void*[1];
                            pArg_lcs[0] = *(void**)&pObj;
                            
                            void*  pMethodSigHashCode;
                            if(GetStaticMethodID( pEnv, pNetBridgeClass, "isMap", "(Ljava/lang/Object;)Z", &pMethodSigHashCode ) == 0)
                            {
                                bool _res;
                                if(CallStaticBooleanMethod( pEnv, pNetBridgeClass, pMethodSigHashCode, 1, pArg_lcs, &_res) != 0)
                                    throw new Exception(GetException(pEnv));
                                return _res;
                            }
                            else
                                throw new Exception(GetException(pEnv));
                        }
                        else
                            throw new Exception(GetException(pEnv));
                        
                    }
                    else
                        throw new Exception(GetException(pEnv));
                }
                catch(Exception e)
                {
                    Console.WriteLine("CLR isMap: " + e);
                    return false;
                }
            }
        }

        private readonly static object objLock_isCollection = new object();
        private unsafe static bool isCollection(void* pEnv, void* pNetBridgeClass, void* pObj)
        {
            lock(objLock_isCollection)
            {
                try
                {
                    if(pObj == IntPtr.Zero.ToPointer())
                        return false;

                    // void*  pEnv;
                    if(true)//AttacheThread((void*)JVMPtr,&pEnv) == 0)
                    {
                        // void* pNetBridgeClass;
                        if(true)//FindClass( pEnv, "app/quant/clr/CLRRuntime", &pNetBridgeClass) == 0 )
                        {
                            void** pArg_lcs = stackalloc void*[1];
                            pArg_lcs[0] = *(void**)&pObj;
                            
                            void*  pMethodSigHashCode;
                            if(GetStaticMethodID( pEnv, pNetBridgeClass, "isCollection", "(Ljava/lang/Object;)Z", &pMethodSigHashCode ) == 0)
                            {
                                bool _res;
                                if(CallStaticBooleanMethod( pEnv, pNetBridgeClass, pMethodSigHashCode, 1, pArg_lcs, &_res) != 0)
                                    throw new Exception(GetException(pEnv));
                                return _res;
                            }
                            else
                                throw new Exception(GetException(pEnv));
                        }
                        else
                            throw new Exception(GetException(pEnv));
                        
                    }
                    else
                        throw new Exception(GetException(pEnv));
                }
                catch(Exception e)
                {
                    Console.WriteLine("CLR isCollection: " + e);
                    return false;
                }
            }
        }

        private readonly static object objLock_getClass = new object();
        private unsafe static int getClass(void* pEnv, void* pObj, ref void* pClass)
        {
            lock(objLock_getClass)
            {
                if(pObj == IntPtr.Zero.ToPointer())
                {
                    Console.WriteLine("CLR getClass null pointer 1");
                    pClass = IntPtr.Zero.ToPointer();
                    return -2;
                }

                // void*  pEnv;
                if(true)//AttacheThread((void*)JVMPtr,&pEnv) == 0)
                {

                    void* pNameClass;
                    void* _pClass;

                    if(GetObjectClass(pEnv, pObj, &_pClass, &pNameClass) == 0)
                    {

                        pClass = _pClass;
                        return 0;
                    }

                    Console.WriteLine("CLR getClass null pointer 2");
                    return -1;
                    
                }

                Console.WriteLine("CLR getClass null pointer 3");
                return -2;
            }
        }

        private readonly static object objLock_getClassName = new object();
        private unsafe static int getClassName(void* pEnv, void* pObj, ref string cName)
        {
            lock(objLock_getClassName)
            {
                if(pObj == IntPtr.Zero.ToPointer())
                {
                    Console.WriteLine("CLR getClassName null pointer 1");
                    cName =  null;
                    return -2;
                }
                // void*  pEnv;// = (void*)EnvPtr;
                if(true)//AttacheThread((void*)JVMPtr,&pEnv) == 0)
                {

                    void* pNameClass;
                    void* _pClass;
                    if(GetObjectClass(pEnv, pObj, &_pClass, &pNameClass) == 0)
                    {

                        cName = GetNetString(pEnv, pNameClass);
                        return 0;
                    }
                    Console.WriteLine("CLR getClassName null pointer 2");
                    return -1;
                    
                }
                Console.WriteLine("CLR getClassName null pointer 3");
                return -2;
            }
        }

        private readonly static object objLock_getArrayLength = new object();
        // private unsafe static int getArrayLength(void* pEnv, JVMObject sig_arr)
        private unsafe static int getArrayLength(void* pEnv, void* arr)
        {
            lock(objLock_getArrayLength)
            { 
                try
                {
                    // void*  pEnv;// = (void*)EnvPtr;
                    // if(AttacheThread((void*)JVMPtr,&pEnv) != 0) throw new Exception ("Attach to thread error");

                    void* pArrayClass;
                    if(FindClass( pEnv, "java/lang/reflect/Array", &pArrayClass) == 0)
                    {
                        void* pArrayLengthMethod;
                        if(GetStaticMethodID( pEnv, pArrayClass, "getLength", "(Ljava/lang/Object;)I", &pArrayLengthMethod) == 0)
                        {
                            // void**  = stackalloc void*[1];
                            // object[] pAr_len_data = new object[]{ sig_arr };
                            // void** pAr_len = (void**)(new StructWrapper(pEnv, pAr_len_data)).Ptr;


                            var size = Unsafe.SizeOf<object[]>();
                            void** _ptr = (void**)Marshal.AllocHGlobal(size);
                            _ptr[0] = arr;

                            int _res;
                            // if(CallStaticIntMethod( pEnv, pArrayClass, pArrayLengthMethod, 1, pAr_len, &_res) != 0)
                            if(CallStaticIntMethod( pEnv, pArrayClass, pArrayLengthMethod, 1, _ptr, &_res) != 0)
                                throw new Exception(GetException(pEnv));
                            return _res;
                        }
                        else
                            throw new Exception(GetException(pEnv));
                    }
                    else
                        throw new Exception(GetException(pEnv));
                }
                catch(Exception e)
                {
                    Console.WriteLine("CLR getArrayLength: " + e);
                    return 0;
                }
            }
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
                    // void** pArg_lcs = stackalloc void*[1];
                    // object[] ar_data = new object[]{ new string[]{ path } };
                    object[] ar_data = new object[]{ paths };
                    // getJavaParameters(pEnv, ref pArg_lcs, ar_data);
                    void** pArg_lcs = (void**)(new StructWrapper(pEnv, ar_data)).Ptr;
                    if(CallStaticVoidMethod( pEnv, pNetBridgeClass, pSetPathMethod, 1, pArg_lcs) != 0)
                        Console.WriteLine("JAVA path not set..." + paths);
                }
                else
                    Console.WriteLine("SetBaseClassPath method not found");
            }
        }

        private readonly static object objLock_Signature = new object();
        public static string[] Signature(object obj)
        {
            // // lock(objLock_Signature)
            {
                // Console.WriteLine("RUNTIME Signature: " + obj);
                if(obj == null) return new string[]{};

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

                        // Console.WriteLine("---- SIG: " + sig);
                    }

                }

                return arr.ToArray();
            }
        }

        private readonly static object objLock_RegisterJVMObject = new object();
        public unsafe static void RegisterJVMObject(void* pEnv, int hashCode, void* pObj)
        {
            // lock(objLock_RegisterJVMObject)
            {
                // void*  pEnv;
                // if(AttacheThread((void*)JVMPtr,&pEnv) != 0) throw new Exception ("Attach to thread error");
                void* pNetBridgeClass;
                void* pSetPathMethod;

                if(FindClass( pEnv, "app/quant/clr/CLRRuntime", &pNetBridgeClass) == 0 )
                {
                    if( GetStaticMethodID( pEnv, pNetBridgeClass, "RegisterObject", "(ILjava/lang/Object;)V", &pSetPathMethod ) == 0 )
                    // if( GetStaticMethodID( pEnv, pNetBridgeClass, "RegisterObject", "(Ljava/lang/Object;)V", &pSetPathMethod ) == 0 )
                    {
                        void** pArg_lcs = stackalloc void*[2];
                        pArg_lcs[0] = *(void**)&hashCode;
                        pArg_lcs[1] = pObj;
                        // void** pArg_lcs = stackalloc void*[1];
                        // pArg_lcs[0] = pObj;
                        
                        if(CallStaticVoidMethod( pEnv, pNetBridgeClass, pSetPathMethod, 2, pArg_lcs) != 0)
                            Console.WriteLine("JAVA Object not registered...");
                    }
                    else
                        Console.WriteLine("RegisterObject method not found");
                }
            }
        }
        

        private readonly static object objLock_GetJVMObject_2 = new object();
        public unsafe static void* GetJVMObject(void* pEnv, void* pNetBridgeClass, int hashCode)
        {
            lock(objLock_GetJVMObject_2)
            {
                // void*  pEnv;
                // if(AttacheThread((void*)JVMPtr,&pEnv) != 0) throw new Exception ("Attach to thread error");
                // void* pNetBridgeClass;
                void* pGetJVMObjectMethod;

                // if(FindClass( pEnv, "app/quant/clr/CLRRuntime", &pNetBridgeClass) == 0 )
                if(true)
                {
                    if( GetStaticMethodID( pEnv, pNetBridgeClass, "GetObject", "(I)Ljava/lang/Object;", &pGetJVMObjectMethod ) == 0 )
                    {
                        // void** pAr_len = stackalloc void*[1];
                        object[] pAr_len_data = new object[]{ hashCode };
                        // getJavaParameters(pEnv, ref pAr_len, pAr_len_data);
                        void** pAr_len = (void**)(new StructWrapper(pEnv, pAr_len_data)).Ptr;

                        void*  pGetJVMObject;
                        if(CallStaticObjectMethod( pEnv, pNetBridgeClass, pGetJVMObjectMethod, &pGetJVMObject, 1, pAr_len) == 0)
                            return pGetJVMObject;
                        else
                        {
                            Console.WriteLine("GetObject error: " + GetException(pEnv));
                            return IntPtr.Zero.ToPointer();
                        }
                    }
                    else
                        Console.WriteLine("GetObject method not found: " + GetException(pEnv));
                }

                return IntPtr.Zero.ToPointer();
            }
        }
        
        private readonly static object objLock_getObject = new object();
        public unsafe static object getObject(void* _pEnv, string cls, void* pObjResult)
        {
            lock(objLock_getObject)
            {
                try
                {
                    string cName = null;
                    IntPtr returnPtr = new IntPtr(pObjResult);

                    if(getClassName(_pEnv, pObjResult, ref cName) == 0)
                    {
                        switch(cName)
                        {
                            case "java.lang.Boolean":
                                void*  pInvokeMethod_boolean;
                                if(GetMethodID( _pEnv, pObjResult, "booleanValue", "()Z", &pInvokeMethod_boolean ) == 0)
                                {
                                    void** pAr_boolean = stackalloc void*[1];
                                    {
                                        bool _res;
                                        if(CallBooleanMethod( _pEnv, pObjResult, pInvokeMethod_boolean, 1, pAr_boolean, &_res) != 0)
                                            throw new Exception(GetException(_pEnv));
                                        return _res;
                                    }
                                }
                                else
                                    throw new Exception(GetException(_pEnv));

                            case "java.lang.Byte":
                                void*  pInvokeMethod_byte;
                                if(GetMethodID( _pEnv, pObjResult, "byteValue", "()B", &pInvokeMethod_byte ) == 0)
                                {
                                    void** pAr_byte = stackalloc void*[1];
                                    byte _res;
                                    if(CallByteMethod( _pEnv, pObjResult, pInvokeMethod_byte, 1, pAr_byte, &_res) != 0)
                                        throw new Exception(GetException(_pEnv));
                                    return _res;
                                }
                                else
                                    throw new Exception(GetException(_pEnv));
                            

                            case "java.lang.Character":
                                void*  pInvokeMethod_char;
                                if(GetMethodID( _pEnv, pObjResult, "charValue", "()C", &pInvokeMethod_char ) == 0)
                                {
                                    void** pAr_char = stackalloc void*[1];
                                    char res_char;
                                    if(CallCharMethod( _pEnv, pObjResult, pInvokeMethod_char, 1, pAr_char, &res_char) != 0)
                                        throw new Exception(GetException(_pEnv));
                                    return res_char;
                                }
                                else
                                    throw new Exception(GetException(_pEnv));

                            case "java.lang.Short":
                                void*  pInvokeMethod_short;

                                if(GetMethodID( _pEnv, pObjResult, "shortValue", "()S", &pInvokeMethod_short ) == 0)
                                {
                                    void** pAr_short = stackalloc void*[1];
                                    short _res;
                                    if(CallShortMethod( _pEnv, pObjResult, pInvokeMethod_short, 1, pAr_short, &_res) != 0)
                                        throw new Exception(GetException(_pEnv));
                                    return _res;
                                }
                                else
                                    throw new Exception(GetException(_pEnv));


                            case "java.lang.Integer":
                                void*  pInvokeMethod_int;
                                if(GetMethodID( _pEnv, pObjResult, "intValue", "()I", &pInvokeMethod_int ) == 0)
                                {
                                    void** pAr_int = stackalloc void*[1];
                                    int res;
                                    if(CallIntMethod( _pEnv, pObjResult, pInvokeMethod_int, 1, pAr_int, &res) != 0)
                                        throw new Exception(GetException(_pEnv));
                                    return res;
                                    
                                }
                                else
                                    throw new Exception(GetException(_pEnv));

                            case "java.lang.Long":
                                void*  pInvokeMethod_long;
                                if(GetMethodID( _pEnv, pObjResult, "longValue", "()J", &pInvokeMethod_long ) == 0)
                                {
                                    void** pAr_long = stackalloc void*[1];
                                    
                                    long res;
                                    if(CallLongMethod( _pEnv, pObjResult, pInvokeMethod_long, 1, pAr_long, &res) != 0)
                                        throw new Exception(GetException(_pEnv));
                                    return res;
                                    
                                }
                                else
                                    throw new Exception(GetException(_pEnv));

                            case "java.lang.Float":
                                void*  pInvokeMethod_float;
                                if(GetMethodID( _pEnv, pObjResult, "floatValue", "()F", &pInvokeMethod_float ) == 0)
                                {
                                    void** pAr_float = stackalloc void*[1];
                                    float _res;
                                    if(CallFloatMethod( _pEnv, pObjResult, pInvokeMethod_float, 1, pAr_float, &_res) != 0)
                                        throw new Exception(GetException(_pEnv));
                                    return _res;
                                }
                                else
                                    throw new Exception(GetException(_pEnv));


                            case "java.lang.Double":
                                void*  pInvokeMethod_double;
                                if(GetMethodID( _pEnv, pObjResult, "doubleValue", "()D", &pInvokeMethod_double ) == 0)
                                {
                                    void** pAr_double = stackalloc void*[1];
                                    // return CallDoubleMethod( _pEnv, pObjResult, pInvokeMethod_double, 1, pAr_double);
                                    double res;
                                    if(CallDoubleMethod( _pEnv, pObjResult, pInvokeMethod_double, 1, pAr_double, &res) != 0)
                                        throw new Exception(GetException(_pEnv));
                                    return res;
                                }
                                else
                                    throw new Exception(GetException(_pEnv));

                            case "java.lang.String":
                                return GetNetString(_pEnv, pObjResult);

                            case "java.time.LocalDateTime":
                                return GetNetDateTime(_pEnv, pObjResult);

                            case "scala.Tuple1":
                            {
                                dynamic _tuple = CreateInstancePtr(_pEnv, cName, null, returnPtr, null );
                                JVMTuple1 tuple = new JVMTuple1(_tuple, _tuple._1());
                                // JVMObject.DB[_tuple.JavaHashCode] = new JVMTuple(_tuple, tuple);
                                var tp = new JVMTuple(_tuple, tuple);
                                // GetID(tp, false);
                                // JVMObject.DB[_tuple.JavaHashCode] = new WeakReference(tp);

                                // tuple.RegisterGCEvent(_tuple.JavaHashCode, delegate(object _obj, int _id)
                                // {
                                //     Runtime.RemoveID(_id);
                                // });
                                
                                return tuple;
                            }
                            case "scala.Tuple2":
                            {
                                dynamic _tuple = CreateInstancePtr(_pEnv, cName, null, returnPtr, null );
                                JVMTuple2 tuple = new JVMTuple2(_tuple, _tuple._1(), _tuple._2());
                                // JVMObject.DB[_tuple.JavaHashCode] = new JVMTuple(_tuple, tuple);
                                var tp = new JVMTuple(_tuple, tuple);
                                
                                // tuple.RegisterGCEvent(_tuple.JavaHashCode, delegate(object _obj, int _id)
                                // {
                                //     Runtime.RemoveID(_id);
                                // });
                                return tuple;
                            }
                            case "scala.Tuple3":
                            {
                                dynamic _tuple = CreateInstancePtr(_pEnv, cName, null, returnPtr, null );
                                JVMTuple3 tuple = new JVMTuple3(_tuple, _tuple._1(), _tuple._2(), _tuple._3());
                                // JVMObject.DB[_tuple.JavaHashCode] = new JVMTuple(_tuple, tuple);
                                
                                var tp = new JVMTuple(_tuple, tuple);
                                
                                // tuple.RegisterGCEvent(_tuple.JavaHashCode, delegate(object _obj, int _id)
                                // {
                                //     Runtime.RemoveID(_id);
                                // });

                                return tuple;
                            }
                            case "scala.Tuple4":
                            {
                                dynamic _tuple = CreateInstancePtr(_pEnv, cName, null, returnPtr, null );
                                JVMTuple4 tuple = new JVMTuple4(_tuple, _tuple._1(), _tuple._2(), _tuple._3(), _tuple._4());
                                
                                var tp = new JVMTuple(_tuple, tuple);
                                // GetID(tp, false);
                                // JVMObject.DB[_tuple.JavaHashCode] = new WeakReference(tp);
                                return tuple;
                            }
                            case "scala.Tuple5":
                            {
                                dynamic _tuple = CreateInstancePtr(_pEnv, cName, null, returnPtr, null );
                                JVMTuple5 tuple = new JVMTuple5(_tuple, _tuple._1(), _tuple._2(), _tuple._3(), _tuple._4(), _tuple._5());
                                
                                var tp = new JVMTuple(_tuple, tuple);
                                // GetID(tp, false);
                                // JVMObject.DB[_tuple.JavaHashCode] = new WeakReference(tp);
                                return tuple;
                            }
                            case "scala.Tuple6":
                            {
                                dynamic _tuple = CreateInstancePtr(_pEnv, cName, null, returnPtr, null );
                                JVMTuple6 tuple = new JVMTuple6(_tuple, _tuple._1(), _tuple._2(), _tuple._3(), _tuple._4(), _tuple._5(), _tuple._6());
                                
                                var tp = new JVMTuple(_tuple, tuple);
                                // GetID(tp, false);
                                // JVMObject.DB[_tuple.JavaHashCode] = new WeakReference(tp);
                                return tuple;
                            }
                            case "scala.Tuple7":
                            {
                                dynamic _tuple = CreateInstancePtr(_pEnv, cName, null, returnPtr, null );
                                JVMTuple7 tuple = new JVMTuple7(_tuple, _tuple._1(), _tuple._2(), _tuple._3(), _tuple._4(), _tuple._5(), _tuple._6(), _tuple._7());
                                
                                var tp = new JVMTuple(_tuple, tuple);
                                // GetID(tp, false);
                                // JVMObject.DB[_tuple.JavaHashCode] = new WeakReference(tp);
                                return tuple;
                            }
                            case "scala.Tuple8":
                            {
                                dynamic _tuple = CreateInstancePtr(_pEnv, cName, null, returnPtr, null );
                                JVMTuple8 tuple = new JVMTuple8(_tuple, _tuple._1(), _tuple._2(), _tuple._3(), _tuple._4(), _tuple._5(), _tuple._6(), _tuple._7(), _tuple._8());
                                
                                var tp = new JVMTuple(_tuple, tuple);
                                // GetID(tp, false);
                                // JVMObject.DB[_tuple.JavaHashCode] = new WeakReference(tp);
                                return tuple;
                            }

                            default:
                                var __res = CreateInstancePtr(_pEnv, cName, null, returnPtr, null );

                                
                                // GetID(__res, false);
                                return __res;
                        }
                    }
                    else
                        throw new Exception(GetException(_pEnv));
                }
                catch(Exception e)
                {
                    Console.WriteLine("CLR getObject: " + e);
                    return null;
                }
            }
        }
        
        private readonly static object objLock_CreateInstance = new object();
        public unsafe static JVMObject CreateInstance( string sClass, params object[] args )
        {
            // // lock(objLock_CreateInstance)
            {
                void*  _pEnv;
                if(AttacheThread((void*)JVMPtr,&_pEnv) != 0) throw new Exception ("Attach to thread error");

                var ret = CreateInstancePtr(_pEnv, sClass, null, IntPtr.Zero, args );
                DetacheThread((void*)JVMPtr);
                return ret;
            }
        }

        private readonly static object objLock_CreateInstancePath = new object();
        public unsafe static JVMObject CreateInstancePath( string sClass, string path, params object[] args )
        {
            // // lock(objLock_CreateInstancePath)
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
                        
                        // void** pArg_lcs = stackalloc void*[2];
                        object[] _ar_data = new object[]{ true, new string[]{ sClass, path } };
                        // getJavaParameters(_pEnv, ref pArg_lcs, _ar_data);
                        void** pArg_lcs = (void**)(new StructWrapper(_pEnv, _ar_data)).Ptr;
                        if(CallStaticObjectMethod( _pEnv, _pNetBridgeClass, pLoadClassMethod, &_pClass, 2, pArg_lcs) == 0)
                        {
                            var ret = CreateInstancePtr(_pEnv, sClass, path, IntPtr.Zero, args );
                            DetacheThread((void*)JVMPtr);
                            return ret;
                        }
                        else
                        {
                            throw new Exception("CreateInstancePath call: CallStaticObjectMethod error");
                        }
                    }
                    else
                    {
                        throw new Exception("CreateInstancePath Get method: CallStaticObjectMethod error");
                    }
                }
                else
                {
                    throw new Exception("CreateInstancePath get CLRRuntime class error");
                }
            }
        }

        private readonly static object objLock_CreateInstancePtr = new object();
        private unsafe static JVMObject CreateInstancePtr(void* pEnv,  string sClass, string path, IntPtr objPtr, object[] args )
        {
            lock(objLock_CreateInstancePtr)
            {
                void*  pNetBridgeClass;  // Class struct of the executed method
                void*  pSignaturesMethod; // The executed method struct
                void*  pArrayClassesMethod; // The executed method struct
                
                try
                {
                    if(Loaded)
                    {
                        // void*  pEnv;
                        // if(AttacheThread((void*)JVMPtr,&pEnv) != 0) throw new Exception ("Attach to thread error");
                        
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
                                            // void** pArg_lcs = stackalloc void*[1];
                                            object[] ar_data = new object[]{ path == null ? new string[]{ sClass } : new string[]{ sClass, path } };
                                            // getJavaParameters(pEnv, ref pArg_lcs, ar_data);
                                            void** pArg_lcs = (void**)(new StructWrapper(pEnv, ar_data)).Ptr;
                                            
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
                                        // void** pArg_sig = stackalloc void*[1];
                                        object[] ar_data = new object[]{ sClass };
                                        // getJavaParameters(pEnv, ref pArg_sig, ar_data);
                                        void** pArg_sig = (void**)(new StructWrapper(pEnv, ar_data)).Ptr;
                                        void* rArr;

                                        if(CallStaticObjectMethod( pEnv, pNetBridgeClass, pSignaturesMethod, &rArr, 1, pArg_sig) == 0)
                                        {
                                            // int sig_hashID = GetID(pEnv, rArr);
                                            // JVMObject sig_arr = new JVMObject(sig_hashID, "[Ljava/lang/String;", true); //IMPORTANT TRUE
                                            // int rArrLen = getArrayLength(pEnv, sig_arr);
                                            int rArrLen = getArrayLength(pEnv, rArr); //TESTING

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
                                                        var exm = GetException(pEnv);
                                                        Console.WriteLine("Error instantiating object (): " + sClass + " " + exm);
                                                        
                                                        throw new Exception(exm);
                                                    }
                                                }
                                                else
                                                {
                                                    // void** ar_newInstance = stackalloc void*[args.Length];
                                                    object[] args_object = new object[args.Length];
                                                    string argSig = "";
                                                    for(int i = 0; i < args.Length; i++)
                                                    {
                                                        argSig += TransformType(args[i]);
                                                        args_object[i] = args[i];
                                                    }

                                                    // getJavaParameters(pEnv, ref ar_newInstance, args_object);
                                                    void** ar_newInstance = (void**)(new StructWrapper(pEnv, args_object)).Ptr;
                                                    
                                                    if(NewObjectP( pEnv, pClass, "(" + argSig + ")V", args.Length, ar_newInstance, &pObj ) != 0)
                                                        throw new Exception("CreateInstancePtr / NewObjectP: " + GetException(pEnv));
                                                }
                                                ObjectPtr = new IntPtr(pObj);
                                            }
                                            else
                                                ObjectPtr = objPtr;


                                            int hashID = GetJVMID(pEnv, ObjectPtr.ToPointer(), true);

                                            dynamic expandoObject = new JVMObject(hashID, sClass, true, "EXPANDO 3901"); //IMPORTANT TRUE
                                            
                                            if(isMap(pEnv, pNetBridgeClass, ObjectPtr.ToPointer()))
                                                expandoObject = new JVMIDictionary(expandoObject);

                                            else if(isCollection(pEnv, pNetBridgeClass, ObjectPtr.ToPointer()))
                                                expandoObject = new JVMICollection(expandoObject);

                                            else if(isIterable(pEnv, pNetBridgeClass, ObjectPtr.ToPointer()))
                                                expandoObject = new JVMIEnumerable(expandoObject);

                                            // JVMObject.DB[expandoObject.JavaHashCode] = expandoObject;
                                            // JVMObject.DB[expandoObject.JavaHashCode] = new WeakReference(expandoObject);

                                            // GetID(expandoObject, false); // TESTING SHOULD NOT BE NEEDED

                                            // Console.WriteLine("------ EXPANDO CREATED");


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
                                                                        void* _pNetBridgeClass;
                                                                        if(FindClass( _pEnv, "app/quant/clr/CLRRuntime", &_pNetBridgeClass) != 0 ) throw new Exception ("Find Class");
                                                                        void* _pObj = GetJVMObject(_pEnv, _pNetBridgeClass, hashID);
                                                                        if(_pObj != IntPtr.Zero.ToPointer())
                                                                        {
                                                                            void*  pField;
                                                                            if(isStatic) 
                                                                            { 
                                                                                void* _pClass = IntPtr.Zero.ToPointer();
                                                                                if(getClass(_pEnv, _pObj, ref _pClass) == 0)
                                                                                    if(GetStaticFieldID( _pEnv, _pClass, name, returnSignature, &pField ) == 0)
                                                                                    {
                                                                                        bool _res;
                                                                                        if(GetStaticBooleanField( _pEnv, _pClass, pField, &_res) != 0)
                                                                                            throw new Exception("CreateInstancePtr / GetStaticBooleanField: " + GetException(_pEnv));
                                                                                        DetacheThread((void*)JVMPtr);
                                                                                        return _res;
                                                                                    }
                                                                                    else
                                                                                        throw new Exception("CreateInstancePtr / GetStaticBooleanFieldID: " + GetException(_pEnv));
                                                                                else
                                                                                    throw new Exception("CreateInstancePtr / GetStaticBooleanField Class: " + GetException(_pEnv));
                                                                            } 
                                                                            else  
                                                                            { 
                                                                                if(GetFieldID( _pEnv, _pObj, name, returnSignature, &pField ) == 0)
                                                                                {
                                                                                    bool _res;
                                                                                    if(GetBooleanField( _pEnv, _pObj, pField, &_res) != 0)
                                                                                        throw new Exception("CreateInstancePtr / GetBooleanField: " + GetException(_pEnv));
                                                                                    DetacheThread((void*)JVMPtr);
                                                                                    return _res;
                                                                                }
                                                                                else
                                                                                    throw new Exception("CreateInstancePtr / GetBooleanFieldID: " + GetException(_pEnv));
                                                                            }
                                                                        }
                                                                        else
                                                                            throw new Exception("CreateInstancePtr / GetBooleanField Class: " + GetException(_pEnv));
                                                                    }),
                                                                    (wrapSetProperty)((val) => {
                                                                        void*  _pEnv;
                                                                        if(AttacheThread((void*)JVMPtr,&_pEnv) != 0) throw new Exception ("Attach to thread error");
                                                                        void* _pNetBridgeClass;
                                                                        if(FindClass( _pEnv, "app/quant/clr/CLRRuntime", &_pNetBridgeClass) != 0 ) throw new Exception ("Find Class");
                                                                        
                                                                        void* _pObj = GetJVMObject(_pEnv, _pNetBridgeClass, hashID);
                                                                        if(_pObj != IntPtr.Zero.ToPointer())
                                                                        {
                                                                            void*  pField;
                                                                            if(isStatic) 
                                                                            { 
                                                                                void* _pClass = IntPtr.Zero.ToPointer();
                                                                                if(getClass(_pEnv, _pObj, ref _pClass) == 0)
                                                                                    if(GetStaticFieldID( _pEnv, _pClass, name, returnSignature, &pField ) == 0)
                                                                                        SetStaticBooleanField( _pEnv, _pClass, pField, (bool) val);
                                                                                    else
                                                                                        throw new Exception("CreateInstancePtr / SetStaticBooleanFieldID: " + GetException(_pEnv));
                                                                                else
                                                                                    throw new Exception("CreateInstancePtr / SetStaticBooleanField Class: " + GetException(_pEnv));
                                                                            } 
                                                                            else  
                                                                            { 
                                                                                if(GetFieldID( _pEnv, _pObj, name, returnSignature, &pField ) == 0)
                                                                                    SetBooleanField( _pEnv, _pObj, pField, (bool) val);
                                                                                else
                                                                                    throw new Exception("CreateInstancePtr / SetBooleanField: " + GetException(_pEnv));
                                                                            }
                                                                        }
                                                                        else
                                                                            new Exception("CreateInstancePtr / SetBooleanField NULL: " + GetException(_pEnv));

                                                                        DetacheThread((void*)JVMPtr);
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
                                                                        void* _pNetBridgeClass;
                                                                        if(FindClass( _pEnv, "app/quant/clr/CLRRuntime", &_pNetBridgeClass) != 0 ) throw new Exception ("Find Class");
                                                                        
                                                                        void* _pObj = GetJVMObject(_pEnv, _pNetBridgeClass, hashID);
                                                                        if(_pObj != IntPtr.Zero.ToPointer())
                                                                        {
                                                                            void*  pField;
                                                                            if(isStatic) 
                                                                            { 
                                                                                void* _pClass = IntPtr.Zero.ToPointer();
                                                                                if(getClass(_pEnv, _pObj, ref _pClass) == 0)
                                                                                    if(GetStaticFieldID( _pEnv, _pClass, name, returnSignature, &pField ) == 0)
                                                                                    {
                                                                                        byte _res;
                                                                                        if(GetStaticByteField( _pEnv, _pClass, pField, &_res) != 0)
                                                                                            throw new Exception("CreateInstancePtr / GetStaticByteField: " + GetException(_pEnv));

                                                                                        DetacheThread((void*)JVMPtr);
                                                                                        return _res;
                                                                                    }
                                                                                    else
                                                                                        throw new Exception("CreateInstancePtr / GetStaticByteFieldID: " + GetException(_pEnv));
                                                                                else
                                                                                    throw new Exception("CreateInstancePtr / GetStaticByteField Class: " + GetException(_pEnv));
                                                                            } 
                                                                            else  
                                                                            { 
                                                                                if(GetFieldID( _pEnv, _pObj, name, returnSignature, &pField ) == 0)
                                                                                {
                                                                                    byte _res;
                                                                                    if(GetByteField( _pEnv, _pObj, pField, &_res) != 0)
                                                                                        throw new Exception("CreateInstancePtr / GetByteField: " + GetException(_pEnv));

                                                                                    DetacheThread((void*)JVMPtr);
                                                                                    return _res;

                                                                                }
                                                                                else
                                                                                    throw new Exception("CreateInstancePtr / GetByteFieldID: " + GetException(_pEnv));
                                                                            }
                                                                        }
                                                                        else
                                                                            throw new Exception("CreateInstancePtr / GetByteField NULL: " + GetException(_pEnv));
                                                                    }),
                                                                    (wrapSetProperty)((val) => {
                                                                        void*  _pEnv;
                                                                        if(AttacheThread((void*)JVMPtr,&_pEnv) != 0) throw new Exception ("Attach to thread error");
                                                                        void* _pNetBridgeClass;
                                                                        if(FindClass( _pEnv, "app/quant/clr/CLRRuntime", &_pNetBridgeClass) != 0 ) throw new Exception ("Find Class");
                                                                        
                                                                        void* _pObj = GetJVMObject(_pEnv, _pNetBridgeClass, hashID);
                                                                        if(_pObj != IntPtr.Zero.ToPointer())
                                                                        {
                                                                            void*  pField;
                                                                            if(isStatic) 
                                                                            { 
                                                                                void* _pClass = IntPtr.Zero.ToPointer();
                                                                                if(getClass(_pEnv, _pObj, ref _pClass) == 0)
                                                                                    if(GetStaticFieldID( _pEnv, _pClass, name, returnSignature, &pField ) == 0)
                                                                                        SetStaticByteField( _pEnv, _pClass, pField, (byte) val);
                                                                                    else
                                                                                        throw new Exception("CreateInstancePtr / SetStaticByteFieldID: " + GetException(_pEnv));
                                                                                else
                                                                                    throw new Exception("CreateInstancePtr / SetStaticByteFieldID Class: " + GetException(_pEnv));
                                                                            } 
                                                                            else  
                                                                            { 
                                                                                if(GetFieldID( _pEnv, _pObj, name, returnSignature, &pField ) == 0)
                                                                                    SetByteField( _pEnv, _pObj, pField, (byte) val);
                                                                                else
                                                                                    throw new Exception("CreateInstancePtr / SetByteFieldID: " + GetException(_pEnv));
                                                                            }
                                                                        }
                                                                        else
                                                                            throw new Exception("CreateInstancePtr / SetByteField NULL: " + GetException(_pEnv));

                                                                        DetacheThread((void*)JVMPtr);
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
                                                                        void* _pNetBridgeClass;
                                                                        if(FindClass( _pEnv, "app/quant/clr/CLRRuntime", &_pNetBridgeClass) != 0 ) throw new Exception ("Find Class");
                                                                        
                                                                        void* _pObj = GetJVMObject(_pEnv, _pNetBridgeClass, hashID);
                                                                        if(_pObj != IntPtr.Zero.ToPointer())
                                                                        {
                                                                            void*  pField;
                                                                            if(isStatic) 
                                                                            { 
                                                                                void* _pClass = IntPtr.Zero.ToPointer();
                                                                                if(getClass(_pEnv, _pObj, ref _pClass) == 0)
                                                                                    if(GetStaticFieldID( _pEnv, _pClass, name, returnSignature, &pField ) == 0)
                                                                                    {
                                                                                        char _res;
                                                                                        if(GetStaticCharField( _pEnv, _pClass, pField, &_res) != 0)
                                                                                            throw new Exception("CreateInstancePtr / GetStaticCharField: " + GetException(_pEnv));

                                                                                        DetacheThread((void*)JVMPtr);
                                                                                        return _res;
                                                                                    }
                                                                                    else
                                                                                        throw new Exception("CreateInstancePtr / GetStaticCharFieldID: " + GetException(_pEnv));
                                                                                else
                                                                                    throw new Exception("CreateInstancePtr / GetStaticCharField Class: " + GetException(_pEnv));
                                                                            } 
                                                                            else  
                                                                            { 
                                                                                if(GetFieldID( _pEnv, _pObj, name, returnSignature, &pField ) == 0)
                                                                                {
                                                                                    char _res;
                                                                                    if(GetCharField( _pEnv, _pObj, pField, &_res) != 0)
                                                                                        throw new Exception("CreateInstancePtr / GetCharField: " + GetException(_pEnv));

                                                                                    DetacheThread((void*)JVMPtr);
                                                                                    return _res;
                                                                                }
                                                                                else
                                                                                    throw new Exception("CreateInstancePtr / GetCharFieldID: " + GetException(_pEnv));
                                                                            }
                                                                        }
                                                                        else
                                                                            throw new Exception("CreateInstancePtr / GetCharField NULL: " + GetException(_pEnv));
                                                                    }),
                                                                    (wrapSetProperty)((val) => {
                                                                        void*  _pEnv;
                                                                        if(AttacheThread((void*)JVMPtr,&_pEnv) != 0) throw new Exception ("Attach to thread error");
                                                                        void* _pNetBridgeClass;
                                                                        if(FindClass( _pEnv, "app/quant/clr/CLRRuntime", &_pNetBridgeClass) != 0 ) throw new Exception ("Find Class");
                                                                        
                                                                        void* _pObj = GetJVMObject(_pEnv, _pNetBridgeClass, hashID);
                                                                        if(_pObj != IntPtr.Zero.ToPointer())
                                                                        {
                                                                            void*  pField;
                                                                            if(isStatic) 
                                                                            { 
                                                                                void* _pClass = IntPtr.Zero.ToPointer();
                                                                                if(getClass(_pEnv, _pObj, ref _pClass) == 0)
                                                                                    if(GetStaticFieldID( _pEnv, _pClass, name, returnSignature, &pField ) == 0)
                                                                                        SetStaticCharField( _pEnv, _pClass, pField, (char) val);
                                                                                    else
                                                                                        throw new Exception("CreateInstancePtr / SetStaticCharFieldID : " + GetException(_pEnv));
                                                                                else
                                                                                    throw new Exception("CreateInstancePtr / SetStaticCharField Class: " + GetException(_pEnv));
                                                                            } 
                                                                            else  
                                                                            { 
                                                                                if(GetFieldID( _pEnv, _pObj, name, returnSignature, &pField ) == 0)
                                                                                    SetCharField( _pEnv, _pObj, pField, (char) val);
                                                                                else
                                                                                    throw new Exception("CreateInstancePtr / SetStaticCharFieldID: " + GetException(_pEnv));
                                                                            }
                                                                        }
                                                                        else
                                                                            throw new Exception("CreateInstancePtr / SetCharField NULL: " + GetException(_pEnv));

                                                                        DetacheThread((void*)JVMPtr);
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
                                                                        void* _pNetBridgeClass;
                                                                        if(FindClass( _pEnv, "app/quant/clr/CLRRuntime", &_pNetBridgeClass) != 0 ) throw new Exception ("Find Class");
                                                                        
                                                                        void* _pObj = GetJVMObject(_pEnv, _pNetBridgeClass, hashID);
                                                                        if(_pObj != IntPtr.Zero.ToPointer())
                                                                        {
                                                                            void*  pField;
                                                                            if(isStatic) 
                                                                            { 
                                                                                void* _pClass = IntPtr.Zero.ToPointer();
                                                                                if(getClass(_pEnv, _pObj, ref _pClass) == 0)
                                                                                    if(GetStaticFieldID( _pEnv, _pClass, name, returnSignature, &pField ) == 0)
                                                                                    {
                                                                                        short _res;
                                                                                        if(GetStaticShortField( _pEnv, _pClass, pField, &_res) != 0)
                                                                                            throw new Exception("CreateInstancePtr / GetStaticShortield: " + GetException(_pEnv));

                                                                                        DetacheThread((void*)JVMPtr);
                                                                                        return _res;
                                                                                    }
                                                                                    else
                                                                                        throw new Exception("CreateInstancePtr / GetStaticShortFieldID: " + GetException(_pEnv));
                                                                                else
                                                                                    throw new Exception("CreateInstancePtr / GetStaticShortField Class: " + GetException(_pEnv));
                                                                            } 
                                                                            else  
                                                                            { 
                                                                                if(GetFieldID( _pEnv, _pObj, name, returnSignature, &pField ) == 0)
                                                                                {
                                                                                    short _res;
                                                                                    if(GetShortField( _pEnv, _pObj, pField, &_res) != 0)
                                                                                        throw new Exception("CreateInstancePtr / GetShortField: " + GetException(_pEnv));

                                                                                    DetacheThread((void*)JVMPtr);
                                                                                    return _res;
                                                                                }
                                                                                else
                                                                                    throw new Exception("CreateInstancePtr / GetShortField ID: " + GetException(_pEnv));
                                                                            }
                                                                        }
                                                                        else
                                                                            throw new Exception("CreateInstancePtr / GetSShortField NULL: " + GetException(_pEnv));
                                                                    }),
                                                                    (wrapSetProperty)((val) => {
                                                                        void*  _pEnv;
                                                                        if(AttacheThread((void*)JVMPtr,&_pEnv) != 0) throw new Exception ("Attach to thread error");
                                                                        void* _pNetBridgeClass;
                                                                        if(FindClass( _pEnv, "app/quant/clr/CLRRuntime", &_pNetBridgeClass) != 0 ) throw new Exception ("Find Class");
                                                                        
                                                                        void* _pObj = GetJVMObject(_pEnv, _pNetBridgeClass, hashID);
                                                                        if(_pObj != IntPtr.Zero.ToPointer())
                                                                        {
                                                                            void*  pField;
                                                                            if(isStatic) 
                                                                            { 
                                                                                void* _pClass = IntPtr.Zero.ToPointer();
                                                                                if(getClass(_pEnv, _pObj, ref _pClass) == 0)
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

                                                                        DetacheThread((void*)JVMPtr);
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
                                                                        void* _pNetBridgeClass;
                                                                        if(FindClass( _pEnv, "app/quant/clr/CLRRuntime", &_pNetBridgeClass) != 0 ) throw new Exception ("Find Class");
                                                                        
                                                                        void* _pObj = GetJVMObject(_pEnv, _pNetBridgeClass, hashID);
                                                                        if(_pObj != IntPtr.Zero.ToPointer())
                                                                        {
                                                                            void*  pField;
                                                                            if(isStatic) 
                                                                            { 
                                                                                void* _pClass = IntPtr.Zero.ToPointer();
                                                                                if(getClass(_pEnv, _pObj, ref _pClass) == 0)
                                                                                    if(GetStaticFieldID( _pEnv, _pClass, name, returnSignature, &pField ) == 0)
                                                                                    {
                                                                                        int _res;
                                                                                        if(GetStaticIntField( _pEnv, _pClass, pField, &_res) != 0)
                                                                                            throw new Exception(GetException(_pEnv));

                                                                                        DetacheThread((void*)JVMPtr);
                                                                                        return _res;
                                                                                    }
                                                                                    else
                                                                                        throw new Exception(GetException(_pEnv));
                                                                                else
                                                                                    throw new Exception(GetException(_pEnv));
                                                                            } 
                                                                            else  
                                                                            { 
                                                                                if(GetFieldID( _pEnv, _pObj, name, returnSignature, &pField ) == 0)
                                                                                {
                                                                                    int _res;
                                                                                    if(GetIntField( _pEnv, _pObj, pField, &_res) != 0)
                                                                                        throw new Exception(GetException(_pEnv));

                                                                                    DetacheThread((void*)JVMPtr);
                                                                                    return _res;
                                                                                }
                                                                                else
                                                                                    throw new Exception(GetException(_pEnv));
                                                                            }
                                                                        }
                                                                        else
                                                                            throw new Exception("Runtime Object not found: " + name);
                                                                    }),
                                                                    (wrapSetProperty)((val) => {
                                                                        void*  _pEnv;
                                                                        if(AttacheThread((void*)JVMPtr,&_pEnv) != 0) throw new Exception ("Attach to thread error");
                                                                        void* _pNetBridgeClass;
                                                                        if(FindClass( _pEnv, "app/quant/clr/CLRRuntime", &_pNetBridgeClass) != 0 ) throw new Exception ("Find Class");
                                                                        
                                                                        void* _pObj = GetJVMObject(_pEnv, _pNetBridgeClass, hashID);
                                                                        if(_pObj != IntPtr.Zero.ToPointer())
                                                                        {
                                                                            void*  pField;
                                                                            if(isStatic) 
                                                                            { 
                                                                                void* _pClass = IntPtr.Zero.ToPointer();
                                                                                if(getClass(_pEnv, _pObj,  ref _pClass) == 0)
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

                                                                        DetacheThread((void*)JVMPtr);
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
                                                                        void* _pNetBridgeClass;
                                                                        if(FindClass( _pEnv, "app/quant/clr/CLRRuntime", &_pNetBridgeClass) != 0 ) throw new Exception ("Find Class");
                                                                        
                                                                        void* _pObj = GetJVMObject(_pEnv, _pNetBridgeClass, hashID);
                                                                        if(_pObj != IntPtr.Zero.ToPointer())
                                                                        {
                                                                            void*  pField;
                                                                            if(isStatic) 
                                                                            { 
                                                                                void* _pClass = IntPtr.Zero.ToPointer();
                                                                                if(getClass(_pEnv, _pObj,  ref _pClass) == 0)
                                                                                    if(GetStaticFieldID( _pEnv, _pClass, name, returnSignature, &pField ) == 0)
                                                                                    {
                                                                                        long _res;
                                                                                        if(GetStaticLongField( _pEnv, _pClass, pField, &_res) != 0)
                                                                                            throw new Exception(GetException(_pEnv));

                                                                                        DetacheThread((void*)JVMPtr);
                                                                                        return _res;
                                                                                    }
                                                                                    else
                                                                                        throw new Exception("Runtime Static Field not found: " + name);
                                                                                else
                                                                                    throw new Exception("Runtime Class not found: " + name);
                                                                            } 
                                                                            else  
                                                                            { 
                                                                                if(GetFieldID( _pEnv, _pObj, name, returnSignature, &pField ) == 0)
                                                                                {
                                                                                    long _res;
                                                                                    if(GetLongField( _pEnv, _pObj, pField, &_res) != 0)
                                                                                        throw new Exception(GetException(_pEnv));

                                                                                    DetacheThread((void*)JVMPtr);
                                                                                    return _res;
                                                                                }
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
                                                                        void* _pNetBridgeClass;
                                                                        if(FindClass( _pEnv, "app/quant/clr/CLRRuntime", &_pNetBridgeClass) != 0 ) throw new Exception ("Find Class");
                                                                        
                                                                        void* _pObj = GetJVMObject(_pEnv, _pNetBridgeClass, hashID);
                                                                        if(_pObj != IntPtr.Zero.ToPointer())
                                                                        {
                                                                            void*  pField;
                                                                            if(isStatic) 
                                                                            { 
                                                                                void* _pClass = IntPtr.Zero.ToPointer();
                                                                                if(getClass(_pEnv, _pObj,  ref _pClass) == 0)
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

                                                                        DetacheThread((void*)JVMPtr);
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
                                                                        void* _pNetBridgeClass;
                                                                        if(FindClass( _pEnv, "app/quant/clr/CLRRuntime", &_pNetBridgeClass) != 0 ) throw new Exception ("Find Class");
                                                                        
                                                                        void* _pObj = GetJVMObject(_pEnv, _pNetBridgeClass, hashID);
                                                                        if(_pObj != IntPtr.Zero.ToPointer())
                                                                        {
                                                                            void*  pField;
                                                                            if(isStatic) 
                                                                            { 
                                                                                void* _pClass = IntPtr.Zero.ToPointer();
                                                                                if(getClass(_pEnv, _pObj,  ref _pClass) == 0)
                                                                                    if(GetStaticFieldID( _pEnv, _pClass, name, returnSignature, &pField ) == 0)
                                                                                    {
                                                                                        float _res;
                                                                                        if(GetStaticFloatField( _pEnv, _pClass, pField, &_res) != 0)
                                                                                            throw new Exception(GetException(_pEnv));

                                                                                        DetacheThread((void*)JVMPtr);
                                                                                        return _res;
                                                                                    }
                                                                                    else
                                                                                        throw new Exception(GetException(_pEnv));
                                                                                else
                                                                                    throw new Exception(GetException(_pEnv));
                                                                            } 
                                                                            else  
                                                                            { 
                                                                                if(GetFieldID( _pEnv, _pObj, name, returnSignature, &pField ) == 0)
                                                                                {
                                                                                    float _res;
                                                                                    if(GetFloatField( _pEnv, _pObj, pField, &_res) != 0)
                                                                                        throw new Exception(GetException(_pEnv));

                                                                                    DetacheThread((void*)JVMPtr);
                                                                                    return _res;
                                                                                }
                                                                                else
                                                                                    throw new Exception(GetException(_pEnv));
                                                                            }
                                                                        }
                                                                        else
                                                                            throw new Exception("Runtime Object not found: " + name);
                                                                    }),
                                                                    (wrapSetProperty)((val) => {
                                                                        void*  _pEnv;
                                                                        if(AttacheThread((void*)JVMPtr,&_pEnv) != 0) throw new Exception ("Attach to thread error");
                                                                        void* _pNetBridgeClass;
                                                                        if(FindClass( _pEnv, "app/quant/clr/CLRRuntime", &_pNetBridgeClass) != 0 ) throw new Exception ("Find Class");
                                                                        
                                                                        void* _pObj = GetJVMObject(_pEnv, _pNetBridgeClass, hashID);
                                                                        if(_pObj != IntPtr.Zero.ToPointer())
                                                                        {
                                                                            void*  pField;
                                                                            if(isStatic) 
                                                                            { 
                                                                                void* _pClass = IntPtr.Zero.ToPointer();
                                                                                if(getClass(_pEnv, _pObj,  ref _pClass) == 0)
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

                                                                        DetacheThread((void*)JVMPtr);
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
                                                                        void* _pNetBridgeClass;
                                                                        if(FindClass( _pEnv, "app/quant/clr/CLRRuntime", &_pNetBridgeClass) != 0 ) throw new Exception ("Find Class");
                                                                
                                                                        void* _pObj = GetJVMObject(_pEnv, _pNetBridgeClass, hashID);
                                                                        if(_pObj != IntPtr.Zero.ToPointer())
                                                                        {
                                                                            void*  pField;
                                                                            if(isStatic) 
                                                                            { 
                                                                                void* _pClass = IntPtr.Zero.ToPointer();
                                                                                if(getClass(_pEnv, _pObj,  ref _pClass) == 0)
                                                                                    if(GetStaticFieldID( _pEnv, _pClass, name, returnSignature, &pField ) == 0)
                                                                                    {
                                                                                        double _res;
                                                                                        if(GetStaticDoubleField( _pEnv, _pClass, pField, &_res) != 0)
                                                                                            throw new Exception(GetException(_pEnv));

                                                                                        DetacheThread((void*)JVMPtr);
                                                                                        return _res;
                                                                                    }
                                                                                    else
                                                                                        throw new Exception(GetException(_pEnv));
                                                                                else
                                                                                    throw new Exception(GetException(_pEnv));
                                                                            } 
                                                                            else  
                                                                            { 
                                                                                if(GetFieldID( _pEnv, _pObj, name, returnSignature, &pField ) == 0)
                                                                                {
                                                                                    double _res;
                                                                                    if(GetDoubleField( _pEnv, _pObj, pField, &_res) != 0)
                                                                                        throw new Exception(GetException(_pEnv));

                                                                                    DetacheThread((void*)JVMPtr);
                                                                                    return _res;
                                                                                }
                                                                                else
                                                                                    throw new Exception(GetException(_pEnv));
                                                                            }
                                                                        }
                                                                        else
                                                                            throw new Exception("Runtime Object not found: " + name);
                                                                    }),
                                                                    (wrapSetProperty)((val) => {
                                                                        void*  _pEnv;
                                                                        if(AttacheThread((void*)JVMPtr,&_pEnv) != 0) throw new Exception ("Attach to thread error");
                                                                        void* _pNetBridgeClass;
                                                                        if(FindClass( _pEnv, "app/quant/clr/CLRRuntime", &_pNetBridgeClass) != 0 ) throw new Exception ("Find Class");
                                                                
                                                                        void* _pObj = GetJVMObject(_pEnv, _pNetBridgeClass, hashID);
                                                                        if(_pObj != IntPtr.Zero.ToPointer())
                                                                        {
                                                                            void*  pField;
                                                                            if(isStatic) 
                                                                            { 
                                                                                void* _pClass = IntPtr.Zero.ToPointer();
                                                                                if(getClass(_pEnv, _pObj,  ref _pClass) == 0)
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

                                                                        DetacheThread((void*)JVMPtr);
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
                                                                        void* _pNetBridgeClass;
                                                                        if(FindClass( _pEnv, "app/quant/clr/CLRRuntime", &_pNetBridgeClass) != 0 ) throw new Exception ("Find Class");
                                                                
                                                                        void* _pObj = GetJVMObject(_pEnv, _pNetBridgeClass, hashID);
                                                                        if(_pObj != IntPtr.Zero.ToPointer())
                                                                        {
                                                                            void*  pField;
                                                                            if(isStatic) 
                                                                            { 
                                                                                void* pObjResult;
                                                                                void* _pClass = IntPtr.Zero.ToPointer();
                                                                                if(getClass(_pEnv, _pObj, ref _pClass) == 0)
                                                                                    if(GetStaticFieldID( _pEnv, _pClass, name, returnSignature, &pField ) == 0)
                                                                                        if(GetStaticObjectField( _pEnv, _pClass, pField, &pObjResult) == 0)
                                                                                        {
                                                                                            string _ret = GetNetString(_pEnv, pObjResult);
                                                                                            DetacheThread((void*)JVMPtr);
                                                                                            return _ret;
                                                                                        }
                                                                                        else
                                                                                            throw new Exception(GetException(_pEnv));
                                                                                    else
                                                                                        throw new Exception(GetException(_pEnv));
                                                                                else
                                                                                    throw new Exception(GetException(_pEnv));
                                                                            } 
                                                                            else  
                                                                            { 
                                                                                void* pObjResult;
                                                                                if(GetFieldID( _pEnv, _pObj, name, returnSignature, &pField ) == 0)
                                                                                    if(GetObjectField( _pEnv, _pObj, pField, &pObjResult) == 0)
                                                                                    {
                                                                                        string _ret = GetNetString(_pEnv, pObjResult);
                                                                                        DetacheThread((void*)JVMPtr);
                                                                                        return _ret;
                                                                                    }
                                                                                    else
                                                                                        throw new Exception(GetException(_pEnv));
                                                                                else
                                                                                    throw new Exception(GetException(_pEnv));
                                                                            }
                                                                        }
                                                                        else
                                                                            throw new Exception(GetException(_pEnv));
                                                                    }),
                                                                    (wrapSetProperty)((val) => {
                                                                        void*  _pEnv;
                                                                        if(AttacheThread((void*)JVMPtr,&_pEnv) != 0) throw new Exception ("Attach to thread error");
                                                                        void* _pNetBridgeClass;
                                                                        if(FindClass( _pEnv, "app/quant/clr/CLRRuntime", &_pNetBridgeClass) != 0 ) throw new Exception ("Find Class");
                                                                
                                                                        void* _pObj = GetJVMObject(_pEnv, _pNetBridgeClass, hashID);
                                                                        if(_pObj != IntPtr.Zero.ToPointer())
                                                                        {
                                                                            void* jstring = GetJavaString(_pEnv, (string)val);
                                                                            void*  pField;
                                                                            if(isStatic) 
                                                                            { 
                                                                                void* _pClass = IntPtr.Zero.ToPointer();
                                                                                if(getClass(_pEnv, _pObj, ref _pClass) == 0)
                                                                                    if(GetStaticFieldID( _pEnv, _pClass, name, returnSignature, &pField ) == 0)
                                                                                        SetStaticObjectField( _pEnv, _pClass, pField, jstring);
                                                                                    else
                                                                                        throw new Exception(GetException(_pEnv));
                                                                                else
                                                                                    throw new Exception(GetException(_pEnv));
                                                                            } 
                                                                            else  
                                                                            { 
                                                                                if(GetFieldID( _pEnv, _pObj, name, returnSignature, &pField ) == 0)
                                                                                    SetObjectField( _pEnv, _pObj, pField, jstring);
                                                                                else
                                                                                    throw new Exception(GetException(_pEnv));
                                                                            }
                                                                        }
                                                                        else
                                                                            throw new Exception(GetException(_pEnv));

                                                                        DetacheThread((void*)JVMPtr);
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
                                                                            void* _pNetBridgeClass;
                                                                            if(FindClass( _pEnv, "app/quant/clr/CLRRuntime", &_pNetBridgeClass) != 0 ) throw new Exception ("Find Class");
                                                                        
                                                                            void* _pObj = GetJVMObject(_pEnv, _pNetBridgeClass, hashID);
                                                                            if(_pObj != IntPtr.Zero.ToPointer())
                                                                            {
                                                                                void*  pField;
                                                                                if(isStatic) 
                                                                                { 
                                                                                    void* pObjResult;
                                                                                    void* _pClass = IntPtr.Zero.ToPointer();
                                                                                    if(getClass(_pEnv, _pObj,  ref _pClass) == 0)
                                                                                        if(GetStaticFieldID( _pEnv, _pClass, name, returnSignature, &pField ) == 0)
                                                                                            if(GetStaticObjectField( _pEnv, _pClass, pField, &pObjResult) == 0)
                                                                                            {
                                                                                                void*  pNetBridgeClass;
                                                                                                if(FindClass(_pEnv, "app/quant/clr/CLRRuntime", &pNetBridgeClass) != 0) throw new Exception ("getJavaArray Find class error");

                                                                                                // int _hashID = GetID(_pEnv, pObjResult);
                                                                                                // int _arr_len = getArrayLength(_pEnv, new JVMObject(_hashID, returnSignature, true)); //IMPORTANT TRUE
                                                                                                int _arr_len = getArrayLength(_pEnv, pObjResult); //TESTING
                                                                                                
                                                                                                var _ret = getJavaArray(_pEnv, pNetBridgeClass, _arr_len, pObjResult, returnSignature);
                                                                                                DetacheThread((void*)JVMPtr);
                                                                                                return _ret;
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
                                                                                            void*  pNetBridgeClass;
                                                                                            if(FindClass(_pEnv, "app/quant/clr/CLRRuntime", &pNetBridgeClass) != 0) throw new Exception ("getJavaArray Find class error");

                                                                                            // int hashID = GetID(_pEnv, pObjResult);
                                                                                            // int _arr_len = getArrayLength(_pEnv, new JVMObject(hashID, returnSignature, true)); //IMPORTANT TRUE
                                                                                            int _arr_len = getArrayLength(_pEnv, pObjResult); //IMPORTANT TRUE
                                                                                            var _ret = getJavaArray(_pEnv, pNetBridgeClass, _arr_len, pObjResult, returnSignature);
                                                                                            
                                                                                            DetacheThread((void*)JVMPtr);
                                                                                            return _ret;
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
                                                                            
                                                                            string typename = val.GetType().ToString();
                                                                            void*  _pEnv;
                                                                            if(AttacheThread((void*)JVMPtr,&_pEnv) != 0) throw new Exception ("Attach to thread error");
                                                                            void* _pNetBridgeClass;
                                                                            if(FindClass( _pEnv, "app/quant/clr/CLRRuntime", &_pNetBridgeClass) != 0 ) throw new Exception ("Find Class");
                                                                        
                                                                            void* _pObj = GetJVMObject(_pEnv, _pNetBridgeClass, hashID);
                                                                            if(_pObj != IntPtr.Zero.ToPointer())
                                                                            {
                                                                                void*  pField;
                                                                                // void*  _pNetBridgeClass;
                                                                                // if(FindClass(_pEnv, "app/quant/clr/CLRRuntime", &_pNetBridgeClass) != 0) throw new Exception ("getJavaArray Find class error");

                                                                                switch(typename)
                                                                                {
                                                                                    case "System.Boolean[]":
                                                                                        JVMObject vobj_bool = getJavaArray(_pEnv, _pNetBridgeClass, (bool[])val);
                                                                                        void* _vobj_bool = GetJVMObject(_pEnv, _pNetBridgeClass, vobj_bool.JavaHashCode);
                                                                                        if(isStatic) 
                                                                                        { 
                                                                                            void* _pClass = IntPtr.Zero.ToPointer();
                                                                                            if(getClass(_pEnv, _pObj,  ref _pClass) == 0)
                                                                                                if(GetStaticFieldID( _pEnv, _pClass, name, returnSignature, &pField ) == 0)
                                                                                                    // SetStaticObjectField( _pEnv, _pClass, pField, vobj_bool.Pointer.ToPointer());
                                                                                                    SetStaticObjectField( _pEnv, _pClass, pField, _vobj_bool);
                                                                                                else
                                                                                                    throw new Exception("Runtime Static Field not found: " + name);
                                                                                            else
                                                                                                throw new Exception("Runtime Class not found: " + name);
                                                                                        } 
                                                                                        else  
                                                                                        { 
                                                                                            if(GetFieldID( _pEnv, _pObj, name, returnSignature, &pField ) == 0)
                                                                                                // SetObjectField( _pEnv, _pObj, pField, vobj_bool.Pointer.ToPointer());
                                                                                                SetObjectField( _pEnv, _pObj, pField, _vobj_bool);
                                                                                            else
                                                                                                throw new Exception("Runtime Field not found: " + name);
                                                                                        }
                                                                                        // if(__DB.ContainsKey(vobj_bool.JavaHashCode))
                                                                                        // {
                                                                                        //     object _out;
                                                                                        //     __DB.TryRemove(vobj_bool.JavaHashCode, out _out);
                                                                                        // }
                                                                                        DetacheThread((void*)JVMPtr);
                                                                                        break;

                                                                                    case "System.Byte[]":
                                                                                        JVMObject vobj_byte = getJavaArray(_pEnv, _pNetBridgeClass, (byte[])val);
                                                                                        void* _vobj_byte = GetJVMObject(_pEnv, _pNetBridgeClass, vobj_byte.JavaHashCode);
                                                                                        if(isStatic) 
                                                                                        { 
                                                                                            void* _pClass = IntPtr.Zero.ToPointer();
                                                                                            if(getClass(_pEnv, _pObj,  ref _pClass) == 0)
                                                                                                if(GetStaticFieldID( _pEnv, _pClass, name, returnSignature, &pField ) == 0)
                                                                                                    // SetStaticObjectField( _pEnv, _pClass, pField, vobj_byte.Pointer.ToPointer());
                                                                                                    SetStaticObjectField( _pEnv, _pClass, pField, _vobj_byte);
                                                                                                else
                                                                                                    throw new Exception("Runtime Static Field not found: " + name);
                                                                                            else
                                                                                                throw new Exception("Runtime Class not found: " + name);
                                                                                        } 
                                                                                        else  
                                                                                        { 
                                                                                            if(GetFieldID( _pEnv, _pObj, name, returnSignature, &pField ) == 0)
                                                                                                // SetObjectField( _pEnv, _pObj, pField, vobj_byte.Pointer.ToPointer());
                                                                                                SetObjectField( _pEnv, _pObj, pField, _vobj_byte);
                                                                                            else
                                                                                                throw new Exception("Runtime Field not found: " + name);
                                                                                        }
                                                                                        // if(__DB.ContainsKey(vobj_byte.JavaHashCode))
                                                                                        // {
                                                                                        //     object _out;
                                                                                        //     __DB.TryRemove(vobj_byte.JavaHashCode, out _out);
                                                                                        // }
                                                                                        DetacheThread((void*)JVMPtr);
                                                                                        break;
                                                                                        
                                                                                    case "System.Char[]":
                                                                                        JVMObject vobj_char = getJavaArray(_pEnv, _pNetBridgeClass, (char[])val);
                                                                                        void* _vobj_char = GetJVMObject(_pEnv, _pNetBridgeClass, vobj_char.JavaHashCode);
                                                                                        if(isStatic) 
                                                                                        { 
                                                                                            void* _pClass = IntPtr.Zero.ToPointer();
                                                                                            if(getClass(_pEnv, _pObj,  ref _pClass) == 0)
                                                                                                if(GetStaticFieldID( _pEnv, _pClass, name, returnSignature, &pField ) == 0)
                                                                                                    // SetStaticObjectField( _pEnv, _pClass, pField, vobj_char.Pointer.ToPointer());
                                                                                                    SetStaticObjectField( _pEnv, _pClass, pField, _vobj_char);
                                                                                                else
                                                                                                    throw new Exception("Runtime Static Field not found: " + name);
                                                                                            else
                                                                                                throw new Exception("Runtime Class not found: " + name);
                                                                                        } 
                                                                                        else  
                                                                                        { 
                                                                                            if(GetFieldID( _pEnv, _pObj, name, returnSignature, &pField ) == 0)
                                                                                                // SetObjectField( _pEnv, _pObj, pField, vobj_char.Pointer.ToPointer());
                                                                                                SetObjectField( _pEnv, _pObj, pField, _vobj_char);
                                                                                            else
                                                                                                throw new Exception("Runtime Field not found: " + name);
                                                                                        }
                                                                                        // if(__DB.ContainsKey(vobj_char.JavaHashCode))
                                                                                        // {
                                                                                        //     object _out;
                                                                                        //     __DB.TryRemove(vobj_char.JavaHashCode, out _out);
                                                                                        // }
                                                                                        DetacheThread((void*)JVMPtr);

                                                                                        break;

                                                                                    case "System.Short[]":
                                                                                        
                                                                                        JVMObject vobj_short = getJavaArray(_pEnv, _pNetBridgeClass, (short[])val);
                                                                                        void* _vobj_short = GetJVMObject(_pEnv, _pNetBridgeClass, vobj_short.JavaHashCode);
                                                                                        if(isStatic) 
                                                                                        { 
                                                                                            void* _pClass = IntPtr.Zero.ToPointer();
                                                                                            if(getClass(_pEnv, _pObj,  ref _pClass) == 0)
                                                                                                if(GetStaticFieldID( _pEnv, _pClass, name, returnSignature, &pField ) == 0)
                                                                                                    // SetStaticObjectField( _pEnv, _pClass, pField, vobj_short.Pointer.ToPointer());
                                                                                                    SetStaticObjectField( _pEnv, _pClass, pField, _vobj_short);
                                                                                                else
                                                                                                    throw new Exception("Runtime Static Field not found: " + name);
                                                                                            else
                                                                                                throw new Exception("Runtime Class not found: " + name);
                                                                                        } 
                                                                                        else  
                                                                                        { 
                                                                                            if(GetFieldID( _pEnv, _pObj, name, returnSignature, &pField ) == 0)
                                                                                                // SetObjectField( _pEnv, _pObj, pField, vobj_short.Pointer.ToPointer());
                                                                                                SetObjectField( _pEnv, _pObj, pField, _vobj_short);
                                                                                            else
                                                                                                throw new Exception("Runtime Field not found: " + name);
                                                                                        }
                                                                                        // if(__DB.ContainsKey(vobj_short.JavaHashCode))
                                                                                        // {
                                                                                        //     object _out;
                                                                                        //     __DB.TryRemove(vobj_short.JavaHashCode, out _out);
                                                                                        // }
                                                                                        DetacheThread((void*)JVMPtr);
                                                                                        break;

                                                                                    case "System.Int32[]":
                                                                                        JVMObject vobj_int = getJavaArray(_pEnv, _pNetBridgeClass, (int[])val);
                                                                                        void* _vobj_int = GetJVMObject(_pEnv, _pNetBridgeClass, vobj_int.JavaHashCode);

                                                                                        if(isStatic) 
                                                                                        { 
                                                                                            void* _pClass = IntPtr.Zero.ToPointer();
                                                                                            if(getClass(_pEnv, _pObj,  ref _pClass) == 0)
                                                                                                if(GetStaticFieldID( _pEnv, _pClass, name, returnSignature, &pField ) == 0)
                                                                                                    // SetStaticObjectField( _pEnv, _pClass, pField, vobj_int.Pointer.ToPointer());
                                                                                                    SetStaticObjectField( _pEnv, _pClass, pField, _vobj_int);
                                                                                                else
                                                                                                    throw new Exception("Runtime Static Field not found: " + name);
                                                                                            else
                                                                                                throw new Exception("Runtime Class not found: " + name);
                                                                                        } 
                                                                                        else  
                                                                                        { 
                                                                                            if(GetFieldID( _pEnv, _pObj, name, returnSignature, &pField ) == 0)
                                                                                                // SetObjectField( _pEnv, _pObj, pField, vobj_int.Pointer.ToPointer());
                                                                                                SetObjectField( _pEnv, _pObj, pField, _vobj_int);
                                                                                            else
                                                                                                throw new Exception("Runtime Field not found: " + name);
                                                                                        }
                                                                                        // if(__DB.ContainsKey(vobj_int.JavaHashCode))
                                                                                        // {
                                                                                        //     object _out;
                                                                                        //     __DB.TryRemove(vobj_int.JavaHashCode, out _out);
                                                                                        // }
                                                                                        DetacheThread((void*)JVMPtr);
                                                                                        break;

                                                                                    case "System.Int64[]":
                                                                                        JVMObject vobj_long = getJavaArray(_pEnv, _pNetBridgeClass, (long[])val);
                                                                                        void* _vobj_long = GetJVMObject(_pEnv, _pNetBridgeClass, vobj_long.JavaHashCode);

                                                                                        if(isStatic) 
                                                                                        { 
                                                                                            void* _pClass = IntPtr.Zero.ToPointer();
                                                                                            if(getClass(_pEnv, _pObj,  ref _pClass) == 0)
                                                                                                if(GetStaticFieldID( _pEnv, _pClass, name, returnSignature, &pField ) == 0)
                                                                                                    // SetStaticObjectField( _pEnv, _pClass, pField, vobj_long.Pointer.ToPointer());
                                                                                                    SetStaticObjectField( _pEnv, _pClass, pField, _vobj_long);
                                                                                                else
                                                                                                    throw new Exception("Runtime Static Field not found: " + name);
                                                                                            else
                                                                                                throw new Exception("Runtime Class not found: " + name);
                                                                                        } 
                                                                                        else  
                                                                                        { 
                                                                                            if(GetFieldID( _pEnv, _pObj, name, returnSignature, &pField ) == 0)
                                                                                                SetObjectField( _pEnv, _pObj, pField, _vobj_long);
                                                                                            else
                                                                                                throw new Exception("Runtime Field not found: " + name);
                                                                                        }
                                                                                        // if(__DB.ContainsKey(vobj_long.JavaHashCode))
                                                                                        // {
                                                                                        //     object _out;
                                                                                        //     __DB.TryRemove(vobj_long.JavaHashCode, out _out);
                                                                                        // }
                                                                                        DetacheThread((void*)JVMPtr);
                                                                                        break;

                                                                                    case "System.Float[]":
                                                                                        JVMObject vobj_float = getJavaArray(_pEnv, _pNetBridgeClass, (float[])val);
                                                                                        void* _vobj_float = GetJVMObject(_pEnv, _pNetBridgeClass, vobj_float.JavaHashCode);

                                                                                        if(isStatic) 
                                                                                        { 
                                                                                            void* _pClass = IntPtr.Zero.ToPointer();
                                                                                            if(getClass(_pEnv, _pObj,  ref _pClass) == 0)
                                                                                                if(GetStaticFieldID( _pEnv, _pClass, name, returnSignature, &pField ) == 0)
                                                                                                    // SetStaticObjectField( _pEnv, _pClass, pField, vobj_float.Pointer.ToPointer());
                                                                                                    SetStaticObjectField( _pEnv, _pClass, pField, _vobj_float);
                                                                                                else
                                                                                                    throw new Exception("Runtime Static Field not found: " + name);
                                                                                            else
                                                                                                throw new Exception("Runtime Class not found: " + name);
                                                                                        } 
                                                                                        else  
                                                                                        { 
                                                                                            if(GetFieldID( _pEnv, _pObj, name, returnSignature, &pField ) == 0)
                                                                                                // SetObjectField( _pEnv, _pObj, pField, vobj_float.Pointer.ToPointer());
                                                                                                SetObjectField( _pEnv, _pObj, pField, _vobj_float);
                                                                                            else
                                                                                                throw new Exception("Runtime Field not found: " + name);
                                                                                        }
                                                                                        // if(__DB.ContainsKey(vobj_float.JavaHashCode))
                                                                                        // {
                                                                                        //     object _out;
                                                                                        //     __DB.TryRemove(vobj_float.JavaHashCode, out _out);
                                                                                        // }
                                                                                        DetacheThread((void*)JVMPtr);
                                                                                        break;


                                                                                    case "System.Double[]":
                                                                                        JVMObject vobj_double = getJavaArray(_pEnv, _pNetBridgeClass, (double[])val);
                                                                                        void* _vobj_double = GetJVMObject(_pEnv, _pNetBridgeClass, vobj_double.JavaHashCode);

                                                                                        if(isStatic) 
                                                                                        { 
                                                                                            void* _pClass = IntPtr.Zero.ToPointer();
                                                                                            if(getClass(_pEnv, _pObj,  ref _pClass) == 0)
                                                                                                if(GetStaticFieldID( _pEnv, _pClass, name, returnSignature, &pField ) == 0)
                                                                                                    // SetStaticObjectField( _pEnv, _pClass, pField, vobj_double.Pointer.ToPointer());
                                                                                                    SetStaticObjectField( _pEnv, _pClass, pField, _vobj_double);
                                                                                                else
                                                                                                    throw new Exception("Runtime Static Field not found: " + name);
                                                                                            else
                                                                                                throw new Exception("Runtime Class not found: " + name);
                                                                                        } 
                                                                                        else  
                                                                                        { 
                                                                                            if(GetFieldID( _pEnv, _pObj, name, returnSignature, &pField ) == 0)
                                                                                                // SetObjectField( _pEnv, _pObj, pField, vobj_double.Pointer.ToPointer());
                                                                                                SetObjectField( _pEnv, _pObj, pField, _vobj_double);
                                                                                            else
                                                                                                throw new Exception("Runtime Field not found: " + name);
                                                                                        }
                                                                                        // if(__DB.ContainsKey(vobj_double.JavaHashCode))
                                                                                        // {
                                                                                        //     object _out;
                                                                                        //     __DB.TryRemove(vobj_double.JavaHashCode, out _out);
                                                                                        // }
                                                                                        DetacheThread((void*)JVMPtr);
                                                                                        break;

                                                                                    default:
                                                                                        JVMObject vobj_obj = getJavaArray(_pEnv, _pNetBridgeClass, (object[])val);
                                                                                        void* _vobj_obj = GetJVMObject(_pEnv, _pNetBridgeClass, vobj_obj.JavaHashCode);
                                                                                        if(isStatic) 
                                                                                        { 
                                                                                            void* _pClass = IntPtr.Zero.ToPointer();
                                                                                            if(getClass(_pEnv, _pObj,  ref _pClass) == 0)
                                                                                                if(GetStaticFieldID( _pEnv, _pClass, name, returnSignature, &pField ) == 0)
                                                                                                    // SetStaticObjectField( _pEnv, _pClass, pField, vobj_obj.Pointer.ToPointer());
                                                                                                    SetStaticObjectField( _pEnv, _pClass, pField, _vobj_obj);
                                                                                                else
                                                                                                    throw new Exception("Runtime Static Field not found: " + name);
                                                                                            else
                                                                                                throw new Exception("Runtime Class not found: " + name);

                                                                                        } 
                                                                                        else  
                                                                                        { 
                                                                                            if(GetFieldID( _pEnv, _pObj, name, returnSignature, &pField ) == 0)
                                                                                                // SetObjectField( _pEnv, _pObj, pField, vobj_obj.Pointer.ToPointer());
                                                                                                SetObjectField( _pEnv, _pObj, pField, _vobj_obj);
                                                                                            else
                                                                                                throw new Exception("Runtime Field not found: " + name);
                                                                                        }
                                                                                        // if(__DB.ContainsKey(vobj_obj.JavaHashCode))
                                                                                        // {
                                                                                        //     object _out;
                                                                                        //     __DB.TryRemove(vobj_obj.JavaHashCode, out _out);
                                                                                        // }
                                                                                        DetacheThread((void*)JVMPtr);
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
                                                                            void* _pNetBridgeClass;
                                                                            if(FindClass( _pEnv, "app/quant/clr/CLRRuntime", &_pNetBridgeClass) != 0 ) throw new Exception ("Find Class");
                                                                        
                                                                            void* _pObj = GetJVMObject(_pEnv, _pNetBridgeClass, hashID);
                                                                            if(_pObj != IntPtr.Zero.ToPointer())
                                                                            {
                                                                                void*  pField;
                                                                                if(isStatic) 
                                                                                { 
                                                                                    void* pObjResult;
                                                                                    void* _pClass = IntPtr.Zero.ToPointer();
                                                                                    if(getClass(_pEnv, _pObj,  ref _pClass) == 0)
                                                                                        if(GetStaticFieldID( _pEnv, _pClass, name, returnSignature, &pField ) == 0)
                                                                                            if(GetStaticObjectField( _pEnv, _pClass, pField, &pObjResult) == 0)
                                                                                            {
                                                                                                IntPtr returnPtr = new IntPtr(pObjResult);

                                                                                                int hashID_res = GetJVMID(_pEnv, pObjResult, true);

                                                                                                if(JVMObject.DB.ContainsKey(hashID_res))
                                                                                                {
                                                                                                    DetacheThread((void*)JVMPtr);
                                                                                                    // return JVMObject.DB[hashID_res];
                                                                                                    return JVMObject.DB[hashID_res].Target;
                                                                                                }

                                                                                                else if(DB.ContainsKey(hashID_res))
                                                                                                {
                                                                                                    DetacheThread((void*)JVMPtr);
                                                                                                    return (JVMObject)DB[hashID_res].Target;
                                                                                                }
                                                                                                else
                                                                                                {
                                                                                                    string cls = returnSignature.StartsWith("L") && returnSignature.EndsWith(";") ? returnSignature.Substring(1).Replace(";","").Replace("/",".") : returnSignature;

                                                                                                    var _ret =  getObject(_pEnv, cls, pObjResult);
                                                                                                    DetacheThread((void*)JVMPtr);
                                                                                                    return _ret;
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

                                                                                            int hashID_res = GetJVMID(_pEnv, pObjResult, true);

                                                                                            if(JVMObject.DB.ContainsKey(hashID_res))
                                                                                            {
                                                                                                // var _ret = JVMObject.DB[hashID_res];
                                                                                                var _ret = JVMObject.DB[hashID_res].Target;
                                                                                                DetacheThread((void*)JVMPtr);
                                                                                                return _ret;
                                                                                            }

                                                                                            else if(DB.ContainsKey(hashID_res))
                                                                                            {
                                                                                                var _ret = (JVMObject)DB[hashID_res].Target;
                                                                                                DetacheThread((void*)JVMPtr);
                                                                                                return _ret;
                                                                                            }
                                                                                            else
                                                                                            {
                                                                                                string cls = returnSignature.StartsWith("L") && returnSignature.EndsWith(";")? returnSignature.Substring(1).Replace(";","") : returnSignature;
                                                                                                var _ret = CreateInstancePtr(_pEnv, cls, null, returnPtr, null );
                                                                                                // GetID(_ret, true); NOT SURE
                                                                                                DetacheThread((void*)JVMPtr);
                                                                                                return _ret;
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
                                                                            void* _pNetBridgeClass;
                                                                            if(FindClass( _pEnv, "app/quant/clr/CLRRuntime", &_pNetBridgeClass) != 0 ) throw new Exception ("Find Class");
                                                                        
                                                                            void* _pObj = GetJVMObject(_pEnv, _pNetBridgeClass, hashID);
                                                                            if(_pObj != IntPtr.Zero.ToPointer())
                                                                            {
                                                                                JVMObject vobj = (JVMObject)val;
                                                                                void* _vobj = GetJVMObject(_pEnv, _pNetBridgeClass, vobj.JavaHashCode);
                                                                                void*  pField;
                                                                                if(isStatic) 
                                                                                { 
                                                                                    void* _pClass = IntPtr.Zero.ToPointer();
                                                                                    if(getClass(_pEnv, _pObj,  ref _pClass) == 0)
                                                                                        if(GetStaticFieldID( _pEnv, _pClass, name, returnSignature, &pField ) == 0)
                                                                                            // SetStaticObjectField( _pEnv, _pClass, pField, vobj.Pointer.ToPointer());
                                                                                            SetStaticObjectField( _pEnv, _pClass, pField, _vobj);
                                                                                        else
                                                                                            throw new Exception("Runtime Static Field not found: " + name);
                                                                                    else
                                                                                        throw new Exception("Runtime Class not found: " + name);
                                                                                } 
                                                                                else  
                                                                                { 
                                                                                    if(GetFieldID( _pEnv, _pObj, name, returnSignature, &pField ) == 0)
                                                                                        // SetObjectField( _pEnv, _pObj, pField, vobj.Pointer.ToPointer());
                                                                                        SetObjectField( _pEnv, _pObj, pField, _vobj);
                                                                                    else
                                                                                        throw new Exception("Runtime Field not found: " + name);
                                                                                }
                                                                            }
                                                                            else
                                                                                throw new Exception("Runtime Object not found: " + name);

                                                                            DetacheThread((void*)JVMPtr);
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

                                                                void* _pNetBridgeClass;
                                                                if(FindClass( _pEnv, "app/quant/clr/CLRRuntime", &_pNetBridgeClass) != 0 ) throw new Exception ("Find Class");
                                                                
                                                                int call_len = call_args == null ? 0 : call_args.Length;
                                                                // void** ar_call = stackalloc void*[call_len];
                                                                // getJavaParameters(_pEnv, ref ar_call, call_args);
                                                                void** ar_call = (void**)(new StructWrapper(_pEnv, call_args)).Ptr;

                                                                void* _pObj = GetJVMObject(_pEnv, _pNetBridgeClass, hashID);
                                                                if(_pObj != IntPtr.Zero.ToPointer())
                                                                {
                                                                    void* pMethod;
                                                                    if(isStatic) 
                                                                    { 
                                                                        void* _pClass = IntPtr.Zero.ToPointer();
                                                                        if(getClass(_pEnv, _pObj,  ref _pClass) == 0)
                                                                            if(GetStaticMethodID( _pEnv, _pClass, name, "(" + preArgsSignature + ")" + returnSignature, &pMethod ) == 0)
                                                                            {
                                                                                bool _res;
                                                                                if(CallStaticBooleanMethod( _pEnv, _pClass, pMethod, call_len, ar_call, &_res) != 0)
                                                                                    throw new Exception(GetException(_pEnv));
                                                                                
                                                                                DetacheThread((void*)JVMPtr);
                                                                                return _res;
                                                                            }
                                                                            else
                                                                                throw new Exception(GetException(_pEnv));
                                                                        else
                                                                            throw new Exception(GetException(_pEnv));
                                                                    } 
                                                                    else  
                                                                    { 
                                                                        if(GetMethodID( _pEnv, _pObj, name, "(" + preArgsSignature + ")" + returnSignature, &pMethod ) == 0)
                                                                        {
                                                                            bool _res;
                                                                            if(CallBooleanMethod( _pEnv, _pObj, pMethod, call_len, ar_call, &_res) != 0)
                                                                                throw new Exception(GetException(_pEnv));

                                                                            DetacheThread((void*)JVMPtr);
                                                                            return _res;
                                                                        }
                                                                        else
                                                                            throw new Exception(GetException(_pEnv));
                                                                    }
                                                                }
                                                                else
                                                                    throw new Exception(GetException(_pEnv));
                                                            }));
                                                            break;
                                                        case "B": //Byte
                                                            expandoObject.TrySetMember(name + argsSignature, (wrapFunction<byte>)((call_args) => {
                                                                
                                                                void*  _pEnv;
                                                                if(AttacheThread((void*)JVMPtr,&_pEnv) != 0) throw new Exception ("Attach to thread error");
                                                                void* _pNetBridgeClass;
                                                                if(FindClass( _pEnv, "app/quant/clr/CLRRuntime", &_pNetBridgeClass) != 0 ) throw new Exception ("Find Class");
                                                                

                                                                int call_len = call_args == null ? 0 : call_args.Length;
                                                                // void** ar_call = stackalloc void*[call_len];
                                                                // getJavaParameters(_pEnv, ref ar_call, call_args);
                                                                void** ar_call = (void**)(new StructWrapper(_pEnv, call_args)).Ptr;
                                                                
                                                                void* _pObj = GetJVMObject(_pEnv, _pNetBridgeClass, hashID);
                                                                if(_pObj != IntPtr.Zero.ToPointer())
                                                                {
                                                                    void* pMethod;
                                                                    if(isStatic) 
                                                                    { 
                                                                        void* _pClass = IntPtr.Zero.ToPointer();
                                                                        if(getClass(_pEnv, _pObj,  ref _pClass) == 0)
                                                                            if(GetStaticMethodID( _pEnv, _pClass, name, "(" + preArgsSignature + ")" + returnSignature, &pMethod ) == 0)
                                                                            {
                                                                                byte _res;
                                                                                if(CallStaticByteMethod( _pEnv, _pClass, pMethod, call_len, ar_call, &_res) != 0)
                                                                                    throw new Exception(GetException(_pEnv));

                                                                                DetacheThread((void*)JVMPtr);
                                                                                return _res;
                                                                            }
                                                                            else
                                                                                throw new Exception(GetException(_pEnv));
                                                                        else
                                                                            throw new Exception(GetException(_pEnv));
                                                                    } 
                                                                    else  
                                                                    { 
                                                                        if(GetMethodID( _pEnv, _pObj, name, "(" + preArgsSignature + ")" + returnSignature, &pMethod ) == 0)
                                                                        {
                                                                            byte _res;
                                                                            if(CallByteMethod( _pEnv, _pObj, pMethod, call_len, ar_call, &_res) != 0)
                                                                                throw new Exception(GetException(_pEnv));

                                                                            DetacheThread((void*)JVMPtr);
                                                                            return _res;
                                                                        }
                                                                        else
                                                                            throw new Exception(GetException(_pEnv));
                                                                    }
                                                                }
                                                                else
                                                                    throw new Exception(GetException(_pEnv));
                                                            }));
                                                            break;
                                                    
                                                        case "C": //Char
                                                            
                                                            expandoObject.TrySetMember(name + argsSignature, (wrapFunction<char>)((call_args) => {
                                                                
                                                                void*  _pEnv;
                                                                if(AttacheThread((void*)JVMPtr,&_pEnv) != 0) throw new Exception ("Attach to thread error");
                                                                void* _pNetBridgeClass;
                                                                if(FindClass( _pEnv, "app/quant/clr/CLRRuntime", &_pNetBridgeClass) != 0 ) throw new Exception ("Find Class");

                                                                int call_len = call_args == null ? 0 : call_args.Length;
                                                                // void** ar_call = stackalloc void*[call_len];
                                                                // getJavaParameters(_pEnv, ref ar_call, call_args);
                                                                void** ar_call = (void**)(new StructWrapper(_pEnv, call_args)).Ptr;
                                                                
                                                                void* _pObj = GetJVMObject(_pEnv, _pNetBridgeClass, hashID);
                                                                if(_pObj != IntPtr.Zero.ToPointer())
                                                                {
                                                                    void* pMethod;
                                                                    if(isStatic) 
                                                                    { 
                                                                        void* _pClass = IntPtr.Zero.ToPointer();
                                                                        if(getClass(_pEnv, _pObj,  ref _pClass) == 0)
                                                                            if(GetStaticMethodID( _pEnv, _pClass, name, "(" + preArgsSignature + ")" + returnSignature, &pMethod ) == 0)
                                                                            {
                                                                                char _res;
                                                                                if(CallStaticCharMethod( _pEnv, _pClass, pMethod, call_len, ar_call, &_res) != 0)
                                                                                    throw new Exception(GetException(_pEnv));

                                                                                DetacheThread((void*)JVMPtr);
                                                                                return _res;
                                                                            }
                                                                            else
                                                                                throw new Exception(GetException(_pEnv));
                                                                        else
                                                                            throw new Exception(GetException(_pEnv));
                                                                    } 
                                                                    else  
                                                                    { 
                                                                        if(GetMethodID( _pEnv, _pObj, name, "(" + preArgsSignature + ")" + returnSignature, &pMethod ) == 0)
                                                                        {
                                                                            char _res;
                                                                            if(CallCharMethod( _pEnv, _pObj, pMethod, call_len, ar_call, &_res) != 0)
                                                                                throw new Exception(GetException(_pEnv));

                                                                            DetacheThread((void*)JVMPtr);
                                                                            return _res;
                                                                        }
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
                                                                void* _pNetBridgeClass;
                                                                if(FindClass( _pEnv, "app/quant/clr/CLRRuntime", &_pNetBridgeClass) != 0 ) throw new Exception ("Find Class");
                                                                

                                                                int call_len = call_args == null ? 0 : call_args.Length;
                                                                // void** ar_call = stackalloc void*[call_len];
                                                                // getJavaParameters(_pEnv, ref ar_call, call_args);
                                                                void** ar_call = (void**)(new StructWrapper(_pEnv, call_args)).Ptr;
                                                                
                                                                void* _pObj = GetJVMObject(_pEnv, _pNetBridgeClass, hashID);
                                                                if(_pObj != IntPtr.Zero.ToPointer())
                                                                {
                                                                    void* pMethod;
                                                                    if(isStatic) 
                                                                    { 
                                                                        void* _pClass = IntPtr.Zero.ToPointer();
                                                                        if(getClass(_pEnv, _pObj,  ref _pClass) == 0)
                                                                            if(GetStaticMethodID( _pEnv, _pClass, name, "(" + preArgsSignature + ")" + returnSignature, &pMethod ) == 0)
                                                                            {
                                                                                short _res;
                                                                                if(CallStaticShortMethod( _pEnv, _pClass, pMethod, call_len, ar_call, &_res) != 0)
                                                                                    throw new Exception(GetException(_pEnv));

                                                                                DetacheThread((void*)JVMPtr);
                                                                                return _res;
                                                                            }
                                                                            else
                                                                                throw new Exception(GetException(_pEnv));
                                                                        else
                                                                            throw new Exception(GetException(_pEnv));
                                                                    } 
                                                                    else  
                                                                    { 
                                                                        if(GetMethodID( _pEnv, _pObj, name, "(" + preArgsSignature + ")" + returnSignature, &pMethod ) == 0)
                                                                        {
                                                                            short _res;
                                                                            if(CallShortMethod( _pEnv, _pObj, pMethod, call_len, ar_call, &_res) != 0)
                                                                                throw new Exception(GetException(_pEnv));

                                                                            DetacheThread((void*)JVMPtr);
                                                                            return _res;
                                                                        }
                                                                        else
                                                                            throw new Exception(GetException(_pEnv));
                                                                    }
                                                                }
                                                                else
                                                                    throw new Exception(GetException(_pEnv));
                                                            }));
                                                            break;
                                                        
                                                        case "I": //Int
                                                            expandoObject.TrySetMember(name + argsSignature, (wrapFunction<int>)((call_args) => {
                                                                void*  _pEnv;
                                                                if(AttacheThread((void*)JVMPtr,&_pEnv) != 0) throw new Exception ("Attach to thread error");
                                                                void* _pNetBridgeClass;
                                                                if(FindClass( _pEnv, "app/quant/clr/CLRRuntime", &_pNetBridgeClass) != 0 ) throw new Exception ("Find Class");
                                                                
                                                                int call_len = call_args == null ? 0 : call_args.Length;
                                                                // void** ar_call = stackalloc void*[call_len];
                                                                // getJavaParameters(_pEnv, ref ar_call, call_args);
                                                                void** ar_call = (void**)(new StructWrapper(_pEnv, call_args)).Ptr;
                                                                
                                                                void* _pObj = GetJVMObject(_pEnv, _pNetBridgeClass, hashID);
                                                                if(_pObj != IntPtr.Zero.ToPointer())
                                                                {
                                                                    void* pMethod;
                                                                    if(isStatic) 
                                                                    { 
                                                                        void* _pClass = IntPtr.Zero.ToPointer();
                                                                        if(getClass(_pEnv, _pObj,  ref _pClass) == 0)
                                                                            if(GetStaticMethodID( _pEnv, _pClass, name, "(" + preArgsSignature + ")" + returnSignature, &pMethod ) == 0)
                                                                            {
                                                                                int _res;
                                                                                if(CallStaticIntMethod( _pEnv, _pClass, pMethod, call_len, ar_call, &_res) != 0)
                                                                                    throw new Exception(GetException(_pEnv));

                                                                                DetacheThread((void*)JVMPtr);
                                                                                return _res;
                                                                            }
                                                                            else
                                                                                throw new Exception("Runtime Static Method not found: " + name + "(" + preArgsSignature + ")" + returnSignature );
                                                                        else
                                                                            throw new Exception("Runtime Class not found: " + name + "(" + preArgsSignature + ")" + returnSignature );
                                                                    } 
                                                                    else  
                                                                    { 
                                                                        if(GetMethodID( _pEnv, _pObj, name, "(" + preArgsSignature + ")" + returnSignature, &pMethod ) == 0)
                                                                        {
                                                                            int _res;
                                                                            if(CallIntMethod( _pEnv, _pObj, pMethod, call_len, ar_call, &_res) != 0)
                                                                                throw new Exception(GetException(_pEnv));

                                                                            DetacheThread((void*)JVMPtr);
                                                                            return _res;
                                                                        }
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
                                                                void* _pNetBridgeClass;
                                                                if(FindClass( _pEnv, "app/quant/clr/CLRRuntime", &_pNetBridgeClass) != 0 ) throw new Exception ("Find Class");
                                                                

                                                                int call_len = call_args == null ? 0 : call_args.Length;
                                                                // void** ar_call = stackalloc void*[call_len];
                                                                // getJavaParameters(_pEnv, ref ar_call, call_args);
                                                                void** ar_call = (void**)(new StructWrapper(_pEnv, call_args)).Ptr;

                                                                void* _pObj = GetJVMObject(_pEnv, _pNetBridgeClass, hashID);
                                                                if(_pObj != IntPtr.Zero.ToPointer())
                                                                {
                                                                    void* pMethod;
                                                                    if(isStatic) 
                                                                    { 
                                                                        void* _pClass = IntPtr.Zero.ToPointer();
                                                                        if(getClass(_pEnv, _pObj,  ref _pClass) == 0)
                                                                            if(GetStaticMethodID( _pEnv, _pClass, name, "(" + preArgsSignature + ")" + returnSignature, &pMethod ) == 0)
                                                                            {
                                                                                long _res;
                                                                                if(CallStaticLongMethod( _pEnv, _pClass, pMethod, call_len, ar_call, &_res) != 0)
                                                                                    throw new Exception(GetException(_pEnv));

                                                                                DetacheThread((void*)JVMPtr);
                                                                                return _res;
                                                                            }
                                                                            else
                                                                                throw new Exception(GetException(_pEnv));
                                                                        else
                                                                            throw new Exception(GetException(_pEnv));
                                                                    } 
                                                                    else  
                                                                    { 
                                                                        if(GetMethodID( _pEnv, _pObj, name, "(" + preArgsSignature + ")" + returnSignature, &pMethod ) == 0)
                                                                        {
                                                                            long _res;
                                                                            if(CallLongMethod( _pEnv, _pObj, pMethod, call_len, ar_call, &_res) != 0)
                                                                                throw new Exception(GetException(_pEnv));

                                                                            DetacheThread((void*)JVMPtr);
                                                                            return _res;
                                                                        }
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
                                                                void* _pNetBridgeClass;
                                                                if(FindClass( _pEnv, "app/quant/clr/CLRRuntime", &_pNetBridgeClass) != 0 ) throw new Exception ("Find Class");
                                                                

                                                                int call_len = call_args == null ? 0 : call_args.Length;
                                                                // void** ar_call = stackalloc void*[call_len];
                                                                // getJavaParameters(_pEnv, ref ar_call, call_args);
                                                                void** ar_call = (void**)(new StructWrapper(_pEnv, call_args)).Ptr;

                                                                void* _pObj = GetJVMObject(_pEnv, _pNetBridgeClass, hashID);
                                                                if(_pObj != IntPtr.Zero.ToPointer())
                                                                {
                                                                    void* pMethod;
                                                                    if(isStatic) 
                                                                    { 
                                                                        void* _pClass = IntPtr.Zero.ToPointer();
                                                                        if(getClass(_pEnv, _pObj,  ref _pClass) == 0)
                                                                            if(GetStaticMethodID( _pEnv, _pClass, name, "(" + preArgsSignature + ")" + returnSignature, &pMethod ) == 0)
                                                                            {
                                                                                float _res;
                                                                                if(CallStaticFloatMethod( _pEnv, _pClass, pMethod, call_len, ar_call, &_res) != 0)
                                                                                    throw new Exception(GetException(_pEnv));

                                                                                DetacheThread((void*)JVMPtr);
                                                                                return _res;
                                                                            }
                                                                            else
                                                                                throw new Exception(GetException(_pEnv));
                                                                        else
                                                                            throw new Exception(GetException(_pEnv));
                                                                    } 
                                                                    else  
                                                                    { 
                                                                        if(GetMethodID( _pEnv, _pObj, name, "(" + preArgsSignature + ")" + returnSignature, &pMethod ) == 0)
                                                                        {
                                                                            float _res;
                                                                            if(CallFloatMethod( _pEnv, _pObj, pMethod, call_len, ar_call, &_res) != 0)
                                                                                throw new Exception(GetException(_pEnv));

                                                                            DetacheThread((void*)JVMPtr);
                                                                            return _res;

                                                                        }
                                                                        else
                                                                            throw new Exception(GetException(_pEnv));
                                                                    }
                                                                }
                                                                else
                                                                    throw new Exception(GetException(_pEnv));
                                                            }));
                                                            break;
                                                        
                                                        case "D": //Double
                                                            
                                                            expandoObject.TrySetMember(name + argsSignature, (wrapFunction<double>)((call_args) => {
                                                                void*  _pEnv;
                                                                if(AttacheThread((void*)JVMPtr,&_pEnv) != 0) throw new Exception ("Attach to thread error");
                                                                void* _pNetBridgeClass;
                                                                if(FindClass( _pEnv, "app/quant/clr/CLRRuntime", &_pNetBridgeClass) != 0 ) throw new Exception ("Find Class");

                                                                int call_len = call_args == null ? 0 : call_args.Length;
                                                                // void** ar_call = stackalloc void*[call_len];
                                                                // getJavaParameters(_pEnv, ref ar_call, call_args);
                                                                void** ar_call = (void**)(new StructWrapper(_pEnv, call_args)).Ptr;
                                                                
                                                                void* _pObj = GetJVMObject(_pEnv, _pNetBridgeClass, hashID);
                                                                if(_pObj != IntPtr.Zero.ToPointer())
                                                                {
                                                                    void* pMethod;
                                                                    if(isStatic) 
                                                                    { 
                                                                        void* _pClass = IntPtr.Zero.ToPointer();
                                                                        if(getClass(_pEnv, _pObj,  ref _pClass) == 0)
                                                                            if(GetStaticMethodID( _pEnv, _pClass, name, "(" + preArgsSignature + ")" + returnSignature, &pMethod ) == 0)
                                                                            {
                                                                                double _res;
                                                                                if(CallStaticDoubleMethod( _pEnv, _pClass, pMethod, call_len, ar_call, &_res) != 0)
                                                                                    throw new Exception(GetException(_pEnv));

                                                                                DetacheThread((void*)JVMPtr);
                                                                                return _res;
                                                                            }
                                                                            else
                                                                                throw new Exception("Runtime Static Method not found: " + name + "(" + preArgsSignature + ")" + returnSignature );
                                                                        else
                                                                            throw new Exception("Runtime Class not found: " + name + "(" + preArgsSignature + ")" + returnSignature );
                                                                    } 
                                                                    else  
                                                                    { 
                                                                        if(GetMethodID( _pEnv, _pObj, name, "(" + preArgsSignature + ")" + returnSignature, &pMethod ) == 0)
                                                                        {
                                                                            double _res;
                                                                            if(CallDoubleMethod( _pEnv, _pObj, pMethod, call_len, ar_call, &_res) != 0)
                                                                                throw new Exception(GetException(_pEnv));

                                                                            DetacheThread((void*)JVMPtr);
                                                                            return _res;
                                                                        }
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
                                                                void* _pNetBridgeClass;
                                                                if(FindClass( _pEnv, "app/quant/clr/CLRRuntime", &_pNetBridgeClass) != 0 ) throw new Exception ("Find Class");
                                                                
                                                                int call_len = call_args == null ? 0 : call_args.Length;
                                                                // void** ar_call = stackalloc void*[call_len];
                                                                // getJavaParameters(_pEnv, ref ar_call, call_args);
                                                                void** ar_call = (void**)(new StructWrapper(_pEnv, call_args)).Ptr;
                                                                
                                                                void* _pObj = GetJVMObject(_pEnv, _pNetBridgeClass, hashID);
                                                                if(_pObj != IntPtr.Zero.ToPointer())
                                                                {
                                                                    void* pMethod;
                                                                    if(isStatic) 
                                                                    { 
                                                                        void* _pClass = IntPtr.Zero.ToPointer();
                                                                        if(getClass(_pEnv, _pObj,  ref _pClass) == 0)
                                                                        {
                                                                            if(GetStaticMethodID( _pEnv, _pClass, name, "(" + preArgsSignature + ")" + returnSignature, &pMethod ) == 0)
                                                                            {
                                                                                if(CallStaticVoidMethod( _pEnv, _pClass, pMethod, call_len, ar_call) != 0)
                                                                                    throw new Exception(GetException(_pEnv));
                                                                            }
                                                                            else
                                                                                throw new Exception(GetException(_pEnv));
                                                                        }
                                                                        else
                                                                            throw new Exception(GetException(_pEnv));
                                                                    } 
                                                                    else  
                                                                    { 
                                                                        if(GetMethodID( _pEnv, _pObj, name, "(" + preArgsSignature + ")" + returnSignature, &pMethod ) == 0)
                                                                        {
                                                                            if(CallVoidMethod( _pEnv, _pObj, pMethod, call_len, ar_call) != 0)
                                                                            throw new Exception(GetException(_pEnv));
                                                                        }
                                                                        else
                                                                            throw new Exception(GetException(_pEnv));
                                                                    }
                                                                }
                                                                else
                                                                    throw new Exception(GetException(_pEnv));

                                                                DetacheThread((void*)JVMPtr);
                                                            }));
                                                            break;
                                                        
                                                        case "Ljava/lang/String;": //String
                                                            
                                                            expandoObject.TrySetMember(name + argsSignature, (wrapFunction<string>)((call_args) => {
                                                                
                                                                void*  _pEnv;
                                                                if(AttacheThread((void*)JVMPtr,&_pEnv) != 0) throw new Exception ("Attach to thread error");
                                                                void* _pNetBridgeClass;
                                                                if(FindClass( _pEnv, "app/quant/clr/CLRRuntime", &_pNetBridgeClass) != 0 ) throw new Exception ("Find Class");
                                                                
                                                                int call_len = call_args == null ? 0 : call_args.Length;
                                                                // void** ar_call = stackalloc void*[call_len];
                                                                // getJavaParameters(_pEnv, ref ar_call, call_args);
                                                                void** ar_call = (void**)(new StructWrapper(_pEnv, call_args)).Ptr;
                                                                
                                                                void* pObjResult = IntPtr.Zero.ToPointer();
                                                                void* _pObj = GetJVMObject(_pEnv, _pNetBridgeClass, hashID);
                                                                if(_pObj != pObjResult)
                                                                {
                                                                    void* pMethod;
                                                                    if(isStatic) 
                                                                    { 
                                                                        void* _pClass = IntPtr.Zero.ToPointer();
                                                                        if(getClass(_pEnv, _pObj,  ref _pClass) == 0)
                                                                            if(GetStaticMethodID( _pEnv, _pClass, name, "(" + preArgsSignature + ")" + returnSignature, &pMethod ) == 0)
                                                                            {
                                                                                if(CallStaticObjectMethod( _pEnv, _pClass, pMethod, &pObjResult, call_len, ar_call) != 0)
                                                                                    throw new Exception(GetException(_pEnv));
                                                                            }
                                                                            else
                                                                                throw new Exception(GetException(_pEnv));
                                                                        else
                                                                            throw new Exception(GetException(_pEnv));
                                                                    } 
                                                                    else  
                                                                    { 
                                                                        if(GetMethodID( _pEnv, _pObj, name, "(" + preArgsSignature + ")" + returnSignature, &pMethod ) == 0)
                                                                        {
                                                                            if(CallObjectMethod( _pEnv, _pObj, pMethod, &pObjResult, call_len, ar_call) != 0)
                                                                                throw new Exception(GetException(_pEnv));
                                                                        }
                                                                        else
                                                                            throw new Exception(GetException(_pEnv));
                                                                    }

                                                                    if(new IntPtr(pObjResult) == IntPtr.Zero)
                                                                    {
                                                                        DetacheThread((void*)JVMPtr);
                                                                        return null;
                                                                    }

                                                                    var _ret = GetNetString(_pEnv, pObjResult);
                                                                    DetacheThread((void*)JVMPtr);
                                                                    return _ret;
                                                                }
                                                                else
                                                                    throw new Exception(GetException(_pEnv));
                                                            }));
                                                            break;

                                                        case "Ljava/time/LocalDateTime;": //String
                                                            
                                                            expandoObject.TrySetMember(name + argsSignature, (wrapFunction<DateTime>)((call_args) => {

                                                                void*  _pEnv;
                                                                if(AttacheThread((void*)JVMPtr,&_pEnv) != 0) throw new Exception ("Attach to thread error");
                                                                void* _pNetBridgeClass;
                                                                if(FindClass( _pEnv, "app/quant/clr/CLRRuntime", &_pNetBridgeClass) != 0 ) throw new Exception ("Find Class");
                                                                

                                                                int call_len = call_args == null ? 0 : call_args.Length;
                                                                // void** ar_call = stackalloc void*[call_len];
                                                                // getJavaParameters(_pEnv, ref ar_call, call_args);
                                                                void** ar_call = (void**)(new StructWrapper(_pEnv, call_args)).Ptr;
                                                                
                                                                void* pObjResult = IntPtr.Zero.ToPointer();
                                                                void* _pObj = GetJVMObject(_pEnv, _pNetBridgeClass, hashID);

                                                                if(_pObj != pObjResult)
                                                                {
                                                                    void* pMethod;
                                                                    if(isStatic) 
                                                                    { 
                                                                        void* _pClass = IntPtr.Zero.ToPointer();
                                                                        if(getClass(_pEnv, _pObj,  ref _pClass) == 0)
                                                                        {
                                                                            if(GetStaticMethodID( _pEnv, _pClass, name, "(" + preArgsSignature + ")" + returnSignature, &pMethod ) == 0)
                                                                            {
                                                                                if(CallStaticObjectMethod( _pEnv, _pClass, pMethod, &pObjResult, call_len, ar_call) != 0)
                                                                                    throw new Exception(GetException(_pEnv));
                                                                            }
                                                                            else
                                                                                throw new Exception(GetException(_pEnv));
                                                                        }
                                                                        else
                                                                            throw new Exception(GetException(_pEnv));
                                                                    } 
                                                                    else  
                                                                    { 
                                                                        if(GetMethodID( _pEnv, _pObj, name, "(" + preArgsSignature + ")" + returnSignature, &pMethod ) == 0)
                                                                        {
                                                                            if(CallObjectMethod( _pEnv, _pObj, pMethod, &pObjResult, call_len, ar_call) != 0)
                                                                                throw new Exception(GetException(_pEnv));
                                                                        }
                                                                        else
                                                                            throw new Exception(GetException(_pEnv));
                                                                    }

                                                                    if(new IntPtr(pObjResult) == IntPtr.Zero)
                                                                    {
                                                                        DetacheThread((void*)JVMPtr);
                                                                        return DateTime.MinValue;
                                                                    }

                                                                    var _ret = GetNetDateTime(_pEnv, pObjResult);
                                                                    DetacheThread((void*)JVMPtr);
                                                                    return _ret;
                                                                }
                                                                else
                                                                    throw new Exception(GetException(_pEnv));
                                                            }));
                                                            break;

                                                        default:
                                                            
                                                            if(returnSignature.StartsWith("["))
                                                            {
                                                                expandoObject.TrySetMember(name + argsSignature, (wrapFunction<object[]>)((call_args)  => {
                                                                    try
                                                                    {
                                                                        void*  _pEnv;
                                                                        if(AttacheThread((void*)JVMPtr,&_pEnv) != 0) throw new Exception ("Attach to thread error");
                                                                        void* _pNetBridgeClass;
                                                                        if(FindClass( _pEnv, "app/quant/clr/CLRRuntime", &_pNetBridgeClass) != 0 ) throw new Exception ("Find Class");
                                                                    

                                                                        int call_len = call_args == null ? 0 : call_args.Length;
                                                                        // void** ar_call = stackalloc void*[call_len];
                                                                        // getJavaParameters(_pEnv, ref ar_call, call_args);
                                                                        void** ar_call = (void**)(new StructWrapper(_pEnv, call_args)).Ptr;


                                                                        void* pObjResult = IntPtr.Zero.ToPointer();
                                                                        void* _pObj = GetJVMObject(_pEnv, _pNetBridgeClass, hashID);
                                                                        if(_pObj != pObjResult)
                                                                        {
                                                                            void* pMethod;
                                                                            if(isStatic) 
                                                                            { 
                                                                                void* _pClass = IntPtr.Zero.ToPointer();
                                                                                if(getClass(_pEnv, _pObj,  ref _pClass) == 0)
                                                                                {
                                                                                    if(GetStaticMethodID( _pEnv, _pClass, name, "(" + preArgsSignature + ")" + returnSignature, &pMethod ) == 0)
                                                                                    {
                                                                                        if(CallStaticObjectMethod( _pEnv, _pClass, pMethod, &pObjResult, call_len, ar_call) != 0)
                                                                                            throw new Exception(GetException(_pEnv));
                                                                                    }
                                                                                    else
                                                                                        throw new Exception(GetException(_pEnv));
                                                                                }
                                                                                else
                                                                                    throw new Exception(GetException(_pEnv));
                                                                            } 
                                                                            else  
                                                                            { 
                                                                                if(GetMethodID( _pEnv, _pObj, name, "(" + preArgsSignature + ")" + returnSignature, &pMethod ) == 0)
                                                                                {
                                                                                    if(CallObjectMethod( _pEnv, _pObj, pMethod, &pObjResult, call_len, ar_call) != 0)
                                                                                        throw new Exception(GetException(_pEnv));
                                                                                }
                                                                                else
                                                                                    throw new Exception(GetException(_pEnv));
                                                                            }

                                                                            IntPtr ptr = new IntPtr(pObjResult);

                                                                            if(ptr == IntPtr.Zero)
                                                                            {
                                                                                DetacheThread((void*)JVMPtr);
                                                                                return null;
                                                                            }
                                                                        
                                                                            // return getJavaArray(ptr, returnSignature);
                                                                            void*  pNetBridgeClass;
                                                                            if(FindClass( _pEnv, "app/quant/clr/CLRRuntime", &pNetBridgeClass) != 0) throw new Exception ("getJavaArray Find class error");

                                                                            // int hashID = GetID(_pEnv, pObjResult);
                                                                            // int _arr_len = getArrayLength(_pEnv, new JVMObject(hashID, returnSignature, true)); 
                                                                            int _arr_len = getArrayLength(_pEnv, pObjResult); //TESTING

                                                                            var _ret = getJavaArray(_pEnv, pNetBridgeClass, _arr_len, pObjResult, returnSignature);
                                                                            DetacheThread((void*)JVMPtr);

                                                                            // if(JVMObject.__DB.ContainsKey(hashID))
                                                                            // {

                                                                            //     JVMObject oo;
                                                                            //     JVMObject.__DB.TryRemove(hashID, out oo);
                                                                            // }

                                                                            return _ret;
                                                                        }
                                                                        else
                                                                        {
                                                                            Console.WriteLine("CLR ARRAY: _pObj != pObjResult");
                                                                            return null;
                                                                        }
                                                                    }
                                                                    catch(Exception e)
                                                                    {
                                                                        Console.WriteLine("CLR inner array invoke: " + e);
                                                                        return null;
                                                                    }
                                                                }));
                                                            }
                                                            else
                                                            {
                                                                expandoObject.TrySetMember(name + argsSignature, (wrapFunction<object>)((call_args)  => {
                                                                    try
                                                                    {
                                                                        void*  _pEnv;
                                                                        if(AttacheThread((void*)JVMPtr,&_pEnv) != 0) throw new Exception ("Attach to thread error");
                                                                        void* _pNetBridgeClass;
                                                                        if(FindClass( _pEnv, "app/quant/clr/CLRRuntime", &_pNetBridgeClass) != 0 ) throw new Exception ("Find Class");
                                                                    

                                                                        int call_len = call_args == null ? 0 : call_args.Length;
                                                                        // void** ar_call = stackalloc void*[call_len];
                                                                        // getJavaParameters(_pEnv, ref ar_call, call_args);
                                                                        void** ar_call = (void**)(new StructWrapper(_pEnv, call_args)).Ptr;

                                                                        void* pObjResult = IntPtr.Zero.ToPointer();
                                                                        void* _pObj = GetJVMObject(_pEnv, _pNetBridgeClass, hashID);
                                                                        if(_pObj != pObjResult)
                                                                        {
                                                                            void* pMethod;
                                                                            if(isStatic) 
                                                                            { 
                                                                                void* _pClass = IntPtr.Zero.ToPointer();
                                                                                if(getClass(_pEnv, _pObj,  ref _pClass) == 0)
                                                                                {
                                                                                    if(GetStaticMethodID( _pEnv, _pClass, name, "(" + preArgsSignature + ")" + returnSignature, &pMethod ) == 0)
                                                                                    {
                                                                                        if(CallStaticObjectMethod( _pEnv, _pClass, pMethod, &pObjResult, call_len, ar_call) != 0)
                                                                                            throw new Exception(GetException(_pEnv));
                                                                                    }
                                                                                    else
                                                                                        throw new Exception(GetException(_pEnv));
                                                                                }
                                                                                else
                                                                                    throw new Exception(GetException(_pEnv));
                                                                            } 
                                                                            else  
                                                                            { 
                                                                                if(GetMethodID( _pEnv, _pObj, name, "(" + preArgsSignature + ")" + returnSignature, &pMethod ) == 0)
                                                                                    CallObjectMethod( _pEnv, _pObj, pMethod, &pObjResult, call_len, ar_call);
                                                                                else
                                                                                    throw new Exception(GetException(_pEnv));
                                                                            }
                                                                            
                                                                            IntPtr returnPtr = new IntPtr(pObjResult);

                                                                            if(returnPtr == IntPtr.Zero)
                                                                            {
                                                                                DetacheThread((void*)JVMPtr);
                                                                                return null;
                                                                            }

                                                                            int hashID_res = GetJVMID(_pEnv, pObjResult, true);

                                                                            
                                                                            if(JVMDelegate.DB.ContainsKey(hashID_res) && JVMDelegate.DB[hashID_res].IsAlive)
                                                                            {
                                                                                var _ret = JVMDelegate.DB[hashID_res].Target;
                                                                                DetacheThread((void*)JVMPtr);
                                                                                return _ret;
                                                                            }


                                                                            else if(Runtime.DB.ContainsKey(hashID_res) && Runtime.DB[hashID_res].IsAlive)
                                                                            {
                                                                                // Console.WriteLine("Runtime Exists: " + hashID_res);
                                                                                var _ret = Runtime.DB[hashID_res].Target;
                                                                                DetacheThread((void*)JVMPtr);
                                                                                return _ret;
                                                                            }

                                                                            
                                                                            else if(JVMObject.DB.ContainsKey(hashID_res) && JVMObject.DB[hashID_res].IsAlive)
                                                                            {
                                                                                // Console.WriteLine("JVMObject Exists: " + hashID_res);

                                                                                // if(JVMObject.DB[hashID_res] is JVMTuple)
                                                                                if(JVMObject.DB[hashID_res].Target is JVMTuple)
                                                                                {
                                                                                    // JVMTuple jobj = JVMObject.DB[hashID_res] as JVMTuple;
                                                                                    JVMTuple jobj = JVMObject.DB[hashID_res].Target as JVMTuple;
                                                                                    DetacheThread((void*)JVMPtr);
                                                                                    return jobj.jVMTuple;
                                                                                }
                                                                                DetacheThread((void*)JVMPtr);
                                                                                // return JVMObject.DB[hashID_res];
                                                                                return JVMObject.DB[hashID_res].Target;
                                                                            }

                                                                            else
                                                                            {
                                                                                string cls = returnSignature.StartsWith("L") && returnSignature.EndsWith(";") ? returnSignature.Substring(1).Replace(";","").Replace("/",".") : returnSignature;

                                                                                var _ret = getObject(_pEnv, cls, pObjResult);
                                                                                // GetID(_ret, false); //NEW
                                                                                DetacheThread((void*)JVMPtr);
                                                                                return _ret;
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            Console.WriteLine("CLR Object: _pObj != pObjResult");
                                                                            return null;
                                                                        }
                                                                    }
                                                                    catch(Exception e)
                                                                    {
                                                                        Console.WriteLine("CLR inner invoke: " + e);
                                                                        return null;
                                                                    }
                                                                }));
                                                            }

                                                            break;
                                                    }
                                                
                                                }
                                            }

                                            // if(JVMObject.__DB.ContainsKey(hashID))
                                            // {

                                            //     JVMObject oo;
                                            //     JVMObject.__DB.TryRemove(hashID, out oo);
                                            // }

                                            // if(JVMObject.__DB.ContainsKey(sig_hashID))
                                            // {

                                            //     JVMObject oo;
                                            //     JVMObject.__DB.TryRemove(sig_hashID, out oo);
                                            // }

                                            ((object)expandoObject).RegisterGCEvent(hashID, delegate(object _obj, int _id)
                                            {
                                                Runtime.RemoveID(_id);
                                            });

                                            return expandoObject;
                                        }
                                        else
                                            throw new Exception(GetException(pEnv));
                                    }
                                    else
                                        throw new Exception(GetException(pEnv));
                                }
                                else 
                                    throw new Exception(GetException(pEnv));
                            }
                            else 
                                throw new Exception(GetException(pEnv));
                        }
                        else 
                            throw new Exception(GetException(pEnv));
                    }
                    else 
                        throw new Exception("JVM Engine not loaded");
                }
                catch(Exception e)
                {
                    Console.WriteLine("CreateInstancePtr: " + e);
                    Console.WriteLine(e.StackTrace);
                    return null;
                }
            }
        }
    }

    internal static class GCInterceptor
    {
        private static ConditionalWeakTable<object, CallbackRef> _table;

        static GCInterceptor()
        {
            _table = new ConditionalWeakTable<object, CallbackRef>();
        }

        public static void RegisterGCEvent(this object obj, int id, Action<object, int> action)
        {
            CallbackRef callbackRef;
            bool found = _table.TryGetValue(obj, out callbackRef);
            if (found)
            {
                callbackRef.Collected += action;
                return;
            }

            callbackRef = new CallbackRef(obj, id);
            callbackRef.Collected += action;
            _table.Add(obj, callbackRef);
        }

        public static void DeregisterGCEvent(this object obj, int id, Action<object, int> action)
        {
            CallbackRef callbackRef;
            bool found = _table.TryGetValue(obj, out callbackRef);
            if (!found)
                throw new Exception("No events registered");

            callbackRef.Collected -= action;
        }

        private class CallbackRef
        {
            private object _obj;
            private int _id;

            public event Action<object, int> Collected;

            public CallbackRef(object obj, int id)
            {
                _obj = obj;
                _id = id;
            }

            ~CallbackRef()
            {
                Action<object, int> handle = Collected;
                if (handle != null)
                    handle(_obj, _id);
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

        ~ObjectWrapper() 
        {
            this.Dispose();
            Runtime.RemoveID(Object.GetHashCode());
            // if (Ptr != IntPtr.Zero) 
            // {
            //     Marshal.FreeHGlobal(Ptr);
            //     Ptr = IntPtr.Zero;
            // }
        }

        public void Dispose() 
        {
            // Marshal.FreeHGlobal(Ptr);
            // Ptr = IntPtr.Zero;
            // GC.SuppressFinalize(this);
        }
    }

    internal static class TypeExtensions
    {
        public static bool IsArrayOf<T>(this Type type)
        {
            return type == typeof (T[]);
        }
    } 

    class StructWrapper : IDisposable 
    {
        public IntPtr Ptr { get; private set; }
        private readonly static object objLock_InvokeFunc = new object();

        // public ConcurrentDictionary<int, object> __DB = new ConcurrentDictionary<int, object>();

        public unsafe StructWrapper(void* pEnv, object[] obj) 
        {
            // // lock(objLock_InvokeFunc)
            {
                if (Ptr != null && obj != null) 
                {
                    var size = Unsafe.SizeOf<object[]>() * obj.Length;
                    Ptr = Marshal.AllocHGlobal(size);
                    void** _ptr = (void**)Ptr;
                    getJavaParameters(pEnv, ref _ptr, obj);
                }
                else 
                {
                    Ptr = IntPtr.Zero;
                }
            }
        }

        ~StructWrapper() 
        {
            if (Ptr != IntPtr.Zero) 
            {
                Marshal.FreeHGlobal(Ptr);
                Ptr = IntPtr.Zero;
            }
        }

        public void Dispose() 
        {
            Marshal.FreeHGlobal(Ptr);
            Ptr = IntPtr.Zero;
            // GC.SuppressFinalize(this);
        }

        public static implicit operator IntPtr(StructWrapper w) 
        {
            return w.Ptr;
        }


        private unsafe void getJavaParameters(void* pEnv, ref void** ar_call, object[] call_args)
        {
            // // lock(objLock_getJavaParameters)
            {
                        
                // void*  pEnv;
                // if(AttacheThread((void*)JVMPtr,&pEnv) != 0) throw new Exception ("Attach to thread error");
                int call_len = call_args == null ? 0 : call_args.Length;

                for(int i = 0; i < call_len; i++)
                {
                    var arg = call_args[i];
                    

                    // Runtime.__DB[argID] = arg;

                    if(arg is ObjectWrapper)
                        arg = (arg as ObjectWrapper).Object;

                    var argID = Runtime.GetID(arg, false);

                    var type = arg.GetType();

                    // arg.RegisterGCEvent(argID, delegate(object _obj, int _id)
                    // {
                    //     // Console.WriteLine("------__________---Object(" + _obj + ") with hash code " + _id + " recently collected: ");
                    //     Runtime.RemoveID(_id);
                    // });



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
                            void* string_arg = Runtime.GetJavaString(pEnv, (string)arg);
                            
                            ar_call[i] = string_arg;
                            break;

                        case TypeCode.DateTime:
                            void* date_arg = Runtime.GetJavaDateTime(pEnv, (DateTime)arg);
                            
                            ar_call[i] = date_arg;
                            break;

                        default:
                            
                            if(arg is JVMTuple)
                            {
                                JVMTuple jobj = arg as JVMTuple; 
                                // void* ptr = (void *)(jobj.jVMObject.Pointer);
                                void* pNetBridgeClass;
                                if(Runtime.FindClass( pEnv, "app/quant/clr/CLRRuntime", &pNetBridgeClass) != 0 ) throw new Exception("getJavaParameters pNetBridgeClass not found");
                                
                                void* ptr = Runtime.GetJVMObject(pEnv, pNetBridgeClass, jobj.jVMObject.JavaHashCode);
                                Runtime.RegisterJVMObject(pEnv, jobj.jVMObject.JavaHashCode, ptr);
                                ar_call[i] = ptr;
                            }

                            else if(arg is IJVMTuple)
                            {
                                IJVMTuple jobj = arg as IJVMTuple; 
                                // void* ptr = (void *)(JVMObject.DB[jobj.JVMObject.JavaHashCode].Pointer);
                                void* pNetBridgeClass;
                                if(Runtime.FindClass( pEnv, "app/quant/clr/CLRRuntime", &pNetBridgeClass) != 0 ) throw new Exception("getJavaParameters pNetBridgeClass not found");
                                
                                void* ptr = Runtime.GetJVMObject(pEnv, pNetBridgeClass, jobj.JVMObject.JavaHashCode);
                                Runtime.RegisterJVMObject(pEnv, jobj.JVMObject.JavaHashCode, ptr);
                                ar_call[i] = ptr;
                            }

                            else if(arg is JVMObject)
                            {
                                JVMObject jobj = arg as JVMObject; 
                                // void* ptr = (void *)(jobj.Pointer);
                                void* pNetBridgeClass;
                                if(Runtime.FindClass( pEnv, "app/quant/clr/CLRRuntime", &pNetBridgeClass) != 0 ) throw new Exception("getJavaParameters pNetBridgeClass not found");
                                
                                void* ptr = Runtime.GetJVMObject(pEnv, pNetBridgeClass, jobj.JavaHashCode);
                                Runtime.RegisterJVMObject(pEnv, jobj.JavaHashCode, ptr);
                                ar_call[i] = ptr;
                            }

                            else if(arg is Array)
                            {
                                Array sub = arg as Array;
                                // Console.WriteLine("-------1: " + _sub.Length);

                                // var sub = _sub.Cast<object>().ToArray();
                                // Console.WriteLine("-------2: " + sub.Length);
                                // JVMObject javaArray = getJavaArray(sub);

                                void* pNetBridgeClass;
                                if(Runtime.FindClass( pEnv, "app/quant/clr/CLRRuntime", &pNetBridgeClass) != 0 ) throw new Exception("getJavaParameters pNetBridgeClass not found");
                                
                                JVMObject javaArray = getJavaArray(pEnv, pNetBridgeClass, sub);
                                void* ptr = Runtime.GetJVMObject(pEnv, pNetBridgeClass, javaArray.JavaHashCode);
                                Runtime.RegisterJVMObject(pEnv, javaArray.JavaHashCode, ptr);

                                // void* ptr = (void*)(new StructWrapper(pEnv, sub)).Ptr;
                                ar_call[i] = ptr;//javaArray.Pointer.ToPointer();

                                // if(Runtime.__DB.ContainsKey(javaArray.JavaHashCode))
                                // {
                                //     object _out;
                                //     Runtime.__DB.TryRemove(javaArray.JavaHashCode, out _out);
                                // }
                            }

                            else if(arg is IEnumerable<object>)
                            {
                                // void* ptr_res = (void *)(arg.GetHashCode());

                                // void** pAr_len = stackalloc void*[2];
                                object[] pAr_len_data = new object[]{ arg.GetType().ToString(), argID };
                                // getJavaParameters(pEnv, ref pAr_len, pAr_len_data);
                                void** pAr_len = (void**)(new StructWrapper(pEnv, pAr_len_data)).Ptr;

                                void* pObj;
                                void* CLRObjClass;
                                void*  pLoadClassMethod; // The executed method struct
                                if(Runtime.FindClass( pEnv, "app/quant/clr/CLRIterable", &CLRObjClass) == 0)
                                {
                                    void* pClass;
                                    if(Runtime.NewObjectP( pEnv, CLRObjClass, "(Ljava/lang/String;I)V", 2, pAr_len, &pObj ) == 0)
                                    {
                                        ar_call[i] = pObj;
                                        Runtime.RegisterJVMObject(pEnv, argID, pObj);
                                        // Runtime.GetID(pEnv, pObj);
                                        // Runtime.RegisterJVMObject(pEnv, Runtime.GetID(pEnv, pObj) ,pObj);
                                        // Runtime.DB.TryAdd(arg.GetHashCode(), arg);
                                        if(!(arg is JVMObject) && !(arg is IJVMTuple))
                                            // Runtime.DB[argID] = arg;
                                            Runtime.DB[argID] = new WeakReference(arg);

                                        // Runtime.RemoveID(argID);
                                    }
                                    else
                                        throw new Exception(Runtime.GetException(pEnv));
                                }
                                else
                                    throw new Exception(Runtime.GetException(pEnv));
                            }

                            else if(arg is System.Func<Object, Object>)
                            {
                                // void* ptr_res = (void *)(arg.GetHashCode());

                                // void** pAr_len = stackalloc void*[2];
                                object[] pAr_len_data = new object[]{ arg.GetType().ToString(), argID };
                                // getJavaParameters(pEnv, ref pAr_len, pAr_len_data);
                                void** pAr_len = (void**)(new StructWrapper(pEnv, pAr_len_data)).Ptr;

                                void* pObj;
                                void* CLRObjClass;
                                void*  pLoadClassMethod; // The executed method struct
                                if(Runtime.FindClass( pEnv, "app/quant/clr/function/CLRFunction1", &CLRObjClass) == 0)
                                {
                                    void* pClass;
                                    if(Runtime.NewObjectP( pEnv, CLRObjClass, "(Ljava/lang/String;I)V", 2, pAr_len, &pObj ) == 0)
                                    {
                                        ar_call[i] = pObj;
                                        Runtime.RegisterJVMObject(pEnv, argID, pObj);
                                        // Runtime.GetID(pEnv, pObj);
                                        // Runtime.RegisterJVMObject(pEnv, Runtime.GetID(pEnv, pObj) ,pObj);
                                        // Runtime.DB.TryAdd(arg.GetHashCode(), arg);
                                        if(!(arg is JVMObject) && !(arg is IJVMTuple))
                                            // Runtime.DB[argID] = arg;
                                            Runtime.DB[argID] = new WeakReference(arg);
                                    }
                                    else
                                        throw new Exception(Runtime.GetException(pEnv));
                                }
                                else
                                    throw new Exception(Runtime.GetException(pEnv));
                            }
                            else if(arg is System.Func<Object, Object, Object>)
                            {
                                // void* ptr_res = (void *)(arg.GetHashCode());

                                // void** pAr_len = stackalloc void*[2];
                                object[] pAr_len_data = new object[]{ arg.GetType().ToString(), argID };
                                // getJavaParameters(pEnv, ref pAr_len, pAr_len_data);
                                void** pAr_len = (void**)(new StructWrapper(pEnv, pAr_len_data)).Ptr;

                                void* pObj;
                                void* CLRObjClass;
                                void*  pLoadClassMethod; // The executed method struct
                                if(Runtime.FindClass( pEnv, "app/quant/clr/function/CLRFunction2", &CLRObjClass) == 0)
                                {
                                    void* pClass;
                                    if(Runtime.NewObjectP( pEnv, CLRObjClass, "(Ljava/lang/String;I)V", 2, pAr_len, &pObj ) == 0)
                                    {
                                        ar_call[i] = pObj;
                                        Runtime.RegisterJVMObject(pEnv, argID, pObj);
                                        // Runtime.GetID(pEnv, pObj);
                                        // Runtime.RegisterJVMObject(pEnv, Runtime.GetID(pEnv, pObj) ,pObj);
                                        // Runtime.DB.TryAdd(arg.GetHashCode(), arg);
                                        if(!(arg is JVMObject) && !(arg is IJVMTuple))
                                            // Runtime.DB[argID] = arg;
                                            Runtime.DB[argID] = new WeakReference(arg);
                                    }
                                    else
                                        throw new Exception(Runtime.GetException(pEnv));
                                }
                                else
                                    throw new Exception(Runtime.GetException(pEnv));
                            }
                            else if(arg is System.Func<Object, Object, Object, Object>)
                            {
                                // void* ptr_res = (void *)(arg.GetHashCode());

                                // void** pAr_len = stackalloc void*[2];
                                object[] pAr_len_data = new object[]{ arg.GetType().ToString(), argID };
                                // getJavaParameters(pEnv, ref pAr_len, pAr_len_data);
                                void** pAr_len = (void**)(new StructWrapper(pEnv, pAr_len_data)).Ptr;

                                void* pObj;
                                void* CLRObjClass;
                                void*  pLoadClassMethod; // The executed method struct
                                if(Runtime.FindClass( pEnv, "app/quant/clr/function/CLRFunction3", &CLRObjClass) == 0)
                                {
                                    void* pClass;
                                    if(Runtime.NewObjectP( pEnv, CLRObjClass, "(Ljava/lang/String;I)V", 2, pAr_len, &pObj ) == 0)
                                    {
                                        ar_call[i] = pObj;
                                        Runtime.RegisterJVMObject(pEnv, argID, pObj);
                                        //Runtime.GetID(pEnv, pObj);
                                        // Runtime.RegisterJVMObject(pEnv, Runtime.GetID(pEnv, pObj) ,pObj);
                                        // Runtime.DB.TryAdd(arg.GetHashCode(), arg);
                                        if(!(arg is JVMObject) && !(arg is IJVMTuple))
                                            // Runtime.DB[argID] = arg;
                                            Runtime.DB[argID] = new WeakReference(arg);
                                    }
                                    else
                                        throw new Exception(Runtime.GetException(pEnv));
                                }
                                else
                                    throw new Exception(Runtime.GetException(pEnv));
                            }
                            else if(arg is System.Func<Object, Object, Object, Object, Object>)
                            {
                                // void* ptr_res = (void *)(arg.GetHashCode());

                                // void** pAr_len = stackalloc void*[2];
                                object[] pAr_len_data = new object[]{ arg.GetType().ToString(), argID };
                                // getJavaParameters(pEnv, ref pAr_len, pAr_len_data);
                                void** pAr_len = (void**)(new StructWrapper(pEnv, pAr_len_data)).Ptr;

                                void* pObj;
                                void* CLRObjClass;
                                void*  pLoadClassMethod; // The executed method struct
                                if(Runtime.FindClass( pEnv, "app/quant/clr/function/CLRFunction4", &CLRObjClass) == 0)
                                {
                                    void* pClass;
                                    if(Runtime.NewObjectP( pEnv, CLRObjClass, "(Ljava/lang/String;I)V", 2, pAr_len, &pObj ) == 0)
                                    {
                                        ar_call[i] = pObj;
                                        Runtime.RegisterJVMObject(pEnv, argID, pObj);
                                        //Runtime.GetID(pEnv, pObj);
                                        // Runtime.RegisterJVMObject(pEnv, Runtime.GetID(pEnv, pObj) ,pObj);
                                        // Runtime.DB.TryAdd(arg.GetHashCode(), arg);
                                        if(!(arg is JVMObject) && !(arg is IJVMTuple))
                                            // Runtime.DB[argID] = arg;
                                            Runtime.DB[argID] = new WeakReference(arg);
                                    }
                                    else
                                        throw new Exception(Runtime.GetException(pEnv));
                                }
                                else
                                    throw new Exception(Runtime.GetException(pEnv));
                            }
                            else if(arg is System.Func<Object, Object, Object, Object, Object, Object>)
                            {
                                // void* ptr_res = (void *)(arg.GetHashCode());

                                // void** pAr_len = stackalloc void*[2];
                                object[] pAr_len_data = new object[]{ arg.GetType().ToString(), argID };
                                // getJavaParameters(pEnv, ref pAr_len, pAr_len_data);
                                void** pAr_len = (void**)(new StructWrapper(pEnv, pAr_len_data)).Ptr;

                                void* pObj;
                                void* CLRObjClass;
                                void*  pLoadClassMethod; // The executed method struct
                                if(Runtime.FindClass( pEnv, "app/quant/clr/function/CLRFunction5", &CLRObjClass) == 0)
                                {
                                    void* pClass;
                                    if(Runtime.NewObjectP( pEnv, CLRObjClass, "(Ljava/lang/String;I)V", 2, pAr_len, &pObj ) == 0)
                                    {
                                        ar_call[i] = pObj;
                                        Runtime.RegisterJVMObject(pEnv, argID, pObj);
                                        //Runtime.GetID(pEnv, pObj);
                                        // Runtime.RegisterJVMObject(pEnv, Runtime.GetID(pEnv, pObj) ,pObj);
                                        // Runtime.DB.TryAdd(arg.GetHashCode(), arg);
                                        if(!(arg is JVMObject) && !(arg is IJVMTuple))
                                            // Runtime.DB[argID] = arg;
                                            Runtime.DB[argID] = new WeakReference(arg);
                                    }
                                    else
                                        throw new Exception(Runtime.GetException(pEnv));
                                }
                                else
                                    throw new Exception(Runtime.GetException(pEnv));
                            }
                            else if(arg is System.Func<Object, Object, Object, Object, Object, Object, Object>)
                            {
                                // void* ptr_res = (void *)(arg.GetHashCode());

                                // void** pAr_len = stackalloc void*[2];
                                object[] pAr_len_data = new object[]{ arg.GetType().ToString(), argID };
                                // getJavaParameters(pEnv, ref pAr_len, pAr_len_data);
                                void** pAr_len = (void**)(new StructWrapper(pEnv, pAr_len_data)).Ptr;

                                void* pObj;
                                void* CLRObjClass;
                                void*  pLoadClassMethod; // The executed method struct
                                if(Runtime.FindClass( pEnv, "app/quant/clr/function/CLRFunction6", &CLRObjClass) == 0)
                                {
                                    void* pClass;
                                    if(Runtime.NewObjectP( pEnv, CLRObjClass, "(Ljava/lang/String;I)V", 2, pAr_len, &pObj ) == 0)
                                    {
                                        ar_call[i] = pObj;
                                        Runtime.RegisterJVMObject(pEnv, argID, pObj);
                                        //Runtime.GetID(pEnv, pObj);
                                        // Runtime.RegisterJVMObject(pEnv, Runtime.GetID(pEnv, pObj) ,pObj);
                                        // Runtime.DB.TryAdd(arg.GetHashCode(), arg);
                                        if(!(arg is JVMObject) && !(arg is IJVMTuple))
                                            // Runtime.DB[argID] = arg;
                                            Runtime.DB[argID] = new WeakReference(arg);
                                    }
                                    else
                                        throw new Exception(Runtime.GetException(pEnv));
                                }
                                else
                                    throw new Exception(Runtime.GetException(pEnv));
                            }
                            else if(arg is System.Func<Object, Object, Object, Object, Object, Object, Object, Object>)
                            {
                                // void* ptr_res = (void *)(arg.GetHashCode());

                                // void** pAr_len = stackalloc void*[2];
                                object[] pAr_len_data = new object[]{ arg.GetType().ToString(), argID };
                                // getJavaParameters(pEnv, ref pAr_len, pAr_len_data);
                                void** pAr_len = (void**)(new StructWrapper(pEnv, pAr_len_data)).Ptr;

                                void* pObj;
                                void* CLRObjClass;
                                void*  pLoadClassMethod; // The executed method struct
                                if(Runtime.FindClass( pEnv, "app/quant/clr/function/CLRFunction7", &CLRObjClass) == 0)
                                {
                                    void* pClass;
                                    if(Runtime.NewObjectP( pEnv, CLRObjClass, "(Ljava/lang/String;I)V", 2, pAr_len, &pObj ) == 0)
                                    {
                                        ar_call[i] = pObj;
                                        Runtime.RegisterJVMObject(pEnv, argID, pObj);
                                        //Runtime.GetID(pEnv, pObj);
                                        // Runtime.RegisterJVMObject(pEnv, Runtime.GetID(pEnv, pObj) ,pObj);
                                        // Runtime.DB.TryAdd(arg.GetHashCode(), arg);
                                        if(!(arg is JVMObject) && !(arg is IJVMTuple))
                                            // Runtime.DB[argID] = arg;
                                            Runtime.DB[argID] = new WeakReference(arg);
                                    }
                                    else
                                        throw new Exception(Runtime.GetException(pEnv));
                                }
                                else
                                    throw new Exception(Runtime.GetException(pEnv));
                            }
                            else if(arg is System.Func<Object, Object, Object, Object, Object, Object, Object, Object, Object>)
                            {
                                // void* ptr_res = (void *)(arg.GetHashCode());

                                // void** pAr_len = stackalloc void*[2];
                                object[] pAr_len_data = new object[]{ arg.GetType().ToString(), argID };
                                // getJavaParameters(pEnv, ref pAr_len, pAr_len_data);
                                void** pAr_len = (void**)(new StructWrapper(pEnv, pAr_len_data)).Ptr;

                                void* pObj;
                                void* CLRObjClass;
                                void*  pLoadClassMethod; // The executed method struct
                                if(Runtime.FindClass( pEnv, "app/quant/clr/function/CLRFunction8", &CLRObjClass) == 0)
                                {
                                    void* pClass;
                                    if(Runtime.NewObjectP( pEnv, CLRObjClass, "(Ljava/lang/String;I)V", 2, pAr_len, &pObj ) == 0)
                                    {
                                        ar_call[i] = pObj;
                                        Runtime.RegisterJVMObject(pEnv, argID, pObj);
                                        //Runtime.GetID(pEnv, pObj);
                                        // Runtime.RegisterJVMObject(pEnv, Runtime.GetID(pEnv, pObj) ,pObj);
                                        // Runtime.DB.TryAdd(arg.GetHashCode(), arg);
                                        if(!(arg is JVMObject) && !(arg is IJVMTuple))
                                            // Runtime.DB[argID] = arg;
                                            Runtime.DB[argID] = new WeakReference(arg);
                                    }
                                    else
                                        throw new Exception(Runtime.GetException(pEnv));
                                }
                                else
                                    throw new Exception(Runtime.GetException(pEnv));
                            }
                            else if(arg is System.Func<Object, Object, Object, Object, Object, Object, Object, Object, Object, Object>)
                            {
                                // void* ptr_res = (void *)(arg.GetHashCode());

                                // void** pAr_len = stackalloc void*[2];
                                object[] pAr_len_data = new object[]{ arg.GetType().ToString(), argID };
                                // getJavaParameters(pEnv, ref pAr_len, pAr_len_data);
                                void** pAr_len = (void**)(new StructWrapper(pEnv, pAr_len_data)).Ptr;

                                void* pObj;
                                void* CLRObjClass;
                                void*  pLoadClassMethod; // The executed method struct
                                if(Runtime.FindClass( pEnv, "app/quant/clr/function/CLRFunction9", &CLRObjClass) == 0)
                                {
                                    void* pClass;
                                    if(Runtime.NewObjectP( pEnv, CLRObjClass, "(Ljava/lang/String;I)V", 2, pAr_len, &pObj ) == 0)
                                    {
                                        ar_call[i] = pObj;
                                        Runtime.RegisterJVMObject(pEnv, argID, pObj);
                                        //Runtime.GetID(pEnv, pObj);
                                        // Runtime.RegisterJVMObject(pEnv, Runtime.GetID(pEnv, pObj) ,pObj);
                                        // Runtime.DB.TryAdd(arg.GetHashCode(), arg);
                                        if(!(arg is JVMObject) && !(arg is IJVMTuple))
                                            // Runtime.DB[argID] = arg;
                                            Runtime.DB[argID] = new WeakReference(arg);
                                    }
                                    else
                                        throw new Exception(Runtime.GetException(pEnv));
                                }
                                else
                                    throw new Exception(Runtime.GetException(pEnv));
                            }
                            else if(arg is System.Func<Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object>)
                            {
                                // void* ptr_res = (void *)(arg.GetHashCode());

                                // void** pAr_len = stackalloc void*[2];
                                object[] pAr_len_data = new object[]{ arg.GetType().ToString(), argID };
                                // getJavaParameters(pEnv, ref pAr_len, pAr_len_data);
                                void** pAr_len = (void**)(new StructWrapper(pEnv, pAr_len_data)).Ptr;

                                void* pObj;
                                void* CLRObjClass;
                                void*  pLoadClassMethod; // The executed method struct
                                if(Runtime.FindClass( pEnv, "app/quant/clr/function/CLRFunction10", &CLRObjClass) == 0)
                                {
                                    void* pClass;
                                    if(Runtime.NewObjectP( pEnv, CLRObjClass, "(Ljava/lang/String;I)V", 2, pAr_len, &pObj ) == 0)
                                    {
                                        ar_call[i] = pObj;
                                        Runtime.RegisterJVMObject(pEnv, argID, pObj);
                                        //Runtime.GetID(pEnv, pObj);
                                        // Runtime.RegisterJVMObject(pEnv, Runtime.GetID(pEnv, pObj) ,pObj);
                                        // Runtime.DB.TryAdd(arg.GetHashCode(), arg);
                                        if(!(arg is JVMObject) && !(arg is IJVMTuple))
                                            // Runtime.DB[argID] = arg;
                                            Runtime.DB[argID] = new WeakReference(arg);
                                    }
                                    else
                                        throw new Exception(Runtime.GetException(pEnv));
                                }
                                else
                                    throw new Exception(Runtime.GetException(pEnv));
                            }
                            else if(arg is System.Func<Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object>)
                            {
                                // void* ptr_res = (void *)(arg.GetHashCode());

                                // void** pAr_len = stackalloc void*[2];
                                object[] pAr_len_data = new object[]{ arg.GetType().ToString(), argID };
                                // getJavaParameters(pEnv, ref pAr_len, pAr_len_data);
                                void** pAr_len = (void**)(new StructWrapper(pEnv, pAr_len_data)).Ptr;

                                void* pObj;
                                void* CLRObjClass;
                                void*  pLoadClassMethod; // The executed method struct
                                if(Runtime.FindClass( pEnv, "app/quant/clr/function/CLRFunction11", &CLRObjClass) == 0)
                                {
                                    void* pClass;
                                    if(Runtime.NewObjectP( pEnv, CLRObjClass, "(Ljava/lang/String;I)V", 2, pAr_len, &pObj ) == 0)
                                    {
                                        ar_call[i] = pObj;
                                        Runtime.RegisterJVMObject(pEnv, argID, pObj);
                                        //Runtime.GetID(pEnv, pObj);
                                        // Runtime.RegisterJVMObject(pEnv, Runtime.GetID(pEnv, pObj) ,pObj);
                                        // Runtime.DB.TryAdd(arg.GetHashCode(), arg);
                                        if(!(arg is JVMObject) && !(arg is IJVMTuple))
                                            // Runtime.DB[argID] = arg;
                                            Runtime.DB[argID] = new WeakReference(arg);
                                    }
                                    else
                                        throw new Exception(Runtime.GetException(pEnv));
                                }
                                else
                                    throw new Exception(Runtime.GetException(pEnv));
                            }
                            else if(arg is System.Func<Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object>)
                            {
                                // void* ptr_res = (void *)(arg.GetHashCode());

                                // void** pAr_len = stackalloc void*[2];
                                object[] pAr_len_data = new object[]{ arg.GetType().ToString(), argID };
                                // getJavaParameters(pEnv, ref pAr_len, pAr_len_data);
                                void** pAr_len = (void**)(new StructWrapper(pEnv, pAr_len_data)).Ptr;

                                void* pObj;
                                void* CLRObjClass;
                                void*  pLoadClassMethod; // The executed method struct
                                if(Runtime.FindClass( pEnv, "app/quant/clr/function/CLRFunction12", &CLRObjClass) == 0)
                                {
                                    void* pClass;
                                    if(Runtime.NewObjectP( pEnv, CLRObjClass, "(Ljava/lang/String;I)V", 2, pAr_len, &pObj ) == 0)
                                    {
                                        ar_call[i] = pObj;
                                        Runtime.RegisterJVMObject(pEnv, argID, pObj);
                                        //Runtime.GetID(pEnv, pObj);
                                        // Runtime.RegisterJVMObject(pEnv, Runtime.GetID(pEnv, pObj) ,pObj);
                                        if(!(arg is JVMObject) && !(arg is IJVMTuple))
                                            // Runtime.DB[argID] = arg;
                                            Runtime.DB[argID] = new WeakReference(arg);
                                    }
                                    else
                                        throw new Exception(Runtime.GetException(pEnv));
                                }
                                else
                                    throw new Exception(Runtime.GetException(pEnv));
                            }
                            else if(arg is System.Func<Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object>)
                            {
                                // void* ptr_res = (void *)(arg.GetHashCode());

                                // void** pAr_len = stackalloc void*[2];
                                object[] pAr_len_data = new object[]{ arg.GetType().ToString(), argID };
                                // getJavaParameters(pEnv, ref pAr_len, pAr_len_data);
                                void** pAr_len = (void**)(new StructWrapper(pEnv, pAr_len_data)).Ptr;

                                void* pObj;
                                void* CLRObjClass;
                                void*  pLoadClassMethod; // The executed method struct
                                if(Runtime.FindClass( pEnv, "app/quant/clr/function/CLRFunction13", &CLRObjClass) == 0)
                                {
                                    void* pClass;
                                    if(Runtime.NewObjectP( pEnv, CLRObjClass, "(Ljava/lang/String;I)V", 2, pAr_len, &pObj ) == 0)
                                    {
                                        ar_call[i] = pObj;
                                        Runtime.RegisterJVMObject(pEnv, argID, pObj);
                                        //Runtime.GetID(pEnv, pObj);
                                        // Runtime.RegisterJVMObject(pEnv, Runtime.GetID(pEnv, pObj) ,pObj);
                                        // Runtime.DB.TryAdd(arg.GetHashCode(), arg);
                                        if(!(arg is JVMObject) && !(arg is IJVMTuple))
                                            // Runtime.DB[argID] = arg;
                                            Runtime.DB[argID] = new WeakReference(arg);
                                    }
                                    else
                                        throw new Exception(Runtime.GetException(pEnv));
                                }
                                else
                                    throw new Exception(Runtime.GetException(pEnv));
                            }
                            else if(arg is System.Func<Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object>)
                            {
                                // void* ptr_res = (void *)(arg.GetHashCode());

                                // void** pAr_len = stackalloc void*[2];
                                object[] pAr_len_data = new object[]{ arg.GetType().ToString(), argID };
                                // getJavaParameters(pEnv, ref pAr_len, pAr_len_data);
                                void** pAr_len = (void**)(new StructWrapper(pEnv, pAr_len_data)).Ptr;

                                void* pObj;
                                void* CLRObjClass;
                                void*  pLoadClassMethod; // The executed method struct
                                if(Runtime.FindClass( pEnv, "app/quant/clr/function/CLRFunction14", &CLRObjClass) == 0)
                                {
                                    void* pClass;
                                    if(Runtime.NewObjectP( pEnv, CLRObjClass, "(Ljava/lang/String;I)V", 2, pAr_len, &pObj ) == 0)
                                    {
                                        ar_call[i] = pObj;
                                        Runtime.RegisterJVMObject(pEnv, argID, pObj);
                                        //Runtime.GetID(pEnv, pObj);
                                        // Runtime.RegisterJVMObject(pEnv, Runtime.GetID(pEnv, pObj) ,pObj);
                                        // Runtime.DB.TryAdd(arg.GetHashCode(), arg);
                                        if(!(arg is JVMObject) && !(arg is IJVMTuple))
                                            // Runtime.DB[argID] = arg;
                                            Runtime.DB[argID] = new WeakReference(arg);
                                    }
                                    else
                                        throw new Exception(Runtime.GetException(pEnv));
                                }
                                else
                                    throw new Exception(Runtime.GetException(pEnv));
                            }
                            else if(arg is System.Func<Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object>)
                            {
                                void* ptr_res = (void *)(argID);

                                // void** pAr_len = stackalloc void*[2];
                                object[] pAr_len_data = new object[]{ arg.GetType().ToString(), argID };
                                // getJavaParameters(pEnv, ref pAr_len, pAr_len_data);
                                void** pAr_len = (void**)(new StructWrapper(pEnv, pAr_len_data)).Ptr;

                                void* pObj;
                                void* CLRObjClass;
                                void*  pLoadClassMethod; // The executed method struct
                                if(Runtime.FindClass( pEnv, "app/quant/clr/function/CLRFunction15", &CLRObjClass) == 0)
                                {
                                    void* pClass;
                                    if(Runtime.NewObjectP( pEnv, CLRObjClass, "(Ljava/lang/String;I)V", 2, pAr_len, &pObj ) == 0)
                                    {
                                        ar_call[i] = pObj;
                                        Runtime.RegisterJVMObject(pEnv, argID, pObj);
                                        //Runtime.GetID(pEnv, pObj);
                                        // Runtime.RegisterJVMObject(pEnv, Runtime.GetID(pEnv, pObj) ,pObj);
                                        // Runtime.DB.TryAdd(arg.GetHashCode(), arg);
                                        if(!(arg is JVMObject) && !(arg is IJVMTuple))
                                            // Runtime.DB[argID] = arg;
                                            Runtime.DB[argID] = new WeakReference(arg);
                                    }
                                    else
                                        throw new Exception(Runtime.GetException(pEnv));
                                }
                                else
                                    throw new Exception(Runtime.GetException(pEnv));
                            }

                            else
                            {
                                // void* ptr_res = (void *)(arg.GetHashCode());

                                // void** pAr_len = stackalloc void*[2];
                                object[] pAr_len_data = new object[]{ arg.GetType().ToString(), argID, false };
                                // getJavaParameters(pEnv, ref pAr_len, pAr_len_data);
                                void** pAr_len = (void**)(new StructWrapper(pEnv, pAr_len_data)).Ptr;

                                void* pObj;
                                void* CLRObjClass;
                                void*  pLoadClassMethod; // The executed method struct
                                if(Runtime.FindClass( pEnv, "app/quant/clr/CLRObject", &CLRObjClass) == 0)
                                {
                                    void* pClass;
                                    if(Runtime.NewObjectP( pEnv, CLRObjClass, "(Ljava/lang/String;IZ)V", 3, pAr_len, &pObj ) == 0)
                                    {
                                        ar_call[i] = pObj;
                                        Runtime.RegisterJVMObject(pEnv, argID, pObj);

                                        //Runtime.GetID(pEnv, pObj);
                                        // Runtime.RegisterJVMObject(pEnv, Runtime.GetID(pEnv, pObj) ,pObj);

                                        // Runtime.DB.TryAdd(arg.GetHashCode(), arg);
                                        if(!(arg is JVMObject) && !(arg is IJVMTuple))
                                            // Runtime.DB[argID] = arg;
                                            Runtime.DB[argID] = new WeakReference(arg);
                                    }
                                    else
                                        throw new Exception("Create CLRObject instance error");        
                                }
                                else
                                    throw new Exception("Get CLRObject class error");        
                            }
                            break;
                    }

                    // if(Runtime.__DB.ContainsKey(argID))
                    // {
                    //     object _out;
                    //     Runtime.__DB.TryRemove(argID, out _out);
                    // }
                }
            }
        }

        private readonly object objLock_getJavaArray_1 = new object();
        private unsafe JVMObject getJavaArray(void* pEnv, void* pNetBridgeClass, object[] array)
        {
            // NEED TO REGISTED GC EVENT IN JVM FOR ARGUMENTS
            // lock(objLock_getJavaArray_1)
            // // lock(objLock_getJavaParameters)
            {
                if(true)
                {
                    Array sub = array as Array;
                    // Runtime.GetID(sub, false);

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
                            if(Runtime.NewBooleanArray( pEnv, arrLength, &pJArray ) != 0)
                                throw new Exception(Runtime.GetException(pEnv));
                            break;

                        case "B":
                            if(Runtime.NewByteArray( pEnv, arrLength, &pJArray ) != 0)
                                throw new Exception(Runtime.GetException(pEnv));
                            break;

                        case "C":
                            if(Runtime.NewCharArray( pEnv, arrLength, &pJArray ) != 0)
                                throw new Exception(Runtime.GetException(pEnv));
                            break;

                        case "S":
                            if(Runtime.NewShortArray( pEnv, arrLength, &pJArray ) != 0)
                                throw new Exception(Runtime.GetException(pEnv));
                            break;

                        case "I":
                            if(Runtime.NewIntArray( pEnv, arrLength, &pJArray ) != 0)
                                throw new Exception(Runtime.GetException(pEnv));
                            break;

                        case "J":
                            if(Runtime.NewLongArray( pEnv, arrLength, &pJArray ) != 0)
                                throw new Exception(Runtime.GetException(pEnv));
                            break;

                        case "F":
                            if(Runtime.NewFloatArray( pEnv, arrLength, &pJArray ) != 0)
                                throw new Exception(Runtime.GetException(pEnv));
                            break;

                        case "D":
                            if(Runtime.NewDoubleArray( pEnv, arrLength, &pJArray ) != 0)
                                throw new Exception(Runtime.GetException(pEnv));
                            break;

                        default:
                            isObject = true;

                            if(arrLength == 0)
                                return null;

                            if(!cls.Contains("java/lang/String"))
                                cls = "java/lang/Object";


                            if(Runtime.NewObjectArray( pEnv, arrLength, cls, &pJArray ) != 0)
                                throw new Exception(Runtime.GetException(pEnv));
                            break;
                    }

                    
                    for(int ii = 0; ii < arrLength; ii++)
                    {
                        var sub_element = sub.GetValue(ii);
                        if(sub_element == null)
                            Runtime.SetObjectArrayElement(pEnv, pJArray, ii, IntPtr.Zero.ToPointer());

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
                            

                            // Runtime.__DB[subID] = sub_element;

                            var sub_type = sub_element.GetType();
                            switch(Type.GetTypeCode(sub_type))
                            { 
                                case TypeCode.Boolean:
                                    if(!isObject)
                                        Runtime.SetBooleanArrayElement(pEnv, pJArray, ii, (bool)sub_element);
                                    else
                                    {
                                        void* pObjBool;
                                        if(Runtime.NewBooleanObject(pEnv, (bool)sub_element, &pObjBool) != 0)
                                            throw new Exception(Runtime.GetException(pEnv));
                                        Runtime.SetObjectArrayElement(pEnv, pJArray, ii, pObjBool);
                                    }

                                    break;

                                case TypeCode.Byte:
                                    if(!isObject)
                                        Runtime.SetByteArrayElement(pEnv, pJArray, ii, (byte)sub_element);
                                    else
                                    {
                                        void* pObjB;
                                        if(Runtime.NewByteObject(pEnv, (byte)sub_element, &pObjB) != 0)
                                            throw new Exception(Runtime.GetException(pEnv));
                                        Runtime.SetObjectArrayElement(pEnv, pJArray, ii, pObjB);
                                    }
                                    break;

                                case TypeCode.Char:
                                    if(!isObject)
                                        Runtime.SetCharArrayElement(pEnv, pJArray, ii, (char)sub_element);
                                    else
                                    {
                                        void* pObjC;
                                        if(Runtime.NewCharacterObject(pEnv, (char)sub_element, &pObjC) != 0)
                                            throw new Exception(Runtime.GetException(pEnv));
                                        Runtime.SetObjectArrayElement(pEnv, pJArray, ii, pObjC);
                                    }
                                    break;

                                case TypeCode.Int16:
                                    if(!isObject)
                                        Runtime.SetShortArrayElement(pEnv, pJArray, ii, (short)sub_element);
                                    else
                                    {
                                        void* pObjS;
                                        if(Runtime.NewShortObject(pEnv, (short)sub_element, &pObjS) != 0)
                                            throw new Exception(Runtime.GetException(pEnv));
                                        Runtime.SetObjectArrayElement(pEnv, pJArray, ii, pObjS);
                                    }
                                    break;

                                case TypeCode.Int32: 
                                    if(!isObject)
                                        Runtime.SetIntArrayElement(pEnv, pJArray, ii, (int)sub_element);
                                    else
                                    {
                                        void* pObjI;
                                        if(Runtime.NewIntegerObject(pEnv, (int)sub_element, &pObjI) != 0)
                                            throw new Exception(Runtime.GetException(pEnv));
                                        Runtime.SetObjectArrayElement(pEnv, pJArray, ii, pObjI);
                                    }
                                    break;
                                    
                                case TypeCode.Int64:
                                    if(!isObject)
                                        Runtime.SetLongArrayElement(pEnv, pJArray, ii, (long)sub_element);
                                    else
                                    {
                                        void* pObjL;
                                        if(Runtime.NewLongObject(pEnv, (long)sub_element, &pObjL) != 0)
                                            throw new Exception(Runtime.GetException(pEnv));
                                        Runtime.SetObjectArrayElement(pEnv, pJArray, ii, pObjL);
                                    }
                                    break;

                                case TypeCode.Single:
                                    if(!isObject)
                                        Runtime.SetFloatArrayElement(pEnv, pJArray, ii, (float)sub_element);
                                    else
                                    {
                                        void* pObjF;
                                        if(Runtime.NewFloatObject(pEnv, (float)sub_element, &pObjF) != 0)
                                            throw new Exception(Runtime.GetException(pEnv));
                                        Runtime.SetObjectArrayElement(pEnv, pJArray, ii, pObjF);
                                    }
                                    break;

                                case TypeCode.Double:
                                    if(!isObject)
                                        Runtime.SetDoubleArrayElement(pEnv, pJArray, ii, (double)sub_element);
                                    else
                                    {
                                        void* pObjD;
                                        if(Runtime.NewDoubleObject(pEnv, (double)sub_element, &pObjD) != 0)
                                            throw new Exception(Runtime.GetException(pEnv));
                                        Runtime.SetObjectArrayElement(pEnv, pJArray, ii, pObjD);
                                    }
                                    break;

                                case TypeCode.String:
                                    void* string_arg_s = Runtime.GetJavaString(pEnv, (string)sub_element);
                                    Runtime.SetObjectArrayElement(pEnv, pJArray, ii, string_arg_s);
                                    break;

                                case TypeCode.DateTime:
                                    void* pDate = Runtime.GetJavaDateTime(pEnv, (DateTime)sub_element);
                                    Runtime.SetObjectArrayElement(pEnv, pJArray, ii, pDate);
                                    break;

                                default:

                                    var subID = Runtime.GetID(sub_element, true); //IMPORTANT
                                    if(JVMDelegate.DB.ContainsKey(subID))
                                    {
                                        JVMDelegate jobj = sub_element as JVMDelegate; 
                                        void* ptr = (void *)(jobj.Pointer);
                                        Runtime.SetObjectArrayElement(pEnv, pJArray, ii, ptr);
                                        Runtime.RegisterJVMObject(pEnv, subID, ptr);
                                    }

                                    else if(Runtime.DB.ContainsKey(subID))
                                    {
                                        // void* ptr_res = (void *)(sub_element.GetHashCode());

                                        void*  pGetCLRObjectMethod;
                                        if(Runtime.GetStaticMethodID( pEnv, pNetBridgeClass, "GetCLRObject", "(I)Lapp/quant/clr/CLRObject;", &pGetCLRObjectMethod ) != 0)
                                            throw new Exception(Runtime.GetException(pEnv));

                                        // void** pAr_len = stackalloc void*[1];
                                        object[] pAr_len_data = new object[]{ subID };
                                        // getJavaParameters(pEnv, ref pAr_len, pAr_len_data);
                                        void** pAr_len = (void**)(new StructWrapper(pEnv, pAr_len_data)).Ptr;

                                        void*  pGetCLRObject;
                                        if(Runtime.CallStaticObjectMethod( pEnv, pNetBridgeClass, pGetCLRObjectMethod, &pGetCLRObject, 1, pAr_len) != 0)
                                            throw new Exception(Runtime.GetException(pEnv));

                                        Runtime.SetObjectArrayElement(pEnv, pJArray, ii, pGetCLRObject);
                                        Runtime.RegisterJVMObject(pEnv, subID, pGetCLRObject);
                                    }

                                    else if(sub_element is JVMTuple)
                                    {
                                        JVMTuple jobj = sub_element as JVMTuple; 
                                        // void* ptr = (void *)(jobj.jVMObject.Pointer);
                                        void* ptr = Runtime.GetJVMObject(pEnv, pNetBridgeClass, jobj.jVMObject.JavaHashCode);
                                        Runtime.SetObjectArrayElement(pEnv, pJArray, ii, ptr);
                                        Runtime.RegisterJVMObject(pEnv, jobj.jVMObject.JavaHashCode, ptr);
                                    }
                                    else if(sub_element is IJVMTuple)
                                    {
                                        IJVMTuple jobj = sub_element as IJVMTuple; 
                                        // void* ptr = (void *)(jobj.JVMObject.Pointer);
                                        void* ptr = Runtime.GetJVMObject(pEnv, pNetBridgeClass, jobj.JVMObject.JavaHashCode);
                                        Runtime.SetObjectArrayElement(pEnv, pJArray, ii, ptr);
                                        Runtime.RegisterJVMObject(pEnv, jobj.JVMObject.JavaHashCode, ptr);
                                    }

                                    else if(sub_element is JVMObject)
                                    {
                                        JVMObject jobj = sub_element as JVMObject; 
                                        // void* ptr = (void *)(jobj.Pointer);
                                        void* ptr = Runtime.GetJVMObject(pEnv, pNetBridgeClass, jobj.JavaHashCode);
                                        Runtime.SetObjectArrayElement(pEnv, pJArray, ii, ptr);
                                        Runtime.RegisterJVMObject(pEnv, jobj.JavaHashCode, ptr);
                                    }

                                    else if(sub_element is Array)
                                    {
                                        Console.WriteLine(" ARRAY( " + ii + "): " + sub_element);
                                        Array _sub_array = sub_element as Array;

                                        object[] sub_array = _sub_array.Cast<object>().ToArray();
                                        // JVMObject javaArray = getJavaArray(sub_array);
                                        JVMObject javaArray = getJavaArray(pEnv, pNetBridgeClass, sub_array);

                                        void* ptr = Runtime.GetJVMObject(pEnv, pNetBridgeClass, javaArray.JavaHashCode);
                                        Runtime.SetObjectArrayElement(pEnv, pJArray, ii, ptr);//javaArray.Pointer.ToPointer());
                                        Runtime.RegisterJVMObject(pEnv, javaArray.JavaHashCode, ptr);
                                    }


                                    else if(sub_element is IEnumerable<object>)
                                    {
                                        // void* ptr_res = (void *)(sub_element.GetHashCode());

                                        // void** pAr_len = stackalloc void*[2];
                                        object[] pAr_len_data = new object[]{ sub_element.GetType().ToString(), subID };
                                        // getJavaParameters(pEnv, ref pAr_len, pAr_len_data);
                                        void** pAr_len = (void**)(new StructWrapper(pEnv, pAr_len_data)).Ptr;

                                        void* pObj;
                                        void* CLRObjClass;
                                        void*  pLoadClassMethod; // The executed method struct
                                        if(Runtime.FindClass( pEnv, "app/quant/clr/CLRIterable", &CLRObjClass) == 0)
                                        {
                                            void* pClass;
                                            if(Runtime.NewObjectP( pEnv, CLRObjClass, "(Ljava/lang/String;I)V", 2, pAr_len, &pObj ) == 0)
                                            {
                                                //Runtime.GetID(pEnv, pObj);
                                                // Runtime.RegisterJVMObject(pEnv, Runtime.GetID(pEnv, pObj), pObj);
                                                // Runtime.DB.TryAdd(res.GetHashCode(), res);
                                                if(!(res is JVMObject) && !(res is IJVMTuple))
                                                    // Runtime.DB[subID] = res;
                                                    Runtime.DB[subID] = new WeakReference(res);
                                                Runtime.SetObjectArrayElement(pEnv, pJArray, ii, pObj);
                                                Runtime.RegisterJVMObject(pEnv, subID, pObj);

                                                // Runtime.RemoveID(subID);
                                                //return pObj;
                                            }
                                            else
                                                throw new Exception(Runtime.GetException(pEnv));
                                        }
                                        else
                                            throw new Exception(Runtime.GetException(pEnv));
                                    }

                                    

                                    else if(res is IEnumerator<object>)
                                    {
                                        // void* ptr_res = (void *)(res.GetHashCode());

                                        // void** pAr_len = stackalloc void*[2];
                                        object[] pAr_len_data = new object[]{ res.GetType().ToString(), subID };
                                        // getJavaParameters(pEnv, ref pAr_len, pAr_len_data);
                                        void** pAr_len = (void**)(new StructWrapper(pEnv, pAr_len_data)).Ptr;

                                        void* pObj;
                                        void* CLRObjClass;
                                        void*  pLoadClassMethod; // The executed method struct
                                        if(Runtime.FindClass( pEnv, "app/quant/clr/CLRIterator", &CLRObjClass) == 0)
                                        {
                                            void* pClass;
                                            if(Runtime.NewObjectP( pEnv, CLRObjClass, "(Ljava/lang/String;I)V", 2, pAr_len, &pObj ) == 0)
                                            {
                                                //Runtime.GetID(pEnv, pObj);
                                                // Runtime.RegisterJVMObject(pEnv, Runtime.GetID(pEnv, pObj) ,pObj);
                                                // Runtime.DB.TryAdd(res.GetHashCode(), res);
                                                if(!(res is JVMObject) && !(res is IJVMTuple))
                                                    // Runtime.DB[subID] = res;
                                                    Runtime.DB[subID] = new WeakReference(res);
                                                Runtime.SetObjectArrayElement(pEnv, pJArray, ii, pObj);
                                                Runtime.RegisterJVMObject(pEnv, subID, pObj);
                                            }
                                            else
                                                throw new Exception(Runtime.GetException(pEnv));
                                        }
                                        else
                                            throw new Exception(Runtime.GetException(pEnv));
                                    }

                                    else if(res is System.Func<Object, Object>)
                                    {
                                        // void* ptr_res = (void *)(res.GetHashCode());

                                        // void** pAr_len = stackalloc void*[2];
                                        object[] pAr_len_data = new object[]{ res.GetType().ToString(), subID };
                                        // getJavaParameters(pEnv, ref pAr_len, pAr_len_data);
                                        void** pAr_len = (void**)(new StructWrapper(pEnv, pAr_len_data)).Ptr;

                                        void* pObj;
                                        void* CLRObjClass;
                                        void*  pLoadClassMethod; // The executed method struct
                                        if(Runtime.FindClass( pEnv, "app/quant/clr/function/CLRFunction1", &CLRObjClass) == 0)
                                        {
                                            void* pClass;
                                            if(Runtime.NewObjectP( pEnv, CLRObjClass, "(Ljava/lang/String;I)V", 2, pAr_len, &pObj ) == 0)
                                            {
                                                //Runtime.GetID(pEnv, pObj);
                                                // Runtime.RegisterJVMObject(pEnv, Runtime.GetID(pEnv, pObj) ,pObj);
                                                // Runtime.DB.TryAdd(res.GetHashCode(), res);
                                                if(!(res is JVMObject) && !(res is IJVMTuple))
                                                    // Runtime.DB[subID] = res;
                                                    Runtime.DB[subID] = new WeakReference(res);
                                                Runtime.SetObjectArrayElement(pEnv, pJArray, ii, pObj);
                                                Runtime.RegisterJVMObject(pEnv, subID, pObj);
                                            }
                                            else
                                                throw new Exception(Runtime.GetException(pEnv));
                                        }
                                        else
                                            throw new Exception(Runtime.GetException(pEnv));
                                    }
                                    else if(res is System.Func<Object, Object, Object>)
                                    {
                                        // void* ptr_res = (void *)(res.GetHashCode());

                                        // void** pAr_len = stackalloc void*[2];
                                        object[] pAr_len_data = new object[]{ res.GetType().ToString(), subID };
                                        // getJavaParameters(pEnv, ref pAr_len, pAr_len_data);
                                        void** pAr_len = (void**)(new StructWrapper(pEnv, pAr_len_data)).Ptr;

                                        void* pObj;
                                        void* CLRObjClass;
                                        void*  pLoadClassMethod; // The executed method struct
                                        if(Runtime.FindClass( pEnv, "app/quant/clr/function/CLRFunction2", &CLRObjClass) == 0)
                                        {
                                            void* pClass;
                                            if(Runtime.NewObjectP( pEnv, CLRObjClass, "(Ljava/lang/String;I)V", 2, pAr_len, &pObj ) == 0)
                                            {
                                                //Runtime.GetID(pEnv, pObj);
                                                // Runtime.RegisterJVMObject(pEnv, Runtime.GetID(pEnv, pObj) ,pObj);
                                                // Runtime.DB.TryAdd(res.GetHashCode(), res);
                                                if(!(res is JVMObject) && !(res is IJVMTuple))
                                                    // Runtime.DB[subID] = res;
                                                    Runtime.DB[subID] = new WeakReference(res);
                                                Runtime.SetObjectArrayElement(pEnv, pJArray, ii, pObj);
                                                Runtime.RegisterJVMObject(pEnv, subID, pObj);
                                            }
                                            else
                                                throw new Exception(Runtime.GetException(pEnv));
                                        }
                                        else
                                            throw new Exception(Runtime.GetException(pEnv));
                                    }
                                    else if(res is System.Func<Object, Object, Object, Object>)
                                    {
                                        // void* ptr_res = (void *)(res.GetHashCode());

                                        // void** pAr_len = stackalloc void*[2];
                                        object[] pAr_len_data = new object[]{ res.GetType().ToString(), subID };
                                        // getJavaParameters(pEnv, ref pAr_len, pAr_len_data);
                                        void** pAr_len = (void**)(new StructWrapper(pEnv, pAr_len_data)).Ptr;

                                        void* pObj;
                                        void* CLRObjClass;
                                        void*  pLoadClassMethod; // The executed method struct
                                        if(Runtime.FindClass( pEnv, "app/quant/clr/function/CLRFunction3", &CLRObjClass) == 0)
                                        {
                                            void* pClass;
                                            if(Runtime.NewObjectP( pEnv, CLRObjClass, "(Ljava/lang/String;I)V", 2, pAr_len, &pObj ) == 0)
                                            {
                                                //Runtime.GetID(pEnv, pObj);
                                                // Runtime.RegisterJVMObject(pEnv, Runtime.GetID(pEnv, pObj) ,pObj);
                                                // Runtime.DB.TryAdd(res.GetHashCode(), res);
                                                if(!(res is JVMObject) && !(res is IJVMTuple))
                                                    // Runtime.DB[subID] = res;
                                                    Runtime.DB[subID] = new WeakReference(res);
                                                Runtime.SetObjectArrayElement(pEnv, pJArray, ii, pObj);
                                                Runtime.RegisterJVMObject(pEnv, subID, pObj);
                                            }
                                            else
                                                throw new Exception(Runtime.GetException(pEnv));
                                        }
                                        else
                                            throw new Exception(Runtime.GetException(pEnv));
                                    }
                                    else if(res is System.Func<Object, Object, Object, Object, Object>)
                                    {
                                        // void* ptr_res = (void *)(res.GetHashCode());

                                        // void** pAr_len = stackalloc void*[2];
                                        object[] pAr_len_data = new object[]{ res.GetType().ToString(), subID };
                                        // getJavaParameters(pEnv, ref pAr_len, pAr_len_data);
                                        void** pAr_len = (void**)(new StructWrapper(pEnv, pAr_len_data)).Ptr;

                                        void* pObj;
                                        void* CLRObjClass;
                                        void*  pLoadClassMethod; // The executed method struct
                                        if(Runtime.FindClass( pEnv, "app/quant/clr/function/CLRFunction4", &CLRObjClass) == 0)
                                        {
                                            void* pClass;
                                            if(Runtime.NewObjectP( pEnv, CLRObjClass, "(Ljava/lang/String;I)V", 2, pAr_len, &pObj ) == 0)
                                            {
                                                //Runtime.GetID(pEnv, pObj);
                                                // Runtime.RegisterJVMObject(pEnv, Runtime.GetID(pEnv, pObj) ,pObj);
                                                // Runtime.DB.TryAdd(res.GetHashCode(), res);
                                                if(!(res is JVMObject) && !(res is IJVMTuple))
                                                    // Runtime.DB[subID] = res;
                                                    Runtime.DB[subID] = new WeakReference(res);
                                                Runtime.SetObjectArrayElement(pEnv, pJArray, ii, pObj);
                                                Runtime.RegisterJVMObject(pEnv, subID, pObj);
                                            }
                                            else
                                                throw new Exception(Runtime.GetException(pEnv));
                                        }
                                        else
                                            throw new Exception(Runtime.GetException(pEnv));
                                    }
                                    else if(res is System.Func<Object, Object, Object, Object, Object, Object>)
                                    {
                                        // void* ptr_res = (void *)(res.GetHashCode());

                                        // void** pAr_len = stackalloc void*[2];
                                        object[] pAr_len_data = new object[]{ res.GetType().ToString(), subID };
                                        // getJavaParameters(pEnv, ref pAr_len, pAr_len_data);
                                        void** pAr_len = (void**)(new StructWrapper(pEnv, pAr_len_data)).Ptr;

                                        void* pObj;
                                        void* CLRObjClass;
                                        void*  pLoadClassMethod; // The executed method struct
                                        if(Runtime.FindClass( pEnv, "app/quant/clr/function/CLRFunction5", &CLRObjClass) == 0)
                                        {
                                            void* pClass;
                                            if(Runtime.NewObjectP( pEnv, CLRObjClass, "(Ljava/lang/String;I)V", 2, pAr_len, &pObj ) == 0)
                                            {
                                                //Runtime.GetID(pEnv, pObj);
                                                // Runtime.RegisterJVMObject(pEnv, Runtime.GetID(pEnv, pObj) ,pObj);
                                                // Runtime.DB.TryAdd(res.GetHashCode(), res);
                                                if(!(res is JVMObject) && !(res is IJVMTuple))
                                                    // Runtime.DB[subID] = res;
                                                    Runtime.DB[subID] = new WeakReference(res);
                                                Runtime.SetObjectArrayElement(pEnv, pJArray, ii, pObj);
                                                Runtime.RegisterJVMObject(pEnv, subID, pObj);
                                            }
                                            else
                                                throw new Exception(Runtime.GetException(pEnv));
                                        }
                                        else
                                            throw new Exception(Runtime.GetException(pEnv));
                                    }
                                    else if(res is System.Func<Object, Object, Object, Object, Object, Object, Object>)
                                    {
                                        // void* ptr_res = (void *)(res.GetHashCode());

                                        // void** pAr_len = stackalloc void*[2];
                                        object[] pAr_len_data = new object[]{ res.GetType().ToString(), subID };
                                        // getJavaParameters(pEnv, ref pAr_len, pAr_len_data);
                                        void** pAr_len = (void**)(new StructWrapper(pEnv, pAr_len_data)).Ptr;

                                        void* pObj;
                                        void* CLRObjClass;
                                        void*  pLoadClassMethod; // The executed method struct
                                        if(Runtime.FindClass( pEnv, "app/quant/clr/function/CLRFunction6", &CLRObjClass) == 0)
                                        {
                                            void* pClass;
                                            if(Runtime.NewObjectP( pEnv, CLRObjClass, "(Ljava/lang/String;I)V", 2, pAr_len, &pObj ) == 0)
                                            {
                                                //Runtime.GetID(pEnv, pObj);
                                                // Runtime.RegisterJVMObject(pEnv, Runtime.GetID(pEnv, pObj) ,pObj);
                                                // Runtime.DB.TryAdd(res.GetHashCode(), res);
                                                if(!(res is JVMObject) && !(res is IJVMTuple))
                                                    // Runtime.DB[subID] = res;
                                                    Runtime.DB[subID] = new WeakReference(res);
                                                Runtime.SetObjectArrayElement(pEnv, pJArray, ii, pObj);
                                                Runtime.RegisterJVMObject(pEnv, subID, pObj);
                                            }
                                            else
                                                throw new Exception(Runtime.GetException(pEnv));
                                        }
                                        else
                                            throw new Exception(Runtime.GetException(pEnv));
                                    }
                                    else if(res is System.Func<Object, Object, Object, Object, Object, Object, Object, Object>)
                                    {
                                        // void* ptr_res = (void *)(res.GetHashCode());

                                        // void** pAr_len = stackalloc void*[2];
                                        object[] pAr_len_data = new object[]{ res.GetType().ToString(), subID };
                                        // getJavaParameters(pEnv, ref pAr_len, pAr_len_data);
                                        void** pAr_len = (void**)(new StructWrapper(pEnv, pAr_len_data)).Ptr;
                                        

                                        void* pObj;
                                        void* CLRObjClass;
                                        void*  pLoadClassMethod; // The executed method struct
                                        if(Runtime.FindClass( pEnv, "app/quant/clr/function/CLRFunction7", &CLRObjClass) == 0)
                                        {
                                            void* pClass;
                                            if(Runtime.NewObjectP( pEnv, CLRObjClass, "(Ljava/lang/String;I)V", 2, pAr_len, &pObj ) == 0)
                                            {
                                                //Runtime.GetID(pEnv, pObj);
                                                // Runtime.RegisterJVMObject(pEnv, Runtime.GetID(pEnv, pObj) ,pObj);
                                                // Runtime.DB.TryAdd(res.GetHashCode(), res);
                                                if(!(res is JVMObject) && !(res is IJVMTuple))
                                                    // Runtime.DB[subID] = res;
                                                    Runtime.DB[subID] = new WeakReference(res);
                                                Runtime.SetObjectArrayElement(pEnv, pJArray, ii, pObj);
                                                Runtime.RegisterJVMObject(pEnv, subID, pObj);
                                            }
                                            else
                                                throw new Exception(Runtime.GetException(pEnv));
                                        }
                                        else
                                            throw new Exception(Runtime.GetException(pEnv));
                                    }
                                    else if(res is System.Func<Object, Object, Object, Object, Object, Object, Object, Object, Object>)
                                    {
                                        // void* ptr_res = (void *)(res.GetHashCode());

                                        // void** pAr_len = stackalloc void*[2];
                                        object[] pAr_len_data = new object[]{ res.GetType().ToString(), subID };
                                        // getJavaParameters(pEnv, ref pAr_len, pAr_len_data);
                                        void** pAr_len = (void**)(new StructWrapper(pEnv, pAr_len_data)).Ptr;

                                        void* pObj;
                                        void* CLRObjClass;
                                        void*  pLoadClassMethod; // The executed method struct
                                        if(Runtime.FindClass( pEnv, "app/quant/clr/function/CLRFunction8", &CLRObjClass) == 0)
                                        {
                                            void* pClass;
                                            if(Runtime.NewObjectP( pEnv, CLRObjClass, "(Ljava/lang/String;I)V", 2, pAr_len, &pObj ) == 0)
                                            {
                                                //Runtime.GetID(pEnv, pObj);
                                                // Runtime.RegisterJVMObject(pEnv, Runtime.GetID(pEnv, pObj) ,pObj);
                                                // Runtime.DB.TryAdd(res.GetHashCode(), res);
                                                if(!(res is JVMObject) && !(res is IJVMTuple))
                                                    // Runtime.DB[subID] = res;
                                                    Runtime.DB[subID] = new WeakReference(res);
                                                Runtime.SetObjectArrayElement(pEnv, pJArray, ii, pObj);
                                                Runtime.RegisterJVMObject(pEnv, subID, pObj);
                                            }
                                            else
                                                throw new Exception(Runtime.GetException(pEnv));
                                        }
                                        else
                                            throw new Exception(Runtime.GetException(pEnv));
                                    }
                                    else if(res is System.Func<Object, Object, Object, Object, Object, Object, Object, Object, Object, Object>)
                                    {
                                        // void* ptr_res = (void *)(res.GetHashCode());

                                        // void** pAr_len = stackalloc void*[2];
                                        object[] pAr_len_data = new object[]{ res.GetType().ToString(), subID };
                                        // getJavaParameters(pEnv, ref pAr_len, pAr_len_data);
                                        void** pAr_len = (void**)(new StructWrapper(pEnv, pAr_len_data)).Ptr;

                                        void* pObj;
                                        void* CLRObjClass;
                                        void*  pLoadClassMethod; // The executed method struct
                                        if(Runtime.FindClass( pEnv, "app/quant/clr/function/CLRFunction9", &CLRObjClass) == 0)
                                        {
                                            void* pClass;
                                            if(Runtime.NewObjectP( pEnv, CLRObjClass, "(Ljava/lang/String;I)V", 2, pAr_len, &pObj ) == 0)
                                            {
                                                //Runtime.GetID(pEnv, pObj);
                                                // Runtime.RegisterJVMObject(pEnv, Runtime.GetID(pEnv, pObj) ,pObj);
                                                // Runtime.DB.TryAdd(res.GetHashCode(), res);
                                                if(!(res is JVMObject) && !(res is IJVMTuple))
                                                    // Runtime.DB[subID] = res;
                                                    Runtime.DB[subID] = new WeakReference(res);
                                                Runtime.SetObjectArrayElement(pEnv, pJArray, ii, pObj);
                                                Runtime.RegisterJVMObject(pEnv, subID, pObj);
                                            }
                                            else
                                                throw new Exception(Runtime.GetException(pEnv));
                                        }
                                        else
                                            throw new Exception(Runtime.GetException(pEnv));
                                    }
                                    else if(res is System.Func<Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object>)
                                    {
                                        // void* ptr_res = (void *)(res.GetHashCode());

                                        // void** pAr_len = stackalloc void*[2];
                                        object[] pAr_len_data = new object[]{ res.GetType().ToString(), subID };
                                        // getJavaParameters(pEnv, ref pAr_len, pAr_len_data);
                                        void** pAr_len = (void**)(new StructWrapper(pEnv, pAr_len_data)).Ptr;

                                        void* pObj;
                                        void* CLRObjClass;
                                        void*  pLoadClassMethod; // The executed method struct
                                        if(Runtime.FindClass( pEnv, "app/quant/clr/function/CLRFunction10", &CLRObjClass) == 0)
                                        {
                                            void* pClass;
                                            if(Runtime.NewObjectP( pEnv, CLRObjClass, "(Ljava/lang/String;I)V", 2, pAr_len, &pObj ) == 0)
                                            {
                                                //Runtime.GetID(pEnv, pObj);
                                                // Runtime.RegisterJVMObject(pEnv, Runtime.GetID(pEnv, pObj) ,pObj);
                                                // Runtime.DB.TryAdd(res.GetHashCode(), res);
                                                if(!(res is JVMObject) && !(res is IJVMTuple))
                                                    // Runtime.DB[subID] = res;
                                                    Runtime.DB[subID] = new WeakReference(res);
                                                Runtime.SetObjectArrayElement(pEnv, pJArray, ii, pObj);
                                                Runtime.RegisterJVMObject(pEnv, subID, pObj);
                                            }
                                            else
                                                throw new Exception(Runtime.GetException(pEnv));
                                        }
                                        else
                                            throw new Exception(Runtime.GetException(pEnv));
                                    }
                                    else if(res is System.Func<Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object>)
                                    {
                                        // void* ptr_res = (void *)(res.GetHashCode());

                                        // void** pAr_len = stackalloc void*[2];
                                        object[] pAr_len_data = new object[]{ res.GetType().ToString(), subID };
                                        // getJavaParameters(pEnv, ref pAr_len, pAr_len_data);
                                        void** pAr_len = (void**)(new StructWrapper(pEnv, pAr_len_data)).Ptr;

                                        void* pObj;
                                        void* CLRObjClass;
                                        void*  pLoadClassMethod; // The executed method struct
                                        if(Runtime.FindClass( pEnv, "app/quant/clr/function/CLRFunction11", &CLRObjClass) == 0)
                                        {
                                            void* pClass;
                                            if(Runtime.NewObjectP( pEnv, CLRObjClass, "(Ljava/lang/String;I)V", 2, pAr_len, &pObj ) == 0)
                                            {
                                                //Runtime.GetID(pEnv, pObj);
                                                // Runtime.RegisterJVMObject(pEnv, Runtime.GetID(pEnv, pObj) ,pObj);
                                                // Runtime.DB.TryAdd(res.GetHashCode(), res);
                                                if(!(res is JVMObject) && !(res is IJVMTuple))
                                                    // Runtime.DB[subID] = res;
                                                    Runtime.DB[subID] = new WeakReference(res);
                                                Runtime.SetObjectArrayElement(pEnv, pJArray, ii, pObj);
                                                Runtime.RegisterJVMObject(pEnv, subID, pObj);
                                            }
                                            else
                                                throw new Exception(Runtime.GetException(pEnv));
                                        }
                                        else
                                            throw new Exception(Runtime.GetException(pEnv));
                                    }
                                    else if(res is System.Func<Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object>)
                                    {
                                        // void* ptr_res = (void *)(res.GetHashCode());

                                        // void** pAr_len = stackalloc void*[2];
                                        object[] pAr_len_data = new object[]{ res.GetType().ToString(), subID };
                                        // getJavaParameters(pEnv, ref pAr_len, pAr_len_data);
                                        void** pAr_len = (void**)(new StructWrapper(pEnv, pAr_len_data)).Ptr;

                                        void* pObj;
                                        void* CLRObjClass;
                                        void*  pLoadClassMethod; // The executed method struct
                                        if(Runtime.FindClass( pEnv, "app/quant/clr/function/CLRFunction12", &CLRObjClass) == 0)
                                        {
                                            void* pClass;
                                            if(Runtime.NewObjectP( pEnv, CLRObjClass, "(Ljava/lang/String;I)V", 2, pAr_len, &pObj ) == 0)
                                            {
                                                //Runtime.GetID(pEnv, pObj);
                                                // Runtime.RegisterJVMObject(pEnv, Runtime.GetID(pEnv, pObj) ,pObj);
                                                // Runtime.DB.TryAdd(res.GetHashCode(), res);
                                                if(!(res is JVMObject) && !(res is IJVMTuple))
                                                    // Runtime.DB[subID] = res;
                                                    Runtime.DB[subID] = new WeakReference(res);
                                                Runtime.SetObjectArrayElement(pEnv, pJArray, ii, pObj);
                                                Runtime.RegisterJVMObject(pEnv, subID, pObj);
                                            }
                                            else
                                                throw new Exception(Runtime.GetException(pEnv));
                                        }
                                        else
                                            throw new Exception(Runtime.GetException(pEnv));
                                    }
                                    else if(res is System.Func<Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object>)
                                    {
                                        // void* ptr_res = (void *)(res.GetHashCode());

                                        // void** pAr_len = stackalloc void*[2];
                                        object[] pAr_len_data = new object[]{ res.GetType().ToString(), subID };
                                        // getJavaParameters(pEnv, ref pAr_len, pAr_len_data);
                                        void** pAr_len = (void**)(new StructWrapper(pEnv, pAr_len_data)).Ptr;

                                        void* pObj;
                                        void* CLRObjClass;
                                        void*  pLoadClassMethod; // The executed method struct
                                        if(Runtime.FindClass( pEnv, "app/quant/clr/function/CLRFunction13", &CLRObjClass) == 0)
                                        {
                                            void* pClass;
                                            if(Runtime.NewObjectP( pEnv, CLRObjClass, "(Ljava/lang/String;I)V", 2, pAr_len, &pObj ) == 0)
                                            {
                                                //Runtime.GetID(pEnv, pObj);
                                                // Runtime.RegisterJVMObject(pEnv, Runtime.GetID(pEnv, pObj) ,pObj);
                                                // Runtime.DB.TryAdd(res.GetHashCode(), res);
                                                if(!(res is JVMObject) && !(res is IJVMTuple))
                                                    // Runtime.DB[subID] = res;
                                                    Runtime.DB[subID] = new WeakReference(res);
                                                Runtime.SetObjectArrayElement(pEnv, pJArray, ii, pObj);
                                                Runtime.RegisterJVMObject(pEnv, subID, pObj);
                                            }
                                            else
                                                throw new Exception(Runtime.GetException(pEnv));
                                        }
                                        else
                                            throw new Exception(Runtime.GetException(pEnv));
                                    }
                                    else if(res is System.Func<Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object>)
                                    {
                                        // void* ptr_res = (void *)(res.GetHashCode());

                                        // void** pAr_len = stackalloc void*[2];
                                        object[] pAr_len_data = new object[]{ res.GetType().ToString(), subID};
                                        // getJavaParameters(pEnv, ref pAr_len, pAr_len_data);
                                        void** pAr_len = (void**)(new StructWrapper(pEnv, pAr_len_data)).Ptr;

                                        void* pObj;
                                        void* CLRObjClass;
                                        void*  pLoadClassMethod; // The executed method struct
                                        if(Runtime.FindClass( pEnv, "app/quant/clr/function/CLRFunction14", &CLRObjClass) == 0)
                                        {
                                            void* pClass;
                                            if(Runtime.NewObjectP( pEnv, CLRObjClass, "(Ljava/lang/String;I)V", 2, pAr_len, &pObj ) == 0)
                                            {
                                                //Runtime.GetID(pEnv, pObj);
                                                // Runtime.RegisterJVMObject(pEnv, Runtime.GetID(pEnv, pObj) ,pObj);
                                                // Runtime.DB.TryAdd(res.GetHashCode(), res);
                                                if(!(res is JVMObject) && !(res is IJVMTuple))
                                                    // Runtime.DB[subID] = res;
                                                    Runtime.DB[subID] = new WeakReference(res);
                                                Runtime.SetObjectArrayElement(pEnv, pJArray, ii, pObj);
                                                Runtime.RegisterJVMObject(pEnv, subID, pObj);
                                            }
                                            else
                                                throw new Exception(Runtime.GetException(pEnv));
                                        }
                                        else
                                            throw new Exception(Runtime.GetException(pEnv));
                                    }
                                    else if(res is System.Func<Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object>)
                                    {
                                        // void* ptr_res = (void *)(res.GetHashCode());

                                        // void** pAr_len = stackalloc void*[2];
                                        object[] pAr_len_data = new object[]{ res.GetType().ToString(), subID };
                                        // getJavaParameters(pEnv, ref pAr_len, pAr_len_data);
                                        void** pAr_len = (void**)(new StructWrapper(pEnv, pAr_len_data)).Ptr;

                                        void* pObj;
                                        void* CLRObjClass;
                                        void*  pLoadClassMethod; // The executed method struct
                                        if(Runtime.FindClass( pEnv, "app/quant/clr/function/CLRFunction15", &CLRObjClass) == 0)
                                        {
                                            void* pClass;
                                            if(Runtime.NewObjectP( pEnv, CLRObjClass, "(Ljava/lang/String;I)V", 2, pAr_len, &pObj ) == 0)
                                            {
                                                //Runtime.GetID(pEnv, pObj);
                                                // Runtime.RegisterJVMObject(pEnv, Runtime.GetID(pEnv, pObj) ,pObj);
                                                // Runtime.DB.TryAdd(res.GetHashCode(), res);
                                                if(!(res is JVMObject) && !(res is IJVMTuple))
                                                    // Runtime.DB[subID] = res;
                                                    Runtime.DB[subID] = new WeakReference(res);
                                                Runtime.SetObjectArrayElement(pEnv, pJArray, ii, pObj);
                                                Runtime.RegisterJVMObject(pEnv, subID, pObj);
                                            }
                                            else
                                                throw new Exception(Runtime.GetException(pEnv));
                                        }
                                        else
                                            throw new Exception(Runtime.GetException(pEnv));
                                    }

                                    else
                                    {
                                        // void* ptr_res = (void *)(sub_element.GetHashCode());

                                        // void** pAr_len = stackalloc void*[2];
                                        object[] pAr_len_data = new object[]{ sub_element.GetType().ToString(), subID, false };
                                        // getJavaParameters(pEnv, ref pAr_len, pAr_len_data);
                                        void** pAr_len = (void**)(new StructWrapper(pEnv, pAr_len_data)).Ptr;

                                        void* pObj;
                                        void* CLRObjClass;
                                        // void*  pLoadClassMethod; // The executed method struct
                                        if(Runtime.FindClass( pEnv, "app/quant/clr/CLRObject", &CLRObjClass) == 0)
                                        {
                                            void* pClass;
                                            if(Runtime.NewObjectP( pEnv, CLRObjClass, "(Ljava/lang/String;IZ)V", 3, pAr_len, &pObj ) == 0)
                                            {
                                                //Runtime.GetID(pEnv, pObj);
                                                // Runtime.RegisterJVMObject(pEnv, Runtime.GetID(pEnv, pObj) ,pObj);
                                                Runtime.SetObjectArrayElement(pEnv, pJArray, ii, pObj);
                                                Runtime.RegisterJVMObject(pEnv, subID, pObj);
                                                // Runtime.DB.TryAdd(sub_element.GetHashCode(), sub_element);
                                                if(!(sub_element is JVMObject) && !(sub_element is IJVMTuple))
                                                    Runtime.DB[subID] = new WeakReference(sub_element);
                                            }
                                            else
                                                throw new Exception(Runtime.GetException(pEnv));
                                        }
                                        else
                                            throw new Exception(Runtime.GetException(pEnv));
                                    }
                                    break;
                            }

                            // sub_element.RegisterGCEvent(subID, delegate(object _obj, int _id)
                            // {
                            //     // Console.WriteLine("----========-----Object(" + _obj + ") with hash code " + _id + " recently collected: ");
                            //     Runtime.RemoveID(_id);
                            // });

                            // if(Runtime.__DB.ContainsKey(subID))
                            // {
                            //     object _out;
                            //     Runtime.__DB.TryRemove(subID, out _out);
                            // }
                            
                        }
                    }

                    int hashID = Runtime.GetJVMID(pEnv, pJArray, false);
                    Runtime.RegisterJVMObject(pEnv, hashID, pJArray);

                    // sub.RegisterGCEvent(hashID, delegate(object _obj, int _id)
                    // {
                    //     // Console.WriteLine("-----++++++----Object(" + _obj + ") with hash code " + _id + " recently collected: ");
                    //     Runtime.RemoveID(_id);
                    // });


                    var jo = new JVMObject(hashID, cls, false, "getJavaArray 7898"); // IMPORTANT FALSE

                    // Runtime.__DB[hashID] = jo;
                    // Runtime.GetID(jo, false);

                    return jo;
                
                }
                else
                    throw new Exception(Runtime.GetException(pEnv));
            }
        }

        private unsafe JVMObject getJavaArray(void* pEnv, void* pNetBridgeClass, IEnumerable<object> array)
        {
            // // lock(objLock_getJavaArray_3)
            {
                int arrLength = array.Count();
                object[] res = new object[arrLength];

                for(int i = 0; i < arrLength; i++)
                    res[i] = array.ElementAt(i);
                

                return getJavaArray(pEnv, pNetBridgeClass, res);
            }
        }

        private unsafe JVMObject getJavaArray(void* pEnv, void* pNetBridgeClass, Array array)
        {
            // // lock(objLock_getJavaArray_4)
            {
                int arrLength = array.Length;
                object[] res = new object[arrLength];

                for(int i = 0; i < arrLength; i++)
                    res[i] = array.GetValue(i);

                return getJavaArray(pEnv, pNetBridgeClass, res);
            }
        }
    }
}

namespace JVM
{
    public class Runtime : QuantApp.Kernel.JVM.Runtime {}
}
