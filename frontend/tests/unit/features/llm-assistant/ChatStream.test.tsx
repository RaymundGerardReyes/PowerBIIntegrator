import { describe, it, expect } from "vitest";
import { screen } from "@testing-library/react";
import { renderWithProviders } from "../../../setup/test-utils";
import { ChatStream } from "@features/llm-assistant/components/ChatStream";

describe("ChatStream Component", () => {
  it("renders streamed tokens and cursor when streaming is active", () => {
    renderWithProviders(
      <ChatStream
        tokens="Synthesizing revenue report..."
        isStreaming={true}
        activeTool={null}
      />
    );

    expect(screen.getByText(/Synthesizing revenue report/i)).toBeInTheDocument();
    expect(screen.getByTestId("stream-cursor")).toBeInTheDocument();
  });

  it("renders active tool execution badge when activeTool is specified", () => {
    renderWithProviders(
      <ChatStream
        tokens="Validating analytics model..."
        isStreaming={true}
        activeTool="validate_analytics_model"
      />
    );

    expect(screen.getByTestId("tool-execution-badge")).toBeInTheDocument();
    expect(screen.getByText(/validate_analytics_model/i)).toBeInTheDocument();
  });

  it("returns null when empty tokens and not streaming", () => {
    const { container } = renderWithProviders(
      <ChatStream tokens="" isStreaming={false} activeTool={null} />
    );

    expect(container.firstChild).toBeNull();
  });
});

