Workflows
===
In **CoFlows** a project is called a Workflow. Developers declare the entire environment through a **package.json** file where they define both the project and it's executing containers together with the required resources.

Every workflow depends on a set of sections:
* **Base** code containing libraries used across the entire project
* [Agents](Agents/General.md "Agents") code describing functions that are called either on a scheduled basis or that react to various external triggers. Agents can pull data from a website or check for updates in a database to give a few examples
* [Queries](Queries/General.md "Queries") code defining functions that are automatically assigned URLs for WebAPI access from external systems.
* **Resources** settings to specify CPU / Memory requests and limits in a Kubernetes format.
* **Pips** Pip dependencies of your python code
* **NuGets** NuGet dependencies of your C#, F# and VB code
* **Jars** Jar dependencies of you Java and Scala code that are remotely hosted and referenced in a URL format.
* **Bins** Dlls or Jar files needed by your project that you provide. These are usually used for custom Dlls or Jars that are not in Maven or NuGet. 
* **Files** Generic files relevant for to the project. **Jupyter** Notebooks are a good example.

## Languages
The QuantApp Engine enables a polyglot environment where developers can code their functions in a variety of languages while allowing all languages to co-exist in the same process in memory. This mitigates the need for a TCP (SOAP, JSON WebAPI, Py4J) overhead which is usual when functions from different languages interact.

Within a **CoFlows** workflow, three computing environments interact within the same process including the **Core CLR** (DotNet Core Language Runtime), **JVM** (Java Virtual Machine) and the **Python interpreter**. Javascript is interpreted within the CLR using the Jint package. 
Note: The CLR is the main execution environment which calls the JVM and and Python interpreter.

The full list is:
* CoreCLR: C#, F# & VB
* JVM: Java & Scala
* Python
* Javascript

## Polyglot <-> Interoperability
The interop functionality in **CoFlows** is achieved through a open-source projects. **CoreCLR** is the main execution environment that loads both the JVM and Python environments and also interprets Javascript. The Python environment is loaded through a fork of the **PythonNet** library while the JVM is loaded through the **QuantApp.Kernel/JVM** libraries. Please note that the **PythonNet** fork was incorporated into the **QuantApp.Kernel/Python** package for additional integration. For further details please read [Polyglot](Polyglot/General.md "Polyglot")

## Deployment
Deploying a project through **CoFlows** can be done through **GitHub** straight into an executing container. 

Once a project has been committed to a **GitHub** repo (or straight into **CoFlows** through the **CoFlows** CLI), **CoFlows** creates a Container with the specified resource requirements. The code is then built and deployed within the container and access to the container is opened through the main CoFlows services managing security and orchestration. Developers can access the container through a variety of ways:
* **WebAPI** access to the Queries
* **CoFlows** UI where live updates to Agents and Queries can be done without the need to restart the container.
* **Jupyter Lab** which in turn offers access to both Notebooks, the Python Console and Terminal within the executing container.  
  Terminal Note: If you run a few experiments in a notebook and want to commit the notebook changes you can use the Terminal. Either commit the changes straight into **CoFlows** through  
  `Bash> coflows commit`  
  or use the standard git cli to commit to **GitHub**  


## Definition

Workflows are declared through a package.json file like:

    {
        "ID": GUID,
        "Name": "Some cool name",
        "Base": [
            {
                "Name": "pyflows.py",
                "Content": "Base/pyflows.py"
            }
        ],
        "Agents": [
            {
                "Name": "agent.py",
                "Content": "Agents/agent.py",
                "Exe": "pkg"
            }
        ],
        "Queries": [
            {
                "Name": "query.py",
                "Content": "Queries/query.py",
                "ID": "test"
            }
        ],
        "Permissions": [
            {
                "ID": email of some user,
                "Permission": 2 (Write) // 1 = Read, 0 = 
            }
        ],
        "NuGets": [
            {
                "ID": "Accord.MachineLearning",
                "Version": "3.8.0"
            },
        ],
        "Pips": [
            {
            "ID": "pyspark"
            }
        ],
        "Jars": [
            {
                "Url": "https://repo1.maven.org/maven2/com/microsoft/sqlserver/mssql-jdbc/7.4.1.jre8/mssql-jdbc-7.4.1.jre8.jar"
            }
        ],
        "Bins": [],
        "Files": []
        ],
        "ReadMe": "README.md",
        "Publisher": null,
        "PublishTimestamp": "0001-01-01T00:00:00",
        "AutoDeploy": true,
        "Container": {
            "Type": "",
            "Request": {
            "Cpu": "",
            "Mem": ""
            },
            "Limit": {
            "Cpu": "",
            "Mem": ""
            }
        }
    }
