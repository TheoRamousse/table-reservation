---
title: "Système de Réservation de Tables — Restaurant"
status: ready-for-review
created: 2026-06-04
updated: 2026-06-04
project: table-reservation
---

# PRD : Système de Réservation de Tables — Restaurant

## 0. Objet du document

Ce PRD s'adresse aux développeurs, à l'a
rchitecte technique et aux parties prenantes du projet. Il structure les exigences fonctionnelles et non-fonctionnelles du système de réservation de tables, en s'appuyant sur le vocabulaire du Glossaire (§3) et les règles métier formalisées dans `documentation/specs.md`. Les exigences techniques de haut niveau (stack, architecture) sont documentées dans `documentation/stack.md` et `documentation/data-model.md` ; ce PRD n'en duplique pas le contenu mais y fait référence.

---

## 1. Vision

Le système de réservation de tables est une application web destinée aux restaurants souhaitant digitaliser et centraliser la gestion de leurs réservations. Il remplace les cahiers de réservation papier et les tableurs fragmentés par une plateforme unique accessible au personnel en salle, aux managers et aux clients en ligne.

Le cœur de l'expérience est la **vue salle en temps réel** : un plan visuel qui montre instantanément l'état de chaque table pour un service donné. Le personnel peut, d'un clic, confirmer une arrivée, noter un no-show ou ouvrir un formulaire de réservation. Les clients, eux, disposent d'un canal en ligne pour réserver à tout moment, avec des règles claires sur les délais et les droits.

Le système gère l'ensemble du cycle de vie d'une réservation — de la création à la complétion — en appliquant automatiquement toutes les règles métier : conflits de tables, capacité du service, plages horaires, droits par rôle. Les notifications asynchrones (confirmation, rappels, annulation restaurant) maintiennent le client informé sans jamais bloquer la transaction principale.

---

## 2. Utilisateurs cibles

### 2.1 Jobs To Be Done

**Client en ligne (`Online`)**
- Réserver une table depuis chez soi, sans appeler le restaurant.
- Connaître instantanément les disponibilités pour une date, un service et un nombre de couverts donnés.
- Annuler ou consulter mes réservations sans friction.

**Personnel en salle (`Staff`)**
- Voir d'un coup d'œil l'état de toutes les tables pour le service en cours.
- Confirmer les arrivées, noter les no-shows et libérer les tables rapidement.
- Créer une réservation pour un client qui appelle ou se présente.
- Assigner ou modifier la table d'une réservation.

**Manager**
- Gérer l'intégralité du flux de réservations y compris les cas complexes (modifications de réservations confirmées, dépassements de délai pour les réservations téléphoniques).
- Lever le blacklist d'un client après analyse.
- Déclarer des jours de fermeture et déclencher les annulations en cascade.

**Administrateur (`Admin`)**
- Configurer les tables (zones, capacités, combinabilité), les services et les plages horaires.
- Accéder à toutes les opérations sans restriction de rôle.

### 2.2 Non-utilisateurs (v1)

- Les systèmes de caisse (POS) — pas d'intégration v1.
- Les plateformes de réservation tierces (TheFork, OpenTable) — pas de connecteur v1.
- Les cuisiniers — les flags `HasAllergyAlert`, `IsCelebration`, `NeedsHighChair` sont visibles en cuisine mais sans interface dédiée v1.

### 2.3 Parcours utilisateur clés

**UJ-1 — Un client réserve une table en ligne pour un dîner en famille.**
- **Persona :** Marie, cliente habituée, 38 ans, réserve depuis son téléphone.
- **État d'entrée :** Non authentifiée, accède au portail de réservation en ligne.
- **Chemin :**
  1. Sélectionne la date (dans 5 jours), le service (Dîner), 4 couverts.
  2. La recherche de disponibilité lui présente les tables disponibles triées par capacité croissante.
  3. Elle choisit la table T3 (6p, Terrasse), saisit ses coordonnées.
  4. Le système valide toutes les règles (RB-001 à RB-006), crée la réservation `Pending`.
  5. Elle reçoit un email de confirmation avec récapitulatif.
