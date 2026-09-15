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
