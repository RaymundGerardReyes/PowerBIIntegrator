import React from "react";
import { Button } from "../Button/Button";

interface EmptyStateProps {
  title: string;
  description: string;
  primaryAction?: {
    label: string;
    onClick: () => void;
  };
  secondaryAction?: {
    label: string;
    onClick: () => void;
  };
}

export const EmptyState: React.FC<EmptyStateProps> = ({
  title,
  description,
  primaryAction,
  secondaryAction
}) => {
  return (
    <div
      className="card"
      style={{
        display: "flex",
        flexDirection: "column",
        alignItems: "flex-start",
        padding: "2rem",
        backgroundColor: "var(--bg-subtle)",
        border: "1px dashed var(--border-strong)"
      }}
    >
      <h3 style={{ margin: "0 0 0.5rem 0", fontSize: "1rem", color: "var(--text-primary)" }}>
        {title}
      </h3>
      <p style={{ margin: "0 0 1.5rem 0", fontSize: "0.875rem", color: "var(--text-secondary)", maxWidth: "600px" }}>
        {description}
      </p>
      
      {(primaryAction || secondaryAction) && (
        <div style={{ display: "flex", gap: "0.75rem", alignItems: "center" }}>
          {primaryAction && (
            <Button variant="primary" onClick={primaryAction.onClick}>
              {primaryAction.label}
            </Button>
          )}
          {secondaryAction && (
            <Button variant="secondary" onClick={secondaryAction.onClick}>
              {secondaryAction.label}
            </Button>
          )}
        </div>
      )}
    </div>
  );
};

