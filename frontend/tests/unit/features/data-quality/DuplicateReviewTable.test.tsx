import React from "react";
import { describe, it, expect, vi } from "vitest";
import { render, screen, fireEvent } from "@testing-library/react";
import { DuplicateReviewTable, DuplicateReviewItem } from "@features/data-quality/components/DuplicateReviewTable";

describe("DuplicateReviewTable", () => {
  it("renders EmptyState when clusters list is empty", () => {
    render(<DuplicateReviewTable clusters={[]} />);
    expect(screen.getByText("No Duplicate Clusters Detected")).toBeInTheDocument();
  });

  it("renders duplicate clusters table with correct records and confidence", () => {
    const mockClusters: DuplicateReviewItem[] = [
      {
        clusterId: "cluster-01",
        ruleFired: "ExactHashDedupe",
        keptRowId: "row_0",
        droppedRowIds: ["row_2"],
        confidenceScore: 1.0,
        reasonCode: "Exact match"
      },
      {
        clusterId: "cluster-02",
        ruleFired: "CompositeKeyDedupe",
        keptRowId: "row_5",
        droppedRowIds: ["row_8", "row_9"],
        confidenceScore: 0.95,
        reasonCode: "Composite key collision on EmployeeId + System"
      }
    ];

    render(<DuplicateReviewTable clusters={mockClusters} />);

    expect(screen.getByText("Deduplication Review")).toBeInTheDocument();
    expect(screen.getByText("2 Duplicate Cluster(s)")).toBeInTheDocument();
    expect(screen.getByText("cluster-01")).toBeInTheDocument();
    expect(screen.getByText("cluster-02")).toBeInTheDocument();
    expect(screen.getByText("100%")).toBeInTheDocument();
    expect(screen.getByText("95%")).toBeInTheDocument();
  });

  it("triggers onKeepOverride when Swap button is clicked", () => {
    const onKeepOverride = vi.fn();
    const mockClusters: DuplicateReviewItem[] = [
      {
        clusterId: "cluster-01",
        ruleFired: "CompositeKeyDedupe",
        keptRowId: "row_0",
        droppedRowIds: ["row_2"],
        confidenceScore: 1.0,
        reasonCode: "Composite collision"
      }
    ];

    render(<DuplicateReviewTable clusters={mockClusters} onKeepOverride={onKeepOverride} />);

    const swapButton = screen.getByRole("button", { name: "Swap" });
    fireEvent.click(swapButton);

    expect(onKeepOverride).toHaveBeenCalledWith("cluster-01", "row_2");
  });
});
