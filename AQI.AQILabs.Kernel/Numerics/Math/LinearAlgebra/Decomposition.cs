using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AQI.AQILabs.Kernel.Numerics.Math.Properties;
using AQI.AQILabs.Kernel.Numerics.Math.Math;

namespace AQI.AQILabs.Kernel.Numerics.Math.LinearAlgebra.Decomposition
{
    using Math = System.Math;

    public class Cholesky
    {
        // Fields
        private readonly AbstractCholesky mCholesky;

        // Methods
        public Cholesky(Matrix matrix)
        {
            if (matrix == null)
            {
                throw new ArgumentNullException("matrix", Resources.NullParameterException);
            }
            if (matrix.Rows != matrix.Columns)
            {
                throw new MatrixNotSquareException(Resources.NotSqurare);
            }
            if (matrix.GetType() == typeof(DenseMatrix))
            {
                this.mCholesky = new DenseCholesky(matrix);
            }
            else
            {
                this.mCholesky = new UserCholesky(matrix);
            }
        }

        public double Determinant()
        {
            return this.mCholesky.Determinant();
        }

        public Matrix Factor()
        {
            return this.mCholesky.Factor();
        }

        public bool IsPositiveDefinite()
        {
            return this.mCholesky.IsPositiveDefinite();
        }

        public Matrix Solve(Matrix input)
        {
            return this.mCholesky.Solve(input);
        }

        public Vector Solve(Vector input)
        {
            return this.mCholesky.Solve(input);
        }

        public void Solve(Matrix input, Matrix result)
        {
            this.mCholesky.Solve(input, result);
        }

        public void Solve(Vector input, Vector result)
        {
            this.mCholesky.Solve(input, result);
        }
    }

    internal class UserCholesky : AbstractCholesky
    {
        // Methods
        public UserCholesky(Matrix matrix)
            : base(matrix)
        {
        }

        protected override bool DoCompute(Matrix data, int order)
        {
            Matrix matrix = data.CreateMatrix(order, order);
            for (int i = 0; i < order; i++)
            {
                double d = 0.0;
                for (int j = 0; j < i; j++)
                {
                    double num4 = 0.0;
                    for (int m = 0; m < j; m++)
                    {
                        num4 += matrix.ValueAt(j, m) * matrix.ValueAt(i, m);
                    }
                    num4 = (data.ValueAt(i, j) - num4) / matrix.ValueAt(j, j);
                    matrix.ValueAt(i, j, num4);
                    d += num4 * num4;
                }
                d = data.ValueAt(i, i) - d;
                if (d <= 0.0)
                {
                    return false;
                }
                matrix.ValueAt(i, i, Math.Sqrt(d));
                for (int k = i + 1; k < order; k++)
                {
                    matrix.ValueAt(i, k, 0.0);
                }
            }
            matrix.CopyTo(data);
            return true;
        }

        protected override void DoSolve(Matrix factor, Matrix result)
        {
            int rows = factor.Rows;
            for (int i = 0; i < result.Columns; i++)
            {
                double num4;
                for (int j = 0; j < rows; j++)
                {
                    num4 = result.ValueAt(j, i);
                    for (int m = j - 1; m >= 0; m--)
                    {
                        num4 -= factor.ValueAt(j, m) * result.ValueAt(m, i);
                    }
                    result.ValueAt(j, i, num4 / factor.ValueAt(j, j));
                }
                for (int k = rows - 1; k >= 0; k--)
                {
                    num4 = result.ValueAt(k, i);
                    for (int n = k + 1; n < rows; n++)
                    {
                        num4 -= factor.ValueAt(n, k) * result.ValueAt(n, i);
                    }
                    result.ValueAt(k, i, num4 / factor.ValueAt(k, k));
                }
            }
        }

        protected override void DoSolve(Matrix factor, Vector result)
        {
            double num3;
            int rows = factor.Rows;
            for (int i = 0; i < rows; i++)
            {
                num3 = result[i];
                for (int k = i - 1; k >= 0; k--)
                {
                    num3 -= factor.ValueAt(i, k) * result[k];
                }
                result[i] = num3 / factor.ValueAt(i, i);
            }
            for (int j = rows - 1; j >= 0; j--)
            {
                num3 = result[j];
                for (int m = j + 1; m < rows; m++)
                {
                    num3 -= factor.ValueAt(m, j) * result[m];
                }
                result[j] = num3 / factor.ValueAt(j, j);
            }
        }
    }

    internal class DenseCholesky : AbstractCholesky
    {
        // Methods
        public DenseCholesky(Matrix matrix)
            : base(matrix)
        {
        }

        protected override bool DoCompute(Matrix matrix, int order)
        {
            double[] data = ((DenseMatrix)matrix).Data;
            double[] src = new double[data.Length];
            for (int i = 0; i < order; i++)
            {
                int num7;
                double d = 0.0;
                for (int j = 0; j < i; j++)
                {
                    double num4 = 0.0;
                    for (int m = 0; m < j; m++)
                    {
                        num4 += src[(m * order) + j] * src[(m * order) + i];
                    }
                    int num6 = j * order;
                    num7 = num6 + i;
                    src[num7] = num4 = (data[num7] - num4) / src[num6 + j];
                    d += num4 * num4;
                }
                num7 = (i * order) + i;
                d = data[num7] - d;
                if (d <= 0.0)
                {
                    return false;
                }
                src[num7] = Math.Sqrt(d);
                for (int k = i + 1; k < order; k++)
                {
                    src[(k * order) + i] = 0.0;
                }
            }
            Buffer.BlockCopy(src, 0, data, 0, src.Length * 8);
            return true;
        }

        protected override void DoSolve(Matrix factor, Matrix result)
        {
            Solve(((DenseMatrix)factor).Data, ((DenseMatrix)result).Data, factor.Rows, result.Columns);
        }

        protected override void DoSolve(Matrix factor, Vector result)
        {
            Solve(((DenseMatrix)factor).Data, ((DenseVector)result).Data, factor.Rows, 1);
        }

        private static void Solve(double[] data, double[] rhs, int order, int columns)
        {
            for (int i = 0; i < columns; i++)
            {
                double num4;
                int num2 = i * order;
                for (int j = 0; j < order; j++)
                {
                    num4 = rhs[(i * order) + j];
                    for (int m = j - 1; m >= 0; m--)
                    {
                        num4 -= data[(m * order) + j] * rhs[num2 + m];
                    }
                    rhs[num2 + j] = num4 / data[(j * order) + j];
                }
                for (int k = order - 1; k >= 0; k--)
                {
                    num4 = rhs[num2 + k];
                    int num7 = k * order;
                    for (int n = k + 1; n < order; n++)
                    {
                        num4 -= data[num7 + n] * rhs[num2 + n];
                    }
                    rhs[num2 + k] = num4 / data[num7 + k];
                }
            }
        }
    }

    internal abstract class AbstractCholesky
    {
        // Fields
        private bool mComputed;
        private double mDeterminant = double.MinValue;
        private readonly Matrix mFactor;
        private bool mIsPositiveDefinite;
        private readonly int mOrder;

        // Methods
        protected AbstractCholesky(Matrix matrix)
        {
            this.mOrder = matrix.Rows;
            this.mFactor = matrix.Clone();
        }

        private void Compute()
        {
            if (!this.mComputed)
            {
                this.mIsPositiveDefinite = this.DoCompute(this.mFactor, this.mOrder);
                this.mComputed = true;
            }
        }

        public double Determinant()
        {
            if (!this.IsPositiveDefinite())
            {
                throw new NotPositiveDefiniteException();
            }
            if (this.mDeterminant == double.MinValue)
            {
                lock (this.mFactor)
                {
                    this.mDeterminant = 1.0;
                    int mOrder = this.mOrder;
                    for (int i = 0; i < mOrder; i++)
                    {
                        double num3 = this.mFactor.ValueAt(i, i);
                        this.mDeterminant *= num3 * num3;
                    }
                }
            }
            return this.mDeterminant;
        }

        protected abstract bool DoCompute(Matrix data, int order);
        protected abstract void DoSolve(Matrix factor, Matrix result);
        protected abstract void DoSolve(Matrix factor, Vector result);
        public Matrix Factor()
        {
            if (!this.IsPositiveDefinite())
            {
                throw new NotPositiveDefiniteException();
            }
            return this.mFactor.Clone();
        }

        public bool IsPositiveDefinite()
        {
            this.Compute();
            return this.mIsPositiveDefinite;
        }

        public Matrix Solve(Matrix input)
        {
            Matrix result = this.mFactor.CreateMatrix(this.mFactor.Columns, input.Columns);
            this.Solve(input, result);
            return result;
        }

        public Vector Solve(Vector input)
        {
            Vector result = this.mFactor.CreateVector(this.mFactor.Columns);
            this.Solve(input, result);
            return result;
        }

