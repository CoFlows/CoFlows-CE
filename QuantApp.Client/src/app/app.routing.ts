import { Routes } from '@angular/router';

import { AdminLayoutComponent } from './core';
import { AuthLayoutComponent } from './core';

import { QuantAppComponent } from './quantapp/core/quantapp.component';

export const AppRoutes: Routes = [{
  path: '',
  component: AdminLayoutComponent,
  canActivate: [QuantAppComponent],
  children: [
      {
        path: '',
        loadChildren: './dashboard/dashboard.module#DashboardModule'
      }, 
      {
        path: 'workspaces',
        loadChildren: './workspaces/workspaces.module#WorkspacesModule'
      }
      // ,
      // {
      //   path: 'trader',
      //   loadChildren: './trader/trader.module#TraderModule'
      // }, 
      // {
      //   path: 'development',
      //   loadChildren: './development/development.module#DevelopmentModule'
      // }
]
}, {
  path: '',
  component: AuthLayoutComponent,
  children: [{
    path: 'authentication',
    loadChildren: './authentication/authentication.module#AuthenticationModule'
  }, {
    path: 'error',
    loadChildren: './error/error.module#ErrorModule'
  }
  // , {
  //   path: 'landing',
  //   loadChildren: './landing/landing.module#LandingModule'
  // }
]
}, {
  path: '**',
  redirectTo: 'error/404'
}];

