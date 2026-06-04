# Conventions C# / .NET

## Nommage

| Élément                   | Convention           | Exemple                                      |
|---------------------------|----------------------|----------------------------------------------|
| Classes, Records, Structs | PascalCase           | `Booking`, `TableLock`, `TimeSlot`           |
| Interfaces                | `I` + PascalCase     | `IBookingRepository`, `IClock`               |
| Méthodes                  | PascalCase           | `TransitionTo`, `CalculateCost`              |
| Propriétés                | PascalCase           | `GuestsCount`, `ArrivalTime`                 |
| Champs privés             | `_` + camelCase      | `_repository`, `_clock`                      |
| Paramètres / variables    | camelCase            | `bookingId`, `guestsCount`                   |
| Constantes                | PascalCase           | `MaxAdvanceDays`, `LateCancelThresholdHours` |
| Enums (type + valeurs)    | PascalCase           | `BookingStatus.Confirmed`                    |
| Méthodes async            | suffixe `Async`      | `CreateBookingAsync`, `GetByIdAsync`         |
| Events de domaine         | suffixe `Event`      | `BookingConfirmedEvent`, `NoShowRegisteredEvent` |
| Commandes MediatR         | suffixe `Command`    | `CreateBookingCommand`                       |
| Queries MediatR           | suffixe `Query`      | `GetFloorSnapshotQuery`                      |
| Handlers                  | suffixe `Handler`    | `CreateBookingCommandHandler`                |
| DTOs en sortie            | suffixe `Dto`        | `BookingDto`, `FloorSnapshotDto`             |
| Exceptions domaine        | suffixe `Exception`  | `BookingConflictException`                   |

## Organisation des fichiers

```
Reservation.Domain/
├── Entities/          Agrégats et entités (Booking, Table, Customer, DiningService)
├── ValueObjects/      TimeSlot, GuestCount, PhoneNumber
├── Enums/             BookingStatus, TableZone, BookingSource, VipLevel
├── Exceptions/        Exceptions métier spécifiques
└── Events/            Domain events (record immuables)

Reservation.Application/
├── Bookings/
│   ├── Commands/      Un fichier par command + handler
│   └── Queries/       Un fichier par query + handler
├── Tables/
├── Customers/
└── Common/            Interfaces IBookingRepository, IClock, INotificationService

Reservation.Infrastructure/
├── Persistence/       DbContext, configurations EF, repositories
│   ├── Configurations/ IEntityTypeConfiguration<T> par entité
│   └── Migrations/
└── Services/          Implémentations NotificationService, Clock

Reservation.Api/
├── Controllers/       Un controller par agrégat
├── Hubs/              SignalR FloorHub
└── Mapping/           Profils Mapster
```

## Couche Domain — règles strictes

- Aucun `using` vers Infrastructure, Application, ASP.NET ou EF Core.
- La logique métier est **sur l'entité** ou dans un **Value Object** — jamais dans un service statique.
- Les entités exposent des méthodes explicites, pas des setters publics :
  ```csharp
  // ✅
  booking.TransitionTo(BookingStatus.Confirmed, actor);
  // ❌
  booking.Status = BookingStatus.Confirmed;
  ```
- Jamais `DateTime.UtcNow` directement — toujours `IClock.UtcNow` (testabilité).
- Les exceptions domaine sont spécifiques et documentent la règle violée :
  ```csharp
  throw new BookingConflictException(tableId, slot);   // ✅
  throw new Exception("conflict");                      // ❌
  throw new ArgumentException("invalid");              // ❌ (hors domain)
  ```

## Couche Application — règles

- Les handlers **orchestrent** uniquement : récupérer, appeler le domaine, persister, émettre.
- Zéro logique métier dans un handler.
- Retourner des DTOs immuables (`record`) depuis les queries — jamais des entités.
- Toujours passer `CancellationToken` en paramètre des méthodes async.
  ```csharp
  public async Task<BookingDto> Handle(GetBookingQuery query, CancellationToken ct)
  ```

## Async / await

- Toutes les méthodes I/O sont `async Task<T>` — jamais `.Result` ni `.Wait()`.
- Ne pas utiliser `async void` sauf pour les event handlers UI (inexistants ici).
- Nommer les méthodes async avec le suffixe `Async`.
- Passer `CancellationToken ct` en dernier paramètre de toute méthode async publique.

## Nullable reference types

- `<Nullable>enable</Nullable>` activé sur tous les projets.
- Les propriétés optionnelles sont explicitement `string?`, `Guid?`, etc.
- Utiliser `ArgumentNullException.ThrowIfNull()` en entrée de constructeur d'entité.

## Gestion des exceptions

```csharp
// Exceptions domaine → 422 en API
public class BookingConflictException : DomainException { ... }

// Ne jamais avaler une exception silencieusement
catch (Exception ex) { _logger.LogError(ex, "..."); throw; }   // ✅
catch (Exception) { }                                           // ❌
```

## EF Core

- Pas de lazy loading — tous les includes sont explicites dans les repositories.
- Les configurations d'entités sont dans des classes `IEntityTypeConfiguration<T>` séparées.
- Jamais de `DbContext` injecté directement dans Application — passer par les interfaces `IRepository`.
- Toute modification de schéma passe par une migration nommée en PascalCase :
  ```bash
  dotnet ef migrations add AjoutIndexBookingsDateService
  ```

## Tests unitaires

```csharp
// Nommage
public void Should_[résultat]_When_[condition]()
// Exemples
public void Should_ThrowException_When_GuestsCountExceedsCapacity()
public void Should_SetLateCancel_When_CancelledLessThan24HoursBefore()

// Structure AAA obligatoire avec lignes vides
[Fact]
public void Should_Block_When_CustomerIsBlacklisted()
{
    // Arrange
    var customer = Customer.Create("Jean", "Dupont", "0600000000");
    customer.Blacklist();

    // Act
    Action act = () => customer.EnsureCanBook();

    // Assert
    act.Should().Throw<CustomerBlacklistedException>();
}

// Cas limites numériques → Theory + InlineData
[Theory]
[InlineData(0)]
[InlineData(-1)]
public void Should_Reject_When_GuestsCountIsInvalid(int count) { ... }
```

- Un test = une assertion principale.
- Pas de logique dans les tests (pas de boucles, pas de conditions, pas de try/catch).
- Catégoriser avec `[Trait("Category", "Unit")]` pour le filtre CI.
- Mocker `IClock` pour contrôler le temps dans les tests.

## Formatage

- Indentation : 4 espaces (pas de tabs).
- Longueur de ligne max : 120 caractères.
- Une classe par fichier, nom du fichier = nom de la classe.
- Les accolades ouvrantes sur la même ligne pour les lambdas et expressions, nouvelle ligne pour les méthodes/classes.
- `var` si le type est évident à droite, type explicite sinon.
