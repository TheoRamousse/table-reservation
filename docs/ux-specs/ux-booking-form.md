---
screen: BookingFormComponent
epic: 4
story: 4.2
status: ready-for-implementation
author: Sally (UX Designer)
date: 2026-06-04
---

# UX Spec — Formulaire de Réservation (BookingFormComponent)

---

## 1. Contexte utilisateur

**Personas & modes :**

| Persona | Mode | Point d'entrée |
|---------|------|---------------|
| Marie (UJ-1) — cliente online | **Mode client** | `/availability` → sélection d'une table |
| Lucas (UJ-2) — Staff | **Mode staff** | Clic sur table libre dans la vue salle |
| Robert (UJ-4) — VVIP par téléphone | **Mode staff (phone)** | Staff crée pour le client |

Le composant s'adapte au rôle de l'utilisateur connecté :
- **Rôle Online** → mode client (ses propres coordonnées, source = `Online`).
- **Rôle Staff/Manager/Admin** → mode staff (recherche de client, source = `Phone` ou `Staff`).

---

## 2. Points d'entrée et pré-remplissage

| Point d'entrée | Champs pré-remplis | Source |
|---------------|-------------------|--------|
| Clic table libre (FloorPlan) | tableId, date, serviceId | `Staff` |
| Sélection depuis AvailabilitySearch | tableId, date, serviceId, guestsCount | `Online` |
| URL directe `/bookings/new` | rien | selon rôle |

Si les paramètres de pré-remplissage sont absents, tous les champs sont vides et
l'utilisateur doit les renseigner manuellement.

---

## 3. Layout — Slide Panel (Staff, depuis Vue Salle)

Le formulaire s'ouvre dans un **right drawer Angular Material** (`mat-drawer`) depuis
la vue salle. Largeur : `w-full sm:w-[480px]`. Fond blanc, scroll interne.

```
┌────────────────────────────────────────────────────┐
│ ←  Nouvelle réservation                       [✕] │  ← drawer header sticky
├────────────────────────────────────────────────────┤
│                                                    │
│  CLIENT                                            │  ← section header
│  ──────────────────────────────────────────────    │
│  [ 🔍 Rechercher par téléphone ou email… ]         │  ← Staff uniquement
│                                                    │
│  Prénom *          Nom *                           │
│  [____________]    [____________]                  │
│                                                    │
│  Email             Téléphone                       │
│  [____________]    [____________]                  │
│                                                    │
│  RÉSERVATION                                       │
│  ──────────────────────────────────────────────    │
│  Date *                 Service *                  │
│  [  08/06/2026  ]       [ Déjeuner ▾ ]            │
│                                                    │
│  Heure d'arrivée *      Couverts *                 │
│  [ 12:00 ▾ ]            [ 2 ]                     │
│                                                    │
│  Table *                                           │
│  [ T3 — Salle — 4 pers. ] [Changer]               │
│                                                    │
│  DEMANDES SPÉCIALES                                │
│  ──────────────────────────────────────────────    │
│  [ Précisez vos besoins particuliers…        ]    │
│  [                                           ]    │
│  [                                       0/500]    │
│                                                    │
│  ⚠️ Alerte allergie sera signalée (si allergie)    │  ← feedback live
│                                                    │
├────────────────────────────────────────────────────┤
│  [Annuler]                        [Réserver →]    │  ← footer sticky
└────────────────────────────────────────────────────┘
```

---

## 4. Layout — Page complète (client online)

Route : `/bookings/new`. Layout centré, max-width `max-w-2xl mx-auto`.

Stepper en haut indiquant la progression :

```
① Disponibilité  →  ② Votre réservation  →  ③ Confirmation
                         ↑ étape active
```

Même structure de formulaire que le panel, mais en pleine page avec plus d'espace.

---

## 5. Section CLIENT

### 5.1 Mode Staff — Recherche de client existant

