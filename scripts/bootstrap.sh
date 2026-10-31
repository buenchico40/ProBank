#!/usr/bin/env bash
# Prepara el entorno local: restaura .NET, instala dependencias de Playwright y valida Terraform.
set -euo pipefail
cd "$(dirname "$0")/.."
dotnet restore src/ProBank.sln
dotnet restore test/ProBank.Tests.sln
( cd test/e2e && npm install && npx playwright install chromium )
( cd infra && terraform fmt -recursive -check || true )
echo "Listo. Abre Run and Debug › 'Api + Web'."
