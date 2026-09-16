import React, { useState } from "react";

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
          { columnPattern: "*_date", operation: "ParseDateUtc" },
          { columnPattern: "amount", operation: "StripCurrency" },
          { columnPattern: "*", operation: "TrimWhitespace" }
        ]
  );

  const addRule = () => {
    setRules([...rules, { columnPattern: "", operation: "TrimWhitespace" }]);
  };

  const removeRule = (index: number) => {
    setRules(rules.filter((_, i) => i !== index));
  };

  return (
    <div className="bg-white border rounded-lg p-5 space-y-4">
      <div className="flex justify-between items-center border-b pb-3">
        <h3 className="text-lg font-semibold text-gray-900">Declarative Cleaning Rules</h3>
        <button
          onClick={addRule}
          className="px-3 py-1 bg-blue-600 text-white text-xs rounded hover:bg-blue-700"
        >
          + Add Rule
        </button>
      </div>

      <div className="space-y-2">
        {rules.map((rule, idx) => (
          <div key={idx} className="flex gap-2 items-center">
            <input
              type="text"
              placeholder="Column pattern (e.g. *_date)"
              value={rule.columnPattern}
              onChange={(e) => {
                const updated = [...rules];
                updated[idx].columnPattern = e.target.value;
                setRules(updated);
              }}
              className="border px-2 py-1 text-sm rounded flex-1"
            />
            <select
              value={rule.operation}
              onChange={(e) => {
                const updated = [...rules];
                updated[idx].operation = e.target.value as any;
                setRules(updated);
              }}
              className="border px-2 py-1 text-sm rounded"
            >
              <option value="TrimWhitespace">Trim Whitespace</option>
              <option value="NormalizeCasing">Normalize Casing</option>
              <option value="ParseDateUtc">Parse Date (UTC)</option>
              <option value="StripCurrency">Strip Currency</option>
              <option value="DefaultOnNull">Default On Null</option>
              <option value="ClipOutliersIqr">Clip Outliers (IQR)</option>
            </select>
            <button
              onClick={() => removeRule(idx)}
              className="text-red-500 hover:text-red-700 text-sm px-2"
            >
              ✕
            </button>
          </div>
        ))}
      </div>

      <div className="flex justify-end pt-3">
        <button
          onClick={() => onSaveRules?.(rules)}
          className="px-4 py-1.5 bg-gray-900 text-white text-xs rounded hover:bg-black"
        >
          Apply Cleaning Rules
        </button>
      </div>
    </div>
  );
};

