import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';

import { FormsModule}    from '@angular/forms';

import { ChartModule } from 'angular-highcharts';
import { CodemirrorModule } from 'ng2-codemirror';

import { TreeTableModule, SharedModule, GrowlModule, TabViewModule, ContextMenuModule, CodeHighlighterModule} from 'primeng/primeng';

import { NguiMapModule} from '@ngui/map';

import { NgbModule } from '@ng-bootstrap/ng-bootstrap';
import { NgxDatatableModule } from '@swimlane/ngx-datatable';


import { WorkflowRoutes } from './workflows.routing';
import { WorkflowComponent } from './workflow/workflow.component';
import { TopicComponent } from './topic/topic.component';
import { AgentComponent } from './agent/agent.component';
import { QueryComponent } from './query/query.component';
import { AppComponent } from './app/app.component';

import { CoFlowsModule } from '../coflows/coflows.module';


@NgModule({
  imports: [
    CommonModule,
    RouterModule.forChild(WorkflowRoutes),    
    ChartModule,
    FormsModule,
    NgbModule,
    NgxDatatableModule,
    TreeTableModule, GrowlModule, TabViewModule, ContextMenuModule, CodeHighlighterModule, SharedModule,
    CodemirrorModule,//.forRoot(),    
    NguiMapModule.forRoot({apiUrl: 'https://maps.google.com/maps/api/js?key=AIzaSyDckDGx8RGsoGHTbMq4fj_3DG5q3CSfd9c&libraries=visualization,places,drawing'}),

    CoFlowsModule
  ],
  declarations: [
    WorkflowComponent,
    TopicComponent,
    AgentComponent,
    QueryComponent,
    AppComponent
  ]
})

export class WorkflowsModule {}
