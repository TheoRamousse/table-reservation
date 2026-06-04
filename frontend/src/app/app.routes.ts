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
  {
    path: 'availability',
    loadComponent: () =>
      import('./features/availability/availability-search.component').then(m => m.AvailabilitySearchComponent),
  },
  {
    path: 'bookings/new',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/booking/booking-form.component').then(m => m.BookingFormComponent),
  },
  {
    path: 'admin/customers',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/customer/customer-search.component').then(m => m.CustomerSearchComponent),
  },
  {
    path: 'admin/closed-days',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/admin/closed-days.component').then(m => m.ClosedDaysComponent),
  },
  {
    path: 'admin/tables',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/admin/table-management.component').then(m => m.TableManagementComponent),
  },
  { path: '', redirectTo: 'availability', pathMatch: 'full' },
  { path: '**', redirectTo: 'availability' },
];
