---
screen: FloorPlanComponent
epic: 3
stories: 3.1, 3.2, 3.3
status: ready-for-implementation
author: Sally (UX Designer)
date: 2026-06-04
---

# UX Spec — Vue Salle (FloorPlanComponent)

> Le composant est **déjà codé et fonctionnel**. Ce document est une spec de **styling** et
> de **polish UX** : il décrit l'apparence cible, les états visuels et les micro-interactions
> à appliquer sur le code existant.

---

## 1. Contexte utilisateur

**Persona principal :** Lucas, serveur, utilise la vue sur tablette en salle pendant le service.

**Job-to-be-done :** Voir d'un coup d'œil l'état de toutes les tables, confirmer les arrivées
et libérer les tables le plus vite possible — sans se perdre dans des menus.

**Contrainte clé :** L'action la plus fréquente (« Asseoir ») doit pouvoir se déclencher en
moins de 3 secondes depuis l'ouverture de la vue (SM-3 du PRD).

---

## 2. Structure de la page

```
┌────────────────────────────────────────────────────────────────┐
│  HEADER STICKY — bg blanc, ombre légère                       │
│  [Vue Salle]  [Date]  [Service ▾]  [42/80 couverts]  ●Temps   │
│                                                    réel  [⏏]  │
├────────────────────────────────────────────────────────────────┤
│  CONTENU PRINCIPAL scrollable                                  │
│                                                                │
│  ── SALLE ──────────────────────────────────────────────────  │
│  ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐     │
│  │  T1  │ │  T2  │ │  T3  │ │  T4  │ │  T5  │ │  T6  │     │
│  └──────┘ └──────┘ └──────┘ └──────┘ └──────┘ └──────┘     │
│                                                                │
│  ── TERRASSE ───────────────────────────────────────────────  │
│  ┌──────┐ ┌──────┐ ┌──────┐                                  │
│  │  T7  │ │  T8  │ │  T9  │                                  │
│  └──────┘ └──────┘ └──────┘                                  │
│                                                                │
├────────────────────────────────────────────────────────────────┤
│  FOOTER — Légende des couleurs                                 │
└────────────────────────────────────────────────────────────────┘
```

---

## 3. Header

### Composants

| Élément | Spec actuelle | Cible |
|---------|--------------|-------|
| Titre « Vue Salle » | `text-lg font-semibold` | Conserver + ajouter une icône `mat-icon` `table_restaurant` à gauche |
| Sélecteur Date | `mat-form-field outline` | Conserver. Largeur fixe `w-36` |
| Sélecteur Service | `mat-form-field outline min-w-36` | Conserver. Mettre `min-w-44` pour les libellés longs |
| Compteur couverts | `text-sm text-gray-600` | Styliser : `font-mono` pour les chiffres, ajouter une couleur d'alerte quand > 90 % de MaxCovers : passer en `text-amber-600 font-semibold` |
| Badge SignalR | Cercle + label | Conserver. S'assurer que le label « Temps réel » est visible à partir de `sm:` |
| Bouton logout | `mat-icon-button` | Conserver |

### Compteur de couverts — logique de couleur

```
totalConfirmedCovers / maxCovers
  < 70 %  → text-gray-600        (neutre)
  70–90 % → text-amber-600       (avertissement)
  > 90 %  → text-red-600 + bold  (alerte)
```

---

## 4. Sections de zones

### En-tête de zone

```html
<!-- Cible -->
<h2 class="flex items-center gap-2 text-sm font-semibold text-gray-500 uppercase tracking-wide mb-3">
  <mat-icon class="text-base text-gray-400">{{ zoneIcon }}</mat-icon>
  {{ group.zone }}
  <span class="text-gray-300 font-normal normal-case tracking-normal">
    ({{ group.tables.length }} tables)
  </span>
</h2>
```

### Icônes de zones

| Zone | mat-icon |
|------|----------|
| Salle | `table_restaurant` |
| Terrasse | `deck` ou `nature` |
| Bar | `local_bar` |
| Salon privé | `meeting_room` |

### Grille

Conserver le grid responsive existant. Ajouter `auto-rows-fr` pour que toutes les cartes
d'une même rangée aient la même hauteur.

---

## 5. TableCard — Spec de styling

> Fichier cible : `table-card.component.html` + `table-card.component.scss`

### Anatomie de la carte (cible)

```
┌────────────────────────────────┐
│████████████████████████████████│  ← barre de statut h-3 (au lieu de h-2)
│                                │
│ T3                       4 pers│  ← numéro gras + capacité gris
│ ●  Réservée                    │  ← dot couleur + label WCAG
│                                │
│ Marie Dupont                   │  ← (si réservation active)
│ 3 cvts · 12h30                 │
│ ⚠️ 🎂                          │  ← flags si présents
└────────────────────────────────┘
```

### Barre de statut

Passer de `h-2` à `h-3`. Plus visible sans être agressive.

### Badge statut

Remplacer le badge `rounded-full` plein par un badge **dot + label** :

```html
<!-- Avant -->
<span class="inline-block text-xs font-semibold px-2 py-0.5 rounded-full text-white [class]="statusConfig().cssClass"">
  {{ statusConfig().label }}
</span>

<!-- Après -->
<span class="inline-flex items-center gap-1.5 text-xs font-medium text-gray-700">
  <span class="inline-block w-2 h-2 rounded-full flex-shrink-0" [class]="statusConfig().cssClass"></span>
  {{ statusConfig().label }}
</span>
```

Avantage : meilleur contraste texte (gris foncé sur fond blanc) tout en conservant la
couleur codée par statut. Satisfait WCAG 2.1 AA pour le texte.

