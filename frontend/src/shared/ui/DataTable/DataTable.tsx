import React from "react";

interface Column<T> {
  key: keyof T;
  header: string;
}

interface DataTableProps<T> {
  columns: Column<T>[];
  rows: T[];
}

export function DataTable<T extends Record<string, unknown>>({ columns, rows }: DataTableProps<T>) {
  return (
    <table>
      <thead>
        <tr>
          {columns.map((col) => (
            <th key={String(col.key)}>{col.header}</th>
          ))}
        </tr>
      </thead>
      <tbody>
        {rows.map((row, idx) => (
          <tr key={idx}>
            {columns.map((col) => (
              <td key={String(col.key)}>{String(row[col.key])}</td>
            ))}
          </tr>
        ))}
      </tbody>
    </table>
  );
}
