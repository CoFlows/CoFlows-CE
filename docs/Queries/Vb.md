VB Query example
===

    Public Class VBQuery    

        Public Shared Function getName() As String
            Return "something"
        End Function

        Public Shared Function Add(x as Integer, y as Integer) As Integer
            Return x + y
        End Function
    End Class

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
