import { describe, it, expect, vi } from "vitest";
import { screen, fireEvent } from "@testing-library/react";
import { renderWithProviders } from "../../../setup/test-utils";
import { CitationBadge } from "@features/ai-advisory/components/CitationBadge";

describe("CitationBadge Component", () => {
  it("renders deterministic rule badge with grounded label", () => {
    renderWithProviders(<CitationBadge id="ExactHashRule-v2" type="rule" />);

    expect(screen.getByText("ExactHashRule-v2")).toBeInTheDocument();
    expect(screen.getByText("✓ Grounded")).toBeInTheDocument();
    expect(screen.getByText("⚖️")).toBeInTheDocument();
  });

  it("renders pipeline run badge with rocket icon", () => {
    renderWithProviders(<CitationBadge id="run-demo-001" type="run" />);

    expect(screen.getByText("run-demo-001")).toBeInTheDocument();
    expect(screen.getByText("🚀")).toBeInTheDocument();
  });

  it("triggers onClick callback on click and keyboard activation", () => {
    const handleClick = vi.fn();
    renderWithProviders(<CitationBadge id="ExactHashRule-v2" type="rule" onClick={handleClick} />);

    const badge = screen.getByRole("button");
    fireEvent.click(badge);
    expect(handleClick).toHaveBeenCalledWith("ExactHashRule-v2");

    fireEvent.keyDown(badge, { key: "Enter" });
    expect(handleClick).toHaveBeenCalledTimes(2);
  });
});
