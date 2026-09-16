import React from "react";

interface ToolExecutionBadgeProps {
  toolName: string;
  status?: "running" | "completed" | "failed";
}

export const ToolExecutionBadge: React.FC<ToolExecutionBadgeProps> = ({
  toolName,
  status = "running"
}) => {
  const getBadgeStyle = () => {
    switch (status) {
      case "completed":
        return "bg-green-100 text-green-800 border-green-300";
      case "failed":
        return "bg-red-100 text-red-800 border-red-300";
      default:
        return "bg-indigo-100 text-indigo-800 border-indigo-300 animate-pulse";
    }
  };

  return (
    <div
      className={`inline-flex items-center space-x-1.5 px-2.5 py-1 rounded-full text-xs font-medium border ${getBadgeStyle()}`}
      data-testid="tool-execution-badge"
    >
      <span className="w-2 h-2 rounded-full bg-current" />
      <span>MCP Tool: {toolName}</span>
      {status === "running" && <span className="italic text-[10px]">(Executing...)</span>}
    </div>
  );
};

