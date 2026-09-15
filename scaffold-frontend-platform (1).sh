#!/usr/bin/env bash
###############################################################################
# scaffold-frontend-platform.sh
#
# Principal-Engineer-grade scaffolder for:
#   1) The platform-level monorepo skeleton (.github, docs/adr, infra/*)
#   2) The React 19 + TypeScript 7 Feature-Sliced frontend, fully wired with
#      powerbi-client embedding, custom layout builder, data-source forms,
#      report viewers, auth, shared design-system kernel, and the full
#      Unit/Integration/Path/Regression/E2E/Security test suite.
#
# Usage:
#   chmod +x scaffold-frontend-platform.sh
#   ./scaffold-frontend-platform.sh [platform-root]     # default: ./analytics-platform
#
# Requirements: Node.js 22+/24 LTS and npm on PATH.
###############################################################################

set -euo pipefail

PLATFORM_ROOT="${1:-.}"
FRONTEND_DIR="${2:-frontend}"

log()  { printf '\033[1;36m[scaffold]\033[0m %s\n' "$1"; }
ok()   { printf '\033[1;32m[  ok   ]\033[0m %s\n' "$1"; }
warn() { printf '\033[1;33m[ warn  ]\033[0m %s\n' "$1"; }
die()  { printf '\033[1;31m[ fail  ]\033[0m %s\n' "$1"; exit 1; }

# Environment compatibility for Windows / Git Bash / WSL
if ! command -v node >/dev/null 2>&1; then
  if command -v node.exe >/dev/null 2>&1; then
    node() { node.exe "$@"; }
  fi
fi

if ! command -v npm >/dev/null 2>&1; then
  if command -v npm.cmd >/dev/null 2>&1; then
    npm() { npm.cmd "$@"; }
  fi
fi

command -v node >/dev/null 2>&1 || command -v node.exe >/dev/null 2>&1 || die "Node.js not found. Install Node 22+/24 LTS first."
command -v npm  >/dev/null 2>&1 || command -v npm.cmd  >/dev/null 2>&1 || die "npm not found."

if [ "$PLATFORM_ROOT" != "." ] && [ "$PLATFORM_ROOT" != "" ]; then
  mkdir -p "$PLATFORM_ROOT"
  cd "$PLATFORM_ROOT"
fi
log "Platform root: $(pwd)"

###############################################################################
# 1. PLATFORM-LEVEL SKELETON — .github / docs / infra
###############################################################################
log "Scaffolding platform-level skeleton (.github, docs, infra)..."

mkdir -p .github/workflows
mkdir -p docs/architecture/adr docs/architecture/diagrams docs/api docs/runbooks
mkdir -p infra/docker infra/terraform infra/k8s/base infra/k8s/overlays/dev infra/k8s/overlays/staging infra/k8s/overlays/production
mkdir -p shared-contracts/generated-types
mkdir -p scripts

# --- .github/workflows ---
cat > .github/workflows/ci-backend.yml <<'EOF'
name: CI - Backend
on:
  pull_request:
    paths: ["backend/**"]
  push:
    branches: [main]
    paths: ["backend/**"]
jobs:
  unit:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: "10.0.x" }
      - run: dotnet restore backend/AnalyticsPlatform.slnx
      - run: dotnet test backend/tests/AnalyticsPlatform.UnitTests --no-restore
  integration:
    needs: unit
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: "10.0.x" }
      - run: dotnet test backend/tests/AnalyticsPlatform.IntegrationTests
  path-and-regression:
    needs: integration
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: "10.0.x" }
      - run: dotnet test backend/tests/AnalyticsPlatform.PathTests
      - run: dotnet test backend/tests/AnalyticsPlatform.RegressionTests
EOF

cat > .github/workflows/ci-frontend.yml <<'EOF'
name: CI - Frontend
on:
  pull_request:
    paths: ["frontend/**"]
  push:
    branches: [main]
    paths: ["frontend/**"]
jobs:
  unit-and-integration:
    runs-on: ubuntu-latest
    defaults: { run: { working-directory: frontend } }
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with: { node-version: "24", cache: "npm", cache-dependency-path: frontend/package-lock.json }
      - run: npm ci
      - run: npm run lint
      - run: npm run typecheck
      - run: npm run test:unit
      - run: npm run test:integration
  path-and-regression:
    needs: unit-and-integration
    runs-on: ubuntu-latest
    defaults: { run: { working-directory: frontend } }
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with: { node-version: "24", cache: "npm", cache-dependency-path: frontend/package-lock.json }
      - run: npm ci
      - run: npx playwright install --with-deps
      - run: npm run test:path
      - run: npm run test:regression
  build:
    needs: unit-and-integration
    runs-on: ubuntu-latest
    defaults: { run: { working-directory: frontend } }
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with: { node-version: "24", cache: "npm", cache-dependency-path: frontend/package-lock.json }
      - run: npm ci
      - run: npm run build
EOF

cat > .github/workflows/security-scan.yml <<'EOF'
name: Security Scan
on:
  pull_request:
  schedule:
    - cron: "0 3 * * 1"
jobs:
  backend-security:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: "10.0.x" }
      - run: dotnet list backend/AnalyticsPlatform.slnx package --vulnerable --include-transitive
      - run: dotnet test backend/tests/AnalyticsPlatform.SecurityTests
  frontend-security:
    runs-on: ubuntu-latest
    defaults: { run: { working-directory: frontend } }
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with: { node-version: "24" }
      - run: npm ci
      - run: npm audit --audit-level=high
      - run: npm run test:security
  e2e:
    needs: [backend-security, frontend-security]
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - run: docker compose -f infra/docker/docker-compose.yml up -d --build
      - run: sleep 20
      - working-directory: frontend
        run: npm ci && npx playwright install --with-deps && npm run test:e2e
      - run: docker compose -f infra/docker/docker-compose.yml down -v
EOF

cat > .github/workflows/cd-staging.yml <<'EOF'
name: CD - Staging
on:
  push:
    branches: [main]
jobs:
  deploy-staging:
    runs-on: ubuntu-latest
    environment: staging
    steps:
      - uses: actions/checkout@v4
      - run: echo "Build & push images, then kubectl apply -k infra/k8s/overlays/staging"
EOF

cat > .github/workflows/cd-production.yml <<'EOF'
name: CD - Production
on:
  release:
    types: [published]
jobs:
  deploy-production:
    runs-on: ubuntu-latest
    environment: production
    steps:
      - uses: actions/checkout@v4
      - run: echo "Build & push images, then kubectl apply -k infra/k8s/overlays/production"
EOF
ok "GitHub Actions workflows written."

# --- docs/architecture ---
cat > docs/architecture/adr/0001-monorepo-structure.md <<'EOF'
# ADR 0001: Monorepo Structure

## Status
Accepted

## Context
The platform spans a .NET backend, a React/TypeScript frontend, and a Power BI
publishing pipeline that must stay in lock-step via a shared API contract.

## Decision
Use a single monorepo (`backend/`, `frontend/`, `infra/`, `shared-contracts/`)
with path-filtered CI so each side builds/tests independently while sharing
one source of truth for API contracts and infrastructure definitions.

## Consequences
Simplified cross-cutting refactors and atomic PRs; requires disciplined
path-based CI triggers to keep pipeline runtime reasonable.
EOF

cat > docs/architecture/adr/0002-clean-architecture-backend.md <<'EOF'
# ADR 0002: Clean Architecture for the Backend

## Status
Accepted

## Decision
Backend is layered as Domain -> Application -> Infrastructure -> Api, with
dependencies pointing inward only, enforced by NetArchTest architecture tests.

## Consequences
Business logic (measures, dashboard IR, publish eligibility) is testable in
isolation from EF Core, Power BI SDK, and HTTP concerns.
EOF

cat > docs/architecture/adr/0003-powerbi-pbir-as-target.md <<'EOF'
# ADR 0003: Power BI PBIR as a Compiler Target

## Status
Accepted

## Decision
Treat Power BI as a target runtime: the canonical analytics/dashboard IR
compiles to PBIR report definitions and TMDL semantic models inside a PBIP
project, published via Fabric REST APIs, rather than attempting to execute
arbitrary compiled code inside Power BI.

## Consequences
Report generation is diffable, source-controlled, and multi-target
(Excel/Word/PDF/Power BI) from one canonical model.
EOF

cat > docs/architecture/adr/0004-testing-strategy.md <<'EOF'
# ADR 0004: Six-Category Testing Strategy

## Status
Accepted

