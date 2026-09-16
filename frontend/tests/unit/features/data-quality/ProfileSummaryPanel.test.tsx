import { describe, it, expect } from "vitest";
import { render, screen } from "@testing-library/react";
import React from "react";
import { ProfileSummaryPanel } from "@features/data-quality/components/ProfileSummaryPanel";
import type { DatasetProfileDto } from "@shared/types/api-contracts";

describe("ProfileSummaryPanel", () => {
  it("renders empty state message when profile is null", () => {
    render(<ProfileSummaryPanel profile={null} />);
    expect(screen.getByText(/No dataset profile available/i)).toBeInTheDocument();
  });

  it("renders deterministic profile with columns and cardinality", () => {
    const mockProfile: DatasetProfileDto = {
      id: "p1",
      datasetName: "Sales2026",
      sourceReference: "sales.csv",
      totalRows: 500,
      profiledAtUtc: "2026-09-16T12:00:00Z",
      columnProfiles: [
        {
          columnName: "OrderId",
          inferredType: "Int64",
          totalRowCount: 500,
          nullCount: 0,
          nullRatio: 0.0,
          distinctCount: 500,
          topValues: ["1", "2"],
          detectedPatternRegex: "^\\d+$",
          cardinalityClass: "High"
        },
        {
          columnName: "Region",
          inferredType: "String",
          totalRowCount: 500,
          nullCount: 0,
          nullRatio: 0.0,
          distinctCount: 3,
          topValues: ["US", "EU"],
          detectedPatternRegex: "^[A-Z]+$",
          cardinalityClass: "Low"
        }
      ]
    };

    render(<ProfileSummaryPanel profile={mockProfile} />);

    expect(screen.getByText("Sales2026")).toBeInTheDocument();
    expect(screen.getByText(/Deterministic Profile/i)).toBeInTheDocument();
    expect(screen.getByText("OrderId")).toBeInTheDocument();
    expect(screen.getByText("Region")).toBeInTheDocument();
    expect(screen.getByText("High")).toBeInTheDocument();
    expect(screen.getByText("Low")).toBeInTheDocument();
  });
});

