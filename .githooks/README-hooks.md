# Activation des hooks Git

Les hooks sont dans `.githooks/` et versionnés avec le projet.  
Chaque membre de l'équipe doit les activer une seule fois après le clone :

```bash
git config core.hooksPath .githooks
```

## Hooks disponibles

| Hook         | Déclencheur        | Action                                      |
|--------------|--------------------|---------------------------------------------|
| `pre-commit` | Avant chaque commit| Lance les tests unitaires — bloque si échec |

## Désactiver temporairement (déconseillé)

```bash
git commit --no-verify   # bypass le hook — à éviter
```
