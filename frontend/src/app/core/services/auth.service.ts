import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap } from 'rxjs';
import { LoginRequest, LoginResponse, UserRole } from '../models/auth.model';

const TOKEN_KEY = 'auth_token';
const ROLE_KEY = 'auth_role';
const EXPIRES_KEY = 'auth_expires';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  private readonly _token = signal<string | null>(localStorage.getItem(TOKEN_KEY));
  private readonly _role = signal<UserRole | null>(
    localStorage.getItem(ROLE_KEY) as UserRole | null
  );

  readonly token = this._token.asReadonly();
  readonly role = this._role.asReadonly();
  readonly isAuthenticated = computed(() => this._token() !== null);

  login(request: LoginRequest) {
    return this.http.post<LoginResponse>('/api/auth/login', request).pipe(
      tap(response => {
        localStorage.setItem(TOKEN_KEY, response.token);
        localStorage.setItem(ROLE_KEY, response.role);
        localStorage.setItem(EXPIRES_KEY, response.expiresAt);
        this._token.set(response.token);
        this._role.set(response.role);
      })
    );
  }

  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(ROLE_KEY);
    localStorage.removeItem(EXPIRES_KEY);
    this._token.set(null);
    this._role.set(null);
    this.router.navigate(['/login']);
  }
}
