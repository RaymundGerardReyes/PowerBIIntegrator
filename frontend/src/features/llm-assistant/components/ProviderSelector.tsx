import React from "react";
import type { LlmProviderPreference } from "../model/types";

interface ProviderSelectorProps {
  value: LlmProviderPreference;
  onChange: (value: LlmProviderPreference) => void;
  allowCloud: boolean;
  isSensitiveMode: boolean;
}

export const ProviderSelector: React.FC<ProviderSelectorProps> = ({
  value,
  onChange,
  allowCloud,
  isSensitiveMode
}) => {
  const isCloudDisabled = !allowCloud || isSensitiveMode;

  const getProviderBadge = () => {
    switch (value) {
      case "AntigravityGemini":
        return {
          text: "✨ Antigravity Gemini (Embedded)",
          bg: "rgba(37, 99, 235, 0.12)",
          color: "var(--primary, #2563eb)",
          border: "rgba(37, 99, 235, 0.3)"
        };
      case "CloudGemini":
        return {
          text: "✨ Gemini 3.1 Flash Live",
          bg: "rgba(37, 99, 235, 0.12)",
          color: "var(--primary, #2563eb)",
          border: "rgba(37, 99, 235, 0.3)"
        };
      case "CloudOpenAi":
        return {
          text: "⚡ OpenAI GPT-4o",
          bg: "rgba(16, 185, 129, 0.12)",
          color: "var(--success, #059669)",
          border: "rgba(16, 185, 129, 0.3)"
        };
      case "CloudAnthropic":
        return {
          text: "🧠 Claude 3.5 Sonnet",
          bg: "rgba(147, 51, 234, 0.12)",
          color: "#9333ea",
          border: "rgba(147, 51, 234, 0.3)"
        };
      default:
        return {
          text: "🦙 Local Ollama (Zero Egress)",
          bg: "rgba(217, 119, 6, 0.12)",
          color: "var(--warning, #d97706)",
          border: "rgba(217, 119, 6, 0.3)"
        };
    }
  };

  const badge = getProviderBadge();

  return (
    <div
      className="provider-selector"
      data-testid="provider-selector"
      style={{
        backgroundColor: "var(--bg-surface, #ffffff)",
        border: "1px solid var(--border-color, #e2e8f0)",
        borderRadius: "8px",
        padding: "0.625rem 0.75rem",
        boxShadow: "var(--shadow-xs, 0 1px 2px rgba(0,0,0,0.04))"
      }}
    >
      <div
        style={{
          display: "flex",
          alignItems: "center",
          justifyContent: "space-between",
          marginBottom: "0.4rem"
        }}
      >
        <label
          htmlFor="llm-provider-select"
          style={{
            fontSize: "0.75rem",
            fontWeight: 600,
            color: "var(--text-primary, #0f172a)",
            display: "flex",
            alignItems: "center",
            gap: "0.35rem"
          }}
        >
          <span>LLM Provider:</span>
        </label>
        <span
          style={{
            fontSize: "0.6875rem",
            fontWeight: 600,
            padding: "0.15rem 0.5rem",
            borderRadius: "9999px",
            backgroundColor: badge.bg,
            color: badge.color,
            border: `1px solid ${badge.border}`,
            display: "inline-flex",
            alignItems: "center",
            gap: "0.25rem"
          }}
        >
          {badge.text}
        </span>
      </div>

      <select
        id="llm-provider-select"
        value={value}
        onChange={(e) => onChange(e.target.value as LlmProviderPreference)}
        aria-label="Select LLM provider"
        style={{
          width: "100%",
          padding: "0.4rem 0.6rem",
          borderRadius: "6px",
          border: "1px solid var(--border-color, #cbd5e1)",
          backgroundColor: "var(--bg-card, #ffffff)",
          color: "var(--text-primary, #0f172a)",
          fontSize: "0.8125rem",
          fontWeight: 500,
          outline: "none",
          cursor: "pointer"
        }}
      >
        <option value="AntigravityGemini">
          ✨ Antigravity Gemini (Embedded - Zero API Key)
        </option>
        <option value="LocalOllama">Local Ollama (Zero-Leak Default)</option>
        <option value="CloudGemini" disabled={isCloudDisabled}>
          Google Gemini 3.1 Flash Live {isCloudDisabled ? "(Disabled by Policy)" : ""}
        </option>
        <option value="CloudOpenAi" disabled={isCloudDisabled}>
          OpenAI Cloud {isCloudDisabled ? "(Disabled by Policy)" : ""}
        </option>
        <option value="CloudAnthropic" disabled={isCloudDisabled}>
          Anthropic Cloud {isCloudDisabled ? "(Disabled by Policy)" : ""}
        </option>
      </select>

      {isSensitiveMode && (
        <div
          data-testid="sensitive-lock-notice"
          style={{
            marginTop: "0.4rem",
            padding: "0.35rem 0.5rem",
            borderRadius: "6px",
            backgroundColor: "var(--warning-bg, #fffbeb)",
            border: "1px solid var(--warning-border, #fde68a)",
            color: "var(--warning, #d97706)",
            fontSize: "0.6875rem",
            fontWeight: 500,
            display: "flex",
            alignItems: "center",
            gap: "0.35rem"
          }}
        >
          <span>🔒 Sensitive mode active: Cloud providers locked to Local Ollama.</span>
        </div>
      )}
    </div>
  );
};
