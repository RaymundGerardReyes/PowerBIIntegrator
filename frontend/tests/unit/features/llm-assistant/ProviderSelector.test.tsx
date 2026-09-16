import { describe, it, expect, vi } from "vitest";
import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { renderWithProviders } from "../../../setup/test-utils";
import { ProviderSelector } from "@features/llm-assistant/components/ProviderSelector";

describe("ProviderSelector", () => {
  it("renders with LocalOllama enabled by default", () => {
    renderWithProviders(
      <ProviderSelector
        value="LocalOllama"
        onChange={vi.fn()}
        allowCloud={true}
        isSensitiveMode={false}
      />
    );

    const select = screen.getByLabelText(/Select LLM provider/i) as HTMLSelectElement;
    expect(select.value).toBe("LocalOllama");
  });

  it("disables cloud options when allowCloud is false", () => {
    renderWithProviders(
      <ProviderSelector
        value="LocalOllama"
        onChange={vi.fn()}
        allowCloud={false}
        isSensitiveMode={false}
      />
    );

    const openAiOption = screen.getByRole("option", { name: /OpenAI Cloud \(Disabled by Policy\)/i });
    expect(openAiOption).toBeDisabled();
  });

  it("disables cloud options and displays warning notice when sensitive mode is active", () => {
    renderWithProviders(
      <ProviderSelector
        value="LocalOllama"
        onChange={vi.fn()}
        allowCloud={true}
        isSensitiveMode={true}
      />
    );

    const openAiOption = screen.getByRole("option", { name: /OpenAI Cloud \(Disabled by Policy\)/i });
    expect(openAiOption).toBeDisabled();

    expect(screen.getByTestId("sensitive-lock-notice")).toBeInTheDocument();
  });
});
