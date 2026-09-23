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
      case "CloudGemini":
        return {
          text: "✨ Gemini 3.1 Flash Live",
          color: "bg-blue-100 text-blue-800 border-blue-200"
        };
      case "CloudOpenAi":
        return {
          text: "⚡ OpenAI GPT-4o",
          color: "bg-emerald-100 text-emerald-800 border-emerald-200"
        };
      case "CloudAnthropic":
        return {
          text: "🧠 Claude 3.5 Sonnet",
          color: "bg-purple-100 text-purple-800 border-purple-200"
        };
      default:
        return {
          text: "🦙 Local Ollama (Zero Egress)",
          color: "bg-amber-100 text-amber-800 border-amber-200"
        };
    }
  };

  const badge = getProviderBadge();

  return (
    <div
      className="provider-selector bg-white/70 dark:bg-gray-800/70 backdrop-blur-md rounded-lg p-2.5 border border-gray-200/80 dark:border-gray-700/80 shadow-xs transition-all"
      data-testid="provider-selector"
    >
      <div className="flex items-center justify-between mb-1.5">
        <label htmlFor="llm-provider-select" className="text-xs font-semibold text-gray-700 dark:text-gray-200 flex items-center gap-1.5">
          <span>LLM Provider:</span>
        </label>
        <span className={`text-[10px] font-medium px-2 py-0.5 rounded-full border ${badge.color}`}>
          {badge.text}
        </span>
      </div>

      <select
        id="llm-provider-select"
        value={value}
        onChange={(e) => onChange(e.target.value as LlmProviderPreference)}
        className="w-full px-2.5 py-1.5 border border-gray-300 dark:border-gray-600 rounded-md text-xs bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-indigo-500/50 shadow-inner"
        aria-label="Select LLM provider"
      >
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
          className="text-xs text-amber-600 dark:text-amber-400 mt-1.5 flex items-center gap-1 bg-amber-50 dark:bg-amber-950/40 px-2 py-1 rounded border border-amber-200/60"
          data-testid="sensitive-lock-notice"
        >
          <span>🔒 Sensitive mode active: Cloud providers locked to Local Ollama.</span>
        </div>
      )}
    </div>
  );
};
