#!/usr/bin/env bash
# Usage: git diff main...branch | ./review-pr.sh
# Output: JSON array on stdout — zero other text
set -euo pipefail

DIFF=$(cat)

if [[ -z "$DIFF" ]]; then
  echo "[]"
  exit 0
fi

RESPONSE=$(jq -n --arg diff "$DIFF" '{
  model: "claude-opus-4-8",
  max_tokens: 4096,
  messages: [{
    role: "user",
    content: (
      "Tu es un reviewer strict pour un système de réservation de tables restaurant.\n"
      + "Backend : ASP.NET Core 10 / C# 14 — Clean Architecture + CQRS (MediatR). Règles métier dans documentation/specs.md (RB-001–RB-017).\n"
      + "Frontend : Angular 20 avec Signals.\n"
      + "Base de données : SQLite via EF Core.\n\n"
      + "Analyse le git diff ci-dessous. Réponds UNIQUEMENT par un tableau JSON — zéro texte avant ou après.\n\n"
      + "Format de chaque finding :\n"
      + "{\"file\": \"chemin/fichier\", \"line\": <entier>, \"severity\": \"critical|warning|info\", \"category\": \"bug|regression|weakened-test|security|changelog\", \"message\": \"description\"}\n\n"
      + "Retourne [] si la PR est propre.\n\n"
      + "Trois axes de revue :\n"
      + "1. BUGS & RÉGRESSIONS — erreurs logiques, règles métier violées (RB-001–RB-017), et le cas star : un test supprimé ou affaibli pour faire passer la CI artificiellement.\n"
      + "2. SÉCURITÉ — secret/token/clé API en dur dans le code, injection SQL, contournement d\'authentification ou d\'autorisation.\n"
      + "3. CHANGELOG — comportement public modifié (contrat API, règle métier, endpoint) sans entrée sous ## [Unreleased] dans CHANGELOG.md.\n\n"
      + "DIFF :\n" + $diff
    )
  }]
}' | curl -s https://api.anthropic.com/v1/messages \
  -H "x-api-key: ${ANTHROPIC_API_KEY}" \
  -H "anthropic-version: 2023-06-01" \
  -H "content-type: application/json" \
  -d @-)

TEXT=$(echo "$RESPONSE" | jq -r '.content[0].text // empty')

if [[ -z "$TEXT" ]]; then
  echo "[]"
  exit 0
fi

echo "$TEXT"
