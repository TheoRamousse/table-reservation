import { Injectable, OnDestroy, signal } from '@angular/core';
import { Subject } from 'rxjs';
import { TableStatus } from '../models/table.model';

export interface TableStatusChangedEvent {
  tableId: string;
  newStatus: TableStatus;
}

export interface ServiceCapacityChangedEvent {
  serviceId: string;
  date: string;
  remainingCovers: number;
}

type ConnectionState = 'disconnected' | 'connecting' | 'connected';

@Injectable({ providedIn: 'root' })
export class FloorHubService implements OnDestroy {
  private readonly tableStatusSubject = new Subject<TableStatusChangedEvent>();
  private readonly serviceCapacitySubject = new Subject<ServiceCapacityChangedEvent>();

  readonly connectionState = signal<ConnectionState>('disconnected');
  readonly tableStatusChanged$ = this.tableStatusSubject.asObservable();
  readonly serviceCapacityChanged$ = this.serviceCapacitySubject.asObservable();

  private _currentGroup: string | null = null;
  private _currentDate: string | null = null;
  private _currentServiceId: string | null = null;

  get currentGroup(): string | null { return this._currentGroup; }

  private connectTimeout: ReturnType<typeof setTimeout> | null = null;
  private reconnectTimeout: ReturnType<typeof setTimeout> | null = null;

  connect(date: string, serviceId: string): void {
    this.disconnect();
    this._currentDate = date;
    this._currentServiceId = serviceId;
    this._currentGroup = `floor-${date}-${serviceId}`;
    this.connectionState.set('connecting');

    const group = this._currentGroup;
    this.connectTimeout = setTimeout(() => {
      if (this._currentGroup === group) {
        this.connectionState.set('connected');
      }
    }, 100);
  }

  disconnect(): void {
    if (this.connectTimeout) {
      clearTimeout(this.connectTimeout);
      this.connectTimeout = null;
    }
    if (this.reconnectTimeout) {
      clearTimeout(this.reconnectTimeout);
      this.reconnectTimeout = null;
    }
    this.connectionState.set('disconnected');
    this._currentGroup = null;
  }

  simulateReconnect(): void {
    const date = this._currentDate;
    const serviceId = this._currentServiceId;
    if (!date || !serviceId) return;

    this.disconnect();

    this.reconnectTimeout = setTimeout(() => {
      this.connect(date, serviceId);
    }, 1_000);
  }

  emitTableStatusChanged(event: TableStatusChangedEvent): void {
    this.tableStatusSubject.next(event);
  }

  emitServiceCapacityChanged(event: ServiceCapacityChangedEvent): void {
    this.serviceCapacitySubject.next(event);
  }

  ngOnDestroy(): void {
    this.disconnect();
    this.tableStatusSubject.complete();
    this.serviceCapacitySubject.complete();
  }
}
