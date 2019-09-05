M Set
===

The **M** set is a persistent and distributed list in the **QuantApp.Kernel** environment. **M** can handle any object serialisable into JSON and ensures the objects in the set are replicated across all it's subscribers.

Data can be queried through LINQ like predicates. A few examples follow:

## C# Example
    using QuantApp.Kernel;

    var m = M.Base("ID");
    m += new { x1 = 1, x2 = 2, x3 = 3 }; // Add new dynamic structure
    m.Add(new { x1 = 2, x2 = 2, x3 = 3 }); // Add new dynamic structure
    
    var res = m[x => M.V<int>(x, "x1") >= 1 || M.V<string>(x, "x1") == "1" ]; // Query

    foreach (var v in res) Console.WriteLine("found: " + v + " " + M.V<int>(v, "x2"));


## F# Example
    open QuantApp.Kernel

    let m = ID |> M.Base
    {| x1 = 1; x2 = 2; x3 = 3 |} |> m.Add
    {| x1 = 2; x2 = 2; x3 = 3 |} |> m.Add
    
    let res = m.[fun x -> M.V<int>(x, "x1") >= 1 || M.V<string>(x, "x1") = "1" ] // Query

    res |> Seq.iter(fun v -> "found: " + v.ToString() + " " + M.V<int>(v, "x2").ToString() |> Console.WriteLine)


## Python Example
    import clr
    import QuantApp.Kernel as qak
    import System
    
    m = qak.M.Base("ID")
    
    m.Add(json.dumps({ "x1": 1, "x2": 2, "x3": 3 }))
    m.Add(json.dumps({ "x1": 2, "x2": 2, "x3": 3 }))

    def lam(x):
        x = json.loads(str(x))
        return x["x1"] >= 1 or x["x2"] == '1'

    res = [json.loads(x) for x in m[System.Func[System.Object, System.Boolean](lam)]] # Query

    for v in res:
        System.Console.WriteLine("found: " + str(v) + " " + str(v["x2"]))
    
    return res


## Scala Example
    import app.quant.clr._
    import app.quant.clr.scala.{SCLRObject => CLR}

    val M = CLR("QuantApp.Kernel.M")
    val m = M.Base[CLR]("ID-scala" , null)
    
    m.Add(new JVMEntry(1, 2, 3))
    m.Add(new JVMEntry(2, 2, 3))

    val res = m.Query[Iterable[AnyRef]](CLR.Func[AnyRef, Boolean](y => { 
        val x = y.asInstanceOf[JVMEntry]
        x.x1 >= 1 || x.x2 == "1"
    }))
    .map(x => x.asInstanceOf[JVMEntry])

    
    res.foreach(v => {
        println("found: " + v + " " + v.x2)
    })


## Javascript Example
    let log = System.Console.WriteLine
    var qkernel = importNamespace('QuantApp.Kernel')

    var m = qkernel.M.Base(ID, null)

    m.Add({ x1 : 1, x2 : 2, x3 : 3 })
    m.Add({ x1 : 1, x2 : 2, x3 : 3 })

    var res = m.Query( jsWrapper.Predicate(function (x) { 
                return x.x1 >= 1 || x.x2 == "1"
            }))

    res.forEach(function(v){
      log('found: ' + v + ' ' + v.x2)  
    })
    

## Java Example
    import app.quant.clr.*;

    public class JavaEntry
    {
        public int x1;
        public int x2;
        public int x3;

        public JavaEntry(int x1, int x2, int x3)
        {
            x1 = x1;
            x2 = x2;
            x3 = x3;
        }
    }

    CLRObject M = CLRRuntime.GetClass("QuantApp.Kernel.M");
    CLRObject m = (CLRObject)M.Invoke("Base", ID, null);

    m.Add(new JavaEntry(1, 2, 3));
    m.Add(new JavaEntry(1, 2, 3));

    Iterable res = (Iterable)m.Invoke("Query", CLRRuntime.CreateDelegate("System.Func`2[System.Object,System.Boolean]", (qx) -> { 
        JavaEntry x = (JavaEntry)qx[0];
        return x.x1 >= 1 || x.x2 == "1";
    }));

    for (JavaEntry v : res) 
        System.out.println("found: " + v + " " + v.x2)


## VB Example
    Imports QuantApp.Kernel

    Public Class Entry
        Public x1 As Integer  
        Public x2 As Integer  
        Public x3 As Integer  
    End Class 

    Dim m As M = M.Base(ID)

    Dim entry1 As Entry = New Entry()
    entry1.x1 = 1
    entry1.x2 = 2
    entry1.x3 = 3

    Dim entry2 As Entry = New Entry()
    entry2.x1 = 1
    entry2.x2 = 2
    entry2.x3 = 3

    m.Add(entry1)
    m.Add(entry2)

    Dim func = Function(x As Entry)
                    Return x.x1 >= 1 Or x2 == "1"
                End Function
    Dim res As System.Collections.Generic.List(Of object) = m(func) 'Query

    For Each v As Object In res
        Console.WriteLine("found: " + v + " " + v.x2)
    Next

                
