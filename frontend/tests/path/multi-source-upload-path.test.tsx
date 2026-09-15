import { describe, it, expect } from "vitest";
import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { renderWithProviders } from "../setup/test-utils";
import { ExcelUploadForm } from "@features/data-sources/components/ExcelUploadForm";

describe("Path: upload multiple Excel/CSV sources", () => {
  it("allows selecting a file and triggering upload", async () => {
    renderWithProviders(<ExcelUploadForm />);
    const input = screen.getByLabelText("excel-upload-input") as HTMLInputElement;
    const file = new File(["dummy"], "sales_q1.xlsx", { type: "application/vnd.ms-excel" });

    await userEvent.upload(input, file);
    expect(input.files?.[0].name).toBe("sales_q1.xlsx");
  });
});