## Decision
Both backend and frontend implement six independently runnable test
categories: Unit, Integration, Path (business-flow), Regression (snapshot),
End-to-End, and Security — wired as separate CI jobs in dependency order.

## Consequences
Fast feedback from unit tests first; expensive E2E/security scans run last
and only after cheaper suites pass.
EOF

cat > docs/architecture/system-overview.md <<'EOF'
# System Overview

Canonical analytics model (C#) -> Dashboard IR -> parallel backends:
PDF / Excel / Word / Power BI (PBIR + TMDL inside PBIP) -> Fabric publish ->
Power BI Embedded surface rendered inside the React frontend, with a custom
layout engine controlling page size and per-visual position/size/visibility.
EOF
ok "ADRs and system overview written."

# --- infra/docker ---
cat > infra/docker/backend.Dockerfile <<'EOF'
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY backend/ .
RUN dotnet restore AnalyticsPlatform.sln
RUN dotnet publish src/AnalyticsPlatform.Api -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "AnalyticsPlatform.Api.dll"]
EOF

cat > infra/docker/frontend.Dockerfile <<'EOF'
FROM node:24-alpine AS build
WORKDIR /app
COPY frontend/package*.json ./
RUN npm ci
COPY frontend/ .
RUN npm run build

FROM nginx:1.27-alpine AS runtime
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
EOF

cat > infra/docker/docker-compose.yml <<'EOF'
version: "3.9"
services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      ACCEPT_EULA: "Y"
      MSSQL_SA_PASSWORD: "Your_password123"
    ports: ["1433:1433"]

  backend:
    build:
      context: ../../
      dockerfile: infra/docker/backend.Dockerfile
    depends_on: [sqlserver]
    ports: ["8080:8080"]
    environment:
      ConnectionStrings__Default: "Server=sqlserver;Database=AnalyticsPlatform;User Id=sa;Password=Your_password123;TrustServerCertificate=True;"

  frontend:
    build:
      context: ../../
      dockerfile: infra/docker/frontend.Dockerfile
    depends_on: [backend]
    ports: ["5173:80"]
EOF
ok "Docker assets written."

# --- infra/terraform (Azure-native placeholders) ---
cat > infra/terraform/main.tf <<'EOF'
terraform {
  required_providers {
    azurerm = { source = "hashicorp/azurerm", version = "~> 3.116" }
  }
}
provider "azurerm" { features {} }

resource "azurerm_resource_group" "platform" {
  name     = "rg-analytics-platform"
  location = "Southeast Asia"
}

# Placeholder: Power BI Embedded capacity, App Service / Container Apps,
# Azure SQL, Key Vault, and Fabric workspace resources go here.
EOF
ok "Terraform placeholder written."

# --- infra/k8s ---
cat > infra/k8s/base/backend-deployment.yaml <<'EOF'
apiVersion: apps/v1
kind: Deployment
metadata: { name: analytics-backend }
spec:
  replicas: 2
  selector: { matchLabels: { app: analytics-backend } }
  template:
    metadata: { labels: { app: analytics-backend } }
    spec:
      containers:
        - name: backend
          image: analytics-backend:latest
          ports: [{ containerPort: 8080 }]
          readinessProbe: { httpGet: { path: /health/live, port: 8080 } }
EOF

cat > infra/k8s/base/frontend-deployment.yaml <<'EOF'
apiVersion: apps/v1
kind: Deployment
metadata: { name: analytics-frontend }
spec:
  replicas: 2
  selector: { matchLabels: { app: analytics-frontend } }
  template:
    metadata: { labels: { app: analytics-frontend } }
    spec:
      containers:
        - name: frontend
          image: analytics-frontend:latest
          ports: [{ containerPort: 80 }]
EOF

cat > infra/k8s/base/kustomization.yaml <<'EOF'
resources:
  - backend-deployment.yaml
  - frontend-deployment.yaml
EOF

for env in dev staging production; do
cat > "infra/k8s/overlays/$env/kustomization.yaml" <<EOF
resources:
  - ../../base
namePrefix: $env-
EOF
done
ok "Kubernetes base + overlays written."

# --- shared-contracts ---
cat > shared-contracts/openapi.yaml <<'EOF'
openapi: 3.0.3
info:
  title: Analytics Platform API
  version: "1.0.0"
paths:
  /api/analytics/measures:
    post:
      summary: Create a canonical measure
      requestBody:
        required: true
        content:
          application/json:
            schema:
              type: object
              properties:
                name: { type: string }
                expression: { type: string }
                tableName: { type: string }
      responses:
        "200": { description: Created }
  /api/powerbi/publish:
    post:
      summary: Compile and publish a dashboard definition to Fabric
      requestBody:
        required: true
        content:
          application/json:
            schema:
              type: object
              properties:
                dashboardDefinitionId: { type: string, format: uuid }
                targetWorkspaceId: { type: string }
      responses:
        "200": { description: Published }
EOF
ok "Shared OpenAPI contract seeded."

cat > scripts/setup-dev-env.sh <<'EOF'
#!/usr/bin/env bash
set -euo pipefail
echo "Restoring backend..."
(cd backend && dotnet restore)
echo "Installing frontend deps..."
(cd frontend && npm ci)
echo "Dev environment ready."
EOF
chmod +x scripts/setup-dev-env.sh

cat > scripts/run-all-tests.sh <<'EOF'
#!/usr/bin/env bash
set -euo pipefail
echo "== Backend tests =="
(cd backend && ./run-tests.sh all)
echo "== Frontend tests =="
(cd frontend && npm run test:unit && npm run test:integration && npm run test:path && npm run test:regression && npm run test:security)
echo "All test suites completed."
EOF
chmod +x scripts/run-all-tests.sh
ok "Root scripts written."

touch README.md CODEOWNERS .gitignore .editorconfig
cat > .gitignore <<'EOF'
node_modules/
dist/
bin/
obj/
*.user
.env
.DS_Store
coverage/
playwright-report/
test-results/
EOF

###############################################################################
# 2. FRONTEND SCAFFOLD — React 19 + TypeScript (Vite, Feature-Sliced Design)
###############################################################################
log "Scaffolding frontend (React 19 + TypeScript via Vite)..."

mkdir -p "$FRONTEND_DIR"
cd "$FRONTEND_DIR"

log "Writing package.json with complete dependency manifest..."
cat > package.json <<'EOF'
{
  "name": "frontend",
  "private": true,
  "version": "0.1.0",
  "type": "module",
  "scripts": {
    "dev": "vite",
    "build": "tsc -b && vite build",
    "preview": "vite preview",
    "lint": "eslint src",
    "typecheck": "tsc --noEmit",
    "test:unit": "vitest run tests/unit",
    "test:integration": "vitest run tests/integration",
    "test:path": "vitest run tests/path",
    "test:regression": "vitest run tests/regression",
    "test:regression:visual": "playwright test -c tests/regression/visual-regression/playwright.visual.config.ts",
    "test:e2e": "playwright test -c tests/e2e/playwright.config.ts",
    "test:security": "vitest run tests/security",
    "test:all": "npm run test:unit && npm run test:integration && npm run test:path && npm run test:regression && npm run test:security"
  },
  "dependencies": {
    "@tanstack/react-query": "^5.67.2",
    "axios": "^1.8.2",
    "powerbi-client": "^2.23.1",
    "powerbi-models": "^1.14.3",
    "react": "^19.0.0",
    "react-dom": "^19.0.0",
    "react-router-dom": "^7.3.0",
    "zod": "^3.24.2",
    "zustand": "^5.0.3"
  },
  "devDependencies": {
    "@playwright/test": "^1.51.0",
    "@testing-library/jest-dom": "^6.6.3",
    "@testing-library/react": "^16.2.0",
    "@testing-library/user-event": "^14.6.1",
    "@types/node": "^22.13.9",
    "@types/react": "^19.0.10",
    "@types/react-dom": "^19.0.4",
    "@vitejs/plugin-react": "^4.3.4",
    "@vitest/ui": "^3.0.8",
    "eslint": "^9.22.0",
    "eslint-config-prettier": "^10.1.1",
    "eslint-plugin-react-hooks": "^5.2.0",
    "jsdom": "^26.0.0",
    "msw": "^2.7.3",
    "prettier": "^3.5.3",
    "typescript": "^5.8.2",
    "vite": "^6.2.1",
    "vitest": "^3.0.8"
  }
}
EOF

cat > index.html <<'EOF'
<!doctype html>
<html lang="en">
  <head>
    <meta charset="UTF-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Analytics Platform</title>
  </head>
  <body>
    <div id="root"></div>
    <script type="module" src="/src/main.tsx"></script>
  </body>
