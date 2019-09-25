import { Routes } from '@angular/router';

import { TraderComponent } from './trader.component';

export const TraderRoutes: Routes = [{
  path: 'trader/:id',
  component: TraderComponent,
  data: {
    heading: 'Trader'
  }
}];
