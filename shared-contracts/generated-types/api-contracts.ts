/**
 * Auto-generated OpenAPI / API Contracts for Analytics Platform
 * Conforms to shared-contracts/openapi.yaml
 */

export type DataSourceType = "excel" | "csv" | "sqlserver" | "postgresql" | "mysql";

export interface ColumnSchemaDto {
  name: string;
  dataType: string;
  isNullable: boolean;
  sampleValues?: string[];
}

export interface DataSourceDefinitionDto {
  id: string;
  name: string;
  type: DataSourceType | string;
  connectionOrPath: string;
  schema?: ColumnSchemaDto[];
}

export interface RegisterDataSourceRequest {
  name: string;
  type: "Excel" | "Csv" | "SqlServer" | "PostgreSql" | "MySql" | string;
  connectionOrPath: string;
}

export interface ReportKpiDto {
  title: string;
  value: string;
  subtitle?: string;
  deltaPercent?: number;
  isPositiveDelta?: boolean;
}

export interface ReportSectionDto {
  heading: string;
  narrative?: string;
  kpis?: ReportKpiDto[];
  tableHeaders?: string[];
  tableRows?: string[][];
}

export interface ReportDocumentModel {
  title: string;
  subtitle?: string;
  author?: string;
  organization?: string;
  version?: string;
  sections: ReportSectionDto[];
}

export interface CreateMeasureRequest {
  name: string;
  expression: string;
  tableName: string;
}

export interface CreateMeasureResponse {
  id: string;
  name: string;
  expression: string;
  tableName: string;
}

export interface CompilePbirRequest {
  dashboardDefinitionId: string;
  semanticModelRelativePath?: string;
}

export interface CompileTmdlRequest {
  analyticsModelId: string;
}

export interface CompilePbipRequest {
  dashboardDefinitionId: string;
  analyticsModelId: string;
  projectName: string;
}

export interface PublishRequest {
  dashboardDefinitionId: string;
  targetWorkspaceId: string;
}

export interface EmbedConfigDto {
  reportId: string;
  embedUrl: string;
  accessToken: string;
  tokenExpiry: string;
}

export type LlmProviderPreference = "LocalOllama" | "CloudOpenAi" | "CloudAnthropic" | "CloudOllama" | "auto" | string;
export type SensitivityLevelDto = "Public" | "Internal" | "Sensitive" | "Restricted";

export interface RunLlmTaskRequest {
  taskType: string;
  userPrompt: string;
  contextIds?: string[];
  providerPreference: LlmProviderPreference;
  sensitivity?: SensitivityLevelDto;
  policyId: string;
  correlationId: string;
}

export interface LlmTaskResultDto {
  rawText: string;
  providerUsed: string;
  isBlocked: boolean;
  guardrailNotice?: string;
  correlationId: string;
  tokenUsage?: {
    promptTokens: number;
    completionTokens: number;
    totalTokens: number;
    estimatedCostUsd?: number;
  };
}

export interface LlmPolicyDto {
  policyId: string;
  name: string;
  allowCloudProvider: boolean;
  allowSensitiveContext: boolean;
  allowedTools: string[];
  maxTokensPerRequest: number;
  maxDailyTokenBudget: number;
  maximumAllowedSensitivity: SensitivityLevelDto | string;
}

export interface StreamLlmChatRequest {
  userPrompt: string;
  contextIds?: string[];
  providerPreference?: string;
  policyId?: string;
  correlationId?: string;
}
