export enum BookingStatus {
  Pending = 'Pending',
  Confirmed = 'Confirmed',
  Seated = 'Seated',
  Completed = 'Completed',
  Cancelled = 'Cancelled',
  NoShow = 'NoShow',
  Rejected = 'Rejected',
}

export enum BookingSource {
  Online = 'Online',
  Phone = 'Phone',
  WalkIn = 'WalkIn',
  Staff = 'Staff',
}

export interface Booking {
  id: string;
  tableId: string | null;
  customerId: string;
  serviceId: string;
  bookingDate: string;
  arrivalTime: string;
  guestsCount: number;
  status: BookingStatus;
  source: BookingSource;
  specialRequests: string | null;
  hasAllergyAlert: boolean;
  isCelebration: boolean;
  needsHighChair: boolean;
  lateCancel: boolean;
  cancellationReason: string | null;
  recurrenceGroupId: string | null;
  createdAt: string;
}

export interface CreateBookingCommand {
  tableId: string | null;
  customerId: string;
  serviceId: string;
  bookingDate: string;
  arrivalTime: string;
  guestsCount: number;
  specialRequests?: string;
  source: BookingSource;
}
