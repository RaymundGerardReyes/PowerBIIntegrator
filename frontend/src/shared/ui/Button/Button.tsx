import React from "react";

interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: "primary" | "secondary" | "danger";
}

export const Button: React.FC<ButtonProps> = ({ variant = "primary", className, ...rest }) => (
  <button className={`btn btn-${variant} ${className ?? ""}`} {...rest} />
);
