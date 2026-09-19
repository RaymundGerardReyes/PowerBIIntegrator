import React, { useState } from "react";
import { Link, useLocation } from "react-router-dom";
import { routePaths } from "../routes/routePaths";
import { useTheme } from "../providers/ThemeProvider";
import { useAuth } from "@features/auth/hooks/useAuth";
import { AssistantPanel } from "@features/llm-assistant/components/AssistantPanel";
import { Button } from "@shared/ui/Button/Button";

interface AppLayoutProps {
  children: React.ReactNode;
}

export const AppLayout: React.FC<AppLayoutProps> = ({ children }) => {
  const location = useLocation();
  const { theme, toggleTheme } = useTheme();
  const { user, isAuthenticated, logout } = useAuth();
  const [isAssistantOpen, setIsAssistantOpen] = useState(false);

  const navItems = [
    { label: "Dashboards & PBIP", path: routePaths.dashboards, icon: "📊" },
    { label: "Data Sources", path: routePaths.dataSources, icon: "🔌" },
    { label: "Data Quality & Advisory", path: routePaths.dataQuality, icon: "✓" },
    { label: "Executive Reports", path: routePaths.reports, icon: "📈" }
  ];

  return (
    <div style={{ minHeight: "100vh", display: "flex", flexDirection: "column", backgroundColor: "var(--bg-primary)" }}>
      {/* Unified Top Navigation Header */}
      <header
        style={{
          height: "56px",
          backgroundColor: "var(--bg-surface)",
          borderBottom: "1px solid var(--border-color)",
          display: "flex",
          alignItems: "center",
          justifyContent: "space-between",
          padding: "0 1.5rem",
          flexShrink: 0
        }}
      >
        <div style={{ display: "flex", alignItems: "center", gap: "2rem" }}>
          {/* Product Identity */}
          <Link
            to={routePaths.dashboards}
            style={{
              display: "flex",
              alignItems: "center",
              gap: "0.5rem",
              textDecoration: "none",
              color: "var(--text-primary)",
              fontWeight: 600,
              fontSize: "0.95rem"
            }}
          >
            <div
              style={{
                width: "24px",
                height: "24px",
                borderRadius: "4px",
                backgroundColor: "#f59e0b",
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
                color: "#111827",
                fontWeight: 900,
                fontSize: "0.75rem"
              }}
            >
              PB
            </div>
            <span>PowerBI Enhanced</span>
          </Link>

          {/* Primary Modules */}
          <nav style={{ display: "flex", alignItems: "center", gap: "0.5rem" }}>
            {navItems.map((item) => {
              const isActive = location.pathname.startsWith(item.path);
              return (
                <Link
                  key={item.path}
                  to={item.path}
                  style={{
                    padding: "0.4rem 0.75rem",
                    borderRadius: "var(--radius-sm)",
                    textDecoration: "none",
                    fontSize: "0.85rem",
                    fontWeight: isActive ? 500 : 400,
                    color: isActive ? "var(--primary)" : "var(--text-secondary)",
                    backgroundColor: isActive ? "var(--primary-tint)" : "transparent",
                    display: "flex",
                    alignItems: "center",
                    gap: "0.4rem",
                    transition: "all 0.15s ease"
                  }}
                >
                  <span style={{ fontSize: "0.9rem" }}>{item.icon}</span>
                  {item.label}
                </Link>
              );
            })}
          </nav>
        </div>

        {/* User / System Controls */}
        <div style={{ display: "flex", alignItems: "center", gap: "0.75rem" }}>
          <button onClick={toggleTheme} className="btn btn-ghost btn-sm" aria-label="toggle-theme-button">
            {theme === "light" ? "🌙 Dark" : "☀️ Light"}
          </button>

          {isAuthenticated && user ? (
            <div style={{ display: "flex", alignItems: "center", gap: "0.5rem" }}>
              <span style={{ fontSize: "0.875rem", color: "var(--text-secondary)" }}>
                {user.displayName || user.email}
              </span>
              <button onClick={logout} className="btn btn-ghost btn-sm">Sign out</button>
            </div>
          ) : (
            <Link to={routePaths.login} style={{ textDecoration: "none" }}>
              <button className="btn btn-secondary btn-sm">Sign in</button>
            </Link>
          )}

          <Button
            onClick={() => setIsAssistantOpen((prev) => !prev)}
            variant={isAssistantOpen ? "primary" : "secondary"}
            className="btn-sm"
            aria-label="toggle-ai-assistant"
            style={{ display: "flex", alignItems: "center", gap: "0.25rem" }}
          >
            <span>✨</span> Advisory Copilot
          </Button>
        </div>
      </header>

      {/* Main Viewport */}
      <div style={{ display: "flex", flex: 1, overflow: "hidden" }}>
        <main
          style={{
            flex: 1,
            padding: "1.5rem",
            overflowY: "auto",
            maxWidth: "1400px",
            margin: "0 auto",
            width: "100%"
          }}
        >
          {children}
        </main>

        {isAssistantOpen && (
          <aside
            aria-label="AI Copilot Assistant Panel"
            style={{
              width: "400px",
              borderLeft: "1px solid var(--border-color)",
              backgroundColor: "var(--bg-surface)",
              display: "flex",
              flexDirection: "column",
              flexShrink: 0
            }}
          >
            <div style={{ padding: "0.75rem", borderBottom: "1px solid var(--border-color)", display: "flex", justifyContent: "space-between", alignItems: "center" }}>
              <div style={{ fontSize: "0.875rem", fontWeight: 600 }}>Model Context Protocol Agent</div>
              <button onClick={() => setIsAssistantOpen(false)} aria-label="close-assistant-panel" style={{ background: "none", border: "none", cursor: "pointer" }}>×</button>
            </div>
            <div style={{ padding: "1rem", flex: 1, overflowY: "auto" }}>
              <AssistantPanel />
            </div>
          </aside>
        )}
      </div>
    </div>
  );
};
