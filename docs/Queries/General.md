Queries
===

Developers can define Web API entry points to the Workflows through Queries.

Every function defined in a query is automatically assigned a _url_ by the **CoFlows** simplifying the process of creating these endpoints.

The structure of a query is defined by the least amount of code to create an "executable" module in each language.

In C# you simply need to define a class with static functions. In F# its a module and respective functions. In Python on the other hand it simply is the actual functions.

The WebAPI url structure is defined by

 
    http(s)://[host]/m/getwb?workbook=[WorkflowID]&id=[QueryID]&name=[Function]&p[0]=x&p[1]=y...

where the

    [host] = name of machine hosting CoFlows
    [WorkflowID] = ID of workflow as defined in package.json
    [QueryID] = ID of Query as defined in package.json
    [Function] = name of function in query
    p[0] = value of first argument taken by function
    p[n] = value of nth argument taken by function

## Examples
* [C# Query](Cs.md "C# Query")
* [F# Query](Fs.md "F# Query")
* [VB Query](Vb.md "VB Query")
* [Python Query](Python.md "Python Query")
* [Java Query](Java.md "Java Query")
* [Scala Query](Scala.md "Scala Query")
* [Javascript Query](Javascript.md "Javascript Query")
