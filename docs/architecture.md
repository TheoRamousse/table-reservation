---
stepsCompleted: [1, 2, 3, 4, 5, 6, 7, 8]
inputDocuments:
  - documentation/specs.md
  - documentation/stack.md
  - documentation/data-model.md
  - documentation/conventions.md
  - docs/prds/prd-table-reservation-2026-06-04/prd.md
workflowType: architecture
project_name: table-reservation
user_name: Yann
date: 2026-06-04
status: final
---

# Document d'Architecture — Système de Réservation de Tables

---

## 1. Analyse du contexte projet

### Vue d'ensemble des exigences

**Exigences fonctionnelles (depuis le PRD) :**
30 exigences fonctionnelles (FR-1 à FR-30) couvrant :
- Cycle de vie complet d'une réservation (création → complétion), avec 17 règles métier à appliquer automatiquement
- Vue salle en temps réel (Floor Plan) avec SignalR
- Recherche de disponibilité avec filtres date / service / couverts / zone
- Fusion de tables combinables
- Gestion des clients avec système de blacklist automatique
- Notifications asynchrones (confirmation, rappels, annulation)
- Gestion des jours de fermeture avec annulation en cascade
- Administration des tables et services (CRUD)

**Exigences non-fonctionnelles critiques :**
- `GET /api/floor/snapshot` : P95 ≤ 200 ms (50 tables max)
- `GET /api/tables/availability` : P95 ≤ 300 ms
- `POST /api/bookings` (toutes validations) : P95 ≤ 500 ms
- Propagation SignalR : P95 ≤ 2 secondes entre mutation et affichage sur un second terminal
- Disponibilité : 99,5 % sur les heures d'ouverture
- Notifications asynchrones découplées des transactions principales
- WCAG 2.1 AA sur la SPA Angular

### Évaluation de la complexité

- **Domaine technique :** Full-stack (backend API REST + frontend SPA + temps réel)
- **Niveau de complexité :** Élevé
- **Indicateurs clés :**
  - Logique métier riche et à forte densité de règles (17 RB, 30+ FR)
  - Temps réel via WebSocket (SignalR)
  - Multi-rôles avec matrice de droits précise (4 rôles)
  - Gestion d'état distribué (concurrence des réservations)
  - Système de notifications asynchrone découplé
  - Architecture hexagonale (Clean Architecture) avec CQRS

### Préoccupations transversales identifiées

| Préoccupation | Impact architectural |
|---------------|---------------------|
| Concurrence des réservations | Isolation transactionnelle en base, index sur `(booking_date, service_id, status)` |
| Temps réel (SignalR) | Hub dédié, reconnexion automatique côté client Angular |
| Sécurité multi-rôles | Vérification d'autorisation à chaque endpoint (jamais côté client) |
| Notifications asynchrones | Découplage via Domain Events + handler Infrastructure (pas de couplage fort) |
| Testabilité du domaine | Injection `IClock` (pas de `DateTime.UtcNow` direct), Value Objects immuables |
| Observabilité | Logging structuré JSON, traces sur les handlers MediatR |
| Accessibilité | Couleurs de statut doublées d'un label textuel (WCAG 2.1 AA) |

---

## 2. Stack technique retenu

> **Note :** Le stack est entièrement prédéfini — aucune décision de choix de technologie n'est à prendre.

### Backend — .NET 10 / ASP.NET Core 10

| Composant | Version | Rôle |
|-----------|---------|------|
| .NET / C# | 10.0 / 14 | Runtime + langage |
| ASP.NET Core | 10.0 | API REST + SignalR |
| Entity Framework Core | 10.0 | ORM |
| SQLite (Microsoft.Data.Sqlite) | latest | Driver EF Core |
| MediatR | latest | Médiateur CQRS (Commands / Queries) |
| FluentValidation | latest | Validation des DTOs |
| Serilog | latest | Logging structuré JSON |
| Mapster | latest | Mapping Domain ↔ DTO |
| Microsoft.AspNetCore.SignalR | intégré | Temps réel |
| xUnit + FluentAssertions | latest | Tests unitaires |
| Microsoft.EntityFrameworkCore.Sqlite | latest | SQLite in-memory pour tests d'intégration |
| Stryker.NET | latest | Mutation testing |

### Frontend — Angular 20

