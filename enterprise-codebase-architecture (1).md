# Enterprise Codebase Infrastructure Design
## C# Analytics Framework + React/TypeScript UI + Power BI Target Runtime

**Architecture Owner:** Principal Software Engineer / Principal Software Architect mindset
**Stack:** React 19.3 + TypeScript 7.0 (Frontend) | .NET 10 LTS (Backend) | Power BI PBIP/PBIR (Analytics Target)
**Design Philosophy:** Feature-Sliced, Clean/Hexagonal Architecture, Test Pyramid + Test Diamond, CI/CD-native

---

## 1. Architectural Principles (Non-Negotiables)

Before the folder structure, the principles that justify every design decision below:

1. **Separation of concerns by feature, not by technical layer alone.** Each feature (Dashboards, Datasets, PowerBiPublishing, Reporting, Auth) owns its own controllers/components, services, models, and tests. This prevents the "1000-file `Controllers/` folder" anti-pattern.
2. **Dependency Inversion at every boundary.** Domain/Core logic never depends on Infrastructure (SQL, Excel readers, Power BI SDK, HTTP). Infrastructure depends on Core via interfaces.
3. **Single Source of Truth for business logic.** The canonical analytics model (Domain layer) is the only place computations happen — Power BI, Excel, PDF, Word are all *rendering targets*, never logic owners.
4. **Testability is a first-class architectural constraint**, not an afterthought. If a class is hard to unit test, the architecture is wrong, not the test.
5. **Every layer has an explicit contract (interface/DTO), never leaked implementation types.**
6. **Fail loud in dev, fail safe in prod.** Structured logging, correlation IDs, and health checks are part of the skeleton, not bolted on later.
7. **CI must be able to run each test category independently** (unit vs integration vs e2e) so failures are diagnosed fast — this is why the testing directory structure mirrors the test pyramid explicitly.

---

## 2. High-Level Repository Layout (Monorepo)

```
analytics-platform/
├── .github/
│   └── workflows/
│       ├── ci-backend.yml
│       ├── ci-frontend.yml
│       ├── cd-staging.yml
│       ├── cd-production.yml
│       └── security-scan.yml
├── docs/
│   ├── architecture/
│   │   ├── adr/                         # Architecture Decision Records
│   │   │   ├── 0001-monorepo-structure.md
│   │   │   ├── 0002-clean-architecture-backend.md
│   │   │   ├── 0003-powerbi-pbir-as-target.md
│   │   │   └── 0004-testing-strategy.md
│   │   ├── diagrams/
│   │   └── system-overview.md
│   ├── api/
│   └── runbooks/
├── infra/
│   ├── docker/
│   │   ├── backend.Dockerfile
│   │   ├── frontend.Dockerfile
│   │   └── docker-compose.yml
│   ├── terraform/                       # or bicep/ for Azure-native IaC
│   └── k8s/
│       ├── base/
│       └── overlays/
│           ├── dev/
│           ├── staging/
│           └── production/
├── backend/                             # .NET 10 solution — Section 3
├── frontend/                            # React 19 + TS 7 app — Section 4
├── shared-contracts/                    # OpenAPI/schema shared between FE & BE
│   ├── openapi.yaml
│   └── generated-types/
├── scripts/
│   ├── setup-dev-env.sh
│   ├── seed-test-data.sh
│   └── run-all-tests.sh
├── .editorconfig
├── .gitignore
├── CODEOWNERS
└── README.md
```

**Rationale:** A monorepo with clear `backend/`, `frontend/`, `infra/`, `shared-contracts/` boundaries lets one PR touch a full feature slice (API + UI + tests) while CI pipelines still run backend/frontend independently via path filters.

---

## 3. Backend Infrastructure (.NET 10 — Clean/Hexagonal Architecture)

### 3.1 Solution Structure