- **Climax :** La réservation apparaît dans la vue salle du restaurant côté Staff avec statut jaune (Pending).
- **Résolution :** Marie reçoit un rappel 48h avant. Le Staff confirme manuellement → statut passe à `Confirmed`.
- **Cas limite :** Si Marie est blacklistée (RB-007), le formulaire affiche une erreur explicite et bloque la création.

**UJ-2 — Le Staff gère les arrivées du service de déjeuner.**
- **Persona :** Lucas, serveur, accède à la vue salle sur tablette en salle.
- **État d'entrée :** Authentifié en tant que Staff, vue salle chargée pour le Déjeuner de ce jour.
- **Chemin :**
  1. La vue affiche les tables colorées : vert (libre), bleu (confirmé), jaune (en attente).
  2. Un groupe arrive pour la table T2 (réservation confirmée). Lucas clique sur T2 → détail de la réservation.
  3. Il clique « Asseoir » → statut passe à `Seated` (table devient orange).
  4. Fin du repas : il clique « Compléter » → statut passe à `Completed` (table redevient verte).
  5. SignalR propage le changement à tous les terminaux en temps réel, sans rechargement.
- **Climax :** La table est libérée visible instantanément par toute l'équipe.
- **Résolution :** La table est disponible pour une rotation ou la prochaine réservation.
- **Cas limite :** Si un client ne se présente pas 15 min après `ArrivalTime`, Lucas peut cliquer « No-show » → `NoShowCount` du client est incrémenté (RB-008).

**UJ-3 — Un Manager déclare un jour de fermeture exceptionnel.**
- **Persona :** Sophie, manager, apprend la veille que le restaurant ferme pour travaux.
- **État d'entrée :** Authentifiée Manager, interface d'administration.
- **Chemin :**
  1. Elle accède à « Jours de fermeture » et ajoute la date avec le motif « Travaux réseau ».
  2. Le système annule automatiquement toutes les réservations `Pending` et `Confirmed` sur cette date.
  3. Les clients concernés sont notifiés avec le motif `RestaurantClosed`.
- **Climax :** Zéro réservation active sur la date de fermeture.
- **Résolution :** Sophie peut consulter la liste des annulations déclenchées.

**UJ-4 — Un client VIP réserve 45 jours à l'avance par téléphone.**
- **Persona :** Robert, client VVIP, appelle le restaurant.
- **État d'entrée :** Staff authentifié crée la réservation au nom du client.
- **Chemin :**
  1. Le Staff cherche le client par téléphone → fiche trouvée, VipLevel = VVIP.
  2. Saisit la date (J+45), le service, 2 couverts. Source = `Phone`.
  3. Le système applique RB-005 : horizon 180 jours pour VVIP → validé.
  4. Réservation créée `Pending`, confirmée immédiatement par le Staff.
- **Climax :** Réservation `Confirmed` pour J+45, Robert reçoit un SMS de confirmation.

---

## 3. Glossaire

- **Service** — Plage horaire de restauration identifiée par un nom (Déjeuner, Dîner, Brunch), une heure de début, une heure de fin, une heure limite de réservation (`LastBookingTime`) et un nombre maximum de couverts (`MaxCovers`).
- **Couvert** — Un convive ; unité de comptage des places assises. `GuestsCount` exprime le nombre de couverts d'une réservation.
- **MaxCovers** — Capacité d'accueil maximale du restaurant sur un Service donné. La somme des couverts des réservations actives (`Pending`, `Confirmed`, `Seated`) ne peut la dépasser (RB-003).
- **Créneau d'occupation** — Intervalle `[ArrivalTime, ArrivalTime + DurationMinutes[` pendant lequel une table est bloquée par une réservation.
- **Table** — Surface physique identifiée par un numéro, une capacité, une capacité minimale, une zone et un flag de combinabilité.
- **Zone** — Secteur physique du restaurant : `Salle`, `Terrasse`, `Bar`, `Salon privé`.
- **Réservation (Booking)** — Engagement d'un Client pour occuper une Table sur un Service à une date et une heure données. Cycle de vie : `Pending → Confirmed → Seated → Completed` ou les transitions d'abandon (RB-014).
- **LateCancel** — Annulation intervenant moins de 24 heures avant `ArrivalTime`. Déclenche le flag `LateCancel = true` et, après 2 occurrences, perte du statut VIP (RB-009).
- **NoShow** — Client ne se présentant pas. Déclenche l'incrémentation de `NoShowCount`. À 3 no-shows cumulés, blacklist automatique (RB-007 + RB-008).
- **WalkIn** — Réservation prise le jour même, sans délai minimum appliqué. `BookingDate` doit obligatoirement être aujourd'hui.
- **Fusion de tables** — Regroupement de deux Tables `IsCombinable = true` pour une réservation nécessitant une capacité supérieure à celle de chaque table individuelle. Matérialisée par un `TableLock`.
- **HasAllergyAlert** — Flag informatif positionné lorsque `SpecialRequests` contient « allergie » ou « intolérance » (RB-012).
- **IsCelebration** — Flag informatif positionné lorsque `SpecialRequests` contient « anniversaire », « mariage » ou « fiançailles ».
- **NeedsHighChair** — Flag informatif positionné lorsque `SpecialRequests` contient « chaise bébé » ou « siège enfant ».
- **Rôle** — Niveau d'accès d'un utilisateur : `Online` (client), `Staff`, `Manager`, `Admin`. Détermine les actions autorisées (RB-010).
- **Snapshot** — État instantané de toutes les tables d'un Service à une date donnée, retourné par `GET /api/floor/snapshot`.

