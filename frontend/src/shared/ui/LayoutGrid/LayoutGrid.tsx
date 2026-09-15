import React from "react";

interface LayoutGridProps {
  width: number;
  height: number;
  children: React.ReactNode;
}

export const LayoutGrid: React.FC<LayoutGridProps> = ({ width, height, children }) => (
  <div style={{ position: "relative", width, height, border: "1px solid #ddd" }}>{children}</div>
);
