# Aesthetic & Minimalistic UI/UX Design Plan
### For the Analytics Platform (React 19 + TypeScript 7 Frontend, Power BI Embed Surface)

Owner: Principal Software Engineer / Design-Systems mindset
Scope: Concrete, research-backed plan to make the dashboard/report UI both aesthetically refined and minimalistic, plus a reusable prompt framework for generating UI with AI tools when needed.
AI note: The data-quality engine remains rule-driven and deterministic; any AI usage here is scoped strictly to optional UI ideation/prompting, not runtime logic.

---

## 1. Design Goals

- **Minimalism with purpose**, not emptiness: remove clutter while keeping every remaining element functional — "less but better," achieved through clean layouts, generous white space, a limited palette, clear typography, and intuitive navigation.[web:130]
- **Aesthetic-usability effect applied deliberately**: Nielsen's heuristic states interfaces should not contain information that is irrelevant or rarely needed — every visible element must earn its place.[web:132]
- **Consistency and hierarchy** across all screens (dashboards, data-quality review panels, Power BI embed surface) so the product feels like one coherent system, not stitched-together views.[web:128]
- **Accessible by default**, not as an afterthought — contrast, spacing, and typography must meet WCAG AA at minimum.[web:134][web:135][web:142]

---

## 2. Core Visual Design Principles

### 2.1 Whitespace as a structural tool
Whitespace is the first principle of minimal, flat UI: it separates unrelated elements, groups related ones, and gives content room to breathe rather than being packed edge-to-edge.[web:124] In dashboard contexts specifically, group related items and separate unrelated ones with space rather than dividing lines — lines add visual noise, spacing adds clarity.[web:123]

### 2.2 Limited, functional color palette
- Use 1–2 brand/primary colors, 5–7 neutral gray shades for structure, and a small set of semantic colors (success, warning, error, info) — this is the standard shape of a modern design system's color system.[web:129][web:131]
- Apply color **sparingly and intentionally**: to signal urgency, state changes, or category — never purely decorative. Limiting palette to functional use is a repeated best practice across dashboard design guides.[web:133][web:120]
- Each color needs defined tonal gradations (e.g., `brand-100` → `brand-900`) with documented contrast specifications so any two adjacent colors are accessibility-safe by construction.[web:131]

### 2.3 Typography hierarchy
- Establish a clear scale: headings larger than subheadings, subheadings larger than body text (e.g., H1 ≈ 48px, body ≈ 16px), so hierarchy is visually obvious without relying on color or weight alone.[web:119]
- Body text should be at least 16px; avoid anything below 9pt for any UI text, including dense data tables.[web:125]
- Line height should sit between 1.125–1.5× the font size — closer to 1.5× for body copy and dyslexia/low-vision friendliness, tighter for large display headings.[web:119][web:125]
- Use semantic type tokens (`text-heading-lg`, `text-body-md`, `text-caption`) rather than raw pixel values scattered through components, so the whole system can be restyled from one place.[web:125]

### 2.4 Spacing system
- Base all spacing on a 4px or 8px increment scale (4, 8, 12, 16, 24, 32, 48, 64...) — this is the near-universal convention in modern design systems and keeps every margin/padding decision consistent and predictable.[web:131]
- Consistent spacing, alignment, and typography together are repeatedly cited as the top three levers for a "clean" dashboard UI.[web:133]

### 2.5 Grid and layout
- Use a consistent grid system to align every dashboard element — cards, charts, KPIs — so nothing feels randomly placed.[web:126]
- Position high-priority items following proven scanning patterns: F-pattern for text-heavy views, Z-pattern for simpler, action-oriented screens.[web:133]
- Use size (not just color) to signal importance: primary KPIs should visually dominate secondary/supporting stats.[web:133]

---

## 3. Dashboard-Specific UX Principles (directly applicable to your Power BI embed + custom canvas)

Five core dashboard design principles consistently appear across current guidance: establish visual hierarchy, maintain consistency, minimize cognitive load, make data accessible, and meet accessibility standards.[web:120]

Concrete tactics, cross-referenced from multiple current sources:

- **Put primary KPIs at the top**; park supporting/secondary stats below the fold — headline first, details on demand.[web:123]
- **Progressive disclosure**: show the summary/status first, let users drill down into trend and detail views rather than exposing everything at once.[web:123][web:120]
- **Group related metrics into clearly labeled sections** (e.g., "Campaign performance," "Data quality," "Publishing status") instead of one undifferentiated grid of cards.[web:123]
- **Filters**: place global filters together above the content, use short plain-language labels, and always visibly show what filters are currently applied — never a hidden state.[web:123][web:126]
- **Keep legends close to their charts** rather than in a distant sidebar, reducing the visual "search" cost for the user.[web:123]
- **Chart-type discipline**: prefer clear bar/line/area charts for trends and comparisons; avoid 3D charts (they distort perception) and avoid pie charts with many slices (they become unreadable past ~5–6 segments).[web:133]
- **Avoid textures/heavy decoration in fills** unless used purposefully for accessibility (e.g., patterns to substitute for color-only differentiation for colorblind users).[web:127][web:145]
- **Loading and performance UX**: prioritize above-the-fold content first, use progressive loading for large datasets, cache frequent queries, and design deliberate loading states rather than blank screens.[web:126]
- **Responsive behavior**: test and design explicitly for different viewport sizes rather than assuming desktop-only usage, especially since your Power BI custom layouts already support page-size and visual-position control.[web:133]

