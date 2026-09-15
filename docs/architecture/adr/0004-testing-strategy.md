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
