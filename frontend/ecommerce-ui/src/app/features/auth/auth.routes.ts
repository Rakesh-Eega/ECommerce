// src/app/features/auth/auth.routes.ts
import { Routes } from '@angular/router';

export const AUTH_ROUTES: Routes = [
  {
    path: '',
    redirectTo: 'login',
    pathMatch: 'full'
  },
  {
    path: 'login',
    loadComponent: () =>
      import('./login/login.component').then(m => m.LoginComponent)
  },
  {
    path: 'register',
    loadComponent: () =>
      import('./register/register.component').then(m => m.RegisterComponent)
  },
  {
    path: 'seller/login',
    loadComponent: () =>
      import('./seller-login/seller-login.component').then(m => m.SellerLoginComponent)
  },
  {
    path: 'seller/register',
    loadComponent: () =>
      import('./seller-register/seller-register.component').then(m => m.SellerRegisterComponent)
  }
];