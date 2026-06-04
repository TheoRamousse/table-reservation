---
stepsCompleted: [1, 2, 3, 4]
inputDocuments:
  - docs/prds/prd-table-reservation-2026-06-04/prd.md
  - docs/architecture.md
  - documentation/specs.md
  - documentation/stack.md
  - documentation/data-model.md
  - documentation/conventions.md
status: final
---

# Système de Réservation de Tables — Epic Breakdown

## Overview

Ce document décompose les exigences du PRD, de l'architecture et des spécifications métier en epics et stories implémentables pour le système de réservation de tables (backend .NET 10 Clean Architecture + frontend Angular 20 + PostgreSQL 16 + SignalR).

---

## Requirements Inventory

### Functional Requirements

FR-1 : Validation des plages horaires — ArrivalTime ∈ [Service.StartTime, Service.LastBookingTime]
FR-2 : Calcul du créneau d'occupation — [ArrivalTime, ArrivalTime + DurationMinutes[
FR-3 : Contrainte de capacité — GuestsCount ∈ [Table.MinCapacity, Table.Capacity]
FR-4 : Plafond de couverts par service — somme Pending+Confirmed+Seated ≤ MaxCovers
FR-5 : Détection des conflits de table — pas de chevauchement de créneaux
FR-6 : Horizon de réservation selon canal × VipLevel (30j / 90j / 180j)
FR-7 : Délai minimum avant le service (2h online, 15min phone/staff, aucun walkin)
FR-8 : Interdiction de réserver dans le passé
FR-9 : Interdiction de réservation pour un client blacklisté
FR-10 : Blacklist automatique après 3 no-shows
FR-11 : Règles d'annulation (statuts autorisés, CancellationReason obligatoire, LateCancel)
FR-12 : Transitions de statut autorisées (matrice RB-014)
FR-13 : Modification d'une réservation avec revalidation complète
FR-14 : Droits par rôle (matrice Online / Staff / Manager / Admin)
FR-15 : Snapshot de la salle (GET /api/floor/snapshot) avec état de chaque table
FR-16 : Codes couleur des statuts de table (6 couleurs)
FR-17 : Interactions sur la vue salle (clic, hover, actions)
FR-18 : Mises à jour temps réel SignalR (TableStatusChanged, BookingUpdated, ServiceCapacityChanged)
FR-19 : Recherche de disponibilité (date + service + couverts + zone?, triée capacité croissante)
FR-20 : Création d'une fusion de tables (IsCombinable, TableLock)
FR-21 : Séparation d'une fusion (Pending/Confirmed uniquement)
FR-22 : Validation longueur SpecialRequests ≤ 500 caractères
FR-23 : Détection automatique flags (HasAllergyAlert, IsCelebration, NeedsHighChair)
FR-24 : Notifications asynchrones (confirmation, rappel 48h, rappel 2h, annulation restaurant)
FR-25 : Déclaration d'un jour de fermeture (ClosedDay) — blocage des nouvelles réservations
FR-26 : Annulation en cascade lors d'un ClosedDay (Pending+Confirmed → Cancelled + notifications)
FR-27 : Recherche de client (téléphone / email)
FR-28 : Lever un blacklist (Staff, Manager, Admin uniquement)
FR-29 : Gestion des tables — CRUD + activation/désactivation (Admin)
FR-30 : Gestion des services — CRUD (Admin)

### NonFunctional Requirements

NFR-1 : GET /api/floor/snapshot — P95 ≤ 200 ms pour 50 tables
NFR-2 : GET /api/tables/availability — P95 ≤ 300 ms
NFR-3 : POST /api/bookings — P95 ≤ 500 ms (toutes validations incluses)
NFR-4 : Propagation SignalR — P95 ≤ 2 secondes entre mutation et affichage second terminal
NFR-5 : Disponibilité — 99,5 % sur les heures d'ouverture
NFR-6 : Notifications asynchrones découplées — défaillance non bloquante pour la transaction principale
NFR-7 : Authentification JWT + autorisation par rôle vérifiée côté API
NFR-8 : Données client accessibles uniquement aux rôles Staff et supérieurs
NFR-9 : Logs d'audit sur lever de blacklist, déclaration de fermeture, transitions de statut
NFR-10 : WCAG 2.1 AA — codes couleur doublés d'un label textuel sur la vue salle
NFR-11 : Logging structuré JSON via Serilog + traces sur handlers MediatR
NFR-12 : Isolation transactionnelle sur les vérifications de disponibilité (pas de race condition)

### Additional Requirements

- Architecture Clean Architecture : Domain → Application → Infrastructure → API (aucune dépendance inverse)
- Pattern CQRS via MediatR — handlers n'ont pas de logique métier, ils orchestrent uniquement
- Domain Events pour les effets de bord asynchrones (notifications, blacklist automatique)
- IClock injecté partout (jamais DateTime.UtcNow directement) — testabilité garantie
- EF Core : snake_case en base, PascalCase en C#, convention globale dans DbContext
- Tests unitaires Domain : nommage Should_{résultat}_When_{condition}, structure AAA, pas de logique dans les tests
- Tests d'intégration : TestContainers PostgreSQL éphémère
- Enums stockés en VARCHAR (lisibilité des données brutes)
- Soft delete via deleted_at nullable sur bookings et customers

### UX Design Requirements

