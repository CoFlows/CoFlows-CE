Python Query example
===

    def getName():
        return "something"
    
    def Add(x, y):
        return x + y


To define a private function start it's name with '__'

    def __somePrivateFunction():
        return "invisible"

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