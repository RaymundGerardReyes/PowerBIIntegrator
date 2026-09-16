import React from "react";

export interface DuplicateReviewItem {
  clusterId: string;
  ruleFired: string;
  keptRowId: string;
  droppedRowIds: string[];
  confidenceScore: number;
  reasonCode: string;
}

interface DuplicateReviewTableProps {
  clusters: DuplicateReviewItem[];
  onKeepOverride?: (clusterId: string, newKeptRowId: string) => void;
}

export const DuplicateReviewTable: React.FC<DuplicateReviewTableProps> = ({ clusters, onKeepOverride }) => {
  if (!clusters.length) {
    return <div className="p-4 text-gray-500">No duplicate clusters detected. Clean data!</div>;
  }

  return (
    <div className="bg-white border rounded-lg p-5 space-y-4">
      <div className="flex justify-between items-center border-b pb-3">
        <h3 className="text-lg font-semibold text-gray-900">Duplicate Clusters Review</h3>
        <span className="text-xs bg-amber-50 text-amber-700 px-2.5 py-1 rounded font-medium">
          {clusters.length} Duplicate Cluster(s)
        </span>
      </div>

      <div className="space-y-3">
        {clusters.map((cluster) => (
          <div key={cluster.clusterId} className="border rounded p-3 bg-gray-50 space-y-2">
            <div className="flex justify-between items-center text-xs">
              <span className="font-semibold text-gray-700">{cluster.ruleFired}</span>
              <span className="text-gray-500 font-mono">Score: {(cluster.confidenceScore * 100).toFixed(0)}%</span>
            </div>
            <p className="text-xs text-gray-600">{cluster.reasonCode}</p>
            <div className="flex items-center gap-2 text-xs">
              <span className="text-green-700 font-medium bg-green-50 px-2 py-0.5 rounded">
                Kept: {cluster.keptRowId}
              </span>
              <span className="text-red-700 bg-red-50 px-2 py-0.5 rounded">
                Dropped: {cluster.droppedRowIds.join(", ")}
              </span>
              {onKeepOverride && (
                <button
                  onClick={() => onKeepOverride(cluster.clusterId, cluster.droppedRowIds[0])}
                  className="ml-auto text-blue-600 hover:underline text-xs"
                >
                  Swap Kept Row
                </button>
              )}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};

