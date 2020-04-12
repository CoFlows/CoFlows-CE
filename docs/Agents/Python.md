Python Agent
===
This is a generic example of Python agent following the generic structure within **CoFlows**.

Note: The python <-> CoreCLR interop is achieved through **PythonNet**.

    import clr

    import System
    import QuantApp.Kernel as qak
    import QuantApp.Engine as qae

    defaultID = "xxx"
    mid = "xxx-MID" #Listener

    def Add(id, data):
        System.Console.WriteLine("Python Add: " + str(id) + " --> " + str(data))
        
    def Exchange(id, data):
        System.Console.WriteLine("Python Exchange: " + str(id) + " --> " + str(data))
        
        
    def Remove(id, data):
        System.Console.WriteLine("Python Remove: " + str(id) + " --> " + str(data))
        
    def Load(data):
        System.Console.WriteLine("Python Loading: " + str(data))
        
    def Body(data):
        System.Console.WriteLine("Python Body: " + str(data))

    def Job(timestamp, data):
        System.Console.WriteLine("Python Job: " + str(timestamp) + " --> " + str(data))

    def pkg():
        return qae.FPKG(
        defaultID, #ID
        "Hello_World_Workflow", #Workflow ID
        "Hello Python Agent", #Name
        "Hello Python Analytics Agent Sample", #Description
        mid, #M ID Listener
        qae.Utils.SetFunction("Load", qae.Load(Load)), 
        qae.Utils.SetFunction("Add", qak.MCallback(Add)), 
        qae.Utils.SetFunction("Exchange", qak.MCallback(Exchange)), 
        qae.Utils.SetFunction("Remove", qak.MCallback(Remove)), 
        qae.Utils.SetFunction("Body", qae.Body(Body)), 
        "0 * * ? * *", #Cron Schedule
        qae.Utils.SetFunction("Job", qae.Job(Job))
        )