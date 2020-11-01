' *
' * The MIT License (MIT)
' * Copyright (c) Arturo Rodriguez All rights reserved.
' * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
' * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
' * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
' *

Imports System
Imports System.Collections.Generic
Imports QuantApp.Engine
Imports QuantApp.Kernel
Imports Newtonsoft.Json.Linq

Public Class XXX
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
            Console.WriteLine("     XXX Initial Execute @ " + DateTime.Now.ToString())
        End If

        Return data
    End Function

    public Shared Sub Job(datetime As DateTime, command As string)
    End Sub

    public Shared Function pkg() As FPKG
        Return new FPKG(
            workspaceID + "-XXX", 'ID
            workspaceID, 'Workflow ID
            "VB XXX Agent", 'Name
            "VB XXX Agent", 'Description
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
