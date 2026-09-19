# PowerBI Enhanced — Full-Stack Architecture Validation & Correction Guide

This document consolidates the **frontend** and **backend** architecture validation findings into one authoritative correction guide. It is meant to be dropped into the repository root (or `docs/architecture/`) as the single source of truth for polishing, re-correcting, and automatically validating the logical workflow of the entire system.

---

## Table of Contents

1. [Guiding Principles](#guiding-principles)
2. [Frontend Architecture Validation & Corrections](#frontend-architecture-validation--corrections)
3. [Backend Architecture Validation & Corrections](#backend-architecture-validation--corrections)
4. [Cross-Stack Contract Validation](#cross-stack-contract-validation)
5. [Unified Test Taxonomy](#unified-test-taxonomy)
6. [CI/CD Enforcement Pipeline](#cicd-enforcement-pipeline)
7. [Correction Checklist (Actionable Backlog)](#correction-checklist-actionable-backlog)

---

## Guiding Principles

1. **Every architectural rule must become a test, not just documentation.** If a rule cannot be tested automatically, it will drift.
2. **Layers only depend downward.** Presentation → Application → Domain. Infrastructure implements Application/Domain interfaces; it never gets referenced upward.
3. **Features are vertical slices.** No cross-feature imports without a public `index.ts` / public API contract.
4. **Contracts (DTOs/schemas) are the seam between frontend and backend.** Both sides must be tested against the same schema source of truth.
5. **Every defect becomes a permanent regression test.** Never delete a regression test once added.
6. **Security and correctness are non-negotiable release gates**, not optional nice-to-haves.

---

## Frontend Architecture Validation & Corrections

### 1. Layer & Dependency Corrections

| Rule | Correction Needed | Enforcement |
|---|---|---|
| `app/` may import `features/*`, `entities/*`, `shared/*` | Audit imports; forbid `features/*` importing from `app/*` | `eslint-plugin-boundaries` rule |
| `features/*` may import `entities/*`, `shared/*`, and itself only | Forbid direct cross-feature imports (e.g. `dashboards` importing internals of `data-quality`) | Custom ESLint boundary rule |
| `entities/*` may import `shared/*` only | Forbid entities importing from features | ESLint boundary rule |
| `shared/*` must not import from `features/*` or `entities/*` | Audit `shared/ui`, `shared/lib` for upward imports | ESLint boundary rule |
| Every feature exposes a single `index.ts` barrel | Add missing barrels; disallow deep imports (`features/x/components/Y` from outside) | `no-restricted-imports` ESLint rule |

**Correction action:** Add an `eslint-plugin-boundaries` config (or equivalent) enforcing the FSD (Feature-Sliced Design) layering: `app > features > entities > shared`, and wire it into CI as a hard failure.

### 2. Application Shell & Navigation

**Rule:** Top `<header>` navigation only. No `<aside>` sidebar. Must contain logo (`/dashboards` link), nav links (`Dashboards & PBIP`, `Data Sources`, `Data Quality & Advisory`, `Executive Reports`), theme toggle, auth controls, copilot drawer toggle.

**Corrections:**
- Add a component test asserting `<aside>` never renders anywhere in `AppLayout`.
- Add a snapshot/DOM test asserting all four nav links exist and route to correct paths.
- Add a test asserting `AuthProvider` state correctly toggles "Sign in" vs. user email + "Sign out".

### 3. Data Ingestion & Active Dataset Context

**Rule:** Uploads use `FormData` (browser sets multipart boundary); backend returns model/dataset IDs; frontend persists `powerbi_active_model_id`, `powerbi_active_dataset_id`, `powerbi_active_model_name` to `localStorage`.

**Corrections:**
- Unit test `apiClient`: when `config.data instanceof FormData`, do **not** manually set `Content-Type`.
- Unit test `apiClient`: `X-Correlation-Id: client-<uuid>` header is always injected, even on retries.
- Integration test: after a mocked successful upload, all three `localStorage` keys are set with matching values (no partial writes).
- Regression test: if a previous bug caused stale dataset context after a failed upload, add an explicit test that failed uploads do **not** overwrite `localStorage`.

### 4. Data-Quality 6-Stage Pipeline

**Rule:** Sequential stages — Profile → Deduplicate → Clean → Transform → Suggest Visuals → Advisory. Each stage calls a dedicated endpoint.

**Corrections:**
- Model pipeline stages as an explicit enum/state machine in `usePipelineRun`; forbid illegal transitions (e.g., `PROFILE → TRANSFORM` skipping `DEDUPLICATE`/`CLEAN`).
- Add a state-machine unit test enumerating all illegal transitions and asserting they throw or are rejected.
- Add integration tests per stage verifying the correct HTTP call (`GET profile`, `POST deduplicate`, `POST clean`, `POST transform`, `GET suggest-visuals`) fires when "Next" is triggered.
- Add one full E2E run of the wizard against a fixture dataset, verifying the terminal advisory panel reflects expected risk output.

### 5. Dashboard Canvas & Semantic Measure Parity

**Rule:** KPI cards must bind only to valid DAX measures (`Total*`, `Sum_*`, `Average_*`, `*_Rate`, or default `TotalRows`). Raw unaggregated columns are rejected.

**Corrections:**
- Extract and unit-test a pure function `isValidMeasureName(name: string): boolean` covering all valid prefixes/suffixes and rejecting raw columns (e.g., `CustomerName`, `OrderId`).
- Unit test `cleanFieldLabel("Table[Column]") === "Column"`.
- Component test: mounting `CardVisual` with a raw-column binding must render an error state, not silently pass through.
- Regression/snapshot test: `dashboardSlice` initial state must always include the default `TotalRows` measure; a failing snapshot signals an accidental removal.

### 6. Pre-Flight Model Validation Engine

**Rule:** `ModelValidationModal` calls `/api/analytics/validate` and renders `isValid`, `errors`, `detectedCycles`, `orphanTables`, `warnings` with correct badge coloring.

**Corrections:**
- Integration test with a mocked "invalid" response (cycles + orphans) asserting red badges and correct list rendering.
- Integration test with a mocked "valid" response asserting green/success badge and no error list rendering.
- Contract test: TypeScript `ValidateAnalyticsModelResponseDto` type must be generated from (or checked against) the backend OpenAPI schema — fail CI on drift.

### 7. Local PBIDesktop & PBIR/PBIP Orchestration

**Rule:** `report.json` must omit `activePageIndex`/`activePageName`, set `layoutOptimization: "None"`, use `reportVersionAtImport`. `pages.json` must include `pageOrder` and `activePageName`.

**Corrections:**
- Store sample PBIR fixtures under `frontend/tests/fixtures/pbir/`.
- Write fixture-based tests asserting forbidden fields are absent and required fields are present, on both the fixtures and any client-side PBIR preview logic.
- Integration test for `powerBiApi` download flow: mocked blob response is correctly handed off to download/preview components.

### 8. AI Advisory & Exposure Unlock

**Rule:** Advisory queries evaluate risk against GDPR/HIPAA/SOC2 policies; confidential exposure unlock requires justification and produces an audit trail.

**Corrections:**
- Unit test `useAdvisoryQuery` normalizes risk levels (`low`/`medium`/`high`) and surfaces citations correctly from mocked responses.
- Component test `ExposureUnlockDialog`: submitting a justification calls `unlockConfidentialExposure()` exactly once with the correct payload, and disables the dialog while pending.
- Security test: verify that unlock actions cannot be triggered without a non-empty justification (client-side guard) — and confirm server-side enforcement exists as a backend security test too.

### 9. Authentication & Session

**Rule:** `AuthProvider` exposes `user`, `isAuthenticated`, `login`, `logout`; `AppLayout` reacts accordingly.

**Corrections:**
- Unit test the reducer/state: `login(user)` sets `isAuthenticated = true`; `logout()` fully clears `user`.
- Component test: `AppLayout` renders correctly for both authenticated and unauthenticated states.
- Security test: confirm tokens are never persisted in plain `localStorage` if they carry sensitive claims (use secure storage or httpOnly cookies where applicable).

### 10. Shared Primitives

**Corrections:**
- Unit test all formatters (`formatCurrency`, `formatPercent`, `formatCompactNumber`) against edge cases: zero, negative numbers, very large numbers, `null`/`undefined`.
- Accessibility test `Modal.tsx`: focus trap, `Esc` key closes, backdrop click closes, ARIA roles present.
- Accessibility test `WorkflowStepper.tsx`: correct ARIA `aria-current` on active step.

---

## Backend Architecture Validation & Corrections

### 1. Layer & Dependency Corrections

| Rule | Correction Needed | Enforcement |
|---|---|---|
| `AnalyticsPlatform.Api` → Application + Domain only | Remove any direct `Infrastructure` references from endpoint classes | Architecture test (reflection-based) |
| `AnalyticsPlatform.Application` → Domain + own interfaces only | Ensure no `using AnalyticsPlatform.Infrastructure` in Application project | Architecture test |
| `AnalyticsPlatform.Domain` → no outward dependencies | Domain project must not reference Application/Infrastructure/Api | Architecture test |
| `AnalyticsPlatform.Infrastructure` implements Application interfaces | Confirm every `I*Generator`/`I*Service` interface has exactly one production implementation registered in DI | DI container validation test |
| Feature isolation (AiAdvisory, Analytics, DataQuality, DataSources, PowerBiPublishing, Reports, LlmOrchestration) | No feature handler directly calls another feature's handler class; use MediatR requests or shared domain services | Architecture test scanning for cross-feature `using` statements |

**Correction action:** Add an `AnalyticsPlatform.Tests.Architecture` project (mirroring the university ERP's `ArchitectureTests`) using a library like `NetArchTest.Rules` to assert these dependency rules automatically on every build.

### 2. API Layer Corrections

**Corrections:**
- Audit every endpoint in `AnalyticsPlatform.Api/Endpoints/*` — confirm each one only (a) parses request, (b) sends a MediatR command/query, (c) maps result to HTTP response. Move any inline business logic into handlers.
- Ensure `ExceptionHandlingMiddleware` maps **every** custom exception type to the correct RFC 7807 status code; add a matrix test enumerating each exception type → expected status code.
- Ensure `CorrelationIdMiddleware` correlation ID appears in: response headers, structured logs, and error `ProblemDetails.Extensions["correlationId"]`.
- Add contract tests generating/validating an OpenAPI spec so frontend DTOs never silently drift from backend DTOs.

### 3. Application Layer Corrections

**Corrections:**
- Confirm `ValidationBehavior` runs for **every** command/query that has a matching `FluentValidation` validator; add a test that scans all commands/queries and asserts a validator exists where expected (or is explicitly exempted).
- Confirm `LoggingBehavior` logs request name, correlation ID, and duration for every MediatR request — add a test using a fake logger to assert log entries are emitted.
- Introduce idempotency checks for `RegisterDataSourceCommand`, `CompilePbirDefinitionCommand`, and `LaunchLocalPowerBiCommand` to avoid duplicate side effects on retry.
- Extract repeated invariants (e.g., "every model must have `TotalRows` measure", "newest-first ordering") into a shared domain service/helper rather than duplicating per-handler logic; add unit tests directly against that shared service.

### 4. Domain Layer Corrections

**Corrections:**
- Move validation logic currently embedded in handlers (e.g., measure name uniqueness, layout bounds checking) into Domain entity methods: `AnalyticsModel.AddMeasure(...)`, `Table.AddColumn(...)`, `Visual.BindMeasure(...)`.
- Add explicit Domain unit tests:
  - `AnalyticsModel` always contains `TotalRows` measure after construction.
  - `Visual.BindMeasure` throws `DomainValidationException` if bound to a raw, non-aggregated column.
  - `LayoutPosition` value object rejects out-of-canvas-bounds coordinates.
- Ensure `DomainValidationException` and `NotFoundException` are the **only** exception types thrown from Domain (no generic `Exception` or `InvalidOperationException` leaking upward).

### 5. Infrastructure Layer Corrections

**Corrections:**
- `TypeInferenceEngine`: add unit tests covering every supported type mapping (`type number`, `Int64.Type`, `type text`, `type datetime`, `type logical`) and confirm bare `number` is never emitted.
- Data connectors (`CsvDataSourceReader`, `ExcelDataSourceReader`, `SqlServerConnector`): add integration tests using fixture files/mock DBs, asserting schema extraction output feeds correctly into `TypeInferenceEngine` and `AnalyticsModelFactory`.
- `PbirGenerator`: add fixture-based tests confirming:
  - `report.json` never contains `activePageIndex` or `activePageName`.
  - `report.json.layoutOptimization === "None"` (string).
  - `themeCollection.baseTheme.reportVersionAtImport` is present.
  - `pages.json` always has `pageOrder` (array) and `activePageName` (string), with at least one page (fallback `Page1`).
- `TmdlGenerator`: add tests confirming partitions never emit `Source = #table(type table [], {})`, and that every measure referenced by any `Visual` exists in the corresponding `<Table>.tmdl`.
- `LocalPowerBiDesktopService`: add tests (with mocked process/registry access) for install-detected/not-detected, running/not-running, and SSAS port detection success/failure paths; ensure timeouts prevent hangs.
- `Llm/Guardrails/PiiRedactionService`: add security-focused unit tests confirming PII patterns (emails, phone numbers, national IDs) are redacted **before** requests reach `ProviderRouter`, especially when `SensitiveMode = true`.

### 6. MCP Server Layer Corrections

**Corrections:**
- Restrict MCP endpoints to read-only/query operations by default; add an architecture test scanning MCP endpoint handlers for any `Command` usage and flag/require explicit justification.
- Add audit logging for every MCP request (agent identity, request payload hash, correlation ID) tied into the same observability pipeline as human-driven requests.

---

## Cross-Stack Contract Validation

The seam between frontend and backend is the highest-risk area for silent drift. Corrections:

1. **Single source of truth for schemas.** Generate backend OpenAPI spec from `AnalyticsPlatform.Api`; generate frontend TypeScript types from that spec (e.g., via `openapi-typescript`) instead of hand-maintaining `shared/types/api-contracts.ts`.
2. **PBIR/TMDL rule parity test.** Both frontend (`frontend/tests/fixtures/pbir`) and backend (`PbirGenerator` fixture tests) must assert the **same** rule set — forbidden fields, required fields, `layoutOptimization` value. Any change to one side without updating the other must fail CI.
3. **Contract test suite (`backend/tests/contract` + `frontend/tests/contract`).** Runs on every PR; fails if:
   - A backend DTO field is renamed/removed without a corresponding frontend type update.
   - A frontend expects a field the backend no longer returns.
4. **Correlation ID round-trip test.** Send a request with a known `X-Correlation-Id` from a simulated frontend client; assert it appears unchanged in backend logs and in the HTTP response header.

---

## Unified Test Taxonomy

| Test Type | Frontend Location | Backend Location | Purpose |
|---|---|---|---|
| Unit | `frontend/tests/unit`, colocated `*.test.tsx` | `backend/tests/unit` | Pure logic: formatters, measure-name validators, type inference, domain invariants |
| Integration | `frontend/tests/integration` | `backend/tests/integration` | API + component wiring, DB/connector integration, mocked HTTP flows |
| Architecture | ESLint boundaries + custom scripts | `backend/tests/architecture` (NetArchTest) | Layer/module dependency rules |
| Contract | `frontend/tests/contract` | `backend/tests/contract` | DTO/schema parity, PBIR/TMDL rule parity |
| End-to-End | `frontend/tests/e2e` (Playwright) | Full-stack E2E against running backend | Critical user journeys: ingest → quality → dashboard → validate → PBIP launch |
| Regression | Tagged within integration/E2E suites | Tagged within integration/E2E suites | One permanent test per resolved defect |
| Security | `frontend/tests/security` | `backend/tests/security` | PII redaction, auth/session, unlock justification enforcement, RBAC |
| Performance | `frontend/tests/performance` | `backend/tests/performance` | Large dataset ingestion, PBIP compile time, LLM streaming throughput |
| Accessibility | `frontend/tests/accessibility` | N/A | WCAG checks on Modal, WorkflowStepper, forms |

---

## CI/CD Enforcement Pipeline

Recommended pipeline stages, in order, each a hard gate:

1. **Lint & Type Check** — ESLint (with boundaries plugin) + `tsc --noEmit` (frontend); `dotnet build` with analyzers (backend).
2. **Unit Tests** — Frontend Vitest + Backend `dotnet test` on unit projects.
3. **Architecture Tests** — ESLint boundary rules (frontend) + `NetArchTest` project (backend).
4. **Contract Tests** — OpenAPI/DTO schema diff + PBIR/TMDL rule parity checks.
5. **Integration Tests** — Mocked/containerized dependencies (SQL, file storage, LLM provider stubs).
6. **Security Tests** — PII redaction, auth, unlock-justification enforcement.
7. **E2E Tests** — Playwright full-stack run against a docker-composed environment.
8. **Performance Tests** — Nightly/pre-release only, not on every commit.

A merge should be blocked if stages 1–6 fail. Stage 7 (E2E) should block release builds. Stage 8 informs release readiness but can run asynchronously.

---

## Correction Checklist (Actionable Backlog)

Use this as a tracked backlog (e.g., GitHub issues or a project board):

### Frontend
- [ ] Add `eslint-plugin-boundaries` config enforcing FSD layering.
- [ ] Add `<aside>`-forbidden test + nav link test for `AppLayout`.
- [ ] Add `apiClient` unit tests (FormData boundary, correlation ID header).
- [ ] Add `localStorage` sync tests for dataset upload success/failure paths.
- [ ] Implement explicit pipeline state machine + illegal-transition tests for data-quality wizard.
- [ ] Extract and unit-test `isValidMeasureName` and `cleanFieldLabel`.
- [ ] Add `CardVisual` raw-column rejection component test.
- [ ] Add `dashboardSlice` default-measure regression snapshot.
- [ ] Add `ModelValidationModal` success/failure integration tests.
- [ ] Add PBIR fixture-based schema tests (frontend-side preview/consumption).
- [ ] Add `ExposureUnlockDialog` justification-required test.
- [ ] Add `AuthProvider` login/logout unit tests + `AppLayout` auth-state tests.
- [ ] Add formatter edge-case tests and `Modal`/`WorkflowStepper` accessibility tests.

### Backend
- [ ] Create `AnalyticsPlatform.Tests.Architecture` project with `NetArchTest` layer rules.
- [ ] Audit all endpoints for business logic leakage; move into handlers.
- [ ] Add exception-to-status-code matrix test for `ExceptionHandlingMiddleware`.
- [ ] Add correlation ID propagation test (header → log → ProblemDetails).
- [ ] Add validator-coverage test for all MediatR commands/queries.
- [ ] Add idempotency checks for upload, PBIR compile, and desktop launch commands.
- [ ] Move measure/layout invariants into Domain entity methods; add Domain unit tests.
- [ ] Add `TypeInferenceEngine` full type-mapping unit tests.
- [ ] Add connector integration tests (CSV/Excel/SQL fixtures).
- [ ] Add `PbirGenerator` fixture tests (forbidden/required fields).
- [ ] Add `TmdlGenerator` partition + measure-parity tests.
- [ ] Add `LocalPowerBiDesktopService` mocked process/registry tests.
- [ ] Add `PiiRedactionService` security tests (redaction before provider routing).
- [ ] Restrict and audit MCP Server endpoints to read-only by default.

### Cross-Stack
- [ ] Generate OpenAPI spec from backend; generate frontend types from it (remove hand-maintained duplicate DTOs).
- [ ] Add PBIR/TMDL rule-parity contract tests shared conceptually between frontend and backend fixtures.
- [ ] Add correlation-ID round-trip contract test.
- [ ] Wire full CI pipeline (lint → unit → architecture → contract → integration → security → E2E → performance).

---

*This guide should be revisited every sprint. Any new business rule discovered during development must be added here first, with a corresponding automated test, before being considered "done."*
