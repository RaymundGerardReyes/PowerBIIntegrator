import { apiClient } from "@shared/lib/http/apiClient";
import type {
  CreateMeasureRequest,
  CreateMeasureResponseDto,
  ValidateAnalyticsModelResponseDto
} from "@shared/types/api-contracts";

export async function createMeasure(payload: CreateMeasureRequest): Promise<CreateMeasureResponseDto> {
  const { data } = await apiClient.post<CreateMeasureResponseDto>("/api/analytics/measures", payload);
  return data;
}

export async function getAnalyticsModel(id: string): Promise<Record<string, unknown>> {
  const { data } = await apiClient.get<Record<string, unknown>>(`/api/analytics/models/${id}`);
  return data;
}

export async function validateAnalyticsModel(id: string): Promise<ValidateAnalyticsModelResponseDto> {
  const { data } = await apiClient.post<ValidateAnalyticsModelResponseDto>(`/api/analytics/models/${id}/validate`);
  return data;
}

