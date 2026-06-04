---
screen: AvailabilitySearchComponent
epic: 4
story: 4.1
status: ready-for-implementation
author: Sally (UX Designer)
date: 2026-06-04
---

# UX Spec — Recherche de Disponibilité (AvailabilitySearchComponent)

---

## 1. Contexte utilisateur

**Personas :**
- **Marie (UJ-1)** — cliente en ligne, cherche une table depuis son téléphone, veut le
  résultat immédiat avec le minimum de friction.
- **Lucas (Staff)** — crée une réservation pour un client qui appelle ; la recherche lui
  donne une vue rapide des places disponibles avant d'ouvrir le formulaire.

**Point d'entrée :**
- Pour le **client online** : page dédiée `/availability`, accessible depuis la page d'accueil
  publique (bouton « Réserver une table »).
- Pour le **Staff** : bouton « + Réservation » dans la vue salle, qui ouvre ce composant en
  première étape d'un flow en deux temps (Recherche → Formulaire).

---

## 2. Structure de la page (client online)

```
┌────────────────────────────────────────────────────────────────┐
│  HERO SECTION (fond neutre ou image restaurant floue)         │
│                                                                │
│       🍽️  Réserver une table                                  │
│                                                                │
│  ┌──────────┐ ┌────────────┐ ┌─────────┐ ┌─────────────────┐ │
│  │   Date   │ │  Service ▾ │ │Couverts │ │   Zone ▾        │ │
│  │ jj/mm/aa │ │ Déjeuner   │ │   [2]   │ │ Toutes zones    │ │
│  └──────────┘ └────────────┘ └─────────┘ └─────────────────┘ │
│                                                                │
│                    [ Rechercher les disponibilités ]          │
└────────────────────────────────────────────────────────────────┘

┌────────────────────────────────────────────────────────────────┐
│  RÉSULTATS                                                     │
│                                                                │
│  3 tables disponibles pour le Déjeuner du 08/06 · 2 couverts │
│                                                                │
│  ┌────────────────┐ ┌────────────────┐ ┌────────────────┐    │
│  │ Table T1       │ │ Table T3       │ │ Table T7       │    │
│  │ 2 personnes    │ │ 4 personnes    │ │ 4 personnes    │    │
│  │ Salle          │ │ Salle          │ │ Terrasse       │    │
│  │                │ │                │ │                │    │
│  │ [Choisir →]    │ │ [Choisir →]    │ │ [Choisir →]    │    │
│  └────────────────┘ └────────────────┘ └────────────────┘    │
└────────────────────────────────────────────────────────────────┘
```

---

## 3. Formulaire de recherche

### Champs

| Champ | Composant | Validation | Defaut |
|-------|-----------|-----------|--------|
| Date | `mat-datepicker` | Requis, ≥ aujourd'hui | Aujourd'hui |
| Service | `mat-select` (depuis `GET /api/services`) | Requis | Premier service disponible |
| Couverts | `<input type="number">` mat-form-field | Requis, min 1, max 30 | 2 |
| Zone | `mat-select` | Optionnel | « Toutes zones » (valeur `null`) |

### Comportement du champ « Service »

- Chargé depuis l'API au montage du composant.
- Afficher uniquement les services avec `StartTime` dans le futur pour la date sélectionnée.
- Si aucun service disponible pour la date : afficher le select désactivé + message inline
  `"Aucun service disponible pour cette date"`.

### Bouton CTA

```html
<button mat-flat-button color="primary"
  class="w-full sm:w-auto px-8 py-2 text-base"
  [disabled]="searchForm.invalid || isSearching()"
  (click)="onSearch()">
  @if (isSearching()) {
    <mat-spinner diameter="20" class="inline-block mr-2" />
    Recherche en cours…
  } @else {
    Rechercher les disponibilités
  }
</button>
```

### Validation inline

- Champs requis : afficher l'erreur Angular Material seulement après `touched` (pas au
  chargement de la page — éviter de submerger Marie d'emblée).
- Date dans le passé : `"Choisissez une date à partir d'aujourd'hui"`.
- Couverts < 1 ou non entier : `"Entrez un nombre de personnes valide"`.

---

## 4. Zones de résultats

### 4.1 État initial (avant recherche)

```
┌──────────────────────────────────────────────┐
│                                              │
│   🗓️                                         │
│   Renseignez vos critères ci-dessus          │
│   pour découvrir les tables disponibles.     │
│                                              │
└──────────────────────────────────────────────┘
```

Background `bg-gray-50`, texte centré, padding vertical généreux (`py-16`).

### 4.2 Chargement

Skeleton de 3 cartes grises (pas de spinner plein écran — le contexte est partiel) :

```html
@for (i of [1, 2, 3]; track i) {
  <div class="rounded-lg bg-gray-100 animate-pulse h-36"></div>
}
```

### 4.3 Résultats trouvés

**En-tête de résultats :**

```html
<p class="text-sm text-gray-600 mb-4">
  <strong>{{ results().length }} table{{ results().length > 1 ? 's' : '' }}</strong>
  disponible{{ results().length > 1 ? 's' : '' }}
  pour le <strong>{{ selectedService()?.name }}</strong>
  du <strong>{{ selectedDate() | date:'dd MMMM' }}</strong>
  · <strong>{{ guestsCount() }} couvert{{ guestsCount() > 1 ? 's' : '' }}</strong>
</p>
```

