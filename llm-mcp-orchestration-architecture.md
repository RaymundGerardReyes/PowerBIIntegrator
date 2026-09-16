# Model Context Protocol (MCP) & LLM Orchestration Architecture
## Enterprise Extension: C# Analytics Platform + React 19 UI + Power BI PBIP/PBIR Target Runtime

**Document Status:** Approved Master Architecture & Implementation Blueprint  
**Architecture Owner:** Principal Software Engineer & Principal Enterprise Architect mindset  
**Target Runtime Stack:** React 19.0 + TypeScript 5.8 (Frontend) | .NET 10 LTS (Backend) | Power BI PBIP/PBIR/TMDL (Compiler Target) | Ollama Local Runtime + Multi-Provider Cloud Fallback | Model Context Protocol (MCP Specification 2024-11-05)  
**Design Philosophy:** Feature-Sliced Design (FSD), Clean/Hexagonal Architecture, Guardrail-First Security Pipeline, Test Pyramid + Test Diamond (6-Category), Local-First Privacy, CI/CD-Native  
**Relationship to Existing Architecture:** Extends `enterprise-codebase-architecture.md` and aligns with `Feasibility and Architecture of a C#-Driven Analytics Framework Targeting Power BI via PBIP PBIR (2026 Status)`. Preserves all existing domain rules, repository boundaries, and test structures unchanged. Introduces one new backend feature slice (`Features/LlmOrchestration`), one new independent deployable host (`AnalyticsPlatform.McpServer`), one new frontend feature slice (`features/llm-assistant`), and shared OpenAPI contract extensions.

---

## 1. Architectural Principles & Non-Negotiables (LLM & MCP Layer)

The seven foundational principles established in `enterprise-codebase-architecture.md` remain strictly active. Six additional non-negotiable invariants govern the LLM and MCP subsystem:

```
┌──────────────────────────────────────────────────────────────────────────────────┐
│                           NON-NEGOTIABLE ARCHITECTURAL INVARIANTS                │
├──────────────────────────────────────────────────────────────────────────────────┤
│ 1. PIPELINE GUARDRAILS     │ Invocations MUST pass MediatR pipeline behaviors    │
│                            │ (PromptGuardrailBehavior & ResponseGuardrailBehavior)│
│                            │ Handlers cannot bypass them by construction.        │
├────────────────────────────┼─────────────────────────────────────────────────────┤
│ 2. POLICY-DRIVEN ROUTING   │ Provider selection is purely declarative via        │
│                            │ ILlmPolicyResolver. Handlers never hardcode cloud.  │
├────────────────────────────┼─────────────────────────────────────────────────────┤
│ 3. SENSITIVE DATA ZERO-LEAK│ CCTV/camera events, PII, and credentials default to │
│                            │ strictly blocked. Exposure requires explicit role,  │
│                            │ tokenized pseudonymization, and logged audit trail. │
├────────────────────────────┼─────────────────────────────────────────────────────┤
│ 4. MCP AS SOLE LLM BOUNDARY│ LLMs NEVER touch DB, repositories, or external REST │
│                            │ directly. All operations run through validated MCP  │
│                            │ tools dispatching to Application IMediator handlers.│
├────────────────────────────┼─────────────────────────────────────────────────────┤
│ 5. LOCAL-FIRST DEFAULT     │ Ollama (local) is the default runtime for all tasks │
│                            │ touching internal data. Cloud is an audited opt-in. │
├────────────────────────────┼─────────────────────────────────────────────────────┤
│ 6. DUAL MCP TRANSPORT      │ McpServer supports Stdio (local CLI/desktop agents) │
│                            │ and Streamed HTTP/SSE (distributed web platform).   │
└──────────────────────────────────────────────────────────────────────────────────┘
```

---

## 2. High-Level Monorepo Topology & Project Layout

