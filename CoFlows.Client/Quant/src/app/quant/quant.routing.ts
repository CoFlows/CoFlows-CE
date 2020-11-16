import { Routes } from '@angular/router';

import { TraderComponent } from './trader/trader.component';
import { SimulationComponent } from './simulation/simulation.component';

export const QuantRoutes: Routes = [{
  path: '',
  children: [
    {
      path: 'trader/:id',
      component: TraderComponent,
      data: {
          heading: 'Trader'
      }
    },
    {
      path: 'simulation/:id',
      component: SimulationComponent,
      data: {
          heading: 'Simulation'
      }
    },

  ]
}];
