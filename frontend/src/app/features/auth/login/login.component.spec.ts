// @integration
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { signal } from '@angular/core';
import { of, throwError } from 'rxjs';
import { LoginComponent } from './login.component';
import { AuthService } from '../../../core/services/auth.service';

describe('LoginComponent', () => {
  let fixture: ComponentFixture<LoginComponent>;
  let component: LoginComponent;
  let loginSpy: jasmine.Spy;

  const mockAuthService = {
    login: jasmine.createSpy('login'),
    logout: jasmine.createSpy('logout'),
    isAuthenticated: signal(false),
    token: signal<string | null>(null),
    role: signal<string | null>(null),
  };

  beforeEach(async () => {
    mockAuthService.login = jasmine.createSpy('login');

    await TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [
        provideNoopAnimations(),
        provideRouter([]),
        { provide: AuthService, useValue: mockAuthService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    loginSpy = mockAuthService.login;
    fixture.detectChanges();
  });

  it('affiche le formulaire de connexion', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('input[type="email"]')).toBeTruthy();
    expect(el.querySelector('input[type="password"]')).toBeTruthy();
    expect(el.querySelector('button[type="submit"]')).toBeTruthy();
  });

  it('appelle authService.login avec les valeurs du formulaire', () => {
    loginSpy.and.returnValue(of({ token: 'tok', expiresAt: '2026-06-05T00:00:00Z', role: 'Staff' }));

    component['form'].setValue({ email: 'staff@restaurant.fr', password: 'password' });
    component['onSubmit']();

    expect(loginSpy).toHaveBeenCalledWith({ email: 'staff@restaurant.fr', password: 'password' });
  });

  it('affiche "Identifiants invalides" sur erreur', () => {
    loginSpy.and.returnValue(throwError(() => ({ status: 401 })));

    component['form'].setValue({ email: 'bad@test.fr', password: 'wrong' });
    component['onSubmit']();
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Identifiants invalides');
  });
});
