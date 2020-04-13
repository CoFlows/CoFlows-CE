import { Injectable } from '@angular/core';
import * as Rx from 'rxjs';

@Injectable()
export class WebsocketService {

    public ws : WebSocket;
    constructor() { }

    private subject: Rx.Subject<MessageEvent>;

    public connect(url): Rx.Subject<MessageEvent> {
        // if (!this.subject) {
        // this.subject = this.create(url);
        // console.log("Successfully connected: " + url);
        // } 
        // return this.subject;
        this.ws = new WebSocket(url);
        return null;
    }

    public send(data:any): void{
        if (this.ws.readyState === 1) {                   
            this.ws.send(JSON.stringify(data));
        }
    }

    public onmessage(process : any): void{
        this.ws.onmessage = process;        
    }

    public onclose(process : any): void{
        this.ws.onclose = process;
        this.subject = null;
    }

    private create(url): Rx.Subject<MessageEvent> {
        this.ws = new WebSocket(url);

        let observable = Rx.Observable.create(
        (obs: Rx.Observer<MessageEvent>) => {            
            this.ws.onmessage = obs.next.bind(obs);
            this.ws.onerror = obs.error.bind(obs);
            //this.ws.onclose = obs.complete.bind(obs);            
            return this.ws.close.bind(this.ws);
        })
    let observer = {
            next: (data: any) => {   
                
                if (this.ws.readyState === 1) {
                    this.ws.send(JSON.stringify(data));
                    console.log('sent', data)
                }
            }
        }
        return Rx.Subject.create(observer, observable);
    }

}