import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  signal,
} from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA, MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { HttpClient } from '@angular/common/http';
import { parse, isAfter, addMinutes } from 'date-fns';
import { TableState } from '../../core/models/table.model';

export interface BookingDetailData {
  table: TableState;
}

// ── Dialog confirmation annulation ────────────────────────────────────────────

@Component({
  selector: 'app-cancel-booking-dialog',
  template: `
    <h2 mat-dialog-title>Annuler la réservation</h2>
    <mat-dialog-content>
      <p class="text-sm text-gray-600 mb-4">
        Veuillez indiquer le motif d'annulation de la réservation de
        <strong>{{ data.customerName }}</strong>.
      </p>
      <form [formGroup]="form">
        <mat-form-field appearance="outline" class="w-full" subscriptSizing="dynamic">
          <mat-label>Motif d'annulation *</mat-label>
          <textarea matInput formControlName="cancellationReason" rows="3"
                    placeholder="Ex : client a rappelé pour annuler…" maxlength="500">
          </textarea>
          <mat-hint align="end">{{ form.get('cancellationReason')?.value?.length ?? 0 }}/500</mat-hint>
          <mat-error>Le motif est obligatoire</mat-error>
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button (click)="ref.close(null)">Annuler</button>
      <button mat-flat-button color="warn" [disabled]="form.invalid" (click)="confirm()">
        Confirmer l'annulation
      </button>
    </mat-dialog-actions>
  `,
  imports: [MatDialogModule, MatButtonModule, MatFormFieldModule, MatInputModule, ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CancelBookingDialogComponent {
  readonly data = inject<{ customerName: string }>(MAT_DIALOG_DATA);
  readonly ref = inject(MatDialogRef<CancelBookingDialogComponent>);
  private readonly fb = inject(FormBuilder);

  readonly form = this.fb.group({
    cancellationReason: ['', [Validators.required, Validators.minLength(3)]],
  });

  confirm(): void {
    if (this.form.invalid) return;
    this.ref.close(this.form.get('cancellationReason')!.value);
  }
}

// ── BookingDetailComponent ─────────────────────────────────────────────────────

@Component({
  selector: 'app-booking-detail',
  templateUrl: './booking-detail.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    MatButtonModule,
    MatDialogModule,
    MatIconModule,
    MatProgressSpinnerModule,
  ],
})
export class BookingDetailComponent {
  readonly data = inject<BookingDetailData>(MAT_DIALOG_DATA);
  readonly ref = inject(MatDialogRef<BookingDetailComponent>);
  private readonly dialog = inject(MatDialog);
  private readonly http = inject(HttpClient);
  private readonly snackBar = inject(MatSnackBar);

  protected readonly isLoading = signal(false);

  protected readonly booking = computed(() => this.data.table.activeBooking!);
  protected readonly status = computed(() => this.data.table.status as string);

  protected readonly canConfirm = computed(() => this.status() === 'Pending');
  protected readonly canCancel = computed(
    () => this.status() === 'Pending' || this.status() === 'Confirmed',
  );
  protected readonly canSeat = computed(() => this.status() === 'Confirmed');
  protected readonly canNoShow = computed(() => {
    if (this.status() !== 'Confirmed') return false;
    const arrival = this.booking().arrivalTime;
    if (!arrival) return false;
    const [h, m] = arrival.split(':').map(Number);
    const arrivalDate = new Date();
    arrivalDate.setHours(h, m, 0, 0);
    return isAfter(new Date(), addMinutes(arrivalDate, 15));
  });
  protected readonly canComplete = computed(() => this.status() === 'Seated');

  protected readonly statusLabel = computed(() => {
    const map: Record<string, string> = {
      Pending: 'En attente',
      Confirmed: 'Réservée',
      Seated: 'Occupée',
      Completed: 'Terminée',
      Cancelled: 'Annulée',
      NoShow: 'No-show',
    };
    return map[this.status()] ?? this.status();
  });

  protected readonly statusClass = computed(() => {
    const map: Record<string, string> = {
      Pending: 'status-en-attente',
      Confirmed: 'status-reservee',
      Seated: 'status-occupee',
      NoShow: 'status-noshow',
      Cancelled: 'bg-gray-200 text-gray-700',
    };
    return map[this.status()] ?? 'bg-gray-100 text-gray-700';
  });

  protected onConfirm(): void {
    this.updateStatus('Confirmed', 'Réservation confirmée');
  }

  protected onSeat(): void {
    this.updateStatus('Seated', 'Client assis');
  }

  protected onNoShow(): void {
    this.updateStatus('NoShow', 'No-show enregistré');
  }

  protected onComplete(): void {
    this.updateStatus('Completed', 'Table terminée');
  }

  protected onCancel(): void {
    const dialogRef = this.dialog.open(CancelBookingDialogComponent, {
      width: '420px',
      data: { customerName: this.booking().customerName },
    });

    dialogRef.afterClosed().subscribe((reason: string | null) => {
      if (!reason) return;
      this.isLoading.set(true);
      this.http
        .delete(`/api/bookings/${this.booking().bookingId}`, {
          body: { cancellationReason: reason },
        })
        .subscribe({
          next: () => {
            this.isLoading.set(false);
            this.snackBar.open('Réservation annulée', 'OK', { duration: 3000 });
            this.ref.close({ action: 'cancelled', bookingId: this.booking().bookingId });
          },
          error: () => {
            this.isLoading.set(false);
            this.snackBar.open('Erreur lors de l\'annulation', 'Fermer', { duration: 4000 });
          },
        });
    });
  }

  private updateStatus(newStatus: string, successMessage: string): void {
    this.isLoading.set(true);
    this.http
      .patch(`/api/bookings/${this.booking().bookingId}/status`, { newStatus })
      .subscribe({
        next: () => {
          this.isLoading.set(false);
          this.snackBar.open(successMessage, 'OK', { duration: 3000 });
          this.ref.close({ action: 'updated', bookingId: this.booking().bookingId, newStatus });
        },
        error: () => {
          this.isLoading.set(false);
          this.snackBar.open('Erreur lors de la mise à jour', 'Fermer', { duration: 4000 });
        },
      });
  }
}
