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


import { WorkspaceRoutes } from './workspaces.routing';
import { WorkspaceComponent } from './workspace/workspace.component';
import { FunctionComponent } from './function/function.component';
import { WorkbookComponent } from './workbook/workbook.component';

import { QuantAppModule } from '../quantapp/quantapp.module';


@NgModule({
  imports: [
    CommonModule,
    RouterModule.forChild(WorkspaceRoutes),    
    ChartModule,
    FormsModule,
    NgbModule,
    NgxDatatableModule,
    TreeTableModule, GrowlModule, TabViewModule, ContextMenuModule, CodeHighlighterModule, SharedModule,
    CodemirrorModule,//.forRoot(),    
    NguiMapModule.forRoot({apiUrl: 'https://maps.google.com/maps/api/js?key=AIzaSyDckDGx8RGsoGHTbMq4fj_3DG5q3CSfd9c&libraries=visualization,places,drawing'}),

    QuantAppModule
  ],
  declarations: [
    WorkspaceComponent,
    FunctionComponent,
    WorkbookComponent
  ]
})

export class WorkspacesModule {}
