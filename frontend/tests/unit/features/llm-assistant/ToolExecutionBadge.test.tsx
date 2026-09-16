import { describe, it, expect } from "vitest";
import { screen } from "@testing-library/react";
import { renderWithProviders } from "../../../setup/test-utils";
import { ToolExecutionBadge } from "@features/llm-assistant/components/ToolExecutionBadge";

describe("ToolExecutionBadge Component", () => {
  it("renders executing status with pulse animation", () => {
    renderWithProviders(
      <ToolExecutionBadge toolName="compile_pbir_definition" status="running" />
    );

    const badge = screen.getByTestId("tool-execution-badge");
    expect(badge).toBeInTheDocument();
    expect(badge).toHaveTextContent(/compile_pbir_definition/i);
    expect(badge).toHaveTextContent(/Executing/i);
  });

  it("renders completed status with green theme", () => {
    renderWithProviders(
      <ToolExecutionBadge toolName="compile_tmdl_model" status="completed" />
    );

    const badge = screen.getByTestId("tool-execution-badge");
    expect(badge).toHaveTextContent(/compile_tmdl_model/i);
    expect(badge).not.toHaveTextContent(/Executing/i);
  });
});

