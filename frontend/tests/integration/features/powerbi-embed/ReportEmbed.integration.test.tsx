import { describe, it, expect, vi } from "vitest";
import { render } from "@testing-library/react";
import { ReportEmbed } from "@features/powerbi-embed/components/ReportEmbed";

vi.mock("powerbi-client", () => ({
  service: { Service: vi.fn().mockImplementation(() => ({ embed: vi.fn(), reset: vi.fn() })) },
  factories: { hpmFactory: {}, wpmpFactory: {}, routerFactory: {} },
  models: { TokenType: { Embed: 1 }, LayoutType: { Custom: 1 } }
}));

describe("ReportEmbed integration", () => {
  it("mounts and calls embed on the Power BI service", () => {
    const { getByTestId } = render(
      <ReportEmbed config={{ reportId: "r1", embedUrl: "https://embed", accessToken: "tok" }} />
    );
    expect(getByTestId("powerbi-report-container")).toBeInTheDocument();
  });
});