```html
<mat-form-field appearance="outline" class="w-full">
  <mat-label>Rechercher un client (téléphone ou email)</mat-label>
  <input matInput [formControl]="customerSearchCtrl"
    placeholder="0612345678 ou marie@example.com" />
  <mat-icon matSuffix>search</mat-icon>
  @if (isSearchingCustomer()) {
    <mat-spinner matSuffix diameter="20" />
  }
</mat-form-field>

@if (foundCustomer()) {
  <div class="flex items-center gap-3 p-3 bg-blue-50 rounded-lg border border-blue-200 mb-4">
    <mat-icon class="text-blue-500">person</mat-icon>
    <div class="flex-1">
      <p class="text-sm font-medium text-gray-900">{{ foundCustomer()!.name }}</p>
      <p class="text-xs text-gray-500">
        {{ foundCustomer()!.noShowCount }} no-show
        @if (foundCustomer()!.vipLevel !== 'None') {
          · <span class="text-amber-600">⭐ {{ foundCustomer()!.vipLevel }}</span>
        }
      </p>
    </div>
    <button mat-icon-button (click)="clearCustomer()" aria-label="Effacer le client">
      <mat-icon>close</mat-icon>
    </button>
  </div>
}

@if (foundCustomer()?.isBlacklisted) {
  <div class="p-3 bg-red-50 border border-red-200 rounded-lg mb-4 flex items-start gap-2">
    <mat-icon class="text-red-500 mt-0.5 text-base">block</mat-icon>
    <p class="text-sm text-red-700">
      Ce client est sur liste noire et ne peut pas être réservé.
    </p>
  </div>
}
```

**Comportement de la recherche :**
- Déclenche `GET /api/customers?phone=…` après 400 ms de debounce.
- Si trouvé : pré-remplit les champs Prénom, Nom, Email, Téléphone et verrouille la saisie.
- Si non trouvé : afficher `"Client non trouvé — saisissez ses coordonnées ci-dessous"` et
  laisser les champs libres.

### 5.2 Champs coordonnées

| Champ | Requis | Validation |
|-------|--------|-----------|
| Prénom | Oui (les deux modes) | Min 2 chars |
| Nom | Oui | Min 2 chars |
| Email | Oui (Online) / Optionnel (Staff) | Format email |
| Téléphone | Optionnel (Online) / Oui si source Phone | Format E.164 souple (`0[0-9]{9}`) |

En mode **Online**, Email est requis (confirmation envoyée par email).
En mode **Staff/Phone**, le téléphone est requis (SMS de confirmation).

---

## 6. Section RÉSERVATION

### 6.1 Date

```html
<mat-form-field appearance="outline">
  <mat-label>Date *</mat-label>
  <input matInput [matDatepicker]="picker" formControlName="date"
    [min]="today" />
  <mat-datepicker-toggle matIconSuffix [for]="picker" />
  <mat-datepicker #picker />
  <mat-error>Date invalide ou passée</mat-error>
</mat-form-field>
```

