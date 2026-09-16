import { useState, useRef, useCallback } from "react";
import { streamLlmChat } from "../api/llmStreamClient";

interface UseLlmStreamOptions {
  onComplete?: (fullText: string) => void;
  onError?: (err: Error) => void;
}

export function useLlmStream(options?: UseLlmStreamOptions) {
  const [tokens, setTokens] = useState<string>("");
  const [isStreaming, setIsStreaming] = useState<boolean>(false);
  const [activeTool, setActiveTool] = useState<string | null>(null);
  const [guardrailWarning, setGuardrailWarning] = useState<string | null>(null);

  const abortControllerRef = useRef<AbortController | null>(null);
  const accumulatedRef = useRef<string>("");

  const startStream = useCallback(
    async (userPrompt: string, providerPreference?: string, policyId?: string) => {
      setTokens("");
      setIsStreaming(true);
      setActiveTool(null);
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
          onToolCall: (tool) => {
            setActiveTool(tool);
          },
          onGuardrailViolation: (warning) => {
            setGuardrailWarning(warning);
          },
          onDone: () => {
            setIsStreaming(false);
            options?.onComplete?.(accumulatedRef.current);
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
    guardrailWarning,
    startStream,
    stopStream
  };
}