```
analytics-platform/
├── .github/
│   └── workflows/
│       ├── ci-backend.yml                    # + MCP contract validation & LLM unit/integration tests
│       ├── ci-frontend.yml                   # + llm-assistant unit, integration, and security tests
│       ├── cd-staging.yml                    # + McpServer container deployment
│       ├── cd-production.yml                 # + McpServer & Ollama StatefulSet deployment
│       └── security-scan.yml                 # + LLM prompt injection & PII leakage blocking gates
├── docs/
│   └── architecture/
│       ├── adr/
│       │   ├── 0005-llm-orchestration-layer.md
│       │   ├── 0006-mcp-as-tool-boundary.md
│       │   ├── 0007-local-first-guardrail-policy.md
│       │   └── 0008-mcp-dual-transport-stdio-sse.md
│       └── diagrams/
│           ├── mcp-llm-orchestration-flow.mmd
│           └── guardrail-pipeline-sequence.mmd
├── backend/
│   ├── AnalyticsPlatform.slnx
│   ├── Directory.Build.props                 # Shared net10.0, Nullable, TreatWarningsAsErrors=true
│   ├── Directory.Packages.props              # CPM: MediatR, FluentValidation, Polly, Presidio/Regex
│   └── src/
│       ├── AnalyticsPlatform.Domain/         # + Features/LlmOrchestration/ (Entities, VOs, Pure Rules)
│       ├── AnalyticsPlatform.Application/    # + Features/LlmOrchestration/ (Commands, Queries, Behaviors)
│       ├── AnalyticsPlatform.Infrastructure/ # + Llm/ (OllamaClient, CloudGateways, Guardrails, Polly)
│       ├── AnalyticsPlatform.Api/            # + Endpoints/LlmEndpoints.cs (REST + SSE Streaming)
│       └── AnalyticsPlatform.McpServer/      # NEW Deployable: Stdio/SSE MCP Host + Tool Registry
├── frontend/
│   └── src/
│       └── features/
│           └── llm-assistant/                # NEW Feature Slice (AssistantPanel, ChatStream, Slice, Hooks)
├── shared-contracts/
│   ├── openapi.yaml                          # + /api/llm/tasks, /api/llm/chat/stream, /api/llm/policies
│   └── generated-types/                      # Regenerated TypeScript contracts
├── infra/
│   ├── docker/
│   │   ├── backend.Dockerfile
│   │   ├── frontend.Dockerfile
│   │   ├── mcp-server.Dockerfile             # NEW: Multi-stage .NET 10 MCP server container
│   │   ├── ollama.Dockerfile                 # NEW: Ollama container with pre-pulled models
│   │   └── docker-compose.yml                # Wired: Api, McpServer, Ollama, SqlServer, Frontend
│   └── k8s/
│       ├── base/
│       │   ├── mcp-server-deployment.yaml     # NEW
│       │   ├── mcp-server-service.yaml        # NEW
│       │   └── ollama-statefulset.yaml       # NEW: GPU node affinity & PV persistence for weights
│       └── overlays/
│           ├── dev/
│           ├── staging/
│           └── production/
└── config/
    └── llm-policies/
        ├── policy.default.json               # LocalOllama, cloud disabled, strict PII redaction
        ├── policy.analytics-nonsensitive.json # Cloud allowed for PBIR generation, DAX synthesis
        └── policy.camera-events-sensitive.json# LocalOllama forced, CCTV/event context, zero-cloud
```

---

## 3. Model Context Protocol (MCP) Server Architecture (`AnalyticsPlatform.McpServer`)

### 3.1 Architectural Separation & Purpose
`AnalyticsPlatform.McpServer` is deployed as an **independent deployable service** adhering to the official **Model Context Protocol (MCP) Specification (2024-11-05)**. It acts as the secure, isolated mediator between LLM agents (Claude Desktop, local CLI tools, Cursor, and the web `llm-assistant`) and the analytics platform's domain capabilities.

```
                    ┌─────────────────────────────────────────────────────────┐
                    │                      LLM CLIENTS                        │
                    │   (Claude Desktop / Cursor / Web UI llm-assistant)      │
                    └───────────┬─────────────────────────────────┬───────────┘
                                │ JSON-RPC 2.0 (Stdio)            │ JSON-RPC 2.0 (SSE / HTTP)
                                ▼                                 ▼
                    ┌─────────────────────────────────────────────────────────┐
                    │               AnalyticsPlatform.McpServer               │
                    │  ┌───────────────────────────────────────────────────┐  │
                    │  │          Transport & Framing Middleware           │  │
                    │  ├───────────────────────────────────────────────────┤  │
                    │  │   Capabilities Negotiation (tools, resources,     │  │
                    │  │   prompts, logging)                               │  │
                    │  ├───────────────────────────────────────────────────┤  │
                    │  │   ToolPermissionMiddleware & AuditLogger          │  │
                    │  ├───────────────────────────────────────────────────┤  │
                    │  │             ToolRegistry & JSON Schemas           │  │
                    │  └─────────────────────────┬─────────────────────────┘  │
                    └────────────────────────────┼────────────────────────────┘
                                                 │ IMediator.Send()
                                                 ▼
                    ┌─────────────────────────────────────────────────────────┐
                    │             AnalyticsPlatform.Application               │
                    │  ┌───────────────────────────────────────────────────┐  │
                    │  │  MediatR Pipeline (Validation + Logging + AuthZ)  │  │
                    │  ├───────────────────────────────────────────────────┤  │
                    │  │  Existing Handlers (CompilePbir, GetModel, etc.)  │  │
                    │  └───────────────────────────────────────────────────┘  │
                    └─────────────────────────────────────────────────────────┘
```

### 3.2 Dual Transport Architecture (Stdio + Streamed HTTP/SSE)
1. **Stdio Transport:** Standard input/output JSON-RPC 2.0 framing. Used when launched as a child process by desktop hosts (e.g., Claude Desktop, VS Code, automated agent sandboxes).
2. **Streamed HTTP / Server-Sent Events (SSE) Transport:** Hosted via Kestrel (`/mcp/sse` for server-to-client events and `/mcp/message` for client-to-server POST requests). Used for distributed container communication and web-based copilot orchestration.

### 3.3 MCP Server Project Structure

