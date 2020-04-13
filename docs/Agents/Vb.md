VB Agent
===
This is a generic example of VB agent following the generic structure within **CoFlows**.

    Imports System
    Imports QuantApp.Engine
    Imports QuantApp.Kernel


    Public Class VBAgent
        Public Class Entry
            Public Name As String  
            Public DateStr As String  
        End Class  

        Private Shared defaultID As String = "xxx"

        Public Shared Sub Load(data() As object) 
            ' Console.WriteLine("VBAgent Agent Load")
        End Sub

        public Shared Sub Add(id As String, data As object)
            Console.WriteLine("VBAgent Agent Add: " + id + " " + data.ToString())
        End Sub

        public Shared Sub Exchange(id As String, data As object) 
            Console.WriteLine("VBAgent Agent Exchange: " + id)
        End Sub

        public Shared Sub Remove(id As String, data As object)
            Console.WriteLine("VBAgent Agent Remove: " + id)
        End Sub

        public Shared Function Body(data As object) As object
            Console.WriteLine("VBAgent Body " + data.ToString())
            Return data
        End Function


        public Shared Sub Job(datetime As DateTime, command As string)
            Console.WriteLine("VBAgent Agent Job")
        End Sub

        
                

        public Shared Function pkg() As FPKG
            Return new FPKG(
                defaultID, 'ID
                "Hello_World_Workflow", 'Workflow ID
                "Hello VB Agent", 'Name
                "Hello VB Analytics Agent Sample", 'Description
                "xxx-MID", 'Listener
                Utils.SetFunction("Load", new Load(AddressOf Load)), 
                Utils.SetFunction("Add", new MCallback(AddressOf Add)), 
                Utils.SetFunction("Exchange", new MCallback(AddressOf Exchange)), 
                Utils.SetFunction("Remove", new MCallback(AddressOf Remove)), 
                Utils.SetFunction("Body", new Body(AddressOf Body)), 
                "0 * * ? * *", 'Cron Schedule
                Utils.SetFunction("Job", new Job(AddressOf Job))
                )
        End Function
    End Class
