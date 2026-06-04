# Règles absolues

- Toute règle métier vit dans `Reservation.Domain` — jamais dans les controllers ou handlers.
- Chaque règle de `documentation/specs.md` doit avoir au moins un test qui la couvre.
- Les transitions de statut passent toutes par `Booking.TransitionTo()` — jamais par assignation directe.
- Ne jamais modifier le schéma EF sans migration (`dotnet ef migrations add`).
- Jamais `DateTime.UtcNow` directement — toujours passer par `IClock`.
- Les handlers MediatR orchestrent uniquement — zéro logique métier.
