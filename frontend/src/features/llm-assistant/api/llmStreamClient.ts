import { streamAntigravityGemini } from "./antigravityGeminiEngine";

export interface StreamChatOptions {
  userPrompt: string;
  contextIds?: string[];
  providerPreference?: string;
  policyId?: string;
  onToken: (token: string) => void;
  onToolCall?: (toolName: string, args?: Record<string, unknown>, result?: Record<string, unknown>, latencyMs?: number) => void;
  onGuardrailViolation?: (warning: string) => void;
  onDone?: () => void;
  signal?: AbortSignal;
}

export async function streamLlmChat(options: StreamChatOptions): Promise<void> {
  const correlationId = crypto.randomUUID();

  // If using Antigravity Gemini or offline copilot, stream directly from the in-app intelligence engine
  if (options.providerPreference === "AntigravityGemini" || options.providerPreference === "CloudGemini" || !options.providerPreference) {
    try {
      await streamAntigravityGemini(options);
      return;
    } catch {
      // Fallback
    }
  }

  try {
    const response = await fetch("/api/llm/chat/stream", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        "X-Correlation-Id": correlationId
      },
      body: JSON.stringify({
        userPrompt: options.userPrompt,
        contextIds: options.contextIds ?? [],
        providerPreference: options.providerPreference ?? "LocalOllama",
        policyId: options.policyId ?? "default",
        correlationId
      }),
      signal: options.signal
    });

    if (!response.ok || !response.body) {
      throw new Error(`Server returned ${response.status}: ${response.statusText}`);
    }

    const reader = response.body.getReader();
    const decoder = new TextDecoder("utf-8");
    let buffer = "";

    let tokenCount = 0;
    while (true) {
      const { done, value } = await reader.read();
      if (done) break;

      buffer += decoder.decode(value, { stream: true });
      const lines = buffer.split("\n");
      buffer = lines.pop() ?? "";

      for (const line of lines) {
        const trimmed = line.trim();
        if (!trimmed.startsWith("data:")) continue;
        const dataStr = trimmed.replace("data:", "").trim();
        if (dataStr === "[DONE]") {
          if (tokenCount === 0 && !options.signal?.aborted) {
            await streamAntigravityGemini(options);
            return;
          }
          options.onDone?.();
          return;
        }

        try {
          const payload = JSON.parse(dataStr);
          if (payload.token) {
            options.onToken(payload.token);
            tokenCount++;
          }
          if (payload.activeTool) options.onToolCall?.(payload.activeTool);
          if (payload.guardrailNotice) options.onGuardrailViolation?.(payload.guardrailNotice);
        } catch {
          if (dataStr) {
            options.onToken(dataStr);
            tokenCount++;
          }
        }
      }
    }
    if (tokenCount === 0 && !options.signal?.aborted) {
      await streamAntigravityGemini(options);
      return;
    }
    options.onDone?.();
  } catch (err: unknown) {
    // If backend streaming fails (e.g. no backend server running or no API key),
    // robustly fallback to the in-app Antigravity Gemini copilot engine
    if (options.signal?.aborted) return;
    await streamAntigravityGemini(options);
  }
}
