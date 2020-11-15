Scala Agent
===
This is a generic example of Scala agent following the generic structure within **CoFlows**.

Note: The Scala <-> CoreCLR interop is achieved through [QuantApp.Kernel/JVM](https://github.com/CoFlows/CoFlows-CE/tree/master/QuantApp.Kernel/JVM "QAJVM").

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
                workspaceID + "-Agent", //ID
                workspaceID, //Workflow ID  
                "Scala Agent", //Name
                "Scala Agent", //Description
                null, //MID

                Utils.SetFunction("Load", CLR.Delegate[Array[AnyRef], Unit]("QuantApp.Engine.Load", data => { })),

                Utils.SetFunction("Add", CLR.Delegate[String, AnyRef, Unit]("QuantApp.Kernel.MCallback", (id, addedObject) => { })),

                Utils.SetFunction("Exchange", CLR.Delegate[String, AnyRef, Unit]("QuantApp.Kernel.MCallback", (id, data) => { })),

                Utils.SetFunction("Remove", CLR.Delegate[String, AnyRef, Unit]("QuantApp.Kernel.MCallback", (id, data) => { })),

                Utils.SetFunction("Body", CLR.Delegate[AnyRef, AnyRef]("QuantApp.Engine.Body", data => { 
                    val map = new Gson().fromJson(data.toString(), classOf[HashMap[String, String]]).asScala
                    if(map.contains("Data") && map("Data") == "Initial Execution")
                        println("     Agent Initial Execute @ : " + (new Date()))
                    data 
                })),
                "0 * * ? * *", //Cron Schedule
                Utils.SetFunction("Job", CLR.Delegate[LocalDateTime, AnyRef, Unit]("QuantApp.Engine.Job", (date, command) => { 
                }))
            )
        }
    }