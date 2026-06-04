import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { MatTooltipModule } from '@angular/material/tooltip';
import { TableState, TableStatus } from '../../../../core/models/table.model';

interface StatusConfig {
  cssClass: string;
  label: string;
}

const STATUS_CONFIGS: Record<TableStatus, StatusConfig> = {
  [TableStatus.Free]:      { cssClass: 'status-libre',       label: 'Libre' },
  [TableStatus.Pending]:   { cssClass: 'status-en-attente',  label: 'En attente' },
  [TableStatus.Confirmed]: { cssClass: 'status-reservee',    label: 'Réservée' },
  [TableStatus.Seated]:    { cssClass: 'status-occupee',     label: 'Occupée' },
  [TableStatus.NoShow]:    { cssClass: 'status-noshow',      label: 'No-show' },
  [TableStatus.Inactive]:  { cssClass: 'status-inactive',    label: 'Inactive' },
};

@Component({
  selector: 'app-table-card',
  templateUrl: './table-card.component.html',
  styleUrl: './table-card.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatTooltipModule],
})
export class TableCardComponent {
  readonly table = input.required<TableState>();
  readonly tableClick = output<TableState>();

  protected readonly TableStatus = TableStatus;

  protected readonly statusConfig = computed(() => STATUS_CONFIGS[this.table().status]);

  protected readonly tooltipText = computed(() => {
    const booking = this.table().activeBooking;
    if (!booking) return '';
    return `${booking.customerName} · ${booking.guestsCount} couverts · ${booking.arrivalTime}`;
  });
}
