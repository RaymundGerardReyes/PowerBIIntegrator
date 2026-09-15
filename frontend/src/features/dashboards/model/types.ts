import type { Visual } from "@entities/visual/types";

export interface Page {
  name: string;
  canvasWidth: number;
  canvasHeight: number;
  visuals: Visual[];
}

export interface DashboardDefinition {
  id: string;
  name: string;
  pages: Page[];
}
