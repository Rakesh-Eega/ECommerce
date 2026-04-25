// src/app/core/auth/interceptors/token.interceptor.ts
import {
  HttpInterceptorFn,
  HttpRequest,
  HttpErrorResponse
} from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from '../auth.service';

const PUBLIC_ROUTES = [
  '/api/auth/login',
  '/api/auth/register',
  '/api/auth/refresh'
];

const isPublicRoute = (url: string): boolean =>
  PUBLIC_ROUTES.some(route => url.includes(route));

const attachToken = <T>(req: HttpRequest<T>, token: string): HttpRequest<T> =>
  req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });

export const tokenInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);

  if (isPublicRoute(req.url)) return next(req);

  const token = authService.getAccessToken();

  // Token expired — refresh silently before sending
  if (token && authService.isTokenExpired()) {
    return authService.refreshToken().pipe(
      switchMap(() => {
        const newToken = authService.getAccessToken();
        return next(newToken ? attachToken(req, newToken) : req);
      })
    );
  }

  const authReq = token ? attachToken(req, token) : req;

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401) {
        authService.logout();
      }
      return throwError(() => error);
    })
  );
};