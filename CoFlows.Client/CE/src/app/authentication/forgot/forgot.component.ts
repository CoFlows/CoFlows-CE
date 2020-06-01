import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { FormBuilder, FormGroup, Validators, FormControl } from '@angular/forms';

import { CoFlowsComponent } from '../../coflows/core/coflows.component';


@Component({
  selector: 'app-forgot',
  templateUrl: './forgot.component.html',
  styleUrls: ['./forgot.component.scss']
})
export class ForgotComponent implements OnInit {

  public form: FormGroup;
  constructor(private fb: FormBuilder, private router: Router, private coflows: CoFlowsComponent) {}

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
      uname: [null , Validators.compose ( [ Validators.required ] )]
    } );
  }

  message = ''

  onSubmit() {
    this.message = 'Resetting your password. An email will be sent to you with instructions...'

    
    this.coflows.PostAnonymous("administration/resetpassword", {
      "Email": this.form.value.uname,
      "From": "no-reply@coflows.com;CoFlows",
      "Subject": "Password Reset",
      "Message": "Your  new  <strong>password</strong>  is <br><br>$Password$<br><br>Please reset immediately!" 
    }, data => {
      this.message = ''
      this.router.navigate ( [ '/authentication/signin' ] );
    })
  }
}
