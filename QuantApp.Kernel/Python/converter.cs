using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;

namespace Python.Runtime
{
    /// <summary>
    /// Performs data conversions between managed types and Python types.
    /// </summary>
    [SuppressUnmanagedCodeSecurity]
    internal class Converter
    {
        private Converter()
        {
        }

        private static NumberFormatInfo nfi;
        private static Type objectType;
        private static Type stringType;
        private static Type singleType;
        private static Type doubleType;
        private static Type decimalType;
        private static Type int16Type;
        private static Type int32Type;
        private static Type int64Type;
        private static Type flagsType;
        private static Type boolType;
        private static Type typeType;
        private static IntPtr decimalCtor;
        private static IntPtr dateTimeCtor;
        private static IntPtr timeSpanCtor;
        private static IntPtr tzInfoCtor;

        static Converter()
        {
            nfi = NumberFormatInfo.InvariantInfo;
            objectType = typeof(Object);
            stringType = typeof(String);
            int16Type = typeof(Int16);
            int32Type = typeof(Int32);
            int64Type = typeof(Int64);
            singleType = typeof(Single);
            doubleType = typeof(Double);
            decimalType = typeof(Decimal);
            flagsType = typeof(FlagsAttribute);
            boolType = typeof(Boolean);
            typeType = typeof(Type);

            IntPtr decimalMod = Runtime.PyImport_ImportModule("decimal");
            if (decimalMod == null) throw new PythonException();

            IntPtr dateTimeMod = Runtime.PyImport_ImportModule("datetime");
            if (dateTimeMod == null) throw new PythonException();
            
            decimalCtor = Runtime.PyObject_GetAttrString(decimalMod, "Decimal");
            if (decimalCtor == null) throw new PythonException();
            
            dateTimeCtor = Runtime.PyObject_GetAttrString(dateTimeMod, "datetime");
            if (dateTimeCtor == null) throw new PythonException();
            
            timeSpanCtor = Runtime.PyObject_GetAttrString(dateTimeMod, "timedelta");
            if (timeSpanCtor == null) throw new PythonException();

            IntPtr tzInfoMod = PythonEngine.ModuleFromString("custom_tzinfo",
                "from datetime import timedelta, tzinfo\n" +
                "class GMT(tzinfo):\n" +
                "    def __init__(self, hours, minutes):\n" +
                "        self.hours = hours\n" +
                "        self.minutes = minutes\n" +
                "    def utcoffset(self, dt):\n" +
                "        return timedelta(hours=self.hours, minutes=self.minutes)\n" +
                "    def tzname(self, dt):\n" +
                "        return \"GMT {0:00}:{1:00}\".format(self.hours, self.minutes)\n" +
                "    def dst (self, dt):\n" +
                "        return timedelta(0)\n").Handle;

            tzInfoCtor = Runtime.PyObject_GetAttrString(tzInfoMod, "GMT");
            if (tzInfoCtor == null) throw new PythonException();
        }


        /// <summary>
        /// Given a builtin Python type, return the corresponding CLR type.
        /// </summary>
        internal static Type GetTypeByAlias(IntPtr op)
        {
            if (op == Runtime.PyStringType)
                return stringType;

            if (op == Runtime.PyUnicodeType)
                return stringType;

            if (op == Runtime.PyIntType)
                return int32Type;

            if (op == Runtime.PyLongType)
                return int64Type;

            if (op == Runtime.PyFloatType)
                return doubleType;

            if (op == Runtime.PyBoolType)
                return boolType;

            return null;
        }

        internal static IntPtr GetPythonTypeByAlias(Type op)
        {
            if (op == stringType)
                return Runtime.PyUnicodeType;

            if (op == int16Type)
                return Runtime.PyIntType;

            if (op == int32Type)
                return Runtime.PyIntType;

            if (op == int64Type && Runtime.IsPython2)
                return Runtime.PyLongType;

            if (op == int64Type)
                return Runtime.PyIntType;

            if (op == doubleType)
                return Runtime.PyFloatType;

            if (op == singleType)
                return Runtime.PyFloatType;

            if (op == boolType)
                return Runtime.PyBoolType;

            if (op == decimalType)
                return Runtime.PyFloatType;

            return IntPtr.Zero;
        }


