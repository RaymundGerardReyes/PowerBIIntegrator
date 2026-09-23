import { useState, useRef, useCallback } from "react";
import { streamLlmChat } from "../api/llmStreamClient";
import type { ToolExecutionDetail } from "../model/types";

interface UseLlmStreamOptions {
  onComplete?: (fullText: string, toolDetails?: ToolExecutionDetail[]) => void;
  onError?: (err: Error) => void;
}

export function useLlmStream(options?: UseLlmStreamOptions) {
  const [tokens, setTokens] = useState<string>("");
  const [isStreaming, setIsStreaming] = useState<boolean>(false);
  const [activeTool, setActiveTool] = useState<string | null>(null);
  const [activeToolDetail, setActiveToolDetail] = useState<ToolExecutionDetail | null>(null);
  const [guardrailWarning, setGuardrailWarning] = useState<string | null>(null);

  const abortControllerRef = useRef<AbortController | null>(null);
  const accumulatedRef = useRef<string>("");
  const toolDetailRef = useRef<ToolExecutionDetail | null>(null);

  const startStream = useCallback(
    async (userPrompt: string, providerPreference?: string, policyId?: string) => {
      setTokens("");
      setIsStreaming(true);
      setActiveTool(null);
      setActiveToolDetail(null);
      toolDetailRef.current = null;
      setGuardrailWarning(null);
      accumulatedRef.current = "";

      abortControllerRef.current?.abort();
      const abortController = new AbortController();
      abortControllerRef.current = abortController;

      try {
        await streamLlmChat({
          userPrompt,
          providerPreference,
          policyId,
          signal: abortController.signal,
          onToken: (token) => {
            accumulatedRef.current += token;
            setTokens((prev) => prev + token);
          },
          onToolCall: (tool, args, result, latencyMs) => {
            setActiveTool(tool);
            const detail: ToolExecutionDetail = {
              toolName: tool,
              status: "completed",
              latencyMs,
              args,
              result
            };
            setActiveToolDetail(detail);
            toolDetailRef.current = detail;
          },
          onGuardrailViolation: (warning) => {
            setGuardrailWarning(warning);
          },
          onDone: () => {
            setIsStreaming(false);
            const details = toolDetailRef.current ? [toolDetailRef.current] : undefined;
            options?.onComplete?.(accumulatedRef.current, details);
          }
        });
      } catch (err: unknown) {
        setIsStreaming(false);
        if (err instanceof Error && err.name !== "AbortError") {
          options?.onError?.(err);
        }
      }
    },
    [options]
  );

  const stopStream = useCallback(() => {
    abortControllerRef.current?.abort();
    setIsStreaming(false);
  }, []);

  return {
    tokens,
    isStreaming,
    activeTool,
    activeToolDetail,
    guardrailWarning,
    startStream,
    stopStream
  };
}
