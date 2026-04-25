// src/app/features/auth/register/register.component.ts
import { Component, inject, signal } from '@angular/core';
import {
  FormBuilder,
  ReactiveFormsModule,
  Validators,
  AbstractControl,
  ValidationErrors
} from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/Auth/auth.service';
import { CommonModule } from '@angular/common';

// Custom validator — passwords must match
const passwordMatchValidator = (
  control: AbstractControl
): ValidationErrors | null => {
  const password = control.get('password')?.value;
  const confirm = control.get('confirmPassword')?.value;
  return password === confirm ? null : { passwordMismatch: true };
};

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [ReactiveFormsModule, CommonModule, RouterLink],
  templateUrl:'./register.component.html' ,
  styleUrl: './register.component.scss'
})
export class RegisterComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);

  readonly form = this.fb.group(
    {
      firstName: ['', [Validators.required, Validators.maxLength(100)]],
      lastName: ['', [Validators.required, Validators.maxLength(100)]],
      email: ['', [Validators.required, Validators.email]],
      password: [
        '',
        [
          Validators.required,
          Validators.minLength(8),
          Validators.pattern(/(?=.*[A-Z])(?=.*[0-9])(?=.*[^a-zA-Z0-9])/)
        ]
      ],
      confirmPassword: ['', Validators.required]
    },
    { validators: passwordMatchValidator }
  );

  onSubmit(): void {
    if (this.form.invalid || this.isLoading()) return;

    this.isLoading.set(true);
    this.errorMessage.set(null);

    const { firstName, lastName, email, password } = this.form.getRawValue();

    this.authService
      .register({
        firstName: firstName!,
        lastName: lastName!,
        email: email!,
        password: password!
      })
      .subscribe({
        next: () => {
          this.successMessage.set('Account created! Redirecting...');
          setTimeout(() => this.router.navigate(['/home']), 1500);
        },
        error: err => {
          this.errorMessage.set(
            err.error?.message || 'Registration failed. Try again.'
          );
          this.isLoading.set(false);
        }
      });
  }
}