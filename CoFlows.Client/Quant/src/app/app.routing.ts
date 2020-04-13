import { Routes } from '@angular/router';

import { AdminLayoutComponent } from './core';
import { AuthLayoutComponent } from './core';

import { CoFlowsComponent } from './coflows/core/coflows.component';

export const AppRoutes: Routes = [{
  path: '',
  component: AdminLayoutComponent,
  canActivate: [CoFlowsComponent],
  children: [
      {
        path: '',
        loadChildren: './dashboard/dashboard.module#DashboardModule'
      }, 
      {
        path: 'workflows',
        loadChildren: './workflows/workflows.module#WorkflowsModule'
      }, 
      {
        path: 'quant',
        loadChildren: './quant/quant.module#QuantModule'
      }
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
]
}, {
  path: '**',
  redirectTo: 'error/404'
}];