| Composant | Version | Rôle |
|-----------|---------|------|
| Angular | 20.x | Framework SPA |
| TypeScript | 5.8 | Langage |
| Angular CLI / Vite (esbuild) | 20.x / intégré | Build |
| Angular Signals | natif | Réactivité (pas de NgRx) |
| Angular HttpClient | natif | Appels REST |
| @microsoft/signalr | 8.x | Client SignalR |
| Angular Material | 20.x | Composants UI |
| Tailwind CSS | 4.x | Utilitaires CSS |
| date-fns | latest | Manipulation de dates |
| Angular CDK | 20.x | Drag & drop, overlay |

### Base de données — SQLite

- Clés primaires en `TEXT` (GUID générés par EF Core, format standard UUID)
- Colonnes `created_at` / `updated_at` sur toutes les tables (type `TEXT`, format ISO 8601)
- Soft delete via `deleted_at` nullable (pas de DELETE physique sur les réservations)
- Enums stockés en `TEXT` (lisibilité des données brutes)
- Nommage `snake_case` en base, `PascalCase` en C# via convention globale dans le DbContext
- `PRAGMA foreign_keys = ON` activé au démarrage du DbContext
- Tests d'intégration : `Data Source=:memory:` (SQLite in-memory, sans fichier)

---

## 3. Architecture générale (Clean Architecture)

```
┌─────────────────────────────────────────────────────────┐
│                     Angular 20 (SPA)                    │
│         Vue salle · Calendrier · Gestion admin          │
└────────────────────────┬────────────────────────────────┘
                         │ HTTP/REST + SignalR (WebSocket)
┌────────────────────────▼────────────────────────────────┐
│                  ASP.NET Core 10 (API)                  │
│              Controllers · SignalR Hubs                  │
└────────────────────────┬────────────────────────────────┘
                         │ MediatR (CQRS)
┌────────────────────────▼────────────────────────────────┐
│              Reservation.Application                     │
│          Commands · Queries · Handlers · DTOs           │
└────────────────────────┬────────────────────────────────┘
                         │ Interfaces (abstractions)
┌────────────────────────▼────────────────────────────────┐
│              Reservation.Domain                          │
│       Entités · ValueObjects · Exceptions · Events      │
└─────────────────────────────────────────────────────────┘
                         ↑ implémente les interfaces
┌─────────────────────────────────────────────────────────┐
│              Reservation.Infrastructure                  │
│      EF Core · Repositories · Notifications · IClock    │
└────────────────────────┬────────────────────────────────┘
                         │ EF Core 10
┌────────────────────────▼────────────────────────────────┐
│                  SQLite                                  │
└─────────────────────────────────────────────────────────┘
```

### Règle de dépendance (strict)

```
Domain ← Application ← Infrastructure
Domain ← Application ← Api
```

- `Reservation.Domain` : **zéro dépendance externe** (pas d'EF, pas d'ASP.NET).
- `Reservation.Application` : dépend uniquement de `Reservation.Domain` + interfaces abstraites.
- `Reservation.Infrastructure` : implémente les interfaces définies dans `Application`.
- `Reservation.Api` : orchestre les couches via MediatR, ne contient pas de logique métier.

---

## 4. Décisions architecturales de fond

### 4.1 Pattern CQRS via MediatR

**Décision :** Toutes les opérations passent par un Command ou Query MediatR. Pas d'appel direct de service dans les controllers.

**Rationale :** Isolation des handlers, testabilité, séparation lecture/écriture naturelle, facilite l'ajout de behaviors (validation pipeline, logging, transactions).

**Règles :**
- Un Command = une intention de modification d'état.
- Une Query = une lecture sans effet de bord.
- Les Handlers n'ont **pas** de logique métier — ils orchestrent uniquement (fetch → appel domaine → persist → publier events).
- Les validations FluentValidation s'appliquent en pipeline MediatR avant le Handler.

```csharp
// Command avec record immuable
public record CreateBookingCommand(
    Guid CustomerId, Guid ServiceId, DateOnly BookingDate,
    TimeOnly ArrivalTime, int GuestsCount, string? SpecialRequests,
    BookingSource Source) : IRequest<BookingDto>;

// Handler orchestre, ne décide pas
public class CreateBookingCommandHandler(
    IBookingRepository bookingRepo,
    ITableRepository tableRepo,
    IClock clock) : IRequestHandler<CreateBookingCommand, BookingDto>
{
    public async Task<BookingDto> Handle(CreateBookingCommand cmd, CancellationToken ct)
    {
        var customer = await customerRepo.GetByIdAsync(cmd.CustomerId, ct); // fetch
        var booking = Booking.Create(customer, ...);                         // logique domaine
        await bookingRepo.AddAsync(booking, ct);                             // persist
        return booking.ToDto();                                              // map
    }
}
```

