# CoFlows - Containerized Polyglot Runtime (Interop)

_We are actively looking for contributors, feedback and stars **(please click the upper right star button)**_

**CoFlows CE (Community Edition)** helps developers create Web APIs and data-centric workflows in various languages including **Python, Java, Scala, C#, F#, VB and Javascript**. These workflows require the management of multiple facets that can be daunting for many coders. **CoFlows CE** aims to manage many of these facets including:
* OpenAPI - Automatic generation of [OpenAPI specifications](https://www.openapis.org)
* Agents - Scheduled [Cron](https://www.freeformatter.com/cron-expression-generator-quartz.html) and Asynchronous workflows
* Apps - Beautiful visual interfaces with [Plotly Dash](https://plotly.com/dash/) apps in python
* User Management - Authentication / Logins
* Permissions - Authorization through groups
* Free SSL - [LetsEncrypt](https://letsencrypt.org) certificate management
* Persistence - Connections to SQL databases including [SQLite](https://www.sqlite.org/index.html), [Microsoft SQL Server](https://www.microsoft.com/en-us/sql-server) and [Postgres SQL](https://www.postgresql.org)
* Cloud Hosting - Support to deploy into [Azure Container Instances](https://azure.microsoft.com/en-us/services/container-instances/)

You can now choose to go through a few hands on tutorials or continue reading this page for a general overview:

* [Tutorial 1 - Create a simple API, manage permissions, secrets and generate OpenAPI specs](docs/tutorial/tutorial-1.md)
* [Tutorial 2 - Add support for third-party dependencies like **pips**, **nugets** and **jars**](docs/tutorial/tutorial-2.md)
* [Tutorial 3 - Create an agent to support scheduled and asynchronous workflows](docs/tutorial/tutorial-3.md)
* [Tutorial 4 - Create a Plotly Dash App](docs/tutorial/tutorial-4.md)
* [Tutorial 5 - Users, Groups, Permissions and Expiries](docs/tutorial/tutorial-5.md)
* [Tutorial 6 - Configurations, Hosting and deployment](docs/tutorial/tutorial-6.md)


## General Overview

_We are actively looking for contributors, feedback and stars **(please click the upper right star button)**_

**CoFlows CE (Community Edition)** is a Containerized Polyglot Runtime that simplifies the development, hosting and deployment of powerful data-centric workflows. **CoFlows** enables developers to create rich **Web-APIs** with almost **zero boiler plate** and scheduled / reactive processes through a range of languages including CoreCLR (C#, F# and VB), JVM (Java and Scala), Python and Javascript. Furthermore, functions written in any of these languages can call each other within the same process with **full interop**. 

For a bit context, one implementation of **CoFlows** is a system to track ships and the imports/exports between countries in order to predict supply and demand dynamics for various commodities. To this end, we worked with GPS data where it was essential for us to quickly plot transformed data on a map for correctly tuning the algorithms. Below is a visual of this example:

| Code |  Visual |  Table |
:-------------------------:|:-------------------------:|:-------------------------: 
![](docs/images/QA_python_agent.png)  | ![](docs/images/QA_vessels_map.png)  |  ![](docs/images/QA_table_query.png)

As a Containerized Polyglot Runtime, its functionality allows developers to build complex workflows leveraging off great open-source libraries written in various languages. The wealth of distributed computing libraries of Java / Scala together with Python's data science tools are all available in order to use the right tool for the right purpose within the same process!

Furthermore, our aim with **CoFlows** is to offer simplicity for **Data Scientists** in order to quickly build self-contained projects while leveraging off popular tools. To this end, once a developer pulls the **CoFlows** image, a range of tools are at their disposal as first class citizens of the **CoFlows** ecosystem:
* [DotNet Core 5.0](https://github.com/dotnet/core)
* [Python 3.7.9](https://www.python.org)
* [Java 11](https://www.java.com/)
* [Scala 2.12.10](https://www.scala-lang.org)
* [JupyterLab](https://github.com/jupyterlab/jupyterlab)

... and yes, it is a big image taking 900Mb to download and 1.9Gb of space during runtime.

Our **Community Edition** is a version of our commercial codebase that we have used in various Data science projects including:
* Algorithmic Trading
* Vessel tracking and commodity trade flow projections
* Macro-economic risk management
* Global equity selection strategy simulations
* Healthcare cost and clinical segmentation analysis on national data

For more information visit https://www.coflows.com.

This edition will be tightly coupled with the **CoFlows Cloud** making it much easier for developers to deploy and host their applications. More information on this coming soon.

## Polygot
Let's start with a definition. According to Wikipedia, in computing, a polyglot is a computer program or script written in a valid form of multiple programming languages, which performs the same operations or output independent of the programming language used to compile or interpret it.

The QuantApp Engine enables a polyglot environment where developers can code their functions in a variety of languages while allowing all languages to co-exist in the same process in memory. This mitigates the need for a TCP (SOAP, JSON WebAPI, Py4J) overhead which is usual when functions from different languages interact.

Within a CoFlows workflow, three computing environments interact within the same process including the Core CLR (DotNet Core Language Runtime), JVM (Java Virtual Machine) and the Python interpreter. Javascript is interpreted within the CLR using the Jint package. Note: The CLR is the main execution environment which calls the JVM and and Python interpreter.

There are multiple sources pushing both the pros and cons of polyglot systems and this is not the place to discuss it. We believe that the ability to express algorithms in the correct language and using the best libraries for the right task is essential. **CoFlows** uses the **QuantApp.Engine** and **QuantApp.Kernel** libraries to offer interop between:
* CoreCLR: C#, F# & VB
* JVM: Java & Scala
* Python
* Javascript

### Notes
_We are actively looking for contributors, feedback and stars **(please click the upper right star button)**_

The Python link is achieved through a fork of the wonderful library [PythonNet](https://github.com/pythonnet/pythonnet "PythonNet").

The link between the **JVM** and **CLR** is achieved through the [QuantApp.Kernel/JVM](https://github.com/CoFlows/CoFlows-CE/tree/master/QuantApp.Kernel/JVM "QAJVM") libraries which have been developed from scratch for this project.

Javascript interpretation is achieved using the great [Jint](https://github.com/sebastienros/jint "Jint") library.

For further details please read [Polyglot](docs/Polyglot/General.md "Polyglot").

## Workflows

Projects in **CoFlows** are called Workflows. They contain the logic that defines the Web APIs and scheduled / reactive processes together with the definition of the entire environment including Nuget, Jar and Pip packages that the Workflow depends on.

## Security, Authentication and Permissions

**CoFlows** has integrated LetsEncrypt functionality to simply the process to enable encrypted SSL connections. The standard process is to purchase an SSL certificate from CA, then transform the certificate into a PFX file which then should be stored securely while being accessible by the container. With LetsEncrypt, **CoFlows** now automatically generates a trusted SSL certificate if the domain name linked to the certificate is pointing to the container IP address.

### Authentication through username/password and Secret Keys
Authentication is done through **CoFlows** internal functionality. Users can either authenticate through the UI with a username and password. Alternatively, you can authenticate WebAPI calls using a secret key. This key can be accessed through the UI as explained below.

### Permissions
There are two types of  in **CoFlows** called Fixed and Group. First is the Fixed type which is hardcoded in the definition of the Workflow. Fixed permissions enable developers to give access to certain users prior to setting up Group permissions. You can think of these permissions as system permissions which should not be used for standard users but rather for system users.

Group permissions on the other hand are dynamic and related to groups. Groups are effectively sets of permissions that allow administrators to organize and control user access to internal features of the workflows.

To manage user permissions you can create multiple groups and add users to these groups. For example, imagine you have a workflow with two types of users called Retail and Professional. Users added to these groups can have 1 of 4 possible permissions:
1) Denied (-1) - Explicit no access to this group, perhaps think of this as black list.
2) View (0) - Lowest level of access to this group. Perhaps users that are not paying for additional services but are having a free trial.
3) Read (1) - Mid level access, perhaps access to read information that is not accessible to (View only) users but no permission to change data.
4) Write (2) - Administrators or users with permissions to change data.

Please note that groups are arbitrary, you can create as many as you want. The meaning of the groups and their permissions are for you to define.

## User Interface

To simplify your interaction with your **CoFlows** project, we have created a user interface. Let's start with a few steps.

1) In the bin folder, execute the server.sh or bat/server.bat scripts. Read more in the README of this repo.

2) Open a browser and go to http://localhost and login with username "root" and password "123".

![login](docs/images/login.png)

alternatively you could create a new user by clicking on "Create an account!"

![login](docs/images/create_user.png)

3) Once logged in, you will see all the workflow hosted by this container. In this example, there is only one workflow called "Workflow". 

![dashboard](docs/images/dashboard.png)

4) By click on the top right corner "root" you are able to logout, change password and see the user profile. In the user profile, you are able to change first and last names and most importantly see the User Secret. This Secret is used to authenticate user permissions when connecting through a WebAPI. 

![dashboard](docs/images/CoFlows_profile_2.png)

5) Closing the profile window and going back to the dashboard you can click on the Workflow to enter it. This first view will let you see the Agents and Queries available in the workflow. This view also has multiple tabs which are discussed below.

![workflow](docs/images/workflow.png)

6) The Source tab lets you browse the Base code of workflow. The base code can be thought of as libraries used by both Agents and Queries. Please note that this code is read-only. To edit these libraries, you must do so outside of this UI.

![workflow](docs/images/workflow_source.png)

7) View the permissions for this Workflow. As stated above, Fixed permissions are readonly and cannot be changed in the UI. The Group permissions are managed in the block below.

![permissions](docs/images/workflow_permissions.png)

9) To add a user to a Group you need to specify their email. The user must have an account on this container with the same email you specify.

![permissions](docs/images/workflow_newuser.png)

10) Groups can be added easily too.

![permissions](docs/images/workflow_newgroup.png)

11) Changing which group you want to manage can be done through select box like so. The Master group is the group for the entire Workflow. All users in the Workflow must be added to this group. Users can only be added to the "sub" groups once they are part of the master group.

![permissions](docs/images/workflow_subgroups.png)

## License 
The MIT License (MIT)
Copyright (c) Arturo Rodriguez All rights reserved.
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
