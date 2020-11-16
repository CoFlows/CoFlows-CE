import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';
import { WebsocketService } from './websocket.service';


export interface Message {
	author: string,
	message: string
}

@Injectable()
export class MessagingService {
    
    //Please change depending on server deployment
    // public secure = true
    // public server = 'coflows.quant.app'
    public secure = false
    public server = 'localhost'
    

    public messages: Subject<any>;

    private CHAT_URL = ''
    constructor(private wsService: WebsocketService) {        
        if( window.location.host != 'localhost:4200'){
            this.server = window.location.host;
        
        if( window.location.protocol == 'https:')
            this.secure = true
        else
            this.secure = false
        }

        this.CHAT_URL = (this.secure ? 'wss' : 'ws') + '://' + this.server + '/live'
    }

    subscribe(handler, key, func): void{
        // console.log('--- CONNECT: ' + this.CHAT_URL + "?_session=" + key)
        this.wsService.connect(this.CHAT_URL + "?_session=" + key, func)
        this.wsService.onmessage(
            response => {
            
            let data = JSON.parse(response.data);
            handler(data);
            
        });
    }
    
    send(data : any) : void{
        this.wsService.send(data)        
    }

    onClose(process: any): void{
        this.wsService.onclose(process);
    }

    // reOpen(): boolean{
    //     // console.log('-- reOpen: ', this.wsService.ws.readyState)
    //     if(this.wsService.ws.readyState !== 1){
    //         this.wsService.connect(this.CHAT_URL);
    //         console.log('connetion reopened');
    //         return true;
    //     }    
    //     else
    //         return false;    
    // }
}