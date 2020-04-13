import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';

import { FormsModule}    from '@angular/forms';

import { ChartModule } from 'angular-highcharts';
import { CodemirrorModule } from 'ng2-codemirror';

import { TreeTableModule, SharedModule, GrowlModule, TabViewModule, ContextMenuModule, CodeHighlighterModule} from 'primeng/primeng';

import { NgbModule } from '@ng-bootstrap/ng-bootstrap';
import { NgxDatatableModule } from '@swimlane/ngx-datatable';


import { QuantRoutes } from './quant.routing';
import { TraderComponent } from './trader/trader.component';
import { SimulationComponent } from './simulation/simulation.component';

import { CoFlowsModule } from '../coflows/coflows.module';

@NgModule({
  imports: [
    CommonModule,
    RouterModule.forChild(QuantRoutes),    
    ChartModule,
    FormsModule,
    NgbModule,
    NgxDatatableModule,
    TreeTableModule, GrowlModule, TabViewModule, ContextMenuModule, CodeHighlighterModule, SharedModule,
    CodemirrorModule,
    CoFlowsModule
  ],
  declarations: [
    TraderComponent,
    SimulationComponent,
  ],
  providers: [
  ],
  exports: [
  ]
})

export class QuantModule {}