        public void Solve(Matrix input, Matrix result)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }
            if (result == null)
            {
                throw new ArgumentNullException("result");
            }
            if (this.mFactor.Rows != input.Rows)
            {
                throw new NotConformableException("input", Resources.ParameterNotConformable);
            }
            if (this.mFactor.Columns != result.Rows)
            {
                throw new NotConformableException("result", Resources.ParameterNotConformable);
            }
            if (input.Columns != result.Columns)
            {
                throw new NotConformableException("result", Resources.ParameterNotConformable);
            }
            if (!this.IsPositiveDefinite())
            {
                throw new NotPositiveDefiniteException();
            }
            input.CopyTo(result);
            this.DoSolve(this.mFactor, result);
        }

        public void Solve(Vector input, Vector result)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }
            if (result == null)
            {
                throw new ArgumentNullException("result");
            }
            if (this.mFactor.Rows != input.Count)
            {
                throw new NotConformableException("input", Resources.ParameterNotConformable);
            }
            if (this.mFactor.Columns != result.Count)
            {
                throw new NotConformableException("result", Resources.ParameterNotConformable);
            }
            if (!this.IsPositiveDefinite())
            {
                throw new NotPositiveDefiniteException();
            }
            input.CopyTo(result);
            this.DoSolve(this.mFactor, result);
        }
    }

    internal class DenseLU : AbstractLU
    {
        // Methods
        public DenseLU(Matrix matrix)
            : base(matrix)
        {
        }

        protected override Matrix ComputeInverse(Matrix matrix, int[] pivots)
        {
            int rows = matrix.Rows;
            DenseMatrix result = new DenseMatrix(rows, rows);
            for (int i = 0; i < rows; i++)
            {
                result.Data[i + (rows * i)] = 1.0;
            }
            this.Solve(base.mMatrix, base.mPivots, result);
            return result;
        }

        protected override void DoCompute(Matrix data, int[] pivots)
        {
            DenseMatrix matrix = (DenseMatrix)data;
            int rows = matrix.Rows;
            for (int i = 0; i < rows; i++)
            {
                pivots[i] = i;
            }
            double[] numArray = new double[rows];
            for (int j = 0; j < rows; j++)
            {
                int num4 = j * rows;
                int index = num4 + j;
                for (int k = 0; k < rows; k++)
                {
                    numArray[k] = matrix.Data[num4 + k];
                }
                for (int m = 0; m < rows; m++)
                {
                    int num8 = Math.Min(m, j);
                    double num9 = 0.0;
                    for (int num10 = 0; num10 < num8; num10++)
                    {
                        num9 += matrix.Data[(num10 * rows) + m] * numArray[num10];
                    }
                    matrix.Data[num4 + m] = numArray[m] -= num9;
                }
                int num12 = j;
                for (int n = j + 1; n < rows; n++)
                {
                    if (Math.Abs(numArray[n]) > Math.Abs(numArray[num12]))
                    {
                        num12 = n;
                    }
                }
                if (num12 != j)
                {
                    for (int num14 = 0; num14 < rows; num14++)
                    {
                        int num15 = num14 * rows;
                        int num16 = num15 + num12;
                        int num17 = num15 + j;
                        double num18 = matrix.Data[num16];
                        matrix.Data[num16] = matrix.Data[num17];
                        matrix.Data[num17] = num18;
                    }
                    pivots[j] = num12;
                }
                if ((j < rows) & (matrix.Data[index] != 0.0))
                {
                    for (int num19 = j + 1; num19 < rows; num19++)
                    {
                        matrix.Data[num4 + num19] /= matrix.Data[index];
                    }
                }
            }
        }

        private static void Pivot(int m, int n, double[] B, int[] pivots)
        {
            for (int i = 0; i < pivots.Length; i++)
            {
                if (pivots[i] != i)
                {
                    int num2 = pivots[i];
                    for (int j = 0; j < n; j++)
                    {
                        int num4 = j * m;
                        int index = num4 + num2;
                        int num6 = num4 + i;
                        double num7 = B[index];
                        B[index] = B[num6];
                        B[num6] = num7;
                    }
                }
            }
        }

        protected override void Solve(Matrix factor, int[] pivots, Matrix result)
        {
            Solve(factor.Rows, result.Columns, ((DenseMatrix)factor).Data, pivots, ((DenseMatrix)result).Data);
        }

        protected override void Solve(Matrix factor, int[] pivots, Vector result)
        {
            Solve(factor.Rows, 1, ((DenseMatrix)factor).Data, pivots, ((DenseVector)result).Data);
        }

        private static void Solve(int order, int columns, double[] factor, int[] pivots, double[] data)
        {
            Pivot(order, columns, data, pivots);
            for (int i = 0; i < order; i++)
            {
                int num2 = i * order;
                for (int k = i + 1; k < order; k++)
                {
                    for (int m = 0; m < columns; m++)
                    {
                        int num5 = m * order;
                        data[k + num5] -= data[i + num5] * factor[k + num2];
                    }
                }
            }
            for (int j = order - 1; j >= 0; j--)
            {
                int index = j + (j * order);
                for (int n = 0; n < columns; n++)
                {
                    data[j + (n * order)] /= factor[index];
                }
                index = j * order;
                for (int num9 = 0; num9 < j; num9++)
                {
                    for (int num10 = 0; num10 < columns; num10++)
                    {
                        int num11 = num10 * order;
                        data[num9 + num11] -= data[j + num11] * factor[num9 + index];
                    }
                }
            }
        }
    }

    internal class UserLU : AbstractLU
    {
        // Methods
        public UserLU(Matrix matrix)
            : base(matrix)
        {
        }

        protected override Matrix ComputeInverse(Matrix matrix, int[] pivots)
        {
            int rows = matrix.Rows;
            Matrix result = matrix.CreateMatrix(rows, rows);
            for (int i = 0; i < rows; i++)
            {
                result.ValueAt(i, i, 1.0);
            }
            this.Solve(base.mMatrix, base.mPivots, result);
            return result;
        }

        protected override void DoCompute(Matrix matrix, int[] pivots)
        {
            int rows = matrix.Rows;
            for (int i = 0; i < rows; i++)
            {
                pivots[i] = i;
            }
            double[] numArray = new double[rows];
            for (int j = 0; j < rows; j++)
            {
                for (int k = 0; k < rows; k++)
                {
                    numArray[k] = matrix.ValueAt(k, j);
                }
                for (int m = 0; m < rows; m++)
                {
                    int num6 = Math.Min(m, j);
                    double num7 = 0.0;
                    for (int num8 = 0; num8 < num6; num8++)
                    {
                        num7 += matrix.ValueAt(m, num8) * numArray[num8];
                    }
                    numArray[m] -= num7;
                    matrix.ValueAt(m, j, numArray[m]);
                }
                int index = j;
                for (int n = j + 1; n < rows; n++)
                {
                    if (Math.Abs(numArray[n]) > Math.Abs(numArray[index]))
                    {
                        index = n;
                    }
                }
                if (index != j)
                {
                    for (int num11 = 0; num11 < rows; num11++)
                    {
                        double num12 = matrix.ValueAt(index, num11);
                        matrix.ValueAt(index, num11, matrix.ValueAt(j, num11));
                        matrix.ValueAt(j, num11, num12);
                    }
                    pivots[j] = index;
                }
                if ((j < rows) & (matrix.ValueAt(j, j) != 0.0))
                {
                    for (int num13 = j + 1; num13 < rows; num13++)
                    {
                        matrix.ValueAt(num13, j, matrix.ValueAt(num13, j) / matrix.ValueAt(j, j));
                    }
                }
            }
        }

        private void Pivot(Matrix data)
        {
            for (int i = 0; i < base.mPivots.Length; i++)
            {
                if (base.mPivots[i] != i)
                {
                    int row = base.mPivots[i];
                    for (int j = 0; j < data.Columns; j++)
                    {
                        double num4 = data.ValueAt(row, j);
                        data.ValueAt(row, j, data.ValueAt(i, j));
                        data.ValueAt(i, j, num4);
                    }
                }
            }
        }

        private void Pivot(Vector data)
        {
            for (int i = 0; i < base.mPivots.Length; i++)
            {
                if (base.mPivots[i] != i)
                {
                    int num2 = base.mPivots[i];
                    double num3 = data[num2];
                    data[num2] = data[i];
                    data[i] = num3;
                }
            }
        }

        protected override void Solve(Matrix factor, int[] pivots, Matrix result)
        {
            this.Pivot(result);
            int rows = factor.Rows;
            for (int i = 0; i < rows; i++)
            {
                for (int k = i + 1; k < rows; k++)
                {
                    for (int m = 0; m < result.Columns; m++)
                    {
                        double num5 = result.ValueAt(i, m) * factor.ValueAt(k, i);
                        result.ValueAt(k, m, result.ValueAt(k, m) - num5);
                    }
                }
            }
            for (int j = rows - 1; j >= 0; j--)
            {
                for (int n = 0; n < result.Columns; n++)
                {
                    result.ValueAt(j, n, result.ValueAt(j, n) / factor.ValueAt(j, j));
                }
                for (int num8 = 0; num8 < j; num8++)
                {
                    for (int num9 = 0; num9 < result.Columns; num9++)
                    {
                        double num10 = result.ValueAt(j, num9) * factor.ValueAt(num8, j);
                        result.ValueAt(num8, num9, result.ValueAt(num8, num9) - num10);
                    }
                }
            }
        }

        protected override void Solve(Matrix factor, int[] pivots, Vector result)
        {
            this.Pivot(result);
            int rows = factor.Rows;
            for (int i = 0; i < rows; i++)
            {
                for (int k = i + 1; k < rows; k++)
                {
                    Vector vector;
                    int num4;
                    (vector = result)[num4 = k] = vector[num4] - (result[i] * factor.ValueAt(k, i));
                }
            }
            for (int j = rows - 1; j >= 0; j--)
            {
                Vector vector2;
                int num6;
                (vector2 = result)[num6 = j] = vector2[num6] / factor.ValueAt(j, j);
                for (int m = 0; m < j; m++)
                {
                    Vector vector3;
                    int num8;
                    (vector3 = result)[num8 = m] = vector3[num8] - (result[j] * factor.ValueAt(m, j));
                }
            }
        }
    }

    internal abstract class AbstractLU
    {
        // Fields
        private bool mComputed;
        private double mDeterminant = double.MinValue;
        private bool mIsSingular;
        protected readonly Matrix mMatrix;
        protected int[] mPivots;

        // Methods
        protected AbstractLU(Matrix matrix)
        {
            this.mMatrix = matrix.Clone();
        }

        private void Compute()
        {
            if (!this.mComputed)
            {
                this.mPivots = new int[this.mMatrix.Rows];
                this.DoCompute(this.mMatrix, this.mPivots);
                for (int i = 0; i < this.mMatrix.Rows; i++)
                {
                    if (this.mMatrix.ValueAt(i, i) == 0.0)
                    {
                        this.mIsSingular = true;
                        break;
                    }
                }
                this.mComputed = true;
            }
        }

        protected abstract Matrix ComputeInverse(Matrix matrix, int[] pivots);
        public double Determinant()
        {
            this.Compute();
            if (this.mIsSingular)
            {
                return 0.0;
            }
            if (this.mDeterminant == double.MinValue)
            {
                lock (this.mMatrix)
                {
                    this.mDeterminant = 1.0;
                    for (int i = 0; i < this.mMatrix.Rows; i++)
                    {
                        if (this.mPivots[i] != i)
                        {
                            this.mDeterminant = -this.mDeterminant * this.mMatrix.ValueAt(i, i);
                        }
                        else
                        {
                            this.mDeterminant *= this.mMatrix.ValueAt(i, i);
                        }
                    }
                }
            }
            return this.mDeterminant;
        }

        protected abstract void DoCompute(Matrix matrix, int[] pivots);
        public Matrix Inverse()
        {
            if (this.IsSingular())
                throw new SingularMatrixException();

            this.Compute();
            return this.ComputeInverse(this.mMatrix, this.mPivots);
        }

        public bool IsSingular()
        {
            this.Compute();
            return this.mIsSingular;
        }

        public Matrix LowerFactor()
        {
            this.Compute();
            Matrix lowerTriangle = this.mMatrix.GetLowerTriangle();
            for (int i = 0; i < lowerTriangle.Rows; i++)
            {
                lowerTriangle.ValueAt(i, i, 1.0);
            }
            return lowerTriangle;
        }

        public int[] Pivots()
        {
            this.Compute();
            int[] dst = new int[this.mPivots.Length];
            Buffer.BlockCopy(this.mPivots, 0, dst, 0, this.mPivots.Length * 4);
            return dst;
        }

        public Matrix Solve(Matrix input)
        {
            Matrix result = this.mMatrix.CreateMatrix(this.mMatrix.Columns, input.Columns);
            this.Solve(input, result);
            return result;
        }

        public Vector Solve(Vector input)
        {
            Vector result = this.mMatrix.CreateVector(this.mMatrix.Columns);
            this.Solve(input, result);
            return result;
        }

        public void Solve(Matrix input, Matrix result)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }
            if (result == null)
            {
                throw new ArgumentNullException("result");
            }
            if (this.mMatrix.Rows != input.Rows)
            {
                throw new NotConformableException("input", Resources.ParameterNotConformable);
            }
            if (this.mMatrix.Columns != result.Rows)
            {
                throw new NotConformableException("result", Resources.ParameterNotConformable);
            }
            if (input.Columns != result.Columns)
            {
                throw new NotConformableException("result", Resources.ParameterNotConformable);
            }
            if (this.IsSingular())
            {
                throw new SingularMatrixException();
            }
            input.CopyTo(result);
            this.Solve(this.mMatrix, this.mPivots, result);
        }

        public void Solve(Vector input, Vector result)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }
            if (result == null)
            {
                throw new ArgumentNullException("result");
            }
            if (this.mMatrix.Rows != input.Count)
            {
                throw new NotConformableException("input", Resources.ParameterNotConformable);
            }
            if (this.mMatrix.Columns != result.Count)
            {
                throw new NotConformableException("result", Resources.ParameterNotConformable);
            }
            if (this.IsSingular())
            {
                throw new SingularMatrixException();
            }
            input.CopyTo(result);
            this.Solve(this.mMatrix, this.mPivots, result);
        }

        protected abstract void Solve(Matrix factor, int[] pivots, Matrix result);
        protected abstract void Solve(Matrix factor, int[] pivots, Vector result);
        public Matrix UpperFactor()
        {
            this.Compute();
            return this.mMatrix.GetUpperTriangle();
        }
    }

    public class LU
    {
        // Fields
        private readonly AbstractLU mLU;

        // Methods
        public LU(Matrix matrix)
        {
            if (matrix == null)
            {
                throw new ArgumentNullException("matrix", Resources.NullParameterException);
            }
            if (matrix.Rows != matrix.Columns)
            {
                throw new MatrixNotSquareException(Resources.NotSqurare);
            }
            if (matrix.GetType() == typeof(DenseMatrix))
            {
                this.mLU = new DenseLU(matrix);
            }
            else
            {
                this.mLU = new UserLU(matrix);
            }
        }

        public double Determinant()
        {
            return this.mLU.Determinant();
        }

        public Matrix Inverse()
        {
            return this.mLU.Inverse();
        }

        public bool IsSingular()
        {
            return this.mLU.IsSingular();
        }

        public Matrix LowerFactor()
        {
            return this.mLU.LowerFactor();
        }

        public int[] Pivots()
        {
            return this.mLU.Pivots();
        }

        public Matrix Solve(Matrix input)
        {
            return this.mLU.Solve(input);
        }

        public Vector Solve(Vector input)
        {
            return this.mLU.Solve(input);
        }

        public void Solve(Matrix input, Matrix result)
        {
            this.mLU.Solve(input, result);
        }

        public void Solve(Vector input, Vector result)
        {
            this.mLU.Solve(input, result);
        }

        public Matrix UpperFactor()
        {
            return this.mLU.UpperFactor();
        }
    }

    internal abstract class AbstractSvd
    {
        // Fields
        protected int mColumns;
        private bool mComputed;
        protected bool mComputeVectors;
        protected bool mConverged;
        protected int mRank;
        protected int mRows;
        protected Vector mS;
        protected Matrix mU;
        protected Matrix mV;

        // Methods
        protected AbstractSvd(bool computeVectors)
        {
            this.mComputeVectors = computeVectors;
        }

        private void Compute()
        {
            if (!this.mComputed)
            {
                this.DoCompute();
                this.mComputed = true;
            }
        }

        public double ConditionNumber()
        {
            int num = Math.Min(this.mRows, this.mColumns) - 1;
            return (this.mS[0] / this.mS[num]);
        }

        public bool Converged()
        {
            this.Compute();
            return this.mConverged;
        }

        protected abstract void DoCompute();
        public double Norm2()
        {
            return this.mS[0];
        }

        public int Rank()
        {
            return this.mRank;
        }

        public Vector S()
        {
            return this.mS.Clone();
        }

        public Matrix Solve(Matrix input)
        {
            if (!this.mComputeVectors)
            {
                throw new InvalidMatrixOperationException(Resources.SingularVectorsNotComputed);
            }
            if (!this.Converged())
            {
                throw new ConvergenceFailedException();
            }
            Matrix result = this.mU.CreateMatrix(this.mColumns, input.Columns);
            this.Solve(input, result);
            return result;
        }

        public Vector Solve(Vector input)
        {
            if (!this.mComputeVectors)
            {
                throw new InvalidMatrixOperationException(Resources.SingularVectorsNotComputed);
            }
            if (!this.Converged())
            {
                throw new ConvergenceFailedException();
            }
            Vector result = this.mU.CreateVector(this.mColumns);
            this.Solve(input, result);
            return result;
        }

        public void Solve(Matrix input, Matrix result)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }
            if (result == null)
            {
                throw new ArgumentNullException("result");
            }
            if (input.Columns != result.Columns)
            {
                throw new NotConformableException("result", Resources.ParameterNotConformable);
            }
            if (!this.mComputeVectors)
            {
                throw new InvalidMatrixOperationException(Resources.SingularVectorsNotComputed);
            }
            if (!this.Converged())
            {
                throw new ConvergenceFailedException();
            }
            if (this.mU.Rows != input.Rows)
            {
                throw new NotConformableException("input", Resources.ParameterNotConformable);
            }
            if (this.mV.Columns != result.Rows)
            {
                throw new NotConformableException("result", Resources.ParameterNotConformable);
            }
            int num = Math.Min(this.mRows, this.mColumns);
            int columns = input.Columns;
            double[] numArray = new double[this.mColumns];
            for (int i = 0; i < columns; i++)
            {
                for (int j = 0; j < this.mColumns; j++)
                {
                    double num5 = 0.0;
                    if (j < num)
                    {
                        for (int m = 0; m < this.mRows; m++)
                        {
                            num5 += this.mU.ValueAt(m, j) * input.ValueAt(m, i);
                        }
                        num5 /= this.mS[j];
                    }
                    numArray[j] = num5;
                }
                for (int k = 0; k < this.mColumns; k++)
                {
                    double num8 = 0.0;
                    for (int n = 0; n < this.mColumns; n++)
                    {
                        num8 += this.mV.ValueAt(n, k) * numArray[n];
                    }
                    result[k, i] = num8;
                }
            }
        }

        public void Solve(Vector input, Vector result)
        {
            double num3;
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }
            if (result == null)
            {
                throw new ArgumentNullException("result");
            }
            if (!this.mComputeVectors)
            {
                throw new InvalidMatrixOperationException(Resources.SingularVectorsNotComputed);
            }
            if (!this.Converged())
            {
                throw new ConvergenceFailedException();
            }
            if (this.mU.Rows != input.Count)
            {
                throw new NotConformableException("input", Resources.ParameterNotConformable);
            }
            if (this.mV.Columns != result.Count)
            {
                throw new NotConformableException("result", Resources.ParameterNotConformable);
            }
            int num = Math.Min(this.mRows, this.mColumns);
            double[] numArray = new double[this.mColumns];
            for (int i = 0; i < this.mColumns; i++)
            {
                num3 = 0.0;
                if (i < num)
                {
                    for (int k = 0; k < this.mRows; k++)
                    {
                        num3 += this.mU.ValueAt(k, i) * input[k];
                    }
                    num3 /= this.mS[i];
                }
                numArray[i] = num3;
            }
            for (int j = 0; j < this.mColumns; j++)
            {
                num3 = 0.0;
                for (int m = 0; m < this.mColumns; m++)
                {
                    num3 += this.mV.ValueAt(m, j) * numArray[m];
                }
                result[j] = num3;
            }
        }

        public Matrix U()
        {
            if (!this.mComputeVectors)
            {
                return null;
            }
            return this.mU.Clone();
        }

        public Matrix W()
        {
            int rows = this.mU.Rows;
            int columns = this.mV.Columns;
            Matrix matrix = this.mU.CreateMatrix(rows, columns);
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    if (i == j)
                    {
                        matrix.ValueAt(i, i, this.mS[i]);
                    }
                }
            }
            return matrix;
        }

        public Matrix VT()
        {
            if (!this.mComputeVectors)
            {
                return null;
            }
            return this.mV.Clone();
        }
    }

    public class Svd
    {
        // Fields
        private readonly int mColumns;
        private readonly int mRows;
        private readonly AbstractSvd mSvd;

        // Methods
        public Svd(Matrix matrix, bool computeVectors)
        {
            if (matrix == null)
            {
                throw new ArgumentNullException("matrix", Resources.NullParameterException);
            }
            this.mRows = matrix.Rows;
            this.mColumns = matrix.Columns;
            if (matrix.GetType() == typeof(DenseMatrix))
            {
                this.mSvd = new DenseSvd((DenseMatrix)matrix, computeVectors);
            }
            else
            {
                this.mSvd = new UserSvd(matrix, computeVectors);
            }
        }

        public double ConditionNumber()
        {
            if (!this.mSvd.Converged())
            {
                throw new ConvergenceFailedException();
            }
            return this.mSvd.ConditionNumber();
        }

        public bool Converged()
        {
            return this.mSvd.Converged();
        }

        public double Norm2()
        {
            if (!this.mSvd.Converged())
            {
                throw new ConvergenceFailedException();
            }
            return this.mSvd.Norm2();
        }

        public int Rank()
        {
            if (!this.mSvd.Converged())
            {
                throw new ConvergenceFailedException();
            }
            return this.mSvd.Rank();
        }

        public Vector S()
        {
            if (!this.mSvd.Converged())
            {
                throw new ConvergenceFailedException();
            }
            return this.mSvd.S();
        }

        public Matrix Solve(Matrix input)
        {
            return this.mSvd.Solve(input);
        }

        public Vector Solve(Vector input)
        {
            return this.mSvd.Solve(input);
        }

        public void Solve(Matrix input, Matrix result)
        {
            this.mSvd.Solve(input, result);
        }

        public void Solve(Vector input, Vector result)
        {
            this.mSvd.Solve(input, result);
        }

        public Matrix U()
        {
            if (!this.mSvd.Converged())
            {
                throw new ConvergenceFailedException();
            }
            return this.mSvd.U();
        }

        public Matrix W()
        {
            if (!this.mSvd.Converged())
            {
                throw new ConvergenceFailedException();
            }
            return this.mSvd.W();
        }

        public Matrix VT()
        {
            if (!this.mSvd.Converged())
            {
                throw new ConvergenceFailedException();
            }
            return this.mSvd.VT();
        }
    }
    internal class DenseSvd : AbstractSvd
    {
        // Fields
        private double[] mMatrix;

        // Methods
        public DenseSvd(DenseMatrix matrix, bool computeVectors)
            : base(computeVectors)
        {
            base.mRows = matrix.Rows;
            base.mColumns = matrix.Columns;
            this.mMatrix = new double[base.mRows * base.mColumns];
            Buffer.BlockCopy(matrix.Data, 0, this.mMatrix, 0, this.mMatrix.Length * 8);
        }

        private static double Ddot(double[] A, int mRows, int col1, int col2, int start)
        {
            double num = 0.0;
            for (int i = start; i < mRows; i++)
            {
                num += A[(col2 * mRows) + i] * A[(col1 * mRows) + i];
            }
            return num;
        }

        private bool Decompose()
        {
            int num7;
            int num8;
            int num10;
            double num11;
            int num13;
            double[] a = new double[base.mColumns];
            double[] numArray2 = new double[base.mRows];
            double c = 0.0;
            double s = 0.0;
            int mRows = base.mRows;
            int num4 = Math.Min(base.mRows - 1, base.mColumns);
            int num5 = Math.Max(0, Math.Min(base.mColumns - 2, base.mRows));
            int num6 = Math.Max(num4, num5);
            double[] data = null;
            double[] numArray4 = null;
            if (base.mComputeVectors)
            {
                data = ((DenseMatrix)base.mU).Data;
                numArray4 = ((DenseMatrix)base.mV).Data;
            }
            for (num7 = 0; num7 < num6; num7++)
            {
                num8 = num7 + 1;
                if (num7 < num4)
                {
                    double num9 = Dnrm2Column(this.mMatrix, base.mRows, num7, num7);
                    base.mS[num7] = num9;
                    if (base.mS[num7] != 0.0)
                    {
                        if (this.mMatrix[(num7 * base.mRows) + num7] != 0.0)
                        {
                            base.mS[num7] = Dsign(base.mS[num7], this.mMatrix[(num7 * base.mRows) + num7]);
                        }
                        DscalColumn(this.mMatrix, base.mRows, num7, num7, 1.0 / base.mS[num7]);
                        this.mMatrix[(num7 * base.mRows) + num7] = 1.0 + this.mMatrix[(num7 * base.mRows) + num7];
                    }
                    base.mS[num7] = -base.mS[num7];
                }
                num10 = num8;
                while (num10 < base.mColumns)
                {
                    if ((num7 < num4) && (base.mS[num7] != 0.0))
                    {
                        num11 = -Ddot(this.mMatrix, base.mRows, num7, num10, num7) / this.mMatrix[(num7 * base.mRows) + num7];
                        for (int i = num7; i < base.mRows; i++)
                        {
                            this.mMatrix[(num10 * base.mRows) + i] += num11 * this.mMatrix[(num7 * base.mRows) + i];
                        }
                    }
                    a[num10] = this.mMatrix[(num10 * base.mRows) + num7];
                    num10++;
                }
                if (base.mComputeVectors && (num7 < num4))
                {
                    num13 = num7;
                    while (num13 < base.mRows)
                    {
                        data[(num7 * base.mRows) + num13] = this.mMatrix[(num7 * base.mRows) + num13];
                        num13++;
                    }
                }
                if (num7 < num5)
                {
                    a[num7] = Dnrm2Vector(a, num8);
                    if (a[num7] != 0.0)
                    {
                        if (a[num8] != 0.0)
                        {
                            a[num7] = Dsign(a[num7], a[num8]);
                        }
                        DscalVector(a, num8, 1.0 / a[num7]);
                        a[num8] = 1.0 + a[num8];
                    }
                    a[num7] = -a[num7];
                    if ((num8 < base.mRows) && (a[num7] != 0.0))
                    {
                        num13 = num8;
                        while (num13 < base.mRows)
                        {
                            numArray2[num13] = 0.0;
                            num13++;
                        }
                        num10 = num8;
                        while (num10 < base.mColumns)
                        {
                            for (int j = num8; j < base.mRows; j++)
                            {
                                numArray2[j] += a[num10] * this.mMatrix[(num10 * base.mRows) + j];
                            }
                            num10++;
                        }
                        for (num10 = num8; num10 < base.mColumns; num10++)
                        {
                            double num16 = -a[num10] / a[num8];
                            for (int k = num8; k < base.mRows; k++)
                            {
                                this.mMatrix[(num10 * base.mRows) + k] += num16 * numArray2[k];
                            }
                        }
                    }
                    if (base.mComputeVectors)
                    {
                        num13 = num8;
                        while (num13 < base.mColumns)
                        {
                            numArray4[(num7 * base.mColumns) + num13] = a[num13];
                            num13++;
                        }
                    }
                }
            }
            int num18 = Math.Min(base.mColumns, base.mRows + 1);
            int num19 = num4 + 1;
            int num20 = num5 + 1;
            if (num4 < base.mColumns)
            {
                base.mS[num19 - 1] = this.mMatrix[((num19 - 1) * base.mRows) + (num19 - 1)];
            }
            if (base.mRows < num18)
            {
                base.mS[num18 - 1] = 0.0;
            }
            if (num20 < num18)
            {
                a[num20 - 1] = this.mMatrix[((num18 - 1) * base.mRows) + (num20 - 1)];
            }
            a[num18 - 1] = 0.0;
            if (base.mComputeVectors)
            {
                num10 = num19 - 1;
                while (num10 < mRows)
                {
                    num13 = 0;
                    while (num13 < base.mRows)
                    {
                        data[(num10 * base.mRows) + num13] = 0.0;
                        num13++;
                    }
                    data[(num10 * base.mRows) + num10] = 1.0;
                    num10++;
                }
                for (num7 = num4 - 1; num7 >= 0; num7--)
                {
                    if (base.mS[num7] != 0.0)
                    {
                        num10 = num7 + 1;
                        while (num10 < mRows)
                        {
                            num11 = -Ddot(data, base.mRows, num7, num10, num7) / data[(num7 * base.mRows) + num7];
                            for (int m = num7; m < base.mRows; m++)
                            {
                                data[(num10 * base.mRows) + m] += num11 * data[(num7 * base.mRows) + m];
                            }
                            num10++;
                        }
                        DscalColumn(data, base.mRows, num7, num7, -1.0);
                        data[(num7 * base.mRows) + num7] = 1.0 + data[(num7 * base.mRows) + num7];
                        num13 = 0;
                        while (num13 < num7)
                        {
                            data[(num7 * base.mRows) + num13] = 0.0;
                            num13++;
                        }
                    }
                    else
                    {
                        num13 = 0;
                        while (num13 < base.mRows)
                        {
                            data[(num7 * base.mRows) + num13] = 0.0;
                            num13++;
                        }
                        data[(num7 * base.mRows) + num7] = 1.0;
                    }
                }
            }
            if (base.mComputeVectors)
            {
                num7 = base.mColumns - 1;
                while (num7 >= 0)
                {
                    num8 = num7 + 1;
                    if ((num7 < num5) && (a[num7] != 0.0))
                    {
                        for (num10 = num8; num10 < base.mColumns; num10++)
                        {
                            num11 = -Ddot(numArray4, base.mColumns, num7, num10, num8) / numArray4[(num7 * base.mColumns) + num8];
                            for (int n = num7; n < base.mColumns; n++)
                            {
                                numArray4[(num10 * base.mColumns) + n] += num11 * numArray4[(num7 * base.mColumns) + n];
                            }
                        }
                    }
                    num13 = 0;
                    while (num13 < base.mColumns)
                    {
                        numArray4[(num7 * base.mColumns) + num13] = 0.0;
                        num13++;
                    }
                    numArray4[(num7 * base.mColumns) + num7] = 1.0;
                    num7--;
                }
            }
            for (num13 = 0; num13 < num18; num13++)
            {
                double num23;
                if (base.mS[num13] != 0.0)
                {
                    num11 = base.mS[num13];
                    num23 = base.mS[num13] / num11;
                    base.mS[num13] = num11;
                    if (num13 < (num18 - 1))
                    {
                        a[num13] /= num23;
                    }
                    if (base.mComputeVectors)
                    {
                        DscalColumn(data, base.mRows, num13, 0, num23);
                    }
                }
                if (num13 == (num18 - 1))
                {
                    break;
                }
                if (a[num13] != 0.0)
                {
                    num11 = a[num13];
                    num23 = num11 / a[num13];
                    a[num13] = num11;
                    base.mS[num13 + 1] *= num23;
                    if (base.mComputeVectors)
                    {
                        DscalColumn(numArray4, base.mColumns, num13 + 1, 0, num23);
                    }
                }
            }
            int num24 = num18;
            int num25 = 0;
            while (num18 > 0)
            {
                double num26;
                double num27;
                int num28;
                double num31;
                int num32;
                int num33;
                if (num25 >= 0x3e8)
                {
                    return false;
                }
                num7 = num18 - 2;
                while (num7 >= 0)
                {
                    num26 = Math.Abs(base.mS[num7]) + Math.Abs(base.mS[num7 + 1]);
                    num27 = num26 + Math.Abs(a[num7]);
                    if (Precision.EqualsWithinDecimalPlaces(num27, num26, 15))
                    {
                        a[num7] = 0.0;
                        break;
                    }
                    num7--;
                }
                if (num7 == (num18 - 2))
                {
                    num28 = 4;
                }
                else
                {
                    int index = num18 - 1;
                    while (index > num7)
                    {
                        num26 = 0.0;
                        if (index != (num18 - 1))
                        {
                            num26 += Math.Abs(a[index]);
                        }
                        if (index != (num7 + 1))
                        {
                            num26 += Math.Abs(a[index - 1]);
                        }
                        num27 = num26 + Math.Abs(base.mS[index]);
                        if (Precision.EqualsWithinDecimalPlaces(num27, num26, 15))
                        {
                            base.mS[index] = 0.0;
                            break;
                        }
                        index--;
                    }
                    if (index == num7)
                    {
                        num28 = 3;
                    }
                    else if (index == (num18 - 1))
                    {
                        num28 = 1;
                    }
                    else
                    {
                        num28 = 2;
                        num7 = index;
                    }
                }
                num7++;
                switch (num28)
                {
                    case 1:
                        num31 = a[num18 - 2];
                        a[num18 - 2] = 0.0;
                        num32 = num7;
                        goto Label_0B6C;

                    case 2:
                        num31 = a[num7 - 1];
                        a[num7 - 1] = 0.0;
                        num33 = num7;
                        goto Label_0C00;

                    case 3:
                        {
                            double num35 = 0.0;
                            num35 = Math.Max(Math.Max(Math.Max(Math.Max(Math.Max(num35, Math.Abs(base.mS[num18 - 1])), Math.Abs(base.mS[num18 - 2])), Math.Abs(a[num18 - 2])), Math.Abs(base.mS[num7])), Math.Abs(a[num7]));
                            double num36 = base.mS[num18 - 1] / num35;
                            double num37 = base.mS[num18 - 2] / num35;
                            double num38 = a[num18 - 2] / num35;
                            double num39 = base.mS[num7] / num35;
                            double num40 = a[num7] / num35;
                            double num41 = (((num37 + num36) * (num37 - num36)) + (num38 * num38)) / 2.0;
                            double num42 = (num36 * num38) * (num36 * num38);
                            double num43 = 0.0;
                            if ((num41 != 0.0) || (num42 != 0.0))
                            {
                                num43 = Math.Sqrt((num41 * num41) + num42);
                                if (num41 < 0.0)
                                {
                                    num43 = -num43;
                                }
                                num43 = num42 / (num41 + num43);
                            }
                            num31 = ((num39 + num36) * (num39 - num36)) + num43;
                            double db = num39 * num40;
                            for (num33 = num7; num33 < (num18 - 1); num33++)
                            {
                                Drotg(ref num31, ref db, ref c, ref s);
                                if (num33 != num7)
                                {
                                    a[num33 - 1] = num31;
                                }
                                num31 = (c * base.mS[num33]) + (s * a[num33]);
                                a[num33] = (c * a[num33]) - (s * base.mS[num33]);
                                db = s * base.mS[num33 + 1];
                                base.mS[num33 + 1] = c * base.mS[num33 + 1];
                                if (base.mComputeVectors)
                                {
                                    Drot(numArray4, base.mColumns, num33, num33 + 1, c, s);
                                }
                                Drotg(ref num31, ref db, ref c, ref s);
                                base.mS[num33] = num31;
                                num31 = (c * a[num33]) + (s * base.mS[num33 + 1]);
                                base.mS[num33 + 1] = (-s * a[num33]) + (c * base.mS[num33 + 1]);
                                db = s * a[num33 + 1];
                                a[num33 + 1] = c * a[num33 + 1];
                                if (base.mComputeVectors && (num33 < base.mRows))
                                {
                                    Drot(data, base.mRows, num33, num33 + 1, c, s);
                                }
                            }
                            a[num18 - 2] = num31;
                            num25++;
                            continue;
                        }
                    case 4:
                        if (base.mS[num7] < 0.0)
                        {
                            base.mS[num7] = -base.mS[num7];
                            if (base.mComputeVectors)
                            {
                                DscalColumn(numArray4, base.mColumns, num7, 0, -1.0);
                            }
                        }
                        goto Label_1004;

                    default:
                        {
                            continue;
                        }
                }
                Label_0AED:
                num33 = ((num18 - 2) - num32) + num7;
                double da = base.mS[num33];
                Drotg(ref da, ref num31, ref c, ref s);
                base.mS[num33] = da;
                if (num33 != num7)
                {
                    num31 = -s * a[num33 - 1];
                    a[num33 - 1] = c * a[num33 - 1];
                }
                if (base.mComputeVectors)
                {
                    Drot(numArray4, base.mColumns, num33, num18 - 1, c, s);
                }
                num32++;
                Label_0B6C:
                if (num32 < (num18 - 1))
                {
                    goto Label_0AED;
                }
                continue;
                Label_0B9C:
                da = base.mS[num33];
                Drotg(ref da, ref num31, ref c, ref s);
                base.mS[num33] = da;
                num31 = -s * a[num33];
                a[num33] = c * a[num33];
                if (base.mComputeVectors)
                {
                    Drot(data, base.mRows, num33, num7 - 1, c, s);
                }
                num33++;
                Label_0C00:
                if (num33 < num18)
                {
                    goto Label_0B9C;
                }
                continue;
                Label_0F4B:
                if (base.mS[num7] >= base.mS[num7 + 1])
                {
                    goto Label_100F;
                }
                num11 = base.mS[num7];
                base.mS[num7] = base.mS[num7 + 1];
                base.mS[num7 + 1] = num11;
                if (base.mComputeVectors && (num7 < base.mColumns))
                {
                    Dswap(numArray4, base.mColumns, num7, num7 + 1);
                }
                if (base.mComputeVectors && (num7 < base.mRows))
                {
                    Dswap(data, base.mRows, num7, num7 + 1);
                }
                num7++;
                Label_1004:
                if (num7 != (num24 - 1))
                {
                    goto Label_0F4B;
                }
                Label_100F:
                num25 = 0;
                num18--;
            }
            if (base.mComputeVectors)
            {
                base.mV = base.mV.Transpose();
            }
            return true;
        }

        private static double Dnrm2Column(double[] A, int mRows, int col, int start)
        {
            double d = 0.0;
            for (int i = start; i < mRows; i++)
            {
                d += A[(col * mRows) + i] * A[(col * mRows) + i];
            }
            return Math.Sqrt(d);
        }

        private static double Dnrm2Vector(double[] A, int start)
        {
            double d = 0.0;
            for (int i = start; i < A.Length; i++)
            {
                d += A[i] * A[i];
            }
            return Math.Sqrt(d);
        }

        protected override void DoCompute()
        {
            int size = Math.Min(base.mRows + 1, base.mColumns);
            base.mS = new DenseVector(size);
            if (base.mComputeVectors)
            {
                base.mU = new DenseMatrix(base.mRows, base.mRows);
                base.mV = new DenseMatrix(base.mColumns, base.mColumns);
            }
            base.mConverged = this.Decompose();
            if (base.mRows < base.mColumns)
            {
                size--;
                Vector vector = new DenseVector(size);
                for (int j = 0; j < size; j++)
                {
                    vector[j] = base.mS[j];
                }
                base.mS = vector;
            }
            double num3 = Math.Pow(2.0, -52.0);
            double num4 = (Math.Max(base.mRows, base.mColumns) * base.mS[0]) * num3;
            base.mRank = 0;
            for (int i = 0; i < size; i++)
            {
                if (base.mS[i] > num4)
                {
                    base.mRank++;
                }
            }
            this.mMatrix = null;
        }

        private static void Drot(double[] A, int mRows, int col1, int col2, double c, double s)
        {
            for (int i = 0; i < mRows; i++)
            {
                double num2 = (c * A[(col1 * mRows) + i]) + (s * A[(col2 * mRows) + i]);
                A[(col2 * mRows) + i] = (c * A[(col2 * mRows) + i]) - (s * A[(col1 * mRows) + i]);
                A[(col1 * mRows) + i] = num2;
            }
        }

        private static void Drotg(ref double da, ref double db, ref double c, ref double s)
        {
            double num5;
            double num6;
            double num = db;
            double num2 = Math.Abs(da);
            double num3 = Math.Abs(db);
            if (num2 > num3)
            {
                num = da;
            }
            double num4 = num2 + num3;
            if (num4 == 0.0)
            {
                c = 1.0;
                s = 0.0;
                num5 = 0.0;
                num6 = 0.0;
            }
            else
            {
                double num7 = da / num4;
                double num8 = db / num4;
                num5 = num4 * Math.Sqrt((num7 * num7) + (num8 * num8));
                if (num < 0.0)
                {
                    num5 = -num5;
                }
                c = da / num5;
                s = db / num5;
                num6 = 1.0;
                if (num2 > num3)
                {
                    num6 = s;
                }
                if ((num3 >= num2) && (c != 0.0))
                {
                    num6 = 1.0 / c;
                }
            }
            da = num5;
            db = num6;
        }

        private static void DscalColumn(double[] A, int mRows, int col, int start, double z)
        {
            for (int i = start; i < mRows; i++)
            {
                A[(col * mRows) + i] *= z;
            }
        }

        private static void DscalVector(double[] A, int start, double z)
        {
            for (int i = start; i < A.Length; i++)
            {
                A[i] *= z;
            }
        }

        private static double Dsign(double z1, double z2)
        {
            return (Math.Abs(z1) * (z2 / Math.Abs(z2)));
        }

        private static void Dswap(double[] A, int mRows, int col1, int col2)
        {
            for (int i = 0; i < mRows; i++)
            {
                double num2 = A[(col1 * mRows) + i];
                A[(col1 * mRows) + i] = A[(col2 * mRows) + i];
                A[(col2 * mRows) + i] = num2;
            }
        }
    }
    internal class UserSvd : AbstractSvd
    {
        // Fields
        private Matrix mMatrix;

        // Methods
        public UserSvd(Matrix matrix, bool mComputeVectors)
            : base(mComputeVectors)
        {
            base.mRows = matrix.Rows;
            base.mColumns = matrix.Columns;
            this.mMatrix = matrix.Clone();
        }

        private static double Ddot(Matrix A, int mRows, int col1, int col2, int start)
        {
            double num = 0.0;
            for (int i = start; i < mRows; i++)
            {
                num += A.ValueAt(i, col2) * A.ValueAt(i, col1);
            }
            return num;
        }

        private bool Decompose()
        {
            int num7;
            int num8;
            int num10;
            double num11;
            int num13;
            double[] a = new double[base.mColumns];
            double[] numArray2 = new double[base.mRows];
            double c = 0.0;
            double s = 0.0;
            int mRows = base.mRows;
            int num4 = Math.Min(base.mRows - 1, base.mColumns);
            int num5 = Math.Max(0, Math.Min(base.mColumns - 2, base.mRows));
            int num6 = Math.Max(num4, num5);
            for (num7 = 0; num7 < num6; num7++)
            {
                num8 = num7 + 1;
                if (num7 < num4)
                {
                    double num9 = Dnrm2Column(this.mMatrix, base.mRows, num7, num7);
                    base.mS[num7] = num9;
                    if (base.mS[num7] != 0.0)
                    {
                        if (this.mMatrix.ValueAt(num7, num7) != 0.0)
                        {
                            base.mS[num7] = Dsign(base.mS[num7], this.mMatrix.ValueAt(num7, num7));
                        }
                        DscalColumn(this.mMatrix, base.mRows, num7, num7, 1.0 / base.mS[num7]);
                        this.mMatrix.ValueAt(num7, num7, 1.0 + this.mMatrix.ValueAt(num7, num7));
                    }
                    base.mS[num7] = -base.mS[num7];
                }
                num10 = num8;
                while (num10 < base.mColumns)
                {
                    if ((num7 < num4) && (base.mS[num7] != 0.0))
                    {
                        num11 = -Ddot(this.mMatrix, base.mRows, num7, num10, num7) / this.mMatrix.ValueAt(num7, num7);
                        for (int i = num7; i < base.mRows; i++)
                        {
                            this.mMatrix.ValueAt(i, num10, this.mMatrix.ValueAt(i, num10) + (num11 * this.mMatrix.ValueAt(i, num7)));
                        }
                    }
                    a[num10] = this.mMatrix.ValueAt(num7, num10);
                    num10++;
                }
                if (base.mComputeVectors && (num7 < num4))
                {
                    num13 = num7;
                    while (num13 < base.mRows)
                    {
                        base.mU.ValueAt(num13, num7, this.mMatrix.ValueAt(num13, num7));
                        num13++;
                    }
                }
                if (num7 < num5)
                {
                    a[num7] = Dnrm2Vector(a, num8);
                    if (a[num7] != 0.0)
                    {
                        if (a[num8] != 0.0)
                        {
                            a[num7] = Dsign(a[num7], a[num8]);
                        }
                        DscalVector(a, num8, 1.0 / a[num7]);
                        a[num8] = 1.0 + a[num8];
                    }
                    a[num7] = -a[num7];
                    if ((num8 < base.mRows) && (a[num7] != 0.0))
                    {
                        num13 = num8;
                        while (num13 < base.mRows)
                        {
                            numArray2[num13] = 0.0;
                            num13++;
                        }
                        num10 = num8;
                        while (num10 < base.mColumns)
                        {
                            for (int j = num8; j < base.mRows; j++)
                            {
                                numArray2[j] += a[num10] * this.mMatrix.ValueAt(j, num10);
                            }
                            num10++;
                        }
                        for (num10 = num8; num10 < base.mColumns; num10++)
                        {
                            double num16 = -a[num10] / a[num8];
                            for (int k = num8; k < base.mRows; k++)
                            {
                                this.mMatrix.ValueAt(k, num10, this.mMatrix.ValueAt(k, num10) + (num16 * numArray2[k]));
                            }
                        }
                    }
                    if (base.mComputeVectors)
                    {
                        num13 = num8;
                        while (num13 < base.mColumns)
                        {
                            base.mV.ValueAt(num13, num7, a[num13]);
                            num13++;
                        }
                    }
                }
            }
            int num18 = Math.Min(base.mColumns, base.mRows + 1);
            int num19 = num4 + 1;
            int num20 = num5 + 1;
            if (num4 < base.mColumns)
            {
                base.mS[num19 - 1] = this.mMatrix.ValueAt(num19 - 1, num19 - 1);
            }
            if (base.mRows < num18)
            {
                base.mS[num18 - 1] = 0.0;
            }
            if (num20 < num18)
            {
                a[num20 - 1] = this.mMatrix.ValueAt(num20 - 1, num18 - 1);
            }
            a[num18 - 1] = 0.0;
            if (base.mComputeVectors)
            {
                num10 = num19 - 1;
                while (num10 < mRows)
                {
                    num13 = 0;
                    while (num13 < base.mRows)
                    {
                        base.mU.ValueAt(num13, num10, 0.0);
                        num13++;
                    }
                    base.mU.ValueAt(num10, num10, 1.0);
                    num10++;
                }
                for (num7 = num4 - 1; num7 >= 0; num7--)
                {
                    if (base.mS[num7] != 0.0)
                    {
                        num10 = num7 + 1;
                        while (num10 < mRows)
                        {
                            num11 = -Ddot(base.mU, base.mRows, num7, num10, num7) / base.mU.ValueAt(num7, num7);
                            for (int m = num7; m < base.mRows; m++)
                            {
                                base.mU.ValueAt(m, num10, base.mU.ValueAt(m, num10) + (num11 * base.mU.ValueAt(m, num7)));
                            }
                            num10++;
                        }
                        DscalColumn(base.mU, base.mRows, num7, num7, -1.0);
                        base.mU.ValueAt(num7, num7, 1.0 + base.mU.ValueAt(num7, num7));
                        num13 = 0;
                        while (num13 < num7)
                        {
                            base.mU.ValueAt(num13, num7, 0.0);
                            num13++;
                        }
                    }
                    else
                    {
                        num13 = 0;
                        while (num13 < base.mRows)
                        {
                            base.mU.ValueAt(num13, num7, 0.0);
                            num13++;
                        }
                        base.mU.ValueAt(num7, num7, 1.0);
                    }
                }
            }
            if (base.mComputeVectors)
            {
                num7 = base.mColumns - 1;
                while (num7 >= 0)
                {
                    num8 = num7 + 1;
                    if ((num7 < num5) && (a[num7] != 0.0))
                    {
                        for (num10 = num8; num10 < base.mColumns; num10++)
                        {
                            num11 = -Ddot(base.mV, base.mColumns, num7, num10, num8) / base.mV.ValueAt(num8, num7);
                            for (int n = num7; n < base.mColumns; n++)
                            {
                                base.mV.ValueAt(n, num10, base.mV.ValueAt(n, num10) + (num11 * base.mV.ValueAt(n, num7)));
                            }
                        }
                    }
                    num13 = 0;
                    while (num13 < base.mColumns)
                    {
                        base.mV.ValueAt(num13, num7, 0.0);
                        num13++;
                    }
                    base.mV.ValueAt(num7, num7, 1.0);
                    num7--;
                }
            }
            for (num13 = 0; num13 < num18; num13++)
            {
                double num23;
                if (base.mS[num13] != 0.0)
                {
                    num11 = base.mS[num13];
                    num23 = base.mS[num13] / num11;
                    base.mS[num13] = num11;
                    if (num13 < (num18 - 1))
                    {
                        a[num13] /= num23;
                    }
                    if (base.mComputeVectors)
                    {
                        DscalColumn(base.mU, base.mRows, num13, 0, num23);
                    }
                }
                if (num13 == (num18 - 1))
                {
                    break;
                }
                if (a[num13] != 0.0)
                {
                    num11 = a[num13];
                    num23 = num11 / a[num13];
                    a[num13] = num11;
                    base.mS[num13 + 1] *= num23;
                    if (base.mComputeVectors)
                    {
                        DscalColumn(base.mV, base.mColumns, num13 + 1, 0, num23);
                    }
                }
            }
            int num24 = num18;
            int num25 = 0;
            while (num18 > 0)
            {
                double num26;
                double num27;
                int num28;
                double num31;
                int num32;
                int num33;
                if (num25 >= 0x3e8)
                {
                    return false;
                }
                num7 = num18 - 2;
                while (num7 >= 0)
                {
                    num26 = Math.Abs(base.mS[num7]) + Math.Abs(base.mS[num7 + 1]);
                    num27 = num26 + Math.Abs(a[num7]);
                    if (Precision.EqualsWithinDecimalPlaces(num27, num26, 15))
                    {
                        a[num7] = 0.0;
                        break;
                    }
                    num7--;
                }
                if (num7 == (num18 - 2))
                {
                    num28 = 4;
                }
                else
                {
                    int index = num18 - 1;
                    while (index > num7)
                    {
                        num26 = 0.0;
                        if (index != (num18 - 1))
                        {
                            num26 += Math.Abs(a[index]);
                        }
                        if (index != (num7 + 1))
                        {
                            num26 += Math.Abs(a[index - 1]);
                        }
                        num27 = num26 + Math.Abs(base.mS[index]);
                        if (Precision.EqualsWithinDecimalPlaces(num27, num26, 15))
                        {
                            base.mS[index] = 0.0;
                            break;
                        }
                        index--;
                    }
                    if (index == num7)
                    {
                        num28 = 3;
                    }
                    else if (index == (num18 - 1))
                    {
                        num28 = 1;
                    }
                    else
                    {
                        num28 = 2;
                        num7 = index;
                    }
                }
                num7++;
                switch (num28)
                {
                    case 1:
                        num31 = a[num18 - 2];
                        a[num18 - 2] = 0.0;
                        num32 = num7;
                        goto Label_0B3E;

                    case 2:
                        num31 = a[num7 - 1];
                        a[num7 - 1] = 0.0;
                        num33 = num7;
                        goto Label_0BD6;

                    case 3:
                        {
                            double num35 = 0.0;
                            num35 = Math.Max(Math.Max(Math.Max(Math.Max(Math.Max(num35, Math.Abs(base.mS[num18 - 1])), Math.Abs(base.mS[num18 - 2])), Math.Abs(a[num18 - 2])), Math.Abs(base.mS[num7])), Math.Abs(a[num7]));
                            double num36 = base.mS[num18 - 1] / num35;
                            double num37 = base.mS[num18 - 2] / num35;
                            double num38 = a[num18 - 2] / num35;
                            double num39 = base.mS[num7] / num35;
                            double num40 = a[num7] / num35;
                            double num41 = (((num37 + num36) * (num37 - num36)) + (num38 * num38)) / 2.0;
                            double num42 = (num36 * num38) * (num36 * num38);
                            double num43 = 0.0;
                            if ((num41 != 0.0) || (num42 != 0.0))
                            {
                                num43 = Math.Sqrt((num41 * num41) + num42);
                                if (num41 < 0.0)
                                {
                                    num43 = -num43;
                                }
                                num43 = num42 / (num41 + num43);
                            }
                            num31 = ((num39 + num36) * (num39 - num36)) + num43;
                            double db = num39 * num40;
                            for (num33 = num7; num33 < (num18 - 1); num33++)
                            {
                                Drotg(ref num31, ref db, ref c, ref s);
                                if (num33 != num7)
                                {
                                    a[num33 - 1] = num31;
                                }
                                num31 = (c * base.mS[num33]) + (s * a[num33]);
                                a[num33] = (c * a[num33]) - (s * base.mS[num33]);
                                db = s * base.mS[num33 + 1];
                                base.mS[num33 + 1] = c * base.mS[num33 + 1];
                                if (base.mComputeVectors)
                                {
                                    Drot(base.mV, base.mColumns, num33, num33 + 1, c, s);
                                }
                                Drotg(ref num31, ref db, ref c, ref s);
                                base.mS[num33] = num31;
                                num31 = (c * a[num33]) + (s * base.mS[num33 + 1]);
                                base.mS[num33 + 1] = (-s * a[num33]) + (c * base.mS[num33 + 1]);
                                db = s * a[num33 + 1];
                                a[num33 + 1] = c * a[num33 + 1];
                                if (base.mComputeVectors && (num33 < base.mRows))
                                {
                                    Drot(base.mU, base.mRows, num33, num33 + 1, c, s);
                                }
                            }
                            a[num18 - 2] = num31;
                            num25++;
                            continue;
                        }
                    case 4:
                        if (base.mS[num7] < 0.0)
                        {
                            base.mS[num7] = -base.mS[num7];
                            if (base.mComputeVectors)
                            {
                                DscalColumn(base.mV, base.mColumns, num7, 0, -1.0);
                            }
                        }
                        goto Label_0FEE;

                    default:
                        {
                            continue;
                        }
                }
                Label_0ABB:
                num33 = ((num18 - 2) - num32) + num7;
                double da = base.mS[num33];
                Drotg(ref da, ref num31, ref c, ref s);
                base.mS[num33] = da;
                if (num33 != num7)
                {
                    num31 = -s * a[num33 - 1];
                    a[num33 - 1] = c * a[num33 - 1];
                }
                if (base.mComputeVectors)
                {
                    Drot(base.mV, base.mColumns, num33, num18 - 1, c, s);
                }
                num32++;
                Label_0B3E:
                if (num32 < (num18 - 1))
                {
                    goto Label_0ABB;
                }
                continue;
                Label_0B6E:
                da = base.mS[num33];
                Drotg(ref da, ref num31, ref c, ref s);
                base.mS[num33] = da;
                num31 = -s * a[num33];
                a[num33] = c * a[num33];
                if (base.mComputeVectors)
                {
                    Drot(base.mU, base.mRows, num33, num7 - 1, c, s);
                }
                num33++;
                Label_0BD6:
                if (num33 < num18)
                {
                    goto Label_0B6E;
                }
                continue;
                Label_0F2D:
                if (base.mS[num7] >= base.mS[num7 + 1])
                {
                    goto Label_0FF9;
                }
                num11 = base.mS[num7];
                base.mS[num7] = base.mS[num7 + 1];
                base.mS[num7 + 1] = num11;
                if (base.mComputeVectors && (num7 < base.mColumns))
                {
                    Dswap(base.mV, base.mColumns, num7, num7 + 1);
                }
                if (base.mComputeVectors && (num7 < base.mRows))
                {
                    Dswap(base.mU, base.mRows, num7, num7 + 1);
                }
                num7++;
                Label_0FEE:
                if (num7 != (num24 - 1))
                {
                    goto Label_0F2D;
                }
                Label_0FF9:
                num25 = 0;
                num18--;
            }
            if (base.mComputeVectors)
            {
                base.mV = base.mV.Transpose();
            }
            return true;
        }

        private static double Dnrm2Column(Matrix A, int mRows, int col, int start)
        {
            double d = 0.0;
            for (int i = start; i < mRows; i++)
            {
                d += A.ValueAt(i, col) * A.ValueAt(i, col);
            }
            return Math.Sqrt(d);
        }

        private static double Dnrm2Vector(double[] A, int start)
        {
            double d = 0.0;
            for (int i = start; i < A.Length; i++)
            {
                d += A[i] * A[i];
            }
            return Math.Sqrt(d);
        }

        protected override void DoCompute()
        {
            int size = Math.Min(base.mRows + 1, base.mColumns);
            base.mS = this.mMatrix.CreateVector(size);
            if (base.mComputeVectors)
            {
                base.mU = this.mMatrix.CreateMatrix(base.mRows, base.mRows);
                base.mV = this.mMatrix.CreateMatrix(base.mColumns, base.mColumns);
            }
            base.mConverged = this.Decompose();
            if (base.mRows < base.mColumns)
            {
                size--;
                Vector vector = this.mMatrix.CreateVector(size);
                for (int j = 0; j < size; j++)
                {
                    vector[j] = base.mS[j];
                }
                base.mS = vector;
            }
            double num3 = Math.Pow(2.0, -52.0);
            double num4 = (Math.Max(base.mRows, base.mColumns) * base.mS[0]) * num3;
            base.mRank = 0;
            for (int i = 0; i < size; i++)
            {
                if (base.mS[i] > num4)
                {
                    base.mRank++;
                }
            }
            this.mMatrix = null;
        }

        private static void Drot(Matrix A, int mRows, int col1, int col2, double c, double s)
        {
            for (int i = 0; i < mRows; i++)
            {
                double num2 = (c * A.ValueAt(i, col1)) + (s * A.ValueAt(i, col2));
                double num3 = (c * A.ValueAt(i, col2)) - (s * A.ValueAt(i, col1));
                A.ValueAt(i, col2, num3);
                A.ValueAt(i, col1, num2);
            }
        }

        private static void Drotg(ref double da, ref double db, ref double c, ref double s)
        {
            double num5;
            double num6;
            double num = db;
            double num2 = Math.Abs(da);
            double num3 = Math.Abs(db);
            if (num2 > num3)
            {
                num = da;
            }
            double num4 = num2 + num3;
            if (num4 == 0.0)
            {
                c = 1.0;
                s = 0.0;
                num5 = 0.0;
                num6 = 0.0;
            }
            else
            {
                double num7 = da / num4;
                double num8 = db / num4;
                num5 = num4 * Math.Sqrt((num7 * num7) + (num8 * num8));
                if (num < 0.0)
                {
                    num5 = -num5;
                }
                c = da / num5;
                s = db / num5;
                num6 = 1.0;
                if (num2 > num3)
                {
                    num6 = s;
                }
                if ((num3 >= num2) && (c != 0.0))
                {
                    num6 = 1.0 / c;
                }
            }
            da = num5;
            db = num6;
        }

        private static void DscalColumn(Matrix A, int mRows, int col, int start, double z)
        {
            for (int i = start; i < mRows; i++)
            {
                A.ValueAt(i, col, A.ValueAt(i, col) * z);
            }
        }

        private static void DscalVector(double[] A, int start, double z)
        {
            for (int i = start; i < A.Length; i++)
            {
                A[i] *= z;
            }
        }

        private static double Dsign(double z1, double z2)
        {
            return (Math.Abs(z1) * (z2 / Math.Abs(z2)));
        }

        private static void Dswap(Matrix A, int mRows, int col1, int col2)
        {
            for (int i = 0; i < mRows; i++)
            {
                double num2 = A.ValueAt(i, col1);
                A.ValueAt(i, col1, A.ValueAt(i, col2));
                A.ValueAt(i, col2, num2);
            }
        }
    }
}