---
name: canvas-layout-verification
description: >-
  Standardized workflow and guidelines for modernizing Power BI dashboard canvas layouts,
  enforcing CSS container queries, size-aware styling, fluid scaling, and executing
  the 40 UI automated verification test matrix.
---

# Canvas Layout Verification & Size-Aware Styling Standard

## Overview
This skill provides the architectural principles, container query patterns, display mode behaviors, and automated testing checklist for the PowerBI Enhanced canvas layout engine. It guarantees that dynamic changes to visuals, viewports, or metrics do not cause visual clipping, horizontal scroll blowout, or layout shifting.

## Core Architectural Invariants

### 1. Canvas Display Modes & Viewport Scaling
- The canvas viewport container must never force fixed horizontal overflow on screens smaller than the native canvas dimensions (1280px / 1920px).
- Supported modes:
  - **`FitToPage` (Default)**: Proportional uniform scaling via `Math.min(availWidth / canvasWidth, availHeight / canvasHeight)` with `transform: scale(...)` and `transform-origin: top left`.
  - **`FitToWidth`**: Width-matched scaling (`availWidth / canvasWidth`) with vertical document scroll flow.
  - **`ActualSize`**: 1:1 pixel rendering (`scale: 1.0`) with standard 2D scrollbars.
  - **Zoom Controls**: Step increments of `±0.1` clamped between `0.25x` and `2.0x`.

### 2. Size-Aware Styling via CSS Container Queries
- Visual card containers must declare:
  ```css
  container-type: inline-size;
  container-name: visual-card;
  ```
- Visual elements adapt to their own container dimensions rather than viewport media queries:
  - **Micro ($< 240$px)**: Icon-only headers, stacked field dropdowns, minimal padding (`0.4rem`).
  - **Compact ($240$–$380$px)**: Truncated titles (`max-width: 120px`), fluid KPI typography.
  - **Standard ($380$–$600$px)**: Standard side-by-side dropdown selectors, full chart axes.
  - **Expanded ($> 600$px)**: Multi-column distributions, wide bar tracks with data labels.

### 3. Fluid Container Scaling & Sizing Units
- Use `clamp()` combined with container query units (`cqi`):
  ```css
  font-size: clamp(1.25rem, 8cqi, 2.25rem);
  ```
- SVG visuals must specify `viewBox="0 0 W H"` and `preserveAspectRatio="xMidYMid meet"` with `width="100%"`.
- Tabular visuals must isolate scrolling within the visual card body (`overflow: auto`).

### 4. Dynamic Visual Type Switching Guardrails
- Switching visual types (`card` $\leftrightarrow$ `barChart` $\leftrightarrow$ `lineChart` $\leftrightarrow$ `donutChart` $\leftrightarrow$ `table`) must:
  - Preserve valid field bindings across compatible slot roles.
  - Dynamically reveal or collapse secondary metric selectors without displacing card coordinates.
  - Enforce minimum dimensions (`min-width: 180px`, `min-height: 120px`).
  - Clamp visual nudging coordinates within `canvasWidth` and `canvasHeight`.

---

## The 40 UI Automated Verification Test Standard

Every canvas change must pass the 40 test case matrix codified in `frontend/tests/unit/features/dashboards/CanvasLayoutResponsive.test.tsx`:

1. **Cases 1–8: Canvas Display Modes & Viewport Scaling**
   - `UI-TC-01`: FitToPage initial render mounts with scale transform.
   - `UI-TC-02`: Switch to FitToWidth mode updates mode button and layout.
   - `UI-TC-03`: Switch to ActualSize mode enables scrollable container.
   - `UI-TC-04`: Viewport resize event triggers dimension update.
   - `UI-TC-05`: Canvas Zoom In button increments zoom badge.
   - `UI-TC-06`: Canvas Zoom Out button decrements zoom badge.
   - `UI-TC-07`: Canvas Zoom Reset restores zoom level.
   - `UI-TC-08`: Canvas coexistence with side drawer / viewport constraints.
2. **Cases 9–16: Dynamic Visual Type Switching**
   - `UI-TC-09`: Switch card -> barChart replaces KPI metric with bar chart content.
   - `UI-TC-10`: Switch barChart -> lineChart replaces bars with continuous line SVG.
   - `UI-TC-11`: Switch lineChart -> donutChart renders donut SVG slices.
   - `UI-TC-12`: Switch donutChart -> table renders multi-column table data grid.
   - `UI-TC-13`: Switch table -> card renders KPI headline and formula pill.
   - `UI-TC-14`: Preserve field bindings across compatible multi-axis type switching.
   - `UI-TC-15`: Single-to-dual slot transition exposes secondary metric selector.
   - `UI-TC-16`: Dual-to-single slot transition collapses secondary metric selector.
3. **Cases 17–24: Size-Aware Styling & Container Queries**
   - `UI-TC-17`: Visual card root element declares `container-type: inline-size`.
   - `UI-TC-18`: Micro card layout (< 240px width) adapts header styling.
   - `UI-TC-19`: Compact card layout (240px - 380px width) sets constrained title width.
   - `UI-TC-20`: Standard card layout (380px - 600px width) sets standard title width.
   - `UI-TC-21`: Expanded card layout (> 600px width) expands layout dimensions.
   - `UI-TC-22`: Fluid KPI font scaling clamp in CardVisual.
   - `UI-TC-23`: SVG fluid viewBox scaling on Line and Donut charts.
   - `UI-TC-24`: Table visual scroll containment with `overflow: auto`.
4. **Cases 25–32: Measure & Dimension Role Validation**
   - `UI-TC-25`: Card visual raw column error state renders warning banner.
   - `UI-TC-26`: Bar chart metric raw column error state renders error banner.
   - `UI-TC-27`: Line chart metric raw column error state renders error banner.
   - `UI-TC-28`: Donut chart slice metric raw column error state renders error banner.
   - `UI-TC-29`: Category axis measure warning badge displays 'Dimension Expected'.
   - `UI-TC-30`: Dropdown optgroup partitions into DAX Measures and Dimensions.
   - `UI-TC-31`: Instant error recovery when selecting valid measure.
   - `UI-TC-32`: DAX validation pill displayed on valid measure binding.
5. **Cases 33–40: Layout Manipulation & Persistence**
   - `UI-TC-33`: Coordinate Nudge X action increments visual X coordinate by +10.
   - `UI-TC-34`: Canvas boundary clamping prevents visual from exceeding canvas width.
   - `UI-TC-35`: Canvas boundary clamping prevents visual from exceeding canvas height.
   - `UI-TC-36`: Minimum visual size constraints enforced (min 180w x 120h).
   - `UI-TC-37`: Active visual z-index elevation on click.
   - `UI-TC-38`: Multi-page canvas navigation loads active page visuals.
   - `UI-TC-39`: Visual empty state renders CTA when page has no visuals.
   - `UI-TC-40`: Layout persistence across store updates.

---

## Quick Verification Commands

```powershell
# Run the 40 UI automated test cases
npx vitest run tests/unit/features/dashboards/CanvasLayoutResponsive.test.tsx --prefix frontend

# Run all frontend tests
npm run test:all --prefix frontend

# Run Playwright E2E specs
npm run test:e2e --prefix frontend
```

