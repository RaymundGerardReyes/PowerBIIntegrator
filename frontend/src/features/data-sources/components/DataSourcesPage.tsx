import React, { useState } from "react";
import { ExcelUploadForm } from "./ExcelUploadForm";
import { CsvUploadForm } from "./CsvUploadForm";
import { SqlConnectionForm } from "./SqlConnectionForm";
import { DataTable } from "@shared/ui/DataTable/DataTable";
import type { ColumnSchemaDto } from "@shared/types/api-contracts";

type Tab = "excel" | "csv" | "sql" | "catalog";

export const DataSourcesPage: React.FC = () => {
  const [activeTab, setActiveTab] = useState<Tab>("excel");
  const [inspectedSchema, setInspectedSchema] = useState<ColumnSchemaDto[] | null>(null);

  // Sample catalog of pre-connected data sources
  const [registeredSources] = useState([
    { id: "ds-1", name: "Global_Sales_2026.xlsx", type: "Excel", status: "Active", tablesCount: 3 },
    { id: "ds-2", name: "Customer_Churn_Monthly.csv", type: "CSV", status: "Active", tablesCount: 1 },
    { id: "ds-3", name: "TelemetryDb@sql-cluster-01", type: "PostgreSql", status: "Connected", tablesCount: 12 }
  ]);

  const schemaColumns = [
    { key: "name" as const, header: "Column Name" },
    { key: "dataType" as const, header: "Data Type" },
    { key: "isNullable" as const, header: "Nullable" },
    { key: "sampleValues" as const, header: "Sample Inferred Values" }
  ];

  const schemaRows: Record<string, unknown>[] = (inspectedSchema ?? []).map((col) => ({
    name: col.name,
    dataType: col.dataType,
    isNullable: col.isNullable ? "Yes" : "No",
    sampleValues: col.sampleValues?.join(", ") || "None"
  }));

  return (
    <div style={{ display: "flex", flexDirection: "column", gap: "1.5rem" }}>
      <div>
        <h2 style={{ marginBottom: "0.25rem" }}>Data Sources & Ingestion Hub</h2>
        <p style={{ margin: 0, color: "var(--text-secondary)" }}>
          Ingest spreadsheet files or register enterprise SQL/PostgreSQL databases to extract TMDL schemas and Power BI analytical models.
        </p>
      </div>

      <div className="tab-list" role="tablist">
        <button
          className={`tab-item ${activeTab === "excel" ? "tab-item-active" : ""}`}
          onClick={() => setActiveTab("excel")}
          role="tab"
          aria-selected={activeTab === "excel"}
        >
          Excel Spreadsheets (.xlsx)
        </button>
        <button
          className={`tab-item ${activeTab === "csv" ? "tab-item-active" : ""}`}
          onClick={() => setActiveTab("csv")}
          role="tab"
          aria-selected={activeTab === "csv"}
        >
          Delimited CSV (.csv)
        </button>
        <button
          className={`tab-item ${activeTab === "sql" ? "tab-item-active" : ""}`}
          onClick={() => setActiveTab("sql")}
          role="tab"
          aria-selected={activeTab === "sql"}
        >
          Relational Database (SQL/Postgres)
        </button>
        <button
          className={`tab-item ${activeTab === "catalog" ? "tab-item-active" : ""}`}
          onClick={() => setActiveTab("catalog")}
          role="tab"
          aria-selected={activeTab === "catalog"}
        >
          Registered Catalog ({registeredSources.length})
        </button>
      </div>

      <div className="card">
        {activeTab === "excel" && (
          <div>
            <h3 style={{ fontSize: "1.125rem", marginBottom: "0.5rem" }}>Upload Excel Workbook</h3>
            <p style={{ fontSize: "0.875rem", marginBottom: "1rem" }}>
              Extracts worksheet structures, headers, and cell types using ClosedXML server-side parser.
            </p>
            <ExcelUploadForm />
          </div>
        )}

        {activeTab === "csv" && (
          <div>
            <h3 style={{ fontSize: "1.125rem", marginBottom: "0.5rem" }}>Upload Delimited CSV</h3>
            <p style={{ fontSize: "0.875rem", marginBottom: "1rem" }}>
              Infers column data types, delimiters, and nullability distributions via streaming reader.
            </p>
            <CsvUploadForm />
          </div>
        )}

        {activeTab === "sql" && (
          <div>
            <h3 style={{ fontSize: "1.125rem", marginBottom: "0.5rem" }}>Register Database Connection</h3>
            <p style={{ fontSize: "0.875rem", marginBottom: "1rem" }}>
              Connects to Microsoft SQL Server, PostgreSQL, or MySQL to introspect schemas and foreign key topologies.
            </p>
            <SqlConnectionForm onConnected={(schema) => setInspectedSchema(schema)} />
          </div>
        )}

        {activeTab === "catalog" && (
          <div>
            <h3 style={{ fontSize: "1.125rem", marginBottom: "0.5rem" }}>Active Data Sources</h3>
            <p style={{ fontSize: "0.875rem", marginBottom: "1rem" }}>
              Sources registered in the metadata repository ready for semantic model compilation.
            </p>
            <DataTable
              columns={[
                { key: "name" as const, header: "Source Name" },
                { key: "type" as const, header: "Connector Type" },
                { key: "tablesCount" as const, header: "Tables / Sheets" },
                { key: "status" as const, header: "Health Status" }
              ]}
              rows={registeredSources}
            />
          </div>
        )}
      </div>

      {inspectedSchema && inspectedSchema.length > 0 && (
        <div className="card">
          <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: "0.75rem" }}>
            <h4 style={{ margin: 0 }}>Live Extracted Schema Preview ({inspectedSchema.length} columns)</h4>
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

