export interface DiningService {
  id: string;
  name: string;
  startTime: string;
  endTime: string;
  lastBookingTime: string;
  durationMinutes: number;
  maxCovers: number;
  isActive: boolean;
}
