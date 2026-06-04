import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  inject,
  signal,
} from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { format, parseISO } from 'date-fns';
import { fr } from 'date-fns/locale';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatDialogModule, MatDialog, MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { HttpClient } from '@angular/common/http';

export interface ClosedDayDto {
  id: string;
  closedDate: string;
  reason: string;
}

// ── Dialog déclaration fermeture ──────────────────────────────────────────────

@Component({
  selector: 'app-closed-day-form-dialog',
  template: `
    <h2 mat-dialog-title>Déclarer une fermeture</h2>
    <mat-dialog-content class="!pt-2">
      <form [formGroup]="form" class="space-y-4 pt-1">
        <mat-form-field appearance="outline" subscriptSizing="dynamic" class="w-full">
          <mat-label>Date de fermeture *</mat-label>
          <input matInput [matDatepicker]="picker" formControlName="date"
                 [min]="minDate" readonly />
          <mat-datepicker-toggle matIconSuffix [for]="picker" />
          <mat-datepicker #picker />
          <mat-error>Date requise (≥ aujourd'hui)</mat-error>
        </mat-form-field>

        <mat-form-field appearance="outline" subscriptSizing="dynamic" class="w-full">
          <mat-label>Motif *</mat-label>
          <textarea matInput formControlName="reason" rows="3"
                    placeholder="Ex : Travaux, congés annuels…" maxlength="200">
          </textarea>
          <mat-hint align="end">{{ form.get('reason')?.value?.length ?? 0 }}/200</mat-hint>
          <mat-error>Motif obligatoire</mat-error>
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button (click)="ref.close(null)">Annuler</button>
      <button mat-flat-button color="primary" [disabled]="form.invalid" (click)="confirm()">
        Confirmer
      </button>
    </mat-dialog-actions>
  `,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatDatepickerModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ClosedDayFormDialogComponent {
  readonly ref = inject(MatDialogRef<ClosedDayFormDialogComponent>);
  private readonly fb = inject(FormBuilder);

  readonly minDate = new Date();

  readonly form = this.fb.group({
    date: [null as Date | null, Validators.required],
    reason: ['', [Validators.required, Validators.maxLength(200)]],
  });

  confirm(): void {
    if (this.form.invalid) return;
    const v = this.form.getRawValue();
    this.ref.close({
      date: format(v.date as Date, 'yyyy-MM-dd'),
      reason: v.reason,
    });
  }
}

// ── ClosedDaysComponent ───────────────────────────────────────────────────────

@Component({
  selector: 'app-closed-days',
  templateUrl: './closed-days.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    MatButtonModule,
    MatDialogModule,
    MatIconModule,
    MatProgressSpinnerModule,
  ],
})
export class ClosedDaysComponent {
  private readonly http = inject(HttpClient);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly closedDays = signal<ClosedDayDto[]>([]);
  protected readonly isLoading = signal(true);

  constructor() {
    this.loadClosedDays();
  }

  protected formatDate(dateStr: string): string {
    return format(parseISO(dateStr), 'dd MMMM yyyy', { locale: fr });
  }

  protected onDeclare(): void {
    const dialogRef = this.dialog.open(ClosedDayFormDialogComponent, {
      width: '420px',
    });

    dialogRef.afterClosed().subscribe((data: { date: string; reason: string } | null) => {
      if (!data) return;
      this.http
        .post<{ closedDay: ClosedDayDto; cancelledBookingsCount: number }>('/api/closed-days', data)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: res => {
            this.closedDays.update(list => [...list, res.closedDay]);
            this.snackBar.open(
              `Fermeture déclarée — ${res.cancelledBookingsCount} réservation${res.cancelledBookingsCount > 1 ? 's' : ''} annulée${res.cancelledBookingsCount > 1 ? 's' : ''}`,
              'OK',
              { duration: 5000 },
            );
          },
          error: () => {
            this.snackBar.open('Erreur lors de la déclaration de fermeture', 'Fermer', { duration: 4000 });
          },
        });
    });
  }

  private loadClosedDays(): void {
    this.isLoading.set(true);
    this.http
      .get<{ closedDays: ClosedDayDto[] }>('/api/closed-days')
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: res => {
          const days = res.closedDays;
          this.closedDays.set(days);
          this.isLoading.set(false);
        },
        error: () => {
          this.isLoading.set(false);
        },
      });
  }
}