---

## 4. Fonctionnalités

### 4.1 Gestion du cycle de vie d'une réservation

**Description :** Fonctionnalité centrale du système. Couvre la création, la modification, l'annulation et la progression de statut d'une réservation. Toutes les règles métier (RB-001 à RB-014) s'appliquent à cette fonctionnalité. Réalise UJ-1, UJ-2, UJ-4.

**Exigences fonctionnelles :**

#### FR-1 : Validation des plages horaires du service

Le système doit rejeter toute réservation dont `ArrivalTime` n'est pas compris dans `[Service.StartTime, Service.LastBookingTime]`.

**Conséquences testables :**
- `ArrivalTime = LastBookingTime` → réservation acceptée.
- `ArrivalTime = LastBookingTime + 1 minute` → rejeté, code `TIME_OUTSIDE_SERVICE`.
- `ArrivalTime < StartTime` → rejeté.

#### FR-2 : Calcul du créneau d'occupation

Le système doit calculer la durée d'occupation implicite à partir du Service (`DurationMinutes`) et bloquer la table pour `[ArrivalTime, ArrivalTime + DurationMinutes[`.

**Conséquences testables :**
- Pour un Déjeuner (120 min), une réservation à 12h00 bloque jusqu'à 14h00.
- Une réservation commençant à 14h00 sur la même table est acceptée (pas de chevauchement).

#### FR-3 : Contrainte de capacité de la table

Le système doit rejeter une réservation si `GuestsCount < Table.MinCapacity` ou `GuestsCount > Table.Capacity`.

**Conséquences testables :**
- `GuestsCount = Table.MinCapacity - 1` → rejeté, code `GUESTS_BELOW_MIN`.
- `GuestsCount = Table.Capacity + 1` → rejeté, code `GUESTS_EXCEED_CAPACITY`.
- `GuestsCount` dans `[MinCapacity, Capacity]` → accepté.

#### FR-4 : Plafond de couverts par service

Le système doit rejeter une réservation si la somme des `GuestsCount` des réservations `Pending + Confirmed + Seated` sur le même Service et la même date dépasse `Service.MaxCovers`.

**Conséquences testables :**
- Ajout d'un couvert portant le total à `MaxCovers + 1` → rejeté, code `SERVICE_FULLY_BOOKED`.
- Les réservations `Cancelled`, `NoShow`, `Completed` ne comptent pas dans le total.

#### FR-5 : Détection des conflits de table

Le système doit rejeter une réservation si le créneau d'occupation chevauche celui d'une réservation existante (`Pending`, `Confirmed` ou `Seated`) sur la même table à la même date.

**Conséquences testables :**
- Deux réservations dos à dos (`A.EndTime == B.ArrivalTime`) → acceptées.
- Un chevauchement d'une seule minute → rejeté, code `TABLE_CONFLICT`.

#### FR-6 : Horizon de réservation selon le canal