```
backend/src/AnalyticsPlatform.McpServer/
├── AnalyticsPlatform.McpServer.csproj
├── Program.cs                                # Host configuration, transport selector, DI setup
├── appsettings.json
├── Hosting/
│   ├── StdioMcpServerHost.cs                 # Stdio stream reader/writer loop
│   └── SseMcpServerHost.cs                   # Kestrel SSE + HTTP endpoint mappings
├── Protocol/
│   ├── JsonRpcRequest.cs                     # JSON-RPC 2.0 Envelope
│   ├── JsonRpcResponse.cs
│   ├── JsonRpcError.cs
│   └── McpCapabilities.cs                    # Tools, Resources, Prompts declaration
├── Security/
│   ├── ToolPermissionMiddleware.cs           # RBAC & Caller Policy Check
│   └── McpAuditLogger.cs                     # Structured invocation audit with CorrelationId
├── Tools/
│   ├── IMcpTool.cs                           # Tool contract
│   ├── ToolRegistry.cs                       # Registration & Dispatcher
│   ├── Analytics/
│   │   ├── GetAnalyticsModelTool.cs          # Wraps GetAnalyticsModelQuery
│   │   └── ValidateAnalyticsModelTool.cs     # Wraps ValidateAnalyticsModelCommand
│   ├── PowerBi/
│   │   ├── CompilePbirDefinitionTool.cs      # Wraps CompilePbirDefinitionCommand
│   │   ├── CompileTmdlSemanticModelTool.cs   # Wraps CompileTmdlSemanticModelCommand
│   │   ├── CompilePbipPackageTool.cs         # Wraps CompilePbipProjectCommand
│   │   └── PublishPbipToFabricTool.cs        # Wraps PublishPbipToFabricCommand (Privileged)
│   ├── Dashboards/
│   │   └── GetDashboardDefinitionTool.cs     # Wraps GetDashboardDefinitionQuery
│   ├── DataSources/
│   │   └── GetDataSourceSchemaTool.cs        # Wraps GetDataSourceSchemaQuery
│   └── SecurityAudit/
│       └── QueryEventSummaryTool.cs          # Wraps QueryEventSummaryQuery (Sensitive Context)
├── Resources/
│   ├── IMcpResourceProvider.cs
│   ├── DashboardResourceProvider.cs          # Exposes dashboard definitions as mcp://dashboards/{id}
│   └── SemanticModelResourceProvider.cs      # Exposes TMDL schemas as mcp://models/{id}
├── Prompts/
│   ├── IMcpPromptProvider.cs
│   └── AnalyticsPromptProvider.cs            # Predefined prompt workflows (e.g. explain-dashboard)
└── Schemas/
    ├── compile-pbir-input.json
    ├── compile-pbir-output.json
    ├── get-analytics-model-input.json
    └── query-event-summary-input.json
```

### 3.4 Tool Contract Specification & Implementation Pattern

Every MCP tool is a strictly typed adapter implementing `IMcpTool`. Tools **never** instantiate DbContext, read file systems, or call external REST endpoints directly. They dispatch into the `Application` layer via `IMediator`:

```csharp
namespace AnalyticsPlatform.McpServer.Tools;

public interface IMcpTool
{
    string Name { get; }
    string Description { get; }
    string InputSchemaJson { get; }
    string OutputSchemaJson { get; }
    Task<McpToolExecutionResult> ExecuteAsync(JsonElement inputParameters, string correlationId, CancellationToken ct);
}
```

#### Reference Implementation: `CompilePbirDefinitionTool`

```csharp
namespace AnalyticsPlatform.McpServer.Tools.PowerBi;

public sealed class CompilePbirDefinitionTool : IMcpTool
{
    private readonly IMediator _mediator;
    private readonly ILogger<CompilePbirDefinitionTool> _logger;

    public string Name => "compile_pbir_definition";
    public string Description => "Compiles an analytics dashboard IR into PBIR report definition files (report.json, pages, visuals).";
    public string InputSchemaJson => """
    {
      "type": "object",
      "properties": {
        "dashboardDefinitionId": { "type": "string", "format": "uuid", "description": "The unique ID of the dashboard definition" },
        "semanticModelRelativePath": { "type": "string", "description": "Relative path to TMDL semantic model" }
      },
      "required": ["dashboardDefinitionId"]
    }
    """;

    public string OutputSchemaJson => """
    {
      "type": "object",
      "properties": {
        "success": { "type": "boolean" },
        "fileCount": { "type": "integer" },
        "reportJsonPath": { "type": "string" }
      }
    }
    """;

    public CompilePbirDefinitionTool(IMediator _mediator, ILogger<CompilePbirDefinitionTool> logger)
    {
        _mediator = _mediator;
        _logger = logger;
    }

    public async Task<McpToolExecutionResult> ExecuteAsync(JsonElement inputParameters, string correlationId, CancellationToken ct)
    {
        _logger.LogInformation("[MCP Tool {ToolName}] Invoking with CorrelationId: {CorrelationId}", Name, correlationId);
        
        var dashboardId = inputParameters.GetProperty("dashboardDefinitionId").GetGuid();
        var modelPath = inputParameters.TryGetProperty("semanticModelRelativePath", out var elem) 
            ? elem.GetString() 
            : "../definition";

        var command = new CompilePbirDefinitionCommand(dashboardId, modelPath ?? "../definition");
        var result = await _mediator.Send(command, ct);

        if (!result.IsSuccess)
        {
            return McpToolExecutionResult.Failed(string.Join("; ", result.Errors));
        }

        return McpToolExecutionResult.Success(new
        {
            success = true,
            fileCount = result.Value?.Files?.Count ?? 0,
            reportJsonPath = "definition/report.json"
        });
    }
}
```

