Source Code
===

The **CoFlows** project is based on a conglomerate of projects aimed to achieve a common goal.

## QuantApp.Kernel
Is the base project that contains logic to manage permissions, users, groups and the [M](M.md "M") set. Furthermore, The Kernel contains the logic that links the CoreCLR together with Python through a fork of the [PythonNet](https://github.com/pythonnet/pythonnet "PythonNet") library and the JVM link library.

## QuantApp.Engine
Contains the codebase that builds and executes the polyglot projects, **CoFlows** Workflows. The **QuantApp.Engine** compiles the files in the **Base** folder in the order specified in the _package.json_ file. 

For example in case of the CLR code, if the developers declares code in C# and then F# that is dependent on the C# code, the library will first create a dll for the C# code and then a dll for the F# that is dependent on the C# dll. This process continues iteratively until all the files are compiled.

The same logic holds when building JVM code. 

Please note that the polyglot link happens during runtime so compilation of CLR, JVM and Python happen independent from each other and order of build is irrelevant.

This library also contains the definitions of the Agents.

## CoFlows.Server
Manages the executable application which fires up the web server and hosts the Web API endpoints.

## QuantApp.Client
Angular 7 based client used to manage and interact with the **CoFlows** environment.