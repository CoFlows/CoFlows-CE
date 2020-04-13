import { Routes } from '@angular/router';

import { WorkflowComponent } from './workflow/workflow.component';
import { AgentComponent } from './agent/agent.component';
import { QueryComponent } from './query/query.component';
import { AppComponent } from './app/app.component';

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
      path: 'agent/:wid/:id',
      component: AgentComponent,
      data: {
          heading: 'Agents'
      }
    },
    {
      path: 'query/:wid/:id',
      component: QueryComponent,
      data: {
          heading: 'Queries'
      }
    },
    {
      path: 'app',
      children: [
        {
          path: "**",
          component: AppComponent,
        }
      ],
      data: {
        heading: 'App'
      }
    }
  ]
}];
