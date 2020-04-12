F# Query example
===

    module FsQuery
        
        let getName = "something"

        let Add x y = x + y

## Web API 1

### Get

    http(s)://[host]/m/getwb?workbook=[WorkflowID]&id=[QueryID]&name=getName

### Result

    something

## Web API 2

### Get

    http(s)://[host]/m/getwb?workbook=[WorkflowID]&id=[QueryID]&name=Add&p[0]=100&p[1]=200

### Result

    300