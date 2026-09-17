import React, { useState } from "react";
import { Button } from "@shared/ui/Button/Button";

interface ExposureUnlockDialogProps {
  isOpen: boolean;
  runId: string;
  userRole?: string;
  onClose: () => void;
  onConfirmUnlock: (reason: string) => Promise<void>;
}

export const ExposureUnlockDialog: React.FC<ExposureUnlockDialogProps> = ({
  isOpen,
  runId,
  userRole = "Analyst",
  onClose,
  onConfirmUnlock
}) => {
  const [reason, setReason] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  if (!isOpen) return null;

  const isDataSteward = userRole === "DataSteward" || userRole === "Admin";

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!reason.trim()) {
      setError("Please provide a business justification for unlocking sample rows.");
      return;
    }

    try {
      setIsSubmitting(true);
      setError(null);
      await onConfirmUnlock(reason.trim());
      onClose();
    } catch (err: any) {
      setError(err?.message || "Failed to authorize confidential sample row exposure.");
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div
      role="dialog"
      aria-modal="true"
      style={{
        position: "fixed",
        inset: 0,
        zIndex: 100,
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        backgroundColor: "rgba(0, 0, 0, 0.5)",
        backdropFilter: "blur(4px)"
      }}
    >
      <div
        style={{
          width: "100%",
          maxWidth: "520px",
          backgroundColor: "var(--bg-surface, #ffffff)",
          color: "var(--text-primary, #111827)",
          borderRadius: "12px",
          border: "1px solid var(--border-color, #e5e7eb)",
          boxShadow: "0 20px 25px -5px rgba(0, 0, 0, 0.1), 0 10px 10px -5px rgba(0, 0, 0, 0.04)",
          padding: "1.5rem"
        }}
      >
        <div style={{ display: "flex", alignItems: "center", gap: "0.75rem", marginBottom: "1rem" }}>
          <div
            style={{
              width: "40px",
              height: "40px",
              borderRadius: "8px",
              backgroundColor: "rgba(245, 158, 11, 0.15)",
              color: "#d97706",
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              fontSize: "1.25rem"
            }}
          >
            🛡️
          </div>
          <div>
            <h3 style={{ margin: 0, fontSize: "1.125rem", fontWeight: 700 }}>
              Elevated Sensitive Row Exposure Request
            </h3>
            <p style={{ margin: 0, fontSize: "0.8rem", color: "var(--text-secondary, #6b7280)" }}>
              Run ID: <code>{runId}</code>
            </p>
          </div>
        </div>

        {!isDataSteward ? (
          <div>
            <div
              style={{
                padding: "0.75rem",
                borderRadius: "8px",
                backgroundColor: "rgba(239, 68, 68, 0.1)",
                color: "#dc2626",
                fontSize: "0.875rem",
                marginBottom: "1.25rem",
                border: "1px solid rgba(239, 68, 68, 0.2)"
              }}
            >
              ⚠️ <strong>Insufficient Privilege:</strong> Your current role (<code>{userRole}</code>) does not hold
              permission to unlock confidential sample rows. Only <code>DataSteward</code> or <code>Admin</code> roles may authorize this action.
            </div>
            <div style={{ display: "flex", justifyContent: "flex-end" }}>
              <Button variant="secondary" onClick={onClose}>
                Close
              </Button>
            </div>
          </div>
        ) : (
          <form onSubmit={handleSubmit}>
            <div
              style={{
                padding: "0.75rem",
                borderRadius: "8px",
                backgroundColor: "rgba(59, 130, 246, 0.08)",
                fontSize: "0.85rem",
                lineHeight: "1.4",
                marginBottom: "1rem",
                border: "1px solid rgba(59, 130, 246, 0.2)"
              }}
            >
              <p style={{ margin: "0 0 0.5rem 0", fontWeight: 600 }}>Governed Least-Privilege Guardrails:</p>
              <ul style={{ margin: 0, paddingLeft: "1.25rem" }}>
                <li>Sample row values from Duplicate Clusters will be exposed for this query only.</li>
                <li><strong>Cloud execution is blocked:</strong> Provider is locked to <code>LocalOllama</code>.</li>
                <li><strong>Restricted PII is never exposed</strong> and remains redacted.</li>
                <li>This authorization is logged to the immutable audit trail.</li>
              </ul>
            </div>

            <label style={{ display: "block", fontSize: "0.875rem", fontWeight: 600, marginBottom: "0.35rem" }}>
              Business Justification <span style={{ color: "#dc2626" }}>*</span>
            </label>
            <textarea
              rows={3}
              value={reason}
              onChange={(e) => setReason(e.target.value)}
              placeholder="e.g., Verifying duplicate false-positive customer tuples for monthly close..."
              style={{
                width: "100%",
                padding: "0.5rem",
                borderRadius: "6px",
                border: "1px solid var(--border-color, #d1d5db)",
                backgroundColor: "var(--bg-primary, #ffffff)",
                color: "var(--text-primary, #111827)",
                fontSize: "0.875rem",
                marginBottom: error ? "0.5rem" : "1.25rem",
                boxSizing: "border-box"
              }}
            />

            {error && (
              <p style={{ color: "#dc2626", fontSize: "0.8rem", margin: "0 0 1rem 0" }}>
                {error}
              </p>
            )}

            <div style={{ display: "flex", justifyContent: "flex-end", gap: "0.75rem" }}>
              <Button type="button" variant="secondary" onClick={onClose} disabled={isSubmitting}>
                Cancel
              </Button>
              <Button type="submit" variant="primary" disabled={isSubmitting}>
                {isSubmitting ? "Authorizing..." : "Unlock & Force Local Execution"}
              </Button>
            </div>
          </form>
        )}
      </div>
    </div>
  );
};

