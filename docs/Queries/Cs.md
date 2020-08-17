C# Query example
===

    public class CsQuery
    {   
        public static string getName()
        {
            return "something";
        }

        public static int Add(int x, int y)
        {
            return x + y;
        }
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