**Carte de table disponible :**

```
┌──────────────────────────────┐
│ T3 · Salle                   │  ← numéro + zone
│                              │
│ ████████░░  4 personnes       │  ← jauge capacité (couverts / capacité)
│ Adapté pour 3 à 4 convives   │  ← hint contextuel
│                              │
│         [ Choisir cette table →] │  ← CTA plein-width
└──────────────────────────────┘
```

**Jauge de capacité (accessibilité + UX) :**

```html
<div class="flex items-center gap-2 my-2">
  <div class="flex-1 bg-gray-100 rounded-full h-1.5">
    <div class="bg-green-500 h-1.5 rounded-full"
      [style.width.%]="(guestsCount() / table.capacity) * 100"
      [attr.aria-valuenow]="guestsCount()"
      [attr.aria-valuemax]="table.capacity"
      role="progressbar"
      [attr.aria-label]="guestsCount() + ' sur ' + table.capacity + ' places'">
    </div>
  </div>
  <span class="text-xs text-gray-500">{{ table.capacity }} pers.</span>
</div>
<p class="text-xs text-gray-400">
  Adapté pour {{ table.minCapacity }} à {{ table.capacity }} convives
</p>
```

**Badge zone :**

```html
<span class="inline-block text-xs bg-gray-100 text-gray-600 px-2 py-0.5 rounded-full">
  {{ table.zone }}
</span>
```

**Tri :** Les cartes sont affichées dans l'ordre retourné par l'API (capacité croissante).
Pas de tri côté client nécessaire.

### 4.4 Aucune table disponible (pas de match guests/capacité)

```
┌───────────────────────────────────────────────────────────────┐
│                                                               │
│  😕  Aucune table disponible                                  │
│                                                               │
│  Nous n'avons pas de table pour {{ guestsCount() }} couverts  │
│  le {{ selectedDate() | date:'dd MMMM' }} en {{ service }}.   │
│                                                               │
│  Suggestions :                                                │
│  • Essayez une autre date                                     │
│  • Essayez le service Dîner                                   │
│  • Retirez le filtre de zone                                  │
│                                                               │
└───────────────────────────────────────────────────────────────┘
```

Les suggestions s'affichent uniquement si elles sont pertinentes (ex : ne pas suggérer
« retirez le filtre zone » si aucune zone n'était filtrée).

### 4.5 Service complet (`reason: "ServiceFullyBooked"`)

```
┌───────────────────────────────────────────────────────────────┐
│                                                               │
│  📅  Service complet                                          │
│                                                               │
│  Le service {{ service }} du {{ date }} est complet.          │
│  Toutes les places sont réservées.                            │
│                                                               │
│  [ Choisir une autre date ]   [ Choisir un autre service ]   │
│                                                               │
└───────────────────────────────────────────────────────────────┘
```

Les deux boutons relancent la recherche avec les paramètres modifiés.

---

## 5. Après la sélection d'une table

Cliquer « Choisir cette table » déclenche la navigation vers `BookingFormComponent`
avec les paramètres suivants en queryParams (ou state du router) :

```typescript
router.navigate(['/bookings/new'], {
  state: {
    tableId: table.id,
    tableNumber: table.number,
    date: selectedDate(),
    serviceId: selectedServiceId(),
    guestsCount: guestsCount()
  }
})
```

Un **fil d'Ariane** ou un **stepper** en haut de la page indique la progression :

```
① Recherche disponibilité  →  ② Vos coordonnées  →  ③ Confirmation
```

---

## 6. Version Staff (intégrée dans un drawer de la vue salle)

Quand le Staff accède à la recherche depuis `FloorPlanComponent` :

- La date et le service sont **pré-remplis** depuis la vue salle courante (non modifiables
  sauf action explicite).
- Le filtre zone est visible et optionnel.
- Les cartes de résultats ont un bouton « Assigner → » au lieu de « Choisir → ».
- La sélection ouvre directement le `BookingFormComponent` en mode Staff dans le même drawer.

---

## 7. Accessibilité

| Critère | Implémentation |
|---------|---------------|
| Focus initial | Mettre le focus sur le champ « Date » au montage |
| Live region résultats | `aria-live="polite"` sur la zone résultats (annonce le nombre trouvé) |
| Cartes de résultats | `role="article"` + `aria-label="Table T3, 4 personnes, Salle"` |
| Bouton Choisir | `aria-label="Choisir la table T3, 4 personnes, Salle"` (contexte complet pour SR) |
| Jauge capacité | `role="progressbar"` + aria-value* (cf. §4.3) |
| Erreurs de formulaire | `aria-describedby` liant le champ et son message d'erreur |

---

## 8. Responsive

| Breakpoint | Comportement |
|-----------|-------------|
| Mobile (< sm) | Formulaire en colonne, 1 carte par ligne |
| Tablette (sm–md) | Formulaire en 2 lignes (date+service / couverts+zone), 2 cartes par ligne |
| Desktop (md+) | Formulaire en ligne, 3–4 cartes par ligne |

---

## 9. Route & Navigation

| Contexte | Route | Composant |
|---------|-------|-----------|
| Client online (accès public) | `/availability` | Standalone, sans authGuard |
| Staff depuis vue salle | Drawer dans `/floor` | Intégré dans FloorPlanComponent |