        /// <summary>
        /// Return a Python object for the given native object, converting
        /// basic types (string, int, etc.) into equivalent Python objects.
        /// This always returns a new reference. Note that the System.Decimal
        /// type has no Python equivalent and converts to a managed instance.
        /// </summary>
        internal static IntPtr ToPython<T>(T value)
        {
            return ToPython(value, typeof(T));
        }

        internal static IntPtr ToPython(object value, Type type)
        {
            if (value is PyObject)
            {
                IntPtr handle = ((PyObject)value).Handle;
                Runtime.XIncref(handle);
                return handle;
            }
            IntPtr result = IntPtr.Zero;

            // Null always converts to None in Python.

            if (value == null)
            {
                result = Runtime.PyNone;
                Runtime.XIncref(result);
                return result;
            }

            if (value is IList && value.GetType().IsGenericType)
            {
                using (var resultlist = new PyList())
                {
                    foreach (object o in (IEnumerable)value)
                    {
                        using (var p = new PyObject(ToPython(o, o?.GetType())))
                        {
                            resultlist.Append(p);
                        }
                    }
                    Runtime.XIncref(resultlist.Handle);
                    return resultlist.Handle;
                }
            }

            // it the type is a python subclass of a managed type then return the
            // underlying python object rather than construct a new wrapper object.
            var pyderived = value as IPythonDerivedType;
            if (null != pyderived)
            {
                return ClassDerivedObject.ToPython(pyderived);
            }

            // hmm - from Python, we almost never care what the declared
            // type is. we'd rather have the object bound to the actual
            // implementing class.

            type = value.GetType();

            TypeCode tc = Type.GetTypeCode(type);

            switch (tc)
            {
                case TypeCode.Object:
                    if (value is TimeSpan)
                    {
                        var timespan = (TimeSpan)value;

                        IntPtr timeSpanArgs = Runtime.PyTuple_New(1);
                        Runtime.PyTuple_SetItem(timeSpanArgs, 0, Runtime.PyFloat_FromDouble(timespan.TotalDays));
                        return Runtime.PyObject_CallObject(timeSpanCtor, timeSpanArgs);
                    }
                    return CLRObject.GetInstHandle(value, type);

                case TypeCode.String:
                    return Runtime.PyUnicode_FromString((string)value);

                case TypeCode.Int32:
                    return Runtime.PyInt_FromInt32((int)value);

                case TypeCode.Boolean:
                    if ((bool)value)
                    {
                        Runtime.XIncref(Runtime.PyTrue);
                        return Runtime.PyTrue;
                    }
                    Runtime.XIncref(Runtime.PyFalse);
                    return Runtime.PyFalse;

                case TypeCode.Byte:
                    return Runtime.PyInt_FromInt32((int)((byte)value));

                case TypeCode.Char:
                    return Runtime.PyUnicode_FromOrdinal((int)((char)value));

                case TypeCode.Int16:
                    return Runtime.PyInt_FromInt32((int)((short)value));

                case TypeCode.Int64:
                    return Runtime.PyLong_FromLongLong((long)value);

                case TypeCode.Single:
                    // return Runtime.PyFloat_FromDouble((double)((float)value));
                    string ss = ((float)value).ToString(nfi);
                    IntPtr ps = Runtime.PyString_FromString(ss);
                    IntPtr op = Runtime.PyFloat_FromString(ps, IntPtr.Zero);
                    Runtime.XDecref(ps);
                    return op;

                case TypeCode.Double:
                    return Runtime.PyFloat_FromDouble((double)value);

                case TypeCode.SByte:
                    return Runtime.PyInt_FromInt32((int)((sbyte)value));

                case TypeCode.UInt16:
                    return Runtime.PyInt_FromInt32((int)((ushort)value));

                case TypeCode.UInt32:
                    return Runtime.PyLong_FromUnsignedLong((uint)value);

                case TypeCode.UInt64:
                    return Runtime.PyLong_FromUnsignedLongLong((ulong)value);

                case TypeCode.Decimal:
                    string d2s = ((decimal)value).ToString(nfi);
                    IntPtr d2p = Runtime.PyString_FromString(d2s);
                    IntPtr decimalArgs = Runtime.PyTuple_New(1);
                    Runtime.PyTuple_SetItem(decimalArgs, 0, d2p);
                    
                    return Runtime.PyObject_CallObject(decimalCtor, decimalArgs);

                case TypeCode.DateTime:
                    var datetime = (DateTime)value;
                    
                    IntPtr dateTimeArgs = Runtime.PyTuple_New(8);
                    Runtime.PyTuple_SetItem(dateTimeArgs, 0, Runtime.PyInt_FromInt32(datetime.Year));
                    Runtime.PyTuple_SetItem(dateTimeArgs, 1, Runtime.PyInt_FromInt32(datetime.Month));
                    Runtime.PyTuple_SetItem(dateTimeArgs, 2, Runtime.PyInt_FromInt32(datetime.Day));
                    Runtime.PyTuple_SetItem(dateTimeArgs, 3, Runtime.PyInt_FromInt32(datetime.Hour));
                    Runtime.PyTuple_SetItem(dateTimeArgs, 4, Runtime.PyInt_FromInt32(datetime.Minute));
                    Runtime.PyTuple_SetItem(dateTimeArgs, 5, Runtime.PyInt_FromInt32(datetime.Second));
                    Runtime.PyTuple_SetItem(dateTimeArgs, 6, Runtime.PyInt_FromInt32(1000 * datetime.Millisecond));
                    Runtime.PyTuple_SetItem(dateTimeArgs, 7, TzInfo(datetime.Kind));

                    return Runtime.PyObject_CallObject(dateTimeCtor, dateTimeArgs);

                default:
                    if (value is IEnumerable)
                    {
                        using (var resultlist = new PyList())
                        {
                            foreach (object o in (IEnumerable)value)
                            {
                                using (var p = new PyObject(ToPython(o, o?.GetType())))
                                {
                                    resultlist.Append(p);
                                }
                            }
                            Runtime.XIncref(resultlist.Handle);
                            return resultlist.Handle;
                        }
                    }
                    result = CLRObject.GetInstHandle(value, type);
                    return result;
            }
        }

