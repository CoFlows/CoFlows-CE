import { Injectable } from '@angular/core';
import * as Rx from 'rxjs';

@Injectable()
export class WebsocketService {

    public ws : WebSocket;
    constructor() { }

    private subject: Rx.Subject<MessageEvent>;

    delay(ms: number) {
        return new Promise( resolve => setTimeout(resolve, ms) );
    }
    public connect(url, func): Rx.Subject<MessageEvent> {
        this.ws = new WebSocket(url)
        this.ws.onopen = x => { func() }
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
                }
            }
        }
        return Rx.Subject.create(observer, observable);
    }

}