#!/usr/bin/env bash
# Usage: git diff main...branch | ./review-pr.sh
# Output: JSON array on stdout — zero other text
set -euo pipefail

DIFF=$(cat)

if [[ -z "$DIFF" ]]; then
  echo "[]"
  exit 0
fi

PROMPT="Tu es un reviewer strict pour un système de réservation de tables restaurant.
Backend : ASP.NET Core 10 / C# 14 — Clean Architecture + CQRS (MediatR). Règles métier dans documentation/specs.md (RB-001–RB-017).
Frontend : Angular 20 avec Signals.
Base de données : SQLite via EF Core.

Analyse le git diff ci-dessous. Réponds UNIQUEMENT par un tableau JSON brut — zéro texte avant ou après, zéro balise markdown, zéro bloc de code. Le premier caractère de ta réponse doit être [ et le dernier ].

Format de chaque finding :
{\"file\": \"chemin/fichier\", \"line\": <entier>, \"severity\": \"critical|warning|info\", \"category\": \"bug|regression|weakened-test|security|changelog\", \"message\": \"description\"}

Retourne [] si la PR est propre.

Trois axes de revue :
1. BUGS & RÉGRESSIONS — erreurs logiques, règles métier violées (RB-001–RB-017), et le cas star : un test supprimé ou affaibli pour faire passer la CI artificiellement.
2. SÉCURITÉ — secret/token/clé API en dur dans le code, injection SQL, contournement d'authentification ou d'autorisation.
3. CHANGELOG — comportement public modifié (contrat API, règle métier, endpoint) sans entrée sous ## [Unreleased] dans CHANGELOG.md.

DIFF :
${DIFF}"

RESPONSE=$(jq -n --arg prompt "$PROMPT" '{
  model: "claude-opus-4-8",
  max_tokens: 4096,
  messages: [{"role": "user", "content": $prompt}]
}' | curl -s https://api.anthropic.com/v1/messages \
  -H "x-api-key: ${ANTHROPIC_API_KEY}" \
  -H "anthropic-version: 2023-06-01" \
  -H "content-type: application/json" \
  -d @-)

TEXT=$(echo "$RESPONSE" | jq -r '.content[0].text // empty' | sed '/^```/d')

if [[ -z "$TEXT" ]]; then
  echo "[]"
  exit 0
fi

echo "$TEXT"
