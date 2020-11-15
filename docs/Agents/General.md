General Agent
===

**CoFlows** agents are designed to react to either changes to an [M](../M.md "M Set") topic, or to scheduled events according to developer defined **cron jobs** within **CoFlows**.

Agents can be written in a variety of languages (C#, F#, VB, Python, Java, Scala and Javascript) and all follow the same generic structure.

Every agent has a set of properties like ID, Name and Description. Every agent also belongs to a Workflow.
Agents, can react to either a 
* scheduled event which is defined by a CRON command in the **ScheduledCommand** property and/or to
* changes to an **M** set defined by the **MID** property and /or to
* external messages sent to agent.

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

## Examples
* [C# Agent](Cs.md "C# Agent")
* [F# Agent](Fs.md "F# Agent")
* [VB Agent](Vb.md "VB Agent")
* [Python Agent](Python.md "Python Agent")
* [Java Agent](Java.md "Java Agent")
* [Scala Agent](Scala.md "Scala Agent")
* [Javascript Agent](Javascript.md "Javascript Agent")
