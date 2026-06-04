import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Customer } from '../models/customer.model';
import { Booking, CreateBookingCommand } from '../models/booking.model';

export interface CreateCustomerCommand {
  firstName: string;
  lastName: string;
  phone?: string;
  email?: string;
}

@Injectable({ providedIn: 'root' })
export class BookingService {
  private readonly http = inject(HttpClient);

  searchCustomers(phone?: string, email?: string): Observable<{ customers: Customer[] }> {
    let params = new HttpParams();
    if (phone) params = params.set('phone', phone);
    if (email) params = params.set('email', email);
    return this.http.get<{ customers: Customer[] }>('/api/customers', { params });
  }

  createCustomer(cmd: CreateCustomerCommand): Observable<Customer> {
    return this.http.post<Customer>('/api/customers', cmd);
  }

  createBooking(cmd: CreateBookingCommand): Observable<Booking> {
    return this.http.post<Booking>('/api/bookings', cmd);
  }
}
