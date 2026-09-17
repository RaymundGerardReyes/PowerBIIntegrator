import React from "react";

export interface Column<T> {
  key: keyof T;
  header: string;
}

export interface DataTableProps<T> {
  columns: Column<T>[];
  rows: T[];
  className?: string;
}

export function DataTable<T extends Record<string, unknown>>({
  columns,
  rows,
  className = ""
}: DataTableProps<T>) {
  return (
    <div
      style={{
        overflowX: "auto",
        borderRadius: "var(--radius-md)",
        border: "1px solid var(--border-color)",
        backgroundColor: "var(--bg-card)",
        boxShadow: "var(--shadow-xs)"
      }}
      className={className}
    >
      <table>
        <thead>
          <tr>
            {columns.map((col) => (
              <th key={String(col.key)}>{col.header}</th>
            ))}
          </tr>
        </thead>
        <tbody>
          {rows.length === 0 ? (
            <tr>
              <td colSpan={columns.length} style={{ textAlign: "center", color: "var(--text-muted)", padding: "2rem" }}>
                No records found.
              </td>
            </tr>
          ) : (
            rows.map((row, idx) => (
              <tr key={idx}>
                {columns.map((col) => (
                  <td key={String(col.key)}>{String(row[col.key] ?? "")}</td>
                ))}
              </tr>
            ))
          )}
        </tbody>
      </table>
    </div>
  );
}
