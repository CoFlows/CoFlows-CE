import { Routes } from '@angular/router';

import { SimulationComponent } from './simulation.component';

export const SimulationRoutes: Routes = [{
  path: 'simulation/:id',
  component: SimulationComponent,
  data: {
    heading: 'Simulation'
  }
}];