        private static IntPtr TzInfo(DateTimeKind kind)
        {
            return Runtime.PyNone; // FIX Due to custom_tzinfo bug // Arturo
            if (kind == DateTimeKind.Unspecified) return Runtime.PyNone;
            var offset = kind == DateTimeKind.Local ? DateTimeOffset.Now.Offset : TimeSpan.Zero;
            IntPtr tzInfoArgs = Runtime.PyTuple_New(2);
            Runtime.PyTuple_SetItem(tzInfoArgs, 0, Runtime.PyFloat_FromDouble(offset.Hours));
            Runtime.PyTuple_SetItem(tzInfoArgs, 1, Runtime.PyFloat_FromDouble(offset.Minutes));
            return Runtime.PyObject_CallObject(tzInfoCtor, tzInfoArgs);
        }


        /// <summary>
        /// In a few situations, we don't have any advisory type information
        /// when we want to convert an object to Python.
        /// </summary>
        internal static IntPtr ToPythonImplicit(object value)
        {
            if (value == null)
            {
                IntPtr result = Runtime.PyNone;
                Runtime.XIncref(result);
                return result;
            }

            return ToPython(value, objectType);
        }


        /// <summary>
        /// Return a managed object for the given Python object, taking funny
        /// byref types into account.
        /// </summary>
        internal static bool ToManaged(IntPtr value, Type type,
            out object result, bool setError)
        {
            if (type.IsByRef)
            {
                type = type.GetElementType();
            }
            return Converter.ToManagedValue(value, type, out result, setError);
        }


