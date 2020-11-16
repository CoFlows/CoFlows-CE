using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace AQI.AQILabs.Kernel.Numerics.Math
{
    [Serializable]
    public class ConvergenceFailedException : AQITimeSeriesMathException
    {
        // Methods
        public ConvergenceFailedException()
        {
        }

        public ConvergenceFailedException(string message)
            : base(message)
        {
        }

        protected ConvergenceFailedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public ConvergenceFailedException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    [Serializable]
    public abstract class AQITimeSeriesMathException : Exception
    {
        // Methods
        protected AQITimeSeriesMathException()
        {
        }

        protected AQITimeSeriesMathException(string message)
            : base(message)
        {
        }

        protected AQITimeSeriesMathException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        protected AQITimeSeriesMathException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    [Serializable]
    public abstract class AQITimeSeriesMathArgumentException : AQITimeSeriesMathException
    {
        // Fields
        private readonly string _parameter;

        // Methods
        protected AQITimeSeriesMathArgumentException()
        {
        }

        protected AQITimeSeriesMathArgumentException(string message)
            : base(message)
        {
        }

        protected AQITimeSeriesMathArgumentException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        protected AQITimeSeriesMathArgumentException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected AQITimeSeriesMathArgumentException(string message, string parameter)
            : base(message)
        {
            this._parameter = parameter;
        }

        protected AQITimeSeriesMathArgumentException(string message, string parameter, Exception inner)
            : base(message, inner)
        {
            this._parameter = parameter;
        }

        //[SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        //public override void GetObjectData(SerializationInfo info, StreamingContext context)
        //{
        //    if (info == null)
        //    {
        //        throw new ArgumentNullException("info");
        //    }
        //    info.AddValue("parameter", this._parameter);
        //    base.GetObjectData(info, context);
        //}

        // Properties
        public override string Message
        {
            get
            {
                if (string.IsNullOrEmpty(this._parameter))
                {
                    return base.Message;
                }
                return (this._parameter + " : " + base.Message);
            }
        }

        public virtual string Parameter
        {
            get
            {
                return this._parameter;
            }
        }
    }

    [Serializable]
    public sealed class NotConformableException : AQITimeSeriesMathArgumentException
    {
        // Methods
        public NotConformableException()
        {
        }

        public NotConformableException(string message)
            : base(message)
        {
        }

        private NotConformableException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public NotConformableException(string message, Exception inner)
            : base(message, inner)
        {
        }

        public NotConformableException(string parameter, string message)
            : base(message, parameter)
        {
        }

        public NotConformableException(string parameter, string message, Exception inner)
            : base(message, parameter, inner)
        {
        }
    }
}