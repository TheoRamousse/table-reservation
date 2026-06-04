import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  inject,
  signal,
} from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDialogModule, MatDialog, MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { HttpClient } from '@angular/common/http';
import { Table, TableZone } from '../../core/models/table.model';

export interface TableRow extends Table {
  isActive: boolean;
}

// ── Dialog formulaire table ───────────────────────────────────────────────────

export interface TableFormDialogData {
  table?: TableRow;
}

function minCapacityValidator(control: AbstractControl): ValidationErrors | null {
  const group = control.parent;
  if (!group) return null;
  const min = group.get('minCapacity')?.value;
  const max = group.get('capacity')?.value;
  if (min !== null && max !== null && min > max) {
    return { minExceedsCapacity: true };
  }
  return null;
}

@Component({
  selector: 'app-table-form-dialog',
  template: `
    <h2 mat-dialog-title>{{ data.table ? 'Modifier la table' : 'Ajouter une table' }}</h2>
    <mat-dialog-content class="!pt-2">
      <form [formGroup]="form" class="space-y-4 pt-1">
        <div class="grid grid-cols-2 gap-3">
          <mat-form-field appearance="outline" subscriptSizing="dynamic">
            <mat-label>Numéro *</mat-label>
            <input matInput type="number" formControlName="number" min="1" />
            <mat-error>Requis</mat-error>
          </mat-form-field>

          <mat-form-field appearance="outline" subscriptSizing="dynamic">
            <mat-label>Zone *</mat-label>
            <mat-select formControlName="zone">
              @for (zone of zones; track zone) {
                <mat-option [value]="zone">{{ zone }}</mat-option>
              }
            </mat-select>
            <mat-error>Requis</mat-error>
          </mat-form-field>

          <mat-form-field appearance="outline" subscriptSizing="dynamic">
            <mat-label>Capacité min *</mat-label>
            <input matInput type="number" formControlName="minCapacity" min="1" />
            <mat-error>
              @if (form.get('minCapacity')?.hasError('required')) { Requis }
              @if (form.get('minCapacity')?.hasError('minExceedsCapacity')) { Min > capacité }
            </mat-error>
          </mat-form-field>

          <mat-form-field appearance="outline" subscriptSizing="dynamic">
            <mat-label>Capacité max *</mat-label>
            <input matInput type="number" formControlName="capacity" min="1" />
            <mat-error>
              @if (form.get('capacity')?.hasError('required')) { Requis }
            </mat-error>
          </mat-form-field>
        </div>

        <mat-checkbox formControlName="isCombinable">Table combinable</mat-checkbox>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button (click)="ref.close(null)">Annuler</button>
      <button mat-flat-button color="primary" [disabled]="form.invalid" (click)="confirm()">
        {{ data.table ? 'Enregistrer' : 'Ajouter' }}
      </button>
    </mat-dialog-actions>
  `,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatCheckboxModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TableFormDialogComponent {
  readonly data = inject<TableFormDialogData>(MAT_DIALOG_DATA);
  readonly ref = inject(MatDialogRef<TableFormDialogComponent>);
  private readonly fb = inject(FormBuilder);

  readonly zones = Object.values(TableZone);

  readonly form = this.fb.group(
    {
      number: [this.data.table?.number ?? null as number | null, [Validators.required, Validators.min(1)]],
      zone: [this.data.table?.zone ?? TableZone.Salle, Validators.required],
      minCapacity: [this.data.table?.minCapacity ?? null as number | null, [Validators.required, Validators.min(1), minCapacityValidator]],
      capacity: [this.data.table?.capacity ?? null as number | null, [Validators.required, Validators.min(1)]],
      isCombinable: [this.data.table?.isCombinable ?? false],
    },
  );

  confirm(): void {
    if (this.form.invalid) return;
    const v = this.form.getRawValue();
    // Re-validate minCapacity <= capacity
    if ((v.minCapacity ?? 0) > (v.capacity ?? 0)) {
      this.form.get('minCapacity')?.setErrors({ minExceedsCapacity: true });
      return;
    }
    this.ref.close(v);
  }
}

// ── TableManagementComponent ──────────────────────────────────────────────────

@Component({
  selector: 'app-table-management',
  templateUrl: './table-management.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    MatButtonModule,
    MatDialogModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatSlideToggleModule,
    MatTableModule,
    MatTooltipModule,
  ],
})
export class TableManagementComponent {
  private readonly http = inject(HttpClient);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly tables = signal<TableRow[]>([]);
  protected readonly isLoading = signal(true);

  protected readonly displayedColumns = ['number', 'zone', 'capacity', 'isCombinable', 'isActive', 'actions'];

  protected readonly sortedTables = computed(() =>
    [...this.tables()].sort((a, b) => a.number - b.number),
  );

  constructor() {
    this.loadTables();
  }

  protected capacityRange(table: TableRow): string {
    return `${table.minCapacity}–${table.capacity}`;
  }

  protected onToggleActive(table: TableRow): void {
    const updated = { ...table, isActive: !table.isActive };
    this.http
      .put<TableRow>(`/api/tables/${table.id}`, updated)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: res => {
          this.tables.update(list => list.map(t => (t.id === res.id ? res : t)));
        },
        error: () => {
          this.snackBar.open('Erreur lors de la mise à jour', 'Fermer', { duration: 4000 });
        },
      });
  }

  protected onEdit(table: TableRow): void {
    this.openFormDialog(table);
  }

  protected onAdd(): void {
    this.openFormDialog();
  }

  private openFormDialog(table?: TableRow): void {
    const dialogRef = this.dialog.open(TableFormDialogComponent, {
      width: '420px',
      data: { table },
    });

    dialogRef.afterClosed().subscribe((formData: Partial<TableRow> | null) => {
      if (!formData) return;

      if (table) {
        const payload = { ...table, ...formData };
        this.http
          .put<TableRow>(`/api/tables/${table.id}`, payload)
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe({
            next: res => {
              this.tables.update(list => list.map(t => (t.id === res.id ? res : t)));
              this.snackBar.open('Table mise à jour', 'OK', { duration: 3000 });
            },
            error: () => {
              this.snackBar.open('Erreur lors de la mise à jour', 'Fermer', { duration: 4000 });
            },
          });
      } else {
        this.http
          .post<TableRow>('/api/tables', { ...formData, isActive: true })
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe({
            next: res => {
              this.tables.update(list => [...list, res]);
              this.snackBar.open('Table ajoutée', 'OK', { duration: 3000 });
            },
            error: () => {
              this.snackBar.open('Erreur lors de la création', 'Fermer', { duration: 4000 });
            },
          });
      }
    });
  }

  private loadTables(): void {
    this.isLoading.set(true);
    this.http
      .get<{ tables: TableRow[] }>('/api/tables')
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: res => {
          const tables = res.tables;
          this.tables.set(tables);
          this.isLoading.set(false);
        },
        error: () => {
          this.isLoading.set(false);
        },
      });
  }
}
