import { describe, it, expect, vi, beforeEach } from "vitest";
import { renderHook, act } from "@testing-library/react";
import { useAdvisoryQuery } from "@features/ai-advisory/hooks/useAdvisoryQuery";
import * as advisoryApi from "@features/ai-advisory/api/advisoryApi";

vi.mock("@features/ai-advisory/api/advisoryApi");

describe("useAdvisoryQuery Hook", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("initializes with default state", () => {
    const { result } = renderHook(() => useAdvisoryQuery("run-demo-001", "DataSteward"));

    expect(result.current.runId).toBe("run-demo-001");
    expect(result.current.query).toBe("");
    expect(result.current.result).toBeNull();
    expect(result.current.isLoading).toBe(false);
    expect(result.current.error).toBeNull();
    expect(result.current.isUnlockedConfidential).toBe(false);
  });

  it("handles successful advisory query execution", async () => {
    const mockData = {
      id: "q-1",
      runId: "run-demo-001",
      questionType: "Duplicates",
      userQuestion: "Why duplicate?",
      answer: "Exact hash matched.",
      providerUsed: "LocalOllama",
      citedRuleIds: ["ExactHashRule-v2"],
      citedRunIds: ["run-demo-001"],
      redactedFieldsCount: 0,
      isUnlockedConfidential: false,
      correlationId: "corr-1",
      timestampUtc: new Date().toISOString()
    };

    vi.mocked(advisoryApi.queryAdvisory).mockResolvedValueOnce(mockData);

    const { result } = renderHook(() => useAdvisoryQuery("run-demo-001", "DataSteward"));

    await act(async () => {
      await result.current.askQuestion("Why duplicate?", "Duplicates");
    });

    expect(result.current.result).toEqual(mockData);
    expect(result.current.isLoading).toBe(false);
    expect(result.current.error).toBeNull();
  });

  it("handles confidential unlock authorization", async () => {
    vi.mocked(advisoryApi.unlockConfidentialExposure).mockResolvedValueOnce({
      success: true,
      token: "unlock-12345",
      message: "Unlocked",
      expiresAtUtc: new Date().toISOString()
    });

    const { result } = renderHook(() => useAdvisoryQuery("run-demo-001", "DataSteward"));

    await act(async () => {
      await result.current.handleUnlockConfirm("Business audit");
    });

    expect(result.current.isUnlockedConfidential).toBe(true);
  });
});
