import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { FormBuilder, FormGroup, Validators, FormControl } from '@angular/forms';

import { QuantAppComponent } from '../../quantapp/core/quantapp.component';

@Component({
  selector: 'app-signin',
  templateUrl: './signin.component.html',
  styleUrls: ['./signin.component.scss'],  
})
export class SigninComponent implements OnInit {

  public form: FormGroup;
  constructor(private fb: FormBuilder, private router: Router, private quantapp: QuantAppComponent) {
    this.quantapp.logout(false)
  }

  bgVideo = ['shutterstock_v2123993.mp4', 'shutterstock_v3389084.mp4', 'shutterstock_v5941457.mp4']
  bgVideoIdx = 0
  videoFlip(element){    
    this.bgVideoIdx++
    if(this.bgVideoIdx == this.bgVideo.length) this.bgVideoIdx = 0;    
    element.target.load()
    element.target.play()
  }

  ngOnInit() {    
    this.form = this.fb.group ( {
      uname: [null , Validators.compose ( [ Validators.required ] )] , password: [null , Validators.compose ( [ Validators.required ] )]
    } );
  }

  errorMessage = ""

  onSubmit() {    
    var name = this.form.value.uname;
    var pass = this.form.value.password;

    let url = localStorage.getItem('QuantAppURL')
    

    if(url != null && url != undefined){
      this.quantapp.login(name, pass, url, x => this.errorMessage = x)
    }
    else{
      this.quantapp.login(name, pass, '/', x => this.errorMessage = x)
    }
    
  }

}
