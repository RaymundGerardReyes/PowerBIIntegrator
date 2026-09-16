import { describe, it, expect, beforeEach } from "vitest";
import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { renderWithProviders } from "../../../setup/test-utils";
import { DashboardWorkspacePage } from "@features/dashboards/components/DashboardWorkspacePage";
import { useDashboardStore } from "@features/dashboards/model/dashboardSlice";

describe("DashboardWorkspacePage", () => {
  beforeEach(() => {
    useDashboardStore.setState({ current: null });
  });

  it("renders the dashboard workspace header, action toolbar, and canvas by default", () => {
    renderWithProviders(<DashboardWorkspacePage />);

    expect(screen.getByText(/Enterprise Revenue & Operations Dashboard/i)).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "toggle-view-mode" })).toHaveTextContent(/Native Embed View/i);
    expect(screen.getByRole("button", { name: "validate-model-btn" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "compile-pbir-btn" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "compile-tmdl-btn" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "download-pbip-btn" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "import-fabric-btn" })).toBeInTheDocument();
  });

  it("toggles between Canvas layout editor and Native Embed views", async () => {
    const user = userEvent.setup();
    renderWithProviders(<DashboardWorkspacePage />);

    const toggleBtn = screen.getByRole("button", { name: "toggle-view-mode" });
    await user.click(toggleBtn);

    expect(toggleBtn).toHaveTextContent(/Layout Canvas Editor/i);

    await user.click(toggleBtn);
    expect(toggleBtn).toHaveTextContent(/Native Embed View/i);
  });

  it("compiles PBIR successfully and displays notification feedback", async () => {
    const user = userEvent.setup();
    renderWithProviders(<DashboardWorkspacePage />);

    const compilePbirBtn = screen.getByRole("button", { name: "compile-pbir-btn" });
    await user.click(compilePbirBtn);

    await waitFor(() => {
      expect(screen.getByText(/PBIR compiled successfully!/i)).toBeInTheDocument();
    });
  });

  it("compiles TMDL successfully and displays notification feedback", async () => {
    const user = userEvent.setup();
    renderWithProviders(<DashboardWorkspacePage />);

    const compileTmdlBtn = screen.getByRole("button", { name: "compile-tmdl-btn" });
    await user.click(compileTmdlBtn);

    await waitFor(() => {
      expect(screen.getByText(/TMDL semantic model compiled/i)).toBeInTheDocument();
    });
  });

  it("opens Model Validation modal when Validate Model is clicked", async () => {
    const user = userEvent.setup();
    renderWithProviders(<DashboardWorkspacePage />);

    const validateBtn = screen.getByRole("button", { name: "validate-model-btn" });
    await user.click(validateBtn);

    await waitFor(() => {
      expect(screen.getByText(/Validate Semantic Model/i)).toBeInTheDocument();
    });
  });
});
