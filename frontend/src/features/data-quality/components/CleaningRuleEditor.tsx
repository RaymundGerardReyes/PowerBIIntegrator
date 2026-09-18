import React, { useState } from "react";
import { Button } from "@shared/ui";

export interface CleaningRuleItem {
  columnPattern: string;
  operation: "TrimWhitespace" | "NormalizeCasing" | "ParseDateUtc" | "StripCurrency" | "DefaultOnNull" | "ClipOutliersIqr";
  ruleParameter?: string;
}

interface CleaningRuleEditorProps {
  initialRules?: CleaningRuleItem[];
  onSaveRules?: (rules: CleaningRuleItem[]) => void;
}

export const CleaningRuleEditor: React.FC<CleaningRuleEditorProps> = ({ initialRules = [], onSaveRules }) => {
  const [rules, setRules] = useState<CleaningRuleItem[]>(
    initialRules.length > 0
      ? initialRules
      : [
          { columnPattern: "transaction_date", operation: "ParseDateUtc" },
          { columnPattern: "amount", operation: "StripCurrency" },
          { columnPattern: "category", operation: "TrimWhitespace" }
        ]
  );

  const addRule = () => {
    setRules([...rules, { columnPattern: "", operation: "TrimWhitespace" }]);
  };

  const removeRule = (index: number) => {
    setRules(rules.filter((_, i) => i !== index));
  };

  const getPreview = (rule: CleaningRuleItem) => {
    if (rule.operation === "ParseDateUtc") return '"08/14/2026" → 2026-08-14T00:00:00Z';
    if (rule.operation === "StripCurrency") return '"$1,250.50" → 1250.50';
    if (rule.operation === "TrimWhitespace") return '"   Retail   " → "Retail"';
    return "Preview not available";
  };

  const getStatus = (rule: CleaningRuleItem) => {
    if (rule.operation === "ParseDateUtc") return "✓ 12,481 rows affected";
    if (rule.operation === "StripCurrency") return "✓ 12,479 rows affected";
    if (rule.operation === "TrimWhitespace") return "✓ 8,241 rows affected";
    return "— Needs evaluation";
  };

  return (
    <div className="card" style={{ display: "flex", flexDirection: "column", gap: "1rem" }}>
      <div style={{ borderBottom: "1px solid var(--border-color)", paddingBottom: "1rem", display: "flex", justifyContent: "space-between", alignItems: "center" }}>
        <div>
          <h3 style={{ margin: 0, fontSize: "1rem" }}>Declarative Cleaning Rules</h3>
          <p style={{ margin: "0.25rem 0 0 0", fontSize: "0.8125rem", color: "var(--text-secondary)" }}>
            Configure deterministic cleaning operations to standardize data before downstream transformation.
          </p>
        </div>
        <Button variant="secondary" className="btn-sm" onClick={addRule}>+ Add Cleaning Rule</Button>
      </div>

      <div style={{ display: "flex", flexDirection: "column", gap: "1.5rem" }}>
        {rules.map((rule, idx) => (
          <div key={idx} style={{ border: "1px solid var(--border-color)", borderRadius: "var(--radius-md)", padding: "1rem", backgroundColor: "var(--bg-surface)" }}>
            <div style={{ display: "flex", justifyContent: "space-between", marginBottom: "0.75rem" }}>
              <div style={{ fontSize: "0.75rem", fontWeight: 700, color: "var(--text-muted)", textTransform: "uppercase" }}>Rule {String(idx + 1).padStart(2, '0')}</div>
              <button onClick={() => removeRule(idx)} style={{ background: "none", border: "none", color: "var(--text-muted)", cursor: "pointer", fontSize: "0.875rem" }}>✕ Remove</button>
            </div>
            
            <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "1rem", marginBottom: "1rem" }}>
              <div className="form-group">
                <label className="form-label">Column Match</label>
                <input
                  type="text"
                  className="form-input"
                  value={rule.columnPattern}
                  onChange={(e) => {
                    const updated = [...rules];
                    updated[idx].columnPattern = e.target.value;
                    setRules(updated);
                  }}
                  placeholder="e.g. amount, *_date"
                />
              </div>
              <div className="form-group">
                <label className="form-label">Operation</label>
                <select
                  className="form-select"
                  value={rule.operation}
                  onChange={(e) => {
                    const updated = [...rules];
                    updated[idx].operation = e.target.value as any;
                    setRules(updated);
                  }}
                >
                  <option value="TrimWhitespace">Trim Whitespace</option>
                  <option value="NormalizeCasing">Normalize Casing</option>
                  <option value="ParseDateUtc">Parse Date (UTC)</option>
                  <option value="StripCurrency">Strip Currency</option>
                  <option value="DefaultOnNull">Default On Null</option>
                  <option value="ClipOutliersIqr">Clip Outliers (IQR)</option>
                </select>
              </div>
            </div>

            <div style={{ backgroundColor: "var(--bg-subtle)", padding: "0.75rem", borderRadius: "var(--radius-sm)", display: "flex", justifyContent: "space-between", alignItems: "center" }}>
              <div>
                <span style={{ fontSize: "0.75rem", fontWeight: 600, color: "var(--text-secondary)", display: "block", marginBottom: "0.25rem" }}>PREVIEW</span>
                <code style={{ fontSize: "0.875rem", color: "var(--text-primary)" }}>{getPreview(rule)}</code>
              </div>
              <div style={{ textAlign: "right" }}>
                <span style={{ fontSize: "0.75rem", fontWeight: 600, color: "var(--text-secondary)", display: "block", marginBottom: "0.25rem" }}>STATUS</span>
                <span style={{ fontSize: "0.8125rem", color: rule.columnPattern ? "var(--success)" : "var(--text-muted)", fontWeight: 500 }}>
                  {getStatus(rule)}
                </span>
              </div>
            </div>
          </div>
        ))}
      </div>

      <div style={{ borderTop: "1px solid var(--border-color)", paddingTop: "1rem", display: "flex", justifyContent: "flex-end", gap: "0.5rem" }}>
        <Button variant="secondary">Preview Changes</Button>
        <Button variant="primary" onClick={() => onSaveRules?.(rules)}>Apply Rules</Button>
      </div>
    </div>
  );
};

