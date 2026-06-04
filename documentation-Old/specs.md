# Système de Réservation de Tables — Spécifications Métier

## Contexte

Application de gestion de réservations de tables dans un restaurant.  
Les réservations sont faites par des **clients** (en ligne ou via le personnel).  
Les tables ont des **capacités**, des **zones** (terrasse, salle, bar), et le restaurant a des **services** (midi, soir) avec leurs plages horaires.

---

## Entités principales

### Table (`Table`)
| Champ          | Type       | Contrainte                                      |
|----------------|------------|-------------------------------------------------|
| Id             | Guid       | Unique                                          |
| Number         | int        | Numéro affiché en salle, unique                 |
| Capacity       | int        | 1–20 personnes                                  |
| MinCapacity    | int        | >= 1, <= Capacity                               |
| Zone           | enum       | `Salle`, `Terrasse`, `Bar`, `Salon privé`       |
| IsActive       | bool       |                                                 |
| IsCombinable   | bool       | La table peut être fusionnée avec une voisine   |

### Client (`Customer`)
| Champ          | Type       | Contrainte                                      |
|----------------|------------|-------------------------------------------------|
| Id             | Guid       | Unique                                          |
| FirstName      | string     | 1–100 caractères                                |
| LastName       | string     | 1–100 caractères                                |
| Phone          | string     | Format valide, unique                           |
| Email          | string?    | Format valide si renseigné                      |
| IsBlacklisted  | bool       |                                                 |
| NoShowCount    | int        | Nombre de no-shows cumulés                      |
| VipLevel       | enum       | `None`, `Regular`, `VIP`, `VVIP`                |

### Service (`DiningService`)
| Champ          | Type     | Contrainte                                      |
|----------------|----------|-------------------------------------------------|
| Id             | Guid     | Unique                                          |
| Name           | string   | Ex. "Déjeuner", "Dîner", "Brunch"               |
| StartTime      | TimeOnly | 00:00–23:59                                     |
| EndTime        | TimeOnly | > StartTime                                     |
| LastBookingTime| TimeOnly | <= EndTime, heure limite pour réserver          |
| MaxCovers      | int      | Nombre max de couverts simultanés               |
| IsActive       | bool     |                                                 |

### Réservation (`Booking`)
| Champ              | Type           | Contrainte                                           |
|--------------------|----------------|------------------------------------------------------|
| Id                 | Guid           | Unique                                               |
| TableId            | Guid?          | FK Table — nullable si table assignée après          |
| CustomerId         | Guid           | FK Customer                                          |
| ServiceId          | Guid           | FK DiningService                                     |
| BookingDate        | DateOnly       | Date du repas                                        |
| ArrivalTime        | TimeOnly       | Heure d'arrivée prévue, dans le service              |
| GuestsCount        | int            | >= 1                                                 |
| Status             | enum           | `Pending`, `Confirmed`, `Seated`, `Completed`, `Cancelled`, `NoShow` |
| SpecialRequests    | string?        | Allergies, chaise bébé, anniversaire…                |
| CreatedAt          | DateTimeOffset |                                                      |
| CancellationReason | string?        | Obligatoire si Status = `Cancelled`                  |
| Source             | enum           | `Online`, `Phone`, `WalkIn`, `Staff`                 |

---

## Règles métier

### RB-001 — Plages horaires du service

- L'`ArrivalTime` doit être **comprise dans** `[StartTime, LastBookingTime]` du service sélectionné.
  - Exemple : service dîner 19:00–23:00, dernière résa 21:30 → arrivée à 21:45 refusée.
- La durée implicite d'occupation d'une table est fixée à **2 heures** (déjeuner) ou **2h30** (dîner) selon le service.
  - La table est donc bloquée de `ArrivalTime` à `ArrivalTime + durée_service`.
- Une réservation ne peut **pas chevaucher deux services** distincts.

### RB-002 — Capacité de la table

- `GuestsCount` doit être **≤ Table.Capacity**.
- `GuestsCount` doit être **≥ Table.MinCapacity**.
  - Exemple : une table de 4 avec MinCapacity = 2 refuse 1 couvert (table sous-exploitée).
- Un dépassement ou un sous-remplissage hors limites est refusé même sans autre conflit.

### RB-003 — Capacité globale du service (couverts max)

- La somme des `GuestsCount` des réservations `Confirmed` + `Seated` + `Pending` sur un même service et une même date ne peut pas dépasser `DiningService.MaxCovers`.
- Si l'ajout d'une réservation dépasse le plafond, elle est refusée avec le motif `ServiceFullyBooked`.
- Les réservations `Cancelled`, `NoShow` et `Completed` **ne comptent pas** dans le total.

### RB-004 — Conflits de table

- Une table ne peut pas avoir deux réservations dont les créneaux d'occupation se chevauchent sur la même date.
- Les créneaux sont `[ArrivalTime, ArrivalTime + durée_service[`.
- `A.EndTime == B.ArrivalTime` → **pas** de conflit (rotation de table autorisée).
- Seules les réservations avec `Status ∈ { Pending, Confirmed, Seated }` bloquent la table.