```
backend/
├── AnalyticsPlatform.sln
├── Directory.Build.props                # shared MSBuild settings, nullable, analyzers
├── Directory.Packages.props              # centralized NuGet version management
├── src/
│   ├── AnalyticsPlatform.Domain/                    # ── CORE (no external deps) ──
│   │   ├── Common/
│   │   │   ├── Entity.cs
│   │   │   ├── ValueObject.cs
│   │   │   ├── Result.cs
│   │   │   └── DomainException.cs
│   │   ├── Features/
│   │   │   ├── Analytics/
│   │   │   │   ├── Entities/
│   │   │   │   │   ├── Measure.cs
│   │   │   │   │   ├── Dimension.cs
│   │   │   │   │   └── AnalyticsModel.cs
│   │   │   │   ├── ValueObjects/
│   │   │   │   │   ├── MeasureExpression.cs
│   │   │   │   │   └── Relationship.cs
│   │   │   │   └── Rules/
│   │   │   │       └── MeasureValidationRules.cs
│   │   │   ├── Dashboards/
│   │   │   │   ├── Entities/
│   │   │   │   │   ├── DashboardDefinition.cs
│   │   │   │   │   ├── Page.cs
│   │   │   │   │   ├── Visual.cs
│   │   │   │   │   └── LayoutSpec.cs
│   │   │   │   └── Rules/
│   │   │   │       └── LayoutBoundsRules.cs
│   │   │   ├── DataSources/
│   │   │   │   ├── Entities/
│   │   │   │   │   ├── DataSourceDefinition.cs   # Excel | CSV | SQL
│   │   │   │   │   └── DatasetSchema.cs
│   │   │   │   └── Rules/
│   │   │   │       └── SchemaCompatibilityRules.cs
│   │   │   └── ReportPublishing/
│   │   │       ├── Entities/
│   │   │       │   └── PublishRequest.cs
│   │   │       └── Rules/
│   │   │           └── PublishEligibilityRules.cs
│   │   └── AnalyticsPlatform.Domain.csproj
│   │
│   ├── AnalyticsPlatform.Application/               # ── USE CASES / ORCHESTRATION ──
│   │   ├── Common/
│   │   │   ├── Behaviors/                # MediatR pipeline behaviors
│   │   │   │   ├── ValidationBehavior.cs
│   │   │   │   ├── LoggingBehavior.cs
│   │   │   │   └── PerformanceBehavior.cs
│   │   │   ├── Interfaces/
│   │   │   │   ├── IDateTimeProvider.cs
│   │   │   │   ├── ICurrentUserContext.cs
│   │   │   │   └── IUnitOfWork.cs
│   │   │   └── Mappings/
│   │   ├── Features/
│   │   │   ├── Analytics/
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── CreateMeasure/
│   │   │   │   │   │   ├── CreateMeasureCommand.cs
│   │   │   │   │   │   ├── CreateMeasureCommandHandler.cs
│   │   │   │   │   │   └── CreateMeasureCommandValidator.cs
│   │   │   │   │   └── ValidateAnalyticsModel/
│   │   │   │   └── Queries/
│   │   │   │       └── GetAnalyticsModel/
│   │   │   ├── Dashboards/
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── CreateDashboardDefinition/
│   │   │   │   │   └── CompileDashboardIR/
│   │   │   │   └── Queries/
│   │   │   │       └── GetDashboardDefinition/
│   │   │   ├── DataSources/
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── RegisterExcelSource/
│   │   │   │   │   ├── RegisterCsvSource/
│   │   │   │   │   └── RegisterSqlConnection/
│   │   │   │   └── Queries/
│   │   │   │       └── ValidateDatasetSchema/
│   │   │   ├── ReportGeneration/
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── GenerateExcelReport/
│   │   │   │   │   ├── GenerateWordReport/
│   │   │   │   │   └── GeneratePdfReport/
│   │   │   ├── PowerBiPublishing/
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── CompilePbirDefinition/
│   │   │   │   │   ├── CompileTmdlSemanticModel/
│   │   │   │   │   ├── PublishPbipToFabric/
│   │   │   │   │   └── GenerateEmbedToken/
│   │   │   │   └── Queries/
│   │   │   │       └── GetReportEmbedConfig/
│   │   │   └── Auth/
│   │   │       ├── Commands/
│   │   │       └── Queries/
│   │   └── AnalyticsPlatform.Application.csproj
│   │
│   ├── AnalyticsPlatform.Infrastructure/            # ── EXTERNAL ADAPTERS ──
│   │   ├── Persistence/
│   │   │   ├── AppDbContext.cs
│   │   │   ├── Configurations/
│   │   │   ├── Migrations/
│   │   │   └── Repositories/
│   │   │       ├── AnalyticsModelRepository.cs
│   │   │       └── DashboardRepository.cs
│   │   ├── DataSourceConnectors/
│   │   │   ├── Excel/
│   │   │   │   └── ExcelDataSourceReader.cs        # EPPlus/ClosedXML
│   │   │   ├── Csv/
│   │   │   │   └── CsvDataSourceReader.cs          # CsvHelper
│   │   │   └── Sql/
│   │   │       ├── SqlServerConnector.cs
│   │   │       ├── PostgresConnector.cs
│   │   │       └── MySqlConnector.cs
│   │   ├── PowerBi/
│   │   │   ├── PbirGenerator.cs                    # IR -> PBIR JSON
│   │   │   ├── TmdlGenerator.cs                    # IR -> TMDL
│   │   │   ├── PbipPackager.cs                     # Assembles .pbip project
│   │   │   ├── FabricRestClient.cs                 # Fabric/Power BI REST calls
│   │   │   └── EmbedTokenService.cs
│   │   ├── DocumentGenerators/
│   │   │   ├── Excel/ExcelReportGenerator.cs
│   │   │   ├── Word/WordReportGenerator.cs
│   │   │   └── Pdf/PdfReportGenerator.cs
│   │   ├── Identity/
│   │   │   └── AzureAdAuthProvider.cs
│   │   └── AnalyticsPlatform.Infrastructure.csproj
│   │
│   └── AnalyticsPlatform.Api/                       # ── PRESENTATION / ENTRYPOINT ──
│       ├── Endpoints/
│       │   ├── AnalyticsEndpoints.cs
│       │   ├── DashboardEndpoints.cs
│       │   ├── DataSourceEndpoints.cs
│       │   └── PowerBiEndpoints.cs
│       ├── Middleware/
│       │   ├── ExceptionHandlingMiddleware.cs
│       │   ├── CorrelationIdMiddleware.cs
│       │   └── RateLimitingMiddleware.cs
│       ├── Filters/
│       ├── HealthChecks/
│       │   ├── SqlHealthCheck.cs
│       │   └── PowerBiHealthCheck.cs
│       ├── Program.cs
│       ├── appsettings.json
│       ├── appsettings.Development.json
│       └── AnalyticsPlatform.Api.csproj
│
└── tests/                                # ── SEE SECTION 5 (TESTING) ──
```

