import { apiClient } from "@shared/lib/http/apiClient";
import type {
  DatasetProfileDto,
  PipelineRunResultDto,
  ChartSuggestionDto
} from "@shared/types/api-contracts";

export async function profileDataset(sourceReference: string, datasetName: string): Promise<DatasetProfileDto> {
  const { data } = await apiClient.post<DatasetProfileDto>("/api/data-quality/profile", {
    sourceReference,
    datasetName
  });
  return data;
}

export async function cleanDataset(sourceReference: string, datasetName: string): Promise<PipelineRunResultDto> {
  const { data } = await apiClient.post<PipelineRunResultDto>("/api/data-quality/clean", {
    sourceReference,
    datasetName
  });
  return data;
}

export async function transformDataset(
  silverSourceTable: string,
  transformationPlanName: string,
  targetGoldTable: string
): Promise<PipelineRunResultDto> {
  const { data } = await apiClient.post<PipelineRunResultDto>("/api/data-quality/transform", {
    silverSourceTable,
    transformationPlanName,
    targetGoldTable
  });
  return data;
}

export async function runFullPipeline(
  sourceReference: string,
  datasetName: string,
  targetGoldTable: string
): Promise<PipelineRunResultDto> {
  const { data } = await apiClient.post<PipelineRunResultDto>("/api/data-quality/run-full-pipeline", {
    sourceReference,
    datasetName,
    targetGoldTable
  });
  return data;
}

export async function getChartSuggestions(tableId: string): Promise<ChartSuggestionDto[]> {
  const { data } = await apiClient.get<ChartSuggestionDto[]>(`/api/data-quality/chart-suggestions/${encodeURIComponent(tableId)}`);
  return data;
}

