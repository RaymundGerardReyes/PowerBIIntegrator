// Generated from shared-contracts/openapi.yaml — regenerate via `npm run gen:contracts`.
export interface CreateMeasureRequest {
  name: string;
  expression: string;
  tableName: string;
}

export interface PublishDashboardRequest {
  dashboardDefinitionId: string;
  targetWorkspaceId: string;
}

export interface PublishDashboardResponse {
  reportId: string;
}
