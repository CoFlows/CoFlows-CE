VB Agent
===
This is a generic example of VB agent following the generic structure within **CoFlows**.

    Imports System
    Imports System.Collections.Generic
    Imports QuantApp.Engine
    Imports QuantApp.Kernel
    Imports Newtonsoft.Json.Linq

    Public Class Agent
        Private Shared workspaceID As String = "$WID$"

        Public Shared Sub Load(data() As object) 
        End Sub

        public Shared Sub Add(id As String, data As object)
        End Sub

        public Shared Sub Exchange(id As String, data As object) 
        End Sub

        public Shared Sub Remove(id As String, data As object)
        End Sub

        public Shared Function Body(data As object) As object
            Dim cmd = JObject.Parse(data.ToString())
            If cmd.ContainsKey("Data") And cmd.Item("Data") = "Initial Execution" Then
                Console.WriteLine("     Agent Initial Execute @ " + DateTime.Now.ToString())
            End If

            Return data
        End Function

        public Shared Sub Job(datetime As DateTime, command As string)
        End Sub

        public Shared Function pkg() As FPKG
            Return new FPKG(
                workspaceID + "-Agent", 'ID
                workspaceID, 'Workflow ID
                "VB Agent", 'Name
                "VB Agent", 'Description
                Nothing, 'MID
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