Le système doit rejeter les réservations créées au-delà de l'horizon autorisé selon la combinaison `Source × VipLevel` :

| Source    | VipLevel None/Regular | VipLevel VIP/VVIP |
|-----------|----------------------|-------------------|
| Online    | 30 jours             | 180 jours         |
| Phone     | 90 jours             | 180 jours         |
| Staff     | 90 jours             | 180 jours         |
| WalkIn    | Aujourd'hui seulement| Aujourd'hui       |

**Conséquences testables :**
- Réservation en ligne à J+31 pour client Standard → rejetée, code `HORIZON_EXCEEDED`.
- Réservation en ligne à J+31 pour client VIP → acceptée.
- WalkIn pour demain → rejeté, code `WALKIN_MUST_BE_TODAY`.

#### FR-7 : Délai minimum avant le service

Le système doit rejeter une réservation en ligne créée moins de 2 heures avant `ArrivalTime` le jour J. Pour `Phone` et `Staff`, le délai est de 15 minutes. Les WalkIn ignorent cette règle.

**Conséquences testables :**
- Réservation Online créée à J, H-1h45 → rejetée, code `MIN_LEAD_TIME_VIOLATED`.
- Réservation Phone créée à J, H-10min → rejetée.
- WalkIn créé à J immédiatement → accepté.

#### FR-8 : Interdiction de réserver dans le passé

Le système doit rejeter toute réservation avec `BookingDate < today`.

**Conséquences testables :**
- `BookingDate = yesterday` → rejeté, code `BOOKING_DATE_IN_PAST`.

#### FR-9 : Gestion des clients blacklistés

Un client avec `IsBlacklisted = true` ne peut créer aucune réservation quel que soit le canal.

**Conséquences testables :**
- Tentative de création par un client blacklisté → rejetée, code `CUSTOMER_BLACKLISTED`.
- Un client blacklisté peut consulter ses réservations passées (lecture seule).

#### FR-10 : Blacklist automatique après 3 no-shows

Lorsqu'un Staff marque une réservation `NoShow`, le système incrémente `Customer.NoShowCount`. Si `NoShowCount` atteint 3, le système positionne automatiquement `IsBlacklisted = true`.

**Conséquences testables :**
- 3e no-show → `IsBlacklisted = true` sans action manuelle supplémentaire.
- La transition `NoShow` n'est autorisée que depuis `Confirmed` et uniquement si l'heure actuelle ≥ `ArrivalTime + 15 min`.

#### FR-11 : Règles d'annulation

Une réservation peut être annulée uniquement depuis `Pending` ou `Confirmed`. `CancellationReason` est obligatoire. Si l'annulation intervient moins de 24h avant `ArrivalTime`, `LateCancel = true` est positionné. Après 2 `LateCancel`, `Customer.VipLevel` est réinitialisé à `None`.

**Conséquences testables :**
- Annulation d'une réservation `Seated` → rejetée, code `CANNOT_CANCEL_SEATED`.
- Annulation sans `CancellationReason` → rejetée (validation 400).
- 2e `LateCancel` → `VipLevel = None` automatiquement.

#### FR-12 : Transitions de statut autorisées

Le système n'autorise que les transitions suivantes :

```
Pending   → Confirmed   (Staff, Manager, Admin)
Pending   → Cancelled   (client propriétaire, Staff, Manager, Admin)
Pending   → Rejected    (Staff, Manager, Admin)
Confirmed → Seated      (Staff, Manager, Admin)
Confirmed → Cancelled   (RB-009 + RB-010)
Confirmed → NoShow      (Staff, Manager, Admin — après ArrivalTime + 15 min)
Seated    → Completed   (Staff, Manager, Admin)
```

**Conséquences testables :**
- `Cancelled → Confirmed` → rejeté, code `INVALID_STATUS_TRANSITION`.
- `Completed → Seated` → rejeté.
- Toute transition non listée → rejetée.

#### FR-13 : Modification d'une réservation

Date, heure et nombre de couverts peuvent être modifiés depuis `Pending` ou `Confirmed` (Manager/Admin uniquement pour `Confirmed`). Toute modification revalide l'ensemble des règles métier. Modifier la date ou l'heure d'une réservation `Confirmed` la repasse à `Pending`.

