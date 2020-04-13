Javascript Agent
===
This is a generic example of Javascript agent following the generic structure within **CoFlows**.

Note: The Javascript <-> CoreCLR interop is achieved through then **Jint** Nuget package.

    let log = System.Console.WriteLine
    var qkernel = importNamespace('QuantApp.Kernel')
    var qengine = importNamespace('QuantApp.Engine')
    var console = importNamespace('System.Console')

    let defaultID = 'xxx'

    let pkg = new qengine.FPKG(
        defaultID, //ID
        'Hello_World_Workflow', //Workflow ID
        'Hello Js Agent', //Name
        'Hello Js Analytics Agent Sample', //Description
        'xxx-MID', //VB Listener
        jsWrapper.SetLoad('Load', 
            function(data){
                log('JS Load: ' + data)
            }),

        jsWrapper.SetCallback('Add', 
            function(id, data){
                log('JS Add: ' + id + ' ' + String(data.Name))//.Name)
            }), 

        jsWrapper.SetCallback('Exchange', 
            function(id, data){
                log('JS Exchange: ' + id + ' ' + data)
            }), 

        jsWrapper.SetCallback('Remove', 
            function(id, data){
                log('JS Remove:' + id + ' ' + data)
            }), 

        jsWrapper.SetBody('Body', 
            function(data){
                log('JS Body: ' + data)
                return data;
            }), 

        '0 * * ? * *', //Cron Schedule
        jsWrapper.SetJob('Job', 
            function(date, data){
                log('JS Job: ' + date + ' ' + data)
            })
        )