        internal static bool ToManagedValue(IntPtr value, Type obType,
            out object result, bool setError)
        {
            if (obType == typeof(PyObject))
            {
                Runtime.XIncref(value); // PyObject() assumes ownership
                result = new PyObject(value);
                return true;
            }

            // Common case: if the Python value is a wrapped managed object
            // instance, just return the wrapped object.
            ManagedType mt = ManagedType.GetManagedObject(value);
            result = null;

            if (mt != null)
            {
                if (mt is CLRObject)
                {
                    object tmp = ((CLRObject)mt).inst;
                    if (obType.IsInstanceOfType(tmp))
                    {
                        result = tmp;
                        return true;
                    }
                    Exceptions.SetError(Exceptions.TypeError, $"value cannot be converted to {obType}");
                    return false;
                }
                if (mt is ClassBase)
                {
                    result = ((ClassBase)mt).type;
                    return true;
                }
                // shouldn't happen
                return false;
            }

            if (value == Runtime.PyNone && !obType.IsValueType)
            {
                result = null;
                return true;
            }

            if (obType.IsGenericType && obType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                if( value == Runtime.PyNone )
                {
                    result = null;
                    return true;
                }
                // Set type to underlying type
                obType = obType.GetGenericArguments()[0];
            }

            if (obType.IsArray)
            {
                return ToArray(value, obType, out result, setError);
            }

            if (obType.IsEnum)
            {
                return ToEnum(value, obType, out result, setError);
            }

            // Conversion to 'Object' is done based on some reasonable default
            // conversions (Python string -> managed string, Python int -> Int32 etc.).
            if (obType == objectType)
            {
                if (Runtime.IsStringType(value))
                {
                    return ToPrimitive(value, stringType, out result, setError);
                }

                if (Runtime.PyBool_Check(value))
                {
                    return ToPrimitive(value, boolType, out result, setError);
                }

                if (Runtime.PyInt_Check(value))
                {
                    return ToPrimitive(value, int32Type, out result, setError);
                }

                if (Runtime.PyLong_Check(value))
                {
                    return ToPrimitive(value, int64Type, out result, setError);
                }

                if (Runtime.PyFloat_Check(value))
                {
                    return ToPrimitive(value, doubleType, out result, setError);
                }

                if (Runtime.PySequence_Check(value))
                {
                    return ToArray(value, typeof(object[]), out result, setError);
                }

                if (setError)
                {
                    Exceptions.SetError(Exceptions.TypeError, "value cannot be converted to Object");
                }

                return false;
            }

            // Conversion to 'Type' is done using the same mappings as above for objects.
            if (obType == typeType)
            {
                if (value == Runtime.PyStringType)
                {
                    result = stringType;
                    return true;
                }

                if (value == Runtime.PyBoolType)
                {
                    result = boolType;
                    return true;
                }

                if (value == Runtime.PyIntType)
                {
                    result = int32Type;
                    return true;
                }

                if (value == Runtime.PyLongType)
                {
                    result = int64Type;
                    return true;
                }

                if (value == Runtime.PyFloatType)
                {
                    result = doubleType;
                    return true;
                }

                if (value == Runtime.PyListType || value == Runtime.PyTupleType)
                {
                    result = typeof(object[]);
                    return true;
                }

                if (setError)
                {
                    Exceptions.SetError(Exceptions.TypeError, "value cannot be converted to Type");
                }

                return false;
            }

            var underlyingType = Nullable.GetUnderlyingType(obType);
            if (underlyingType != null)
            {
                return ToManagedValue(value, underlyingType, out result, setError);
            }

            var opImplicit = obType.GetMethod("op_Implicit", new[] { obType });
            if (opImplicit != null)
            {
                if (ToManagedValue(value, opImplicit.ReturnType, out result, setError))
                {
                    opImplicit = obType.GetMethod("op_Implicit", new[] { result.GetType() });
                    if (opImplicit != null)
                    {
                        result = opImplicit.Invoke(null, new[] { result });
                    }
                    return opImplicit != null;
                }
            }

            return ToPrimitive(value, obType, out result, setError);
        }

