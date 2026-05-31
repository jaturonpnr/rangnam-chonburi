import { Routes } from '@angular/router';
import { authGuard } from './core/services/auth.guard';

export const routes: Routes = [
  { path: '', loadComponent: () => import('./features/calculator/calculator.component').then(m => m.CalculatorComponent) },
  { path: 'thank-you/:quoteNumber', loadComponent: () => import('./features/thank-you/thank-you.component').then(m => m.ThankYouComponent) },
  { path: 'admin/login', loadComponent: () => import('./features/admin/login/login.component').then(m => m.LoginComponent) },
  { path: 'admin', canActivate: [authGuard], children: [
    { path: 'dashboard', loadComponent: () => import('./features/admin/dashboard/dashboard.component').then(m => m.DashboardComponent) },
    { path: 'leads', loadComponent: () => import('./features/admin/leads/leads-list.component').then(m => m.LeadsListComponent) },
    { path: 'leads/:id', loadComponent: () => import('./features/admin/leads/lead-detail.component').then(m => m.LeadDetailComponent) },
    { path: 'pricing', loadComponent: () => import('./features/admin/pricing/pricing.component').then(m => m.PricingComponent) },
    { path: '', redirectTo: 'dashboard', pathMatch: 'full' }
  ]},
  { path: '**', redirectTo: '' }
];
