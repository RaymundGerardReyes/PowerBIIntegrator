import { apiClient } from "@shared/lib/http/apiClient";
import type {
  RunLlmTaskRequest,
  LlmTaskResultDto,
  LlmPolicyDto
} from "../model/types";

export async function runLlmTask(req: RunLlmTaskRequest): Promise<LlmTaskResultDto> {
  const { data } = await apiClient.post<LlmTaskResultDto>("/api/llm/tasks", req);
  return data;
}

export async function getLlmPolicies(): Promise<LlmPolicyDto[]> {
  const { data } = await apiClient.get<LlmPolicyDto[]>("/api/llm/policies");
  return data;
}