### RB-005 — Horizon de réservation

- Une réservation ne peut pas être créée pour une date **passée** (`BookingDate < today`).
- Les réservations en ligne (`Source = Online`) sont acceptées jusqu'à **30 jours à l'avance**.
- Les réservations par téléphone ou en personne (`Phone`, `WalkIn`, `Staff`) sont acceptées jusqu'à **90 jours à l'avance**.
- Les clients `VIP` et `VVIP` bénéficient d'un horizon étendu à **180 jours**, quel que soit le canal.
- Les réservations `WalkIn` doivent avoir `BookingDate = today`.

### RB-006 — Délai minimum avant le service

- Une réservation en ligne ne peut pas être créée **moins de 2 heures** avant `ArrivalTime` le jour J.
- Une réservation `Phone` ou `Staff` peut être créée jusqu'à **15 minutes** avant `ArrivalTime`.
- Les `WalkIn` ignorent cette règle (arrivée immédiate).

### RB-007 — Client blacklisté / no-show répété

- Un client avec `IsBlacklisted = true` **ne peut pas** créer de réservation (tous canaux).
- Un client avec `NoShowCount >= 3` est automatiquement **blacklisté** (`IsBlacklisted = true`).
- Un client blacklisté peut toujours **consulter** ses réservations passées.
- Seul un `Staff` ou `Admin` peut lever le blacklist (`IsBlacklisted = false`).

### RB-008 — No-show

- Si un client ne se présente pas et que le restaurant marque la réservation `NoShow` :
  - `Customer.NoShowCount` est incrémenté de 1.
  - Si `NoShowCount` atteint 3, la règle RB-007 s'applique automatiquement.
- La transition vers `NoShow` n'est possible que depuis `Confirmed` et uniquement après `ArrivalTime + 15 minutes`.

### RB-009 — Annulation

- Une réservation peut être annulée si `Status ∈ { Pending, Confirmed }`.
- L'annulation est **impossible** si `Status = Seated` (le client est déjà à table).
- **Annulation tardive** : si l'annulation intervient moins de **24 heures** avant `ArrivalTime`, le flag `LateCancel = true` est positionné.
- `CancellationReason` est obligatoire.
- Après 2 annulations tardives, le client est automatiquement passé en `VipLevel = None` (perte des avantages).

### RB-010 — Droits par rôle du personnel

| Action                       | `Online` (client) | `Staff`       | `Manager` | `Admin` |
|------------------------------|-------------------|---------------|-----------|---------|
| Créer réservation            | Oui (ses propres) | Oui           | Oui       | Oui     |
| Confirmer / Rejeter          | Non               | Oui           | Oui       | Oui     |
| Modifier une réservation     | Oui (Pending)     | Oui (Pending) | Oui       | Oui     |
| Annuler                      | Oui (ses propres) | Oui           | Oui       | Oui     |
| Lever un blacklist           | Non               | Oui           | Oui       | Oui     |
| Réserver au-delà de 30j      | Non (sauf VIP)    | Non           | Oui       | Oui     |
| Assigner / changer une table | Non               | Oui           | Oui       | Oui     |

### RB-011 — Fusion de tables

- Deux tables peuvent être fusionnées si `IsCombinable = true` pour les deux.
- La capacité combinée = somme des capacités individuelles.
- Une fusion n'est possible que si les deux tables sont **libres** sur le même créneau.
- Une réservation sur une table fusionnée est liée à la table principale (`TableId`) ; la table secondaire est verrouillée via un `TableLock`.
- La séparation des tables n'est possible que si `Status ∈ { Pending, Confirmed }`.

### RB-012 — Demandes spéciales

- Les demandes spéciales (`SpecialRequests`) sont **optionnelles** mais limitées à **500 caractères**.
- Certaines demandes déclenchent un flag interne :
  - Mention "allergie" ou "intolérance" → flag `HasAllergyAlert = true` (visible en cuisine).
  - Mention "anniversaire", "mariage", "fiançailles" → flag `IsCelebration = true`.
  - Mention "chaise bébé" ou "siège enfant" → flag `NeedsHighChair = true`.
- Ces flags sont informatifs et ne bloquent pas la réservation.

### RB-013 — Modification d'une réservation

- La date, l'heure et le nombre de couverts peuvent être modifiés si `Status ∈ { Pending, Confirmed }`.
  - Exception : un `Manager` ou `Admin` peut modifier une réservation `Confirmed`.
- Toute modification revalide **toutes** les règles (conflits, couverts max, capacité, horaires).
- La table assignée peut être changée uniquement par `Staff`, `Manager` ou `Admin`.
- Modifier la date ou l'heure réinitialise le statut à `Pending` si la réservation était `Confirmed`.

### RB-014 — Transition de statuts

Les seules transitions autorisées sont :

