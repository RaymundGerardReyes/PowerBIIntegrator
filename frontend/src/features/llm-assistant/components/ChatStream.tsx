import React from "react";
import { ToolExecutionBadge } from "./ToolExecutionBadge";

interface ChatStreamProps {
  tokens: string;
  isStreaming: boolean;
  activeTool?: string | null;
  provider?: string;
}

export const ChatStream: React.FC<ChatStreamProps> = ({
  tokens,
  isStreaming,
  activeTool,
  provider = "Antigravity Gemini Live"
}) => {
  if (!tokens && !isStreaming && !activeTool) {
    return null;
  }

  return (
    <div
      data-testid="chat-stream-container"
      style={{
        display: "flex",
        flexDirection: "column",
        alignItems: "flex-start",
        margin: "8px 0",
        maxWidth: "92%"
      }}
    >
      {activeTool && (
        <div style={{ marginBottom: "6px" }}>
          <ToolExecutionBadge
            toolName={activeTool}
            status={isStreaming ? "running" : "completed"}
          />
        </div>
      )}

      <div
        style={{
          position: "relative",
          padding: "10px 14px",
          borderRadius: "12px 12px 12px 2px",
          fontSize: "13px",
          lineHeight: 1.5,
          backgroundColor: "var(--bg-surface, #ffffff)",
          color: "var(--text-primary, #0f172a)",
          border: "1px solid var(--border-color, #e2e8f0)",
          boxShadow: "var(--shadow-xs, 0 1px 2px rgba(0,0,0,0.05))",
          whiteSpace: "pre-wrap",
          wordBreak: "break-word"
        }}
      >
        {tokens}
        {isStreaming && (
          <span
            data-testid="stream-cursor"
            style={{
              display: "inline-block",
              width: "6px",
              height: "14px",
              marginLeft: "4px",
              backgroundColor: "var(--primary, #2563eb)",
              borderRadius: "1px",
              verticalAlign: "middle"
            }}
          />
        )}
      </div>

      {isStreaming && (
        <div
          style={{
            display: "flex",
            alignItems: "center",
            gap: "5px",
            marginTop: "4px",
            fontSize: "10px",
            color: "var(--primary, #2563eb)",
            fontWeight: 500
          }}
        >
          <span
            style={{
              width: "5px",
              height: "5px",
              borderRadius: "50%",
              backgroundColor: "var(--primary, #2563eb)"
            }}
          />
          <span>Streaming live from {provider}...</span>
        </div>
      )}
    </div>
  );
};
