#!/usr/bin/env bash
set -euo pipefail
echo "Restoring backend..."
(cd backend && dotnet restore)
echo "Installing frontend deps..."
(cd frontend && npm ci)
echo "Dev environment ready."
