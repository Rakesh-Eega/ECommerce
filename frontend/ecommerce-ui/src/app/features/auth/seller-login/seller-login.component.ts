// src/app/features/auth/seller-login/seller-login.component.ts
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../core/Auth/auth.service';

@Component({
  selector: 'app-seller-login',
  standalone: true,
  imports: [ReactiveFormsModule, CommonModule, RouterLink],
  templateUrl: './seller-login.component.html',
  styleUrl:    './seller-login.component.scss'
})
export class SellerLoginComponent {
  private readonly fb          = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router      = inject(Router);
  private readonly route       = inject(ActivatedRoute);

  readonly isLoading    = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly form = this.fb.group({
    email:    ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]]
  });

  onSubmit(): void {
    if (this.form.invalid || this.isLoading()) return;

    this.isLoading.set(true);
    this.errorMessage.set(null);

    const { email, password } = this.form.getRawValue();

    this.authService.login({ email: email!, password: password! }).subscribe({
      next: (response) => {
        // Only allow Seller or Admin through this portal
        if (response.user.role !== 'Seller' && response.user.role !== 'Admin') {
          this.authService.logout();
          this.errorMessage.set('This portal is for sellers only. Please use the customer login.');
          this.isLoading.set(false);
          return;
        }
        const returnUrl = this.route.snapshot.queryParams['returnUrl'] || '/seller/dashboard';
        this.router.navigateByUrl(returnUrl);
      },
      error: err => {
        this.errorMessage.set(err.error?.message || 'Login failed. Try again.');
        this.isLoading.set(false);
      }
    });
  }
}