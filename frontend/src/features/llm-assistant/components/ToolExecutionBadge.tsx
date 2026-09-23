import React, { useState } from "react";

interface ToolExecutionBadgeProps {
  toolName: string;
  status?: "running" | "completed" | "failed";
  latencyMs?: number;
  args?: Record<string, unknown> | string;
  result?: Record<string, unknown> | string;
}

export const ToolExecutionBadge: React.FC<ToolExecutionBadgeProps> = ({
  toolName,
  status = "running",
  latencyMs,
  args,
  result
}) => {
  const [isExpanded, setIsExpanded] = useState(false);

  const getStatusConfig = () => {
    switch (status) {
      case "completed":
        return {
          pill: "bg-emerald-50 dark:bg-emerald-950/60 text-emerald-800 dark:text-emerald-300 border-emerald-300 dark:border-emerald-700/60",
          dot: "bg-emerald-500",
          icon: "✓"
        };
      case "failed":
        return {
          pill: "bg-rose-50 dark:bg-rose-950/60 text-rose-800 dark:text-rose-300 border-rose-300 dark:border-rose-700/60",
          dot: "bg-rose-500",
          icon: "✕"
        };
      default:
        return {
          pill: "bg-indigo-50 dark:bg-indigo-950/60 text-indigo-800 dark:text-indigo-300 border-indigo-300 dark:border-indigo-700/60 animate-pulse",
          dot: "bg-indigo-500 animate-ping",
          icon: "⚙"
        };
    }
  };

  const config = getStatusConfig();
  const hasDetails = Boolean(args || result || latencyMs !== undefined);

  return (
    <div className="my-1.5 flex flex-col font-sans transition-all">
      <div
        className={`inline-flex items-center space-x-1.5 px-3 py-1 rounded-full text-xs font-medium border shadow-2xs transition-all ${config.pill} ${
          hasDetails ? "cursor-pointer hover:shadow-sm" : ""
        }`}
        data-testid="tool-execution-badge"
        onClick={() => hasDetails && setIsExpanded((prev) => !prev)}
        role={hasDetails ? "button" : undefined}
        aria-expanded={hasDetails ? isExpanded : undefined}
        title={hasDetails ? "Click to toggle tool execution telemetry" : undefined}
      >
        <span className={`w-2 h-2 rounded-full ${config.dot}`} />
        <span className="font-mono text-[11px]">MCP Tool: {toolName}</span>
        {status === "running" && <span className="italic text-[10px]">(Executing...)</span>}
        {status === "completed" && <span className="text-[10px] text-emerald-600 font-semibold">{config.icon}</span>}
        {status === "failed" && <span className="text-[10px] text-rose-600 font-semibold">{config.icon}</span>}
        {latencyMs !== undefined && (
          <span className="text-[9px] opacity-75 font-mono px-1 bg-black/5 dark:bg-white/10 rounded">
            {latencyMs}ms
          </span>
        )}
        {hasDetails && (
          <span className="text-[10px] opacity-70 ml-1 select-none">
            {isExpanded ? "▲" : "▼"}
          </span>
        )}
      </div>

      {hasDetails && isExpanded && (
        <div
          data-testid="tool-execution-details"
          className="mt-1 p-2 bg-gray-900 text-gray-100 rounded-md text-[11px] font-mono border border-gray-700/80 shadow-md max-w-full overflow-x-auto space-y-1.5"
        >
          {args && (
            <div>
              <span className="text-gray-400 font-bold block text-[10px] uppercase tracking-wider">Arguments:</span>
              <pre className="text-emerald-400 whitespace-pre-wrap break-all text-[10px] m-0">
                {typeof args === "string" ? args : JSON.stringify(args, null, 2)}
              </pre>
            </div>
          )}
          {result && (
            <div className="border-t border-gray-700/60 pt-1">
              <span className="text-gray-400 font-bold block text-[10px] uppercase tracking-wider">Output:</span>
              <pre className="text-cyan-300 whitespace-pre-wrap break-all text-[10px] m-0">
                {typeof result === "string" ? result : JSON.stringify(result, null, 2)}
              </pre>
            </div>
          )}
        </div>
      )}
    </div>
  );
};
