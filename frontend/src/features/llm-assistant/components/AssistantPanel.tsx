import React, { useState, useRef, useEffect } from "react";
import { useLlmAssistantStore } from "../model/llmAssistantSlice";
import { useLlmStream } from "../hooks/useLlmStream";
import { ProviderSelector } from "./ProviderSelector";
import { SensitiveModeToggle } from "./SensitiveModeToggle";
import { GuardrailNotice } from "./GuardrailNotice";
import { ChatStream } from "./ChatStream";
import { ToolExecutionBadge } from "./ToolExecutionBadge";
import type { ChatMessage, ToolExecutionDetail } from "../model/types";
import "../styles/llm-assistant.css";

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
    activeToolDetail,
    guardrailWarning,
    startStream
  } = useLlmStream({
    onComplete: (fullText, toolDetails) => {
      const assistantMsg: ChatMessage = {
        id: crypto.randomUUID(),
        sender: "assistant",
        text: fullText,
        timestamp: new Date().toISOString(),
        providerUsed: providerPreference,
        guardrailNotice: guardrailWarning ?? undefined,
        toolDetails: toolDetails && toolDetails.length > 0 ? toolDetails : undefined
      };
      addMessage(assistantMsg);
      setIsSubmitting(false);
    },
    onError: (err) => {
      const errorMsg: ChatMessage = {
        id: crypto.randomUUID(),
        sender: "system",
        text: err.message || "Error streaming from Antigravity Gemini engine.",
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

    try {
      await startStream(trimmed, providerPreference, activePolicyId);
    } catch (err: unknown) {
      setIsSubmitting(false);
      const errorMsg: ChatMessage = {
        id: crypto.randomUUID(),
        sender: "system",
        text: err instanceof Error ? err.message : "Error executing Copilot reasoning.",
        timestamp: new Date().toISOString(),
        isBlocked: true
      };
      addMessage(errorMsg);
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
        data-testid="open-assistant-btn"
        aria-label="Open AI Analytics Assistant"
        style={{
          position: "fixed",
          bottom: "1.25rem",
          right: "1.25rem",
          display: "inline-flex",
          alignItems: "center",
          gap: "8px",
          padding: "10px 18px",
          borderRadius: "9999px",
          background: "linear-gradient(135deg, #2563eb 0%, #7c3aed 100%)",
          color: "#ffffff",
          border: "1px solid rgba(255, 255, 255, 0.25)",
          boxShadow: "0 8px 24px rgba(37, 99, 235, 0.35)",
          cursor: "pointer",
          zIndex: 50,
          fontWeight: 600,
          fontSize: "12px",
          transition: "all 0.2s cubic-bezier(0.4, 0, 0.2, 1)",
          backdropFilter: "blur(12px)"
        }}
      >
        <span
          style={{
            width: "8px",
            height: "8px",
            borderRadius: "50%",
            backgroundColor: "#10b981",
            boxShadow: "0 0 6px #10b981"
          }}
        />
        <span>✨ AI Copilot</span>
        {providerPreference === "CloudGemini" && (
          <span
            style={{
              fontSize: "10px",
              fontFamily: "monospace",
              backgroundColor: "rgba(255, 255, 255, 0.2)",
              padding: "2px 6px",
              borderRadius: "9999px"
            }}
          >
            Live
          </span>
        )}
      </button>
    );
  }

  const isDocked = dockMode === "docked";

  return (
    <section
      data-testid="assistant-panel"
      aria-label="AI Analytics Assistant Panel"
      className={`copilot-panel ${isDocked ? "copilot-panel-docked w-full" : "copilot-panel-floating fixed"}`}
      style={{
        display: "flex",
        flexDirection: "column",
        width: isDocked ? "100%" : "420px",
        height: isDocked ? "100%" : "640px",
        maxWidth: isDocked ? "100%" : "calc(100vw - 2.5rem)",
        maxHeight: isDocked ? "100%" : "calc(100vh - 2.5rem)",
        position: isDocked ? "relative" : "fixed",
        bottom: isDocked ? "auto" : "1.25rem",
        right: isDocked ? "auto" : "1.25rem",
        borderRadius: isDocked ? "0" : "12px",
        backgroundColor: "var(--bg-surface, #ffffff)",
        border: "1px solid var(--border-color, #e2e8f0)",
        boxShadow: "var(--shadow-lg, 0 10px 25px -5px rgba(0,0,0,0.1))",
        zIndex: 50,
        overflow: "hidden",
        boxSizing: "border-box"
      }}
    >
      {/* Header Toolbar */}
      <div
        style={{
          display: "flex",
          justifyContent: "space-between",
          alignItems: "center",
          padding: "10px 14px",
          background: "linear-gradient(135deg, #1e293b 0%, #0f172a 100%)",
          color: "#ffffff",
          borderBottom: "1px solid rgba(255, 255, 255, 0.1)",
          userSelect: "none",
          flexShrink: 0
        }}
      >
        <div style={{ display: "flex", alignItems: "center", gap: "8px" }}>
          <span style={{ fontSize: "16px", lineHeight: 1 }}>✨</span>
          <div style={{ display: "flex", flexDirection: "column" }}>
            <div style={{ display: "flex", alignItems: "center", gap: "6px" }}>
              <h3 style={{ fontSize: "12px", fontWeight: 700, margin: 0, color: "#ffffff", lineHeight: 1.2 }}>
                Analytics Copilot
              </h3>
              <span
                style={{
                  fontSize: "9px",
                  fontFamily: "monospace",
                  padding: "1px 5px",
                  borderRadius: "4px",
                  backgroundColor: "rgba(59, 130, 246, 0.3)",
                  border: "1px solid rgba(147, 197, 253, 0.3)",
                  color: "#93c5fd"
                }}
              >
                MCP Live
              </span>
            </div>
            <div style={{ display: "flex", alignItems: "center", gap: "5px", fontSize: "10px", color: "#94a3b8", marginTop: "2px" }}>
              <span
                style={{
                  width: "6px",
                  height: "6px",
                  borderRadius: "50%",
                  backgroundColor: isVoiceActive ? "#ec4899" : "#10b981",
                  boxShadow: isVoiceActive ? "0 0 6px #ec4899" : "0 0 4px #10b981"
                }}
              />
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

        <div style={{ display: "flex", alignItems: "center", gap: "6px" }}>
          {/* Voice Toggle Button */}
          <button
            onClick={toggleVoice}
            data-testid="toggle-voice-btn"
            title={isVoiceActive ? "Disable live voice streaming" : "Enable Gemini Live bidirectional voice streaming"}
            aria-label="Toggle Live Voice"
            style={{
              display: "inline-flex",
              alignItems: "center",
              gap: "4px",
              padding: "4px 8px",
              borderRadius: "6px",
              fontSize: "11px",
              fontWeight: 500,
              cursor: "pointer",
              transition: "all 0.15s ease",
              backgroundColor: isVoiceActive ? "#ec4899" : "rgba(255, 255, 255, 0.12)",
              color: "#ffffff",
              border: `1px solid ${isVoiceActive ? "#f472b6" : "rgba(255, 255, 255, 0.2)"}`,
              boxShadow: isVoiceActive ? "0 0 8px rgba(236, 72, 153, 0.5)" : "none"
            }}
          >
            <span>🎙️</span>
            <span>{isVoiceActive ? "Voice Live" : "Voice"}</span>
          </button>

          {/* Dock / Undock Toggle Button */}
          <button
            onClick={() => setDockMode(isDocked ? "floating" : "docked")}
            data-testid="dock-toggle-btn"
            title={isDocked ? "Undock to floating window" : "Dock to sidebar panel"}
            aria-label={isDocked ? "Undock to floating window" : "Dock to sidebar panel"}
            style={{
              padding: "4px 8px",
              borderRadius: "6px",
              fontSize: "11px",
              cursor: "pointer",
              backgroundColor: "rgba(255, 255, 255, 0.12)",
              color: "#ffffff",
              border: "1px solid rgba(255, 255, 255, 0.2)",
              lineHeight: 1
            }}
          >
            {isDocked ? "🗗" : "📌"}
          </button>

          {/* Close Panel Button */}
          <button
            onClick={togglePanel}
            data-testid="close-assistant-btn"
            aria-label="Close Assistant Panel"
            style={{
              padding: "3px 8px",
              borderRadius: "6px",
              fontSize: "14px",
              fontWeight: 700,
              cursor: "pointer",
              backgroundColor: "rgba(255, 255, 255, 0.12)",
              color: "#ffffff",
              border: "1px solid rgba(255, 255, 255, 0.2)",
              lineHeight: 1
            }}
          >
            &times;
          </button>
        </div>
      </div>

      {/* Voice Frequency Waveform Visualizer */}
      {isVoiceActive && (
        <div
          data-testid="voice-waveform"
          style={{
            display: "flex",
            alignItems: "center",
            justifyContent: "space-between",
            padding: "6px 12px",
            backgroundColor: "rgba(236, 72, 153, 0.08)",
            borderBottom: "1px solid rgba(236, 72, 153, 0.2)",
            color: "#db2777",
            fontSize: "11px",
            fontWeight: 500
          }}
        >
          <div style={{ display: "flex", alignItems: "center", gap: "6px" }}>
            <span>🔊</span>
            <span>Gemini Live Audio Active ({liveStatus})</span>
          </div>
          <div style={{ display: "flex", alignItems: "center", gap: "3px", height: "14px" }}>
            <span style={{ width: "3px", height: "10px", backgroundColor: "#ec4899", borderRadius: "2px" }} />
            <span style={{ width: "3px", height: "14px", backgroundColor: "#ec4899", borderRadius: "2px" }} />
            <span style={{ width: "3px", height: "8px", backgroundColor: "#ec4899", borderRadius: "2px" }} />
            <span style={{ width: "3px", height: "12px", backgroundColor: "#ec4899", borderRadius: "2px" }} />
            <span style={{ width: "3px", height: "6px", backgroundColor: "#ec4899", borderRadius: "2px" }} />
          </div>
        </div>
      )}

      {/* Controls Card: Provider & Sensitive Mode */}
      <div
        style={{
          padding: "10px 12px",
          backgroundColor: "var(--bg-subtle, #f8fafc)",
          borderBottom: "1px solid var(--border-color, #e2e8f0)",
          display: "flex",
          flexDirection: "column",
          gap: "8px",
          flexShrink: 0
        }}
      >
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
        data-testid="chat-history"
        style={{
          flex: 1,
          padding: "12px",
          overflowY: "auto",
          display: "flex",
          flexDirection: "column",
          gap: "10px",
          backgroundColor: "var(--bg-primary, #f8fafc)"
        }}
      >
        {messages.length === 0 ? (
          <div
            style={{
              display: "flex",
              flexDirection: "column",
              alignItems: "center",
              justifyContent: "center",
              textAlign: "center",
              padding: "16px 8px",
              margin: "auto 0"
            }}
          >
            <div
              style={{
                width: "42px",
                height: "42px",
                borderRadius: "50%",
                backgroundColor: "var(--primary-tint, rgba(37, 99, 235, 0.1))",
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
                fontSize: "20px",
                marginBottom: "8px"
              }}
            >
              ✨
            </div>
            <h4
              style={{
                fontSize: "13px",
                fontWeight: 700,
                color: "var(--text-primary, #0f172a)",
                margin: "0 0 4px 0"
              }}
            >
              Power BI Analytics Copilot
            </h4>
            <p
              style={{
                fontSize: "11px",
                color: "var(--text-muted, #64748b)",
                maxWidth: "280px",
                margin: "0 0 14px 0",
                lineHeight: 1.4
              }}
            >
              Ask about DAX measures, verify PBIR layout parity, or request optimal visual rearrangement.
            </p>

            {/* Quick Action Prompt Chips */}
            <div style={{ width: "100%", display: "flex", flexDirection: "column", gap: "6px" }} data-testid="quick-action-chips">
              <span
                style={{
                  fontSize: "10px",
                  fontWeight: 700,
                  textTransform: "uppercase",
                  color: "var(--text-muted, #64748b)",
                  letterSpacing: "0.04em",
                  textAlign: "left"
                }}
              >
                Suggested Prompts
              </span>
              <div style={{ display: "flex", flexWrap: "wrap", gap: "6px" }}>
                {quickActionChips.map((chip, idx) => (
                  <button
                    key={idx}
                    onClick={() => handleSendPrompt(chip.prompt)}
                    style={{
                      display: "inline-flex",
                      alignItems: "center",
                      gap: "6px",
                      padding: "6px 12px",
                      borderRadius: "20px",
                      backgroundColor: "var(--bg-surface, #ffffff)",
                      border: "1px solid var(--border-color, #e2e8f0)",
                      color: "var(--text-primary, #0f172a)",
                      fontSize: "11px",
                      fontWeight: 500,
                      cursor: "pointer",
                      textAlign: "left",
                      boxShadow: "var(--shadow-xs, 0 1px 2px rgba(0,0,0,0.04))",
                      transition: "all 0.15s ease"
                    }}
                  >
                    <span>💡</span>
                    <span>{chip.label}</span>
                  </button>
                ))}
              </div>
            </div>
          </div>
        ) : (
          messages.map((m) => (
            <div
              key={m.id}
              data-testid={`message-${m.sender}`}
              style={{
                display: "flex",
                flexDirection: "column",
                alignItems: m.sender === "user" ? "flex-end" : "flex-start",
                width: "100%"
              }}
            >
              <div
                style={{
                  maxWidth: "88%",
                  padding: "8px 12px",
                  borderRadius: m.sender === "user" ? "12px 12px 2px 12px" : "12px 12px 12px 2px",
                  backgroundColor:
                    m.sender === "user"
                      ? "var(--primary, #2563eb)"
                      : m.sender === "system"
                      ? "var(--danger-bg, #fef2f2)"
                      : "var(--bg-surface, #ffffff)",
                  color:
                    m.sender === "user"
                      ? "var(--primary-contrast, #ffffff)"
                      : m.sender === "system"
                      ? "var(--danger, #dc2626)"
                      : "var(--text-primary, #0f172a)",
                  border: m.sender === "user" ? "none" : "1px solid var(--border-color, #e2e8f0)",
                  fontSize: "12px",
                  lineHeight: 1.5,
                  boxShadow: "var(--shadow-xs, 0 1px 2px rgba(0,0,0,0.05))",
                  whiteSpace: "pre-wrap",
                  wordBreak: "break-word"
                }}
              >
                {m.text}
              </div>

              {m.toolDetails && m.toolDetails.length > 0 && (
                <div style={{ marginTop: "4px", width: "100%" }}>
                  {m.toolDetails.map((tool: ToolExecutionDetail, idx: number) => (
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
                <span
                  style={{
                    fontSize: "9px",
                    fontFamily: "monospace",
                    color: "var(--text-muted, #64748b)",
                    marginTop: "2px",
                    padding: "0 2px"
                  }}
                >
                  via {m.providerUsed}
                </span>
              )}
            </div>
          ))
        )}

        {/* Real-Time Chat Stream Rendering */}
        <ChatStream
          tokens={tokens}
          isStreaming={isStreaming}
          activeTool={activeTool}
          provider={providerPreference}
        />

        {isSubmitting && !isStreaming && (
          <div
            style={{
              display: "flex",
              alignItems: "center",
              gap: "6px",
              fontSize: "11px",
              color: "var(--text-muted, #64748b)",
              fontStyle: "italic",
              padding: "4px 0"
            }}
          >
            <span
              style={{
                width: "6px",
                height: "6px",
                borderRadius: "50%",
                backgroundColor: "var(--primary, #2563eb)"
              }}
            />
            <span>Copilot is reasoning with Antigravity Gemini...</span>
          </div>
        )}

        <div ref={chatBottomRef} />
      </div>

      {/* Input Bar */}
      <form
        onSubmit={handleSubmit}
        style={{
          display: "flex",
          alignItems: "center",
          gap: "8px",
          padding: "8px 12px",
          borderTop: "1px solid var(--border-color, #e2e8f0)",
          backgroundColor: "var(--bg-surface, #ffffff)",
          flexShrink: 0
        }}
      >
        <input
          type="text"
          value={inputPrompt}
          onChange={(e) => setInputPrompt(e.target.value)}
          placeholder="Ask Copilot about your Power BI model..."
          disabled={isSubmitting || isStreaming}
          data-testid="assistant-input"
          aria-label="Assistant question input"
          style={{
            flex: 1,
            padding: "8px 12px",
            borderRadius: "6px",
            border: "1px solid var(--border-color, #cbd5e1)",
            backgroundColor: "var(--bg-primary, #f8fafc)",
            color: "var(--text-primary, #0f172a)",
            fontSize: "12px",
            outline: "none"
          }}
        />

        {messages.length > 0 && (
          <button
            type="button"
            onClick={clearMessages}
            title="Clear conversation history"
            aria-label="Clear chat history"
            style={{
              padding: "6px 8px",
              borderRadius: "6px",
              border: "1px solid var(--border-color, #e2e8f0)",
              backgroundColor: "var(--bg-surface, #ffffff)",
              cursor: "pointer",
              fontSize: "12px",
              lineHeight: 1
            }}
          >
            🧹
          </button>
        )}

        <button
          type="submit"
          disabled={isSubmitting || isStreaming || !inputPrompt.trim()}
          data-testid="assistant-send-btn"
          style={{
            display: "inline-flex",
            alignItems: "center",
            gap: "4px",
            padding: "8px 14px",
            borderRadius: "6px",
            border: "none",
            backgroundColor: isSubmitting || isStreaming || !inputPrompt.trim() ? "var(--border-strong, #cbd5e1)" : "var(--primary, #2563eb)",
            color: "var(--primary-contrast, #ffffff)",
            fontSize: "12px",
            fontWeight: 600,
            cursor: isSubmitting || isStreaming || !inputPrompt.trim() ? "not-allowed" : "pointer",
            transition: "all 0.15s ease",
            lineHeight: 1
          }}
        >
          <span>Send</span>
          <span style={{ fontSize: "10px" }}>➤</span>
        </button>
      </form>
    </section>
  );
};
