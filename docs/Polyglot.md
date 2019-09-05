Polyglot
===

The interop functionality in **CoFlows** is achieved through a open-source projects. **CoreCLR** is the main execution environment that loads both the JVM and Python environments and also interprets Javascript. The Python environment is loaded through a fork of the **PythonNet** library while the JVM is loaded through the **QuantApp.Kernel/JVM** libraries. Please note that the **PythonNet** fork was incorporated into the **QuantApp.Kernel/Python** package for additional integration.