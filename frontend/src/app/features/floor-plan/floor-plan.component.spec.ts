// @integration
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { signal } from '@angular/core';
import { Subject } from 'rxjs';
import { of } from 'rxjs';
import { FloorPlanComponent } from './floor-plan.component';
import { FloorService } from '../../core/services/floor.service';
import { FloorHubService, TableStatusChangedEvent, ServiceCapacityChangedEvent } from '../../core/services/floor-hub.service';
import { AuthService } from '../../core/services/auth.service';
import { FloorSnapshot } from '../../core/models/floor-snapshot.model';
import { TableStatus, TableZone } from '../../core/models/table.model';

const mockSnapshot: FloorSnapshot = {
  date: '2026-06-04',
  serviceId: 'svc-1',
  serviceName: 'Déjeuner',
  totalConfirmedCovers: 7,
  maxCovers: 60,
  tables: [
    { id: 't1', number: 1, capacity: 4, minCapacity: 2, zone: TableZone.Salle, isActive: true, isCombinable: false, status: TableStatus.Free, activeBooking: null },
    { id: 't2', number: 2, capacity: 4, minCapacity: 2, zone: TableZone.Terrasse, isActive: true, isCombinable: false, status: TableStatus.Confirmed, activeBooking: { bookingId: 'b1', customerId: 'c1', customerName: 'Dupont Jean', guestsCount: 3, arrivalTime: '12:30', hasAllergyAlert: false, isCelebration: false, needsHighChair: false } },
  ],
};

describe('FloorPlanComponent', () => {
  let fixture: ComponentFixture<FloorPlanComponent>;

  const connectionStateSignal = signal<'disconnected' | 'connecting' | 'connected'>('connected');
  const tableStatusSubject = new Subject<TableStatusChangedEvent>();
  const serviceCapacitySubject = new Subject<ServiceCapacityChangedEvent>();

  const mockFloorService = {
    getServices: jasmine.createSpy().and.returnValue(of([{ id: 'svc-1', name: 'Déjeuner', startTime: '12:00', endTime: '14:30', lastBookingTime: '13:30', durationMinutes: 90, maxCovers: 60, isActive: true }])),
    getSnapshot: jasmine.createSpy().and.returnValue(of(mockSnapshot)),
    updateBookingStatus: jasmine.createSpy().and.returnValue(of(null)),
  };

  const mockFloorHubService = {
    connectionState: connectionStateSignal,
    tableStatusChanged$: tableStatusSubject.asObservable(),
    serviceCapacityChanged$: serviceCapacitySubject.asObservable(),
    connect: jasmine.createSpy(),
    disconnect: jasmine.createSpy(),
    simulateReconnect: jasmine.createSpy(),
  };

  const mockAuthService = {
    logout: jasmine.createSpy(),
    isAuthenticated: signal(true),
    token: signal('tok'),
    role: signal('Staff'),
  };

  beforeEach(async () => {
    connectionStateSignal.set('connected');
    mockFloorService.getSnapshot.calls.reset();

    await TestBed.configureTestingModule({
      imports: [FloorPlanComponent],
      providers: [
        provideNoopAnimations(),
        provideRouter([]),
        { provide: FloorService, useValue: mockFloorService },
        { provide: FloorHubService, useValue: mockFloorHubService },
        { provide: AuthService, useValue: mockAuthService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(FloorPlanComponent);
    fixture.detectChanges();
    fixture.detectChanges(); // 2e passe pour effets + signal connection
  });

  // ── Rendu de base (Story 3.1) ────────────────────────────────────────────────

  it('affiche le titre "Vue Salle"', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Vue Salle');
  });

  it('affiche les tables lorsque le snapshot est chargé', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelectorAll('app-table-card').length).toBe(2);
  });

  it('affiche les groupes de zones', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Salle');
    expect(el.textContent).toContain('Terrasse');
  });

  it('affiche le compteur de couverts', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('7');
    expect(el.textContent).toContain('60');
  });

  // ── Temps réel via mock SignalR (Story 3.3) ──────────────────────────────────

  it('appelle connect() avec date et serviceId lors du chargement', () => {
    expect(mockFloorHubService.connect).toHaveBeenCalledWith('2026-06-04', 'svc-1');
  });

  it('met à jour le statut de la table lors de TableStatusChanged', () => {
    // t1 est Free initialement
    const component = fixture.componentInstance as any;
    expect(component.snapshot().tables[0].status).toBe(TableStatus.Free);

    tableStatusSubject.next({ tableId: 't1', newStatus: TableStatus.Confirmed });
    fixture.detectChanges();

    expect(component.snapshot().tables[0].status).toBe(TableStatus.Confirmed);
  });

  it("ne modifie pas les autres tables lors de TableStatusChanged", () => {
    const component = fixture.componentInstance as any;
    tableStatusSubject.next({ tableId: 't1', newStatus: TableStatus.Seated });
    fixture.detectChanges();

    // t2 doit rester Confirmed
    expect(component.snapshot().tables[1].status).toBe(TableStatus.Confirmed);
  });

  it('met à jour le compteur de couverts lors de ServiceCapacityChanged', () => {
    const component = fixture.componentInstance as any;
    // maxCovers = 60, remainingCovers = 35 → totalConfirmedCovers = 60 - 35 = 25
    serviceCapacitySubject.next({ serviceId: 'svc-1', date: '2026-06-04', remainingCovers: 35 });
    fixture.detectChanges();

    expect(component.snapshot().totalConfirmedCovers).toBe(25);
  });

  it('recharge le snapshot lors de la reconnexion SignalR', () => {
    const callCount = mockFloorService.getSnapshot.calls.count();

    // Simule déconnexion puis reconnexion
    connectionStateSignal.set('disconnected');
    fixture.detectChanges();
    connectionStateSignal.set('connected');
    fixture.detectChanges();

    expect(mockFloorService.getSnapshot.calls.count()).toBeGreaterThan(callCount);
  });

  it('affiche le badge de connexion dans le template', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('[data-connection-state]')).toBeTruthy();
  });
});
