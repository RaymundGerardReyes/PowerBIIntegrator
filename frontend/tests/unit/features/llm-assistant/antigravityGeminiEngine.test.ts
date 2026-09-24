import { describe, it, expect, vi } from "vitest";
import { streamAntigravityGemini } from "@features/llm-assistant/api/antigravityGeminiEngine";

describe("antigravityGeminiEngine Non-Deterministic Generative Engine", () => {
  it("generates contextual Spanish response when greeted with 'Hola'", async () => {
    let streamedText = "";
    let toolCalled = "";

    await streamAntigravityGemini({
      userPrompt: "Hola",
      onToken: (token) => {
        streamedText += token;
      },
      onToolCall: (toolName) => {
        toolCalled = toolName;
      }
    });

    expect(toolCalled).toBe("initialize_copilot_session");
    expect(streamedText).toMatch(/¡Hola!|¡Saludos!/i);
    expect(streamedText).toContain("1,309 registros");
    expect(streamedText).toContain("titanic");
  });

  it("accurately reports the 1,309 record count for 'How many records where in already existed?'", async () => {
    let streamedText = "";
    let toolCalled = "";

    await streamAntigravityGemini({
      userPrompt: "How many records where in already existed?",
      onToken: (token) => {
        streamedText += token;
      },
      onToolCall: (toolName) => {
        toolCalled = toolName;
      }
    });

    expect(toolCalled).toBe("query_dataset_statistics");
    expect(streamedText).toContain("1,309 records");
    expect(streamedText).toContain("titanic");
    expect(streamedText).toContain("pclass");
    expect(streamedText).toContain("TotalRows");
  });

  it("provides comprehensive data logic analysis for 'Please do guide me analyze well the current logic of the Data I have right now?'", async () => {
    let streamedText = "";
    let toolCalled = "";

    await streamAntigravityGemini({
      userPrompt: "Please do guide me analyze well the current logic of the Data I have right now?",
      onToken: (token) => {
        streamedText += token;
      },
      onToolCall: (toolName) => {
        toolCalled = toolName;
      }
    });

    expect(toolCalled).toBe("analyze_data_domain_logic");
    expect(streamedText).toContain("target_Rate");
    expect(streamedText).toContain("pclass");
    expect(streamedText).toContain("sex");
    expect(streamedText).toContain("1,309 records");
  });

  it("audits DAX measures when asked to audit measures", async () => {
    let streamedText = "";
    let toolCalled = "";

    await streamAntigravityGemini({
      userPrompt: "Audit all declared DAX measures",
      onToken: (token) => {
        streamedText += token;
      },
      onToolCall: (toolName) => {
        toolCalled = toolName;
      }
    });

    expect(toolCalled).toBe("audit_semantic_measures");
    expect(streamedText).toContain("TotalRows");
    expect(streamedText).toContain("target_Rate");
    expect(streamedText).toContain("Total_fare");
  });

  it("respects AbortSignal to cancel streaming cleanly", async () => {
    const controller = new AbortController();
    let tokenCount = 0;

    const streamPromise = streamAntigravityGemini({
      userPrompt: "How many records where in already existed?",
      signal: controller.signal,
      onToken: () => {
        tokenCount++;
        controller.abort();
      }
    });

    await streamPromise;
    expect(tokenCount).toBeLessThan(10);
  });
});
