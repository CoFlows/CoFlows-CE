import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';

import { NgxChartsModule } from '@swimlane/ngx-charts';
import { ChartModule } from 'angular-highcharts';

import { FormsModule}    from '@angular/forms';
import { NgbModule } from '@ng-bootstrap/ng-bootstrap';
import { NgxDatatableModule } from '@swimlane/ngx-datatable';
import { TreeTableModule, SharedModule, GrowlModule, TabViewModule, ContextMenuModule, CodeHighlighterModule} from 'primeng/primeng';



import { DashboardComponent } from './dashboard.component';
import { DashboardRoutes } from './dashboard.routing';

import { CoFlowsModule } from '../coflows/coflows.module';

@NgModule({
  imports: [
    CommonModule, 
    ChartModule,
    NgxDatatableModule,
    FormsModule,
    NgbModule,
    NgxDatatableModule,
    TreeTableModule, GrowlModule, TabViewModule, ContextMenuModule, CodeHighlighterModule, SharedModule, 
    RouterModule.forChild(DashboardRoutes), NgxChartsModule,

    CoFlowsModule
  ],
  declarations: [DashboardComponent]
})

export class DashboardModule {}
