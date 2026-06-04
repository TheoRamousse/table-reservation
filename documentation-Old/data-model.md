# Modèle de données — Système de Réservation Restaurant

## Diagramme entité-relation

```
customers ──────────────< bookings >────────────── tables
                              │                       │
                         dining_services          table_locks
                              │
                          closed_days
```

## Tables SQL

### `customers`

```sql
CREATE TABLE customers (
    id              TEXT          PRIMARY KEY,                          -- GUID généré par EF Core
    first_name      TEXT          NOT NULL,
    last_name       TEXT          NOT NULL,
    phone           TEXT          NOT NULL UNIQUE,
    email           TEXT          UNIQUE,
    is_blacklisted  INTEGER       NOT NULL DEFAULT 0,                   -- 0 = false, 1 = true
    no_show_count   INTEGER       NOT NULL DEFAULT 0,
    vip_level       TEXT          NOT NULL DEFAULT 'None',              -- None | Regular | VIP | VVIP
    created_at      TEXT          NOT NULL DEFAULT (datetime('now')),   -- ISO 8601
    updated_at      TEXT          NOT NULL DEFAULT (datetime('now')),
    deleted_at      TEXT                                                -- soft delete
);
```

### `tables`

```sql
CREATE TABLE tables (
    id              TEXT          PRIMARY KEY,
    number          INTEGER       NOT NULL UNIQUE,
    capacity        INTEGER       NOT NULL CHECK (capacity BETWEEN 1 AND 20),
    min_capacity    INTEGER       NOT NULL CHECK (min_capacity >= 1),
    zone            TEXT          NOT NULL,                             -- Salle | Terrasse | Bar | SalonPrive
    is_active       INTEGER       NOT NULL DEFAULT 1,
    is_combinable   INTEGER       NOT NULL DEFAULT 0,
    created_at      TEXT          NOT NULL DEFAULT (datetime('now')),
    updated_at      TEXT          NOT NULL DEFAULT (datetime('now')),
    CONSTRAINT chk_min_capacity CHECK (min_capacity <= capacity)
);
```

### `dining_services`

```sql
CREATE TABLE dining_services (
    id                  TEXT    PRIMARY KEY,
    name                TEXT    NOT NULL,                               -- Déjeuner | Dîner | Brunch
    start_time          TEXT    NOT NULL,                               -- format HH:MM
    end_time            TEXT    NOT NULL,
    last_booking_time   TEXT    NOT NULL,
    duration_minutes    INTEGER NOT NULL,                               -- 120 (déjeuner) ou 150 (dîner)
    max_covers          INTEGER NOT NULL CHECK (max_covers > 0),
    is_active           INTEGER NOT NULL DEFAULT 1,
    created_at          TEXT    NOT NULL DEFAULT (datetime('now')),
    updated_at          TEXT    NOT NULL DEFAULT (datetime('now')),
    CONSTRAINT chk_end_after_start       CHECK (end_time > start_time),
    CONSTRAINT chk_last_booking_in_range CHECK (last_booking_time <= end_time)
);
```

### `bookings`

