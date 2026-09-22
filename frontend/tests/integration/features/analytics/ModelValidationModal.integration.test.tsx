import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import { ModelValidationModal } from "@features/analytics/components/ModelValidationModal";
import * as analyticsApi from "@features/analytics/api/analyticsApi";
import type { ValidateAnalyticsModelResponseDto } from "@shared/types/api-contracts";

vi.mock("@features/analytics/api/analyticsApi");

describe("ModelValidationModal - Integration Tests", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("does not render contents when open is false", () => {
    const { container } = render(
      <ModelValidationModal open={false} onClose={vi.fn()} modelId="mod-001" modelName="SalesModel" />
    );
    expect(container.firstChild).toBeNull();
  });

  it("renders header and validate button when open is true", () => {
    render(
      <ModelValidationModal open={true} onClose={vi.fn()} modelId="mod-001" modelName="SalesModel" />
    );

    expect(screen.getByText("Validate Semantic Model")).toBeInTheDocument();
    expect(screen.getByText(/Validates/i)).toBeInTheDocument();
    expect(screen.getByText("SalesModel")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "run-model-validation" })).toBeInTheDocument();
  });

  it("executes validation and renders 'Valid & Deployable' for clean models", async () => {
    const validResponse: ValidateAnalyticsModelResponseDto = {
      modelId: "mod-001",
      modelName: "SalesModel",
      isValid: true,
      errors: [],
      warnings: [],
      detectedCycles: [],
      orphanTables: []
    };

    vi.mocked(analyticsApi.validateAnalyticsModel).mockResolvedValueOnce(validResponse);

    render(
      <ModelValidationModal open={true} onClose={vi.fn()} modelId="mod-001" modelName="SalesModel" />
    );

    const validateBtn = screen.getByRole("button", { name: "run-model-validation" });
    fireEvent.click(validateBtn);

    expect(analyticsApi.validateAnalyticsModel).toHaveBeenCalledWith("mod-001");

    await waitFor(() => {
      expect(screen.getByText("Valid & Deployable")).toBeInTheDocument();
    });

    expect(screen.queryByText(/Errors \(/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/Detected Cycles/i)).not.toBeInTheDocument();
  });

  it("renders 'Issues Detected' with cycles, orphan tables, and errors for broken models", async () => {
    const invalidResponse: ValidateAnalyticsModelResponseDto = {
      modelId: "mod-002",
      modelName: "CorruptedModel",
      isValid: false,
      errors: ["DAX parse error in Measure 'TotalRevenue': Unexpected token"],
      warnings: ["High cardinality column 'CustomerId' may slow down cross-filtering"],
      detectedCycles: ["DimCustomer -> FactSales -> DimCustomer"],
      orphanTables: ["StagingAuditLogs"]
    };

    vi.mocked(analyticsApi.validateAnalyticsModel).mockResolvedValueOnce(invalidResponse);

    render(
      <ModelValidationModal open={true} onClose={vi.fn()} modelId="mod-002" modelName="CorruptedModel" />
    );

    const validateBtn = screen.getByRole("button", { name: "run-model-validation" });
    fireEvent.click(validateBtn);

    await waitFor(() => {
      expect(screen.getByText("Issues Detected")).toBeInTheDocument();
    });

    expect(screen.getByText("Errors (1)")).toBeInTheDocument();
    expect(screen.getByText(/DAX parse error in Measure 'TotalRevenue'/i)).toBeInTheDocument();

    expect(screen.getByText("Detected Cycles")).toBeInTheDocument();
    expect(screen.getByText("DimCustomer -> FactSales -> DimCustomer")).toBeInTheDocument();

    expect(screen.getByText("Orphan Tables (1)")).toBeInTheDocument();
    expect(screen.getByText("StagingAuditLogs")).toBeInTheDocument();

    expect(screen.getByText("Warnings (1)")).toBeInTheDocument();
    expect(screen.getByText(/High cardinality column 'CustomerId'/i)).toBeInTheDocument();
  });

  it("renders danger alert on API failure", async () => {
    vi.mocked(analyticsApi.validateAnalyticsModel).mockRejectedValueOnce(
      new Error("Gateway Timeout: Analytics engine unreachable")
    );

    render(
      <ModelValidationModal open={true} onClose={vi.fn()} modelId="mod-003" modelName="TimeoutModel" />
    );

    const validateBtn = screen.getByRole("button", { name: "run-model-validation" });
    fireEvent.click(validateBtn);

    await waitFor(() => {
      expect(screen.getByText("Gateway Timeout: Analytics engine unreachable")).toBeInTheDocument();
    });
  });

  it("calls onClose when close button is clicked", () => {
    const handleClose = vi.fn();
    render(
      <ModelValidationModal open={true} onClose={handleClose} modelId="mod-001" modelName="SalesModel" />
    );

    const closeBtn = screen.getByRole("button", { name: "close-modal" });
    fireEvent.click(closeBtn);

    expect(handleClose).toHaveBeenCalledTimes(1);
  });
});
