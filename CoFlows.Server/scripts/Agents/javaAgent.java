/*
 * The MIT License (MIT)
 * Copyright (c) Arturo Rodriguez All rights reserved.
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

import app.quant.clr.*;

import java.time.*;
import java.time.format.*;

import java.util.*;
import com.google.gson.Gson;

class XXX
{
    public XXX(){}

    private static String workspaceID = "$WID$";
    public static Object pkg()
    {
        CLRObject Utils = CLRRuntime.GetClass("QuantApp.Engine.Utils");

        CLRObject M = CLRRuntime.GetClass("QuantApp.Kernel.M");

        return CLRRuntime.CreateInstance("QuantApp.Engine.FPKG",
            workspaceID + "-XXX", //ID
            workspaceID, //Workflow ID  
            "Java XXX Agent", //Name
            "Java XXX Agent", //Description
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
                    System.out.println("     XXX Initial Execute @ " + (new Date()));
                
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