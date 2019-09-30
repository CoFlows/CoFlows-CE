/*
 * The MIT License (MIT)
 * Copyright (c) 2007-2019, Arturo Rodriguez All rights reserved.
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

#include <inttypes.h>
#include <jni.h>

#include "app_quant_clr_CLRRuntime.h"

#include <memory.h>
#include <stdlib.h>
#include <iostream>
#include <cstdio>

#include <limits.h>
#include <stdlib.h>
#include <string.h>
#include <set>
#include <dirent.h>
#include <sys/stat.h>

using namespace std;
extern "C" {

    static int g_nExitCode = 0;

    void system_exit(jint nCode)
    {
        g_nExitCode = nCode;
    }


    int MakeJavaVMInitArgs(char* classpath, char* libpath, void** ppArgs )
    {
        int nOptSize = 2;
        JavaVMInitArgs* pArgs    = new JavaVMInitArgs();
        JavaVMOption*   pOptions = new JavaVMOption[nOptSize];


        pOptions[0].optionString = new char[strlen("-Djava.class.path=")+strlen(classpath)+1];
        sprintf( pOptions[0].optionString, "-Djava.class.path=%s", classpath );
        
        pOptions[1].optionString = new char[strlen("-Djava.library.path=")+strlen(libpath)+1];
        sprintf( pOptions[1].optionString, "-Djava.library.path=%s", libpath );


        memset(pArgs, 0, sizeof(JavaVMInitArgs));
        pArgs->version = JNI_VERSION_1_6;

        pArgs->options = pOptions;
        pArgs->nOptions = nOptSize;
        pArgs->ignoreUnrecognized = JNI_TRUE;

        *ppArgs = pArgs;

        return 0;
    }

    /*
    Free the allocated JVM init argumets
    */

    void FreeJavaVMInitArgs( void* pArgs )
    {
        delete ((JavaVMInitArgs*)pArgs)->options[0].optionString;
        delete ((JavaVMInitArgs*)pArgs)->options;
        delete &pArgs;
    }

    /*
    Static wrapper on FindClass() JNI function.
    See the description in
    http://jre.sfbay/java/re/jdk/6/promoted/latest/docs/technotes/guides/jni/spec/functions.html#wp16027
    */

    int FindClass(JNIEnv* pEnv, const char* szClass, jclass* pClass )
    {
        *pClass = pEnv->FindClass( szClass );

        if(pEnv->ExceptionCheck() == JNI_TRUE)
        {
            //pEnv->ExceptionDescribe();
            return -1;
        }
        if(*pClass != NULL)
            return 0;
        else
            return -2;

    }

    int AttacheThread(JavaVM* pVM, void** pEnv)
    {
        int getEnvStat = pVM->GetEnv((void **)pEnv, JNI_VERSION_1_6);
        

        if (getEnvStat == JNI_EDETACHED) 
        {
            if (pVM->AttachCurrentThread((void **)pEnv, NULL) != 0) 
                return -2;
            else
                return 0;            
        } 
        else if (getEnvStat == JNI_OK) 
            return 0;
        else if (getEnvStat == JNI_EVERSION) 
            return -3;

        return -1;
    }

    int DetacheThread(JavaVM* pVM)
    {
        return pVM->DetachCurrentThread();
    }


    //object
    int NewObjectP(JNIEnv* pEnv, jclass cls, const char* szArgs, int len, void** pArgs, jobject* pobj)
    {
        jmethodID methodID = pEnv->GetMethodID(cls, "<init>", szArgs);
        if(pEnv->ExceptionCheck() == JNI_TRUE){
            // //pEnv->ExceptionDescribe();
            return -1;
        }

        void** args = (void**)malloc(sizeof(void *) * len);

        for(int i = 0; i < len; i++)
            args[i] = pArgs[i];

        
        *pobj = pEnv->NewObjectA(cls, methodID, (const jvalue*)args);
        if(pEnv->ExceptionCheck() == JNI_TRUE){
            // //pEnv->ExceptionDescribe();
            free(args);
            return -1;
        }

        free(args);

        if( pobj != NULL )
            return 0;
        else
            return -2;
    }

    int NewObject(JNIEnv* pEnv, const char* szType, const char* szArgs, int len, void** pArgs, jobject* pobj)
    {
        jclass cls = pEnv->FindClass( szType );
        jmethodID methodID = pEnv->GetMethodID(cls, "<init>", szArgs);
        if(pEnv->ExceptionCheck() == JNI_TRUE){
            // //pEnv->ExceptionDescribe();
            return -1;
        }

        void** args = (void**)malloc(sizeof(void *) * len);

        for(int i = 0; i < len; i++)
            args[i] = pArgs[i];

        
        *pobj = pEnv->NewObjectA(cls, methodID, (const jvalue*)args);
        if(pEnv->ExceptionCheck() == JNI_TRUE){
            // //pEnv->ExceptionDescribe();
            free(args);
            return -1;
        }

        free(args);

        if( pobj != NULL )
            return 0;
        else
            return -2;
    }

    //bool object
    int NewBooleanObject(JNIEnv* pEnv, bool val, jobject* pobj)
    {
        jmethodID methodID = pEnv->GetMethodID(pEnv->FindClass( "java/lang/Boolean" ), "<init>", "(Z)V");
        if(pEnv->ExceptionCheck() == JNI_TRUE){
            // //pEnv->ExceptionDescribe();
            return -1;
        }
        
        *pobj = pEnv->NewObject(pEnv->FindClass( "java/lang/Boolean" ), methodID, val);
        if(pEnv->ExceptionCheck() == JNI_TRUE){
            // //pEnv->ExceptionDescribe();
            return -1;
        }

        if( pobj != NULL )
            return 0;
        else
            return -2;
    }

    //bool object
    int NewByteObject(JNIEnv* pEnv, jbyte val, jobject* pobj)
    {
        jmethodID methodID = pEnv->GetMethodID(pEnv->FindClass( "java/lang/Byte" ), "<init>", "(B)V");
        if(pEnv->ExceptionCheck() == JNI_TRUE){
            // //pEnv->ExceptionDescribe();
            return -1;
        }
        
        *pobj = pEnv->NewObject(pEnv->FindClass( "java/lang/Byte" ), methodID, val);

        if(pEnv->ExceptionCheck() == JNI_TRUE){
            // //pEnv->ExceptionDescribe();
            return -1;
        }

        if( pobj != NULL )
            return 0;
        else
            return -2;
    }

    //char object
    int NewCharacterObject(JNIEnv* pEnv, char val, jobject* pobj)
    {
        jmethodID methodID = pEnv->GetMethodID(pEnv->FindClass( "java/lang/Character" ), "<init>", "(C)V");
        if(pEnv->ExceptionCheck() == JNI_TRUE){
            // //pEnv->ExceptionDescribe();
            return -1;
        }
        
        *pobj = pEnv->NewObject(pEnv->FindClass( "java/lang/Character" ), methodID, val);

        if(pEnv->ExceptionCheck() == JNI_TRUE){
            // //pEnv->ExceptionDescribe();
            return -1;
        }

        if( pobj != NULL )
            return 0;
        else
            return -2;
    }

    //short object
    int NewShortObject(JNIEnv* pEnv, short val, jobject* pobj)
    {
        jmethodID methodID = pEnv->GetMethodID(pEnv->FindClass( "java/lang/Short" ), "<init>", "(S)V");
        if(pEnv->ExceptionCheck() == JNI_TRUE){
            // //pEnv->ExceptionDescribe();
            return -1;
        }
        
        *pobj = pEnv->NewObject(pEnv->FindClass( "java/lang/Short" ), methodID, val);
        
        if(pEnv->ExceptionCheck() == JNI_TRUE){
            // //pEnv->ExceptionDescribe();
            return -1;
        }

        if( pobj != NULL )
            return 0;
        else
            return -2;
    }

    //int object
    int NewIntegerObject(JNIEnv* pEnv, int val, jobject* pobj)
    {
        jmethodID methodID = pEnv->GetMethodID(pEnv->FindClass( "java/lang/Integer" ), "<init>", "(I)V");
        if(pEnv->ExceptionCheck() == JNI_TRUE){
            // //pEnv->ExceptionDescribe();
            return -1;
        }
        
        *pobj = pEnv->NewObject(pEnv->FindClass( "java/lang/Integer" ), methodID, val);
        
        if(pEnv->ExceptionCheck() == JNI_TRUE){
            // //pEnv->ExceptionDescribe();
            return -1;
        }

        if( pobj != NULL )
            return 0;
        else
            return -2;
    }

    //long object
    int NewLongObject(JNIEnv* pEnv, long val, jobject* pobj)
    {
        jmethodID methodID = pEnv->GetMethodID(pEnv->FindClass( "java/lang/Long" ), "<init>", "(J)V");
        if(pEnv->ExceptionCheck() == JNI_TRUE){
            // //pEnv->ExceptionDescribe();
            return -1;
        }
        
        *pobj = pEnv->NewObject(pEnv->FindClass( "java/lang/Long" ), methodID, val);
        
        if(pEnv->ExceptionCheck() == JNI_TRUE){
            // //pEnv->ExceptionDescribe();
            return -1;
        }

        if( pobj != NULL )
            return 0;
        else
            return -2;
    }

    //float object
    int NewFloatObject(JNIEnv* pEnv, float val, jobject* pobj)
    {
        jmethodID methodID = pEnv->GetMethodID(pEnv->FindClass( "java/lang/Float" ), "<init>", "(F)V");
        if(pEnv->ExceptionCheck() == JNI_TRUE){
            // //pEnv->ExceptionDescribe();
            return -1;
        }
        
        *pobj = pEnv->NewObject(pEnv->FindClass( "java/lang/Float" ), methodID, val);
        
        if(pEnv->ExceptionCheck() == JNI_TRUE){
            // //pEnv->ExceptionDescribe();
            return -1;
        }

        if( pobj != NULL )
            return 0;
        else
            return -2;
    }



    //double object
    int NewDoubleObject(JNIEnv* pEnv, double val, jobject* pobj)
    {
        jmethodID methodID = pEnv->GetMethodID(pEnv->FindClass( "java/lang/Double" ), "<init>", "(D)V");
        if(pEnv->ExceptionCheck() == JNI_TRUE){
            // //pEnv->ExceptionDescribe();
            return -1;
        }

        *pobj = pEnv->NewObject(pEnv->FindClass( "java/lang/Double" ), methodID, val);
        if(pEnv->ExceptionCheck() == JNI_TRUE){
            // //pEnv->ExceptionDescribe();
            return -1;
        }

        if( pobj != NULL )
            return 0;
        else
            return -2;
    }

    

    

    //Methods
    int GetStaticMethodID(JNIEnv* pEnv, jclass pClass, const char* szName, const char* szArgs, jmethodID* pMid)
    {
        *pMid = pEnv->GetStaticMethodID( pClass, szName, szArgs);

        if(pEnv->ExceptionCheck() == JNI_TRUE){
            // //pEnv->ExceptionDescribe();
            return -1;
        }

        if( *pMid != NULL )
            return 0;
        else
            return -2;
    }

    int GetMethodID(JNIEnv* pEnv, jobject pObj, const char* szName, const char* szArgs, jmethodID*  pMid)
    {
        jclass cls = pEnv->GetObjectClass(pObj);

        *pMid = pEnv->GetMethodID(cls, szName, szArgs);

        if(pEnv->ExceptionCheck() == JNI_TRUE)
        {
            // //pEnv->ExceptionDescribe();
            return -1;
        }

        if( *pMid != NULL )
            return 0;
        else
            return -2;
    }

    //Fields
    int GetStaticFieldID(JNIEnv* pEnv, jclass pClass, const char* szName, const char* sig, jfieldID* pFid)
    {
        *pFid = pEnv->GetStaticFieldID( pClass, szName, sig);

        if(pEnv->ExceptionCheck() == JNI_TRUE){
            // //pEnv->ExceptionDescribe();
            return -1;
        }

        if( *pFid != NULL )
            return 0;
        else
            return -2;
    }

    int GetFieldID(JNIEnv* pEnv, jobject pObj, const char* szName, const char* sig, jfieldID*  pFid)
    {
        jclass cls = pEnv->GetObjectClass(pObj);
        if(pEnv->ExceptionCheck() == JNI_TRUE){
            // //pEnv->ExceptionDescribe();
            return -1;
        }

        *pFid = pEnv->GetFieldID(cls, szName, sig);

        if(pEnv->ExceptionCheck() == JNI_TRUE)
        {
            //pEnv->ExceptionDescribe();
            return -1;
        }

        if( *pFid != NULL )
            return 0;
        else
            return -2;
    }

    
    //void
    int CallStaticVoidMethod(JNIEnv* pEnv, jclass pClass, jmethodID pMid, int len, void** pArgs)
    { 
        void** args = (void**)malloc(sizeof(void *) * len);

        for(int i = 0; i < len; i++)
            args[i] = pArgs[i];

        pEnv->CallStaticVoidMethodA( pClass, pMid, (const jvalue*)args);
        if( pEnv->ExceptionCheck() == JNI_TRUE )
        {
            // //pEnv->ExceptionDescribe();
            free(args);
            return -1;
        }
        
        free(args);
        return 0;
    }

    int CallVoidMethod(JNIEnv* pEnv, jclass pClass, jmethodID pMid, int len, void** pArgs)
    {
        void** args = (void**)malloc(sizeof(void *) * len);

        for(int i = 0; i < len; i++)
            args[i] = pArgs[i];

        pEnv->CallVoidMethodA( pClass, pMid, (const jvalue*)args);
        if( pEnv->ExceptionCheck() == JNI_TRUE )
        {
            // //pEnv->ExceptionDescribe();
            return -1;
        }

        free(args);
        return 0;
    }

    //object

    int GetObjectClass(JNIEnv* pEnv, jobject pobj, jclass* cls, jstring* clsname)
    {
        jclass _cls = pEnv->GetObjectClass(pobj);

        void** args = (void**)malloc(sizeof(void *) * 1);
        jmethodID pMid = pEnv->GetMethodID(_cls, "getClass", "()Ljava/lang/Class;");

        if(pEnv->ExceptionCheck() == JNI_TRUE){
            // //pEnv->ExceptionDescribe();

            free(args);
            return -1;
        }
        jobject jcls = pEnv->CallObjectMethodA(_cls, pMid, (const jvalue*)args);

        if(pEnv->ExceptionCheck() == JNI_TRUE){
            // //pEnv->ExceptionDescribe();
            free(args);
            return -1;
        }

        jclass __cls = pEnv->GetObjectClass(jcls);
        if(pEnv->ExceptionCheck() == JNI_TRUE){
            // //pEnv->ExceptionDescribe();
            free(args);
            return -1;
        }

        jmethodID pMid2 = pEnv->GetMethodID(__cls, "getName", "()Ljava/lang/String;");
        if(pEnv->ExceptionCheck() == JNI_TRUE){
            // //pEnv->ExceptionDescribe();
            free(args);
            return -1;
        }

        jobject jclsName = pEnv->CallObjectMethodA(_cls, pMid2, (const jvalue*)args);

        if( pEnv->ExceptionCheck() == JNI_TRUE )
        {
            // //pEnv->ExceptionDescribe();
            free(args);
            return -1;
        }

        *cls = _cls;
        *clsname = (jstring)jclsName;
        free(args);
        return 0;
    }
    
    int CallStaticObjectMethod(JNIEnv* pEnv, jclass pClass, jmethodID pMid, jobject* pobj, int len, void** pArgs)
    {
        void** args = (void**)malloc(sizeof(void *) * len);

        for(int i = 0; i < len; i++)
            args[i] = pArgs[i];

        jobject val = pEnv->CallStaticObjectMethodA( pClass, pMid, (const jvalue*)args);
        if( pEnv->ExceptionCheck() == JNI_TRUE )
        {
            // //pEnv->ExceptionDescribe();
            free(args);
            return -1;
        }
        *pobj = val;
        free(args);
        return 0;
    }
    int CallObjectMethod(JNIEnv* pEnv, jobject pObject, jmethodID pMid, jobject* pobj, int len, void** pArgs)
    {
        void** args = (void**)malloc(sizeof(void *) * len);

        for(int i = 0; i < len; i++)
            args[i] = pArgs[i];

        jobject val = pEnv->CallObjectMethodA( pObject, pMid, (const jvalue*)args);

        if( pEnv->ExceptionCheck() == JNI_TRUE )
        {
            // //pEnv->ExceptionDescribe();
            free(args);
            return -1;
        }
        
        *pobj = val;
        free(args);
        return 0;
    }

    int GetStaticObjectField(JNIEnv* pEnv, jclass pClass, jfieldID pMid, jobject* pobj)
    {
        jobject val = pEnv->GetStaticObjectField(pClass, pMid);
        
        if( pEnv->ExceptionCheck() == JNI_TRUE )
        {
            // //pEnv->ExceptionDescribe();
            return -1;
        }
        
        *pobj = val;
        return 0;
    }

    int GetObjectField(JNIEnv* pEnv, jobject pObject, jfieldID pMid, jobject* pobj)
    {
        jobject val = pEnv->GetObjectField( pObject, pMid);
        
        if( pEnv->ExceptionCheck() == JNI_TRUE )
        {
            // //pEnv->ExceptionDescribe();
            return -1;
        }
        
        *pobj = val;
        return 0;
    }

    int SetStaticObjectField(JNIEnv* pEnv, jclass pClass, jfieldID pMid, jobject val)
    {
        pEnv->SetStaticObjectField(pClass, pMid, val);
        
        if( pEnv->ExceptionCheck() == JNI_TRUE )
        {
            // //pEnv->ExceptionDescribe();
            return -1;
        }
        return 0;
    }

    int SetObjectField(JNIEnv* pEnv, jobject pObject, jfieldID pMid, jobject val)
    {
        pEnv->SetObjectField( pObject, pMid, val);
        
        if( pEnv->ExceptionCheck() == JNI_TRUE )
        {
            // //pEnv->ExceptionDescribe();
            return -1;
        }
        return 0;
    }

    //int
    int CallStaticIntMethod(JNIEnv* pEnv, jclass pClass, jmethodID pMid, int len, void** pArgs, int* res)
    {
        void** args = (void**)malloc(sizeof(void *) * len);

        for(int i = 0; i < len; i++)
            args[i] = pArgs[i];

        *res = pEnv->CallStaticIntMethodA(pClass, pMid, (const jvalue*)args);
        if(pEnv->ExceptionCheck() == JNI_TRUE){
            //pEnv->ExceptionDescribe();
            return -1;
        }
        free(args);
        return 0;

    }

    int CallIntMethod(JNIEnv* pEnv, jobject pObject, jmethodID pMid, int len, void** pArgs, int* res)
    {
        void** args = (void**)malloc(sizeof(void *) * len);

        for(int i = 0; i < len; i++)
            args[i] = pArgs[i];

        *res = pEnv->CallIntMethodA( pObject, pMid, (const jvalue*)args);

        if(pEnv->ExceptionCheck() == JNI_TRUE){
            //pEnv->ExceptionDescribe();
            free(args);
            return -1;
        }
        free(args);
        return 0;
    }

    int GetStaticIntField(JNIEnv* pEnv, jclass pClass, jfieldID pMid, int* res)
    {
        *res = pEnv->GetStaticIntField(pClass, pMid);
        if(pEnv->ExceptionCheck() == JNI_TRUE){
            //pEnv->ExceptionDescribe();
            return -1;
        }
        return 0;
    }

    int GetIntField(JNIEnv* pEnv, jobject pObject, jfieldID pMid, int* res)
    {
        *res = pEnv->GetIntField(pObject, pMid);
        if(pEnv->ExceptionCheck() == JNI_TRUE){
            //pEnv->ExceptionDescribe();
            return -1;
        }
        return 0;
    }

    int SetStaticIntField(JNIEnv* pEnv, jclass pClass, jfieldID pMid, int val)
    {
        
        pEnv->SetStaticIntField(pClass, pMid, val);
        
        if( pEnv->ExceptionCheck() == JNI_TRUE )
        {
            //pEnv->ExceptionDescribe();
            return -1;
        }
        return 0;
    }

    int SetIntField(JNIEnv* pEnv, jobject pObject, jfieldID pMid, int val)
    {
        pEnv->SetIntField( pObject, pMid, val);
        
        if( pEnv->ExceptionCheck() == JNI_TRUE )
        {
            //pEnv->ExceptionDescribe();
            return -1;
        }
        return 0;
    }


    //long
    int CallStaticLongMethod(JNIEnv* pEnv, jclass pClass, jmethodID pMid, int len, void** pArgs, long* val)
    {
        void** args = (void**)malloc(sizeof(void *) * len);

        for(int i = 0; i < len; i++)
            args[i] = pArgs[i];

        *val = pEnv->CallStaticLongMethodA( pClass, pMid, (const jvalue*)args);
        if(pEnv->ExceptionCheck() == JNI_TRUE){
            //pEnv->ExceptionDescribe();
            free(args);
            return -1;
        }
        free(args);
        return 0;
    }

    int CallLongMethod(JNIEnv* pEnv, jobject pObject, jmethodID pMid, int len, void** pArgs, long* val)
    {
        void** args = (void**)malloc(sizeof(void *) * len);

        for(int i = 0; i < len; i++)
            args[i] = pArgs[i];

        *val = pEnv->CallLongMethodA(pObject, pMid, (const jvalue*)args);
        if(pEnv->ExceptionCheck() == JNI_TRUE){
            //pEnv->ExceptionDescribe();
            free(args);
            return -1;
        }

        return 0;
    }

    int GetStaticLongField(JNIEnv* pEnv, jclass pClass, jfieldID pMid, long* val)
    {
        *val = pEnv->GetStaticLongField(pClass, pMid);
        if(pEnv->ExceptionCheck() == JNI_TRUE){
            //pEnv->ExceptionDescribe();
            return -1;
        }

        return 0;
    }

    int GetLongField(JNIEnv* pEnv, jobject pObject, jfieldID pMid, long* val)
    {
        *val = pEnv->GetLongField(pObject, pMid);
        if(pEnv->ExceptionCheck() == JNI_TRUE){
            //pEnv->ExceptionDescribe();
            return -1;
        }

        return 0;
    }

    int SetStaticLongField(JNIEnv* pEnv, jclass pClass, jfieldID pMid, long val)
    {
        pEnv->SetStaticLongField(pClass, pMid, val);
        
        if( pEnv->ExceptionCheck() == JNI_TRUE )
        {
            //pEnv->ExceptionDescribe();
            return -1;
        }
        return 0;
    }

    int SetLongField(JNIEnv* pEnv, jobject pObject, jfieldID pMid, long val)
    {
        pEnv->SetLongField( pObject, pMid, val);
        
        if( pEnv->ExceptionCheck() == JNI_TRUE )
        {
            //pEnv->ExceptionDescribe();
            return -1;
        }
        return 0;
    }

    //float
    int CallStaticFloatMethod(JNIEnv* pEnv, jclass pClass, jmethodID pMid, int len, void** pArgs, float* val)
    {
        void** args = (void**)malloc(sizeof(void *) * len);

        for(int i = 0; i < len; i++)
            args[i] = pArgs[i];

        *val = pEnv->CallStaticFloatMethodA( pClass, pMid, (const jvalue*)args);
        if(pEnv->ExceptionCheck() == JNI_TRUE){
            //pEnv->ExceptionDescribe();
            free(args);
            return -1;
        }

        return 0;
    }

    int CallFloatMethod(JNIEnv* pEnv, jobject pObject, jmethodID pMid, int len, void** pArgs, float* val)
    {
        void** args = (void**)malloc(sizeof(void *) * len);

        for(int i = 0; i < len; i++)
            args[i] = pArgs[i];

        // float val = pEnv->CallFloatMethodA(pObject, pMid, (const jvalue*)args);
        *val = pEnv->CallFloatMethodA(pObject, pMid, (const jvalue*)args);
        if(pEnv->ExceptionCheck() == JNI_TRUE){
            //pEnv->ExceptionDescribe();
            free(args);
            // return -666;
            return -1;
        }

        // return val;
        return 0;
    }

    int GetStaticFloatField(JNIEnv* pEnv, jclass pClass, jfieldID pMid, float *val)
    {
        *val = pEnv->GetStaticFloatField(pClass, pMid);
        if(pEnv->ExceptionCheck() == JNI_TRUE)
            return -1;
        return 0;
    }

    int GetFloatField(JNIEnv* pEnv, jobject pObject, jfieldID pMid, float *val)
    {

        *val = pEnv->GetFloatField(pObject, pMid);
        if(pEnv->ExceptionCheck() == JNI_TRUE)
            return -11;
        return 0;
    }

    int SetStaticFloatField(JNIEnv* pEnv, jclass pClass, jfieldID pMid, float val)
    {
        
        pEnv->SetStaticFloatField(pClass, pMid, val);
        
        if( pEnv->ExceptionCheck() == JNI_TRUE )
        {
            //pEnv->ExceptionDescribe();
            return -1;
        }
        return 0;
    }

    int SetFloatField(JNIEnv* pEnv, jobject pObject, jfieldID pMid, float val)
    {
        
        pEnv->SetFloatField( pObject, pMid, val);
        
        if( pEnv->ExceptionCheck() == JNI_TRUE )
        {
            //pEnv->ExceptionDescribe();
            return -1;
        }
        return 0;
    }

    //double
    int CallStaticDoubleMethod(JNIEnv* pEnv, jclass pClass, jmethodID pMid, int len, void** pArgs, double* val)
    {
        void** args = (void**)malloc(sizeof(void *) * len);

        for(int i = 0; i < len; i++)
            args[i] = pArgs[i];

        *val = pEnv->CallStaticDoubleMethodA(pClass, pMid, (const jvalue*)args);
        if(pEnv->ExceptionCheck() == JNI_TRUE){
            free(args);
            return -1;
        }

        return 0;
    }

    int CallDoubleMethod(JNIEnv* pEnv, jobject pObject, jmethodID pMid, int len, void** pArgs, double* val)
    {
        void** args = (void**)malloc(sizeof(void *) * len);

        for(int i = 0; i < len; i++)
            args[i] = pArgs[i];

        *val = pEnv->CallDoubleMethodA( pObject, pMid, (const jvalue*)args);
        if(pEnv->ExceptionCheck() == JNI_TRUE){
            free(args);
            return -1;
        }

        return 0;
    }

    int GetStaticDoubleField(JNIEnv* pEnv, jclass pClass, jfieldID pMid, double* val)
    {
        *val = pEnv->GetStaticDoubleField(pClass, pMid);
        if(pEnv->ExceptionCheck() == JNI_TRUE){
            //pEnv->ExceptionDescribe();
            return -1;
        }

        return 0;
    }

    int GetDoubleField(JNIEnv* pEnv, jobject pObject, jfieldID pMid, double* val)
    {
        *val = pEnv->GetDoubleField(pObject, pMid);
        if(pEnv->ExceptionCheck() == JNI_TRUE){
            //pEnv->ExceptionDescribe();
            return -1;
        }

        return 0;
    }

    int SetStaticDoubleField(JNIEnv* pEnv, jclass pClass, jfieldID pMid, double val)
    {
        
        pEnv->SetStaticDoubleField(pClass, pMid, val);
        
        if( pEnv->ExceptionCheck() == JNI_TRUE )
        {
            //pEnv->ExceptionDescribe();
            return -1;
        }
        return 0;
    }

    int SetDoubleField(JNIEnv* pEnv, jobject pObject, jfieldID pMid, double val)
    {
        
        pEnv->SetDoubleField( pObject, pMid, val);
        
        if( pEnv->ExceptionCheck() == JNI_TRUE )
        {
            //pEnv->ExceptionDescribe();
            return -1;
        }
        return 0;
    }

    //bool
    int CallStaticBooleanMethod(JNIEnv* pEnv, jclass pClass, jmethodID pMid, int len, void** pArgs, bool* val)
    {
        void** args = (void**)malloc(sizeof(void *) * len);

        for(int i = 0; i < len; i++)
            args[i] = pArgs[i];

        *val = pEnv->CallStaticBooleanMethodA( pClass, pMid, (const jvalue*)args);
        if(pEnv->ExceptionCheck() == JNI_TRUE){
            //pEnv->ExceptionDescribe();
            free(args);
            return -1;
        }

        return 0;
    }

    int CallBooleanMethod(JNIEnv* pEnv, jobject pObject, jmethodID pMid, int len, void** pArgs, bool* val)
    {
        void** args = (void**)malloc(sizeof(void *) * len);

        for(int i = 0; i < len; i++)
            args[i] = pArgs[i];

        *val = pEnv->CallBooleanMethodA( pObject, pMid, (const jvalue*)args);
        if(pEnv->ExceptionCheck() == JNI_TRUE){
            //pEnv->ExceptionDescribe();
            free(args);
            return -1;
        }

        return 0;
    }

    int GetStaticBooleanField(JNIEnv* pEnv, jclass pClass, jfieldID pMid, bool* val)
    {
        *val = pEnv->GetStaticBooleanField(pClass, pMid);
        if(pEnv->ExceptionCheck() == JNI_TRUE){
            //pEnv->ExceptionDescribe();
            return -1;
        }

        return 0;
    }

    int GetBooleanField(JNIEnv* pEnv, jobject pObject, jfieldID pMid, bool* val)
    {
        *val = pEnv->GetBooleanField(pObject, pMid);
        if(pEnv->ExceptionCheck() == JNI_TRUE){
            //pEnv->ExceptionDescribe();
            
            return -1;
        }

        return 0;
    }

    int SetStaticBooleanField(JNIEnv* pEnv, jclass pClass, jfieldID pMid, bool val)
    {
        
        pEnv->SetStaticBooleanField(pClass, pMid, val);
        
        if( pEnv->ExceptionCheck() == JNI_TRUE )
        {
            //pEnv->ExceptionDescribe();
            return -1;
        }
        return 0;
    }

    int SetBooleanField(JNIEnv* pEnv, jobject pObject, jfieldID pMid, bool val)
    {
        
        pEnv->SetBooleanField( pObject, pMid, val);
        
        if( pEnv->ExceptionCheck() == JNI_TRUE )
        {
            //pEnv->ExceptionDescribe();
            return -1;
        }
        return 0;
    }

    //byte
    int CallStaticByteMethod(JNIEnv* pEnv, jclass pClass, jmethodID pMid, int len, void** pArgs, jbyte* val)
    {
        void** args = (void**)malloc(sizeof(void *) * len);

        for(int i = 0; i < len; i++)
            args[i] = pArgs[i];

        *val = pEnv->CallStaticByteMethodA( pClass, pMid, (const jvalue*)args);
        if(pEnv->ExceptionCheck() == JNI_TRUE){
            //pEnv->ExceptionDescribe();
            free(args);
            return -1;
        }

        return 0;
    }

    int CallByteMethod(JNIEnv* pEnv, jobject pObject, jmethodID pMid, int len, void** pArgs, jbyte* val)
    {
        void** args = (void**)malloc(sizeof(void *) * len);

        for(int i = 0; i < len; i++)
            args[i] = pArgs[i];

        *val = pEnv->CallByteMethodA( pObject, pMid, (const jvalue*)args);
        if(pEnv->ExceptionCheck() == JNI_TRUE){
            //pEnv->ExceptionDescribe();
            free(args);
            return -1;
        }

        return 0;
    }

    int GetStaticByteField(JNIEnv* pEnv, jclass pClass, jfieldID pMid, jbyte* val)
    {
        *val = pEnv->GetStaticByteField(pClass, pMid);
        if(pEnv->ExceptionCheck() == JNI_TRUE){
            //pEnv->ExceptionDescribe();
            return -1;
        }

        return 0;
    }

    int GetByteField(JNIEnv* pEnv, jobject pObject, jfieldID pMid, jbyte* val)
    {
        *val = pEnv->GetByteField(pObject, pMid);
        if(pEnv->ExceptionCheck() == JNI_TRUE){
            //pEnv->ExceptionDescribe();
            return -1;
        }

        return 0;
    }

    int SetStaticByteField(JNIEnv* pEnv, jclass pClass, jfieldID pMid, jbyte val)
    {
        pEnv->SetStaticByteField(pClass, pMid, val);
        
        if( pEnv->ExceptionCheck() == JNI_TRUE )
        {
            //pEnv->ExceptionDescribe();
            return -1;
        }
        return 0;
    }

    int SetByteField(JNIEnv* pEnv, jobject pObject, jfieldID pMid, jbyte val)
    {
        pEnv->SetByteField( pObject, pMid, val);
        
        if( pEnv->ExceptionCheck() == JNI_TRUE )
        {
            //pEnv->ExceptionDescribe();
            return -1;
        }
        return 0;
    }

    //char
    int CallStaticCharMethod(JNIEnv* pEnv, jclass pClass, jmethodID pMid, int len, void** pArgs, char* val)
    {
        void** args = (void**)malloc(sizeof(void *) * len);

        for(int i = 0; i < len; i++)
            args[i] = pArgs[i];

        *val = pEnv->CallStaticCharMethodA( pClass, pMid, (const jvalue*)args);
        if(pEnv->ExceptionCheck() == JNI_TRUE){
            //pEnv->ExceptionDescribe();
            free(args);
            return -1;
        }

        return 0;
    }

    int CallCharMethod(JNIEnv* pEnv, jobject pObject, jmethodID pMid, int len, void** pArgs, char* val)
    {
        void** args = (void**)malloc(sizeof(void *) * len);

        for(int i = 0; i < len; i++)
            args[i] = pArgs[i];
            
        *val = pEnv->CallCharMethodA( pObject, pMid, (const jvalue*)args);
        if(pEnv->ExceptionCheck() == JNI_TRUE){
            //pEnv->ExceptionDescribe();
            free(args);
            return -1;
        }

        return 0;
    }

    int GetStaticCharField(JNIEnv* pEnv, jclass pClass, jfieldID pMid, char* val)
    {
        *val = pEnv->GetStaticCharField(pClass, pMid);
        if(pEnv->ExceptionCheck() == JNI_TRUE){
            //pEnv->ExceptionDescribe();
            
            return -1;
        }

        return 0;
    }

    int GetCharField(JNIEnv* pEnv, jobject pObject, jfieldID pMid, char* val)
    {
        *val = pEnv->GetCharField(pObject, pMid);
        if(pEnv->ExceptionCheck() == JNI_TRUE){
            //pEnv->ExceptionDescribe();
            
            return -1;
        }

        return 0;
    }

    int SetStaticCharField(JNIEnv* pEnv, jclass pClass, jfieldID pMid, char val)
    {
        
        pEnv->SetStaticCharField(pClass, pMid, val);
        
        if( pEnv->ExceptionCheck() == JNI_TRUE )
        {
            //pEnv->ExceptionDescribe();
            return -1;
        }
        return 0;
    }

    int SetCharField(JNIEnv* pEnv, jobject pObject, jfieldID pMid, char val)
    {
        
        pEnv->SetCharField( pObject, pMid, val);
        
        if( pEnv->ExceptionCheck() == JNI_TRUE )
        {
            //pEnv->ExceptionDescribe();
            return -1;
        }
        return 0;
    }


    //short
    int CallStaticShortMethod(JNIEnv* pEnv, jclass pClass, jmethodID pMid, int len, void** pArgs, short* val)
    {
        void** args = (void**)malloc(sizeof(void *) * len);

        for(int i = 0; i < len; i++)
            args[i] = pArgs[i];

        *val = pEnv->CallStaticShortMethodA( pClass, pMid, (const jvalue*)args);
        if(pEnv->ExceptionCheck() == JNI_TRUE){
            //pEnv->ExceptionDescribe();
            free(args);
            return -1;
        }

        return 0;
    }

    int CallShortMethod(JNIEnv* pEnv, jobject pObject, jmethodID pMid, int len, void** pArgs, short* val)
    {
        void** args = (void**)malloc(sizeof(void *) * len);

        for(int i = 0; i < len; i++)
            args[i] = pArgs[i];

        *val = pEnv->CallShortMethodA( pObject, pMid, (const jvalue*)args);
        if(pEnv->ExceptionCheck() == JNI_TRUE){
            //pEnv->ExceptionDescribe();
            free(args);
            return -1;
        }

        return 0;
    }

    int GetStaticShortField(JNIEnv* pEnv, jclass pClass, jfieldID pMid, short* val)
    {
        *val = pEnv->GetStaticShortField(pClass, pMid);
        if(pEnv->ExceptionCheck() == JNI_TRUE){
            //pEnv->ExceptionDescribe();
            return -1;
        }

        return 0;
    }

    int GetShortField(JNIEnv* pEnv, jobject pObject, jfieldID pMid, short* val)
    {
        *val = pEnv->GetShortField(pObject, pMid);
        if(pEnv->ExceptionCheck() == JNI_TRUE){
            //pEnv->ExceptionDescribe();
            return -1;
        }

        return 0;
    }

    int SetStaticShortField(JNIEnv* pEnv, jclass pClass, jfieldID pMid, short val)
    {
        pEnv->SetStaticShortField(pClass, pMid, val);
        
        if( pEnv->ExceptionCheck() == JNI_TRUE )
        {
            //pEnv->ExceptionDescribe();
            return -1;
        }
        return 0;
    }

    int SetShortField(JNIEnv* pEnv, jobject pObject, jfieldID pMid, short val)
    {
        
        pEnv->SetShortField( pObject, pMid, val);
        
        if( pEnv->ExceptionCheck() == JNI_TRUE )
        {
            //pEnv->ExceptionDescribe();
            return -1;
        }
        return 0;
    }



    //string back and forth
    jstring GetJavaString(JNIEnv* pEnv, const char* nString)
    {
        return pEnv->NewStringUTF(nString);
    }

    const char* GetNetString(JNIEnv* pEnv, jstring jString)
    {
        if(jString == (jstring)0){
            return "";
        }
        
        const char* res = pEnv->GetStringUTFChars(jString, 0);
        if(pEnv->ExceptionCheck() == JNI_TRUE){
            return "";
        }
        return res;
    }

    const char* GetException(JNIEnv* pEnv)
    {
        jthrowable exception = pEnv->ExceptionOccurred();

        pEnv->ExceptionClear();

        jclass clr_runtime_class = pEnv->FindClass("app/quant/clr/CLRRuntime");
        jmethodID mid_clr_getError =
            pEnv->GetStaticMethodID(clr_runtime_class,
                            "GetError",
                            "(Ljava/lang/Exception;)Ljava/lang/String;");

        int len = 1;
        void** args = (void**)malloc(sizeof(void *) * len);

        for(int i = 0; i < len; i++)
            args[i] = exception;

        jstring jString = (jstring)pEnv->CallStaticObjectMethodA( clr_runtime_class, mid_clr_getError, (const jvalue*)args);

        if( pEnv->ExceptionCheck() == JNI_TRUE )
        {
            //pEnv->ExceptionDescribe();
            // free(args);
            return "error when getting error !";
        }
        
        if(jString == (jstring)0){
            return "error when getting error !!";
        }
        
        const char* res = pEnv->GetStringUTFChars(jString, 0);
        if(pEnv->ExceptionCheck() == JNI_TRUE){
            //pEnv->ExceptionDescribe();
            return "error when getting error !!!";
        }
        return res;
    }

    //object array

    int NewObjectArrayP(JNIEnv* pEnv, int nDimension, jclass cls, jobjectArray* pArray )
    {
        *pArray = pEnv->NewObjectArray( nDimension, cls, NULL);

        if(pEnv->ExceptionCheck() == JNI_TRUE){
            //pEnv->ExceptionDescribe();
            return -1;
        }

        if( pArray != NULL )
            return 0;
        else
            return -2;

    }
    
    int NewObjectArray(JNIEnv* pEnv, int nDimension, const char* szType, jobjectArray* pArray )
    {
        *pArray = pEnv->NewObjectArray( nDimension, pEnv->FindClass( szType ), NULL);

        if(pEnv->ExceptionCheck() == JNI_TRUE){
            //pEnv->ExceptionDescribe();
            return -1;
        }

        if( pArray != NULL )
            return 0;
        else
            return -2;

    }

    int SetObjectArrayElement(JNIEnv* pEnv, jobjectArray pArray, int index, jobject value)
    {
        pEnv->SetObjectArrayElement(pArray, index, value);
        if( pEnv->ExceptionCheck() == JNI_TRUE )
        {
            //pEnv->ExceptionDescribe();
            return -1;
        }
        else
            return 0;
    }

    int GetObjectArrayElement(JNIEnv* pEnv, jobjectArray pArray, int index, jobject* pobj)
    {
        jobject val = pEnv->GetObjectArrayElement(pArray, index);
        if( pEnv->ExceptionCheck() == JNI_TRUE )
        {
            //pEnv->ExceptionDescribe();
            return -1;
        }
        else
            *pobj = val;
            return 0;
    }

    //int array
    int NewIntArray(JNIEnv* pEnv, int nDimension, jintArray* pArray )
    {
        *pArray = pEnv->NewIntArray(nDimension);
        
        if(pEnv->ExceptionCheck() == JNI_TRUE){
            //pEnv->ExceptionDescribe();
            return -1;
        }
        if( pArray != NULL )
            return 0;
        else
            return -2;

    }

    int SetIntArrayElement(JNIEnv* pEnv, jintArray pArray, int index, int value)
    {
        const jint elements[] = { value };
        pEnv->SetIntArrayRegion(pArray, index, 1, elements);
        if( pEnv->ExceptionCheck() == JNI_TRUE )
        {
            //pEnv->ExceptionDescribe();
            return -1;
        }
        else
            return 0;
    }

    int GetIntArrayElement(JNIEnv* pEnv, jintArray pArray, int index)
    {
        jint *val = pEnv->GetIntArrayElements(pArray, 0);
        return val[index];
    }

    //long array
    int NewLongArray(JNIEnv* pEnv, int nDimension, jlongArray* pArray )
    {
        *pArray = pEnv->NewLongArray( nDimension);

        if(pEnv->ExceptionCheck() == JNI_TRUE)
        {
            //pEnv->ExceptionDescribe();
            return -1;
        }

        if( pArray != NULL )
            return 0;
        else
            return -2;

    }

    int SetLongArrayElement(JNIEnv* pEnv, jlongArray pArray, int index, long value)
    {
        const jlong elements[] = { value };
        pEnv->SetLongArrayRegion(pArray, index, 1, elements);
        if( pEnv->ExceptionCheck() == JNI_TRUE )
        {
            //pEnv->ExceptionDescribe();
            return -1;
        }
        else
            return 0;
    }

    long GetLongArrayElement(JNIEnv* pEnv, jlongArray pArray, int index)
    {
        jlong *val = pEnv->GetLongArrayElements(pArray, 0);
        if(pEnv->ExceptionCheck() == JNI_TRUE){
            //pEnv->ExceptionDescribe();
            return -666;
        }
        return val[index];
    }

    //float array
    int NewFloatArray(JNIEnv* pEnv, int nDimension, jfloatArray* pArray )
    {
        *pArray = pEnv->NewFloatArray( nDimension);

        if(pEnv->ExceptionCheck() == JNI_TRUE)
        {
            //pEnv->ExceptionDescribe();
            return -1;
        }

        if( pArray != NULL )
            return 0;
        else
            return -2;

    }

    int SetFloatArrayElement(JNIEnv* pEnv, jfloatArray pArray, int index, float value)
    {
        float elements[] = { value };
        pEnv->SetFloatArrayRegion(pArray, index, 1, elements);
        if( pEnv->ExceptionCheck() == JNI_TRUE )
        {
            //pEnv->ExceptionDescribe();
            return -1;
        }
        else
            return 0;
    }

    float GetFloatArrayElement(JNIEnv* pEnv, jfloatArray pArray, int index)
    {
        float *val = pEnv->GetFloatArrayElements(pArray, 0);
        if(pEnv->ExceptionCheck() == JNI_TRUE){
            //pEnv->ExceptionDescribe();
            return -666;
        }
        return val[index];
    }

    //double array
    int NewDoubleArray(JNIEnv* pEnv, int nDimension, jdoubleArray* pArray )
    {
        *pArray = pEnv->NewDoubleArray( nDimension);

        if(pEnv->ExceptionCheck() == JNI_TRUE)
        {
            //pEnv->ExceptionDescribe();
            return -1;
        }

        if( pArray != NULL )
            return 0;
        else
            return -2;

    }

    int SetDoubleArrayElement(JNIEnv* pEnv, jdoubleArray pArray, int index, double value)
    {
        double elements[] = { value };
        pEnv->SetDoubleArrayRegion(pArray, index, 1, elements);
        if( pEnv->ExceptionCheck() == JNI_TRUE )
        {
            //pEnv->ExceptionDescribe();
            return -1;
        }
        else
            return 0;
    }

    double GetDoubleArrayElement(JNIEnv* pEnv, jdoubleArray pArray, int index)
    {
        double *val = pEnv->GetDoubleArrayElements(pArray, 0);
        if(pEnv->ExceptionCheck() == JNI_TRUE){
            //pEnv->ExceptionDescribe();
            return -666;
        }
        return val[index];
    }

    //boolean array
    int NewBooleanArray(JNIEnv* pEnv, int nDimension, jbooleanArray* pArray )
    {
        *pArray = pEnv->NewBooleanArray( nDimension);

        if(pEnv->ExceptionCheck() == JNI_TRUE)
        {
            //pEnv->ExceptionDescribe();
            return -1;
        }

        if( pArray != NULL )
            return 0;
        else
            return -2;

    }

    int SetBooleanArrayElement(JNIEnv* pEnv, jbooleanArray pArray, int index, bool value)
    {
        jboolean elements[] = { value };
        pEnv->SetBooleanArrayRegion(pArray, index, 1, elements);
        if( pEnv->ExceptionCheck() == JNI_TRUE )
        {
            //pEnv->ExceptionDescribe();
            return -1;
        }
        else
            return 0;
    }

    bool GetBooleanArrayElement(JNIEnv* pEnv, jbooleanArray pArray, int index)
    {
        jboolean *val = pEnv->GetBooleanArrayElements(pArray, 0);
        if(pEnv->ExceptionCheck() == JNI_TRUE){
            //pEnv->ExceptionDescribe();
            return false;
        }
        return (bool)val[index];
    }

    
    //byte array
    int NewByteArray(JNIEnv* pEnv, int nDimension, jbyteArray* pArray )
    {
        *pArray = pEnv->NewByteArray( nDimension);

        if(pEnv->ExceptionCheck() == JNI_TRUE)
        {
            //pEnv->ExceptionDescribe();
            return -1;
        }

        if( pArray != NULL )
            return 0;
        else
            return -2;

    }

    int SetByteArrayElement(JNIEnv* pEnv, jbyteArray pArray, int index, jbyte value)
    {
        jbyte elements[] = { value };
        pEnv->SetByteArrayRegion(pArray, index, 1, elements);
        if( pEnv->ExceptionCheck() == JNI_TRUE )
        {
            //pEnv->ExceptionDescribe();
            return -1;
        }
        else
            return 0;
    }

    jbyte GetByteArrayElement(JNIEnv* pEnv, jbyteArray pArray, int index)
    {
        jbyte *val = pEnv->GetByteArrayElements(pArray, 0);
        if(pEnv->ExceptionCheck() == JNI_TRUE){
            //pEnv->ExceptionDescribe();
            return -1;
        }
        return val[index];
    }


    //short array
    int NewShortArray(JNIEnv* pEnv, int nDimension, jshortArray* pArray )
    {
        *pArray = pEnv->NewShortArray( nDimension);

        if(pEnv->ExceptionCheck() == JNI_TRUE)
        {
            //pEnv->ExceptionDescribe();
            return -1;
        }

        if( pArray != NULL )
            return 0;
        else
            return -2;

    }

    int SetShortArrayElement(JNIEnv* pEnv, jshortArray pArray, int index, short value)
    {
        short elements[] = { value };
        pEnv->SetShortArrayRegion(pArray, index, 1, elements);
        if( pEnv->ExceptionCheck() == JNI_TRUE )
        {
            //pEnv->ExceptionDescribe();
            return -1;
        }
        else
            return 0;
    }

    short GetShortArrayElement(JNIEnv* pEnv, jshortArray pArray, int index)
    {
        short *val = pEnv->GetShortArrayElements(pArray, 0);
        if(pEnv->ExceptionCheck() == JNI_TRUE){
            //pEnv->ExceptionDescribe();
            return -666;
        }
        return val[index];
    }

    //char array
    int NewCharArray(JNIEnv* pEnv, int nDimension, jcharArray* pArray )
    {
        *pArray = pEnv->NewCharArray( nDimension);

        if(pEnv->ExceptionCheck() == JNI_TRUE)
        {
            //pEnv->ExceptionDescribe();
            return -1;
        }

        if( pArray != NULL )
            return 0;
        else
            return -2;

    }

    int SetCharArrayElement(JNIEnv* pEnv, jcharArray pArray, int index, char value)
    {
        char elements[] = { value };
        pEnv->SetCharArrayRegion(pArray, index, 1, (jchar *)elements);
        if( pEnv->ExceptionCheck() == JNI_TRUE )
        {
            //pEnv->ExceptionDescribe();
            return -1;
        }
        else
            return 0;
    }

    char GetCharArrayElement(JNIEnv* pEnv, jcharArray pArray, int index)
    {
        jchar *val = pEnv->GetCharArrayElements(pArray, 0);
        if(pEnv->ExceptionCheck() == JNI_TRUE){
            //pEnv->ExceptionDescribe();
            return (char)-1;
        }
        return val[index];
    }






    /*
    Static wrapper on DestroyJavaVM() JNI function.
    See the description in
    http://jre.sfbay/java/re/jdk/6/promoted/latest/docs/technotes/guides/jni/spec/invocation.html#destroy_java_vm
    */

    int DestroyJavaVM( JavaVM* pJVM ){
        pJVM->DestroyJavaVM();
        return 0;
    }


    
    int (*fnCreateInstance)(const char*, int, void**);

    void SetfnCreateInstance(void* cb)
    {
        fnCreateInstance = (int (*)(const char*, int, void**))cb;
    }

    JNIEXPORT jint JNICALL Java_app_quant_clr_CLRRuntime_nativeCreateInstance(JNIEnv* pEnv, jclass cls, jstring classname, jint len, jobjectArray args)
    {
        const char* _classname = GetNetString(pEnv, classname);
        int val = fnCreateInstance(_classname, len, (void**)args);
        if(pEnv->ExceptionCheck() == JNI_TRUE){
            //pEnv->ExceptionDescribe();
            return -1;
        }
        return val;
    }

    jobject (*fnInvoke)(int, const char*, int, void**);

    void SetfnInvoke(void* cb)
    {
        fnInvoke = (jobject (*)(int, const char*, int, void**))cb;
    }

    JNIEXPORT jobject JNICALL Java_app_quant_clr_CLRRuntime_nativeInvoke(JNIEnv* pEnv, jclass cls, jint ptr, jstring funcname, jint len, jobjectArray args)
    {
        const char* _funcname = GetNetString(pEnv, funcname);
        jobject val = fnInvoke(ptr, _funcname, len, (void**)args);
        if(pEnv->ExceptionCheck() == JNI_TRUE){
            //pEnv->ExceptionDescribe();
            return NULL;
        }
        return val;
    }

    jobject (*fnGetProperty)(int, const char*);

    void SetfnGetProperty(void* cb)
    {
        fnGetProperty = (jobject (*)(int, const char*))cb;
    }

    JNIEXPORT jobject JNICALL Java_app_quant_clr_CLRRuntime_nativeGetProperty(JNIEnv* pEnv, jclass cls, jint ptr, jstring name)
    {
        const char* _name = GetNetString(pEnv, name);
        jobject val = fnGetProperty(ptr, _name);
        if(pEnv->ExceptionCheck() == JNI_TRUE){
            //pEnv->ExceptionDescribe();
            return NULL;
        }
        return val;
    }

    jobject (*fnSetProperty)(int, const char*, void**);

    void SetfnSetProperty(void* cb)
    {
        fnSetProperty = (jobject (*)(int, const char*, void**))cb;
    }

    JNIEXPORT void JNICALL Java_app_quant_clr_CLRRuntime_nativeSetProperty(JNIEnv* pEnv, jclass cls, jint ptr, jstring name, jobjectArray value)
    {
        const char* _name = GetNetString(pEnv, name);
        fnSetProperty(ptr, _name, (void**)value);
    }



    jobject (*fnRegisterFunc)(const char*, int);

    void SetfnRegisterFunc(void* cb)
    {
        fnRegisterFunc = (jobject (*)(const char*, int))cb;
    }

    JNIEXPORT jobject JNICALL Java_app_quant_clr_CLRRuntime_nativeRegisterFunc(JNIEnv* pEnv, jclass cls, jstring funcname, jint hash)
    {
        const char* _funcname = GetNetString(pEnv, funcname);
        
        jobject val = fnRegisterFunc(_funcname, hash);
        if(pEnv->ExceptionCheck() == JNI_TRUE){
            //pEnv->ExceptionDescribe();
            return NULL;
        }
        return val;
    }


    jobject (*fnInvokeFunc)(int, int, void**);

    void SetfnInvokeFunc(void* cb)
    {
        fnInvokeFunc = (jobject (*)(int, int, void**))cb;
    }

    JNIEXPORT jobject JNICALL Java_app_quant_clr_CLRRuntime_nativeInvokeFunc(JNIEnv* pEnv, jclass cls, jint ptr, jint len, jobjectArray args)
    {
        jobject val = fnInvokeFunc(ptr, len, (void**)args);
        if(pEnv->ExceptionCheck() == JNI_TRUE){
            //pEnv->ExceptionDescribe();
            return NULL;
        }
        return val;
    }

}