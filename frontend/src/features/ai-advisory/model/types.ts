export interface AdvisoryQueryRequest {
  runId: string;
  questionType: string;
  userQuestion: string;
  userId?: string;
  userRole?: string;
  unlockConfidential?: boolean;
  correlationId?: string;
}

export interface AdvisoryResultDto {
  id: string;
  runId: string;
  questionType: string;
  userQuestion: string;
  answer: string;
  providerUsed: string;
  citedRuleIds: string[];
  citedRunIds: string[];
  redactedFieldsCount: number;
  isUnlockedConfidential: boolean;
  correlationId: string;
  timestampUtc: string;
}

export interface AdvisoryPolicyDto {
  policyName: string;
  allowCloudProvider: boolean;
  maxExposureLevel: string;
  allowedTools: string[];
  requireHumanApprovalForConfidential: boolean;
}

export interface UnlockConfidentialRequest {
  runId: string;
  userId: string;
  reason: string;
  userRole: string;
}

export interface UnlockConfidentialResultDto {
  success: boolean;
  token: string;
  message: string;
  expiresAtUtc: string;
}

