/**
 * Auto-generated OpenAPI / API Contracts for Analytics Platform
 * Conforms to shared-contracts/openapi.yaml
 */

export type DataSourceType = "excel" | "csv" | "sqlserver" | "postgresql" | "mysql";

export interface ColumnSchemaDto {
  ordinal?: number;
  name: string;
  dataType?: string;
  inferredType?: string;
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

export interface AnalyticsModelDto {
  id: string;
  name: string;
  culture: string;
  tables: string[];
  measures: string[];
  relationshipsCount: number;
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
  analyticsModelId?: string;
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

export interface PbirReportDefinitionDto {
  reportId: string;
  files: Record<string, string>;
}

export interface TmdlSemanticModelDto {
  modelName: string;
  tables?: Array<{ name: string; columns?: string[] }>;
  relationships?: Array<{ fromTable: string; toTable: string }>;
  files?: Record<string, string>;
}

export interface PbipCompilationResultDto {
  projectName: string;
  pbirDefinition: PbirReportDefinitionDto;
  tmdlModel: TmdlSemanticModelDto;
  manifestJson?: string;
}

export interface FabricPublishResultDto {
  success: boolean;
  workspaceId: string;
  reportId?: string;
  webUrl?: string;
}

export interface ImportPowerBiArtifactResponseDto {
  importId: string;
  datasetId?: string;
  reportId?: string;
  displayName: string;
  importState: string;
}

export interface LocalPowerBiStatusDto {
  isInstalled: boolean;
  desktopExecutablePath?: string | null;
  isRunning: boolean;
  processId?: number | null;
  analysisServicesPort?: number | null;
  defaultOutputDirectory: string;
  installationType: string;
}

export interface LaunchLocalPowerBiRequest {
  dashboardDefinitionId: string;
  analyticsModelId: string;
  projectName?: string;
  outputDirectory?: string;
}

export interface LaunchProjectResultDto {
  success: boolean;
  projectName: string;
  pbipFilePath: string;
  projectDirectory: string;
  launchedInDesktop: boolean;
  message?: string;
}

export interface OpenLocalPowerBiFolderRequest {
  folderPath?: string;
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

export interface CreateMeasureRequest {
  name: string;
  expression: string;
  tableName: string;
}

export interface CreateMeasureResponseDto {
  id: string;
  name: string;
  expression: string;
  tableName: string;
}

export interface ValidateAnalyticsModelResponseDto {
  modelId: string;
  modelName: string;
  isValid: boolean;
  errors: string[];
  warnings: string[];
  orphanTables: string[];
  detectedCycles: string[];
}

export interface ColumnProfileDto {
  columnName: string;
  inferredType: string;
  totalRowCount: number;
  nullCount: number;
  nullRatio: number;
  distinctCount: number;
  minValue?: string;
  maxValue?: string;
  topValues: string[];
  detectedPatternRegex: string;
  cardinalityClass: "Low" | "Medium" | "High";
}

export interface DatasetProfileDto {
  id: string;
  datasetName: string;
  sourceReference: string;
  totalRows: number;
  profiledAtUtc: string;
  columnProfiles: ColumnProfileDto[];
}

export interface StageRunSummaryDto {
  stageName: string;
  isSuccess: boolean;
  inputRowCount: number;
  outputRowCount: number;
  quarantinedRowCount: number;
  triggeredRules: string[];
  details: string;
}

export interface PipelineRunResultDto {
  id: string;
  runId: string;
  sourceReference: string;
  startedAtUtc: string;
  completedAtUtc: string;
  isSuccess: boolean;
  stageSummaries: StageRunSummaryDto[];
}

export interface ChartSuggestionDto {
  recommendedVisualType: "lineChart" | "barChart" | "scatterPlot" | "table" | string;
  confidenceScore: number;
  reason: string;
}