Aucun document UX Design séparé — les spécifications visuelles sont intégrées dans le PRD et l'architecture :
- UX-DR1 : Codes couleur des statuts de table (FR-16) doublés d'un label textuel (NFR-10 WCAG 2.1 AA)
- UX-DR2 : Tooltip sur hover des tables (nom client, couverts, heure d'arrivée)
- UX-DR3 : Sélecteur date + service dans l'en-tête de la vue salle
- UX-DR4 : Snackbar Angular Material pour les erreurs et confirmations
- UX-DR5 : Composants partagés : StatusBadge, ConfirmDialog, CustomerSearch

### FR Coverage Map

FR-1 → Epic 2 (Création réservation — validation plage horaire)
FR-2 → Epic 2 (Création réservation — créneau d'occupation)
FR-3 → Epic 2 (Création réservation — capacité table)
FR-4 → Epic 2 (Création réservation — couverts max service)
FR-5 → Epic 2 (Création réservation — conflits)
FR-6 → Epic 2 (Création réservation — horizon)
FR-7 → Epic 2 (Création réservation — délai minimum)
FR-8 → Epic 2 (Création réservation — date passée)
FR-9 → Epic 2 (Création réservation — client blacklisté)
FR-10 → Epic 2 (Gestion clients — blacklist automatique no-show)
FR-11 → Epic 2 (Annulation — LateCancel, VIP loss)
FR-12 → Epic 2 (Transitions de statut)
FR-13 → Epic 2 (Modification réservation)
FR-14 → Epic 2 (Droits par rôle)
FR-15 → Epic 3 (View salle — snapshot)
FR-16 → Epic 3 (Vue salle — codes couleur)
FR-17 → Epic 3 (Vue salle — interactions)
FR-18 → Epic 3 (Vue salle — SignalR)
FR-19 → Epic 4 (Disponibilité + portail client)
FR-20 → Epic 5 (Fusion de tables)
FR-21 → Epic 5 (Séparation de tables)
FR-22 → Epic 5 (Demandes spéciales — longueur)
FR-23 → Epic 5 (Demandes spéciales — flags)
FR-24 → Epic 6 (Notifications asynchrones)
FR-25 → Epic 7 (Administration — jours de fermeture)
FR-26 → Epic 7 (Administration — annulation en cascade)
FR-27 → Epic 7 (Administration — recherche client)
FR-28 → Epic 7 (Administration — lever blacklist)
FR-29 → Epic 7 (Administration — gestion tables)
FR-30 → Epic 7 (Administration — gestion services)

---

## Epic List

### Epic 1 : Infrastructure & Configuration de base
L'équipe peut développer, tester et démarrer le projet localement avec une base de données et une authentification fonctionnelles.

### Epic 2 : Gestion des réservations (domaine + API)
Un utilisateur authentifié peut créer, modifier, annuler et faire progresser une réservation en respectant toutes les règles métier — accessible via l'API REST avec contrôle des droits par rôle.
**FRs couverts :** FR-1 à FR-14

### Epic 3 : Vue salle en temps réel (Floor Plan)
Le staff visualise l'état en temps réel de toutes les tables pour un service donné et effectue les actions de gestion des arrivées depuis une interface intuitive.
**FRs couverts :** FR-15, FR-16, FR-17, FR-18

### Epic 4 : Disponibilité et portail de réservation client
Un client peut rechercher les disponibilités et réserver une table en ligne depuis le portail public.
**FRs couverts :** FR-19

### Epic 5 : Fonctionnalités avancées (fusion, demandes spéciales, fermeture)
Le restaurant gère les situations avancées : fusion de tables pour les grands groupes, besoins particuliers des clients, et jours de fermeture exceptionnels avec annulation automatique.
**FRs couverts :** FR-20, FR-21, FR-22, FR-23, FR-25, FR-26

### Epic 6 : Notifications asynchrones
Les clients reçoivent des notifications automatiques (email et SMS) à chaque étape clé de leur réservation, sans bloquer les transactions principales.
**FRs couverts :** FR-24

### Epic 7 : Administration et gestion des clients
L'administrateur configure le restaurant (tables, services) et le staff accède aux outils de gestion des clients (recherche, blacklist).
**FRs couverts :** FR-27, FR-28, FR-29, FR-30

---

## Epic 1 : Infrastructure & Configuration de base

L'équipe dispose d'une base de développement fonctionnelle : solution .NET structurée en Clean Architecture, base PostgreSQL avec migrations EF Core, projet Angular configuré et authentification JWT opérationnelle.

### Story 1.1 : Scaffolding de la solution .NET (Clean Architecture)

En tant que développeur,
je veux créer la structure de la solution .NET avec les quatre projets (Domain, Application, Infrastructure, Api) et leurs dépendances,
afin que l'équipe ait une base de code correctement organisée qui empêche les dépendances inverses.

**Acceptance Criteria :**

**Given** un environnement de développement avec .NET 10 SDK installé
**When** la solution est créée
**Then** elle contient quatre projets : `Reservation.Domain`, `Reservation.Application`, `Reservation.Infrastructure`, `Reservation.Api`
**And** `Reservation.Domain` n'a aucune référence vers Application, Infrastructure ou Api
**And** `Reservation.Application` référence uniquement `Reservation.Domain`
**And** `Reservation.Infrastructure` référence `Reservation.Application` et `Reservation.Domain`
**And** `Reservation.Api` référence `Reservation.Application` et `Reservation.Infrastructure`

**Given** les packages NuGet requis
**When** les projets sont restaurés
**Then** `Reservation.Api` inclut : `MediatR`, `FluentValidation.AspNetCore`, `Serilog.AspNetCore`, `Microsoft.AspNetCore.SignalR`, `Microsoft.AspNetCore.Authentication.JwtBearer`
**And** `Reservation.Infrastructure` inclut : `Microsoft.EntityFrameworkCore`, `Npgsql.EntityFrameworkCore.PostgreSQL`, `Mapster`
**And** `Reservation.Domain.Tests` inclut : `xUnit`, `FluentAssertions`

**Given** la solution scaffoldée
**When** `dotnet build` est exécuté
**Then** la compilation réussit sans erreur ni warning

### Story 1.2 : Base de données PostgreSQL + EF Core + migrations initiales

En tant que développeur,
je veux configurer EF Core avec PostgreSQL, créer le DbContext et les migrations initiales pour toutes les entités,
afin que la base de données soit prête à recevoir les données en développement et en production.

**Acceptance Criteria :**

**Given** un Docker Compose avec un service `postgres` (PostgreSQL 16)
**When** `docker compose up -d postgres` est exécuté
**Then** la base de données `reservation` est accessible sur le port 5432

**Given** le `ReservationDbContext` configuré avec la convention `snake_case` globale
**When** `dotnet ef migrations add InitialCreate` est exécuté
**Then** une migration est générée créant les tables : `tables`, `customers`, `dining_services`, `bookings`, `table_locks`, `closed_days`
**And** chaque table possède les colonnes `created_at` et `updated_at` (TIMESTAMPTZ NOT NULL)
**And** la table `bookings` possède la colonne `deleted_at` nullable (soft delete)
**And** les enums sont stockés en VARCHAR

**Given** la migration générée
**When** `dotnet ef database update` est exécuté
**Then** tous les index sont créés : `idx_bookings_date_service`, `idx_bookings_customer`, `idx_bookings_table_date`, `idx_table_locks_booking`

**Given** l'`UpdatedAtInterceptor` configuré sur le DbContext
**When** une entité est modifiée et `SaveChanges` est appelé
**Then** la colonne `updated_at` est mise à jour automatiquement sans code dans les handlers

### Story 1.3 : Scaffolding Angular + services de base + intercepteurs

En tant que développeur,
je veux créer le projet Angular avec la structure de dossiers cible, les modèles TypeScript alignés sur le backend et les intercepteurs HTTP de base,
afin que le frontend soit prêt à consommer l'API et à afficher des erreurs cohérentes.

**Acceptance Criteria :**

**Given** Angular CLI 20 installé
**When** le projet Angular est créé et configuré
**Then** la structure `src/app/core/`, `features/`, `shared/` est en place
**And** Tailwind CSS 4.x et Angular Material 20 sont configurés
**And** `ng serve` démarre sans erreur sur `http://localhost:4200`

**Given** les modèles TypeScript créés dans `core/models/`
**When** les interfaces sont définies
**Then** `Booking`, `BookingStatus`, `Table`, `TableZone`, `Customer`, `DiningService`, `FloorSnapshot` existent avec les champs alignés sur les DTOs backend
**And** `BookingStatus` est un enum string : `Pending | Confirmed | Seated | Completed | Cancelled | NoShow`

**Given** l'intercepteur d'erreur global `error.interceptor.ts`
**When** l'API retourne une erreur HTTP
**Then** un snackbar Angular Material affiche le message d'erreur issu du champ `message` de la réponse
**And** les erreurs réseau affichent un message générique

### Story 1.4 : Authentification JWT — backend + login Angular

En tant qu'utilisateur,
je veux me connecter avec mes identifiants et recevoir un token JWT,
afin d'accéder aux fonctionnalités protégées selon mon rôle.

**Acceptance Criteria :**

**Given** un utilisateur avec des identifiants valides
**When** il envoie `POST /api/auth/login` avec `{ email, password }`
**Then** l'API retourne HTTP 200 avec `{ token, expiresAt, role }`
**And** le token JWT contient le claim de rôle (`Online`, `Staff`, `Manager`, `Admin`)

**Given** un utilisateur avec des identifiants invalides
**When** il envoie `POST /api/auth/login`
**Then** l'API retourne HTTP 401

**Given** le composant de login Angular
**When** le token est reçu
**Then** il est stocké et inclus dans les requêtes suivantes via `auth.interceptor.ts` (header `Authorization: Bearer {token}`)

**Given** un endpoint protégé par `[Authorize]`
**When** une requête est envoyée sans token ou avec un token expiré
**Then** l'API retourne HTTP 401

---

## Epic 2 : Gestion des réservations (domaine + API)

Le staff et les clients peuvent créer, modifier, annuler et faire progresser des réservations via l'API REST, avec toutes les règles métier appliquées et tous les droits par rôle respectés.

### Story 2.1 : Entités du domaine et règles de structure

En tant que développeur,
je veux créer les entités, Value Objects, enums et exceptions du domaine,
afin que la couche domain encapsule tous les invariants métier sans dépendance externe.

**Acceptance Criteria :**

**Given** l'entité `Booking`
**When** elle est examinée
**Then** elle expose les méthodes comportementales : `Create(...)`, `Cancel(reason, now, role)`, `TransitionTo(status, role, now)`, `Modify(...)`
**And** elle n'importe aucun namespace `Microsoft.EntityFrameworkCore` ou `Microsoft.AspNetCore`

**Given** le Value Object `TimeSlot`
**When** deux créneaux sont comparés
**Then** `TimeSlot.Overlaps(other)` retourne `false` si `A.EndTime == B.StartTime` (rotation autorisée)
**And** `TimeSlot.Overlaps(other)` retourne `true` si les créneaux se chevauchent d'une minute

**Given** l'interface `IClock`
**When** `Booking.Create(...)` a besoin de l'heure courante
**Then** il utilise `IClock.UtcNow` injecté — jamais `DateTime.UtcNow` directement

**Given** la classe `SystemClock` dans Infrastructure
**When** elle implémente `IClock`
**Then** `SystemClock.UtcNow` retourne `DateTimeOffset.UtcNow`

**Given** les tests unitaires des entités Domain
**When** ils sont exécutés avec `dotnet test`
**Then** ils passent sans référence à EF Core ou à l'Infrastructure

### Story 2.2 : Création d'une réservation — règles métier RB-001 à RB-006

En tant qu'utilisateur authentifié,
je veux créer une réservation en fournissant la date, le service, l'heure d'arrivée, le nombre de couverts et mes coordonnées,
afin de réserver une table disponible en respectant toutes les contraintes du restaurant.

**Acceptance Criteria :**

**Given** une `ArrivalTime` égale à `Service.LastBookingTime`
**When** `POST /api/bookings` est appelé
**Then** la réservation est créée avec HTTP 201

**Given** une `ArrivalTime` dépassant `Service.LastBookingTime` d'une minute
**When** `POST /api/bookings` est appelé
**Then** l'API retourne HTTP 422 avec `code: "TIME_OUTSIDE_SERVICE"` (RB-001)

**Given** un `GuestsCount` égal à `Table.MinCapacity - 1`
**When** `POST /api/bookings` est appelé
**Then** l'API retourne HTTP 422 avec `code: "GUESTS_BELOW_MIN"` (RB-002)

**Given** un `GuestsCount` égal à `Table.Capacity + 1`
**When** `POST /api/bookings` est appelé
**Then** l'API retourne HTTP 422 avec `code: "GUESTS_EXCEED_CAPACITY"` (RB-002)

**Given** un ajout de couvert portant le total du service à `MaxCovers + 1`
**When** `POST /api/bookings` est appelé
**Then** l'API retourne HTTP 422 avec `code: "SERVICE_FULLY_BOOKED"` (RB-003)

**Given** deux réservations dos à dos sur la même table (`A.EndTime == B.ArrivalTime`)
**When** la seconde réservation est créée
**Then** elle est acceptée (HTTP 201) — pas de conflit (RB-004)

**Given** deux réservations dont les créneaux se chevauchent d'une minute
**When** la seconde réservation est créée
**Then** l'API retourne HTTP 422 avec `code: "TABLE_CONFLICT"` (RB-004)

**Given** un client `Standard` (Online) tentant de réserver à J+31
**When** `POST /api/bookings` est appelé avec `Source: Online`
**Then** l'API retourne HTTP 422 avec `code: "HORIZON_EXCEEDED"` (RB-005)

**Given** un client `VIP` tentant de réserver à J+31 via le canal Online
**When** `POST /api/bookings` est appelé
**Then** la réservation est acceptée (horizon = 180 jours pour VIP) (RB-005)

**Given** un WalkIn avec `BookingDate` = demain
**When** `POST /api/bookings` est appelé avec `Source: WalkIn`
**Then** l'API retourne HTTP 422 avec `code: "WALKIN_MUST_BE_TODAY"` (RB-005)

**Given** une réservation Online créée à H-1h45 (moins de 2h avant l'arrivée)
**When** `POST /api/bookings` est appelé
**Then** l'API retourne HTTP 422 avec `code: "MIN_LEAD_TIME_VIOLATED"` (RB-006)

**Given** une réservation Phone créée à H-10min (moins de 15 min avant l'arrivée)
**When** `POST /api/bookings` est appelé
**Then** l'API retourne HTTP 422 avec `code: "MIN_LEAD_TIME_VIOLATED"` (RB-006)

**Given** une réservation créée pour une date passée
**When** `POST /api/bookings` est appelé
**Then** l'API retourne HTTP 422 avec `code: "BOOKING_DATE_IN_PAST"` (RB-005)

### Story 2.3 : Transitions de statut — RB-014

En tant que membre du staff,
je veux faire progresser une réservation à travers ses statuts autorisés (Confirmed, Seated, Completed, NoShow),
afin de refléter l'état réel de l'occupation des tables.

**Acceptance Criteria :**

**Given** une réservation `Pending` et un acteur `Staff`
**When** `PATCH /api/bookings/{id}/status` avec `{ newStatus: "Confirmed" }` est appelé
**Then** la réservation passe à `Confirmed` (HTTP 200)

**Given** une réservation `Confirmed` et un acteur `Staff`
**When** `PATCH /api/bookings/{id}/status` avec `{ newStatus: "Seated" }` est appelé
**Then** la réservation passe à `Seated` (HTTP 200)

**Given** une réservation `Seated` et un acteur `Staff`
**When** `PATCH /api/bookings/{id}/status` avec `{ newStatus: "Completed" }` est appelé
**Then** la réservation passe à `Completed` (HTTP 200)

**Given** une réservation `Confirmed` et un acteur `Staff`
**When** `PATCH /api/bookings/{id}/status` avec `{ newStatus: "NoShow" }` est appelé avant `ArrivalTime + 15 min`
**Then** l'API retourne HTTP 422 avec `code: "NOSHOW_TOO_EARLY"` (RB-008)

**Given** une réservation `Confirmed` et un acteur `Staff`
**When** `PATCH /api/bookings/{id}/status` avec `{ newStatus: "NoShow" }` est appelé après `ArrivalTime + 15 min`
**Then** la réservation passe à `NoShow` (HTTP 200)
**And** `Customer.NoShowCount` est incrémenté de 1

**Given** la 3e réservation `NoShow` d'un client
**When** la transition est appliquée
**Then** `Customer.IsBlacklisted` passe automatiquement à `true` (RB-007 + RB-008)

**Given** une tentative de transition `Cancelled → Confirmed`
**When** `PATCH /api/bookings/{id}/status` est appelé
**Then** l'API retourne HTTP 409 avec `code: "INVALID_STATUS_TRANSITION"` (RB-014)

**Given** une tentative de transition `Completed → Seated`
**When** `PATCH /api/bookings/{id}/status` est appelé
**Then** l'API retourne HTTP 409 avec `code: "INVALID_STATUS_TRANSITION"` (RB-014)

**Given** une réservation `Seated` et un acteur `Staff`
**When** `PATCH /api/bookings/{id}/status` avec `{ newStatus: "Cancelled" }` est appelé
**Then** l'API retourne HTTP 422 avec `code: "CANNOT_CANCEL_SEATED"` (RB-009)

### Story 2.4 : Annulation et modification d'une réservation — RB-009, RB-013

En tant qu'utilisateur autorisé,
je veux annuler ou modifier une réservation en respectant les contraintes de statut et de rôle,
afin de gérer les changements de plans des clients sans violer les règles du restaurant.

**Acceptance Criteria :**

**Given** une réservation `Pending` ou `Confirmed`
**When** `DELETE /api/bookings/{id}` est appelé avec `{ cancellationReason: "..." }`
**Then** la réservation passe à `Cancelled` (HTTP 200)
**And** `LateCancel = true` si l'annulation intervient moins de 24h avant `ArrivalTime`

**Given** une annulation sans `cancellationReason`
**When** `DELETE /api/bookings/{id}` est appelé
**Then** l'API retourne HTTP 400 (validation)

**Given** le 2e `LateCancel` d'un client
**When** l'annulation est appliquée
**Then** `Customer.VipLevel` passe automatiquement à `None` (perte des avantages)

**Given** une réservation `Confirmed` et un acteur `Online` (client)
**When** `PUT /api/bookings/{id}` est appelé pour modifier la date
**Then** l'API retourne HTTP 403 avec `code: "INSUFFICIENT_ROLE"` (RB-013 + RB-010)

**Given** une réservation `Confirmed` et un acteur `Manager`
**When** `PUT /api/bookings/{id}` est appelé pour modifier la date
**Then** la réservation est modifiée et repasse à `Pending` (RB-013)
**And** toutes les règles métier (RB-001 à RB-006) sont revalidées sur les nouvelles valeurs

**Given** un client `Online` tentant d'annuler la réservation d'un autre client
**When** `DELETE /api/bookings/{id}` est appelé
**Then** l'API retourne HTTP 403 (RB-010)

### Story 2.5 : Gestion des clients — blacklist et no-show (RB-007, RB-008)

En tant que membre du staff,
je veux créer un client, consulter sa fiche et gérer son statut de blacklist,
afin de contrôler l'accès au service de réservation.

**Acceptance Criteria :**

**Given** un client avec `IsBlacklisted = true`
**When** une tentative de création de réservation est effectuée (tous canaux)
**Then** l'API retourne HTTP 422 avec `code: "CUSTOMER_BLACKLISTED"` (RB-007)

**Given** un client blacklisté
**When** il consulte ses réservations passées (`GET /api/customers/{id}/bookings`)
**Then** ses réservations passées sont accessibles (lecture seule)

**Given** un acteur `Staff`
**When** `PATCH /api/customers/{id}/blacklist` avec `{ isBlacklisted: false }` est appelé
**Then** `Customer.IsBlacklisted` passe à `false` (HTTP 200)
**And** l'action est journalisée dans les logs d'audit

**Given** un acteur `Online` (client)
**When** il tente de lever son propre blacklist
**Then** l'API retourne HTTP 403

**Given** `GET /api/customers?phone=0612345678`
**When** la recherche est effectuée par le Staff
**Then** la fiche client retournée inclut `NoShowCount`, `VipLevel`, `IsBlacklisted`

---

## Epic 3 : Vue salle en temps réel (Floor Plan)

Le staff peut visualiser l'état de toutes les tables pour un service donné, effectuer des actions de gestion (asseoir, compléter, no-show) et voir les changements se propager en temps réel sur tous les terminaux.

### Story 3.1 : Snapshot de la salle et composant FloorPlan statique

En tant que membre du staff,
je veux voir une vue visuelle de toutes les tables du restaurant pour un service et une date donnés,
afin de connaître instantanément l'état d'occupation de la salle.

**Acceptance Criteria :**

**Given** une date et un serviceId valides
**When** `GET /api/floor/snapshot?date=2026-06-15&serviceId={id}` est appelé
**Then** l'API retourne HTTP 200 avec la liste de toutes les tables actives groupées par zone
**And** chaque table inclut : `id`, `number`, `capacity`, `zone`, `status`, la réservation active si applicable (`customerId`, `guestCount`, `arrivalTime`), les flags `hasAllergyAlert`, `isCelebration`, `needsHighChair`
**And** le temps de réponse est ≤ 200 ms (P95) pour 50 tables

**Given** le composant Angular `FloorPlanComponent`
**When** une date et un service sont sélectionnés
**Then** les tables sont affichées groupées par zone (Salle, Terrasse, Bar, Salon privé)
**And** chaque table affiche un badge de couleur selon son statut (FR-16) ET un label textuel du statut (WCAG 2.1 AA)

**Given** les codes couleur définis
**When** les tables sont affichées
**Then** Libre = vert `#22c55e`, Réservée = bleu `#3b82f6`, En attente = jaune `#eab308`, Occupée = orange `#f97316`, No-show = rouge `#ef4444`, Inactive = gris `#9ca3af`

**Given** une table inactive (`IsActive = false`)
**When** le snapshot est chargé
**Then** la table n'apparaît pas dans les résultats

### Story 3.2 : Interactions sur les tables (clic, hover, actions)

En tant que membre du staff,
je veux interagir avec les tables de la vue salle pour ouvrir des détails, effectuer des actions de statut et créer des réservations,
afin de gérer les arrivées et les départs depuis la vue salle directement.

**Acceptance Criteria :**

**Given** un hover sur une table avec une réservation
**When** la souris survole la carte de table
**Then** un tooltip s'affiche avec : nom du client, nombre de couverts, heure d'arrivée

**Given** un clic sur une table **libre**
**When** le staff clique dessus
**Then** le formulaire de réservation s'ouvre pré-rempli avec cette table sélectionnée

**Given** un clic sur une table **réservée** ou **en attente**
**When** le staff clique dessus
**Then** le détail de la réservation s'ouvre avec les actions disponibles selon le statut

**Given** un clic sur une table **occupée** (`Seated`)
**When** le staff clique dessus
**Then** un dialogue de confirmation apparaît pour marquer la table comme `Completed`
**And** après confirmation, `PATCH /api/bookings/{id}/status` avec `Completed` est appelé

**Given** le détail d'une réservation `Confirmed` ouverte
**When** le staff clique « Asseoir »
**Then** `PATCH /api/bookings/{id}/status` avec `Seated` est appelé

### Story 3.3 : Temps réel via SignalR

En tant que membre du staff,
je veux que les changements d'état des tables se reflètent automatiquement sur mon terminal sans recharger la page,
afin que la vue salle reste toujours synchronisée en temps réel avec l'activité du restaurant.

**Acceptance Criteria :**

**Given** le hub SignalR `/hubs/floor` configuré dans le backend
**When** `FloorHub` est démarré
**Then** les clients se connectent et rejoignent le groupe `floor-{date}-{serviceId}` correspondant à leur vue actuelle

**Given** un changement de statut déclenché sur le terminal A (ex: `Seated`)
**When** l'événement `TableStatusChanged { tableId, newStatus }` est émis
**Then** le composant Angular sur le terminal B met à jour la couleur de la table en moins de 2 secondes (P95)
**And** la mise à jour se fait via le Signal `floorSnapshot` sans rechargement de page

**Given** une déconnexion SignalR
**When** le client se reconnecte automatiquement
**Then** il recharge le snapshot complet (`GET /api/floor/snapshot`) pour récupérer l'état actuel
**And** reprend l'abonnement au groupe

**Given** la création d'une réservation qui modifie les couverts disponibles
**When** l'événement `ServiceCapacityChanged { serviceId, date, remainingCovers }` est émis
**Then** le compteur de couverts dans l'en-tête de la vue salle est mis à jour

---

## Epic 4 : Disponibilité et portail de réservation client

Un client peut rechercher les tables disponibles pour une date, un service et un nombre de couverts donnés, puis créer une réservation en ligne depuis son navigateur.

### Story 4.1 : Recherche de disponibilité (API + composant Angular)

En tant que client en ligne,
je veux saisir une date, un service et un nombre de couverts pour voir les tables disponibles,
afin de choisir la table qui me convient avant de réserver.

**Acceptance Criteria :**

**Given** une requête avec `date`, `serviceId`, `guestsCount` valides
**When** `GET /api/tables/availability?date=&serviceId=&guestsCount=&zone=` est appelé
**Then** l'API retourne les tables dont `guestsCount ∈ [Table.MinCapacity, Table.Capacity]`, triées par capacité croissante
**And** le temps de réponse est ≤ 300 ms (P95)

**Given** un service dont `MaxCovers` est atteint
**When** la recherche de disponibilité est effectuée
**Then** l'API retourne une liste vide avec `reason: "ServiceFullyBooked"`

**Given** une table `IsActive = false`
**When** la recherche de disponibilité est effectuée
**Then** cette table n'apparaît jamais dans les résultats

**Given** le filtre `zone` fourni en paramètre
**When** la recherche est effectuée
**Then** seules les tables de la zone demandée sont retournées

**Given** le composant Angular `AvailabilitySearchComponent`
**When** les résultats sont affichés
**Then** les tables sont triées par capacité croissante avec leur numéro, zone et disponibilité

### Story 4.2 : Formulaire de réservation client en ligne

En tant que client en ligne,
je veux remplir un formulaire de réservation avec mes coordonnées, choisir une table disponible et confirmer ma réservation,
afin d'obtenir une confirmation immédiate de ma réservation.

**Acceptance Criteria :**

**Given** le composant Angular `BookingFormComponent` avec une table présélectionnée
**When** le client remplit ses coordonnées et soumet le formulaire
**Then** `POST /api/bookings` est appelé avec `Source: Online`
**And** une réservation `Pending` est créée (HTTP 201)

**Given** une réservation créée avec succès
**When** la réponse est reçue
**Then** un message de confirmation s'affiche avec : date, service, heure d'arrivée, table, nombre de couverts

**Given** une erreur de validation (ex: table plus disponible)
**When** l'API retourne HTTP 422
**Then** le message d'erreur correspondant au `code` s'affiche à l'utilisateur

**Given** un client `IsBlacklisted = true` tentant de réserver
**When** `POST /api/bookings` est appelé
**Then** l'API retourne HTTP 422 avec `code: "CUSTOMER_BLACKLISTED"` et le formulaire affiche le message approprié

---

## Epic 5 : Fonctionnalités avancées (fusion, demandes spéciales, fermeture)

Le restaurant peut gérer les grandes tablées via la fusion de tables, prendre en compte les besoins particuliers des clients via les demandes spéciales, et déclarer des jours de fermeture avec annulation automatique des réservations concernées.

### Story 5.1 : Fusion de tables (RB-011)

En tant que membre du staff,
je veux fusionner deux tables combinables pour accueillir un groupe dont la taille dépasse une table individuelle,
afin de maximiser l'utilisation des espaces pour les grands groupes.

**Acceptance Criteria :**

**Given** deux tables `IsCombinable = true` toutes deux libres sur le créneau demandé
**When** une réservation est créée en spécifiant `tableId` (principale) et `secondaryTableId`
**Then** un `TableLock` est créé liant les deux tables
**And** la capacité effective = `Table1.Capacity + Table2.Capacity`

**Given** une table avec `IsCombinable = false`
**When** une tentative de fusion est effectuée
**Then** l'API retourne HTTP 422 avec `code: "TABLE_NOT_COMBINABLE"` (RB-011)

**Given** l'une des deux tables occupée sur le créneau demandé
**When** une tentative de fusion est effectuée
**Then** l'API retourne HTTP 422 avec `code: "TABLE_CONFLICT"`

**Given** une réservation sur tables fusionnées avec statut `Pending` ou `Confirmed`
**When** `DELETE /api/bookings/{id}/table-lock` est appelé (séparation)
**Then** le `TableLock` est supprimé et les deux tables redeviennent indépendantes

**Given** une réservation sur tables fusionnées avec statut `Seated` ou `Completed`
**When** une séparation est tentée
**Then** l'API retourne HTTP 422 (séparation impossible — RB-011)

### Story 5.2 : Demandes spéciales et flags informatifs (RB-012)

En tant que client,
je veux préciser mes besoins particuliers (allergies, célébration, chaise bébé) dans ma réservation,
afin que le restaurant soit informé et puisse s'organiser en conséquence.

**Acceptance Criteria :**

**Given** un `SpecialRequests` de 501 caractères
**When** `POST /api/bookings` est appelé
**Then** l'API retourne HTTP 400 avec `code: "SPECIAL_REQUESTS_TOO_LONG"` (RB-012)

**Given** un `SpecialRequests` contenant "Allergie aux arachides"
**When** la réservation est créée
**Then** `HasAllergyAlert = true` est positionné automatiquement

**Given** un `SpecialRequests` contenant "anniversaire de mariage"
**When** la réservation est créée
**Then** `IsCelebration = true` est positionné automatiquement

**Given** un `SpecialRequests` contenant "chaise bébé requise"
**When** la réservation est créée
**Then** `NeedsHighChair = true` est positionné automatiquement

**Given** une réservation avec `HasAllergyAlert = true`
**When** le snapshot de la salle est retourné
**Then** le flag `hasAllergyAlert` est visible dans le détail de la table
**And** le flag est informatif et n'a pas bloqué la création

### Story 5.3 : Jours de fermeture — déclaration et annulation en cascade (RB-017)

En tant que manager ou administrateur,
je veux déclarer un jour de fermeture exceptionnel,
afin que le restaurant annule automatiquement toutes les réservations existantes sur cette date et notifie les clients concernés.

**Acceptance Criteria :**

**Given** un Manager authentifié
**When** `POST /api/closed-days` avec `{ date: "2026-06-20", reason: "Travaux" }` est appelé
**Then** un `ClosedDay` est créé (HTTP 201)

**Given** un `ClosedDay` déclaré pour le 2026-06-20
**When** `POST /api/bookings` avec `bookingDate: "2026-06-20"` est appelé
**Then** l'API retourne HTTP 422 avec `code: "RESTAURANT_CLOSED"` (RB-017)

**Given** des réservations `Pending` et `Confirmed` existantes sur la date déclarée fermée
**When** le `ClosedDay` est créé
**Then** toutes ces réservations passent à `Cancelled` avec `cancellationReason: "RestaurantClosed"` automatiquement
**And** un `ClosedDayDeclaredEvent` est émis pour déclencher les notifications client

**Given** des réservations `Seated` ou `Completed` existantes sur la date fermée (cas théorique)
**When** le `ClosedDay` est déclaré
**Then** ces réservations ne sont pas annulées

---

## Epic 6 : Notifications asynchrones

Les clients reçoivent des notifications automatiques par email et SMS à chaque étape clé de leur réservation, sans que ces envois ne bloquent ou ne retardent les opérations du restaurant.

### Story 6.1 : Infrastructure Domain Events + service de notification

En tant que développeur,
je veux mettre en place l'infrastructure de Domain Events et le service de notification,
afin que les effets de bord (emails, SMS) soient découplés des transactions principales.

**Acceptance Criteria :**

**Given** un `Booking` dont l'état change (confirmation, annulation, etc.)
**When** `bookingRepo.SaveChanges()` est appelé
**Then** les Domain Events collectés sur l'entité sont dispatchés via MediatR Publish après le SaveChanges

**Given** l'interface `INotificationService` dans Application
**When** `NotificationService` dans Infrastructure est examiné
**Then** il implémente `INotificationService` avec les méthodes `SendConfirmationAsync`, `SendReminderAsync`, `SendCancellationAsync`
**And** `NotificationService` n'a aucune référence directe aux entités Domain (il reçoit des DTOs de notification)

**Given** une défaillance du service SMS ou email
**When** la notification échoue
**Then** l'exception est loguée mais ne remonte pas dans la transaction principale
**And** la réservation reste dans son statut attendu (la notification ne bloque pas)

**Given** un client sans email ni téléphone valide
**When** une notification doit lui être envoyée
**Then** la notification est ignorée silencieusement (aucune exception levée)

### Story 6.2 : Notifications client — confirmation, rappels et annulation

En tant que client,
je veux recevoir une confirmation de réservation par email et SMS, des rappels 48h et 2h avant mon repas, et une notification si le restaurant annule,
afin d'être informé à chaque étape et de pouvoir me préparer.

**Acceptance Criteria :**

**Given** une réservation passant à `Confirmed`
**When** le `BookingConfirmedEvent` est traité par le handler Infrastructure
**Then** un email ET un SMS sont envoyés avec : date, service, heure d'arrivée, nombre de couverts, numéro de table

**Given** une réservation `Confirmed` dont `ArrivalTime` est dans 48h
**When** le job de rappel s'exécute
**Then** un email ET un SMS de rappel sont envoyés avec un lien d'annulation

**Given** une réservation `Confirmed` dont `ArrivalTime` est dans 2h
**When** le job de rappel final s'exécute
**Then** un SMS (uniquement) de rappel final est envoyé

**Given** une réservation annulée par le restaurant (`cancellationReason = "RestaurantClosed"`)
**When** le `BookingCancelledByRestaurantEvent` est traité
**Then** un email ET un SMS sont envoyés avec la raison et une proposition de report

---

## Epic 7 : Administration et gestion des clients

L'administrateur peut configurer les tables et les services du restaurant, et le staff dispose des outils pour gérer les clients (recherche, levée de blacklist) et les jours de fermeture depuis l'interface Angular.

### Story 7.1 : Gestion des tables et services — API + interface Admin

En tant qu'administrateur,
je veux créer, modifier et désactiver les tables du restaurant ainsi que configurer les services (plages horaires, couverts max),
afin de maintenir à jour la configuration du restaurant.

**Acceptance Criteria :**

**Given** un Admin authentifié
**When** `POST /api/tables` avec `{ number, capacity, minCapacity, zone, isCombinable }` est appelé
**Then** la table est créée (HTTP 201)
**And** contrainte : `minCapacity <= capacity`, sinon HTTP 400

**Given** un Admin
**When** `PUT /api/tables/{id}` avec `{ isActive: false }` est appelé
**Then** la table est désactivée
**And** elle n'apparaît plus dans les résultats de disponibilité

**Given** un acteur `Staff` (non Admin)
**When** `POST /api/tables` est appelé
**Then** l'API retourne HTTP 403

**Given** un Admin
**When** `POST /api/services` avec `{ name, startTime, endTime, lastBookingTime, durationMinutes, maxCovers }` est appelé
**Then** le service est créé (HTTP 201)
**And** contrainte : `endTime > startTime` et `lastBookingTime <= endTime`, sinon HTTP 400

**Given** le composant Angular `TableManagementComponent`
**When** un Admin consulte la liste des tables
**Then** toutes les tables sont affichées avec leur statut actif/inactif et les boutons CRUD

### Story 7.2 : Gestion des clients et interface Admin — recherche, blacklist, fermeture

En tant que membre du staff,
je veux rechercher un client, consulter sa fiche et gérer son blacklist depuis l'interface Angular, ainsi que gérer les jours de fermeture,
afin d'avoir tous les outils de gestion opérationnelle en un seul endroit.

**Acceptance Criteria :**

**Given** le composant Angular `CustomerSearchComponent`
**When** le staff saisit un numéro de téléphone ou un email
**Then** la recherche `GET /api/customers?phone=...` est appelée
**And** la fiche client s'affiche avec `NoShowCount`, `VipLevel`, `IsBlacklisted`

**Given** une fiche client affichant `IsBlacklisted = true`
**When** le staff clique sur « Lever le blacklist »
**Then** un dialogue de confirmation s'affiche
**And** après confirmation, `PATCH /api/customers/{id}/blacklist` avec `{ isBlacklisted: false }` est appelé
**And** un message de succès s'affiche

**Given** le composant Angular `ClosedDaysComponent`
**When** un Manager ouvre la vue des jours de fermeture
**Then** la liste des jours de fermeture existants est affichée
**And** un formulaire permet d'en ajouter avec une date et un motif

**Given** un Manager qui soumet un nouveau jour de fermeture
**When** le formulaire est soumis
**Then** `POST /api/closed-days` est appelé
**And** la liste se met à jour avec le nouveau jour
**And** un message indique le nombre de réservations annulées en cascade
