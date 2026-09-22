import { describe, it, expect, vi, beforeEach } from "vitest";
import { renderHook, act } from "@testing-library/react";
import {
  usePipelineRun,
  isValidStageTransition,
  VALID_STAGE_TRANSITIONS
} from "@features/data-quality/hooks/usePipelineRun";
import * as api from "@features/data-quality/api/dataQualityApi";

vi.mock("@features/data-quality/api/dataQualityApi");

describe("usePipelineRun - 6-Stage Sequential State Machine", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("validates legal forward and backward stage transitions", () => {
    expect(isValidStageTransition("profile", "dedupe")).toBe(true);
    expect(isValidStageTransition("dedupe", "clean")).toBe(true);
    expect(isValidStageTransition("clean", "transform")).toBe(true);
    expect(isValidStageTransition("transform", "visuals")).toBe(true);
    expect(isValidStageTransition("visuals", "advisory")).toBe(true);

    // Backward review steps
    expect(isValidStageTransition("dedupe", "profile")).toBe(true);
    expect(isValidStageTransition("clean", "dedupe")).toBe(true);

    // Same stage is identity
    expect(isValidStageTransition("clean", "clean")).toBe(true);
  });

  it("rejects illegal skip transitions", () => {
    expect(isValidStageTransition("profile", "transform")).toBe(false);
    expect(isValidStageTransition("profile", "visuals")).toBe(false);
    expect(isValidStageTransition("profile", "advisory")).toBe(false);
    expect(isValidStageTransition("dedupe", "visuals")).toBe(false);
    expect(isValidStageTransition("dedupe", "advisory")).toBe(false);
  });

  it("initializes at 'profile' stage with empty state", () => {
    const { result } = renderHook(() => usePipelineRun());
    expect(result.current.currentStage).toBe("profile");
    expect(result.current.profile).toBeNull();
    expect(result.current.runResult).toBeNull();
    expect(result.current.suggestions).toEqual([]);
    expect(result.current.error).toBeNull();
  });

  it("advances stage sequentially through transitionTo", () => {
    const { result } = renderHook(() => usePipelineRun());

    act(() => {
      result.current.transitionTo("dedupe");
    });
    expect(result.current.currentStage).toBe("dedupe");

    act(() => {
      result.current.transitionTo("clean");
    });
    expect(result.current.currentStage).toBe("clean");

    act(() => {
      result.current.transitionTo("transform");
    });
    expect(result.current.currentStage).toBe("transform");
  });

  it("throws and rejects stage transition upon illegal transition attempt", () => {
    const { result } = renderHook(() => usePipelineRun("profile"));

    expect(() => {
      act(() => {
        result.current.transitionTo("transform"); // skipping dedupe and clean
      });
    }).toThrow(/Illegal stage transition from 'profile' to 'transform'/i);

    expect(result.current.currentStage).toBe("profile");
  });

  it("resets pipeline to profile and clears state via resetPipeline", () => {
    const { result } = renderHook(() => usePipelineRun("clean"));

    act(() => {
      result.current.resetPipeline();
    });

    expect(result.current.currentStage).toBe("profile");
    expect(result.current.profile).toBeNull();
    expect(result.current.error).toBeNull();
  });

  it("advances to 'dedupe' stage upon successful runProfiling", async () => {
    vi.mocked(api.profileDataset).mockResolvedValueOnce({
      id: "p1",
      datasetName: "SampleDataset",
      sourceReference: "DataPath",
      totalRows: 1200,
      profiledAtUtc: new Date().toISOString(),
      columnProfiles: []
    });

    const { result } = renderHook(() => usePipelineRun());

    await act(async () => {
      await result.current.runProfiling("DataPath", "SampleDataset");
    });

    expect(result.current.currentStage).toBe("dedupe");
    expect(result.current.profile).not.toBeNull();
    expect(result.current.error).toBeNull();
  });

  it("advances to 'visuals' stage upon successful executeFullPipeline", async () => {
    vi.mocked(api.runFullPipeline).mockResolvedValueOnce({
      id: "r1",
      runId: "run-001",
      sourceReference: "DataPath",
      startedAtUtc: new Date().toISOString(),
      completedAtUtc: new Date().toISOString(),
      isSuccess: true,
      stageSummaries: [
        {
          stageName: "transform",
          isSuccess: true,
          inputRowCount: 1200,
          outputRowCount: 1185,
          quarantinedRowCount: 15,
          triggeredRules: [],
          details: "Transform stage completed successfully"
        }
      ]
    });

    vi.mocked(api.getChartSuggestions).mockResolvedValueOnce([
      {
        recommendedVisualType: "barChart",
        confidenceScore: 0.95,
        reason: "Category distribution"
      }
    ]);

    const { result } = renderHook(() => usePipelineRun("transform"));

    await act(async () => {
      await result.current.executeFullPipeline("DataPath", "RawData", "GoldData");
    });

    expect(result.current.currentStage).toBe("visuals");
    expect(result.current.runResult).not.toBeNull();
    expect(result.current.suggestions).toHaveLength(1);
    expect(result.current.error).toBeNull();
  });
});
