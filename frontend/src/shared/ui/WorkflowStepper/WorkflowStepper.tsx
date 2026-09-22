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
    <div
      style={{
        display: "flex",
        alignItems: "center",
        borderBottom: "1px solid var(--border-color)",
        paddingBottom: "0.5rem",
        marginBottom: "1.5rem",
        gap: "1.5rem",
        overflowX: "auto"
      }}
      role="tablist"
    >
      {steps.map((step, index) => {
        const isActive = step.status === "active";
        
        let icon = "○"; // pending
        if (step.status === "completed") icon = "✓";
        if (step.status === "active") icon = "●";
        if (step.status === "error") icon = "✕";

        return (
          <button
            key={step.id}
            onClick={() => onStepClick(step.id)}
            aria-selected={isActive}
            aria-current={isActive ? "step" : undefined}
            style={{
              display: "flex",
              alignItems: "center",
              gap: "0.5rem",
              background: "none",
              border: "none",
              padding: "0.25rem 0",
              cursor: "pointer",
              fontSize: "0.875rem",
              fontWeight: isActive ? 600 : 500,
              color: isActive ? "var(--primary)" : "var(--text-secondary)",
              opacity: step.status === "blocked" ? 0.5 : 1,
              whiteSpace: "nowrap"
            }}
            disabled={step.status === "blocked"}
          >
            <span style={{ fontSize: "1rem", color: isActive ? "var(--primary)" : (step.status === "completed" ? "var(--success)" : "inherit") }}>
              {icon}
            </span>
            <span>{step.label}</span>
            {step.hasBadge && step.badgeCount !== undefined && (
              <span
                style={{
                  backgroundColor: "var(--bg-subtle)",
                  color: "var(--text-secondary)",
                  padding: "0.1rem 0.4rem",
                  borderRadius: "var(--radius-sm)",
                  fontSize: "0.7rem",
                  marginLeft: "0.25rem"
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