        /// <summary>
        /// Convert a Python value to an instance of a primitive managed type.
        /// </summary>
        private static bool ToPrimitive(IntPtr value, Type obType, out object result, bool setError)
        {
            IntPtr overflow = Exceptions.OverflowError;
            TypeCode tc = Type.GetTypeCode(obType);
            result = null;
            IntPtr op;
            int ival;

            switch (tc)
            {
                case TypeCode.Object:
                    if (obType == typeof(TimeSpan))
                    {
                        op = Runtime.PyObject_Str(value);
                        TimeSpan ts;
                        var arr = Runtime.GetManagedString(op).Split(',');
                        string sts = arr.Length == 1 ? arr[0] : arr[1];
                        if (!TimeSpan.TryParse(sts, out ts))
                        {
                            goto type_error;
                        }
                        Runtime.XDecref(op);

                        int days = 0;
                        if (arr.Length > 1)
                        {
                            if (!int.TryParse(arr[0].Split(' ')[0].Trim(), out days))
                            {
                                goto type_error;
                            }
                        }
                        result = ts.Add(TimeSpan.FromDays(days));
                        return true;
                    }
                    break;

                case TypeCode.String:
                    string st = Runtime.GetManagedString(value);
                    if (st == null)
                    {
                        goto type_error;
                    }
                    result = st;
                    return true;

                case TypeCode.Int32:
                    // Trickery to support 64-bit platforms.
                    if (Runtime.IsPython2 && Runtime.Is32Bit)
                    {
                        op = Runtime.PyNumber_Int(value);

                        // As of Python 2.3, large ints magically convert :(
                        if (Runtime.PyLong_Check(op))
                        {
                            Runtime.XDecref(op);
                            goto overflow;
                        }

                        if (op == IntPtr.Zero)
                        {
                            if (Exceptions.ExceptionMatches(overflow))
                            {
                                goto overflow;
                            }
                            goto type_error;
                        }
                        ival = (int)Runtime.PyInt_AsLong(op);
                        Runtime.XDecref(op);
                        result = ival;
                        return true;
                    }
                    else // Python3 always use PyLong API
                    {
                        op = Runtime.PyNumber_Long(value);
                        if (op == IntPtr.Zero)
                        {
                            Exceptions.Clear();
                            if (Exceptions.ExceptionMatches(overflow))
                            {
                                goto overflow;
                            }
                            goto type_error;
                        }
                        long ll = (long)Runtime.PyLong_AsLongLong(op);
                        Runtime.XDecref(op);
                        if (ll == -1 && Exceptions.ErrorOccurred())
                        {
                            goto overflow;
                        }
                        if (ll > Int32.MaxValue || ll < Int32.MinValue)
                        {
                            goto overflow;
                        }
                        result = (int)ll;
                        return true;
                    }

                case TypeCode.Boolean:
                    result = Runtime.PyObject_IsTrue(value) != 0;
                    return true;

                case TypeCode.Byte:
#if PYTHON3
                    if (Runtime.PyObject_TypeCheck(value, Runtime.PyBytesType))
                    {
                        if (Runtime.PyBytes_Size(value) == 1)
                        {
                            op = Runtime.PyBytes_AS_STRING(value);
                            result = (byte)Marshal.ReadByte(op);
                            return true;
                        }
                        goto type_error;
                    }
#elif PYTHON2
                    if (Runtime.PyObject_TypeCheck(value, Runtime.PyStringType))
                    {
                        if (Runtime.PyString_Size(value) == 1)
                        {
                            op = Runtime.PyString_AsString(value);
                            result = (byte)Marshal.ReadByte(op);
                            return true;
                        }
                        goto type_error;
                    }
#endif

                    op = Runtime.PyNumber_Int(value);
                    if (op == IntPtr.Zero)
                    {
                        if (Exceptions.ExceptionMatches(overflow))
                        {
                            goto overflow;
                        }
                        goto type_error;
                    }
                    ival = (int)Runtime.PyInt_AsLong(op);
                    Runtime.XDecref(op);

                    if (ival > Byte.MaxValue || ival < Byte.MinValue)
                    {
                        goto overflow;
                    }
                    byte b = (byte)ival;
                    result = b;
                    return true;

                case TypeCode.SByte:
#if PYTHON3
                    if (Runtime.PyObject_TypeCheck(value, Runtime.PyBytesType))
                    {
                        if (Runtime.PyBytes_Size(value) == 1)
                        {
                            op = Runtime.PyBytes_AS_STRING(value);
                            result = (byte)Marshal.ReadByte(op);
                            return true;
                        }
                        goto type_error;
                    }
#elif PYTHON2
                    if (Runtime.PyObject_TypeCheck(value, Runtime.PyStringType))
                    {
                        if (Runtime.PyString_Size(value) == 1)
                        {
                            op = Runtime.PyString_AsString(value);
                            result = (sbyte)Marshal.ReadByte(op);
                            return true;
                        }
                        goto type_error;
                    }
#endif

                    op = Runtime.PyNumber_Int(value);
                    if (op == IntPtr.Zero)
                    {
                        if (Exceptions.ExceptionMatches(overflow))
                        {
                            goto overflow;
                        }
                        goto type_error;
                    }
                    ival = (int)Runtime.PyInt_AsLong(op);
                    Runtime.XDecref(op);

                    if (ival > SByte.MaxValue || ival < SByte.MinValue)
                    {
                        goto overflow;
                    }
                    sbyte sb = (sbyte)ival;
                    result = sb;
                    return true;

                case TypeCode.Char:
#if PYTHON3
                    if (Runtime.PyObject_TypeCheck(value, Runtime.PyBytesType))
                    {
                        if (Runtime.PyBytes_Size(value) == 1)
                        {
                            op = Runtime.PyBytes_AS_STRING(value);
                            result = (byte)Marshal.ReadByte(op);
                            return true;
                        }
                        goto type_error;
                    }
#elif PYTHON2
                    if (Runtime.PyObject_TypeCheck(value, Runtime.PyStringType))
                    {
                        if (Runtime.PyString_Size(value) == 1)
                        {
                            op = Runtime.PyString_AsString(value);
                            result = (char)Marshal.ReadByte(op);
                            return true;
                        }
                        goto type_error;
                    }
#endif
                    else if (Runtime.PyObject_TypeCheck(value, Runtime.PyUnicodeType))
                    {
                        if (Runtime.PyUnicode_GetSize(value) == 1)
                        {
                            op = Runtime.PyUnicode_AsUnicode(value);
                            Char[] buff = new Char[1];
                            Marshal.Copy(op, buff, 0, 1);
                            result = buff[0];
                            return true;
                        }
                        goto type_error;
                    }

                    op = Runtime.PyNumber_Int(value);
                    if (op == IntPtr.Zero)
                    {
                        goto type_error;
                    }
                    ival = Runtime.PyInt_AsLong(op);
                    Runtime.XDecref(op);
                    if (ival > Char.MaxValue || ival < Char.MinValue)
                    {
                        goto overflow;
                    }
                    result = (char)ival;
                    return true;

                case TypeCode.Int16:
                    op = Runtime.PyNumber_Int(value);
                    if (op == IntPtr.Zero)
                    {
                        if (Exceptions.ExceptionMatches(overflow))
                        {
                            goto overflow;
                        }
                        goto type_error;
                    }
                    ival = (int)Runtime.PyInt_AsLong(op);
                    Runtime.XDecref(op);
                    if (ival > Int16.MaxValue || ival < Int16.MinValue)
                    {
                        goto overflow;
                    }
                    short s = (short)ival;
                    result = s;
                    return true;

                case TypeCode.Int64:
                    op = Runtime.PyNumber_Long(value);
                    if (op == IntPtr.Zero)
                    {
                        if (Exceptions.ExceptionMatches(overflow))
                        {
                            goto overflow;
                        }
                        goto type_error;
                    }
                    long l = (long)Runtime.PyLong_AsLongLong(op);
                    Runtime.XDecref(op);
                    if ((l == -1) && Exceptions.ErrorOccurred())
                    {
                        goto overflow;
                    }
                    result = l;
                    return true;

                case TypeCode.UInt16:
                    op = Runtime.PyNumber_Int(value);
                    if (op == IntPtr.Zero)
                    {
                        if (Exceptions.ExceptionMatches(overflow))
                        {
                            goto overflow;
                        }
                        goto type_error;
                    }
                    ival = (int)Runtime.PyInt_AsLong(op);
                    Runtime.XDecref(op);
                    if (ival > UInt16.MaxValue || ival < UInt16.MinValue)
                    {
                        goto overflow;
                    }
                    ushort us = (ushort)ival;
                    result = us;
                    return true;

                case TypeCode.UInt32:
                    op = Runtime.PyNumber_Long(value);
                    if (op == IntPtr.Zero)
                    {
                        if (Exceptions.ExceptionMatches(overflow))
                        {
                            goto overflow;
                        }
                        goto type_error;
                    }
                    uint ui = (uint)Runtime.PyLong_AsUnsignedLong(op);

                    if (Exceptions.ErrorOccurred())
                    {
                        Runtime.XDecref(op);
                        goto overflow;
                    }

                    IntPtr check = Runtime.PyLong_FromUnsignedLong(ui);
                    int err = Runtime.PyObject_Compare(check, op);
                    Runtime.XDecref(check);
                    Runtime.XDecref(op);
                    if (0 != err || Exceptions.ErrorOccurred())
                    {
                        goto overflow;
                    }

                    result = ui;
                    return true;

                case TypeCode.UInt64:
                    op = Runtime.PyNumber_Long(value);
                    if (op == IntPtr.Zero)
                    {
                        if (Exceptions.ExceptionMatches(overflow))
                        {
                            goto overflow;
                        }
                        goto type_error;
                    }
                    ulong ul = (ulong)Runtime.PyLong_AsUnsignedLongLong(op);
                    Runtime.XDecref(op);
                    if (Exceptions.ErrorOccurred())
                    {
                        goto overflow;
                    }
                    result = ul;
                    return true;


                case TypeCode.Single:
                    op = Runtime.PyNumber_Float(value);
                    if (op == IntPtr.Zero)
                    {
                        if (Exceptions.ExceptionMatches(overflow))
                        {
                            goto overflow;
                        }
                        goto type_error;
                    }
                    double dd = Runtime.PyFloat_AsDouble(op);
                    Runtime.CheckExceptionOccurred();
                    Runtime.XDecref(op);
                    if (dd > Single.MaxValue || dd < Single.MinValue)
                    {
                        if (!double.IsInfinity(dd))
                        {
                            goto overflow;
                        }
                    }
                    result = (float)dd;
                    return true;

                case TypeCode.Double:
                    op = Runtime.PyNumber_Float(value);
                    if (op == IntPtr.Zero)
                    {
                        goto type_error;
                    }
                    double d = Runtime.PyFloat_AsDouble(op);
                    Runtime.CheckExceptionOccurred();
                    Runtime.XDecref(op);
                    result = d;
                    return true;

                case TypeCode.Decimal:
                    op = Runtime.PyObject_Str(value);
                    decimal m;
                    string sm = Runtime.GetManagedString(op);
                    if (!Decimal.TryParse(sm, NumberStyles.Number, nfi, out m))
                    {
                        goto type_error;
                    }
                    Runtime.XDecref(op);
                    result = m;
                    return true;

                case TypeCode.DateTime:
                    op = Runtime.PyObject_Str(value);
                    DateTime dt;
                    string sdt = Runtime.GetManagedString(op);
                    if (!DateTime.TryParse(sdt, out dt))
                    {
                        goto type_error;
                    }
                    Runtime.XDecref(op);
                    result = dt;
                    return true;
            }


            type_error:

            if (setError)
            {
                string tpName = Runtime.PyObject_GetTypeName(value);
                Exceptions.SetError(Exceptions.TypeError, $"'{tpName}' value cannot be converted to {obType}");
            }

            return false;

            overflow:

            if (setError)
            {
                Exceptions.SetError(Exceptions.OverflowError, "value too large to convert");
            }

            return false;
        }


