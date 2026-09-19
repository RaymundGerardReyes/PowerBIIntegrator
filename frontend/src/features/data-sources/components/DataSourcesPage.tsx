import React, { useState } from "react";
import { Link } from "react-router-dom";
import { ExcelUploadForm } from "./ExcelUploadForm";
import { CsvUploadForm } from "./CsvUploadForm";
import { SqlConnectionForm } from "./SqlConnectionForm";
import { DataTable, EmptyState } from "@shared/ui";
import { useDataSources } from "../hooks/useDataSources";
import type { ColumnSchemaDto } from "@shared/types/api-contracts";

type Tab = "excel" | "csv" | "sql" | "catalog" | null;

export const DataSourcesPage: React.FC = () => {
  const [activeTab, setActiveTab] = useState<Tab>("excel");
  const [inspectedSchema, setInspectedSchema] = useState<ColumnSchemaDto[] | null>(null);

  const { data: realSources } = useDataSources();
  const registeredSources: Record<string, unknown>[] = (realSources && realSources.length > 0)
    ? realSources.map((s) => ({
        id: s.id,
        name: s.name,
        type: String(s.type),
        tablesCount: s.schema?.length ? 1 : 0,
        status: "Active",
        action: (
          <Link
            to={`/dashboards?dataset=${encodeURIComponent(s.name)}`}
            className="btn btn-secondary btn-sm"
            style={{ textDecoration: "none", fontSize: "0.75rem", padding: "0.25rem 0.5rem" }}
          >
            Open Dashboard →
          </Link>
        )
      }))
    : [
        { id: "ds-1", name: "Global_Sales_2026.xlsx", type: "Excel", status: "Active", tablesCount: 3, action: null },
        { id: "ds-2", name: "Customer_Churn_Monthly.csv", type: "CSV", status: "Active", tablesCount: 1, action: null },
        { id: "ds-3", name: "TelemetryDb@sql-cluster-01", type: "SQL", status: "Active", tablesCount: 2, action: null }
      ];

  const schemaColumns = [
    { key: "name" as const, header: "Column Name" },
    { key: "dataType" as const, header: "Data Type" },
    { key: "isNullable" as const, header: "Nullable" },
    { key: "sampleValues" as const, header: "Sample Values" }
  ];

  const schemaRows: Record<string, unknown>[] = (inspectedSchema ?? []).map((col) => ({
    name: col.name,
    dataType: col.inferredType ?? col.dataType ?? "String",
    isNullable: col.isNullable ? "Yes" : "No",
    sampleValues: col.sampleValues?.join(", ") || "None"
  }));

  const sourceTypes = [
    { id: "excel" as Tab, label: "Excel Spreadsheets (.xlsx)", icon: "📊", desc: "Local spreadsheet files" },
    { id: "csv" as Tab, label: "Delimited CSV (.csv)", icon: "📄", desc: "Delimited text data" },
    { id: "sql" as Tab, label: "Relational Database", icon: "🗄️", desc: "Relational DB connection" },
    { id: "catalog" as Tab, label: "Registered Catalog", icon: "📁", desc: "Existing connections" }
  ];

  return (
    <div style={{ display: "flex", flexDirection: "column", gap: "2rem" }}>
      <div>
        <h2 style={{ marginBottom: "0.25rem", fontSize: "1.25rem" }}>Data Sources & Ingestion Hub</h2>
        <p style={{ margin: 0, color: "var(--text-secondary)", fontSize: "0.875rem" }}>
          Configure enterprise data sources to extract schemas and ingest data into the medallion architecture.
        </p>
      </div>

      <div role="tablist" style={{ display: "flex", gap: "1rem", flexWrap: "wrap" }}>
        {sourceTypes.map(source => (
          <div
            key={source.id}
            role="tab"
            aria-selected={activeTab === source.id}
            aria-label={source.label}
            onClick={() => setActiveTab(source.id)}
            style={{
              flex: "1 1 200px",
              padding: "1.5rem",
              borderRadius: "var(--radius-md)",
              border: `1px solid ${activeTab === source.id ? "var(--primary)" : "var(--border-color)"}`,
              backgroundColor: activeTab === source.id ? "var(--primary-tint)" : "var(--bg-surface)",
              cursor: "pointer",
              display: "flex",
              flexDirection: "column",
              gap: "0.5rem",
              transition: "all 0.15s ease",
              boxShadow: activeTab === source.id ? "var(--shadow-sm)" : "none"
            }}
          >
            <span style={{ fontSize: "1.5rem" }}>{source.icon}</span>
            <span style={{ fontWeight: 600, fontSize: "0.95rem" }}>{source.label}</span>
            <span style={{ fontSize: "0.75rem", color: "var(--text-secondary)" }}>{source.desc}</span>
          </div>
        ))}
      </div>

      {activeTab === null && (
        <EmptyState
          title="No Data Source Selected"
          description="Select a data source type above to begin configuration. You can upload local files or connect directly to an enterprise database."
        />
      )}

      {activeTab !== null && (
        <div className="card" style={{ padding: "1.5rem" }}>
          {activeTab === "excel" && (
            <div>
              <h3 style={{ fontSize: "1rem", marginBottom: "0.25rem" }}>Configure Excel Source</h3>
              <p style={{ fontSize: "0.875rem", color: "var(--text-secondary)", marginBottom: "1.5rem" }}>
                Select a local .xlsx file. The system will detect worksheets, headers, and column data types.
              </p>
              <ExcelUploadForm />
            </div>
          )}

          {activeTab === "csv" && (
            <div>
              <h3 style={{ fontSize: "1rem", marginBottom: "0.25rem" }}>Configure CSV Source</h3>
              <p style={{ fontSize: "0.875rem", color: "var(--text-secondary)", marginBottom: "1.5rem" }}>
                Upload delimited text data. Infers column data types, delimiters, and generates TMDL semantic model tables.
              </p>
              <CsvUploadForm />
            </div>
          )}

          {activeTab === "sql" && (
            <div>
              <h3 style={{ fontSize: "1rem", marginBottom: "0.25rem" }}>Register Database Connection</h3>
              <p style={{ fontSize: "0.875rem", color: "var(--text-secondary)", marginBottom: "1.5rem" }}>
                Enter connection details to introspect database tables and foreign keys.
              </p>
              <SqlConnectionForm onConnected={(schema) => setInspectedSchema(schema)} />
            </div>
          )}

          {activeTab === "catalog" && (
            <div>
              <h3 style={{ fontSize: "1rem", marginBottom: "0.25rem" }}>Registered Data Sources</h3>
              <p style={{ fontSize: "0.875rem", color: "var(--text-secondary)", marginBottom: "1.5rem" }}>
                Sources currently registered and available for semantic model compilation.
              </p>
              <DataTable
                columns={[
                  { key: "name" as const, header: "Source Name" },
                  { key: "type" as const, header: "Connector Type" },
                  { key: "tablesCount" as const, header: "Tables / Sheets" },
                  { key: "status" as const, header: "Health Status" },
                  { key: "action" as const, header: "Action" }
                ]}
                rows={registeredSources}
              />
            </div>
          )}
        </div>
      )}

      {inspectedSchema && inspectedSchema.length > 0 && (
        <div className="card">
          <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: "1rem" }}>
            <h4 style={{ margin: 0, fontSize: "0.95rem" }}>Extracted Schema Preview ({inspectedSchema.length} columns)</h4>
            <button
              onClick={() => setInspectedSchema(null)}
              className="btn btn-secondary btn-sm"
            >
              Clear Preview
            </button>
          </div>
          <DataTable columns={schemaColumns} rows={schemaRows} />
        </div>
      )}
    </div>
  );
};

