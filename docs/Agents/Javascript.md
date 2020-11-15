Javascript Agent
===
This is a generic example of Javascript agent following the generic structure within **CoFlows**.

Note: The Javascript <-> CoreCLR interop is achieved through then **Jint** Nuget package.


    var qengine = importNamespace('QuantApp.Engine')

    let workspaceID = '$WID$'

    let pkg = new qengine.FPKG(
        workspaceID + '-Agent', //ID
        workspaceID, //Workflow ID
        'Javascript Agent', //Name
        'Javascript Agent', //Description
        null, //MID
        jsWrapper.Load('Load', function(data){ }),
        jsWrapper.Callback('Add', function(id, data){ }), 
        jsWrapper.Callback('Exchange', function(id, data){ }), 
        jsWrapper.Callback('Remove', function(id, data){ }), 
        jsWrapper.Body('Body', function(data){ 
            if('Data' in data && data['Data'] == 'Initial Execution')
                log('     Agent Initial Execute @ ' + Date(Date.now()).toString())
            return data 
        }), 
        '0 * * ? * *', //Cron Schedule
        jsWrapper.Job('Job', function(date, data){ })
        )