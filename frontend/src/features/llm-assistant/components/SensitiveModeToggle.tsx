import React from "react";

interface SensitiveModeToggleProps {
  enabled: boolean;
  onToggle: (enabled: boolean) => void;
}

export const SensitiveModeToggle: React.FC<SensitiveModeToggleProps> = ({ enabled, onToggle }) => {
  return (
    <div
      className="sensitive-mode-toggle"
      data-testid="sensitive-mode-toggle"
      onClick={() => onToggle(!enabled)}
      style={{
        display: "flex",
        alignItems: "center",
        justifyContent: "space-between",
        padding: "0.5rem 0.75rem",
        borderRadius: "8px",
        backgroundColor: enabled ? "var(--warning-bg, #fffbeb)" : "var(--bg-surface, #ffffff)",
        border: `1px solid ${enabled ? "var(--warning-border, #fde68a)" : "var(--border-color, #e2e8f0)"}`,
        cursor: "pointer",
        userSelect: "none",
        transition: "all 0.15s ease",
        boxShadow: "var(--shadow-xs, 0 1px 2px rgba(0,0,0,0.04))"
      }}
    >
      <div style={{ display: "flex", alignItems: "center", gap: "0.5rem" }}>
        <span style={{ fontSize: "1rem", lineHeight: 1 }} aria-hidden="true">
          {enabled ? "🛡️" : "🌐"}
        </span>
        <div style={{ display: "flex", flexDirection: "column" }}>
          <label
            htmlFor="sensitive-mode-checkbox"
            style={{
              fontSize: "0.75rem",
              fontWeight: 600,
              color: enabled ? "var(--warning, #d97706)" : "var(--text-primary, #0f172a)",
              cursor: "pointer",
              lineHeight: 1.2
            }}
          >
            Sensitive / Camera Data Mode
          </label>
          <span
            style={{
              fontSize: "0.6875rem",
              color: enabled ? "var(--warning, #b45309)" : "var(--text-muted, #64748b)",
              lineHeight: 1.3,
              marginTop: "2px"
            }}
          >
            {enabled ? "Zero-Data-Leak Enforced (Local Ollama)" : "Cloud & Local models available"}
          </span>
        </div>
      </div>

      <div style={{ position: "relative", width: "36px", height: "20px" }}>
        <input
          type="checkbox"
          id="sensitive-mode-checkbox"
          checked={enabled}
          onChange={(e) => onToggle(e.target.checked)}
          style={{ opacity: 0, position: "absolute", width: "100%", height: "100%", cursor: "pointer", zIndex: 1 }}
          aria-label="Toggle Sensitive Data Mode"
        />
        <div
          style={{
            position: "absolute",
            top: 0,
            left: 0,
            right: 0,
            bottom: 0,
            backgroundColor: enabled ? "var(--warning, #d97706)" : "var(--border-strong, #cbd5e1)",
            borderRadius: "9999px",
            transition: "background-color 0.2s ease"
          }}
        >
          <div
            style={{
              position: "absolute",
              top: "2px",
              left: enabled ? "18px" : "2px",
              width: "16px",
              height: "16px",
              backgroundColor: "#ffffff",
              borderRadius: "50%",
              boxShadow: "0 1px 3px rgba(0,0,0,0.2)",
              transition: "left 0.2s ease"
            }}
          />
        </div>
      </div>
    </div>
  );
};
