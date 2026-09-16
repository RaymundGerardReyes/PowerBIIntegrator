import React from "react";
import { ToolExecutionBadge } from "./ToolExecutionBadge";

interface ChatStreamProps {
  tokens: string;
  isStreaming: boolean;
  activeTool?: string | null;
}

export const ChatStream: React.FC<ChatStreamProps> = ({
  tokens,
  isStreaming,
  activeTool
}) => {
  if (!tokens && !isStreaming && !activeTool) {
    return null;
  }

  return (
    <div className="flex flex-col items-start my-2 max-w-[85%]" data-testid="chat-stream-container">
      {activeTool && (
        <div className="mb-1">
          <ToolExecutionBadge toolName={activeTool} status={isStreaming ? "running" : "completed"} />
        </div>
      )}
      <div className="px-3 py-2 rounded-lg text-sm bg-gray-100 text-gray-900 border border-gray-200 whitespace-pre-wrap">
        {tokens}
        {isStreaming && (
          <span className="inline-block w-1.5 h-4 ml-0.5 bg-indigo-600 animate-pulse align-middle" data-testid="stream-cursor" />
        )}
      </div>
    </div>
  );
};

