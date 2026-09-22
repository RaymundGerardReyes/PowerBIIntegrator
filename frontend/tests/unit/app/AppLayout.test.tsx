import { describe, it, expect } from "vitest";
import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { renderWithProviders } from "../../setup/test-utils";
import { AppLayout } from "@app/layout/AppLayout";
import { ThemeProvider } from "@app/providers/ThemeProvider";
import { AuthProvider } from "@app/providers/AuthProvider";

describe("AppLayout", () => {
  const renderLayout = (content: React.ReactNode = <div>Test Child Content</div>) =>
    renderWithProviders(
      <ThemeProvider>
        <AuthProvider>
          <AppLayout>{content}</AppLayout>
        </AuthProvider>
      </ThemeProvider>
    );

  it("renders top brand navigation and main content", () => {
    renderLayout();

    expect(screen.getByText(/PowerBI Enhanced/i)).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /Dashboards & PBIP/i })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /Data Sources/i })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /Data Quality & Advisory/i })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /Executive Reports/i })).toBeInTheDocument();
    expect(screen.getByText("Test Child Content")).toBeInTheDocument();
  });

  it("toggles the AI Copilot assistant panel drawer on click", async () => {
    const user = userEvent.setup();
    renderLayout();

    expect(screen.queryByLabelText(/AI Copilot Assistant Panel/i)).not.toBeInTheDocument();

    const toggleBtn = screen.getByRole("button", { name: /toggle-ai-assistant/i });
    await user.click(toggleBtn);

    expect(screen.getByLabelText(/AI Copilot Assistant Panel/i)).toBeInTheDocument();
    expect(screen.getByText(/Model Context Protocol Agent/i)).toBeInTheDocument();

    const closeBtn = screen.getByRole("button", { name: /close-assistant-panel/i });
    await user.click(closeBtn);

    expect(screen.queryByLabelText(/AI Copilot Assistant Panel/i)).not.toBeInTheDocument();
  });

  it("strictly forbids <aside> sidebar anywhere in the layout shell", async () => {
    const user = userEvent.setup();
    renderLayout();

    // Invariant: Aside sidebar is forbidden
    expect(document.querySelector("aside")).toBeNull();

    // Even when copilot drawer opens, it must use section, not aside
    const toggleBtn = screen.getByRole("button", { name: /toggle-ai-assistant/i });
    await user.click(toggleBtn);
    expect(document.querySelector("aside")).toBeNull();
  });

  it("verifies all four top navigation links route to exact target paths", () => {
    renderLayout();

    const dashboardsLink = screen.getByRole("link", { name: /Dashboards & PBIP/i });
    const dataSourcesLink = screen.getByRole("link", { name: /Data Sources/i });
    const dataQualityLink = screen.getByRole("link", { name: /Data Quality & Advisory/i });
    const reportsLink = screen.getByRole("link", { name: /Executive Reports/i });

    expect(dashboardsLink).toHaveAttribute("href", "/dashboards");
    expect(dataSourcesLink).toHaveAttribute("href", "/data-sources");
    expect(dataQualityLink).toHaveAttribute("href", "/data-quality");
    expect(reportsLink).toHaveAttribute("href", "/reports");
  });

  it("renders 'Sign in' link by default when unauthenticated", () => {
    renderLayout();

    expect(screen.getByRole("button", { name: /Sign in/i })).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /Sign out/i })).not.toBeInTheDocument();
  });

  it("toggles theme when clicking the theme toggle button", async () => {
    const user = userEvent.setup();
    renderLayout();

    const themeBtn = screen.getByRole("button", { name: /toggle-theme-button/i });
    expect(themeBtn).toHaveTextContent(/Dark/i);

    await user.click(themeBtn);
    expect(themeBtn).toHaveTextContent(/Light/i);
  });
});

