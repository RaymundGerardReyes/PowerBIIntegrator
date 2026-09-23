import React, { useState, useRef, useEffect } from "react";
import { useLlmAssistantStore } from "../model/llmAssistantSlice";
import { runLlmTask } from "../api/llmApi";
import { useLlmStream } from "../hooks/useLlmStream";
import { ProviderSelector } from "./ProviderSelector";
import { SensitiveModeToggle } from "./SensitiveModeToggle";
import { GuardrailNotice } from "./GuardrailNotice";
import { ChatStream } from "./ChatStream";
import { ToolExecutionBadge } from "./ToolExecutionBadge";
import type { ChatMessage } from "../model/types";

export const AssistantPanel: React.FC = () => {
  const {
    isOpen,
    togglePanel,
    dockMode,
    setDockMode,
    providerPreference,
    setProviderPreference,
    sensitiveMode,
    setSensitiveMode,
    activePolicyId,
    isVoiceActive,
    toggleVoice,
    liveStatus,
    messages,
    addMessage,
    clearMessages
  } = useLlmAssistantStore();

  const [inputPrompt, setInputPrompt] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const chatBottomRef = useRef<HTMLDivElement>(null);

  const {
    tokens,
    isStreaming,
    activeTool,
    guardrailWarning,
    startStream
  } = useLlmStream({
    onComplete: (fullText) => {
      const assistantMsg: ChatMessage = {
        id: crypto.randomUUID(),
        sender: "assistant",
        text: fullText,
        timestamp: new Date().toISOString(),
        providerUsed: providerPreference,
        guardrailNotice: guardrailWarning ?? undefined
      };
      addMessage(assistantMsg);
      setIsSubmitting(false);
    },
    onError: (err) => {
      const errorMsg: ChatMessage = {
        id: crypto.randomUUID(),
        sender: "system",
        text: err.message || "Error streaming from LLM provider.",
        timestamp: new Date().toISOString(),
        isBlocked: true
      };
      addMessage(errorMsg);
      setIsSubmitting(false);
    }
  });

  useEffect(() => {
    if (typeof chatBottomRef.current?.scrollIntoView === "function") {
      chatBottomRef.current.scrollIntoView({ behavior: "smooth" });
    }
  }, [messages, tokens, isStreaming]);

  const handleSendPrompt = async (promptToSend: string) => {
    const trimmed = promptToSend.trim();
    if (!trimmed || isSubmitting || isStreaming) return;

    const userMsg: ChatMessage = {
      id: crypto.randomUUID(),
      sender: "user",
      text: trimmed,
      timestamp: new Date().toISOString()
    };

    addMessage(userMsg);
    setInputPrompt("");
    setIsSubmitting(true);

    // If Gemini or streaming-compatible provider, run stream client
    if (providerPreference === "CloudGemini") {
      try {
        await startStream(trimmed, providerPreference, activePolicyId);
      } catch (err: unknown) {
        setIsSubmitting(false);
        const errorMsg: ChatMessage = {
          id: crypto.randomUUID(),
          sender: "system",
          text: err instanceof Error ? err.message : "Error establishing Gemini Live stream.",
          timestamp: new Date().toISOString(),
          isBlocked: true
        };
        addMessage(errorMsg);
      }
    } else {
      // Direct REST fallback
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
        setIsSubmitting(false);
      }
    }
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    handleSendPrompt(inputPrompt);
  };

  const quickActionChips = [
    { label: "Audit Semantic Measures", prompt: "Audit all declared measures in this semantic model for DAX syntax and parity." },
    { label: "Verify PBIR Parity", prompt: "Verify that visual.json bindings match the underlying table schema and measures." },
    { label: "Suggest Optimal Layout", prompt: "Suggest the best UX layout rearrangement for my visual cards to maximize readability." },
    { label: "Check Data Quality Rules", prompt: "Inspect the active dataset for null values, outlier distributions, and quality metrics." }
  ];

  if (!isOpen) {
    return (
      <button
        onClick={togglePanel}
        className="fixed bottom-4 right-4 bg-linear-to-r from-indigo-600 via-purple-600 to-pink-600 text-white px-4 py-2.5 rounded-full shadow-xl hover:shadow-indigo-500/30 hover:scale-105 active:scale-95 transition-all duration-200 flex items-center gap-2 z-50 font-medium text-xs border border-white/20 backdrop-blur-md"
        data-testid="open-assistant-btn"
        aria-label="Open AI Analytics Assistant"
      >
        <span className="w-2 h-2 rounded-full bg-emerald-400 animate-ping" />
        <span>✨ AI Copilot</span>
        {providerPreference === "CloudGemini" && (
          <span className="text-[10px] bg-white/20 px-1.5 py-0.5 rounded-full font-mono">Live</span>
        )}
      </button>
    );
  }

  const isDocked = dockMode === "docked";

  return (
    <section
      className={`assistant-panel flex flex-col z-50 bg-white/90 dark:bg-gray-900/90 backdrop-blur-xl border border-gray-200 dark:border-gray-800 shadow-2xl transition-all duration-300 ${
        isDocked
          ? "w-full h-full"
          : "fixed bottom-4 right-4 w-[420px] max-w-[calc(100vw-2rem)] h-[620px] max-h-[calc(100vh-2rem)] rounded-2xl overflow-hidden"
      }`}
      data-testid="assistant-panel"
      aria-label="AI Analytics Assistant Panel"
    >
      {/* Header */}
      <div className="flex justify-between items-center px-4 py-3 bg-linear-to-r from-indigo-700 via-indigo-800 to-purple-900 text-white shrink-0 select-none">
        <div className="flex items-center gap-2">
          <span className="text-base">✨</span>
          <div>
            <div className="flex items-center gap-1.5">
              <h3 className="font-semibold text-xs leading-none">Analytics Copilot</h3>
              <span className="text-[9px] bg-indigo-500/40 border border-indigo-400/30 px-1.5 py-0.5 rounded text-indigo-100 font-mono">
                MCP Live
              </span>
            </div>
            <div className="flex items-center gap-1 text-[10px] text-indigo-200 mt-0.5">
              <span className={`w-1.5 h-1.5 rounded-full ${isVoiceActive ? "bg-pink-400 animate-ping" : "bg-emerald-400"}`} />
              <span>
                {providerPreference === "CloudGemini"
                  ? "Gemini 3.1 Flash Live (Active)"
                  : providerPreference === "LocalOllama"
                  ? "Local Ollama (Zero-Leak)"
                  : providerPreference}
              </span>
            </div>
          </div>
        </div>

        <div className="flex items-center gap-1.5">
          {/* Voice Toggle */}
          <button
            onClick={toggleVoice}
            className={`px-2 py-1 rounded text-xs transition-all flex items-center gap-1 ${
              isVoiceActive
                ? "bg-pink-500 text-white shadow-xs shadow-pink-500/50 animate-pulse"
                : "bg-white/10 hover:bg-white/20 text-indigo-100"
            }`}
            title={isVoiceActive ? "Disable live voice streaming" : "Enable Gemini Live bidirectional voice streaming"}
            aria-label="Toggle Live Voice"
            data-testid="toggle-voice-btn"
          >
            <span>🎙️</span>
            <span className="text-[10px] font-medium hidden sm:inline">
              {isVoiceActive ? "Voice Live" : "Voice"}
            </span>
          </button>

          {/* Dock / Undock Toggle */}
          <button
            onClick={() => setDockMode(isDocked ? "floating" : "docked")}
            className="p-1 rounded bg-white/10 hover:bg-white/20 text-indigo-100 text-xs transition-colors"
            title={isDocked ? "Undock to floating window" : "Dock to sidebar panel"}
            aria-label={isDocked ? "Undock to floating window" : "Dock to sidebar panel"}
            data-testid="dock-toggle-btn"
          >
            {isDocked ? "🗗" : "📌"}
          </button>

          {/* Close / Minimize */}
          <button
            onClick={togglePanel}
            className="p-1 rounded bg-white/10 hover:bg-red-500/80 text-white text-xs transition-colors leading-none font-bold"
            data-testid="close-assistant-btn"
            aria-label="Close Assistant Panel"
          >
            &times;
          </button>
        </div>
      </div>

      {/* Voice Waveform Live Visualizer (when voice is enabled) */}
      {isVoiceActive && (
        <div
          data-testid="voice-waveform"
          className="bg-pink-950/40 border-b border-pink-500/30 px-3 py-1.5 flex items-center justify-between text-pink-300 text-xs animate-fade-in"
        >
          <div className="flex items-center gap-2">
            <span className="text-xs">🔊</span>
            <span className="text-[11px] font-medium">Gemini Live Audio Active ({liveStatus})</span>
          </div>
          <div className="flex items-center gap-1 h-3">
            <span className="w-1 bg-pink-400 rounded-full animate-bounce [animation-delay:-0.3s] h-3" />
            <span className="w-1 bg-pink-400 rounded-full animate-bounce [animation-delay:-0.15s] h-2" />
            <span className="w-1 bg-pink-400 rounded-full animate-bounce h-3.5" />
            <span className="w-1 bg-pink-400 rounded-full animate-bounce [animation-delay:-0.2s] h-1.5" />
            <span className="w-1 bg-pink-400 rounded-full animate-bounce [animation-delay:-0.05s] h-2.5" />
          </div>
        </div>
      )}

      {/* Policy & Provider Settings Controls */}
      <div className="p-2 border-b border-gray-200 dark:border-gray-800 bg-gray-50/80 dark:bg-gray-800/50 flex flex-col space-y-2 shrink-0">
        <ProviderSelector
          value={providerPreference}
          onChange={setProviderPreference}
          allowCloud={!sensitiveMode}
          isSensitiveMode={sensitiveMode}
        />
        <SensitiveModeToggle enabled={sensitiveMode} onToggle={setSensitiveMode} />
      </div>

      {/* Chat Messages History */}
      <div
        className="flex-1 p-3 overflow-y-auto space-y-3 min-h-0 bg-transparent"
        data-testid="chat-history"
      >
        {messages.length === 0 ? (
          <div className="flex flex-col items-center justify-center h-full text-center px-2 py-4">
            <div className="w-10 h-10 rounded-full bg-indigo-100 dark:bg-indigo-950/60 flex items-center justify-center text-indigo-600 dark:text-indigo-400 text-lg mb-2 shadow-inner">
              ✨
            </div>
            <h4 className="font-semibold text-xs text-gray-800 dark:text-gray-200 mb-1">
              Power BI Analytics Copilot
            </h4>
            <p className="text-gray-500 dark:text-gray-400 text-[11px] max-w-[280px] mb-3 leading-relaxed">
              Ask about DAX measures, verify PBIR layout parity, or request optimal visual rearrangement.
            </p>

            {/* Quick Action Chips */}
            <div className="w-full space-y-1.5" data-testid="quick-action-chips">
              <span className="text-[10px] uppercase font-bold text-gray-400 tracking-wider block text-left">
                Suggested Prompts
              </span>
              <div className="flex flex-wrap gap-1.5">
                {quickActionChips.map((chip, idx) => (
                  <button
                    key={idx}
                    onClick={() => handleSendPrompt(chip.prompt)}
                    className="text-left text-[11px] px-2.5 py-1.5 rounded-lg bg-gray-100 dark:bg-gray-800 hover:bg-indigo-50 dark:hover:bg-indigo-950/40 text-gray-700 dark:text-gray-300 hover:text-indigo-600 dark:hover:text-indigo-400 border border-gray-200 dark:border-gray-700 hover:border-indigo-300 transition-all shadow-2xs"
                  >
                    💡 {chip.label}
                  </button>
                ))}
              </div>
            </div>
          </div>
        ) : (
          messages.map((m) => (
            <div
              key={m.id}
              className={`flex flex-col ${m.sender === "user" ? "items-end" : "items-start"}`}
              data-testid={`message-${m.sender}`}
            >
              <div
                className={`max-w-[88%] px-3.5 py-2 rounded-xl text-xs leading-relaxed transition-all shadow-xs ${
                  m.sender === "user"
                    ? "bg-linear-to-r from-indigo-600 to-indigo-700 text-white rounded-br-xs"
                    : m.sender === "system"
                    ? "bg-red-50 text-red-900 border border-red-200 dark:bg-red-950/40 dark:text-red-300"
                    : "bg-gray-100 dark:bg-gray-800 text-gray-900 dark:text-gray-100 border border-gray-200 dark:border-gray-700 rounded-bl-xs"
                }`}
              >
                {m.text}
              </div>

              {m.toolDetails && m.toolDetails.length > 0 && (
                <div className="mt-1 space-y-1">
                  {m.toolDetails.map((tool, idx) => (
                    <ToolExecutionBadge
                      key={idx}
                      toolName={tool.toolName}
                      status={tool.status}
                      latencyMs={tool.latencyMs}
                      args={tool.args}
                      result={tool.result}
                    />
                  ))}
                </div>
              )}

              {m.guardrailNotice && (
                <GuardrailNotice message={m.guardrailNotice} isBlocked={m.isBlocked} />
              )}

              {m.providerUsed && (
                <span className="text-[9px] text-gray-400 dark:text-gray-500 mt-0.5 px-1 font-mono">
                  via {m.providerUsed}
                </span>
              )}
            </div>
          ))
        )}

        {/* Live Chat Stream Rendering */}
        <ChatStream
          tokens={tokens}
          isStreaming={isStreaming}
          activeTool={activeTool}
          provider={providerPreference}
        />

        {isSubmitting && !isStreaming && (
          <div className="flex items-center gap-1.5 text-xs text-gray-500 dark:text-gray-400 italic py-1">
            <span className="w-1.5 h-1.5 rounded-full bg-indigo-500 animate-ping" />
            <span>Copilot is reasoning with {providerPreference}...</span>
          </div>
        )}

        <div ref={chatBottomRef} />
      </div>

      {/* Input Bar */}
      <form
        onSubmit={handleSubmit}
        className="p-2.5 border-t border-gray-200 dark:border-gray-800 bg-white dark:bg-gray-900 shrink-0 flex items-center space-x-2"
      >
        <input
          type="text"
          value={inputPrompt}
          onChange={(e) => setInputPrompt(e.target.value)}
          placeholder="Ask Copilot about your Power BI model..."
          disabled={isSubmitting || isStreaming}
          className="flex-1 border border-gray-300 dark:border-gray-700 bg-gray-50 dark:bg-gray-800 text-gray-900 dark:text-gray-100 rounded-lg px-3 py-2 text-xs focus:outline-none focus:ring-2 focus:ring-indigo-500/50 shadow-inner"
          data-testid="assistant-input"
          aria-label="Assistant question input"
        />

        {messages.length > 0 && (
          <button
            type="button"
            onClick={clearMessages}
            className="p-2 text-gray-400 hover:text-gray-600 dark:hover:text-gray-200 text-xs rounded hover:bg-gray-100 dark:hover:bg-gray-800 transition"
            title="Clear conversation history"
            aria-label="Clear chat history"
          >
            🧹
          </button>
        )}

        <button
          type="submit"
          disabled={isSubmitting || isStreaming || !inputPrompt.trim()}
          className="bg-linear-to-r from-indigo-600 to-purple-600 text-white px-3.5 py-2 rounded-lg text-xs font-semibold disabled:opacity-50 hover:shadow-md hover:from-indigo-700 hover:to-purple-700 transition-all flex items-center gap-1"
          data-testid="assistant-send-btn"
        >
          <span>Send</span>
          <span>➤</span>
        </button>
      </form>
    </section>
  );
};
