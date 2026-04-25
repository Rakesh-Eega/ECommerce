// src/app/core/auth/auth.service.ts
import { Injectable, inject, signal, computed, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap, catchError, EMPTY, Observable } from 'rxjs';
import { AuthResponse, LoginRequest, RegisterRequest, UserDto } from './models/auth.models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly platformId = inject(PLATFORM_ID); // ← inject this
  private readonly apiUrl = `${environment.apiUrl}/api/auth`;

  // Safe localStorage wrapper
  private isBrowser(): boolean {
    return isPlatformBrowser(this.platformId);
  }

  private getItem(key: string): string | null {
    return this.isBrowser() ? localStorage.getItem(key) : null;
  }

  private setItem(key: string, value: string): void {
    if (this.isBrowser()) localStorage.setItem(key, value);
  }

  private removeItem(key: string): void {
    if (this.isBrowser()) localStorage.removeItem(key);
  }

  // Signals
  private readonly _currentUser = signal<UserDto | null>(
    this.loadUserFromStorage()
  );
  private readonly _accessToken = signal<string | null>(
    this.getItem('access_token')
  );
  private readonly _isLoading = signal<boolean>(false);

  readonly currentUser = this._currentUser.asReadonly();
  readonly isLoading = this._isLoading.asReadonly();
  readonly isAuthenticated = computed(() => this._currentUser() !== null);
  readonly isAdmin = computed(() => this._currentUser()?.role === 'Admin');
  readonly isSeller = computed(() => this._currentUser()?.role === 'Seller');
  readonly isCustomer = computed(() => this._currentUser()?.role === 'Customer');
  readonly userFullName = computed(() => {
    const user = this._currentUser();
    return user ? `${user.firstName} ${user.lastName}` : '';
  });

  register(request: RegisterRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.apiUrl}/register`, request)
      .pipe(tap(response => this.handleAuthSuccess(response)));
  }

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.apiUrl}/login`, request)
      .pipe(tap(response => this.handleAuthSuccess(response)));
  }

  refreshToken(): Observable<AuthResponse> {
    const token = this.getItem('refresh_token');
    if (!token) return EMPTY;

    return this.http
      .post<AuthResponse>(`${this.apiUrl}/refresh`, { refreshToken: token })
      .pipe(
        tap(response => this.handleAuthSuccess(response)),
        catchError(() => {
          this.logout();
          return EMPTY;
        })
      );
  }

  logout(): void {
    const refreshToken = this.getItem('refresh_token');
    if (refreshToken) {
      this.http.post(`${this.apiUrl}/logout`, { refreshToken }).subscribe();
    }
    this.clearStorage();
    this._currentUser.set(null);
    this._accessToken.set(null);
    this.router.navigate(['/auth/login']);
  }

  getAccessToken(): string | null {
    return this._accessToken();
  }

  isTokenExpired(): boolean {
    const expiry = this.getItem('token_expiry');
    if (!expiry) return true;
    return new Date(expiry).getTime() - 30000 < Date.now();
  }

  private handleAuthSuccess(response: AuthResponse): void {
    this.setItem('access_token', response.accessToken);
    this.setItem('refresh_token', response.refreshToken);
    this.setItem('token_expiry', response.accessTokenExpiry);
    this.setItem('current_user', JSON.stringify(response.user));
    this._currentUser.set(response.user);
    this._accessToken.set(response.accessToken);
  }

  private clearStorage(): void {
    ['access_token', 'refresh_token', 'token_expiry', 'current_user']
      .forEach(key => this.removeItem(key));
  }

  private loadUserFromStorage(): UserDto | null {
    try {
      const raw = this.getItem('current_user');
      return raw ? JSON.parse(raw) : null;
    } catch {
      return null;
    }
  }
}