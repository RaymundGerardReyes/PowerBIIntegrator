# Optional AI Advisory Tier — Orchestration Logic & Instructions

Scope: Read-only AI orchestration tier that reads outputs from the rule-driven Data Quality & Transformation Engine (DQTE) and provides guidance, explanations, or advanced suggestions — operating strictly under the guardrails and controller model already designed in the LLM Gateway.
Status: Implementation-ready logic specification, not core-path code.
Hard constraint: This tier NEVER writes, mutates, approves, or executes anything in the DQTE, Domain, or Power BI publishing pipeline. It is advisory-only, at all times.

---

## 1. Purpose and Boundary

The Advisory Tier exists to answer questions like:

- "Why were these 42 rows treated as duplicates?"
- "Why did this column fail schema validation?"
- "What transformation plan would you suggest for merging these two tables?"
- "Is this chart suggestion appropriate given the data shape?"

It must never:

- Modify a `SchemaContract`, `DedupeRuleSet`, `CleaningRule`, or `TransformationPlan`.
- Trigger a pipeline run, approve a quarantined batch, or publish to Power BI.
- Receive raw sensitive data (PII, credentials, connection strings) unless explicitly permitted by policy and redacted first.

This boundary is enforced structurally, not by prompt instruction — the tier is wired only to read-only tools, following the same "read-only role, least privilege" principle used for safe LLM tool-use in production systems.[web:159][web:155]

---

## 2. Position in the Overall Architecture

```
        DQTE (Domain / Application / Infrastructure — deterministic, AI-free)
                                  │
                    PipelineRunResult / DatasetProfile /
                    DuplicateCluster / SchemaContract /
                    TransformationPlan / ChartSuggestion
                                  │
                         (read-only projection)
                                  │
                                  ▼
                     ┌─────────────────────────┐
                     │   ADVISORY CONTEXT       │
                     │   ASSEMBLER (backend)    │
                     └────────────┬─────────────┘
                                  │
                                  ▼
                     ┌─────────────────────────┐
                     │  PROMPT GUARDRAIL        │
                     │  (PII redaction, policy) │
                     └────────────┬─────────────┘
                                  │
                                  ▼
                     ┌─────────────────────────┐
                     │      LLM GATEWAY         │
                     │ (local Ollama / cloud    │
                     │  API key, per policy)    │
                     └────────────┬─────────────┘
                                  │
                                  ▼
                     ┌─────────────────────────┐
                     │  RESPONSE GUARDRAIL      │
                     │  (output filter, audit)  │
                     └────────────┬─────────────┘
                                  │
                                  ▼
                        AdvisoryResult (text +
                        structured citations back
                        to DQTE run/rule IDs)
                                  │
                                  ▼
                     React UI: AdvisoryPanel.tsx
```

The LLM Gateway, guardrails, and provider selection (local Ollama vs. cloud API key) reuse the design already established for the platform's LLM orchestration layer — this tier is a **new consumer** of that gateway, not a new gateway.

---

## 3. Read-Only Tool Contract (MCP-style)

Every tool exposed to the LLM is explicitly read-only and schema-validated, following the enterprise function-calling pattern: name, description, JSON input/output schema, and an allowlist entry — nothing is invoked unless it appears on the allowlist.[web:155][web:159]

| Tool name | Reads | Returns | Write access |
|---|---|---|---|
| `get_pipeline_run_result` | `PipelineRunResult` by run ID | Row counts in/out per stage, rules triggered, quarantine count | None |
| `get_dataset_profile` | `DatasetProfile` by dataset ID | Column profiles (types, null %, cardinality, detected patterns) — no raw row values | None |
| `get_duplicate_clusters` | `DuplicateCluster`s by run ID | Cluster summaries: rule fired, similarity score, kept/dropped row IDs (no raw PII fields) | None |
| `get_schema_violations` | `SchemaCompatibilityRule` evaluation results | Violated rule, column, expected vs. actual type/constraint | None |
| `get_transformation_plan` | `TransformationPlan` by ID | Step sequence, source/target schema, step type | None |
| `get_chart_suggestions` | `VisualMappingRule` outputs for a Gold table | Ranked suggestions + rule-based `Reason` string | None |

Rules for this table:

