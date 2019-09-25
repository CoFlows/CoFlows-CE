using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Globalization;
using System.Diagnostics;
using System.Runtime.Serialization;
using AQI.AQILabs.Kernel.Numerics.Math.Properties;
using AQI.AQILabs.Kernel.Numerics.Math.LinearAlgebra.Decomposition;

namespace AQI.AQILabs.Kernel.Numerics.Math.LinearAlgebra
{
    using Math = System.Math;

    [Serializable]
    public sealed class NotPositiveDefiniteException : MatrixException
    {
        // Methods
        public NotPositiveDefiniteException()
        {
        }

        public NotPositiveDefiniteException(string message)
            : base(message)
        {
        }

        private NotPositiveDefiniteException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public NotPositiveDefiniteException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    [Serializable]
    public sealed class SingularMatrixException : MatrixException
    {
        // Methods
        public SingularMatrixException()
        {
        }

        public SingularMatrixException(string message) : base(message)
        {
        }

        private SingularMatrixException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public SingularMatrixException(string message, Exception inner) : base(message, inner)
        {
        }
    }

    [Serializable]
    public sealed class InvalidMatrixOperationException : MatrixException
    {
        // Methods
        public InvalidMatrixOperationException()
        {
        }

        public InvalidMatrixOperationException(string message)
            : base(message)
        {
        }

        private InvalidMatrixOperationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public InvalidMatrixOperationException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    [Serializable]
    public abstract class MatrixException : AQITimeSeriesMathException
    {
        // Methods
        protected MatrixException()
        {
        }

        protected MatrixException(string message)
            : base(message)
        {
        }

        protected MatrixException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        protected MatrixException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    [Serializable]
    public sealed class MatrixNotSquareException : MatrixException
    {
        // Methods
        public MatrixNotSquareException()
        {
        }

        public MatrixNotSquareException(string message)
            : base(message)
        {
        }

        private MatrixNotSquareException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public MatrixNotSquareException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    [Serializable]
    public abstract class Vector : IFormattable, IEnumerable<double>, IEnumerable, ICloneable, IEquatable<Vector>
    {
        // Fields
        private int mCount;

        // Methods
        protected Vector(int size)
        {
            if (size < 1)
            {
                throw new ArgumentException(Resources.NotPositive, "size");
            }
            this.mCount = size;
        }

        public virtual double AbsoluteMaximum()
        {
            return Math.Abs(this[this.AbsoluteMaximumIndex()]);
        }

        public virtual int AbsoluteMaximumIndex()
        {
            int num = 0;
            double num2 = Math.Abs(this[num]);
            for (int i = 1; i < this.Count; i++)
            {
                double num4 = Math.Abs(this[i]);
                if (num4 > num2)
                {
                    num = i;
                    num2 = num4;
                }
            }
            return num;
        }

        public virtual double AbsoluteMinimum()
        {
            return Math.Abs(this[this.AbsoluteMinimumIndex()]);
        }

        public virtual int AbsoluteMinimumIndex()
        {
            int num = 0;
            double num2 = Math.Abs(this[num]);
            for (int i = 1; i < this.Count; i++)
            {
                double num4 = Math.Abs(this[i]);
                if (num4 < num2)
                {
                    num = i;
                    num2 = num4;
                }
            }
            return num;
        }

        public virtual void Add(Vector other)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }
            if (this.Count != other.Count)
            {
                throw new NotConformableException("other", Resources.ParameterNotConformable);
            }
            for (int i = 0; i < this.Count; i++)
            {
                Vector vector;
                int num2;
                (vector = this)[num2 = i] = vector[num2] + other[i];
            }
        }

        public virtual void Add(double scalar)
        {
            if (scalar != 0.0)
            {
                for (int i = 0; i < this.Count; i++)
                {
                    Vector vector;
                    int num2;
                    (vector = this)[num2 = i] = vector[num2] + scalar;
                }
            }
        }

        public virtual void Add(Vector other, Vector result)
        {
            if (result == null)
            {
                throw new ArgumentNullException("result");
            }
            if (this.Count != result.Count)
            {
                throw new NotConformableException("result");
            }
            if (object.ReferenceEquals(this, result) || object.ReferenceEquals(other, result))
            {
                Vector vector = result.CreateVector(result.Count);
                this.Add(other, vector);
                vector.CopyTo(result);
            }
            else
            {
                this.CopyTo(result);
                result.Add(other);
            }
        }

        public virtual void Add(double scalar, Vector result)
        {
            if (result == null)
            {
                throw new ArgumentNullException("result");
            }
            if (this.Count != result.Count)
            {
                throw new NotConformableException("result", Resources.ParameterNotConformable);
            }
            this.CopyTo(result);
            result.Add(scalar);
        }

        public virtual void AddScaledVector(double scale, Vector other)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }
            if (this.Count != other.Count)
            {
                throw new NotConformableException("other", Resources.ParameterNotConformable);
            }
            for (int i = 0; i < this.Count; i++)
            {
                Vector vector;
                int num2;
                (vector = this)[num2 = i] = vector[num2] + (scale * other[i]);
            }
        }

