import { create } from "zustand";
import type { DashboardDefinition, Page } from "./types";
import type { Visual, VisualLayout } from "@entities/visual/types";

export function computeAutoRearrange(
  visuals: Visual[],
  canvasWidth = 1280,
  canvasHeight = 720
): Visual[] {
  if (visuals.length === 0) return [];

  const kpis = visuals.filter((v) => v.visualType === "card");
  const charts = visuals.filter((v) => v.visualType !== "card");

  const margin = 20;
  const gap = 20;
  let currentY = margin;

  const rearranged: Visual[] = [];

  // 1. Position KPI Cards in top row(s)
  if (kpis.length > 0) {
    const kpiCount = kpis.length;
    const cardsPerRow = Math.min(kpiCount, 4);
    const kpiWidth = Math.floor((canvasWidth - 2 * margin - (cardsPerRow - 1) * gap) / cardsPerRow);
    const kpiHeight = 150;

    kpis.forEach((kpi, index) => {
      const col = index % cardsPerRow;
      const row = Math.floor(index / cardsPerRow);
      const x = margin + col * (kpiWidth + gap);
      const y = currentY + row * (kpiHeight + gap);
      rearranged.push({
        ...kpi,
        layout: {
          ...kpi.layout,
          x,
          y,
          width: kpiWidth,
          height: kpiHeight
        }
      });
    });

    const kpiRows = Math.ceil(kpiCount / cardsPerRow);
    currentY += kpiRows * (kpiHeight + gap);
  }

  // 2. Position Charts in subsequent balanced grid
  if (charts.length > 0) {
    const availableHeight = Math.max(260, canvasHeight - currentY - margin);

    if (charts.length === 1) {
      rearranged.push({
        ...charts[0],
        layout: {
          ...charts[0].layout,
          x: margin,
          y: currentY,
          width: canvasWidth - 2 * margin,
          height: Math.min(availableHeight, 460)
        }
      });
    } else if (charts.length === 2) {
      const chartWidth = Math.floor((canvasWidth - 2 * margin - gap) / 2);
      const chartHeight = Math.min(availableHeight, 460);
      charts.forEach((chart, index) => {
        rearranged.push({
          ...chart,
          layout: {
            ...chart.layout,
            x: margin + index * (chartWidth + gap),
            y: currentY,
            width: chartWidth,
            height: chartHeight
          }
        });
      });
    } else if (charts.length === 3) {
      const leftWidth = Math.floor((canvasWidth - 2 * margin - gap) * 0.58);
      const rightWidth = canvasWidth - 2 * margin - gap - leftWidth;
      const stackedHeight = Math.floor((availableHeight - gap) / 2);

      rearranged.push({
        ...charts[0],
        layout: {
          ...charts[0].layout,
          x: margin,
          y: currentY,
          width: leftWidth,
          height: availableHeight
        }
      });

      rearranged.push({
        ...charts[1],
        layout: {
          ...charts[1].layout,
          x: margin + leftWidth + gap,
          y: currentY,
          width: rightWidth,
          height: stackedHeight
        }
      });

      rearranged.push({
        ...charts[2],
        layout: {
          ...charts[2].layout,
          x: margin + leftWidth + gap,
          y: currentY + stackedHeight + gap,
          width: rightWidth,
          height: stackedHeight
        }
      });
    } else {
      const cols = charts.length >= 6 ? 3 : 2;
      const chartWidth = Math.floor((canvasWidth - 2 * margin - (cols - 1) * gap) / cols);
      const rows = Math.ceil(charts.length / cols);
      const chartHeight = Math.max(240, Math.floor((availableHeight - (rows - 1) * gap) / rows));

      charts.forEach((chart, index) => {
        const col = index % cols;
        const row = Math.floor(index / cols);
        const x = margin + col * (chartWidth + gap);
        const y = currentY + row * (chartHeight + gap);
        rearranged.push({
          ...chart,
          layout: {
            ...chart.layout,
            x,
            y,
            width: chartWidth,
            height: chartHeight
          }
        });
      });
    }
  }

  return rearranged;
}

interface DashboardState {
  current: DashboardDefinition | null;
  setDashboard: (dashboard: DashboardDefinition) => void;
  updateVisualLayout: (pageName: string, visualName: string, layout: Partial<VisualLayout>) => void;
  updateVisualType: (pageName: string, visualName: string, visualType: string) => void;
  updateVisualTitle: (pageName: string, visualName: string, title: string) => void;
  updateVisualBoundField: (pageName: string, visualName: string, fieldIndex: number, newField: string) => void;
  updateVisualBoundFields: (pageName: string, visualName: string, boundFields: string[]) => void;
  addVisual: (pageName: string, visual: Visual) => void;
  addPage: (pageName?: string) => string;
  removePage: (pageName: string) => void;
  moveVisualToPage: (sourcePage: string, targetPage: string, visualName: string) => void;
  rearrangePageVisuals: (pageName: string) => void;
}

