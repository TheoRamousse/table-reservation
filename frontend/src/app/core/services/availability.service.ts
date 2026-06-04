import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AvailableTable } from '../models/table.model';

export interface AvailabilityResponse {
  tables: AvailableTable[];
  reason: 'ServiceFullyBooked' | null;
}

@Injectable({ providedIn: 'root' })
export class AvailabilityService {
  private readonly http = inject(HttpClient);

  search(date: string, serviceId: string, guestsCount: number, zone?: string): Observable<AvailabilityResponse> {
    let params = new HttpParams()
      .set('date', date)
      .set('serviceId', serviceId)
      .set('guestsCount', guestsCount);
    if (zone) params = params.set('zone', zone);
    return this.http.get<AvailabilityResponse>('/api/tables/availability', { params });
  }
}
