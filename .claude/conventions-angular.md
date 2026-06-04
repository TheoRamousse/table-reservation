# Conventions Angular / TypeScript

## Nommage

| Élément              | Convention                   | Exemple                                  |
|----------------------|------------------------------|------------------------------------------|
| Composants           | PascalCase + `Component`     | `FloorPlanComponent`                     |
| Services             | PascalCase + `Service`       | `BookingService`, `SignalRService`       |
| Interfaces / Models  | PascalCase                   | `Booking`, `FloorSnapshot`, `TableState` |
| Enums                | PascalCase (type + valeurs)  | `BookingStatus.Confirmed`                |
| Fichiers composants  | kebab-case + `.component.ts` | `floor-plan.component.ts`                |
| Fichiers services    | kebab-case + `.service.ts`   | `booking.service.ts`                     |
| Fichiers models      | kebab-case + `.model.ts`     | `booking.model.ts`                       |
| Signals              | camelCase                    | `floorSnapshot`, `selectedService`       |
| Computed signals     | camelCase, descriptif        | `tablesBySalle`, `totalCovers`           |
| Handlers de template | `on` + PascalCase            | `onTableClick()`, `onServiceChange()`    |
| Pipes                | camelCase                    | `bookingStatusPipe`                      |

## Structure d'un composant

Ordre strict des membres dans la classe :

```typescript
@Component({
  selector: 'app-floor-plan',
  templateUrl: './floor-plan.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FloorPlanComponent {
  // 1. Injections (inject() — jamais de constructeur)
  private readonly bookingService = inject(BookingService);
  private readonly signalRService = inject(SignalRService);

  // 2. Inputs / Outputs
  readonly date = input.required<Date>();
  readonly tableSelected = output<TableState>();

  // 3. Signals d'état (writable)
  protected readonly isLoading = signal(false);
  protected readonly floorSnapshot = signal<FloorSnapshot | null>(null);

  // 4. Computed signals (dérivés, readonly)
  protected readonly tablesBySalle = computed(() =>
    this.floorSnapshot()?.tables.filter(t => t.zone === 'Salle') ?? []
  );
  protected readonly totalCovers = computed(() =>
    this.floorSnapshot()?.totalConfirmedCovers ?? 0
  );

  // 5. Lifecycle hooks
  readonly #loadEffect = effect(() => {
    this.loadSnapshot(this.date());
  });

  // 6. Méthodes publiques/protégées
  protected onTableClick(table: TableState): void { ... }

  // 7. Méthodes privées
  private async loadSnapshot(date: Date): Promise<void> { ... }
}
```

## Règles Angular Signals

- `signal()` pour l'état local mutable.
- `computed()` pour tout état dérivé — jamais recalculé manuellement.
- `effect()` pour les effets de bord (appels HTTP déclenchés par un signal) — limiter leur usage.
- `toSignal()` pour convertir un Observable en signal dans un composant.
- Jamais de mutation directe d'un objet dans un signal — toujours `.set()` ou `.update()` :
  ```typescript
  // ✅
  this.floorSnapshot.update(s => ({ ...s, tables: updatedTables }));
  // ❌
  this.floorSnapshot()!.tables.push(newTable);
  ```
- `ChangeDetectionStrategy.OnPush` sur tous les composants.

## Services HTTP

```typescript
// Dans le service : retourner Observable
@Injectable({ providedIn: 'root' })
export class BookingService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/bookings';

  getById(id: string): Observable<Booking> {
    return this.http.get<Booking>(`${this.baseUrl}/${id}`);
  }

  create(command: CreateBookingCommand): Observable<Booking> {
    return this.http.post<Booking>(this.baseUrl, command);
  }
}

// Dans le composant : convertir en signal avec toSignal()
readonly booking = toSignal(
  this.bookingService.getById(this.bookingId()),
  { initialValue: null }
);
```

- Les services ne gèrent **pas** les erreurs UI (pas de snackbar dans un service).
- Les services exposent uniquement des méthodes, pas de state partagé (sauf SignalRService).
- Les URLs d'API sont relatives — jamais d'URL absolue hardcodée.