This maps directly onto your existing `DashboardCanvas.tsx`, `VisualLayoutEditor.tsx`, and `ReportEmbed.tsx` components — the layout/positioning logic you already have (page size, visual x/y/width/height) is the mechanical layer; the principles above are the *judgment* layer that should guide default layouts and templates.

---

## 4. Design-System Foundations to Formalize

A complete, current design system typically includes the following building blocks — use this as your checklist when formalizing `shared/ui`:[web:129][web:131]

| Layer | Contents | Notes |
|---|---|---|
| Color tokens | 1–2 brand colors, 5–7 neutral grays, semantic colors (success/warn/error/info), each with tonal scale (e.g., 12 shades) | Must pass contrast checks at every pairing used in real UI[web:129][web:131] |
| Typography tokens | 8-step type scale, defined line-heights, defined weights, responsive scaling rules | Semantic naming, not raw px values[web:125][web:129] |
| Spacing tokens | 4px/8px increment scale | Applied to margin, padding, gap uniformly[web:131] |
| Elevation/shadow tokens | 3–4 levels (flat, low, medium, high) | Used sparingly to indicate layering, not decoration |
| Radius tokens | 2–3 levels (sharp, soft, pill) | Consistent across buttons, cards, inputs |
| Iconography | Single consistent icon set, consistent stroke width | Minimize labels by pairing intuitive icons with tooltips rather than long text[web:133] |
| Motion tokens | Duration + easing pairs for hover/transition states | Subtle, purposeful — never distracting |

### Accessibility contrast requirements (hard constraints, not suggestions)
- Normal text: minimum **4.5:1** contrast ratio (WCAG AA); **7:1** for AAA.[web:134][web:135][web:142]
- Large text (≥18pt, or ≥14pt bold): minimum **3:1** (AA); **4.5:1** (AAA).[web:134][web:135][web:142]
- Non-text UI components and graphical objects (borders, icons, chart elements): minimum **3:1** against adjacent colors.[web:135][web:147]
- Never use color as the only differentiator for meaning — pair with icons, labels, or patterns so colorblind users aren't excluded.[web:145]
- Adjust brightness/value rather than hue when tuning a brand color for compliance, to preserve brand identity while meeting contrast targets.[web:145]

---

## 5. Recommended Frontend Tooling for Aesthetic + Minimal UI (React 19 / TS 7)

Given your stack, current (2026) React UI ecosystem options worth evaluating for your `shared/ui` design-system layer:[web:136][web:137]

- **Tailwind-first, accessible component libraries** (e.g., Untitled UI React, HeroUI, Tailgrids) — built on Tailwind CSS with React Aria for accessibility, and designed around token-based theming (colors, spacing, fonts) rather than hardcoded styles, which matches your design-token approach above.[web:136][web:137][web:140][web:141]
- Look specifically for libraries offering **robust theming via design tokens** rather than component-level style overrides — this keeps your minimalistic aesthetic centrally controlled and consistent across Dashboards, Data-Quality, and Power BI embed features.[web:136]
- For any AI-assisted component generation, prefer libraries with strong out-of-the-box accessibility foundations (React Aria, ARIA-compliant primitives) so minimalism doesn't come at the cost of usability for assistive technology.[web:137]

---

## 6. A Prompt Framework for AI-Assisted UI Generation (Secondary/Optional Tool)

Since you may occasionally use AI tools (e.g., for rapid mockups or component scaffolding) to iterate on aesthetics, use a **structured prompt framework** rather than ad-hoc requests — this is the single biggest factor separating polished AI-generated UI from generic output.[web:138][web:139][web:146]

### 6.1 The PROMPT framework
Structure every AI UI-generation prompt around: **P**latform, **R**ole/User, **O**utput specification, **M**ood/Style, **P**atterns/Components, **T**echnical constraints.[web:146]

### 6.2 Required elements in every prompt

1. **Name the screen precisely.** "Generate the Executive Overview dashboard page" — not "create a dashboard."[web:139]
2. **State platform and viewport.** Web app, target browser, desktop-first with defined breakpoints (e.g., 1440px, 1024px, 768px).[web:146][web:139]
3. **Describe the user and their moment.** "A data steward reviewing detected duplicate rows before approving a transformation" — context shapes correct information density.[web:146][web:138]
4. **List exact components and content, not vague categories.** Name actual elements: KPI cards, a data table with sortable columns, a duplicate-cluster review panel, filter chips, an approve/reject action bar — not "some widgets."[web:139][web:138]
5. **Name a specific design style instead of "clean and modern."** E.g., "minimal flat UI, generous whitespace, 1 accent color (#2F6FEB), neutral gray scale, no gradients, no drop shadows except on modals."[web:146]
6. **Specify grid, spacing, and hierarchy explicitly.** Column counts, max content width, primary KPI size vs. secondary stat size, F-pattern or Z-pattern scanning intent.[web:139][web:133]
7. **Define constraints — what NOT to do.** "No 3D charts, no pie charts with more than 5 segments, no decorative textures, no color-only status indicators."[web:146][web:133]
8. **Specify states.** Explicitly request loading, empty, and error variants — not just the "happy path" screen.[web:146]
9. **Request responsive behavior explicitly.** Describe how layout should adapt at each breakpoint rather than assuming the model infers it.[web:138][web:139]
10. **Iterate with real feedback loops.** Generate → screenshot → feed back with specific deltas ("increase spacing between KPI cards to 24px, reduce chart legend font size") rather than regenerating from scratch each time.[web:146]

