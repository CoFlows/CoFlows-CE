import { Component, ViewChild, ElementRef, EventEmitter, Output, Input, OnInit } from '@angular/core';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';


import { FormBuilder, FormGroup, Validators, FormControl } from '@angular/forms';
import { CustomValidators } from 'ng2-validation';

import { QuantAppComponent } from '../../quantapp/core/quantapp.component';

const oldPassword = new FormControl('', Validators.required);
const newPassword = new FormControl('', Validators.required);
const confirmNewPassword = new FormControl('', CustomValidators.equalTo(newPassword));



@Component({
  selector: 'app-header',
  templateUrl: './header.component.html',
  styleUrls: ['./header.component.scss']
})


export class HeaderComponent implements OnInit {
  @Input() heading: string
  @Output() toggleSidebar = new EventEmitter<void>()
  @Output() openSearch = new EventEmitter<void>()
  @Output() toggleFullscreen = new EventEmitter<void>()

  @ViewChild('profile')
  private profile:ElementRef

  
  public formProfile: FormGroup
  public formPassword: FormGroup

  message = null

  constructor (private fb: FormBuilder, public quantapp: QuantAppComponent, private modalService: NgbModal){
    
  }
  
  ngOnInit() {
    this.formProfile = this.fb.group( {
      firstName: new FormControl(this.quantapp.quser.User.FirstName, Validators.required),
      lastName: new FormControl(this.quantapp.quser.User.LastName, Validators.required)
    } );

    this.formPassword = this.fb.group( {
      oldPassword: oldPassword,
      newPassword: newPassword,
      confirmNewPassword: confirmNewPassword
    } );
  
    if(this.quantapp.quser.User.FirstName == '' || this.quantapp.quser.User.FirstName == null){
      this.open(this.profile)
    }

  }
  
  logout() : void {    
    this.quantapp.logout(true) //Arturo
  }

  passChangeMessage = ''
  onSubmitPassword() {
    
    this.quantapp.changePassword(
      this.formPassword.value.oldPassword, 
      this.formPassword.value.newPassword, 
      () => {  
        this.quantapp.showMessage('password updated...')
        this.modalService.dismissAll(this.activeModal)
        this.activeModal = {}
      }, (x) => { 
        this.passChangeMessage = x
        console.log(x)
      })
  }

  close(){
    this.modalService.dismissAll(this.activeModal)
    this.activeModal = {}
  }

  onSubmitProfile() {

    this.quantapp.Post('administration/updateuser_app', { UserID: this.quantapp.quser.User.ID, First: this.formProfile.value.firstName, Last: this.formProfile.value.lastName},
        data => {
            if(data.Data == "ok"){
              this.quantapp.showMessage('profile updated...')

              this.modalService.dismissAll(this.activeModal)
              this.activeModal = {}
              location.reload()

            }
            console.log(data)
        });
  }


  activeModal = {}
  open(content) {
    this.activeModal = content
    this.modalService.open(content).result.then((result) => {
      },
      (reason) => {

        if(content == this.profile && (this.quantapp.quser.User.FirstName == '' || this.quantapp.quser.User.FirstName == null)){
          this.open(this.profile)
        }
        
      }
    );
}
}