**Conséquences testables :**
- Client Online modifie une réservation `Confirmed` → rejeté, code `INSUFFICIENT_ROLE`.
- Modification de date d'une réservation `Confirmed` → statut repasse à `Pending`.
- La table peut être changée uniquement par Staff, Manager ou Admin.

#### FR-14 : Droits par rôle

| Action                            | Online           | Staff | Manager | Admin |
|-----------------------------------|------------------|-------|---------|-------|
| Créer une réservation             | Oui (ses propres)| Oui   | Oui     | Oui   |
| Confirmer / Rejeter               | Non              | Oui   | Oui     | Oui   |
| Modifier (Pending)                | Oui (ses propres)| Oui   | Oui     | Oui   |
| Modifier (Confirmed)              | Non              | Non   | Oui     | Oui   |
| Annuler                           | Oui (ses propres)| Oui   | Oui     | Oui   |
| Lever un blacklist                | Non              | Oui   | Oui     | Oui   |
| Réserver au-delà de 30 jours     | Non (sauf VIP)   | Non   | Oui     | Oui   |
| Assigner / changer une table      | Non              | Oui   | Oui     | Oui   |

**Conséquences testables :**
- Client Online tente d'annuler la réservation d'un autre client → rejeté, code `FORBIDDEN`.

---

### 4.2 Vue salle en temps réel (Floor Plan)

**Description :** Composant central de l'interface Staff. Affiche l'état de chaque table sous forme de cartes colorées, mises à jour en temps réel via SignalR. Réalise UJ-2.

**Exigences fonctionnelles :**

#### FR-15 : Snapshot de la salle

Le système doit retourner, pour une date et un Service donnés, l'état de toutes les tables via `GET /api/floor/snapshot`. Chaque table inclut son statut courant, la réservation active si applicable (client, couverts, `ArrivalTime`), et les flags informatifs.

**Conséquences testables :**
- La réponse contient toutes les tables actives groupées par Zone.
- Les tables inactives (`IsActive = false`) n'apparaissent pas.

#### FR-16 : Codes couleur des statuts

| Statut table | Couleur         |
|--------------|-----------------|
| Libre        | Vert `#22c55e`  |
| Réservée     | Bleu `#3b82f6`  |
| En attente   | Jaune `#eab308` |
| Occupée      | Orange `#f97316`|
| No-show      | Rouge `#ef4444` |
| Inactive     | Gris `#9ca3af`  |

#### FR-17 : Interactions sur la vue salle

- Clic sur une table **libre** → ouvre le formulaire de réservation pré-rempli avec cette table.
- Clic sur une table **réservée** ou **en attente** → ouvre le détail de la réservation.
- Clic sur une table **occupée** → permet de déclencher la complétion (`Completed`).
- Hover → tooltip : nom du client, couverts, heure d'arrivée.

#### FR-18 : Mises à jour temps réel via SignalR

Le système doit pousser les événements suivants sur le hub `/hubs/floor` sans rechargement de page :
- `TableStatusChanged { tableId, status, bookingId }` — à chaque changement de statut.
- `BookingUpdated { bookingId, newStatus }` — à chaque transition de réservation.
- `ServiceCapacityChanged { serviceId, date, remainingCovers }` — à chaque variation des couverts.

**Conséquences testables :**
- Une transition `Seated` sur le terminal A se reflète sur le terminal B en moins de 2 secondes.

---

### 4.3 Recherche de disponibilité

**Description :** Permet à un client ou au Staff de trouver les tables disponibles avant de créer une réservation. Réalise UJ-1, UJ-4.

**Exigences fonctionnelles :**

#### FR-19 : Requête de disponibilité

`GET /api/tables/availability?date=&serviceId=&guestsCount=&zone=` doit retourner les tables disponibles correspondant à `guestsCount ∈ [Table.MinCapacity, Table.Capacity]`, triées par capacité croissante. Si `MaxCovers` est atteint pour le service, retourne une liste vide avec le motif `ServiceFullyBooked`.

**Conséquences testables :**
- Les tables inactives n'apparaissent jamais dans les résultats.
- Les tables fusionnables sont proposées en dernier recours si aucune table individuelle ne convient.
- Le filtre `zone` est optionnel ; si absent, toutes les zones sont retournées.

