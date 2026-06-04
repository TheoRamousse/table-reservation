// @integration
import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { AuthService } from './auth.service';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('isAuthenticated() est false sans token en localStorage', () => {
    expect(service.isAuthenticated()).toBeFalse();
  });

  it('login stocke le token et met isAuthenticated à true', () => {
    service.login({ email: 'staff@restaurant.fr', password: 'password' }).subscribe();

    const req = httpMock.expectOne('/api/auth/login');
    expect(req.request.method).toBe('POST');
    req.flush({ token: 'my-token', expiresAt: '2026-06-05T00:00:00Z', role: 'Staff' });

    expect(service.isAuthenticated()).toBeTrue();
    expect(service.token()).toBe('my-token');
    expect(service.role()).toBe('Staff');
    expect(localStorage.getItem('auth_token')).toBe('my-token');
  });
});
