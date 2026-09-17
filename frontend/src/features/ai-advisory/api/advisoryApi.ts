import { apiClient } from "@shared/lib/http/apiClient";
import type {
  AdvisoryQueryRequest,
  AdvisoryResultDto,
  AdvisoryPolicyDto,
  UnlockConfidentialRequest,
  UnlockConfidentialResultDto
} from "../model/types";

export async function queryAdvisory(request: AdvisoryQueryRequest): Promise<AdvisoryResultDto> {
  const { data } = await apiClient.post<AdvisoryResultDto>("/api/advisory/query", request);
  return data;
}

export async function getAdvisoryPolicies(): Promise<AdvisoryPolicyDto[]> {
  const { data } = await apiClient.get<AdvisoryPolicyDto[]>("/api/advisory/policies");
  return data;
}

export async function unlockConfidentialExposure(
  request: UnlockConfidentialRequest
): Promise<UnlockConfidentialResultDto> {
  const { data } = await apiClient.post<UnlockConfidentialResultDto>("/api/advisory/unlock", request);
  return data;
}

