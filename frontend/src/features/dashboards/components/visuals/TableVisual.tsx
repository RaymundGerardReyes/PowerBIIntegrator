import React from "react";
import type { Visual } from "@entities/visual/types";
import { cleanFieldLabel, isValidMeasureName } from "@entities/measure";

interface TableVisualProps {
  visual: Visual;
}

function getSampleCellValue(header: string, rowIndex: number): string {
  const h = header.toLowerCase();

  if (h === "pclass" || h === "class" || h === "tier") {
    const vals = ["1", "1", "2", "3", "3"];
    return vals[rowIndex % vals.length];
  }

  if (h.includes("name")) {
    const vals = [
      "Allen, Miss. Elisabeth Walton",
      "Allison, Master. Hudson Trevor",
      "Baxter, Mrs. James (Helene)",
      "Behr, Mr. Karl Howell",
      "Birnbaum, Mr. Jakob"
    ];
    return vals[rowIndex % vals.length];
  }

  if (h === "sex" || h === "gender") {
    const vals = ["female", "male", "female", "male", "male"];
    return vals[rowIndex % vals.length];
  }

  if (h.includes("age")) {
    const vals = ["29", "2", "50", "26", "25"];
    return vals[rowIndex % vals.length];
  }

  if (h === "sibsp" || h.includes("sibling")) {
    const vals = ["0", "1", "0", "0", "0"];
    return vals[rowIndex % vals.length];
  }

  if (h === "parch" || h.includes("parent")) {
    const vals = ["0", "2", "1", "0", "0"];
    return vals[rowIndex % vals.length];
  }

  if (h.includes("ticket")) {
    const vals = ["24160", "113781", "PC 17558", "111369", "13905"];
    return vals[rowIndex % vals.length];
  }

  if (h.includes("fare") || h.includes("price") || h.includes("cost")) {
    const vals = ["$211.34", "$151.55", "$247.52", "$30.00", "$13.00"];
    return vals[rowIndex % vals.length];
  }

  if (h.includes("cabin") || h.includes("room")) {
    const vals = ["B5", "C22 C26", "B58 B60", "C148", "—"];
    return vals[rowIndex % vals.length];
  }

  if (h.includes("embark") || h.includes("port")) {
    const vals = ["Southampton", "Southampton", "Cherbourg", "Cherbourg", "Queenstown"];
    return vals[rowIndex % vals.length];
  }

  if (h.includes("surviv") || h.includes("target")) {
    const vals = ["1", "1", "1", "1", "0"];
    return vals[rowIndex % vals.length];
  }

  if (h.includes("revenue") || h.includes("sales") || h.includes("amount")) {
    const vals = ["$12,450", "$8,230", "$19,100", "$4,500", "$7,800"];
    return vals[rowIndex % vals.length];
  }

  if (h.includes("date")) {
    const vals = ["2026-01-15", "2026-02-20", "2026-03-12", "2026-04-05", "2026-05-18"];
    return vals[rowIndex % vals.length];
  }

  if (h.includes("status")) {
    const vals = ["Active", "Active", "Pending", "Closed", "Active"];
    return vals[rowIndex % vals.length];
  }

  if (h.includes("count") || h.includes("qty") || h.includes("quantity")) {
    const vals = ["14", "8", "22", "6", "9"];
    return vals[rowIndex % vals.length];
  }

  const fallbacks = ["Val Alpha", "Val Beta", "Val Gamma", "Val Delta", "Val Epsilon"];
  return fallbacks[rowIndex % fallbacks.length];
}

export const TableVisual: React.FC<TableVisualProps> = ({ visual }) => {
  const fields = visual.boundFields.map((f) => cleanFieldLabel(f));

  const sampleHeaders = fields.length > 0 ? fields.slice(0, 8) : ["Column1", "Column2", "Column3", "Measure"];

  // 5 realistic sample rows mapped by column domain
  const rowIndices = [0, 1, 2, 3, 4];
  const sampleRows = rowIndices.map((rIdx) =>
    sampleHeaders.map((header) => getSampleCellValue(header, rIdx))
  );

  return (
    <div
      style={{
        display: "flex",
        flexDirection: "column",
        height: "100%",
        padding: "0.5rem",
        overflow: "hidden",
        boxSizing: "border-box"
      }}
    >
      <div style={{ marginBottom: "0.35rem" }}>
        <span style={{ fontSize: "0.8125rem", fontWeight: 700, color: "var(--text-primary, #111827)" }}>
          Tabular Grid View ({fields.length} Fields)
        </span>
        <p style={{ margin: 0, fontSize: "0.7rem", color: "var(--text-muted, #9ca3af)" }}>
          Source records mapped to PBIR matrix container
        </p>
      </div>

      <div style={{ flex: 1, overflow: "auto", border: "1px solid var(--border-color, #e5e7eb)", borderRadius: "4px" }}>
        <table style={{ width: "100%", borderCollapse: "collapse", fontSize: "0.75rem" }}>
          <thead>
            <tr style={{ backgroundColor: "var(--bg-subtle, #f8fafc)", borderBottom: "1px solid var(--border-color, #e2e8f0)" }}>
              {sampleHeaders.map((h, i) => {
                const isMeasure = isValidMeasureName(h);
                return (
                  <th key={i} style={{ padding: "5px 8px", textAlign: "left", fontWeight: 600, color: "var(--text-secondary, #475569)", whiteSpace: "nowrap" }}>
                    {isMeasure && <span style={{ marginRight: "3px", color: "var(--color-primary, #2563eb)", fontSize: "0.65rem", fontWeight: 700 }}>[fx]</span>}
                    {h}
                  </th>
                );
              })}
            </tr>
          </thead>
          <tbody>
            {sampleRows.map((row, rIdx) => (
              <tr key={rIdx} style={{ borderBottom: "1px solid var(--border-color, #f1f5f9)", backgroundColor: rIdx % 2 === 1 ? "rgba(248, 250, 252, 0.5)" : "transparent" }}>
                {sampleHeaders.map((_, cIdx) => (
                  <td key={cIdx} style={{ padding: "4px 8px", color: "var(--text-primary, #1e293b)", whiteSpace: "nowrap", fontSize: "0.725rem" }}>
                    {row[cIdx] ?? "—"}
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
};
