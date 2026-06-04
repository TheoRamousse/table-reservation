import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () =>
      import('./features/auth/login/login.component').then(m => m.LoginComponent),
  },
  {
    path: 'floor',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/floor-plan/floor-plan.component').then(m => m.FloorPlanComponent),
  },
  { path: '', redirectTo: 'floor', pathMatch: 'full' },
  { path: '**', redirectTo: 'floor' },
];
