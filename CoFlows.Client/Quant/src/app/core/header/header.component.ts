import { Component, ViewChild, ElementRef, EventEmitter, Output, Input, OnInit } from '@angular/core';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';


import { FormBuilder, FormGroup, Validators, FormControl } from '@angular/forms';
import { CustomValidators } from 'ng2-validation';

import { CoFlowsComponent } from '../../coflows/core/coflows.component';

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

  constructor (private fb: FormBuilder, public coflows: CoFlowsComponent, private modalService: NgbModal){
    
  }
  
  ngOnInit() {
    this.formProfile = this.fb.group( {
      firstName: new FormControl(this.coflows.quser.User.FirstName, Validators.required),
      lastName: new FormControl(this.coflows.quser.User.LastName, Validators.required)
    } );

    this.formPassword = this.fb.group( {
      oldPassword: oldPassword,
      newPassword: newPassword,
      confirmNewPassword: confirmNewPassword
    } );
  
    if(this.coflows.quser.User.FirstName == '' || this.coflows.quser.User.FirstName == null){
      this.open(this.profile)
    }

  }
  
  logout() : void {    
    this.coflows.logout(true) //Arturo
  }

  passChangeMessage = ''
  onSubmitPassword() {
    
    this.coflows.changePassword(
      this.formPassword.value.oldPassword, 
      this.formPassword.value.newPassword, 
      () => {  
        this.coflows.showMessage('password updated...')
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

    this.coflows.Post('administration/updateuser', { UserID: this.coflows.quser.User.ID, FirstName: this.formProfile.value.firstName, LastName: this.formProfile.value.lastName},
        data => {
            if(data.Data == "ok"){
              this.coflows.showMessage('profile updated...')

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

        if(content == this.profile && (this.coflows.quser.User.FirstName == '' || this.coflows.quser.User.FirstName == null)){
          this.open(this.profile)
        }
        
      }
    );
}
}
