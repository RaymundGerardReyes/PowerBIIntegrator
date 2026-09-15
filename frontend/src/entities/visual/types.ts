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
  visualType: string;
  layout: VisualLayout;
  boundFields: string[];
}
