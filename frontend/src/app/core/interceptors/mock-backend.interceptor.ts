import { HttpErrorResponse, HttpInterceptorFn, HttpResponse } from '@angular/common/http';
import { of, throwError } from 'rxjs';
import { FloorSnapshot } from '../models/floor-snapshot.model';
import { DiningService } from '../models/dining-service.model';

// ── Auth ─────────────────────────────────────────────────────────────────────

const MOCK_USERS: Record<string, string> = {
  'staff@restaurant.fr': 'Staff',
  'manager@restaurant.fr': 'Manager',
  'admin@restaurant.fr': 'Admin',
  'online@restaurant.fr': 'Online',
};
const MOCK_PASSWORD = 'password';

// ── Données métier ────────────────────────────────────────────────────────────

const MOCK_SERVICES: DiningService[] = [
  { id: 'svc-dejeuner', name: 'Déjeuner', startTime: '12:00', endTime: '14:30', lastBookingTime: '13:30', durationMinutes: 90, maxCovers: 60, isActive: true },
  { id: 'svc-diner', name: 'Dîner', startTime: '19:00', endTime: '23:00', lastBookingTime: '21:30', durationMinutes: 120, maxCovers: 80, isActive: true },
];

const MOCK_SNAPSHOT: FloorSnapshot = {
  date: '2026-06-04',
  serviceId: 'svc-dejeuner',
  serviceName: 'Déjeuner',
  totalConfirmedCovers: 17,
  maxCovers: 60,
  tables: [
    // Zone Salle
    { id: 't1', number: 1, capacity: 2, minCapacity: 1, zone: 'Salle' as any, isActive: true, isCombinable: false, status: 'Free' as any, activeBooking: null },
    { id: 't2', number: 2, capacity: 4, minCapacity: 2, zone: 'Salle' as any, isActive: true, isCombinable: true, status: 'Confirmed' as any, activeBooking: { bookingId: 'b1', customerId: 'c1', customerName: 'Dupont Jean', guestsCount: 3, arrivalTime: '12:30', hasAllergyAlert: false, isCelebration: true, needsHighChair: false } },
    { id: 't3', number: 3, capacity: 6, minCapacity: 2, zone: 'Salle' as any, isActive: true, isCombinable: false, status: 'Seated' as any, activeBooking: { bookingId: 'b2', customerId: 'c2', customerName: 'Martin Sophie', guestsCount: 4, arrivalTime: '12:00', hasAllergyAlert: true, isCelebration: false, needsHighChair: true } },
    { id: 't4', number: 4, capacity: 4, minCapacity: 2, zone: 'Salle' as any, isActive: true, isCombinable: true, status: 'Pending' as any, activeBooking: { bookingId: 'b3', customerId: 'c3', customerName: 'Bernard Luc', guestsCount: 3, arrivalTime: '13:00', hasAllergyAlert: false, isCelebration: false, needsHighChair: false } },
    { id: 't5', number: 5, capacity: 2, minCapacity: 1, zone: 'Salle' as any, isActive: true, isCombinable: false, status: 'NoShow' as any, activeBooking: { bookingId: 'b4', customerId: 'c4', customerName: 'Lefebvre Paul', guestsCount: 2, arrivalTime: '12:15', hasAllergyAlert: false, isCelebration: false, needsHighChair: false } },
    { id: 't6', number: 6, capacity: 8, minCapacity: 4, zone: 'Salle' as any, isActive: true, isCombinable: true, status: 'Free' as any, activeBooking: null },
    // Zone Terrasse
    { id: 't7', number: 7, capacity: 4, minCapacity: 2, zone: 'Terrasse' as any, isActive: true, isCombinable: false, status: 'Free' as any, activeBooking: null },
    { id: 't8', number: 8, capacity: 4, minCapacity: 2, zone: 'Terrasse' as any, isActive: true, isCombinable: true, status: 'Confirmed' as any, activeBooking: { bookingId: 'b5', customerId: 'c5', customerName: 'Garcia Marie', guestsCount: 4, arrivalTime: '12:45', hasAllergyAlert: true, isCelebration: false, needsHighChair: false } },
    { id: 't9', number: 9, capacity: 2, minCapacity: 1, zone: 'Terrasse' as any, isActive: true, isCombinable: false, status: 'Seated' as any, activeBooking: { bookingId: 'b6', customerId: 'c6', customerName: 'Thomas Pierre', guestsCount: 2, arrivalTime: '12:00', hasAllergyAlert: false, isCelebration: false, needsHighChair: false } },
    { id: 't10', number: 10, capacity: 6, minCapacity: 3, zone: 'Terrasse' as any, isActive: true, isCombinable: true, status: 'Free' as any, activeBooking: null },
    // Zone Bar
    { id: 't11', number: 11, capacity: 4, minCapacity: 2, zone: 'Bar' as any, isActive: true, isCombinable: false, status: 'Free' as any, activeBooking: null },
    { id: 't12', number: 12, capacity: 2, minCapacity: 1, zone: 'Bar' as any, isActive: true, isCombinable: false, status: 'Confirmed' as any, activeBooking: { bookingId: 'b7', customerId: 'c7', customerName: 'Petit Emma', guestsCount: 1, arrivalTime: '12:30', hasAllergyAlert: false, isCelebration: false, needsHighChair: false } },
  ],
};

// ── Intercepteur ──────────────────────────────────────────────────────────────

export const mockBackendInterceptor: HttpInterceptorFn = (req, next) => {
  // POST /api/auth/login
  if (req.method === 'POST' && req.url.endsWith('/api/auth/login')) {
    const body = req.body as { email: string; password: string };
    const role = MOCK_USERS[body?.email];

    if (role && body.password === MOCK_PASSWORD) {
      const expiresAt = new Date();
      expiresAt.setHours(expiresAt.getHours() + 8);
      return of(new HttpResponse({ status: 200, body: { token: `mock-jwt-${role.toLowerCase()}`, expiresAt: expiresAt.toISOString(), role } }));
    }
    return throwError(() => new HttpErrorResponse({ status: 401, statusText: 'Unauthorized' }));
  }

  // GET /api/dining-services
  if (req.method === 'GET' && req.url.endsWith('/api/dining-services')) {
    return of(new HttpResponse({ status: 200, body: MOCK_SERVICES }));
  }

  // GET /api/floor/snapshot
  if (req.method === 'GET' && req.url.includes('/api/floor/snapshot')) {
    const params = new URL(req.urlWithParams, 'http://localhost').searchParams;
    const date = params.get('date') ?? MOCK_SNAPSHOT.date;
    const serviceId = params.get('serviceId') ?? MOCK_SNAPSHOT.serviceId;
    const svc = MOCK_SERVICES.find(s => s.id === serviceId);
    return of(new HttpResponse({ status: 200, body: { ...MOCK_SNAPSHOT, date, serviceId, serviceName: svc?.name ?? MOCK_SNAPSHOT.serviceName } }));
  }

  // PATCH /api/bookings/{id}/status
  if (req.method === 'PATCH' && req.url.match(/\/api\/bookings\/[^/]+\/status/)) {
    const body = req.body as { newStatus: string };
    return of(new HttpResponse({ status: 200, body: { status: body.newStatus } }));
  }

  return next(req);
};
