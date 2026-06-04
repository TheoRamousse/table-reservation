# Règles absolues

- Toute règle métier vit dans `Reservation.Domain` — jamais dans les controllers ou handlers.
- Chaque règle de `documentation/specs.md` doit avoir au moins un test qui la couvre.
- Les transitions de statut passent toutes par `Booking.TransitionTo()` — jamais par assignation directe.
- Ne jamais modifier le schéma EF sans migration (`dotnet ef migrations add`).
- Jamais `DateTime.UtcNow` directement — toujours passer par `IClock`.
- Les handlers MediatR orchestrent uniquement — zéro logique métier.

## Règles Git (obligatoires pour tout agent qui code)

- **Début de story** : créer une branche `feature/<story-id>-<nom-court>` depuis `develop` avant d'écrire la première ligne de code. Ex : `feature/story-3-1-floor-plan`.
- **Commits atomiques** : committer après chaque unité logique (modèles créés, service créé, composant créé, tests ajoutés) — pas tout à la fin.
- **Format obligatoire** : Conventional Commits — `feat(frontend): <description>`. Voir `conventions-git.md` pour les types et scopes.
- Ne jamais committer sur `main` ou `develop` directement.
