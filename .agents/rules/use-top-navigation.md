---
name: use-top-navigation
description: Enforce Top Navigation over Left Sidebar Navigation
trigger: always_on
---

# Constraint
The user explicitly prefers a Top Navigation (Top Navbar) application shell over a Left Sidebar (Aside) application shell.

## Rules
- When generating or modifying the `AppLayout` or global navigation shells, NEVER use a left `<aside>` sidebar.
- In addition to disallowing left sidebar navigation, NEVER use `<aside>` elements anywhere inside `AppLayout.tsx`.
- Slide-over drawers (such as the AI Advisory panel) must be structured as `<section aria-label="...">` or `<div role="region">`.
- Include automated component assertions verifying `document.querySelector("aside") === null` both when drawers are open and closed.
- ALWAYS place global navigation links (Dashboards, Data Sources, Data Quality, etc.) in a top `<header>` or `<nav>` bar that spans horizontally across the screen.
- Group the product identity (branding/logo), the primary workspace links, and the user/system controls (theme, logout, AI panel toggle) into a single unified top header bar.

## Example Top Navigation Structure
```tsx
<div style={{ minHeight: "100vh", display: "flex", flexDirection: "column" }}>
  <header style={{ display: "flex", justifyContent: "space-between", alignItems: "center", height: "56px" }}>
    <div className="brand">...</div>
    <nav className="workspace-links">...</nav>
    <div className="user-controls">...</div>
  </header>
  <main style={{ flex: 1 }}>...</main>
</div>
```