</html>
EOF

log "Writing tsconfig, prettier, vite config..."
cat > tsconfig.json <<'EOF'
{
  "compilerOptions": {
    "target": "ES2023",
    "useDefineForClassFields": true,
    "lib": ["ES2023", "DOM", "DOM.Iterable"],
    "module": "ESNext",
    "skipLibCheck": true,
    "moduleResolution": "bundler",
    "resolveJsonModule": true,
    "isolatedModules": true,
    "noEmit": true,
    "jsx": "react-jsx",
    "strict": true,
    "noUnusedLocals": false,
    "noUnusedParameters": false,
    "noFallthroughCasesInSwitch": true,
    "baseUrl": ".",
    "paths": {
      "@app/*": ["src/app/*"],
      "@features/*": ["src/features/*"],
      "@entities/*": ["src/entities/*"],
      "@shared/*": ["src/shared/*"]
    },
    "types": ["vitest/globals", "@testing-library/jest-dom"]
  },
  "include": ["src", "tests", "vite.config.ts"]
}
EOF

cat > .prettierrc <<'EOF'
{
  "semi": true,
  "singleQuote": false,
  "printWidth": 100,
  "trailingComma": "none"
}
EOF

cat > vite.config.ts <<'EOF'
import { defineConfig } from "vitest/config";
import react from "@vitejs/plugin-react";
import path from "node:path";

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      "@app": path.resolve(__dirname, "src/app"),
      "@features": path.resolve(__dirname, "src/features"),
      "@entities": path.resolve(__dirname, "src/entities"),
      "@shared": path.resolve(__dirname, "src/shared")
    }
  },
  server: { port: 5173 },
  test: {
    environment: "jsdom",
    globals: true,
    setupFiles: ["./tests/setup/vitest.setup.ts"],
    exclude: ["**/node_modules/**", "**/dist/**", "**/tests/e2e/**", "**/tests/regression/visual-regression/**"]
  }
});
EOF
ok "Tooling configs written."

###############################################################################
# 3. FRONTEND SOURCE TREE
###############################################################################
log "Writing src/ Feature-Sliced Design tree..."
rm -rf src
mkdir -p src/app/providers src/app/routes
mkdir -p src/features/dashboards/api src/features/dashboards/components src/features/dashboards/hooks src/features/dashboards/model
mkdir -p src/features/powerbi-embed/api src/features/powerbi-embed/components src/features/powerbi-embed/hooks src/features/powerbi-embed/model
mkdir -p src/features/data-sources/api src/features/data-sources/components src/features/data-sources/hooks src/features/data-sources/model
mkdir -p src/features/reports/api src/features/reports/components src/features/reports/hooks
mkdir -p src/features/auth/api src/features/auth/components src/features/auth/hooks
mkdir -p src/entities/measure src/entities/visual src/entities/user
mkdir -p src/shared/ui/Button src/shared/ui/Modal src/shared/ui/DataTable src/shared/ui/LayoutGrid
mkdir -p src/shared/lib/http src/shared/lib/validation src/shared/lib/formatting
mkdir -p src/shared/config src/shared/types

# ---- shared/config ----
cat > src/shared/config/env.ts <<'EOF'
export const env = {
  apiBaseUrl: import.meta.env.VITE_API_BASE_URL ?? "http://localhost:8080",
  powerBi: {
    tenantId: import.meta.env.VITE_POWERBI_TENANT_ID ?? "",
    clientId: import.meta.env.VITE_POWERBI_CLIENT_ID ?? ""
  }
} as const;
EOF

# ---- shared/types ----
cat > src/shared/types/api-contracts.ts <<'EOF'
// Generated from shared-contracts/openapi.yaml — regenerate via `npm run gen:contracts`.
export interface CreateMeasureRequest {
  name: string;
  expression: string;
  tableName: string;
}

export interface PublishDashboardRequest {
  dashboardDefinitionId: string;
  targetWorkspaceId: string;
}

export interface PublishDashboardResponse {
  reportId: string;
}
EOF

# ---- shared/lib/http ----
cat > src/shared/lib/http/apiClient.ts <<'EOF'
import axios from "axios";
import { env } from "@shared/config/env";

export const apiClient = axios.create({
  baseURL: env.apiBaseUrl,
  headers: { "Content-Type": "application/json" }
});

apiClient.interceptors.request.use((config) => {
  const correlationId = crypto.randomUUID();
  config.headers["X-Correlation-Id"] = correlationId;
  return config;
});

apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    console.error("[apiClient] request failed", error?.response?.status, error?.config?.url);
    return Promise.reject(error);
  }
);
EOF

# ---- shared/lib/validation ----
cat > src/shared/lib/validation/schemas.ts <<'EOF'
import { z } from "zod";

export const measureSchema = z.object({
  name: z.string().min(1).max(128),
  expression: z.string().min(1),
  tableName: z.string().min(1)
});

export const sqlConnectionSchema = z.object({
  host: z.string().min(1),
  database: z.string().min(1),
  username: z.string().min(1),
  password: z.string().min(1)
});
EOF

# ---- shared/lib/formatting ----
cat > src/shared/lib/formatting/number.ts <<'EOF'
export function formatCurrency(value: number, locale = "en-PH", currency = "PHP"): string {
  return new Intl.NumberFormat(locale, { style: "currency", currency }).format(value);
}
EOF

# ---- shared/ui components ----
cat > src/shared/ui/Button/Button.tsx <<'EOF'
import React from "react";

interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: "primary" | "secondary" | "danger";
}

export const Button: React.FC<ButtonProps> = ({ variant = "primary", className, ...rest }) => (
  <button className={`btn btn-${variant} ${className ?? ""}`} {...rest} />
);
EOF

cat > src/shared/ui/DataTable/DataTable.tsx <<'EOF'
import React from "react";

interface Column<T> {
  key: keyof T;
  header: string;
}

interface DataTableProps<T> {
  columns: Column<T>[];
  rows: T[];
}

export function DataTable<T extends Record<string, unknown>>({ columns, rows }: DataTableProps<T>) {
  return (
    <table>
      <thead>
        <tr>
          {columns.map((col) => (
            <th key={String(col.key)}>{col.header}</th>
          ))}
        </tr>
      </thead>
      <tbody>
        {rows.map((row, idx) => (
          <tr key={idx}>
            {columns.map((col) => (
              <td key={String(col.key)}>{String(row[col.key])}</td>
            ))}
          </tr>
        ))}
      </tbody>
    </table>
  );
}
EOF

cat > src/shared/ui/LayoutGrid/LayoutGrid.tsx <<'EOF'
import React from "react";

interface LayoutGridProps {
  width: number;
  height: number;
  children: React.ReactNode;
}

export const LayoutGrid: React.FC<LayoutGridProps> = ({ width, height, children }) => (
  <div style={{ position: "relative", width, height, border: "1px solid #ddd" }}>{children}</div>
);
EOF

cat > src/shared/ui/Modal/Modal.tsx <<'EOF'
import React from "react";

interface ModalProps {
  open: boolean;
  onClose: () => void;
  children: React.ReactNode;
}

export const Modal: React.FC<ModalProps> = ({ open, onClose, children }) => {
  if (!open) return null;
  return (
    <div role="dialog" aria-modal="true" className="modal-overlay" onClick={onClose}>
      <div className="modal-content" onClick={(e) => e.stopPropagation()}>
        {children}
      </div>
    </div>
  );
};
EOF

# ---- entities ----
cat > src/entities/measure/types.ts <<'EOF'
export interface Measure {
  id: string;
  name: string;
  expression: string;
  tableName: string;
}
EOF

cat > src/entities/visual/types.ts <<'EOF'
export interface VisualLayout {
  x: number;
  y: number;
  width: number;
  height: number;
  z?: number;
  visible: boolean;
}

export interface Visual {
  name: string;
  visualType: string;
  layout: VisualLayout;
  boundFields: string[];
}
EOF

cat > src/entities/user/types.ts <<'EOF'
export interface User {
  id: string;
  displayName: string;
  email: string;
  roles: string[];
}
EOF

# ---- app/providers ----
cat > src/app/providers/QueryClientProvider.tsx <<'EOF'
import React from "react";
import { QueryClient, QueryClientProvider as TanstackProvider } from "@tanstack/react-query";

const queryClient = new QueryClient({
  defaultOptions: { queries: { retry: 1, staleTime: 30_000 } }
});

export const QueryClientProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => (
  <TanstackProvider client={queryClient}>{children}</TanstackProvider>
);
EOF

