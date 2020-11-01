/*
 * The MIT License (MIT)
 * Copyright (c) Arturo Rodriguez All rights reserved.
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
 
import scala.collection._
import collection.JavaConverters._

import app.quant.clr._
import app.quant.clr.scala.{SCLRObject => CLR}

import java.time._
import java.util._

import com.google.gson._

class XXX {
    private val workspaceID = "$WID$"
    def pkg() = {
        val Utils = CLR("QuantApp.Engine.Utils")
        val M = CLR("QuantApp.Kernel.M")

        CLR("QuantApp.Engine.FPKG",
            workspaceID + "-XXX", //ID
            workspaceID, //Workflow ID  
            "Scala XXX Agent", //Name
            "Scala XXX Agent", //Description
            null, //MID

            Utils.SetFunction("Load", CLR.Delegate[Array[AnyRef], Unit]("QuantApp.Engine.Load", data => { })),

            Utils.SetFunction("Add", CLR.Delegate[String, AnyRef, Unit]("QuantApp.Kernel.MCallback", (id, addedObject) => { })),

            Utils.SetFunction("Exchange", CLR.Delegate[String, AnyRef, Unit]("QuantApp.Kernel.MCallback", (id, data) => { })),

            Utils.SetFunction("Remove", CLR.Delegate[String, AnyRef, Unit]("QuantApp.Kernel.MCallback", (id, data) => { })),

            Utils.SetFunction("Body", CLR.Delegate[AnyRef, AnyRef]("QuantApp.Engine.Body", data => { 
                val map = new Gson().fromJson(data.toString(), classOf[HashMap[String, String]]).asScala
                if(map.contains("Data") && map("Data") == "Initial Execution")
                    println("     XXX Initial Execute @ : " + (new Date()))
                data 
            })),
            "0 * * ? * *", //Cron Schedule
            Utils.SetFunction("Job", CLR.Delegate[LocalDateTime, AnyRef, Unit]("QuantApp.Engine.Job", (date, command) => { 
            }))
        )
    }
}