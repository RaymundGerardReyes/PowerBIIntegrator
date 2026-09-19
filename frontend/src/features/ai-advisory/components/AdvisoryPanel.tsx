import React, { useState } from "react";
import { useAdvisoryQuery } from "../hooks/useAdvisoryQuery";
import { ExposureUnlockDialog } from "./ExposureUnlockDialog";
import { Button } from "@shared/ui";

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
  const [customInput, setCustomInput] = useState("");

  const {
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
    { label: "Why did this column fail schema validation?", type: "Schema" }
  ];

  const observations = [
    { type: "warning", icon: "⚠", text: "Customer email contains 3.1% null values." },
    { type: "info", icon: "ℹ", text: "transaction_date contains mixed timezone formats." },
    { type: "success", icon: "✓", text: "No severe schema anomalies detected." },
    { type: "info", icon: "ℹ", text: "Column 'category' could be optimized as a categorical dimension." }
  ];

  return (
    <div className="card" style={{ display: "flex", flexDirection: "column", height: "100%", padding: 0 }}>
      {/* Header */}
      <div
        style={{
          padding: "1rem",
          borderBottom: "1px solid var(--border-color)",
          display: "flex",
          alignItems: "center",
          justifyContent: "space-between",
          backgroundColor: "var(--bg-subtle)"
        }}
      >
        <div>
          <div style={{ display: "flex", alignItems: "center", gap: "0.5rem" }}>
            <h3 style={{ margin: 0, fontSize: "1rem", fontWeight: 600 }}>AI Advisory Tier</h3>
            <span className="badge badge-info" style={{ fontSize: "0.7rem" }}>
              Read-Only Guardrailed
            </span>
          </div>
          <p style={{ margin: "0.25rem 0 0 0", fontSize: "0.75rem", color: "var(--text-secondary)" }}>
            {observations.length} automated observations from run {runId}
          </p>
        </div>

        <div style={{ display: "flex", alignItems: "center", gap: "0.5rem" }}>
          <button
            type="button"
            onClick={() => setIsUnlockModalOpen(true)}
            className="btn btn-secondary btn-sm"
          >
            <span>{isUnlockedConfidential ? "🔓" : "🔒"}</span>{" "}
            {isUnlockedConfidential ? "Unlocked" : "Unlock Sensitive Data"}
          </button>

          {onClose && (
            <button
              onClick={onClose}
              style={{ background: "none", border: "none", cursor: "pointer", fontSize: "1rem", color: "var(--text-muted)" }}
            >
              ✕
            </button>
          )}
        </div>
      </div>

      {/* Preset Questions & Custom Question Input */}
      <div style={{ padding: "1rem", borderBottom: "1px solid var(--border-color)", backgroundColor: "var(--bg-card)" }}>
        <div style={{ fontSize: "0.75rem", fontWeight: 600, color: "var(--text-secondary)", marginBottom: "0.5rem" }}>
          Grounded Governance Prompts
        </div>
        <div style={{ display: "flex", flexDirection: "column", gap: "0.35rem", marginBottom: "0.75rem" }}>
          {presetQuestions.map((q, idx) => (
            <button
              key={idx}
              className="btn btn-secondary btn-sm"
              style={{ textAlign: "left", fontSize: "0.75rem", padding: "0.3rem 0.5rem" }}
              onClick={() => askQuestion(q.label, q.type)}
              disabled={isLoading}
            >
              💬 {q.label}
            </button>
          ))}
        </div>

        <div style={{ display: "flex", gap: "0.5rem" }}>
          <input
            className="form-input"
            style={{ flex: 1, fontSize: "0.8rem", padding: "0.35rem 0.5rem" }}
            placeholder="Ask a question about pipeline transformations..."
            value={customInput}
            onChange={(e) => setCustomInput(e.target.value)}
            disabled={isLoading}
          />
          <Button
            className="btn-sm"
            onClick={() => {
              if (customInput.trim()) {
                askQuestion(customInput, "General");
                setCustomInput("");
              }
            }}
            disabled={isLoading || !customInput.trim()}
          >
            {isLoading ? "Querying..." : "Ask AI Advisor"}
          </Button>
        </div>

        {error && (
          <div style={{ marginTop: "0.5rem", color: "var(--danger)", fontSize: "0.75rem" }}>
            {error}
          </div>
        )}
      </div>

      {/* Narrative & Citations */}
      {result && (
        <div style={{ padding: "1rem", borderBottom: "1px solid var(--border-color)", backgroundColor: "rgba(16, 185, 129, 0.05)" }}>
          <h4 style={{ margin: "0 0 0.5rem 0", fontSize: "0.9rem", color: "var(--primary)" }}>
            AI Advisory Narrative
          </h4>
          <p style={{ margin: 0, fontSize: "0.85rem", lineHeight: 1.5 }}>
            {result.answer}
          </p>
          {result.citedRuleIds && result.citedRuleIds.length > 0 && (
            <div style={{ marginTop: "0.75rem", display: "flex", flexWrap: "wrap", gap: "0.35rem", alignItems: "center" }}>
              <span style={{ fontSize: "0.7rem", color: "var(--text-muted)" }}>Cited Rules:</span>
              {result.citedRuleIds.map((rule, idx) => (
                <span key={idx} className="badge badge-info" style={{ fontSize: "0.68rem" }}>
                  {rule}
                </span>
              ))}
            </div>
          )}
        </div>
      )}

      {/* Observations List */}
      <div style={{ padding: "1rem", flex: 1, display: "flex", flexDirection: "column", gap: "0.75rem", overflowY: "auto" }}>
        {observations.map((obs, idx) => (
          <div
            key={idx}
            style={{
              display: "flex",
              alignItems: "flex-start",
              gap: "0.75rem",
              padding: "0.75rem",
              border: "1px solid var(--border-color)",
              borderRadius: "var(--radius-md)",
              backgroundColor: obs.type === "warning" ? "var(--warning-bg)" : obs.type === "success" ? "var(--success-bg)" : "var(--info-bg)"
            }}
          >
            <span
              style={{
                fontSize: "1.25rem",
                lineHeight: 1,
                color: obs.type === "warning" ? "var(--warning)" : obs.type === "success" ? "var(--success)" : "var(--info)"
              }}
            >
              {obs.icon}
            </span>
            <div style={{ flex: 1 }}>
              <p style={{ margin: 0, fontSize: "0.875rem", fontWeight: 500 }}>{obs.text}</p>
              {obs.type === "warning" && (
                <div style={{ marginTop: "0.5rem" }}>
                  <Button variant="secondary" className="btn-sm">Review Records</Button>
                </div>
              )}
            </div>
          </div>
        ))}
      </div>

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
