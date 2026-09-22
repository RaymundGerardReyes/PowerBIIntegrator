import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import { CsvUploadForm } from "@features/data-sources/components/CsvUploadForm";
import { ExcelUploadForm } from "@features/data-sources/components/ExcelUploadForm";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { BrowserRouter } from "react-router-dom";
import * as dataSourcesApi from "@features/data-sources/api/dataSourcesApi";

vi.mock("@features/data-sources/api/dataSourcesApi");

describe("Data Sources Upload & Active Dataset Synchronization", () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false }, mutations: { retry: false } }
    });
    localStorage.clear();
    vi.clearAllMocks();
  });

  const renderWithProviders = (ui: React.ReactElement) => {
    return render(
      <QueryClientProvider client={queryClient}>
        <BrowserRouter>{ui}</BrowserRouter>
      </QueryClientProvider>
    );
  };

  it("synchronizes all 3 localStorage keys on successful CSV upload", async () => {
    vi.mocked(dataSourcesApi.uploadFile).mockResolvedValueOnce({
      id: "ds-csv-001",
      name: "Financials_2026.csv",
      type: "csv",
      connectionOrPath: "C:\\data\\Financials_2026.csv",
      schema: [{ name: "Revenue", dataType: "Decimal", isNullable: false }]
    });

    renderWithProviders(<CsvUploadForm />);

    const input = screen.getByLabelText("csv-upload-input") as HTMLInputElement;
    const file = new File(["dummy content"], "Financials_2026.csv", { type: "text/csv" });
    Object.defineProperty(input, "files", { value: [file] });

    const uploadBtn = screen.getByRole("button", { name: /Upload CSV/i });
    fireEvent.click(uploadBtn);

    await waitFor(() => {
      expect(screen.getByText(/Successfully registered: Financials_2026.csv/i)).toBeInTheDocument();
    });

    expect(localStorage.getItem("powerbi_active_model_name")).toBe("Financials_2026.csv");
    expect(localStorage.getItem("powerbi_active_dataset_id")).toBe("ds-csv-001");
    expect(localStorage.getItem("powerbi_active_model_id")).toBe("ds-csv-001");
  });

  it("synchronizes all 3 localStorage keys on successful Excel upload", async () => {
    vi.mocked(dataSourcesApi.uploadFile).mockResolvedValueOnce({
      id: "ds-xls-002",
      name: "Corporate_Budget.xlsx",
      type: "excel",
      connectionOrPath: "C:\\data\\Corporate_Budget.xlsx",
      schema: [{ name: "BudgetAmount", dataType: "Decimal", isNullable: false }]
    });

    renderWithProviders(<ExcelUploadForm />);

    const input = screen.getByLabelText("excel-upload-input") as HTMLInputElement;
    const file = new File(["dummy content"], "Corporate_Budget.xlsx", {
      type: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
    });
    Object.defineProperty(input, "files", { value: [file] });

    const uploadBtn = screen.getByRole("button", { name: /Upload File/i });
    fireEvent.click(uploadBtn);

    await waitFor(() => {
      expect(screen.getByText(/Successfully registered: Corporate_Budget.xlsx/i)).toBeInTheDocument();
    });

    expect(localStorage.getItem("powerbi_active_model_name")).toBe("Corporate_Budget.xlsx");
    expect(localStorage.getItem("powerbi_active_dataset_id")).toBe("ds-xls-002");
    expect(localStorage.getItem("powerbi_active_model_id")).toBe("ds-xls-002");
  });

  it("leaves localStorage untouched when upload fails", async () => {
    localStorage.setItem("powerbi_active_model_name", "ExistingDataset.csv");
    localStorage.setItem("powerbi_active_dataset_id", "existing-id");
    localStorage.setItem("powerbi_active_model_id", "existing-id");

    vi.mocked(dataSourcesApi.uploadFile).mockRejectedValueOnce(new Error("File corrupt or unreadable"));

    renderWithProviders(<ExcelUploadForm />);

    const input = screen.getByLabelText("excel-upload-input") as HTMLInputElement;
    const file = new File(["corrupt"], "Corrupt.xlsx", { type: "application/vnd.ms-excel" });
    Object.defineProperty(input, "files", { value: [file] });

    const uploadBtn = screen.getByRole("button", { name: /Upload File/i });
    fireEvent.click(uploadBtn);

    await waitFor(() => {
      expect(screen.getByRole("alert")).toHaveTextContent("File corrupt or unreadable");
    });

    // Verify localStorage retains original values without corruption
    expect(localStorage.getItem("powerbi_active_model_name")).toBe("ExistingDataset.csv");
    expect(localStorage.getItem("powerbi_active_dataset_id")).toBe("existing-id");
    expect(localStorage.getItem("powerbi_active_model_id")).toBe("existing-id");
  });
});
