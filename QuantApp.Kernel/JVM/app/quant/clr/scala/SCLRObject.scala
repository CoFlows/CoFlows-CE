/*
 * The MIT License (MIT)
 * Copyright (c) Arturo Rodriguez All rights reserved.
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

package app.quant.clr.scala

import scala.language.dynamics
import scala.language.implicitConversions
import collection.mutable

import java.lang.reflect.Type
import scala.reflect._

import app.quant.clr._

import collection.JavaConverters._

class SCLRObject(val clrObject : CLRObject) extends Dynamic with mutable.Map[String, Any] {
    lazy val fields = mutable.Map.empty[String, Any]

    if(clrObject != null){
        CLRRuntime.SetID(this, clrObject.Pointer)
        val runtime = CLRRuntime.GetClass("QuantApp.Kernel.JVM.Runtime")
        var _sig = runtime.Invoke("Signature", clrObject)
        
        // println("SCALA(" + clrObject.hashCode()  + "): " + clrObject + " <--> " + _sig)
        // val sig = runtime.Invoke("Signature", clrObject).asInstanceOf[Array[String]]
        
        if(_sig != null) {
            val sig = _sig.asInstanceOf[Array[String]]
            if(sig != null) {
                sig.foreach(signature => {
                
                    if(signature.startsWith("M/") || signature.startsWith("S-M/")){
                        val isStatic = signature.startsWith("S-")
                        var name = signature.replaceAll("M/","").replaceAll("S-","")
                        name = name.substring(0, name.indexOf("-"))
                        val argsSignature = signature.substring(signature.indexOf("(") + 1, signature.lastIndexOf(")"))
                        val returnSignature = signature.substring(signature.indexOf(")") + 1)

                        // println("SCALA: " + name + argsSignature)

                        updateDynamic(name + argsSignature)((x:Array[Any]) => { 
                            val args = 
                                x.map(_ match { 
                                    case null => null
                                    case o: CLRObject => o.asInstanceOf[CLRObject]
                                    case o: AnyRef => o.asInstanceOf[Object]
                                })

                            clrObject.InvokeArr(name, args)
                        })
                    }
                })
            }
            else {
                println("JVM SCLRObject sig null")
            }
        }
        else {
            println("JVM SCLRObject _sig null")
        }
    }
  
    def CLRObject : CLRObject = clrObject

    // override def hashCode() : Int = clrObject.hashCode()
    override def hashCode() : Int = {
        // println("SCALA HASHCODE: " + clrObject.Pointer )//+ " " + CLRRuntime.GetID(clrObject) + " " + CLRRuntime.GetID(this))
        // CLRRuntime.GetID(clrObject)
        clrObject.Pointer
    }
    // override def hashCode() : Int = clrObject.Pointer

    def -=(k: String): this.type = { fields -= k; this }

    def +=(f: (String, Any)): this.type = { fields += f; this }

    def iterator = fields.iterator

    def get(k: String): Option[Any] = fields get k 

    val lockApply = 0
    // def applyDynamic[R >: Null <: Any](namep: String)(args: Any*): R = {    
    def applyDynamic[R <: Any](namep: String)(args: Any*): R = this.lockApply.synchronized {
        val argSig = args.map(x => if(x == null) null else x.getClass).map(CLRRuntime.TransformType(_)).map(x => x.replaceAll("app/quant/clr/CLRObject", "java/lang/Object").replaceAll("app/quant/clr/scala/SCLRObject", "java/lang/Object")).mkString

        // println("SCALA APPLYDYNAMIC: " + namep)
        
        val name = namep + argSig
        val func = this.getOrElse(name, null)

        try {
            val res = 
                if(func != null){
                    val res = func.asInstanceOf[Function1[Array[Any],Any]](args.map(_.asInstanceOf[Any]).toArray)

                    val ares = 
                        if(res.isInstanceOf[app.quant.clr.CLRIterable])
                            res.asInstanceOf[app.quant.clr.CLRIterable].asScala
                        
                        else 
                            res
                        
                    ares.asInstanceOf[R]
                }
                else {
                    val vargs = 
                        args.map(_ match { 
                            case null => null
                            case o: CLRObject => o.asInstanceOf[CLRObject]
                            case o: AnyRef => o.asInstanceOf[AnyRef]
                        }).toArray
                    
                    val res = clrObject.InvokeArr(namep, vargs )
                    
                    val ares = 
                        if(res.isInstanceOf[app.quant.clr.CLRIterable])
                            res.asInstanceOf[app.quant.clr.CLRIterable].asScala 
                        else 
                            res
                    ares.asInstanceOf[R]
                }

            if(res.isInstanceOf[CLRObject]) 
                (new SCLRObject(res.asInstanceOf[CLRObject])).asInstanceOf[R] 
            else 
                res
        }
        catch {
            case e : Exception => {
                println("ERROR: " + namep + " " + argSig)
                e.printStackTrace(System.out)
                None.asInstanceOf[R] 
            }
        }
    }

    def selectDynamic[R <: Any](name: String): R = {
    // def selectDynamic[R >: Null <: Any](name: String): R = {
        try {
            val res = clrObject.GetProperty(name)
 
            val ares = 
                if(res.isInstanceOf[app.quant.clr.CLRIterable]) {
                    res.asInstanceOf[app.quant.clr.CLRIterable].asScala 
                }
                else {
                    res
                }
            
            if(ares.isInstanceOf[CLRObject]) {
                (new SCLRObject(ares.asInstanceOf[CLRObject])).asInstanceOf[R] 
            }
            else {
                ares.asInstanceOf[R] 
            }
        }
        catch {
            case e : Exception => {
                println("ERROR: " + name)
                e.printStackTrace(System.out)
                None.asInstanceOf[R] 
            }
        }
    }

    def updateDynamic(name: String)(value: Any) { fields += name -> value }

        class Assigner(ob: SCLRObject, message: String) {
        def :=(value: Any): Unit = ob += (message -> value)
    }

    implicit def anyToassigner(a: Any): Assigner = a match {
        case x: Assigner => x
        case _ => sys.error("Not an assigner.")
    }
}

object SCLRObject {
    def apply(name : String, args : Any*) = { 
        // println("SCALA APPLY: " + name + " " + this.hashCode())
        val vargs = 
            args.map(_ match { 
              case null => null
              case o: CLRObject => o.asInstanceOf[CLRObject]
              case o: AnyRef => o.asInstanceOf[Object]
            }).toArray
      new SCLRObject(CLRRuntime.CreateInstanceArr(name, vargs))
    }
    def apply(obj: AnyRef) = new SCLRObject(obj.asInstanceOf[CLRObject])

    def PyImport(name : String) : SCLRObject = new SCLRObject(CLRRuntime.GetClass("Python.Runtime.Py").Invoke("Import",name).asInstanceOf[CLRObject])
    
    def Func[R : ClassTag](func : () => R) = {
        val sr = CLRRuntime.TransformNetType(classTag[R].runtimeClass)
        CLRRuntime.CreateDelegate("System.Func`1[" + sr + "]", (qx : Array[AnyRef]) => { func() }.asInstanceOf[AnyRef])
    }

    def Func[P0 : ClassTag, R : ClassTag](func : (P0) => R) = {
        val sp0 = CLRRuntime.TransformNetType(classTag[P0].runtimeClass)
        val sr = CLRRuntime.TransformNetType(classTag[R].runtimeClass)
        CLRRuntime.CreateDelegate("System.Func`2[" + sp0 + "," + sr + "]", (qx : Array[AnyRef]) => { func(qx(0).asInstanceOf[P0]) }.asInstanceOf[AnyRef])
    }

    def Func[P0 : ClassTag, P1 : ClassTag, R : ClassTag](func : (P0, P1) => R) = {
        val sp0 = CLRRuntime.TransformNetType(classTag[P0].runtimeClass)
        val sp1 = CLRRuntime.TransformNetType(classTag[P1].runtimeClass)
        val sr = CLRRuntime.TransformNetType(classTag[R].runtimeClass)
        CLRRuntime.CreateDelegate("System.Func`3[" + sp0 + "," + sp1 + "," + sr + "]", (qx : Array[AnyRef]) => { func(qx(0).asInstanceOf[P0], qx(1).asInstanceOf[P1]) }.asInstanceOf[AnyRef])
    }

    def Func[P0 : ClassTag, P1 : ClassTag, P2 : ClassTag, R : ClassTag](func : (P0, P1, P2) => R) = {
        val sp0 = CLRRuntime.TransformNetType(classTag[P0].runtimeClass)
        val sp1 = CLRRuntime.TransformNetType(classTag[P1].runtimeClass)
        val sp2 = CLRRuntime.TransformNetType(classTag[P2].runtimeClass)
        val sr = CLRRuntime.TransformNetType(classTag[R].runtimeClass)
        CLRRuntime.CreateDelegate("System.Func`4[" + sp0 + "," + sp1 + "," + sp2 + "," + sr + "]", (qx : Array[AnyRef]) => { func(qx(0).asInstanceOf[P0], qx(1).asInstanceOf[P1], qx(2).asInstanceOf[P2]) }.asInstanceOf[AnyRef])
    }

    def Func[P0 : ClassTag, P1 : ClassTag, P2 : ClassTag, P3 : ClassTag, R : ClassTag](func : (P0, P1, P2, P3) => R) = {
        val sp0 = CLRRuntime.TransformNetType(classTag[P0].runtimeClass)
        val sp1 = CLRRuntime.TransformNetType(classTag[P1].runtimeClass)
        val sp2 = CLRRuntime.TransformNetType(classTag[P2].runtimeClass)
        val sp3 = CLRRuntime.TransformNetType(classTag[P3].runtimeClass)
        val sr = CLRRuntime.TransformNetType(classTag[R].runtimeClass)
        CLRRuntime.CreateDelegate("System.Func`5[" + sp0 + "," + sp1 + "," + sp2 + "," + sp3 + "," + sr + "]", (qx : Array[AnyRef]) => { func(qx(0).asInstanceOf[P0], qx(1).asInstanceOf[P1], qx(2).asInstanceOf[P2], qx(3).asInstanceOf[P3]) }.asInstanceOf[AnyRef])
    }

    def Func[P0 : ClassTag, P1 : ClassTag, P2 : ClassTag, P3 : ClassTag, P4 : ClassTag, R : ClassTag](func : (P0, P1, P2, P3, P4) => R) = {
        val sp0 = CLRRuntime.TransformNetType(classTag[P0].runtimeClass)
        val sp1 = CLRRuntime.TransformNetType(classTag[P1].runtimeClass)
        val sp2 = CLRRuntime.TransformNetType(classTag[P2].runtimeClass)
        val sp3 = CLRRuntime.TransformNetType(classTag[P3].runtimeClass)
        val sp4 = CLRRuntime.TransformNetType(classTag[P4].runtimeClass)
        val sr = CLRRuntime.TransformNetType(classTag[R].runtimeClass)
        CLRRuntime.CreateDelegate("System.Func`6[" + sp0 + "," + sp1 + "," + sp2 + "," + sp3 + "," + sp4 + "," + sr + "]", (qx : Array[AnyRef]) => { func(qx(0).asInstanceOf[P0], qx(1).asInstanceOf[P1], qx(2).asInstanceOf[P2], qx(3).asInstanceOf[P3], qx(4).asInstanceOf[P4]) }.asInstanceOf[AnyRef])
    }

    def Func[P0 : ClassTag, P1 : ClassTag, P2 : ClassTag, P3 : ClassTag, P4 : ClassTag, P5 : ClassTag, R : ClassTag](func : (P0, P1, P2, P3, P4, P5) => R) = {
        val sp0 = CLRRuntime.TransformNetType(classTag[P0].runtimeClass)
        val sp1 = CLRRuntime.TransformNetType(classTag[P1].runtimeClass)
        val sp2 = CLRRuntime.TransformNetType(classTag[P2].runtimeClass)
        val sp3 = CLRRuntime.TransformNetType(classTag[P3].runtimeClass)
        val sp4 = CLRRuntime.TransformNetType(classTag[P4].runtimeClass)
        val sp5 = CLRRuntime.TransformNetType(classTag[P5].runtimeClass)
        val sr = CLRRuntime.TransformNetType(classTag[R].runtimeClass)
        CLRRuntime.CreateDelegate("System.Func`7[" + sp0 + "," + sp1 + "," + sp2 + "," + sp3 + "," + sp4 + "," + sp5 + "," + sr + "]", (qx : Array[AnyRef]) => { func(qx(0).asInstanceOf[P0], qx(1).asInstanceOf[P1], qx(2).asInstanceOf[P2], qx(3).asInstanceOf[P3], qx(4).asInstanceOf[P4], qx(5).asInstanceOf[P5]) }.asInstanceOf[AnyRef])
    }

    def Func[P0 : ClassTag, P1 : ClassTag, P2 : ClassTag, P3 : ClassTag, P4 : ClassTag, P5 : ClassTag, P6 : ClassTag, R : ClassTag](func : (P0, P1, P2, P3, P4, P5, P6) => R) = {
        val sp0 = CLRRuntime.TransformNetType(classTag[P0].runtimeClass)
        val sp1 = CLRRuntime.TransformNetType(classTag[P1].runtimeClass)
        val sp2 = CLRRuntime.TransformNetType(classTag[P2].runtimeClass)
        val sp3 = CLRRuntime.TransformNetType(classTag[P3].runtimeClass)
        val sp4 = CLRRuntime.TransformNetType(classTag[P4].runtimeClass)
        val sp5 = CLRRuntime.TransformNetType(classTag[P5].runtimeClass)
        val sp6 = CLRRuntime.TransformNetType(classTag[P6].runtimeClass)
        val sr = CLRRuntime.TransformNetType(classTag[R].runtimeClass)
        CLRRuntime.CreateDelegate("System.Func`8[" + sp0 + "," + sp1 + "," + sp2 + "," + sp3 + "," + sp4 + "," + sp5 + "," + sp6 + "," + sr + "]", (qx : Array[AnyRef]) => { func(qx(0).asInstanceOf[P0], qx(1).asInstanceOf[P1], qx(2).asInstanceOf[P2], qx(3).asInstanceOf[P3], qx(4).asInstanceOf[P4], qx(5).asInstanceOf[P5], qx(5).asInstanceOf[P6]) }.asInstanceOf[AnyRef])
    }

    def Func[P0 : ClassTag, P1 : ClassTag, P2 : ClassTag, P3 : ClassTag, P4 : ClassTag, P5 : ClassTag, P6 : ClassTag, P7 : ClassTag, R : ClassTag](func : (P0, P1, P2, P3, P4, P5, P6, P7) => R) = {
        val sp0 = CLRRuntime.TransformNetType(classTag[P0].runtimeClass)
        val sp1 = CLRRuntime.TransformNetType(classTag[P1].runtimeClass)
        val sp2 = CLRRuntime.TransformNetType(classTag[P2].runtimeClass)
        val sp3 = CLRRuntime.TransformNetType(classTag[P3].runtimeClass)
        val sp4 = CLRRuntime.TransformNetType(classTag[P4].runtimeClass)
        val sp5 = CLRRuntime.TransformNetType(classTag[P5].runtimeClass)
        val sp6 = CLRRuntime.TransformNetType(classTag[P6].runtimeClass)
        val sp7 = CLRRuntime.TransformNetType(classTag[P7].runtimeClass)
        val sr = CLRRuntime.TransformNetType(classTag[R].runtimeClass)
        CLRRuntime.CreateDelegate("System.Func`9[" + sp0 + "," + sp1 + "," + sp2 + "," + sp3 + "," + sp4 + "," + sp5 + "," + sp6 + "," + sp7 + "," + sr + "]", (qx : Array[AnyRef]) => { func(qx(0).asInstanceOf[P0], qx(1).asInstanceOf[P1], qx(2).asInstanceOf[P2], qx(3).asInstanceOf[P3], qx(4).asInstanceOf[P4], qx(5).asInstanceOf[P5], qx(6).asInstanceOf[P6], qx(7).asInstanceOf[P7]) }.asInstanceOf[AnyRef])
    }

    def Func[P0 : ClassTag, P1 : ClassTag, P2 : ClassTag, P3 : ClassTag, P4 : ClassTag, P5 : ClassTag, P6 : ClassTag, P7 : ClassTag, P8 : ClassTag, R : ClassTag](func : (P0, P1, P2, P3, P4, P5, P6, P7, P8) => R) = {
        val sp0 = CLRRuntime.TransformNetType(classTag[P0].runtimeClass)
        val sp1 = CLRRuntime.TransformNetType(classTag[P1].runtimeClass)
        val sp2 = CLRRuntime.TransformNetType(classTag[P2].runtimeClass)
        val sp3 = CLRRuntime.TransformNetType(classTag[P3].runtimeClass)
        val sp4 = CLRRuntime.TransformNetType(classTag[P4].runtimeClass)
        val sp5 = CLRRuntime.TransformNetType(classTag[P5].runtimeClass)
        val sp6 = CLRRuntime.TransformNetType(classTag[P6].runtimeClass)
        val sp7 = CLRRuntime.TransformNetType(classTag[P7].runtimeClass)
        val sp8 = CLRRuntime.TransformNetType(classTag[P8].runtimeClass)
        val sr = CLRRuntime.TransformNetType(classTag[R].runtimeClass)
        CLRRuntime.CreateDelegate("System.Func`10[" + sp0 + "," + sp1 + "," + sp2 + "," + sp3 + "," + sp4 + "," + sp5 + "," + sp6 + "," + sp7 + "," + sp8 + "," + sr + "]", (qx : Array[AnyRef]) => { func(qx(0).asInstanceOf[P0], qx(1).asInstanceOf[P1], qx(2).asInstanceOf[P2], qx(3).asInstanceOf[P3], qx(4).asInstanceOf[P4], qx(5).asInstanceOf[P5], qx(6).asInstanceOf[P6], qx(7).asInstanceOf[P7], qx(8).asInstanceOf[P8]) }.asInstanceOf[AnyRef])
    }

    def Func[P0 : ClassTag, P1 : ClassTag, P2 : ClassTag, P3 : ClassTag, P4 : ClassTag, P5 : ClassTag, P6 : ClassTag, P7 : ClassTag, P8 : ClassTag, P9 : ClassTag, R : ClassTag](func : (P0, P1, P2, P3, P4, P5, P6, P7, P8, P9) => R) = {
        val sp0 = CLRRuntime.TransformNetType(classTag[P0].runtimeClass)
        val sp1 = CLRRuntime.TransformNetType(classTag[P1].runtimeClass)
        val sp2 = CLRRuntime.TransformNetType(classTag[P2].runtimeClass)
        val sp3 = CLRRuntime.TransformNetType(classTag[P3].runtimeClass)
        val sp4 = CLRRuntime.TransformNetType(classTag[P4].runtimeClass)
        val sp5 = CLRRuntime.TransformNetType(classTag[P5].runtimeClass)
        val sp6 = CLRRuntime.TransformNetType(classTag[P6].runtimeClass)
        val sp7 = CLRRuntime.TransformNetType(classTag[P7].runtimeClass)
        val sp8 = CLRRuntime.TransformNetType(classTag[P8].runtimeClass)
        val sp9 = CLRRuntime.TransformNetType(classTag[P9].runtimeClass)
        val sr = CLRRuntime.TransformNetType(classTag[R].runtimeClass)
        CLRRuntime.CreateDelegate("System.Func`11[" + sp0 + "," + sp1 + "," + sp2 + "," + sp3 + "," + sp4 + "," + sp5 + "," + sp6 + "," + sp7 + "," + sp8 + "," + sp9 + "," + sr + "]", (qx : Array[AnyRef]) => { func(qx(0).asInstanceOf[P0], qx(1).asInstanceOf[P1], qx(2).asInstanceOf[P2], qx(3).asInstanceOf[P3], qx(4).asInstanceOf[P4], qx(5).asInstanceOf[P5], qx(6).asInstanceOf[P6], qx(7).asInstanceOf[P7], qx(8).asInstanceOf[P8], qx(9).asInstanceOf[P9]) }.asInstanceOf[AnyRef])
    }

    def Func[P0 : ClassTag, P1 : ClassTag, P2 : ClassTag, P3 : ClassTag, P4 : ClassTag, P5 : ClassTag, P6 : ClassTag, P7 : ClassTag, P8 : ClassTag, P9 : ClassTag, P10 : ClassTag, R : ClassTag](func : (P0, P1, P2, P3, P4, P5, P6, P7, P8, P9, P10) => R) = {
        val sp0 = CLRRuntime.TransformNetType(classTag[P0].runtimeClass)
        val sp1 = CLRRuntime.TransformNetType(classTag[P1].runtimeClass)
        val sp2 = CLRRuntime.TransformNetType(classTag[P2].runtimeClass)
        val sp3 = CLRRuntime.TransformNetType(classTag[P3].runtimeClass)
        val sp4 = CLRRuntime.TransformNetType(classTag[P4].runtimeClass)
        val sp5 = CLRRuntime.TransformNetType(classTag[P5].runtimeClass)
        val sp6 = CLRRuntime.TransformNetType(classTag[P6].runtimeClass)
        val sp7 = CLRRuntime.TransformNetType(classTag[P7].runtimeClass)
        val sp8 = CLRRuntime.TransformNetType(classTag[P8].runtimeClass)
        val sp9 = CLRRuntime.TransformNetType(classTag[P9].runtimeClass)
        val sp10 = CLRRuntime.TransformNetType(classTag[P10].runtimeClass)
        val sr = CLRRuntime.TransformNetType(classTag[R].runtimeClass)
        CLRRuntime.CreateDelegate("System.Func`12[" + sp0 + "," + sp1 + "," + sp2 + "," + sp3 + "," + sp4 + "," + sp5 + "," + sp6 + "," + sp7 + "," + sp8 + "," + sp9 + "," + sp10 + "," + sr + "]", (qx : Array[AnyRef]) => { func(qx(0).asInstanceOf[P0], qx(1).asInstanceOf[P1], qx(2).asInstanceOf[P2], qx(3).asInstanceOf[P3], qx(4).asInstanceOf[P4], qx(5).asInstanceOf[P5], qx(6).asInstanceOf[P6], qx(7).asInstanceOf[P7], qx(8).asInstanceOf[P8], qx(9).asInstanceOf[P9], qx(10).asInstanceOf[P10]) }.asInstanceOf[AnyRef])
    }

    def Func[P0 : ClassTag, P1 : ClassTag, P2 : ClassTag, P3 : ClassTag, P4 : ClassTag, P5 : ClassTag, P6 : ClassTag, P7 : ClassTag, P8 : ClassTag, P9 : ClassTag, P10 : ClassTag, P11 : ClassTag, R : ClassTag](func : (P0, P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11) => R) = {
        val sp0 = CLRRuntime.TransformNetType(classTag[P0].runtimeClass)
        val sp1 = CLRRuntime.TransformNetType(classTag[P1].runtimeClass)
        val sp2 = CLRRuntime.TransformNetType(classTag[P2].runtimeClass)
        val sp3 = CLRRuntime.TransformNetType(classTag[P3].runtimeClass)
        val sp4 = CLRRuntime.TransformNetType(classTag[P4].runtimeClass)
        val sp5 = CLRRuntime.TransformNetType(classTag[P5].runtimeClass)
        val sp6 = CLRRuntime.TransformNetType(classTag[P6].runtimeClass)
        val sp7 = CLRRuntime.TransformNetType(classTag[P7].runtimeClass)
        val sp8 = CLRRuntime.TransformNetType(classTag[P8].runtimeClass)
        val sp9 = CLRRuntime.TransformNetType(classTag[P9].runtimeClass)
        val sp10 = CLRRuntime.TransformNetType(classTag[P10].runtimeClass)
        val sp11 = CLRRuntime.TransformNetType(classTag[P11].runtimeClass)
        val sr = CLRRuntime.TransformNetType(classTag[R].runtimeClass)
        CLRRuntime.CreateDelegate("System.Func`13[" + sp0 + "," + sp1 + "," + sp2 + "," + sp3 + "," + sp4 + "," + sp5 + "," + sp6 + "," + sp7 + "," + sp8 + "," + sp9 + "," + sp10 + "," + sp11 + "," + sr + "]", (qx : Array[AnyRef]) => { func(qx(0).asInstanceOf[P0], qx(1).asInstanceOf[P1], qx(2).asInstanceOf[P2], qx(3).asInstanceOf[P3], qx(4).asInstanceOf[P4], qx(5).asInstanceOf[P5], qx(6).asInstanceOf[P6], qx(7).asInstanceOf[P7], qx(8).asInstanceOf[P8], qx(9).asInstanceOf[P9], qx(10).asInstanceOf[P10], qx(11).asInstanceOf[P11]) }.asInstanceOf[AnyRef])
    }

    def Func[P0 : ClassTag, P1 : ClassTag, P2 : ClassTag, P3 : ClassTag, P4 : ClassTag, P5 : ClassTag, P6 : ClassTag, P7 : ClassTag, P8 : ClassTag, P9 : ClassTag, P10 : ClassTag, P11 : ClassTag, P12 : ClassTag, R : ClassTag](func : (P0, P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12) => R) = {
        val sp0 = CLRRuntime.TransformNetType(classTag[P0].runtimeClass)
        val sp1 = CLRRuntime.TransformNetType(classTag[P1].runtimeClass)
        val sp2 = CLRRuntime.TransformNetType(classTag[P2].runtimeClass)
        val sp3 = CLRRuntime.TransformNetType(classTag[P3].runtimeClass)
        val sp4 = CLRRuntime.TransformNetType(classTag[P4].runtimeClass)
        val sp5 = CLRRuntime.TransformNetType(classTag[P5].runtimeClass)
        val sp6 = CLRRuntime.TransformNetType(classTag[P6].runtimeClass)
        val sp7 = CLRRuntime.TransformNetType(classTag[P7].runtimeClass)
        val sp8 = CLRRuntime.TransformNetType(classTag[P8].runtimeClass)
        val sp9 = CLRRuntime.TransformNetType(classTag[P9].runtimeClass)
        val sp10 = CLRRuntime.TransformNetType(classTag[P10].runtimeClass)
        val sp11 = CLRRuntime.TransformNetType(classTag[P11].runtimeClass)
        val sp12 = CLRRuntime.TransformNetType(classTag[P12].runtimeClass)
        val sr = CLRRuntime.TransformNetType(classTag[R].runtimeClass)
        CLRRuntime.CreateDelegate("System.Func`14[" + sp0 + "," + sp1 + "," + sp2 + "," + sp3 + "," + sp4 + "," + sp5 + "," + sp6 + "," + sp7 + "," + sp8 + "," + sp9 + "," + sp10 + "," + sp11 + "," + sp12 + "," + sr + "]", (qx : Array[AnyRef]) => { func(qx(0).asInstanceOf[P0], qx(1).asInstanceOf[P1], qx(2).asInstanceOf[P2], qx(3).asInstanceOf[P3], qx(4).asInstanceOf[P4], qx(5).asInstanceOf[P5], qx(6).asInstanceOf[P6], qx(7).asInstanceOf[P7], qx(8).asInstanceOf[P8], qx(9).asInstanceOf[P9], qx(10).asInstanceOf[P10], qx(11).asInstanceOf[P11], qx(12).asInstanceOf[P12]) }.asInstanceOf[AnyRef])
    }

    def Func[P0 : ClassTag, P1 : ClassTag, P2 : ClassTag, P3 : ClassTag, P4 : ClassTag, P5 : ClassTag, P6 : ClassTag, P7 : ClassTag, P8 : ClassTag, P9 : ClassTag, P10 : ClassTag, P11 : ClassTag, P12 : ClassTag, P13 : ClassTag, R : ClassTag](func : (P0, P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13) => R) = {
        val sp0 = CLRRuntime.TransformNetType(classTag[P0].runtimeClass)
        val sp1 = CLRRuntime.TransformNetType(classTag[P1].runtimeClass)
        val sp2 = CLRRuntime.TransformNetType(classTag[P2].runtimeClass)
        val sp3 = CLRRuntime.TransformNetType(classTag[P3].runtimeClass)
        val sp4 = CLRRuntime.TransformNetType(classTag[P4].runtimeClass)
        val sp5 = CLRRuntime.TransformNetType(classTag[P5].runtimeClass)
        val sp6 = CLRRuntime.TransformNetType(classTag[P6].runtimeClass)
        val sp7 = CLRRuntime.TransformNetType(classTag[P7].runtimeClass)
        val sp8 = CLRRuntime.TransformNetType(classTag[P8].runtimeClass)
        val sp9 = CLRRuntime.TransformNetType(classTag[P9].runtimeClass)
        val sp10 = CLRRuntime.TransformNetType(classTag[P10].runtimeClass)
        val sp11 = CLRRuntime.TransformNetType(classTag[P11].runtimeClass)
        val sp12 = CLRRuntime.TransformNetType(classTag[P12].runtimeClass)
        val sp13 = CLRRuntime.TransformNetType(classTag[P13].runtimeClass)
        val sr = CLRRuntime.TransformNetType(classTag[R].runtimeClass)
        CLRRuntime.CreateDelegate("System.Func`15[" + sp0 + "," + sp1 + "," + sp2 + "," + sp3 + "," + sp4 + "," + sp5 + "," + sp6 + "," + sp7 + "," + sp8 + "," + sp9 + "," + sp10 + "," + sp11 + "," + sp12 + "," + sp13 + "," + sr + "]", (qx : Array[AnyRef]) => { func(qx(0).asInstanceOf[P0], qx(1).asInstanceOf[P1], qx(2).asInstanceOf[P2], qx(3).asInstanceOf[P3], qx(4).asInstanceOf[P4], qx(5).asInstanceOf[P5], qx(6).asInstanceOf[P6], qx(7).asInstanceOf[P7], qx(8).asInstanceOf[P8], qx(9).asInstanceOf[P9], qx(10).asInstanceOf[P10], qx(11).asInstanceOf[P11], qx(12).asInstanceOf[P12], qx(13).asInstanceOf[P13]) }.asInstanceOf[AnyRef])
    }

    def Func[P0 : ClassTag, P1 : ClassTag, P2 : ClassTag, P3 : ClassTag, P4 : ClassTag, P5 : ClassTag, P6 : ClassTag, P7 : ClassTag, P8 : ClassTag, P9 : ClassTag, P10 : ClassTag, P11 : ClassTag, P12 : ClassTag, P13 : ClassTag, P14 : ClassTag, R : ClassTag](func : (P0, P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14) => R) = {
        val sp0 = CLRRuntime.TransformNetType(classTag[P0].runtimeClass)
        val sp1 = CLRRuntime.TransformNetType(classTag[P1].runtimeClass)
        val sp2 = CLRRuntime.TransformNetType(classTag[P2].runtimeClass)
        val sp3 = CLRRuntime.TransformNetType(classTag[P3].runtimeClass)
        val sp4 = CLRRuntime.TransformNetType(classTag[P4].runtimeClass)
        val sp5 = CLRRuntime.TransformNetType(classTag[P5].runtimeClass)
        val sp6 = CLRRuntime.TransformNetType(classTag[P6].runtimeClass)
        val sp7 = CLRRuntime.TransformNetType(classTag[P7].runtimeClass)
        val sp8 = CLRRuntime.TransformNetType(classTag[P8].runtimeClass)
        val sp9 = CLRRuntime.TransformNetType(classTag[P9].runtimeClass)
        val sp10 = CLRRuntime.TransformNetType(classTag[P10].runtimeClass)
        val sp11 = CLRRuntime.TransformNetType(classTag[P11].runtimeClass)
        val sp12 = CLRRuntime.TransformNetType(classTag[P12].runtimeClass)
        val sp13 = CLRRuntime.TransformNetType(classTag[P13].runtimeClass)
        val sp14 = CLRRuntime.TransformNetType(classTag[P14].runtimeClass)
        val sr = CLRRuntime.TransformNetType(classTag[R].runtimeClass)
        CLRRuntime.CreateDelegate("System.Func`16[" + sp0 + "," + sp1 + "," + sp2 + "," + sp3 + "," + sp4 + "," + sp5 + "," + sp6 + "," + sp7 + "," + sp8 + "," + sp9 + "," + sp10 + "," + sp11 + "," + sp12 + "," + sp13 + "," + sp14 + "," + sr + "]", (qx : Array[AnyRef]) => { func(qx(0).asInstanceOf[P0], qx(1).asInstanceOf[P1], qx(2).asInstanceOf[P2], qx(3).asInstanceOf[P3], qx(4).asInstanceOf[P4], qx(5).asInstanceOf[P5], qx(6).asInstanceOf[P6], qx(7).asInstanceOf[P7], qx(8).asInstanceOf[P8], qx(9).asInstanceOf[P9], qx(10).asInstanceOf[P10], qx(11).asInstanceOf[P11], qx(12).asInstanceOf[P12], qx(13).asInstanceOf[P13], qx(14).asInstanceOf[P14]) }.asInstanceOf[AnyRef])
    }

    def Func[P0 : ClassTag, P1 : ClassTag, P2 : ClassTag, P3 : ClassTag, P4 : ClassTag, P5 : ClassTag, P6 : ClassTag, P7 : ClassTag, P8 : ClassTag, P9 : ClassTag, P10 : ClassTag, P11 : ClassTag, P12 : ClassTag, P13 : ClassTag, P14 : ClassTag, P15 : ClassTag, R : ClassTag](func : (P0, P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15) => R) = {
        val sp0 = CLRRuntime.TransformNetType(classTag[P0].runtimeClass)
        val sp1 = CLRRuntime.TransformNetType(classTag[P1].runtimeClass)
        val sp2 = CLRRuntime.TransformNetType(classTag[P2].runtimeClass)
        val sp3 = CLRRuntime.TransformNetType(classTag[P3].runtimeClass)
        val sp4 = CLRRuntime.TransformNetType(classTag[P4].runtimeClass)
        val sp5 = CLRRuntime.TransformNetType(classTag[P5].runtimeClass)
        val sp6 = CLRRuntime.TransformNetType(classTag[P6].runtimeClass)
        val sp7 = CLRRuntime.TransformNetType(classTag[P7].runtimeClass)
        val sp8 = CLRRuntime.TransformNetType(classTag[P8].runtimeClass)
        val sp9 = CLRRuntime.TransformNetType(classTag[P9].runtimeClass)
        val sp10 = CLRRuntime.TransformNetType(classTag[P10].runtimeClass)
        val sp11 = CLRRuntime.TransformNetType(classTag[P11].runtimeClass)
        val sp12 = CLRRuntime.TransformNetType(classTag[P12].runtimeClass)
        val sp13 = CLRRuntime.TransformNetType(classTag[P13].runtimeClass)
        val sp14 = CLRRuntime.TransformNetType(classTag[P14].runtimeClass)
        val sp15 = CLRRuntime.TransformNetType(classTag[P15].runtimeClass)
        val sr = CLRRuntime.TransformNetType(classTag[R].runtimeClass)
        CLRRuntime.CreateDelegate("System.Func`17[" + sp0 + "," + sp1 + "," + sp2 + "," + sp3 + "," + sp4 + "," + sp5 + "," + sp6 + "," + sp7 + "," + sp8 + "," + sp9 + "," + sp10 + "," + sp11 + "," + sp12 + "," + sp13 + "," + sp14 + "," + sp15 + "," + sr + "]", (qx : Array[AnyRef]) => { func(qx(0).asInstanceOf[P0], qx(1).asInstanceOf[P1], qx(2).asInstanceOf[P2], qx(3).asInstanceOf[P3], qx(4).asInstanceOf[P4], qx(5).asInstanceOf[P5], qx(6).asInstanceOf[P6], qx(7).asInstanceOf[P7], qx(8).asInstanceOf[P8], qx(9).asInstanceOf[P9], qx(10).asInstanceOf[P10], qx(11).asInstanceOf[P11], qx(12).asInstanceOf[P12], qx(13).asInstanceOf[P13], qx(14).asInstanceOf[P14], qx(15).asInstanceOf[P15]) }.asInstanceOf[AnyRef])
    }








    def Delegate[R : ClassTag](classname : String, func : () => R) = {
        val sr = CLRRuntime.TransformNetType(classTag[R].runtimeClass)
        CLRRuntime.CreateDelegate(classname, (qx : Array[AnyRef]) => { func() }.asInstanceOf[AnyRef])
    }

    def Delegate[P0 : ClassTag, R : ClassTag](classname : String, func : (P0) => R) = {
        val sp0 = CLRRuntime.TransformNetType(classTag[P0].runtimeClass)
        val sr = CLRRuntime.TransformNetType(classTag[R].runtimeClass)
        CLRRuntime.CreateDelegate(classname, (qx : Array[AnyRef]) => { func(qx(0).asInstanceOf[P0]) }.asInstanceOf[AnyRef])
    }

    def Delegate[P0 : ClassTag, P1 : ClassTag, R : ClassTag](classname : String, func : (P0, P1) => R) = {
        val sp0 = CLRRuntime.TransformNetType(classTag[P0].runtimeClass)
        val sp1 = CLRRuntime.TransformNetType(classTag[P1].runtimeClass)
        val sr = CLRRuntime.TransformNetType(classTag[R].runtimeClass)
        CLRRuntime.CreateDelegate(classname, (qx : Array[AnyRef]) => { func(qx(0).asInstanceOf[P0], qx(1).asInstanceOf[P1]) }.asInstanceOf[AnyRef])
    }

    def Delegate[P0 : ClassTag, P1 : ClassTag, P2 : ClassTag, R : ClassTag](classname : String, func : (P0, P1, P2) => R) = {
        val sp0 = CLRRuntime.TransformNetType(classTag[P0].runtimeClass)
        val sp1 = CLRRuntime.TransformNetType(classTag[P1].runtimeClass)
        val sp2 = CLRRuntime.TransformNetType(classTag[P2].runtimeClass)
        val sr = CLRRuntime.TransformNetType(classTag[R].runtimeClass)
        CLRRuntime.CreateDelegate(classname, (qx : Array[AnyRef]) => { func(qx(0).asInstanceOf[P0], qx(1).asInstanceOf[P1], qx(2).asInstanceOf[P2]) }.asInstanceOf[AnyRef])
    }

    def Delegate[P0 : ClassTag, P1 : ClassTag, P2 : ClassTag, P3 : ClassTag, R : ClassTag](classname : String, func : (P0, P1, P2, P3) => R) = {
        val sp0 = CLRRuntime.TransformNetType(classTag[P0].runtimeClass)
        val sp1 = CLRRuntime.TransformNetType(classTag[P1].runtimeClass)
        val sp2 = CLRRuntime.TransformNetType(classTag[P2].runtimeClass)
        val sp3 = CLRRuntime.TransformNetType(classTag[P3].runtimeClass)
        val sr = CLRRuntime.TransformNetType(classTag[R].runtimeClass)
        CLRRuntime.CreateDelegate(classname, (qx : Array[AnyRef]) => { func(qx(0).asInstanceOf[P0], qx(1).asInstanceOf[P1], qx(2).asInstanceOf[P2], qx(3).asInstanceOf[P3]) }.asInstanceOf[AnyRef])
    }

    def Delegate[P0 : ClassTag, P1 : ClassTag, P2 : ClassTag, P3 : ClassTag, P4 : ClassTag, R : ClassTag](classname : String, func : (P0, P1, P2, P3, P4) => R) = {
        val sp0 = CLRRuntime.TransformNetType(classTag[P0].runtimeClass)
        val sp1 = CLRRuntime.TransformNetType(classTag[P1].runtimeClass)
        val sp2 = CLRRuntime.TransformNetType(classTag[P2].runtimeClass)
        val sp3 = CLRRuntime.TransformNetType(classTag[P3].runtimeClass)
        val sp4 = CLRRuntime.TransformNetType(classTag[P4].runtimeClass)
        val sr = CLRRuntime.TransformNetType(classTag[R].runtimeClass)
        CLRRuntime.CreateDelegate(classname, (qx : Array[AnyRef]) => { func(qx(0).asInstanceOf[P0], qx(1).asInstanceOf[P1], qx(2).asInstanceOf[P2], qx(3).asInstanceOf[P3], qx(4).asInstanceOf[P4]) }.asInstanceOf[AnyRef])
    }

    def Delegate[P0 : ClassTag, P1 : ClassTag, P2 : ClassTag, P3 : ClassTag, P4 : ClassTag, P5 : ClassTag, R : ClassTag](classname : String, func : (P0, P1, P2, P3, P4, P5) => R) = {
        val sp0 = CLRRuntime.TransformNetType(classTag[P0].runtimeClass)
        val sp1 = CLRRuntime.TransformNetType(classTag[P1].runtimeClass)
        val sp2 = CLRRuntime.TransformNetType(classTag[P2].runtimeClass)
        val sp3 = CLRRuntime.TransformNetType(classTag[P3].runtimeClass)
        val sp4 = CLRRuntime.TransformNetType(classTag[P4].runtimeClass)
        val sp5 = CLRRuntime.TransformNetType(classTag[P5].runtimeClass)
        val sr = CLRRuntime.TransformNetType(classTag[R].runtimeClass)
        CLRRuntime.CreateDelegate(classname, (qx : Array[AnyRef]) => { func(qx(0).asInstanceOf[P0], qx(1).asInstanceOf[P1], qx(2).asInstanceOf[P2], qx(3).asInstanceOf[P3], qx(4).asInstanceOf[P4], qx(5).asInstanceOf[P5]) }.asInstanceOf[AnyRef])
    }

    def Delegate[P0 : ClassTag, P1 : ClassTag, P2 : ClassTag, P3 : ClassTag, P4 : ClassTag, P5 : ClassTag, P6 : ClassTag, R : ClassTag](classname : String, func : (P0, P1, P2, P3, P4, P5, P6) => R) = {
        val sp0 = CLRRuntime.TransformNetType(classTag[P0].runtimeClass)
        val sp1 = CLRRuntime.TransformNetType(classTag[P1].runtimeClass)
        val sp2 = CLRRuntime.TransformNetType(classTag[P2].runtimeClass)
        val sp3 = CLRRuntime.TransformNetType(classTag[P3].runtimeClass)
        val sp4 = CLRRuntime.TransformNetType(classTag[P4].runtimeClass)
        val sp5 = CLRRuntime.TransformNetType(classTag[P5].runtimeClass)
        val sp6 = CLRRuntime.TransformNetType(classTag[P6].runtimeClass)
        val sr = CLRRuntime.TransformNetType(classTag[R].runtimeClass)
        CLRRuntime.CreateDelegate(classname, (qx : Array[AnyRef]) => { func(qx(0).asInstanceOf[P0], qx(1).asInstanceOf[P1], qx(2).asInstanceOf[P2], qx(3).asInstanceOf[P3], qx(4).asInstanceOf[P4], qx(5).asInstanceOf[P5], qx(5).asInstanceOf[P6]) }.asInstanceOf[AnyRef])
    }

    def Delegate[P0 : ClassTag, P1 : ClassTag, P2 : ClassTag, P3 : ClassTag, P4 : ClassTag, P5 : ClassTag, P6 : ClassTag, P7 : ClassTag, R : ClassTag](classname : String, func : (P0, P1, P2, P3, P4, P5, P6, P7) => R) = {
        val sp0 = CLRRuntime.TransformNetType(classTag[P0].runtimeClass)
        val sp1 = CLRRuntime.TransformNetType(classTag[P1].runtimeClass)
        val sp2 = CLRRuntime.TransformNetType(classTag[P2].runtimeClass)
        val sp3 = CLRRuntime.TransformNetType(classTag[P3].runtimeClass)
        val sp4 = CLRRuntime.TransformNetType(classTag[P4].runtimeClass)
        val sp5 = CLRRuntime.TransformNetType(classTag[P5].runtimeClass)
        val sp6 = CLRRuntime.TransformNetType(classTag[P6].runtimeClass)
        val sp7 = CLRRuntime.TransformNetType(classTag[P7].runtimeClass)
        val sr = CLRRuntime.TransformNetType(classTag[R].runtimeClass)
        CLRRuntime.CreateDelegate(classname, (qx : Array[AnyRef]) => { func(qx(0).asInstanceOf[P0], qx(1).asInstanceOf[P1], qx(2).asInstanceOf[P2], qx(3).asInstanceOf[P3], qx(4).asInstanceOf[P4], qx(5).asInstanceOf[P5], qx(6).asInstanceOf[P6], qx(7).asInstanceOf[P7]) }.asInstanceOf[AnyRef])
    }

    def Delegate[P0 : ClassTag, P1 : ClassTag, P2 : ClassTag, P3 : ClassTag, P4 : ClassTag, P5 : ClassTag, P6 : ClassTag, P7 : ClassTag, P8 : ClassTag, R : ClassTag](classname : String, func : (P0, P1, P2, P3, P4, P5, P6, P7, P8) => R) = {
        val sp0 = CLRRuntime.TransformNetType(classTag[P0].runtimeClass)
        val sp1 = CLRRuntime.TransformNetType(classTag[P1].runtimeClass)
        val sp2 = CLRRuntime.TransformNetType(classTag[P2].runtimeClass)
        val sp3 = CLRRuntime.TransformNetType(classTag[P3].runtimeClass)
        val sp4 = CLRRuntime.TransformNetType(classTag[P4].runtimeClass)
        val sp5 = CLRRuntime.TransformNetType(classTag[P5].runtimeClass)
        val sp6 = CLRRuntime.TransformNetType(classTag[P6].runtimeClass)
        val sp7 = CLRRuntime.TransformNetType(classTag[P7].runtimeClass)
        val sp8 = CLRRuntime.TransformNetType(classTag[P8].runtimeClass)
        val sr = CLRRuntime.TransformNetType(classTag[R].runtimeClass)
        CLRRuntime.CreateDelegate(classname, (qx : Array[AnyRef]) => { func(qx(0).asInstanceOf[P0], qx(1).asInstanceOf[P1], qx(2).asInstanceOf[P2], qx(3).asInstanceOf[P3], qx(4).asInstanceOf[P4], qx(5).asInstanceOf[P5], qx(6).asInstanceOf[P6], qx(7).asInstanceOf[P7], qx(8).asInstanceOf[P8]) }.asInstanceOf[AnyRef])
    }

    def Delegate[P0 : ClassTag, P1 : ClassTag, P2 : ClassTag, P3 : ClassTag, P4 : ClassTag, P5 : ClassTag, P6 : ClassTag, P7 : ClassTag, P8 : ClassTag, P9 : ClassTag, R : ClassTag](classname : String, func : (P0, P1, P2, P3, P4, P5, P6, P7, P8, P9) => R) = {
        val sp0 = CLRRuntime.TransformNetType(classTag[P0].runtimeClass)
        val sp1 = CLRRuntime.TransformNetType(classTag[P1].runtimeClass)
        val sp2 = CLRRuntime.TransformNetType(classTag[P2].runtimeClass)
        val sp3 = CLRRuntime.TransformNetType(classTag[P3].runtimeClass)
        val sp4 = CLRRuntime.TransformNetType(classTag[P4].runtimeClass)
        val sp5 = CLRRuntime.TransformNetType(classTag[P5].runtimeClass)
        val sp6 = CLRRuntime.TransformNetType(classTag[P6].runtimeClass)
        val sp7 = CLRRuntime.TransformNetType(classTag[P7].runtimeClass)
        val sp8 = CLRRuntime.TransformNetType(classTag[P8].runtimeClass)
        val sp9 = CLRRuntime.TransformNetType(classTag[P9].runtimeClass)
        val sr = CLRRuntime.TransformNetType(classTag[R].runtimeClass)
        CLRRuntime.CreateDelegate(classname, (qx : Array[AnyRef]) => { func(qx(0).asInstanceOf[P0], qx(1).asInstanceOf[P1], qx(2).asInstanceOf[P2], qx(3).asInstanceOf[P3], qx(4).asInstanceOf[P4], qx(5).asInstanceOf[P5], qx(6).asInstanceOf[P6], qx(7).asInstanceOf[P7], qx(8).asInstanceOf[P8], qx(9).asInstanceOf[P9]) }.asInstanceOf[AnyRef])
    }

    def Delegate[P0 : ClassTag, P1 : ClassTag, P2 : ClassTag, P3 : ClassTag, P4 : ClassTag, P5 : ClassTag, P6 : ClassTag, P7 : ClassTag, P8 : ClassTag, P9 : ClassTag, P10 : ClassTag, R : ClassTag](classname : String, func : (P0, P1, P2, P3, P4, P5, P6, P7, P8, P9, P10) => R) = {
        val sp0 = CLRRuntime.TransformNetType(classTag[P0].runtimeClass)
        val sp1 = CLRRuntime.TransformNetType(classTag[P1].runtimeClass)
        val sp2 = CLRRuntime.TransformNetType(classTag[P2].runtimeClass)
        val sp3 = CLRRuntime.TransformNetType(classTag[P3].runtimeClass)
        val sp4 = CLRRuntime.TransformNetType(classTag[P4].runtimeClass)
        val sp5 = CLRRuntime.TransformNetType(classTag[P5].runtimeClass)
        val sp6 = CLRRuntime.TransformNetType(classTag[P6].runtimeClass)
        val sp7 = CLRRuntime.TransformNetType(classTag[P7].runtimeClass)
        val sp8 = CLRRuntime.TransformNetType(classTag[P8].runtimeClass)
        val sp9 = CLRRuntime.TransformNetType(classTag[P9].runtimeClass)
        val sp10 = CLRRuntime.TransformNetType(classTag[P10].runtimeClass)
        val sr = CLRRuntime.TransformNetType(classTag[R].runtimeClass)
        CLRRuntime.CreateDelegate(classname, (qx : Array[AnyRef]) => { func(qx(0).asInstanceOf[P0], qx(1).asInstanceOf[P1], qx(2).asInstanceOf[P2], qx(3).asInstanceOf[P3], qx(4).asInstanceOf[P4], qx(5).asInstanceOf[P5], qx(6).asInstanceOf[P6], qx(7).asInstanceOf[P7], qx(8).asInstanceOf[P8], qx(9).asInstanceOf[P9], qx(10).asInstanceOf[P10]) }.asInstanceOf[AnyRef])
    }

    def Delegate[P0 : ClassTag, P1 : ClassTag, P2 : ClassTag, P3 : ClassTag, P4 : ClassTag, P5 : ClassTag, P6 : ClassTag, P7 : ClassTag, P8 : ClassTag, P9 : ClassTag, P10 : ClassTag, P11 : ClassTag, R : ClassTag](classname : String, func : (P0, P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11) => R) = {
        val sp0 = CLRRuntime.TransformNetType(classTag[P0].runtimeClass)
        val sp1 = CLRRuntime.TransformNetType(classTag[P1].runtimeClass)
        val sp2 = CLRRuntime.TransformNetType(classTag[P2].runtimeClass)
        val sp3 = CLRRuntime.TransformNetType(classTag[P3].runtimeClass)
        val sp4 = CLRRuntime.TransformNetType(classTag[P4].runtimeClass)
        val sp5 = CLRRuntime.TransformNetType(classTag[P5].runtimeClass)
        val sp6 = CLRRuntime.TransformNetType(classTag[P6].runtimeClass)
        val sp7 = CLRRuntime.TransformNetType(classTag[P7].runtimeClass)
        val sp8 = CLRRuntime.TransformNetType(classTag[P8].runtimeClass)
        val sp9 = CLRRuntime.TransformNetType(classTag[P9].runtimeClass)
        val sp10 = CLRRuntime.TransformNetType(classTag[P10].runtimeClass)
        val sp11 = CLRRuntime.TransformNetType(classTag[P11].runtimeClass)
        val sr = CLRRuntime.TransformNetType(classTag[R].runtimeClass)
        CLRRuntime.CreateDelegate(classname, (qx : Array[AnyRef]) => { func(qx(0).asInstanceOf[P0], qx(1).asInstanceOf[P1], qx(2).asInstanceOf[P2], qx(3).asInstanceOf[P3], qx(4).asInstanceOf[P4], qx(5).asInstanceOf[P5], qx(6).asInstanceOf[P6], qx(7).asInstanceOf[P7], qx(8).asInstanceOf[P8], qx(9).asInstanceOf[P9], qx(10).asInstanceOf[P10], qx(11).asInstanceOf[P11]) }.asInstanceOf[AnyRef])
    }

    def Delegate[P0 : ClassTag, P1 : ClassTag, P2 : ClassTag, P3 : ClassTag, P4 : ClassTag, P5 : ClassTag, P6 : ClassTag, P7 : ClassTag, P8 : ClassTag, P9 : ClassTag, P10 : ClassTag, P11 : ClassTag, P12 : ClassTag, R : ClassTag](classname : String, func : (P0, P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12) => R) = {
        val sp0 = CLRRuntime.TransformNetType(classTag[P0].runtimeClass)
        val sp1 = CLRRuntime.TransformNetType(classTag[P1].runtimeClass)
        val sp2 = CLRRuntime.TransformNetType(classTag[P2].runtimeClass)
        val sp3 = CLRRuntime.TransformNetType(classTag[P3].runtimeClass)
        val sp4 = CLRRuntime.TransformNetType(classTag[P4].runtimeClass)
        val sp5 = CLRRuntime.TransformNetType(classTag[P5].runtimeClass)
        val sp6 = CLRRuntime.TransformNetType(classTag[P6].runtimeClass)
        val sp7 = CLRRuntime.TransformNetType(classTag[P7].runtimeClass)
        val sp8 = CLRRuntime.TransformNetType(classTag[P8].runtimeClass)
        val sp9 = CLRRuntime.TransformNetType(classTag[P9].runtimeClass)
        val sp10 = CLRRuntime.TransformNetType(classTag[P10].runtimeClass)
        val sp11 = CLRRuntime.TransformNetType(classTag[P11].runtimeClass)
        val sp12 = CLRRuntime.TransformNetType(classTag[P12].runtimeClass)
        val sr = CLRRuntime.TransformNetType(classTag[R].runtimeClass)
        CLRRuntime.CreateDelegate(classname, (qx : Array[AnyRef]) => { func(qx(0).asInstanceOf[P0], qx(1).asInstanceOf[P1], qx(2).asInstanceOf[P2], qx(3).asInstanceOf[P3], qx(4).asInstanceOf[P4], qx(5).asInstanceOf[P5], qx(6).asInstanceOf[P6], qx(7).asInstanceOf[P7], qx(8).asInstanceOf[P8], qx(9).asInstanceOf[P9], qx(10).asInstanceOf[P10], qx(11).asInstanceOf[P11], qx(12).asInstanceOf[P12]) }.asInstanceOf[AnyRef])
    }

    def Delegate[P0 : ClassTag, P1 : ClassTag, P2 : ClassTag, P3 : ClassTag, P4 : ClassTag, P5 : ClassTag, P6 : ClassTag, P7 : ClassTag, P8 : ClassTag, P9 : ClassTag, P10 : ClassTag, P11 : ClassTag, P12 : ClassTag, P13 : ClassTag, R : ClassTag](classname : String, func : (P0, P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13) => R) = {
        val sp0 = CLRRuntime.TransformNetType(classTag[P0].runtimeClass)
        val sp1 = CLRRuntime.TransformNetType(classTag[P1].runtimeClass)
        val sp2 = CLRRuntime.TransformNetType(classTag[P2].runtimeClass)
        val sp3 = CLRRuntime.TransformNetType(classTag[P3].runtimeClass)
        val sp4 = CLRRuntime.TransformNetType(classTag[P4].runtimeClass)
        val sp5 = CLRRuntime.TransformNetType(classTag[P5].runtimeClass)
        val sp6 = CLRRuntime.TransformNetType(classTag[P6].runtimeClass)
        val sp7 = CLRRuntime.TransformNetType(classTag[P7].runtimeClass)
        val sp8 = CLRRuntime.TransformNetType(classTag[P8].runtimeClass)
        val sp9 = CLRRuntime.TransformNetType(classTag[P9].runtimeClass)
        val sp10 = CLRRuntime.TransformNetType(classTag[P10].runtimeClass)
        val sp11 = CLRRuntime.TransformNetType(classTag[P11].runtimeClass)
        val sp12 = CLRRuntime.TransformNetType(classTag[P12].runtimeClass)
        val sp13 = CLRRuntime.TransformNetType(classTag[P13].runtimeClass)
        val sr = CLRRuntime.TransformNetType(classTag[R].runtimeClass)
        CLRRuntime.CreateDelegate(classname, (qx : Array[AnyRef]) => { func(qx(0).asInstanceOf[P0], qx(1).asInstanceOf[P1], qx(2).asInstanceOf[P2], qx(3).asInstanceOf[P3], qx(4).asInstanceOf[P4], qx(5).asInstanceOf[P5], qx(6).asInstanceOf[P6], qx(7).asInstanceOf[P7], qx(8).asInstanceOf[P8], qx(9).asInstanceOf[P9], qx(10).asInstanceOf[P10], qx(11).asInstanceOf[P11], qx(12).asInstanceOf[P12], qx(13).asInstanceOf[P13]) }.asInstanceOf[AnyRef])
    }

    def Delegate[P0 : ClassTag, P1 : ClassTag, P2 : ClassTag, P3 : ClassTag, P4 : ClassTag, P5 : ClassTag, P6 : ClassTag, P7 : ClassTag, P8 : ClassTag, P9 : ClassTag, P10 : ClassTag, P11 : ClassTag, P12 : ClassTag, P13 : ClassTag, P14 : ClassTag, R : ClassTag](classname : String, func : (P0, P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14) => R) = {
        val sp0 = CLRRuntime.TransformNetType(classTag[P0].runtimeClass)
        val sp1 = CLRRuntime.TransformNetType(classTag[P1].runtimeClass)
        val sp2 = CLRRuntime.TransformNetType(classTag[P2].runtimeClass)
        val sp3 = CLRRuntime.TransformNetType(classTag[P3].runtimeClass)
        val sp4 = CLRRuntime.TransformNetType(classTag[P4].runtimeClass)
        val sp5 = CLRRuntime.TransformNetType(classTag[P5].runtimeClass)
        val sp6 = CLRRuntime.TransformNetType(classTag[P6].runtimeClass)
        val sp7 = CLRRuntime.TransformNetType(classTag[P7].runtimeClass)
        val sp8 = CLRRuntime.TransformNetType(classTag[P8].runtimeClass)
        val sp9 = CLRRuntime.TransformNetType(classTag[P9].runtimeClass)
        val sp10 = CLRRuntime.TransformNetType(classTag[P10].runtimeClass)
        val sp11 = CLRRuntime.TransformNetType(classTag[P11].runtimeClass)
        val sp12 = CLRRuntime.TransformNetType(classTag[P12].runtimeClass)
        val sp13 = CLRRuntime.TransformNetType(classTag[P13].runtimeClass)
        val sp14 = CLRRuntime.TransformNetType(classTag[P14].runtimeClass)
        val sr = CLRRuntime.TransformNetType(classTag[R].runtimeClass)
        CLRRuntime.CreateDelegate(classname, (qx : Array[AnyRef]) => { func(qx(0).asInstanceOf[P0], qx(1).asInstanceOf[P1], qx(2).asInstanceOf[P2], qx(3).asInstanceOf[P3], qx(4).asInstanceOf[P4], qx(5).asInstanceOf[P5], qx(6).asInstanceOf[P6], qx(7).asInstanceOf[P7], qx(8).asInstanceOf[P8], qx(9).asInstanceOf[P9], qx(10).asInstanceOf[P10], qx(11).asInstanceOf[P11], qx(12).asInstanceOf[P12], qx(13).asInstanceOf[P13], qx(14).asInstanceOf[P14]) }.asInstanceOf[AnyRef])
    }

    def Delegate[P0 : ClassTag, P1 : ClassTag, P2 : ClassTag, P3 : ClassTag, P4 : ClassTag, P5 : ClassTag, P6 : ClassTag, P7 : ClassTag, P8 : ClassTag, P9 : ClassTag, P10 : ClassTag, P11 : ClassTag, P12 : ClassTag, P13 : ClassTag, P14 : ClassTag, P15 : ClassTag, R : ClassTag](classname : String, func : (P0, P1, P2, P3, P4, P5, P6, P7, P8, P9, P10, P11, P12, P13, P14, P15) => R) = {
        val sp0 = CLRRuntime.TransformNetType(classTag[P0].runtimeClass)
        val sp1 = CLRRuntime.TransformNetType(classTag[P1].runtimeClass)
        val sp2 = CLRRuntime.TransformNetType(classTag[P2].runtimeClass)
        val sp3 = CLRRuntime.TransformNetType(classTag[P3].runtimeClass)
        val sp4 = CLRRuntime.TransformNetType(classTag[P4].runtimeClass)
        val sp5 = CLRRuntime.TransformNetType(classTag[P5].runtimeClass)
        val sp6 = CLRRuntime.TransformNetType(classTag[P6].runtimeClass)
        val sp7 = CLRRuntime.TransformNetType(classTag[P7].runtimeClass)
        val sp8 = CLRRuntime.TransformNetType(classTag[P8].runtimeClass)
        val sp9 = CLRRuntime.TransformNetType(classTag[P9].runtimeClass)
        val sp10 = CLRRuntime.TransformNetType(classTag[P10].runtimeClass)
        val sp11 = CLRRuntime.TransformNetType(classTag[P11].runtimeClass)
        val sp12 = CLRRuntime.TransformNetType(classTag[P12].runtimeClass)
        val sp13 = CLRRuntime.TransformNetType(classTag[P13].runtimeClass)
        val sp14 = CLRRuntime.TransformNetType(classTag[P14].runtimeClass)
        val sp15 = CLRRuntime.TransformNetType(classTag[P15].runtimeClass)
        val sr = CLRRuntime.TransformNetType(classTag[R].runtimeClass)
        CLRRuntime.CreateDelegate(classname, (qx : Array[AnyRef]) => { func(qx(0).asInstanceOf[P0], qx(1).asInstanceOf[P1], qx(2).asInstanceOf[P2], qx(3).asInstanceOf[P3], qx(4).asInstanceOf[P4], qx(5).asInstanceOf[P5], qx(6).asInstanceOf[P6], qx(7).asInstanceOf[P7], qx(8).asInstanceOf[P8], qx(9).asInstanceOf[P9], qx(10).asInstanceOf[P10], qx(11).asInstanceOf[P11], qx(12).asInstanceOf[P12], qx(13).asInstanceOf[P13], qx(14).asInstanceOf[P14], qx(15).asInstanceOf[P15]) }.asInstanceOf[AnyRef])
    }
}
  