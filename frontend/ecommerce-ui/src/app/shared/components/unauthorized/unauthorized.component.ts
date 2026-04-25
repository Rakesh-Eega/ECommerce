// src/app/shared/components/unauthorized/unauthorized.component.ts
import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-unauthorized',
  standalone: true,
  imports: [RouterLink],
  template: `
    <div class="unauthorized-container">
      <h1>403</h1>
      <p>You don't have permission to access this page.</p>
      <a routerLink="/home" class="btn-primary">Go Home</a>
    </div>
  `
})
export class UnauthorizedComponent {}