- Every tool's output schema must **exclude raw cell values** by default; only aggregated/structural metadata is returned unless a specific policy explicitly allows sample-row exposure (see Section 5).
- Tool definitions are versioned; a breaking schema change requires a new tool version, not silent mutation — matching general tool-versioning guidance for enterprise LLM deployments.[web:155]
- No tool in this tier accepts a write payload of any kind. If a future write-capable tool is ever added (e.g., "apply suggested transformation"), it belongs to a **separate, human-approved gateway**, never this tier.[web:155]

---

## 4. Advisory Context Assembler — Logic

Backend component (`Features/AiAdvisory/Application`) responsible for building the context sent to the LLM. Runs entirely before any guardrail or LLM call.

**Step-by-step logic:**

1. Receive `AdvisoryRequest { RunId, QuestionType, UserQuestion, UserId }`.
2. Resolve `AdvisoryPolicy` for the user's role and the requested `QuestionType` (see Section 6 — Controller/Policy Engine).
3. Call only the read-only tools permitted by the resolved policy (e.g., a "junior analyst" role may see `get_chart_suggestions` but not `get_duplicate_clusters` with row-level detail).
4. Assemble a **structured context object** — never raw free text dumps — containing:
   - Tool name + tool output (JSON).
   - Rule IDs and rule descriptions (from Domain rule metadata) so the LLM can cite them.
   - Explicitly exclude any field flagged `Sensitive = true` in the Domain metadata unless `AdvisoryPolicy.AllowSensitiveContext == true`.
5. Pass the structured context + user question to the Prompt Guardrail (Section 6).

This mirrors metadata-driven retrieval patterns where structured, filtered metadata — not raw documents — is what gets passed into the model context, improving both precision and safety.[web:150][web:154][web:162]

---

## 5. Sensitive Data Exposure Control (the "optional controlling method")

This directly implements your requirement: *"optional controlling method for exposing sensitive data to LLM model."*

### 5.1 Field-level sensitivity tagging (Domain layer)

Every Domain field that can appear in advisory context carries a `SensitivityLevel`:

```csharp
public enum SensitivityLevel
{
    Public = 0,      // e.g., rule names, row counts, column names (non-PII)
    Internal = 1,    // e.g., table names, schema versions
    Confidential = 2,// e.g., sample values, customer identifiers
    Restricted = 3   // e.g., raw PII, credentials — never sent to any LLM
}
```

`ColumnProfile`, `DuplicateCluster`, and any other advisory-eligible entity must declare a `SensitivityLevel` per field. This mirrors the "mark data sensitivity levels in tool definitions" defense used in enterprise LLM tool-calling security guidance.[web:160]

### 5.2 Policy-driven exposure decision

```csharp
public sealed class AdvisoryPolicy
{
    public bool AllowCloudProvider { get; init; }
    public SensitivityLevel MaxExposureLevel { get; init; } // e.g., Internal by default
    public IReadOnlySet<string> AllowedTools { get; init; }
    public bool RequireHumanApprovalForConfidential { get; init; }
}
```

**Decision rule (evaluated for every field before it enters context):**

```
if field.SensitivityLevel > policy.MaxExposureLevel:
    field = Redact(field)     // replace with "[REDACTED: <type>]" placeholder
elif field.SensitivityLevel == Confidential and policy.RequireHumanApprovalForConfidential:
    field = PendingApproval(field)  // held out, flagged for optional manual unlock
else:
    field = include as-is (still passes through Prompt Guardrail)
```

- `Restricted` fields are **never** eligible for inclusion, regardless of policy — this is a hard-coded ceiling, not configurable, matching the principle that some data classes should never cross the LLM boundary at all.[web:151][web:160]
- Cloud providers get a **stricter default ceiling** than local Ollama (e.g., cloud defaults to `MaxExposureLevel = Public`, local can be configured up to `Internal` or `Confidential` for trusted on-prem use), because local models never leave your infrastructure.

### 5.3 The "optional controlling method" — explicit opt-in unlock

Provide an explicit, audited, per-request unlock mechanism rather than a silent policy toggle:

