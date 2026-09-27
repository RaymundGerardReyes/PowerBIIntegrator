import React from "react";

interface ContextBarProps {
  title: string;
  metadata: { label: string; value: string | number | React.ReactNode }[];
  primaryAction?: React.ReactNode;
  secondaryAction?: React.ReactNode;
}

export const ContextBar: React.FC<ContextBarProps> = ({
  title,
  metadata,
  primaryAction,
  secondaryAction
}) => {
  return (
    <div
      style={{
        display: "flex",
        alignItems: "center",
        justifyContent: "space-between",
        padding: "0.75rem 1.25rem",
        backgroundColor: "var(--bg-card)",
        border: "1px solid var(--border-color)",
        borderRadius: "var(--radius-lg)",
        flexWrap: "wrap",
        gap: "0.75rem",
        boxShadow: "var(--shadow-xs)"
      }}
    >
      <div style={{ display: "flex", alignItems: "center", gap: "1.25rem", flexWrap: "wrap", minWidth: 0 }}>
        <div style={{
          fontWeight: 700,
          fontSize: "0.9375rem",
          color: "var(--text-primary)",
          letterSpacing: "-0.02em",
          whiteSpace: "nowrap"
        }}>
          {title}
        </div>

        <div style={{ display: "flex", gap: "0.875rem", flexWrap: "wrap", alignItems: "center" }}>
          {metadata.map((item, i) => (
            <div
              key={i}
              style={{
                display: "flex",
                alignItems: "center",
                gap: "0.3rem",
                fontSize: "0.8125rem"
              }}
            >
              <span style={{ color: "var(--text-muted)", fontWeight: 400 }}>{item.label}</span>
              <span style={{ color: "var(--border-strong)", userSelect: "none" }}>·</span>
              <span style={{ fontWeight: 600, color: "var(--text-secondary)" }}>{item.value}</span>
            </div>
          ))}
        </div>
      </div>

      <div style={{ display: "flex", gap: "0.5rem", alignItems: "center", flexShrink: 0 }}>
        {secondaryAction}
        {primaryAction}
      </div>
    </div>
  );
};
