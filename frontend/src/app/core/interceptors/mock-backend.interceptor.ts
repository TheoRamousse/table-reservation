import { HttpErrorResponse, HttpInterceptorFn, HttpResponse } from '@angular/common/http';
import { of, throwError } from 'rxjs';

// Comptes de démo — remplacer par le vrai backend quand disponible
const MOCK_USERS: Record<string, string> = {
  'staff@restaurant.fr': 'Staff',
  'manager@restaurant.fr': 'Manager',
  'admin@restaurant.fr': 'Admin',
  'online@restaurant.fr': 'Online',
};
const MOCK_PASSWORD = 'password';

export const mockBackendInterceptor: HttpInterceptorFn = (req, next) => {
  if (req.method !== 'POST' || !req.url.endsWith('/api/auth/login')) {
    return next(req);
  }

  const body = req.body as { email: string; password: string };
  const role = MOCK_USERS[body?.email];

  if (role && body.password === MOCK_PASSWORD) {
    const expiresAt = new Date();
    expiresAt.setHours(expiresAt.getHours() + 8);

    return of(
      new HttpResponse({
        status: 200,
        body: {
          token: `mock-jwt-${role.toLowerCase()}`,
          expiresAt: expiresAt.toISOString(),
          role,
        },
      })
    );
  }

  return throwError(
    () => new HttpErrorResponse({ status: 401, statusText: 'Unauthorized' })
  );
};