Désactiver les dates correspondant à des `ClosedDay` (liste chargée depuis l'API).

### 6.2 Service

```html
<mat-form-field appearance="outline">
  <mat-label>Service *</mat-label>
  <mat-select formControlName="serviceId">
    @for (svc of availableServices(); track svc.id) {
      <mat-option [value]="svc.id">{{ svc.name }}</mat-option>
    }
  </mat-select>
  <mat-error>Sélectionnez un service</mat-error>
</mat-form-field>
```

### 6.3 Heure d'arrivée

```html
<mat-form-field appearance="outline">
  <mat-label>Heure d'arrivée *</mat-label>
  <mat-select formControlName="arrivalTime">
    @for (slot of timeSlots(); track slot.value) {
      <mat-option [value]="slot.value">{{ slot.label }}</mat-option>
    }
  </mat-select>
  <mat-error>Heure requise</mat-error>
</mat-form-field>
```

**Créneaux horaires :** générés côté client entre `Service.StartTime` et
`Service.LastBookingTime` par pas de 15 min. Libellé : `"12h00"`, `"12h15"`, etc.

### 6.4 Couverts

```html
<mat-form-field appearance="outline">
  <mat-label>Couverts *</mat-label>
  <input matInput type="number" formControlName="guestsCount"
    [min]="1" [max]="30" />
  <mat-hint>Capacité de la table : {{ selectedTable()?.capacity }} pers.</mat-hint>
  <mat-error *ngIf="guestsCountCtrl.hasError('min')">Minimum 1 couvert</mat-error>
  <mat-error *ngIf="guestsCountCtrl.hasError('tableCapacity')">
    Dépasse la capacité de la table (max {{ selectedTable()?.capacity }})
  </mat-error>
</mat-form-field>
```

Valider côté client que `guestsCount ≤ table.capacity` pour retour immédiat sans appel API.

### 6.5 Table

```html
<div class="flex items-center gap-3 p-3 bg-gray-50 rounded-lg border border-gray-200">
  @if (selectedTable()) {
    <mat-icon class="text-gray-400">table_restaurant</mat-icon>
    <div class="flex-1">
      <p class="text-sm font-medium text-gray-900">
        Table T{{ selectedTable()!.number }}
      </p>
      <p class="text-xs text-gray-500">
        {{ selectedTable()!.zone }} · {{ selectedTable()!.capacity }} personnes
      </p>
    </div>
    @if (isStaff()) {
      <button mat-stroked-button (click)="openTablePicker()">Changer</button>
    }
  } @else {
    <p class="text-sm text-gray-400 italic">Aucune table sélectionnée</p>
    <button mat-stroked-button (click)="openTablePicker()">Choisir une table</button>
  }
</div>
```

Le bouton « Changer » est visible **uniquement en mode Staff** (les clients ne peuvent pas
changer de table). Il ouvre un dialogue de sélection de table depuis le snapshot courant.

---

## 7. Section DEMANDES SPÉCIALES

```html
<mat-form-field appearance="outline" class="w-full">
  <mat-label>Demandes spéciales (optionnel)</mat-label>
  <textarea matInput formControlName="specialRequests"
    rows="3"
    placeholder="Allergies, célébrations, chaise haute…"
    maxlength="500">
  </textarea>
  <mat-hint align="end">{{ specialRequests.value?.length ?? 0 }}/500</mat-hint>
  <mat-error>Maximum 500 caractères</mat-error>
</mat-form-field>
```

### Feedback live sur les flags détectés

Affiché sous le textarea, mis à jour à chaque frappe (debounce 300 ms) :

```html
<div class="flex flex-wrap gap-2 mt-1" aria-live="polite">
  @if (detectedFlags().hasAllergyAlert) {
    <span class="inline-flex items-center gap-1 text-xs bg-amber-50 text-amber-700
      border border-amber-200 px-2 py-0.5 rounded-full">
      <mat-icon class="text-xs !text-amber-500">warning</mat-icon>
      Alerte allergie sera signalée
    </span>
  }
  @if (detectedFlags().isCelebration) {
    <span class="inline-flex items-center gap-1 text-xs bg-pink-50 text-pink-700
      border border-pink-200 px-2 py-0.5 rounded-full">
      🎂 Célébration notée
    </span>
  }
  @if (detectedFlags().needsHighChair) {
    <span class="inline-flex items-center gap-1 text-xs bg-blue-50 text-blue-700
      border border-blue-200 px-2 py-0.5 rounded-full">
      👶 Chaise haute demandée
    </span>
  }
</div>
```

Ce feedback est **informatif seulement** — il n'est pas nécessaire de l'afficher pour
soumettre le formulaire. Il rassure l'utilisateur que sa demande est bien comprise.

---

## 8. Footer des actions

```html
<div class="flex justify-end gap-3 pt-4 border-t border-gray-100">
  <button mat-stroked-button type="button" (click)="onCancel()">
    Annuler
  </button>
  <button mat-flat-button color="primary"
    type="submit"
    [disabled]="bookingForm.invalid || isSubmitting() || blockedByBlacklist()">
    @if (isSubmitting()) {
      <mat-spinner diameter="20" class="inline-block mr-2" />
      Réservation en cours…
    } @else {
      Réserver
      <mat-icon matIconSuffix>arrow_forward</mat-icon>
    }
  </button>
</div>
```

**Règle :** Le bouton « Réserver » est désactivé si :
- Le formulaire est invalide (validation Angular).
- La soumission est en cours (`isSubmitting() = true`).
- Le client trouvé est blacklisté (`blockedByBlacklist() = true`).

---

## 9. Gestion des erreurs API

Mapper chaque code d'erreur backend vers un message utilisateur humain.
Les erreurs s'affichent dans un **banner d'erreur** en haut du formulaire
(pas dans un snackbar — pour ce cas, l'utilisateur doit lire et comprendre avant de
réessayer) :

```html
@if (apiError()) {
  <div class="p-4 bg-red-50 border border-red-200 rounded-lg flex items-start gap-3"
    role="alert">
    <mat-icon class="text-red-500 mt-0.5">error_outline</mat-icon>
    <div>
      <p class="text-sm font-medium text-red-800">Impossible de créer la réservation</p>
      <p class="text-sm text-red-700 mt-1">{{ apiError() }}</p>
    </div>
    <button mat-icon-button class="ml-auto" (click)="clearError()">
      <mat-icon>close</mat-icon>
    </button>
  </div>
}
```

### Table de correspondance codes → messages utilisateur

| Code API | Message affiché |
|----------|-----------------|
| `TIME_OUTSIDE_SERVICE` | L'heure saisie est en dehors du créneau de ce service. |
| `GUESTS_BELOW_MIN` | Le nombre de couverts est inférieur au minimum de la table. |
| `GUESTS_EXCEED_CAPACITY` | Trop de couverts pour cette table (maximum : N). |
| `SERVICE_FULLY_BOOKED` | Ce service est complet, toutes les places sont réservées. |
| `TABLE_CONFLICT` | Cette table est déjà occupée sur ce créneau. Choisissez une autre heure ou une autre table. |
| `HORIZON_EXCEEDED` | La date est trop éloignée. Vous ne pouvez pas réserver aussi longtemps à l'avance. |
| `MIN_LEAD_TIME_VIOLATED` | Délai trop court avant le service. Réservez au moins 2 heures à l'avance. |
| `BOOKING_DATE_IN_PAST` | La date choisie est dans le passé. |
| `CUSTOMER_BLACKLISTED` | Ce client ne peut pas effectuer de réservation. Contactez l'établissement. |
| `RESTAURANT_CLOSED` | Le restaurant est fermé à cette date. |
| `WALKIN_MUST_BE_TODAY` | Une réservation sans délai (walk-in) doit être pour aujourd'hui. |
| *(erreur générique)* | Une erreur inattendue s'est produite. Veuillez réessayer. |

---

## 10. État de succès

Après une réponse `HTTP 201` de l'API, **remplacer entièrement le formulaire** par une
confirmation (pas de navigation — évite la perte de contexte pour le Staff en plein service) :

```
┌────────────────────────────────────────────────────┐
│                                                    │
│         ✅                                         │  ← icône verte animée
│   Réservation enregistrée                          │
│                                                    │
│   Marie Dupont                                     │
│   Déjeuner · Mardi 8 juin · 12h30                 │
│   Table T3 — Salle · 2 couverts                   │
│                                                    │
│   Statut : En attente de confirmation              │  ← badge statut jaune
│                                                    │
│   [+ Nouvelle réservation]   [Retour à la salle]  │
│                                                    │
└────────────────────────────────────────────────────┘
```

**Animation :** L'icône ✅ entre avec une animation `scale-in` (CSS `@keyframes` ou
Angular Animation) pour attirer l'attention sans être agressive.