        private static void SetConversionError(IntPtr value, Type target)
        {
            IntPtr ob = Runtime.PyObject_Repr(value);
            string src = Runtime.GetManagedString(ob);
            Runtime.XDecref(ob);
            Exceptions.SetError(Exceptions.TypeError, $"Cannot convert {src} to {target}");
        }


        /// <summary>
        /// Convert a Python value to a correctly typed managed array instance.
        /// The Python value must support the Python sequence protocol and the
        /// items in the sequence must be convertible to the target array type.
        /// </summary>
        private static bool ToArray(IntPtr value, Type obType, out object result, bool setError)
        {
            Type elementType = obType.GetElementType();
            var size = Runtime.PySequence_Size(value);
            result = null;

            if (size < 0 || elementType.IsGenericType)
            {
                if (setError)
                {
                    SetConversionError(value, obType);
                }
                return false;
            }

            Array items = Array.CreateInstance(elementType, size);

            // XXX - is there a better way to unwrap this if it is a real array?
            for (var i = 0; i < size; i++)
            {
                object obj = null;
                IntPtr item = Runtime.PySequence_GetItem(value, i);
                if (item == IntPtr.Zero)
                {
                    if (setError)
                    {
                        SetConversionError(value, obType);
                        return false;
                    }
                }

                if (!Converter.ToManaged(item, elementType, out obj, true))
                {
                    Runtime.XDecref(item);
                    return false;
                }

                items.SetValue(obj, i);
                Runtime.XDecref(item);
            }

            result = items;
            return true;
        }


        /// <summary>
        /// Convert a Python value to a correctly typed managed enum instance.
        /// </summary>
        private static bool ToEnum(IntPtr value, Type obType, out object result, bool setError)
        {
            Type etype = Enum.GetUnderlyingType(obType);
            result = null;

            if (!ToPrimitive(value, etype, out result, setError))
            {
                return false;
            }

            if (Enum.IsDefined(obType, result))
            {
                result = Enum.ToObject(obType, result);
                return true;
            }

            if (obType.GetCustomAttributes(flagsType, true).Length > 0)
            {
                result = Enum.ToObject(obType, result);
                return true;
            }

            if (setError)
            {
                Exceptions.SetError(Exceptions.ValueError, "invalid enumeration value");
            }

            return false;
        }
    }

    public static class ConverterExtension
    {
        public static PyObject ToPython(this object o)
        {
            return new PyObject(Converter.ToPython(o, o?.GetType()));
        }
    }
}
