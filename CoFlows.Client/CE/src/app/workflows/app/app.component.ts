import { Component, Input } from '@angular/core';
import { CoFlowsComponent } from '../../coflows/core/coflows.component';
import { ActivatedRoute, Router } from '@angular/router';

import { Location } from "@angular/common";

import { DomSanitizer } from '@angular/platform-browser';


@Component({
  templateUrl: 'app.component.html'
})
export class AppComponent {

    url = {}

    constructor(private readonly location: Location, private activatedRoute: ActivatedRoute, private router: Router, public coflows: CoFlowsComponent, private sanitization:DomSanitizer){

        coflows.CheckUser(() => {
            var n=0
            this.activatedRoute.url.subscribe(params => {
                {
                    let cleanUrl = params.map(x => decodeURIComponent(x.path)).join('/')
                    let fullUrl = this.coflows.coflows_server + cleanUrl
                    
                    if(fullUrl.includes('?'))
                        fullUrl += '&_cokey=' + coflows.quser.Secret
                    else
                        fullUrl += '?_cokey=' + coflows.quser.Secret

                    if(n == 0)
                        this.url = this.sanitization.bypassSecurityTrustResourceUrl(fullUrl)
                    n++
                    this.location.go('/workflows/app/' + cleanUrl)
                }
                
            })
        })
    }
}