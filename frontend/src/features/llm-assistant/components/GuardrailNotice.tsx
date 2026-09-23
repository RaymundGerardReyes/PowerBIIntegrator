import React from "react";

interface GuardrailNoticeProps {
  message?: string;
  isBlocked?: boolean;
}

export const GuardrailNotice: React.FC<GuardrailNoticeProps> = ({ message, isBlocked }) => {
  if (!message) return null;

  const isDanger = Boolean(isBlocked);

  return (
    <div
      role="alert"
      aria-live="polite"
      data-testid="guardrail-notice"
      style={{
        display: "flex",
        alignItems: "flex-start",
        gap: "8px",
        padding: "8px 10px",
        margin: "6px 0",
        borderRadius: "6px",
        backgroundColor: isDanger ? "var(--danger-bg, #fef2f2)" : "var(--warning-bg, #fffbeb)",
        border: `1px solid ${isDanger ? "var(--danger-border, #fecaca)" : "var(--warning-border, #fde68a)"}`,
        color: isDanger ? "var(--danger, #dc2626)" : "var(--warning, #d97706)",
        fontSize: "12px",
        lineHeight: 1.4,
        boxShadow: "var(--shadow-xs, 0 1px 2px rgba(0,0,0,0.04))"
      }}
    >
      <span style={{ fontSize: "14px", lineHeight: 1, flexShrink: 0 }}>
        {isDanger ? "🛑" : "⚠️"}
      </span>
      <div style={{ display: "flex", flexDirection: "column", gap: "2px", minWidth: 0, flex: 1 }}>
        <div style={{ display: "flex", alignItems: "center", gap: "6px" }}>
          <span
            style={{
              fontSize: "10px",
              fontWeight: 700,
              fontFamily: "monospace",
              padding: "1px 5px",
              borderRadius: "4px",
              backgroundColor: isDanger ? "var(--danger, #dc2626)" : "var(--warning, #d97706)",
              color: "#ffffff"
            }}
          >
            {isDanger ? "[BLOCKED]" : "[GUARDRAIL NOTICE]"}
          </span>
          <span style={{ fontSize: "10px", color: "var(--text-muted, #64748b)", fontWeight: 500 }}>
            Policy Enforcement
          </span>
        </div>
        <p style={{ margin: "2px 0 0 0", color: "inherit", fontSize: "12px", lineHeight: 1.4 }}>
          {message}
        </p>
      </div>
    </div>
  );
};
