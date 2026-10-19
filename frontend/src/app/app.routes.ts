import { Routes } from '@angular/router';
import { TransportRegisterComponent } from './register/transport-register.component';

export const routes: Routes = [
  { path: '', component: TransportRegisterComponent, title: 'Transports · LCA Tools' },
  {
    path: 'transports/:id',
    loadComponent: () => import('./wizard/wizard.component').then(m => m.WizardComponent),
    title: 'Transport GWP calculator · LCA Tools',
  },
  { path: '**', redirectTo: '' },
];
