import React from "react";
import { describe, it, expect, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { WorkflowStepper, type WorkflowStep } from "@shared/ui/WorkflowStepper/WorkflowStepper";

describe("WorkflowStepper Accessibility & Interaction Tests", () => {
  const steps: WorkflowStep[] = [
    { id: "profile", label: "Profile", status: "completed" },
    { id: "dedupe", label: "Deduplicate", status: "completed" },
    { id: "clean", label: "Clean", status: "active" },
    { id: "transform", label: "Transform", status: "pending" },
    { id: "visuals", label: "Suggest Visuals", status: "blocked" },
    { id: "advisory", label: "AI Advisory", status: "pending", hasBadge: true, badgeCount: 3 }
  ];

  it("renders tablist container with all step buttons", () => {
    const handleClick = vi.fn();
    render(<WorkflowStepper steps={steps} onStepClick={handleClick} />);

    expect(screen.getByRole("tablist")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /Profile/i })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /Clean/i })).toBeInTheDocument();
  });

  it("sets aria-current='step' and aria-selected=true on the active step only", () => {
    const handleClick = vi.fn();
    render(<WorkflowStepper steps={steps} onStepClick={handleClick} />);

    const activeStepBtn = screen.getByRole("button", { name: /Clean/i });
    expect(activeStepBtn).toHaveAttribute("aria-current", "step");
    expect(activeStepBtn).toHaveAttribute("aria-selected", "true");

    const pendingStepBtn = screen.getByRole("button", { name: /Transform/i });
    expect(pendingStepBtn).not.toHaveAttribute("aria-current");
    expect(pendingStepBtn).toHaveAttribute("aria-selected", "false");
  });

  it("disables blocked steps and prevents click events", async () => {
    const handleClick = vi.fn();
    const user = userEvent.setup();
    render(<WorkflowStepper steps={steps} onStepClick={handleClick} />);

    const blockedBtn = screen.getByRole("button", { name: /Suggest Visuals/i });
    expect(blockedBtn).toBeDisabled();

    await user.click(blockedBtn);
    expect(handleClick).not.toHaveBeenCalled();
  });

  it("calls onStepClick with step id when an enabled step is clicked", async () => {
    const handleClick = vi.fn();
    const user = userEvent.setup();
    render(<WorkflowStepper steps={steps} onStepClick={handleClick} />);

    const completedBtn = screen.getByRole("button", { name: /Profile/i });
    await user.click(completedBtn);
    expect(handleClick).toHaveBeenCalledWith("profile");
  });

  it("renders badge count when hasBadge and badgeCount are provided", () => {
    const handleClick = vi.fn();
    render(<WorkflowStepper steps={steps} onStepClick={handleClick} />);

    expect(screen.getByText("3")).toBeInTheDocument();
  });
});

