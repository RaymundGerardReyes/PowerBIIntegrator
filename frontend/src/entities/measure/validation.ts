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

/**
 * Asserts whether a field is considered a dimension/categorical column rather than a DAX measure.
 */
export function isDimensionCandidate(name: string): boolean {
  if (!name) return false;
  return !isValidMeasureName(name);
}

export interface VisualRoleValidationResult {
  isValid: boolean;
  error?: string;
  warning?: string;
}

/**
 * Validates data roles for a given visual type per ADR 0005.
 * Strictly guarantees that metric slots bind to valid measures, while category slots
 * prefer dimensions.
 */
export function validateVisualRoles(visualType: string, boundFields: string[]): VisualRoleValidationResult {
  const normType = (visualType || "").toLowerCase();

  // Single-value visuals and KPI cards: strictly DAX measure
  if (normType === "card") {
    const field = boundFields[0];
    if (!field) {
      return { isValid: false, error: "A KPI Card requires at least one bound measure field." };
    }
    const clean = cleanFieldLabel(field);
    if (!isValidMeasureName(clean)) {
      return {
        isValid: false,
        error: `Raw unaggregated column '${clean}' cannot be used in a KPI Card. Please bind to a DAX measure (e.g. Total_${clean} or TotalRows).`
      };
    }
    return { isValid: true };
  }

  // Bar and Column charts: Slot 0 = Category (dimension), Slot 1 = Value (measure)
  if (normType === "barchart" || normType === "columnchart") {
    if (boundFields.length === 0) {
      return { isValid: true };
    }
    const cat = cleanFieldLabel(boundFields[0]);
    let warning: string | undefined;
    if (isValidMeasureName(cat)) {
      warning = `Dimension Expected: Measure '${cat}' bound to Category axis.`;
    }

    if (boundFields.length > 1) {
      const metric = cleanFieldLabel(boundFields[1]);
      if (!isValidMeasureName(metric)) {
        return {
          isValid: false,
          error: `Raw unaggregated column '${metric}' cannot be bound to the Value (Y) axis. Please bind to a DAX measure (e.g. Total_${metric} or TotalRows).`,
          warning
        };
      }
    }
    return { isValid: true, warning };
  }

  // Line and Area charts: Slot 0 = Timeline/Category, Slot 1 = Metric (measure)
  if (normType === "linechart" || normType === "areachart") {
    if (boundFields.length === 0) {
      return { isValid: true };
    }
    if (boundFields.length > 1) {
      const metric = cleanFieldLabel(boundFields[1]);
      if (!isValidMeasureName(metric)) {
        return {
          isValid: false,
          error: `Raw unaggregated column '${metric}' cannot be bound to the Y axis. Please bind to a DAX measure (e.g. Total_${metric} or TotalRows).`
        };
      }
    }
    return { isValid: true };
  }

  // Donut and Pie charts: Slot 0 = Category, Slot 1 = Slice Metric (measure)
  if (normType === "donutchart" || normType === "piechart") {
    if (boundFields.length > 1) {
      const metric = cleanFieldLabel(boundFields[1]);
      if (!isValidMeasureName(metric)) {
        return {
          isValid: false,
          error: `Raw unaggregated column '${metric}' cannot be bound to the slice value. Please bind to a DAX measure.`
        };
      }
    }
    return { isValid: true };
  }

  // Table grid: accepts both columns and measures in Values role
  if (normType === "table" || normType === "tableex") {
    return { isValid: true };
  }

  return { isValid: true };
}