```
Pending   → Confirmed   (Staff, Manager, Admin)
Pending   → Cancelled   (client propriétaire, Staff, Manager, Admin)
Pending   → Rejected    (Staff, Manager, Admin)
Confirmed → Seated      (Staff, Manager, Admin — client arrivé)
Confirmed → Cancelled   (selon RB-009 et RB-010)
Confirmed → NoShow      (Staff, Manager, Admin — après ArrivalTime + 15 min)
Seated    → Completed   (Staff, Manager, Admin — fin du repas)
```

Toute autre transition (ex. `Cancelled → Confirmed`, `Completed → Seated`) est **refusée**.

### RB-015 — Notifications client

- À la **confirmation** : SMS et/ou email de confirmation avec récapitulatif.
- **48h avant** le repas : rappel avec lien d'annulation.
- **2h avant** le repas : rappel final (SMS uniquement).
- En cas d'**annulation par le restaurant** : notification avec la raison et proposition de report.
- Les notifications sont **asynchrones** — leur échec ne bloque pas la transaction métier.
- Si le client n'a ni email ni téléphone valide, la notification est silencieusement ignorée.

### RB-016 — Recherche de disponibilité

- La recherche prend en entrée : `date`, `serviceId`, `guestsCount`, `zone?`.
- Elle retourne les tables disponibles correspondant à `guestsCount ∈ [Table.MinCapacity, Table.Capacity]`.
- Les tables sont triées par **capacité croissante** (proposer la table la mieux adaptée en premier).
- Une table inactive n'apparaît **jamais** dans les résultats.
- Si `MaxCovers` est atteint pour le service, la recherche retourne une liste vide avec le motif `ServiceFullyBooked`.
- Les tables fusionnables sont proposées en dernier recours si aucune table individuelle ne convient.

### RB-017 — Gestion des jours de fermeture

- Le restaurant peut déclarer des **jours de fermeture** (`ClosedDay`) avec une date et un motif.
- Aucune réservation n'est acceptée sur un jour de fermeture.
- Les réservations `Confirmed` ou `Pending` existantes sur un jour nouvellement déclaré fermé sont **automatiquement annulées** avec le motif `RestaurantClosed` et les clients sont notifiés.

---

## Cas limites documentés (priorité TDD)

| Cas                                                                        | Règle(s)              |
|----------------------------------------------------------------------------|-----------------------|
| ArrivalTime = LastBookingTime + 1 min                                      | RB-001                |
| GuestsCount = Table.MinCapacity - 1                                        | RB-002                |
| GuestsCount = Table.Capacity + 1                                           | RB-002                |
| Ajout d'un couvert qui dépasse MaxCovers du service                        | RB-003                |
| Deux réservations dos à dos sur la même table (fin == début suivant)       | RB-004                |
| Réservation en ligne pour dans 31 jours (client Standard)                  | RB-005                |
| Réservation en ligne pour dans 31 jours (client VIP)                       | RB-005                |
| WalkIn avec BookingDate = demain                                            | RB-005                |
| Réservation en ligne 1h45 avant ArrivalTime                                | RB-006                |
| Réservation Phone 10 minutes avant ArrivalTime                             | RB-006                |
| Création par un client blacklisté                                           | RB-007                |
| 3e no-show → blacklist automatique                                          | RB-007 + RB-008       |
| Transition NoShow depuis Confirmed avant ArrivalTime + 15 min              | RB-008                |
| Annulation d'une réservation Seated                                        | RB-009                |
| 2e LateCancel → perte du statut VIP                                        | RB-009                |
| Client Online tente d'annuler la réservation d'un autre client             | RB-010                |
| Fusion de deux tables dont une seule est IsCombinable                      | RB-011                |
| SpecialRequests contenant "allergie" → HasAllergyAlert = true              | RB-012                |
| SpecialRequests de 501 caractères                                          | RB-012                |
| Modification d'une réservation Confirmed par un client Online              | RB-013                |
| Modification de date → statut Confirmed repasse à Pending                  | RB-013                |
| Transition Cancelled → Confirmed                                           | RB-014                |
| Transition Seated → Cancelled                                              | RB-014                |
| Réservation sur un jour de fermeture                                        | RB-017                |
| Déclaration d'un jour de fermeture avec réservations Confirmed existantes  | RB-017                |

---

## Glossaire

| Terme               | Définition                                                              |
|---------------------|-------------------------------------------------------------------------|
| Service             | Plage horaire de restauration (déjeuner, dîner, brunch…)               |
| Couvert             | Un convive — unité de comptage des places                              |
| MaxCovers           | Capacité d'accueil maximale du restaurant sur un service               |
| Créneau d'occupation| `[ArrivalTime, ArrivalTime + durée_service[` pour une table            |
| LateCancel          | Annulation moins de 24h avant l'heure d'arrivée prévue                |
| NoShow              | Client qui ne se présente pas sans annuler                             |
| WalkIn              | Réservation prise le jour même sans délai minimum                      |
| Fusion de tables    | Regroupement physique de deux tables combinables pour un grand groupe  |
| HasAllergyAlert     | Flag informatif indiquant une allergie/intolérance déclarée            |
| IsCelebration       | Flag informatif indiquant un événement festif                          |
