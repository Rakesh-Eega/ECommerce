// src/app/core/auth/guards/auth.guard.ts
import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../auth.service';

export const authGuard: CanActivateFn = (route) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.isAuthenticated()) {
    router.navigate(['/auth/login'], {
      queryParams: { returnUrl: route.url.join('/') }
    });
    return false;
  }

  // Role check
  const requiredRoles: string[] = route.data?.['roles'] ?? [];
  if (requiredRoles.length > 0) {
    const userRole = authService.currentUser()?.role;
    if (!userRole || !requiredRoles.includes(userRole)) {
      router.navigate(['/unauthorized']);
      return false;
    }
  }

  return true;
};

export const adminGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.isAdmin()) {
    router.navigate(['/unauthorized']);
    return false;
  }
  return true;
};