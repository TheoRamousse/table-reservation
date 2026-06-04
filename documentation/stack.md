# Stack Technique — Système de Réservation Restaurant

## Vue d'ensemble

```
┌─────────────────────────────────────────────────────────┐
│                     Angular 20 (SPA)                    │
│         Vue salle · Calendrier · Gestion admin          │
└────────────────────────┬────────────────────────────────┘
                         │ HTTP/REST + SignalR (WebSocket)
┌────────────────────────▼────────────────────────────────┐
│                  ASP.NET Core 10 (API)                  │
│          Controllers · Domain · Application             │
└────────────────────────┬────────────────────────────────┘
                         │ EF Core 10
┌────────────────────────▼────────────────────────────────┐
│                  PostgreSQL 16                           │
└─────────────────────────────────────────────────────────┘
```

---

## Backend — .NET 10 / ASP.NET Core 10

### Framework & runtime

| Composant          | Version  | Rôle                                          |
|--------------------|----------|-----------------------------------------------|
| .NET               | 10.0     | Runtime LTS                                   |
| ASP.NET Core       | 10.0     | API REST + SignalR                            |
| C#                 | 14       | Langage principal                             |
| Entity Framework Core | 10.0 | ORM                                           |
| PostgreSQL (Npgsql)| 9.x      | Driver EF Core pour Postgres                  |

### Architecture du projet

```
Reservation.sln
├── src/
│   ├── Reservation.Domain/          # Entités, Value Objects, règles métier pures
│   ├── Reservation.Application/     # Use cases, DTOs, interfaces de services
│   ├── Reservation.Infrastructure/  # EF Core, repositories, notifications
│   └── Reservation.Api/             # Controllers, SignalR Hubs, Program.cs
└── tests/
    ├── Reservation.Domain.Tests/    # Tests unitaires (xUnit)
    ├── Reservation.Application.Tests/
    └── Reservation.Integration.Tests/ # Tests d'intégration (TestContainers)
```

Architecture **Clean Architecture** (Domain → Application → Infrastructure/Api).  
La couche Domain n'a **aucune dépendance** externe (pas d'EF, pas d'ASP.NET).

### Bibliothèques backend

| Bibliothèque              | Usage                                           |
|---------------------------|-------------------------------------------------|
| xUnit 2.x                 | Framework de tests                              |
| FluentAssertions          | Assertions expressives dans les tests           |
| Stryker.NET               | Mutation testing                                |
| TestContainers            | PostgreSQL éphémère pour les tests d'intégration|
| FluentValidation          | Validation des DTOs en entrée d'API             |
| MediatR                   | Médiateur CQRS (Commands / Queries)             |
| Serilog                   | Logging structuré (JSON)                        |
| Mapster                   | Mapping Domain ↔ DTO                            |
| Microsoft.AspNetCore.SignalR | Temps réel (mise à jour état des tables)     |

### API REST — principaux endpoints

```
POST   /api/bookings                  Créer une réservation
GET    /api/bookings/{id}             Détail d'une réservation
PUT    /api/bookings/{id}             Modifier une réservation
DELETE /api/bookings/{id}             Annuler une réservation
PATCH  /api/bookings/{id}/status      Changer le statut (confirm, seat, complete…)

GET    /api/tables/availability       Disponibilité des tables (date + service + couverts)
GET    /api/tables/floor-plan         État visuel de toutes les tables à un instant T

GET    /api/services                  Liste des services (déjeuner, dîner…)
GET    /api/customers/{id}            Fiche client
POST   /api/customers                 Créer un client

GET    /api/floor/snapshot?date=&serviceId=   Snapshot complet salle pour la vue visuelle
```

### SignalR Hub

```
Hub : /hubs/floor
Événements émis par le serveur :
  - TableStatusChanged   { tableId, status, bookingId }
  - BookingUpdated       { bookingId, newStatus }
  - ServiceCapacityChanged { serviceId, date, remainingCovers }
```

---

## Frontend — Angular 20

### Framework & outillage

| Composant           | Version   | Rôle                                         |
|---------------------|-----------|----------------------------------------------|
| Angular             | 20.x      | Framework SPA                                |
| TypeScript          | 5.8       | Langage                                      |
| Angular CLI         | 20.x      | Build, scaffold, dev server                  |
| Vite (via esbuild)  | intégré   | Build tool (Angular 20 default)              |
| Angular Signals     | natif     | Réactivité fine-grained (pas de NgRx)        |
| Angular HttpClient  | natif     | Appels API REST                              |
| @microsoft/signalr  | 8.x       | Client SignalR (temps réel)                  |
| Angular CDK         | 20.x      | Drag & drop, overlay (positionnement tables) |

### Bibliothèques UI

| Bibliothèque         | Usage                                             |
|----------------------|---------------------------------------------------|
| Angular Material 20  | Composants UI (date picker, dialogs, snackbar…)   |
| date-fns             | Manipulation des dates                            |
| Tailwind CSS 4.x     | Utilitaires CSS (layout, couleurs statuts)        |

### Structure du projet frontend

```
src/
├── app/
│   ├── core/
│   │   ├── services/          # BookingService, TableService, SignalRService
│   │   ├── models/            # Interfaces TypeScript (Booking, Table, Service…)
│   │   └── interceptors/      # Auth, error handling
│   ├── features/
│   │   ├── floor-plan/        # Vue visuelle de la salle (composant principal)
│   │   │   ├── floor-plan.component.ts
│   │   │   ├── table-card.component.ts
│   │   │   └── service-selector.component.ts
│   │   ├── booking/
│   │   │   ├── booking-form.component.ts
│   │   │   ├── booking-detail.component.ts
│   │   │   └── booking-list.component.ts
│   │   └── admin/
│   │       ├── table-management.component.ts
│   │       └── service-management.component.ts
│   └── shared/
│       ├── components/        # StatusBadge, ConfirmDialog, CustomerSearch
│       └── pipes/             # BookingStatusPipe, ZonePipe
└── environments/
```