Le bouton « Retour à la salle » ferme le drawer et recharge le snapshot pour que la
nouvelle réservation apparaisse immédiatement.

---

## 11. Accessibilité

| Critère | Implémentation |
|---------|---------------|
| Focus initial | Au montage, mettre le focus sur le premier champ vide |
| Sections | `<fieldset>` + `<legend>` pour « Client », « Réservation », « Demandes spéciales » |
| Erreurs API | `role="alert"` sur le banner (annoncé par les SR) |
| Flags live | `aria-live="polite"` sur la zone flags (§7) |
| Bouton désactivé | `aria-disabled="true"` + tooltip expliquant pourquoi |
| Labels | Tous les `mat-form-field` ont un `mat-label` explicite |

---

## 12. Responsive

| Breakpoint | Comportement |
|-----------|-------------|
| Mobile | Tous les champs en colonne (1 par ligne) |
| Tablette (≥ sm) | Champs en 2 colonnes (Prénom/Nom, Date/Service, Heure/Couverts) |
| Desktop (≥ md) | Idem tablette dans le drawer. En pleine page : 3 colonnes possibles |

---

## 13. Source de réservation (champ caché)

Le champ `source` est déterminé automatiquement selon le rôle et ne s'affiche
pas dans le formulaire :

| Rôle connecté | Source envoyée à l'API |
|---------------|----------------------|
| Online | `Online` |
| Staff / Manager / Admin | `Staff` (ou `Phone` si le staff crée via recherche téléphonique) |

> Amélioration future : permettre au Staff de basculer entre `Staff` et `Phone`
> via un toggle discret pour les réservations téléphoniques (impacte l'horizon de
> réservation RB-005).
