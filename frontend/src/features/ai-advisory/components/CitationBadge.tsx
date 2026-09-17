import React from "react";

interface CitationBadgeProps {
  id: string;
  type?: "rule" | "run";
  onClick?: (id: string) => void;
}

export const CitationBadge: React.FC<CitationBadgeProps> = ({ id, type = "rule", onClick }) => {
  const isRule = type === "rule";
  const bgColor = isRule ? "rgba(59, 130, 246, 0.12)" : "rgba(16, 185, 129, 0.12)";
  const textColor = isRule ? "#2563eb" : "#059669";
  const borderColor = isRule ? "rgba(59, 130, 246, 0.3)" : "rgba(16, 185, 129, 0.3)";

  return (
    <span
      role="button"
      tabIndex={0}
      onClick={() => onClick?.(id)}
      onKeyDown={(e) => {
        if (e.key === "Enter" || e.key === " ") {
          onClick?.(id);
        }
      }}
      title={`Grounded Citation: ${id} (${isRule ? "Deterministic Rule" : "Pipeline Run"})`}
      style={{
        display: "inline-flex",
        alignItems: "center",
        gap: "0.25rem",
        padding: "0.2rem 0.5rem",
        borderRadius: "9999px",
        fontSize: "0.75rem",
        fontWeight: 600,
        backgroundColor: bgColor,
        color: textColor,
        border: `1px solid ${borderColor}`,
        cursor: onClick ? "pointer" : "default",
        userSelect: "none",
        transition: "all 0.15s ease",
        marginRight: "0.35rem",
        marginBottom: "0.35rem"
      }}
    >
      <span style={{ fontSize: "0.7rem", opacity: 0.8 }}>{isRule ? "⚖️" : "🚀"}</span>
      <span>{id}</span>
      <span style={{ fontSize: "0.65rem", opacity: 0.7, marginLeft: "2px" }}>✓ Grounded</span>
    </span>
  );
};