- UI control: `AdvisoryPanel.tsx` shows a locked "Show detailed row samples" toggle, disabled by default.
- Enabling it requires:
  1. User has the `DataSteward` or higher role.
  2. Explicit confirmation dialog naming exactly what will be exposed (e.g., "3 sample rows from DuplicateCluster #12 will be sent to the LLM").
  3. Provider is forced to `LocalOllama` only when `Confidential` data is unlocked — cloud providers remain blocked for this session regardless of user role.
  4. The unlock event and its scope are written to the audit log with a TTL — the elevated exposure applies only to the current request, never persists as a session-wide setting.

This gives you a genuine **optional controlling method**: sensitive exposure is possible, but always explicit, scoped, audited, and biased toward local-only execution.

---

## 6. Guardrail and Controller Logic (reusing the LLM Gateway)

### 6.1 Prompt Guardrail (pre-LLM)

Executed after context assembly, before any model call:

1. **Redaction pass** — regex/dictionary-based PII detection over any `Confidential`-tier field that passed the exposure decision (defense-in-depth, even after field-level tagging).
2. **Prompt-injection scan** — check the `UserQuestion` for instructions like "ignore previous rules," "reveal the API key," "output raw table contents" — block and log if detected.
3. **Schema/tool-call boundary enforcement** — confirm the assembled context only contains data from the allowlisted tools resolved in Section 4; reject if any out-of-policy field is present (defense against a coding error, not just a malicious prompt).[web:155][web:160]

### 6.2 Provider Selection Controller

```
function ChooseProvider(policy, contextSensitivity):
    if contextSensitivity >= Confidential:
        return LocalOllama   // never cloud, regardless of policy.AllowCloudProvider
    if not policy.AllowCloudProvider:
        return LocalOllama
    return Cloud  // API-key based, per user/task policy
```

This hard-codes "confidential context forces local" as a non-overridable rule, not a configurable default — the strongest form of the sensitive-data guardrail.

### 6.3 Response Guardrail (post-LLM)

1. **Output redaction pass** — re-scan the model's response for any leaked identifiers, even ones not present in the original context (models can occasionally reconstruct patterns).
2. **Citation validation** — require the response to reference only rule IDs / run IDs that were actually present in the assembled context; if the model cites a rule/run ID not in context, treat the response as hallucinated and either regenerate once or fall back to a templated "insufficient grounded context" message.
3. **Category filter** — block disallowed output categories per existing platform policy (unsafe instructions, secret exfiltration attempts, etc.).

### 6.4 Audit and Telemetry

Every advisory request logs (independent of guardrail outcome):

- Correlation ID linking to the originating `PipelineRunResult`.
- Policy evaluated, provider chosen, sensitivity ceiling applied.
- Guardrail actions taken (redacted fields, blocked prompts, blocked outputs).
- Final response returned to the user, tagged with cited rule/run IDs.

This treats every advisory interaction like a production event, not a disposable chat transcript — consistent with enterprise LLM tool-use auditing guidance.[web:155][web:160]

---

## 7. Explainability Discipline — Why the LLM Is Allowed to "Explain" Safely

Because the DQTE is itself deterministic and rule-based, the Advisory Tier's job is fundamentally **summarization and translation of already-correct, already-logged decisions** — not independent reasoning about the data. This is a materially safer use of an LLM than having it infer new judgments from raw data, and aligns with the pattern of using LLMs to narrate transparent-by-design analytical processes rather than replace them.[web:149]

Concretely:

- The LLM is only ever asked to explain **outputs that already exist** (`PipelineRunResult`, `DuplicateCluster.Reason`, `VisualMappingRule.Reason`) — it does not get to re-derive whether a row is a duplicate or whether a schema is valid.
- Every explanation must ground itself in a specific rule ID from the Domain layer's rule catalogue (e.g., "ExactHashRule v2", "CompositeKeyRule: CustomerId+InvoiceNumber"), which the Response Guardrail validates against the assembled context (Section 6.3).
- If no grounded rule exists for a user's question (e.g., "why is my revenue lower this month" — a business question, not a pipeline-decision question), the Advisory Tier must respond with an explicit "outside advisory scope" message rather than speculating.

---

## 8. Backend Structure (mapped to existing Clean/Hexagonal layout)

