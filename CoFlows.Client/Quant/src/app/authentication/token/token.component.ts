import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, FormControl } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';

import { CoFlowsComponent } from '../../coflows/core/coflows.component';

@Component({
  selector: 'app-token',
  templateUrl: './token.component.html',
  styleUrls: ['./token.component.scss'],  
})
export class TokenComponent implements OnInit {

  token = null
  public form: FormGroup;
  constructor(private activatedRoute: ActivatedRoute, private fb: FormBuilder, private coflows: CoFlowsComponent) {
    this.activatedRoute.params.subscribe(params => {
      let _token = params['token']
      if(_token != null && _token != "0" && _token != "1"){
        this.token = _token
        this.coflows.logout(false)
        this.coflows.oauth(this.token, null)
      }
    })

    this.activatedRoute.queryParams.subscribe(params => {
      let _token = params['access_token']
      if(_token != null){
        this.token = _token
        this.coflows.oauth(this.token, null)
      }
      else
        this.coflows.logout(false)
    })
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

  onSubmit() {    
    var name = this.form.value.uname;
    var pass = this.form.value.password;

    let url = localStorage.getItem('QuantAppURL');
    

    if(url != null && url != undefined){
      this.coflows.login(name, pass, url, x => x)
    }
    else{
      this.coflows.login(name, pass, '/', x => x)
    }
    
  }

}