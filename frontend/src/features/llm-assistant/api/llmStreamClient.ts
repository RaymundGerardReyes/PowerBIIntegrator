export interface StreamChatOptions {
  userPrompt: string;
  contextIds?: string[];
  providerPreference?: string;
  policyId?: string;
  onToken: (token: string) => void;
  onToolCall?: (toolName: string) => void;
  onGuardrailViolation?: (warning: string) => void;
  onDone?: () => void;
  signal?: AbortSignal;
}

export async function streamLlmChat(options: StreamChatOptions): Promise<void> {
  const correlationId = crypto.randomUUID();
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
    throw new Error(`Failed to initiate stream: ${response.statusText}`);
  }

  const reader = response.body.getReader();
  const decoder = new TextDecoder("utf-8");
  let buffer = "";

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
        options.onDone?.();
        return;
      }

      try {
        const payload = JSON.parse(dataStr);
        if (payload.token) options.onToken(payload.token);
        if (payload.activeTool) options.onToolCall?.(payload.activeTool);
        if (payload.guardrailNotice) options.onGuardrailViolation?.(payload.guardrailNotice);
      } catch {
        if (dataStr) options.onToken(dataStr);
      }
    }
  }
  options.onDone?.();
}

