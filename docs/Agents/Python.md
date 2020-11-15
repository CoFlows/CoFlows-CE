Python Agent
===
This is a generic example of Python agent following the generic structure within **CoFlows**.

Note: The python <-> CoreCLR interop is achieved through **PythonNet**.

    import clr

    import System
    import QuantApp.Kernel as qak
    import QuantApp.Engine as qae

    import json
    import datetime

    workspaceID = "$WID$"

    def Add(id, data):
        pass

    def Exchange(id, data):
        pass

    def Remove(id, data):
        pass
        
    def Load(data):
        pass
        
    def Body(data):
        cmd = json.loads(data)
        if 'Data' in cmd and cmd['Data'] == 'Initial Execution':    
            print('     Agent Initial Execute @ : ' + str(datetime.datetime.now()))

        return data

    def Job(timestamp, data):
        pass

    def pkg():
        return qae.FPKG(
        workspaceID + "-Agent", #ID
        workspaceID, #Workflow ID
        "Python XXX Agent", #Name
        "Python XXX Agent", #Description
        None, #M ID Listener
        qae.Utils.SetFunction("Load", qae.Load(Load)), 
        qae.Utils.SetFunction("Add", qak.MCallback(Add)), 
        qae.Utils.SetFunction("Exchange", qak.MCallback(Exchange)), 
        qae.Utils.SetFunction("Remove", qak.MCallback(Remove)), 
        qae.Utils.SetFunction("Body", qae.Body(Body)), 
        "0 * * ? * *", #Cron Schedule
        qae.Utils.SetFunction("Job", qae.Job(Job))
        )