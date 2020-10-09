import { Component, Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';

import { Router, CanActivate, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';

import { interval } from "rxjs"
import { takeWhile } from "rxjs/operators"


import { MessagingService } from './messaging.service';


@Component({
  selector: 'coflows',
  template: '',  
})


@Injectable()
export class CoFlowsComponent implements  CanActivate  {
    public coflows_server: string = ''

    private header : any = null
    
    public quser: any = null

    public static UpdateInstruments = false
     // Inject HttpClient into your component or service.
    constructor(private http: HttpClient, private router: Router, private msService: MessagingService) {
                
        this.coflows_server = (msService.secure ? 'https' : 'http') + '://' + msService.server + '/'

        // this.msService.subscribe(msg => this.ProcessMessage(msg))

        let storedToken = localStorage.getItem('CoFlows-CoFlowsJWT')
        // console.log('TOKEN LOAD: ' + storedToken)
        
        if(storedToken == null || storedToken == "" || storedToken == "null")
            this.header = null
        else
            this.header = new HttpHeaders().set('Authorization', `Bearer ` + storedToken)

        // console.log('Header Load:', this.header, storedToken)
        
        // this.msService.onClose(event => {
        //     console.log('connection closed', event, this.msService)
        //     // this.checkLogin(this.router.url)
        //     this.logout(true)
        // });
        
        interval(10000)
        .pipe(takeWhile(i => (this.quser != null && this.quser.User.Loggedin)))
        .subscribe(i => { 
            //if(this.quser != null && this.quser.User.Loggedin){
                this.Ping()
            //}
        })

        interval(10000)
        .pipe(takeWhile(i => !this.checking && !(this.quser != null && this.quser.User.Loggedin)))
        .subscribe(i => { 
            //if(this.quser == null || !this.quser.User.Loggedin){                
                // console.log('check login', this.quser, this.header)
                this.checkLogin(this.router.url)
            //}            
        })  
        
        interval(500)
        .subscribe(i => { 
            if(CoFlowsComponent.UpdateInstruments){
            CoFlowsComponent._Instruments.forEach((v, id) => {
                CoFlowsComponent.Instruments.get(id).Last = CoFlowsComponent._Instruments.get(id).Last
                CoFlowsComponent.Instruments.get(id).Bid = CoFlowsComponent._Instruments.get(id).Bid
                CoFlowsComponent.Instruments.get(id).Ask = CoFlowsComponent._Instruments.get(id).Ask
            });
        }})

    }

    canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): boolean {                
        CoFlowsComponent.UpdateInstruments = false
        if(state.url == "/authentication/signin"){
            return true
        }
        else{
            if(this.quser != null && this.quser.User.Loggedin){
                return true
            }
            else{            
                
                this.checkLogin(state.url)
                return false
            }
        }
    }

    private CheckUser(func : any) {
        let waiting = false
        let iid = setInterval(x => {
            try{
                if(!(this.quser != null && this.quser.User.Loggedin) && this.header != null && !waiting){
                    waiting = true
                    this.http.get(this.coflows_server + 'account/whoami', { headers: this.header })
                    .toPromise().then(response => {  
                        this.quser = response
                        waiting = false

                        this.msService.subscribe(msg => this.ProcessMessage(msg), this.quser.User.Session, func)
                        this.msService.onClose(event => {
                            console.log('connection closed', event, this.msService)
                            this.logout(true)
                        })

                        clearInterval(iid)
                        return

                        })
                    }

                if(!waiting && this.quser != null && this.quser.User.Loggedin){
                    func()
                    clearInterval(iid)
                }
            }
            catch(e){
                // console.log('checkuser', e)
                localStorage.setItem('CoFlows-CoFlowsJWT', null)
                clearInterval(iid)
            }

        }, 500);
    }

    checking = false
    checkLogin(url : string): void {
        // console.log('check user', this.header)
        this.checking = true
        if(this.router.url == '/authentication/signup' || this.router.url == '/authentication/forgot'){
            // console.log(this.quser)
            this.checking = false
            return
        }
        try{
            // console.log(this.header, this.quser)
            if(this.header == null){// || this.quser == null){
                this.quser = null
                this.header = null
                this.router.navigate ( [ '/authentication/signin' ] )
                localStorage.setItem('CoFlowsURL', url)
                localStorage.setItem('CoFlows-CoFlowsJWT', null)
                this.checking = false
                return
            }

            //if(this.header != null){// && this.quser != null){
            else{
                this.http.get(this.coflows_server + 'account/whoami', { headers: this.header })
                .toPromise().then(response => {  
                    this.quser = response
                    // console.log('login user', this.quser, this.header)
                    
                    if(this.quser != null && this.quser.User.Loggedin){
                        // if(this.msService.reOpen()){                
                            // location.rseload();
                            // this.msService.subscribe(msg => {this.ProcessMessage(msg);});
                            this.msService.subscribe(msg => this.ProcessMessage(msg), this.quser.User.Session, x => {})
                            this.msService.onClose(event => {
                                console.log('connection closed', event, this.msService)
                                // this.checkLogin(this.router.url)
                                this.logout(true)
                            })
                        // }

                        if(url == '/authentication/signin' || url == null)
                            url = '/'

                        this.router.navigate ( [ url ] );
                        this.checking = false
                        return true
                    }
                    else{
                        this.header = null
                        localStorage.setItem('CoFlows-CoFlowsJWT', null)
                        this.router.navigate ( [ '/authentication/signin' ] )
                        
                        if(url != '/authentication/signin' && url != null)
                            localStorage.setItem('CoFlowsURL', url);
                        this.checking = false
                        return false
                    }                   
                }).catch(err => {
                    // console.log(err)
                    this.quser = null
                    this.header = null
                    this.router.navigate ( [ '/authentication/signin' ] )
                    localStorage.setItem('CoFlowsURL', url)
                    localStorage.setItem('CoFlows-CoFlowsJWT', null)
                });
            }
            
        }
        catch(e){
            // console.log('checklogin', e)
            this.quser = null
            this.header = null
            this.router.navigate ( [ '/authentication/signin' ] )
            localStorage.setItem('CoFlowsURL', url)
            localStorage.setItem('CoFlows-CoFlowsJWT', null)
        }
        this.checking = false
    
    }

    message = null

    showMessage(message){
        this.message = message
        let iid = setInterval(x => {
          this.message = null
          clearInterval(iid)
        }, 2000)
      }

    oauth(token: string, url: string){
        if(!(token == null || token == "null" || token == "")){

            // console.log('--- SAVE token', token)
            // Read the result field from the JSON response.
            this.header = new HttpHeaders().set('Authorization', `Bearer ` + token);
            
            localStorage.setItem('CoFlows-CoFlowsJWT', token);
            
            this.http.get(this.coflows_server + 'account/whoami', { headers: this.header })
                .toPromise().then(response => {  
                    
                    this.quser = response

                    // console.log(' -- OAUTH ', this.quser.User)

                    if(url == '/authentication/signin' || url == null || url == 'null')
                        url = '/'

                    localStorage.setItem('CoFlowsURL', null);

                    this.msService.subscribe(msg => this.ProcessMessage(msg), this.quser.User.Session, x => {})
                    this.msService.onClose(event => {
                        console.log('connection closed', event, this.msService)
                        // this.checkLogin(this.router.url)
                        this.logout(true)
                    })

                    this.router.navigate ( [ url ] );
                });

            // if(url == '/authentication/signin' || url == null || url == 'null')
            //     url = '/'

            // localStorage.setItem('CoFlowsURL', null);
            // this.router.navigate ( [ url ] );       
        }
        else{
            this.quser = null;
            this.header = null;
            localStorage.setItem('CoFlows-CoFlowsJWT', null);
        }
    }

    login(name: string, pass: string, url : string, callback): void {
        this.http.post(this.coflows_server + 'account/login', {Username : name, Password: pass})
        .subscribe(
            (data : any) => {
                let token = data.token                
                if(!(token == null || token == "null" || token == "")){
                    // Read the result field from the JSON response.
                    this.header = new HttpHeaders().set('Authorization', `Bearer ` + token)
                    
                    localStorage.setItem('CoFlows-CoFlowsJWT', token)

                    
                    this.http.get(this.coflows_server + 'account/whoami', { headers: this.header })
                        .toPromise().then(response => {  
                            this.quser = response;

                            if(url == '/authentication/signin' || url == null || url == 'null')
                                url = '/'

                            localStorage.setItem('CoFlowsURL', null);

                            this.msService.subscribe(msg => this.ProcessMessage(msg), this.quser.User.Session, x => {})
                            this.msService.onClose(event => {
                                console.log('connection closed', event, this.msService)
                                // this.checkLogin(this.router.url)
                                this.logout(true)
                            })

                            this.router.navigate ( [ url ] );
                        });

                    return "";
                }
                else{
                    this.quser = null;
                    this.header = null;
                    localStorage.setItem('CoFlows-CoFlowsJWT', null);

                    callback("login error")
                }
            },
            err => {
                console.log(err)
                this.quser = null;
                this.header = null;
                localStorage.setItem('CoFlows-CoFlowsJWT', null);
                
                callback("login error")
            }
        );
    } 
    
    logout(navigate): void {

        this.http.get(this.coflows_server + 'account/logout', { headers: this.header })
        .toPromise().then(response => {  
            this.quser = response;

            this.quser = null
            this.header = null
            this.quser = null
            localStorage.setItem('CoFlows-CoFlowsJWT', null)
            if(navigate){
                this.router.navigate ( [ '/authentication/signin' ] )
                location.reload()
            }
        }).catch(err => {});

    } 

    changePassword(oldPassword: string, newPassword: string, success, fail): void {
        // console.log(this.quser)
        this.Post('administration/updatepassword',{ UserID:this.quser.User.ID, OldPassword: oldPassword, NewPassword: newPassword  },
        data => {
            if(data.Data == "ok"){
                success()
            }
            else
                fail(data.Data)
        });
    }
    

    Ping():void {  
        
        var mess = {
            Type: 15,
            Content: (new Date()).toJSON()
        }; 
        this.Send(mess);       
    };

    Send(message : any){
        this.msService.send(message);
    }

    Subscribe(id):void {
        var mess = {
            Type: 1,
            Content: id
        };
        this.msService.send(mess);
    };

    public static Instruments = new Map<string,any>();

    FindInstrument(id) : any {
        
        if (!CoFlowsComponent.Instruments.has(id)) {
            this.Subscribe(id);

            var instrument = {
                ID: id,
                Last: 0,
                Open: 0,
                Tick: 0,
                Executed: 0,
                High: 0,
                Low: 0,
                Volume: 0,
                OpenInterest: 0,
                AdjClose: 0,
                Bid: 0,
                Ask: 0,
                Close: 0,
                MarketCap: 0,

                IsActive: false,
                ScheduleCommand: '',
                LastPortfolioUpdate: new Date()
            };

            var _instrument = {
                ID: id,
                Last: 0,
                Open: 0,
                Tick: 0,
                Executed: 0,
                High: 0,
                Low: 0,
                Volume: 0,
                OpenInterest: 0,
                AdjClose: 0,
                Bid: 0,
                Ask: 0,
                Close: 0,
                MarketCap: 0
            };

            CoFlowsComponent.Instruments.set(id, instrument);
            CoFlowsComponent._Instruments.set(id, _instrument);            
        }

        return CoFlowsComponent.Instruments.get(id);        
    }

    private static _Instruments = new Map<string,any>();

    public static ClearInstruments(){
        CoFlowsComponent._Instruments = new Map<string,any>()
        CoFlowsComponent.Instruments = new Map<string,any>()
        CoFlowsComponent.UpdateInstruments = false
    }
    
    _FindInstrument(id) : any {            
        return CoFlowsComponent._Instruments.get(id);        
    }

    private Add = new Map<string,any>();
    private Exchange = new Map<string,any>();
    private Remove = new Map<string,any>();
    private Load = new Map<string,any>();

    ProcessMessage(event): void {
        var message = event;//JSON.parse(message_str);

        var content = message.Content;
        var counter = message.Counter;

        switch (message.Type) {
            case 1: // Subscribe
                break;

            case 2: // MarketData

                var id = content.InstrumentID;
                var date = Date.parse(content.Timestamp);
                var type = content.Type;
                var value = content.Value;

                var instrument = this._FindInstrument(id);

                if(instrument == undefined)
                    break;

                switch (type) {
                    case 1: //Last
                        instrument.Last = value;
                        break;
                    case 2: //Open
                        instrument.Open = value;
                        break;
                    case 3: //Tick
                        instrument.Tick = value;
                        break;
                    case 4: //Executed
                        instrument.Execute = value;
                        break;
                    case 5: //High
                        instrument.High = value;
                        break;
                    case 6: //Low
                        instrument.Low = value;
                        break;
                    case 7: //Volume
                        instrument.Volume = value;
                        break;
                    case 8: //OpenInterest
                        instrument.OpenInterest = value;
                        break;
                    case 9: //AdjClose
                        instrument.AdjClose = value;
                        break;
                    case 10: //Bid
                        instrument.Bid = value;
                        break;
                    case 11: //Ask
                        instrument.Ask = value;
                        break;
                    case 12: //Close
                        instrument.Close = value;
                        break;
                    case 13: //MarketCap
                        instrument.MarketCap = value;
                        break;
                }                                
                break;

            case 3: // StrategyData
                //console.log(message)
                break;

            case 4: // UpdateOrder
                
                // if(content.Order.Unit != 0 && content.Order.ExecutionLevel != 0){
                //     console.log(message)
                //     var id = content.Order.PortfolioID;
                //     var date = Date.parse(content.Timestamp);
                //     var type = content.Type;
                //     var value = content.Value;
    
                //     var instrument = this.FindInstrument(id);
    
                //     instrument.LastPortfolioUpdate = new Date()
                // }
                
                break;

            case 5: // UpdatePosition
                // console.log(message)
                // var id = content.Position.PortfolioID;
                // var date = Date.parse(content.Timestamp);
                // var type = content.Type;
                // var value = content.Value;

                // var instrument = this.FindInstrument(id);

                // instrument.LastPortfolioUpdate = new Date()
                break;

            case 6: // AddNewOrder
                // if(content.Order.Unit != 0 && content.Order.ExecutionLevel != 0){
                //     console.log(message)
                //     var id = content.Order.PortfolioID;
                //     var date = Date.parse(content.Timestamp);
                //     var type = content.Type;
                //     var value = content.Value;

                //     var instrument = this.FindInstrument(id);

                //     instrument.LastPortfolioUpdate = new Date()
                // }
                break;

            case 7: // AddNewPosition
                // console.log(message)
                // var id = content.Position.PortfolioID;
                // var date = Date.parse(content.Timestamp);
                // var type = content.Type;
                // var value = content.Value;

                // var instrument = this.FindInstrument(id);

                // instrument.LastPortfolioUpdate = new Date()
                break;

            case 8: // SavePortfolio
                console.log(message)
                
                break;

            case 9: // Property
                var content = message.Content
                var strategy = this.FindInstrument(content.ID)
                var propertyName = content.Name
                var value = content.Value
                
                if(propertyName == "ScheduleCommand"){
                    strategy.ScheduleCommand = value
                }
                
                break;

            case 10: // Function
                var content = message.Content
                var strategy = this.FindInstrument(content.ID)
                var functionName = content.Name
                var parameters = content.Parameters
                
                if(functionName == "StartSchedulerLocal"){
                    strategy.IsActive = true;
                }
                else if(functionName == "StopSchedulerLocal"){
                    strategy.IsActive = false;
                }
                else if(functionName == "SaveLocal"){
                    strategy.LastPortfolioUpdate = new Date()
                }

                break;

            case 11: // UpdateQueue
                console.log(message)
                break;

            case 12: // CreateAccount
                console.log(message)
                break;

            case 13: // CreateSubStrategy
                console.log(message)
                break;

            case 14: // CRUD Message

                var message = content;
                var data = message.Value;
                var obj = data;

                try {
                    obj = JSON.parse(data);
                }
                catch (err) { }

                //console.log(message, data, obj)
                

                obj = { Key: message.ID, Value: obj }

                if (message.Type == 1) { // Create
                    //obj = JSON.parse(data.Value);
                    //obj = JSON.parse(data);
                    if(this.Add.has(message.TopicID)) this.Add.get(message.TopicID)(obj);
                }
                else if (message.Type == 2) { // Read

                }
                else if (message.Type == 3) { // Update
                    if (this.UserData.has(message.TopicID)) {
                        // console.log(message);

                        this.UserData.get(message.TopicID).Value = obj;
                        if(this.Exchange.has(message.TopicID)) this.Exchange.get(message.TopicID)(obj);
                    }                    
                }

                else if (message.Type == 4) { // Delete
                    if(this.Remove.has(message.TopicID)) this.Remove.get(message.TopicID)(obj);
                }
                //console.log(data)
                //Actions.get(message.TopicID)({Type: message.Type, Value: obj });

                //this.Load.get(message.TopicID)({ Type: message.Type, Value: obj });
                break;

            case 15: // PING
                console.log('pong')//, message)
                break;
        }
    }
     
    private UserData = new Map<string, any>();
    
    GetData(scope, type, action):any {
        this.CheckUser(() => {

        
        
            var key = "--" + type + "-" + scope.User.ID;
            this.http.get(this.coflows_server + 'account/userdata?groupid=' + scope.Group.ID + '&type=' + type, { headers: this.header })
            .toPromise().then(function(resp){
                
                if (this.UserData.containsKey(key)) {
                    this.UserData.get(key).Value = resp;
                }
                else {
                    this.Subscribe(key);
                    var container = { Key: key, Value: resp };
                    this.UserData.put(key, container);
                }

                this.Actions.put(key, action);
                this.Actions.get(key)(this.UserData.get(key));
            })
        })
    }

    LinkAction(key, load, add, exchange, remove):any {
        this.CheckUser(() => {
            this.http.get(this.coflows_server + 'm/rawdata?type=' + key, { headers: this.header })
                .toPromise().then((resp_raw : any[]) => {
                    //console.log(resp_raw)
                    let resp = resp_raw.map(x => { 
                        var obj = x.Entry;

                        try {
                            obj = JSON.parse(x.Entry);
                        }
                        catch (err) {
                            //console.log(err)
                         } 

                        return {Key: x.EntryID, Value: obj }
                    })

                    if (this.UserData.has(key)) {
                        this.UserData.get(key).Value = resp;
                    }
                    else {
                        this.Subscribe(key);
                        var container = { Key: key, Value: resp };
                        this.UserData.set(key, container);
                    }

                    if(load != null){
                        this.Load.set(key, load);
                        this.Load.get(key)(this.UserData.get(key).Value);
                    }

                    if(add != null){
                        this.Add.set(key, add);
                    }

                    if(exchange != null){
                        this.Exchange.set(key, exchange);
                    }

                    if(remove != null){
                        this.Remove.set(key, remove);
                    }
                })
        })
    }
    
    Post(url : string, data : any, func : any, err?: any): void {     
        this.CheckUser(() => {
            this.http.post(this.coflows_server + url, data, { headers: this.header })
            .toPromise().then(response => {  
                func(response);
            })
            .catch(error =>  {if(err != undefined) err(error) } )
        })
    }

    PostAnonymous(url : string, data : any, func : any, err?: any): void {     
        this.http.post(this.coflows_server + url, data, { headers: this.header })
        .toPromise().then(response => {  
            func(response);
        })
        .catch(error =>  {if(err != undefined) err(error) } )
    }

    Get(url : string, func : any, err?: any): void {        
        this.CheckUser(() => {
            this.http.get(this.coflows_server + url, { headers: this.header })
            .toPromise().then(response => {  
                func(response);
            })
            .catch(error =>  {if(err != undefined) err(error) } )
        })
    }

    GetFile(url : string, func : any): void {        
        this.CheckUser(() => {                    
            this.http.get(
                this.coflows_server + url, 
                { 
                    headers: this.header,
                    responseType: 'blob'
                }
            )
            .subscribe(response => {  
                func(response);
            })
        })
    }

    formatNumber(str, short) {
        var base = "";
        if (str == undefined) {
            return "";
        }
        if (short) {

            if (Math.abs(str) >= 1000000000) {
                str = str / 1000000000;
                str = str.toFixed(2);
                base = "B";
            }
            else if (Math.abs(str) >= 100000) {
                str = str / 1000000;
                str = str.toFixed(2);
                base = "M";
            }
            else if (Math.abs(str) >= 1000) {
                str = str / 1000;
                str = str.toFixed(2);
                base = "k";
            }
            else {
                str = str.toFixed(2);
            }
        }
        else {
            str = str.toFixed(2);
        }


        //return str;
        var parts = (Math.abs(str) + "").split("."),
            main = parts[0],
            len = main.length,
            output = "",
            i = len - 1;

        while (i >= 0) {
            output = main.charAt(i) + output;
            if ((len - i) % 3 === 0 && i > 0) {
                output = "," + output;
            }
            --i;
        }
        // put decimal part back
        if (parts.length > 1) {
            output += "." + parts[1];
        }
        return (str < 0 ? "-" : "") + output + base;
    }

    getLocalJson(file, func) {
        this.http.get(file).toPromise().then(x => {
            func(x) 
        })
    } 

    isArray(data){
        return Array.isArray(data)
    }

    stringify(item){
        return JSON.stringify(item)
    }
}