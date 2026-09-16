import type {
  LlmProviderPreference,
  SensitivityLevelDto,
  RunLlmTaskRequest,
  LlmTaskResultDto,
  LlmPolicyDto
} from "@shared/types/api-contracts";

export type {
  LlmProviderPreference,
  SensitivityLevelDto,
  RunLlmTaskRequest,
  LlmTaskResultDto,
  LlmPolicyDto
};

export interface ChatMessage {
  id: string;
  sender: "user" | "assistant" | "system";
  text: string;
  timestamp: string;
  providerUsed?: string;
  guardrailNotice?: string;
  isBlocked?: boolean;
}
