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
    {
      id: "excel" as Tab,
      label: "Excel Spreadsheets",
      sublabel: ".xlsx",
      desc: "Upload local spreadsheet files with automatic schema detection",
      icon: (
        <svg width="28" height="28" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
          <rect x="3" y="3" width="18" height="18" rx="3" fill="#16a34a" opacity="0.12"/>
          <path d="M7 8l3 4-3 4M13 8h4M13 12h4M13 16h4" stroke="#16a34a" strokeWidth="1.75" strokeLinecap="round"/>
        </svg>
      )
    },
    {
      id: "csv" as Tab,
      label: "Delimited CSV",
      sublabel: ".csv",
      desc: "Import structured text files with auto-detected delimiters",
      icon: (
        <svg width="28" height="28" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
          <rect x="3" y="3" width="18" height="18" rx="3" fill="#6366f1" opacity="0.12"/>
          <path d="M7 8h10M7 12h10M7 16h6" stroke="#6366f1" strokeWidth="1.75" strokeLinecap="round"/>
        </svg>
      )
    },
    {
      id: "sql" as Tab,
      label: "Relational Database",
      sublabel: "SQL",
      desc: "Connect to enterprise databases and introspect schemas",
      icon: (
        <svg width="28" height="28" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
          <rect x="3" y="3" width="18" height="18" rx="3" fill="#0ea5e9" opacity="0.12"/>
          <ellipse cx="12" cy="8" rx="5" ry="2.5" stroke="#0ea5e9" strokeWidth="1.5"/>
          <path d="M7 8v4c0 1.38 2.24 2.5 5 2.5s5-1.12 5-2.5V8M7 12v4c0 1.38 2.24 2.5 5 2.5s5-1.12 5-2.5v-4" stroke="#0ea5e9" strokeWidth="1.5"/>
        </svg>
      )
    },
    {
      id: "catalog" as Tab,
      label: "Registered Catalog",
      sublabel: "Hub",
      desc: "Manage and reuse existing registered data connections",
      icon: (
        <svg width="28" height="28" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
          <rect x="3" y="3" width="18" height="18" rx="3" fill="#f59e0b" opacity="0.12"/>
          <path d="M12 7v5l3 3M12 4a8 8 0 100 16A8 8 0 0012 4z" stroke="#f59e0b" strokeWidth="1.5" strokeLinecap="round"/>
        </svg>
      )
    }
  ];

  return (
    <div style={{ display: "flex", flexDirection: "column", gap: "1.5rem" }}>
      <div>
        <h2 style={{ marginBottom: "0.25rem", fontSize: "1.25rem", letterSpacing: "-0.025em" }}>Data Sources & Ingestion</h2>
        <p style={{ margin: 0, color: "var(--text-muted)", fontSize: "0.875rem" }}>
          Configure enterprise data sources to extract schemas and ingest into the medallion architecture.
        </p>
      </div>

      <div role="tablist" style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(200px, 1fr))", gap: "0.75rem" }}>
        {sourceTypes.map(source => (
          <div
            key={source.id}
            role="tab"
            aria-selected={activeTab === source.id}
            aria-label={source.label}
            onClick={() => setActiveTab(source.id)}
            style={{
              padding: "1.125rem 1.25rem",
              borderRadius: "var(--radius-lg)",
              border: `1.5px solid ${activeTab === source.id ? "var(--primary)" : "var(--border-color)"}`,
              backgroundColor: activeTab === source.id ? "var(--primary-tint)" : "var(--bg-card)",
              cursor: "pointer",
              display: "flex",
              flexDirection: "column",
              gap: "0.625rem",
              transition: "all 0.15s cubic-bezier(0.4, 0, 0.2, 1)",
              boxShadow: activeTab === source.id ? "var(--shadow-primary)" : "var(--shadow-xs)",
              transform: activeTab === source.id ? "translateY(-1px)" : "none"
            }}
          >
            {source.icon}
            <div>
              <div style={{ fontWeight: 600, fontSize: "0.9rem", color: "var(--text-primary)", lineHeight: 1.3 }}>
                {source.label}
                <span style={{ marginLeft: "0.35rem", fontSize: "0.7rem", color: "var(--text-muted)", fontFamily: "monospace" }}>
                  {source.sublabel}
                </span>
              </div>
              <div style={{ fontSize: "0.75rem", color: "var(--text-muted)", marginTop: "0.2rem", lineHeight: 1.4 }}>
                {source.desc}
              </div>
            </div>
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

