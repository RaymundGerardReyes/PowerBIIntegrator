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

  it("renders Google Gemini 3.1 Flash Live option and triggers onChange on selection", async () => {
    const user = userEvent.setup();
    const handleChange = vi.fn();

    renderWithProviders(
      <ProviderSelector
        value="CloudGemini"
        onChange={handleChange}
        allowCloud={true}
        isSensitiveMode={false}
      />
    );

    const geminiOption = screen.getByRole("option", { name: /Google Gemini 3.1 Flash Live/i });
    expect(geminiOption).toBeInTheDocument();
    expect(geminiOption).toBeEnabled();

    const select = screen.getByLabelText(/Select LLM provider/i);
    await user.selectOptions(select, "LocalOllama");
    expect(handleChange).toHaveBeenCalledWith("LocalOllama");
  });

  it("disables cloud options including Gemini when allowCloud is false", () => {
    renderWithProviders(
      <ProviderSelector
        value="LocalOllama"
        onChange={vi.fn()}
        allowCloud={false}
        isSensitiveMode={false}
      />
    );

    const geminiOption = screen.getByRole("option", { name: /Google Gemini 3.1 Flash Live \(Disabled by Policy\)/i });
    expect(geminiOption).toBeDisabled();

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

    const geminiOption = screen.getByRole("option", { name: /Google Gemini 3.1 Flash Live \(Disabled by Policy\)/i });
    expect(geminiOption).toBeDisabled();

    const openAiOption = screen.getByRole("option", { name: /OpenAI Cloud \(Disabled by Policy\)/i });
    expect(openAiOption).toBeDisabled();

    expect(screen.getByTestId("sensitive-lock-notice")).toBeInTheDocument();
  });

  it("keeps AntigravityGemini enabled even when allowCloud is false or sensitive mode is active", () => {
    renderWithProviders(
      <ProviderSelector
        value="AntigravityGemini"
        onChange={vi.fn()}
        allowCloud={false}
        isSensitiveMode={true}
      />
    );

    const antigravityOption = screen.getByRole("option", { name: /Antigravity Gemini \(Embedded - Zero API Key\)/i });
    expect(antigravityOption).toBeInTheDocument();
    expect(antigravityOption).toBeEnabled();
  });
});
