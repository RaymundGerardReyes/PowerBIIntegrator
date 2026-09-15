import { describe, it, expect } from "vitest";
import { render } from "@testing-library/react";
import { ExcelReportPreview } from "@features/reports/components/ExcelReportPreview";

describe("Security: XSS sanitization in rendered data", () => {
  it("does not execute injected script markup from data rows", () => {
    const { container } = render(
      <ExcelReportPreview rows={[{ name: "<img src=x onerror=alert(1) />" }]} />
    );
    expect(container.querySelector("img")).toBeNull();
  });
});
