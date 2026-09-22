import React from "react";
import { describe, it, expect } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { AuthProvider, useAuthContext } from "@app/providers/AuthProvider";

const ConsumerComponent: React.FC = () => {
  const { user, isAuthenticated, login, logout } = useAuthContext();

  return (
    <div>
      <div data-testid="auth-status">{isAuthenticated ? "authenticated" : "unauthenticated"}</div>
      <div data-testid="user-email">{user ? user.email : "none"}</div>
      <button
        onClick={() =>
          login({
            id: "u-123",
            email: "analyst@enterprise.com",
            displayName: "Test Analyst",
            roles: ["PowerBiAuthor"]
          })
        }
      >
        Log In
      </button>
      <button onClick={logout}>Log Out</button>
    </div>
  );
};

describe("AuthProvider Unit Tests", () => {
  it("initializes with unauthenticated state", () => {
    render(
      <AuthProvider>
        <ConsumerComponent />
      </AuthProvider>
    );

    expect(screen.getByTestId("auth-status")).toHaveTextContent("unauthenticated");
    expect(screen.getByTestId("user-email")).toHaveTextContent("none");
  });

  it("updates state to authenticated upon login", async () => {
    const user = userEvent.setup();
    render(
      <AuthProvider>
        <ConsumerComponent />
      </AuthProvider>
    );

    await user.click(screen.getByRole("button", { name: /Log In/i }));

    expect(screen.getByTestId("auth-status")).toHaveTextContent("authenticated");
    expect(screen.getByTestId("user-email")).toHaveTextContent("analyst@enterprise.com");
  });

  it("clears user and reverts to unauthenticated upon logout", async () => {
    const user = userEvent.setup();
    render(
      <AuthProvider>
        <ConsumerComponent />
      </AuthProvider>
    );

    await user.click(screen.getByRole("button", { name: /Log In/i }));
    expect(screen.getByTestId("auth-status")).toHaveTextContent("authenticated");

    await user.click(screen.getByRole("button", { name: /Log Out/i }));
    expect(screen.getByTestId("auth-status")).toHaveTextContent("unauthenticated");
    expect(screen.getByTestId("user-email")).toHaveTextContent("none");
  });

  it("throws an error when useAuthContext is used outside AuthProvider", () => {
    const BadConsumer = () => {
      useAuthContext();
      return null;
    };

    expect(() => render(<BadConsumer />)).toThrow(
      "useAuthContext must be used within AuthProvider"
    );
  });
});

