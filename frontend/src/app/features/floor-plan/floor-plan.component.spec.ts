// @integration
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { signal } from '@angular/core';
import { of } from 'rxjs';
import { FloorPlanComponent } from './floor-plan.component';
import { FloorService } from '../../core/services/floor.service';
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

  const mockFloorService = {
    getServices: jasmine.createSpy().and.returnValue(of([{ id: 'svc-1', name: 'Déjeuner', startTime: '12:00', endTime: '14:30', lastBookingTime: '13:30', durationMinutes: 90, maxCovers: 60, isActive: true }])),
    getSnapshot: jasmine.createSpy().and.returnValue(of(mockSnapshot)),
    updateBookingStatus: jasmine.createSpy().and.returnValue(of(null)),
  };

  const mockAuthService = {
    logout: jasmine.createSpy(),
    isAuthenticated: signal(true),
    token: signal('tok'),
    role: signal('Staff'),
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FloorPlanComponent],
      providers: [
        provideNoopAnimations(),
        provideRouter([]),
        { provide: FloorService, useValue: mockFloorService },
        { provide: AuthService, useValue: mockAuthService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(FloorPlanComponent);
    fixture.detectChanges();
    // second pass pour que l'effet auto-select + snapshot se propagent
    fixture.detectChanges();
  });

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
});
