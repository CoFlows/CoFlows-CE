Javascript Query example
===

    let getName = 'something'
    
    let Add = function(x, y) {
            return x + y
        }

## Web API 1

### Get

    http(s)://[host]/flow/getwb?workbook=[WorkflowID]&id=[QueryID]&name=getName

### Result

    something

## Web API 2

### Get

    http(s)://[host]/flow/getwb?workbook=[WorkflowID]&id=[QueryID]&name=Add&p[0]=100&p[1]=200

### Result

    300