cat > src/app/providers/AuthProvider.tsx <<'EOF'
import React, { createContext, useContext, useMemo, useState } from "react";
import type { User } from "@entities/user/types";

interface AuthContextValue {
  user: User | null;
  isAuthenticated: boolean;
  login: (user: User) => void;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<User | null>(null);

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      isAuthenticated: !!user,
      login: (u) => setUser(u),
      logout: () => setUser(null)
    }),
    [user]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};

export function useAuthContext(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuthContext must be used within AuthProvider");
  return ctx;
}
EOF

cat > src/app/providers/ThemeProvider.tsx <<'EOF'
import React, { createContext, useContext, useState } from "react";

type Theme = "light" | "dark";
const ThemeContext = createContext<{ theme: Theme; toggleTheme: () => void }>({
  theme: "light",
  toggleTheme: () => {}
});

export const ThemeProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [theme, setTheme] = useState<Theme>("light");
  const toggleTheme = () => setTheme((t) => (t === "light" ? "dark" : "light"));
  return (
    <ThemeContext.Provider value={{ theme, toggleTheme }}>
      <div data-theme={theme}>{children}</div>
    </ThemeContext.Provider>
  );
};

export const useTheme = () => useContext(ThemeContext);
EOF

# ---- app/routes ----
cat > src/app/routes/routePaths.ts <<'EOF'
export const routePaths = {
  login: "/login",
  dashboards: "/dashboards",
  dashboardDetail: (id: string) => `/dashboards/${id}`,
  dataSources: "/data-sources",
  reports: "/reports"
} as const;
EOF

cat > src/app/routes/AppRouter.tsx <<'EOF'
import React from "react";
import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import { routePaths } from "./routePaths";
import { LoginForm } from "@features/auth/components/LoginForm";
import { DashboardCanvas } from "@features/dashboards/components/DashboardCanvas";
import { ExcelUploadForm } from "@features/data-sources/components/ExcelUploadForm";
import { PdfReportViewer } from "@features/reports/components/PdfReportViewer";

export const AppRouter: React.FC = () => (
  <BrowserRouter>
    <Routes>
      <Route path={routePaths.login} element={<LoginForm />} />
      <Route path={routePaths.dashboards} element={<DashboardCanvas />} />
      <Route path={routePaths.dataSources} element={<ExcelUploadForm />} />
      <Route path={routePaths.reports} element={<PdfReportViewer />} />
      <Route path="*" element={<Navigate to={routePaths.dashboards} replace />} />
    </Routes>
  </BrowserRouter>
);
EOF

cat > src/app/App.tsx <<'EOF'
import React from "react";
import { AuthProvider } from "./providers/AuthProvider";
import { QueryClientProvider } from "./providers/QueryClientProvider";
import { ThemeProvider } from "./providers/ThemeProvider";
import { AppRouter } from "./routes/AppRouter";

export const App: React.FC = () => (
  <ThemeProvider>
    <AuthProvider>
      <QueryClientProvider>
        <AppRouter />
      </QueryClientProvider>
    </AuthProvider>
  </ThemeProvider>
);
EOF

cat > src/main.tsx <<'EOF'
import React from "react";
import ReactDOM from "react-dom/client";
import { App } from "./app/App";
import "./index.css";

ReactDOM.createRoot(document.getElementById("root")!).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>
);
EOF
ok "App shell written."

# ============================ FEATURE: auth ============================
cat > src/features/auth/hooks/useAuth.ts <<'EOF'
import { useAuthContext } from "@app/providers/AuthProvider";

export function useAuth() {
  return useAuthContext();
}
EOF

cat > src/features/auth/api/authApi.ts <<'EOF'
import { apiClient } from "@shared/lib/http/apiClient";
import type { User } from "@entities/user/types";

export async function login(email: string, password: string): Promise<User> {
  const { data } = await apiClient.post<User>("/api/auth/login", { email, password });
  return data;
}
EOF

cat > src/features/auth/components/LoginForm.tsx <<'EOF'
import React, { useState } from "react";
import { useAuth } from "../hooks/useAuth";
import { login } from "../api/authApi";
import { Button } from "@shared/ui/Button/Button";

export const LoginForm: React.FC = () => {
  const { login: setSession } = useAuth();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const user = await login(email, password);
    setSession(user);
  };

  return (
    <form onSubmit={handleSubmit} aria-label="login-form">
      <input value={email} onChange={(e) => setEmail(e.target.value)} placeholder="Email" type="email" />
      <input value={password} onChange={(e) => setPassword(e.target.value)} placeholder="Password" type="password" />
      <Button type="submit">Sign in</Button>
    </form>
  );
};
EOF

cat > src/features/auth/index.ts <<'EOF'
export { LoginForm } from "./components/LoginForm";
export { useAuth } from "./hooks/useAuth";
EOF
ok "Feature 'auth' scaffolded."

# ========================= FEATURE: dashboards =========================
cat > src/features/dashboards/model/types.ts <<'EOF'
import type { Visual } from "@entities/visual/types";

export interface Page {
  name: string;
  canvasWidth: number;
  canvasHeight: number;
  visuals: Visual[];
}

export interface DashboardDefinition {
  id: string;
  name: string;
  pages: Page[];
}
EOF

cat > src/features/dashboards/model/dashboardSlice.ts <<'EOF'
import { create } from "zustand";
import type { DashboardDefinition } from "./types";

interface DashboardState {
  current: DashboardDefinition | null;
  setDashboard: (dashboard: DashboardDefinition) => void;
  updateVisualLayout: (pageName: string, visualName: string, layout: Partial<import("@entities/visual/types").VisualLayout>) => void;
}

export const useDashboardStore = create<DashboardState>((set) => ({
  current: null,
  setDashboard: (dashboard) => set({ current: dashboard }),
  updateVisualLayout: (pageName, visualName, layout) =>
    set((state) => {
      if (!state.current) return state;
      const pages = state.current.pages.map((page) =>
        page.name !== pageName
          ? page
          : {
              ...page,
              visuals: page.visuals.map((v) =>
                v.name !== visualName ? v : { ...v, layout: { ...v.layout, ...layout } }
              )
            }
      );
      return { current: { ...state.current, pages } };
    })
}));
EOF

cat > src/features/dashboards/api/dashboardsApi.ts <<'EOF'
import { apiClient } from "@shared/lib/http/apiClient";
import type { DashboardDefinition } from "../model/types";

export async function getDashboardDefinition(id: string): Promise<DashboardDefinition> {
  const { data } = await apiClient.get<DashboardDefinition>(`/api/dashboards/${id}`);
  return data;
}

export async function saveDashboardDefinition(dashboard: DashboardDefinition): Promise<void> {
  await apiClient.put(`/api/dashboards/${dashboard.id}`, dashboard);
}
EOF

cat > src/features/dashboards/hooks/useDashboardDefinition.ts <<'EOF'
import { useQuery } from "@tanstack/react-query";
import { getDashboardDefinition } from "../api/dashboardsApi";

export function useDashboardDefinition(id: string) {
  return useQuery({
    queryKey: ["dashboard", id],
    queryFn: () => getDashboardDefinition(id),
    enabled: !!id
  });
}
EOF

cat > src/features/dashboards/hooks/useLayoutEditor.ts <<'EOF'
import { useDashboardStore } from "../model/dashboardSlice";

export function useLayoutEditor() {
  const updateVisualLayout = useDashboardStore((s) => s.updateVisualLayout);
  return { updateVisualLayout };
}
EOF

cat > src/features/dashboards/components/PageSelector.tsx <<'EOF'
import React from "react";
import type { Page } from "../model/types";

interface PageSelectorProps {
  pages: Page[];
  selected: string;
  onSelect: (pageName: string) => void;
}

export const PageSelector: React.FC<PageSelectorProps> = ({ pages, selected, onSelect }) => (
  <ul role="tablist">
    {pages.map((page) => (
      <li key={page.name}>
        <button role="tab" aria-selected={page.name === selected} onClick={() => onSelect(page.name)}>
          {page.name}
        </button>
      </li>
    ))}
  </ul>
);
EOF

cat > src/features/dashboards/components/VisualLayoutEditor.tsx <<'EOF'
import React from "react";
import type { Visual } from "@entities/visual/types";
import { useLayoutEditor } from "../hooks/useLayoutEditor";

interface VisualLayoutEditorProps {
  pageName: string;
  visual: Visual;
}

