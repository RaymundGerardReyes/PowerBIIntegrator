# ADR 0003: Power BI PBIR as a Compiler Target

## Status
Accepted

## Decision
Treat Power BI as a target runtime: the canonical analytics/dashboard IR
compiles to PBIR report definitions and TMDL semantic models inside a PBIP
project, published via Fabric REST APIs, rather than attempting to execute
arbitrary compiled code inside Power BI.

## Consequences
Report generation is diffable, source-controlled, and multi-target
(Excel/Word/PDF/Power BI) from one canonical model.
