---
name: pbir-schema-rules
description: Enforce exact schema structure for Power BI PBIR generation
trigger: always_on
---

# Constraint
When generating or modifying `.pbip` or PBIR definition files (specifically `report.json` and `pages.json`), the schema format must be strictly compliant with Power BI Desktop's expectations to avoid import errors.

## Rules
- `report.json` must NOT contain `activePageIndex` or `activePageName`.
- `report.json` must include `layoutOptimization` set to the string `"None"` (NOT numeric `0`).
- Inside `report.json`'s `themeCollection.baseTheme`, use `reportVersionAtImport` instead of `version`.
- `definition/pages/pages.json` must contain both `pageOrder` (array of page names) AND `activePageName` (string name of default active page).
- A PBIR report must always contain at least one page. If the underlying data structure has no pages, you must generate a default `Page1`.
- Page folder names and `page.json` `name` properties must be clean alphanumeric identifiers (e.g. `OverviewAnalytics`), using `displayName` for human-readable labels (e.g. `Overview & Analytics`).