---

### 4.4 Fusion de tables

**Description :** Permet de regrouper deux tables combinables pour accommoder un groupe dont la taille dépasse toute table individuelle. Réalise UJ-4.

**Exigences fonctionnelles :**

#### FR-20 : Création d'une fusion

Deux tables peuvent être fusionnées si `IsCombinable = true` pour les deux, et si les deux tables sont libres sur le créneau demandé. La capacité combinée = somme des capacités individuelles. La réservation est liée à la table principale (`TableId`). Un `TableLock` est créé pour verrouiller la table secondaire.

**Conséquences testables :**
- Fusion d'une table `IsCombinable = false` avec une autre → rejetée, code `TABLE_NOT_COMBINABLE`.
- Si l'une des deux tables est occupée sur le créneau → rejetée, code `TABLE_CONFLICT`.

#### FR-21 : Séparation d'une fusion

La séparation n'est possible que si `Status ∈ { Pending, Confirmed }`. Le `TableLock` est supprimé.

---

### 4.5 Demandes spéciales et flags informatifs

**Description :** Permet aux clients de préciser leurs besoins ; le système positionne des flags lisibles en cuisine. Réalise UJ-1.

**Exigences fonctionnelles :**

#### FR-22 : Validation des demandes spéciales

`SpecialRequests` est optionnel mais limité à 500 caractères.

**Conséquences testables :**
- `SpecialRequests` de 501 caractères → rejeté, code `SPECIAL_REQUESTS_TOO_LONG`.

#### FR-23 : Détection automatique des flags

Le système analyse `SpecialRequests` à la sauvegarde et positionne :
- `HasAllergyAlert = true` si le texte contient « allergie » ou « intolérance » (insensible à la casse).
- `IsCelebration = true` si le texte contient « anniversaire », « mariage » ou « fiançailles ».
- `NeedsHighChair = true` si le texte contient « chaise bébé » ou « siège enfant ».

**Conséquences testables :**
- `SpecialRequests = "Allergie aux arachides"` → `HasAllergyAlert = true`.
- Ces flags sont informatifs et ne bloquent pas la création de la réservation.

---

### 4.6 Notifications client

**Description :** Informe le client par SMS et/ou email à chaque étape clé. Les notifications sont asynchrones et n'impactent pas la transaction principale. Réalise UJ-1, UJ-3.

**Exigences fonctionnelles :**

#### FR-24 : Notifications déclenchées

| Événement                   | Canal        | Délai      |
|-----------------------------|--------------|------------|
| Confirmation de réservation | Email + SMS  | Immédiat   |
| Rappel avant le repas       | Email + SMS  | J-48h      |
| Rappel final                | SMS seulement| J-2h       |
| Annulation par le restaurant| Email + SMS  | Immédiat   |

**Conséquences testables :**
- L'échec d'envoi d'une notification ne fait pas échouer la transaction principale.
- Si le client n'a ni email ni téléphone valide, la notification est ignorée silencieusement.

---

### 4.7 Gestion des jours de fermeture

**Description :** Permet à un Manager ou Admin de déclarer des jours de fermeture, déclenchant l'annulation automatique des réservations existantes. Réalise UJ-3.

**Exigences fonctionnelles :**

#### FR-25 : Déclaration d'un jour de fermeture

Un Manager ou Admin peut créer un `ClosedDay` avec une date et un motif. Aucune réservation ne peut être créée sur une date de fermeture.

**Conséquences testables :**
- Tentative de réservation sur un `ClosedDay` → rejetée, code `RESTAURANT_CLOSED`.

#### FR-26 : Annulation en cascade

Lors de la déclaration d'un `ClosedDay`, toutes les réservations `Pending` et `Confirmed` sur cette date sont automatiquement annulées avec le motif `RestaurantClosed`. Les clients sont notifiés.

**Conséquences testables :**
- Après déclaration d'un `ClosedDay`, zéro réservation `Pending` ou `Confirmed` sur cette date.
- Les réservations `Seated` ou `Completed` (cas théorique) ne sont pas annulées.

---

### 4.8 Gestion des clients

