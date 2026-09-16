import React, { useState } from "react";
import { Modal } from "@shared/ui/Modal/Modal";
import { Button } from "@shared/ui/Button/Button";
import { validateAnalyticsModel } from "../api/analyticsApi";
import type { ValidateAnalyticsModelResponseDto } from "@shared/types/api-contracts";

interface ModelValidationModalProps {
  open: boolean;
  onClose: () => void;
  modelId: string;
  modelName?: string;
}

export const ModelValidationModal: React.FC<ModelValidationModalProps> = ({
  open,
  onClose,
  modelId,
  modelName = "Semantic Model"
}) => {
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState<ValidateAnalyticsModelResponseDto | null>(null);
  const [error, setError] = useState<string | null>(null);

  const handleValidate = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await validateAnalyticsModel(modelId);
      setResult(data);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Failed to validate analytics model");
    } finally {
      setLoading(false);
    }
  };

  return (
    <Modal open={open} onClose={onClose}>
      <div style={{ display: "flex", flexDirection: "column", gap: "1rem" }}>
        <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
          <h3 style={{ margin: 0 }}>Validate Semantic Model</h3>
          <button
            onClick={onClose}
            style={{ background: "none", border: "none", cursor: "pointer", fontSize: "1.25rem" }}
            aria-label="close-modal"
          >
            ×
          </button>
        </div>

        <p style={{ margin: 0, color: "var(--text-secondary)" }}>
          Validates <strong>{modelName}</strong> against Power BI relational integrity, detecting cyclic dependencies,
          orphan tables, and invalid expressions.
        </p>

        <div style={{ display: "flex", gap: "0.5rem" }}>
          <Button onClick={handleValidate} disabled={loading || !modelId} aria-label="run-model-validation">
            {loading ? "Validating..." : "Run Validation"}
          </Button>
        </div>

        {error && (
          <div style={{ padding: "0.75rem", backgroundColor: "var(--danger-bg)", color: "var(--danger)", borderRadius: "var(--radius-sm)" }}>
            {error}
          </div>
        )}

        {result && (
          <div style={{ display: "flex", flexDirection: "column", gap: "0.75rem", marginTop: "0.5rem" }}>
            <div style={{ display: "flex", alignItems: "center", gap: "0.5rem" }}>
              <span className={`badge ${result.isValid ? "badge-success" : "badge-danger"}`}>
                {result.isValid ? "Valid & Deployable" : "Issues Detected"}
              </span>
              <span style={{ fontSize: "0.875rem", color: "var(--text-muted)" }}>
                Model: {result.modelName}
              </span>
            </div>

            {result.errors.length > 0 && (
              <div>
                <h5 style={{ color: "var(--danger)", margin: "0.25rem 0" }}>Errors ({result.errors.length})</h5>
                <ul style={{ margin: 0, paddingLeft: "1.25rem", color: "var(--danger)" }}>
                  {result.errors.map((err, idx) => (
                    <li key={idx} style={{ fontSize: "0.875rem" }}>{err}</li>
                  ))}
                </ul>
              </div>
            )}

            {result.detectedCycles.length > 0 && (
              <div>
                <h5 style={{ color: "var(--danger)", margin: "0.25rem 0" }}>Detected Cycles</h5>
                <ul style={{ margin: 0, paddingLeft: "1.25rem", color: "var(--danger)" }}>
                  {result.detectedCycles.map((c, idx) => (
                    <li key={idx} style={{ fontSize: "0.875rem" }}>{c}</li>
                  ))}
                </ul>
              </div>
            )}

            {result.orphanTables.length > 0 && (
              <div>
                <h5 style={{ color: "var(--warning)", margin: "0.25rem 0" }}>Orphan Tables ({result.orphanTables.length})</h5>
                <div style={{ display: "flex", flexWrap: "wrap", gap: "0.375rem" }}>
                  {result.orphanTables.map((t, idx) => (
                    <span key={idx} className="badge badge-warning">{t}</span>
                  ))}
                </div>
              </div>
            )}

            {result.warnings.length > 0 && (
              <div>
                <h5 style={{ color: "var(--warning)", margin: "0.25rem 0" }}>Warnings ({result.warnings.length})</h5>
                <ul style={{ margin: 0, paddingLeft: "1.25rem", color: "var(--text-secondary)" }}>
                  {result.warnings.map((w, idx) => (
                    <li key={idx} style={{ fontSize: "0.875rem" }}>{w}</li>
                  ))}
                </ul>
              </div>
            )}
          </div>
        )}
      </div>
    </Modal>
  );
};

