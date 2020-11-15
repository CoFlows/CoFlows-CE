# Tutorial 2 - APIs with third-party dependencies

This tutorial builds on the [first tutorial](tutorial-1.md) and explains how to create an API that depends on libraries like **[pips](https://pypi.org/project/pip/), [nugets](https://www.nuget.org) or [jars](https://maven.apache.org)** with [**CoFlows CE (Community Edition)**](https://github.com/QuantApp/CoFlows-CE). 

Through the terminal, enter into the **bin** folder if you are using linux/macos or alternative enter the **bin/bat** folder if you are using windows.


## Add dependencies
To add a dependency you can run the following commands:
    
    linux/macos:    
    sh add.sh pip { name of pip }
    sh add.sh jar { url of jar }
    sh add.sh nuget { name of nuget } { version of nuget }

    windows:
    add.bat pip { name of pip }
    add.bat jar { url of jar }
    add.bat nuget { name of nuget } { version of nuget }

As an example, we will build on the previous Python example and add an API that executes operations on a Panda DataFrame. Start with:

    sh add.sh pip pandas

then insert the function to the previous query **pyapi.py** 

    import pandas as pd
    ### <api name="Panda">
    ###     <description>Function that uses a dataframe's group by</description>
    ###     <returns>returns an string</returns>
    ###     <param name="_table" type="JSON">table</param>
    ###     <permissions>
    ###         <group id="9a7adf48-183f-4d44-8ab2-c0afd1610c71" permission="read"/>
    ###     </permissions>
    ### </api>
    def Panda(_table):
        table = json.loads(_table)

        df = pd.DataFrame(list(zip(table['x'], table['y'])), columns =['X', 'Y']).groupby(['X']).count().rename(columns={ 'Y': 'count' })

        return df.to_string()

This API can be called through the following command:

    curl -X POST -d '{"x":["x1", "x2", "x1", "x3", "x1", "x2"], "y":["y1", "y2", "y3", "y4", "y5", "y6"]}' -H "Content-Type: application/json" -g "http://localhost/flow/query/9a7adf48-183f-4d44-8ab2-c0afd1610c71/pyapi.py/Panda2?_cokey=30be80ea-835b-4524-a43a-21742aae77fa"


The function takes the variables to creates the following DataFrame internally:

        X   Y
    0  x1  y1
    1  x2  y2
    2  x1  y3
    3  x3  y4
    4  x1  y5
    5  x2  y6

the executes a groupby command and the result is:

        count
    X        
    x1      3
    x2      2
    x3      1

## Next Tutorial
Please continue on to the [Third Tutorial](tutorial-3.md) to learn about **Agents** also known as **daemons** to create scheduled and asynchronouze workflows with [**CoFlows CE (Community Edition)**](https://github.com/QuantApp/CoFlows-CE). 

  