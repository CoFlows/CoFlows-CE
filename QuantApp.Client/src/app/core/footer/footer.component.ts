import { Component } from '@angular/core';

import { QuantAppComponent } from '../../quantapp/core/quantapp.component';

@Component({
  selector: 'app-footer',
  templateUrl: './footer.component.html',
  styleUrls: ['./footer.component.scss']
})
export class FooterComponent {
  year : any = {} 

  constructor (
    public quantapp: QuantAppComponent){
      this.year = (new Date()).getFullYear()
    }
}
