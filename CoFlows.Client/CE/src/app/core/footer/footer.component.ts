import { Component } from '@angular/core';

import { CoFlowsComponent } from '../../coflows/core/coflows.component';

@Component({
  selector: 'app-footer',
  templateUrl: './footer.component.html',
  styleUrls: ['./footer.component.scss']
})
export class FooterComponent {
  year : any = {} 

  constructor (
    public coflows: CoFlowsComponent){
      this.year = (new Date()).getFullYear()
    }
}
