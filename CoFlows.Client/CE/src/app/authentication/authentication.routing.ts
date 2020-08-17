import { Routes } from '@angular/router';

import { SigninComponent } from './signin/signin.component';
import { SignupComponent } from './signup/signup.component';
import { TokenComponent } from './token/token.component';
import { ForgotComponent } from './forgot/forgot.component';

export const AuthenticationRoutes: Routes = [
  {
    path: '',
    children: [{
      path: 'signin',
      component: SigninComponent
    }, {
      path: 'signup',
      component: SignupComponent
    }, {
      path: 'token/:token',
      // path: 'token?:id_token',
      component: TokenComponent
    }, {
      path: 'forgot',
      component: ForgotComponent
    }
  ]
  }
];