### États hover & focus

```scss
// Ajouter dans table-card.component.scss
:host {
  display: block;

  &:focus-within > div {
    @apply ring-2 ring-blue-400 ring-offset-1;
  }
}

div {
  @apply transition-all duration-150;

  &:hover {
    @apply -translate-y-0.5 shadow-lg;
  }

  &:active {
    @apply translate-y-0 shadow-sm;
  }
}
```

L'effet `-translate-y-0.5` sur hover donne un retour tactile subtil qui indique
la cliquabilité, crucial pour le staff sur tablette.

### Carte « Libre » — indicateur d'action

Pour les tables libres, ajouter une icône `+` discrète pour signaler que le clic
ouvrira un formulaire :

```html
@if (table().status === TableStatus.Free) {
  <div class="absolute bottom-2 right-2 text-gray-300">
    <mat-icon class="text-base">add_circle_outline</mat-icon>
  </div>
}
```

### Carte inactive

Opacité déjà à `0.40`. Ajouter `cursor-not-allowed pointer-events-none` pour
éviter le click accidentel.

### Couleurs de statut

Aucun changement sur les couleurs PRD. Elles sont conformes et déjà codées :

| Statut | Hex | Classe CSS |
|--------|-----|------------|
| Libre | `#22c55e` | `.status-libre` |
| En attente | `#eab308` | `.status-en-attente` |
| Réservée | `#3b82f6` | `.status-reservee` |
| Occupée | `#f97316` | `.status-occupee` |
| No-show | `#ef4444` | `.status-noshow` |
| Inactive | `#9ca3af` | `.status-inactive` |

---

## 6. États de la vue principale

### 6.1 Chargement

```html
<!-- Déjà codé — conserver -->
<div class="flex-1 flex items-center justify-center">
  <mat-spinner diameter="48" />
</div>
```

### 6.2 Aucun service sélectionné

```html
<div class="flex-1 flex flex-col items-center justify-center gap-3 text-gray-400">
  <mat-icon class="text-5xl !text-gray-300">table_restaurant</mat-icon>
  <p class="text-sm">Sélectionnez un service pour afficher la salle.</p>
</div>
```

### 6.3 Snapshot vide (service sans tables)

```html
<div class="flex-1 flex flex-col items-center justify-center gap-3 text-gray-400">
  <mat-icon class="text-5xl !text-gray-300">inventory_2</mat-icon>
  <p class="text-sm">Aucune table active pour ce service.</p>
</div>
```

### 6.4 Erreur de chargement

Via le snackbar global (déjà géré par `error.interceptor.ts`). Pas d'état d'erreur
inline nécessaire sur cette vue.

---

## 7. Interactions (déjà codées — spec comportementale)

| Action | Déclencheur | Comportement attendu |
|--------|-------------|----------------------|
| Clic table **Libre** | `onTableClick` | Ouvrir `BookingFormComponent` en slide panel (right drawer) pré-rempli avec tableId, date, serviceId |
| Clic table **Réservée / En attente** | `onTableClick` | Ouvrir panneau de détail de la réservation avec actions disponibles selon statut |
| Clic table **Occupée** | `onTableClick` | Afficher `ConfirmActionDialogComponent` « Marquer comme terminé ? » |
| Hover table | `matTooltip` | Tooltip : `Nom · N couverts · 12h30` (déjà codé) |
| Clic logout | `logout()` | Déconnexion + redirection /login |

### Dialogue de confirmation (ConfirmActionDialog)

```
┌──────────────────────────────────────┐
│  Terminer la table ?                 │
│                                      │
│  Marie Dupont — T3                   │
│  3 couverts · arrivée 12h30          │
│                                      │
│            [Annuler]  [Terminer]     │
└──────────────────────────────────────┘
```

Le bouton « Terminer » est le CTA primaire (couleur accent).

---

## 8. Footer — Légende

Conserver le footer existant. Ajouter `title` sur chaque span pour l'accessibilité clavier :

```html
<span class="flex items-center gap-1.5" [title]="item.label">
  <span class="inline-block w-3 h-3 rounded-full" [class]="item.cls" aria-hidden="true"></span>
  {{ item.label }}
</span>
```

---

## 9. Accessibilité (WCAG 2.1 AA)

| Critère | Implémentation |
|---------|---------------|
| Couleur + label textuel | Badge dot + label (cf. §5) |
| Focus visible | `ring-2 ring-blue-400` sur focus-within (cf. §5) |
| Rôle des cartes | Ajouter `role="button"` + `tabindex="0"` sur le `<div>` cliquable |
| Tooltip clavier | `matTooltip` s'active au focus — vérifier avec tabulation |
| Images décoratives | `aria-hidden="true"` sur les icônes décoratives |

---

## 10. Responsive

| Breakpoint | Comportement |
|-----------|-------------|
| Mobile (< sm) | 2 colonnes, header compact, label SignalR masqué |
| Tablette (sm–lg) | 3–4 colonnes, usage cible en service |
| Desktop (lg+) | 5–6 colonnes, vue panoramique |

Pas de changement sur la grille existante — elle est déjà correcte.

---

## 11. Tokens de design

> Tailwind CSS 4 + Angular Material 20. Pas de design system custom à créer.

| Usage | Valeur |
|-------|--------|
| Surface principale | `bg-gray-50` |
| Cartes | `bg-white` |
| Bordures | `border-gray-200` |
| Texte primaire | `text-gray-900` |
| Texte secondaire | `text-gray-500` |
| Accent actions | `mat-primary` (bleu Angular Material) |
| Danger | `text-red-600` / `bg-red-50` |
