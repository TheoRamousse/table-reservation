import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  effect,
  inject,
  input,
  output,
  signal,
  untracked,
} from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  FormControl,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { Router } from '@angular/router';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { of, switchMap } from 'rxjs';
import { debounceTime, distinctUntilChanged, filter, tap } from 'rxjs/operators';
import { format, parse } from 'date-fns';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { AuthService } from '../../core/services/auth.service';
import { FloorService } from '../../core/services/floor.service';
import { AvailabilityService } from '../../core/services/availability.service';
import { BookingService } from '../../core/services/booking.service';
import { DiningService } from '../../core/models/dining-service.model';
import { Booking, BookingSource } from '../../core/models/booking.model';
import { Customer } from '../../core/models/customer.model';
import { AvailableTable } from '../../core/models/table.model';

interface SelectedTable {
  id: string;
  number: number;
  zone: string;
  capacity: number;
  isCombinable?: boolean;
}

interface RouterState {
  tableId?: string;
  tableNumber?: number;
  tableZone?: string;
  tableCapacity?: number;
  date?: string;
  serviceId?: string;
  guestsCount?: number;
}

function generateTimeSlots(startTime: string, lastBookingTime: string): { value: string; label: string }[] {
  const toMinutes = (t: string) => { const [h, m] = t.split(':').map(Number); return h * 60 + m; };
  const slots: { value: string; label: string }[] = [];
  const start = toMinutes(startTime);
  const end = toMinutes(lastBookingTime);
  for (let t = start; t <= end; t += 15) {
    const h = Math.floor(t / 60), m = t % 60;
    slots.push({
      value: `${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}`,
      label: `${String(h).padStart(2, '0')}h${String(m).padStart(2, '0')}`,
    });
  }
  return slots;
}

const ERROR_MESSAGES: Record<string, string> = {
  TIME_OUTSIDE_SERVICE: "L'heure saisie est en dehors du créneau de ce service.",
  GUESTS_BELOW_MIN: "Le nombre de couverts est inférieur au minimum de la table.",
  GUESTS_EXCEED_CAPACITY: "Trop de couverts pour cette table.",
  SERVICE_FULLY_BOOKED: "Ce service est complet, toutes les places sont réservées.",
  TABLE_CONFLICT: "Cette table est déjà occupée sur ce créneau. Choisissez une autre heure.",
  HORIZON_EXCEEDED: "La date est trop éloignée pour réserver à l'avance.",
  MIN_LEAD_TIME_VIOLATED: "Délai trop court avant le service. Réservez au moins 2 heures à l'avance.",
  BOOKING_DATE_IN_PAST: "La date choisie est dans le passé.",
  CUSTOMER_BLACKLISTED: "Ce client ne peut pas effectuer de réservation. Contactez l'établissement.",
  RESTAURANT_CLOSED: "Le restaurant est fermé à cette date.",
  WALKIN_MUST_BE_TODAY: "Une réservation sans délai (walk-in) doit être pour aujourd'hui.",
};

