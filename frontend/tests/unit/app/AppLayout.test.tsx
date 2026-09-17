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
      <MemoryRouter initialEntries={["/dashboards"]}>
        <ThemeProvider>
          <AuthProvider>
            <AppLayout>{content}</AppLayout>
          </AuthProvider>
        </ThemeProvider>
      </MemoryRouter>
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

  it("toggles theme when clicking the theme toggle button", async () => {
    const user = userEvent.setup();
    renderLayout();

    const themeBtn = screen.getByRole("button", { name: /toggle-theme-button/i });
    expect(themeBtn).toHaveTextContent(/Dark/i);

    await user.click(themeBtn);
    expect(themeBtn).toHaveTextContent(/Light/i);
  });
});

