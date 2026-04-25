// src/app/features/auth/seller-register/seller-register.component.ts
import { Component, inject, signal } from '@angular/core';
import {
  FormBuilder, ReactiveFormsModule, Validators,
  AbstractControl, ValidationErrors
} from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';

const passwordMatchValidator = (control: AbstractControl): ValidationErrors | null => {
  const password = control.get('password')?.value;
  const confirm  = control.get('confirmPassword')?.value;
  return password === confirm ? null : { passwordMismatch: true };
};

@Component({
  selector: 'app-seller-register',
  standalone: true,
  imports: [ReactiveFormsModule, CommonModule, RouterLink],
  templateUrl: './seller-register.component.html',
  styleUrl:    './seller-register.component.scss'
})
export class SellerRegisterComponent {
  private readonly fb     = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly http   = inject(HttpClient);

  readonly isLoading      = signal(false);
  readonly errorMessage   = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);
  readonly currentStep    = signal(1); // 2-step form

  readonly form = this.fb.group({
    // Step 1 — Personal Info
    firstName: ['', [Validators.required, Validators.maxLength(100)]],
    lastName:  ['', [Validators.required, Validators.maxLength(100)]],
    email:     ['', [Validators.required, Validators.email]],
    phone:     ['', [Validators.required, Validators.pattern(/^[6-9]\d{9}$/)]],

    // Step 2 — Business Info + Password
    businessName: ['', [Validators.required, Validators.maxLength(200)]],
    gstin:        ['', [Validators.pattern(/^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z]{1}[1-9A-Z]{1}Z[0-9A-Z]{1}$/)]],
    password: [
      '',
      [
        Validators.required,
        Validators.minLength(8),
        Validators.pattern(/(?=.*[A-Z])(?=.*[0-9])(?=.*[^a-zA-Z0-9])/)
      ]
    ],
    confirmPassword: ['', Validators.required]
  }, { validators: passwordMatchValidator });

  nextStep(): void {
    const step1Fields = ['firstName', 'lastName', 'email', 'phone'];
    const allValid = step1Fields.every(f => this.form.get(f)?.valid);
    if (!allValid) {
      step1Fields.forEach(f => this.form.get(f)?.markAsTouched());
      return;
    }
    this.currentStep.set(2);
  }

  prevStep(): void {
    this.currentStep.set(1);
  }

  onSubmit(): void {
    if (this.form.invalid || this.isLoading()) return;

    this.isLoading.set(true);
    this.errorMessage.set(null);

    const { firstName, lastName, email, password } = this.form.getRawValue();

    this.http.post<any>(
      `${environment.apiUrl}/api/auth/register/seller`,
      { firstName, lastName, email, password }
    ).subscribe({
      next: () => {
        this.successMessage.set('Seller account created! Redirecting to login...');
        setTimeout(() => this.router.navigate(['/auth/seller/login']), 2000);
      },
      error: err => {
        this.errorMessage.set(err.error?.message || 'Registration failed. Try again.');
        this.isLoading.set(false);
      }
    });
  }
}