### 4.2 Domaine riche (Rich Domain Model)

**Décision :** Les règles métier sont des méthodes sur les entités ou des Value Objects. Pas de services statiques pour la logique métier.

**Rationale :** Encapsulation totale des invariants, testabilité pure (pas de mock d'infrastructure), impossibilité de contourner les règles.

**Règles :**
- Les entités exposent des méthodes comportementales : `Booking.Cancel(reason)`, `Booking.TransitionTo(status)`, `Customer.RegisterNoShow()`.
- Les invariants sont vérifiés dans les constructeurs / méthodes et lèvent des exceptions domaine spécifiques.
- Jamais `DateTime.UtcNow` directement — toujours `IClock.UtcNow` injecté.

```csharp
// Bonne pratique
public void Cancel(string reason, DateTimeOffset now, UserRole actorRole)
{
    if (Status is BookingStatus.Seated)
        throw new InvalidStatusTransitionException(Status, BookingStatus.Cancelled);
    if (now > ArrivalTime.AddHours(-24))
        LateCancel = true;
    // ...
}

// Interdit
public static bool CanCancel(Booking b) => ...; // logique hors entité
```

### 4.3 Domain Events pour les effets de bord asynchrones

**Décision :** Les effets de bord (notifications, blacklist automatique) sont déclenchés via Domain Events collectés sur l'entité et dispatchés après le SaveChanges.

**Rationale :** Découplage entre la transaction principale et les effets de bord, notifications asynchrones sans bloquer la réponse HTTP.

**Événements déclarés :**
- `BookingConfirmedEvent` → déclenche la notification de confirmation.
- `BookingCancelledByRestaurantEvent` → notification avec motif.
- `CustomerNoShowRegisteredEvent` → vérifie et applique la blacklist si `NoShowCount >= 3`.
- `ClosedDayDeclaredEvent` → annulation en cascade des réservations + notifications.

**Pattern de dispatch (Infrastructure) :**
```csharp
// Dans un SaveChanges interceptor ou UoW, après persist :
foreach (var domainEvent in aggregateRoot.DomainEvents)
    await mediator.Publish(domainEvent, ct);
aggregateRoot.ClearDomainEvents();
```

### 4.4 Temps réel via SignalR

**Décision :** Hub `/hubs/floor` avec groupes par `(date, serviceId)`. Les mutations d'état déclenchent automatiquement les notifications SignalR via un service Infrastructure appelé depuis les handlers.

**Hub et événements :**
```
Hub : /hubs/floor
Groupe : floor-{date}-{serviceId}

Événements émis :
  TableStatusChanged   { tableId, newStatus, bookingId? }
  BookingUpdated       { bookingId, newStatus }
  ServiceCapacityChanged { serviceId, date, remainingCovers }
```

**Reconnexion côté Angular :**
- `@microsoft/signalr` gère la reconnexion automatique avec backoff.
- À la reconnexion, le composant Angular recharge le snapshot complet (`GET /api/floor/snapshot`).

### 4.5 Gestion des transactions et de la concurrence

**Décision :** Transactions explicites autour des opérations de création/modification de réservation pour éviter les race conditions.

**Rationale :** Deux clients pourraient simultanément réserver la même table. Le `SELECT FOR UPDATE` + transaction garantit l'isolation.

**Règles :**
- Toute vérification de disponibilité (conflits FR-5, couverts FR-4) se fait **dans la même transaction** que l'insertion.
- Les index `(booking_date, service_id, status)` et `(table_id, booking_date)` minimisent le temps de lock.
- En cas de conflit détecté, l'exception domaine `BookingConflictException` est levée → HTTP 422.

### 4.6 Stratégie d'authentification et d'autorisation

**Décision :** JWT Bearer tokens. Autorisation basée sur des `Claims` de rôle vérifiés à chaque endpoint via des policies ASP.NET Core.

**Règles :**
- 4 rôles : `Online`, `Staff`, `Manager`, `Admin`.
- Policies nommées par action : `[Authorize(Policy = "CanConfirmBooking")]` → Staff, Manager, Admin.
- La vérification de rôle se fait côté API — jamais côté Angular (le frontend adapte l'UI, pas la sécurité).
- Les données client (email, téléphone) ne sont accessibles qu'aux rôles Staff et supérieurs (filtrage en Query).

---

## 5. Patterns d'implémentation et règles de cohérence

> Ces règles garantissent que plusieurs agents IA produisent du code compatible et cohérent.

### 5.1 Conventions de nommage

#### Base de données (snake_case)
```sql
-- Tables : snake_case, pluriel
bookings, customers, dining_services, tables, table_locks, closed_days

-- Colonnes : snake_case
booking_date, arrival_time, guests_count, is_blacklisted, no_show_count

-- Clés étrangères : {table_singulier}_id
customer_id, service_id, table_id, primary_table_id

-- Index : idx_{table}_{colonnes}
idx_bookings_date_service, idx_bookings_customer, idx_table_locks_booking

-- Contraintes : chk_{table}_{description}
chk_min_capacity, chk_cancellation_reason, chk_different_tables
```

#### API REST (kebab-case pour les ressources, camelCase pour les paramètres JSON)
```
GET    /api/bookings/{id}
POST   /api/bookings
PUT    /api/bookings/{id}
PATCH  /api/bookings/{id}/status
DELETE /api/bookings/{id}
GET    /api/tables/availability?date=&serviceId=&guestsCount=&zone=
GET    /api/floor/snapshot?date=&serviceId=
```

**Réponse d'erreur unifiée :**
```json
{
  "code": "BOOKING_CONFLICT",
  "message": "La table 4 est déjà réservée sur ce créneau.",
  "details": { "tableId": "...", "conflictingBookingId": "..." }
}
```

**Codes HTTP par cas :**

| Cas | Code |
|-----|------|
| Création réussie | 201 |
| Lecture / MAJ réussie | 200 |
| Règle métier violée | 422 |
| Ressource introuvable | 404 |
| Transition de statut invalide | 409 |
| Erreur de validation DTO | 400 |
| Droits insuffisants | 403 |

#### C# (PascalCase pour tout sauf les champs privés et paramètres)
```csharp
// Classes, Records, Interfaces
public record CreateBookingCommand(...) : IRequest<BookingDto>;
public interface IBookingRepository { ... }
public class BookingCommandHandler(...) { ... }

// Champs privés : _camelCase
private readonly IBookingRepository _repository;

// Paramètres et variables locales : camelCase
public async Task Handle(CreateBookingCommand command, ...) {
    var guestsCount = command.GuestsCount;
}

// Constantes : PascalCase
public const int MaxSpecialRequestsLength = 500;
public const int OnlineBookingHorizonDays = 30;
```

#### Angular / TypeScript
```typescript
// Composants : PascalCase + suffixe
FloorPlanComponent, BookingFormComponent, TableCardComponent

// Services : PascalCase + Service
BookingService, SignalRService, TableService

// Fichiers : kebab-case + type
floor-plan.component.ts, booking.service.ts, booking.model.ts

// Signals : camelCase
floorSnapshot = signal<FloorSnapshot | null>(null);
isLoading = signal(false);

// Computed : camelCase descriptif
tablesBySalle = computed(() => ...);
totalConfirmedCovers = computed(() => ...);

// Méthodes de template : camelCase verbe
onTableClick(table: TableState): void { ... }
onServiceChange(event: Event): void { ... }
```

### 5.2 Patterns de structure

#### Organisation des composants Angular
```typescript
@Component({ ... })
export class MonComposant {
  // 1. Injections (inject())
  private readonly service = inject(MonService);

  // 2. Inputs / Outputs
  @Input() id!: string;
  @Output() confirmed = new EventEmitter<void>();

  // 3. Signals d'état
  data = signal<MonType | null>(null);
  isLoading = signal(false);

  // 4. Computed signals
  derivedValue = computed(() => this.data()?.property ?? []);

  // 5. Lifecycle hooks
  ngOnInit() { this.load(); }

  // 6. Méthodes publiques (template)
  onAction(): void { ... }

  // 7. Méthodes privées
  private load(): void { ... }
}
```

**Règle :** Préférer `inject()` à l'injection par constructeur. Pas de logique dans les templates — extraire dans des `computed()` ou des méthodes.

#### Services Angular
```typescript
@Injectable({ providedIn: 'root' })
export class BookingService {
  private readonly http = inject(HttpClient);

  getBooking(id: string): Observable<Booking> {
    return this.http.get<Booking>(`/api/bookings/${id}`);
  }

  createBooking(dto: CreateBookingDto): Observable<Booking> {
    return this.http.post<Booking>('/api/bookings', dto);
  }
}
```

**Règle :** Les services retournent des `Observable`. La conversion en signal se fait dans les composants via `toSignal()`.

### 5.3 Patterns de format

#### Formats de données Angular ↔ API
```typescript
// Dates : ISO string côté API → date-fns côté affichage
arrivalTime: string;  // "2026-06-15T19:30:00"
// Affichage : format(parseISO(booking.arrivalTime), 'HH:mm', { locale: fr })

// Enums : alignés sur le backend (string literals)
export enum BookingStatus {
  Pending   = 'Pending',
  Confirmed = 'Confirmed',
  Seated    = 'Seated',
  Completed = 'Completed',
  Cancelled = 'Cancelled',
  NoShow    = 'NoShow',
}

// IDs : toujours string (UUID côté Angular)
id: string;  // "3fa85f64-5717-4562-b3fc-2c963f66afa6"
```

### 5.4 Patterns de gestion d'erreur

#### Backend : pipeline MediatR + middleware global
```csharp
// 1. Validation FluentValidation en pipeline → HTTP 400
// 2. Exception domaine → HTTP approprié via ExceptionMiddleware global

public class ExceptionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        try { await next(ctx); }
        catch (BookingConflictException ex)
            { await WriteError(ctx, 422, "TABLE_CONFLICT", ex.Message); }
        catch (InvalidStatusTransitionException ex)
            { await WriteError(ctx, 409, "INVALID_TRANSITION", ex.Message); }
        catch (CustomerBlacklistedException ex)
            { await WriteError(ctx, 422, "CUSTOMER_BLACKLISTED", ex.Message); }
        // ...
    }
}
```

#### Frontend : intercepteur HTTP global
```typescript
// error.interceptor.ts
export const errorInterceptor: HttpInterceptorFn = (req, next) =>
  next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      // Affiche un snackbar selon le code d'erreur
      const code = err.error?.code ?? 'UNKNOWN_ERROR';
      snackbar.open(MESSAGES[code] ?? err.error?.message);
      return throwError(() => err);
    })
  );
```

### 5.5 Patterns de tests (C#)

```csharp
// Nommage : Should_{résultat}_When_{condition}
[Fact]
public void Should_ThrowException_When_GuestsCountExceedsCapacity()
{
    // Arrange
    var table = Table.Create(number: 1, capacity: 4, minCapacity: 2, zone: TableZone.Salle);

    // Act
    var act = () => Booking.Create(customer, table, service, date, time, guestsCount: 5, ...);

    // Assert
    act.Should().Throw<GuestsCountExceedsCapacityException>();
}

// Theory pour les cas limites numériques
[Theory]
[InlineData(1)]    // Sous MinCapacity (2)
[InlineData(5)]    // Dessus Capacity (4)
public void Should_Reject_When_GuestsCountOutOfRange(int count)
{
    // ...
}
```

**Règles absolues :**
- Structure AAA séparée par des lignes vides.
- Pas de logique dans les tests (pas de boucles, pas de conditions).
- Un test = une assertion principale.
- `[Theory]` + `[InlineData]` pour les seuils et bornes.
- Les tests sont committés **avec** le code qu'ils couvrent.

---

## 6. Structure du projet

### Backend — Solution .NET

```
Reservation.sln
├── src/
│   ├── Reservation.Domain/
│   │   ├── Entities/
│   │   │   ├── Booking.cs              # Agrégat principal — cycle de vie complet
│   │   │   ├── Table.cs                # Capacité, zone, combinabilité
│   │   │   ├── Customer.cs             # VipLevel, NoShowCount, IsBlacklisted
│   │   │   ├── DiningService.cs        # Plages horaires, MaxCovers
│   │   │   ├── TableLock.cs            # Fusion de tables
│   │   │   └── ClosedDay.cs            # Jours de fermeture
│   │   ├── ValueObjects/
│   │   │   ├── TimeSlot.cs             # [ArrivalTime, ArrivalTime + Duration[
│   │   │   └── GuestCount.cs           # Invariants MinCapacity / Capacity
│   │   ├── Enums/
│   │   │   ├── BookingStatus.cs        # Pending | Confirmed | Seated | Completed | Cancelled | NoShow | Rejected
│   │   │   ├── TableZone.cs            # Salle | Terrasse | Bar | SalonPrive
│   │   │   ├── BookingSource.cs        # Online | Phone | WalkIn | Staff
│   │   │   └── VipLevel.cs             # None | Regular | VIP | VVIP
│   │   ├── Exceptions/
│   │   │   ├── BookingConflictException.cs
│   │   │   ├── ServiceFullyBookedException.cs
│   │   │   ├── InvalidStatusTransitionException.cs
│   │   │   ├── CustomerBlacklistedException.cs
│   │   │   ├── HorizonExceededException.cs
│   │   │   └── ...                     # Une exception par règle métier
│   │   ├── Events/
│   │   │   ├── BookingConfirmedEvent.cs
│   │   │   ├── BookingCancelledByRestaurantEvent.cs
│   │   │   ├── CustomerNoShowRegisteredEvent.cs
│   │   │   └── ClosedDayDeclaredEvent.cs
│   │   └── Interfaces/
│   │       └── IClock.cs               # Abstraction de l'horloge pour la testabilité
│   │
│   ├── Reservation.Application/
│   │   ├── Bookings/
│   │   │   ├── Commands/
│   │   │   │   ├── CreateBookingCommand.cs + Handler
│   │   │   │   ├── CancelBookingCommand.cs + Handler
│   │   │   │   ├── ModifyBookingCommand.cs + Handler
│   │   │   │   └── ChangeBookingStatusCommand.cs + Handler
│   │   │   └── Queries/
│   │   │       ├── GetBookingByIdQuery.cs + Handler
│   │   │       └── GetBookingsByCustomerQuery.cs + Handler
│   │   ├── Tables/
│   │   │   └── Queries/
│   │   │       ├── GetAvailableTablesQuery.cs + Handler   # FR-19
│   │   │       └── GetFloorSnapshotQuery.cs + Handler     # FR-15
│   │   ├── Customers/
│   │   │   ├── Commands/
│   │   │   │   ├── CreateCustomerCommand.cs + Handler
│   │   │   │   └── LiftBlacklistCommand.cs + Handler      # FR-28
│   │   │   └── Queries/
│   │   │       └── SearchCustomerQuery.cs + Handler       # FR-27
│   │   ├── ClosedDays/
│   │   │   └── Commands/
│   │   │       └── DeclareClosedDayCommand.cs + Handler   # FR-25, FR-26
│   │   ├── DTOs/
│   │   │   ├── BookingDto.cs
│   │   │   ├── FloorSnapshotDto.cs
│   │   │   ├── TableStateDto.cs
│   │   │   ├── AvailableTableDto.cs
│   │   │   └── CustomerDto.cs
│   │   ├── Interfaces/
│   │   │   ├── IBookingRepository.cs
│   │   │   ├── ITableRepository.cs
│   │   │   ├── ICustomerRepository.cs
│   │   │   ├── IDiningServiceRepository.cs
│   │   │   └── INotificationService.cs
│   │   └── Behaviors/
│   │       ├── ValidationBehavior.cs   # Pipeline FluentValidation
│   │       └── LoggingBehavior.cs      # Trace MediatR
│   │
│   ├── Reservation.Infrastructure/
│   │   ├── Persistence/
│   │   │   ├── ReservationDbContext.cs
│   │   │   ├── Configurations/         # EF Core Fluent API par entité
│   │   │   │   ├── BookingConfiguration.cs
│   │   │   │   ├── TableConfiguration.cs
│   │   │   │   └── ...
│   │   │   ├── Repositories/
│   │   │   │   ├── BookingRepository.cs
│   │   │   │   ├── TableRepository.cs
│   │   │   │   ├── CustomerRepository.cs
│   │   │   │   └── DiningServiceRepository.cs
│   │   │   ├── Interceptors/
│   │   │   │   └── UpdatedAtInterceptor.cs   # updated_at automatique
│   │   │   └── Migrations/
│   │   ├── Notifications/
│   │   │   ├── NotificationService.cs        # Implémente INotificationService
│   │   │   ├── EmailSender.cs
│   │   │   └── SmsSender.cs
│   │   ├── Clock/
│   │   │   └── SystemClock.cs               # Implémente IClock
│   │   └── SignalR/
│   │       └── FloorNotificationService.cs  # Pousse les events SignalR
│   │
│   └── Reservation.Api/
│       ├── Controllers/
│       │   ├── BookingsController.cs
│       │   ├── TablesController.cs
│       │   ├── CustomersController.cs
│       │   ├── ServicesController.cs
│       │   ├── FloorController.cs
│       │   └── ClosedDaysController.cs
│       ├── Hubs/
│       │   └── FloorHub.cs                  # SignalR Hub /hubs/floor
│       ├── Middleware/
│       │   └── ExceptionMiddleware.cs       # Mapping exception → HTTP
│       ├── Program.cs
│       └── appsettings.json / appsettings.Development.json
│
└── tests/
    ├── Reservation.Domain.Tests/
    │   ├── Entities/
    │   │   ├── BookingTests.cs          # Tests des règles métier RB-001..RB-014
    │   │   ├── CustomerTests.cs         # RB-007, RB-008, RB-009
    │   │   └── TableTests.cs
    │   └── ValueObjects/
    │       └── TimeSlotTests.cs
    ├── Reservation.Application.Tests/
    │   └── Bookings/
    │       ├── CreateBookingCommandHandlerTests.cs
    │       └── ChangeBookingStatusCommandHandlerTests.cs
    └── Reservation.Integration.Tests/
        ├── BookingsApiTests.cs          # Tests d'intégration avec TestContainers
        └── FloorSnapshotTests.cs
```

### Frontend — Angular 20

```
frontend/
├── src/
│   ├── app/
│   │   ├── core/
│   │   │   ├── services/
│   │   │   │   ├── booking.service.ts
│   │   │   │   ├── table.service.ts
│   │   │   │   ├── customer.service.ts
│   │   │   │   ├── dining-service.service.ts
│   │   │   │   └── signalr.service.ts
│   │   │   ├── models/
│   │   │   │   ├── booking.model.ts       # interface Booking + enum BookingStatus
│   │   │   │   ├── table.model.ts         # interface Table + enum TableZone
│   │   │   │   ├── floor-snapshot.model.ts
│   │   │   │   ├── customer.model.ts
│   │   │   │   └── dining-service.model.ts
│   │   │   └── interceptors/
│   │   │       ├── auth.interceptor.ts    # JWT Bearer
│   │   │       └── error.interceptor.ts   # Gestion globale des erreurs HTTP
│   │   ├── features/
│   │   │   ├── floor-plan/
│   │   │   │   ├── floor-plan.component.ts   # Composant principal — réalise UJ-2
│   │   │   │   ├── table-card.component.ts   # Carte table avec codes couleur FR-16
│   │   │   │   └── service-selector.component.ts
│   │   │   ├── booking/
│   │   │   │   ├── booking-form.component.ts    # Formulaire — réalise UJ-1
│   │   │   │   ├── booking-detail.component.ts
│   │   │   │   └── booking-list.component.ts
│   │   │   ├── availability/
│   │   │   │   └── availability-search.component.ts  # FR-19
│   │   │   ├── customer/
│   │   │   │   ├── customer-search.component.ts
│   │   │   │   └── customer-detail.component.ts
│   │   │   └── admin/
│   │   │       ├── table-management.component.ts  # FR-29
│   │   │       ├── service-management.component.ts # FR-30
│   │   │       └── closed-days.component.ts       # FR-25
│   │   └── shared/
│   │       ├── components/
│   │       │   ├── status-badge.component.ts   # FR-16
│   │       │   ├── confirm-dialog.component.ts
│   │       │   └── customer-search.component.ts
│   │       └── pipes/
│   │           ├── booking-status.pipe.ts
│   │           └── zone.pipe.ts
│   └── environments/
│       ├── environment.ts
│       └── environment.prod.ts
├── angular.json
├── tailwind.config.ts
└── tsconfig.json
```

---

## 7. Flux de données et intégration

### Flux de création de réservation

```
Client Angular
  │ POST /api/bookings (BookingDto)
  ▼
BookingsController.Create()
  │ mediator.Send(CreateBookingCommand)
  ▼
ValidationBehavior (FluentValidation) ─── Échec → HTTP 400
  ▼
CreateBookingCommandHandler
  │ 1. Charge Customer, Table, DiningService depuis les repositories
  │ 2. Booking.Create() → lève exceptions domaine si violation RB
  │    ├── BookingConflictException → HTTP 422 BOOKING_CONFLICT
  │    ├── ServiceFullyBookedException → HTTP 422 SERVICE_FULLY_BOOKED
  │    ├── CustomerBlacklistedException → HTTP 422 CUSTOMER_BLACKLISTED
  │    └── HorizonExceededException → HTTP 422 HORIZON_EXCEEDED
  │ 3. bookingRepo.AddAsync(booking)
  │ 4. Dispatch Domain Events
  ▼
NotificationService (asynchrone, découplé)
  └── Email + SMS de confirmation
FloorNotificationService (SignalR)
  └── Pousse ServiceCapacityChanged à tous les clients connectés
```

### Flux temps réel SignalR

```
Mutation d'état (ex: ChangeBookingStatusCommand → Seated)
  │
  ▼
FloorNotificationService.NotifyTableStatusChanged(tableId, Seated, bookingId)
  │
  ▼
FloorHub → Groupe "floor-2026-06-15-{serviceId}"
  │
  ▼
Angular SignalRService.on('TableStatusChanged', handler)
  │
  ▼
floorSnapshot.update(s => ({                    // Signal mutation
  ...s,
  tables: s.tables.map(t =>
    t.id === event.tableId
      ? { ...t, status: event.status }
      : t
  )
}))
  │
  ▼
Affichage mis à jour automatiquement (< 2s P95)  // via Computed signals
```

---

## 8. Validation architecturale

### Couverture des exigences fonctionnelles

| FR | Composant responsable | Couche |
|----|----------------------|--------|
| FR-1 à FR-14 (règles RB) | `Booking.Create()`, `Booking.TransitionTo()` | Domain |
| FR-15 (Snapshot) | `GetFloorSnapshotQuery` + `FloorController` | Application + API |
| FR-16, FR-17, FR-18 (Vue salle) | `FloorPlanComponent` + `SignalRService` | Frontend + Infrastructure |
| FR-19 (Disponibilité) | `GetAvailableTablesQuery` + `TablesController` | Application + API |
| FR-20, FR-21 (Fusion) | `Table.Combine()` + `TableLock` | Domain |
| FR-22, FR-23 (Flags) | `Booking.ParseSpecialRequests()` | Domain |
| FR-24 (Notifications) | `NotificationService` + Domain Events | Infrastructure |
| FR-25, FR-26 (Fermeture) | `ClosedDay` + `DeclareClosedDayCommand` | Domain + Application |
| FR-27, FR-28 (Clients) | `CustomerRepository` + `LiftBlacklistCommand` | Infrastructure + Application |
| FR-29, FR-30 (Admin) | `TableManagementComponent` + CRUD Handlers | Frontend + Application |

### Contraintes architecturales vérifiées

- ✅ `Reservation.Domain` : zéro référence à EF Core, ASP.NET Core ou Infrastructure.
- ✅ `IClock` injecté partout où `DateTime.UtcNow` serait utilisé → testabilité garantie.
- ✅ Handlers MediatR sans logique métier — ils orchestrent uniquement.
- ✅ Notifications asynchrones découplées via Domain Events — défaillance non bloquante.
- ✅ Transactions englobant les vérifications de disponibilité → pas de race condition.
- ✅ Autorisation vérifiée côté API (policies) — jamais côté Angular seulement.
- ✅ Angular Signals uniquement (pas de NgRx) — cohérence de la réactivité.
- ✅ Structure AAA dans les tests — pas de logique dans les méthodes de test.

### Points de risque et mitigations

| Risque | Mitigation |
|--------|------------|
| Concurrence sur la création de réservation | Transaction SQLite (isolation sérialisée par défaut) + index couvrant |
| Scalabilité de SignalR | Groups par (date, serviceId) pour limiter le broadcast |
| Dérive des notifications asynchrones | Retry policy + dead letter queue (à configurer en Infrastructure) |
| Test de la logique de blacklist automatique | `IClock` mockable + tests unitaires Domain dédiés |
| Performance du Snapshot avec 50+ tables | Index sur `(booking_date, service_id, status)` + requête EF optimisée avec `AsNoTracking()` |

---

## 9. Guide de démarrage rapide

### Prérequis

```
.NET 10 SDK
Node.js 22 LTS
Angular CLI 20 (npm install -g @angular/cli)
```

### Lancer l'environnement de développement

```bash
# 1. Migrations EF Core
cd src/Reservation.Api
dotnet ef database update

# 3. Backend
dotnet run

# 4. Frontend (autre terminal)
cd frontend
ng serve
```

### Variables d'environnement backend (`appsettings.Development.json`)

```json
{
  "ConnectionStrings": {
    "Default": "Data Source=reservation.db"
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:4200"]
  },
  "Jwt": {
    "SecretKey": "...",
    "Issuer": "reservation-api",
    "Audience": "reservation-app"
  }
}
```

### Ordre d'implémentation recommandé

1. **Infrastructure de base :** Solution .NET, projets, packages NuGet, Docker Compose, migration initiale.
2. **Domain :** Entités, Value Objects, Enums, Exceptions — testés en isolation.
3. **Application :** Commands/Queries pour les bookings (FR-1 à FR-14), Handlers, DTOs.
4. **API :** Controllers, middleware d'exception, authentification JWT.
5. **Frontend :** Services Angular, modèles TypeScript, composants par feature.
6. **Temps réel :** SignalR Hub + `FloorNotificationService` + `SignalRService` Angular.
7. **Notifications :** Domain Events + `NotificationService` Infrastructure.
8. **Admin :** CRUD tables/services + `ClosedDays`.
