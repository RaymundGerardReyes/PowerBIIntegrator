/**
 * Validation utilities for semantic model measures and field labels.
 * Strictly adheres to the Semantic Model Measure Parity invariant and
 * FULLSTACK_ARCHITECTURE_VALIDATION_GUIDE.md.
 */

/**
 * Extracts the raw field or measure name from a table-qualified identifier.
 * e.g. "Table[Column]" -> "Column", "Sales[TotalRevenue]" -> "TotalRevenue".
 */
export function cleanFieldLabel(field: string): string {
  if (!field) return "";
  if (field.includes("[")) {
    return field.substring(field.indexOf("[") + 1).replace("]", "").trim();
  }
  return field.trim();
}

/**
 * Asserts whether a given field/measure name conforms to the measure naming rules.
 * KPI single-value visuals and Cards MUST bind only to valid DAX measures.
 * Valid measures must start with "Total", "Sum", "Average", "Avg", end with "_Rate"/"Rate"/"_Pct",
 * or equal "TotalRows". Raw unaggregated columns (e.g., "Revenue", "Amount", "OrderId") are rejected.
 */
export function isValidMeasureName(name: string): boolean {
  if (!name) return false;
  const clean = cleanFieldLabel(name);
  const lower = clean.toLowerCase();

  // Special cases for default table count measure
  if (lower === "totalrows" || lower === "total_rows" || lower.startsWith("totalrows_")) {
    return true;
  }

  // Measures starting with Total, Sum, Average, Avg
  if (
    lower.startsWith("total_") ||
    lower.startsWith("total") ||
    lower.startsWith("sum_") ||
    lower.startsWith("sum") ||
    lower.startsWith("average_") ||
    lower.startsWith("average") ||
    lower.startsWith("avg_") ||
    lower.startsWith("avg")
  ) {
    return true;
  }

  // Measures ending with rate or pct
  if (
    lower.endsWith("_rate") ||
    lower.endsWith("rate") ||
    lower.endsWith("_pct") ||
    lower.endsWith("_percent") ||
    lower.endsWith("percentage")
  ) {
    return true;
  }

  return false;
}

