import React from "react";
import type { ChartSuggestionDto } from "@shared/types/api-contracts";

interface ChartSuggestionPanelProps {
  suggestions: ChartSuggestionDto[];
  onSelectSuggestion?: (visualType: string) => void;
}

export const ChartSuggestionPanel: React.FC<ChartSuggestionPanelProps> = ({ suggestions, onSelectSuggestion }) => {
  if (!suggestions.length) {
    return <div className="p-4 text-gray-400">No chart suggestions available.</div>;
  }

  return (
    <div className="bg-white border rounded-lg p-5 space-y-4">
      <div className="flex justify-between items-center border-b pb-3">
        <div>
          <h3 className="text-lg font-semibold text-gray-900">Recommended Visuals</h3>
          <p className="text-xs text-gray-500">Heuristic column-role mapping (Deterministic, no ML)</p>
        </div>
        <span className="text-xs bg-emerald-50 text-emerald-700 px-2 py-0.5 rounded font-medium">
          PBIR Target Ready
        </span>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
        {suggestions.map((s, idx) => (
          <div key={idx} className="border rounded p-3 bg-gray-50 hover:border-blue-300 transition-colors">
            <div className="flex justify-between items-center mb-1">
              <span className="font-semibold text-sm text-gray-900 capitalize">{s.recommendedVisualType}</span>
              <span className="text-xs font-mono text-emerald-600">{(s.confidenceScore * 100).toFixed(0)}% Confidence</span>
            </div>
            <p className="text-xs text-gray-600 mb-3">{s.reason}</p>
            {onSelectSuggestion && (
              <button
                onClick={() => onSelectSuggestion(s.recommendedVisualType)}
                className="w-full py-1 text-center bg-white border border-gray-300 rounded text-xs font-medium text-gray-700 hover:bg-gray-100"
              >
                Add to Dashboard Canvas
              </button>
            )}
          </div>
        ))}
      </div>
    </div>
  );
};

