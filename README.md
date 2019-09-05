# CoFlows Community Edition

**CoFlows Community Edition** is a polyglot compute engine that simplifies the workflow of creating and deploying powerful data-centric applications. **CoFlows** enables developers to create rich **Web-APIs** with almost **no boiler plate code** and scheduled / reactive processes through a range of languages including CoreCLR (C#, F# and VB), JVM (Java and Scala), Python and javascrict.

**CoFlows'** polyglot functionality allows developers to built complex workflows leveraging off great open-source libraries written in various languages. The wealth of distributed computing libraries of Java / Scala together with Python's data science tools are all available in order to use the right tool for the right purpose.

Projects in **CoFlows** are called Workspaces. They contain the logic that defines the web apis and scheduled / reactive processes togehter with the definition of the entire environment including Nuget, Jar and Pip packages that the Workspace depends on. For furthere information please read [Workspace](docs/WorkspaceStructure.md "Workspace").

A number of APIs are available for developers to use but a notable one is the **M** set which is a NoSQL database in the **QuantApp.Kernel** environment. For more information please see [M](docs/M.md "M").

## Setup  
Install the docker cli tools for Linux congtainers. Pull the docker public quantapp/coflows:ce image.

    docker pull quantapp/coflows:ce

Download a **CoFlows** package from a sample repo or create your own package. If you create your own package from scratch please read [Workspace](docs/WorkspaceStructure.md "Workspace"). Ensure a file called _quantapp_config.json_ exists in the folder you are running **CoFlows** in.

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
        }
    }

## Running  
To run the local server just execute the server.sh script of your projects _bin_ folder or type:  

    docker run -v $(pwd)/mnt:/App/mnt -p 80:80 -t quantapp/coflows:ce

when logging in please use:  

    Username: root
    Password: 123

## GitHub Link
Link your **CoFlows** server to automatically pull changes to a project hosted on a GitHub repo by following the instructions of the following link [GitHub](docs/GitLink.md "GitHub").


## License 
The MIT License (MIT)
Copyright (c) 2007-2019, Arturo Rodriguez All rights reserved.
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.