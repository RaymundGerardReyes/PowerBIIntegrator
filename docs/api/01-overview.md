# Enterprise Analytics Platform - API Overview

The Enterprise Power BI Analytics Platform exposes a high-performance Minimal API built on .NET 10 LTS. The API adheres strictly to Clean Architecture, CQRS with MediatR, and OpenAPI 3.0 standards.

## Base URL & Protocols
- **Local Development**: `https://localhost:7148` or `http://localhost:5000`
- **Container / Ingress**: `https://analytics.enterprise.internal/api`
- **Protocol**: HTTP/1.1 and HTTP/2 with JSON payloads (`application/json`), SSE streams (`text/event-stream`), and binary document streams (`application/pdf`, `application/zip`, `application/vnd.openxmlformats-*`).

## Authentication & Security
- **Authentication**: JWT Bearer token via Entra ID (Azure AD) or Service Principal authentication.
- **Authorization**: Role-based access control (`Admin`, `Analyst`, `Viewer`).
- **Security Headers**: Content-Security-Policy (CSP), Strict-Transport-Security (HSTS), X-Content-Type-Options (`nosniff`), and X-Frame-Options.
- **Input Sanitization**: Formula injection mitigation on tabular exports (`=`, `+`, `-`, `@`), path traversal protection (`..`), and PII prompt redaction.

## API Subsystems
1. [Power BI PBIP & TMDL Compiler API](02-powerbi-compiler-api.md)
2. [Data Sources & Schema Extraction API](03-data-sources-api.md)
3. [Multi-Target Report Generation API](04-report-generation-api.md)
4. [LLM Orchestration & MCP Protocol API](05-llm-and-mcp-api.md)

