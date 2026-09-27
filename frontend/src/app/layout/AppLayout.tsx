import React, { useState } from "react";
import { Link, useLocation } from "react-router-dom";
import { routePaths } from "../routes/routePaths";
import { useTheme } from "../providers/ThemeProvider";
import { useAuth } from "@features/auth/hooks/useAuth";
import { AssistantPanel } from "@features/llm-assistant/components/AssistantPanel";
import { useLlmAssistantStore } from "@features/llm-assistant/model/llmAssistantSlice";
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
    { label: "Data Quality", path: routePaths.dataQuality },
    { label: "Executive Reports", path: routePaths.reports }
  ];

  return (
    <div style={{ minHeight: "100vh", display: "flex", flexDirection: "column", backgroundColor: "var(--bg-primary)" }}>
      {/* Unified Top Navigation Header — Glassmorphism */}
      <header
        style={{
          height: "60px",
          backgroundColor: "var(--header-bg)",
          backdropFilter: "var(--header-blur)",
          WebkitBackdropFilter: "var(--header-blur)",
          borderBottom: "1px solid var(--header-border)",
          display: "flex",
          alignItems: "center",
          justifyContent: "space-between",
          padding: "0 1.75rem",
          flexShrink: 0,
          position: "sticky",
          top: 0,
          zIndex: 100
        }}
      >
        <div style={{ display: "flex", alignItems: "center", gap: "2.25rem" }}>
          {/* Product Identity — Refined */}
          <Link
            to={routePaths.dashboards}
            style={{
              display: "flex",
              alignItems: "center",
              gap: "0.5rem",
              textDecoration: "none",
              color: "var(--text-primary)",
              fontWeight: 700,
              fontSize: "0.9375rem",
              letterSpacing: "-0.02em"
            }}
          >
            <div
              style={{
                width: "28px",
                height: "28px",
                borderRadius: "8px",
                background: "linear-gradient(135deg, var(--primary) 0%, #6d28d9 100%)",
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
                color: "#ffffff",
                fontWeight: 800,
                fontSize: "0.7rem",
                letterSpacing: "0.02em",
                boxShadow: "var(--shadow-primary)"
              }}
            >
              PB
            </div>
            <span style={{ color: "var(--text-primary)" }}>PowerBI</span>
            <span style={{ color: "var(--text-muted)", fontWeight: 400 }}>Enhanced</span>
          </Link>

          {/* Primary Modules */}
          <nav style={{ display: "flex", alignItems: "center", gap: "0.125rem" }}>
            {navItems.map((item) => {
              const isActive = location.pathname.startsWith(item.path);
              return (
                <Link
                  key={item.path}
                  to={item.path}
                  className={`nav-link${isActive ? " nav-link--active" : ""}`}
                >
                  {item.label}
                </Link>
              );
            })}
          </nav>
        </div>

        {/* User / System Controls */}
        <div style={{ display: "flex", alignItems: "center", gap: "0.5rem" }}>
          <button
            onClick={toggleTheme}
            className="btn btn-ghost btn-sm"
            aria-label="toggle-theme-button"
            title={theme === "light" ? "Switch to Dark mode" : "Switch to Light mode"}
            style={{ fontSize: "1rem", padding: "0.375rem 0.5rem" }}
          >
            {theme === "light" ? "🌙" : "☀️"}
          </button>

          {isAuthenticated && user ? (
            <div style={{ display: "flex", alignItems: "center", gap: "0.375rem" }}>
              <span style={{
                fontSize: "0.8125rem",
                color: "var(--text-muted)",
                maxWidth: "140px",
                overflow: "hidden",
                textOverflow: "ellipsis",
                whiteSpace: "nowrap"
              }}>
                {user.displayName || user.email}
              </span>
              <button onClick={logout} className="btn btn-ghost btn-sm">Sign out</button>
            </div>
          ) : (
            <Link to={routePaths.login} style={{ textDecoration: "none" }}>
              <button className="btn btn-secondary btn-sm">Sign in</button>
            </Link>
          )}

          <div style={{ width: "1px", height: "20px", backgroundColor: "var(--border-color)", margin: "0 0.125rem" }} />

          <Button
            onClick={() => {
              setIsAssistantOpen((prev) => {
                const next = !prev;
                useLlmAssistantStore.getState().setOpen(next);
                return next;
              });
            }}
            variant={isAssistantOpen ? "primary" : "secondary"}
            className="btn-sm"
            aria-label="toggle-ai-assistant"
            style={{ display: "flex", alignItems: "center", gap: "0.35rem", fontWeight: 500 }}
          >
            <span style={{ fontSize: "0.8rem" }}>✦</span> Copilot
          </Button>
        </div>
      </header>

      {/* Main Viewport */}
      <div style={{ display: "flex", flex: 1, overflow: "hidden" }}>
        <main
          style={{
            flex: 1,
            padding: "1.5rem 2rem",
            overflowY: "auto",
            width: "100%",
            minWidth: 0
          }}
        >
          {children}
        </main>

        {isAssistantOpen && (
          <section
            aria-label="AI Copilot Assistant Panel"
            style={{
              width: "400px",
              borderLeft: "1px solid var(--border-color)",
              backgroundColor: "var(--bg-card)",
              display: "flex",
              flexDirection: "column",
              flexShrink: 0
            }}
          >
            <div style={{
              padding: "0.875rem 1rem",
              borderBottom: "1px solid var(--border-color)",
              display: "flex",
              justifyContent: "space-between",
              alignItems: "center",
              backgroundColor: "var(--bg-card)"
            }}>
              <div style={{ fontSize: "0.875rem", fontWeight: 600, color: "var(--text-primary)" }}>
                ✦ AI Advisory Copilot
              </div>
              <button
                onClick={() => {
                  setIsAssistantOpen(false);
                  useLlmAssistantStore.getState().setOpen(false);
                }}
                aria-label="close-assistant-panel"
                style={{
                  background: "none",
                  border: "none",
                  cursor: "pointer",
                  color: "var(--text-muted)",
                  fontSize: "1.125rem",
                  lineHeight: 1,
                  padding: "0.25rem"
                }}
              >
                ×
              </button>
            </div>
            <div style={{ padding: 0, flex: 1, overflowY: "auto", display: "flex", flexDirection: "column" }}>
              <AssistantPanel />
            </div>
          </section>
        )}
      </div>
    </div>
  );
};
