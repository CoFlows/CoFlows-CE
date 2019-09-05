import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { FormBuilder, FormGroup, Validators, FormControl } from '@angular/forms';
import { CustomValidators } from 'ng2-validation';

import { QuantAppComponent } from '../../quantapp/core/quantapp.component';

const password = new FormControl('', Validators.required);
const confirmPassword = new FormControl('', CustomValidators.equalTo(password));

@Component({
  selector: 'app-signup',
  templateUrl: './signup.component.html',
  styleUrls: ['./signup.component.scss']
})
export class SignupComponent implements OnInit {

  public form: FormGroup;
  constructor(private fb: FormBuilder, private router: Router, private quantapp: QuantAppComponent) {}

  bgVideo = ['shutterstock_v2123993.mp4', 'shutterstock_v3389084.mp4', 'shutterstock_v5941457.mp4']
  bgVideoIdx = 0
  videoFlip(element){    
    this.bgVideoIdx++
    if(this.bgVideoIdx == this.bgVideo.length) this.bgVideoIdx = 0;    
    element.target.load()
    element.target.play()
  }
  
  ngOnInit() {
    this.form = this.fb.group( {
      email: [null , Validators.compose ( [ Validators.required ] )],
      fname: [null , Validators.compose ( [ Validators.required ] )],
      lname: [null , Validators.compose ( [ Validators.required ] )],
      password: password,
      confirmPassword: confirmPassword
    } );
  }

  onSubmit() {
    
    let data = {
      FirstName: this.form.value.fname,
      LastName: this.form.value.lname,
      Email: this.form.value.email,
      Password: this.form.value.password,
      ConfirmPassword: this.form.value.confirmPassword
    }
    
    this.quantapp.Post('account/register',data,response =>{
      console.log(response)

      if(response.User.ID){
        console.log('User created...')

        this.quantapp.login(data.Email, data.Password, '/')
      }
      else{
        console.log('Sign up not working...')
      }
    })
  }

}
