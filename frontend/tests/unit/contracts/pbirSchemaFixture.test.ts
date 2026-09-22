import { describe, it, expect } from "vitest";
import reportJson from "../../fixtures/pbir/definition/report.json";
import pagesJson from "../../fixtures/pbir/definition/pages/pages.json";
import pageJson from "../../fixtures/pbir/definition/pages/OverviewAnalytics/page.json";

describe("PBIR Schema Contract & Fixture Validation", () => {
  describe("report.json schema compliance", () => {
    it("strictly omits activePageIndex and activePageName", () => {
      const record = reportJson as Record<string, unknown>;
      expect(record.activePageIndex).toBeUndefined();
      expect(record.activePageName).toBeUndefined();
    });

    it("has layoutOptimization set to exact string 'None'", () => {
      expect(reportJson.layoutOptimization).toBe("None");
      expect(typeof reportJson.layoutOptimization).toBe("string");
    });

    it("uses reportVersionAtImport instead of version in themeCollection.baseTheme", () => {
      const theme = reportJson.themeCollection.baseTheme as Record<string, unknown>;
      expect(theme.reportVersionAtImport).toBeDefined();
      expect(typeof theme.reportVersionAtImport).toBe("string");
      expect(theme.version).toBeUndefined();
    });
  });

  describe("pages.json schema compliance", () => {
    it("contains non-empty pageOrder array and activePageName", () => {
      expect(Array.isArray(pagesJson.pageOrder)).toBe(true);
      expect(pagesJson.pageOrder.length).toBeGreaterThan(0);
      expect(typeof pagesJson.activePageName).toBe("string");
      expect(pagesJson.pageOrder).toContain(pagesJson.activePageName);
    });
  });

  describe("page.json schema compliance", () => {
    it("has clean alphanumeric identifier and separate human-readable displayName", () => {
      expect(pageJson.name).toMatch(/^[a-zA-Z0-9]+$/);
      expect(pageJson.displayName).toBe("Overview & Analytics");
      expect(pageJson.width).toBe(1280);
      expect(pageJson.height).toBe(720);
      expect(pageJson.displayOption).toBe("FitToPage");
    });
  });
});

