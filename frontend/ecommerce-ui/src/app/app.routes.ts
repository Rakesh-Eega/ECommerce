// src/app/app.routes.ts
import { Routes } from '@angular/router';
import { authGuard, adminGuard } from './core/Auth/guards/auth.guard';

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'home',
    pathMatch: 'full'
  },
  {
    path: 'auth',
    loadChildren: () =>
      import('./features/auth/auth.routes').then(m => m.AUTH_ROUTES)
  },
  {
    path: 'home',
    loadComponent: () =>
      import('./features/home/home.component').then(m => m.HomeComponent)
  },
  {
    path: 'products',
    loadChildren: () =>
      import('./features/products/product.routes').then(m => m.PRODUCT_ROUTES)
  },
  {
    path: 'orders',
    canActivate: [authGuard],
    loadChildren: () =>
      import('./features/orders/order.routes').then(m => m.ORDER_ROUTES)
  },
  {
    path: 'admin',
    canActivate: [adminGuard],
    loadChildren: () =>
      import('./features/admin/admin.routes').then(m => m.ADMIN_ROUTES)
  },
  {
    path: 'unauthorized',
    loadComponent: () =>
      import('./shared/components/unauthorized/unauthorized.component')
        .then(m => m.UnauthorizedComponent)
  },
  { path: '**', redirectTo: 'home' }
];