@Component({
  selector: 'app-booking-form',
  templateUrl: './booking-form.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatDatepickerModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
  ],
})
export class BookingFormComponent {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);
  private readonly floorService = inject(FloorService);
  private readonly availabilityService = inject(AvailabilityService);
  private readonly bookingService = inject(BookingService);
  private readonly destroyRef = inject(DestroyRef);

  // Inputs (mode drawer depuis FloorPlan)
  readonly drawerMode = input<boolean>(false);
  readonly prefilledTableId = input<string | null>(null);
  readonly prefilledTableNumber = input<number | null>(null);
  readonly prefilledTableZone = input<string | null>(null);
  readonly prefilledTableCapacity = input<number | null>(null);
  readonly prefilledTableIsCombinable = input<boolean | null>(null);
  readonly prefilledDate = input<string | null>(null);
  readonly prefilledServiceId = input<string | null>(null);
  readonly prefilledGuestsCount = input<number | null>(null);

  // Outputs
  readonly bookingCreated = output<Booking>();
  readonly closed = output<void>();

  // Rôle
  protected readonly isStaff = computed(() => {
    const r = this.auth.role();
    return r === 'Staff' || r === 'Manager' || r === 'Admin';
  });
  protected readonly isOnline = computed(() => this.auth.role() === 'Online');

  protected readonly minDate = new Date();

  protected readonly services = toSignal(
    this.floorService.getServices(),
    { initialValue: [] as DiningService[] },
  );

  // Formulaire principal
  protected readonly form = this.fb.group({
    firstName: ['', [Validators.required, Validators.minLength(2)]],
    lastName: ['', [Validators.required, Validators.minLength(2)]],
    email: ['', [Validators.email]],
    phone: ['', [Validators.pattern(/^0[0-9]{9}$/)]],
    date: [new Date() as Date | null, [Validators.required, this.dateNotInPast.bind(this)]],
    serviceId: ['', Validators.required],
    arrivalTime: ['', Validators.required],
    guestsCount: [2, [Validators.required, Validators.min(1), Validators.max(30)]],
    specialRequests: ['', Validators.maxLength(500)],
  });

  // Recherche client (Staff uniquement)
  protected readonly customerSearchCtrl = new FormControl('');
  protected readonly foundCustomer = signal<Customer | null>(null);
  protected readonly isSearchingCustomer = signal(false);

  // Table sélectionnée
  protected readonly selectedTable = signal<SelectedTable | null>(null);

  // Table secondaire (fusion)
  protected readonly secondaryTableId = signal<string | null>(null);
  protected readonly combinableTables = signal<AvailableTable[]>([]);

  // État
  protected readonly apiError = signal<string | null>(null);
  protected readonly isSubmitting = signal(false);
  protected readonly successBooking = signal<Booking | null>(null);
  protected readonly successCustomerName = signal('');

  // Dérivés réactifs depuis le formulaire
  private readonly serviceId$ = toSignal(
    this.form.get('serviceId')!.valueChanges,
    { initialValue: this.form.get('serviceId')!.value as string },
  );
  private readonly specialRequests$ = toSignal(
    this.form.get('specialRequests')!.valueChanges,
    { initialValue: '' },
  );

  protected readonly selectedService = computed(() =>
    this.services().find(s => s.id === this.serviceId$()) ?? null,
  );

  protected readonly timeSlots = computed(() => {
    const svc = this.selectedService();
    return svc ? generateTimeSlots(svc.startTime, svc.lastBookingTime) : [];
  });

  protected readonly detectedFlags = computed(() => {
    const sr = this.specialRequests$() ?? '';
    return {
      hasAllergyAlert: /allergi/i.test(sr),
      isCelebration: /anniversaire|célébration|fête/i.test(sr),
      needsHighChair: /chaise|bébé|enfant/i.test(sr),
    };
  });

  protected readonly blockedByBlacklist = computed(
    () => this.foundCustomer()?.isBlacklisted === true,
  );

  protected readonly successServiceName = computed(() => {
    const b = this.successBooking();
    return b ? (this.services().find(s => s.id === b.serviceId)?.name ?? '') : '';
  });

  protected readonly showFusionSection = computed(
    () => this.selectedTable()?.isCombinable === true,
  );

  protected readonly selectedSecondaryTable = computed(() => {
    const id = this.secondaryTableId();
    if (!id) return null;
    return this.combinableTables().find(t => t.id === id) ?? null;
  });

  protected readonly combinedCapacity = computed(() => {
    const primary = this.selectedTable();
    const secondary = this.selectedSecondaryTable();
    if (!primary || !secondary) return null;
    return primary.capacity + secondary.capacity;
  });

  protected readonly combinableTablesForSelect = computed(() =>
    this.combinableTables().filter(t => t.id !== this.selectedTable()?.id),
  );

  constructor() {
    // Pré-remplissage depuis l'état de navigation (mode page)
    const state = this.router.lastSuccessfulNavigation?.extras.state as RouterState | undefined;
    if (state?.tableId) {
      this.selectedTable.set({
        id: state.tableId,
        number: state.tableNumber ?? 0,
        zone: state.tableZone ?? '',
        capacity: state.tableCapacity ?? 0,
      });
    }
    if (state?.date) this.form.get('date')?.setValue(parse(state.date, 'yyyy-MM-dd', new Date()), { emitEvent: false });
    if (state?.serviceId) this.form.get('serviceId')?.setValue(state.serviceId, { emitEvent: false });
    if (state?.guestsCount) this.form.get('guestsCount')?.setValue(state.guestsCount, { emitEvent: false });

    // Sync form avec inputs (mode drawer)
    effect(() => {
      const id = this.prefilledTableId();
      if (id !== null) {
        untracked(() => this.selectedTable.set({
          id,
          number: this.prefilledTableNumber() ?? 0,
          zone: this.prefilledTableZone() ?? '',
          capacity: this.prefilledTableCapacity() ?? 0,
          isCombinable: this.prefilledTableIsCombinable() ?? false,
        }));
      }
    });
    effect(() => {
      const d = this.prefilledDate();
      if (d) untracked(() => this.form.get('date')?.setValue(parse(d, 'yyyy-MM-dd', new Date()), { emitEvent: false }));
    });
    effect(() => {
      // emitEvent: true (défaut) pour que valueChanges émette et mette à jour timeSlots()
      const s = this.prefilledServiceId();
      if (s) untracked(() => this.form.get('serviceId')?.setValue(s));
    });
    effect(() => {
      const g = this.prefilledGuestsCount();
      if (g !== null) untracked(() => this.form.get('guestsCount')?.setValue(g, { emitEvent: false }));
    });

    // Auto-select premier service si aucun
    effect(() => {
      const svcs = this.services();
      if (svcs.length > 0 && !this.form.get('serviceId')?.value) {
        untracked(() => this.form.get('serviceId')?.setValue(svcs[0].id));
      }
    });

    // Charge les tables combinables quand la table principale est combinable
    effect(() => {
      const table = this.selectedTable();
      const serviceId = this.serviceId$();
      const dateVal = this.form.get('date')?.value as Date | null;
      if (!table?.isCombinable || !serviceId || !dateVal) {
        untracked(() => {
          this.combinableTables.set([]);
          this.secondaryTableId.set(null);
        });
        return;
      }
      const date = format(dateVal, 'yyyy-MM-dd');
      const guestsCount = this.form.get('guestsCount')?.value ?? 1;
      this.availabilityService
        .search(date, serviceId, guestsCount as number)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe(res => {
          const others = res.tables.filter(t => t.isCombinable && t.id !== table.id);
          untracked(() => this.combinableTables.set(others));
        });
    });

    // Recherche client avec debounce (Staff)
    this.customerSearchCtrl.valueChanges.pipe(
      debounceTime(400),
      distinctUntilChanged(),
      tap(v => {
        if (!v || v.length < 3) {
          this.foundCustomer.set(null);
          this.isSearchingCustomer.set(false);
        }
      }),
      filter(v => (v?.length ?? 0) >= 3),
      tap(() => this.isSearchingCustomer.set(true)),
      switchMap(v => {
        const isEmail = v!.includes('@');
        return isEmail
          ? this.bookingService.searchCustomers(undefined, v!)
          : this.bookingService.searchCustomers(v!, undefined);
      }),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe(res => {
      const found = res.customers[0] ?? null;
      this.foundCustomer.set(found);
      this.isSearchingCustomer.set(false);
      if (found) {
        this.form.patchValue({
          firstName: found.firstName,
          lastName: found.lastName,
          email: found.email ?? '',
          phone: found.phone,
        });
      }
    });
  }

  protected onSubmit(): void {
    if (this.form.invalid || this.isSubmitting() || this.blockedByBlacklist()) return;

    this.isSubmitting.set(true);
    this.apiError.set(null);

    const v = this.form.getRawValue();
    const source = this.isStaff() ? BookingSource.Staff : BookingSource.Online;

    const customer$ = this.foundCustomer()
      ? of(this.foundCustomer()!)
      : this.bookingService.createCustomer({
          firstName: v.firstName!,
          lastName: v.lastName!,
          email: v.email || undefined,
          phone: v.phone || undefined,
        });

    const dateValue = v.date as unknown as Date | null;
    const bookingDate = dateValue ? format(dateValue, 'yyyy-MM-dd') : '';

    customer$.pipe(
      switchMap(customer =>
        this.bookingService.createBooking({
          tableId: this.selectedTable()?.id ?? null,
          secondaryTableId: this.secondaryTableId() ?? undefined,
          customerId: customer.id,
          serviceId: v.serviceId!,
          bookingDate,
          arrivalTime: v.arrivalTime!,
          guestsCount: v.guestsCount!,
          specialRequests: v.specialRequests || undefined,
          source,
        }),
      ),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: booking => {
        this.successBooking.set(booking);
        this.successCustomerName.set(`${v.firstName} ${v.lastName}`);
        this.isSubmitting.set(false);
        if (this.drawerMode()) this.bookingCreated.emit(booking);
      },
      error: err => {
        const code = err?.error?.code as string | undefined;
        this.apiError.set(ERROR_MESSAGES[code ?? ''] ?? 'Une erreur inattendue s\'est produite. Veuillez réessayer.');
        this.isSubmitting.set(false);
      },
    });
  }

  protected onSecondaryTableChange(tableId: string | null): void {
    this.secondaryTableId.set(tableId || null);
  }

  protected clearCustomer(): void {
    this.foundCustomer.set(null);
    this.customerSearchCtrl.setValue('');
    this.form.patchValue({ firstName: '', lastName: '', email: '', phone: '' });
  }

  protected clearError(): void { this.apiError.set(null); }

  protected onCancel(): void {
    if (this.drawerMode()) this.closed.emit();
    else this.router.navigate(['/availability']);
  }

  protected onNewBooking(): void {
    this.successBooking.set(null);
    this.selectedTable.set(null);
    this.secondaryTableId.set(null);
    this.combinableTables.set([]);
    this.foundCustomer.set(null);
    this.customerSearchCtrl.setValue('');
    this.form.reset({ date: new Date(), guestsCount: 2 });
  }

  protected onBackToFloor(): void { this.closed.emit(); }
  protected onBackToAvailability(): void { this.router.navigate(['/availability']); }

  private dateNotInPast(control: AbstractControl): ValidationErrors | null {
    if (!control.value) return null;
    const date = control.value as Date;
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    return date < today ? { dateInPast: true } : null;
  }
}
