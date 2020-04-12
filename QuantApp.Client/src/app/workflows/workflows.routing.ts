import { Routes } from '@angular/router';

import { WorkflowComponent } from './workflow/workflow.component';
import { FunctionComponent } from './function/function.component';
import { WorkbookComponent } from './workbook/workbook.component';

export const WorkflowRoutes: Routes = [{
  path: '',
  children: [
    {
      path: 'workflow/:id',
      component: WorkflowComponent,
      data: {
          heading: 'Workflow'
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
