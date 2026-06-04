import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { FloorSnapshot } from '../models/floor-snapshot.model';
import { DiningService } from '../models/dining-service.model';

@Injectable({ providedIn: 'root' })
export class FloorService {
  private readonly http = inject(HttpClient);

  getServices(): Observable<DiningService[]> {
    return this.http.get<DiningService[]>('/api/dining-services');
  }

  getSnapshot(date: string, serviceId: string): Observable<FloorSnapshot> {
    return this.http.get<FloorSnapshot>(`/api/floor/snapshot?date=${date}&serviceId=${serviceId}`);
  }

  updateBookingStatus(bookingId: string, newStatus: string): Observable<void> {
    return this.http.patch<void>(`/api/bookings/${bookingId}/status`, { newStatus });
  }
}
