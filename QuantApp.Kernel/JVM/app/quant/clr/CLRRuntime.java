/*
 * The MIT License (MIT)
 * Copyright (c) Arturo Rodriguez All rights reserved.
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

package app.quant.clr;

import java.io.*;
import java.lang.ref.WeakReference;
import java.lang.reflect.*;
import java.util.*;
import java.util.concurrent.*;
import java.util.function.*;

import java.net.*;

public class CLRRuntime 
{
    static 
    {
        // load the C-library
        /*
            Solaris: libJNIWrapper.so
            Linux: libJNIWrapper.so
            Win: JNIWrapper.dll
            Mac: libJNIWrapper.jnilib

        */
        System.loadLibrary("JNIWrapper");

        Thread thread = new Thread() {
            public void run() {
                while(true)
                {

                    try
                    {
                        Thread.sleep(10000);
                        for(Map.Entry<Integer, Integer> entry : CLRRuntime._DBID.entrySet()) {
                            Integer key = entry.getKey();
                            Integer value = entry.getValue();
                
                            if(CLRRuntime.DB.containsKey(key) && CLRRuntime.DB.get(key) == null)
                            {
                                CLRRuntime._DBID.remove(key);
                                CLRRuntime.DB.remove(key);
                            }
                        }
                
                        for(Map.Entry<Integer, Integer> entry : CLRRuntime._SDBID.entrySet()) {
                            Integer key = entry.getKey();
                            Integer value = entry.getValue();
                
                            if(CLRRuntime.DB.containsKey(key) && CLRRuntime.DB.get(key) == null)
                            {
                                CLRRuntime._SDBID.remove(key);
                                CLRRuntime.DB.remove(key);
                            }
                        }
                
                        for(Map.Entry<Integer, WeakReference> entry : CLRRuntime.DB.entrySet()) {
                            Integer key = entry.getKey();
                            WeakReference value = entry.getValue();
                            
                            if(value == null)
                                CLRRuntime.DB.remove(key);
                        }
                
                        for(Map.Entry<Integer, Object> entry : CLRRuntime.__DB.entrySet()) {
                            Integer key = entry.getKey();
                            Object value = entry.getValue();
                
                            if(value == null)
                                CLRRuntime.__DB.remove(key);
                        }
                    }
                    catch(Exception e){}
                }
            }
        };
        
        thread.start();
    }

     
    public CLRRuntime() {
    }

    public static String GetError(Exception ex)
    {
        StringWriter errors = new StringWriter();
        ex.printStackTrace(new PrintWriter(errors));
        return errors.toString();
    }

    public static CLRObject CreateInstance(String classname, Object... args)
    {
        Object[] oa = (Object[])args;
        int len = oa.length;
        int ptr = nativeCreateInstance(classname, len, oa);
        return new CLRObject(classname, ptr, false);
    }

    public static CLRObject CreateInstanceArr(String classname, Object[] args)
    {
        int len = args.length;
        int ptr = nativeCreateInstance(classname, len, args);
        return new CLRObject(classname, ptr, false);
    }

    public static CLRObject GetClass(String classname)
    {
        int ptr = nativeCreateInstance(classname, 0, new Object[0]);
        return new CLRObject(classname, ptr, false);
    }

    public static Object Invoke(int ptr, String funcname, Object... args)
    {
        try
        {
            Object[] oa = (Object[])args;
            int len = oa.length;
            Object res = nativeInvoke(ptr, funcname, len, oa);

            int id = GetID(res, false);
            GCInterceptor.RegisterGCEvent(res, id);

            if(res instanceof CLRObject)
                CLRObject.DB.put(id, new WeakReference(res));

            return res;
        }
        catch(Exception e)
        {
            System.out.println("JAVA Invoke(" + ptr + "): " + e + " " + args);
            e.printStackTrace(System.out);
            // throw e;
            return null;
        }
    }

    public static Object GetProperty(int ptr, String name)
    {
        Object res = nativeGetProperty(ptr, name);
        return res;
    }

    public static void SetProperty(int ptr, String name, Object value)
    {
        nativeSetProperty(ptr, name, new Object[]{ value });
    }

    public static CLRObject GetCLRObject(int ptr)
    {
        if(CLRObject.DB.containsKey(ptr))
        {
            CLRObject res = (CLRObject)CLRObject.DB.get(ptr).get();
    
            return res;
        }
        return null;
    }

    
    public static WeakHashMap<Object, Integer> DBID = new WeakHashMap<Object, Integer>();
    public static Map<Integer, Integer> _DBID = new HashMap<Integer, Integer>();

    public static WeakHashMap<Object, Integer> SDBID = new WeakHashMap<Object, Integer>();
    public static Map<Integer, Integer> _SDBID = new HashMap<Integer, Integer>();
    

    public synchronized static int GetID(Object obj, boolean cache)
    {
        if(obj == null || obj.hashCode() == 0)
        {
            return 0;
        }

        if(obj instanceof CLRObject)
        {
            return ((CLRObject)obj).Pointer;
        }

        else if(SDBID.containsKey(obj))
            return SDBID.get(obj);

        else if(!DBID.containsKey(obj))
        {
            int id = CreateID();
            GCInterceptor.RegisterGCEvent(obj, id);

            DBID.put(obj, id);
            DB.put(id, new WeakReference(obj));
            if(cache) //NEED TO CHECK... IMPORTANT!
                __DB.put(id, obj);
            return id;

            // for(int i = 0; i < 100; i++)
            // {
            //     UUID uuid = UUID.randomUUID();
            //     String randomUUIDString = uuid.toString();
            //     int id = randomUUIDString.hashCode();
            //     if(!_DBID.containsKey(id))
            //     {
            //         GCInterceptor.RegisterGCEvent(obj, id);

            //         _DBID.put(id, id);
            //         DBID.put(obj, id);
            //         DB.put(id, new WeakReference(obj));
            //         __DB.put(id, obj);
            //         return id;
            //     }
            // }
        }
        return DBID.get(obj);
    }

    public synchronized static int CreateID()
    {
        for(int i = 0; i < 100; i++)
        {
            UUID uuid = UUID.randomUUID();
            String randomUUIDString = uuid.toString();
            int id = randomUUIDString.hashCode();
            if(!_DBID.containsKey(id))
            {
                _DBID.put(id, id);
                return id;
            }
        }

        System.out.println("JVM ERROR CREATEID");
        return 0;
    }

    public static void RemoveID(int id)
    {
        if(__DB.containsKey(id))
        {
            __DB.remove(id);
        }

        if(CLRRuntime._DBID.containsKey(id))
        {
            CLRRuntime._DBID.remove(id);
        }
    }

    public static void SetID(Object obj, int id)
    {
        if(!SDBID.containsKey(obj))
        {
            if(!_SDBID.containsKey(id))
            {
                _SDBID.put(id, id);
                SDBID.put(obj, id);
                DB.put(id, new WeakReference(obj));

                GCInterceptor.RegisterGCEvent(obj, id);
            }
        }
    }

    public static Map<Integer, Object> __DB = new HashMap<Integer, Object>();
    public static Map<Integer, WeakReference> DB = new HashMap<Integer, WeakReference>();
    public static Object GetObject(int ptr)
    {
        if(DB.containsKey(ptr) && DB.get(ptr).get() != null)
        {
            Object obj = DB.get(ptr).get();
            if(!__DB.containsKey(ptr))
                __DB.put(ptr, obj); //IMPORTANT CHECK... NOT SURE YET

            GCInterceptor.RegisterGCEvent(obj, ptr);
            return obj;
        }
        
        System.out.println("------------------------------------JAVA GetObject ERROR: " + " " + ptr);
        return null;
    }
    public static void RegisterObject(int _ptr, Object obj)
    {
        GCInterceptor.RegisterGCEvent(obj, _ptr);
    }

    public static ConcurrentHashMap<Integer, WeakReference> Functions = new ConcurrentHashMap<Integer, WeakReference>();
    

    public static CLRObject CreateDelegate(String classname, Function<Object[], Object> func)
    {
        CLRDelegate del = new CLRDelegate(classname, func);
        int hash = GetID(del, true);
        CLRObject clr = new CLRObject(classname, hash, false);

        GCInterceptor.RegisterGCEvent(del, hash); //TEST REMOVE DELEGATE!

        if(!Functions.containsKey(hash))
            Functions.put(hash, new WeakReference(del));
        else
            System.out.println("Java CreateDelegate ERROR exists: " + hash);

        nativeRegisterFunc(classname, hash);
        return clr;
    }

    public static Object Python(Function<Object[], Object> func)
    {
        CLRObject runtime = CLRRuntime.GetClass("QuantApp.Kernel.JVM.Runtime");
        return runtime.Invoke("Python", CreateDelegate("System.Func`2[System.Object[], System.Object]", func));
    }

    public static CLRObject PyImport(String name)
    {
        return (CLRObject)CLRRuntime.GetClass("Python.Runtime.Py").Invoke("Import", name);
    }

    public static Object InvokeDelegate(CLRObject clrFunc, Object[] args)
    {
        try
        {
            int id = GetID(clrFunc, false);
            GCInterceptor.RegisterGCEvent(clrFunc, id);
            Object res = nativeInvokeFunc(id, args.length, args);
        
            if(res == null)
                return null;

            int rid = GetID(res, false);
            GCInterceptor.RegisterGCEvent(res, rid);

            if(res instanceof CLRObject)
                CLRObject.DB.put(rid, new WeakReference(res));
            return res;
        }
        catch(Exception e)
        {
            System.out.println("JAVA InvokeDelegate(" + clrFunc + "): " + e + " " + args);
            e.printStackTrace(System.out);

            return null;
        }
    }

    public static Object InvokeDelegate(int hashCode, Object[] args)
    {
        try
        {
            Object res = ((CLRDelegate)Functions.get(hashCode).get()).func.apply(args);

            int rid = GetID(res, false); 

            GCInterceptor.RegisterGCEvent(res, rid);

            if(res instanceof CLRObject)
                CLRObject.DB.put(rid, new WeakReference(res));
            return res;
        }
        catch (Exception e) 
        {
            System.out.println("JAVA InvokeDelegate(" + hashCode + "): " + e + " " + args);
            e.printStackTrace(System.out);
            return null;
        }
    }

    public static native int nativeCreateInstance(String classname, int len, Object[] args);
    public static native Object nativeInvoke(int ptr, String funcname, int len, Object[] args);
    public static native Object nativeRegisterFunc(String classname, int ptr);
    public static native Object nativeInvokeFunc(int ptr, int len, Object[] args);

    public static native Object nativeGetProperty(int ptr, String name);
    public static native void nativeSetProperty(int ptr, String name, Object[] value);
    public static native void nativeRemoveObject(int ptr);

    public static String TransformType(Type stype)
    {
        if(stype == null)
            return "Ljava/lang/Object;";
        String type = stype.toString();
        switch(type) 
        { 
            case "boolean": 
                return "Z";

            case "byte": 
                return "B";

            case "char": 
                return "C";

            case "short": 
                return "S";

            case "int": 
                return "I";
                
            case "long": 
                return "J";

            case "float": 
                return "F";

            case "double": 
                return "D";

            case "void": 
                return "V";

            default: 
                String res = type.replaceAll("interface ", "").replaceAll("\\.","/").replaceFirst("class ", "");
                
                if(res.length() > 1)
                {
                    
                    if(res.startsWith("["))
                    {
                        if(!res.endsWith(";") && res.length() > 2)
                            res += ";";
                    }
                    else
                    {
                        if(!res.endsWith(";"))
                            res += ";";
                    }

                    if(!res.startsWith("L") && !res.startsWith("["))
                        res = "L" + res;
                }
                return res;
        } 
    }

    public static String TransformNetType(Type stype)
    {
        if(stype == null)
            return "System.Object";
        String type = stype.toString();
        switch(type) 
        { 
            case "boolean": 
                return "System.Boolean";

            case "byte": 
                return "System.Byte";

            case "char": 
                return "System.Char";

            case "short": 
                return "System.Int16";

            case "int": 
                return "System.Int32";
                
            case "long": 
                return "System.Int64";

            case "float": 
                return "System.Single";

            case "double": 
                return "System.Double";

            case "void": 
                return "System.Void";

            default: 
                String res = type.replaceAll("interface ", "").replaceAll("\\.","/").replaceFirst("class ", "");
                
                if(res.contains("["))
                    return "System.Object[]";
                else
                    return "System.Object";
        } 
    }

    public static String[] Signatures(String clsName) throws Exception
    {
        try
        {
            List<String> signatures = new ArrayList<String>();  

            Class cls = null;

            try
            {
                cls = Class.forName(clsName);
            }
            catch(Exception e)
            {
                cls = ClassLoaders.containsKey(clsName) ? ClassLoaders.get(clsName).loadClass(clsName) : baseLoader.loadClass(clsName);
            }

            for (Class c = cls; c != null; c = c.getSuperclass()) 
            {
                for (Constructor constructor : c.getDeclaredConstructors()) 
                {
                    if(constructor.getName() == clsName)
                    {
                        String signature = "C/" + constructor.getName() + "(";
                        
                        Class<?>[] pType  = constructor.getParameterTypes();
                        for (int i = 0; i < pType.length; i++) 
                            signature += TransformType(pType[i]);
                        signature += ")";
                        signatures.add(signature);
                    }
                }

                // for(Class ci : c.getInterfaces())
                // {
                //     // if(ci.getName() == clsName)
                //     {
                //         for (Method method : ci.getDeclaredMethods()) 
                //         {
                //             boolean isStatic = java.lang.reflect.Modifier.isStatic(method.getModifiers());
                //             String signature = (isStatic ? "S-" : "") + "M/" + method.getName()+ "(";
                            
                //             Class<?>[] pType  = method.getParameterTypes();
                //             for (int i = 0; i < pType.length; i++) 
                //                 signature += TransformType(pType[i]);
                //             signature += ")" + TransformType(method.getReturnType());
                //             signatures.add(signature);
                //             System.out.println("JAVA INTERFACE: " + ci.getName() + " " + signature);
                //         }

                //         for (Field field : ci.getFields()) 
                //         {
                //             boolean isStatic = java.lang.reflect.Modifier.isStatic(field.getModifiers());
                //             String signature = (isStatic ? "S-" : "") + "F/" + field.getName() + "-" + TransformType(field.getType());
                //             signatures.add(signature);
                //         }
                //     }

                // }

                for (Method method : c.getDeclaredMethods()) 
                {
                    boolean isStatic = java.lang.reflect.Modifier.isStatic(method.getModifiers());
                    String signature = (isStatic ? "S-" : "") + "M/" + method.getName()+ "(";
                    
                    Class<?>[] pType  = method.getParameterTypes();
                    for (int i = 0; i < pType.length; i++) 
                        signature += TransformType(pType[i]);
                    signature += ")" + TransformType(method.getReturnType());
                    signatures.add(signature);
                }

                for (Field field : c.getFields()) 
                {
                    boolean isStatic = java.lang.reflect.Modifier.isStatic(field.getModifiers());
                    String signature = (isStatic ? "S-" : "") + "F/" + field.getName() + "-" + TransformType(field.getType());
                    signatures.add(signature);
                }
            }

            String signaturesArr[] = new String[signatures.size()];
            return signatures.toArray(signaturesArr);
        }
        catch(Exception e)
        {
            System.out.println("JVM Signature(" + clsName + "): " + e);
            return null;

        }
    }

    public static String[] ArrayClasses(Object[] arr)
    {
        if(arr == null)
        {
            System.out.println("JAVA ARRAY CLASSES ERROR");
        }
        String[] classes = new String[arr.length];
        for (int i = 0; i < arr.length; i++) 
        {
            if(arr[i] == null)
                classes[i] = null;
            else
            {
                if(
                    arr[i] instanceof Boolean
                    || arr[i] instanceof Byte
                    || arr[i] instanceof Character
                    || arr[i] instanceof Short
                    || arr[i] instanceof Integer
                    || arr[i] instanceof Long
                    || arr[i] instanceof Float
                    || arr[i] instanceof Double
                )

                    classes[i] = "prim-" + arr[i].getClass().getName() + "-" + arr[i];
                else
                    classes[i] = arr[i].getClass().getName();
            }
        }
            
        return classes;
    }


    private static URLClassLoader baseLoader;

    public static void SetBaseClassPath(String[] files) throws Exception
    {
        ArrayList<URL> classLoaderUrls = new ArrayList<URL>();
        for (int i = 0; i < files.length; i++) 
            classLoaderUrls.add((new File(files[i])).toURI().toURL());
        
        
        if(baseLoader != null)
            for(URL url : baseLoader.getURLs())
                classLoaderUrls.add(url);

        baseLoader = new URLClassLoader(classLoaderUrls.toArray(new URL[0]));
    }

    private static HashMap<String, URLClassLoader> ClassLoaders = new HashMap<String, URLClassLoader>();

    public static Class LoadClass(String[] names) throws Exception
    {
        return LoadClass(false, names);
    }

    public static Class LoadClass(boolean force, String[] names) throws Exception
    {
        
        String name = names[0];

        if(!force && ClassLoaders.containsKey(name))
            return ClassLoaders.get(name).loadClass(name);

        if(names.length > 1)
        {
            ArrayList<URL> classLoaderUrls = new ArrayList<URL>();
            
            for (int i = 0; i < names.length - 1; i++)
                if(names[i + 1] != null) 
                    classLoaderUrls.add((new File(names[i + 1])).toURI().toURL());
            
            URLClassLoader urlClassLoader = new URLClassLoader(classLoaderUrls.toArray(new URL[0]), baseLoader);
            
            ClassLoaders.put(name, urlClassLoader);
            Class cls = urlClassLoader.loadClass(name);

            return cls;
        }
        
        return baseLoader.loadClass(name);
    }

    public static boolean isIterable(Object obj) 
    {
        if(obj == null)
            System.out.println("JAVA ERROR isIterable NULL");

        return isClass(Iterable.class, obj.getClass().getGenericSuperclass()) || isClass(scala.collection.Iterable.class, obj.getClass().getGenericSuperclass());
    }

    public static boolean isMap(Object obj) 
    {
        if(obj == null)
            System.out.println("JAVA ERROR isMap NULL");

        return isClass(Map.class, obj.getClass().getGenericSuperclass());
    }

    public static boolean isCollection(Object obj) 
    {
        if(obj == null)
            System.out.println("JAVA ERROR isCollection NULL");

        return isClass(Collection.class, obj.getClass().getGenericSuperclass());
    }

    public static boolean isClass(Class cls, Type type) 
    {
        if ( type instanceof Class && isClass(cls, ( Class ) type ) ) 
            return true;
        
        if ( type instanceof ParameterizedType ) 
            return isClass(cls, ( ( ParameterizedType ) type ).getRawType() );
        
        if ( type instanceof WildcardType ) 
        {
            Type[] upperBounds = ( ( WildcardType ) type ).getUpperBounds();
            return upperBounds.length != 0 && isClass( cls, upperBounds[0] );
        }

        return false;
    }
  
    private static boolean isClass(Class cls, Class<?> clazz) 
    {
        List<Class<?>> classes = new ArrayList<Class<?>>();
        computeClassHierarchy( clazz, classes );
        return classes.contains( cls );
    }

    private static void computeClassHierarchy(Class<?> clazz, List<Class<?>> classes) 
    {
        for ( Class current = clazz; current != null; current = current.getSuperclass() ) 
        {
            if ( classes.contains( current ) ) 
                return;
            
            classes.add( current );
            for ( Class currentInterface : current.getInterfaces() ) 
                computeClassHierarchy( currentInterface, classes );
            
        }
    }
}

