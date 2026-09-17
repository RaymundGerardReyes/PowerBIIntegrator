import { describe, it, expect } from "vitest";
import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { renderWithProviders } from "../../../setup/test-utils";
import { LocalDesktopOrchestrator } from "@features/powerbi-embed/components/LocalDesktopOrchestrator";

describe("LocalDesktopOrchestrator", () => {
  it("renders local desktop status and controls", async () => {
    renderWithProviders(
      <LocalDesktopOrchestrator
        modelId="11111111-1111-1111-1111-111111111111"
        modelName="Enterprise Sales & Finance Semantic Model"
        dashboardId="77777777-7777-7777-7777-777777777777"
        dashboardName="Enterprise Revenue & Operations Dashboard"
      />
    );

    expect(screen.getByText(/Local Power BI Desktop Orchestrator/i)).toBeInTheDocument();
    expect(screen.getByText(/Windows 11 Native/i)).toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getByText(/Power BI Desktop Detected/i)).toBeInTheDocument();
      expect(screen.getByText(/Analysis Services Engine/i)).toBeInTheDocument();
    });

    expect(screen.getByRole("button", { name: "launch-desktop-btn" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "open-folder-btn" })).toBeInTheDocument();
  });

  it("launches project and displays launch notification feedback", async () => {
    const user = userEvent.setup();
    renderWithProviders(
      <LocalDesktopOrchestrator
        modelId="11111111-1111-1111-1111-111111111111"
        modelName="Enterprise Sales & Finance Semantic Model"
        dashboardId="77777777-7777-7777-7777-777777777777"
        dashboardName="Enterprise Revenue & Operations Dashboard"
      />
    );

    const launchBtn = screen.getByRole("button", { name: "launch-desktop-btn" });
    await user.click(launchBtn);

    await waitFor(() => {
      expect(screen.getByText(/Launched project/i)).toBeInTheDocument();
    });
  });
});

