Java Agent
===
This is a generic example of Java agent following the generic structure within **CoFlows**.

Note: The Java <-> CoreCLR interop is achieved through [QuantApp.Kernel/JVM](https://github.com/CoFlows/CoFlows-CE/tree/master/QuantApp.Kernel/JVM "QAJVM").

    import app.quant.clr.*;

    import java.time.*;
    import java.time.format.*;

    import java.util.*;
    import com.google.gson.Gson;

    class Agent
    {
        public Agent(){}

        private static String workspaceID = "$WID$";
        public static Object pkg()
        {
            CLRObject Utils = CLRRuntime.GetClass("QuantApp.Engine.Utils");

            CLRObject M = CLRRuntime.GetClass("QuantApp.Kernel.M");

            return CLRRuntime.CreateInstance("QuantApp.Engine.FPKG",
                workspaceID + "-Agent", //ID
                workspaceID, //Workflow ID  
                "Java Agent", //Name
                "Java Agent", //Description
                null, //MID

                Utils.Invoke("SetFunction", "Load", CLRRuntime.CreateDelegate("QuantApp.Engine.Load", (x) -> { 
                    System.out.println("Java Agent Load");
                    return 0;
                })),

                Utils.Invoke("SetFunction", "Add", CLRRuntime.CreateDelegate("QuantApp.Kernel.MCallback", (x) -> { 
                    return 0;
                })),

                Utils.Invoke("SetFunction", "Exchange", CLRRuntime.CreateDelegate("QuantApp.Kernel.MCallback", (x) -> { 
                    String id = (String)x[0];
                    Object data = x[1];

                    System.out.println("Java Agent Exchange");
                    return 0;
                })),

                Utils.Invoke("SetFunction", "Remove", CLRRuntime.CreateDelegate("QuantApp.Kernel.MCallback", (x) -> { 
                    String id = (String)x[0];
                    Object data = x[1];

                    return 0;
                })),

                Utils.Invoke("SetFunction", "Body", CLRRuntime.CreateDelegate("QuantApp.Engine.Body", (x) -> { 
                    Object data = x[0];

                    Map map = new Gson().fromJson(data.toString(), Map.class);
                    if(map.containsKey("Data") && map.get("Data").equals("Initial Execution"))
                        System.out.println("     Agent Initial Execute @ " + (new Date()));
                    
                    return data;
                })),

                "0 * * ? * *", //Cron Schedule
                Utils.Invoke("SetFunction", "Job", CLRRuntime.CreateDelegate("QuantApp.Engine.Job", (x) -> { 
                    Object date = x[0];
                    String command = (String)x[1];

                    return 0;
                }))
            );
        }
    }