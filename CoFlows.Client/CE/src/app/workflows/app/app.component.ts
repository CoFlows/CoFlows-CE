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

        var n=0
        this.activatedRoute.url.subscribe(params => {
            // if(n == 0)
            {
                let cleanUrl = params.map(x => decodeURIComponent(x.path)).join('/')
                let fullUrl = this.coflows.coflows_server + cleanUrl
                if(n == 0)
                    this.url = this.sanitization.bypassSecurityTrustResourceUrl(fullUrl)
                n++
                this.location.go('/workflows/app/' + cleanUrl)
            }
            
        })
    }
}