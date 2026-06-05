import { Routes } from '@angular/router';
import { authGuard } from './core/services/auth.guard';

export const routes: Routes = [
  { path: '', loadComponent: () => import('./features/home/home.component').then(m => m.HomeComponent) },
  { path: 'calculator', loadComponent: () => import('./features/calculator/calculator.component').then(m => m.CalculatorComponent) },
  { path: 'thank-you/:quoteNumber', loadComponent: () => import('./features/thank-you/thank-you.component').then(m => m.ThankYouComponent) },
  { path: 'w/:token', loadComponent: () => import('./features/warranty/warranty.component').then(m => m.WarrantyComponent) },
  { path: 'portfolio', loadComponent: () => import('./features/portfolio/portfolio.component').then(m => m.PortfolioComponent) },
  { path: 'admin/login', loadComponent: () => import('./features/admin/login/login.component').then(m => m.LoginComponent) },
  { path: 'admin', canActivate: [authGuard], children: [
    { path: 'dashboard', loadComponent: () => import('./features/admin/dashboard/dashboard.component').then(m => m.DashboardComponent) },
    { path: 'leads', loadComponent: () => import('./features/admin/leads/leads-list.component').then(m => m.LeadsListComponent) },
    { path: 'leads/:id', loadComponent: () => import('./features/admin/leads/lead-detail.component').then(m => m.LeadDetailComponent) },
    { path: 'pricing', loadComponent: () => import('./features/admin/pricing/pricing.component').then(m => m.PricingComponent) },
    { path: 'jobs', loadComponent: () => import('./features/admin/jobs/jobs-list.component').then(m => m.JobsListComponent) },
    { path: 'jobs/:id', loadComponent: () => import('./features/admin/jobs/job-detail.component').then(m => m.JobDetailComponent) },
    { path: 'service-requests', loadComponent: () => import('./features/admin/service-requests/service-requests.component').then(m => m.ServiceRequestsComponent) },
    { path: 'portfolio-import', loadComponent: () => import('./features/admin/portfolio-import/portfolio-import.component').then(m => m.PortfolioImportComponent) },
    { path: 'portfolio-posts', loadComponent: () => import('./features/admin/portfolio-posts/portfolio-posts.component').then(m => m.PortfolioPostsComponent) },
    { path: '', redirectTo: 'dashboard', pathMatch: 'full' }
  ]},
  { path: '**', redirectTo: '' }
];
