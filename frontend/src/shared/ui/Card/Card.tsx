import React from "react";

export interface CardProps extends React.HTMLAttributes<HTMLDivElement> {
  hoverable?: boolean;
}

export const Card: React.FC<CardProps> = ({
  hoverable = false,
  className = "",
  style,
  children,
  ...rest
}) => {
  return (
    <div
      className={`card ${hoverable ? "card-hover" : ""} ${className}`}
      style={style}
      {...rest}
    >
      {children}
    </div>
  );
};