        public virtual void AddScaledVector(double scale, Vector other, Vector result)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }
            if (result == null)
            {
                throw new ArgumentNullException("result");
            }
            if (this.Count != other.Count)
            {
                throw new NotConformableException("other", Resources.ParameterNotConformable);
            }
            if (this.Count != result.Count)
            {
                throw new NotConformableException("result", Resources.ParameterNotConformable);
            }
            for (int i = 0; i < this.Count; i++)
            {
                result[i] = this[i] + (scale * other[i]);
            }
        }

        public virtual void Clear()
        {
            for (int i = 0; i < this.Count; i++)
            {
                this[i] = 0.0;
            }
        }

        public Vector Clone()
        {
            Vector target = this.CreateVector(this.Count);
            this.CopyTo(target);
            return target;
        }

        public virtual void CopyTo(Vector target)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }
            if (this.Count != target.Count)
            {
                throw new NotConformableException("target", Resources.ParameterNotConformable);
            }
            if (!object.ReferenceEquals(this, target))
            {
                for (int i = 0; i < this.Count; i++)
                {
                    target[i] = this[i];
                }
            }
        }

        public virtual void CopyTo(Vector destination, int offset, int destinationOffset, int count)
        {
            if (destination == null)
            {
                throw new ArgumentNullException("destination");
            }
            if (offset >= this.mCount)
            {
                throw new ArgumentOutOfRangeException("offset");
            }
            if ((offset + count) > this.mCount)
            {
                throw new ArgumentOutOfRangeException("count");
            }
            if (destinationOffset >= destination.Count)
            {
                throw new ArgumentOutOfRangeException("destinationOffset");
            }
            if ((destinationOffset + count) > destination.Count)
            {
                throw new ArgumentOutOfRangeException("count");
            }
            if (object.ReferenceEquals(this, destination))
            {
                Vector vector = destination.CreateVector(destination.Count);
                this.CopyTo(vector, offset, destinationOffset, count);
                vector.CopyTo(destination);
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    destination[destinationOffset + i] = this[offset + i];
                }
            }
        }

        public abstract Matrix CreateMatrix(int rows, int columns);
        protected internal abstract Vector CreateVector(int size);
        public virtual void Divide(double scalar)
        {
            this.Multiply((double)(1.0 / scalar));
        }

        public virtual void Divide(double scalar, Vector result)
        {
            this.Multiply(1.0 / scalar, result);
        }

        public virtual double DotProduct()
        {
            return this.DotProduct(this);
        }

        public virtual double DotProduct(Vector other)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }
            if (this.Count != other.Count)
            {
                throw new NotConformableException("other", Resources.ParameterNotConformable);
            }
            double num = 0.0;
            for (int i = 0; i < this.Count; i++)
            {
                num += this[i] * other[i];
            }
            return num;
        }

        public bool Equals(Vector other)
        {
            if (other == null)
            {
                return false;
            }
            if (this.Count != other.Count)
            {
                return false;
            }
            if (!object.ReferenceEquals(this, other))
            {
                for (int i = 0; i < this.Count; i++)
                {
                    if (this[i] != other[i])
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as Vector);
        }

        public virtual IEnumerator<double> GetEnumerator()
        {
            EnumeratorDouble d__ = new EnumeratorDouble(0);
            d__.__this = this;
            return d__;
        }

        public virtual IEnumerator<KeyValuePair<int, double>> GetEnumerator(int index, int length)
        {
            EnumeratorKeyValuePair d__ = new EnumeratorKeyValuePair(0);
            d__.__this = this;
            d__.index = index;
            d__.length = length;
            return d__;
        }

        public override int GetHashCode()
        {
            int num = Math.Min(this.Count, 20);
            long num2 = 0L;
            for (int i = 0; i < num; i++)
            {
                num2 ^= BitConverter.DoubleToInt64Bits(this[i]);
            }
            return BitConverter.ToInt32(BitConverter.GetBytes(num2), 4);
        }

        public virtual IEnumerable<KeyValuePair<int, double>> GetIndexedEnumerator()
        {
            IndexedEnumeratorKeyValuePair d__ = new IndexedEnumeratorKeyValuePair(-2);
            d__.__this = this;
            return d__;
        }

        public virtual Vector GetSubVector(int index, int length)
        {
            if ((index < 0) || (index >= this.Count))
            {
                throw new ArgumentOutOfRangeException("index");
            }
            if ((index + length) > this.Count)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            Vector vector = this.CreateVector(length);
            int num = index;
            for (int i = 0; num < (index + length); i++)
            {
                vector[i] = this[num];
                num++;
            }
            return vector;
        }

        public virtual double InfinityNorm()
        {
            double num = 0.0;
            for (int i = 0; i < this.Count; i++)
            {
                num = Math.Max(num, Math.Abs(this[i]));
            }
            return num;
        }

        public virtual double Maximum()
        {
            return this[this.MaximumIndex()];
        }

        public virtual int MaximumIndex()
        {
            int num = 0;
            double num2 = this[0];
            for (int i = 1; i < this.Count; i++)
            {
                if (num2 < this[i])
                {
                    num = i;
                    num2 = this[i];
                }
            }
            return num;
        }

        public virtual double Minimum()
        {
            return this[this.MinimumIndex()];
        }

        public virtual int MinimumIndex()
        {
            int num = 0;
            double num2 = this[0];
            for (int i = 1; i < this.Count; i++)
            {
                if (num2 > this[i])
                {
                    num = i;
                    num2 = this[i];
                }
            }
            return num;
        }
        /*
        public virtual Matrix Multiply(Vector other)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }
            
            Matrix result = new DenseMatrix(this.Count, other.Count);
            this.Multiply(other, result);
            return result;
            
        }
        */
        public virtual double Multiply(Vector other)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }

            double result = 0;
            for (int i = 0; i < this.Count; i++)
                result += this[i] * other[i];
            return result;

        }

        public virtual void Multiply(double scalar)
        {
            if (scalar != 1.0)
            {
                for (int i = 0; i < this.Count; i++)
                {
                    Vector vector;
                    int num2;
                    (vector = this)[num2 = i] = vector[num2] * scalar;
                }
            }
        }

        public virtual void Multiply(Vector other, Matrix result)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }
            if (result == null)
            {
                throw new ArgumentNullException("result");
            }
            if ((result.Rows != this.Count) || (result.Columns != other.Count))
            {
                throw new NotConformableException("result", Resources.ResultMatrixIncorrectDimensions);
            }
            for (int i = 0; i < this.Count; i++)
            {
                for (int j = 0; j < other.Count; j++)
                {
                    result[i, j] = this[i] * other[j];
                }
            }
        }

        public virtual void Multiply(double scalar, Vector result)
        {
            if (result == null)
            {
                throw new ArgumentNullException("result");
            }
            if (this.Count != result.Count)
            {
                throw new NotConformableException("result", Resources.ParameterNotConformable);
            }
            this.CopyTo(result);
            result.Multiply(scalar);
        }

        public virtual void Negate()
        {
            this.Multiply((double)-1.0);
        }

        public virtual Vector Normalize(int pValue)
        {
            if (pValue < 1)
            {
                throw new ArgumentOutOfRangeException("pValue", Resources.NotPositive);
            }
            Vector vector = this.Clone();
            double scalar = this.PNorm(pValue);
            vector.Divide(scalar);
            return vector;
        }

        public static Vector operator +(Vector leftSide, Vector rightSide)
        {
            if (rightSide == null)
            {
                throw new ArgumentNullException("rightSide");
            }
            if (leftSide == null)
            {
                throw new ArgumentNullException("leftSide");
            }
            if (leftSide.mCount != rightSide.mCount)
            {
                throw new NotConformableException(Resources.ParametersNotConformable);
            }
            Vector vector = leftSide.Clone();
            vector.Add(rightSide);
            return vector;
        }

        public static Vector operator /(Vector leftSide, double rightSide)
        {
            if (leftSide == null)
            {
                throw new ArgumentNullException("leftSide");
            }
            Vector vector = leftSide.Clone();
            vector.Divide(rightSide);
            return vector;
        }
        /*
        public static Matrix operator *(Vector leftSide, Vector rightSide)
        {
            if (rightSide == null)
            {
                throw new ArgumentNullException("rightSide");
            }
            if (leftSide == null)
            {
                throw new ArgumentNullException("leftSide");
            }
            return leftSide.Multiply(rightSide);
        }
        */
        public static double operator *(Vector leftSide, Vector rightSide)
        {
            if (rightSide == null)
            {
                throw new ArgumentNullException("rightSide");
            }
            if (leftSide == null)
            {
                throw new ArgumentNullException("leftSide");
            }
            return leftSide.Multiply(rightSide);
        }
        public static Vector operator *(Vector leftSide, double rightSide)
        {
            if (leftSide == null)
            {
                throw new ArgumentNullException("leftSide");
            }
            Vector vector = leftSide.Clone();
            vector.Multiply(rightSide);
            return vector;
        }

        public static Vector operator *(double leftSide, Vector rightSide)
        {
            if (rightSide == null)
            {
                throw new ArgumentNullException("rightSide");
            }
            Vector vector = rightSide.Clone();
            vector.Multiply(leftSide);
            return vector;
        }

        public static Vector operator -(Vector leftSide, Vector rightSide)
        {
            if (rightSide == null)
            {
                throw new ArgumentNullException("rightSide");
            }
            if (leftSide == null)
            {
                throw new ArgumentNullException("leftSide");
            }
            if (leftSide.Count != rightSide.Count)
            {
                throw new NotConformableException(Resources.ParametersNotConformable);
            }
            Vector vector = leftSide.Clone();
            vector.Subtract(rightSide);
            return vector;
        }

        public static Vector operator -(Vector rightSide)
        {
            if (rightSide == null)
            {
                throw new ArgumentNullException("rightSide");
            }
            Vector vector = rightSide.Clone();
            vector.Negate();
            return vector;
        }

        public static Vector operator +(Vector rightSide)
        {
            if (rightSide == null)
            {
                throw new ArgumentNullException("rightSide");
            }
            return rightSide.Clone();
        }

        public virtual Vector Plus()
        {
            return this.Clone();
        }

        public virtual double PNorm(int pValue)
        {
            if (pValue < 1)
            {
                throw new ArgumentOutOfRangeException("pValue", Resources.NotPositive);
            }
            if (pValue == 1)
            {
                double num = 0.0;
                for (int j = 0; j < this.Count; j++)
                {
                    num += Math.Abs(this[j]);
                }
                return num;
            }
            double x = 0.0;
            for (int i = 0; i < this.Count; i++)
            {
                x += Math.Pow(Math.Abs(this[i]), (double)pValue);
            }
            return Math.Pow(x, 1.0 / ((double)pValue));
        }

        public virtual Vector PointwiseMultiply(Vector other)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }
            if (this.Count != other.Count)
            {
                throw new NotConformableException("other", Resources.ParameterNotConformable);
            }
            Vector result = this.CreateVector(this.Count);
            this.PointwiseMultiply(other, result);
            return result;
        }

        public virtual void PointwiseMultiply(Vector other, Vector result)
        {
            if (result == null)
            {
                throw new ArgumentNullException("result");
            }
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }
            if (this.Count != other.Count)
            {
                throw new NotConformableException("other", Resources.ParameterNotConformable);
            }
            if (this.Count != result.Count)
            {
                throw new NotConformableException("result");
            }
            for (int i = 0; i < this.Count; i++)
            {
                result[i] = this[i] * other[i];
            }
        }

        public virtual void SetValues(double[] values)
        {
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }
            if (values.Length != this.Count)
            {
                throw new NotConformableException("values", Resources.ArrayParameterNotConformable);
            }
            for (int i = 0; i < values.Length; i++)
            {
                this[i] = values[i];
            }
        }

        public virtual void Subtract(Vector other)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }
            if (this.Count != other.Count)
            {
                throw new NotConformableException("other", Resources.ParameterNotConformable);
            }
            for (int i = 0; i < this.Count; i++)
            {
                Vector vector;
                int num2;
                (vector = this)[num2 = i] = vector[num2] - other[i];
            }
        }

        public virtual void Subtract(double scalar)
        {
            this.Add(-scalar);
        }

        public virtual void Subtract(Vector other, Vector result)
        {
            if (result == null)
            {
                throw new ArgumentNullException("result");
            }
            if (this.Count != result.Count)
            {
                throw new NotConformableException("result", Resources.ParameterNotConformable);
            }
            if (object.ReferenceEquals(this, result) || object.ReferenceEquals(other, result))
            {
                Vector vector = result.CreateVector(result.Count);
                this.Subtract(other, vector);
                vector.CopyTo(result);
            }
            else
            {
                this.CopyTo(result);
                result.Subtract(other);
            }
        }

        public virtual void Subtract(double scalar, Vector result)
        {
            this.Add(-scalar, result);
        }

        public virtual double Sum()
        {
            double num = 0.0;
            for (int i = 0; i < this.Count; i++)
            {
                num += this[i];
            }
            return num;
        }

        public virtual double SumMagnitudes()
        {
            double num = 0.0;
            for (int i = 0; i < this.Count; i++)
            {
                num += Math.Abs(this[i]);
            }
            return num;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        object ICloneable.Clone()
        {
            return this.Clone();
        }

        public virtual double[] ToArray()
        {
            double[] numArray = new double[this.Count];
            for (int i = 0; i < numArray.Length; i++)
            {
                numArray[i] = this[i];
            }
            return numArray;
        }

        public override string ToString()
        {
            return this.ToString(null, null);
        }

        public virtual string ToString(IFormatProvider formatProvider)
        {
            return this.ToString(null, formatProvider);
        }

        public virtual string ToString(string format)
        {
            return this.ToString(format, null);
        }

        public virtual string ToString(string format, IFormatProvider formatProvider)
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < this.Count; i++)
            {
                builder.Append(this[i].ToString(format, formatProvider));
                if (i != (this.Count - 1))
                {
                    builder.Append(", ");
                }
            }
            return builder.ToString();
        }

        // Properties
        public int Count
        {
            get
            {
                return this.mCount;
            }
        }

        public abstract double this[int index] { get; set; }

        // Nested Types
        [CompilerGenerated]
        private sealed class EnumeratorDouble : IEnumerator<double>, IEnumerator, IDisposable
        {
            // Fields
            private int __state;
            private double __current;
            public Vector __this;
            public int __1;

            // Methods
            //[DebuggerHidden]
            public EnumeratorDouble(int __state)
            {
                this.__state = __state;
            }

            public bool MoveNext()
            {
                switch (this.__state)
                {
                    case 0:
                        this.__state = -1;
                        this.__1 = 0;
                        break;

                    case 1:
                        this.__state = -1;
                        this.__1++;
                        break;

                    default:
                        goto Label_0075;
                }
                if (this.__1 < this.__this.Count)
                {
                    this.__current = this.__this[this.__1];
                    this.__state = 1;
                    return true;
                }
                Label_0075:
                return false;
            }

            //[DebuggerHidden]
            void IEnumerator.Reset()
            {
                throw new NotSupportedException();
            }

            void IDisposable.Dispose()
            {
            }

            // Properties
            double IEnumerator<double>.Current
            {
                //[DebuggerHidden]
                get
                {
                    return this.__current;
                }
            }

            object IEnumerator.Current
            {
                //[DebuggerHidden]
                get
                {
                    return this.__current;
                }
            }
        }

        [CompilerGenerated]
        private sealed class EnumeratorKeyValuePair : IEnumerator<KeyValuePair<int, double>>, IEnumerator, IDisposable
        {
            // Fields
            private int __state;
            private KeyValuePair<int, double> __current;
            public Vector __this;
            public int __8;
            public int index;
            public int length;

            // Methods
            [DebuggerHidden]
            public EnumeratorKeyValuePair(int __state)
            {
                this.__state = __state;
            }

            public bool MoveNext()
            {
                switch (this.__state)
                {
                    case 0:
                        this.__state = -1;
                        if (this.index > this.__this.mCount)
                        {
                            throw new ArgumentOutOfRangeException("index");
                        }
                        if ((this.index + this.length) > this.__this.mCount)
                        {
                            throw new ArgumentOutOfRangeException("length");
                        }
                        this.__8 = this.index;
                        while (this.__8 < this.length)
                        {
                            this.__current = new KeyValuePair<int, double>(this.__8, this.__this[this.__8]);
                            this.__state = 1;
                            return true;
                        }
                        break;

                    case 1:
                        this.__state = -1;
                        this.__8++;
                        while (this.__8 < this.length)
                        {
                            this.__current = new KeyValuePair<int, double>(this.__8, this.__this[this.__8]);
                            this.__state = 1;
                            return true;
                        }
                        break;
                }
                return false;
            }

            [DebuggerHidden]
            void IEnumerator.Reset()
            {
                throw new NotSupportedException();
            }

            void IDisposable.Dispose()
            {
            }

            // Properties
            KeyValuePair<int, double> IEnumerator<KeyValuePair<int, double>>.Current
            {
                [DebuggerHidden]
                get
                {
                    return this.__current;
                }
            }

            object IEnumerator.Current
            {
                [DebuggerHidden]
                get
                {
                    return this.__current;
                }
            }
        }

        [CompilerGenerated]
        private sealed class IndexedEnumeratorKeyValuePair : IEnumerable<KeyValuePair<int, double>>, IEnumerable, IEnumerator<KeyValuePair<int, double>>, IEnumerator, IDisposable
        {
            // Fields
            private int __state;
            private KeyValuePair<int, double> __current;
            public Vector __this;
            private int __initialThreadId;
            public int __4;

            // Methods
            [DebuggerHidden]
            public IndexedEnumeratorKeyValuePair(int __state)
            {
                this.__state = __state;
                this.__initialThreadId = Thread.CurrentThread.ManagedThreadId;
            }

            public bool MoveNext()
            {
                switch (this.__state)
                {
                    case 0:
                        this.__state = -1;
                        this.__4 = 0;
                        break;

                    case 1:
                        this.__state = -1;
                        this.__4++;
                        break;

                    default:
                        goto Label_0080;
                }
                if (this.__4 < this.__this.Count)
                {
                    this.__current = new KeyValuePair<int, double>(this.__4, this.__this[this.__4]);
                    this.__state = 1;
                    return true;
                }
                Label_0080:
                return false;
            }

            [DebuggerHidden]
            IEnumerator<KeyValuePair<int, double>> IEnumerable<KeyValuePair<int, double>>.GetEnumerator()
            {
                if ((Thread.CurrentThread.ManagedThreadId == this.__initialThreadId) && (this.__state == -2))
                {
                    this.__state = 0;
                    return this;
                }
                Vector.IndexedEnumeratorKeyValuePair d__ = new Vector.IndexedEnumeratorKeyValuePair(0);
                d__.__this = this.__this;
                return d__;
            }

            [DebuggerHidden]
            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable<KeyValuePair<Int32, Double>>)this).GetEnumerator();
            }

            [DebuggerHidden]
            void IEnumerator.Reset()
            {
                throw new NotSupportedException();
            }

            void IDisposable.Dispose()
            {
            }

            // Properties
            KeyValuePair<int, double> IEnumerator<KeyValuePair<int, double>>.Current
            {
                [DebuggerHidden]
                get
                {
                    return this.__current;
                }
            }

            object IEnumerator.Current
            {
                [DebuggerHidden]
                get
                {
                    return this.__current;
                }
            }
        }
    }

    [Serializable]
    public class DenseVector : Vector
    {
        // Fields
        private readonly double[] mData;

        // Methods
        public DenseVector(Vector other)
            : this(other.Count)
        {
            DenseVector vector = other as DenseVector;
            if (vector == null)
            {
                foreach (KeyValuePair<int, double> pair in other.GetIndexedEnumerator())
                {
                    this.mData[pair.Key] = pair.Value;
                }
            }
            else
            {
                Buffer.BlockCopy(vector.mData, 0, this.mData, 0, this.mData.Length * 8);
            }
        }

        public DenseVector(double[] array)
            : this(array.Length)
        {
            Buffer.BlockCopy(array, 0, this.mData, 0, array.Length * 8);
        }

        public DenseVector(IList<double> list)
            : this(list.Count)
        {
            for (int i = 0; i < this.mData.Length; i++)
            {
                this.mData[i] = list[i];
            }
        }

        public DenseVector(int size)
            : base(size)
        {
            this.mData = new double[size];
        }

        public DenseVector(int size, double value)
            : this(size)
        {
            for (int i = 0; i < this.mData.Length; i++)
            {
                this.mData[i] = value;
            }
        }

        public override void Add(Vector other)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }
            if (base.Count != other.Count)
            {
                throw new NotConformableException("other", Resources.ParameterNotConformable);
            }
            DenseVector vector = other as DenseVector;
            if (vector == null)
            {
                base.Add(other);
            }
            else
            {
                for (int i = 0; i < this.mData.Length; i++)
                {
                    this.mData[i] += vector.mData[i];
                }
            }
        }

        public override void Add(double scalar)
        {
            if (scalar != 0.0)
            {
                for (int i = 0; i < this.mData.Length; i++)
                {
                    this.mData[i] += scalar;
                }
            }
        }

        public Matrix GetDiagonalMatrix()
        {
            DenseMatrix matrix = new DenseMatrix(this.Count, this.Count, 0.0);
            for (int i = 0; i < this.Count; i++)
                matrix[i, i] = this[i];

            return matrix;
        }

        public override void Clear()
        {
            Array.Clear(this.mData, 0, this.mData.Length);
        }

        public override void CopyTo(Vector target)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }
            if (base.Count != target.Count)
            {
                throw new NotConformableException("target", Resources.ParameterNotConformable);
            }
            DenseVector vector = target as DenseVector;
            if (vector == null)
            {
                for (int i = 0; i < this.mData.Length; i++)
                {
                    target[i] = this.mData[i];
                }
            }
            else
            {
                Buffer.BlockCopy(this.mData, 0, vector.mData, 0, this.mData.Length * 8);
            }
        }

        public override void CopyTo(Vector destination, int offset, int destinationOffset, int count)
        {
            if (destination == null)
            {
                throw new ArgumentNullException("destination");
            }
            if (offset >= this.mData.Length)
            {
                throw new ArgumentOutOfRangeException("offset");
            }
            if ((offset + count) > this.mData.Length)
            {
                throw new ArgumentOutOfRangeException("count");
            }
            if (destinationOffset >= destination.Count)
            {
                throw new ArgumentOutOfRangeException("destinationOffset");
            }
            if ((destinationOffset + count) > destination.Count)
            {
                throw new ArgumentOutOfRangeException("count");
            }
            DenseVector vector = destination as DenseVector;
            if (vector == null)
            {
                base.CopyTo(destination, offset, destinationOffset, count);
            }
            else
            {
                Buffer.BlockCopy(this.mData, offset * 8, vector.mData, destinationOffset * 8, count * 8);
            }
        }

        public override Matrix CreateMatrix(int rows, int columns)
        {
            return new DenseMatrix(rows, columns);
        }

        protected internal override Vector CreateVector(int size)
        {
            return new DenseVector(size);
        }

        public override double DotProduct(Vector other)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }
            if (base.Count != other.Count)
            {
                throw new NotConformableException("other", Resources.ParameterNotConformable);
            }
            DenseVector vector = other as DenseVector;
            if (vector == null)
            {
                return base.DotProduct(other);
            }
            double num = 0.0;
            for (int i = 0; i < this.mData.Length; i++)
            {
                num += this.mData[i] * vector.mData[i];
            }
            return num;
        }

        public override Vector GetSubVector(int index, int length)
        {
            if ((index < 0) || (index >= base.Count))
            {
                throw new ArgumentOutOfRangeException("index");
            }
            if ((index + length) > base.Count)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            DenseVector vector = new DenseVector(length);
            Buffer.BlockCopy(this.mData, index * 8, vector.mData, 0, length * 8);
            return vector;
        }

        public override void Multiply(double scalar)
        {
            if (scalar != 1.0)
            {
                for (int i = 0; i < this.mData.Length; i++)
                {
                    this.mData[i] *= scalar;
                }
            }
        }

        public static DenseVector operator +(DenseVector leftSide, DenseVector rightSide)
        {
            if (rightSide == null)
            {
                throw new ArgumentNullException("rightSide");
            }
            if (leftSide == null)
            {
                throw new ArgumentNullException("leftSide");
            }
            if (leftSide.Count != rightSide.Count)
            {
                throw new NotConformableException(Resources.ParametersNotConformable);
            }
            DenseVector vector = new DenseVector(leftSide);
            vector.Add(rightSide);
            return vector;
        }

        public static DenseVector operator /(DenseVector leftSide, double rightSide)
        {
            if (leftSide == null)
            {
                throw new ArgumentNullException("leftSide");
            }
            DenseVector vector = new DenseVector(leftSide);
            vector.Divide(rightSide);
            return vector;
        }
        /*
        public static DenseMatrix operator *(DenseVector leftSide, DenseVector rightSide)
        {
            if (rightSide == null)
            {
                throw new ArgumentNullException("rightSide");
            }
            if (leftSide == null)
            {
                throw new ArgumentNullException("leftSide");
            }
            return (DenseMatrix)leftSide.Multiply(rightSide);
        }
        */
        public static double operator *(DenseVector leftSide, DenseVector rightSide)
        {
            if (rightSide == null)
            {
                throw new ArgumentNullException("rightSide");
            }
            if (leftSide == null)
            {
                throw new ArgumentNullException("leftSide");
            }
            return leftSide.Multiply(rightSide);
        }

        public static DenseVector operator *(DenseVector leftSide, double rightSide)
        {
            if (leftSide == null)
            {
                throw new ArgumentNullException("leftSide");
            }
            DenseVector vector = new DenseVector(leftSide);
            vector.Multiply(rightSide);
            return vector;
        }

        public static DenseVector operator *(double leftSide, DenseVector rightSide)
        {
            if (rightSide == null)
            {
                throw new ArgumentNullException("rightSide");
            }
            DenseVector vector = new DenseVector(rightSide);
            vector.Multiply(leftSide);
            return vector;
        }

        public static DenseVector operator -(DenseVector leftSide, DenseVector rightSide)
        {
            if (rightSide == null)
            {
                throw new ArgumentNullException("rightSide");
            }
            if (leftSide == null)
            {
                throw new ArgumentNullException("leftSide");
            }
            if (leftSide.Count != rightSide.Count)
            {
                throw new NotConformableException(Resources.ParametersNotConformable);
            }
            DenseVector vector = new DenseVector(leftSide);
            vector.Subtract(rightSide);
            return vector;
        }

        public static DenseVector operator -(DenseVector rightSide)
        {
            if (rightSide == null)
            {
                throw new ArgumentNullException("rightSide");
            }
            DenseVector vector = new DenseVector(rightSide);
            vector.Negate();
            return vector;
        }

        public static DenseVector operator +(DenseVector rightSide)
        {
            if (rightSide == null)
            {
                throw new ArgumentNullException("rightSide");
            }
            return new DenseVector(rightSide);
        }

        public override void PointwiseMultiply(Vector other, Vector result)
        {
            if (result == null)
            {
                throw new ArgumentNullException("result");
            }
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }
            if (base.Count != other.Count)
            {
                throw new NotConformableException("other", Resources.ParameterNotConformable);
            }
            if (base.Count != result.Count)
            {
                throw new NotConformableException("result");
            }
            if ((other is DenseVector) && (result is DenseVector))
            {
                DenseVector vector = (DenseVector)other;
                DenseVector vector2 = (DenseVector)result;
                for (int i = 0; i < this.mData.Length; i++)
                {
                    vector2.mData[i] = this.mData[i] * vector.mData[i];
                }
            }
            else
            {
                base.PointwiseMultiply(other, result);
            }
        }

        public override void SetValues(double[] values)
        {
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }
            if (values.Length != base.Count)
            {
                throw new NotConformableException("values", Resources.ArrayParameterNotConformable);
            }
            Buffer.BlockCopy(values, 0, this.mData, 0, this.mData.Length * 8);
        }

        public override void Subtract(Vector other)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }
            if (base.Count != other.Count)
            {
                throw new NotConformableException("other", Resources.ParameterNotConformable);
            }
            DenseVector vector = other as DenseVector;
            if (vector == null)
            {
                base.Subtract(other);
            }
            else
            {
                for (int i = 0; i < this.mData.Length; i++)
                {
                    this.mData[i] -= vector.mData[i];
                }
            }
        }

        public override void Subtract(double scalar)
        {
            if (scalar != 0.0)
            {
                for (int i = 0; i < this.mData.Length; i++)
                {
                    this.mData[i] -= scalar;
                }
            }
        }

        public override double Sum()
        {
            double num = 0.0;
            for (int i = 0; i < this.mData.Length; i++)
            {
                num += this.mData[i];
            }
            return num;
        }

        public override double[] ToArray()
        {
            double[] dst = new double[base.Count];
            Buffer.BlockCopy(this.mData, 0, dst, 0, this.mData.Length * 8);
            return dst;
        }

        // Properties
        public double[] Data
        {
            get
            {
                return this.mData;
            }
        }

        public override double this[int index]
        {
            get
            {
                return this.mData[index];
            }
            set
            {
                this.mData[index] = value;
            }
        }
    }
    [Serializable]
    public abstract class Matrix : MarshalByRefObject, IFormattable, ICloneable, IEquatable<Matrix>
    {
        // Fields
        private readonly int mColumns;
        private readonly int mRows;

        // Methods
        protected Matrix(int rows, int columns)
        {
            if (rows < 1)
            {
                throw new ArgumentException(Resources.NotPositive, "rows");
            }
            if (columns < 1)
            {
                throw new ArgumentException(Resources.NotPositive, "columns");
            }
            this.mRows = rows;
            this.mColumns = columns;
        }

        public virtual void Add(Matrix other)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }
            if ((other.Rows != this.Rows) || (other.Columns != this.Columns))
            {
                throw new NotConformableException("other", Resources.ParameterNotConformable);
            }
            for (int i = 0; i < this.Rows; i++)
            {
                for (int j = 0; j < this.Columns; j++)
                {
                    double num3 = this.ValueAt(i, j) + other.ValueAt(i, j);
                    this.ValueAt(i, j, num3);
                }
            }
        }

        public virtual void Add(double scalar)
        {
            if (scalar != 0.0)
            {
                for (int i = 0; i < this.Rows; i++)
                {
                    for (int j = 0; j < this.Columns; j++)
                    {
                        this.ValueAt(i, j, this.ValueAt(i, j) + scalar);
                    }
                }
            }
        }

        public virtual void Add(Matrix other, Matrix result)
        {
            if (result == null)
            {
                throw new ArgumentNullException("result");
            }
            if ((result.Rows != this.Rows) || (result.Columns != this.Columns))
            {
                throw new NotConformableException("result", Resources.ParameterNotConformable);
            }
            if (object.ReferenceEquals(other, result))
            {
                other.CopyTo(result);
                result.Add(this);
            }
            else
            {
                this.CopyTo(result);
                result.Add(other);
            }
        }

        public virtual void Add(double scalar, Matrix result)
        {
            if (result == null)
            {
                throw new ArgumentNullException("result");
            }
            if ((result.Rows != this.Rows) || (result.Columns != this.Columns))
            {
                throw new NotConformableException("result", Resources.ParameterNotConformable);
            }
            this.CopyTo(result);
            result.Add(scalar);
        }

        public virtual Matrix Append(Matrix right)
        {
            if (right == null)
            {
                throw new ArgumentNullException("right");
            }
            if (right.Rows != this.Rows)
            {
                throw new NotConformableException("right", Resources.ParameterNotConformable);
            }
            Matrix result = this.CreateMatrix(this.Rows, this.Columns + right.Columns);
            this.Append(right, result);
            return result;
        }

        public virtual void Append(Matrix right, Matrix result)
        {
            if (right == null)
            {
                throw new ArgumentNullException("right");
            }
            if (right.Rows != this.Rows)
            {
                throw new NotConformableException("right", Resources.ParameterNotConformable);
            }
            if (result == null)
            {
                throw new ArgumentNullException("result");
            }
            if ((result.Columns != (this.Columns + right.Columns)) || (result.Rows != this.Rows))
            {
                throw new NotConformableException("result", Resources.ParameterNotConformable);
            }
            for (int i = 0; i < this.Columns; i++)
            {
                for (int k = 0; k < this.Rows; k++)
                {
                    result.ValueAt(k, i, this.ValueAt(k, i));
                }
            }
            for (int j = 0; j < right.Columns; j++)
            {
                for (int m = 0; m < right.Rows; m++)
                {
                    result.ValueAt(m, j + this.Columns, right.ValueAt(m, j));
                }
            }
        }

        public virtual void Clear()
        {
            for (int i = 0; i < this.Rows; i++)
            {
                for (int j = 0; j < this.Columns; j++)
                {
                    this.ValueAt(i, j, 0.0);
                }
            }
        }

        public virtual Matrix Clone()
        {
            Matrix target = this.CreateMatrix(this.Rows, this.Columns);
            this.CopyTo(target);
            return target;
        }

        public virtual double ConditionNumber()
        {
            Svd svd = new Svd(this, false);
            return svd.ConditionNumber();
        }

        public virtual void CopyTo(Matrix target)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }
            if (!object.ReferenceEquals(this, target))
            {
                if ((this.Rows != target.Rows) || (this.Columns != target.Columns))
                {
                    throw new NotConformableException("target", Resources.ParameterNotConformable);
                }
                foreach (KeyValuePair<int, Vector> pair in this.GetColumnEnumerator())
                {
                    foreach (KeyValuePair<int, double> pair2 in pair.Value.GetIndexedEnumerator())
                    {
                        target.ValueAt(pair2.Key, pair.Key, pair2.Value);
                    }
                }
            }
        }

        protected internal abstract Matrix CreateMatrix(int numberOfRows, int numberOfColumns);
        protected internal abstract Vector CreateVector(int size);
        public virtual double Determinant()
        {
            if (this.Rows != this.Columns)
            {
                throw new MatrixNotSquareException();
            }
            LU lu = new LU(this);
            return lu.Determinant();
        }

        public virtual Matrix DiagonalStack(Matrix lower)
        {
            if (lower == null)
            {
                throw new ArgumentNullException("lower");
            }
            Matrix result = this.CreateMatrix(this.Rows + lower.Rows, this.Columns + lower.Columns);
            this.DiagonalStack(lower, result);
            return result;
        }

        public virtual void DiagonalStack(Matrix lower, Matrix result)
        {
            if (lower == null)
            {
                throw new ArgumentNullException("lower");
            }
            if (result == null)
            {
                throw new ArgumentNullException("result");
            }
            if ((result.Rows != (this.Rows + lower.Rows)) || (result.Columns != (this.Columns + lower.Columns)))
            {
                throw new NotConformableException("result", Resources.ParameterNotConformable);
            }
            for (int i = 0; i < this.Columns; i++)
            {
                for (int k = 0; k < this.Rows; k++)
                {
                    result.ValueAt(k, i, this.ValueAt(k, i));
                }
            }
            for (int j = 0; j < lower.Columns; j++)
            {
                for (int m = 0; m < lower.Rows; m++)
                {
                    result.ValueAt(m + this.Rows, j + this.Columns, lower.ValueAt(m, j));
                }
            }
        }

        public virtual void Divide(double scalar)
        {
            this.Multiply((double)(1.0 / scalar));
        }

        public virtual void Divide(double scalar, Matrix result)
        {
            this.Multiply((double)(1.0 / scalar), result);
        }

        public bool Equals(Matrix other)
        {
            if (other == null)
            {
                return false;
            }
            if ((this.Columns != other.Columns) || (this.Rows != other.Rows))
            {
                return false;
            }
            if (!object.ReferenceEquals(this, other))
            {
                for (int i = 0; i < this.Rows; i++)
                {
                    for (int j = 0; j < this.Columns; j++)
                    {
                        if (this.ValueAt(i, j) != other.ValueAt(i, j))
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as Matrix);
        }

        public virtual double FrobeniusNorm()
        {
            Matrix matrix = this.CreateMatrix(this.mRows, this.mRows);
            matrix.Gemm(1.0, 0.0, false, true, this, this);
            double d = 0.0;
            for (int i = 0; i < this.mRows; i++)
            {
                d += Math.Abs(matrix.ValueAt(i, i));
            }
            return Math.Sqrt(d);
        }

        public virtual void Gemm(double alpha, double beta, bool transposeA, bool transposeB, Matrix a, Matrix b)
        {
            if (a == null)
            {
                throw new ArgumentNullException("a");
            }
            if (b == null)
            {
                throw new ArgumentNullException("b");
            }
            int rows = a.Rows;
            int num2 = b.Rows;
            int columns = a.Columns;
            int num4 = b.Columns;
            if (transposeA && transposeB)
            {
                if (rows != num4)
                {
                    throw new NotConformableException();
                }
                if ((this.mRows != columns) && (this.mColumns != num2))
                {
                    throw new NotConformableException();
                }
            }
            else if (transposeA)
            {
                if (rows != num2)
                {
                    throw new NotConformableException();
                }
                if ((this.mRows != columns) && (this.mColumns != num4))
                {
                    throw new NotConformableException();
                }
            }
            else if (transposeB)
            {
                if (columns != num4)
                {
                    throw new NotConformableException();
                }
                if ((this.mRows != rows) && (this.mColumns != num2))
                {
                    throw new NotConformableException();
                }
            }
            else
            {
                if (columns != num2)
                {
                    throw new NotConformableException();
                }
                if ((this.mRows != rows) && (this.mColumns != num4))
                {
                    throw new NotConformableException();
                }
            }
            if ((alpha == 0.0) && (beta == 0.0))
            {
                this.Clear();
            }
            else
            {
                Matrix matrix;
                if (object.ReferenceEquals(a, this) || object.ReferenceEquals(a, b))
                {
                    matrix = a.Clone();
                }
                else
                {
                    matrix = a;
                }
                Matrix matrix2 = object.ReferenceEquals(this, b) ? b.Clone() : b;
                if (alpha == 1.0)
                {
                    if (beta == 0.0)
                    {
                        if (transposeA && transposeB)
                        {
                            for (int i = 0; i != columns; i++)
                            {
                                for (int j = 0; j != num2; j++)
                                {
                                    double num7 = 0.0;
                                    for (int k = 0; k != num4; k++)
                                    {
                                        num7 += matrix.ValueAt(k, j) * matrix2.ValueAt(i, k);
                                    }
                                    this.ValueAt(j, i, num7);
                                }
                            }
                        }
                        else if (transposeA)
                        {
                            for (int m = 0; m != num4; m++)
                            {
                                for (int n = 0; n != columns; n++)
                                {
                                    double num11 = 0.0;
                                    for (int num12 = 0; num12 != rows; num12++)
                                    {
                                        num11 += matrix.ValueAt(num12, n) * matrix2.ValueAt(num12, m);
                                    }
                                    this.ValueAt(n, m, num11);
                                }
                            }
                        }
                        else if (transposeB)
                        {
                            for (int num13 = 0; num13 != num2; num13++)
                            {
                                for (int num14 = 0; num14 != rows; num14++)
                                {
                                    double num15 = 0.0;
                                    for (int num16 = 0; num16 != columns; num16++)
                                    {
                                        num15 += matrix.ValueAt(num14, num16) * matrix2.ValueAt(num13, num16);
                                    }
                                    this.ValueAt(num14, num13, num15);
                                }
                            }
                        }
                        else
                        {
                            for (int num17 = 0; num17 != num4; num17++)
                            {
                                for (int num18 = 0; num18 != rows; num18++)
                                {
                                    double num19 = 0.0;
                                    for (int num20 = 0; num20 != columns; num20++)
                                    {
                                        num19 += matrix.ValueAt(num18, num20) * matrix2.ValueAt(num20, num17);
                                    }
                                    this.ValueAt(num18, num17, num19);
                                }
                            }
                        }
                    }
                    else if (transposeA && transposeB)
                    {
                        for (int num21 = 0; num21 != columns; num21++)
                        {
                            for (int num22 = 0; num22 != num2; num22++)
                            {
                                double num23 = 0.0;
                                for (int num24 = 0; num24 != num4; num24++)
                                {
                                    num23 += matrix.ValueAt(num24, num22) * matrix2.ValueAt(num21, num24);
                                }
                                this.ValueAt(num22, num21, num23 + (this.ValueAt(num22, num21) * beta));
                            }
                        }
                    }
                    else if (transposeA)
                    {
                        for (int num25 = 0; num25 != num4; num25++)
                        {
                            for (int num26 = 0; num26 != columns; num26++)
                            {
                                double num27 = 0.0;
                                for (int num28 = 0; num28 != rows; num28++)
                                {
                                    num27 += matrix.ValueAt(num28, num26) * matrix2.ValueAt(num28, num25);
                                }
                                this.ValueAt(num26, num25, num27 + (this.ValueAt(num26, num25) * beta));
                            }
                        }
                    }
                    else if (transposeB)
                    {
                        for (int num29 = 0; num29 != num2; num29++)
                        {
                            for (int num30 = 0; num30 != rows; num30++)
                            {
                                double num31 = 0.0;
                                for (int num32 = 0; num32 != columns; num32++)
                                {
                                    num31 += matrix.ValueAt(num30, num32) * matrix2.ValueAt(num29, num32);
                                }
                                this.ValueAt(num30, num29, num31 + (this.ValueAt(num30, num29) * beta));
                            }
                        }
                    }
                    else
                    {
                        for (int num33 = 0; num33 != num4; num33++)
                        {
                            for (int num34 = 0; num34 != rows; num34++)
                            {
                                double num35 = 0.0;
                                for (int num36 = 0; num36 != columns; num36++)
                                {
                                    num35 += matrix.ValueAt(num34, num36) * matrix2.ValueAt(num36, num33);
                                }
                                this.ValueAt(num34, num33, num35 + (this.ValueAt(num34, num33) * beta));
                            }
                        }
                    }
                }
                else if (transposeA && transposeB)
                {
                    for (int num37 = 0; num37 != columns; num37++)
                    {
                        for (int num38 = 0; num38 != num2; num38++)
                        {
                            double num39 = 0.0;
                            for (int num40 = 0; num40 != num4; num40++)
                            {
                                num39 += matrix.ValueAt(num40, num38) * matrix2.ValueAt(num37, num40);
                            }
                            this.ValueAt(num38, num37, (alpha * num39) + (this.ValueAt(num38, num37) * beta));
                        }
                    }
                }
                else if (transposeA)
                {
                    for (int num41 = 0; num41 != num4; num41++)
                    {
                        for (int num42 = 0; num42 != columns; num42++)
                        {
                            double num43 = 0.0;
                            for (int num44 = 0; num44 != rows; num44++)
                            {
                                num43 += matrix.ValueAt(num44, num42) * matrix2.ValueAt(num44, num41);
                            }
                            this.ValueAt(num42, num41, (alpha * num43) + (this.ValueAt(num42, num41) * beta));
                        }
                    }
                }
                else if (transposeB)
                {
                    for (int num45 = 0; num45 != num2; num45++)
                    {
                        for (int num46 = 0; num46 != rows; num46++)
                        {
                            double num47 = 0.0;
                            for (int num48 = 0; num48 != columns; num48++)
                            {
                                num47 += matrix.ValueAt(num46, num48) * matrix2.ValueAt(num45, num48);
                            }
                            this.ValueAt(num46, num45, (alpha * num47) + (this.ValueAt(num46, num45) * beta));
                        }
                    }
                }
                else
                {
                    for (int num49 = 0; num49 != num4; num49++)
                    {
                        for (int num50 = 0; num50 != rows; num50++)
                        {
                            double num51 = 0.0;
                            for (int num52 = 0; num52 != columns; num52++)
                            {
                                num51 += matrix.ValueAt(num50, num52) * matrix2.ValueAt(num52, num49);
                            }
                            this.ValueAt(num50, num49, (alpha * num51) + (this.ValueAt(num50, num49) * beta));
                        }
                    }
                }
            }
        }

        public virtual Vector GetColumn(int index)
        {
            Vector result = this.CreateVector(this.Rows);
            this.GetColumn(index, 0, this.Rows, result);
            return result;
        }

        public virtual void GetColumn(int index, Vector result)
        {
            this.GetColumn(index, 0, this.Rows, result);
        }

        public virtual Vector GetColumn(int columnIndex, int rowIndex, int length)
        {
            Vector result = this.CreateVector(length);
            this.GetColumn(columnIndex, rowIndex, length, result);
            return result;
        }

        public virtual void GetColumn(int columnIndex, int rowIndex, int length, Vector result)
        {
            if (result == null)
            {
                throw new ArgumentNullException("result");
            }
            if ((columnIndex >= this.mColumns) || (columnIndex < 0))
            {
                throw new ArgumentOutOfRangeException("columnIndex");
            }
            if ((rowIndex >= this.mRows) || (rowIndex < 0))
            {
                throw new ArgumentOutOfRangeException("rowIndex");
            }
            if ((rowIndex + length) > this.mRows)
            {
                throw new ArgumentOutOfRangeException("length");
            }
            if (length < 1)
            {
                throw new ArgumentException(Resources.NotPositive, "length");
            }
            if (result.Count < length)
            {
                throw new NotConformableException("result", Resources.ParameterNotConformable);
            }
            int row = rowIndex;
            for (int i = 0; row < (rowIndex + length); i++)
            {
                result[i] = this.ValueAt(row, columnIndex);
                row++;
            }
        }

        public virtual IEnumerable<KeyValuePair<int, Vector>> GetColumnEnumerator()
        {
            ColumnEnumeratorKeyValuePair d__ = new ColumnEnumeratorKeyValuePair(-2);
            d__.__this = this;
            return d__;
        }

        public virtual IEnumerable<KeyValuePair<int, Vector>> GetColumnEnumerator(int index, int length)
        {
            ColumnEnumeratorKeyValuePairLength d__ = new ColumnEnumeratorKeyValuePairLength(-2);
            d__.__this = this;
            d__.__index = index;
            d__.__length = length;
            return d__;
        }

        public virtual Vector GetDiagonal()
        {
            int size = Math.Min(this.mRows, this.mColumns);
            Vector vector = this.CreateVector(size);
            for (int i = 0; i < size; i++)
            {
                vector[i] = this.ValueAt(i, i);
            }
            return vector;
        }

        public override int GetHashCode()
        {
            int num = Math.Min(this.Rows * this.Columns, 0x19);
            long num2 = 0L;
            for (int i = 0; i < num; i++)
            {
                int num4 = i % this.Columns;
                int num5 = (i - num4) / this.Rows;
                num2 ^= BitConverter.DoubleToInt64Bits(this[num5, num4]);
            }
            return BitConverter.ToInt32(BitConverter.GetBytes(num2), 4);
        }

        public virtual Matrix GetLowerTriangle()
        {
            Matrix matrix = this.CreateMatrix(this.mRows, this.mColumns);
            for (int i = 0; i < this.mColumns; i++)
            {
                for (int j = 0; j < this.mRows; j++)
                {
                    if (j >= i)
                    {
                        matrix.ValueAt(j, i, this.ValueAt(j, i));
                    }
                }
            }
            return matrix;
        }

        public virtual void GetLowerTriangle(Matrix result)
        {
            if (result == null)
            {
                throw new ArgumentNullException("result");
            }
            if ((result.Rows != this.Rows) || (result.Columns != this.Columns))
            {
                throw new NotConformableException("result", Resources.ParameterNotConformable);
            }
            for (int i = 0; i < this.mColumns; i++)
            {
                for (int j = 0; j < this.mRows; j++)
                {
                    if (j >= i)
                    {
                        result.ValueAt(j, i, this.ValueAt(j, i));
                    }
                    else
                    {
                        result.ValueAt(j, i, 0.0);
                    }
                }
            }
        }

        public virtual Vector GetRow(int index)
        {
            Vector result = this.CreateVector(this.mColumns);
            this.GetRow(index, 0, this.Columns, result);
            return result;
        }

        public virtual void GetRow(int index, Vector result)
        {
            this.GetRow(index, 0, this.Columns, result);
        }

        public virtual Vector GetRow(int rowIndex, int columnIndex, int length)
        {
            Vector result = this.CreateVector(length);
            this.GetRow(rowIndex, columnIndex, length, result);
            return result;
        }

        public virtual void GetRow(int rowIndex, int columnIndex, int length, Vector result)
        {
            if (result == null)
            {
                throw new ArgumentNullException("result");
            }
            if ((rowIndex >= this.mRows) || (rowIndex < 0))
            {
                throw new ArgumentOutOfRangeException("rowIndex");
            }
            if ((columnIndex >= this.mColumns) || (columnIndex < 0))
            {
                throw new ArgumentOutOfRangeException("columnIndex");
            }
            if ((columnIndex + length) > this.mColumns)
            {
                throw new ArgumentOutOfRangeException("length");
            }
            if (length < 1)
            {
                throw new ArgumentException(Resources.NotPositive, "length");
            }
            if (result.Count < length)
            {
                throw new NotConformableException("result", Resources.ParameterNotConformable);
            }
            int column = columnIndex;
            for (int i = 0; column < (columnIndex + length); i++)
            {
                result[i] = this.ValueAt(rowIndex, column);
                column++;
            }
        }

        public virtual IEnumerable<KeyValuePair<int, Vector>> GetRowEnumerator()
        {
            RowEnumeratorKeyValuePair _e = new RowEnumeratorKeyValuePair(-2);
            _e.__this = this;
            return _e;
        }

        public virtual IEnumerable<KeyValuePair<int, Vector>> GetRowEnumerator(int index, int length)
        {
            RowEnumeratorKeyValuePairLength d__ = new RowEnumeratorKeyValuePairLength(-2);
            d__.__this = this;
            d__.__index = index;
            d__.__length = length;
            return d__;
        }

        public virtual Matrix GetStrictlyLowerTriangle()
        {
            Matrix matrix = this.CreateMatrix(this.mRows, this.mColumns);
            for (int i = 0; i < this.mColumns; i++)
            {
                for (int j = 0; j < this.mRows; j++)
                {
                    if (j > i)
                    {
                        matrix.ValueAt(j, i, this.ValueAt(j, i));
                    }
                }
            }
            return matrix;
        }

        public virtual void GetStrictlyLowerTriangle(Matrix result)
        {
            if (result == null)
            {
                throw new ArgumentNullException("result");
            }
            if ((result.Rows != this.Rows) || (result.Columns != this.Columns))
            {
                throw new NotConformableException("result", Resources.ParameterNotConformable);
            }
            for (int i = 0; i < this.mColumns; i++)
            {
                for (int j = 0; j < this.mRows; j++)
                {
                    if (j > i)
                    {
                        result.ValueAt(j, i, this.ValueAt(j, i));
                    }
                    else
                    {
                        result.ValueAt(j, i, 0.0);
                    }
                }
            }
        }

        public virtual Matrix GetStrictlyUpperTriangle()
        {
            Matrix matrix = this.CreateMatrix(this.mRows, this.mColumns);
            for (int i = 0; i < this.mColumns; i++)
            {
                for (int j = 0; j < this.mRows; j++)
                {
                    if (j < i)
                    {
                        matrix.ValueAt(j, i, this.ValueAt(j, i));
                    }
                }
            }
            return matrix;
        }

        public virtual void GetStrictlyUpperTriangle(Matrix result)
        {
            if (result == null)
            {
                throw new ArgumentNullException("result");
            }
            if ((result.Rows != this.Rows) || (result.Columns != this.Columns))
            {
                throw new NotConformableException("result", Resources.ParameterNotConformable);
            }
            for (int i = 0; i < this.mColumns; i++)
            {
                for (int j = 0; j < this.mRows; j++)
                {
                    if (j < i)
                    {
                        result.ValueAt(j, i, this.ValueAt(j, i));
                    }
                    else
                    {
                        result.ValueAt(j, i, 0.0);
                    }
                }
            }
        }

        public virtual Matrix GetSubMatrix(int rowIndex, int rowLength, int columnIndex, int columnLength)
        {
            if ((rowIndex >= this.mRows) || (rowIndex < 0))
            {
                throw new ArgumentOutOfRangeException("rowIndex");
            }
            if ((columnIndex >= this.mColumns) || (columnIndex < 0))
            {
                throw new ArgumentOutOfRangeException("columnIndex");
            }
            if (rowLength < 1)
            {
                throw new ArgumentException(Resources.NotPositive, "rowLength");
            }
            if (columnLength < 1)
            {
                throw new ArgumentException(Resources.NotPositive, "columnLength");
            }
            int num = columnIndex + columnLength;
            int num2 = rowIndex + rowLength;
            if (num2 > this.mRows)
            {
                throw new ArgumentOutOfRangeException("rowLength");
            }
            if (num > this.mColumns)
            {
                throw new ArgumentOutOfRangeException("columnLength");
            }
            Matrix matrix = this.CreateMatrix(rowLength, columnLength);
            int column = columnIndex;
            for (int i = 0; column < num; i++)
            {
                int row = rowIndex;
                for (int j = 0; row < num2; j++)
                {
                    matrix.ValueAt(j, i, this.ValueAt(row, column));
                    row++;
                }
                column++;
            }
            return matrix;
        }

        public virtual Matrix GetUpperTriangle()
        {
            Matrix matrix = this.CreateMatrix(this.mRows, this.mColumns);
            for (int i = 0; i < this.mColumns; i++)
            {
                for (int j = 0; j < this.mRows; j++)
                {
                    if (j <= i)
                    {
                        matrix.ValueAt(j, i, this.ValueAt(j, i));
                    }
                }
            }
            return matrix;
        }

        public virtual void GetUpperTriangle(Matrix result)
        {
            if (result == null)
            {
                throw new ArgumentNullException("result");
            }
            if ((result.Rows != this.Rows) || (result.Columns != this.Columns))
            {
                throw new NotConformableException("result", Resources.ParameterNotConformable);
            }
            for (int i = 0; i < this.mColumns; i++)
            {
                for (int j = 0; j < this.mRows; j++)
                {
                    if (j <= i)
                    {
                        result.ValueAt(j, i, this.ValueAt(j, i));
                    }
                    else
                    {
                        result.ValueAt(j, i, 0.0);
                    }
                }
            }
        }

        public virtual double InfinityNorm()
        {
            double num = 0.0;
            for (int i = 0; i < this.mRows; i++)
            {
                double num3 = 0.0;
                for (int j = 0; j < this.mColumns; j++)
                {
                    num3 += Math.Abs(this.ValueAt(i, j));
                }
                num = Math.Max(num, num3);
            }
            return num;
        }

        public virtual Matrix InsertColumn(int columnIndex, Vector column)
        {
            if (column == null)
            {
                throw new ArgumentNullException("column");
            }
            if ((columnIndex < 0) || (columnIndex > this.mColumns))
            {
                throw new ArgumentOutOfRangeException("columnIndex");
            }
            if (column.Count != this.mRows)
            {
                throw new NotConformableException("row", Resources.ParameterNotConformable);
            }
            Matrix matrix = this.CreateMatrix(this.mRows, this.mColumns + 1);
            for (int i = 0; i < columnIndex; i++)
            {
                matrix.SetColumn(i, this.GetColumn(i));
            }
            matrix.SetColumn(columnIndex, column);
            for (int j = columnIndex + 1; j < (this.mColumns + 1); j++)
            {
                matrix.SetColumn(j, this.GetColumn(j - 1));
            }
            return matrix;
        }

        public virtual Matrix InsertRow(int rowIndex, Vector row)
        {
            if (row == null)
            {
                throw new ArgumentNullException("row");
            }
            if ((rowIndex < 0) || (rowIndex > this.mRows))
            {
                throw new ArgumentOutOfRangeException("rowIndex");
            }
            if (row.Count != this.mColumns)
            {
                throw new NotConformableException("row", Resources.ParameterNotConformable);
            }
            Matrix matrix = this.CreateMatrix(this.mRows + 1, this.mColumns);
            for (int i = 0; i < rowIndex; i++)
            {
                matrix.SetRow(i, this.GetRow(i));
            }
            matrix.SetRow(rowIndex, row);
            for (int j = rowIndex + 1; j < (this.mRows + 1); j++)
            {
                matrix.SetRow(j, this.GetRow(j - 1));
            }
            return matrix;
        }

        public virtual Matrix Inverse()
        {
            if (this.mRows != this.mColumns)
            {
                throw new MatrixNotSquareException();
            }
            return new LU(this).Inverse();
        }

        public virtual Matrix Solve(Matrix input)
        {
            return new LU(this).Solve(input);
        }

        public virtual Vector Solve(Vector input)
        {
            return new LU(this).Solve(input);
        }

        public virtual Matrix KroneckerProduct(Matrix other)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }
            Matrix result = this.CreateMatrix(this.Rows * other.Rows, this.Columns * other.Columns);
            this.KroneckerProduct(other, result);
            return result;
        }

        public virtual void KroneckerProduct(Matrix other, Matrix result)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }
            if (result == null)
            {
                throw new ArgumentNullException("result");
            }
            if ((result.Rows != (this.Rows * other.Rows)) || (result.Columns != (this.Columns * other.Columns)))
            {
                throw new NotConformableException("result", Resources.ParameterNotConformable);
            }
            for (int i = 0; i < this.Columns; i++)
            {
                for (int j = 0; j < this.Rows; j++)
                {
                    result.SetSubMatrix(j * other.Rows, other.Rows, i * other.Columns, other.Columns, (Matrix)(this.ValueAt(j, i) * other));
                }
            }
        }

        public virtual double L1Norm()
        {
            double num = 0.0;
            for (int i = 0; i < this.mColumns; i++)
            {
                double num3 = 0.0;
                for (int j = 0; j < this.mRows; j++)
                {
                    num3 += Math.Abs(this.ValueAt(j, i));
                }
                num = Math.Max(num, num3);
            }
            return num;
        }

        public virtual double L2Norm()
        {
            Svd svd = new Svd(this, false);
            return svd.Norm2();
        }

        public virtual Vector LeftMultiply(Vector leftSide)
        {
            Vector result = this.CreateVector(this.Columns);
            this.LeftMultiply(leftSide, result);
            return result;
        }

        public virtual void LeftMultiply(Vector leftSide, Vector result)
        {
            if (leftSide == null)
            {
                throw new ArgumentNullException("leftSide");
            }
            if (this.Rows != leftSide.Count)
            {
                throw new NotConformableException("leftSide", Resources.ParameterNotConformable);
            }
            if (result == null)
            {
                throw new ArgumentNullException("result");
            }
            if (this.Columns != result.Count)
            {
                throw new NotConformableException("result", Resources.ParameterNotConformable);
            }
            if (object.ReferenceEquals(leftSide, result))
            {
                Vector vector = result.CreateVector(result.Count);
                this.LeftMultiply(leftSide, vector);
                vector.CopyTo(result);
            }
            else
            {
                for (int i = 0; i != this.Columns; i++)
                {
                    double num2 = 0.0;
                    for (int j = 0; j != leftSide.Count; j++)
                    {
                        num2 += leftSide[j] * this.ValueAt(j, i);
                    }
                    result[i] = num2;
                }
            }
        }

        public virtual Matrix Multiply(Matrix other)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }
            if (this.Columns != other.Rows)
            {
                throw new NotConformableException("other", Resources.ParameterNotConformable);
            }
            Matrix result = this.CreateMatrix(this.Rows, other.Columns);
            this.Multiply(other, result);
            return result;
        }

        public virtual Vector Multiply(Vector rightSide)
        {
            Vector result = this.CreateVector(this.Rows);
            this.Multiply(rightSide, result);
            return result;
        }

        public virtual void Multiply(double scalar)
        {
            if (scalar != 1.0)
            {
                for (int i = 0; i < this.Rows; i++)
                {
                    for (int j = 0; j < this.Columns; j++)
                    {
                        this.ValueAt(i, j, this.ValueAt(i, j) * scalar);
                    }
                }
            }
        }

        public virtual void Multiply(Matrix other, Matrix result)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }
            if (result == null)
            {
                throw new ArgumentNullException("result");
            }
            if (this.Columns != other.Rows)
            {
                throw new NotConformableException("other", Resources.ParameterNotConformable);
            }
            if ((result.Rows != this.Rows) || (result.Columns != other.Columns))
            {
                throw new NotConformableException("result", Resources.ParameterNotConformable);
            }
            if (object.ReferenceEquals(this, result) || object.ReferenceEquals(other, result))
            {
                Matrix matrix = result.CreateMatrix(result.Rows, result.Columns);
                this.Multiply(other, matrix);
                matrix.CopyTo(result);
            }
            else
            {
                result.Gemm(1.0, 0.0, false, false, this, other);
            }
        }

        public virtual void Multiply(Vector rightSide, Vector result)
        {
            if (rightSide == null)
            {
                throw new ArgumentNullException("rightSide");
            }
            if (this.Columns != rightSide.Count)
            {
                throw new NotConformableException("rightSide", Resources.ParameterNotConformable);
            }
            if (result == null)
            {
                throw new ArgumentNullException("result");
            }
            if (this.Rows != result.Count)
            {
                throw new NotConformableException("result", Resources.ParameterNotConformable);
            }
            if (object.ReferenceEquals(rightSide, result))
            {
                Vector vector = result.CreateVector(result.Count);
                this.Multiply(rightSide, vector);
                vector.CopyTo(result);
            }
            else
            {
                for (int i = 0; i != this.Rows; i++)
                {
                    double num2 = 0.0;
                    for (int j = 0; j != this.Columns; j++)
                    {
                        num2 += this.ValueAt(i, j) * rightSide[j];
                    }
                    result[i] = num2;
                }
            }
        }

        public virtual void Multiply(double scalar, Matrix result)
        {
            if (result == null)
            {
                throw new ArgumentNullException("result");
            }
            if ((result.Rows != this.Rows) || (result.Columns != this.Columns))
            {
                throw new NotConformableException("result", Resources.ParameterNotConformable);
            }
            this.CopyTo(result);
            result.Multiply(scalar);
        }

        public virtual void Negate()
        {
            this.Multiply((double)-1.0);
        }

        public virtual void Negate(Matrix result)
        {
            if (result == null)
            {
                throw new ArgumentNullException("result");
            }
            if ((result.Rows != this.Rows) || (result.Columns != this.Columns))
            {
                throw new NotConformableException("result", Resources.ParameterNotConformable);
            }
            this.CopyTo(result);
            result.Negate();
        }

        public virtual Matrix NormalizeColumns(int pValue)
        {
            if (pValue < 1)
            {
                throw new ArgumentOutOfRangeException("pValue", Resources.NotPositive);
            }
            Matrix matrix = this.Clone();
            for (int i = 0; i < this.Columns; i++)
            {
                Vector column = this.GetColumn(i);
                double num2 = column.PNorm(pValue);
                for (int j = 0; j < this.Rows; j++)
                {
                    matrix[j, i] = column[j] / num2;
                }
            }
            return matrix;
        }

        public virtual Matrix NormalizeRows(int pValue)
        {
            if (pValue < 1)
            {
                throw new ArgumentOutOfRangeException("pValue", Resources.NotPositive);
            }
            Matrix matrix = this.Clone();
            for (int i = 0; i < this.Rows; i++)
            {
                Vector row = this.GetRow(i);
                double num2 = row.PNorm(pValue);
                for (int j = 0; j < this.Columns; j++)
                {
                    matrix[i, j] = row[j] / num2;
                }
            }
            return matrix;
        }

        public static Matrix operator +(Matrix leftSide, Matrix rightSide)
        {
            if (rightSide == null)
            {
                throw new ArgumentNullException("rightSide");
            }
            if (leftSide == null)
            {
                throw new ArgumentNullException("leftSide");
            }
            if ((leftSide.Rows != rightSide.Rows) || (leftSide.Columns != rightSide.Columns))
            {
                throw new NotConformableException(Resources.ParametersNotConformable);
            }
            Matrix matrix = leftSide.Clone();
            matrix.Add(rightSide);
            return matrix;
        }

        public static Matrix operator /(Matrix leftSide, double rightSide)
        {
            if (leftSide == null)
            {
                throw new ArgumentNullException("leftSide");
            }
            Matrix matrix = leftSide.Clone();
            matrix.Divide(rightSide);
            return matrix;
        }

        public static Matrix operator *(Matrix leftSide, Matrix rightSide)
        {
            if (leftSide == null)
            {
                throw new ArgumentNullException("leftSide");
            }
            if (rightSide == null)
            {
                throw new ArgumentNullException("rightSide");
            }
            if (leftSide.Columns != rightSide.Rows)
            {
                throw new NotConformableException(Resources.ParametersNotConformable);
            }
            return leftSide.Multiply(rightSide);
        }

        public static Vector operator *(Matrix leftSide, Vector rightSide)
        {
            if (leftSide == null)
            {
                throw new ArgumentNullException("leftSide");
            }
            if (rightSide == null)
            {
                throw new ArgumentNullException("rightSide");
            }
            if (leftSide.Columns != rightSide.Count)
            {
                throw new NotConformableException(Resources.ParametersNotConformable);
            }
            return leftSide.Multiply(rightSide);
        }

        public static Matrix operator *(Matrix leftSide, double rightSide)
        {
            if (leftSide == null)
            {
                throw new ArgumentNullException("leftSide");
            }
            Matrix matrix = leftSide.Clone();
            matrix.Multiply(rightSide);
            return matrix;
        }

        public static Vector operator *(Vector leftSide, Matrix rightSide)
        {
            if (leftSide == null)
            {
                throw new ArgumentNullException("leftSide");
            }
            if (rightSide == null)
            {
                throw new ArgumentNullException("rightSide");
            }
            if (leftSide.Count != rightSide.Rows)
            {
                throw new NotConformableException(Resources.ParametersNotConformable);
            }
            return rightSide.LeftMultiply(leftSide);
        }

        public static Matrix operator *(double leftSide, Matrix rightSide)
        {
            if (rightSide == null)
            {
                throw new ArgumentNullException("rightSide");
            }
            Matrix matrix = rightSide.Clone();
            matrix.Multiply(leftSide);
            return matrix;
        }

        public static Matrix operator -(Matrix leftSide, Matrix rightSide)
        {
            if (rightSide == null)
            {
                throw new ArgumentNullException("rightSide");
            }
            if (leftSide == null)
            {
                throw new ArgumentNullException("leftSide");
            }
            if ((leftSide.Rows != rightSide.Rows) || (leftSide.Columns != rightSide.Columns))
            {
                throw new NotConformableException(Resources.ParametersNotConformable);
            }
            Matrix matrix = leftSide.Clone();
            matrix.Subtract(rightSide);
            return matrix;
        }

        public static Matrix operator -(Matrix rightSide)
        {
            if (rightSide == null)
            {
                throw new ArgumentNullException("rightSide");
            }
            Matrix matrix = rightSide.Clone();
            matrix.Negate();
            return matrix;
        }

        public static Matrix operator +(Matrix rightSide)
        {
            if (rightSide == null)
            {
                throw new ArgumentNullException("rightSide");
            }
            return rightSide.Clone();
        }

        public virtual Matrix Plus()
        {
            return this.Clone();
        }

        public virtual Matrix PointwiseMultiply(Matrix other)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }
            if ((this.Columns != other.Columns) || (this.Rows != other.Rows))
            {
                throw new NotConformableException("other", Resources.ParameterNotConformable);
            }
            Matrix result = this.CreateMatrix(this.Rows, this.Columns);
            this.PointwiseMultiply(other, result);
            return result;
        }

        public virtual void PointwiseMultiply(Matrix other, Matrix result)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }
            if (result == null)
            {
                throw new ArgumentNullException("result");
            }
            if ((this.Columns != other.Columns) || (this.Rows != other.Rows))
            {
                throw new NotConformableException("result");
            }
            if ((this.Columns != result.Columns) || (this.Rows != result.Rows))
            {
                throw new NotConformableException("result");
            }
            for (int i = 0; i < this.Rows; i++)
            {
                for (int j = 0; j < this.Columns; j++)
                {
                    result.ValueAt(i, j, this.ValueAt(i, j) * other.ValueAt(i, j));
                }
            }
        }

        private void RangeCheck(int row, int column)
        {
            if ((row < 0) || (row >= this.mRows))
            {
                throw new ArgumentOutOfRangeException("row");
            }
            if ((column < 0) || (column >= this.mColumns))
            {
                throw new ArgumentOutOfRangeException("column");
            }
        }

        public virtual void SetColumn(int index, double[] source)
        {
            if ((index < 0) || (index >= this.mColumns))
            {
                throw new ArgumentOutOfRangeException("index");
            }
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            if (source.Length != this.mRows)
            {
                throw new NotConformableException("source", Resources.ParameterNotConformable);
            }
            for (int i = 0; i < this.mRows; i++)
            {
                this.ValueAt(i, index, source[i]);
            }
        }

        public virtual void SetColumn(int index, Vector source)
        {
            if ((index < 0) || (index >= this.mColumns))
            {
                throw new ArgumentOutOfRangeException("index");
            }
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            if (source.Count != this.mRows)
            {
                throw new NotConformableException("source", Resources.ParameterNotConformable);
            }
            for (int i = 0; i < this.mRows; i++)
            {
                this.ValueAt(i, index, source[i]);
            }
        }

        public virtual void SetDiagonal(double[] source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            int num = Math.Min(this.Rows, this.Columns);
            if (source.Length != num)
            {
                throw new NotConformableException("source", Resources.ParameterNotConformable);
            }
            for (int i = 0; i < num; i++)
            {
                this.ValueAt(i, i, source[i]);
            }
        }

        public virtual void SetDiagonal(Vector source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            int num = Math.Min(this.Rows, this.Columns);
            if (source.Count != num)
            {
                throw new NotConformableException("source", Resources.ParameterNotConformable);
            }
            for (int i = 0; i < num; i++)
            {
                this.ValueAt(i, i, source[i]);
            }
        }

        public virtual void SetRow(int index, Vector source)
        {
            if ((index < 0) || (index >= this.mRows))
            {
                throw new ArgumentOutOfRangeException("index");
            }
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            if (source.Count != this.mColumns)
            {
                throw new NotConformableException("source", Resources.ParameterNotConformable);
            }
            for (int i = 0; i < this.mColumns; i++)
            {
                this.ValueAt(index, i, source[i]);
            }
        }

        public virtual void SetRow(int index, double[] source)
        {
            if ((index < 0) || (index >= this.mRows))
            {
                throw new ArgumentOutOfRangeException("index");
            }
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            if (source.Length != this.mColumns)
            {
                throw new NotConformableException("source", Resources.ParameterNotConformable);
            }
            for (int i = 0; i < this.mColumns; i++)
            {
                this.ValueAt(index, i, source[i]);
            }
        }

        public virtual void SetSubMatrix(int rowIndex, int rowLength, int columnIndex, int columnLength, Matrix subMatrix)
        {
            if ((rowIndex >= this.mRows) || (rowIndex < 0))
            {
                throw new ArgumentOutOfRangeException("rowIndex");
            }
            if ((columnIndex >= this.mColumns) || (columnIndex < 0))
            {
                throw new ArgumentOutOfRangeException("columnIndex");
            }
            if (rowLength < 1)
            {
                throw new ArgumentException(Resources.NotPositive, "rowLength");
            }
            if (columnLength < 1)
            {
                throw new ArgumentException(Resources.NotPositive, "columnLength");
            }
            if (columnLength > subMatrix.Columns)
            {
                throw new ArgumentOutOfRangeException("columnLength", "columnLength can be at most the number of columns in subMatrix.");
            }
            if (rowLength > subMatrix.Rows)
            {
                throw new ArgumentOutOfRangeException("rowLength", "rowLength can be at most the number of rows in subMatrix.");
            }
            int num = columnIndex + columnLength;
            int num2 = rowIndex + rowLength;
            if (num2 > this.mRows)
            {
                throw new ArgumentOutOfRangeException("rowLength");
            }
            if (num > this.mColumns)
            {
                throw new ArgumentOutOfRangeException("columnLength");
            }
            int column = columnIndex;
            for (int i = 0; column < num; i++)
            {
                int row = rowIndex;
                for (int j = 0; row < num2; j++)
                {
                    this.ValueAt(row, column, subMatrix[j, i]);
                    row++;
                }
                column++;
            }
        }

        public virtual Matrix Stack(Matrix lower)
        {
            if (lower == null)
            {
                throw new ArgumentNullException("lower");
            }
            if (lower.Columns != this.Columns)
            {
                throw new NotConformableException("lower", Resources.ParameterNotConformable);
            }
            Matrix result = this.CreateMatrix(this.Rows + lower.Rows, this.Columns);
            this.Stack(lower, result);
            return result;
        }

        public virtual void Stack(Matrix lower, Matrix result)
        {
            if (lower == null)
            {
                throw new ArgumentNullException("lower");
            }
            if (lower.Columns != this.Columns)
            {
                throw new NotConformableException("lower", Resources.ParameterNotConformable);
            }
            if (result == null)
            {
                throw new ArgumentNullException("result");
            }
            if ((result.Rows != (this.Rows + lower.Rows)) || (result.Columns != this.Columns))
            {
                throw new NotConformableException("result", Resources.ParameterNotConformable);
            }
            for (int i = 0; i < this.Columns; i++)
            {
                for (int k = 0; k < this.Rows; k++)
                {
                    result.ValueAt(k, i, this.ValueAt(k, i));
                }
            }
            for (int j = 0; j < lower.Columns; j++)
            {
                for (int m = 0; m < lower.Rows; m++)
                {
                    result.ValueAt(m + this.Rows, j, lower.ValueAt(m, j));
                }
            }
        }

        public virtual void Subtract(Matrix other)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }
            if ((other.Rows != this.Rows) || (other.Columns != this.Columns))
            {
                throw new NotConformableException("other", Resources.ParameterNotConformable);
            }
            for (int i = 0; i < this.Rows; i++)
            {
                for (int j = 0; j < this.Columns; j++)
                {
                    double num3 = this.ValueAt(i, j) - other.ValueAt(i, j);
                    this.ValueAt(i, j, num3);
                }
            }
        }

        public virtual void Subtract(double scalar)
        {
            this.Add(-scalar);
        }

        public virtual void Subtract(Matrix other, Matrix result)
        {
            if (result == null)
            {
                throw new ArgumentNullException("result");
            }
            if ((result.Rows != this.Rows) || (result.Columns != this.Columns))
            {
                throw new NotConformableException("result", Resources.ParameterNotConformable);
            }
            if (object.ReferenceEquals(this, result) || object.ReferenceEquals(other, result))
            {
                Matrix matrix = result.CreateMatrix(result.Rows, result.Columns);
                this.Subtract(other, matrix);
                matrix.CopyTo(result);
            }
            else
            {
                this.CopyTo(result);
                result.Subtract(other);
            }
        }

        public virtual void Subtract(double scalar, Matrix result)
        {
            if (result == null)
            {
                throw new ArgumentNullException("result");
            }
            if ((result.Rows != this.Rows) || (result.Columns != this.Columns))
            {
                throw new NotConformableException("result", Resources.ParameterNotConformable);
            }
            this.CopyTo(result);
            result.Subtract(scalar);
        }

        object ICloneable.Clone()
        {
            return this.Clone();
        }

        public virtual double[,] ToArray()
        {
            double[,] numArray = new double[this.mRows, this.mColumns];
            for (int i = 0; i < this.mColumns; i++)
            {
                for (int j = 0; j < this.mRows; j++)
                {
                    numArray[j, i] = this.ValueAt(j, i);
                }
            }
            return numArray;
        }

        public virtual double[] ToColumnWiseArray()
        {
            double[] numArray = new double[this.Rows * this.Columns];
            foreach (KeyValuePair<int, Vector> pair in this.GetColumnEnumerator())
            {
                int num = pair.Key * this.Rows;
                foreach (KeyValuePair<int, double> pair2 in pair.Value.GetIndexedEnumerator())
                {
                    numArray[num + pair2.Key] = pair2.Value;
                }
            }
            return numArray;
        }

        public virtual double[] ToRowWiseArray()
        {
            double[] numArray = new double[this.Rows * this.Columns];
            foreach (KeyValuePair<int, Vector> pair in this.GetRowEnumerator())
            {
                int num = pair.Key * this.Columns;
                foreach (KeyValuePair<int, double> pair2 in pair.Value.GetIndexedEnumerator())
                {
                    numArray[num + pair2.Key] = pair2.Value;
                }
            }
            return numArray;
        }

        public override string ToString()
        {
            return this.ToString(null, null);
        }

        public virtual string ToString(IFormatProvider formatProvider)
        {
            return this.ToString(null, formatProvider);
        }

        public virtual string ToString(string format)
        {
            return this.ToString(format, null);
        }

        public virtual string ToString(string format, IFormatProvider formatProvider)
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < this.Rows; i++)
            {
                for (int j = 0; j < this.Columns; j++)
                {
                    builder.Append(this.ValueAt(i, j).ToString(format, formatProvider));
                    if (j != (this.Columns - 1))
                    {
                        builder.Append(", ");
                    }
                }
                if (i != (this.Rows - 1))
                {
                    builder.Append(Environment.NewLine);
                }
            }
            return builder.ToString();
        }

        public virtual double Trace()
        {
            if (this.Rows != this.Columns)
            {
                throw new MatrixNotSquareException();
            }
            double num = 0.0;
            for (int i = 0; i < this.Rows; i++)
            {
                num += this[i, i];
            }
            return num;
        }

        public virtual Matrix Transpose()
        {
            Matrix matrix = this.CreateMatrix(this.mColumns, this.mRows);
            for (int i = 0; i < this.mColumns; i++)
            {
                for (int j = 0; j < this.mRows; j++)
                {
                    matrix.ValueAt(i, j, this.ValueAt(j, i));
                }
            }
            return matrix;
        }

        protected internal abstract double ValueAt(int row, int column);
        protected internal abstract void ValueAt(int row, int column, double value);

        // Properties
        public int Columns
        {
            get
            {
                return this.mColumns;
            }
        }

        public virtual double this[int row, int column]
        {
            get
            {
                this.RangeCheck(row, column);
                return this.ValueAt(row, column);
            }
            set
            {
                this.RangeCheck(row, column);
                this.ValueAt(row, column, value);
            }
        }

        public int Rows
        {
            get
            {
                return this.mRows;
            }
        }

        // Nested Types
        [CompilerGenerated]
        private sealed class ColumnEnumeratorKeyValuePairLength : IEnumerable<KeyValuePair<int, Vector>>, IEnumerable, IEnumerator<KeyValuePair<int, Vector>>, IEnumerator, IDisposable
        {
            // Fields
            private int __state;
            private KeyValuePair<int, Vector> __current;
            public int __index;
            public int __length;
            public Matrix __this;
            private int __initialThreadId;
            public int __2;
            public int __1;
            public int index;
            public int length;

            // Methods
            [DebuggerHidden]
            public ColumnEnumeratorKeyValuePairLength(int __state)
            {
                this.__state = __state;
                this.__initialThreadId = Thread.CurrentThread.ManagedThreadId;
            }

            public bool MoveNext()
            {
                switch (this.__state)
                {
                    case 0:
                        this.__state = -1;
                        if ((this.index >= this.__this.mColumns) || (this.index < 0))
                        {
                            throw new ArgumentOutOfRangeException("index");
                        }
                        if ((this.index + this.length) > this.__this.mColumns)
                        {
                            throw new ArgumentOutOfRangeException("length");
                        }
                        if (this.length < 1)
                        {
                            throw new ArgumentException(Resources.NotPositive, "length");
                        }
                        this.__1 = this.index + this.length;
                        this.__2 = this.index;
                        while (this.__2 < this.__1)
                        {
                            this.__current = new KeyValuePair<int, Vector>(this.__2, this.__this.GetColumn(this.__2));
                            this.__state = 1;
                            return true;
                        }
                        break;

                    case 1:
                        this.__state = -1;
                        this.__2++;
                        while (this.__2 < this.__1)
                        {
                            this.__current = new KeyValuePair<int, Vector>(this.__2, this.__this.GetColumn(this.__2));
                            this.__state = 1;
                            return true;
                        }
                        break;
                }
                return false;
            }

            [DebuggerHidden]
            IEnumerator<KeyValuePair<int, Vector>> IEnumerable<KeyValuePair<int, Vector>>.GetEnumerator()
            {
                Matrix.ColumnEnumeratorKeyValuePairLength d__;
                if ((Thread.CurrentThread.ManagedThreadId == this.__initialThreadId) && (this.__state == -2))
                {
                    this.__state = 0;
                    d__ = this;
                }
                else
                {
                    d__ = new Matrix.ColumnEnumeratorKeyValuePairLength(0);
                    d__.__this = this.__this;
                }
                d__.index = this.__index;
                d__.length = this.__length;
                return d__;
            }

            [DebuggerHidden]
            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable<KeyValuePair<Int32, Vector>>)this).GetEnumerator();
            }

            [DebuggerHidden]
            void IEnumerator.Reset()
            {
                throw new NotSupportedException();
            }

            void IDisposable.Dispose()
            {
            }

            // Properties
            KeyValuePair<int, Vector> IEnumerator<KeyValuePair<int, Vector>>.Current
            {
                [DebuggerHidden]
                get
                {
                    return this.__current;
                }
            }

            object IEnumerator.Current
            {
                [DebuggerHidden]
                get
                {
                    return this.__current;
                }
            }
        }

        [CompilerGenerated]
        private sealed class ColumnEnumeratorKeyValuePair : IEnumerable<KeyValuePair<int, Vector>>, IEnumerable, IEnumerator<KeyValuePair<int, Vector>>, IEnumerator, IDisposable
        {
            // Fields
            private int __state;
            private KeyValuePair<int, Vector> __current;
            public Matrix __this;
            private int __initialThreadId;
            public int __6;

            // Methods
            [DebuggerHidden]
            public ColumnEnumeratorKeyValuePair(int __state)
            {
                this.__state = __state;
                this.__initialThreadId = Thread.CurrentThread.ManagedThreadId;
            }

            public bool MoveNext()
            {
                switch (this.__state)
                {
                    case 0:
                        this.__state = -1;
                        this.__6 = 0;
                        break;

                    case 1:
                        this.__state = -1;
                        this.__6++;
                        break;

                    default:
                        goto Label_0080;
                }
                if (this.__6 < this.__this.mColumns)
                {
                    this.__current = new KeyValuePair<int, Vector>(this.__6, this.__this.GetColumn(this.__6));
                    this.__state = 1;
                    return true;
                }
                Label_0080:
                return false;
            }

            [DebuggerHidden]
            IEnumerator<KeyValuePair<int, Vector>> IEnumerable<KeyValuePair<int, Vector>>.GetEnumerator()
            {
                if ((Thread.CurrentThread.ManagedThreadId == this.__initialThreadId) && (this.__state == -2))
                {
                    this.__state = 0;
                    return this;
                }
                Matrix.ColumnEnumeratorKeyValuePair d__ = new Matrix.ColumnEnumeratorKeyValuePair(0);
                d__.__this = this.__this;
                return d__;
            }

            [DebuggerHidden]
            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable<KeyValuePair<Int32, Vector>>)this).GetEnumerator();
            }

            [DebuggerHidden]
            void IEnumerator.Reset()
            {
                throw new NotSupportedException();
            }

            void IDisposable.Dispose()
            {
            }

            // Properties
            KeyValuePair<int, Vector> IEnumerator<KeyValuePair<int, Vector>>.Current
            {
                [DebuggerHidden]
                get
                {
                    return this.__current;
                }
            }

            object IEnumerator.Current
            {
                [DebuggerHidden]
                get
                {
                    return this.__current;
                }
            }
        }

        [CompilerGenerated]
        private sealed class RowEnumeratorKeyValuePairLength : IEnumerable<KeyValuePair<int, Vector>>, IEnumerable, IEnumerator<KeyValuePair<int, Vector>>, IEnumerator, IDisposable
        {
            // Fields
            private int __state;
            private KeyValuePair<int, Vector> __current;
            public int __index;
            public int __length;
            public Matrix __this;
            private int __initialThreadId;
            public int __b;
            public int __a;
            public int index;
            public int length;

            // Methods
            [DebuggerHidden]
            public RowEnumeratorKeyValuePairLength(int __state)
            {
                this.__state = __state;
                this.__initialThreadId = Thread.CurrentThread.ManagedThreadId;
            }

            public bool MoveNext()
            {
                switch (this.__state)
                {
                    case 0:
                        this.__state = -1;
                        if ((this.index >= this.__this.mRows) || (this.index < 0))
                        {
                            throw new ArgumentOutOfRangeException("index");
                        }
                        if ((this.index + this.length) > this.__this.mRows)
                        {
                            throw new ArgumentOutOfRangeException("length");
                        }
                        if (this.length < 1)
                        {
                            throw new ArgumentException(Resources.NotPositive, "length");
                        }
                        this.__a = this.index + this.length;
                        this.__b = this.index;
                        while (this.__b < this.__a)
                        {
                            this.__current = new KeyValuePair<int, Vector>(this.__b, this.__this.GetRow(this.__b));
                            this.__state = 1;
                            return true;
                        }
                        break;

                    case 1:
                        this.__state = -1;
                        this.__b++;
                        while (this.__b < this.__a)
                        {
                            this.__current = new KeyValuePair<int, Vector>(this.__b, this.__this.GetRow(this.__b));
                            this.__state = 1;
                            return true;
                        }
                        break;
                }
                return false;
            }

            [DebuggerHidden]
            IEnumerator<KeyValuePair<int, Vector>> IEnumerable<KeyValuePair<int, Vector>>.GetEnumerator()
            {
                Matrix.RowEnumeratorKeyValuePairLength d__;
                if ((Thread.CurrentThread.ManagedThreadId == this.__initialThreadId) && (this.__state == -2))
                {
                    this.__state = 0;
                    d__ = this;
                }
                else
                {
                    d__ = new Matrix.RowEnumeratorKeyValuePairLength(0);
                    d__.__this = this.__this;
                }
                d__.index = this.__index;
                d__.length = this.__length;
                return d__;
            }

            [DebuggerHidden]
            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable<KeyValuePair<Int32, Vector>>)this).GetEnumerator();
            }

            [DebuggerHidden]
            void IEnumerator.Reset()
            {
                throw new NotSupportedException();
            }

            void IDisposable.Dispose()
            {
            }

            // Properties
            KeyValuePair<int, Vector> IEnumerator<KeyValuePair<int, Vector>>.Current
            {
                [DebuggerHidden]
                get
                {
                    return this.__current;
                }
            }

            object IEnumerator.Current
            {
                [DebuggerHidden]
                get
                {
                    return this.__current;
                }
            }
        }

        [CompilerGenerated]
        private sealed class RowEnumeratorKeyValuePair : IEnumerable<KeyValuePair<int, Vector>>, IEnumerable, IEnumerator<KeyValuePair<int, Vector>>, IEnumerator, IDisposable
        {
            // Fields
            private int __state;
            private KeyValuePair<int, Vector> __current;
            public Matrix __this;
            private int __initialThreadId;
            public int __f;

            // Methods
            [DebuggerHidden]
            public RowEnumeratorKeyValuePair(int __state)
            {
                this.__state = __state;
                this.__initialThreadId = Thread.CurrentThread.ManagedThreadId;
            }

            public bool MoveNext()
            {
                switch (this.__state)
                {
                    case 0:
                        this.__state = -1;
                        this.__f = 0;
                        break;

                    case 1:
                        this.__state = -1;
                        this.__f++;
                        break;

                    default:
                        goto Label_0080;
                }
                if (this.__f < this.__this.mRows)
                {
                    this.__current = new KeyValuePair<int, Vector>(this.__f, this.__this.GetRow(this.__f));
                    this.__state = 1;
                    return true;
                }
                Label_0080:
                return false;
            }

            [DebuggerHidden]
            IEnumerator<KeyValuePair<int, Vector>> IEnumerable<KeyValuePair<int, Vector>>.GetEnumerator()
            {
                if ((Thread.CurrentThread.ManagedThreadId == this.__initialThreadId) && (this.__state == -2))
                {
                    this.__state = 0;
                    return this;
                }
                Matrix.RowEnumeratorKeyValuePair _e = new Matrix.RowEnumeratorKeyValuePair(0);
                _e.__this = this.__this;
                return _e;
            }

            [DebuggerHidden]
            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable<KeyValuePair<Int32, Vector>>)this).GetEnumerator();
            }

            [DebuggerHidden]
            void IEnumerator.Reset()
            {
                throw new NotSupportedException();
            }

            void IDisposable.Dispose()
            {
            }

            // Properties
            KeyValuePair<int, Vector> IEnumerator<KeyValuePair<int, Vector>>.Current
            {
                [DebuggerHidden]
                get
                {
                    return this.__current;
                }
            }

            object IEnumerator.Current
            {
                [DebuggerHidden]
                get
                {
                    return this.__current;
                }
            }
        }
    }
    [Serializable]
    public class DenseMatrix : Matrix
    {
        // Fields
        private readonly double[] mData;

        // Methods
        public DenseMatrix(Matrix other)
            : this(other.Rows, other.Columns)
        {
            DenseMatrix matrix = other as DenseMatrix;
            if (matrix == null)
            {
                foreach (KeyValuePair<int, Vector> pair in other.GetColumnEnumerator())
                {
                    int num = pair.Key * base.Rows;
                    foreach (KeyValuePair<int, double> pair2 in pair.Value.GetIndexedEnumerator())
                    {
                        this.mData[num + pair2.Key] = pair2.Value;
                    }
                }
            }
            else
            {
                Buffer.BlockCopy(matrix.mData, 0, this.mData, 0, this.mData.Length * 8);
            }
        }

        public DenseMatrix(int order)
            : this(order, order)
        {
        }

        public DenseMatrix(double[,] array)
            : this(array.GetLength(0), array.GetLength(1))
        {
            for (int i = 0; i < base.Columns; i++)
            {
                int num2 = i * base.Rows;
                for (int j = 0; j < base.Rows; j++)
                {
                    this.mData[num2 + j] = array[j, i];
                }
            }
        }

        public DenseMatrix(int rows, int columns)
            : base(rows, columns)
        {
            this.mData = new double[rows * columns];
        }

        public DenseMatrix(int rows, int columns, double value)
            : this(rows, columns)
        {
            for (int i = 0; i < this.mData.Length; i++)
            {
                this.mData[i] = value;
            }
        }

        public static Matrix Identity(int order)
        {
            DenseMatrix matrix = new DenseMatrix(order, order, 0.0);
            for (int i = 0; i < order; i++)
            {
                matrix[i, i] = 1;
            }
            return matrix;
        }

        public override void Add(Matrix other)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }
            if ((other.Rows != base.Rows) || (other.Columns != base.Columns))
            {
                throw new NotConformableException("other", Resources.ParameterNotConformable);
            }
            if (other is DenseMatrix)
            {
                DenseMatrix matrix = (DenseMatrix)other;
                for (int i = 0; i < this.mData.Length; i++)
                {
                    this.mData[i] += matrix.mData[i];
                }
            }
            else
            {
                base.Add(other);
            }
        }

        public override void Add(double scalar)
        {
            if (scalar != 0.0)
            {
                for (int i = 0; i < this.mData.Length; i++)
                {
                    this.mData[i] += scalar;
                }
            }
        }

        public override void Append(Matrix right, Matrix result)
        {
            if (right == null)
            {
                throw new ArgumentNullException("right");
            }
            if (right.Rows != base.Rows)
            {
                throw new NotConformableException("right", Resources.ParameterNotConformable);
            }
            if (result == null)
            {
                throw new ArgumentNullException("result");
            }
            if ((result.Columns != (base.Columns + right.Columns)) || (result.Rows != base.Rows))
            {
                throw new NotConformableException("result", Resources.ParameterNotConformable);
            }
            int count = (base.Rows * base.Columns) * 8;
            int num2 = (right.Rows * right.Columns) * 8;
            DenseMatrix matrix = (DenseMatrix)result;
            Buffer.BlockCopy(this.mData, 0, matrix.mData, 0, count);
            Buffer.BlockCopy(((DenseMatrix)right).mData, 0, matrix.mData, count, num2);
        }

        public override void Clear()
        {
            Array.Clear(this.mData, 0, this.mData.Length);
        }

        public override Matrix Clone()
        {
            return new DenseMatrix(this);
        }

        public override void CopyTo(Matrix target)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }
            if (!object.ReferenceEquals(this, target))
            {
                if ((base.Rows != target.Rows) || (base.Columns != target.Columns))
                {
                    throw new NotConformableException();
                }
                DenseMatrix matrix = target as DenseMatrix;
                if (matrix == null)
                {
                    base.CopyTo(target);
                }
                else
                {
                    Buffer.BlockCopy(this.mData, 0, matrix.mData, 0, this.mData.Length * 8);
                }
            }
        }

        protected internal override Matrix CreateMatrix(int numberOfRows, int numberOfColumns)
        {
            return new DenseMatrix(numberOfRows, numberOfColumns);
        }

        protected internal override Vector CreateVector(int size)
        {
            return new DenseVector(size);
        }

        public override double Determinant()
        {
            if (base.Rows != base.Columns)
            {
                throw new MatrixNotSquareException();
            }
            DenseLU elu = new DenseLU(this);
            return elu.Determinant();
        }

        public override void Gemm(double alpha, double beta, bool transposeA, bool transposeB, Matrix a, Matrix b)
        {
            if (a == null)
            {
                throw new ArgumentNullException("a");
            }
            if (b == null)
            {
                throw new ArgumentNullException("b");
            }
            if (!(a is DenseMatrix) || !(b is DenseMatrix))
            {
                base.Gemm(alpha, beta, transposeA, transposeB, a, b);
            }
            else
            {
                int rows = a.Rows;
                int num2 = b.Rows;
                int columns = a.Columns;
                int num4 = b.Columns;
                if (transposeA && transposeB)
                {
                    if (rows != num4)
                    {
                        throw new NotConformableException();
                    }
                    if ((base.Rows != columns) && (base.Columns != num2))
                    {
                        throw new NotConformableException();
                    }
                }
                else if (transposeA)
                {
                    if (rows != num2)
                    {
                        throw new NotConformableException();
                    }
                    if ((base.Rows != columns) && (base.Columns != num4))
                    {
                        throw new NotConformableException();
                    }
                }
                else if (transposeB)
                {
                    if (columns != num4)
                    {
                        throw new NotConformableException();
                    }
                    if ((base.Rows != rows) && (base.Columns != num2))
                    {
                        throw new NotConformableException();
                    }
                }
                else
                {
                    if (columns != num2)
                    {
                        throw new NotConformableException();
                    }
                    if ((base.Rows != rows) && (base.Columns != num4))
                    {
                        throw new NotConformableException();
                    }
                }
                if ((alpha == 0.0) && (beta == 0.0))
                {
                    this.Clear();
                }
                else
                {
                    double[] numArray;
                    double[] numArray2;
                    if (object.ReferenceEquals(a, this) || object.ReferenceEquals(a, b))
                    {
                        numArray = new DenseMatrix(a).mData;
                    }
                    else
                    {
                        numArray = ((DenseMatrix)a).mData;
                    }
                    if (object.ReferenceEquals(a, this) || object.ReferenceEquals(a, b))
                    {
                        numArray2 = new DenseMatrix(b).mData;
                    }
                    else
                    {
                        numArray2 = ((DenseMatrix)b).mData;
                    }
                    double[] mData = this.mData;
                    if (alpha == 1.0)
                    {
                        if (beta == 0.0)
                        {
                            if (transposeA && transposeB)
                            {
                                for (int i = 0; i != columns; i++)
                                {
                                    int num6 = i * base.Rows;
                                    for (int j = 0; j != num2; j++)
                                    {
                                        int num8 = j * rows;
                                        double num9 = 0.0;
                                        for (int k = 0; k != num4; k++)
                                        {
                                            num9 += numArray[num8 + k] * numArray2[(k * num2) + i];
                                        }
                                        mData[num6 + j] = num9;
                                    }
                                }
                            }
                            else if (transposeA)
                            {
                                for (int m = 0; m != num4; m++)
                                {
                                    int num12 = m * base.Rows;
                                    int num13 = m * num2;
                                    for (int n = 0; n != columns; n++)
                                    {
                                        int num15 = n * rows;
                                        double num16 = 0.0;
                                        for (int num17 = 0; num17 != rows; num17++)
                                        {
                                            num16 += numArray[num15 + num17] * numArray2[num13 + num17];
                                        }
                                        mData[num12 + n] = num16;
                                    }
                                }
                            }
                            else if (transposeB)
                            {
                                for (int num18 = 0; num18 != num2; num18++)
                                {
                                    int num19 = num18 * base.Rows;
                                    for (int num20 = 0; num20 != rows; num20++)
                                    {
                                        double num21 = 0.0;
                                        for (int num22 = 0; num22 != columns; num22++)
                                        {
                                            num21 += numArray[(num22 * rows) + num20] * numArray2[(num22 * num2) + num18];
                                        }
                                        mData[num19 + num20] = num21;
                                    }
                                }
                            }
                            else
                            {
                                for (int num23 = 0; num23 != num4; num23++)
                                {
                                    int num24 = num23 * base.Rows;
                                    int num25 = num23 * num2;
                                    for (int num26 = 0; num26 != rows; num26++)
                                    {
                                        double num27 = 0.0;
                                        for (int num28 = 0; num28 != columns; num28++)
                                        {
                                            num27 += numArray[(num28 * rows) + num26] * numArray2[num25 + num28];
                                        }
                                        mData[num24 + num26] = num27;
                                    }
                                }
                            }
                        }
                        else if (transposeA && transposeB)
                        {
                            for (int num29 = 0; num29 != columns; num29++)
                            {
                                int num30 = num29 * base.Rows;
                                for (int num31 = 0; num31 != num2; num31++)
                                {
                                    int num32 = num31 * rows;
                                    double num33 = 0.0;
                                    for (int num34 = 0; num34 != num4; num34++)
                                    {
                                        num33 += numArray[num32 + num34] * numArray2[(num34 * num2) + num29];
                                    }
                                    mData[num30 + num31] = (mData[num30 + num31] * beta) + num33;
                                }
                            }
                        }
                        else if (transposeA)
                        {
                            for (int num35 = 0; num35 != num4; num35++)
                            {
                                int num36 = num35 * base.Rows;
                                int num37 = num35 * num2;
                                for (int num38 = 0; num38 != columns; num38++)
                                {
                                    int num39 = num38 * rows;
                                    double num40 = 0.0;
                                    for (int num41 = 0; num41 != rows; num41++)
                                    {
                                        num40 += numArray[num39 + num41] * numArray2[num37 + num41];
                                    }
                                    mData[num36 + num38] = num40 + (mData[num36 + num38] * beta);
                                }
                            }
                        }
                        else if (transposeB)
                        {
                            for (int num42 = 0; num42 != num2; num42++)
                            {
                                int num43 = num42 * base.Rows;
                                for (int num44 = 0; num44 != rows; num44++)
                                {
                                    double num45 = 0.0;
                                    for (int num46 = 0; num46 != columns; num46++)
                                    {
                                        num45 += numArray[(num46 * rows) + num44] * numArray2[(num46 * num2) + num42];
                                    }
                                    mData[num43 + num44] = num45 + (mData[num43 + num44] * beta);
                                }
                            }
                        }
                        else
                        {
                            for (int num47 = 0; num47 != num4; num47++)
                            {
                                int num48 = num47 * base.Rows;
                                int num49 = num47 * num2;
                                for (int num50 = 0; num50 != rows; num50++)
                                {
                                    double num51 = 0.0;
                                    for (int num52 = 0; num52 != columns; num52++)
                                    {
                                        num51 += numArray[(num52 * rows) + num50] * numArray2[num49 + num52];
                                    }
                                    mData[num48 + num50] = num51 + (mData[num48 + num50] * beta);
                                }
                            }
                        }
                    }
                    else if (transposeA && transposeB)
                    {
                        for (int num53 = 0; num53 != columns; num53++)
                        {
                            int num54 = num53 * base.Rows;
                            for (int num55 = 0; num55 != num2; num55++)
                            {
                                int num56 = num55 * rows;
                                double num57 = 0.0;
                                for (int num58 = 0; num58 != num4; num58++)
                                {
                                    num57 += numArray[num56 + num58] * numArray2[(num58 * num2) + num53];
                                }
                                mData[num54 + num55] = (mData[num54 + num55] * beta) + (alpha * num57);
                            }
                        }
                    }
                    else if (transposeA)
                    {
                        for (int num59 = 0; num59 != num4; num59++)
                        {
                            int num60 = num59 * base.Rows;
                            int num61 = num59 * num2;
                            for (int num62 = 0; num62 != columns; num62++)
                            {
                                int num63 = num62 * rows;
                                double num64 = 0.0;
                                for (int num65 = 0; num65 != rows; num65++)
                                {
                                    num64 += numArray[num63 + num65] * numArray2[num61 + num65];
                                }
                                mData[num60 + num62] = (alpha * num64) + (mData[num60 + num62] * beta);
                            }
                        }
                    }
                    else if (transposeB)
                    {
                        for (int num66 = 0; num66 != num2; num66++)
                        {
                            int num67 = num66 * base.Rows;
                            for (int num68 = 0; num68 != rows; num68++)
                            {
                                double num69 = 0.0;
                                for (int num70 = 0; num70 != columns; num70++)
                                {
                                    num69 += numArray[(num70 * rows) + num68] * numArray2[(num70 * num2) + num66];
                                }
                                mData[num67 + num68] = (alpha * num69) + (mData[num67 + num68] * beta);
                            }
                        }
                    }
                    else
                    {
                        for (int num71 = 0; num71 != num4; num71++)
                        {
                            int num72 = num71 * base.Rows;
                            int num73 = num71 * num2;
                            for (int num74 = 0; num74 != rows; num74++)
                            {
                                double num75 = 0.0;
                                for (int num76 = 0; num76 != columns; num76++)
                                {
                                    num75 += numArray[(num76 * rows) + num74] * numArray2[num73 + num76];
                                }
                                mData[num72 + num74] = (alpha * num75) + (mData[num72 + num74] * beta);
                            }
                        }
                    }
                }
            }
        }

        public override void GetColumn(int columnIndex, int rowIndex, int length, Vector result)
        {
            if (result == null)
            {
                throw new ArgumentNullException("result");
            }
            if ((columnIndex >= base.Columns) || (columnIndex < 0))
            {
                throw new ArgumentOutOfRangeException("columnIndex");
            }
            if ((rowIndex >= base.Rows) || (rowIndex < 0))
            {
                throw new ArgumentOutOfRangeException("rowIndex");
            }
            if ((rowIndex + length) > base.Rows)
            {
                throw new ArgumentOutOfRangeException("length");
            }
            if (length < 1)
            {
                throw new ArgumentException(Resources.NotPositive, "length");
            }
            if (result.Count < length)
            {
                throw new NotConformableException("result", Resources.ParameterNotConformable);
            }
            if (result is DenseVector)
            {
                Buffer.BlockCopy(this.Data, ((columnIndex * base.Rows) + rowIndex) * 8, ((DenseVector)result).Data, 0, length * 8);
            }
            else
            {
                base.GetColumn(columnIndex, rowIndex, length, result);
            }
        }

        public override Matrix Inverse()
        {
            if (base.Rows != base.Columns)
            {
                throw new MatrixNotSquareException();
            }
            return new DenseLU(this).Inverse();
        }

        public override void Multiply(double scalar)
        {
            if (scalar != 1.0)
            {
                for (int i = 0; i < this.mData.Length; i++)
                {
                    this.mData[i] *= scalar;
                }
            }
        }

        public static DenseMatrix operator +(DenseMatrix leftSide, DenseMatrix rightSide)
        {
            if (rightSide == null)
            {
                throw new ArgumentNullException("rightSide");
            }
            if (leftSide == null)
            {
                throw new ArgumentNullException("leftSide");
            }
            if ((leftSide.Rows != rightSide.Rows) || (leftSide.Columns != rightSide.Columns))
            {
                throw new NotConformableException(Resources.ParametersNotConformable);
            }
            DenseMatrix matrix = new DenseMatrix(leftSide);
            matrix.Add(rightSide);
            return matrix;
        }

        public static DenseMatrix operator /(DenseMatrix leftSide, double rightSide)
        {
            if (leftSide == null)
            {
                throw new ArgumentNullException("leftSide");
            }
            DenseMatrix matrix = new DenseMatrix(leftSide);
            matrix.Divide(rightSide);
            return matrix;
        }

        public static DenseMatrix operator *(DenseMatrix leftSide, DenseMatrix rightSide)
        {
            if (leftSide == null)
            {
                throw new ArgumentNullException("leftSide");
            }
            if (rightSide == null)
            {
                throw new ArgumentNullException("rightSide");
            }
            if (leftSide.Columns != rightSide.Rows)
            {
                throw new NotConformableException(Resources.ParametersNotConformable);
            }
            return (DenseMatrix)leftSide.Multiply(rightSide);
        }

        public static DenseVector operator *(DenseMatrix leftSide, DenseVector rightSide)
        {
            if (leftSide == null)
            {
                throw new ArgumentNullException("leftSide");
            }
            if (rightSide == null)
            {
                throw new ArgumentNullException("rightSide");
            }
            if (leftSide.Columns != rightSide.Count)
            {
                throw new NotConformableException(Resources.ParametersNotConformable);
            }
            return (DenseVector)leftSide.Multiply(rightSide);
        }

        public static DenseMatrix operator *(DenseMatrix leftSide, double rightSide)
        {
            if (leftSide == null)
            {
                throw new ArgumentNullException("leftSide");
            }
            DenseMatrix matrix = new DenseMatrix(leftSide);
            matrix.Multiply(rightSide);
            return matrix;
        }

        public static DenseVector operator *(DenseVector leftSide, DenseMatrix rightSide)
        {
            if (leftSide == null)
            {
                throw new ArgumentNullException("leftSide");
            }
            if (rightSide == null)
            {
                throw new ArgumentNullException("rightSide");
            }
            if (leftSide.Count != rightSide.Rows)
            {
                throw new NotConformableException(Resources.ParametersNotConformable);
            }
            return (DenseVector)rightSide.LeftMultiply(leftSide);
        }

        public static DenseMatrix operator *(double leftSide, DenseMatrix rightSide)
        {
            if (rightSide == null)
            {
                throw new ArgumentNullException("rightSide");
            }
            DenseMatrix matrix = new DenseMatrix(rightSide);
            matrix.Multiply(leftSide);
            return matrix;
        }

        public static DenseMatrix operator -(DenseMatrix leftSide, DenseMatrix rightSide)
        {
            if (rightSide == null)
            {
                throw new ArgumentNullException("rightSide");
            }
            if (leftSide == null)
            {
                throw new ArgumentNullException("leftSide");
            }
            if ((leftSide.Rows != rightSide.Rows) || (leftSide.Columns != rightSide.Columns))
            {
                throw new NotConformableException(Resources.ParametersNotConformable);
            }
            DenseMatrix matrix = new DenseMatrix(leftSide);
            matrix.Subtract(rightSide);
            return matrix;
        }

        public static DenseMatrix operator -(DenseMatrix rightSide)
        {
            if (rightSide == null)
            {
                throw new ArgumentNullException("rightSide");
            }
            DenseMatrix matrix = new DenseMatrix(rightSide);
            matrix.Negate();
            return matrix;
        }

        public override void PointwiseMultiply(Matrix other, Matrix result)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }
            if (result == null)
            {
                throw new ArgumentNullException("result");
            }
            if ((base.Columns != other.Columns) || (base.Rows != other.Rows))
            {
                throw new NotConformableException("result");
            }
            if ((base.Columns != result.Columns) || (base.Rows != result.Rows))
            {
                throw new NotConformableException("result");
            }
            if ((result is DenseMatrix) && (other is DenseMatrix))
            {
                DenseMatrix matrix = (DenseMatrix)result;
                DenseMatrix matrix2 = (DenseMatrix)other;
                for (int i = 0; i < this.mData.Length; i++)
                {
                    matrix.mData[i] = this.mData[i] * matrix2.mData[i];
                }
            }
            else
            {
                base.PointwiseMultiply(other, result);
            }
        }

        public override void SetColumn(int index, double[] source)
        {
            if ((index < 0) || (index >= base.Columns))
            {
                throw new ArgumentOutOfRangeException("index");
            }
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            if (source.Length != base.Rows)
            {
                throw new NotConformableException("source", Resources.ParameterNotConformable);
            }
            Buffer.BlockCopy(source, 0, this.mData, (index * base.Rows) * 8, source.Length * 8);
        }

        public override void Subtract(Matrix other)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }
            if ((other.Rows != base.Rows) || (other.Columns != base.Columns))
            {
                throw new NotConformableException("other", Resources.ParameterNotConformable);
            }
            if (other is DenseMatrix)
            {
                DenseMatrix matrix = (DenseMatrix)other;
                for (int i = 0; i < this.mData.Length; i++)
                {
                    this.mData[i] -= matrix.mData[i];
                }
            }
            else
            {
                base.Subtract(other);
            }
        }

        public override double[] ToColumnWiseArray()
        {
            double[] dst = new double[this.mData.Length];
            Buffer.BlockCopy(this.mData, 0, dst, 0, this.mData.Length * 8);
            return dst;
        }

        public override Matrix Transpose()
        {
            DenseMatrix matrix = new DenseMatrix(base.Columns, base.Rows);
            for (int i = 0; i < base.Columns; i++)
            {
                int num2 = i * base.Rows;
                for (int j = 0; j < base.Rows; j++)
                {
                    matrix.mData[(j * base.Columns) + i] = this.mData[num2 + j];
                }
            }
            return matrix;
        }

        protected internal override double ValueAt(int row, int column)
        {
            return this.mData[(column * base.Rows) + row];
        }

        protected internal override void ValueAt(int row, int column, double value)
        {
            this.mData[(column * base.Rows) + row] = value;
        }

        // Properties
        internal double[] Data
        {
            get
            {
                return this.mData;
            }
        }
    }
}
