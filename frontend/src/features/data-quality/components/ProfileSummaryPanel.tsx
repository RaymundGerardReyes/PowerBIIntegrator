import React from "react";
import type { DatasetProfileDto } from "@shared/types/api-contracts";

interface ProfileSummaryPanelProps {
  profile: DatasetProfileDto | null;
  isLoading?: boolean;
}

export const ProfileSummaryPanel: React.FC<ProfileSummaryPanelProps> = ({ profile, isLoading }) => {
  if (isLoading) {
    return <div className="p-4 text-gray-500 animate-pulse">Profiling dataset deterministically...</div>;
  }

  if (!profile) {
    return <div className="p-4 text-gray-400">No dataset profile available. Please select a source.</div>;
  }

  return (
    <div className="bg-white border rounded-lg shadow-sm p-5 space-y-4">
      <div className="flex justify-between items-center border-b pb-3">
        <div>
          <h3 className="text-lg font-semibold text-gray-900">{profile.datasetName}</h3>
          <p className="text-xs text-gray-500">Source: {profile.sourceReference} | Total Rows: {profile.totalRows.toLocaleString()}</p>
        </div>
        <span className="text-xs bg-blue-50 text-blue-700 px-2.5 py-1 rounded-full font-medium">
          Deterministic Profile
        </span>
      </div>

      <div className="overflow-x-auto">
        <table className="min-w-full divide-y divide-gray-200 text-sm">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-3 py-2 text-left font-medium text-gray-600">Column</th>
              <th className="px-3 py-2 text-left font-medium text-gray-600">Inferred Type</th>
              <th className="px-3 py-2 text-left font-medium text-gray-600">Null %</th>
              <th className="px-3 py-2 text-left font-medium text-gray-600">Distinct</th>
              <th className="px-3 py-2 text-left font-medium text-gray-600">Cardinality</th>
              <th className="px-3 py-2 text-left font-medium text-gray-600">Regex Signature</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-100">
            {profile.columnProfiles.map((col) => (
              <tr key={col.columnName} className="hover:bg-gray-50">
                <td className="px-3 py-2 font-medium text-gray-900">{col.columnName}</td>
                <td className="px-3 py-2 text-gray-600 font-mono text-xs">{col.inferredType}</td>
                <td className="px-3 py-2 text-gray-600">{(col.nullRatio * 100).toFixed(1)}%</td>
                <td className="px-3 py-2 text-gray-600">{col.distinctCount.toLocaleString()}</td>
                <td className="px-3 py-2">
                  <span className={`px-2 py-0.5 text-xs rounded-full font-medium ${
                    col.cardinalityClass === "High" ? "bg-purple-50 text-purple-700" :
                    col.cardinalityClass === "Medium" ? "bg-amber-50 text-amber-700" :
                    "bg-green-50 text-green-700"
                  }`}>
                    {col.cardinalityClass}
                  </span>
                </td>
                <td className="px-3 py-2 text-gray-500 font-mono text-xs truncate max-w-xs">{col.detectedPatternRegex}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
};

