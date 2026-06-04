# Conventions Git

## Branches

| Préfixe      | Usage                                    | Exemple                               |
|--------------|------------------------------------------|---------------------------------------|
| `main`       | Production — merge via PR uniquement     | —                                     |
| `develop`    | Intégration — base de toutes les features| —                                     |
| `feature/*`  | Nouvelle fonctionnalité                  | `feature/floor-plan-view`             |
| `fix/*`      | Correction de bug                        | `fix/late-cancel-flag`                |
| `test/*`     | Ajout ou amélioration de tests           | `test/rb007-cancellation-edge-cases`  |
| `refactor/*` | Refactoring sans changement de comportement | `refactor/booking-aggregate`       |
| `docs/*`     | Documentation uniquement                 | `docs/update-specs-rb015`             |

- Toujours brancher depuis `develop`, jamais depuis `main`.
- Nom de branche en **kebab-case**, anglais ou français, court (< 50 caractères).
- Supprimer la branche après merge.

## Format des commits (Conventional Commits)

```
<type>(<scope>): <description courte>

Types  : feat | fix | test | refactor | docs | chore | perf
Scopes : domain | application | api | frontend | db | ci | config

Exemples valides :
feat(domain): ajouter règle RB-007 annulation tardive
test(domain): couvrir cas limite no-show count = 3
fix(api): corriger code HTTP 409 sur conflit de table
refactor(domain): extraire TimeSlot en value object
chore(db): migration index bookings_date_service
docs: mettre à jour specs RB-015 récurrence
```

**Règles :**
- Description en minuscules, sans point final.
- Maximum 72 caractères sur la première ligne.
- Un commit = une intention — pas de "fix stuff", "wip", "misc".
- Les tests sont committés **avec** le code qu'ils couvrent (pas dans un commit séparé).
- En anglais ou en français — choisir et tenir.

## Workflow

```
develop ──┬── feature/floor-plan ──────────────────┐
          │                                        ↓ PR + review
          └── fix/late-cancel ────────────────────→ develop ──→ main (release)
```

1. Créer la branche depuis `develop`.
2. Commits atomiques tout au long du développement.
3. Ouvrir une **Pull Request** vers `develop` quand la feature est terminée.
4. La PR doit passer : tests unitaires + tests d'intégration + review (1 approbateur min).
5. Merge en **squash** sur `develop` pour garder un historique propre.
6. Les releases vers `main` se font via PR dédiée, avec tag semver (`v1.0.0`).

## Règle de commit obligatoire

**Les tests unitaires doivent passer avant tout commit.**  
Un hook git (`pre-commit`) est fourni dans `.githooks/` — voir `README-hooks.md` pour l'activer.  
Le hook Claude Code dans `.claude/settings.json` applique la même règle automatiquement.
