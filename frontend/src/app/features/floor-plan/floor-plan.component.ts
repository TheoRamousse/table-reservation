import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  effect,
  inject,
  signal,
  untracked,
} from '@angular/core';
import { Router } from '@angular/router';
import { takeUntilDestroyed, toObservable, toSignal } from '@angular/core/rxjs-interop';
import { filter, switchMap } from 'rxjs';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule, MatDatepickerInputEvent } from '@angular/material/datepicker';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { BookingFormComponent } from '../booking/booking-form.component';
import { format, parse } from 'date-fns';
import { FloorService } from '../../core/services/floor.service';
import { FloorHubService } from '../../core/services/floor-hub.service';
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
    MatDatepickerModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatSidenavModule,
    MatTooltipModule,
    TableCardComponent,
    BookingFormComponent,
  ],
})
export class FloorPlanComponent {
  // 1. Injections
  private readonly floorService = inject(FloorService);
  private readonly floorHub = inject(FloorHubService);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly snackBar = inject(MatSnackBar);
  private readonly dialog = inject(MatDialog);
  private readonly destroyRef = inject(DestroyRef);

  // 2. Signals d'état
  protected readonly selectedDate = signal<string>(format(new Date(), 'yyyy-MM-dd'));
  protected readonly selectedServiceId = signal<string>('');
  protected readonly snapshot = signal<FloorSnapshot | null>(null);
  protected readonly connectionState = this.floorHub.connectionState;
  protected readonly drawerOpen = signal(false);
  protected readonly drawerTable = signal<TableState | null>(null);

  // 3. Signals dérivés
  protected readonly services = toSignal(this.floorService.getServices(), { initialValue: [] as DiningService[] });

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

  protected readonly selectedDateAsDate = computed(() =>
    parse(this.selectedDate(), 'yyyy-MM-dd', new Date()),
  );

  protected readonly isLoading = computed(
    () => this.selectedServiceId() !== '' && this.snapshot() === null,
  );

  protected readonly connectionTooltip = computed(() => {
    switch (this.connectionState()) {
      case 'connected':  return 'Temps réel actif';
      case 'connecting': return 'Connexion en cours…';
      default:           return 'Hors ligne';
    }
  });

  // 4. Effects et souscriptions

  // Connecte le hub quand date ou service change
  readonly #connectEffect = effect(() => {
    const date = this.selectedDate();
    const serviceId = this.selectedServiceId();
    if (!serviceId) return;
    untracked(() => this.floorHub.connect(date, serviceId));
  });

  // Recharge le snapshot dès que connexion ET service sont disponibles (initial + reconnexion — AC 3.3.1, 3.3.3)
  // Le computed combine les trois signaux pour éviter la race condition entre connectionState et selectedServiceId
  readonly #snapshotOnConnect = toObservable(
    computed(() => ({
      state: this.floorHub.connectionState(),
      date: this.selectedDate(),
      serviceId: this.selectedServiceId(),
    })),
  ).pipe(
    filter(({ state, serviceId }) => state === 'connected' && !!serviceId),
    switchMap(({ date, serviceId }) => {
      this.snapshot.set(null);
      return this.floorService.getSnapshot(date, serviceId);
    }),
    takeUntilDestroyed(this.destroyRef),
  ).subscribe(snap => this.snapshot.set(snap));

  // Patche le statut d'une table sur événement SignalR (AC 3.3.2)
  readonly #tableStatusSub = this.floorHub.tableStatusChanged$.pipe(
    takeUntilDestroyed(this.destroyRef),
  ).subscribe(event => {
    this.snapshot.update(s => {
      if (!s) return s;
      return {
        ...s,
        tables: s.tables.map(t =>
          t.id === event.tableId ? { ...t, status: event.newStatus } : t,
        ),
      };
    });
  });

  // Met à jour le compteur de couverts (AC 3.3.4)
  readonly #capacitySub = this.floorHub.serviceCapacityChanged$.pipe(
    takeUntilDestroyed(this.destroyRef),
  ).subscribe(event => {
    this.snapshot.update(s => {
      if (!s) return s;
      return { ...s, totalConfirmedCovers: s.maxCovers - event.remainingCovers };
    });
  });

  // Auto-sélection du premier service
  readonly #autoSelectService = effect(() => {
    const svcs = this.services();
    if (svcs.length > 0 && !this.selectedServiceId()) {
      untracked(() => this.selectedServiceId.set(svcs[0].id));
    }
  });

  // 5. Handlers de template

  protected onDateChange(event: MatDatepickerInputEvent<Date>): void {
    if (event.value) {
      this.selectedDate.set(format(event.value, 'yyyy-MM-dd'));
    }
  }

  protected onServiceSelected(serviceId: string): void {
    this.selectedServiceId.set(serviceId);
  }

  protected onTableClick(table: TableState): void {
    switch (table.status) {
      case TableStatus.Free:
        this.drawerTable.set(table);
        this.drawerOpen.set(true);
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

  protected onDrawerClose(): void {
    this.drawerOpen.set(false);
    this.drawerTable.set(null);
  }

  protected onBookingCreated(): void {
    this.drawerOpen.set(false);
    this.drawerTable.set(null);
    // Recharge le snapshot pour afficher la nouvelle réservation
    const date = this.selectedDate();
    const serviceId = this.selectedServiceId();
    if (serviceId) {
      this.snapshot.set(null);
      this.floorService.getSnapshot(date, serviceId).subscribe(snap => this.snapshot.set(snap));
    }
  }

  protected logout(): void {
    this.authService.logout();
  }

  // 6. Méthodes privées

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