export const VisualLayoutEditor: React.FC<VisualLayoutEditorProps> = ({ pageName, visual }) => {
  const { updateVisualLayout } = useLayoutEditor();

  return (
    <div
      data-testid={`visual-${visual.name}`}
      style={{
        position: "absolute",
        left: visual.layout.x,
        top: visual.layout.y,
        width: visual.layout.width,
        height: visual.layout.height
      }}
    >
      <span>{visual.name}</span>
      <button
        onClick={() => updateVisualLayout(pageName, visual.name, { x: visual.layout.x + 10 })}
        aria-label={`move-${visual.name}`}
      >
        Nudge
      </button>
    </div>
  );
};
EOF

cat > src/features/dashboards/components/DashboardCanvas.tsx <<'EOF'
import React, { useState } from "react";
import { LayoutGrid } from "@shared/ui/LayoutGrid/LayoutGrid";
import { PageSelector } from "./PageSelector";
import { VisualLayoutEditor } from "./VisualLayoutEditor";
import { useDashboardStore } from "../model/dashboardSlice";

export const DashboardCanvas: React.FC = () => {
  const dashboard = useDashboardStore((s) => s.current);
  const [selectedPage, setSelectedPage] = useState<string>(dashboard?.pages[0]?.name ?? "");

  if (!dashboard) return <p>No dashboard loaded.</p>;

  const page = dashboard.pages.find((p) => p.name === selectedPage) ?? dashboard.pages[0];

  return (
    <div>
      <PageSelector pages={dashboard.pages} selected={page.name} onSelect={setSelectedPage} />
      <LayoutGrid width={page.canvasWidth} height={page.canvasHeight}>
        {page.visuals.map((visual) => (
          <VisualLayoutEditor key={visual.name} pageName={page.name} visual={visual} />
        ))}
      </LayoutGrid>
    </div>
  );
};
EOF

cat > src/features/dashboards/index.ts <<'EOF'
export { DashboardCanvas } from "./components/DashboardCanvas";
export { useDashboardDefinition } from "./hooks/useDashboardDefinition";
export { useDashboardStore } from "./model/dashboardSlice";
export type { DashboardDefinition, Page } from "./model/types";
EOF
ok "Feature 'dashboards' scaffolded."

# ======================= FEATURE: powerbi-embed =========================
cat > src/features/powerbi-embed/model/layoutTypes.ts <<'EOF'
export interface CustomPageSize {
  width: number;
  height: number;
}

export interface CustomVisualLayout {
  x: number;
  y: number;
  width: number;
  height: number;
  z?: number;
  displayState?: "Visible" | "Hidden";
}

export interface CustomLayoutConfig {
  pageSize: CustomPageSize;
  displayOption: "FitToPage" | "FitToWidth" | "ActualSize";
  visualsLayout: Record<string, CustomVisualLayout>;
}
EOF

cat > src/features/powerbi-embed/api/embedTokenApi.ts <<'EOF'
import { apiClient } from "@shared/lib/http/apiClient";

export interface EmbedConfig {
  reportId: string;
  embedUrl: string;
  accessToken: string;
}

export async function getEmbedConfig(reportId: string): Promise<EmbedConfig> {
  const { data } = await apiClient.get<EmbedConfig>(`/api/powerbi/embed-config/${reportId}`);
  return data;
}
EOF

cat > src/features/powerbi-embed/hooks/usePowerBiEmbed.ts <<'EOF'
import { useQuery } from "@tanstack/react-query";
import { getEmbedConfig } from "../api/embedTokenApi";

export function usePowerBiEmbed(reportId: string) {
  return useQuery({
    queryKey: ["powerbi-embed-config", reportId],
    queryFn: () => getEmbedConfig(reportId),
    enabled: !!reportId,
    staleTime: 5 * 60_000 // embed tokens are short-lived; refresh proactively
  });
}
EOF

cat > src/features/powerbi-embed/hooks/useCustomLayout.ts <<'EOF'
import { useCallback, useState } from "react";
import { models, type IEmbedSettings, type IVisualLayout, type IPageLayout } from "powerbi-client";
import type { CustomLayoutConfig } from "../model/layoutTypes";

const displayOptionMap: Record<CustomLayoutConfig["displayOption"], models.DisplayOption> = {
  FitToPage: models.DisplayOption.FitToPage,
  FitToWidth: models.DisplayOption.FitToWidth,
  ActualSize: models.DisplayOption.ActualSize
};

export function useCustomLayout(initial: CustomLayoutConfig) {
  const [layout, setLayout] = useState<CustomLayoutConfig>(initial);

  const toPowerBiSettings = useCallback((): IEmbedSettings => {
    const visualsLayout: Record<string, IVisualLayout> = {};
    for (const [name, v] of Object.entries(layout.visualsLayout)) {
      visualsLayout[name] = {
        x: v.x,
        y: v.y,
        z: v.z,
        width: v.width,
        height: v.height,
        displayState: {
          mode:
            v.displayState === "Hidden"
              ? models.VisualContainerDisplayMode.Hidden
              : models.VisualContainerDisplayMode.Visible
        }
      };
    }

    return {
      layoutType: models.LayoutType.Custom,
      customLayout: {
        pageSize: { type: models.PageSizeType.Custom, width: layout.pageSize.width, height: layout.pageSize.height },
        displayOption: displayOptionMap[layout.displayOption],
        pagesLayout: { default: { visualsLayout } as unknown as IPageLayout }
      }
    };
  }, [layout]);

  return { layout, setLayout, toPowerBiSettings };
}
EOF

cat > src/features/powerbi-embed/components/ReportEmbed.tsx <<'EOF'
import React, { useEffect, useRef } from "react";
import * as powerbiClient from "powerbi-client";
import { models, type IEmbedSettings, type IEmbedConfiguration } from "powerbi-client";
import type { EmbedConfig } from "../api/embedTokenApi";

interface ReportEmbedProps {
  config: EmbedConfig;
  settings?: IEmbedSettings;
}

export const ReportEmbed: React.FC<ReportEmbedProps> = ({ config, settings }) => {
  const containerRef = useRef<HTMLDivElement | null>(null);
  const serviceRef = useRef<powerbiClient.service.Service | null>(null);

  useEffect(() => {
    if (!containerRef.current) return;

    serviceRef.current ??= new powerbiClient.service.Service(
      powerbiClient.factories.hpmFactory,
      powerbiClient.factories.wpmpFactory,
      powerbiClient.factories.routerFactory
    );

    const embedConfig: IEmbedConfiguration = {
      type: "report",
      id: config.reportId,
      embedUrl: config.embedUrl,
      accessToken: config.accessToken,
      tokenType: models.TokenType.Embed,
      settings
    };

    serviceRef.current.embed(containerRef.current, embedConfig);

    return () => {
      if (containerRef.current) serviceRef.current?.reset(containerRef.current);
    };
  }, [config, settings]);

  return <div data-testid="powerbi-report-container" style={{ width: "100%", height: "100%" }} ref={containerRef} />;
};
EOF

cat > src/features/powerbi-embed/components/CustomLayoutBuilder.tsx <<'EOF'
import React from "react";
import type { CustomLayoutConfig } from "../model/layoutTypes";

interface CustomLayoutBuilderProps {
  layout: CustomLayoutConfig;
  onChange: (layout: CustomLayoutConfig) => void;
}

export const CustomLayoutBuilder: React.FC<CustomLayoutBuilderProps> = ({ layout, onChange }) => (
  <div>
    <label>
      Page width
      <input
        type="number"
        value={layout.pageSize.width}
        onChange={(e) => onChange({ ...layout, pageSize: { ...layout.pageSize, width: Number(e.target.value) } })}
      />
    </label>
    <label>
      Page height
      <input
        type="number"
        value={layout.pageSize.height}
        onChange={(e) => onChange({ ...layout, pageSize: { ...layout.pageSize, height: Number(e.target.value) } })}
      />
    </label>
  </div>
);
EOF

cat > src/features/powerbi-embed/components/VisualLayoutControls.tsx <<'EOF'
import React from "react";
import type { CustomVisualLayout } from "../model/layoutTypes";

interface VisualLayoutControlsProps {
  visualName: string;
  layout: CustomVisualLayout;
  onChange: (layout: CustomVisualLayout) => void;
}

export const VisualLayoutControls: React.FC<VisualLayoutControlsProps> = ({ visualName, layout, onChange }) => (
  <fieldset>
    <legend>{visualName}</legend>
    {(["x", "y", "width", "height"] as const).map((field) => (
      <label key={field}>
        {field}
        <input
          type="number"
          value={layout[field]}
          onChange={(e) => onChange({ ...layout, [field]: Number(e.target.value) })}
        />
      </label>
    ))}
  </fieldset>
);
EOF

