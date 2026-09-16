import { describe, it, expect } from "vitest";
import { screen, fireEvent } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { renderWithProviders } from "../../../setup/test-utils";
import { ReportsHubPage } from "@features/reports/components/ReportsHubPage";

describe("ReportsHubPage", () => {
  it("renders format selector pills and defaults to PDF preview", () => {
    renderWithProviders(<ReportsHubPage />);

    expect(screen.getByText(/Executive Report Generation Hub/i)).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /PDF Document/i })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /Excel Workbook/i })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /Word Document/i })).toBeInTheDocument();

    expect(screen.getByText(/Live PDF Preview/i)).toBeInTheDocument();
  });

  it("switches preview to Excel workbook preview when Excel pill is clicked", async () => {
    const user = userEvent.setup();
    renderWithProviders(<ReportsHubPage />);

    const excelPill = screen.getByRole("button", { name: /Excel Workbook/i });
    await user.click(excelPill);

    expect(screen.getByText(/Live EXCEL Preview/i)).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "export-excel-button" })).toBeInTheDocument();
  });

  it("switches preview to Word document preview when Word pill is clicked", async () => {
    const user = userEvent.setup();
    renderWithProviders(<ReportsHubPage />);

    const wordPill = screen.getByRole("button", { name: /Word Document/i });
    await user.click(wordPill);

    expect(screen.getByText(/Live WORD Preview/i)).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "export-word-button" })).toBeInTheDocument();
  });

  it("allows updating report title metadata", async () => {
    renderWithProviders(<ReportsHubPage />);

    const titleInput = screen.getByLabelText("report-title-input") as HTMLInputElement;
    expect(titleInput.value).toBe("Executive Revenue & Analytics Report");

    fireEvent.change(titleInput, { target: { value: "Q4 Regional Performance Overview" } });

    expect(titleInput.value).toBe("Q4 Regional Performance Overview");
  });
});