---

## 4. Backend Clean / Hexagonal Architecture (.NET 10)

The LLM orchestration subsystem is structured within the established Clean Architecture layers:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                      AnalyticsPlatform.Domain                                │
│  Features/LlmOrchestration/                                                 │
│    Entities:       LlmTask, LlmTaskResult, GuardrailDecision, LlmSession    │
│    Value Objects:  LlmProvider, LlmPolicy, SensitivityLevel, TokenUsage     │
│    Rules:          ProviderSelectionRules, SensitiveExposureRules           │
└──────────────────────────────────────┬──────────────────────────────────────┘
                                       ▲
┌──────────────────────────────────────┴──────────────────────────────────────┐
│                    AnalyticsPlatform.Application                            │
│  Features/LlmOrchestration/                                                 │
│    Commands:       RunLlmTaskCommand, StreamLlmTaskCommand, RegisterPolicy  │
│    Queries:        GetLlmPoliciesQuery, GetLlmTaskHistoryQuery              │
│    Pipeline:       PromptGuardrailBehavior, ResponseGuardrailBehavior       │
│    Interfaces:     ILlmGateway, IOllamaClient, ICloudLlmClient,             │
│                    IPromptGuardrailService, IResponseGuardrailService       │
└──────────────────────────────────────┬──────────────────────────────────────┘
                                       ▲
┌──────────────────────────────────────┴──────────────────────────────────────┐
│                    AnalyticsPlatform.Infrastructure                         │
│  Llm/                                                                       │
│    Providers:      OllamaLocalClient, OpenAiCloudClient, AnthropicClient    │
│    Guardrails:     PiiRedactionService, PromptInjectionDetector             │
│    Policy:         LlmPolicyRepository, ProviderRouter                      │
│    Resilience:     PollyLlmResiliencePipeline                               │
│    Persistence:    Configurations/LlmTaskConfiguration, AuditLogRepository  │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 4.1 Domain Layer: Entities, Value Objects & Pure Rules

#### Value Objects & Enums

```csharp
namespace AnalyticsPlatform.Domain.Features.LlmOrchestration.ValueObjects;

public enum LlmProviderType
{
    LocalOllama = 1,
    CloudOpenAi = 2,
    CloudAnthropic = 3,
    CloudOllama = 4
}

public enum SensitivityLevel
{
    Public = 0,
    Internal = 1,
    Sensitive = 2,
    Restricted = 3
}

public sealed record LlmPolicy(
    string PolicyId,
    string Name,
    bool AllowCloudProvider,
    bool AllowSensitiveContext,
    IReadOnlyList<string> AllowedTools,
    int MaxTokensPerRequest,
    int MaxDailyTokenBudget,
    SensitivityLevel MaximumAllowedSensitivity
);

public sealed record TokenUsage(
    int PromptTokens,
    int CompletionTokens,
    int TotalTokens,
    decimal EstimatedCostUsd
);
```

#### Pure Domain Rules: `ProviderSelectionRules`

```csharp
namespace AnalyticsPlatform.Domain.Features.LlmOrchestration.Rules;

public static class ProviderSelectionRules
{
    public static Result<LlmProviderType> ResolveProvider(
        LlmPolicy policy,
        SensitivityLevel taskSensitivity,
        LlmProviderType requestedPreference,
        bool userHasCloudPrivilege)
    {
        // Rule 1: Restricted or Sensitive data NEVER leaves the local perimeter
        if (taskSensitivity >= SensitivityLevel.Sensitive)
        {
            return Result<LlmProviderType>.Success(LlmProviderType.LocalOllama);
        }

        // Rule 2: If policy explicitly forbids cloud, force LocalOllama regardless of request
        if (!policy.AllowCloudProvider)
        {
            return Result<LlmProviderType>.Success(LlmProviderType.LocalOllama);
        }

        // Rule 3: User role constraint
        if (!userHasCloudPrivilege && requestedPreference != LlmProviderType.LocalOllama)
        {
            return Result<LlmProviderType>.Success(LlmProviderType.LocalOllama);
        }

        // Rule 4: If preference is valid under policy, honor it
        return Result<LlmProviderType>.Success(requestedPreference);
    }
}
```

### 4.2 Application Layer: Commands, Pipeline Behaviors & Gateway Contracts

#### MediatR Pipeline Guardrails
The pipeline guarantees that every command implementing `ILlmGuardedRequest` is automatically validated, sanitized, and audited before hitting any handler:

