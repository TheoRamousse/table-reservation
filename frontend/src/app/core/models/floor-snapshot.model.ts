import { TableState } from './table.model';

export interface FloorSnapshot {
  date: string;
  serviceId: string;
  serviceName: string;
  totalConfirmedCovers: number;
  maxCovers: number;
  tables: TableState[];
}
