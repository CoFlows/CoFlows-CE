Scala Agent
===
This is a generic example of Scala agent following the generic structure within **CoFlows**.

Note: The Scala <-> CoreCLR interop is achieved through [QuantApp.Kernel/JVM](https://github.com/CoFlows/CoFlows-CE/tree/master/QuantApp.Kernel/JVM "QAJVM").

    import scala.collection._

    import app.quant.clr._
    import app.quant.clr.scala.{SCLRObject => CLR}

    class ScalaAgent {
        private val defaultID = "xxx"
        def pkg() = {
            val Utils = CLR("QuantApp.Engine.Utils")
            val M = CLR("QuantApp.Kernel.M")

            CLR("QuantApp.Engine.FPKG",
                defaultID, //ID
                "Hello_World_Workflow", //Workflow ID  
                "Hello Scala Agent", //Name
                "Hello Scala Analytics Agent Sample", //Description
                "xxx-MID", //Scala Listener

                Utils.SetFunction("Load", CLR.Delegate[Array[AnyRef], Unit]("QuantApp.Engine.Load", data => { 
                    println("Scala Load: " + data)
                })),

                Utils.SetFunction("Add", CLR.Delegate[String, AnyRef, Unit]("QuantApp.Kernel.MCallback", (id, addedObject) => { 
                    println("Scala Add: " + addedObject)
                })),

                Utils.SetFunction("Exchange", CLR.Delegate[String, AnyRef, Unit]("QuantApp.Kernel.MCallback", (id, data) => { 
                    println("Scala Exchange: " + data)
                })),

                Utils.SetFunction("Remove", CLR.Delegate[String, AnyRef, Unit]("QuantApp.Kernel.MCallback", (id, data) => { 
                    println("Scala Remove: " + data)
                })),

                Utils.SetFunction("Body", CLR.Delegate[AnyRef, AnyRef]("QuantApp.Engine.Body", data => {
                    println("Scala Body: " + data)
                    data
                })),

                "0 * * ? * *", //Cron Schedule
                Utils.SetFunction("Job", CLR.Delegate[LocalDateTime, AnyRef, Unit]("QuantApp.Engine.Job", (date, command) => { 
                    println("Scala Job: " + date)
                }))
            )
        }
    }