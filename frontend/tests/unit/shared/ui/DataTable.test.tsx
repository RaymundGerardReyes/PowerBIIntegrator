import { describe, it, expect } from "vitest";
import { screen } from "@testing-library/react";
import { renderWithProviders } from "../../../setup/test-utils";
import { DataTable } from "@shared/ui/DataTable/DataTable";

describe("DataTable", () => {
  it("renders headers and row values", () => {
    renderWithProviders(
      <DataTable columns={[{ key: "name", header: "Name" }]} rows={[{ name: "Revenue" }]} />
    );
    expect(screen.getByText("Name")).toBeInTheDocument();
    expect(screen.getByText("Revenue")).toBeInTheDocument();
  });
});