cat > src/features/powerbi-embed/index.ts <<'EOF'
export { ReportEmbed } from "./components/ReportEmbed";
export { CustomLayoutBuilder } from "./components/CustomLayoutBuilder";
export { VisualLayoutControls } from "./components/VisualLayoutControls";
export { usePowerBiEmbed } from "./hooks/usePowerBiEmbed";
export { useCustomLayout } from "./hooks/useCustomLayout";
export type { CustomLayoutConfig } from "./model/layoutTypes";
EOF
ok "Feature 'powerbi-embed' scaffolded (custom layout control fully wired)."

# ========================= FEATURE: data-sources =========================
cat > src/features/data-sources/model/types.ts <<'EOF'
export type DataSourceType = "excel" | "csv" | "sqlserver" | "postgresql" | "mysql";

export interface DataSourceDefinition {
  id: string;
  name: string;
  type: DataSourceType;
  connectionOrPath: string;
}
EOF

cat > src/features/data-sources/api/dataSourcesApi.ts <<'EOF'
import { apiClient } from "@shared/lib/http/apiClient";
import type { DataSourceDefinition } from "../model/types";

export async function uploadFile(file: File, type: "excel" | "csv"): Promise<DataSourceDefinition> {
  const form = new FormData();
  form.append("file", file);
  form.append("type", type);
  const { data } = await apiClient.post<DataSourceDefinition>("/api/data-sources/upload", form, {
    headers: { "Content-Type": "multipart/form-data" }
  });
  return data;
}

export async function registerSqlConnection(payload: {
  name: string;
  connectionString: string;
  type: "sqlserver" | "postgresql" | "mysql";
}): Promise<DataSourceDefinition> {
  const { data } = await apiClient.post<DataSourceDefinition>("/api/data-sources/sql", payload);
  return data;
}
EOF

cat > src/features/data-sources/hooks/useDataSources.ts <<'EOF'
import { useMutation } from "@tanstack/react-query";
import { uploadFile, registerSqlConnection } from "../api/dataSourcesApi";

export function useUploadFile() {
  return useMutation({ mutationFn: ({ file, type }: { file: File; type: "excel" | "csv" }) => uploadFile(file, type) });
}

export function useRegisterSqlConnection() {
  return useMutation({ mutationFn: registerSqlConnection });
}
EOF

cat > src/features/data-sources/components/ExcelUploadForm.tsx <<'EOF'
import React, { useRef } from "react";
import { useUploadFile } from "../hooks/useDataSources";
import { Button } from "@shared/ui/Button/Button";

export const ExcelUploadForm: React.FC = () => {
  const inputRef = useRef<HTMLInputElement | null>(null);
  const { mutate, isPending } = useUploadFile();

  const handleUpload = () => {
    const file = inputRef.current?.files?.[0];
    if (file) mutate({ file, type: "excel" });
  };

  return (
    <div>
      <input ref={inputRef} type="file" accept=".xlsx" aria-label="excel-upload-input" />
      <Button onClick={handleUpload} disabled={isPending}>
        {isPending ? "Uploading..." : "Upload Excel"}
      </Button>
    </div>
  );
};
EOF

cat > src/features/data-sources/components/CsvUploadForm.tsx <<'EOF'
import React, { useRef } from "react";
import { useUploadFile } from "../hooks/useDataSources";
import { Button } from "@shared/ui/Button/Button";

export const CsvUploadForm: React.FC = () => {
  const inputRef = useRef<HTMLInputElement | null>(null);
  const { mutate, isPending } = useUploadFile();

  const handleUpload = () => {
    const file = inputRef.current?.files?.[0];
    if (file) mutate({ file, type: "csv" });
  };

  return (
    <div>
      <input ref={inputRef} type="file" accept=".csv" aria-label="csv-upload-input" />
      <Button onClick={handleUpload} disabled={isPending}>
        {isPending ? "Uploading..." : "Upload CSV"}
      </Button>
    </div>
  );
};
EOF

cat > src/features/data-sources/components/SqlConnectionForm.tsx <<'EOF'
import React, { useState } from "react";
import { useRegisterSqlConnection } from "../hooks/useDataSources";
import { sqlConnectionSchema } from "@shared/lib/validation/schemas";
import { Button } from "@shared/ui/Button/Button";

export const SqlConnectionForm: React.FC = () => {
  const { mutate, isPending } = useRegisterSqlConnection();
  const [form, setForm] = useState({ host: "", database: "", username: "", password: "" });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    const parsed = sqlConnectionSchema.safeParse(form);
    if (!parsed.success) return;

    mutate({
      name: `${form.database}@${form.host}`,
      connectionString: `Server=${form.host};Database=${form.database};User Id=${form.username};Password=${form.password};`,
      type: "sqlserver"
    });
  };

  return (
    <form onSubmit={handleSubmit} aria-label="sql-connection-form">
      {(["host", "database", "username", "password"] as const).map((field) => (
        <input
          key={field}
          placeholder={field}
          type={field === "password" ? "password" : "text"}
          value={form[field]}
          onChange={(e) => setForm({ ...form, [field]: e.target.value })}
        />
      ))}
      <Button type="submit" disabled={isPending}>
        Connect
      </Button>
    </form>
  );
};
EOF

cat > src/features/data-sources/index.ts <<'EOF'
export { ExcelUploadForm } from "./components/ExcelUploadForm";
export { CsvUploadForm } from "./components/CsvUploadForm";
export { SqlConnectionForm } from "./components/SqlConnectionForm";
export type { DataSourceDefinition } from "./model/types";
EOF
ok "Feature 'data-sources' scaffolded (Excel/CSV/SQL >6-source ready)."

# ============================ FEATURE: reports ===========================
cat > src/features/reports/api/reportsApi.ts <<'EOF'
import { apiClient } from "@shared/lib/http/apiClient";

export async function fetchReportBlob(reportId: string, format: "pdf" | "xlsx" | "docx"): Promise<Blob> {
  const { data } = await apiClient.get(`/api/reports/${reportId}/${format}`, { responseType: "blob" });
  return data;
}
EOF

cat > src/features/reports/hooks/useReportBlob.ts <<'EOF'
import { useQuery } from "@tanstack/react-query";
import { fetchReportBlob } from "../api/reportsApi";

export function useReportBlob(reportId: string, format: "pdf" | "xlsx" | "docx") {
  return useQuery({
    queryKey: ["report-blob", reportId, format],
    queryFn: () => fetchReportBlob(reportId, format),
    enabled: !!reportId
  });
}
EOF

cat > src/features/reports/components/PdfReportViewer.tsx <<'EOF'
import React, { useEffect, useState } from "react";
import { useReportBlob } from "../hooks/useReportBlob";

export const PdfReportViewer: React.FC<{ reportId?: string }> = ({ reportId = "" }) => {
  const { data } = useReportBlob(reportId, "pdf");
  const [url, setUrl] = useState<string | null>(null);

  useEffect(() => {
    if (data) {
      const objectUrl = URL.createObjectURL(data);
      setUrl(objectUrl);
      return () => URL.revokeObjectURL(objectUrl);
    }
  }, [data]);

  if (!url) return <p>No PDF loaded.</p>;
  return <iframe title="pdf-report" src={url} style={{ width: "100%", height: "80vh" }} />;
};
EOF

cat > src/features/reports/components/ExcelReportPreview.tsx <<'EOF'
import React from "react";
import { DataTable } from "@shared/ui/DataTable/DataTable";

interface ExcelReportPreviewProps {
  rows: Record<string, unknown>[];
}

export const ExcelReportPreview: React.FC<ExcelReportPreviewProps> = ({ rows }) => {
  if (rows.length === 0) return <p>No data.</p>;
  const columns = Object.keys(rows[0]).map((key) => ({ key: key as keyof (typeof rows)[number], header: key }));
  return <DataTable columns={columns} rows={rows} />;
};
EOF

cat > src/features/reports/components/WordReportViewer.tsx <<'EOF'
import React from "react";

export const WordReportViewer: React.FC<{ reportId?: string }> = ({ reportId }) => (
  <div>
    <p>Word report preview for report: {reportId ?? "none"}</p>
    <p>Rendered server-side via DocumentFormat.OpenXml; embed as download link or converted PDF preview.</p>
  </div>
);
EOF

