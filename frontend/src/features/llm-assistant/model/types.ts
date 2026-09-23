import type {
  LlmProviderPreference as BaseLlmProviderPreference,
  SensitivityLevelDto,
  RunLlmTaskRequest,
  LlmTaskResultDto,
  LlmPolicyDto
} from "@shared/types/api-contracts";

export type LlmProviderPreference = BaseLlmProviderPreference | "CloudGemini" | "AntigravityGemini";

export type {
  SensitivityLevelDto,
  RunLlmTaskRequest,
  LlmTaskResultDto,
  LlmPolicyDto
};

export interface ToolExecutionDetail {
  toolName: string;
  status: "running" | "completed" | "failed";
  latencyMs?: number;
  args?: Record<string, unknown> | string;
  result?: Record<string, unknown> | string;
}

export type LiveStreamingStatus = "idle" | "listening" | "thinking" | "speaking";
export type DockMode = "docked" | "floating";

export interface ChatMessage {
  id: string;
  sender: "user" | "assistant" | "system";
  text: string;
  timestamp: string;
  providerUsed?: string;
  guardrailNotice?: string;
  isBlocked?: boolean;
  toolDetails?: ToolExecutionDetail[];
}