### 3.2 Backend Layer Dependency Rules

| Layer | May depend on | Must NOT depend on |
|---|---|---|
| Domain | Nothing (pure C#) | Application, Infrastructure, Api, EF Core, any SDK |
| Application | Domain | Infrastructure, Api |
| Infrastructure | Domain, Application (interfaces only) | Api |
| Api | Application, Infrastructure (composition root only) | — |

This is enforced with **architecture tests** (Section 5.1.4) using `NetArchTest` so a violation fails CI, not code review.

---

## 4. Frontend Infrastructure (React 19 + TypeScript 7 — Feature-Sliced Design)

### 4.1 Directory Structure

```
frontend/
├── package.json
├── tsconfig.json
├── vite.config.ts
├── .eslintrc.cjs
├── .prettierrc
├── src/
│   ├── app/                              # App shell: providers, router, store
│   │   ├── providers/
│   │   │   ├── AuthProvider.tsx
│   │   │   ├── QueryClientProvider.tsx
│   │   │   └── ThemeProvider.tsx
│   │   ├── routes/
│   │   │   ├── AppRouter.tsx
│   │   │   └── routePaths.ts
│   │   └── App.tsx
│   │
│   ├── features/                         # ── FEATURE MODULES ──
│   │   ├── dashboards/
│   │   │   ├── api/
│   │   │   │   └── dashboardsApi.ts
│   │   │   ├── components/
│   │   │   │   ├── DashboardCanvas.tsx
│   │   │   │   ├── VisualLayoutEditor.tsx
│   │   │   │   └── PageSelector.tsx
│   │   │   ├── hooks/
│   │   │   │   ├── useDashboardDefinition.ts
│   │   │   │   └── useLayoutEditor.ts
│   │   │   ├── model/
│   │   │   │   ├── types.ts
│   │   │   │   └── dashboardSlice.ts
│   │   │   └── index.ts                  # public API of the feature
│   │   │
│   │   ├── powerbi-embed/
│   │   │   ├── api/
│   │   │   │   └── embedTokenApi.ts
│   │   │   ├── components/
│   │   │   │   ├── ReportEmbed.tsx
│   │   │   │   ├── VisualLayoutControls.tsx
│   │   │   │   └── CustomLayoutBuilder.tsx
│   │   │   ├── hooks/
│   │   │   │   ├── usePowerBiEmbed.ts
│   │   │   │   └── useCustomLayout.ts
│   │   │   ├── model/
│   │   │   │   └── layoutTypes.ts
│   │   │   └── index.ts
│   │   │
│   │   ├── data-sources/
│   │   │   ├── api/
│   │   │   ├── components/
│   │   │   │   ├── ExcelUploadForm.tsx
│   │   │   │   ├── CsvUploadForm.tsx
│   │   │   │   └── SqlConnectionForm.tsx
│   │   │   ├── hooks/
│   │   │   └── model/
│   │   │
│   │   ├── reports/
│   │   │   ├── api/
│   │   │   ├── components/
│   │   │   │   ├── ExcelReportPreview.tsx
│   │   │   │   ├── PdfReportViewer.tsx
│   │   │   │   └── WordReportViewer.tsx
│   │   │   └── hooks/
│   │   │
│   │   └── auth/
│   │       ├── api/
│   │       ├── components/
│   │       │   └── LoginForm.tsx
│   │       └── hooks/
│   │           └── useAuth.ts
│   │
│   ├── entities/                         # Cross-feature domain objects (UI-level)
│   │   ├── measure/
│   │   ├── visual/
│   │   └── user/
│   │
│   ├── shared/                           # ── SHARED KERNEL ──
│   │   ├── ui/                           # Design system components
│   │   │   ├── Button/
│   │   │   ├── Modal/
│   │   │   ├── DataTable/
│   │   │   └── LayoutGrid/
│   │   ├── lib/
│   │   │   ├── http/
│   │   │   │   └── apiClient.ts
│   │   │   ├── validation/
│   │   │   └── formatting/
│   │   ├── config/
│   │   │   └── env.ts
│   │   └── types/
│   │       └── api-contracts.ts          # generated from shared-contracts/openapi.yaml
│   │
│   └── main.tsx
│
├── public/
└── tests/                                # ── SEE SECTION 5 (TESTING) ──
```

### 4.2 Frontend Architectural Rules

- **Feature isolation:** a feature (`features/dashboards`) may import from `shared/` and `entities/`, but never directly from another feature's internals — only via that feature's `index.ts` public API.
- **No business logic in components.** Components render; `hooks/` and `model/` own logic and state; `api/` owns network calls typed against `shared/types/api-contracts.ts` (generated from the same OpenAPI contract the backend publishes — this keeps FE/BE in sync automatically).
- **State management:** local/server state via TanStack Query (or RTK Query); global UI state via lightweight store (Zustand/Redux Toolkit) scoped per feature.

---

## 5. Testing Infrastructure (Both Frontend and Backend)

The testing directory is organized to mirror **six explicit test categories** requested: Unit, Integration, Path (functional/business-path) testing, Regression, End-to-End, and Security — each runnable independently in CI and locally.

### 5.1 Backend Test Structure

```
backend/tests/
├── AnalyticsPlatform.UnitTests/
│   ├── Domain/
│   │   ├── Analytics/MeasureValidationRulesTests.cs
│   │   ├── Dashboards/LayoutBoundsRulesTests.cs
│   │   └── DataSources/SchemaCompatibilityRulesTests.cs
│   ├── Application/
│   │   ├── Analytics/CreateMeasureCommandHandlerTests.cs
│   │   ├── Dashboards/CompileDashboardIRHandlerTests.cs
│   │   └── PowerBiPublishing/CompilePbirDefinitionHandlerTests.cs
│   ├── Infrastructure/
│   │   ├── PowerBi/PbirGeneratorTests.cs
│   │   ├── PowerBi/TmdlGeneratorTests.cs
│   │   └── DataSourceConnectors/ExcelDataSourceReaderTests.cs
│   └── AnalyticsPlatform.UnitTests.csproj      # xUnit + FluentAssertions + NSubstitute
│
├── AnalyticsPlatform.IntegrationTests/
│   ├── Api/
│   │   ├── DashboardEndpointsTests.cs           # WebApplicationFactory + real DB (Testcontainers)
│   │   ├── DataSourceEndpointsTests.cs
│   │   └── PowerBiEndpointsTests.cs
│   ├── Persistence/
│   │   └── AnalyticsModelRepositoryTests.cs     # Testcontainers: SQL Server/Postgres
│   ├── ExternalServices/
│   │   ├── FabricRestClientTests.cs             # WireMock.Net stub of Fabric API
│   │   └── SqlConnectorIntegrationTests.cs      # against real containerized SQL
│   ├── Fixtures/
│   │   ├── DatabaseFixture.cs
│   │   └── PowerBiApiMockFixture.cs
│   └── AnalyticsPlatform.IntegrationTests.csproj
│
├── AnalyticsPlatform.PathTests/                  # ── Business-path / functional flow tests ──
│   ├── DashboardCreationPathTests.cs             # e.g. Create model -> Compile IR -> Generate PBIR
│   ├── MultiSourceIngestionPathTests.cs          # Excel + CSV + SQL merged into one model
│   ├── PublishToFabricPathTests.cs               # Full publish pipeline, happy + branch paths
│   └── AnalyticsPlatform.PathTests.csproj
│
├── AnalyticsPlatform.RegressionTests/
│   ├── Snapshots/
│   │   ├── PbirOutput/                           # golden-file JSON snapshots
│   │   └── TmdlOutput/
│   ├── PbirGenerationRegressionTests.cs          # Verify.Xunit snapshot comparisons
│   ├── ReportCalculationRegressionTests.cs       # Ensures measure results don't silently drift
│   └── AnalyticsPlatform.RegressionTests.csproj
│
├── AnalyticsPlatform.E2ETests/
│   ├── Scenarios/
│   │   ├── FullPublishWorkflow.feature           # SpecFlow/Reqnroll BDD scenario
│   │   └── DashboardEditToEmbedWorkflow.feature
│   ├── StepDefinitions/
│   ├── Environment/
│   │   └── docker-compose.e2e.yml                # spins up API + DB + mock Power BI
│   └── AnalyticsPlatform.E2ETests.csproj
│
├── AnalyticsPlatform.SecurityTests/
│   ├── AuthZ/
│   │   ├── RoleBasedAccessTests.cs               # verify RLS-equivalent access rules
│   │   └── EmbedTokenScopeTests.cs               # tokens don't leak cross-tenant access
│   ├── InputValidation/
│   │   ├── SqlInjectionTests.cs                  # parametrized query fuzzing
│   │   └── SchemaInjectionTests.cs               # malicious Excel/CSV payloads
│   ├── DependencyScan/
│   │   └── (invoked via `dotnet list package --vulnerable` in CI, not test code)
│   ├── ZapBaselineScan/
│   │   └── zap-baseline.conf                     # OWASP ZAP baseline scan config
│   └── AnalyticsPlatform.SecurityTests.csproj
│
└── Directory.Build.props                         # shared test SDK versions across all test projects
```

**Backend test tooling:** xUnit, FluentAssertions, NSubstitute (mocking), Testcontainers (real DB/SQL in integration tests), WireMock.Net (stub Fabric/Power BI REST), Verify.Xunit (snapshot/regression testing), Reqnroll/SpecFlow (BDD E2E), OWASP ZAP + `dotnet list package --vulnerable` + Roslyn security analyzers (security testing).

### 5.2 Frontend Test Structure

```
frontend/tests/
├── unit/
│   ├── features/
│   │   ├── dashboards/DashboardCanvas.test.tsx
│   │   ├── powerbi-embed/useCustomLayout.test.ts
│   │   └── data-sources/SqlConnectionForm.test.tsx
│   └── shared/
│       ├── ui/DataTable.test.tsx
│       └── lib/apiClient.test.ts
│                                                  # Vitest + React Testing Library
│
├── integration/
│   ├── api/
│   │   ├── dashboardsApi.integration.test.ts     # MSW (Mock Service Worker) against contract
│   │   └── embedTokenApi.integration.test.ts
│   ├── features/
│   │   └── powerbi-embed/ReportEmbed.integration.test.tsx  # mounts with mocked powerbi-client
│   └── mocks/
│       ├── handlers.ts                           # MSW request handlers
│       └── server.ts
│
├── path/                                          # ── Functional/business-path tests ──
│   ├── dashboard-authoring-path.test.tsx          # Create dashboard -> add visuals -> layout -> save
│   ├── multi-source-upload-path.test.tsx          # Upload 6+ Excel/CSV + configure SQL, end state
│   └── embed-customization-path.test.tsx          # Apply custom layout -> verify rendered state
│
├── regression/
│   ├── __snapshots__/
│   ├── DashboardCanvas.regression.test.tsx        # Snapshot testing (Vitest snapshot)
│   ├── visual-layout-schema.regression.test.ts    # Ensures layout JSON shape doesn't silently change
│   └── visual-regression/
│       ├── playwright.visual.config.ts
│       └── dashboard.visual.spec.ts               # Playwright + pixel-diff visual regression
│
├── e2e/
│   ├── playwright.config.ts
│   ├── specs/
│   │   ├── login-and-navigate.spec.ts
│   │   ├── create-dashboard-and-publish.spec.ts
│   │   └── embed-and-customize-layout.spec.ts
│   └── fixtures/
│       └── testUsers.ts
│
├── security/
│   ├── xss-injection.test.tsx                     # sanitization of user-authored dashboard text
│   ├── auth-token-exposure.test.ts                # tokens never logged/exposed in DOM/localStorage misuse
│   ├── dependency-audit.config.json                # npm audit / Snyk config
│   └── csp-headers.test.ts                        # verifies Content-Security-Policy enforcement
│
├── setup/
│   ├── vitest.setup.ts
│   └── test-utils.tsx                              # custom render() with providers
│
└── package.json                                   # scripts: test:unit, test:integration, test:path,
                                                     # test:regression, test:e2e, test:security
```

**Frontend test tooling:** Vitest + React Testing Library (unit), MSW (integration mocking), Playwright (E2E + visual regression), `npm audit`/Snyk + custom CSP/XSS assertions (security).

### 5.3 Test Category Definitions (What Each Directory Actually Verifies)

| Category | Backend example | Frontend example | Runs in CI on |
|---|---|---|---|
| **Unit** | `MeasureValidationRulesTests` validates a single domain rule in isolation | `useCustomLayout.test.ts` verifies hook logic with no network | Every commit |
| **Integration** | `AnalyticsModelRepositoryTests` against a real containerized SQL instance | `dashboardsApi.integration.test.ts` against MSW-mocked contract | Every PR |
| **Path** | `MultiSourceIngestionPathTests` walks Excel+CSV+SQL merge end-to-end within the service layer | `multi-source-upload-path.test.tsx` walks the UI flow of uploading 6 files and configuring a DB connection | Every PR |
| **Regression** | `PbirGenerationRegressionTests` diffs generated PBIR JSON against golden snapshots | `DashboardCanvas.regression.test.tsx` + Playwright visual diff | Nightly + pre-release |
| **End-to-End** | `FullPublishWorkflow.feature` — compile IR → publish to Fabric → verify report exists | `create-dashboard-and-publish.spec.ts` — full browser flow against staging | Pre-release / nightly |
| **Security** | `RoleBasedAccessTests`, ZAP baseline scan, `dotnet list package --vulnerable` | XSS/CSP tests, `npm audit`/Snyk | Every PR + scheduled weekly |

### 5.4 Test Pyramid Ratio (Guardrail)

```
                /\
               /E2E\          <- few, slow, highest confidence (full workflow)
              /------\
             / Path & \
            /Regression\      <- moderate count, verify business flows & prevent drift
           /------------\
          / Integration  \    <- verify real boundaries (DB, HTTP, Power BI API)
         /----------------\
        /       Unit       \  <- majority of tests, fast, isolated
       /--------------------\
```

Enforce this ratio via CI test-count reporting: unit tests should outnumber integration tests roughly 3–5x, and integration should outnumber E2E similarly, keeping the suite fast while still catching real defects.

---

## 6. CI/CD Pipeline Mapping to Test Categories

```yaml
# .github/workflows/ci-backend.yml (excerpt)
jobs:
  unit:
    steps: [ dotnet test tests/AnalyticsPlatform.UnitTests ]
  integration:
    needs: unit
    services: { sqlserver, mock-fabric-api }
    steps: [ dotnet test tests/AnalyticsPlatform.IntegrationTests ]
  path-tests:
    needs: integration
    steps: [ dotnet test tests/AnalyticsPlatform.PathTests ]
  regression:
    needs: path-tests
    steps: [ dotnet test tests/AnalyticsPlatform.RegressionTests ]
  security:
    needs: unit
    steps:
      - dotnet list package --vulnerable --include-transitive
      - dotnet test tests/AnalyticsPlatform.SecurityTests
      - run: zap-baseline.py -t $STAGING_URL
  e2e:
    needs: [integration, security]
    steps: [ dotnet test tests/AnalyticsPlatform.E2ETests ]
```

Frontend pipeline mirrors this with `npm run test:unit`, `test:integration`, `test:path`, `test:regression`, `test:security`, `test:e2e` (Playwright), gated in the same dependency order — fast feedback first, expensive/slow suites last.

---

## 7. Observability, Debuggability & Maintainability Hooks

- **Correlation IDs** injected at `CorrelationIdMiddleware` (backend) and propagated via request headers from `apiClient.ts` (frontend), so a single user action can be traced across FE → API → Power BI publish pipeline in logs.
- **Structured logging** (Serilog on backend, console/log-shipping on frontend) with feature-tagged log scopes (`Dashboards`, `PowerBiPublishing`, `DataSources`) so logs can be filtered per feature, matching the folder structure 1:1.
- **Health checks** (`/health/live`, `/health/ready`) covering SQL connectivity and Power BI API reachability, surfaced to k8s liveness/readiness probes.
- **Architecture Decision Records (ADRs)** in `docs/architecture/adr/` capture *why* Clean Architecture, PBIR-as-target, and this testing taxonomy were chosen — critical for onboarding and long-term maintainability.
- **Centralized package version management** (`Directory.Packages.props`, single `package.json` lockfile) prevents version drift across feature teams.
- **Contract-first FE/BE sync** via `shared-contracts/openapi.yaml` generating `api-contracts.ts`, eliminating an entire class of integration bugs before tests even run.

---

## 8. Summary: Why This Structure Meets the Stated Goals

| Goal | How this design achieves it |
|---|---|
| Maintainable | Feature-first folders on both sides; Clean Architecture dependency rules enforced by architecture tests |
| Scalable | New features (e.g., `features/scheduling`) drop in without touching existing modules; monorepo path-filtered CI scales with team size |
| Debuggable | Correlation IDs, structured per-feature logging, health checks, golden-file regression snapshots pinpoint exact drift |
| Feature-organized | Explicit `Features/` (backend) and `features/` (frontend) directories per business capability |
| Full test taxonomy | Six independent, CI-wired test categories (Unit, Integration, Path, Regression, E2E, Security) on both frontend and backend |
