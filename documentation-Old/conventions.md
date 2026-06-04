# Conventions — Système de Réservation Restaurant

## Git

### Branches

```
main          Production — merge via PR uniquement, protégée
develop       Intégration — base de toutes les features
feature/*     Nouvelle fonctionnalité  (ex. feature/booking-floor-plan)
fix/*         Correction de bug        (ex. fix/late-cancel-flag)
test/*        Ajout/amélioration tests (ex. test/rb007-cancellation)
```

### Format des commits (Conventional Commits)

```
<type>(<scope>): <description courte en français>

Types : feat | fix | test | refactor | docs | chore
Scopes : domain | application | api | frontend | db | ci

Exemples :
feat(domain): ajouter règle RB-007 annulation tardive
test(domain): couvrir cas limite no-show count = 3
fix(api): corriger statut HTTP sur conflit de table
chore(db): migration ajout index bookings_date_service
```

- Un commit = une intention. Pas de "fix stuff" ou "wip".
- Les tests sont committés **avec** le code qu'ils couvrent (pas après).

---

## C# / .NET

### Nommage

| Élément                  | Convention          | Exemple                          |
|--------------------------|---------------------|----------------------------------|
| Classes, Records         | PascalCase          | `BookingService`, `TableLock`    |
| Interfaces               | `I` + PascalCase    | `IBookingRepository`             |
| Méthodes                 | PascalCase          | `TransitionTo`, `CalculateCost`  |
| Propriétés               | PascalCase          | `GuestsCount`, `ArrivalTime`     |
| Champs privés            | `_` + camelCase     | `_repository`, `_clock`          |
| Paramètres / variables   | camelCase           | `bookingId`, `guestsCount`       |
| Constantes               | PascalCase          | `MaxAdvanceDays`, `LateCancelThresholdHours` |
| Enums                    | PascalCase (valeurs)| `BookingStatus.Confirmed`        |

### Organisation des fichiers Domain

```
Reservation.Domain/
├── Entities/
│   ├── Booking.cs           # Agrégat principal
│   ├── Table.cs
│   ├── Customer.cs
│   └── DiningService.cs
├── ValueObjects/
│   ├── TimeSlot.cs          # [ArrivalTime, ArrivalTime + durée[
│   └── GuestCount.cs
├── Enums/
│   ├── BookingStatus.cs
│   ├── TableZone.cs
│   └── BookingSource.cs
├── Exceptions/
│   ├── BookingConflictException.cs
│   ├── ServiceFullyBookedException.cs
│   └── InvalidStatusTransitionException.cs
└── Events/
    ├── BookingConfirmedEvent.cs
    └── ClosedDayDeclaredEvent.cs
```

### Règles Domain

- Les entités **ne dépendent que** d'autres entités du domaine. Aucun `using` vers Infrastructure ou Application.
- Les règles métier sont des **méthodes sur l'entité** ou des **Value Objects** — pas dans des services statiques.
- Les exceptions domaine sont **spécifiques** (pas `Exception` ou `ArgumentException`).
- Injection de l'horloge via `IClock` (jamais `DateTime.UtcNow` directement → testabilité).

### Commandes / Queries (MediatR)

```
Application/
├── Bookings/
│   ├── Commands/
│   │   ├── CreateBookingCommand.cs      + Handler
│   │   ├── CancelBookingCommand.cs      + Handler
│   │   └── ChangeBookingStatusCommand.cs + Handler
│   └── Queries/
│       ├── GetFloorSnapshotQuery.cs     + Handler
│       └── GetAvailableTablesQuery.cs   + Handler
```

- Une commande = un fichier avec `Command` + `Handler` (record + classe nested ou fichier séparé).
- Les handlers ne contiennent **pas** de logique métier — ils orchestrent uniquement.
- Les DTOs en sortie sont des `record` C# (immuables).

### Tests

```csharp
// Nommage des méthodes de test
[Fact]
public void Should_[résultat_attendu]_When_[condition]()

// Exemples
public void Should_ThrowException_When_GuestsCountExceedsCapacity()
public void Should_SetLateCancel_When_CancelledLessThan24HoursBefore()
public void Should_IncrementNoShowCount_When_CustomerMarkedNoShow()
```

- Structure **AAA** : Arrange / Act / Assert — séparés par une ligne vide.
- Pas de logique dans les tests (pas de boucles, pas de conditions).
- `[Theory]` + `[InlineData]` pour les cas limites numériques (seuils, bornes).
- Un test = une assertion principale (les asserts secondaires tolérés si liés).

---

## Angular / TypeScript

### Nommage

| Élément            | Convention                        | Exemple                             |
|--------------------|-----------------------------------|-------------------------------------|
| Composants         | PascalCase + suffixe              | `FloorPlanComponent`                |
| Services           | PascalCase + `Service`            | `BookingService`, `SignalRService`  |
| Interfaces/Models  | PascalCase                        | `Booking`, `FloorSnapshot`          |
| Fichiers           | kebab-case + type                 | `floor-plan.component.ts`           |
| Signals            | camelCase                         | `floorSnapshot`, `selectedService`  |
| Computed signals   | camelCase (descriptif)            | `tablesBySalle`, `totalCovers`      |
| Méthodes template  | camelCase verbe                   | `onTableClick()`, `onServiceChange()`|

### Structure d'un composant

```typescript
@Component({ ... })
export class FloorPlanComponent {
  // 1. Injections (inject())
  private readonly bookingService = inject(BookingService);

  // 2. Inputs / Outputs
  @Input() date!: Date;

  // 3. Signals d'état
  floorSnapshot = signal<FloorSnapshot | null>(null);
  isLoading = signal(false);

  // 4. Computed signals
  tablesBySalle = computed(() =>
    this.floorSnapshot()?.tables.filter(t => t.zone === 'Salle') ?? []
  );

  // 5. Méthodes
  onTableClick(table: TableState): void { ... }
}
```

- Préférer `inject()` à l'injection par constructeur.
- Pas de logique dans les templates — extraire dans des `computed()` ou des méthodes.
- Les appels HTTP retournent des `Observable` dans les services, convertis en signals dans les composants via `toSignal()`.

### Modèles TypeScript

```typescript
// Toujours des interfaces pour les réponses API
export interface Booking {
  id: string;
  tableId: string | null;
  customerId: string;
  guestsCount: number;
  status: BookingStatus;
  arrivalTime: string;   // ISO string — conversion via date-fns côté affichage
}

// Enums alignés sur le backend
export enum BookingStatus {
  Pending   = 'Pending',
  Confirmed = 'Confirmed',
  Seated    = 'Seated',
  Completed = 'Completed',
  Cancelled = 'Cancelled',
  NoShow    = 'NoShow',
}
```

---

## API — Conventions REST

| Cas                      | Code HTTP  | Exemple                                      |
|--------------------------|------------|----------------------------------------------|
| Création réussie         | `201`      | `POST /api/bookings`                         |
| Lecture réussie          | `200`      | `GET /api/bookings/{id}`                     |
| Mise à jour réussie      | `200`      | `PUT /api/bookings/{id}`                     |
| Règle métier violée      | `422`      | Conflit de table, capacité dépassée          |
| Ressource introuvable    | `404`      |                                              |
| Transition invalide      | `409`      | Statut → statut interdit                     |
| Erreur de validation DTO | `400`      | Champ manquant, format invalide              |

Structure d'erreur unifiée :

```json
{
  "code": "BOOKING_CONFLICT",
  "message": "La table 4 est déjà réservée sur ce créneau.",
  "details": { "tableId": "...", "conflictingBookingId": "..." }
}
```
