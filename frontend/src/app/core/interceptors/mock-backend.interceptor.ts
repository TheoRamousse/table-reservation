import { HttpErrorResponse, HttpInterceptorFn, HttpResponse } from '@angular/common/http';
import { of, throwError } from 'rxjs';
import { FloorSnapshot } from '../models/floor-snapshot.model';
import { DiningService } from '../models/dining-service.model';
import { Customer, VipLevel } from '../models/customer.model';

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

const MOCK_CLOSED_DAYS: Array<{ id: string; date: string; reason: string }> = [];

const MOCK_CUSTOMERS: Customer[] = [
  { id: 'c1', firstName: 'Jean', lastName: 'Dupont', phone: '0601020304', email: 'jean.dupont@example.com', isBlacklisted: false, noShowCount: 0, vipLevel: VipLevel.None },
  { id: 'c2', firstName: 'Sophie', lastName: 'Martin', phone: '0605060708', email: 'sophie.martin@example.com', isBlacklisted: false, noShowCount: 1, vipLevel: VipLevel.Regular },
  { id: 'c3', firstName: 'Pierre', lastName: 'Noshow', phone: '0611121314', email: 'noshow@example.com', isBlacklisted: true, noShowCount: 3, vipLevel: VipLevel.None },
];

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

  // GET /api/tables/availability
  if (req.method === 'GET' && req.url.includes('/api/tables/availability')) {
    const params = new URL(req.urlWithParams, 'http://localhost').searchParams;
    const guestsCount = parseInt(params.get('guestsCount') ?? '2', 10);
    const zone = params.get('zone');
    const tables = MOCK_SNAPSHOT.tables
      .filter(t => t.status === 'Free' && t.isActive)
      .filter(t => guestsCount >= t.minCapacity && guestsCount <= t.capacity)
      .filter(t => !zone || t.zone === zone)
      .map(({ id, number, capacity, minCapacity, zone: z, isCombinable }) => ({ id, number, capacity, minCapacity, zone: z, isCombinable }))
      .sort((a, b) => a.capacity - b.capacity);
    return of(new HttpResponse({ status: 200, body: { tables, reason: null } }));
  }

  // GET /api/closed-days
  if (req.method === 'GET' && req.url.endsWith('/api/closed-days')) {
    return of(new HttpResponse({ status: 200, body: MOCK_CLOSED_DAYS }));
  }

  // POST /api/closed-days
  if (req.method === 'POST' && req.url.endsWith('/api/closed-days')) {
    const body = req.body as { date: string; reason: string };
    const closedDay = {
      id: `cd-${Math.random().toString(36).slice(2, 8)}`,
      date: body.date,
      reason: body.reason,
    };
    MOCK_CLOSED_DAYS.push(closedDay);
    return of(new HttpResponse({ status: 201, body: { closedDay, cancelledBookingsCount: 2 } }));
  }

  // PATCH /api/customers/:id/blacklist
  if (req.method === 'PATCH' && req.url.match(/\/api\/customers\/[^/]+\/blacklist/)) {
    const id = req.url.split('/').slice(-2)[0];
    const body = req.body as { isBlacklisted: boolean } | null;
    const customer = MOCK_CUSTOMERS.find(c => c.id === id);
    if (!customer) {
      return throwError(() => new HttpErrorResponse({ status: 404, statusText: 'Not Found' }));
    }
    customer.isBlacklisted = body?.isBlacklisted ?? customer.isBlacklisted;
    return of(new HttpResponse({ status: 200, body: { ...customer } }));
  }

  // GET /api/customers (search — exclut /api/customers/{id}/...)
  if (req.method === 'GET' && req.url.includes('/api/customers') && !req.url.includes('/api/customers/')) {
    const params = new URL(req.urlWithParams, 'http://localhost').searchParams;
    const phone = params.get('phone') ?? '';
    const email = params.get('email') ?? '';
    const found = MOCK_CUSTOMERS.filter(c =>
      (phone && c.phone.includes(phone)) || (email && (c.email ?? '').includes(email))
    );
    return of(new HttpResponse({ status: 200, body: { customers: found } }));
  }

  // POST /api/customers
  if (req.method === 'POST' && req.url.endsWith('/api/customers')) {
    const body = req.body as Partial<Customer>;
    const created: Customer = {
      id: `c-${Math.random().toString(36).slice(2, 8)}`,
      firstName: body.firstName ?? '',
      lastName: body.lastName ?? '',
      phone: body.phone ?? '',
      email: (body as any).email ?? null,
      isBlacklisted: false,
      noShowCount: 0,
      vipLevel: VipLevel.None,
    };
    return of(new HttpResponse({ status: 201, body: created }));
  }

  // DELETE /api/bookings/:id
  if (req.method === 'DELETE' && req.url.match(/\/api\/bookings\/[^/]+$/) && !req.url.includes('/table-lock')) {
    const id = req.url.split('/').pop()!;
    const body = req.body as { cancellationReason?: string } | null;
    return of(new HttpResponse({
      status: 200,
      body: {
        id,
        status: 'Cancelled',
        cancellationReason: body?.cancellationReason ?? null,
      },
    }));
  }

  // PUT /api/bookings/:id
  if (req.method === 'PUT' && req.url.match(/\/api\/bookings\/[^/]+$/)) {
    const id = req.url.split('/').pop()!;
    const body = req.body as { status?: string } | null;
    return of(new HttpResponse({
      status: 200,
      body: {
        id,
        status: body?.status ?? 'Pending',
      },
    }));
  }

  // DELETE /api/bookings/:id/table-lock
  if (req.method === 'DELETE' && req.url.match(/\/api\/bookings\/[^/]+\/table-lock/)) {
    return of(new HttpResponse({ status: 204, body: null }));
  }

  // POST /api/bookings
  if (req.method === 'POST' && req.url.endsWith('/api/bookings')) {
    const body = req.body as any;
    const sr: string = body.specialRequests ?? '';
    const booking = {
      id: `b-${Math.random().toString(36).slice(2, 8)}`,
      tableId: body.tableId ?? null,
      customerId: body.customerId,
      serviceId: body.serviceId,
      bookingDate: body.bookingDate,
      arrivalTime: body.arrivalTime,
      guestsCount: body.guestsCount,
      status: 'Pending',
      source: body.source,
      specialRequests: sr || null,
      hasAllergyAlert: /allergi/i.test(sr),
      isCelebration: /anniversaire|célébration|fête/i.test(sr),
      needsHighChair: /chaise|bébé|enfant/i.test(sr),
      lateCancel: false,
      cancellationReason: null,
      recurrenceGroupId: null,
      createdAt: new Date().toISOString(),
    };
    return of(new HttpResponse({ status: 201, body: booking }));
  }

  return next(req);
};
