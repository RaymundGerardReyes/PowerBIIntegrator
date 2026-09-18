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
        padding: "0.75rem 1rem",
        backgroundColor: "var(--bg-surface)",
        border: "1px solid var(--border-color)",
        borderRadius: "var(--radius-md)",
        marginBottom: "1rem",
        flexWrap: "wrap",
        gap: "1rem"
      }}
    >
      <div style={{ display: "flex", alignItems: "center", gap: "1.5rem", flexWrap: "wrap" }}>
        <div style={{ fontWeight: 600, fontSize: "0.95rem" }}>{title}</div>
        
        <div style={{ display: "flex", gap: "1rem", flexWrap: "wrap" }}>
          {metadata.map((item, i) => (
            <div key={i} style={{ display: "flex", alignItems: "center", gap: "0.35rem", fontSize: "0.85rem" }}>
              <span style={{ color: "var(--text-muted)" }}>{item.label}:</span>
              <span style={{ fontWeight: 500 }}>{item.value}</span>
            </div>
          ))}
        </div>
      </div>

      <div style={{ display: "flex", gap: "0.5rem" }}>
        {secondaryAction}
        {primaryAction}
      </div>
    </div>
  );
};

