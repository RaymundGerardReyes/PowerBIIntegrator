import { describe, it, expect, beforeEach, vi } from "vitest";
import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { renderWithProviders } from "../../../setup/test-utils";
import { AssistantPanel } from "@features/llm-assistant/components/AssistantPanel";
import { useLlmAssistantStore } from "@features/llm-assistant/model/llmAssistantSlice";

// Mock the API calls
vi.mock("@features/llm-assistant/api/llmApi", () => ({
  runLlmTask: vi.fn().mockResolvedValue({
    rawText: "Mocked analysis: DAX measures are valid and bound to TotalRows.",
    providerUsed: "LocalOllama",
    guardrailNotice: undefined,
    isBlocked: false
  })
}));

vi.mock("@features/llm-assistant/api/llmStreamClient", () => ({
  streamLlmChat: vi.fn().mockImplementation(async ({ onToken, onDone }) => {
    onToken("Gemini 3.1 Flash Live response token ");
    onDone?.();
  })
}));

describe("AssistantPanel Modern UI/UX & Gemini Live Orchestration", () => {
  beforeEach(() => {
    useLlmAssistantStore.setState({
      isOpen: false,
      dockMode: "docked",
      providerPreference: "CloudGemini",
      sensitiveMode: false,
      activePolicyId: "default",
      isVoiceActive: false,
      liveStatus: "idle",
      messages: []
    });
  });

  it("renders closed floating button with Live badge and opens panel on click", async () => {
    const user = userEvent.setup();
    renderWithProviders(<AssistantPanel />);

    const openBtn = screen.getByTestId("open-assistant-btn");
    expect(openBtn).toBeInTheDocument();
    expect(openBtn).toHaveTextContent(/AI Copilot/i);
    expect(openBtn).toHaveTextContent(/Live/i);

    await user.click(openBtn);

    expect(screen.getByTestId("assistant-panel")).toBeInTheDocument();
    expect(screen.getByText(/Gemini 3.1 Flash Live \(Active\)/i)).toBeInTheDocument();
  });

  it("strictly forbids <aside> element in DOM when open, closed, or floating", async () => {
    const user = userEvent.setup();
    renderWithProviders(<AssistantPanel />);

    expect(document.querySelector("aside")).toBeNull();

    // Open panel
    await user.click(screen.getByTestId("open-assistant-btn"));
    expect(document.querySelector("aside")).toBeNull();

    // Toggle to floating mode
    const dockBtn = screen.getByTestId("dock-toggle-btn");
    await user.click(dockBtn);
    expect(document.querySelector("aside")).toBeNull();
  });

  it("toggles dock mode between docked and floating window", async () => {
    const user = userEvent.setup();
    useLlmAssistantStore.setState({ isOpen: true, dockMode: "docked" });

    renderWithProviders(<AssistantPanel />);
    const panel = screen.getByTestId("assistant-panel");
    expect(panel.className).toContain("w-full");

    const dockBtn = screen.getByTestId("dock-toggle-btn");
    await user.click(dockBtn);

    expect(useLlmAssistantStore.getState().dockMode).toBe("floating");
    expect(panel.className).toContain("fixed");
  });

  it("toggles live voice streaming and displays animated waveform visualizer", async () => {
    const user = userEvent.setup();
    useLlmAssistantStore.setState({ isOpen: true, isVoiceActive: false });

    renderWithProviders(<AssistantPanel />);
    expect(screen.queryByTestId("voice-waveform")).not.toBeInTheDocument();

    const voiceBtn = screen.getByTestId("toggle-voice-btn");
    await user.click(voiceBtn);

    expect(useLlmAssistantStore.getState().isVoiceActive).toBe(true);
    expect(screen.getByTestId("voice-waveform")).toBeInTheDocument();
    expect(screen.getByText(/Gemini Live Audio Active/i)).toBeInTheDocument();

    await user.click(voiceBtn);
    expect(useLlmAssistantStore.getState().isVoiceActive).toBe(false);
    expect(screen.queryByTestId("voice-waveform")).not.toBeInTheDocument();
  });

  it("renders contextual Power BI quick action chips and populates conversation", async () => {
    const user = userEvent.setup();
    useLlmAssistantStore.setState({ isOpen: true, messages: [] });

    renderWithProviders(<AssistantPanel />);

    expect(screen.getByText(/Audit Semantic Measures/i)).toBeInTheDocument();
    expect(screen.getByText(/Verify PBIR Parity/i)).toBeInTheDocument();
    expect(screen.getByText(/Suggest Optimal Layout/i)).toBeInTheDocument();
    expect(screen.getByText(/Check Data Quality Rules/i)).toBeInTheDocument();

    // Click quick action chip
    const auditChip = screen.getByText(/Audit Semantic Measures/i);
    await user.click(auditChip);

    // Verify user message and streamed assistant response were added to store
    expect(useLlmAssistantStore.getState().messages).toHaveLength(2);
    expect(useLlmAssistantStore.getState().messages[0].text).toContain("Audit all declared measures");
    expect(useLlmAssistantStore.getState().messages[1].text).toContain("Gemini 3.1 Flash Live response token");
  });

  it("enforces zero-data-leak when sensitive mode is toggled, reverting CloudGemini to LocalOllama", async () => {
    const user = userEvent.setup();
    useLlmAssistantStore.setState({
      isOpen: true,
      providerPreference: "CloudGemini",
      sensitiveMode: false
    });

    renderWithProviders(<AssistantPanel />);

    const sensitiveCheckbox = screen.getByLabelText(/Sensitive \/ Camera Data Mode/i);
    await user.click(sensitiveCheckbox);

    expect(useLlmAssistantStore.getState().sensitiveMode).toBe(true);
    expect(useLlmAssistantStore.getState().providerPreference).toBe("LocalOllama");
    expect(screen.getByTestId("sensitive-lock-notice")).toBeInTheDocument();
  });
});
