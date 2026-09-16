# LLM Orchestration & Model Context Protocol (MCP) API

The platform exposes both REST SSE endpoints for browser-based assistants and a standalone MCP Server (Stdio & SSE) for autonomous AI agents.

## REST Endpoints

### 1. Stream Chat Tokens
`POST /api/llm/chat/stream`
- **Request Body**:
  ```json
  {
    "userPrompt": "Summarize revenue trend by product line",
    "contextIds": ["3fa85f64-5717-4562-b3fc-2c963f66afa6"],
    "providerPreference": "LocalOllama",
    "policyId": "default",
    "correlationId": "corr-101"
  }
  ```
- **Response**: `200 OK` (`text/event-stream`) streaming `data: <token>\n\n` ending with `data: [DONE]\n\n`.

### 2. Run Governed LLM Task
`POST /api/llm/tasks/run`
- **Description**: Executes prompt guardrails, PII sanitization, provider routing, and output content filtering.
- **Response**: `200 OK` (`LlmTaskResultDto`).

## Model Context Protocol (MCP) Server

- **Transports**:
  - **Stdio**: JSON-RPC 2.0 over standard I/O (flag: `--stdio`).
  - **Kestrel SSE**: `GET /mcp/sse` (event channel) and `POST /mcp/message` (JSON-RPC requests).
- **Supported Tools**:
  - `get_analytics_model`
  - `validate_analytics_model`
  - `compile_pbir_definition`
  - `compile_tmdl_semantic_model`
  - `compile_pbip_package`
  - `publish_pbip_to_fabric` (Privileged)
  - `get_dashboard_definition`
  - `get_data_source_schema`
  - `query_event_summary` (Sensitive)

