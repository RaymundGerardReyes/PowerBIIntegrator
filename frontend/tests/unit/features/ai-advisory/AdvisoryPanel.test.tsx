import { describe, it, expect, vi, beforeEach } from "vitest";
import { screen, fireEvent, waitFor } from "@testing-library/react";
import { renderWithProviders } from "../../../setup/test-utils";
import { AdvisoryPanel } from "@features/ai-advisory/components/AdvisoryPanel";
import * as advisoryApi from "@features/ai-advisory/api/advisoryApi";

vi.mock("@features/ai-advisory/api/advisoryApi");

describe("AdvisoryPanel Component", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders header with Read-Only Guardrailed badge and preset questions", () => {
    renderWithProviders(<AdvisoryPanel runId="run-demo-001" userRole="DataSteward" />);

    expect(screen.getByText("AI Advisory Tier")).toBeInTheDocument();
    expect(screen.getByText("Read-Only Guardrailed")).toBeInTheDocument();
    expect(screen.getByText(/Why were these 42 rows treated as duplicates\?/i)).toBeInTheDocument();
    expect(screen.getByText(/Why did this column fail schema validation\?/i)).toBeInTheDocument();
  });

  it("executes advisory query when preset question is clicked and displays grounded response with citations", async () => {
    const mockResponse = {
      id: "test-query-id",
      runId: "run-demo-001",
      questionType: "Duplicates",
      userQuestion: "Why were these 42 rows treated as duplicates?",
      answer: "42 rows were routed to quarantine via ExactHashRule-v2.",
      providerUsed: "LocalOllama",
      citedRuleIds: ["ExactHashRule-v2", "CompositeKeyRule-CustInv"],
      citedRunIds: ["run-demo-001"],
      redactedFieldsCount: 0,
      isUnlockedConfidential: false,
      correlationId: "corr-123",
      timestampUtc: new Date().toISOString()
    };

    vi.mocked(advisoryApi.queryAdvisory).mockResolvedValueOnce(mockResponse);

    renderWithProviders(<AdvisoryPanel runId="run-demo-001" userRole="DataSteward" />);

    const presetBtn = screen.getByText(/Why were these 42 rows treated as duplicates\?/i);
    fireEvent.click(presetBtn);

    await waitFor(() => {
      expect(screen.getByText("AI Advisory Narrative")).toBeInTheDocument();
      expect(screen.getByText(/42 rows were routed to quarantine via ExactHashRule-v2/i)).toBeInTheDocument();
      expect(screen.getByText("ExactHashRule-v2")).toBeInTheDocument();
      expect(screen.getByText("CompositeKeyRule-CustInv")).toBeInTheDocument();
    });
  });

  it("allows submitting custom question via input field", async () => {
    const mockResponse = {
      id: "test-query-custom",
      runId: "run-demo-001",
      questionType: "General",
      userQuestion: "Can you explain the Silver stage?",
      answer: "Silver stage deduplicated records.",
      providerUsed: "LocalOllama",
      citedRuleIds: ["PipelineExecutionStageRule"],
      citedRunIds: ["run-demo-001"],
      redactedFieldsCount: 0,
      isUnlockedConfidential: false,
      correlationId: "corr-456",
      timestampUtc: new Date().toISOString()
    };

    vi.mocked(advisoryApi.queryAdvisory).mockResolvedValueOnce(mockResponse);

    renderWithProviders(<AdvisoryPanel runId="run-demo-001" userRole="DataSteward" />);

    const input = screen.getByPlaceholderText(/Ask a question about pipeline transformations/i);
    fireEvent.change(input, { target: { value: "Can you explain the Silver stage?" } });

    const askBtn = screen.getByText("Ask AI Advisor");
    fireEvent.click(askBtn);

    await waitFor(() => {
      expect(screen.getByText("Silver stage deduplicated records.")).toBeInTheDocument();
      expect(screen.getByText("PipelineExecutionStageRule")).toBeInTheDocument();
    });
  });
});
