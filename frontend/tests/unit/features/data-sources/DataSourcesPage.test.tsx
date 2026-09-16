import { describe, it, expect } from "vitest";
import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { renderWithProviders } from "../../../setup/test-utils";
import { DataSourcesPage } from "@features/data-sources/components/DataSourcesPage";

describe("DataSourcesPage", () => {
  it("renders all ingestion tabs and defaults to Excel upload", () => {
    renderWithProviders(<DataSourcesPage />);

    expect(screen.getByText(/Data Sources & Ingestion Hub/i)).toBeInTheDocument();
    expect(screen.getByRole("tab", { name: /Excel Spreadsheets/i })).toBeInTheDocument();
    expect(screen.getByRole("tab", { name: /Delimited CSV/i })).toBeInTheDocument();
    expect(screen.getByRole("tab", { name: /Relational Database/i })).toBeInTheDocument();
    expect(screen.getByRole("tab", { name: /Registered Catalog/i })).toBeInTheDocument();

    expect(screen.getByLabelText("excel-upload-input")).toBeInTheDocument();
  });

  it("switches to CSV upload tab when clicked", async () => {
    const user = userEvent.setup();
    renderWithProviders(<DataSourcesPage />);

    const csvTab = screen.getByRole("tab", { name: /Delimited CSV/i });
    await user.click(csvTab);

    expect(screen.getByLabelText("csv-upload-input")).toBeInTheDocument();
    expect(screen.getByText(/Infers column data types, delimiters/i)).toBeInTheDocument();
  });

  it("switches to Relational Database tab with provider dropdown", async () => {
    const user = userEvent.setup();
    renderWithProviders(<DataSourcesPage />);

    const sqlTab = screen.getByRole("tab", { name: /Relational Database/i });
    await user.click(sqlTab);

    const providerSelect = screen.getByLabelText("sql-provider-select") as HTMLSelectElement;
    expect(providerSelect).toBeInTheDocument();
    expect(providerSelect.value).toBe("sqlserver");

    await user.selectOptions(providerSelect, "postgresql");
    expect(providerSelect.value).toBe("postgresql");

    expect(screen.getByLabelText("sql-host-input")).toBeInTheDocument();
    expect(screen.getByLabelText("sql-database-input")).toBeInTheDocument();
  });

  it("switches to Catalog tab and displays active sources", async () => {
    const user = userEvent.setup();
    renderWithProviders(<DataSourcesPage />);

    const catalogTab = screen.getByRole("tab", { name: /Registered Catalog/i });
    await user.click(catalogTab);

    expect(screen.getByText("Global_Sales_2026.xlsx")).toBeInTheDocument();
    expect(screen.getByText("Customer_Churn_Monthly.csv")).toBeInTheDocument();
    expect(screen.getByText("TelemetryDb@sql-cluster-01")).toBeInTheDocument();
  });
});

