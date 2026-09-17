import React from "react";
import { useAdvisoryQuery } from "../hooks/useAdvisoryQuery";
import { CitationBadge } from "./CitationBadge";
import { ExposureUnlockDialog } from "./ExposureUnlockDialog";
import { Button } from "@shared/ui/Button/Button";

interface AdvisoryPanelProps {
  runId?: string;
  userRole?: string;
  onClose?: () => void;
}

export const AdvisoryPanel: React.FC<AdvisoryPanelProps> = ({
  runId = "run-demo-001",
  userRole = "DataSteward",
  onClose
}) => {
  const {
    query,
    setQuery,
    result,
    isLoading,
    error,
    isUnlockedConfidential,
    isUnlockModalOpen,
    setIsUnlockModalOpen,
    askQuestion,
    handleUnlockConfirm
  } = useAdvisoryQuery(runId, userRole);

  const presetQuestions = [
    { label: "Why were these 42 rows treated as duplicates?", type: "Duplicates" },
    { label: "Why did this column fail schema validation?", type: "Schema" },
    { label: "What transformation plan would you suggest for merging tables?", type: "Transformation" },
    { label: "Is this chart suggestion appropriate given the data shape?", type: "Visual" }
  ];

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    askQuestion(query);
  };

  return (
    <div
      style={{
        display: "flex",
        flexDirection: "column",
        height: "100%",
        backgroundColor: "var(--bg-surface, #ffffff)",
        color: "var(--text-primary, #111827)",
        borderRadius: "12px",
        border: "1px solid var(--border-color, #e5e7eb)",
        boxShadow: "0 4px 6px -1px rgba(0, 0, 0, 0.05)",
        overflow: "hidden"
      }}
    >
      {/* Header */}
      <div
        style={{
          padding: "1rem 1.25rem",
          borderBottom: "1px solid var(--border-color, #e5e7eb)",
          display: "flex",
          alignItems: "center",
          justifyContent: "space-between",
          backgroundColor: "var(--bg-secondary, #f9fafb)"
        }}
      >
        <div style={{ display: "flex", alignItems: "center", gap: "0.5rem" }}>
          <span style={{ fontSize: "1.25rem" }}>💡</span>
          <div>
            <div style={{ display: "flex", alignItems: "center", gap: "0.5rem" }}>
              <h3 style={{ margin: 0, fontSize: "1rem", fontWeight: 700 }}>AI Advisory Tier</h3>
              <span
                style={{
                  fontSize: "0.65rem",
                  padding: "0.15rem 0.4rem",
                  borderRadius: "9999px",
                  backgroundColor: "rgba(16, 185, 129, 0.15)",
                  color: "#059669",
                  fontWeight: 700
                }}
              >
                Read-Only Guardrailed
              </span>
            </div>
            <p style={{ margin: 0, fontSize: "0.75rem", color: "var(--text-secondary, #6b7280)" }}>
              Run ID: <code>{runId}</code> • Grounded DQTE Explanations
            </p>
          </div>
        </div>

        <div style={{ display: "flex", alignItems: "center", gap: "0.5rem" }}>
          <button
            type="button"
            onClick={() => setIsUnlockModalOpen(true)}
            style={{
              display: "flex",
              alignItems: "center",
              gap: "0.35rem",
              padding: "0.3rem 0.6rem",
              borderRadius: "6px",
              fontSize: "0.75rem",
              fontWeight: 600,
              cursor: "pointer",
              backgroundColor: isUnlockedConfidential ? "rgba(16, 185, 129, 0.15)" : "rgba(245, 158, 11, 0.15)",
              color: isUnlockedConfidential ? "#059669" : "#d97706",
              border: `1px solid ${isUnlockedConfidential ? "rgba(16, 185, 129, 0.3)" : "rgba(245, 158, 11, 0.3)"}`
            }}
          >
            <span>{isUnlockedConfidential ? "🔓" : "🔒"}</span>
            <span>{isUnlockedConfidential ? "Sensitive Rows Unlocked" : "Unlock Sensitive Samples"}</span>
          </button>

          {onClose && (
            <button
              onClick={onClose}
              style={{
                background: "none",
                border: "none",
                cursor: "pointer",
                fontSize: "1.25rem",
                color: "var(--text-secondary, #6b7280)"
              }}
              aria-label="Close Advisor"
            >
              ✕
            </button>
          )}
        </div>
      </div>

      {/* Body Area */}
      <div style={{ flex: 1, overflowY: "auto", padding: "1.25rem", display: "flex", flexDirection: "column", gap: "1rem" }}>
        {/* Preset Queries */}
        <div>
          <p style={{ margin: "0 0 0.5rem 0", fontSize: "0.8rem", fontWeight: 600, color: "var(--text-secondary, #6b7280)" }}>
            Common Pipeline Explanations:
          </p>
          <div style={{ display: "flex", flexWrap: "wrap", gap: "0.5rem" }}>
            {presetQuestions.map((pq, idx) => (
              <button
                key={idx}
                type="button"
                onClick={() => {
                  setQuery(pq.label);
                  askQuestion(pq.label, pq.type);
                }}
                disabled={isLoading}
                style={{
                  padding: "0.4rem 0.75rem",
                  borderRadius: "20px",
                  fontSize: "0.8rem",
                  backgroundColor: "var(--bg-secondary, #f3f4f6)",
                  color: "var(--text-primary, #374151)",
                  border: "1px solid var(--border-color, #e5e7eb)",
                  cursor: "pointer",
                  textAlign: "left",
                  transition: "all 0.15s ease"
                }}
              >
                {pq.label}
              </button>
            ))}
          </div>
        </div>

        {/* Error Notification */}
        {error && (
          <div
            style={{
              padding: "0.75rem",
              borderRadius: "8px",
              backgroundColor: "rgba(239, 68, 68, 0.1)",
              color: "#dc2626",
              fontSize: "0.85rem",
              border: "1px solid rgba(239, 68, 68, 0.2)"
            }}
          >
            ⚠️ {error}
          </div>
        )}

        {/* Response View */}
        {result && (
          <div
            style={{
              padding: "1rem",
              borderRadius: "8px",
              backgroundColor: "var(--bg-secondary, #f9fafb)",
              border: "1px solid var(--border-color, #e5e7eb)"
            }}
          >
            <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", marginBottom: "0.75rem" }}>
              <span style={{ fontSize: "0.8rem", fontWeight: 700, color: "#2563eb" }}>
                AI Advisory Narrative
              </span>
              <span style={{ fontSize: "0.7rem", color: "var(--text-secondary, #6b7280)" }}>
                Provider: <strong>{result.providerUsed}</strong> {result.isUnlockedConfidential ? "(Local perimeter enforced)" : ""}
              </span>
            </div>

            <div style={{ whiteSpace: "pre-wrap", fontSize: "0.875rem", lineHeight: "1.6", marginBottom: "1rem" }}>
              {result.answer}
            </div>

            {/* Grounded Citations */}
            {result.citedRuleIds.length > 0 && (
              <div style={{ marginTop: "0.75rem", borderTop: "1px solid var(--border-color, #e5e7eb)", paddingTop: "0.75rem" }}>
                <p style={{ margin: "0 0 0.35rem 0", fontSize: "0.75rem", fontWeight: 700, color: "var(--text-secondary, #6b7280)" }}>
                  Verified Grounded Rule Citations:
                </p>
                <div style={{ display: "flex", flexWrap: "wrap" }}>
                  {result.citedRuleIds.map((rId) => (
                    <CitationBadge key={rId} id={rId} type="rule" />
                  ))}
                  {result.citedRunIds.map((runId) => (
                    <CitationBadge key={runId} id={runId} type="run" />
                  ))}
                </div>
              </div>
            )}

            {/* Redaction Guardrail Telemetry */}
            <div
              style={{
                marginTop: "0.5rem",
                display: "flex",
                alignItems: "center",
                gap: "1rem",
                fontSize: "0.7rem",
                color: "var(--text-secondary, #6b7280)"
              }}
            >
              <span>Redacted Fields: {result.redactedFieldsCount}</span>
              <span>Correlation ID: <code>{result.correlationId.slice(0, 8)}...</code></span>
            </div>
          </div>
        )}
      </div>

      {/* Input Area */}
      <form
        onSubmit={handleSubmit}
        style={{
          padding: "1rem 1.25rem",
          borderTop: "1px solid var(--border-color, #e5e7eb)",
          backgroundColor: "var(--bg-secondary, #f9fafb)",
          display: "flex",
          gap: "0.75rem"
        }}
      >
        <input
          type="text"
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          placeholder="Ask a question about pipeline transformations or duplicate rules..."
          disabled={isLoading}
          style={{
            flex: 1,
            padding: "0.6rem 0.85rem",
            borderRadius: "6px",
            border: "1px solid var(--border-color, #d1d5db)",
            fontSize: "0.875rem",
            backgroundColor: "var(--bg-surface, #ffffff)",
            color: "var(--text-primary, #111827)"
          }}
        />
        <Button type="submit" variant="primary" disabled={isLoading || !query.trim()}>
          {isLoading ? "Analyzing..." : "Ask AI Advisor"}
        </Button>
      </form>

      {/* Unlock Dialog */}
      <ExposureUnlockDialog
        isOpen={isUnlockModalOpen}
        runId={runId}
        userRole={userRole}
        onClose={() => setIsUnlockModalOpen(false)}
        onConfirmUnlock={handleUnlockConfirm}
      />
    </div>
  );
};

