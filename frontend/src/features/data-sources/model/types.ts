export type DataSourceType = "excel" | "csv" | "sqlserver" | "postgresql" | "mysql";

export interface DataSourceDefinition {
  id: string;
  name: string;
  type: DataSourceType;
  connectionOrPath: string;
}
