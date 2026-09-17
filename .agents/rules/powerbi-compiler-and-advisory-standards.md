---
name: powerbi-compiler-and-advisory-standards
description: Architectural rules and invariants for Power BI PBIP/PBIR/TMDL compilation, local desktop workflows, cloud embedding boundaries, and read-only AI Advisory tier least-privilege security.
---

# Power BI Compiler, Runtime Boundaries & Advisory Tier Standards

## 1. Compiler vs. Runtime Boundary
- **C# as Compiler & Orchestrator:** The .NET 10 backend serves as a compiler that translates canonical analytics models and dashboard intermediate representations (IR) into:
  - **PBIP / PBIR / TMDL** text metadata for local **Power BI Desktop** consumption.
  - **Native HTML5/SVG** for in-browser client-side interactive rendering.
  - **ClosedXML (Excel), QuestPDF (PDF), DocumentFormat.OpenXml (Word)** for multi-target executive reporting.
- Power BI is never treated as a CLR runtime for arbitrary C# binaries; integration occurs exclusively via schema compilation, REST APIs, and embedded iframes.

## 2. Local Power BI Desktop Workflow Invariant
- **Desktop Non-Server Reality:** Power BI Desktop (`PBIDesktop.exe`) is a standalone Windows desktop client that does not host an HTTP embedding server or issue OAuth/JWT embed tokens on `localhost`.
- **PBIP Artifact Generation:** Local development workflows must produce compiled `.pbip` zip archives containing `definition.pbir` (enhanced report format) and `*.tmdl` (semantic models with DAX measures) that can be unzipped and opened directly in Power BI Desktop on Windows without cloud dependencies.

## 3. Power BI Embedded (Cloud iframe) Security & Fallback
- **Cloud Token Prerequisite:** Rendering interactive reports via the Microsoft `powerbi-client` SDK inside an `<iframe>` requires a valid Azure Entra ID / Microsoft Fabric service principal JWT token (`api.powerbi.com`).
- **Graceful Fallback:** In local or offline development environments, `EmbedTokenService` must supply demonstration tokens and the frontend must display an informative notice with direct one-click actions:
  - **"Open in Power BI Desktop (.pbip)"**
  - **"Switch to Layout Canvas Editor"**
  This prevents unhandled 403 Forbidden network errors from Microsoft.

## 4. AI Advisory Tier Least-Privilege Guardrails
- **Read-Only Invariant:** Advisory tools must only query metadata and execution results (`PipelineRunResult`, `DatasetProfile`, `DuplicateCluster`, `SchemaViolation`, `TransformationPlan`, `ChartSuggestion`). They must never mutate Domain state or execute write operations.
- **Data Sensitivity Hierarchy:**
  - `Public (0)`: Row counts, stage names, rule IDs.
  - `Internal (1)`: Schema types, column names, transformation steps.
  - `Sensitive/Confidential (2)`: Sample values, row keys, duplicate clusters — requires explicit user role elevation (`DataSteward`) and forces local model execution (`LocalOllama`).
  - `Restricted (3)`: Raw PII/PHI — strictly blocked and redacted from all LLM prompts under all circumstances.
- **Grounded Explainability:** All model explanations must strictly ground their citations in verified rule IDs and run IDs present in the assembled advisory context. Hallucinated rules or unverified citations must be rejected.

