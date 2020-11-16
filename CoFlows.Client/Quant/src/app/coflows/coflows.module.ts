import { NgModule, Injectable } from '@angular/core';
import { CommonModule } from '@angular/common';

import { FormsModule}    from '@angular/forms';

import { ChartModule } from 'angular-highcharts';
import { CodemirrorModule } from 'ng2-codemirror';

import { TreeTableModule, SharedModule, GrowlModule, TabViewModule, ContextMenuModule, CodeHighlighterModule} from 'primeng/primeng';

import { NguiMapModule} from '@ngui/map';

import { NgbModule } from '@ng-bootstrap/ng-bootstrap';
import { NgxDatatableModule } from '@swimlane/ngx-datatable';


import { CoFlowsComponent } from './core/coflows.component';
import { WebsocketService } from './core/websocket.service';
import { MessagingService } from './core/messaging.service';

import { CFQueryComponent } from './query/cfquery.component';
import { QAStrategiesComponent } from '../quant/strategies/qastrategies.component';



@NgModule({
  imports: [
    CommonModule,
    ChartModule,
    FormsModule,
    NgbModule,
    NgxDatatableModule,
    TreeTableModule, GrowlModule, TabViewModule, ContextMenuModule, CodeHighlighterModule, SharedModule,
    CodemirrorModule,//.forRoot(),    
    NguiMapModule.forRoot({apiUrl: 'https://maps.google.com/maps/api/js?key=AIzaSyDckDGx8RGsoGHTbMq4fj_3DG5q3CSfd9c&libraries=visualization,places,drawing'})
  ],
  declarations: [
    CoFlowsComponent,
    CFQueryComponent,
    QAStrategiesComponent
  ],
  providers: [
    CoFlowsComponent,
    WebsocketService, 
    MessagingService,
    CFQueryComponent
    ,
    QAStrategiesComponent
  ],
  exports: [
    CFQueryComponent
    ,
    QAStrategiesComponent
  ]
})

export class CoFlowsModule {}
