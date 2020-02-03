# CoFlows - Polyglot Runtime (Interop)

**CoFlows Community Edition** is a polyglot runtime that simplifies the development, hosting and deployment of powerful data-centric workflows. **CoFlows** enables developers to create rich **Web-APIs** with almost **zero boiler plate** and scheduled / reactive processes through a range of languages including CoreCLR (C#, F# and VB), JVM (Java and Scala), Python and javascript. Furthermore, functions written in any of these languages can call each other within the same process with **full interop**.

Our **Community Edition** is a version of our commercial codebase that we have used in various Data science projects including:
* Algorithmic Trading
* Vessel tracking and commodity trade flow projections
* Macro-economic risk management
* Global equity selection strategy simulations
* Healthcare cost and clinical segmentation analysis on national data

For more information visit https://www.quant.app

This edition will be tightly coupled with the **CoFlows Cloud** making it much easier for developers to deploy and host their applications. More information on this coming soon.

**CoFlows'** polyglot functionality allows developers to build complex workflows leveraging off great open-source libraries written in various languages. The wealth of distributed computing libraries of Java / Scala together with Python's data science tools are all available in order to use the right tool for the right purpose in the same process!

Furthermore, our aim with **CoFlows** is to offer simplicity for **Data Scientists** to quickly build self-contained projects while leveraging off popular tools. To this end, once a developer pulls the **CoFlows** image a range of tools are at their disposal as first class citizens of the **CoFlows** ecosystem:
* DotNet Core 3.0
* Python 3.7.4
* Java 1.8
* Scala 2.11.8
* JupyterLab

... and yes, it is a big image taking 900Mb to download and 1.9Gb of space during runtime.

Projects in **CoFlows** are called Workspaces. They contain the logic that defines the Web APIs and scheduled / reactive processes together with the definition of the entire environment including Nuget, Jar and Pip packages that the Workspace depends on. For further information please read [Workspace](docs/Workspace.md "Workspace").

A number of APIs are available for developers to use but a notable one is the **M** set which is a **NoSQL persistent and distributed list** in the **QuantApp.Kernel** environment. For more information please see [M](docs/M.md "M").

## Polygot
Let's start with a definition. According to Wikipedia, in computing, a polyglot is a computer program or script written in a valid form of multiple programming languages, which performs the same operations or output independent of the programming language used to compile or interpret it.

There are multiple sources pushing both the pros and cons of polyglot systems and this is not the place to discuss it. We believe that the ability to express algorithms in the correct language and using the best libraries for the right task is essential. **CoFlows** uses the **QuantApp.Engine** and **QuantApp.Kernel** libraries to offer interop between:
* CoreCLR: C#, F# & VB
* JVM: Java & Scala
* Python
* Javascript

### Notes
The Python link is achieved through a fork of the wonderful library [PythonNet](https://github.com/pythonnet/pythonnet "PythonNet").

The link between the **JVM** and **CLR** is achieved through the [QuantApp.Kernel/JVM](https://github.com/QuantApp/CoFlows-CE/tree/master/QuantApp.Kernel/JVM "QAJVM") libraries which have been developed from scratch for this project.

Javascript interpretation is achieved using the great [Jint](https://github.com/sebastienros/jint "Jint") library.

For further details please read [Polyglot](docs/Polyglot/General.md "Polyglot")

## Setup  
Install the docker cli tools for Linux containers. Pull the docker public coflows/ce image.

    docker pull coflows/ce

Alternatively you can pull a windows core image coflows/ce-win.

    docker pull coflows/ce-win

Download a **CoFlows** package from a sample repo or create your own package. If you create your own package from scratch please read [Workspace](docs/Workspace.md "Workspace"). Ensure a file called _quantapp_config.json_ exists in the folder you are running **CoFlows** in.

    quantapp_config.json
    {
        "Database": "mnt/database.db",
        "Workspace": "mnt/package.json",
        "Server":{
            "Host": "localhost", //Set the host name
            "SecretKey": "26499e5e555e9957725f51cc4d400384", //User key used for Jypter Labs - No need to change
            "SSL":{
                "Cert": "", //Set a name for the pfx file containing the certificate
                "Password": "" //Set the password for the pfx file
            }
        },
        "Cloud":{
            "Host": "coflows.quant.app", //Set the cloud host name
            "SecretKey": "xxx", //Set your cloud secret key (login to CoFlows Cloud, then at the top right click on your name, profile and your secret key will appear)
            "SSL": true
        },
        "AzureContainerInstance": {
            "AuthFile":"mnt/Files/my.azureauth",
            "Dns": "coflows-container",
            "Region": "UKSouth",
            "Cores": 4,
            "Mem": 4,
            "Gpu": {
                "SKU": "",
                "Cores": 1
            }
        }
    }

## Running  
To run the local server you first need a workspace. Either create your own or download a template. Then just execute the server.sh script of your workspace's _bin_ folder or type:  

    docker run -v $(pwd)/mnt:/app/mnt -p 80:80 -t coflows/ce

when logging in please use:  

    Username: root
    Password: 123

## GitHub Link
Link your **CoFlows** server to automatically pull changes to a project hosted on a GitHub repo by following the instructions of the following link [GitHub](docs/GitLink.md "GitHub").


## CoFlows CLI

            Cloud:

                -Deploy a container to the cloud
                 unix: bin/cloud_deploy.sh cloud
                 win:  bin/bat/cloud_deploy.bat cloud

                -Buid the in the cloud
                 unix: bin/build.sh cloud
                 win:  bin/bat/build.bat cloud

                -Print logs of container in the cloud
                 unix: bin/cloud_log.sh
                 win: bin/bat/cloud_log.bat

                -Restart container in the cloud
                 unix: bin/cloud_restart.sh
                 win: bin/bat/cloud_restart.bat

                -Remove container in the cloud
                 unix: bin/cloud_remove.sh
                 win: bin/bat/cloud_remove.bat

                -Execute query in the cloud
                 unix: bin/query.sh local query_id function_name  parameters[0] ... parameters[n]
                 win:  bin/bat/query.bat local query_id function_name  parameters[0] ... parameters[n]

                -Execute query locally (custom quantapp_config.json file)
                 unix: bin/query_customg.sh {custom_quantapp_config.json} cloud query_id function_name  parameters[0] ... parameters[n]
                 win:  bin/bat/query_custom.bat {custom_quantapp_config.json} cloud query_id function_name  parameters[0] ... parameters[n]

            Local:

                -Buid the code locally
                 unix: bin/build.sh local
                 win:  bin/bat/build.bat local

                -Execute query locally
                 unix: bin/query.sh local query_id function_name  parameters[0] ... parameters[n]
                 win:  bin/bat/query.bat local query_id function_name  parameters[0] ... parameters[n]

                -Execute query locally (custom quantapp_config.json file)
                 unix: bin/query_customg.sh {custom_quantapp_config.json} local query_id function_name  parameters[0] ... parameters[n]
                 win:  bin/bat/query_custom.bat {custom_quantapp_config.json} local query_id function_name  parameters[0] ... parameters[n]


                -Server
                 unix: bin/server.sh
                 win: bin/bat/server.bat

            Azure Container Instances:

                -Deploy to an Azure Container Instance
                 unix: bin/azure_deploy.sh local
                 win:  bin/bat/azure_deploy.bat local

                -Remove an Azure Container Instance
                 unix: bin/azure_remove.sh
                 win:  bin/bat/azure_remove.bat


## License 
The MIT License (MIT)
Copyright (c) 2007-2019, Arturo Rodriguez All rights reserved.
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.