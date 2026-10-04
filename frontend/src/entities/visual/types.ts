export type VisualType =
  | "barChart"
  | "columnChart"
  | "lineChart"
  | "areaChart"
  | "table"
  | "tableEx"
  | "matrix"
  | "card"
  | "donutChart"
  | "pieChart"
  | "slicer";

export const VISUAL_TYPES = {
  BarChart: "barChart",
  ColumnChart: "columnChart",
  LineChart: "lineChart",
  AreaChart: "areaChart",
  Table: "tableEx",
  Matrix: "matrix",
  Card: "card",
  DonutChart: "donutChart",
  PieChart: "pieChart",
  Slicer: "slicer",
} as const;

export interface VisualLayout {
  x: number;
  y: number;
  width: number;
  height: number;
  z?: number;
  visible: boolean;
}

export interface Visual {
  name: string;
  visualType: VisualType | string;
  layout: VisualLayout;
  boundFields: string[];
}
