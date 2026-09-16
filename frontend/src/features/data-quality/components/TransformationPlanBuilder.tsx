import React, { useState } from "react";

export interface TransformationStepItem {
  order: number;
  operationType: "Filter" | "DeriveColumn" | "Rename" | "Aggregate" | "Join";
  targetColumn: string;
  expressionOrSource: string;
}

interface TransformationPlanBuilderProps {
  onPlanSubmit?: (steps: TransformationStepItem[]) => void;
}

export const TransformationPlanBuilder: React.FC<TransformationPlanBuilderProps> = ({ onPlanSubmit }) => {
  const [steps, setSteps] = useState<TransformationStepItem[]>([
    { order: 1, operationType: "Filter", targetColumn: "Status", expressionOrSource: "Status != 'CANCELLED'" },
    { order: 2, operationType: "DeriveColumn", targetColumn: "NetRevenue", expressionOrSource: "GrossRevenue - DiscountAmount" }
  ]);

  const addStep = () => {
    setSteps([
      ...steps,
      { order: steps.length + 1, operationType: "DeriveColumn", targetColumn: "", expressionOrSource: "" }
    ]);
  };

  return (
    <div className="bg-white border rounded-lg p-5 space-y-4">
      <div className="flex justify-between items-center border-b pb-3">
        <div>
          <h3 className="text-lg font-semibold text-gray-900">Transformation Plan Builder</h3>
          <p className="text-xs text-gray-500">Construct Silver-to-Gold deterministic transformation DAG</p>
        </div>
        <button
          onClick={addStep}
          className="px-3 py-1 bg-blue-600 text-white text-xs rounded hover:bg-blue-700"
        >
          + Add Step
        </button>
      </div>

      <div className="space-y-3">
        {steps.map((step, idx) => (
          <div key={idx} className="border rounded p-3 bg-gray-50 flex items-center gap-3">
            <span className="font-mono text-xs font-semibold text-gray-400">#{step.order}</span>
            <span className="bg-blue-100 text-blue-800 text-xs px-2 py-0.5 rounded font-medium">
              {step.operationType}
            </span>
            <div className="flex-1 text-xs">
              <span className="font-semibold text-gray-800">{step.targetColumn}</span>:{" "}
              <code className="text-gray-600 bg-white px-1.5 py-0.5 rounded border">{step.expressionOrSource}</code>
            </div>
            <button
              onClick={() => setSteps(steps.filter((_, i) => i !== idx))}
              className="text-red-500 hover:text-red-700 text-xs"
            >
              Remove
            </button>
          </div>
        ))}
      </div>

      <div className="flex justify-end pt-3">
        <button
          onClick={() => onPlanSubmit?.(steps)}
          className="px-4 py-1.5 bg-green-600 text-white text-xs rounded hover:bg-green-700"
        >
          Execute Transformation Plan
        </button>
      </div>
    </div>
  );
};