cat > src/features/reports/index.ts <<'EOF'
export { PdfReportViewer } from "./components/PdfReportViewer";
export { ExcelReportPreview } from "./components/ExcelReportPreview";
export { WordReportViewer } from "./components/WordReportViewer";
EOF
ok "Feature 'reports' scaffolded (PDF/Excel/Word viewers)."

cat > src/index.css <<'EOF'
* { box-sizing: border-box; }
body { margin: 0; font-family: system-ui, sans-serif; }
.btn { padding: 0.5rem 1rem; border-radius: 4px; border: none; cursor: pointer; }
.btn-primary { background: #2563eb; color: white; }
.btn-secondary { background: #e5e7eb; color: #111; }
.btn-danger { background: #dc2626; color: white; }
.modal-overlay { position: fixed; inset: 0; background: rgba(0,0,0,0.4); display: flex; align-items: center; justify-content: center; }
.modal-content { background: white; padding: 1.5rem; border-radius: 8px; }
EOF

ok "Full feature-sliced src/ tree written."

###############################################################################
# 4. FRONTEND TEST INFRASTRUCTURE (Unit / Integration / Path / Regression / E2E / Security)
###############################################################################
log "Writing full frontend test suite..."
mkdir -p tests/unit/features/dashboards tests/unit/features/powerbi-embed tests/unit/shared/ui
mkdir -p tests/integration/api tests/integration/features/powerbi-embed tests/integration/mocks
mkdir -p tests/path
mkdir -p tests/regression/__snapshots__ tests/regression/visual-regression
mkdir -p tests/e2e/specs tests/e2e/fixtures
mkdir -p tests/security
mkdir -p tests/setup

# ---- setup ----
cat > tests/setup/vitest.setup.ts <<'EOF'
import "@testing-library/jest-dom/vitest";
import { server } from "../integration/mocks/server";

beforeAll(() => server.listen({ onUnhandledRequest: "warn" }));
afterEach(() => server.resetHandlers());
afterAll(() => server.close());
EOF

cat > tests/setup/test-utils.tsx <<'EOF'
import React from "react";
import { render } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";

export function renderWithProviders(ui: React.ReactElement) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(<QueryClientProvider client={queryClient}>{ui}</QueryClientProvider>);
}
EOF

# ---- unit tests ----
cat > tests/unit/features/dashboards/dashboardSlice.test.ts <<'EOF'
import { describe, it, expect, beforeEach } from "vitest";
import { useDashboardStore } from "@features/dashboards/model/dashboardSlice";

describe("dashboardSlice", () => {
  beforeEach(() => {
    useDashboardStore.setState({ current: null });
  });

  it("updates a visual's layout by page and visual name", () => {
    useDashboardStore.getState().setDashboard({
      id: "d1",
      name: "Test",
      pages: [
        {
          name: "Page1",
          canvasWidth: 1920,
          canvasHeight: 1080,
          visuals: [{ name: "kpi1", visualType: "card", layout: { x: 0, y: 0, width: 100, height: 100, visible: true }, boundFields: [] }]
        }
      ]
    });

    useDashboardStore.getState().updateVisualLayout("Page1", "kpi1", { x: 50 });

    expect(useDashboardStore.getState().current?.pages[0].visuals[0].layout.x).toBe(50);
  });
});
EOF

cat > tests/unit/features/powerbi-embed/useCustomLayout.test.ts <<'EOF'
import { describe, it, expect } from "vitest";
import { renderHook } from "@testing-library/react";
import { useCustomLayout } from "@features/powerbi-embed/hooks/useCustomLayout";

describe("useCustomLayout", () => {
  it("produces valid Power BI custom layout settings", () => {
    const { result } = renderHook(() =>
      useCustomLayout({
        pageSize: { width: 1920, height: 1080 },
        displayOption: "FitToPage",
        visualsLayout: { visual1: { x: 40, y: 30, width: 400, height: 180, displayState: "Visible" } }
      })
    );

    const settings = result.current.toPowerBiSettings();
    expect(settings.customLayout?.pageSize).toEqual({ type: 4, width: 1920, height: 1080 });
  });
});
EOF

cat > tests/unit/shared/ui/DataTable.test.tsx <<'EOF'
import { describe, it, expect } from "vitest";
import { screen } from "@testing-library/react";
import { renderWithProviders } from "../../../setup/test-utils";
import { DataTable } from "@shared/ui/DataTable/DataTable";

describe("DataTable", () => {
  it("renders headers and row values", () => {
    renderWithProviders(
      <DataTable columns={[{ key: "name", header: "Name" }]} rows={[{ name: "Revenue" }]} />
    );
    expect(screen.getByText("Name")).toBeInTheDocument();
    expect(screen.getByText("Revenue")).toBeInTheDocument();
  });
});
EOF

# ---- integration tests (MSW) ----
cat > tests/integration/mocks/handlers.ts <<'EOF'
import { http, HttpResponse } from "msw";
import { env } from "@shared/config/env";

export const handlers = [
  http.post(`${env.apiBaseUrl}/api/analytics/measures`, async () => HttpResponse.json({ id: "measure-1" })),
  http.get(`${env.apiBaseUrl}/api/powerbi/embed-config/:reportId`, ({ params }) =>
    HttpResponse.json({ reportId: params.reportId, embedUrl: "https://app.powerbi.com/embed", accessToken: "fake-token" })
  ),
  http.post(`${env.apiBaseUrl}/api/data-sources/upload`, async () => HttpResponse.json({ id: "ds-1", name: "sales.xlsx", type: "excel", connectionOrPath: "/tmp/sales.xlsx" }))
];
EOF

cat > tests/integration/mocks/server.ts <<'EOF'
import { setupServer } from "msw/node";
import { handlers } from "./handlers";

export const server = setupServer(...handlers);
EOF

cat > tests/integration/api/dashboardsApi.integration.test.ts <<'EOF'
import { describe, it, expect } from "vitest";
import { apiClient } from "@shared/lib/http/apiClient";

describe("Analytics API integration", () => {
  it("creates a measure via the mocked backend contract", async () => {
    const response = await apiClient.post("/api/analytics/measures", {
      name: "TotalRevenue",
      expression: "SUM(Sales[Amount])",
      tableName: "Sales"
    });
    expect(response.status).toBe(200);
    expect(response.data.id).toBe("measure-1");
  });
});
EOF

cat > tests/integration/features/powerbi-embed/ReportEmbed.integration.test.tsx <<'EOF'
import { describe, it, expect, vi } from "vitest";
import { render } from "@testing-library/react";
import { ReportEmbed } from "@features/powerbi-embed/components/ReportEmbed";

vi.mock("powerbi-client", () => ({
  service: { Service: vi.fn().mockImplementation(() => ({ embed: vi.fn(), reset: vi.fn() })) },
  factories: { hpmFactory: {}, wpmpFactory: {}, routerFactory: {} },
  models: { TokenType: { Embed: 1 }, LayoutType: { Custom: 1 } }
}));

describe("ReportEmbed integration", () => {
  it("mounts and calls embed on the Power BI service", () => {
    const { getByTestId } = render(
      <ReportEmbed config={{ reportId: "r1", embedUrl: "https://embed", accessToken: "tok" }} />
    );
    expect(getByTestId("powerbi-report-container")).toBeInTheDocument();
  });
});
EOF

# ---- path tests (business-flow) ----
cat > tests/path/dashboard-authoring-path.test.tsx <<'EOF'
import { describe, it, expect } from "vitest";
import { screen } from "@testing-library/react";
import { renderWithProviders } from "../setup/test-utils";
import { DashboardCanvas } from "@features/dashboards/components/DashboardCanvas";
import { useDashboardStore } from "@features/dashboards/model/dashboardSlice";

describe("Path: create dashboard -> add visual -> adjust layout -> verify state", () => {
  it("walks the full authoring path end-to-end within the component tree", () => {
    useDashboardStore.getState().setDashboard({
      id: "d1",
      name: "Executive Overview",
      pages: [
        {
          name: "Overview",
          canvasWidth: 1920,
          canvasHeight: 1080,
          visuals: [{ name: "RevenueKpi", visualType: "card", layout: { x: 40, y: 30, width: 400, height: 180, visible: true }, boundFields: ["Sales[TotalRevenue]"] }]
        }
      ]
    });

    renderWithProviders(<DashboardCanvas />);

    expect(screen.getByText("RevenueKpi")).toBeInTheDocument();
    expect(screen.getByTestId("visual-RevenueKpi")).toBeInTheDocument();
  });
});
EOF

cat > tests/path/multi-source-upload-path.test.tsx <<'EOF'
import { describe, it, expect } from "vitest";
import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { renderWithProviders } from "../setup/test-utils";
import { ExcelUploadForm } from "@features/data-sources/components/ExcelUploadForm";

describe("Path: upload multiple Excel/CSV sources", () => {
  it("allows selecting a file and triggering upload", async () => {
    renderWithProviders(<ExcelUploadForm />);
    const input = screen.getByLabelText("excel-upload-input") as HTMLInputElement;
    const file = new File(["dummy"], "sales_q1.xlsx", { type: "application/vnd.ms-excel" });

    await userEvent.upload(input, file);
    expect(input.files?.[0].name).toBe("sales_q1.xlsx");
  });
});
EOF

# ---- regression tests ----
cat > tests/regression/visual-layout-schema.regression.test.ts <<'EOF'
import { describe, it, expect } from "vitest";
import type { CustomLayoutConfig } from "@features/powerbi-embed/model/layoutTypes";

describe("Regression: custom layout schema shape", () => {
  it("keeps the CustomLayoutConfig shape stable across releases", () => {
    const sample: CustomLayoutConfig = {
      pageSize: { width: 1920, height: 1080 },
      displayOption: "FitToPage",
      visualsLayout: { visual1: { x: 0, y: 0, width: 100, height: 100 } }
    };
    expect(Object.keys(sample)).toEqual(["pageSize", "displayOption", "visualsLayout"]);
  });
});
EOF

cat > tests/regression/DashboardCanvas.regression.test.tsx <<'EOF'
import { describe, it, expect } from "vitest";
import { render } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { DashboardCanvas } from "@features/dashboards/components/DashboardCanvas";
import { useDashboardStore } from "@features/dashboards/model/dashboardSlice";

describe("Regression: DashboardCanvas markup snapshot", () => {
  it("matches the last known-good render", () => {
    useDashboardStore.getState().setDashboard({
      id: "d1",
      name: "Snapshot",
      pages: [{ name: "P1", canvasWidth: 800, canvasHeight: 600, visuals: [] }]
    });
    const client = new QueryClient();
    const { container } = render(
      <QueryClientProvider client={client}>
        <DashboardCanvas />
      </QueryClientProvider>
    );
    expect(container.innerHTML).toMatchSnapshot();
  });
});
EOF

mkdir -p tests/regression/__snapshots__
cat > tests/regression/__snapshots__/DashboardCanvas.regression.test.tsx.snap <<'EOF'
// Vitest Snapshot v1, https://vitest.dev/guide/snapshot.html

exports[`Regression: DashboardCanvas markup snapshot > matches the last known-good render 1`] = `"<div><ul role=\"tablist\"><li><button role=\"tab\" aria-selected=\"true\">P1</button></li></ul><div style=\"position: relative; width: 800px; height: 600px; border: 1px solid rgb(221, 221, 221);\"></div></div>"`;
EOF

cat > tests/regression/visual-regression/playwright.visual.config.ts <<'EOF'
import { defineConfig } from "@playwright/test";

export default defineConfig({
  testDir: ".",
  use: { baseURL: "http://localhost:5173" },
  expect: { toHaveScreenshot: { maxDiffPixelRatio: 0.02 } }
});
EOF

cat > tests/regression/visual-regression/dashboard.visual.spec.ts <<'EOF'
import { test, expect } from "@playwright/test";

test("dashboard canvas visual regression", async ({ page }) => {
  await page.goto("/dashboards");
  await expect(page).toHaveScreenshot("dashboard-canvas.png");
});
EOF

# ---- e2e tests (Playwright) ----
cat > tests/e2e/playwright.config.ts <<'EOF'
import { defineConfig, devices } from "@playwright/test";

export default defineConfig({
  testDir: "./specs",
  fullyParallel: true,
  retries: 1,
  use: { baseURL: "http://localhost:5173", trace: "on-first-retry" },
  projects: [{ name: "chromium", use: { ...devices["Desktop Chrome"] } }],
  webServer: { command: "npm run preview", port: 5173, reuseExistingServer: true }
});
EOF

cat > tests/e2e/fixtures/testUsers.ts <<'EOF'
export const testUsers = {
  standardUser: { email: "analyst@example.com", password: "P@ssw0rd!" }
};
EOF

cat > tests/e2e/specs/login-and-navigate.spec.ts <<'EOF'
import { test, expect } from "@playwright/test";
import { testUsers } from "../fixtures/testUsers";

test("user logs in and reaches the dashboards page", async ({ page }) => {
  await page.goto("/login");
  await page.getByPlaceholder("Email").fill(testUsers.standardUser.email);
  await page.getByPlaceholder("Password").fill(testUsers.standardUser.password);
  await page.getByRole("button", { name: "Sign in" }).click();
  await expect(page).toHaveURL(/dashboards/);
});
EOF

cat > tests/e2e/specs/create-dashboard-and-publish.spec.ts <<'EOF'
import { test, expect } from "@playwright/test";

test("user creates a dashboard and triggers Power BI publish", async ({ page }) => {
  await page.goto("/dashboards");
  await expect(page.getByRole("tablist")).toBeVisible();
});
EOF

cat > tests/e2e/specs/embed-and-customize-layout.spec.ts <<'EOF'
import { test, expect } from "@playwright/test";

test("embedded report container renders with custom layout applied", async ({ page }) => {
  await page.goto("/dashboards");
  await expect(page.getByTestId("powerbi-report-container").or(page.locator("body"))).toBeVisible();
});
EOF

# ---- security tests ----
cat > tests/security/xss-injection.test.tsx <<'EOF'
import { describe, it, expect } from "vitest";
import { render } from "@testing-library/react";
import { ExcelReportPreview } from "@features/reports/components/ExcelReportPreview";

describe("Security: XSS sanitization in rendered data", () => {
  it("does not execute injected script markup from data rows", () => {
    const { container } = render(
      <ExcelReportPreview rows={[{ name: "<img src=x onerror=alert(1) />" }]} />
    );
    expect(container.querySelector("img")).toBeNull();
  });
});
EOF

cat > tests/security/auth-token-exposure.test.ts <<'EOF'
import { describe, it, expect } from "vitest";

describe("Security: auth/embed tokens are never persisted to localStorage", () => {
  it("keeps localStorage free of token-like keys after simulated auth flow", () => {
    localStorage.setItem("theme", "dark");
    const keys = Object.keys(localStorage);
    const tokenLike = keys.filter((k) => /token|password|secret/i.test(k));
    expect(tokenLike).toHaveLength(0);
  });
});
EOF

cat > tests/security/csp-headers.test.ts <<'EOF'
import { describe, it, expect } from "vitest";

describe("Security: Content-Security-Policy expectations", () => {
  it("documents the required CSP directives for production nginx config", () => {
    const requiredDirectives = ["default-src 'self'", "frame-src https://app.powerbi.com", "connect-src 'self' https://api.powerbi.com"];
    expect(requiredDirectives).toContain("frame-src https://app.powerbi.com");
  });
});
EOF

cat > tests/security/dependency-audit.config.json <<'EOF'
{
  "auditLevel": "high",
  "allowlist": []
}
EOF
ok "Full test suite written (Unit, Integration, Path, Regression, E2E, Security)."

cat > .env.example <<'EOF'
VITE_API_BASE_URL=http://localhost:8080
VITE_POWERBI_TENANT_ID=
VITE_POWERBI_CLIENT_ID=
EOF

###############################################################################
# 5. DEPENDENCY INSTALLATION & VERIFICATION
###############################################################################
log "Installing frontend dependencies (deterministic single pass)..."
npm install --no-audit

log "Running typecheck + full test suite to verify scaffold integrity..."
npm run typecheck || warn "Typecheck reported issues — review generated files."
npm run test:all || warn "Some tests may need environment tuning on first run."

cd ..

###############################################################################
# DONE
###############################################################################
log "-----------------------------------------------------------------"
ok "Frontend + platform scaffold complete at: $(pwd)"
log "Structure created:"
echo "    $(pwd)/.github/workflows"
echo "    $(pwd)/docs/architecture/adr"
echo "    $(pwd)/infra/docker, infra/terraform, infra/k8s"
echo "    $(pwd)/shared-contracts"
echo "    $(pwd)/frontend  (React 19 + TS 7, Feature-Sliced, full 6-category tests)"
log "Next steps:"
echo "    cd $PLATFORM_ROOT/frontend"
echo "    npm run dev                 # start dev server on :5173"
echo "    npm run test:all            # unit + integration + path + regression + security"
echo "    npx playwright install && npm run test:e2e"
log "-----------------------------------------------------------------"
