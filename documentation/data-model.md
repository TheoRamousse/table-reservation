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
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    first_name      VARCHAR(100)  NOT NULL,
    last_name       VARCHAR(100)  NOT NULL,
    phone           VARCHAR(20)   NOT NULL UNIQUE,
    email           VARCHAR(255)  UNIQUE,
    is_blacklisted  BOOLEAN       NOT NULL DEFAULT FALSE,
    no_show_count   INT           NOT NULL DEFAULT 0,
    vip_level       VARCHAR(20)   NOT NULL DEFAULT 'None',  -- None | Regular | VIP | VVIP
    created_at      TIMESTAMPTZ   NOT NULL DEFAULT now(),
    updated_at      TIMESTAMPTZ   NOT NULL DEFAULT now(),
    deleted_at      TIMESTAMPTZ   -- soft delete
);
```

### `tables`

```sql
CREATE TABLE tables (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    number          INT           NOT NULL UNIQUE,
    capacity        INT           NOT NULL CHECK (capacity BETWEEN 1 AND 20),
    min_capacity    INT           NOT NULL CHECK (min_capacity >= 1),
    zone            VARCHAR(20)   NOT NULL,  -- Salle | Terrasse | Bar | SalonPrive
    is_active       BOOLEAN       NOT NULL DEFAULT TRUE,
    is_combinable   BOOLEAN       NOT NULL DEFAULT FALSE,
    created_at      TIMESTAMPTZ   NOT NULL DEFAULT now(),
    updated_at      TIMESTAMPTZ   NOT NULL DEFAULT now(),
    CONSTRAINT chk_min_capacity CHECK (min_capacity <= capacity)
);
```

### `dining_services`

```sql
CREATE TABLE dining_services (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name                VARCHAR(100)  NOT NULL,          -- Déjeuner | Dîner | Brunch
    start_time          TIME          NOT NULL,
    end_time            TIME          NOT NULL,
    last_booking_time   TIME          NOT NULL,
    duration_minutes    INT           NOT NULL,          -- durée implicite d'occupation (120 ou 150)
    max_covers          INT           NOT NULL CHECK (max_covers > 0),
    is_active           BOOLEAN       NOT NULL DEFAULT TRUE,
    created_at          TIMESTAMPTZ   NOT NULL DEFAULT now(),
    updated_at          TIMESTAMPTZ   NOT NULL DEFAULT now(),
    CONSTRAINT chk_end_after_start       CHECK (end_time > start_time),
    CONSTRAINT chk_last_booking_in_range CHECK (last_booking_time <= end_time)
);
```

### `bookings`

```sql
CREATE TABLE bookings (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    table_id            UUID          REFERENCES tables(id),          -- nullable (assignée après)
    customer_id         UUID          NOT NULL REFERENCES customers(id),
    service_id          UUID          NOT NULL REFERENCES dining_services(id),
    booking_date        DATE          NOT NULL,
    arrival_time        TIME          NOT NULL,
    guests_count        INT           NOT NULL CHECK (guests_count >= 1),
    status              VARCHAR(20)   NOT NULL DEFAULT 'Pending',
        -- Pending | Confirmed | Seated | Completed | Cancelled | NoShow | Rejected
    source              VARCHAR(20)   NOT NULL,   -- Online | Phone | WalkIn | Staff
    special_requests    VARCHAR(500),
    has_allergy_alert   BOOLEAN       NOT NULL DEFAULT FALSE,
    is_celebration      BOOLEAN       NOT NULL DEFAULT FALSE,
    needs_high_chair    BOOLEAN       NOT NULL DEFAULT FALSE,
    late_cancel         BOOLEAN       NOT NULL DEFAULT FALSE,
    cancellation_reason VARCHAR(500),
    recurrence_group_id UUID,                                          -- nullable, lien groupe récurrent
    created_at          TIMESTAMPTZ   NOT NULL DEFAULT now(),
    updated_at          TIMESTAMPTZ   NOT NULL DEFAULT now(),
    deleted_at          TIMESTAMPTZ,                                   -- soft delete
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
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    booking_id      UUID NOT NULL REFERENCES bookings(id) ON DELETE CASCADE,
    primary_table_id   UUID NOT NULL REFERENCES tables(id),
    secondary_table_id UUID NOT NULL REFERENCES tables(id),
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    CONSTRAINT chk_different_tables CHECK (primary_table_id != secondary_table_id)
);

CREATE UNIQUE INDEX idx_table_locks_booking ON table_locks (booking_id);
```

### `closed_days`

```sql
CREATE TABLE closed_days (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    closed_date DATE          NOT NULL UNIQUE,
    reason      VARCHAR(255)  NOT NULL,
    created_at  TIMESTAMPTZ   NOT NULL DEFAULT now(),
    created_by  UUID          -- référence utilisateur staff
);
```

---

## Mapping Domaine → BDD (EF Core)

| Entité .NET       | Table SQL          | Remarques                                              |
|-------------------|--------------------|--------------------------------------------------------|
| `Table`           | `tables`           | `Number` unique, contrainte `MinCapacity <= Capacity`  |
| `Customer`        | `customers`        | `NoShowCount >= 3` déclenche blacklist (logique domain)|
| `DiningService`   | `dining_services`  | `DurationMinutes` dérive du type de service            |
| `Booking`         | `bookings`         | Flags dérivés de `SpecialRequests` au moment de la sauvegarde |
| `TableLock`       | `table_locks`      | Créé uniquement lors d'une fusion, cascade delete      |
| `ClosedDay`       | `closed_days`      | Déclenche annulation en cascade via domain event       |

## Conventions EF Core

- Nommage snake_case en base, PascalCase en C# → convention globale dans `DbContext`.
- Pas de lazy loading — tous les includes sont explicites.
- Les enums sont stockés en `VARCHAR` (lisibilité des données brutes).
- `updated_at` mis à jour via un `SaveChanges` interceptor global.
- Migrations dans `Reservation.Infrastructure/Migrations/`.
