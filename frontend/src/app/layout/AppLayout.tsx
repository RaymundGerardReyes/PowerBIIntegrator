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
    { label: "Dashboards & PBIP", path: routePaths.dashboards },
    { label: "Data Sources", path: routePaths.dataSources },
    { label: "Executive Reports", path: routePaths.reports }
  ];

  return (
    <div style={{ minHeight: "100vh", display: "flex", flexDirection: "column", backgroundColor: "var(--bg-primary)" }}>
      {/* Enterprise Top Navigation Bar */}
      <header
        style={{
          position: "sticky",
          top: 0,
          zIndex: 50,
          backgroundColor: "var(--bg-surface)",
          borderBottom: "1px solid var(--border-color)",
          padding: "0 1.5rem",
          height: "64px",
          display: "flex",
          alignItems: "center",
          justifyContent: "space-between",
          boxShadow: "var(--shadow-sm)"
        }}
        role="banner"
      >
        <div style={{ display: "flex", alignItems: "center", gap: "2rem" }}>
          {/* Brand Logo */}
          <Link
            to={routePaths.dashboards}
            style={{
              display: "flex",
              alignItems: "center",
              gap: "0.5rem",
              textDecoration: "none",
              color: "var(--text-primary)",
              fontWeight: 700,
              fontSize: "1.125rem"
            }}
          >
            <div
              style={{
                width: "28px",
                height: "28px",
                borderRadius: "6px",
                backgroundColor: "#f59e0b",
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
                color: "#111827",
                fontWeight: 900,
                fontSize: "0.875rem"
              }}
            >
              PB
            </div>
            <span>PowerBI Enhanced</span>
            <span className="badge badge-info" style={{ fontSize: "0.65rem", textTransform: "uppercase" }}>
              2026 LTS
            </span>
          </Link>

          {/* Navigation Links */}
          <nav style={{ display: "flex", gap: "0.5rem" }} aria-label="Main Navigation">
            {navItems.map((item) => {
              const isActive = location.pathname.startsWith(item.path);
              return (
                <Link
                  key={item.path}
                  to={item.path}
                  style={{
                    padding: "0.5rem 0.875rem",
                    borderRadius: "var(--radius-sm)",
                    textDecoration: "none",
                    fontSize: "0.875rem",
                    fontWeight: isActive ? 600 : 500,
                    color: isActive ? "var(--primary)" : "var(--text-secondary)",
                    backgroundColor: isActive ? "rgba(37, 99, 235, 0.08)" : "transparent",
                    transition: "all 0.15s ease"
                  }}
                  aria-current={isActive ? "page" : undefined}
                >
                  {item.label}
                </Link>
              );
            })}
          </nav>
        </div>

        {/* Global Action Bar */}
        <div style={{ display: "flex", alignItems: "center", gap: "0.75rem" }}>
          {/* Theme Toggle */}
          <button
            onClick={toggleTheme}
            className="btn btn-secondary btn-sm"
            aria-label="toggle-theme-button"
            title={`Switch to ${theme === "light" ? "Dark" : "Light"} Mode`}
          >
            {theme === "light" ? "🌙 Dark" : "☀️ Light"}
          </button>

          {/* User Profile / Auth Status */}
          {isAuthenticated && user ? (
            <div style={{ display: "flex", alignItems: "center", gap: "0.5rem" }}>
              <span style={{ fontSize: "0.875rem", color: "var(--text-secondary)" }}>
                {user.displayName || user.email}
              </span>
              <button
                onClick={logout}
                className="btn btn-secondary btn-sm"
                aria-label="logout-button"
              >
                Sign out
              </button>
            </div>
          ) : (
            <Link to={routePaths.login} style={{ textDecoration: "none" }}>
              <button className="btn btn-secondary btn-sm">Sign in</button>
            </Link>
          )}

          {/* LLM Copilot Trigger Button */}
          <Button
            onClick={() => setIsAssistantOpen((prev) => !prev)}
            variant={isAssistantOpen ? "primary" : "secondary"}
            aria-label="toggle-ai-assistant"
            style={{ display: "flex", alignItems: "center", gap: "0.5rem" }}
          >
            <span style={{ fontSize: "1rem" }}>✨</span>
            <span>AI Copilot</span>
            <span
              style={{
                width: "8px",
                height: "8px",
                borderRadius: "50%",
                backgroundColor: isAssistantOpen ? "#10b981" : "var(--text-muted)",
                display: "inline-block"
              }}
            />
          </Button>
        </div>
      </header>

      {/* Main Body Viewport */}
      <div style={{ display: "flex", flex: 1, position: "relative", overflow: "hidden" }}>
        {/* Main Content */}
        <main
          style={{
            flex: 1,
            padding: "1.5rem",
            maxWidth: "1600px",
            margin: "0 auto",
            width: "100%",
            transition: "all 0.2s ease"
          }}
          role="main"
        >
          {children}
        </main>

        {/* Docked AI Assistant Drawer */}
        {isAssistantOpen && (
          <aside
            style={{
              width: "420px",
              borderLeft: "1px solid var(--border-color)",
              backgroundColor: "var(--bg-surface)",
              boxShadow: "var(--shadow-lg)",
              height: "calc(100vh - 64px)",
              position: "sticky",
              top: "64px",
              overflowY: "auto",
              zIndex: 40,
              display: "flex",
              flexDirection: "column"
            }}
            aria-label="AI Copilot Assistant Panel"
          >
            <div
              style={{
                padding: "0.75rem 1rem",
                borderBottom: "1px solid var(--border-color)",
                display: "flex",
                justifyContent: "space-between",
                alignItems: "center",
                backgroundColor: "var(--bg-card)"
              }}
            >
              <div style={{ display: "flex", alignItems: "center", gap: "0.5rem" }}>
                <span style={{ fontSize: "1.1rem" }}>✨</span>
                <strong style={{ fontSize: "0.95rem" }}>Model Context Protocol Agent</strong>
              </div>
              <button
                onClick={() => setIsAssistantOpen(false)}
                style={{ background: "none", border: "none", cursor: "pointer", fontSize: "1.25rem", color: "var(--text-secondary)" }}
                aria-label="close-assistant-panel"
              >
                ×
              </button>
            </div>
            <div style={{ padding: "1rem", flex: 1 }}>
              <AssistantPanel />
            </div>
          </aside>
        )}
      </div>
    </div>
  );
};
