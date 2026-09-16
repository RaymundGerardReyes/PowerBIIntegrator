import { describe, it, expect, beforeEach } from "vitest";
import { useLlmAssistantStore } from "@features/llm-assistant/model/llmAssistantSlice";

describe("llmAssistantSlice", () => {
  beforeEach(() => {
    useLlmAssistantStore.setState({
      isOpen: false,
      providerPreference: "LocalOllama",
      sensitiveMode: false,
      activePolicyId: "default",
      messages: []
    });
  });

  it("toggles panel open state correctly", () => {
    expect(useLlmAssistantStore.getState().isOpen).toBe(false);

    useLlmAssistantStore.getState().togglePanel();
    expect(useLlmAssistantStore.getState().isOpen).toBe(true);

    useLlmAssistantStore.getState().togglePanel();
    expect(useLlmAssistantStore.getState().isOpen).toBe(false);
  });

  it("adds and clears messages correctly", () => {
    useLlmAssistantStore.getState().addMessage({
      id: "m1",
      sender: "user",
      text: "Analyze measures",
      timestamp: new Date().toISOString()
    });

    expect(useLlmAssistantStore.getState().messages).toHaveLength(1);
    expect(useLlmAssistantStore.getState().messages[0].text).toBe("Analyze measures");

    useLlmAssistantStore.getState().clearMessages();
    expect(useLlmAssistantStore.getState().messages).toHaveLength(0);
  });

  it("updates provider preference and sensitive mode", () => {
    useLlmAssistantStore.getState().setProviderPreference("CloudOpenAi");
    expect(useLlmAssistantStore.getState().providerPreference).toBe("CloudOpenAi");

    useLlmAssistantStore.getState().setSensitiveMode(true);
    expect(useLlmAssistantStore.getState().sensitiveMode).toBe(true);
  });
});
