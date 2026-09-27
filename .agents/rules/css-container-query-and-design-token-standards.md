
# Invariant
CSS `container-type` and `container-name` MUST be declared via CSS stylesheets (class rules), never as React inline style props. The `containerType` key is silently accepted by TypeScript but is not a valid `CSSProperties` key and does not produce any CSS output — causing `@container` rules to never fire.

## Rules

- **CSS Container Query Declaration**:
  - Visual card containers must declare `container-type: inline-size` and `container-name: visual-card` in `index.css` using the `.visual-card` CSS class:
    ```css
    .visual-card {
      container-type: inline-size;
      container-name: visual-card;
    }
    ```
  - The React element must carry `className="visual-card"` (NOT `style={{ containerType: "inline-size" }}`).
  - Violating this causes ALL `@container visual-card (...)` rules to never activate — size-aware tiers silently fail.

- **CSS Class Names for @container Descendants**:
  - All elements inside `.visual-card` that are targeted by `@container` rules must carry explicit CSS class names:
    - `.vc-title` — visual name span
    - `.vc-header` — drag-to-move header div
    - `.vc-field-select` — primary field selector (Slot 0)
    - `.vc-measure-select` — secondary measure selector (Slot 1)
    - `.vc-footer-field-pill` — bound field display pill in footer
    - `.vc-move-select` — move-to-page select
  - Never rely solely on inline `maxWidth` JS-computed from `layout.width` — the CSS `@container` rule overrides correctly by actual rendered container size.

- **@container Size Tier Rules in index.css**:
  - All 4 size tiers must be declared in `index.css`:
    - **Micro** (`max-width: 239px`): `flex-wrap: wrap`, 90px title, 80px selects
    - **Compact** (`240px ≤ width ≤ 379px`): `flex-wrap: nowrap`, 120px title, 105px selects
    - **Standard** (`380px ≤ width ≤ 599px`): 200px title, 130px selects
    - **Expanded** (`min-width: 600px`): 260px title, 160px selects

- **Test Assertion Pattern**:
  - `UI-TC-17` must verify `card.classList.contains("visual-card")`, NOT `card.style.containerType`:
    ```ts
    expect(card.classList.contains("visual-card")).toBe(true);
    ```
  - Asserting `card.style.containerType` will always pass as an empty string even when the class is correctly set, giving false confidence.

- **Design Token Compliance**:
  - All state-dependent UI elements (congestion banners, alerts, empty states) must use CSS design tokens (`var(--warning-bg)`, `var(--warning-border)`, `var(--warning)`), never hardcoded hex colors.
  - This ensures proper dark mode adaptation under `[data-theme="dark"]`.

- **Canvas Outer Viewport**:
  - The canvas editing area must use `.canvas-viewport-outer` CSS class with the dotted radial-gradient grid pattern:
    ```css
    background-image: radial-gradient(circle, var(--bg-canvas-dot) 1px, transparent 1px);
    background-size: 24px 24px;
    ```
  - `--bg-canvas-outer` and `--bg-canvas-dot` must be defined in both `:root` (light) and `[data-theme="dark"]`.

- **Navigation Link Pattern**:
  - All top-nav links must use `className="nav-link"` and `className="nav-link nav-link--active"` instead of inline styles.
  - The `nav-link--active::after` rule provides the underline pip indicator.
  - The `nav-link:focus-visible` rule provides the keyboard focus ring.

- **No maxWidth Restriction on main**:
  - The `<main>` element in `AppLayout` must NOT have `maxWidth` or `margin: 0 auto` — this squeezes the dashboard canvas when the AI assistant panel is open.
  - Use `width: 100%; min-width: 0` instead.