---

## Vue visuelle de la salle (Floor Plan)

C'est le composant central de l'application. Il permet au personnel de visualiser en temps réel l'état de toutes les tables pour un service donné.

### Fonctionnement

```
┌──────────────────────────────────────────────────────────────────┐
│  [Date picker]   [Service : Déjeuner ▼]   [🔄 Temps réel : ON]  │
├──────────────────────────────────────────────────────────────────┤
│                                                                  │
│   ┌──────┐  ┌──────┐  ┌──────┐  ┌──────────────────┐           │
│   │  T1  │  │  T2  │  │  T3  │  │       T4+T5       │           │
│   │  4p  │  │  2p  │  │  6p  │  │    fusionnées     │           │
│   │ LIBRE│  │ RÉSE.│  │ASSIS │  │      8p RÉSE.     │           │
│   └──────┘  └──────┘  └──────┘  └──────────────────┘           │
│                                                                  │
│   ═══════════════ Terrasse ════════════════                      │
│   ┌──────┐  ┌──────┐  ┌──────┐                                  │
│   │  T6  │  │  T7  │  │  T8  │                                  │
│   │  4p  │  │  4p  │  │  2p  │                                  │
│   │ LIBRE│  │NO-SHW│  │ LIBRE│                                  │
│   └──────┘  └──────┘  └──────┘                                  │
│                                                                  │
│  Couverts confirmés : 24/60   [+ Nouvelle réservation]          │
└──────────────────────────────────────────────────────────────────┘
```

### Codes couleur des tables

| Statut         | Couleur       | Description                               |
|----------------|---------------|-------------------------------------------|
| Libre          | Vert (#22c55e) | Aucune réservation sur ce créneau         |
| Réservée       | Bleu (#3b82f6) | Réservation Confirmed à venir             |
| En attente     | Jaune (#eab308)| Réservation Pending (non confirmée)       |
| Occupée        | Orange (#f97316)| Statut Seated — clients en cours de repas|
| No-show        | Rouge (#ef4444)| Client ne s'est pas présenté              |
| Inactive       | Gris (#9ca3af) | Table désactivée                          |

### Interactions disponibles

- **Clic sur une table libre** → ouvre le formulaire de réservation pré-rempli avec cette table.
- **Clic sur une table réservée** → ouvre le détail de la réservation (client, couverts, demandes spéciales, actions).
- **Clic sur une table occupée (Seated)** → permet de marquer `Completed` (libération de table).
- **Hover** → tooltip avec le nom du client, nombre de couverts, heure d'arrivée.
- **Sélection date + service** → recharge le snapshot via `GET /api/floor/snapshot`.
- **Temps réel** → les changements de statut sont poussés via SignalR sans reload de page.

### Flux de données (Angular Signals)

```typescript
// Signal central de l'état de la salle
floorSnapshot = signal<FloorSnapshot | null>(null);

// Computed : tables par zone
tablesBySalle  = computed(() => floorSnapshot()?.tables.filter(t => t.zone === 'Salle'));
tablesTerrasse = computed(() => floorSnapshot()?.tables.filter(t => t.zone === 'Terrasse'));

// Mise à jour temps réel via SignalR
signalRService.on('TableStatusChanged', (event) => {
  floorSnapshot.update(s => ({
    ...s,
    tables: s.tables.map(t =>
      t.id === event.tableId ? { ...t, status: event.status } : t
    )
  }));
});
```

---

## Base de données — PostgreSQL 16

### Principales tables

```sql
tables          -- Table physique du restaurant
customers       -- Clients
dining_services -- Services (déjeuner, dîner…)
bookings        -- Réservations
table_locks     -- Verrous pour tables fusionnées
closed_days     -- Jours de fermeture
```

### Conventions

- Clés primaires en `UUID` (v7, ordonnées dans le temps).
- Colonnes `created_at` et `updated_at` sur toutes les tables (type `timestamptz`).
- Soft delete via colonne `deleted_at` nullable (pas de `DELETE` physique sur les réservations).
- Index sur `(booking_date, service_id, status)` pour les requêtes de disponibilité.

---

## Environnement de développement

### Prérequis

```
.NET 10 SDK
Node.js 22 LTS
Angular CLI 20   (npm install -g @angular/cli)
Docker Desktop   (pour PostgreSQL via Docker Compose)
```

### Lancer le projet

```bash
# Base de données
docker compose up -d postgres

# Backend
cd src/Reservation.Api
dotnet run

# Frontend
cd frontend
ng serve
```

### Variables d'environnement backend (`.env` ou `appsettings.Development.json`)

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Database=reservation;Username=postgres;Password=postgres"
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:4200"]
  }
}
```

---

## Qualité & CI

| Outil           | Rôle                                                  |
|-----------------|-------------------------------------------------------|
| xUnit           | Tests unitaires et d'intégration .NET                 |
| Stryker.NET     | Mutation testing — vérifie la solidité des tests      |
| Karma / Jest    | Tests unitaires Angular                               |
| GitHub Actions  | CI : build, tests, Stryker, review automatique Claude |
| SonarQube       | Analyse statique (optionnel)                          |