**Description :** Recherche, création et consultation des fiches client. Réalise UJ-1, UJ-2, UJ-4.

**Exigences fonctionnelles :**

#### FR-27 : Recherche de client

Le Staff peut rechercher un client par numéro de téléphone ou email. La recherche retourne la fiche client avec `NoShowCount`, `VipLevel`, `IsBlacklisted`.

#### FR-28 : Lever un blacklist

Un Staff, Manager ou Admin peut remettre `IsBlacklisted = false` sur un client. Cette action est journalisée.

**Conséquences testables :**
- Un client Online ne peut pas lever son propre blacklist.

---

### 4.9 Configuration des tables et services (Admin)

**Description :** Permet à un Admin de gérer les tables et les services. Non accessible aux autres rôles.

**Exigences fonctionnelles :**

#### FR-29 : Gestion des tables

Un Admin peut créer, modifier (capacité, zone, `IsCombinable`, `IsActive`) et désactiver des tables. Une table désactivée disparaît des résultats de disponibilité.

#### FR-30 : Gestion des services

Un Admin peut créer et modifier les Services (plages horaires, `MaxCovers`, `DurationMinutes`). La modification d'un Service ne rétroagit pas sur les réservations existantes.

---

## 5. Non-objectifs (v1 explicites)

- Intégration avec des plateformes de réservation tierces (TheFork, OpenTable, etc.).
- Module de caisse / facturation (POS).
- Interface dédiée pour la cuisine (les flags sont lisibles mais sans tableau de bord cuisinier).
- Réservations récurrentes (champ `recurrence_group_id` présent en base mais non exposé en v1).
- Application mobile native (iOS / Android) — la SPA Angular est responsive.
- Gestion multi-restaurant (un seul établissement par instance).
- Système de paiement en ligne ou d'empreinte carte pour garantie.
- Programme de fidélité (les niveaux VIP sont gérés manuellement par le Staff).

---

## 6. Périmètre MVP

### 6.1 Dans le périmètre

- Création, modification, annulation et progression de statut des réservations (RB-001 à RB-014).
- Vue salle en temps réel avec SignalR (UJ-2).
- Recherche de disponibilité avec filtres date / service / couverts / zone.
- Fusion de tables (UJ-4).
- Gestion des clients (recherche, création, blacklist).
- Notifications asynchrones (confirmation, rappels 48h et 2h, annulation).
- Gestion des jours de fermeture avec annulation en cascade.
- Configuration des tables et services par l'Admin.
- Authentification et autorisation par rôle (Online, Staff, Manager, Admin).

### 6.2 Hors périmètre MVP

- Intégrations tierces (POS, TheFork…) — déféré à v2.
- Interface cuisine dédiée — déféré à v2 `[NOTE FOR PM : à réévaluer si les cuisiniers expriment le besoin]`.
- Réservations récurrentes — infrastructure en base mais logique déféré à v2.
- Application mobile native — déféré selon adoption de la SPA.
- Gestion multi-restaurant — architecture mono-instance en v1.

---

## 7. Métriques de succès

**Primaires**

- **SM-1 :** Taux d'occupation des tables ≥ 85 % sur les services actifs, mesuré sur 30 jours glissants. Valide FR-19 (disponibilité), FR-3, FR-4.
- **SM-2 :** Taux de no-shows ≤ 5 % (vs baseline secteur ~10 %). Valide FR-10, FR-11 (blacklist auto), FR-24 (rappels).
- **SM-3 :** Délai de prise en charge d'une arrivée (clic « Asseoir ») ≤ 10 secondes sur la vue salle. Valide FR-17, FR-18.

**Secondaires**

- **SM-4 :** Propagation des événements SignalR ≤ 2 secondes (P95) entre la mutation et l'affichage sur un second terminal. Valide FR-18.
- **SM-5 :** Taux de complétion du formulaire de réservation en ligne ≥ 70 %. Valide UJ-1.
- **SM-6 :** Zéro perte de réservation due à un conflit de table non détecté en production. Valide FR-5.

**Contre-métriques (à ne pas optimiser)**