```
AnalyticsPlatform.Domain/Features/AiAdvisory/
    Entities/AdvisoryPolicy.cs
    Entities/SensitivityLevel.cs
    Rules/ExposureDecisionRules.cs      // pure logic: field -> include/redact/hold

AnalyticsPlatform.Application/Features/AiAdvisory/
    Commands/RunAdvisoryQueryCommand.cs
    Commands/RunAdvisoryQueryCommandHandler.cs
    Queries/GetAdvisoryPolicyQuery.cs
    Interfaces/IReadOnlyToolRegistry.cs
    Interfaces/IPromptGuardrailService.cs   // reused from LLM Gateway
    Interfaces/IResponseGuardrailService.cs // reused from LLM Gateway
    Interfaces/ILlmGateway.cs               // reused from LLM Gateway

AnalyticsPlatform.Infrastructure/Features/AiAdvisory/
    Tools/GetPipelineRunResultTool.cs
    Tools/GetDatasetProfileTool.cs
    Tools/GetDuplicateClustersTool.cs
    Tools/GetSchemaViolationsTool.cs
    Tools/GetTransformationPlanTool.cs
    Tools/GetChartSuggestionsTool.cs
    Redaction/PiiRedactionEngine.cs
    Audit/AdvisoryAuditLogger.cs

AnalyticsPlatform.Api/Endpoints/
    AiAdvisoryEndpoints.cs
        POST /advisory/query
        GET  /advisory/policies
```

Each read-only tool implementation wraps an existing DQTE query handler (`GetPipelineRunHistoryQuery`, etc.) — it never calls a Command handler, structurally preventing writes at the type level (tools are only constructed with `IRequestHandler<TQuery, TResult>`, never `IRequestHandler<TCommand, TResult>`).

---

## 9. Frontend Structure (React 19 + TS 7)

```
features/ai-advisory/
    api/advisoryApi.ts
    components/AdvisoryPanel.tsx        // question input + response display
    components/ExposureUnlockDialog.tsx // explicit sensitive-data opt-in (Section 5.3)
    components/CitationBadge.tsx        // renders cited rule/run IDs as clickable refs
    hooks/useAdvisoryQuery.ts
    model/types.ts
    index.ts
```

`AdvisoryPanel.tsx` behavior:

- Default state: provider = auto (policy-resolved), exposure = `Public/Internal` only.
- `CitationBadge.tsx` renders every rule/run ID the response cites, linking back to the actual DQTE run detail view — reinforcing that the AI is narrating verifiable data, not inventing conclusions.
- `ExposureUnlockDialog.tsx` implements the explicit, scoped, audited unlock flow from Section 5.3; disabled entirely for roles below `DataSteward`.

---

## 10. Test Coverage (extends existing six-category taxonomy)

| Category | Advisory-tier specific tests |
|---|---|
| Unit | `ExposureDecisionRulesTests` (field sensitivity vs. policy ceiling matrix), `ProviderSelectionControllerTests` (confidential forces local, never overridable) |
| Integration | `AiAdvisoryEndpointsTests` with mocked LLM Gateway, verifying only allowlisted tools are ever invoked |
| Path | `AdvisoryExplainDuplicateClusterPathTests` — full flow from question → context assembly → guardrails → grounded response with valid citations |
| Regression | `RedactionGoldenFileTests` — snapshot of redaction behavior per sensitivity level, catching silent policy drift |
| E2E | `AdvisoryUnlockAndAuditWorkflow.feature` — user requests confidential exposure, unlock dialog appears, provider forced to local, audit log entry verified |
| Security | `AdvisoryPromptInjectionTests`, `AdvisorySensitiveFieldLeakTests` (attempt to leak `Restricted` fields via crafted questions — must always fail closed) |

---

## 11. Summary

The Advisory Tier is a strictly read-only, policy-gated consumer of the DQTE's already-correct, already-audited outputs. It reuses the existing LLM Gateway (local Ollama default, cloud API key optional) and its guardrails, adds a field-level `SensitivityLevel` model with a non-overridable ceiling for `Restricted` data, forces local-only execution whenever confidential context is present, and requires citation-grounded responses tied back to real rule/run IDs. Every advisory interaction is audited like a production event. This satisfies the "optional controlling method for exposing sensitive data to the LLM" requirement precisely: exposure is possible, explicit, scoped, reversible per request, and biased toward the safest execution path by default.[web:155][web:159][web:160]
