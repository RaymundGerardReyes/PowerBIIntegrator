# Invariant
All PowerBI Enhanced frontend components must follow the aesthetic minimalism design standards established in v1.19.0.

## Rules

### Design Tokens
- Radius scale MUST be: `--radius-xs:2px`, `--radius-sm:4px`, `--radius-md:8px`, `--radius-lg:12px`, `--radius-xl:16px`. Never deviate from this scale.
- NEVER use hardcoded pixel radius values in CSS (e.g., `border-radius: 6px`) — always use `var(--radius-*)` tokens.
- Shadow scale MUST have 5 distinct levels: xs, sm, md, lg, xl — each with different blur/spread values so depth is visually distinguishable.
- Add `--shadow-xl` for modals/popovers and `--shadow-primary` for primary button glow effects.
- Primary color is deep indigo-blue `#1a56db` (light mode) / `#4d7ef7` (dark mode). NEVER regress to `#2563eb`.
- The `--primary-glow` token MUST exist as `rgba(26, 86, 219, 0.18)` (light) for use in hover shadow effects.
- Header tokens `--header-bg`, `--header-blur`, `--header-border` MUST be present in both `:root` and `[data-theme="dark"]` for glassmorphism.

### Navigation Header
- Header MUST be `position: sticky; top: 0; z-index: 100` — never static.
- Header background MUST use `var(--header-bg)` with `backdrop-filter: var(--header-blur)` and `-webkit-backdrop-filter` for Safari.
- Nav items MUST use only `.nav-link` / `.nav-link--active` CSS classes — NO inline styles on nav links.
- Nav items MUST NOT contain emoji icons. Text labels only in `<nav>`.
- Active nav underline pip offset MUST be `bottom: -12px` (calibrated for 60px header height). Update if header height changes.
- Logo MUST use a CSS gradient: `linear-gradient(135deg, var(--primary) 0%, #6d28d9 100%)` — NOT a flat solid color.

### Buttons
- Primary buttons MUST use `box-shadow: var(--shadow-primary)` and `transform: translateY(-1px)` on hover.
- All `.btn` elements MUST use `border-radius: var(--radius-md)` (NOT `radius-sm`).
- `.btn-sm` uses `border-radius: var(--radius-sm)`.
- Grouped action clusters MUST use `.toolbar-group` with `overflow: hidden` and child `.btn` selectors overriding border/radius/shadow to produce a seamless grouped button strip.
- `.toolbar-divider` MUST be a `1px` wide line at `height: 20px` with `align-self: center`.

### Cards and Containers
- `.card` MUST use `border-radius: var(--radius-lg)` and `padding: var(--space-6)`.
- `.canvas-host-card` MUST use `border-radius: var(--radius-lg)`.
- Lifted context containers (ContextBar, workspace header, modal headers) MUST use `border-radius: var(--radius-lg)`.
- Hard-coded pixel values for `border-radius` in inline styles are prohibited — extract to CSS class or use a token.

### Forms
- `.form-input`, `.form-select`, `.form-textarea` MUST use `border-radius: var(--radius-md)`.
- `.form-label` color MUST be `var(--text-secondary)` (NOT `--text-primary` — labels should be lighter than field values).
- All form elements MUST have `appearance: none` and `font-family: inherit`.
- Hover state: `border-color: var(--border-strong)`.
- Focus state: `border-color: var(--primary)` + `box-shadow: 0 0 0 3px var(--primary-tint)`.

### Content Area Icons
- Emoji icons (`📊`, `📄`, `🗄️`, `📁`, etc.) in card grids and tab selectors MUST be replaced with inline SVGs.
- Each source/type card SVG icon MUST use a tinted background `<rect>` with `opacity="0.12"` and a semantic stroke color matching its category color.
- Acceptable semantic colors per category: Excel=`#16a34a`, CSV=`#6366f1`, SQL=`#0ea5e9`, Catalog=`#f59e0b`.

### WorkflowStepper
- MUST use `.workflow-stepper` wrapper and `.workflow-step` + `.step--active` / `.step--done` / `.step--error` CSS class modifiers.
- Active step: `color: var(--primary)`, `.step--active::before` dot glows with `box-shadow: 0 0 0 3px var(--primary-tint)`.
- Completed steps MUST show a filled circle `✓` badge (14×14px, `border-radius: 50%`, `background: var(--success)`).
- NEVER use character icon approach (`icon = "○"`, `icon = "●"`, `icon = "✓"` as text) — use the CSS `::before` dot or SVG badge.

### ContextBar
- MUST use `border-radius: var(--radius-lg)` and `box-shadow: var(--shadow-xs)`.
- Metadata items MUST use a `·` dot separator between label and value (not `:` colon).
- Title MUST use `fontWeight: 700` and `letterSpacing: -0.02em`.
- Background MUST be `var(--bg-card)` (NOT `--bg-surface`) so it appears slightly lifted.

### Typography
- `body` font stack MUST lead with `"Inter"`: `"Inter", -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif`.
- `body` MUST have `font-size: 0.9375rem`, `line-height: 1.6`, and `-webkit-font-smoothing: antialiased`.
- Heading `letter-spacing` MUST be `-0.025em` to give modern tight titling.
- `h1`=1.625rem, `h2`=1.25rem, `h3`=1.0625rem, `h4`=0.9375rem.

### Duplicate CSS Prevention
- When adding new CSS sections to `index.css`, always check for existing declarations of the same class first using `Select-String` before adding.
- Each CSS utility class (`.form-input`, `.toolbar-group`, `.canvas-viewport-outer`, etc.) MUST have exactly ONE declaration block in the file.
