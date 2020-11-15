'''
 * The MIT License (MIT)
 * Copyright (c) Arturo Rodriguez All rights reserved.
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 '''
 
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
        print('     XXX Initial Execute @ : ' + str(datetime.datetime.now()))

    return json.dumps(cmd)

def Job(timestamp, data):
    pass

def pkg():
    return qae.FPKG(
    workspaceID + "-XXX", #ID
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