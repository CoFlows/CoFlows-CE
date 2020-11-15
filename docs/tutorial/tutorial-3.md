# Tutorial 3 - Agents, scheduled and asynchronous workflows

This tutorial builds on the [second tutorial](tutorial-2.md) and explains how to create a scheduled and / or asynchronous workflows with [**CoFlows CE (Community Edition)**](https://github.com/QuantApp/CoFlows-CE). 


## Agents
Agents are designed to either react to scheduled events according to developer defined **cron jobs** or to changes in an [M Set](../M.md "M Set"). They can be written in a variety of languages (C#, F#, VB, Python, Java, Scala and Javascript) and all follow the same generic structure.

Every agent has a set of defining properties including ID, Name and Description. All agents also belong to a Workflow and they react to either a 
* scheduled event which is defined by a CRON command in the **ScheduledCommand** property and/or to
* changes to an **M** set defined by the **MID** property and /or to
* external messages sent to agent.

### M Set
The **M** set is a persistent and distributed list. Subscribers can link to this list to receive updates on changes to its state.  **M** can handle any object serialisable into JSON and ensures the objects in the set are replicated across all it's subscribers.

Please read more about [M Sets here](../M.md "M Set").

### Create an agent
Through the terminal, enter into the **bin** folder if you are using linux/macos or alternatively enter the **bin/bat** folder if you are using windows.

To add an Agent please run the following command from within the **bin** or **bin/bat** folder as mentioned above:

    linux/macos:    sh add.sh agent (cs, fs, py, java, scala, js, vb) {name of agent}
    windows:        add.bat agent (cs, fs, py, java, scala, js, vb) {name of agent}

This will create a new subfolder and a source file with a sample Agent. 

Lets run an example step by step to create a Python Agent assuming we are using a macos based machine.

    sh add.sh agent py pyagent

your folder structure should now look as follows:

    📦/
    ┣ 📂Agents
    ┃ ┗ 📜pyapi.py

The pyapi.py looks like this:

    import clr

    import System
    import QuantApp.Kernel as qak
    import QuantApp.Engine as qae

    import json
    import datetime

    workspaceID = "$WID$"

    def Add(id, data):
        pass

    def Exchange(id, data):
        pass

    def Remove(id, data):
        pass
        
    def Load(data):
        pass
        
    def Body(data):
        cmd = json.loads(data)
        if 'Data' in cmd and cmd['Data'] == 'Initial Execution':    
            print('     pyagent Initial Execute @ ' + str(datetime.datetime.now()))

        return data

    def Job(timestamp, data):
        pass

    def pkg():
        return qae.FPKG(
        workspaceID + "-pyagent", #ID
        workspaceID, #Workflow ID
        "Python pyagent Agent", #Name
        "Python pyagent Agent", #Description
        None, #M ID Listener
        qae.Utils.SetFunction("Load", qae.Load(Load)), 
        qae.Utils.SetFunction("Add", qak.MCallback(Add)), 
        qae.Utils.SetFunction("Exchange", qak.MCallback(Exchange)), 
        qae.Utils.SetFunction("Remove", qak.MCallback(Remove)), 
        qae.Utils.SetFunction("Body", qae.Body(Body)), 
        "0 * * ? * *", #Cron Schedule
        qae.Utils.SetFunction("Job", qae.Job(Job))
        )

## pkg() function
The **pkg()** function is called by **CoFlows** to load the agent. The name of function is created by default but can be changed manually. If you choose to change the name of the **pkg()** function you must also manually change the **package.json** entry for this agent.

After adding the agent through the CLI command above, an entry to the **package.json** is added.

    "Agents": [
        {
        "Name": "pyagent.py",
        "Content": "Agents/pyagent.py",
        "Exe": "pkg"
        }
    ],

In the event you need to change the name of the "pkg" function, then you need to change the **Exe** entry accordingly.

## Callback functions
As previously mentioned Agents can react to [M Sets](../M.md "M Set"). These reactions are defined through a set of callback functions:

### Messaging the Agent
Communicating with an Agent can be done by calling the **Body** function through an HTTP Post request. The **Body** function will also be executed when **CoFlows** starts in order to run startup scripts. The sample below shows how a startup script can be executed.

    def Body(data):
        cmd = json.loads(data)
        if 'Data' in cmd and cmd['Data'] == 'Initial Execution':    
            print('     pyagent Initial Execute @ ' + str(datetime.datetime.now()))

        return json.dumps(cmd)

Calling the Agent through curl

    curl -X POST -d '{"y":["y1", "y2", "y3", "y4", "y5", "y6"]}' -H "Content-Type: application/json" -H "_cokey: 30be80ea-835b-4524-a43a-21742aae77fa" -g "http://localhost/flow/agent/9a7adf48-183f-4d44-8ab2-c0afd1610c71-pyagent"

using the Secret in the **cokey** header as described in [tutorial 1](tutorial-1.md).

### Scheduled Jobs
Agents can run jobs according to [cron](https://www.freeformatter.com/cron-expression-generator-quartz.html) schedules. The **Job** function is executed according to the schedule entry in the **pkg()** function. There is free tool that helps define [cron jobs here](https://www.freeformatter.com/cron-expression-generator-quartz.html). Please visit that site for more details but below are two examples:

    "0 * * ? * *", #Cron Schedule - Every hour
    "0 0/1 0 ? * *", #Cron Schedule - Every minute

Note that the tool formats cron jobs with years included, this yields cron schedules with 7 elements. **CoFlows** doesn't accept years in the cron job so we only take 6 elements like we have done in the samples above. The **Job** function takes two variables, **timestamp** which is when the function is called and **data** a label for the job that is being run:

    def Job(timestamp, data):
        pass

### M Set - Add 
The **Add** function is called if an object is added to the **M Set** which the Agent has subscribed to. The **id** variable contains 
id of the object and the **data** variable is the actual object.

    def Add(id, data):
        pass

### M Set - Exchange 
The **Exchange** function is called if an object with an **id** is exchanged for the object **data**.

    def Exchange(id, data):
        pass

### M Set - Remove 
The **Remove** function is called if an object with an **id** is removed.

    def Remove(id, data):
        pass

### M Set - Load 
When the Agent is started, the **Load** function is called in ordered for the Agent to have a full view of the objects currently in the **M Set**.

    def Load(data):
        pass

## Examples
* [C# Agent](../Agents/Cs.md "C# Agent")
* [F# Agent](../Agents/Fs.md "F# Agent")
* [VB Agent](../Agents/Vb.md "VB Agent")
* [Python Agent](../Agents/Python.md "Python Agent")
* [Java Agent](../Agents/Java.md "Java Agent")
* [Scala Agent](../Agents/Scala.md "Scala Agent")
* [Javascript Agent](../Agents/Javascript.md "Javascript Agent")


## General Structure
To summarise the structure of Agent's as described above, we have:
### Properties

    ID                  "The ID of the Agent, we recommend using a GUID"
    WorkflowID          "ID of the Workflow the Agent belongs to"
    Name                "Name of Agent"
    Description         "Description of Agent's reason to exist"
    MID                 "ID of the M set which the Agent should react to"
    ScheduleCommand     "CRON job definition"


The agent reactions are defined by the following callbacks
### M Callbacks

    Load                "Executed when loading the M set"
    Add                 "Executed when an entry is added to the M set"
    Exchange            "Executed when an entry is changed to the M set"
    Remove              "Executed when an entry is removed to the M set"

### External Message and CRON Callbacks

    Body                "Executed when an external message is sent to the agent"
    Job                 "Executed when a CRON Job is defined"
## Next Tutorial
Please continue on to the [Fourth Tutorial](tutorial-4.md) to learn about Plotly Dash Apps with [**CoFlows CE (Community Edition)**](https://github.com/QuantApp/CoFlows-CE). 

  