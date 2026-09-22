import { useState } from "react";
import type { DatasetProfileDto, PipelineRunResultDto, ChartSuggestionDto } from "@shared/types/api-contracts";
import * as api from "../api/dataQualityApi";

export type PipelineStage = "profile" | "dedupe" | "clean" | "transform" | "visuals" | "advisory";

export const PIPELINE_STAGES: readonly PipelineStage[] = [
  "profile",
  "dedupe",
  "clean",
  "transform",
  "visuals",
  "advisory"
] as const;

export const VALID_STAGE_TRANSITIONS: Record<PipelineStage, PipelineStage[]> = {
  profile: ["dedupe"],
  dedupe: ["profile", "clean"],
  clean: ["dedupe", "transform"],
  transform: ["clean", "visuals"],
  visuals: ["transform", "advisory"],
  advisory: ["visuals", "profile"]
};

export function isValidStageTransition(from: PipelineStage, to: PipelineStage): boolean {
  if (from === to) return true;
  return VALID_STAGE_TRANSITIONS[from]?.includes(to) ?? false;
}

export function usePipelineRun(initialStage: PipelineStage = "profile") {
  const [currentStage, setCurrentStage] = useState<PipelineStage>(initialStage);
  const [profile, setProfile] = useState<DatasetProfileDto | null>(null);
  const [runResult, setRunResult] = useState<PipelineRunResultDto | null>(null);
  const [suggestions, setSuggestions] = useState<ChartSuggestionDto[]>([]);
  const [isRunning, setIsRunning] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const transitionTo = (nextStage: PipelineStage) => {
    if (!isValidStageTransition(currentStage, nextStage)) {
      const msg = `Illegal stage transition from '${currentStage}' to '${nextStage}'. Pipeline stages must proceed sequentially: profile -> dedupe -> clean -> transform -> visuals -> advisory.`;
      setError(msg);
      throw new Error(msg);
    }
    setError(null);
    setCurrentStage(nextStage);
  };

  const resetPipeline = () => {
    setCurrentStage("profile");
    setProfile(null);
    setRunResult(null);
    setSuggestions([]);
    setError(null);
    setIsRunning(false);
  };

  const runProfiling = async (sourceReference: string, datasetName: string) => {
    setIsRunning(true);
    setError(null);
    try {
      const data = await api.profileDataset(sourceReference, datasetName);
      setProfile(data);
      setCurrentStage("dedupe");
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Failed to profile dataset");
    } finally {
      setIsRunning(false);
    }
  };

  const executeFullPipeline = async (sourceReference: string, datasetName: string, targetGoldTable: string) => {
    setIsRunning(true);
    setError(null);
    try {
      const res = await api.runFullPipeline(sourceReference, datasetName, targetGoldTable);
      setRunResult(res);
      const chartData = await api.getChartSuggestions(targetGoldTable);
      setSuggestions(chartData);
      setCurrentStage("visuals");
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Pipeline execution failed");
    } finally {
      setIsRunning(false);
    }
  };

  return {
    currentStage,
    profile,
    runResult,
    suggestions,
    isRunning,
    error,
    transitionTo,
    resetPipeline,
    runProfiling,
    executeFullPipeline
  };
}
