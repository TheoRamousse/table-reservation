# Contrat d'interface API — Système de Réservation de Tables

> Document de référence partagé entre le backend (.NET 10) et le frontend (Angular 20).  
> Toute modification de shape, de code d'erreur ou d'événement SignalR doit passer par une PR sur ce fichier.

---

## Conventions globales

### Authentification

Toutes les routes protégées requièrent le header :
```
Authorization: Bearer <jwt_token>
```

Le token JWT contient le claim de rôle : `Online` | `Staff` | `Manager` | `Admin`.

### Formats de données

| Type       | Format                              | Exemple                          |
|------------|-------------------------------------|----------------------------------|
| Date       | `YYYY-MM-DD` (string)               | `"2026-06-15"`                   |
| Heure      | `HH:mm` (string)                    | `"19:30"`                        |
| DateTime   | ISO 8601 UTC (string)               | `"2026-06-15T17:30:00Z"`         |
| UUID       | string lowercase                    | `"3fa85f64-5717-4562-b3fc-2c963f66afa6"` |
| Décimal    | number (JSON)                       | `4.5`                            |
| Booléen    | boolean (JSON)                      | `true`                           |

### Structure d'erreur unifiée

Toutes les erreurs retournent :
```json
{
  "code": "CODE_ERREUR",
  "message": "Message lisible par un humain.",
  "details": { }
}
```

`details` est optionnel et enrichit le contexte (champs concernés, valeurs impliquées).

### Codes HTTP

| Cas                           | Code |
|-------------------------------|------|
| Création réussie              | 201  |
| Lecture / mise à jour réussie | 200  |
| Suppression / action sans corps | 204 |
| Erreur de validation DTO      | 400  |
| Non authentifié               | 401  |
| Droits insuffisants           | 403  |
| Ressource introuvable         | 404  |
| Transition de statut invalide | 409  |
| Règle métier violée           | 422  |

---

## Catalogue des codes d'erreur

