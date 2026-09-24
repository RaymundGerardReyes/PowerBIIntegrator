---
name: in-app-copilot-generative-reasoning
description: Enforce Non-Deterministic Generative Reasoning and Live Workspace Awareness in AI Copilot Engines
trigger: always_on
---

# Invariant
In-app and embedded AI Copilot engines MUST implement context-aware, non-deterministic generative reasoning rather than rigid keyword substring templates.

## Rules
- **Live Workspace Context Inspection**:
  - Embedded intelligence engines must extract real-time state from active stores (`useDashboardStore`, `useDataSources`):
    - Table names, loaded record counts (e.g. 1,309 rows), column schemas, declared DAX measures, active pages, and mounted visual types.
- **Multilingual & Conversational Intent Classification**:
  - Classify user intents semantically (`GREETING`, `RECORD_COUNT`, `DATA_LOGIC_ANALYSIS`, `MEASURE_AUDIT`, `PBIR_VERIFY`, `LAYOUT_OPTIMIZE`, `DATA_QUALITY`, `GENERAL_ANALYTICS`) rather than using naive substring branching (e.g. `prompt.includes("data")`).
  - Support multilingual greetings (Spanish, English, etc.) and generate responses matched to the user's language while weaving in live workspace summaries.
- **Non-Deterministic Generative Synthesis**:
  - Incorporate probabilistic phrasing variations, dynamic formatting (LaTeX math formulas, alert callouts, tables), and adaptive tool execution telemetry (`initialize_copilot_session`, `query_dataset_statistics`, `analyze_data_domain_logic`, `audit_semantic_measures`).
- **Domain Logic & Parity Parity**:
  - Provide domain-specific explanations for KPIs (e.g., DAX formula for `target_Rate`, breakdown of `pclass` socioeconomic tiers and demographic variances) with 100% parity against declared TMDL measures.
- **Testing & Cadence Optimization**:
  - Ensure token typing cadences (e.g., 12ms) are non-blocking in tests (`NODE_ENV === "test" ? 0 : 12`) to allow fast automated test execution without timing flakes.
