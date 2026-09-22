import { describe, it, expect } from "vitest";
import {
  formatCurrency,
  formatPercent,
  formatCompactNumber
} from "@shared/lib/formatting/number";

describe("Number Formatters - Edge Cases & Robustness", () => {
  describe("formatCurrency", () => {
    it("formats standard positive values in PHP", () => {
      const result = formatCurrency(1250.5);
      expect(result).toMatch(/1,250\.50/);
    });

    it("formats zero", () => {
      const result = formatCurrency(0);
      expect(result).toMatch(/0\.00/);
    });

    it("formats negative numbers", () => {
      const result = formatCurrency(-500);
      expect(result).toMatch(/-.*500\.00/);
    });

    it("formats very large numbers", () => {
      const result = formatCurrency(1000000000);
      expect(result).toMatch(/1,000,000,000\.00/);
    });

    it("safely handles null, undefined, and NaN by returning zero format", () => {
      expect(formatCurrency(null)).toMatch(/0\.00/);
      expect(formatCurrency(undefined)).toMatch(/0\.00/);
      expect(formatCurrency(NaN)).toMatch(/0\.00/);
    });
  });

  describe("formatPercent", () => {
    it("formats decimal ratios as percentages", () => {
      expect(formatPercent(0.425)).toBe("42.5%");
      expect(formatPercent(1.0)).toBe("100.0%");
    });

    it("formats zero", () => {
      expect(formatPercent(0)).toBe("0.0%");
    });

    it("formats negative percentages", () => {
      expect(formatPercent(-0.125)).toBe("-12.5%");
    });

    it("formats percentages exceeding 100%", () => {
      expect(formatPercent(2.5)).toBe("250.0%");
    });

    it("safely handles null, undefined, and NaN", () => {
      expect(formatPercent(null)).toBe("0.0%");
      expect(formatPercent(undefined)).toBe("0.0%");
      expect(formatPercent(NaN)).toBe("0.0%");
    });
  });

  describe("formatCompactNumber", () => {
    it("formats thousands with K suffix", () => {
      expect(formatCompactNumber(1200)).toBe("1.2K");
      expect(formatCompactNumber(85000)).toBe("85K");
    });

    it("formats millions with M suffix", () => {
      expect(formatCompactNumber(3400000)).toBe("3.4M");
    });

    it("formats billions with B suffix", () => {
      expect(formatCompactNumber(2500000000)).toBe("2.5B");
    });

    it("formats zero and small numbers", () => {
      expect(formatCompactNumber(0)).toBe("0");
      expect(formatCompactNumber(42)).toBe("42");
    });

    it("formats negative compact numbers", () => {
      expect(formatCompactNumber(-45000)).toBe("-45K");
    });

    it("safely handles null, undefined, and NaN", () => {
      expect(formatCompactNumber(null)).toBe("0");
      expect(formatCompactNumber(undefined)).toBe("0");
      expect(formatCompactNumber(NaN)).toBe("0");
    });
  });
});

