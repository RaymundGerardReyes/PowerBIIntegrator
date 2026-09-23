import { describe, it, expect } from "vitest";
import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
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

  it("renders latency badge and expands details on click", async () => {
    const user = userEvent.setup();

    renderWithProviders(
      <ToolExecutionBadge
        toolName="analyze_dax_measures"
        status="completed"
        latencyMs={142}
        args={{ tableName: "Sales", measure: "TotalRevenue" }}
        result={{ status: "valid", daxParity: true }}
      />
    );

    expect(screen.getByText("142ms")).toBeInTheDocument();
    expect(screen.queryByTestId("tool-execution-details")).not.toBeInTheDocument();

    const badge = screen.getByTestId("tool-execution-badge");
    await user.click(badge);

    expect(screen.getByTestId("tool-execution-details")).toBeInTheDocument();
    expect(screen.getByText(/TotalRevenue/i)).toBeInTheDocument();
    expect(screen.getByText(/daxParity/i)).toBeInTheDocument();

    await user.click(badge);
    expect(screen.queryByTestId("tool-execution-details")).not.toBeInTheDocument();
  });
});