### 6.3 Layered generation approach (for complex screens)
For non-trivial screens (e.g., the Data-Quality review dashboard), generate in explicit passes rather than one shot:[web:146]

1. **Layout pass** — structural wireframe only (grid, sections, placeholders).
2. **Theme pass** — apply color tokens, typography, spacing.
3. **Content pass** — fill with realistic sample data (real column names, real duplicate-cluster examples) instead of lorem ipsum.
4. **State pass** — generate loading/empty/error variants.
5. **Responsive pass** — adapt for tablet/mobile breakpoints.

### 6.4 Example ready-to-use prompt template

```
Screen: [exact screen name, e.g., "Duplicate Cluster Review Panel"]
Platform: Web app, React 19 + TypeScript, desktop-first (1440px), responsive down to 1024px and 768px
User & context: [role] reviewing [specific task] at [decision point]
Components required (exact list):
  - [Component 1 with purpose]
  - [Component 2 with purpose]
  - [Component 3 with purpose]
Visual style: Minimal flat UI, [N] accent color(s) [hex], neutral gray scale [N shades],
  8px spacing grid, [type scale reference], no gradients, no decorative shadows,
  WCAG AA contrast minimum on all text and interactive elements
Hierarchy: [Primary element] largest/top-left, [secondary elements] smaller/below,
  F-pattern / Z-pattern scanning intent
Constraints (do NOT):
  - No 3D charts
  - No pie charts with more than 5 segments
  - No color-only status indicators (pair with icon/label)
  - No dense unlabeled icon rows
States to include: default, loading, empty, error
Responsive behavior: [describe collapse/stack behavior per breakpoint]
```

---

## 7. Applying This to Your Existing Feature Slices

| Existing component | Aesthetic/minimalism actions |
|---|---|
| `DashboardCanvas.tsx` | Enforce 8px grid, limit visible KPI cards above the fold, apply progressive disclosure for drill-downs.[web:123][web:131] |
| `ReportEmbed.tsx` / Power BI custom layout | Default `customLayout.pageSize` and visual positions to match the F/Z-pattern hierarchy rather than arbitrary placement.[web:133] |
| `DuplicateReviewTable.tsx` (Data-Quality feature) | Use color sparingly for match-confidence indicators; always pair with a text/icon label, never color alone.[web:145][web:133] |
| `ChartSuggestionPanel.tsx` | Enforce the chart-type discipline rules (no 3D, no overcrowded pies) directly in the `VisualMappingRule` defaults so suggestions are aesthetically safe by design.[web:133] |
| `shared/ui` (Button, Modal, DataTable, LayoutGrid) | Formalize as design tokens (color/typography/spacing/radius/elevation) rather than per-component hardcoded styles, enabling one place to tune the whole product's aesthetic.[web:129][web:131] |

---

## 8. Quality Gates (add to existing test/CI taxonomy)

Extend your existing frontend testing categories with lightweight, automatable aesthetic/accessibility checks:

- **Contrast regression tests**: automated contrast-ratio checks (e.g., via axe-core or Storybook a11y addon) on every token pairing used in components, failing CI below 4.5:1 for text / 3:1 for UI graphics.[web:134][web:135]
- **Visual regression** (already planned via Playwright pixel-diff): extend to catch unintended whitespace/spacing drift, not just color/layout bugs.
- **Design-token lint rule**: fail CI if a component uses a raw hex/px value instead of a token — keeps minimalism enforced structurally, not just by convention.

---

## 9. Summary

Aesthetic, minimalistic UI for this platform is achieved by treating whitespace, a limited functional color palette, a clear typographic and spacing scale, and disciplined chart-type choices as **non-negotiable system constraints** — not stylistic preferences — enforced through design tokens, accessibility contrast gates, and consistent grid/hierarchy rules across every dashboard and data-quality screen.[web:120][web:129][web:130][web:131][web:133] Where AI tools are used to accelerate UI iteration, a structured prompt framework (platform, user context, exact components, named style, explicit constraints, defined states, responsive rules, layered generation) reliably produces production-quality results instead of generic output.[web:138][web:139][web:146] The rule-driven data-quality engine itself remains untouched by this: AI, if used at all, stays confined to optional UI ideation under the same guardrail discipline already established for the LLM Gateway, never as a dependency for correctness or runtime behavior.