- **SM-C1 :** Taux de faux positifs de blacklist (clients blacklistés par erreur) — si SM-2 baisse par sur-blacklistage, c'est un signal d'alarme. Contrebalance SM-2.
- **SM-C2 :** Temps de chargement du Snapshot — ne pas sacrifier la fraîcheur des données pour optimiser SM-3.

---

## 8. Exigences non-fonctionnelles transversales

### Performance

- `GET /api/floor/snapshot` : P95 ≤ 200 ms pour un service avec jusqu'à 50 tables.
- `GET /api/tables/availability` : P95 ≤ 300 ms.
- `POST /api/bookings` (création avec toutes les validations) : P95 ≤ 500 ms.
- Interface Angular : First Contentful Paint ≤ 2 s sur connexion 4G.

### Disponibilité et fiabilité

- Disponibilité cible : 99,5 % (service restaurant — heures d'ouverture critiques).
- Les notifications asynchrones sont découplées : leur défaillance n'impacte pas les transactions de réservation.
- Les changements de statut via SignalR doivent résister aux déconnexions (reconnexion automatique du client SignalR, rechargement du snapshot à la reconnexion).

### Sécurité

- Authentification par JWT (durée de session configurable).
- Autorisation par rôle vérifiée côté API à chaque endpoint (pas de trust côté client).
- Les données client (email, téléphone) sont accessibles uniquement aux rôles Staff et supérieurs.
- Protection CSRF sur toutes les mutations.
- Logs d'audit sur : lever de blacklist, déclaration de fermeture, transitions de statut.

### Observabilité

- Logging structuré JSON via Serilog avec corrélation des requêtes.
- Traces distribuées sur les handlers MediatR (création, modification, transitions).
- Métriques : taux de rejet par règle métier (RB-001 à RB-017) pour détecter les abus ou les bugs de configuration.

### Accessibilité

- La SPA Angular respecte WCAG 2.1 niveau AA pour les composants standard (formulaires, navigation, messages d'erreur).
- La vue salle ne repose pas uniquement sur la couleur pour communiquer le statut (label textuel ou icône complémentaire).

---

## 9. Questions ouvertes

1. **Authentification client en ligne :** le PRD suppose une authentification (email + mot de passe ou OTP SMS). Le flux exact d'inscription / connexion n'est pas spécifié dans les sources. `[À confirmer avec le PM]`
2. **Nombre de tables en production :** les métriques de performance sont calibrées sur 50 tables. Si l'établissement en a davantage, les index doivent être réévalués. `[À confirmer avec le client]`
3. **Stratégie de déploiement :** SaaS multi-tenant ou instance dédiée par restaurant ? L'architecture actuelle est mono-instance. `[À décider avant le démarrage de l'infrastructure]`
4. **Canal SMS :** quel provider SMS (Twilio, AWS SNS, autre) ? Le choix impacte la couche Infrastructure. `[À décider en phase d'architecture]`
5. **RGPD / retention des données :** quelle durée de conservation des données client et des réservations ? Le soft delete est en place mais la politique d'expiration n'est pas définie. `[À définir avec le DPO]`
6. **Interface cuisine :** les flags `HasAllergyAlert`, `IsCelebration`, `NeedsHighChair` sont-ils suffisants en v1 ou faut-il un écran en cuisine dès le MVP ? `[À valider avec le client]`

---

## 10. Index des hypothèses

- `[ASSUMPTION §4.8]` La levée du blacklist par le Staff est journalisée mais ne requiert pas de validation hiérarchique (Manager ou Admin) — comportement dérivé de RB-007 / RB-010.
- `[ASSUMPTION §4.6]` Le système de notification utilise un canal email ET SMS distincts ; la défaillance d'un canal n'empêche pas l'envoi sur l'autre.
- `[ASSUMPTION §6.1]` L'authentification (inscription, connexion, gestion de session) est incluse dans le périmètre MVP mais n'est pas détaillée dans ce PRD — elle fera l'objet d'une story dédiée.
- `[ASSUMPTION §7]` Les métriques de succès sont mesurées sur l'environnement de production après la mise en ligne initiale ; elles ne s'appliquent pas aux environnements de staging.
- `[ASSUMPTION §8]` La SPA Angular est accessible depuis un navigateur desktop et tablette. Le support mobile n'est pas optimisé en v1 mais la SPA est responsive.
