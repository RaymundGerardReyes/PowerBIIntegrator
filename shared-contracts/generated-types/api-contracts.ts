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

