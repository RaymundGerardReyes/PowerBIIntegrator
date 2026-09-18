import React, { useState } from "react";
import { Button, DataTable } from "@shared/ui";

export interface TransformationStepItem {
  order: number;
  operationType: "Filter" | "DeriveColumn" | "Rename" | "Aggregate" | "Join";
  targetColumn: string;
  expressionOrSource: string;
}

interface TransformationPlanBuilderProps {
  onPlanSubmit?: (steps: TransformationStepItem[]) => void;
}

export const TransformationPlanBuilder: React.FC<TransformationPlanBuilderProps> = ({ onPlanSubmit }) => {
  const [steps, setSteps] = useState<TransformationStepItem[]>([
    { order: 1, operationType: "Filter", targetColumn: "Status", expressionOrSource: "Status != 'CANCELLED'" },
    { order: 2, operationType: "DeriveColumn", targetColumn: "NetRevenue", expressionOrSource: "GrossRevenue - DiscountAmount" }
  ]);

  const columns = [
    { key: "order" as const, header: "Step" },
    { key: "operationType" as const, header: "Operation" },
    { key: "targetColumn" as const, header: "Target" },
    { key: "expressionOrSource" as const, header: "Expression / Source" },
    { key: "actions" as const, header: "Actions" }
  ];

  const rows = steps.map((step, idx) => ({
    order: `#${step.order}`,
    operationType: <span style={{ fontWeight: 600, color: "var(--primary)" }}>{step.operationType}</span>,
    targetColumn: step.targetColumn,
    expressionOrSource: <code style={{ backgroundColor: "var(--bg-card)", padding: "0.1rem 0.3rem", borderRadius: "var(--radius-sm)" }}>{step.expressionOrSource}</code>,
    actions: <button onClick={() => setSteps(steps.filter((_, i) => i !== idx))} style={{ background: "none", border: "none", cursor: "pointer", color: "var(--text-muted)" }}>✕</button>
  }));

  return (
    <div className="card" style={{ display: "flex", flexDirection: "column", gap: "1.5rem" }}>
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-end", borderBottom: "1px solid var(--border-color)", paddingBottom: "1rem" }}>
        <div>
          <h3 style={{ margin: 0, fontSize: "1rem" }}>Silver → Gold Transformation</h3>
          <p style={{ margin: "0.25rem 0 0 0", fontSize: "0.8125rem", color: "var(--text-secondary)" }}>
            Construct the deterministic transformation DAG for analytics-ready modeling.
          </p>
        </div>
        <Button variant="secondary" className="btn-sm" onClick={() => setSteps([...steps, { order: steps.length + 1, operationType: "DeriveColumn", targetColumn: "NewColumn", expressionOrSource: "" }])}>
          + Add Step
        </Button>
      </div>

      <div style={{ display: "grid", gridTemplateColumns: "repeat(3, 1fr)", gap: "1rem" }}>
        <div style={{ padding: "1rem", backgroundColor: "var(--bg-subtle)", borderRadius: "var(--radius-md)", border: "1px solid var(--border-color)", textAlign: "center" }}>
          <h4 style={{ margin: "0 0 0.5rem 0", fontSize: "0.875rem", color: "var(--text-secondary)", textTransform: "uppercase", letterSpacing: "0.05em" }}>BRONZE</h4>
          <p style={{ margin: 0, fontSize: "0.875rem", fontWeight: 600 }}>Raw Source</p>
          <p style={{ margin: 0, fontSize: "0.75rem", color: "var(--text-muted)" }}>124,320 Rows</p>
        </div>
        <div style={{ padding: "1rem", backgroundColor: "var(--bg-subtle)", borderRadius: "var(--radius-md)", border: "1px solid var(--border-color)", textAlign: "center" }}>
          <h4 style={{ margin: "0 0 0.5rem 0", fontSize: "0.875rem", color: "var(--text-secondary)", textTransform: "uppercase", letterSpacing: "0.05em" }}>SILVER</h4>
          <p style={{ margin: 0, fontSize: "0.875rem", fontWeight: 600 }}>Cleaned / Standardized</p>
          <p style={{ margin: 0, fontSize: "0.75rem", color: "var(--text-muted)" }}>122,104 Rows</p>
        </div>
        <div style={{ padding: "1rem", backgroundColor: "var(--primary-tint)", borderRadius: "var(--radius-md)", border: "1px solid var(--primary)", textAlign: "center" }}>
          <h4 style={{ margin: "0 0 0.5rem 0", fontSize: "0.875rem", color: "var(--primary)", textTransform: "uppercase", letterSpacing: "0.05em" }}>GOLD</h4>
          <p style={{ margin: 0, fontSize: "0.875rem", fontWeight: 600 }}>Analytics-Ready</p>
          <p style={{ margin: 0, fontSize: "0.75rem", color: "var(--text-secondary)" }}>Fact / Dimension Models</p>
        </div>
      </div>

      <div>
        <h4 style={{ margin: "0 0 0.75rem 0", fontSize: "0.875rem" }}>Transformation Steps</h4>
        <DataTable columns={columns} rows={rows} />
      </div>

      <div style={{ display: "flex", justifyContent: "flex-end", borderTop: "1px solid var(--border-color)", paddingTop: "1rem" }}>
        <Button variant="primary" onClick={() => onPlanSubmit?.(steps)}>
          Execute Transformation Plan
        </Button>
      </div>
    </div>
  );
};