## Modèles TypeScript

```typescript
// Interfaces pour les réponses API (jamais de class)
export interface Booking {
  id: string;
  tableId: string | null;
  customerId: string;
  guestsCount: number;
  status: BookingStatus;
  arrivalTime: string;        // ISO string — affichage via date-fns
  specialRequests: string | null;
  hasAllergyAlert: boolean;
}

// Enums alignés exactement sur le backend (mêmes valeurs string)
export enum BookingStatus {
  Pending   = 'Pending',
  Confirmed = 'Confirmed',
  Seated    = 'Seated',
  Completed = 'Completed',
  Cancelled = 'Cancelled',
  NoShow    = 'NoShow',
  Rejected  = 'Rejected',
}

// Commands (corps de requêtes POST/PUT) : interfaces séparées
export interface CreateBookingCommand {
  tableId: string | null;
  customerId: string;
  serviceId: string;
  bookingDate: string;       // 'YYYY-MM-DD'
  arrivalTime: string;       // 'HH:mm'
  guestsCount: number;
  specialRequests?: string;
  source: BookingSource;
}
```

## Templates HTML

- Zéro logique dans le template — extraire dans `computed()` ou une méthode.
  ```html
  <!-- ✅ -->
  <span [class]="tableStatusClass()">{{ table.number }}</span>

  <!-- ❌ -->
  <span [class]="table.status === 'Confirmed' ? 'blue' : table.status === 'Seated' ? 'orange' : 'green'">
  ```
- Utiliser `@if` / `@for` / `@switch` (syntaxe Angular 17+ — pas de `*ngIf`/`*ngFor`).
- Toujours fournir un `track` dans `@for` :
  ```html
  @for (table of tablesBySalle(); track table.id) { ... }
  ```
- Les événements utilisent `(click)` etc. et appellent une méthode nommée `onXxx()`.

## CSS / Tailwind

- Classes utilitaires Tailwind directement dans le template — pas de CSS custom sauf exception.
- Les couleurs de statut sont déclarées dans `tailwind.config.js` sous `theme.extend.colors` :
  ```js
  tableLibre:    '#22c55e',
  tableReservee: '#3b82f6',
  tableSeated:   '#f97316',
  tableNoShow:   '#ef4444',
  tableInactive: '#9ca3af',
  ```
- Jamais de `style=""` inline pour les couleurs — toujours une classe CSS.

## Gestion des erreurs frontend

```typescript
// Intercepteur global HTTP → traduction des codes erreur API en messages
// 422 → message métier depuis error.code
// 404 → "Ressource introuvable"
// 500 → "Erreur serveur, réessayez"

// Dans le composant : gérer l'état d'erreur via signal
protected readonly error = signal<string | null>(null);

this.bookingService.create(command).subscribe({
  next: () => { ... },
  error: (err: ApiError) => this.error.set(err.message),
});
```

## SignalR — intégration

```typescript
// SignalRService centralisé — un seul hub connecté
@Injectable({ providedIn: 'root' })
export class SignalRService {
  private connection: HubConnection;

  readonly tableStatusChanged = toSignal(
    fromEventPattern<TableStatusChangedEvent>(
      handler => this.connection.on('TableStatusChanged', handler),
      handler => this.connection.off('TableStatusChanged', handler)
    ),
    { initialValue: null }
  );
}
```

- Les composants s'abonnent au signal SignalR via `effect()` — ils ne créent jamais leur propre connexion Hub.

## Tests Angular

- Un fichier `.spec.ts` par composant et par service.
- Utiliser `TestBed` uniquement pour les tests d'intégration — les tests unitaires de logique pure (computed, méthodes) n'ont pas besoin de `TestBed`.
- Mocker les services avec des objets partiels via `jasmine.createSpyObj`.
- Catégoriser avec un commentaire `// @unit` ou `// @integration` en tête de fichier.
