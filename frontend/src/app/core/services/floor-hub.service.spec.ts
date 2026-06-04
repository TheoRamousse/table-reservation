// @unit
import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { FloorHubService, TableStatusChangedEvent, ServiceCapacityChangedEvent } from './floor-hub.service';
import { TableStatus } from '../models/table.model';

describe('FloorHubService', () => {
  let service: FloorHubService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(FloorHubService);
  });

  afterEach(() => {
    service.ngOnDestroy();
  });

  describe('connectionState', () => {
    it('démarre à "disconnected"', () => {
      expect(service.connectionState()).toBe('disconnected');
    });

    it('passe à "connecting" immédiatement lors de connect()', fakeAsync(() => {
      service.connect('2026-06-04', 'svc-1');
      expect(service.connectionState()).toBe('connecting');
      tick(200);
    }));

    it('passe à "connected" après le délai simulé', fakeAsync(() => {
      service.connect('2026-06-04', 'svc-1');
      tick(100);
      expect(service.connectionState()).toBe('connected');
      tick(8_000);
    }));

    it('repasse à "disconnected" après disconnect()', fakeAsync(() => {
      service.connect('2026-06-04', 'svc-1');
      tick(100);
      service.disconnect();
      expect(service.connectionState()).toBe('disconnected');
    }));

    it('rejoint le groupe floor-{date}-{serviceId}', fakeAsync(() => {
      service.connect('2026-06-15', 'svc-diner');
      expect(service.currentGroup).toBe('floor-2026-06-15-svc-diner');
      tick(8_200);
    }));

    it('reconnecte avec les mêmes paramètres si connect() est rappelé', fakeAsync(() => {
      service.connect('2026-06-04', 'svc-1');
      tick(100);
      service.connect('2026-06-04', 'svc-2');
      expect(service.connectionState()).toBe('connecting');
      expect(service.currentGroup).toBe('floor-2026-06-04-svc-2');
      tick(8_200);
    }));
  });

  describe('tableStatusChanged$', () => {
    it('émet quand emitTableStatusChanged() est appelé', () => {
      const received: TableStatusChangedEvent[] = [];
      service.tableStatusChanged$.subscribe(e => received.push(e));

      service.emitTableStatusChanged({ tableId: 't1', newStatus: TableStatus.Seated });

      expect(received.length).toBe(1);
      expect(received[0]).toEqual({ tableId: 't1', newStatus: TableStatus.Seated });
    });

    it('émet plusieurs événements successifs', () => {
      const received: TableStatusChangedEvent[] = [];
      service.tableStatusChanged$.subscribe(e => received.push(e));

      service.emitTableStatusChanged({ tableId: 't1', newStatus: TableStatus.Confirmed });
      service.emitTableStatusChanged({ tableId: 't2', newStatus: TableStatus.Free });

      expect(received.length).toBe(2);
    });

    it("ne complète pas le Subject avant ngOnDestroy()", () => {
      let completed = false;
      service.tableStatusChanged$.subscribe({ complete: () => (completed = true) });
      expect(completed).toBeFalse();
    });

    it("complète le Subject après ngOnDestroy()", () => {
      let completed = false;
      service.tableStatusChanged$.subscribe({ complete: () => (completed = true) });
      service.ngOnDestroy();
      expect(completed).toBeTrue();
    });
  });

  describe('serviceCapacityChanged$', () => {
    it('émet quand emitServiceCapacityChanged() est appelé', () => {
      const received: ServiceCapacityChangedEvent[] = [];
      service.serviceCapacityChanged$.subscribe(e => received.push(e));

      const event: ServiceCapacityChangedEvent = { serviceId: 'svc-1', date: '2026-06-04', remainingCovers: 42 };
      service.emitServiceCapacityChanged(event);

      expect(received.length).toBe(1);
      expect(received[0]).toEqual(event);
    });
  });

  describe('simulateReconnect()', () => {
    it('repasse à "disconnected" immédiatement', fakeAsync(() => {
      service.connect('2026-06-04', 'svc-1');
      tick(100);

      service.simulateReconnect();

      expect(service.connectionState()).toBe('disconnected');
      tick(2_000);
    }));

    it('revient à "connected" après le délai de reconnexion', fakeAsync(() => {
      service.connect('2026-06-04', 'svc-1');
      tick(100);

      service.simulateReconnect();
      tick(1_000); // reconnect delay
      tick(100);   // connect delay
      expect(service.connectionState()).toBe('connected');
      tick(8_000);
    }));

    it('ne fait rien si non connecté', fakeAsync(() => {
      service.simulateReconnect();
      expect(service.connectionState()).toBe('disconnected');
    }));
  });
});