```sql
CREATE TABLE bookings (
    id                  TEXT    PRIMARY KEY,
    table_id            TEXT    REFERENCES tables(id),                  -- nullable (assignée après)
    customer_id         TEXT    NOT NULL REFERENCES customers(id),
    service_id          TEXT    NOT NULL REFERENCES dining_services(id),
    booking_date        TEXT    NOT NULL,                               -- format YYYY-MM-DD
    arrival_time        TEXT    NOT NULL,                               -- format HH:MM
    guests_count        INTEGER NOT NULL CHECK (guests_count >= 1),
    status              TEXT    NOT NULL DEFAULT 'Pending',
        -- Pending | Confirmed | Seated | Completed | Cancelled | NoShow | Rejected
    source              TEXT    NOT NULL,                               -- Online | Phone | WalkIn | Staff
    special_requests    TEXT,
    has_allergy_alert   INTEGER NOT NULL DEFAULT 0,
    is_celebration      INTEGER NOT NULL DEFAULT 0,
    needs_high_chair    INTEGER NOT NULL DEFAULT 0,
    late_cancel         INTEGER NOT NULL DEFAULT 0,
    cancellation_reason TEXT,
    recurrence_group_id TEXT,
    created_at          TEXT    NOT NULL DEFAULT (datetime('now')),
    updated_at          TEXT    NOT NULL DEFAULT (datetime('now')),
    deleted_at          TEXT,                                           -- soft delete
    CONSTRAINT chk_cancellation_reason CHECK (
        status != 'Cancelled' OR cancellation_reason IS NOT NULL
    )
);

-- Index principaux
CREATE INDEX idx_bookings_date_service  ON bookings (booking_date, service_id, status);
CREATE INDEX idx_bookings_customer      ON bookings (customer_id);
CREATE INDEX idx_bookings_table_date    ON bookings (table_id, booking_date) WHERE deleted_at IS NULL;
CREATE INDEX idx_bookings_recurrence    ON bookings (recurrence_group_id) WHERE recurrence_group_id IS NOT NULL;
```

### `table_locks`

Verrous pour les fusions de tables (deux tables physiques liées à une réservation unique).

```sql
CREATE TABLE table_locks (
    id                 TEXT PRIMARY KEY,
    booking_id         TEXT NOT NULL REFERENCES bookings(id) ON DELETE CASCADE,
    primary_table_id   TEXT NOT NULL REFERENCES tables(id),
    secondary_table_id TEXT NOT NULL REFERENCES tables(id),
    created_at         TEXT NOT NULL DEFAULT (datetime('now')),
    CONSTRAINT chk_different_tables CHECK (primary_table_id != secondary_table_id)
);

CREATE UNIQUE INDEX idx_table_locks_booking ON table_locks (booking_id);
```

### `closed_days`

```sql
CREATE TABLE closed_days (
    id          TEXT    PRIMARY KEY,
    closed_date TEXT    NOT NULL UNIQUE,                                -- format YYYY-MM-DD
    reason      TEXT    NOT NULL,
    created_at  TEXT    NOT NULL DEFAULT (datetime('now')),
    created_by  TEXT                                                    -- référence utilisateur staff
);
```

---

## Mapping Domaine → BDD (EF Core)

| Entité .NET       | Table SQL          | Remarques                                                        |
|-------------------|--------------------|------------------------------------------------------------------|
| `Table`           | `tables`           | `Number` unique, contrainte `MinCapacity <= Capacity`            |
| `Customer`        | `customers`        | `NoShowCount >= 3` déclenche blacklist (logique domain)          |
| `DiningService`   | `dining_services`  | `DurationMinutes` dérive du type de service                      |
| `Booking`         | `bookings`         | Flags dérivés de `SpecialRequests` au moment de la sauvegarde    |
| `TableLock`       | `table_locks`      | Créé uniquement lors d'une fusion, cascade delete                |
| `ClosedDay`       | `closed_days`      | Déclenche annulation en cascade via domain event                 |

## Conventions EF Core

- Nommage snake_case en base, PascalCase en C# → convention globale dans `DbContext`.
- Pas de lazy loading — tous les includes sont explicites.
- Les enums sont stockés en `TEXT` (lisibilité des données brutes).
- Les booléens (`bool` C#) sont mappés en `INTEGER` (0/1) — comportement par défaut du driver SQLite.
- Les dates/heures (`DateOnly`, `TimeOnly`, `DateTimeOffset`) sont mappées en `TEXT` au format ISO 8601.
- Les GUIDs (`Guid` C#) sont mappés en `TEXT` — générés par EF Core côté application (pas de DEFAULT SQL).
- `updated_at` mis à jour via un `SaveChanges` interceptor global.
- Les clés étrangères SQLite sont désactivées par défaut — activer via `PRAGMA foreign_keys = ON` au démarrage du `DbContext`.
- Migrations dans `Reservation.Infrastructure/Migrations/`.
- Tests d'intégration : connexion `Data Source=:memory:` (SQLite in-memory, pas de fichier sur disque).
