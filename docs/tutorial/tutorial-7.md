# Tutorial 3 - Agents, scheduled and asynchronous workflows

This tutorial builds on the [second tutorial](tutorial-2.md) and explains how to create an API that depends on libraries like **[pips](https://pypi.org/project/pip/), [nugets](https://www.nuget.org) or [jars](https://maven.apache.org)** with [**CoFlows CE (Community Edition)**](https://github.com/QuantApp/CoFlows-CE). 


Through the terminal, enter into the **bin** folder if you are using linux/macos or alternative enter the **bin/bat** folder if you are using windows.

## APIs
To add an API you must first create a new **Query** which in the context of CoFlows is a collection of APIs. Please run the following command from the within the **bin** or **bin/bat** folder as mentioned above:

    linux/macos:    sh add.sh query (cs, fs, py, java, scala, js, vb) {name of query}
    windows:        add.bat query (cs, fs, py, java, scala, js, vb) {name of query}

This will create a new subfolder and a source file with two sample APIs:
* Add - This API takes two variables returns their sum
* Permission - This API returns the name of the current user and their permission to the existing group. More on groups below.

These two functions are automatically assigned web end-points (URLs) transforming them to Web APIs.

Lets run an example step by step to create a Python API assuming we are using a macos based machine.

    sh add.sh query py pyapi

your folder structure should now look as follows:

    📦/
    ┣ 📂Queries
    ┃ ┗ 📜pyapi.py

The pyapi.py looks like this:

    ### <api name="Add">
    ###     <description>Function that adds two numbers</description>
    ###     <returns>returns an integer</returns>
    ###     <param name="x" type="integer">First number to add</param>
    ###     <param name="y" type="integer">Second number to add</param>
    ###     <permissions>
    ###         <group id="$WID$" permission="read"/>
    ###     </permissions>
    ### </api>
    def Add(x, y):
        return x + y


    ### <api name="Permission">
    ###     <description>Function that returns a permission</description>
    ###     <returns> returns an string</returns>
    ###     <permissions>
    ###         <group id="$WID$" permission="view"/>
    ###     </permissions>
    ### </api>
    def Permission():
        quser = qak.User.ContextUser
        permission = qak.User.PermissionContext("$WID$")
        if permission == qak.AccessType.Write:
            return quser.FirstName + " WRITE"
        elif permission == qak.AccessType.Read:
            return quser.FirstName + " READ"
        elif permission == qak.AccessType.View:
            return quser.FirstName + " VIEW"
        else:
            return quser.FirstName + " DENIED"

where both of these function here are assigned API end-points. In this code there is a variable $WID$ which automatically is set to the ID of the Workflow. This might be as good a time as any to mention groups and permissions in **CoFlows**. 

### Permissions
Each Workflow has a unique **ID** which is visible in the **package.json** folder. The **package.json** file fully declares the Workflow and it's dependencies. Please note that the Workflows **ID** is automatically set by **CoFlows** if the ID entry is empty in the **package.json** file. 

The **ID** also defines a group which is used to declare permissions / authorisations to the APIs. These permissions are used in two places in the example above.

First, the Permission function calls the PermissionContext library to get the permission of the user that is currently logged in:

    permission = qak.User.PermissionContext("$WID$")

Secondly, the permissions are used in the meta-data of both functions to define who may access these functions as APIs:

    ###     <permissions>
    ###         <group id="$WID$" permission="view"/>
    ###     </permissions>

It is possible to create multiple groups and assign different permissions to each group. User's are deemd to have access if they are part of a group and at least have the specified permission in this group.

There are four different permission levels:
* Denied (-1)
* View (0)
* Read (1)
* Write (2)

The numbers linked to each permission define the permission's value and in the example above, the minimum permission a user must have in the $WID$ group is **Read**. If a user is not part of the group, the user has no access. If the user has the **View** permission, the user has no access because **View** < **Read**. If the user has the **Read** or **Write** permission, then the user has access and is allowed to call the API.

Now that we have an understanding of these principles, lets run the code by typing:

    sh server.sh

This will generate the following output:

    CoFlows CE - NetCoreApp 3.1... Python starting... QuantApp Server 11/01/2020 12:17:17
    DB Connected
    Local deployment
    Workflow-Clean started
    SSL encryption is not used....
    warn: Microsoft.AspNetCore.DataProtection.Repositories.FileSystemXmlRepository[60]
        Storing keys in a directory '/root/.aspnet/DataProtection-Keys' that may not be persisted outside of the container. Protected data will be unavailable when container is destroyed.
    warn: Microsoft.AspNetCore.DataProtection.KeyManagement.XmlKeyManager[35]
        No XML encryptor configured. Key {5ce74f79-603d-4145-bd52-f3f3b2489778} may be persisted to storage in unencrypted form.

In this example the $WID$ value was automatically set to 9a7adf48-183f-4d44-8ab2-c0afd1610c71 which can be seen below.

The go to the http://localhost url and you will see:

![](../images/CoFlows_login.png)

Login with 
* Username: root
* Password: 123

and you will see:
![](../images/CoFlows_dashboard.png)

now click on the **pyapi.py** link in the page below:
![](../images/CoFlows_workbook.png)

to see the source code view of the query:
![](../images/CoFlows_query_source.png)

finally you will be able to run the query to see the values of all apis / functions that take no paramteres. In the this case it is only the **Permission** API since **Add** requires to parameters:
![](../images/CoFlows_query_run.png)

The end-points to these APIs are now visible and are for the **Permission** API:
http://localhost/flow/query/9a7adf48-183f-4d44-8ab2-c0afd1610c71/pyapi.py/Permission?_cokey=26499e5e555e9957725f51cc4d400384

and for the **Add** API:
http://localhost/flow/query/9a7adf48-183f-4d44-8ab2-c0afd1610c71/pyapi.py/Add?p[0]=X&p[[1]=Y&_cokey=26499e5e555e9957725f51cc4d400384
where X and Y are any numbers.

Using curl you are able to test this API

    curl "http://localhost/flow/query/9a7adf48-183f-4d44-8ab2-c0afd1610c71/pyapi.py/Add?p[0]=1&p[1]=2&_cokey=26499e5e555e9957725f51cc4d400384"

for the sake of security, it is preferable to include the **cokey** in the hearder instead of the URL 

    curl -H "_cokey: 30be80ea-835b-4524-a43a-21742aae77fa" "http://localhost/flow/query/9a7adf48-183f-4d44-8ab2-c0afd1610c71/pyapi.py/Add?p[0]=1&p[1]=2"

Please note that in the format of the URL is

    http://localhost/flow/query/{$WID$}/{Query}/{API}?p[0]=1&...p[n]=2&_cokey=XXX

where **p[0],...,p[n]** represent the parameters of the function / API. A more secure alternative is

    http://localhost/flow/query/{$WID$}/{Query}/{API}?p[0]=1&...p[n]=2

with the header "_cokey: XXX".


## Secret Keys
As you may have noticed in the URLs for the API end-points above, there is an extra parameter called the **_cokey**. The **_cokey** parameter is an identification key that allows APIs calls withouth the need to go through the standard login process and aqcuire a JWT token.

The secret key is visible in the profile which you access by click in the to right corner
![](../images/CoFlows_profile_1.png)
and then select profile:
![](../images/CoFlows_profile_2.png)

The secret key should be kept secret as it offers access to the system through the related user. This key can also be re-generated by click on the yellow button above.


## Build
Build the workflow

    sh build.sh local

This steps populats the Base, Agents and Queries sections of the **package.json** file. It also pulls all dependencies declared in the nuget, jars and pip sections of the **package.json** entries. Note that F# requires Base files to be ordered according to their dependencies. If a F# base files depends on another F# file please make sure that the the files are ordered accordingly.

## Execute
Run the server in local mode

    sh server.sh localhost.json

## Notes:
* If ID in package is empty then build.sh will auto generate an ID
* build.sh will add new entries to the Files, Queries, Agents and Base sections of the package.json. It will not delete entries, this needs to be done manually.
* $WID$ is code to populate the Workflow ID in queries and agents. NOTE: Files will be overwritten once with the replacements.
* Java Agents require GSON and JSON jars
  

## Dash App
Add new App

## Agents
Add new Agents

    sh add.sh agent (cs, fs, py, java, scala, js, vb)

Startup scripts



Folder

    📦/
    ┣ 📂Files
    ┃ ┗ 📜my.azureauth
    ┣ 📂bin
    ┃ ┣ 📂bat
    ┃ ┃ ┣ 📜aci_deploy.bat
    ┃ ┃ ┣ 📜aci_remove.bat
    ┃ ┃ ┣ 📜add.bat
    ┃ ┃ ┣ 📜build.bat
    ┃ ┃ ┣ 📜cloud_deploy.bat
    ┃ ┃ ┣ 📜cloud_log.bat
    ┃ ┃ ┣ 📜cloud_remove.bat
    ┃ ┃ ┣ 📜cloud_restart.bat
    ┃ ┃ ┣ 📜query.bat
    ┃ ┃ ┗ 📜server.bat
    ┃ ┣ 📜aci_deploy.sh
    ┃ ┣ 📜aci_remove.sh
    ┃ ┣ 📜add.sh
    ┃ ┣ 📜build.sh
    ┃ ┣ 📜cloud_deploy.sh
    ┃ ┣ 📜cloud_log.sh
    ┃ ┣ 📜cloud_remove.sh
    ┃ ┣ 📜cloud_restart.sh
    ┃ ┣ 📜query.sh
    ┃ ┣ 📜query_custom.sh
    ┃ ┗ 📜server.sh
    ┣ 📂jars
    ┃ ┗ 📜.gitignore
    ┣ 📂nugets
    ┃ ┗ 📜.gitignore
    ┣ 📂pip
    ┃ ┗ 📜.gitignore
    ┣ 📜LICENSE
    ┣ 📜README.md
    ┣ 📜coflows_config.json
    ┣ 📜localhost.json
    ┗ 📜package.json