export enum TableZone {
  Salle = 'Salle',
  Terrasse = 'Terrasse',
  Bar = 'Bar',
  SalonPrive = 'SalonPrive',
}

export enum TableStatus {
  Free = 'Free',
  Pending = 'Pending',
  Confirmed = 'Confirmed',
  Seated = 'Seated',
  NoShow = 'NoShow',
  Inactive = 'Inactive',
}

export interface Table {
  id: string;
  number: number;
  capacity: number;
  minCapacity: number;
  zone: TableZone;
  isActive: boolean;
  isCombinable: boolean;
}

export interface ActiveBooking {
  bookingId: string;
  customerId: string;
  customerName: string;
  guestsCount: number;
  arrivalTime: string;
  hasAllergyAlert: boolean;
  isCelebration: boolean;
  needsHighChair: boolean;
}

export interface TableState extends Table {
  status: TableStatus;
  activeBooking: ActiveBooking | null;
}

export interface AvailableTable {
  id: string;
  number: number;
  capacity: number;
  minCapacity: number;
  zone: TableZone;
  isCombinable: boolean;
}