```csharp
namespace AnalyticsPlatform.Application.Features.LlmOrchestration.Behaviors;

public sealed class PromptGuardrailBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ILlmGuardedRequest
{
    private readonly IPromptGuardrailService _guardrails;
    private readonly ILlmPolicyRepository _policies;
    private readonly ILogger<PromptGuardrailBehavior<TRequest, TResponse>> _logger;

    public PromptGuardrailBehavior(
        IPromptGuardrailService guardrails,
        ILlmPolicyRepository policies,
        ILogger<PromptGuardrailBehavior<TRequest, TResponse>> logger)
    {
        _guardrails = guardrails;
        _policies = policies;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var policy = await _policies.GetPolicyByIdAsync(request.PolicyId, ct);
        var guardrailResult = await _guardrails.ValidateAndSanitizeAsync(
            request.UserPrompt, 
            request.ContextPayload, 
            policy, 
            request.CorrelationId, 
            ct);

        if (guardrailResult.IsBlocked)
        {
            _logger.LogWarning("[Guardrail Blocked] Task blocked by reason: {Reason} (CorrelationId: {CorrelationId})",
                guardrailResult.BlockReason, request.CorrelationId);
            
            throw new SecurityException($"Prompt blocked by security guardrails: {guardrailResult.BlockReason}");
        }

        // Inject sanitized content into mutable request wrapper
        request.UpdateSanitizedPrompt(guardrailResult.SanitizedPrompt);

        return await next();
    }
}
```

### 4.3 Infrastructure Layer: Ollama Client, Cloud Gateways & Resilience

#### Resilience Pipeline with Polly v8
All LLM API calls pass through a centralized Polly v8 resilience strategy configuring timeouts, retries with jitter, and circuit breakers:

```csharp
namespace AnalyticsPlatform.Infrastructure.Llm.Resilience;

public static class PollyLlmResilience
{
    public static ResiliencePipeline CreateLlmPipeline()
    {
        return new ResiliencePipelineBuilder()
            .AddTimeout(TimeSpan.FromSeconds(45))
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                Delay = TimeSpan.FromSeconds(2),
                ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>()
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(30),
                MinimumThroughput = 5,
                BreakDuration = TimeSpan.FromSeconds(30)
            })
            .Build();
    }
}
```

#### Ollama Local Client (`OllamaLocalClient.cs`)
Supports JSON schema grammar constraints, SSE streaming, and local vector embeddings:

```csharp
namespace AnalyticsPlatform.Infrastructure.Llm.Providers;

public sealed class OllamaLocalClient : IOllamaClient
{
    private readonly HttpClient _httpClient;
    private readonly ResiliencePipeline _resilience;
    private readonly ILogger<OllamaLocalClient> _logger;

    public OllamaLocalClient(HttpClient httpClient, ILogger<OllamaLocalClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _resilience = PollyLlmResilience.CreateLlmPipeline();
    }

    public async Task<OllamaChatResponse> SendChatAsync(OllamaChatRequest request, CancellationToken ct)
    {
        return await _resilience.ExecuteAsync(async state =>
        {
            var response = await _httpClient.PostAsJsonAsync("/api/chat", state, ct);
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<OllamaChatResponse>(cancellationToken: ct))!;
        }, request, ct);
    }

    public async IAsyncEnumerable<string> StreamChatAsync(OllamaChatRequest request, [EnumeratorCancellation] CancellationToken ct)
    {
        request = request with { Stream = true };
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/chat")
        {
            Content = JsonContent.Create(request)
        };

        using var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrWhiteSpace(line)) continue;

            var chunk = JsonSerializer.Deserialize<OllamaStreamChunk>(line);
            if (chunk?.Message?.Content != null)
            {
                yield return chunk.Message.Content;
            }
        }
    }
}
```

### 4.4 Presentation / API Layer (`AnalyticsPlatform.Api`)

