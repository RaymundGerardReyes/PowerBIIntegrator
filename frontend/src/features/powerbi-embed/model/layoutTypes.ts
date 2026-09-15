export interface CustomPageSize {
  width: number;
  height: number;
}

export interface CustomVisualLayout {
  x: number;
  y: number;
  width: number;
  height: number;
  z?: number;
  displayState?: "Visible" | "Hidden";
}

export interface CustomLayoutConfig {
  pageSize: CustomPageSize;
  displayOption: "FitToPage" | "FitToWidth" | "ActualSize";
  visualsLayout: Record<string, CustomVisualLayout>;
}
