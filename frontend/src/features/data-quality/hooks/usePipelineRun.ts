import { useState } from "react";
import type { DatasetProfileDto, PipelineRunResultDto, ChartSuggestionDto } from "@shared/types/api-contracts";
import * as api from "../api/dataQualityApi";

export function usePipelineRun() {
  const [profile, setProfile] = useState<DatasetProfileDto | null>(null);
  const [runResult, setRunResult] = useState<PipelineRunResultDto | null>(null);
  const [suggestions, setSuggestions] = useState<ChartSuggestionDto[]>([]);
  const [isRunning, setIsRunning] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const runProfiling = async (sourceReference: string, datasetName: string) => {
    setIsRunning(true);
    setError(null);
    try {
      const data = await api.profileDataset(sourceReference, datasetName);
      setProfile(data);
    } catch (err: any) {
      setError(err.message || "Failed to profile dataset");
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
    } catch (err: any) {
      setError(err.message || "Pipeline execution failed");
    } finally {
      setIsRunning(false);
    }
  };

  return {
    profile,
    runResult,
    suggestions,
    isRunning,
    error,
    runProfiling,
    executeFullPipeline
  };
}

