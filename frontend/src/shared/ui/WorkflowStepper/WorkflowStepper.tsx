import React from "react";

export interface WorkflowStep {
  id: string;
  label: string;
  status: "completed" | "active" | "pending" | "blocked" | "error";
  hasBadge?: boolean;
  badgeCount?: number;
}

interface WorkflowStepperProps {
  steps: WorkflowStep[];
  onStepClick: (stepId: string) => void;
}

export const WorkflowStepper: React.FC<WorkflowStepperProps> = ({ steps, onStepClick }) => {
  return (
    <div className="workflow-stepper" role="tablist">
      {steps.map((step) => {
        const isActive = step.status === "active";
        const isDone = step.status === "completed";
        const isError = step.status === "error";

        let stepClass = "workflow-step";
        if (isActive) stepClass += " step--active";
        else if (isDone) stepClass += " step--done";
        else if (isError) stepClass += " step--error";

        return (
          <button
            key={step.id}
            onClick={() => onStepClick(step.id)}
            aria-selected={isActive}
            aria-current={isActive ? "step" : undefined}
            className={stepClass}
            disabled={step.status === "blocked"}
            style={{ opacity: step.status === "blocked" ? 0.45 : 1 }}
          >
            {isDone && (
              <span style={{
                display: "inline-flex",
                alignItems: "center",
                justifyContent: "center",
                width: "14px",
                height: "14px",
                borderRadius: "50%",
                backgroundColor: "var(--success)",
                color: "#fff",
                fontSize: "0.55rem",
                fontWeight: 700,
                lineHeight: 1,
                flexShrink: 0
              }}>✓</span>
            )}
            {isError && (
              <span style={{
                display: "inline-flex",
                alignItems: "center",
                justifyContent: "center",
                width: "14px",
                height: "14px",
                borderRadius: "50%",
                backgroundColor: "var(--danger)",
                color: "#fff",
                fontSize: "0.55rem",
                fontWeight: 700,
                flexShrink: 0
              }}>✕</span>
            )}
            <span>{step.label}</span>
            {step.hasBadge && step.badgeCount !== undefined && (
              <span
                style={{
                  backgroundColor: isActive ? "var(--primary-tint)" : "var(--bg-subtle)",
                  color: isActive ? "var(--primary)" : "var(--text-muted)",
                  padding: "1px 6px",
                  borderRadius: "var(--radius-full)",
                  fontSize: "0.7rem",
                  fontWeight: 600,
                  border: isActive ? "1px solid var(--primary-glow)" : "1px solid var(--border-color)"
                }}
              >
                {step.badgeCount}
              </span>
            )}
          </button>
        );
      })}
    </div>
  );
};
