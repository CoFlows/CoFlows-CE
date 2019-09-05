import { Routes } from '@angular/router';

import { WorkspaceComponent } from './workspace/workspace.component';
import { FunctionComponent } from './function/function.component';
import { WorkbookComponent } from './workbook/workbook.component';

export const WorkspaceRoutes: Routes = [{
  path: '',
  children: [
    {
      path: 'workspace/:id',
      component: WorkspaceComponent,
      data: {
          heading: 'Workspace'
      }
    },
    {
      path: 'function/:wid/:id',
      component: FunctionComponent,
      data: {
          heading: 'Agents'
      }
    },
    {
      path: 'workbook/:wid/:id',
      component: WorkbookComponent,
      data: {
          heading: 'Queries'
      }
    }
  ]
}];
