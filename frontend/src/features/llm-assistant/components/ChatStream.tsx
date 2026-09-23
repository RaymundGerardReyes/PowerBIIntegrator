import React from "react";
import { ToolExecutionBadge } from "./ToolExecutionBadge";

interface ChatStreamProps {
  tokens: string;
  isStreaming: boolean;
  activeTool?: string | null;
  provider?: string;
}

export const ChatStream: React.FC<ChatStreamProps> = ({
  tokens,
  isStreaming,
  activeTool,
  provider = "Gemini 3.1 Flash Live"
}) => {
  if (!tokens && !isStreaming && !activeTool) {
    return null;
  }

  return (
    <div
      className="flex flex-col items-start my-2.5 max-w-[92%] transition-all animate-fade-in"
      data-testid="chat-stream-container"
    >
      {activeTool && (
        <div className="mb-1.5">
          <ToolExecutionBadge
            toolName={activeTool}
            status={isStreaming ? "running" : "completed"}
          />
        </div>
      )}

      <div className="relative px-3.5 py-2.5 rounded-xl text-sm bg-linear-to-br from-indigo-50/80 to-white/90 dark:from-indigo-950/40 dark:to-gray-900/90 text-gray-900 dark:text-gray-100 border border-indigo-200/70 dark:border-indigo-800/60 shadow-xs backdrop-blur-md whitespace-pre-wrap leading-relaxed">
        {tokens}
        {isStreaming && (
          <span
            className="inline-block w-2 h-4 ml-1 bg-indigo-600 dark:bg-indigo-400 animate-pulse align-middle rounded-xs shadow-xs shadow-indigo-500/50"
            data-testid="stream-cursor"
          />
        )}
      </div>

      {isStreaming && (
        <div className="flex items-center gap-1.5 mt-1 text-[10px] text-indigo-600 dark:text-indigo-400 font-medium">
          <span className="w-1.5 h-1.5 rounded-full bg-indigo-500 animate-ping" />
          <span>Streaming live from {provider}...</span>
        </div>
      )}
    </div>
  );
};
