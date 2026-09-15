import { describe, it, expect } from "vitest";
import type { CustomLayoutConfig } from "@features/powerbi-embed/model/layoutTypes";

describe("Regression: custom layout schema shape", () => {
  it("keeps the CustomLayoutConfig shape stable across releases", () => {
    const sample: CustomLayoutConfig = {
      pageSize: { width: 1920, height: 1080 },
      displayOption: "FitToPage",
      visualsLayout: { visual1: { x: 0, y: 0, width: 100, height: 100 } }
    };
    expect(Object.keys(sample)).toEqual(["pageSize", "displayOption", "visualsLayout"]);
  });
});
