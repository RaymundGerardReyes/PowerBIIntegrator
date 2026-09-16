import React, { useState } from "react";
import { useLlmAssistantStore } from "../model/llmAssistantSlice";
import { runLlmTask } from "../api/llmApi";
import { ProviderSelector } from "./ProviderSelector";
import { SensitiveModeToggle } from "./SensitiveModeToggle";
import { GuardrailNotice } from "./GuardrailNotice";
import type { ChatMessage } from "../model/types";

export const AssistantPanel: React.FC = () => {
  const {
    isOpen,
    togglePanel,
    providerPreference,
    setProviderPreference,
    sensitiveMode,
    setSensitiveMode,
    activePolicyId,
    messages,
    addMessage
  } = useLlmAssistantStore();

  const [inputPrompt, setInputPrompt] = useState("");
  const [isLoading, setIsLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!inputPrompt.trim() || isLoading) return;

    const userMsg: ChatMessage = {
      id: crypto.randomUUID(),
      sender: "user",
      text: inputPrompt,
      timestamp: new Date().toISOString()
    };

    addMessage(userMsg);
    setInputPrompt("");
    setIsLoading(true);

    try {
      const response = await runLlmTask({
        taskType: "InteractiveChat",
        userPrompt: userMsg.text,
        contextIds: [],
        providerPreference,
        sensitivity: sensitiveMode ? "Sensitive" : "Internal",
        policyId: activePolicyId,
        correlationId: crypto.randomUUID()
      });

      const assistantMsg: ChatMessage = {
        id: crypto.randomUUID(),
        sender: "assistant",
        text: response.rawText,
        timestamp: new Date().toISOString(),
        providerUsed: response.providerUsed,
        guardrailNotice: response.guardrailNotice,
        isBlocked: response.isBlocked
      };

      addMessage(assistantMsg);
    } catch (err: unknown) {
      const errorMsg: ChatMessage = {
        id: crypto.randomUUID(),
        sender: "system",
        text: err instanceof Error ? err.message : "Error executing LLM task.",
        timestamp: new Date().toISOString(),
        isBlocked: true
      };
      addMessage(errorMsg);
    } finally {
      setIsLoading(false);
    }
  };

  if (!isOpen) {
    return (
      <button
        onClick={togglePanel}
        className="fixed bottom-4 right-4 bg-indigo-600 text-white px-4 py-2 rounded-full shadow-lg hover:bg-indigo-700 transition"
        data-testid="open-assistant-btn"
        aria-label="Open AI Analytics Assistant"
      >
        AI Assistant
      </button>
    );
  }

  return (
    <div
      className="fixed bottom-4 right-4 w-96 h-[520px] bg-white border border-gray-300 shadow-2xl rounded-lg flex flex-col z-50"
      data-testid="assistant-panel"
      role="complementary"
      aria-label="AI Analytics Assistant Panel"
    >
      <div className="flex justify-between items-center px-4 py-3 bg-indigo-700 text-white rounded-t-lg">
        <h3 className="font-semibold text-sm">Analytics Copilot (MCP)</h3>
        <button
          onClick={togglePanel}
          className="text-white hover:text-gray-200 text-lg font-bold"
          data-testid="close-assistant-btn"
          aria-label="Close Assistant Panel"
        >
          &times;
        </button>
      </div>

      <div className="p-2 border-b bg-gray-50 flex flex-col space-y-2">
        <ProviderSelector
          value={providerPreference}
          onChange={setProviderPreference}
          allowCloud={!sensitiveMode}
          isSensitiveMode={sensitiveMode}
        />
        <SensitiveModeToggle enabled={sensitiveMode} onToggle={setSensitiveMode} />
      </div>

      <div className="flex-1 p-3 overflow-y-auto space-y-3" data-testid="chat-history">
        {messages.length === 0 ? (
          <p className="text-gray-400 text-xs text-center mt-10">
            Ask the copilot to analyze measures, compile PBIR reports, or explain data anomalies.
          </p>
        ) : (
          messages.map((m) => (
            <div
              key={m.id}
              className={`flex flex-col ${m.sender === "user" ? "items-end" : "items-start"}`}
              data-testid={`message-${m.sender}`}
            >
              <div
                className={`max-w-[85%] px-3 py-2 rounded-lg text-sm ${
                  m.sender === "user"
                    ? "bg-indigo-600 text-white"
                    : m.sender === "system"
                    ? "bg-red-100 text-red-800"
                    : "bg-gray-100 text-gray-900 border"
                }`}
              >
                {m.text}
              </div>
              {m.guardrailNotice && <GuardrailNotice message={m.guardrailNotice} isBlocked={m.isBlocked} />}
              {m.providerUsed && (
                <span className="text-[10px] text-gray-400 mt-0.5">Executed on: {m.providerUsed}</span>
              )}
            </div>
          ))
        )}
        {isLoading && <div className="text-xs text-gray-500 italic">Copilot is thinking...</div>}
      </div>

      <form onSubmit={handleSubmit} className="p-2 border-t flex space-x-2">
        <input
          type="text"
          value={inputPrompt}
          onChange={(e) => setInputPrompt(e.target.value)}
          placeholder="Ask about your analytics model..."
          disabled={isLoading}
          className="flex-1 border rounded px-3 py-1.5 text-sm focus:outline-none focus:ring-1 focus:ring-indigo-500"
          data-testid="assistant-input"
          aria-label="Assistant question input"
        />
        <button
          type="submit"
          disabled={isLoading || !inputPrompt.trim()}
          className="bg-indigo-600 text-white px-3 py-1.5 rounded text-sm disabled:opacity-50 hover:bg-indigo-700"
          data-testid="assistant-send-btn"
        >
          Send
        </button>
      </form>
    </div>
  );
};
