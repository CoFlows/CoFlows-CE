C# Polyglot
===

    using QuantApp.Kernel;
    using System.Collections.Generic;
    using QuantApp.Kernel.JVM;

    public class CsPolyglot
    {   
        public static object Python()
        {
            string res = "";
            using(Py.GIL())
            {
                dynamic np = Py.Import("pybase");
                dynamic rr = np.pybase("Blue-", 100, true);

                res = rr.Color + rr.func(2);
            }
            
            return res;
        }
        
        public static object JavaTest()
        {
            var entry = JVM.Runtime.CreateInstance("javabase.JVMEntry", 1, 2, 3);
            dynamic cc = entry;
            return ls;
        }
    }