export const useDashboardStore = create<DashboardState>((set) => ({
  current: null,
  setDashboard: (dashboard) => set({ current: dashboard }),
  updateVisualLayout: (pageName, visualName, layout) =>
    set((state) => {
      if (!state.current) return state;
      const pages = state.current.pages.map((page) =>
        page.name !== pageName
          ? page
          : {
              ...page,
              visuals: page.visuals.map((v) =>
                v.name !== visualName ? v : { ...v, layout: { ...v.layout, ...layout } }
              )
            }
      );
      return { current: { ...state.current, pages } };
    }),
  updateVisualType: (pageName, visualName, visualType) =>
    set((state) => {
      if (!state.current) return state;
      const pages = state.current.pages.map((page) =>
        page.name !== pageName
          ? page
          : {
              ...page,
              visuals: page.visuals.map((v) =>
                v.name !== visualName ? v : { ...v, visualType }
              )
            }
      );
      return { current: { ...state.current, pages } };
    }),
  updateVisualTitle: (pageName, visualName, name) =>
    set((state) => {
      if (!state.current) return state;
      const pages = state.current.pages.map((page) =>
        page.name !== pageName
          ? page
          : {
              ...page,
              visuals: page.visuals.map((v) =>
                v.name !== visualName ? v : { ...v, name }
              )
            }
      );
      return { current: { ...state.current, pages } };
    }),
  updateVisualBoundField: (pageName, visualName, fieldIndex, newField) =>
    set((state) => {
      if (!state.current) return state;
      const pages = state.current.pages.map((page) =>
        page.name !== pageName
          ? page
          : {
              ...page,
              visuals: page.visuals.map((v) => {
                if (v.name !== visualName) return v;
                const updated = [...v.boundFields];
                if (fieldIndex < updated.length) {
                  updated[fieldIndex] = newField;
                } else {
                  updated.push(newField);
                }
                return { ...v, boundFields: updated };
              })
            }
      );
      return { current: { ...state.current, pages } };
    }),
  updateVisualBoundFields: (pageName, visualName, boundFields) =>
    set((state) => {
      if (!state.current) return state;
      const pages = state.current.pages.map((page) =>
        page.name !== pageName
          ? page
          : {
              ...page,
              visuals: page.visuals.map((v) =>
                v.name !== visualName ? v : { ...v, boundFields: [...boundFields] }
              )
            }
      );
      return { current: { ...state.current, pages } };
    }),
  addVisual: (pageName, visual) =>
    set((state) => {
      if (!state.current) return state;
      const pages = state.current.pages.map((page) =>
        page.name !== pageName
          ? page
          : {
              ...page,
              visuals: [...page.visuals, visual]
            }
      );
      return { current: { ...state.current, pages } };
    }),
  addPage: (pageName) => {
    let createdPageName = "";
    set((state) => {
      if (!state.current) return state;
      const existingNames = new Set(state.current.pages.map((p) => p.name));
      let candidate = pageName?.trim() || `Page ${state.current.pages.length + 1}`;
      let counter = 2;
      while (existingNames.has(candidate)) {
        candidate = `Page ${state.current.pages.length + counter}`;
        counter++;
      }
      createdPageName = candidate;
      const newPage: Page = {
        name: candidate,
        canvasWidth: 1280,
        canvasHeight: 720,
        visuals: []
      };
      return {
        current: {
          ...state.current,
          pages: [...state.current.pages, newPage]
        }
      };
    });
    return createdPageName;
  },
  removePage: (pageName) =>
    set((state) => {
      if (!state.current || state.current.pages.length <= 1) return state;
      const pages = state.current.pages.filter((p) => p.name !== pageName);
      return { current: { ...state.current, pages } };
    }),
  moveVisualToPage: (sourcePageName, targetPageName, visualName) =>
    set((state) => {
      if (!state.current || sourcePageName === targetPageName) return state;
      const sourcePage = state.current.pages.find((p) => p.name === sourcePageName);
      const targetPage = state.current.pages.find((p) => p.name === targetPageName);
      if (!sourcePage || !targetPage) return state;

      const visualToMove = sourcePage.visuals.find((v) => v.name === visualName);
      if (!visualToMove) return state;

      // Position in target page cleanly
      const existingInTarget = targetPage.visuals.length;
      const targetX = 20 + (existingInTarget % 3) * 380;
      const targetY = 20 + Math.floor(existingInTarget / 3) * 260;

      const movedVisual: Visual = {
        ...visualToMove,
        layout: {
          ...visualToMove.layout,
          x: targetX,
          y: targetY
        }
      };

      const pages = state.current.pages.map((p) => {
        if (p.name === sourcePageName) {
          return { ...p, visuals: p.visuals.filter((v) => v.name !== visualName) };
        }
        if (p.name === targetPageName) {
          return { ...p, visuals: [...p.visuals, movedVisual] };
        }
        return p;
      });

      return { current: { ...state.current, pages } };
    }),
  rearrangePageVisuals: (pageName) =>
    set((state) => {
      if (!state.current) return state;
      const pages = state.current.pages.map((p) => {
        if (p.name !== pageName) return p;
        const rearrangedVisuals = computeAutoRearrange(p.visuals, p.canvasWidth, p.canvasHeight);
        return { ...p, visuals: rearrangedVisuals };
      });
      return { current: { ...state.current, pages } };
    })
}));
