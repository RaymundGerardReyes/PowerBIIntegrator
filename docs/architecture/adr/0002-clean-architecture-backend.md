# ADR 0002: Clean Architecture for the Backend

## Status
Accepted

## Decision
Backend is layered as Domain -> Application -> Infrastructure -> Api, with
dependencies pointing inward only, enforced by NetArchTest architecture tests.

## Consequences
Business logic (measures, dashboard IR, publish eligibility) is testable in
isolation from EF Core, Power BI SDK, and HTTP concerns.
