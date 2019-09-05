using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace Python.Runtime
{
    public static class DelegateShim2
    {
        public static Delegate CreateDelegate(Type dtype, object obj, string methodName)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            var methods = obj.GetType().GetTypeInfo().GetMember(methodName, BindingFlags.Default);
            //if (!methods.Any())
            if (methods == null || methods.Length == 0)
            {
                throw new InvalidOperationException("Method does not exist");
            }

            return CreateDelegate(dtype, (MethodInfo)methods[0]);
        }

        internal static Delegate CreateDelegate(Type dtype, MethodInfo method)
        {
            return method.CreateDelegate(dtype);
        }
    }
}