Minimal API endpoint mappings in [`LlmEndpoints.cs`](file:///d:/PowerBIEnhanced/backend/src/AnalyticsPlatform.Api/Endpoints/LlmEndpoints.cs):

```csharp
namespace AnalyticsPlatform.Api.Endpoints;

public static class LlmEndpoints
{
    public static void MapLlmEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/llm").WithTags("LLM Orchestration");

        group.MapPost("/tasks", async (RunLlmTaskCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return result.IsSuccess && result.Value != null
                ? Results.Ok(result.Value)
                : Results.BadRequest(result.Errors);
        });

        group.MapGet("/policies", async (ISender sender) =>
        {
            var result = await sender.Send(new GetLlmPoliciesQuery());
            return Results.Ok(result);
        });

        group.MapPost("/chat/stream", async (StreamLlmChatCommand command, ISender sender, HttpContext context, CancellationToken ct) =>
        {
            context.Response.Headers.Append("Content-Type", "text/event-stream");
            context.Response.Headers.Append("Cache-Control", "no-cache");
            context.Response.Headers.Append("X-Accel-Buffering", "no");

            var stream = await sender.Send(command, ct);
            await foreach (var token in stream.WithCancellation(ct))
            {
                await context.Response.WriteAsync($"data: {JsonSerializer.Serialize(new { token })}\n\n", ct);
                await context.Response.Body.FlushAsync(ct);
            }
            await context.Response.WriteAsync("data: [DONE]\n\n", ct);
        });
    }
}
```

---

## 5. Frontend Feature Slice: `llm-assistant` (React 19 + TypeScript 5.8)

### 5.1 Feature Directory Layout (Feature-Sliced Design)

```
frontend/src/features/llm-assistant/
├── index.ts                              # Public API of the feature
├── api/
│   ├── llmApi.ts                         # REST endpoints (POST /api/llm/tasks, GET policies)
│   └── llmStreamClient.ts                # Fetch + ReadableStream SSE client
├── components/
│   ├── AssistantPanel.tsx                # Collapsible docking container in App shell
│   ├── ChatStream.tsx                    # Token streaming renderer with Markdown & KaTeX
│   ├── ProviderSelector.tsx              # Local vs Cloud selector with policy disablement
│   ├── SensitiveModeToggle.tsx           # Role-gated sensitive data toggle
│   ├── GuardrailNotice.tsx               # Banner showing redacted PII or policy downgrades
│   └── ToolExecutionBadge.tsx            # Animated pill indicating active MCP tool call
├── hooks/
│   ├── useLlmTask.ts                     # TanStack Query mutation for atomic tasks
│   ├── useLlmStream.ts                   # Custom hook managing SSE streaming state
│   └── useLlmPolicies.ts                 # Queries available policy constraints
└── model/
    ├── types.ts                          # TypeScript contracts matching OpenAPI
    └── llmAssistantSlice.ts              # Zustand store for panel state, history, active tools
```

### 5.2 Streaming SSE Client Implementation (`llmStreamClient.ts`)

```typescript
export interface StreamChatOptions {
  userPrompt: string;
  contextIds: string[];
  providerPreference: "auto" | "local" | "cloud";
  onToken: (token: string) => void;
  onToolCall?: (toolName: string) => void;
  onGuardrailViolation?: (warning: string) => void;
  signal?: AbortSignal;
}

export async function streamLlmChat(options: StreamChatOptions): Promise<void> {
  const correlationId = crypto.randomUUID();
  const response = await fetch("/api/llm/chat/stream", {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      "X-Correlation-Id": correlationId
    },
    body: JSON.stringify({
      userPrompt: options.userPrompt,
      contextIds: options.contextIds,
      providerPreference: options.providerPreference
    }),
    signal: options.signal
  });

  if (!response.ok || !response.body) {
    throw new Error(`Failed to initiate stream: ${response.statusText}`);
  }

  const reader = response.body.getReader();
  const decoder = new TextDecoder("utf-8");
  let buffer = "";

  while (true) {
    const { done, value } = await reader.read();
    if (done) break;

    buffer += decoder.decode(value, { stream: true });
    const lines = buffer.split("\n");
    buffer = lines.pop() ?? "";

    for (const line of lines) {
      const trimmed = line.trim();
      if (!trimmed.startsWith("data:")) continue;
      const dataStr = trimmed.replace("data:", "").trim();
      if (dataStr === "[DONE]") return;

      try {
        const payload = JSON.parse(dataStr);
        if (payload.token) options.onToken(payload.token);
        if (payload.activeTool) options.onToolCall?.(payload.activeTool);
        if (payload.guardrailNotice) options.onGuardrailViolation?.(payload.guardrailNotice);
      } catch (err) {
        console.error("Malformed SSE event payload", err);
      }
    }
  }
}
```

---

## 6. Shared Contracts (OpenAPI 3.0.3 Additions)

The following paths and schemas are merged into [`shared-contracts/openapi.yaml`](file:///d:/PowerBIEnhanced/shared-contracts/openapi.yaml):

```yaml
  /api/llm/tasks:
    post:
      summary: Dispatch an LLM task through guardrail pipeline and provider router
      tags: [LLM Orchestration]
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/RunLlmTaskRequest'
      responses:
        "200":
          description: Task completed successfully
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/LlmTaskResultDto'
        "400":
          description: Guardrail violation or validation error

  /api/llm/chat/stream:
    post:
      summary: Stream tokens via Server-Sent Events (SSE)
      tags: [LLM Orchestration]
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/StreamLlmChatRequest'
      responses:
        "200":
          description: Server-sent event stream
          content:
            text/event-stream:
              schema:
                type: string

components:
  schemas:
    RunLlmTaskRequest:
      type: object
      required: [taskType, userPrompt, providerPreference]
      properties:
        taskType: { type: string, example: "ExplainDashboard" }
        userPrompt: { type: string, example: "Analyze anomalies in revenue measure" }
        contextIds: { type: array, items: { type: string } }
        providerPreference: { type: string, enum: [auto, local, cloud] }
        sensitiveMode: { type: boolean, default: false }

    LlmTaskResultDto:
      type: object
      required: [text, provider, blocked, correlationId]
      properties:
        text: { type: string }
        provider: { type: string, enum: [LocalOllama, CloudOpenAi, CloudAnthropic, CloudOllama] }
        blocked: { type: boolean }
        guardrailNotice: { type: string }
        tokenUsage:
          type: object
          properties:
            promptTokens: { type: integer }
            completionTokens: { type: integer }
            totalTokens: { type: integer }
        correlationId: { type: string, format: uuid }
```

---

## 7. Security, Privacy & Guardrail Architecture

```
                                PROMPT INVOCATION
                                       │
                                       ▼
                     ┌───────────────────────────────────┐
                     │    PromptInjectionDetector        │
                     │  - Heuristic Jailbreak Filter     │
                     │  - Vector Cosine Similarity Check │
                     └─────────────────┬─────────────────┘
                                       │ Passed
                                       ▼
                     ┌───────────────────────────────────┐
                     │      PiiRedactionService          │
                     │  - Reversible Token Pseudonymizer │
                     │  - Regex + Entity Replacement     │
                     │    ("Jane" -> "{{PERSON_1}}")     │
                     └─────────────────┬─────────────────┘
                                       │
                                       ▼
                     ┌───────────────────────────────────┐
                     │     ProviderSelectionRules        │
                     │  - Sensitive Context? -> FORCE    │
                     │    LocalOllama                    │
                     └─────────────────┬─────────────────┘
                                       │
                                       ▼
                              EXECUTE LLM / MCP
                                       │
                                       ▼
                     ┌───────────────────────────────────┐
                     │     OutputContentFilter           │
                     │  - Hallucinated Secret Scan       │
                     │  - De-anonymization Mapping       │
                     └─────────────────┬─────────────────┘
                                       │
                                       ▼
                               CLEAN RESPONSE
```

### 7.1 PII Redaction & Reversible Pseudonymization
To allow natural document and report synthesis without exposing real identity information, `PiiRedactionService` utilizes deterministic tokenized replacement:
- Names are mapped to `{{PERSON_N}}`.
- IP addresses are mapped to `{{IP_N}}`.
- Email addresses are mapped to `{{EMAIL_N}}`.
- The mapping dictionary is kept strictly in local memory and scoped only to the immediate request execution context.

### 7.2 Adversarial Prompt Injection Defense
- **Stage 1 (Heuristic Signature Matching):** Scans for known prompt escape tokens (`"Ignore all previous instructions"`, `"<system>"`, `"DAN mode"`).
- **Stage 2 (Local Vector Embedding Cosine Distance):** Generates an embedding via Ollama's `nomic-embed-text` and calculates cosine distance against a database of known adversarial jailbreak vectors. If similarity exceeds `0.82`, execution is immediately halted.

---

## 8. Observability, Distributed Tracing & Telemetry

1. **Correlation ID End-to-End Tracing:** The `X-Correlation-Id` initialized by the frontend or API gateway is passed to `LlmTask`, propagated into the MCP Server via JSON-RPC metadata, injected into MCP Tool execution, and tagged in all Serilog log lines.
2. **Audit Sink (`GuardrailDecision`):** Every single guardrail decision (allowed, redacted, or blocked) writes a tamper-evident audit record containing:
   - `CorrelationId` (Guid)
   - `TimestampUtc` (DateTime)
   - `UserId` / `TenantId`
   - `PolicyId`
   - `ProviderSelected`
   - `ViolationsDetected` (Array of violation codes)
   - `TokenCount`
3. **Health Checks:**
   - `/health/live` & `/health/ready` include `OllamaHealthCheck` (validates HTTP reachability to `http://localhost:11434/api/version`) and `McpServerHealthCheck`.

---

## 9. Comprehensive 6-Category Testing Infrastructure Matrix

Testing rigorously covers both backend and frontend across the exact six categories:

| Category | Backend Target & Tooling | Frontend Target & Tooling | CI Gate Behavior |
|---|---|---|---|
| **1. Unit** | `ProviderSelectionRulesTests`, `SensitiveExposureRulesTests`, `PromptGuardrailBehaviorTests` (xUnit + FluentAssertions + NSubstitute) | `ProviderSelector.test.tsx`, `useLlmStream.test.ts`, `dashboardSlice.test.ts` (Vitest + RTL) | Runs on every commit |
| **2. Integration** | `OllamaLocalClientTests` (Ollama Testcontainer), `McpServerContractTests` (Schema validation against JSON-RPC) | `llmApi.integration.test.ts` (Mock Service Worker / MSW contract verification) | Runs on PR |
| **3. Path** | `LlmSensitiveTaskBlockedPathTests` (Forces local provider on sensitive context), `LlmMcpToolInvocationPathTests` | `llm-assistant-sensitive-mode-path.test.tsx` (Verifies state downgrade and warning banner) | Runs on PR |
| **4. Regression** | `McpToolSchemasRegressionTests` (Verify.Xunit golden-file JSON schema diffs) | `visual-layout-schema.regression.test.ts`, snapshot regression tests | Runs Nightly & PR |
| **5. E2E** | `LlmAssistedDashboardWorkflow.feature` (Reqnroll BDD scenario: Prompt -> MCP Tool -> PBIR Generated) | `llm-assisted-authoring.spec.ts` (Playwright browser automation) | Pre-release / Nightly |
| **6. Security (BLOCKING)** | `SensitiveDataLeakageTests`, `PromptInjectionFuzzTests`, `ApiKeyExposureTests`, `NetArchTest` | `llm-prompt-xss.test.tsx`, `auth-token-exposure.test.ts` | **BLOCKING GATE** (Fails build on any leak) |

---

## 10. Containerization, Kubernetes & Deployment Topology

### 10.1 MCP Server Multi-Stage Dockerfile (`infra/docker/mcp-server.Dockerfile`)

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["backend/Directory.Build.props", "backend/Directory.Packages.props", "./backend/"]
COPY ["backend/src/AnalyticsPlatform.McpServer/AnalyticsPlatform.McpServer.csproj", "backend/src/AnalyticsPlatform.McpServer/"]
COPY ["backend/src/AnalyticsPlatform.Application/AnalyticsPlatform.Application.csproj", "backend/src/AnalyticsPlatform.Application/"]
COPY ["backend/src/AnalyticsPlatform.Domain/AnalyticsPlatform.Domain.csproj", "backend/src/AnalyticsPlatform.Domain/"]
RUN dotnet restore "backend/src/AnalyticsPlatform.McpServer/AnalyticsPlatform.McpServer.csproj"

COPY backend/ ./backend/
WORKDIR "/src/backend/src/AnalyticsPlatform.McpServer"
RUN dotnet publish "AnalyticsPlatform.McpServer.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5005
ENTRYPOINT ["dotnet", "AnalyticsPlatform.McpServer.dll"]
```

### 10.2 Kubernetes Topology (`infra/k8s/base/ollama-statefulset.yaml`)
- Self-hosted Ollama deployed as a Kubernetes `StatefulSet` with GPU node affinity (`nvidia.com/gpu: 1`).
- Persistent Volume Claim (PVC) mounted to `/root/.ollama` to cache pre-downloaded LLM weights (`llama3.3:8b`, `deepseek-r1:8b`).
- ClusterIP Service restricting Ollama exposure strictly to backend pods within the internal Kubernetes network perimeter.

---

## 11. Concrete Sequenced Implementation Roadmap (8 Phases)

To ensure zero regressions and continuous delivery of working software, development follows this strict sequence:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                       SEQUENCED IMPLEMENTATION PHASES                       │
├─────────────────────────────────────────────────────────────────────────────┤
│ PHASE 1: DOMAIN CORE & RULES (ZERO I/O)                                     │
│ - Implement LlmTask, LlmPolicy, SensitivityLevel, ProviderSelectionRules   │
│ - Deliver ProviderSelectionRulesTests (100% branch coverage)                │
├─────────────────────────────────────────────────────────────────────────────┤
│ PHASE 2: APPLICATION LAYER & PIPELINE GUARDRAILS                            │
│ - Implement RunLlmTaskCommand, MediatR Prompt & Response Guardrails        │
│ - Deliver RunLlmTaskCommandHandlerTests with NSubstitute mock adapters      │
├─────────────────────────────────────────────────────────────────────────────┤
│ PHASE 3: LOCAL OLLAMA CLIENT & RESILIENCE                                   │
│ - Implement OllamaLocalClient, Polly resilience pipeline, SSE parser        │
│ - Deliver OllamaLocalClientTests against Ollama container fixture           │
├─────────────────────────────────────────────────────────────────────────────┤
│ PHASE 4: MCP SERVER HOST & READ-ONLY TOOLS                                  │
│ - Scaffold AnalyticsPlatform.McpServer with Stdio & SSE hosts              │
│ - Implement get_analytics_model and get_dashboard_definition tools          │
│ - Deliver McpServerContractTests schema verification                        │
├─────────────────────────────────────────────────────────────────────────────┤
│ PHASE 5: MCP COMPILATION TOOLS (PBIR & TMDL)                                │
│ - Implement compile_pbir_definition and compile_tmdl_model tools            │
│ - Deliver LlmToolInvocationPathTests (Command -> Tool -> PBIR files)        │
├─────────────────────────────────────────────────────────────────────────────┤
│ PHASE 6: CLOUD GATEWAYS & POLICY-FLAGGED ROUTING                            │
│ - Implement OpenAiCloudClient & AnthropicCloudClient                        │
│ - Wire Azure Key Vault / User Secrets credential loading                    │
│ - Deliver ApiKeyExposureTests and SensitiveDataLeakageTests                 │
├─────────────────────────────────────────────────────────────────────────────┤
│ PHASE 7: FRONTEND LLM-ASSISTANT FEATURE SLICE                               │
│ - Build AssistantPanel, ChatStream, ProviderSelector, SensitiveModeToggle  │
│ - Wire Zustand store and SSE client; add MSW mock handlers & tests         │
├─────────────────────────────────────────────────────────────────────────────┤
│ PHASE 8: E2E BDD INTEGRATION & CI/CD GATES                                  │
│ - Author Reqnroll scenario: LlmAssistedDashboardExplanation.feature         │
│ - Configure blocking CI security gates in GitHub Actions workflows          │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 12. Architectural Conformance & Verification Checklist

Before merging any code belonging to this subsystem, the following verification checklist must be satisfied:

- [ ] **Architecture Test Passed:** `Domain_Should_Not_DependOn_OtherLayers` passes in `AnalyticsPlatform.SecurityTests`.
- [ ] **Zero Warnings:** Build passes with `TreatWarningsAsErrors=true` across all projects.
- [ ] **Security Gates Green:** `SensitiveDataLeakageTests` and `ApiKeyExposureTests` execute and pass.
- [ ] **Schema Conformance:** MCP JSON schemas match the MCP 2024-11-05 specification.
- [ ] **Correlation Propagation:** `X-Correlation-Id` is verified across Frontend -> Api -> MediatR -> McpServer -> Tool.
- [ ] **Contract Sync:** `shared-contracts/openapi.yaml` matches TypeScript contracts in `frontend/src/shared/types/api-contracts.ts`.
- [ ] **Local-First Verification:** With cloud policies disabled, all tasks succeed locally using Ollama.
