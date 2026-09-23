import React, { useState } from "react";

interface ToolExecutionBadgeProps {
  toolName: string;
  status?: "running" | "completed" | "failed";
  latencyMs?: number;
  args?: Record<string, unknown> | string;
  result?: Record<string, unknown> | string;
}

export const ToolExecutionBadge: React.FC<ToolExecutionBadgeProps> = ({
  toolName,
  status = "running",
  latencyMs,
  args,
  result
}) => {
  const [isExpanded, setIsExpanded] = useState(false);

  const getStatusStyles = () => {
    switch (status) {
      case "completed":
        return {
          bg: "var(--success-bg, #ecfdf5)",
          border: "var(--success-border, #a7f3d0)",
          color: "var(--success, #059669)",
          dot: "#10b981",
          icon: "✓"
        };
      case "failed":
        return {
          bg: "var(--danger-bg, #fef2f2)",
          border: "var(--danger-border, #fecaca)",
          color: "var(--danger, #dc2626)",
          dot: "#ef4444",
          icon: "✕"
        };
      default:
        return {
          bg: "rgba(37, 99, 235, 0.08)",
          border: "rgba(37, 99, 235, 0.25)",
          color: "var(--primary, #2563eb)",
          dot: "#3b82f6",
          icon: "⚙"
        };
    }
  };

  const current = getStatusStyles();
  const hasDetails = Boolean(args || result || latencyMs !== undefined);

  return (
    <div style={{ margin: "4px 0", display: "flex", flexDirection: "column", width: "fit-content", maxWidth: "100%" }}>
      <div
        data-testid="tool-execution-badge"
        onClick={() => hasDetails && setIsExpanded((prev) => !prev)}
        role={hasDetails ? "button" : undefined}
        aria-expanded={hasDetails ? isExpanded : undefined}
        title={hasDetails ? "Click to toggle tool execution telemetry" : undefined}
        style={{
          display: "inline-flex",
          alignItems: "center",
          gap: "6px",
          padding: "3px 10px",
          borderRadius: "9999px",
          backgroundColor: current.bg,
          border: `1px solid ${current.border}`,
          color: current.color,
          fontSize: "11px",
          fontWeight: 500,
          cursor: hasDetails ? "pointer" : "default",
          boxShadow: "var(--shadow-xs, 0 1px 2px rgba(0,0,0,0.05))",
          userSelect: "none",
          transition: "all 0.15s ease"
        }}
      >
        <span
          style={{
            width: "6px",
            height: "6px",
            borderRadius: "50%",
            backgroundColor: current.dot
          }}
        />
        <span style={{ fontFamily: "monospace", fontSize: "11px", fontWeight: 600 }}>
          MCP Tool: {toolName}
        </span>
        {status === "running" && <span style={{ fontStyle: "italic", fontSize: "10px", opacity: 0.9 }}>(Executing...)</span>}
        {status === "completed" && <span style={{ fontSize: "10px", fontWeight: 700 }}>{current.icon}</span>}
        {status === "failed" && <span style={{ fontSize: "10px", fontWeight: 700 }}>{current.icon}</span>}
        {latencyMs !== undefined && (
          <span
            style={{
              fontSize: "10px",
              fontFamily: "monospace",
              padding: "1px 4px",
              backgroundColor: "rgba(0, 0, 0, 0.06)",
              borderRadius: "4px",
              marginLeft: "2px"
            }}
          >
            {latencyMs}ms
          </span>
        )}
        {hasDetails && (
          <span style={{ fontSize: "9px", opacity: 0.7, marginLeft: "2px" }}>
            {isExpanded ? "▲" : "▼"}
          </span>
        )}
      </div>

      {hasDetails && isExpanded && (
        <div
          data-testid="tool-execution-details"
          style={{
            marginTop: "4px",
            padding: "8px 10px",
            backgroundColor: "#0f172a",
            color: "#f8fafc",
            borderRadius: "6px",
            fontSize: "11px",
            fontFamily: "monospace",
            border: "1px solid #334155",
            maxWidth: "100%",
            overflowX: "auto"
          }}
        >
          {args && (
            <div style={{ marginBottom: "6px" }}>
              <span style={{ color: "#94a3b8", fontWeight: 700, fontSize: "10px", textTransform: "uppercase", display: "block" }}>
                Arguments:
              </span>
              <pre style={{ margin: "2px 0 0 0", color: "#34d399", whiteSpace: "pre-wrap", wordBreak: "break-all", fontSize: "10px" }}>
                {typeof args === "string" ? args : JSON.stringify(args, null, 2)}
              </pre>
            </div>
          )}
          {result && (
            <div style={{ borderTop: "1px solid #1e293b", paddingTop: "4px" }}>
              <span style={{ color: "#94a3b8", fontWeight: 700, fontSize: "10px", textTransform: "uppercase", display: "block" }}>
                Output:
              </span>
              <pre style={{ margin: "2px 0 0 0", color: "#38bdf8", whiteSpace: "pre-wrap", wordBreak: "break-all", fontSize: "10px" }}>
                {typeof result === "string" ? result : JSON.stringify(result, null, 2)}
              </pre>
            </div>
          )}
        </div>
      )}
    </div>
  );
};
