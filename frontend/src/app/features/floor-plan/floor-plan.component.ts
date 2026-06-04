import { ChangeDetectionStrategy, Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { Router } from '@angular/router';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { switchMap, filter } from 'rxjs';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { format } from 'date-fns';
import { FloorService } from '../../core/services/floor.service';
import { AuthService } from '../../core/services/auth.service';
import { DiningService } from '../../core/models/dining-service.model';
import { FloorSnapshot } from '../../core/models/floor-snapshot.model';
import { TableState, TableStatus } from '../../core/models/table.model';
import { TableCardComponent } from './components/table-card/table-card.component';

@Component({
  selector: 'app-floor-plan',
  templateUrl: './floor-plan.component.html',
  styleUrl: './floor-plan.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    TableCardComponent,
  ],
})
export class FloorPlanComponent {
  private readonly floorService = inject(FloorService);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly snackBar = inject(MatSnackBar);
  private readonly dialog = inject(MatDialog);

  protected readonly selectedDate = signal<string>(format(new Date(), 'yyyy-MM-dd'));
  protected readonly selectedServiceId = signal<string>('');

  protected readonly services = toSignal(this.floorService.getServices(), { initialValue: [] as DiningService[] });

  private readonly params$ = toObservable(
    computed(() => ({ date: this.selectedDate(), serviceId: this.selectedServiceId() }))
  ).pipe(
    filter(p => !!p.serviceId),
    switchMap(p => this.floorService.getSnapshot(p.date, p.serviceId))
  );

  protected readonly snapshot = toSignal<FloorSnapshot | null>(this.params$, { initialValue: null });

  protected readonly tablesByZone = computed(() => {
    const tables = this.snapshot()?.tables ?? [];
    const map = new Map<string, TableState[]>();
    for (const table of tables) {
      const zone = table.zone as string;
      if (!map.has(zone)) map.set(zone, []);
      map.get(zone)!.push(table);
    }
    return [...map.entries()].map(([zone, zoneTables]) => ({ zone, tables: zoneTables }));
  });

  protected readonly isLoading = computed(() => this.selectedServiceId() !== '' && this.snapshot() === null);

  readonly #autoSelectService = effect(() => {
    const svcs = this.services();
    if (svcs.length > 0 && !this.selectedServiceId()) {
      untracked(() => this.selectedServiceId.set(svcs[0].id));
    }
  });

  protected onDateChange(event: Event): void {
    this.selectedDate.set((event.target as HTMLInputElement).value);
  }

  protected onServiceSelected(serviceId: string): void {
    this.selectedServiceId.set(serviceId);
  }

  protected onServicesLoaded(): void {
    const svcs = this.services();
    if (svcs.length > 0 && !this.selectedServiceId()) {
      this.selectedServiceId.set(svcs[0].id);
    }
  }

  protected onTableClick(table: TableState): void {
    switch (table.status) {
      case TableStatus.Free:
        this.snackBar.open('Créer une réservation pour cette table — fonctionnalité à venir', 'Fermer', { duration: 3000 });
        break;
      case TableStatus.Pending:
      case TableStatus.Confirmed:
        this.openConfirmSeatDialog(table);
        break;
      case TableStatus.Seated:
        this.openCompleteTableDialog(table);
        break;
      default:
        this.snackBar.open(`Table ${table.number} — ${table.status}`, 'Fermer', { duration: 2000 });
    }
  }

  private openConfirmSeatDialog(table: TableState): void {
    const booking = table.activeBooking!;
    const ref = this.dialog.open(ConfirmActionDialogComponent, {
      data: {
        title: `Table ${table.number} — ${booking.customerName}`,
        message: `${booking.guestsCount} couverts · Arrivée à ${booking.arrivalTime}`,
        confirmLabel: 'Asseoir',
      },
    });
    ref.afterClosed().subscribe((confirmed: boolean) => {
      if (!confirmed) return;
      this.floorService.updateBookingStatus(booking.bookingId, 'Seated').subscribe({
        next: () => this.snackBar.open(`Table ${table.number} passée à Seated`, 'OK', { duration: 3000 }),
        error: () => {},
      });
    });
  }

  private openCompleteTableDialog(table: TableState): void {
    const booking = table.activeBooking!;
    const ref = this.dialog.open(ConfirmActionDialogComponent, {
      data: {
        title: `Terminer la table ${table.number} ?`,
        message: `${booking.customerName} · ${booking.guestsCount} couverts`,
        confirmLabel: 'Terminer',
      },
    });
    ref.afterClosed().subscribe((confirmed: boolean) => {
      if (!confirmed) return;
      this.floorService.updateBookingStatus(booking.bookingId, 'Completed').subscribe({
        next: () => this.snackBar.open(`Table ${table.number} marquée Completed`, 'OK', { duration: 3000 }),
        error: () => {},
      });
    });
  }

  protected logout(): void {
    this.authService.logout();
  }
}

// ── Dialogue de confirmation inline ──────────────────────────────────────────

import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';

interface DialogData { title: string; message: string; confirmLabel: string; }

@Component({
  selector: 'app-confirm-action-dialog',
  template: `
    <h2 mat-dialog-title>{{ data.title }}</h2>
    <mat-dialog-content>{{ data.message }}</mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button (click)="ref.close(false)">Annuler</button>
      <button mat-flat-button (click)="ref.close(true)">{{ data.confirmLabel }}</button>
    </mat-dialog-actions>
  `,
  imports: [MatDialogModule, MatButtonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ConfirmActionDialogComponent {
  readonly data = inject<DialogData>(MAT_DIALOG_DATA);
  readonly ref = inject(MatDialogRef<ConfirmActionDialogComponent>);
}
