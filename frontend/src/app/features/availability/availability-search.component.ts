import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  effect,
  inject,
  signal,
  computed,
  untracked,
} from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { Router } from '@angular/router';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { format } from 'date-fns';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { FloorService } from '../../core/services/floor.service';
import { AvailabilityService } from '../../core/services/availability.service';
import { DiningService } from '../../core/models/dining-service.model';
import { AvailableTable } from '../../core/models/table.model';

@Component({
  selector: 'app-availability-search',
  templateUrl: './availability-search.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
  ],
})
export class AvailabilitySearchComponent {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly availabilityService = inject(AvailabilityService);
  private readonly floorService = inject(FloorService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly today = format(new Date(), 'yyyy-MM-dd');

  protected readonly services = toSignal(
    this.floorService.getServices(),
    { initialValue: [] as DiningService[] },
  );

  protected readonly searchState = signal<'idle' | 'loading' | 'done'>('idle');
  protected readonly results = signal<AvailableTable[]>([]);
  protected readonly fullyBooked = signal(false);
  protected readonly isSearching = computed(() => this.searchState() === 'loading');

  protected readonly lastDate = signal('');
  protected readonly lastServiceName = signal('');
  protected readonly lastGuestsCount = signal(0);
  protected readonly lastZoneFiltered = signal(false);

  protected readonly searchForm = this.fb.group({
    date: [this.today, [Validators.required, this.dateNotInPast.bind(this)]],
    serviceId: ['', Validators.required],
    guestsCount: [2, [Validators.required, Validators.min(1), Validators.max(30)]],
    zone: [null as string | null],
  });

  readonly #autoSelectService = effect(() => {
    const svcs = this.services();
    if (svcs.length > 0 && !this.searchForm.get('serviceId')?.value) {
      untracked(() => this.searchForm.get('serviceId')?.setValue(svcs[0].id));
    }
  });

  protected onSearch(): void {
    if (this.searchForm.invalid || this.isSearching()) return;

    const { date, serviceId, guestsCount, zone } = this.searchForm.getRawValue();
    const serviceName = this.services().find(s => s.id === serviceId)?.name ?? '';

    this.lastDate.set(date ?? '');
    this.lastServiceName.set(serviceName);
    this.lastGuestsCount.set(guestsCount ?? 0);
    this.lastZoneFiltered.set(!!zone);
    this.searchState.set('loading');
    this.results.set([]);
    this.fullyBooked.set(false);

    this.availabilityService
      .search(date!, serviceId!, guestsCount!, zone ?? undefined)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: res => {
          this.results.set(res.tables);
          this.fullyBooked.set(res.reason === 'ServiceFullyBooked');
          this.searchState.set('done');
        },
        error: () => this.searchState.set('done'),
      });
  }

  protected onChooseTable(table: AvailableTable): void {
    const { date, serviceId, guestsCount } = this.searchForm.getRawValue();
    this.router.navigate(['/bookings/new'], {
      state: {
        tableId: table.id,
        tableNumber: table.number,
        tableZone: table.zone,
        tableCapacity: table.capacity,
        date,
        serviceId,
        guestsCount,
      },
    });
  }

  protected resetDate(): void {
    this.searchForm.get('date')?.setValue(this.today);
    this.searchState.set('idle');
  }

  protected resetService(): void {
    const svcs = this.services();
    this.searchForm.get('serviceId')?.setValue(svcs[0]?.id ?? '');
    this.searchState.set('idle');
  }

  private dateNotInPast(control: AbstractControl): ValidationErrors | null {
    if (!control.value) return null;
    return control.value < format(new Date(), 'yyyy-MM-dd') ? { dateInPast: true } : null;
  }
}
