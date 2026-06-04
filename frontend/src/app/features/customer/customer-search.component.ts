import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  inject,
  signal,
} from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { debounceTime, distinctUntilChanged, filter, switchMap, tap } from 'rxjs/operators';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { HttpClient } from '@angular/common/http';
import { BookingService } from '../../core/services/booking.service';
import { Customer, VipLevel } from '../../core/models/customer.model';

// ── Dialog confirmation lever blacklist ───────────────────────────────────────

import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';

@Component({
  selector: 'app-unblacklist-dialog',
  template: `
    <h2 mat-dialog-title>Lever le blacklist</h2>
    <mat-dialog-content>
      <p class="text-sm text-gray-700">
        Confirmer la levée du blacklist pour
        <strong>{{ data.customerName }}</strong> ?
      </p>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button (click)="ref.close(false)">Annuler</button>
      <button mat-flat-button color="primary" (click)="ref.close(true)">Confirmer</button>
    </mat-dialog-actions>
  `,
  imports: [MatDialogModule, MatButtonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UnblacklistDialogComponent {
  readonly data = inject<{ customerName: string }>(MAT_DIALOG_DATA);
  readonly ref = inject(MatDialogRef<UnblacklistDialogComponent>);
}

// ── CustomerSearchComponent ───────────────────────────────────────────────────

@Component({
  selector: 'app-customer-search',
  templateUrl: './customer-search.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
  ],
})
export class CustomerSearchComponent {
  private readonly bookingService = inject(BookingService);
  private readonly http = inject(HttpClient);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly searchCtrl = new FormControl('');
  protected readonly customers = signal<Customer[]>([]);
  protected readonly isSearching = signal(false);
  protected readonly hasSearched = signal(false);

  protected readonly hasResults = computed(() => this.customers().length > 0);
  protected readonly isEmpty = computed(() => this.hasSearched() && !this.isSearching() && !this.hasResults());

  protected readonly VipLevel = VipLevel;

  protected readonly vipLabelMap: Record<VipLevel, string> = {
    [VipLevel.None]: '',
    [VipLevel.Regular]: 'Regular',
    [VipLevel.VIP]: 'VIP',
    [VipLevel.VVIP]: 'VVIP',
  };

  constructor() {
    this.searchCtrl.valueChanges.pipe(
      debounceTime(400),
      distinctUntilChanged(),
      tap(v => {
        if (!v || v.length < 3) {
          this.customers.set([]);
          this.hasSearched.set(false);
          this.isSearching.set(false);
        }
      }),
      filter(v => (v?.length ?? 0) >= 3),
      tap(() => {
        this.isSearching.set(true);
        this.hasSearched.set(true);
      }),
      switchMap(v => {
        const isEmail = v!.includes('@');
        return isEmail
          ? this.bookingService.searchCustomers(undefined, v!)
          : this.bookingService.searchCustomers(v!, undefined);
      }),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe(res => {
      this.customers.set(res.customers);
      this.isSearching.set(false);
    });
  }

  protected onUnblacklist(customer: Customer): void {
    const dialogRef = this.dialog.open(UnblacklistDialogComponent, {
      width: '380px',
      data: { customerName: `${customer.firstName} ${customer.lastName}` },
    });

    dialogRef.afterClosed().subscribe((confirmed: boolean) => {
      if (!confirmed) return;
      this.http
        .patch<Customer>(`/api/customers/${customer.id}/blacklist`, { isBlacklisted: false })
        .subscribe({
          next: updated => {
            this.customers.update(list =>
              list.map(c => (c.id === updated.id ? updated : c)),
            );
            this.snackBar.open('Blacklist levé avec succès', 'OK', { duration: 3000 });
          },
          error: () => {
            this.snackBar.open('Erreur lors de la levée du blacklist', 'Fermer', { duration: 4000 });
          },
        });
    });
  }
}
