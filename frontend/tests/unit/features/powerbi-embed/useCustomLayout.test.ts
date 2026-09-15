import { describe, it, expect } from "vitest";
import { renderHook } from "@testing-library/react";
import { useCustomLayout } from "@features/powerbi-embed/hooks/useCustomLayout";

describe("useCustomLayout", () => {
  it("produces valid Power BI custom layout settings", () => {
    const { result } = renderHook(() =>
      useCustomLayout({
        pageSize: { width: 1920, height: 1080 },
        displayOption: "FitToPage",
        visualsLayout: { visual1: { x: 40, y: 30, width: 400, height: 180, displayState: "Visible" } }
      })
    );

    const settings = result.current.toPowerBiSettings();
    expect(settings.customLayout?.pageSize).toEqual({ type: 4, width: 1920, height: 1080 });
  });
});
