import { Component } from '@angular/core';
import { CoFlowsComponent } from '../../coflows/core/coflows.component';
import { ActivatedRoute } from '@angular/router';

// import { Location } from "@angular/common";

import { DomSanitizer } from '@angular/platform-browser';


@Component({
  templateUrl: 'app.component.html'
})
export class AppComponent {

    url = {}

    // constructor(private readonly location: Location, private activatedRoute: ActivatedRoute, private router: Router, public coflows: CoFlowsComponent, private sanitization:DomSanitizer){
    constructor(private activatedRoute: ActivatedRoute, public coflows: CoFlowsComponent, private sanitization:DomSanitizer){

    var count = 0

    this.activatedRoute.url.subscribe(params => {
        console.log('LOAD APP', count)
        if(count == 0){
            console.log(params)
            let cleanUrl = params.map(x => x.path).join('/')
            // let cleanUrl = params.map(x => decodeURIComponent(x.path)).join('/')
            let fullUrl = this.coflows.coflows_server + cleanUrl
            this.url = this.sanitization.bypassSecurityTrustResourceUrl(fullUrl)
            count++
            // this.location.go(cleanUrl)
        }
        
    });

    }
}