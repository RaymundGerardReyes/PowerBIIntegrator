import { describe, it, expect, vi } from "vitest";
import { screen, fireEvent, act } from "@testing-library/react";
import { renderWithProviders } from "../../../setup/test-utils";
import { ExposureUnlockDialog } from "@features/ai-advisory/components/ExposureUnlockDialog";

describe("ExposureUnlockDialog Component", () => {
  it("renders insufficient privilege warning for non-steward roles", () => {
    renderWithProviders(
      <ExposureUnlockDialog
        isOpen={true}
        runId="run-demo-001"
        userRole="Viewer"
        onClose={vi.fn()}
        onConfirmUnlock={vi.fn()}
      />
    );

    expect(screen.getByText(/Insufficient Privilege/i)).toBeInTheDocument();
    expect(screen.getByText(/does not hold permission to unlock confidential sample rows/i)).toBeInTheDocument();
  });

  it("renders justification form for DataSteward role and submits reason", async () => {
    const handleConfirm = vi.fn().mockResolvedValue(undefined);
    const handleClose = vi.fn();

    renderWithProviders(
      <ExposureUnlockDialog
        isOpen={true}
        runId="run-demo-001"
        userRole="DataSteward"
        onClose={handleClose}
        onConfirmUnlock={handleConfirm}
      />
    );

    expect(screen.getByText(/Governed Least-Privilege Guardrails/i)).toBeInTheDocument();
    expect(screen.getByText(/LocalOllama/i)).toBeInTheDocument();

    const textarea = screen.getByPlaceholderText(/Verifying duplicate false-positive customer tuples/i);
    fireEvent.change(textarea, { target: { value: "Auditing suspicious duplicate cluster row values" } });

    const submitBtn = screen.getByText(/Unlock & Force Local Execution/i);
    await act(async () => {
      fireEvent.click(submitBtn);
    });

    expect(handleConfirm).toHaveBeenCalledWith("Auditing suspicious duplicate cluster row values");
  });

  it("rejects submission and displays error when justification is empty", async () => {
    const handleConfirm = vi.fn();
    renderWithProviders(
      <ExposureUnlockDialog
        isOpen={true}
        runId="run-demo-002"
        userRole="DataSteward"
        onClose={vi.fn()}
        onConfirmUnlock={handleConfirm}
      />
    );

    const submitBtn = screen.getByText(/Unlock & Force Local Execution/i);
    await act(async () => {
      fireEvent.click(submitBtn);
    });

    expect(screen.getByText(/Please provide a business justification for unlocking sample rows/i)).toBeInTheDocument();
    expect(handleConfirm).not.toHaveBeenCalled();
  });

  it("does not render when isOpen is false", () => {
    const { container } = renderWithProviders(
      <ExposureUnlockDialog
        isOpen={false}
        runId="run-demo-001"
        userRole="DataSteward"
        onClose={vi.fn()}
        onConfirmUnlock={vi.fn()}
      />
    );

    expect(container.firstChild).toBeNull();
  });
});
