import React from "react";

export interface BadgeProps extends React.HTMLAttributes<HTMLSpanElement> {
  variant?: "info" | "success" | "warning" | "danger";
  icon?: React.ReactNode;
}

export const Badge: React.FC<BadgeProps> = ({
  variant = "info",
  icon,
  className = "",
  children,
  ...rest
}) => {
  return (
    <span className={`badge badge-${variant} ${className}`} {...rest}>
      {icon && <span style={{ display: "inline-flex", alignItems: "center" }}>{icon}</span>}
      <span>{children}</span>
    </span>
  );
};

