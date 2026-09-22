import React, { useState } from "react";
import { describe, it, expect, vi } from "vitest";
import { render, screen, fireEvent } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { Modal } from "@shared/ui/Modal/Modal";

describe("Modal Accessibility & Interaction Tests", () => {
  it("renders with dialog role and aria-modal=true when open", () => {
    const handleClose = vi.fn();
    render(
      <Modal open={true} onClose={handleClose} title="Test Modal Dialog">
        <div>Modal Content Body</div>
      </Modal>
    );

    const dialog = screen.getByRole("dialog");
    expect(dialog).toBeInTheDocument();
    expect(dialog).toHaveAttribute("aria-modal", "true");
    expect(screen.getByText("Test Modal Dialog")).toBeInTheDocument();
  });

  it("does not render into DOM when open is false", () => {
    const handleClose = vi.fn();
    render(
      <Modal open={false} onClose={handleClose} title="Hidden Modal">
        <div>Hidden Content</div>
      </Modal>
    );

    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
  });

  it("closes when the Escape key is pressed", () => {
    const handleClose = vi.fn();
    render(
      <Modal open={true} onClose={handleClose} title="Escape Test">
        <div>Press Escape</div>
      </Modal>
    );

    fireEvent.keyDown(window, { key: "Escape" });
    expect(handleClose).toHaveBeenCalledTimes(1);
  });

  it("closes when the backdrop overlay is clicked", async () => {
    const handleClose = vi.fn();
    const user = userEvent.setup();
    render(
      <Modal open={true} onClose={handleClose} title="Backdrop Test">
        <div>Click outside to close</div>
      </Modal>
    );

    const overlay = screen.getByRole("dialog");
    await user.click(overlay);
    expect(handleClose).toHaveBeenCalledTimes(1);
  });

  it("does not close when clicking inside the modal content box", async () => {
    const handleClose = vi.fn();
    const user = userEvent.setup();
    render(
      <Modal open={true} onClose={handleClose} title="Inner Click Test">
        <button>Inside Button</button>
      </Modal>
    );

    await user.click(screen.getByRole("button", { name: "Inside Button" }));
    expect(handleClose).not.toHaveBeenCalled();
  });

  it("closes when clicking the close button", async () => {
    const handleClose = vi.fn();
    const user = userEvent.setup();
    render(
      <Modal open={true} onClose={handleClose} title="Close Button Test">
        <div>Content</div>
      </Modal>
    );

    const closeBtn = screen.getByRole("button", { name: /close modal/i });
    await user.click(closeBtn);
    expect(handleClose).toHaveBeenCalledTimes(1);
  });
});
