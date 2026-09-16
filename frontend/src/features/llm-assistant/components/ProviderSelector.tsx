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

  return (
    <div className="provider-selector" data-testid="provider-selector">
      <label htmlFor="llm-provider-select" className="text-sm font-medium">
        LLM Provider:
      </label>
      <select
        id="llm-provider-select"
        value={value}
        onChange={(e) => onChange(e.target.value as LlmProviderPreference)}
        className="ml-2 px-2 py-1 border rounded text-sm bg-white"
        aria-label="Select LLM provider"
      >
        <option value="LocalOllama">Local Ollama (Zero-Leak Default)</option>
        <option value="CloudOpenAi" disabled={isCloudDisabled}>
          OpenAI Cloud {isCloudDisabled ? "(Disabled by Policy)" : ""}
        </option>
        <option value="CloudAnthropic" disabled={isCloudDisabled}>
          Anthropic Cloud {isCloudDisabled ? "(Disabled by Policy)" : ""}
        </option>
      </select>
      {isSensitiveMode && (
        <span className="text-xs text-amber-600 block mt-1" data-testid="sensitive-lock-notice">
          Sensitive mode active: Cloud providers locked to Local Ollama.
        </span>
      )}
    </div>
  );
};