| Code                       | HTTP | Règle  | Description                                              | `details`                                   |
|----------------------------|------|--------|----------------------------------------------------------|---------------------------------------------|
| `TIME_OUTSIDE_SERVICE`     | 422  | RB-001 | ArrivalTime hors plage du service                        | `{ lastBookingTime }`                       |
| `GUESTS_BELOW_MIN`         | 422  | RB-002 | GuestsCount < Table.MinCapacity                          | `{ minCapacity, provided }`                 |
| `GUESTS_EXCEED_CAPACITY`   | 422  | RB-002 | GuestsCount > Table.Capacity                             | `{ capacity, provided }`                    |
| `SERVICE_FULLY_BOOKED`     | 422  | RB-003 | MaxCovers du service atteint                             | `{ maxCovers, currentTotal }`               |
| `TABLE_CONFLICT`           | 422  | RB-004 | Créneau déjà occupé sur cette table                      | `{ tableId, conflictingBookingId }`         |
| `BOOKING_DATE_IN_PAST`     | 422  | RB-005 | BookingDate est dans le passé                            | `{ provided }`                              |
| `HORIZON_EXCEEDED`         | 422  | RB-005 | Réservation trop loin dans le futur pour ce rôle/canal   | `{ maxDays, provided }`                     |
| `WALKIN_MUST_BE_TODAY`     | 422  | RB-005 | WalkIn avec BookingDate ≠ today                          | `{ today, provided }`                       |
| `MIN_LEAD_TIME_VIOLATED`   | 422  | RB-006 | Réservation trop proche de l'ArrivalTime                 | `{ minMinutes, minutesLeft }`               |
| `CUSTOMER_BLACKLISTED`     | 422  | RB-007 | Client blacklisté                                        | `{ customerId }`                            |
| `NOSHOW_TOO_EARLY`         | 422  | RB-008 | Passage en NoShow avant ArrivalTime + 15 min             | `{ earliestNoShowAt }`                      |
| `CANNOT_CANCEL_SEATED`     | 422  | RB-009 | Annulation impossible sur statut Seated                  | `{ currentStatus }`                         |
| `LATE_CANCEL`              | 200  | RB-009 | Annulation acceptée mais tardive (flag positionné)       | *(pas d'erreur — info dans la réponse)*     |
| `INVALID_STATUS_TRANSITION`| 409  | RB-014 | Transition de statut non autorisée                       | `{ from, to }`                              |
| `TABLE_NOT_COMBINABLE`     | 422  | RB-011 | Une des tables a IsCombinable = false                    | `{ tableId }`                               |
| `SPECIAL_REQUESTS_TOO_LONG`| 400  | RB-012 | SpecialRequests > 500 caractères                         | `{ maxLength, provided }`                   |
| `RESTAURANT_CLOSED`        | 422  | RB-017 | Jour de fermeture déclaré                                | `{ closedDate, reason }`                    |
| `INSUFFICIENT_ROLE`        | 403  | RB-010 | Rôle insuffisant pour cette action                       | `{ required, actual }`                      |

---

## DTOs partagés

### `BookingDto`
```json
{
  "id": "uuid",
  "tableId": "uuid | null",
  "customerId": "uuid",
  "serviceId": "uuid",
  "bookingDate": "YYYY-MM-DD",
  "arrivalTime": "HH:mm",
  "guestsCount": 4,
  "status": "Pending | Confirmed | Seated | Completed | Cancelled | NoShow | Rejected",
  "source": "Online | Phone | WalkIn | Staff",
  "specialRequests": "string | null",
  "hasAllergyAlert": false,
  "isCelebration": false,
  "needsHighChair": false,
  "lateCancel": false,
  "cancellationReason": "string | null",
  "recurrenceGroupId": "uuid | null",
  "createdAt": "ISO8601"
}
```

### `TableDto`
```json
{
  "id": "uuid",
  "number": 5,
  "capacity": 6,
  "minCapacity": 2,
  "zone": "Salle | Terrasse | Bar | SalonPrive",
  "isActive": true,
  "isCombinable": false
}
```

### `TableStateDto` *(utilisé dans le snapshot salle)*
```json
{
  "id": "uuid",
  "number": 5,
  "capacity": 6,
  "minCapacity": 2,
  "zone": "Salle | Terrasse | Bar | SalonPrive",
  "isActive": true,
  "isCombinable": false,
  "status": "Free | Pending | Confirmed | Seated | NoShow | Inactive",
  "activeBooking": {
    "bookingId": "uuid",
    "customerId": "uuid",
    "customerName": "Jean Dupont",
    "guestsCount": 4,
    "arrivalTime": "HH:mm",
    "hasAllergyAlert": false,
    "isCelebration": true,
    "needsHighChair": false
  }
}
```
`activeBooking` est `null` si la table est libre.

### `FloorSnapshotDto`
```json
{
  "date": "YYYY-MM-DD",
  "serviceId": "uuid",
  "serviceName": "Dîner",
  "totalConfirmedCovers": 24,
  "maxCovers": 60,
  "tables": [ "TableStateDto[]" ]
}
```

### `AvailableTableDto`
```json
{
  "id": "uuid",
  "number": 3,
  "capacity": 4,
  "minCapacity": 2,
  "zone": "Terrasse",
  "isCombinable": false
}
```

### `CustomerDto`
```json
{
  "id": "uuid",
  "firstName": "Jean",
  "lastName": "Dupont",
  "phone": "0612345678",
  "email": "jean.dupont@email.com | null",
  "isBlacklisted": false,
  "noShowCount": 1,
  "vipLevel": "None | Regular | VIP | VVIP"
}
```

> **Restriction :** `phone`, `email`, `noShowCount`, `isBlacklisted`, `vipLevel` ne sont retournés que pour les rôles `Staff`, `Manager`, `Admin`. Un rôle `Online` reçoit uniquement `id`, `firstName`, `lastName`.

### `DiningServiceDto`
```json
{
  "id": "uuid",
  "name": "Dîner",
  "startTime": "HH:mm",
  "endTime": "HH:mm",
  "lastBookingTime": "HH:mm",
  "durationMinutes": 150,
  "maxCovers": 60,
  "isActive": true
}
```

### `ClosedDayDto`
```json
{
  "id": "uuid",
  "closedDate": "YYYY-MM-DD",
  "reason": "Travaux",
  "createdAt": "ISO8601"
}
```

---

## Endpoints REST

### Authentification

#### `POST /api/auth/login`
Obtenir un token JWT.

**Accès :** Public

**Corps de la requête :**
```json
{
  "email": "staff@restaurant.fr",
  "password": "..."
}
```

**Réponse 200 :**
```json
{
  "token": "eyJ...",
  "expiresAt": "ISO8601",
  "role": "Staff"
}
```

**Erreurs :** `401` — identifiants invalides.

---

### Réservations

#### `POST /api/bookings`
Créer une réservation.

**Accès :** `Online` (ses propres), `Staff`, `Manager`, `Admin`

**Corps de la requête :**
```json
{
  "tableId": "uuid | null",
  "customerId": "uuid",
  "serviceId": "uuid",
  "bookingDate": "YYYY-MM-DD",
  "arrivalTime": "HH:mm",
  "guestsCount": 4,
  "specialRequests": "Allergie aux arachides. (optionnel, max 500 car.)",
  "source": "Online | Phone | WalkIn | Staff",
  "secondaryTableId": "uuid | null"
}
```
> `tableId` peut être `null` si l'attribution de table est différée.  
> `secondaryTableId` est renseigné uniquement pour une fusion de tables (RB-011).

**Réponse 201 :** `BookingDto`

**Erreurs possibles :** `TIME_OUTSIDE_SERVICE`, `GUESTS_BELOW_MIN`, `GUESTS_EXCEED_CAPACITY`, `SERVICE_FULLY_BOOKED`, `TABLE_CONFLICT`, `BOOKING_DATE_IN_PAST`, `HORIZON_EXCEEDED`, `WALKIN_MUST_BE_TODAY`, `MIN_LEAD_TIME_VIOLATED`, `CUSTOMER_BLACKLISTED`, `TABLE_NOT_COMBINABLE`, `SPECIAL_REQUESTS_TOO_LONG`, `RESTAURANT_CLOSED`, `INSUFFICIENT_ROLE`

---

#### `GET /api/bookings/{id}`
Obtenir le détail d'une réservation.

**Accès :** `Online` (ses propres), `Staff`, `Manager`, `Admin`

**Réponse 200 :** `BookingDto`

**Erreurs :** `404`, `403`

---

#### `PUT /api/bookings/{id}`
Modifier une réservation.

**Accès :** `Online` (Pending uniquement), `Staff` (Pending uniquement), `Manager`, `Admin`

**Corps de la requête :**
```json
{
  "bookingDate": "YYYY-MM-DD",
  "arrivalTime": "HH:mm",
  "guestsCount": 4,
  "specialRequests": "string | null",
  "tableId": "uuid | null"
}
```
> Tous les champs sont optionnels — seuls les champs fournis sont modifiés.  
> La modification d'une réservation `Confirmed` par `Manager`/`Admin` la repasse à `Pending`.

**Réponse 200 :** `BookingDto`

**Erreurs possibles :** `TIME_OUTSIDE_SERVICE`, `GUESTS_BELOW_MIN`, `GUESTS_EXCEED_CAPACITY`, `TABLE_CONFLICT`, `SPECIAL_REQUESTS_TOO_LONG`, `INSUFFICIENT_ROLE`, `409` (statut non modifiable)

---

#### `DELETE /api/bookings/{id}`
Annuler une réservation.

**Accès :** `Online` (ses propres), `Staff`, `Manager`, `Admin`

**Corps de la requête :**
```json
{
  "cancellationReason": "Client indisponible. (obligatoire)"
}
```

**Réponse 200 :** `BookingDto` (avec `status: "Cancelled"` et `lateCancel` si applicable)

**Erreurs possibles :** `CANNOT_CANCEL_SEATED`, `INVALID_STATUS_TRANSITION`, `INSUFFICIENT_ROLE`, `400` (cancellationReason manquant)

---

#### `PATCH /api/bookings/{id}/status`
Changer le statut d'une réservation (hors annulation).

**Accès :** `Staff`, `Manager`, `Admin`

**Corps de la requête :**
```json
{
  "newStatus": "Confirmed | Seated | Completed | NoShow | Rejected"
}
```

**Transitions autorisées :**

| De          | Vers        | Rôle minimum |
|-------------|-------------|--------------|
| `Pending`   | `Confirmed` | `Staff`      |
| `Pending`   | `Rejected`  | `Staff`      |
| `Confirmed` | `Seated`    | `Staff`      |
| `Confirmed` | `NoShow`    | `Staff`      |
| `Seated`    | `Completed` | `Staff`      |

**Réponse 200 :** `BookingDto`

**Erreurs possibles :** `INVALID_STATUS_TRANSITION`, `NOSHOW_TOO_EARLY`, `INSUFFICIENT_ROLE`

---

#### `DELETE /api/bookings/{id}/table-lock`
Séparer une fusion de tables.

**Accès :** `Staff`, `Manager`, `Admin`

**Corps :** aucun

**Réponse 204**

**Erreurs possibles :** `404`, `422` (séparation impossible si Seated/Completed)

---

### Tables

#### `GET /api/tables/availability`
Rechercher les tables disponibles.

**Accès :** Public (authentifié)

**Paramètres de requête :**

| Paramètre    | Type    | Obligatoire | Description                       |
|--------------|---------|-------------|-----------------------------------|
| `date`       | string  | Oui         | `YYYY-MM-DD`                      |
| `serviceId`  | uuid    | Oui         |                                   |
| `guestsCount`| int     | Oui         | Nombre de convives                |
| `zone`       | string  | Non         | `Salle | Terrasse | Bar | SalonPrive` |

**Réponse 200 :**
```json
{
  "tables": [ "AvailableTableDto[]" ],
  "reason": "ServiceFullyBooked | null"
}
```
> `tables` est vide et `reason = "ServiceFullyBooked"` si MaxCovers atteint.

---

#### `POST /api/tables`
Créer une table.

**Accès :** `Admin`

**Corps de la requête :**
```json
{
  "number": 12,
  "capacity": 6,
  "minCapacity": 2,
  "zone": "Terrasse",
  "isCombinable": false
}
```

**Réponse 201 :** `TableDto`

**Erreurs :** `400` (minCapacity > capacity, number déjà existant), `403`

---

#### `PUT /api/tables/{id}`
Modifier une table.

**Accès :** `Admin`

**Corps de la requête :** mêmes champs que POST + `isActive: bool`

**Réponse 200 :** `TableDto`

---

### Vue salle (Floor Plan)

#### `GET /api/floor/snapshot`
Obtenir l'état visuel de toutes les tables pour un service donné.

**Accès :** `Staff`, `Manager`, `Admin`

**Paramètres de requête :**

| Paramètre   | Type   | Obligatoire | Description  |
|-------------|--------|-------------|--------------|
| `date`      | string | Oui         | `YYYY-MM-DD` |
| `serviceId` | uuid   | Oui         |              |

**Réponse 200 :** `FloorSnapshotDto`

**Contrainte de performance :** P95 ≤ 200 ms pour 50 tables.

---

### Clients

#### `GET /api/customers`
Rechercher un client.

**Accès :** `Staff`, `Manager`, `Admin`

**Paramètres de requête (au moins un) :**

| Paramètre | Type   | Description        |
|-----------|--------|--------------------|
| `phone`   | string | Numéro de téléphone|
| `email`   | string | Adresse email      |

**Réponse 200 :**
```json
{
  "customers": [ "CustomerDto[]" ]
}
```

---

#### `POST /api/customers`
Créer un client.

**Accès :** `Staff`, `Manager`, `Admin`

**Corps de la requête :**
```json
{
  "firstName": "Jean",
  "lastName": "Dupont",
  "phone": "0612345678",
  "email": "jean@email.fr (optionnel)"
}
```

**Réponse 201 :** `CustomerDto`

**Erreurs :** `400` (téléphone invalide ou déjà existant)

---

#### `GET /api/customers/{id}`
Obtenir la fiche d'un client.

**Accès :** `Staff`, `Manager`, `Admin`

**Réponse 200 :** `CustomerDto`

---

#### `GET /api/customers/{id}/bookings`
Obtenir les réservations d'un client.

**Accès :** `Online` (ses propres), `Staff`, `Manager`, `Admin`

**Paramètres de requête :**

| Paramètre | Type   | Description                          |
|-----------|--------|--------------------------------------|
| `status`  | string | Filtre optionnel sur le statut        |
| `from`    | string | Date de début (`YYYY-MM-DD`)         |
| `to`      | string | Date de fin (`YYYY-MM-DD`)           |

**Réponse 200 :**
```json
{
  "bookings": [ "BookingDto[]" ]
}
```

---

#### `PATCH /api/customers/{id}/blacklist`
Modifier le statut de blacklist d'un client.

**Accès :** `Staff`, `Manager`, `Admin`

**Corps de la requête :**
```json
{
  "isBlacklisted": false
}
```

**Réponse 200 :** `CustomerDto`

> L'action est journalisée dans les logs d'audit (NFR-9).

---

### Services de restauration

#### `GET /api/services`
Lister les services actifs.

**Accès :** Public (authentifié)

**Réponse 200 :**
```json
{
  "services": [ "DiningServiceDto[]" ]
}
```

---

#### `POST /api/services`
Créer un service.

**Accès :** `Admin`

**Corps de la requête :**
```json
{
  "name": "Dîner",
  "startTime": "19:00",
  "endTime": "23:00",
  "lastBookingTime": "21:30",
  "durationMinutes": 150,
  "maxCovers": 60
}
```

**Réponse 201 :** `DiningServiceDto`

**Erreurs :** `400` (endTime <= startTime, lastBookingTime > endTime)

---

#### `PUT /api/services/{id}`
Modifier un service.

**Accès :** `Admin`

**Corps de la requête :** mêmes champs que POST + `isActive: bool`

**Réponse 200 :** `DiningServiceDto`

---

### Jours de fermeture

#### `GET /api/closed-days`
Lister les jours de fermeture.

**Accès :** Public (authentifié)

**Réponse 200 :**
```json
{
  "closedDays": [ "ClosedDayDto[]" ]
}
```

---

#### `POST /api/closed-days`
Déclarer un jour de fermeture.

**Accès :** `Manager`, `Admin`

**Corps de la requête :**
```json
{
  "date": "YYYY-MM-DD",
  "reason": "Travaux exceptionnels"
}
```

**Réponse 201 :**
```json
{
  "closedDay": "ClosedDayDto",
  "cancelledBookingsCount": 7
}
```

**Erreurs :** `400` (date dans le passé), `409` (jour déjà déclaré fermé)

---

## Hub SignalR

### Connexion

```
URL   : /hubs/floor
Auth  : token JWT dans le paramètre query ?access_token=<token>
        (standard SignalR — HttpClient ne peut pas envoyer le header Bearer sur WebSocket)
```

### Rejoindre un groupe

Après connexion, le client invoque :
```
InvokeAsync("JoinFloorGroup", date: "YYYY-MM-DD", serviceId: "uuid")
```

Pour quitter (changement de service/date) :
```
InvokeAsync("LeaveFloorGroup", date: "YYYY-MM-DD", serviceId: "uuid")
```

### Événements émis par le serveur

#### `TableStatusChanged`
Déclenché à chaque changement de statut d'une table.
```json
{
  "tableId": "uuid",
  "newStatus": "Free | Pending | Confirmed | Seated | NoShow | Inactive",
  "bookingId": "uuid | null"
}
```

#### `BookingUpdated`
Déclenché quand une réservation change de statut (complète le précédent pour les vues liste).
```json
{
  "bookingId": "uuid",
  "newStatus": "Pending | Confirmed | Seated | Completed | Cancelled | NoShow | Rejected",
  "tableId": "uuid | null"
}
```

#### `ServiceCapacityChanged`
Déclenché quand le total de couverts confirmés du service change.
```json
{
  "serviceId": "uuid",
  "date": "YYYY-MM-DD",
  "totalConfirmedCovers": 28,
  "remainingCovers": 32
}
```

### Comportement à la reconnexion

Après reconnexion automatique (`@microsoft/signalr` avec backoff exponentiel), le client Angular doit :
1. Rappeler `JoinFloorGroup` pour rejoindre le groupe.
2. Recharger le snapshot complet via `GET /api/floor/snapshot` pour compenser les événements manqués.

---

## Matrice des droits par endpoint

| Endpoint                              | Online | Staff | Manager | Admin |
|---------------------------------------|--------|-------|---------|-------|
| `POST /api/auth/login`                | ✅     | ✅    | ✅      | ✅    |
| `POST /api/bookings`                  | ✅*    | ✅    | ✅      | ✅    |
| `GET /api/bookings/{id}`              | ✅*    | ✅    | ✅      | ✅    |
| `PUT /api/bookings/{id}`              | ✅**   | ✅**  | ✅      | ✅    |
| `DELETE /api/bookings/{id}`           | ✅*    | ✅    | ✅      | ✅    |
| `PATCH /api/bookings/{id}/status`     | ❌     | ✅    | ✅      | ✅    |
| `DELETE /api/bookings/{id}/table-lock`| ❌     | ✅    | ✅      | ✅    |
| `GET /api/tables/availability`        | ✅     | ✅    | ✅      | ✅    |
| `POST /api/tables`                    | ❌     | ❌    | ❌      | ✅    |
| `PUT /api/tables/{id}`                | ❌     | ❌    | ❌      | ✅    |
| `GET /api/floor/snapshot`             | ❌     | ✅    | ✅      | ✅    |
| `GET /api/customers`                  | ❌     | ✅    | ✅      | ✅    |
| `POST /api/customers`                 | ❌     | ✅    | ✅      | ✅    |
| `GET /api/customers/{id}`             | ❌     | ✅    | ✅      | ✅    |
| `GET /api/customers/{id}/bookings`    | ✅*    | ✅    | ✅      | ✅    |
| `PATCH /api/customers/{id}/blacklist` | ❌     | ✅    | ✅      | ✅    |
| `GET /api/services`                   | ✅     | ✅    | ✅      | ✅    |
| `POST /api/services`                  | ❌     | ❌    | ❌      | ✅    |
| `PUT /api/services/{id}`              | ❌     | ❌    | ❌      | ✅    |
| `GET /api/closed-days`                | ✅     | ✅    | ✅      | ✅    |
| `POST /api/closed-days`               | ❌     | ❌    | ✅      | ✅    |
| Hub SignalR `/hubs/floor`             | ❌     | ✅    | ✅      | ✅    |

`*` Ses propres ressources uniquement.  
`**` Pending uniquement (Confirmed nécessite Manager/Admin).
